# INTEGRATION TESTS VALIDATION
# Executa testes de integracao das APIs

$ErrorActionPreference = "Stop"
Write-Host "Running integration tests..." -ForegroundColor Yellow

# Verificar se containers estao rodando
Write-Host "`nChecking container dependencies..." -ForegroundColor Cyan

$postgresRunning = docker ps --filter "name=postgresql" --filter "status=running" -q
$rabbitmqRunning = docker ps --filter "name=rabbitmq" --filter "status=running" -q

if (-not $postgresRunning) {
    Write-Host "[ERROR] PostgreSQL container is not running!" -ForegroundColor Red
    Write-Host "Start with: docker run -d --name postgresql-test -e POSTGRES_USER=sa -e POSTGRES_PASSWORD=YourSecurePassword123! -e POSTGRES_DB=GameStore -p 5432:5432 postgres:16-alpine" -ForegroundColor Yellow
    exit 1
}

if (-not $rabbitmqRunning) {
    Write-Host "[WARNING] RabbitMQ container is not running (may be optional)" -ForegroundColor Yellow
}

Write-Host "[OK] Container dependencies validated" -ForegroundColor Green

$PassedTests = @()
$FailedTests = @()

$integrationTestProjects = @(
    "GameStore.Usuarios.API.Tests",
    "GameStore.Catalogo.API.Tests",
    "GameStore.Vendas.API.Tests"
)

$baseDir = "..\.."
Set-Location $baseDir

foreach ($project in $integrationTestProjects) {
    if (Test-Path $project) {
        Write-Host "`nTesting $project..." -ForegroundColor Cyan
        
        dotnet test $project --no-build --verbosity quiet --nologo
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] $project passed" -ForegroundColor Green
            $PassedTests += $project
        } else {
            Write-Host "[FAIL] $project failed" -ForegroundColor Red
            $FailedTests += $project
        }
    } else {
        Write-Host "[SKIP] $project not found" -ForegroundColor Yellow
    }
}

Set-Location scripts\local-validation

Write-Host "`n=== INTEGRATION TESTS SUMMARY ===" -ForegroundColor Cyan
Write-Host "Passed: $($PassedTests.Count)" -ForegroundColor Green
Write-Host "Failed: $($FailedTests.Count)" -ForegroundColor $(if ($FailedTests.Count -eq 0) { "Green" } else { "Red" })

if ($FailedTests.Count -gt 0) {
    Write-Host "`nFailed Projects:" -ForegroundColor Red
    $FailedTests | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nAll integration tests passed!" -ForegroundColor Green
exit 0
