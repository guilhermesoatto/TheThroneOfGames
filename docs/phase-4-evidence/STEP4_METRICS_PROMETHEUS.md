# Step 4: Metrics - Prometheus and OpenTelemetry Integration

## Overview

Implemented Prometheus metrics instrumentation and OpenTelemetry integration for comprehensive observability of the TheThroneOfGames platform.

## Deliverables

### 1. Telemetry Extensions (TelemetryExtensions.cs)
**Purpose**: Centralized configuration for OpenTelemetry and Prometheus instrumentation

**Features**:
- OpenTelemetry SDK initialization
- Prometheus exporter configuration
- ASP.NET Core instrumentation (HTTP requests, latency)
- HTTP client instrumentation
- Runtime metrics collection
- Process-level metrics

**Custom Metrics**:
1. **EventsPublishedCounter** - Total domain events published (labeled by event_type, status)
2. **QueueLengthGauge** - Current RabbitMQ queue length (for HPA scaling)
3. **EventProcessingLatencyHistogram** - Event processing duration (distribution)
4. **UsersRegisteredCounter** - Cumulative user registrations
5. **GamesPurchasedCounter** - Cumulative game purchases (labeled by game_name)
6. **ActiveUsersGauge** - Active users online in real-time
7. **DatabaseLatencyHistogram** - Query performance monitoring (labeled by query_type, status)
8. **AuthenticationAttemptsCounter** - Auth attempts tracking (labeled by result)
9. **ValidationErrorsCounter** - Form/data validation failures (labeled by field)

### 2. NuGet Dependencies Added

**prometheus-net.AspNetCore** (8.1.0)
- Prometheus metrics export format
- Built-in ASP.NET Core middleware
- /metrics endpoint (Prometheus scrape target)

**OpenTelemetry** (1.7.0)
- Core OpenTelemetry SDK
- Metrics and tracing APIs

**OpenTelemetry.Exporter.Prometheus.AspNetCore** (1.7.0-rc.1)
- Prometheus exporter for OpenTelemetry metrics
- Push/Pull model support

**OpenTelemetry.Instrumentation.AspNetCore** (1.7.0)
- Automatic ASP.NET Core metrics:
  - HTTP request duration (histogram)
  - HTTP request size (histogram)
  - HTTP response size (histogram)
  - Active HTTP requests (gauge)

**OpenTelemetry.Instrumentation.Http** (1.7.0)
- HTTP client request instrumentation
- Outbound service call tracing

**OpenTelemetry.Instrumentation.Runtime** (0.3.0-alpha.1)
- Runtime metrics:
  - GC collections
  - Memory allocation
  - Exceptions thrown

**OpenTelemetry.Instrumentation.Process** (0.3.0-alpha.1)
- Process-level metrics:
  - CPU time
  - Memory usage
  - Process uptime

### 3. Integration Points

**Docker-Compose Integration**:
- API exposes /metrics endpoint on port 5000
- Prometheus configured in monitoring/prometheus.yml
- Prometheus scrapes API metrics every 5 seconds
- Data stored in `prometheus-data` volume

**Grafana Integration**:
- Prometheus configured as datasource
- Ready for custom dashboards:
  - API performance (request rates, latencies)
  - Queue length (for HPA decisions)
  - Resource usage (CPU, memory, GC)
  - Business metrics (users registered, purchases)

### 4. Metrics Hierarchy

**Infrastructure Metrics** (Automatic):
```
# HTTP Server
http_requests_received_total
http_request_duration_seconds (histogram)
http_request_body_size_bytes (histogram)
http_response_body_size_bytes (histogram)
http_request_route_gauge (active requests)

# Runtime
process_cpu_seconds_total
process_memory_bytes
process_resident_memory_bytes
dotnet_gc_collections_total
dotnet_exceptions_thrown_total
```

