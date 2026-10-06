# Deploy (Render + Neon)

A API e o front-end Blazor sobem no **Render** como serviços Docker; o banco é um Postgres no **Neon**.
A **Vercel não serve** para o `OrderFlow.Web`: ele é Blazor Server interativo e precisa de um processo
.NET sempre ligado, com conexão SignalR/WebSocket aberta por usuário, o que a Vercel não oferece.

## Imagens
Os Dockerfiles usam a **raiz do repositório** como contexto de build:

```
docker build -f src/OrderFlow.Api/Dockerfile -t orderflow-api .
docker build -f src/OrderFlow.Web/Dockerfile -t orderflow-web .
```

No Render: *Runtime* Docker, *Dockerfile Path* `./src/OrderFlow.Api/Dockerfile` (ou `Web`), *Docker Build Context Directory* `.`
(raiz). Os containers escutam na porta da variável `PORT` (8080 se ela não existir).

## Health check
API e Web respondem `200` em `GET /health` (só confirma que o processo está de pé; não consulta o banco,
para não acordar o Neon). No Render, *Health Check Path* = `/health`.

## Variáveis de ambiente

| Serviço | Variável | Valor |
|---|---|---|
| API | `ConnectionStrings__Default` | Connection string do Neon no formato **Npgsql** (ver abaixo) |
| API | `Jwt__Key` | Chave com no mínimo 32 caracteres (`openssl rand -base64 48`). Sem ela a API não sobe |
| Web | `Api__BaseUrl` | URL pública da API no Render (ex.: `https://orderflow-api.onrender.com`) |

Segredos ficam só no painel do Render; nunca em `appsettings*.json` nem no git.

### Connection string do Neon
O Neon entrega uma URL `postgresql://...`, que o Npgsql não aceita. Use:

```
Host=<host-do-neon>;Database=<banco>;Username=<usuario>;Password=<senha>;SSL Mode=Require
```

Use o host **direto** (sem `-pooler`): a API aplica as migrations ao iniciar (`Database:AutoMigrate`)
e isso não combina com o pooler (PgBouncer).

## Limitações conhecidas
- A sessão do Web (token no navegador, criptografado) cai a cada deploy ou reinício: as chaves de
  criptografia ficam no disco do container, que não persiste.
- Planos gratuitos costumam suspender o serviço (Render) e o banco (Neon) por inatividade; o primeiro
  acesso depois disso é lento.
- RabbitMQ e Worker ficam fora do deploy por enquanto (ainda não há publicação/consumo de eventos).
