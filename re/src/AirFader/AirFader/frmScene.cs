using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;

namespace AirFader;

public class frmScene : Form
{
	private AirButton btnLoad;

	private AirButton btnCancel;

	private AirButton btnUp5;

	private AirButton btnUp;

	private AirButton btnDown;

	private AirButton btnDown5;

	private Label lblSceneMode;

	private string activeMode = "";

	private WindowStatic thisWin = new WindowStatic();

	private IContainer components;

	private DataGridView gridScene;

	private DataGridViewTextBoxColumn sceneActive;

	private DataGridViewTextBoxColumn colSceneNum;

	private DataGridViewTextBoxColumn colSceneName;

	private DataGridViewTextBoxColumn colSceneComment;

	public frmScene()
	{
		InitializeComponent();
		gridScene.Left = 0;
		gridScene.Top = 0;
		gridScene.Width = base.ClientRectangle.Width - 90;
		gridScene.Height = base.ClientRectangle.Height - 10;
		BackColor = Settings.Default.c_background;
		lblSceneMode = new Label();
		lblSceneMode.Size = new Size(200, 30);
		lblSceneMode.Top = 20;
		lblSceneMode.Left = 10;
		lblSceneMode.Text = "Select Scene to Load...";
		lblSceneMode.TextAlign = ContentAlignment.MiddleCenter;
		lblSceneMode.ForeColor = Settings.Default.c_foreground;
		base.Controls.Add(lblSceneMode);
		btnLoad = new AirButton();
		btnCancel = new AirButton();
		btnLoad.Size = new Size(70, 55);
		btnLoad.Top = 20;
		btnLoad.Left = gridScene.Right + 10;
		btnLoad.Text = "Load";
		btnLoad.ParentColor = BackColor;
		btnLoad.BackColor = Settings.Default.c_button_inactive;
		btnLoad.ForeColor = Settings.Default.c_button_inactive_fore;
		btnLoad.Click += btnLoad_Click;
		base.Controls.Add(btnLoad);
		btnCancel.Size = new Size(70, 55);
		btnCancel.Top = btnLoad.Bottom + 10;
		btnCancel.Left = gridScene.Right + 10;
		btnCancel.Text = "Cancel";
		btnCancel.ParentColor = BackColor;
		btnCancel.BackColor = Settings.Default.c_button_inactive;
		btnCancel.ForeColor = Settings.Default.c_button_inactive_fore;
		btnCancel.Click += btnCancel_Click;
		base.Controls.Add(btnCancel);
		btnDown5 = new AirButton();
		btnDown5.Size = new Size(70, 55);
		btnDown5.Top = gridScene.Bottom - btnDown5.Height;
		btnDown5.Left = gridScene.Right + 10;
		btnDown5.Text = "Page\nDown";
		btnDown5.Tag = 5;
		btnDown5.ParentColor = BackColor;
		btnDown5.BackColor = Settings.Default.c_button_inactive;
		btnDown5.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDown5.Click += btnArrow_Click;
		base.Controls.Add(btnDown5);
		btnDown = new AirButton();
		btnDown.Size = new Size(70, 55);
		btnDown.Top = btnDown5.Top - btnDown.Height - 10;
		btnDown.Left = gridScene.Right + 10;
		btnDown.Text = "Down";
		btnDown.Tag = 1;
		btnDown.ParentColor = BackColor;
		btnDown.BackColor = Settings.Default.c_button_inactive;
		btnDown.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDown.Click += btnArrow_Click;
		base.Controls.Add(btnDown);
		btnUp = new AirButton();
		btnUp.Size = new Size(70, 55);
		btnUp.Top = btnDown.Top - btnUp.Height - 10;
		btnUp.Left = gridScene.Right + 10;
		btnUp.Text = "Up";
		btnUp.Tag = -1;
		btnUp.ParentColor = BackColor;
		btnUp.BackColor = Settings.Default.c_button_inactive;
		btnUp.ForeColor = Settings.Default.c_button_inactive_fore;
		btnUp.Click += btnArrow_Click;
		base.Controls.Add(btnUp);
		btnUp5 = new AirButton();
		btnUp5.Size = new Size(70, 55);
		btnUp5.Top = btnUp.Top - btnUp5.Height - 10;
		btnUp5.Left = gridScene.Right + 10;
		btnUp5.Text = "Page\nUp";
		btnUp5.Tag = -5;
		btnUp5.ParentColor = BackColor;
		btnUp5.BackColor = Settings.Default.c_button_inactive;
		btnUp5.ForeColor = Settings.Default.c_button_inactive_fore;
		btnUp5.Click += btnArrow_Click;
		base.Controls.Add(btnUp5);
		thisWin.Parse(Settings.Default.windowScene);
		thisWin.SetWindow(this);
	}

