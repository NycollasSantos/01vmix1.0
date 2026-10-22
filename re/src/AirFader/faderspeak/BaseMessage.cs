using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "BaseMessage")]
public class BaseMessage : IExtensible
{
	private Command _command;

	private ChannelParam _channelParam;

	private GenMessage _genMessage;

	private UserRecord _userRecord;

	private SystemStatus _sysStatus;

	private ChannelSync _channelSync;

	private BusSync _busSync;

	private GEQSync _geqSync;

	private RackSync _rackSync;

	private uint _sessionID;

	private string _userName = "";

	private string _passHash = "";

	private GEQSetParam _geqSetParam;

	private string _version = "";

	private GeneralParam _generalParam;

	private readonly List<MeterData> _meterData = new List<MeterData>();

	private IExtension extensionObject;

	[DefaultValue(null)]
	[ProtoMember(1, IsRequired = false, Name = "command", DataFormat = DataFormat.Default)]
	public Command command
	{
		get
		{
			return _command;
		}
		set
		{
			_command = value;
		}
	}

	[ProtoMember(2, IsRequired = false, Name = "channelParam", DataFormat = DataFormat.Default)]
	[DefaultValue(null)]
	public ChannelParam channelParam
	{
		get
		{
			return _channelParam;
		}
		set
		{
			_channelParam = value;
		}
	}

	[ProtoMember(3, IsRequired = false, Name = "genMessage", DataFormat = DataFormat.Default)]
	[DefaultValue(null)]
	public GenMessage genMessage
	{
		get
		{
			return _genMessage;
		}
		set
		{
			_genMessage = value;
		}
	}

	[DefaultValue(null)]
	[ProtoMember(4, IsRequired = false, Name = "userRecord", DataFormat = DataFormat.Default)]
	public UserRecord userRecord
	{
		get
		{
			return _userRecord;
		}
		set
		{
			_userRecord = value;
		}
	}

	[DefaultValue(null)]
	[ProtoMember(5, IsRequired = false, Name = "sysStatus", DataFormat = DataFormat.Default)]
	public SystemStatus sysStatus
	{
		get
		{
			return _sysStatus;
		}
		set
		{
			_sysStatus = value;
		}
	}

	[DefaultValue(null)]
	[ProtoMember(6, IsRequired = false, Name = "channelSync", DataFormat = DataFormat.Default)]
	public ChannelSync channelSync
	{
		get
		{
			return _channelSync;
		}
		set
		{
			_channelSync = value;
		}
	}

	[DefaultValue(null)]
	[ProtoMember(7, IsRequired = false, Name = "busSync", DataFormat = DataFormat.Default)]
	public BusSync busSync
	{
		get
		{
			return _busSync;
		}
		set
		{
			_busSync = value;
		}
	}

	[ProtoMember(8, IsRequired = false, Name = "geqSync", DataFormat = DataFormat.Default)]
	[DefaultValue(null)]
	public GEQSync geqSync
	{
		get
		{
			return _geqSync;
		}
		set
		{
			_geqSync = value;
		}
	}

	[DefaultValue(null)]
	[ProtoMember(9, IsRequired = false, Name = "rackSync", DataFormat = DataFormat.Default)]
	public RackSync rackSync
	{
		get
		{
			return _rackSync;
		}
		set
		{
			_rackSync = value;
		}
	}

	[ProtoMember(10, IsRequired = false, Name = "sessionID", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0L)]
	public uint sessionID
	{
		get
		{
			return _sessionID;
		}
		set
		{
			_sessionID = value;
		}
	}

	[DefaultValue("")]
	[ProtoMember(11, IsRequired = false, Name = "userName", DataFormat = DataFormat.Default)]
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

	[DefaultValue("")]
	[ProtoMember(12, IsRequired = false, Name = "passHash", DataFormat = DataFormat.Default)]
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

	[DefaultValue(null)]
	[ProtoMember(13, IsRequired = false, Name = "geqSetParam", DataFormat = DataFormat.Default)]
	public GEQSetParam geqSetParam
	{
		get
		{
			return _geqSetParam;
		}
		set
		{
			_geqSetParam = value;
		}
	}

	[DefaultValue("")]
	[ProtoMember(14, IsRequired = false, Name = "version", DataFormat = DataFormat.Default)]
	public string version
	{
		get
		{
			return _version;
		}
		set
		{
			_version = value;
		}
	}

	[ProtoMember(15, IsRequired = false, Name = "generalParam", DataFormat = DataFormat.Default)]
	[DefaultValue(null)]
	public GeneralParam generalParam
	{
		get
		{
			return _generalParam;
		}
		set
		{
			_generalParam = value;
		}
	}

	[ProtoMember(16, Name = "meterData", DataFormat = DataFormat.Default)]
	public List<MeterData> meterData => _meterData;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
