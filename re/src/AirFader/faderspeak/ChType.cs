using ProtoBuf;

namespace faderspeak;

[ProtoContract(Name = "ChType")]
public enum ChType
{
	[ProtoEnum(Name = "CHTYPE_NONE", Value = 0)]
	CHTYPE_NONE,
	[ProtoEnum(Name = "CHTYPE_INPUT", Value = 1)]
	CHTYPE_INPUT,
	[ProtoEnum(Name = "CHTYPE_STIN", Value = 2)]
	CHTYPE_STIN,
	[ProtoEnum(Name = "CHTYPE_MIX", Value = 3)]
	CHTYPE_MIX,
	[ProtoEnum(Name = "CHTYPE_MATRIX", Value = 4)]
	CHTYPE_MATRIX,
	[ProtoEnum(Name = "CHTYPE_STEREO", Value = 5)]
	CHTYPE_STEREO,
	[ProtoEnum(Name = "CHTYPE_MONITOR", Value = 6)]
	CHTYPE_MONITOR,
	[ProtoEnum(Name = "CHTYPE_CUE", Value = 7)]
	CHTYPE_CUE,
	[ProtoEnum(Name = "CHTYPE_OSC", Value = 8)]
	CHTYPE_OSC,
	[ProtoEnum(Name = "CHTYPE_TB", Value = 9)]
	CHTYPE_TB,
	[ProtoEnum(Name = "CHTYPE_HA_IN", Value = 10)]
	CHTYPE_HA_IN,
	[ProtoEnum(Name = "CHTYPE_MS_IN", Value = 11)]
	CHTYPE_MS_IN,
	[ProtoEnum(Name = "CHTYPE_SLOT_IN", Value = 12)]
	CHTYPE_SLOT_IN,
	[ProtoEnum(Name = "CHTYPE_SLOT_OUT", Value = 13)]
	CHTYPE_SLOT_OUT,
	[ProtoEnum(Name = "CHTYPE_OMNI_OUT", Value = 14)]
	CHTYPE_OMNI_OUT,
	[ProtoEnum(Name = "CHTYPE_D2TR_OUT", Value = 15)]
	CHTYPE_D2TR_OUT,
	[ProtoEnum(Name = "CHTYPE_RACK", Value = 16)]
	CHTYPE_RACK,
	[ProtoEnum(Name = "CHTYPE_EFFECT", Value = 17)]
	CHTYPE_EFFECT,
	[ProtoEnum(Name = "CHTYPE_CLIP", Value = 18)]
	CHTYPE_CLIP
}
