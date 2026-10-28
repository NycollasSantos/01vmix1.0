# Instalador por usuario do 01vmix. Execute com PowerShell 5.1 ou superior.
$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$destination = Join-Path $env:LOCALAPPDATA 'Programs\01vmix'
$programs = [Environment]::GetFolderPath('Programs')
$shortcutDir = Join-Path $programs '01vmix'
$shortcutPath = Join-Path $shortcutDir '01vmix.lnk'

if (-not (Test-Path -LiteralPath (Join-Path $source '01vmix.exe'))) {
    throw '01vmix.exe nao encontrado. Extraia a pasta inteira antes de executar o instalador.'
}

New-Item -ItemType Directory -Path $destination -Force | Out-Null
Get-ChildItem -LiteralPath $source -File | Where-Object { $_.Name -notin @('Instalar.ps1', 'LEIA-ME.txt') } |
    Copy-Item -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $source 'Desinstalar.ps1') -Destination $destination -Force

New-Item -ItemType Directory -Path $shortcutDir -Force | Out-Null
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $destination '01vmix.exe'
$shortcut.WorkingDirectory = $destination
$shortcut.Description = '01vmix'
$shortcut.IconLocation = (Join-Path $destination '01vmix.exe') + ',0'
$shortcut.Save()

$key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\01vmix'
New-Item -Path $key -Force | Out-Null
New-ItemProperty -Path $key -Name DisplayName -Value '01vmix' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $key -Name DisplayVersion -Value '3.2' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $key -Name Publisher -Value '01vmix contributors' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $key -Name InstallLocation -Value $destination -PropertyType String -Force | Out-Null
New-ItemProperty -Path $key -Name UninstallString -Value ('powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $destination 'Desinstalar.ps1') + '"') -PropertyType String -Force | Out-Null
Write-Host "01vmix instalado em $destination"
Write-Host 'Atalho criado no Menu Iniciar > 01vmix.'
