# Guia de estudo: cadastro e login no OrderFlow

Este guia explica **o que foi feito, onde está e por quê**, para você conseguir defender cada decisão numa entrevista. Leia o código junto com ele. A estrutura foi simplificada no ADR 0003: o fluxo de auth está quase todo em um arquivo.

## 1. O caminho de um login

```
Navegador ──▶ Login.razor ──▶ AuthApiClient ──HTTP──▶ AuthController ──▶ AppDbContext (Postgres)
                                                          │  ├─ IdentityPasswordHasher (PBKDF2)
                                                          │  └─ JwtTokenGenerator (JWT)
   localStorage (token criptografado) ◀── JwtAuthenticationStateProvider ◀── devolve { accessToken, expiresAtUtc, user }
```

1. O usuário preenche o formulário (`Pages/Login.razor`). O Blazor valida com DataAnnotations antes de enviar.
2. `AuthApiClient` faz `POST /api/auth/login` na API.
3. `AuthController` recebe o JSON em um `LoginRequest`; o `[ApiController]` já devolve 400 se os dados forem inválidos.
4. A action `Login` busca o usuário pelo e-mail, confere a senha com o hash e pede um token.
5. A resposta volta com o JWT; `JwtAuthenticationStateProvider.SignInAsync` guarda o token (criptografado) no navegador e avisa o Blazor que o usuário mudou.
6. Em `/perfil`, o token é enviado em `Authorization: Bearer ...` para `GET /api/auth/me`. A API valida assinatura, emissor, audiência e validade antes de responder.

## 2. Mapa dos arquivos

| Camada | Arquivo | Papel |
|---|---|---|
| Api | `Features/Auth/AuthController.cs` | Endpoints `/api/auth/*`, a lógica de cadastro/login/me |
| Api | `Features/Auth/RegisterRequest.cs`, `LoginRequest.cs`, `ChangePasswordRequest.cs`, `AuthResponse.cs`, `UserResponse.cs` | Corpos de entrada e de resposta dos endpoints |
| Api | `Features/Auth/AuthExtensions.cs` | Injeção de dependência e configuração do JwtBearer |
| Application | `Auth/PasswordPolicy.cs` | Regras de senha (tamanho, letras e números) |
| Application | `Auth/AuthClaimTypes.cs` | Nomes das claims do JWT (`sub`, `email`, `role`) |
| Infrastructure | `Auth/IdentityPasswordHasher.cs` | Hash e verificação de senha |
| Infrastructure | `Auth/JwtTokenGenerator.cs`, `JwtOptions.cs` | Criação do token |
| Infrastructure | `Persistence/AppDbContext.cs` | Acesso ao banco (tabela `users`) |
| Web | `Features/Auth/JwtAuthenticationStateProvider.cs` | Guarda o token e diz ao Blazor quem está logado |
| Web | `Features/Auth/AuthApiClient.cs` | Fala com a API e converte erros em mensagens |
| Web | `Features/Auth/Login.razor`, `Register.razor`, `Profile.razor` | Telas |
| Web | `Features/Auth/RequireAuth.razor` | Protege conteúdo: quem não está logado vai para o login |

## 3. Conceitos para dominar

- **Hash de senha**: nunca se guarda a senha, só um hash com *salt* aleatório. PBKDF2 é lento de propósito, o que dificulta ataques de força bruta. Não é criptografia: não dá para "descriptografar".
- **JWT**: três partes (cabeçalho, payload, assinatura) em Base64. O payload é **legível por qualquer um**; a assinatura garante que ninguém o alterou. Por isso não se coloca segredo dentro do token.
- **Claims**: afirmações sobre o usuário dentro do token (`sub` = id, `email`, `role`).
- **Autenticação × autorização**: autenticar é provar quem você é (login); autorizar é decidir o que você pode fazer (`[Authorize]`, papéis).
- **Corpo da requisição**: `RegisterRequest` e `LoginRequest` dizem quais campos o JSON deve ter e quais validações valem. Campos que não existem na classe (como `role`) são ignorados.
- **Injeção de dependência**: o controller pede `AppDbContext`, `IdentityPasswordHasher` e `JwtTokenGenerator` no construtor; quem monta tudo é a API (`AddJwtAuth`, `AddDatabase`).
- **Tempo de vida (lifetime)**: o `AppDbContext` é `Scoped` (um por requisição); hasher e gerador de token são `Singleton` (não guardam estado).
- **ProblemDetails**: formato padrão (RFC 9457) para respostas de erro da API.

## 4. Perguntas prováveis de entrevista

**Por que o JWT e não cookie/sessão?**
A API é stateless e pode servir vários clientes (Blazor hoje, outros amanhã). O token carrega a identidade, sem estado no servidor. Contrapartida: não dá para revogar um token antes de expirar (sem refresh token ou lista de bloqueio).

**Onde o token fica guardado e isso é seguro?**
No `localStorage`, mas criptografado pelo servidor (`ProtectedLocalStorage`): um script injetado na página não consegue ler o JWT. Ainda assim, quem tiver acesso ao navegador do usuário pode usar a sessão.

**Por que o login devolve a mesma mensagem para e-mail inexistente e senha errada?**
Para não revelar quais e-mails têm conta (enumeração de usuários). Melhoria possível: igualar também o tempo de resposta.

**O que acontece se dois cadastros com o mesmo e-mail chegam ao mesmo tempo?**
O índice único do banco recusa o segundo, e o controller transforma essa violação em 409. A garantia está no banco, não no código.

**Por que `Role` não vem do formulário?**
Se viesse, qualquer pessoa se cadastraria como Admin. O servidor decide: o cadastro sempre cria `Customer`, e `RegisterRequest` nem tem esse campo.

**O front-end valida o token?**
Não verifica a assinatura (não tem a chave). Lê as claims só para mostrar o e-mail e o papel. Quem protege os dados é a API.

**Por que a lógica está no controller, sem camada de serviço?**
Eram três endpoints, e as camadas extras só se pagavam nos testes unitários. Foi uma troca consciente (ADR 0003): menos código para ler, em troca de testar o auth com banco. Se surgirem regras reaproveitadas por vários controllers ou pelo Worker, extrai-se um serviço nessa hora.

**Como testar?**
Hoje não há testes do auth. O caminho é teste de integração (Testcontainers + Postgres) chamando `/api/auth/*`.

**O que melhoraria?**
Refresh token, limite de tentativas de login, confirmação de e-mail, testes de integração, persistência das chaves do Data Protection em produção e tratamento global de erros.

## 5. Como rodar

1. `docker compose up -d` (Postgres e RabbitMQ).
2. `dotnet run --project src/OrderFlow.Api --launch-profile http` (API em http://localhost:5222; aplica as migrations sozinha).
3. Em outro terminal: `dotnet run --project src/OrderFlow.Web` (abre o navegador em http://localhost:5021).
4. Crie uma conta em **Criar conta**; você já entra logado e pode ver `/perfil`.

Para testar só a API, use `src/OrderFlow.Api/OrderFlow.Api.http` no Visual Studio.

## 6. Exercícios para fixar (faça você)

- Adicione o endpoint `POST /api/auth/change-password`: um corpo novo no fim do `AuthController.cs` e uma action nova, que busca o usuário pelo `sub` do token.
- Troque a regra de senha (por exemplo, exigir um símbolo) e veja em quais arquivos precisa mexer (`PasswordPolicy` e o formulário do Web).
- Faça o token expirar em 5 minutos e observe o que o front-end faz.
