# T01 — Dockerfile (FCG Platform)

**Sprint:** 1 | **Phase:** Fase 2 | **Priority:** P0 (blocks T03, T04, T05)

## Context

The FCG Platform must be packaged into a **Build Artifact** (Docker image) so it can be shipped consistently to any cloud environment. The image must be lean (small footprint), non-root (security), and multi-stage (clean separation between build and runtime).

## User Story

> As a **DevOps Engineer**,  
> I want the FCG Platform packaged as an optimized Docker image,  
> so that every Release is reproducible, secure, and fast to deploy.

## Gherkin Scenarios

```gherkin
Feature: FCG Platform Dockerfile

  Background:
    Given the FCG Platform source code is at the repository root
    And Docker is installed in the CI environment

  Scenario: Build produces a working FCG Platform image
    Given a valid Dockerfile exists at the repository root
    When "docker build -t fcg-platform:latest ." is executed
    Then the build succeeds with exit code 0
    And the resulting image runs the FCG Platform web server on port 8080

  Scenario: Image uses a non-root user
    Given the FCG Platform image is built
    When "docker run fcg-platform:latest whoami" is executed
    Then the output is NOT "root"

  Scenario: Multi-stage build produces a small image
    Given the Dockerfile uses a multi-stage build (builder + runtime stages)
    When the final image is inspected
    Then the image size is less than 200MB
    And the build tools (compilers, SDK) are NOT present in the runtime layer

  Scenario: Image exposes the correct port
    Given the FCG Platform image is built
    When the image metadata is inspected
    Then the exposed port is 8080
```

## Acceptance Criteria

- [x] Dockerfile exists at repository root
- [x] Multi-stage build: `builder` stage compiles, `runtime` stage runs
- [ ] Base image pinned to **SHA digest** (e.g., `node:20-alpine@sha256:<digest>`) — never `node:latest` or bare tag
- [x] Application runs as a non-root user (UID ≥ 1000, e.g., `uid=1001`)
- [ ] `EXPOSE 8080` declared
- [ ] `.dockerignore` excludes `node_modules`, `.git`, `*.log`, test files
- [ ] `trivy image --severity HIGH,CRITICAL --exit-code 1 <image>` passes (zero CVEs)
- [ ] `trivy image --scanners secret <image>` passes (no embedded secrets)
- [ ] `securityContext` in Kubernetes Pod spec: `readOnlyRootFilesystem: true`, `allowPrivilegeEscalation: false`, `capabilities.drop: [ALL]`

## Best Practices

- Use `--no-cache` in CI to ensure clean builds
- **Pin base image to SHA digest** — a tag can be overwritten; a digest cannot (e.g., `node:20-alpine@sha256:abc123...`). Run `docker pull node:20-alpine && docker inspect --format='{{index .RepoDigests 0}}' node:20-alpine` to get it
- Use `COPY --chown` to avoid post-copy permission fixes
- Add `HEALTHCHECK` instruction so Kubernetes liveness probe auto-discovers it
- Run `trivy image --scanners secret` in CI — secrets accidentally baked into layers are a critical supply-chain risk
- Do NOT install `curl` or `wget` in the runtime image unless strictly required (reduces attack surface)

## Technical Notes

```dockerfile
# Pattern: multi-stage Node.js example
# Pin digest: docker inspect --format='{{index .RepoDigests 0}}' node:20-alpine
FROM node:20-alpine@sha256:<digest> AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build && npm prune --omit=dev

FROM node:20-alpine@sha256:<digest> AS runtime
RUN addgroup -S fcg && adduser -S fcg -G fcg
WORKDIR /app
COPY --from=builder --chown=fcg:fcg /app/dist ./dist
COPY --from=builder --chown=fcg:fcg /app/node_modules ./node_modules
USER fcg
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD wget -qO- http://localhost:8080/health || exit 1
# No shell entrypoint — reduces attack surface
ENTRYPOINT ["node", "dist/main.js"]
```

## Dependencies

- None (first task in the chain)

## Definition of Done

- [x] Dockerfile merged to `main`
- [ ] CI runs `docker build` and scan — zero HIGH/CRITICAL CVEs
- [ ] Image pushed to Container Registry (see T04)
