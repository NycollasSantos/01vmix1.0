using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AirControls;

public class AirButton : Button
{
	public class ExtendedGraphics
	{
		private Graphics mGraphics;

		public Graphics Graphics
		{
			get
			{
				return mGraphics;
			}
			set
			{
				mGraphics = value;
			}
		}

		public ExtendedGraphics(Graphics graphics)
		{
			Graphics = graphics;
		}

		public void FillRoundRectangle(Brush brush, int x, int y, int width, int height, int radius)
		{
			float x2 = Convert.ToSingle(x);
			float y2 = Convert.ToSingle(y);
			float width2 = Convert.ToSingle(width);
			float height2 = Convert.ToSingle(height);
			float radius2 = Convert.ToSingle(radius);
			FillRoundRectangle(brush, x2, y2, width2, height2, radius2);
		}

		public void FillRoundRectangle(Brush brush, float x, float y, float width, float height, float radius)
		{
			RectangleF baseRect = new RectangleF(x, y, width, height);
			GraphicsPath roundedRect = GetRoundedRect(baseRect, radius);
			Graphics.FillPath(brush, roundedRect);
		}

		public void DrawRoundRectangle(Pen pen, int x, int y, int width, int height, int radius)
		{
			float x2 = Convert.ToSingle(x);
			float y2 = Convert.ToSingle(y);
			float width2 = Convert.ToSingle(width);
			float height2 = Convert.ToSingle(height);
			float radius2 = Convert.ToSingle(radius);
			DrawRoundRectangle(pen, x2, y2, width2, height2, radius2);
		}

		public void DrawRoundRectangle(Pen pen, float x, float y, float width, float height, float radius)
		{
			RectangleF baseRect = new RectangleF(x, y, width, height);
			GraphicsPath roundedRect = GetRoundedRect(baseRect, radius);
			Graphics.DrawPath(pen, roundedRect);
		}

		public GraphicsPath GetRoundedRect(RectangleF baseRect, float radius)
		{
			if (radius <= 0f)
			{
				GraphicsPath graphicsPath = new GraphicsPath();
				graphicsPath.AddRectangle(baseRect);
				graphicsPath.CloseFigure();
				return graphicsPath;
			}
			if ((double)radius >= (double)Math.Min(baseRect.Width, baseRect.Height) / 2.0)
			{
				return GetCapsule(baseRect);
			}
			float num = radius * 2f;
			RectangleF rect = new RectangleF(size: new SizeF(num, num), location: baseRect.Location);
			GraphicsPath graphicsPath2 = new GraphicsPath();
			graphicsPath2.AddArc(rect, 180f, 90f);
			rect.X = baseRect.Right - num;
			graphicsPath2.AddArc(rect, 270f, 90f);
			rect.Y = baseRect.Bottom - num;
			graphicsPath2.AddArc(rect, 0f, 90f);
			rect.X = baseRect.Left;
			graphicsPath2.AddArc(rect, 90f, 90f);
			graphicsPath2.CloseFigure();
			return graphicsPath2;
		}

		private GraphicsPath GetCapsule(RectangleF baseRect)
		{
			GraphicsPath graphicsPath = new GraphicsPath();
			try
			{
				if (baseRect.Width > baseRect.Height)
				{
					float height = baseRect.Height;
					RectangleF rect = new RectangleF(size: new SizeF(height, height), location: baseRect.Location);
					graphicsPath.AddArc(rect, 90f, 180f);
					rect.X = baseRect.Right - height;
					graphicsPath.AddArc(rect, 270f, 180f);
				}
				else if (baseRect.Width < baseRect.Height)
				{
					float height = baseRect.Width;
					RectangleF rect = new RectangleF(size: new SizeF(height, height), location: baseRect.Location);
					graphicsPath.AddArc(rect, 180f, 180f);
					rect.Y = baseRect.Bottom - height;
					graphicsPath.AddArc(rect, 0f, 180f);
				}
				else
				{
					graphicsPath.AddEllipse(baseRect);
				}
			}
			catch (Exception)
			{
				graphicsPath.AddEllipse(baseRect);
			}
			finally
			{
				graphicsPath.CloseFigure();
			}
			return graphicsPath;
		}
	}

