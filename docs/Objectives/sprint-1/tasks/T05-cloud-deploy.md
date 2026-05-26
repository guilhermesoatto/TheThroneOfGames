# T05 — Cloud Deploy (FCG Platform on Cloud)

**Sprint:** 1 | **Phase:** Fase 2 | **Priority:** P1 (depends on T01, T03, T04)

## Context

The FCG Platform must run on a **Cloud Provider** (AWS, Azure, GCP, or other) so it is accessible to all Players (students). The cloud environment must be able to handle variable load from Players — this is the scalability and resilience requirement for this phase.

## User Story

> As a **Player (student)**,  
> I want the FCG Platform to be available on the internet at all times,  
> so that I can access games and features without downtime.

## Gherkin Scenarios

```gherkin
Feature: FCG Platform — Cloud Deployment

  Background:
    Given a Build Artifact is available in the Container Registry
    And cloud credentials are configured in the CD Pipeline

  Scenario: FCG Platform is accessible after first deploy
    Given the CD Pipeline runs for the first time
    When the deploy step completes
    Then the FCG Platform is reachable at a public Cloud URL
    And GET <cloud-url>/health returns HTTP 200

  Scenario: New Release replaces the running version
    Given the FCG Platform version "1.0" is running
    And a new Build Artifact tagged "1.1" is pushed to the Container Registry
    When the CD Pipeline deploys "1.1"
    Then Players experience zero downtime (rolling update or blue-green)
    And the FCG Platform now reports version "1.1"

  Scenario: Cloud service restarts on crash
    Given the FCG Platform container crashes unexpectedly
    When the cloud service health check detects the failure
    Then the container is automatically restarted
    And the FCG Platform is accessible again within 60 seconds

  Scenario: Environment variables are injected at runtime
    Given secrets (DB connection string, API keys) are stored in the cloud secret manager
    When the FCG Platform container starts
    Then the application reads configuration from environment variables
    And no secrets appear in the container image or pipeline logs
```

## Acceptance Criteria

- [ ] FCG Platform deployed to chosen Cloud Provider (AWS ECS / Azure App Service / GCP Cloud Run / K8s)
- [ ] Public URL documented in README
- [ ] `/health` endpoint returns `200 OK` with `{ "status": "ok" }`
- [ ] Rolling update or blue-green strategy configured (zero-downtime deploys)
- [ ] Auto-restart enabled (ECS task restart policy / Kubernetes restart policy)
- [ ] All secrets injected via cloud secret manager or environment variables — never in image
- [ ] Resource limits defined (CPU and memory caps to control cloud costs)

## Best Practices

- For MVP scalability: start with AWS ECS Fargate or Azure Container Apps — both auto-scale with zero cluster management overhead
- Always define a health check path so the load balancer can route only to healthy instances
- Tag all cloud resources with `project=fcg`, `env=prod`, `phase=2` for cost tracking
- Keep compute in the free tier if possible (AWS Free Tier / Azure Student subscription)

## Suggested Cloud Options

| Cloud | Service | Notes |
|-------|---------|-------|
| AWS | ECS Fargate | Serverless containers, free tier available |
| Azure | Container Apps | Auto-scale to zero, student license |
| GCP | Cloud Run | Pay-per-request, generous free tier |
| Any | Plain VM + Docker | Simpler but less scalable |

## Technical Notes

```bash
# AWS ECS Fargate quick-start (after ECR push)
aws ecs create-service \
  --cluster fcg-cluster \
  --service-name fcg-platform \
  --task-definition fcg-platform:1 \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "..."

# Health check endpoint (Express.js example)
app.get('/health', (req, res) => res.json({ status: 'ok', version: process.env.APP_VERSION }));
```

## Dependencies

- T01 — Dockerfile
- T03 — CD Pipeline (deploys the image)
- T04 — Container Registry (source of Build Artifact)

## Definition of Done

- [ ] FCG Platform URL in README
- [ ] `/health` returning 200 after CD run
- [ ] Auto-restart verified (stop container manually, confirm it recovers)
