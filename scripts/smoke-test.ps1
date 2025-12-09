param(
    [string]$BaseUrl = $null,
    [int]$Retries = 30,
    [int]$IntervalSeconds = 2
)

if (-not $BaseUrl) {
    if ($env:BASE_URL) { $BaseUrl = $env:BASE_URL } else { $BaseUrl = 'http://localhost:5000' }
}

Write-Host "[smoke-test] BaseUrl=$BaseUrl Retries=$Retries IntervalSeconds=$IntervalSeconds"

function Wait-ForUrl {
    param($Url)
    for ($i = 1; $i -le $Retries; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -Method Get
            if ($resp.StatusCode -eq 200) {
                Write-Host "[smoke-test] OK $Url"
                return $true
            }
            Write-Host "[smoke-test] Waiting for $Url -> $($resp.StatusCode) ($i/$Retries)"
        }
        catch {
            Write-Host "[smoke-test] Waiting for $Url -> error ($i/$Retries)"
        }
        Start-Sleep -Seconds $IntervalSeconds
    }
    Write-Error "[smoke-test] Timeout waiting for $Url"
    return $false
}

# Checks
if (-not (Wait-ForUrl "$BaseUrl/health")) { exit 1 }
if (-not (Wait-ForUrl "$BaseUrl/health/ready")) { exit 1 }

Write-Host "[smoke-test] Performing sample request to /"
try {
    $r = Invoke-WebRequest -Uri "$BaseUrl/" -UseBasicParsing -TimeoutSec 10 -Method Get
    Write-Host "[smoke-test] GET / -> $($r.StatusCode)"
} catch {
    Write-Error "[smoke-test] GET / failed: $_"
    exit 1
}

Write-Host "[smoke-test] Completed"
exit 0
