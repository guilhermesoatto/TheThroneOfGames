# 🚀 GUIA DE DEPLOYMENT - KUBERNETES & CI/CD

**Data:** 07/01/2026  
**Versão:** 1.0  
**Status:** ✅ Pronto para Deploy

---

## 📋 VISÃO GERAL

Este guia documenta como fazer deploy da aplicação "The Throne of Games" em Kubernetes (local ou cloud) e como configurar o pipeline CI/CD no GitHub Actions.

### O Que Foi Implementado

✅ **CI/CD Pipeline Completo** (9 jobs)  
✅ **12 Manifestos Kubernetes**  
✅ **Script de Deploy Automatizado**  
✅ **Blue-Green Deployment**  
✅ **Auto-Scaling (HPA)**  
✅ **Network Policies**  
✅ **Security Scanning**

---

## 🎯 OPÇÃO 1: DEPLOY LOCAL EM KUBERNETES

### Pré-requisitos

```powershell
# 1. Instalar kubectl
choco install kubernetes-cli

# 2. Escolher uma opção de cluster local:

# Opção A: Docker Desktop (mais simples)
# - Habilitar Kubernetes nas configurações do Docker Desktop

# Opção B: k3d (recomendado para desenvolvimento)
choco install k3d

# Opção C: minikube
choco install minikube
```

### Deploy Rápido

```powershell
# 1. Navegar para o diretório de scripts
cd scripts

# 2. Executar deploy completo
.\deploy-k8s-local.ps1

# OU com criação de cluster k3d
.\deploy-k8s-local.ps1 -ClusterType k3d -CreateCluster

# OU com minikube
.\deploy-k8s-local.ps1 -ClusterType minikube -CreateCluster
```

### O Que o Script Faz

1. ✅ Verifica dependências (kubectl)
2. ✅ Cria cluster se solicitado
3. ✅ Constrói imagens Docker localmente
4. ✅ Importa imagens para o cluster
5. ✅ Aplica todos os manifestos Kubernetes:
   - Namespaces
   - ConfigMaps
   - Secrets
   - StatefulSets (SQL Server, RabbitMQ)
   - Deployments (3 APIs)
   - HPA
   - Network Policies
   - Ingress
6. ✅ Aguarda pods ficarem prontos
7. ✅ Configura port forwards automáticos
8. ✅ Exibe URLs de acesso

### URLs Após Deploy Local

```
🌐 APIs:
   http://localhost:5001/swagger - Usuarios API
   http://localhost:5002/swagger - Catalogo API
   http://localhost:5003/swagger - Vendas API

📊 Management:
   http://localhost:15672 - RabbitMQ (guest/guest)
```

### Comandos Úteis

```powershell
# Ver todos os pods
kubectl get pods -n thethroneofgames

# Ver logs de um pod específico
kubectl logs -f <pod-name> -n thethroneofgames

# Ver métricas de recursos
kubectl top pods -n thethroneofgames

# Ver HPA status
kubectl get hpa -n thethroneofgames

# Restart de um deployment
kubectl rollout restart deployment/usuarios-api -n thethroneofgames

# Escalar manualmente (temporário)
kubectl scale deployment/usuarios-api --replicas=5 -n thethroneofgames

# Ver eventos
kubectl get events -n thethroneofgames --sort-by='.lastTimestamp'

# Deletar tudo
kubectl delete namespace thethroneofgames
```

---

## ☁️ OPÇÃO 2: DEPLOY EM CLOUD (Azure/AWS/GCP)

### Azure Kubernetes Service (AKS)

```powershell
# 1. Login no Azure
az login

# 2. Criar Resource Group
az group create --name thethroneofgames-rg --location eastus

# 3. Criar cluster AKS
az aks create `
  --resource-group thethroneofgames-rg `
  --name thethroneofgames-aks `
  --node-count 3 `
  --node-vm-size Standard_D2s_v3 `
  --enable-managed-identity `
  --generate-ssh-keys

# 4. Obter credenciais
az aks get-credentials `
  --resource-group thethroneofgames-rg `
  --name thethroneofgames-aks

# 5. Deploy da aplicação
kubectl apply -f k8s/namespaces.yaml
kubectl apply -f k8s/configmaps.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/statefulsets/
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/hpa.yaml
kubectl apply -f k8s/ingress.yaml

# 6. Verificar status
kubectl get all -n thethroneofgames
```

