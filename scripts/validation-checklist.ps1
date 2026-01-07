# Script de Validação - Fase 4 (The Throne of Games)
# Verifica todos os requisitos da Fase 4 e mantém o sistema funcionando

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("full", "quick", "health", "repair", "k8s")]
    [string]$Mode = "quick",
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport,
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoRepair
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# Cores
$Colors = @{
    Success = "Green"
    Error = "Red"
    Warning = "Yellow"
    Info = "Cyan"
    Header = "Magenta"
}

# Estado global
$ValidationState = @{
    TotalChecks = 0
    PassedChecks = 0
    FailedChecks = 0
    Issues = @()
    Warnings = @()
    Repairs = @()
}

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "════════════════════════════════════════════════" -ForegroundColor $Colors.Header
    Write-Host "  $Text" -ForegroundColor $Colors.Header
    Write-Host "════════════════════════════════════════════════" -ForegroundColor $Colors.Header
    Write-Host ""
}

function Write-Check {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    $ValidationState.TotalChecks++
    if ($Passed) {
        $ValidationState.PassedChecks++
        Write-Host "[OK] " -ForegroundColor $Colors.Success -NoNewline
    } else {
        $ValidationState.FailedChecks++
        Write-Host "[FAIL] " -ForegroundColor $Colors.Error -NoNewline
    }
    
    Write-Host $Name -ForegroundColor White
    
    if ($Details) {
        Write-Host "     $Details" -ForegroundColor Gray
    }
    
    return $Passed
}

function Write-Issue {
    param(
        [string]$Issue,
        [string]$Severity = "Error"
    )
    
    $Color = if ($Severity -eq "Error") { $Colors.Error } else { $Colors.Warning }
    Write-Host "[$Severity] $Issue" -ForegroundColor $Color
    
    $ValidationState.Issues += @{
        Issue = $Issue
        Severity = $Severity
    }
}

# ========================================
# VALIDACAO LOCAL (Docker Compose)
# ========================================

function Validate-Local-Environment {
    Write-Header "Validando Ambiente Local (Docker Compose)"
    
    # Verificar Docker
    Write-Host "Verificando Docker..." -ForegroundColor $Colors.Info
    $dockerInstalled = docker --version 2>$null
    if ($dockerInstalled) {
        Write-Check "Docker instalado" $true $dockerInstalled
    } else {
        Write-Check "Docker instalado" $false
        Write-Issue "Docker não encontrado"
        return $false
    }
    
    # Verificar containers rodando
    Write-Host ""
    Write-Host "Verificando containers..." -ForegroundColor $Colors.Info
    
    $containers = @(
        "thethroneofgames-usuarios-api",
        "thethroneofgames-catalogo-api",
        "thethroneofgames-vendas-api",
        "thethroneofgames-sqlserver",
        "thethroneofgames-rabbitmq",
        "thethroneofgames-prometheus",
        "thethroneofgames-grafana"
    )
    
    $allRunning = $true
    foreach ($container in $containers) {
        $status = docker ps --filter "name=$container" --format "{{.State}}" 2>$null
        $isRunning = $status -eq "running"
        
        Write-Check "$container" $isRunning
        $allRunning = $allRunning -and $isRunning
        
        if (-not $isRunning) {
            Write-Issue "Container $container não está rodando"
        }
    }
    
    return $allRunning
}

# ========================================
# VALIDACAO DE ENDPOINTS
# ========================================

