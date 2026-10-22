using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace AirFader.Properties;

[CompilerGenerated]
[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
internal sealed class Settings : ApplicationSettingsBase
{
	private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

	public static Settings Default => defaultInstance;

	[DefaultSettingValue("Coral")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public Color c_channel_on
	{
		get
		{
			return (Color)this["c_channel_on"];
		}
		set
		{
			this["c_channel_on"] = value;
		}
	}

	[DebuggerNonUserCode]
	[DefaultSettingValue("Gray")]
	[UserScopedSetting]
	public Color c_channel_off
	{
		get
		{
			return (Color)this["c_channel_off"];
		}
		set
		{
			this["c_channel_off"] = value;
		}
	}

	[DefaultSettingValue("Lime")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public Color c_button_active
	{
		get
		{
			return (Color)this["c_button_active"];
		}
		set
		{
			this["c_button_active"] = value;
		}
	}

	[DefaultSettingValue("WhiteSmoke")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public Color c_button_inactive
	{
		get
		{
			return (Color)this["c_button_inactive"];
		}
		set
		{
			this["c_button_inactive"] = value;
		}
	}

	[DebuggerNonUserCode]
	[DefaultSettingValue("8")]
	[UserScopedSetting]
	public int faderCount
	{
		get
		{
			return (int)this["faderCount"];
		}
		set
		{
			this["faderCount"] = value;
		}
	}

	[DebuggerNonUserCode]
	[DefaultSettingValue("11")]
	[UserScopedSetting]
	public int flashUDF
	{
		get
		{
			return (int)this["flashUDF"];
		}
		set
		{
			this["flashUDF"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("50, 50, 50")]
	public Color c_background
	{
		get
		{
			return (Color)this["c_background"];
		}
		set
		{
			this["c_background"] = value;
		}
	}

	[DebuggerNonUserCode]
	[UserScopedSetting]
	[DefaultSettingValue("Right")]
	public string layerPos
	{
		get
		{
			return (string)this["layerPos"];
		}
		set
		{
			this["layerPos"] = value;
		}
	}

	[DefaultSettingValue("Lime")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public Color EQActive
	{
		get
		{
			return (Color)this["EQActive"];
		}
		set
		{
			this["EQActive"] = value;
		}
	}

	[DefaultSettingValue("LightSteelBlue")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public Color EQInactive
	{
		get
		{
			return (Color)this["EQInactive"];
		}
		set
		{
			this["EQInactive"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("64")]
	public int maxChannels
	{
		get
		{
			return (int)this["maxChannels"];
		}
		set
		{
			this["maxChannels"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("")]
	[DebuggerNonUserCode]
	public string MIDI_Input
	{
		get
		{
			return (string)this["MIDI_Input"];
		}
		set
		{
			this["MIDI_Input"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("")]
	[DebuggerNonUserCode]
	public string MIDI_Output
	{
		get
		{
			return (string)this["MIDI_Output"];
		}
		set
		{
			this["MIDI_Output"] = value;
		}
	}

	[DefaultSettingValue("")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public string LicenseKey
	{
		get
		{
			return (string)this["LicenseKey"];
		}
		set
		{
			this["LicenseKey"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("")]
	[DebuggerNonUserCode]
	public string LicenseUser
	{
		get
		{
			return (string)this["LicenseUser"];
		}
		set
		{
			this["LicenseUser"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("Yamaha LS9-32")]
	[DebuggerNonUserCode]
	public string ConsoleType
	{
		get
		{
			return (string)this["ConsoleType"];
		}
		set
		{
			this["ConsoleType"] = value;
		}
	}

	[DefaultSettingValue("0,1,2,3,72,73,74,98")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public string CustomLayer
	{
		get
		{
			return (string)this["CustomLayer"];
		}
		set
		{
			this["CustomLayer"] = value;
		}
	}

	[DefaultSettingValue("5")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public int BusMeter
	{
		get
		{
			return (int)this["BusMeter"];
		}
		set
		{
			this["BusMeter"] = value;
		}
	}

	[DefaultSettingValue("0")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public int ChannelMeter
	{
		get
		{
			return (int)this["ChannelMeter"];
		}
		set
		{
			this["ChannelMeter"] = value;
		}
	}

	[DebuggerNonUserCode]
	[DefaultSettingValue("96")]
	[UserScopedSetting]
	public string MasterSection
	{
		get
		{
			return (string)this["MasterSection"];
		}
		set
		{
			this["MasterSection"] = value;
		}
	}

	[DebuggerNonUserCode]
	[DefaultSettingValue("50,50,1000,600,Normal,0")]
	[UserScopedSetting]
	public string windowGEQ
	{
		get
		{
			return (string)this["windowGEQ"];
		}
		set
		{
			this["windowGEQ"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("50,50,1020,745,Normal,0")]
	public string windowMain
	{
		get
		{
			return (string)this["windowMain"];
		}
		set
		{
			this["windowMain"] = value;
		}
	}

	[DefaultSettingValue("50,50,808,634,Normal,0")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public string windowChannel
	{
		get
		{
			return (string)this["windowChannel"];
		}
		set
		{
			this["windowChannel"] = value;
		}
	}

	[DebuggerNonUserCode]
	[DefaultSettingValue("True")]
	[UserScopedSetting]
	public bool Upgraded
	{
		get
		{
			return (bool)this["Upgraded"];
		}
		set
		{
			this["Upgraded"] = value;
		}
	}

	[DefaultSettingValue("False")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public bool Expired
	{
		get
		{
			return (bool)this["Expired"];
		}
		set
		{
			this["Expired"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("50,50,700,450,Normal,0")]
	[DebuggerNonUserCode]
	public string windowScene
	{
		get
		{
			return (string)this["windowScene"];
		}
		set
		{
			this["windowScene"] = value;
		}
	}

	[DefaultSettingValue("0")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public int MIDI_Channel
	{
		get
		{
			return (int)this["MIDI_Channel"];
		}
		set
		{
			this["MIDI_Channel"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("1")]
	[DebuggerNonUserCode]
	public string Version
	{
		get
		{
			return (string)this["Version"];
		}
		set
		{
			this["Version"] = value;
		}
	}

	[DefaultSettingValue("500,100,800,600,Normal,0")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public string windowEFX
	{
		get
		{
			return (string)this["windowEFX"];
		}
		set
		{
			this["windowEFX"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("Label,Cue,Name,Mute,Fader")]
	[DebuggerNonUserCode]
	public string ChannelStrip
	{
		get
		{
			return (string)this["ChannelStrip"];
		}
		set
		{
			this["ChannelStrip"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("Black")]
	[DebuggerNonUserCode]
	public Color c_channel_on_fore
	{
		get
		{
			return (Color)this["c_channel_on_fore"];
		}
		set
		{
			this["c_channel_on_fore"] = value;
		}
	}

	[DefaultSettingValue("Black")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public Color c_channel_off_fore
	{
		get
		{
			return (Color)this["c_channel_off_fore"];
		}
		set
		{
			this["c_channel_off_fore"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("Black")]
	[DebuggerNonUserCode]
	public Color c_button_active_fore
	{
		get
		{
			return (Color)this["c_button_active_fore"];
		}
		set
		{
			this["c_button_active_fore"] = value;
		}
	}

	[DefaultSettingValue("Black")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public Color c_button_inactive_fore
	{
		get
		{
			return (Color)this["c_button_inactive_fore"];
		}
		set
		{
			this["c_button_inactive_fore"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("Lime")]
	[DebuggerNonUserCode]
	public Color c_led_fore
	{
		get
		{
			return (Color)this["c_led_fore"];
		}
		set
		{
			this["c_led_fore"] = value;
		}
	}

	[DefaultSettingValue("Black")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public Color c_led_back
	{
		get
		{
			return (Color)this["c_led_back"];
		}
		set
		{
			this["c_led_back"] = value;
		}
	}

	[DefaultSettingValue("WhiteSmoke")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public Color c_foreground
	{
		get
		{
			return (Color)this["c_foreground"];
		}
		set
		{
			this["c_foreground"] = value;
		}
	}

	[DefaultSettingValue("Original")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public string c_theme
	{
		get
		{
			return (string)this["c_theme"];
		}
		set
		{
			this["c_theme"] = value;
		}
	}

	[DefaultSettingValue("50, 50, 50")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public Color c_fader_back
	{
		get
		{
			return (Color)this["c_fader_back"];
		}
		set
		{
			this["c_fader_back"] = value;
		}
	}

	[DefaultSettingValue("WhiteSmoke")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public Color c_fader_fore
	{
		get
		{
			return (Color)this["c_fader_fore"];
		}
		set
		{
			this["c_fader_fore"] = value;
		}
	}

	[UserScopedSetting]
	[DefaultSettingValue("Coral")]
	[DebuggerNonUserCode]
	public Color c_fader_tail
	{
		get
		{
			return (Color)this["c_fader_tail"];
		}
		set
		{
			this["c_fader_tail"] = value;
		}
	}

	[DefaultSettingValue("12301")]
	[DebuggerNonUserCode]
	[UserScopedSetting]
	public int serverPort
	{
		get
		{
			return (int)this["serverPort"];
		}
		set
		{
			this["serverPort"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool serverAutoStart
	{
		get
		{
			return (bool)this["serverAutoStart"];
		}
		set
		{
			this["serverAutoStart"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool ConsoleFollow
	{
		get
		{
			return (bool)this["ConsoleFollow"];
		}
		set
		{
			this["ConsoleFollow"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool autoConnect
	{
		get
		{
			return (bool)this["autoConnect"];
		}
		set
		{
			this["autoConnect"] = value;
		}
	}

	[DefaultSettingValue("True")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public bool autoMeters
	{
		get
		{
			return (bool)this["autoMeters"];
		}
		set
		{
			this["autoMeters"] = value;
		}
	}

	[ApplicationScopedSetting]
	[DefaultSettingValue("")]
	[DebuggerNonUserCode]
	public string GlobalKey => (string)this["GlobalKey"];

	[DefaultSettingValue("")]
	[ApplicationScopedSetting]
	[DebuggerNonUserCode]
	public string GlobalUser => (string)this["GlobalUser"];

	[DefaultSettingValue("True")]
	[UserScopedSetting]
	[DebuggerNonUserCode]
	public bool confirmExit
	{
		get
		{
			return (bool)this["confirmExit"];
		}
		set
		{
			this["confirmExit"] = value;
		}
	}

	[DebuggerNonUserCode]
	[UserScopedSetting]
	[DefaultSettingValue("1")]
	public int debugLevel
	{
		get
		{
			return (int)this["debugLevel"];
		}
		set
		{
			this["debugLevel"] = value;
		}
	}

	[DebuggerNonUserCode]
	[UserScopedSetting]
		[DefaultSettingValue("pt-BR")]
		public string Language
	{
		get
		{
			return (string)this["Language"];
		}
		set
		{
			this["Language"] = value;
		}
	}
}
