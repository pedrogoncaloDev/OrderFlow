# CLAUDE.md — Instruções para trabalhar no OrderFlow

Leia também `CONTEXT.md` (visão do produto, arquitetura, modelo de dados e estado atual).

## O que é
OrderFlow: sistema de pedidos em **.NET 9** (projeto de portfólio) com API REST, mensageria (RabbitMQ) e front-end Blazor. Monorepo (ver `docs/adr/0001-monorepo.md`).

## Stack
- **.NET 9** (`net9.0`, `Nullable` e `ImplicitUsings` habilitados em todos os projetos).
- **API**: ASP.NET Core Web API com Controllers + OpenAPI (`Microsoft.AspNetCore.OpenApi`). Autenticação JWT implementada (`/api/auth/register`, `/login`, `/me`; ver ADR 0002 e ADR 0003: a lógica fica direto no `AuthController`, sem serviço nem repositório). Swagger UI ainda não.
- **Banco**: PostgreSQL 15 via EF Core 9 + `Npgsql.EntityFrameworkCore.PostgreSQL` + `EFCore.NamingConventions` (snake_case).
- **Mensageria**: RabbitMQ 3 (management) — subido no Docker, ainda sem código de publicação/consumo.
- **Web**: Blazor (Razor Components, render mode Interactive Server).
- **Testes**: xUnit 2.9 (`tests/OrderFlow.UnitTests`, `tests/OrderFlow.IntegrationTests`) — hoje só há placeholders.
- **Config local**: `.env` na raiz carregado por `DotNetEnv` (`Env.TraversePath().Load()` na API).
- **CI**: `.github/workflows/ci.yml` — restore, build (Release) e test em push/PR para `main`, com .NET 9.0.x.

## Comandos (rodar na raiz do repo)

### Infra (Postgres + RabbitMQ)
```
cp .env.example .env      # ajuste as senhas; o .env NÃO vai para o git
docker compose up -d
```
RabbitMQ Management: http://localhost:15672 · PostgreSQL: `localhost:5432` (credenciais do `.env`).

### Build e testes
```
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

### Rodar
```
dotnet run --project src/OrderFlow.Api --launch-profile http     # http://localhost:5222
dotnet run --project src/OrderFlow.Web
dotnet run --project src/OrderFlow.Worker
```
Com `Database:AutoMigrate = true` (padrão em `appsettings.json`), a API aplica as migrations ao iniciar.

### Migrations (EF Core)
O `DbContext` e as migrations ficam em `OrderFlow.Infrastructure`; o startup project é a API:
```
dotnet ef migrations add NomeDaMigration --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```
(requer `dotnet tool install --global dotnet-ef`). Mudou entidade ou configuration → gere migration e **revise** o arquivo gerado antes de commitar. Nunca edite uma migration já aplicada/mergeada; crie outra.

## Arquitetura e regras de dependência
```
Domain  ←  Application  ←  Infrastructure
                ↑               ↑
                └──────  Api  ──┘      Api também referencia Contracts
