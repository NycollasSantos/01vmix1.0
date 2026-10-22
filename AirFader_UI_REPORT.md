# AirFader 3.2.5833.4432 — UI Layer Reverse-Engineering Report

**Product:** AirFader (Molten Logic LLC) — Yamaha console control surface
**Assembly:** `AirFader.exe` v3.2.5833.4432, .NET Framework 2.0 (CLR `v2.0.50727`), WinForms, x86
**Source of truth:** ILSpy-decompiled C# under `re\src\AirFader\AirFader\` + decompiled `re\src\AirControls\`
**Report scope:** the *user interface layer* only — windows, controls, layout, theming, state, and the code paths that drive them.

> Every claim below is cited as `file.cs:line`. Code blocks marked **VERBATIM** are copied unmodified from the decompilation.

---

## 0. Method, corpus and file inventory

The decompiled corpus was produced with ILSpy command line (`-p` project export). The only warning emitted was a benign version-skew notice ("Latest version is '11.1.0.9782' (yours is '9.0.0.7889')").

### 0.1 Application source files

| File | Lines | Role |
|---|---:|---|
| `MixEngine.cs` | 17,532 | Engine, protocol, licensing math, channel offsets, logging, metrics |
| `frmChannel.cs` | 8,084 | Channel strip editor (EQ / Dyn / Sends) |
| `frmSurface16.cs` | 5,907 | Main window, layer & fader modes, page buttons |
| `frmConfig.cs` | 4,034 | Settings dialog (7+ tabs), themes, custom layer, update check |
| `FaderService.cs` | 1,995 | Optional TCP server for iOS/Android clients |
| `Channel.cs` | 1,620 | Channel model, `Channel.Pr` parameter enum, dB map |
| `frmBounce.cs` | 815 | "Bounce" Pong easter-egg game |
| `frmGraphicEQ.cs` | 924 | 31-band GEQ editor |
| `EFX.cs` | 535 | Effect rack model + 12 effect parameter tables |
| `frmEFX.cs` | 612 | Effect rack editor (32 knobs) |
| `frmScene.cs` | 476 | Scene browser / recall / save grid |
| `frmKeyboard.cs` | 411 | On-screen keyboard for name entry |
| `frmUpdate.cs` | 275 | Registration / update / unregister dialog |
| `frmTrial.cs` | 244 | Trial request dialog |
| `frmConfirm.cs` | 254 | Yes/No / OK / Fail modal (all UI messaging goes through it) |
| `frmDebug.cs` | 169 | Debug settings dialog |
| `OutPort.cs` | 252 | Output port model (Omni / Slot / 2TR) |
| `CTabControl.cs` | 143 | Owner-drawn tab control |
| `Console.cs` | 196 | Console model & per-model limits |
| `RGBHSL.cs` | 171 | RGB↔HSL colour maths |
| `Rack.cs` | 77 | GEQ/EFX rack model |
| `frmFancyBox.cs` | 70 | Legacy info box |
| `WindowStatic.cs` | 86 | Window position/size/state persistence |
| `GEQ.cs` | 83 | 31-band graphic EQ model |
| `Scene.cs` | 97 | Scene model |
| `Program.cs` | 43 | Entry point |
| `Group.cs`, `DCAGroup.cs`, `MuteGroup.cs`, `ChannelSend.cs`, `PEQ.cs`, `TabPanel.cs`, `NonFlickerBox.cs`, `MyWebClient.cs`, `Skin.cs` | — | Small models / helpers |
| `Consoles.cs`, `ConsoleVersions.cs`, `ConsoleTypes.cs`, `ChannelTypes.cs`, `ConnectionStatus.cs`, `ConnectionPhase.cs`, `OutportTypes.cs`, `FaderType.cs` | — | Enums |

### 0.2 The `.resx` files are almost empty

All three designer resource files contain only the form icon plus a handful of text values — the real control layout is **not** in the resx, it is emitted imperatively inside `InitializeComponent()` in the `.cs` files:

- `re\src\AirFader\AirFader.frmSurface16.resx`
- `re\src\AirFader\AirFader.frmChannel.resx`
- `re\src\AirFader\AirFader.frmConfig.resx`

This is a strong signal about the authors' style: the UI is largely **built in code at runtime**, not laid out in the WinForms designer. The same pattern shows up in `frmEFX` (entire UI constructed in `frmEFX_Load`, `frmEFX.cs:95-184`), `frmScene` (`frmScene.cs:44-126`), `frmKeyboard` (`frmKeyboard.cs:61-125`) and `frmGraphicEQ` (`frmGraphicEQ.cs:385-507`).

### 0.3 `AirControls.dll`

Decompiled to `re\src\AirControls\`. `ilspycmd -l i` returned **no interfaces** — the library is a set of concrete `Control` subclasses consumed by the app, not a plugin abstraction. Full inventory in §9.

---

## 1. Main window and the layer model — `frmSurface16`

### 1.1 Two independent "mode" axes

The main window models the console with two orthogonal enumerations:

```csharp
public enum LayerModes
{
    FOH,
    Master,
    Custom,
    OutportDelay,
    OutportAtt,
    DCA
}
```
`frmSurface16.cs:21-29`

```csharp
public enum FaderModes
{
    FOH,
    SOF,
    MOF,
    HA,
    Pan,
    DCA,
    OutportDelay,
    OutportAtt
}
```
`frmSurface16.cs:31-41`

- **Layer** = *which set of console channels* is on screen (front-of-house inputs, master buses, a user-defined mix, output attenuation/delay, or DCA groups).
- **Fader mode** = *what each vertical strip is bound to* (channel fader, sends-on-faders, matrix-on-faders, head amp, pan, DCA, outport delay, outport attenuation).

The two are switched independently: `switchLayerMode(LayerModes)` at `frmSurface16.cs:3648` and `switchFaderMode(FaderModes)` at `frmSurface16.cs:3260`.

### 1.2 Layer contents are `List<int>` of console channel indices

```csharp
public List<int> layerFOH = new List<int>
{
    0, 1, 2, ... 70, 71
};

public List<int> layerMaster = new List<int>
{
    72, 73, 74, 75, 76, 77, 78, 79, 80, 81,
    82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
    92, 93, 94, 95, 98, -1, 96, 97
};

public List<int> layerCustom = new List<int> { 0, 1, 64, 65, 72, 96, 97, 98 };
```
`frmSurface16.cs:280-299`

`layerCustom` is **not** hardcoded in practice — it is loaded from the `CustomLayer` user setting at startup:

```csharp
layerCustom.Clear();
string[] array2 = Settings.Default.CustomLayer.Split(',');
foreach (string s in array2)
{
    int result = 0;
    if (int.TryParse(s, out result))
    {
        layerCustom.Add(result);
    }
}
```
`frmSurface16.cs:547-556`

The shipped default is `"0,1,2,3,72,73,74,98"` (`app.config:61-63`).

### 1.3 Decoding the channel-ID space

The IDs above are not arbitrary. `MixEngine` defines the console channel-map offsets:

```csharp
chOffsetInput  = 0;
chOffsetSTIN   = 64;
chOffsetMix    = 72;
chOffsetMatrix = 88;
chOffsetStereo = 96;
chOffsetMonitor= 99;
```
`MixEngine.cs:833-838`

These are re-set per detected console model (`MixEngine.cs:949-953`, `970-974`, `991-995`, `1012-1016`, `1034-1038`, `1056-1060`, `1072-1076`).

`Channel.setChannelType()` maps an ID back to a human label (`Channel.cs:1561-1606`):

```csharp
case ChannelTypes.Input:
    subChannel = channel;
    _name = "Ch" + (subChannel + 1).ToString().PadLeft(2);
    break;
case ChannelTypes.STIN:
    subChannel = channel;
    _name = "StIn" + ((subChannel - MixEngine.chOffsetSTIN) / 2 + 1) + ((subChannel % 2 == 0) ? "L" : "R");
    break;
case ChannelTypes.Mix:
    subChannel = channel - MixEngine.chOffsetMix;
    _name = "Mx" + (subChannel + 1).ToString().PadLeft(2);
    break;
case ChannelTypes.Matrix:
    subChannel = channel - MixEngine.chOffsetMatrix;
    _name = "Mt" + (subChannel + 1).ToString().PadLeft(2);
    break;
case ChannelTypes.Stereo:
    subChannel = channel - MixEngine.chOffsetStereo;
    switch (subChannel)
    {
    case 0: _name = "St L"; break;
    case 1: _name = "St R"; break;
    case 2: _name = "Mono"; break;
    case 3: _name = "Monitor"; break;
    }
    break;
```
`Channel.cs:1566-1603`

**Therefore the previously-unresolved IDs resolve as:**

| ID | Meaning |
|---:|---|
| 0-63 | Input channels `Ch01`..`Ch64` (max 64 inputs) |
| 64-71 | ST IN pairs `StIn1L`..`StIn4R` (4 stereo pairs = 8 channels) |
| 72-87 | Mix buses `Mx01`..`Mx16` |
| 88-95 | Matrix sends `Mt01`..`Mt08` |
| 96 / 97 | `St L` / `St R` (stereo master pair) |
| 98 | `Mono` (mono sum of the stereo master) |
| 99 | `Monitor` |

This explains `layerMaster` exactly: MIX 1-16 (72-87), MATRIX 1-8 (88-95), `98` (Mono), `-1` (a deliberate **blank/placeholder** slot), `96`/`97` (St L / St R). And it explains the default `CustomLayer` `0,1,2,3,72,73,74,98` = "inputs 1-4, mix buses 1-3, mono".

### 1.4 Paging and the `Tag` offset mechanism

The main window shows `Settings.Default.faderCount` channels at a time (default 8, range 4-32 — `app.config:25-27`, `frmConfig.cs:2664`). Each "page" of a layer is selected by a page button whose `Tag` holds `pageIndex * faderCount`:

```csharp
panelLayer.Controls[num].Text = num * faderCount + 1 + " - " + (((num + 1) * faderCount > num2) ? num2.ToString() : ((num + 1) * faderCount).ToString());
...
panelLayer.Controls[num].Tag = num * faderCount;
```
`frmSurface16.cs:3685-3688` (FOH paging); equivalent code for master `3725-3746`, custom `3773-3778`, outport `3444-3465`.

Five integer offsets select the active page within each layer:

```csharp
private int offsetFOH;      // frmSurface16.cs:198
private int offsetCustom;   // :200
private int offsetMaster;   // :202
private int offsetOutport;  // :204
private int offsetDCA;      // :206
```

They are assigned when a page button is clicked (`:2146-2162`) and consumed by `updateUI()` to highlight the active page:

```csharp
if ((offsetCustom == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.Custom) ||
    (offsetDCA == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.DCA) ||
    (offsetMaster == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.Master) ||
    (offsetFOH == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.FOH) ||
    (offsetOutport == (int)panelLayer.Controls[i].Tag && (layerMode == LayerModes.OutportAtt || layerMode == LayerModes.OutportDelay)))
