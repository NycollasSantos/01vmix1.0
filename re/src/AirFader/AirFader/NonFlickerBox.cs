using System.Windows.Forms;

namespace AirFader;

internal class NonFlickerBox : PictureBox
{
	public NonFlickerBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, value: true);
		UpdateStyles();
	}
}
