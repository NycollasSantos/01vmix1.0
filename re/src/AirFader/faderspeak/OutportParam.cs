using System;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "OutportParam")]
public class OutportParam : IExtensible
{
	private OutportParam _param;

	private int _index;

	private int _value;

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "param", DataFormat = DataFormat.Default)]
	public OutportParam param
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

	[ProtoMember(3, IsRequired = true, Name = "value", DataFormat = DataFormat.TwosComplement)]
	public int value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