```
`frmSurface16.cs:2410`

The channel shown at strip position `l` for a given layer is resolved by index into the corresponding `List<int>`:

```csharp
num = offsetFOH + l;
num2 = ((num < layerFOH.Count) ? layerFOH[num] : (-1));
...
num = offsetMaster + l;
num2 = ((num < layerMaster.Count) ? layerMaster[num] : (-1));
...
num = offsetCustom + l;
num2 = ((num < layerCustom.Count) ? layerCustom[num] : (-1));
```
`frmSurface16.cs:2609-2622`

`-1` is the "no channel" sentinel, consistent with the blank slot in `layerMaster`.

### 1.5 Page / mode buttons

All page buttons are `AirControls.AirButton` instances created in the constructor and added to `panelConsole`:

| Field | Line | Label | `Tag` |
|---|---|---|---|
| `btnPageHome` | `frmSurface16.cs:228` | "Home" | `"Home"` |
| `btnPageSOF` | `:230` | "Sends" (`BackText` = "On Faders") | `"SOF"` |
| `btnPageMOF` | `:232` | "Matrix" | `"MOF"` |
| `btnPageMaster` | `:234` | — | — |
| `btnPageCustom` | `:236` | — | — |
| `btnPageDCA` | `:238` | — | — |
| `btnPageUDF` | `:240` | — | — |
| `btnMeter` | `:242` | — | — |
| `btnGEQ` | `:244` | — | — |
| `btnPageHA` | `:246` | — | — |
| `btnPagePan` | `:248` | — | — |
| `btnPageOutPortAtt` | `:250` | — | — |
| `btnPageOutPortDelay` | `:252` | — | — |

Click handlers: `btnPageHome_Click` `:4799`, `btnPageMaster_Click` `:4828`, `btnPageCustom_Click` `:4850`, `btnPageSOF_Click` `:4902`, `btnPageMOF_Click` `:4924`, `btnPageDCA_Click` `:1208`.

### 1.6 Startup sequence (`frmSurface16()`)

`frmSurface16.cs:473-522` — four distinct concerns, in order:

1. **Stale-instance cleanup.** Kills any other `airfader.exe` after asking the user:
   ```csharp
   Process currentProcess = Process.GetCurrentProcess();
   Process[] processes = Process.GetProcesses();
   foreach (Process process in processes)
   {
       if (process.ProcessName.ToLower().Contains("airfader.exe") && process.Id != currentProcess.Id && frmConfirm.askQuestion("It looks like AirFader is already running,\nor didn't exit cleanly last time.\nShould I fix it?", "Terminate old program?", this) == DialogResult.Yes)
       {
           process.Kill();
       }
   }
   ```
   `frmSurface16.cs:477-485`
2. **Upgrade migration + trial-expiry reset.**
   ```csharp
   if (Settings.Default.Upgraded)
   {
       Settings.Default.Upgrade();
       Settings.Default.Upgraded = false;
       if (Settings.Default.Version != Application.ProductVersion)
       {
           Settings.Default.Expired = false;
           Settings.Default.Version = Application.ProductVersion;
       }
       Settings.Default.Save();
   }
   ```
   `frmSurface16.cs:492-502` — note that **upgrading to a new build clears the "expired" flag**, which is how a new trial window is granted per version.
3. **Debug log deletion** (`isDebug` builds only) — deletes `%APPDATA%\AirFader\Error.Log` at startup: `frmSurface16.cs:503-517`.
4. **Model + layout load** — `MixEngine.setConsoleType(Settings.Default.ConsoleType)` (`:523`), `MixEngine.hMainWindow = this` (`:524`), then the `ChannelStrip` ordering is parsed from `Settings.Default.ChannelStrip.Split(',')` into `arrLayout` (`:537-542`), then `CustomLayer` is parsed (`:547-556`).

### 1.7 Per-strip control arrays

The channel strip is composed from parallel arrays rather than a per-channel control tree:

| Array | Line | Element type |
|---|---|---|
| `arrFaders` | `frmSurface16.cs:322` | `AirControls.AirFader` |
| `arrLabels` | `:324` | `AirButton` (channel number / name label) |
| `arrNames` | `:326` | `TextBox` (inline renaming) |
| `arrCues` | `:328` | `AirButton` |
| `arrMutes` | `:330` | `AirButton` |
| `arrPhantom` | `:332` | `AirButton` |
| `arrPans` | `:334` | `AirKnob` |
| `arrHAs` | `:336` | `AirKnob` |
| `arrHilights` | `:338` | `AirPanel` |
| `arrInfo` | `:340` | `Label` |
| `arrBtnUDF` | `:344` | `AirButton[16]` — user-defined function buttons |
| `arrBtnMM` | `:346` | `AirButton[8]` — mute/matrix buttons |

### 1.8 Strip element order is user-configurable

`arrLayout` holds the vertical order of strip elements, seeded from the `ChannelStrip` setting (default `"Label,Cue,Name,Mute,Fader"` — `app.config:100-102`). Each element has a nominal height weight:

```csharp
private Dictionary<string, decimal> arrLayoutHeight = new Dictionary<string, decimal>
{
    { "Label", 1.2m },
    { "Cue", 0.8m },
    { "Pan", 2.0m },
    { "Phantom", 0.8m },
    { "HA", 2.0m },
    { "Name", 1.0m },
    { "Fader", 10.0m },
    { "Mute", 1.0m },
    { "Info", 1.0m }
};
```
`frmSurface16.cs:305-316`

### 1.9 The connect button is the licence gate

```csharp
if (MixEngine.connStatus == ConnectionStatus.Disconnected)
{
    surfaceChanged = false;
    if (MixEngine.getLicenseKey().Length != 19)
    {
        frmTrial frmTrial2 = new frmTrial();
        frmTrial2.ShowDialog(this);
        return;
    }
    if (!MixEngine.checkSerialV2(MixEngine.getLicenseUser(), MixEngine.getLicenseKey()))
    {
        frmConfirm.sayFail("Invalid License Key. Please visit \"Config --> About\" to Register.", "Not Connected", this);
        return;
    }
    lblConnStatus.Text = "Connecting...";
    lblConnStatus.ForeColor = Color.Yellow;
    ...
}
```
`frmSurface16.cs:1791-1804`

Note the cheap length check (`!= 19`) — a key is `AF` + 2 + 4 + 4 + 4 base32 chars plus 3 hyphens = 22 characters, and `getLicenseKey()` returns the trimmed stored value; the real validation is `checkSerialV2`.

### 1.10 Meter refresh

`refreshMeters()` at `frmSurface16.cs:2275` drives the animated meter columns (`meterValue`, `meterInc`, `meterTicks` at `:370-372`), with `bAllowPaint` / `WM_PAINT` / `WM_SETREDRAW` constants at `:57-59` and `:364` used to suppress flicker during redraw.

---

## 2. Channel strip editor — `frmChannel`

### 2.1 Structure

`frmChannel` is a full-screen-capable editor for a single console channel, hosted inside a `CTabControl` with three `TabPage`s:

```csharp
private CTabControl tabSection;   // frmChannel.cs:246
private TabPage tabSends;         // :248
private TabPage tabEQ;            // :250
private TabPage tabDyn;           // :252
```

The tabs are added/removed dynamically depending on the channel type — e.g. `tabSends` is removed for bus channels (`:901-903`) and re-added when the user switches back to a send view (`:951-953`).

### 2.2 Control inventory

**Header / transport**
`lblHeader` `:372`, `btnPage0` `:240`, `btnPage1` `:242`, `btnClose` `:244`, `btnNext` `:406`, `btnPrev` `:408`, `btnCopy` `:462`, `btnFine` `:416`.

**Main fader & pan**
`chanFader` (`AirFader`) `:268`, `chanMute` (`AirButton`) `:456`, `chanPan` (`AirKnob`) `:370`, `chanLCRLevel` `:368`, `knobHA` `:450`, `chanPhantom` `:308`, `chanPhase` `:306`, `chanPanMode` `:310`.

**EQ tab**
`eq1Q/eq1F/eq1G` `:290/:282/:274`, `eq2Q/eq2F/eq2G` `:292/:284/:276`, `eq3Q/eq3F/eq3G` `:294/:286/:278`, `eq4Q/eq4F/eq4G` `:296/:288/:280`, `eqAtt` `:428`, `eqHPFFreq` `:366`, `eq4LPF` `:458`, plus `faderGain` `:270`, `faderFreq` `:272`, `faderQ` `:298`, `faderGain`/`faderFreq`/`faderQ` are `AirFader`s used as large horizontal read-outs, `btnEQOn` `:430`, `btnHPFOn` `:422`, `btnLowEQType` `:424`, `btnHiEQType` `:426`, `btnFlat` `:448`.

**Dyn tab**
`dyn00`..`dyn05` `:264`..`:254`, `dyn10`..`dyn15` `:384`..`:398`, `btnDyn0On` `:374`, `btnDyn1On` `:392`, `btnDyn0Type` `:304`, `btnDyn1Type` `:398`.

**Meters**
`airMeterPre` `:412`, `airMeterPost` `:414`, `meterDyn0In` `:432`, `meterDyn0Gain` `:434`, `meterDyn1In` `:436`, `meterDyn1Gain` `:438`.

**Sends tab**
`btnSendOn00..07` `:362`..`:336`, `btnSendPre00..07` `:364`..`:338`, plus `btnSendOn02` `:454`.

**Misc**
`btnDirectOut` `:312`, `btnInsertOn` `:314`, `btnToMonoOn` `:300`, `btnToStereoOn` `:302`, `btnSafe` `:334`, `btnMuteAssign0..7` `:328`..`:316`, `lblMuteGroups` `:332`, `lblChan00..07` `:222`..`:208`, `lblEq1..lblEq4` `:440`..`:442`, `lblParam` `:452`, `lblQ` `:410`, `lblFreq` `:460`.

### 2.3 The EQ band-type model

`setEQType(band, type)` (`frmChannel.cs:1581-1666`) is the pivot of the PEQ UI. For the **low** band (band 1) three types are supported, and the *shape of the UI itself changes*:

```csharp
case 1:
    switch (type)
    {
    case 0:
        btnLowEQType.Text = "Peak";
        eq1Q.Show();
        eq1G.KnobLabel = "Low";
        eq1G.KnobStyle = 0;
        eq1G.Maximum = 180;
        eq1G.Minimum = -180;
        eq1G.Steps = 5;
        eq1G.Tag = "Low";
        eq1G.ValueType = "div10";
        eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1G);
        break;
    case 1:
        btnLowEQType.Text = "Shelf";
        eq1Q.Hide();
        ...
        break;
    case 2:
        btnLowEQType.Text = "HPF";
        eq1Q.Hide();
        eq1G.KnobLabel = "HPF";
        eq1G.Maximum = 1;
        eq1G.Minimum = 0;
        eq1G.Steps = 1;
        eq1G.Tag = "eq1HPF";
        eq1G.ValueType = "onoff";
        eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1HPFOn);
        break;
    }
    break;
```
`frmChannel.cs:1585-1625`

For the **high** band (band 4) the picker offers `Peak / Shelf / LPF` (`btnHiEQType_Click` at `frmChannel.cs:3568`, picker array at `:3590`), and the type toggles which knobs are visible:

```csharp
case 0:
    btnHiEQType.Text = "Peak";
    if (activeGain == eq4LPF) { activeGain = eq4G; }
    eq4Q.Show();
    eq4G.Show();
    eq4LPF.Hide();
    break;
case 1:
    btnHiEQType.Text = "Shelf";
    ...
    eq4Q.Hide(); eq4G.Show(); eq4LPF.Hide();
    break;
case 2:
    btnHiEQType.Text = "LPF";
    if (activeGain == eq4G) { activeGain = eq4LPF; }
    eq4Q.Hide(); eq4G.Hide(); eq4LPF.Show();
    break;
```
`frmChannel.cs:1626-1660`

Note `ValueType` strings (`"div10"`, `"onoff"`, `"fader"`, `"geq"`) — these are the `AirControls` display-format selectors (§9).

### 2.4 Header text

`setHeader()` (`frmChannel.cs:1668`) prefixes the channel number and appends the group assignment:

```csharp
case ChannelTypes.Input:
case ChannelTypes.STIN:
    lblHeader.Text = "Ch " + (MixEngine.chData[activeChannel].subChannel + 1) + ((MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) != 0) ? (" [Grp " + MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) + "]") : "") + "\n" + MixEngine.chData[activeChannel].name;
    break;
