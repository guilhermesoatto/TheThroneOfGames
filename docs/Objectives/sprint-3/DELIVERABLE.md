# Sprint 3 — Deliverable Artifact
> Phase 4 | FCG — FIAP Cloud Games

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **Message Broker** | The infrastructure component (RabbitMQ, Kafka, or SQS) that decouples Microservices via async Events |
| **Queue** | An ordered buffer holding Events until a consumer processes them |
| **Dead-Letter Queue (DLQ)** | A Queue that captures Events that failed processing after all retry attempts |
| **Cluster** | The Kubernetes control plane and worker nodes that orchestrate all Microservice Containers |
| **Pod** | The smallest deployable unit in Kubernetes — wraps one or more Containers |
| **Deployment** | A Kubernetes resource declaring the desired state of a Microservice (replicas, image, env) |
| **HPA** | Horizontal Pod Autoscaler — automatically scales Pod count based on CPU/memory metrics |
| **Helm Chart** | A packaged, versioned set of Kubernetes manifests for a Microservice |
| **ConfigMap** | A Kubernetes resource holding non-sensitive configuration key-value pairs |
| **Secret** | A Kubernetes resource holding sensitive credentials, injected into Pods as environment variables |
| **APM** | Application Performance Monitoring — real-time visibility into request latency and error rates |

## Sprint Goal

> Achieve **production-grade scalability** by introducing **async Message Brokers** between Microservices, running all services in a **Kubernetes Cluster** with **HPA auto-scaling**, securing configuration via **ConfigMaps and Secrets**, and confirming performance with **APM**.

## Deliverables Checklist

- [ ] `T01` — Async Messaging (RabbitMQ / Kafka / SQS) between Microservices
- [ ] `T02` — Optimized Docker images for all Microservices
- [ ] `T03` — Kubernetes Cluster provisioned on Cloud
- [ ] `T04` — Kubernetes Manifests (YAML or Helm Charts) for all services
- [ ] `T05` — HPA Autoscaling configured per Microservice
- [ ] `T06` — ConfigMaps and Secrets managing all configuration
- [ ] `T07` — APM monitoring for performance and health

## Architectural Constraints (Fase 4)

- All inter-Microservice communication for **domain Events** must be **async** via the Message Broker
- Synchronous HTTP calls are still allowed for direct Player-facing requests (API Gateway → Microservice)
- All Kubernetes manifests must be **version-controlled** — no `kubectl apply` from the console
- Secrets must **never** appear in `ConfigMaps`, Git history, or pipeline logs

## Acceptance Criteria (Sprint Gate)

```gherkin
Feature: Sprint 3 Release Gate

  Scenario: System handles high Player concurrency
    Given 500 concurrent Players are making requests
    When the load causes CPU to exceed 70%
    Then HPA scales the affected Pods from 1 to 3 replicas automatically
    And no Player requests fail with 5xx errors during scale-out
    And Pods scale back down after load subsides

  Scenario: Payment notification survives Payments Microservice restart
    Given the Payments Microservice emits a "TransactionCompleted" Event
    And the Payments Microservice restarts immediately after
    When the notification consumer recovers
    Then the Event is still in the Queue
    And the Player notification is eventually sent (at-least-once delivery)

  Scenario: No secret is visible in any manifest or pipeline log
    Given all ConfigMaps and Secrets are audited
    When Git history and pipeline logs are scanned for secrets
    Then no credentials, connection strings, or API keys are found in plain text
```

## Linked Tasks

| ID | Title | File |
|----|-------|------|
| T01 | Async Messaging | [tasks/T01-async-messaging.md](tasks/T01-async-messaging.md) |
| T02 | Docker Optimization | [tasks/T02-docker-optimization.md](tasks/T02-docker-optimization.md) |
| T03 | Kubernetes Cluster | [tasks/T03-kubernetes-cluster.md](tasks/T03-kubernetes-cluster.md) |
| T04 | Kubernetes Manifests | [tasks/T04-kubernetes-manifests.md](tasks/T04-kubernetes-manifests.md) |
| T05 | HPA Autoscaling | [tasks/T05-hpa-autoscaling.md](tasks/T05-hpa-autoscaling.md) |
| T06 | ConfigMaps and Secrets | [tasks/T06-configmaps-secrets.md](tasks/T06-configmaps-secrets.md) |
| T07 | APM Monitoring | [tasks/T07-apm-monitoring.md](tasks/T07-apm-monitoring.md) |
