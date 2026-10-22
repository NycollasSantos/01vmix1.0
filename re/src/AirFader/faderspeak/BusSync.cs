using System;
using System.Collections.Generic;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "BusSync")]
public class BusSync : IExtensible
{
	private int _BusChannel;

	private int _StartCh;

	private readonly List<ChType> _chType = new List<ChType>();

	private readonly List<bool> _On = new List<bool>();

	private readonly List<bool> _Pos = new List<bool>();

	private readonly List<bool> _Cue = new List<bool>();

	private readonly List<int> _Level = new List<int>();

	private readonly List<int> _Pan = new List<int>();

	private readonly List<string> _Name = new List<string>();

	private readonly List<string> _HardName = new List<string>();

	private readonly List<bool> _Pair = new List<bool>();

	private readonly List<bool> _PanLink = new List<bool>();

	private readonly List<int> _BusType = new List<int>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "BusChannel", DataFormat = DataFormat.TwosComplement)]
	public int BusChannel
	{
		get
		{
			return _BusChannel;
		}
		set
		{
			_BusChannel = value;
		}
	}

	[ProtoMember(2, IsRequired = true, Name = "StartCh", DataFormat = DataFormat.TwosComplement)]
	public int StartCh
	{
		get
		{
			return _StartCh;
		}
		set
		{
			_StartCh = value;
		}
	}

	[ProtoMember(3, Name = "chType", DataFormat = DataFormat.TwosComplement)]
	public List<ChType> chType => _chType;

	[ProtoMember(4, Name = "On", DataFormat = DataFormat.Default)]
	public List<bool> On => _On;

	[ProtoMember(5, Name = "Pos", DataFormat = DataFormat.Default)]
	public List<bool> Pos => _Pos;

	[ProtoMember(6, Name = "Cue", DataFormat = DataFormat.Default)]
	public List<bool> Cue => _Cue;

	[ProtoMember(7, Name = "Level", DataFormat = DataFormat.TwosComplement)]
	public List<int> Level => _Level;

	[ProtoMember(8, Name = "Pan", DataFormat = DataFormat.TwosComplement)]
	public List<int> Pan => _Pan;

	[ProtoMember(10, Name = "Name", DataFormat = DataFormat.Default)]
	public List<string> Name => _Name;

	[ProtoMember(11, Name = "HardName", DataFormat = DataFormat.Default)]
	public List<string> HardName => _HardName;

	[ProtoMember(12, Name = "Pair", DataFormat = DataFormat.Default)]
	public List<bool> Pair => _Pair;

	[ProtoMember(13, Name = "PanLink", DataFormat = DataFormat.Default)]
	public List<bool> PanLink => _PanLink;

	[ProtoMember(14, Name = "BusType", DataFormat = DataFormat.TwosComplement)]
	public List<int> BusType => _BusType;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
