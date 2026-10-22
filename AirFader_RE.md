# AirFader 3.2.5833.4432 — Engenharia Reversa

**Alvo:** `C:\Program Files (x86)\air\AirFader`
**Fabricante:** Molten Logic LLC (Copyright © 2015)
**Natureza:** superfície de controle remota (faders) para consoles de áudio digitais Yamaha via MIDI/SysEx
**Data da análise:** 21/10/2026

---

## 1. Escopo e método

Análise **estática completa**, sem execução do aplicativo e sem alteração dos arquivos instalados.

Pipeline de descompilação montado do zero (o ambiente não tinha SDK .NET, apenas o runtime 8.0):

1. Download do pacote NuGet `ilspycmd 9.0.0.7889` (engines `ICSharpCode.Decompiler` + `ICSharpCode.ILSpyX`).
2. Execução via `dotnet <runtime>\ilspycmd.dll` (funciona com o runtime 8.0 já presente, sem SDK).
3. Decompilação em projeto compilável: `ilspycmd -p -o <dir> AirFader.exe` e `AirControls.dll`.

Artefatos gerados:

| Caminho | Conteúdo |
|---|---|
| `re/src/AirFader/` | código-fonte C# reconstruído (72 arquivos, 90 arquivos .cs) |
| `re/src/AirControls/` | biblioteca de controles customizados decompilada |
| `re/analysis/airfader_keygen.py` | reimplementação do algoritmo de licença (Python, funcional) — **não incluído** neste repositório |
| `AirFader_UI_REPORT.md` | relatório detalhado da camada de UI (2.066 linhas) |

Cobertura: 100% do código gerenciado — nenhum obfuscator, nenhum packer. Os nomes de símbolos são originais do desenvolvedor.

---

## 2. Resumo executivo

AirFader é um cliente Windows Forms (.NET Framework 2.0/3.5, x86) que se apresenta ao console de áudio via **MIDI System Exclusive** (protocolo Yamaha, manufacturer ID `0x43`), mantendo um modelo interno completo de canais/parâmetros do console, e opcionalmente **expõe um servidor TCP** para clientes remotos (apps iOS/Android) usando um protocolo proprietary chamado `faderspeak`, serializado com Protocol Buffers.

Achados principais:

| # | Achado | Severidade |
|---|---|---|
| 1 | **Algoritmo de licença totalmente quebrado** — checksum não-secreto, chave perpetualtrivial de gerar para qualquer nome | **Crítico** |
| 2 | **Backdoor de desenvolvedor embarcado** (`Adam`/`ANY` → chave perpetual de 99 clientes) | **Crítico** |
| 3 | Servidor TCP sem TLS, com credenciais em MD5 e hash passável em texto claro | **Alto** |
| 4 | Licença/telemetria trafegando em HTTP simples (chave de licença em cleartext na URL) | **Alto** |
| 5 | Identificação de máquina (`CPUID` + `HDID` + MAC) não é usada na validação da chave | **Médio** |
| 6 | Sem criptografia ou assinatura na serialização protobuf do protocolo de rede | **Médio** |

---

## 3. Arquitetura

```
Program.Main  →  frmSurface16  (janela principal / superfície de controle)
                    │
                    ├── MixEngine          (17.532 linhas — núcleo: estado, filas, SysEx, licença)
                    │     ├── Channel / Console / Rack / GEQ / EFX / OutPort / Scene / Group / DCAGroup
                    │     └── Sanford.Multimedia.Midi  →  console Yamaha
                    │
                    ├── FaderService        (servidor TCP :12301, protobuf "faderspeak")
                    │     └── faderspeak/*   (contratos protobuf-net)
                    │
                    └── Forms auxiliares
                          frmChannel  frmConfig  frmEFX  frmGraphicEQ  frmScene
                          frmBounce  frmTrial  frmUpdate  frmConfirm  frmDebug
                          frmKeyboard  frmFancyBox

AirControls.dll  →  AirFader, AirKnob, AirMeter, AirButton, AirPanel, AirLabel, Values
```

