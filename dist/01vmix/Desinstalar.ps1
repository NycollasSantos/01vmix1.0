$ErrorActionPreference = 'Stop'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\01vmix'
$shortcutDir = Join-Path ([Environment]::GetFolderPath('Programs')) '01vmix'
$key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\01vmix'
Get-Process -Name '01vmix' -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path -LiteralPath $shortcutDir) { Remove-Item -LiteralPath $shortcutDir -Recurse -Force }
if (Test-Path -LiteralPath $key) { Remove-Item -LiteralPath $key -Recurse -Force }
if (Test-Path -LiteralPath $installDir) { Remove-Item -LiteralPath $installDir -Recurse -Force }
Write-Host '01vmix desinstalado. Os dados do usuario em AppData foram preservados.'
