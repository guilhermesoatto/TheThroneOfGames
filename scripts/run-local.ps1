# Script de Execucao Local - The Throne of Games
# Este script inicia toda a infraestrutura localmente com Docker Compose

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('start', 'stop', 'restart', 'logs', 'status')]
    [string]$Action = 'start',
    
    [Parameter(Mandatory=$false)]
    [switch]$LoadData
)

$ComposeFile = "docker-compose.local.yml"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

Set-Location $RootDir

Write-Host ""
Write-Host "===============================================================" -ForegroundColor Green
Write-Host "     THE THRONE OF GAMES - AMBIENTE LOCAL" -ForegroundColor Green
Write-Host "===============================================================" -ForegroundColor Green
Write-Host ""

function Show-Status {
    Write-Host "Status dos servicos:" -ForegroundColor Cyan
    docker-compose -f $ComposeFile ps
}

function Show-Logs {
    param([string]$Service = "")
    
    if ($Service) {
        docker-compose -f $ComposeFile logs -f --tail=100 $Service
    } else {
        docker-compose -f $ComposeFile logs -f --tail=50
    }
}

function Start-Services {
    Write-Host "Iniciando serviços..." -ForegroundColor Yellow
    Write-Host ""
    
    # Verificar se Docker esta rodando
    try {
        docker ps | Out-Null
    }
    catch {
        Write-Host "[X] Docker nao esta rodando. Por favor, inicie o Docker Desktop." -ForegroundColor Red
        exit 1
    }
    
    # Build das imagens
    Write-Host "1. Construindo imagens Docker..." -ForegroundColor Cyan
    docker-compose -f $ComposeFile build --parallel
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[X] Erro ao construir imagens." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   [OK] Imagens construidas com sucesso!" -ForegroundColor Green
    Write-Host ""
    
    # Iniciar servicos
    Write-Host "2. Iniciando containers..." -ForegroundColor Cyan
    docker-compose -f $ComposeFile up -d
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[X] Erro ao iniciar containers." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   [OK] Containers iniciados!" -ForegroundColor Green
    Write-Host ""
    
    # Aguardar servicos ficarem prontos
    Write-Host "3. Aguardando servicos ficarem prontos..." -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    
    $maxRetries = 30
    $retryCount = 0
    $allHealthy = $false
    
    while (-not $allHealthy -and $retryCount -lt $maxRetries) {
        $retryCount++
        Write-Host "   Verificando saude dos servicos... (tentativa $retryCount/$maxRetries)" -ForegroundColor Yellow
        
        $containers = docker-compose -f $ComposeFile ps --format json | ConvertFrom-Json
        $unhealthy = $containers | Where-Object { $_.Health -ne "healthy" -and $_.State -eq "running" }
        
        if (-not $unhealthy) {
            $allHealthy = $true
        }
        else {
            Start-Sleep -Seconds 2
        }
    }
    
    if ($allHealthy) {
        Write-Host "   [OK] Todos os servicos estao saudaveis!" -ForegroundColor Green
    }
    else {
        Write-Host "   [!] Alguns servicos podem ainda estar inicializando..." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "===============================================================" -ForegroundColor Green
    Write-Host "  AMBIENTE LOCAL INICIADO COM SUCESSO!" -ForegroundColor Green
    Write-Host "===============================================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "URLs dos Servicos:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Grafana (Monitoramento):" -ForegroundColor Yellow
    Write-Host "     http://localhost:3000" -ForegroundColor White
    Write-Host "     Usuario: admin / Senha: admin" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Prometheus (Metricas):" -ForegroundColor Yellow
    Write-Host "     http://localhost:9090" -ForegroundColor White
    Write-Host ""
    Write-Host "  RabbitMQ (Mensageria):" -ForegroundColor Yellow
    Write-Host "     http://localhost:15672" -ForegroundColor White
    Write-Host "     Usuario: guest / Senha: guest" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Usuarios API:" -ForegroundColor Yellow
    Write-Host "     http://localhost:5001/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "  Catalogo API:" -ForegroundColor Yellow
    Write-Host "     http://localhost:5002/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "  Vendas API:" -ForegroundColor Yellow
    Write-Host "     http://localhost:5003/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "  SQL Server:" -ForegroundColor Yellow
    Write-Host "     localhost:1433" -ForegroundColor White
    Write-Host "     SA Password: YourSecurePassword123!" -ForegroundColor Gray
    Write-Host ""
    
    if ($LoadData) {
        Write-Host "===============================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "4. Carregando dados iniciais..." -ForegroundColor Cyan
        Write-Host ""
        
        # Aguardar mais um pouco para garantir que APIs estao prontas
        Start-Sleep -Seconds 10
        
        # Executar script de carga
        & "$ScriptDir\load-initial-data.ps1"
    }
    else {
        Write-Host "Dica: Execute com -LoadData para carregar dados iniciais" -ForegroundColor Cyan
        Write-Host "   Exemplo: .\run-local.ps1 -LoadData" -ForegroundColor Gray
        Write-Host ""
    }
    
    Write-Host "Para ver logs: .\scripts\run-local.ps1 -Action logs" -ForegroundColor Cyan
    Write-Host "Para parar: .\scripts\run-local.ps1 -Action stop" -ForegroundColor Cyan
    Write-Host ""
}

function Stop-Services {
    Write-Host "Parando servicos..." -ForegroundColor Yellow
    docker-compose -f $ComposeFile down
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Servicos parados com sucesso!" -ForegroundColor Green
    }
    else {
        Write-Host "[X] Erro ao parar servicos." -ForegroundColor Red
    }
}

function Restart-Services {
    Write-Host "Reiniciando servicos..." -ForegroundColor Yellow
    Stop-Services
    Start-Sleep -Seconds 2
    Start-Services
}

# Executar ação
switch ($Action) {
    'start' {
        Start-Services
    }
    'stop' {
        Stop-Services
    }
    'restart' {
        Restart-Services
    }
    'logs' {
        Show-Logs
    }
    'status' {
        Show-Status
    }
}
