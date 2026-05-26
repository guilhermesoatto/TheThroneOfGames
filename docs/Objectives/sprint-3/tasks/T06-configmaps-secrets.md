# T06 — ConfigMaps and Secrets

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P0 (required before any Pod can start)

## Context

Every FCG Microservice needs runtime configuration (DB URLs, broker addresses, API keys). **ConfigMaps** hold non-sensitive config, and Kubernetes **Secrets** hold credentials. This task eliminates all hardcoded configuration from Docker images and pipeline YAML files.

## User Story

> As a **security engineer**,  
> I want all FCG configuration and credentials to be injected via ConfigMaps and Secrets,  
> so that no sensitive data ever appears in code, container images, or Git history.

## Gherkin Scenarios

```gherkin
Feature: ConfigMaps and Secrets — Configuration Injection

  Background:
    Given the Kubernetes Cluster is running (T03)
    And the "fcg" namespace exists

  Scenario: Microservice reads DB URL from ConfigMap
    Given a ConfigMap "users-config" contains "DB_HOST=postgres.fcg.svc"
    When the Users Pod starts
    Then the "DB_HOST" environment variable inside the Pod equals "postgres.fcg.svc"
    And the Dockerfile contains no hardcoded DB_HOST value

  Scenario: Microservice reads DB password from Secret
    Given a Secret "users-secrets" contains "DB_PASSWORD=<hashed>"
    When the Users Pod starts
    Then the "DB_PASSWORD" environment variable is available inside the container
    And "kubectl get secret users-secrets -o yaml" shows the value as base64, not plaintext
    And the plaintext password does NOT appear in any log line

  Scenario: Pod fails to start if required Secret is missing
    Given the "payments-secrets" Secret does not exist
    When the Payments Deployment is applied
    Then the Payments Pods enter "Pending" or "CreateContainerConfigError" state
    And an error event states the Secret is missing

  Scenario: Rotating a Secret does not require a new image build
    Given the DB password is rotated
    When the "users-secrets" Secret is updated in Kubernetes
    And the Users Pods are restarted (rolling restart)
    Then the Users Microservice connects to the DB with the new password
    And no code change or new Docker image is required

  Scenario: Git history contains no secrets
    Given all configuration is stored in Kubernetes resources
    When the Git repository history is scanned with "git-secrets" or "trufflehog"
    Then no credentials, API keys, or connection strings are found
```

## Acceptance Criteria

- [ ] `ConfigMap` manifest per Microservice: `users-config`, `games-config`, `payments-config`
- [ ] `Secret` manifest per Microservice: `users-secrets`, `games-secrets`, `payments-secrets`
- [ ] ConfigMaps hold: `DB_HOST`, `DB_PORT`, `BROKER_URL`, `APP_PORT`, `LOG_LEVEL`, `OTEL_EXPORTER_OTLP_ENDPOINT`
- [ ] Secrets hold: `DB_PASSWORD`, `JWT_SECRET`, `PAYMENT_PROVIDER_KEY`, `REGISTRY_TOKEN` (as applicable)
- [ ] All Pods reference ConfigMaps and Secrets via `envFrom` (not hardcoded `env` values)
- [ ] Secrets committed to Git ONLY as sealed or encrypted form (SealedSecrets, SOPS, or External Secrets Operator) — never as plaintext base64
- [ ] README documents how to bootstrap Secrets for a new developer

## Best Practices

- Use **Sealed Secrets** (Bitnami) or **External Secrets Operator** to store encrypted secrets in Git safely
- Never pass secrets as command-line args (visible in `ps aux` inside the Pod)
- Rotate secrets without downtime: update the Secret, then trigger a rolling restart (`kubectl rollout restart deployment`)
- Audit access to Secrets with Kubernetes RBAC — only the specific ServiceAccount for each Microservice should read its own Secret

## Technical Notes

```yaml
# users-configmap.yaml (non-sensitive)
apiVersion: v1
kind: ConfigMap
metadata:
  name: users-config
  namespace: fcg
data:
  DB_HOST: "postgres.fcg.svc.cluster.local"
  DB_PORT: "5432"
  APP_PORT: "8080"
  LOG_LEVEL: "info"
  BROKER_URL: "amqp://rabbitmq.fcg.svc.cluster.local"
```

```yaml
# users-secrets.yaml — DO NOT commit plaintext. Use SealedSecret instead.
# For local dev only:
apiVersion: v1
kind: Secret
metadata:
  name: users-secrets
  namespace: fcg
type: Opaque
stringData:        # kubectl converts to base64 automatically
  DB_PASSWORD: "change-me-in-production"
  JWT_SECRET: "change-me-in-production"
```

```bash
# SealedSecrets: encrypt before committing to Git
kubeseal --format yaml < users-secrets-plain.yaml > users-secrets-sealed.yaml
git add users-secrets-sealed.yaml  # safe to commit
```

## Dependencies

- T03 — Cluster and namespace must exist
- Must be completed BEFORE T04 Pods can start

## Definition of Done

- [ ] All 3 ConfigMaps and 3 Secrets created in `fcg` namespace
- [ ] Pods start without `CreateContainerConfigError`
- [ ] Git scan finds zero plaintext credentials
- [ ] README documents Secret bootstrapping process
