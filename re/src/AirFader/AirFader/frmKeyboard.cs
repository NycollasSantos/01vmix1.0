using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;

namespace AirFader;

public class frmKeyboard : Form
{
	private TextBox newText;

	private AirButton btnSave;

	private AirButton btnCancel;

	private AirButton btnSpace;

	private AirButton btnBack;

	private AirButton btnDel;

	private AirButton btnClear;

	private AirButton btnShift;

	private AirButton btnShiftLock;

	private Label lblProperty;

	private List<AirButton> arrKeys = new List<AirButton>(47);

	private int activeIndex;

	private string activeProperty = "";

	private int shiftMode;

	private string keyMap = "`1234567890-=\\qwertyuiop[]asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+|QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?";

	private bool isResizing;

	private IContainer components;

	private Font GetFontForTextBoxHeight(int TextBoxHeight, Font OriginalFont)
	{
		float num = TextBoxHeight;
		Font font = new Font(OriginalFont.FontFamily, OriginalFont.Size, OriginalFont.Style, GraphicsUnit.Pixel);
		if (num < 8f)
		{
			num = 8f;
		}
		float num2 = font.FontFamily.GetEmHeight(font.Style);
		float num3 = font.FontFamily.GetLineSpacing(font.Style);
		float emSize = (num - 7f) * num2 / num3;
		return new Font(font.FontFamily, emSize, font.Style, GraphicsUnit.Pixel);
	}

	public frmKeyboard()
	{
		InitializeComponent();
		newText = new TextBox();
		newText.ForeColor = Color.Lime;
		newText.BackColor = Color.Black;
		base.Controls.Add(newText);
		lblProperty = new Label();
		lblProperty.Font = newText.Font;
		lblProperty.TextAlign = ContentAlignment.MiddleLeft;
		lblProperty.ForeColor = Color.WhiteSmoke;
		base.Controls.Add(lblProperty);
		btnSave = new AirButton();
		btnSave.Text = "Save";
		btnSave.BackColor = Settings.Default.c_button_inactive;
		btnSave.ParentColor = BackColor;
		btnSave.Click += btnSave_Click;
		base.Controls.Add(btnSave);
		btnCancel = new AirButton();
		btnCancel.Text = "Cancel";
		btnCancel.BackColor = Settings.Default.c_button_inactive;
		btnCancel.ParentColor = BackColor;
		btnCancel.Click += btnCancel_Click;
		base.Controls.Add(btnCancel);
		btnSpace = new AirButton();
		btnSpace.Text = "SPACE";
		btnSpace.BackColor = Settings.Default.c_button_inactive;
		btnSpace.ParentColor = BackColor;
		btnSpace.Click += btnKey_Click;
		base.Controls.Add(btnSpace);
		btnBack = new AirButton();
		btnBack.Text = "BS";
		btnBack.BackColor = Settings.Default.c_button_inactive;
		btnBack.ParentColor = BackColor;
		btnBack.Click += btnKey_Click;
		base.Controls.Add(btnBack);
		btnDel = new AirButton();
		btnDel.Text = "DEL";
		btnDel.BackColor = Settings.Default.c_button_inactive;
		btnDel.ParentColor = BackColor;
		btnDel.Click += btnKey_Click;
		base.Controls.Add(btnDel);
		btnClear = new AirButton();
		btnClear.Text = "CLEAR";
		btnClear.BackColor = Settings.Default.c_button_inactive;
		btnClear.ParentColor = BackColor;
		btnClear.Click += btnKey_Click;
		base.Controls.Add(btnClear);
		btnShift = new AirButton();
		btnShift.Text = "SHIFT";
		btnShift.BackColor = Settings.Default.c_button_inactive;
		btnShift.ParentColor = BackColor;
		btnShift.Click += btnKey_Click;
		base.Controls.Add(btnShift);
		for (int i = 0; i < 47; i++)
		{
			AirButton airButton = new AirButton();
			airButton.BackColor = Settings.Default.c_button_inactive;
			airButton.ParentColor = BackColor;
			airButton.Text = keyMap.Substring(i, 1);
			airButton.Click += btnKey_Click;
			base.Controls.Add(airButton);
			arrKeys.Add(airButton);
		}
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		Dispose();
	}