**Fluxo de dados em dois sentidos:**

- **Console ← AirFader:** `MixEngine.sendParam()` monta um template SysEx textual, substitui marcadores (`mmmm`, `cccc`, `zzzz`, …) por valores codificados via *stuffing* de 7 bits, converte hex→bytes e envia por `SysExMessage` no `OutputDevice` MIDI.
- **Console → AirFader:** `HandleSysExMessageReceived` enfileira bytes; `queueRunner` converte para string hex e `processSysEx()` despacha por padrões (ex.: `F043103E127FF7` = resposta LS9-32).

**Sincronização:** máquina de estados `ConnectionPhase` = `Closed → AutoDetect → PreSync → Syncing → PostSync → Synced`. O *slow dump* (`slowDump()`, `slowChannelSync()`) re-sincroniza canal a canal; a fila de requisições tem *throttle* de ~2 mensagens por 2 s com timeout de 5 s por item.

---

## 4. Protocolo de rede `faderspeak`

### 4.1 Framing

Confirmado no código (`FaderService.cs`):

```
Empacotamento:  [4 bytes BIG-ENDIAN length][payload protobuf-net BaseMessage]
```

- **Envio:** `Serializer.SerializeWithLengthPrefix(stream, msgBase, PrefixStyle.Fixed32BigEndian)` (`FaderService.cs` em `sendProto(BaseMessage, SocketPacket)`).
- **Recebimento:** `BitConverter.ToInt32(msgBuffer, 0)` → `SwapInt32()` → `Serializer.Deserialize<BaseMessage>(new MemoryStream(msgBuffer, 4, v))` (`FaderService.cs:525-545`).

### 4.2 Autenticação

`FaderService.processProc()` (`FaderService.cs:597+`):

1. Cliente envia `BaseMessage` com `userName` + `passHash` + `version`.
2. `version` vazia ou `< 20130224` → `MSG_DENIED` ("Server is 3.x").
3. Busca usuário no banco (`%APPDATA%\AirFader\Users.dat`, protobuf length-prefixed `UserRecord`).
4. Compara `baseMsg.passHash == userRecord.passHash` — **comparação de string simples**, sem PBKDF/bcrypt.
5. Se `userRecord.active == false` → `MSG_DENIED`.
6. Sucesso → atribui `sessionID` aleatório, responde `MSG_AUTH` com `version = "20151201"`.

Mensagens subsequentes exigem `sessionID` coincidente, senão `MSG_DENIED` ("Transaction blocked").

### 4.3 Contratos protobuf

Namespace `faderspeak`, todos `[ProtoContract]`/`IExtensible`:

| Classe | Papel |
|---|---|
| `BaseMessage` | envelope raiz (16 campos) |
| `Command` / `CmdParam` | comandos: `CMD_SENDPARAM`, `CMD_SYNCCHANNEL`, `CMD_BUSSYNC`, `CMD_GETSYS`, `CMD_GOODBYE`, `CMD_RESYNC`, … |
| `ChannelParam` / `ChParam` | 83 parâmetros de canal (`CH_FADER`, `CH_HA`, `CH_PHANTOM`, `CH_EQ*`, `CH_DYN*`, `CH_SENDTOMIX*`…) |
| `GeneralParam` / `GenParam` | params gerais: mute master, seleção de canal, **meters**, outports |
| `SystemStatus` | status do console: tipo, offsets de canal, max inputs/STIN/mix/matrix |
| `ChannelSync` / `BusSync` / `GEQSync` / `RackSync` | sincronização de estado |
| `MeterData` | 8 pontos de medição por canal (pre, eq in/out, dyn0 in/gr, dyn1 in/gr, post) |
| `UserRecord` | credenciais + matriz de permissões |
| `GenMessage` / `MsgType` | `MSG_OK/FAIL/AUTH/DENIED/CONSOLE_ONLINE/OFFLINE/BCAST` |

**IDs de canal** (offsets em `MixEngine.cs:833-838`): Input=0, STIN=64, Mix=72, Matrix=88, Stereo=96, Monitor=99. Total 100 canais (`chData = new Channel[100]`).

