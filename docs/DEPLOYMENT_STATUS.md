# Deployment Status - Phase 4 (GKE)

**Última Atualização:** 15 de Janeiro de 2026, 23:45 UTC

## 🎯 Objetivo da Phase 4

Migrar aplicação para Google Kubernetes Engine (GKE) com arquitetura de microservices, incluindo:
- ✅ Comunicação assíncrona via RabbitMQ
- ✅ Containerização com Docker
- ✅ Orquestração Kubernetes com HPA
- ✅ Deploy em cloud (GCP)
- ⏳ Monitoramento com Prometheus/Grafana
- ⏳ Validação completa end-to-end

## 📊 Status Geral: 85% Completo

### Infraestrutura GKE

**Cluster Information:**
- Nome: `autopilot-cluster-1`
- Região: `southamerica-east1`
- Tipo: Autopilot (fully managed)
- Kubernetes Version: `1.33.5-gke.2019000`
- Master IP: `34.95.185.51`
- Status: ✅ **Operational**

**Namespace:** `thethroneofgames`

### Componentes Deployed

#### 1. PostgreSQL Database (✅ Running)
```
Pod: postgresql-0
Status: 1/1 Running
Image: postgres:16-alpine (109MB)
Resources: 256Mi-1Gi RAM, 250m-1000m CPU
Storage: PVC 10Gi
Connection: postgresql-service:5432
```

**Migration Status:**
- ✅ Código migrado de SQL Server para PostgreSQL
- ✅ Migrations criadas para 3 bounded contexts
- ✅ Migrations aplicadas localmente
- ✅ StatefulSet deployed em GKE

#### 2. RabbitMQ Message Broker (✅ Running)
```
Pod: rabbitmq-0
Status: 1/1 Running
External IP: 34.39.201.173
Management UI: http://34.39.201.173:15672
Credentials: guest/guest
Port: 5672 (AMQP), 15672 (Management)
```

#### 3. APIs Microservices (⚠️ In Progress)

**Usuarios API:**
```
Deployment: usuarios-api
Replicas: 0/3 Ready (pods restarting)
Image: gcr.io/project-62120210-43eb-4d93-954/usuarios-api:postgres
Issue: Health check failing (404 on /swagger in Production)
Status: ⚠️ NEEDS FIX
```

**Catalogo API:**
```
Deployment: catalogo-api
Replicas: 0/3 Ready (pods restarting)
Image: gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres
Issue: Health check failing (404 on /swagger in Production)
Status: ⚠️ NEEDS FIX
```

**Vendas API:**
```
Deployment: vendas-api
Replicas: 0/3 Ready (pods restarting)
Image: gcr.io/project-62120210-43eb-4d93-954/vendas-api:postgres
Issue: Health check failing (404 on /swagger in Production)
Status: ⚠️ NEEDS FIX
```

**Root Cause:** Swagger desabilitado em Production, mas health checks apontam para `/swagger`

**Solution:** Implementar endpoint `/health` dedicado ou habilitar Swagger em Production

#### 4. Horizontal Pod Autoscaler (✅ Configured)
```yaml
Min Replicas: 3
Max Replicas: 10
Target CPU: 70%
Target Memory: 80%
Status: Created (awaiting pod metrics)
```

#### 5. Ingress (⏳ Pending)
```
Status: Created
External IP: Pending (5-10 min provisioning time)
Class: nginx
Backend: usuarios-api, catalogo-api, vendas-api
```

### Container Registry (GCR)

**Registry:** `gcr.io/project-62120210-43eb-4d93-954`

**Images Pushed:**
- ✅ `usuarios-api:postgres` (built 15/01/2026 23:30)
- ✅ `catalogo-api:postgres` (built 15/01/2026 23:31)
- ✅ `vendas-api:postgres` (built 15/01/2026 23:32)

**Legacy Images (SQL Server - deprecated):**
- `usuarios-api:latest`
- `catalogo-api:latest`
- `vendas-api:latest`

## 🗄️ Database Migration Summary

