using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;

namespace AirFader;

public class frmEFX : Form
{
	private delegate void EFXParamChangedCB(int objectIndex, string param, int paramIndex);

	private string[] mapGEQ = new string[31]
	{
		"20", "25", "31.5", "40", "50", "63", "80", "100", "125", "160",
		"200", "250", "315", "400", "500", "640", "800", "1k", "1.25k", "1.6k",
		"2k", "2.5k", "3.15k", "4k", "5k", "6.3k", "8k", "10k", "12.5k", "16k",
		"20k"
	};

	private WindowStatic thisWin = new WindowStatic();

	private AirKnob[] arrParam = new AirKnob[32];

	private Label[] arrParamLabel = new Label[32];

	private AirKnob activeKnob;

	private AirButton btnEFXType;

	private AirButton btnBypass;

	private AirButton btnFine;

	private AirButton btnClose;

	private Label lblTitle;

	private Label lblType;

	private int _activeEFX;

	private List<int> arrChangedKnobs = new List<int>();

	private IContainer components;

	private AirPanel panelParam0;

	private AirPanel panelParam1;

	private AirPanel panelParam2;

	private AirPanel panelParam3;

	private AirControls.AirFader paramFader;

	public int ActiveEFX
	{
		get
		{
			return _activeEFX;
		}
		set
		{
			_activeEFX = value;
			prepKnobs();
		}
	}

	public frmEFX()
	{
		InitializeComponent();
		thisWin.Parse(Settings.Default.windowEFX);
		thisWin.SetWindow(this);
	}

	private Font FontFit(string fontName, FontStyle fontStyle, string thisText, int thisWide, int thisHigh, float maxSize)
	{
		Graphics graphics = CreateGraphics();
		while (maxSize > 4f)
		{
			Font font = new Font(fontName, maxSize, fontStyle);
			SizeF sizeF = graphics.MeasureString(thisText, font);
			if (sizeF.Width <= (float)thisWide && sizeF.Height <= (float)thisHigh)
			{
				return font;
			}
			maxSize -= 0.5f;
		}
		return new Font(fontName, 4f, fontStyle);
	}

