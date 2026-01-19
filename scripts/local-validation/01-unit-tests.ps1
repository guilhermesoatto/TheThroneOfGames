# UNIT TESTS VALIDATION
# Executa testes unitarios de todos os bounded contexts

$ErrorActionPreference = "Stop"
Write-Host "Running unit tests for all bounded contexts..." -ForegroundColor Yellow

$PassedTests = @()
$FailedTests = @()

$unitTestProjects = @(
    "GameStore.Catalogo.Tests",
    "GameStore.Usuarios.Tests",
    "GameStore.Vendas.Tests",
    "GameStore.Common.Tests"
)

$baseDir = "..\.."
Set-Location $baseDir

foreach ($project in $unitTestProjects) {
    Write-Host "`nTesting $project..." -ForegroundColor Cyan
    
    dotnet test $project --no-build --verbosity quiet --nologo
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] $project passed" -ForegroundColor Green
        $PassedTests += $project
    } else {
        Write-Host "[FAIL] $project failed" -ForegroundColor Red
        $FailedTests += $project
    }
}

Set-Location scripts\local-validation

Write-Host "`n=== UNIT TESTS SUMMARY ===" -ForegroundColor Cyan
Write-Host "Passed: $($PassedTests.Count)/$($unitTestProjects.Count)" -ForegroundColor Green
Write-Host "Failed: $($FailedTests.Count)/$($unitTestProjects.Count)" -ForegroundColor $(if ($FailedTests.Count -eq 0) { "Green" } else { "Red" })

if ($FailedTests.Count -gt 0) {
    Write-Host "`nFailed Projects:" -ForegroundColor Red
    $FailedTests | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nAll unit tests passed!" -ForegroundColor Green
exit 0
