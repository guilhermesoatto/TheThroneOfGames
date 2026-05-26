# T03 — CD Pipeline (Continuous Delivery)

**Sprint:** 1 | **Phase:** Fase 2 | **Priority:** P0 (depends on T01, T02, T04)

## Context

The second half of the **Deployment Pipeline** is Continuous Delivery: when code is merged to `main`, the FCG Platform must be automatically built, packaged as a **Build Artifact**, pushed to the **Container Registry**, and deployed to the Cloud — with zero manual steps.

## User Story

> As a **release manager**,  
> I want every merge to `main` to automatically deploy the FCG Platform to the Cloud,  
> so that Releases are fast, consistent, and risk-free.

## Gherkin Scenarios

```gherkin
Feature: CD Pipeline — Automated Deployment

  Background:
    Given the CI Pipeline has already passed on the Pull Request
    And T01 (Dockerfile) and T04 (Container Registry) are complete

  Scenario: Merge to main triggers a new Release
    Given a Pull Request is approved and merged into "main"
    When the CD Pipeline triggers
    Then a new Build Artifact is created tagged with the commit SHA and "latest"
    And the Build Artifact is pushed to the Container Registry
    And the FCG Platform on the Cloud is updated to the new version
    And the FCG Platform Health Metric returns HTTP 200 on the /health endpoint

  Scenario: Failed docker build blocks deployment
    Given the CD Pipeline is triggered
    When the docker build step fails
    Then no Build Artifact is pushed
    And the cloud deployment does not occur
    And the pipeline status is "failed"
    And the team is notified

  Scenario: Rollback is possible from Container Registry
    Given a bad Release was deployed
    When an operator re-runs the CD Pipeline pointing to a previous commit SHA tag
    Then the Container Registry provides the previous Build Artifact
    And the Cloud deployment reverts to the previous version

  Scenario: CD pipeline is idempotent
    Given the same commit SHA triggers the CD Pipeline twice
    When both runs complete
    Then the FCG Platform is in the same final state
    And no duplicate resources are created in the cloud
```

## Acceptance Criteria

- [ ] CD pipeline file exists (`.github/workflows/cd.yml` or as a separate stage in Multistage pipeline)
- [ ] Trigger: `push` to `main` (merge event) only
- [ ] Steps: checkout → docker build → docker push → cloud deploy
- [ ] Build Artifact is tagged with `${{ github.sha }}` AND `latest`
- [ ] Cloud credentials injected via secrets — never hardcoded in YAML
- [ ] Post-deploy health check step validates the FCG Platform is live
- [ ] Pipeline failure sends notification (email, Slack, or Teams)

## Best Practices

- Use OIDC (Workload Identity Federation) for cloud auth instead of long-lived access keys
- Separate `build-and-push` job from `deploy` job so each has a single responsibility
- Always tag images with the Git SHA — `latest` alone is not rollback-safe
- Run a smoke test after deploy (hit `/health` endpoint and assert HTTP 200)

## Technical Notes

```yaml
# Pattern: GitHub Actions CD
name: CD
on:
  push:
    branches: [main]

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write           # OIDC
    steps:
      - uses: actions/checkout@v4
      - name: Log in to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ secrets.REGISTRY_URL }}
          username: ${{ secrets.REGISTRY_USER }}
          password: ${{ secrets.REGISTRY_TOKEN }}
      - name: Build and Push
        uses: docker/build-push-action@v5
        with:
          push: true
          tags: |
            ${{ secrets.REGISTRY_URL }}/fcg-platform:${{ github.sha }}
            ${{ secrets.REGISTRY_URL }}/fcg-platform:latest

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Cloud
        run: |
          # cloud-specific deploy command here
          # e.g.: aws ecs update-service or kubectl set image ...
      - name: Health Check
        run: |
          curl --fail --retry 5 --retry-delay 10 ${{ secrets.FCG_HEALTH_URL }}/health
```

## Dependencies

- T01 — Dockerfile must exist
- T02 — CI Pipeline must pass first (branch protection)
- T04 — Container Registry credentials must be configured

## Definition of Done

- [ ] CD pipeline triggers on every merge to `main`
- [ ] Build Artifact tagged with SHA visible in Container Registry
- [ ] FCG Platform accessible on Cloud URL after pipeline
- [ ] Health check step passes at end of CD run
