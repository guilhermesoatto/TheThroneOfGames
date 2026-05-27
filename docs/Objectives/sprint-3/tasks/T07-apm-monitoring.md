# T07 — APM Monitoring (Application Performance Monitoring)

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P1 (required to validate HPA thresholds)

## Context

With all three Microservices running in Kubernetes and auto-scaling via HPA, the team needs **Application Performance Monitoring (APM)** to observe request latency, error rates, throughput, and Pod health in real time. APM feeds the metrics that HPA uses and gives the team early warning of degradation before Players notice.

## User Story

> As a **platform operator**,  
> I want APM dashboards showing request latency, error rates, and Pod metrics,  
> so that I can confirm the FCG Platform is performing well and HPA is scaling correctly.

## Gherkin Scenarios

```gherkin
Feature: APM Monitoring — Performance Observability

  Background:
    Given all three Microservices are running in the Kubernetes Cluster
    And an APM tool is configured (Datadog, New Relic, Prometheus+Grafana, or AWS CloudWatch)

  Scenario: Request latency is visible per Microservice
    Given the FCG Platform is serving Player requests
    When I open the APM Dashboard
    Then I can see p50, p95, and p99 request latency for each Microservice
    And requests with latency > 1 second are highlighted as anomalies

  Scenario: HTTP error rate alert fires on 5xx spike
    Given the APM alert is configured: error rate > 5% over 1 minute
    When the Games Microservice returns 5xx for 6% of requests
    Then the alert fires within 2 minutes
    And the on-call team is notified

  Scenario: HPA scaling events are correlated with APM metrics
    Given the HPA scales the Games Deployment from 1 to 3 Pods
    When I view the APM Dashboard for that time window
    Then I can see CPU usage spike before scale-out
    And I can see latency dropping after the new Pods become ready
    And the replica count metric shows the change from 1 to 3

  Scenario: Pod restart is captured as an incident
    Given the Payments Pod is OOMKilled and restarted
    When I open the APM incident view
    Then the OOMKill event appears with the timestamp
    And the memory consumption graph shows the spike before the kill

  Scenario: Monitoring continues after Cluster upgrade
    Given the Kubernetes Cluster version is upgraded
    When the upgrade completes
    Then the APM Agent Pods restart automatically
    And metrics collection resumes within 5 minutes
    And no manual reconfiguration is needed
```

## Acceptance Criteria

- [ ] APM tool deployed and collecting from all 3 Microservices
- [ ] Metrics visible: `http.request.duration.p95`, `http.error.rate`, `process.cpu.utilization`, `process.memory.usage`
- [ ] Kubernetes cluster-level metrics visible: Pod count, Pod restart count, Node CPU/memory
- [ ] At least one alert rule configured: `error_rate > 5% for 1 minute`
- [ ] Dashboard showing per-Microservice latency and HPA replica count in the same view
- [ ] APM Agent deployed as a Kubernetes DaemonSet or sidecar (not as a manual install)
- [ ] APM data retained for at least 7 days

## Suggested APM Options

| Tool | Deployment Model | Notes |
|------|-----------------|-------|
| Datadog | DaemonSet agent | Best K8s integration, 14-day free trial |
| New Relic | DaemonSet agent | Free tier: 100GB/month |
| Prometheus + Grafana | Self-hosted in Cluster | Open-source, full control |
| AWS CloudWatch Container Insights | DaemonSet | Native if on EKS |
| Azure Monitor | DaemonSet / OOTB AKS | Native if on AKS |

## Technical Notes

```yaml
# Prometheus + Grafana via Helm (simplest self-hosted option)
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm upgrade --install kube-prometheus-stack \
  prometheus-community/kube-prometheus-stack \
  --namespace monitoring --create-namespace \
  --set grafana.adminPassword=$GRAFANA_PASSWORD
```

```typescript
// Application-level HTTP metrics (prom-client for Node.js)
import { Histogram, collectDefaultMetrics, register } from 'prom-client';
collectDefaultMetrics({ prefix: 'fcg_users_' });

const httpDuration = new Histogram({
  name: 'fcg_users_http_request_duration_seconds',
  help: 'HTTP request duration in seconds',
  labelNames: ['method', 'route', 'status_code'],
});

// Middleware
app.use((req, res, next) => {
  const end = httpDuration.startTimer();
  res.on('finish', () => end({ method: req.method, route: req.route?.path, status_code: res.statusCode }));
  next();
});
```

## Dependencies

- T03 — Kubernetes Cluster running
- T04 — Microservice Pods deployed
- T05 — HPA active (to correlate scaling events with APM metrics)

## Definition of Done

- [ ] APM Dashboard screenshot showing p95 latency + HPA scale event included in demo video
- [ ] At least one alert rule triggered and notified during load test
- [ ] APM Agent running as DaemonSet confirmed with `kubectl get pods -n monitoring`
