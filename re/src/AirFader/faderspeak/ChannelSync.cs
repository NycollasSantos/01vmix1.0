using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "ChannelSync")]
public class ChannelSync : IExtensible
{
	private int _Channel;

	private ChType _chType;

	private int _HA;

	private bool _Phantom;

	private bool _Phase;

	private int _Fader;

	private int _RelativeHA;

	private int _RelativeFader;

	private bool _On;

	private bool _Cue;

	private string _Name = "";

	private int _Pan;

	private int _Balance;

	private int _Group;

	private int _Pair;

	private int _BusType;

	private int _PanLink;

	private bool _MuteGroup1;

	private bool _MuteGroup2;

	private bool _MuteGroup3;

	private bool _MuteGroup4;

	private bool _MuteGroup5;

	private bool _MuteGroup6;

	private bool _MuteGroup7;

	private bool _MuteGroup8;

	private bool _DCAGroup1;

	private bool _DCAGroup2;

	private bool _DCAGroup3;

	private bool _DCAGroup4;

	private bool _DCAGroup5;

	private bool _DCAGroup6;

	private bool _DCAGroup7;

	private bool _DCAGroup8;

	private bool _InsertOn;

	private bool _DirectOutOn;

	private bool _ToStereoOn;

	private bool _ToMonoOn;

	private int _PatchIn;

	private bool _EQHPFOn;

	private int _EQHPFFreq;

	private int _EQAtt;

	private int _EQType;

	private bool _EQOn;

	private int _EQ1Type;

	private int _EQ4Type;

	private bool _EQ1HPFOn;

	private bool _EQ4LPFOn;

	private int _EQ1Q;

	private int _EQ1F;

	private int _EQ1G;

	private int _EQ2Q;

	private int _EQ2F;

	private int _EQ2G;

	private int _EQ3Q;

	private int _EQ3F;

	private int _EQ3G;

	private int _EQ4Q;

	private int _EQ4F;

	private int _EQ4G;

	private bool _Dyn0On;

	private int _Dyn0Type;

	private int _Dyn0Attack;

	private int _Dyn0Range;

	private int _Dyn0Hold;

	private int _Dyn0Decay;

	private int _Dyn0Ratio;

	private int _Dyn0Gain;

	private int _Dyn0Knee;

	private int _Dyn0Threshold;

	private bool _Dyn1On;

	private int _Dyn1Type;

	private int _Dyn1Attack;

	private int _Dyn1Range;

	private int _Dyn1Hold;

	private int _Dyn1Release;

	private int _Dyn1Ratio;

	private int _Dyn1Gain;

	private int _Dyn1Knee;

	private int _Dyn1Threshold;

	private string _HardName = "";

	private readonly List<int> _SendToMix = new List<int>();

	private readonly List<bool> _SendToMixOn = new List<bool>();

	private readonly List<bool> _SendToMixPre = new List<bool>();

	private readonly List<int> _SendToMixPan = new List<int>();

	private readonly List<int> _SendToMatrix = new List<int>();

	private readonly List<bool> _SendToMatrixOn = new List<bool>();

	private readonly List<bool> _SendToMatrixPre = new List<bool>();

