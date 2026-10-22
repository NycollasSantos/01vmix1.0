using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AirFader;

public class frmFancyBox : Form
{
	private IContainer components;

	private Label label2;

	private Button btnCancel;

	private Button btnRegister;

	public frmFancyBox()
	{
		InitializeComponent();
		Loc.Apply(this);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmFancyBox));
		this.label2 = new System.Windows.Forms.Label();
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnRegister = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(12, 9);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(435, 90);
		this.label2.TabIndex = 14;
		this.label2.Text = "01vmix does not need a license key.\r\n\r\nThis is the free, open-source build - all features are unlocked.";
		this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
		this.btnCancel.Location = new System.Drawing.Point(236, 102);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(102, 45);
		this.btnCancel.TabIndex = 12;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnRegister.Location = new System.Drawing.Point(128, 102);
		this.btnRegister.Name = "btnRegister";
		this.btnRegister.Size = new System.Drawing.Size(102, 45);
		this.btnRegister.TabIndex = 11;
		this.btnRegister.Text = "Ok";
		this.btnRegister.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(453, 156);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnRegister);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "frmFancyBox";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Info";
		base.TopMost = true;
		base.ResumeLayout(false);
	}
}
