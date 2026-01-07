# Kubernetes Quick Reference - The Throne of Games

## 🚀 Quick Start

### One-Command Deploy
```bash
cd kubernetes/ && bash deploy.sh
```

### Verify Deployment
```bash
bash kubernetes/verify.sh
```

### Cleanup All Resources
```bash
bash kubernetes/cleanup.sh
```

## 📋 Essential Commands

### Deployment Management
```bash
# Deploy using Kustomize
kubectl apply -k kubernetes/

# View all resources
kubectl get all -n thethroneofgames

# Watch deployments
kubectl get deployment -n thethroneofgames -w

# Check pod status
kubectl get pods -n thethroneofgames -o wide

# Get events (recent issues)
kubectl get events -n thethroneofgames --sort-by='.lastTimestamp'
```

### Logs & Debugging
```bash
# View logs for service
kubectl logs -n thethroneofgames -l app=usuarios-api -f

# Last 100 lines
kubectl logs -n thethroneofgames <pod-name> --tail=100

# Execute command in pod
kubectl exec -it -n thethroneofgames <pod-name> -- /bin/bash

# Describe pod (detailed info)
kubectl describe pod -n thethroneofgames <pod-name>

# Port forward to service
kubectl port-forward -n thethroneofgames svc/usuarios-api-service 8001:80
```

### Scaling & Autoscaling
```bash
# Manual scaling
kubectl scale deployment usuarios-api --replicas=5 -n thethroneofgames

# View HPA status
kubectl get hpa -n thethroneofgames

# View HPA details
kubectl describe hpa usuarios-api-hpa -n thethroneofgames

# Manual HPA trigger (based on metrics)
kubectl top pods -n thethroneofgames
```

### Configuration Management
```bash
# Edit ConfigMap
kubectl edit configmap usuarios-api-config -n thethroneofgames

# View ConfigMap
kubectl get configmap usuarios-api-config -n thethroneofgames -o yaml

# Restart deployment (apply config changes)
kubectl rollout restart deployment/usuarios-api -n thethroneofgames

# View rollout history
kubectl rollout history deployment/usuarios-api -n thethroneofgames

# Rollback to previous version
kubectl rollout undo deployment/usuarios-api -n thethroneofgames
```

### Resource Monitoring
```bash
# Pod resource usage
kubectl top pods -n thethroneofgames

# Node resource usage
kubectl top nodes

# Resource requests/limits
kubectl describe nodes

# HPA metrics
kubectl get hpa -n thethroneofgames -o wide
```

## 🔗 Service Access

### Port Forwarding
```bash
# Usuarios API
kubectl port-forward -n thethroneofgames svc/usuarios-api-service 8001:80

# Catalogo API
kubectl port-forward -n thethroneofgames svc/catalogo-api-service 8002:80

# Vendas API
kubectl port-forward -n thethroneofgames svc/vendas-api-service 8003:80

# Database (SQL Server)
kubectl port-forward -n thethroneofgames svc/mssql-service 1433:1433

# RabbitMQ Management UI
kubectl port-forward -n thethroneofgames svc/rabbitmq-service 15672:15672

# Prometheus
kubectl port-forward -n thethroneofgames-monitoring svc/prometheus-service 9090:9090
```

### DNS Names (from inside cluster)
```
mssql-service:1433
rabbitmq-service:5672
usuarios-api-service:80
catalogo-api-service:80
vendas-api-service:80
prometheus-service:9090 (in thethroneofgames-monitoring namespace)
```

### Ingress URLs (external)
```
http://localhost/api/usuarios
http://localhost/api/catalogo
http://localhost/api/vendas
http://localhost/swagger
```

## 📊 Monitoring & Observability

### Prometheus Queries
```yaml
# API pod count
count(container_last_seen{pod=~"usuarios-api.*"})

# CPU usage
rate(container_cpu_usage_seconds_total[5m])

# Memory usage
container_memory_usage_bytes

# Network I/O
rate(container_network_receive_bytes_total[5m])

# HTTP requests
rate(http_requests_total[5m])
```