	private Color _borderColor = Color.Black;

	private Color _parentColor = Color.Gray;

	private Color _backColor = Color.Gray;

	private Color _LED = Color.Gray;

	private bool _highlight;

	private Pen _borderPen;

	private Brush _fillBrush;

	private Brush _fillClickBrush;

	private Brush _LEDBrush;

	private Brush _textBrush;

	public Font SubFont;

	private string _backText = "";

	private bool _mouseDown;

	public string Flag = "";

	private StringFormat formatCenter;

	private IContainer components;

	public Color BorderColor
	{
		get
		{
			return _borderColor;
		}
		set
		{
			_borderColor = value;
			Invalidate();
		}
	}

	public override Color BackColor
	{
		get
		{
			return _backColor;
		}
		set
		{
			_LED = value;
			_backColor = value;
			if (base.ClientRectangle.Width > 0 && base.ClientRectangle.Height > 0)
			{
				_fillBrush = new LinearGradientBrush(base.ClientRectangle, _backColor, ControlPaint.Dark(_backColor, 0.1f), 45f);
				_fillClickBrush = new LinearGradientBrush(base.ClientRectangle, ControlPaint.Dark(_backColor, 0.05f), ControlPaint.Dark(_backColor, 0.15f), 45f);
				_LEDBrush = new SolidBrush(_LED);
				Invalidate();
			}
		}
	}

	public override Color ForeColor
	{
		get
		{
			return base.ForeColor;
		}
		set
		{
			base.ForeColor = value;
			_textBrush = new SolidBrush(base.ForeColor);
		}
	}

	public Color LEDColor
	{
		get
		{
			return _LED;
		}
		set
		{
			_LED = value;
			_LEDBrush = new SolidBrush(_LED);
			Invalidate();
		}
	}

	public override Font Font
	{
		get
		{
			return base.Font;
		}
		set
		{
			base.Font = value;
		}
	}

	public string BackText
	{
		get
		{
			return _backText;
		}
		set
		{
			_backText = value;
			Invalidate();
		}
	}

	public Color ParentColor
	{
		get
		{
			return _parentColor;
		}
		set
		{
			_parentColor = value;
			Invalidate();
		}
	}

	public override string Text
	{
		get
		{
			return base.Text;
		}
		set
		{
			base.Text = value;
		}
	}

	public AirButton()
	{
		DoubleBuffered = true;
		SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		UpdateStyles();
		formatCenter = new StringFormat();
		formatCenter.Alignment = StringAlignment.Center;
		formatCenter.LineAlignment = StringAlignment.Near;
		InitializeComponent();
		_borderPen = new Pen(_borderColor, 2f);
		if (base.ClientRectangle.Width > 0 && base.ClientRectangle.Height > 0)
		{
			_fillBrush = new LinearGradientBrush(base.ClientRectangle, _backColor, ControlPaint.Dark(_backColor, 0.1f), 45f);
			_fillClickBrush = new LinearGradientBrush(base.ClientRectangle, ControlPaint.Dark(_backColor, 0.05f), ControlPaint.Dark(_backColor, 0.15f), 45f);
			_LEDBrush = new SolidBrush(_LED);
			_textBrush = new SolidBrush(ForeColor);
			if (SubFont == null)
			{
				SubFont = new Font(Font.FontFamily, Font.Size * 0.75f, Font.Style);
			}
		}
	}

	private Font FontFit(string fontName, FontStyle fontStyle, string thisText, int thisWide, int thisHigh, float maxSize)
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

