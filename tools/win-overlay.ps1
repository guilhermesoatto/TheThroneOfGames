# tools/win-overlay.ps1
#
# Opens an always-on-top terminal window, positioned in a corner of the screen, running the
# given command in a loop (e.g. `docker stats`) -- used by record-delivery.js to overlay live
# monitoring (containers/network) during full-screen recording (ffmpeg gdigrab).
#
# NOTE: this file is kept plain-ASCII on purpose (including comments) -- see win-tts.ps1 for
# why: Windows PowerShell 5.1 misreads accented characters typed directly into a .ps1 file
# without a UTF-8 BOM, which breaks the parser.
#
# Why via conhost.exe explicitly: on Windows 11 with Windows Terminal as the default host,
# `Start-Process powershell` opens the window under a different process than the one
# Start-Process returns, so neither `$proc.MainWindowHandle` nor `FindWindow` by title can
# reliably locate it. Forcing conhost.exe as the host avoids that ambiguity -- the resulting
# window always shows up as a powershell.exe process with MainWindowTitle populated correctly
# (confirmed via Get-Process, which is the lookup mechanism used below).
#
# This script SPAWNS the window and returns (prints the powershell.exe PID to stdout) -- the
# window keeps running detached. Caller is responsible for killing it later (taskkill /PID /F).
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File win-overlay.ps1 `
#     -Title "GameStoreOverlay-xyz" -Command "docker stats" -WorkingDirectory "C:\repo" `
#     -X 20 -Y 500 -Width 560 -Height 340
param(
  [Parameter(Mandatory = $true)][string]$Title,
  [Parameter(Mandatory = $true)][string]$Command,
  [string]$WorkingDirectory = (Get-Location).Path,
  [int]$X = 20,
  [int]$Y = 500,
  [int]$Width = 560,
  [int]$Height = 340,
  [int]$TimeoutSeconds = 10
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class GameStoreOverlayWin32 {
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
"@
$HWND_TOPMOST = [IntPtr](-1)
$SWP_SHOWWINDOW = 0x0040

$innerCommand = "`$host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$WorkingDirectory'; $Command"
Start-Process -FilePath "conhost.exe" -ArgumentList @('powershell', '-NoExit', '-Command', $innerCommand) | Out-Null

$target = $null
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not $target -and (Get-Date) -lt $deadline) {
  Start-Sleep -Milliseconds 300
  $target = Get-Process powershell -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -eq $Title } | Select-Object -First 1
}

if (-not $target) {
  Write-Error "win-overlay: window '$Title' did not appear within ${TimeoutSeconds}s."
  exit 1
}

[GameStoreOverlayWin32]::SetWindowPos($target.MainWindowHandle, $HWND_TOPMOST, $X, $Y, $Width, $Height, $SWP_SHOWWINDOW) | Out-Null
Write-Output $target.Id