```
`frmChannel.cs:1672-1675` (Mix and Matrix variants at `:1676-1681`).

### 2.5 Meters are driven from `Channel.Pr` meter points

The meter taps are enumerated at the end of the `Channel.Pr` enum (`Channel.cs:84-91`): `MeterPre, MeterDyn0In, MeterDyn1In, MeterDyn0GR, MeterDyn1GR, MeterEQIn, MeterEQOut, MeterPost`.

The strip binds them in `frmChannel.cs:1081-1101`:
- `airMeterPre` ← `Channel.Pr.MeterPre`
- `airMeterPost` ← `Channel.Pr.MeterPost`
- `meterDyn0In` ← `MeterDyn0In`, `meterDyn1In` ← `MeterDyn1In`
- `meterDyn0Gain` / `meterDyn1Gain` ← `Math.Abs(MeterDyn0GR - 126)` / `Math.abs(MeterDyn1GR - 126)` — i.e. gain-reduction meters are shown as *distance from unity*, inverted.

Gain-reduction tick marks are placed at `frmChannel.cs:1378` and `:1398`.

### 2.6 The "Window Options" popup

`btnOptions_Click` (`frmChannel.cs:5232`) opens a floating options window offering fullscreen mode and a **ConsoleFollow** link (`:5362`). Its central trick is that it re-orders the *strip* (not the channel editor) elements, checking the relative position of "Mute" and "Fader" to decide layout:

```csharp
if ("Mute".IndexOf > "Fader".IndexOf) { ... }
```
`frmChannel.cs:4955`

### 2.7 EQ curve rendering

`picEQ_Plot()` (`frmChannel.cs:2812`) rasterises the PEQ response curve into a `PictureBox` (`picEQ` sized to `tabEQ` minus 20 px at `:4783-4784`), using the same `calcGainAt()` maths that later appears in the Bounce game (§6). `calcGainAt` is at `frmChannel.cs:2675`.

### 2.8 Sends paging

`setSOFPage(page)` (`frmChannel.cs:2535`) switches which send bank is visible; `showJumper()` (`:1938`) and `switchJumper` (`:2052`) implement the send-page jumper control.

---

## 3. Configuration and settings — `frmConfig`

### 3.1 Tabs

`frmConfig` is a large multi-tab dialog. `TabPage` fields:

| Tab | Field | Line |
|---|---|---|
| Devices | `tabDevices` | `frmConfig.cs:75` |
| Customize | `tabCustomize` | `:77` |
| About | `tabAbout` | `:91` |
| Custom Layer | `tabCustomLayer` | `:147` |
| Fader Strip | `tabFaderStrip` | `:171` |
| (unnamed) | `tabPage1` | `:189` |
| (unnamed) | `tabPage2` | `:209` |
| (unnamed) | `tabPage3` | `:237` |
| (unnamed) | `tabPage4` | `:255` |
| Users | `tabUsers` | `:331` |

### 3.2 Devices tab

| Control | Line | Notes |
|---|---|---|
| `lbInputDev` | `:65` | MIDI input list |
| `lbOutputDev` | `:67` | MIDI output list |
| `cbConsoleType` | `:145` | `{"Yamaha LS9-16","Yamaha LS9-32","Yamaha M7CL-32","Yamaha M7CL-48","Yamaha 01V96","Yamaha 01V96i"}` (`:2393`) |
| `cbNumFaders` | `:93` | designer list `{4,8,12,16,24,32}` (`:2664`), but repopulated at runtime as 1..32 (`:531-534`) |
| `cbMaxChannel` | `:115` | `{8,16,24,32,40,48,56,64}` (`:2645`) |
| `cbMasterSection` | `:165` | `{"None","Stereo","Mono","Mono+ST","Monitor","ST+Monitor","Monitor+ST"}` (`:2632`) |
| `cbUDKLamp` | `:83` | `{"None","1".."12"}` (`:2687`) |
| `btnScan` | `:169` | Scans for a running console window via `GetWindowText((IntPtr)792644, ...)` (`:1089-1094`) |
| `btnFixDME` | `:95` | DM (Digital MIDI) fix-up helper |
| `btnSweep` | `:109` | Console capability sweep |
| `chkGO` | `:111` | |

### 3.3 Customize tab (colours)

Colours are edited with plain `Button` controls whose `BackColor` *is* the swatch:

`btnActiveBack` `:281` / `btnActiveFore` `:277`, `btnInactiveBack` `:289` / `btnInactiveFore` `:291`, `btnChOnBack` `:283` / `btnChOnFore` `:285`, `btnChOffBack` `:295` / `btnChOffFore` `:297`, `btnLEDColorBack` `:301` / `btnLEDColorFore` `:303`, `btnBackground` `:307` / `btnForeground` `:311`, `btnFaderBack` `:317` / `btnFaderFore` `:319` / `btnFaderTail` `:323`, and `cbTheme` `:315`.

Clicking any of them goes through one handler:

```csharp
private void btnColor_Click(object sender, EventArgs e)
{
    try
    {
        ((Button)sender).BackColor = pickColor(((Button)sender).BackColor);
    }
    catch (Exception ex)
    {
        MixEngine.logError("btnColor_Click()", "UI", ex.ToString());
    }
}
```
`frmConfig.cs:1209-1219`

`pickColor` forces the theme into "Custom":

```csharp
private Color pickColor(Color currColor)
{
    try
    {
        ColorDialog colorDialog = new ColorDialog();
        colorDialog.AllowFullOpen = true;
        colorDialog.AnyColor = true;
        colorDialog.SolidColorOnly = false;
        colorDialog.Color = currColor;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            redoLayout = true;
            cbTheme.Text = "Custom";
            return colorDialog.Color;
        }
        return currColor;
    }
    catch (Exception ex)
    {
        MixEngine.logError("pickColor()", "UI", ex.ToString());
    }
    return Color.Black;
}
```
`frmConfig.cs:1185-1207`

### 3.4 Custom Layer tab

Two list boxes: `lbChannels` (source, 100 entries — a "Not Assigned" sentinel plus all 99 channels) and `lbCustomLayer` (destination).

```csharp
lbChannels.Items.Clear();
lbChannels.Items.Add(new lbChanItem("Not Assigned", -1));
for (int l = 0; l < 99; l++)
{
    string newName = "[" + MixEngine.chData[l].hardname + "] " + MixEngine.chData[l].name;
    lbChannels.Items.Add(new lbChanItem(newName, l));
}
lbCustomLayer.Items.Clear();
foreach (int item2 in parentForm.layerCustom)
{
    ...
}
```
`frmConfig.cs:608-618`

Editing handlers: `btnCLAdd_Click` `:1001`, `btnCLRemove_Click` `:1026`, `btnCLUp_Click` `:1041`, `btnCLDown_Click` `:1060`, with double-click shortcuts at `:1079` and `:1084`.

Add inserts *after* the current selection, or at the end:

```csharp
int num = lbCustomLayer.SelectedIndex;
if (num == -1)
{
    num = lbCustomLayer.Items.Count;
}
foreach (lbChanItem selectedItem in lbChannels.SelectedItems)
{
    lbCustomLayer.Items.Insert(num++, selectedItem);
}
```
`frmConfig.cs:1009-1017`

### 3.5 Fader Strip tab

`lbControls` (available elements) ↔ `lbStrip` (current order). Handlers `btnCSAdd_Click` `:1096`, `btnCSRemove_Click` `:1120`, `btnCSUp_Click`/`btnCSDown_Click`, double-click shortcuts.

### 3.6 About tab

`lblOwner` `:125`, `lblKey` `:135`, `lblVersion` `:129`, `lblProduct` `:131`, `btnRegister` `:133`, `btnUpdate` `:137`, `pictureBox1` `:123`.

The version label is standard vs. server depending on the licence:

```csharp
lblVersion.Text = Application.ProductVersion + " (Server)";
```
`frmConfig.cs:901` (the `(Standard)` branch is at `:895-899`, driven by `MixEngine.licenseClients == 1`).

`pictureBox1_DoubleClick` opens the debug dialog — a hidden back door:

```csharp
private void pictureBox1_DoubleClick(object sender, EventArgs e)
{
    try
    {
        frmDebug frmDebug2 = new frmDebug();
        frmDebug2.Show();
    }
    catch (Exception ex)
    {
        MixEngine.logError("picture_DoubleClick()", "UI", ex.ToString());
    }
}
```
`frmConfig.cs:988-999`

### 3.7 Users tab / server

`lbUsers` `:339`, `btnNewUser` `:335`, `btnUserSave` `:337`, `chkEnabled` `:343`, `chkChannel` `:353`, `chkMainMix` `:355`, `chkMix0..chkMix15` `:341`..`:379`.

Helpers: `initServerConfig()` `frmConfig.cs:1389` (enumerates local IPs via `Dns.GetHostAddresses(Dns.GetHostName())`), `initConnectionList()` `:1434`, `initUserList()` `:1487`, `btnNewUser_Click` `:1505`, `btnUserSave_Click` `:1571`, `lbUsers_SelectedIndexChanged` `:1662`. Port default `12301` (`app.config:136-138`).

### 3.8 Load / Save

`frmConfig_Load` at `frmConfig.cs:526-676` hydrates every control from `Settings.Default`; `btnSave_Click` at `:677-806` persists them. On load it also silently re-validates the licence in quiet mode:

```csharp
MixEngine.checkSerialV2(MixEngine.getLicenseUser(), MixEngine.getLicenseKey(), quiet: true);
if (MixEngine.licenseClients == 1)
{
    ...
}
```
`frmConfig.cs:655-657`

### 3.9 Complete settings inventory (`app.config`)

All 49 settings, with shipped defaults:

| Setting | Default | app.config line |
|---|---|---:|
| `c_channel_on` | `Coral` | 13 |
| `c_channel_off` | `Gray` | 16 |
| `c_button_active` | `Lime` | 19 |
| `c_button_inactive` | `WhiteSmoke` | 22 |
| `faderCount` | `8` | 25 |
| `flashUDF` | `11` | 28 |
| `c_background` | `50, 50, 50` | 31 |
| `layerPos` | `Right` | 34 |
| `EQActive` | `Lime` | 37 |
| `EQInactive` | `LightSteelBlue` | 40 |
| `maxChannels` | `64` | 43 |
| `MIDI_Input` | *(empty)* | 46 |
| `MIDI_Output` | *(empty)* | 49 |
| `LicenseKey` | *(empty)* | 52 |
| `LicenseUser` | *(empty)* | 55 |
| `ConsoleType` | `Yamaha LS9-32` | 58 |
| `CustomLayer` | `0,1,2,3,72,73,74,98` | 61 |
| `BusMeter` | `5` | 64 |
| `ChannelMeter` | `0` | 67 |
| `MasterSection` | `96` | 70 |
| `windowGEQ` | `50,50,1000,600,Normal,0` | 73 |
| `windowMain` | `50,50,1020,745,Normal,0` | 76 |
| `windowChannel` | `50,50,808,634,Normal,0` | 79 |
| `Upgraded` | `True` | 82 |
| `Expired` | `False` | 85 |
| `windowScene` | `50,50,700,450,Normal,0` | 88 |
| `MIDI_Channel` | `0` | 91 |
| `Version` | `1` | 94 |
| `windowEFX` | `500,100,800,600,Normal,0` | 97 |
| `ChannelStrip` | `Label,Cue,Name,Mute,Fader` | 100 |
| `c_channel_on_fore` | `Black` | 103 |
| `c_channel_off_fore` | `Black` | 106 |
| `c_button_active_fore` | `Black` | 109 |
| `c_button_inactive_fore` | `Black` | 112 |
| `c_led_fore` | `Lime` | 115 |
| `c_led_back` | `Black` | 118 |
| `c_foreground` | `WhiteSmoke` | 121 |
| `c_theme` | `Original` | 124 |
| `c_fader_back` | `50, 50, 50` | 127 |
| `c_fader_fore` | `WhiteSmoke` | 130 |
| `c_fader_tail` | `Coral` | 133 |
| `serverPort` | `12301` | 136 |
| `serverAutoStart` | `False` | 139 |
| `ConsoleFollow` | `False` | 142 |
| `autoConnect` | `False` | 145 |
| `autoMeters` | `True` | 148 |
| `confirmExit` | `True` | 151 |
| `debugLevel` | `1` | 154 |
| `GlobalKey` | *(empty)* — **applicationSettings**, machine-wide | 176 |
| `GlobalUser` | *(empty)* — **applicationSettings**, machine-wide | 179 |

Note the split: 47 settings live in `<userSettings>` (per-user), while `GlobalKey`/`GlobalUser` live in `<applicationSettings>` (`app.config:174-183`), which `updateAppConfig` writes with `ConfigurationUserLevel.None` — i.e. **machine-wide**, requiring Administrator (§5.5).

---

## 4. Theme / skin system

### 4.1 `Skin.cs` — the runtime skin holder

```csharp
public Color btnActiveFG = Color.Black;
public Color btnActiveBG = Color.LightGreen;
public Color btnInactiveFG = Color.Black;
public Color btnInactiveBG = Color.LightGray;
```
`Skin.cs` (22 lines total)

`frmSurface16` holds one instance: `private Skin theme = new Skin();` (`frmSurface16.cs:318`).

### 4.2 `RGBHSL.cs` — colour maths used by the GEQ/EFX editors

Provides `SetBrightness` / `ModifyBrightness`, `SetSaturation` / `ModifySaturation`, `SetHue` / `ModifyHue`, plus RGB↔HSL conversion (171 lines).

It is used to auto-derive a contrasting band colour from the fader background:

```csharp
Color color = ((!((double)Settings.Default.c_fader_back.GetBrightness() > 0.5)) ? RGBHSL.ModifyBrightness(Settings.Default.c_fader_back, 2.0) : RGBHSL.ModifyBrightness(Settings.Default.c_fader_back, 0.6));
```
`frmGraphicEQ.cs:389`

### 4.3 Theme presets

`cbTheme` is populated with eight entries:

```csharp
string[] array = new string[8] { "Custom", "Original", "Contrast", "Fire", "Ice", "Yamawho", "SemiSonus", "Mackish" };
```
`frmConfig.cs:646`

`syncColors(theme)` (`frmConfig.cs:1226-1374`) applies each preset to the 15 colour buttons. Palette summary:

| Theme | Active BG | Inactive BG | Ch On BG | Background | Fader Tail | Line |
|---|---|---|---|---|---|---:|
| Original | `Lime` | `WhiteSmoke` | `Coral` | `FromArgb(50,50,50)` | `Coral` | 1232 |
| Contrast | `Yellow` | `Blue` | `Blue` | `Black` | `Yellow` | 1249 |
| Fire | `#FFF60B` | `#FF6100` | `#FF6100` | `#4D0600` | `#FFF60B` | 1266 |
| Ice | `#B5E1FF` | `#6681FF` | `#9E66FF` | `#1A2C96` | `#6681FF` | 1283 |
| Yamawho | `Lime` | `WhiteSmoke` | `Coral` | `FromArgb(50,50,50)` | `DarkGray` | 1300 |
| SemiSonus | `#FAAF8F` | `#C2CDE2` | `#C2E2CD` | `#102051` | `#FAAF8F` | 1317 |
| Mackish | `#AF405F` | `#C4C4C9` | `#898D99` | `#515153` | `#AF405F` | 1334 |
| Custom | *from settings* | *from settings* | *from settings* | *from settings* | *from settings* | 1351 |

