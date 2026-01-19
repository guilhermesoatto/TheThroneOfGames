# 🎯 KUBERNETES BEST PRACTICES

**Projeto:** The Throne of Games  
**Data:** 07/01/2026  
**Versão:** 1.0

---

## 📋 ÍNDICE

1. [Resource Management](#resource-management)
2. [High Availability](#high-availability)
3. [Security](#security)
4. [Monitoring & Observability](#monitoring--observability)
5. [CI/CD & GitOps](#cicd--gitops)
6. [Cost Optimization](#cost-optimization)
7. [Disaster Recovery](#disaster-recovery)
8. [Performance Tuning](#performance-tuning)

---

## 💾 RESOURCE MANAGEMENT

### Sempre Definir Requests e Limits

✅ **CORRETO:**
```yaml
resources:
  requests:
    memory: "512Mi"
    cpu: "300m"
  limits:
    memory: "2Gi"
    cpu: "1500m"
```

❌ **EVITAR:**
```yaml
# Sem definição de recursos
# Pode causar OOMKilled ou throttling inesperado
```

### Requests vs Limits

- **Requests**: Recursos garantidos pelo scheduler
- **Limits**: Máximo que o pod pode usar

**Regra de ouro:**
```
requests = uso médio esperado
limits = picos máximos permitidos
```

### QoS Classes

```yaml
# Guaranteed (melhor QoS)
requests.memory == limits.memory
requests.cpu == limits.cpu

# Burstable (QoS médio)
requests.memory < limits.memory

# BestEffort (pior QoS, será morto primeiro)
# Sem requests nem limits
```

### Resource Quotas por Namespace

```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: compute-quota
  namespace: thethroneofgames
spec:
  hard:
    requests.cpu: "10"
    requests.memory: 20Gi
    limits.cpu: "20"
    limits.memory: 40Gi
    persistentvolumeclaims: "5"
    pods: "50"
```

---

## 🏗️ HIGH AVAILABILITY

### Multi-Replica Deployments

```yaml
# MÍNIMO para produção
replicas: 3

# Distribuir em múltiplas zonas
affinity:
  podAntiAffinity:
    preferredDuringSchedulingIgnoredDuringExecution:
    - weight: 100
      podAffinityTerm:
        labelSelector:
          matchExpressions:
          - key: app
            operator: In
            values:
            - usuarios-api
        topologyKey: topology.kubernetes.io/zone
```

### Pod Disruption Budgets

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: usuarios-api-pdb
  namespace: thethroneofgames
spec:
  minAvailable: 2  # Sempre manter 2 pods disponíveis
  selector:
    matchLabels:
      app: usuarios-api
      tier: backend
```

### Health Probes Corretas

```yaml
# Liveness: Detecta deadlocks (kill e restart)
livenessProbe:
  httpGet:
    path: /health/live
    port: 5001
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3

# Readiness: Detecta se pode receber tráfego
readinessProbe:
  httpGet:
    path: /health/ready
    port: 5001
  initialDelaySeconds: 15
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 3
```

**Importante:**
- Liveness ≠ Readiness
- Liveness muito agressivo = restart loops
- Readiness: pode ser temporariamente unhealthy

### Graceful Shutdown

```yaml
# No Deployment
terminationGracePeriodSeconds: 30

# No código .NET
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Graceful shutdown iniciado");
    // Finalizar conexões, processar fila, etc.
});
```

---

## 🔒 SECURITY

### Princípios

1. **Least Privilege**: Mínimos privilégios necessários
2. **Defense in Depth**: Múltiplas camadas de segurança
3. **Zero Trust**: Nenhuma comunicação é confiável por padrão

### Network Policies (Zero Trust)

```yaml
# 1. Negar tudo por padrão
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: deny-all
spec:
  podSelector: {}
  policyTypes:
  - Ingress
  - Egress

# 2. Permitir apenas o necessário
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-api-to-db
spec:
  podSelector:
    matchLabels:
      app: sqlserver
  policyTypes:
  - Ingress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          tier: backend
    ports:
    - protocol: TCP
      port: 1433
```

### Pod Security Standards

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: thethroneofgames
  labels:
    pod-security.kubernetes.io/enforce: restricted
    pod-security.kubernetes.io/audit: restricted
    pod-security.kubernetes.io/warn: restricted
```

### Security Context

```yaml
securityContext:
  # Pod-level
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 2000
  seccompProfile:
    type: RuntimeDefault
  
  # Container-level
  allowPrivilegeEscalation: false
  capabilities:
    drop:
    - ALL
  readOnlyRootFilesystem: true
```

### Secrets Management

**Opções (da mais simples para mais robusta):**

1. **Kubernetes Secrets (nativo)**
   ```powershell
   kubectl create secret generic app-secrets \
     --from-literal=jwt-secret=your-secret \
     --from-literal=db-password=your-password
   ```

2. **Sealed Secrets** (GitOps-friendly)
   ```powershell
   kubeseal --format yaml < secret.yaml > sealed-secret.yaml
   # Pode commitar sealed-secret.yaml no Git
   ```

3. **External Secrets Operator** (recomendado)
   - Integra com Azure Key Vault, AWS Secrets Manager, GCP Secret Manager
   - Secrets nunca ficam no Git
   ```yaml
   apiVersion: external-secrets.io/v1beta1
   kind: ExternalSecret
   metadata:
     name: app-secrets
   spec:
     secretStoreRef:
       name: azure-keyvault
       kind: SecretStore
     target:
       name: app-secrets
     data:
     - secretKey: jwt-secret
       remoteRef:
         key: jwt-secret
   ```

### RBAC (Role-Based Access Control)

```yaml
# ServiceAccount para a aplicação
apiVersion: v1
kind: ServiceAccount
metadata:
  name: usuarios-api-sa
  namespace: thethroneofgames

---
# Role com permissões mínimas
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: usuarios-api-role
rules:
- apiGroups: [""]
  resources: ["configmaps", "secrets"]
  verbs: ["get", "list", "watch"]

---
# RoleBinding
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: usuarios-api-binding
subjects:
- kind: ServiceAccount
  name: usuarios-api-sa
roleRef:
  kind: Role
  name: usuarios-api-role
  apiGroup: rbac.authorization.k8s.io
```

---

## 📊 MONITORING & OBSERVABILITY

### 4 Pilares da Observabilidade

1. **Metrics** (Prometheus)
2. **Logs** (EFK/Loki)
3. **Traces** (Jaeger/Tempo)
4. **Events** (Kubernetes Events)

### Golden Signals

Monitore estas 4 métricas:

1. **Latency**: Tempo de resposta
   ```promql
   histogram_quantile(0.95, 
     rate(http_request_duration_seconds_bucket[5m])
   )
   ```

2. **Traffic**: Requisições por segundo
   ```promql
   rate(http_requests_total[5m])
   ```

3. **Errors**: Taxa de erro
   ```promql
   rate(http_requests_total{status=~"5.."}[5m])
   / rate(http_requests_total[5m])
   ```

4. **Saturation**: Uso de recursos
   ```promql
   container_memory_working_set_bytes
   / container_spec_memory_limit_bytes
   ```

### Alertas Essenciais

```yaml
# Prometheus AlertManager
groups:
- name: thethroneofgames
  rules:
  # Pod crashlooping
  - alert: PodCrashLooping
    expr: rate(kube_pod_container_status_restarts_total[15m]) > 0
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "Pod {{ $labels.pod }} está crashlooping"

  # Alta latência
  - alert: HighLatency
    expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "P95 latency acima de 1s"

  # Alta taxa de erro
  - alert: HighErrorRate
    expr: |
      rate(http_requests_total{status=~"5.."}[5m])
      / rate(http_requests_total[5m]) > 0.05
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "Taxa de erro acima de 5%"

  # Pods não prontos
  - alert: PodsNotReady
    expr: kube_pod_status_ready{condition="false"} > 0
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "Pod {{ $labels.pod }} não está Ready"
```

### Structured Logging

```csharp
// .NET Serilog configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "UsuariosAPI")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// Log com contexto
logger.LogInformation(
    "Pedido criado: {PedidoId} - Usuario: {UsuarioId} - Valor: {Valor}",
    pedido.Id, pedido.UsuarioId, pedido.ValorTotal
);
```

---

## 🔄 CI/CD & GITOPS

### GitOps Principles

1. **Declarative**: Tudo em YAML/Helm charts
2. **Versioned**: Git como single source of truth
3. **Automated**: Deploys automáticos via Pull
4. **Auditable**: Histórico completo no Git

### ArgoCD Setup (Recomendado)

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: thethroneofgames
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/guilhermesoatto/TheThroneOfGames.git
    targetRevision: HEAD
    path: k8s
  destination:
    server: https://kubernetes.default.svc
    namespace: thethroneofgames
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
    - CreateNamespace=true
```

### Deployment Strategies

#### 1. Rolling Update (Padrão)
```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxUnavailable: 1
    maxSurge: 1
```
✅ Simples, zero downtime  
❌ Versões misturadas temporariamente

#### 2. Blue-Green (Implementado no CI/CD)
```yaml
# Blue (atual)
app: usuarios-api
version: v1.0

# Green (nova)
app: usuarios-api
version: v1.1

# Switch de tráfego
kubectl patch service usuarios-api -p '{"spec":{"selector":{"version":"v1.1"}}}'
```
✅ Rollback instantâneo, sem mistura de versões  
❌ Mais recursos (2x pods temporariamente)

#### 3. Canary (Progressivo)
```yaml
# Implementar com Istio/Linkerd
# 10% → 25% → 50% → 100%
```
✅ Menor risco, testa com usuários reais  
❌ Complexo, precisa service mesh

### Image Tagging Strategy

```yaml
# ✅ CORRETO: Multi-tag
ghcr.io/org/app:v1.2.3        # Semver
ghcr.io/org/app:abc123d       # Git SHA
ghcr.io/org/app:master        # Branch
ghcr.io/org/app:latest        # Latest (não usar em produção!)

# ❌ EVITAR: Apenas latest
ghcr.io/org/app:latest
```

---

## 💰 COST OPTIMIZATION

### Vertical Pod Autoscaler (VPA)

```yaml
apiVersion: autoscaling.k8s.io/v1
kind: VerticalPodAutoscaler
metadata:
  name: usuarios-api-vpa
spec:
  targetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: usuarios-api
  updatePolicy:
    updateMode: "Recreate"  # Ou "Initial", "Off"
```

Analisa uso real e ajusta requests/limits automaticamente.

### Cluster Autoscaler

```yaml
# Em cloud providers
# Adiciona/remove nodes baseado na demanda
```

### Karpenter (AWS) ou KEDA (Event-driven)

```yaml
# KEDA: Escala baseado em eventos
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: rabbitmq-scaler
spec:
  scaleTargetRef:
    name: pedidos-processor
  triggers:
  - type: rabbitmq
    metadata:
      queueName: pedidos
      queueLength: "10"
```

### Spot Instances / Preemptible VMs

```yaml
# Tolerations para nodes spot
tolerations:
- key: "kubernetes.azure.com/scalesetpriority"
  operator: "Equal"
  value: "spot"
  effect: "NoSchedule"

# Ou node affinity
affinity:
  nodeAffinity:
    preferredDuringSchedulingIgnoredDuringExecution:
    - weight: 1
      preference:
        matchExpressions:
        - key: karpenter.sh/capacity-type
          operator: In
          values:
          - spot
```

**Economia:** 60-80% vs On-Demand  
**Trade-off:** Pode ser interrompido a qualquer momento

---

## 🔥 DISASTER RECOVERY

### Backup Strategy (3-2-1 Rule)

- **3** cópias dos dados
- **2** tipos de mídia diferentes
- **1** cópia offsite

### Velero (Kubernetes Backup)

```powershell
# Instalar Velero
velero install --provider azure --use-volume-snapshots=true

# Backup do namespace
velero backup create thethroneofgames-backup \
  --include-namespaces thethroneofgames

# Schedule automático
velero schedule create daily-backup \
  --schedule="0 2 * * *" \
  --include-namespaces thethroneofgames

# Restore
velero restore create --from-backup thethroneofgames-backup
```

### Database Backup

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: sqlserver-backup
spec:
  schedule: "0 2 * * *"  # 2 AM daily
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: mcr.microsoft.com/mssql-tools
            command:
            - /bin/bash
            - -c
            - |
              /opt/mssql-tools/bin/sqlcmd -S sqlserver-service \
                -U sa -P $SA_PASSWORD \
                -Q "BACKUP DATABASE [PlataformaJogos] TO DISK = '/backup/db-$(date +%Y%m%d).bak'"
            volumeMounts:
            - name: backup
              mountPath: /backup
          volumes:
          - name: backup
            persistentVolumeClaim:
              claimName: backup-pvc
          restartPolicy: OnFailure
```

### RTO & RPO

**RTO (Recovery Time Objective):** Tempo máximo de indisponibilidade  
**RPO (Recovery Point Objective):** Máximo de dados que pode ser perdido

```
Nosso target:
- RTO: < 1 hora
- RPO: < 15 minutos

Estratégias:
- Backups a cada 15 min (RPO)
- Hot standby em outra região (RTO)
- Automated disaster recovery playbook
```

---

## ⚡ PERFORMANCE TUNING

### Database Connection Pooling

```csharp
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=sqlserver-service;Database=PlataformaJogos;User Id=sa;Password=...;Min Pool Size=10;Max Pool Size=100;Connection Lifetime=300;"
}
```

### Redis Caching

```yaml
# Deployment do Redis
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "1Gi"
            cpu: "500m"
```

```csharp
// .NET Configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration["Redis:ConnectionString"];
    options.InstanceName = "PlataformaJogos:";
});

// Uso
await cache.SetStringAsync($"jogo:{id}", JsonSerializer.Serialize(jogo), 
    new DistributedCacheEntryOptions {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    });
```

### HTTP Keep-Alive & Connection Reuse

```csharp
// HttpClient factory (singleton)
services.AddHttpClient("ExternalAPI", client =>
{
    client.BaseAddress = new Uri("https://api.externa.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 20,
    UseProxy = false
});
```

### Async/Await Best Practices

```csharp
// ✅ CORRETO
public async Task<Pedido> CriarPedidoAsync(PedidoDto dto)
{
    var pedido = new Pedido();
    await _repository.AddAsync(pedido);
    await _eventBus.PublishAsync(new PedidoCriadoEvent(pedido.Id));
    return pedido;
}

// ❌ EVITAR: Bloqueio com .Result ou .Wait()
public Pedido CriarPedido(PedidoDto dto)
{
    var pedido = new Pedido();
    _repository.AddAsync(pedido).Result;  // Deadlock risk!
    return pedido;
}
```

---

## 📚 CHECKLIST DE PRODUÇÃO

### Antes do Go-Live

#### Infrastructure
- [ ] Multi-node cluster (mínimo 3 nodes)
- [ ] Auto-scaling configurado (HPA + CA)
- [ ] Persistent volumes com backups
- [ ] Network policies ativas
- [ ] Ingress com TLS válido
- [ ] DNS configurado

#### Application
- [ ] Replicas >= 3 por deployment
- [ ] Health probes funcionando
- [ ] Resources requests/limits definidos
- [ ] Graceful shutdown implementado
- [ ] Connection pooling otimizado
- [ ] Caching implementado

#### Security
- [ ] Secrets em vault (não em Git)
- [ ] RBAC configurado
- [ ] Pod Security Standards
- [ ] Vulnerability scanning no CI/CD
- [ ] Security Context definido
- [ ] Network policies testadas

#### Monitoring
- [ ] Prometheus + Grafana instalado
- [ ] Dashboards criados
- [ ] Alertas configurados
- [ ] Logs centralizados
- [ ] Tracing distribuído
- [ ] Runbooks documentados

#### CI/CD
- [ ] Pipeline completo (build, test, deploy)
- [ ] Blue-Green ou Canary deployment
- [ ] Automated rollback
- [ ] Performance tests no pipeline
- [ ] Ambientes: dev, staging, prod

#### Disaster Recovery
- [ ] Backups automatizados
- [ ] Restore testado
- [ ] RTO/RPO documentados
- [ ] DR playbook criado
- [ ] Multi-region setup (se aplicável)

---

**Última atualização:** 07/01/2026  
**Autor:** DevOps Team  
**Versão:** 1.0
