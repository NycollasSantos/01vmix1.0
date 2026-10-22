using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using AirFader.Properties;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using faderspeak;

namespace AirFader;

public class MixEngine
{
	public enum Pr
	{
		None,
		Cue,
		MuteMaster,
		DCAMute,
		GEQBand,
		GEQFlat,
		GEQOn,
		GEQLink,
		GEQLinkLR,
		GEQType,
		RackType,
		RackSource,
		Name,
		HA,
		Phantom,
		SceneName,
		SceneDump,
		SceneValid,
		SceneLoad,
		SceneSave,
		SceneNum
	}

	public class queueItem
	{
		public enum msgType
		{
			Channel,
			Engine
		}

		public msgType type;

		public Channel.Pr channelParam;

		public Pr engineParam;

		public int channel;

		public int index;

		public queueItem()
		{
			type = msgType.Channel;
		}

		public queueItem(Channel.Pr newParam, int newChannel, int newIndex)
		{
			type = msgType.Channel;
			channelParam = newParam;
			channel = newChannel;
			index = newIndex;
		}

		public queueItem(Pr newParam, int newChannel, int newIndex)
		{
			type = msgType.Engine;
			engineParam = newParam;
			channel = newChannel;
			index = newIndex;
		}

		public override bool Equals(object obj)
		{
			if (obj is queueItem)
			{
				queueItem queueItem = (queueItem)obj;
				if (type == msgType.Channel)
				{
					if (queueItem.channel == channel && queueItem.channelParam == channelParam && queueItem.type == type && queueItem.index == index)
					{
						return true;
					}
				}
				else if (queueItem.channel == channel && queueItem.engineParam == engineParam && queueItem.type == type && queueItem.index == index)
				{
					return true;
				}
			}
			return false;
		}
	}

	public class ConnParams
	{
		public string inputName { get; set; }

		public string outputName { get; set; }
	}

	public delegate void EngineMessageHandler(string strMessage, int errorCode);

	public delegate void EngineEventHandler(string property, int index);

	public delegate void ChannelHandler(Channel.Pr param, int channel, int paramIndex, int value);

	public delegate void EngineHandler(Pr param, int rack, int paramIndex, int value);

	public delegate void EFXParamHandler(int objectIndex, string param, int paramIndex);

	public class reqQueueItem
	{
		public enum msgType
		{
			Channel,
			Engine
		}

		public msgType type;

		public Channel.Pr channelParam;

		public Pr engineParam;

		public int channel;

		public int index;

		public long timeQueued;

		public long timeSent;

		public int retries;

		public bool sent;

		public int sentCount => reqQueue.FindAll((reqQueueItem item) => item.sent).Count;

		public int waitingCount => reqQueue.FindAll((reqQueueItem item) => !item.sent).Count;

		public reqQueueItem(Channel.Pr newParam, int newIndex, int newChannel)
		{
			type = msgType.Channel;
			channelParam = newParam;
			channel = newChannel;
			index = newIndex;
			sent = false;
			timeQueued = DateTime.Now.Ticks;
		}

		public reqQueueItem(Pr newParam, int newIndex, int newChannel)
		{
			type = msgType.Engine;
			engineParam = newParam;
			channel = newChannel;
			index = newIndex;
			sent = false;
			timeQueued = DateTime.Now.Ticks;
		}
	}

	public static Dictionary<Pr, int> paramValue;

	public static Dictionary<Pr, int> paramMin;

	public static Dictionary<Pr, int> paramMax;

	public static Dictionary<Pr, string> paramName;

	public static Dictionary<string, Pr> nameParam;

	public static FaderService faderServer;

	public static List<int> updateChannel;

	public static List<int> updateRack;

	public static Queue<int> sceneNameQueue;

	public static ManualResetEvent termFlag;

	public static decimal[] dbMap;

	public static string[] portNameLS9;

	public static string[] portNameM7CL;

	public static string[] mapOutPatchLS9;

	public static string[] mapOutPatchM7CL;

	public static string[] mapOutPatch01V96;

	public static string[] mapInPatchLS9;

	public static string[] mapInPatchM7CL;

	public static string[] mapInPatch01V96;

	public static string[] mapOutPatch;

	public static string[] mapInPatch;

	private static int[] offset1;

	public static OutputDevice outDevice;

	public static InputDevice inDevice;

	public static Queue<byte[]> inQueue;

	private static ManualResetEvent inWait;

	public static IWin32Window hMainWindow;

	public static System.Timers.Timer heartBeat;

	public static int heartBeats;

	public static ConnectionStatus connStatus;

	public static ConnectionPhase connPhase;

	public static string model;

	public static string macaddr;

	public static int MIDIdeviceID;

	public static int licenseClients;

	// -----------------------------------------------------------------------
	// Licensing policy (open-source build).
	//
	// The commercial product required a purchased license key, offered a
	// time limited trial and limited the number of simultaneously connected
	// mobile clients. This build is free and open source, so the whole
	// licensing subsystem is switched off: no key, no trial, no online
	// registration and no vendor phone-home.
	//
	// Set LICENSING_ENABLED to true to restore the original behaviour.
	// -----------------------------------------------------------------------
	public const bool LICENSING_ENABLED = false;

	// Maximum number of mobile clients accepted by the built-in server.
	public const int UNLOCKED_CLIENT_LIMIT = 99;

	// Owner/key shown in the About tab when no license is required.
	public const string OPEN_SOURCE_OWNER = "Open Source";

	public const string OPEN_SOURCE_KEY = "Not required";

	private static DateTime dtConnStart;

	public static int maxAndroidConnections;

	public static int maxIOSConnections;

	public static int chOffsetInput;

	public static int chOffsetSTIN;

	public static int chOffsetMix;

	public static int chOffsetMatrix;

	public static int chOffsetStereo;

	public static int chOffsetMonitor;

	public static string dumpData;

	public static volatile int xStereoMeter;

	public static int xMeterSample;

	public static int xQ;

	public static int xF;

	private static Queue<queueItem> sendQueue;

	private static ManualResetEvent sendFlag;

	private static Thread commThread;

	private static Thread sendThread;

	public static Channel[] chData;

	public static Group[] chGroup;

	public static MuteGroup[] muteGroup;

	public static DCAGroup[] DCA;

	public static Scene[] sceneData;

	public static Rack[] RackData;

	public static GEQ[] GEQData;

	public static EFX[] EFXData;

	public static PEQ PEQCopy;

	public static OutPort[] OutPort;

	public static int[] HA;

	public static int[] Phantom;

	private static int serialRetry;

	private static object sysExLock;

	private static int meterCount;

	public static List<reqQueueItem> reqQueue;

	public static int channelHold;

	private static TextWriter logFile;

	private static object logLock;

	private static bool logIsOK;

	public static event EngineMessageHandler NewMessage;

	public static event EngineEventHandler Action;

	public static event ChannelHandler ChannelChanged;

	public static event EngineHandler EngineChanged;

	public static event EFXParamHandler EFXParamChanged;

	public static ulong Base36ToUInt64(string base36String)
	{
		try
		{
			string text = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			base36String = base36String.ToUpper();
			int num = 0;
			int num2 = 0;
			ulong num3 = 0uL;
			do
			{
				num2 = text.IndexOf(base36String[num]);
				num3 = num3 * 36 + (uint)num2;
				num++;
			}
			while (num < base36String.Length);
			return num3;
		}
		catch (Exception)
		{
			Loc.Msg("Problem in Base36to64()", "Static Error");
		}
		return 0uL;
	}

	public static string UInt64ToBase36(ulong uInt64)
	{
		try
		{
			string text = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			uint num = 12u;
			ulong num2 = 0uL;
			char[] array = new char[num + 1];
			do
			{
				num2 = uInt64 % 36;
				array[num] = text[(int)num2];
				uInt64 = (uInt64 - num2) / 36;
				num--;
			}
			while (uInt64 != 0);
			char[] array2 = new char[1];
			char[] trimChars = array2;
			return new string(array).Trim(trimChars);
		}
		catch (Exception)
		{
			Loc.Msg("Problem in Base64to36()", "Static Error");
		}
		return "";
	}

	public static string StuffValueStr(int value)
	{
		long num = value;
		long num2 = 0L;
		int num3 = 0;
		for (int i = 0; i < 5; i++)
		{
			num2 += (num & 0x7F) << num3;
			num >>= 7;
			num3 += 8;
		}
		return num2.ToString("X10");
	}

	public static string StuffString(string name)
	{
		byte[] array = Encoding.ASCII.GetBytes(name);
		Array.Resize(ref array, 4);
		Array.Reverse(array);
		long num = BitConverter.ToInt32(array, 0);
		long num2 = 0L;
		int num3 = 0;
		while (num > 0)
		{
			num2 += (num & 0x7F) << num3;
			num >>= 7;
			num3 += 8;
		}
		return num2.ToString("X10");
	}

	public static string UnStuffString(string hex)
	{
		long num = long.Parse(hex, NumberStyles.AllowHexSpecifier);
		long num2 = 0L;
		int num3 = 0;
		while (num > 0)
		{
			num2 += (num & 0x7F) << num3;
			num >>= 8;
			num3 += 7;
		}
		string text = "";
		while (num2 > 0)
		{
			text = (((num2 & 0xFF) != 0) ? ((char)(num2 & 0xFF) + text) : ("\t" + text));
			num2 >>= 8;
		}
		return text;
	}

	public static string UnStuffValue(string hex)
	{
		long num = long.Parse(hex, NumberStyles.AllowHexSpecifier);
		long num2 = 0L;
		int num3 = 0;
		while (num > 0)
		{
			num2 += (num & 0x7F) << num3;
			num >>= 8;
			num3 += 7;
		}
		string text = "";
		return num2.ToString("x8");
	}

	public static byte[] UnStuffBytes(string hex)
	{
		long num = long.Parse(hex, NumberStyles.AllowHexSpecifier);
		long num2 = 0L;
		int num3 = 0;
		while (num > 0)
		{
			num2 += (num & 0x7F) << num3;
			num >>= 8;
			num3 += 7;
		}
		return ReverseBytes(BitConverter.GetBytes((int)num2));
	}

	private static byte[] ReverseBytes(byte[] inArray)
	{
		int num = inArray.Length - 1;
		for (int i = 0; i < inArray.Length / 2; i++)
		{
			byte b = inArray[i];
			inArray[i] = inArray[num];
			inArray[num] = b;
			num--;
		}
		return inArray;
	}

	public static byte[] ToByteArray(string hex)
	{
		int length = hex.Length;
		byte[] array = new byte[length / 2];
		try
		{
			for (int i = 0; i < length; i += 2)
			{
				array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
		}
		catch (Exception ex)
		{
			logError("ToByteArray()", "Math", ex.ToString());
		}
		return array;
	}

	public static string Right(string param, int length)
	{
		return param.Substring(param.Length - length, length);
	}

	public static int FuzzyFind(decimal[] group, decimal find)
	{
		int num = Array.BinarySearch(group, find);
		if (num >= 0)
		{
			return num;
		}
		if (num < 0)
		{
			num = -num - 1;
		}
		if (num == 0)
		{
			return num;
		}
		if (num >= group.Length)
		{
			return group.Length - 1;
		}
		if (num > group.Length - 1)
		{
			num = group.Length - 1;
		}
		if (num <= 0)
		{
			logError("FuzzyFind()", "Math", "Subzero index (" + num + "). Please notify support!");
		}
		if (num >= group.Length)
		{
			logError("FuzzyFind()", "Math", "Overlimit index (" + num + "). Please notify support!");
		}
		decimal num2 = group[num] - find;
		decimal num3 = find - group[num - 1];
		if (num2 < num3)
		{
			return num;
		}
		return num - 1;
	}

	static MixEngine()
	{
		paramValue = new Dictionary<Pr, int>();
		paramMin = new Dictionary<Pr, int>();
		paramMax = new Dictionary<Pr, int>();
		paramName = new Dictionary<Pr, string>();
		nameParam = new Dictionary<string, Pr>();
		faderServer = new FaderService();
		updateChannel = new List<int>();
		updateRack = new List<int>();
		sceneNameQueue = new Queue<int>();
		termFlag = new ManualResetEvent(initialState: false);
		dbMap = new decimal[1024]
		{
			-999.00m, -138.00m, -135.00m, -132.00m, -129.00m, -126.00m, -123.00m, -120.00m, -117.00m, -114.00m,
			-111.00m, -108.00m, -105.00m, -102.00m, -99.00m, -96.00m, -95.00m, -94.00m, -93.00m, -92.00m,
			-91.00m, -90.00m, -89.00m, -88.00m, -87.00m, -86.00m, -85.00m, -84.00m, -83.00m, -82.00m,
			-81.00m, -80.00m, -79.00m, -78.00m, -77.80m, -77.60m, -77.40m, -77.20m, -77.00m, -76.80m,
			-76.60m, -76.40m, -76.20m, -76.00m, -75.80m, -75.60m, -75.40m, -75.20m, -75.00m, -74.80m,
			-74.60m, -74.40m, -74.20m, -74.00m, -73.80m, -73.60m, -73.40m, -73.20m, -73.00m, -72.80m,
			-72.60m, -72.40m, -72.20m, -72.00m, -71.80m, -71.60m, -71.40m, -71.20m, -71.00m, -70.80m,
			-70.60m, -70.40m, -70.20m, -70.00m, -69.80m, -69.60m, -69.40m, -69.20m, -69.00m, -68.80m,
			-68.60m, -68.40m, -68.20m, -68.00m, -67.80m, -67.60m, -67.40m, -67.20m, -67.00m, -66.80m,
			-66.60m, -66.40m, -66.20m, -66.00m, -65.80m, -65.60m, -65.40m, -65.20m, -65.00m, -64.80m,
			-64.60m, -64.40m, -64.20m, -64.00m, -63.80m, -63.60m, -63.40m, -63.20m, -63.00m, -62.80m,
			-62.60m, -62.40m, -62.20m, -62.00m, -61.80m, -61.60m, -61.40m, -61.20m, -61.00m, -60.80m,
			-60.60m, -60.40m, -60.20m, -60.00m, -59.80m, -59.60m, -59.40m, -59.20m, -59.00m, -58.80m,
			-58.60m, -58.40m, -58.20m, -58.00m, -57.80m, -57.60m, -57.40m, -57.20m, -57.00m, -56.80m,
			-56.60m, -56.40m, -56.20m, -56.00m, -55.80m, -55.60m, -55.40m, -55.20m, -55.00m, -54.80m,
			-54.60m, -54.40m, -54.20m, -54.00m, -53.80m, -53.60m, -53.40m, -53.20m, -53.00m, -52.80m,
			-52.60m, -52.40m, -52.20m, -52.00m, -51.80m, -51.60m, -51.40m, -51.20m, -51.00m, -50.80m,
			-50.60m, -50.40m, -50.20m, -50.00m, -49.80m, -49.60m, -49.40m, -49.20m, -49.00m, -48.80m,
			-48.60m, -48.40m, -48.20m, -48.00m, -47.80m, -47.60m, -47.40m, -47.20m, -47.00m, -46.80m,
			-46.60m, -46.40m, -46.20m, -46.00m, -45.80m, -45.60m, -45.40m, -45.20m, -45.00m, -44.80m,
			-44.60m, -44.40m, -44.20m, -44.00m, -43.80m, -43.60m, -43.40m, -43.20m, -43.00m, -42.80m,
			-42.60m, -42.40m, -42.20m, -42.00m, -41.80m, -41.60m, -41.40m, -41.20m, -41.00m, -40.80m,
			-40.60m, -40.40m, -40.20m, -40.00m, -39.90m, -39.80m, -39.70m, -39.60m, -39.50m, -39.40m,
			-39.30m, -39.20m, -39.10m, -39.00m, -38.90m, -38.80m, -38.70m, -38.60m, -38.50m, -38.40m,
			-38.30m, -38.20m, -38.10m, -38.00m, -37.90m, -37.80m, -37.70m, -37.60m, -37.50m, -37.40m,
			-37.30m, -37.20m, -37.10m, -37.00m, -36.90m, -36.80m, -36.70m, -36.60m, -36.50m, -36.40m,
			-36.30m, -36.20m, -36.10m, -36.00m, -35.90m, -35.80m, -35.70m, -35.60m, -35.50m, -35.40m,
			-35.30m, -35.20m, -35.10m, -35.00m, -34.90m, -34.80m, -34.70m, -34.60m, -34.50m, -34.40m,
			-34.30m, -34.20m, -34.10m, -34.00m, -33.90m, -33.80m, -33.70m, -33.60m, -33.50m, -33.40m,
			-33.30m, -33.20m, -33.10m, -33.00m, -32.90m, -32.80m, -32.70m, -32.60m, -32.50m, -32.40m,
			-32.30m, -32.20m, -32.10m, -32.00m, -31.90m, -31.80m, -31.70m, -31.60m, -31.50m, -31.40m,
			-31.30m, -31.20m, -31.10m, -31.00m, -30.90m, -30.80m, -30.70m, -30.60m, -30.50m, -30.40m,
			-30.30m, -30.20m, -30.10m, -30.00m, -29.90m, -29.80m, -29.70m, -29.60m, -29.50m, -29.40m,
			-29.30m, -29.20m, -29.10m, -29.00m, -28.90m, -28.80m, -28.70m, -28.60m, -28.50m, -28.40m,
			-28.30m, -28.20m, -28.10m, -28.00m, -27.90m, -27.80m, -27.70m, -27.60m, -27.50m, -27.40m,
			-27.30m, -27.20m, -27.10m, -27.00m, -26.90m, -26.80m, -26.70m, -26.60m, -26.50m, -26.40m,
			-26.30m, -26.20m, -26.10m, -26.00m, -25.90m, -25.80m, -25.70m, -25.60m, -25.50m, -25.40m,
			-25.30m, -25.20m, -25.10m, -25.00m, -24.90m, -24.80m, -24.70m, -24.60m, -24.50m, -24.40m,
			-24.30m, -24.20m, -24.10m, -24.00m, -23.90m, -23.80m, -23.70m, -23.60m, -23.50m, -23.40m,
			-23.30m, -23.20m, -23.10m, -23.00m, -22.90m, -22.80m, -22.70m, -22.60m, -22.50m, -22.40m,
			-22.30m, -22.20m, -22.10m, -22.00m, -21.90m, -21.80m, -21.70m, -21.60m, -21.50m, -21.40m,
			-21.30m, -21.20m, -21.10m, -21.00m, -20.90m, -20.80m, -20.70m, -20.60m, -20.50m, -20.40m,
			-20.30m, -20.20m, -20.10m, -20.00m, -19.95m, -19.90m, -19.85m, -19.80m, -19.75m, -19.70m,
			-19.65m, -19.60m, -19.55m, -19.50m, -19.45m, -19.40m, -19.35m, -19.30m, -19.25m, -19.20m,
			-19.15m, -19.10m, -19.05m, -19.00m, -18.95m, -18.90m, -18.85m, -18.80m, -18.75m, -18.70m,
			-18.65m, -18.60m, -18.55m, -18.50m, -18.45m, -18.40m, -18.35m, -18.30m, -18.25m, -18.20m,
			-18.15m, -18.10m, -18.05m, -18.00m, -17.95m, -17.90m, -17.85m, -17.80m, -17.75m, -17.70m,
			-17.65m, -17.60m, -17.55m, -17.50m, -17.45m, -17.40m, -17.35m, -17.30m, -17.25m, -17.20m,
			-17.15m, -17.10m, -17.05m, -17.00m, -16.95m, -16.90m, -16.85m, -16.80m, -16.75m, -16.70m,
			-16.65m, -16.60m, -16.55m, -16.50m, -16.45m, -16.40m, -16.35m, -16.30m, -16.25m, -16.20m,
			-16.15m, -16.10m, -16.05m, -16.00m, -15.95m, -15.90m, -15.85m, -15.80m, -15.75m, -15.70m,
			-15.65m, -15.60m, -15.55m, -15.50m, -15.45m, -15.40m, -15.35m, -15.30m, -15.25m, -15.20m,
			-15.15m, -15.10m, -15.05m, -15.00m, -14.95m, -14.90m, -14.85m, -14.80m, -14.75m, -14.70m,
			-14.65m, -14.60m, -14.55m, -14.50m, -14.45m, -14.40m, -14.35m, -14.30m, -14.25m, -14.20m,
			-14.15m, -14.10m, -14.05m, -14.00m, -13.95m, -13.90m, -13.85m, -13.80m, -13.75m, -13.70m,
			-13.65m, -13.60m, -13.55m, -13.50m, -13.45m, -13.40m, -13.35m, -13.30m, -13.25m, -13.20m,
			-13.15m, -13.10m, -13.05m, -13.00m, -12.95m, -12.90m, -12.85m, -12.80m, -12.75m, -12.70m,
			-12.65m, -12.60m, -12.55m, -12.50m, -12.45m, -12.40m, -12.35m, -12.30m, -12.25m, -12.20m,
			-12.15m, -12.10m, -12.05m, -12.00m, -11.95m, -11.90m, -11.85m, -11.80m, -11.75m, -11.70m,
			-11.65m, -11.60m, -11.55m, -11.50m, -11.45m, -11.40m, -11.35m, -11.30m, -11.25m, -11.20m,
			-11.15m, -11.10m, -11.05m, -11.00m, -10.95m, -10.90m, -10.85m, -10.80m, -10.75m, -10.70m,
			-10.65m, -10.60m, -10.55m, -10.50m, -10.45m, -10.40m, -10.35m, -10.30m, -10.25m, -10.20m,
			-10.15m, -10.10m, -10.05m, -10.00m, -9.95m, -9.90m, -9.85m, -9.80m, -9.75m, -9.70m,
			-9.65m, -9.60m, -9.55m, -9.50m, -9.45m, -9.40m, -9.35m, -9.30m, -9.25m, -9.20m,
			-9.15m, -9.10m, -9.05m, -9.00m, -8.95m, -8.90m, -8.85m, -8.80m, -8.75m, -8.70m,
			-8.65m, -8.60m, -8.55m, -8.50m, -8.45m, -8.40m, -8.35m, -8.30m, -8.25m, -8.20m,
			-8.15m, -8.10m, -8.05m, -8.00m, -7.95m, -7.90m, -7.85m, -7.80m, -7.75m, -7.70m,
			-7.65m, -7.60m, -7.55m, -7.50m, -7.45m, -7.40m, -7.35m, -7.30m, -7.25m, -7.20m,
			-7.15m, -7.10m, -7.05m, -7.00m, -6.95m, -6.90m, -6.85m, -6.80m, -6.75m, -6.70m,
			-6.65m, -6.60m, -6.55m, -6.50m, -6.45m, -6.40m, -6.35m, -6.30m, -6.25m, -6.20m,
			-6.15m, -6.10m, -6.05m, -6.00m, -5.95m, -5.90m, -5.85m, -5.80m, -5.75m, -5.70m,
			-5.65m, -5.60m, -5.55m, -5.50m, -5.45m, -5.40m, -5.35m, -5.30m, -5.25m, -5.20m,
			-5.15m, -5.10m, -5.05m, -5.00m, -4.95m, -4.90m, -4.85m, -4.80m, -4.75m, -4.70m,
			-4.65m, -4.60m, -4.55m, -4.50m, -4.45m, -4.40m, -4.35m, -4.30m, -4.25m, -4.20m,
			-4.15m, -4.10m, -4.05m, -4.00m, -3.95m, -3.90m, -3.85m, -3.80m, -3.75m, -3.70m,
			-3.65m, -3.60m, -3.55m, -3.50m, -3.45m, -3.40m, -3.35m, -3.30m, -3.25m, -3.20m,
			-3.15m, -3.10m, -3.05m, -3.00m, -2.95m, -2.90m, -2.85m, -2.80m, -2.75m, -2.70m,
			-2.65m, -2.60m, -2.55m, -2.50m, -2.45m, -2.40m, -2.35m, -2.30m, -2.25m, -2.20m,
			-2.15m, -2.10m, -2.05m, -2.00m, -1.95m, -1.90m, -1.85m, -1.80m, -1.75m, -1.70m,
			-1.65m, -1.60m, -1.55m, -1.50m, -1.45m, -1.40m, -1.35m, -1.30m, -1.25m, -1.20m,
			-1.15m, -1.10m, -1.05m, -1.00m, -0.95m, -0.90m, -0.85m, -0.80m, -0.75m, -0.70m,
			-0.65m, -0.60m, -0.55m, -0.50m, -0.45m, -0.40m, -0.35m, -0.30m, -0.25m, -0.20m,
			-0.15m, -0.10m, -0.05m, 0.00m, 0.05m, 0.10m, 0.15m, 0.20m, 0.25m, 0.30m,
			0.35m, 0.40m, 0.45m, 0.50m, 0.55m, 0.60m, 0.65m, 0.70m, 0.75m, 0.80m,
			0.85m, 0.90m, 0.95m, 1.00m, 1.05m, 1.10m, 1.15m, 1.20m, 1.25m, 1.30m,
			1.35m, 1.40m, 1.45m, 1.50m, 1.55m, 1.60m, 1.65m, 1.70m, 1.75m, 1.80m,
			1.85m, 1.90m, 1.95m, 2.00m, 2.05m, 2.10m, 2.15m, 2.20m, 2.25m, 2.30m,
			2.35m, 2.40m, 2.45m, 2.50m, 2.55m, 2.60m, 2.65m, 2.70m, 2.75m, 2.80m,
			2.85m, 2.90m, 2.95m, 3.00m, 3.05m, 3.10m, 3.15m, 3.20m, 3.25m, 3.30m,
			3.35m, 3.40m, 3.45m, 3.50m, 3.55m, 3.60m, 3.65m, 3.70m, 3.75m, 3.80m,
			3.85m, 3.90m, 3.95m, 4.00m, 4.05m, 4.10m, 4.15m, 4.20m, 4.25m, 4.30m,
			4.35m, 4.40m, 4.45m, 4.50m, 4.55m, 4.60m, 4.65m, 4.70m, 4.75m, 4.80m,
			4.85m, 4.90m, 4.95m, 5.00m, 5.05m, 5.10m, 5.15m, 5.20m, 5.25m, 5.30m,
			5.35m, 5.40m, 5.45m, 5.50m, 5.55m, 5.60m, 5.65m, 5.70m, 5.75m, 5.80m,
			5.85m, 5.90m, 5.95m, 6.00m, 6.05m, 6.10m, 6.15m, 6.20m, 6.25m, 6.30m,
			6.35m, 6.40m, 6.45m, 6.50m, 6.55m, 6.60m, 6.65m, 6.70m, 6.75m, 6.80m,
			6.85m, 6.90m, 6.95m, 7.00m, 7.05m, 7.10m, 7.15m, 7.20m, 7.25m, 7.30m,
			7.35m, 7.40m, 7.45m, 7.50m, 7.55m, 7.60m, 7.65m, 7.70m, 7.75m, 7.80m,
			7.85m, 7.90m, 7.95m, 8.00m, 8.05m, 8.10m, 8.15m, 8.20m, 8.25m, 8.30m,
			8.35m, 8.40m, 8.45m, 8.50m, 8.55m, 8.60m, 8.65m, 8.70m, 8.75m, 8.80m,
			8.85m, 8.90m, 8.95m, 9.00m, 9.05m, 9.10m, 9.15m, 9.20m, 9.25m, 9.30m,
			9.35m, 9.40m, 9.45m, 9.50m, 9.55m, 9.60m, 9.65m, 9.70m, 9.75m, 9.80m,
			9.85m, 9.90m, 9.95m, 10.00m
		};
		portNameLS9 = new string[50]
		{
			"Omni 1", "Omni 2", "Omni 3", "Omni 4", "Omni 5", "Omni 6", "Omni 7", "Omni 8", "Omni 9", "Omni 10",
			"Omni 11", "Omni 12", "Omni 13", "Omni 14", "Omni 15", "Omni 16", "Slot1-1", "Slot1-2", "Slot1-3", "Slot1-4",
			"Slot1-5", "Slot1-6", "Slot1-7", "Slot1-8", "Slot1-9", "Slot1-10", "Slot1-11", "Slot1-12", "Slot1-13", "Slot1-14",
			"Slot1-15", "Slot1-16", "Slot2-1", "Slot2-2", "Slot2-3", "Slot2-4", "Slot2-5", "Slot2-6", "Slot2-7", "Slot2-8",
			"Slot2-9", "Slot2-10", "Slot2-11", "Slot2-12", "Slot2-13", "Slot2-14", "Slot2-15", "Slot2-16", "2TR L", "2TR R"
		};
		portNameM7CL = new string[66]
		{
			"Omni 1", "Omni 2", "Omni 3", "Omni 4", "Omni 5", "Omni 6", "Omni 7", "Omni 8", "Omni 9", "Omni 10",
			"Omni 11", "Omni 12", "Omni 13", "Omni 14", "Omni 15", "Omni 16", "Slot1-1", "Slot1-2", "Slot1-3", "Slot1-4",
			"Slot1-5", "Slot1-6", "Slot1-7", "Slot1-8", "Slot1-9", "Slot1-10", "Slot1-11", "Slot1-12", "Slot1-13", "Slot1-14",
			"Slot1-15", "Slot1-16", "Slot2-1", "Slot2-2", "Slot2-3", "Slot2-4", "Slot2-5", "Slot2-6", "Slot2-7", "Slot2-8",
			"Slot2-9", "Slot2-10", "Slot2-11", "Slot2-12", "Slot2-13", "Slot2-14", "Slot2-15", "Slot2-16", "Slot3-1", "Slot3-2",
			"Slot3-3", "Slot3-4", "Slot3-5", "Slot3-6", "Slot3-7", "Slot3-8", "Slot3-9", "Slot3-10", "Slot3-11", "Slot3-12",
			"Slot3-13", "Slot3-14", "Slot3-15", "Slot3-16", "2TR L", "2TR R"
		};
		mapOutPatchLS9 = new string[185]
		{
			"NONE", "MIX1", "MIX2", "MIX3", "MIX4", "MIX5", "MIX6", "MIX7", "MIX8", "MIX9",
			"MIX10", "MIX11", "MIX12", "MIX13", "MIX14", "MIX15", "MIX16", "MATRIX1", "MATRIX2", "MATRIX3",
			"MATRIX4", "MATRIX5", "MATRIX6", "MATRIX7", "MATRIX8", "STL", "STR", "STC", "MONL", "MONR",
			"MONC", "DIRECT1", "DIRECT2", "DIRECT3", "DIRECT4", "DIRECT5", "DIRECT6", "DIRECT7", "DIRECT8", "DIRECT9",
			"DIRECT10", "DIRECT11", "DIRECT12", "DIRECT13", "DIRECT14", "DIRECT15", "DIRECT16", "DIRECT17", "DIRECT18", "DIRECT19",
			"DIRECT20", "DIRECT21", "DIRECT22", "DIRECT23", "DIRECT24", "DIRECT25", "DIRECT26", "DIRECT27", "DIRECT28", "DIRECT29",
			"DIRECT30", "DIRECT31", "DIRECT32", "DIRECT33", "DIRECT34", "DIRECT35", "DIRECT36", "DIRECT37", "DIRECT38", "DIRECT39",
			"DIRECT40", "DIRECT41", "DIRECT42", "DIRECT43", "DIRECT44", "DIRECT45", "DIRECT46", "DIRECT47", "DIRECT48", "DIRECT49",
			"DIRECT50", "DIRECT51", "DIRECT52", "DIRECT53", "DIRECT54", "DIRECT55", "DIRECT56", "DIRECT57", "DIRECT58", "DIRECT59",
			"DIRECT60", "DIRECT61", "DIRECT62", "DIRECT63", "DIRECT64", "INSIN1", "INSIN2", "INSIN3", "INSIN4", "INSIN5",
			"INSIN6", "INSIN7", "INSIN8", "INSIN9", "INSIN10", "INSIN11", "INSIN12", "INSIN13", "INSIN14", "INSIN15",
			"INSIN16", "INSIN17", "INSIN18", "INSIN19", "INSIN20", "INSIN21", "INSIN22", "INSIN23", "INSIN24", "INSIN25",
			"INSIN26", "INSIN27", "INSIN28", "INSIN29", "INSIN30", "INSIN31", "INSIN32", "INSMIX1", "INSMIX2", "INSMIX3",
			"INSMIX4", "INSMIX5", "INSMIX6", "INSMIX7", "INSMIX8", "INSMIX9", "INSMIX10", "INSMIX11", "INSMIX12", "INSMIX13",
			"INSMIX14", "INSMIX15", "INSMIX16", "INSMTX1", "INSMTX2", "INSMTX3", "INSMTX4", "INSMTX5", "INSMTX6", "INSMTX7",
			"INSMTX8", "INSSTL", "INSSTR", "INSSTC", "CASMIX1", "CASMIX2", "CASMIX3", "CASMIX4", "CASMIX5", "CASMIX6",
			"CASMIX7", "CASMIX8", "CASMIX9", "CASMIX10", "CASMIX11", "CASMIX12", "CASMIX13", "CASMIX14", "CASMIX15", "CASMIX16",
			"CASMTX1", "CASMTX2", "CASMTX3", "CASMTX4", "CASMTX5", "CASMTX6", "CASMTX7", "CASMTX8", "CASSTL", "CASSTR",
			"CASSTC", "CASCUEL", "CASCUER", "STL+C", "STR+C"
		};
		mapOutPatchM7CL = new string[183]
		{
			"NONE", "MIX1", "MIX2", "MIX3", "MIX4", "MIX5", "MIX6", "MIX7", "MIX8", "MIX9",
			"MIX10", "MIX11", "MIX12", "MIX13", "MIX14", "MIX15", "MIX16", "MATRIX1", "MATRIX2", "MATRIX3",
			"MATRIX4", "MATRIX5", "MATRIX6", "MATRIX7", "MATRIX8", "STL", "STR", "STC", "MONL", "MONR",
			"MONC", "DIRECT1", "DIRECT2", "DIRECT3", "DIRECT4", "DIRECT5", "DIRECT6", "DIRECT7", "DIRECT8", "DIRECT9",
			"DIRECT10", "DIRECT11", "DIRECT12", "DIRECT13", "DIRECT14", "DIRECT15", "DIRECT16", "DIRECT17", "DIRECT18", "DIRECT19",
			"DIRECT20", "DIRECT21", "DIRECT22", "DIRECT23", "DIRECT24", "DIRECT25", "DIRECT26", "DIRECT27", "DIRECT28", "DIRECT29",
			"DIRECT30", "DIRECT31", "DIRECT32", "DIRECT33", "DIRECT34", "DIRECT35", "DIRECT36", "DIRECT37", "DIRECT38", "DIRECT39",
			"DIRECT40", "DIRECT41", "DIRECT42", "DIRECT43", "DIRECT44", "DIRECT45", "DIRECT46", "DIRECT47", "DIRECT48", "INSIN1",
			"INSIN2", "INSIN3", "INSIN4", "INSIN5", "INSIN6", "INSIN7", "INSIN8", "INSIN9", "INSIN10", "INSIN11",
			"INSIN12", "INSIN13", "INSIN14", "INSIN15", "INSIN16", "INSIN17", "INSIN18", "INSIN19", "INSIN20", "INSIN21",
			"INSIN22", "INSIN23", "INSIN24", "INSIN25", "INSIN26", "INSIN27", "INSIN28", "INSIN29", "INSIN30", "INSIN31",
			"INSIN32", "INSIN33", "INSIN34", "INSIN35", "INSIN36", "INSIN37", "INSIN38", "INSIN39", "INSIN40", "INSIN41",
			"INSIN42", "INSIN43", "INSIN44", "INSIN45", "INSIN46", "INSIN47", "INSIN48", "INSMIX1", "INSMIX2", "INSMIX3",
			"INSMIX4", "INSMIX5", "INSMIX6", "INSMIX7", "INSMIX8", "INSMIX9", "INSMIX10", "INSMIX11", "INSMIX12", "INSMIX13",
			"INSMIX14", "INSMIX15", "INSMIX16", "INSMTX1", "INSMTX2", "INSMTX3", "INSMTX4", "INSMTX5", "INSMTX6", "INSMTX7",
			"INSMTX8", "INSSTL", "INSSTR", "INSSTC", "CASMIX1", "CASMIX2", "CASMIX3", "CASMIX4", "CASMIX5", "CASMIX6",
			"CASMIX7", "CASMIX8", "CASMIX9", "CASMIX10", "CASMIX11", "CASMIX12", "CASMIX13", "CASMIX14", "CASMIX15", "CASMIX16",
			"CASMTX1", "CASMTX2", "CASMTX3", "CASMTX4", "CASMTX5", "CASMTX6", "CASMTX7", "CASMTX8", "CASSTL", "CASSTR",
			"CASSTC", "CASCUEL", "CASCUER"
		};
		mapOutPatch01V96 = new string[196]
		{
			"NONE", "BUS1", "BUS2", "BUS3", "BUS4", "BUS5", "BUS6", "BUS7", "BUS8", "AUX1",
			"AUX2", "AUX3", "AUX4", "AUX5", "AUX6", "AUX7", "AUX8", "", "", "",
			"", "", "", "", "", "", "", "", "", "STEREO-L",
			"STEREO-R", "INSCH1", "INSCH2", "INSCH3", "INSCH4", "INSCH5", "INSCH6", "INSCH7", "INSCH8", "INSCH9",
			"INSCH10", "INSCH11", "INSCH12", "INSCH13", "INSCH14", "INSCH15", "INSCH16", "INSCH17", "INSCH18", "INSCH19",
			"INSCH20", "INSCH21", "INSCH22", "INSCH23", "INSCH24", "INSCH25", "INSCH26", "INSCH27", "INSCH28", "INSCH29",
			"INSCH30", "INSCH31", "INSCH32", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "INSBUS1", "INSBUS2", "INSBUS3",
			"INSBUS4", "INSBUS5", "INSBUS6", "INSBUS7", "INSBUS8", "INSAUX1", "INSAUX2", "INSAUX3", "INSAUX4", "INSAUX5",
			"INSAUX6", "INSAUX7", "INSAUX8", "", "", "", "", "", "", "",
			"", "", "", "", "", "INSSTL", "INSSTR", "", "", "",
			"", "", "", "", "", "", "", "CAS BUS1", "CAS BUS2", "CAS BUS3",
			"CAS BUS4", "CAS BUS5", "CAS BUS6", "CAS BUS7", "CAS BUS8", "CAS AUX1", "CAS AUX2", "CAS AUX3", "CAS AUX4", "CAS AUX5",
			"CAS AUX6", "CAS AUX7", "CAS AUX8", "CAS ST L", "CAS ST R", "CAS SOLO L", "CAS SOLO R", "", "", "",
			"", "", "", "", "", ""
		};
		mapInPatchLS9 = new string[85]
		{
			"NONE", "IN 1", "IN 2", "IN 3", "IN 4", "IN 5", "IN 6", "IN 7", "IN 8", "IN 9",
			"IN 10", "IN 11", "IN 12", "IN 13", "IN 14", "IN 15", "IN 16", "IN 17", "IN 18", "IN 19",
			"IN 20", "IN 21", "IN 22", "IN 23", "IN 24", "IN 25", "IN 26", "IN 27", "IN 28", "IN 29",
			"IN 30", "IN 31", "IN 32", "SL 1-1", "SL 1-2", "SL 1-3", "SL 1-4", "SL 1-5", "SL 1-6", "SL 1-7",
			"SL 1-8", "SL 1-9", "SL 1-10", "SL 1-11", "SL 1-12", "SL 1-13", "SL 1-14", "SL 1-15", "SL 1-16", "SL 2-1",
			"SL 2-2", "SL 2-3", "SL 2-4", "SL 2-5", "SL 2-6", "SL 2-7", "SL 2-8", "SL 2-9", "SL 2-10", "SL 2-11",
			"SL 2-12", "SL 2-13", "SL 2-14", "SL 2-15", "SL 2-16", "RACK1-1", "RACK1-2", "RACK2-1", "RACK2-2", "RACK3-1",
			"RACK3-2", "RACK4-1", "RACK4-2", "RACK5-1", "RACK5-2", "RACK6-1", "RACK6-2", "RACK7-1", "RACK7-2", "RACK8-1",
			"RACK8-2", "2TR IN L", "2TR IN R", "MP3 PB L", "MP3 PB R"
		};
		mapInPatchM7CL = new string[121]
		{
			"NONE", "IN 1", "IN 2", "IN 3", "IN 4", "IN 5", "IN 6", "IN 7", "IN 8", "IN 9",
			"IN 10", "IN 11", "IN 12", "IN 13", "IN 14", "IN 15", "IN 16", "IN 17", "IN 18", "IN 19",
			"IN 20", "IN 21", "IN 22", "IN 23", "IN 24", "IN 25", "IN 26", "IN 27", "IN 28", "IN 29",
			"IN 30", "IN 31", "IN 32", "IN 33", "IN 34", "IN 35", "IN 36", "IN 37", "IN 38", "IN 39",
			"IN 40", "IN 41", "IN 42", "IN 43", "IN 44", "IN 45", "IN 46", "IN 47", "IN 48", "ST IN1 L",
			"ST IN1 R", "ST IN2 L", "ST IN2 R", "ST IN3 L", "ST IN3 R", "ST IN4 L", "ST IN4 R", "SL 1-1", "SL 1-2", "SL 1-3",
			"SL 1-4", "SL 1-5", "SL 1-6", "SL 1-7", "SL 1-8", "SL 1-9", "SL 1-10", "SL 1-11", "SL 1-12", "SL 1-13",
			"SL 1-14", "SL 1-15", "SL 1-16", "SL 2-1", "SL 2-2", "SL 2-3", "SL 2-4", "SL 2-5", "SL 2-6", "SL 2-7",
			"SL 2-8", "SL 2-9", "SL 2-10", "SL 2-11", "SL 2-12", "SL 2-13", "SL 2-14", "SL 2-15", "SL 2-16", "SL 3-1",
			"SL 3-2", "SL 3-3", "SL 3-4", "SL 3-5", "SL 3-6", "SL 3-7", "SL 3-8", "SL 3-9", "SL 3-10", "SL 3-11",
			"SL 3-12", "SL 3-13", "SL 3-14", "SL 3-15", "SL 3-16", "RACK1-1", "RACK1-2", "RACK2-1", "RACK2-2", "RACK3-1",
			"RACK3-2", "RACK4-1", "RACK4-2", "RACK5-1", "RACK5-2", "RACK6-1", "RACK6-2", "RACK7-1", "RACK7-2", "RACK8-1",
			"RACK8-2"
		};
		mapInPatch01V96 = new string[179]
		{
			"NONE", "AD1", "AD2", "AD3", "AD4", "AD5", "AD6", "AD7", "AD8", "AD9",
			"AD10", "AD11", "AD12", "AD13", "AD14", "AD15", "AD16", "", "", "",
			"", "", "", "", "", "S1-1", "S1-2", "S1-3", "S1-4", "S1-5",
			"S1-6", "S1-7", "S1-8", "S1-9", "S1-10", "S1-11", "S1-12", "S1-13", "S1-14", "S1-15",
			"S1-16", "ADAT1", "ADAT2", "ADAT3", "ADAT4", "ADAT5", "ADAT6", "ADAT7", "ADAT8", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "FX1-1", "FX1-2", "", "", "", "", "", "", "FX2-1",
			"FX2-2", "", "", "", "", "", "", "FX3-1", "FX3-2", "FX4-1",
			"FX4-2", "", "", "", "", "", "", "", "", "2TD1L",
			"2TD1R", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "", ""
		};
		mapOutPatch = mapOutPatchLS9;
		mapInPatch = mapInPatchLS9;
		offset1 = new int[256]
		{
			8, 13, 167, 180, 196, 59, 125, 221, 191, 66,
			148, 226, 88, 84, 147, 32, 70, 197, 116, 210,
			97, 225, 160, 215, 90, 218, 253, 175, 91, 38,
			54, 154, 129, 55, 30, 192, 76, 15, 184, 45,
			217, 234, 149, 41, 132, 48, 99, 105, 35, 7,
			166, 224, 60, 178, 195, 140, 255, 118, 61, 256,
			36, 164, 181, 182, 68, 103, 146, 216, 69, 204,
			114, 57, 254, 113, 235, 219, 124, 158, 43, 203,
			40, 207, 205, 139, 247, 117, 126, 137, 119, 258,
			26, 87, 134, 232, 155, 242, 150, 172, 233, 22,
			58, 238, 246, 179, 171, 213, 222, 243, 96, 93,
			100, 64, 18, 141, 173, 46, 11, 31, 20, 106,
			206, 85, 53, 251, 168, 230, 194, 101, 89, 187,
			102, 177, 77, 12, 239, 135, 108, 10, 245, 86,
			95, 63, 170, 115, 138, 110, 241, 159, 52, 56,
			145, 201, 17, 104, 220, 152, 74, 143, 162, 259,
			51, 81, 123, 188, 144, 42, 157, 153, 82, 75,
			209, 83, 71, 94, 73, 165, 34, 25, 211, 80,
			72, 250, 240, 248, 237, 214, 27, 24, 65, 228,
			29, 183, 199, 227, 127, 229, 107, 5, 109, 128,
			39, 257, 193, 252, 9, 131, 50, 142, 67, 33,
			223, 111, 78, 189, 212, 133, 92, 161, 121, 47,
			44, 14, 176, 244, 169, 186, 122, 120, 21, 136,
			163, 19, 249, 151, 156, 260, 23, 16, 202, 79,
			8, 130, 174, 200, 198, 62, 37, 6, 236, 112,
			28, 185, 231, 190, 49, 98
		};
		outDevice = null;
		inDevice = null;
		inQueue = new Queue<byte[]>();
		inWait = new ManualResetEvent(initialState: false);
		connStatus = ConnectionStatus.Disconnected;
		connPhase = ConnectionPhase.Closed;
		MIDIdeviceID = 0;
		licenseClients = UNLOCKED_CLIENT_LIMIT;
		maxAndroidConnections = 0;
		maxIOSConnections = 0;
		chOffsetInput = 0;
		chOffsetSTIN = 64;
		chOffsetMix = 72;
		chOffsetMatrix = 88;
		chOffsetStereo = 96;
		chOffsetMonitor = 99;
		dumpData = "";
		xStereoMeter = 0;
		xMeterSample = 0;
		xQ = 0;
		xF = 4;
		sendQueue = new Queue<queueItem>();
		sendFlag = new ManualResetEvent(initialState: false);
		chData = new Channel[100];
		chGroup = new Group[41];
		muteGroup = new MuteGroup[8];
		DCA = new DCAGroup[8];
		sceneData = new Scene[301];
		RackData = new Rack[16];
		GEQData = new GEQ[16];
		EFXData = new EFX[4];
		PEQCopy = new PEQ();
		OutPort = new OutPort[66];
		HA = new int[99];
		Phantom = new int[99];
		serialRetry = 0;
		sysExLock = new object();
		meterCount = 0;
		reqQueue = new List<reqQueueItem>();
		channelHold = -1;
		logLock = new object();
		logIsOK = true;
		for (int i = 0; i < chData.Length; i++)
		{
			chData[i] = new Channel(i, ChannelTypes.Input);
		}
		for (int j = 0; j < 41; j++)
		{
			chGroup[j] = new Group();
		}
		for (int k = 0; k < 8; k++)
		{
			muteGroup[k] = new MuteGroup(k);
		}
		for (int l = 0; l < 8; l++)
		{
			DCA[l] = new DCAGroup(l);
		}
		chData[96].setParam("LOGIC", Channel.Pr.Group, 0, 40);
		chData[97].setParam("LOGIC", Channel.Pr.Group, 0, 40);
		for (int m = 0; m < 301; m++)
		{
			sceneData[m] = new Scene(m);
		}
		for (int n = 0; n < 16; n++)
		{
			GEQData[n] = new GEQ(n);
		}
		for (int num = 0; num < 16; num++)
		{
			RackData[num] = new Rack(num);
			if (num < 4)
			{
				RackData[num].Type = 1;
			}
			else
			{
				RackData[num].Type = 3;
			}
		}
		for (int num2 = 0; num2 < 4; num2++)
		{
			EFXData[num2] = new EFX(num2);
		}
		EFXData[0].setEFXType(0);
		EFXData[1].setEFXType(1);
		EFXData[2].setEFXType(3);
		EFXData[3].setEFXType(7);
		for (int num3 = 0; num3 < 99; num3++)
		{
			HA[num3] = 0;
			Phantom[num3] = 0;
		}
		for (int num4 = 0; num4 < 16; num4++)
		{
			OutPort[num4] = new OutPort(num4, OutportTypes.Omni);
		}
		for (int num5 = 16; num5 < 64; num5++)
		{
			OutPort[num5] = new OutPort(num5, OutportTypes.Slot);
		}
		for (int num6 = 64; num6 < 66; num6++)
		{
			OutPort[num6] = new OutPort(num6, OutportTypes.TwoTR);
		}
		initParams();
	}

	public static void setConsoleType(string consoleType)
	{
		logDebug("Setting ConsoleType: " + consoleType);
		switch (consoleType)
		{
		case "Yamaha LS9-32":
			if (Console.type == ConsoleTypes.LS9 && Console.maxInputs == 72)
			{
				OnAction("MasterSection", 0);
				return;
			}
			Console.type = ConsoleTypes.LS9;
			Console.maxInputs = 72;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 32;
			Console.maxMIX = 16;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 88;
			chOffsetMix = 72;
			chOffsetStereo = 96;
			chOffsetSTIN = 64;
			chOffsetMonitor = 99;
			mapInPatch = mapInPatchLS9;
			mapOutPatch = mapOutPatchLS9;
			break;
		case "Yamaha LS9-16":
			if (Console.type == ConsoleTypes.LS9 && Console.maxInputs == 40)
			{
				OnAction("MasterSection", 0);
				return;
			}
			Console.type = ConsoleTypes.LS9;
			Console.maxInputs = 40;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 16;
			Console.maxMIX = 16;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 88;
			chOffsetMix = 72;
			chOffsetStereo = 96;
			chOffsetSTIN = 64;
			chOffsetMonitor = 99;
			mapInPatch = mapInPatchLS9;
			mapOutPatch = mapOutPatchLS9;
			break;
		case "Yamaha M7CL-32":
			if (Console.type == ConsoleTypes.M7CL && Console.maxInputs == 40)
			{
				OnAction("MasterSection", 0);
				return;
			}
			Console.type = ConsoleTypes.M7CL;
			Console.maxInputs = 40;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 48;
			Console.maxMIX = 16;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 72;
			chOffsetMix = 56;
			chOffsetStereo = 80;
			chOffsetMonitor = 83;
			chOffsetSTIN = 48;
			mapInPatch = mapInPatchM7CL;
			mapOutPatch = mapOutPatchM7CL;
			break;
		case "Yamaha M7CL-48":
			if (Console.type == ConsoleTypes.M7CL && Console.maxInputs == 56)
			{
				OnAction("MasterSection", 0);
				return;
			}
			Console.type = ConsoleTypes.M7CL;
			Console.maxInputs = 56;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 48;
			Console.maxMIX = 16;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 72;
			chOffsetMix = 56;
			chOffsetStereo = 80;
			chOffsetSTIN = 48;
			chOffsetMonitor = 83;
			mapInPatch = mapInPatchM7CL;
			mapOutPatch = mapOutPatchM7CL;
			break;
		case "Yamaha 01V96":
			if (Console.type == ConsoleTypes.O1V96V2 && Console.ver == ConsoleVersions.O1V96V2 && Console.maxInputs == 40)
			{
				OnAction("MasterSection", 0);
				return;
			}
			Console.type = ConsoleTypes.O1V96V2;
			Console.ver = ConsoleVersions.O1V96V2;
			Console.maxInputs = 40;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 16;
			Console.maxMIX = 8;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 48;
			chOffsetMix = 40;
			chOffsetStereo = 56;
			chOffsetSTIN = 32;
			chOffsetMonitor = 59;
			mapInPatch = mapInPatch01V96;
			mapOutPatch = mapOutPatch01V96;
			break;
		case "Yamaha 01V96i":
			if (Console.type == ConsoleTypes.O1V96V2 && Console.ver == ConsoleVersions.O1V96i && Console.maxInputs == 40)
			{
				OnAction("MasterSection", 0);
				return;
			}
			Console.type = ConsoleTypes.O1V96V2;
			Console.ver = ConsoleVersions.O1V96i;
			Console.maxInputs = 40;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 16;
			Console.maxMIX = 8;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 48;
			chOffsetMix = 40;
			chOffsetStereo = 56;
			chOffsetSTIN = 32;
			chOffsetMonitor = 59;
			mapInPatch = mapInPatch01V96;
			mapOutPatch = mapOutPatch01V96;
			break;
		default:
			Console.type = ConsoleTypes.LS9;
			Console.maxInputs = 72;
			Console.maxSTIN = 8;
			Console.maxOutSlot = 32;
			Console.maxMIX = 16;
			Console.maxMATRIX = 8;
			chOffsetInput = 0;
			chOffsetMatrix = 88;
			chOffsetMix = 72;
			chOffsetStereo = 96;
			chOffsetSTIN = 64;
			chOffsetMonitor = 99;
			mapInPatch = mapInPatchLS9;
			mapOutPatch = mapOutPatchLS9;
			break;
		}
		for (int i = 0; i < chData.Length; i++)
		{
			chData[i].channelType = ChannelTypes.None;
			chData[i].initParams();
		}
		for (int j = chOffsetInput; j < Console.maxInputs - Console.maxSTIN; j++)
		{
			chData[j].setChannelType(ChannelTypes.Input);
		}
		for (int k = chOffsetSTIN; k < chOffsetSTIN + Console.maxSTIN; k++)
		{
			chData[k].setChannelType(ChannelTypes.STIN);
		}
		for (int l = chOffsetMix; l < chOffsetMix + Console.maxMIX; l++)
		{
			chData[l].setChannelType(ChannelTypes.Mix);
		}
		for (int m = chOffsetMatrix; m < chOffsetMatrix + Console.maxMATRIX; m++)
		{
			chData[m].setChannelType(ChannelTypes.Matrix);
		}
		for (int n = chOffsetStereo; n < chOffsetStereo + 3; n++)
		{
			chData[n].setChannelType(ChannelTypes.Stereo);
		}
		chData[chOffsetMonitor].setChannelType(ChannelTypes.Monitor);
		OnAction("ConsoleType", 0);
		logDebug("ConsoleType: " + Console.type.ToString() + ", Ver: " + Console.ver);
	}

	public static void setOutPortParam(string source, OutPort.Pr param, int index, int value)
	{
		if (index >= 0 && index < OutPort.Length)
		{
			OutPort[index].setParam(source, param, value);
		}
	}

	public static void setOutPortParam(string source, OutPort.Pr param, int index, bool value)
	{
		if (index >= 0 && index < OutPort.Length)
		{
			OutPort[index].setParam(source, param, value);
		}
	}

	private static string base32(ulong num, int digits)
	{
		try
		{
			string text = "0123456789ABCDEFGHXJKYMNWPQRZTUV";
			uint num2 = 12u;
			ulong num3 = 0uL;
			char[] array = new char[num2 + 1];
			do
			{
				num3 = num % 32;
				array[num2] = text[(int)num3];
				num = (num - num3) / 32;
				num2--;
			}
			while (num != 0);
			char[] array2 = new char[1];
			char[] trimChars = array2;
			string text2 = new string('0', digits) + new string(array).Trim(trimChars);
			return text2.Substring(text2.Length - digits);
		}
		catch (Exception)
		{
			Loc.Msg("Problem in Base10to32()", "Static Error");
		}
		return "";
	}

	public static ulong base10(string base32String)
	{
		try
		{
			string text = "0123456789ABCDEFGHXJKYMNWPQRZTUV";
			base32String = base32String.ToUpper();
			int num = 0;
			int num2 = 0;
			ulong num3 = 0uL;
			do
			{
				num2 = text.IndexOf(base32String[num]);
				num3 = num3 * 32 + (uint)num2;
				num++;
			}
			while (num < base32String.Length);
			return num3;
		}
		catch (Exception)
		{
			Loc.Msg("Problem in Base10()", "Static Error");
		}
		return 0uL;
	}

	private static string genKeyV2(string name, int version, int server, int time, int rand, bool ignoreCase)
	{
		try
		{
			name = Regex.Replace(name, "[^a-zA-Z0-9 ]", "");
			name = name.Trim();
			if (ignoreCase)
			{
				name = name.ToUpper();
			}
			int num = server * 16 + version;
			string s = "AF" + base32((ulong)num, 2) + name + base32((ulong)time, 4);
			ulong num2 = 0uL;
			ulong num3 = 0uL;
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			foreach (byte b in bytes)
			{
				num2 += (ulong)((long)(++num3) * (long)offset1[b]);
			}
			ulong num4 = (ulong)rand ^ num2;
			string text = "AF" + base32((ulong)num, 2) + "-" + base32(num4, 4) + "-" + base32((ulong)time, 4) + "-" + base32((ulong)rand, 4);
			return text.ToUpper();
		}
		catch (Exception ex)
		{
			logError("genKeyV2()", "Math", ex.ToString());
		}
		return "";
	}

	public static string getLicenseKey()
	{
		if (!LICENSING_ENABLED)
		{
			return OPEN_SOURCE_KEY;
		}
		if (Settings.Default.GlobalKey.Length > 0)
		{
			return Settings.Default.GlobalKey;
		}
		return Settings.Default.LicenseKey;
	}

	public static string getLicenseUser()
	{
		if (!LICENSING_ENABLED)
		{
			return OPEN_SOURCE_OWNER;
		}
		if (Settings.Default.GlobalUser.Length > 0)
		{
			return Settings.Default.GlobalUser;
		}
		return Settings.Default.LicenseUser;
	}

	public static void updateAppConfig(string key, string value)
	{
		Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		ClientSettingsSection clientSettingsSection = (ClientSettingsSection)configuration.SectionGroups["applicationSettings"].Sections["AirFader.Properties.Settings"];
		SettingElement settingElement = clientSettingsSection.Settings.Get(key);
		if (settingElement != null)
		{
			clientSettingsSection.Settings.Remove(settingElement);
			settingElement.Value.ValueXml.InnerXml = value;
			clientSettingsSection.Settings.Add(settingElement);
		}
		else
		{
			settingElement = new SettingElement(key, SettingsSerializeAs.String);
			settingElement.Value = new SettingValueElement();
			XmlDocument xmlDocument = new XmlDocument();
			settingElement.Value.ValueXml = xmlDocument.CreateElement("value");
			settingElement.Value.ValueXml.InnerXml = value;
			clientSettingsSection.Settings.Add(settingElement);
		}
		configuration.Save(ConfigurationSaveMode.Modified);
		ConfigurationManager.RefreshSection("applicationSettings");
		Settings.Default.Reload();
	}

	public static bool checkSerialV2(string name, string key)
	{
		return checkSerialV2(name, key, quiet: false);
	}

	public static bool checkSerialV2(string name, string key, bool quiet)
	{
		if (!LICENSING_ENABLED)
		{
			licenseClients = UNLOCKED_CLIENT_LIMIT;
			return true;
		}
		try
		{
			if (serialRetry >= 10)
			{
				return false;
			}
			name = name.Trim();
			key = key.Trim().ToUpper();
			key = key.Replace("O", "0");
			key = key.Replace("I", "1");
			key = key.Replace("L", "1");
			key = key.Replace("S", "5");
			string[] array = key.Split(new char[1] { '-' }, 4);
			if (array.Length != 4)
			{
				return false;
			}
			if (key.Substring(0, 4).ToUpper() == "AF01")
			{
				if (!quiet)
				{
					frmConfirm.sayFail("This key is for a previous version\nPlease visit http://www.airfader.com/upgrade for a free exchange!", "Previous Version", hMainWindow);
				}
				return false;
			}
			int rand = (int)base10(array[3]);
			int num = (int)base10(array[2]);
			int num2 = (int)base10(array[0].Substring(2, 2));
			int num3 = num2 & 0xF;
			int num4 = (num2 & 0x10) / 16;
			string text = genKeyV2(name, num3, num4, num, rand, ignoreCase: false);
			if (text != key)
			{
				text = genKeyV2(name, num3, num4, num, rand, ignoreCase: true);
			}
			if (text == key)
			{
				if (num3 < 2)
				{
					if (!quiet)
					{
						frmConfirm.sayFail("Sorry, this key is for a previous version\nPlease visit http://www.airfader.com/upgrade for a free exchange!", "Previous Version", hMainWindow);
					}
					return false;
				}
				if (num4 == 1)
				{
					licenseClients = 99;
				}
				else
				{
					licenseClients = 1;
				}
				DateTime dateTime = new DateTime(2011, 1, 1);
				TimeSpan timeSpan = DateTime.Now - dateTime;
				if (num == 36500)
				{
					return true;
				}
				if (Settings.Default.Expired)
				{
					if (!quiet)
					{
						frmConfirm.sayFail("Thanks for trying 01vmix!\nYour trial period has ended.\nPlease visit http://www.airfader.com to purchase", "Trial Ended", hMainWindow);
					}
					serialRetry++;
					return false;
				}
				if (num < timeSpan.Days)
				{
					if (!quiet)
					{
						frmConfirm.sayFail("Thanks for trying 01vmix!\nThis trial license key has expired.\nVisit http://www.airfader.com", "Trial Expired", hMainWindow);
					}
					Settings.Default.Expired = true;
					Settings.Default.Save();
					serialRetry++;
					return false;
				}
				DateTime dateTime2 = dateTime.AddDays(num);
				if (!quiet)
				{
					frmConfirm.sayOK("This trial expires " + dateTime2.ToShortDateString() + ".\nTo purchase, visit http://www.airfader.com", "Trial Mode", hMainWindow);
				}
				return true;
			}
			serialRetry++;
			return false;
		}
		catch (Exception ex)
		{
			logError("CheckSerial()", "Math", ex.ToString());
		}
		return false;
	}

	public static void doConnect(object state)
	{
		ConnParams connParams = (ConnParams)state;
		int num = connectConsole(connParams.inputName, connParams.outputName);
		int num2 = num;
		if (num2 == 1)
		{
			connPhase = ConnectionPhase.PreSync;
		}
	}

	public static void enQueue(Channel.Pr newParam, int newChannel, int newIndex)
	{
		lock (sendQueue)
		{
			queueItem item = new queueItem(newParam, newChannel, newIndex);
			if (!sendQueue.Contains(item))
			{
				sendQueue.Enqueue(new queueItem(newParam, newChannel, newIndex));
				sendFlag.Set();
			}
		}
	}

	public static void enQueue(Pr newParam, int newChannel, int newIndex)
	{
		lock (sendQueue)
		{
			queueItem item = new queueItem(newParam, newChannel, newIndex);
			if (!sendQueue.Contains(item))
			{
				sendQueue.Enqueue(new queueItem(newParam, newChannel, newIndex));
				sendFlag.Set();
			}
		}
	}

	public static void queueRunner(object state)
	{
		byte[] array = null;
		string text = "";
		bool flag = false;
		WaitHandle[] waitHandles = new WaitHandle[2] { inWait, termFlag };
		while (WaitHandle.WaitAny(waitHandles) != 1)
		{
			flag = true;
			while (flag)
			{
				lock (inQueue)
				{
					if (inQueue.Count > 0)
					{
						array = inQueue.Dequeue();
					}
					else
					{
						flag = false;
						inWait.Reset();
						array = null;
					}
				}
				if (array != null)
				{
					text = "";
					byte[] array2 = array;
					foreach (byte b in array2)
					{
						text += $"{b:X2}";
					}
					processSysEx(text);
					array = null;
				}
			}
		}
	}

	public static int connectConsole(string inputName, string outputName)
	{
		int num = -1;
		int num2 = -1;
		bool flag = false;
		try
		{
			logDebug("ConnectConsole called...");
			sceneNameQueue.Clear();
			if (string.IsNullOrEmpty(inputName) || string.IsNullOrEmpty(outputName))
			{
				return -1;
			}
			if (connStatus == ConnectionStatus.Connected)
			{
				disconnectConsole();
			}
			for (int i = 0; i < InputDevice.DeviceCount; i++)
			{
				if (InputDevice.GetDeviceCapabilities(i).name == inputName)
				{
					num = i;
					i = InputDevice.DeviceCount;
				}
			}
			for (int j = 0; j < OutputDeviceBase.DeviceCount; j++)
			{
				if (OutputDeviceBase.GetDeviceCapabilities(j).name == outputName)
				{
					num2 = j;
					j = OutputDeviceBase.DeviceCount;
				}
			}
			if (num == -1 || num2 == -1)
			{
				return -1;
			}
			try
			{
				logDebug("Creating InputDevice on " + num);
				inDevice = new InputDevice(num);
				logDebug("Device Created.");
				inDevice.SysExMessageReceived += HandleSysExMessageReceived;
				inDevice.Error += inDevice_Error;
			}
			catch (Exception ex2)
			{
				try
				{
					if (inDevice != null)
					{
						inDevice.SysExMessageReceived -= HandleSysExMessageReceived;
						inDevice.Error -= inDevice_Error;
						inDevice.StopRecording();
						inDevice.Close();
						inDevice = null;
					}
				}
				catch (Exception ex)
				{
					logDebug("Conn Error: When cleaning up input device:\n" + ex.ToString());
				}
				if (ex2.Message.ToString() == "Undefined external error.")
				{
					logDebug("Conn Error: Undefined external error when creating input device:\n" + ex2.ToString());
					return -2;
				}
				logDebug("Conn Error: When creating input device:\n" + ex2.ToString());
				return -2;
			}
			try
			{
				if (flag)
				{
					Loc.Msg("Creating OutputDevice on " + num2);
				}
				logDebug("Creating OutputDevice on " + num2);
				outDevice = new OutputDevice(num2);
				logDebug("Device Created.");
			}
			catch (OutputDeviceException ex4)
			{
				try
				{
					if (inDevice != null)
					{
						inDevice.SysExMessageReceived -= HandleSysExMessageReceived;
						inDevice.Error -= inDevice_Error;
						inDevice.StopRecording();
						inDevice.Close();
						inDevice = null;
					}
					if (outDevice != null)
					{
						outDevice.Close();
						outDevice = null;
					}
				}
				catch (Exception ex3)
				{
					logDebug("Conn Error: When cleaning up output device:\n" + ex3.ToString());
				}
				if (ex4.Message.ToString() == "Undefined external error.")
				{
					logDebug("Conn Error: Undefined external error when creating output device:\n" + ex4.ToString());
					return -2;
				}
				logDebug("Conn Error: When creating output device:\n" + ex4.ToString());
				return -2;
			}
			try
			{
				if (flag)
				{
					Loc.Msg("Starting InputDevice");
				}
				inDevice.StartRecording();
			}
			catch (Exception ex5)
			{
				logDebug("Conn Error: When starting input device stream:\n" + ex5.ToString());
				disconnectConsole();
				return -2;
			}
			updateChannel.Clear();
			updateRack.Clear();
			inQueue.Clear();
			sendQueue.Clear();
			heartBeat = new System.Timers.Timer();
			heartBeat.Interval = 50.0;
			heartBeat.AutoReset = false;
			heartBeat.Elapsed += heartBeat_Elapsed;
			heartBeat.Enabled = true;
			heartBeats = 0;
			connStatus = ConnectionStatus.Connected;
			if (commThread == null)
			{
				commThread = new Thread(queueRunner);
				commThread.Start();
			}
			if (sendThread == null)
			{
				sendThread = new Thread(sendQueueRunner);
				sendThread.Start();
			}
			GenMessage genMessage = new GenMessage();
			genMessage.msgtype = MsgType.MSG_CONSOLE_ONLINE;
			genMessage.message = "Console connected to server.";
			faderServer.sendProto("LOGIC", genMessage);
			dtConnStart = DateTime.Now;
			reqParam("StopMeter", 0, 0);
		}
		catch (Exception ex6)
		{
			logError("Connect()", "UI", ex6.ToString());
		}
		return 1;
	}

	public static void sendQueueRunner(object state)
	{
		queueItem queueItem = null;
		bool flag = false;
		WaitHandle[] waitHandles = new WaitHandle[2] { sendFlag, termFlag };
		while (WaitHandle.WaitAny(waitHandles) != 1)
		{
			flag = true;
			while (flag)
			{
				lock (sendQueue)
				{
					if (sendQueue.Count > 0)
					{
						queueItem = sendQueue.Dequeue();
					}
					else
					{
						flag = false;
						sendFlag.Reset();
						queueItem = null;
					}
				}
				if (queueItem == null || queueItem.channel < 0)
				{
					continue;
				}
				switch (queueItem.type)
				{
				case queueItem.msgType.Channel:
					if (queueItem.channelParam == Channel.Pr.Name)
					{
						sendParam(queueItem.channelParam, queueItem.channel, queueItem.index, chData[queueItem.channel].name);
					}
					else
					{
						sendParam(queueItem.channelParam, queueItem.channel, queueItem.index, chData[queueItem.channel].getParam(queueItem.channelParam, queueItem.index));
					}
					break;
				case queueItem.msgType.Engine:
					switch (queueItem.engineParam)
					{
					case Pr.MuteMaster:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, muteGroup[queueItem.channel].muted ? 1 : 0);
						break;
					case Pr.GEQBand:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, GEQData[queueItem.channel]._Band[queueItem.index]);
						break;
					case Pr.GEQFlat:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, 1);
						break;
					case Pr.GEQLink:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, GEQData[queueItem.channel]._link);
						break;
					case Pr.GEQLinkLR:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, GEQData[queueItem.channel]._linkLR);
						break;
					case Pr.GEQOn:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, GEQData[queueItem.channel]._on);
						break;
					case Pr.GEQType:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, GEQData[queueItem.channel]._type);
						break;
					case Pr.Phantom:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, Phantom[queueItem.channel]);
						break;
					case Pr.HA:
						sendParam(queueItem.engineParam, queueItem.channel, queueItem.index, HA[queueItem.channel]);
						break;
					case Pr.SceneSave:
						setParam("SaveScene", queueItem.channel, sceneData[queueItem.channel].sceneName);
						break;
					case Pr.SceneDump:
						reqParam("SceneDump", 0, 0);
						Thread.Sleep(200);
						break;
					case Pr.SceneName:
						setParam("SceneName", queueItem.channel, sceneData[queueItem.channel].sceneName);
						break;
					default:
						logDebug("Unknown Engine Param in sendQueue: " + queueItem.engineParam);
						break;
					case Pr.RackSource:
					case Pr.SceneLoad:
						break;
					}
					break;
				}
				Thread.Sleep(30);
			}
		}
	}

	private static void heartBeat_Elapsed(object sender, ElapsedEventArgs e)
	{
		try
		{
			if (updateRack.Count != 0)
			{
				int[] array = updateRack.ToArray();
				updateRack.Clear();
				int[] array2 = array;
				foreach (int num in array2)
				{
					EFX.Pr[] array3 = EFXData[num].changedParams.ToArray();
					EFXData[num].changedParams.Clear();
					int[] array4 = EFXData[num].changedIndexes.ToArray();
					EFXData[num].changedIndexes.Clear();
					EFX.Pr[] array5 = array3;
					foreach (EFX.Pr pr in array5)
					{
						switch (pr)
						{
						case EFX.Pr.BPM:
							setParam("EFX.BPM", num, EFXData[num].bpm);
							break;
						case EFX.Pr.Bypass:
							setParam("EFX.BYPASS", num, EFXData[num]._bypass ? 1 : 0);
							break;
						case EFX.Pr.Mix:
							setParam("EFX.MIX", num, EFXData[num]._mix);
							break;
						case EFX.Pr.Type:
							setParam("EFX.TYPE", num, EFXData[num]._type);
							break;
						case EFX.Pr.Param:
						{
							int[] array6 = array4;
							foreach (int num2 in array6)
							{
								sendParam(pr, num, num2, EFXData[num].pValue[num2]);
							}
							break;
						}
						}
					}
				}
			}
			if (reqQueue.Count > 0 && reqQueue.FindAll((reqQueueItem item) => item.timeSent > DateTime.Now.Ticks - 20000000).Count < 2)
			{
				foreach (reqQueueItem item in reqQueue.FindAll((reqQueueItem item) => item.sent))
				{
					if (connStatus == ConnectionStatus.Disconnected)
					{
						continue;
					}
					if (item.type == reqQueueItem.msgType.Channel)
					{
						logDebug("Waiting " + (DateTime.Now.Ticks - item.timeSent) / 10000000 + " seconds for " + item.channelParam.ToString() + " for channel " + item.channel);
					}
					else if (item.type == reqQueueItem.msgType.Engine)
					{
						logDebug("Waiting " + (DateTime.Now.Ticks - item.timeSent) / 10000000 + " seconds for " + item.engineParam.ToString() + " for engine " + item.channel);
					}
					if (item.timeSent < DateTime.Now.Ticks - 50000000)
					{
						if (item.type == reqQueueItem.msgType.Channel)
						{
							logDebug("GIVING UP ON " + item.channelParam.ToString() + " for channel " + item.channel);
							logError("Sync", "Sync", "I'm missing parameters from the sync (" + item.channelParam.ToString() + "). Check communication and try again.");
						}
						else if (item.type == reqQueueItem.msgType.Engine)
						{
							logDebug("GIVING UP ON " + item.engineParam.ToString() + " for engine " + item.channel);
							logError("Sync", "Sync", "I'm missing parameters from the sync (" + item.engineParam.ToString() + "). Check communication and try again.");
						}
						reqQueue.Remove(item);
					}
				}
				reqQueueItem nextItem = reqQueue.Find((reqQueueItem item) => !item.sent);
				if (nextItem != null)
				{
					if (nextItem.type == reqQueueItem.msgType.Channel)
					{
						logDebug("Sync Channel " + nextItem.channelParam.ToString() + "...");
						MixEngine.NewMessage("Sync " + nextItem.channelParam, 0);
						reqParam(nextItem.channelParam, nextItem.index, nextItem.channel);
						nextItem.sent = true;
						nextItem.timeSent = DateTime.Now.Ticks;
					}
					if (nextItem.type == reqQueueItem.msgType.Engine)
					{
						logDebug("Sync Engine " + nextItem.engineParam.ToString() + "...");
						MixEngine.NewMessage("Sync " + nextItem.engineParam, 0);
						reqParam(nextItem.engineParam, nextItem.index, nextItem.channel);
						nextItem.sent = true;
						nextItem.timeSent = DateTime.Now.Ticks;
					}
				}
			}
			heartBeats++;
			if (heartBeats > 120 && connStatus == ConnectionStatus.Connected)
			{
				connStatus = ConnectionStatus.Bad;
				MixEngine.NewMessage("Connection Bad...", -97);
				logDebug("Connection Bad due to " + heartBeats + " heartbeats missed");
			}
			else if (heartBeats > 300 && connStatus == ConnectionStatus.Bad)
			{
				MixEngine.NewMessage("Connection Lost!", -99);
				logDebug("Connection Lost due to " + heartBeats + " heartbeats missed");
				heartBeats = 0;
				disconnectConsole();
			}
			if (heartBeat != null)
			{
				heartBeat.Interval = 50.0;
				if (!heartBeat.Enabled)
				{
					heartBeat.Enabled = true;
				}
			}
		}
		catch (Exception ex)
		{
			logDebug(ex.Message);
		}
	}

	public static bool disconnectConsole()
	{
		logDebug("Disconnecting from console...");
		if (heartBeat != null)
		{
			try
			{
				heartBeat.Dispose();
			}
			catch (Exception)
			{
				heartBeat = null;
			}
			heartBeat = null;
		}
		GenMessage genMessage = new GenMessage();
		genMessage.msgtype = MsgType.MSG_CONSOLE_OFFLINE;
		genMessage.message = "Console disconnected from server.";
		faderServer.sendProto("LOGIC", genMessage);
		updateChannel.Clear();
		updateRack.Clear();
		inQueue.Clear();
		sendQueue.Clear();
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			folderPath += "\\01vmix";
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}
			string text = folderPath + "\\Conn.Log";
			TextWriter textWriter;
			if (File.Exists(text))
			{
				textWriter = new StreamWriter(text, append: true);
			}
			else
			{
				textWriter = new StreamWriter(text, append: true);
				textWriter.WriteLine("LogTime,01vmix,Key,Console,Version,ConnDuration,MaxAndroid,MaxIOS");
			}
			textWriter.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			textWriter.Write(",");
			textWriter.Write(Application.ProductVersion);
			textWriter.Write(",");
			textWriter.Write(getLicenseKey());
			textWriter.Write(",");
			textWriter.Write(Console.type.ToString());
			textWriter.Write(",");
			textWriter.Write(Console.ver.ToString());
			textWriter.Write(",");
			TimeSpan timeSpan = DateTime.Now.Subtract(dtConnStart);
			textWriter.Write($"{timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}");
			textWriter.Write(",");
			textWriter.Write(maxAndroidConnections.ToString());
			textWriter.Write(",");
			textWriter.Write(maxIOSConnections.ToString());
			textWriter.WriteLine("");
			textWriter.Close();
			maxIOSConnections = 0;
			maxAndroidConnections = 0;
			try
			{
				if (LICENSING_ENABLED)
				{
					using (MyWebClient myWebClient = new MyWebClient())
					{
						myWebClient.timeOutSec = 1;
						myWebClient.UploadFile("http://www.airfader.com/data/recvmetrics.php", "POST", text);
					}
					File.Delete(text);
				}
			}
			catch (Exception ex2)
			{
				logDebug(1, "Metrics skipped: " + ex2.Message);
			}
		}
		catch (Exception ex3)
		{
			logDebug(1, "Metrics file problem: " + ex3.ToString());
		}
		connStatus = ConnectionStatus.Disconnected;
		if (inDevice == null && outDevice == null)
		{
			return true;
		}
		try
		{
			if (inDevice != null)
			{
				inDevice.SysExMessageReceived -= HandleSysExMessageReceived;
				inDevice.Error -= inDevice_Error;
				try
				{
					inDevice.StopRecording();
				}
				catch (Exception ex4)
				{
					logDebug("disconnectConsole().inDevice.StopRecording(): " + ex4.ToString());
				}
				try
				{
					inDevice.Reset();
				}
				catch (Exception ex5)
				{
					logDebug("disconnectConsole().inDevice.Reset(): " + ex5.ToString());
				}
				try
				{
					inDevice.Close();
				}
				catch (Exception ex6)
				{
					logDebug("disconnectConsole().inDevice.Close(): " + ex6.ToString());
				}
			}
		}
		catch (Exception ex7)
		{
			logError("Connect()", "Connection", "When disconnecting input: " + ex7.ToString());
		}
		try
		{
			if (outDevice != null)
			{
				try
				{
					outDevice.Reset();
				}
				catch (Exception ex8)
				{
					logDebug("disconnectConsole().outDevice.Reset(): " + ex8.ToString());
				}
				try
				{
					outDevice.Close();
				}
				catch (Exception ex9)
				{
					logDebug("disconnectConsole().outDevice.Close(): " + ex9.ToString());
				}
			}
		}
		catch (Exception ex10)
		{
			logError("Connect()", "Connection", "When disconnecting output: " + ex10.ToString());
		}
		inDevice = null;
		outDevice = null;
		MixEngine.NewMessage("Disconnected", 1);
		return true;
	}

	private static void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
	{
	}

	private static void HandleSysExMessageReceived(object sender, SysExMessageEventArgs e)
	{
		try
		{
			lock (inQueue)
			{
				inQueue.Enqueue(e.Message.GetBytes());
				inWait.Set();
			}
		}
		catch (Exception ex)
		{
			logError("HandleSysExMsg()", "InEvent", ex.ToString());
		}
	}

	private static void HandleSysCommonMessageReceived(object sender, SysCommonMessageEventArgs e)
	{
	}

	private static void HandleSysRealtimeMessageReceived(object sender, SysRealtimeMessageEventArgs e)
	{
	}

	private static void inDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
	{
		logDebug("InDeviceError(): " + e.Error.Message);
	}

	public static byte[] UnStuffDump01V96(string hex, int len)
	{
		byte[] array = new byte[hex.Length / 2 + 1];
		byte[] array2 = new byte[hex.Length / 2 + 1];
		for (int i = 0; i < hex.Length / 2; i++)
		{
			array[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
		}
		int num = 0;
		int num2 = 0;
		while (num < array.Length - 8)
		{
			int num3 = (array[num + 4] << 7) + array[num + 5];
			for (int j = len; j < num3 + len - 6 - 1; j += 8)
			{
				for (int k = 0; k < 7; k++)
				{
					if (num + j + k + 1 < array.Length)
					{
						try
						{
							array[num + j] <<= 1;
							array2[num2 + k] = (byte)(array[num + j + k + 1] + (array[num + j] & 0x80));
						}
						catch (Exception ex)
						{
							logError("UnStuffDump()", "Math", ex.ToString());
							logDebug("Bulkpos: " + num + ", Realpos: " + num2 + ", i: " + k + ", Offset: " + j + ", MaxSize: " + array2.Length + ", DataLen: " + num3);
							return null;
						}
					}
				}
				num2 += 7;
			}
			num += num3 + 6;
			num += 2;
		}
		return array2;
	}

	private static byte[] UnStuffDumpLS9(string hex)
	{
		byte[] array = new byte[hex.Length / 2 + 1];
		byte[] array2 = new byte[hex.Length / 2 + 1];
		for (int i = 0; i < hex.Length / 2; i++)
		{
			array[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
		}
		int num = 0;
		int num2 = 0;
		int num3 = 19;
		while (num < array.Length - 8)
		{
			int num4 = (array[num + 4] << 7) + array[num + 5];
			for (int j = num3; j < num4 + num3 - 6 - 8; j += 8)
			{
				for (int k = 0; k < 7; k++)
				{
					if (num + j + k + 1 < array.Length)
					{
						try
						{
							array[num + j] <<= 1;
							array2[num2 + k] = (byte)(array[num + j + k + 1] + (array[num + j] & 0x80));
						}
						catch (Exception ex)
						{
							logError("UnStuffDump()", "Math", ex.ToString());
							return null;
						}
					}
				}
				num2 += 7;
			}
			num += num4 + 6;
			num += 2;
		}
		return array2;
	}

	private static byte[] UnStuffDumpM7CL(string hex)
	{
		byte[] array = new byte[hex.Length / 2 + 1];
		byte[] array2 = new byte[hex.Length / 2 + 1];
		for (int i = 0; i < hex.Length / 2; i++)
		{
			array[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
		}
		int num = 0;
		int num2 = 0;
		int num3 = 12;
		while (num < array.Length - 8)
		{
			int num4 = (array[num + 4] << 7) + array[num + 5];
			for (int j = num3; j < num4 + num3 - 6 - 1; j += 8)
			{
				for (int k = 0; k < 7; k++)
				{
					if (num + j + k + 1 < array.Length)
					{
						try
						{
							array[num + j] <<= 1;
							array2[num2 + k] = (byte)(array[num + j + k + 1] + (array[num + j] & 0x80));
						}
						catch (Exception ex)
						{
							logError("UnStuffDump()", "Math", ex.ToString());
							logDebug("Bulkpos: " + num + ", Realpos: " + num2 + ", i: " + k + ", Offset: " + j + ", MaxSize: " + array2.Length + ", DataLen: " + num4);
							return null;
						}
					}
				}
				num2 += 7;
			}
			num += num4 + 6;
			num += 2;
		}
		return array2;
	}

	public static bool processDump01V96(string hex, int len)
	{
		dumpData = hex;
		Application.DoEvents();
		byte[] array = UnStuffDump01V96(dumpData, len);
		if (array != null)
		{
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				folderPath += "\\01vmix";
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				BinaryWriter binaryWriter = new BinaryWriter(File.Open(folderPath + "\\Scene.dat", FileMode.Create));
				binaryWriter.Write(array);
				binaryWriter.Close();
			}
			catch (Exception ex)
			{
				logError("ProcessDump()", "InEvent", ex.ToString());
			}
		}
		return true;
	}

	private static bool processDumpLS9(string hex)
	{
		int num = 0;
		if (Console.type == ConsoleTypes.LS9)
		{
			num = int.Parse(hex.Substring(36, 2));
		}
		else if (Console.type == ConsoleTypes.M7CL)
		{
			num = int.Parse(hex.Substring(22, 2));
		}
		MixEngine.NewMessage("Bulk " + num, 0);
		if (num == 0)
		{
			dumpData = hex;
			return true;
		}
		if (num < 4)
		{
			dumpData += hex;
			return true;
		}
		dumpData += hex;
		Application.DoEvents();
		connPhase = ConnectionPhase.Syncing;
		logDebug("=== Begin processing dump ===");
		logDebug("Unstuff bytes");
		byte[] array = UnStuffDumpLS9(dumpData);
		if (array != null)
		{
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				folderPath += "\\01vmix";
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				BinaryWriter binaryWriter = new BinaryWriter(File.Open(folderPath + "\\Scene.dat", FileMode.Create));
				binaryWriter.Write(array);
				binaryWriter.Close();
			}
			catch (Exception ex)
			{
				logError("ProcessDump()", "InEvent", ex.ToString());
			}
		}
		logDebug("Set scene name");
		string @string = Encoding.ASCII.GetString(array, 0, 16);
		char[] trimChars = new char[1];
		Console.sceneName = @string.Trim(trimChars);
		setParam("LOGIC", Console.sceneNum, Pr.SceneName, 0, Console.sceneName);
		int num2 = 0;
		switch (array[17])
		{
		case 0:
		{
			logDebug("Looks like an LS9 older than v1.30");
			for (int num14 = 0; num14 < 72; num14++)
			{
				chData[num14].setParam("LOGIC", Channel.Pr.On, 0, (array[1461 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.Pan, 0, (sbyte)array[1542 + num14]);
				setParam("LOGIC", num14, Pr.Phantom, 0, ((array[739 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0) ? 1 : 0);
				chData[num14].setParam("LOGIC", Channel.Pr.Phase, 0, ((array[1452 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0) ? 1 : 0);
				chData[num14].setParam("LOGIC", Channel.Pr.PatchIn, 0, array[16105 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.InsertOn, 0, (array[1208 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.DirectOutOn, 0, (array[1226 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.ToStereoOn, 0, (array[6725 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.ToMonoOn, 0, (array[6734 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0On, 0, (array[1993 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Type, 0, array[2002 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Attack, 0, array[2389 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Range, 0, (sbyte)array[2461 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Hold, 0, array[2533 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Decay, 0, array[2605 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Ratio, 0, array[2677 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Gain, 0, array[2749 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Knee, 0, array[2821 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn0Threshold, 0, (short)(array[2894 + num14 * 2] << 8) + array[2895 + num14 * 2]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[3119 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[3128 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[3515 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[3731 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[3803 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[3875 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[3947 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[4020 + num14 * 2] << 8) + array[4021 + num14 * 2]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[4326 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0) ? 1 : 0);
				chData[num14].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[4335 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[1768 + num14 * 2] << 8) + array[1769 + num14 * 2]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQHPFOn, 0, (array[4236 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.EQHPFFreq, 0, array[4245 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[4344 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[4416 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[4497 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[4515 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[4813 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[5111 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[5409 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[4587 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[4885 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[5183 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[5481 + num14]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[4660 + num14 * 2] << 8) + array[4661 + num14 * 2]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[4958 + num14 * 2] << 8) + array[4959 + num14 * 2]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[5256 + num14 * 2] << 8) + array[5257 + num14 * 2]);
				chData[num14].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[5554 + num14 * 2] << 8) + array[5555 + num14 * 2]);
				num2 = (array[1614 + num14 * 2] << 8) + array[1615 + num14 * 2];
				chData[num14].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				Channel obj5 = chData[num14];
				string text5 = Encoding.ASCII.GetString(array, 16308 + num14 * 4, 4) + Encoding.ASCII.GetString(array, 16596 + num14 * 4, 2);
				char[] trimChars6 = new char[1];
				obj5.setParam("LOGIC", Channel.Pr.Name, 0, text5.Trim(trimChars6));
				for (int num15 = 0; num15 < 16; num15++)
				{
					chData[num14].setParam("LOGIC", Channel.Pr.SendToMixOn, num15, ((array[6770 + num15 * 162 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0) ? 1 : 0);
					chData[num14].setParam("LOGIC", Channel.Pr.SendToMixPre, num15, ((array[6779 + num15 * 162 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0) ? 1 : 0);
					num2 = array[6788 + num15 * 162 + num14 * 2] * 256 + array[6789 + num15 * 162 + num14 * 2];
					chData[num14].setParam("LOGIC", Channel.Pr.SendToMix, num15, num2);
					if (num15 % 2 == 0)
					{
						chData[num14].setParam("LOGIC", Channel.Pr.SendToMixPan, num15, (sbyte)array[9362 + num14 + num15 / 2 * 72]);
					}
				}
				chData[num14].setParam("LOGIC", Channel.Pr.Group, 0, array[15974 + num14]);
				for (int num16 = 0; num16 < 8; num16++)
				{
					if ((array[6581 + 9 * num16 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0)
					{
						chData[num14].setParam("LOGIC", Channel.Pr.MuteGroup, num16, 1);
					}
					else
					{
						chData[num14].setParam("LOGIC", Channel.Pr.MuteGroup, num16, 0);
					}
				}
				num2 = (array[15776 + num14 * 2] << 8) + array[15777 + num14 * 2];
				chData[num14].setParam("LOGIC", Channel.Pr.RelativeFader, 0, num2);
				chData[num14].setParam("CONSOLE", Channel.Pr.RelativeHA, 0, chData[num14].getParam(Channel.Pr.HA));
			}
			for (int num17 = 0; num17 < 32; num17++)
			{
				setParam("LOGIC", num17, Pr.HA, 0, array[748 + num17]);
				setParam("LOGIC", num17 + 32, Pr.HA, 0, array[17380 + num17 / 8 + num17 % 8 * 4]);
			}
			for (int num18 = 0; num18 < Console.maxMIX; num18++)
			{
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.BusType, 0, (sbyte)array[621 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Pair, 0, (array[617 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.PanLink, 0, (array[637 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				num2 = (array[11660 + num18 * 2] << 8) + array[11661 + num18 * 2];
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.On, 0, (array[11642 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Pan, 0, (sbyte)array[12660 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Balance, 0, ((num18 % 2 != 0) ? 1 : (-1)) * (sbyte)array[11644 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.ToStereoOn, 0, (array[12656 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.ToMonoOn, 0, (array[12676 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[11744 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[11746 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[11832 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[11880 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[11896 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[11912 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[11928 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[11944 + num18 * 2] << 8) + array[11945 + num18 * 2]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[11994 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0) ? 1 : 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[11996 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[11694 + num18 * 2] << 8) + array[11695 + num18 * 2]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[11998 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[12014 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[12030 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[12032 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[12036 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[12102 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[12168 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[12234 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[12052 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[12118 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[12184 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[12250 + num18]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[12068 + num18 * 2] << 8) + array[12069 + num18 * 2]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[12134 + num18 * 2] << 8) + array[12135 + num18 * 2]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[12200 + num18 * 2] << 8) + array[12201 + num18 * 2]);
				chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[12266 + num18 * 2] << 8) + array[12267 + num18 * 2]);
				for (int num19 = 0; num19 < 8; num19++)
				{
					chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrixOn, num19, ((array[12304 + num19 * 36 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0) ? 1 : 0);
					chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrixPre, num19, ((array[12306 + num19 * 36 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0) ? 1 : 0);
					num2 = array[12308 + num19 * 36 + num18 * 2] * 256 + array[12309 + num19 * 36 + num18 * 2];
					chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrix, num19, num2);
					if (num19 % 2 == 0)
					{
						chData[num18].setParam("LOGIC", Channel.Pr.SendToMatrixPan, num19, (sbyte)array[12592 + num18 + num19 / 2 * 16]);
					}
				}
				Channel obj6 = chData[num18 + chOffsetMix];
				string text6 = Encoding.ASCII.GetString(array, 17096 + num18 * 4, 4) + Encoding.ASCII.GetString(array, 17160 + num18 * 4, 2);
				char[] trimChars7 = new char[1];
				obj6.setParam("LOGIC", Channel.Pr.Name, 0, text6.Trim(trimChars7));
				for (int num20 = 0; num20 < 8; num20++)
				{
					if ((array[12678 + 2 * num20 + num18 / 8] & (int)Math.Pow(2.0, num18 % 8)) > 0)
					{
						chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, num20, 1);
					}
					else
					{
						chData[num18 + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, num20, 0);
					}
				}
			}
			for (int num21 = 0; num21 < Console.maxMATRIX; num21++)
			{
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Pair, 0, (array[639 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0);
				num2 = (array[12756 + num21 * 2] << 8) + array[12757 + num21 * 2];
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.On, 0, (array[12746 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Balance, 0, ((num21 % 2 != 0) ? 1 : (-1)) * (sbyte)array[12748 + num21]);
				Channel obj7 = chData[num21 + chOffsetMatrix];
				string text7 = Encoding.ASCII.GetString(array, 17224 + num21 * 4, 4) + Encoding.ASCII.GetString(array, 17256 + num21 * 4, 2);
				char[] trimChars8 = new char[1];
				obj7.setParam("LOGIC", Channel.Pr.Name, 0, text7.Trim(trimChars8));
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[12799 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[12800 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[12843 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[12867 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[12875 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[12883 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[12891 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[12900 + num21 * 2] << 8) + array[12901 + num21 * 2]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[12925 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0) ? 1 : 0);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[12926 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[12774 + num21 * 2] << 8) + array[12775 + num21 * 2]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[12927 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[12935 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[12944 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[12943 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[12946 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[12979 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[13013 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[13047 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[12954 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[12987 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[13021 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[13055 + num21]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[12962 + num21 * 2] << 8) + array[12963 + num21 * 2]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[12996 + num21 * 2] << 8) + array[12997 + num21 * 2]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[13030 + num21 * 2] << 8) + array[13031 + num21 * 2]);
				chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[13064 + num21 * 2] << 8) + array[13065 + num21 * 2]);
				for (int num22 = 0; num22 < 8; num22++)
				{
					if ((array[13080 + num22 + num21 / 8] & (int)Math.Pow(2.0, num21 % 8)) > 0)
					{
						chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, num22, 1);
					}
					else
					{
						chData[num21 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, num22, 0);
					}
				}
			}
			for (int num23 = 0; num23 < 3; num23++)
			{
				num2 = (array[13116 + num23 * 2] << 8) + array[13117 + num23 * 2];
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				num2 = (array[15968 + num23 * 2] << 8) + array[15969 + num23 * 2];
				chData[chOffsetStereo + num23].setParam("LOGIC", Channel.Pr.RelativeFader, 0, num2);
				chData[chOffsetStereo + num23].setParam("LOGIC", Channel.Pr.On, 0, (array[13112 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
				Channel obj8 = chData[num23 + chOffsetStereo];
				string text8 = Encoding.ASCII.GetString(array, 17288 + num23 * 4, 4) + Encoding.ASCII.GetString(array, 17300 + num23 * 4, 2);
				char[] trimChars9 = new char[1];
				obj8.setParam("LOGIC", Channel.Pr.Name, 0, text8.Trim(trimChars9));
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[13134 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[13135 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[13153 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[13162 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[13165 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[13168 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[13171 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[13174 + num23 * 2] << 8) + array[13175 + num23 * 2]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[13184 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0) ? 1 : 0);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[13185 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[13124 + num23 * 2] << 8) + array[13125 + num23 * 2]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[13186 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[13189 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[13192 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[13193 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[13195 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[13209 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[13223 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[13237 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[13198 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[13212 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[13226 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[13240 + num23]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[13202 + num23 * 2] << 8) + array[13203 + num23 * 2]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[13216 + num23 * 2] << 8) + array[13217 + num23 * 2]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[13230 + num23 * 2] << 8) + array[13231 + num23 * 2]);
				chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[13244 + num23 * 2] << 8) + array[13245 + num23 * 2]);
				for (int num24 = 0; num24 < 8; num24++)
				{
					if ((array[13330 + num24 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0)
					{
						chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, num24, 1);
					}
					else
					{
						chData[num23 + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, num24, 0);
					}
				}
			}
			for (int num25 = 0; num25 < 8; num25++)
			{
				setParam("LOGIC", num25, Pr.RackType, 0, array[13492 + num25]);
				setParam("LOGIC", num25, Pr.GEQOn, 0, ((array[13517 + num25 / 8] & (int)Math.Pow(2.0, num25 % 8)) > 0) ? 1 : 0);
				setParam("LOGIC", num25 + 8, Pr.GEQOn, 0, ((array[13518 + num25 / 8] & (int)Math.Pow(2.0, num25 % 8)) > 0) ? 1 : 0);
				setParam("LOGIC", num25, Pr.RackSource, 0, array[17076 + num25]);
				setParam("LOGIC", num25 + 8, Pr.RackSource, 0, array[17076 + num25 + 8]);
				setParam("LOGIC", num25, Pr.GEQLink, 0, array[13501 + num25]);
				setParam("LOGIC", num25, Pr.GEQLinkLR, 0, array[13509 + num25]);
				setParam("LOGIC", num25 + 8, Pr.GEQLinkLR, 0, array[13509 + num25]);
				for (int num26 = 0; num26 < 31; num26++)
				{
					setParam("LOGIC", num25, Pr.GEQBand, num26, (short)(array[13538 + num25 * 2 + num26 * 16] << 8) + array[13539 + num25 * 2 + num26 * 16]);
				}
				for (int num27 = 0; num27 < 31; num27++)
				{
					setParam("LOGIC", num25 + 8, Pr.GEQBand, num27, (short)(array[14034 + num25 * 2 + num27 * 16] << 8) + array[14035 + num25 * 2 + num27 * 16]);
				}
			}
			for (int num28 = 0; num28 < 4; num28++)
			{
				EFXData[num28].setParam("LOGIC", EFX.Pr.BPM, (short)(array[14596 + num28 * 2] << 8) + array[14597 + num28 * 2]);
				EFXData[num28].setParam("LOGIC", EFX.Pr.Type, array[14621 + num28]);
				EFXData[num28].setParam("LOGIC", EFX.Pr.Bypass, ((array[14608 + num28 / 8] & (int)Math.Pow(2.0, num28 % 8)) > 0) ? 1 : 0);
				EFXData[num28].setParam("LOGIC", EFX.Pr.Mix, array[14617 + num28]);
				for (int num29 = 0; num29 < 32; num29++)
				{
					EFXData[num28].setParamByMIDIIndex("LOGIC", num29 + 11, (short)(array[14630 + num28 * 4 + num29 * 16] << 8) + array[14631 + num28 * 4 + num29 * 16]);
				}
			}
			for (int num30 = 0; num30 < 8; num30++)
			{
				muteGroup[num30].muted = (array[616 + num30 / 8] & (int)Math.Pow(2.0, num30 % 8)) > 0;
			}
			chGroup[0].ha = (array[326] & (int)Math.Pow(2.0, 0.0)) > 0;
			chGroup[0].dyn1 = (array[326] & (int)Math.Pow(2.0, 1.0)) > 0;
			chGroup[0].dyn2 = (array[326] & (int)Math.Pow(2.0, 2.0)) > 0;
			chGroup[0].eq = (array[326] & (int)Math.Pow(2.0, 3.0)) > 0;
			chGroup[0].on = (array[326] & (int)Math.Pow(2.0, 4.0)) > 0;
			chGroup[0].fader = (array[326] & (int)Math.Pow(2.0, 5.0)) > 0;
			chGroup[0].insert = (array[326] & (int)Math.Pow(2.0, 6.0)) > 0;
			chGroup[0].directout = (array[326] & (int)Math.Pow(2.0, 7.0)) > 0;
			break;
		}
		case 1:
		{
			logDebug("Looks like an LS9 v1.30+");
			logDebug("Sync Channel 1-72");
			for (int i = 0; i < 72; i++)
			{
				chData[i].setParam("LOGIC", Channel.Pr.On, 0, (array[1523 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.Pan, 0, (sbyte)array[1604 + i]);
				setParam("LOGIC", i, Pr.Phantom, 0, ((array[739 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				chData[i].setParam("LOGIC", Channel.Pr.Phase, 0, ((array[1514 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				chData[i].setParam("LOGIC", Channel.Pr.PatchIn, 0, array[16189 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.InsertOn, 0, (array[1208 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.DirectOutOn, 0, (array[1289 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.ToStereoOn, 0, (array[6787 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.ToMonoOn, 0, (array[6796 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0On, 0, (array[2055 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Type, 0, array[2064 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Attack, 0, array[2451 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Range, 0, (sbyte)array[2523 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Hold, 0, array[2595 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Decay, 0, array[2667 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Ratio, 0, array[2739 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Gain, 0, array[2811 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Knee, 0, array[2883 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn0Threshold, 0, (short)(array[2956 + i * 2] << 8) + array[2957 + i * 2]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[3181 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[3190 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[3577 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[3793 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[3865 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[3937 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[4009 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[4082 + i * 2] << 8) + array[4083 + i * 2]);
				chData[i].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[4388 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				chData[i].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[4397 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[1830 + i * 2] << 8) + array[1831 + i * 2]);
				chData[i].setParam("LOGIC", Channel.Pr.EQHPFOn, 0, (array[4298 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.EQHPFFreq, 0, array[4307 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[4406 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[4478 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[4559 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
				chData[i].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[4577 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[4875 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[5173 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[5471 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[4649 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[4947 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[5245 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[5543 + i]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[4722 + i * 2] << 8) + array[4723 + i * 2]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[5020 + i * 2] << 8) + array[5021 + i * 2]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[5318 + i * 2] << 8) + array[5319 + i * 2]);
				chData[i].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[5616 + i * 2] << 8) + array[5617 + i * 2]);
				num2 = (array[1676 + i * 2] << 8) + array[1677 + i * 2];
				chData[i].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				Channel obj = chData[i];
				string text = Encoding.ASCII.GetString(array, 16392 + i * 4, 4) + Encoding.ASCII.GetString(array, 16680 + i * 4, 2);
				char[] trimChars2 = new char[1];
				obj.setParam("LOGIC", Channel.Pr.Name, 0, text.Trim(trimChars2));
				for (int j = 0; j < 16; j++)
				{
					chData[i].setParam("LOGIC", Channel.Pr.SendToMixOn, j, ((array[6832 + j * 162 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
					chData[i].setParam("LOGIC", Channel.Pr.SendToMixPre, j, ((array[6841 + j * 162 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
					num2 = array[6850 + j * 162 + i * 2] * 256 + array[6851 + j * 162 + i * 2];
					chData[i].setParam("LOGIC", Channel.Pr.SendToMix, j, num2);
					if (j % 2 == 0)
					{
						chData[i].setParam("LOGIC", Channel.Pr.SendToMixPan, j, (sbyte)array[9424 + i + j / 2 * 72]);
					}
				}
				chData[i].setParam("LOGIC", Channel.Pr.Group, 0, array[16058 + i]);
				for (int k = 0; k < 8; k++)
				{
					if ((array[6643 + 9 * k + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0)
					{
						chData[i].setParam("LOGIC", Channel.Pr.MuteGroup, k, 1);
					}
					else
					{
						chData[i].setParam("LOGIC", Channel.Pr.MuteGroup, k, 0);
					}
				}
				num2 = (array[15860 + i * 2] << 8) + array[15861 + i * 2];
				chData[i].setParam("LOGIC", Channel.Pr.RelativeFader, 0, num2);
				chData[i].setParam("LOGIC", Channel.Pr.RelativeHA, 0, chData[i].getParam(Channel.Pr.HA));
			}
			for (int l = 0; l < 32; l++)
			{
				setParam("LOGIC", l, Pr.HA, 0, array[748 + l]);
				setParam("LOGIC", l + 32, Pr.HA, 0, array[17464 + l / 8 + l % 8 * 4]);
			}
			for (int m = 0; m < Console.maxMIX; m++)
			{
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.BusType, 0, (sbyte)array[621 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Pair, 0, (array[617 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.PanLink, 0, (array[637 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				num2 = (array[11736 + m * 2] << 8) + array[11737 + m * 2];
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.On, 0, (array[11718 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Pan, 0, (sbyte)array[12736 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Balance, 0, ((m % 2 != 0) ? 1 : (-1)) * (sbyte)array[11720 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.ToStereoOn, 0, (array[12732 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.ToMonoOn, 0, (array[12752 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[11820 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[11822 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[11908 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[11956 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[11972 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[11988 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[12004 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[12020 + m * 2] << 8) + array[12021 + m * 2]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[12070 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0) ? 1 : 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[12072 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[11770 + m * 2] << 8) + array[11771 + m * 2]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[12074 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[12090 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[12106 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[12108 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[12112 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[12178 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[12244 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[12310 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[12128 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[12194 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[12260 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[12326 + m]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[12144 + m * 2] << 8) + array[12145 + m * 2]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[12210 + m * 2] << 8) + array[12211 + m * 2]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[12276 + m * 2] << 8) + array[12277 + m * 2]);
				chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[12342 + m * 2] << 8) + array[12343 + m * 2]);
				for (int n = 0; n < 8; n++)
				{
					chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrixOn, n, ((array[12380 + n * 36 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0) ? 1 : 0);
					chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrixPre, n, ((array[12382 + n * 36 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0) ? 1 : 0);
					num2 = array[12384 + n * 36 + m * 2] * 256 + array[12385 + n * 36 + m * 2];
					chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrix, n, num2);
					if (n % 2 == 0)
					{
						chData[m].setParam("LOGIC", Channel.Pr.SendToMatrixPan, n, (sbyte)array[12668 + m + n / 2 * 16]);
					}
				}
				Channel obj2 = chData[m + chOffsetMix];
				string text2 = Encoding.ASCII.GetString(array, 17180 + m * 4, 4) + Encoding.ASCII.GetString(array, 17244 + m * 4, 2);
				char[] trimChars3 = new char[1];
				obj2.setParam("LOGIC", Channel.Pr.Name, 0, text2.Trim(trimChars3));
				for (int num3 = 0; num3 < 8; num3++)
				{
					if ((array[12754 + 2 * num3 + m / 8] & (int)Math.Pow(2.0, m % 8)) > 0)
					{
						chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, num3, 1);
					}
					else
					{
						chData[m + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, num3, 0);
					}
				}
			}
			for (int num4 = 0; num4 < Console.maxMATRIX; num4++)
			{
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Pair, 0, (array[639 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0);
				num2 = (array[12838 + num4 * 2] << 8) + array[12839 + num4 * 2];
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.On, 0, (array[12829 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Balance, 0, ((num4 % 2 != 0) ? 1 : (-1)) * (sbyte)array[12830 + num4]);
				Channel obj3 = chData[num4 + chOffsetMatrix];
				string text3 = Encoding.ASCII.GetString(array, 17308 + num4 * 4, 4) + Encoding.ASCII.GetString(array, 17340 + num4 * 4, 2);
				char[] trimChars4 = new char[1];
				obj3.setParam("LOGIC", Channel.Pr.Name, 0, text3.Trim(trimChars4));
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[12881 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[12882 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[12925 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[12949 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[12957 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[12965 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[12973 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[12982 + num4 * 2] << 8) + array[12983 + num4 * 2]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[13007 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0) ? 1 : 0);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[13008 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[12856 + num4 * 2] << 8) + array[12857 + num4 * 2]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[13009 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[13017 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[13025 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[13026 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[13028 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[13061 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[13095 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[13129 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[13036 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[13069 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[13103 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[13137 + num4]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[13044 + num4 * 2] << 8) + array[13045 + num4 * 2]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[13078 + num4 * 2] << 8) + array[13079 + num4 * 2]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[13112 + num4 * 2] << 8) + array[13113 + num4 * 2]);
				chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[13146 + num4 * 2] << 8) + array[13147 + num4 * 2]);
				for (int num5 = 0; num5 < 8; num5++)
				{
					if ((array[13162 + num5 + num4 / 8] & (int)Math.Pow(2.0, num4 % 8)) > 0)
					{
						chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, num5, 1);
					}
					else
					{
						chData[num4 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, num5, 0);
					}
				}
			}
			for (int num6 = 0; num6 < 3; num6++)
			{
				num2 = (array[13200 + num6 * 2] << 8) + array[13201 + num6 * 2];
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Fader, 0, num2);
				num2 = (array[16052 + num6 * 2] << 8) + array[16053 + num6 * 2];
				chData[chOffsetStereo + num6].setParam("LOGIC", Channel.Pr.RelativeFader, 0, num2);
				chData[chOffsetStereo + num6].setParam("LOGIC", Channel.Pr.On, 0, (array[13196 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
				Channel obj4 = chData[num6 + chOffsetStereo];
				string text4 = Encoding.ASCII.GetString(array, 17372 + num6 * 4, 4) + Encoding.ASCII.GetString(array, 17384 + num6 * 4, 2);
				char[] trimChars5 = new char[1];
				obj4.setParam("LOGIC", Channel.Pr.Name, 0, text4.Trim(trimChars5));
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[13218 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[13219 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[13237 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[13246 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[13249 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[13252 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[13255 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[13258 + num6 * 2] << 8) + array[13259 + num6 * 2]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[13268 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0) ? 1 : 0);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[13269 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[13208 + num6 * 2] << 8) + array[13209 + num6 * 2]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[13270 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[13273 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[13276 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[13277 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1Q, 0, array[13279 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2Q, 0, array[13293 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3Q, 0, array[13307 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4Q, 0, array[13321 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[13282 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[13296 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[13310 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[13324 + num6]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[13286 + num6 * 2] << 8) + array[13287 + num6 * 2]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[13300 + num6 * 2] << 8) + array[13301 + num6 * 2]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[13314 + num6 * 2] << 8) + array[13315 + num6 * 2]);
				chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[13328 + num6 * 2] << 8) + array[13329 + num6 * 2]);
				for (int num7 = 0; num7 < 8; num7++)
				{
					if ((array[13414 + num7 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0)
					{
						chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, num7, 1);
					}
					else
					{
						chData[num6 + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, num7, 0);
					}
				}
			}
			for (int num8 = 0; num8 < 8; num8++)
			{
				setParam("LOGIC", num8, Pr.RackType, 0, array[13576 + num8]);
				setParam("LOGIC", num8, Pr.GEQOn, 0, ((array[13601 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0) ? 1 : 0);
				setParam("LOGIC", num8 + 8, Pr.GEQOn, 0, ((array[13602 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0) ? 1 : 0);
				setParam("LOGIC", num8, Pr.RackSource, 0, array[17160 + num8]);
				setParam("LOGIC", num8 + 8, Pr.RackSource, 0, array[17160 + num8 + 8]);
				setParam("LOGIC", num8, Pr.GEQLink, 0, array[13585 + num8]);
				setParam("LOGIC", num8, Pr.GEQLinkLR, 0, array[13593 + num8]);
				setParam("LOGIC", num8 + 8, Pr.GEQLinkLR, 0, array[13593 + num8]);
				for (int num9 = 0; num9 < 31; num9++)
				{
					setParam("LOGIC", num8, Pr.GEQBand, num9, (short)(array[13622 + num8 * 2 + num9 * 16] << 8) + array[13623 + num8 * 2 + num9 * 16]);
				}
				for (int num10 = 0; num10 < 31; num10++)
				{
					setParam("LOGIC", num8 + 8, Pr.GEQBand, num10, (short)(array[14118 + num8 * 2 + num10 * 16] << 8) + array[14119 + num8 * 2 + num10 * 16]);
				}
			}
			for (int num11 = 0; num11 < 4; num11++)
			{
				EFXData[num11].setParam("LOGIC", EFX.Pr.BPM, (short)(array[14680 + num11 * 2] << 8) + array[14681 + num11 * 2]);
				EFXData[num11].setParam("LOGIC", EFX.Pr.Bypass, ((array[14692 + num11 / 8] & (int)Math.Pow(2.0, num11 % 8)) > 0) ? 1 : 0);
				EFXData[num11].setParam("LOGIC", EFX.Pr.Mix, array[14701 + num11]);
				EFXData[num11].setParam("LOGIC", EFX.Pr.Type, array[14705 + num11]);
				for (int num12 = 0; num12 < 32; num12++)
				{
					EFXData[num11].setParamByMIDIIndex("LOGIC", num12 + 11, (short)(array[14714 + num11 * 4 + num12 * 16] << 8) + array[14715 + num11 * 4 + num12 * 16]);
				}
			}
			for (int num13 = 0; num13 < 8; num13++)
			{
				muteGroup[num13].muted = (array[616 + num13 / 8] & (int)Math.Pow(2.0, num13 % 8)) > 0;
			}
			chGroup[0].ha = (array[326] & (int)Math.Pow(2.0, 0.0)) > 0;
			chGroup[0].dyn1 = (array[326] & (int)Math.Pow(2.0, 1.0)) > 0;
			chGroup[0].dyn2 = (array[326] & (int)Math.Pow(2.0, 2.0)) > 0;
			chGroup[0].eq = (array[326] & (int)Math.Pow(2.0, 3.0)) > 0;
			chGroup[0].on = (array[326] & (int)Math.Pow(2.0, 4.0)) > 0;
			chGroup[0].fader = (array[326] & (int)Math.Pow(2.0, 5.0)) > 0;
			chGroup[0].insert = (array[326] & (int)Math.Pow(2.0, 6.0)) > 0;
			chGroup[0].directout = (array[326] & (int)Math.Pow(2.0, 7.0)) > 0;
			chGroup[0].mixLevelSend = (array[367] & (int)Math.Pow(2.0, 0.0)) > 0;
			chGroup[0].mixLevelOn = (array[367] & (int)Math.Pow(2.0, 1.0)) > 0;
			chGroup[0].mixSend[0] = (array[367] & (int)Math.Pow(2.0, 4.0)) > 0;
			chGroup[0].mixSend[1] = (array[367] & (int)Math.Pow(2.0, 5.0)) > 0;
			chGroup[0].mixSend[2] = (array[367] & (int)Math.Pow(2.0, 6.0)) > 0;
			chGroup[0].mixSend[3] = (array[367] & (int)Math.Pow(2.0, 7.0)) > 0;
			chGroup[0].mixSend[4] = (array[366] & (int)Math.Pow(2.0, 0.0)) > 0;
			chGroup[0].mixSend[5] = (array[366] & (int)Math.Pow(2.0, 1.0)) > 0;
			chGroup[0].mixSend[6] = (array[366] & (int)Math.Pow(2.0, 2.0)) > 0;
			chGroup[0].mixSend[7] = (array[366] & (int)Math.Pow(2.0, 3.0)) > 0;
			chGroup[0].mixSend[8] = (array[366] & (int)Math.Pow(2.0, 4.0)) > 0;
			chGroup[0].mixSend[9] = (array[366] & (int)Math.Pow(2.0, 5.0)) > 0;
			chGroup[0].mixSend[10] = (array[366] & (int)Math.Pow(2.0, 6.0)) > 0;
			chGroup[0].mixSend[11] = (array[366] & (int)Math.Pow(2.0, 7.0)) > 0;
			chGroup[0].mixSend[12] = (array[365] & (int)Math.Pow(2.0, 0.0)) > 0;
			chGroup[0].mixSend[13] = (array[365] & (int)Math.Pow(2.0, 1.0)) > 0;
			chGroup[0].mixSend[14] = (array[365] & (int)Math.Pow(2.0, 2.0)) > 0;
			chGroup[0].mixSend[15] = (array[365] & (int)Math.Pow(2.0, 3.0)) > 0;
			break;
		}
		}
		logDebug("=== Process LS9 Dump Complete ===");
		MixEngine.NewMessage("Sync Complete", 0);
		connPhase = ConnectionPhase.PostSync;
		setParam("LOGIC", 0, Pr.None, 0, 1);
		return true;
	}

	private static bool processDumpM7CL(string hex)
	{
		int num = 0;
		int num2 = 0;
		if (Console.type == ConsoleTypes.LS9)
		{
			num = int.Parse(hex.Substring(36, 2));
		}
		else if (Console.type == ConsoleTypes.M7CL)
		{
			num = int.Parse(hex.Substring(22, 2));
		}
		MixEngine.NewMessage("Bulk " + num, 0);
		if (num == 0)
		{
			dumpData = hex;
			return true;
		}
		if (num < 4)
		{
			dumpData += hex;
			return true;
		}
		dumpData += hex;
		Application.DoEvents();
		byte[] array = UnStuffDumpM7CL(dumpData);
		if (array != null)
		{
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				folderPath += "\\01vmix";
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				BinaryWriter binaryWriter = new BinaryWriter(File.Open(folderPath + "\\Scene.dat", FileMode.Create));
				binaryWriter.Write(array);
				binaryWriter.Close();
			}
			catch (Exception ex)
			{
				logError("ProcessDump()", "InEvent", ex.ToString());
			}
		}
		switch (array[17])
		{
		case 2:
			MixEngine.NewMessage("M7CL Ver 3.0x", 1);
			Console.ver = ConsoleVersions.M7CLv3;
			num2 = 0;
			break;
		case 4:
			MixEngine.NewMessage("M7CL Ver 3.5x", 1);
			Console.ver = ConsoleVersions.M7CLv35;
			num2 = 28;
			break;
		default:
			MixEngine.NewMessage("Not compatible with M7CL Ver " + array[32] + "\nI can't sync but can try to mix...", 2);
			return false;
		}
		connPhase = ConnectionPhase.Syncing;
		string @string = Encoding.ASCII.GetString(array, 0, 16);
		char[] trimChars = new char[1];
		Console.sceneName = @string.Trim(trimChars);
		setParam("LOGIC", Console.sceneNum, Pr.SceneName, 0, Console.sceneName);
		int num3 = 0;
		for (int i = 0; i < 56; i++)
		{
			chData[i].setParam("LOGIC", Channel.Pr.On, 0, (array[num2 + 1297 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.Pan, 0, (sbyte)array[num2 + 1360 + i]);
			setParam("LOGIC", i, Pr.Phantom, 0, ((array[num2 + 625 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
			chData[i].setParam("LOGIC", Channel.Pr.Phase, 0, ((array[num2 + 1290 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
			chData[i].setParam("LOGIC", Channel.Pr.PatchIn, 0, array[num2 + 13669 + i]);
			setParam("LOGIC", i, Pr.HA, 0, array[num2 + 632 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.ToStereoOn, 0, (array[num2 + 5393 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.ToMonoOn, 0, (array[num2 + 5400 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0On, 0, (array[num2 + 1711 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Type, 0, array[num2 + 1718 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Attack, 0, array[num2 + 2019 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Range, 0, (sbyte)array[num2 + 2075 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Hold, 0, array[num2 + 2131 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Decay, 0, array[num2 + 2187 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Ratio, 0, array[num2 + 2243 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Gain, 0, array[num2 + 2299 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Knee, 0, array[num2 + 2355 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn0Threshold, 0, (short)(array[num2 + 2412 + i * 2] << 8) + array[num2 + 2413 + i * 2]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[num2 + 2587 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[num2 + 2594 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[num2 + 2895 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[num2 + 3063 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[num2 + 3119 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[num2 + 3175 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[num2 + 3231 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[num2 + 3288 + i * 2] << 8) + array[num2 + 3289 + i * 2]);
			chData[i].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[num2 + 3533 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[num2 + 1536 + i * 2] << 8) + array[num2 + 1537 + i * 2]);
			chData[i].setParam("LOGIC", Channel.Pr.EQHPFOn, 0, (array[num2 + 3456 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.EQHPFFreq, 0, array[num2 + 3463 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[num2 + 3540 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[num2 + 3596 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[num2 + 3659 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0);
			chData[i].setParam("LOGIC", Channel.Pr.EQ1Q, 0, (sbyte)array[num2 + 3673 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ2Q, 0, (sbyte)array[num2 + 3905 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ3Q, 0, (sbyte)array[num2 + 4137 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ4Q, 0, (sbyte)array[num2 + 4369 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[num2 + 3729 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[num2 + 3961 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[num2 + 4193 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[num2 + 4425 + i]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[num2 + 3786 + i * 2] << 8) + array[num2 + 3787 + i * 2]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[num2 + 4018 + i * 2] << 8) + array[num2 + 4019 + i * 2]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[num2 + 4250 + i * 2] << 8) + array[num2 + 4251 + i * 2]);
			chData[i].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[num2 + 4482 + i * 2] << 8) + array[num2 + 4483 + i * 2]);
			num3 = (array[num2 + 1416 + i * 2] << 8) + array[num2 + 1417 + i * 2];
			chData[i].setParam("LOGIC", Channel.Pr.Fader, 0, num3);
			Channel obj = chData[i];
			string text = Encoding.ASCII.GetString(array, num2 + 13840 + i * 4, 4) + Encoding.ASCII.GetString(array, num2 + 14064 + i * 4, 4);
			char[] trimChars2 = new char[1];
			obj.setParam("LOGIC", Channel.Pr.Name, 0, text.Trim(trimChars2));
			for (int j = 0; j < 16; j++)
			{
				chData[i].setParam("LOGIC", Channel.Pr.SendToMixOn, j, ((array[num2 + 5428 + j * 126 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				chData[i].setParam("LOGIC", Channel.Pr.SendToMixPre, j, ((array[num2 + 5435 + j * 126 + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				num3 = array[num2 + 5442 + j * 126 + i * 2] * 256 + array[num2 + 5443 + j * 126 + i * 2];
				chData[i].setParam("LOGIC", Channel.Pr.SendToMix, j, num3);
				if (j % 2 == 0)
				{
					chData[i].setParam("LOGIC", Channel.Pr.SendToMixPan, j, (sbyte)array[num2 + 7444 + i + j / 2 * 56]);
				}
			}
			for (int k = 0; k < 8; k++)
			{
				chData[i].setParam("LOGIC", Channel.Pr.SendToMatrixOn, k, ((array[num2 + 7913 + k * 126 + i / 8 + ((k > 0) ? 1 : 0)] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				chData[i].setParam("LOGIC", Channel.Pr.SendToMatrixPre, k, ((array[num2 + 7920 + k * 126 + i / 8 + ((k > 0) ? 1 : 0)] & (int)Math.Pow(2.0, i % 8)) > 0) ? 1 : 0);
				num3 = array[num2 + 7928 + k * 126 + i * 2] * 256 + array[num2 + 7929 + k * 126 + i * 2];
				chData[i].setParam("LOGIC", Channel.Pr.SendToMatrix, k, num3);
				if (k % 2 == 0)
				{
					chData[i].setParam("LOGIC", Channel.Pr.SendToMatrixPan, k, (sbyte)array[num2 + 8922 + i + k / 2 * 56]);
				}
			}
			chData[i].setParam("LOGIC", Channel.Pr.Group, 0, array[num2 + 13554 + i]);
			for (int l = 0; l < 8; l++)
			{
				if ((array[num2 + 5281 + 7 * l + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0)
				{
					chData[i].setParam("LOGIC", Channel.Pr.MuteGroup, l, 1);
				}
				else
				{
					chData[i].setParam("LOGIC", Channel.Pr.MuteGroup, l, 0);
				}
			}
			for (int m = 0; m < 8; m++)
			{
				if ((array[num2 + 5337 + 7 * m + i / 8] & (int)Math.Pow(2.0, i % 8)) > 0)
				{
					chData[i].setParam("LOGIC", Channel.Pr.DCAGroup, m, 1);
				}
				else
				{
					chData[i].setParam("LOGIC", Channel.Pr.DCAGroup, m, 0);
				}
			}
			num3 = (array[num2 + 13388 + i * 2] << 8) + array[num2 + 13389 + i * 2];
			chData[i].setParam("LOGIC", Channel.Pr.RelativeFader, 0, num3);
			chData[i].setParam("LOGIC", Channel.Pr.RelativeHA, 0, chData[i].getParam(Channel.Pr.HA));
		}
		for (int n = 0; n < Console.maxMIX; n++)
		{
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.BusType, 0, (sbyte)array[num2 + 527 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Pair, 0, (array[num2 + 523 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.PanLink, 0, (array[num2 + 543 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			num3 = (array[num2 + 9270 + n * 2] << 8) + array[num2 + 9271 + n * 2];
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Fader, 0, num3);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.On, 0, (array[num2 + 9252 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Pan, 0, (sbyte)array[num2 + 10270 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.ToStereoOn, 0, (array[num2 + 10266 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.ToMonoOn, 0, (array[num2 + 10286 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[num2 + 9354 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[num2 + 9356 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[num2 + 9442 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[num2 + 9490 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[num2 + 9506 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[num2 + 9522 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[num2 + 9538 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[num2 + 9554 + n * 2] << 8) + array[num2 + 9555 + n * 2]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[num2 + 9604 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0) ? 1 : 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[num2 + 9606 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[num2 + 9304 + n * 2] << 8) + array[num2 + 9305 + n * 2]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[num2 + 9608 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[num2 + 9624 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[num2 + 9640 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[num2 + 9642 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1Q, 0, (sbyte)array[num2 + 9646 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2Q, 0, (sbyte)array[num2 + 9712 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3Q, 0, (sbyte)array[num2 + 9778 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4Q, 0, (sbyte)array[num2 + 9844 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[num2 + 9662 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[num2 + 9728 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[num2 + 9794 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[num2 + 9860 + n]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[num2 + 9678 + n * 2] << 8) + array[num2 + 9679 + n * 2]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[num2 + 9744 + n * 2] << 8) + array[num2 + 9745 + n * 2]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[num2 + 9810 + n * 2] << 8) + array[num2 + 9811 + n * 2]);
			chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[num2 + 9876 + n * 2] << 8) + array[num2 + 9877 + n * 2]);
			for (int num4 = 0; num4 < 8; num4++)
			{
				chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrixOn, num4, ((array[num2 + 9914 + num4 * 36 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0) ? 1 : 0);
				chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrixPre, num4, ((array[num2 + 9916 + num4 * 36 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0) ? 1 : 0);
				num3 = array[num2 + 9918 + num4 * 36 + n * 2] * 256 + array[num2 + 9919 + num4 * 36 + n * 2];
				chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.SendToMatrix, num4, num3);
			}
			Channel obj2 = chData[n + chOffsetMix];
			string text2 = Encoding.ASCII.GetString(array, num2 + 14444 + n * 4, 4) + Encoding.ASCII.GetString(array, num2 + 14508 + n * 4, 4);
			char[] trimChars3 = new char[1];
			obj2.setParam("LOGIC", Channel.Pr.Name, 0, text2.Trim(trimChars3));
			for (int num5 = 0; num5 < 8; num5++)
			{
				if ((array[num2 + 10288 + 2 * num5 + n / 8] & (int)Math.Pow(2.0, n % 8)) > 0)
				{
					chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, num5, 1);
				}
				else
				{
					chData[n + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, num5, 0);
				}
			}
		}
		for (int num6 = 0; num6 < Console.maxMATRIX; num6++)
		{
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.BusType, 0, (sbyte)array[num2 + 546 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Pair, 0, (array[num2 + 545 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.PanLink, 0, (array[num2 + 554 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			num3 = (array[num2 + 10372 + num6 * 2] << 8) + array[num2 + 10373 + num6 * 2];
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Fader, 0, num3);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.On, 0, (array[num2 + 10363 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			Channel obj3 = chData[num6 + chOffsetMatrix];
			string text3 = Encoding.ASCII.GetString(array, num2 + 14572 + num6 * 4, 4) + Encoding.ASCII.GetString(array, num2 + 14604 + num6 * 4, 4);
			char[] trimChars4 = new char[1];
			obj3.setParam("LOGIC", Channel.Pr.Name, 0, text3.Trim(trimChars4));
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[num2 + 10415 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[num2 + 10416 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[num2 + 10459 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[num2 + 10483 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[num2 + 10491 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[num2 + 10499 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[num2 + 10507 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[num2 + 10516 + num6 * 2] << 8) + array[num2 + 10517 + num6 * 2]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[num2 + 10541 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0) ? 1 : 0);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[num2 + 10542 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[num2 + 10390 + num6 * 2] << 8) + array[num2 + 10391 + num6 * 2]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[num2 + 10543 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[num2 + 10551 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[num2 + 10559 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[num2 + 10560 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1Q, 0, (sbyte)array[num2 + 10562 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2Q, 0, (sbyte)array[num2 + 10595 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3Q, 0, (sbyte)array[num2 + 10629 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4Q, 0, (sbyte)array[num2 + 10663 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[num2 + 10570 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[num2 + 10603 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[num2 + 10637 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[num2 + 10671 + num6]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[num2 + 10578 + num6 * 2] << 8) + array[num2 + 10579 + num6 * 2]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[num2 + 10612 + num6 * 2] << 8) + array[num2 + 10613 + num6 * 2]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[num2 + 10646 + num6 * 2] << 8) + array[num2 + 10647 + num6 * 2]);
			chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[num2 + 10680 + num6 * 2] << 8) + array[num2 + 10681 + num6 * 2]);
			for (int num7 = 0; num7 < 8; num7++)
			{
				if ((array[num2 + 10696 + num7 + num6 / 8] & (int)Math.Pow(2.0, num6 % 8)) > 0)
				{
					chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, num7, 1);
				}
				else
				{
					chData[num6 + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, num7, 0);
				}
			}
		}
		for (int num8 = 0; num8 < 3; num8++)
		{
			num3 = (array[num2 + 10734 + num8 * 2] << 8) + array[num2 + 10735 + num8 * 2];
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Fader, 0, num3);
			chData[chOffsetStereo + num8].setParam("LOGIC", Channel.Pr.On, 0, (array[num2 + 10730 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0);
			Channel obj4 = chData[num8 + chOffsetStereo];
			string text4 = Encoding.ASCII.GetString(array, num2 + 14636 + num8 * 4, 4) + Encoding.ASCII.GetString(array, num2 + 14648 + num8 * 4, 4);
			char[] trimChars5 = new char[1];
			obj4.setParam("LOGIC", Channel.Pr.Name, 0, text4.Trim(trimChars5));
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1On, 0, (array[num2 + 10752 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, array[num2 + 10753 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, array[num2 + 10771 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, array[num2 + 10780 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, array[num2 + 10783 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, array[num2 + 10786 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, array[num2 + 10789 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, (short)(array[num2 + 10792 + num8 * 2] << 8) + array[num2 + 10793 + num8 * 2]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQType, 0, ((array[num2 + 10802 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0) ? 1 : 0);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQOn, 0, (array[num2 + 10803 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQAtt, 0, (short)(array[num2 + 10742 + num8 * 2] << 8) + array[num2 + 10743 + num8 * 2]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1Type, 0, array[num2 + 10804 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4Type, 0, array[num2 + 10807 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, (array[num2 + 10810 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, (array[num2 + 10811 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1Q, 0, (sbyte)array[num2 + 10813 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2Q, 0, (sbyte)array[num2 + 10827 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3Q, 0, (sbyte)array[num2 + 10841 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4Q, 0, (sbyte)array[num2 + 10855 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1F, 0, array[num2 + 10816 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2F, 0, array[num2 + 10830 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3F, 0, array[num2 + 10844 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4F, 0, array[num2 + 10858 + num8]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ1G, 0, (short)(array[num2 + 10820 + num8 * 2] << 8) + array[num2 + 10821 + num8 * 2]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ2G, 0, (short)(array[num2 + 10834 + num8 * 2] << 8) + array[num2 + 10835 + num8 * 2]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ3G, 0, (short)(array[num2 + 10848 + num8 * 2] << 8) + array[num2 + 10849 + num8 * 2]);
			chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.EQ4G, 0, (short)(array[num2 + 10862 + num8 * 2] << 8) + array[num2 + 10863 + num8 * 2]);
			for (int num9 = 0; num9 < 8; num9++)
			{
				if ((array[num2 + 10948 + num9 + num8 / 8] & (int)Math.Pow(2.0, num8 % 8)) > 0)
				{
					chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, num9, 1);
				}
				else
				{
					chData[num8 + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, num9, 0);
				}
			}
		}
		for (int num10 = 0; num10 < 8; num10++)
		{
			setParam("LOGIC", num10, Pr.RackType, 0, array[num2 + 11104 + num10]);
			setParam("LOGIC", num10, Pr.GEQOn, 0, ((array[num2 + 11129 + num10 / 8] & (int)Math.Pow(2.0, num10 % 8)) > 0) ? 1 : 0);
			setParam("LOGIC", num10 + 8, Pr.GEQOn, 0, ((array[num2 + 11130 + num10 / 8] & (int)Math.Pow(2.0, num10 % 8)) > 0) ? 1 : 0);
			setParam("LOGIC", num10, Pr.RackSource, 0, array[num2 + 14424 + num10]);
			setParam("LOGIC", num10 + 8, Pr.RackSource, 0, array[num2 + 14424 + num10 + 8]);
			setParam("LOGIC", num10, Pr.GEQLink, 0, array[num2 + 11113 + num10]);
			setParam("LOGIC", num10, Pr.GEQLinkLR, 0, array[num2 + 11121 + num10]);
			setParam("LOGIC", num10 + 8, Pr.GEQLinkLR, 0, array[num2 + 11121 + num10]);
			for (int num11 = 0; num11 < 31; num11++)
			{
				setParam("LOGIC", num10, Pr.GEQBand, num11, (short)(array[num2 + 11150 + num10 * 2 + num11 * 16] << 8) + array[num2 + 11151 + num10 * 2 + num11 * 16]);
			}
			for (int num12 = 0; num12 < 31; num12++)
			{
				setParam("LOGIC", num10 + 8, Pr.GEQBand, num12, (short)(array[num2 + 11646 + num10 * 2 + num12 * 16] << 8) + array[num2 + 11647 + num10 * 2 + num12 * 16]);
			}
		}
		for (int num13 = 0; num13 < 8; num13++)
		{
			num3 = (array[num2 + 10998 + num13 * 2] << 8) + array[num2 + 10999 + num13 * 2];
			if (num3 >= 0 && num3 < dbMap.Length)
			{
				DCA[num13].setFader(num3, "console");
			}
			DCA[num13].setMute((array[num2 + 10964 + num13 / 8] & (int)Math.Pow(2.0, num13 % 8)) > 0, "console");
		}
		for (int num14 = 0; num14 < 8; num14++)
		{
			muteGroup[num14].muted = (array[num2 + 522 + num14 / 8] & (int)Math.Pow(2.0, num14 % 8)) > 0;
		}
		chGroup[0].att = (array[264 + num2] & (int)Math.Pow(2.0, 0.0)) > 0;
		chGroup[0].ha = (array[265 + num2] & (int)Math.Pow(2.0, 0.0)) > 0;
		chGroup[0].dyn1 = (array[265 + num2] & (int)Math.Pow(2.0, 1.0)) > 0;
		chGroup[0].dyn2 = (array[265 + num2] & (int)Math.Pow(2.0, 2.0)) > 0;
		chGroup[0].eq = (array[265 + num2] & (int)Math.Pow(2.0, 3.0)) > 0;
		chGroup[0].on = (array[265 + num2] & (int)Math.Pow(2.0, 4.0)) > 0;
		chGroup[0].fader = (array[265 + num2] & (int)Math.Pow(2.0, 5.0)) > 0;
		chGroup[0].insert = (array[265 + num2] & (int)Math.Pow(2.0, 6.0)) > 0;
		chGroup[0].directout = (array[265 + num2] & (int)Math.Pow(2.0, 7.0)) > 0;
		chGroup[0].mixLevelSend = (array[327 + num2] & (int)Math.Pow(2.0, 0.0)) > 0;
		chGroup[0].mixLevelOn = (array[327 + num2] & (int)Math.Pow(2.0, 1.0)) > 0;
		chGroup[0].matrixLevelSend = (array[327 + num2] & (int)Math.Pow(2.0, 2.0)) > 0;
		chGroup[0].matrixLevelOn = (array[327 + num2] & (int)Math.Pow(2.0, 3.0)) > 0;
		chGroup[0].mixSend[0] = (array[327 + num2] & (int)Math.Pow(2.0, 4.0)) > 0;
		chGroup[0].mixSend[1] = (array[327 + num2] & (int)Math.Pow(2.0, 5.0)) > 0;
		chGroup[0].mixSend[2] = (array[327 + num2] & (int)Math.Pow(2.0, 6.0)) > 0;
		chGroup[0].mixSend[3] = (array[327 + num2] & (int)Math.Pow(2.0, 7.0)) > 0;
		chGroup[0].mixSend[4] = (array[326 + num2] & (int)Math.Pow(2.0, 0.0)) > 0;
		chGroup[0].mixSend[5] = (array[326 + num2] & (int)Math.Pow(2.0, 1.0)) > 0;
		chGroup[0].mixSend[6] = (array[326 + num2] & (int)Math.Pow(2.0, 2.0)) > 0;
		chGroup[0].mixSend[7] = (array[326 + num2] & (int)Math.Pow(2.0, 3.0)) > 0;
		chGroup[0].mixSend[8] = (array[326 + num2] & (int)Math.Pow(2.0, 4.0)) > 0;
		chGroup[0].mixSend[9] = (array[326 + num2] & (int)Math.Pow(2.0, 5.0)) > 0;
		chGroup[0].mixSend[10] = (array[326 + num2] & (int)Math.Pow(2.0, 6.0)) > 0;
		chGroup[0].mixSend[11] = (array[326 + num2] & (int)Math.Pow(2.0, 7.0)) > 0;
		chGroup[0].mixSend[12] = (array[325 + num2] & (int)Math.Pow(2.0, 0.0)) > 0;
		chGroup[0].mixSend[13] = (array[325 + num2] & (int)Math.Pow(2.0, 1.0)) > 0;
		chGroup[0].mixSend[14] = (array[325 + num2] & (int)Math.Pow(2.0, 2.0)) > 0;
		chGroup[0].mixSend[15] = (array[325 + num2] & (int)Math.Pow(2.0, 3.0)) > 0;
		chGroup[0].matrixSend[0] = (array[325 + num2] & (int)Math.Pow(2.0, 4.0)) > 0;
		chGroup[0].matrixSend[1] = (array[325 + num2] & (int)Math.Pow(2.0, 5.0)) > 0;
		chGroup[0].matrixSend[2] = (array[325 + num2] & (int)Math.Pow(2.0, 6.0)) > 0;
		chGroup[0].matrixSend[3] = (array[325 + num2] & (int)Math.Pow(2.0, 7.0)) > 0;
		chGroup[0].matrixSend[4] = (array[324 + num2] & (int)Math.Pow(2.0, 0.0)) > 0;
		chGroup[0].matrixSend[5] = (array[324 + num2] & (int)Math.Pow(2.0, 1.0)) > 0;
		chGroup[0].matrixSend[6] = (array[324 + num2] & (int)Math.Pow(2.0, 2.0)) > 0;
		chGroup[0].matrixSend[7] = (array[324 + num2] & (int)Math.Pow(2.0, 3.0)) > 0;
		MixEngine.NewMessage("Sync Complete", 0);
		connPhase = ConnectionPhase.PostSync;
		setParam("LOGIC", 0, Pr.None, 0, 1);
		return true;
	}

	private static void processSysEx(string msgData)
	{
		int num = 0;
		int num2 = 0;
		int group = 0;
		string text = "";
		string text2 = "";
		string text3 = "";
		bool flag = false;
		try
		{
			if (connStatus == ConnectionStatus.Bad)
			{
				MixEngine.NewMessage("Connection restored.", -98);
				connStatus = ConnectionStatus.Connected;
			}
			else if (connStatus == ConnectionStatus.Disconnected)
			{
				MixEngine.NewMessage("Late Data Received.", 0);
				return;
			}
			heartBeats = 0;
			switch (msgData)
			{
			case "F043103E127FF7":
				MixEngine.NewMessage("Connected", 0);
				if (Console.type != 0)
				{
					setConsoleType("Yamaha LS9-32");
				}
				if (connPhase == ConnectionPhase.PreSync)
				{
					connPhase = ConnectionPhase.Syncing;
					reqParam("StereoMonoLink", 0, 0);
					Thread.Sleep(250);
					enQueue(Pr.SceneDump, 0, 0);
					MixEngine.NewMessage("Syncing...", 0);
				}
				if (connPhase == ConnectionPhase.PostSync)
				{
					reqParam("LibNumDump", 0);
					reqParam("OutPortDump", 0);
					reqParam("CueMode", 0);
					reqParam("Lamp", 0);
					reqParam("MonitorLevel", 0);
					reqParam("MonitorOn", 0);
					reqParam("PrefNaviToSelIn", 0);
					reqParam("PrefNaviToSelOut", 0);
					reqParam("PrefCueSelLink", 0);
					reqParam("SelChannel", 0);
					reqParam("ClockSpeed", 0);
					if (Console.sceneHigh == 300)
					{
						reqParam("SceneRange", 0, 0);
					}
					connPhase = ConnectionPhase.Synced;
					MixEngine.NewMessage("Post Sync...", 0);
				}
				flag = true;
				break;
			case "F043103E117FF7":
				MixEngine.NewMessage("Connected", 0);
				if (Console.type != ConsoleTypes.M7CL)
				{
					setConsoleType("Yamaha M7CL-48");
				}
				if (connPhase == ConnectionPhase.PreSync)
				{
					enQueue(Pr.SceneDump, 0, 0);
					connPhase = ConnectionPhase.Syncing;
				}
				if (connPhase == ConnectionPhase.PostSync)
				{
					reqParam("LibNumDump", 0);
					reqParam("OutPortDump", 0);
					reqParam("CueMode", 0);
					reqParam("Lamp", 0);
					reqParam("MonitorLevel", 0);
					reqParam("MonitorOn", 0);
					reqParam("PrefNaviToSelIn", 0);
					reqParam("PrefNaviToSelOut", 0);
					reqParam("PrefCueSelLink", 0);
					reqParam("SelChannel", 0);
					reqParam("ClockSpeed", 0);
					if (Console.sceneHigh == 300)
					{
						reqParam("SceneRange", 0, 0);
					}
					connPhase = ConnectionPhase.Synced;
					MixEngine.NewMessage("Post Sync...", 0);
				}
				flag = true;
				break;
			case "F043103E0D7FF7":
				MixEngine.NewMessage("Connected", 0);
				if (Console.type != ConsoleTypes.O1V96V2 || Console.ver != ConsoleVersions.O1V96V2)
				{
					setConsoleType("Yamaha 01V96");
				}
				if (connPhase == ConnectionPhase.PreSync)
				{
					reqParam("CueMode", 0);
					reqParam("SelChannel", 0);
					reqParam("ClockSpeed", 0);
					connPhase = ConnectionPhase.PostSync;
					reqParam("slowdump", 0);
					MixEngine.NewMessage("Syncing...", 0);
				}
				_ = connPhase;
				_ = 4;
				flag = true;
				break;
			case "F043103E1A7FF7":
				MixEngine.NewMessage("Connected", 0);
				if (Console.type != ConsoleTypes.O1V96V2 || Console.ver != ConsoleVersions.O1V96i)
				{
					setConsoleType("Yamaha 01V96i");
				}
				if (connPhase == ConnectionPhase.PreSync)
				{
					reqParam("CueMode", 0);
					reqParam("SelChannel", 0);
					reqParam("ClockSpeed", 0);
					connPhase = ConnectionPhase.PostSync;
					reqParam("slowdump", 0);
					MixEngine.NewMessage("Syncing...", 0);
				}
				_ = connPhase;
				_ = 4;
				flag = true;
				break;
			}
			if (flag)
			{
				return;
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E1221") || msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E1121") || msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E0D21") || msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E1A21"))
			{
				int num3 = Convert.ToInt32(msgData.Substring(12, 2), 16);
				int num4 = Convert.ToInt32(msgData.Substring(14, 2), 16);
				num = Convert.ToInt32(msgData.Substring(16, 2), 16);
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (num3)
					{
					case 2:
						num = chOffsetMix + num;
						break;
					case 1:
						num = chOffsetMatrix + num;
						break;
					case 4:
						num = chOffsetStereo + num;
						break;
					}
				}
				else
				{
					switch (num3)
					{
					case 1:
						num = chOffsetMix + num;
						break;
					case 2:
						num = chOffsetMatrix + num;
						break;
					case 3:
						num = chOffsetStereo + num;
						break;
					}
				}
				if (num >= 127)
				{
					num = 0;
				}
				ChannelParam channelParam = new ChannelParam();
				channelParam.param = ChParam.CH_METERPRE;
				channelParam.channel = num;
				channelParam.index = 0;
				ChannelParam channelParam2 = new ChannelParam();
				channelParam2.param = ChParam.CH_METEREQIN;
				channelParam2.channel = num;
				channelParam2.index = 0;
				ChannelParam channelParam3 = new ChannelParam();
				channelParam3.param = ChParam.CH_METEREQOUT;
				channelParam3.channel = num;
				channelParam3.index = 0;
				ChannelParam channelParam4 = new ChannelParam();
				channelParam4.param = ChParam.CH_METERDYN0IN;
				channelParam4.channel = num;
				channelParam4.index = 0;
				ChannelParam channelParam5 = new ChannelParam();
				channelParam5.param = ChParam.CH_METERDYN1IN;
				channelParam5.channel = num;
				channelParam5.index = 0;
				ChannelParam channelParam6 = new ChannelParam();
				channelParam6.param = ChParam.CH_METERPOST;
				channelParam6.channel = num;
				channelParam6.index = 0;
				ChannelParam channelParam7 = new ChannelParam();
				channelParam7.param = ChParam.CH_METERDYN0GR;
				channelParam7.channel = num;
				channelParam7.index = 0;
				ChannelParam channelParam8 = new ChannelParam();
				channelParam8.param = ChParam.CH_METERDYN1GR;
				channelParam8.channel = num;
				channelParam8.index = 0;
				for (int i = 18; i < msgData.Length - 2; i += 2)
				{
					if (num4 == 127)
					{
						if (Console.type == ConsoleTypes.O1V96V2)
						{
							if (num3 == 0)
							{
								for (int j = 0; j < 6; j++)
								{
									num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(i, 4)), 16);
									num2 = (int)Math.Round((double)(num2 - 37) * 0.031170805879371516);
									if (num2 < 0)
									{
										num2 = 0;
									}
									if (num2 > 127)
									{
										num2 = 127;
									}
									i += 4;
									switch (j)
									{
									case 0:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
										channelParam.value.Add(num2);
										chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, num2);
										channelParam4.value.Add(num2);
										break;
									case 1:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
										channelParam5.value.Add(num2);
										chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
										channelParam3.value.Add(num2);
										break;
									case 2:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
										channelParam6.value.Add(num2);
										break;
									case 3:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
										channelParam8.value.Add(num2);
										break;
									case 4:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
										channelParam2.value.Add(num2);
										break;
									case 5:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, num2);
										channelParam7.value.Add(num2);
										break;
									}
									if (i >= msgData.Length)
									{
										logError("ProcessSysEx()", "InEvent", "Meter Data Count Out of Range!");
										return;
									}
								}
								i -= 2;
							}
							else
							{
								for (int k = 0; k < 4; k++)
								{
									num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(i, 4)), 16);
									num2 = (int)Math.Round((double)(num2 - 37) * 0.031170805879371516);
									if (num2 < 0)
									{
										num2 = 0;
									}
									if (num2 > 127)
									{
										num2 = 127;
									}
									i += 4;
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, 0);
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, 0);
									switch (k)
									{
									case 0:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
										channelParam.value.Add(num2);
										chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
										channelParam2.value.Add(num2);
										break;
									case 1:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
										channelParam5.value.Add(num2);
										chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
										channelParam3.value.Add(num2);
										break;
									case 2:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
										channelParam6.value.Add(num2);
										break;
									case 3:
										chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
										channelParam8.value.Add(num2);
										break;
									}
									if (i >= msgData.Length)
									{
										logError("ProcessSysEx()", "InEvent", "Meter Data Count Out of Range!");
										return;
									}
								}
								i -= 2;
							}
						}
						else if (num3 == 0)
						{
							for (int l = 0; l < 9; l++)
							{
								num2 = Convert.ToInt32(msgData.Substring(i, 2), 16);
								switch (l)
								{
								case 0:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
									channelParam.value.Add(num2);
									break;
								case 1:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
									channelParam2.value.Add(num2);
									break;
								case 2:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
									channelParam3.value.Add(num2);
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, num2);
									channelParam4.value.Add(num2);
									break;
								case 3:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
									channelParam5.value.Add(num2);
									break;
								case 5:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
									channelParam6.value.Add(num2);
									break;
								case 7:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, num2);
									channelParam7.value.Add(num2);
									break;
								case 8:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
									channelParam8.value.Add(num2);
									break;
								}
								i += 2;
								if (i >= msgData.Length)
								{
									logError("ProcessSysEx()", "InEvent", "Meter Data Count Out of Range!");
									return;
								}
							}
							i -= 2;
						}
						else
						{
							for (int m = 0; m < 6; m++)
							{
								num2 = Convert.ToInt32(msgData.Substring(i, 2), 16);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, 0);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, 0);
								switch (m)
								{
								case 0:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
									channelParam.value.Add(num2);
									chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
									channelParam2.value.Add(num2);
									break;
								case 1:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
									channelParam3.value.Add(num2);
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
									channelParam5.value.Add(num2);
									break;
								case 3:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
									channelParam6.value.Add(num2);
									break;
								case 5:
									chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
									channelParam8.value.Add(num2);
									break;
								}
								i += 2;
								if (i >= msgData.Length)
								{
									logError("ProcessSysEx()", "InEvent", "Meter Data Count Out of Range!");
									return;
								}
							}
							i -= 2;
						}
					}
					else if (Console.type == ConsoleTypes.O1V96V2)
					{
						num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(i, 4)), 16);
						num2 = (int)Math.Round((double)(num2 - 37) * 0.031170805879371516);
						if (num2 < 0)
						{
							num2 = 0;
						}
						if (num2 > 127)
						{
							num2 = 127;
						}
						i += 2;
						if (num3 == 0)
						{
							switch (num4)
							{
							case 0:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
								channelParam.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, num2);
								channelParam4.value.Add(num2);
								break;
							case 1:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
								channelParam5.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
								channelParam3.value.Add(num2);
								break;
							case 2:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
								channelParam6.value.Add(num2);
								break;
							case 3:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
								channelParam8.value.Add(num2);
								break;
							case 4:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
								channelParam2.value.Add(num2);
								break;
							case 5:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, num2);
								channelParam7.value.Add(num2);
								break;
							}
						}
						else
						{
							chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, 0);
							chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, 0);
							switch (num4)
							{
							case 0:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
								channelParam.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
								channelParam2.value.Add(num2);
								break;
							case 1:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
								channelParam5.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
								channelParam3.value.Add(num2);
								break;
							case 2:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
								channelParam6.value.Add(num2);
								break;
							case 3:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
								channelParam8.value.Add(num2);
								break;
							}
						}
					}
					else
					{
						num2 = Convert.ToInt32(msgData.Substring(i, 2), 16);
						if (num3 == 0)
						{
							switch (num4)
							{
							case 0:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
								channelParam.value.Add(num2);
								break;
							case 1:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
								channelParam2.value.Add(num2);
								break;
							case 2:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
								channelParam3.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, num2);
								channelParam4.value.Add(num2);
								break;
							case 3:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
								channelParam5.value.Add(num2);
								break;
							case 5:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
								channelParam6.value.Add(num2);
								break;
							case 7:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, num2);
								channelParam7.value.Add(num2);
								break;
							case 8:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
								channelParam8.value.Add(num2);
								break;
							}
						}
						else
						{
							chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0In, 0, 0);
							chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn0GR, 0, 0);
							switch (num4)
							{
							case 0:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPre, 0, num2);
								channelParam.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQIn, 0, num2);
								channelParam2.value.Add(num2);
								break;
							case 1:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterEQOut, 0, num2);
								channelParam3.value.Add(num2);
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1In, 0, num2);
								channelParam5.value.Add(num2);
								break;
							case 3:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterPost, 0, num2);
								channelParam6.value.Add(num2);
								break;
							case 5:
								chData[num].setParam("CONSOLE", Channel.Pr.MeterDyn1GR, 0, num2);
								channelParam8.value.Add(num2);
								break;
							}
						}
					}
					num++;
				}
				PushChannel(Channel.Pr.MeterPre, 0, 0, 0);
				if (channelParam.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam);
				}
				if (channelParam2.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam2);
				}
				if (channelParam3.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam3);
				}
				if (channelParam6.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam6);
				}
				if (channelParam4.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam4);
				}
				if (channelParam5.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam5);
				}
				if (channelParam7.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam7);
				}
				if (channelParam8.value.Count > 0)
				{
					faderServer.sendProto("CONSOLE", channelParam8);
				}
				return;
			}
			logDebug(3, "CONS: " + msgData);
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E1201") && msgData.Length == 36)
			{
				num = Convert.ToInt32(msgData.Substring(20, 4), 16);
				num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(24, 10)), 16);
				switch (msgData.Substring(12, 8))
				{
				case "010B0000":
					chData[num].setParam("CONSOLE", Channel.Pr.PatchIn, 0, num2);
					flag = true;
					break;
				case "002A0000":
				case "032D0000":
					chData[num].setParam("CONSOLE", Channel.Pr.Phantom, 0, num2);
					flag = true;
					break;
				case "002A0001":
				case "032D0001":
					chData[num].setParam("CONSOLE", Channel.Pr.HA, 0, num2);
					flag = true;
					break;
				case "002E0000":
					chData[num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					flag = true;
					break;
				case "002F0000":
					chData[num].setParam("CONSOLE", Channel.Pr.DirectOutOn, 0, num2);
					flag = true;
					break;
				case "00300000":
					chData[num].setParam("CONSOLE", Channel.Pr.Phase, 0, num2);
					flag = true;
					break;
				case "00310000":
					chData[num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					flag = true;
					break;
				case "00320001":
					chData[num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					flag = true;
					break;
				case "00330000":
					chData[num].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					flag = true;
					break;
				case "00410000":
					chData[num].setParam("CONSOLE", Channel.Pr.ToStereoOn, 0, num2);
					flag = true;
					break;
				case "00420000":
					chData[num].setParam("CONSOLE", Channel.Pr.ToMonoOn, 0, num2);
					flag = true;
					break;
				case "00370000":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0On, 0, num2);
					flag = true;
					break;
				case "00370001":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Type, 0, num2);
					flag = true;
					break;
				case "00370009":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Attack, 0, num2);
					flag = true;
					break;
				case "0037000A":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Range, 0, num2);
					flag = true;
					break;
				case "0037000B":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Hold, 0, num2);
					flag = true;
					break;
				case "0037000C":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Decay, 0, num2);
					flag = true;
					break;
				case "0037000D":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Ratio, 0, num2);
					flag = true;
					break;
				case "0037000E":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Gain, 0, num2);
					flag = true;
					break;
				case "0037000F":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Knee, 0, num2);
					flag = true;
					break;
				case "00370010":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Threshold, 0, num2);
					flag = true;
					break;
				case "003A0000":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					flag = true;
					break;
				case "003A0001":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					flag = true;
					break;
				case "003A0009":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					flag = true;
					break;
				case "003A000A":
					flag = true;
					break;
				case "003A000B":
					flag = true;
					break;
				case "003A000C":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					flag = true;
					break;
				case "003A000D":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					flag = true;
					break;
				case "003A000E":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					flag = true;
					break;
				case "003A000F":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					flag = true;
					break;
				case "003A0010":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					flag = true;
					break;
				case "00340001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					flag = true;
					break;
				case "003C0000":
					chData[num].setParam("CONSOLE", Channel.Pr.EQHPFOn, 0, num2);
					flag = true;
					break;
				case "003C0001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQHPFFreq, 0, num2);
					flag = true;
					break;
				case "003D0000":
					chData[num].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, num2);
					flag = true;
					break;
				case "003D0001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					flag = true;
					break;
				case "003D0002":
					chData[num].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					flag = true;
					break;
				case "003D0003":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					flag = true;
					break;
				case "003D0004":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					flag = true;
					break;
				case "003D0005":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					flag = true;
					break;
				case "003D0006":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					flag = true;
					break;
				case "003D0008":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					flag = true;
					break;
				case "003D0009":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					flag = true;
					break;
				case "003D000A":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					flag = true;
					break;
				case "003D000C":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					flag = true;
					break;
				case "003D000D":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					flag = true;
					break;
				case "003D000E":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					flag = true;
					break;
				case "003D0010":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					flag = true;
					break;
				case "003D0011":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					flag = true;
					break;
				case "003D0012":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					flag = true;
					break;
				case "003D0014":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					flag = true;
					break;
				case "003D0015":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					flag = true;
					break;
				case "003D0016":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					flag = true;
					break;
				case "00220000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
					break;
				case "00230000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.BusType, 0, num2);
					break;
				case "00240000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.PanLink, 0, num2);
					break;
				case "004B0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					break;
				case "004C0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "004D0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Balance, 0, num2);
					break;
				case "004E0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					break;
				case "004F0001":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					break;
				case "00570000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.ToStereoOn, 0, num2);
					break;
				case "00570002":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					break;
				case "00580000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.ToMonoOn, 0, num2);
					break;
				case "00520000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					break;
				case "00520001":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					break;
				case "00520009":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					break;
				case "0052000C":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					break;
				case "0052000D":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					break;
				case "0052000E":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					break;
				case "0052000F":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					break;
				case "00520010":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					break;
				case "00540000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "00540001":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					break;
				case "00540002":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					break;
				case "00540003":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					break;
				case "00540004":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					break;
				case "00540005":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					break;
				case "00540006":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					break;
				case "00540008":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					break;
				case "00540009":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					break;
				case "0054000A":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					break;
				case "0054000C":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					break;
				case "0054000D":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					break;
				case "0054000E":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					break;
				case "00540010":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					break;
				case "00540011":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					break;
				case "00540012":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					break;
				case "00540014":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					break;
				case "00540015":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					break;
				case "00540016":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					break;
				case "00250000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
					break;
				case "00260000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.BusType, 0, num2);
					break;
				case "00270000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.PanLink, 0, num2);
					break;
				case "005C0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					break;
				case "005D0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "005E0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Balance, 0, num2);
					break;
				case "005F0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					break;
				case "00600001":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					break;
				case "00630000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					break;
				case "00630001":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					break;
				case "00630009":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					break;
				case "0063000C":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					break;
				case "0063000D":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					break;
				case "0063000E":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					break;
				case "0063000F":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					break;
				case "00630010":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					break;
				case "00650000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "00650001":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					break;
				case "00650002":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					break;
				case "00650003":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					break;
				case "00650004":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					break;
				case "00650005":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					break;
				case "00650006":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					break;
				case "00650008":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					break;
				case "00650009":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					break;
				case "0065000A":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					break;
				case "0065000C":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					break;
				case "0065000D":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					break;
				case "0065000E":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					break;
				case "00650010":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					break;
				case "00650011":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					break;
				case "00650012":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					break;
				case "00650014":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					break;
				case "00650015":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					break;
				case "00650016":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					break;
				case "006A0000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					break;
				case "006B0000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "006C0000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					break;
				case "006D0000":
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					break;
				case "006E0001":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					break;
				case "00710000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					break;
				case "00710001":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					break;
				case "00710009":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					break;
				case "0071000C":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					break;
				case "0071000D":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					break;
				case "0071000E":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					break;
				case "0071000F":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					break;
				case "00710010":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					break;
				case "00730000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "00730001":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					break;
				case "00730002":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					break;
				case "00730003":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					break;
				case "00730004":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					break;
				case "00730005":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					break;
				case "00730006":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					break;
				case "00730008":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					break;
				case "00730009":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					break;
				case "0073000A":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					break;
				case "0073000C":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					break;
				case "0073000D":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					break;
				case "0073000E":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					break;
				case "00730010":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					break;
				case "00730011":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					break;
				case "00730012":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					break;
				case "00730014":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					break;
				case "00730015":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					break;
				case "00730016":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					break;
				case "02080000":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, num2);
					flag = true;
					break;
				case "021E0000":
					OutPort[num + 16].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, num2);
					flag = true;
					break;
				case "021E0002":
					OutPort[num + 16].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, num2);
					flag = true;
					break;
				case "021F0000":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, num2);
					flag = true;
					break;
				case "021F0002":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, num2);
					flag = true;
					break;
				case "02200000":
					OutPort[num + 48].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, num2);
					flag = true;
					break;
				case "02200002":
					OutPort[num + 48].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, num2);
					flag = true;
					break;
				case "02230000":
					OutPort[num + 16].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, num2);
					flag = true;
					break;
				case "02240000":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, num2);
					flag = true;
					break;
				case "02250000":
					OutPort[num + 48].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, num2);
					flag = true;
					break;
				case "00120000":
					for (group = 0; group < 39; group++)
					{
						if ((num2 & (int)Math.Pow(2.0, 0.0)) > 0)
						{
							if (!chGroup[0].ha)
							{
								foreach (int member in chGroup[group].members)
								{
									chData[member].setParam("LOGIC", Channel.Pr.RelativeHA, 0, chData[member].getParam(Channel.Pr.HA));
								}
							}
							chGroup[0].ha = true;
						}
						else
						{
							chGroup[0].ha = false;
						}
						if ((num2 & (int)Math.Pow(2.0, 5.0)) > 0)
						{
							if (!chGroup[0].fader)
							{
								foreach (int member2 in chGroup[group].members)
								{
									chData[member2].setParam("LOGIC", Channel.Pr.RelativeFader, 0, chData[member2].getParam(Channel.Pr.Fader));
								}
							}
							chGroup[0].fader = true;
						}
						else
						{
							chGroup[0].fader = false;
						}
						int num8 = 0;
						if (chGroup[group].members.Count > 0)
						{
							num8 = chGroup[group].members[0];
						}
						chGroup[0].dyn1 = (num2 & (int)Math.Pow(2.0, 1.0)) > 0;
						if (chGroup[0].dyn1)
						{
							foreach (int member3 in chGroup[group].members)
							{
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0On, 0, chData[num8].getParam(Channel.Pr.Dyn0On));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Type, 0, chData[num8].getParam(Channel.Pr.Dyn0Type));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Attack, 0, chData[num8].getParam(Channel.Pr.Dyn0Attack));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Range, 0, chData[num8].getParam(Channel.Pr.Dyn0Range));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Hold, 0, chData[num8].getParam(Channel.Pr.Dyn0Hold));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Decay, 0, chData[num8].getParam(Channel.Pr.Dyn0Decay));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Ratio, 0, chData[num8].getParam(Channel.Pr.Dyn0Ratio));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Gain, 0, chData[num8].getParam(Channel.Pr.Dyn0Gain));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Knee, 0, chData[num8].getParam(Channel.Pr.Dyn0Knee));
								chData[member3].setParam("LOGIC", Channel.Pr.Dyn0Threshold, 0, chData[num8].getParam(Channel.Pr.Dyn0Threshold));
							}
						}
						chGroup[0].dyn2 = (num2 & (int)Math.Pow(2.0, 2.0)) > 0;
						if (chGroup[0].dyn2)
						{
							foreach (int member4 in chGroup[group].members)
							{
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1On, 0, chData[num8].getParam(Channel.Pr.Dyn1On));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, chData[num8].getParam(Channel.Pr.Dyn1Type));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, chData[num8].getParam(Channel.Pr.Dyn1Attack));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Range, 0, chData[num8].getParam(Channel.Pr.Dyn1Range));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Hold, 0, chData[num8].getParam(Channel.Pr.Dyn1Hold));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, chData[num8].getParam(Channel.Pr.Dyn1Release));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, chData[num8].getParam(Channel.Pr.Dyn1Ratio));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, chData[num8].getParam(Channel.Pr.Dyn1Gain));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, chData[num8].getParam(Channel.Pr.Dyn1Knee));
								chData[member4].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, chData[num8].getParam(Channel.Pr.Dyn1Threshold));
							}
						}
						chGroup[0].eq = (num2 & (int)Math.Pow(2.0, 3.0)) > 0;
						if (chGroup[0].eq)
						{
							foreach (int member5 in chGroup[group].members)
							{
								chData[member5].setParam("LOGIC", Channel.Pr.EQType, 0, chData[num8].getParam(Channel.Pr.EQType));
								chData[member5].setParam("LOGIC", Channel.Pr.EQOn, 0, chData[num8].getParam(Channel.Pr.EQOn));
								chData[member5].setParam("LOGIC", Channel.Pr.EQHPFOn, 0, chData[num8].getParam(Channel.Pr.EQHPFOn));
								chData[member5].setParam("LOGIC", Channel.Pr.EQHPFFreq, 0, chData[num8].getParam(Channel.Pr.EQHPFFreq));
								chData[member5].setParam("LOGIC", Channel.Pr.EQAtt, 0, chData[num8].getParam(Channel.Pr.EQAtt));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ1Type, 0, chData[num8].getParam(Channel.Pr.EQ1Type));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, chData[num8].getParam(Channel.Pr.EQ1HPFOn));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ4Type, 0, chData[num8].getParam(Channel.Pr.EQ4Type));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, chData[num8].getParam(Channel.Pr.EQ4LPFOn));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ1Q, 0, chData[num8].getParam(Channel.Pr.EQ1Q));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ1F, 0, chData[num8].getParam(Channel.Pr.EQ1F));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ1G, 0, chData[num8].getParam(Channel.Pr.EQ1G));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ2Q, 0, chData[num8].getParam(Channel.Pr.EQ2Q));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ2F, 0, chData[num8].getParam(Channel.Pr.EQ2F));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ2G, 0, chData[num8].getParam(Channel.Pr.EQ2G));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ3Q, 0, chData[num8].getParam(Channel.Pr.EQ3Q));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ3F, 0, chData[num8].getParam(Channel.Pr.EQ3F));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ3G, 0, chData[num8].getParam(Channel.Pr.EQ3G));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ4Q, 0, chData[num8].getParam(Channel.Pr.EQ4Q));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ4F, 0, chData[num8].getParam(Channel.Pr.EQ4F));
								chData[member5].setParam("LOGIC", Channel.Pr.EQ4G, 0, chData[num8].getParam(Channel.Pr.EQ4G));
							}
						}
						chGroup[0].on = (num2 & (int)Math.Pow(2.0, 4.0)) > 0;
						if (!chGroup[0].on)
						{
							continue;
						}
						foreach (int member6 in chGroup[group].members)
						{
							chData[member6].setParam("CONSOLE", Channel.Pr.On, 0, chData[num8].getParam(Channel.Pr.On));
						}
					}
					break;
				case "00120001":
					for (group = 0; group < 39; group++)
					{
						int num5 = 0;
						if (chGroup[group].members.Count > 0)
						{
							num5 = chGroup[group].members[0];
						}
						for (int n = 0; n < Console.maxMIX; n++)
						{
							chGroup[0].mixSend[n] = (num2 & (int)Math.Pow(2.0, n + 4)) > 0;
						}
						chGroup[0].mixLevelSend = (num2 & (int)Math.Pow(2.0, 0.0)) > 0;
						if (chGroup[0].mixLevelSend)
						{
							foreach (int member7 in chGroup[group].members)
							{
								for (int num6 = 0; num6 < Console.maxMIX; num6++)
								{
									if (chGroup[0].mixSend[num6])
									{
										chData[member7].setParam("LOGIC", Channel.Pr.SendToMix, num6, chData[num5].getParam(Channel.Pr.SendToMix, num6));
									}
								}
							}
						}
						chGroup[0].mixLevelOn = (num2 & (int)Math.Pow(2.0, 1.0)) > 0;
						if (!chGroup[0].mixLevelOn)
						{
							continue;
						}
						foreach (int member8 in chGroup[group].members)
						{
							for (int num7 = 0; num7 < Console.maxMIX; num7++)
							{
								if (chGroup[0].mixSend[num7])
								{
									chData[member8].setParam("LOGIC", Channel.Pr.SendToMixOn, num7, chData[num5].getParam(Channel.Pr.SendToMixOn, num7));
								}
							}
						}
					}
					break;
				case "00210000":
					setParam("CONSOLE", num, Pr.MuteMaster, 0, num2);
					flag = true;
					break;
				case "01060000":
					if (num2 >= 0 && num2 <= 38)
					{
						chData[num].setParam("CONSOLE", Channel.Pr.Group, 0, num2);
					}
					flag = true;
					break;
				case "01240000":
					group = Convert.ToInt32(msgData.Substring(22, 2), 16);
					switch (group)
					{
					case 41:
						Console.PrefNaviToSelIn = num2 == 1;
						break;
					case 42:
						Console.PrefNaviToSelOut = num2 == 1;
						break;
					case 46:
						Console.PrefCueSelLink = num2 == 1;
						break;
					case 48:
						Console.LinkSTMono = num2 == 1;
						if (num2 == 1)
						{
							chData[chOffsetStereo].setParam("LOGIC", Channel.Pr.RelativeFader, 0, chData[chOffsetStereo].getParam(Channel.Pr.Fader));
							chData[chOffsetStereo + 1].setParam("LOGIC", Channel.Pr.RelativeFader, 0, chData[chOffsetStereo + 1].getParam(Channel.Pr.Fader));
							chData[chOffsetStereo + 2].setParam("LOGIC", Channel.Pr.RelativeFader, 0, chData[chOffsetStereo + 2].getParam(Channel.Pr.Fader));
							chData[chOffsetStereo + 2].setParam("LOGIC", Channel.Pr.On, 0, chData[chOffsetStereo].getParam(Channel.Pr.On));
						}
						break;
					}
					break;
				case "015D0002":
					Console.CueMode = num2;
					Console.clearCue();
					break;
				case "015D000E":
					Console.clearCue();
					break;
				case "015E0000":
					setParam("CONSOLE", num, Pr.Cue, 0, num2);
					break;
				case "015F0000":
					setParam("CONSOLE", num + chOffsetMix, Pr.Cue, 0, num2);
					break;
				case "01600000":
					setParam("CONSOLE", num + chOffsetMatrix, Pr.Cue, 0, num2);
					break;
				case "01610000":
					setParam("CONSOLE", num + chOffsetStereo, Pr.Cue, 0, num2);
					break;
				case "021C0008":
					chData[chOffsetMonitor].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "021C000D":
					chData[chOffsetMonitor].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					break;
				case "02270002":
					Console.Lamp = num2;
					MixEngine.NewMessage("LAMP:0", -1);
					break;
				case "02390010":
					Console.selChannel = num2;
					break;
				case "02610000":
					switch (num2)
					{
					case 0:
						Console.clockSpeed = 44;
						break;
					case 1:
						Console.clockSpeed = 48;
						break;
					case 2:
						Console.clockSpeed = 88;
						break;
					case 3:
						Console.clockSpeed = 96;
						break;
					}
					break;
				case "02390035":
					if (num2 >= 0 && num2 <= 4096)
					{
						if ((num2 & (int)Math.Pow(2.0, Settings.Default.flashUDF)) != 0)
						{
							MixEngine.NewMessage("ATTENTION", 99);
						}
						else
						{
							MixEngine.NewMessage("", 99);
						}
					}
					break;
				case "02410000":
					chData[num].IsHeld = num2 == 1;
					if (num2 == 1)
					{
						channelHold = num;
					}
					else
					{
						channelHold = -1;
					}
					break;
				case "02440000":
					chData[chOffsetStereo + num].IsHeld = num2 == 1;
					break;
				}
				if (flag)
				{
					return;
				}
				switch (msgData.Substring(12, 6))
				{
				case "003F00":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "004300":
				{
					int index = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMixOn, index, num2);
						break;
					case 1:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMixPre, index, num2);
						break;
					case 2:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMix, index, num2);
						break;
					}
					flag = true;
					break;
				}
				case "004400":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num].setParam("CONSOLE", Channel.Pr.SendToMixPan, group, num2);
					flag = true;
					break;
				case "005500":
				{
					int index = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrixOn, index, num2);
						break;
					case 1:
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrixPre, index, num2);
						break;
					case 2:
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrix, index, num2);
						break;
					}
					flag = true;
					break;
				}
				case "005600":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrixPan, group, num2);
					flag = true;
					break;
				case "005900":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "006600":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "007400":
				{
					int index = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMixOn, index, num2);
						break;
					case 1:
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMixPre, index, num2);
						break;
					case 2:
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMix, index, num2);
						break;
					}
					flag = true;
					break;
				}
				case "007500":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMatrixPan, group, num2);
					flag = true;
					break;
				case "007600":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "007E00":
					setParam("CONSOLE", num, Pr.RackType, 0, num2);
					flag = true;
					break;
				case "007F00":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					if (group >= 9 && group <= 39)
					{
						setParam("CONSOLE", num, Pr.GEQBand, group - 9, num2);
					}
					else if (group >= 40 && group <= 70)
					{
						setParam("CONSOLE", num + 8, Pr.GEQBand, group - 40, num2);
					}
					else if (group == 0)
					{
						setParam("CONSOLE", num, Pr.GEQType, 0, num2);
					}
					else if (group == 1)
					{
						setParam("CONSOLE", num, Pr.GEQLink, 0, num2);
					}
					else if (group == 2)
					{
						setParam("CONSOLE", num, Pr.GEQLinkLR, 0, num2);
					}
					else if (group == 3)
					{
						setParam("CONSOLE", num, Pr.GEQOn, 0, num2);
					}
					else if (group == 4)
					{
						setParam("CONSOLE", num + 8, Pr.GEQOn, 0, num2);
					}
					else if (group == 5)
					{
						setParam("CONSOLE", num, Pr.GEQFlat, 0, 1);
					}
					else if (group == 6)
					{
						setParam("CONSOLE", num + 8, Pr.GEQFlat, 0, 1);
					}
					else if (group == 7 && num2 == 15)
					{
						setParam("CONSOLE", num, Pr.GEQFlat, 0, 1);
					}
					else if (group == 8 && num2 == 15)
					{
						setParam("CONSOLE", num + 8, Pr.GEQFlat, 0, 1);
					}
					flag = true;
					break;
				case "011400":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 2)) : (text2.Substring(0, 4) + text3.Substring(4, 2)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "011800":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					if (group == 1)
					{
						num += 8;
					}
					setParam("CONSOLE", num, Pr.RackSource, 0, num2);
					flag = true;
					break;
				case "011B00":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num + chOffsetMix].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 2)) : (text2.Substring(0, 4) + text3.Substring(4, 2)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "011C00":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num + chOffsetMatrix].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 2)) : (text2.Substring(0, 4) + text3.Substring(4, 2)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "011D00":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num + chOffsetStereo].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 2)) : (text2.Substring(0, 4) + text3.Substring(4, 2)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "022800":
					switch (msgData.Substring(18, 2))
					{
					case "0D":
						Console.macaddr = num2.ToString("x2") + "xxxxxxxxxx";
						break;
					case "0E":
						Console.macaddr = Console.macaddr.Substring(0, 2) + num2.ToString("x2") + Console.macaddr.Substring(4, 8);
						break;
					case "0F":
						Console.macaddr = Console.macaddr.Substring(0, 4) + num2.ToString("x2") + Console.macaddr.Substring(6, 6);
						break;
					case "10":
						Console.macaddr = Console.macaddr.Substring(0, 6) + num2.ToString("x2") + Console.macaddr.Substring(8, 4);
						break;
					case "11":
						Console.macaddr = Console.macaddr.Substring(0, 8) + num2.ToString("x2") + Console.macaddr.Substring(10, 2);
						break;
					case "12":
						Console.macaddr = Console.macaddr.Substring(0, 10) + num2.ToString("x2");
						break;
					}
					if (!Console.macaddr.Contains("x"))
					{
						MixEngine.NewMessage("MAC:" + num.ToString("000"), -1);
					}
					flag = true;
					break;
				case "010000":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					if (group == 4)
					{
						EFXData[num].setParam("CONSOLE", EFX.Pr.BPM, num2);
					}
					else if (group == 6)
					{
						EFXData[num].setParam("CONSOLE", EFX.Pr.Bypass, num2);
					}
					else if (group == 9)
					{
						EFXData[num].setParam("CONSOLE", EFX.Pr.Mix, num2);
					}
					else if (group == 10)
					{
						EFXData[num].setParam("CONSOLE", EFX.Pr.Type, num2);
					}
					else if (group >= 11 && group <= 42)
					{
						EFXData[num].setParamByMIDIIndex("CONSOLE", group, num2);
					}
					flag = true;
					break;
				}
				if (flag)
				{
					return;
				}
			}
			if (msgData.StartsWith("F0430" + Settings.Default.MIDI_Channel + "3E") && msgData.Length >= 26)
			{
				switch (msgData.Substring(12, 18))
				{
				case "125343454E455F5F5F":
					MixEngine.NewMessage("DUMP RECEIVED", -1);
					processDumpLS9(msgData);
					flag = true;
					break;
				case "124C49425F4E554D5F":
					Console.sceneNum = UnStuffDumpLS9(msgData)[28] * 256 + UnStuffDumpLS9(msgData)[29];
					setParam("CONSOLE", Console.sceneNum, Pr.SceneName, 0, Console.sceneName);
					flag = true;
					break;
				case "124F55545F504F5254":
				{
					byte[] array = UnStuffDumpLS9(msgData);
					for (int num9 = 0; num9 < Console.maxOutOmni; num9++)
					{
						OutPort[num9].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, (short)(array[376 + num9 * 2] << 8) + array[377 + num9 * 2]);
						OutPort[num9].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, (short)(array[156 + num9 * 2] << 8) + array[157 + num9 * 2]);
						OutPort[num9].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, (array[372 + num9 / 8] & (int)Math.Pow(2.0, num9 % 8)) > 0);
						OutPort[num9].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, (array[418 + num9 / 8] & (int)Math.Pow(2.0, num9 % 8)) > 0);
					}
					for (int num10 = 0; num10 < Console.maxOutSlot; num10++)
					{
						OutPort[num10 + Console.maxOutOmni].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, (short)(array[308 + num10 * 2] << 8) + array[309 + num10 * 2]);
						OutPort[num10 + Console.maxOutOmni].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, (short)(array[188 + num10 * 2] << 8) + array[189 + num10 * 2]);
						OutPort[num10 + Console.maxOutOmni].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, (array[299 + num10 / 8] & (int)Math.Pow(2.0, num10 % 8)) > 0);
						OutPort[num10 + Console.maxOutOmni].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, (array[414 + num10 / 8] & (int)Math.Pow(2.0, num10 % 8)) > 0);
					}
					for (int num11 = 0; num11 < 2; num11++)
					{
						OutPort[num11 + Console.maxOutOmni + Console.maxOutSlot].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, (short)(array[410 + num11 * 2] << 8) + array[411 + num11 * 2]);
						OutPort[num11 + Console.maxOutOmni + Console.maxOutSlot].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, (short)(array[252 + num11 * 2] << 8) + array[253 + num11 * 2]);
						OutPort[num11 + Console.maxOutOmni + Console.maxOutSlot].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, (array[408 + num11 / 8] & (int)Math.Pow(2.0, num11 % 8)) > 0);
						OutPort[num11 + Console.maxOutOmni + Console.maxOutSlot].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, (array[420 + num11 / 8] & (int)Math.Pow(2.0, num11 % 8)) > 0);
					}
					flag = true;
					break;
				}
				}
			}
			if (flag)
			{
				return;
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E12") && msgData.Length >= 54)
			{
				switch (msgData.Substring(12, 32))
				{
				case "4C696241747262745343454E455F5F5F":
					if (msgData.Substring(48, 4) == "024B")
					{
						num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
						int num13 = int.Parse(msgData.Substring(54, 2));
						Buffer.BlockCopy(UnStuffBytes(msgData.Substring(60, 10)), 0, sceneData[num].byteName, num13 * 4, 4);
						setParam("CONSOLE", num, Pr.SceneName, 0, sceneData[num].rawName);
						if (sceneNameQueue.Count > 0 && num13 == 0)
						{
							MixEngine.NewMessage("Load Scene " + sceneNameQueue.Peek().ToString("000") + "...", 0);
							logDebug("Request Scene Name #" + sceneNameQueue.Peek());
							reqParam("SceneName", sceneNameQueue.Dequeue());
						}
					}
					flag = true;
					break;
				case "4C696245786973745343454E455F5F5F":
					if (msgData.Substring(44, 2) == "01")
					{
						int num14 = Convert.ToInt32(UnStuffValue(msgData.Substring(50, 4)), 16);
						int num15 = Convert.ToInt32(UnStuffValue(msgData.Substring(54, 4)), 16);
						for (int num16 = num14; num16 <= num15; num16++)
						{
							sceneData[num16].isValid = true;
							logDebug("Enqueue Scene Name #" + num16);
							sceneNameQueue.Enqueue(num16);
						}
						MixEngine.NewMessage("Load Scene " + sceneNameQueue.Peek().ToString("000") + "...", 0);
						logDebug("Request Scene Name #" + sceneNameQueue.Peek());
						reqParam("SceneName", sceneNameQueue.Dequeue());
						Console.sceneHigh = num15;
						MixEngine.NewMessage("Top Scene " + Console.sceneHigh.ToString("000") + "...", 0);
						if (num15 < 300)
						{
							reqParam("SceneRange", num15 + 1, 0);
						}
					}
					flag = true;
					break;
				case "4C696252636C5F5F5343454E455F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "4C69625374725F5F5343454E455F5F5F":
					Console.sceneNum = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", Console.sceneNum, Pr.SceneSave, 0, 1);
					flag = true;
					break;
				case "524C69625374725F5343454E455F5F5F":
					if (Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16) != 0)
					{
						MixEngine.NewMessage("Scene Save Failed", 3);
					}
					else
					{
						Console.sceneNum = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
						Console.sceneName = sceneData[Console.sceneNum].sceneName;
						setParam("CONSOLE", Console.sceneNum, Pr.SceneSave, 0, 1);
						MixEngine.NewMessage("Scene Save Confirmed", 2);
					}
					flag = true;
					break;
				case "524C696252636C5F5343454E455F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "4C6962556E52636C44594E415F5F5F5F":
				case "4C696252636C5F5F44594E415F5F5F5F":
					num = Convert.ToInt32(msgData.Substring(50, 2), 16);
					if (num >= 0 && num <= 72)
					{
						reqParam("Dyn0On", num, 0);
						reqParam("Dyn0Type", num, 0);
						reqParam("Dyn0Attack", num, 0);
						reqParam("Dyn0Range", num, 0);
						reqParam("Dyn0Hold", num, 0);
						reqParam("Dyn0Decay", num, 0);
						reqParam("Dyn0Ratio", num, 0);
						reqParam("Dyn0Gain", num, 0);
						reqParam("Dyn0Knee", num, 0);
						reqParam("Dyn0Threshold", num, 0);
						reqParam("Dyn1On", num, 0);
						reqParam("Dyn1Type", num, 0);
						reqParam("Dyn1Attack", num, 0);
						reqParam("Dyn1Range", num, 0);
						reqParam("Dyn1Hold", num, 0);
						reqParam("Dyn1Release", num, 0);
						reqParam("Dyn1Ratio", num, 0);
						reqParam("Dyn1Gain", num, 0);
						reqParam("Dyn1Knee", num, 0);
						reqParam("Dyn1Threshold", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C494E45515F5F5F5F":
				case "4C696252636C5F5F494E45515F5F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16);
					if (num >= 0 && num < chData.Length)
					{
						reqParam("eqHPFOn", num, 0);
						reqParam("eqHPFFreq", num, 0);
						reqParam("eqAtt", num, 0);
						reqParam("eqType", num, 0);
						reqParam("eqOn", num, 0);
						reqParam("eq1Type", num, 0);
						reqParam("eq4Type", num, 0);
						reqParam("eq1HPF", num, 0);
						reqParam("eq4LPF", num, 0);
						reqParam("eq1Q", num, 0);
						reqParam("eq1F", num, 0);
						reqParam("eq1G", num, 0);
						reqParam("eq2Q", num, 0);
						reqParam("eq2F", num, 0);
						reqParam("eq2G", num, 0);
						reqParam("eq3Q", num, 0);
						reqParam("eq3F", num, 0);
						reqParam("eq3G", num, 0);
						reqParam("eq4Q", num, 0);
						reqParam("eq4F", num, 0);
						reqParam("eq4G", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C4F555445515F5F5F":
				case "4C696252636C5F5F4F555445515F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16);
					if (num >= 256 && num <= 271)
					{
						num += chOffsetMix - 256;
					}
					else if (num >= 512 && num <= 519)
					{
						num += chOffsetMatrix - 512;
					}
					else if (num >= 1024 && num <= 1026)
					{
						num += chOffsetStereo - 1024;
					}
					if (num >= 0 && num < chData.Length)
					{
						reqParam("eqHPFOn", num, 0);
						reqParam("eqHPFFreq", num, 0);
						reqParam("eqAtt", num, 0);
						reqParam("eqType", num, 0);
						reqParam("eqOn", num, 0);
						reqParam("eq1Type", num, 0);
						reqParam("eq4Type", num, 0);
						reqParam("eq1HPF", num, 0);
						reqParam("eq4LPF", num, 0);
						reqParam("eq1Q", num, 0);
						reqParam("eq1F", num, 0);
						reqParam("eq1G", num, 0);
						reqParam("eq2Q", num, 0);
						reqParam("eq2F", num, 0);
						reqParam("eq2G", num, 0);
						reqParam("eq3Q", num, 0);
						reqParam("eq3F", num, 0);
						reqParam("eq3G", num, 0);
						reqParam("eq4Q", num, 0);
						reqParam("eq4F", num, 0);
						reqParam("eq4G", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C4745515F5F5F5F5F":
				case "4C696252636C5F5F4745515F5F5F5F5F":
					num = Convert.ToInt32(msgData.Substring(50, 2), 16);
					if (num >= 0 && num <= 15)
					{
						for (int num12 = 0; num12 < 62; num12++)
						{
							reqParam("GEQBand", num, num12);
						}
					}
					break;
				}
			}
			if (flag)
			{
				return;
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E1101") && msgData.Length == 36)
			{
				num = Convert.ToInt32(msgData.Substring(20, 4), 16);
				num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(24, 10)), 16);
				switch (msgData.Substring(12, 8))
				{
				case "010A0000":
					chData[num].setParam("CONSOLE", Channel.Pr.PatchIn, 0, num2);
					flag = true;
					break;
				case "00290000":
				case "03250000":
					chData[num].setParam("CONSOLE", Channel.Pr.Phantom, 0, num2);
					flag = true;
					break;
				case "00290001":
				case "03250001":
					chData[num].setParam("CONSOLE", Channel.Pr.HA, 0, num2);
					flag = true;
					break;
				case "002D0000":
					chData[num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					flag = true;
					break;
				case "002E0000":
					chData[num].setParam("CONSOLE", Channel.Pr.DirectOutOn, 0, num2);
					flag = true;
					break;
				case "002F0000":
					chData[num].setParam("CONSOLE", Channel.Pr.Phase, 0, num2);
					flag = true;
					break;
				case "00300000":
					chData[num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					flag = true;
					break;
				case "00310001":
					if (num2 >= -63 && num2 <= 63)
					{
						chData[num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					}
					flag = true;
					break;
				case "00320000":
					chData[num].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					flag = true;
					break;
				case "00400000":
					chData[num].setParam("CONSOLE", Channel.Pr.ToStereoOn, 0, num2);
					flag = true;
					break;
				case "00410000":
					chData[num].setParam("CONSOLE", Channel.Pr.ToMonoOn, 0, num2);
					flag = true;
					break;
				case "00360000":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0On, 0, num2);
					flag = true;
					break;
				case "00360001":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Type, 0, num2);
					flag = true;
					break;
				case "00360009":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Attack, 0, num2);
					flag = true;
					break;
				case "0036000A":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Range, 0, num2);
					flag = true;
					break;
				case "0036000B":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Hold, 0, num2);
					flag = true;
					break;
				case "0036000C":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Decay, 0, num2);
					flag = true;
					break;
				case "0036000D":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Ratio, 0, num2);
					flag = true;
					break;
				case "0036000E":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Gain, 0, num2);
					flag = true;
					break;
				case "0036000F":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Knee, 0, num2);
					flag = true;
					break;
				case "00360010":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Threshold, 0, num2);
					flag = true;
					break;
				case "00390000":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					flag = true;
					break;
				case "00390001":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					flag = true;
					break;
				case "00390009":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					flag = true;
					break;
				case "0039000A":
					flag = true;
					break;
				case "0039000B":
					flag = true;
					break;
				case "0039000C":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					flag = true;
					break;
				case "0039000D":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					flag = true;
					break;
				case "0039000E":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					flag = true;
					break;
				case "0039000F":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					flag = true;
					break;
				case "00390010":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					flag = true;
					break;
				case "00330001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					flag = true;
					break;
				case "003B0000":
					chData[num].setParam("CONSOLE", Channel.Pr.EQHPFOn, 0, num2);
					flag = true;
					break;
				case "003B0001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQHPFFreq, 0, num2);
					flag = true;
					break;
				case "003C0000":
					chData[num].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "003C0001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					flag = true;
					break;
				case "003C0002":
					chData[num].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					flag = true;
					break;
				case "003C0003":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					flag = true;
					break;
				case "003C0004":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					flag = true;
					break;
				case "003C0005":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					flag = true;
					break;
				case "003C0006":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					flag = true;
					break;
				case "003C0008":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					flag = true;
					break;
				case "003C0009":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					flag = true;
					break;
				case "003C000A":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					flag = true;
					break;
				case "003C000C":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					flag = true;
					break;
				case "003C000D":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					flag = true;
					break;
				case "003C000E":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					flag = true;
					break;
				case "003C0010":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					flag = true;
					break;
				case "003C0011":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					flag = true;
					break;
				case "003C0012":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					flag = true;
					break;
				case "003C0014":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					flag = true;
					break;
				case "003C0015":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					flag = true;
					break;
				case "003C0016":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					flag = true;
					break;
				case "00210000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
					break;
				case "00220000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.BusType, 0, num2);
					break;
				case "00230000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.PanLink, 0, num2);
					break;
				case "004A0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					break;
				case "004B0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					flag = true;
					break;
				case "004C0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Balance, 0, num2);
					flag = true;
					break;
				case "004D0000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					flag = true;
					break;
				case "004E0001":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					flag = true;
					break;
				case "00560000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.ToStereoOn, 0, num2);
					flag = true;
					break;
				case "00560002":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					flag = true;
					break;
				case "00570000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.ToMonoOn, 0, num2);
					flag = true;
					break;
				case "00510000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					break;
				case "00510001":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					break;
				case "00510009":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					break;
				case "0051000C":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					break;
				case "0051000D":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					break;
				case "0051000E":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					break;
				case "0051000F":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					break;
				case "00510010":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					break;
				case "00530000":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "00530001":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					break;
				case "00530002":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					break;
				case "00530003":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					break;
				case "00530004":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					break;
				case "00530005":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					break;
				case "00530006":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					break;
				case "00530008":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					break;
				case "00530009":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					break;
				case "0053000A":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					break;
				case "0053000C":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					break;
				case "0053000D":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					break;
				case "0053000E":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					break;
				case "00530010":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					break;
				case "00530011":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					break;
				case "00530012":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					break;
				case "00530014":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					break;
				case "00530015":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					break;
				case "00530016":
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					break;
				case "00240000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
					break;
				case "00250000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.BusType, 0, num2);
					break;
				case "00260000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.PanLink, 0, num2);
					break;
				case "005B0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					flag = true;
					break;
				case "005C0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					flag = true;
					break;
				case "005D0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Balance, 0, num2);
					flag = true;
					break;
				case "005E0000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					flag = true;
					break;
				case "005F0001":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					flag = true;
					break;
				case "00620000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					break;
				case "00620001":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					break;
				case "00620009":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					break;
				case "0062000C":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					break;
				case "0062000D":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					break;
				case "0062000E":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					break;
				case "0062000F":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					break;
				case "00620010":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					break;
				case "00640000":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "00640001":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					break;
				case "00640002":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					break;
				case "00640003":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					break;
				case "00640004":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					break;
				case "00640005":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					break;
				case "00640006":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					break;
				case "00640008":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					break;
				case "00640009":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					break;
				case "0064000A":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					break;
				case "0064000C":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					break;
				case "0064000D":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					break;
				case "0064000E":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					break;
				case "00640010":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					break;
				case "00640011":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					break;
				case "00640012":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					break;
				case "00640014":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					break;
				case "00640015":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					break;
				case "00640016":
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					break;
				case "00690000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					break;
				case "006A0000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "006B0000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					break;
				case "006C0000":
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					flag = true;
					break;
				case "006D0001":
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					flag = true;
					break;
				case "00700000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					break;
				case "00700001":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					break;
				case "00700009":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					break;
				case "0070000C":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					break;
				case "0070000D":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					break;
				case "0070000E":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					break;
				case "0070000F":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					break;
				case "00700010":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					break;
				case "00720000":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQFlatten, 0, 1);
					break;
				case "00720001":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					break;
				case "00720002":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					break;
				case "00720003":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					break;
				case "00720004":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					break;
				case "00720005":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					break;
				case "00720006":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					break;
				case "00720008":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					break;
				case "00720009":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					break;
				case "0072000A":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					break;
				case "0072000C":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					break;
				case "0072000D":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					break;
				case "0072000E":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					break;
				case "00720010":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					break;
				case "00720011":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					break;
				case "00720012":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					break;
				case "00720014":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					break;
				case "00720015":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					break;
				case "00720016":
					chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					break;
				case "02030000":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, num2);
					flag = true;
					break;
				case "02190000":
					OutPort[num + 16].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, num2);
					flag = true;
					break;
				case "02190002":
					OutPort[num + 16].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, num2);
					flag = true;
					break;
				case "021A0000":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, num2);
					flag = true;
					break;
				case "021A0002":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, num2);
					flag = true;
					break;
				case "021B0000":
					OutPort[num + 64].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, num2);
					flag = true;
					break;
				case "021B0002":
					OutPort[num + 64].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, num2);
					flag = true;
					break;
				case "021E0000":
					OutPort[num + 16].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, num2);
					flag = true;
					break;
				case "021F0000":
					OutPort[num].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, num2);
					flag = true;
					break;
				case "02200000":
					OutPort[num + 64].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, num2);
					flag = true;
					break;
				case "00770000":
					DCA[num].setMute(num2 == 1, "console");
					break;
				case "00790000":
					DCA[num].setFader(num2, "console");
					break;
				case "00120000":
					for (group = 0; group < 39; group++)
					{
						if ((num2 & (int)Math.Pow(2.0, 0.0)) > 0)
						{
							if (!chGroup[0].ha)
							{
								foreach (int member9 in chGroup[group].members)
								{
									chData[member9].setParam("LOGIC", Channel.Pr.RelativeHA, 0, chData[member9].getParam(Channel.Pr.HA));
								}
							}
							chGroup[0].ha = true;
						}
						else
						{
							chGroup[0].ha = false;
						}
						if ((num2 & (int)Math.Pow(2.0, 5.0)) > 0)
						{
							if (!chGroup[0].fader)
							{
								foreach (int member10 in chGroup[group].members)
								{
									chData[member10].setParam("LOGIC", Channel.Pr.RelativeFader, 0, chData[member10].getParam(Channel.Pr.Fader));
								}
							}
							chGroup[0].fader = true;
						}
						else
						{
							chGroup[0].fader = false;
						}
						int num21 = 0;
						if (chGroup[group].members.Count > 0)
						{
							num21 = chGroup[group].members[0];
						}
						chGroup[0].dyn1 = (num2 & (int)Math.Pow(2.0, 1.0)) > 0;
						if (chGroup[0].dyn1)
						{
							foreach (int member11 in chGroup[group].members)
							{
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0On, 0, chData[num21].getParam(Channel.Pr.Dyn0On));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Type, 0, chData[num21].getParam(Channel.Pr.Dyn0Type));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Attack, 0, chData[num21].getParam(Channel.Pr.Dyn0Attack));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Range, 0, chData[num21].getParam(Channel.Pr.Dyn0Range));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Hold, 0, chData[num21].getParam(Channel.Pr.Dyn0Hold));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Decay, 0, chData[num21].getParam(Channel.Pr.Dyn0Decay));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Ratio, 0, chData[num21].getParam(Channel.Pr.Dyn0Ratio));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Gain, 0, chData[num21].getParam(Channel.Pr.Dyn0Gain));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Knee, 0, chData[num21].getParam(Channel.Pr.Dyn0Knee));
								chData[member11].setParam("LOGIC", Channel.Pr.Dyn0Threshold, 0, chData[num21].getParam(Channel.Pr.Dyn0Threshold));
							}
						}
						chGroup[0].dyn2 = (num2 & (int)Math.Pow(2.0, 2.0)) > 0;
						if (chGroup[0].dyn2)
						{
							foreach (int member12 in chGroup[group].members)
							{
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1On, 0, chData[num21].getParam(Channel.Pr.Dyn1On));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Type, 0, chData[num21].getParam(Channel.Pr.Dyn1Type));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Attack, 0, chData[num21].getParam(Channel.Pr.Dyn1Attack));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Range, 0, chData[num21].getParam(Channel.Pr.Dyn1Range));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Hold, 0, chData[num21].getParam(Channel.Pr.Dyn1Hold));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Release, 0, chData[num21].getParam(Channel.Pr.Dyn1Release));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Ratio, 0, chData[num21].getParam(Channel.Pr.Dyn1Ratio));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Gain, 0, chData[num21].getParam(Channel.Pr.Dyn1Gain));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Knee, 0, chData[num21].getParam(Channel.Pr.Dyn1Knee));
								chData[member12].setParam("LOGIC", Channel.Pr.Dyn1Threshold, 0, chData[num21].getParam(Channel.Pr.Dyn1Threshold));
							}
						}
						chGroup[0].eq = (num2 & (int)Math.Pow(2.0, 3.0)) > 0;
						if (chGroup[0].eq)
						{
							foreach (int member13 in chGroup[group].members)
							{
								chData[member13].setParam("LOGIC", Channel.Pr.EQType, 0, chData[num21].getParam(Channel.Pr.EQType));
								chData[member13].setParam("LOGIC", Channel.Pr.EQOn, 0, chData[num21].getParam(Channel.Pr.EQOn));
								chData[member13].setParam("LOGIC", Channel.Pr.EQHPFOn, 0, chData[num21].getParam(Channel.Pr.EQHPFOn));
								chData[member13].setParam("LOGIC", Channel.Pr.EQHPFFreq, 0, chData[num21].getParam(Channel.Pr.EQHPFFreq));
								chData[member13].setParam("LOGIC", Channel.Pr.EQAtt, 0, chData[num21].getParam(Channel.Pr.EQAtt));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ1Type, 0, chData[num21].getParam(Channel.Pr.EQ1Type));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ1HPFOn, 0, chData[num21].getParam(Channel.Pr.EQ1HPFOn));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ4Type, 0, chData[num21].getParam(Channel.Pr.EQ4Type));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ4LPFOn, 0, chData[num21].getParam(Channel.Pr.EQ4LPFOn));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ1Q, 0, chData[num21].getParam(Channel.Pr.EQ1Q));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ1F, 0, chData[num21].getParam(Channel.Pr.EQ1F));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ1G, 0, chData[num21].getParam(Channel.Pr.EQ1G));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ2Q, 0, chData[num21].getParam(Channel.Pr.EQ2Q));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ2F, 0, chData[num21].getParam(Channel.Pr.EQ2F));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ2G, 0, chData[num21].getParam(Channel.Pr.EQ2G));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ3Q, 0, chData[num21].getParam(Channel.Pr.EQ3Q));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ3F, 0, chData[num21].getParam(Channel.Pr.EQ3F));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ3G, 0, chData[num21].getParam(Channel.Pr.EQ3G));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ4Q, 0, chData[num21].getParam(Channel.Pr.EQ4Q));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ4F, 0, chData[num21].getParam(Channel.Pr.EQ4F));
								chData[member13].setParam("LOGIC", Channel.Pr.EQ4G, 0, chData[num21].getParam(Channel.Pr.EQ4G));
							}
						}
						chGroup[0].on = (num2 & (int)Math.Pow(2.0, 4.0)) > 0;
						if (!chGroup[0].on)
						{
							continue;
						}
						foreach (int member14 in chGroup[group].members)
						{
							chData[member14].setParam("CONSOLE", Channel.Pr.On, 0, chData[num21].getParam(Channel.Pr.On));
						}
					}
					break;
				case "00120001":
					for (group = 0; group < 39; group++)
					{
						int num17 = 0;
						if (chGroup[group].members.Count > 0)
						{
							num17 = chGroup[group].members[0];
						}
						for (int num18 = 0; num18 < Console.maxMIX; num18++)
						{
							chGroup[0].mixSend[num18] = (num2 & (int)Math.Pow(2.0, num18 + 4)) > 0;
						}
						chGroup[0].mixLevelSend = (num2 & (int)Math.Pow(2.0, 0.0)) > 0;
						if (chGroup[0].mixLevelSend)
						{
							foreach (int member15 in chGroup[group].members)
							{
								for (int num19 = 0; num19 < Console.maxMIX; num19++)
								{
									if (chGroup[0].mixSend[num19])
									{
										chData[member15].setParam("LOGIC", Channel.Pr.SendToMix, num19, chData[num17].getParam(Channel.Pr.SendToMix, num19));
									}
								}
							}
						}
						chGroup[0].mixLevelOn = (num2 & (int)Math.Pow(2.0, 1.0)) > 0;
						if (!chGroup[0].mixLevelOn)
						{
							continue;
						}
						foreach (int member16 in chGroup[group].members)
						{
							for (int num20 = 0; num20 < Console.maxMIX; num20++)
							{
								if (chGroup[0].mixSend[num20])
								{
									chData[member16].setParam("LOGIC", Channel.Pr.SendToMixOn, num20, chData[num17].getParam(Channel.Pr.SendToMixOn, num20));
								}
							}
						}
					}
					break;
				case "00200000":
					setParam("CONSOLE", num, Pr.MuteMaster, 0, num2);
					flag = true;
					break;
				case "01050000":
					if (num2 >= 0 && num2 <= 38)
					{
						chData[num].setParam("CONSOLE", Channel.Pr.Group, 0, num2);
					}
					flag = true;
					break;
				case "01220000":
					group = Convert.ToInt32(msgData.Substring(22, 2), 16);
					switch (group)
					{
					case 41:
						Console.PrefNaviToSelIn = num2 == 1;
						break;
					case 42:
						Console.PrefNaviToSelOut = num2 == 1;
						break;
					case 46:
						Console.PrefCueSelLink = num2 == 1;
						break;
					}
					break;
				case "02170008":
					chData[chOffsetMonitor].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "02170013":
					chData[chOffsetMonitor].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					break;
				case "02310032":
					if (Console.PrefNaviToSelIn || Console.PrefNaviToSelOut)
					{
						reqParam("SelChannel", 0);
					}
					break;
				case "01590002":
					Console.CueMode = num2;
					Console.clearCue();
					break;
				case "0159000E":
					Console.clearCue();
					break;
				case "015A0000":
					setParam("CONSOLE", num, Pr.Cue, 0, num2);
					break;
				case "015B0000":
					setParam("CONSOLE", num + chOffsetMix, Pr.Cue, 0, num2);
					break;
				case "015C0000":
					setParam("CONSOLE", num + chOffsetMatrix, Pr.Cue, 0, num2);
					break;
				case "015D0000":
					setParam("CONSOLE", num + chOffsetStereo, Pr.Cue, 0, num2);
					break;
				case "02220002":
					Console.Lamp = num2;
					MixEngine.NewMessage("LAMP:0", -1);
					break;
				case "02310012":
					if (Console.ver == ConsoleVersions.M7CLv3)
					{
						Console.selChannel = num2;
					}
					break;
				case "02320012":
					if (Console.ver == ConsoleVersions.M7CLv35)
					{
						Console.selChannel = num2;
					}
					break;
				case "02590000":
					switch (num2)
					{
					case 0:
						Console.clockSpeed = 44;
						break;
					case 1:
						Console.clockSpeed = 48;
						break;
					case 2:
						Console.clockSpeed = 88;
						break;
					case 3:
						Console.clockSpeed = 96;
						break;
					}
					break;
				}
				switch (msgData.Substring(12, 6))
				{
				case "003E00":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "003F00":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num].setParam("LOGIC", Channel.Pr.DCAGroup, group, num2);
					flag = true;
					break;
				case "004200":
				{
					int index2 = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMixOn, index2, num2);
						break;
					case 1:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMixPre, index2, num2);
						break;
					case 2:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMix, index2, num2);
						break;
					}
					flag = true;
					break;
				}
				case "004300":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num].setParam("CONSOLE", Channel.Pr.SendToMixPan, group, num2);
					flag = true;
					break;
				case "004400":
				{
					int index2 = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMatrixOn, index2, num2);
						break;
					case 1:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMatrixPre, index2, num2);
						break;
					case 2:
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMatrix, index2, num2);
						break;
					}
					flag = true;
					break;
				}
				case "004500":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num].setParam("CONSOLE", Channel.Pr.SendToMatrixPan, group, num2);
					flag = true;
					break;
				case "005400":
				{
					int index2 = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrixOn, index2, num2);
						break;
					case 1:
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrixPre, index2, num2);
						break;
					case 2:
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrix, index2, num2);
						break;
					}
					flag = true;
					break;
				}
				case "005500":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.SendToMatrixPan, group, num2);
					flag = true;
					break;
				case "005800":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetMix].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "005900":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetMix].setParam("LOGIC", Channel.Pr.DCAGroup, group, num2);
					flag = true;
					break;
				case "006500":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetMatrix].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "006600":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetMatrix].setParam("LOGIC", Channel.Pr.DCAGroup, group, num2);
					flag = true;
					break;
				case "007300":
				{
					int index2 = (Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) / 3;
					switch ((Convert.ToInt32(msgData.Substring(18, 2), 16) - 3) % 3)
					{
					case 0:
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMixOn, index2, num2);
						break;
					case 1:
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMixPre, index2, num2);
						break;
					case 2:
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMix, index2, num2);
						break;
					}
					flag = true;
					break;
				}
				case "007400":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16) * 2;
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.SendToMatrixPan, group, num2);
					flag = true;
					break;
				case "007500":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetStereo].setParam("LOGIC", Channel.Pr.MuteGroup, group, num2);
					flag = true;
					break;
				case "007600":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					chData[num + chOffsetStereo].setParam("LOGIC", Channel.Pr.DCAGroup, group, num2);
					flag = true;
					break;
				case "007D00":
					setParam("CONSOLE", num, Pr.RackType, 0, num2);
					flag = true;
					break;
				case "007E00":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					if (group >= 9 && group <= 39)
					{
						setParam("CONSOLE", num, Pr.GEQBand, group - 9, num2);
					}
					else if (group >= 40 && group <= 70)
					{
						setParam("CONSOLE", num + 8, Pr.GEQBand, group - 40, num2);
					}
					else if (group == 0)
					{
						setParam("CONSOLE", num, Pr.GEQType, 0, num2);
					}
					else if (group == 1)
					{
						setParam("CONSOLE", num, Pr.GEQLink, 0, num2);
					}
					else if (group == 2)
					{
						setParam("CONSOLE", num, Pr.GEQLinkLR, 0, num2);
					}
					else if (group == 3)
					{
						setParam("CONSOLE", num, Pr.GEQOn, 0, num2);
					}
					else if (group == 4)
					{
						setParam("CONSOLE", num + 8, Pr.GEQOn, 0, num2);
					}
					else if (group == 5)
					{
						setParam("CONSOLE", num, Pr.GEQFlat, 0, 1);
					}
					else if (group == 6)
					{
						setParam("CONSOLE", num + 8, Pr.GEQFlat, 0, 1);
					}
					else if (group == 7 && num2 == 15)
					{
						setParam("CONSOLE", num, Pr.GEQFlat, 0, 1);
					}
					else if (group == 8 && num2 == 15)
					{
						setParam("CONSOLE", num + 8, Pr.GEQFlat, 0, 1);
					}
					flag = true;
					break;
				case "011300":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 4)) : (text2.Substring(0, 4) + text3.Substring(4, 4)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "011700":
					group = Convert.ToInt32(msgData.Substring(18, 2), 16);
					if (group == 1)
					{
						num += 8;
					}
					setParam("CONSOLE", num, Pr.RackSource, 0, num2);
					flag = true;
					break;
				case "011900":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num + chOffsetMix].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 4)) : (text2.Substring(0, 4) + text3.Substring(4, 4)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "011A00":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num + chOffsetMatrix].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 4)) : (text2.Substring(0, 4) + text3.Substring(4, 4)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "011B00":
					text = msgData.Substring(18, 2);
					text2 = UnStuffString(msgData.Substring(24, 10)) + "\t\t\t\t";
					text3 = chData[num + chOffsetStereo].name + "\t\t\t\t\t\t\t\t";
					text3 = ((!(text == "00")) ? (text3.Substring(0, 4) + text2.Substring(0, 4)) : (text2.Substring(0, 4) + text3.Substring(4, 4)));
					text3 = text3.Replace("\t", string.Empty);
					chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Name, 0, text3);
					flag = true;
					break;
				case "022300":
					switch (msgData.Substring(18, 2))
					{
					case "0D":
						Console.macaddr = num2.ToString("x2") + "xxxxxxxxxx";
						break;
					case "0E":
						Console.macaddr = Console.macaddr.Substring(0, 2) + num2.ToString("x2") + Console.macaddr.Substring(4, 8);
						break;
					case "0F":
						Console.macaddr = Console.macaddr.Substring(0, 4) + num2.ToString("x2") + Console.macaddr.Substring(6, 6);
						break;
					case "10":
						Console.macaddr = Console.macaddr.Substring(0, 6) + num2.ToString("x2") + Console.macaddr.Substring(8, 4);
						break;
					case "11":
						Console.macaddr = Console.macaddr.Substring(0, 8) + num2.ToString("x2") + Console.macaddr.Substring(10, 2);
						break;
					case "12":
						Console.macaddr = Console.macaddr.Substring(0, 10) + num2.ToString("x2");
						break;
					}
					if (!Console.macaddr.Contains("x"))
					{
						MixEngine.NewMessage("MAC:" + num.ToString("000"), -1);
					}
					flag = true;
					break;
				}
				return;
			}
			if (msgData.StartsWith("F0430" + Settings.Default.MIDI_Channel + "3E") && msgData.Length >= 26)
			{
				switch (msgData.Substring(12, 4))
				{
				case "114D":
					MixEngine.NewMessage("M7CL DUMP RECEIVED", -1);
					processDumpM7CL(msgData);
					flag = true;
					break;
				case "1153":
				{
					byte[] array2 = UnStuffDumpM7CL(msgData);
					if (msgData.Substring(16, 4) == "0405")
					{
						Console.sceneNum = array2[28] * 256 + array2[29];
						setParam("CONSOLE", Console.sceneNum, Pr.SceneName, 0, Console.sceneName);
					}
					else if (msgData.Substring(16, 4) == "0402")
					{
						for (int num22 = 0; num22 < 16; num22++)
						{
							OutPort[num22].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, (short)(array2[448 + num22 * 2] << 8) + array2[449 + num22 * 2]);
							OutPort[num22].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, (short)(array2[156 + num22 * 2] << 8) + array2[157 + num22 * 2]);
							OutPort[num22].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, (array2[440 + num22 / 8] & (int)Math.Pow(2.0, num22 % 8)) > 0);
							OutPort[num22].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, (array2[524 + num22 / 8] & (int)Math.Pow(2.0, num22 % 8)) > 0);
						}
						for (int num23 = 0; num23 < 48; num23++)
						{
							OutPort[num23 + 16].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, (short)(array2[344 + num23 * 2] << 8) + array2[345 + num23 * 2]);
							OutPort[num23 + 16].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, (short)(array2[188 + num23 * 2] << 8) + array2[189 + num23 * 2]);
							OutPort[num23 + 16].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, (array2[331 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
							OutPort[num23 + 16].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, (array2[518 + num23 / 8] & (int)Math.Pow(2.0, num23 % 8)) > 0);
						}
						for (int num24 = 0; num24 < 2; num24++)
						{
							OutPort[num24 + 64].setParam("CONSOLE", AirFader.OutPort.Pr.DelayTime, (short)(array2[514 + num24 * 2] << 8) + array2[515 + num24 * 2]);
							OutPort[num24 + 64].setParam("CONSOLE", AirFader.OutPort.Pr.Attenuation, (short)(array2[284 + num24 * 2] << 8) + array2[285 + num24 * 2]);
							OutPort[num24 + 64].setParam("CONSOLE", AirFader.OutPort.Pr.DelayOn, (array2[512 + num24 / 8] & (int)Math.Pow(2.0, num24 % 8)) > 0);
							OutPort[num24 + 64].setParam("CONSOLE", AirFader.OutPort.Pr.Phase, (array2[528 + num24 / 8] & (int)Math.Pow(2.0, num24 % 8)) > 0);
						}
					}
					flag = true;
					break;
				}
				}
			}
			if (flag)
			{
				return;
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E11") && msgData.Length >= 54)
			{
				switch (msgData.Substring(12, 32))
				{
				case "4C696241747262745343454E455F5F5F":
					if ((msgData.Substring(48, 4) == "0244" && Console.ver == ConsoleVersions.M7CLv35) || (msgData.Substring(48, 4) == "0243" && Console.ver == ConsoleVersions.M7CLv3))
					{
						num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
						int num29 = int.Parse(msgData.Substring(54, 2));
						logDebug("Got Block " + num29 + " for Name of Scene #" + num);
						Buffer.BlockCopy(UnStuffBytes(msgData.Substring(60, 10)), 0, sceneData[num].byteName, num29 * 4, 4);
						setParam("CONSOLE", num, Pr.SceneName, 0, sceneData[num].rawName);
						if (sceneNameQueue.Count > 0 && num29 == 0)
						{
							MixEngine.NewMessage("Load Scene " + sceneNameQueue.Peek().ToString("000") + "...", 0);
							logDebug("Request Scene Name #" + sceneNameQueue.Peek());
							reqParam("SceneName", sceneNameQueue.Dequeue());
						}
					}
					flag = true;
					break;
				case "4C696245786973745343454E455F5F5F":
					if (msgData.Substring(44, 2) == "01")
					{
						int num26 = Convert.ToInt32(UnStuffValue(msgData.Substring(50, 4)), 16);
						int num27 = Convert.ToInt32(UnStuffValue(msgData.Substring(54, 4)), 16);
						logDebug("LowScene: " + num26 + ", HiScene: " + num27);
						for (int num28 = num26; num28 <= num27; num28++)
						{
							sceneData[num28].isValid = true;
							logDebug("Enqueue Scene Name #" + num28);
							sceneNameQueue.Enqueue(num28);
						}
						MixEngine.NewMessage("Load Scene " + sceneNameQueue.Peek().ToString("000") + "...", 0);
						logDebug("Request Scene Name #" + sceneNameQueue.Peek());
						reqParam("SceneName", sceneNameQueue.Dequeue());
						Console.sceneHigh = num27;
						MixEngine.NewMessage("Top Scene " + Console.sceneHigh.ToString("000") + "...", 0);
						if (num27 < 300)
						{
							reqParam("SceneRange", num27 + 1, 0);
						}
					}
					flag = true;
					break;
				case "4C696252636C5F5F5343454E455F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "4C69625374725F5F5343454E455F5F5F":
					Console.sceneNum = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", Console.sceneNum, Pr.SceneSave, 0, 1);
					flag = true;
					break;
				case "524C69625374725F5343454E455F5F5F":
					Console.sceneNum = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					Console.sceneName = sceneData[Console.sceneNum].sceneName;
					setParam("CONSOLE", Console.sceneNum, Pr.SceneSave, 0, 1);
					MixEngine.NewMessage("Scene Save Confirmed", 2);
					flag = true;
					break;
				case "524C696252636C5F5343454E455F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "4C6962556E52636C44594E415F5F5F5F":
				case "4C696252636C5F5F44594E415F5F5F5F":
					num = Convert.ToInt32(msgData.Substring(50, 2), 16);
					if (num >= 0 && num <= 72)
					{
						reqParam("Dyn0On", num, 0);
						reqParam("Dyn0Type", num, 0);
						reqParam("Dyn0Attack", num, 0);
						reqParam("Dyn0Range", num, 0);
						reqParam("Dyn0Hold", num, 0);
						reqParam("Dyn0Decay", num, 0);
						reqParam("Dyn0Ratio", num, 0);
						reqParam("Dyn0Gain", num, 0);
						reqParam("Dyn0Knee", num, 0);
						reqParam("Dyn0Threshold", num, 0);
						reqParam("Dyn1On", num, 0);
						reqParam("Dyn1Type", num, 0);
						reqParam("Dyn1Attack", num, 0);
						reqParam("Dyn1Range", num, 0);
						reqParam("Dyn1Hold", num, 0);
						reqParam("Dyn1Release", num, 0);
						reqParam("Dyn1Ratio", num, 0);
						reqParam("Dyn1Gain", num, 0);
						reqParam("Dyn1Knee", num, 0);
						reqParam("Dyn1Threshold", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C494E45515F5F5F5F":
				case "4C696252636C5F5F494E45515F5F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16);
					if (num >= 0 && num < chData.Length)
					{
						reqParam("eqHPFOn", num, 0);
						reqParam("eqHPFFreq", num, 0);
						reqParam("eqAtt", num, 0);
						reqParam("eqType", num, 0);
						reqParam("eqOn", num, 0);
						reqParam("eq1Type", num, 0);
						reqParam("eq4Type", num, 0);
						reqParam("eq1HPF", num, 0);
						reqParam("eq4LPF", num, 0);
						reqParam("eq1Q", num, 0);
						reqParam("eq1F", num, 0);
						reqParam("eq1G", num, 0);
						reqParam("eq2Q", num, 0);
						reqParam("eq2F", num, 0);
						reqParam("eq2G", num, 0);
						reqParam("eq3Q", num, 0);
						reqParam("eq3F", num, 0);
						reqParam("eq3G", num, 0);
						reqParam("eq4Q", num, 0);
						reqParam("eq4F", num, 0);
						reqParam("eq4G", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C4F555445515F5F5F":
				case "4C696252636C5F5F4F555445515F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16);
					if (num >= 256 && num <= 271)
					{
						num += chOffsetMix - 256;
					}
					else if (num >= 512 && num <= 519)
					{
						num += chOffsetMatrix - 512;
					}
					else if (num >= 1024 && num <= 1026)
					{
						num += chOffsetStereo - 1024;
					}
					if (num >= 0 && num < chData.Length)
					{
						reqParam("eqHPFOn", num, 0);
						reqParam("eqHPFFreq", num, 0);
						reqParam("eqAtt", num, 0);
						reqParam("eqType", num, 0);
						reqParam("eqOn", num, 0);
						reqParam("eq1Type", num, 0);
						reqParam("eq4Type", num, 0);
						reqParam("eq1HPF", num, 0);
						reqParam("eq4LPF", num, 0);
						reqParam("eq1Q", num, 0);
						reqParam("eq1F", num, 0);
						reqParam("eq1G", num, 0);
						reqParam("eq2Q", num, 0);
						reqParam("eq2F", num, 0);
						reqParam("eq2G", num, 0);
						reqParam("eq3Q", num, 0);
						reqParam("eq3F", num, 0);
						reqParam("eq3G", num, 0);
						reqParam("eq4Q", num, 0);
						reqParam("eq4F", num, 0);
						reqParam("eq4G", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C4745515F5F5F5F5F":
				case "4C696252636C5F5F4745515F5F5F5F5F":
					num = Convert.ToInt32(msgData.Substring(50, 2), 16);
					if (num >= 0 && num <= 15)
					{
						for (int num25 = 0; num25 < 62; num25++)
						{
							reqParam("GEQBand", num, num25);
						}
					}
					break;
				}
			}
			if (flag)
			{
				return;
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E0F01") && msgData.Length == 30)
			{
				num = Convert.ToInt32(msgData.Substring(16, 2), 16);
				num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(18, 10)), 16);
				switch (msgData.Substring(12, 4))
				{
				case "2301":
					chData[num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
					break;
				case "2401":
					chData[num].setParam("CONSOLE", Channel.Pr.DirectOutOn, 0, num2);
					break;
				case "2501":
					chData[num].setParam("CONSOLE", Channel.Pr.Phase, 0, num2);
					break;
				case "2601":
					chData[num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
					break;
				case "2702":
					chData[num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
					flag = true;
					break;
				case "2801":
					chData[num].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
					flag = true;
					break;
				case "2B01":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0On, 0, num2);
					flag = true;
					break;
				case "2B08":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Type, 0, num2);
					flag = true;
					break;
				case "2B09":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Attack, 0, num2);
					flag = true;
					break;
				case "2B0A":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Range, 0, num2);
					flag = true;
					break;
				case "2B0B":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Hold, 0, num2);
					flag = true;
					break;
				case "2B0C":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Decay, 0, num2);
					flag = true;
					break;
				case "2B0D":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Threshold, 0, num2);
					flag = true;
					break;
				case "2E01":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
					flag = true;
					break;
				case "2E04":
					num2 += 2;
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2);
					flag = true;
					break;
				case "2E05":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
					flag = true;
					break;
				case "2E06":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
					flag = true;
					break;
				case "2E07":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
					flag = true;
					break;
				case "2E08":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
					flag = true;
					break;
				case "2E09":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
					flag = true;
					break;
				case "2E0A":
					chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
					flag = true;
					break;
				case "2902":
					chData[num].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
					flag = true;
					break;
				case "3001":
					chData[num].setParam("CONSOLE", Channel.Pr.EQHPFOn, 0, num2);
					flag = true;
					break;
				case "3002":
					chData[num].setParam("CONSOLE", Channel.Pr.EQHPFFreq, 0, num2);
					flag = true;
					break;
				case "3102":
					chData[num].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
					flag = true;
					break;
				case "3103":
					chData[num].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
					flag = true;
					break;
				case "3104":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, num2);
					flag = true;
					break;
				case "3105":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, num2);
					flag = true;
					break;
				case "3106":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
					flag = true;
					break;
				case "3107":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
					flag = true;
					break;
				case "3109":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
					flag = true;
					break;
				case "310A":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
					flag = true;
					break;
				case "310B":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
					flag = true;
					break;
				case "310D":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
					flag = true;
					break;
				case "310E":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
					flag = true;
					break;
				case "310F":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
					flag = true;
					break;
				case "3111":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
					flag = true;
					break;
				case "3112":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
					flag = true;
					break;
				case "3113":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
					flag = true;
					break;
				case "3115":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
					flag = true;
					break;
				case "3116":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
					flag = true;
					break;
				case "3117":
					chData[num].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
					flag = true;
					break;
				}
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E7F01") && msgData.Length >= 28)
			{
				for (int num30 = 0; num30 < (msgData.Length - 20) / 8; num30++)
				{
					num = Convert.ToInt32(msgData.Substring(16, 2), 16);
					if (num == 127)
					{
						num = num30;
					}
					num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(18 + num30 * 8, 8)), 16);
					if (num2 > 134217727)
					{
						num2 -= 268435456;
					}
					switch (msgData.Substring(12, 4))
					{
					case "1900":
						ackParam(Channel.Pr.InsertOn, num);
						chData[num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
						flag = true;
						break;
					case "2202":
						ackParam(Channel.Pr.DirectOutOn, num);
						chData[num].setParam("CONSOLE", Channel.Pr.DirectOutOn, 0, num2 == 1);
						flag = true;
						break;
					case "1700":
						ackParam(Channel.Pr.Phase, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Phase, 0, num2);
						flag = true;
						break;
					case "1800":
						ackParam(Channel.Pr.Pair, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
						flag = true;
						break;
					case "1A00":
						ackParam(Channel.Pr.On, num);
						chData[num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
						flag = true;
						break;
					case "1B00":
						ackParam(Channel.Pr.Pan, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
						flag = true;
						break;
					case "1C00":
						ackParam(Channel.Pr.Fader, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
						flag = true;
						break;
					case "2200":
						ackParam(Channel.Pr.ToStereoOn, num);
						chData[num].setParam("CONSOLE", Channel.Pr.ToStereoOn, 0, num2 == 1);
						flag = true;
						break;
					case "1E00":
						ackParam(Channel.Pr.Dyn0On, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0On, 0, num2);
						flag = true;
						break;
					case "1E05":
						ackParam(Channel.Pr.Dyn0Type, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Type, 0, num2);
						flag = true;
						break;
					case "1E06":
						ackParam(Channel.Pr.Dyn0Attack, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Attack, 0, num2);
						flag = true;
						break;
					case "1E07":
						ackParam(Channel.Pr.Dyn0Range, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Range, 0, num2);
						flag = true;
						break;
					case "1E08":
						ackParam(Channel.Pr.Dyn0Hold, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Hold, 0, num2);
						flag = true;
						break;
					case "1E09":
						ackParam(Channel.Pr.Dyn0Decay, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Decay, 0, num2);
						flag = true;
						break;
					case "1E0A":
						ackParam(Channel.Pr.Dyn0Threshold, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn0Threshold, 0, num2);
						flag = true;
						break;
					case "1F01":
						ackParam(Channel.Pr.Dyn1On, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
						flag = true;
						break;
					case "1F03":
						ackParam(Channel.Pr.Dyn1Type, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2 + 2);
						flag = true;
						break;
					case "1F04":
						ackParam(Channel.Pr.Dyn1Attack, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
						flag = true;
						break;
					case "1F05":
						ackParam(Channel.Pr.Dyn1Release, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
						flag = true;
						break;
					case "1F06":
						ackParam(Channel.Pr.Dyn1Ratio, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
						flag = true;
						break;
					case "1F07":
						ackParam(Channel.Pr.Dyn1Gain, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
						flag = true;
						break;
					case "1F08":
						ackParam(Channel.Pr.Dyn1Knee, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
						flag = true;
						break;
					case "1F09":
						ackParam(Channel.Pr.Dyn1Threshold, num);
						chData[num].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
						flag = true;
						break;
					case "1D00":
						ackParam(Channel.Pr.EQAtt, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
						flag = true;
						break;
					case "2000":
						ackParam(Channel.Pr.EQType, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
						flag = true;
						break;
					case "200F":
						ackParam(Channel.Pr.EQOn, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
						flag = true;
						break;
					case "2004":
						ackParam(Channel.Pr.EQ1HPFOn, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
						flag = true;
						break;
					case "200E":
						ackParam(Channel.Pr.EQ4LPFOn, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
						flag = true;
						break;
					case "2001":
						ackParam(Channel.Pr.EQ1Q, num);
						switch (num2)
						{
						case 41:
							chData[num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 1);
							break;
						case 44:
							chData[num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 2);
							break;
						default:
							chData[num].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
							chData[num].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 0);
							break;
						}
						flag = true;
						break;
					case "2002":
						ackParam(Channel.Pr.EQ1F, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
						flag = true;
						break;
					case "2003":
						ackParam(Channel.Pr.EQ1G, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
						flag = true;
						break;
					case "2005":
						ackParam(Channel.Pr.EQ2Q, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
						flag = true;
						break;
					case "2006":
						ackParam(Channel.Pr.EQ2F, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
						flag = true;
						break;
					case "2007":
						ackParam(Channel.Pr.EQ2G, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
						flag = true;
						break;
					case "2008":
						ackParam(Channel.Pr.EQ3Q, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
						flag = true;
						break;
					case "2009":
						ackParam(Channel.Pr.EQ3F, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
						flag = true;
						break;
					case "200A":
						ackParam(Channel.Pr.EQ3G, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
						flag = true;
						break;
					case "200B":
						ackParam(Channel.Pr.EQ4Q, num);
						switch (num2)
						{
						case 42:
							chData[num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 1);
							break;
						case 43:
							chData[num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 2);
							break;
						default:
							chData[num].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
							chData[num].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 0);
							break;
						}
						flag = true;
						break;
					case "200C":
						ackParam(Channel.Pr.EQ4F, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
						flag = true;
						break;
					case "200D":
						ackParam(Channel.Pr.EQ4G, num);
						chData[num].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
						flag = true;
						break;
					case "3400":
						ackParam(Channel.Pr.Pair, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
						break;
					case "3500":
						ackParam(Channel.Pr.InsertOn, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
						break;
					case "3600":
						ackParam(Channel.Pr.On, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.On, 0, num2);
						break;
					case "3700":
						ackParam(Channel.Pr.BusType, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.BusType, 0, num2);
						break;
					case "3800":
						ackParam(Channel.Pr.Balance, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Balance, 0, num2);
						break;
					case "3900":
						ackParam(Channel.Pr.Fader, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
						break;
					case "3A00":
						ackParam(Channel.Pr.EQAtt, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
						break;
					case "3B01":
						ackParam(Channel.Pr.Dyn1On, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
						flag = true;
						break;
					case "3B03":
						ackParam(Channel.Pr.Dyn1Type, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2 + 2);
						flag = true;
						break;
					case "3B04":
						ackParam(Channel.Pr.Dyn1Attack, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
						flag = true;
						break;
					case "3B05":
						ackParam(Channel.Pr.Dyn1Release, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
						flag = true;
						break;
					case "3B06":
						ackParam(Channel.Pr.Dyn1Ratio, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
						flag = true;
						break;
					case "3B07":
						ackParam(Channel.Pr.Dyn1Gain, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
						flag = true;
						break;
					case "3B08":
						ackParam(Channel.Pr.Dyn1Knee, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
						flag = true;
						break;
					case "3B09":
						ackParam(Channel.Pr.Dyn1Threshold, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
						flag = true;
						break;
					case "3C00":
						ackParam(Channel.Pr.EQType, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
						break;
					case "3C0F":
						ackParam(Channel.Pr.EQOn, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
						break;
					case "3C04":
						ackParam(Channel.Pr.EQ1HPFOn, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
						break;
					case "3C0E":
						ackParam(Channel.Pr.EQ4LPFOn, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
						break;
					case "3C01":
						ackParam(Channel.Pr.EQ1Q, num);
						switch (num2)
						{
						case 41:
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 1);
							break;
						case 44:
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 2);
							break;
						default:
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 0);
							break;
						}
						break;
					case "3C02":
						ackParam(Channel.Pr.EQ1F, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
						break;
					case "3C03":
						ackParam(Channel.Pr.EQ1G, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
						break;
					case "3C05":
						ackParam(Channel.Pr.EQ2Q, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
						break;
					case "3C06":
						ackParam(Channel.Pr.EQ2F, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
						break;
					case "3C07":
						ackParam(Channel.Pr.EQ2G, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
						break;
					case "3C08":
						ackParam(Channel.Pr.EQ3Q, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
						break;
					case "3C09":
						ackParam(Channel.Pr.EQ3F, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
						break;
					case "3C0A":
						ackParam(Channel.Pr.EQ3G, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
						break;
					case "3C0B":
						ackParam(Channel.Pr.EQ4Q, num);
						switch (num2)
						{
						case 42:
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 1);
							break;
						case 43:
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 2);
							break;
						default:
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
							chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 0);
							break;
						}
						break;
					case "3C0C":
						ackParam(Channel.Pr.EQ4F, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
						break;
					case "3C0D":
						ackParam(Channel.Pr.EQ4G, num);
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
						break;
					case "2700":
						ackParam(Channel.Pr.Pair, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Pair, 0, num2);
						break;
					case "2800":
						ackParam(Channel.Pr.InsertOn, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
						break;
					case "2900":
						ackParam(Channel.Pr.On, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.On, 0, num2);
						break;
					case "2A00":
						ackParam(Channel.Pr.Balance, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Balance, 0, num2);
						break;
					case "2B00":
						ackParam(Channel.Pr.Fader, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
						break;
					case "2C00":
						ackParam(Channel.Pr.EQAtt, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
						break;
					case "3200":
						ackParam(Channel.Pr.ToStereoOn, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.ToStereoOn, 0, num2);
						break;
					case "3202":
						ackParam(Channel.Pr.Pan, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
						break;
					case "2D01":
						ackParam(Channel.Pr.Dyn1On, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
						flag = true;
						break;
					case "2D03":
						ackParam(Channel.Pr.Dyn1Type, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2 + 2);
						flag = true;
						break;
					case "2D04":
						ackParam(Channel.Pr.Dyn1Attack, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
						flag = true;
						break;
					case "2D05":
						ackParam(Channel.Pr.Dyn1Release, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
						flag = true;
						break;
					case "2D06":
						ackParam(Channel.Pr.Dyn1Ratio, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
						flag = true;
						break;
					case "2D07":
						ackParam(Channel.Pr.Dyn1Gain, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
						flag = true;
						break;
					case "2D08":
						ackParam(Channel.Pr.Dyn1Knee, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
						flag = true;
						break;
					case "2D09":
						ackParam(Channel.Pr.Dyn1Threshold, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
						flag = true;
						break;
					case "2E00":
						ackParam(Channel.Pr.EQType, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
						break;
					case "2E0F":
						ackParam(Channel.Pr.EQOn, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
						break;
					case "2E04":
						ackParam(Channel.Pr.EQ1HPFOn, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
						break;
					case "2E0E":
						ackParam(Channel.Pr.EQ4LPFOn, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
						break;
					case "2E01":
						ackParam(Channel.Pr.EQ1Q, num);
						switch (num2)
						{
						case 41:
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 1);
							break;
						case 44:
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 2);
							break;
						default:
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 0);
							break;
						}
						break;
					case "2E02":
						ackParam(Channel.Pr.EQ1F, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
						break;
					case "2E03":
						ackParam(Channel.Pr.EQ1G, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
						break;
					case "2E05":
						ackParam(Channel.Pr.EQ2Q, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
						break;
					case "2E06":
						ackParam(Channel.Pr.EQ2F, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
						break;
					case "2E07":
						ackParam(Channel.Pr.EQ2G, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
						break;
					case "2E08":
						ackParam(Channel.Pr.EQ3Q, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
						break;
					case "2E09":
						ackParam(Channel.Pr.EQ3F, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
						break;
					case "2E0A":
						ackParam(Channel.Pr.EQ3G, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
						break;
					case "2E0B":
						ackParam(Channel.Pr.EQ4Q, num);
						switch (num2)
						{
						case 42:
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 1);
							break;
						case 43:
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 2);
							break;
						default:
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
							chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 0);
							break;
						}
						break;
					case "2E0C":
						ackParam(Channel.Pr.EQ4F, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
						break;
					case "2E0D":
						ackParam(Channel.Pr.EQ4G, num);
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
						break;
					case "4C00":
						ackParam(Channel.Pr.InsertOn, num);
						chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.InsertOn, 0, num2);
						break;
					case "4D00":
						ackParam(Channel.Pr.On, num);
						chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.On, 0, num2);
						break;
					case "4E00":
						ackParam(Channel.Pr.Pan, num);
						chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.Pan, 0, num2);
						break;
					case "4F00":
						ackParam(Channel.Pr.Fader, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Fader, 0, num2);
						break;
					case "5000":
						ackParam(Channel.Pr.EQAtt, num);
						chData[chOffsetStereo + num].setParam("CONSOLE", Channel.Pr.EQAtt, 0, num2);
						break;
					case "5101":
						ackParam(Channel.Pr.Dyn1On, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1On, 0, num2);
						flag = true;
						break;
					case "5103":
						ackParam(Channel.Pr.Dyn1Type, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Type, 0, num2 + 2);
						flag = true;
						break;
					case "5104":
						ackParam(Channel.Pr.Dyn1Attack, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Attack, 0, num2);
						flag = true;
						break;
					case "5105":
						ackParam(Channel.Pr.Dyn1Release, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Release, 0, num2);
						flag = true;
						break;
					case "5106":
						ackParam(Channel.Pr.Dyn1Ratio, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Ratio, 0, num2);
						flag = true;
						break;
					case "5107":
						ackParam(Channel.Pr.Dyn1Gain, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Gain, 0, num2);
						flag = true;
						break;
					case "5108":
						ackParam(Channel.Pr.Dyn1Knee, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Knee, 0, num2);
						flag = true;
						break;
					case "5109":
						ackParam(Channel.Pr.Dyn1Threshold, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Dyn1Threshold, 0, num2);
						flag = true;
						break;
					case "5200":
						ackParam(Channel.Pr.EQType, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQType, 0, num2);
						break;
					case "520F":
						ackParam(Channel.Pr.EQOn, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQOn, 0, num2);
						break;
					case "5204":
						ackParam(Channel.Pr.EQ1HPFOn, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1HPFOn, 0, num2);
						break;
					case "520E":
						ackParam(Channel.Pr.EQ4LPFOn, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4LPFOn, 0, num2);
						break;
					case "5201":
						ackParam(Channel.Pr.EQ1Q, num);
						switch (num2)
						{
						case 41:
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 1);
							break;
						case 44:
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, 40);
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 2);
							break;
						default:
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1Q, 0, num2);
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1Type, 0, 0);
							break;
						}
						break;
					case "5202":
						ackParam(Channel.Pr.EQ1F, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1F, 0, num2);
						break;
					case "5203":
						ackParam(Channel.Pr.EQ1G, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ1G, 0, num2);
						break;
					case "5205":
						ackParam(Channel.Pr.EQ2Q, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ2Q, 0, num2);
						break;
					case "5206":
						ackParam(Channel.Pr.EQ2F, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ2F, 0, num2);
						break;
					case "5207":
						ackParam(Channel.Pr.EQ2G, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ2G, 0, num2);
						break;
					case "5208":
						ackParam(Channel.Pr.EQ3Q, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ3Q, 0, num2);
						break;
					case "5209":
						ackParam(Channel.Pr.EQ3F, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ3F, 0, num2);
						break;
					case "520A":
						ackParam(Channel.Pr.EQ3G, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ3G, 0, num2);
						break;
					case "520B":
						ackParam(Channel.Pr.EQ4Q, num);
						switch (num2)
						{
						case 42:
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 1);
							break;
						case 43:
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, 40);
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 2);
							break;
						default:
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4Q, 0, num2);
							chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4Type, 0, 0);
							break;
						}
						break;
					case "520C":
						ackParam(Channel.Pr.EQ4F, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4F, 0, num2);
						break;
					case "520D":
						ackParam(Channel.Pr.EQ4G, num);
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.EQ4G, 0, num2);
						break;
					case "5D00":
						setParam("CONSOLE", num, Pr.MuteMaster, 0, num2);
						flag = true;
						break;
					}
					switch (msgData.Substring(12, 2))
					{
					case "26":
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						if (group >= 8 && group <= 15)
						{
							chData[num].setParam("LOGIC", Channel.Pr.MuteGroup, group - 8, num2);
						}
						flag = true;
						break;
					case "22":
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						if (group >= 3 && group <= 10)
						{
							chData[num].setParam("LOGIC", Channel.Pr.DCAGroup, group - 3, num2);
						}
						flag = true;
						break;
					case "23":
					{
						int mix = Convert.ToInt32(msgData.Substring(14, 2), 16) / 3;
						switch (Convert.ToInt32(msgData.Substring(14, 2), 16) % 3)
						{
						case 0:
							chData[num].setParam("CONSOLE", Channel.Pr.SendToMixOn, mix, num2);
							reqQueue.RemoveAll((reqQueueItem item) => item.channelParam == Channel.Pr.SendToMixOn && item.index == mix);
							break;
						case 1:
							chData[num].setParam("CONSOLE", Channel.Pr.SendToMixPre, mix, num2);
							reqQueue.RemoveAll((reqQueueItem item) => item.channelParam == Channel.Pr.SendToMixPre && item.index == mix);
							break;
						case 2:
							chData[num].setParam("CONSOLE", Channel.Pr.SendToMix, mix, num2);
							reqQueue.RemoveAll((reqQueueItem item) => item.channelParam == Channel.Pr.SendToMix && item.index == mix);
							break;
						}
						flag = true;
						break;
					}
					case "24":
						group = Convert.ToInt32(msgData.Substring(14, 2), 16) * 2;
						chData[num].setParam("CONSOLE", Channel.Pr.SendToMixPan, group, num2);
						reqQueue.RemoveAll((reqQueueItem item) => item.channelParam == Channel.Pr.SendToMixPan && item.index == group);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					return;
				}
			}
			if ((msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E0D") || msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E1A")) && msgData.Length >= 28)
			{
				for (int num31 = 0; num31 < (msgData.Length - 20) / 8; num31++)
				{
					num = Convert.ToInt32(msgData.Substring(16, 2), 16);
					if (num == 127)
					{
						num = num31;
					}
					switch (msgData.Substring(10, 4))
					{
					case "0201":
						ackParam(Channel.Pr.PatchIn, num);
						num2 = Convert.ToInt32(UnStuffValue(msgData.Substring(num31 * 8 + 18, 8)), 16);
						chData[num].setParam("CONSOLE", Channel.Pr.PatchIn, 0, num2);
						flag = true;
						break;
					case "0204":
						ackParam(Channel.Pr.Name, num);
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						text3 = chData[num].name.PadRight(17);
						text2 = text3.Substring(0, group) + UnStuffString(msgData.Substring(num31 * 8 + 24, 2)) + text3.Substring(group + 1, 16 - group);
						text2 = text2.Substring(0, 4).TrimEnd();
						chData[num].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						flag = true;
						break;
					case "020F":
						ackParam(Channel.Pr.Name, num);
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						text3 = chData[num + chOffsetMatrix].name.PadRight(17);
						text2 = text3.Substring(0, group) + UnStuffString(msgData.Substring(num31 * 8 + 24, 2)) + text3.Substring(group + 1, 16 - group);
						text2 = text2.Substring(0, 4).TrimEnd();
						chData[num + chOffsetMatrix].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						flag = true;
						break;
					case "0210":
						ackParam(Channel.Pr.Name, num);
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						text3 = chData[num + chOffsetMix].name.PadRight(17);
						text2 = text3.Substring(0, group) + UnStuffString(msgData.Substring(num31 * 8 + 24, 2)) + text3.Substring(group + 1, 16 - group);
						text2 = text2.Substring(0, 4).TrimEnd();
						chData[num + chOffsetMix].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						flag = true;
						break;
					case "0212":
						ackParam(Channel.Pr.Name, num);
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						text3 = chData[num + chOffsetStereo].name.PadRight(17);
						text2 = text3.Substring(0, group) + UnStuffString(msgData.Substring(num31 * 8 + 24, 2)) + text3.Substring(group + 1, 16 - group);
						text2 = text2.Substring(0, 4).TrimEnd();
						chData[num + chOffsetStereo].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						chData[num + chOffsetStereo + 1].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						flag = true;
						break;
					case "0217":
						ackParam(Channel.Pr.Name, num);
						group = Convert.ToInt32(msgData.Substring(14, 2), 16);
						text3 = chData[num * 2 + chOffsetSTIN].name.PadRight(17);
						text2 = text3.Substring(0, group) + UnStuffString(msgData.Substring(num31 * 8 + 24, 2)) + text3.Substring(group + 1, 15 - group);
						text2 = text2.Substring(0, 4).TrimEnd();
						chData[num * 2 + chOffsetSTIN].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						chData[num * 2 + chOffsetSTIN + 1].setParam("CONSOLE", Channel.Pr.Name, 0, text2);
						flag = true;
						break;
					case "032D":
					{
						string text4;
						if ((text4 = msgData.Substring(14, 2)) != null && text4 == "01")
						{
							num2 = Convert.ToInt32(msgData.Substring(18, 8), 16);
							Console.CueMode = num2;
							Console.clearCue();
						}
						break;
					}
					case "032E":
					{
						string text4;
						if ((text4 = msgData.Substring(14, 2)) != null && text4 == "00")
						{
							num2 = Convert.ToInt32(msgData.Substring(num31 * 8 + 24, 2), 16);
							setParam("CONSOLE", num, Pr.Cue, 0, num2);
							ackParam(Channel.Pr.Cue, num);
						}
						break;
					}
					case "032F":
					{
						string text4;
						if ((text4 = msgData.Substring(14, 2)) != null && text4 == "00")
						{
							num2 = Convert.ToInt32(msgData.Substring(num31 * 8 + 24, 2), 16);
							if (num < 8)
							{
								setParam("CONSOLE", num + chOffsetMatrix, Pr.Cue, 0, num2);
							}
							else
							{
								setParam("CONSOLE", num - 8 + chOffsetMix, Pr.Cue, 0, num2);
							}
							ackParam(Channel.Pr.Cue, num);
						}
						break;
					}
					case "0409":
					{
						string text4;
						if ((text4 = msgData.Substring(14, 2)) != null && text4 == "18")
						{
							num2 = Convert.ToInt32(msgData.Substring(18, 8), 16);
							Console.selChannel = num2;
						}
						break;
					}
					case "040A":
						if (msgData.Substring(14, 2).Equals("01"))
						{
							ackParam(Pr.SceneNum, 0);
							group = Convert.ToInt32(UnStuffValue(msgData.Substring(18, 8)), 16) - 1;
							Console.sceneNum = group;
							setParam("LOGIC", Console.sceneNum, Pr.SceneName, 0, sceneData[group].sceneName);
							reqQueue.Add(new reqQueueItem(Pr.SceneName, 0, group));
							flag = true;
						}
						break;
					case "0424":
						num2 = Convert.ToInt32(msgData.Substring(18, 8), 16);
						logDebug("Got Word Clock Change to: " + num2);
						switch (num2)
						{
						case 0:
							Console.clockSpeed = 44;
							break;
						case 1:
							Console.clockSpeed = 48;
							break;
						case 2:
							Console.clockSpeed = 88;
							break;
						case 3:
							Console.clockSpeed = 96;
							break;
						}
						break;
					}
				}
			}
			if (msgData.StartsWith("F0431" + Settings.Default.MIDI_Channel + "3E7F") && msgData.Length >= 24)
			{
				switch (msgData.Substring(10, 4))
				{
				case "1000":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(14, 4)), 16);
					logDebug("Console SCENE Lib Recall for scene " + num);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "5000":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(14, 4)), 16);
					logDebug("Remote SCENE Lib Recall for scene " + num);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "1020":
					Console.sceneNum = Convert.ToInt32(UnStuffValue(msgData.Substring(14, 4)), 16);
					sceneData[Console.sceneNum].isValid = true;
					setParam("CONSOLE", Console.sceneNum, Pr.SceneSave, 0, 1);
					flag = true;
					break;
				case "5020":
					if (Convert.ToInt32(UnStuffValue(msgData.Substring(24, 4)), 16) != 0)
					{
						MixEngine.NewMessage("Scene Save Failed", 3);
					}
					else
					{
						Console.sceneNum = Convert.ToInt32(UnStuffValue(msgData.Substring(14, 4)), 16);
						Console.sceneName = sceneData[Console.sceneNum].sceneName;
						sceneData[Console.sceneNum].isValid = true;
						setParam("CONSOLE", Console.sceneNum, Pr.SceneSave, 0, 1);
						MixEngine.NewMessage("Scene Save Confirmed", 2);
					}
					flag = true;
					break;
				case "1007":
				case "5007":
				case "1008":
				case "5008":
					reqQueue.Add(new reqQueueItem(Channel.Pr.Name, 0, 127));
					reqQueue.Add(new reqQueueItem(Channel.Pr.PatchIn, 0, 127));
					break;
				case "524C696252636C5F5343454E455F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
					setParam("CONSOLE", num, Pr.SceneLoad, 0, 1);
					flag = true;
					break;
				case "1001":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(18, 4)), 16);
					logDebug("EQ Lib Recall on channel " + num);
					if (num >= 512)
					{
						num = num - 512 + chOffsetStereo;
					}
					else if (num >= 256)
					{
						num = num - 256 + chOffsetMix;
					}
					else if (num >= 128)
					{
						num = num - 128 + chOffsetMatrix;
					}
					if (num >= 0 && num < chData.Length)
					{
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQAtt, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQType, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQOn, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1HPFOn, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4LPFOn, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1Q, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1F, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1G, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2Q, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2F, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2G, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3Q, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3F, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3G, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4Q, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4F, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4G, 0, num));
					}
					flag = true;
					break;
				case "1002":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(18, 4)), 16);
					logDebug("GATE Lib Recall on channel " + num);
					if (num >= 512)
					{
						num = num - 512 + chOffsetStereo;
					}
					else if (num >= 256)
					{
						num = num - 256 + chOffsetMix;
					}
					else if (num >= 128)
					{
						num = num - 128 + chOffsetMatrix;
					}
					if (num >= 0 && num < chData.Length)
					{
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0On, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Type, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Attack, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Range, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Hold, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Decay, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Threshold, 0, num));
					}
					flag = true;
					break;
				case "1003":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(18, 4)), 16);
					logDebug("COMP Lib Recall on channel " + num);
					if (num >= 512)
					{
						num = num - 512 + chOffsetStereo;
					}
					else if (num >= 256)
					{
						num = num - 256 + chOffsetMix;
					}
					else if (num >= 128)
					{
						num = num - 128 + chOffsetMatrix;
					}
					if (num >= 0 && num < chData.Length)
					{
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1On, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Type, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Attack, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Release, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Ratio, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Gain, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Knee, 0, num));
						reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Threshold, 0, num));
					}
					flag = true;
					break;
				case "1006":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(18, 4)), 16);
					logDebug("CHANNEL Lib Recall on channel " + num);
					if (num >= 512)
					{
						num = num - 512 + chOffsetStereo;
					}
					else if (num >= 256)
					{
						num = num - 256 + chOffsetMix;
					}
					else if (num >= 128)
					{
						num = num - 128 + chOffsetMatrix;
					}
					slowChannelSync(num);
					break;
				case "1040":
					group = Convert.ToInt32(UnStuffValue(msgData.Substring(14, 4)), 16);
					ackParam(Pr.SceneName, group);
					text2 = hexToString(msgData.Substring(18, 32));
					text2 = text2.TrimEnd();
					sceneData[group].isValid = true;
					setParam("LOGIC", group, Pr.SceneName, 0, text2);
					flag = true;
					break;
				}
				switch (msgData.Substring(10, 14))
				{
				case "4C696241747262745343454E455F5F5F":
					if (msgData.Substring(48, 4) == "024B")
					{
						num = Convert.ToInt32(UnStuffValue(msgData.Substring(44, 4)), 16);
						int num33 = int.Parse(msgData.Substring(54, 2));
						Buffer.BlockCopy(UnStuffBytes(msgData.Substring(60, 10)), 0, sceneData[num].byteName, num33 * 4, 4);
						setParam("CONSOLE", num, Pr.SceneName, 0, sceneData[num].rawName);
						if (sceneNameQueue.Count > 0 && num33 == 0)
						{
							MixEngine.NewMessage("Load Scene " + sceneNameQueue.Peek().ToString("000") + "...", 0);
							logDebug("Request Scene Name #" + sceneNameQueue.Peek());
							reqParam("SceneName", sceneNameQueue.Dequeue());
						}
					}
					flag = true;
					break;
				case "4C696245786973745343454E455F5F5F":
					if (msgData.Substring(44, 2) == "01")
					{
						int num34 = Convert.ToInt32(UnStuffValue(msgData.Substring(50, 4)), 16);
						int num35 = Convert.ToInt32(UnStuffValue(msgData.Substring(54, 4)), 16);
						for (int num36 = num34; num36 <= num35; num36++)
						{
							sceneData[num36].isValid = true;
							logDebug("Enqueue Scene Name #" + num36);
							sceneNameQueue.Enqueue(num36);
						}
						MixEngine.NewMessage("Load Scene " + sceneNameQueue.Peek().ToString("000") + "...", 0);
						logDebug("Request Scene Name #" + sceneNameQueue.Peek());
						reqParam("SceneName", sceneNameQueue.Dequeue());
						Console.sceneHigh = num35;
						MixEngine.NewMessage("Top Scene " + Console.sceneHigh.ToString("000") + "...", 0);
						if (num35 < 300)
						{
							reqParam("SceneRange", num35 + 1, 0);
						}
					}
					flag = true;
					break;
				case "4C6962556E52636C44594E415F5F5F5F":
				case "4C696252636C5F5F44594E415F5F5F5F":
					num = Convert.ToInt32(msgData.Substring(50, 2), 16);
					if (num >= 0 && num <= 72)
					{
						reqParam("Dyn0On", num, 0);
						reqParam("Dyn0Type", num, 0);
						reqParam("Dyn0Attack", num, 0);
						reqParam("Dyn0Range", num, 0);
						reqParam("Dyn0Hold", num, 0);
						reqParam("Dyn0Decay", num, 0);
						reqParam("Dyn0Ratio", num, 0);
						reqParam("Dyn0Gain", num, 0);
						reqParam("Dyn0Knee", num, 0);
						reqParam("Dyn0Threshold", num, 0);
						reqParam("Dyn1On", num, 0);
						reqParam("Dyn1Type", num, 0);
						reqParam("Dyn1Attack", num, 0);
						reqParam("Dyn1Range", num, 0);
						reqParam("Dyn1Hold", num, 0);
						reqParam("Dyn1Release", num, 0);
						reqParam("Dyn1Ratio", num, 0);
						reqParam("Dyn1Gain", num, 0);
						reqParam("Dyn1Knee", num, 0);
						reqParam("Dyn1Threshold", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C494E45515F5F5F5F":
				case "4C696252636C5F5F494E45515F5F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16);
					if (num >= 0 && num < chData.Length)
					{
						reqParam("eqHPFOn", num, 0);
						reqParam("eqHPFFreq", num, 0);
						reqParam("eqAtt", num, 0);
						reqParam("eqType", num, 0);
						reqParam("eqOn", num, 0);
						reqParam("eq1Type", num, 0);
						reqParam("eq4Type", num, 0);
						reqParam("eq1HPF", num, 0);
						reqParam("eq4LPF", num, 0);
						reqParam("eq1Q", num, 0);
						reqParam("eq1F", num, 0);
						reqParam("eq1G", num, 0);
						reqParam("eq2Q", num, 0);
						reqParam("eq2F", num, 0);
						reqParam("eq2G", num, 0);
						reqParam("eq3Q", num, 0);
						reqParam("eq3F", num, 0);
						reqParam("eq3G", num, 0);
						reqParam("eq4Q", num, 0);
						reqParam("eq4F", num, 0);
						reqParam("eq4G", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C4F555445515F5F5F":
				case "4C696252636C5F5F4F555445515F5F5F":
					num = Convert.ToInt32(UnStuffValue(msgData.Substring(48, 4)), 16);
					if (num >= 256 && num <= 271)
					{
						num += chOffsetMix - 256;
					}
					else if (num >= 512 && num <= 519)
					{
						num += chOffsetMatrix - 512;
					}
					else if (num >= 1024 && num <= 1026)
					{
						num += chOffsetStereo - 1024;
					}
					if (num >= 0 && num < chData.Length)
					{
						reqParam("eqHPFOn", num, 0);
						reqParam("eqHPFFreq", num, 0);
						reqParam("eqAtt", num, 0);
						reqParam("eqType", num, 0);
						reqParam("eqOn", num, 0);
						reqParam("eq1Type", num, 0);
						reqParam("eq4Type", num, 0);
						reqParam("eq1HPF", num, 0);
						reqParam("eq4LPF", num, 0);
						reqParam("eq1Q", num, 0);
						reqParam("eq1F", num, 0);
						reqParam("eq1G", num, 0);
						reqParam("eq2Q", num, 0);
						reqParam("eq2F", num, 0);
						reqParam("eq2G", num, 0);
						reqParam("eq3Q", num, 0);
						reqParam("eq3F", num, 0);
						reqParam("eq3G", num, 0);
						reqParam("eq4Q", num, 0);
						reqParam("eq4F", num, 0);
						reqParam("eq4G", num, 0);
					}
					flag = true;
					break;
				case "4C6962556E52636C4745515F5F5F5F5F":
				case "4C696252636C5F5F4745515F5F5F5F5F":
					num = Convert.ToInt32(msgData.Substring(50, 2), 16);
					if (num >= 0 && num <= 15)
					{
						for (int num32 = 0; num32 < 62; num32++)
						{
							reqParam("GEQBand", num, num32);
						}
					}
					break;
				}
			}
			if (!flag)
			{
			}
		}
		catch (Exception ex)
		{
			logError("ProcessSysEx::BulkSection", "InEvent", ex.ToString());
		}
	}

	public static void ShowError(string strMessage, int errorCode)
	{
		MixEngine.NewMessage(strMessage, errorCode);
	}

	public static void OnAction(string property, int index)
	{
		if (MixEngine.Action != null)
		{
			MixEngine.Action(property, index);
		}
	}

	public static void PushChannel(Channel.Pr param, int channel, int paramIndex, int value)
	{
		if (MixEngine.ChannelChanged != null)
		{
			MixEngine.ChannelChanged(param, channel, paramIndex, value);
		}
	}

	public static void PushEngine(Pr param, int rack, int paramIndex, int value)
	{
		if (MixEngine.EngineChanged != null)
		{
			MixEngine.EngineChanged(param, rack, paramIndex, value);
		}
	}

	public static void PushEFXParam(int objectIndex, string param, int paramIndex)
	{
		if (MixEngine.EFXParamChanged != null)
		{
			MixEngine.EFXParamChanged(objectIndex, param, paramIndex);
		}
	}

	private static string reverseString(string s)
	{
		char[] array = s.ToCharArray();
		Array.Reverse(array);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] += '\u0001';
		}
		return new string(array);
	}

	public static string hexToString(string hexString)
	{
		try
		{
			string text = string.Empty;
			for (int i = 0; i < hexString.Length; i += 2)
			{
				string empty = string.Empty;
				empty = hexString.Substring(i, 2);
				uint value = Convert.ToUInt32(empty, 16);
				char c = Convert.ToChar(value);
				text += c;
			}
			return text;
		}
		catch (Exception ex)
		{
			System.Console.WriteLine(ex.Message);
		}
		return string.Empty;
	}

	public static string stringToHex(string asciiString)
	{
		string text = "";
		foreach (char c in asciiString)
		{
			int num = c;
			text += $"{Convert.ToUInt32(num.ToString()):X2}";
		}
		return text;
	}

	public static string getCPUID()
	{
		try
		{
			string text = string.Empty;
			ManagementClass managementClass = new ManagementClass("win32_processor");
			ManagementObjectCollection instances = managementClass.GetInstances();
			foreach (ManagementObject item in instances)
			{
				if (text == "")
				{
					text = item.Properties["processorID"].Value.ToString();
					break;
				}
			}
			return text;
		}
		catch (Exception)
		{
			return "";
		}
	}

	public static string getHDID(string drive)
	{
		ManagementObject managementObject = new ManagementObject("win32_logicaldisk.deviceid=\"" + drive + ":\"");
		managementObject.Get();
		return managementObject["VolumeSerialNumber"].ToString();
	}

	private static int checkMAC(string macaddr)
	{
		string text = reverseString(macaddr.ToUpper());
		string machineID = getCPUID() + ":" + getHDID("C");
		List<string> list = new List<string>();
		list.Add("W");
		list.Add("I");
		list.Add("3");
		list.Add("7");
		list.Add("0");
		list.Add("Y");
		list.Add("A");
		list.Add("C");
		list.Add("P");
		list.Add("1");
		list.Add("M");
		list.Add("5");
		list.Add("H");
		list.Add("V");
		list.Add("X");
		list.Add("O");
		list.Add("N");
		list.Add("F");
		list.Add("2");
		list.Add("S");
		list.Add("G");
		list.Add("J");
		list.Add("9");
		list.Add("Z");
		list.Add("D");
		list.Add("E");
		list.Add("8");
		list.Add("Q");
		list.Add("U");
		list.Add("T");
		list.Add("B");
		list.Add("R");
		list.Add("4");
		list.Add("6");
		list.Add("L");
		list.Add("K");
		List<string> list2 = list;
		int num = 0;
		int x;
		for (x = 0; x < machineID.Length; x++)
		{
			if (list2.Contains(machineID.Substring(x, 1)))
			{
				num = (num + list2.FindIndex((string code) => code == machineID.Substring(x, 1)) * (x + 1)) % 250624;
			}
		}
		if (DateTime.Now >= DateTime.Parse("2010-12-14 12:00"))
		{
			return -2;
		}
		if (text == ":CFB63FE1B11")
		{
			return 1;
		}
		if (text == "88FB63FE1B11")
		{
			return 1;
		}
		return -99;
	}

	public static bool slowDump(bool retry)
	{
		logDebug("Starting SlowDump...");
		reqQueue.Add(new reqQueueItem(Channel.Pr.Cue, 0, 127));
		reqQueue.Add(new reqQueueItem(Pr.SceneNum, 0, 0));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pair, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Fader, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.On, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pan, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Phase, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.ToStereoOn, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.PatchIn, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Name, 0, 127));
		for (int i = 0; i < 8; i++)
		{
			reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMix, i, 127));
			reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMixOn, i, 127));
			reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMixPre, i, 127));
			if (i % 2 == 0)
			{
				reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMixPan, i, 127));
			}
		}
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQType, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1Q, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1F, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1G, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1HPFOn, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2Q, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2F, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2G, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3Q, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3F, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3G, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4Q, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4F, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4G, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4LPFOn, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQOn, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQAtt, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0On, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Type, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Attack, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Range, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Hold, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Decay, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Threshold, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1On, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Type, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Attack, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Release, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Ratio, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Gain, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Knee, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Threshold, 0, 127));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pair, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Fader, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.On, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Name, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQType, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1Q, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1F, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1G, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1HPFOn, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2Q, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2F, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2G, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3Q, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3F, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3G, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4Q, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4F, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4G, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4LPFOn, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQOn, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQAtt, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1On, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Type, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Attack, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Release, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Ratio, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Gain, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Knee, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Threshold, 0, 511));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pair, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Fader, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.On, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pan, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.ToStereoOn, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Name, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQType, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1Q, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1F, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1G, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1HPFOn, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2Q, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2F, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2G, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3Q, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3F, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3G, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4Q, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4F, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4G, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4LPFOn, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQOn, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQAtt, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1On, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Type, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Attack, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Release, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Ratio, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Gain, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Knee, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Threshold, 0, 255));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Fader, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.On, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Name, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQType, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1Q, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1F, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1G, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1HPFOn, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2Q, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2F, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2G, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3Q, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3F, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3G, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4Q, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4F, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4G, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4LPFOn, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQOn, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQAtt, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1On, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Type, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Attack, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Release, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Ratio, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Gain, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Knee, 0, 512));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Threshold, 0, 512));
		reqParam("MuteMaster", 127);
		for (int j = 0; j < 8; j++)
		{
			reqParam("MuteGroupMembers", j);
			reqParam("DCAGroupMembers", j);
		}
		for (int k = 0; k < 100; k++)
		{
			reqQueue.Add(new reqQueueItem(Pr.SceneName, 0, k));
		}
		return true;
	}

	public static bool slowChannelSync(int channel)
	{
		logDebug("Starting SlowChannelSync...");
		reqQueue.Add(new reqQueueItem(Channel.Pr.Cue, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pair, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Fader, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.On, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Pan, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Phase, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.ToStereoOn, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.PatchIn, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Name, 0, channel));
		for (int i = 0; i < 8; i++)
		{
			reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMix, i, channel));
			reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMixOn, i, channel));
			reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMixPre, i, channel));
			if (i % 2 == 0)
			{
				reqQueue.Add(new reqQueueItem(Channel.Pr.SendToMixPan, i, channel));
			}
		}
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQType, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1Q, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1F, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1G, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ1HPFOn, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2Q, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2F, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ2G, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3Q, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3F, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ3G, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4Q, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4F, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4G, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQ4LPFOn, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQOn, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.EQAtt, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0On, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Type, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Attack, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Range, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Hold, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Decay, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn0Threshold, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1On, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Type, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Attack, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Release, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Ratio, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Gain, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Knee, 0, channel));
		reqQueue.Add(new reqQueueItem(Channel.Pr.Dyn1Threshold, 0, channel));
		for (int j = 0; j < 8; j++)
		{
			reqParam("MuteGroupMembers", j);
			reqParam("DCAGroupMembers", j);
		}
		return true;
	}

	private static void initParam(Pr param, int value, int min, int max, string name)
	{
		paramValue[param] = value;
		paramMin[param] = min;
		paramMax[param] = max;
		paramName[param] = name;
		nameParam[name] = param;
	}

	private static void initParams()
	{
		initParam(Pr.MuteMaster, 0, 0, 1, "MuteMaster");
		initParam(Pr.Cue, 0, 0, 1, "Cue");
		initParam(Pr.GEQBand, 0, -150, 150, "GEQBand");
		initParam(Pr.GEQFlat, 0, 0, 1, "GEQFlat");
		initParam(Pr.GEQLink, 0, 0, 8, "GEQLink");
		initParam(Pr.GEQLinkLR, 0, 0, 2, "GEQLinkLR");
		initParam(Pr.GEQOn, 1, 0, 1, "GEQOn");
		initParam(Pr.GEQType, 0, 0, 1, "GEQType");
		initParam(Pr.RackSource, 0, 0, 184, "RackSource");
		initParam(Pr.RackType, 1, 0, 4, "RackType");
		initParam(Pr.HA, 0, 0, 72, "HA");
		initParam(Pr.Phantom, 0, 0, 72, "Phantom");
		initParam(Pr.SceneDump, 0, 0, 1, "SceneDump");
		initParam(Pr.SceneName, 0, 0, 1, "SceneName");
		initParam(Pr.SceneValid, 0, 0, 1, "SceneValid");
		initParam(Pr.SceneLoad, 0, 0, 301, "SceneLoad");
		initParam(Pr.SceneSave, 0, 0, 301, "SceneSave");
	}

	public static string getParamStr(int rack, Pr param, int index)
	{
		if (param == Pr.RackSource)
		{
			if (Console.type == ConsoleTypes.LS9)
			{
				if (RackData[rack]._source == 0)
				{
					return "(No Patch)";
				}
				if (RackData[rack]._source >= 1 && RackData[rack]._source <= 16)
				{
					return chData[RackData[rack]._source - 1 + chOffsetMix].name;
				}
				if (RackData[rack]._source >= 17 && RackData[rack]._source <= 24)
				{
					return chData[RackData[rack]._source - 17 + chOffsetMatrix].name;
				}
				if (RackData[rack]._source >= 95 && RackData[rack]._source <= 126)
				{
					return chData[RackData[rack]._source - 95 + chOffsetInput].name;
				}
				if (RackData[rack]._source >= 127 && RackData[rack]._source <= 142)
				{
					return chData[RackData[rack]._source - 127 + chOffsetMix].name;
				}
				if (RackData[rack]._source >= 143 && RackData[rack]._source <= 150)
				{
					return chData[RackData[rack]._source - 143 + chOffsetMatrix].name;
				}
				if (RackData[rack]._source >= 151 && RackData[rack]._source <= 153)
				{
					return chData[RackData[rack]._source - 151 + chOffsetStereo].name;
				}
				if (RackData[rack]._source >= 0 && RackData[rack]._source < mapOutPatch.Length)
				{
					return mapOutPatch[RackData[rack]._source];
				}
				return "Unknown";
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				if (RackData[rack]._source == 0)
				{
					return "(No Patch)";
				}
				if (RackData[rack]._source >= 1 && RackData[rack]._source <= 16)
				{
					return chData[RackData[rack]._source - 1 + chOffsetMix].name;
				}
				if (RackData[rack]._source >= 17 && RackData[rack]._source <= 24)
				{
					return chData[RackData[rack]._source - 17 + chOffsetMatrix].name;
				}
				if (RackData[rack]._source >= 79 && RackData[rack]._source <= 126)
				{
					return chData[RackData[rack]._source - 79 + chOffsetInput].name;
				}
				if (RackData[rack]._source >= 127 && RackData[rack]._source <= 142)
				{
					return chData[RackData[rack]._source - 127 + chOffsetMix].name;
				}
				if (RackData[rack]._source >= 143 && RackData[rack]._source <= 150)
				{
					return chData[RackData[rack]._source - 143 + chOffsetMatrix].name;
				}
				if (RackData[rack]._source >= 151 && RackData[rack]._source <= 153)
				{
					return chData[RackData[rack]._source - 151 + chOffsetStereo].name;
				}
				if (RackData[rack]._source >= 0 && RackData[rack]._source < mapOutPatch.Length)
				{
					return mapOutPatch[RackData[rack]._source];
				}
				return "Unknown";
			}
			return "Unknown";
		}
		return "";
	}

	public static int getParam(int rack, Pr param, int index)
	{
		switch (param)
		{
		case Pr.MuteMaster:
			if (!muteGroup[rack].muted)
			{
				return 0;
			}
			return 1;
		case Pr.GEQBand:
			return GEQData[rack]._Band[index];
		case Pr.GEQFlat:
			return 1;
		case Pr.GEQLink:
			return GEQData[rack]._link;
		case Pr.GEQLinkLR:
			if (rack > 7)
			{
				rack -= 8;
			}
			return GEQData[rack]._linkLR;
		case Pr.GEQOn:
			return GEQData[rack]._on;
		case Pr.GEQType:
			return GEQData[rack]._type;
		case Pr.RackType:
			return RackData[rack]._type;
		case Pr.HA:
			if (index >= 0 && index < HA.Length)
			{
				return HA[index];
			}
			return 0;
		case Pr.Phantom:
			if (index >= 0 && index < Phantom.Length)
			{
				return Phantom[index];
			}
			return 0;
		default:
			return 0;
		}
	}

	public static void setParam(string source, int channel, Pr param, int index, int value)
	{
		source = source.ToUpper();
		GEQParam gEQParam = GEQParam.GEQ_NONE;
		GenParam genParam = GenParam.GEN_NONE;
		if (param == Pr.None)
		{
			ChannelParam channelParam = new ChannelParam();
			channelParam.channel = 0;
			channelParam.param = ChParam.CH_NONE;
			faderServer.sendProto(source, channelParam);
			PushEngine(param, 0, 0, 0);
			return;
		}
		if (!paramMin.ContainsKey(param))
		{
			logDebug("MixEngine.setParam was called with " + param.ToString() + " which doesn't exist");
			return;
		}
		if (value < paramMin[param] || value > paramMax[param])
		{
			logDebug("Value " + value + " out of range for " + paramName[param] + " on Rack " + channel);
			return;
		}
		switch (param)
		{
		case Pr.MuteMaster:
			genParam = GenParam.GEN_MUTEMASTER;
			muteGroup[channel].muted = value == 1;
			break;
		case Pr.Cue:
			if (value == 1)
			{
				Console.addCue(channel);
				if (Console.PrefCueSelLink)
				{
					Console.selChannel = channel;
				}
				chData[channel].setParam(source, Channel.Pr.Cue, 0, 1);
			}
			else
			{
				Console.removeCue(channel);
				chData[channel].setParam(source, Channel.Pr.Cue, 0, 0);
			}
			break;
		case Pr.GEQBand:
			if (GEQData[channel]._type == 1 && GEQData[channel].BandsAvail <= 0 && GEQData[channel]._Band[index] == 0 && value != 0)
			{
				setParam("LOGIC", channel, Pr.GEQBand, index, 0);
				return;
			}
			gEQParam = GEQParam.GEQ_BAND;
			GEQData[channel]._Band[index] = value;
			if (!(source != "LOGIC"))
			{
				break;
			}
			if (channel > 7)
			{
				if (GEQData[channel - 8]._linkLR != 0)
				{
					setParam("LOGIC", channel - 8, Pr.GEQBand, index, value);
				}
			}
			else if (GEQData[channel]._linkLR != 0)
			{
				setParam("LOGIC", channel + 8, Pr.GEQBand, index, value);
			}
			if (GEQData[channel]._link != 0)
			{
				if (channel % 2 == 0)
				{
					setParam("LOGIC", channel + 1, Pr.GEQBand, index, value);
				}
				else
				{
					setParam("LOGIC", channel - 1, Pr.GEQBand, index, value);
				}
			}
			break;
		case Pr.GEQFlat:
			for (index = 0; index < 31; index++)
			{
				setParam("LOGIC", channel, Pr.GEQBand, index, 0);
			}
			break;
		case Pr.GEQOn:
			gEQParam = GEQParam.GEQ_ON;
			GEQData[channel]._on = value;
			if (!(source != "LOGIC"))
			{
				break;
			}
			if (channel > 7)
			{
				if (GEQData[channel - 8]._linkLR != 0)
				{
					setParam("LOGIC", channel - 8, Pr.GEQOn, 0, value);
				}
			}
			else if (GEQData[channel]._linkLR != 0)
			{
				setParam("LOGIC", channel + 8, Pr.GEQOn, 0, value);
			}
			if (GEQData[channel]._link != 0)
			{
				if (channel % 2 == 0)
				{
					setParam("LOGIC", channel + 1, Pr.GEQOn, 0, value);
				}
				else
				{
					setParam("LOGIC", channel - 1, Pr.GEQOn, 0, value);
				}
			}
			break;
		case Pr.GEQLink:
			gEQParam = GEQParam.GEQ_LINK;
			GEQData[channel]._link = value;
			if (!(source != "LOGIC"))
			{
				break;
			}
			if (channel % 2 == 0)
			{
				setParam("LOGIC", channel + 1, Pr.GEQLink, 0, value);
				if (value > 0)
				{
					for (int k = 0; k < 31; k++)
					{
						setParam("LOGIC", channel + 1, Pr.GEQBand, k, GEQData[channel]._Band[k]);
					}
					setParam("LOGIC", channel + 1, Pr.GEQOn, 0, GEQData[channel]._on);
				}
				break;
			}
			setParam("LOGIC", channel - 1, Pr.GEQLink, 0, value);
			if (value > 0)
			{
				for (int l = 0; l < 31; l++)
				{
					setParam("LOGIC", channel - 1, Pr.GEQBand, l, GEQData[channel]._Band[l]);
				}
				setParam("LOGIC", channel - 1, Pr.GEQOn, 0, GEQData[channel]._on);
			}
			break;
		case Pr.GEQLinkLR:
			gEQParam = GEQParam.GEQ_LINKLR;
			GEQData[channel]._linkLR = value;
			if (!(source != "LOGIC"))
			{
				break;
			}
			setParam("LOGIC", channel + ((channel > 7) ? (-8) : 8), Pr.GEQLinkLR, 0, value);
			switch (value)
			{
			case 1:
			{
				for (int j = 0; j < 31; j++)
				{
					setParam("LOGIC", channel + ((channel <= 7) ? 8 : 0), Pr.GEQBand, j, GEQData[channel + ((channel > 7) ? (-8) : 0)]._Band[j]);
				}
				setParam("LOGIC", channel + ((channel <= 7) ? 8 : 0), Pr.GEQOn, 0, GEQData[channel + ((channel > 7) ? (-8) : 0)]._on);
				break;
			}
			case 2:
			{
				for (int i = 0; i < 31; i++)
				{
					setParam("LOGIC", channel + ((channel > 7) ? (-8) : 0), Pr.GEQBand, i, GEQData[channel + ((channel <= 7) ? 8 : 0)]._Band[i]);
				}
				setParam("LOGIC", channel + ((channel > 7) ? (-8) : 0), Pr.GEQOn, 0, GEQData[channel + ((channel <= 7) ? 8 : 0)]._on);
				break;
			}
			}
			break;
		case Pr.GEQType:
			gEQParam = GEQParam.GEQ_TYPE;
			GEQData[channel]._type = value;
			break;
		case Pr.RackType:
			gEQParam = GEQParam.GEQ_SOURCE;
			RackData[channel]._type = value;
			switch (value)
			{
			case 1:
				setParam("LOGIC", channel + ((channel > 7) ? (-8) : 0), Pr.GEQType, 0, 0);
				break;
			case 2:
				setParam("LOGIC", channel + ((channel > 7) ? (-8) : 0), Pr.GEQType, 0, 1);
				setParam("LOGIC", channel + ((channel <= 7) ? 8 : 0), Pr.GEQType, 0, 1);
				break;
			}
			break;
		case Pr.RackSource:
			gEQParam = GEQParam.GEQ_SOURCE;
			RackData[channel]._source = value;
			break;
		case Pr.HA:
			if (channel >= 0 && channel < HA.Length)
			{
				HA[channel] = value;
			}
			break;
		case Pr.Phantom:
			if (channel >= 0 && channel < Phantom.Length)
			{
				Phantom[channel] = value;
			}
			break;
		case Pr.SceneLoad:
			Console.sceneNum = channel;
			Console.sceneName = sceneData[channel].sceneName;
			if (source == "CONSOLE")
			{
				enQueue(Pr.SceneDump, 0, 0);
			}
			break;
		case Pr.SceneSave:
			sceneData[channel].isValid = true;
			if (channel > Console.sceneHigh)
			{
				Console.sceneHigh = channel;
			}
			Console.sceneNum = channel;
			Console.sceneName = sceneData[channel].sceneName;
			if (source == "CONSOLE")
			{
				reqParam("SceneName", channel);
			}
			break;
		default:
			paramValue[param] = value;
			break;
		case Pr.SceneName:
		case Pr.SceneDump:
		case Pr.SceneValid:
			break;
		}
		if (connPhase == ConnectionPhase.Syncing)
		{
			return;
		}
		if (source != "CONSOLE" && source != "LOGIC" && connStatus != 0)
		{
			enQueue(param, channel, index);
		}
		PushEngine(param, channel, index, value);
		switch (gEQParam)
		{
		case GEQParam.GEQ_SOURCE:
		{
			RackSync rackSync = new RackSync();
			rackSync.RackSlot = channel;
			rackSync.RackSource = RackData[channel]._source;
			rackSync.RackType = RackData[channel]._type;
			faderServer.sendProto(source, rackSync);
			break;
		}
		default:
		{
			GEQSetParam gEQSetParam = new GEQSetParam();
			gEQSetParam.rack = channel;
			gEQSetParam.index = index;
			gEQSetParam.param = gEQParam;
			gEQSetParam.value.Add(value);
			faderServer.sendProto(source, gEQSetParam);
			break;
		}
		case GEQParam.GEQ_NONE:
			if (genParam != 0)
			{
				GeneralParam generalParam = new GeneralParam();
				generalParam.index = channel;
				generalParam.param = genParam;
				generalParam.value.Add(value);
				faderServer.sendProto(source, generalParam);
			}
			break;
		}
	}

	public static void setParam(string source, int channel, Pr param, int index, string value)
	{
		source = source.ToUpper();
		switch (param)
		{
		case Pr.SceneName:
			sceneData[channel].sceneName = value;
			if (channel == Console.sceneNum)
			{
				Console.sceneName = value;
			}
			break;
		case Pr.SceneSave:
			sceneData[channel].sceneName = value;
			Console.sceneNum = channel;
			Console.sceneName = value;
			break;
		}
		if (source != "CONSOLE" && source != "LOGIC" && connStatus != 0)
		{
			enQueue(param, channel, index);
		}
		PushEngine(param, channel, index, 0);
	}

	public static bool sendParam(Channel.Pr param, int channel, int mix, string value)
	{
		List<string> list = new List<string>();
		if (param == Channel.Pr.Name)
		{
			value += "\0\0\0\0\0\0\0\0";
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120101140000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E120101140001cccc" + StuffString(value.Substring(4, 2)) + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110101130000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E110101130001cccc" + StuffString(value.Substring(4, 4)) + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value = value.Replace("\0", string.Empty).PadRight(4, ' ');
					if (chData[channel].channelType == ChannelTypes.Input)
					{
						list.Add("F0431n3E0D020400cc000000" + $"{Convert.ToInt32(value.ToCharArray(0, 1)[0]):X}" + "F7");
						list.Add("F0431n3E0D020401cc000000" + $"{Convert.ToInt32(value.ToCharArray(1, 1)[0]):X}" + "F7");
						list.Add("F0431n3E0D020402cc000000" + $"{Convert.ToInt32(value.ToCharArray(2, 1)[0]):X}" + "F7");
						list.Add("F0431n3E0D020403cc000000" + $"{Convert.ToInt32(value.ToCharArray(3, 1)[0]):X}" + "F7");
					}
					else
					{
						channel = (channel - chOffsetSTIN) / 2;
						list.Add("F0431n3E0D021700cc000000" + $"{Convert.ToInt32(value.ToCharArray(0, 1)[0]):X}" + "F7");
						list.Add("F0431n3E0D021701cc000000" + $"{Convert.ToInt32(value.ToCharArray(1, 1)[0]):X}" + "F7");
						list.Add("F0431n3E0D021702cc000000" + $"{Convert.ToInt32(value.ToCharArray(2, 1)[0]):X}" + "F7");
						list.Add("F0431n3E0D021703cc000000" + $"{Convert.ToInt32(value.ToCharArray(3, 1)[0]):X}" + "F7");
					}
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201011B0000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E1201011B0001cccc" + StuffString(value.Substring(4, 2)) + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110101190000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E110101190001cccc" + StuffString(value.Substring(4, 4)) + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value = value.Replace("\0", string.Empty).PadRight(4, ' ');
					list.Add("F0431n3E0D021000cc000000" + $"{Convert.ToInt32(value.ToCharArray(0, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D021001cc000000" + $"{Convert.ToInt32(value.ToCharArray(1, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D021002cc000000" + $"{Convert.ToInt32(value.ToCharArray(2, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D021003cc000000" + $"{Convert.ToInt32(value.ToCharArray(3, 1)[0]):X}" + "F7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201011C0000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E1201011C0001cccc" + StuffString(value.Substring(4, 2)) + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101011A0000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E1101011A0001cccc" + StuffString(value.Substring(4, 4)) + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value = value.Replace("\0", string.Empty).PadRight(4, ' ');
					list.Add("F0431n3E0D020F00cc000000" + $"{Convert.ToInt32(value.ToCharArray(0, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D020F01cc000000" + $"{Convert.ToInt32(value.ToCharArray(1, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D020F02cc000000" + $"{Convert.ToInt32(value.ToCharArray(2, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D020F03cc000000" + $"{Convert.ToInt32(value.ToCharArray(3, 1)[0]):X}" + "F7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201011D0000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E1201011D0001cccc" + StuffString(value.Substring(4, 2)) + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101011B0000cccc" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E1101011B0001cccc" + StuffString(value.Substring(4, 4)) + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value = value.Replace("\0", string.Empty).PadRight(4, ' ');
					channel = (channel - chOffsetStereo) / 2;
					list.Add("F0431n3E0D021200cc000000" + $"{Convert.ToInt32(value.ToCharArray(0, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D021201cc000000" + $"{Convert.ToInt32(value.ToCharArray(1, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D021202cc000000" + $"{Convert.ToInt32(value.ToCharArray(2, 1)[0]):X}" + "F7");
					list.Add("F0431n3E0D021203cc000000" + $"{Convert.ToInt32(value.ToCharArray(3, 1)[0]):X}" + "F7");
				}
				break;
			}
		}
		else
		{
			logDebug("Couldn't find MIDI command for: " + chData[channel].paramName[param]);
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("\t", "");
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cc", Right(StuffValueStr(chData[channel].subChannel), 2));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			sendData(text);
		}
		return true;
	}

	public static bool sendParam(Channel.Pr param, int channel, int mix, int value)
	{
		List<string> list = new List<string>();
		switch (param)
		{
		case Channel.Pr.InsertOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201002E0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101002D0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F012301ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011900ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201004B0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004A0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F013C01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013500ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005C0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005B0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F014F01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012800ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201006A0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100690000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F015D01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F014C00ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.DirectOutOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201002F0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101002E0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F012401ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("");
				}
				break;
			}
			break;
		case Channel.Pr.Phase:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120100300000ccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101002F0000ccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0431n3E0F012501ccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011700ccddddddddF7");
			}
			break;
		case Channel.Pr.On:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100310000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100300000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F012601ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011A00ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201004C0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004B0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F013D01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013600ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005D0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005C0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F015001ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012900ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201006B0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101006A0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F015E01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F014D00ccddddddddF7");
				}
				break;
			case ChannelTypes.Monitor:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201021C0008ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110102170008ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.MuteGroup:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003F00" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003E00" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F0126" + (mix + 8).ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005900" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005800" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F0140" + (mix + 4).ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201006600" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101006500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F0133" + (mix + 4).ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007600" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F0156" + (mix + 4).ToString("X2") + "ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.DCAGroup:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003F00" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F0122" + (mix + 3).ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005900" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101006600" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007600" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Pan:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100320001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100310001ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F012702ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011B00ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100570002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100560002ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F013E01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013800ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F015101ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013202ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					if (chData[channel].subChannel == 0)
					{
						value = -value;
					}
					list.Add("F0431n3E1201006C0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					if (chData[channel].subChannel == 0)
					{
						value = -value;
					}
					list.Add("F0431n3E1101006B0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					if (chData[channel].subChannel == 0)
					{
						value = -value;
					}
					list.Add("F0431n3E0F015F01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F014E00ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Balance:
			if (chData[channel].subChannel % 2 == 0)
			{
				value = -value;
			}
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201004D0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004C0000ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005E0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005D0000ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201006C0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101006B0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F015F01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F014E00ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.ToStereoOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100410000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100400000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012200ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100570000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100560000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013200ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.ToMonoOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100420000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100410000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100580000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100570000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("");
				}
				break;
			}
			break;
		case Channel.Pr.Fader:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100330000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100320000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F012801ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011C00ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201004E0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004D0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F013F01ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013900ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005F0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005E0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F015201ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012B00ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201006D0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101006C0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F016001ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F014F00ccddddddddF7");
				}
				break;
			case ChannelTypes.Monitor:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201021C000DccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110102170013ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQAtt:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				value = (int)(Math.Round((decimal)value / 10m) * 10m);
			}
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100340001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100330001ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("F0431n3E0F012902ccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011D00ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201004F0001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004E0001ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013A00ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100600001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005F0001ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012C00ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201006E0001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101006D0001ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015000ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Cue:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201015E0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101015A0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E0D032E00ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201015F0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101015B0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E0D032F00" + (chData[channel].subChannel + 8).ToString("X2") + "ddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120101600000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101015C0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E0D032F00" + chData[channel].subChannel.ToString("X2") + "ddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120101610000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101015D0000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn0On:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120100370000ccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100360000ccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0\t43\t1n\t3E\t0F\t01\t2B\t01\tcc\tdd\tdd\tdd\tdd\tdd\tF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E00ccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn0Type:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120100370001ccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100360001ccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E05ccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn0Attack:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120100370009ccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100360009ccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0\t43\t1n\t3E\t0F\t01\t2B\t09\tcc\tdd\tdd\tdd\tdd\tdd\tF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E06ccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn0Range:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010037000AccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11010036000AccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0\t43\t1n\t3E\t0F\t01\t2B\t0A\tcc\tdd\tdd\tdd\tdd\tdd\tF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E07ccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn0Hold:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010037000BccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11010036000BccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0\t43\t1n\t3E\t0F\t01\t2B\t0B\tcc\tdd\tdd\tdd\tdd\tdd\tF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E08ccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn0Decay:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010037000CccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11010036000CccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0\t43\t1n\t3E\t0F\t01\t2B\t0C\tcc\tdd\tdd\tdd\tdd\tdd\tF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E09ccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn0Ratio:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010037000DccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11010036000DccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("");
			}
			break;
		case Channel.Pr.Dyn0Gain:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010037000EccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11010036000EccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("");
			}
			break;
		case Channel.Pr.Dyn0Knee:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010037000FccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11010036000FccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("");
			}
			break;
		case Channel.Pr.Dyn0Threshold:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120100370010ccccddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100360010ccccddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("F0\t43\t1n\t3E\t0F\t01\t2B\t0D\tcc\tdd\tdd\tdd\tdd\tdd\tF7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F011E0AccddddddddF7");
			}
			break;
		case Channel.Pr.Dyn1On:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100390000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F01ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100520000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100510000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B01ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100630000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100620000ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D01ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100710000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100700000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015101ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Type:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A0001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100390001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value -= 2;
					list.Add("F0431n3E7F011F03ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100520001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100510001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value -= 2;
					list.Add("F0431n3E7F013B03ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100630001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100620001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value -= 2;
					list.Add("F0431n3E7F012D03ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100710001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100700001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					value -= 2;
					list.Add("F0431n3E7F015103ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Attack:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A0009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100390009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F04ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100520009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100510009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B04ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100630009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100620009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D04ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100710009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100700009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015104ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Range:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010039000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010052000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010051000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010063000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010062000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010071000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010070000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Hold:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010039000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010052000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010051000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010063000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010062000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010071000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010070000BccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Release:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010039000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F05ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010052000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010051000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B05ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010063000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010062000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D05ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010071000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010070000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015105ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Ratio:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010039000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F06ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010052000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010051000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B06ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010063000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010062000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D06ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010071000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010070000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015106ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Gain:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010039000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F07ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010052000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010051000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B07ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010063000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010062000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D07ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010071000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010070000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015107ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Knee:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010039000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F08ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010052000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010051000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B08ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010063000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010062000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D08ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010071000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010070000FccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015108ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Threshold:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003A0010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100390010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F011F09ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100520010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100510010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013B09ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100630010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100620010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012D09ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100710010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100700010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015109ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQHPFOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003C0000ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003B0000ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQHPFFreq:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003C0001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003B0001ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQType:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0001ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530001ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640001ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730001ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720001ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01200FccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C0FccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E0FccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720002ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01520FccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1Type:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 41;
						list.Add("F0431n3E7F012001ccddddddddF7");
						break;
					case 2:
						value = 44;
						list.Add("F0431n3E7F012001ccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ1Q);
						list.Add("F0431n3E7F012001ccddddddddF7");
						break;
					}
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 41;
						list.Add("F0431n3E7F013C01ccddddddddF7");
						break;
					case 2:
						value = 44;
						list.Add("F0431n3E7F013C01ccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ1Q);
						list.Add("F0431n3E7F013C01ccddddddddF7");
						break;
					}
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 41;
						list.Add("F0431n3E7F012E01ccddddddddF7");
						break;
					case 2:
						value = 44;
						list.Add("F0431n3E7F012E01ccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ1Q);
						list.Add("F0431n3E7F012E01ccddddddddF7");
						break;
					}
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720003ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 41;
						list.Add("F0431n3E7F015201ccddddddddF7");
						break;
					case 2:
						value = 44;
						list.Add("F0431n3E7F015201ccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ1Q);
						list.Add("F0431n3E7F015201ccddddddddF7");
						break;
					}
				}
				break;
			}
			break;
		case Channel.Pr.EQ4Type:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 42;
						list.Add("F0431n3E7F01200BccddddddddF7");
						break;
					case 2:
						value = 43;
						list.Add("F0431n3E7F01200BccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ4Q);
						list.Add("F0431n3E7F01200BccddddddddF7");
						break;
					}
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 42;
						list.Add("F0431n3E7F013C0BccddddddddF7");
						break;
					case 2:
						value = 43;
						list.Add("F0431n3E7F013C0BccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ4Q);
						list.Add("F0431n3E7F013C0BccddddddddF7");
						break;
					}
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 42;
						list.Add("F0431n3E7F012E0BccddddddddF7");
						break;
					case 2:
						value = 43;
						list.Add("F0431n3E7F012E0BccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ4Q);
						list.Add("F0431n3E7F012E0BccddddddddF7");
						break;
					}
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720004ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 1:
						value = 42;
						list.Add("F0431n3E7F01520BccddddddddF7");
						break;
					case 2:
						value = 43;
						list.Add("F0431n3E7F01520BccddddddddF7");
						break;
					default:
						value = chData[channel].getParam(Channel.Pr.EQ4Q);
						list.Add("F0431n3E7F01520BccddddddddF7");
						break;
					}
				}
				break;
			}
			break;
		case Channel.Pr.EQ1HPFOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012004ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C04ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E04ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720005ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015204ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4LPFOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01200EccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C0EccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E0EccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720006ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01520EccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1Q:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012001ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C01ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E01ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720008ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015201ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1F:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012002ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C02ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E02ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720009ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015202ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1G:
			value = (int)(Math.Round((decimal)value / 5.0m) * 5m);
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012003ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010054000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010053000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C03ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010065000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010064000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E03ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010073000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010072000AccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015203ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ2Q:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012005ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010054000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010053000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C05ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010065000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010064000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E05ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010073000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010072000CccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015205ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ2F:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012006ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010054000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010053000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C06ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010065000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010064000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E06ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010073000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010072000DccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015206ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ2G:
			value = (int)(Math.Round((decimal)value / 5.0m) * 5m);
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012007ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010054000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010053000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C07ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010065000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010064000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E07ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E12010073000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E11010072000EccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015207ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ3Q:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012008ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C08ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E08ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720010ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015208ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ3F:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012009ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C09ccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E09ccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720011ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F015209ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ3G:
			value = (int)(Math.Round((decimal)value / 5.0m) * 5m);
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01200AccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C0AccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E0AccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720012ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01520AccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4Q:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01200BccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C0BccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E0BccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720014ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01520BccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4F:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01200CccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C0CccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E0CccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720015ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01520CccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4G:
			value = (int)(Math.Round((decimal)value / 5.0m) * 5m);
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201003D0016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101003C0016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01200DccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100540016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100530016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F013C0DccddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100650016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100640016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F012E0DccddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120100730016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110100720016ccccddddddddddF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F01520DccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMixOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					mix = mix * 3 + 3;
					list.Add("F0431n3E1201004300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					mix = mix * 3 + 3;
					list.Add("F0431n3E1101004200" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					mix *= 3;
					list.Add("F0431n3E7F0123" + mix.ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					mix = mix * 3 + 3;
					list.Add("F0431n3E1201005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					mix = mix * 3 + 3;
					list.Add("F0431n3E1101005400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type != ConsoleTypes.O1V96V2)
				{
				}
				break;
			case ChannelTypes.Matrix:
				if (channel >= 0 && channel < chData.Length)
				{
					if (Console.type == ConsoleTypes.LS9)
					{
						channel = chData[channel].subChannel * 3 + 3;
						list.Add("F0431n3E1201005500" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					else if (Console.type == ConsoleTypes.M7CL)
					{
						channel = chData[channel].subChannel * 3 + 3;
						list.Add("F0431n3E1101005400" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					else if (Console.type != ConsoleTypes.O1V96V2)
					{
					}
				}
				break;
			case ChannelTypes.Stereo:
				mix = mix * 3 + 3;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMatrixOn:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				mix = mix * 3 + 3;
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				mix = mix * 3 + 3;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (channel >= 0 && channel < chData.Length)
				{
					channel = chData[channel].subChannel * 3 + 3;
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0431n3E1201005500" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					else if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0431n3E1101005400" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
				}
				break;
			case ChannelTypes.Stereo:
				mix = mix * 3 + 3;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMixPre:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					mix = mix * 3 + 4;
					list.Add("F0431n3E1201004300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					mix = mix * 3 + 4;
					list.Add("F0431n3E1101004200" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					mix = mix * 3 + 1;
					list.Add("F0431n3E7F0123" + mix.ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				mix = mix * 3 + 4;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (channel >= 0 && channel < chData.Length)
				{
					channel = chData[channel].subChannel * 3 + 4;
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0431n3E1201005500" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					else if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0431n3E1101005400" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
				}
				break;
			case ChannelTypes.Stereo:
				mix = mix * 3 + 4;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMatrixPre:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				mix = mix * 3 + 4;
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				mix = mix * 3 + 4;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (channel >= 0 && channel < chData.Length)
				{
					channel = chData[channel].subChannel * 3 + 4;
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0431n3E1201005500" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					else if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0431n3E1101005400" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
				}
				break;
			case ChannelTypes.Stereo:
				mix = mix * 3 + 4;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMix:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					mix = mix * 3 + 5;
					list.Add("F0431n3E1201004300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					mix = mix * 3 + 5;
					list.Add("F0431n3E1101004200" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					mix = mix * 3 + 2;
					list.Add("F0431n3E7F0123" + mix.ToString("X2") + "ccddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				mix = mix * 3 + 5;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (channel >= 0 && channel < chData.Length)
				{
					channel = chData[channel].subChannel * 3 + 5;
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0431n3E1201005500" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					else if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0431n3E1101005400" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
				}
				break;
			case ChannelTypes.Stereo:
				mix = mix * 3 + 5;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMatrix:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				mix = mix * 3 + 5;
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				mix = mix * 3 + 5;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (channel >= 0 && channel < chData.Length)
				{
					channel = chData[channel].subChannel * 3 + 5;
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0431n3E1201005500" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0431n3E1101005400" + channel.ToString("X2") + mix.ToString("X4") + "ddddddddddF7");
					}
				}
				break;
			case ChannelTypes.Stereo:
				mix = mix * 3 + 5;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMixPan:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				mix /= 2;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201004400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004300" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0431n3E7F0124" + mix.ToString("X2") + "ccddddddddF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMatrixPan:
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				mix /= 2;
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101004500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Mix:
				mix /= 2;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201005600" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101005500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			case ChannelTypes.Stereo:
				mix /= 2;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007500" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007400" + mix.ToString("X2") + "ccccddddddddddF7");
				}
				break;
			}
			break;
		default:
			logDebug("Couldn't find MIDI command for: " + chData[channel].paramName[param]);
			break;
		case Channel.Pr.HA:
		case Channel.Pr.Phantom:
			break;
		}
		if (list.Count == 0)
		{
			logDebug("Empty MIDI command for: " + chData[channel].paramName[param]);
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("\t", "");
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cc", Right(StuffValueStr(chData[channel].subChannel), 2));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			text = text.Replace("dddddddddd", StuffValueStr(value));
			text = text.Replace("dddddddd", Right(StuffValueStr(value), 8));
			sendData(text);
		}
		return true;
	}

	public static bool sendParam(EFX.Pr param, int rack, int paramIndex, int value)
	{
		List<string> list = new List<string>();
		if (param == EFX.Pr.Param && Console.type == ConsoleTypes.LS9)
		{
			if (paramIndex >= 0 && paramIndex < EFXData[rack].pParamIndex.Length)
			{
				paramIndex = EFXData[rack].pParamIndex[paramIndex];
			}
			list.Add("F0431n3E12010100iiiizzzzddddddddddF7");
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("iiii", Right(StuffValueStr(paramIndex), 4));
			text = text.Replace("zzzz", Right(StuffValueStr(rack), 4));
			text = text.Replace("dddddddddd", StuffValueStr(value));
			sendData(text);
		}
		return true;
	}

	public static bool sendParam(Pr param, int group, int paramIndex, int value)
	{
		List<string> list = new List<string>();
		switch (param)
		{
		case Pr.MuteMaster:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120100210000ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100200000ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F015D00ggddddddddF7");
			}
			break;
		case Pr.GEQBand:
			if (group > 7)
			{
				group -= 8;
				paramIndex += 31;
			}
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201007F00" + (paramIndex + 9).ToString("X2") + "ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101007E00" + (paramIndex + 9).ToString("X2") + "ggggddddddddddF7");
			}
			break;
		case Pr.GEQFlat:
			if (group > 7)
			{
				group -= 8;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007F0006ggggddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007E0006ggggddddddddddF7");
				}
			}
			else if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201007F0005ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101007E0005ggggddddddddddF7");
			}
			break;
		case Pr.GEQLink:
			if (group > 7)
			{
				group -= 8;
			}
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201007F0001ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101007E0001ggggddddddddddF7");
			}
			break;
		case Pr.GEQLinkLR:
			if (group > 7)
			{
				group -= 8;
			}
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201007F0002ggggddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101007E0002ggggddddddddddF7");
			}
			break;
		case Pr.GEQOn:
			if (group < 8)
			{
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201007F0003ggggddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101007E0003ggggddddddddddF7");
				}
				break;
			}
			group -= 8;
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201007F0004ggggddddddddddF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101007E0004ggggddddddddddF7");
			}
			break;
		case Pr.Phantom:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201002A0000ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100290000ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("");
			}
			break;
		case Pr.HA:
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201002A0001ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100290001ggggddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("");
			}
			break;
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("gggg", Right(StuffValueStr(group), 4));
			text = text.Replace("gg", Right(StuffValueStr(group), 2));
			text = text.Replace("iiii", Right(StuffValueStr(paramIndex), 4));
			text = text.Replace("dddddddddd", StuffValueStr(value));
			text = text.Replace("dddddddd", Right(StuffValueStr(value), 8));
			sendData(text);
		}
		return true;
	}

	public static bool setParam(string param, int channel, string value)
	{
		List<string> list = new List<string>();
		switch (param)
		{
		case "SceneName":
			value += "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00000000" + StuffString(value.Substring(0, 4)) + "F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00010000" + StuffString(value.Substring(4, 4)) + "F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00020000" + StuffString(value.Substring(8, 4)) + "F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00030000" + StuffString(value.Substring(12, 4)) + "F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				if (Console.ver == ConsoleVersions.M7CLv35)
				{
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400000000" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400010000" + StuffString(value.Substring(4, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400020000" + StuffString(value.Substring(8, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400030000" + StuffString(value.Substring(12, 4)) + "F7");
				}
				else
				{
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300000000" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300010000" + StuffString(value.Substring(4, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300020000" + StuffString(value.Substring(8, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300030000" + StuffString(value.Substring(12, 4)) + "F7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F043103E7F1040zzzz" + stringToHex(value.PadRight(16, ' ').Substring(0, 16)) + "F7");
			}
			break;
		case "SaveScene":
			if (Console.type == ConsoleTypes.LS9)
			{
				value += "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
				list.Add("F0431n3E12004C69625374725F5F5343454E455F5F5Fzzzz0400F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00000000" + StuffString(value.Substring(0, 4)) + "F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00010000" + StuffString(value.Substring(4, 4)) + "F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00020000" + StuffString(value.Substring(8, 4)) + "F7");
				list.Add("F0431n3E12004C696241747262745343454E455F5F5Fzzzz024B00030000" + StuffString(value.Substring(12, 4)) + "F7");
				sceneData[channel].isValid = true;
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				if (Console.ver == ConsoleVersions.M7CLv35)
				{
					value += "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
					list.Add("F0431n3E11004C69625374725F5F5343454E455F5F5Fzzzz0400F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400000000" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400010000" + StuffString(value.Substring(4, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400020000" + StuffString(value.Substring(8, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024400030000" + StuffString(value.Substring(12, 4)) + "F7");
				}
				else
				{
					value += "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
					list.Add("F0431n3E11004C69625374725F5F5343454E455F5F5Fzzzz0400F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300000000" + StuffString(value.Substring(0, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300010000" + StuffString(value.Substring(4, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300020000" + StuffString(value.Substring(8, 4)) + "F7");
					list.Add("F0431n3E11004C696241747262745343454E455F5F5Fzzzz024300030000" + StuffString(value.Substring(12, 4)) + "F7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F1020zzzz0200F7");
				list.Add("F043103E7F1040zzzz" + stringToHex(value.PadRight(16, ' ').Substring(0, 16)) + "F7");
			}
			break;
		default:
			Loc.Msg("Unknown command: " + param, "Engine Error");
			break;
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			if (channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			sendData(text);
		}
		return true;
	}

	public static bool setParam(string param, int channel, bool value)
	{
		return setParam(param, channel, value ? 1 : 0);
	}

	public static bool setParam(string param, int channel, decimal value)
	{
		param = param.ToUpper();
		switch (param)
		{
		case "FADER":
		case "BUSFADER":
		case "DCAFADER":
		case "STFADER":
			return setParam(param, channel, FuzzyFind(dbMap, value));
		default:
			return setParam(param, channel, (int)Math.Round(value));
		}
	}

	public static bool setParam(string param, int channel, int value)
	{
		List<string> list = new List<string>();
		switch (param.ToUpper())
		{
		case "DCAFADER":
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100790000zzzzddddddddddF7");
			}
			break;
		case "DCAMUTE":
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100770000zzzzddddddddddF7");
			}
			break;
		case "MUTEDCA":
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110100770000zzzzddddddddddF7");
			}
			break;
		case "LAMP":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120102270002zzzzddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110102220002zzzzddddddddddF7");
			}
			break;
		case "UDF":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120102390035zzzzddddddddddF7");
			}
			break;
		case "OSCFREQ":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201021B0004zzzzddddddddddF7");
			}
			break;
		case "SELCHANNEL":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201023900100000000000zzzzF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				if (Console.ver == ConsoleVersions.M7CLv35)
				{
					list.Add("F0431n3E1101023200120000000000zzzzF7");
				}
				else
				{
					list.Add("F0431n3E1101023100120000000000zzzzF7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E0D040918000000zzzzF7");
			}
			break;
		case "RECALLSCENE":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12004C696252636C5F5F5343454E455F5F5Fzzzz0400F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E11004C696252636C5F5F5343454E455F5F5Fzzzz0400F7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0431n3E7F1000zzzz0200F7");
			}
			break;
		case "OUTPORTDELAYON":
			if (OutPort[channel].type == OutportTypes.Omni)
			{
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201021F0000zzzzddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101021A0000zzzzddddddddddF7");
				}
			}
			else if (OutPort[channel].type == OutportTypes.Slot)
			{
				channel = OutPort[channel].subindex;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201021E0000zzzzddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110102190000zzzzddddddddddF7");
				}
			}
			else if (OutPort[channel].type == OutportTypes.TwoTR)
			{
				channel = OutPort[channel].subindex;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120102200000zzzzddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101021B0000zzzzddddddddddF7");
				}
			}
			break;
		case "OUTPORTDELAYTIME":
			if (OutPort[channel].type == OutportTypes.Omni)
			{
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201021F0002zzzzddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101021A0002zzzzddddddddddF7");
				}
			}
			else if (OutPort[channel].type == OutportTypes.Slot)
			{
				channel = OutPort[channel].subindex;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E1201021E0002zzzzddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110102190002zzzzddddddddddF7");
				}
			}
			else if (OutPort[channel].type == OutportTypes.TwoTR)
			{
				channel = OutPort[channel].subindex;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120102200002zzzzddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101021B0002zzzzddddddddddF7");
				}
			}
			break;
		case "OUTPORTPHASE":
			if (OutPort[channel].type == OutportTypes.Omni)
			{
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120102240000zzzzddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101021F0000zzzzddddddddddF7");
				}
			}
			else if (OutPort[channel].type == OutportTypes.Slot)
			{
				channel = OutPort[channel].subindex;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120102230000zzzzddddddddddF7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E1101021E0000zzzzddddddddddF7");
				}
			}
			else if (OutPort[channel].type == OutportTypes.TwoTR)
			{
				channel = OutPort[channel].subindex;
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0431n3E120102250000zzzzddddddddddF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0431n3E110102200000zzzzddddddddddF7");
				}
			}
			break;
		case "OUTPORTATT":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120102080000zzzzddddddddddF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110102030000zzzzddddddddddF7");
			}
			break;
		case "EFX.BPM":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120101000004zzzzddddddddddF7");
			}
			break;
		case "EFX.BYPASS":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120101000006zzzzddddddddddF7");
			}
			break;
		case "EFX.MIX":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120101000009zzzzddddddddddF7");
			}
			break;
		case "EFX.TYPE":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E12010100000AzzzzddddddddddF7");
			}
			break;
		default:
			Loc.Msg("Unknown command: " + param, "Engine Error");
			break;
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			text = text.Replace("dddddddddd", StuffValueStr(value));
			sendData(text);
		}
		return true;
	}

	public static bool reqParam(Pr param, int index, int channel)
	{
		List<string> list = new List<string>();
		int value = 0;
		switch (param)
		{
		case Pr.SceneName:
			reqParam("SceneName", channel, index);
			break;
		case Pr.SceneNum:
			reqParam("SceneNum", channel, index);
			break;
		default:
			reqQueue.RemoveAll((reqQueueItem item) => item.type == reqQueueItem.msgType.Engine && item.engineParam == param);
			logDebug("reqParam called for unknown MixEngine.Pr " + param);
			break;
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("mmmm", Right(StuffValueStr(value), 4));
			text = text.Replace("mm", Right(StuffValueStr(value), 2));
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cc", Right(StuffValueStr(chData[channel].subChannel), 2));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			sendData(text);
		}
		return true;
	}

	public static bool reqParam(Channel.Pr param, int index, int channel)
	{
		List<string> list = new List<string>();
		int value = 0;
		switch (param)
		{
		case Channel.Pr.Fader:
			reqParam("Fader", channel, index);
			break;
		case Channel.Pr.On:
			reqParam("InputOn", channel, index);
			break;
		case Channel.Pr.Pan:
			reqParam("InputPan", channel, index);
			break;
		case Channel.Pr.PatchIn:
			reqParam("InputPatch", channel, index);
			break;
		case Channel.Pr.Pair:
			reqParam("InputPair", channel, index);
			break;
		case Channel.Pr.Phase:
			reqParam("InputPhase", channel, index);
			break;
		case Channel.Pr.ToStereoOn:
			reqParam("toStereoOn", channel, index);
			break;
		case Channel.Pr.Name:
			reqParam("InputName", channel, index);
			break;
		case Channel.Pr.Cue:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D032E007FF7");
				list.Add("F0433n3E0D032F007FF7");
			}
			break;
		case Channel.Pr.EQType:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120007FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E007FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C007FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152007FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012000ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E00ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C00ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015200ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQAtt:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011D007FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012C007FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013A007FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0150007FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011D00ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012C00ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013A00ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015000ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1Q:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120017FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E017FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C017FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152017FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012001ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E01ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C01ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015201ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1F:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120027FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E027FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C027FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152027FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012002ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E02ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C02ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015202ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1G:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120037FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E037FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C037FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152037FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012003ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E03ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C03ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015203ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ1HPFOn:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120047FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E047FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C047FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152047FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012004ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E04ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C04ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015204ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ2Q:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120057FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E057FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C057FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152057FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012005ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E05ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C05ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015205ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ2F:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120067FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E067FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C067FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152067FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012006ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E06ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C06ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015206ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ2G:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120077FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E077FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C077FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152077FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012007ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E07ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C07ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015207ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ3Q:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120087FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E087FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C087FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152087FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012008ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E08ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C08ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015208ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ3F:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F0120097FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E097FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C097FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0152097FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F012009ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E09ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C09ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015209ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ3G:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F01200A7FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E0A7FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C0A7FF7");
				break;
			case 512:
				list.Add("F0433n3E7F01520A7FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F01200AccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E0AccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C0AccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F01520AccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4Q:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F01200B7FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E0B7FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C0B7FF7");
				break;
			case 512:
				list.Add("F0433n3E7F01520B7FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F01200BccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E0BccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C0BccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F01520BccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4F:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F01200C7FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E0C7FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C0C7FF7");
				break;
			case 512:
				list.Add("F0433n3E7F01520C7FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F01200CccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E0CccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C0CccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F01520CccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4G:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F01200D7FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E0D7FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C0D7FF7");
				break;
			case 512:
				list.Add("F0433n3E7F01520D7FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F01200DccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E0DccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C0DccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F01520DccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQ4LPFOn:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F01200E7FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E0E7FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C0E7FF7");
				break;
			case 512:
				list.Add("F0433n3E7F01520E7FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F01200EccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E0EccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C0EccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F01520EccF7");
				}
				break;
			}
			break;
		case Channel.Pr.EQOn:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F01200F7FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012E0F7FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013C0F7FF7");
				break;
			case 512:
				list.Add("F0433n3E7F01520F7FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F01200FccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012E0FccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013C0FccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F01520FccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn0On:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E007FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E00ccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn0Type:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E057FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E05ccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn0Attack:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E067FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E06ccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn0Range:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E077FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E07ccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn0Hold:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E087FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E08ccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn0Decay:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E097FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E09ccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn0Threshold:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				switch (channel)
				{
				case 127:
					list.Add("F0433n3E7F011E0A7FF7");
					break;
				case 255:
					list.Add("");
					break;
				case 511:
					list.Add("");
					break;
				case 512:
					list.Add("");
					break;
				default:
					list.Add("F0433n3E7F011E0AccF7");
					break;
				}
			}
			break;
		case Channel.Pr.Dyn1On:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F017FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D017FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B017FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151017FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F01ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D01ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B01ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015101ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Type:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F037FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D037FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B037FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151037FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F03ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D03ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B03ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015103ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Attack:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F047FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D047FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B047FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151047FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F04ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D04ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B04ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015104ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Release:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F057FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D057FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B057FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151057FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F05ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D05ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B05ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015105ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Ratio:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F067FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D067FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B067FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151067FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F06ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D06ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B06ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015106ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Gain:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F077FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D077FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B077FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151077FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F07ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D07ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B07ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015107ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Knee:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F087FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D087FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B087FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151087FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F08ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D08ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B08ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015108ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.Dyn1Threshold:
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E7F011F097FF7");
				break;
			case 255:
				list.Add("F0433n3E7F012D097FF7");
				break;
			case 511:
				list.Add("F0433n3E7F013B097FF7");
				break;
			case 512:
				list.Add("F0433n3E7F0151097FF7");
				break;
			default:
				if (chData[channel].channelType == ChannelTypes.Input)
				{
					list.Add("F0433n3E7F011F09ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Matrix)
				{
					list.Add("F0433n3E7F012D09ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Mix)
				{
					list.Add("F0433n3E7F013B09ccF7");
				}
				else if (chData[channel].channelType == ChannelTypes.Stereo)
				{
					list.Add("F0433n3E7F015109ccF7");
				}
				break;
			}
			break;
		case Channel.Pr.SendToMixOn:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				value = index * 3;
				if (channel == 127)
				{
					list.Add("F0433n3E7F0123mm7FF7");
				}
				else
				{
					list.Add("F0433n3E7F0123mmccF7");
				}
			}
			break;
		case Channel.Pr.SendToMixPre:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				value = index * 3 + 1;
				if (channel == 127)
				{
					list.Add("F0433n3E7F0123mm7FF7");
				}
				else
				{
					list.Add("F0433n3E7F0123mmccF7");
				}
			}
			break;
		case Channel.Pr.SendToMixPan:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				value = index / 2;
				if (channel == 127)
				{
					list.Add("F0433n3E7F0124mm7FF7");
				}
				else
				{
					list.Add("F0433n3E7F0124mmccF7");
				}
			}
			break;
		case Channel.Pr.SendToMix:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				value = index * 3 + 2;
				if (channel == 127)
				{
					list.Add("F0433n3E7F0123mm7FF7");
				}
				else
				{
					list.Add("F0433n3E7F0123mmccF7");
				}
			}
			break;
		default:
			reqQueue.RemoveAll((reqQueueItem item) => item.channelParam == param);
			logDebug("reqParam called for unknown Channel.Pr " + param);
			break;
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("mmmm", Right(StuffValueStr(value), 4));
			text = text.Replace("mm", Right(StuffValueStr(value), 2));
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cc", Right(StuffValueStr(chData[channel].subChannel), 2));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			sendData(text);
		}
		return true;
	}

	public static bool reqParam(string param, int channel)
	{
		return reqParam(param, channel, 0);
	}

	public static bool reqParam(string param, int channel, int mix)
	{
		List<string> list = new List<string>();
		if (channel < 0)
		{
			return false;
		}
		switch (param)
		{
		case "SceneName":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12004C696241747262745343454E455F5F5Fzzzz024B00000000F7");
				list.Add("F0433n3E12004C696241747262745343454E455F5F5Fzzzz024B00010000F7");
				list.Add("F0433n3E12004C696241747262745343454E455F5F5Fzzzz024B00020000F7");
				list.Add("F0433n3E12004C696241747262745343454E455F5F5Fzzzz024B00030000F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				if (Console.ver == ConsoleVersions.M7CLv35)
				{
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024400000000F7");
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024400010000F7");
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024400020000F7");
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024400030000F7");
				}
				else
				{
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024300000000F7");
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024300010000F7");
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024300020000F7");
					list.Add("F0433n3E11004C696241747262745343454E455F5F5Fzzzz024300030000F7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E7F1040zzzzF7");
			}
			break;
		case "SceneNum":
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D040A0100F7");
			}
			break;
		case "SelChannel":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201023900100000F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				if (Console.ver == ConsoleVersions.M7CLv35)
				{
					list.Add("F0433n3E1101023200120000F7");
				}
				else
				{
					list.Add("F0433n3E1101023100120000F7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D04091800F7");
			}
			break;
		case "ClockSpeed":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201026100000000F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				if (Console.ver == ConsoleVersions.M7CLv35)
				{
					list.Add("F0433n3E1101025900000000F7");
				}
				else
				{
					list.Add("F0433n3E1101025800000000F7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D04240000F7");
			}
			break;
		case "CueMode":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201015D00020000F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E1101015900020000F7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D032D0100F7");
			}
			break;
		case "PrefNaviToSelIn":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E1201012400000029F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E1101012200000029F7");
			}
			break;
		case "PrefNaviToSelOut":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120101240000002AF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110101220000002AF7");
			}
			break;
		case "PrefCueSelLink":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0431n3E120101240000002EF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0431n3E110101220000002EF7");
			}
			break;
		case "Lamp":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120102270002zzzzF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110102220002zzzzF7");
			}
			break;
		case "MonitorLevel":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201021C000DzzzzF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110102170013zzzzF7");
			}
			break;
		case "MonitorOn":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201021C0008zzzzF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110102170008zzzzF7");
			}
			break;
		case "MuteMaster":
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E7F015DzzzzF7");
			}
			break;
		case "MuteGroupMembers":
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				channel += 8;
				list.Add("F0433n3E7F0126zz7FF7");
			}
			break;
		case "DCAGroupMembers":
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				channel += 3;
				list.Add("F0433n3E7F0122zz7FF7");
			}
			break;
		case "StereoMonoLink":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201012400000030F7");
			}
			break;
		case "InputMeter":
		case "InputMeterRange":
			if (channel == 127)
			{
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122100" + mix.ToString("X2") + channel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112100" + mix.ToString("X2") + channel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2100" + mix.ToString("X2") + channel.ToString("X2") + "0001F7");
				}
				break;
			}
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122100" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112100" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2100" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122101" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112101" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2102" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122102" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112102" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2101" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122103" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112103" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2104" + mix.ToString("X2") + chData[channel].subChannel.ToString("X2") + "0001F7");
				}
				break;
			}
			break;
		case "StopMeter":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12217F00000001F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11217F00000001F7");
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D217F00000000F7");
			}
			break;
		case "InputPhantom":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201002A0000ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100290000ccccF7");
			}
			break;
		case "InputHA":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E1201002A0001ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100290001ccccF7");
			}
			break;
		case "InputPatch":
			if (channel == 127)
			{
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D0201007FF7");
				}
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E0D020100ccF7");
			}
			break;
		case "InputPhase":
			if (channel == 127)
			{
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0117007FF7");
				}
				break;
			}
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100300000ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E1101002F0000ccccF7");
			}
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				list.Add("F0433n3E7F011700ccF7");
			}
			break;
		case "InputOn":
			switch (channel)
			{
			case 127:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100310000007FF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100300000007FF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F011A007FF7");
				}
				break;
			case 255:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0129007FF7");
				}
				break;
			case 511:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0136007FF7");
				}
				break;
			case 512:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F014D007FF7");
				}
				break;
			default:
				switch (chData[channel].channelType)
				{
				case ChannelTypes.Input:
				case ChannelTypes.STIN:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E120100310000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E110100300000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F011A00ccF7");
					}
					break;
				case ChannelTypes.Mix:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201004C0000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101004B0000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F013600ccF7");
					}
					break;
				case ChannelTypes.Matrix:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201005D0000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101005C0000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F012900ccF7");
					}
					break;
				case ChannelTypes.Stereo:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201006B0000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101006A0000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F014D00ccF7");
					}
					break;
				}
				break;
			}
			break;
		case "Fader":
			switch (channel)
			{
			case 127:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100330000007FF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100320000007FF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F011C007FF7");
				}
				break;
			case 255:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F012B007FF7");
				}
				break;
			case 511:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0139007FF7");
				}
				break;
			case 512:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F014F007FF7");
				}
				break;
			default:
				switch (chData[channel].channelType)
				{
				case ChannelTypes.Input:
				case ChannelTypes.STIN:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E120100330000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E110100320000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F011C00ccF7");
					}
					break;
				case ChannelTypes.Mix:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201004E0000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101004D0000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F013900ccF7");
					}
					break;
				case ChannelTypes.Matrix:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201005F0000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101005E0000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F012B00ccF7");
					}
					break;
				case ChannelTypes.Stereo:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201006D0000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101006C0000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F014F00ccF7");
					}
					break;
				default:
					logDebug("Tried to reqParam for unknown channel type");
					break;
				}
				break;
			}
			break;
		case "InputPan":
			switch (channel)
			{
			case 127:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100320001007FF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100310001007FF7");
				}
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F011B007FF7");
				}
				break;
			case 255:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0132027FF7");
				}
				break;
			default:
				switch (chData[channel].channelType)
				{
				case ChannelTypes.Input:
				case ChannelTypes.STIN:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E120100320001ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E110100310001ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F011B00ccF7");
					}
					break;
				case ChannelTypes.Mix:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E120100570002ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E110100560002ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F013800ccF7");
					}
					break;
				case ChannelTypes.Matrix:
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F013202ccF7");
					}
					break;
				case ChannelTypes.Stereo:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E1201006C0000ccccF7");
					}
					else if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E1101006B0000ccccF7");
					}
					else if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F014E00ccF7");
					}
					break;
				}
				break;
			}
			break;
		case "InputPair":
			switch (channel)
			{
			case 127:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0118007FF7");
				}
				break;
			case 511:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0134007FF7");
				}
				break;
			case 255:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0127007FF7");
				}
				break;
			default:
				switch (chData[channel].channelType)
				{
				case ChannelTypes.Input:
				case ChannelTypes.STIN:
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F011800ccF7");
					}
					break;
				case ChannelTypes.Mix:
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F013400ccF7");
					}
					break;
				case ChannelTypes.Matrix:
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F012700ccF7");
					}
					break;
				case ChannelTypes.Stereo:
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F014B00ccF7");
					}
					break;
				}
				break;
			}
			break;
		case "insertOn":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201002E0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101002D0000ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201004B0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101004A0000ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201005C0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101005B0000ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201006A0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100690000ccccF7");
				}
				break;
			}
			break;
		case "directOutOn":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201002F0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101002E0000ccccF7");
				}
				break;
			}
			break;
		case "toStereoOn":
			switch (channel)
			{
			case 127:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0122007FF7");
				}
				break;
			case 255:
				if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E7F0132007FF7");
				}
				break;
			default:
				switch (chData[channel].channelType)
				{
				case ChannelTypes.Input:
				case ChannelTypes.STIN:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E120100410000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E110100400000ccccF7");
					}
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F012200ccF7");
					}
					break;
				case ChannelTypes.Mix:
					if (Console.type == ConsoleTypes.LS9)
					{
						list.Add("F0433n3E120100570000ccccF7");
					}
					if (Console.type == ConsoleTypes.M7CL)
					{
						list.Add("F0433n3E110100560000ccccF7");
					}
					break;
				case ChannelTypes.Matrix:
					if (Console.type == ConsoleTypes.O1V96V2)
					{
						list.Add("F0433n3E7F013200ccF7");
					}
					break;
				}
				break;
			}
			break;
		case "toMonoOn":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100420000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100410000ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100580000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100570000ccccF7");
				}
				break;
			}
			break;
		case "InputCue":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201015E0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101015A0000ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201015F0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101015B0000ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120101600000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101015C0000ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120101610000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101015D0000ccccF7");
				}
				break;
			}
			break;
		case "InputName":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120101140000ccccF7");
				list.Add("F0433n3E120101140001ccccF7");
			}
			if (Console.type != ConsoleTypes.O1V96V2)
			{
				break;
			}
			switch (channel)
			{
			case 127:
				list.Add("F0433n3E0D0204007FF7");
				list.Add("F0433n3E0D0204017FF7");
				list.Add("F0433n3E0D0204027FF7");
				list.Add("F0433n3E0D0204037FF7");
				list.Add("F0433n3E0D0217007FF7");
				list.Add("F0433n3E0D0217017FF7");
				list.Add("F0433n3E0D0217027FF7");
				list.Add("F0433n3E0D0217037FF7");
				break;
			case 255:
				list.Add("F0433n3E0D020F007FF7");
				list.Add("F0433n3E0D020F017FF7");
				list.Add("F0433n3E0D020F027FF7");
				list.Add("F0433n3E0D020F037FF7");
				break;
			case 511:
				list.Add("F0433n3E0D0210007FF7");
				list.Add("F0433n3E0D0210017FF7");
				list.Add("F0433n3E0D0210027FF7");
				list.Add("F0433n3E0D0210037FF7");
				break;
			case 512:
				list.Add("F0433n3E0D0212007FF7");
				list.Add("F0433n3E0D0212017FF7");
				list.Add("F0433n3E0D0212027FF7");
				list.Add("F0433n3E0D0212037FF7");
				break;
			default:
				switch (chData[channel].channelType)
				{
				case ChannelTypes.Input:
					list.Add("F0433n3E0D020400ccF7");
					list.Add("F0433n3E0D020401ccF7");
					list.Add("F0433n3E0D020402ccF7");
					list.Add("F0433n3E0D020403ccF7");
					break;
				case ChannelTypes.STIN:
					list.Add("F0433n3E0D021700ccF7");
					list.Add("F0433n3E0D021701ccF7");
					list.Add("F0433n3E0D021702ccF7");
					list.Add("F0433n3E0D021703ccF7");
					break;
				case ChannelTypes.Mix:
					list.Add("F0433n3E0D021000ccF7");
					list.Add("F0433n3E0D021001ccF7");
					list.Add("F0433n3E0D021002ccF7");
					list.Add("F0433n3E0D021003ccF7");
					break;
				case ChannelTypes.Matrix:
					list.Add("F0433n3E0D020F00ccF7");
					list.Add("F0433n3E0D020F01ccF7");
					list.Add("F0433n3E0D020F02ccF7");
					list.Add("F0433n3E0D020F03ccF7");
					break;
				case ChannelTypes.Stereo:
					list.Add("F0433n3E0D021200ccF7");
					list.Add("F0433n3E0D021201ccF7");
					list.Add("F0433n3E0D021202ccF7");
					list.Add("F0433n3E0D021203ccF7");
					break;
				}
				break;
			}
			break;
		case "eqHPFOn":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003C0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003B0000ccccF7");
				}
				break;
			}
			break;
		case "eqHPFFreq":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003C0001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003B0001ccccF7");
				}
				break;
			}
			break;
		case "eqAtt":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100340001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100330001ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201004F0001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101004E0001ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100600001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101005F0001ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201006E0001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101006D0001ccccF7");
				}
				break;
			}
			break;
		case "eqType":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0001ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530001ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640001ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720001ccccF7");
				}
				break;
			}
			break;
		case "eqOn":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0002ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0002ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540002ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530002ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650002ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640002ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730002ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720002ccccF7");
				}
				break;
			}
			break;
		case "eq1Type":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0003ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0003ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540003ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530003ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650003ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640003ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730003ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720003ccccF7");
				}
				break;
			}
			break;
		case "eq4Type":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0004ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0004ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540004ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530004ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650004ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640004ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730004ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720004ccccF7");
				}
				break;
			}
			break;
		case "eq1HPF":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0005ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0005ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540005ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530005ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650005ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640005ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730005ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720005ccccF7");
				}
				break;
			}
			break;
		case "eq4LPF":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0006ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0006ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540006ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530006ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650006ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640006ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730006ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720006ccccF7");
				}
				break;
			}
			break;
		case "eq1Q":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0008ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0008ccccF7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540008ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530008ccccF7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650008ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640008ccccF7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730008ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720008ccccF7");
				}
				break;
			}
			break;
		case "eq1F":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq1G":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010054000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010053000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010065000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010064000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010073000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010072000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq2Q":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010054000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010053000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010065000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010064000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010073000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010072000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq2F":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010054000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010053000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010065000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010064000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010073000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010072000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq2G":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010054000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010053000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010065000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010064000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010073000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010072000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq3Q":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq3F":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0011ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0011ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540011ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530011ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650011ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640011ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730011ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720011ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq3G":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0012ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0012ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540012ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530012ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650012ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640012ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730012ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720012ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq4Q":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0014ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0014ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540014ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530014ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650014ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640014ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730014ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720014ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq4F":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0015ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0015ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540015ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530015ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650015ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640015ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730015ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720015ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "eq4G":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003D0016ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E1101003C0016ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100540016ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100530016ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100650016ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100640016ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100730016ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100720016ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn0On":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370000ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360000ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Type":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370001ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360001ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0FilterType":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370006ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360006ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0FilterFreq":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370007ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360007ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0FilterQ":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370008ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360008ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Attack":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370009ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360009ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Range":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010037000AccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11010036000AccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Hold":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010037000BccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11010036000BccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Decay":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010037000CccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11010036000CccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Ratio":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010037000DccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11010036000DccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Gain":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010037000EccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11010036000EccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Knee":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010037000FccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11010036000FccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn0Threshold":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E120100370010ccccF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E110100360010ccccF7");
			}
			if (Console.type == ConsoleTypes.PM5D)
			{
				list.Add("");
			}
			break;
		case "Dyn1On":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A0000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100390000ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100520000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100510000ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100630000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100620000ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100710000ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100700000ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Type":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A0001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100390001ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100520001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100510001ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100630001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100620001ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100710001ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100700001ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1FilterFreq":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A0007ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100390007ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100520007ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100510007ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100630007ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100620007ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100710007ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100700007ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Attack":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A0009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100390009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100520009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100510009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100630009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100620009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100710009ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100700009ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Range":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010039000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010052000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010051000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010063000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010062000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010071000AccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010070000AccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Hold":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A000BccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010039000BccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010052000BccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010051000BccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010063000BccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010062000BccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010071000BccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010070000BccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Release":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010039000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010052000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010051000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010063000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010062000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010071000CccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010070000CccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Ratio":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010039000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010052000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010051000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010063000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010062000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010071000DccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010070000DccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Gain":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010039000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010052000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010051000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010063000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010062000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010071000EccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010070000EccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Knee":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A000FccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010039000FccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010052000FccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010051000FccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010063000FccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010062000FccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E12010071000FccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E11010070000FccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "Dyn1Threshold":
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E1201003A0010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100390010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100520010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100510010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100630010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100620010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E120100710010ccccF7");
				}
				if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E110100700010ccccF7");
				}
				if (Console.type == ConsoleTypes.PM5D)
				{
					list.Add("");
				}
				break;
			}
			break;
		case "GEQBand":
			if (Console.type == ConsoleTypes.LS9)
			{
				if (channel > 7)
				{
					channel -= 8;
					mix += 31;
				}
				list.Add("F0433n3E1201007F00" + (mix + 9).ToString("X2") + "zzzzF7");
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E1101007E00" + (mix + 9).ToString("X2") + "zzzzF7");
			}
			break;
		case "SceneDump":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0432n3E125343454E455F5F5F0400F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0432n3E114D0400F7");
			}
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				slowDump(retry: false);
			}
			break;
		case "SceneRange":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12004C696245786973745343454E455F5F5FzzzzF7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0433n3E11004C696245786973745343454E455F5F5FzzzzF7");
			}
			break;
		case "LibNumDump":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0432n3E124C49425F4E554D5F0400F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0432n3E11530405F7");
			}
			break;
		case "OutPortDump":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0432n3E124F55545F504F52540400F7");
			}
			else if (Console.type == ConsoleTypes.M7CL)
			{
				list.Add("F0432n3E11530402F7");
			}
			break;
		case "slowdump":
			slowDump(retry: false);
			return true;
		case "macrequest":
			if (Console.type == ConsoleTypes.LS9)
			{
				list.Add("F0433n3E12010228000D0000F7");
				list.Add("F0433n3E12010228000E0000F7");
				list.Add("F0433n3E12010228000F0000F7");
				list.Add("F0433n3E1201022800100000F7");
				list.Add("F0433n3E1201022800110000F7");
				list.Add("F0433n3E1201022800120000F7");
			}
			break;
		default:
			Loc.Msg("Unknown command: " + param, "Engine Error");
			break;
		}
		string text = "";
		foreach (string item in list)
		{
			text = item;
			text = text.Replace("n", MIDIdeviceID.ToString());
			text = text.Replace("mmmm", Right(StuffValueStr(mix), 4));
			text = text.Replace("mm", Right(StuffValueStr(mix), 2));
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cccc", Right(StuffValueStr(chData[channel].subChannel), 4));
			}
			if (channel >= 0 && channel < chData.Length)
			{
				text = text.Replace("cc", Right(StuffValueStr(chData[channel].subChannel), 2));
			}
			text = text.Replace("zzzz", Right(StuffValueStr(channel), 4));
			text = text.Replace("zz", Right(StuffValueStr(channel), 2));
			sendData(text);
		}
		return true;
	}

	public static bool reqParam(string param, int channel, int range, int point)
	{
		List<string> list = new List<string>();
		if (channel < 0)
		{
			return false;
		}
		string text;
		if ((text = param) != null && text == "InputMeterRange")
		{
			switch (chData[channel].channelType)
			{
			case ChannelTypes.Input:
			case ChannelTypes.STIN:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122100" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112100" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2100" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				break;
			case ChannelTypes.Mix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122101" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112101" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2102" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				break;
			case ChannelTypes.Matrix:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122102" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112102" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2101" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				break;
			case ChannelTypes.Stereo:
				if (Console.type == ConsoleTypes.LS9)
				{
					list.Add("F0433n3E122103" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.M7CL)
				{
					list.Add("F0433n3E112103" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					list.Add("F0433n3E0D2104" + point.ToString("X2") + chData[channel].subChannel.ToString("X2") + range.ToString("X4") + "F7");
				}
				break;
			}
		}
		else
		{
			Loc.Msg("Unknown command: " + param, "Engine Error");
		}
		string text2 = "";
		foreach (string item in list)
		{
			text2 = item;
			text2 = text2.Replace("n", MIDIdeviceID.ToString());
			sendData(text2);
		}
		return true;
	}

	public static void ackParam(Channel.Pr findParam, int findChannel)
	{
		reqQueue.RemoveAll((reqQueueItem item) => item.channelParam == findParam && item.sent);
	}

	public static void ackParam(Pr findParam, int findChannel)
	{
		reqQueue.RemoveAll((reqQueueItem item) => item.engineParam == findParam && item.sent);
	}

	public static bool sendData(string strData)
	{
		if (strData.Length == 0)
		{
			return false;
		}
		if (connStatus == ConnectionStatus.Disconnected)
		{
			return false;
		}
		if (Console.ver == ConsoleVersions.O1V96i)
		{
			strData = strData.Replace("F0431" + MIDIdeviceID + "3E0D", "F0431" + MIDIdeviceID + "3E1A");
			strData = strData.Replace("F0433" + MIDIdeviceID + "3E0D", "F0433" + MIDIdeviceID + "3E1A");
		}
		logDebug(3, "SEND: " + strData);
		if (outDevice != null && !outDevice.IsDisposed)
		{
			try
			{
				outDevice.Send(new SysExMessage(ToByteArray(strData)));
				MixEngine.NewMessage("SENT: " + strData, -1);
			}
			catch (OutputDeviceException ex)
			{
				int errorCode = ex.ErrorCode;
				if (errorCode == 1 || errorCode == 6)
				{
					MixEngine.NewMessage("Connection lost!", -99);
					heartBeats = 0;
					disconnectConsole();
				}
				else
				{
					logError("SendData()", "OutEvent", "Error #" + ex.ErrorCode + "\n" + ex.ToString());
				}
				return false;
			}
		}
		return true;
	}

	public static void logError(string Source, string Category, string Message)
	{
		logDebug("ERROR: [" + Source + "][" + Category + "] " + Message);
		switch (Category)
		{
		case "UI":
			if (frmConfirm.sayFail("Unfortunately, there was an error updating the User Interface.\nI wrote it down in Error.Log and will try to keep going.", "User Interface Error", hMainWindow) == DialogResult.OK)
			{
				if (uploadLogFile())
				{
					Loc.Msg("Successfully reported problem to developer. Thank you!");
				}
				else
				{
					Loc.Msg("Could not upload problem report. Please contact support.");
				}
			}
			return;
		case "OutEvent":
			if (frmConfirm.sayFail("Something bad happened when I tried to tell the console what you changed.\nI wrote it down in Error.Log but you probably better re-start and/or re-sync.", "Outbound Event Error", hMainWindow) == DialogResult.OK)
			{
				if (uploadLogFile())
				{
					Loc.Msg("Successfully reported problem to developer. Thank you!");
				}
				else
				{
					Loc.Msg("Could not upload problem report. Please contact support.");
				}
			}
			return;
		case "InEvent":
			if (frmConfirm.sayFail("Something bad happened when the console told me about a parameter change.\nI wrote it down in Error.Log but you probably better re-start and/or re-sync.", "Inbound Event Error", hMainWindow) == DialogResult.OK)
			{
				if (uploadLogFile())
				{
					Loc.Msg("Successfully reported problem to developer. Thank you!");
				}
				else
				{
					Loc.Msg("Could not upload problem report. Please contact support.");
				}
			}
			return;
		case "Connection":
			frmConfirm.sayFail("Could not connect due to: " + Message, "Connection Failed", hMainWindow);
			return;
		case "Sync":
			frmConfirm.sayFail(Message, "Sync Error", hMainWindow);
			return;
		case "Server":
			frmConfirm.sayFail(Message, "Server Error", hMainWindow);
			return;
		}
		if (frmConfirm.sayFail("I've run into a problem with: " + Category + ".\nI wrote it down in Error.Log and will try to keep going.", "General Error", hMainWindow) == DialogResult.OK)
		{
			if (uploadLogFile())
			{
				Loc.Msg("Successfully reported problem to developer. Thank you!");
			}
			else
			{
				Loc.Msg("Could not upload problem report. Please contact support.");
			}
		}
	}

	public static void logDebug(int logLevel, string Message)
	{
		if (Settings.Default.debugLevel >= logLevel)
		{
			logDebug(Message);
		}
	}

	public static void logDebug(string Message)
	{
		if (!logIsOK)
		{
			return;
		}
		lock (logLock)
		{
			if (logFile == null)
			{
				try
				{
					string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					folderPath += "\\01vmix";
					if (!Directory.Exists(folderPath))
					{
						Directory.CreateDirectory(folderPath);
					}
					logFile = new StreamWriter(folderPath + "\\Error.Log", append: true);
				}
				catch (IOException)
				{
					frmConfirm.sayFail("I could not create the error log file.\nI will keep going, but won't record problems.", "Logging Failed", hMainWindow);
					logIsOK = false;
				}
			}
			int num = 0;
			while (num < 3)
			{
				try
				{
					logFile.WriteLine(DateTime.Now.ToString() + " [" + Thread.CurrentThread.ManagedThreadId.ToString("D6") + "] -- " + Message);
					logFile.Flush();
					return;
				}
				catch (Exception)
				{
					num++;
					Thread.Sleep(20);
				}
			}
			if (num >= 3)
			{
				System.Console.WriteLine("Could not write Error Log: " + Message);
			}
		}
	}

	public static void logClose()
	{
		if (logFile != null)
		{
			logFile.Close();
			logFile = null;
			logIsOK = true;
		}
	}

	public static bool uploadLogFile()
	{
		if (!LICENSING_ENABLED)
		{
			// Open-source build: logs stay on the local machine, they are never
			// uploaded to the original vendor.
			logClose();
			return false;
		}
		try
		{
			logClose();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			folderPath += "\\01vmix\\Error.Log";
			using (WebClient webClient = new WebClient())
			{
				webClient.UploadFile("http://www.airfader.com/data/recvlog.php", "POST", folderPath);
			}
			return true;
		}
		catch (Exception ex)
		{
			logError("cmdOpenLog()", "UI", ex.ToString());
		}
		return false;
	}
}
