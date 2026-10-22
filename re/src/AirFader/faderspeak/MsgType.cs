using ProtoBuf;

namespace faderspeak;

[ProtoContract(Name = "MsgType")]
public enum MsgType
{
	[ProtoEnum(Name = "MSG_OK", Value = 0)]
	MSG_OK = 0,
	[ProtoEnum(Name = "MSG_FAIL", Value = 1)]
	MSG_FAIL = 1,
	[ProtoEnum(Name = "MSG_AUTH", Value = 2)]
	MSG_AUTH = 2,
	[ProtoEnum(Name = "MSG_DENIED", Value = 3)]
	MSG_DENIED = 3,
	[ProtoEnum(Name = "MSG_CONSOLE_ONLINE", Value = 5)]
	MSG_CONSOLE_ONLINE = 5,
	[ProtoEnum(Name = "MSG_CONSOLE_OFFLINE", Value = 6)]
	MSG_CONSOLE_OFFLINE = 6,
	[ProtoEnum(Name = "MSG_BCAST", Value = 9)]
	MSG_BCAST = 9
}
