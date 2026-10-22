using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "GEQSetParam")]
public class GEQSetParam : IExtensible
{
	private GEQParam _param;

	private int _rack;

	private int _index;

	private readonly List<int> _value = new List<int>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "param", DataFormat = DataFormat.TwosComplement)]
	public GEQParam param
	{
		get
		{
			return _param;
		}
		set
		{
			_param = value;
		}
	}

	[ProtoMember(2, IsRequired = true, Name = "rack", DataFormat = DataFormat.TwosComplement)]
	public int rack
	{
		get
		{
			return _rack;
		}
		set
		{
			_rack = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(3, IsRequired = false, Name = "index", DataFormat = DataFormat.TwosComplement)]
	public int index
	{
		get
		{
			return _index;
		}
		set
		{
			_index = value;
		}
	}

	[ProtoMember(4, Name = "value", DataFormat = DataFormat.TwosComplement)]
	public List<int> value => _value;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
