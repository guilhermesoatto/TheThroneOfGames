# T06 — API Gateway

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P0 (entry point for all Player requests)

## Context

The **API Gateway** is the single entry point that routes, authenticates, and protects all Player requests to the three Microservices. Without it, clients would need to know each Microservice's internal URL, making security and versioning unmanageable.

## User Story

> As a **Player**,  
> I want a single URL to access all FCG features (Users, Games, Payments),  
> so that I don't need to track multiple service addresses and my requests are always authenticated.

## Gherkin Scenarios

```gherkin
Feature: API Gateway — Routing and Security

  Background:
    Given the API Gateway is deployed (AWS API Gateway, Azure APIM, Kong, or similar)
    And all three Microservices are registered as backends

  Scenario: Player request is routed to the correct Microservice
    Given the API Gateway is running
    When GET /api/games is called
    Then the request is forwarded to the Games Microservice
    And the response from the Games Microservice is returned to the Player

  Scenario: Unauthenticated request to protected route is rejected
    Given a request without a JWT
    When GET /api/payments/transactions is called
    Then the API Gateway returns HTTP 401
    And the request does not reach the Payments Microservice

  Scenario: Valid JWT passes authentication at the Gateway
    Given a Player has a valid JWT from the Users Microservice
    When GET /api/games is called with the JWT in the Authorization header
    Then the API Gateway validates the JWT
    And the request is forwarded to the Games Microservice
    And the response is returned to the Player

  Scenario: Unknown route returns 404 from the Gateway
    Given the API Gateway is running
    When GET /api/nonexistent is called
    Then HTTP 404 is returned by the Gateway
    And no Microservice is invoked

  Scenario: Rate limiting protects Microservices from overload
    Given the rate limit is set to 100 requests per minute per IP
    When a client makes 101 requests in one minute
    Then the 101st request returns HTTP 429 (Too Many Requests)
    And the underlying Microservices are not overloaded

  Scenario: HTTPS is enforced
    Given a Player sends a request over plain HTTP
    When the API Gateway receives the request
    Then it redirects to HTTPS or rejects with HTTP 301/400
```

## Acceptance Criteria

- [ ] Single base URL routing to all three Microservices
- [ ] Route map: `/api/users/*` → Users MS, `/api/games/*` → Games MS, `/api/payments/*` → Payments MS
- [ ] JWT validation on all routes except `POST /api/users/register` and `POST /api/users/login`
- [ ] **JWT `alg:none` attack blocked**: Gateway MUST reject tokens where the `alg` header is `none` or any unexpected algorithm; only HS256 or RS256 accepted
- [ ] Rate limiting: minimum 100 req/min per IP, 1000 req/min per authenticated Player ID
- [ ] HTTPS enforced — TLS certificate configured; plain HTTP requests rejected (301 redirect or 400)
- [ ] Microservice internal URLs are not publicly exposed
- [ ] **CORS**: `Access-Control-Allow-Origin: *` is **forbidden** in production — configure origin whitelist via environment variable `ALLOWED_ORIGINS`
- [ ] **HTTP Security Headers** enforced at Gateway for all responses: `Strict-Transport-Security: max-age=31536000; includeSubDomains`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy: default-src 'none'`, `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] API Gateway configured via infrastructure-as-code (not via console GUI)

## Best Practices

- Keep the Gateway thin — it routes and secures, it does not contain business logic
- Centralise JWT validation at the Gateway; Microservices trust the forwarded `X-Player-Id` header
- Use API keys for service-to-service calls if direct internal routes exist
- Export OpenAPI spec from the Gateway for client SDK generation

## Suggested Options

| Option | Notes |
|--------|-------|
| AWS API Gateway + Lambda Authorizer | Native AWS, pay-per-call |
| Azure APIM | Managed, student subscription available |
| Kong (self-hosted) | Open-source, plugin ecosystem |
| Nginx | Simple reverse proxy if advanced features not needed |

## Technical Notes

```yaml
# AWS API Gateway route example (CDK or SAM)
Routes:
  GetGames:
    Method: GET
    Path: /api/games
    AuthorizationType: JWT
    Target: !Sub integrations/${GamesServiceIntegration}
  Register:
    Method: POST
    Path: /api/users/register
    AuthorizationType: NONE
    Target: !Sub integrations/${UsersServiceIntegration}
```

## Dependencies

- T01, T02, T03 — All three Microservices must be deployed before Gateway routing can be tested

## Definition of Done

- [x] All routes reachable through single Gateway URL (`/api/usuario/*`, `/api/game/*`, `/api/admin/*`, `/api/pedidos/*` — ver `api-gateway/nginx.conf`)
- [ ] JWT validation tested with valid and invalid tokens (validação do JWT acontece em cada microsserviço, não no Gateway em si — o nginx apenas roteia)
- [ ] Rate limiting verified with load test (configurado no nginx — `limit_req_zone` 20r/s, burst 40 — mas sem load test executado)
- [x] IaC code for Gateway in repository (`api-gateway/nginx.conf` + `docker-compose.yml`)