	private void frmEFX_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		btnEFXType = new AirButton();
		btnEFXType.Text = "Effect\nType";
		btnEFXType.BackColor = Settings.Default.c_button_inactive;
		btnEFXType.ParentColor = BackColor;
		btnEFXType.Click += btnEFXType_Click;
		base.Controls.Add(btnEFXType);
		btnBypass = new AirButton();
		btnBypass.Text = "Bypass";
		btnBypass.BackColor = Settings.Default.c_button_inactive;
		btnBypass.ParentColor = BackColor;
		btnBypass.Click += btnBypass_Click;
		base.Controls.Add(btnBypass);
		btnFine = new AirButton();
		btnFine.Text = "Fine\nMode";
		btnFine.BackColor = Settings.Default.c_button_inactive;
		btnFine.ParentColor = BackColor;
		btnFine.Click += btnFine_Click;
		base.Controls.Add(btnFine);
		btnClose = new AirButton();
		btnClose.Text = "Close";
		btnClose.BackColor = Settings.Default.c_button_inactive;
		btnClose.ParentColor = BackColor;
		btnClose.Click += btnClose_Click;
		base.Controls.Add(btnClose);
		lblTitle = new Label();
		lblTitle.Text = "";
		lblTitle.TextAlign = ContentAlignment.MiddleCenter;
		lblTitle.BackColor = BackColor;
		lblTitle.ForeColor = Color.WhiteSmoke;
		base.Controls.Add(lblTitle);
		lblType = new Label();
		lblType.Text = "";
		lblType.TextAlign = ContentAlignment.MiddleCenter;
		lblType.BackColor = BackColor;
		lblType.ForeColor = Color.Lime;
		base.Controls.Add(lblType);
		for (int i = 0; i < 32; i++)
		{
			arrParam[i] = new AirKnob();
			arrParam[i].KnobStyle = 0;
			arrParam[i].Channel = i;
			arrParam[i].Click += knobParam_Click;
			arrParam[i].MouseDown += knobParam_MouseDown;
			arrParam[i].KnobChanged += frmEFX_KnobChanged;
			arrParam[i].BackColor = panelParam0.BackColor;
			if (i < 8)
			{
				panelParam0.Controls.Add(arrParam[i]);
			}
			else if (i < 16)
			{
				panelParam1.Controls.Add(arrParam[i]);
			}
			else if (i < 24)
			{
				panelParam2.Controls.Add(arrParam[i]);
			}
			else if (i < 32)
			{
				panelParam3.Controls.Add(arrParam[i]);
			}
			arrParamLabel[i] = new Label();
			arrParamLabel[i].Text = (i + 1).ToString();
			arrParamLabel[i].BackColor = panelParam0.BackColor;
			arrParamLabel[i].ForeColor = Color.GhostWhite;
			arrParamLabel[i].Font = new Font("Arial", 8f, FontStyle.Bold);
			arrParamLabel[i].TextAlign = ContentAlignment.MiddleCenter;
			if (i < 8)
			{
				panelParam0.Controls.Add(arrParamLabel[i]);
			}
			else if (i < 16)
			{
				panelParam1.Controls.Add(arrParamLabel[i]);
			}
			else if (i < 24)
			{
				panelParam2.Controls.Add(arrParamLabel[i]);
			}
			else if (i < 32)
			{
				panelParam3.Controls.Add(arrParamLabel[i]);
			}
		}
		prepKnobs();
		doLayout();
		MixEngine.EFXParamChanged += MixEngine_EFXParamChanged;
	}

	private void prepKnobs()
	{
		for (int i = 0; i < 32; i++)
		{
			if (i == 31)
			{
				i = 31;
			}
			if (MixEngine.EFXData[_activeEFX].pType[i] == Values.knobTypes.None)
			{
				arrParam[i].Hide();
				arrParamLabel[i].Hide();
			}
			else
			{
				arrParam[i].Show();
				arrParamLabel[i].Show();
			}
			arrParam[i].KnobType = MixEngine.EFXData[_activeEFX].pType[i];
			arrParam[i].Minimum = MixEngine.EFXData[_activeEFX].pMin[i];
			arrParam[i].Maximum = MixEngine.EFXData[_activeEFX].pMax[i];
			arrParam[i].Value = MixEngine.EFXData[_activeEFX].pValue[i];
			arrParamLabel[i].Text = MixEngine.EFXData[_activeEFX].pLabel[i];
			arrParamLabel[i].ForeColor = MixEngine.EFXData[_activeEFX].pColor[i];
			lblType.Text = MixEngine.EFXData[_activeEFX].EFXName;
			lblTitle.Text = "Rack " + (_activeEFX + 5);
			Text = lblTitle.Text;
			knobParam_Click(arrParam[0], EventArgs.Empty);
		}
		arrParam[30].Value = MixEngine.EFXData[_activeEFX]._mix;
		arrParam[31].Value = MixEngine.EFXData[_activeEFX].bpm;
		if (MixEngine.EFXData[_activeEFX]._bypass)
		{
			btnBypass.BackColor = Settings.Default.c_button_active;
		}
		else
		{
			btnBypass.BackColor = Settings.Default.c_button_inactive;
		}
	}

	private void doLayout()
	{
		if (thisWin.Ready && btnEFXType != null)
		{
			panelParam0.Width = base.ClientRectangle.Width - 10;
			panelParam0.Height = (int)Math.Round((double)base.ClientRectangle.Height * 0.75 / 4.0) - 5;
			panelParam0.Location = new Point(5, (int)Math.Round((double)base.ClientRectangle.Height * 0.14));
			panelParam1.Size = panelParam0.Size;
			panelParam1.Location = new Point(5, panelParam0.Bottom + 5);
			panelParam2.Size = panelParam0.Size;
			panelParam2.Location = new Point(5, panelParam1.Bottom + 5);
			panelParam3.Size = panelParam0.Size;
			panelParam3.Location = new Point(5, panelParam2.Bottom + 5);
			paramFader.Height = base.ClientRectangle.Height - panelParam3.Bottom - 10;
			paramFader.Width = panelParam3.Width;
			paramFader.Left = 5;
			paramFader.Top = panelParam3.Bottom + 5;
			btnEFXType.Top = 10;
			btnEFXType.Left = 10;
			btnEFXType.Height = panelParam0.Top - 20;
			btnEFXType.Width = (int)Math.Round((double)btnEFXType.Height * 1.0);
			btnBypass.Top = btnEFXType.Top;
			btnBypass.Left = btnEFXType.Right + 10;
			btnBypass.Size = btnEFXType.Size;
			btnClose.Size = btnEFXType.Size;
			btnClose.Location = new Point(base.ClientRectangle.Width - btnEFXType.Size.Width - 10, btnEFXType.Top);
			btnFine.Size = btnEFXType.Size;
			btnFine.Location = new Point(btnClose.Left - btnEFXType.Size.Width - 10, btnEFXType.Top);
			lblTitle.Size = new Size(btnFine.Left - btnBypass.Right - 20, (int)Math.Round((float)btnEFXType.Height * 0.4f));
			lblTitle.Location = new Point(btnBypass.Right + 10, btnEFXType.Top);
			lblTitle.Font = FontFit("Arial", FontStyle.Bold, "Rack 8", lblTitle.Width, lblTitle.Height, 14f);
			lblType.Size = new Size(btnFine.Left - btnBypass.Right - 20, (int)Math.Round((float)btnEFXType.Height * 0.6f));
			lblType.Location = new Point(btnBypass.Right + 10, lblTitle.Bottom + 1);
			lblType.Font = FontFit("Arial", FontStyle.Bold, "Stereo Reverb", lblType.Width, lblType.Height, 18f);
			int num = panelParam0.Height - 15;
			Size size = new Size((int)Math.Round((double)panelParam0.Width / 9.0), (int)Math.Round((double)num * 0.8));
			Size size2 = new Size(size.Width, (int)Math.Round((double)num * 0.2));
			Font font = FontFit("Arial", FontStyle.Bold, "ABCDwxyz", size2.Width, size2.Height, 16f);
			for (int i = 0; i < 32; i++)
			{
				arrParamLabel[i].Size = size2;
				arrParamLabel[i].Font = font;
				arrParamLabel[i].Location = new Point(i % 8 * size.Width + size.Width / 2, 7);
				arrParam[i].Size = size;
				arrParam[i].Location = new Point(arrParamLabel[i].Left, arrParamLabel[i].Bottom);
			}
		}
	}

	private void frmEFX_KnobChanged(AirKnob sender, int channel, int value, string flag)
	{
		paramFader.Value = value;
		if (channel >= 0 && channel < 30)
		{
			MixEngine.EFXData[_activeEFX].setParamByKnob("UI", channel, value);
			return;
		}
		switch (channel)
		{
		case 30:
			MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.Mix, value);
			break;
		case 31:
			MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.BPM, value);
			break;
		}
	}

	private void paramFader_FaderChanged(int channel, int value, string flag)
	{
		if (activeKnob == null)
		{
			knobParam_Click(arrParam[0], EventArgs.Empty);
		}
		activeKnob.Value = value;
		if (channel >= 0 && channel < 30)
		{
			MixEngine.EFXData[_activeEFX].setParamByKnob("UI", channel, value);
		}
		else
		{
			switch (channel)
			{
			case 30:
				MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.Mix, value);
				break;
			case 31:
				MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.BPM, value);
				break;
			}
		}
		activeKnob.Invalidate();
	}

	private void btnEFXType_Click(object sender, EventArgs e)
	{
		Form form = new Form();
		form.Width = base.ClientRectangle.Width - 20;
		form.Height = base.ClientRectangle.Height - 20;
		form.BackColor = Color.FromArgb(150, 50, 50);
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		form.TopMost = true;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = PointToScreen(new Point(10, 10));
		Label label = new Label();
		label.ForeColor = Color.White;
		label.Font = new Font("Arial", 12f, FontStyle.Bold);
		label.Location = new Point(20, 9);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 40, 24);
		label.TabIndex = 0;
		label.Text = "Choose Effects Type";
		label.TextAlign = ContentAlignment.TopCenter;
		form.Controls.Add(label);
		string[] array = new string[48]
		{
			"Reverb Hall", "Reverb Room", "Reverb Stage", "Reverb Plate", "Early Ref.", "Gate Reverb", "Reverse Gate", "Mono Delay", "Stereo Delay", "Mod. Delay",
			"Delay LCR", "Echo", "Chorus", "Flange", "Symphonic", "Phaser", "Auto Pan", "Tremolo", "HQ. Pitch", "Dual Pitch",
			"Rotary", "Ring Mod.", "Mod. Filter", "Distortion", "Amp Simulate", "Dyna. Filter", "Dyna. Flange", "Dyna. Phaser", "Rev+ Chorus", "Rev-> Chorus",
			"Rev+ Flange", "Rev-> Flange", "Rev+ Sympho.", "Rev-> Sympho.", "Rev-> Pan", "Delay+ ER.", "Delay-> ER.", "Delay+ Rev", "Delay-> Rev", "Dist-> Delay",
			"Multi Filter", "Freeze", "Stereo Reverb", "M.Band Dyna.", "M.Band Comp", "REV-X Hall", "REV-X Room", "REV-X Plate"
		};
		Size size = new Size((int)Math.Round((double)(form.Width - 20) / 8.0) - 10, (int)Math.Round((double)(form.Height - 35) / 7.0) - 10);
		for (int i = 0; i < array.Length; i++)
		{
			AirButton airButton = new AirButton();
			airButton.BackColor = ((i == MixEngine.EFXData[_activeEFX]._type) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			airButton.ParentColor = form.BackColor;
			airButton.Size = size;
			airButton.Location = new Point(i % 8 * (size.Width + 10) + 10, i / 8 * (size.Height + 10) + 35);
			airButton.Name = "btnPicker" + i;
			airButton.TabIndex = i + 100;
			airButton.Tag = i;
			airButton.Text = array[i].Replace(" ", "\n");
			airButton.Click += btnPicker_Click;
			form.Controls.Add(airButton);
		}
		AirButton airButton2 = new AirButton();
		airButton2.BackColor = Settings.Default.c_button_inactive;
		airButton2.ParentColor = form.BackColor;
		airButton2.Size = size;
		airButton2.Location = new Point(form.Width - size.Width - 10, form.Height - size.Height - 10);
		airButton2.Name = "Cancel";
		airButton2.TabIndex = 200;
		airButton2.Tag = -1;
		airButton2.Text = "Cancel";
		airButton2.Click += btnPicker_Click;
		form.Controls.Add(airButton2);
		form.ShowDialog();
	}

	private void btnBypass_Click(object sender, EventArgs e)
	{
		MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.Bypass, (!MixEngine.EFXData[_activeEFX]._bypass) ? 1 : 0);
	}

	private void btnPicker_Click(object sender, EventArgs e)
	{
		int result = 0;
		if (int.TryParse(((AirButton)sender).Tag.ToString(), out result) && result >= 0 && result < 48)
		{
			MixEngine.EFXData[_activeEFX].setParam("UI", EFX.Pr.Type, result);
			prepKnobs();
		}
		((Form)((AirButton)sender).Parent).Close();
	}

	private void knobParam_Click(object sender, EventArgs e)
	{
		if (activeKnob != null)
		{
			activeKnob.Active = false;
		}
		activeKnob = (AirKnob)sender;
		activeKnob.Active = true;
		paramFader.Channel = activeKnob.Channel;
		paramFader.Minimum = activeKnob.Minimum;
		paramFader.Maximum = activeKnob.Maximum;
		paramFader.Value = activeKnob.Value;
		paramFader.Invalidate();
	}

	private void knobParam_MouseDown(object sender, MouseEventArgs e)
	{
		knobParam_Click(sender, EventArgs.Empty);
	}

	private void btnClose_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void btnFine_Click(object sender, EventArgs e)
	{
	}

	private void frmEFX_Resize(object sender, EventArgs e)
	{
		if (thisWin.Ready)
		{
			doLayout();
			if (base.WindowState == FormWindowState.Normal && base.FormBorderStyle == FormBorderStyle.Sizable)
			{
				thisWin.WindowSize = base.Size;
			}
		}
	}

	private void frmEFX_FormClosed(object sender, FormClosedEventArgs e)
	{
		MixEngine.EFXParamChanged -= MixEngine_EFXParamChanged;
	}

	private void MixEngine_EFXParamChanged(int objectIndex, string param, int paramIndex)
	{
		if (base.InvokeRequired)
		{
			EFXParamChangedCB method = MixEngine_EFXParamChanged;
			BeginInvoke(method, objectIndex, param, paramIndex);
		}
		else
		{
			if (_activeEFX != objectIndex)
			{
				return;
			}
			switch (param)
			{
			case "Param":
				if (paramIndex >= 0 && paramIndex < 30)
				{
					arrParam[paramIndex].Value = MixEngine.EFXData[_activeEFX].pValue[paramIndex];
				}
				break;
			case "MIX":
				arrParam[30].Value = MixEngine.EFXData[_activeEFX]._mix;
				if (activeKnob == arrParam[30])
				{
					paramFader.Value = arrParam[30].Value;
				}
				break;
			case "BPM":
				arrParam[31].Value = MixEngine.EFXData[_activeEFX].bpm;
				if (activeKnob == arrParam[31])
				{
					paramFader.Value = arrParam[31].Value;
				}
				break;
			case "BYPASS":
				if (MixEngine.EFXData[_activeEFX]._bypass)
				{
					btnBypass.BackColor = Settings.Default.c_button_active;
				}
				else
				{
					btnBypass.BackColor = Settings.Default.c_button_inactive;
				}
				break;
			case "TYPE":
				prepKnobs();
				break;
			}
		}
	}

	private void frmEFX_Deactivate(object sender, EventArgs e)
	{
		thisWin.WindowState = ((base.FormBorderStyle == FormBorderStyle.None) ? "Fullscreen" : base.WindowState.ToString());
		thisWin.Page = 0;
		Settings.Default.windowEFX = WindowStatic.Store(thisWin);
		Settings.Default.Save();
	}

	private void frmEFX_LocationChanged(object sender, EventArgs e)
	{
		if (thisWin.Ready && base.WindowState == FormWindowState.Normal && base.FormBorderStyle == FormBorderStyle.Sizable)
		{
			thisWin.WindowPos = base.Location;
		}
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmEFX));
		this.paramFader = new AirControls.AirFader();
		this.panelParam3 = new AirControls.AirPanel();
		this.panelParam2 = new AirControls.AirPanel();
		this.panelParam1 = new AirControls.AirPanel();
		this.panelParam0 = new AirControls.AirPanel();
		base.SuspendLayout();
		this.paramFader.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.paramFader.Channel = 0;
		this.paramFader.FaderColor = "Blue";
		this.paramFader.FaderStyle = 23;
		this.paramFader.FaderTails = false;
		this.paramFader.FineMode = false;
		this.paramFader.Horizontal = true;
		this.paramFader.IsActive = false;
		this.paramFader.KnobColor = "White";
		this.paramFader.LabelType = "none";
		this.paramFader.Location = new System.Drawing.Point(28, 232);
		this.paramFader.Maximum = 0;
		this.paramFader.MeterLevel = 0;
		this.paramFader.MeterType = "side";
		this.paramFader.Minimum = -540;
		this.paramFader.Name = "paramFader";
		this.paramFader.ShowKnob = false;
		this.paramFader.ShowMeter = false;
		this.paramFader.Size = new System.Drawing.Size(237, 31);
		this.paramFader.Steps = 1;
		this.paramFader.TabIndex = 203;
		this.paramFader.TickType = "fader";
		this.paramFader.Value = 0;
		this.paramFader.ValueDB = 0m;
		this.paramFader.ValueType = "none";
		this.paramFader.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(paramFader_FaderChanged);
		this.panelParam3.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.panelParam3.BorderColor = System.Drawing.Color.Black;
		this.panelParam3.BorderSize = 1f;
		this.panelParam3.EQColor = System.Drawing.Color.Lime;
		this.panelParam3.Location = new System.Drawing.Point(28, 183);
		this.panelParam3.Name = "panelParam3";
		this.panelParam3.Padding = new System.Windows.Forms.Padding(4);
		this.panelParam3.PanelType = 0;
		this.panelParam3.Size = new System.Drawing.Size(237, 43);
		this.panelParam3.TabIndex = 202;
		this.panelParam2.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.panelParam2.BorderColor = System.Drawing.Color.Black;
		this.panelParam2.BorderSize = 1f;
		this.panelParam2.EQColor = System.Drawing.Color.Lime;
		this.panelParam2.Location = new System.Drawing.Point(28, 134);
		this.panelParam2.Name = "panelParam2";
		this.panelParam2.Padding = new System.Windows.Forms.Padding(4);
		this.panelParam2.PanelType = 0;
		this.panelParam2.Size = new System.Drawing.Size(237, 43);
		this.panelParam2.TabIndex = 202;
		this.panelParam1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.panelParam1.BorderColor = System.Drawing.Color.Black;
		this.panelParam1.BorderSize = 1f;
		this.panelParam1.EQColor = System.Drawing.Color.Lime;
		this.panelParam1.Location = new System.Drawing.Point(28, 85);
		this.panelParam1.Name = "panelParam1";
		this.panelParam1.Padding = new System.Windows.Forms.Padding(4);
		this.panelParam1.PanelType = 0;
		this.panelParam1.Size = new System.Drawing.Size(237, 43);
		this.panelParam1.TabIndex = 202;
		this.panelParam0.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.panelParam0.BorderColor = System.Drawing.Color.Black;
		this.panelParam0.BorderSize = 1f;
		this.panelParam0.EQColor = System.Drawing.Color.Lime;
		this.panelParam0.Location = new System.Drawing.Point(28, 36);
		this.panelParam0.Name = "panelParam0";
		this.panelParam0.Padding = new System.Windows.Forms.Padding(4);
		this.panelParam0.PanelType = 0;
		this.panelParam0.Size = new System.Drawing.Size(237, 43);
		this.panelParam0.TabIndex = 201;
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		base.ClientSize = new System.Drawing.Size(292, 266);
		base.Controls.Add(this.paramFader);
		base.Controls.Add(this.panelParam3);
		base.Controls.Add(this.panelParam2);
		base.Controls.Add(this.panelParam1);
		base.Controls.Add(this.panelParam0);
		base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		base.Name = "frmEFX";
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "Effects";
		base.Deactivate += new System.EventHandler(frmEFX_Deactivate);
		base.Load += new System.EventHandler(frmEFX_Load);
		base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(frmEFX_FormClosed);
		base.Resize += new System.EventHandler(frmEFX_Resize);
		base.LocationChanged += new System.EventHandler(frmEFX_LocationChanged);
		base.ResumeLayout(false);
	}
}