Hex parsing uses `ColorTranslator.FromHtml`:

```csharp
private Color HexColor(string h)
{
    return ColorTranslator.FromHtml(h);
}
```
`frmConfig.cs:1221-1224`

Changing the theme triggers a re-layout:

```csharp
private void cbTheme_SelectedIndexChanged(object sender, EventArgs e)
{
    try
    {
        syncColors(cbTheme.Text);
        redoLayout = true;
    }
    catch (Exception ex)
    {
        MixEngine.logError("cbTheme_changed()", "UI", ex.ToString());
    }
}
```
`frmConfig.cs:1376-1387`

### 4.4 `CTabControl.cs` — owner-drawn tabs

`CTabControl` is a `TabControl` with `DrawMode = OwnerDrawFixed`, its background taken from `Settings.Default.c_background`, and a `FontFit` routine that auto-shrinks the tab font until the text fits. The custom draw handler is in `frmChannel.cs:1919-1935`:

```csharp
TabPage tabPage = tabSection.TabPages[e.Index];
...
graphics.DrawString(tabPage.Text, font, brush, tabRect, new StringFormat(stringFormat));
```
`frmChannel.cs:1919`, `:1935`

### 4.5 `NonFlickerBox.cs` / `TabPanel.cs`

`NonFlickerBox` is a 12-line double-buffered `PictureBox`; `TabPanel` is an empty `UserControl` used as a placeholder container (29 lines).

### 4.6 Window position persistence — `WindowStatic.cs`

`WindowStatic` serialises geometry as a comma string `x,y,w,h,state,page`:

```csharp
thisWin.Parse(Settings.Default.windowGEQ);
thisWin.SetWindow(this);
```
`frmGraphicEQ.cs:197-198`

The `page` component doubles as the "last selected band" for the GEQ window (`setActiveBand(thisWin.Page)` at `frmGraphicEQ.cs:209`). Matching settings: `windowMain`, `windowChannel`, `windowGEQ`, `windowEFX`, `windowScene`.

---

## 5. Trial, licensing, registration and update logic

This is the most security-relevant part of the UI layer. All of it lives in `MixEngine` + three forms.

### 5.1 Key format

Keys are 4 dash-separated groups: `AF` + 2 base32 chars, 4 chars, 4 chars, 4 chars.

```csharp
private static string base32(ulong num, int digits)
{
    try
    {
        string text = "0123456789ABCDEFGHXJKYMNWPQRZTUV";
        uint num2 = 12u;
        ulong num3 = 0uL;
        char[] array = new char[num2 + 1];
        do
        {
            num3 = num % 32;
            array[num2] = text[(int)num3];
            num = (num - num3) / 32;
            num2--;
        }
        while (num != 0);
        char[] array2 = new char[1];
        char[] trimChars = array2;
        string text2 = new string('0', digits) + new string(array).Trim(trimChars);
        return text2.Substring(text2.Length - digits);
    }
    catch (Exception)
    {
        MessageBox.Show("Problem in Base10to32()", "Static Error");
    }
    return "";
}
```
`MixEngine.cs:1127-1153` (verbatim)

The custom alphabet deliberately omits `I`, `L`, `O`, `S` — `"0123456789ABCDEFGHXJKYMNWPQRZTUV"` — to avoid read-back ambiguity, which is why the validator still has to defensively re-map them (§5.2).

The generator:

```csharp
private static string genKeyV2(string name, int version, int server, int time, int rand, bool ignoreCase)
{
    try
    {
        name = Regex.Replace(name, "[^a-zA-Z0-9 ]", "");
        name = name.Trim();
        if (ignoreCase)
        {
            name = name.ToUpper();
        }
        int num = server * 16 + version;
        string s = "AF" + base32((ulong)num, 2) + name + base32((ulong)time, 4);
        ulong num2 = 0uL;
        ulong num3 = 0uL;
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        foreach (byte b in bytes)
        {
            num2 += (ulong)((long)(++num3) * (long)offset1[b]);
        }
        ulong num4 = (ulong)rand ^ num2;
        string text = "AF" + base32((ulong)num, 2) + "-" + base32(num4, 4) + "-" + base32((ulong)time, 4) + "-" + base32((ulong)rand, 4);
        return text.ToUpper();
    }
    catch (Exception ex)
    {
        logError("genKeyV2()", "Math", ex.ToString());
    }
    return "";
}
```
`MixEngine.cs:1180-1208` (verbatim)

Semantics of the four groups:

| Group | Meaning |
|---|---|
| `AF` + 2 chars | `server * 16 + version`; low nibble = `version`, bit 4 = `server` flag |
| group 2 | `rand XOR weightedChecksum(ownerName)` — binds the key to the owner's name |
| group 3 | the **trial start day offset** (days since 2011-01-01) |
| group 4 | the random nonce |

### 5.2 The trial / expiry check — **VERBATIM**

`MixEngine.checkSerialV2(name, key, quiet)` — `MixEngine.cs:1258-1354`:

```csharp
public static bool checkSerialV2(string name, string key, bool quiet)
{
    try
    {
        if (serialRetry >= 10)
        {
            return false;
        }
        name = name.Trim();
        key = key.Trim().ToUpper();
        key = key.Replace("O", "0");
        key = key.Replace("I", "1");
        key = key.Replace("L", "1");
        key = key.Replace("S", "5");
        string[] array = key.Split(new char[1] { '-' }, 4);
        if (array.Length != 4)
        {
            return false;
        }
        if (key.Substring(0, 4).ToUpper() == "AF01")
        {
            if (!quiet)
            {
                frmConfirm.sayFail("This key is for a previous version\nPlease visit http://www.airfader.com/upgrade for a free exchange!", "Previous Version", hMainWindow);
            }
            return false;
        }
        int rand = (int)base10(array[3]);
        int num = (int)base10(array[2]);
        int num2 = (int)base10(array[0].Substring(2, 2));
        int num3 = num2 & 0xF;
        int num4 = (num2 & 0x10) / 16;
        string text = genKeyV2(name, num3, num4, num, rand, ignoreCase: false);
        if (text != key)
        {
            text = genKeyV2(name, num3, num4, num, rand, ignoreCase: true);
        }
        if (text == key)
        {
            if (num3 < 2)
            {
                if (!quiet)
                {
                    frmConfirm.sayFail("Sorry, this key is for a previous version\nPlease visit http://www.airfader.com/upgrade for a free exchange!", "Previous Version", hMainWindow);
                }
                return false;
            }
            if (num4 == 1)
            {
                licenseClients = 99;
            }
            else
            {
                licenseClients = 1;
            }
            DateTime dateTime = new DateTime(2011, 1, 1);
            TimeSpan timeSpan = DateTime.Now - dateTime;
            if (num == 36500)
            {
                return true;
            }
            if (Settings.Default.Expired)
            {
                if (!quiet)
                {
                    frmConfirm.sayFail("Thanks for trying AirFader!\nYour trial period has ended.\nPlease visit http://www.airfader.com to purchase", "Trial Ended", hMainWindow);
                }
                serialRetry++;
                return false;
            }
            if (num < timeSpan.Days)
            {
                if (!quiet)
                {
                    frmConfirm.sayFail("Thanks for trying AirFader!\nThis trial license key has expired.\nVisit http://www.airfader.com", "Trial Expired", hMainWindow);
                }
                Settings.Default.Expired = true;
                Settings.Default.Save();
                serialRetry++;
                return false;
            }
            DateTime dateTime2 = dateTime.AddDays(num);
            if (!quiet)
            {
                frmConfirm.sayOK("This trial expires " + dateTime2.ToShortDateString() + ".\nTo purchase, visit http://www.airfader.com", "Trial Mode", hMainWindow);
            }
            return true;
        }
        serialRetry++;
        return false;
    }
    catch (Exception ex)
    {
        logError("CheckSerial()", "Math", ex.ToString());
    }
    return false;
}
```
`MixEngine.cs:1258-1354` (verbatim)

**How the expiry works:**

