# OrderFlow

Mini ERP de pedidos em **.NET 9**: **API REST** com autenticação JWT, **front-end Blazor** e banco PostgreSQL. A mensageria (RabbitMQ + Worker) está planejada para a próxima etapa.

> Projeto de portfólio. Cada usuário gerencia o próprio negócio: cadastra seus clientes e produtos e (em breve) cria pedidos. Os dados são isolados por dono ([ADR 0004](docs/adr/0004-dados-por-dono.md)).

## O que já funciona

- **Autenticação**: cadastro, login e perfil com JWT e hash de senha ([guia de estudo](docs/guia-autenticacao.md)).
- **Produtos**: listar (com busca e paginação), criar, editar e excluir; produto já vendido não pode ser excluído.
- **Clientes**: listar (com busca por nome ou e-mail e paginação), criar, editar e excluir; e-mail único por dono.
- **Front-end Blazor** com telas de login, cadastro, perfil, produtos e clientes.
- **Banco**: PostgreSQL 15 via EF Core 9, nomes em snake_case, migrations aplicadas ao iniciar a API.
- **Deploy**: Dockerfiles da API e do Web, `render.yaml` e instruções em [docs/deploy.md](docs/deploy.md).

Ainda não existe: pedidos, mensageria e testes automatizados reais (hoje há só placeholders; a CI compila e roda o que existe).

## Arquitetura

```
Blazor Web  ──HTTP/JWT──▶  API  ──▶  PostgreSQL
```

O backend é **um projeto só**, `OrderFlow.Api`, organizado por feature ([ADR 0005](docs/adr/0005-pastas-por-feature.md) e [ADR 0006](docs/adr/0006-backend-em-projeto-unico.md)). Quando o Worker for implementado, `Domain/` e `Infrastructure/` voltam a ser projetos.

## Estrutura

| Pasta | Descrição |
|---|---|
| `src/OrderFlow.Api` | Web API (ASP.NET Core, Controllers + OpenAPI) |
| ├ `Features/` | Uma pasta por recurso: `Auth/`, `Products/`, `Customers/` (controller, requests, responses) |
| ├ `Domain/` | Entidades e enums |
| ├ `Infrastructure/` | `AppDbContext`, configurações do EF Core e migrations |
| └ `Common/` | Código usado por mais de uma feature |
| `src/OrderFlow.Web` | Front-end Blazor (Interactive Server), também organizado em `Features/` |
| `tests/` | Projetos de testes unitários e de integração (placeholders por enquanto) |
| `docs/` | Guia de autenticação, deploy e ADRs (decisões de arquitetura) |

## Como rodar

Pré-requisitos: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) e um PostgreSQL 15 (via Docker ou instalado na máquina).

**1. Configuração.** Crie o `.env` a partir do exemplo e ajuste as senhas (ele não vai para o git):

```bash
cp .env.example .env
```

A API usa `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` e `POSTGRES_PORT`. O `Jwt__Key` pode ficar vazio em desenvolvimento: a API gera uma chave temporária (as sessões caem ao reiniciar).

**2. Banco de dados.** Escolha uma opção:

- **Docker** (sobe também o RabbitMQ, ainda sem uso): `docker compose up -d`
- **PostgreSQL local**: crie o usuário e o banco com os mesmos valores do `.env`:

  ```sql
  CREATE USER orderflow WITH PASSWORD 'sua-senha';
  CREATE DATABASE orderflow OWNER orderflow;
  ```

**3. API e front-end** (em dois terminais, na raiz do repositório):

```bash
dotnet run --project src/OrderFlow.Api --launch-profile http   # http://localhost:5222
dotnet run --project src/OrderFlow.Web --launch-profile http   # http://localhost:5021
```

Ao iniciar, a API aplica as migrations. Abra `http://localhost:5021` e crie uma conta em *Cadastro*. A documentação OpenAPI da API fica disponível só em Development.

## Build e testes

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## Migrations

```bash
dotnet ef migrations add NomeDaMigration --project src/OrderFlow.Api
dotnet ef database update --project src/OrderFlow.Api
```

Requer `dotnet tool install --global dotnet-ef`.

## Roadmap

- [x] **v0.1 (parcial)**: API, banco, JWT, produtos e clientes
- [ ] **v0.1 (restante)**: pedidos (valida estoque, copia o preço, calcula o total e baixa o estoque numa transação) e testes de integração
- [ ] **v0.2**: mensageria (RabbitMQ + Worker)
- [x] **v0.3 (parcial)**: front-end Blazor com login, produtos e clientes; faltam as telas de pedidos

## Decisões técnicas

Registradas em [`docs/adr`](docs/adr). Para o estado detalhado do projeto, veja o [CONTEXT.md](CONTEXT.md).