### Check Service Health
```bash
# Test API endpoint
curl -s http://localhost:8001/swagger | head -20

# Test database
kubectl exec -it -n thethroneofgames mssql-0 -- \
  sqlcmd -S localhost -U sa -Q "SELECT 1"

# Test RabbitMQ
kubectl exec -it -n thethroneofgames rabbitmq-0 -- \
  rabbitmqctl status
```

## 🐛 Troubleshooting

### Pod Debugging
```bash
# Check why pod isn't starting
kubectl describe pod -n thethroneofgames <pod-name>
kubectl logs -n thethroneofgames <pod-name>

# Run debug pod
kubectl run -it --rm debug --image=busybox -- sh

# Test DNS resolution
kubectl run -it --rm debug --image=busybox -- \
  nslookup mssql-service.thethroneofgames.svc.cluster.local
```

### Database Issues
```bash
# Check database logs
kubectl logs -n thethroneofgames mssql-0

# Connect to database
kubectl exec -it -n thethroneofgames mssql-0 -- \
  sqlcmd -S localhost -U sa

# Check PVC status
kubectl get pvc -n thethroneofgames
kubectl describe pvc mssql-data -n thethroneofgames
```

### Network Issues
```bash
# Check service endpoints
kubectl get endpoints -n thethroneofgames

# Check ingress configuration
kubectl get ingress -n thethroneofgames
kubectl describe ingress thethroneofgames-ingress -n thethroneofgames

# Test pod-to-pod connectivity
kubectl exec -it -n thethroneofgames <pod-name> -- \
  curl -v http://usuarios-api-service:80/swagger
```

### Scaling Issues
```bash
# Check HPA status
kubectl get hpa -n thethroneofgames

# View HPA events
kubectl describe hpa usuarios-api-hpa -n thethroneofgames

# Check metrics availability
kubectl get --raw /apis/metrics.k8s.io/v1beta1/pods

# Manual scale if HPA fails
kubectl scale deployment usuarios-api --replicas=3 -n thethroneofgames
```

## 📁 File Locations

### Kubernetes Manifests
```
kubernetes/
├── namespaces/namespace.yaml           - Namespaces
├── database/mssql.yaml                 - SQL Server
├── database/secrets.yaml               - DB secrets & RabbitMQ config
├── rabbitmq/                           - RabbitMQ configuration
├── usuarios-api/usuarios-api.yaml      - Usuarios microservice
├── catalogo-api/catalogo-api.yaml      - Catalogo microservice
├── vendas-api/vendas-api.yaml          - Vendas microservice
├── ingress/ingress.yaml                - Ingress & network policies
├── monitoring/prometheus.yaml          - Prometheus monitoring
├── kustomization.yaml                  - Kustomize config
├── deploy.sh                           - Deploy script
├── verify.sh                           - Verification script
├── cleanup.sh                          - Cleanup script
├── KUBERNETES_SETUP.md                 - Full documentation
├── KUBERNETES_DEPLOYMENT_REPORT.md     - Deployment report
└── QUICK_REFERENCE.md                  - This file
```

## 🔐 Security Commands

### Manage Secrets
```bash
# View secrets (base64 encoded)
kubectl get secret -n thethroneofgames jwt-secret -o yaml

# Decode secret value
kubectl get secret -n thethroneofgames jwt-secret -o jsonpath='{.data.key}' | base64 -d

# Update secret
kubectl create secret generic jwt-secret \
  --from-literal=key=new-jwt-key \
  -n thethroneofgames --dry-run=client -o yaml | kubectl apply -f -

# View RBAC
kubectl get rolebindings -n thethroneofgames
kubectl describe clusterrole prometheus -n thethroneofgames-monitoring
```

## 📈 Performance Tuning

### Resource Requests/Limits (Current)
```yaml
Microservices:
  Request: 250m CPU, 256Mi Memory
  Limit: 500m CPU, 512Mi Memory
  
Database:
  Request: 2000m CPU, 2Gi Memory
  Limit: 4000m CPU, 4Gi Memory
  
RabbitMQ:
  Request: 1000m CPU, 512Mi Memory
  Limit: 2000m CPU, 1Gi Memory
```

### Scaling Limits
```yaml
Minimum Replicas: 3
Maximum Replicas: 10
CPU Threshold: 70%
Memory Threshold: 80%
```

