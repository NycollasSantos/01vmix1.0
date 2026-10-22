#requires -Version 2.0
<#
    deploy.ps1 - install the rebuilt 01vmix (free / open-source, no license)
    over the existing AirFader installation.

    The product was renamed from "AirFader" to "01vmix":
      * executable        AirFader.exe  -> 01vmix.exe
      * install folder    C:\Program Files (x86)\air\AirFader
                       -> C:\Program Files (x86)\01vmix
      * data folder       %APPDATA%\AirFader -> %APPDATA%\01vmix
      * Start menu entry  AirFader\AirFader.lnk -> 01vmix\01vmix.lnk

    The target folder is writable by administrators only, so this script
    re-launches itself elevated when it is not.

    Usage:
        powershell -NoProfile -ExecutionPolicy Bypass -File re\deploy.ps1
#>

param(
    [string]$InstallDir = 'C:\Program Files (x86)\01vmix',
    [string]$LegacyDir  = 'C:\Program Files (x86)\air\AirFader',
    [string]$RepoRoot   = (Split-Path -Parent $PSScriptRoot),
    [switch]$Elevated
)

$ErrorActionPreference = 'Stop'

# Every run is logged so that failures inside the elevated child process
# (whose console output is not attached to the caller) can be inspected.
$LogFile = Join-Path $env:TEMP '01vmix-deploy.log'
function Write-Log([string]$Message) {
    $line = '{0}  {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
    Write-Host $line
    try { Add-Content -LiteralPath $LogFile -Value $line } catch { }
}
trap {
    Write-Log ("ERROR: " + $_.Exception.ToString())
    exit 1
}

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $pr = New-Object Security.Principal.WindowsPrincipal($id)
    return $pr.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    if ($Elevated) {
        Write-Host 'ERROR: could not obtain elevation.' -ForegroundColor Red
        exit 1
    }
    Write-Host 'Requesting administrator rights...' -ForegroundColor Yellow
    $psExe = Join-Path $PSHOME 'powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"",
                 '-InstallDir', "`"$InstallDir`"", '-LegacyDir', "`"$LegacyDir`"",
                 '-RepoRoot', "`"$RepoRoot`"", '-Elevated')
    try {
        $proc = Start-Process -FilePath $psExe -ArgumentList $argList -Verb RunAs -Wait -PassThru
        if ($proc.ExitCode -ne 0) {
            Write-Host "ERROR: elevated deploy failed (exit code $($proc.ExitCode))." -ForegroundColor Red
            exit $proc.ExitCode
        }
    }
    catch {
        Write-Host "ERROR: elevation failed - $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    exit 0
}

$BuildDir  = Join-Path $RepoRoot 'build\out'
$BackupDir = Join-Path $RepoRoot 're\original'

if (-not (Test-Path -LiteralPath (Join-Path $BuildDir '01vmix.exe'))) {
    Write-Host "ERROR: no build found at $BuildDir\01vmix.exe - run re\build.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "Installing 01vmix to $InstallDir" -ForegroundColor Cyan

# --- keep a copy of the original commercial binary ---------------------------
if (-not (Test-Path -LiteralPath $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
}
$backup = Join-Path $BackupDir 'AirFader.exe.orig'
if ((Test-Path -LiteralPath (Join-Path $LegacyDir 'AirFader.exe')) -and -not (Test-Path -LiteralPath $backup)) {
    Copy-Item -LiteralPath (Join-Path $LegacyDir 'AirFader.exe') -Destination $backup -Force
    Write-Host "  backed up the original binary -> $backup"
}

# --- create the target folder ------------------------------------------------
if (-not (Test-Path -LiteralPath $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# --- third-party runtime dependencies (taken from the existing install) -------
foreach ($name in @('protobuf-net.dll', 'AirControls.dll',
                    'Sanford.Multimedia.Midi.dll', 'Sanford.Multimedia.dll',
                    'Sanford.Multimedia.Timers.dll', 'Sanford.Collections.dll',
                    'Sanford.Threading.dll', 'Meterdata.dat')) {
    foreach ($src in @($InstallDir, $LegacyDir)) {
        $p = Join-Path $src $name
        if (-not (Test-Path -LiteralPath $p)) { continue }
        $dst = Join-Path $InstallDir $name
        if ([string]::Equals($p, $dst, [StringComparison]::OrdinalIgnoreCase)) { break }
        Copy-Item -LiteralPath $p -Destination $InstallDir -Force
        break
    }
}

# --- the rebuilt application -------------------------------------------------
foreach ($f in @(
    @{ Src = (Join-Path $BuildDir '01vmix.exe');         Dst = '01vmix.exe' },
    @{ Src = (Join-Path $BuildDir '01vmix.pdb');         Dst = '01vmix.pdb' },
    @{ Src = (Join-Path $BuildDir '01vmix.exe.config');  Dst = '01vmix.exe.config' },
    @{ Src = (Join-Path $RepoRoot 'LICENSE');             Dst = 'LICENSE' },
    @{ Src = (Join-Path $RepoRoot 'README.md');           Dst = 'README.md' }
)) {
    if (-not (Test-Path -LiteralPath $f.Src)) { continue }
    Get-Process 01vmix, AirFader -ErrorAction SilentlyContinue | Stop-Process -Force
    Copy-Item -LiteralPath $f.Src -Destination (Join-Path $InstallDir $f.Dst) -Force
    Write-Host "  installed $($f.Dst)"
}

# --- remove the old installation ---------------------------------------------
# The vendor's Inno Setup uninstaller (unins000.exe/dat) hardcodes the old
# paths, so it cannot survive the move; it is dropped together with the old
# folder and the Add/Remove Programs entry is rewritten below.
if (Test-Path -LiteralPath $LegacyDir) {
    Remove-Item -LiteralPath $LegacyDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  removed the old installation $LegacyDir"
}
$legacyParent = Split-Path -Parent $LegacyDir
if ((Test-Path -LiteralPath $legacyParent) -and
    -not (Get-ChildItem -LiteralPath $legacyParent -Force)) {
    Remove-Item -LiteralPath $legacyParent -Force -ErrorAction SilentlyContinue
}

# --- Start menu shortcut -----------------------------------------------------
$menuRoot = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs'
$newMenu  = Join-Path $menuRoot '01vmix'
$oldMenu  = Join-Path $menuRoot 'AirFader'
if (-not (Test-Path -LiteralPath $newMenu)) {
    New-Item -ItemType Directory -Path $newMenu -Force | Out-Null
}
$sh = New-Object -ComObject WScript.Shell
$lnk = $sh.CreateShortcut((Join-Path $newMenu '01vmix.lnk'))
$lnk.TargetPath       = Join-Path $InstallDir '01vmix.exe'
$lnk.WorkingDirectory = $InstallDir
$lnk.Description      = '01vmix'
$lnk.IconLocation     = (Join-Path $InstallDir '01vmix.exe') + ',0'
$lnk.Save()
Write-Host "  created start menu shortcut $newMenu\01vmix.lnk"
if (Test-Path -LiteralPath $oldMenu) {
    Remove-Item -LiteralPath $oldMenu -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  removed the old start menu shortcut $oldMenu"
}

# --- Add/Remove Programs entry ----------------------------------------------
$uninstallKey = 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{8F80CF64-0FC9-4C60-9ACC-262D276B174D}_is1'
if (Test-Path -LiteralPath $uninstallKey) {
    Set-ItemProperty -LiteralPath $uninstallKey -Name 'DisplayName'     -Value '01vmix version 3.2'
    Set-ItemProperty -LiteralPath $uninstallKey -Name 'InstallLocation' -Value ($InstallDir + '\')
    Set-ItemProperty -LiteralPath $uninstallKey -Name 'UninstallString' -Value ('"' + $env:ComSpec + '" /c rmdir /s /q "' + $InstallDir + '"')
    Set-ItemProperty -LiteralPath $uninstallKey -Name 'Publisher'       -Value '01vmix contributors'
    Set-ItemProperty -LiteralPath $uninstallKey -Name 'URLInfoAbout'    -Value ''
    Write-Host '  updated the Add/Remove Programs entry'
}

# --- migrate the per-user data folder ---------------------------------------
$oldData = Join-Path $env:APPDATA 'AirFader'
$newData = Join-Path $env:APPDATA '01vmix'
if (Test-Path -LiteralPath $oldData) {
    if (-not (Test-Path -LiteralPath $newData)) {
        Move-Item -LiteralPath $oldData -Destination $newData -Force
    } else {
        Copy-Item -Path (Join-Path $oldData '*') -Destination $newData -Force -ErrorAction SilentlyContinue
    }
    Write-Host "  migrated the data folder -> $newData"
}

Write-Host ''
Write-Host 'Verifying...' -ForegroundColor Cyan
$info = Get-Item -LiteralPath (Join-Path $InstallDir '01vmix.exe')
Write-Host "  01vmix.exe  $($info.Length) bytes"

$refs = & (Join-Path $RepoRoot 're\tools\ShowRefs.exe') $info.FullName 2>&1 |
        Select-String -Pattern 'ImageRuntime'
foreach ($r in $refs) { Write-Host "  $($r.Line.Trim())" }

Write-Host ''
Write-Host 'DEPLOY OK' -ForegroundColor Green
exit 0
