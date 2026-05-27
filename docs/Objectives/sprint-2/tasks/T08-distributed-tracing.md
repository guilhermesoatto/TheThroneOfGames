# T08 — Distributed Tracing

**Sprint:** 2 | **Phase:** Fase 3 | **Priority:** P1 (observability across all Microservices)

## Context

With three Microservices and Serverless Functions, a single Player request can touch multiple systems. **Distributed Tracing** links all the spans of a request journey into a single **Trace**, making it possible to pinpoint latency, errors, and cascade failures across the FCG Platform.

## User Story

> As a **platform engineer**,  
> I want to trace a Player's request across all Microservices in one view,  
> so that I can identify which service caused a latency spike or error.

## Gherkin Scenarios

```gherkin
Feature: Distributed Tracing — Cross-Service Observability

  Background:
    Given OpenTelemetry (or equivalent) is configured on all three Microservices
    And a tracing backend is running (Jaeger, Zipkin, AWS X-Ray, or Azure Monitor)

  Scenario: Full purchase flow produces a linked Trace
    Given a Player initiates a Game purchase
    When the request flows through: API Gateway → Games MS → Payments MS → Serverless Function
    Then a single Trace is visible in the tracing UI
    And the Trace contains spans for each service
    And the total end-to-end duration is visible

  Scenario: Slow span is identifiable
    Given the Payments Microservice takes 3 seconds to process
    When the Trace for that request is inspected
    Then the Payments MS span is highlighted as the bottleneck
    And its duration is shown as 3 seconds within the total Trace

  Scenario: trace_id propagates through all services
    Given a request enters the API Gateway with no trace header
    When the API Gateway generates a trace_id
    Then the trace_id is propagated in the W3C TraceContext header to every downstream service
    And every service log line for that request includes the same trace_id

  Scenario: Error trace is linked to the failing span
    Given the Games Microservice throws an exception on a search
    When the Trace is viewed
    Then the failing span is marked with an error status
    And the error message and stack trace are attached to the span

  Scenario: Tracing overhead does not degrade performance
    Given 100% trace sampling is configured in development
    When a load test runs 200 requests per second
    Then p99 latency increases by no more than 10ms compared to no-tracing baseline
```

## Acceptance Criteria

- [ ] OpenTelemetry SDK (or vendor SDK) instrumented in all three Microservices
- [ ] `trace_id` and `span_id` propagated via W3C TraceContext (`traceparent` header) between services
- [ ] Same `trace_id` appears in both application logs and tracing backend
- [ ] Tracing backend deployed and accessible (Jaeger, AWS X-Ray, or Azure Monitor)
- [ ] All HTTP inbound/outbound calls automatically create spans (auto-instrumentation)
- [ ] Database query spans included in traces
- [ ] Sampling rate configurable via environment variable (`OTEL_TRACES_SAMPLER_ARG`)
- [ ] Trace data retained for at least 7 days

## Best Practices

- Use OpenTelemetry (vendor-neutral) over proprietary SDKs to avoid lock-in
- Log the `trace_id` on every log line so logs and traces correlate in one click
- Start with 100% sampling in dev/staging; use 10% head-based sampling in production
- Add custom attributes to spans for FCG domain context: `player.id`, `game.id`, `txn.id`

## Technical Notes

```typescript
// OpenTelemetry SDK setup (Node.js)
import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter({
    url: process.env.OTEL_EXPORTER_OTLP_ENDPOINT,
  }),
  instrumentations: [getNodeAutoInstrumentations()],
});
sdk.start();
```

```typescript
// Add domain attributes to a span
import { trace } from '@opentelemetry/api';

const span = trace.getActiveSpan();
span?.setAttribute('fcg.player_id', playerId);
span?.setAttribute('fcg.game_id', gameId);
```

## Dependencies

- T01, T02, T03 — All Microservices must be deployed
- T07 — Event Log writes should include `trace_id` from active span

## Definition of Done

- [ ] Full purchase trace visible end-to-end in the tracing UI (screenshot in video)
- [ ] Logs show `trace_id` on every line
- [ ] Tracing backend URL documented in README
