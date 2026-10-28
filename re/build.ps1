#requires -Version 5.1
<#
    Build script for the open-source AirFader build.

    Recompiles 01vmix from the patched, decompiled C# sources using Roslyn
    (csc.exe from Microsoft.Net.Compilers.Toolset 4.8) against .NET Framework
    4.8 reference assemblies.

    No .NET SDK is required - only the .NET Framework 4.8 runtime.
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    # NOTE: $PSScriptRoot is NOT reliably populated inside a param-block default
    # (it evaluates to '' when the script is launched with -File), which used to
    # send the output to C:\build\out. Leave it empty and resolve it below.
    [string]$OutputDir = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- paths ------------------------------------------------------------------
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot    = (Resolve-Path (Join-Path $ScriptDir '..')).Path
$SourceDir   = Join-Path $RepoRoot 're\src\AirFader'
# Third-party runtime dependencies are staged from an existing installation.
# The current install directory is the renamed one; the legacy vendor folder is
# still searched so the build keeps working during the transition.
$InstallDirs = @(
    'C:\Program Files (x86)\01vmix',
    'C:\Program Files (x86)\air\AirFader'
)
$ToolRoot    = 'C:\Users\DELL\AppData\Local\Temp\opencode'
$CscPath     = Join-Path $ToolRoot 'roslyn2\pkg\tasks\net472\csc.exe'
# The shipped AirFader.exe targets the .NET Framework 2.0 runtime
# (ImageRuntime v2.0.50727) and AirFader.exe.config activates that runtime with
# <supportedRuntime version="v2.0.50727"/>. Building against the net20 reference
# assemblies keeps mscorlib/System at Version=2.0.0.0 so the config stays valid.
$RefAsmDir   = Join-Path $ToolRoot 'refasm20\x\build\.NETFramework\v2.0'

if (-not $OutputDir) { $OutputDir = Join-Path $RepoRoot 'build\out' }

foreach ($p in @($SourceDir, $CscPath, $RefAsmDir)) {
    if (-not (Test-Path -LiteralPath $p)) {
        throw "Required path not found: $p"
    }
}

# first existing installation directory wins
$InstallDir = $null
foreach ($d in $InstallDirs) {
    if (Test-Path -LiteralPath $d) { $InstallDir = $d; break }
}
if (-not $InstallDir) { throw "No installation directory found (looked for: $($InstallDirs -join ', '))" }

$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "=== 01vmix open-source build ===" -ForegroundColor Cyan
Write-Host "  Source   : $SourceDir"
Write-Host "  Output   : $OutputDir"
Write-Host "  Compiler : $CscPath"

# --- source files -----------------------------------------------------------
$sources = @()
$sources += Get-ChildItem -Path $SourceDir -Recurse -Filter '*.cs' -File |
            Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
            Select-Object -ExpandProperty FullName

# --- framework references ----------------------------------------------------
# Only the assemblies the original AirFader.exe actually references, taken from
# the .NET Framework 2.0 reference assemblies. Referencing the whole folder
# would drag in Microsoft.Build.*/Microsoft.VisualBasic.* and add bogus
# assembly references that the original does not have.
#
# IMPORTANT: pass them as -lib:<dir> + -r:<shortname>. Passing a full path in
# -reference: makes csc import the .NET 4 assembly of the same name from the
# GAC as well ("CS1703: multiple assemblies with equivalent identity"), and
# that only happens when the command line comes from a response file.
$frameworkRefNames = @(
    'mscorlib.dll', 'System.dll', 'System.Data.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Xml.dll', 'System.Web.dll',
    'System.Configuration.dll', 'System.Management.dll'
)
$frameworkRefs = @()
$skipped = @()
foreach ($n in $frameworkRefNames) {
    $p = Join-Path $RefAsmDir $n
    if (-not (Test-Path -LiteralPath $p)) { throw "Missing reference assembly: $p" }
    # Some files in the reference-assembly folder are type-forwarding wrappers or
    # native images with no CLI metadata - they must not be passed to csc.
    try {
        [void][System.Reflection.AssemblyName]::GetAssemblyName($p)
        $frameworkRefs += $p
    } catch {
        $skipped += $n
    }
}
if ($skipped.Count) {
    Write-Host "  Skipped $($skipped.Count) non-managed reference file(s): $($skipped -join ', ')"
}

