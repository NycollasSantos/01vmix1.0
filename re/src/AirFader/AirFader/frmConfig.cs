using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using AirFader.Properties;
using Sanford.Multimedia.Midi;
using faderspeak;

namespace AirFader;

public class frmConfig : Form
{
	private class lbChanItem
	{
		public string name = "";

		public int channel = -1;

		public lbChanItem(string newName, int newChannel)
		{
			name = newName;
			channel = newChannel;
		}

		public override string ToString()
		{
			return name;
		}
	}

	public struct COPYDATASTRUCT
	{
		public IntPtr dwData;

		public int cbData;

		[MarshalAs(UnmanagedType.LPStr)]
		public string lpData;
	}

	public const int WM_COPYDATA = 74;

	private frmSurface16 parentForm;

	private bool redoLayout;

	private bool dataflag;

	private UserRecord tempUser = new UserRecord();

	private System.Windows.Forms.Timer timerConnList;

	private IContainer components;

	private ListBox lbInputDev;

	private ListBox lbOutputDev;

	private Label lblInDev;

	private Label lblOutDev;

	private TabControl tabConfig;

	private TabPage tabDevices;

	private TabPage tabCustomize;

	private CheckBox checkBox1;

	private Label label1;

	private ComboBox cbUDKLamp;

	private Button btnSave;

	private Button btnCancel;

	private CheckBox checkBox2;

	private TabPage tabAbout;

	private ComboBox cbNumFaders;

	private Button btnFixDME;

	private TextBox txtMeter;

	private TextBox txtQ;

	private TextBox txtFreq;

	private Label label4;

	private Label label3;

	private Label label2;

	private Button btnSweep;

	private CheckBox chkGO;

	private Label lblNumFaders;

	private ComboBox cbMaxChannel;

	private Label label5;

	private Label label6;

	private TextBox txtGain;

	private Label lblBrand;

	private Label lblOwner;

	private Label label9;

	private Label lblVersion;

	private Label lblProduct;

	private Button btnRegister;

	private Label lblKey;

	private Button btnUpdate;

	private Label label7;

	private Label label8;

	private Label label10;

	private ComboBox cbConsoleType;

	private TabPage tabCustomLayer;

	private Button btnCLRemove;

	private Button btnCLAdd;

	private ListBox lbCustomLayer;

	private ListBox lbChannels;

	private Label label11;

	private Label lbCLDest;

	private Button btnCLDown;

	private Button btnCLUp;

	private ComboBox cbMasterSection;

	private Label label12;

	private Button btnScan;

	private TabPage tabFaderStrip;

	private Label label29;

	private Label label30;

	private Button btnCSDown;

	private Button btnCSUp;

	private Button btnCSRemove;

	private Button btnCSAdd;

	private ListBox lbStrip;

	private ListBox lbControls;

	private TabPage tabPage1;

	private Button button1;

	private Label label13;

	private ComboBox comboBox1;

	private Label label14;

	private Button button2;

	private ListBox listBox1;

	private Label label15;

	private ListBox listBox2;

	private Label label16;

	private TabPage tabPage2;

	private ComboBox comboBox2;

	private Label label17;

	private ComboBox comboBox3;

	private Label label18;

	private Label label19;

	private ComboBox comboBox4;

	private CheckBox checkBox6;

	private CheckBox checkBox7;

	private CheckBox checkBox8;

	private CheckBox checkBox9;

	private Label label20;

	private ComboBox comboBox5;

	private CheckBox checkBox10;

	private TabPage tabPage3;

	private Label label21;

	private Label label22;

	private Button button3;

	private Button button4;

	private Button button5;

	private Button button6;

	private ListBox listBox3;

	private ListBox listBox4;

	private TabPage tabPage4;

	private Label label23;

	private Button button7;

	private Label label24;

	private Button button8;

	private Label label25;

	private Label label26;

	private Label label27;

	private Label label28;

	private Label lblBrand2;

	private Label label31;

	private Button btnActiveFore;

	private ColorDialog colorPick;

	private Button btnActiveBack;

	private Button btnChOnBack;

	private Button btnChOnFore;

	private Label label32;

	private Button btnInactiveBack;

	private Button btnInactiveFore;

	private Label label34;

	private Button btnChOffBack;

	private Button btnChOffFore;

	private Label label33;

	private Button btnLEDColorBack;

	private Button btnLEDColorFore;

	private Label label35;

	private Button btnBackground;

	private Label label36;

	private Button btnForeground;

	private Label label37;

	private ComboBox cbTheme;

	private Button btnFaderBack;

	private Button btnFaderFore;

	private Label label38;

	private Button btnFaderTail;

	private Label label39;

	private Label label40;

	private Label label41;

	private TabPage tabUsers;

	private Label label42;

	private Button btnNewUser;

	private Button btnUserSave;

	private ListBox lbUsers;

	private CheckBox chkMix0;

	private CheckBox chkEnabled;

	private TextBox txtPassword;

	private TextBox txtUserName;

	private Label label43;

	private Label label44;

	private CheckBox chkChannel;

	private CheckBox chkMainMix;

	private CheckBox chkMix15;

	private CheckBox chkMix14;

	private CheckBox chkMix13;

	private CheckBox chkMix12;

	private CheckBox chkMix11;

	private CheckBox chkMix10;

	private CheckBox chkMix9;

	private CheckBox chkMix8;

	private CheckBox chkMix7;

	private CheckBox chkMix6;

	private CheckBox chkMix5;

	private CheckBox chkMix4;

	private CheckBox chkMix3;

	private CheckBox chkMix2;

	private CheckBox chkMix1;

	private Label lblAddEditUser;

	private Label lblPermissions;

	private Label label45;

	private Label label46;

	private Button btnDelUser;

	private TabPage tabServer;

	private Label label48;

	private Label label47;

	private Label lblServerIP;

	private Label label50;

	private Label label49;

	private TextBox txtServerPort;

	private CheckBox chkServerAutoStart;

	private Button btnServerStart;

	private Label lblServerStatus;

	private Label label51;

	private Button btnNone;

	private Button btnPermAll;

	private TextBox txtMessage;

	private Button btnSendMsg;

	private Label label52;

	private CheckBox chkRack;

	private CheckBox chkMaster;

	private Label label53;

	private CheckBox chkAutoConnect;

	private ListView lvConnections;

	private ColumnHeader colId;

	private ColumnHeader colName;

	private ColumnHeader colIp;

	private ColumnHeader colClient;

	private CheckBox chkChanSTIN;

	private CheckBox chkChan7;

	private CheckBox chkChan6;

	private CheckBox chkChan5;

	private CheckBox chkChan4;

	private CheckBox chkChan3;

	private CheckBox chkChan2;

	private CheckBox chkChan1;

	private CheckBox chkChan0;

	private Label label55;

	private Label label54;

	private Button btnServerDrop;

	private Button btnServerDisable;

	private ColumnHeader colSince;

	private Button btnSendMsgSel;

	private Button btnMsgClear;

	private CheckBox chkAutoMeters;

	private CheckBox chkMix23;

	private CheckBox chkMix22;

	private CheckBox chkMix21;

	private CheckBox chkMix20;

	private CheckBox chkMix19;

	private CheckBox chkMix18;

	private CheckBox chkMix17;

	private CheckBox chkMix16;

	private Label label56;

	private CheckBox chkChan8;

	private CheckBox chkMtx7;

	private CheckBox chkMtx6;

	private CheckBox chkMtx5;

	private CheckBox chkMtx4;

	private CheckBox chkMtx3;

	private CheckBox chkMtx2;

	private CheckBox chkMtx1;

	private CheckBox chkMtx0;

	private CheckBox chkConfirmExit;

	private ComboBox cbLanguage;

	private Label labelLang;

	private bool initializing;

	public frmConfig(frmSurface16 opener)
	{
		parentForm = opener;
		CenterToParent();
		InitializeComponent();
	}

