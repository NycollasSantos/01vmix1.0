using System.ComponentModel;
using System.Windows.Forms;

namespace AirFader;

public class TabPanel : UserControl
{
	private IContainer components;

	public TabPanel()
	{
		InitializeComponent();
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
		this.components = new System.ComponentModel.Container();
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
	}
}
