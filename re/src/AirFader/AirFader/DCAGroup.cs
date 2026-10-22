using System.Collections.Generic;

namespace AirFader;

public class DCAGroup
{
	public bool _muted;

	private int _group;

	private int _fader;

	public string _name;

	public string hardname;

	public List<int> members;

	public int fader => _fader;

	public DCAGroup(int group)
	{
		_muted = false;
		_group = group;
		_fader = 0;
		members = new List<int>();
		hardname = "DCA " + (group + 1);
		_name = hardname;
	}

	public void setFader(int value, string source)
	{
		if (_fader != value)
		{
			_fader = value;
			if (source != "console")
			{
				MixEngine.setParam("DCAFader", _group, value);
			}
			MixEngine.OnAction("DCAFader", _group);
		}
	}

	public void setMute(bool value, string source)
	{
		if (_muted != value)
		{
			if (source != "console")
			{
				MixEngine.setParam("DCAMute", _group, value);
			}
			_muted = value;
			MixEngine.OnAction("DCAMute", _group);
		}
	}
}
