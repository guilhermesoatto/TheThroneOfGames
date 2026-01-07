# Deploy Local Kubernetes - The Throne of Games
# Este script faz o deploy completo da aplicação em um cluster Kubernetes local (k3d, minikube, Docker Desktop)

param(
    [Parameter()]
    [ValidateSet('k3d', 'minikube', 'docker-desktop')]
    [string]$ClusterType = 'docker-desktop',
    
    [Parameter()]
    [switch]$CreateCluster,
    
    [Parameter()]
    [switch]$DeleteCluster,
    
    [Parameter()]
    [switch]$SkipBuild,
    
    [Parameter()]
    [switch]$WatchLogs
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Verificar kubectl
Write-Step "Verificando dependências"
try {
    $kubectlVersion = kubectl version --client --short 2>$null
    Write-Success "kubectl encontrado: $kubectlVersion"
} catch {
    Write-Error-Custom "kubectl não encontrado. Por favor, instale kubectl."
    exit 1
}

# Criar cluster se necessário
if ($CreateCluster) {
    Write-Step "Criando cluster Kubernetes local"
    
    switch ($ClusterType) {
        'k3d' {
            Write-Info "Criando cluster k3d..."
            k3d cluster create thethroneofgames `
                --api-port 6443 `
                --port "8080:80@loadbalancer" `
                --port "8443:443@loadbalancer" `
                --agents 2
            kubectl config use-context k3d-thethroneofgames
        }
        'minikube' {
            Write-Info "Criando cluster minikube..."
            minikube start --profile thethroneofgames --cpus 4 --memory 8192
            minikube profile thethroneofgames
            kubectl config use-context thethroneofgames
        }
        'docker-desktop' {
            Write-Info "Usando Docker Desktop Kubernetes..."
            kubectl config use-context docker-desktop
        }
    }
    
    Write-Success "Cluster criado com sucesso"
}

# Deletar cluster se solicitado
if ($DeleteCluster) {
    Write-Step "Deletando cluster Kubernetes"
    
    switch ($ClusterType) {
        'k3d' {
            k3d cluster delete thethroneofgames
        }
        'minikube' {
            minikube delete --profile thethroneofgames
        }
        'docker-desktop' {
            Write-Warning "Não é possível deletar cluster Docker Desktop. Use o Docker Desktop UI."
        }
    }
    
    Write-Success "Cluster deletado"
    exit 0
}

# Verificar contexto atual
Write-Step "Verificando contexto Kubernetes"
$currentContext = kubectl config current-context
Write-Info "Contexto atual: $currentContext"

# Build de imagens Docker (se necessário)
if (-not $SkipBuild) {
    Write-Step "Construindo imagens Docker localmente"
    
    $services = @(
        @{Name="usuarios-api"; Path="GameStore.Usuarios.API"},
        @{Name="catalogo-api"; Path="GameStore.Catalogo.API"},
        @{Name="vendas-api"; Path="GameStore.Vendas.API"}
    )
    
    foreach ($service in $services) {
        Write-Info "Building $($service.Name)..."
        docker build -t "thethroneofgames/$($service.Name):latest" -f "$($service.Path)/Dockerfile" .
        
        if ($ClusterType -eq 'k3d') {
            Write-Info "Importando imagem para k3d..."
            k3d image import "thethroneofgames/$($service.Name):latest" --cluster thethroneofgames
        }
        
        if ($ClusterType -eq 'minikube') {
            Write-Info "Carregando imagem para minikube..."
            minikube image load "thethroneofgames/$($service.Name):latest" --profile thethroneofgames
        }
        
        Write-Success "$($service.Name) construído"
    }
}

# Deploy dos manifestos Kubernetes
Write-Step "Aplicando manifestos Kubernetes"

Write-Info "1. Criando namespaces..."
kubectl apply -f ../k8s/namespaces.yaml
Start-Sleep -Seconds 2

Write-Info "2. Criando ConfigMaps..."
kubectl apply -f ../k8s/configmaps.yaml

Write-Info "3. Criando Secrets..."
kubectl apply -f ../k8s/secrets.yaml

Write-Info "4. Aplicando StatefulSets (Database & Messaging)..."
kubectl apply -f ../k8s/statefulsets/

Write-Info "Aguardando StatefulSets ficarem prontos..."
kubectl wait --for=condition=ready pod -l app=sqlserver -n thethroneofgames --timeout=180s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n thethroneofgames --timeout=180s
Write-Success "StatefulSets prontos"

Write-Info "5. Aplicando Deployments (APIs)..."
# Atualizar imagens para usar locais
$deployFiles = Get-ChildItem ../k8s/deployments/*.yaml

foreach ($file in $deployFiles) {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'ghcr.io/guilhermesoatto/thethroneofgames/', 'thethroneofgames/'
    $content = $content -replace 'imagePullPolicy: Always', 'imagePullPolicy: Never'
    $content | kubectl apply -f -
}

Write-Info "Aguardando Deployments ficarem prontos..."
kubectl wait --for=condition=available deployment/usuarios-api -n thethroneofgames --timeout=180s
kubectl wait --for=condition=available deployment/catalogo-api -n thethroneofgames --timeout=180s
kubectl wait --for=condition=available deployment/vendas-api -n thethroneofgames --timeout=180s
Write-Success "Deployments prontos"

Write-Info "6. Aplicando HPA (Horizontal Pod Autoscaler)..."
kubectl apply -f ../k8s/hpa.yaml

Write-Info "7. Aplicando Network Policies..."
kubectl apply -f ../k8s/network-policies.yaml

Write-Info "8. Aplicando Ingress..."
kubectl apply -f ../k8s/ingress.yaml

Write-Success "Todos os manifestos aplicados com sucesso"

# Verificar status
Write-Step "Verificando status do deployment"

Write-Info "Pods no namespace thethroneofgames:"
kubectl get pods -n thethroneofgames

Write-Info "`nServices no namespace thethroneofgames:"
kubectl get services -n thethroneofgames

Write-Info "`nHorizontal Pod Autoscalers:"
kubectl get hpa -n thethroneofgames

# Port forward para acesso local
Write-Step "Configurando port forwards para acesso local"

Write-Info "Iniciando port forwards em background..."

# Matar port forwards existentes
Get-Process | Where-Object {$_.ProcessName -eq "kubectl" -and $_.CommandLine -like "*port-forward*"} | Stop-Process -Force -ErrorAction SilentlyContinue

# Port forwards
Start-Process kubectl -ArgumentList "port-forward -n thethroneofgames svc/usuarios-api 5001:5001" -WindowStyle Hidden
Start-Process kubectl -ArgumentList "port-forward -n thethroneofgames svc/catalogo-api 5002:5002" -WindowStyle Hidden
Start-Process kubectl -ArgumentList "port-forward -n thethroneofgames svc/vendas-api 5003:5003" -WindowStyle Hidden
Start-Process kubectl -ArgumentList "port-forward -n thethroneofgames svc/rabbitmq-management 15672:15672" -WindowStyle Hidden

Start-Sleep -Seconds 5
Write-Success "Port forwards configurados"

# Mostrar URLs de acesso
Write-Step "URLs de Acesso"
Write-Host ""
Write-Host "🌐 APIs:" -ForegroundColor Green
Write-Host "   Usuarios API:  http://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "   Catalogo API:  http://localhost:5002/swagger" -ForegroundColor Cyan
Write-Host "   Vendas API:    http://localhost:5003/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Management:" -ForegroundColor Green
Write-Host "   RabbitMQ:      http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
Write-Host ""

# Comandos úteis
Write-Step "Comandos Úteis"
Write-Host ""
Write-Host "📋 Ver logs de um pod:" -ForegroundColor Yellow
Write-Host "   kubectl logs -f <pod-name> -n thethroneofgames" -ForegroundColor Gray
Write-Host ""
Write-Host "📊 Ver métricas dos pods:" -ForegroundColor Yellow
Write-Host "   kubectl top pods -n thethroneofgames" -ForegroundColor Gray
Write-Host ""
Write-Host "🔄 Restart de um deployment:" -ForegroundColor Yellow
Write-Host "   kubectl rollout restart deployment/<deployment-name> -n thethroneofgames" -ForegroundColor Gray
Write-Host ""
Write-Host "🗑️  Deletar tudo:" -ForegroundColor Yellow
Write-Host "   kubectl delete namespace thethroneofgames" -ForegroundColor Gray
Write-Host ""

# Watch logs se solicitado
if ($WatchLogs) {
    Write-Step "Assistindo logs (Ctrl+C para parar)"
    kubectl logs -f -l tier=backend -n thethroneofgames --all-containers=true
}

Write-Step "Deploy Completo!"
Write-Success "Aplicação rodando em Kubernetes"
