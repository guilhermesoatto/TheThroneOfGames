# T03 — Kubernetes Cluster

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P0 (all other Sprint 3 tasks depend on it)

## Context

The FCG Microservices must run in a **Kubernetes Cluster** on a Cloud Provider to support automatic scaling, self-healing, and structured networking. The Cluster is the execution environment for all Pods from Sprint 3 onward.

## User Story

> As a **platform engineer**,  
> I want a managed Kubernetes Cluster on the Cloud,  
> so that all FCG Microservices run in an orchestrated, self-healing, auto-scalable environment.

## Gherkin Scenarios

```gherkin
Feature: Kubernetes Cluster — Cloud Provisioning

  Background:
    Given a Cloud Provider account is available (AWS, Azure, GCP, or other)

  Scenario: Cluster is provisioned and reachable
    Given the Cluster is created via IaC (Terraform, eksctl, az aks create, etc.)
    When "kubectl get nodes" is executed
    Then at least 2 worker nodes are in "Ready" state
    And the Kubernetes version is 1.28 or higher

  Scenario: FCG namespace is isolated from the default namespace
    Given the Cluster is running
    When a Pod is deployed to the "fcg" namespace
    Then it is isolated from Pods in "default" and "kube-system"
    And resource quotas are applied to the "fcg" namespace

  Scenario: Cluster auto-repairs a failed node
    Given 2 worker nodes are running
    When one node becomes NotReady
    Then the Cluster scheduler moves Pods from the failed node to the healthy node
    And all Microservices remain accessible within 60 seconds

  Scenario: Kubectl access is role-based
    Given a developer has read-only RBAC permissions
    When they attempt "kubectl delete pod <name>"
    Then the operation is denied with a permissions error
    And no Pod is deleted
```

## Acceptance Criteria

- [ ] Kubernetes Cluster provisioned on Cloud (EKS, AKS, GKE, OKE, or equivalent)
- [ ] Minimum 2 worker nodes (for HA and HPA headroom)
- [ ] Kubernetes version ≥ 1.28
- [ ] `fcg` namespace created with resource quotas (CPU, memory limits)
- [ ] Cluster provisioned via IaC (eksctl, Terraform, az CLI, or CloudFormation) — not console
- [ ] `kubectl` context configured and working in CI/CD pipeline
- [ ] RBAC configured: developer role (read), operator role (deploy), no wildcard cluster-admin for app accounts
- [ ] Metrics Server installed (required for HPA)
- [ ] Cluster costs estimated and documented (teardown after video recording)

## Best Practices

- Use managed node groups (AWS Managed Node Groups / AKS node pools) — less operational overhead
- Enable cluster-level logging (CloudWatch Logs / Azure Monitor) from day one
- Apply `PodDisruptionBudget` to each Microservice Deployment to prevent all Pods going down during node drain
- Tag all cloud resources: `project=fcg`, `env=prod`, `phase=4`

## Suggested Options

| Cloud | Service | Free Tier |
|-------|---------|-----------|
| AWS | EKS | Free tier on worker EC2 (t3.medium) — cluster endpoint costs ~$0.10/hr |
| Azure | AKS | Free cluster management, pay only for VMs (student subscription) |
| GCP | GKE Autopilot | Pay-per-Pod, free tier available |
| Oracle | OKE | Always-free tier available |

## Technical Notes

```bash
# AWS EKS via eksctl
eksctl create cluster \
  --name fcg-cluster \
  --region us-east-1 \
  --nodegroup-name fcg-nodes \
  --node-type t3.medium \
  --nodes 2 \
  --nodes-min 1 \
  --nodes-max 5 \
  --managed

# Install Metrics Server (required for HPA)
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Create FCG namespace with resource quota
kubectl create namespace fcg
kubectl apply -f k8s/namespace-quota.yaml
```

## Dependencies

- T02 — Optimized Docker images should exist before deploying to the Cluster

## Definition of Done

- [ ] `kubectl get nodes` shows ≥ 2 Ready nodes
- [ ] Metrics Server running (`kubectl top nodes` works)
- [ ] `fcg` namespace created with quota
- [ ] Cluster IaC code committed to repository