---

## 5. Camada MIDI / SysEx

### 5.1 Codificação "stuffing"

Yamaha usa 7 bits por byte nos dados SysEx. O app empacota/desempacota manualmente:

- `StuffValueStr(int)` (`MixEngine.cs:384-396`) — empacota um int em 5 bytes de 7 bits, devolve string hex de 10 chars.
- `StuffString(string)` (`MixEngine.cs:398-413`) — nome de 4 chars, ASCII reversed → int32 → mesmo empacotamento.
- `UnStuffString` / `UnStuffValue` / `UnStuffBytes` (`MixEngine.cs:415-462`) — o inverso.

Templates SysEx observados:

```
F0431n3E120101140000cccc<stuffed>F7   → definir nome de canal (LS9)
F0431n3E1201023900100000F7            → selecionar canal (LS9)
F0433n3E7F011A00ccddddddddF7          → ligar/desligar canal (01V96)
F0431n3E1101012200000029F7            → PrefNaviToSelIn (M7CL)
```

O byte `n` é o `MIDIdeviceID`; `0x43` = Yamaha; `0x3E` = submensagem AIR.

### 5.2 Consoles suportados

`ConsoleTypes`: `LS9, M7CL, PM5D, O1V96V2, Unknown, CLx, QLx, TFx`
`ConsoleVersions`: `LS9, M7CLv3, M7CLv35, Unknown, O1V96V2, O1V96i`

Cada modelo tem sua própria tabela de *patch maps* (`mapInPatch*`, `mapOutPatch*`) e handlers de dump (`processDumpLS9`, `processDumpM7CL`, `processDump01V96`).

**Variante O1V96i:** `sendData()` reescreve o byte de comando `0x0D` → `0x1A` (`MixEngine.cs:17349-17352`).

### 5.3 Estrutura de memória

| Tipo | Dimensão | Observação |
|---|---|---|
| `Channel[] chData` | 100 | índices por offset de tipo |
| `EFX[] EFXData` | 4 | 4 racks de efeito |
| `Scene[] sceneData` | 301 | nome em 32 bytes ASCII + comentário |
| `GEQ` | 31 bandas | por rack |
| `OutPort` | `maxOutOmni + maxOutSlot + maxOut2TR` | |
| `Group` / `MuteGroup` / `DCAGroup` | — | agrupamentos |

---

## 6. Licenciamento — algoritmo quebrado (verificado)

### 6.1 Formato da chave

```
AF XX - YYYY - ZZZZ - WWWW
        │       │       └─ rand (base32 custom, 4 dígitos)
        │       └───────── dias desde 2011-01-01 (base32 custom)  |  36500 = PERPETUAL
        └───────────────── checksum (base32 custom, 4 dígitos)
  └────────────────────── version/server (base32 custom, 2 dígitos)
```

Alfabeto base32 custom (32 símbolos, **sem** `I`, `L`, `O`, `S`):
`0123456789ABCDEFGHXJKYMNWPQRZTUV`

### 6.2 Geração — `MixEngine.genKeyV2()` (`MixEngine.cs:1180-1208`)

```csharp
name = Regex.Replace(name, "[^a-zA-Z0-9 ]", "");
name = name.Trim();
if (ignoreCase) name = name.ToUpper();

int num = server * 16 + version;
string s = "AF" + base32((ulong)num, 2) + name + base32((ulong)time, 4);

ulong num2 = 0, num3 = 0;
byte[] bytes = Encoding.ASCII.GetBytes(s);
foreach (byte b in bytes)
    num2 += (ulong)((long)(++num3) * (long)offset1[b]);   // soma ponderada

ulong num4 = (ulong)rand ^ num2;                          // ← o "segredo" é rand
return ("AF" + base32((ulong)num, 2) + "-" + base32(num4, 4)
      + "-" + base32((ulong)time, 4) + "-" + base32((ulong)rand, 4)).ToUpper();
```

`offset1` é uma tabela de 256 pesos fixos (`MixEngine.cs:794-822`).

