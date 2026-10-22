using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;

namespace AirFader;

public class frmGraphicEQ : Form
{
	private delegate void countBandsCallback();

	private delegate void EngineChangedCB(MixEngine.Pr param, int rack, int paramIndex, int value);

	private string[] mapGEQ = new string[31]
	{
		"20", "25", "31.5", "40", "50", "63", "80", "100", "125", "160",
		"200", "250", "315", "400", "500", "640", "800", "1k", "1.25k", "1.6k",
		"2k", "2.5k", "3.15k", "4k", "5k", "6.3k", "8k", "10k", "12.5k", "16k",
		"20k"
	};

	private AirPanel panelLevels = new AirPanel();

	private List<AirControls.AirFader> arrLevels = new List<AirControls.AirFader>();

	private List<Label> arrLabels = new List<Label>();

	private List<Label> arrGainLabelsL = new List<Label>();

	private List<Label> arrGainLabelsR = new List<Label>();

	private List<int> arrChangedBands = new List<int>();

	private WindowStatic thisWin = new WindowStatic();

	private Font fontLabel = new Font("Arial", 12f);

	private AirButton btnOn = new AirButton();

	private AirButton btnFlatAll = new AirButton();

	private AirButton btnFlatSel = new AirButton();

	private AirButton btnLink = new AirButton();

	private AirButton btnMax = new AirButton();

	private AirButton btnClose = new AirButton();

	private AirButton btnPrev = new AirButton();

	private AirButton btnNext = new AirButton();

	private Label lblHeader = new Label();

	private Label lblBands = new Label();

	private int activeRack = -1;

	private int selectedBand;

	private System.Threading.Timer heartBeat;

	private IContainer components;

	private AirControls.AirFader faderGEQGain;

	private AirControls.AirFader faderGEQBand;

	public int ActiveRack
	{
		get
		{
			return activeRack;
		}
		set
		{
			activeRack = value;
			syncBands();
			if (MixEngine.GEQData[activeRack].Type != 0)
			{
				countBands();
				lblBands.Show();
			}
			else
			{
				lblBands.Text = "";
			}
		}
	}

	private Font FontFit(string fontName, FontStyle fontStyle, string thisText, int thisWide, int thisHigh, float maxSize)
	{
		try
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
		catch (Exception)
		{
			return new Font("Arial", 12f);
		}
	}

	private void countBands()
	{
		if (MixEngine.GEQData[activeRack].Type == 0)
		{
			return;
		}
		if (lblBands.InvokeRequired)
		{
			countBandsCallback method = countBands;
			BeginInvoke(method, new object[0]);
			return;
		}
		int num = 0;
		for (int i = 0; i < 31; i++)
		{
			if (MixEngine.GEQData[activeRack]._Band[i] != 0)
			{
				num++;
			}
		}
		MixEngine.GEQData[activeRack].BandsAvail = 15 - num;
		if (MixEngine.GEQData[activeRack].BandsAvail > 3)
		{
			lblBands.ForeColor = Color.Lime;
			lblBands.Text = MixEngine.GEQData[activeRack].BandsAvail + " Bands Avail";
			for (int j = 0; j < 31; j++)
			{
				if (arrLevels[j].Value == 0 && arrLevels[j].FaderColor != "Blue")
				{
					arrLevels[j].FaderColor = "Blue";
				}
			}
		}
		else if (MixEngine.GEQData[activeRack].BandsAvail > 1)
		{
			lblBands.ForeColor = Color.Yellow;
			lblBands.Text = MixEngine.GEQData[activeRack].BandsAvail + " Bands Avail";
			for (int k = 0; k < 31; k++)
			{
				if (arrLevels[k].Value == 0 && arrLevels[k].FaderColor != "Blue")
				{
					arrLevels[k].FaderColor = "Blue";
				}
			}
		}
		else if (MixEngine.GEQData[activeRack].BandsAvail == 1)
		{
			lblBands.ForeColor = Color.Yellow;
			lblBands.Text = MixEngine.GEQData[activeRack].BandsAvail + " Band Avail";
			for (int l = 0; l < 31; l++)
			{
				if (arrLevels[l].Value == 0 && arrLevels[l].FaderColor != "Blue")
				{
					arrLevels[l].FaderColor = "Blue";
				}
			}
		}
		else
		{
			lblBands.ForeColor = Color.Red;
			lblBands.Text = "No Bands Avail";
			for (int m = 0; m < 31; m++)
			{
				if (arrLevels[m].Value == 0 && arrLevels[m].FaderColor != "Red")
				{
					arrLevels[m].FaderColor = "Red";
				}
			}
		}
		if (faderGEQGain.FaderColor != arrLevels[selectedBand].FaderColor)
		{
			faderGEQGain.FaderColor = arrLevels[selectedBand].FaderColor;
		}
	}

