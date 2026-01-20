#!/usr/bin/env pwsh
# K8s Performance Validation - Post-Deploy Health Check
# Uses kubectl port-forward to test services internally

param(
    [string]$Namespace = "thethroneofgames",
    [int]$Requests = 5
)

Write-Host "`n=== K8S PERFORMANCE VALIDATION ===" -ForegroundColor Cyan
Write-Host "Namespace: $Namespace | Requests: $Requests`n"

$services = @(
    @{Name="usuarios-api"; LocalPort=8081; SvcName="usuarios-api-service"},
    @{Name="catalogo-api"; LocalPort=8082; SvcName="catalogo-api-service"},
    @{Name="vendas-api"; LocalPort=8083; SvcName="vendas-api-service"}
)

$results = @()
$pids = @()

# Start port-forwards
Write-Host "Setting up port-forwards..." -ForegroundColor Gray
foreach ($svc in $services) {
    $job = Start-Job -ScriptBlock {
        param($ns, $svcName, $localPort)
        kubectl port-forward "svc/$svcName" "${localPort}:80" -n $ns 2>&1
    } -ArgumentList $Namespace, $svc.SvcName, $svc.LocalPort
    $pids += $job.Id
    Start-Sleep -Seconds 1
}

# Wait for port-forwards to establish
Start-Sleep -Seconds 5

# Test each service
foreach ($svc in $services) {
    Write-Host "Testing $($svc.Name)..." -ForegroundColor Yellow
    
    $url = "http://localhost:$($svc.LocalPort)/health"
    $success = 0
    $failed = 0
    $latencies = @()
    
    for ($i = 0; $i -lt $Requests; $i++) {
        try {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
            $sw.Stop()
            
            if ($response.StatusCode -eq 200) {
                $success++
                $latencies += $sw.ElapsedMilliseconds
            } else {
                $failed++
            }
        } catch {
            $failed++
            Write-Host "  Request $i failed: $($_.Exception.Message)" -ForegroundColor DarkGray
        }
        Start-Sleep -Milliseconds 200
    }
    
    $avgLatency = if ($latencies.Count -gt 0) { [Math]::Round(($latencies | Measure-Object -Average).Average, 2) } else { 0 }
    $successRate = if ($Requests -gt 0) { ($success / $Requests) * 100 } else { 0 }
    
    $results += @{
        Service = $svc.Name
        Success = $success
        Failed = $failed
        SuccessRate = $successRate
        AvgLatency = $avgLatency
    }
    
    if ($successRate -ge 80) {
        Write-Host "  OK - Success: $success/$Requests ($([Math]::Round($successRate,1))%) | Latency: ${avgLatency}ms" -ForegroundColor Green
    } else {
        Write-Host "  FAIL - Success: $success/$Requests ($([Math]::Round($successRate,1))%) | Latency: ${avgLatency}ms" -ForegroundColor Red
    }
}

# Cleanup port-forwards
Write-Host "`nCleaning up..." -ForegroundColor Gray
foreach ($pid in $pids) {
    Stop-Job -Id $pid -ErrorAction SilentlyContinue
    Remove-Job -Id $pid -Force -ErrorAction SilentlyContinue
}

# Summary
Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
$passedServices = ($results | Where-Object { $_.SuccessRate -ge 80 }).Count
$totalServices = $results.Count

Write-Host "Services OK: $passedServices/$totalServices"

if ($passedServices -eq $totalServices) {
    Write-Host "PASS - All services healthy`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "FAIL - Some services unhealthy`n" -ForegroundColor Red
    exit 1
}
