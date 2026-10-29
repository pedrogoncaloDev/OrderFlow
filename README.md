# OrderFlow

Sistema de pedidos com **API REST**, **mensageria** e **front-end Blazor**, feito em .NET.

> Projeto de portfólio: API com EF Core (migrations automáticas), autenticação JWT, worker consumindo eventos via RabbitMQ e interface web em Blazor.

## Arquitetura

```
Blazor Web  ──HTTP/JWT──▶  API  ──▶  PostgreSQL
                            │
                            └─ publica eventos ──▶ RabbitMQ ──▶ Worker
```

## Estrutura

| Pasta | Descrição |
|---|---|
| `src/OrderFlow.Api` | ASP.NET Core Web API: `Features/` (Auth, Products, Customers), `Domain/` (entidades), `Infrastructure/` (EF Core e migrations), `Common/` |
| `src/OrderFlow.Web` | Front-end Blazor |
| `tests/` | Testes unitários e de integração |
| `docs/` | Diagramas e ADRs (decisões de arquitetura) |

## Como rodar

Crie o `.env` a partir do exemplo e suba a infraestrutura (PostgreSQL 15 + RabbitMQ):

```bash
cp .env.example .env
```

```bash
docker compose up -d
```

- RabbitMQ Management: http://localhost:15672 (credenciais do `.env`)
- PostgreSQL: `localhost:5432` (credenciais do `.env`)

Depois, rode a API e o front-end (em dois terminais):

```bash
dotnet run --project src/OrderFlow.Api --launch-profile http   # http://localhost:5222
dotnet run --project src/OrderFlow.Web                          # http://localhost:5021
```

Cadastro, login e perfil já funcionam. Veja o [guia de estudo da autenticação](docs/guia-autenticacao.md).

## Roadmap

- [ ] **v0.1**: API + banco + JWT
- [ ] **v0.2**: Mensageria (RabbitMQ + Worker)
- [ ] **v0.3**: Front-end Blazor

## Decisões técnicas

Registradas em [`docs/adr`](docs/adr).
