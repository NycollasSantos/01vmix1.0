using System.Drawing;

namespace AirFader;

public class RGBHSL
{
	public class HSL
	{
		private double _h;

		private double _s;

		private double _l;

		public double H
		{
			get
			{
				return _h;
			}
			set
			{
				_h = value;
				_h = ((_h > 1.0) ? 1.0 : ((_h < 0.0) ? 0.0 : _h));
			}
		}

		public double S
		{
			get
			{
				return _s;
			}
			set
			{
				_s = value;
				_s = ((_s > 1.0) ? 1.0 : ((_s < 0.0) ? 0.0 : _s));
			}
		}

		public double L
		{
			get
			{
				return _l;
			}
			set
			{
				_l = value;
				_l = ((_l > 1.0) ? 1.0 : ((_l < 0.0) ? 0.0 : _l));
			}
		}

		public HSL()
		{
			_h = 0.0;
			_s = 0.0;
			_l = 0.0;
		}
	}

	public static Color SetBrightness(Color c, double brightness)
	{
		HSL hSL = RGB_to_HSL(c);
		hSL.L = brightness;
		return HSL_to_RGB(hSL);
	}

	public static Color ModifyBrightness(Color c, double brightness)
	{
		HSL hSL = RGB_to_HSL(c);
		hSL.L *= brightness;
		return HSL_to_RGB(hSL);
	}

	public static Color SetSaturation(Color c, double Saturation)
	{
		HSL hSL = RGB_to_HSL(c);
		hSL.S = Saturation;
		return HSL_to_RGB(hSL);
	}

	public static Color ModifySaturation(Color c, double Saturation)
	{
		HSL hSL = RGB_to_HSL(c);
		hSL.S *= Saturation;
		return HSL_to_RGB(hSL);
	}

	public static Color SetHue(Color c, double Hue)
	{
		HSL hSL = RGB_to_HSL(c);
		hSL.H = Hue;
		return HSL_to_RGB(hSL);
	}

	public static Color ModifyHue(Color c, double Hue)
	{
		HSL hSL = RGB_to_HSL(c);
		hSL.H *= Hue;
		return HSL_to_RGB(hSL);
	}

	public static Color HSL_to_RGB(HSL hsl)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		if (hsl.L == 0.0)
		{
			num = (num2 = (num3 = 0.0));
		}
		else if (hsl.S == 0.0)
		{
			num = (num2 = (num3 = hsl.L));
		}
		else
		{
			double num4 = ((hsl.L <= 0.5) ? (hsl.L * (1.0 + hsl.S)) : (hsl.L + hsl.S - hsl.L * hsl.S));
			double num5 = 2.0 * hsl.L - num4;
			double[] array = new double[3]
			{
				hsl.H + 1.0 / 3.0,
				hsl.H,
				hsl.H - 1.0 / 3.0
			};
			double[] array2 = new double[3];
			double[] array3 = array2;
			for (int i = 0; i < 3; i++)
			{
				if (array[i] < 0.0)
				{
					array[i] += 1.0;
				}
				if (array[i] > 1.0)
				{
					array[i] -= 1.0;
				}
				if (6.0 * array[i] < 1.0)
				{
					array3[i] = num5 + (num4 - num5) * array[i] * 6.0;
				}
				else if (2.0 * array[i] < 1.0)
				{
					array3[i] = num4;
				}
				else if (3.0 * array[i] < 2.0)
				{
					array3[i] = num5 + (num4 - num5) * (2.0 / 3.0 - array[i]) * 6.0;
				}
				else
				{
					array3[i] = num5;
				}
			}
			num = array3[0];
			num2 = array3[1];
			num3 = array3[2];
		}
		return Color.FromArgb((int)(255.0 * num), (int)(255.0 * num2), (int)(255.0 * num3));
	}

	public static HSL RGB_to_HSL(Color c)
	{
		HSL hSL = new HSL();
		hSL.H = (double)c.GetHue() / 360.0;
		hSL.L = c.GetBrightness();
		hSL.S = c.GetSaturation();
		return hSL;
	}
}