	public frmGraphicEQ()
	{
		InitializeComponent();
		Loc.Apply(this);
		thisWin.Parse(Settings.Default.windowGEQ);
		thisWin.SetWindow(this);
		faderGEQBand.ValueType = "geq";
		faderGEQBand.LabelType = "geq";
		faderGEQBand.Minimum = 0;
		faderGEQBand.Maximum = mapGEQ.Length - 1;
		faderGEQBand.BackColor = Settings.Default.c_fader_back;
		faderGEQBand.ForeColor = Settings.Default.c_fader_fore;
		faderGEQGain.BackColor = Settings.Default.c_fader_back;
		faderGEQGain.ForeColor = Settings.Default.c_fader_fore;
		makeLevels();
		doLayout();
		setActiveBand(thisWin.Page);
		MixEngine.EngineChanged += MixEngine_EngineChanged;
		heartBeat = new System.Threading.Timer(tickUI, "tick", 50, -1);
	}

	private void MixEngine_EngineChanged(MixEngine.Pr param, int rack, int paramIndex, int value)
	{
		if (lblHeader.InvokeRequired)
		{
			EngineChangedCB method = MixEngine_EngineChanged;
			BeginInvoke(method, param, rack, paramIndex, value);
		}
		else if (param == MixEngine.Pr.None)
		{
			ActiveRack = activeRack;
		}
		else
		{
			if (rack != activeRack)
			{
				return;
			}
			switch (param)
			{
			case MixEngine.Pr.GEQBand:
				arrLevels[paramIndex].Value = value;
				if (arrLevels[paramIndex].Value == 0)
				{
					if (MixEngine.GEQData[activeRack].BandsAvail == 0 && MixEngine.GEQData[activeRack]._type == 1)
					{
						arrLevels[paramIndex].FaderColor = "Red";
					}
					else
					{
						arrLevels[paramIndex].FaderColor = "Blue";
					}
				}
				else
				{
					arrLevels[paramIndex].FaderColor = "Yellow";
				}
				if (paramIndex == selectedBand)
				{
					faderGEQGain.Value = arrLevels[selectedBand].Value;
					faderGEQGain.FaderColor = arrLevels[selectedBand].FaderColor;
				}
				if (MixEngine.GEQData[activeRack].Type != 0)
				{
					countBands();
				}
				break;
			case MixEngine.Pr.GEQFlat:
				syncBands();
				break;
			case MixEngine.Pr.GEQOn:
				btnOn.BackColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQOn, 0) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
				btnOn.ForeColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQOn, 0) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
				break;
			case MixEngine.Pr.GEQLink:
			case MixEngine.Pr.GEQLinkLR:
				btnLink.BackColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) > 0 || MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) > 0) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
				btnLink.ForeColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) > 0 || MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) > 0) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
				break;
			case MixEngine.Pr.GEQType:
			case MixEngine.Pr.RackType:
				if (askQuestion("Rack config changed, close window?") == DialogResult.Yes)
				{
					Close();
				}
				break;
			case MixEngine.Pr.RackSource:
				lblHeader.Text = "GEQ " + (activeRack + 1) + ": " + MixEngine.getParamStr(activeRack, MixEngine.Pr.RackSource, 0);
				fontLabel = FontFit("Arial", FontStyle.Bold, lblHeader.Text, lblHeader.Width - 6, lblHeader.Height - 4, 16f);
				lblHeader.Font = fontLabel;
				break;
			}
		}
	}

	private void syncBands()
	{
		for (int i = 0; i < 31; i++)
		{
			arrLevels[i].Value = MixEngine.getParam(activeRack, MixEngine.Pr.GEQBand, i);
			if (arrLevels[i].Value == 0)
			{
				arrLevels[i].FaderColor = "Blue";
			}
			else
			{
				arrLevels[i].FaderColor = "Yellow";
			}
		}
		faderGEQGain.Value = arrLevels[selectedBand].Value;
		faderGEQGain.FaderColor = arrLevels[selectedBand].FaderColor;
		btnOn.BackColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQOn, 0) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
		btnOn.ForeColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQOn, 0) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
		btnLink.BackColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) > 0 || MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) > 0) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
		btnLink.ForeColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) > 0 || MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) > 0) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
		fontLabel = FontFit("Arial", FontStyle.Bold, "GEQ 4B: OOOOOOO", lblHeader.Width - 6, lblHeader.Height - 4, 16f);
		lblHeader.Font = fontLabel;
		if (MixEngine.GEQData[activeRack].Type == 0)
		{
			lblHeader.Text = "GEQ " + (activeRack + 1) + ": " + MixEngine.getParamStr(activeRack, MixEngine.Pr.RackSource, 0);
			if (activeRack % 2 == 0)
			{
				btnLink.Text = "Link\n" + (activeRack + 1) + " > " + (activeRack + 2);
			}
			else
			{
				btnLink.Text = "Link\n" + (activeRack + 1) + " > " + activeRack;
			}
		}
		else if (activeRack < 8)
		{
			lblHeader.Text = "GEQ " + (activeRack + 1) + "A: " + MixEngine.getParamStr(activeRack, MixEngine.Pr.RackSource, 0);
			btnLink.Text = "Link\n" + (activeRack + 1) + "A > " + (activeRack + 1) + "B";
		}
		else
		{
			lblHeader.Text = "GEQ " + (activeRack + 1 - 8) + "B: " + MixEngine.getParamStr(activeRack, MixEngine.Pr.RackSource, 0);
			btnLink.Text = "Link\n" + (activeRack + 1 - 8) + "B > " + (activeRack + 1 - 8) + "A";
		}
		Text = lblHeader.Text;
	}

	private void MixEngine_Action(string property, int index)
	{
		switch (property)
		{
		case "RackType":
			if (index == activeRack && askQuestion("Rack config changed, close window?") == DialogResult.Yes)
			{
				Close();
			}
			break;
		case "RackSource":
			if (index == activeRack)
			{
				lblHeader.Text = "GEQ " + (activeRack + 1) + ": " + MixEngine.getParamStr(activeRack, MixEngine.Pr.RackSource, 0);
				fontLabel = FontFit("Arial", FontStyle.Bold, lblHeader.Text, lblHeader.Width - 6, lblHeader.Height - 4, 16f);
				lblHeader.Font = fontLabel;
			}
			break;
		case "GEQLink":
		case "GEQLinkLR":
			btnLink.BackColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) > 0 || MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) > 0) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnLink.ForeColor = ((MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) > 0 || MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) > 0) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		}
	}

	private void tickUI(object data)
	{
		try
		{
			heartBeat.Change(-1, -1);
		}
		catch (Exception)
		{
			return;
		}
		if (arrChangedBands.Count > 0)
		{
			arrChangedBands.ToArray();
			arrChangedBands.Clear();
		}
		try
		{
			heartBeat.Change(50, -1);
		}
		catch (Exception)
		{
		}
	}

	private void makeLevels()
	{
		panelLevels.Controls.Clear();
		arrLevels.Clear();
		Color color = ((!((double)Settings.Default.c_fader_back.GetBrightness() > 0.5)) ? RGBHSL.ModifyBrightness(Settings.Default.c_fader_back, 2.0) : RGBHSL.ModifyBrightness(Settings.Default.c_fader_back, 0.6));
		int num = 0;
		string[] array = mapGEQ;
		foreach (string text in array)
		{
			AirControls.AirFader airFader = new AirControls.AirFader();
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
			airFader.ForeColor = Settings.Default.c_fader_fore;
			airFader.FaderChanged += meterGEQBand_FaderChanged;
			airFader.GotFocus += meterGEQBand_GotFocus;
			panelLevels.Controls.Add(airFader);
			arrLevels.Add(airFader);
			Label label = new Label();
			label.Text = text;
			label.TextAlign = ContentAlignment.MiddleCenter;
			label.ForeColor = Settings.Default.c_fader_fore;
			label.BackColor = airFader.BackColor;
			label.FlatStyle = FlatStyle.System;
			label.Tag = num;
			label.Click += meterGEQLabel_Click;
			panelLevels.Controls.Add(label);
			arrLabels.Add(label);
			num++;
		}
		base.Controls.Add(panelLevels);
		btnOn.Text = "On";
		btnOn.BackColor = Settings.Default.c_button_inactive;
		btnOn.ForeColor = Settings.Default.c_button_inactive_fore;
		btnOn.ParentColor = BackColor;
		btnOn.Click += btnOn_Click;
		base.Controls.Add(btnOn);
		btnFlatAll.Text = "Flat\nAll";
		btnFlatAll.BackColor = Settings.Default.c_button_inactive;
		btnFlatAll.ForeColor = Settings.Default.c_button_inactive_fore;
		btnFlatAll.ParentColor = BackColor;
		btnFlatAll.Click += btnFlatAll_Click;
		base.Controls.Add(btnFlatSel);
		btnFlatSel.Text = "Flat\nSel";
		btnFlatSel.BackColor = Settings.Default.c_button_inactive;
		btnFlatSel.ForeColor = Settings.Default.c_button_inactive_fore;
		btnFlatSel.ParentColor = BackColor;
		btnFlatSel.Click += btnFlatSel_Click;
		base.Controls.Add(btnFlatAll);
		btnLink.Text = "Link";
		btnLink.BackColor = Settings.Default.c_button_inactive;
		btnLink.ForeColor = Settings.Default.c_button_inactive_fore;
		btnLink.ParentColor = BackColor;
		btnLink.Click += btnLink_Click;
		base.Controls.Add(btnLink);
		btnMax.Text = "Full\nScreen";
		btnMax.BackColor = ((base.WindowState == FormWindowState.Maximized) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
		btnMax.ForeColor = ((base.WindowState == FormWindowState.Maximized) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
		btnMax.ParentColor = BackColor;
		btnMax.Click += btnMax_Click;
		base.Controls.Add(btnMax);
		btnClose.Text = "Close";
		btnClose.BackColor = Settings.Default.c_button_inactive;
		btnClose.ForeColor = Settings.Default.c_button_inactive_fore;
		btnClose.ParentColor = BackColor;
		btnClose.Click += btnClose_Click;
		base.Controls.Add(btnClose);
		btnPrev.Text = "<<";
		btnPrev.BackColor = Settings.Default.c_button_inactive;
		btnPrev.ForeColor = Settings.Default.c_button_inactive_fore;
		btnPrev.ParentColor = BackColor;
		btnPrev.Click += btnPrev_Click;
		base.Controls.Add(btnPrev);
		btnNext.Text = ">>";
		btnNext.BackColor = Settings.Default.c_button_inactive;
		btnNext.ForeColor = Settings.Default.c_button_inactive_fore;
		btnNext.ParentColor = BackColor;
		btnNext.Click += btnNext_Click;
		base.Controls.Add(btnNext);
		lblHeader.Text = "Graphic EQ";
		lblHeader.TextAlign = ContentAlignment.TopCenter;
		lblHeader.BackColor = Color.Black;
		lblHeader.ForeColor = Color.Lime;
		base.Controls.Add(lblHeader);
		lblBands.Text = "31 Bands Avail";
		lblBands.TextAlign = ContentAlignment.BottomRight;
		lblBands.BackColor = Color.Black;
		lblBands.ForeColor = Color.Lime;
		base.Controls.Add(lblBands);
		string[] array2 = new string[11]
		{
			"+15", "+12", "", "+6", "", "0", "", "-6", "", "-12",
			"-15"
		};
		for (int j = 0; j < array2.Length; j++)
		{
			Label label2 = new Label();
			label2.Text = array2[j];
			label2.ForeColor = Color.White;
			label2.BackColor = BackColor;
			label2.FlatStyle = FlatStyle.System;
			label2.Font = new Font("Arial", 10f, FontStyle.Bold);
			label2.TextAlign = ContentAlignment.MiddleCenter;
			panelLevels.Controls.Add(label2);
			arrGainLabelsL.Add(label2);
			Label label3 = new Label();
			label3.Text = array2[j];
			label3.ForeColor = Color.White;
			label3.BackColor = BackColor;
			label3.FlatStyle = FlatStyle.System;
			label3.Font = new Font("Arial", 10f, FontStyle.Bold);
			label3.TextAlign = ContentAlignment.MiddleCenter;
			panelLevels.Controls.Add(label3);
			arrGainLabelsR.Add(label3);
		}
	}

	private void btnOn_Click(object sender, EventArgs e)
	{
		MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQOn, 0, (MixEngine.getParam(activeRack, MixEngine.Pr.GEQOn, 0) != 1) ? 1 : 0);
	}

	private void btnLink_Click(object sender, EventArgs e)
	{
		if (MixEngine.getParam(activeRack, MixEngine.Pr.GEQType, 0) == 0)
		{
			if (MixEngine.getParam(activeRack, MixEngine.Pr.GEQLink, 0) == 0)
			{
				if (askQuestion("Linking will copy these settings to the other GEQ. Proceed?") == DialogResult.Yes)
				{
					MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQLink, 0, activeRack + 1);
				}
			}
			else
			{
				MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQLink, 0, 0);
			}
		}
		else
		{
			if (MixEngine.getParam(activeRack, MixEngine.Pr.GEQType, 0) != 1)
			{
				return;
			}
			if (MixEngine.getParam(activeRack, MixEngine.Pr.GEQLinkLR, 0) == 0)
			{
				if (askQuestion("Linking will copy these settings to the other GEQ. Proceed?") == DialogResult.Yes)
				{
					MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQLinkLR, 0, (activeRack <= 7) ? 1 : 2);
				}
			}
			else
			{
				MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQLinkLR, 0, 0);
			}
		}
	}

	private void btnMax_Click(object sender, EventArgs e)
	{
		if (base.FormBorderStyle == FormBorderStyle.Sizable)
		{
			base.WindowState = FormWindowState.Normal;
			base.FormBorderStyle = FormBorderStyle.None;
			base.Bounds = Screen.PrimaryScreen.Bounds;
			btnMax.BackColor = Settings.Default.c_button_active;
		}
		else
		{
			base.Bounds = new Rectangle(thisWin.WindowPos, thisWin.WindowSize);
			base.FormBorderStyle = FormBorderStyle.Sizable;
			btnMax.BackColor = Settings.Default.c_button_inactive;
		}
	}

	private void btnClose_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void btnPrev_Click(object sender, EventArgs e)
	{
		int num = activeRack;
		if (num <= 0)
		{
			return;
		}
		if (num >= 8)
		{
			if (MixEngine.RackData[num - 8].Type == 1 || MixEngine.RackData[num - 8].Type == 2)
			{
				ActiveRack = num - 8;
				return;
			}
			num -= 8;
		}
		while (num > 0)
		{
			num--;
			if (MixEngine.RackData[num].Type == 2)
			{
				ActiveRack = num + 8;
				break;
			}
			if (MixEngine.RackData[num].Type == 1)
			{
				ActiveRack = num;
				break;
			}
		}
	}

	private void btnNext_Click(object sender, EventArgs e)
	{
		int num = activeRack;
		if (num >= 15)
		{
			return;
		}
		if (num < 8 && MixEngine.RackData[num].Type == 2)
		{
			ActiveRack = num + 8;
			return;
		}
		num %= 8;
		while (num < 8)
		{
			num++;
			if (MixEngine.RackData[num].Type == 1 || MixEngine.RackData[num].Type == 2)
			{
				ActiveRack = num;
				break;
			}
		}
	}

	private void btnFlatSel_Click(object sender, EventArgs e)
	{
		MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQBand, selectedBand, 0);
	}

	private void btnFlatAll_Click(object sender, EventArgs e)
	{
		if (askQuestion("Flatten All Bands?") == DialogResult.Yes)
		{
			MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQFlat, 0, 1);
		}
	}

	private DialogResult askQuestion(string question)
	{
		frmConfirm frmConfirm2 = new frmConfirm();
		frmConfirm2.Question = question;
		DialogResult result = frmConfirm2.ShowDialog(this);
		frmConfirm2.Dispose();
		return result;
	}

	private void setActiveBand(int band)
	{
		if (band != selectedBand)
		{
			arrLevels[selectedBand].IsActive = false;
			arrLabels[selectedBand].BackColor = arrLevels[selectedBand].BackColor;
			arrLabels[selectedBand].ForeColor = Settings.Default.c_fader_fore;
			selectedBand = band;
			faderGEQGain.Value = arrLevels[selectedBand].Value;
			faderGEQGain.FaderColor = arrLevels[selectedBand].FaderColor;
			faderGEQBand.Value = selectedBand;
			arrLevels[selectedBand].IsActive = true;
			arrLevels[selectedBand].Focus();
			arrLabels[selectedBand].BackColor = Color.Goldenrod;
			arrLabels[selectedBand].ForeColor = Color.Black;
		}
	}

	private void meterGEQBand_GotFocus(object sender, EventArgs e)
	{
		setActiveBand(((AirControls.AirFader)sender).Channel);
	}

	private void meterGEQLabel_Click(object sender, EventArgs e)
	{
		setActiveBand(int.Parse(((Label)sender).Tag.ToString()));
	}

	private void meterGEQBand_FaderChanged(int channel, int value, string flag)
	{
		if (value != 0)
		{
			arrLevels[channel].FaderColor = "Yellow";
		}
		else
		{
			arrLevels[channel].FaderColor = "Blue";
		}
		MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQBand, channel, value);
	}

	private void frmGraphicEQ_Resize(object sender, EventArgs e)
	{
		if (base.WindowState == FormWindowState.Normal && base.FormBorderStyle == FormBorderStyle.Sizable)
		{
			thisWin.WindowSize = base.Size;
		}
		doLayout();
	}

	private void faderGEQGain_FaderChanged(int channel, int value, string flag)
	{
		arrLevels[selectedBand].Value = value;
		if (arrLevels[selectedBand].Value == 0)
		{
			arrLevels[selectedBand].FaderColor = "Blue";
		}
		else
		{
			arrLevels[selectedBand].FaderColor = "Yellow";
		}
		faderGEQGain.FaderColor = arrLevels[selectedBand].FaderColor;
		MixEngine.setParam("UI", activeRack, MixEngine.Pr.GEQBand, selectedBand, value);
	}

	private void faderGEQBand_FaderChanged(int channel, int value, string flag)
	{
		if (selectedBand != value)
		{
			setActiveBand(value);
		}
	}

	private void doLayout()
	{
		btnOn.Height = (int)Math.Round((double)base.ClientRectangle.Height * 0.08);
		btnOn.Width = (int)Math.Round((double)base.ClientRectangle.Width * 0.08);
		btnOn.Top = 10;
		btnOn.Left = 10;
		Font font = FontFit("Arial", FontStyle.Bold, "Cancel\nThis", btnOn.Width - 6, btnOn.Height - 6, 20f);
		btnOn.Font = font;
		btnFlatSel.Font = font;
		btnFlatSel.Size = btnOn.Size;
		btnFlatSel.Location = new Point(btnOn.Right + 20, btnOn.Top);
		btnFlatAll.Font = font;
		btnFlatAll.Size = btnOn.Size;
		btnFlatAll.Location = new Point(btnFlatSel.Right + 10, btnOn.Top);
		btnLink.Font = font;
		btnLink.Size = btnOn.Size;
		btnLink.Location = new Point(btnFlatAll.Right + 10, btnOn.Top);
		faderGEQBand.Height = (int)Math.Round((double)base.ClientRectangle.Height * 0.08);
		if (faderGEQBand.Height < 50)
		{
			faderGEQBand.Height = 50;
		}
		faderGEQBand.Top = base.ClientRectangle.Height - faderGEQBand.Height - 10;
		faderGEQBand.Left = 10;
		faderGEQGain.Width = (int)Math.Round((double)faderGEQBand.Height * 1.1);
		faderGEQGain.Left = base.ClientRectangle.Width - faderGEQGain.Width - 10;
		faderGEQGain.Top = 10;
		faderGEQGain.Height = base.ClientRectangle.Height - 20;
		faderGEQBand.Width = faderGEQGain.Left - 20;
		panelLevels.Location = new Point(10, btnOn.Bottom + 10);
		panelLevels.Width = faderGEQGain.Left - 20;
		panelLevels.Height = faderGEQBand.Top - panelLevels.Top - 10;
		btnClose.Font = font;
		btnClose.Size = btnOn.Size;
		btnClose.Location = new Point(faderGEQGain.Left - btnClose.Width - 10, btnOn.Top);
		btnMax.Font = font;
		btnMax.Size = btnOn.Size;
		btnMax.Location = new Point(btnClose.Left - btnMax.Width - 10, btnOn.Top);
		btnNext.Font = font;
		btnNext.Size = btnOn.Size;
		btnNext.Location = new Point(btnMax.Left - btnNext.Width - 10, btnOn.Top);
		btnPrev.Font = font;
		btnPrev.Size = btnOn.Size;
		btnPrev.Location = new Point(btnNext.Left - btnPrev.Width - 10, btnOn.Top);
		lblHeader.Left = btnLink.Right + 10;
		lblHeader.Width = btnPrev.Left - btnLink.Right - 20;
		lblHeader.Height = (int)Math.Round((double)btnOn.Height * 0.66);
		lblHeader.Top = btnOn.Top;
		fontLabel = FontFit("Arial", FontStyle.Bold, lblHeader.Text, lblHeader.Width - 6, lblHeader.Height - 4, 16f);
		lblHeader.Font = fontLabel;
		lblBands.Left = lblHeader.Left;
		lblBands.Width = lblHeader.Width;
		lblBands.Height = btnOn.Height - lblHeader.Height;
		lblBands.Top = lblHeader.Bottom;
		fontLabel = FontFit("Arial", FontStyle.Bold, lblBands.Text, lblBands.Width - 6, lblBands.Height - 2, 16f);
		lblBands.Font = fontLabel;
		double num = Math.Round((double)panelLevels.Width / ((double)arrLevels.Count + 2.0) - 0.0);
		Size size = new Size((int)Math.Round(num) - 1, panelLevels.Height - 26);
		fontLabel = FontFit("Arial", FontStyle.Bold, "12.5k", size.Width, 24, 16f);
		for (int i = 0; i < arrLevels.Count; i++)
		{
			arrLabels[i].Width = size.Width;
			arrLabels[i].Height = 20;
			arrLabels[i].Location = new Point((int)Math.Round((double)(i + 1) * (num + 0.0)), 5);
			arrLabels[i].Font = fontLabel;
			arrLevels[i].Size = size;
			arrLevels[i].Location = new Point((int)Math.Round((double)(i + 1) * (num + 0.0)), 25);
		}
		fontLabel = FontFit("Arial", FontStyle.Bold, "+15", (int)Math.Round((double)size.Width / 1.0) - 4, 24, 16f);
		for (int j = 0; j < arrGainLabelsL.Count; j++)
		{
			arrGainLabelsL[j].Font = fontLabel;
			arrGainLabelsL[j].Width = (int)Math.Round((double)size.Width / 1.0) - 1;
			arrGainLabelsL[j].Height = 20;
			arrGainLabelsL[j].Location = new Point(1, (int)Math.Round((double)j / 10.0 * (double)((float)size.Height * 0.75f) + 22.0 + (double)((float)size.Height * 0.13f)));
			arrGainLabelsR[j].Font = fontLabel;
			arrGainLabelsR[j].Width = (int)Math.Round((double)size.Width / 1.0) - 1;
			arrGainLabelsR[j].Height = 20;
			arrGainLabelsR[j].Location = new Point((int)num * 32, (int)Math.Round((double)j / 10.0 * (double)((float)size.Height * 0.75f) + 22.0 + (double)((float)size.Height * 0.13f)));
		}
		panelLevels.Invalidate();
	}

	private void frmGraphicEQ_Deactivate(object sender, EventArgs e)
	{
		thisWin.WindowState = ((base.FormBorderStyle == FormBorderStyle.None) ? "Fullscreen" : base.WindowState.ToString());
		thisWin.Page = selectedBand;
		Settings.Default.windowGEQ = WindowStatic.Store(thisWin);
		Settings.Default.Save();
	}

	private void frmGraphicEQ_LocationChanged(object sender, EventArgs e)
	{
		if (base.WindowState == FormWindowState.Normal && base.FormBorderStyle == FormBorderStyle.Sizable)
		{
			thisWin.WindowPos = base.Location;
		}
	}

	private void frmGraphicEQ_FormClosing(object sender, FormClosingEventArgs e)
	{
	}

	private void frmGraphicEQ_FormClosed(object sender, FormClosedEventArgs e)
	{
		MixEngine.EngineChanged -= MixEngine_EngineChanged;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmGraphicEQ));
		this.faderGEQBand = new AirControls.AirFader();
		this.faderGEQGain = new AirControls.AirFader();
		base.SuspendLayout();
		this.faderGEQBand.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.faderGEQBand.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.faderGEQBand.Channel = 0;
		this.faderGEQBand.FaderColor = "Blue";
		this.faderGEQBand.FaderStyle = 23;
		this.faderGEQBand.FaderTails = false;
		this.faderGEQBand.FineMode = false;
		this.faderGEQBand.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.faderGEQBand.Horizontal = true;
		this.faderGEQBand.IsActive = false;
		this.faderGEQBand.KnobColor = "White";
		this.faderGEQBand.LabelType = "geq";
		this.faderGEQBand.Location = new System.Drawing.Point(12, 397);
		this.faderGEQBand.Maximum = 30;
		this.faderGEQBand.MeterLevel = 0;
		this.faderGEQBand.MeterType = "side";
		this.faderGEQBand.Minimum = 0;
		this.faderGEQBand.Name = "faderGEQBand";
		this.faderGEQBand.ShowKnob = false;
		this.faderGEQBand.ShowMeter = false;
		this.faderGEQBand.Size = new System.Drawing.Size(651, 62);
		this.faderGEQBand.Steps = 1;
		this.faderGEQBand.TabIndex = 22;
		this.faderGEQBand.TailColor = System.Drawing.Color.Coral;
		this.faderGEQBand.TickType = "geq";
		this.faderGEQBand.Value = 30;
		this.faderGEQBand.ValueDB = new decimal(new int[4] { 8100, 0, 0, -2147352576 });
		this.faderGEQBand.ValueInt = 30;
		this.faderGEQBand.ValueType = "geq";
		this.faderGEQBand.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(faderGEQBand_FaderChanged);
		this.faderGEQGain.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.faderGEQGain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.faderGEQGain.Channel = 0;
		this.faderGEQGain.FaderColor = "White";
		this.faderGEQGain.FaderStyle = 0;
		this.faderGEQGain.FaderTails = false;
		this.faderGEQGain.FineMode = false;
		this.faderGEQGain.ForeColor = System.Drawing.Color.WhiteSmoke;
		this.faderGEQGain.Horizontal = false;
		this.faderGEQGain.IsActive = false;
		this.faderGEQGain.KnobColor = "White";
		this.faderGEQGain.LabelType = "div10";
		this.faderGEQGain.Location = new System.Drawing.Point(671, 13);
		this.faderGEQGain.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.faderGEQGain.Maximum = 150;
		this.faderGEQGain.MeterLevel = 0;
		this.faderGEQGain.MeterType = "side";
		this.faderGEQGain.Minimum = -150;
		this.faderGEQGain.Name = "faderGEQGain";
		this.faderGEQGain.ShowKnob = false;
		this.faderGEQGain.ShowMeter = false;
		this.faderGEQGain.Size = new System.Drawing.Size(66, 445);
		this.faderGEQGain.Steps = 5;
		this.faderGEQGain.TabIndex = 21;
		this.faderGEQGain.TailColor = System.Drawing.Color.Coral;
		this.faderGEQGain.TickType = "none";
		this.faderGEQGain.Value = 150;
		this.faderGEQGain.ValueDB = new decimal(new int[4] { 5460, 0, 0, -2147352576 });
		this.faderGEQGain.ValueInt = 150;
		this.faderGEQGain.ValueType = "div10";
		this.faderGEQGain.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(faderGEQGain_FaderChanged);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackColor = AirFader.Properties.Settings.Default.c_background;
		base.ClientSize = new System.Drawing.Size(857, 471);
		base.Controls.Add(this.faderGEQBand);
		base.Controls.Add(this.faderGEQGain);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "frmGraphicEQ";
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "GraphicEQ";
		base.TopMost = true;
		base.Deactivate += new System.EventHandler(frmGraphicEQ_Deactivate);
		base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(frmGraphicEQ_FormClosed);
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(frmGraphicEQ_FormClosing);
		base.Resize += new System.EventHandler(frmGraphicEQ_Resize);
		base.LocationChanged += new System.EventHandler(frmGraphicEQ_LocationChanged);
		base.ResumeLayout(false);
	}
}
