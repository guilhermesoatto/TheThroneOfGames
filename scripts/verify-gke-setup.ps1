# Quick Verification Script - GKE Setup
# Verifica se gcloud, kubectl e acesso ao cluster estao configurados

Write-Host "`n=== VERIFICACAO DE AMBIENTE GKE ===`n" -ForegroundColor Cyan

$allGood = $true

# 1. Verificar gcloud
Write-Host "1. Verificando gcloud CLI..." -ForegroundColor Yellow
try {
    $gcloudVersion = gcloud version 2>&1 | Select-String "Google Cloud SDK"
    if ($gcloudVersion) {
        Write-Host "   gcloud: OK" -ForegroundColor Green
        gcloud version --format="value(version)"
    } else {
        Write-Host "   gcloud: ERRO" -ForegroundColor Red
        $allGood = $false
    }
} catch {
    Write-Host "   gcloud: NAO ENCONTRADO" -ForegroundColor Red
    $allGood = $false
}

# 2. Verificar kubectl
Write-Host "`n2. Verificando kubectl..." -ForegroundColor Yellow
try {
    $kubectlVersion = kubectl version --client --short 2>&1
    Write-Host "   kubectl: OK" -ForegroundColor Green
    kubectl version --client --output=yaml 2>$null | Select-String "gitVersion"
} catch {
    Write-Host "   kubectl: NAO ENCONTRADO" -ForegroundColor Red
    $allGood = $false
}

# 3. Verificar autenticacao GCP
Write-Host "`n3. Verificando autenticacao GCP..." -ForegroundColor Yellow
try {
    $account = gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>&1
    if ($account) {
        Write-Host "   Conta ativa: $account" -ForegroundColor Green
    } else {
        Write-Host "   Nenhuma conta ativa!" -ForegroundColor Red
        Write-Host "   Execute: gcloud auth login" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "   Erro ao verificar autenticacao" -ForegroundColor Red
    $allGood = $false
}

# 4. Verificar projeto configurado
Write-Host "`n4. Verificando projeto GCP..." -ForegroundColor Yellow
try {
    $project = gcloud config get-value project 2>$null
    if ($project) {
        Write-Host "   Projeto ativo: $project" -ForegroundColor Green
    } else {
        Write-Host "   Nenhum projeto configurado!" -ForegroundColor Red
        Write-Host "   Execute: gcloud config set project project-62120210-43eb-4d93-954" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "   Erro ao verificar projeto" -ForegroundColor Red
    $allGood = $false
}

# 5. Listar clusters disponiveis
Write-Host "`n5. Verificando clusters GKE..." -ForegroundColor Yellow
try {
    $clusters = gcloud container clusters list --format="table(name,location,status)" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host $clusters
    } else {
        Write-Host "   Nenhum cluster encontrado ou erro ao listar" -ForegroundColor Yellow
        Write-Host "   Certifique-se de que o cluster existe na regiao southamerica-east1" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Erro ao listar clusters" -ForegroundColor Red
}

# 6. Verificar conexao ao cluster
Write-Host "`n6. Verificando conexao ao cluster..." -ForegroundColor Yellow
try {
    $currentContext = kubectl config current-context 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Contexto atual: $currentContext" -ForegroundColor Green
        
        # Tentar acessar o cluster
        Write-Host "`n7. Testando acesso ao cluster..." -ForegroundColor Yellow
        $nodes = kubectl get nodes 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Acesso ao cluster: OK" -ForegroundColor Green
            Write-Host "`n   Nodes:"
            kubectl get nodes
        } else {
            Write-Host "   Nao foi possivel acessar o cluster" -ForegroundColor Red
            Write-Host "   Execute: gcloud container clusters get-credentials autopilot-cluster-1 --region southamerica-east1" -ForegroundColor Yellow
            $allGood = $false
        }
    } else {
        Write-Host "   Nenhum cluster configurado no kubectl" -ForegroundColor Red
        Write-Host "   Execute: gcloud container clusters get-credentials autopilot-cluster-1 --region southamerica-east1" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "   Erro ao verificar contexto kubectl" -ForegroundColor Red
    $allGood = $false
}

# 8. Verificar namespace thethroneofgames
Write-Host "`n8. Verificando namespace 'thethroneofgames'..." -ForegroundColor Yellow
try {
    $namespace = kubectl get namespace thethroneofgames 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Namespace encontrado" -ForegroundColor Green
        
        # Listar recursos no namespace
        Write-Host "`n9. Recursos no namespace:" -ForegroundColor Yellow
        Write-Host "   Pods:"
        kubectl get pods -n thethroneofgames 2>$null
        Write-Host "`n   Services:"
        kubectl get services -n thethroneofgames 2>$null
        Write-Host "`n   HPA:"
        kubectl get hpa -n thethroneofgames 2>$null
        Write-Host "`n   Ingress:"
        kubectl get ingress -n thethroneofgames 2>$null
    } else {
        Write-Host "   Namespace nao encontrado" -ForegroundColor Yellow
        Write-Host "   O namespace sera criado no primeiro deploy" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Erro ao verificar namespace" -ForegroundColor Yellow
}

# Resumo final
Write-Host "`n=== RESUMO ===" -ForegroundColor Cyan
if ($allGood) {
    Write-Host "TUDO OK! Ambiente configurado corretamente." -ForegroundColor Green
    Write-Host "`nVoce pode executar:" -ForegroundColor Yellow
    Write-Host "  .\scripts\test-kubernetes-deployment.ps1" -ForegroundColor White
} else {
    Write-Host "ATENCAO! Alguns problemas foram encontrados." -ForegroundColor Red
    Write-Host "`nPara corrigir, execute os comandos sugeridos acima." -ForegroundColor Yellow
}

Write-Host ""