### AWS Elastic Kubernetes Service (EKS)

```powershell
# 1. Configurar AWS CLI
aws configure

# 2. Criar cluster EKS
eksctl create cluster `
  --name thethroneofgames `
  --region us-east-1 `
  --nodegroup-name standard-workers `
  --node-type t3.medium `
  --nodes 3 `
  --nodes-min 2 `
  --nodes-max 5 `
  --managed

# 3. Obter credenciais
aws eks update-kubeconfig --name thethroneofgames --region us-east-1

# 4. Deploy da aplicação (mesmos comandos que Azure)
kubectl apply -f k8s/
```

### Google Kubernetes Engine (GKE)

```powershell
# 1. Login no GCP
gcloud auth login

# 2. Criar cluster GKE
gcloud container clusters create thethroneofgames `
  --zone us-central1-a `
  --num-nodes 3 `
  --machine-type n1-standard-2 `
  --disk-size 50

# 3. Obter credenciais
gcloud container clusters get-credentials thethroneofgames

# 4. Deploy da aplicação (mesmos comandos)
kubectl apply -f k8s/
```

---

## 🔄 CI/CD PIPELINE - GITHUB ACTIONS

### Configuração Inicial

#### 1. Secrets do GitHub

Navegue para **Settings → Secrets and variables → Actions** e adicione:

```
KUBE_CONFIG_DEV       - Kubeconfig do cluster de desenvolvimento (base64)
KUBE_CONFIG_STAGING   - Kubeconfig do cluster de staging (base64)
KUBE_CONFIG_PROD      - Kubeconfig do cluster de produção (base64)
SONAR_HOST_URL        - URL do SonarQube (opcional)
SONAR_TOKEN           - Token do SonarQube (opcional)
```

#### Como Obter Kubeconfig Base64

```powershell
# Linux/Mac
cat ~/.kube/config | base64

# Windows PowerShell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$env:USERPROFILE\.kube\config"))
```

### Estrutura do Pipeline

```yaml
├── build-and-test         # Compila e testa o código
├── docker-build           # Constrói e publica imagens Docker
├── security-scan          # Scan de vulnerabilidades (Trivy)
├── code-quality           # Análise de qualidade (SonarQube)
├── deploy-dev             # Deploy em Development
├── deploy-staging         # Deploy em Staging
├── deploy-production      # Deploy em Production (Blue-Green)
├── performance-test       # Testes de carga automatizados
└── cleanup                # Limpeza de recursos antigos
```

### Triggers

O pipeline é acionado em:
- **Push** para branches `master` ou `develop`
- **Pull Request** para branches `master` ou `develop`
- **Manual** via `workflow_dispatch`

### Fluxo de Deploy

```
Push to develop → Build → Test → Security → Deploy Dev → Smoke Tests
                                                ↓
Push to master  → Build → Test → Security → Deploy Staging → Integration Tests
                                                ↓
                                        Deploy Production (Blue-Green)
                                                ↓
                                        Performance Tests
```

### Ambientes GitHub

Configure 3 ambientes em **Settings → Environments**:

1. **development**
   - URL: https://dev.thethroneofgames.com
   - Auto-deploy: Sim
   - Required reviewers: Não

2. **staging**
   - URL: https://staging.thethroneofgames.com
   - Auto-deploy: Sim
   - Required reviewers: Não

3. **production**
   - URL: https://thethroneofgames.com
   - Auto-deploy: Não (manual approval)
   - Required reviewers: Sim (adicionar aprovadores)
   - Wait timer: 10 minutos

---

## 📊 MONITORAMENTO & OBSERVABILIDADE

### Métricas Disponíveis

```powershell
# CPU e Memory de todos os pods
kubectl top pods -n thethroneofgames

# Nodes do cluster
kubectl top nodes

# HPA status (auto-scaling)
kubectl get hpa -n thethroneofgames -w

# Eventos em tempo real
kubectl get events -n thethroneofgames -w
```

### Logs Agregados

```powershell
# Todos os logs de um deployment
kubectl logs -f deployment/usuarios-api -n thethroneofgames

# Logs de múltiplos pods
kubectl logs -f -l app=usuarios-api -n thethroneofgames

# Logs de todos os backend services
kubectl logs -f -l tier=backend -n thethroneofgames --all-containers=true
```

