# CONTEXT.md — Contexto do projeto OrderFlow

> Instruções operacionais (comandos, convenções, regras) estão em `CLAUDE.md`. Este arquivo explica **o quê** e **por quê**, e o estado atual.
> Última atualização: 2026-10-04 (branch `refactor/simplify-auth`, a partir da `main`).

## 1. Visão do produto
**OrderFlow** é um sistema de pedidos com **API REST**, **mensageria** e **front-end Blazor**, feito em .NET. É um **projeto de portfólio**: o objetivo é demonstrar uma arquitetura em camadas, EF Core com migrations automáticas, autenticação JWT, um worker consumindo eventos via RabbitMQ e uma interface web.

Domínio: **clientes** fazem **pedidos** compostos por **itens** (produtos com quantidade e preço congelado no momento da compra). **Usuários** autenticam na plataforma com papel `Customer` (opcionalmente vinculado a um cliente) ou `Admin`.

Autor: Pedro Gonçalo (GitHub `pedrogoncaloDev`). Repositório: github.com/pedrogoncaloDev/OrderFlow. Documentação em português.

## 2. Arquitetura
```
Blazor Web ──HTTP/JWT──▶ API ──▶ PostgreSQL
                          │
                          └─ publica eventos ──▶ RabbitMQ ──▶ Worker
```
Monorepo (ADR 0001): um único `docker compose up` deve subir o sistema inteiro, contratos compartilhados sem NuGet privado, um só pipeline de CI.

## 3. Estrutura da solução (`OrderFlow.sln`)
```
OrderFlow/
├── src/
│   ├── OrderFlow.Domain          entidades e enums (sem dependências)
│   ├── OrderFlow.Application     casos de uso, validações, DTOs (hoje só `AuthClaimTypes` e `PasswordPolicy`)
│   ├── OrderFlow.Infrastructure  EF Core: AppDbContext, Configurations, Migrations
│   ├── OrderFlow.Contracts       eventos/contratos compartilhados (vazio por enquanto)
│   ├── OrderFlow.Api             Web API (composition root)
│   ├── OrderFlow.Worker          Worker Service (template; ainda não consome nada)
│   └── OrderFlow.Web             Blazor (template: Home, Counter, Weather)
├── tests/
│   ├── OrderFlow.UnitTests       xUnit — só placeholder (UnitTest1)
│   └── OrderFlow.IntegrationTests xUnit — só placeholder; referencia a Api
├── docs/adr/0001-monorepo.md
├── docker-compose.yml            postgres:15 + rabbitmq:3-management
├── .env.example · .editorconfig · .gitattributes · .github/workflows/ci.yml
```
Referências entre projetos: Application → Domain · Infrastructure → Application · Api → Application, Infrastructure, Contracts · Worker → Infrastructure, Contracts · Web → nenhuma · UnitTests → Domain, Application · IntegrationTests → Api.

## 4. Modelo de dados (PostgreSQL, snake_case)
| Tabela | Campos principais | Regras |
|---|---|---|
| `customers` | id (Guid), owner_id → users, name (≤200), email (≤320), phone (≤30, opcional), created_at | Dono = usuário do token (ADR 0004); e-mail **único por dono** (índice owner_id + email) |
| `products` | id, name (≤200), description (≤1000, opcional), price (18,2), stock_quantity, created_at | — |
| `orders` | id, customer_id → customers, status (texto, ≤20), total_amount (18,2), created_at | FK `Restrict`: não apaga cliente com pedidos |
| `order_items` | id, order_id → orders, product_id → products, quantity, unit_price (18,2) | order `Cascade`; produto `Restrict`; `unit_price` é cópia do preço no momento da compra |
| `users` | id, email (≤320, **único**), password_hash (≤200), role (texto, ≤20), customer_id → customers (opcional), created_at | cliente apagado → `customer_id` = NULL (`SetNull`) |

Enums (guardados como texto): `OrderStatus` = Pending, Confirmed, Cancelled · `UserRole` = Customer, Admin.
Relações: Customer 1─N Order 1─N OrderItem N─1 Product · User N─0..1 Customer.
Migrations: `20260930234747_InitialCreate`, `20261008012314_AddOwnerAndPhoneToCustomers` (mais o `AppDbContextModelSnapshot`).
Observação: `Order.TotalAmount` é um campo armazenado — a regra de cálculo (soma de `Quantity × UnitPrice`) ainda não existe.

## 5. Configuração e ambientes
- `.env` (raiz; ver `.env.example`): `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `RABBITMQ_PORT`, `RABBITMQ_MANAGEMENT_PORT`. É lido pelo Docker Compose **e** pela API (DotNetEnv, `TraversePath`).
- Connection string: `ConnectionStrings:Default` se existir; senão é montada do `.env` com `Host=localhost`.
- `Database:AutoMigrate` (`appsettings.json` da API, padrão `true`): aplica migrations no startup.
- API em desenvolvimento: `http://localhost:5222` (http) / `https://localhost:7100` (https). OpenAPI exposto só em Development (`MapOpenApi`).