### 6.3 Por que é quebrável

`num4 = rand ^ num2`. Como **`rand` é escolhível pelo atacante** e `num2` é uma função pública e determinística de `(nome, versão, server, tempo)`, basta:

```
escolher rand = 0
num4 = num2 mod 2^20
chave = AF + base32(server*16+version, 2) + "-" + base32(num4,4) + "-" + base32(36500,4) + "-" + "0000"
```

Não há assinatura digital, HMAC, chave do fabricante nem entropia real. A "assinatura" é apenas uma XOR com um valor que o próprio cliente escolhe.

### 6.4 Validação — `MixEngine.checkSerialV2()` (`MixEngine.cs:1258-1354`)

- Normaliza `O→0`, `I→1`, `L→1`, `S→5` (tenta corrigir OCR).
- Divide em 4 grupos por `-`; precisa de exatamente 4.
- Rejeita prefixo `AF01` (versão antiga).
- Decodifica `version = num2 & 0xF`, `server = (num2 & 0x10) / 16`.
- Recalcula `genKeyV2()` com o nome **exato** do usuário; se falhar, tenta com nome em maiúsculas (`ignoreCase: true`).
- `version < 2` → rejeita.
- `server == 1` → `licenseClients = 99` (licença multi-sala); senão `1`.
- `tempo == 36500` → **retorna `true` imediatamente** (licença perpetual).
- Senão: `tempo < (DateTime.Now - 2011-01-01).Days` → trial expirado; grava `Settings.Default.Expired = true`.
- Bloqueio de força bruta: `serialRetry >= 10` → sempre falso.

### 6.5 Prova prática

`re/analysis/airfader_keygen.py` reimplementa o algoritmo **verbatim** e valida contra o validador do próprio app. O script **não é publicado** neste repositório (ver `.gitignore`); a saída abaixo é o registro da execução:

```
1) Verificar a chave do backdoor embarcado (frmUpdate.cs:83-87)
   backdoor key        : AF0X-Y9X9-13KK-K053
   owner               : Adam Ellsworth
   group2 (checksum)   : 697929
   group3 (time days)  : 36500 (36500 == sentinela perpetual)
   group4 (rand)       : 655523
   checkSerialV2 ->    : True | perpetual license (clients=99, version=2)
   recomputed key      : AF0X-Y9X9-13KK-K053 (match)

2) Gerar chaves válidas próprias
   Attacker       -> AF0J-0W6C-13KK-0000     valid=True  (perpetual license (clients=99, version=3))
   RE Lab         -> AF0J-0G69-13KK-0000     valid=True  (perpetual license (clients=99, version=3))
   Molten Logic   -> AF0J-0VT9-13KK-0000     valid=True  (perpetual license (clients=99, version=3))
```

**Conclusão:** é possível gerar, para qualquer nome de proprietário, uma chave perpetual válida com 99 clientes conectados simultaneamente, sem qualquer material secreto do fabricante.

### 6.6 Backdoor de desenvolvedor embarcado

`frmUpdate.cs:83-87`:

```csharp
if (txtOwnerName.Text.Equals("Adam") && txtLicenseKey.Text.Equals("ANY"))
{
    txtOwnerName.Text = "Adam Ellsworth";
    txtLicenseKey.Text = "AF0X-Y9X9-13KK-K053";
}
```

Trocar o dono por `Adam` e a chave por `ANY` gera automaticamente a chave perpetual de 99 clientes. Este é o caminho mais trivial para burlar a licença.

### 6.7 Identificação de máquina é decorativa

`getCPUID()` (WMI `win32_processor`), `getHDID("C")` (`win32_logicaldisk` VolumeSerialNumber) e `checkMAC()` existem, mas **não participam da validação da chave** — são enviados apenas ao servidor de registro:

```
http://www.airfader.com/licensing/register.php?k=<CHAVE>&m=<CPUID:HDID>&v=<versão>
http://www.airfader.com/licensing/clear.php?k=<CHAVE>&m=<CPUID:HDID>&v=<versão>
```

