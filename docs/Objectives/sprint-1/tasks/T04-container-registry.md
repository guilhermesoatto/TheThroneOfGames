# T04 — Container Registry

**Sprint:** 1 | **Phase:** Fase 2 | **Priority:** P1 (needed by T03, T05)

## Context

Before the **Deployment Pipeline** can ship the FCG Platform, a **Container Registry** must exist to store and version all **Build Artifacts**. The registry is the single source of truth for every image version ever shipped to the Cloud.

## User Story

> As a **DevOps Engineer**,  
> I want a Container Registry to store all FCG Platform images,  
> so that every Release can be reliably deployed or rolled back by SHA tag.

## Gherkin Scenarios

```gherkin
Feature: Container Registry — Build Artifact Storage

  Background:
    Given a Container Registry is provisioned (DockerHub, ECR, ACR, or GCR)
    And CI/CD pipeline credentials have push/pull access

  Scenario: CI/CD can push a Build Artifact
    Given the CD Pipeline builds the FCG Platform image tagged with a commit SHA
    When the pipeline executes "docker push"
    Then the image appears in the Container Registry under the "fcg-platform" repository
    And both the SHA tag and "latest" tag are present

  Scenario: Old images are retained for rollback
    Given 10 Releases have been pushed to the Container Registry
    When a rollback is needed to Release #7
    Then the Build Artifact for that commit SHA is still available in the registry
    And it can be pulled and deployed successfully

  Scenario: Unauthorized push is rejected
    Given an anonymous client attempts to push an image
    When the push command is executed
    Then the registry rejects the request with HTTP 401
    And no image is stored

  Scenario: Image is scannable for vulnerabilities
    Given a Build Artifact exists in the Container Registry
    When a vulnerability scan is triggered (docker scout / trivy)
    Then the scan report is available
    And HIGH and CRITICAL CVEs are zero (or the team has accepted exceptions)
```

## Acceptance Criteria

- [ ] Registry provisioned and accessible from the CI/CD environment
- [ ] Repository `fcg-platform` created inside the registry
- [ ] Push/pull credentials stored as pipeline secrets (never in code)
- [ ] Image retention policy configured (keep last 20 tags minimum)
- [ ] Vulnerability scanning enabled on push
- [ ] README.md documents the registry URL and how to authenticate locally

## Best Practices

- Use a private registry (ECR private, ACR, or DockerHub private repo) — avoid public exposure
- Enable immutable tags to prevent overwriting a released SHA
- Automate cleanup of dangling images older than 90 days via lifecycle policy
- Store the registry URL as a pipeline variable, not hardcoded

## Technical Notes

```bash
# ECR example: create repository
aws ecr create-repository \
  --repository-name fcg-platform \
  --image-scanning-configuration scanOnPush=true \
  --image-tag-mutability IMMUTABLE

# DockerHub: just create the repo in the UI and set to private
# then push:
docker tag fcg-platform:latest <username>/fcg-platform:<sha>
docker push <username>/fcg-platform:<sha>
```

## Dependencies

- T01 — Dockerfile must exist to produce images

## Definition of Done

- [ ] Registry URL documented in README
- [ ] At least one SHA-tagged image present after first CD run
- [ ] Vulnerability scan result available for the first image
