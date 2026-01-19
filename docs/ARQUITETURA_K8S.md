# 🏗️ Arquitetura Kubernetes - The Throne of Games

**Versão:** 1.0  
**Data:** 7 de Janeiro de 2026  
**Status:** ✅ Implementado

---

## 📊 Arquitetura Geral

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        KUBERNETES CLUSTER                              │
│                     (Local, AKS, GKE, EKS)                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐   │
│  │             INGRESS CONTROLLER (NGINX)                        │   │
│  │  ┌─────────────────────────────────────────────────────────┐  │   │
│  │  │ /api/usuarios  → usuarios-api Service:80             │  │   │
│  │  │ /api/catalogo  → catalogo-api Service:80             │  │   │
│  │  │ /api/vendas    → vendas-api Service:80               │  │   │
│  │  │ SSL/TLS Enabled (Secrets)                             │  │   │
│  │  └─────────────────────────────────────────────────────────┘  │   │
│  └────────────────────────────────────────────────────────────────┘   │
│         ↓                    ↓                    ↓                     │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐           │
│  │ Usuarios API │     │ Catalogo API │     │  Vendas API  │           │
│  │ Deployment   │     │ Deployment   │     │ Deployment   │           │
│  │              │     │              │     │              │           │
│  │ Replicas:    │     │ Replicas:    │     │ Replicas:    │           │
│  │ 3-10 (HPA)   │     │ 3-10 (HPA)   │     │ 3-10 (HPA)   │           │
│  │              │     │              │     │              │           │
│  │ ┌──────────┐ │     │ ┌──────────┐ │     │ ┌──────────┐ │           │
│  │ │Container │ │     │ │Container │ │     │ │Container │ │           │
│  │ │.NET 9.0  │ │     │ │.NET 9.0  │ │     │ │.NET 9.0  │ │           │
│  │ │Port 80   │ │     │ │Port 80   │ │     │ │Port 80   │ │           │
│  │ └──────────┘ │     │ └──────────┘ │     │ └──────────┘ │           │
│  │              │     │              │     │              │           │
│  │ Resources:   │     │ Resources:   │     │ Resources:   │           │
│  │ CPU: 200m    │     │ CPU: 200m    │     │ CPU: 200m    │           │
│  │ RAM: 256Mi   │     │ RAM: 256Mi   │     │ RAM: 256Mi   │           │
│  └──────────────┘     └──────────────┘     └──────────────┘           │
│         │                    │                    │                    │
│         └────────────────────┼────────────────────┘                    │
│                              ↓                                         │
│  ┌──────────────────────────────────────────────────────┐             │
│  │          SQL SERVER (StatefulSet)                    │             │
│  │  ┌────────────────────────────────────────────────┐  │             │
│  │  │ mssql-0 Container                             │  │             │
│  │  │ Image: mcr.microsoft.com/mssql/server:2019    │  │             │
│  │  │ Port: 1433                                     │  │             │
│  │  │ Resources: CPU: 500m, RAM: 1Gi                │  │             │
│  │  │                                                │  │             │
│  │  │ Persistent Volume: 10Gi                       │  │             │
│  │  │ Storage Class: default                        │  │             │
│  │  └────────────────────────────────────────────────┘  │             │
│  │                                                      │             │
│  │ Service: mssql-service:1433 (Headless)            │             │
│  └──────────────────────────────────────────────────────┘             │
│                                                                         │
│  ┌──────────────────────────────────────────────────────┐             │
│  │         RABBITMQ (StatefulSet)                       │             │
│  │  ┌────────────────────────────────────────────────┐  │             │
│  │  │ rabbitmq-0 Container                          │  │             │
│  │  │ Image: rabbitmq:3.12-management-alpine       │  │             │
│  │  │ Ports: 5672 (AMQP), 15672 (Management)       │  │             │
│  │  │ Resources: CPU: 200m, RAM: 512Mi             │  │             │
│  │  │                                               │  │             │
│  │  │ Persistent Volume: 5Gi                       │  │             │
│  │  │ Storage Class: default                       │  │             │
│  │  └────────────────────────────────────────────────┘  │             │
│  │                                                      │             │
│  │ Service: rabbitmq-service:5672 (Headless)         │             │
│  │ External Service: rabbitmq-external (NodePort)   │             │
│  └──────────────────────────────────────────────────────┘             │
│                                                                         │
│  ┌──────────────────────────────────────────────────────┐             │
│  │    MONITORING NAMESPACE (Separado)                  │             │
│  │  ┌────────────────────────────────────────────────┐  │             │
│  │  │ Prometheus Deployment                         │  │             │
│  │  │ - Scraping metrics de todos pods             │  │             │
│  │  │ - Storage: 5Gi                               │  │             │
│  │  │ - Retention: 7 dias                          │  │             │
│  │  └────────────────────────────────────────────────┘  │             │
│  │                                                      │             │
│  │  ┌────────────────────────────────────────────────┐  │             │
│  │  │ Grafana Deployment                           │  │             │
│  │  │ - Visualização de métricas                  │  │             │
│  │  │ - Dashboards pré-configurados              │  │             │
│  │  │ - UI: localhost:3000                        │  │             │
│  │  └────────────────────────────────────────────────┘  │             │
│  └──────────────────────────────────────────────────────┘             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Componentes Principais