`checkMAC()` (`MixEngine.cs:8011-8075`) contém um *kill switch* temporal (`>= 2010-12-14 12:00` → -2) e dois padrões de MAC reverso allow-listed (`:CFB63FE1B11`, `88FB63FE1B11`) — vestígios de depuração.

---

## 7. Dados e configuração

### 7.1 `Meterdata.dat` — decodificado

15.872 bytes. **Não é dado de medição ao vivo**: é uma tabela de *lookup* da curva de fader (*taper*) usada para converter dB ↔ posição de pixel do knob/cap do fader e do medidor.

Estratégia de indexação (de `frmChannel.cs:2700-2807`):

| Faixa | Estrutura | Uso |
|---|---|---|
| `0 … 15250` | 41 blocos × 5084 (41×124) | lei de fader por faixa de ganho (passos de 6 dB) |
| `15251 … 15623` | 3 blocos × 124 | lei alternativa (ganho >18 dB) |
| `15624 … 15871` | 124 entradas × 2 bytes big-endian | escala do medidor, valor = `-(hi*256+lo)/10` |

Constante mágica: **124** = número de passos de resolução; **5084 = 41 × 124**; **15251 = 123×124 − 1**; **15624 = 126×124**.

### 7.2 `AirFader.exe.config`

49 `userSettings` + 2 `applicationSettings` (`GlobalKey`, `GlobalUser`). Destaques:

| Chave | Padrão | Significado |
|---|---|---|
| `ConsoleType` | `Yamaha LS9-32` | console alvo |
| `serverPort` | `12301` | porta do servidor TCP |
| `serverAutoStart` / `autoConnect` / `autoMeters` | `False`/`False`/`True` | |
| `faderCount` / `maxChannels` | `8` / `64` | |
| `CustomLayer` | `0,1,2,3,72,73,74,98` | → Mix 1–3 (72–74) e Stereo Mono (98) |
| `ChannelStrip` | `Label,Cue,Name,Mute,Fader` | ordem dos elementos do strip |
| `LicenseKey` / `LicenseUser` | vazio | |
| `Expired` / `Upgraded` | `False` / `True` | flags de trial |
| `debugLevel` | `1` | |

### 7.3 Persistência

| Arquivo | Conteúdo |
|---|---|
| `%APPDATA%\AirFader\Users.dat` | `UserRecord` protobuf length-prefixed — **hash de senha em MD5** |
| `%APPDATA%\AirFader\Error.Log` | log de depuração |
| `%APPDATA%\AirFader\Critical.Log` | crashlog (`Program.Main`) |
| `AirFader.exe.config` | settings globais (`GlobalKey`/`GlobalUser` — applicationSettings, machine-wide) |

---

## 8. Endereços de rede hardcoded

| URL | Uso | Criptografia |
|---|---|---|
| `http://www.airfader.com/licensing/register.php` | registra chave+máquina | **HTTP** |
| `http://www.airfader.com/licensing/clear.php` | desregistra | **HTTP** |
| `https://www.airfader.com/licensing/key.php?action=AUTOTRIAL` | solicita trial | HTTPS |
| `http://www.airfader.com/data/ver.php?key=&ver=` | checa atualização | **HTTP** (chave em cleartext) |
| `http://www.airfader.com/data/recvlog.php` | upload do log de erros | **HTTP** |
| `http://www.airfader.com/data/recvmetrics.php` | telemetria de conexões | **HTTP** |
| `http://www.airfader.com/download` | baixa atualização | HTTP |
| `http://www.airfader.com/upgrade` | troca de versão | HTTP |

E-mail: `support@airfader.com`

---

## 9. UI — resumo

Relatório completo em **`AirFader_UI_REPORT.md`** (2.066 linhas). Pontos-chave:

