using ProtoBuf;

namespace faderspeak;

[ProtoContract(Name = "GEQParam")]
public enum GEQParam
{
	[ProtoEnum(Name = "GEQ_NONE", Value = 0)]
	GEQ_NONE,
	[ProtoEnum(Name = "GEQ_TYPE", Value = 1)]
	GEQ_TYPE,
	[ProtoEnum(Name = "GEQ_BANDSAVAIL", Value = 2)]
	GEQ_BANDSAVAIL,
	[ProtoEnum(Name = "GEQ_LINK", Value = 3)]
	GEQ_LINK,
	[ProtoEnum(Name = "GEQ_LINKLR", Value = 4)]
	GEQ_LINKLR,
	[ProtoEnum(Name = "GEQ_ON", Value = 5)]
	GEQ_ON,
	[ProtoEnum(Name = "GEQ_BAND", Value = 6)]
	GEQ_BAND,
	[ProtoEnum(Name = "GEQ_SOURCE", Value = 7)]
	GEQ_SOURCE
}
