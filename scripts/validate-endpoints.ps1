# Script de Validação de Endpoints - GameStore APIs
# Testa todos os endpoints dos microservices simulando fluxo real

$ErrorActionPreference = "Stop"

# Configuração
$UsuariosAPI = "http://localhost:5001"
$CatalogoAPI = "http://localhost:5002"
$VendasAPI = "http://localhost:5003"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "🚀 GAMESTORE - VALIDAÇÃO DE ENDPOINTS" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "Testing: $Name..." -NoNewline
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params -UseBasicParsing
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host " ✅ PASSED" -ForegroundColor Green
            $script:testsPassed++
            return $response
        } else {
            Write-Host " ❌ FAILED (Status: $($response.StatusCode), Expected: $ExpectedStatus)" -ForegroundColor Red
            $script:testsFailed++
            return $null
        }
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq $ExpectedStatus) {
            Write-Host " ✅ PASSED (Expected error: $status)" -ForegroundColor Green
            $script:testsPassed++
        } else {
            Write-Host " ❌ FAILED - $($_.Exception.Message)" -ForegroundColor Red
            $script:testsFailed++
        }
        return $null
    }
}

# ========================================
# 1. USUARIOS API - Authentication
# ========================================
Write-Host "`n[1/5] USUARIOS API - Authentication" -ForegroundColor Yellow

$randomEmail = "testuser$(Get-Random -Minimum 1000 -Maximum 9999)@test.com"

# 1.1 Register new user
$registerBody = @{
    name = "Test User"
    email = $randomEmail
    password = "Test@123!"
    role = "User"
}
$registerResponse = Test-Endpoint -Name "POST /register" -Method POST -Url "$UsuariosAPI/register" -Body $registerBody

# 1.2 Login as admin
$loginBody = @{
    email = "admin@test.com"
    password = "Admin@123!"
}
$loginResponse = Test-Endpoint -Name "POST /api/Usuario/login (Admin)" -Method POST -Url "$UsuariosAPI/api/Usuario/login" -Body $loginBody

$adminToken = $null
if ($loginResponse) {
    try {
        $loginData = $loginResponse.Content | ConvertFrom-Json
        if ($loginData.token) {
            $adminToken = $loginData.token
        } else {
            $adminToken = $loginResponse.Content
        }
    } catch {
        $adminToken = $loginResponse.Content
    }
    Write-Host "   Token received: $($adminToken.Substring(0,20))..." -ForegroundColor Gray
}

# 1.3 Get profile (requires auth)
if ($adminToken) {
    $headers = @{ Authorization = "Bearer $adminToken" }
    Test-Endpoint -Name "GET /api/Usuario/profile" -Method GET -Url "$UsuariosAPI/api/Usuario/profile" -Headers $headers
}

# ========================================
# 2. CATALOGO API - Games CRUD
# ========================================
Write-Host "`n[2/5] CATALOGO API - Games CRUD" -ForegroundColor Yellow

# 2.1 List all games
Test-Endpoint -Name "GET /api/Game" -Method GET -Url "$CatalogoAPI/api/Game"

$gameId = $null

# 2.2 Create game (requires admin auth)
if ($adminToken) {
    $headers = @{ Authorization = "Bearer $adminToken" }
    
    $createGameBody = @{
        name = "Test Game $(Get-Random -Minimum 100 -Maximum 999)"
        genre = "Action"
        price = 59.99
        description = "Test game created by validation script"
    }
    
    $createResponse = Test-Endpoint -Name "POST /api/Admin/Game" -Method POST -Url "$CatalogoAPI/api/Admin/Game" -Body $createGameBody -Headers $headers -ExpectedStatus 201
    
    if ($createResponse) {
        $gameData = $createResponse.Content | ConvertFrom-Json
        $gameId = $gameData.data.id
        Write-Host "   Game created with ID: $gameId" -ForegroundColor Gray
    }
}

# 2.3 Get game by ID
if ($gameId) {
    Test-Endpoint -Name "GET /api/Game/{id}" -Method GET -Url "$CatalogoAPI/api/Game/$gameId"
}