- **Construída imperativamente em código**, não pelo designer do VS (os `.resx` contêm apenas ícones) — explains the "campos fantasma" nas capturas.
- **`frmSurface16`** — janela principal. Duas eixos independentes: `LayerModes` (FOH / Master / Custom) e `FaderModes`. Cada camada é uma `List<int>` de IDs de console. Paging via `Tag` (offset da página).
- **`frmChannel`** — editor de strip: HA/phantom/phase, pan/balance, grupos, mute/DCA, EQ de 4 bandas + HPF/LPF, 2 compressores, sends, inserts, direct out, to-stereo/to-mono, 8 pontos de medição.
- **`frmConfig`** — 12 abas (Devices, Customize, Custom Layer, Fader Strip, Users, About…). Inclui seletor de dispositivos MIDI e controle do servidor TCP.
- **`frmGraphicEQ`** — GEQ de 31 bandas com orçamento de bandas e semântica de cor por rack.
- **`frmEFX`** — editor de rack de efeitos (48 tipos no seletor).
- **`frmBounce`** — easter egg: **Pong** onde a bola "nada" na curva do PEQ e os faders viram as raquetes.
- **`frmTrial` / `frmUpdate` / `frmConfirm`** — fluxo de licença e atualização.
- **Tema/cores**: `Skin.cs` + `RGBHSL.cs`, 8 presets, `CTabControl` owner-drawn.
- **`AirControls.dll`**: `AirFader`, `AirKnob`, `AirMeter`, `AirButton`, `AirPanel`, `AirLabel`, `Values` (enum `knobTypes` com 26 membros).

---

## 10. Vulnerabilidades / observações de segurança

1. **Licença quebrável (Crítico)** — checksum não-secreto, sentinel `36500` para perpetual, `rand` controlável pelo cliente. Verificado com keygen funcional.
2. **Backdoor de desenvolvimento (Crítico)** — `Adam`/`ANY` → chave perpetual de 99 clientes, embarcada no binário em texto claro.
3. **Servidor TCP sem TLS (Alto)** — `IPAddress.Any` na porta 12301, tráfego protobuf em claro; qualquer um na rede local pode capturar ou injetar comandos de mixagem.
4. **Credenciais fracas (Alto)** — `passHash` é MD5 simples, armazenado em texto claro (`Users.dat`) e comparado sem constante de tempo.
5. **Chave de licença vazando em HTTP (Alto)** — `ver.php?key=<chave>` e `register.php?k=<chave>` trafegam em cleartext, permitindo captura passiva por proxy/MITM.
6. **Trial reset trivial (Médio)** — `Settings.Default.Expired` é um flag local; o reset acontece automaticamente em upgrade de versão (`frmSurface16.cs:498`) e o usuário pode editar `AirFader.exe.config`.
7. **Zero cryptographic protection no protocolo (Médio)** — nada impede um cliente forjado de emitir `CMD_SENDPARAM` arbitrário.
8. **Identificação de máquina decorativa (Médio)** — CPUID/HDID não protegem a chave; `GlobalKey`/`GlobalUser` são applicationSettings e persistem por máquina (e são resetáveis).
9. **Upload de log com dados do usuário (Baixo)** — `Error.Log` é enviado ao vendor via POST HTTP, potencialmente contendo nomes de canais, topologias e identificadores de máquina.

---

## 11. Limites e confiança

- **Alta confiança:** arquitetura, framing do protocolo, algoritmo de licença (reimplementado e validado contra o validador original), backdoor, layout de `Meterdata.dat` — todos verificados por leitura direta do código decompilado e/ou execução do reimplementado.
- **Não verificado em runtime:** comportamento real contra hardware Yamaha (nenhum console conectado), semântica exata das respostas SysEx além das tabelas de decodificação, e o conteúdo dos `Users.dat`/logs existentes no ambiente.
- **Sem execução do app:** nenhuma análise dinâmica, tracing de API ou rede foi realizada.
- Os `.resx` são quase vazios porque a UI é construída em código — isso é uma característica do app, não perda de informação na decompilação.

---

## 12. Reprodução

```powershell
# Decompilar
dotnet ilspycmd.dll -p -o re\src\AirFader "C:\Program Files (x86)\air\AirFader\AirFader.exe"

# Gerar/validar chaves (o script não é publicado — ver seção 6.5)
python re\analysis\airfader_keygen.py
```
