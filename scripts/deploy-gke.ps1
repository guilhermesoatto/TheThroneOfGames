# Deploy completo para GKE (Google Kubernetes Engine)
# Cluster: autopilot-cluster-1
# Region: southamerica-east1
# Project: project-62120210-43eb-4d93-954

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipPush = $false,
    [string]$ImageTag = "latest"
)

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  DEPLOY PARA GKE - THE THRONE OF GAMES" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$ErrorActionPreference = "Stop"

# Configurações
$PROJECT_ID = "project-62120210-43eb-4d93-954"
$CLUSTER_NAME = "autopilot-cluster-1"
$REGION = "southamerica-east1"
$NAMESPACE = "thethroneofgames"
$REGISTRY = "gcr.io/$PROJECT_ID"

# Microservices
$SERVICES = @(
    @{Name="usuarios-api"; Path="GameStore.Usuarios.API"; Port=5001},
    @{Name="catalogo-api"; Path="GameStore.Catalogo.API"; Port=5002},
    @{Name="vendas-api"; Path="GameStore.Vendas.API"; Port=5003}
)

# ============================================
# 1. VERIFICAR AMBIENTE
# ============================================
Write-Host "`n[1/7] Verificando ambiente..." -ForegroundColor Yellow

# Verificar gcloud
try {
    $gcloudVersion = gcloud version --format="value(version)" 2>&1
    Write-Host "   ✓ gcloud CLI: $gcloudVersion" -ForegroundColor Green
} catch {
    Write-Host "   ✗ gcloud CLI não encontrado!" -ForegroundColor Red
    Write-Host "   Instale em: https://cloud.google.com/sdk/docs/install" -ForegroundColor Yellow
    exit 1
}

# Verificar autenticação
$account = gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>&1
if (-not $account) {
    Write-Host "   ✗ Não autenticado no GCP!" -ForegroundColor Red
    Write-Host "   Execute: gcloud auth login" -ForegroundColor Yellow
    exit 1
}
Write-Host "   ✓ Conta ativa: $account" -ForegroundColor Green

# Verificar projeto
$currentProject = gcloud config get-value project 2>$null
if ($currentProject -ne $PROJECT_ID) {
    Write-Host "   Configurando projeto: $PROJECT_ID" -ForegroundColor Cyan
    gcloud config set project $PROJECT_ID
}
Write-Host "   ✓ Projeto: $PROJECT_ID" -ForegroundColor Green

# Verificar kubectl
try {
    kubectl version --client --output=yaml 2>$null | Out-Null
    Write-Host "   ✓ kubectl instalado" -ForegroundColor Green
} catch {
    Write-Host "   ✗ kubectl não encontrado!" -ForegroundColor Red
    exit 1
}

# ============================================
# 2. CONECTAR AO CLUSTER
# ============================================
Write-Host "`n[2/7] Conectando ao cluster GKE..." -ForegroundColor Yellow

try {
    gcloud container clusters get-credentials $CLUSTER_NAME --region $REGION --project $PROJECT_ID 2>&1 | Out-Null
    Write-Host "   ✓ Conectado ao cluster: $CLUSTER_NAME" -ForegroundColor Green
    
    $nodes = kubectl get nodes --no-headers 2>&1 | Measure-Object
    Write-Host "   ✓ Nodes disponíveis: $($nodes.Count)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Erro ao conectar ao cluster!" -ForegroundColor Red
    Write-Host "   $_" -ForegroundColor Red
    exit 1
}

