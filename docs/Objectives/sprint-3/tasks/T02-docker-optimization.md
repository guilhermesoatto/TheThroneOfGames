# T02 — Docker Image Optimization

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P1 (impacts deploy speed and Cluster cost)

## Context

Faster deployments and lower cloud costs depend on lean, secure Docker images. All three Microservice images from Sprint 2 must be audited and optimised: smaller base images, fewer layers, no build tools in the runtime layer, and zero HIGH/CRITICAL CVEs. Lean images also reduce Kubernetes Pod startup time, which directly improves HPA scale-out speed.

## User Story

> As a **DevOps Engineer**,  
> I want all FCG Microservice images to be lean and secure,  
> so that Kubernetes Pods start faster and cloud storage costs are minimised.

## Gherkin Scenarios

```gherkin
Feature: Docker Image Optimization

  Background:
    Given Dockerfiles exist for all three Microservices (Users, Games, Payments)

  Scenario: Each image uses a minimal base
    Given all Dockerfiles use alpine or distroless base images
    When each image is built
    Then each image size is less than 150MB
    And no unnecessary OS packages are installed

  Scenario: Build tools are not present in the runtime image
    Given a multi-stage Dockerfile
    When the runtime image layer is inspected
    Then compilers, SDKs, and test frameworks are NOT present

  Scenario: Images have zero HIGH or CRITICAL CVEs
    Given a Microservice image is built
    When a vulnerability scan runs (trivy or docker scout)
    Then the scan reports zero HIGH vulnerabilities
    And the scan reports zero CRITICAL vulnerabilities

  Scenario: Container runs as non-root
    Given a Microservice image
    When "docker run <image> whoami" is executed
    Then the output is NOT "root"
    And the user UID is not 0

  Scenario: Image build is reproducible
    Given the same commit SHA
    When the docker build runs twice with the same base image digest
    Then both resulting images are functionally identical
    And both pass the vulnerability scan
```

## Acceptance Criteria

- [ ] Multi-stage Dockerfile for each of the 3 Microservices
- [ ] Runtime base image: `node:20-alpine`, `python:3.12-slim`, or `distroless` (language-appropriate)
- [ ] Final image size ≤ 150MB per Microservice
- [ ] Non-root user defined with UID ≥ 1000
- [ ] `HEALTHCHECK` instruction present in every Dockerfile
- [ ] Zero HIGH/CRITICAL CVEs on scan (trivy or docker scout)
- [ ] `.dockerignore` excludes: `node_modules`, `.git`, `*.test.*`, `*.spec.*`, `dist-test/`
- [ ] Base image version pinned to digest or specific tag (e.g., `node:20.14-alpine3.20`)

## Best Practices

- Use `--platform=linux/amd64` in CI to ensure images are built for the Cluster architecture
- Add `LABEL org.opencontainers.image.revision=$GIT_SHA` for image traceability
- Run `trivy image --exit-code 1 --severity HIGH,CRITICAL <image>` in CI to fail the build on CVEs
- Order Dockerfile layers from least-changed to most-changed to maximise layer caching

## Technical Notes

```dockerfile
# Optimized multi-stage pattern
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build && npm prune --omit=dev

FROM node:20-alpine AS runtime
RUN addgroup -S fcg && adduser -S fcg -G fcg
WORKDIR /app
COPY --from=builder --chown=fcg:fcg /app/dist ./dist
COPY --from=builder --chown=fcg:fcg /app/node_modules ./node_modules
USER fcg
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
CMD ["node", "dist/main.js"]
```

## Dependencies

- T03 — Kubernetes Cluster pulls these optimized images

## Definition of Done

- [ ] All 3 Microservice images ≤ 150MB
- [ ] trivy scan reports 0 HIGH/CRITICAL CVEs on all 3 images
- [ ] Build time measured and within 3 minutes per image in CI
