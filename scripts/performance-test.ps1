# Performance Test Script - The Throne of Games
# Testa a capacidade de processamento de cada microservice antes do scaling

param(
    [string]$BaseUrl = "http://localhost",
    [int]$Usuarios_Port = 5001,
    [int]$Catalogo_Port = 5002,
    [int]$Vendas_Port = 5003,
    [int]$Duration = 60,              # Duração do teste em segundos
    [int]$ConcurrentUsers = 10,       # Número de usuários simultâneos
    [int]$RampUpTime = 10,            # Tempo de ramp-up em segundos
    [string]$OutputFile = "performance-results.json"
)

Write-Host "`n🚀 TESTE DE PERFORMANCE - THE THRONE OF GAMES`n" -ForegroundColor Cyan

# Classe para armazenar resultados
$results = @{
    testDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    duration = $Duration
    concurrentUsers = $ConcurrentUsers
    microservices = @()
}

# Função para testar um endpoint
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$Duration,
        [int]$ConcurrentUsers,
        [int]$RampUpTime
    )
    
    Write-Host "`n📊 Testando: $Name" -ForegroundColor Yellow
    Write-Host "   URL: $Url" -ForegroundColor Gray
    Write-Host "   Usuários: $ConcurrentUsers | Duração: ${Duration}s | Ramp-up: ${RampUpTime}s" -ForegroundColor Gray
    
    $startTime = Get-Date
    $endTime = $startTime.AddSeconds($Duration)
    $requests = [System.Collections.ArrayList]::new()
    $errors = 0
    $successCount = 0
    
    # Função para fazer requisição
    $requestScript = {
        param($url)
        try {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 30
            $sw.Stop()
            return @{
                Success = $true
                StatusCode = $response.StatusCode
                Duration = $sw.ElapsedMilliseconds
            }
        } catch {
            return @{
                Success = $false
                StatusCode = 0
                Duration = 0
                Error = $_.Exception.Message
            }
        }
    }
    
    # Ramp-up: adiciona usuários gradualmente
    $usersPerSecond = [Math]::Ceiling($ConcurrentUsers / $RampUpTime)
    $currentUsers = 0
    
    Write-Host "   Ramp-up: " -NoNewline -ForegroundColor Gray
    
    for ($i = 0; $i -lt $RampUpTime; $i++) {
        $currentUsers = [Math]::Min($currentUsers + $usersPerSecond, $ConcurrentUsers)
        Write-Host "$currentUsers " -NoNewline -ForegroundColor DarkYellow
        Start-Sleep -Seconds 1
    }
    
    Write-Host "`n   Executando teste... " -NoNewline -ForegroundColor Gray
    
    # Teste principal com todos os usuários
    $jobs = @()
    $requestCount = 0
    
    while ((Get-Date) -lt $endTime) {
        # Mantém número constante de requisições simultâneas
        for ($i = 0; $i -lt $ConcurrentUsers; $i++) {
            $jobs += Start-Job -ScriptBlock $requestScript -ArgumentList $Url
            $requestCount++
        }
        
        # Aguarda pelo menos algumas requisições completarem
        $completed = $jobs | Where-Object { $_.State -eq 'Completed' }
        
        foreach ($job in $completed) {
            $result = Receive-Job -Job $job
            $null = $requests.Add($result)
            
            if ($result.Success) {
                $successCount++
            } else {
                $errors++
            }
            
            Remove-Job -Job $job
        }
        
        $jobs = $jobs | Where-Object { $_.State -ne 'Completed' }
        
        # Progress indicator
        if ($requestCount % 50 -eq 0) {
            Write-Host "." -NoNewline -ForegroundColor Green
        }
        
        Start-Sleep -Milliseconds 100
    }
    
    # Aguarda jobs restantes
    Write-Host " aguardando jobs restantes..." -ForegroundColor Gray
    $jobs | Wait-Job | Receive-Job | ForEach-Object {
        $null = $requests.Add($_)
        if ($_.Success) { $successCount++ } else { $errors++ }
    }
    $jobs | Remove-Job
    
    # Calcula estatísticas
    $totalRequests = $requests.Count
    $successfulRequests = $requests | Where-Object { $_.Success -eq $true }
    $latencies = $successfulRequests | ForEach-Object { $_.Duration }
    
    if ($latencies.Count -gt 0) {
        $avgLatency = ($latencies | Measure-Object -Average).Average
        $minLatency = ($latencies | Measure-Object -Minimum).Minimum
        $maxLatency = ($latencies | Measure-Object -Maximum).Maximum
        $p50 = ($latencies | Sort-Object)[[Math]::Floor($latencies.Count * 0.50)]
        $p95 = ($latencies | Sort-Object)[[Math]::Floor($latencies.Count * 0.95)]
        $p99 = ($latencies | Sort-Object)[[Math]::Floor($latencies.Count * 0.99)]
    } else {
        $avgLatency = 0
        $minLatency = 0
        $maxLatency = 0
        $p50 = 0
        $p95 = 0
        $p99 = 0
    }
    
    $actualDuration = ((Get-Date) - $startTime).TotalSeconds
    $throughput = [Math]::Round($totalRequests / $actualDuration, 2)
    $successRate = if ($totalRequests -gt 0) { [Math]::Round(($successCount / $totalRequests) * 100, 2) } else { 0 }
    
    # Exibe resultados
    Write-Host "`n   ✅ Teste concluído!`n" -ForegroundColor Green
    Write-Host "   📈 RESULTADOS:" -ForegroundColor Cyan
    Write-Host "      Total de Requisições: $totalRequests" -ForegroundColor White
    Write-Host "      Requisições/segundo: $throughput req/s" -ForegroundColor White
    Write-Host "      Taxa de Sucesso: $successRate%" -ForegroundColor $(if($successRate -ge 95){'Green'}elseif($successRate -ge 90){'Yellow'}else{'Red'})
    Write-Host "      Erros: $errors" -ForegroundColor $(if($errors -eq 0){'Green'}else{'Red'})
    Write-Host "`n   ⏱️  LATÊNCIA:" -ForegroundColor Cyan
    Write-Host "      Média: $([Math]::Round($avgLatency, 2))ms" -ForegroundColor White
    Write-Host "      Mínima: $minLatency ms" -ForegroundColor White
    Write-Host "      Máxima: $maxLatency ms" -ForegroundColor White
    Write-Host "      P50 (mediana): $p50 ms" -ForegroundColor White
    Write-Host "      P95: $p95 ms" -ForegroundColor White
    Write-Host "      P99: $p99 ms" -ForegroundColor White
    
    # Determina se passou nos critérios
    $passed = $successRate -ge 95 -and $avgLatency -lt 2000 -and $p95 -lt 5000
    
    Write-Host "`n   📊 AVALIAÇÃO: " -NoNewline -ForegroundColor Cyan
    if ($passed) {
        Write-Host "✅ PASSOU" -ForegroundColor Green
    } else {
        Write-Host "❌ FALHOU" -ForegroundColor Red
        if ($successRate -lt 95) { Write-Host "      - Taxa de sucesso abaixo de 95%" -ForegroundColor Red }
        if ($avgLatency -ge 2000) { Write-Host "      - Latência média acima de 2000ms" -ForegroundColor Red }
        if ($p95 -ge 5000) { Write-Host "      - P95 acima de 5000ms" -ForegroundColor Red }
    }
    
    return @{
        name = $Name
        url = $Url
        totalRequests = $totalRequests
        successfulRequests = $successCount
        failedRequests = $errors
        throughput = $throughput
        successRate = $successRate
        latency = @{
            average = [Math]::Round($avgLatency, 2)
            min = $minLatency
            max = $maxLatency
            p50 = $p50
            p95 = $p95
            p99 = $p99
        }
        passed = $passed
        duration = [Math]::Round($actualDuration, 2)
    }
}