	private void btnSave_Click(object sender, EventArgs e)
	{
		switch (activeProperty)
		{
		case "ChannelName":
			MixEngine.chData[activeIndex].setParam("UI", Channel.Pr.Name, 0, newText.Text);
			break;
		case "SceneName":
			MixEngine.setParam("UI", activeIndex, MixEngine.Pr.SceneName, 0, newText.Text);
			break;
		case "SaveScene":
			MixEngine.setParam("UI", activeIndex, MixEngine.Pr.SceneSave, 0, newText.Text);
			break;
		}
		Close();
	}

	public void setProperty(string property, int index)
	{
		activeIndex = index;
		activeProperty = property;
		switch (property)
		{
		case "ChannelName":
		{
			if (Console.type == ConsoleTypes.M7CL)
			{
				newText.MaxLength = 8;
			}
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				newText.MaxLength = 4;
			}
			else
			{
				newText.MaxLength = 6;
			}
			newText.Text = MixEngine.chData[index].name;
			string text = (MixEngine.chData[index].subChannel + 1).ToString();
			switch (MixEngine.chData[index].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				lblProperty.Text = "Channel #" + text + " Name";
				break;
			case ChannelTypes.Matrix:
				lblProperty.Text = "Matrix #" + text + " Name";
				break;
			case ChannelTypes.Mix:
				lblProperty.Text = "Mix #" + text + " Name";
				break;
			case ChannelTypes.Stereo:
				lblProperty.Text = "Stereo #" + text + " Name";
				break;
			}
			break;
		}
		case "SceneName":
			newText.MaxLength = 16;
			newText.Text = MixEngine.sceneData[index].sceneName;
			lblProperty.Text = "Rename Scene #" + index + " To...";
			break;
		case "SaveScene":
			newText.MaxLength = 16;
			newText.Text = Console.sceneName;
			lblProperty.Text = "Save To Scene #" + index + " As...";
			break;
		}
		newText.SelectionStart = newText.Text.Length;
		newText.SelectionLength = 0;
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

	private void doLayout()
	{
		Size size = new Size((int)Math.Floor((double)(base.ClientRectangle.Width - 20) / 14.0) - 5, (int)Math.Floor((double)(base.ClientRectangle.Height - 20) / 6.0) - 10);
		Font font = FontFit("Arial", FontStyle.Bold, "Save", size.Width - 2, size.Height - 2, 20f);
		btnBack.Font = font;
		btnBack.Width = (int)Math.Round((double)size.Width * 1.5);
		btnBack.Height = size.Height;
		btnBack.Location = new Point(base.ClientRectangle.Width - btnBack.Width - 10, 10);
		btnDel.Font = font;
		btnDel.Size = btnBack.Size;
		btnDel.Location = new Point(btnBack.Left - btnDel.Width - 5, 10);
		btnClear.Font = font;
		btnClear.Size = btnBack.Size;
		btnClear.Location = new Point(10, 10);
		lblProperty.Font = GetFontForTextBoxHeight((int)Math.Round((double)size.Height * 0.3) + 10, lblProperty.Font);
		lblProperty.Width = base.ClientRectangle.Width - (btnBack.Width + 5) * 6;
		lblProperty.Height = (int)Math.Round((double)size.Height * 0.3);
		lblProperty.Left = (int)Math.Round((double)base.ClientRectangle.Width / 2.0 - (double)lblProperty.Width / 2.0);
		lblProperty.Top = 10;
		newText.Top = lblProperty.Bottom + 5;
		newText.Left = lblProperty.Left;
		newText.Size = lblProperty.Size;
		newText.Font = GetFontForTextBoxHeight((int)Math.Round((double)size.Height * 0.6), newText.Font);
		int num = 10;
		int num2 = btnClear.Bottom + 10;
		for (int i = 0; i < arrKeys.Count; i++)
		{
			arrKeys[i].Font = font;
			arrKeys[i].Location = new Point(num, num2);
			arrKeys[i].Size = size;
			num += size.Width + 5;
			if (i == 13)
			{
				num = (int)((double)size.Width * 1.5) + 10;
				num2 += size.Height + 10;
			}
			if (i == 25)
			{
				num = (int)((double)size.Width * 2.0) + 10;
				num2 += size.Height + 10;
			}
			if (i == 36)
			{
				num = (int)((double)size.Width * 2.5) + 10;
				num2 += size.Height + 10;
				btnShift.Font = font;
				btnShift.Width = (int)Math.Round((double)size.Width * 1.5);
				btnShift.Height = size.Height;
				btnShift.Location = new Point(10, num2);
			}
		}
		num = (int)((double)size.Width * 2.5) + 10;
		num2 += size.Height + 10;
		btnSpace.Font = font;
		btnSpace.Width = (int)Math.Round((double)base.ClientRectangle.Width / 3.0);
		btnSpace.Height = size.Height;
		btnSpace.Left = (int)Math.Round((double)base.ClientRectangle.Width / 3.0);
		btnSpace.Top = num2;
		btnCancel.Font = font;
		btnCancel.Size = btnBack.Size;
		btnCancel.Left = base.ClientRectangle.Width - btnCancel.Width - 10;
		btnCancel.Top = num2;
		btnSave.Font = font;
		btnSave.Size = btnBack.Size;
		btnSave.Left = btnCancel.Left - btnSave.Width - 10;
		btnSave.Top = num2;
	}

