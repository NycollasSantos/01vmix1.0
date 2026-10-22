using System.Drawing.Text;
using System.Windows.Forms;

namespace AirControls;

public class AirLabel : Label
{
	private TextRenderingHint _hint;

	public TextRenderingHint TextRenderingHint
	{
		get
		{
			return _hint;
		}
		set
		{
			_hint = value;
		}
	}

	protected override void OnPaint(PaintEventArgs pe)
	{
		pe.Graphics.TextRenderingHint = TextRenderingHint;
		base.OnPaint(pe);
	}
}