	private GraphicsPath RoundRectangle(Graphics objG, Brush fillBrush, Pen objP, float h, float v, float width, float height, float radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(h + width - radius * 2f, v, radius * 2f, radius * 2f, 270f, 90f);
		graphicsPath.AddArc(h + width - radius * 2f, v + height - radius * 2f, radius * 2f, radius * 2f, 0f, 90f);
		graphicsPath.AddArc(h, v + height - radius * 2f, radius * 2f, radius * 2f, 90f, 90f);
		graphicsPath.AddArc(h, v, radius * 2f, radius * 2f, 180f, 90f);
		objG.FillPath(fillBrush, graphicsPath);
		objG.DrawPath(objP, graphicsPath);
		return graphicsPath;
	}

	protected override void OnResize(EventArgs e)
	{
		if (base.ClientRectangle.Width != 0 && base.ClientRectangle.Height != 0)
		{
			_fillBrush = new LinearGradientBrush(base.ClientRectangle, _backColor, ControlPaint.Dark(_backColor, 0.1f), 45f);
			_fillClickBrush = new LinearGradientBrush(base.ClientRectangle, ControlPaint.Dark(_backColor, 0.05f), ControlPaint.Dark(_backColor, 0.15f), 45f);
			_LEDBrush = new SolidBrush(_LED);
			Invalidate();
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnMouseDown(MouseEventArgs mevent)
	{
		if (mevent.Button == MouseButtons.Left)
		{
			_mouseDown = true;
		}
		Invalidate();
		base.OnMouseDown(mevent);
	}

	protected override void OnMouseUp(MouseEventArgs mevent)
	{
		_mouseDown = false;
		Invalidate();
		base.OnMouseUp(mevent);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (base.ClientRectangle.Width != 0 && base.ClientRectangle.Height != 0)
		{
			ExtendedGraphics extendedGraphics = new ExtendedGraphics(e.Graphics);
			e.Graphics.Clear(_parentColor);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			if (_mouseDown)
			{
				extendedGraphics.FillRoundRectangle(_fillClickBrush, 0, 0, base.Width - 2, base.Height - 2, 12);
				extendedGraphics.DrawRoundRectangle(_borderPen, 0, 0, base.Width - 2, base.Height - 2, 12);
			}
			else
			{
				extendedGraphics.FillRoundRectangle(_fillBrush, 0, 0, base.Width - 2, base.Height - 2, 12);
				extendedGraphics.DrawRoundRectangle(_borderPen, 0, 0, base.Width - 2, base.Height - 2, 12);
			}
			base.Region = new Region(extendedGraphics.GetRoundedRect(new Rectangle(-1, -1, base.Width + 1, base.Height + 1), 10f));
			float num = 0f;
			float num2 = 0f;
			if (BackText.Length > 0)
			{
				num = e.Graphics.MeasureString(Text, Font, base.Width).Height * 0.95f;
				num2 = e.Graphics.MeasureString(BackText, SubFont, base.Width).Height * 0.95f;
				e.Graphics.DrawString(Text, Font, _textBrush, base.Width / 2, (float)base.Height / 2f - (num + num2) / 2f - 1f, formatCenter);
				e.Graphics.DrawString(BackText, SubFont, _textBrush, base.Width / 2, (float)base.Height / 2f - (num + num2) / 2f + num - 2f, formatCenter);
			}
			else
			{
				num = e.Graphics.MeasureString(Text, Font, base.Width).Height * 0.95f;
				e.Graphics.DrawString(Text, Font, _textBrush, (float)base.Width / 2f, (float)base.Height / 2f - num / 2f - 0f, formatCenter);
			}
			if (_LED != _backColor)
			{
				e.Graphics.FillEllipse(_LEDBrush, (float)base.Width * 0.4f, (float)base.Height * 0.075f, (float)base.Width * 0.2f, (float)base.Height * 0.15f);
			}
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
		base.SuspendLayout();
		this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		base.Name = "AirMeter";
		base.Size = new System.Drawing.Size(90, 88);
		base.ResumeLayout(false);
	}
}
