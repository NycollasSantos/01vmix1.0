using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "GEQSync")]
public class GEQSync : IExtensible
{
	private int _GEQRack;

	private int _GEQType;

	private int _GEQBandsAvail;

	private int _GEQLink;

	private int _GEQLinkLR;

	private bool _GEQOn;

	private readonly List<int> _GEQBand = new List<int>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "GEQRack", DataFormat = DataFormat.TwosComplement)]
	public int GEQRack
	{
		get
		{
			return _GEQRack;
		}
		set
		{
			_GEQRack = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(2, IsRequired = false, Name = "GEQType", DataFormat = DataFormat.TwosComplement)]
	public int GEQType
	{
		get
		{
			return _GEQType;
		}
		set
		{
			_GEQType = value;
		}
	}

	[ProtoMember(3, IsRequired = false, Name = "GEQBandsAvail", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int GEQBandsAvail
	{
		get
		{
			return _GEQBandsAvail;
		}
		set
		{
			_GEQBandsAvail = value;
		}
	}

	[ProtoMember(4, IsRequired = false, Name = "GEQLink", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int GEQLink
	{
		get
		{
			return _GEQLink;
		}
		set
		{
			_GEQLink = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(5, IsRequired = false, Name = "GEQLinkLR", DataFormat = DataFormat.TwosComplement)]
	public int GEQLinkLR
	{
		get
		{
			return _GEQLinkLR;
		}
		set
		{
			_GEQLinkLR = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(6, IsRequired = false, Name = "GEQOn", DataFormat = DataFormat.Default)]
	public bool GEQOn
	{
		get
		{
			return _GEQOn;
		}
		set
		{
			_GEQOn = value;
		}
	}

	[ProtoMember(7, Name = "GEQBand", DataFormat = DataFormat.TwosComplement)]
	public List<int> GEQBand => _GEQBand;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