# 2.4 Update game (requires admin auth)
if ($adminToken -and $gameId) {
    $headers = @{ Authorization = "Bearer $adminToken" }
    
    $updateGameBody = @{
        name = "Updated Test Game"
        genre = "RPG"
        price = 69.99
        description = "Updated by validation script"
    }
    
    Test-Endpoint -Name "PUT /api/Admin/Game/{id}" -Method PUT -Url "$CatalogoAPI/api/Admin/Game/$gameId" -Body $updateGameBody -Headers $headers
}

# ========================================
# 3. VENDAS API - Health Check
# ========================================
Write-Host "`n[3/5] VENDAS API - Health Check" -ForegroundColor Yellow

Test-Endpoint -Name "GET /api/health" -Method GET -Url "$VendasAPI/api/health"

# ========================================
# 4. Event Bus - RabbitMQ Management
# ========================================
Write-Host "`n[4/5] EVENT BUS - RabbitMQ Management" -ForegroundColor Yellow

try {
    $rabbitMqUrl = "http://localhost:15672"
    $response = Invoke-WebRequest -Uri $rabbitMqUrl -UseBasicParsing -TimeoutSec 3
    Write-Host "RabbitMQ Management UI accessible" -NoNewline
    Write-Host " ✅ PASSED" -ForegroundColor Green
    $script:testsPassed++
}
catch {
    Write-Host "RabbitMQ Management UI not accessible" -NoNewline
    Write-Host " ⚠️ WARNING" -ForegroundColor Yellow
}

# ========================================
# 5. Monitoring - Prometheus & Grafana
# ========================================
Write-Host "`n[5/5] MONITORING - Prometheus & Grafana" -ForegroundColor Yellow

try {
    $prometheusUrl = "http://localhost:9090"
    $response = Invoke-WebRequest -Uri $prometheusUrl -UseBasicParsing -TimeoutSec 3
    Write-Host "Prometheus accessible" -NoNewline
    Write-Host " ✅ PASSED" -ForegroundColor Green
    $script:testsPassed++
}
catch {
    Write-Host "Prometheus not accessible" -NoNewline
    Write-Host " ⚠️ WARNING" -ForegroundColor Yellow
}

try {
    $grafanaUrl = "http://localhost:3000"
    $response = Invoke-WebRequest -Uri $grafanaUrl -UseBasicParsing -TimeoutSec 3
    Write-Host "Grafana accessible" -NoNewline
    Write-Host " ✅ PASSED" -ForegroundColor Green
    $script:testsPassed++
}
catch {
    Write-Host "Grafana not accessible" -NoNewline
    Write-Host " ⚠️ WARNING" -ForegroundColor Yellow
}

# ========================================
# CLEANUP - Delete test game
# ========================================
if ($adminToken -and $gameId) {
    Write-Host "`n[CLEANUP] Removing test game..." -ForegroundColor Gray
    $headers = @{ Authorization = "Bearer $adminToken" }
    Test-Endpoint -Name "DELETE /api/Admin/Game/{id}" -Method DELETE -Url "$CatalogoAPI/api/Admin/Game/$gameId" -Headers $headers
}

# ========================================
# RESULTADO FINAL
# ========================================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "📊 RESULTADO DA VALIDAÇÃO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$totalTests = $testsPassed + $testsFailed
$successRate = if ($totalTests -gt 0) { [math]::Round(($testsPassed / $totalTests) * 100, 2) } else { 0 }

Write-Host "`nTotal de Testes: $totalTests" -ForegroundColor White
Write-Host "✅ Passaram: $testsPassed" -ForegroundColor Green
Write-Host "❌ Falharam: $testsFailed" -ForegroundColor Red
Write-Host "📈 Taxa de Sucesso: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } else { "Yellow" })

if ($testsFailed -eq 0) {
    Write-Host "`n🎉 TODOS OS TESTES PASSARAM!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠️ ALGUNS TESTES FALHARAM!" -ForegroundColor Yellow
    exit 1
}