	public void setMode(string mode)
	{
		switch (mode)
		{
		case "SaveScene":
			btnLoad.Text = "Save";
			activeMode = "SaveScene";
			lblSceneMode.Text = "Select Location to Save Scene...";
			break;
		case "LoadScene":
			btnLoad.Text = "Load";
			activeMode = "LoadScene";
			lblSceneMode.Text = "Select Scene to Load...";
			break;
		}
	}

	private void btnArrow_Click(object sender, EventArgs e)
	{
		int num = 0;
		if (gridScene.SelectedRows.Count == 1)
		{
			num = gridScene.SelectedRows[0].Index;
		}
		num += int.Parse(((AirButton)sender).Tag.ToString());
		if (num < 0)
		{
			num = 0;
		}
		if (num >= gridScene.Rows.Count)
		{
			num = gridScene.Rows.Count - 1;
		}
		gridScene.Rows[num].Selected = true;
		if (gridScene.SelectedRows.Count == 1)
		{
			if (gridScene.SelectedRows[0].Index <= 5)
			{
				gridScene.FirstDisplayedScrollingRowIndex = 0;
			}
			else
			{
				gridScene.FirstDisplayedScrollingRowIndex = gridScene.SelectedRows[0].Index - 5;
			}
		}
	}

	public void refreshGrid()
	{
		gridScene.Invalidate();
	}

