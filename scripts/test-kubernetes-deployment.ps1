# Test Kubernetes Deployment - The Throne of Games
# Executa testes de performance contra o cluster GKE

param(
    [string]$ClusterName = "autopilot-cluster-1",
    [string]$Region = "southamerica-east1",
    [string]$ProjectId = "project-62120210-43eb-4d93-954",
    [string]$Namespace = "thethroneofgames",
    [int]$Duration = 30,
    [int]$ConcurrentUsers = 5
)

Write-Host "`n=== TESTE DE DEPLOYMENT KUBERNETES ===" -ForegroundColor Cyan
Write-Host "Cluster: $ClusterName" -ForegroundColor White
Write-Host "Region: $Region" -ForegroundColor White
Write-Host "Project: $ProjectId" -ForegroundColor White
Write-Host "Namespace: $Namespace`n" -ForegroundColor White

# 1. Verificar se gcloud está instalado
Write-Host "1. Verificando gcloud CLI..." -ForegroundColor Yellow
try {
    $gcloudVersion = gcloud version 2>&1 | Select-String "Google Cloud SDK" | Out-String
    Write-Host "   gcloud instalado" -ForegroundColor Green
} catch {
    Write-Host "   ERRO: gcloud CLI não encontrado!" -ForegroundColor Red
    Write-Host "   Instale: https://cloud.google.com/sdk/docs/install" -ForegroundColor Yellow
    exit 1
}

# 2. Verificar se kubectl está instalado
Write-Host "`n2. Verificando kubectl..." -ForegroundColor Yellow
try {
    $kubectlVersion = kubectl version --client --short 2>&1
    Write-Host "   kubectl instalado" -ForegroundColor Green
} catch {
    Write-Host "   ERRO: kubectl não encontrado!" -ForegroundColor Red
    Write-Host "   Instale: gcloud components install kubectl" -ForegroundColor Yellow
    exit 1
}

# 3. Autenticar e conectar ao cluster
Write-Host "`n3. Conectando ao cluster GKE..." -ForegroundColor Yellow
try {
    $getCredentials = gcloud container clusters get-credentials $ClusterName `
        --region $Region `
        --project $ProjectId 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Conectado ao cluster com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "   ERRO ao conectar ao cluster" -ForegroundColor Red
        Write-Host "   $getCredentials" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "   ERRO: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 4. Verificar namespace
Write-Host "`n4. Verificando namespace '$Namespace'..." -ForegroundColor Yellow
$namespaceExists = kubectl get namespace $Namespace 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "   Namespace '$Namespace' não encontrado" -ForegroundColor Red
    Write-Host "   Usando namespace 'default'" -ForegroundColor Yellow
    $Namespace = "default"
} else {
    Write-Host "   Namespace '$Namespace' encontrado" -ForegroundColor Green
}

# 5. Listar pods
Write-Host "`n5. Status dos Pods:" -ForegroundColor Yellow
kubectl get pods -n $Namespace
Write-Host ""

# 6. Verificar serviços
Write-Host "6. Status dos Services:" -ForegroundColor Yellow
$services = kubectl get services -n $Namespace -o json | ConvertFrom-Json

$usuariosService = $services.items | Where-Object { $_.metadata.name -like "*usuarios*" } | Select-Object -First 1
$catalogoService = $services.items | Where-Object { $_.metadata.name -like "*catalogo*" } | Select-Object -First 1
$vendasService = $services.items | Where-Object { $_.metadata.name -like "*vendas*" } | Select-Object -First 1

if (-not $usuariosService) {
    Write-Host "   AVISO: Service usuarios-api não encontrado" -ForegroundColor Yellow
}
if (-not $catalogoService) {
    Write-Host "   AVISO: Service catalogo-api não encontrado" -ForegroundColor Yellow
}
if (-not $vendasService) {
    Write-Host "   AVISO: Service vendas-api não encontrado" -ForegroundColor Yellow
}

kubectl get services -n $Namespace
Write-Host ""