**Business Metrics** (Custom):
```
# Events
thethroneofgames_events_published_total (event_type, status)
thethroneofgames_event_processing_latency_seconds (event_type)

# Queue
thethroneofgames_queue_length (queue_name)

# Users
thethroneofgames_users_registered_total
thethroneofgames_active_users

# Sales
thethroneofgames_games_purchased_total (game_name)

# Database
thethroneofgames_database_latency_seconds (query_type, status)

# Security
thethroneofgames_authentication_attempts_total (result)
thethroneofgames_validation_errors_total (field)
```

### 5. Usage Examples

**Recording Event Publication**:
```csharp
using TheThroneOfGames.API.Telemetry;

// In event publishing code
ApplicationMetrics.RecordEventPublished("UsuarioAtivadoEvent", "success");
```

**Updating Queue Length**:
```csharp
// Periodic update from RabbitMQ consumer
ApplicationMetrics.UpdateQueueLength("usuario.eventos", queueLength);
```

**Recording Processing Time**:
```csharp
var stopwatch = Stopwatch.StartNew();
// ... process event ...
stopwatch.Stop();
ApplicationMetrics.RecordEventProcessingLatency("UsuarioAtivadoEvent", stopwatch.Elapsed.TotalSeconds);
```

### 6. Prometheus Queries

**API Request Rate**:
```promql
rate(http_requests_received_total[1m])
```

**P95 Request Latency**:
```promql
histogram_quantile(0.95, http_request_duration_seconds_bucket)
```

**Queue Length for HPA**:
```promql
thethroneofgames_queue_length{queue_name="usuario.eventos"}
```

**Event Publishing Rate**:
```promql
rate(thethroneofgames_events_published_total[1m])
```

**Error Rate**:
```promql
rate(thethroneofgames_events_published_total{status="error"}[1m]) / rate(thethroneofgames_events_published_total[1m])
```

## Architecture

```
App Request
    ↓
ASP.NET Core Instrumentation (automatic)
    ↓
Application Code
    ↓
Business Metrics Recording (manual)
    ↓
OpenTelemetry Collector
    ↓
Prometheus Exporter
    ↓
/metrics Endpoint (Prometheus scrape)
    ↓
Prometheus Storage (prometheus-data volume)
    ↓
Grafana Visualization
```

## Verification Steps

1. **Check /metrics Endpoint**:
   ```bash
   curl http://localhost:5000/metrics
   ```
   Should show Prometheus format metrics

2. **Verify in Prometheus UI**:
   - Visit http://localhost:9090
   - Graph → Select metric → Execute
   - Example: `http_requests_received_total`

3. **Verify in Grafana**:
   - Visit http://localhost:3000
   - Data Sources → Prometheus should show "UP"
   - Explore → Query metrics

4. **Check Custom Metrics**:
   ```promql
   # In Prometheus UI
   {__name__=~"thethroneofgames_.*"}
   ```

## Next Steps

1. **Step 5**: Implement Polly resilience policies for retry/circuit-breaker
2. **Step 6+**: Use queue_length metric for Kubernetes HPA scaling
3. **Create Dashboards**: Build Grafana dashboards for visualization
4. **Alerting**: Configure Prometheus alert rules
5. **Tracing**: Implement distributed tracing with Jaeger

## Acceptance Criteria Met

✅ Prometheus metrics exported on /metrics endpoint
✅ OpenTelemetry SDK configured
✅ ASP.NET Core instrumentation enabled
✅ HTTP request metrics collected
✅ Custom business metrics defined
✅ Queue length metric for autoscaling
✅ Integration with docker-compose stack
✅ Prometheus configured to scrape metrics
✅ Grafana datasource ready
✅ Scalable metric collection

## Notes

- Metrics are collected automatically by OpenTelemetry instrumentations
- Custom business metrics should be recorded at key points in application logic
- Queue length metric will be updated by RabbitMQ consumer in background tasks
- Prometheus retention policy: 30 days (configurable in docker-compose.yml)
- Grafana is pre-configured to read Prometheus datasource
