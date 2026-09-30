# ADR 0001: Monorepo

**Status:** Aceita

## Contexto
API, Worker e Web fazem parte do mesmo sistema e compartilham contratos (eventos e DTOs).

## Decisão
Manter tudo em um único repositório.

## Consequências
- Um único `docker compose up` sobe o sistema inteiro.
- Contratos compartilhados sem necessidade de pacotes NuGet privados.
- Um só pipeline de CI e um só histórico.