# 7. Verificar Ingress
Write-Host "7. Verificando Ingress:" -ForegroundColor Yellow
$ingress = kubectl get ingress -n $Namespace -o json 2>$null | ConvertFrom-Json
if ($ingress.items.Count -gt 0) {
    $ingressIp = $ingress.items[0].status.loadBalancer.ingress[0].ip
    if ($ingressIp) {
        Write-Host "   Ingress IP: $ingressIp" -ForegroundColor Green
        $baseUrl = "http://$ingressIp"
    } else {
        Write-Host "   Ingress ainda não tem IP público (pode levar alguns minutos)" -ForegroundColor Yellow
        $usePortForward = $true
    }
} else {
    Write-Host "   Nenhum Ingress encontrado" -ForegroundColor Yellow
    $usePortForward = $true
}

# 8. Port-forward ou usar Ingress
if ($usePortForward) {
    Write-Host "`n8. Configurando Port-Forward..." -ForegroundColor Yellow
    Write-Host "   Iniciando port-forwards em background..." -ForegroundColor Gray
    
    # Port-forward para cada serviço
    $portForwardJobs = @()
    
    # Usuarios API - porta 5001
    if ($usuariosService) {
        $usuariosPort = $usuariosService.spec.ports[0].port
        Write-Host "   Port-forward usuarios-api: localhost:5001 -> service:$usuariosPort" -ForegroundColor Gray
        $job1 = Start-Job -ScriptBlock {
            param($ns, $svc)
            kubectl port-forward -n $ns service/$svc 5001:$using:usuariosPort
        } -ArgumentList $Namespace, $usuariosService.metadata.name
        $portForwardJobs += $job1
        Start-Sleep -Seconds 2
    }
    
    # Catalogo API - porta 5002
    if ($catalogoService) {
        $catalogoPort = $catalogoService.spec.ports[0].port
        Write-Host "   Port-forward catalogo-api: localhost:5002 -> service:$catalogoPort" -ForegroundColor Gray
        $job2 = Start-Job -ScriptBlock {
            param($ns, $svc)
            kubectl port-forward -n $ns service/$svc 5002:$using:catalogoPort
        } -ArgumentList $Namespace, $catalogoService.metadata.name
        $portForwardJobs += $job2
        Start-Sleep -Seconds 2
    }
    
    # Vendas API - porta 5003
    if ($vendasService) {
        $vendasPort = $vendasService.spec.ports[0].port
        Write-Host "   Port-forward vendas-api: localhost:5003 -> service:$vendasPort" -ForegroundColor Gray
        $job3 = Start-Job -ScriptBlock {
            param($ns, $svc)
            kubectl port-forward -n $ns service/$svc 5003:$using:vendasPort
        } -ArgumentList $Namespace, $vendasService.metadata.name
        $portForwardJobs += $job3
        Start-Sleep -Seconds 2
    }
    
    Write-Host "   Aguardando port-forwards ficarem ativos..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
    
    $baseUrl = "http://localhost"
} else {
    Write-Host "`n8. Usando Ingress para testes..." -ForegroundColor Yellow
    Write-Host "   Base URL: $baseUrl" -ForegroundColor Green
}

# 9. Executar testes de performance
Write-Host "`n9. Executando testes de performance..." -ForegroundColor Yellow
Write-Host "   Duração: $Duration segundos" -ForegroundColor Gray
Write-Host "   Usuários concorrentes: $ConcurrentUsers" -ForegroundColor Gray
Write-Host ""

$testResults = @{
    timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    cluster = $ClusterName
    region = $Region
    baseUrl = $baseUrl
    microservices = @()
}