### Update Resources
```bash
# Edit deployment
kubectl edit deployment usuarios-api -n thethroneofgames

# Or patch deployment
kubectl patch deployment usuarios-api -n thethroneofgames -p \
  '{"spec":{"template":{"spec":{"containers":[{"name":"usuarios-api","resources":{"limits":{"memory":"1Gi"}}}]}}}}'
```

## 🔄 Updates & Rollbacks

### Update Image
```bash
# Set new image version
kubectl set image deployment/usuarios-api \
  usuarios-api=usuarios-api:v2 \
  -n thethroneofgames

# Monitor rollout
kubectl rollout status deployment/usuarios-api -n thethroneofgames

# Watch deployment during update
kubectl get deployment usuarios-api -n thethroneofgames -w
```

### Rollback
```bash
# Rollback to previous version
kubectl rollout undo deployment/usuarios-api -n thethroneofgames

# Rollback to specific revision
kubectl rollout undo deployment/usuarios-api --to-revision=2 -n thethroneofgames

# View rollout history
kubectl rollout history deployment/usuarios-api -n thethroneofgames
```

## 🧹 Cleanup Operations

### Remove Specific Resources
```bash
# Delete pod (will be recreated by deployment)
kubectl delete pod -n thethroneofgames <pod-name>

# Delete deployment (and all its pods)
kubectl delete deployment usuarios-api -n thethroneofgames

# Delete service
kubectl delete svc usuarios-api-service -n thethroneofgames

# Delete configmap
kubectl delete configmap usuarios-api-config -n thethroneofgames

# Delete PVC (WARNING: deletes data!)
kubectl delete pvc mssql-data -n thethroneofgames
```

### Remove All
```bash
# Delete entire namespace (removes all resources in it)
kubectl delete namespace thethroneofgames

# Or use Kustomize
kubectl delete -k kubernetes/

# Or run cleanup script
bash kubernetes/cleanup.sh
```

## 📞 Common Scenarios

### "Pod is CrashLoopBackOff"
```bash
# Check logs
kubectl logs -n thethroneofgames <pod-name>

# Check events
kubectl describe pod -n thethroneofgames <pod-name>

# Possible solutions:
# - Check ConfigMap for wrong values
# - Check database connectivity
# - Check resource availability
# - Increase resource limits
```

### "Database connection timeout"
```bash
# Verify database pod is running
kubectl get pods -n thethroneofgames -l app=mssql

# Test connectivity
kubectl run -it --rm debug --image=busybox -- \
  nslookup mssql-service.thethroneofgames

# Check service
kubectl get svc -n thethroneofgames mssql-service

# Port-forward and test
kubectl port-forward -n thethroneofgames svc/mssql-service 1433:1433
# In another terminal: sqlcmd -S localhost -U sa
```

### "High CPU/Memory usage"
```bash
# Check current usage
kubectl top pods -n thethroneofgames

# Check HPA status
kubectl get hpa -n thethroneofgames

# Force scaling
kubectl scale deployment usuarios-api --replicas=5 -n thethroneofgames

# Monitor metrics
watch kubectl top pods -n thethroneofgames

# Adjust limits if needed
kubectl set resources deployment usuarios-api \
  --requests=cpu=500m,memory=512Mi \
  --limits=cpu=1000m,memory=1Gi \
  -n thethroneofgames
```

### "Services can't communicate"
```bash
# Check DNS
kubectl run -it --rm debug --image=busybox -- \
  nslookup <service-name>.thethroneofgames

# Check endpoints
kubectl get endpoints -n thethroneofgames

# Check network policies
kubectl get networkpolicies -n thethroneofgames

# Test connectivity between pods
kubectl exec -it -n thethroneofgames <pod-name> -- \
  curl -v http://<service>:80
```

## 📚 Additional Resources

- **Full Docs**: Read `KUBERNETES_SETUP.md`
- **Deployment Report**: Read `KUBERNETES_DEPLOYMENT_REPORT.md`
- **Kubernetes Official**: https://kubernetes.io/docs/
- **NGINX Ingress**: https://kubernetes.github.io/ingress-nginx/
- **Prometheus**: https://prometheus.io/

---

**Version**: 1.0
**Last Updated**: 2024
**Quick Reference for Kubernetes Deployment - TheThroneOfGames**