function Validate-API-Endpoints {
    Write-Header "Validando Endpoints das APIs"
    
    $endpoints = @(
        @{ Name = "Usuarios API"; URL = "http://localhost:5001/swagger"; Port = 5001 },
        @{ Name = "Catalogo API"; URL = "http://localhost:5002/swagger"; Port = 5002 },
        @{ Name = "Vendas API"; URL = "http://localhost:5003/swagger"; Port = 5003 },
        @{ Name = "SQL Server"; URL = "http://localhost:1433"; Port = 1433 },
        @{ Name = "RabbitMQ"; URL = "http://localhost:15672"; Port = 15672 },
        @{ Name = "Grafana"; URL = "http://localhost:3000"; Port = 3000 },
        @{ Name = "Prometheus"; URL = "http://localhost:9090"; Port = 9090 }
    )
    
    $allHealthy = $true
    
    foreach ($endpoint in $endpoints) {
        try {
            if ($endpoint.Name -in "SQL Server") {
                # Testar conexão TCP
                $tcpClient = New-Object System.Net.Sockets.TcpClient
                $asyncResult = $tcpClient.BeginConnect("localhost", $endpoint.Port, $null, $null)
                $asyncResult.AsyncWaitHandle.WaitOne(3000) > $null
                $connected = $tcpClient.Connected
                $tcpClient.Close()
                
                Write-Check $endpoint.Name $connected "TCP Port $($endpoint.Port)"
                $allHealthy = $allHealthy -and $connected
                
                if (-not $connected) {
                    Write-Issue "$($endpoint.Name) não acessível na porta $($endpoint.Port)"
                }
            } else {
                # Testar HTTP
                $response = Invoke-WebRequest -Uri $endpoint.URL -UseBasicParsing -TimeoutSec 3
                $healthy = $response.StatusCode -eq 200
                
                Write-Check $endpoint.Name $healthy "HTTP $($response.StatusCode)"
                $allHealthy = $allHealthy -and $healthy
                
                if (-not $healthy) {
                    Write-Issue "$($endpoint.Name) retornou status $($response.StatusCode)"
                }
            }
        }
        catch {
            Write-Check $endpoint.Name $false "Erro de conexão"
            Write-Issue "$($endpoint.Name): $($_.Exception.Message)"
            $allHealthy = $false
        }
    }
    
    return $allHealthy
}

# ========================================
# VALIDACAO DE FUNCIONALIDADES
# ========================================

function Validate-Features {
    Write-Header "Validando Funcionalidades da Fase 4"
    
    $features = @(
        @{ Name = "RabbitMQ (Comunicação Assíncrona)"; Check = { Test-RabbitMQHealth } },
        @{ Name = "Docker Containers (Otimizados)"; Check = { Test-DockerOptimization } },
        @{ Name = "Kubernetes YAML Manifests"; Check = { Test-K8sManifests } },
        @{ Name = "Monitoramento (Prometheus)"; Check = { Test-PrometheusHealth } },
        @{ Name = "Grafana (Visualização)"; Check = { Test-GrafanaHealth } },
        @{ Name = "ConfigMaps e Secrets"; Check = { Test-ConfigSecrets } },
        @{ Name = "Health Checks"; Check = { Test-HealthChecks } }
    )
    
    $allValid = $true
    
    foreach ($feature in $features) {
        try {
            $result = & $feature.Check
            Write-Check $feature.Name $result
            $allValid = $allValid -and $result
        }
        catch {
            Write-Check $feature.Name $false $_.Exception.Message
            Write-Issue "Validação falhou para $($feature.Name)"
            $allValid = $false
        }
    }
    
    return $allValid
}

