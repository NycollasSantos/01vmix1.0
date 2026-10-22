#requires -Version 5.1
<#
    build-tools.ps1 - compiles the diagnostic harnesses in re\tools against the
    .NET Framework 2.0 reference assemblies (x86), exactly like build.ps1 does for
    the application itself.

    Usage:
        powershell -NoProfile -ExecutionPolicy Bypass -File re\tools\build-tools.ps1
#>
[CmdletBinding()]
param(
    [string[]]$Names = @('TestLoc')
)

$ErrorActionPreference = 'Stop'

$ToolRoot  = 'C:\Users\DELL\AppData\Local\Temp\opencode'
$CscPath   = Join-Path $ToolRoot 'roslyn2\pkg\tasks\net472\csc.exe'
$RefAsmDir = Join-Path $ToolRoot 'refasm20\x\build\.NETFramework\v2.0'
$ToolsDir  = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path -LiteralPath $CscPath))  { throw "csc not found: $CscPath" }
if (-not (Test-Path -LiteralPath $RefAsmDir)) { throw "ref assemblies not found: $RefAsmDir" }

$refs = @()
foreach ($n in @('mscorlib.dll', 'System.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll')) {
    $refs += (Join-Path $RefAsmDir $n)
}

foreach ($name in $Names) {
    $src = Join-Path $ToolsDir ($name + '.cs')
    if (-not (Test-Path -LiteralPath $src)) { throw "source not found: $src" }
    $out = Join-Path $ToolsDir ($name + '.exe')
    $args = @('-target:exe', '-platform:x86', '-langversion:12.0', '-optimize+', '-nostdlib+', '-noconfig',
              '-nowarn:CS0168,CS0219,CS0162,CS0649,CS1591', "-out:`"$out`"")
    foreach ($r in $refs) { $args += "-reference:`"$r`"" }
    $args += "`"$src`""
    & $CscPath $args
    if ($LASTEXITCODE -ne 0) { throw "compile failed: $name" }
    Write-Host "built $out" -ForegroundColor Green
}
