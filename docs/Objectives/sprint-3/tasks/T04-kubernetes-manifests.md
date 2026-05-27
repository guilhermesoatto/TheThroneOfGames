# T04 — Kubernetes Manifests (Deployments, Services, Ingress)

**Sprint:** 3 | **Phase:** Fase 4 | **Priority:** P0 (deploys all Microservices to Cluster)

## Context

All FCG Microservices must be declared as **Deployments** in Kubernetes so the Cluster can manage their lifecycle. Services expose Pods internally, and an Ingress Controller routes external Player traffic through the Cluster. All manifests are version-controlled — no manual `kubectl apply` from the console.

## User Story

> As a **DevOps Engineer**,  
> I want every FCG Microservice declared as a Kubernetes Deployment,  
> so that the Cluster manages availability, rolling updates, and self-healing automatically.

## Gherkin Scenarios

```gherkin
Feature: Kubernetes Manifests — Microservice Deployments

  Background:
    Given a Kubernetes Cluster is running (T03)
    And optimized Docker images are in the Container Registry (T02)

  Scenario: Users Microservice is deployed and reachable
    Given the Users Deployment manifest is applied
    When "kubectl get pods -n fcg" is executed
    Then at least one Users Pod is in "Running" state
    And the Pod is accessible via its Kubernetes Service

  Scenario: Rolling update deploys new image with zero downtime
    Given the Users Microservice has 2 running Pods
    When the CD Pipeline deploys a new image version
    Then Kubernetes performs a rolling update
    And at least 1 Pod serves traffic during the update
    And no Player request returns 503 during the update

  Scenario: Crashed Pod is automatically replaced
    Given a Users Pod is running
    When the Pod crashes (OOMKilled or exits with non-zero code)
    Then Kubernetes creates a replacement Pod within 30 seconds
    And the Service continues to route traffic to healthy Pods

  Scenario: Resource limits prevent noisy-neighbor issues
    Given all Pods have CPU and memory limits defined
    When the Payments Pod consumes all its allocated memory
    Then the Payments Pod is OOMKilled and restarted
    And the Users and Games Pods are unaffected

  Scenario: All manifests are applied from Git via CD Pipeline
    Given a developer merges manifest changes to "main"
    When the CD Pipeline runs
    Then "kubectl apply -f k8s/" deploys the updated manifests
    And the Cluster state matches the Git state
```

## Acceptance Criteria

- [ ] Kubernetes `Deployment` manifest for each of 3 Microservices (Users, Games, Payments)
- [ ] Kubernetes `Service` manifest for each Microservice (ClusterIP for internal, LoadBalancer/NodePort for external if no Ingress)
- [ ] `Ingress` resource configured with an Ingress Controller (nginx, Traefik, or AWS ALB Ingress)
- [ ] `replicas: 2` minimum for each Deployment
- [ ] Resource requests AND limits set for every container
- [ ] `rollingUpdate` strategy: `maxUnavailable: 0`, `maxSurge: 1`
- [ ] `readinessProbe` and `livenessProbe` configured on each container
- [ ] **`securityContext`** on every container: `readOnlyRootFilesystem: true`, `allowPrivilegeEscalation: false`, `capabilities.drop: [ALL]`
- [ ] **Dedicated `ServiceAccount`** per Microservice with `automountServiceAccountToken: false`
- [ ] **`NetworkPolicy`** per Microservice: allow ingress only from API Gateway Pod, allow egress only to its own database and the Message Broker
- [ ] `kubectl apply --dry-run=server -f k8s/` runs in CD Pipeline before live apply
- [ ] All manifests in `k8s/` folder, committed to repository
- [ ] CD Pipeline applies manifests via `kubectl apply -f k8s/` (or `helm upgrade`)

## Best Practices

- Set `requests` lower than `limits` to allow burst but prevent OOM at rest
- Use `readinessProbe` to prevent traffic being sent to not-yet-ready Pods
- Set `terminationGracePeriodSeconds: 30` to allow in-flight requests to complete
- Use `imagePullPolicy: Always` for `latest` tag; use `IfNotPresent` for SHA-tagged images
- **Never use the `default` ServiceAccount** — create a dedicated SA per Microservice with minimum RBAC permissions
- Set `readOnlyRootFilesystem: true` — if the app needs to write temp files, mount a specific `emptyDir` volume instead of making the whole FS writable

## Technical Notes

```yaml
# users-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: users
  namespace: fcg
spec:
  replicas: 2
  selector:
    matchLabels:
      app: users
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 0
      maxSurge: 1
  template:
    metadata:
      labels:
        app: users
    spec:
      containers:
        - name: users
          image: <registry>/fcg-users:latest
          ports:
            - containerPort: 8080
          resources:
            requests:
              cpu: "100m"
              memory: "128Mi"
            limits:
              cpu: "500m"
              memory: "256Mi"
          readinessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 15
            periodSeconds: 20
          envFrom:
            - configMapRef:
                name: users-config
            - secretRef:
                name: users-secrets
      securityContext:          # Pod-level: all containers inherit
        runAsNonRoot: true
      serviceAccountName: users-sa   # Dedicated SA, not default
      automountServiceAccountToken: false
```

```yaml
# users-securitycontext (container-level)
          securityContext:
            readOnlyRootFilesystem: true
            allowPrivilegeEscalation: false
            capabilities:
              drop: ["ALL"]
```

```yaml
# users-networkpolicy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: users-netpol
  namespace: fcg
spec:
  podSelector:
    matchLabels:
      app: users
  policyTypes: [Ingress, Egress]
  ingress:
    - from:
        - podSelector: { matchLabels: { role: api-gateway } }
      ports: [{ port: 8080, protocol: TCP }]
  egress:
    - to:
        - podSelector: { matchLabels: { role: postgres } }
      ports: [{ port: 5432, protocol: TCP }]
    - to:   # Allow DNS
        - namespaceSelector: {}
          podSelector: { matchLabels: { k8s-app: kube-dns } }
      ports: [{ port: 53, protocol: UDP }]
```

## Dependencies

- T02 — Optimized images in Container Registry
- T03 — Cluster must be running
- T06 — ConfigMaps and Secrets must exist before Pods start

## Definition of Done

- [ ] All 3 Deployments running with ≥ 2 Pods each
- [ ] Rolling update tested: zero 503s during image update (verify with `hey` or `k6`)
- [ ] All manifests committed to repository under `k8s/`