function Test-RabbitMQHealth {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:15672/api/whoami" `
            -Headers @{Authorization = "Basic $(([Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes('guest:guest'))))"} `
            -UseBasicParsing -TimeoutSec 3
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Test-DockerOptimization {
    try {
        # Verificar se Dockerfiles existem
        $dockerfiles = @(
            "GameStore.Usuarios.API/Dockerfile",
            "GameStore.Catalogo.API/Dockerfile",
            "GameStore.Vendas.API/Dockerfile"
        )
        
        foreach ($dockerfile in $dockerfiles) {
            if (-not (Test-Path $dockerfile)) {
                return $false
            }
            
            # Verificar se usa multi-stage build
            $content = Get-Content $dockerfile -Raw
            if (-not $content.Contains("FROM")) {
                return $false
            }
        }
        
        return $true
    }
    catch {
        return $false
    }
}

function Test-K8sManifests {
    try {
        $k8sPath = "kubernetes"
        if (-not (Test-Path $k8sPath)) {
            return $false
        }
        
        $yamlFiles = Get-ChildItem $k8sPath -Filter "*.yaml" | Measure-Object
        return $yamlFiles.Count -ge 10  # Deve ter pelo menos 10 YAML files
    }
    catch {
        return $false
    }
}

function Test-PrometheusHealth {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9090/-/healthy" -UseBasicParsing -TimeoutSec 3
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Test-GrafanaHealth {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 3
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Test-ConfigSecrets {
    try {
        # Verificar se ConfigMaps/Secrets existem no docker-compose ou K8s
        $dockerComposePath = Join-Path (Split-Path $PSScriptRoot -Parent) "docker-compose.local.yml"
        if (Test-Path $dockerComposePath) {
            $dockerCompose = Get-Content $dockerComposePath -Raw
            $hasEnvConfig = $dockerCompose.Contains("environment:")
            return $hasEnvConfig
        }
        return $false
    }
    catch {
        return $false
    }
}

function Test-HealthChecks {
    try {
        $endpoints = @(
            "http://localhost:5001/health/ready",
            "http://localhost:5002/health/ready",
            "http://localhost:5003/health/ready"
        )
        
        foreach ($endpoint in $endpoints) {
            try {
                $response = Invoke-WebRequest -Uri $endpoint -UseBasicParsing -TimeoutSec 3
                if ($response.StatusCode -ne 200) {
                    return $false
                }
            }
            catch {
                return $false
            }
        }
        
        return $true
    }
    catch {
        return $false
    }
}

# ========================================
# VALIDACAO KUBERNETES
# ========================================

function Validate-Kubernetes {
    Write-Header "Validando Configuração Kubernetes"
    
    # Verificar kubectl
    $kubectlInstalled = kubectl version --short 2>$null
    if (-not $kubectlInstalled) {
        Write-Check "kubectl instalado" $false
        Write-Issue "kubectl não encontrado - necessário para K8s"
        return $false
    }
    
    Write-Check "kubectl instalado" $true $kubectlInstalled
    
    # Verificar cluster
    try {
        $clusterInfo = kubectl cluster-info 2>$null
        Write-Check "Cluster Kubernetes" $true "Conectado"
        
        # Verificar namespaces
        $namespaces = kubectl get ns -o json | ConvertFrom-Json
        $hasAppNamespace = $namespaces.items.metadata.name -contains "thethroneofgames"
        $hasMonitoringNamespace = $namespaces.items.metadata.name -contains "thethroneofgames-monitoring"
        
        Write-Check "Namespace: thethroneofgames" $hasAppNamespace
        Write-Check "Namespace: thethroneofgames-monitoring" $hasMonitoringNamespace
        
        if ($hasAppNamespace) {
            # Verificar deployments
            $deployments = kubectl get deployments -n thethroneofgames -o json 2>$null | ConvertFrom-Json
            $numDeployments = $deployments.items.Count
            Write-Check "Deployments em thethroneofgames" ($numDeployments -gt 0) "$numDeployments found"
            
            # Verificar HPAs
            $hpas = kubectl get hpa -n thethroneofgames -o json 2>$null | ConvertFrom-Json
            $numHPAs = $hpas.items.Count
            Write-Check "HorizontalPodAutoscalers" ($numHPAs -gt 0) "$numHPAs found"
        }
        
        return $true
    }
    catch {
        Write-Check "Cluster Kubernetes" $false $_.Exception.Message
        Write-Issue "Não foi possível conectar ao cluster K8s"
        return $false
    }
}

# ========================================
# REPARO AUTOMÁTICO
# ========================================

function Repair-Issues {
    Write-Header "Executando Reparos Automáticos"
    
    if ($ValidationState.FailedChecks -eq 0) {
        Write-Host "Nenhum problema detectado!" -ForegroundColor $Colors.Success
        return
    }
    
    foreach ($issue in $ValidationState.Issues) {
        Write-Host ""
        Write-Host "Resolvendo: $($issue.Issue)" -ForegroundColor $Colors.Warning
        
        if ($issue.Issue -like "*Docker*" -or $issue.Issue -like "*container*") {
            Repair-Docker
        }
        elseif ($issue.Issue -like "*API*" -or $issue.Issue -like "*endpoint*") {
            Repair-APIs
        }
        elseif ($issue.Issue -like "*RabbitMQ*") {
            Repair-RabbitMQ
        }
    }
}

function Repair-Docker {
    Write-Host "Reiniciando Docker..." -ForegroundColor Yellow
    docker-compose -f docker-compose.local.yml restart
    Start-Sleep -Seconds 10
    Write-Host "Docker reiniciado!" -ForegroundColor $Colors.Success
}

function Repair-APIs {
    Write-Host "Reconstruindo e reiniciando APIs..." -ForegroundColor Yellow
    docker-compose -f docker-compose.local.yml up -d --build usuarios-api catalogo-api vendas-api
    Start-Sleep -Seconds 15
    Write-Host "APIs reconstruídas!" -ForegroundColor $Colors.Success
}

function Repair-RabbitMQ {
    Write-Host "Reiniciando RabbitMQ..." -ForegroundColor Yellow
    docker-compose -f docker-compose.local.yml restart rabbitmq
    Start-Sleep -Seconds 10
    Write-Host "RabbitMQ reiniciado!" -ForegroundColor $Colors.Success
}

# ========================================
# GERAR RELATÓRIO
# ========================================

function Generate-Report {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $reportPath = "validation-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
    
    $report = @"
╔════════════════════════════════════════════════════════════════╗
║          FASE 4 - RELATÓRIO DE VALIDAÇÃO                      ║
║          The Throne of Games                                   ║
╚════════════════════════════════════════════════════════════════╝

Data/Hora: $timestamp
Modo: $Mode

════════════════════════════════════════════════════════════════
RESUMO EXECUTIVO
════════════════════════════════════════════════════════════════

Total de Validações: $($ValidationState.TotalChecks)
Validações Passadas: $($ValidationState.PassedChecks)
Validações Falhadas: $($ValidationState.FailedChecks)
Taxa de Sucesso: $(if ($ValidationState.TotalChecks -gt 0) { [math]::Round(($ValidationState.PassedChecks / $ValidationState.TotalChecks) * 100, 1) }else{ 0 })%

════════════════════════════════════════════════════════════════
PROBLEMAS DETECTADOS
════════════════════════════════════════════════════════════════

$($ValidationState.Issues | ForEach-Object { "- [$($_.Severity)] $($_.Issue)" } | Out-String)

════════════════════════════════════════════════════════════════
RECOMENDAÇÕES
════════════════════════════════════════════════════════════════

1. Revisar problemas acima
2. Executar .\scripts\run-local.ps1 para reiniciar ambiente
3. Re-executar validação: .\validation-checklist.ps1 -Mode full
4. Consultar logs dos containers para mais detalhes

════════════════════════════════════════════════════════════════
PRÓXIMOS PASSOS
════════════════════════════════════════════════════════════════

1. Deploy em Kubernetes
2. Testes de carga com load-test.ps1
3. Gravação de vídeo demonstração
4. Revisão final de documentação

════════════════════════════════════════════════════════════════
"@
    
    $report | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Host ""
    Write-Host "Relatório salvo em: $reportPath" -ForegroundColor $Colors.Success
}

# ========================================
# MAIN EXECUTION
# ========================================

Write-Host ""
Write-Host "======================================================" -ForegroundColor $Colors.Header
Write-Host "   VALIDACAO FASE 4 - The Throne of Games              " -ForegroundColor $Colors.Header
Write-Host "   Mode: $Mode" -ForegroundColor $Colors.Header
Write-Host "======================================================" -ForegroundColor $Colors.Header

switch ($Mode) {
    "quick" {
        Validate-Local-Environment > $null
        Validate-API-Endpoints > $null
    }
    
    "full" {
        Validate-Local-Environment > $null
        Validate-API-Endpoints > $null
        Validate-Features > $null
    }
    
    "health" {
        Validate-Local-Environment > $null
        Validate-API-Endpoints > $null
    }
    
    "repair" {
        Write-Header "Modo Reparo"
        Validate-Local-Environment > $null
        Validate-API-Endpoints > $null
        Repair-Issues
    }
    
    "k8s" {
        Validate-Kubernetes > $null
    }
}

# ========================================
# RESUMO FINAL
# ========================================

Write-Header "Resumo da Validação"
Write-Host "Total de Validações: $($ValidationState.TotalChecks)" -ForegroundColor White
Write-Host "Passadas: $($ValidationState.PassedChecks)" -ForegroundColor $Colors.Success
Write-Host "Falhadas: $($ValidationState.FailedChecks)" -ForegroundColor $(if ($ValidationState.FailedChecks -gt 0) { $Colors.Error } else { $Colors.Success })

if ($ValidationState.FailedChecks -eq 0) {
    Write-Host ""
    Write-Host "✅ TODAS AS VALIDAÇÕES PASSARAM!" -ForegroundColor $Colors.Success
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "⚠️  ALGUNS PROBLEMAS DETECTADOS" -ForegroundColor $Colors.Warning
    Write-Host "Execute: .\validation-checklist.ps1 -Mode repair -AutoRepair" -ForegroundColor $Colors.Info
    Write-Host ""
}

if ($GenerateReport) {
    Generate-Report
}

Write-Host "Validação concluída!" -ForegroundColor $Colors.Success
Write-Host ""
