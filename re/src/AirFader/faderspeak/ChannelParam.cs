using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "ChannelParam")]
public class ChannelParam : IExtensible
{
	private ChParam _param;

	private ChType _chType;

	private int _channel;

	private int _index;

	private readonly List<int> _value = new List<int>();

	private readonly List<string> _valuestr = new List<string>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "param", DataFormat = DataFormat.TwosComplement)]
	public ChParam param
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

	[DefaultValue(ChType.CHTYPE_NONE)]
	[ProtoMember(2, IsRequired = false, Name = "chType", DataFormat = DataFormat.TwosComplement)]
	public ChType chType
	{
		get
		{
			return _chType;
		}
		set
		{
			_chType = value;
		}
	}

	[ProtoMember(3, IsRequired = true, Name = "channel", DataFormat = DataFormat.TwosComplement)]
	public int channel
	{
		get
		{
			return _channel;
		}
		set
		{
			_channel = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(4, IsRequired = false, Name = "index", DataFormat = DataFormat.TwosComplement)]
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

	[ProtoMember(5, Name = "value", DataFormat = DataFormat.TwosComplement)]
	public List<int> value => _value;

	[ProtoMember(16, Name = "valuestr", DataFormat = DataFormat.Default)]
	public List<string> valuestr => _valuestr;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
