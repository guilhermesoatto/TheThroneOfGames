# Kubernetes Orchestration for The Throne of Games

## Overview

This directory contains all Kubernetes manifests for deploying The Throne of Games microservices platform in a Kubernetes cluster. The architecture follows best practices for microservices orchestration, including service discovery, persistent storage, monitoring, and auto-scaling.

## Directory Structure

```
kubernetes/
├── namespaces/              # Kubernetes namespace definitions
│   └── namespace.yaml       # Production and monitoring namespaces
├── database/                # SQL Server StatefulSet
│   ├── mssql.yaml          # SQL Server deployment with persistence
│   └── secrets.yaml        # Database credentials and RabbitMQ config
├── rabbitmq/               # RabbitMQ message broker
│   ├── configmap.yaml      # RabbitMQ configuration
│   ├── pvc.yaml           # Persistent volume claim for RabbitMQ
│   ├── statefulset.yaml   # RabbitMQ StatefulSet deployment
│   └── service.yaml       # RabbitMQ services (internal + external)
├── usuarios-api/           # Usuarios microservice
│   └── usuarios-api.yaml  # Deployment, Service, HPA, ConfigMap
├── catalogo-api/           # Catalogo microservice
│   └── catalogo-api.yaml  # Deployment, Service, HPA, ConfigMap
├── vendas-api/             # Vendas microservice
│   └── vendas-api.yaml    # Deployment, Service, HPA, ConfigMap
├── ingress/                # Ingress controller configuration
│   ├── ingress.yaml       # Ingress rules and network policies
│   └── secrets.yaml       # JWT secrets and TLS certificates
├── monitoring/             # Monitoring stack
│   └── prometheus.yaml    # Prometheus monitoring setup
├── kustomization.yaml      # Kustomize configuration for all resources
└── KUBERNETES_SETUP.md    # This file
```

## Prerequisites

Before deploying, ensure you have:

1. **Kubernetes Cluster** (1.20+)
   - Minikube, Docker Desktop Kubernetes, EKS, AKS, GKE, or any K8s cluster
   - kubectl configured to access your cluster

2. **Container Images**
   ```bash
   # Build and push to registry (or use local images)
   docker build -t usuarios-api:latest ./TheThroneOfGames.Usuarios.API
   docker build -t catalogo-api:latest ./TheThroneOfGames.Catalogo.API
   docker build -t vendas-api:latest ./TheThroneOfGames.Vendas.API
   ```

3. **Storage Support**
   - PersistentVolume provisioner (local-path, EBS, Azure Disk, etc.)
   - Default StorageClass or custom StorageClass configuration

4. **Ingress Controller** (optional, for external traffic)
   ```bash
   # For NGINX Ingress Controller
   helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
   helm install nginx-ingress ingress-nginx/ingress-nginx --namespace ingress-nginx --create-namespace
   ```

## Quick Start

### 1. Deploy All Resources at Once (Recommended)

```bash
# Deploy all resources using Kustomize
kubectl apply -k kubernetes/

# Verify deployments
kubectl get pods -n thethroneofgames
kubectl get services -n thethroneofgames
kubectl get ingress -n thethroneofgames
```

### 2. Deploy Resources Individually

```bash
# Create namespace first
kubectl apply -f kubernetes/namespaces/namespace.yaml

# Deploy database
kubectl apply -f kubernetes/database/mssql.yaml
kubectl apply -f kubernetes/database/secrets.yaml

# Deploy message broker
kubectl apply -f kubernetes/rabbitmq/configmap.yaml
kubectl apply -f kubernetes/rabbitmq/pvc.yaml
kubectl apply -f kubernetes/rabbitmq/statefulset.yaml
kubectl apply -f kubernetes/rabbitmq/service.yaml

# Deploy microservices
kubectl apply -f kubernetes/usuarios-api/usuarios-api.yaml
kubectl apply -f kubernetes/catalogo-api/catalogo-api.yaml
kubectl apply -f kubernetes/vendas-api/vendas-api.yaml

# Deploy ingress
kubectl apply -f kubernetes/ingress/ingress.yaml
kubectl apply -f kubernetes/ingress/secrets.yaml

# Deploy monitoring
kubectl apply -f kubernetes/monitoring/prometheus.yaml
```

