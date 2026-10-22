namespace AirFader;

public class GEQ
{
	public delegate void GEQEventHandler(string function, int value);

	public int _type;

	public int BandsAvail = 31;

	public int _link;

	public int _linkLR;

	public int _on = 1;

	public int Rack;

	public int[] _Band = new int[31];

	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			MixEngine.OnAction("RackType", Rack);
		}
	}

	public event GEQEventHandler Changed;

	public GEQ(int rack)
	{
		Rack = rack;
		for (int i = 0; i < 31; i++)
		{
			_Band[i] = 0;
		}
	}

	public int getBand(int band)
	{
		if (band >= 0 && band < _Band.Length)
		{
			return _Band[band];
		}
		return 0;
	}

	public void console_setBand(int band, int value)
	{
		if (band >= 0 && band < _Band.Length)
		{
			_Band[band] = value;
			OnChange("GEQBand", band);
			if (_link != 0 && MixEngine.GEQData[Rack - Rack % 2 * 2 + 1].getBand(band) != value)
			{
				MixEngine.GEQData[Rack - Rack % 2 * 2 + 1].console_setBand(band, value);
			}
			if (_linkLR != 0 && MixEngine.GEQData[Rack + 8 - Rack / 8 * 16].getBand(band) != value)
			{
				MixEngine.GEQData[Rack + 8 - Rack / 8 * 16].console_setBand(band, value);
			}
		}
	}

	protected void OnChange(string function, int value)
	{
		if (this.Changed != null)
		{
			this.Changed(function, value);
		}
	}

	public void Notify()
	{
		OnChange("Unknown", 0);
	}
}
