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

- [x] OpenTelemetry SDK (or vendor SDK) instrumented in all three Microservices — já existia (AspNetCore+HttpClient), agora também `GameStore.Notifications.Functions`
- [x] `trace_id` e `span_id` propagados entre serviços — via W3C traceparent embutido no **corpo JSON** da mensagem (não em headers AMQP — o binding `[RabbitMQTrigger]` do Azure Functions Worker isolated só expõe o body como string, sem acesso a headers/properties; ver `GameStore.Common.Tracing.TraceContextPropagator`). Verificado com teste real contra RabbitMQ (Testcontainers): `GameStore.Common.Tests/TraceContextPropagationTests.cs`
- [x] Mesmo `trace_id` aparece em logs e no backend de tracing — verificado ao vivo: `TraceId":"94ce12a7ebe896e2822a3ffb8446c063"` idêntico nos logs Serilog (`Serilog.Enrichers.Span`) e na API do Jaeger para a mesma requisição
- [x] Backend de tracing implantado e acessível — Jaeger (`jaegertracing/all-in-one`) via `docker-compose.yml`, armazenamento no Elasticsearch já usado pelo Catálogo; UI em `http://localhost:16686`, verificado via `GET /api/services` e `GET /api/traces`
- [x] Chamadas HTTP inbound criam spans automaticamente — verificado ao vivo (span `GET api/Game`)
- [x] Spans de query de banco incluídos nos traces — `OpenTelemetry.Instrumentation.EntityFrameworkCore` (pacote beta — sem versão estável ainda), verificado ao vivo: span filho `GameStore` com `db.statement` contendo o SQL real executado
- [x] Taxa de amostragem configurável via `OTEL_TRACES_SAMPLER_ARG` — lido de configuração/env var, aplicado via `TraceIdRatioBasedSampler`; não testado sob carga
- [x] Dados de trace retidos — Jaeger com storage no Elasticsearch (persistente entre restarts do container), não in-memory (padrão do `jaegertracing/all-in-one` sem `SPAN_STORAGE_TYPE`); retenção exata de 7 dias não configurada (depende de política de ILM do Elasticsearch, não configurada nesta branch)

**Limitação conhecida:** o cenário "API Gateway → Games MS → Payments MS → Serverless Function" do Gherkin acima não existe hoje como chamada síncrona (nenhum microsserviço chama outro diretamente — ver `docs/architecture-flow.md`). A única fronteira cross-service real é o RabbitMQ (`GameStore.Vendas` publica `PedidoFinalizadoEvent`); a propagação de trace nessa fronteira foi implementada e comprovada com um teste de integração real (publish→consume via broker real), mas **não há hoje um consumer rodando ao vivo** para gerar uma tela do Jaeger com o trace completo Vendas→Functions: `GameStore.Notifications.Functions` não está containerizado/orquestrado neste docker-compose (Azure Functions Core Tools não fazem parte do stack), e o consumer equivalente em `GameStore.Usuarios` (`PedidoFinalizadoEventConsumer`) existe mas nunca foi registrado no DI/`EventConsumerService` daquele serviço — gap pré-existente, fora do escopo desta tarefa.

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

- [ ] Full purchase trace visible end-to-end in the tracing UI — verificado para o trecho síncrono (HTTP + DB) de cada microsserviço isoladamente; o trecho assíncrono via RabbitMQ foi comprovado por teste automatizado, não por uma trace única ao vivo (ver limitação acima)
- [x] Logs show `trace_id` on every line — verificado ao vivo nos logs do catalogo-api
- [x] Tracing backend URL documented in README — `http://localhost:16686`