1. The key's group 3 decodes to `num` = a day-offset from the epoch `2011-01-01` — this is the trial *start* date.
2. `num == 36500` is the **perpetual-licence sentinel** (100 years) and returns `true` immediately.
3. `Settings.Default.Expired` is the sticky "this machine already burned its trial" flag. If set, the trial is refused.
4. `num < timeSpan.Days` means the key's start date is in the future (i.e. the trial window has elapsed relative to today's clock since epoch) → **expired**: the flag is written to disk and the check fails.
5. Otherwise the trial is valid and the expiry date (`2011-01-01 + num` days) is shown.

The `Expired` flag is reset on upgrade (§1.6) — `Settings.Default.Expired = false` at `frmSurface16.cs:498` — which is why the expiry check is per-version rather than absolute.

`serialRetry` (incremented at `:1325`, `:1336`, `:1346`) acts as a rate limiter: after 10 failed attempts the function short-circuits to `false` (`MixEngine.cs:1262-1265`).

The two-argument overload delegates to the quiet-capable one:

```csharp
public static bool checkSerialV2(string name, string key)
{
    return checkSerialV2(name, key, quiet: false);
}
```
`MixEngine.cs:1253-1256`

### 5.3 The update check — **VERBATIM**

`frmConfig.btnUpdate_Click` — `frmConfig.cs:949-986`:

```csharp
private void btnUpdate_Click(object sender, EventArgs e)
{
    string text = "";
    try
    {
        btnUpdate.Text = "Connecting...";
        btnUpdate.Enabled = false;
        string url = "http://www.airfader.com/data/ver.php?key=" + MixEngine.getLicenseKey() + "&ver=" + Application.ProductVersion;
        text = GetPage(url);
        if (!text.Contains("<AirFader>"))
        {
            frmConfirm.sayFail("Could not connect to update service. Visit AirFader.com", "Unexpected Response");
            btnUpdate.Text = "Check for Updates";
            btnUpdate.Enabled = true;
            return;
        }
        if (text.Contains("Current"))
        {
            frmConfirm.sayOK("You are running the latest version.", Application.ProductVersion);
            btnUpdate.Text = "Check for Updates";
            btnUpdate.Enabled = true;
            return;
        }
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(text);
        XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("newver");
        if (frmConfirm.askQuestion("New Version " + elementsByTagName[0].InnerText + " Available!\nDo you want to download it now?", "New Version") == DialogResult.Yes)
        {
            Process.Start("http://www.airfader.com/download");
        }
        btnUpdate.Text = "Check for Updates";
        btnUpdate.Enabled = true;
    }
    catch (Exception ex)
    {
        MixEngine.logError("btnUpdateClick()", "UI", ex.ToString());
    }
}
```
`frmConfig.cs:949-986` (verbatim)

The accompanying HTTP GET helper — **VERBATIM**:

```csharp
private string GetPage(string url)
{
    WebResponse webResponse = null;
    StreamReader streamReader = null;
    string text = "";
    try
    {
        WebRequest webRequest = WebRequest.Create(url);
        webResponse = webRequest.GetResponse();
        Stream responseStream = webResponse.GetResponseStream();
        streamReader = new StreamReader(responseStream);
        char[] array = new char[256];
        for (int num = streamReader.Read(array, 0, array.Length); num > 0; num = streamReader.Read(array, 0, array.Length))
        {
            text += new string(array, 0, num);
        }
    }
    catch (UriFormatException)
    {
        frmConfirm.sayFail("The request URI was malformed.", "Internet Error");
    }
    catch (WebException)
    {
        frmConfirm.sayFail("The request URI could not be found.", "Internet Error");
    }
    catch (IOException)
    {
        frmConfirm.sayFail("The request URI could not be retrieved.", "Internet Error");
    }
    finally
    {
        webResponse?.Close();
        streamReader?.Close();
    }
    return text;
}
```
`frmConfig.cs:912-947` (verbatim)

**Observations:**

- Plain `http://` — **no TLS**. The licence key is transmitted in cleartext in the query string (`?key=...`).
- `WebRequest.Create(url).GetResponse()` uses the .NET default `HttpWebRequest`, which on .NET 2.0/3.5 follows the machine's IE proxy/TLS settings; there is **no certificate pinning and no request timeout** — a hung server blocks the UI thread indefinitely.
- The response is parsed as **XML** with root `<AirFader>`, and the version is read from a `<newver>` element; the literal string `"Current"` anywhere in the body means "up to date".
- Downloading is delegated to the browser via `Process.Start("http://www.airfader.com/download")` — the app never downloads or updates itself.
- The `GetPage` variant in `frmTrial.cs:38-73` and `frmUpdate.cs:38-73` is identical except it returns the string `"SKIP"` on error instead of showing a dialog.

### 5.4 Trial request (`frmTrial`)

`frmTrial` is shown when the stored key is not 19 characters (`frmSurface16.cs:1794-1799`). It collects owner name + email, validates them, and POSTs nothing — it just does a GET:

```csharp
text = "https://www.airfader.com/licensing/key.php?action=AUTOTRIAL&name=" + HttpUtility.UrlEncode(txtOwnerName.Text) + "&email=" + HttpUtility.UrlEncode(txtEmail.Text) + "&v=" + Application.ProductVersion;
text2 = GetPage(text);
MixEngine.logDebug("AutoTrial result: " + text2);
string[] array = text2.Split(new string[1] { ":" }, StringSplitOptions.None);
if (array.Length == 3 && array[0].Equals("OK"))
{
    bool flag = false;
    WindowsIdentity current = WindowsIdentity.GetCurrent();
    WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
    if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
    {
        MixEngine.updateAppConfig("GlobalKey", array[1]);
        MixEngine.updateAppConfig("GlobalUser", txtOwnerName.Text);
    }
    else
    {
        if (Settings.Default.GlobalKey.Length > 0)
        {
            frmConfirm.sayOK("A Global License Key has already been set!\nTo over-write this key, run AirFader as an Administrator.", "Global Key");
        }
        Settings.Default.LicenseKey = array[1];
        Settings.Default.LicenseUser = txtOwnerName.Text;
        Settings.Default.Save();
    }
    frmConfirm.sayOK("Registration successful!\nIf you have any questions during your trial,\ndon't hesitate to contact us at support@airfader.com\n\nThank you!", "Success");
    Close();
}
else if (array.Length == 2 && array[0].Equals("ERROR"))
{
    frmConfirm.sayFail(array[1], "Registration Error");
}
else
{
    frmConfirm.sayFail("Could not complete registration due to an error.\nPlease visit http://airfader.com/trial to receive a key.", "Registration Error");
}
```
`frmTrial.cs:92-126` (verbatim)

This is the only place in the UI layer that uses **HTTPS** (`https://www.airfader.com/licensing/key.php`). The server response is a colon-delimited record: `OK:<key>:<something>` or `ERROR:<message>`.

Note `MixEngine.getCPUID() + ":" + MixEngine.getHDID("C")` is computed at `frmTrial.cs:89` but assigned to `_` (discarded) — the trial request does **not** send a machine fingerprint, unlike the paid registration (§5.6).

### 5.5 Global vs. per-user keys

```csharp
public static string getLicenseKey()
{
    if (Settings.Default.GlobalKey.Length > 0)
    {
        return Settings.Default.GlobalKey;
    }
    return Settings.Default.LicenseKey;
}

public static string getLicenseUser()
{
    if (Settings.Default.GlobalUser.Length > 0)
    {
        return Settings.Default.GlobalUser;
    }
    return Settings.Default.LicenseUser;
}
```
`MixEngine.cs:1210-1226` (verbatim)

The "global" key lives in `<applicationSettings>` and is written machine-wide:

```csharp
public static void updateAppConfig(string key, string value)
{
    Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    ClientSettingsSection clientSettingsSection = (ClientSettingsSection)configuration.SectionGroups["applicationSettings"].Sections["AirFader.Properties.Settings"];
    SettingElement settingElement = clientSettingsSection.Settings.Get(key);
    if (settingElement != null)
    {
        clientSettingsSection.Settings.Remove(settingElement);
        settingElement.Value.ValueXml.InnerXml = value;
        clientSettingsSection.Settings.Add(settingElement);
    }
    else
    {
        settingElement = new SettingElement(key, SettingsSerializeAs.String);
        settingElement.Value = new SettingValueElement();
        XmlDocument xmlDocument = new XmlDocument();
        settingElement.Value.ValueXml = xmlDocument.CreateElement("value");
        settingElement.Value.ValueXml.InnerXml = value;
        clientSettingsSection.Settings.Add(settingElement);
    }
    configuration.Save(ConfigurationSaveMode.Modified);
    ConfigurationManager.RefreshSection("applicationSettings");
    Settings.Default.Reload();
}
```
`MixEngine.cs:1228-1251` (verbatim)

`ConfigurationUserLevel.None` means the change applies to **every user of the machine**, hence the Administrator check in every caller.

### 5.6 Paid registration (`frmUpdate`)

`frmUpdate` is opened from `Config → About → Register` (`frmConfig.cs:889`).

```csharp
btnRegister.Text = "Registering...";
btnRegister.Enabled = false;
string str = MixEngine.getCPUID() + ":" + MixEngine.getHDID("C");
string text = "";
string text2 = "";
if (txtOwnerName.Text.Equals("Adam") && txtLicenseKey.Text.Equals("ANY"))
{
    txtOwnerName.Text = "Adam Ellsworth";
    txtLicenseKey.Text = "AF0X-Y9X9-13KK-K053";
}
txtLicenseKey.Text = txtLicenseKey.Text.ToUpper();
if (MixEngine.checkSerialV2(txtOwnerName.Text, txtLicenseKey.Text))
{
    text = "http://www.airfader.com/licensing/register.php?k=" + txtLicenseKey.Text + "&m=" + HttpUtility.UrlEncode(str) + "&v=" + Application.ProductVersion;
    switch (GetPage(text))
    {
    case "EXCEED":
        frmConfirm.sayFail("This License Key has exceeded the registration limit.\nContact support.", "Reg Limit Exceeded");
        break;
    case "BLACK":
        frmConfirm.sayFail("This License Key is reported stolen.\nContact support.", "Invalid Key");
        break;
    case "FAIL":
        frmConfirm.sayFail("This License Key does not appear valid.\nContact support.", "Invalid Key");
        break;
    default:
    {
        bool flag = false;
        WindowsIdentity current = WindowsIdentity.GetCurrent();
        WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
        if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            MixEngine.updateAppConfig("GlobalKey", txtLicenseKey.Text);
            MixEngine.updateAppConfig("GlobalUser", txtOwnerName.Text);
        }
        else
        {
            if (Settings.Default.GlobalKey.Length > 0)
            {
                frmConfirm.sayOK("A Global License Key has already been set!\nTo over-write this key, run AirFader as an Administrator.", "Access Denied");
            }
            Settings.Default.LicenseKey = txtLicenseKey.Text;
            Settings.Default.LicenseUser = txtOwnerName.Text;
            Settings.Default.Save();
        }
        frmConfirm.sayOK("License Verified.\nThank you for supporting AirFader development!", "Success");
        base.DialogResult = DialogResult.OK;
        Close();
        break;
    }
    }
}
else
{
    frmConfirm.sayFail("This License Key does not appear valid.", "Invalid Key");
}
```
`frmUpdate.cs:75-135` (verbatim)

Notable findings:

- **Back-door developer key.** `frmUpdate.cs:82-86` — entering owner name `Adam` and key `ANY` silently substitutes the real key `AF0X-Y9X9-13KK-K053` for owner `Adam Ellsworth`. This is a shipped hard-coded credential in the release build.
- **Cleartext HTTP.** The key and the machine fingerprint (`CPUID:HDID("C")`) are sent as a query string over plain HTTP.
- **Local validation before network.** `checkSerialV2` runs first, so an invalid key never reaches the server.
- **Server response is a bare token**: `EXCEED` / `BLACK` / `FAIL` / anything else = success. Any unexpected body is treated as success.

