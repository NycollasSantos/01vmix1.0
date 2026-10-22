using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AirFader;

internal enum UiLanguage
{
	English = 0,
	PortugueseBrazil = 1
}

/// <summary>
/// Minimal localization layer for 01vmix.
///
/// The original application is a WinForms app with every string hardcoded in
/// the designer files, so a full .resx based localization is not possible
/// without rewriting every form.  Instead the English text of every control is
/// captured at runtime and translated through a lookup table, which keeps the
/// patch small and allows switching the language at any time.
///
/// Because a lot of the labels are built while the window is running (faders,
/// effect knobs, the buttons of the "home" panel, ...) the translation cannot
/// be a one shot pass over the control tree: every control is hooked and every
/// text that shows up later is translated on the spot.
/// </summary>
internal static class Loc
{
	private static UiLanguage _language = UiLanguage.English;

	/// <summary>Guards against the recursion caused by writing a translated text back.</summary>
	private static bool _applying;

	/// <summary>The English text of every control that has been seen so far.</summary>
	private static readonly Dictionary<Control, string> originalText = new Dictionary<Control, string>();

	private static readonly Dictionary<string, string> ptBr = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		{ "01vmix does not need a license key.\r\n\r\nThis is the free, open-source build - all features are unlocked.", "O 01vmix n\u00e3o precisa de chave de licen\u00e7a.\r\n\r\nEsta \u00e9 a vers\u00e3o gratuita e de c\u00f3digo aberto - todos os recursos est\u00e3o liberados." },
		{ "Don't see your console? Go back and configure the Yamaha DME Network Driver first.\r\nVisit the AirFader.com FAQ for instructions.", "N\u00e3o encontra o seu console? Volte e configure primeiro o Yamaha DME Network Driver.\r\nVisite a FAQ do AirFader.com para ver as instru\u00e7\u00f5es." },
		{ "If 01vmix will not sync via IP after recent network problems or a software crash, the DME MIDI driver may not be responding. Click below to attempt to restart. (Will not affect the console.)", "Se o 01vmix n\u00e3o sincronizar via IP depois de problemas de rede ou de uma falha do programa, o driver MIDI do DME pode n\u00e3o estar respondendo. Clique abaixo para tentar reinici\u00e1-lo. (N\u00e3o afeta o console.)" },
		{ "Please enter your Owner Name and License Key.\r\nBoth must match your email receipt exactly!\r\n\r\nTo get a Free Trial key, visit http://airfader.com/trial", "Informe o Nome do Propriet\u00e1rio e a Chave de Licen\u00e7a.\r\nOs dois devem ser id\u00eanticos ao seu recibo de e-mail!\r\n\r\nPara obter uma chave de teste gr\u00e1tis, acesse http://airfader.com/trial" },
		{ "This will try to restart a crashed DME driver on this computer.\n01vmix will exit. Proceed?", "Isso vai tentar reiniciar um driver do DME travado neste computador.\nO 01vmix ser\u00e1 fechado. Continuar?" },
		{ "Changes to permissions are enforced immediately", "Altera\u00e7\u00f5es nas permiss\u00f5es s\u00e3o aplicadas imediatamente" },
		{ "Move a DIFFERENT fader to select Player 2's controller.", "Mova um fader DIFERENTE para selecionar o controlador do Jogador 2." },
		{ "Move any fader to select Player 1's controller.", "Mova qualquer fader para selecionar o controlador do Jogador 1." },
		{ "Click user above to edit permissions", "Clique no usu\u00e1rio acima para editar as permiss\u00f5es" },
		{ "WARN: Subzero index (", "AVISO: \u00edndice abaixo de zero (" },
		{ "WARN: Overlimit index (", "AVISO: \u00edndice acima do limite (" },
		{ "in FuzzyFind. Please notify support!", "em FuzzyFind. Avise o suporte!" },
		{ "CRITICAL ERROR - Please Report (Critical.Log)", "ERRO CR\u00cdTICO - Envie o relat\u00f3rio (Critical.Log)" },
		{ "Successfully reported problem to developer. Thank you!", "Problema relatado ao desenvolvedor com sucesso. Obrigado!" },
		{ "Could not upload problem report. Please contact support.", "N\u00e3o foi poss\u00edvel enviar o relat\u00f3rio do problema. Contate o suporte." },
		{ "Upload failed. Are you connected to the Internet right now?", "Falha no envio. Voc\u00ea est\u00e1 conectado \u00e0 Internet neste momento?" },
		{ "ERROR: Max Channels is zero. Attempting to fix...", "ERRO: Max Channels \u00e9 zero. Tentando corrigir..." },
		{ "ERROR: Console.MaxInputs is zero. Attempting to fix... Notify Support!", "ERRO: Console.MaxInputs \u00e9 zero. Tentando corrigir... Avise o suporte!" },
		{ "ERROR: Channel Range is zero. Attempting to fix... Notify Support!", "ERRO: Channel Range \u00e9 zero. Tentando corrigir... Avise o suporte!" },
		{ "ERROR: NumFaders is Zero in frmSurface16_resize!", "ERRO: NumFaders \u00e9 zero em frmSurface16_resize!" },
		{ "Unknown command: ", "Comando desconhecido: " },
		{ "Creating OutputDevice on ", "Criando OutputDevice em " },
		{ "Starting InputDevice", "Iniciando InputDevice" },
		{ "Missing critical data! [MeterData]", "Faltam dados cr\u00edticos! [MeterData]" },
		{ "Error loading! [MeterData]", "Erro ao carregar! [MeterData]" },
		{ "Invalid Channel #", "Canal # inv\u00e1lido" },
		{ "Problem in Base36to64()", "Problema em Base36to64()" },
		{ "Problem in Base64to36()", "Problema em Base64to36()" },
		{ "Problem in Base10to32()", "Problema em Base10to32()" },
		{ "Problem in Base10()", "Problema em Base10()" },
		{ "Static Error", "Erro Est\u00e1tico" },
		{ "Engine Error", "Erro do Engine" },
		{ "Upload successful.", "Envio bem-sucedido." },
		{ "Restart DME", "Reiniciar DME" },
		{ "***", "***" },
		{ "About", "Sobre" },
		{ "Active Button", "Bot\u00e3o Ativo" },
		{ "Add ->", "Adicionar ->" },
		{ "Add User", "Adicionar Usu\u00e1rio" },
		{ "Add/Edit User", "Adicionar/Editar Usu\u00e1rio" },
		{ "Again!", "De novo!" },
		{ "All", "Todos" },
		{ "Apply", "Aplicar" },
		{ "Are you sure?", "Tem certeza?" },
		{ "Ask to confirm Exit", "Confirmar antes de sair" },
		{ "Auto-Connect when 01vmix starts", "Conectar automaticamente ao iniciar o 01vmix" },
		{ "Available Channels", "Canais dispon\u00edveis" },
		{ "Available Controls", "Controles dispon\u00edveis" },
		{ "Back", "Fundo" },
		{ "BUS Assign", "Atribuir BUS" },
		{ "Bypass", "Ignorar" },
		{ "Cancel", "Cancelar" },
		{ "Channel #", "Canal #" },
		{ "Channel Editor", "Editor de Canal" },
		{ "Channel Off", "Canal Desligado" },
		{ "Channel On", "Canal Ligado" },
		{ "Channel Params", "Par\u00e2metros do Canal" },
		{ "Channel Strip", "Strip de Canal" },
		{ "Check for Updates", "Verificar Atualiza\u00e7\u00f5es" },
		{ "Choose Dynamics #1 Type", "Escolher o Tipo de Din\u00e2mica #1" },
		{ "Choose Dynamics #2 Type", "Escolher o Tipo de Din\u00e2mica #2" },
		{ "Choose Effects Type", "Escolher o Tipo de Efeito" },
		{ "CLEAR", "LIMPAR" },
		{ "Close", "Fechar" },
		{ "Closing Conn...", "Fechando a conex\u00e3o..." },
		{ "Color Theme", "Tema de Cores" },
		{ "Config", "Config" },
		{ "Configuration", "Configura\u00e7\u00e3o" },
		{ "Connect", "Conectar" },
		{ "Connected", "Conectado" },
		{ "Connecting...", "Conectando..." },
		{ "Connection Bad...", "Conex\u00e3o ruim..." },
		{ "Connection Error.", "Erro de conex\u00e3o." },
		{ "Connection Failed", "Falha na conex\u00e3o" },
		{ "Connection Lost!", "Conex\u00e3o perdida!" },
		{ "Connection Restored!", "Conex\u00e3o restaurada!" },
		{ "Console Type", "Tipo de Console" },
		{ "Copy", "Copiar" },
		{ "Custom", "Personalizado" },
		{ "Custom Fader Layer", "Camada de Faders Personalizada" },
		{ "Custom Layer", "Camada Personalizada" },
		{ "Customize", "Personalizar" },
		{ "Debug Log Level", "N\u00edvel de Log de Depura\u00e7\u00e3o" },
		{ "Debug Settings", "Configura\u00e7\u00f5es de Depura\u00e7\u00e3o" },
		{ "Debug...", "Depura\u00e7\u00e3o..." },
		{ "Delay", "Delay" },
		{ "Direct\nOut", "Sa\u00edda\nDireta" },
		{ "Delete User", "Excluir Usu\u00e1rio" },
		{ "Device Not Found", "Dispositivo N\u00e3o Encontrado" },
		{ "Device not found.", "Dispositivo n\u00e3o encontrado." },
		{ "Devices", "Dispositivos" },
		{ "Devices opened.", "Dispositivos abertos." },
		{ "Disable", "Desativar" },
		{ "Done.", "Conclu\u00eddo." },
		{ "Down", "Para baixo" },
		{ "Duck", "Duck" },
		{ "Edit User '", "Editar Usu\u00e1rio '" },
		{ "Effect\nType", "Efeito\nTipo" },
		{ "Effects", "Efeitos" },
		{ "Email Address", "Endere\u00e7o de E-mail" },
		{ "Enable Meters when connecting", "Ativar os medidores ao conectar" },
		{ "Enabled", "Ativado" },
		{ "EQ Att", "Atenua\u00e7\u00e3o de EQ" },
		{ "EQ On", "EQ Ligado" },
		{ "Exit", "Sair" },
		{ "Expand", "Expandir" },
		{ "Fader Strip", "Strip de Faders" },
		{ "Fader Tails", "Ponta dos Faders" },
		{ "Faders", "Faders" },
		{ "Fine\nMode", "Modo\nFino" },
		{ "Flat", "Plano" },
		{ "Flat\nAll", "Plano\nTudo" },
		{ "Flat\nSel", "Plano\nSel" },
		{ "Frame Colors", "Cores da Moldura" },
		{ "Frequency", "Frequ\u00eancia" },
		{ "Full Screen", "Tela Cheia" },
		{ "Full\nScreen", "Tela\nCheia" },
		{ "Gain", "Ganho" },
		{ "Game Over!", "Fim de Jogo!" },
		{ "Get Text", "Obter Texto" },
		{ "Getting key...", "Obtendo a chave..." },
		{ "GO", "VAI" },
		{ "Graphic EQ", "EQ Gr\u00e1fico" },
		{ "GraphicEQ", "EQ Gr\u00e1fico" },
		{ "High EQ Type", "Tipo de EQ High" },
		{ "High-contrast color scheme", "Esquema de cores de alto contraste" },
		{ "Home", "In\u00edcio" },
		{ "HPF On", "HPF Ligado" },
		{ "I already have one!", "J\u00e1 tenho um!" },
		{ "Inactive Button", "Bot\u00e3o Inativo" },
		{ "Info", "Informa\u00e7\u00f5es" },
		{ "Input Device", "Dispositivo de Entrada" },
		{ "Inputs", "Entradas" },
		{ "Insert\nOn", "Insert\nLigado" },
		{ "Intercom Flasher", "Pisca Intercom" },
		{ "IP Address", "Endere\u00e7o IP" },
		{ "Jump", "Saltar" },
		{ "Jump to Channel", "Ir para o Canal" },
		{ "Keyboard", "Teclado" },
		{ "License Key", "Chave de Licen\u00e7a" },
		{ "Link", "Vincular" },
		{ "Link To Console\nChannel Select", "Vincular \u00e0 Sele\u00e7\u00e3o de\nCanal do Console" },
		{ "Link\n", "Vincular\n" },
		{ "Load", "Carregar" },
		{ "Lock", "Bloquear" },
		{ "Low EQ Type", "Tipo de EQ Low" },
		{ "Main", "Principal" },
		{ "Main Mix (FOH)", "Mix Principal (FOH)" },
		{ "Master Fader Section", "Se\u00e7\u00e3o do Fader Master" },
		{ "Master Faders", "Faders Master" },
		{ "Matrix on Faders", "Matrix nos Faders" },
		{ "Matrix Permissions", "Permiss\u00f5es de Matrix" },
		{ "Maximum Channels", "M\u00e1ximo de Canais" },
		{ "Meter", "Medidor" },
		{ "Mix +\nMatrix", "Mix +\nMatrix" },
		{ "Mix Permissions", "Permiss\u00f5es de Mix" },
		{ "Mode", "Modo" },
		{ "Mono", "Mono" },
		{ "MONO\n", "MONO\n" },
		{ "Mute Groups", "Grupos de Mute" },
		{ "New User", "Novo Usu\u00e1rio" },
		{ "New Window", "Nova Janela" },
		{ "No", "N\u00e3o" },
		{ "No Bands Avail", "Sem Bandas Dispon\u00edveis" },
		{ "No License Key", "Sem Chave de Licen\u00e7a" },
		{ "None", "Nenhum" },
		{ "Not Connected", "N\u00e3o Conectado" },
		{ "Number of Faders to Show", "N\u00famero de Faders a Mostrar" },
		{ "Off", "Desligado" },
		{ "Ok", "Ok" },
		{ "Ok!", "Ok!" },
		{ "On", "Ligado" },
		{ "Open Log File", "Abrir o Arquivo de Log" },
		{ "Open Log Folder", "Abrir a Pasta de Log" },
		{ "Option", "Op\u00e7\u00e3o" },
		{ "Options", "Op\u00e7\u00f5es" },
		{ "Output Device", "Dispositivo de Sa\u00edda" },
		{ "Owner Name", "Nome do Propriet\u00e1rio" },
		{ "Param", "Par\u00e2metro" },
		{ "Password", "Senha" },
		{ "Paste", "Colar" },
		{ "Peak", "Pico" },
		{ "Per-Channel Permissions", "Permiss\u00f5es por Canal" },
		{ "Permissions", "Permiss\u00f5es" },
		{ "Phase", "Fase" },
		{ "Phase\nInvert", "Fase\nInvertida" },
		{ "Player 1", "Jogador 1" },
		{ "Player 2", "Jogador 2" },
		{ "Port", "Porta" },
		{ "Preparing...", "Preparando..." },
		{ "Press ON to Pause", "Pressione LIGADO para Pausar" },
		{ "Register", "Registrar" },
		{ "Registering...", "Registrando..." },
		{ "Registration", "Registro" },
		{ "Remote Control Users", "Usu\u00e1rios de Controle Remoto" },
		{ "Remote Version", "Vers\u00e3o Remota" },
		{ "Rename Scene #", "Renomear a Cena #" },
		{ "Report", "Relat\u00f3rio" },
		{ "Request Trial", "Solicitar Teste" },
		{ "Restart DME Driver", "Reiniciar o Driver do DME" },
		{ "'s Permissions", "'s Permiss\u00f5es" },
		{ "Save", "Salvar" },
		{ "Save To Scene #", "Salvar na Cena #" },
		{ "SCENE", "CENA" },
		{ "Scene List", "Lista de Cenas" },
		{ "Screen Locked", "Tela Bloqueada" },
		{ "Select Input Channel", "Selecionar o Canal de Entrada" },
		{ "Select Location to Save Scene...", "Selecione o Local para Salvar a Cena..." },
		{ "Select Mix/Matrix/Stereo Bus", "Selecionar o Bus Mix/Matrix/Stereo" },
		{ "Select Scene to Load...", "Selecione a Cena para Carregar..." },
		{ "Send", "Enviar" },
		{ "Send Log Now", "Enviar o Log Agora" },
		{ "Send Message:", "Enviar Mensagem:" },
		{ "Send To All", "Enviar para Todos" },
		{ "Sends On Faders", "Sends nos Faders" },
		{ "Server", "Servidor" },
		{ "Server Configuration", "Configura\u00e7\u00e3o do Servidor" },
		{ "Show \"Fader tails\"", "Mostrar a \"ponta dos faders\"" },
		{ "Show Master (Stereo) fader", "Mostrar o fader Master (Stereo)" },
		{ "SPACE", "ESPA\u00c7O" },
		{ "Start", "Iniciar" },
		{ "Start in Full Screen mode", "Iniciar em modo Tela Cheia" },
		{ "Start Server when 01vmix Starts", "Iniciar o Servidor quando o 01vmix iniciar" },
		{ "Starting DME...", "Iniciando o DME..." },
		{ "Status", "Status" },
		{ "Stay On Top", "Sempre no Topo" },
		{ "Stereo", "Est\u00e9reo" },
		{ "Stop", "Parar" },
		{ "Stopping DME...", "Parando o DME..." },
		{ "Text", "Texto" },
		{ "Tools", "Ferramentas" },
		{ "Type", "Tipo" },
		{ "UDF Key for Intercom Flasher", "Tecla UDF para o Pisca Intercom" },
		{ "UnLock", "Desbloquear" },
		{ "Unregister", "Cancelar Registro" },
		{ "Unregistering...", "Cancelando o registro..." },
		{ "Up", "Para cima" },
		{ "User Name", "Nome de Usu\u00e1rio" },
		{ "Users", "Usu\u00e1rios" },
		{ "Window Options", "Op\u00e7\u00f5es de Janela" },
		{ "Yes", "Sim" },
		{ "123 - Demo Scene", "123 - Cena Demo" },
		{ "31 Bands Avail", "31 Bandas Dispon\u00edveis" },
		{ "Language", "Idioma" },
		{ "Language / Idioma", "Language / Idioma" },

