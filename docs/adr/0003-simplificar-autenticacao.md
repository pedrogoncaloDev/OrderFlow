# ADR 0003: Simplificar a autenticação (sem camadas intermediárias)

**Status:** Aceita. Substitui a parte do ADR 0002 que descreve a estrutura em camadas (`AuthService`, repositório, interfaces e `AuthResult`). O restante do ADR 0002 (JWT, hash de senha, sessão no Blazor) continua valendo.

## Contexto
Cadastro, login e `/me` ocupavam cerca de 20 arquivos: serviço, três interfaces, repositório, `AuthResult`, cinco DTOs e testes com repositório falso. Para três endpoints, o fluxo ficou difícil de acompanhar e de manter por uma pessoa só. As abstrações só se pagavam nos testes unitários.

## Decisão
- A lógica fica no `AuthController` (Api), que usa o `AppDbContext` diretamente.
- `IdentityPasswordHasher` e `JwtTokenGenerator` (Infrastructure) viram classes concretas, registradas no DI sem interface.
- Os corpos das requisições (`RegisterRequest`, `LoginRequest`) são classes pequenas no mesmo arquivo do controller, com as validações em DataAnnotations. As respostas são objetos anônimos; o formato JSON não mudou (`accessToken`, `expiresAtUtc`, `user`).
- E-mail duplicado é resolvido só pelo índice único do banco (409 ao violar), sem consulta prévia.
- Foram removidos `AuthService`, `IAuthService`, `IUserRepository`/`UserRepository`, `IPasswordHasher`, `ITokenGenerator`, `AuthResult` e os DTOs da Application, além dos testes unitários do `AuthService`, que dependiam dessas abstrações.

## Consequências
- Muito menos arquivos e um único lugar para ler o fluxo de auth.
- Testar o auth passa a exigir banco: falta cobrir `/api/auth/*` com testes de integração (Testcontainers + Postgres).
- A Application fica só com `AuthClaimTypes` e `PasswordPolicy`. Se surgirem regras compartilhadas entre controllers ou com o Worker, extrair um serviço nessa hora, não antes.
- Regras do `CLAUDE.md` mantidas: `Role` definida no servidor, hash de senha nunca na resposta.
