# Step 6: Kubernetes Manifests & HPA - Implementation Summary

## Overview

Implemented production-grade Kubernetes manifests with Horizontal Pod Autoscaling (HPA), high availability, and security best practices for deploying TheThroneOfGames in Kubernetes clusters.

## Deliverables

### 1. Core Infrastructure Files

#### 01-namespace.yaml
- **Purpose**: Logical isolation for all application resources
- **Namespace**: `thethroneofgames`
- **Labels**: App, environment, managed-by tags for resource organization
- **Benefits**: Resource quotas, network policies, RBAC isolation

#### 02-configmap.yaml
Three ConfigMaps:
1. **api-config** - Non-sensitive API settings
   - ASPNETCORE environment variables
   - RabbitMQ configuration (host, port, exchange names)
   - Database connection settings
   - JWT issuer/audience configuration
   - Resilience policy thresholds
   - Prometheus metrics configuration

2. **rabbitmq-config** - RabbitMQ server configuration
   - Default user/pass
   - Memory limits (2GB disk free, 60% watermark)
   - Channel limits (2048)
   - Heartbeat (60s)
   - Definitions file location

3. **prometheus-config** - Prometheus scrape configuration
   - Global scrape interval: 15s
   - Evaluation interval: 15s
   - Jobs for API, RabbitMQ, Kubernetes API servers
   - Dynamic service discovery via `kubernetes_sd_configs`

#### 03-secrets.yaml
Five Kubernetes Secrets:
1. **app-secrets** - Core application secrets
   - SQL Server credentials
   - RabbitMQ credentials
   - JWT secret keys
   - Email/API passwords

2. **rabbitmq-admin-secret** - RabbitMQ management UI credentials

3. **docker-registry-credentials** - Private Docker registry auth

4. **tls-certificate** - HTTPS/TLS certificates

All base64 encoded, managed by external secret operator in production.

#### 04-services.yaml
Seven Kubernetes Services:

| Service | Type | Purpose | Ports |
|---------|------|---------|-------|
| api-service | LoadBalancer | External API access | 80→5000, 9090 |
| api-internal-service | ClusterIP (headless) | Internal communication | 5000 |
| rabbitmq-service | ClusterIP | Message broker access | 5672, 15672, 25672 |
| rabbitmq-headless-service | ClusterIP (headless) | StatefulSet clustering | Same as above |
| mssql-service | ClusterIP | Database access | 1433 |
| prometheus-service | ClusterIP | Metrics scraping | 9090 |
| grafana-service | LoadBalancer | Grafana UI access | 3000 |

**Headless Services**: Used by StatefulSets for stable DNS names (e.g., `rabbitmq-0.rabbitmq-headless-service`, `rabbitmq-1`, `rabbitmq-2`)

### 2. Workload Deployments

#### 05-deployment-api.yaml
**API Deployment** - Production-ready ASP.NET Core deployment

**Key Features**:
- **Replicas**: 3 initial (scaled by HPA 3-10)
- **Strategy**: RollingUpdate with maxSurge=1, maxUnavailable=0 (zero-downtime)
- **Pod Anti-Affinity**: Spreads replicas across nodes for high availability

**Health Checks**:
- **Liveness**: `/health` endpoint, 30s initial, 10s period, 3 failures = restart
- **Readiness**: `/health/ready` endpoint, 10s initial, 5s period, 3 failures = remove from LB
- **Startup**: `/health/startup` endpoint, 30 retries * 5s = 150s max startup time

**Resource Management**:
- **Requests**: 100m CPU, 256Mi memory
- **Limits**: 500m CPU, 512Mi memory

**Init Containers**:
- Wait for MSSQL (nc check port 1433)
- Wait for RabbitMQ (nc check port 5672)
- Prevents race conditions during startup

**Security**:
- Non-root user (UID 1000)
- Read-only root filesystem option
- Dropped ALL capabilities
- Pod security context with fsGroup=1000

**Lifecycle**:
- 15s preStop delay for graceful connection draining
- 30s terminationGracePeriodSeconds
- ImagePullPolicy: Always

#### 06-statefulset-databases.yaml
Two StatefulSets for data persistence:

**RabbitMQ StatefulSet**:
- **Replicas**: 3 (high availability cluster)
- **Service**: rabbitmq-headless-service for stable DNS
- **Storage**: 10Gi PersistentVolume per pod
- **Clustering**: Automatic via RABBITMQ_NODENAME env var
- **Health Checks**: rabbitmq-diagnostics ping/check_running
- **Resources**: 250m CPU request, 512Mi memory; 1000m/1Gi limits
- **Pod Anti-Affinity**: Required (different nodes for each pod)
- **Graceful Shutdown**: Stops app, resets, quits cleanly
- **User**: Non-root (UID 999)

**MSSQL StatefulSet**:
- **Replicas**: 1 (single instance; use Always On AG for HA)
- **Storage**: 20Gi data + 5Gi logs volumes
- **Edition**: Developer (change to Standard/Enterprise for production)
- **Health Checks**: sqlcmd SELECT 1 query
- **Resources**: 500m CPU request, 1Gi memory; 2000m/2Gi limits
- **Graceful Shutdown**: 30s termination period for clean shutdown

### 3. Auto-Scaling & High Availability

#### 07-hpa-pdb.yaml

**Horizontal Pod Autoscaler (HPA)** - v2 API

**Scaling Metrics**:
1. **CPU Utilization**: Scale when >70% average
2. **Memory Utilization**: Scale when >80% average
3. **Custom Metric** (Polly integration): RabbitMQ queue length
   - Scales 1 replica per 100 pending messages
   - Via `rabbitmq_queue_messages_ready` metric

**Scaling Behavior**:

*Scale Up (aggressive)*:
- Stabilization window: 60s
- Policies: 100% increase OR +3 pods per minute
- Select: Max (scale faster if needed)

*Scale Down (conservative)*:
- Stabilization window: 300s (5 minutes)
- Policies: 50% decrease OR -1 pod per minute
- Select: Min (scale slower to prevent thrashing)

**Vertical Pod Autoscaler (VPA)**:
- Auto-updates resource requests based on actual usage
- Min: 50m CPU, 128Mi memory
- Max: 2000m CPU, 2Gi memory
- Update mode: Auto

**Pod Disruption Budget (PDB)**:
- API: minAvailable=2 (keep 2+ pods during cluster updates)
- RabbitMQ: minAvailable=2 (maintain cluster quorum)
- Prevents eviction of too many pods simultaneously

**Priority Classes**:
- `api-high-priority` (value=1000): Reduce eviction risk
- `system-cluster-critical` (value=2000000000): Critical system pods

### 4. Security & Access Control

#### 08-rbac.yaml

**Service Accounts**:
- `api-service-account`: For API pods
- `rabbitmq-service-account`: For RabbitMQ StatefulSet
- `mssql-service-account`: For MSSQL pods
- `prometheus-service-account`: For monitoring

**Namespace Roles & Bindings**:

*API Role*:
- Get/list/watch pods and logs
- Get/list configmaps, secrets
- Get/list/watch services, endpoints

*RabbitMQ Role*:
- Get/list/watch pods
- Get/list/watch endpoints (for headless service discovery)

*Prometheus ClusterRole*:
- Get/list/watch nodes, pods, services, endpoints
- Get/list/watch deployments, statefulsets, ingresses
- Read metrics across cluster

All restricted to minimal required permissions (least privilege).

### 5. Monitoring Stack

#### 09-monitoring-deployments.yaml

**Prometheus Deployment**:
- **Image**: prom/prometheus:v2.48.0
- **Storage**: 20Gi PersistentVolume
- **Retention**: 30 days of metrics
- **Scrape Interval**: 15s
- **Resources**: 100m/256Mi requests, 500m/1Gi limits
- **Service**: ClusterIP on port 9090

**Grafana Deployment**:
- **Image**: grafana/grafana:10.2.0
- **Storage**: 5Gi for dashboards/plugins
- **Default User**: admin (password from secrets)
- **Sign-up**: Disabled
- **Resources**: 100m/128Mi requests, 500m/512Mi limits
- **Datasource**: Prometheus auto-provisioned

**Persistent Volume Claims**:
- prometheus-pvc: 20Gi
- grafana-pvc: 5Gi
- StorageClass: standard

### 6. Networking & Traffic Management

#### 10-network-policies-ingress.yaml

**Network Policies** (Zero-Trust by default):