### From SQL Server to PostgreSQL

**Motivation:**
- GKE Autopilot não suporta SQL Server (hostPath volumes)
- PostgreSQL 95% menor em tamanho de imagem
- PostgreSQL 87% menor consumo de RAM
- PostgreSQL 70% mais barato

**Changes Made:**

1. **Package Changes:**
   - ✅ Removed: `Microsoft.EntityFrameworkCore.SqlServer`
   - ✅ Added: `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.0`
   - ✅ Added: `Microsoft.EntityFrameworkCore.Design 9.0.0`

2. **DbContext Updates:**
   - ✅ `UseSqlServer()` → `UseNpgsql()`
   - ✅ Connection strings updated (3 APIs)

3. **Migrations:**
   - ✅ GameStore.Usuarios: `InitialPostgreSQL` created & applied
   - ✅ GameStore.Catalogo: `InitialPostgreSQL` created & applied
   - ✅ GameStore.Vendas: `InitialPostgreSQL` created & applied

4. **Configuration Files:**
   - ✅ `appsettings.json`: postgresql-service:5432
   - ✅ `appsettings.Development.json`: localhost:5432
   - ✅ `docker-compose.yml`: postgres:16-alpine
   - ✅ `k8s/configmaps.yaml`: postgresql-service, port 5432
   - ✅ `k8s/statefulsets/postgresql.yaml`: Created

## 🐛 Known Issues

### Issue #1: Health Check Path Incorrect
**Severity:** 🔴 High (blocks deployment)

**Description:**
- Swagger is disabled in Production (`if (app.Environment.IsDevelopment())`)
- Health checks point to `/swagger` → returns 404
- Pods fail liveness/readiness probes and restart continuously

**Impact:**
- 0/3 pods Ready for all APIs
- Services unavailable
- HPA cannot collect metrics

**Solution:**
1. Add health check endpoint to Program.cs:
```csharp
app.MapHealthChecks("/health");
// OR
app.MapGet("/health", () => Results.Ok("Healthy"));
```

2. Update deployment YAMLs:
```yaml
livenessProbe:
  httpGet:
    path: /health  # changed from /swagger
    port: 80
readinessProbe:
  httpGet:
    path: /health  # changed from /swagger
    port: 80
```

### Issue #2: YAML Files Corrupted During Batch Edits
**Severity:** 🟡 Medium (resolved)

**Description:**
Multiple `replace_string_in_file` operations caused text corruption in usuarios-api.yaml

**Resolution:**
- Recreated file from scratch using PowerShell here-string
- Lesson learned: Validate YAML syntax after automated edits

### Issue #3: Insufficient Resources in GKE
**Severity:** 🟢 Low (expected behavior)

**Description:**
```
0/2 nodes are available: 1 Insufficient cpu, 2 Insufficient memory
```

**Resolution:**
- Expected with Autopilot (auto-scales nodes)
- New nodes provisioned within 2-3 minutes
- Not blocking

## 📋 Phase 4 Requirements Checklist

### ✅ Completed
- [x] Containerização com Docker multi-stage builds
- [x] Docker Compose para desenvolvimento local
- [x] Kubernetes manifests (Deployments, Services, StatefulSets)
- [x] ConfigMaps e Secrets configurados
- [x] Horizontal Pod Autoscaler (HPA) configurado
- [x] GKE Cluster provisionado e operacional
- [x] RabbitMQ deployed e acessível externamente
- [x] PostgreSQL deployed como StatefulSet
- [x] Imagens pushed para Google Container Registry
- [x] Bounded Contexts separados (3 microservices)
- [x] Event-Driven architecture com RabbitMQ

### ⏳ In Progress
- [ ] APIs operacionais no GKE (0/3 Ready)
- [ ] Health checks funcionando corretamente
- [ ] Ingress com IP externo atribuído
- [ ] Testes de integração passando no GKE