### 3. Verify Deployment Status

```bash
# Check pods status
kubectl get pods -n thethroneofgames
kubectl get pods -n thethroneofgames-monitoring

# Check services
kubectl get svc -n thethroneofgames

# Check ingress
kubectl get ingress -n thethroneofgames

# Check persistent volumes
kubectl get pvc -n thethroneofgames

# View pod logs
kubectl logs -n thethroneofgames -l app=usuarios-api --tail=100 -f
kubectl logs -n thethroneofgames -l app=catalogo-api --tail=100 -f
kubectl logs -n thethroneofgames -l app=vendas-api --tail=100 -f
```

## Component Details

### 1. Namespaces

- **thethroneofgames**: Production namespace for all microservices, database, and message broker
- **thethroneofgames-monitoring**: Monitoring infrastructure (Prometheus, Grafana)

### 2. Database (SQL Server)

- **Type**: StatefulSet (stable network identity and persistent storage)
- **Image**: mcr.microsoft.com/mssql/server:2019-latest
- **Storage**: 10Gi PersistentVolume
- **Port**: 1433 (internal)
- **Access**: `mssql-service:1433` from within cluster
- **Credentials**: Configured via secrets.yaml

**Database Configuration:**
```
Server: mssql-service
Port: 1433
Database: GameStore
User: sa
Password: (from secrets)
```

### 3. Message Broker (RabbitMQ)

- **Type**: StatefulSet
- **Image**: rabbitmq:3.12-management-alpine
- **Storage**: 5Gi PersistentVolume
- **Ports**:
  - 5672: AMQP protocol (internal)
  - 15672: Management UI (external via LoadBalancer)
- **Access**: `rabbitmq-service:5672` from within cluster
- **Management UI**: `http://localhost:15672` (after port-forward)

**RabbitMQ Configuration:**
- Pre-configured exchanges and queues for event bus
- Dead-letter queues for failed messages
- Management credentials: guest/guest (change in production)

### 4. Usuarios API

- **Replicas**: 3 (auto-scales 3-10 based on CPU/Memory)
- **Image**: usuarios-api:latest
- **Port**: 80 (HTTP)
- **Endpoint**: `/api/usuarios`
- **Resources**:
  - Request: 250m CPU, 256Mi Memory
  - Limit: 500m CPU, 512Mi Memory
- **Health Checks**: Liveness & Readiness probes

**Features:**
- User management and authentication
- JWT token generation
- User roles and permissions
- Email activation workflow

### 5. Catalogo API

- **Replicas**: 3 (auto-scales 3-10 based on CPU/Memory)
- **Image**: catalogo-api:latest
- **Port**: 80 (HTTP)
- **Endpoint**: `/api/catalogo`
- **Resources**:
  - Request: 250m CPU, 256Mi Memory
  - Limit: 500m CPU, 512Mi Memory

**Features:**
- Game catalog management
- Game search and filtering
- Game ratings and reviews
- Category management

### 6. Vendas API

- **Replicas**: 3 (auto-scales 3-10 based on CPU/Memory)
- **Image**: vendas-api:latest
- **Port**: 80 (HTTP)
- **Endpoint**: `/api/vendas`
- **Resources**:
  - Request: 250m CPU, 256Mi Memory
  - Limit: 500m CPU, 512Mi Memory

**Features:**
- Order management
- Shopping cart functionality
- Payment processing
- Order tracking

### 7. Ingress Controller

**Routing Rules:**
- `/api/usuarios` → usuarios-api-service:80
- `/api/catalogo` → catalogo-api-service:80
- `/api/vendas` → vendas-api-service:80
- `/swagger` → usuarios-api-service:80
- `/rabbitmq` → rabbitmq-service:15672

**Features:**
- CORS enabled for all origins
- Request body size limit: 50MB
- SSL/TLS support (with cert-manager)
- Path-based routing

### 8. Monitoring (Prometheus)

- **Image**: prom/prometheus:latest
- **Port**: 9090
- **Access**: `http://localhost:9090` (after port-forward)
- **Storage**: 7-day retention
- **Scrape Interval**: 15 seconds