1. **Default Deny**: Block all ingress/egress
2. **API Ingress**: Allow from LoadBalancer, Prometheus scraping
3. **API→RabbitMQ**: Allow API pods to connect AMQP (5672)
4. **API→MSSQL**: Allow API pods to connect SQL (1433)
5. **RabbitMQ Clustering**: Allow inter-pod communication (25672)
6. **Prometheus Scrape**: Allow monitoring endpoints
7. **DNS**: Allow UDP port 53 to kube-dns (critical for service discovery)

**Ingress Resources** (HTTP/HTTPS routing):

| Host | Service | Port | SSL |
|------|---------|------|-----|
| api.thethroneofgames.com | api-service | 80 | ✅ Let's Encrypt |
| grafana.thethroneofgames.com | grafana-service | 3000 | ✅ Let's Encrypt |
| prometheus.thethroneofgames.com | prometheus-service | 9090 | ✅ Let's Encrypt |

**Ingress Features**:
- nginx ingress controller
- rate-limiting: 100 req/s
- SSL/TLS termination
- cert-manager auto-renewal
- Path-based routing

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Kubernetes Cluster                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Ingress Controllers (nginx)                                │
│  ├─ api.thethroneofgames.com → api-service (LB:80)         │
│  ├─ grafana.thethroneofgames.com → grafana-service (LB:3000)
│  └─ prometheus.thethroneofgames.com → prometheus-service    │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│  Namespace: thethroneofgames                                │
│                                                              │
│  API Deployment (Replicas: 3-10 via HPA)                   │
│  ├─ Pod 1: api-container                                    │
│  ├─ Pod 2: api-container                                    │
│  └─ Pod 3+: api-container (scaled by HPA)                   │
│     └─ Metrics: CPU (70%), Memory (80%), RabbitMQ queue     │
│                                                              │
│  RabbitMQ StatefulSet (Replicas: 3)                        │
│  ├─ rabbitmq-0: Master                                      │
│  ├─ rabbitmq-1: Node                                        │
│  └─ rabbitmq-2: Node                                        │
│     └─ Clustering via DNS (rabbitmq-headless-service)       │
│     └─ Storage: 10Gi PersistentVolume each                  │
│                                                              │
│  MSSQL StatefulSet (Replicas: 1)                           │
│  └─ mssql-0: Database Server                                │
│     └─ Storage: 20Gi (data) + 5Gi (logs)                    │
│                                                              │
│  Monitoring Stack                                           │
│  ├─ Prometheus (metrics scraping)                           │
│  │  └─ scrapes /metrics from API every 15s                  │
│  └─ Grafana (visualization)                                 │
│     └─ datasource: prometheus-service:9090                  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## HPA Autoscaling Flow

```
Monitor Metrics (15s interval)
  ↓
Check: CPU >70% OR Memory >80% OR Queue >100?
  ↓
Yes → Scale Up
  ├─ Add 2 pods (100% increase) or +3 pods max per minute
  └─ Wait 60s (stabilization) before next scale-up
  ↓
No → Check Scale Down (5min stabilization)
  ├─ Remove 1 pod (50% decrease) or -1 pod max per minute
  └─ Wait 5 minutes before scale-down
  ↓
Repeat
```

## Deployment Order

1. **01-namespace.yaml** - Create namespace
2. **02-configmap.yaml** - Create configuration
3. **03-secrets.yaml** - Create secrets (use external secret operator)
4. **08-rbac.yaml** - Create service accounts and roles
5. **04-services.yaml** - Create services
6. **06-statefulset-databases.yaml** - Start RabbitMQ and MSSQL
7. **05-deployment-api.yaml** - Deploy API
8. **09-monitoring-deployments.yaml** - Deploy Prometheus/Grafana
9. **07-hpa-pdb.yaml** - Enable autoscaling
10. **10-network-policies-ingress.yaml** - Apply network policies and ingress

## Deployment Commands

