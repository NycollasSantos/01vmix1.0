using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "GeneralParam")]
public class GeneralParam : IExtensible
{
	private GenParam _param;

	private int _index;

	private int _subindex;

	private bool _request;

	private readonly List<int> _value = new List<int>();

	private readonly List<string> _valuestr = new List<string>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "param", DataFormat = DataFormat.TwosComplement)]
	public GenParam param
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

	[ProtoMember(2, IsRequired = true, Name = "index", DataFormat = DataFormat.TwosComplement)]
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

	[ProtoMember(3, IsRequired = false, Name = "subindex", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int subindex
	{
		get
		{
			return _subindex;
		}
		set
		{
			_subindex = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(4, IsRequired = false, Name = "request", DataFormat = DataFormat.Default)]
	public bool request
	{
		get
		{
			return _request;
		}
		set
		{
			_request = value;
		}
	}

	[ProtoMember(6, Name = "value", DataFormat = DataFormat.TwosComplement)]
	public List<int> value => _value;

	[ProtoMember(7, Name = "valuestr", DataFormat = DataFormat.Default)]
	public List<string> valuestr => _valuestr;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
