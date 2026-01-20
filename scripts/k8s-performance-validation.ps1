#!/usr/bin/env pwsh
# Simple Performance Test - K8s Post-Deploy Validation
# Validates deployed services health and basic performance

param(
    [string]$Namespace = "thethroneofgames",
    [int]$Duration = 30,
    [int]$Requests = 10
)

Write-Host "`n=== K8S PERFORMANCE VALIDATION ===" -ForegroundColor Cyan
Write-Host "Namespace: $Namespace | Duration: ${Duration}s | Requests: $Requests`n"

$services = @(
    @{Name="usuarios-api"; Port=30001},
    @{Name="catalogo-api"; Port=30002},
    @{Name="vendas-api"; Port=30003}
)

$results = @()

foreach ($svc in $services) {
    Write-Host "Testing $($svc.Name)..." -ForegroundColor Yellow
    
    $nodeIP = (kubectl get nodes -o jsonpath='{.items[0].status.addresses[?(@.type=="ExternalIP")].address}' 2>$null)
    if ([string]::IsNullOrEmpty($nodeIP)) {
        $nodeIP = (kubectl get nodes -o jsonpath='{.items[0].status.addresses[?(@.type=="InternalIP")].address}' 2>$null)
    }
    
    if ([string]::IsNullOrEmpty($nodeIP)) {
        Write-Host "  ERROR: Cannot get node IP" -ForegroundColor Red
        continue
    }
    
    $url = "http://${nodeIP}:$($svc.Port)/health"
    
    $success = 0
    $failed = 0
    $latencies = @()
    
    for ($i = 0; $i -lt $Requests; $i++) {
        try {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            $sw.Stop()
            
            if ($response.StatusCode -eq 200) {
                $success++
                $latencies += $sw.ElapsedMilliseconds
            } else {
                $failed++
            }
        } catch {
            $failed++
        }
        
        Start-Sleep -Milliseconds 100
    }
    
    if ($latencies.Count -gt 0) {
        $avgLatency = ($latencies | Measure-Object -Average).Average
        $p95Latency = ($latencies | Sort-Object)[[Math]::Floor($latencies.Count * 0.95)]
    } else {
        $avgLatency = 0
        $p95Latency = 0
    }
    
    $successRate = if ($Requests -gt 0) { ($success / $Requests) * 100 } else { 0 }
    
    $result = @{
        Service = $svc.Name
        Success = $success
        Failed = $failed
        SuccessRate = $successRate
        AvgLatency = [Math]::Round($avgLatency, 2)
        P95Latency = $p95Latency
    }
    
    $results += $result
    
    if ($successRate -ge 90) {
        Write-Host "  OK - Success: $success/$Requests ($([Math]::Round($successRate,1))%) | Latency: ${avgLatency}ms" -ForegroundColor Green
    } elseif ($successRate -ge 50) {
        Write-Host "  WARN - Success: $success/$Requests ($([Math]::Round($successRate,1))%) | Latency: ${avgLatency}ms" -ForegroundColor Yellow
    } else {
        Write-Host "  FAIL - Success: $success/$Requests ($([Math]::Round($successRate,1))%) | Latency: ${avgLatency}ms" -ForegroundColor Red
    }
}

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
$allSuccess = ($results | Where-Object { $_.SuccessRate -ge 90 }).Count
$totalServices = $results.Count

Write-Host "Services OK: $allSuccess/$totalServices"

if ($allSuccess -eq $totalServices) {
    Write-Host "PASS - All services healthy`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "FAIL - Some services unhealthy`n" -ForegroundColor Red
    exit 1
}
