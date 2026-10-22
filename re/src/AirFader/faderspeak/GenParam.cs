using ProtoBuf;

namespace faderspeak;

[ProtoContract(Name = "GenParam")]
public enum GenParam
{
	[ProtoEnum(Name = "GEN_NONE", Value = 0)]
	GEN_NONE = 0,
	[ProtoEnum(Name = "GEN_MUTEMASTER", Value = 1)]
	GEN_MUTEMASTER = 1,
	[ProtoEnum(Name = "GEN_SELCHANNEL", Value = 2)]
	GEN_SELCHANNEL = 2,
	[ProtoEnum(Name = "GEN_METERS", Value = 3)]
	GEN_METERS = 3,
	[ProtoEnum(Name = "GEN_OUT_NONE", Value = 5)]
	GEN_OUT_NONE = 5,
	[ProtoEnum(Name = "GEN_OUT_DELAYON", Value = 6)]
	GEN_OUT_DELAYON = 6,
	[ProtoEnum(Name = "GEN_OUT_DELAYTIME", Value = 7)]
	GEN_OUT_DELAYTIME = 7,
	[ProtoEnum(Name = "GEN_OUT_PHASE", Value = 8)]
	GEN_OUT_PHASE = 8,
	[ProtoEnum(Name = "GEN_OUT_ATT", Value = 9)]
	GEN_OUT_ATT = 9,
	[ProtoEnum(Name = "GEN_OUT_NAME", Value = 10)]
	GEN_OUT_NAME = 10,
	[ProtoEnum(Name = "GEN_OUT_COUNT", Value = 11)]
	GEN_OUT_COUNT = 11,
	[ProtoEnum(Name = "GEN_OUT_ALL", Value = 12)]
	GEN_OUT_ALL = 12
}
