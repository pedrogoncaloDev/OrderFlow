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
| `src/OrderFlow.Api` | ASP.NET Core Web API (JWT, Swagger, migrations na inicialização) |
| `src/OrderFlow.Application` | Casos de uso, validações, DTOs |
| `src/OrderFlow.Domain` | Entidades e regras de negócio |
| `src/OrderFlow.Infrastructure` | EF Core, migrations, mensageria |
| `src/OrderFlow.Contracts` | Eventos e contratos compartilhados |
| `src/OrderFlow.Worker` | Worker Service que consome eventos |
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

## Roadmap

- [ ] **v0.1**: API + banco + JWT
- [ ] **v0.2**: Mensageria (RabbitMQ + Worker)
- [ ] **v0.3**: Front-end Blazor

## Decisões técnicas

Registradas em [`docs/adr`](docs/adr).
