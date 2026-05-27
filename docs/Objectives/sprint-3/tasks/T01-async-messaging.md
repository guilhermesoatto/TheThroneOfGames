# T01 — Async Messaging (Message Broker)

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P0 (unlocks resilient inter-service communication)

## Context

The FCG Microservices currently communicate synchronously. When load spikes, a slow Payments service blocks the Games service. Introducing a **Message Broker** (RabbitMQ, Apache Kafka, or AWS SQS) decouples them: a Microservice emits an **Event** to a **Queue**, and the consumer processes it when ready — surviving restarts and scale-outs without data loss.

## User Story

> As a **platform engineer**,  
> I want Microservices to communicate via a Message Broker for critical operations,  
> so that a slow or restarting service never causes data loss or cascading failures.

## Gherkin Scenarios

```gherkin
Feature: Async Messaging — Decoupled Microservice Communication

  Background:
    Given a Message Broker (RabbitMQ, Kafka, or SQS) is running
    And all three Microservices are connected as producers and/or consumers

  Scenario: TransactionCompleted Event survives Payments Microservice restart
    Given the Payments Microservice publishes "TransactionCompleted" to the Queue
    And the Payments Microservice restarts immediately after publishing
    When the notification consumer comes back online
    Then the Event is still in the Queue
    And the Player notification is sent (at-least-once delivery guaranteed)

  Scenario: Games Microservice reacts to TransactionCompleted
    Given a "TransactionCompleted" Event is in the Queue
    When the Games Microservice consumer reads the Event
    Then the Game is marked as owned by the Player in the Games database
    And a "GameAccessGranted" Event is appended to the Event Log

  Scenario: Consumer acknowledges messages only after successful processing
    Given the Games consumer reads a "TransactionCompleted" Event
    And the database write fails on first attempt
    When the consumer retries
    Then the Message Broker redelivers the Event
    And the Event is not lost
    And after successful processing the consumer sends an ACK

  Scenario: Failed messages go to the Dead-Letter Queue
    Given a consumer fails to process an Event 3 times in a row
    When the maximum retry count is exceeded
    Then the Event is moved to the Dead-Letter Queue (DLQ)
    And an alert is triggered
    And the Event is NOT discarded

  Scenario: Message Broker handles 1000 Events per second
    Given 500 concurrent Players complete Transactions simultaneously
    When 1000 "TransactionCompleted" Events are published to the Queue
    Then all 1000 Events are processed within 30 seconds
    And no Events are dropped
```

## Acceptance Criteria

- [ ] Message Broker deployed: RabbitMQ **or** Apache Kafka **or** AWS SQS
- [ ] Queues/topics defined: `transaction.completed`, `transaction.failed`, `game.purchase.initiated`
- [ ] All three Microservices implement producers and/or consumers as appropriate
- [ ] Consumer acknowledgement (manual ACK) — no auto-ACK
- [ ] Retry policy: at least 3 retries with exponential backoff before DLQ
- [ ] Dead-Letter Queue configured and monitored
- [ ] Message Broker connection string injected via Kubernetes Secret (see T06)
- [ ] Messages include `correlation_id` and `trace_id` for tracing (see T08 from Sprint 2)

## Best Practices

- Use **manual ACK**: acknowledge only after the message is fully processed and persisted
- Design consumers to be **idempotent** — processing the same message twice must not cause duplicate side effects
- Use **durable queues** and **persistent messages** so they survive broker restarts
- Separate exchange/topic per domain context — do not mix all Events in one giant queue

## Suggested Broker Options

| Broker | Notes |
|--------|-------|
| RabbitMQ | Easy to run locally and in K8s via Helm, AMQP protocol |
| Apache Kafka | Best for high-throughput event streaming, requires more setup |
| AWS SQS | Managed, zero ops, integrates natively with Lambda |

## Technical Notes

```typescript
// RabbitMQ producer example (amqplib)
const channel = await connection.createChannel();
await channel.assertQueue('transaction.completed', { durable: true });
channel.sendToQueue(
  'transaction.completed',
  Buffer.from(JSON.stringify({ txnId, playerId, gameId, correlationId, traceId })),
  { persistent: true }
);
```

```typescript
// Consumer with manual ACK
channel.consume('transaction.completed', async (msg) => {
  if (!msg) return;
  try {
    await grantGameAccess(JSON.parse(msg.content.toString()));
    channel.ack(msg);
  } catch (err) {
    channel.nack(msg, false, /* requeue */ retryCount < 3);
  }
});
```

## Dependencies

- T03 — Kubernetes Cluster (broker runs as a Pod or managed service)
- T06 — Secrets for broker credentials

## Definition of Done

- [ ] All Gherkin scenarios have passing automated tests (including DLQ scenario)
- [ ] DLQ alert fires when a message is dead-lettered (manual test)
- [ ] Broker dashboard accessible (RabbitMQ Management UI / Kafka UI / SQS console)