		// --- labels that are created while the window is running ----------------
		{ "STATUS", "ESTADO" },
		{ "UDF", "UDF" },
		{ "Cue", "Cue" },
		{ "+48v", "+48v" },
		{ "Sends", "Envios" },
		{ "Matrix", "Matrix" },
		{ "Master", "Master" },
		{ "DCA", "DCA" },
		{ "Rack", "Rack" },
		{ "Meters", "Medidores" },
		{ "Sync", "Sincronizar" },
		{ "Load\nScene", "Carregar\nCena" },
		{ "Save\nScene", "Salvar\nCena" },
		{ "Prev\nScene", "Cena\nAnterior" },
		{ "Next\nScene", "Cena\nPr\u00f3xima" },
		{ "Mute 1", "Mute 1" },
		{ "Mute 2", "Mute 2" },
		{ "Mute 3", "Mute 3" },
		{ "Mute 4", "Mute 4" },
		{ "Mute 5", "Mute 5" },
		{ "Mute 6", "Mute 6" },
		{ "Mute 7", "Mute 7" },
		{ "Mute 8", "Mute 8" },
		{ "HA", "HA" },
		{ "Pan", "Pan" },
		{ "Output\nAtt", "Sa\u00edda\nAten." },
		{ "Output\nDelay", "Sa\u00edda\nAtraso" },
		{ "Lamp", "Luz" },
		{ "Bounce", "Bounce" },
		{ "[On]", "[Ligado]" },
		{ "Normal\nScreen", "Normal\nTela" },
		{ "2TR", "2TR" },
		{ "1 - 8", "1 - 8" },

