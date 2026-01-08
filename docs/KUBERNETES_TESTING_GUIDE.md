# Guia de Instalação e Teste do Cluster GKE

## 🔧 Pré-requisitos

### 1. Instalar Google Cloud SDK (gcloud CLI)

**Windows**:
```powershell
# Download e instale de:
# https://cloud.google.com/sdk/docs/install-sdk#windows

# Ou use o instalador interativo:
(New-Object Net.WebClient).DownloadFile("https://dl.google.com/dl/cloudsdk/channels/rapid/GoogleCloudSDKInstaller.exe", "$env:Temp\GoogleCloudSDKInstaller.exe")
& $env:Temp\GoogleCloudSDKInstaller.exe
```

**Após instalar, reinicie o PowerShell e execute**:
```powershell
gcloud init
gcloud auth login
gcloud config set project project-62120210-43eb-4d93-954
```

### 2. Instalar kubectl

```powershell
gcloud components install kubectl
```

### 3. Configurar acesso ao cluster

```powershell
gcloud container clusters get-credentials autopilot-cluster-1 `
  --region southamerica-east1 `
  --project project-62120210-43eb-4d93-954
```

## 🚀 Executar Testes no Kubernetes

Após configurar o gcloud:

```powershell
# Execute o script de teste
.\scripts\test-kubernetes-deployment.ps1

# Ou com parâmetros customizados
.\scripts\test-kubernetes-deployment.ps1 `
  -Duration 60 `
  -ConcurrentUsers 10
```

## 📊 Verificações Manuais

### Verificar se o cluster existe
```powershell
gcloud container clusters list --region southamerica-east1
```

### Verificar pods
```powershell
kubectl get pods -n thethroneofgames
```

### Verificar services
```powershell
kubectl get services -n thethroneofgames
```

### Verificar HPA
```powershell
kubectl get hpa -n thethroneofgames
```

### Verificar Ingress e IP externo
```powershell
kubectl get ingress -n thethroneofgames
```

### Obter logs de um pod
```powershell
kubectl logs -f <POD_NAME> -n thethroneofgames
```

### Port-forward manual
```powershell
# Usuarios API
kubectl port-forward -n thethroneofgames service/usuarios-api 5001:80

# Em outro terminal, teste:
curl http://localhost:5001/swagger
```

## 🔄 Alternativa: Testar Localmente

Se o cluster não estiver disponível, teste os containers locais:

```powershell
# Certifique-se de que os containers estão rodando
docker-compose up -d

# Execute os testes locais
.\scripts\quick-performance-test.ps1
```

## 📝 Criar o Cluster (se não existir)

```powershell
gcloud container clusters create-auto autopilot-cluster-1 `
  --region=southamerica-east1 `
  --project=project-62120210-43eb-4d93-954
```

**Tempo de criação**: ~10-15 minutos

## 🎯 Deploy no Cluster (manual)

```powershell
# 1. Criar namespace
kubectl create namespace thethroneofgames

# 2. Aplicar ConfigMaps e Secrets
kubectl apply -f k8s/configmaps.yaml
kubectl apply -f k8s/secrets.yaml

# 3. Deploy dos serviços
kubectl apply -f k8s/deployments/
kubectl apply -f k8s/hpa.yaml
kubectl apply -f k8s/ingress.yaml

# 4. Verificar status
kubectl get all -n thethroneofgames
```

## 💡 Troubleshooting

### Erro: "cluster not found"
- Verifique se o cluster foi criado
- Confirme nome e região corretos
- Execute: `gcloud container clusters list`

### Erro: "unauthorized"
- Execute: `gcloud auth login`
- Configure projeto: `gcloud config set project PROJECT_ID`

### Pods não iniciam
- Verifique logs: `kubectl logs <POD_NAME> -n thethroneofgames`
- Verifique eventos: `kubectl get events -n thethroneofgames`
- Verifique recursos: `kubectl describe pod <POD_NAME> -n thethroneofgames`

### Ingress sem IP
- Aguarde 5-10 minutos (provisionamento do Load Balancer)
- Verifique: `kubectl describe ingress -n thethroneofgames`

## 🧹 Limpeza (economia de custos)

```powershell
# Deletar todos os recursos
kubectl delete namespace thethroneofgames

# Ou deletar o cluster inteiro
gcloud container clusters delete autopilot-cluster-1 `
  --region southamerica-east1 `
  --quiet
```

---

**Links Úteis**:
- [Instalar gcloud SDK](https://cloud.google.com/sdk/docs/install)
- [Configurar kubectl](https://cloud.google.com/kubernetes-engine/docs/how-to/cluster-access-for-kubectl)
- [GKE Quickstart](https://cloud.google.com/kubernetes-engine/docs/quickstart)