**Monitored Targets:**
- Kubernetes API server
- Kubernetes nodes
- Kubernetes pods
- Usuarios API
- Catalogo API
- Vendas API

## Port Forwarding for Local Development

### Access Services Locally

```bash
# SQL Server
kubectl port-forward -n thethroneofgames svc/mssql-service 1433:1433

# RabbitMQ Management UI
kubectl port-forward -n thethroneofgames svc/rabbitmq-service 15672:15672

# Prometheus
kubectl port-forward -n thethroneofgames-monitoring svc/prometheus-service 9090:9090

# Usuarios API
kubectl port-forward -n thethroneofgames svc/usuarios-api-service 8001:80

# Catalogo API
kubectl port-forward -n thethroneofgames svc/catalogo-api-service 8002:80

# Vendas API
kubectl port-forward -n thethroneofgames svc/vendas-api-service 8003:80
```

## Accessing Services

### From Outside Cluster (via Ingress)
```
http://localhost/api/usuarios
http://localhost/api/catalogo
http://localhost/api/vendas
http://localhost/rabbitmq
```

### From Inside Cluster (DNS names)
```
mssql-service:1433
rabbitmq-service:5672
usuarios-api-service:80
catalogo-api-service:80
vendas-api-service:80
prometheus-service:9090
```

## Configuration Management

### Environment Variables (ConfigMaps)

Each microservice has a ConfigMap with environment variables:
- ASPNETCORE_ENVIRONMENT: Production
- ConnectionStrings__DefaultConnection
- Jwt settings
- EventBus (RabbitMQ) settings
- Logging configuration

### Secrets

Located in `kubernetes/ingress/secrets.yaml`:
- JWT signing key
- TLS certificates
- Database credentials

### Updating Configuration

```bash
# Update ConfigMap
kubectl edit configmap usuarios-api-config -n thethroneofgames

# Restart pods to apply changes
kubectl rollout restart deployment/usuarios-api -n thethroneofgames
```

## Scaling

### Manual Scaling

```bash
# Scale deployment
kubectl scale deployment usuarios-api --replicas=5 -n thethroneofgames

# Check current replicas
kubectl get deployment -n thethroneofgames
```

### Auto-Scaling

Each microservice has a HorizontalPodAutoscaler configured:
- **Min Replicas**: 3
- **Max Replicas**: 10
- **CPU Target**: 70% utilization
- **Memory Target**: 80% utilization

The cluster automatically scales pods based on metrics:

```bash
# View HPA status
kubectl get hpa -n thethroneofgames
kubectl describe hpa usuarios-api-hpa -n thethroneofgames
```

## Health Checks

### Liveness Probe
- Checks if pod is alive
- Restarts pod if unhealthy
- HTTP GET /swagger on port 80
- Initial delay: 30 seconds
- Interval: 10 seconds
- Failure threshold: 3

### Readiness Probe
- Checks if pod is ready to receive traffic
- Removes from service if not ready
- HTTP GET /swagger on port 80
- Initial delay: 15 seconds
- Interval: 5 seconds
- Failure threshold: 3

## Persistent Storage

### Database Volume
- Size: 10Gi
- StorageClass: default
- Mount Path: /var/opt/mssql
- Access Mode: ReadWriteOnce

### RabbitMQ Volume
- Size: 5Gi
- StorageClass: default
- Mount Path: /var/lib/rabbitmq
- Access Mode: ReadWriteOnce

## Monitoring & Debugging

### View Logs

```bash
# View logs for specific pod
kubectl logs -n thethroneofgames <pod-name>

# Stream logs
kubectl logs -n thethroneofgames <pod-name> -f

# View logs for all pods with label
kubectl logs -n thethroneofgames -l app=usuarios-api
```

### Describe Resources

```bash
# Describe pod
kubectl describe pod -n thethroneofgames <pod-name>

# Describe deployment
kubectl describe deployment usuarios-api -n thethroneofgames

# Describe service
kubectl describe svc usuarios-api-service -n thethroneofgames
```

### Execute Commands in Pod

```bash
# Execute bash in pod
kubectl exec -it -n thethroneofgames <pod-name> -- /bin/bash

# Run command
kubectl exec -n thethroneofgames <pod-name> -- dotnet --version
```