# Função para testar endpoint
function Test-KubernetesEndpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$Duration,
        [int]$Users
    )
    
    Write-Host "   Testando $Name..." -ForegroundColor Cyan
    
    $startTime = Get-Date
    $endTime = $startTime.AddSeconds($Duration)
    $successCount = 0
    $errorCount = 0
    $latencies = @()
    
    while ((Get-Date) -lt $endTime) {
        $jobs = @()
        
        for ($i = 0; $i -lt $Users; $i++) {
            $jobs += Start-Job -ScriptBlock {
                param($url)
                try {
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
                    $sw.Stop()
                    return @{ Success = $true; Duration = $sw.ElapsedMilliseconds }
                } catch {
                    return @{ Success = $false; Duration = 0 }
                }
            } -ArgumentList $Url
        }
        
        $jobs | Wait-Job | Receive-Job | ForEach-Object {
            if ($_.Success) {
                $successCount++
                $latencies += $_.Duration
            } else {
                $errorCount++
            }
        }
        $jobs | Remove-Job
        
        Start-Sleep -Milliseconds 500
    }
    
    $totalRequests = $successCount + $errorCount
    $actualDuration = ((Get-Date) - $startTime).TotalSeconds
    $throughput = [Math]::Round($totalRequests / $actualDuration, 2)
    $successRate = if ($totalRequests -gt 0) { [Math]::Round(($successCount / $totalRequests) * 100, 2) } else { 0 }
    
    $avgLatency = if ($latencies.Count -gt 0) { [Math]::Round(($latencies | Measure-Object -Average).Average, 2) } else { 0 }
    $maxLatency = if ($latencies.Count -gt 0) { ($latencies | Measure-Object -Maximum).Maximum } else { 0 }
    
    $passed = $successRate -ge 90 -and $avgLatency -lt 3000
    
    Write-Host "      Throughput: $throughput req/s | Sucesso: $successRate% | Latencia: ${avgLatency}ms" -ForegroundColor White
    Write-Host "      Status: $(if($passed){'PASSOU'}else{'FALHOU'})" -ForegroundColor $(if($passed){"Green"}else{"Red"})
    
    return @{
        name = $Name
        url = $Url
        totalRequests = $totalRequests
        successfulRequests = $successCount
        failedRequests = $errorCount
        throughput = $throughput
        successRate = $successRate
        avgLatency = $avgLatency
        maxLatency = $maxLatency
        passed = $passed
    }
}

# Testar cada microservice
if ($usuariosService -or (-not $usePortForward)) {
    $usuariosUrl = if ($usePortForward) { "$baseUrl:5001/swagger" } else { "$baseUrl/usuarios/swagger" }
    $testResults.microservices += Test-KubernetesEndpoint -Name "Usuarios API" -Url $usuariosUrl -Duration $Duration -Users $ConcurrentUsers
}

if ($catalogoService -or (-not $usePortForward)) {
    $catalogoUrl = if ($usePortForward) { "$baseUrl:5002/swagger" } else { "$baseUrl/catalogo/swagger" }
    $testResults.microservices += Test-KubernetesEndpoint -Name "Catalogo API" -Url $catalogoUrl -Duration $Duration -Users $ConcurrentUsers
}

if ($vendasService -or (-not $usePortForward)) {
    $vendasUrl = if ($usePortForward) { "$baseUrl:5003/swagger" } else { "$baseUrl/vendas/swagger" }
    $testResults.microservices += Test-KubernetesEndpoint -Name "Vendas API" -Url $vendasUrl -Duration $Duration -Users $ConcurrentUsers
}

# 10. Salvar resultados
Write-Host "`n10. Salvando resultados..." -ForegroundColor Yellow
$resultsFile = "kubernetes-test-results.json"
$testResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $resultsFile -Encoding UTF8
Write-Host "    Resultados salvos em: $resultsFile" -ForegroundColor Green

# 11. Resumo final
Write-Host "`n=== RESUMO DOS TESTES ===" -ForegroundColor Cyan
$totalPassed = ($testResults.microservices | Where-Object { $_.passed }).Count
$totalFailed = $testResults.microservices.Count - $totalPassed

Write-Host "Aprovados: $totalPassed | Reprovados: $totalFailed" -ForegroundColor White

foreach ($result in $testResults.microservices) {
    $status = if ($result.passed) { "PASSOU" } else { "FALHOU" }
    $color = if ($result.passed) { "Green" } else { "Red" }
    Write-Host "   $($result.name): $status" -ForegroundColor $color
}

# 12. Verificar HPA
Write-Host "`n12. Status do HPA (Horizontal Pod Autoscaler):" -ForegroundColor Yellow
kubectl get hpa -n $Namespace

# 13. Cleanup
if ($usePortForward -and $portForwardJobs.Count -gt 0) {
    Write-Host "`n13. Limpando port-forwards..." -ForegroundColor Yellow
    $portForwardJobs | Stop-Job
    $portForwardJobs | Remove-Job
    Write-Host "    Port-forwards encerrados" -ForegroundColor Green
}

# Resultado final
Write-Host ""
if ($totalFailed -eq 0) {
    Write-Host "=== SUCESSO: Todos os testes passaram! ===" -ForegroundColor Green
    exit 0
} else {
    Write-Host "=== FALHA: $totalFailed teste(s) falharam ===" -ForegroundColor Red
    exit 1
}
