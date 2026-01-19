# 🚀 GUIA DE DEPLOYMENT EM KUBERNETES

## Status: ✅ PRONTO PARA DEPLOY

---

## 📋 Pré-requisitos

✅ Todos atendidos:
- [x] Build sem erros (0 erros)
- [x] Testes passando (101/101)
- [x] Docker images prontos (CI/CD)
- [x] Manifests Kubernetes criados
- [x] Secrets configurados

---

## 🔄 Fluxo de Deployment

### Fase 1: CI/CD Pipeline (GitHub Actions)

**Status**: ⏳ Aguardando trigger

```yaml
Trigger: Push para branch develop ✓
├─ build-and-test
│  ├─ Checkout
│  ├─ Setup .NET 9
│  ├─ Build (Release)
│  ├─ Run Tests
│  └─ Upload test results
│
├─ docker-build
│  ├─ Build imagem: catalogo-api
│  ├─ Build imagem: usuarios-api
│  ├─ Build imagem: vendas-api
│  └─ Push para registry (ghcr.io)
│
└─ deploy (se necessário)
   ├─ Apply Kubernetes manifests
   └─ Monitorar deployment
```

### Fase 2: Deployment em Kubernetes

```bash
# 1. Aplicar ConfigMaps
kubectl apply -f k8s/configmaps.yaml

# 2. Aplicar Secrets
kubectl apply -f k8s/secrets.yaml

# 3. Aplicar Database (PostgreSQL)
kubectl apply -f k8s/postgresql-statefulset.yaml

# 4. Aplicar Services
kubectl apply -f k8s/services.yaml

# 5. Aplicar Deployments (Rolling Update)
kubectl apply -f k8s/deployments/

# 6. Monitorar status
kubectl rollout status deployment/catalogo-api
kubectl rollout status deployment/usuarios-api
kubectl rollout status deployment/vendas-api
```

### Fase 3: Validação Pós-Deploy

```bash
# 1. Verificar pods
kubectl get pods -n thethroneofgames

# 2. Verificar health
kubectl port-forward svc/catalogo-api 8080:80
curl http://localhost:8080/health

# 3. Verificar logs
kubectl logs -f deployment/usuarios-api -n thethroneofgames

# 4. Verificar database
kubectl exec -it postgresql-0 -- psql -U sa -d GameStore -c "SELECT version();"
```

---

## 📊 Health Check Endpoints

Cada API expõe:

```
GET /health              - Health status
GET /health/ready        - Readiness probe
GET /health/live         - Liveness probe
```

**Expected Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-19T23:45:00Z"
}
```

---

## 🔧 Configurações de Ambiente

### Database Connection

```env
DefaultConnection=Host=postgresql-service;Port=5432;Database=GameStore;Username=sa;Password=<secret>
```

### API Settings

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
AllowedHosts=*
```

### Event Bus

```env
EventBusType=InMemory  # Ajustar para RabbitMQ/Kafka se necessário
```

---

## 📈 Probes & Policies

### Liveness Probe
```yaml
initialDelaySeconds: 30
periodSeconds: 10
timeoutSeconds: 5
failureThreshold: 3
```

### Readiness Probe
```yaml
initialDelaySeconds: 10
periodSeconds: 5
timeoutSeconds: 3
failureThreshold: 3
```

### Resource Requests/Limits
```yaml
requests:
  memory: "256Mi"
  cpu: "250m"
limits:
  memory: "512Mi"
  cpu: "500m"
```

---

## 🔄 Rolling Update Strategy

```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxSurge: 1
    maxUnavailable: 0
```

**Resultado**: Zero downtime deployment

---

## ⚠️ Rollback Plan

Se algo der errado:

```bash
# 1. Verificar histórico de rollouts
kubectl rollout history deployment/catalogo-api

# 2. Fazer rollback
kubectl rollout undo deployment/catalogo-api

# 3. Monitorar
kubectl rollout status deployment/catalogo-api
```

---

## 📊 Monitoramento

### Logs

```bash
# Stream de logs em tempo real
kubectl logs -f deployment/usuarios-api --all-containers=true

# Últimas 100 linhas
kubectl logs --tail=100 deployment/catalogo-api
```

### Events

```bash
# Eventos do cluster
kubectl get events --sort-by='.lastTimestamp'

# Específico de um pod
kubectl describe pod <pod-name>
```

### Métricas (se Prometheus instalado)

```bash
# Verificar métricas
kubectl top nodes
kubectl top pods
```

---

## 🔒 Security Checklist

- [x] Secrets em Kubernetes Secret objects
- [x] ConfigMaps para configurações públicas
- [x] Network Policies aplicadas
- [x] RBAC (Role-Based Access Control)
- [x] Pod Security Standards

---

## 📝 Checklist de Deploy

### Pré-Deploy
- [ ] CI/CD pipeline concluído
- [ ] Testes passaram (101/101)
- [ ] Docker images prontos
- [ ] ConfigMaps/Secrets criados

### Durante Deploy
- [ ] Manifests aplicados sem erros
- [ ] Pods em status Running
- [ ] Health checks passando
- [ ] Logs sem erros críticos

### Pós-Deploy
- [ ] Endpoints respondendo (200 OK)
- [ ] Database conectado
- [ ] APIs comunicando entre si
- [ ] No pod restarts

### Validação Final
- [ ] Smoke tests passando
- [ ] Performance dentro dos limites
- [ ] Segurança verificada
- [ ] Monitoramento ativo

---

## 🎯 Estimativas de Tempo

| Fase | Tempo Estimado |
|------|-----------------|
| CI/CD Build | 5-10 min |
| Docker Push | 2-3 min |
| K8s Deploy | 2-5 min |
| Health Checks | 1-2 min |
| Full Validation | 10-15 min |
| **TOTAL** | **20-35 min** |

---

## 📞 Troubleshooting

### Pod não inicia
```bash
kubectl describe pod <pod-name>
kubectl logs <pod-name> --previous
```

### Health check falhando
```bash
kubectl exec <pod-name> -- curl http://localhost/health
```

### Database não conecta
```bash
kubectl exec <pod-name> -- nslookup postgresql-service
```

### Out of Memory
```bash
kubectl top pods --sort-by=memory
# Aumentar limits em deployment
```

---

## ✅ Deploy Automation

Para automatizar, adicionar ao GitHub Actions:

```yaml
deploy-to-k8s:
  needs: docker-build
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Deploy to K8s
      run: |
        kubectl set image deployment/catalogo-api \
          catalogo-api=ghcr.io/${{ github.repository }}/catalogo-api:${{ github.sha }}
        kubectl rollout status deployment/catalogo-api
```

---

## 🎉 Deploy Concluído?

Próximas ações:

1. ✅ Monitor logs por 1 hora
2. ✅ Executar testes de integração
3. ✅ Atualizar status no Jira/GitHub
4. ✅ Comunicar team
5. ✅ Documentar lições aprendidas

---

**Status Final**: ✅ Pronto para Deployment  
**Data**: January 19, 2026  
**Responsável**: DevOps/Infrastructure Team