## Troubleshooting

### Pod not starting

```bash
# Check events
kubectl describe pod -n thethroneofgames <pod-name>

# Check logs
kubectl logs -n thethroneofgames <pod-name>

# Check resource availability
kubectl top nodes
kubectl top pods -n thethroneofgames
```

### Database connection issues

```bash
# Test connectivity
kubectl exec -it -n thethroneofgames <pod-name> -- \
  sqlcmd -S mssql-service -U sa -P <password> -Q "SELECT 1"

# Check DNS
kubectl run -it --rm debug --image=busybox:1.28 -- nslookup mssql-service.thethroneofgames.svc.cluster.local
```

### RabbitMQ issues

```bash
# Check RabbitMQ logs
kubectl logs -n thethroneofgames -l app=rabbitmq

# Access RabbitMQ CLI
kubectl exec -it -n thethroneofgames rabbitmq-0 -- \
  rabbitmqctl status
```

## Updates & Rollback

### Update Image

```bash
# Set new image
kubectl set image deployment/usuarios-api \
  usuarios-api=usuarios-api:v2 -n thethroneofgames

# Watch rollout
kubectl rollout status deployment/usuarios-api -n thethroneofgames
```

### Rollback Deployment

```bash
# View rollout history
kubectl rollout history deployment/usuarios-api -n thethroneofgames

# Rollback to previous version
kubectl rollout undo deployment/usuarios-api -n thethroneofgames

# Rollback to specific revision
kubectl rollout undo deployment/usuarios-api --to-revision=2 -n thethroneofgames
```

## Cleanup

### Remove All Resources

```bash
# Using Kustomize
kubectl delete -k kubernetes/

# Or individual components
kubectl delete namespace thethroneofgames
kubectl delete namespace thethroneofgames-monitoring
```

## Production Considerations

### Security

1. **Network Policies**: Restrict pod-to-pod communication
2. **RBAC**: Implement role-based access control
3. **Pod Security**: Run containers as non-root users
4. **Secrets**: Use external secret management (Vault, AWS Secrets Manager)
5. **TLS/mTLS**: Enable HTTPS for all communications

### Backup & Disaster Recovery

1. **Database Backups**: Regular SQL Server backups to external storage
2. **Persistent Volumes**: Backup PV snapshots
3. **Configuration**: Store ConfigMaps and Secrets in version control (encrypted)
4. **Cluster State**: Use backup tools (Velero, etcd backup)

### Performance & Optimization

1. **Resource Requests/Limits**: Monitor and adjust as needed
2. **HPA Metrics**: Configure custom metrics for scaling
3. **Cache**: Implement Redis for application caching
4. **CDN**: Use CDN for static assets
5. **Database Indexing**: Optimize SQL queries and indexes

### Monitoring & Logging

1. **Centralized Logging**: ELK Stack, Splunk, or CloudWatch
2. **APM**: Application Performance Monitoring (Datadog, New Relic)
3. **Alerting**: Alert Manager with Prometheus
4. **Dashboards**: Grafana dashboards for visualization

## Advanced Topics

### Service Mesh (Optional)

For advanced traffic management and security:
```bash
# Install Istio
istio/bin/istioctl install --set profile=demo -y

# Enable sidecar injection
kubectl label namespace thethroneofgames istio-injection=enabled
```

### GitOps Deployment

Implement continuous deployment with ArgoCD:
```bash
# Install ArgoCD
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
```

### Infrastructure as Code (Optional)

Consider using Terraform or Pulumi for infrastructure management:
- Dynamic resource provisioning
- Multi-environment management
- State tracking and rollback

## Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Kustomize Documentation](https://kubectl.docs.kubernetes.io/guides/introduction/kustomize/)
- [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/)
- [Prometheus Operator](https://github.com/prometheus-operator/prometheus-operator)
- [RabbitMQ on Kubernetes](https://www.rabbitmq.com/kubernetes/operator/operator-overview.html)

## Support & Contribution

For issues or questions, please open an issue in the repository or contact the development team.

---

**Last Updated**: 2024
**Version**: 1.0
**Maintainer**: TheThroneOfGames Development Team
