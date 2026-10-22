using System;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "Command")]
public class Command : IExtensible
{
	private CmdParam _command;

	private ChParam _param;

	private ChType _chType;

	private int _channelLo;

	private int _channelHi;

	private int _indexLo;

	private int _indexHi;

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "command", DataFormat = DataFormat.TwosComplement)]
	public CmdParam command
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

	[ProtoMember(2, IsRequired = true, Name = "param", DataFormat = DataFormat.TwosComplement)]
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
	[ProtoMember(3, IsRequired = false, Name = "chType", DataFormat = DataFormat.TwosComplement)]
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

	[ProtoMember(5, IsRequired = false, Name = "channelLo", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int channelLo
	{
		get
		{
			return _channelLo;
		}
		set
		{
			_channelLo = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(6, IsRequired = false, Name = "channelHi", DataFormat = DataFormat.TwosComplement)]
	public int channelHi
	{
		get
		{
			return _channelHi;
		}
		set
		{
			_channelHi = value;
		}
	}

	[ProtoMember(7, IsRequired = false, Name = "indexLo", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int indexLo
	{
		get
		{
			return _indexLo;
		}
		set
		{
			_indexLo = value;
		}
	}

	[ProtoMember(8, IsRequired = false, Name = "indexHi", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int indexHi
	{
		get
		{
			return _indexHi;
		}
		set
		{
			_indexHi = value;
		}
	}

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
