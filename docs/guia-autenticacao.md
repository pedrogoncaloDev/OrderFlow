# Guia de estudo: cadastro e login no OrderFlow

Este guia explica **o que foi feito, onde está e por quê**, para você conseguir defender cada decisão numa entrevista. Leia o código junto com ele.

## 1. O caminho de um login

```
Navegador ──▶ Login.razor ──▶ AuthApiClient ──HTTP──▶ AuthController ──▶ AuthService
                                                                            │  ├─ IUserRepository (EF Core + Postgres)
                                                                            │  ├─ IPasswordHasher (PBKDF2)
                                                                            │  └─ ITokenGenerator (JWT)
   localStorage (token criptografado) ◀── JwtAuthenticationStateProvider ◀──┘  devolve AuthResponse
```

1. O usuário preenche o formulário (`Pages/Login.razor`). O Blazor valida com DataAnnotations antes de enviar.
2. `AuthApiClient` faz `POST /api/auth/login` na API.
3. `AuthController` recebe o JSON; o `[ApiController]` já devolve 400 se os dados forem inválidos.
4. `AuthService.LoginAsync` busca o usuário pelo e-mail, confere a senha com o hash e pede um token.
5. A resposta volta com o JWT; `JwtAuthenticationStateProvider.SignInAsync` guarda o token (criptografado) no navegador e avisa o Blazor que o usuário mudou.
6. Em `/perfil`, o token é enviado em `Authorization: Bearer ...` para `GET /api/auth/me`. A API valida assinatura, emissor, audiência e validade antes de responder.

## 2. Mapa dos arquivos

| Camada | Arquivo | Papel |
|---|---|---|
| Application | `Auth/AuthService.cs` | Regras de cadastro e login |
| Application | `Auth/IUserRepository.cs`, `IPasswordHasher.cs`, `ITokenGenerator.cs` | Contratos que a Infrastructure implementa |
| Application | `Auth/RegisterRequest.cs`, `LoginRequest.cs`, `AuthResponse.cs` | DTOs (entrada e saída) |
| Application | `Auth/AuthResult.cs` | Resultado sem exceções: sucesso ou código de erro |
| Infrastructure | `Persistence/Repositories/UserRepository.cs` | Consultas EF Core; trata e-mail duplicado |
| Infrastructure | `Auth/IdentityPasswordHasher.cs` | Hash e verificação de senha |
| Infrastructure | `Auth/JwtTokenGenerator.cs`, `JwtOptions.cs` | Criação do token |
| Api | `Controllers/AuthController.cs` | Endpoints `/api/auth/*` |
| Api | `Extensions/AuthExtensions.cs` | Injeção de dependência e configuração do JwtBearer |
| Web | `Services/JwtAuthenticationStateProvider.cs` | Guarda o token e diz ao Blazor quem está logado |
| Web | `Services/AuthApiClient.cs` | Fala com a API e converte erros em mensagens |
| Web | `Components/Pages/Login.razor`, `Register.razor`, `Profile.razor` | Telas |
| Web | `Components/Shared/RequireAuth.razor` | Protege conteúdo: quem não está logado vai para o login |
| Testes | `tests/OrderFlow.UnitTests/Auth/AuthServiceTests.cs` | Testes do fluxo com repositório falso |

## 3. Conceitos para dominar

- **Hash de senha**: nunca se guarda a senha, só um hash com *salt* aleatório. PBKDF2 é lento de propósito, o que dificulta ataques de força bruta. Não é criptografia: não dá para "descriptografar".
- **JWT**: três partes (cabeçalho, payload, assinatura) em Base64. O payload é **legível por qualquer um**; a assinatura garante que ninguém o alterou. Por isso não se coloca segredo dentro do token.
- **Claims**: afirmações sobre o usuário dentro do token (`sub` = id, `email`, `role`).
- **Autenticação × autorização**: autenticar é provar quem você é (login); autorizar é decidir o que você pode fazer (`[Authorize]`, papéis).
- **Injeção de dependência**: o `AuthService` pede interfaces no construtor; quem monta tudo é a API (`AddJwtAuth`). Nos testes, trocamos por versões falsas.
- **Inversão de dependência**: a Application define *o que precisa* (interfaces); a Infrastructure define *como fazer*. A dependência nunca aponta para fora.
- **Tempo de vida (lifetime)**: o repositório é `Scoped` (usa o DbContext, um por requisição); hasher e gerador de token são `Singleton` (não guardam estado).
- **ProblemDetails**: formato padrão (RFC 9457) para respostas de erro da API.

## 4. Perguntas prováveis de entrevista

**Por que o JWT e não cookie/sessão?**
A API é stateless e pode servir vários clientes (Blazor hoje, outros amanhã). O token carrega a identidade, sem estado no servidor. Contrapartida: não dá para revogar um token antes de expirar (sem refresh token ou lista de bloqueio).

**Onde o token fica guardado e isso é seguro?**
No `localStorage`, mas criptografado pelo servidor (`ProtectedLocalStorage`): um script injetado na página não consegue ler o JWT. Ainda assim, quem tiver acesso ao navegador do usuário pode usar a sessão.

**Por que o login devolve a mesma mensagem para e-mail inexistente e senha errada?**
Para não revelar quais e-mails têm conta (enumeração de usuários). Melhoria possível: igualar também o tempo de resposta.

**O que acontece se dois cadastros com o mesmo e-mail chegam ao mesmo tempo?**
A checagem em código pode passar nas duas; o índice único do banco recusa a segunda, e `TryAddAsync` transforma essa violação em `false`. A garantia final está no banco, não no código.

**Por que `Role` não vem do formulário?**
Se viesse, qualquer pessoa se cadastraria como Admin. O servidor decide.

**Por que `AuthResult` em vez de lançar exceção?**
E-mail repetido e senha errada são situações normais, não erros do sistema. Exceções são caras e escondem o fluxo; o resultado explícito obriga quem chama a tratar cada caso.

**O front-end valida o token?**
Não verifica a assinatura (não tem a chave). Lê as claims só para mostrar o e-mail e o papel. Quem protege os dados é a API.

**Como você testou?**
`AuthServiceTests` cobre cadastro, normalização de e-mail, duplicidade (inclusive a corrida), login certo e errado, com repositório e hasher falsos, sem banco. Falta cobrir a API de ponta a ponta (testes de integração com Postgres).

**O que melhoraria?**
Refresh token, limite de tentativas de login, confirmação de e-mail, testes de integração, persistência das chaves do Data Protection em produção e tratamento global de erros.

## 5. Como rodar

1. `docker compose up -d` (Postgres e RabbitMQ).
2. `dotnet run --project src/OrderFlow.Api --launch-profile http` (API em http://localhost:5222; aplica as migrations sozinha).
3. Em outro terminal: `dotnet run --project src/OrderFlow.Web` (abre o navegador em http://localhost:5021).
4. Crie uma conta em **Criar conta**; você já entra logado e pode ver `/perfil`.

Para testar só a API, use `src/OrderFlow.Api/OrderFlow.Api.http` no Visual Studio.

## 6. Exercícios para fixar (faça você)

- Adicione o endpoint `POST /api/auth/change-password` e escreva os testes dele.
- Troque a regra de senha (por exemplo, exigir um símbolo) e veja em quais arquivos precisa mexer.
- Faça o token expirar em 5 minutos e observe o que o front-end faz.
