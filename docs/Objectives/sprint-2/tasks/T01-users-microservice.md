# T01 — Users Microservice

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P0 (foundational — all other services depend on Player identity)

## Context

The **Player Profile** aggregate — covering registration, authentication, and profile management — must be extracted from the FCG monolith into a standalone **Users Microservice**. This service is the identity authority: it owns credentials and emits Events like `PlayerRegistered` and `PlayerLoggedIn`.

## User Story

> As a **Player**,  
> I want to register, login, and manage my profile through a dedicated service,  
> so that my identity is isolated from game and payment concerns.

## Gherkin Scenarios

```gherkin
Feature: Users Microservice — Player Identity Management

  Background:
    Given the Users Microservice is running and reachable via the API Gateway
    And it has its own isolated database

  Scenario: Player registers successfully
    Given a new Player provides a valid email "player@fcg.com" and password
    When POST /users/register is called with those credentials
    Then the Player account is created
    And a "PlayerRegistered" Event is appended to the Event Log
    And HTTP 201 is returned with the Player's ID

  Scenario: Duplicate email is rejected
    Given a Player with email "player@fcg.com" already exists
    When POST /users/register is called with the same email
    Then HTTP 409 is returned
    And no duplicate account is created
    And no Event is emitted

  Scenario: Player logs in and receives a JWT
    Given a registered Player with email "player@fcg.com"
    When POST /users/login is called with correct credentials
    Then HTTP 200 is returned
    And the response contains a signed JWT
    And the JWT expiry is 1 hour

  Scenario: Invalid credentials are rejected
    Given a registered Player
    When POST /users/login is called with a wrong password
    Then HTTP 401 is returned
    And no JWT is issued

  Scenario: Player updates their profile
    Given an authenticated Player with a valid JWT
    When PATCH /users/me is called with a new display name
    Then HTTP 200 is returned
    And the Player's profile is updated in the Users database
    And a "PlayerProfileUpdated" Event is appended to the Event Log

  Scenario: Users Microservice is independently deployable
    Given the Games and Payments Microservices are down
    When the Users Microservice is started
    Then registration and login work normally
    And no cross-service dependency error occurs
```

## Acceptance Criteria

- [ ] Separate repository `fcg-users` (or equivalent) with its own CI/CD pipeline
- [ ] Own database/schema — no shared tables with Games or Payments
- [ ] Endpoints: `POST /users/register`, `POST /users/login`, `GET /users/me`, `PATCH /users/me`
- [ ] Passwords stored as bcrypt hashes (cost factor ≥ 12) — never plaintext
- [ ] JWT signed with RS256 or HS256 — secret injected via environment variable
- [ ] JWT validation middleware reusable by Games and Payments (publish as shared lib or document public key)
- [ ] Emits `PlayerRegistered`, `PlayerLoggedIn`, `PlayerProfileUpdated` Events to Event Log
- [ ] Input validation on all endpoints (email format, password min-length 8)
- [ ] **Prototype Pollution protection**: `req.body` keys `__proto__`, `constructor`, `prototype` are rejected at the validation layer with HTTP 400
- [ ] **Content-Length limit**: payloads > 1MB are rejected with HTTP 413 (configure in framework body-parser or API Gateway)
- [ ] **HTTP Security Headers** on every response: `Strict-Transport-Security`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy: default-src 'none'`

## Best Practices

- Hash passwords with bcrypt, never MD5/SHA-1
- Store JWT secret/private key in cloud secret manager
- Rate-limit login endpoint to prevent brute-force attacks (e.g., 10 req/min per IP)
- Never log raw passwords or tokens — log only Player ID and sanitized metadata

## Dependencies

- T06 — API Gateway (to route traffic to this service)
- T07 — Event Sourcing infrastructure (to write Events)

## Definition of Done

- [ ] All Gherkin scenarios have passing automated tests
- [ ] CI/CD pipeline for `fcg-users` is green
- [ ] Service deployed and reachable via API Gateway
