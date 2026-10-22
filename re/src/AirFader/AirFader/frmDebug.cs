using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AirFader.Properties;

namespace AirFader;

public class frmDebug : Form
{
	private IContainer components;

	private Button cmdOpenLog;

	private Button cmdOpenFolder;

	private ComboBox cbLogLevel;

	private Button cmdSave;

	private Label lblDebug;

	private Label lblTitle;

	private Button btnSendLog;

	public frmDebug()
	{
		InitializeComponent();
	}

	private void cmdOpenLog_Click(object sender, EventArgs e)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			folderPath += "\\01vmix\\Error.Log";
			Process.Start(folderPath);
		}
		catch (Exception ex)
		{
			MixEngine.logError("cmdOpenLog()", "UI", ex.ToString());
		}
	}

	private void cmdOpenFolder_Click(object sender, EventArgs e)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			folderPath += "\\01vmix\\";
			Process.Start(folderPath);
		}
		catch (Exception ex)
		{
			MixEngine.logError("cmdOpenFolder()", "UI", ex.ToString());
		}
	}

	private void frmDebug_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		cbLogLevel.SelectedIndex = Settings.Default.debugLevel;
	}

	private void cmdSave_Click(object sender, EventArgs e)
	{
		Settings.Default.debugLevel = cbLogLevel.SelectedIndex;
		Settings.Default.Save();
		Close();
	}

	private void btnSendLog_Click(object sender, EventArgs e)
	{
		if (MixEngine.uploadLogFile())
		{
			Loc.Msg("Upload successful.");
		}
		else
		{
			Loc.Msg("Upload failed. Are you connected to the Internet right now?");
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmDebug));
		this.cmdOpenLog = new System.Windows.Forms.Button();
		this.cmdOpenFolder = new System.Windows.Forms.Button();
		this.cbLogLevel = new System.Windows.Forms.ComboBox();
		this.cmdSave = new System.Windows.Forms.Button();
		this.lblDebug = new System.Windows.Forms.Label();
		this.lblTitle = new System.Windows.Forms.Label();
		this.btnSendLog = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.cmdOpenLog.Location = new System.Drawing.Point(17, 51);
		this.cmdOpenLog.Name = "cmdOpenLog";
		this.cmdOpenLog.Size = new System.Drawing.Size(147, 37);
		this.cmdOpenLog.TabIndex = 0;
		this.cmdOpenLog.Text = "Open Log File";
		this.cmdOpenLog.UseVisualStyleBackColor = true;
		this.cmdOpenLog.Click += new System.EventHandler(cmdOpenLog_Click);
		this.cmdOpenFolder.Location = new System.Drawing.Point(17, 94);
		this.cmdOpenFolder.Name = "cmdOpenFolder";
		this.cmdOpenFolder.Size = new System.Drawing.Size(147, 37);
		this.cmdOpenFolder.TabIndex = 1;
		this.cmdOpenFolder.Text = "Open Log Folder";
		this.cmdOpenFolder.UseVisualStyleBackColor = true;
		this.cmdOpenFolder.Click += new System.EventHandler(cmdOpenFolder_Click);
		this.cbLogLevel.FormattingEnabled = true;
		this.cbLogLevel.Items.AddRange(new object[4] { "0", "1", "2", "3" });
		this.cbLogLevel.Location = new System.Drawing.Point(212, 67);
		this.cbLogLevel.Name = "cbLogLevel";
		this.cbLogLevel.Size = new System.Drawing.Size(147, 21);
		this.cbLogLevel.TabIndex = 2;
		this.cmdSave.Location = new System.Drawing.Point(212, 94);
		this.cmdSave.Name = "cmdSave";
		this.cmdSave.Size = new System.Drawing.Size(147, 37);
		this.cmdSave.TabIndex = 3;
		this.cmdSave.Text = "Save";
		this.cmdSave.UseVisualStyleBackColor = true;
		this.cmdSave.Click += new System.EventHandler(cmdSave_Click);
		this.lblDebug.AutoSize = true;
		this.lblDebug.Location = new System.Drawing.Point(209, 51);
		this.lblDebug.Name = "lblDebug";
		this.lblDebug.Size = new System.Drawing.Size(89, 13);
		this.lblDebug.TabIndex = 4;
		this.lblDebug.Text = "Debug Log Level";
		this.lblTitle.AutoSize = true;
		this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblTitle.Location = new System.Drawing.Point(13, 13);
		this.lblTitle.Name = "lblTitle";
		this.lblTitle.Size = new System.Drawing.Size(138, 24);
		this.lblTitle.TabIndex = 5;
		this.lblTitle.Text = "Debug Settings";
		this.btnSendLog.Location = new System.Drawing.Point(17, 137);
		this.btnSendLog.Name = "btnSendLog";
		this.btnSendLog.Size = new System.Drawing.Size(147, 37);
		this.btnSendLog.TabIndex = 6;
		this.btnSendLog.Text = "Send Log Now";
		this.btnSendLog.UseVisualStyleBackColor = true;
		this.btnSendLog.Click += new System.EventHandler(btnSendLog_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(383, 209);
		base.Controls.Add(this.btnSendLog);
		base.Controls.Add(this.lblTitle);
		base.Controls.Add(this.lblDebug);
		base.Controls.Add(this.cmdSave);
		base.Controls.Add(this.cbLogLevel);
		base.Controls.Add(this.cmdOpenFolder);
		base.Controls.Add(this.cmdOpenLog);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "frmDebug";
		this.Text = "Debug Settings";
		base.Load += new System.EventHandler(frmDebug_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