### 1. Namespaces

```yaml
# Namespace 1: thethroneofgames
- APIs (Usuarios, Catalogo, Vendas)
- Backend Infrastructure (SQL Server, RabbitMQ)
- Ingress Controller
- Network Policies

# Namespace 2: thethroneofgames-monitoring
- Prometheus
- Grafana
- Alertmanager (futuro)
```

### 2. Deployments

#### Usuarios API

```yaml
Deployment:
  Name: usuarios-api
  Namespace: thethroneofgames
  Replicas: 3-10 (via HPA)
  
  Pod Template:
    Container:
      Image: thethroneofgames-usuarios-api:latest
      Port: 80
      Resources:
        Requests:
          CPU: 100m
          Memory: 128Mi
        Limits:
          CPU: 500m
          Memory: 512Mi
      
      Environment Variables:
        - Database Connection String
        - JWT Secret
        - RabbitMQ URL
      
      Probes:
        Readiness:
          HTTP GET /health/ready
          Initial Delay: 10s
          Timeout: 5s
        Liveness:
          HTTP GET /health
          Initial Delay: 30s
          Timeout: 5s
      
      Volumes:
        - ConfigMap: usuarios-config
        - Secret: usuarios-secrets
```

#### Catalogo API

```yaml
Deployment:
  Name: catalogo-api
  Namespace: thethroneofgames
  Replicas: 3-10 (via HPA)
  
  Pod Template:
    Container:
      Image: thethroneofgames-catalogo-api:latest
      Port: 80
      Resources:
        Requests:
          CPU: 100m
          Memory: 128Mi
        Limits:
          CPU: 500m
          Memory: 512Mi
      
      Environment Variables:
        - Database Connection String
        - JWT Secret
        - RabbitMQ URL
        - Cache Configuration
      
      Probes:
        Readiness: HTTP GET /health/ready
        Liveness: HTTP GET /health
      
      Volumes:
        - ConfigMap: catalogo-config
        - Secret: catalogo-secrets
```

#### Vendas API

```yaml
Deployment:
  Name: vendas-api
  Namespace: thethroneofgames
  Replicas: 3-10 (via HPA)
  
  Pod Template:
    Container:
      Image: thethroneofgames-vendas-api:latest
      Port: 80
      Resources:
        Requests:
          CPU: 100m
          Memory: 128Mi
        Limits:
          CPU: 500m
          Memory: 512Mi
      
      Environment Variables:
        - Database Connection String
        - JWT Secret
        - RabbitMQ URL
        - Payment Gateway Config
      
      Probes:
        Readiness: HTTP GET /health/ready
        Liveness: HTTP GET /health
      
      Volumes:
        - ConfigMap: vendas-config
        - Secret: vendas-secrets
```

### 3. StatefulSets

#### SQL Server

```yaml
StatefulSet:
  Name: mssql
  Namespace: thethroneofgames
  Replicas: 1
  Service: mssql-service (Headless)
  
  Pod Template:
    Container:
      Image: mcr.microsoft.com/mssql/server:2019-latest
      Port: 1433
      Resources:
        Requests:
          CPU: 250m
          Memory: 512Mi
        Limits:
          CPU: 1000m
          Memory: 2Gi
      
      Environment:
        ACCEPT_EULA: Y
        SA_PASSWORD: (from Secret)
        MSSQL_PID: Express
      
      Volume Mounts:
        - mssql-data: /var/opt/mssql
      
      Probes:
        Readiness:
          Exec: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1"
        Liveness:
          Exec: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT @@VERSION"
  
  PersistentVolumeClaim:
    Name: mssql-data
    Size: 10Gi
    StorageClass: default
    AccessMode: ReadWriteOnce
```

#### RabbitMQ