	private void frmConfig_Load(object sender, EventArgs e)
	{
		try
		{
			initializing = true;
			tabAbout.BackColor = Color.FromArgb(50, 50, 50);
			cbNumFaders.Items.Clear();
			for (int i = 1; i <= 32; i++)
			{
				cbNumFaders.Items.Add(i.ToString());
			}
			if (Settings.Default.flashUDF == -1)
			{
				cbUDKLamp.SelectedIndex = cbUDKLamp.FindStringExact("None");
			}
			else
			{
				cbUDKLamp.SelectedIndex = cbUDKLamp.FindStringExact((Settings.Default.flashUDF + 1).ToString());
			}
			chkConfirmExit.Checked = Settings.Default.confirmExit;
			cbMaxChannel.SelectedIndex = cbMaxChannel.FindStringExact(Settings.Default.maxChannels.ToString());
			cbNumFaders.SelectedIndex = cbNumFaders.FindStringExact(Settings.Default.faderCount.ToString());
			cbConsoleType.SelectedIndex = cbConsoleType.FindStringExact(Settings.Default.ConsoleType);
			if (Settings.Default.autoConnect)
			{
				chkAutoConnect.Checked = true;
			}
			else
			{
				chkAutoConnect.Checked = false;
			}
			if (Settings.Default.autoMeters)
			{
				chkAutoMeters.Checked = true;
			}
			else
			{
				chkAutoMeters.Checked = false;
			}
			switch (Settings.Default.MasterSection.Replace(" ", ""))
			{
			case "96":
			case "Stereo":
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("Stereo");
				break;
			case "98":
			case "Mono":
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("Mono");
				break;
			case "96,98":
			case "Both":
			case "Mono+ST":
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("Mono+ST");
				break;
			case "Monitor":
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("Monitor");
				break;
			case "ST+Monitor":
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("ST+Monitor");
				break;
			case "Monitor+ST":
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("Monitor+ST");
				break;
			default:
				cbMasterSection.SelectedIndex = cbMasterSection.FindStringExact("None");
				break;
			}
			for (int j = 0; j < InputDevice.DeviceCount; j++)
			{
				lbInputDev.Items.Add(InputDevice.GetDeviceCapabilities(j).name);
				if (InputDevice.GetDeviceCapabilities(j).name == Settings.Default.MIDI_Input)
				{
					lbInputDev.SelectedIndex = lbInputDev.Items.Count - 1;
				}
			}
			for (int k = 0; k < OutputDeviceBase.DeviceCount; k++)
			{
				lbOutputDev.Items.Add(OutputDeviceBase.GetDeviceCapabilities(k).name);
				if (OutputDeviceBase.GetDeviceCapabilities(k).name == Settings.Default.MIDI_Output)
				{
					lbOutputDev.SelectedIndex = lbOutputDev.Items.Count - 1;
				}
			}
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
				string text = "";
				text = ((item2 != -1) ? ("[" + MixEngine.chData[item2].hardname + "] " + MixEngine.chData[item2].name) : "Not Assigned");
				lbCustomLayer.Items.Add(new lbChanItem(text, item2));
			}
			lbStrip.Items.Clear();
			List<string> list = new List<string>();
			list.Add("Label");
			list.Add("Cue");
			list.Add("HA");
			list.Add("Phantom");
			list.Add("Pan");
			list.Add("Name");
			list.Add("Mute");
			list.Add("Fader");
			list.Add("Info");
			List<string> list2 = list;
			List<string> list3 = new List<string>(Settings.Default.ChannelStrip.Split(','));
			foreach (string item3 in list3)
			{
				lbStrip.Items.Add(item3);
			}
			foreach (string item4 in list2)
			{
				if (!list3.Contains(item4))
				{
					lbControls.Items.Add(item4);
				}
			}
			string[] array = new string[9] { "Custom", "Original", "Dark Modern", "Contrast", "Fire", "Ice", "Yamawho", "SemiSonus", "Mackish" };
			cbTheme.Items.Clear();
			string[] array2 = array;
			foreach (string item in array2)
			{
				cbTheme.Items.Add(item);
			}
			cbTheme.Text = Settings.Default.c_theme;
			syncColors(Settings.Default.c_theme);
			MixEngine.checkSerialV2(MixEngine.getLicenseUser(), MixEngine.getLicenseKey(), quiet: true);
			lblProduct.Text = Application.ProductName;
			if (MixEngine.LICENSING_ENABLED)
			{
				if (MixEngine.licenseClients == 1)
				{
					lblVersion.Text = Application.ProductVersion + " (Standard)";
				}
				else
				{
					lblVersion.Text = Application.ProductVersion + " (Server)";
				}
				lblOwner.Text = MixEngine.getLicenseUser();
				lblKey.Text = MixEngine.getLicenseKey();
				btnRegister.Visible = true;
			}
			else
			{
				// Open-source build: there is no license to register.
				lblVersion.Text = Application.ProductVersion;
				lblOwner.Text = MixEngine.OPEN_SOURCE_OWNER;
				lblKey.Text = MixEngine.OPEN_SOURCE_KEY;
				btnRegister.Visible = false;
			}
			initServerConfig();
			initUserList();
			btnNewUser_Click(null, null);
			cbLanguage.SelectedIndex = (int)Loc.Language;
			Loc.Apply(this);
			initializing = false;
		}
		catch (Exception ex)
		{
			initializing = false;
			MixEngine.logError("frmConfig_Load()", "UI", ex.ToString());
		}
	}

	private void cbLanguage_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			if (initializing || cbLanguage.SelectedIndex < 0)
			{
				return;
			}
			Loc.SetLanguage((UiLanguage)cbLanguage.SelectedIndex);
			Settings.Default.Language = Loc.LanguageId;
			Settings.Default.Save();
			Loc.Apply(this);
			if (parentForm != null)
			{
				Loc.Apply(parentForm);
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("cbLanguage_SelectedIndexChanged()", "UI", ex.ToString());
		}
	}

	private void btnSave_Click(object sender, EventArgs e)
	{
		try
		{
			Settings.Default.MIDI_Input = lbInputDev.Text;
			Settings.Default.MIDI_Output = lbOutputDev.Text;
			int result = 0;
			if (int.TryParse(txtServerPort.Text, out result))
			{
				Settings.Default.serverPort = result;
			}
			Settings.Default.serverAutoStart = chkServerAutoStart.Checked;
			Settings.Default.autoConnect = chkAutoConnect.Checked;
			Settings.Default.autoMeters = chkAutoMeters.Checked;
			Settings.Default.c_theme = cbTheme.Text;
			Settings.Default.c_button_active = btnActiveBack.BackColor;
			Settings.Default.c_button_inactive = btnInactiveBack.BackColor;
			Settings.Default.c_button_active_fore = btnActiveFore.BackColor;
			Settings.Default.c_button_inactive_fore = btnInactiveFore.BackColor;
			Settings.Default.c_channel_on = btnChOnBack.BackColor;
			Settings.Default.c_channel_off = btnChOffBack.BackColor;
			Settings.Default.c_channel_on_fore = btnChOnFore.BackColor;
			Settings.Default.c_channel_off_fore = btnChOffFore.BackColor;
			Settings.Default.c_led_back = btnLEDColorBack.BackColor;
			Settings.Default.c_led_fore = btnLEDColorFore.BackColor;
			Settings.Default.c_fader_back = btnFaderBack.BackColor;
			Settings.Default.c_fader_fore = btnFaderFore.BackColor;
			Settings.Default.c_fader_tail = btnFaderTail.BackColor;
			Settings.Default.c_background = btnBackground.BackColor;
			Settings.Default.c_foreground = btnForeground.BackColor;
			if (Settings.Default.ConsoleType != cbConsoleType.Text)
			{
				Settings.Default.ConsoleType = cbConsoleType.Text;
				MixEngine.setConsoleType(Settings.Default.ConsoleType);
				redoLayout = true;
			}
			if (Settings.Default.faderCount != int.Parse(cbNumFaders.SelectedItem.ToString()))
			{
				Settings.Default.faderCount = int.Parse(cbNumFaders.SelectedItem.ToString());
				redoLayout = true;
			}
			if (Settings.Default.maxChannels != int.Parse(cbMaxChannel.SelectedItem.ToString()))
			{
				Settings.Default.maxChannels = int.Parse(cbMaxChannel.SelectedItem.ToString());
				redoLayout = true;
			}
			if (cbUDKLamp.SelectedItem.ToString() == "None")
			{
				Settings.Default.flashUDF = -1;
			}
			else
			{
				Settings.Default.flashUDF = int.Parse(cbUDKLamp.SelectedItem.ToString()) - 1;
			}
			Settings.Default.confirmExit = chkConfirmExit.Checked;
			parentForm.layerCustom.Clear();
			string text = "";
			if (lbCustomLayer.Items.Count > 0)
			{
				foreach (lbChanItem item2 in lbCustomLayer.Items)
				{
					parentForm.layerCustom.Add(item2.channel);
					text = text + item2.channel + ",";
				}
				text = text.Substring(0, text.Length - 1);
			}
			Settings.Default.CustomLayer = text;
			if (parentForm.layerMode == frmSurface16.LayerModes.Custom)
			{
				redoLayout = true;
			}
			string[] array = new string[lbStrip.Items.Count];
			for (int i = 0; i < lbStrip.Items.Count; i++)
			{
				array[i] = lbStrip.Items[i].ToString();
			}
			if (Settings.Default.ChannelStrip != string.Join(",", array))
			{
				Settings.Default.ChannelStrip = string.Join(",", array);
				parentForm.arrLayout.Clear();
				string[] array2 = array;
				foreach (string item in array2)
				{
					parentForm.arrLayout.Add(item);
				}
				redoLayout = true;
			}
			if (Settings.Default.MasterSection != cbMasterSection.Text)
			{
				switch (cbMasterSection.Text)
				{
				case "Stereo":
					Settings.Default.MasterSection = "Stereo";
					break;
				case "Mono":
					Settings.Default.MasterSection = "Mono";
					break;
				case "Mono+ST":
					Settings.Default.MasterSection = "Mono+ST";
					break;
				case "Monitor":
					Settings.Default.MasterSection = "Monitor";
					break;
				case "ST+Monitor":
					Settings.Default.MasterSection = "ST+Monitor";
					break;
				case "Monitor+ST":
					Settings.Default.MasterSection = "Monitor+ST";
					break;
				default:
					Settings.Default.MasterSection = "";
					break;
				}
				MixEngine.OnAction("MasterSection", 0);
			}
			else if (redoLayout)
			{
				parentForm.numFaders_Change();
			}
			Settings.Default.Save();
			parentForm.frmConfigH = null;
			Close();
			Dispose();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnSave_Click()", "UI", ex.ToString());
		}
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		try
		{
			parentForm.frmConfigH = null;
			Close();
			Dispose();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCancel_Click()", "UI", ex.ToString());
		}
	}

	private void btnStopDME_Click(object sender, EventArgs e)
	{
		if (Loc.Msg("This will try to restart a crashed DME driver on this computer.\n01vmix will exit. Proceed?", "Restart DME", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			btnFixDME.Enabled = false;
			btnFixDME.Text = "Closing Conn...";
			Application.DoEvents();
			try
			{
				Application.DoEvents();
				Thread.Sleep(TimeSpan.FromSeconds(1.0));
			}
			catch (Exception ex)
			{
				MixEngine.logError("StopDME()", "OutEvent", ex.ToString());
			}
			btnFixDME.Text = "Stopping DME...";
			Application.DoEvents();
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				if (process.ProcessName.StartsWith("DME-N Network Driver"))
				{
					process.Kill();
				}
			}
			btnFixDME.Text = "Preparing...";
			Application.DoEvents();
			Thread.Sleep(TimeSpan.FromSeconds(5.0));
			btnFixDME.Text = "Starting DME...";
			Process process2 = new Process();
			process2.StartInfo.FileName = "c:\\windows\\system32\\DME-N Network Driver.exe";
			process2.Start();
			Application.DoEvents();
			Thread.Sleep(TimeSpan.FromSeconds(5.0));
			btnFixDME.Text = "Done.";
			parentForm.isGlobalShutdown = true;
			parentForm.Close();
			Close();
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("initConnectionList Error: " + ex2.Message);
		}
	}

	[DllImport("User32.dll")]
	public static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	private void frmConfig_FormClosing(object sender, FormClosingEventArgs e)
	{
		try
		{
			parentForm.frmConfigH = null;
		}
		catch (Exception ex)
		{
			MixEngine.logError("frmConfig_FormClosing()", "UI", ex.ToString());
		}
	}

	private void btnRegister_Click(object sender, EventArgs e)
	{
		try
		{
			if (!MixEngine.LICENSING_ENABLED)
			{
				frmConfirm.sayOK("This is the free, open-source build of 01vmix.\nNo license key is required - all features are unlocked.", "Registration", MixEngine.hMainWindow);
				return;
			}
			frmUpdate frmUpdate2 = new frmUpdate();
			frmUpdate2.ShowDialog();
			if (MixEngine.licenseClients == 1)
			{
				lblVersion.Text = Application.ProductVersion + " (Standard)";
			}
			else
			{
				lblVersion.Text = Application.ProductVersion + " (Server)";
			}
			lblOwner.Text = MixEngine.getLicenseUser();
			lblKey.Text = MixEngine.getLicenseKey();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnRegister_Click()", "UI", ex.ToString());
		}
	}

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

	private void btnUpdate_Click(object sender, EventArgs e)
	{
		string text = "";
		try
		{
			btnUpdate.Text = "Connecting...";
			btnUpdate.Enabled = false;
			if (!MixEngine.LICENSING_ENABLED)
			{
				// The original update service is tied to the vendor's license
				// database and downloads the commercial build. This build is
				// maintained separately, so no online update is performed.
				frmConfirm.sayOK("Online updates are not available for this build.\nPlease use the source repository to get the latest release.", "Update Check");
				btnUpdate.Text = "Check for Updates";
				btnUpdate.Enabled = true;
				return;
			}
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

	private void lblBrand_DoubleClick(object sender, EventArgs e)
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

	private void btnCLAdd_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbChannels.SelectedItems.Count <= 0)
			{
				return;
			}
			int num = lbCustomLayer.SelectedIndex;
			if (num == -1)
			{
				num = lbCustomLayer.Items.Count;
			}
			foreach (lbChanItem selectedItem in lbChannels.SelectedItems)
			{
				lbCustomLayer.Items.Insert(num++, selectedItem);
			}
			lbChannels.Focus();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCLAdd_Click()", "UI", ex.ToString());
		}
	}

	private void btnCLRemove_Click(object sender, EventArgs e)
	{
		try
		{
			while (lbCustomLayer.SelectedItems.Count > 0)
			{
				lbCustomLayer.Items.Remove(lbCustomLayer.SelectedItem);
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCLRemove_Click()", "UI", ex.ToString());
		}
	}

	private void btnCLUp_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbCustomLayer.SelectedItems.Count > 0 && lbCustomLayer.SelectedIndex > 0)
			{
				lbChanItem item = (lbChanItem)lbCustomLayer.SelectedItem;
				int selectedIndex = lbCustomLayer.SelectedIndex;
				lbCustomLayer.Items.RemoveAt(selectedIndex);
				lbCustomLayer.Items.Insert(selectedIndex - 1, item);
				lbCustomLayer.SelectedIndex = selectedIndex - 1;
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCLUP_Click()", "UI", ex.ToString());
		}
	}

	private void btnCLDown_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbCustomLayer.SelectedItems.Count > 0 && lbCustomLayer.SelectedIndex < lbCustomLayer.Items.Count - 1)
			{
				lbChanItem item = (lbChanItem)lbCustomLayer.SelectedItem;
				int selectedIndex = lbCustomLayer.SelectedIndex;
				lbCustomLayer.Items.RemoveAt(selectedIndex);
				lbCustomLayer.Items.Insert(selectedIndex + 1, item);
				lbCustomLayer.SelectedIndex = selectedIndex + 1;
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCLDown_Click()", "UI", ex.ToString());
		}
	}

	private void lbChannels_DoubleClick(object sender, EventArgs e)
	{
		btnCLAdd_Click(null, EventArgs.Empty);
	}

	private void lbCustomLayer_DoubleClick(object sender, EventArgs e)
	{
		btnCLRemove_Click(null, EventArgs.Empty);
	}

	private void btnScan_Click(object sender, EventArgs e)
	{
		StringBuilder stringBuilder = new StringBuilder(12);
		GetWindowText((IntPtr)792644, stringBuilder, 12);
		btnScan.Text = stringBuilder.ToString();
	}

	private void btnCSAdd_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbControls.SelectedItems.Count > 0)
			{
				int num = lbStrip.SelectedIndex;
				if (num == -1)
				{
					num = lbStrip.Items.Count;
				}
				while (lbControls.SelectedItems.Count > 0)
				{
					lbStrip.Items.Insert(num++, lbControls.SelectedItem);
					lbControls.Items.Remove(lbControls.SelectedItem);
				}
				lbControls.Focus();
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCSAdd_Click()", "UI", ex.ToString());
		}
	}

	private void btnCSRemove_Click(object sender, EventArgs e)
	{
		try
		{
			while (lbStrip.SelectedItems.Count > 0)
			{
				lbControls.Items.Add(lbStrip.SelectedItem);
				lbStrip.Items.Remove(lbStrip.SelectedItem);
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCSRemove_Click()", "UI", ex.ToString());
		}
	}

	private void btnCSUp_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbStrip.SelectedItems.Count > 0 && lbStrip.SelectedIndex > 0)
			{
				string item = (string)lbStrip.SelectedItem;
				int selectedIndex = lbStrip.SelectedIndex;
				lbStrip.Items.RemoveAt(selectedIndex);
				lbStrip.Items.Insert(selectedIndex - 1, item);
				lbStrip.SelectedIndex = selectedIndex - 1;
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCSUp_Click()", "UI", ex.ToString());
		}
	}

	private void btnCSDown_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbStrip.SelectedItems.Count > 0 && lbStrip.SelectedIndex < lbStrip.Items.Count - 1)
			{
				string item = (string)lbStrip.SelectedItem;
				int selectedIndex = lbStrip.SelectedIndex;
				lbStrip.Items.RemoveAt(selectedIndex);
				lbStrip.Items.Insert(selectedIndex + 1, item);
				lbStrip.SelectedIndex = selectedIndex + 1;
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnCSDown_Click()", "UI", ex.ToString());
		}
	}

	private void lbControls_DoubleClick(object sender, EventArgs e)
	{
		btnCSAdd_Click(null, EventArgs.Empty);
	}

	private void lbStrip_DoubleClick(object sender, EventArgs e)
	{
		btnCSRemove_Click(null, EventArgs.Empty);
	}

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

	private Color HexColor(string h)
	{
		return ColorTranslator.FromHtml(h);
	}

	private void syncColors(string theme)
	{
		try
		{
			switch (theme)
			{
			case "Original":
				btnActiveBack.BackColor = Color.Lime;
				btnActiveFore.BackColor = Color.Black;
				btnInactiveBack.BackColor = Color.WhiteSmoke;
				btnInactiveFore.BackColor = Color.Black;
				btnChOffBack.BackColor = Color.Gray;
				btnChOffFore.BackColor = Color.Black;
				btnChOnBack.BackColor = Color.Coral;
				btnChOnFore.BackColor = Color.Black;
				btnForeground.BackColor = Color.WhiteSmoke;
				btnBackground.BackColor = Color.FromArgb(50, 50, 50);
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = Color.Lime;
				btnFaderBack.BackColor = Color.FromArgb(50, 50, 50);
				btnFaderFore.BackColor = Color.WhiteSmoke;
				btnFaderTail.BackColor = Color.Coral;
				break;
				case "Dark Modern":
				btnActiveBack.BackColor = Color.FromArgb(26, 188, 156);
				btnActiveFore.BackColor = Color.WhiteSmoke;
				btnInactiveBack.BackColor = Color.FromArgb(42, 54, 68);
				btnInactiveFore.BackColor = Color.WhiteSmoke;
				btnChOffBack.BackColor = Color.FromArgb(53, 68, 84);
				btnChOffFore.BackColor = Color.WhiteSmoke;
				btnChOnBack.BackColor = Color.FromArgb(26, 188, 156);
				btnChOnFore.BackColor = Color.WhiteSmoke;
				btnForeground.BackColor = Color.WhiteSmoke;
				btnBackground.BackColor = Color.FromArgb(18, 25, 34);
				btnLEDColorBack.BackColor = Color.FromArgb(24, 33, 44);
				btnLEDColorFore.BackColor = Color.FromArgb(83, 224, 192);
				btnFaderBack.BackColor = Color.FromArgb(31, 42, 56);
				btnFaderFore.BackColor = Color.WhiteSmoke;
				btnFaderTail.BackColor = Color.FromArgb(26, 188, 156);
				break;
			case "Contrast":
				btnActiveBack.BackColor = Color.Yellow;
				btnActiveFore.BackColor = Color.Black;
				btnInactiveBack.BackColor = Color.Blue;
				btnInactiveFore.BackColor = Color.White;
				btnChOffBack.BackColor = Color.Red;
				btnChOffFore.BackColor = Color.White;
				btnChOnBack.BackColor = Color.Blue;
				btnChOnFore.BackColor = Color.White;
				btnForeground.BackColor = Color.White;
				btnBackground.BackColor = Color.Black;
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = Color.Lime;
				btnFaderBack.BackColor = Color.Black;
				btnFaderFore.BackColor = Color.White;
				btnFaderTail.BackColor = Color.Yellow;
				break;
			case "Fire":
				btnActiveBack.BackColor = HexColor("#FFF60B");
				btnActiveFore.BackColor = Color.Black;
				btnInactiveBack.BackColor = HexColor("#FF6100");
				btnInactiveFore.BackColor = HexColor("#FFFFCC");
				btnChOffBack.BackColor = Color.Gray;
				btnChOffFore.BackColor = Color.Black;
				btnChOnBack.BackColor = HexColor("#FF6100");
				btnChOnFore.BackColor = Color.Black;
				btnForeground.BackColor = HexColor("#FFF60B");
				btnBackground.BackColor = HexColor("#4D0600");
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = HexColor("#FF6100");
				btnFaderBack.BackColor = HexColor("#4D0600");
				btnFaderFore.BackColor = Color.Yellow;
				btnFaderTail.BackColor = HexColor("#FFF60B");
				break;
			case "Ice":
				btnActiveBack.BackColor = HexColor("#B5E1FF");
				btnActiveFore.BackColor = Color.Black;
				btnInactiveBack.BackColor = HexColor("#6681FF");
				btnInactiveFore.BackColor = HexColor("#B5E1FF");
				btnChOffBack.BackColor = Color.Gray;
				btnChOffFore.BackColor = Color.Black;
				btnChOnBack.BackColor = HexColor("#9E66FF");
				btnChOnFore.BackColor = Color.Black;
				btnForeground.BackColor = HexColor("#B5E1FF");
				btnBackground.BackColor = HexColor("#1A2C96");
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = HexColor("#1A2C96");
				btnFaderBack.BackColor = HexColor("#4EB8FF");
				btnFaderFore.BackColor = HexColor("#1A2C96");
				btnFaderTail.BackColor = HexColor("#6681FF");
				break;
			case "Yamawho":
				btnActiveBack.BackColor = Color.Lime;
				btnActiveFore.BackColor = Color.Black;
				btnInactiveBack.BackColor = Color.WhiteSmoke;
				btnInactiveFore.BackColor = Color.Black;
				btnChOffBack.BackColor = Color.Gray;
				btnChOffFore.BackColor = Color.Black;
				btnChOnBack.BackColor = Color.Coral;
				btnChOnFore.BackColor = Color.Black;
				btnForeground.BackColor = Color.WhiteSmoke;
				btnBackground.BackColor = Color.FromArgb(50, 50, 50);
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = Color.Lime;
				btnFaderBack.BackColor = Color.FromArgb(50, 50, 50);
				btnFaderFore.BackColor = Color.WhiteSmoke;
				btnFaderTail.BackColor = Color.DarkGray;
				break;
			case "SemiSonus":
				btnActiveBack.BackColor = HexColor("#FAAF8F");
				btnActiveFore.BackColor = Color.Black;
				btnInactiveBack.BackColor = HexColor("#C2CDE2");
				btnInactiveFore.BackColor = HexColor("#001133");
				btnChOffBack.BackColor = HexColor("#E2C2CD");
				btnChOffFore.BackColor = Color.Black;
				btnChOnBack.BackColor = HexColor("#C2E2CD");
				btnChOnFore.BackColor = Color.Black;
				btnForeground.BackColor = HexColor("#CCD1D5");
				btnBackground.BackColor = HexColor("#102051");
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = HexColor("#00FF33");
				btnFaderBack.BackColor = HexColor("#E0E0E2");
				btnFaderFore.BackColor = HexColor("#242425");
				btnFaderTail.BackColor = HexColor("#FAAF8F");
				break;
			case "Mackish":
				btnActiveBack.BackColor = HexColor("#AF405F");
				btnActiveFore.BackColor = Color.WhiteSmoke;
				btnInactiveBack.BackColor = HexColor("#C4C4C9");
				btnInactiveFore.BackColor = Color.White;
				btnChOffBack.BackColor = HexColor("#F94045");
				btnChOffFore.BackColor = Color.Black;
				btnChOnBack.BackColor = HexColor("#898D99");
				btnChOnFore.BackColor = Color.WhiteSmoke;
				btnBackground.BackColor = HexColor("#515153");
				btnForeground.BackColor = HexColor("#EAF2E5");
				btnLEDColorBack.BackColor = Color.Black;
				btnLEDColorFore.BackColor = HexColor("#3BBE18");
				btnFaderBack.BackColor = HexColor("#CCD5DA");
				btnFaderFore.BackColor = HexColor("#28252C");
				btnFaderTail.BackColor = HexColor("#AF405F");
				break;
			case "Custom":
				btnActiveBack.BackColor = Settings.Default.c_button_active;
				btnInactiveBack.BackColor = Settings.Default.c_button_inactive;
				btnActiveFore.BackColor = Settings.Default.c_button_active_fore;
				btnInactiveFore.BackColor = Settings.Default.c_button_inactive_fore;
				btnChOnBack.BackColor = Settings.Default.c_channel_on;
				btnChOffBack.BackColor = Settings.Default.c_channel_off;
				btnChOnFore.BackColor = Settings.Default.c_channel_on_fore;
				btnChOffFore.BackColor = Settings.Default.c_channel_off_fore;
				btnLEDColorBack.BackColor = Settings.Default.c_led_back;
				btnLEDColorFore.BackColor = Settings.Default.c_led_fore;
				btnFaderBack.BackColor = Settings.Default.c_fader_back;
				btnFaderFore.BackColor = Settings.Default.c_fader_fore;
				btnFaderTail.BackColor = Settings.Default.c_fader_tail;
				btnBackground.BackColor = Settings.Default.c_background;
				btnForeground.BackColor = Settings.Default.c_foreground;
				break;
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("syncColors()", "UI", ex.ToString());
		}
	}

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

	private void initServerConfig()
	{
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
			string text = "";
			IPAddress[] array = hostAddresses;
			foreach (IPAddress iPAddress in array)
			{
				if (!IPAddress.IsLoopback(iPAddress) && iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					text = text + iPAddress.ToString() + "\n";
				}
			}
			lblServerIP.Text = text;
			txtServerPort.Text = Settings.Default.serverPort.ToString();
			if (Settings.Default.serverAutoStart)
			{
				chkServerAutoStart.Checked = true;
			}
			else
			{
				chkServerAutoStart.Checked = false;
			}
			if (MixEngine.faderServer.isOnline)
			{
				lblServerStatus.BackColor = Color.Green;
				lblServerStatus.ForeColor = Color.White;
				lblServerStatus.Text = "Online";
				btnServerStart.Text = "Stop";
			}
			else
			{
				lblServerStatus.BackColor = Color.Red;
				lblServerStatus.ForeColor = Color.White;
				lblServerStatus.Text = "Offline";
				btnServerStart.Text = "Start";
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("initServerConfig()", "UI", ex.ToString());
		}
	}

	private void initConnectionList()
	{
		try
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			for (int i = 0; i < MixEngine.faderServer.workerSockets.Count; i++)
			{
				dictionary.Add(MixEngine.faderServer.workerSockets[i].clientNumber, i);
			}
			foreach (ListViewItem item in lvConnections.Items)
			{
				int key = (int)item.Tag;
				if (dictionary.ContainsKey(key))
				{
					TimeSpan timeSpan = DateTime.Now.Subtract(MixEngine.faderServer.workerSockets[dictionary[key]].dtConnected);
					item.SubItems[4].Text = Math.Truncate(timeSpan.TotalMinutes) + ":" + timeSpan.Seconds.ToString("D2");
					dictionary.Remove(key);
				}
				else
				{
					item.Remove();
				}
			}
			foreach (int key2 in dictionary.Keys)
			{
				FaderService.SocketPacket socketPacket = MixEngine.faderServer.workerSockets[dictionary[key2]];
				UserRecord userRecord = MixEngine.faderServer.findUser(socketPacket.userID);
				if (userRecord != null)
				{
					ListViewItem listViewItem2 = new ListViewItem(socketPacket.clientNumber.ToString());
					listViewItem2.Tag = socketPacket.clientNumber;
					if (userRecord == null)
					{
						listViewItem2.SubItems.Add("(None)");
					}
					else
					{
						listViewItem2.SubItems.Add(userRecord.userName);
					}
					listViewItem2.SubItems.Add(((IPEndPoint)socketPacket.currentSocket.RemoteEndPoint).Address.ToString());
					listViewItem2.SubItems.Add(socketPacket.clientVersion);
					TimeSpan timeSpan2 = DateTime.Now.Subtract(socketPacket.dtConnected);
					listViewItem2.SubItems.Add(Math.Truncate(timeSpan2.TotalMinutes) + ":" + timeSpan2.Seconds.ToString("D2"));
					lvConnections.Items.Add(listViewItem2);
				}
			}
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("initConnectionList Error: " + ex.Message);
		}
	}

	private void initUserList()
	{
		try
		{
			lbUsers.Items.Clear();
			lbUsers.DisplayMember = "userName";
			lbUsers.ValueMember = "userId";
			foreach (UserRecord item in MixEngine.faderServer.listUsers())
			{
				lbUsers.Items.Add(item);
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("initUserList()", "UI", ex.ToString());
		}
	}

	private void btnNewUser_Click(object sender, EventArgs e)
	{
		try
		{
			chkEnabled.Checked = true;
			chkChannel.Checked = true;
			chkMainMix.Checked = true;
			chkMaster.Checked = true;
			chkRack.Checked = true;
			chkMix0.Checked = true;
			chkMix1.Checked = true;
			chkMix2.Checked = true;
			chkMix3.Checked = true;
			chkMix4.Checked = true;
			chkMix5.Checked = true;
			chkMix6.Checked = true;
			chkMix7.Checked = true;
			chkMix8.Checked = true;
			chkMix9.Checked = true;
			chkMix10.Checked = true;
			chkMix11.Checked = true;
			chkMix12.Checked = true;
			chkMix13.Checked = true;
			chkMix14.Checked = true;
			chkMix15.Checked = true;
			chkMix16.Checked = true;
			chkMix17.Checked = true;
			chkMix18.Checked = true;
			chkMix19.Checked = true;
			chkMix20.Checked = true;
			chkMix21.Checked = true;
			chkMix22.Checked = true;
			chkMix23.Checked = true;
			chkMtx0.Checked = true;
			chkMtx1.Checked = true;
			chkMtx2.Checked = true;
			chkMtx3.Checked = true;
			chkMtx4.Checked = true;
			chkMtx5.Checked = true;
			chkMtx6.Checked = true;
			chkMtx7.Checked = true;
			chkChan0.Checked = true;
			chkChan1.Checked = true;
			chkChan2.Checked = true;
			chkChan3.Checked = true;
			chkChan4.Checked = true;
			chkChan5.Checked = true;
			chkChan6.Checked = true;
			chkChan7.Checked = true;
			chkChan8.Checked = true;
			chkChanSTIN.Checked = true;
			txtUserName.Text = "";
			txtPassword.Text = "";
			lblPermissions.Text = "Permissions";
			lblAddEditUser.Text = "Add User";
			lbUsers.SelectedItems.Clear();
			tempUser = new UserRecord();
			tempUser.userId = 0u;
			txtUserName.Focus();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnNewUser_Click()", "UI", ex.ToString());
		}
	}

	private void btnUserSave_Click(object sender, EventArgs e)
	{
		try
		{
			if (txtUserName.Text.Length == 0)
			{
				frmConfirm.sayFail("Username can't be blank.", "User Not Saved");
				return;
			}
			if (tempUser.userId == 0 && MixEngine.faderServer.findUser(txtUserName.Text) != null)
			{
				frmConfirm.sayFail("User Name already exists!", "User Not Saved");
				return;
			}
			MD5 md5Hash = MD5.Create();
			tempUser.userName = txtUserName.Text;
			if (txtPassword.Text != "********")
			{
				tempUser.passHash = MixEngine.faderServer.GetMd5Hash(md5Hash, txtPassword.Text);
			}
			while (tempUser.accessMix.Count < 24)
			{
				tempUser.accessMix.Add(item: false);
			}
			while (tempUser.accessMatrix.Count < 8)
			{
				tempUser.accessMatrix.Add(item: false);
			}
			while (tempUser.accessChannels.Count < 10)
			{
				tempUser.accessChannels.Add(item: true);
			}
			tempUser.active = chkEnabled.Checked;
			tempUser.accessMainMix = chkMainMix.Checked;
			tempUser.accessChannel = chkChannel.Checked;
			tempUser.accessMasters = chkMaster.Checked;
			tempUser.accessRack = chkRack.Checked;
			tempUser.accessMix[0] = chkMix0.Checked;
			tempUser.accessMix[1] = chkMix1.Checked;
			tempUser.accessMix[2] = chkMix2.Checked;
			tempUser.accessMix[3] = chkMix3.Checked;
			tempUser.accessMix[4] = chkMix4.Checked;
			tempUser.accessMix[5] = chkMix5.Checked;
			tempUser.accessMix[6] = chkMix6.Checked;
			tempUser.accessMix[7] = chkMix7.Checked;
			tempUser.accessMix[8] = chkMix8.Checked;
			tempUser.accessMix[9] = chkMix9.Checked;
			tempUser.accessMix[10] = chkMix10.Checked;
			tempUser.accessMix[11] = chkMix11.Checked;
			tempUser.accessMix[12] = chkMix12.Checked;
			tempUser.accessMix[13] = chkMix13.Checked;
			tempUser.accessMix[14] = chkMix14.Checked;
			tempUser.accessMix[15] = chkMix15.Checked;
			tempUser.accessMix[16] = chkMix16.Checked;
			tempUser.accessMix[17] = chkMix17.Checked;
			tempUser.accessMix[18] = chkMix18.Checked;
			tempUser.accessMix[19] = chkMix19.Checked;
			tempUser.accessMix[20] = chkMix20.Checked;
			tempUser.accessMix[21] = chkMix21.Checked;
			tempUser.accessMix[22] = chkMix22.Checked;
			tempUser.accessMix[23] = chkMix23.Checked;
			tempUser.accessMatrix[0] = chkMtx0.Checked;
			tempUser.accessMatrix[1] = chkMtx1.Checked;
			tempUser.accessMatrix[2] = chkMtx2.Checked;
			tempUser.accessMatrix[3] = chkMtx3.Checked;
			tempUser.accessMatrix[4] = chkMtx4.Checked;
			tempUser.accessMatrix[5] = chkMtx5.Checked;
			tempUser.accessMatrix[6] = chkMtx6.Checked;
			tempUser.accessMatrix[7] = chkMtx7.Checked;
			tempUser.accessChannels[0] = chkChan0.Checked;
			tempUser.accessChannels[1] = chkChan1.Checked;
			tempUser.accessChannels[2] = chkChan2.Checked;
			tempUser.accessChannels[3] = chkChan3.Checked;
			tempUser.accessChannels[4] = chkChan4.Checked;
			tempUser.accessChannels[5] = chkChan5.Checked;
			tempUser.accessChannels[6] = chkChan6.Checked;
			tempUser.accessChannels[7] = chkChan7.Checked;
			tempUser.accessChannels[8] = chkChan8.Checked;
			tempUser.accessChannels[9] = chkChanSTIN.Checked;
			if (!MixEngine.faderServer.updateUser(tempUser))
			{
				frmConfirm.sayFail("Unknown error saving user.", "User Not Saved");
			}
			initUserList();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnUserSave_Click()", "UI", ex.ToString());
		}
	}

	private void lbUsers_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			if (lbUsers.SelectedItems.Count == 1)
			{
				tempUser = (UserRecord)lbUsers.SelectedItems[0];
				txtUserName.Text = tempUser.userName;
				txtPassword.Text = "********";
				while (tempUser.accessMix.Count < 24)
				{
					tempUser.accessMix.Add(item: false);
				}
				while (tempUser.accessMatrix.Count < 8)
				{
					tempUser.accessMatrix.Add(item: false);
				}
				while (tempUser.accessChannels.Count < 10)
				{
					tempUser.accessChannels.Add(item: true);
				}
				chkMix0.Checked = tempUser.accessMix[0];
				chkMix1.Checked = tempUser.accessMix[1];
				chkMix2.Checked = tempUser.accessMix[2];
				chkMix3.Checked = tempUser.accessMix[3];
				chkMix4.Checked = tempUser.accessMix[4];
				chkMix5.Checked = tempUser.accessMix[5];
				chkMix6.Checked = tempUser.accessMix[6];
				chkMix7.Checked = tempUser.accessMix[7];
				chkMix8.Checked = tempUser.accessMix[8];
				chkMix9.Checked = tempUser.accessMix[9];
				chkMix10.Checked = tempUser.accessMix[10];
				chkMix11.Checked = tempUser.accessMix[11];
				chkMix12.Checked = tempUser.accessMix[12];
				chkMix13.Checked = tempUser.accessMix[13];
				chkMix14.Checked = tempUser.accessMix[14];
				chkMix15.Checked = tempUser.accessMix[15];
				chkMix16.Checked = tempUser.accessMix[16];
				chkMix17.Checked = tempUser.accessMix[17];
				chkMix18.Checked = tempUser.accessMix[18];
				chkMix19.Checked = tempUser.accessMix[19];
				chkMix20.Checked = tempUser.accessMix[20];
				chkMix21.Checked = tempUser.accessMix[21];
				chkMix22.Checked = tempUser.accessMix[22];
				chkMix23.Checked = tempUser.accessMix[23];
				chkMtx0.Checked = tempUser.accessMatrix[0];
				chkMtx1.Checked = tempUser.accessMatrix[1];
				chkMtx2.Checked = tempUser.accessMatrix[2];
				chkMtx3.Checked = tempUser.accessMatrix[3];
				chkMtx4.Checked = tempUser.accessMatrix[4];
				chkMtx5.Checked = tempUser.accessMatrix[5];
				chkMtx6.Checked = tempUser.accessMatrix[6];
				chkMtx7.Checked = tempUser.accessMatrix[7];
				chkChan0.Checked = tempUser.accessChannels[0];
				chkChan1.Checked = tempUser.accessChannels[1];
				chkChan2.Checked = tempUser.accessChannels[2];
				chkChan3.Checked = tempUser.accessChannels[3];
				chkChan4.Checked = tempUser.accessChannels[4];
				chkChan5.Checked = tempUser.accessChannels[5];
				chkChan6.Checked = tempUser.accessChannels[6];
				chkChan7.Checked = tempUser.accessChannels[7];
				chkChan8.Checked = tempUser.accessChannels[8];
				chkChanSTIN.Checked = tempUser.accessChannels[9];
				chkEnabled.Checked = tempUser.active;
				chkMainMix.Checked = tempUser.accessMainMix;
				chkChannel.Checked = tempUser.accessChannel;
				chkMaster.Checked = tempUser.accessMasters;
				chkRack.Checked = tempUser.accessRack;
				lblPermissions.Text = tempUser.userName + "'s Permissions";
				lblAddEditUser.Text = "Edit User '" + tempUser.userName + "'";
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("lbUsers_changed()", "UI", ex.ToString());
		}
	}

	private void txtUserName_TextChanged(object sender, EventArgs e)
	{
		lblPermissions.Text = txtUserName.Text + "'s Permissions";
	}

	private void btnDelUser_Click(object sender, EventArgs e)
	{
		try
		{
			if (lbUsers.SelectedItems.Count == 1)
			{
				MixEngine.faderServer.deleteUser((UserRecord)lbUsers.SelectedItems[0]);
				initUserList();
				btnNewUser_Click(null, null);
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnDelUser_Click()", "UI", ex.ToString());
		}
	}

	private void btnServerStart_Click(object sender, EventArgs e)
	{
		try
		{
			int result;
			if (MixEngine.faderServer.isOnline)
			{
				MixEngine.logDebug("Shutting down FaderService...");
				MixEngine.faderServer.stop();
				initServerConfig();
			}
			else if (int.TryParse(txtServerPort.Text, out result) && result != 0)
			{
				Settings.Default.serverPort = result;
				MixEngine.logDebug("Starting FaderService...");
				MixEngine.faderServer.start(IPAddress.Any, result);
				MixEngine.logDebug("FaderService listening on port #" + result + ".");
				initServerConfig();
			}
			else
			{
				frmConfirm.sayFail("Port Number is not valid.\nServer won't start.", "Server Not Started");
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnServerStart_Click()", "UI", ex.ToString());
		}
	}

	private void btnPermAll_Click(object sender, EventArgs e)
	{
		try
		{
			chkMix0.Checked = true;
			chkMix1.Checked = true;
			chkMix2.Checked = true;
			chkMix3.Checked = true;
			chkMix4.Checked = true;
			chkMix5.Checked = true;
			chkMix6.Checked = true;
			chkMix7.Checked = true;
			chkMix8.Checked = true;
			chkMix9.Checked = true;
			chkMix10.Checked = true;
			chkMix11.Checked = true;
			chkMix12.Checked = true;
			chkMix13.Checked = true;
			chkMix14.Checked = true;
			chkMix15.Checked = true;
			chkMix16.Checked = true;
			chkMix17.Checked = true;
			chkMix18.Checked = true;
			chkMix19.Checked = true;
			chkMix20.Checked = true;
			chkMix21.Checked = true;
			chkMix22.Checked = true;
			chkMix23.Checked = true;
			chkMtx0.Checked = true;
			chkMtx1.Checked = true;
			chkMtx2.Checked = true;
			chkMtx3.Checked = true;
			chkMtx4.Checked = true;
			chkMtx5.Checked = true;
			chkMtx6.Checked = true;
			chkMtx7.Checked = true;
			chkChan0.Checked = true;
			chkChan1.Checked = true;
			chkChan2.Checked = true;
			chkChan3.Checked = true;
			chkChan4.Checked = true;
			chkChan5.Checked = true;
			chkChan6.Checked = true;
			chkChan7.Checked = true;
			chkChan8.Checked = true;
			chkChanSTIN.Checked = true;
			chkMainMix.Checked = true;
			chkChannel.Checked = true;
			chkMaster.Checked = true;
			chkRack.Checked = true;
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnPermAll_Click()", "UI", ex.ToString());
		}
	}

	private void btnNone_Click(object sender, EventArgs e)
	{
		try
		{
			chkMix0.Checked = false;
			chkMix1.Checked = false;
			chkMix2.Checked = false;
			chkMix3.Checked = false;
			chkMix4.Checked = false;
			chkMix5.Checked = false;
			chkMix6.Checked = false;
			chkMix7.Checked = false;
			chkMix8.Checked = false;
			chkMix9.Checked = false;
			chkMix10.Checked = false;
			chkMix11.Checked = false;
			chkMix12.Checked = false;
			chkMix13.Checked = false;
			chkMix14.Checked = false;
			chkMix15.Checked = false;
			chkMix16.Checked = false;
			chkMix17.Checked = false;
			chkMix18.Checked = false;
			chkMix19.Checked = false;
			chkMix20.Checked = false;
			chkMix21.Checked = false;
			chkMix22.Checked = false;
			chkMix23.Checked = false;
			chkMtx0.Checked = false;
			chkMtx1.Checked = false;
			chkMtx2.Checked = false;
			chkMtx3.Checked = false;
			chkMtx4.Checked = false;
			chkMtx5.Checked = false;
			chkMtx6.Checked = false;
			chkMtx7.Checked = false;
			chkChan0.Checked = false;
			chkChan1.Checked = false;
			chkChan2.Checked = false;
			chkChan3.Checked = false;
			chkChan4.Checked = false;
			chkChan5.Checked = false;
			chkChan6.Checked = false;
			chkChan7.Checked = false;
			chkChan8.Checked = false;
			chkChanSTIN.Checked = false;
			chkMainMix.Checked = false;
			chkChannel.Checked = false;
			chkMaster.Checked = false;
			chkRack.Checked = false;
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnNone_Click()", "UI", ex.ToString());
		}
	}

	private void btnSendMsg_Click(object sender, EventArgs e)
	{
		try
		{
			GenMessage genMessage = new GenMessage();
			genMessage.msgtype = MsgType.MSG_BCAST;
			genMessage.message = txtMessage.Text;
			MixEngine.faderServer.sendProto("UI", genMessage);
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnSendMsg_Click()", "UI", ex.ToString());
		}
	}

	private void btnServerRefresh_Click(object sender, EventArgs e)
	{
		initConnectionList();
	}

	private void btnServerDrop_Click(object sender, EventArgs e)
	{
		try
		{
			foreach (ListViewItem item in lvConnections.SelectedItems)
			{
				List<FaderService.SocketPacket> workerSockets = MixEngine.faderServer.workerSockets;
				Predicate<FaderService.SocketPacket> match = (FaderService.SocketPacket obj) => obj.clientNumber == (int)item.Tag;
				FaderService.SocketPacket socketPacket = workerSockets.Find(match);
				if (socketPacket != null)
				{
					MixEngine.faderServer.dropConnection(socketPacket);
				}
			}
			initConnectionList();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnServerDrop_Click()", "UI", ex.ToString());
		}
	}

	private void btnServerDisable_Click(object sender, EventArgs e)
	{
		try
		{
			foreach (ListViewItem item in lvConnections.SelectedItems)
			{
				List<FaderService.SocketPacket> workerSockets = MixEngine.faderServer.workerSockets;
				Predicate<FaderService.SocketPacket> match = (FaderService.SocketPacket obj) => obj.clientNumber == (int)item.Tag;
				FaderService.SocketPacket socketPacket = workerSockets.Find(match);
				if (socketPacket != null)
				{
					uint userID = socketPacket.userID;
					UserRecord userRecord = MixEngine.faderServer.findUser(userID);
					if (userRecord != null)
					{
						MixEngine.faderServer.disableUser(userID);
						frmConfirm.sayOK("Disabled user " + userRecord.userName, "User Disabled");
					}
					else
					{
						frmConfirm.sayFail("Could not find userID #" + userID + "!", "Error");
					}
				}
			}
			initConnectionList();
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnServerDisable()", "UI", ex.ToString());
		}
	}

	private void tabConfig_Selecting(object sender, TabControlCancelEventArgs e)
	{
		try
		{
			if (timerConnList != null)
			{
				timerConnList.Start();
				return;
			}
			timerConnList = new System.Windows.Forms.Timer();
			timerConnList.Interval = (int)TimeSpan.FromSeconds(1.0).TotalMilliseconds;
			timerConnList.Tick += timerConnList_Tick;
			timerConnList.Start();
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("timerConnList Error: " + ex.Message);
		}
	}

	private void timerConnList_Tick(object sender, EventArgs e)
	{
		initConnectionList();
	}

	private void tabConfig_Deselecting(object sender, TabControlCancelEventArgs e)
	{
		try
		{
			if (timerConnList != null)
			{
				timerConnList.Stop();
			}
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("tabConfig_Deselecting Error: " + ex.Message);
		}
	}

	private void btnSendMsgSel_Click(object sender, EventArgs e)
	{
		try
		{
			GenMessage genMessage = new GenMessage();
			genMessage.msgtype = MsgType.MSG_BCAST;
			genMessage.message = txtMessage.Text;
			foreach (ListViewItem item in lvConnections.SelectedItems)
			{
				List<FaderService.SocketPacket> workerSockets = MixEngine.faderServer.workerSockets;
				Predicate<FaderService.SocketPacket> match = (FaderService.SocketPacket obj) => obj.clientNumber == (int)item.Tag;
				FaderService.SocketPacket destInfo = workerSockets.Find(match);
				MixEngine.faderServer.sendProto(genMessage, destInfo);
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("btnSendMsg_Click()", "UI", ex.ToString());
		}
	}

	private void btnMsgClear_Click(object sender, EventArgs e)
	{
		txtMessage.Text = "";
		txtMessage.Focus();
	}

	private void lblBrand_Click(object sender, EventArgs e)
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmConfig));
		this.lbInputDev = new System.Windows.Forms.ListBox();
		this.lbOutputDev = new System.Windows.Forms.ListBox();
		this.lblInDev = new System.Windows.Forms.Label();
		this.lblOutDev = new System.Windows.Forms.Label();
		this.tabConfig = new System.Windows.Forms.TabControl();
		this.tabDevices = new System.Windows.Forms.TabPage();
		this.chkAutoMeters = new System.Windows.Forms.CheckBox();
		this.chkAutoConnect = new System.Windows.Forms.CheckBox();
		this.label53 = new System.Windows.Forms.Label();
		this.btnScan = new System.Windows.Forms.Button();
		this.label10 = new System.Windows.Forms.Label();
		this.cbConsoleType = new System.Windows.Forms.ComboBox();
		this.label7 = new System.Windows.Forms.Label();
		this.btnFixDME = new System.Windows.Forms.Button();
		this.tabCustomize = new System.Windows.Forms.TabPage();
		this.chkConfirmExit = new System.Windows.Forms.CheckBox();
		this.label41 = new System.Windows.Forms.Label();
		this.label40 = new System.Windows.Forms.Label();
		this.btnFaderTail = new System.Windows.Forms.Button();
		this.label39 = new System.Windows.Forms.Label();
		this.btnFaderBack = new System.Windows.Forms.Button();
		this.btnFaderFore = new System.Windows.Forms.Button();
		this.label38 = new System.Windows.Forms.Label();
		this.label37 = new System.Windows.Forms.Label();
		this.cbTheme = new System.Windows.Forms.ComboBox();
		this.btnForeground = new System.Windows.Forms.Button();
		this.btnBackground = new System.Windows.Forms.Button();
		this.label36 = new System.Windows.Forms.Label();
		this.btnLEDColorBack = new System.Windows.Forms.Button();
		this.btnLEDColorFore = new System.Windows.Forms.Button();
		this.label35 = new System.Windows.Forms.Label();
		this.btnInactiveBack = new System.Windows.Forms.Button();
		this.btnInactiveFore = new System.Windows.Forms.Button();
		this.label34 = new System.Windows.Forms.Label();
		this.btnChOffBack = new System.Windows.Forms.Button();
		this.btnChOffFore = new System.Windows.Forms.Button();
		this.label33 = new System.Windows.Forms.Label();
		this.btnChOnBack = new System.Windows.Forms.Button();
		this.btnChOnFore = new System.Windows.Forms.Button();
		this.label32 = new System.Windows.Forms.Label();
		this.btnActiveBack = new System.Windows.Forms.Button();
		this.btnActiveFore = new System.Windows.Forms.Button();
		this.label31 = new System.Windows.Forms.Label();
		this.cbMasterSection = new System.Windows.Forms.ComboBox();
		this.label12 = new System.Windows.Forms.Label();
		this.cbMaxChannel = new System.Windows.Forms.ComboBox();
		this.label5 = new System.Windows.Forms.Label();
		this.lblNumFaders = new System.Windows.Forms.Label();
		this.cbNumFaders = new System.Windows.Forms.ComboBox();
		this.checkBox2 = new System.Windows.Forms.CheckBox();
		this.label1 = new System.Windows.Forms.Label();
		this.cbUDKLamp = new System.Windows.Forms.ComboBox();
		this.checkBox1 = new System.Windows.Forms.CheckBox();
		this.tabCustomLayer = new System.Windows.Forms.TabPage();
		this.label11 = new System.Windows.Forms.Label();
		this.lbCLDest = new System.Windows.Forms.Label();
		this.btnCLDown = new System.Windows.Forms.Button();
		this.btnCLUp = new System.Windows.Forms.Button();
		this.btnCLRemove = new System.Windows.Forms.Button();
		this.btnCLAdd = new System.Windows.Forms.Button();
		this.lbCustomLayer = new System.Windows.Forms.ListBox();
		this.lbChannels = new System.Windows.Forms.ListBox();
		this.tabFaderStrip = new System.Windows.Forms.TabPage();
		this.label29 = new System.Windows.Forms.Label();
		this.label30 = new System.Windows.Forms.Label();
		this.btnCSDown = new System.Windows.Forms.Button();
		this.btnCSUp = new System.Windows.Forms.Button();
		this.btnCSRemove = new System.Windows.Forms.Button();
		this.btnCSAdd = new System.Windows.Forms.Button();
		this.lbStrip = new System.Windows.Forms.ListBox();
		this.lbControls = new System.Windows.Forms.ListBox();
		this.tabServer = new System.Windows.Forms.TabPage();
		this.btnMsgClear = new System.Windows.Forms.Button();
		this.btnSendMsgSel = new System.Windows.Forms.Button();
		this.btnServerDrop = new System.Windows.Forms.Button();
		this.btnServerDisable = new System.Windows.Forms.Button();
		this.lvConnections = new System.Windows.Forms.ListView();
		this.colId = new System.Windows.Forms.ColumnHeader();
		this.colName = new System.Windows.Forms.ColumnHeader();
		this.colIp = new System.Windows.Forms.ColumnHeader();
		this.colClient = new System.Windows.Forms.ColumnHeader();
		this.colSince = new System.Windows.Forms.ColumnHeader();
		this.label52 = new System.Windows.Forms.Label();
		this.txtMessage = new System.Windows.Forms.TextBox();
		this.btnSendMsg = new System.Windows.Forms.Button();
		this.lblServerStatus = new System.Windows.Forms.Label();
		this.label51 = new System.Windows.Forms.Label();
		this.chkServerAutoStart = new System.Windows.Forms.CheckBox();
		this.btnServerStart = new System.Windows.Forms.Button();
		this.txtServerPort = new System.Windows.Forms.TextBox();
		this.lblServerIP = new System.Windows.Forms.Label();
		this.label50 = new System.Windows.Forms.Label();
		this.label49 = new System.Windows.Forms.Label();
		this.label48 = new System.Windows.Forms.Label();
		this.label47 = new System.Windows.Forms.Label();
		this.tabUsers = new System.Windows.Forms.TabPage();
		this.chkChan8 = new System.Windows.Forms.CheckBox();
		this.chkMtx7 = new System.Windows.Forms.CheckBox();
		this.chkMtx6 = new System.Windows.Forms.CheckBox();
		this.chkMtx5 = new System.Windows.Forms.CheckBox();
		this.chkMtx4 = new System.Windows.Forms.CheckBox();
		this.chkMtx3 = new System.Windows.Forms.CheckBox();
		this.chkMtx2 = new System.Windows.Forms.CheckBox();
		this.chkMtx1 = new System.Windows.Forms.CheckBox();
		this.chkMtx0 = new System.Windows.Forms.CheckBox();
		this.label56 = new System.Windows.Forms.Label();
		this.chkMix23 = new System.Windows.Forms.CheckBox();
		this.chkMix22 = new System.Windows.Forms.CheckBox();
		this.chkMix21 = new System.Windows.Forms.CheckBox();
		this.chkMix20 = new System.Windows.Forms.CheckBox();
		this.chkMix19 = new System.Windows.Forms.CheckBox();
		this.chkMix18 = new System.Windows.Forms.CheckBox();
		this.chkMix17 = new System.Windows.Forms.CheckBox();
		this.chkMix16 = new System.Windows.Forms.CheckBox();
		this.chkChanSTIN = new System.Windows.Forms.CheckBox();
		this.chkChan7 = new System.Windows.Forms.CheckBox();
		this.chkChan6 = new System.Windows.Forms.CheckBox();
		this.chkChan5 = new System.Windows.Forms.CheckBox();
		this.chkChan4 = new System.Windows.Forms.CheckBox();
		this.chkChan3 = new System.Windows.Forms.CheckBox();
		this.chkChan2 = new System.Windows.Forms.CheckBox();
		this.chkChan1 = new System.Windows.Forms.CheckBox();
		this.chkChan0 = new System.Windows.Forms.CheckBox();
		this.label55 = new System.Windows.Forms.Label();
		this.label54 = new System.Windows.Forms.Label();
		this.chkRack = new System.Windows.Forms.CheckBox();
		this.chkMaster = new System.Windows.Forms.CheckBox();
		this.btnNone = new System.Windows.Forms.Button();
		this.btnPermAll = new System.Windows.Forms.Button();
		this.btnDelUser = new System.Windows.Forms.Button();
		this.label46 = new System.Windows.Forms.Label();
		this.label45 = new System.Windows.Forms.Label();
		this.lblAddEditUser = new System.Windows.Forms.Label();
		this.lblPermissions = new System.Windows.Forms.Label();
		this.label44 = new System.Windows.Forms.Label();
		this.chkChannel = new System.Windows.Forms.CheckBox();
		this.chkMainMix = new System.Windows.Forms.CheckBox();
		this.chkMix15 = new System.Windows.Forms.CheckBox();
		this.chkMix14 = new System.Windows.Forms.CheckBox();
		this.chkMix13 = new System.Windows.Forms.CheckBox();
		this.chkMix12 = new System.Windows.Forms.CheckBox();
		this.chkMix11 = new System.Windows.Forms.CheckBox();
		this.chkMix10 = new System.Windows.Forms.CheckBox();
		this.chkMix9 = new System.Windows.Forms.CheckBox();
		this.chkMix8 = new System.Windows.Forms.CheckBox();
		this.chkMix7 = new System.Windows.Forms.CheckBox();
		this.chkMix6 = new System.Windows.Forms.CheckBox();
		this.chkMix5 = new System.Windows.Forms.CheckBox();
		this.chkMix4 = new System.Windows.Forms.CheckBox();
		this.chkMix3 = new System.Windows.Forms.CheckBox();
		this.chkMix2 = new System.Windows.Forms.CheckBox();
		this.chkMix1 = new System.Windows.Forms.CheckBox();
		this.chkMix0 = new System.Windows.Forms.CheckBox();
		this.chkEnabled = new System.Windows.Forms.CheckBox();
		this.txtPassword = new System.Windows.Forms.TextBox();
		this.txtUserName = new System.Windows.Forms.TextBox();
		this.label43 = new System.Windows.Forms.Label();
		this.label42 = new System.Windows.Forms.Label();
		this.btnNewUser = new System.Windows.Forms.Button();
		this.btnUserSave = new System.Windows.Forms.Button();
		this.lbUsers = new System.Windows.Forms.ListBox();
		this.tabAbout = new System.Windows.Forms.TabPage();
		this.label8 = new System.Windows.Forms.Label();
		this.btnUpdate = new System.Windows.Forms.Button();
		this.lblKey = new System.Windows.Forms.Label();
		this.btnRegister = new System.Windows.Forms.Button();
		this.lblOwner = new System.Windows.Forms.Label();
		this.label9 = new System.Windows.Forms.Label();
		this.lblVersion = new System.Windows.Forms.Label();
		this.lblProduct = new System.Windows.Forms.Label();
		this.lblBrand = new System.Windows.Forms.Label();
		this.cbLanguage = new System.Windows.Forms.ComboBox();
		this.labelLang = new System.Windows.Forms.Label();
		this.label6 = new System.Windows.Forms.Label();
		this.txtGain = new System.Windows.Forms.TextBox();
		this.chkGO = new System.Windows.Forms.CheckBox();
		this.txtMeter = new System.Windows.Forms.TextBox();
		this.txtQ = new System.Windows.Forms.TextBox();
		this.txtFreq = new System.Windows.Forms.TextBox();
		this.label4 = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.btnSweep = new System.Windows.Forms.Button();
		this.btnSave = new System.Windows.Forms.Button();
		this.btnCancel = new System.Windows.Forms.Button();
		this.tabPage1 = new System.Windows.Forms.TabPage();
		this.button1 = new System.Windows.Forms.Button();
		this.label13 = new System.Windows.Forms.Label();
		this.comboBox1 = new System.Windows.Forms.ComboBox();
		this.label14 = new System.Windows.Forms.Label();
		this.button2 = new System.Windows.Forms.Button();
		this.listBox1 = new System.Windows.Forms.ListBox();
		this.label15 = new System.Windows.Forms.Label();
		this.listBox2 = new System.Windows.Forms.ListBox();
		this.label16 = new System.Windows.Forms.Label();
		this.tabPage2 = new System.Windows.Forms.TabPage();
		this.comboBox2 = new System.Windows.Forms.ComboBox();
		this.label17 = new System.Windows.Forms.Label();
		this.comboBox3 = new System.Windows.Forms.ComboBox();
		this.label18 = new System.Windows.Forms.Label();
		this.label19 = new System.Windows.Forms.Label();
		this.comboBox4 = new System.Windows.Forms.ComboBox();
		this.checkBox6 = new System.Windows.Forms.CheckBox();
		this.checkBox7 = new System.Windows.Forms.CheckBox();
		this.checkBox8 = new System.Windows.Forms.CheckBox();
		this.checkBox9 = new System.Windows.Forms.CheckBox();
		this.label20 = new System.Windows.Forms.Label();
		this.comboBox5 = new System.Windows.Forms.ComboBox();
		this.checkBox10 = new System.Windows.Forms.CheckBox();
		this.tabPage3 = new System.Windows.Forms.TabPage();
		this.label21 = new System.Windows.Forms.Label();
		this.label22 = new System.Windows.Forms.Label();
		this.button3 = new System.Windows.Forms.Button();
		this.button4 = new System.Windows.Forms.Button();
		this.button5 = new System.Windows.Forms.Button();
		this.button6 = new System.Windows.Forms.Button();
		this.listBox3 = new System.Windows.Forms.ListBox();
		this.listBox4 = new System.Windows.Forms.ListBox();
		this.tabPage4 = new System.Windows.Forms.TabPage();
		this.label23 = new System.Windows.Forms.Label();
		this.button7 = new System.Windows.Forms.Button();
		this.label24 = new System.Windows.Forms.Label();
		this.button8 = new System.Windows.Forms.Button();
		this.label25 = new System.Windows.Forms.Label();
		this.label26 = new System.Windows.Forms.Label();
		this.label27 = new System.Windows.Forms.Label();
		this.label28 = new System.Windows.Forms.Label();
		this.lblBrand2 = new System.Windows.Forms.Label();
		this.colorPick = new System.Windows.Forms.ColorDialog();
		this.tabConfig.SuspendLayout();
		this.tabDevices.SuspendLayout();
		this.tabCustomize.SuspendLayout();
		this.tabCustomLayer.SuspendLayout();
		this.tabFaderStrip.SuspendLayout();
		this.tabServer.SuspendLayout();
		this.tabUsers.SuspendLayout();
		this.tabAbout.SuspendLayout();
		this.tabPage1.SuspendLayout();
		this.tabPage2.SuspendLayout();
		this.tabPage3.SuspendLayout();
		this.tabPage4.SuspendLayout();
		base.SuspendLayout();
		this.lbInputDev.FormattingEnabled = true;
		this.lbInputDev.Location = new System.Drawing.Point(20, 28);
		this.lbInputDev.Name = "lbInputDev";
		this.lbInputDev.Size = new System.Drawing.Size(219, 95);
		this.lbInputDev.TabIndex = 2;
		this.lbOutputDev.FormattingEnabled = true;
		this.lbOutputDev.Location = new System.Drawing.Point(20, 159);
		this.lbOutputDev.Name = "lbOutputDev";
		this.lbOutputDev.Size = new System.Drawing.Size(219, 95);
		this.lbOutputDev.TabIndex = 3;
		this.lblInDev.AutoSize = true;
		this.lblInDev.Location = new System.Drawing.Point(20, 12);
		this.lblInDev.Name = "lblInDev";
		this.lblInDev.Size = new System.Drawing.Size(68, 13);
		this.lblInDev.TabIndex = 65;
		this.lblInDev.Text = "Input Device";
		this.lblOutDev.AutoSize = true;
		this.lblOutDev.Location = new System.Drawing.Point(20, 143);
		this.lblOutDev.Name = "lblOutDev";
		this.lblOutDev.Size = new System.Drawing.Size(76, 13);
		this.lblOutDev.TabIndex = 66;
		this.lblOutDev.Text = "Output Device";
		this.tabConfig.Controls.Add(this.tabDevices);
		this.tabConfig.Controls.Add(this.tabCustomize);
		this.tabConfig.Controls.Add(this.tabCustomLayer);
		this.tabConfig.Controls.Add(this.tabFaderStrip);
		this.tabConfig.Controls.Add(this.tabServer);
		this.tabConfig.Controls.Add(this.tabUsers);
		this.tabConfig.Controls.Add(this.tabAbout);
		this.tabConfig.Location = new System.Drawing.Point(12, 12);
		this.tabConfig.Name = "tabConfig";
		this.tabConfig.SelectedIndex = 0;
		this.tabConfig.Size = new System.Drawing.Size(582, 451);
		this.tabConfig.TabIndex = 1;
		this.tabConfig.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(tabConfig_Selecting);
		this.tabConfig.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(tabConfig_Deselecting);
		this.tabDevices.Controls.Add(this.chkAutoMeters);
		this.tabDevices.Controls.Add(this.chkAutoConnect);
		this.tabDevices.Controls.Add(this.label53);
		this.tabDevices.Controls.Add(this.btnScan);
		this.tabDevices.Controls.Add(this.label10);
		this.tabDevices.Controls.Add(this.cbConsoleType);
		this.tabDevices.Controls.Add(this.label7);
		this.tabDevices.Controls.Add(this.btnFixDME);
		this.tabDevices.Controls.Add(this.lbInputDev);
		this.tabDevices.Controls.Add(this.lblOutDev);
		this.tabDevices.Controls.Add(this.lbOutputDev);
		this.tabDevices.Controls.Add(this.lblInDev);
		this.tabDevices.Location = new System.Drawing.Point(4, 22);
		this.tabDevices.Name = "tabDevices";
		this.tabDevices.Padding = new System.Windows.Forms.Padding(3);
		this.tabDevices.Size = new System.Drawing.Size(574, 425);
		this.tabDevices.TabIndex = 0;
		this.tabDevices.Text = "Devices";
		this.tabDevices.UseVisualStyleBackColor = true;
		this.chkAutoMeters.Location = new System.Drawing.Point(248, 211);
		this.chkAutoMeters.Name = "chkAutoMeters";
		this.chkAutoMeters.Size = new System.Drawing.Size(220, 19);
		this.chkAutoMeters.TabIndex = 84;
		this.chkAutoMeters.Text = "Enable Meters when connecting";
		this.chkAutoMeters.UseVisualStyleBackColor = true;
		this.chkAutoConnect.Location = new System.Drawing.Point(248, 186);
		this.chkAutoConnect.Name = "chkAutoConnect";
		this.chkAutoConnect.Size = new System.Drawing.Size(220, 19);
		this.chkAutoConnect.TabIndex = 69;
		this.chkAutoConnect.Text = "Auto-Connect when 01vmix starts";
		this.chkAutoConnect.UseVisualStyleBackColor = true;
		this.label53.Location = new System.Drawing.Point(9, 266);
		this.label53.Name = "label53";
		this.label53.Size = new System.Drawing.Size(481, 37);
		this.label53.TabIndex = 83;
		this.label53.Text = "Don't see your console? Go back and configure the Yamaha DME Network Driver first.\r\nVisit the AirFader.com FAQ for instructions.";
		this.btnScan.Location = new System.Drawing.Point(248, 236);
		this.btnScan.Name = "btnScan";
		this.btnScan.Size = new System.Drawing.Size(75, 23);
		this.btnScan.TabIndex = 82;
		this.btnScan.Text = "Get Text";
		this.btnScan.UseVisualStyleBackColor = true;
		this.btnScan.Visible = false;
		this.btnScan.Click += new System.EventHandler(btnScan_Click);
		this.label10.AutoSize = true;
		this.label10.Location = new System.Drawing.Point(248, 143);
		this.label10.Name = "label10";
		this.label10.Size = new System.Drawing.Size(72, 13);
		this.label10.TabIndex = 81;
		this.label10.Text = "Console Type";
		this.cbConsoleType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbConsoleType.FormattingEnabled = true;
		this.cbConsoleType.Items.AddRange(new object[6] { "Yamaha LS9-16", "Yamaha LS9-32", "Yamaha M7CL-32", "Yamaha M7CL-48", "Yamaha 01V96", "Yamaha 01V96i" });
		this.cbConsoleType.Location = new System.Drawing.Point(248, 159);
		this.cbConsoleType.Name = "cbConsoleType";
		this.cbConsoleType.Size = new System.Drawing.Size(147, 21);
		this.cbConsoleType.TabIndex = 80;
		this.label7.Location = new System.Drawing.Point(245, 28);
		this.label7.Name = "label7";
		this.label7.Size = new System.Drawing.Size(239, 62);
		this.label7.TabIndex = 79;
		this.label7.Text = "If 01vmix will not sync via IP after recent network problems or a software crash, the DME MIDI driver may not be responding. Click below to attempt to restart. (Will not affect the console.)";
		this.btnFixDME.BackColor = System.Drawing.SystemColors.Control;
		this.btnFixDME.Location = new System.Drawing.Point(248, 93);
		this.btnFixDME.Name = "btnFixDME";
		this.btnFixDME.Size = new System.Drawing.Size(236, 30);
		this.btnFixDME.TabIndex = 67;
		this.btnFixDME.Text = "Restart DME Driver";
		this.btnFixDME.UseVisualStyleBackColor = false;
		this.btnFixDME.Click += new System.EventHandler(btnStopDME_Click);
		this.tabCustomize.Controls.Add(this.chkConfirmExit);
		this.tabCustomize.Controls.Add(this.label41);
		this.tabCustomize.Controls.Add(this.label40);
		this.tabCustomize.Controls.Add(this.btnFaderTail);
		this.tabCustomize.Controls.Add(this.label39);
		this.tabCustomize.Controls.Add(this.btnFaderBack);
		this.tabCustomize.Controls.Add(this.btnFaderFore);
		this.tabCustomize.Controls.Add(this.label38);
		this.tabCustomize.Controls.Add(this.label37);
		this.tabCustomize.Controls.Add(this.cbTheme);
		this.tabCustomize.Controls.Add(this.btnForeground);
		this.tabCustomize.Controls.Add(this.btnBackground);
		this.tabCustomize.Controls.Add(this.label36);
		this.tabCustomize.Controls.Add(this.btnLEDColorBack);
		this.tabCustomize.Controls.Add(this.btnLEDColorFore);
		this.tabCustomize.Controls.Add(this.label35);
		this.tabCustomize.Controls.Add(this.btnInactiveBack);
		this.tabCustomize.Controls.Add(this.btnInactiveFore);
		this.tabCustomize.Controls.Add(this.label34);
		this.tabCustomize.Controls.Add(this.btnChOffBack);
		this.tabCustomize.Controls.Add(this.btnChOffFore);
		this.tabCustomize.Controls.Add(this.label33);
		this.tabCustomize.Controls.Add(this.btnChOnBack);
		this.tabCustomize.Controls.Add(this.btnChOnFore);
		this.tabCustomize.Controls.Add(this.label32);
		this.tabCustomize.Controls.Add(this.btnActiveBack);
		this.tabCustomize.Controls.Add(this.btnActiveFore);
		this.tabCustomize.Controls.Add(this.label31);
		this.tabCustomize.Controls.Add(this.cbMasterSection);
		this.tabCustomize.Controls.Add(this.label12);
		this.tabCustomize.Controls.Add(this.cbMaxChannel);
		this.tabCustomize.Controls.Add(this.label5);
		this.tabCustomize.Controls.Add(this.lblNumFaders);
		this.tabCustomize.Controls.Add(this.cbNumFaders);
		this.tabCustomize.Controls.Add(this.checkBox2);
		this.tabCustomize.Controls.Add(this.label1);
		this.tabCustomize.Controls.Add(this.cbUDKLamp);
		this.tabCustomize.Controls.Add(this.checkBox1);
		this.tabCustomize.Controls.Add(this.cbLanguage);
		this.tabCustomize.Controls.Add(this.labelLang);
		this.tabCustomize.Location = new System.Drawing.Point(4, 22);
		this.tabCustomize.Name = "tabCustomize";
		this.tabCustomize.Padding = new System.Windows.Forms.Padding(3);
		this.tabCustomize.Size = new System.Drawing.Size(574, 425);
		this.tabCustomize.TabIndex = 1;
		this.tabCustomize.Text = "Options";
		this.tabCustomize.UseVisualStyleBackColor = true;
		this.chkConfirmExit.AutoSize = true;
		this.chkConfirmExit.Checked = true;
		this.chkConfirmExit.CheckState = System.Windows.Forms.CheckState.Checked;
		this.chkConfirmExit.Location = new System.Drawing.Point(310, 120);
		this.chkConfirmExit.Name = "chkConfirmExit";
		this.chkConfirmExit.Size = new System.Drawing.Size(113, 17);
		this.chkConfirmExit.TabIndex = 43;
		this.chkConfirmExit.Text = "Ask to confirm Exit";
		this.chkConfirmExit.UseVisualStyleBackColor = true;
		this.label41.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, 0);
		this.label41.Location = new System.Drawing.Point(157, 32);
		this.label41.Name = "label41";
		this.label41.Size = new System.Drawing.Size(42, 13);
		this.label41.TabIndex = 42;
		this.label41.Text = "Back";
		this.label41.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label40.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, 0);
		this.label40.Location = new System.Drawing.Point(109, 32);
		this.label40.Name = "label40";
		this.label40.Size = new System.Drawing.Size(42, 13);
		this.label40.TabIndex = 41;
		this.label40.Text = "Text";
		this.label40.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnFaderTail.Location = new System.Drawing.Point(157, 238);
		this.btnFaderTail.Name = "btnFaderTail";
		this.btnFaderTail.Size = new System.Drawing.Size(42, 23);
		this.btnFaderTail.TabIndex = 40;
		this.btnFaderTail.UseVisualStyleBackColor = true;
		this.btnFaderTail.Click += new System.EventHandler(btnColor_Click);
		this.label39.Location = new System.Drawing.Point(5, 241);
		this.label39.Name = "label39";
		this.label39.Size = new System.Drawing.Size(98, 13);
		this.label39.TabIndex = 39;
		this.label39.Text = "Fader Tails";
		this.label39.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.btnFaderBack.Location = new System.Drawing.Point(157, 209);
		this.btnFaderBack.Name = "btnFaderBack";
		this.btnFaderBack.Size = new System.Drawing.Size(42, 23);
		this.btnFaderBack.TabIndex = 38;
		this.btnFaderBack.UseVisualStyleBackColor = true;
		this.btnFaderBack.Click += new System.EventHandler(btnColor_Click);
		this.btnFaderFore.Location = new System.Drawing.Point(109, 209);
		this.btnFaderFore.Name = "btnFaderFore";
		this.btnFaderFore.Size = new System.Drawing.Size(42, 23);
		this.btnFaderFore.TabIndex = 37;
		this.btnFaderFore.UseVisualStyleBackColor = true;
		this.btnFaderFore.Click += new System.EventHandler(btnColor_Click);
		this.label38.Location = new System.Drawing.Point(5, 214);
		this.label38.Name = "label38";
		this.label38.Size = new System.Drawing.Size(98, 13);
		this.label38.TabIndex = 36;
		this.label38.Text = "Faders";
		this.label38.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.label37.Location = new System.Drawing.Point(5, 9);
		this.label37.Name = "label37";
		this.label37.Size = new System.Drawing.Size(98, 13);
		this.label37.TabIndex = 35;
		this.label37.Text = "Color Theme";
		this.label37.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.cbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbTheme.FormattingEnabled = true;
		this.cbTheme.Location = new System.Drawing.Point(109, 6);
		this.cbTheme.Name = "cbTheme";
		this.cbTheme.Size = new System.Drawing.Size(121, 21);
		this.cbTheme.TabIndex = 34;
		this.cbTheme.SelectedIndexChanged += new System.EventHandler(cbTheme_SelectedIndexChanged);
		this.btnForeground.Location = new System.Drawing.Point(109, 272);
		this.btnForeground.Name = "btnForeground";
		this.btnForeground.Size = new System.Drawing.Size(42, 23);
		this.btnForeground.TabIndex = 33;
		this.btnForeground.UseVisualStyleBackColor = true;
		this.btnForeground.Click += new System.EventHandler(btnColor_Click);
		this.btnBackground.Location = new System.Drawing.Point(157, 272);
		this.btnBackground.Name = "btnBackground";
		this.btnBackground.Size = new System.Drawing.Size(42, 23);
		this.btnBackground.TabIndex = 32;
		this.btnBackground.UseVisualStyleBackColor = true;
		this.btnBackground.Click += new System.EventHandler(btnColor_Click);
		this.label36.Location = new System.Drawing.Point(5, 277);
		this.label36.Name = "label36";
		this.label36.Size = new System.Drawing.Size(98, 13);
		this.label36.TabIndex = 30;
		this.label36.Text = "Frame Colors";
		this.label36.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.btnLEDColorBack.Location = new System.Drawing.Point(157, 180);
		this.btnLEDColorBack.Name = "btnLEDColorBack";
		this.btnLEDColorBack.Size = new System.Drawing.Size(42, 23);
		this.btnLEDColorBack.TabIndex = 29;
		this.btnLEDColorBack.UseVisualStyleBackColor = true;
		this.btnLEDColorBack.Click += new System.EventHandler(btnColor_Click);
		this.btnLEDColorFore.Location = new System.Drawing.Point(109, 180);
		this.btnLEDColorFore.Name = "btnLEDColorFore";
		this.btnLEDColorFore.Size = new System.Drawing.Size(42, 23);
		this.btnLEDColorFore.TabIndex = 28;
		this.btnLEDColorFore.UseVisualStyleBackColor = true;
		this.btnLEDColorFore.Click += new System.EventHandler(btnColor_Click);
		this.label35.Location = new System.Drawing.Point(5, 185);
		this.label35.Name = "label35";
		this.label35.Size = new System.Drawing.Size(98, 13);
		this.label35.TabIndex = 27;
		this.label35.Text = "\"LED\" Labels";
		this.label35.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.btnInactiveBack.Location = new System.Drawing.Point(157, 77);
		this.btnInactiveBack.Name = "btnInactiveBack";
		this.btnInactiveBack.Size = new System.Drawing.Size(42, 23);
		this.btnInactiveBack.TabIndex = 26;
		this.btnInactiveBack.UseVisualStyleBackColor = true;
		this.btnInactiveBack.Click += new System.EventHandler(btnColor_Click);
		this.btnInactiveFore.Location = new System.Drawing.Point(109, 77);
		this.btnInactiveFore.Name = "btnInactiveFore";
		this.btnInactiveFore.Size = new System.Drawing.Size(42, 23);
		this.btnInactiveFore.TabIndex = 25;
		this.btnInactiveFore.UseVisualStyleBackColor = true;
		this.btnInactiveFore.Click += new System.EventHandler(btnColor_Click);
		this.label34.Location = new System.Drawing.Point(5, 82);
		this.label34.Name = "label34";
		this.label34.Size = new System.Drawing.Size(98, 13);
		this.label34.TabIndex = 24;
		this.label34.Text = "Inactive Button";
		this.label34.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.btnChOffBack.Location = new System.Drawing.Point(157, 143);
		this.btnChOffBack.Name = "btnChOffBack";
		this.btnChOffBack.Size = new System.Drawing.Size(42, 23);
		this.btnChOffBack.TabIndex = 23;
		this.btnChOffBack.UseVisualStyleBackColor = true;
		this.btnChOffBack.Click += new System.EventHandler(btnColor_Click);
		this.btnChOffFore.Location = new System.Drawing.Point(109, 143);
		this.btnChOffFore.Name = "btnChOffFore";
		this.btnChOffFore.Size = new System.Drawing.Size(42, 23);
		this.btnChOffFore.TabIndex = 22;
		this.btnChOffFore.UseVisualStyleBackColor = true;
		this.btnChOffFore.Click += new System.EventHandler(btnColor_Click);
		this.label33.Location = new System.Drawing.Point(5, 148);
		this.label33.Name = "label33";
		this.label33.Size = new System.Drawing.Size(98, 13);
		this.label33.TabIndex = 21;
		this.label33.Text = "Channel Off";
		this.label33.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.btnChOnBack.Location = new System.Drawing.Point(157, 114);
		this.btnChOnBack.Name = "btnChOnBack";
		this.btnChOnBack.Size = new System.Drawing.Size(42, 23);
		this.btnChOnBack.TabIndex = 20;
		this.btnChOnBack.UseVisualStyleBackColor = true;
		this.btnChOnBack.Click += new System.EventHandler(btnColor_Click);
		this.btnChOnFore.Location = new System.Drawing.Point(109, 114);
		this.btnChOnFore.Name = "btnChOnFore";
		this.btnChOnFore.Size = new System.Drawing.Size(42, 23);
		this.btnChOnFore.TabIndex = 19;
		this.btnChOnFore.UseVisualStyleBackColor = true;
		this.btnChOnFore.Click += new System.EventHandler(btnColor_Click);
		this.label32.Location = new System.Drawing.Point(5, 119);
		this.label32.Name = "label32";
		this.label32.Size = new System.Drawing.Size(98, 13);
		this.label32.TabIndex = 18;
		this.label32.Text = "Channel On";
		this.label32.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.btnActiveBack.Location = new System.Drawing.Point(157, 48);
		this.btnActiveBack.Name = "btnActiveBack";
		this.btnActiveBack.Size = new System.Drawing.Size(42, 23);
		this.btnActiveBack.TabIndex = 17;
		this.btnActiveBack.UseVisualStyleBackColor = true;
		this.btnActiveBack.Click += new System.EventHandler(btnColor_Click);
		this.btnActiveFore.Location = new System.Drawing.Point(109, 48);
		this.btnActiveFore.Name = "btnActiveFore";
		this.btnActiveFore.Size = new System.Drawing.Size(42, 23);
		this.btnActiveFore.TabIndex = 16;
		this.btnActiveFore.UseVisualStyleBackColor = true;
		this.btnActiveFore.Click += new System.EventHandler(btnColor_Click);
		this.label31.Location = new System.Drawing.Point(5, 53);
		this.label31.Name = "label31";
		this.label31.Size = new System.Drawing.Size(98, 13);
		this.label31.TabIndex = 15;
		this.label31.Text = "Active Button";
		this.label31.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.cbMasterSection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbMasterSection.FormattingEnabled = true;
		this.cbMasterSection.Items.AddRange(new object[7] { "None", "Stereo", "Mono", "Mono+ST", "Monitor", "ST+Monitor", "Monitor+ST" });
		this.cbMasterSection.Location = new System.Drawing.Point(393, 88);
		this.cbMasterSection.Name = "cbMasterSection";
		this.cbMasterSection.Size = new System.Drawing.Size(97, 21);
		this.cbMasterSection.TabIndex = 14;
		this.label12.Location = new System.Drawing.Point(251, 92);
		this.label12.Name = "label12";
		this.label12.Size = new System.Drawing.Size(136, 13);
		this.label12.TabIndex = 13;
		this.label12.Text = "Master Fader Section";
		this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.cbMaxChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbMaxChannel.FormattingEnabled = true;
		this.cbMaxChannel.Items.AddRange(new object[8] { "8", "16", "24", "32", "40", "48", "56", "64" });
		this.cbMaxChannel.Location = new System.Drawing.Point(393, 34);
		this.cbMaxChannel.Name = "cbMaxChannel";
		this.cbMaxChannel.Size = new System.Drawing.Size(56, 21);
		this.cbMaxChannel.TabIndex = 10;
		this.label5.Location = new System.Drawing.Point(251, 38);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(136, 13);
		this.label5.TabIndex = 9;
		this.label5.Text = "Maximum Channels";
		this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.lblNumFaders.Location = new System.Drawing.Point(251, 11);
		this.lblNumFaders.Name = "lblNumFaders";
		this.lblNumFaders.Size = new System.Drawing.Size(136, 13);
		this.lblNumFaders.TabIndex = 8;
		this.lblNumFaders.Text = "Number of Faders to Show";
		this.lblNumFaders.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.cbNumFaders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbNumFaders.FormattingEnabled = true;
		this.cbNumFaders.Items.AddRange(new object[6] { "4", "8", "12", "16", "24", "32" });
		this.cbNumFaders.Location = new System.Drawing.Point(393, 7);
		this.cbNumFaders.Name = "cbNumFaders";
		this.cbNumFaders.Size = new System.Drawing.Size(56, 21);
		this.cbNumFaders.TabIndex = 7;
		this.checkBox2.AutoSize = true;
		this.checkBox2.Checked = true;
		this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox2.Location = new System.Drawing.Point(270, 245);
		this.checkBox2.Name = "checkBox2";
		this.checkBox2.Size = new System.Drawing.Size(114, 17);
		this.checkBox2.TabIndex = 3;
		this.checkBox2.Text = "Show \"Fader tails\"";
		this.checkBox2.UseVisualStyleBackColor = true;
		this.checkBox2.Visible = false;
		this.label1.Location = new System.Drawing.Point(251, 65);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(136, 13);
		this.label1.TabIndex = 2;
		this.label1.Text = "UDF Key for Intercom Flasher";
		this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.cbUDKLamp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbUDKLamp.FormattingEnabled = true;
		this.cbUDKLamp.Items.AddRange(new object[13]
		{
			"None", "1", "2", "3", "4", "5", "6", "7", "8", "9",
			"10", "11", "12"
		});
		this.cbUDKLamp.Location = new System.Drawing.Point(393, 61);
		this.cbUDKLamp.Name = "cbUDKLamp";
		this.cbUDKLamp.Size = new System.Drawing.Size(56, 21);
		this.cbUDKLamp.TabIndex = 12;
		this.checkBox1.AutoSize = true;
		this.checkBox1.Checked = true;
		this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox1.Location = new System.Drawing.Point(270, 224);
		this.checkBox1.Name = "checkBox1";
		this.checkBox1.Size = new System.Drawing.Size(164, 17);
		this.checkBox1.TabIndex = 0;
		this.checkBox1.Text = "\"Click anywhere\" fader mode";
		this.checkBox1.UseVisualStyleBackColor = true;
		this.checkBox1.Visible = false;
		this.labelLang.Location = new System.Drawing.Point(251, 150);
		this.labelLang.Name = "labelLang";
		this.labelLang.Size = new System.Drawing.Size(136, 13);
		this.labelLang.TabIndex = 44;
		this.labelLang.Text = "Language";
		this.labelLang.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.cbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cbLanguage.FormattingEnabled = true;
		this.cbLanguage.Items.AddRange(new object[2] { "English", "Português (Brasil)" });
		this.cbLanguage.Location = new System.Drawing.Point(393, 146);
		this.cbLanguage.Name = "cbLanguage";
		this.cbLanguage.Size = new System.Drawing.Size(97, 21);
		this.cbLanguage.TabIndex = 45;
		this.cbLanguage.SelectedIndexChanged += new System.EventHandler(cbLanguage_SelectedIndexChanged);
		this.tabCustomLayer.Controls.Add(this.label11);
		this.tabCustomLayer.Controls.Add(this.lbCLDest);
		this.tabCustomLayer.Controls.Add(this.btnCLDown);
		this.tabCustomLayer.Controls.Add(this.btnCLUp);
		this.tabCustomLayer.Controls.Add(this.btnCLRemove);
		this.tabCustomLayer.Controls.Add(this.btnCLAdd);
		this.tabCustomLayer.Controls.Add(this.lbCustomLayer);
		this.tabCustomLayer.Controls.Add(this.lbChannels);
		this.tabCustomLayer.Location = new System.Drawing.Point(4, 22);
		this.tabCustomLayer.Name = "tabCustomLayer";
		this.tabCustomLayer.Size = new System.Drawing.Size(574, 425);
		this.tabCustomLayer.TabIndex = 3;
		this.tabCustomLayer.Text = "Custom Layer";
		this.tabCustomLayer.UseVisualStyleBackColor = true;
		this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label11.Location = new System.Drawing.Point(3, 10);
		this.label11.Name = "label11";
		this.label11.Size = new System.Drawing.Size(189, 23);
		this.label11.TabIndex = 7;
		this.label11.Text = "Available Channels";
		this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lbCLDest.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lbCLDest.Location = new System.Drawing.Point(304, 10);
		this.lbCLDest.Name = "lbCLDest";
		this.lbCLDest.Size = new System.Drawing.Size(189, 23);
		this.lbCLDest.TabIndex = 6;
		this.lbCLDest.Text = "Custom Fader Layer";
		this.lbCLDest.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnCLDown.Location = new System.Drawing.Point(210, 213);
		this.btnCLDown.Name = "btnCLDown";
		this.btnCLDown.Size = new System.Drawing.Size(75, 38);
		this.btnCLDown.TabIndex = 5;
		this.btnCLDown.Text = "Down";
		this.btnCLDown.UseVisualStyleBackColor = true;
		this.btnCLDown.Click += new System.EventHandler(btnCLDown_Click);
		this.btnCLUp.Location = new System.Drawing.Point(210, 169);
		this.btnCLUp.Name = "btnCLUp";
		this.btnCLUp.Size = new System.Drawing.Size(75, 38);
		this.btnCLUp.TabIndex = 4;
		this.btnCLUp.Text = "Up";
		this.btnCLUp.UseVisualStyleBackColor = true;
		this.btnCLUp.Click += new System.EventHandler(btnCLUp_Click);
		this.btnCLRemove.Location = new System.Drawing.Point(210, 80);
		this.btnCLRemove.Name = "btnCLRemove";
		this.btnCLRemove.Size = new System.Drawing.Size(75, 38);
		this.btnCLRemove.TabIndex = 3;
		this.btnCLRemove.Text = "<- Remove";
		this.btnCLRemove.UseVisualStyleBackColor = true;
		this.btnCLRemove.Click += new System.EventHandler(btnCLRemove_Click);
		this.btnCLAdd.Location = new System.Drawing.Point(210, 36);
		this.btnCLAdd.Name = "btnCLAdd";
		this.btnCLAdd.Size = new System.Drawing.Size(75, 38);
		this.btnCLAdd.TabIndex = 2;
		this.btnCLAdd.Text = "Add ->";
		this.btnCLAdd.UseVisualStyleBackColor = true;
		this.btnCLAdd.Click += new System.EventHandler(btnCLAdd_Click);
		this.lbCustomLayer.FormattingEnabled = true;
		this.lbCustomLayer.Location = new System.Drawing.Point(304, 36);
		this.lbCustomLayer.Name = "lbCustomLayer";
		this.lbCustomLayer.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.lbCustomLayer.Size = new System.Drawing.Size(189, 303);
		this.lbCustomLayer.TabIndex = 1;
		this.lbCustomLayer.DoubleClick += new System.EventHandler(lbCustomLayer_DoubleClick);
		this.lbChannels.FormattingEnabled = true;
		this.lbChannels.Location = new System.Drawing.Point(3, 36);
		this.lbChannels.Name = "lbChannels";
		this.lbChannels.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.lbChannels.Size = new System.Drawing.Size(187, 303);
		this.lbChannels.TabIndex = 0;
		this.lbChannels.DoubleClick += new System.EventHandler(lbChannels_DoubleClick);
		this.tabFaderStrip.Controls.Add(this.label29);
		this.tabFaderStrip.Controls.Add(this.label30);
		this.tabFaderStrip.Controls.Add(this.btnCSDown);
		this.tabFaderStrip.Controls.Add(this.btnCSUp);
		this.tabFaderStrip.Controls.Add(this.btnCSRemove);
		this.tabFaderStrip.Controls.Add(this.btnCSAdd);
		this.tabFaderStrip.Controls.Add(this.lbStrip);
		this.tabFaderStrip.Controls.Add(this.lbControls);
		this.tabFaderStrip.Location = new System.Drawing.Point(4, 22);
		this.tabFaderStrip.Name = "tabFaderStrip";
		this.tabFaderStrip.Size = new System.Drawing.Size(574, 425);
		this.tabFaderStrip.TabIndex = 4;
		this.tabFaderStrip.Text = "Fader Strip";
		this.tabFaderStrip.UseVisualStyleBackColor = true;
		this.label29.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label29.Location = new System.Drawing.Point(3, 10);
		this.label29.Name = "label29";
		this.label29.Size = new System.Drawing.Size(189, 23);
		this.label29.TabIndex = 15;
		this.label29.Text = "Available Controls";
		this.label29.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label30.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label30.Location = new System.Drawing.Point(304, 10);
		this.label30.Name = "label30";
		this.label30.Size = new System.Drawing.Size(189, 23);
		this.label30.TabIndex = 14;
		this.label30.Text = "Channel Strip";
		this.label30.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnCSDown.Location = new System.Drawing.Point(210, 213);
		this.btnCSDown.Name = "btnCSDown";
		this.btnCSDown.Size = new System.Drawing.Size(75, 38);
		this.btnCSDown.TabIndex = 13;
		this.btnCSDown.Text = "Down";
		this.btnCSDown.UseVisualStyleBackColor = true;
		this.btnCSDown.Click += new System.EventHandler(btnCSDown_Click);
		this.btnCSUp.Location = new System.Drawing.Point(210, 169);
		this.btnCSUp.Name = "btnCSUp";
		this.btnCSUp.Size = new System.Drawing.Size(75, 38);
		this.btnCSUp.TabIndex = 12;
		this.btnCSUp.Text = "Up";
		this.btnCSUp.UseVisualStyleBackColor = true;
		this.btnCSUp.Click += new System.EventHandler(btnCSUp_Click);
		this.btnCSRemove.Location = new System.Drawing.Point(210, 80);
		this.btnCSRemove.Name = "btnCSRemove";
		this.btnCSRemove.Size = new System.Drawing.Size(75, 38);
		this.btnCSRemove.TabIndex = 11;
		this.btnCSRemove.Text = "<- Remove";
		this.btnCSRemove.UseVisualStyleBackColor = true;
		this.btnCSRemove.Click += new System.EventHandler(btnCSRemove_Click);
		this.btnCSAdd.Location = new System.Drawing.Point(210, 36);
		this.btnCSAdd.Name = "btnCSAdd";
		this.btnCSAdd.Size = new System.Drawing.Size(75, 38);
		this.btnCSAdd.TabIndex = 10;
		this.btnCSAdd.Text = "Add ->";
		this.btnCSAdd.UseVisualStyleBackColor = true;
		this.btnCSAdd.Click += new System.EventHandler(btnCSAdd_Click);
		this.lbStrip.FormattingEnabled = true;
		this.lbStrip.Location = new System.Drawing.Point(304, 36);
		this.lbStrip.Name = "lbStrip";
		this.lbStrip.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.lbStrip.Size = new System.Drawing.Size(189, 303);
		this.lbStrip.TabIndex = 9;
		this.lbStrip.DoubleClick += new System.EventHandler(lbStrip_DoubleClick);
		this.lbControls.FormattingEnabled = true;
		this.lbControls.Location = new System.Drawing.Point(3, 36);
		this.lbControls.Name = "lbControls";
		this.lbControls.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.lbControls.Size = new System.Drawing.Size(187, 303);
		this.lbControls.TabIndex = 8;
		this.lbControls.DoubleClick += new System.EventHandler(lbControls_DoubleClick);
		this.tabServer.Controls.Add(this.btnMsgClear);
		this.tabServer.Controls.Add(this.btnSendMsgSel);
		this.tabServer.Controls.Add(this.btnServerDrop);
		this.tabServer.Controls.Add(this.btnServerDisable);
		this.tabServer.Controls.Add(this.lvConnections);
		this.tabServer.Controls.Add(this.label52);
		this.tabServer.Controls.Add(this.txtMessage);
		this.tabServer.Controls.Add(this.btnSendMsg);
		this.tabServer.Controls.Add(this.lblServerStatus);
		this.tabServer.Controls.Add(this.label51);
		this.tabServer.Controls.Add(this.chkServerAutoStart);
		this.tabServer.Controls.Add(this.btnServerStart);
		this.tabServer.Controls.Add(this.txtServerPort);
		this.tabServer.Controls.Add(this.lblServerIP);
		this.tabServer.Controls.Add(this.label50);
		this.tabServer.Controls.Add(this.label49);
		this.tabServer.Controls.Add(this.label48);
		this.tabServer.Controls.Add(this.label47);
		this.tabServer.Location = new System.Drawing.Point(4, 22);
		this.tabServer.Name = "tabServer";
		this.tabServer.Size = new System.Drawing.Size(574, 425);
		this.tabServer.TabIndex = 6;
		this.tabServer.Text = "Server";
		this.tabServer.UseVisualStyleBackColor = true;
		this.btnMsgClear.Location = new System.Drawing.Point(418, 382);
		this.btnMsgClear.Name = "btnMsgClear";
		this.btnMsgClear.Size = new System.Drawing.Size(75, 38);
		this.btnMsgClear.TabIndex = 79;
		this.btnMsgClear.Text = "Clear";
		this.btnMsgClear.UseVisualStyleBackColor = true;
		this.btnMsgClear.Click += new System.EventHandler(btnMsgClear_Click);
		this.btnSendMsgSel.Location = new System.Drawing.Point(337, 334);
		this.btnSendMsgSel.Name = "btnSendMsgSel";
		this.btnSendMsgSel.Size = new System.Drawing.Size(75, 38);
		this.btnSendMsgSel.TabIndex = 78;
		this.btnSendMsgSel.Text = "Send";
		this.btnSendMsgSel.UseVisualStyleBackColor = true;
		this.btnSendMsgSel.Click += new System.EventHandler(btnSendMsgSel_Click);
		this.btnServerDrop.Location = new System.Drawing.Point(9, 334);
		this.btnServerDrop.Name = "btnServerDrop";
		this.btnServerDrop.Size = new System.Drawing.Size(75, 38);
		this.btnServerDrop.TabIndex = 77;
		this.btnServerDrop.Text = "Drop";
		this.btnServerDrop.UseVisualStyleBackColor = true;
		this.btnServerDrop.Click += new System.EventHandler(btnServerDrop_Click);
		this.btnServerDisable.Location = new System.Drawing.Point(90, 334);
		this.btnServerDisable.Name = "btnServerDisable";
		this.btnServerDisable.Size = new System.Drawing.Size(75, 38);
		this.btnServerDisable.TabIndex = 76;
		this.btnServerDisable.Text = "Disable";
		this.btnServerDisable.UseVisualStyleBackColor = true;
		this.btnServerDisable.Click += new System.EventHandler(btnServerDisable_Click);
		this.lvConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[5] { this.colId, this.colName, this.colIp, this.colClient, this.colSince });
		this.lvConnections.FullRowSelect = true;
		this.lvConnections.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
		this.lvConnections.HideSelection = false;
		this.lvConnections.Location = new System.Drawing.Point(9, 203);
		this.lvConnections.Name = "lvConnections";
		this.lvConnections.Size = new System.Drawing.Size(484, 125);
		this.lvConnections.TabIndex = 74;
		this.lvConnections.UseCompatibleStateImageBehavior = false;
		this.lvConnections.View = System.Windows.Forms.View.Details;
		this.colId.Text = "ID";
		this.colId.Width = 35;
		this.colName.Text = "User Name";
		this.colName.Width = 93;
		this.colIp.Text = "IP Address";
		this.colIp.Width = 101;
		this.colClient.Text = "Remote Version";
		this.colClient.Width = 102;
		this.colSince.Text = "Connected";
		this.colSince.Width = 142;
		this.label52.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label52.Location = new System.Drawing.Point(0, 389);
		this.label52.Name = "label52";
		this.label52.Size = new System.Drawing.Size(130, 24);
		this.label52.TabIndex = 73;
		this.label52.Text = "Send Message:";
		this.label52.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.txtMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.txtMessage.Location = new System.Drawing.Point(136, 390);
		this.txtMessage.Name = "txtMessage";
		this.txtMessage.Size = new System.Drawing.Size(276, 22);
		this.txtMessage.TabIndex = 72;
		this.btnSendMsg.Location = new System.Drawing.Point(418, 334);
		this.btnSendMsg.Name = "btnSendMsg";
		this.btnSendMsg.Size = new System.Drawing.Size(75, 38);
		this.btnSendMsg.TabIndex = 71;
		this.btnSendMsg.Text = "Send To All";
		this.btnSendMsg.UseVisualStyleBackColor = true;
		this.btnSendMsg.Click += new System.EventHandler(btnSendMsg_Click);
		this.lblServerStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblServerStatus.Location = new System.Drawing.Point(322, 132);
		this.lblServerStatus.Name = "lblServerStatus";
		this.lblServerStatus.Size = new System.Drawing.Size(114, 24);
		this.lblServerStatus.TabIndex = 70;
		this.lblServerStatus.Text = "Status";
		this.lblServerStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label51.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label51.Location = new System.Drawing.Point(322, 112);
		this.label51.Name = "label51";
		this.label51.Size = new System.Drawing.Size(114, 24);
		this.label51.TabIndex = 69;
		this.label51.Text = "Status";
		this.label51.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.chkServerAutoStart.Location = new System.Drawing.Point(202, 161);
		this.chkServerAutoStart.Name = "chkServerAutoStart";
		this.chkServerAutoStart.Size = new System.Drawing.Size(134, 36);
		this.chkServerAutoStart.TabIndex = 68;
		this.chkServerAutoStart.Text = "Start Server when 01vmix Starts";
		this.chkServerAutoStart.UseVisualStyleBackColor = true;
		this.btnServerStart.Location = new System.Drawing.Point(342, 159);
		this.btnServerStart.Name = "btnServerStart";
		this.btnServerStart.Size = new System.Drawing.Size(75, 38);
		this.btnServerStart.TabIndex = 67;
		this.btnServerStart.Text = "Start";
		this.btnServerStart.UseVisualStyleBackColor = true;
		this.btnServerStart.Click += new System.EventHandler(btnServerStart_Click);
		this.txtServerPort.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.txtServerPort.Location = new System.Drawing.Point(205, 133);
		this.txtServerPort.Name = "txtServerPort";
		this.txtServerPort.Size = new System.Drawing.Size(89, 22);
		this.txtServerPort.TabIndex = 66;
		this.txtServerPort.Text = "12345";
		this.lblServerIP.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblServerIP.Location = new System.Drawing.Point(2, 136);
		this.lblServerIP.Name = "lblServerIP";
		this.lblServerIP.Size = new System.Drawing.Size(194, 108);
		this.lblServerIP.TabIndex = 65;
		this.lblServerIP.Text = "123.456.789.0";
		this.lblServerIP.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.label50.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label50.Location = new System.Drawing.Point(202, 112);
		this.label50.Name = "label50";
		this.label50.Size = new System.Drawing.Size(92, 24);
		this.label50.TabIndex = 64;
		this.label50.Text = "Port";
		this.label50.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label49.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label49.Location = new System.Drawing.Point(2, 112);
		this.label49.Name = "label49";
		this.label49.Size = new System.Drawing.Size(194, 24);
		this.label49.TabIndex = 63;
		this.label49.Text = "IP Address";
		this.label49.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label48.Location = new System.Drawing.Point(6, 29);
		this.label48.Name = "label48";
		this.label48.Size = new System.Drawing.Size(507, 83);
		this.label48.TabIndex = 62;
		this.label48.Text = resources.GetString("label48.Text");
		this.label47.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label47.Location = new System.Drawing.Point(0, 5);
		this.label47.Name = "label47";
		this.label47.Size = new System.Drawing.Size(493, 24);
		this.label47.TabIndex = 61;
		this.label47.Text = "Server Configuration";
		this.label47.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.tabUsers.Controls.Add(this.chkChan8);
		this.tabUsers.Controls.Add(this.chkMtx7);
		this.tabUsers.Controls.Add(this.chkMtx6);
		this.tabUsers.Controls.Add(this.chkMtx5);
		this.tabUsers.Controls.Add(this.chkMtx4);
		this.tabUsers.Controls.Add(this.chkMtx3);
		this.tabUsers.Controls.Add(this.chkMtx2);
		this.tabUsers.Controls.Add(this.chkMtx1);
		this.tabUsers.Controls.Add(this.chkMtx0);
		this.tabUsers.Controls.Add(this.label56);
		this.tabUsers.Controls.Add(this.chkMix23);
		this.tabUsers.Controls.Add(this.chkMix22);
		this.tabUsers.Controls.Add(this.chkMix21);
		this.tabUsers.Controls.Add(this.chkMix20);
		this.tabUsers.Controls.Add(this.chkMix19);
		this.tabUsers.Controls.Add(this.chkMix18);
		this.tabUsers.Controls.Add(this.chkMix17);
		this.tabUsers.Controls.Add(this.chkMix16);
		this.tabUsers.Controls.Add(this.chkChanSTIN);
		this.tabUsers.Controls.Add(this.chkChan7);
		this.tabUsers.Controls.Add(this.chkChan6);
		this.tabUsers.Controls.Add(this.chkChan5);
		this.tabUsers.Controls.Add(this.chkChan4);
		this.tabUsers.Controls.Add(this.chkChan3);
		this.tabUsers.Controls.Add(this.chkChan2);
		this.tabUsers.Controls.Add(this.chkChan1);
		this.tabUsers.Controls.Add(this.chkChan0);
		this.tabUsers.Controls.Add(this.label55);
		this.tabUsers.Controls.Add(this.label54);
		this.tabUsers.Controls.Add(this.chkRack);
		this.tabUsers.Controls.Add(this.chkMaster);
		this.tabUsers.Controls.Add(this.btnNone);
		this.tabUsers.Controls.Add(this.btnPermAll);
		this.tabUsers.Controls.Add(this.btnDelUser);
		this.tabUsers.Controls.Add(this.label46);
		this.tabUsers.Controls.Add(this.label45);
		this.tabUsers.Controls.Add(this.lblAddEditUser);
		this.tabUsers.Controls.Add(this.lblPermissions);
		this.tabUsers.Controls.Add(this.label44);
		this.tabUsers.Controls.Add(this.chkChannel);
		this.tabUsers.Controls.Add(this.chkMainMix);
		this.tabUsers.Controls.Add(this.chkMix15);
		this.tabUsers.Controls.Add(this.chkMix14);
		this.tabUsers.Controls.Add(this.chkMix13);
		this.tabUsers.Controls.Add(this.chkMix12);
		this.tabUsers.Controls.Add(this.chkMix11);
		this.tabUsers.Controls.Add(this.chkMix10);
		this.tabUsers.Controls.Add(this.chkMix9);
		this.tabUsers.Controls.Add(this.chkMix8);
		this.tabUsers.Controls.Add(this.chkMix7);
		this.tabUsers.Controls.Add(this.chkMix6);
		this.tabUsers.Controls.Add(this.chkMix5);
		this.tabUsers.Controls.Add(this.chkMix4);
		this.tabUsers.Controls.Add(this.chkMix3);
		this.tabUsers.Controls.Add(this.chkMix2);
		this.tabUsers.Controls.Add(this.chkMix1);
		this.tabUsers.Controls.Add(this.chkMix0);
		this.tabUsers.Controls.Add(this.chkEnabled);
		this.tabUsers.Controls.Add(this.txtPassword);
		this.tabUsers.Controls.Add(this.txtUserName);
		this.tabUsers.Controls.Add(this.label43);
		this.tabUsers.Controls.Add(this.label42);
		this.tabUsers.Controls.Add(this.btnNewUser);
		this.tabUsers.Controls.Add(this.btnUserSave);
		this.tabUsers.Controls.Add(this.lbUsers);
		this.tabUsers.Location = new System.Drawing.Point(4, 22);
		this.tabUsers.Name = "tabUsers";
		this.tabUsers.Padding = new System.Windows.Forms.Padding(3);
		this.tabUsers.Size = new System.Drawing.Size(574, 425);
		this.tabUsers.TabIndex = 5;
		this.tabUsers.Text = "Users";
		this.tabUsers.UseVisualStyleBackColor = true;
		this.chkChan8.Location = new System.Drawing.Point(205, 352);
		this.chkChan8.Name = "chkChan8";
		this.chkChan8.Size = new System.Drawing.Size(70, 16);
		this.chkChan8.TabIndex = 97;
		this.chkChan8.Text = "Ch 65-72";
		this.chkChan8.UseVisualStyleBackColor = true;
		this.chkMtx7.Location = new System.Drawing.Point(527, 264);
		this.chkMtx7.Name = "chkMtx7";
		this.chkMtx7.Size = new System.Drawing.Size(40, 16);
		this.chkMtx7.TabIndex = 96;
		this.chkMtx7.Text = "8";
		this.chkMtx7.UseVisualStyleBackColor = true;
		this.chkMtx6.Location = new System.Drawing.Point(481, 264);
		this.chkMtx6.Name = "chkMtx6";
		this.chkMtx6.Size = new System.Drawing.Size(40, 16);
		this.chkMtx6.TabIndex = 95;
		this.chkMtx6.Text = "7";
		this.chkMtx6.UseVisualStyleBackColor = true;
		this.chkMtx5.Location = new System.Drawing.Point(435, 264);
		this.chkMtx5.Name = "chkMtx5";
		this.chkMtx5.Size = new System.Drawing.Size(40, 16);
		this.chkMtx5.TabIndex = 94;
		this.chkMtx5.Text = "6";
		this.chkMtx5.UseVisualStyleBackColor = true;
		this.chkMtx4.Location = new System.Drawing.Point(389, 264);
		this.chkMtx4.Name = "chkMtx4";
		this.chkMtx4.Size = new System.Drawing.Size(40, 16);
		this.chkMtx4.TabIndex = 93;
		this.chkMtx4.Text = "5";
		this.chkMtx4.UseVisualStyleBackColor = true;
		this.chkMtx3.Location = new System.Drawing.Point(343, 264);
		this.chkMtx3.Name = "chkMtx3";
		this.chkMtx3.Size = new System.Drawing.Size(40, 16);
		this.chkMtx3.TabIndex = 92;
		this.chkMtx3.Text = "4";
		this.chkMtx3.UseVisualStyleBackColor = true;
		this.chkMtx2.Location = new System.Drawing.Point(297, 264);
		this.chkMtx2.Name = "chkMtx2";
		this.chkMtx2.Size = new System.Drawing.Size(40, 16);
		this.chkMtx2.TabIndex = 91;
		this.chkMtx2.Text = "3";
		this.chkMtx2.UseVisualStyleBackColor = true;
		this.chkMtx1.Location = new System.Drawing.Point(251, 264);
		this.chkMtx1.Name = "chkMtx1";
		this.chkMtx1.Size = new System.Drawing.Size(40, 16);
		this.chkMtx1.TabIndex = 90;
		this.chkMtx1.Text = "2";
		this.chkMtx1.UseVisualStyleBackColor = true;
		this.chkMtx0.Location = new System.Drawing.Point(205, 264);
		this.chkMtx0.Name = "chkMtx0";
		this.chkMtx0.Size = new System.Drawing.Size(40, 16);
		this.chkMtx0.TabIndex = 89;
		this.chkMtx0.Text = "1";
		this.chkMtx0.UseVisualStyleBackColor = true;
		this.label56.AutoEllipsis = true;
		this.label56.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label56.Location = new System.Drawing.Point(202, 239);
		this.label56.Name = "label56";
		this.label56.Size = new System.Drawing.Size(362, 22);
		this.label56.TabIndex = 88;
		this.label56.Text = "Matrix Permissions";
		this.label56.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.chkMix23.Location = new System.Drawing.Point(527, 220);
		this.chkMix23.Name = "chkMix23";
		this.chkMix23.Size = new System.Drawing.Size(40, 16);
		this.chkMix23.TabIndex = 87;
		this.chkMix23.Text = "24";
		this.chkMix23.UseVisualStyleBackColor = true;
		this.chkMix22.Location = new System.Drawing.Point(481, 220);
		this.chkMix22.Name = "chkMix22";
		this.chkMix22.Size = new System.Drawing.Size(40, 16);
		this.chkMix22.TabIndex = 86;
		this.chkMix22.Text = "23";
		this.chkMix22.UseVisualStyleBackColor = true;
		this.chkMix21.Location = new System.Drawing.Point(435, 220);
		this.chkMix21.Name = "chkMix21";
		this.chkMix21.Size = new System.Drawing.Size(40, 16);
		this.chkMix21.TabIndex = 85;
		this.chkMix21.Text = "22";
		this.chkMix21.UseVisualStyleBackColor = true;
		this.chkMix20.Location = new System.Drawing.Point(389, 220);
		this.chkMix20.Name = "chkMix20";
		this.chkMix20.Size = new System.Drawing.Size(40, 16);
		this.chkMix20.TabIndex = 84;
		this.chkMix20.Text = "21";
		this.chkMix20.UseVisualStyleBackColor = true;
		this.chkMix19.Location = new System.Drawing.Point(343, 220);
		this.chkMix19.Name = "chkMix19";
		this.chkMix19.Size = new System.Drawing.Size(40, 16);
		this.chkMix19.TabIndex = 83;
		this.chkMix19.Text = "20";
		this.chkMix19.UseVisualStyleBackColor = true;
		this.chkMix18.Location = new System.Drawing.Point(297, 220);
		this.chkMix18.Name = "chkMix18";
		this.chkMix18.Size = new System.Drawing.Size(40, 16);
		this.chkMix18.TabIndex = 82;
		this.chkMix18.Text = "19";
		this.chkMix18.UseVisualStyleBackColor = true;
		this.chkMix17.Location = new System.Drawing.Point(251, 220);
		this.chkMix17.Name = "chkMix17";
		this.chkMix17.Size = new System.Drawing.Size(40, 16);
		this.chkMix17.TabIndex = 81;
		this.chkMix17.Text = "18";
		this.chkMix17.UseVisualStyleBackColor = true;
		this.chkMix16.Location = new System.Drawing.Point(205, 220);
		this.chkMix16.Name = "chkMix16";
		this.chkMix16.Size = new System.Drawing.Size(40, 16);
		this.chkMix16.TabIndex = 80;
		this.chkMix16.Text = "17";
		this.chkMix16.UseVisualStyleBackColor = true;
		this.chkChanSTIN.Location = new System.Drawing.Point(297, 352);
		this.chkChanSTIN.Name = "chkChanSTIN";
		this.chkChanSTIN.Size = new System.Drawing.Size(70, 16);
		this.chkChanSTIN.TabIndex = 79;
		this.chkChanSTIN.Text = "ST-IN";
		this.chkChanSTIN.UseVisualStyleBackColor = true;
		this.chkChan7.Location = new System.Drawing.Point(494, 330);
		this.chkChan7.Name = "chkChan7";
		this.chkChan7.Size = new System.Drawing.Size(70, 16);
		this.chkChan7.TabIndex = 78;
		this.chkChan7.Text = "Ch 57-64";
		this.chkChan7.UseVisualStyleBackColor = true;
		this.chkChan6.Location = new System.Drawing.Point(390, 330);
		this.chkChan6.Name = "chkChan6";
		this.chkChan6.Size = new System.Drawing.Size(70, 16);
		this.chkChan6.TabIndex = 77;
		this.chkChan6.Text = "Ch 49-56";
		this.chkChan6.UseVisualStyleBackColor = true;
		this.chkChan5.Location = new System.Drawing.Point(297, 330);
		this.chkChan5.Name = "chkChan5";
		this.chkChan5.Size = new System.Drawing.Size(70, 16);
		this.chkChan5.TabIndex = 76;
		this.chkChan5.Text = "Ch 41-48";
		this.chkChan5.UseVisualStyleBackColor = true;
		this.chkChan4.Location = new System.Drawing.Point(205, 330);
		this.chkChan4.Name = "chkChan4";
		this.chkChan4.Size = new System.Drawing.Size(70, 16);
		this.chkChan4.TabIndex = 75;
		this.chkChan4.Text = "Ch 33-40";
		this.chkChan4.UseVisualStyleBackColor = true;
		this.chkChan3.Location = new System.Drawing.Point(494, 308);
		this.chkChan3.Name = "chkChan3";
		this.chkChan3.Size = new System.Drawing.Size(70, 16);
		this.chkChan3.TabIndex = 74;
		this.chkChan3.Text = "Ch 25-32";
		this.chkChan3.UseVisualStyleBackColor = true;
		this.chkChan2.Location = new System.Drawing.Point(390, 308);
		this.chkChan2.Name = "chkChan2";
		this.chkChan2.Size = new System.Drawing.Size(70, 16);
		this.chkChan2.TabIndex = 73;
		this.chkChan2.Text = "Ch 17-24";
		this.chkChan2.UseVisualStyleBackColor = true;
		this.chkChan1.Location = new System.Drawing.Point(297, 308);
		this.chkChan1.Name = "chkChan1";
		this.chkChan1.Size = new System.Drawing.Size(70, 16);
		this.chkChan1.TabIndex = 72;
		this.chkChan1.Text = "Ch 9-16";
		this.chkChan1.UseVisualStyleBackColor = true;
		this.chkChan0.Location = new System.Drawing.Point(205, 308);
		this.chkChan0.Name = "chkChan0";
		this.chkChan0.Size = new System.Drawing.Size(70, 16);
		this.chkChan0.TabIndex = 71;
		this.chkChan0.Text = "Ch 1-8";
		this.chkChan0.UseVisualStyleBackColor = true;
		this.label55.AutoEllipsis = true;
		this.label55.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label55.Location = new System.Drawing.Point(205, 283);
		this.label55.Name = "label55";
		this.label55.Size = new System.Drawing.Size(359, 22);
		this.label55.TabIndex = 70;
		this.label55.Text = "Per-Channel Permissions";
		this.label55.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label54.AutoEllipsis = true;
		this.label54.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label54.Location = new System.Drawing.Point(202, 151);
		this.label54.Name = "label54";
		this.label54.Size = new System.Drawing.Size(362, 22);
		this.label54.TabIndex = 69;
		this.label54.Text = "Mix Permissions";
		this.label54.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.chkRack.AutoSize = true;
		this.chkRack.Location = new System.Drawing.Point(353, 111);
		this.chkRack.Name = "chkRack";
		this.chkRack.Size = new System.Drawing.Size(80, 17);
		this.chkRack.TabIndex = 68;
		this.chkRack.Text = "Rack/GEQ";
		this.chkRack.UseVisualStyleBackColor = true;
		this.chkMaster.AutoSize = true;
		this.chkMaster.Location = new System.Drawing.Point(205, 131);
		this.chkMaster.Name = "chkMaster";
		this.chkMaster.Size = new System.Drawing.Size(93, 17);
		this.chkMaster.TabIndex = 67;
		this.chkMaster.Text = "Master Faders";
		this.chkMaster.UseVisualStyleBackColor = true;
		this.btnNone.Location = new System.Drawing.Point(443, 79);
		this.btnNone.Name = "btnNone";
		this.btnNone.Size = new System.Drawing.Size(47, 26);
		this.btnNone.TabIndex = 66;
		this.btnNone.Text = "None";
		this.btnNone.UseVisualStyleBackColor = true;
		this.btnNone.Click += new System.EventHandler(btnNone_Click);
		this.btnPermAll.Location = new System.Drawing.Point(390, 79);
		this.btnPermAll.Name = "btnPermAll";
		this.btnPermAll.Size = new System.Drawing.Size(47, 26);
		this.btnPermAll.TabIndex = 65;
		this.btnPermAll.Text = "All";
		this.btnPermAll.UseVisualStyleBackColor = true;
		this.btnPermAll.Click += new System.EventHandler(btnPermAll_Click);
		this.btnDelUser.Location = new System.Drawing.Point(115, 314);
		this.btnDelUser.Name = "btnDelUser";
		this.btnDelUser.Size = new System.Drawing.Size(75, 38);
		this.btnDelUser.TabIndex = 63;
		this.btnDelUser.Text = "Delete User";
		this.btnDelUser.UseVisualStyleBackColor = true;
		this.btnDelUser.Click += new System.EventHandler(btnDelUser_Click);
		this.label46.Location = new System.Drawing.Point(238, 381);
		this.label46.Name = "label46";
		this.label46.Size = new System.Drawing.Size(133, 38);
		this.label46.TabIndex = 62;
		this.label46.Text = "Changes to permissions are enforced immediately";
		this.label46.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label45.Location = new System.Drawing.Point(0, 293);
		this.label45.Name = "label45";
		this.label45.Size = new System.Drawing.Size(190, 15);
		this.label45.TabIndex = 61;
		this.label45.Text = "Click user above to edit permissions";
		this.label45.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblAddEditUser.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblAddEditUser.Location = new System.Drawing.Point(209, 13);
		this.lblAddEditUser.Name = "lblAddEditUser";
		this.lblAddEditUser.Size = new System.Drawing.Size(228, 16);
		this.lblAddEditUser.TabIndex = 60;
		this.lblAddEditUser.Text = "Add/Edit User";
		this.lblAddEditUser.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblPermissions.AutoEllipsis = true;
		this.lblPermissions.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPermissions.Location = new System.Drawing.Point(196, 79);
		this.lblPermissions.Name = "lblPermissions";
		this.lblPermissions.Size = new System.Drawing.Size(188, 26);
		this.lblPermissions.TabIndex = 59;
		this.lblPermissions.Text = "'s Permissions";
		this.lblPermissions.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label44.Location = new System.Drawing.Point(326, 32);
		this.label44.Name = "label44";
		this.label44.Size = new System.Drawing.Size(89, 13);
		this.label44.TabIndex = 58;
		this.label44.Text = "Password";
		this.label44.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.chkChannel.AutoSize = true;
		this.chkChannel.Location = new System.Drawing.Point(205, 108);
		this.chkChannel.Name = "chkChannel";
		this.chkChannel.Size = new System.Drawing.Size(103, 17);
		this.chkChannel.TabIndex = 57;
		this.chkChannel.Text = "Channel Params";
		this.chkChannel.UseVisualStyleBackColor = true;
		this.chkMainMix.AutoSize = true;
		this.chkMainMix.Location = new System.Drawing.Point(353, 131);
		this.chkMainMix.Name = "chkMainMix";
		this.chkMainMix.Size = new System.Drawing.Size(99, 17);
		this.chkMainMix.TabIndex = 56;
		this.chkMainMix.Text = "Main Mix (FOH)";
		this.chkMainMix.UseVisualStyleBackColor = true;
		this.chkMix15.Location = new System.Drawing.Point(527, 198);
		this.chkMix15.Name = "chkMix15";
		this.chkMix15.Size = new System.Drawing.Size(40, 16);
		this.chkMix15.TabIndex = 55;
		this.chkMix15.Text = "16";
		this.chkMix15.UseVisualStyleBackColor = true;
		this.chkMix14.Location = new System.Drawing.Point(481, 198);
		this.chkMix14.Name = "chkMix14";
		this.chkMix14.Size = new System.Drawing.Size(40, 16);
		this.chkMix14.TabIndex = 54;
		this.chkMix14.Text = "15";
		this.chkMix14.UseVisualStyleBackColor = true;
		this.chkMix13.Location = new System.Drawing.Point(435, 198);
		this.chkMix13.Name = "chkMix13";
		this.chkMix13.Size = new System.Drawing.Size(40, 16);
		this.chkMix13.TabIndex = 53;
		this.chkMix13.Text = "14";
		this.chkMix13.UseVisualStyleBackColor = true;
		this.chkMix12.Location = new System.Drawing.Point(389, 198);
		this.chkMix12.Name = "chkMix12";
		this.chkMix12.Size = new System.Drawing.Size(40, 16);
		this.chkMix12.TabIndex = 52;
		this.chkMix12.Text = "13";
		this.chkMix12.UseVisualStyleBackColor = true;
		this.chkMix11.Location = new System.Drawing.Point(343, 198);
		this.chkMix11.Name = "chkMix11";
		this.chkMix11.Size = new System.Drawing.Size(40, 16);
		this.chkMix11.TabIndex = 51;
		this.chkMix11.Text = "12";
		this.chkMix11.UseVisualStyleBackColor = true;
		this.chkMix10.Location = new System.Drawing.Point(297, 198);
		this.chkMix10.Name = "chkMix10";
		this.chkMix10.Size = new System.Drawing.Size(40, 16);
		this.chkMix10.TabIndex = 50;
		this.chkMix10.Text = "11";
		this.chkMix10.UseVisualStyleBackColor = true;
		this.chkMix9.Location = new System.Drawing.Point(251, 198);
		this.chkMix9.Name = "chkMix9";
		this.chkMix9.Size = new System.Drawing.Size(40, 16);
		this.chkMix9.TabIndex = 49;
		this.chkMix9.Text = "10";
		this.chkMix9.UseVisualStyleBackColor = true;
		this.chkMix8.Location = new System.Drawing.Point(205, 198);
		this.chkMix8.Name = "chkMix8";
		this.chkMix8.Size = new System.Drawing.Size(40, 16);
		this.chkMix8.TabIndex = 48;
		this.chkMix8.Text = "9";
		this.chkMix8.UseVisualStyleBackColor = true;
		this.chkMix7.Location = new System.Drawing.Point(527, 176);
		this.chkMix7.Name = "chkMix7";
		this.chkMix7.Size = new System.Drawing.Size(40, 16);
		this.chkMix7.TabIndex = 47;
		this.chkMix7.Text = "8";
		this.chkMix7.UseVisualStyleBackColor = true;
		this.chkMix6.Location = new System.Drawing.Point(481, 176);
		this.chkMix6.Name = "chkMix6";
		this.chkMix6.Size = new System.Drawing.Size(40, 16);
		this.chkMix6.TabIndex = 46;
		this.chkMix6.Text = "7";
		this.chkMix6.UseVisualStyleBackColor = true;
		this.chkMix5.Location = new System.Drawing.Point(435, 176);
		this.chkMix5.Name = "chkMix5";
		this.chkMix5.Size = new System.Drawing.Size(40, 16);
		this.chkMix5.TabIndex = 45;
		this.chkMix5.Text = "6";
		this.chkMix5.UseVisualStyleBackColor = true;
		this.chkMix4.Location = new System.Drawing.Point(389, 176);
		this.chkMix4.Name = "chkMix4";
		this.chkMix4.Size = new System.Drawing.Size(40, 16);
		this.chkMix4.TabIndex = 44;
		this.chkMix4.Text = "5";
		this.chkMix4.UseVisualStyleBackColor = true;
		this.chkMix3.Location = new System.Drawing.Point(343, 176);
		this.chkMix3.Name = "chkMix3";
		this.chkMix3.Size = new System.Drawing.Size(40, 16);
		this.chkMix3.TabIndex = 43;
		this.chkMix3.Text = "4";
		this.chkMix3.UseVisualStyleBackColor = true;
		this.chkMix2.Location = new System.Drawing.Point(297, 176);
		this.chkMix2.Name = "chkMix2";
		this.chkMix2.Size = new System.Drawing.Size(40, 16);
		this.chkMix2.TabIndex = 42;
		this.chkMix2.Text = "3";
		this.chkMix2.UseVisualStyleBackColor = true;
		this.chkMix1.Location = new System.Drawing.Point(251, 176);
		this.chkMix1.Name = "chkMix1";
		this.chkMix1.Size = new System.Drawing.Size(40, 16);
		this.chkMix1.TabIndex = 41;
		this.chkMix1.Text = "2";
		this.chkMix1.UseVisualStyleBackColor = true;
		this.chkMix0.Location = new System.Drawing.Point(205, 176);
		this.chkMix0.Name = "chkMix0";
		this.chkMix0.Size = new System.Drawing.Size(40, 16);
		this.chkMix0.TabIndex = 40;
		this.chkMix0.Text = "1";
		this.chkMix0.UseVisualStyleBackColor = true;
		this.chkEnabled.Location = new System.Drawing.Point(443, 48);
		this.chkEnabled.Name = "chkEnabled";
		this.chkEnabled.Size = new System.Drawing.Size(70, 25);
		this.chkEnabled.TabIndex = 39;
		this.chkEnabled.Text = "Enabled";
		this.chkEnabled.UseVisualStyleBackColor = true;
		this.txtPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.txtPassword.Location = new System.Drawing.Point(326, 48);
		this.txtPassword.Name = "txtPassword";
		this.txtPassword.PasswordChar = '*';
		this.txtPassword.Size = new System.Drawing.Size(111, 22);
		this.txtPassword.TabIndex = 38;
		this.txtPassword.Text = "Password";
		this.txtUserName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.txtUserName.Location = new System.Drawing.Point(209, 48);
		this.txtUserName.Name = "txtUserName";
		this.txtUserName.Size = new System.Drawing.Size(111, 22);
		this.txtUserName.TabIndex = 37;
		this.txtUserName.Text = "User Name";
		this.txtUserName.TextChanged += new System.EventHandler(txtUserName_TextChanged);
		this.label43.Location = new System.Drawing.Point(209, 32);
		this.label43.Name = "label43";
		this.label43.Size = new System.Drawing.Size(89, 13);
		this.label43.TabIndex = 36;
		this.label43.Text = "User Name";
		this.label43.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.label42.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label42.Location = new System.Drawing.Point(3, 10);
		this.label42.Name = "label42";
		this.label42.Size = new System.Drawing.Size(189, 23);
		this.label42.TabIndex = 11;
		this.label42.Text = "Remote Control Users";
		this.label42.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnNewUser.Location = new System.Drawing.Point(6, 314);
		this.btnNewUser.Name = "btnNewUser";
		this.btnNewUser.Size = new System.Drawing.Size(75, 38);
		this.btnNewUser.TabIndex = 10;
		this.btnNewUser.Text = "New User";
		this.btnNewUser.UseVisualStyleBackColor = true;
		this.btnNewUser.Click += new System.EventHandler(btnNewUser_Click);
		this.btnUserSave.Location = new System.Drawing.Point(377, 381);
		this.btnUserSave.Name = "btnUserSave";
		this.btnUserSave.Size = new System.Drawing.Size(75, 38);
		this.btnUserSave.TabIndex = 9;
		this.btnUserSave.Text = "Apply";
		this.btnUserSave.UseVisualStyleBackColor = true;
		this.btnUserSave.Click += new System.EventHandler(btnUserSave_Click);
		this.lbUsers.FormattingEnabled = true;
		this.lbUsers.Location = new System.Drawing.Point(3, 36);
		this.lbUsers.Name = "lbUsers";
		this.lbUsers.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.lbUsers.Size = new System.Drawing.Size(187, 251);
		this.lbUsers.TabIndex = 8;
		this.lbUsers.SelectedIndexChanged += new System.EventHandler(lbUsers_SelectedIndexChanged);
		this.tabAbout.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.tabAbout.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
		this.tabAbout.Controls.Add(this.label8);
		this.tabAbout.Controls.Add(this.btnUpdate);
		this.tabAbout.Controls.Add(this.lblKey);
		this.tabAbout.Controls.Add(this.btnRegister);
		this.tabAbout.Controls.Add(this.lblOwner);
		this.tabAbout.Controls.Add(this.label9);
		this.tabAbout.Controls.Add(this.lblVersion);
		this.tabAbout.Controls.Add(this.lblProduct);
		this.tabAbout.Controls.Add(this.lblBrand);
		this.tabAbout.Location = new System.Drawing.Point(4, 22);
		this.tabAbout.Name = "tabAbout";
		this.tabAbout.Size = new System.Drawing.Size(574, 425);
		this.tabAbout.TabIndex = 2;
		this.tabAbout.Text = "About";
		this.tabAbout.UseVisualStyleBackColor = true;
		this.label8.ForeColor = System.Drawing.Color.WhiteSmoke;
		this.label8.Location = new System.Drawing.Point(5, 274);
		this.label8.Name = "label8";
		this.label8.Size = new System.Drawing.Size(487, 17);
		this.label8.TabIndex = 8;
		this.label8.Text = "Copyright © 2010-2015, Molten Logic LLC -- Design/Programming: Adam Ellsworth";
		this.label8.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.btnUpdate.BackColor = System.Drawing.SystemColors.Control;
		this.btnUpdate.Location = new System.Drawing.Point(311, 134);
		this.btnUpdate.Name = "btnUpdate";
		this.btnUpdate.Size = new System.Drawing.Size(132, 30);
		this.btnUpdate.TabIndex = 21;
		this.btnUpdate.Text = "Check for Updates";
		this.btnUpdate.UseVisualStyleBackColor = false;
		this.btnUpdate.Click += new System.EventHandler(btnUpdate_Click);
		this.lblKey.AutoSize = true;
		this.lblKey.ForeColor = System.Drawing.Color.White;
		this.lblKey.Location = new System.Drawing.Point(309, 63);
		this.lblKey.Name = "lblKey";
		this.lblKey.Size = new System.Drawing.Size(46, 13);
		this.lblKey.TabIndex = 6;
		this.lblKey.Text = "01vmix";
		this.btnRegister.BackColor = System.Drawing.SystemColors.Control;
		this.btnRegister.Location = new System.Drawing.Point(311, 96);
		this.btnRegister.Name = "btnRegister";
		this.btnRegister.Size = new System.Drawing.Size(132, 30);
		this.btnRegister.TabIndex = 20;
		this.btnRegister.Text = "Registration";
		this.btnRegister.UseVisualStyleBackColor = false;
		this.btnRegister.Click += new System.EventHandler(btnRegister_Click);
		this.lblOwner.AutoSize = true;
		this.lblOwner.ForeColor = System.Drawing.Color.White;
		this.lblOwner.Location = new System.Drawing.Point(309, 50);
		this.lblOwner.Name = "lblOwner";
		this.lblOwner.Size = new System.Drawing.Size(46, 13);
		this.lblOwner.TabIndex = 4;
		this.lblOwner.Text = "01vmix";
		this.label9.ForeColor = System.Drawing.Color.White;
		this.label9.Location = new System.Drawing.Point(308, 167);
		this.label9.Name = "label9";
		this.label9.Size = new System.Drawing.Size(184, 88);
		this.label9.TabIndex = 3;
		this.label9.Text = resources.GetString("label9.Text");
		this.label9.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
		this.lblVersion.AutoSize = true;
		this.lblVersion.ForeColor = System.Drawing.Color.White;
		this.lblVersion.Location = new System.Drawing.Point(309, 37);
		this.lblVersion.Name = "lblVersion";
		this.lblVersion.Size = new System.Drawing.Size(46, 13);
		this.lblVersion.TabIndex = 2;
		this.lblVersion.Text = "01vmix";
		this.lblProduct.AutoSize = true;
		this.lblProduct.ForeColor = System.Drawing.Color.White;
		this.lblProduct.Location = new System.Drawing.Point(309, 24);
		this.lblProduct.Name = "lblProduct";
		this.lblProduct.Size = new System.Drawing.Size(46, 13);
		this.lblProduct.TabIndex = 1;
		this.lblProduct.Text = "01vmix";
		this.lblBrand.AutoSize = false;
		this.lblBrand.Font = new System.Drawing.Font("Arial", 28f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblBrand.ForeColor = System.Drawing.Color.White;
		this.lblBrand.Location = new System.Drawing.Point(5, 24);
		this.lblBrand.Name = "lblBrand";
		this.lblBrand.Size = new System.Drawing.Size(300, 231);
		this.lblBrand.TabIndex = 0;
		this.lblBrand.Text = "01VMIX";
		this.lblBrand.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblBrand.Click += new System.EventHandler(lblBrand_Click);
		this.lblBrand.DoubleClick += new System.EventHandler(lblBrand_DoubleClick);
		this.label6.AutoSize = true;
		this.label6.Location = new System.Drawing.Point(390, 483);
		this.label6.Name = "label6";
		this.label6.Size = new System.Drawing.Size(29, 13);
		this.label6.TabIndex = 78;
		this.label6.Text = "Gain";
		this.label6.Visible = false;
		this.txtGain.Location = new System.Drawing.Point(453, 480);
		this.txtGain.Name = "txtGain";
		this.txtGain.Size = new System.Drawing.Size(37, 20);
		this.txtGain.TabIndex = 77;
		this.txtGain.Visible = false;
		this.chkGO.AutoSize = true;
		this.chkGO.Checked = true;
		this.chkGO.CheckState = System.Windows.Forms.CheckState.Checked;
		this.chkGO.Location = new System.Drawing.Point(453, 584);
		this.chkGO.Name = "chkGO";
		this.chkGO.Size = new System.Drawing.Size(42, 17);
		this.chkGO.TabIndex = 76;
		this.chkGO.Text = "GO";
		this.chkGO.UseVisualStyleBackColor = true;
		this.chkGO.Visible = false;
		this.txtMeter.Location = new System.Drawing.Point(453, 558);
		this.txtMeter.Name = "txtMeter";
		this.txtMeter.Size = new System.Drawing.Size(37, 20);
		this.txtMeter.TabIndex = 75;
		this.txtMeter.Visible = false;
		this.txtQ.Location = new System.Drawing.Point(453, 532);
		this.txtQ.Name = "txtQ";
		this.txtQ.Size = new System.Drawing.Size(37, 20);
		this.txtQ.TabIndex = 74;
		this.txtQ.Visible = false;
		this.txtFreq.Location = new System.Drawing.Point(453, 506);
		this.txtFreq.Name = "txtFreq";
		this.txtFreq.Size = new System.Drawing.Size(37, 20);
		this.txtFreq.TabIndex = 73;
		this.txtFreq.Visible = false;
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(391, 561);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(34, 13);
		this.label4.TabIndex = 72;
		this.label4.Text = "Meter";
		this.label4.Visible = false;
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(391, 532);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(15, 13);
		this.label3.TabIndex = 71;
		this.label3.Text = "Q";
		this.label3.Visible = false;
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(391, 509);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(57, 13);
		this.label2.TabIndex = 70;
		this.label2.Text = "Frequency";
		this.label2.Visible = false;
		this.btnSweep.Location = new System.Drawing.Point(371, 584);
		this.btnSweep.Name = "btnSweep";
		this.btnSweep.Size = new System.Drawing.Size(75, 23);
		this.btnSweep.TabIndex = 69;
		this.btnSweep.Text = "Sweep";
		this.btnSweep.UseVisualStyleBackColor = true;
		this.btnSweep.Visible = false;
		this.btnSave.Location = new System.Drawing.Point(189, 469);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(75, 38);
		this.btnSave.TabIndex = 4;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Click += new System.EventHandler(btnSave_Click);
		this.btnCancel.Location = new System.Drawing.Point(280, 469);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 38);
		this.btnCancel.TabIndex = 69;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.tabPage1.Controls.Add(this.button1);
		this.tabPage1.Controls.Add(this.label13);
		this.tabPage1.Controls.Add(this.comboBox1);
		this.tabPage1.Controls.Add(this.label14);
		this.tabPage1.Controls.Add(this.button2);
		this.tabPage1.Controls.Add(this.listBox1);
		this.tabPage1.Controls.Add(this.label15);
		this.tabPage1.Controls.Add(this.listBox2);
		this.tabPage1.Controls.Add(this.label16);
		this.tabPage1.Location = new System.Drawing.Point(4, 22);
		this.tabPage1.Name = "tabPage1";
		this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
		this.tabPage1.Size = new System.Drawing.Size(496, 275);
		this.tabPage1.TabIndex = 0;
		this.tabPage1.Text = "Devices";
		this.tabPage1.UseVisualStyleBackColor = true;
		this.button1.Location = new System.Drawing.Point(247, 212);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 82;
		this.button1.Text = "Get Text";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Visible = false;
		this.label13.AutoSize = true;
		this.label13.Location = new System.Drawing.Point(251, 145);
		this.label13.Name = "label13";
		this.label13.Size = new System.Drawing.Size(72, 13);
		this.label13.TabIndex = 81;
		this.label13.Text = "Console Type";
		this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox1.FormattingEnabled = true;
		this.comboBox1.Items.AddRange(new object[5] { "Yamaha LS9-16", "Yamaha LS9-32", "Yamaha M7CL-32", "Yamaha M7CL-48", "Yamaha 01V96" });
		this.comboBox1.Location = new System.Drawing.Point(251, 161);
		this.comboBox1.Name = "comboBox1";
		this.comboBox1.Size = new System.Drawing.Size(147, 21);
		this.comboBox1.TabIndex = 80;
		this.label14.Location = new System.Drawing.Point(248, 30);
		this.label14.Name = "label14";
		this.label14.Size = new System.Drawing.Size(239, 62);
		this.label14.TabIndex = 79;
		this.label14.Text = "If 01vmix will not sync via IP after recent network problems or a software crash, the DME MIDI driver may not be responding. Click below to attempt to restart. (Will not affect the console.)";
		this.button2.BackColor = System.Drawing.SystemColors.Control;
		this.button2.Location = new System.Drawing.Point(251, 95);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(236, 30);
		this.button2.TabIndex = 67;
		this.button2.Text = "Restart DME Driver";
		this.button2.UseVisualStyleBackColor = false;
		this.listBox1.FormattingEnabled = true;
		this.listBox1.Location = new System.Drawing.Point(23, 30);
		this.listBox1.Name = "listBox1";
		this.listBox1.Size = new System.Drawing.Size(219, 95);
		this.listBox1.TabIndex = 2;
		this.label15.AutoSize = true;
		this.label15.Location = new System.Drawing.Point(23, 145);
		this.label15.Name = "label15";
		this.label15.Size = new System.Drawing.Size(76, 13);
		this.label15.TabIndex = 66;
		this.label15.Text = "Output Device";
		this.listBox2.FormattingEnabled = true;
		this.listBox2.Location = new System.Drawing.Point(23, 161);
		this.listBox2.Name = "listBox2";
		this.listBox2.Size = new System.Drawing.Size(219, 95);
		this.listBox2.TabIndex = 3;
		this.label16.AutoSize = true;
		this.label16.Location = new System.Drawing.Point(23, 14);
		this.label16.Name = "label16";
		this.label16.Size = new System.Drawing.Size(68, 13);
		this.label16.TabIndex = 65;
		this.label16.Text = "Input Device";
		this.tabPage2.Controls.Add(this.comboBox2);
		this.tabPage2.Controls.Add(this.label17);
		this.tabPage2.Controls.Add(this.comboBox3);
		this.tabPage2.Controls.Add(this.label18);
		this.tabPage2.Controls.Add(this.label19);
		this.tabPage2.Controls.Add(this.comboBox4);
		this.tabPage2.Controls.Add(this.checkBox6);
		this.tabPage2.Controls.Add(this.checkBox7);
		this.tabPage2.Controls.Add(this.checkBox8);
		this.tabPage2.Controls.Add(this.checkBox9);
		this.tabPage2.Controls.Add(this.label20);
		this.tabPage2.Controls.Add(this.comboBox5);
		this.tabPage2.Controls.Add(this.checkBox10);
		this.tabPage2.Location = new System.Drawing.Point(4, 22);
		this.tabPage2.Name = "tabPage2";
		this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
		this.tabPage2.Size = new System.Drawing.Size(496, 275);
		this.tabPage2.TabIndex = 1;
		this.tabPage2.Text = "Customize";
		this.tabPage2.UseVisualStyleBackColor = true;
		this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox2.FormattingEnabled = true;
		this.comboBox2.Items.AddRange(new object[2] { "Stereo", "None" });
		this.comboBox2.Location = new System.Drawing.Point(370, 93);
		this.comboBox2.Name = "comboBox2";
		this.comboBox2.Size = new System.Drawing.Size(97, 21);
		this.comboBox2.TabIndex = 14;
		this.label17.Location = new System.Drawing.Point(228, 97);
		this.label17.Name = "label17";
		this.label17.Size = new System.Drawing.Size(136, 13);
		this.label17.TabIndex = 13;
		this.label17.Text = "Master Fader Section";
		this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox3.FormattingEnabled = true;
		this.comboBox3.Items.AddRange(new object[8] { "8", "16", "24", "32", "40", "48", "56", "64" });
		this.comboBox3.Location = new System.Drawing.Point(370, 39);
		this.comboBox3.Name = "comboBox3";
		this.comboBox3.Size = new System.Drawing.Size(56, 21);
		this.comboBox3.TabIndex = 10;
		this.label18.Location = new System.Drawing.Point(228, 43);
		this.label18.Name = "label18";
		this.label18.Size = new System.Drawing.Size(136, 13);
		this.label18.TabIndex = 9;
		this.label18.Text = "Maximum Channels";
		this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.label19.Location = new System.Drawing.Point(228, 16);
		this.label19.Name = "label19";
		this.label19.Size = new System.Drawing.Size(136, 13);
		this.label19.TabIndex = 8;
		this.label19.Text = "Number of Faders to Show";
		this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.comboBox4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox4.FormattingEnabled = true;
		this.comboBox4.Items.AddRange(new object[6] { "4", "8", "12", "16", "24", "32" });
		this.comboBox4.Location = new System.Drawing.Point(370, 12);
		this.comboBox4.Name = "comboBox4";
		this.comboBox4.Size = new System.Drawing.Size(56, 21);
		this.comboBox4.TabIndex = 7;
		this.checkBox6.AutoSize = true;
		this.checkBox6.Checked = true;
		this.checkBox6.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox6.Location = new System.Drawing.Point(10, 60);
		this.checkBox6.Name = "checkBox6";
		this.checkBox6.Size = new System.Drawing.Size(155, 17);
		this.checkBox6.TabIndex = 6;
		this.checkBox6.Text = "Show Master (Stereo) fader";
		this.checkBox6.UseVisualStyleBackColor = true;
		this.checkBox6.Visible = false;
		this.checkBox7.AutoSize = true;
		this.checkBox7.Checked = true;
		this.checkBox7.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox7.Location = new System.Drawing.Point(10, 83);
		this.checkBox7.Name = "checkBox7";
		this.checkBox7.Size = new System.Drawing.Size(144, 17);
		this.checkBox7.TabIndex = 5;
		this.checkBox7.Text = "Start in Full Screen mode";
		this.checkBox7.UseVisualStyleBackColor = true;
		this.checkBox7.Visible = false;
		this.checkBox8.AutoSize = true;
		this.checkBox8.Checked = true;
		this.checkBox8.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox8.Location = new System.Drawing.Point(10, 106);
		this.checkBox8.Name = "checkBox8";
		this.checkBox8.Size = new System.Drawing.Size(155, 17);
		this.checkBox8.TabIndex = 4;
		this.checkBox8.Text = "High-contrast color scheme";
		this.checkBox8.UseVisualStyleBackColor = true;
		this.checkBox8.Visible = false;
		this.checkBox9.AutoSize = true;
		this.checkBox9.Checked = true;
		this.checkBox9.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox9.Location = new System.Drawing.Point(10, 37);
		this.checkBox9.Name = "checkBox9";
		this.checkBox9.Size = new System.Drawing.Size(114, 17);
		this.checkBox9.TabIndex = 3;
		this.checkBox9.Text = "Show \"Fader tails\"";
		this.checkBox9.UseVisualStyleBackColor = true;
		this.checkBox9.Visible = false;
		this.label20.Location = new System.Drawing.Point(228, 70);
		this.label20.Name = "label20";
		this.label20.Size = new System.Drawing.Size(136, 13);
		this.label20.TabIndex = 2;
		this.label20.Text = "UDF Key for Intercom Flasher";
		this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.comboBox5.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox5.FormattingEnabled = true;
		this.comboBox5.Items.AddRange(new object[13]
		{
			"None", "1", "2", "3", "4", "5", "6", "7", "8", "9",
			"10", "11", "12"
		});
		this.comboBox5.Location = new System.Drawing.Point(370, 66);
		this.comboBox5.Name = "comboBox5";
		this.comboBox5.Size = new System.Drawing.Size(56, 21);
		this.comboBox5.TabIndex = 12;
		this.checkBox10.AutoSize = true;
		this.checkBox10.Checked = true;
		this.checkBox10.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBox10.Location = new System.Drawing.Point(10, 14);
		this.checkBox10.Name = "checkBox10";
		this.checkBox10.Size = new System.Drawing.Size(164, 17);
		this.checkBox10.TabIndex = 0;
		this.checkBox10.Text = "\"Click anywhere\" fader mode";
		this.checkBox10.UseVisualStyleBackColor = true;
		this.checkBox10.Visible = false;
		this.tabPage3.Controls.Add(this.label21);
		this.tabPage3.Controls.Add(this.label22);
		this.tabPage3.Controls.Add(this.button3);
		this.tabPage3.Controls.Add(this.button4);
		this.tabPage3.Controls.Add(this.button5);
		this.tabPage3.Controls.Add(this.button6);
		this.tabPage3.Controls.Add(this.listBox3);
		this.tabPage3.Controls.Add(this.listBox4);
		this.tabPage3.Location = new System.Drawing.Point(4, 22);
		this.tabPage3.Name = "tabPage3";
		this.tabPage3.Size = new System.Drawing.Size(496, 275);
		this.tabPage3.TabIndex = 3;
		this.tabPage3.Text = "Custom Layer";
		this.tabPage3.UseVisualStyleBackColor = true;
		this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label21.Location = new System.Drawing.Point(3, 10);
		this.label21.Name = "label21";
		this.label21.Size = new System.Drawing.Size(189, 23);
		this.label21.TabIndex = 7;
		this.label21.Text = "Available Controls";
		this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label22.Location = new System.Drawing.Point(304, 10);
		this.label22.Name = "label22";
		this.label22.Size = new System.Drawing.Size(189, 23);
		this.label22.TabIndex = 6;
		this.label22.Text = "Custom Fader Layer";
		this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.button3.Location = new System.Drawing.Point(210, 223);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(75, 38);
		this.button3.TabIndex = 5;
		this.button3.Text = "Down";
		this.button3.UseVisualStyleBackColor = true;
		this.button4.Location = new System.Drawing.Point(210, 36);
		this.button4.Name = "button4";
		this.button4.Size = new System.Drawing.Size(75, 38);
		this.button4.TabIndex = 4;
		this.button4.Text = "Up";
		this.button4.UseVisualStyleBackColor = true;
		this.button5.Location = new System.Drawing.Point(210, 150);
		this.button5.Name = "button5";
		this.button5.Size = new System.Drawing.Size(75, 38);
		this.button5.TabIndex = 3;
		this.button5.Text = "<- Remove";
		this.button5.UseVisualStyleBackColor = true;
		this.button6.Location = new System.Drawing.Point(210, 106);
		this.button6.Name = "button6";
		this.button6.Size = new System.Drawing.Size(75, 38);
		this.button6.TabIndex = 2;
		this.button6.Text = "Add ->";
		this.button6.UseVisualStyleBackColor = true;
		this.listBox3.FormattingEnabled = true;
		this.listBox3.Location = new System.Drawing.Point(304, 36);
		this.listBox3.Name = "listBox3";
		this.listBox3.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.listBox3.Size = new System.Drawing.Size(189, 225);
		this.listBox3.TabIndex = 1;
		this.listBox4.FormattingEnabled = true;
		this.listBox4.Location = new System.Drawing.Point(3, 36);
		this.listBox4.Name = "listBox4";
		this.listBox4.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
		this.listBox4.Size = new System.Drawing.Size(187, 225);
		this.listBox4.TabIndex = 0;
		this.tabPage4.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.tabPage4.Controls.Add(this.label23);
		this.tabPage4.Controls.Add(this.button7);
		this.tabPage4.Controls.Add(this.label24);
		this.tabPage4.Controls.Add(this.button8);
		this.tabPage4.Controls.Add(this.label25);
		this.tabPage4.Controls.Add(this.label26);
		this.tabPage4.Controls.Add(this.label27);
		this.tabPage4.Controls.Add(this.label28);
		this.tabPage4.Controls.Add(this.lblBrand2);
		this.tabPage4.Location = new System.Drawing.Point(4, 22);
		this.tabPage4.Name = "tabPage4";
		this.tabPage4.Size = new System.Drawing.Size(496, 275);
		this.tabPage4.TabIndex = 2;
		this.tabPage4.Text = "About";
		this.label23.ForeColor = System.Drawing.Color.WhiteSmoke;
		this.label23.Location = new System.Drawing.Point(5, 248);
		this.label23.Name = "label23";
		this.label23.Size = new System.Drawing.Size(487, 17);
		this.label23.TabIndex = 8;
		this.label23.Text = "Copyright © 2012, Molten Logic LLC -- Design/Programming: Adam Ellsworth";
		this.label23.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.button7.BackColor = System.Drawing.SystemColors.Control;
		this.button7.Location = new System.Drawing.Point(311, 121);
		this.button7.Name = "button7";
		this.button7.Size = new System.Drawing.Size(132, 30);
		this.button7.TabIndex = 21;
		this.button7.Text = "Check for Updates";
		this.button7.UseVisualStyleBackColor = false;
		this.label24.AutoSize = true;
		this.label24.ForeColor = System.Drawing.Color.White;
		this.label24.Location = new System.Drawing.Point(309, 63);
		this.label24.Name = "label24";
		this.label24.Size = new System.Drawing.Size(46, 13);
		this.label24.TabIndex = 6;
		this.label24.Text = "01vmix";
		this.button8.BackColor = System.Drawing.SystemColors.Control;
		this.button8.Location = new System.Drawing.Point(311, 85);
		this.button8.Name = "button8";
		this.button8.Size = new System.Drawing.Size(132, 30);
		this.button8.TabIndex = 20;
		this.button8.Text = "Registration";
		this.button8.UseVisualStyleBackColor = false;
		this.label25.AutoSize = true;
		this.label25.ForeColor = System.Drawing.Color.White;
		this.label25.Location = new System.Drawing.Point(309, 50);
		this.label25.Name = "label25";
		this.label25.Size = new System.Drawing.Size(46, 13);
		this.label25.TabIndex = 4;
		this.label25.Text = "01vmix";
		this.label26.ForeColor = System.Drawing.Color.White;
		this.label26.Location = new System.Drawing.Point(308, 149);
		this.label26.Name = "label26";
		this.label26.Size = new System.Drawing.Size(184, 88);
		this.label26.TabIndex = 3;
		this.label26.Text = resources.GetString("label26.Text");
		this.label26.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
		this.label27.AutoSize = true;
		this.label27.ForeColor = System.Drawing.Color.White;
		this.label27.Location = new System.Drawing.Point(309, 37);
		this.label27.Name = "label27";
		this.label27.Size = new System.Drawing.Size(46, 13);
		this.label27.TabIndex = 2;
		this.label27.Text = "01vmix";
		this.label28.AutoSize = true;
		this.label28.ForeColor = System.Drawing.Color.White;
		this.label28.Location = new System.Drawing.Point(309, 24);
		this.label28.Name = "label28";
		this.label28.Size = new System.Drawing.Size(46, 13);
		this.label28.TabIndex = 1;
		this.label28.Text = "01vmix";
		this.lblBrand2.AutoSize = false;
		this.lblBrand2.Font = new System.Drawing.Font("Arial", 28f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblBrand2.ForeColor = System.Drawing.Color.White;
		this.lblBrand2.Location = new System.Drawing.Point(5, 24);
		this.lblBrand2.Name = "lblBrand2";
		this.lblBrand2.Size = new System.Drawing.Size(300, 213);
		this.lblBrand2.TabIndex = 0;
		this.lblBrand2.Text = "01VMIX";
		this.lblBrand2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.colorPick.SolidColorOnly = true;
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		base.ClientSize = new System.Drawing.Size(607, 511);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnSave);
		base.Controls.Add(this.tabConfig);
		base.Controls.Add(this.chkGO);
		base.Controls.Add(this.label6);
		base.Controls.Add(this.btnSweep);
		base.Controls.Add(this.txtGain);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.txtMeter);
		base.Controls.Add(this.label4);
		base.Controls.Add(this.txtQ);
		base.Controls.Add(this.txtFreq);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		base.Name = "frmConfig";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Configuration";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(frmConfig_FormClosing);
		base.Load += new System.EventHandler(frmConfig_Load);
		this.tabConfig.ResumeLayout(false);
		this.tabDevices.ResumeLayout(false);
		this.tabDevices.PerformLayout();
		this.tabCustomize.ResumeLayout(false);
		this.tabCustomize.PerformLayout();
		this.tabCustomLayer.ResumeLayout(false);
		this.tabFaderStrip.ResumeLayout(false);
		this.tabServer.ResumeLayout(false);
		this.tabServer.PerformLayout();
		this.tabUsers.ResumeLayout(false);
		this.tabUsers.PerformLayout();
		this.tabAbout.ResumeLayout(false);
		this.tabAbout.PerformLayout();
		this.tabPage1.ResumeLayout(false);
		this.tabPage1.PerformLayout();
		this.tabPage2.ResumeLayout(false);
		this.tabPage2.PerformLayout();
		this.tabPage3.ResumeLayout(false);
		this.tabPage4.ResumeLayout(false);
		this.tabPage4.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
