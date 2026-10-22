using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AirControls.Properties;

namespace AirControls;

public class AirPanel : Panel
{
	private Pen borderPen = new Pen(Color.Black);

	private Color _eqColor = Color.Lime;

	private int _panelType;

	private int _focusEQ;

	private int _activeEQ;

	private Point[] _eqPoints;

	private Point[] _eqDots;

	private Font fntArial = new Font("Arial", 10f);

	public Color BorderColor
	{
		get
		{
			return borderPen.Color;
		}
		set
		{
			borderPen.Color = value;
			Invalidate();
		}
	}

	public Color EQColor
	{
		get
		{
			return _eqColor;
		}
		set
		{
			_eqColor = value;
			Invalidate();
		}
	}

	public float BorderSize
	{
		get
		{
			return borderPen.Width;
		}
		set
		{
			borderPen.Width = value;
			Invalidate();
		}
	}

	public int PanelType
	{
		get
		{
			return _panelType;
		}
		set
		{
			_panelType = value;
		}
	}

	public void SetPoints(Point[] EQPoints, Point[] EQDots, int focusEQ, int activeEQ)
	{
		_eqPoints = EQPoints;
		_eqDots = EQDots;
		_focusEQ = focusEQ;
		_activeEQ = activeEQ;
		Invalidate();
	}

	public AirPanel()
	{
		DoubleBuffered = true;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		UpdateStyles();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		if (_panelType == 0)
		{
			e.Graphics.Clear(BackColor);
			e.Graphics.DrawImageUnscaled(Resources.screw_head, 2, 2);
			e.Graphics.DrawImageUnscaled(Resources.screw_head, base.Width - 20, 2);
			e.Graphics.DrawImageUnscaled(Resources.screw_head, 2, base.Height - 20);
			e.Graphics.DrawImageUnscaled(Resources.screw_head, base.Width - 20, base.Height - 20);
		}
		if (_panelType == 2)
		{
			e.Graphics.Clear(BackColor);
		}
	}

	private void AirPanel_Resize(object sender, EventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		_ = base.ClientRectangle;
		new Pen(Color.LightGray, 1f);
		if (_panelType == 1 && _eqPoints != null)
		{
			e.Graphics.DrawImage(Resources.eq_gif, 0, 0, base.Width, base.Height);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Brush brush = new SolidBrush(Color.FromArgb(200, _eqColor));
			e.Graphics.FillPolygon(brush, _eqPoints);
			for (int i = 0; i < 4; i++)
			{
				e.Graphics.FillRectangle((i == _activeEQ) ? Brushes.Yellow : ((i == _focusEQ) ? Brushes.Coral : Brushes.Blue), _eqDots[i].X - 2, _eqDots[i].Y - 2, 5, 5);
			}
		}
		if (_panelType == 2)
		{
			ControlPaint.DrawBorder(e.Graphics, base.ClientRectangle, borderPen.Color, ButtonBorderStyle.Solid);
		}
		else
		{
			ControlPaint.DrawBorder(e.Graphics, base.ClientRectangle, Color.FromArgb(105, 105, 105), 1, ButtonBorderStyle.Solid, Color.FromArgb(169, 169, 169), 1, ButtonBorderStyle.Solid, Color.FromArgb(75, 75, 75), 1, ButtonBorderStyle.Solid, Color.FromArgb(25, 25, 25), 1, ButtonBorderStyle.Solid);
		}
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.Name = "AirPanel";
		base.Size = new System.Drawing.Size(118, 123);
		base.ResumeLayout(false);
	}
}
