# CONTAINER LIFECYCLE VALIDATION
# Remove, recria e valida containers de teste

$ErrorActionPreference = "Continue"
Write-Host "Testing container lifecycle..." -ForegroundColor Yellow

$containers = @(
    @{ Name = "postgresql-test"; Image = "postgres:16-alpine"; Ports = "5432:5432"; Env = @("-e", "POSTGRES_USER=sa", "-e", "POSTGRES_PASSWORD=YourSecurePassword123!", "-e", "POSTGRES_DB=GameStore") },
    @{ Name = "rabbitmq-test"; Image = "rabbitmq:3-management-alpine"; Ports = "5672:5672", "15672:15672"; Env = @() }
)

$allSuccess = $true

# PHASE 1: Criar rede Docker
Write-Host "`n[NETWORK] Creating Docker network 'gamestore-test'..." -ForegroundColor Cyan
docker network create gamestore-test 2>$null
Write-Host "[OK] Network ready" -ForegroundColor Green

# PHASE 2: Infraestrutura (PostgreSQL, RabbitMQ)
Write-Host "`n=== INFRASTRUCTURE CONTAINERS ===" -ForegroundColor Magenta

foreach ($container in $containers) {
    Write-Host "`n[INFRA] Processing $($container.Name)..." -ForegroundColor Cyan
    docker stop $container.Name 2>$null
    docker rm $container.Name 2>$null
    
    $portArgs = @()
    foreach ($port in $container.Ports) {
        $portArgs += "-p"
        $portArgs += $port
    }
    
    $createArgs = @("run", "-d", "--name", $container.Name, "--network", "gamestore-test") + $container.Env + $portArgs + $container.Image
    & docker $createArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] Failed to create $($container.Name)" -ForegroundColor Red
        $allSuccess = $false
        continue
    }
    
    Write-Host "[PHASE] Waiting for $($container.Name) to be healthy..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    $status = docker inspect --format='{{.State.Status}}' $container.Name
    if ($status -eq "running") {
        Write-Host "[OK] $($container.Name) is running" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $($container.Name) is not running (status: $status)" -ForegroundColor Red
        $allSuccess = $false
    }
}

# PHASE 3: Validar conectividade de infraestrutura
Write-Host "`n=== INFRASTRUCTURE CONNECTIVITY ===" -ForegroundColor Magenta

# PostgreSQL
Write-Host "[TEST] PostgreSQL connectivity..." -ForegroundColor Cyan
docker exec postgresql-test pg_isready -U sa 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] PostgreSQL accepting connections" -ForegroundColor Green
} else {
    Write-Host "[FAIL] PostgreSQL not ready" -ForegroundColor Red
    $allSuccess = $false
}

# RabbitMQ (apenas verificar container rodando)
Write-Host "[TEST] RabbitMQ connectivity..." -ForegroundColor Cyan
$rabbitStatus = docker inspect --format='{{.State.Status}}' rabbitmq-test 2>$null
if ($rabbitStatus -eq "running") {
    Write-Host "[OK] RabbitMQ container is running" -ForegroundColor Green
} else {
    Write-Host "[WARN] RabbitMQ status: $rabbitStatus" -ForegroundColor Yellow
}

# PHASE 4: Testar APIs localmente (sem build Docker)
Write-Host "`n=== LOCAL API TESTS ===" -ForegroundColor Magenta
Write-Host "[INFO] Testing APIs via dotnet run (local, sem containers)..." -ForegroundColor Yellow

# Voltar para diretório de scripts
Set-Location C:\Users\Guilherme\source\repos\TheThroneOfGames

# Voltar para diretório de scripts
Set-Location C:\Users\Guilherme\source\repos\TheThroneOfGames

# Executar testes de integração contra infraestrutura containerizada
Write-Host "`n[TEST] Running integration tests against containerized infrastructure..." -ForegroundColor Cyan

$testProjects = @(
    "GameStore.Usuarios.API.Tests",
    "GameStore.Catalogo.API.Tests",
    "GameStore.Vendas.API.Tests"
)

$testsPassed = 0
$testsFailed = 0

foreach ($project in $testProjects) {
    Write-Host "`n[TEST] $project..." -ForegroundColor Yellow
    $testOutput = dotnet test $project --no-build --verbosity minimal 2>&1
    $result = $testOutput | Select-String "Aprovado|Falha"
    
    if ($result -match "Aprovado") {
        Write-Host "[OK] $result" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "[FAIL] $result" -ForegroundColor Red
        $testsFailed++
        $allSuccess = $false
    }
}

Write-Host "`n=== CONTAINER CYCLE SUMMARY ===" -ForegroundColor Cyan
Write-Host "Infrastructure Containers: $(if ($allSuccess) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($allSuccess) { "Green" } else { "Red" })
Write-Host "Integration Tests: $testsPassed passed, $testsFailed failed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })

if ($allSuccess -and $testsFailed -eq 0) {
    Write-Host "`nAll containers and tests validated successfully" -ForegroundColor Green
    Set-Location scripts\local-validation
    exit 0
} else {
    Write-Host "`nSome validations failed" -ForegroundColor Red
    Set-Location scripts\local-validation
    exit 1
}