## 6. Estado atual (o que existe × o que falta)
**Pronto**
- Estrutura da solução e referências entre projetos.
- Entidades de domínio, `AppDbContext`, configurações Fluent API e migration inicial.
- Infra local via Docker (Postgres + RabbitMQ com healthchecks).
- CI de build/test; `.editorconfig`/`.gitattributes`; ADR do monorepo.
- Startup da API com carregamento de `.env`, DbContext e auto-migrate.

- Cadastro, login e `/api/auth/me` com JWT e hash de senha (tudo no `AuthController`, que usa o `AppDbContext`, o `IdentityPasswordHasher` e o `JwtTokenGenerator`; sem serviço, repositório ou interfaces). Ver ADR 0002, ADR 0003 e `docs/guia-autenticacao.md`.
- Front-end Blazor com login, cadastro, página de perfil protegida e layout (sem Counter/Weather).
- **Clientes** (mini ERP, ADR 0004): `CustomersController` (`/api/customers`: listar com busca por nome ou e-mail e paginação, obter, criar, editar, excluir; 404 para cliente de outro usuário, 409 para e-mail repetido do mesmo dono ou ao excluir cliente com pedidos) e telas `/clientes`, `/clientes/novo`, `/clientes/{id}/editar`. Ainda sem testes automatizados.

**Ainda não existe**
- Controllers/endpoints de produtos e pedidos; casos de uso na Application (os controllers falam direto com o `AppDbContext`, como no auth).
- Publicação de eventos e consumo no Worker; `Contracts` vazio; nenhum pacote de RabbitMQ referenciado.
- Dockerfiles e serviços `api`/`worker`/`web` no compose (estão comentados).
- Testes reais (apenas `UnitTest1` vazio) — a CI passa, mas sem cobertura.
- Testes do auth: os unitários do `AuthService` foram removidos junto com o serviço (ADR 0003); falta cobrir `/api/auth/*` com testes de integração.
- Seed de dados; paginação; tratamento global de erros (ProblemDetails).

## 7. Roadmap (do README)
- [ ] **v0.1** — API + banco + JWT (JWT, cadastro e login prontos; faltam os CRUDs de produtos, clientes e pedidos)
- [ ] **v0.2** — Mensageria (RabbitMQ + Worker)
- [ ] **v0.3** — Front-end Blazor

### Próximos passos naturais
1. **v0.1**: repositórios/serviços em Application; endpoints de produtos, clientes e pedidos; criação de pedido (valida estoque, copia `UnitPrice`, calcula `TotalAmount`, baixa estoque numa transação); registro/login com JWT; Swagger UI; ProblemDetails; testes unitários do domínio e de integração (Testcontainers + Postgres é uma boa opção).
2. **v0.2**: definir eventos em `Contracts` (ex.: `OrderCreated`, `OrderConfirmed`), publicar a partir da API (considerar Outbox para consistência com o banco) e consumir no Worker (idempotência, retry, dead-letter).
3. **v0.3**: Blazor consumindo a API (login, catálogo, carrinho/pedido, painel admin).
4. Containerizar `api`, `worker` e `web` e ativar os blocos do compose (lembrar de `ConnectionStrings__Default` apontando para `postgres`).

## 8. Histórico (git)
Initial commit → estrutura inicial de pastas → remoção dos `Class1.cs` de exemplo → entidades de domínio e DbContext → configurações EF Core e migration inicial → merge do PR #1 (`feat/database-entities-and-dbcontext`). Branch ativa: `main`.

## 9. Dívida técnica e pontos de atenção
- `Customer.cs` usa tabs misturados com espaços (contraria o `.editorconfig`) e declara propriedades `required` **com** valor padrão `string.Empty` (redundante; o `required` perde o efeito).
- `Order.TotalAmount` e `Product.StockQuantity` sem invariantes: entidades são anêmicas (setters públicos). Avaliar encapsular regras no domínio (ex.: `Order.AddItem`, `Order.Confirm/Cancel`, validação de transição de status).
- Sem índices em `orders.customer_id`/`status` e `order_items.order_id/product_id` além dos criados por convenção do EF para FKs — revisar conforme consultas reais.
- O `Program.cs` do Worker ainda é template. No Web, `Counter`/`Weather` foram removidos.
- Autenticação sem refresh token, sem bloqueio por tentativas e sem confirmação de e-mail (ver ADR 0002).
- Código de autenticação (e a simplificação do ADR 0003) escrito sem `dotnet build`/`dotnet test` no ambiente (SDK indisponível): rodar ambos antes de commitar.
- `.csproj` de Application/Domain/Infrastructure/Contracts e os `UnitTest1.cs` têm BOM UTF-8; `.editorconfig` define `charset = utf-8` (sem BOM). Padronizar.
- `StartupExtensions` está definido dentro de `Program.cs` da API; ao crescer, mover para arquivos próprios (`Extensions/`).
- Não consegui rodar `dotnet build`/`dotnet test` ao montar estes documentos (SDK indisponível no ambiente), então o estado de build é inferido da leitura do código.

## 10. Glossário
- **Customer**: cliente (pessoa/empresa que compra). **User**: credencial de acesso, pode estar ligada a um Customer. **Order**: pedido. **OrderItem**: linha do pedido, com preço congelado. **ADR**: Architecture Decision Record, em `docs/adr`.
