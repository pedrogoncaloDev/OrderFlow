# ADR 0002: Autenticação com JWT (API) e sessão no Blazor Server

**Status:** Aceita

## Contexto
O sistema precisa de cadastro e login de usuários. A API é consumida pelo front-end Blazor e, no futuro, por outros clientes. A entidade `User` já existia (e-mail único, `PasswordHash`, `Role`).

## Decisão
- A **API** emite um **JWT** (HMAC-SHA256) em `POST /api/auth/register` e `POST /api/auth/login`; `GET /api/auth/me` é protegido por `[Authorize]`.
- Senhas são guardadas só como **hash** (`PasswordHasher` do ASP.NET Core Identity, PBKDF2 com salt). O papel (`Role`) é sempre `Customer` no cadastro, definido no servidor.
- A lógica fica em `AuthService` (Application), que depende de interfaces (`IUserRepository`, `IPasswordHasher`, `ITokenGenerator`) implementadas na Infrastructure. Isso permite testar o fluxo sem banco.
- Falhas esperadas (e-mail repetido, senha errada) são retornadas como `AuthResult`, não como exceção. A API as traduz para 409 e 401 (`ProblemDetails`). Login com usuário inexistente e com senha errada dão a mesma resposta.
- A corrida de dois cadastros com o mesmo e-mail é resolvida pelo índice único do banco (`TryAddAsync`).
- A chave do JWT vem de `Jwt__Key` (variável de ambiente / `.env`). Em Development, se ausente, a API gera uma chave temporária; fora dele, ausência da chave impede a inicialização.
- No **Blazor** (Interactive Server, sem pré-renderização), o token fica no `localStorage` **criptografado** pelo `ProtectedLocalStorage`, e um `AuthenticationStateProvider` próprio expõe o usuário à interface. As chamadas à API levam o token no cabeçalho `Authorization: Bearer`.

## Consequências
- Sem pré-renderização, a primeira exibição depende do circuito SignalR conectar (um instante de "carregando").
- O front-end lê as claims do token só para exibição; não valida a assinatura. Toda decisão de acesso é da API.
- Não há refresh token nem revogação: o token vale até expirar (60 min por padrão). Logout apenas apaga o token do navegador.
- Fora do ambiente local, será preciso persistir as chaves do Data Protection do Web (por exemplo em volume no Docker), senão sessões salvas ficam ilegíveis a cada reinício.
- Futuro: refresh token, bloqueio por tentativas de login, confirmação de e-mail.
