# T05 — HPA Autoscaling (Horizontal Pod Autoscaler)

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P0 (core requirement of Fase 4)

## Context

During peak usage (game launches, new feature releases), the number of Players spikes. The **HPA** watches CPU and memory metrics and automatically scales the Pod count for each Microservice up or down. This ensures FCG can handle high load without over-provisioning resources during quiet periods.

## User Story

> As a **platform engineer**,  
> I want each Microservice to scale automatically based on CPU and memory load,  
> so that Players never experience slowdowns during peak traffic and cloud costs stay proportional to demand.

## Gherkin Scenarios

```gherkin
Feature: HPA — Horizontal Pod Autoscaling

  Background:
    Given the Kubernetes Cluster has the Metrics Server installed
    And HPA resources are defined for all three Microservices
    And each Deployment has CPU requests defined

  Scenario: Games Microservice scales out under load
    Given the Games Deployment has 1 running Pod
    And HPA is set: minReplicas=1, maxReplicas=5, targetCPUUtilization=70%
    When a load test generates CPU usage above 70% for 60 seconds
    Then HPA increases the Games replica count to at least 2
    And new Pods reach "Running" state within 90 seconds
    And CPU per Pod drops below 70%

  Scenario: Microservice scales back down after load subsides
    Given the Games Deployment was scaled to 4 Pods during a load spike
    When CPU drops below 50% for 5 consecutive minutes
    Then HPA reduces the replica count back to 1
    And cloud compute costs decrease accordingly

  Scenario: Scale-down does not drop below minReplicas
    Given HPA minReplicas is set to 1 for the Payments Microservice
    When there is zero load for 30 minutes
    Then the Payments Deployment keeps exactly 1 Pod running
    And it does not scale to 0

  Scenario: HPA does not exceed maxReplicas
    Given HPA maxReplicas is set to 5 for the Users Microservice
    When CPU spikes to 100% causing repeated scale-out events
    Then the Users Deployment never exceeds 5 Pods
    And HPA status shows "at maximum replicas"

  Scenario: HPA metrics are visible in the Monitoring Dashboard
    Given APM and Prometheus are collecting metrics (T07)
    When I open the Monitoring Dashboard
    Then I can see the current replica count for each Microservice
    And I can see the HPA scale-out and scale-in events over time
```

## Acceptance Criteria

- [ ] `HorizontalPodAutoscaler` manifest for each of 3 Microservices
- [ ] Settings per HPA: `minReplicas: 1`, `maxReplicas: 5`, `targetCPUUtilizationPercentage: 70`
- [ ] CPU `requests` defined on every container (required for HPA to calculate utilization)
- [ ] Scale-out verified by running a load test (`hey`, `k6`, or `wrk`) and observing `kubectl get hpa -n fcg -w`
- [ ] Scale-down verified by stopping the load test and waiting for cooldown
- [ ] HPA events visible in `kubectl describe hpa <name> -n fcg`
- [ ] HPA manifests committed to `k8s/` folder in repository

## Best Practices

- Set `stabilizationWindowSeconds` for scale-down (default 300s) to avoid flapping
- Use `behavior` block in HPA spec to control scale-up/scale-down rates independently
- Do NOT set `minReplicas: 0` unless the service is completely non-critical (cold start = Player-facing delay)
- Combine HPA with a `PodDisruptionBudget` to ensure at least 1 Pod is always available during scale-down

## Technical Notes

```yaml
# users-hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: users-hpa
  namespace: fcg
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: users
  minReplicas: 1
  maxReplicas: 5
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
    scaleUp:
      stabilizationWindowSeconds: 0
```

```bash
# Verify HPA in action
kubectl get hpa -n fcg -w

# Generate load (hey tool)
hey -z 2m -c 50 http://<ingress-url>/api/games
```

## Dependencies

- T03 — Kubernetes Cluster with Metrics Server installed
- T04 — Deployments with CPU requests defined

## Definition of Done

- [ ] `kubectl get hpa -n fcg` shows all 3 HPAs with non-zero TARGETS
- [ ] Load test screenshot shows scale-out in the Monitoring Dashboard
- [ ] HPA manifests committed to repository
