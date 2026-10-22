using System.Collections.Generic;

namespace AirFader;

public class MuteGroup
{
	private bool _muted;

	private int _group;

	public List<int> members;

	public bool muted
	{
		get
		{
			return _muted;
		}
		set
		{
			_muted = value;
		}
	}

	public MuteGroup(int group)
	{
		_muted = false;
		_group = group;
		members = new List<int>();
	}
}
