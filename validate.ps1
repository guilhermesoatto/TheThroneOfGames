#!/usr/bin/env powershell
#Requires -Version 7.0
cd 'C:\Users\Guilherme\source\repos\TheThroneOfGames'
Write-Host "=== BUILDING ===" -ForegroundColor Cyan
$buildResult = dotnet build 2>&1
if ($buildResult -match "êxito") {
    Write-Host "✅ BUILD PASSED" -ForegroundColor Green
    Write-Host "`n=== RUNNING TESTS ===" -ForegroundColor Cyan
    dotnet test --no-build --verbosity minimal 2>&1
} else {
    Write-Host "❌ BUILD FAILED" -ForegroundColor Red
    $buildResult | Select-String "error" | Select-Object -First 10
}
