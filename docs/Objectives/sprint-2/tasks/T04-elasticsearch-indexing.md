# T04 — Elasticsearch Indexing (Game Catalog Search)

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P1 (enhances T02)

## Context

The **Game Catalog** must support fast, relevant, full-text search so Players can discover games quickly. Elasticsearch provides the indexing, query, and aggregation capabilities needed. Games are indexed from the Games Microservice database and kept in sync via Events.

## User Story

> As a **Player**,  
> I want to search for Games by title, genre, or description and see popularity metrics,  
> so that I can discover relevant games in milliseconds.

## Gherkin Scenarios

```gherkin
Feature: Elasticsearch — Game Catalog Indexing and Search

  Background:
    Given an Elasticsearch cluster is running
    And a "games" index exists with the correct mapping

  Scenario: Game is indexed on publish
    Given an admin publishes a new Game "Elden Ring" with genre "RPG"
    When the "GamePublished" Event is processed
    Then the Game is indexed in Elasticsearch within 5 seconds
    And GET /games?q=elden returns "Elden Ring" in results

  Scenario: Full-text search returns relevant results
    Given 100 Games are indexed in Elasticsearch
    When GET /games?q=dragon is called
    Then all Games with "dragon" in title or description are returned
    And results are ordered by relevance score (highest first)
    And response time is under 200ms

  Scenario: Genre filter narrows results
    Given Games of genres "RPG", "FPS", and "Strategy" are indexed
    When GET /games?genre=RPG is called
    Then only RPG Games are returned
    And non-RPG Games do not appear in results

  Scenario: Aggregation returns most popular Games
    Given 100 Games have purchase counts indexed
    When GET /games/metrics/popular is called
    Then HTTP 200 is returned
    And the response contains a ranked list of the top 10 most purchased Games
    And each entry includes the Game title and purchase count

  Scenario: Game recommendation query uses Player history
    Given a Player has purchased games with genres "RPG" and "Adventure"
    When GET /games/recommendations is called
    Then Elasticsearch returns Games scored by genre similarity
    And Games already purchased by the Player are excluded

  Scenario: Elasticsearch is re-indexed from scratch without downtime
    Given the "games" index needs to be rebuilt
    When a re-index job runs against the current database
    Then all Games are indexed in the new index
    And an index alias swap makes the new index live atomically
    And no search downtime occurs during the swap
```

## Acceptance Criteria

- [ ] Elasticsearch cluster provisioned (managed service: AWS OpenSearch, Elastic Cloud, or self-hosted)
- [ ] `games` index mapping defined with: `title` (text, analyzed), `description` (text), `genre` (keyword), `tags` (keyword array), `purchaseCount` (integer)
- [ ] Indexing triggered asynchronously on `GamePublished` Event
- [ ] Search endpoint supports: `?q=` (full-text), `?genre=` (filter), pagination
- [ ] Aggregation endpoint for top-10 most popular Games
- [ ] Recommendation query uses `more_like_this` or scored genre matching
- [ ] Zero-downtime re-index via alias swap (blue-green index pattern)

## Best Practices

- Use index aliases — never point application code directly at a concrete index name
- Set `refresh_interval` to `1s` (default) for near-real-time indexing; increase during bulk loads
- Do not store sensitive Player data in the Games index
- Test queries in Kibana Dev Tools before coding them in the application

## Technical Notes

```json
// Example Games index mapping
{
  "mappings": {
    "properties": {
      "id":            { "type": "keyword" },
      "title":         { "type": "text", "analyzer": "standard" },
      "description":   { "type": "text" },
      "genre":         { "type": "keyword" },
      "tags":          { "type": "keyword" },
      "price":         { "type": "float" },
      "purchaseCount": { "type": "integer" },
      "publishedAt":   { "type": "date" }
    }
  }
}
```

## Dependencies

- T02 — Games Microservice emits `GamePublished` Events that trigger indexing

## Definition of Done

- [ ] All Gherkin scenarios have passing automated tests — **indexação real implementada e testada** (`ElasticsearchJogoIndexerTests`, 3/3 passando com Testcontainers.Elasticsearch real, ver `GameStore.Catalogo/Infrastructure/Search/`), mas nenhum endpoint HTTP expõe busca/agregação/recomendação ao cliente
- [ ] Search latency p95 < 200ms under 50 concurrent requests (sem load test)
- [ ] Index mapping documented in repository (mapping inferido automaticamente do POCO `JogoSearchDocument`, sem mapping explícito documentado)
