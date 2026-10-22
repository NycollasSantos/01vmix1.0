#requires -Version 2.0
<#
    dump-ui.ps1 - walks the 01vmix UI and dumps every control text.

    Used to verify the pt-BR localization: the same dump is produced for
    English and for Portuguese (Brazil); the texts that never change are the
    ones still missing from Loc.ptBr.

    Must be run with the 32-bit PowerShell (the application is an x86
    .NET Framework 2.0 assembly):

        C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe -NoProfile -File dump-ui.ps1 <exe> <out>

    The forms that start a UI timer (frmSurface16) are created but not shown -
    showing them leaves a 50 ms timer running that blocks on Invoke() when the
    form is disposed.  For those forms Loc.Apply() is called explicitly, which
    is exactly what the Load handler does.
#>
param(
    [string]$Exe = 'C:\Users\DELL\Desktop\cloneair\build\out\01vmix.exe',
    [string]$Out = (Join-Path $env:TEMP 'ui-dump.txt'),
    [string]$Forms = 'frmSurface16,frmConfig,frmScene,frmEFX,frmGraphicEQ,frmKeyboard,frmTrial,frmUpdate,frmDebug,frmFancyBox,frmConfirm,frmChannel',
    [string[]]$NoShow = @('frmSurface16','frmConfig')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# AirControls.dll declares "fineMode" and "FineMode" - PowerShell's member
# adapter refuses such types (CLS compliance), so every control member is
# fetched through the base Control type by reflection.
$controlType = [System.Windows.Forms.Control]
$pText    = $controlType.GetProperty('Text')
$pName    = $controlType.GetProperty('Name')
$pControls = $controlType.GetProperty('Controls')

$asm = [System.Reflection.Assembly]::LoadFrom($Exe)
$locType = $asm.GetType('AirFader.Loc', $true)
$langType = $asm.GetType('AirFader.UiLanguage', $true)
$flags = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
$setLang = $locType.GetMethod('SetLanguage', $flags)
$applyLoc = $locType.GetMethod('Apply', $flags, $null, @([System.Windows.Forms.Control]), $null)

$lines = New-Object System.Collections.Generic.List[string]

function Save-Lines {
    [System.IO.File]::WriteAllLines($Out, $script:lines, [System.Text.Encoding]::UTF8)
}

function Dump-Control($c, $prefix) {
    $text = ''
    $name = ''
    try { $text = [string]$pText.GetValue($c, $null) } catch { }
    try { $name = [string]$pName.GetValue($c, $null) } catch { }
    if (-not [string]::IsNullOrEmpty($text)) {
        $flat = $text -replace "`r`n", ' | ' -replace "`n", ' | '
        $script:lines.Add(("{0}.{1}  =  {2}" -f $prefix, $name, $flat))
    }
    $kids = $null
    try { $kids = $pControls.GetValue($c, $null) } catch { }
    if ($kids) {
        foreach ($child in $kids) { Dump-Control $child ("$prefix.$name") }
    }
}

function Dump-Forms($languageName) {
    $lang = [System.Enum]::Parse($langType, $languageName)
    $setLang.Invoke($null, @($lang)) | Out-Null

    foreach ($n in ($Forms -split ',')) {
        $t = $asm.GetType("AirFader.$n", $false)
        if (-not $t) { continue }
        $form = $null
        $show = -not ($NoShow -contains $n)
        try {
            $ctor = $t.GetConstructor(@($asm.GetType('AirFader.frmSurface16')))
            if ($ctor) {
                $surface = [System.Activator]::CreateInstance($asm.GetType('AirFader.frmSurface16'))
                $form = $ctor.Invoke(@($surface))
            } else {
                $form = [System.Activator]::CreateInstance($t)
            }
            if ($show) {
                $form.Show()
                [System.Windows.Forms.Application]::DoEvents()
            }
        } catch {
            $script:lines.Add("[$n] cannot create: $($_.Exception.GetBaseException().Message)")
        }
        if (-not $form) { continue }
        try {
            if (-not $show) { $applyLoc.Invoke($null, @($form)) | Out-Null }
            $script:lines.Add("--- $n (title: $($form.Text)) ---")
            Dump-Control $form $n
        } finally {
            try { if ($show) { $form.Close() }; $form.Dispose() } catch { }
        }
        Save-Lines
    }
}

$lines.Add('### ENGLISH ###')
Save-Lines
Dump-Forms 'English'
$lines.Add('')
$lines.Add('### PT-BR ###')
Save-Lines
Dump-Forms 'PortugueseBrazil'
Save-Lines

"wrote $Out ($($lines.Count) lines)"