```bash
# Apply all manifests in order
kubectl apply -f 01-namespace.yaml
kubectl apply -f 02-configmap.yaml
kubectl apply -f 03-secrets.yaml
kubectl apply -f 08-rbac.yaml
kubectl apply -f 04-services.yaml
kubectl apply -f 06-statefulset-databases.yaml
kubectl apply -f 05-deployment-api.yaml
kubectl apply -f 09-monitoring-deployments.yaml
kubectl apply -f 07-hpa-pdb.yaml
kubectl apply -f 10-network-policies-ingress.yaml

# Or apply all at once:
kubectl apply -f kubernetes/

# Verify deployment
kubectl get all -n thethroneofgames
kubectl get pods -n thethroneofgames -w
kubectl get hpa -n thethroneofgames -w

# Check logs
kubectl logs -n thethroneofgames -f deployment/api-deployment -c api

# Port forward for local testing
kubectl port-forward -n thethroneofgames svc/api-service 5000:80
kubectl port-forward -n thethroneofgames svc/grafana-service 3000:3000
```

## High Availability Features

✅ **API**:
- 3-10 replicas (HPA scaled)
- Pod anti-affinity (spread across nodes)
- PDB minAvailable=2 (quorum protection)
- LoadBalancer service with health checks
- Rolling updates (maxUnavailable=0)

✅ **RabbitMQ**:
- 3-node cluster (quorum)
- StatefulSet with stable DNS
- PDB minAvailable=2 (cluster stability)
- Persistent storage (durable queues)
- Auto-recovery on node failure

✅ **Database**:
- Persistent storage (durable)
- 30s graceful shutdown
- Health check monitoring
- 20Gi data + 5Gi logs separation

✅ **Monitoring**:
- Prometheus retention: 30 days
- Grafana dashboard persistence
- Metrics-based alerting support

## Scaling Scenarios

**Scenario 1: Traffic Spike**
```
Queue Length: 5000 messages
Required Replicas: 5000 / 100 = 50 (but capped at maxReplicas=10)
Actual Replicas: 10
Result: Queue processes faster, RabbitMQ load decreases
```

**Scenario 2: CPU Spike**
```
Average CPU: 85% (>70% threshold)
Current Replicas: 3
Action: Scale to 5 (100% increase)
Wait: 60s (stabilization)
Result: Load balanced, CPU normalized
```

**Scenario 3: Scale Down (Low Traffic)**
```
CPU: 40%, Memory: 50%, Queue: 50 messages
Wait: 300s (5 minute stabilization to avoid thrashing)
Action: Scale down to 2 (but minReplicas=3, so stays at 3)
Result: Resource efficiency maintained, minimum HA preserved
```

## Monitoring & Alerting

**Key Metrics to Monitor**:
- `api_http_requests_total`: Total API requests
- `api_http_request_duration_seconds`: API latency
- `api_events_published_total`: Event publishing rate
- `api_rabbitmq_queue_length`: Pending messages
- `rabbitmq_queue_messages_ready`: RabbitMQ queue depth
- `container_memory_usage_bytes`: Memory consumption
- `container_cpu_usage_seconds_total`: CPU time

**Grafana Dashboards to Create**:
1. API Performance (latency, throughput, errors)
2. RabbitMQ Cluster Health (queue depth, connections, consumers)
3. Resource Utilization (CPU, memory, disk)
4. Kubernetes Events (pod restarts, node events)
5. HPA Status (replicas, metrics, scaling events)

## Security Considerations

✅ **Network Security**:
- Zero-trust network policies
- Headless services for internal communication
- Ingress with TLS termination

✅ **Authentication**:
- RBAC with service accounts
- Minimal required permissions
- Secret management (external operator recommended)

✅ **Pod Security**:
- Non-root users
- Read-only root filesystem
- Dropped capabilities
- Resource limits (DoS prevention)

✅ **Secrets Management**:
- External secret operator (HashiCorp Vault)
- Secret rotation support
- Encrypted secrets in etcd (ETCD encryption)

## Next Steps

1. **Step 7**: Create Helm chart for templated deployments
2. **Step 8**: GitHub Actions CI/CD pipeline for image building and deployment
3. **Step 10**: Create custom metrics and Grafana dashboards for business KPIs
4. **Production**: Add cert-manager for automated TLS, implement backup strategy

## References

- [Kubernetes Deployments](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [Horizontal Pod Autoscaler](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)
- [Network Policies](https://kubernetes.io/docs/concepts/services-networking/network-policies/)
- [RBAC Authorization](https://kubernetes.io/docs/reference/access-authn-authz/rbac/)
- [StatefulSets](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/)
- [Prometheus on Kubernetes](https://prometheus.io/docs/prometheus/latest/getting_started/)