### Prometheus Queries

Se Prometheus estiver instalado:

```promql
# Request rate por API
rate(http_requests_total{namespace="thethroneofgames"}[5m])

# P95 latency
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate
rate(http_requests_total{namespace="thethroneofgames",status=~"5.."}[5m])
```

---

## 🔒 SEGURANÇA

### Network Policies Aplicadas

1. **allow-api-to-database**: Apenas APIs podem acessar SQL Server
2. **allow-api-to-rabbitmq**: Apenas APIs podem acessar RabbitMQ
3. **allow-external-to-apis**: Apenas Ingress pode acessar APIs
4. **deny-all-by-default**: Nega todo tráfego não explicitamente permitido

### Verificar Network Policies

```powershell
kubectl get networkpolicies -n thethroneofgames
kubectl describe networkpolicy <policy-name> -n thethroneofgames
```

### Security Scanning

O pipeline CI/CD executa automaticamente:
- **Trivy**: Scan de vulnerabilidades em imagens Docker
- **Results**: Enviados para GitHub Security tab

Executar localmente:

```powershell
# Scan de uma imagem
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock `
  aquasec/trivy image thethroneofgames/usuarios-api:latest

# Scan do filesystem
docker run --rm -v ${PWD}:/workspace `
  aquasec/trivy fs /workspace
```

---

## 📈 AUTO-SCALING (HPA)

### Configuração Atual

```yaml
minReplicas: 3
maxReplicas: 10
targetCPU: 70%
targetMemory: 80%
```

### Como Funciona

- **Scale Up**: Imediato (100% increase, max 2 pods/15s)
- **Scale Down**: 300s stabilization, 50% decrease

### Testar Auto-Scaling

```powershell
# 1. Gerar carga
cd scripts
.\load-test.ps1 -NumUsuarios 100 -NumPedidos 500 -ConcurrentUsers 20

# 2. Assistir HPA em ação
kubectl get hpa -n thethroneofgames -w

# 3. Ver pods sendo criados
kubectl get pods -n thethroneofgames -w
```

---

## 🐛 TROUBLESHOOTING

### Pods não iniciam

```powershell
# Ver motivo
kubectl describe pod <pod-name> -n thethroneofgames

# Ver logs de inicialização
kubectl logs <pod-name> -n thethroneofgames --previous
```

### Erros de conectividade

```powershell
# Verificar services
kubectl get svc -n thethroneofgames

# Testar conectividade interna
kubectl run test-pod --rm -it --image=busybox -n thethroneofgames -- sh
# Dentro do pod:
wget -O- http://usuarios-api:5001/swagger
```

### HPA não funciona

```powershell
# Verificar metrics-server
kubectl get apiservice v1beta1.metrics.k8s.io -o yaml

# Instalar metrics-server se necessário
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

### Persistent Volumes issues

```powershell
# Ver PVCs
kubectl get pvc -n thethroneofgames

# Ver PVs
kubectl get pv

# Descrever problema
kubectl describe pvc <pvc-name> -n thethroneofgames
```

---

## 📚 REFERÊNCIAS

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure AKS Documentation](https://docs.microsoft.com/en-us/azure/aks/)
- [AWS EKS Documentation](https://docs.aws.amazon.com/eks/)
- [GCP GKE Documentation](https://cloud.google.com/kubernetes-engine/docs)
- [Helm Charts](https://helm.sh/docs/)

---

## ✅ CHECKLIST DE DEPLOYMENT

### Pré-Deploy
- [ ] Verificar recursos do cluster (CPU, Memory, Storage)
- [ ] Revisar secrets e configmaps
- [ ] Atualizar imagens Docker no Container Registry
- [ ] Testar manifests localmente primeiro
- [ ] Backup de banco de dados (se migration)

### Durante Deploy
- [ ] Monitorar logs durante rollout
- [ ] Verificar health probes
- [ ] Confirmar pods em Ready state
- [ ] Testar endpoints após deploy
- [ ] Verificar métricas no Prometheus/Grafana

### Pós-Deploy
- [ ] Executar smoke tests
- [ ] Validar auto-scaling
- [ ] Verificar logs por erros
- [ ] Testar rollback se necessário
- [ ] Documentar mudanças

---

**Última atualização:** 07/01/2026  
**Autor:** DevOps Team  
**Versão:** 1.0
