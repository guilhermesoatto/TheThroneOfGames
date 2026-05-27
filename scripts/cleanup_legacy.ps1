# Arquiva diretórios legados em artifacts/legacy_backup_{timestamp}
$paramScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
param(
    [string]$Root = $null,
    [switch]$WhatIf
)
if([string]::IsNullOrEmpty($Root)){
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $Root = (Resolve-Path (Join-Path -Path $scriptDir -ChildPath "..")).Path
}
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$backupDir = Join-Path -Path (Join-Path -Path $Root -ChildPath "artifacts") -ChildPath ("legacy_backup_$timestamp")
$targets = @(
    "TheThroneOfGames\TheThroneOfGames.Domain",
    "TheThroneOfGames\TheThroneOfGames.Application",
    "TheThroneOfGames\TheThroneOfGames.Infrastructure"
)
Write-Host "Backup target folder: $backupDir"
if(!(Test-Path -Path $backupDir)){
    if(-not $WhatIf){ New-Item -Path $backupDir -ItemType Directory -Force | Out-Null }
}
$report = @()
foreach($t in $targets){
    $full = Join-Path -Path $Root -ChildPath $t
    if(Test-Path -Path $full){
        $dest = Join-Path -Path $backupDir -ChildPath (Split-Path -Path $t -Leaf)
        if($WhatIf){
            Write-Host "WhatIf: Move '$full' -> '$dest'"
            $report += [pscustomobject]@{ Source=$full; Dest=$dest; Action="WhatIf" }
        } else {
            Write-Host "Moving: $full -> $dest"
            Move-Item -Path $full -Destination $dest -Force
            $report += [pscustomobject]@{ Source=$full; Dest=$dest; Action="Moved" }
        }
    } else {
        Write-Host "Not found: $full"
        $report += [pscustomobject]@{ Source=$full; Dest=$null; Action="NotFound" }
    }
}
# Save report
$reportFile = Join-Path -Path $backupDir -ChildPath "cleanup_report_$timestamp.json"
$report | ConvertTo-Json -Depth 5 | Out-File -FilePath $reportFile -Encoding UTF8
Write-Host "Report saved to: $reportFile"
Write-Host "Done."