		// --- effect parameters (EFX) --------------------------------------------
		{ "Mix", "Mix" },
		{ "HPF", "HPF" },
		{ "LPF", "LPF" },
		{ "Gate Lvl", "Niv. Gate" },
		{ "Attack", "Ataque" },
		{ "Hold", "Espera" },
		{ "Decay", "Decaimento" },
		{ "Rev Time", "Tempo Rev" },
		{ "Ini.Dly", "Atraso Inicial" },
		{ "Hi.Ratio", "Raz\u00e3o Alta" },
		{ "Lo.Ratio", "Raz\u00e3o Baixa" },
		{ "Diff.", "Dif." },
		{ "Density", "Densidade" },
		{ "E/R Dly", "Atraso E/R" },
		{ "E/R Bal.", "Bal. E/R" },

		// --- configuration ------------------------------------------------------
		{ "\"LED\" Labels", "\"LED\" R\u00f3tulos" },
		{ "\"Click anywhere\" fader mode", "Modo de fader \"clique em qualquer lugar\"" },
		{ "<- Remove", "<- Remover" },
		{ "Clear", "Limpar" },
		{ "Drop", "Soltar" },
		{ "Sweep", "Varredura" },
		{ "Copyright \u00a9 2010-2015, Molten Logic LLC -- Design/Programming: Adam Ellsworth", "Copyright \u00a9 2010-2015, Molten Logic LLC -- Design/Programa\u00e7\u00e3o: Adam Ellsworth" },
		{ "Copyright \u00a9 2012, Molten Logic LLC -- Design/Programming: Adam Ellsworth", "Copyright \u00a9 2012, Molten Logic LLC -- Design/Programa\u00e7\u00e3o: Adam Ellsworth" },
		{ "This computer program is protected by copyright law and international treaties. Unauthorized reproduction, distribution, or reverse-engineering is prohibited and may result in severe civil and criminal penalties.", "Este programa de computador \u00e9 protegido por leis de direitos autorais e tratados internacionais. A reprodu\u00e7\u00e3o, a distribui\u00e7\u00e3o ou a engenharia reversa n\u00e3o autorizadas s\u00e3o proibidas e podem resultar em san\u00e7\u00f5es civis e criminais graves." },
		{ "Server mode allows mobile devices (such as Android phones and tablets) to control the console remotely using the \"AirFader\" mobile app. Access and permissions are set on the \"Users\" tab.\n\nThe IP Address and Port Number below should be entered into AirFader's Setup screen on the mobile device. The mobile devices, server, and console must all be on the same network (unless advanced IP routing or NAT is used.)", "O modo servidor permite que dispositivos m\u00f3veis (como telefones e tablets Android) controlem o console remotamente usando o aplicativo m\u00f3vel \"AirFader\". O acesso e as permiss\u00f5es s\u00e3o definidos na aba \"Users\".\n\nO Endere\u00e7o IP e o N\u00famero de Porta abaixo devem ser informados na tela Setup do aplicativo AirFader no dispositivo m\u00f3vel. Os dispositivos m\u00f3veis, o servidor e o console devem estar todos na mesma rede (a menos que seja usado roteamento de IP avan\u00e7ado ou NAT.)" },

