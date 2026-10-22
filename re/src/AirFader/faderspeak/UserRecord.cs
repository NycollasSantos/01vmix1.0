using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "UserRecord")]
public class UserRecord : IExtensible
{
	private uint _userId;

	private string _userName;

	private string _passHash;

	private bool _active;

	private bool _accessChannel;

	private bool _accessMainMix;

	private readonly List<bool> _accessMix = new List<bool>();

	private readonly List<bool> _accessMatrix = new List<bool>();

	private readonly List<bool> _accessChannels = new List<bool>();

	private bool _accessMasters;

	private bool _accessRack;

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "userId", DataFormat = DataFormat.TwosComplement)]
	public uint userId
	{
		get
		{
			return _userId;
		}
		set
		{
			_userId = value;
		}
	}

	[ProtoMember(2, IsRequired = true, Name = "userName", DataFormat = DataFormat.Default)]
	public string userName
	{
		get
		{
			return _userName;
		}
		set
		{
			_userName = value;
		}
	}

	[ProtoMember(3, IsRequired = true, Name = "passHash", DataFormat = DataFormat.Default)]
	public string passHash
	{
		get
		{
			return _passHash;
		}
		set
		{
			_passHash = value;
		}
	}

	[ProtoMember(4, IsRequired = true, Name = "active", DataFormat = DataFormat.Default)]
	public bool active
	{
		get
		{
			return _active;
		}
		set
		{
			_active = value;
		}
	}

	[ProtoMember(5, IsRequired = true, Name = "accessChannel", DataFormat = DataFormat.Default)]
	public bool accessChannel
	{
		get
		{
			return _accessChannel;
		}
		set
		{
			_accessChannel = value;
		}
	}

	[ProtoMember(6, IsRequired = true, Name = "accessMainMix", DataFormat = DataFormat.Default)]
	public bool accessMainMix
	{
		get
		{
			return _accessMainMix;
		}
		set
		{
			_accessMainMix = value;
		}
	}

	[ProtoMember(7, Name = "accessMix", DataFormat = DataFormat.Default)]
	public List<bool> accessMix => _accessMix;

	[ProtoMember(8, Name = "accessMatrix", DataFormat = DataFormat.Default)]
	public List<bool> accessMatrix => _accessMatrix;

	[ProtoMember(9, Name = "accessChannels", DataFormat = DataFormat.Default)]
	public List<bool> accessChannels => _accessChannels;

	[ProtoMember(10, IsRequired = false, Name = "accessMasters", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool accessMasters
	{
		get
		{
			return _accessMasters;
		}
		set
		{
			_accessMasters = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(11, IsRequired = false, Name = "accessRack", DataFormat = DataFormat.Default)]
	public bool accessRack
	{
		get
		{
			return _accessRack;
		}
		set
		{
			_accessRack = value;
		}
	}

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
