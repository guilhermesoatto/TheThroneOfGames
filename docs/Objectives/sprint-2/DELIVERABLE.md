# Sprint 2 — Deliverable Artifact
> Phase 3 | FCG — FIAP Cloud Games

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **Microservice** | An independently deployable service owning a single bounded context (Users, Games, or Payments) |
| **Game Catalog** | The indexed collection of all FCG games searchable by Players |
| **Player Profile** | The aggregate containing a Player's identity, preferences, and game history |
| **Transaction** | A Payment attempt made by a Player to acquire a Game |
| **Serverless Function** | A stateless compute unit invoked by an Event or HTTP trigger |
| **Event** | An immutable fact that something happened in the FCG domain (e.g., `PlayerRegistered`, `GamePurchased`) |
| **API Gateway** | The single entry point routing Player requests to the correct Microservice |
| **Event Log** | The append-only, ordered store of all domain Events (Event Sourcing) |
| **Trace** | The distributed record of a request's journey across all Microservices |

## Sprint Goal

> Decompose the FCG Platform into **three Microservices** (Users, Games, Payments), enable fast **Game Catalog search** via Elasticsearch, offload async work to **Serverless Functions**, protect all services behind an **API Gateway**, and make every state change auditable via an **Event Log**.

## Deliverables Checklist

- [x] `T01` — Users Microservice extracted and deployed
- [x] `T02` — Games Microservice with Game Catalog extracted and deployed
- [x] `T03` — Payments Microservice extracted and deployed
- [x] `T04` — Elasticsearch indexing Games (busca ainda não exposta via endpoint HTTP — ver nota abaixo)
- [x] `T05` — Serverless Functions for async operations (notifications, payment processing)
- [x] `T06` — API Gateway routing and securing all Microservices
- [x] `T07` — Event Sourcing — Event Log capturing all domain state changes (sem replay/projeções)
- [ ] `T08` — Distributed Tracing across all Microservices

> **Nota de status real (2026-07-07):** T01–T07 têm implementação funcional real e testada nesta branch
> (ver `docs/ai/tasks/prd-sprint-02-fase3.json` para o detalhamento tarefa a tarefa). T04 e T05 usam uma
> stack diferente da sugerida nos Gherkins abaixo (.NET/Elasticsearch/RabbitMQ + Azure Functions isolated
> worker, não AWS Lambda/SQS) — os cenários Gherkin e critérios de aceite detalhados em `tasks/*.md` foram
> escritos como um template genérico e **não foram atualizados item a item** para refletir essa stack; ver
> o corpo do PRD e o chat para o gap analysis real. T08 não teve nenhum trabalho realizado (sem collector,
> sem chamada síncrona entre serviços para propagar trace).

## Architectural Constraints (Fase 3)

- Each Microservice lives in its **own repository** with its own CI/CD pipeline
- Microservices do **not** share databases
- All inter-service communication at this phase may still be synchronous (HTTP/REST) — async messaging comes in Sprint 3
- Event Sourcing is append-only — no UPDATE or DELETE on the Event Log table

## Acceptance Criteria (Sprint Gate)

```gherkin
Feature: Sprint 2 Release Gate

  Scenario: All three Microservices are independently deployable
    Given the three Microservice repositories exist
    When each Microservice is deployed independently
    Then each one is accessible via the API Gateway
    And no Microservice directly calls another's database

  Scenario: Game Catalog search returns relevant results
    Given Games are indexed in Elasticsearch
    When a Player searches for "RPG" in the Game Catalog
    Then results are returned in under 200ms
    And the response includes aggregated metrics (most popular games)

  Scenario: Payment triggers a Serverless Function
    Given a Player initiates a Transaction
    When the Payments Microservice emits a PaymentProcessed event
    Then a Serverless Function fires and sends a notification to the Player

  Scenario: All Events are persisted in the Event Log
    Given a Player registers and buys a Game
    When the Event Log is queried
    Then it contains "PlayerRegistered" and "GamePurchased" events in order
    And neither event has been modified or deleted
```

## Linked Tasks

| ID | Title | File |
|----|-------|------|
| T01 | Users Microservice | [tasks/T01-users-microservice.md](tasks/T01-users-microservice.md) |
| T02 | Games Microservice | [tasks/T02-games-microservice.md](tasks/T02-games-microservice.md) |
| T03 | Payments Microservice | [tasks/T03-payments-microservice.md](tasks/T03-payments-microservice.md) |
| T04 | Elasticsearch Indexing | [tasks/T04-elasticsearch-indexing.md](tasks/T04-elasticsearch-indexing.md) |
| T05 | Serverless Functions | [tasks/T05-serverless-functions.md](tasks/T05-serverless-functions.md) |
| T06 | API Gateway | [tasks/T06-api-gateway.md](tasks/T06-api-gateway.md) |
| T07 | Event Sourcing | [tasks/T07-event-sourcing.md](tasks/T07-event-sourcing.md) |
| T08 | Distributed Tracing | [tasks/T08-distributed-tracing.md](tasks/T08-distributed-tracing.md) |
