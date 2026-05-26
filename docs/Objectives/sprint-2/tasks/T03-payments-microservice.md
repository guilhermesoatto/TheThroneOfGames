# T03 — Payments Microservice

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P0 (required for Game purchase flow)

## Context

All financial operations — processing **Transactions**, checking payment status, and issuing refunds — belong to the **Payments Microservice**. This service is the financial authority and must be isolated from Games and Users to ensure that a payment outage never takes down the entire FCG Platform.

## User Story

> As a **Player**,  
> I want to pay for Games through a secure and reliable payment service,  
> so that my Transactions are processed safely and I receive confirmation quickly.

## Gherkin Scenarios

```gherkin
Feature: Payments Microservice — Transaction Processing

  Background:
    Given the Payments Microservice is running and reachable via the API Gateway
    And it has its own isolated database

  Scenario: Player initiates a Transaction
    Given an authenticated Player wants to buy Game "Dragon Quest" for 29.99
    When POST /payments/transactions is called with the Game ID and payment details
    Then HTTP 202 is returned (Transaction is queued for processing)
    And a Transaction ID is returned
    And a "TransactionInitiated" Event is appended to the Event Log

  Scenario: Successful payment completes a Transaction
    Given a Transaction is in "PENDING" state
    When the payment provider confirms the charge
    Then the Transaction state transitions to "COMPLETED"
    And a "TransactionCompleted" Event is appended to the Event Log
    And a notification is triggered (via Serverless Function in T05)

  Scenario: Payment failure transitions Transaction to FAILED
    Given a Transaction is in "PENDING" state
    And the payment provider declines the charge
    When the provider sends a failure callback
    Then the Transaction state transitions to "FAILED"
    And a "TransactionFailed" Event is appended to the Event Log
    And no Game access is granted to the Player

  Scenario: Player checks Transaction status
    Given a Transaction was initiated with ID "txn-abc-123"
    When GET /payments/transactions/txn-abc-123 is called with the Player's JWT
    Then HTTP 200 is returned
    And the response includes the current Transaction state

  Scenario: Player cannot view another Player's Transaction
    Given Player A has Transaction "txn-abc-123"
    And Player B is authenticated
    When GET /payments/transactions/txn-abc-123 is called with Player B's JWT
    Then HTTP 403 is returned

  Scenario: Payments Microservice is independently deployable
    Given the Users and Games Microservices are down
    When the Payments Microservice is started
    Then it can process previously queued Transactions
    And no cross-service dependency error causes a crash
```

## Acceptance Criteria

- [ ] Separate repository `fcg-payments` with its own CI/CD pipeline
- [ ] Own database/schema — financial records isolated from other services
- [ ] Endpoints: `POST /payments/transactions`, `GET /payments/transactions/:id`
- [ ] Transaction states: `PENDING` → `COMPLETED` | `FAILED` | `REFUNDED`
- [ ] Emits `TransactionInitiated`, `TransactionCompleted`, `TransactionFailed` Events to Event Log
- [ ] Authorization: Player can only access their own Transactions
- [ ] Payment provider credentials injected via environment variables — never hardcoded
- [ ] Idempotency key supported to prevent duplicate charges (header: `Idempotency-Key`)

## Best Practices

- Never log full card numbers or CVV — log only masked last-4 digits and transaction ID
- Use idempotency keys on payment provider calls to survive retries safely
- Separate payment provider integration behind an interface (swap providers without changing domain logic)
- Store Transaction history immutably — no UPDATE/DELETE (aligns with Event Sourcing)

## Dependencies

- T01 — JWT for Player authentication
- T05 — Serverless Function triggered on `TransactionCompleted`
- T06 — API Gateway routing
- T07 — Event Sourcing infrastructure

## Definition of Done

- [ ] All Gherkin scenarios have passing automated tests
- [ ] No payment credentials appear in logs, images, or code
- [ ] Service deployed and reachable via API Gateway
