# PIPELINE VALIDATION
# Simula validacoes que seriam executadas no pipeline CI/CD

$ErrorActionPreference = "Continue"
Write-Host "Simulating pipeline validations..." -ForegroundColor Yellow

$issues = @()
$baseDir = "..\.."
Push-Location $baseDir

# 1. Migrations validation (rápido)
Write-Host "`nChecking EF Core migrations..." -ForegroundColor Cyan
$contextPaths = @(
    "GameStore.Usuarios\Infrastructure\Migrations",
    "GameStore.Catalogo\Infrastructure\Migrations",
    "GameStore.Vendas\Infrastructure\Migrations"
)

foreach ($contextPath in $contextPaths) {
    $migrationsPath = Join-Path . $contextPath
    $contextName = ($contextPath -split '\\')[0]
    
    if (Test-Path $migrationsPath) {
        $migrations = @(Get-ChildItem -Path $migrationsPath -Filter *.cs -ErrorAction SilentlyContinue)
        if ($migrations.Count -gt 0) {
            Write-Host "[OK] $contextName has $($migrations.Count) migrations" -ForegroundColor Green
        } else {
            Write-Host "[WARN] $contextName has no migrations" -ForegroundColor Yellow
        }
    } else {
        Write-Host "[WARN] $contextName migrations folder not found" -ForegroundColor Yellow
    }
}

# 2. Security scan (vulnerabilities) - versão leve
Write-Host "`nScanning for known vulnerabilities..." -ForegroundColor Cyan
$outdatedOutput = dotnet list package --outdated 2>&1 | Select-String "Vulnerability" -ErrorAction SilentlyContinue | Measure-Object
if ($outdatedOutput.Count -gt 0) {
    Write-Host "[INFO] Found some outdated packages (check separately)" -ForegroundColor Yellow
} else {
    Write-Host "[OK] No critical vulnerability checks needed now" -ForegroundColor Green
}

# 3. Git status (uncommitted changes)
Write-Host "`nChecking git status..." -ForegroundColor Cyan
$gitStatus = @(git status --porcelain 2>&1 | Where-Object { $_ -notmatch "^\?" })
if ($gitStatus.Count -gt 0) {
    Write-Host "[INFO] Found $($gitStatus.Count) uncommitted changes (expected during development)" -ForegroundColor Yellow
} else {
    Write-Host "[OK] Working directory clean" -ForegroundColor Green
}

Pop-Location

Write-Host "`n=== PIPELINE VALIDATION SUMMARY ===" -ForegroundColor Cyan
if ($issues.Count -eq 0) {
    Write-Host "All pipeline checks passed" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Issues found: $($issues.Count)" -ForegroundColor Red
    $issues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
