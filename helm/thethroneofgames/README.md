# TheThrone ofGames Helm Chart

A comprehensive Helm chart for deploying the TheThroneOfGames microservices platform on Kubernetes with high availability, autoscaling, and monitoring.

## Prerequisites

- Kubernetes 1.20+
- Helm 3.0+
- kubectl configured to access your cluster

## Quick Start

### 1. Add the Helm Repository (Optional - for public charts)

```bash
helm repo add thethroneofgames https://charts.thethroneofgames.com
helm repo update
```

### 2. Install the Chart

**Development:**
```bash
helm install thethroneofgames ./thethroneofgames -f values-dev.yaml -n thethroneofgames --create-namespace
```

**Staging:**
```bash
helm install thethroneofgames ./thethroneofgames -f values-staging.yaml -n thethroneofgames --create-namespace
```

**Production:**
```bash
helm install thethroneofgames ./thethroneofgames -f values-prod.yaml -n thethroneofgames --create-namespace
```

### 3. Verify Installation

```bash
kubectl get pods -n thethroneofgames -w
kubectl get svc -n thethroneofgames
kubectl get ingress -n thethroneofgames
```

### 4. Access Services

**API:**
```bash
# Development
kubectl port-forward -n thethroneofgames svc/thethroneofgames-api-service 5000:80

# Production
https://api.thethroneofgames.com
```

**Grafana:**
```bash
# Development
kubectl port-forward -n thethroneofgames svc/grafana-service 3000:3000

# Production
https://grafana.thethroneofgames.com
```

**Prometheus:**
```bash
# Development
kubectl port-forward -n thethroneofgames svc/prometheus-service 9090:9090

# Production
https://prometheus.thethroneofgames.com
```

## Chart Structure

```
thethroneofgames/
├── Chart.yaml                 # Chart metadata
├── values.yaml               # Default values
├── values-dev.yaml          # Development overrides
├── values-staging.yaml       # Staging overrides
├── values-prod.yaml         # Production overrides
├── README.md                # This file
└── templates/
    ├── _helpers.tpl         # Template helpers
    ├── namespace.yaml       # Kubernetes namespace
    ├── deployment-api.yaml  # API deployment
    ├── configmap.yaml       # Configuration
    ├── services.yaml        # Service definitions
    ├── ingress.yaml         # Ingress routes
    ├── hpa-pdb.yaml        # Autoscaling & disruption budgets
    └── serviceaccount.yaml  # RBAC service accounts
```

## Configuration

### Core Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `global.environment` | `production` | Deployment environment |
| `global.domain` | `thethroneofgames.com` | DNS domain |
| `namespace.create` | `true` | Create namespace |
| `namespace.name` | `thethroneofgames` | Namespace name |

### API Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `api.enabled` | `true` | Enable API deployment |
| `api.replicas` | `3` | Number of replicas |
| `api.image.repository` | `thethroneofgames/api` | Docker image |
| `api.image.tag` | `latest` | Image tag |
| `api.resources.requests.cpu` | `100m` | CPU request |
| `api.resources.limits.cpu` | `500m` | CPU limit |
| `api.autoscaling.enabled` | `true` | Enable HPA |
| `api.autoscaling.minReplicas` | `3` | Minimum pods |
| `api.autoscaling.maxReplicas` | `10` | Maximum pods |

### RabbitMQ Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `rabbitmq.enabled` | `true` | Enable RabbitMQ |
| `rabbitmq.replicas` | `3` | Cluster nodes |
| `rabbitmq.storage.size` | `10Gi` | Storage size |
| `rabbitmq.clustering.enabled` | `true` | Enable clustering |

### MSSQL Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `mssql.enabled` | `true` | Enable MSSQL |
| `mssql.storage.dataSize` | `20Gi` | Data storage |
| `mssql.storage.logsSize` | `5Gi` | Logs storage |
| `mssql.edition` | `Developer` | SQL Edition |

### Prometheus Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `prometheus.enabled` | `true` | Enable Prometheus |
| `prometheus.storage.size` | `20Gi` | Storage size |
| `prometheus.storage.retention` | `30d` | Retention period |
| `prometheus.scrapeInterval` | `15s` | Scrape interval |

