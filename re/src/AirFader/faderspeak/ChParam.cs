using ProtoBuf;

namespace faderspeak;

[ProtoContract(Name = "ChParam")]
public enum ChParam
{
	[ProtoEnum(Name = "CH_NONE", Value = 0)]
	CH_NONE,
	[ProtoEnum(Name = "CH_HA", Value = 1)]
	CH_HA,
	[ProtoEnum(Name = "CH_PHANTOM", Value = 2)]
	CH_PHANTOM,
	[ProtoEnum(Name = "CH_PHASE", Value = 3)]
	CH_PHASE,
	[ProtoEnum(Name = "CH_FADER", Value = 4)]
	CH_FADER,
	[ProtoEnum(Name = "CH_RELATIVEHA", Value = 5)]
	CH_RELATIVEHA,
	[ProtoEnum(Name = "CH_RELATIVEFADER", Value = 6)]
	CH_RELATIVEFADER,
	[ProtoEnum(Name = "CH_ON", Value = 7)]
	CH_ON,
	[ProtoEnum(Name = "CH_CUE", Value = 8)]
	CH_CUE,
	[ProtoEnum(Name = "CH_NAME", Value = 9)]
	CH_NAME,
	[ProtoEnum(Name = "CH_PAN", Value = 10)]
	CH_PAN,
	[ProtoEnum(Name = "CH_BALANCE", Value = 11)]
	CH_BALANCE,
	[ProtoEnum(Name = "CH_GROUP", Value = 12)]
	CH_GROUP,
	[ProtoEnum(Name = "CH_PAIR", Value = 13)]
	CH_PAIR,
	[ProtoEnum(Name = "CH_BUSTYPE", Value = 14)]
	CH_BUSTYPE,
	[ProtoEnum(Name = "CH_PANLINK", Value = 15)]
	CH_PANLINK,
	[ProtoEnum(Name = "CH_MUTEGROUP", Value = 16)]
	CH_MUTEGROUP,
	[ProtoEnum(Name = "CH_DCAGROUP", Value = 17)]
	CH_DCAGROUP,
	[ProtoEnum(Name = "CH_INSERTON", Value = 18)]
	CH_INSERTON,
	[ProtoEnum(Name = "CH_DIRECTOUTON", Value = 19)]
	CH_DIRECTOUTON,
	[ProtoEnum(Name = "CH_TOSTEREOON", Value = 20)]
	CH_TOSTEREOON,
	[ProtoEnum(Name = "CH_TOMONOON", Value = 21)]
	CH_TOMONOON,
	[ProtoEnum(Name = "CH_PATCHIN", Value = 22)]
	CH_PATCHIN,
	[ProtoEnum(Name = "CH_EQHPFON", Value = 23)]
	CH_EQHPFON,
	[ProtoEnum(Name = "CH_EQHPFFREQ", Value = 24)]
	CH_EQHPFFREQ,
	[ProtoEnum(Name = "CH_EQATT", Value = 25)]
	CH_EQATT,
	[ProtoEnum(Name = "CH_EQTYPE", Value = 26)]
	CH_EQTYPE,
	[ProtoEnum(Name = "CH_EQON", Value = 27)]
	CH_EQON,
	[ProtoEnum(Name = "CH_EQ1TYPE", Value = 28)]
	CH_EQ1TYPE,
	[ProtoEnum(Name = "CH_EQ4TYPE", Value = 29)]
	CH_EQ4TYPE,
	[ProtoEnum(Name = "CH_EQ1HPFON", Value = 30)]
	CH_EQ1HPFON,
	[ProtoEnum(Name = "CH_EQ4LPFON", Value = 31)]
	CH_EQ4LPFON,
	[ProtoEnum(Name = "CH_EQ1Q", Value = 32)]
	CH_EQ1Q,
	[ProtoEnum(Name = "CH_EQ1F", Value = 33)]
	CH_EQ1F,
	[ProtoEnum(Name = "CH_EQ1G", Value = 34)]
	CH_EQ1G,
	[ProtoEnum(Name = "CH_EQ2Q", Value = 35)]
	CH_EQ2Q,
	[ProtoEnum(Name = "CH_EQ2F", Value = 36)]
	CH_EQ2F,
	[ProtoEnum(Name = "CH_EQ2G", Value = 37)]
	CH_EQ2G,
	[ProtoEnum(Name = "CH_EQ3Q", Value = 38)]
	CH_EQ3Q,
	[ProtoEnum(Name = "CH_EQ3F", Value = 39)]
	CH_EQ3F,
	[ProtoEnum(Name = "CH_EQ3G", Value = 40)]
	CH_EQ3G,
	[ProtoEnum(Name = "CH_EQ4Q", Value = 41)]
	CH_EQ4Q,
	[ProtoEnum(Name = "CH_EQ4F", Value = 42)]
	CH_EQ4F,
	[ProtoEnum(Name = "CH_EQ4G", Value = 43)]
	CH_EQ4G,
	[ProtoEnum(Name = "CH_SENDTOMIX", Value = 44)]
	CH_SENDTOMIX,
	[ProtoEnum(Name = "CH_SENDTOMIXON", Value = 45)]
	CH_SENDTOMIXON,
	[ProtoEnum(Name = "CH_SENDTOMIXPRE", Value = 46)]
	CH_SENDTOMIXPRE,
	[ProtoEnum(Name = "CH_SENDTOMIXPAN", Value = 47)]
	CH_SENDTOMIXPAN,
	[ProtoEnum(Name = "CH_SENDTOMATRIX", Value = 48)]
	CH_SENDTOMATRIX,
	[ProtoEnum(Name = "CH_SENDTOMATRIXON", Value = 49)]
	CH_SENDTOMATRIXON,
	[ProtoEnum(Name = "CH_SENDTOMATRIXPRE", Value = 50)]
	CH_SENDTOMATRIXPRE,
	[ProtoEnum(Name = "CH_SENDTOMATRIXPAN", Value = 51)]
	CH_SENDTOMATRIXPAN,
	[ProtoEnum(Name = "CH_DYN0ON", Value = 52)]
	CH_DYN0ON,
	[ProtoEnum(Name = "CH_DYN0TYPE", Value = 53)]
	CH_DYN0TYPE,
	[ProtoEnum(Name = "CH_DYN0ATTACK", Value = 54)]
	CH_DYN0ATTACK,
	[ProtoEnum(Name = "CH_DYN0RANGE", Value = 55)]
	CH_DYN0RANGE,
	[ProtoEnum(Name = "CH_DYN0HOLD", Value = 56)]
	CH_DYN0HOLD,
	[ProtoEnum(Name = "CH_DYN0DECAY", Value = 57)]
	CH_DYN0DECAY,
	[ProtoEnum(Name = "CH_DYN0RATIO", Value = 58)]
	CH_DYN0RATIO,
	[ProtoEnum(Name = "CH_DYN0GAIN", Value = 59)]
	CH_DYN0GAIN,
	[ProtoEnum(Name = "CH_DYN0KNEE", Value = 60)]
	CH_DYN0KNEE,
	[ProtoEnum(Name = "CH_DYN0THRESHOLD", Value = 61)]
	CH_DYN0THRESHOLD,
	[ProtoEnum(Name = "CH_DYN1ON", Value = 62)]
	CH_DYN1ON,
	[ProtoEnum(Name = "CH_DYN1TYPE", Value = 63)]
	CH_DYN1TYPE,
	[ProtoEnum(Name = "CH_DYN1ATTACK", Value = 64)]
	CH_DYN1ATTACK,
	[ProtoEnum(Name = "CH_DYN1RANGE", Value = 65)]
	CH_DYN1RANGE,
	[ProtoEnum(Name = "CH_DYN1HOLD", Value = 66)]
	CH_DYN1HOLD,
	[ProtoEnum(Name = "CH_DYN1RELEASE", Value = 67)]
	CH_DYN1RELEASE,
	[ProtoEnum(Name = "CH_DYN1RATIO", Value = 68)]
	CH_DYN1RATIO,
	[ProtoEnum(Name = "CH_DYN1GAIN", Value = 69)]
	CH_DYN1GAIN,
	[ProtoEnum(Name = "CH_DYN1KNEE", Value = 70)]
	CH_DYN1KNEE,
	[ProtoEnum(Name = "CH_DYN1THRESHOLD", Value = 71)]
	CH_DYN1THRESHOLD,
	[ProtoEnum(Name = "CH_EQFLATTEN", Value = 72)]
	CH_EQFLATTEN,
	[ProtoEnum(Name = "CH_METERPRE", Value = 73)]
	CH_METERPRE,
	[ProtoEnum(Name = "CH_TYPE", Value = 74)]
	CH_TYPE,
	[ProtoEnum(Name = "CH_HARDNAME", Value = 75)]
	CH_HARDNAME,
	[ProtoEnum(Name = "CH_METERDYN0IN", Value = 76)]
	CH_METERDYN0IN,
	[ProtoEnum(Name = "CH_METERDYN1IN", Value = 77)]
	CH_METERDYN1IN,
	[ProtoEnum(Name = "CH_METERDYN0GR", Value = 78)]
	CH_METERDYN0GR,
	[ProtoEnum(Name = "CH_METERDYN1GR", Value = 79)]
	CH_METERDYN1GR,
	[ProtoEnum(Name = "CH_METEREQIN", Value = 80)]
	CH_METEREQIN,
	[ProtoEnum(Name = "CH_METEREQOUT", Value = 81)]
	CH_METEREQOUT,
	[ProtoEnum(Name = "CH_METERPOST", Value = 82)]
	CH_METERPOST
}