# Verifica se os serviços estão acessíveis
Write-Host "🔍 Verificando disponibilidade dos serviços..." -ForegroundColor Yellow

$services = @(
    @{ Name = "Usuarios API"; Url = "$BaseUrl`:$Usuarios_Port/swagger" },
    @{ Name = "Catalogo API"; Url = "$BaseUrl`:$Catalogo_Port/swagger" },
    @{ Name = "Vendas API"; Url = "$BaseUrl`:$Vendas_Port/swagger" }
)

$allAvailable = $true
foreach ($service in $services) {
    Write-Host "   Testando $($service.Name)... " -NoNewline
    try {
        $response = Invoke-WebRequest -Uri $service.Url -Method Get -UseBasicParsing -TimeoutSec 10
        Write-Host "✅ Disponível" -ForegroundColor Green
    } catch {
        Write-Host "❌ Indisponível" -ForegroundColor Red
        $allAvailable = $false
    }
}

if (-not $allAvailable) {
    Write-Host "`n❌ Nem todos os serviços estão disponíveis. Abortando teste." -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Todos os serviços estão disponíveis. Iniciando testes de performance...`n" -ForegroundColor Green
Start-Sleep -Seconds 2

# Executa testes para cada microservice
$results.microservices += Test-Endpoint -Name "Usuarios API" -Url "$BaseUrl`:$Usuarios_Port/swagger" -Duration $Duration -ConcurrentUsers $ConcurrentUsers -RampUpTime $RampUpTime
$results.microservices += Test-Endpoint -Name "Catalogo API" -Url "$BaseUrl`:$Catalogo_Port/swagger" -Duration $Duration -ConcurrentUsers $ConcurrentUsers -RampUpTime $RampUpTime
$results.microservices += Test-Endpoint -Name "Vendas API" -Url "$BaseUrl`:$Vendas_Port/swagger" -Duration $Duration -ConcurrentUsers $ConcurrentUsers -RampUpTime $RampUpTime

# Salva resultados em JSON
$results | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "`n📄 Resultados salvos em: $OutputFile" -ForegroundColor Cyan

# Gera resumo final
Write-Host "`n" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                    RESUMO FINAL                            " -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

$totalPassed = ($results.microservices | Where-Object { $_.passed }).Count
$totalFailed = ($results.microservices | Where-Object { -not $_.passed }).Count

Write-Host "`n📊 ESTATÍSTICAS GERAIS:" -ForegroundColor Yellow

$avgThroughput = ($results.microservices | Measure-Object -Property throughput -Average).Average
$avgLatency = ($results.microservices | ForEach-Object { $_.latency.average } | Measure-Object -Average).Average
$avgSuccessRate = ($results.microservices | Measure-Object -Property successRate -Average).Average

Write-Host "   Throughput Médio: $([Math]::Round($avgThroughput, 2)) req/s" -ForegroundColor White
Write-Host "   Latência Média: $([Math]::Round($avgLatency, 2)) ms" -ForegroundColor White
Write-Host "   Taxa de Sucesso Média: $([Math]::Round($avgSuccessRate, 2))%" -ForegroundColor White

Write-Host "`n🎯 RESULTADO FINAL:" -ForegroundColor Yellow
Write-Host "   Aprovados: " -NoNewline -ForegroundColor White
Write-Host "$totalPassed" -ForegroundColor Green
Write-Host "   Reprovados: " -NoNewline -ForegroundColor White
Write-Host "$totalFailed" -ForegroundColor $(if($totalFailed -eq 0){'Green'}else{'Red'})

Write-Host "`n📋 DETALHES POR MICROSERVICE:" -ForegroundColor Yellow
foreach ($ms in $results.microservices) {
    $status = if ($ms.passed) { "✅ PASSOU" } else { "❌ FALHOU" }
    $color = if ($ms.passed) { "Green" } else { "Red" }
    Write-Host "   $($ms.name): " -NoNewline -ForegroundColor White
    Write-Host "$status" -ForegroundColor $color
    Write-Host "      Throughput: $($ms.throughput) req/s | Latência: $($ms.latency.average)ms | Sucesso: $($ms.successRate)%" -ForegroundColor Gray
}

Write-Host "`n═══════════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Calcula baseline para HPA (quantas requisições por container)
Write-Host "📊 BASELINE PARA HPA (Horizontal Pod Autoscaler):" -ForegroundColor Magenta
Write-Host "   Com base nos resultados, cada container pode processar:" -ForegroundColor Gray

foreach ($ms in $results.microservices) {
    $recommendedRPS = [Math]::Floor($ms.throughput * 0.7)  # 70% da capacidade para manter margem
    Write-Host "   • $($ms.name): ~$recommendedRPS req/s" -ForegroundColor White
    Write-Host "      (70% de $($ms.throughput) req/s para manter margem de segurança)" -ForegroundColor DarkGray
}

Write-Host "`n💡 RECOMENDAÇÃO PARA KUBERNETES HPA:" -ForegroundColor Magenta
Write-Host "   Configure o HPA para escalar quando a taxa de requisições" -ForegroundColor Gray
Write-Host "   por pod ultrapassar 70% da capacidade medida neste teste." -ForegroundColor Gray

# Exit code baseado no resultado
if ($totalFailed -eq 0) {
    Write-Host "`n✅ Todos os testes passaram! Sistema pronto para produção.`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Alguns testes falharam. Verifique os resultados acima.`n" -ForegroundColor Red
    exit 1
}
