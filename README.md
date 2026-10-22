# 01vmix — free & open-source build of AirFader

A clean-room rebuild of **AirFader 3.2.5833.4432** (Molten Logic LLC) with the
**entire licensing subsystem removed** and the product **renamed to 01vmix**.
The result is the full program, free to use, with no key, no trial, no
registration and no phone-home.

The rebuilt executable is functionally the same application: same UI, same MIDI
engine, same `faderspeak` server for the Android/iOS apps, same Yamaha DME
console support. Only the commercial restrictions are gone and the product is
now called **01vmix**.

---

## What was changed

| Area | Original behaviour | This build |
|---|---|---|
| License key | Required to connect (`frmSurface16`, `MixEngine.checkSerialV2`) | **Not required** — connection is unconditional |
| Trial | 14-day trial key requested from `airfader.com` | **Removed** — the trial dialog is never shown |
| Client limit | 1 mobile client (standard) / 99 (server key) | **99 clients** (`MixEngine.UNLOCKED_CLIENT_LIMIT`) |
| Registration dialog | Online validation against the vendor server | **Removed** — the *Registration* button is hidden |
| Update check | `ver.php?key=<license key>` | Disabled — no vendor server is contacted |
| Vendor backdoor | `Adam` / `ANY` accepted as a master key | **Removed** |
| Telemetry | Connection metrics POSTed to `recvmetrics.php` | **Removed** |
| Error-log upload | `Error.Log` POSTed to `recvlog.php` | **Removed** — logs stay local |
| Product name | AirFader | **01vmix** (binary, windows, folders, shortcut, registry) |
| Install folder | `C:\Program Files (x86)\air\AirFader` | `C:\Program Files (x86)\01vmix` |
| Interface language | English only | **English / Português (Brasil)** — switchable at runtime in *Configurações* |

The licensing switch lives in one place — `MixEngine.LICENSING_ENABLED` in
`re/src/AirFader/AirFader/MixEngine.cs`. Set it back to `true` to restore the
original commercial behaviour.

Everything else is a faithful rebuild of the shipped binary: the UI was
recreated from the embedded resources, and the code was recovered from the
original .NET assembly (see `AirFader_RE.md` and `AirFader_UI_REPORT.md`).

> The **internal namespace is still `AirFader`** on purpose. The user settings
> section is declared as `AirFader.Properties.Settings` in `01vmix.exe.config`
> and the 14 embedded resource streams are named `AirFader.*`; renaming the
> namespace would invalidate both. Only the user-visible product name changed.

---

## Idioma (Language)

The interface is bilingual: **English** and **Português (Brasil)**.

Português (Brasil) is the default; the language is selected in
**Configurações → Configurações → Idioma / Language** and is stored in
`Settings.Default.Language` (`en` / `pt-BR`), so the choice survives a restart
and is applied to every window immediately — including the ones that are
already open.

The translation layer lives in a single file,
`re/src/AirFader/AirFader/Loc.cs`:

* `ptBr` — the lookup table with every translated string;
* `Loc.Apply(form)` — walks a window, translates it and **hooks it**, so the
  texts that are created while the program runs (faders, effect knobs, the
  buttons of the *Home* panel, the EFX parameter labels, …) are translated as
  soon as they appear;
* `Loc.SetLanguage(lang)` — re-translates every control that is already on
  screen;
* `Loc.T(text)` / `Loc.Msg(...)` — translate a single string or a message box.

To add a translation, add a pair to the `ptBr` dictionary:

```csharp
{ "Clear", "Limpar" },
```

The key is the **English** text exactly as it is assigned to the control
(`\n` for the line breaks used by the multi-line buttons). Strings that are
kept in English on purpose — the brand name, the MIDI/fader abbreviations
(`UDF`, `DCA`, `2TR`, `+48v`, …), the keyboard keys, the frequency labels of
the graphic EQ and the values typed by the user — are simply not in the table.

---

## Requirements

* Windows 7 or later, **x86** (32-bit).
* **.NET Framework 2.0** — the original binary targets `v2.0.50727` and the
  rebuild keeps that exact target, so it runs on any Windows that still ships
  the 2.0 runtime.
* The **Yamaha DME Network Driver** (or a compatible virtual MIDI driver) for
  console communication.
* The AirFader mobile apps talk to the built-in server over the `faderspeak`
  protocol on the configured port.

---

## Layout

```
re/
  src/AirFader/        decompiled + patched C# sources (68 files, net20)
  resources/           the 14 original .resources streams (icons, bitmaps)
  tools/               build/diagnostic helpers (x86)
  build.ps1            build script
  deploy.ps1           install / uninstall script (elevated)
  migrate-settings.ps1 moves the existing user settings to the 01vmix profile
build/out/             build output (01vmix.exe + runtime DLLs) — not tracked
AirFader_RE.md         reverse-engineering report
AirFader_UI_REPORT.md  full UI / protocol reference
```

### Not published here

The following material stays local and is excluded by `.gitignore`:

| Path | Why |
|---|---|
| `build/out/` | build output — rebuild it with `re/build.ps1` |
| `re/tools/*.exe` | compiled helpers — rebuild them with `re/tools/build-tools.ps1` |
| `re/original/AirFader.exe.orig` | the original commercial binary — © Molten Logic LLC, not redistributable |
| `airfader.apk` | the original Android app — © Molten Logic LLC, not redistributable |
| `re/original/legacy-settings/` | local backup of this machine's user settings — contains a third-party license key |
| `re/analysis/airfader_keygen.py` | key generator for the commercial product — the algorithm analysis itself is documented in `AirFader_RE.md` §6 |

