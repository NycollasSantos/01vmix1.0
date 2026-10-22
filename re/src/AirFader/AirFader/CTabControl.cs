using System;
using System.Drawing;
using System.Windows.Forms;
using AirFader.Properties;

namespace AirFader;

public class CTabControl : TabControl
{
	public CTabControl()
	{
		base.DrawMode = TabDrawMode.OwnerDrawFixed;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		base.DrawItem += RepaintControls;
		Invalidate();
	}

	private Font FontFit(Font thisFont, string thisText, int thisWide, float maxSize)
	{
		Graphics graphics = CreateGraphics();
		float num = 0f;
		while (maxSize > 4f)
		{
			Font font = new Font(thisFont.FontFamily, maxSize, thisFont.Style);
			num = graphics.MeasureString(thisText, font).Width;
			if (num <= (float)thisWide)
			{
				return font;
			}
			maxSize -= 0.5f;
		}
		return new Font(thisFont.FontFamily, 4f, thisFont.Style);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		Invalidate();
	}

	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
		pevent.Graphics.Clear(Settings.Default.c_background);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (base.SelectedIndex == -1)
		{
			return;
		}
		Font = FontFit(Font, "Sends", GetTabRect(0).Width - 4, 16f);
		StringFormat stringFormat = new StringFormat();
		stringFormat.Alignment = StringAlignment.Center;
		stringFormat.LineAlignment = StringAlignment.Center;
		Pen pen = new Pen(Brushes.LightGray, 3f);
		int i;
		Rectangle tabRect;
		Brush brush;
		Brush brush2;
		for (i = 0; i < base.TabCount; i++)
		{
			tabRect = GetTabRect(i);
			if (i != base.SelectedIndex)
			{
				tabRect.Y += 3;
				tabRect.Height -= 3;
				if ((double)Settings.Default.c_background.GetBrightness() < 0.5)
				{
					brush = new SolidBrush(RGBHSL.ModifyBrightness(Settings.Default.c_background, 1.6));
					pen.Brush = new SolidBrush(RGBHSL.ModifyBrightness(Settings.Default.c_background, 2.0));
					brush2 = new SolidBrush(Settings.Default.c_foreground);
				}
				else
				{
					brush = new SolidBrush(RGBHSL.ModifyBrightness(Settings.Default.c_background, 0.4));
					pen.Brush = new SolidBrush(RGBHSL.ModifyBrightness(Settings.Default.c_background, 0.2));
					brush2 = new SolidBrush(Settings.Default.c_foreground);
				}
				_ = base.TabPages[i].Text;
				e.Graphics.FillRectangle(brush, tabRect);
				e.Graphics.DrawRectangle(pen, tabRect);
				e.Graphics.DrawString(base.TabPages[i].Text, Font, Brushes.WhiteSmoke, GetTabRect(i), stringFormat);
			}
		}
		Rectangle clientRectangle = base.TabPages[0].ClientRectangle;
		clientRectangle.Y += GetTabRect(0).Height + 2;
		clientRectangle.X++;
		clientRectangle.Height += 4;
		clientRectangle.Width += 4;
		i = base.SelectedIndex;
		tabRect = GetTabRect(i);
		brush = new SolidBrush(Settings.Default.c_background);
		brush2 = new SolidBrush(Settings.Default.c_foreground);
		_ = base.TabPages[i].Text;
		e.Graphics.FillRectangle(brush, tabRect);
		pen.Color = Color.LightGray;
		e.Graphics.DrawRectangle(pen, tabRect);
		pen.Color = Settings.Default.c_background;
		e.Graphics.DrawLine(pen, tabRect.X + 2, tabRect.Y + tabRect.Height, tabRect.X + tabRect.Width - 1, tabRect.Y + tabRect.Height);
		e.Graphics.DrawString(base.TabPages[i].Text, Font, brush2, GetTabRect(i), stringFormat);
		base.OnPaint(e);
	}

	private void RepaintControls(object sender, DrawItemEventArgs e)
	{
		Rectangle rectangle = e.Bounds;
		base.TabPages[e.Index].BackColor = Settings.Default.c_background;
		Font font;
		Brush brush;
		Brush brush2;
		if (e.Index == base.SelectedIndex)
		{
			font = new Font(e.Font, e.Font.Style);
			brush = new SolidBrush(Color.DimGray);
			brush2 = new SolidBrush(Settings.Default.c_foreground);
			rectangle = new Rectangle(rectangle.X + base.Padding.X / 2, rectangle.Y + base.Padding.Y, rectangle.Width - base.Padding.X, rectangle.Height - base.Padding.Y * 2);
		}
		else
		{
			font = new Font(e.Font, e.Font.Style & ~FontStyle.Bold);
			brush = new SolidBrush(base.TabPages[e.Index].BackColor);
			brush2 = new SolidBrush(base.TabPages[e.Index].ForeColor);
		}
		string s = base.TabPages[e.Index].Text;
		StringFormat stringFormat = new StringFormat();
		stringFormat.Alignment = StringAlignment.Center;
		stringFormat.LineAlignment = StringAlignment.Center;
		e.Graphics.FillRectangle(brush, rectangle);
		e.Graphics.DrawString(s, font, brush2, rectangle, stringFormat);
		Brush brush3 = new SolidBrush(Color.DodgerBlue);
		Rectangle tabRect = GetTabRect(base.TabPages.Count - 1);
		Rectangle rect = default(Rectangle);
		rect.Location = new Point(tabRect.Right + base.Left, base.Top);
		rect.Size = new Size(base.Right - rect.Left, tabRect.Height);
		e.Graphics.FillRectangle(brush3, rect);
		brush3.Dispose();
		stringFormat.Dispose();
		brush.Dispose();
		brush2.Dispose();
		font.Dispose();
	}
}