		// --- scene browser --------------------------------------------------------
		{ "Page\nDown", "P\u00e1gina\nAbaixo" },
		{ "Page\nUp", "P\u00e1gina\nAcima" }
	};

	/// <summary>The same table, but with the line endings normalized (CRLF -&gt; LF).</summary>
	private static readonly Dictionary<string, string> ptBrNormalized = BuildNormalizedTable(ptBr);

	/// <summary>Names shown in the language selector.</summary>
	internal static readonly string[] LanguageNames = { "English", "Portugu\u00eas (Brasil)" };

	/// <summary>Stable identifiers stored in the user settings.</summary>
	internal static readonly string[] LanguageIds = { "en", "pt-BR" };

	internal static UiLanguage Language
	{
		get { return _language; }
	}

	internal static string LanguageId
	{
		get { return LanguageIds[(int)_language]; }
	}

	private static Dictionary<string, string> BuildNormalizedTable(Dictionary<string, string> source)
	{
		Dictionary<string, string> table = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> pair in source)
		{
			string key = Normalize(pair.Key);
			if (!table.ContainsKey(key))
			{
				table[key] = pair.Value;
			}
		}
		return table;
	}

	/// <summary>Removes the difference between CRLF and LF line endings.</summary>
	private static string Normalize(string text)
	{
		if (text == null)
		{
			return null;
		}
		if (text.IndexOf('\r') >= 0)
		{
			text = text.Replace("\r\n", "\n").Replace('\r', '\n');
		}
		return text;
	}

	/// <summary>Converts the stored identifier into a language, defaulting to English.</summary>
	internal static UiLanguage ParseLanguage(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			for (int i = 0; i < LanguageIds.Length; i++)
			{
				if (string.Equals(LanguageIds[i], id, StringComparison.OrdinalIgnoreCase))
				{
					return (UiLanguage)i;
				}
			}
		}
		return UiLanguage.English;
	}

	/// <summary>Selects the language and re-translates every control already on screen.</summary>
	internal static void SetLanguage(UiLanguage language)
	{
		_language = language;
		Purge();
		List<KeyValuePair<Control, string>> snapshot = new List<KeyValuePair<Control, string>>(originalText);
		foreach (KeyValuePair<Control, string> pair in snapshot)
		{
			Control control = pair.Key;
			if (control == null || control.IsDisposed)
			{
				continue;
			}
			string translated = T(pair.Value);
			if (!string.Equals(control.Text, translated, StringComparison.Ordinal))
			{
				SetText(control, translated);
			}
		}
	}

	/// <summary>Translates a single string (returns it unchanged when there is no translation).</summary>
	internal static string T(string text)
	{
		if (text == null || _language != UiLanguage.PortugueseBrazil)
		{
			return text;
		}
		string result;
		if (ptBr.TryGetValue(text, out result))
		{
			return result;
		}
		string normalized = Normalize(text);
		if (!string.Equals(normalized, text, StringComparison.Ordinal) && ptBrNormalized.TryGetValue(normalized, out result))
		{
			return result;
		}
		return text;
	}

	/// <summary>
	/// Translates a control and all of its children.  Controls that hold user
	/// input (text boxes, combo boxes, ...) are left untouched.
	///
	/// The controls are also hooked, so a text that is assigned later (at
	/// runtime) is translated as soon as it shows up.
	/// </summary>
	internal static void Apply(Control root)
	{
		if (root == null)
		{
			return;
		}
		Hook(root);
		root.ControlAdded -= OnControlAdded;
		root.ControlAdded += OnControlAdded;
	}

	private static void Hook(Control control)
	{
		if (control == null)
		{
			return;
		}
		if (IsTranslatable(control))
		{
			control.TextChanged -= OnTextChanged;
			control.TextChanged += OnTextChanged;
			Capture(control);
		}
		foreach (Control child in control.Controls)
		{
			Hook(child);
		}
		control.ControlAdded -= OnControlAdded;
		control.ControlAdded += OnControlAdded;
	}

	private static void OnControlAdded(object sender, ControlEventArgs e)
	{
		Apply(e.Control);
	}

	/// <summary>Translates a text that was assigned to a control while the app is running.</summary>
	private static void OnTextChanged(object sender, EventArgs e)
	{
		if (_applying)
		{
			return;
		}
		Control control = sender as Control;
		if (control == null || !IsTranslatable(control))
		{
			return;
		}
		string current = control.Text;
		string original;
		if (!originalText.TryGetValue(control, out original))
		{
			original = current;
			originalText[control] = original;
		}
		else if (string.Equals(current, T(original), StringComparison.Ordinal))
		{
			// This is the translation that was written before - nothing to do.
			return;
		}
		else
		{
			// A new text was assigned at runtime: it becomes the new original.
			original = current;
			originalText[control] = original;
		}
		string translated = T(original);
		if (!string.Equals(current, translated, StringComparison.Ordinal))
		{
			SetText(control, translated);
		}
	}

	/// <summary>Translates a control and remembers its original (English) text.</summary>
	private static void Capture(Control control)
	{
		string current = control.Text;
		if (string.IsNullOrEmpty(current))
		{
			return;
		}
		string original;
		if (!originalText.TryGetValue(control, out original))
		{
			original = current;
			originalText[control] = original;
		}
		string translated = T(original);
		if (!string.Equals(current, translated, StringComparison.Ordinal))
		{
			SetText(control, translated);
		}
	}

	/// <summary>Writes a text without triggering the translation hook again.</summary>
	private static void SetText(Control control, string text)
	{
		_applying = true;
		try
		{
			control.Text = text;
		}
		finally
		{
			_applying = false;
		}
	}

	private static bool IsTranslatable(Control control)
	{
		if (control is TextBoxBase)
		{
			return false;
		}
		if (control is ComboBox || control is ListBox || control is NumericUpDown || control is DateTimePicker)
		{
			return false;
		}
		return true;
	}

	/// <summary>Drops the references of the controls that were closed already.</summary>
	private static void Purge()
	{
		List<Control> dead = null;
		foreach (KeyValuePair<Control, string> pair in originalText)
		{
			if (pair.Key == null || pair.Key.IsDisposed)
			{
				if (dead == null)
				{
					dead = new List<Control>();
				}
				dead.Add(pair.Key);
			}
		}
		if (dead != null)
		{
			foreach (Control control in dead)
			{
				originalText.Remove(control);
			}
		}
	}

	// --- message boxes ---------------------------------------------------------

	internal static DialogResult Msg(string text)
	{
		return MessageBox.Show(T(text));
	}

	internal static DialogResult Msg(string text, string caption)
	{
		return MessageBox.Show(T(text), T(caption));
	}

	internal static DialogResult Msg(string text, string caption, MessageBoxButtons buttons)
	{
		return MessageBox.Show(T(text), T(caption), buttons);
	}

	internal static DialogResult Msg(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
	{
		return MessageBox.Show(T(text), T(caption), buttons, icon);
	}

	internal static DialogResult Msg(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
	{
		return MessageBox.Show(T(text), T(caption), buttons, icon, defaultButton);
	}
}