```yaml
StatefulSet:
  Name: rabbitmq
  Namespace: thethroneofgames
  Replicas: 1
  Service: rabbitmq-service (Headless)
  
  Pod Template:
    Container:
      Image: rabbitmq:3.12-management-alpine
      Ports:
        - 5672 (AMQP)
        - 15672 (Management)
      Resources:
        Requests:
          CPU: 100m
          Memory: 256Mi
        Limits:
          CPU: 500m
          Memory: 1Gi
      
      Environment:
        RABBITMQ_DEFAULT_USER: guest
        RABBITMQ_DEFAULT_PASS: guest
        RABBITMQ_ERLANG_COOKIE: secret
      
      Volume Mounts:
        - rabbitmq-data: /var/lib/rabbitmq
      
      Probes:
        Readiness:
          Exec: rabbitmq-diagnostics -q ping
        Liveness:
          Exec: rabbitmq-diagnostics -q status
  
  PersistentVolumeClaim:
    Name: rabbitmq-data
    Size: 5Gi
    StorageClass: default
    AccessMode: ReadWriteOnce
```

### 4. Services

```yaml
# LoadBalancer Service (Ingress)
apiVersion: v1
kind: Service
metadata:
  name: nginx-ingress
  namespace: thethroneofgames
spec:
  type: LoadBalancer
  ports:
    - port: 80
      targetPort: 80
      protocol: TCP
    - port: 443
      targetPort: 443
      protocol: TCP
  selector:
    app: nginx-ingress

---

# ClusterIP Services
apiVersion: v1
kind: Service
metadata:
  name: usuarios-api
  namespace: thethroneofgames
spec:
  type: ClusterIP
  ports:
    - port: 80
      targetPort: 80
  selector:
    app: usuarios-api

---

# Headless Service (StatefulSet)
apiVersion: v1
kind: Service
metadata:
  name: mssql-service
  namespace: thethroneofgames
spec:
  clusterIP: None
  ports:
    - port: 1433
      targetPort: 1433
  selector:
    app: mssql
```

### 5. Horizontal Pod Autoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: usuarios-api-hpa
  namespace: thethroneofgames
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: usuarios-api
  
  minReplicas: 3
  maxReplicas: 10
  
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
  
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 50
          periodSeconds: 60
    
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
        - type: Percent
          value: 100
          periodSeconds: 15
        - type: Pods
          value: 2
          periodSeconds: 15
      selectPolicy: Max
```

### 6. ConfigMaps

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: usuarios-config
  namespace: thethroneofgames
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information"
        }
      },
      "ConnectionStrings": {
        "DefaultConnection": "Server=mssql-service;Database=UsuariosDB;User Id=sa;Password=$(SA_PASSWORD);"
      },
      "EventBus": {
        "HostName": "rabbitmq-service",
        "Port": 5672,
        "UserName": "guest",
        "Password": "guest"
      },
      "Jwt": {
        "Issuer": "TheThrone ofGames",
        "Audience": "TheThroneOfGamesClients",
        "SecurityKey": "$(JWT_SECRET)"
      }
    }
```

### 7. Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: usuarios-secrets
  namespace: thethroneofgames
type: Opaque
data:
  SA_PASSWORD: base64(YourSecurePassword123!)
  JWT_SECRET: base64(YourJWTSecretKey)
  DB_CONNECTION_STRING: base64(connection_string)
```

### 8. Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: thethroneofgames-ingress
  namespace: thethroneofgames
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  ingressClassName: nginx
  tls:
    - hosts:
        - thethroneofgames.local
      secretName: tls-secret
  
  rules:
    - host: thethroneofgames.local
      http:
        paths:
          - path: /api/usuarios
            pathType: Prefix
            backend:
              service:
                name: usuarios-api
                port:
                  number: 80
          
          - path: /api/catalogo
            pathType: Prefix
            backend:
              service:
                name: catalogo-api
                port:
                  number: 80
          
          - path: /api/vendas
            pathType: Prefix
            backend:
              service:
                name: vendas-api
                port:
                  number: 80
```

---

## 🔄 Fluxo de Requisição