	private void frmScene_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		DataTable dataTable = new DataTable();
		dataTable.Columns.Add("sceneNumText", typeof(string));
		dataTable.Columns.Add("sceneName", typeof(string));
		dataTable.Columns.Add("sceneComment", typeof(string));
		dataTable.Columns.Add("sceneActive", typeof(string));
		Scene[] sceneData = MixEngine.sceneData;
		foreach (Scene scene in sceneData)
		{
			if (scene.sceneNum == 0)
			{
				dataTable.LoadDataRow(new string[4] { "0", "Initial Data", "", "Y" }, LoadOption.OverwriteChanges);
			}
			else
			{
				dataTable.LoadDataRow(new string[4]
				{
					scene.sceneNumText,
					scene.sceneName,
					scene.sceneComment,
					scene.isValid ? "Y" : "N"
				}, LoadOption.OverwriteChanges);
			}
		}
		gridScene.DataSource = dataTable;
		try
		{
			foreach (DataGridViewRow item in (IEnumerable)gridScene.Rows)
			{
				if ((string)item.Cells["sceneActive"].Value == "Y")
				{
					item.DefaultCellStyle.BackColor = Color.DarkGreen;
				}
				else
				{
					item.DefaultCellStyle.BackColor = Color.DimGray;
				}
			}
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("** Error Loading Scene List ** " + ex.Message);
		}
		gridScene.Rows[Console.sceneNum].Selected = true;
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void btnLoad_Click(object sender, EventArgs e)
	{
		try
		{
			if (gridScene.SelectedRows.Count == 1)
			{
				int num = int.Parse(gridScene.SelectedRows[0].Cells["colSceneNum"].Value.ToString());
				switch (activeMode)
				{
				case "LoadScene":
					MixEngine.setParam("RecallScene", num, 0);
					Close();
					break;
				case "SaveScene":
				{
					frmKeyboard frmKeyboard2 = new frmKeyboard();
					frmKeyboard2.setProperty("SaveScene", num);
					frmKeyboard2.Show();
					Close();
					break;
				}
				}
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("GridScene::btnLoad_Click", "UI", ex.ToString());
		}
	}

	private void gridScene_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		try
		{
			if (gridScene.SelectedRows.Count == 1)
			{
				int num = int.Parse(gridScene.Rows[e.RowIndex].Cells["colSceneNum"].Value.ToString());
				switch (activeMode)
				{
				case "LoadScene":
					MixEngine.setParam("RecallScene", num, 0);
					Close();
					break;
				case "SaveScene":
				{
					frmKeyboard frmKeyboard2 = new frmKeyboard();
					frmKeyboard2.setProperty("SaveScene", num);
					frmKeyboard2.Show();
					Close();
					break;
				}
				}
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("GridScene_CellDoubleClick", "UI", ex.ToString());
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

	private void frmScene_Resize(object sender, EventArgs e)
	{
		gridScene.Top = 40;
		gridScene.Left = 0;
		gridScene.Width = base.ClientRectangle.Width - 90;
		gridScene.Height = base.ClientRectangle.Height - 45;
		lblSceneMode.Top = 10;
		lblSceneMode.Height = 25;
		lblSceneMode.Left = 0;
		lblSceneMode.Width = gridScene.Width;
		lblSceneMode.Font = FontFit("Arial", FontStyle.Bold, lblSceneMode.Text, lblSceneMode.Width, lblSceneMode.Height, 20f);
		Font font = FontFit("Arial", FontStyle.Bold, "Cancel", btnLoad.Width - 4, btnLoad.Height - 4, 20f);
		btnLoad.Font = font;
		btnLoad.Left = gridScene.Right + 10;
		btnCancel.Font = font;
		btnCancel.Left = gridScene.Right + 10;
		btnDown5.Font = font;
		btnDown5.Top = gridScene.Bottom - btnDown5.Height;
		btnDown5.Left = gridScene.Right + 10;
		btnDown.Font = font;
		btnDown.Top = btnDown5.Top - btnDown.Height - 10;
		btnDown.Left = gridScene.Right + 10;
		btnUp.Font = font;
		btnUp.Top = btnDown.Top - btnUp.Height - 10;
		btnUp.Left = gridScene.Right + 10;
		btnUp5.Font = font;
		btnUp5.Top = btnUp.Top - btnUp5.Height - 10;
		btnUp5.Left = gridScene.Right + 10;
		if (base.WindowState == FormWindowState.Normal && base.FormBorderStyle == FormBorderStyle.Sizable)
		{
			thisWin.WindowSize = base.Size;
		}
	}

	private void gridScene_SelectionChanged(object sender, EventArgs e)
	{
	}

	private void frmScene_LocationChanged(object sender, EventArgs e)
	{
		if (base.WindowState == FormWindowState.Normal && base.FormBorderStyle == FormBorderStyle.Sizable)
		{
			thisWin.WindowPos = base.Location;
		}
	}

	private void frmScene_Deactivate(object sender, EventArgs e)
	{
		thisWin.WindowState = ((base.FormBorderStyle == FormBorderStyle.None) ? "Fullscreen" : base.WindowState.ToString());
		Settings.Default.windowScene = WindowStatic.Store(thisWin);
		Settings.Default.Save();
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
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmScene));
		this.gridScene = new System.Windows.Forms.DataGridView();
		this.sceneActive = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.colSceneNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.colSceneName = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.colSceneComment = new System.Windows.Forms.DataGridViewTextBoxColumn();
		((System.ComponentModel.ISupportInitialize)this.gridScene).BeginInit();
		base.SuspendLayout();
		this.gridScene.AllowUserToAddRows = false;
		this.gridScene.AllowUserToDeleteRows = false;
		this.gridScene.AllowUserToOrderColumns = true;
		this.gridScene.AllowUserToResizeColumns = false;
		this.gridScene.AllowUserToResizeRows = false;
		this.gridScene.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
		this.gridScene.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
		dataGridViewCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle.BackColor = System.Drawing.Color.Black;
		dataGridViewCellStyle.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		dataGridViewCellStyle.ForeColor = System.Drawing.Color.Lime;
		dataGridViewCellStyle.SelectionBackColor = System.Drawing.Color.Black;
		dataGridViewCellStyle.SelectionForeColor = System.Drawing.Color.Lime;
		dataGridViewCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.gridScene.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle;
		this.gridScene.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.gridScene.Columns.AddRange(this.sceneActive, this.colSceneNum, this.colSceneName, this.colSceneComment);
		dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		dataGridViewCellStyle2.BackColor = System.Drawing.Color.DimGray;
		dataGridViewCellStyle2.Font = new System.Drawing.Font("Arial", 15.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
		dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
		this.gridScene.DefaultCellStyle = dataGridViewCellStyle2;
		this.gridScene.GridColor = System.Drawing.Color.Silver;
		this.gridScene.Location = new System.Drawing.Point(12, 12);
		this.gridScene.MultiSelect = false;
		this.gridScene.Name = "gridScene";
		this.gridScene.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
		dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		dataGridViewCellStyle3.BackColor = System.Drawing.Color.RoyalBlue;
		dataGridViewCellStyle3.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
		dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.RoyalBlue;
		dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
		dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.gridScene.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
		this.gridScene.RowHeadersVisible = false;
		this.gridScene.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		this.gridScene.Size = new System.Drawing.Size(331, 247);
		this.gridScene.TabIndex = 1;
		this.gridScene.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(gridScene_CellDoubleClick);
		this.gridScene.SelectionChanged += new System.EventHandler(gridScene_SelectionChanged);
		this.sceneActive.DataPropertyName = "sceneActive";
		this.sceneActive.FillWeight = 14.59391f;
		this.sceneActive.HeaderText = "Active";
		this.sceneActive.Name = "sceneActive";
		this.sceneActive.ReadOnly = true;
		this.sceneActive.Resizable = System.Windows.Forms.DataGridViewTriState.True;
		this.sceneActive.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.sceneActive.Visible = false;
		this.colSceneNum.DataPropertyName = "sceneNumText";
		dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		this.colSceneNum.DefaultCellStyle = dataGridViewCellStyle4;
		this.colSceneNum.FillWeight = 15.06091f;
		this.colSceneNum.HeaderText = " #";
		this.colSceneNum.Name = "colSceneNum";
		this.colSceneNum.ReadOnly = true;
		this.colSceneName.DataPropertyName = "sceneName";
		this.colSceneName.FillWeight = 40.16244f;
		this.colSceneName.HeaderText = "Title";
		this.colSceneName.Name = "colSceneName";
		this.colSceneName.ReadOnly = true;
		this.colSceneComment.DataPropertyName = "sceneComment";
		this.colSceneComment.FillWeight = 45.18274f;
		this.colSceneComment.HeaderText = "Comment";
		this.colSceneComment.Name = "colSceneComment";
		this.colSceneComment.ReadOnly = true;
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
		base.ClientSize = new System.Drawing.Size(696, 455);
		base.Controls.Add(this.gridScene);
		this.DoubleBuffered = true;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "frmScene";
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "Scene List";
		base.Deactivate += new System.EventHandler(frmScene_Deactivate);
		base.Load += new System.EventHandler(frmScene_Load);
		base.Resize += new System.EventHandler(frmScene_Resize);
		base.LocationChanged += new System.EventHandler(frmScene_LocationChanged);
		((System.ComponentModel.ISupportInitialize)this.gridScene).EndInit();
		base.ResumeLayout(false);
	}
}
