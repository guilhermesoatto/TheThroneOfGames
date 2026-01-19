# Script de Teste de Carga - The Throne of Games
# Testa todos os endpoints com dados aleatorios e coleta metricas de performance

param(
    [Parameter(Mandatory=$false)]
    [int]$NumUsuarios = 50,
    
    [Parameter(Mandatory=$false)]
    [int]$NumJogos = 100,
    
    [Parameter(Mandatory=$false)]
    [int]$NumPedidos = 200,
    
    [Parameter(Mandatory=$false)]
    [int]$ConcurrentUsers = 10,
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrlUsuarios = "http://localhost:5001",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrlCatalogo = "http://localhost:5002",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrlVendas = "http://localhost:5003",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDataCreation,
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# Cores para output
$script:Colors = @{
    Success = "Green"
    Error = "Red"
    Warning = "Yellow"
    Info = "Cyan"
    Header = "Magenta"
}

# Metricas globais
$script:Metrics = @{
    TotalRequests = 0
    SuccessRequests = 0
    FailedRequests = 0
    TotalDuration = 0
    MinDuration = [double]::MaxValue
    MaxDuration = 0
    Durations = @()
    EndpointMetrics = @{}
}

# Geradores de dados aleatorios
function Get-RandomName {
    $firstNames = @("João", "Maria", "Pedro", "Ana", "Carlos", "Julia", "Lucas", "Beatriz", "Rafael", "Camila",
                    "Felipe", "Larissa", "Bruno", "Fernanda", "Diego", "Juliana", "Gustavo", "Mariana")
    $lastNames = @("Silva", "Santos", "Oliveira", "Souza", "Costa", "Pereira", "Rodrigues", "Almeida", "Lima", "Carvalho")
    
    return "$($firstNames | Get-Random) $($lastNames | Get-Random)"
}

function Get-RandomEmail {
    param([string]$Name)
    
    $cleanName = $Name.ToLower() -replace ' ', '.'
    $domains = @("email.com", "gmail.com", "outlook.com", "hotmail.com", "yahoo.com")
    $random = Get-Random -Minimum 1000 -Maximum 9999
    
    return "$cleanName$random@$($domains | Get-Random)"
}

function Get-RandomPassword {
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%"
    $password = -join ((1..12) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
    return $password + "Aa1!"  # Garantir que atende requisitos
}

function Get-RandomGameTitle {
    $adjectives = @("Epic", "Legendary", "Dark", "Lost", "Final", "Ancient", "Cyber", "Mystic", "Golden", "Shadow")
    $nouns = @("Warriors", "Quest", "Kingdom", "Chronicles", "Legends", "Odyssey", "Legacy", "Empire", "Adventure", "Saga")
    $numbers = @("", " II", " III", " IV", " V", " 2077", " 2024", " 2025")
    
    return "$($adjectives | Get-Random) $($nouns | Get-Random)$($numbers | Get-Random)"
}

function Get-RandomPrice {
    return [math]::Round((Get-Random -Minimum 99 -Maximum 399) + (Get-Random) / 2, 2)
}

function Get-RandomStock {
    return Get-Random -Minimum 10 -Maximum 200
}

function Get-RandomGameDescription {
    $descriptions = @(
        "Um jogo emocionante de acao e aventura",
        "Explore mundos fantasticos e descubra segredos antigos",
        "Uma experiencia unica de RPG com graficos incriveis",
        "Lute pela sobrevivencia em um mundo pos-apocaliptico",
        "Estrategia e tatica em tempo real",
        "A melhor experiencia multiplayer do ano",
        "Historia envolvente com escolhas que importam",
        "Graficos de ultima geracao e jogabilidade fluida"
    )
    
    return $descriptions | Get-Random
}

# Funcao para fazer requisicoes e coletar metricas
function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [string]$EndpointName
    )
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $success = $false
    $statusCode = 0
    $errorMessage = $null
    
    try {
        $params = @{
            Method = $Method
            Uri = $Url
            Headers = $Headers
            ContentType = "application/json"
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        $success = $true
        
        $result = $null
        if ($response.Content) {
            try {
                $result = $response.Content | ConvertFrom-Json
            } catch {
                $result = $response.Content
            }
        }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { 0 }
        $errorMessage = $_.Exception.Message
        $result = $null
    }
    finally {
        $stopwatch.Stop()
        $duration = $stopwatch.Elapsed.TotalMilliseconds
        
        # Atualizar metricas
        $script:Metrics.TotalRequests++
        if ($success) {
            $script:Metrics.SuccessRequests++
        } else {
            $script:Metrics.FailedRequests++
        }
        
        $script:Metrics.TotalDuration += $duration
        $script:Metrics.Durations += $duration
        
        if ($duration -lt $script:Metrics.MinDuration) {
            $script:Metrics.MinDuration = $duration
        }
        if ($duration -gt $script:Metrics.MaxDuration) {
            $script:Metrics.MaxDuration = $duration
        }
        
        # Metricas por endpoint
        if (-not $script:Metrics.EndpointMetrics.ContainsKey($EndpointName)) {
            $script:Metrics.EndpointMetrics[$EndpointName] = @{
                Total = 0
                Success = 0
                Failed = 0
                TotalDuration = 0
                MinDuration = [double]::MaxValue
                MaxDuration = 0
                Durations = @()
            }
        }
        
        $endpointMetric = $script:Metrics.EndpointMetrics[$EndpointName]
        $endpointMetric.Total++
        if ($success) {
            $endpointMetric.Success++
        } else {
            $endpointMetric.Failed++
        }
        $endpointMetric.TotalDuration += $duration
        $endpointMetric.Durations += $duration
        
        if ($duration -lt $endpointMetric.MinDuration) {
            $endpointMetric.MinDuration = $duration
        }
        if ($duration -gt $endpointMetric.MaxDuration) {
            $endpointMetric.MaxDuration = $duration
        }
    }
    
    return @{
        Success = $success
        StatusCode = $statusCode
        Duration = $duration
        Result = $result
        Error = $errorMessage
    }
}

# Banner
function Show-Banner {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor $script:Colors.Header
    Write-Host "           THE THRONE OF GAMES - TESTE DE CARGA" -ForegroundColor $script:Colors.Header
    Write-Host "======================================================================" -ForegroundColor $script:Colors.Header
    Write-Host ""
    Write-Host "Configuracao:" -ForegroundColor $script:Colors.Info
    Write-Host "  Usuarios a criar: $NumUsuarios" -ForegroundColor White
    Write-Host "  Jogos a criar: $NumJogos" -ForegroundColor White
    Write-Host "  Pedidos a criar: $NumPedidos" -ForegroundColor White
    Write-Host "  Usuarios concorrentes: $ConcurrentUsers" -ForegroundColor White
    Write-Host ""
    Write-Host "Endpoints:" -ForegroundColor $script:Colors.Info
    Write-Host "  Usuarios API: $BaseUrlUsuarios" -ForegroundColor White
    Write-Host "  Catalogo API: $BaseUrlCatalogo" -ForegroundColor White
    Write-Host "  Vendas API: $BaseUrlVendas" -ForegroundColor White
    Write-Host ""
}

# Teste de conectividade
function Test-Connectivity {
    Write-Host "Verificando conectividade com as APIs..." -ForegroundColor $script:Colors.Info
    
    $apis = @(
        @{ Name = "Usuarios"; Url = "$BaseUrlUsuarios/swagger" },
        @{ Name = "Catalogo"; Url = "$BaseUrlCatalogo/swagger" },
        @{ Name = "Vendas"; Url = "$BaseUrlVendas/swagger" }
    )
    
    $allOk = $true
    foreach ($api in $apis) {
        try {
            $response = Invoke-WebRequest -Uri $api.Url -UseBasicParsing -TimeoutSec 5
            Write-Host "  [OK] $($api.Name) API - Status: $($response.StatusCode)" -ForegroundColor $script:Colors.Success
        }
        catch {
            Write-Host "  [ERRO] $($api.Name) API - Nao acessivel" -ForegroundColor $script:Colors.Error
            $allOk = $false
        }
    }
    
    Write-Host ""
    
    if (-not $allOk) {
        Write-Host "ERRO: Nem todas as APIs estao acessiveis. Execute:" -ForegroundColor $script:Colors.Error
        Write-Host "  cd scripts && .\run-local.ps1" -ForegroundColor $script:Colors.Warning
        exit 1
    }
}

# Criar usuarios
function Create-TestUsers {
    param([int]$Count)
    
    Write-Host "Criando $Count usuarios de teste..." -ForegroundColor $script:Colors.Info
    
    $users = @()
    $adminCreated = $false
    
    for ($i = 1; $i -le $Count; $i++) {
        $name = Get-RandomName
        $email = Get-RandomEmail -Name $name
        $password = Get-RandomPassword
        $role = if (-not $adminCreated -and $i -eq 1) { "Admin"; $adminCreated = $true } else { "User" }
        
        $userData = @{
            name = $name
            email = $email
            password = $password
            role = $role
        }
        
        Write-Progress -Activity "Criando usuarios" -Status "Usuario $i de $Count" -PercentComplete (($i / $Count) * 100)
        
        $response = Invoke-ApiRequest -Method POST -Url "$BaseUrlUsuarios/api/Usuario/pre-register" -Body $userData -EndpointName "Usuario/PreRegister"
        
        if (-not $response.Success) {
            Write-Host "  [ERRO] Falha ao criar usuario $email - Status: $($response.StatusCode) - Erro: $($response.Error)" -ForegroundColor $script:Colors.Error
        }
        
        if ($response.Success) {
            # Auto-ativar usuario (assumindo que temos acesso ao token)
            # Em producao, isso viria do email
            $activationToken = if ($response.Result.activationToken) { $response.Result.activationToken } else { [guid]::NewGuid().ToString() }
            
            $activationResponse = Invoke-ApiRequest -Method POST -Url "$BaseUrlUsuarios/api/Usuario/activate?activationToken=$activationToken" -EndpointName "Usuario/Activate"
            
            $users += @{
                Name = $name
                Email = $email
                Password = $password
                Role = $role
                Token = $null
                Activated = $activationResponse.Success
            }
            
            if ($i % 10 -eq 0) {
                Write-Host "  Criados: $i usuarios" -ForegroundColor $script:Colors.Success
            }
        }
    }
    
    Write-Progress -Activity "Criando usuarios" -Completed
    Write-Host "  [OK] $($users.Count) usuarios criados" -ForegroundColor $script:Colors.Success
    Write-Host ""
    
    return $users
}

# Autenticar usuarios
function Authenticate-Users {
    param([array]$Users)
    
    Write-Host "Autenticando usuarios..." -ForegroundColor $script:Colors.Info
    
    $authenticatedCount = 0
    
    for ($i = 0; $i -lt $Users.Count; $i++) {
        $user = $Users[$i]
        
        if (-not $user.Activated) { continue }
        
        Write-Progress -Activity "Autenticando usuarios" -Status "Usuario $($i+1) de $($Users.Count)" -PercentComplete ((($i+1) / $Users.Count) * 100)
        
        $loginData = @{
            email = $user.Email
            password = $user.Password
        }
        
        $response = Invoke-ApiRequest -Method POST -Url "$BaseUrlUsuarios/api/Usuario/login" -Body $loginData -EndpointName "Usuario/Login"
        
        if ($response.Success -and $response.Result.token) {
            $user.Token = $response.Result.token
            $authenticatedCount++
        }
        
        Start-Sleep -Milliseconds 10
    }
    
    Write-Progress -Activity "Autenticando usuarios" -Completed
    Write-Host "  [OK] $authenticatedCount usuarios autenticados" -ForegroundColor $script:Colors.Success
    Write-Host ""
}

# Criar jogos
function Create-TestGames {
    param(
        [int]$Count,
        [string]$AdminToken
    )
    
    Write-Host "Criando $Count jogos de teste..." -ForegroundColor $script:Colors.Info
    
    $games = @()
    $headers = @{ "Authorization" = "Bearer $AdminToken" }
    
    for ($i = 1; $i -le $Count; $i++) {
        $gameData = @{
            titulo = Get-RandomGameTitle
            descricao = Get-RandomGameDescription
            preco = Get-RandomPrice
            estoque = Get-RandomStock
        }
        
        Write-Progress -Activity "Criando jogos" -Status "Jogo $i de $Count" -PercentComplete (($i / $Count) * 100)
        
        $response = Invoke-ApiRequest -Method POST -Url "$BaseUrlCatalogo/api/Game" -Body $gameData -Headers $headers -EndpointName "Game/Create"
        
        if ($response.Success -and $response.Result.id) {
            $games += @{
                Id = $response.Result.id
                Titulo = $gameData.titulo
                Preco = $gameData.preco
                Estoque = $gameData.estoque
            }
            
            if ($i % 20 -eq 0) {
                Write-Host "  Criados: $i jogos" -ForegroundColor $script:Colors.Success
            }
        }
    }
    
    Write-Progress -Activity "Criando jogos" -Completed
    Write-Host "  [OK] $($games.Count) jogos criados" -ForegroundColor $script:Colors.Success
    Write-Host ""
    
    return $games
}

# Testar endpoints de leitura
function Test-ReadEndpoints {
    param(
        [array]$Users,
        [array]$Games
    )
    
    Write-Host "Testando endpoints de leitura..." -ForegroundColor $script:Colors.Info
    
    $authenticatedUsers = $Users | Where-Object { $_.Token }
    
    if ($authenticatedUsers.Count -eq 0) {
        Write-Host "  [AVISO] Nenhum usuario autenticado" -ForegroundColor $script:Colors.Warning
        return
    }
    
    # Listar jogos
    Write-Host "  Testando GET /api/Game" -ForegroundColor $script:Colors.Info
    for ($i = 0; $i -lt [math]::Min(20, $authenticatedUsers.Count); $i++) {
        $user = $authenticatedUsers[$i]
        $headers = @{ "Authorization" = "Bearer $($user.Token)" }
        
        $response = Invoke-ApiRequest -Method GET -Url "$BaseUrlCatalogo/api/Game" -Headers $headers -EndpointName "Game/List"
        Start-Sleep -Milliseconds 50
    }
    
    # Buscar jogos especificos
    if ($Games.Count -gt 0) {
        Write-Host "  Testando GET /api/Game/{id}" -ForegroundColor $script:Colors.Info
        for ($i = 0; $i -lt [math]::Min(30, $Games.Count); $i++) {
            $game = $Games | Get-Random
            $user = $authenticatedUsers | Get-Random
            $headers = @{ "Authorization" = "Bearer $($user.Token)" }
            
            $response = Invoke-ApiRequest -Method GET -Url "$BaseUrlCatalogo/api/Game/$($game.Id)" -Headers $headers -EndpointName "Game/GetById"
            Start-Sleep -Milliseconds 30
        }
    }
    
    Write-Host "  [OK] Endpoints de leitura testados" -ForegroundColor $script:Colors.Success
    Write-Host ""
}

# Criar pedidos
function Create-TestOrders {
    param(
        [int]$Count,
        [array]$Users,
        [array]$Games
    )
    
    Write-Host "Criando $Count pedidos de teste..." -ForegroundColor $script:Colors.Info
    
    $authenticatedUsers = $Users | Where-Object { $_.Token -and $_.Role -eq "User" }
    
    if ($authenticatedUsers.Count -eq 0) {
        Write-Host "  [AVISO] Nenhum usuario autenticado para criar pedidos" -ForegroundColor $script:Colors.Warning
        return @()
    }
    
    if ($Games.Count -eq 0) {
        Write-Host "  [AVISO] Nenhum jogo disponivel para pedidos" -ForegroundColor $script:Colors.Warning
        return @()
    }
    
    $orders = @()
    
    for ($i = 1; $i -le $Count; $i++) {
        $user = $authenticatedUsers | Get-Random
        $numItems = Get-Random -Minimum 1 -Maximum 5
        $items = @()
        
        for ($j = 0; $j -lt $numItems; $j++) {
            $game = $Games | Get-Random
            $items += @{
                jogoId = $game.Id
                quantidade = Get-Random -Minimum 1 -Maximum 3
            }
        }
        
        $orderData = @{
            itens = $items
        }
        
        Write-Progress -Activity "Criando pedidos" -Status "Pedido $i de $Count" -PercentComplete (($i / $Count) * 100)
        
        $headers = @{ "Authorization" = "Bearer $($user.Token)" }
        $response = Invoke-ApiRequest -Method POST -Url "$BaseUrlVendas/api/Pedido" -Body $orderData -Headers $headers -EndpointName "Pedido/Create"
        
        if ($response.Success) {
            $orders += @{
                Id = if ($response.Result.id) { $response.Result.id } else { $i }
                UserId = $user.Email
                NumItems = $numItems
            }
            
            if ($i % 50 -eq 0) {
                Write-Host "  Criados: $i pedidos" -ForegroundColor $script:Colors.Success
            }
        }
        
        Start-Sleep -Milliseconds 20
    }
    
    Write-Progress -Activity "Criando pedidos" -Completed
    Write-Host "  [OK] $($orders.Count) pedidos criados" -ForegroundColor $script:Colors.Success
    Write-Host ""
    
    return $orders
}

# Teste de carga concorrente
function Test-ConcurrentLoad {
    param(
        [array]$Users,
        [array]$Games,
        [int]$Concurrent
    )
    
    Write-Host "Executando teste de carga concorrente ($Concurrent threads)..." -ForegroundColor $script:Colors.Info
    
    $authenticatedUsers = $Users | Where-Object { $_.Token }
    
    if ($authenticatedUsers.Count -eq 0) {
        Write-Host "  [AVISO] Nenhum usuario autenticado para teste de carga" -ForegroundColor $script:Colors.Warning
        return
    }
    
    $jobs = @()
    $totalRequests = $Concurrent * 10
    
    # Criar jobs para executar em paralelo
    for ($i = 0; $i -lt $Concurrent; $i++) {
        $job = Start-Job -ScriptBlock {
            param($BaseUrlCatalogo, $Users, $Games, $RequestsPerThread)
            
            $results = @{
                Success = 0
                Failed = 0
                TotalTime = 0
            }
            
            for ($j = 0; $j -lt $RequestsPerThread; $j++) {
                $user = $Users | Get-Random
                $headers = @{ "Authorization" = "Bearer $($user.Token)" }
                
                $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
                
                try {
                    if ($Games.Count -gt 0 -and (Get-Random -Maximum 2) -eq 0) {
                        $game = $Games | Get-Random
                        $response = Invoke-WebRequest -Method GET -Uri "$BaseUrlCatalogo/api/Game/$($game.Id)" -Headers $headers -UseBasicParsing -TimeoutSec 10
                    } else {
                        $response = Invoke-WebRequest -Method GET -Uri "$BaseUrlCatalogo/api/Game" -Headers $headers -UseBasicParsing -TimeoutSec 10
                    }
                    
                    $results.Success++
                }
                catch {
                    $results.Failed++
                }
                finally {
                    $stopwatch.Stop()
                    $results.TotalTime += $stopwatch.Elapsed.TotalMilliseconds
                }
                
                Start-Sleep -Milliseconds (Get-Random -Minimum 10 -Maximum 100)
            }
            
            return $results
        } -ArgumentList $BaseUrlCatalogo, $authenticatedUsers, $Games, 10
        
        $jobs += $job
    }
    
    # Aguardar conclusao
    Write-Host "  Aguardando conclusao de $($jobs.Count) threads..." -ForegroundColor $script:Colors.Info
    
    $completed = 0
    while ($completed -lt $jobs.Count) {
        $completed = ($jobs | Where-Object { $_.State -eq 'Completed' }).Count
        Write-Progress -Activity "Teste de carga concorrente" -Status "$completed de $($jobs.Count) threads concluidas" -PercentComplete (($completed / $jobs.Count) * 100)
        Start-Sleep -Milliseconds 500
    }
    
    Write-Progress -Activity "Teste de carga concorrente" -Completed
    
    # Coletar resultados
    $totalSuccess = 0
    $totalFailed = 0
    $totalTime = 0
    
    foreach ($job in $jobs) {
        $result = Receive-Job -Job $job
        $totalSuccess += $result.Success
        $totalFailed += $result.Failed
        $totalTime += $result.TotalTime
        Remove-Job -Job $job
    }
    
    $avgTime = if ($totalSuccess -gt 0) { [math]::Round($totalTime / $totalSuccess, 2) } else { 0 }
    
    Write-Host "  [OK] Teste concorrente concluido" -ForegroundColor $script:Colors.Success
    Write-Host "      Total: $($totalSuccess + $totalFailed) requisicoes" -ForegroundColor White
    Write-Host "      Sucesso: $totalSuccess" -ForegroundColor $script:Colors.Success
    Write-Host "      Falhas: $totalFailed" -ForegroundColor $(if ($totalFailed -gt 0) { $script:Colors.Warning } else { $script:Colors.Success })
    Write-Host "      Tempo medio: $avgTime ms" -ForegroundColor White
    Write-Host ""
}

# Gerar relatorio
function Generate-Report {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor $script:Colors.Header
    Write-Host "                    RELATORIO DE TESTE DE CARGA" -ForegroundColor $script:Colors.Header
    Write-Host "======================================================================" -ForegroundColor $script:Colors.Header
    Write-Host ""
    
    # Metricas gerais
    Write-Host "METRICAS GERAIS:" -ForegroundColor $script:Colors.Info
    Write-Host "  Total de requisicoes: $($script:Metrics.TotalRequests)" -ForegroundColor White
    Write-Host "  Requisicoes bem-sucedidas: $($script:Metrics.SuccessRequests) ($([math]::Round(($script:Metrics.SuccessRequests / $script:Metrics.TotalRequests) * 100, 2))%)" -ForegroundColor $script:Colors.Success
    Write-Host "  Requisicoes falhadas: $($script:Metrics.FailedRequests) ($([math]::Round(($script:Metrics.FailedRequests / $script:Metrics.TotalRequests) * 100, 2))%)" -ForegroundColor $(if ($script:Metrics.FailedRequests -gt 0) { $script:Colors.Warning } else { $script:Colors.Success })
    Write-Host ""
    
    # Metricas de tempo
    Write-Host "TEMPOS DE RESPOSTA:" -ForegroundColor $script:Colors.Info
    $avgDuration = [math]::Round($script:Metrics.TotalDuration / $script:Metrics.TotalRequests, 2)
    $sortedDurations = $script:Metrics.Durations | Sort-Object
    $p50 = $sortedDurations[[math]::Floor($sortedDurations.Count * 0.5)]
    $p95 = $sortedDurations[[math]::Floor($sortedDurations.Count * 0.95)]
    $p99 = $sortedDurations[[math]::Floor($sortedDurations.Count * 0.99)]
    
    Write-Host "  Minimo: $([math]::Round($script:Metrics.MinDuration, 2)) ms" -ForegroundColor White
    Write-Host "  Medio: $avgDuration ms" -ForegroundColor White
    Write-Host "  Maximo: $([math]::Round($script:Metrics.MaxDuration, 2)) ms" -ForegroundColor White
    Write-Host "  P50 (Mediana): $([math]::Round($p50, 2)) ms" -ForegroundColor White
    Write-Host "  P95: $([math]::Round($p95, 2)) ms" -ForegroundColor White
    Write-Host "  P99: $([math]::Round($p99, 2)) ms" -ForegroundColor White
    Write-Host ""
    
    # Metricas por endpoint
    Write-Host "METRICAS POR ENDPOINT:" -ForegroundColor $script:Colors.Info
    Write-Host ""
    
    $endpointTable = @()
    foreach ($endpoint in $script:Metrics.EndpointMetrics.Keys | Sort-Object) {
        $metric = $script:Metrics.EndpointMetrics[$endpoint]
        $successRate = [math]::Round(($metric.Success / $metric.Total) * 100, 1)
        $avgTime = [math]::Round($metric.TotalDuration / $metric.Total, 2)
        
        $sortedEndpointDurations = $metric.Durations | Sort-Object
        $endpointP95 = if ($sortedEndpointDurations.Count -gt 0) {
            $sortedEndpointDurations[[math]::Floor($sortedEndpointDurations.Count * 0.95)]
        } else { 0 }
        
        $endpointTable += [PSCustomObject]@{
            Endpoint = $endpoint
            Total = $metric.Total
            Success = $metric.Success
            Failed = $metric.Failed
            'Success %' = $successRate
            'Avg (ms)' = $avgTime
            'P95 (ms)' = [math]::Round($endpointP95, 2)
        }
    }
    
    $endpointTable | Format-Table -AutoSize
    
    # Salvar relatorio em arquivo
    if ($GenerateReport) {
        $reportPath = "load-test-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
        
        @"
======================================================================
THE THRONE OF GAMES - RELATORIO DE TESTE DE CARGA
======================================================================
Data: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')

CONFIGURACAO:
- Usuarios criados: $NumUsuarios
- Jogos criados: $NumJogos
- Pedidos criados: $NumPedidos
- Usuarios concorrentes: $ConcurrentUsers

METRICAS GERAIS:
- Total de requisicoes: $($script:Metrics.TotalRequests)
- Requisicoes bem-sucedidas: $($script:Metrics.SuccessRequests) ($([math]::Round(($script:Metrics.SuccessRequests / $script:Metrics.TotalRequests) * 100, 2))%)
- Requisicoes falhadas: $($script:Metrics.FailedRequests) ($([math]::Round(($script:Metrics.FailedRequests / $script:Metrics.TotalRequests) * 100, 2))%)

TEMPOS DE RESPOSTA:
- Minimo: $([math]::Round($script:Metrics.MinDuration, 2)) ms
- Medio: $avgDuration ms
- Maximo: $([math]::Round($script:Metrics.MaxDuration, 2)) ms
- P50 (Mediana): $([math]::Round($p50, 2)) ms
- P95: $([math]::Round($p95, 2)) ms
- P99: $([math]::Round($p99, 2)) ms

METRICAS POR ENDPOINT:
$($endpointTable | Format-Table -AutoSize | Out-String)

======================================================================
"@ | Out-File -FilePath $reportPath -Encoding UTF8
        
        Write-Host "Relatorio salvo em: $reportPath" -ForegroundColor $script:Colors.Success
        Write-Host ""
    }
}

# Execucao principal
try {
    Show-Banner
    Test-Connectivity
    
    if (-not $SkipDataCreation) {
        # Criar dados
        $users = Create-TestUsers -Count $NumUsuarios
        Authenticate-Users -Users $users
        
        # Pegar admin para criar jogos
        $adminUser = $users | Where-Object { $_.Role -eq "Admin" -and $_.Token } | Select-Object -First 1
        
        if (-not $adminUser) {
            Write-Host "ERRO: Nenhum usuario admin autenticado" -ForegroundColor $script:Colors.Error
            exit 1
        }
        
        $games = Create-TestGames -Count $NumJogos -AdminToken $adminUser.Token
        
        # Testar endpoints de leitura
        Test-ReadEndpoints -Users $users -Games $games
        
        # Criar pedidos
        $orders = Create-TestOrders -Count $NumPedidos -Users $users -Games $games
        
        # Teste de carga concorrente
        Test-ConcurrentLoad -Users $users -Games $games -Concurrent $ConcurrentUsers
    }
    
    # Gerar relatorio
    Generate-Report
    
    Write-Host "Teste de carga concluido com sucesso!" -ForegroundColor $script:Colors.Success
    Write-Host ""
}
catch {
    Write-Host "ERRO durante o teste de carga: $_" -ForegroundColor $script:Colors.Error
    exit 1
}
