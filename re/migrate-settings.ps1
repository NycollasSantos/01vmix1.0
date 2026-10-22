#requires -Version 2.0
<#
    migrate-settings.ps1 - move the user settings of the commercial AirFader
    installation over to the renamed 01vmix build.

    Windows stores user settings of a .NET application in

        %LOCALAPPDATA%\<company>\<app>_Url_<hash>\<version>\user.config

    The company level is taken from the assembly attributes; because the
    original binary shipped without a company name the folder was named after
    the entry point namespace ("AirFader").  01vmix sets AssemblyCompany, so
    the settings now live in %LOCALAPPDATA%\01vmix\...

    The settings section itself ("AirFader.Properties.Settings") is unchanged,
    therefore the existing values can be copied over verbatim.  The license
    settings are dropped - 01vmix does not use them.

    Usage:
        powershell -NoProfile -ExecutionPolicy Bypass -File re\migrate-settings.ps1
        powershell -NoProfile -ExecutionPolicy Bypass -File re\migrate-settings.ps1 -Cleanup
#>

param(
    [string]$OldRoot = (Join-Path $env:LOCALAPPDATA 'AirFader'),
    [string]$NewRoot = (Join-Path $env:LOCALAPPDATA '01vmix'),
    [string]$OldRoaming = (Join-Path $env:APPDATA 'AirFader'),
    [switch]$Cleanup
)

$ErrorActionPreference = 'Stop'

function Get-UserConfig([string]$Root, [string]$ExePattern) {
    if (-not (Test-Path -LiteralPath $Root)) { return @() }
    @(Get-ChildItem -LiteralPath $Root -Recurse -Filter 'user.config' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match $ExePattern })
}

$oldConfigs = Get-UserConfig $OldRoot 'AirFader\.exe_Url_'
$newConfigs = Get-UserConfig $NewRoot '01vmix\.exe_Url_'

if ($oldConfigs.Count -eq 0) {
    Write-Host "No legacy settings found under $OldRoot - nothing to migrate."
} else {
    # the richest legacy file holds the real user settings
    $source = $oldConfigs | Sort-Object Length -Descending | Select-Object -First 1
    Write-Host "Legacy settings: $($source.FullName) ($($source.Length) bytes)"

    [xml]$doc = Get-Content -LiteralPath $source.FullName -Raw
    $section = $doc.configuration.userSettings.'AirFader.Properties.Settings'
    if ($section) {
        foreach ($name in @('LicenseKey', 'LicenseUser')) {
            $node = @($section.setting) | Where-Object { $_.name -eq $name } | Select-Object -First 1
            if ($node) { [void]$section.RemoveChild($node) }
        }
    }

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = '    '
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)

    if ($newConfigs.Count -eq 0) {
        Write-Host "No 01vmix settings folder found under $NewRoot - start 01vmix once first." -ForegroundColor Yellow
    }

    foreach ($target in $newConfigs) {
        $writer = [System.Xml.XmlWriter]::Create($target.FullName, $settings)
        try { $doc.Save($writer) } finally { $writer.Close() }
        Write-Host "  migrated -> $($target.FullName) ($((Get-Item $target.FullName).Length) bytes)"
    }
}

if ($Cleanup) {
    foreach ($legacy in @($OldRoot, $OldRoaming)) {
        if (Test-Path -LiteralPath $legacy) {
            Remove-Item -LiteralPath $legacy -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  removed legacy folder $legacy"
        }
    }
}

Write-Host 'SETTINGS MIGRATION OK' -ForegroundColor Green
exit 0
