<#
Helm dry-run helper script

Usage:
  - Run lint + template + dry-run:
      .\scripts\helm-dryrun.ps1

  - Also run tests (optional):
      .\scripts\helm-dryrun.ps1 -RunTests

This script will:
  1. Ensure `helm` is available (try Chocolatey, Scoop or download fallback).
  2. Run `helm lint` on `helm/thethroneofgames` and save output to `artifacts/helm-lint.txt`.
  3. Render templates with `values-dev.yaml` and save to `artifacts/helm-template.yaml`.
  4. Run `helm install --dry-run --debug` and save to `artifacts/helm-dryrun.txt`.
  5. Optionally run `dotnet test` and save TRX results to `artifacts/test-results.trx`.

Notes:
  - Run from repository root.
  - On Windows you can install Chocolatey (https://chocolatey.org) or Scoop (https://scoop.sh)
  - The script runs with current user context; some installers may require elevated privileges.
#>

param(
    [switch]$RunTests
)

function Write-Log {
    param($Message,$Level="INFO")
    $timestamp = (Get-Date).ToString('s')
    Write-Host "[$timestamp] [$Level] $Message"
}

# Navigate to repo root (script location assumption)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir
Set-Location ..\
$repoRoot = Get-Location

$artifacts = Join-Path $repoRoot "artifacts"
if (-not (Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }

function Ensure-Helm {
    Write-Log "Checking for helm..."
    $helmCmd = Get-Command helm -ErrorAction SilentlyContinue
    if ($helmCmd) {
        Write-Log "Helm found: $($helmCmd.Path)"
        return $true
    }

    Write-Log "Helm not found. Attempting to install..."

    # Try Chocolatey
    $choco = Get-Command choco -ErrorAction SilentlyContinue
    if ($choco) {
        Write-Log "Installing Helm via Chocolatey..."
        choco install kubernetes-helm -y | Out-Null
        if (Get-Command helm -ErrorAction SilentlyContinue) { Write-Log "Helm installed via Chocolatey."; return $true }
    }

    # Try Scoop
    $scoop = Get-Command scoop -ErrorAction SilentlyContinue
    if ($scoop) {
        Write-Log "Installing Helm via Scoop..."
        scoop install helm | Out-Null
        if (Get-Command helm -ErrorAction SilentlyContinue) { Write-Log "Helm installed via Scoop."; return $true }
    }

    # Manual download fallback
    Write-Log "Falling back to manual Helm download..."
    $helmVersion = "v3.12.0"  # fallback version; update if needed
    $zipName = "helm-$helmVersion-windows-amd64.zip"
    $downloadUrl = "https://get.helm.sh/$zipName"
    $tmp = Join-Path $env:TEMP "helm-install"
    if (-not (Test-Path $tmp)) { New-Item -ItemType Directory -Path $tmp | Out-Null }
    $zipPath = Join-Path $tmp $zipName

    try {
        Write-Log "Downloading Helm $helmVersion from $downloadUrl"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -UseBasicParsing
        Expand-Archive -Path $zipPath -DestinationPath $tmp -Force
        $helmExe = Join-Path $tmp "windows-amd64\helm.exe"
        if (Test-Path $helmExe) {
            $dest = Join-Path $env:ProgramFiles "helm"
            if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
            Copy-Item -Path $helmExe -Destination $dest -Force
            $env:Path = "$dest;$env:Path"
            Write-Log "Helm binary placed in $dest and PATH updated for this session."
            if (Get-Command helm -ErrorAction SilentlyContinue) { Write-Log "Helm is available."; return $true }
        }
    }
    catch {
        Write-Log "Failed to download or install Helm: $_" "ERROR"
    }

    # Manual download fallback with user-local directory
    Write-Log "Falling back to manual Helm download (user-local)..."
    $helmVersion = "v3.12.0"  # fallback version; update if needed
    $zipName = "helm-$helmVersion-windows-amd64.zip"
    $downloadUrl = "https://get.helm.sh/$zipName"
    $tmp = Join-Path $env:TEMP "helm-install"
    if (-not (Test-Path $tmp)) { New-Item -ItemType Directory -Path $tmp | Out-Null }
    $zipPath = Join-Path $tmp $zipName

    try {
        Write-Log "Downloading Helm $helmVersion from $downloadUrl"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -UseBasicParsing
        Expand-Archive -Path $zipPath -DestinationPath $tmp -Force
        $helmExe = Join-Path $tmp "windows-amd64\helm.exe"
        if (Test-Path $helmExe) {
            # Use user-local directory instead of Program Files
            $dest = Join-Path $env:USERPROFILE "AppData\Local\helm"
            if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
            Copy-Item -Path $helmExe -Destination $dest -Force
            $env:Path = "$dest;$env:Path"
            Write-Log "Helm binary placed in $dest and PATH updated for this session."
            if (Get-Command helm -ErrorAction SilentlyContinue) { Write-Log "Helm is available."; return $true }
        }
    }
    catch {
        Write-Log "Failed to download or install Helm: $_" "ERROR"
    }

    Write-Log "Helm installation failed; please install Helm manually and re-run this script." "ERROR"
    return $false
}

# Ensure helm available
if (-not (Ensure-Helm)) {
    Write-Log "Helm is required. Exiting." "ERROR"
    exit 2
}

# Run helm lint
$chartPath = Join-Path $repoRoot "helm\thethroneofgames"
$lintOutput = Join-Path $artifacts "helm-lint.txt"
Write-Log "Running helm lint on $chartPath"
try {
    helm lint $chartPath 2>&1 | Tee-Object -FilePath $lintOutput
    Write-Log "Helm lint completed. Output saved to $lintOutput"
} catch {
    Write-Log "helm lint failed: $_" "ERROR"
}

# Run helm template (render)
$templateOutput = Join-Path $artifacts "helm-template.yaml"
Write-Log "Rendering templates (helm template) using values-dev.yaml"
try {
    helm template thethroneofgames $chartPath -f (Join-Path $chartPath "values-dev.yaml") --namespace thethroneofgames 2>&1 | Tee-Object -FilePath $templateOutput
    Write-Log "helm template completed. Output saved to $templateOutput"
} catch {
    Write-Log "helm template failed: $_" "ERROR"
}

# Run dry-run install with debug
$dryRunOutput = Join-Path $artifacts "helm-dryrun.txt"
Write-Log "Running helm install --dry-run --debug"
try {
    helm install thethroneofgames $chartPath -f (Join-Path $chartPath "values-dev.yaml") --namespace thethroneofgames --dry-run --debug 2>&1 | Tee-Object -FilePath $dryRunOutput
    Write-Log "Helm dry-run completed. Output saved to $dryRunOutput"
} catch {
    Write-Log "helm dry-run failed: $_" "ERROR"
}

# Optionally run tests
if ($RunTests) {
    Write-Log "Running dotnet restore/build/test"
    try {
        dotnet restore | Tee-Object -FilePath (Join-Path $artifacts "dotnet-restore.txt")
        dotnet build -c Release | Tee-Object -FilePath (Join-Path $artifacts "dotnet-build.txt")
        $testTrx = Join-Path $artifacts "test-results.trx"
        dotnet test Test\Test.csproj -c Release --logger "trx;LogFileName=$testTrx" | Tee-Object -FilePath (Join-Path $artifacts "dotnet-test.txt")
        Write-Log "dotnet tests completed. TRX: $testTrx"
    } catch {
        Write-Log "dotnet test failed: $_" "ERROR"
    }
}

Write-Log "All tasks complete. Artifacts located in: $artifacts"
Write-Host "Files:"
Get-ChildItem -Path $artifacts | ForEach-Object { Write-Host " - $($_.FullName)" }

exit 0
