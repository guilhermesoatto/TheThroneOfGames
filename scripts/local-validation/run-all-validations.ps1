# MASTER VALIDATION SCRIPT
# Executa todas as validacoes locais em sequencia

$ErrorActionPreference = "Continue"
$StartTime = Get-Date

Write-Host "`n=== LOCAL VALIDATION SUITE ===" -ForegroundColor Cyan
Write-Host "Executando todas as validacoes antes do commit`n" -ForegroundColor Yellow

# Navegar para o diretorio correto
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Array para armazenar resultados
$results = @()

# Etapa 0: Build
Write-Host "`n=== STEP 0: BUILD ===" -ForegroundColor Cyan
Write-Host "Building solution..." -ForegroundColor Yellow

Set-Location ..\..
$buildOutput = dotnet build TheThroneOfGames.sln --verbosity quiet 2>&1
$buildSuccess = $LASTEXITCODE -eq 0
Set-Location scripts\local-validation

if ($buildSuccess) {
    Write-Host "Build completed" -ForegroundColor Green
    $results += [PSCustomObject]@{ Step = "0-Build"; Status = "PASSED"; Duration = "N/A" }
} else {
    Write-Host "Build failed" -ForegroundColor Red
    $errorLines = $buildOutput | Select-String "error" | Select-Object -First 5
    $errorLines | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $results += [PSCustomObject]@{ Step = "0-Build"; Status = "FAILED"; Duration = "N/A" }
    
    Write-Host "`nCRITICAL: Build failed. Cannot proceed with tests." -ForegroundColor Red
    exit 1
}

# Scripts de validacao
$validationScripts = @(
    @{ Number = 1; Name = "Unit Tests"; Script = "01-unit-tests.ps1" },
    @{ Number = 2; Name = "Integration Tests"; Script = "02-integration-tests.ps1" },
    @{ Number = 3; Name = "Containers Cycle"; Script = "03-containers-cycle.ps1" },
    @{ Number = 4; Name = "Orchestration"; Script = "04-orchestration-tests.ps1" },
    @{ Number = 5; Name = "Pipeline Validation"; Script = "05-pipeline-validation.ps1" },
    @{ Number = 6; Name = "Pre-Commit"; Script = "06-pre-commit-validation.ps1" }
)

foreach ($validation in $validationScripts) {
    Write-Host "`n=== STEP $($validation.Number): $($validation.Name.ToUpper()) ===" -ForegroundColor Cyan
    
    $stepStart = Get-Date
    & ".\$($validation.Script)"
    $stepDuration = ((Get-Date) - $stepStart).TotalSeconds
    
    if ($LASTEXITCODE -eq 0) {
        $results += [PSCustomObject]@{ 
            Step = "$($validation.Number)-$($validation.Name)"; 
            Status = "PASSED"; 
            Duration = "$([math]::Round($stepDuration, 1))s" 
        }
    } else {
        $results += [PSCustomObject]@{ 
            Step = "$($validation.Number)-$($validation.Name)"; 
            Status = "FAILED"; 
            Duration = "$([math]::Round($stepDuration, 1))s" 
        }
    }
}

# Resumo final
Write-Host "`n`n=== FINAL RESULTS ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize

$totalDuration = ((Get-Date) - $StartTime).TotalSeconds
$passed = ($results | Where-Object { $_.Status -eq "PASSED" }).Count
$failed = ($results | Where-Object { $_.Status -eq "FAILED" }).Count

Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  Passed: $passed/$($results.Count)" -ForegroundColor Green
Write-Host "  Failed: $failed/$($results.Count)" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "  Total Duration: $([math]::Round($totalDuration, 1))s" -ForegroundColor Yellow

if ($failed -eq 0) {
    Write-Host "`nALL VALIDATIONS PASSED - Ready to commit!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nVALIDATIONS FAILED - Fix issues before committing" -ForegroundColor Red
    exit 1
}