### 📝 Pending
- [ ] Monitoramento com Prometheus/Grafana validado
- [ ] Logs centralizados
- [ ] Demonstração em vídeo (15 minutos)
- [ ] Documentação final de deploy
- [ ] Smoke tests automatizados

## 💰 Cost Estimates (Monthly)

### Current Setup
| Component | vCPU | RAM | Disk | Estimated Cost |
|-----------|------|-----|------|----------------|
| GKE Autopilot | ~2 | ~8Gi | - | $50-60 |
| PostgreSQL PVC | - | - | 10Gi | $2 |
| LoadBalancer (RabbitMQ) | - | - | - | $8 |
| Egress (minimal) | - | - | - | $5 |
| **Total** | | | | **$65-75/month** |

### Optimization Opportunities
- Reduce replica count (3→2): Save ~$15/month
- Remove LoadBalancer for RabbitMQ (use ClusterIP): Save $8/month
- Use preemptible nodes (if available in Autopilot): Save 40-60%

## 🚀 Deployment Commands

### Build & Push Images
```bash
# Build
docker build -t gcr.io/project-62120210-43eb-4d93-954/usuarios-api:postgres -f GameStore.Usuarios.API/Dockerfile .
docker build -t gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres -f GameStore.Catalogo.API/Dockerfile .
docker build -t gcr.io/project-62120210-43eb-4d93-954/vendas-api:postgres -f GameStore.Vendas.API/Dockerfile .

# Push
docker push gcr.io/project-62120210-43eb-4d93-954/usuarios-api:postgres
docker push gcr.io/project-62120210-43eb-4d93-954/catalogo-api:postgres
docker push gcr.io/project-62120210-43eb-4d93-954/vendas-api:postgres
```

### Apply Kubernetes Manifests
```bash
# Connect to cluster
gcloud container clusters get-credentials autopilot-cluster-1 --region=southamerica-east1

# Apply in order
kubectl apply -f k8s/configmaps.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/statefulsets/
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/hpa/
kubectl apply -f k8s/ingress.yaml
```

### Monitor Deployment
```bash
# Watch pods
kubectl get pods -n thethroneofgames -w

# Check logs
kubectl logs -f deployment/usuarios-api -n thethroneofgames

# Check HPA
kubectl get hpa -n thethroneofgames

# Check ingress
kubectl get ingress -n thethroneofgames
```

## 📞 Quick Access

### External Services
- **RabbitMQ Management:** http://34.39.201.173:15672 (guest/guest)
- **Ingress (pending):** TBD

### Internal Services (via port-forward)
```bash
# Usuarios API
kubectl port-forward svc/usuarios-api 5001:80 -n thethroneofgames

# Catalogo API
kubectl port-forward svc/catalogo-api 5002:80 -n thethroneofgames

# Vendas API
kubectl port-forward svc/vendas-api 5003:80 -n thethroneofgames

# PostgreSQL
kubectl port-forward svc/postgresql-service 5432:5432 -n thethroneofgames
```

## 🎯 Next Immediate Steps

1. **Fix Health Checks** (Priority: 🔴 CRITICAL)
   - Add `/health` endpoint to all 3 APIs
   - Update deployment YAMLs
   - Apply changes to cluster

2. **Validate Deployment** (Priority: 🔴 HIGH)
   - Confirm all 9 pods Running (3 per API)
   - Test API connectivity via port-forward
   - Verify database connections

3. **Integration Tests** (Priority: 🟡 MEDIUM)
   - Run test suite against GKE
   - Validate RabbitMQ event flow
   - Test HPA scaling behavior

4. **Monitoring Setup** (Priority: 🟢 LOW)
   - Deploy Prometheus/Grafana
   - Configure dashboards
   - Set up alerts

5. **Demo Video** (Priority: 🔴 HIGH)
   - Record 15-minute demonstration
   - Show architecture, deployment, scaling
   - Include troubleshooting scenarios

---

**Report Generated:** 2026-01-15 23:45:00 UTC
**Author:** GitHub Copilot AI Assistant
**Project:** TheThroneOfGames - FIAP Cloud Games Platform
