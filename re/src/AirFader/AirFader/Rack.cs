namespace AirFader;

public class Rack
{
	public delegate void RackEventHandler(string function, int value);

	private string[] mapOutPatch = new string[185]
	{
		"NONE", "MIX1", "MIX2", "MIX3", "MIX4", "MIX5", "MIX6", "MIX7", "MIX8", "MIX9",
		"MIX10", "MIX11", "MIX12", "MIX13", "MIX14", "MIX15", "MIX16", "MATRIX1", "MATRIX2", "MATRIX3",
		"MATRIX4", "MATRIX5", "MATRIX6", "MATRIX7", "MATRIX8", "STL", "STR", "STC", "MONL", "MONR",
		"MONC", "DIRECT1", "DIRECT2", "DIRECT3", "DIRECT4", "DIRECT5", "DIRECT6", "DIRECT7", "DIRECT8", "DIRECT9",
		"DIRECT10", "DIRECT11", "DIRECT12", "DIRECT13", "DIRECT14", "DIRECT15", "DIRECT16", "DIRECT17", "DIRECT18", "DIRECT19",
		"DIRECT20", "DIRECT21", "DIRECT22", "DIRECT23", "DIRECT24", "DIRECT25", "DIRECT26", "DIRECT27", "DIRECT28", "DIRECT29",
		"DIRECT30", "DIRECT31", "DIRECT32", "DIRECT33", "DIRECT34", "DIRECT35", "DIRECT36", "DIRECT37", "DIRECT38", "DIRECT39",
		"DIRECT40", "DIRECT41", "DIRECT42", "DIRECT43", "DIRECT44", "DIRECT45", "DIRECT46", "DIRECT47", "DIRECT48", "DIRECT49",
		"DIRECT50", "DIRECT51", "DIRECT52", "DIRECT53", "DIRECT54", "DIRECT55", "DIRECT56", "DIRECT57", "DIRECT58", "DIRECT59",
		"DIRECT60", "DIRECT61", "DIRECT62", "DIRECT63", "DIRECT64", "INSIN1", "INSIN2", "INSIN3", "INSIN4", "INSIN5",
		"INSIN6", "INSIN7", "INSIN8", "INSIN9", "INSIN10", "INSIN11", "INSIN12", "INSIN13", "INSIN14", "INSIN15",
		"INSIN16", "INSIN17", "INSIN18", "INSIN19", "INSIN20", "INSIN21", "INSIN22", "INSIN23", "INSIN24", "INSIN25",
		"INSIN26", "INSIN27", "INSIN28", "INSIN29", "INSIN30", "INSIN31", "INSIN32", "INSMIX1", "INSMIX2", "INSMIX3",
		"INSMIX4", "INSMIX5", "INSMIX6", "INSMIX7", "INSMIX8", "INSMIX9", "INSMIX10", "INSMIX11", "INSMIX12", "INSMIX13",
		"INSMIX14", "INSMIX15", "INSMIX16", "INSMTX1", "INSMTX2", "INSMTX3", "INSMTX4", "INSMTX5", "INSMTX6", "INSMTX7",
		"INSMTX8", "INSSTL", "INSSTR", "INSSTC", "CASMIX1", "CASMIX2", "CASMIX3", "CASMIX4", "CASMIX5", "CASMIX6",
		"CASMIX7", "CASMIX8", "CASMIX9", "CASMIX10", "CASMIX11", "CASMIX12", "CASMIX13", "CASMIX14", "CASMIX15", "CASMIX16",
		"CASMTX1", "CASMTX2", "CASMTX3", "CASMTX4", "CASMTX5", "CASMTX6", "CASMTX7", "CASMTX8", "CASSTL", "CASSTR",
		"CASSTC", "CASCUEL", "CASCUER", "STL+C", "STR+C"
	};

	public int _type;

	public int _rack;

	public int _source;

	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			if (_type == 1)
			{
				MixEngine.GEQData[_rack].Type = 0;
			}
			else if (_type == 2)
			{
				MixEngine.GEQData[_rack].Type = 1;
				MixEngine.GEQData[_rack + 8].Type = 1;
			}
			MixEngine.OnAction("RackType", _rack);
		}
	}

	public event RackEventHandler Changed;

	public Rack(int rack)
	{
		_rack = rack;
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
