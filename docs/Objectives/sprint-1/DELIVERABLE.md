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

- [ ] `T01` — Dockerfile written and validated (lean, non-root, multi-stage)
- [ ] `T02` — CI Pipeline: executes tests on every PR/Commit
- [ ] `T03` — CD Pipeline: deploys on merge to `main`
- [ ] `T04` — Container Registry configured and image published
- [ ] `T05` — FCG Platform running on a Cloud Provider
- [ ] `T06` — Monitoring Stack collecting Health Metrics

## Architectural Constraints (Fase 2)

- Architecture remains **monolithic** — no service split in this sprint
- The pipeline is the only gate to production; no manual SSH deploys allowed
- Docker image must be pushed **before** the deploy step runs

## Scope alignment (2026-09-06)

O enunciado oficial da atividade (`fase 2.pdf`) pede **CI (compilar → testar → gerar artefato)
+ CD (deploy automatizado) demonstrados em vídeo**. Ajustes de escopo desta entrega:

- Runtime migrado para **.NET 10 (LTS)** — nenhuma referência a `net9.0` / `9.0.x`.
- CI reorganizada em **jobs separados**: `build`, `lint`, `test`, `integration`, `e2e`, `package`, `deploy`.
- **`T05` (cloud)** e **`T06` (observabilidade)** ficam **fora do CI**: por ser monólito, não há
  slave services (SQL Server, Prometheus, Grafana) disponíveis na esteira. O "deploy em produção"
  é a **imagem versionada publicada no GHCR** (`package`) promovida para `:production` (`deploy`);
  a demonstração no vídeo é `docker compose pull && docker compose up -d` puxando essa imagem, com
  Prometheus/Grafana subindo junto localmente.
- Testes de integração migrados de Testcontainers+SQL para **EF Core InMemory** (rodam sem Docker).
- Critério "cobertura > 80%" **não atingido** — gap conhecido, ver
  [../../reports/alinhamento-rubrica-fase2.md](../../reports/alinhamento-rubrica-fase2.md).

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