### 5.7 Unregister

```csharp
string str = MixEngine.getCPUID() + ":" + MixEngine.getHDID("C");
string url = "http://www.airfader.com/licensing/clear.php?k=" + txtLicenseKey.Text + "&m=" + HttpUtility.UrlEncode(str) + "&v=" + Application.ProductVersion;
GetPage(url);
...
MixEngine.updateAppConfig("GlobalKey", "");
MixEngine.updateAppConfig("GlobalUser", "");
...
Settings.Default.LicenseKey = "";
Settings.Default.LicenseUser = "";
Settings.Default.Save();
```
`frmUpdate.cs:163-176` (verbatim excerpt)

Guarded by a confirmation prompt at `frmUpdate.cs:155`.

### 5.8 The modal used by all of this — `frmConfirm`

`frmConfirm` provides four presentation modes:

```csharp
public enum confirmTypes
{
    YesNo,
    Ok,
    Fail,
    OkUpload
}
```
`frmConfirm.cs:11-17`

Static helpers: `askQuestion(question, title[, parent])` `:44`/`:49`, `sayOK(question, title[, parent])` `:67`/`:72`, `sayFail(question, title[, parent])` `:90`/`:95`.

`OkUpload` is the "Report / Ok" variant used for error reporting: the "Yes" button is relabelled **"Report"** (`frmConfirm.cs:150-158`), which presumably routes into the log-upload path (§8).

```csharp
case confirmTypes.OkUpload:
    btnYes.BackColor = Color.Lime;
    btnNo.BackColor = Color.MistyRose;
    btnYes.Text = "Report";
    btnNo.Text = "Ok";
    btnNo.Show();
    btnYes.Left = base.ClientRectangle.Width / 2 - btnYes.Width - 20;
    btnNo.Left = btnYes.Right + 40;
    break;
```
`frmConfirm.cs:150-158` (verbatim)

Note `sayFail` **ignores** its `parent` argument and calls `ShowDialog()` with no owner (`frmConfirm.cs:103`), while `askQuestion`/`sayOK` pass it through — an inconsistency that can leave the dialog behind other windows.

---

## 6. "Bounce" — the Pong easter egg (`frmBounce`)

`frmBounce` is a hidden game in which the player controls a paddle with a **real console fader**, the ball bounces along the channel's PEQ response curve, and the score is measured in dB.

### 6.1 Setup

```csharp
ConsoleTypes type = Console.type;
if (type == ConsoleTypes.O1V96V2)
{
    faderMap = faderMap01V96;
}
else
{
    faderMap = faderMapLS9;
}
meterData = File.ReadAllBytes("MeterData.dat");
BackColor = Settings.Default.c_background;
```
`frmBounce.cs:330-340`

Two 1024-entry fader-law tables are embedded in the source (`faderMapLS9` at `frmBounce.cs:22-127`, `faderMap01V96` at `:129-234`) — one per console family — and a binary `MeterData.dat` file is loaded from the working directory to model filter shapes.

The playfield is an `AirPanel` named `peqPong` (`frmBounce.cs:292`), sized `960x640` and sent to back (`:347-348`), and the EQ curve is drawn with `AirPanel.SetPoints(plots, eqPoints, focusEQ, activeEQ)` — the same `AirPanel` API used by `frmGraphicEQ`.

### 6.2 Gameplay

- **Player selection is via fader movement.** `MixEngine_ChannelChanged` watches for `Channel.Pr.Fader` changes; the first fader moved becomes Player 1, the second distinct fader becomes Player 2:
  ```csharp
  if (param == Channel.Pr.Fader && player1ch == -1)
  {
      player1ch = channel;
      lblPlayer1Ch.Text = MixEngine.chData[player1ch].hardname;
      lblHelp.Text = "Move a DIFFERENT fader to select Player 2's controller.";
  }
  else if (param == Channel.Pr.Fader && player2ch == -1 && channel != player1ch)
  {
      player2ch = channel;
      lblPlayer2Ch.Text = MixEngine.chData[player2ch].hardname;
      gameTime.Enabled = true;
      lblHelp.Text = "Press ON to Pause";
  }
  else if (param == Channel.Pr.On && (channel == player1ch || channel == player2ch))
  {
      gameTime.Enabled = !gameTime.Enabled;
  }
  ```
  `frmBounce.cs:385-401` (verbatim)
- **Paddle position** is derived from the channel's fader value through the per-console fader map, inverted (up = quiet):
  ```csharp
  decimal num2 = faderMap[MixEngine.chData[player1ch].getParam(Channel.Pr.Fader)];
  int num3 = (int)Math.Round((1023m - num2) / 1023.0m * (decimal)(640 - picBoxPlayer.Height) + 80m);
  ```
  `frmBounce.cs:485-486`
- **The ball rides the EQ curve.** Every tick the curve is recomputed and the ball is placed on it:
  ```csharp
  plots[i - 4].X = (int)Math.Round((decimal)(peqPong.Width - 1) / 120m * (decimal)(i - 4));
  plots[i - 4].Y = (int)Math.Round((calcGainAt(pongGain, 15m, pongFreq, i) + 20m) / 40m * (decimal)peqPong.Height);
  ...
  peqPong.SetPoints(plots, eqPoints, 0, 0);
  gameAreaCollisions();
  padlleCollision();
  playerMovement();
  ```
  `frmBounce.cs:430-442`
- **Scoring.** A point is awarded when the ball passes the frequency axis ends, and the score is displayed in dB: `lblPlayer1Score.Text = "+" + player1Score + "dB";` (`frmBounce.cs:454`). First to 10 wins (`frmBounce.cs:463`).
- **Difficulty ramps:** `ballSpeedX *= 1.05m; ballSpeedY *= 1.05m;` on every reset (`frmBounce.cs:476-477`).
- `aiMovement()` is a stub (`:498-501`).

### 6.3 `calcGainAt`

`frmBounce.cs:519-...` reimplements the console's filter-response maths. It switches on the Q value: `41` = low shelf, `42` = high shelf, `43` = HPF, each interpolating between curves stored in `meterData` with the index `(gain/6 - 1) * 124 + (freq delta) + 15251`.

---

## 7. GEQ and EFX editors

### 7.1 `frmGraphicEQ` — 31-band graphic EQ

**Band frequency map** (identical table in `frmGraphicEQ.cs:18-24` and `frmEFX.cs:15-21`):

```csharp
private string[] mapGEQ = new string[31]
{
    "20", "25", "31.5", "40", "50", "63", "80", "100", "125", "160",
    "200", "250", "315", "400", "500", "640", "800", "1k", "1.25k", "1.6k",
    "2k", "2.5k", "3.15k", "4k", "5k", "6.3k", "8k", "10k", "12.5k", "16k",
    "20k"
};
```

**31 vertical `AirFader` strips**, each a gain slider from −150 to +150 in 5 steps, colour-coded by state:

```csharp
airFader.Minimum = -150;
airFader.Maximum = 150;
airFader.Value = 0;
airFader.Steps = 5;
airFader.Channel = num;
airFader.ValueType = "geq";
airFader.LabelType = "geq";
airFader.FaderColor = "Blue";
airFader.FaderTails = false;
airFader.BackColor = (((num - num / 16) % 4 < 2) ? color : Settings.Default.c_fader_back);
```
`frmGraphicEQ.cs:395-405` (verbatim)

The alternating background (every other group of 4) comes from `RGBHSL.ModifyBrightness` (`:389`).

**Colour semantics:**

| Colour | Meaning | Source |
|---|---|---|
| Blue | band at 0 dB (inactive but assignable) | `:295`, `:146-149` |
| Yellow | band has gain | `:299`, `:248` |
| Red | no bands left available | `:182`, `:239` |

**Band budget.** The console allows 15 usable bands per GEQ:

```csharp
MixEngine.GEQData[activeRack].BandsAvail = 15 - num;
```
`frmGraphicEQ.cs:139`

and the label changes colour accordingly: Lime if >3 available, Yellow if 1-3, Red if none (`:140-187`).

**Buttons:** `btnOn` ("On"), `btnFlatAll` ("Flat All"), `btnFlatSel` ("Flat Sel"), `btnLink` ("Link"), `btnMax` ("Full Screen"), `btnClose`, `btnPrev` ("<<"), `btnNext` (">>") — all built at `frmGraphicEQ.cs:423-470`.

**Link semantics** depend on the rack type:
- GEQ type 0 (single GEQ per rack pair) → `Pr.GEQLink` set to `activeRack + 1` (`:522`)
- GEQ type 1 (A/B GEQ) → `Pr.GEQLinkLR` set to `1` or `2` depending on whether the rack is in the A or B half (`:540`)

The link button's caption shows the target, e.g. `"Link\n1 > 2"`, `"Link\n1A > 1B"` (`:315-330`).

**Rack navigation.** `btnPrev_Click` (`:572`) and `btnNext_Click` (`:604`) walk the 16 racks, distinguishing rack types `1` (GEQ) and `2` (dual A/B GEQ) and skipping type `3` (EFX) racks. The model initialises racks 0-3 as GEQ and 4-15 as EFX:

```csharp
for (int num = 0; num < 16; num++)
{
    RackData[num] = new Rack(num);
    if (num < 4)
    {
        RackData[num].Type = 1;
    }
    else
    {
        RackData[num].Type = 3;
    }
}
```
`MixEngine.cs:891-902`

**Rack-type change** prompts to close the window:

```csharp
case MixEngine.Pr.GEQType:
case MixEngine.Pr.RackType:
    if (askQuestion("Rack config changed, close window?") == DialogResult.Yes)
    {
        Close();
    }
    break;
```
`frmGraphicEQ.cs:272-278`

**Header auto-sizing** uses a `FontFit` helper that shrinks the font in 0.5 pt steps until the text fits (`:96-117`).

**Heartbeat timer**: `heartBeat = new System.Threading.Timer(tickUI, "tick", 50, -1);` (`:211`), with `tickUI` at `:361`.

### 7.2 `GEQ.cs` — the model

83 lines. `BandsAvail = 31`, `_Band = new int[31]`, `_type`/`Type` (which fires `MixEngine.OnAction("RackType", Rack)`), `_link`, `_linkLR`, `_on = 1`, and `Rack`.

`console_setBand` mirrors a band change to the paired rack:

```csharp
if (_link != 0) → mirror to `Rack - Rack%2*2 + 1`
if (_linkLR != 0) → mirror to `Rack + 8 - Rack/8*16`
```
`GEQ.cs:54` onward.

### 7.3 `frmEFX` — effect rack editor

**32 knobs** in four `AirPanel` rows of 8:

```csharp
for (int i = 0; i < 32; i++)
{
    arrParam[i] = new AirKnob();
    arrParam[i].KnobStyle = 0;
    arrParam[i].Channel = i;
    ...
    if (i < 8) { panelParam0.Controls.Add(arrParam[i]); }
    else if (i < 16) { panelParam1.Controls.Add(arrParam[i]); }
    else if (i < 24) { panelParam2.Controls.Add(arrParam[i]); }
    else { panelParam3.Controls.Add(arrParam[i]); }
}
```
`frmEFX.cs:133-157`

Knobs 0-29 are effect parameters; **knob 30 is MIX and knob 31 is BPM**, which are handled specially:

```csharp
arrParam[30].Value = MixEngine.EFXData[_activeEFX]._mix;
arrParam[31].Value = MixEngine.EFXData[_activeEFX].bpm;
```
`frmEFX.cs:215-216`

and

```csharp
switch (channel)
{
case 30:
    MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.Mix, value);
    break;
case 31:
    MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.BPM, value);
    break;
}
```
`frmEFX.cs:284-292`

