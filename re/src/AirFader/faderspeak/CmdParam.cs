using ProtoBuf;

namespace faderspeak;

[ProtoContract(Name = "CmdParam")]
public enum CmdParam
{
	[ProtoEnum(Name = "CMD_NONE", Value = 0)]
	CMD_NONE = 0,
	[ProtoEnum(Name = "CMD_SENDPARAM", Value = 1)]
	CMD_SENDPARAM = 1,
	[ProtoEnum(Name = "CMD_SYNCPARAM", Value = 2)]
	CMD_SYNCPARAM = 2,
	[ProtoEnum(Name = "CMD_SYNCCHANNEL", Value = 3)]
	CMD_SYNCCHANNEL = 3,
	[ProtoEnum(Name = "CMD_GETSYS", Value = 4)]
	CMD_GETSYS = 4,
	[ProtoEnum(Name = "CMD_GOODBYE", Value = 9)]
	CMD_GOODBYE = 9,
	[ProtoEnum(Name = "CMD_CHANNELSYNC", Value = 10)]
	CMD_CHANNELSYNC = 10,
	[ProtoEnum(Name = "CMD_BUSSYNC", Value = 11)]
	CMD_BUSSYNC = 11,
	[ProtoEnum(Name = "CMD_GEQSYNC", Value = 12)]
	CMD_GEQSYNC = 12,
	[ProtoEnum(Name = "CMD_RACKSYNC", Value = 13)]
	CMD_RACKSYNC = 13,
	[ProtoEnum(Name = "CMD_OUTPORTSYNC", Value = 14)]
	CMD_OUTPORTSYNC = 14,
	[ProtoEnum(Name = "CMD_MUTEMASTERSYNC", Value = 15)]
	CMD_MUTEMASTERSYNC = 15,
	[ProtoEnum(Name = "CMD_RESYNC", Value = 16)]
	CMD_RESYNC = 16
}
