# Sprint 1 — Deliverable Artifact
> Phase 2 | FCG — FIAP Cloud Games

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **FCG Platform** | The FIAP Cloud Games monolith application deployed to cloud |
| **Build Artifact** | Compiled, versioned Docker image stored in a registry |
| **Deployment Pipeline** | Automated chain: Code → Test → Build → Ship → Run |
| **Health Metric** | Observable signal indicating the FCG Platform resource state |
| **Container Registry** | Central repository holding all versioned FCG images |
| **Release** | A successful, observable merge-to-main that produces a running FCG instance |

## Sprint Goal

> Deliver a **fully automated Deployment Pipeline** that produces a **lean Docker image**, publishes it to a **Container Registry**, deploys to a **Cloud Provider**, and exposes **Health Metrics** — all without manual intervention.

## Deliverables Checklist

- [ ] `T01` — Dockerfile written and validated (lean, non-root, multi-stage) _(Audit: partial)_
- [ ] `T02` — CI Pipeline: executes tests on every PR/Commit _(Audit: partial)_
- [ ] `T03` — CD Pipeline: deploys on merge to `main` _(Audit: partial)_
- [ ] `T04` — Container Registry configured and image published _(Audit: partial)_
- [ ] `T05` — FCG Platform running on a Cloud Provider _(Audit: partial)_
- [ ] `T06` — Monitoring Stack collecting Health Metrics _(Audit: partial)_

## Architectural Constraints (Fase 2)

- Architecture remains **monolithic** — no service split in this sprint
- The pipeline is the only gate to production; no manual SSH deploys allowed
- Docker image must be pushed **before** the deploy step runs

## Acceptance Criteria (Sprint Gate)

```gherkin
Feature: Sprint 1 Release Gate

  Scenario: Automated Release is working end-to-end
    Given a developer merges a Pull Request into "main"
    When the CD Pipeline finishes
    Then the FCG Platform is accessible on the Cloud URL
    And the Container Registry contains a new Build Artifact tagged with the commit SHA
    And the Monitoring Stack shows the application is healthy

  Scenario: Broken code is blocked from reaching production
    Given a developer opens a Pull Request with failing tests
    When the CI Pipeline runs
    Then the pipeline fails
    And no Build Artifact is created
    And the merge is blocked
```

## Linked Tasks

| ID | Title | File |
|----|-------|------|
| T01 | Dockerfile | [tasks/T01-dockerfile.md](tasks/T01-dockerfile.md) |
| T02 | CI Pipeline | [tasks/T02-ci-pipeline.md](tasks/T02-ci-pipeline.md) |
| T03 | CD Pipeline | [tasks/T03-cd-pipeline.md](tasks/T03-cd-pipeline.md) |
| T04 | Container Registry | [tasks/T04-container-registry.md](tasks/T04-container-registry.md) |
| T05 | Cloud Deploy | [tasks/T05-cloud-deploy.md](tasks/T05-cloud-deploy.md) |
| T06 | Monitoring Stack | [tasks/T06-monitoring-stack.md](tasks/T06-monitoring-stack.md) |
