# T02 — Games Microservice (Game Catalog)

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P0 (core business domain)

## Context

The **Game Catalog** — listing, searching, and recommending Games — must be extracted into a standalone **Games Microservice**. This service is the authority on all Game data and integrates with Elasticsearch (T04) for fast, relevant search and recommendations for Players.

## User Story

> As a **Player**,  
> I want to browse, search, and get recommendations for Games,  
> so that I can quickly discover games that match my interests.

## Gherkin Scenarios

```gherkin
Feature: Games Microservice — Game Catalog

  Background:
    Given the Games Microservice is running and reachable via the API Gateway
    And it has its own isolated database and Elasticsearch index

  Scenario: Player lists available Games
    Given there are 50 Games in the Game Catalog
    When GET /games is called
    Then HTTP 200 is returned
    And the response contains a paginated list of Games
    And each Game includes: id, title, genre, price, and coverUrl

  Scenario: Player searches for a Game by title
    Given a Game titled "Dragon Quest" exists in the Game Catalog
    When GET /games?q=dragon is called
    Then HTTP 200 is returned
    And "Dragon Quest" appears in the results
    And results are returned in under 200ms

  Scenario: Game recommendations based on Player history
    Given a Player has purchased Games with genre "RPG"
    When GET /games/recommendations is called with the Player's JWT
    Then HTTP 200 is returned
    And the results are predominantly RPG Games
    And at least 5 recommendations are returned

  Scenario: Player purchases a Game
    Given an authenticated Player
    And Game "Dragon Quest" costs 29.99
    When POST /games/{id}/purchase is called
    Then HTTP 202 is returned (purchase handed off to Payments Microservice)
    And a "GamePurchaseInitiated" Event is appended to the Event Log

  Scenario: Games Microservice handles Elasticsearch being down
    Given the Elasticsearch cluster is unavailable
    When GET /games?q=dragon is called
    Then the service falls back to database search
    And HTTP 200 is returned (degraded but functional)
    And a warning Health Metric is recorded

  Scenario: New Game is added to the Catalog
    Given an admin user with valid credentials
    When POST /games is called with valid Game data
    Then HTTP 201 is returned
    And the Game is stored in the database
    And the Game is indexed in Elasticsearch (async)
    And a "GamePublished" Event is appended to the Event Log
```

## Acceptance Criteria

- [ ] Separate repository `fcg-games` with its own CI/CD pipeline
- [ ] Own database/schema — no shared tables
- [ ] Endpoints: `GET /games`, `GET /games/:id`, `GET /games/recommendations`, `POST /games/:id/purchase`, `POST /games` (admin)
- [ ] Elasticsearch integration for full-text search and aggregation (see T04)
- [ ] Pagination implemented on `GET /games` (default page size: 20)
- [ ] Emits `GamePublished`, `GamePurchaseInitiated` Events to Event Log
- [ ] Fallback to DB search when Elasticsearch is unavailable (circuit breaker)
- [ ] Authorization: purchase requires valid JWT; admin routes require admin role

## Best Practices

- Index Games in Elasticsearch asynchronously (do not block the HTTP response)
- Use cursor-based pagination for large catalogs (not offset-based)
- Validate all incoming Game data at the API boundary (title max 200 chars, price >= 0)
- Document API with OpenAPI/Swagger spec in the repository

## Dependencies

- T01 — JWT format needed for Player-authenticated endpoints
- T04 — Elasticsearch must be provisioned
- T06 — API Gateway routing
- T07 — Event Sourcing infrastructure

## Definition of Done

- [ ] All Gherkin scenarios have passing automated tests (sem endpoint `?q=`/recomendações via HTTP; compra é responsabilidade de `GameStore.Vendas`, não deste serviço)
- [ ] Search returns results in < 200ms (load test evidence) — busca via Elasticsearch implementada e testada em `GameStore.Catalogo.Tests`, mas não exposta em nenhum endpoint HTTP do `GameController` nem load-testada
- [x] Service deployed and reachable via API Gateway (`/api/game/*` via `api-gateway/nginx.conf`)