	private readonly List<int> _SendToMatrixPan = new List<int>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "Channel", DataFormat = DataFormat.TwosComplement)]
	public int Channel
	{
		get
		{
			return _Channel;
		}
		set
		{
			_Channel = value;
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

	[ProtoMember(3, IsRequired = false, Name = "HA", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int HA
	{
		get
		{
			return _HA;
		}
		set
		{
			_HA = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(4, IsRequired = false, Name = "Phantom", DataFormat = DataFormat.Default)]
	public bool Phantom
	{
		get
		{
			return _Phantom;
		}
		set
		{
			_Phantom = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(5, IsRequired = false, Name = "Phase", DataFormat = DataFormat.Default)]
	public bool Phase
	{
		get
		{
			return _Phase;
		}
		set
		{
			_Phase = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(6, IsRequired = false, Name = "Fader", DataFormat = DataFormat.TwosComplement)]
	public int Fader
	{
		get
		{
			return _Fader;
		}
		set
		{
			_Fader = value;
		}
	}

	[ProtoMember(7, IsRequired = false, Name = "RelativeHA", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int RelativeHA
	{
		get
		{
			return _RelativeHA;
		}
		set
		{
			_RelativeHA = value;
		}
	}

	[ProtoMember(8, IsRequired = false, Name = "RelativeFader", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int RelativeFader
	{
		get
		{
			return _RelativeFader;
		}
		set
		{
			_RelativeFader = value;
		}
	}

	[ProtoMember(9, IsRequired = false, Name = "On", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool On
	{
		get
		{
			return _On;
		}
		set
		{
			_On = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(10, IsRequired = false, Name = "Cue", DataFormat = DataFormat.Default)]
	public bool Cue
	{
		get
		{
			return _Cue;
		}
		set
		{
			_Cue = value;
		}
	}

	[ProtoMember(11, IsRequired = false, Name = "Name", DataFormat = DataFormat.Default)]
	[DefaultValue("")]
	public string Name
	{
		get
		{
			return _Name;
		}
		set
		{
			_Name = value;
		}
	}

	[ProtoMember(12, IsRequired = false, Name = "Pan", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Pan
	{
		get
		{
			return _Pan;
		}
		set
		{
			_Pan = value;
		}
	}

	[ProtoMember(13, IsRequired = false, Name = "Balance", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Balance
	{
		get
		{
			return _Balance;
		}
		set
		{
			_Balance = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(14, IsRequired = false, Name = "Group", DataFormat = DataFormat.TwosComplement)]
	public int Group
	{
		get
		{
			return _Group;
		}
		set
		{
			_Group = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(15, IsRequired = false, Name = "Pair", DataFormat = DataFormat.TwosComplement)]
	public int Pair
	{
		get
		{
			return _Pair;
		}
		set
		{
			_Pair = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(16, IsRequired = false, Name = "BusType", DataFormat = DataFormat.TwosComplement)]
	public int BusType
	{
		get
		{
			return _BusType;
		}
		set
		{
			_BusType = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(17, IsRequired = false, Name = "PanLink", DataFormat = DataFormat.TwosComplement)]
	public int PanLink
	{
		get
		{
			return _PanLink;
		}
		set
		{
			_PanLink = value;
		}
	}

	[ProtoMember(18, IsRequired = false, Name = "MuteGroup1", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool MuteGroup1
	{
		get
		{
			return _MuteGroup1;
		}
		set
		{
			_MuteGroup1 = value;
		}
	}

	[ProtoMember(19, IsRequired = false, Name = "MuteGroup2", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool MuteGroup2
	{
		get
		{
			return _MuteGroup2;
		}
		set
		{
			_MuteGroup2 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(20, IsRequired = false, Name = "MuteGroup3", DataFormat = DataFormat.Default)]
	public bool MuteGroup3
	{
		get
		{
			return _MuteGroup3;
		}
		set
		{
			_MuteGroup3 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(21, IsRequired = false, Name = "MuteGroup4", DataFormat = DataFormat.Default)]
	public bool MuteGroup4
	{
		get
		{
			return _MuteGroup4;
		}
		set
		{
			_MuteGroup4 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(22, IsRequired = false, Name = "MuteGroup5", DataFormat = DataFormat.Default)]
	public bool MuteGroup5
	{
		get
		{
			return _MuteGroup5;
		}
		set
		{
			_MuteGroup5 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(23, IsRequired = false, Name = "MuteGroup6", DataFormat = DataFormat.Default)]
	public bool MuteGroup6
	{
		get
		{
			return _MuteGroup6;
		}
		set
		{
			_MuteGroup6 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(24, IsRequired = false, Name = "MuteGroup7", DataFormat = DataFormat.Default)]
	public bool MuteGroup7
	{
		get
		{
			return _MuteGroup7;
		}
		set
		{
			_MuteGroup7 = value;
		}
	}

	[ProtoMember(25, IsRequired = false, Name = "MuteGroup8", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool MuteGroup8
	{
		get
		{
			return _MuteGroup8;
		}
		set
		{
			_MuteGroup8 = value;
		}
	}

	[ProtoMember(26, IsRequired = false, Name = "DCAGroup1", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool DCAGroup1
	{
		get
		{
			return _DCAGroup1;
		}
		set
		{
			_DCAGroup1 = value;
		}
	}

	[ProtoMember(27, IsRequired = false, Name = "DCAGroup2", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool DCAGroup2
	{
		get
		{
			return _DCAGroup2;
		}
		set
		{
			_DCAGroup2 = value;
		}
	}

	[ProtoMember(28, IsRequired = false, Name = "DCAGroup3", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool DCAGroup3
	{
		get
		{
			return _DCAGroup3;
		}
		set
		{
			_DCAGroup3 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(29, IsRequired = false, Name = "DCAGroup4", DataFormat = DataFormat.Default)]
	public bool DCAGroup4
	{
		get
		{
			return _DCAGroup4;
		}
		set
		{
			_DCAGroup4 = value;
		}
	}

	[ProtoMember(30, IsRequired = false, Name = "DCAGroup5", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool DCAGroup5
	{
		get
		{
			return _DCAGroup5;
		}
		set
		{
			_DCAGroup5 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(31, IsRequired = false, Name = "DCAGroup6", DataFormat = DataFormat.Default)]
	public bool DCAGroup6
	{
		get
		{
			return _DCAGroup6;
		}
		set
		{
			_DCAGroup6 = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(32, IsRequired = false, Name = "DCAGroup7", DataFormat = DataFormat.Default)]
	public bool DCAGroup7
	{
		get
		{
			return _DCAGroup7;
		}
		set
		{
			_DCAGroup7 = value;
		}
	}

	[ProtoMember(33, IsRequired = false, Name = "DCAGroup8", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool DCAGroup8
	{
		get
		{
			return _DCAGroup8;
		}
		set
		{
			_DCAGroup8 = value;
		}
	}

	[ProtoMember(34, IsRequired = false, Name = "InsertOn", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool InsertOn
	{
		get
		{
			return _InsertOn;
		}
		set
		{
			_InsertOn = value;
		}
	}

	[ProtoMember(35, IsRequired = false, Name = "DirectOutOn", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool DirectOutOn
	{
		get
		{
			return _DirectOutOn;
		}
		set
		{
			_DirectOutOn = value;
		}
	}

	[ProtoMember(36, IsRequired = false, Name = "ToStereoOn", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool ToStereoOn
	{
		get
		{
			return _ToStereoOn;
		}
		set
		{
			_ToStereoOn = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(37, IsRequired = false, Name = "ToMonoOn", DataFormat = DataFormat.Default)]
	public bool ToMonoOn
	{
		get
		{
			return _ToMonoOn;
		}
		set
		{
			_ToMonoOn = value;
		}
	}

	[ProtoMember(38, IsRequired = false, Name = "PatchIn", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int PatchIn
	{
		get
		{
			return _PatchIn;
		}
		set
		{
			_PatchIn = value;
		}
	}

	[ProtoMember(39, IsRequired = false, Name = "EQHPFOn", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool EQHPFOn
	{
		get
		{
			return _EQHPFOn;
		}
		set
		{
			_EQHPFOn = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(40, IsRequired = false, Name = "EQHPFFreq", DataFormat = DataFormat.TwosComplement)]
	public int EQHPFFreq
	{
		get
		{
			return _EQHPFFreq;
		}
		set
		{
			_EQHPFFreq = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(41, IsRequired = false, Name = "EQAtt", DataFormat = DataFormat.TwosComplement)]
	public int EQAtt
	{
		get
		{
			return _EQAtt;
		}
		set
		{
			_EQAtt = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(42, IsRequired = false, Name = "EQType", DataFormat = DataFormat.TwosComplement)]
	public int EQType
	{
		get
		{
			return _EQType;
		}
		set
		{
			_EQType = value;
		}
	}

	[ProtoMember(43, IsRequired = false, Name = "EQOn", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool EQOn
	{
		get
		{
			return _EQOn;
		}
		set
		{
			_EQOn = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(44, IsRequired = false, Name = "EQ1Type", DataFormat = DataFormat.TwosComplement)]
	public int EQ1Type
	{
		get
		{
			return _EQ1Type;
		}
		set
		{
			_EQ1Type = value;
		}
	}

	[ProtoMember(45, IsRequired = false, Name = "EQ4Type", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int EQ4Type
	{
		get
		{
			return _EQ4Type;
		}
		set
		{
			_EQ4Type = value;
		}
	}

	[DefaultValue(false)]
	[ProtoMember(46, IsRequired = false, Name = "EQ1HPFOn", DataFormat = DataFormat.Default)]
	public bool EQ1HPFOn
	{
		get
		{
			return _EQ1HPFOn;
		}
		set
		{
			_EQ1HPFOn = value;
		}
	}

	[ProtoMember(47, IsRequired = false, Name = "EQ4LPFOn", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool EQ4LPFOn
	{
		get
		{
			return _EQ4LPFOn;
		}
		set
		{
			_EQ4LPFOn = value;
		}
	}

	[ProtoMember(48, IsRequired = false, Name = "EQ1Q", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int EQ1Q
	{
		get
		{
			return _EQ1Q;
		}
		set
		{
			_EQ1Q = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(49, IsRequired = false, Name = "EQ1F", DataFormat = DataFormat.TwosComplement)]
	public int EQ1F
	{
		get
		{
			return _EQ1F;
		}
		set
		{
			_EQ1F = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(50, IsRequired = false, Name = "EQ1G", DataFormat = DataFormat.TwosComplement)]
	public int EQ1G
	{
		get
		{
			return _EQ1G;
		}
		set
		{
			_EQ1G = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(51, IsRequired = false, Name = "EQ2Q", DataFormat = DataFormat.TwosComplement)]
	public int EQ2Q
	{
		get
		{
			return _EQ2Q;
		}
		set
		{
			_EQ2Q = value;
		}
	}

	[ProtoMember(52, IsRequired = false, Name = "EQ2F", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int EQ2F
	{
		get
		{
			return _EQ2F;
		}
		set
		{
			_EQ2F = value;
		}
	}

	[ProtoMember(53, IsRequired = false, Name = "EQ2G", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int EQ2G
	{
		get
		{
			return _EQ2G;
		}
		set
		{
			_EQ2G = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(54, IsRequired = false, Name = "EQ3Q", DataFormat = DataFormat.TwosComplement)]
	public int EQ3Q
	{
		get
		{
			return _EQ3Q;
		}
		set
		{
			_EQ3Q = value;
		}
	}

	[ProtoMember(55, IsRequired = false, Name = "EQ3F", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int EQ3F
	{
		get
		{
			return _EQ3F;
		}
		set
		{
			_EQ3F = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(56, IsRequired = false, Name = "EQ3G", DataFormat = DataFormat.TwosComplement)]
	public int EQ3G
	{
		get
		{
			return _EQ3G;
		}
		set
		{
			_EQ3G = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(57, IsRequired = false, Name = "EQ4Q", DataFormat = DataFormat.TwosComplement)]
	public int EQ4Q
	{
		get
		{
			return _EQ4Q;
		}
		set
		{
			_EQ4Q = value;
		}
	}

	[ProtoMember(58, IsRequired = false, Name = "EQ4F", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int EQ4F
	{
		get
		{
			return _EQ4F;
		}
		set
		{
			_EQ4F = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(59, IsRequired = false, Name = "EQ4G", DataFormat = DataFormat.TwosComplement)]
	public int EQ4G
	{
		get
		{
			return _EQ4G;
		}
		set
		{
			_EQ4G = value;
		}
	}

	[ProtoMember(60, IsRequired = false, Name = "Dyn0On", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool Dyn0On
	{
		get
		{
			return _Dyn0On;
		}
		set
		{
			_Dyn0On = value;
		}
	}

	[ProtoMember(61, IsRequired = false, Name = "Dyn0Type", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn0Type
	{
		get
		{
			return _Dyn0Type;
		}
		set
		{
			_Dyn0Type = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(62, IsRequired = false, Name = "Dyn0Attack", DataFormat = DataFormat.TwosComplement)]
	public int Dyn0Attack
	{
		get
		{
			return _Dyn0Attack;
		}
		set
		{
			_Dyn0Attack = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(63, IsRequired = false, Name = "Dyn0Range", DataFormat = DataFormat.TwosComplement)]
	public int Dyn0Range
	{
		get
		{
			return _Dyn0Range;
		}
		set
		{
			_Dyn0Range = value;
		}
	}

	[ProtoMember(64, IsRequired = false, Name = "Dyn0Hold", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn0Hold
	{
		get
		{
			return _Dyn0Hold;
		}
		set
		{
			_Dyn0Hold = value;
		}
	}

	[ProtoMember(65, IsRequired = false, Name = "Dyn0Decay", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn0Decay
	{
		get
		{
			return _Dyn0Decay;
		}
		set
		{
			_Dyn0Decay = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(66, IsRequired = false, Name = "Dyn0Ratio", DataFormat = DataFormat.TwosComplement)]
	public int Dyn0Ratio
	{
		get
		{
			return _Dyn0Ratio;
		}
		set
		{
			_Dyn0Ratio = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(67, IsRequired = false, Name = "Dyn0Gain", DataFormat = DataFormat.TwosComplement)]
	public int Dyn0Gain
	{
		get
		{
			return _Dyn0Gain;
		}
		set
		{
			_Dyn0Gain = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(68, IsRequired = false, Name = "Dyn0Knee", DataFormat = DataFormat.TwosComplement)]
	public int Dyn0Knee
	{
		get
		{
			return _Dyn0Knee;
		}
		set
		{
			_Dyn0Knee = value;
		}
	}

	[ProtoMember(69, IsRequired = false, Name = "Dyn0Threshold", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn0Threshold
	{
		get
		{
			return _Dyn0Threshold;
		}
		set
		{
			_Dyn0Threshold = value;
		}
	}

	[ProtoMember(70, IsRequired = false, Name = "Dyn1On", DataFormat = DataFormat.Default)]
	[DefaultValue(false)]
	public bool Dyn1On
	{
		get
		{
			return _Dyn1On;
		}
		set
		{
			_Dyn1On = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(71, IsRequired = false, Name = "Dyn1Type", DataFormat = DataFormat.TwosComplement)]
	public int Dyn1Type
	{
		get
		{
			return _Dyn1Type;
		}
		set
		{
			_Dyn1Type = value;
		}
	}

	[ProtoMember(72, IsRequired = false, Name = "Dyn1Attack", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn1Attack
	{
		get
		{
			return _Dyn1Attack;
		}
		set
		{
			_Dyn1Attack = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(73, IsRequired = false, Name = "Dyn1Range", DataFormat = DataFormat.TwosComplement)]
	public int Dyn1Range
	{
		get
		{
			return _Dyn1Range;
		}
		set
		{
			_Dyn1Range = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(74, IsRequired = false, Name = "Dyn1Hold", DataFormat = DataFormat.TwosComplement)]
	public int Dyn1Hold
	{
		get
		{
			return _Dyn1Hold;
		}
		set
		{
			_Dyn1Hold = value;
		}
	}

	[ProtoMember(75, IsRequired = false, Name = "Dyn1Release", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn1Release
	{
		get
		{
			return _Dyn1Release;
		}
		set
		{
			_Dyn1Release = value;
		}
	}

	[ProtoMember(76, IsRequired = false, Name = "Dyn1Ratio", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn1Ratio
	{
		get
		{
			return _Dyn1Ratio;
		}
		set
		{
			_Dyn1Ratio = value;
		}
	}

	[ProtoMember(77, IsRequired = false, Name = "Dyn1Gain", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn1Gain
	{
		get
		{
			return _Dyn1Gain;
		}
		set
		{
			_Dyn1Gain = value;
		}
	}

	[DefaultValue(0)]
	[ProtoMember(78, IsRequired = false, Name = "Dyn1Knee", DataFormat = DataFormat.TwosComplement)]
	public int Dyn1Knee
	{
		get
		{
			return _Dyn1Knee;
		}
		set
		{
			_Dyn1Knee = value;
		}
	}

	[ProtoMember(79, IsRequired = false, Name = "Dyn1Threshold", DataFormat = DataFormat.TwosComplement)]
	[DefaultValue(0)]
	public int Dyn1Threshold
	{
		get
		{
			return _Dyn1Threshold;
		}
		set
		{
			_Dyn1Threshold = value;
		}
	}

	[ProtoMember(80, IsRequired = false, Name = "HardName", DataFormat = DataFormat.Default)]
	[DefaultValue("")]
	public string HardName
	{
		get
		{
			return _HardName;
		}
		set
		{
			_HardName = value;
		}
	}

	[ProtoMember(81, Name = "SendToMix", DataFormat = DataFormat.TwosComplement)]
	public List<int> SendToMix => _SendToMix;

	[ProtoMember(82, Name = "SendToMixOn", DataFormat = DataFormat.Default)]
	public List<bool> SendToMixOn => _SendToMixOn;

	[ProtoMember(83, Name = "SendToMixPre", DataFormat = DataFormat.Default)]
	public List<bool> SendToMixPre => _SendToMixPre;

	[ProtoMember(84, Name = "SendToMixPan", DataFormat = DataFormat.TwosComplement)]
	public List<int> SendToMixPan => _SendToMixPan;

	[ProtoMember(85, Name = "SendToMatrix", DataFormat = DataFormat.TwosComplement)]
	public List<int> SendToMatrix => _SendToMatrix;

	[ProtoMember(86, Name = "SendToMatrixOn", DataFormat = DataFormat.Default)]
	public List<bool> SendToMatrixOn => _SendToMatrixOn;

	[ProtoMember(87, Name = "SendToMatrixPre", DataFormat = DataFormat.Default)]
	public List<bool> SendToMatrixPre => _SendToMatrixPre;

	[ProtoMember(88, Name = "SendToMatrixPan", DataFormat = DataFormat.TwosComplement)]
	public List<int> SendToMatrixPan => _SendToMatrixPan;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
