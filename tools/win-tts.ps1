# tools/win-tts.ps1
#
# Generates a narration clip (WAV) from text, using SAPI (System.Speech) already built into
# Windows -- no cloud API/key needed. Used by record-delivery.js to narrate each demo step
# in PT-BR ("-VoiceName" points at a pt-BR voice; the text itself is still Portuguese).
#
# "Microsoft Maria Desktop" is the pt-BR voice confirmed available on this machine and visible
# from Windows PowerShell 5.1 (which is what Node invokes via child_process -- PowerShell 7
# sees a different set of installed voices, so the exact name matters).
#
# NOTE: this file is kept plain-ASCII on purpose (including comments). Windows PowerShell 5.1
# reads .ps1 files using the system codepage unless there's a UTF-8 BOM, so any accented
# character typed directly into the script source (not passed as a -Text argument) gets
# corrupted and breaks the parser ("string missing terminator"). Confirmed the hard way.
#
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File win-tts.ps1 -Text "..." -OutFile "clip.wav"
param(
  [Parameter(Mandatory = $true)][string]$Text,
  [Parameter(Mandatory = $true)][string]$OutFile,
  [string]$VoiceName = "Microsoft Maria Desktop"
)

Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try { $synth.SelectVoice($VoiceName) } catch { Write-Warning "Voice '$VoiceName' not found, using default system voice." }
$synth.Rate = 0
$synth.SetOutputToWaveFile($OutFile)
$synth.Speak($Text)
$synth.Dispose()
