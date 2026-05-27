# T07 — Event Sourcing (Event Log)

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P1 (cross-cutting — all Microservices write to it)

## Context

Every state change in the FCG domain must be captured as an immutable **Event** in the **Event Log**. This provides a complete, auditable history: how a Player registered, which Games were purchased, and how each Transaction progressed. The Event Log is the ground truth from which any Microservice state can be reconstructed.

## User Story

> As a **platform auditor**,  
> I want every FCG domain action to be recorded as an immutable Event,  
> so that we can reconstruct system state, audit Transactions, and diagnose bugs using history.

## Gherkin Scenarios

```gherkin
Feature: Event Sourcing — Immutable Event Log

  Background:
    Given the Event Log database table (or stream) is provisioned
    And all three Microservices are configured to write Events to it

  Scenario: PlayerRegistered Event is persisted
    Given a new Player registers via the Users Microservice
    When the registration succeeds
    Then a "PlayerRegistered" Event is appended to the Event Log
    And the Event contains: event_type, player_id, email (hashed), correlation_id, timestamp
    And the Event cannot be modified or deleted

  Scenario: GamePurchased Event is persisted with correct sequence
    Given Player "p-001" purchases Game "Dragon Quest"
    When the Transaction completes
    Then a "GamePurchased" Event is appended after the "TransactionCompleted" Event
    And the Event Log query for Player "p-001" returns all Events in causal order

  Scenario: Event Log is append-only
    Given an Event "TransactionCompleted" with ID "evt-123" exists
    When any process attempts to UPDATE or DELETE "evt-123"
    Then the operation is rejected by the database constraint
    And the original Event remains unchanged

  Scenario: Microservice state can be rebuilt from Event Log
    Given the Payments Microservice database is empty
    When the Event Replay job runs against the full Event Log
    Then all Transactions are reconstructed in their correct final states
    And the rebuilt state matches the original database snapshot

  Scenario: Event has a correlation_id linking it to its originating request
    Given a Player makes a purchase that generates 3 Events
    When those Events are queried by correlation_id "corr-xyz"
    Then all 3 Events are returned
    And they share the same correlation_id and trace_id
```

## Acceptance Criteria

- [ ] `events` table/stream with columns: `id (UUID)`, `event_type`, `aggregate_id`, `aggregate_type`, `payload (JSON)`, `correlation_id`, `trace_id`, `occurred_at (timestamp, immutable)`, `schema_version`
- [ ] Database constraint prevents UPDATE and DELETE on the `events` table
- [ ] All three Microservices write Events synchronously in the same transaction as their state change (Outbox Pattern recommended)
- [ ] `correlation_id` and `trace_id` are required fields — Events without them are rejected
- [ ] Event schema versioned (`schema_version` field) to support future migrations
- [ ] Event Log exposed as a read-only query API for audit use (`GET /events?aggregate_id=&type=`)

## Best Practices

- Use the **Transactional Outbox Pattern** to guarantee Events are published even if the message broker is down: write Event to an `outbox` table in the same DB transaction, then a background worker publishes it
- Never put PII (email, full name) in the Event payload — use IDs and hashed values
- Add an index on `(aggregate_id, occurred_at)` for efficient replay queries
- Define Event types as constants in a shared contract (not free-text strings)

## Technical Notes

```sql
-- Event Log table definition
CREATE TABLE events (
  id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
  event_type      VARCHAR(100) NOT NULL,
  aggregate_id    UUID         NOT NULL,
  aggregate_type  VARCHAR(100) NOT NULL,
  payload         JSONB        NOT NULL,
  correlation_id  UUID         NOT NULL,
  trace_id        VARCHAR(64)  NOT NULL,
  schema_version  INT          NOT NULL DEFAULT 1,
  occurred_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Prevent UPDATE/DELETE
CREATE RULE no_update_events AS ON UPDATE TO events DO INSTEAD NOTHING;
CREATE RULE no_delete_events AS ON DELETE TO events DO INSTEAD NOTHING;
```

## Event Catalog

| Event Type | Emitted By | Payload Fields |
|-----------|-----------|---------------|
| `PlayerRegistered` | Users MS | player_id, email_hash |
| `PlayerLoggedIn` | Users MS | player_id |
| `GamePublished` | Games MS | game_id, title, genre |
| `GamePurchaseInitiated` | Games MS | game_id, player_id, price |
| `TransactionInitiated` | Payments MS | txn_id, player_id, amount |
| `TransactionCompleted` | Payments MS | txn_id, player_id, game_id |
| `TransactionFailed` | Payments MS | txn_id, reason |

## Dependencies

- Must be provisioned before T01, T02, T03 can write Events

## Definition of Done

- [ ] Event Log table created with constraints
- [ ] All Gherkin scenarios have passing automated tests
- [ ] Outbox worker running and publishing Events reliably
- [ ] Event Catalog documented in repository
