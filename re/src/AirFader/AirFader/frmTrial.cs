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

public class frmTrial : Form
{
	private IContainer components;

	private TextBox txtEmail;

	private Button btnRegister;

	private TextBox txtOwnerName;

	private Label lblOwnerName;

	private Button btnCancel;

	private Label label2;

	private Label label3;

	private Button btnGoReg;

	public frmTrial()
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
		if (txtOwnerName.Text.Length < 1)
		{
			frmConfirm.sayFail("Owner Name is required", "Registration Error");
			return;
		}
		if (txtEmail.Text.Length < 5 || !txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
		{
			frmConfirm.sayFail("A valid Email Address is required", "Registration Error");
			return;
		}
		btnRegister.Text = "Getting key...";
		btnRegister.Enabled = false;
		if (!MixEngine.LICENSING_ENABLED)
		{
			// Open-source build: no trial key is issued and no data is sent to
			// the vendor. Everything is already unlocked.
			frmConfirm.sayOK("This is the free, open-source build of 01vmix.\nNo trial key is required - all features are unlocked.", "Trial", MixEngine.hMainWindow);
			btnRegister.Text = "Request Trial";
			btnRegister.Enabled = true;
			Close();
			return;
		}
		string text = "";
		string text2 = "";
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
					frmConfirm.sayOK("A Global License Key has already been set!\nTo over-write this key, run 01vmix as an Administrator.", "Global Key");
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
		btnRegister.Text = "Request Trial";
		btnRegister.Enabled = true;
	}

	private void frmRegister_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		txtOwnerName.Text = "";
		txtEmail.Text = "";
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void txtLicenseKey_TextChanged(object sender, EventArgs e)
	{
	}

	private void btnGoReg_Click(object sender, EventArgs e)
	{
		frmUpdate frmUpdate2 = new frmUpdate();
		if (frmUpdate2.ShowDialog() != DialogResult.Cancel)
		{
			Close();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmTrial));
		this.txtEmail = new System.Windows.Forms.TextBox();
		this.btnRegister = new System.Windows.Forms.Button();
		this.txtOwnerName = new System.Windows.Forms.TextBox();
		this.lblOwnerName = new System.Windows.Forms.Label();
		this.btnCancel = new System.Windows.Forms.Button();
		this.label2 = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.btnGoReg = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.txtEmail.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.txtEmail.Location = new System.Drawing.Point(129, 131);
		this.txtEmail.Name = "txtEmail";
		this.txtEmail.Size = new System.Drawing.Size(273, 22);
		this.txtEmail.TabIndex = 2;
		this.btnRegister.Location = new System.Drawing.Point(219, 168);
		this.btnRegister.Name = "btnRegister";
		this.btnRegister.Size = new System.Drawing.Size(102, 33);
		this.btnRegister.TabIndex = 3;
		this.btnRegister.Text = "Request Trial";
		this.btnRegister.UseVisualStyleBackColor = true;
		this.btnRegister.Click += new System.EventHandler(btnRegister_Click);
		this.txtOwnerName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.txtOwnerName.Location = new System.Drawing.Point(129, 103);
		this.txtOwnerName.Name = "txtOwnerName";
		this.txtOwnerName.Size = new System.Drawing.Size(273, 22);
		this.txtOwnerName.TabIndex = 1;
		this.lblOwnerName.AutoSize = true;
		this.lblOwnerName.Location = new System.Drawing.Point(42, 108);
		this.lblOwnerName.Name = "lblOwnerName";
		this.lblOwnerName.Size = new System.Drawing.Size(69, 13);
		this.lblOwnerName.TabIndex = 5;
		this.lblOwnerName.Text = "Owner Name";
		this.btnCancel.Location = new System.Drawing.Point(327, 168);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 33);
		this.btnCancel.TabIndex = 4;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
		this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(12, 9);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(435, 76);
		this.label2.TabIndex = 6;
		this.label2.Text = "01vmix does not need a license key.\r\n\r\nThis is the free, open-source build - all features are unlocked.";
		this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(42, 136);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(73, 13);
		this.label3.TabIndex = 7;
		this.label3.Text = "Email Address";
		this.btnGoReg.Location = new System.Drawing.Point(45, 168);
		this.btnGoReg.Name = "btnGoReg";
		this.btnGoReg.Size = new System.Drawing.Size(133, 33);
		this.btnGoReg.TabIndex = 8;
		this.btnGoReg.Text = "I already have one!";
		this.btnGoReg.UseVisualStyleBackColor = true;
		this.btnGoReg.Click += new System.EventHandler(btnGoReg_Click);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		base.ClientSize = new System.Drawing.Size(459, 217);
		base.Controls.Add(this.btnGoReg);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.lblOwnerName);
		base.Controls.Add(this.txtOwnerName);
		base.Controls.Add(this.btnRegister);
		base.Controls.Add(this.txtEmail);
		base.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
		base.Name = "frmTrial";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "No License Key";
		base.Load += new System.EventHandler(frmRegister_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
