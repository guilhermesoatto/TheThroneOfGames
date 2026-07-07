# T05 — Serverless Functions (Async Processing)

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P1 (depends on T03 for payment trigger)

## Context

Asynchronous, stateless operations — such as sending purchase confirmation notifications and processing payment callbacks — must not block the HTTP request cycle. **Serverless Functions** handle these by reacting to **Events** emitted by the Microservices, keeping the core services lean and responsive.

## User Story

> As a **Player**,  
> I want to receive a notification when my Transaction completes,  
> so that I know immediately when I can start playing my newly purchased Game.

## Gherkin Scenarios

```gherkin
Feature: Serverless Functions — Async Event Processing

  Background:
    Given the Serverless Functions are deployed (AWS Lambda or Azure Functions)
    And they are connected to the Event source (Event Log, SQS, or API Gateway webhook)

  Scenario: Player receives notification on successful Transaction
    Given a Transaction transitions to "COMPLETED" state
    When the Payments Microservice emits a "TransactionCompleted" Event
    Then the "NotifyPlayer" Serverless Function is triggered within 10 seconds
    And the Player receives an email or push notification with the purchase details
    And the Function execution is logged with a correlation_id

  Scenario: Notification is not sent on failed Transaction
    Given a Transaction transitions to "FAILED" state
    When the Payments Microservice emits a "TransactionFailed" Event
    Then the "NotifyPlayer" Serverless Function is triggered
    And the Player receives a failure notification with a retry suggestion
    And no Game access is granted

  Scenario: Serverless Function is retried on failure
    Given the "NotifyPlayer" Function throws an exception
    When the execution fails
    Then the platform retries the Function at least 2 more times
    And if all retries fail, the Event is sent to a dead-letter queue

  Scenario: Payment callback is processed asynchronously
    Given a payment provider sends an HTTP callback for Transaction "txn-abc"
    When the callback triggers the "ProcessPaymentCallback" Function
    Then the Transaction state is updated in the Payments Microservice
    And a "TransactionCompleted" or "TransactionFailed" Event is emitted
    And the Function returns HTTP 200 to the provider within 3 seconds

  Scenario: Function scales to zero when idle
    Given no Events are firing for 5 minutes
    When the cloud checks the Function's invocation count
    Then the Function instance count is 0 (scaled to zero)
    And no compute cost is incurred during this idle period
```

## Acceptance Criteria

- [ ] At least two Serverless Functions deployed: `NotifyPlayer` and `ProcessPaymentCallback`
- [ ] Functions triggered by Events (via SQS, SNS, EventBridge, or HTTP trigger)
- [ ] Each Function execution logs: `function_name`, `correlation_id`, `trace_id`, `event_type`, outcome
- [ ] Retry policy configured: minimum 3 attempts before dead-letter
- [ ] Dead-letter queue configured for failed executions
- [ ] Functions deployed via Serverless Framework, AWS SAM, Terraform, or cloud CLI (not via console GUI)
- [ ] Cold start p95 latency < 2 seconds (use provisioned concurrency if needed)

## Best Practices

- Keep Functions small and single-purpose (one Function = one responsibility)
- Never store state inside the Function — use the database or cache for state
- Inject all configuration (notification service API key, DB connection) via environment variables
- Use `correlation_id` passed from the triggering Event so distributed traces connect
- Test Functions locally with the Serverless Framework `invoke local` before deploying

## Technical Notes

```typescript
// AWS Lambda: NotifyPlayer handler example
export const handler = async (event: SQSEvent): Promise<void> => {
  for (const record of event.Records) {
    const domainEvent = JSON.parse(record.body);
    const { playerId, gameTitle, correlationId } = domainEvent;

    logger.info({ correlationId, playerId, event: 'NotifyPlayer.Start' });

    await notificationService.send({
      to: playerId,
      subject: `Purchase confirmed: ${gameTitle}`,
      body: `Your purchase of ${gameTitle} is complete. Enjoy!`
    });

    logger.info({ correlationId, playerId, event: 'NotifyPlayer.Done' });
  }
};
```

## Dependencies

- T03 — Payments Microservice emits `TransactionCompleted` / `TransactionFailed` Events
- T07 — Event Log or queue must be in place to trigger Functions

## Definition of Done

- [ ] Both Functions deployed and triggered by Events in staging environment — **triggers reais implementados** (`[RabbitMQTrigger]` do `Microsoft.Azure.Functions.Worker.Extensions.RabbitMQ` escutando `PedidoFinalizadoEvent` via filas fan-out dedicadas em `GameStore.Notifications.Functions`, build verificado), mas sem deploy em nenhum ambiente de staging/cloud
- [ ] Retry policy and dead-letter queue verified with a forced failure test (não testado)
- [ ] Execution logs show `correlation_id` on every invocation (logs incluem `PedidoId`/`UserId`, não um `correlation_id` dedicado)
