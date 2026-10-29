# ADR 0006: Backend em um projeto só (por enquanto)

**Status:** Aceita. Substitui, no que toca à divisão em projetos, o ADR 0001 (monorepo) e as referências a Application, Contracts e Worker dos ADRs 0002 e 0003.

## Contexto
A solução nasceu dividida em `Domain`, `Application`, `Infrastructure`, `Contracts`, `Api` e `Worker`, antes de existir código para a maior parte dela: a `Application` tinha três arquivos (depois do ADR 0003), o `Contracts` estava vazio e o Worker era só o template. A divisão deixava o projeto difícil de entender (cada mudança passava por vários projetos e referências) sem entregar benefício hoje.

## Decisão
- O backend passa a ser **um projeto**, `OrderFlow.Api`, com pastas: `Features/` (Auth, Products; ADR 0005), `Domain/` (entidades e enums), `Infrastructure/` (`Persistence/` e `Migrations/`) e `Common/`. O `OrderFlow.Web` continua separado, por ser outro aplicativo.
- `Application` deixa de existir: `PasswordPolicy`, `EmailNormalizer` e `AuthClaimTypes` foram para `Features/Auth/`. O hasher de senha e o gerador de JWT, que estavam na Infrastructure, também foram para `Features/Auth/`, porque são parte da autenticação.
- `Contracts` e `Worker` foram removidos da solution. Voltam quando a mensageria for implementada (o histórico do git guarda os projetos originais).
- Os namespaces seguem as pastas: `OrderFlow.Api.Domain.Entities`, `OrderFlow.Api.Infrastructure.Persistence`, `OrderFlow.Api.Features.Auth`.
- As migrations existentes foram movidas sem alterar o conteúdo; só os namespaces mudaram (inclusive os nomes de entidade dentro do snapshot). Os IDs são os mesmos, então bancos já migrados continuam válidos, e `dotnet ef migrations has-pending-model-changes` não acusa mudanças.
- Os comandos do EF ficam sem `--startup-project`: `dotnet ef migrations add Nome --project src/OrderFlow.Api`.
- Para não deixar o EF Core em duas versões (os testes caíam na 9.0.1 e a Api na 9.0.22), `Microsoft.EntityFrameworkCore` e `Microsoft.EntityFrameworkCore.Relational` passaram a ser referenciados direto na Api. Isso também zerou os avisos MSB3277 que já existiam.

## Consequências
- Menos projetos e referências para entender; o `dotnet ef` e o Dockerfile da Api ficam mais simples.
- O compilador deixa de impedir que o `Domain` use EF Core ou ASP.NET. A regra passa a ser disciplina e está no `CLAUDE.md`.
- Quando o Worker for implementado: `Domain/` e `Infrastructure/` viram projetos de novo (`OrderFlow.Domain`, `OrderFlow.Infrastructure`), nasce o `OrderFlow.Contracts` e o Worker referencia os dois. Para isso ser barato, `Infrastructure/` deve conter só persistência (nada de HTTP ou JWT) e `Domain/` não deve depender de `Features/`.
- Os testes (`UnitTests` e `IntegrationTests`) referenciam a Api.
