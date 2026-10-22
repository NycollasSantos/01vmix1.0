using System.Drawing;

namespace AirFader;

internal class Skin
{
	public Color btnActiveFG;

	public Color btnActiveBG;

	public Color btnInactiveFG;

	public Color btnInactiveBG;

	public Skin()
	{
		btnActiveFG = Color.Black;
		btnActiveBG = Color.LightGreen;
		btnInactiveFG = Color.Black;
		btnInactiveBG = Color.LightGray;
	}
}
