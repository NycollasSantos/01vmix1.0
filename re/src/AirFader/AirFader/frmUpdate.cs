using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Web;
using System.Windows.Forms;
using AirFader.Properties;

namespace AirFader;

public class frmUpdate : Form
{
	private IContainer components;

	private TextBox txtLicenseKey;

	private Label label1;

	private Button btnRegister;

	private TextBox txtOwnerName;

	private Label lblOwnerName;

	private Button btnCancel;

	private Button btnClear;

	private Label label2;

	public frmUpdate()
	{
		InitializeComponent();
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
			return text;
		}
		catch (UriFormatException)
		{
			return "SKIP";
		}
		catch (WebException)
		{
			return "SKIP";
		}
		catch (IOException)
		{
			return "SKIP";
		}
		finally
		{
			webResponse?.Close();
			streamReader?.Close();
		}
	}

	private void btnRegister_Click(object sender, EventArgs e)
	{
		btnRegister.Text = "Registering...";
		btnRegister.Enabled = false;
		txtLicenseKey.Text = txtLicenseKey.Text.ToUpper();
		if (!MixEngine.checkSerialV2(txtOwnerName.Text, txtLicenseKey.Text))
		{
			frmConfirm.sayFail("This License Key does not appear valid.", "Invalid Key");
			btnRegister.Text = "Register";
			btnRegister.Enabled = true;
			return;
		}
		// The key is validated and stored locally. The original build also
		// contacted the vendor's licensing server; this build never sends
		// anything to a remote server.
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
				frmConfirm.sayOK("A Global License Key has already been set!\nTo over-write this key, run 01vmix as an Administrator.", "Access Denied");
			}
			Settings.Default.LicenseKey = txtLicenseKey.Text;
			Settings.Default.LicenseUser = txtOwnerName.Text;
			Settings.Default.Save();
		}
		frmConfirm.sayOK("License Verified.\nThank you for supporting 01vmix development!", "Success");
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void frmRegister_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		txtOwnerName.Text = MixEngine.getLicenseUser();
		txtLicenseKey.Text = MixEngine.getLicenseKey();
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		base.DialogResult = DialogResult.Cancel;
		Close();
	}

	private void txtLicenseKey_TextChanged(object sender, EventArgs e)
	{
	}

	private void btnClear_Click(object sender, EventArgs e)
	{
		if (frmConfirm.askQuestion("This will remove your license information from this computer. If you have Internet access, it will mark your key available for a future install.\nContinue?", "Unregister") != DialogResult.Yes)
		{
			return;
		}
		try
		{
			btnClear.Text = "Unregistering...";
			btnClear.Enabled = false;
			// No remote call: the license data is only removed locally.
			bool flag = false;
			WindowsIdentity current = WindowsIdentity.GetCurrent();
			WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
			if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
			{
				MixEngine.updateAppConfig("GlobalKey", "");
				MixEngine.updateAppConfig("GlobalUser", "");
			}
			Settings.Default.LicenseKey = "";
			Settings.Default.LicenseUser = "";
			Settings.Default.Save();
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("btnClear_Click(): " + ex.ToString());
		}
		base.DialogResult = DialogResult.Cancel;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmUpdate));
		this.txtLicenseKey = new System.Windows.Forms.TextBox();
		this.label1 = new System.Windows.Forms.Label();
		this.btnRegister = new System.Windows.Forms.Button();
		this.txtOwnerName = new System.Windows.Forms.TextBox();
		this.lblOwnerName = new System.Windows.Forms.Label();
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnClear = new System.Windows.Forms.Button();
		this.label2 = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.txtLicenseKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.txtLicenseKey.Location = new System.Drawing.Point(98, 123);
		this.txtLicenseKey.Name = "txtLicenseKey";
		this.txtLicenseKey.Size = new System.Drawing.Size(240, 22);
		this.txtLicenseKey.TabIndex = 2;
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(27, 128);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(65, 13);
		this.label1.TabIndex = 1;
		this.label1.Text = "License Key";
		this.btnRegister.Location = new System.Drawing.Point(181, 151);
		this.btnRegister.Name = "btnRegister";
		this.btnRegister.Size = new System.Drawing.Size(75, 23);
		this.btnRegister.TabIndex = 3;
		this.btnRegister.Text = "Register";
		this.btnRegister.UseVisualStyleBackColor = true;
		this.btnRegister.Click += new System.EventHandler(btnRegister_Click);
		this.txtOwnerName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.txtOwnerName.Location = new System.Drawing.Point(98, 94);
		this.txtOwnerName.Name = "txtOwnerName";
		this.txtOwnerName.Size = new System.Drawing.Size(240, 22);
		this.txtOwnerName.TabIndex = 1;
		this.lblOwnerName.AutoSize = true;
		this.lblOwnerName.Location = new System.Drawing.Point(27, 99);
		this.lblOwnerName.Name = "lblOwnerName";
		this.lblOwnerName.Size = new System.Drawing.Size(69, 13);
		this.lblOwnerName.TabIndex = 5;
		this.lblOwnerName.Text = "Owner Name";
		this.btnCancel.Location = new System.Drawing.Point(263, 151);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 23);
		this.btnCancel.TabIndex = 4;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.btnClear.Location = new System.Drawing.Point(30, 151);
		this.btnClear.Name = "btnClear";
		this.btnClear.Size = new System.Drawing.Size(92, 23);
		this.btnClear.TabIndex = 6;
		this.btnClear.Text = "Unregister";
		this.btnClear.UseVisualStyleBackColor = true;
		this.btnClear.Click += new System.EventHandler(btnClear_Click);
		this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(12, 9);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(347, 76);
		this.label2.TabIndex = 7;
		this.label2.Text = "Please enter your Owner Name and License Key.\r\nBoth must match your email receipt exactly!\r\n\r\nTo get a Free Trial key, visit http://airfader.com/trial";
		this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		base.ClientSize = new System.Drawing.Size(371, 213);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.btnClear);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.lblOwnerName);
		base.Controls.Add(this.txtOwnerName);
		base.Controls.Add(this.btnRegister);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.txtLicenseKey);
		base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		base.Name = "frmUpdate";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Register";
		base.Load += new System.EventHandler(frmRegister_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
