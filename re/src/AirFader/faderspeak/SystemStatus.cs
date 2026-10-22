using System;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "SystemStatus")]
public class SystemStatus : IExtensible
{
	private int _consoleType;

	private int _consoleStatus;

	private int _meterStatus;

	private int _clockSpeed;

	private int _chOffsetInput;

	private int _chOffsetSTIN;

	private int _chOffsetMix;

	private int _chOffsetMatrix;

	private int _chOffsetStereo;

	private int _chOffsetMonitor;

	private int _maxInputs;

	private int _maxSTIN;

	private int _maxMix;

	private int _maxMatrix;

	private string _consoleLabel = "";

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = false, Name = "consoleType", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int consoleType
	{
		get
		{
			return _consoleType;
		}
		set
		{
			_consoleType = value;
		}
	}

	[ProtoMember(2, IsRequired = false, Name = "consoleStatus", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int consoleStatus
	{
		get
		{
			return _consoleStatus;
		}
		set
		{
			_consoleStatus = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(3, IsRequired = false, Name = "meterStatus", DataFormat = DataFormat.TwosComplement)]
	public int meterStatus
	{
		get
		{
			return _meterStatus;
		}
		set
		{
			_meterStatus = value;
		}
	}

	[ProtoMember(4, IsRequired = false, Name = "clockSpeed", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int clockSpeed
	{
		get
		{
			return _clockSpeed;
		}
		set
		{
			_clockSpeed = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(10, IsRequired = false, Name = "chOffsetInput", DataFormat = DataFormat.TwosComplement)]
	public int chOffsetInput
	{
		get
		{
			return _chOffsetInput;
		}
		set
		{
			_chOffsetInput = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(11, IsRequired = false, Name = "chOffsetSTIN", DataFormat = DataFormat.TwosComplement)]
	public int chOffsetSTIN
	{
		get
		{
			return _chOffsetSTIN;
		}
		set
		{
			_chOffsetSTIN = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(12, IsRequired = false, Name = "chOffsetMix", DataFormat = DataFormat.TwosComplement)]
	public int chOffsetMix
	{
		get
		{
			return _chOffsetMix;
		}
		set
		{
			_chOffsetMix = value;
		}
	}

	[ProtoMember(13, IsRequired = false, Name = "chOffsetMatrix", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int chOffsetMatrix
	{
		get
		{
			return _chOffsetMatrix;
		}
		set
		{
			_chOffsetMatrix = value;
		}
	}

	[ProtoMember(14, IsRequired = false, Name = "chOffsetStereo", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int chOffsetStereo
	{
		get
		{
			return _chOffsetStereo;
		}
		set
		{
			_chOffsetStereo = value;
		}
	}

	[ProtoMember(15, IsRequired = false, Name = "chOffsetMonitor", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int chOffsetMonitor
	{
		get
		{
			return _chOffsetMonitor;
		}
		set
		{
			_chOffsetMonitor = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(16, IsRequired = false, Name = "maxInputs", DataFormat = DataFormat.TwosComplement)]
	public int maxInputs
	{
		get
		{
			return _maxInputs;
		}
		set
		{
			_maxInputs = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(17, IsRequired = false, Name = "maxSTIN", DataFormat = DataFormat.TwosComplement)]
	public int maxSTIN
	{
		get
		{
			return _maxSTIN;
		}
		set
		{
			_maxSTIN = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(18, IsRequired = false, Name = "maxMix", DataFormat = DataFormat.TwosComplement)]
	public int maxMix
	{
		get
		{
			return _maxMix;
		}
		set
		{
			_maxMix = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(19, IsRequired = false, Name = "maxMatrix", DataFormat = DataFormat.TwosComplement)]
	public int maxMatrix
	{
		get
		{
			return _maxMatrix;
		}
		set
		{
			_maxMatrix = value;
		}
	}

	[ProtoMember(32, IsRequired = false, Name = "consoleLabel", DataFormat = DataFormat.Default)]
	[DefaultValue("")]
	public string consoleLabel
	{
		get
		{
			return _consoleLabel;
		}
		set
		{
			_consoleLabel = value;
		}
	}

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