**Effect type picker** — a modal borderless `Form` with 48 buttons:

```csharp
string[] array = new string[48]
{
    "Reverb Hall", "Reverb Room", "Reverb Stage", "Reverb Plate", "Early Ref.", "Gate Reverb", "Reverse Gate", "Mono Delay", "Stereo Delay", "Mod. Delay",
    "Delay LCR", "Echo", "Chorus", "Flange", "Symphonic", "Phaser", "Auto Pan", "Tremolo", "HQ. Pitch", "Dual Pitch",
    "Rotary", "Ring Mod.", "Mod. Filter", "Distortion", "Amp Simulate", "Dyna. Filter", "Dyna. Flange", "Dyna. Phaser", "Rev+ Chorus", "Rev-> Chorus",
    "Rev+ Flange", "Rev-> Flange", "Rev+ Sympho.", "Rev-> Sympho.", "Rev-> Pan", "Delay+ ER.", "Delay-> ER.", "Delay+ Rev", "Delay-> Rev", "Dist-> Delay",
    "Multi Filter", "Freeze", "Stereo Reverb", "M.Band Dyna.", "M.Band Comp", "REV-X Hall", "REV-X Room", "REV-X Plate"
};
```
`frmEFX.cs:342-349` (verbatim)

The picker is a `TopMost`, borderless, `FormStartPosition.Manual` form with background `Color.FromArgb(150, 50, 50)` (`frmEFX.cs:326`). The current type is highlighted with `c_button_active` (`:354`). "Cancel" has `Tag = -1` (`:372`).

The window title is `"Rack " + (_activeEFX + 5)` — i.e. EFX racks are numbered 5-8 (`frmEFX.cs:211`), consistent with racks 0-3 being GEQ and 4-15 covering both.

### 7.4 `EFX.cs` — the effect model

`EFX.Pr` enum: `None, BPM, Bypass, Mix, Type, Param` (`EFX.cs:10-18`).

Per-effect parameter tables are built by `setEFXType(int)`, which starts at `EFX.cs:135`. Each effect declares up to 30 params plus MIX, with a label, min/max, default, a `Values.knobTypes` display type and a label colour. Example — REVERB HALL (`EFX.cs:139-164`):

```csharp
case 0:
{
    _type = 0;
    EFXName = "REVERB HALL";
    initParam(0, 12, "Rev Time", 29, 0, 83, Values.knobTypes.EFXRevTime, Color.Yellow);
    initParam(1, 11, "Ini.Dly", 360, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
    initParam(2, 13, "Hi.Ratio", 2, 0, 9, Values.knobTypes.EFXDiv10, Color.Yellow);
    initParam(3, 14, "Lo.Ratio", 13, 0, 23, Values.knobTypes.EFXDiv10, Color.Yellow);
    initParam(4, 15, "Diff.", 8, 0, 10, Values.knobTypes.Integer, Color.Yellow);
    initParam(5, 16, "Density", 100, 0, 100, Values.knobTypes.Percent, Color.Yellow);
    initParam(6, 19, "E/R Dly", 20, 0, 1000, Values.knobTypes.Div10MS, Color.LightGreen);
    initParam(7, 20, "E/R Bal.", 44, 0, 100, Values.knobTypes.Percent, Color.LightGreen);
    initParam(8, 17, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
    initParam(9, 18, "LPF", 85, 0, 101, Values.knobTypes.EFXLPF, Color.White);
    ...
    initParam(12, 21, "Gate Lvl", 0, 0, 61, Values.knobTypes.EFXGate, Color.Red);
    initParam(13, 22, "Attack", 4, 0, 120, Values.knobTypes.IntegerMS, Color.Red);
    initParam(14, 23, "Hold", 172, 0, 215, Values.knobTypes.HoldTime48, Color.Red);
    initParam(15, 24, "Decay", 12, 0, 159, Values.knobTypes.Decay48, Color.Red);
    for (int num2 = 16; num2 < 32; num2++)
    {
        initParam(num2, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
    }
    initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
    break;
}
```
`EFX.cs:139-164` (verbatim)

**Colours encode function groups**: Yellow = tone/decay, LightGreen = early reflections, White = filters, Red = gate.

The model exposes 12 effect types in `setEFXType` (cases 0-11): REVERB HALL, REVERB ROOM, REVERB STAGE, REVERB PLATE, EARLY REF., GATE REVERB, REVERSE GATE, MONO DELAY, STEREO DELAY, MOD. DELAY, DELAY LCR, ECHO (`EFX.cs:139-411`). The other 36 types in the picker UI have **no model implementation** — selecting them produces an empty knob set.

Default rack setup:

```csharp
EFXData[0].setEFXType(0);   // REVERB HALL
EFXData[1].setEFXType(1);   // REVERB ROOM
EFXData[2].setEFXType(3);   // REVERB PLATE
EFXData[3].setEFXType(7);   // MONO DELAY
```
`MixEngine.cs:903-910`

**Tempo coupling**: for MONO DELAY (`_type == 7`), changing BPM or the note parameter auto-derives the other (`EFX.cs:107-120`):

```csharp
int type = _type;
if (type == 7)
{
    if (pValue[6] == 1 && index == 0)
    {
        double find = (double)pValue[0] * 0.1 / (60.0 / (double)bpm * 1000.0) - 0.0208;
        setParamByIndex("LOGIC", 7, findNearest(find, Values.FloatMap[Values.knobTypes.EFXNote]));
    }
    else if (pValue[6] == 1 && index == 7)
    {
        double value2 = 60.0 / (double)bpm * 1000.0 * Values.FloatMap[Values.knobTypes.EFXNote][pValue[7]];
        setParamByIndex("LOGIC", 0, (int)(Math.Round(value2, 1) * 10.0));
    }
}
```
`EFX.cs:107-120` (verbatim)

---

## 8. Debug UI — `frmDebug`

A 169-line modeless form with **standard WinForms controls** (`Button`, `ComboBox`, `Label`) — deliberately not the AirControls skin, so it looks foreign next to the rest of the app.

| Control | Line | Text / behaviour |
|---|---|---|
| `cmdOpenLog` | `frmDebug.cs:109` | "Open Log File" → `Process.Start("%APPDATA%\AirFader\Error.Log")` (`:33-45`) |
| `cmdOpenFolder` | `:112` | "Open Log Folder" → `Process.Start("%APPDATA%\AirFader\")` (`:47-59`) |
| `cbLogLevel` | `:119` | Items `"0","1","2","3"` (`:120`) |
| `cmdSave` | `:125` | "Save" → persists `Settings.Default.debugLevel` and closes (`:66-71`) |
| `lblDebug` | `:132` | "Debug Log Level" |
| `lblTitle` | `:137` | "Debug Settings" (14.25 pt) |
| `btnSendLog` | `:145` | "Send Log Now" (`:73-83`) |

The log-upload action:

```csharp
private void btnSendLog_Click(object sender, EventArgs e)
{
    if (MixEngine.uploadLogFile())
    {
        MessageBox.Show("Upload successful.");
    }
    else
    {
        MessageBox.Show("Upload failed. Are you connected to the Internet right now?");
    }
}
```
`frmDebug.cs:73-83` (verbatim)

which posts the raw log file to the vendor:

```csharp
public static bool uploadLogFile()
{
    try
    {
        logClose();
        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        folderPath += "\\AirFader\\Error.Log";
        using (WebClient webClient = new WebClient())
        {
            webClient.UploadFile("http://www.airfader.com/data/recvlog.php", "POST", folderPath);
        }
        return true;
    }
    catch (Exception ex)
    {
        logError("cmdOpenLog()", "UI", ex.ToString());
    }
    ...
}
```
`MixEngine.cs:17513-17530` (verbatim excerpt)

**The debug window is hidden** behind a double-click on the About-tab logo (`frmConfig.cs:988-999`, §3.6).

### 8.1 The other debug affordances

- `isDebug` compile-time flag: when true, the app deletes `Error.Log` on startup and writes verbose progress (`frmSurface16.cs:503-521`).
- The log level is persisted as `Settings.Default.debugLevel` (default `1` — `app.config:154-156`).
- `MixEngine` also ships a **usage-metrics uploader** that POSTs session metrics anonymously:

  ```csharp
  using (MyWebClient myWebClient = new MyWebClient())
  {
      myWebClient.timeOutSec = 1;
      myWebClient.UploadFile("http://www.airfader.com/data/recvmetrics.php", "POST", text);
  }
  ```
  `MixEngine.cs:1874-1878` (verbatim)

  `MyWebClient` is a 16-line `WebClient` subclass with a `timeOutSec` property.

---

## 9. `AirControls.dll` — the custom control library