Worker → Infrastructure + Contracts      Web → (nada por enquanto; falará com a API via HTTP)
```
- **Domain**: entidades e enums; sem dependências de pacotes. Não referencie EF Core aqui.
- **Application**: casos de uso, validações e DTOs; depende só de Domain.
- **Infrastructure**: EF Core (`AppDbContext`, `Configurations/`, `Migrations/`) e, futuramente, mensageria.
- **Contracts**: eventos/contratos compartilhados entre API e Worker.
- **Api** é o composition root (DI, pipeline HTTP). Mapeamento de entidades no banco fica em `IEntityTypeConfiguration<T>` (Fluent API), não em data annotations; `AppDbContext` aplica tudo com `ApplyConfigurationsFromAssembly`.
- Dependência nunca aponta "para fora": Domain não conhece Application, que não conhece Infrastructure.

## Convenções
- **Organização por feature** (ADR 0005): em `OrderFlow.Api` e `OrderFlow.Web`, cada recurso tem a própria pasta em `Features/` (`Features/Auth/`, `Features/Products/`) com controller/página, requests, responses, extensions e cliente HTTP juntos; o que é de mais de uma feature fica em `Common/`, ao lado de `Features/`. Namespace = pasta (`OrderFlow.Api.Features.Products`). Recurso novo → pasta nova em `Features/`. Domain, Application e Infrastructure continuam por camada.
- **Idioma**: commits, comentários, mensagens de erro e documentação em **português (pt-BR)**; identificadores de código em inglês.
- **Commits**: Conventional Commits em português — `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`.
- **Commits são do usuário**: o Claude **não faz commits** (nem `git commit`, nem `--amend`, nem push). No máximo, ao terminar uma tarefa, sugere a mensagem de commit (Conventional Commits em pt-BR) para o usuário commitar. Pode usar `git add`/`git mv` e criar branch quando pedido.
- **Git**: branch por funcionalidade (`feat/feature-name`), PR para `main`. **Nomes de branch sempre em inglês**, em kebab-case, com o prefixo do tipo (`feat/`, `fix/`, `docs/`, `refactor/`, `test/`, `chore/`); ex.: `feat/change-password`. Não commite direto em `main`.
- **Formatação** (`.editorconfig`): UTF-8, **LF**, 4 espaços em C#, 2 espaços em `yml/json/md/csproj`, newline final, sem espaços no fim da linha. Não use tabs. (`*.sln` é CRLF por `.gitattributes`.)
- **Banco**: tabelas e colunas em snake_case (convention); enums persistidos como **texto** (`HasConversion<string>()`); valores monetários com `HasPrecision(18, 2)`; chaves `Guid` geradas na aplicação (`ValueGeneratedNever`); datas em UTC (`DateTime.UtcNow`).
- **Exclusão**: cliente com pedidos e produto já vendido não podem ser apagados (`Restrict`); apagar pedido apaga itens (`Cascade`); apagar cliente zera `users.customer_id` (`SetNull`). Mantenha essas regras ao evoluir o modelo.
- **Testes**: código novo de domínio/aplicação com teste em `tests/OrderFlow.UnitTests`; endpoints e persistência em `tests/OrderFlow.IntegrationTests`.
- **Decisões de arquitetura** novas → ADR em `docs/adr/NNNN-titulo.md` (formato: Status, Contexto, Decisão, Consequências).

## Segurança — regras duras
- **Nunca** leia, imprima ou commite o `.env` (tem credenciais). Use `.env.example` como referência e atualize-o ao criar novas variáveis.
- Também fora do git: `appsettings.Development.local.json`, `*.pfx`, `bin/`, `obj/`, `.vs/`, `*.user`.
- Não coloque segredos (senhas, chaves JWT, connection strings reais) em `appsettings*.json` versionados.
- Senhas só como hash (`User.PasswordHash`), com algoritmo adequado (ex.: ASP.NET Core Identity `PasswordHasher` ou BCrypt/Argon2); nunca retorne o hash em DTOs.
- Defina `Role` no servidor; nunca aceite `Role` vinda do cliente no cadastro.

## Armadilhas conhecidas (detalhes em CONTEXT.md)
- Existem o `AuthController` e o `CustomersController`; os de produtos e pedidos ainda não. Cadastros são **isolados por dono** (ADR 0004): todo acesso filtra por `owner_id` (do claim `sub`, nunca do corpo) e registro de outro usuário responde 404.
- O cliente HTTP do Web que herda de `AuthorizedApiClient` é registrado sozinho (`AddApiClients`); para um módulo novo basta criar a classe.
- O auth não tem camadas intermediárias (ADR 0003): a lógica está no `AuthController` com o `AppDbContext`, e os corpos (`RegisterRequest`, `LoginRequest`) ficam no mesmo arquivo. Não há testes do auth até existirem testes de integração. Em recursos novos, decida caso a caso se vale extrair um serviço.
- A chave do JWT vem de `Jwt__Key` (`.env`). Em Development, se vazia, a API gera uma chave temporária (sessões caem ao reiniciar); fora dele, a API não sobe sem a chave.
- A connection string montada a partir do `.env` usa `Host=localhost` fixo; para API em container será preciso configurar `ConnectionStrings:Default`.
- Há Dockerfiles para a Api e o Web (contexto de build = raiz do repo; ver `docs/deploy.md`); o Worker ainda não tem, e os blocos `api`, `worker` e `web` do `docker-compose.yml` seguem comentados.
- `OrderFlow.Web` tem login, cadastro, perfil e layout próprios. Roda em modo interativo **sem pré-renderização** (o token fica no navegador); não use `[Authorize]` nas páginas, use `<RequireAuth>`.
- `.vs/` existe no disco (Visual Studio) mas está no `.gitignore`; ignore-o em buscas.

## Antes de dar uma tarefa por concluída
1. `dotnet build -c Release` e `dotnet test -c Release` passam sem warnings novos.
2. Entidade/configuration alterada → migration criada e revisada.
3. Nenhum segredo ou arquivo gerado no diff.
4. README/CONTEXT/ADR atualizados se a arquitetura ou o roadmap mudaram.
