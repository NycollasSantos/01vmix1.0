using System;
using System.Collections.Generic;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "MeterData")]
public class MeterData : IExtensible
{
	private int _m_ch;

	private ChType _chType;

	private readonly List<int> _m_pre = new List<int>();

	private readonly List<int> _m_eq_IN = new List<int>();

	private readonly List<int> _m_eq_OUT = new List<int>();

	private readonly List<int> _m_dyn0_IN = new List<int>();

	private readonly List<int> _m_dyn0_GR = new List<int>();

	private readonly List<int> _m_dyn1_IN = new List<int>();

	private readonly List<int> _m_dyn1_GR = new List<int>();

	private readonly List<int> _m_post = new List<int>();

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "m_ch", DataFormat = DataFormat.TwosComplement)]
	public int m_ch
	{
		get
		{
			return _m_ch;
		}
		set
		{
			_m_ch = value;
		}
	}

	[ProtoMember(2, IsRequired = true, Name = "chType", DataFormat = DataFormat.TwosComplement)]
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

	[ProtoMember(3, Name = "m_pre", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_pre => _m_pre;

	[ProtoMember(4, Name = "m_eq_IN", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_eq_IN => _m_eq_IN;

	[ProtoMember(5, Name = "m_eq_OUT", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_eq_OUT => _m_eq_OUT;

	[ProtoMember(6, Name = "m_dyn0_IN", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_dyn0_IN => _m_dyn0_IN;

	[ProtoMember(7, Name = "m_dyn0_GR", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_dyn0_GR => _m_dyn0_GR;

	[ProtoMember(8, Name = "m_dyn1_IN", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_dyn1_IN => _m_dyn1_IN;

	[ProtoMember(9, Name = "m_dyn1_GR", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_dyn1_GR => _m_dyn1_GR;

	[ProtoMember(10, Name = "m_post", DataFormat = DataFormat.TwosComplement)]
	public List<int> m_post => _m_post;

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
