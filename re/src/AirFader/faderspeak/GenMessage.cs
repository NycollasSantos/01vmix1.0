using System;
using System.ComponentModel;
using ProtoBuf;

namespace faderspeak;

[Serializable]
[ProtoContract(Name = "GenMessage")]
public class GenMessage : IExtensible
{
	private MsgType _msgtype;

	private string _message = "";

	private IExtension extensionObject;

	[ProtoMember(1, IsRequired = true, Name = "msgtype", DataFormat = DataFormat.TwosComplement)]
	public MsgType msgtype
	{
		get
		{
			return _msgtype;
		}
		set
		{
			_msgtype = value;
		}
	}

	[ProtoMember(2, IsRequired = false, Name = "message", DataFormat = DataFormat.Default)]
	[DefaultValue("")]
	public string message
	{
		get
		{
			return _message;
		}
		set
		{
			_message = value;
		}
	}

	IExtension IExtensible.GetExtensionObject(bool createIfMissing)
	{
		return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
	}
}