	private void frmKeyboard_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		doLayout();
	}

	private void btnKey_Click(object sender, EventArgs e)
	{
		int selectionStart = newText.SelectionStart;
		string text = ((AirButton)sender).Text;
		string text2 = "";
		switch (text.ToUpper())
		{
		case "SPACE":
			if (newText.Text.Length < newText.MaxLength)
			{
				text2 = newText.Text.Substring(0, selectionStart) + " " + newText.Text.Substring(selectionStart);
				newText.Text = text2;
				newText.SelectionStart = selectionStart + 1;
			}
			break;
		case "BS":
			if (selectionStart > 0)
			{
				text2 = newText.Text.Substring(0, selectionStart - 1) + newText.Text.Substring(selectionStart);
				newText.Text = text2;
				newText.SelectionStart = selectionStart - 1;
			}
			break;
		case "DEL":
			if (selectionStart < newText.Text.Length)
			{
				text2 = newText.Text.Substring(0, selectionStart) + newText.Text.Substring(selectionStart + 1);
				newText.Text = text2;
				newText.SelectionStart = selectionStart;
			}
			break;
		case "SHIFT":
		{
			if (shiftMode == 47)
			{
				shiftMode = 0;
				btnShift.BackColor = Settings.Default.c_button_inactive;
			}
			else
			{
				shiftMode = 47;
				btnShift.BackColor = Settings.Default.c_button_active;
			}
			for (int i = 0; i < arrKeys.Count; i++)
			{
				arrKeys[i].Text = keyMap.Substring(i + shiftMode, 1);
			}
			break;
		}
		case "CLEAR":
			newText.Text = "";
			newText.SelectionStart = 0;
			break;
		default:
			if (newText.Text.Length < newText.MaxLength)
			{
				text2 = newText.Text.Substring(0, selectionStart) + text + newText.Text.Substring(selectionStart);
				newText.Text = text2;
				newText.SelectionStart = selectionStart + 1;
			}
			break;
		}
		newText.Focus();
	}

	private void frmKeyboard_Resize(object sender, EventArgs e)
	{
		if (!isResizing)
		{
			doLayout();
		}
	}

	private void frmKeyboard_ResizeBegin(object sender, EventArgs e)
	{
		isResizing = true;
	}

	private void frmKeyboard_ResizeEnd(object sender, EventArgs e)
	{
		isResizing = false;
		doLayout();
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
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		base.ClientSize = new System.Drawing.Size(762, 397);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
		base.Name = "frmKeyboard";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Keyboard";
		base.TopMost = true;
		base.Load += new System.EventHandler(frmKeyboard_Load);
		base.ResizeBegin += new System.EventHandler(frmKeyboard_ResizeBegin);
		base.Resize += new System.EventHandler(frmKeyboard_Resize);
		base.ResizeEnd += new System.EventHandler(frmKeyboard_ResizeEnd);
		base.ResumeLayout(false);
	}
}