Decompiled to `re\src\AirControls\`. Seven public types, no interfaces, no abstract bases. All inherit directly from `System.Windows.Forms.Control` / `UserControl` / `Button`.

### 9.1 `AirFader` — the fader / vertical slider

`re\src\AirControls\AirFader.cs`

**Delegate:**
```csharp
public delegate void AirFaderEventHandler(int channel, int value, string flag);
```
`AirFader.cs` (event `FaderChanged` at `:1245`)

**Properties (with line numbers):**

| Property | Line | Notes |
|---|---:|---|
| `Channel` | 789 | Index used by the event |
| `MeterLevel` | 801 | Clamped to 127 |
| `Value` | 818 | |
| `ValueDB` | 854 | dB conversion |
| `ValueInt` | 884 | |
| `ValueLabel` | 908 | Text shown on the cap |
| `ValueType` | 910 | Display format selector: `"fader"`, `"geq"`, `"div10"`, `"onoff"`, … |
| `LabelType` | 932 | `"fader"` / `"geq"` |
| `TickType` | 945 | `"fader"` |
| `MeterType` | 958 | `"side"` |
| `Maximum` | 971 | default 100 |
| `Minimum` | 989 | default 0 |
| `Steps` | 1007 | default 1 |
| `Horizontal` | 1020 | |
| `FaderTails` | 1033 | |
| `FaderStyle` | 1045 | |
| `FaderColor` | 1064 | default `"White"` |
| `KnobColor` | 1110 | default `"White"` |
| `BackColor` | 1136 | |
| `ForeColor` | 1165 | |
| `TailColor` | 1179 | |
| `FineMode` | 1193 | |
| `IsActive` | 1205 | |
| `ShowMeter` | 1218 | |
| `ShowKnob` | 1231 | |

**Methods:** `SuspendPaint()` `:1263`, `ResumePaint()` `:1268`, `SetValue(int)` `:1341`.

**Internals:** a 1024-entry `dbMap` converts the 0-1023 linear fader value to dB; the default `ValueType` is `"fader"` (0-100 with 1 step), but the app reassigns it per use (`"geq"` in the GEQ editor, `"div10"`/`"onoff"` in the PEQ editor).

### 9.2 `AirKnob` — rotary encoder

`re\src\AirControls\AirKnob.cs`

```csharp
public delegate void AirKnobEventHandler(AirKnob sender, int channel, int value, string flag);
```

| Property | Line |
|---|---:|
| `Channel` | 640 |
| `Value` | 652 |
| `ValueDB` | 676 |
| `ValueType` | 715 |
| `KnobLabel` | 727 |
| `KnobType` | 740 (`Values.knobTypes`) |
| `ValueList` | 753 |
| `Active` | 766 |
| `Maximum` | 779 |
| `Minimum` | 792 |
| `Steps` | 805 |
| `KnobStyle` | 817 |
| `FineMode` | 48 |
| `Flag` | 50 |

Event `KnobChanged` at `:833`.

### 9.3 `AirMeter` — level meter

`re\src\AirControls\AirMeter.cs`

| Property | Line |
|---|---:|
| `Channel` | 331 |
| `Value` | 343 |
| `ValueDB` | 367 |
| `Maximum` | 379 |
| `Minimum` | 392 |
| `TickMark` | 405 |
| `BarStyle` | 418 |
| `BorderColor` | 433 |
| `BarColor` | 446 |
| `FineMode` | 30 |

### 9.4 `AirButton` — the workhorse control

`re\src\AirControls\AirButton.cs`

Used for **everything**: page buttons, mute/cue, on/off, type pickers, dialog buttons. Notable members:

- `BackText` — the text drawn on the button's "background" region (used for "On Faders" / "Offline" second-line captions, e.g. `btnPageSOF.BackText = "On Faders"` at `frmSurface16.cs:574`).
- `LEDColor` — a separate LED indicator colour (used in `frmConfirm.cs:123-127`).
- `ParentColor` — the colour the button blends into, allowing seamless embedding in panels (`frmSurface16.cs:563`).
- `SubFont` (`:152`) and `Flag` (`:158`).
- Nested helper class `AirButton.ExtendedGraphics` with `FillRoundRectangle` (`:33`), `DrawRoundRectangle` (`:43`), `GetRoundedRect` (`:50`, `:60`, `:67`) — the rounded-rectangle drawing primitives used for the button faces.

### 9.5 `AirPanel` — the EQ curve canvas

`re\src\AirControls\AirPanel.cs`

| Member | Line |
|---|---:|
| `BorderColor` | 27 |
| `EQColor` | 40 (default `Color.Lime`) |
| `BorderSize` | 53 |
| `PanelType` | 66 |
| `SetPoints(Point[] EQPoints, Point[] EQDots, int focusEQ, int activeEQ)` | 78 |

`SetPoints` is the shared drawing primitive behind **both** the GEQ curve (`frmGraphicEQ.cs:439`-style usage) and the Bounce game playfield (`frmBounce.cs:360`, `:439`).

### 9.6 `AirLabel`

`re\src\AirControls\AirLabel.cs` — a label with a `TextRenderingHint` property (anti-aliased text). Used for the game score labels in `frmBounce` (`frmBounce.cs:278-290`).

### 9.7 `Values` — the shared enum + value maps

`re\src\AirControls\Values.cs`

```csharp
public enum knobTypes
{
    None, Integer, IntegerMS, IntegerPlusOne, OnOff, Percent,
    Div10, Div10MS, List, HoldTime44, HoldTime48, Decay44, Decay48,
    EFXDiv10, EFXRevTime, EFXHPF, EFXLPF, EFXGate, EFXRefType,
    EFXFBGain, EFXGateType, EFXNote, EFXModNote, EFXModFreq, EFXModWave,
    EFXLevelPct
}
```
`Values.cs:7-35`

Plus a `ValueMap` dictionary (`Values.cs:37+`) and a `FloatMap` dictionary of `double[]` lookup tables (used by `EFX.cs:113` and `:117` for tempo/note conversion).

These `knobTypes` are exactly the display-format contract between the app and the library: the app sets `AirKnob.KnobType` and the control decides how to render the value (integer, percent, dB×10, milliseconds, note names, filter frequencies…).

---

## 10. Hardcoded strings, URLs and e-mail addresses

### 10.1 Complete URL inventory

| URL | Purpose | Location |
|---|---|---|
| `http://www.airfader.com/data/ver.php?key=<KEY>&ver=<VERSION>` | Update check | `frmConfig.cs:956` |
| `http://www.airfader.com/download` | Opens the browser to download | `frmConfig.cs:977` |
| `https://www.airfader.com/licensing/key.php?action=AUTOTRIAL&name=<NAME>&email=<EMAIL>&v=<VER>` | Trial key request (**only HTTPS endpoint**) | `frmTrial.cs:92` |
| `http://www.airfader.com/licensing/register.php?k=<KEY>&m=<MACHINEID>&v=<VER>` | Licence registration | `frmUpdate.cs:90` |
| `http://www.airfader.com/licensing/clear.php?k=<KEY>&m=<MACHINEID>&v=<VER>` | Unregister / release the key | `frmUpdate.cs:164` |
| `http://www.airfader.com/data/recvlog.php` | Error-log upload (POST) | `MixEngine.cs:17522` |
| `http://www.airfader.com/data/recvmetrics.php` | Usage-metrics upload (POST) | `MixEngine.cs:1877` |

All `www.airfader.com` URLs are HTTP except the trial request. Query strings carry the licence key and the machine fingerprint (`CPUID:HDID("C")`) in cleartext.

### 10.2 E-mail addresses

| Address | Location |
|---|---|
| `support@airfader.com` | `frmTrial.cs:116` |

### 10.3 User-facing URLs embedded in messages

| Message text | Location |
|---|---|
| `http://www.airfader.com/upgrade` (free exchange for old keys) | `MixEngine.cs:1281`, `MixEngine.cs:1301` |
| `http://www.airfader.com` (purchase) | `MixEngine.cs:1323`, `:1332`, `:1342` |
| `http://airfader.com/trial` | `frmTrial.cs:125`, `frmUpdate.cs:255` |

### 10.4 Shipped credential

| Value | Location |
|---|---|
| Owner `Adam`, key `ANY` → substituted with `AF0X-Y9X9-13KK-K053` / `Adam Ellsworth` | `frmUpdate.cs:82-86` |

### 10.5 Hardcoded machine identifiers

- `MixEngine.getCPUID()` + `":"` + `MixEngine.getHDID("C")` — a CPU+disk fingerprint sent with every registration/unregistration (`frmUpdate.cs:79`, `:163`).
- `GetWindowText((IntPtr)792644, stringBuilder, 12)` — the fixed window handle used by `btnScan` to find a running console (`frmConfig.cs:1092`).
- `"MeterData.dat"` — a data file loaded from the *current working directory* by the Bounce game (`frmBounce.cs:339`).

### 10.6 Vendor product identifiers

| String | Location |
|---|---|
| `Yamaha LS9-16`, `Yamaha LS9-32`, `Yamaha M7CL-32`, `Yamaha M7CL-48`, `Yamaha 01V96`, `Yamaha 01V96i` | `frmConfig.cs:2393` |
| `Yamaha LS9-32` (default `ConsoleType`) | `app.config:58-60` |

Console-model enum `ConsoleTypes`: `LS9, M7CL, PM5D, O1V96V2, Unknown, CLx, QLx, TFx` (`ConsoleTypes.cs`).

---

## 11. Cross-cutting observations

### 11.1 Architectural style

The entire UI is **imperative and runtime-constructed**. `InitializeComponent()` only creates the bare WinForms shells (tab pages, list boxes, static buttons); every themeable control (`AirButton`, `AirFader`, `AirKnob`, `AirMeter`, `AirPanel`) is instantiated in code, given explicit colours from `Settings.Default`, and added to a panel. This makes live theme switching possible (change `Settings.Default.c_*` → recolour every control → `redoLayout = true`), which a designer-built UI could not do without a full re-layout pass.

### 11.2 Centralised messaging

Every user-facing dialog goes through `frmConfirm` (`askQuestion` / `sayOK` / `sayFail`), except `frmDebug` which uses raw `MessageBox.Show`. `frmConfirm` is `FixedDialog`, `ShowInTaskbar = false`, `StartPosition = CenterParent`, with `AcceptButton = btnYes` and `CancelButton = btnNo` (`frmConfirm.cs:237-249`).

### 11.3 Layering violations found

| Finding | Location |
|---|---|
| `MixEngine` (engine layer) directly opens UI dialogs (`frmConfirm.sayFail`) | `MixEngine.cs:1281`, `:1301`, `:1323`, `:1332`, `:1342` |
| `MixEngine` holds a static `hMainWindow` reference to `frmSurface16` | `MixEngine.cs:524` (set), used at `MixEngine.cs:1281` |
| `frmConfig` reaches into the parent form's `layerCustom` list | `frmConfig.cs:618` |
| `EFX.cs` writes to `MixEngine.updateRack` and calls `MixEngine.PushEFXParam` | `EFX.cs:102-122` |

### 11.4 Threading

`MixEngine` raises events from its I/O threads; every handler that touches UI marshals via `InvokeRequired` + `BeginInvoke`:

```csharp
if (lblHeader.InvokeRequired)
{
    EngineChangedCB method = MixEngine_EngineChanged;
    BeginInvoke(method, param, rack, paramIndex, value);
}
```
`frmGraphicEQ.cs:216-220` (same pattern in `frmChannel`, `frmEFX.cs:443-447`, `frmBounce.cs:375-380`).

### 11.5 Security summary of the UI layer

| Issue | Severity | Evidence |
|---|---|---|
| Licence keys + machine fingerprint sent over plaintext HTTP | High | `frmUpdate.cs:90`, `:164`; `frmConfig.cs:956` |
| Shipped developer back-door key `AF0X-Y9X9-13KK-K053` | High | `frmUpdate.cs:82-86` |
| Client-side-only licence validation (fully reversible) | High | `MixEngine.cs:1258-1354` |
| Machine-wide licence writes from a user-configurable path | Medium | `MixEngine.cs:1228-1251` |
| No HTTP timeout; UI thread can hang on a slow server | Medium | `frmConfig.cs:912-947` |
| Licence validity inferable offline from the key itself (day-offset) | Medium | `MixEngine.cs:1286`, `:1313-1328` |
| Error log upload (may contain user data) is opt-in only | Low | `MixEngine.cs:17513-17530` |
| Anonymous usage metrics uploaded by default | Informational | `MixEngine.cs:1874-1878` |
| `MeterData.dat` loaded from CWD (path/loading issue) | Low | `frmBounce.cs:339` |
| `sayFail` ignores its parent window handle | Low | `frmConfirm.cs:103` |

---

## Appendix A — Form map

| Form | File | Opened from |
|---|---|---|
| `frmSurface16` | `frmSurface16.cs` | `Program.cs` (main window) |
| `frmChannel` | `frmChannel.cs` | Main window (double-click a strip) |
| `frmConfig` | `frmConfig.cs` | Main window ("Config" button) |
| `frmTrial` | `frmTrial.cs` | `frmSurface16.cs:1796` |
| `frmUpdate` | `frmUpdate.cs` | `frmConfig.cs:889`, `frmTrial.cs:148` |
| `frmGraphicEQ` | `frmGraphicEQ.cs` | Main window GEQ button |
| `frmEFX` | `frmEFX.cs` | EFX rack button |
| `frmScene` | `frmScene.cs` | Scene load/save |
| `frmKeyboard` | `frmKeyboard.cs` | Channel/scene rename |
| `frmConfirm` | `frmConfirm.cs` | Everywhere |
| `frmDebug` | `frmDebug.cs` | `frmConfig.cs:992` (logo double-click) |
| `frmBounce` | `frmBounce.cs` | Hidden easter egg |
| `frmFancyBox` | `frmFancyBox.cs` | Legacy/unused info box |

## Appendix B — `AirControls` public API summary

| Type | Base | Key members |
|---|---|---|
| `AirFader` | `Control` | 24 properties incl. `Value`, `ValueDB`, `ValueType`, `FaderColor`, `ShowMeter`; event `FaderChanged` |
| `AirKnob` | `Control` | `KnobType`, `KnobLabel`, `ValueList`, `Active`; event `KnobChanged` |
| `AirMeter` | `Control` | `Value`, `ValueDB`, `TickMark`, `BarStyle` |
| `AirButton` | `Button`-like | `BackText`, `LEDColor`, `ParentColor`, `SubFont`, `Flag`; nested `ExtendedGraphics` |
| `AirPanel` | `Control` | `SetPoints(Point[], Point[], int, int)`, `EQColor`, `BorderSize`, `PanelType` |
| `AirLabel` | `Control` | `TextRenderingHint` |
| `Values` | static | `enum knobTypes` (26 members), `ValueMap`, `FloatMap` |