### Grafana Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `grafana.enabled` | `true` | Enable Grafana |
| `grafana.storage.size` | `5Gi` | Storage size |
| `grafana.admin.user` | `admin` | Admin username |

## Customization

### Override Values

```bash
# Install with custom values
helm install thethroneofgames ./thethroneofgames \
  --values values-prod.yaml \
  --set api.replicas=5 \
  --set api.image.tag=v1.2.3 \
  -n thethroneofgames
```

### Create Custom Values File

```bash
# Copy and modify
cp values.yaml my-values.yaml
helm install thethroneofgames ./thethroneofgames -f my-values.yaml
```

## Upgrading

### Update Existing Release

```bash
helm upgrade thethroneofgames ./thethroneofgames -f values-prod.yaml
```

### Rollback to Previous Version

```bash
helm rollback thethroneofgames
```

### View Release History

```bash
helm history thethroneofgames
```

## Monitoring & Debugging

### Check Deployment Status

```bash
kubectl get all -n thethroneofgames
kubectl describe deployment api-deployment -n thethroneofgames
kubectl describe statefulset rabbitmq -n thethroneofgames
kubectl describe statefulset mssql -n thethroneofgames
```

### View Logs

```bash
# API logs
kubectl logs -n thethroneofgames -f deployment/api-deployment -c api

# RabbitMQ logs
kubectl logs -n thethroneofgames -f statefulset/rabbitmq

# MSSQL logs
kubectl logs -n thethroneofgames -f statefulset/mssql
```

### Monitor Autoscaling

```bash
kubectl get hpa -n thethroneofgames -w
kubectl describe hpa thethroneofgames-api-hpa -n thethroneofgames
```

### Access Pod Shell

```bash
kubectl exec -it -n thethroneofgames pod/api-deployment-xxxxx -- /bin/bash
```

## High Availability Features

✅ **Pod Anti-Affinity**: Spreads replicas across nodes
✅ **Pod Disruption Budgets**: Protects quorum during updates
✅ **Health Checks**: Liveness, readiness, startup probes
✅ **Horizontal Scaling**: CPU/memory/custom metric autoscaling
✅ **Rolling Updates**: Zero-downtime deployments
✅ **RabbitMQ Clustering**: 3-node cluster with persistent storage
✅ **Database Storage**: Persistent volumes for data durability
✅ **Network Policies**: Zero-trust security

## Security Features

✅ **Non-Root Users**: Containers run as unprivileged users
✅ **Capability Dropping**: ALL capabilities dropped
✅ **RBAC**: Service accounts with least privilege
✅ **Network Policies**: Restrict pod-to-pod communication
✅ **Secrets Management**: External secret operator support
✅ **TLS/HTTPS**: Automatic certificate management with cert-manager
✅ **Image Pull Secrets**: Private Docker registry support

## Troubleshooting

### Pod Fails to Start

```bash
# Check events
kubectl describe pod pod-name -n thethroneofgames

# Check logs
kubectl logs pod-name -n thethroneofgames
```

### Persistent Volume Not Mounting

```bash
# Check PVC status
kubectl get pvc -n thethroneofgames

# Check PV status
kubectl get pv

# Describe PVC
kubectl describe pvc pvc-name -n thethroneofgames
```

### HPA Not Scaling

```bash
# Check HPA status
kubectl describe hpa api-hpa -n thethroneofgames

# Check metrics server
kubectl get deployment metrics-server -n kube-system
```

### Network Connectivity Issues

```bash
# Test pod-to-pod connectivity
kubectl run -it --rm debug --image=busybox -- sh
# Inside pod: nc -zv rabbitmq-service 5672
```

## Uninstalling

```bash
# Remove release
helm uninstall thethroneofgames -n thethroneofgames

# Delete namespace
kubectl delete namespace thethroneofgames
```

## Contributing

See [CONTRIBUTING.md](../../../CONTRIBUTING.md)

## License

MIT License - See [LICENSE](../../../LICENSE)

## Support

- GitHub Issues: https://github.com/guilhermesoatto/TheThroneOfGames/issues
- Documentation: https://github.com/guilhermesoatto/TheThroneOfGames/wiki
