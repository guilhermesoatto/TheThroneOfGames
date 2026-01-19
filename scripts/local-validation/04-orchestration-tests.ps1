# ORCHESTRATION VALIDATION
# Valida arquivos de orquestracao (K8s, Docker Compose, Dockerfiles)

$ErrorActionPreference = "Continue"
Write-Host "Validating orchestration files..." -ForegroundColor Yellow

$issues = @()
$baseDir = "..\.."

# Navegar para diretório raiz do projeto
Push-Location $baseDir

# Obter diretório absoluto
$projectRoot = Get-Location

# Validar K8s manifests
$k8sPath = Join-Path -Path $projectRoot -ChildPath "k8s"

if ($null -ne $k8sPath) {
    Write-Host "`nValidating Kubernetes manifests..." -ForegroundColor Cyan
    $yamlFiles = Get-ChildItem -Path $k8sPath -Recurse -Filter *.yaml -ErrorAction SilentlyContinue
    
    foreach ($file in $yamlFiles) {
        Write-Host "  Checking $($file.Name)..." -ForegroundColor Gray
        
        $kubectlAvailable = Get-Command kubectl -ErrorAction SilentlyContinue
        if ($kubectlAvailable) {
            kubectl apply --dry-run=client -f $file.FullName 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
                $issues += "K8s manifest invalid: $($file.Name)"
                Write-Host "  [FAIL] $($file.Name)" -ForegroundColor Red
            } else {
                Write-Host "  [OK] $($file.Name)" -ForegroundColor Green
            }
        } else {
            Write-Host "  [SKIP] kubectl not available" -ForegroundColor Yellow
        }
    }
}

# Validar Docker Compose files
Write-Host "`nValidating Docker Compose files..." -ForegroundColor Cyan
$composeFiles = Get-ChildItem -Path $baseDir -Filter docker-compose*.yml -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "docker-compose*.yml" }

foreach ($file in $composeFiles) {
    Write-Host "  Checking $($file.Name)..." -ForegroundColor Gray
    docker-compose -f $file.FullName config 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        $issues += "Docker Compose invalid: $($file.Name)"
        Write-Host "  [FAIL] $($file.Name)" -ForegroundColor Red
    } else {
        Write-Host "  [OK] $($file.Name)" -ForegroundColor Green
    }
}

# Validar Dockerfiles
Write-Host "`nValidating Dockerfiles..." -ForegroundColor Cyan
$dockerfiles = Get-ChildItem -Path $baseDir -Recurse -Filter Dockerfile* -ErrorAction SilentlyContinue

foreach ($file in $dockerfiles) {
    Write-Host "  Checking $($file.Name) in $($file.Directory.Name)..." -ForegroundColor Gray
    $content = Get-Content $file.FullName -Raw
    if ($content -match "FROM\s+\S+") {
        Write-Host "  [OK] $($file.Name)" -ForegroundColor Green
    } else {
        $issues += "Dockerfile invalid: $($file.FullName)"
        Write-Host "  [FAIL] $($file.Name)" -ForegroundColor Red
    }
}

Write-Host "`n=== ORCHESTRATION VALIDATION SUMMARY ===" -ForegroundColor Cyan
if ($issues.Count -eq 0) {
    Write-Host "All orchestration files are valid" -ForegroundColor Green
    Pop-Location
    exit 0
} else {
    Write-Host "Issues found: $($issues.Count)" -ForegroundColor Red
    $issues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Pop-Location
    exit 1
}
