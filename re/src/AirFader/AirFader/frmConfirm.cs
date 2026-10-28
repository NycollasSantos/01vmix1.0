using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using AirControls;

namespace AirFader;

public class frmConfirm : Form
{
	public enum confirmTypes
	{
		YesNo,
		Ok,
		Fail,
		OkUpload
	}

	private string _question = "";

	private confirmTypes confirmType;

	private IContainer components;

	private Label lblConfirm;

	private AirButton btnNo;

	private AirButton btnYes;

	public string Question
	{
		get
		{
			return _question;
		}
		set
		{
			_question = value;
			lblConfirm.Text = _question;
		}
	}

	public static DialogResult askQuestion(string question, string title)
	{
		return askQuestion(question, title, null);
	}

	public static DialogResult askQuestion(string question, string title, IWin32Window parent)
	{
		try
		{
			frmConfirm frmConfirm2 = new frmConfirm();
			frmConfirm2.Question = question;
			frmConfirm2.setType(confirmTypes.YesNo);
			frmConfirm2.Text = title;
			DialogResult result = frmConfirm2.ShowDialog(parent);
			frmConfirm2.Dispose();
			return result;
		}
		catch (Exception)
		{
			return DialogResult.Cancel;
		}
	}

	public static DialogResult sayOK(string question, string title)
	{
		return sayOK(question, title, null);
	}

	public static DialogResult sayOK(string question, string title, IWin32Window parent)
	{
		try
		{
			frmConfirm frmConfirm2 = new frmConfirm();
			frmConfirm2.Question = question;
			frmConfirm2.setType(confirmTypes.Ok);
			frmConfirm2.Text = title;
			DialogResult result = frmConfirm2.ShowDialog(parent);
			frmConfirm2.Dispose();
			return result;
		}
		catch (Exception)
		{
			return DialogResult.Cancel;
		}
	}

	public static DialogResult sayFail(string question, string title)
	{
		return sayFail(question, title, null);
	}

	public static DialogResult sayFail(string question, string title, IWin32Window parent)
	{
		try
		{
			frmConfirm frmConfirm2 = new frmConfirm();
			frmConfirm2.Question = question;
			frmConfirm2.setType(confirmTypes.OkUpload);
			frmConfirm2.Text = title;
			DialogResult result = frmConfirm2.ShowDialog();
			frmConfirm2.Dispose();
			return result;
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("sayFail() error " + ex.ToString());
			return DialogResult.Cancel;
		}
	}

	public void setType(confirmTypes newType)
	{
		confirmType = newType;
	}

	public frmConfirm()
	{
		InitializeComponent();
		btnYes.BackColor = SystemColors.Control;
		btnYes.LEDColor = SystemColors.Control;
		btnYes.ParentColor = SystemColors.Control;
		btnNo.BackColor = SystemColors.Control;
		btnNo.LEDColor = SystemColors.Control;
		btnNo.ParentColor = SystemColors.Control;
		BackColor = SystemColors.Control;
	}

	private void frmConfirm_Load(object sender, EventArgs e)
	{
		switch (confirmType)
		{
		case confirmTypes.YesNo:
			btnYes.BackColor = Color.Lime;
			btnNo.BackColor = Color.MistyRose;
			btnYes.Text = "Yes";
			btnNo.Text = "No";
			btnNo.Show();
			btnYes.Left = base.ClientRectangle.Width / 2 - btnYes.Width - 20;
			btnNo.Left = btnYes.Right + 40;
			break;
		case confirmTypes.Fail:
			btnYes.BackColor = Color.MistyRose;
			btnYes.Text = "Ok";
			btnNo.Hide();
			btnYes.Left = base.ClientRectangle.Width / 2 - btnYes.Width / 2;
			break;
		case confirmTypes.OkUpload:
			btnYes.BackColor = Color.Lime;
			btnNo.BackColor = Color.MistyRose;
			btnYes.Text = "Report";
			btnNo.Text = "Ok";
			btnNo.Show();
			btnYes.Left = base.ClientRectangle.Width / 2 - btnYes.Width - 20;
			btnNo.Left = btnYes.Right + 40;
			break;
		default:
			btnYes.BackColor = SystemColors.Control;
			btnYes.Text = "Ok!";
			btnNo.Hide();
			btnYes.Left = base.ClientRectangle.Width / 2 - btnYes.Width / 2;
			break;
		}
		Loc.Apply(this);
	}

	private void btnNo_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.No;
		Close();
	}

	private void btnYes_Click(object sender, EventArgs e)
	{
		if (confirmType == confirmTypes.YesNo)
		{
			base.DialogResult = DialogResult.Yes;
		}
		else
		{
			base.DialogResult = DialogResult.OK;
		}
		Close();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmConfirm));
		this.lblConfirm = new System.Windows.Forms.Label();
		this.btnNo = new AirControls.AirButton();
		this.btnYes = new AirControls.AirButton();
		base.SuspendLayout();
		this.lblConfirm.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblConfirm.ForeColor = System.Drawing.Color.Black;
		this.lblConfirm.Location = new System.Drawing.Point(12, 9);
		this.lblConfirm.Name = "lblConfirm";
		this.lblConfirm.Size = new System.Drawing.Size(403, 114);
		this.lblConfirm.TabIndex = 54;
		this.lblConfirm.Text = "Are you sure?";
		this.lblConfirm.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnNo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnNo.BackText = "";
		this.btnNo.BorderColor = System.Drawing.Color.Black;
		this.btnNo.DialogResult = System.Windows.Forms.DialogResult.No;
		this.btnNo.Font = new System.Drawing.Font("Arial", 18f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnNo.LEDColor = System.Drawing.SystemColors.Control;
		this.btnNo.Location = new System.Drawing.Point(223, 126);
		this.btnNo.Name = "btnNo";
		this.btnNo.ParentColor = System.Drawing.SystemColors.Control;
		this.btnNo.Size = new System.Drawing.Size(104, 55);
		this.btnNo.TabIndex = 53;
		this.btnNo.Text = "No";
		this.btnNo.Click += new System.EventHandler(btnNo_Click);
		this.btnYes.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnYes.BackText = "";
		this.btnYes.BorderColor = System.Drawing.Color.Black;
		this.btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
		this.btnYes.Font = new System.Drawing.Font("Arial", 18f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnYes.LEDColor = System.Drawing.SystemColors.Control;
		this.btnYes.Location = new System.Drawing.Point(100, 126);
		this.btnYes.Name = "btnYes";
		this.btnYes.ParentColor = System.Drawing.SystemColors.Control;
		this.btnYes.Size = new System.Drawing.Size(104, 55);
		this.btnYes.TabIndex = 52;
		this.btnYes.Text = "Yes";
		this.btnYes.Click += new System.EventHandler(btnYes_Click);
		base.AcceptButton = this.btnYes;
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackColor = System.Drawing.SystemColors.Control;
		base.CancelButton = this.btnNo;
		base.ClientSize = new System.Drawing.Size(427, 190);
		base.Controls.Add(this.btnNo);
		base.Controls.Add(this.btnYes);
		base.Controls.Add(this.lblConfirm);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		base.Name = "frmConfirm";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Info";
		base.Load += new System.EventHandler(frmConfirm_Load);
		base.ResumeLayout(false);
	}
}