# --- third-party references (ship alongside the app) ------------------------
$appRefs = @()
foreach ($name in @('protobuf-net.dll', 'AirControls.dll',
                    'Sanford.Multimedia.Midi.dll', 'Sanford.Multimedia.dll',
                    'Sanford.Multimedia.Timers.dll', 'Sanford.Collections.dll',
                    'Sanford.Threading.dll')) {
    $ap = Join-Path $InstallDir $name
    if (Test-Path -LiteralPath $ap) { $appRefs += $ap }
}

# --- resources ---------------------------------------------------------------
# The original .resources streams are extracted verbatim from the shipped
# AirFader.exe (see re\tools\ExtractRes.cs) and embedded with /resource.
# Re-compiling the .resx through resgen produces streams that the CLR refuses
# to read ("not a valid resource file stream"), so we never regenerate them.
#
# NOTE: /linkresource must NOT be used here - Roslyn emits those as *linked*
# resources, which are not stored in the assembly at all. The runtime then
# looks for "<name>.resources" next to the exe and throws
# FileNotFoundException("Could not load file or assembly 'X.resources'").
# /resource embeds the bytes for real.
$ResDir = Join-Path $RepoRoot 're\resources'
$resourceArgs = @()
if (Test-Path -LiteralPath $ResDir) {
    foreach ($rf in Get-ChildItem -Path $ResDir -Filter '*.resources' -File | Sort-Object Name) {
        $resourceArgs += ('/resource:' + "`"$($rf.FullName)`"" + ',' + $rf.Name)
    }
    Write-Host "  Resources: $($resourceArgs.Count) stream(s) embedded verbatim from re\resources"
} else {
    Write-Warning "re\resources not found - icons and images will be missing."
}

# --- assemble the csc command line -------------------------------------------
# IMPORTANT: the arguments are passed to csc.exe directly, NOT through a
# response file. When the same arguments come from a .rsp, csc resolves the
# .NET 2.0 reference assemblies incorrectly and also imports the .NET 4 copies
# from the GAC ("CS1703: multiple assemblies with equivalent identity").
# The direct command line is fine - CreateProcess allows 32K characters.
$cscArgs = New-Object System.Collections.Generic.List[string]

$cscArgs.Add('-target:winexe')
$cscArgs.Add('-platform:x86')
$cscArgs.Add('-langversion:12.0')
$cscArgs.Add('-unsafe+')
$cscArgs.Add('-optimize+')
$cscArgs.Add('-debug:pdbonly')
$cscArgs.Add('-define:DEBUG')
$cscArgs.Add('-nostdlib+')
$cscArgs.Add('-noconfig')
$cscArgs.Add("-out:`"$OutputDir\01vmix.exe`"")

$icon = Join-Path $SourceDir 'app.ico'
if (Test-Path -LiteralPath $icon) { $cscArgs.Add("-win32icon:`"$icon`"") }
$manifest = Join-Path $SourceDir 'app.manifest'
if (Test-Path -LiteralPath $manifest) { $cscArgs.Add("-win32manifest:`"$manifest`"") }

# suppress the noise that comes with decompiled code
$nowarn = @(
    'CS0168','CS0219','CS0414','CS0649','CS0169','CS1591','CS0108','CS0109',
    'CS0672','CS0809','CS8981','CS0162','CS8078','CS1717','CS1712','CS0067',
    'CS8632','CS0282','CS3001','CS3002','CS3003','CS3005','CS3006','CS3008',
    'CS3016','CS3021','CS3024','CS3027','CS0618','CS0612','CS0628','CS0809',
    'CS0652','CS0659','CS0660','CS0661','CS4014','CS0184','CS0183','CS0182',
    'CS0164','CS0161','CS0160','CS1998','CS4019','CS0109','CS1718','CS0136',
    'CS0078','CS1525','CS1718','CS8600','CS8601','CS8602','CS8603','CS8604',
    'CS8618','CS8619','CS8620','CS8625','CS8629','CS8631','CS8634','CS8765',
    'CS8767','CS8769','CS8714','CS0122','CS0069','CS0108','CS0114','CS0612'
) | Sort-Object -Unique
$cscArgs.Add('-nowarn:' + ($nowarn -join ','))

foreach ($r in $frameworkRefs) { $cscArgs.Add("-reference:`"$r`"") }
foreach ($r in $appRefs)       { $cscArgs.Add("-reference:`"$r`"") }
foreach ($r in $resourceArgs) { $cscArgs.Add([string]$r) }
foreach ($s in $sources) { $cscArgs.Add([string]"`"$s`"") }

# --- compile -----------------------------------------------------------------
Write-Host ""
Write-Host "Compiling $($sources.Count) source files..." -ForegroundColor Cyan

$out = & $CscPath $cscArgs.ToArray() 2>&1
$exit = $LASTEXITCODE

if ($out) {
    $errLines = @()
    $warnLines = @()
    foreach ($line in $out) {
        $s = "$line"
        if ($s -match ': error ') { $errLines += $s } else { $warnLines += $s }
    }
    if ($warnLines.Count -gt 0) {
        Write-Host ""
        Write-Host "--- $($warnLines.Count) warning(s) ---" -ForegroundColor DarkYellow
        $warnLines | Select-Object -First 40 | ForEach-Object { Write-Host $_ -ForegroundColor DarkYellow }
        if ($warnLines.Count -gt 40) { Write-Host "... $($warnLines.Count - 40) more" }
    }
    if ($errLines.Count -gt 0) {
        Write-Host ""
        Write-Host "--- $($errLines.Count) ERROR(S) ---" -ForegroundColor Red
        $errLines | Select-Object -First 80 | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        if ($errLines.Count -gt 80) { Write-Host "... $($errLines.Count - 80) more" }
    }
}

if ($exit -ne 0) {
    Write-Host ""
    Write-Host "BUILD FAILED (exit $exit)" -ForegroundColor Red
    exit $exit
}

# --- stage runtime dependencies ---------------------------------------------
Write-Host ""
Write-Host "Staging runtime dependencies..." -ForegroundColor Cyan
foreach ($name in @('protobuf-net.dll', 'AirControls.dll',
                    'Sanford.Multimedia.Midi.dll', 'Sanford.Multimedia.dll',
                    'Sanford.Multimedia.Timers.dll', 'Sanford.Collections.dll',
                    'Sanford.Threading.dll',
                    'Meterdata.dat', '01vmix.exe.config')) {
    $src = Join-Path $InstallDir $name
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination $OutputDir -Force
    }
}

$brandLogo = Join-Path $SourceDir '01vmix-wordmark.png'
if (-not (Test-Path -LiteralPath $brandLogo)) { throw "Required logo asset not found: $brandLogo" }
Copy-Item -LiteralPath $brandLogo -Destination (Join-Path $OutputDir '01vmix-wordmark.png') -Force
# rebuild the app config from the project copy so the settings section matches
$cfg = Join-Path $SourceDir 'app.config'
if (Test-Path -LiteralPath $cfg) {
    Copy-Item -LiteralPath $cfg -Destination (Join-Path $OutputDir '01vmix.exe.config') -Force
}

Write-Host ""
Write-Host "BUILD OK -> $OutputDir\01vmix.exe" -ForegroundColor Green
Get-ChildItem $OutputDir | Select-Object Name, Length | Format-Table -AutoSize | Out-String -Width 120 | Write-Host
