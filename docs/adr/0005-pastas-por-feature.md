# ADR 0005: Organização por feature na Api e no Web

**Status:** Aceita. Complementa o ADR 0001 (monorepo); não altera as camadas da solução.

## Contexto
Na Api, um recurso se espalhava por `Controllers/`, `Models/`, `Requests/` e `Extensions/`; no Web, por `Components/Pages/`, `Components/Shared/`, `Services/` e `Models/`. Para alterar produtos era preciso abrir de quatro a cinco pastas, e cada módulo novo (clientes, pedidos) tocaria todas elas.

## Decisão
- Em `OrderFlow.Api` e `OrderFlow.Web`, cada recurso tem uma pasta dentro de `Features/` (`Features/Auth/`, `Features/Products/`) com tudo dele: controller ou páginas, requests, responses, extensions, cliente HTTP e componentes específicos.
- O que serve a mais de uma feature fica em `Common/`, na raiz do projeto ao lado de `Features/`, para não se confundir com as features (Api: `PagedResponse`, `ClaimsPrincipalExtensions`; Web: `ApiClientBase`, `ApiResult`, `PagedResult`, `BrFormat`).
- Namespace igual à pasta (`OrderFlow.Api.Features.Products`, `OrderFlow.Web.Features.Auth`). O `AuthorizedApiClient` fica em `Features/Auth/` porque depende da sessão (`JwtAuthenticationStateProvider`); assim `Common/` não depende de nenhuma feature.
- No Web, `Components/` guarda só o que é da casca da aplicação: `App`, `Routes`, `Layout/`, páginas sem feature (`Home`, `Error`) e componentes genéricos em `Shared/` (`FormField`, `Pager`, `ConfirmDialog`...). O `_Imports.razor` foi para a raiz do projeto para valer nas pastas de feature.
- Domain, Application e Infrastructure continuam organizados por camada (a regra de dependência do `CLAUDE.md` não muda).

## Consequências
- Recurso novo = pasta nova, sem mexer nas existentes; o código de um recurso fica no mesmo lugar.
- Features podem depender de `Common/`; dependência entre features (ex.: `Features/Products/` usando `Features/Auth/`) deve ser rara e só em direção ao que é base (autenticação).
- Controllers continuam sendo descobertos por assembly, então nenhuma configuração muda. Os caminhos de rota (`@page`, `[Route]`) não mudam.
