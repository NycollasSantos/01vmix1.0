using System.Collections.Generic;
using faderspeak;

namespace AirFader;

public class Console
{
	public delegate void ConsoleEventHandler(string property);

	public static ConsoleTypes type;

	public static ConsoleVersions ver;

	public static string macaddr;

	public static int maxInputs;

	public static int maxSTIN;

	public static int maxMIX;

	public static int maxMATRIX;

	public static int maxOutOmni;

	public static int maxOutSlot;

	public static int maxOut2TR;

	public static int _clockSpeed;

	private static volatile int _selChannel;

	public static bool LinkSTMono;

	public static int Lamp;

	public static bool LampToggle;

	public static int CueMode;

	public static List<int> CuedChannels;

	public static bool PrefNaviToSelIn;

	public static bool PrefNaviToSelOut;

	public static bool PrefCueSelLink;

	public static volatile string sceneName;

	public static volatile int sceneNum;

	public static volatile int sceneHigh;

	public static bool hasInputHA;

	public static bool hasInputPhantom;

	public static bool hasInputLPF;

	public static int clockSpeed
	{
		get
		{
			return _clockSpeed;
		}
		set
		{
			_clockSpeed = value;
			MixEngine.faderServer.sendStatus();
			OnChange("ClockSpeed");
		}
	}

	public static int selChannel
	{
		get
		{
			return _selChannel;
		}
		set
		{
			_selChannel = value;
			GeneralParam generalParam = new GeneralParam();
			generalParam.index = 0;
			generalParam.param = GenParam.GEN_SELCHANNEL;
			generalParam.value.Add(value);
			MixEngine.faderServer.sendProto("LOGIC", generalParam);
			OnChange("SelChannel");
		}
	}

	public static event ConsoleEventHandler Changed;

	static Console()
	{
		type = ConsoleTypes.Unknown;
		ver = ConsoleVersions.Unknown;
		maxInputs = 72;
		maxSTIN = 8;
		maxMIX = 16;
		maxMATRIX = 8;
		maxOutOmni = 16;
		maxOutSlot = 32;
		maxOut2TR = 2;
		_clockSpeed = 44;
		_selChannel = 0;
		LinkSTMono = false;
		Lamp = 0;
		LampToggle = false;
		CueMode = 0;
		CuedChannels = new List<int>();
		PrefNaviToSelIn = true;
		PrefNaviToSelOut = true;
		PrefCueSelLink = true;
		sceneName = "";
		sceneNum = 0;
		sceneHigh = 300;
		hasInputHA = true;
		hasInputPhantom = true;
		hasInputLPF = true;
		macaddr = "xxxxxxxxxxxx";
	}

	public static int getOutportCount()
	{
		return maxOutOmni + maxOutSlot + maxOut2TR;
	}

	public static void addCue(int cuechannel)
	{
		if (CuedChannels.Contains(cuechannel))
		{
			return;
		}
		if (CueMode == 1)
		{
			for (int num = CuedChannels.Count - 1; num >= 0; num--)
			{
				MixEngine.chData[CuedChannels[num]].setParam("LOGIC", Channel.Pr.Cue, 0, 0);
				removeCue(CuedChannels[num]);
			}
		}
		if (type == ConsoleTypes.O1V96V2)
		{
			for (int num2 = CuedChannels.Count - 1; num2 >= 0; num2--)
			{
				switch (MixEngine.chData[cuechannel].channelType)
				{
				case ChannelTypes.Input:
				case ChannelTypes.STIN:
					if (MixEngine.chData[CuedChannels[num2]].channelType == ChannelTypes.Mix || MixEngine.chData[CuedChannels[num2]].channelType == ChannelTypes.Matrix)
					{
						MixEngine.chData[CuedChannels[num2]].setParam("LOGIC", Channel.Pr.Cue, 0, 0);
						removeCue(CuedChannels[num2]);
					}
					break;
				default:
					if (MixEngine.chData[CuedChannels[num2]].channelType == ChannelTypes.Input || MixEngine.chData[CuedChannels[num2]].channelType == ChannelTypes.STIN)
					{
						MixEngine.chData[CuedChannels[num2]].setParam("LOGIC", Channel.Pr.Cue, 0, 0);
						removeCue(CuedChannels[num2]);
					}
					break;
				}
			}
		}
		CuedChannels.Add(cuechannel);
	}

	public static void removeCue(int cuechannel)
	{
		if (CuedChannels.Contains(cuechannel))
		{
			CuedChannels.Remove(cuechannel);
		}
	}

	public static void clearCue()
	{
		for (int num = CuedChannels.Count - 1; num >= 0; num--)
		{
			MixEngine.chData[CuedChannels[num]].setParam("LOGIC", Channel.Pr.Cue, 0, 0);
			removeCue(CuedChannels[num]);
		}
	}

	protected static void OnChange(string property)
	{
		if (Console.Changed != null)
		{
			Console.Changed(property);
		}
	}
}