```
┌──────────────────┐
│  Cliente HTTP    │
└──────────────────┘
        ↓
 GET /api/usuarios/profile
        ↓
┌──────────────────────────┐
│  NGINX Ingress           │
│  (Port 80/443)           │
│  URL Routing             │
└──────────────────────────┘
        ↓
 Roteia para /api/usuarios
        ↓
┌──────────────────────────┐
│  Service: usuarios-api   │
│  (Port 80)               │
│  Load Balancing          │
└──────────────────────────┘
        ↓
 Seleciona Pod (Round Robin)
        ↓
┌──────────────────────────┐
│  Pod usuarios-api-xyz1   │
│  (.NET Application)      │
│  Port 80                 │
└──────────────────────────┘
        ↓
 Conecta ao SQL Server
        ↓
┌──────────────────────────┐
│  Service: mssql-service  │
│  Port 1433               │
└──────────────────────────┘
        ↓
┌──────────────────────────┐
│  Pod mssql-0             │
│  SQL Server Instance     │
└──────────────────────────┘
        ↓
 Query → Response
        ↓
 Volta pela rede
        ↓
┌──────────────────┐
│  Cliente HTTP    │
│  Response 200 OK │
└──────────────────┘
```

---

## 📊 Escalabilidade

### Cenário 1: Carga Baixa (Off-Peak)

```
Usuarios API: 3 replicas
Catalogo API: 3 replicas
Vendas API: 3 replicas

CPU Usage: ~20% per pod
Memory Usage: ~150Mi per pod

Total Resources:
- CPU: 9 × 100m = 900m
- Memory: 9 × 128Mi = 1.1Gi
```

### Cenário 2: Carga Normal (Business Hours)

```
Usuarios API: 5 replicas
Catalogo API: 5 replicas
Vendas API: 5 replicas

CPU Usage: ~50% per pod
Memory Usage: ~300Mi per pod

Total Resources:
- CPU: 15 × 250m = 3.75 CPUs
- Memory: 15 × 300Mi = 4.5Gi
```

### Cenário 3: Carga Alta (Peak Hours)

```
Usuarios API: 10 replicas
Catalogo API: 10 replicas
Vendas API: 10 replicas

CPU Usage: ~70% per pod
Memory Usage: ~400Mi per pod

Total Resources:
- CPU: 30 × 350m = 10.5 CPUs
- Memory: 30 × 400Mi = 12Gi

Auto-scaling ativa:
- Monitora CPU e Memory
- Cria novos pods automaticamente
- Max limit: 10 replicas por API
```

---

## 🔐 Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: thethroneofgames-netpol
  namespace: thethroneofgames
spec:
  podSelector: {}
  
  policyTypes:
    - Ingress
    - Egress
  
  ingress:
    # Ingress Controller pode acessar APIs
    - from:
        - namespaceSelector:
            matchLabels:
              name: ingress-nginx
      to:
        - ports:
            - protocol: TCP
              port: 80
    
    # APIs podem acessar SQL Server
    - from:
        - podSelector:
            matchLabels:
              tier: api
      to:
        - podSelector:
            matchLabels:
              app: mssql
          ports:
            - protocol: TCP
              port: 1433
    
    # APIs podem acessar RabbitMQ
    - from:
        - podSelector:
            matchLabels:
              tier: api
      to:
        - podSelector:
            matchLabels:
              app: rabbitmq
          ports:
            - protocol: TCP
              port: 5672
```

---

## 📈 Monitoramento de Arquitetura

### Métricas Coletadas

```
# Cluster
- Node CPU/Memory/Disk usage
- Pod CPU/Memory usage
- Network bandwidth

# Applications
- HTTP Request Rate
- Response Time (P50, P95, P99)
- Error Rate
- Request Latency

# Infrastructure
- Pod Restarts
- PVC Usage
- Service Connectivity

# Events
- Pod Evictions
- Node Pressure
- Unschedulable Pods
```

### Dashboards Grafana

1. **Cluster Overview**
   - Total nodes, pods, services
   - Resource utilization
   - Node status

2. **Application Performance**
   - Request rate by service
   - Response time distribution
   - Error rate

3. **Infrastructure**
   - PVC usage trend
   - Network I/O
   - Pod restart count

4. **Auto Scaling**
   - HPA metrics
   - Desired vs current replicas
   - Scaling events timeline

---

## ✅ Deployment Checklist

- [ ] Cluster Kubernetes criado (local, AKS, GKE)
- [ ] Namespaces criados (thethroneofgames, monitoring)
- [ ] ConfigMaps e Secrets configurados
- [ ] SQL Server StatefulSet deployado
- [ ] RabbitMQ StatefulSet deployado
- [ ] APIs deployadas com HPA
- [ ] Ingress configurado
- [ ] Prometheus & Grafana rodando
- [ ] Health checks validados
- [ ] HPA testado com carga
- [ ] Network Policies aplicadas
- [ ] Backup/Recovery testado

---

**Versão:** 1.0  
**Última Atualização:** 7 de Janeiro de 2026  
**Status:** ✅ Implementado e Documentado