Everything needed to build and run 01vmix from this repository is included.

---

## Building

Requires only the .NET Framework reference assemblies and Roslyn's `csc`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File re\build.ps1
```

The script compiles against the **.NET Framework 2.0** reference assemblies
(`Microsoft.NETFramework.ReferenceAssemblies.net20`), embeds the 14 original
`.resources` streams with `/resource:` (Roslyn's `/linkresource:` does *not*
embed them, and the runtime then looks for sidecar files that do not exist),
and stages the third-party DLLs plus `Meterdata.dat` into `build/out`.

Result: `build/out/01vmix.exe`, `ImageRuntime v2.0.50727`,
`mscorlib/System/... Version=2.0.0.0` — byte-for-byte compatible with the
runtime expectations of the original.

> Note: the compiler arguments are passed on the **direct command line**.
> When the same arguments come from a `.rsp` file, `csc` mis-resolves the 2.0
> reference assemblies and also imports the .NET 4 copies from the GAC
> (`CS1703: multiple assemblies with equivalent identity`).

### Why .NET Framework 2.0 and not 4.8

The shipped `AirFader.exe` is a .NET Framework 2.0 assembly
(`ImageRuntime v2.0.50727`, `mscorlib Version=2.0.0.0`) and its
`AirFader.exe.config` pins `<supportedRuntime version="v2.0.50727"/>`.
Rebuilding against 4.8 produces an executable that **crashes before `Main`**
with `0xE0434F4D`, because the CLR 2.0 host cannot load the 4.0 `mscorlib`.
Matching the original target fixes it.

---

## Installing

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File re\deploy.ps1
```

The script asks for administrator rights (it writes to `Program Files (x86)`),
then:

* installs `01vmix.exe`, `01vmix.exe.config`, the third-party DLLs and
  `Meterdata.dat` into `C:\Program Files (x86)\01vmix`;
* creates the Start Menu shortcut
  `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\01vmix\01vmix.lnk`;
* rewrites the *Add/Remove Programs* entry (the vendor's Inno Setup
  uninstaller hardcoded the old path and could not survive the move);
* moves the data folder `%APPDATA%\AirFader` to `%APPDATA%\01vmix`;
* removes the old installation folder.

A log of every run is written to `%TEMP%\01vmix-deploy.log`.

### Moving the existing user settings

Windows stores the settings of a .NET application in

```
%LOCALAPPDATA%\<company>\<app>_Url_<hash>\<version>\user.config
```

The original binary shipped without a company name, so the folder was named
after the entry point namespace (`AirFader`). 01vmix declares
`AssemblyCompany("01vmix")`, so the settings now live in
`%LOCALAPPDATA%\01vmix\...`. After starting 01vmix once (so it creates its
profile), copy the old values over:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File re\migrate-settings.ps1 -Cleanup
```

The script copies the settings verbatim (the section name did not change) and
drops the `LicenseKey` / `LicenseUser` entries, because 01vmix does not use
them. `-Cleanup` also deletes the leftover `%LOCALAPPDATA%\AirFader` and
`%APPDATA%\AirFader` folders (a backup is kept locally in
`re/original/legacy-settings`, which is not published).

---

## Running

```
build\out\01vmix.exe
```

| Location | Content |
|---|---|
| `%APPDATA%\01vmix\` | `Conn.Log`, `Error.Log`, `Users.dat`, `Critical.Log` |
| `%LOCALAPPDATA%\01vmix\` | `user.config` (all user settings) |

Unhandled exceptions are appended to `%APPDATA%\01vmix\Critical.Log`.

---

## Verification

Two x86 test harnesses in `re/tools/` check the unlock end to end:

```
re\tools\TestLicense.exe build\out\01vmix.exe
    LICENSING_ENABLED    = False
    UNLOCKED_CLIENT_LIMIT= 99
    licenseClients       = 99
    checkSerialV2(bogus) = True
    getLicenseKey()      = Not required
    getLicenseUser()     = Open Source
    RESULT: UNLOCKED-OK

re\tools\TestAbout.exe build\out\01vmix.exe
    lblOwner            = Open Source
    lblKey              = Not required
    btnRegister.Visible = False
    RESULT: ABOUT-OK
```

The localization is verified by dumping the text of every control twice — once
in English and once in Portuguese — and comparing the two passes:

```powershell
# 32-bit PowerShell: the assembly is an x86 .NET Framework 2.0 binary
C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe -NoProfile -File re\tools\dump-ui.ps1
python re\tools\cmp-loc.py
```

`cmp-loc.py` prints, per window, how many texts were translated and which ones
are still in English. The remaining ones are the brand name, the technical
abbreviations (`UDF`, `DCA`, `2TR`, `+48v`, `Cue`, …), the keyboard keys, the
frequency labels of the graphic EQ and the values the user types — they are
meant to stay in English.

---

## Attribution

AirFader is the work of **Molten Logic LLC** (adam@airfader.com). This
repository contains a re-implementation of the published 3.2.5833.4432 binary,
released under the MIT license above. The original product, its trademarks and
its name remain the property of their respective owners; this project is not
affiliated with or endorsed by Molten Logic LLC.
