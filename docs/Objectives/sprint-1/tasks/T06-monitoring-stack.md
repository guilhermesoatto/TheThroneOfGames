# T06 — Monitoring Stack (Health Metrics)

**Sprint:** 1 | **Phase:** Fase 2 | **Priority:** P1 (depends on T05)

## Context

The FCG Platform must expose **Health Metrics** so the team can detect resource exhaustion, latency spikes, and outages before Players report them. A Monitoring Stack collects, stores, and visualises these metrics in real time.

## User Story

> As a **platform operator**,  
> I want a Monitoring Stack collecting Health Metrics from the FCG Platform,  
> so that I can identify and resolve infrastructure problems before Players are affected.

## Gherkin Scenarios

```gherkin
Feature: Monitoring Stack — Health Metrics Collection

  Background:
    Given the FCG Platform is running on the Cloud
    And a Monitoring Stack is provisioned

  Scenario: CPU usage metric is visible
    Given the FCG Platform is under load from Players
    When I open the Monitoring Dashboard
    Then I can see a CPU usage graph over the last 1 hour
    And an alert fires when CPU exceeds 80% for more than 5 minutes

  Scenario: Memory usage metric is visible
    Given the FCG Platform container is running
    When I open the Monitoring Dashboard
    Then I can see a memory consumption graph
    And an alert fires when memory exceeds 85% of the container limit

  Scenario: HTTP error rate is tracked
    Given the FCG Platform is serving Player requests
    When more than 5% of HTTP responses in 1 minute are 5xx errors
    Then an alert fires and the team is notified

  Scenario: Application uptime is visible
    Given the Monitoring Stack is connected to the FCG Platform
    When I view the uptime panel
    Then I can see the FCG Platform uptime percentage for the last 7 days

  Scenario: Dashboard is accessible without deploying again
    Given the Monitoring Stack is deployed once
    When the FCG Platform is redeployed via CD Pipeline
    Then the Monitoring Stack continues to collect metrics automatically
    And no manual reconfiguration is needed
```

## Acceptance Criteria

- [x] At least one monitoring tool configured: Prometheus + Grafana **or** Datadog **or** New Relic **or** AWS CloudWatch **or** Azure Monitor
- [ ] Metrics collected: CPU, memory, HTTP request rate, HTTP error rate, uptime
- [ ] At least one alert rule configured (CPU > 80% or error rate > 5%)
- [ ] Dashboard accessible via URL (screenshot included in video demo)
- [x] Metrics scraping/collection is automated — no manual steps after deploy
- [ ] Application exposes a `/metrics` endpoint if using Prometheus

## Best Practices

- For simplicity: AWS CloudWatch (zero extra infra if already on AWS) or Grafana Cloud free tier
- Add `correlation_id` to application logs so traces link back to metrics spikes
- Use a structured JSON log format so monitoring tools can parse and filter efficiently
- Define alert thresholds based on measured baselines, not assumptions

## Suggested Stack Options

| Stack | Effort | Notes |
|-------|--------|-------|
| Prometheus + Grafana | Medium | Self-hosted, full control |
| AWS CloudWatch | Low | Native if using ECS/EC2 |
| Azure Monitor | Low | Native if using Azure |
| Datadog | Low | SaaS, 14-day trial available |
| New Relic | Low | Free tier up to 100GB/month |

## Technical Notes

```yaml
# Prometheus scrape config example
scrape_configs:
  - job_name: 'fcg-platform'
    static_configs:
      - targets: ['fcg-platform:8080']
    metrics_path: '/metrics'
    scrape_interval: 15s
```

```typescript
// Express.js: expose /metrics for Prometheus
import { register, collectDefaultMetrics } from 'prom-client';
collectDefaultMetrics();
app.get('/metrics', async (req, res) => {
  res.set('Content-Type', register.contentType);
  res.end(await register.metrics());
});
```

## Dependencies

- T05 — FCG Platform must be running on Cloud before metrics can be collected

## Definition of Done

- [ ] Dashboard screenshot included in the demo video
- [ ] At least one alert rule active and tested
- [x] Monitoring Stack URL documented in README
