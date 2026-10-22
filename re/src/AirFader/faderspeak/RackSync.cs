using System;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "RackSync")]
public class RackSync : IExtensible
{
	private int _RackSlot;

	private int _RackType;

	private int _RackSource;

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "RackSlot", DataFormat = DataFormat.TwosComplement)]
	public int RackSlot
	{
		get
		{
			return _RackSlot;
		}
		set
		{
			_RackSlot = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(2, IsRequired = false, Name = "RackType", DataFormat = DataFormat.TwosComplement)]
	public int RackType
	{
		get
		{
			return _RackType;
		}
		set
		{
			_RackType = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(3, IsRequired = false, Name = "RackSource", DataFormat = DataFormat.TwosComplement)]
	public int RackSource
	{
		get
		{
			return _RackSource;
		}
		set
		{
			_RackSource = value;
		}
	}

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
