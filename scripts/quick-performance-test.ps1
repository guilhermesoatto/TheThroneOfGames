# Quick Performance Test - Versão rápida para CI/CD
# Testa capacidade básica de cada microservice em menos tempo

param(
    [string]$BaseUrl = "http://localhost",
    [int]$Duration = 30,              # Apenas 30 segundos para CI/CD
    [int]$ConcurrentUsers = 5,        # Menos usuários para CI/CD
    [string]$OutputFile = "quick-performance-results.json"
)

Write-Host "`nTESTE RAPIDO DE PERFORMANCE - CI/CD`n" -ForegroundColor Cyan

$results = @{
    testDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    duration = $Duration
    concurrentUsers = $ConcurrentUsers
    environment = "CI/CD"
    microservices = @()
}

# Função simplificada para teste rápido
function Test-ServiceQuick {
    param(
        [string]$Name,
        [string]$Url,
        [int]$Duration,
        [int]$Users
    )
    
    Write-Host "Testando: $Name ($Duration segundos, $Users usuarios)" -ForegroundColor Yellow
    
    $startTime = Get-Date
    $endTime = $startTime.AddSeconds($Duration)
    $successCount = 0
    $errorCount = 0
    $latencies = @()
    
    while ((Get-Date) -lt $endTime) {
        $jobs = @()
        
        # Faz requisições em paralelo
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
        
        # Coleta resultados
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
    
    Write-Host "   Throughput: $throughput req/s | Sucesso: $successRate% | Latencia: ${avgLatency}ms | " -NoNewline -ForegroundColor White
    Write-Host $(if($passed){"PASSOU"}else{"FALHOU"}) -ForegroundColor $(if($passed){"Green"}else{"Red"})
    
    return @{
        name = $Name
        url = $Url
        totalRequests = $totalRequests
        successfulRequests = $successCount
        failedRequests = $errorCount
        throughput = $throughput
        successRate = $successRate
        latency = @{
            average = $avgLatency
            max = $maxLatency
        }
        passed = $passed
    }
}

# Testa cada microservice
$services = @(
    @{ Name = "Usuarios API"; Port = 5001 },
    @{ Name = "Catalogo API"; Port = 5002 },
    @{ Name = "Vendas API"; Port = 5003 }
)

foreach ($service in $services) {
    $url = "$BaseUrl`:$($service.Port)/swagger"
    $results.microservices += Test-ServiceQuick -Name $service.Name -Url $url -Duration $Duration -Users $ConcurrentUsers
}

# Salva resultados
$results | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputFile -Encoding UTF8

# Resumo
Write-Host "`nRESUMO:" -ForegroundColor Cyan
$totalPassed = ($results.microservices | Where-Object { $_.passed }).Count
$totalFailed = $results.microservices.Count - $totalPassed

Write-Host "   Aprovados: $totalPassed | Reprovados: $totalFailed" -ForegroundColor White

if ($totalFailed -eq 0) {
    Write-Host "`nTodos os testes passaram!`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n$totalFailed teste(s) falharam!`n" -ForegroundColor Red
    exit 1
}
