# PRE-COMMIT VALIDATION
# Valida arquivos antes do commit (secrets, senhas, tamanhos)

$ErrorActionPreference = "Continue"
Write-Host "Running pre-commit validations..." -ForegroundColor Yellow

$script:Issues = @()
$baseDir = "..\.."

# 1. Scan for secrets
Write-Host "`nScanning for secrets and credentials..." -ForegroundColor Cyan
$secretPatterns = @(
    "password\s*=\s*[`"'][^`"']+[`"']",
    "api[_-]?key\s*=\s*[`"'][^`"']+[`"']",
    "token\s*=\s*[`"'][^`"']+[`"']",
    "secret\s*=\s*[`"'][^`"']+[`"']",
    "-----BEGIN (RSA|DSA|EC|OPENSSH) PRIVATE KEY-----"
)

$filesToScan = Get-ChildItem -Path $baseDir -Recurse -Include *.cs,*.json,*.yml,*.yaml,*.config `
    -Exclude node_modules,bin,obj,packages | Where-Object { -not $_.PSIsContainer }

$secretsFound = $false
foreach ($file in $filesToScan) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content) {
        foreach ($pattern in $secretPatterns) {
            if ($content -match $pattern) {
                $script:Issues += "Possible secret in: $($file.FullName)"
                Write-Host "[WARN] Possible secret in $($file.Name)" -ForegroundColor Yellow
                $secretsFound = $true
            }
        }
    }
}

if (-not $secretsFound) {
    Write-Host "[OK] No obvious secrets found" -ForegroundColor Green
}

# 2. Check for hardcoded connection strings with passwords
Write-Host "`nChecking connection strings..." -ForegroundColor Cyan
$appSettings = Get-ChildItem -Path $baseDir -Recurse -Filter appsettings*.json -ErrorAction SilentlyContinue
foreach ($file in $appSettings) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "Password\s*=\s*(?!<<.*>>)[^;]+") {
        Write-Host "[WARN] Connection string with password in $($file.Name)" -ForegroundColor Yellow
    } else {
        Write-Host "[OK] $($file.Name)" -ForegroundColor Green
    }
}

# 3. Check for large files
Write-Host "`nChecking for large files (>10MB)..." -ForegroundColor Cyan
$largeFiles = Get-ChildItem -Path $baseDir -Recurse -File | Where-Object { $_.Length -gt 10MB }
if ($largeFiles.Count -gt 0) {
    $largeFiles | ForEach-Object {
        $sizeMB = [math]::Round($_.Length / 1MB, 2)
        $script:Issues += "Large file: $($_.Name) ($sizeMB MB)"
        Write-Host "[WARN] Large file: $($_.Name) ($sizeMB MB)" -ForegroundColor Yellow
    }
} else {
    Write-Host "[OK] No large files found" -ForegroundColor Green
}

# 4. Check line endings (gitattributes)
Write-Host "`nChecking .gitattributes..." -ForegroundColor Cyan
$gitattributes = Join-Path $baseDir ".gitattributes"
if (Test-Path $gitattributes) {
    Write-Host "[OK] .gitattributes exists" -ForegroundColor Green
} else {
    Write-Host "[INFO] .gitattributes not found (optional)" -ForegroundColor Yellow
}

Write-Host "`n=== PRE-COMMIT VALIDATION SUMMARY ===" -ForegroundColor Cyan
if ($script:Issues.Count -eq 0) {
    Write-Host "All pre-commit checks passed" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Issues found: $($script:Issues.Count)" -ForegroundColor Yellow
    Write-Host "(Review these before committing)" -ForegroundColor Yellow
    $script:Issues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    exit 0
}