# ============================================
# 3. BUILD DAS IMAGENS DOCKER
# ============================================
if (-not $SkipBuild) {
    Write-Host "`n[3/7] Building imagens Docker..." -ForegroundColor Yellow
    
    foreach ($service in $SERVICES) {
        Write-Host "   Building $($service.Name)..." -ForegroundColor Cyan
        
        $imageName = "$REGISTRY/$($service.Name):$ImageTag"
        
        docker build `
            -t $imageName `
            -f "$($service.Path)/Dockerfile" `
            . 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ $($service.Name): Build OK" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Erro no build de $($service.Name)" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "`n[3/7] Build pulado (-SkipBuild)" -ForegroundColor Gray
}

# ============================================
# 4. PUSH PARA GOOGLE CONTAINER REGISTRY
# ============================================
if (-not $SkipPush) {
    Write-Host "`n[4/7] Enviando imagens para GCR..." -ForegroundColor Yellow
    
    # Configurar Docker auth
    gcloud auth configure-docker --quiet 2>&1 | Out-Null
    
    foreach ($service in $SERVICES) {
        Write-Host "   Pushing $($service.Name)..." -ForegroundColor Cyan
        
        $imageName = "$REGISTRY/$($service.Name):$ImageTag"
        
        docker push $imageName 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ $($service.Name): Push OK" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Erro no push de $($service.Name)" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "`n[4/7] Push pulado (-SkipPush)" -ForegroundColor Gray
}

# ============================================
# 5. CRIAR NAMESPACE
# ============================================
Write-Host "`n[5/7] Preparando namespace Kubernetes..." -ForegroundColor Yellow

$namespaceExists = kubectl get namespace $NAMESPACE 2>$null
if (-not $namespaceExists) {
    kubectl create namespace $NAMESPACE 2>&1 | Out-Null
    Write-Host "   ✓ Namespace criado: $NAMESPACE" -ForegroundColor Green
} else {
    Write-Host "   ✓ Namespace já existe: $NAMESPACE" -ForegroundColor Green
}

# ============================================
# 6. APLICAR MANIFESTOS KUBERNETES
# ============================================
Write-Host "`n[6/7] Aplicando manifestos Kubernetes..." -ForegroundColor Yellow

# Ordem de aplicação (importante!)
$manifestOrder = @(
    "k8s/namespaces.yaml",
    "k8s/configmaps.yaml",
    "k8s/secrets.yaml",
    "k8s/statefulsets/sqlserver.yaml",
    "k8s/statefulsets/rabbitmq.yaml",
    "k8s/deployments/usuarios-api.yaml",
    "k8s/deployments/catalogo-api.yaml",
    "k8s/deployments/vendas-api.yaml",
    "k8s/hpa.yaml",
    "k8s/network-policies.yaml",
    "k8s/ingress.yaml"
)

foreach ($manifest in $manifestOrder) {
    if (Test-Path $manifest) {
        $fileName = Split-Path $manifest -Leaf
        Write-Host "   Aplicando $fileName..." -ForegroundColor Cyan
        
        # Atualizar imagens nos deployments
        if ($manifest -like "*deployments*") {
            $content = Get-Content $manifest -Raw
            
            # Substituir image tags
            $content = $content -replace "image: thethroneofgames-usuarios-api:.*", "image: $REGISTRY/usuarios-api:$ImageTag"
            $content = $content -replace "image: thethroneofgames-catalogo-api:.*", "image: $REGISTRY/catalogo-api:$ImageTag"
            $content = $content -replace "image: thethroneofgames-vendas-api:.*", "image: $REGISTRY/vendas-api:$ImageTag"
            
            # Aplicar com pipe
            $content | kubectl apply -f - 2>&1 | Out-Null
        } else {
            kubectl apply -f $manifest 2>&1 | Out-Null
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ $fileName aplicado" -ForegroundColor Green
        } else {
            Write-Host "   ⚠ $fileName com avisos (pode ser normal)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠ $manifest não encontrado" -ForegroundColor Yellow
    }
}

# ============================================
# 7. AGUARDAR DEPLOYMENTS
# ============================================
Write-Host "`n[7/7] Aguardando pods ficarem prontos..." -ForegroundColor Yellow

foreach ($service in $SERVICES) {
    Write-Host "   Aguardando $($service.Name)..." -ForegroundColor Cyan
    
    kubectl rollout status deployment/$($service.Name) -n $NAMESPACE --timeout=5m 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ $($service.Name): READY" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ $($service.Name): timeout ou erro" -ForegroundColor Yellow
    }
}

# ============================================
# 8. STATUS FINAL
# ============================================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  DEPLOY CONCLUÍDO COM SUCESSO!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "📊 STATUS DO CLUSTER:" -ForegroundColor Yellow
kubectl get pods -n $NAMESPACE

Write-Host "`n🌐 SERVICES:" -ForegroundColor Yellow
kubectl get svc -n $NAMESPACE

Write-Host "`n📈 HPA:" -ForegroundColor Yellow
kubectl get hpa -n $NAMESPACE

Write-Host "`n🔗 INGRESS:" -ForegroundColor Yellow
$ingressIP = kubectl get ingress -n $NAMESPACE -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}' 2>$null
if ($ingressIP) {
    Write-Host "   IP Externo: http://$ingressIP" -ForegroundColor Green
    Write-Host "   Swagger: http://$ingressIP/swagger" -ForegroundColor Cyan
} else {
    Write-Host "   Ingress ainda não tem IP externo (aguarde alguns minutos)" -ForegroundColor Yellow
}

Write-Host "`n💡 COMANDOS ÚTEIS:" -ForegroundColor Yellow
Write-Host "   Ver logs:       kubectl logs -f deployment/usuarios-api -n $NAMESPACE" -ForegroundColor Gray
Write-Host "   Ver pods:       kubectl get pods -n $NAMESPACE -w" -ForegroundColor Gray
Write-Host "   Ver eventos:    kubectl get events -n $NAMESPACE --sort-by='.lastTimestamp'" -ForegroundColor Gray
Write-Host "   Escalar:        kubectl scale deployment/usuarios-api --replicas=5 -n $NAMESPACE" -ForegroundColor Gray
Write-Host "   Port-forward:   kubectl port-forward svc/usuarios-api 5001:80 -n $NAMESPACE`n" -ForegroundColor Gray

$timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'
Write-Host "✅ Deploy finalizado em: $timestamp" -ForegroundColor Green
