using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using AirFader.Properties;
using ProtoBuf;
using faderspeak;

namespace AirFader;

public class FaderService
{
	private class queueItem
	{
		public string source;

		public string msg;

		public queueItem()
		{
			source = string.Empty;
			msg = string.Empty;
		}

		public queueItem(string xSource, string xMsg)
		{
			source = xSource;
			msg = xMsg;
		}
	}

	public class SocketPacket
	{
		public Socket currentSocket;

		public int clientNumber;

		public string message;

		public string sourcetag;

		public uint sessionID;

		public uint userID;

		public string clientVersion;

		public bool useMeters;

		public DateTime dtConnected;

		public byte[] recvBuffer = new byte[64];

		public byte[] msgBuffer = new byte[512];

		public int dataReceived;

		public SocketPacket(Socket new_socket, int new_clientNumber)
		{
			currentSocket = new_socket;
			clientNumber = new_clientNumber;
		}
	}

	public AsyncCallback pfnWorkerCallBack;

	private Socket mainSocket;

	private Timer timerBeat;

	public List<SocketPacket> workerSockets = new List<SocketPacket>(2);

	private int clientCount;

	private Dictionary<ChParam, Channel.Pr> speakToParam = new Dictionary<ChParam, Channel.Pr>();

	private Queue<queueItem> outQueue = new Queue<queueItem>();

	private ManualResetEvent outFlag = new ManualResetEvent(initialState: false);

	private Thread queueThread;

	public bool isOnline;

	private List<UserRecord> userDB = new List<UserRecord>();

	private MD5 md5Hasher;

	public UserRecord findUser(string username)
	{
		return userDB.Find((UserRecord u) => u.userName.ToLower() == username.ToLower());
	}

	public UserRecord findUser(uint userid)
	{
		return userDB.Find((UserRecord u) => u.userId == userid);
	}

	public List<UserRecord> listUsers()
	{
		userDB.Sort((UserRecord u1, UserRecord u2) => u1.userName.CompareTo(u2.userName));
		return userDB;
	}

	public void disableUser(uint userid)
	{
		UserRecord userRecord = findUser(userid);
		if (userRecord != null)
		{
			userRecord.active = false;
			updateUser(userRecord);
		}
	}

	public bool updateUser(UserRecord user)
	{
		try
		{
			if (user.userName == "")
			{
				return false;
			}
			if (user.userId != 0)
			{
				int num = userDB.FindIndex((UserRecord u) => u.userId == user.userId);
				if (num >= 0)
				{
					userDB[num] = user;
					for (int num2 = workerSockets.Count - 1; num2 >= 0; num2--)
					{
						if (workerSockets[num2].userID == user.userId)
						{
							if (!user.active)
							{
								MixEngine.logDebug("User " + user.userName + " not active.");
								GenMessage genMessage = new GenMessage();
								genMessage.msgtype = MsgType.MSG_DENIED;
								genMessage.message = "User not enabled.";
								sendProto(genMessage, workerSockets[num2]);
								workerSockets[num2].currentSocket.Close(2);
							}
							else
							{
								GenMessage genMessage2 = new GenMessage();
								genMessage2.msgtype = MsgType.MSG_AUTH;
								genMessage2.message = "Logged in";
								BaseMessage baseMessage = new BaseMessage();
								baseMessage.genMessage = genMessage2;
								baseMessage.userRecord = user;
								baseMessage.version = "20151123";
								sendProto(baseMessage, workerSockets[num2]);
							}
							MixEngine.logDebug("Access changed for logged-in user " + user.userName);
						}
					}
					storeUserList();
					return true;
				}
			}
			user.userId = 0u;
			foreach (UserRecord item in userDB)
			{
				user.userId = Math.Max(user.userId, item.userId);
				if (user.userName == item.userName)
				{
					return false;
				}
			}
			user.userId++;
			userDB.Add(user);
			storeUserList();
			return true;
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("ERROR - Could not save User database due to: " + ex.Message);
			return false;
		}
	}

	public bool deleteUser(UserRecord user)
	{
		if (user.userId != 0)
		{
			int num = userDB.FindIndex((UserRecord u) => u.userId == user.userId);
			if (num >= 0)
			{
				userDB.RemoveAt(num);
				storeUserList();
				return true;
			}
		}
		return false;
	}

	public bool storeUserList()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		folderPath += "\\01vmix";
		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}
		FileStream fileStream = File.Create(folderPath + "\\Users.dat");
		foreach (UserRecord item in userDB)
		{
			Serializer.SerializeWithLengthPrefix(fileStream, item, PrefixStyle.Fixed32);
		}
		fileStream.Close();
		return true;
	}

	public bool loadUserList()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		folderPath += "\\01vmix";
		if (File.Exists(folderPath + "\\Users.dat"))
		{
			FileStream fileStream = File.OpenRead(folderPath + "\\Users.dat");
			userDB.Clear();
			for (UserRecord userRecord = Serializer.DeserializeWithLengthPrefix<UserRecord>(fileStream, PrefixStyle.Fixed32); userRecord != null; userRecord = Serializer.DeserializeWithLengthPrefix<UserRecord>(fileStream, PrefixStyle.Fixed32))
			{
				userDB.Add(userRecord);
			}
			fileStream.Close();
		}
		return true;
	}

	public FaderService()
	{
		speakToParam.Add(ChParam.CH_BUSTYPE, Channel.Pr.BusType);
		speakToParam.Add(ChParam.CH_PANLINK, Channel.Pr.PanLink);
		speakToParam.Add(ChParam.CH_GROUP, Channel.Pr.Group);
		speakToParam.Add(ChParam.CH_PAIR, Channel.Pr.Pair);
		speakToParam.Add(ChParam.CH_CUE, Channel.Pr.Cue);
		speakToParam.Add(ChParam.CH_PATCHIN, Channel.Pr.PatchIn);
		speakToParam.Add(ChParam.CH_HA, Channel.Pr.HA);
		speakToParam.Add(ChParam.CH_PHANTOM, Channel.Pr.Phantom);
		speakToParam.Add(ChParam.CH_PHASE, Channel.Pr.Phase);
		speakToParam.Add(ChParam.CH_PAN, Channel.Pr.Pan);
		speakToParam.Add(ChParam.CH_ON, Channel.Pr.On);
		speakToParam.Add(ChParam.CH_FADER, Channel.Pr.Fader);
		speakToParam.Add(ChParam.CH_INSERTON, Channel.Pr.InsertOn);
		speakToParam.Add(ChParam.CH_DIRECTOUTON, Channel.Pr.DirectOutOn);
		speakToParam.Add(ChParam.CH_TOSTEREOON, Channel.Pr.ToStereoOn);
		speakToParam.Add(ChParam.CH_TOMONOON, Channel.Pr.ToMonoOn);
		speakToParam.Add(ChParam.CH_EQON, Channel.Pr.EQOn);
		speakToParam.Add(ChParam.CH_EQATT, Channel.Pr.EQAtt);
		speakToParam.Add(ChParam.CH_EQFLATTEN, Channel.Pr.EQFlatten);
		speakToParam.Add(ChParam.CH_EQ1HPFON, Channel.Pr.EQ1HPFOn);
		speakToParam.Add(ChParam.CH_EQ1TYPE, Channel.Pr.EQ1Type);
		speakToParam.Add(ChParam.CH_EQ4LPFON, Channel.Pr.EQ4LPFOn);
		speakToParam.Add(ChParam.CH_EQ4TYPE, Channel.Pr.EQ4Type);
		speakToParam.Add(ChParam.CH_EQHPFFREQ, Channel.Pr.EQHPFFreq);
		speakToParam.Add(ChParam.CH_EQHPFON, Channel.Pr.EQHPFOn);
		speakToParam.Add(ChParam.CH_EQ1G, Channel.Pr.EQ1G);
		speakToParam.Add(ChParam.CH_EQ2G, Channel.Pr.EQ2G);
		speakToParam.Add(ChParam.CH_EQ3G, Channel.Pr.EQ3G);
		speakToParam.Add(ChParam.CH_EQ4G, Channel.Pr.EQ4G);
		speakToParam.Add(ChParam.CH_EQ1F, Channel.Pr.EQ1F);
		speakToParam.Add(ChParam.CH_EQ2F, Channel.Pr.EQ2F);
		speakToParam.Add(ChParam.CH_EQ3F, Channel.Pr.EQ3F);
		speakToParam.Add(ChParam.CH_EQ4F, Channel.Pr.EQ4F);
		speakToParam.Add(ChParam.CH_EQ1Q, Channel.Pr.EQ1Q);
		speakToParam.Add(ChParam.CH_EQ2Q, Channel.Pr.EQ2Q);
		speakToParam.Add(ChParam.CH_EQ3Q, Channel.Pr.EQ3Q);
		speakToParam.Add(ChParam.CH_EQ4Q, Channel.Pr.EQ4Q);
		speakToParam.Add(ChParam.CH_DYN0ON, Channel.Pr.Dyn0On);
		speakToParam.Add(ChParam.CH_DYN0TYPE, Channel.Pr.Dyn0Type);
		speakToParam.Add(ChParam.CH_DYN0ATTACK, Channel.Pr.Dyn0Attack);
		speakToParam.Add(ChParam.CH_DYN0RANGE, Channel.Pr.Dyn0Range);
		speakToParam.Add(ChParam.CH_DYN0HOLD, Channel.Pr.Dyn0Hold);
		speakToParam.Add(ChParam.CH_DYN0DECAY, Channel.Pr.Dyn0Decay);
		speakToParam.Add(ChParam.CH_DYN0RATIO, Channel.Pr.Dyn0Ratio);
		speakToParam.Add(ChParam.CH_DYN0GAIN, Channel.Pr.Dyn0Gain);
		speakToParam.Add(ChParam.CH_DYN0KNEE, Channel.Pr.Dyn0Knee);
		speakToParam.Add(ChParam.CH_DYN0THRESHOLD, Channel.Pr.Dyn0Threshold);
		speakToParam.Add(ChParam.CH_DYN1ON, Channel.Pr.Dyn1On);
		speakToParam.Add(ChParam.CH_DYN1TYPE, Channel.Pr.Dyn1Type);
		speakToParam.Add(ChParam.CH_DYN1ATTACK, Channel.Pr.Dyn1Attack);
		speakToParam.Add(ChParam.CH_DYN1RANGE, Channel.Pr.Dyn1Range);
		speakToParam.Add(ChParam.CH_DYN1HOLD, Channel.Pr.Dyn1Hold);
		speakToParam.Add(ChParam.CH_DYN1RELEASE, Channel.Pr.Dyn1Release);
		speakToParam.Add(ChParam.CH_DYN1RATIO, Channel.Pr.Dyn1Ratio);
		speakToParam.Add(ChParam.CH_DYN1GAIN, Channel.Pr.Dyn1Gain);
		speakToParam.Add(ChParam.CH_DYN1KNEE, Channel.Pr.Dyn1Knee);
		speakToParam.Add(ChParam.CH_DYN1THRESHOLD, Channel.Pr.Dyn1Threshold);
		md5Hasher = MD5.Create();
		loadUserList();
	}

	public bool start(IPAddress serverIP, int serverPort)
	{
		try
		{
			mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint localEP = new IPEndPoint(IPAddress.Any, serverPort);
			mainSocket.Bind(localEP);
			mainSocket.Listen(5);
			mainSocket.BeginAccept(onClientConnect, null);
			timerBeat = new Timer(tickBeat, null, TimeSpan.FromSeconds(2.0), TimeSpan.FromMilliseconds(-1.0));
			isOnline = true;
			return true;
		}
		catch (SocketException ex)
		{
			MixEngine.logDebug("FaderService.start(): Socket Error: " + ex.Message);
			MixEngine.logError("FaderService", "Server", "Could not start server - is the port already in use?\nTry restarting 01vmix or rebooting the computer.\nIf the problem continues, another program is using this port. Try another number (be sure to enter same number in Remote Clients.)");
		}
		return false;
	}

	public bool stop()
	{
		try
		{
			isOnline = false;
			CloseSockets();
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("FaderService.stop() Error: " + ex.Message);
		}
		return true;
	}

	private void tickBeat(object state)
	{
		try
		{
			if (!isOnline)
			{
				timerBeat.Dispose();
				timerBeat = null;
			}
			else
			{
				sendProto("SYSTEM", new BaseMessage());
				timerBeat.Change(TimeSpan.FromSeconds(2.0), TimeSpan.FromMilliseconds(-1.0));
			}
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("FaderServer.tickBeat() Error: " + ex.Message);
		}
	}

	public void sendStatus()
	{
		SystemStatus systemStatus = new SystemStatus();
		if (MixEngine.connStatus == ConnectionStatus.Connected)
		{
			systemStatus.consoleStatus = 1;
		}
		else
		{
			systemStatus.consoleStatus = 0;
		}
		systemStatus.chOffsetInput = MixEngine.chOffsetInput;
		systemStatus.chOffsetMatrix = MixEngine.chOffsetMatrix;
		systemStatus.chOffsetMix = MixEngine.chOffsetMix;
		systemStatus.chOffsetMonitor = MixEngine.chOffsetMonitor;
		systemStatus.chOffsetStereo = MixEngine.chOffsetStereo;
		systemStatus.chOffsetSTIN = MixEngine.chOffsetSTIN;
		systemStatus.maxInputs = Console.maxInputs;
		systemStatus.maxSTIN = Console.maxSTIN;
		systemStatus.maxMix = Console.maxMIX;
		systemStatus.maxMatrix = Console.maxMATRIX;
		systemStatus.consoleType = (int)Console.type;
		systemStatus.consoleLabel = Settings.Default.MIDI_Input;
		systemStatus.clockSpeed = Console.clockSpeed;
		sendProto("SYSTEM", systemStatus);
	}

	public void onClientConnect(IAsyncResult asyn)
	{
		try
		{
			if (mainSocket.IsBound)
			{
				Socket socket = mainSocket.EndAccept(asyn);
				Interlocked.Increment(ref clientCount);
				socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, optionValue: true);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 10000);
				SocketPacket socketPacket = new SocketPacket(socket, clientCount);
				socketPacket.sourcetag = "AFLITE_" + socket.RemoteEndPoint.ToString();
				// MixEngine.licenseClients is UNLOCKED_CLIENT_LIMIT (99) in the
				// open-source build, so the server accepts many clients.
				if (workerSockets.Count < MixEngine.licenseClients)
				{
					lock (workerSockets)
					{
						workerSockets.Add(socketPacket);
					}
					WaitForData(socketPacket);
				}
				else
				{
					MixEngine.logDebug("Server user limit exceeded. Denied user from [" + ((IPEndPoint)socketPacket.currentSocket.RemoteEndPoint).Address.ToString() + "]");
					GenMessage genMessage = new GenMessage();
					genMessage.msgtype = MsgType.MSG_DENIED;
					genMessage.message = "Server user limit exceeded.";
					sendProto(genMessage, socketPacket);
					socketPacket.currentSocket.Close(2);
				}
				mainSocket.BeginAccept(onClientConnect, null);
			}
			else
			{
				MixEngine.logDebug("onClientConnect(): Server no longer bound - must be stopping server.");
			}
		}
		catch (ObjectDisposedException)
		{
			MixEngine.logDebug("onClientConnect(): Object Disposed Exception. Closing down server?");
		}
		catch (SocketException ex2)
		{
			MixEngine.logDebug("SOCKET: " + ex2.Message);
		}
	}

	private Channel.Pr findKey(Dictionary<Channel.Pr, string> d, string value)
	{
		foreach (KeyValuePair<Channel.Pr, string> item in d)
		{
			if (item.Value.ToUpper() == value.ToUpper())
			{
				return item.Key;
			}
		}
		return Channel.Pr.None;
	}

	public void WaitForData(SocketPacket socPak)
	{
		try
		{
			if (pfnWorkerCallBack == null)
			{
				pfnWorkerCallBack = OnDataReceived;
			}
			socPak.currentSocket.BeginReceive(socPak.recvBuffer, 0, socPak.recvBuffer.Length, SocketFlags.None, pfnWorkerCallBack, socPak);
		}
		catch (SocketException ex)
		{
			MixEngine.logDebug("SOCKET WAIT: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("WaitForData() Error: " + ex2.Message);
		}
	}

	private short SwapInt16(short v)
	{
		try
		{
			return (short)(((v & 0xFF) << 8) | ((v >> 8) & 0xFF));
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("SwapInt16() Error: " + ex.Message);
		}
		return 0;
	}

	private int SwapInt32(int v)
	{
		try
		{
			return ((SwapInt16((short)v) & 0xFFFF) << 16) | (SwapInt16((short)(v >> 16)) & 0xFFFF);
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("SwapInt32() Error: " + ex.Message);
		}
		return 0;
	}

	public void OnDataReceived(IAsyncResult asyn)
	{
		SocketPacket socketPacket = (SocketPacket)asyn.AsyncState;
		try
		{
			int num = 0;
			if (socketPacket.currentSocket.Connected)
			{
				num = socketPacket.currentSocket.EndReceive(asyn);
			}
			if (num < 1)
			{
				socketPacket.currentSocket.Close();
				lock (workerSockets)
				{
					workerSockets.Remove(socketPacket);
				}
				MixEngine.logDebug("Client disconnected. Now there are " + workerSockets.Count);
				return;
			}
			if (socketPacket.dataReceived + num >= socketPacket.msgBuffer.Length)
			{
				MixEngine.logError("TCP BUFFER", "Network Data Stream", "Received " + num + " bytes which (added to " + socketPacket.dataReceived + ") would exceed buffer length");
				socketPacket.dataReceived = 0;
				num = 0;
				socketPacket.currentSocket.Close();
				lock (workerSockets)
				{
					workerSockets.Remove(socketPacket);
					return;
				}
			}
			try
			{
				Buffer.BlockCopy(socketPacket.recvBuffer, 0, socketPacket.msgBuffer, socketPacket.dataReceived, num);
				socketPacket.dataReceived += num;
			}
			catch (Exception ex)
			{
				MixEngine.logError("TCP BUFFER", "Network Data Stream", ex.Message);
			}
			while (socketPacket.dataReceived >= 4)
			{
				int v = BitConverter.ToInt32(socketPacket.msgBuffer, 0);
				v = SwapInt32(v);
				if (socketPacket.dataReceived - 4 < v)
				{
					break;
				}
				try
				{
					BaseMessage baseMsg = Serializer.Deserialize<BaseMessage>(new MemoryStream(socketPacket.msgBuffer, 4, v));
					processProc(baseMsg, socketPacket);
				}
				catch (ProtoException ex2)
				{
					MixEngine.logDebug("ProtoException: " + ex2.Message);
					MixEngine.logDebug("ProtoException: " + ex2.Source);
					MixEngine.logDebug("ProtoException: " + ex2.Data.ToString());
				}
				socketPacket.dataReceived -= v + 4;
				Buffer.BlockCopy(socketPacket.msgBuffer, 4 + v, socketPacket.msgBuffer, 0, socketPacket.dataReceived);
			}
			if (socketPacket != null)
			{
				if (socketPacket.currentSocket.Connected)
				{
					WaitForData(socketPacket);
					return;
				}
				workerSockets.Remove(socketPacket);
				MixEngine.logDebug("Client ended. Now there are " + workerSockets.Count);
			}
		}
		catch (ObjectDisposedException ex3)
		{
			MixEngine.logDebug("DATA RECV ERROR 1: " + ex3.Message + "\n" + ex3.StackTrace);
		}
		catch (SocketException ex4)
		{
			MixEngine.logDebug("DATA RECV ERROR #" + ex4.ErrorCode + ": " + ex4.Message);
			try
			{
				socketPacket.currentSocket.Disconnect(reuseSocket: false);
			}
			catch (Exception ex5)
			{
				MixEngine.logDebug("Socket error [" + ex5.InnerException.Message + "] during disconnect: " + ex5.Message);
			}
			lock (workerSockets)
			{
				workerSockets.Remove(socketPacket);
			}
		}
	}

	public void dropConnection(SocketPacket socketInfo)
	{
		try
		{
			MixEngine.logDebug("Force-disconnecting socket #" + socketInfo.clientNumber);
			GenMessage genMessage = new GenMessage();
			genMessage.msgtype = MsgType.MSG_DENIED;
			genMessage.message = "Disconnected by Admin";
			sendProto(genMessage, socketInfo);
			socketInfo.currentSocket.Close(1);
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("dropConnection() Error: " + ex.Message);
		}
	}

	public void processProc(BaseMessage baseMsg, SocketPacket socketInfo)
	{
		try
		{
			if (baseMsg.userName.Length > 0)
			{
				if (baseMsg.version.Length == 0)
				{
					MixEngine.logDebug("Denied connection from very old client.");
					GenMessage genMessage = new GenMessage();
					genMessage.msgtype = MsgType.MSG_DENIED;
					genMessage.message = "Please update AirFader mobile client.\n(Server is 3.x)";
					sendProto(genMessage, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
				float result = 0f;
				if (float.TryParse(baseMsg.version, out result) && result < 20130224f)
				{
					MixEngine.logDebug("Denied connection from old client (Ver " + baseMsg.version + ").");
					GenMessage genMessage2 = new GenMessage();
					genMessage2.msgtype = MsgType.MSG_DENIED;
					genMessage2.message = "Please update AirFader mobile client.\n(Server is 3.x)";
					sendProto(genMessage2, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
				if (baseMsg.version.CompareTo("20151122.0") < 0)
				{
					socketInfo.useMeters = true;
				}
				else
				{
					socketInfo.useMeters = false;
				}
				socketInfo.clientVersion = baseMsg.version;
				socketInfo.dtConnected = DateTime.Now;
				UserRecord userRecord = userDB.Find((UserRecord u) => u.userName.ToLower() == baseMsg.userName.ToLower());
				if (userRecord == null)
				{
					MixEngine.logDebug("Invalid username received.");
					GenMessage genMessage3 = new GenMessage();
					genMessage3.msgtype = MsgType.MSG_DENIED;
					genMessage3.message = "User does not exist.";
					sendProto(genMessage3, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
				if (!(baseMsg.passHash == userRecord.passHash))
				{
					MixEngine.logDebug("Invalid password received.");
					GenMessage genMessage4 = new GenMessage();
					genMessage4.msgtype = MsgType.MSG_DENIED;
					genMessage4.message = "Password does not match.";
					sendProto(genMessage4, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
				if (!userRecord.active)
				{
					MixEngine.logDebug("User " + userRecord.userName + " not active.");
					GenMessage genMessage5 = new GenMessage();
					genMessage5.msgtype = MsgType.MSG_DENIED;
					genMessage5.message = "User not enabled.";
					sendProto(genMessage5, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
				Random random = new Random();
				socketInfo.sessionID = (uint)random.Next(1, int.MaxValue);
				socketInfo.userID = userRecord.userId;
				GenMessage genMessage6 = new GenMessage();
				genMessage6.msgtype = MsgType.MSG_AUTH;
				genMessage6.message = "Logged in";
				BaseMessage baseMessage = new BaseMessage();
				baseMessage.genMessage = genMessage6;
				baseMessage.userRecord = userRecord;
				baseMessage.version = "20151201";
				sendProto(baseMessage, socketInfo);
				MixEngine.logDebug("User [" + userRecord.userName + "] logged in from [" + ((IPEndPoint)socketInfo.currentSocket.RemoteEndPoint).Address.ToString() + "] as Client #" + workerSockets.Count);
				int num = 0;
				int num2 = 0;
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket.clientVersion == "20151123.0")
					{
						num++;
					}
					else if (workerSocket.clientVersion == "20140120.0")
					{
						num2++;
					}
					else
					{
						num++;
					}
				}
				MixEngine.maxAndroidConnections = Math.Max(num, MixEngine.maxAndroidConnections);
				MixEngine.maxIOSConnections = Math.Max(num2, MixEngine.maxIOSConnections);
			}
			else
			{
				if (baseMsg.sessionID == 0)
				{
					MixEngine.logDebug("SessionID is 0 and no login info received. May not be a real client from " + ((IPEndPoint)socketInfo.currentSocket.RemoteEndPoint).Address.ToString());
					GenMessage genMessage7 = new GenMessage();
					genMessage7.msgtype = MsgType.MSG_DENIED;
					genMessage7.message = "Login name is required.";
					sendProto(genMessage7, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
				if (baseMsg.sessionID != socketInfo.sessionID)
				{
					MixEngine.logDebug("SessionID " + baseMsg.sessionID + " received doesn't match Socket SessionID " + socketInfo.sessionID);
					GenMessage genMessage8 = new GenMessage();
					genMessage8.msgtype = MsgType.MSG_DENIED;
					genMessage8.message = "Transaction blocked.";
					sendProto(genMessage8, socketInfo);
					socketInfo.currentSocket.Close(2);
					return;
				}
			}
			if (baseMsg.command != null)
			{
				switch (baseMsg.command.command)
				{
				case CmdParam.CMD_GOODBYE:
					socketInfo.currentSocket.Close(2);
					MixEngine.logDebug("Client said Goodbye.");
					break;
				case CmdParam.CMD_GETSYS:
				{
					SystemStatus systemStatus = new SystemStatus();
					if (MixEngine.connStatus == ConnectionStatus.Connected)
					{
						systemStatus.consoleStatus = 1;
					}
					else
					{
						systemStatus.consoleStatus = 0;
					}
					systemStatus.chOffsetInput = MixEngine.chOffsetInput;
					systemStatus.chOffsetMatrix = MixEngine.chOffsetMatrix;
					systemStatus.chOffsetMix = MixEngine.chOffsetMix;
					systemStatus.chOffsetMonitor = MixEngine.chOffsetMonitor;
					systemStatus.chOffsetStereo = MixEngine.chOffsetStereo;
					systemStatus.chOffsetSTIN = MixEngine.chOffsetSTIN;
					systemStatus.maxInputs = Console.maxInputs;
					systemStatus.maxSTIN = Console.maxSTIN;
					systemStatus.maxMix = Console.maxMIX;
					systemStatus.maxMatrix = Console.maxMATRIX;
					systemStatus.consoleType = (int)Console.type;
					systemStatus.consoleLabel = Settings.Default.MIDI_Input;
					systemStatus.clockSpeed = Console.clockSpeed;
					sendProto(systemStatus, socketInfo);
					break;
				}
				case CmdParam.CMD_BUSSYNC:
				{
					BusSync busSync = new BusSync();
					busSync.BusChannel = baseMsg.command.channelLo;
					busSync.StartCh = 0;
					for (int k = 0; k < MixEngine.chOffsetStereo + 3; k++)
					{
						busSync.chType.Add((ChType)MixEngine.chData[k].channelType);
						busSync.Cue.Add(MixEngine.chData[k].getParamBool(Channel.Pr.Cue));
						busSync.Name.Add(MixEngine.chData[k].name);
						busSync.HardName.Add(MixEngine.chData[k].hardname);
						busSync.Pair.Add(MixEngine.chData[k].getParamBool(Channel.Pr.Pair));
						busSync.PanLink.Add(MixEngine.chData[k].getParamBool(Channel.Pr.PanLink));
						busSync.BusType.Add(MixEngine.chData[k].getParam(Channel.Pr.BusType));
						if (busSync.BusChannel >= MixEngine.chOffsetStereo)
						{
							busSync.On.Add(MixEngine.chData[k].getParamBool(Channel.Pr.On));
							busSync.Level.Add(MixEngine.chData[k].getParam(Channel.Pr.Fader));
						}
						else if (busSync.BusChannel >= MixEngine.chOffsetMatrix)
						{
							busSync.On.Add(MixEngine.chData[k].getParamBool(Channel.Pr.SendToMatrixOn, busSync.BusChannel - MixEngine.chOffsetMatrix));
							busSync.Level.Add(MixEngine.chData[k].getParam(Channel.Pr.SendToMatrix, busSync.BusChannel - MixEngine.chOffsetMatrix));
							busSync.Pan.Add(MixEngine.chData[k].getParam(Channel.Pr.SendToMatrixPan, busSync.BusChannel - MixEngine.chOffsetMatrix));
							busSync.Pos.Add(MixEngine.chData[k].getParamBool(Channel.Pr.SendToMatrixPre, busSync.BusChannel - MixEngine.chOffsetMatrix));
						}
						else if (busSync.BusChannel >= MixEngine.chOffsetMix)
						{
							busSync.On.Add(MixEngine.chData[k].getParamBool(Channel.Pr.SendToMixOn, busSync.BusChannel - MixEngine.chOffsetMix));
							busSync.Level.Add(MixEngine.chData[k].getParam(Channel.Pr.SendToMix, busSync.BusChannel - MixEngine.chOffsetMix));
							busSync.Pan.Add(MixEngine.chData[k].getParam(Channel.Pr.SendToMixPan, busSync.BusChannel - MixEngine.chOffsetMix));
							busSync.Pos.Add(MixEngine.chData[k].getParamBool(Channel.Pr.SendToMixPre, busSync.BusChannel - MixEngine.chOffsetMix));
						}
					}
					sendProto(busSync, socketInfo);
					break;
				}
				case CmdParam.CMD_MUTEMASTERSYNC:
				{
					MixEngine.logDebug("Sending MuteMasterSync");
					GeneralParam generalParam = new GeneralParam();
					generalParam.param = GenParam.GEN_MUTEMASTER;
					generalParam.index = 0;
					for (int n = 0; n < MixEngine.muteGroup.Length; n++)
					{
						ChannelParam channelParam2 = new ChannelParam();
						channelParam2.param = ChParam.CH_MUTEGROUP;
						channelParam2.channel = 0;
						channelParam2.index = n;
						for (int num3 = 0; num3 < MixEngine.chOffsetStereo + 3; num3++)
						{
							if (MixEngine.muteGroup[n].members.Contains(num3))
							{
								channelParam2.value.Add(1);
							}
							else
							{
								channelParam2.value.Add(0);
							}
						}
						sendProto(channelParam2, socketInfo);
						generalParam.value.Add(MixEngine.muteGroup[n].muted ? 1 : 0);
					}
					sendProto(generalParam, socketInfo);
					break;
				}
				case CmdParam.CMD_CHANNELSYNC:
				{
					ChannelSync channelSync = new ChannelSync();
					int channelLo = baseMsg.command.channelLo;
					MixEngine.logDebug("Sending ChannelSync for channel " + channelLo);
					channelSync.Channel = channelLo;
					channelSync.chType = (ChType)MixEngine.chData[channelLo].channelType;
					channelSync.HA = MixEngine.chData[channelLo].getParam(Channel.Pr.HA);
					channelSync.Phantom = MixEngine.chData[channelLo].getParamBool(Channel.Pr.Phantom);
					channelSync.Phase = MixEngine.chData[channelLo].getParamBool(Channel.Pr.Phase);
					channelSync.Fader = MixEngine.chData[channelLo].getParam(Channel.Pr.Fader);
					channelSync.RelativeHA = MixEngine.chData[channelLo].getParam(Channel.Pr.RelativeHA);
					channelSync.RelativeFader = MixEngine.chData[channelLo].getParam(Channel.Pr.RelativeFader);
					channelSync.On = MixEngine.chData[channelLo].getParamBool(Channel.Pr.On);
					channelSync.Cue = MixEngine.chData[channelLo].getParamBool(Channel.Pr.Cue);
					channelSync.Name = MixEngine.chData[channelLo].name;
					channelSync.Pan = MixEngine.chData[channelLo].getParam(Channel.Pr.Pan);
					channelSync.Balance = MixEngine.chData[channelLo].getParam(Channel.Pr.Balance);
					channelSync.Group = MixEngine.chData[channelLo].getParam(Channel.Pr.Group);
					channelSync.Pair = MixEngine.chData[channelLo].getParam(Channel.Pr.Pair);
					channelSync.BusType = MixEngine.chData[channelLo].getParam(Channel.Pr.BusType);
					channelSync.PanLink = MixEngine.chData[channelLo].getParam(Channel.Pr.PanLink);
					channelSync.MuteGroup1 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 0);
					channelSync.MuteGroup2 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 1);
					channelSync.MuteGroup3 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 2);
					channelSync.MuteGroup4 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 3);
					channelSync.MuteGroup5 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 4);
					channelSync.MuteGroup6 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 5);
					channelSync.MuteGroup7 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 6);
					channelSync.MuteGroup8 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.MuteGroup, 7);
					channelSync.DCAGroup1 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 0);
					channelSync.DCAGroup2 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 1);
					channelSync.DCAGroup3 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 2);
					channelSync.DCAGroup4 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 3);
					channelSync.DCAGroup5 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 4);
					channelSync.DCAGroup6 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 5);
					channelSync.DCAGroup7 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 6);
					channelSync.DCAGroup8 = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DCAGroup, 7);
					channelSync.InsertOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.InsertOn);
					channelSync.DirectOutOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.DirectOutOn);
					channelSync.ToStereoOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.ToStereoOn);
					channelSync.ToMonoOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.ToMonoOn);
					channelSync.PatchIn = MixEngine.chData[channelLo].getParam(Channel.Pr.PatchIn);
					channelSync.EQHPFOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.EQHPFOn);
					channelSync.EQHPFFreq = MixEngine.chData[channelLo].getParam(Channel.Pr.EQHPFFreq);
					channelSync.EQAtt = MixEngine.chData[channelLo].getParam(Channel.Pr.EQAtt);
					channelSync.EQType = MixEngine.chData[channelLo].getParam(Channel.Pr.EQType);
					channelSync.EQOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.EQOn);
					channelSync.EQ1Type = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ1Type);
					channelSync.EQ4Type = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ4Type);
					channelSync.EQ1HPFOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.EQ1HPFOn);
					channelSync.EQ4LPFOn = MixEngine.chData[channelLo].getParamBool(Channel.Pr.EQ4LPFOn);
					channelSync.EQ1Q = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ1Q);
					channelSync.EQ1F = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ1F);
					channelSync.EQ1G = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ1G);
					channelSync.EQ2Q = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ2Q);
					channelSync.EQ2F = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ2F);
					channelSync.EQ2G = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ2G);
					channelSync.EQ3Q = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ3Q);
					channelSync.EQ3F = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ3F);
					channelSync.EQ3G = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ3G);
					channelSync.EQ4Q = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ4Q);
					channelSync.EQ4F = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ4F);
					channelSync.EQ4G = MixEngine.chData[channelLo].getParam(Channel.Pr.EQ4G);
					channelSync.Dyn0On = MixEngine.chData[channelLo].getParamBool(Channel.Pr.Dyn0On);
					channelSync.Dyn0Type = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Type);
					channelSync.Dyn0Attack = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Attack);
					channelSync.Dyn0Range = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Range);
					channelSync.Dyn0Hold = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Hold);
					channelSync.Dyn0Decay = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Decay);
					channelSync.Dyn0Ratio = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Ratio);
					channelSync.Dyn0Gain = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Gain);
					channelSync.Dyn0Knee = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Knee);
					channelSync.Dyn0Threshold = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn0Threshold);
					channelSync.Dyn1On = MixEngine.chData[channelLo].getParamBool(Channel.Pr.Dyn1On);
					channelSync.Dyn1Type = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Type);
					channelSync.Dyn1Attack = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Attack);
					channelSync.Dyn1Range = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Range);
					channelSync.Dyn1Hold = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Hold);
					channelSync.Dyn1Release = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Release);
					channelSync.Dyn1Ratio = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Ratio);
					channelSync.Dyn1Gain = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Gain);
					channelSync.Dyn1Knee = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Knee);
					channelSync.Dyn1Threshold = MixEngine.chData[channelLo].getParam(Channel.Pr.Dyn1Threshold);
					channelSync.HardName = MixEngine.chData[channelLo].hardname;
					for (int l = 0; l < Console.maxMIX; l++)
					{
						channelSync.SendToMix.Add(MixEngine.chData[channelLo].getParam(Channel.Pr.SendToMix, l));
						channelSync.SendToMixOn.Add(MixEngine.chData[channelLo].getParamBool(Channel.Pr.SendToMixOn, l));
						channelSync.SendToMixPre.Add(MixEngine.chData[channelLo].getParamBool(Channel.Pr.SendToMixPre, l));
						channelSync.SendToMixPan.Add(MixEngine.chData[channelLo].getParam(Channel.Pr.SendToMixPan, l));
					}
					for (int m = 0; m < Console.maxMATRIX; m++)
					{
						channelSync.SendToMatrix.Add(MixEngine.chData[channelLo].getParam(Channel.Pr.SendToMatrix, m));
						channelSync.SendToMatrixOn.Add(MixEngine.chData[channelLo].getParamBool(Channel.Pr.SendToMatrixOn, m));
						channelSync.SendToMatrixPre.Add(MixEngine.chData[channelLo].getParamBool(Channel.Pr.SendToMatrixPre, m));
						channelSync.SendToMatrixPan.Add(MixEngine.chData[channelLo].getParam(Channel.Pr.SendToMatrixPan, m));
					}
					sendProto(channelSync, socketInfo);
					break;
				}
				case CmdParam.CMD_GEQSYNC:
				{
					GEQSync gEQSync = new GEQSync();
					int num5 = (gEQSync.GEQRack = baseMsg.command.channelLo);
					gEQSync.GEQType = MixEngine.GEQData[num5]._type;
					gEQSync.GEQBandsAvail = MixEngine.GEQData[num5].BandsAvail;
					gEQSync.GEQLink = MixEngine.GEQData[num5]._link;
					gEQSync.GEQLinkLR = MixEngine.GEQData[num5]._linkLR;
					gEQSync.GEQOn = MixEngine.GEQData[num5]._on == 1;
					for (int num6 = 0; num6 < 31; num6++)
					{
						gEQSync.GEQBand.Add(MixEngine.GEQData[num5]._Band[num6]);
					}
					sendProto(gEQSync, socketInfo);
					break;
				}
				case CmdParam.CMD_RACKSYNC:
				{
					for (int num4 = 0; num4 < 16; num4++)
					{
						RackSync rackSync = new RackSync();
						rackSync.RackSlot = num4;
						rackSync.RackSource = MixEngine.RackData[num4]._source;
						rackSync.RackType = MixEngine.RackData[num4]._type;
						sendProto(rackSync, socketInfo);
					}
					break;
				}
				case CmdParam.CMD_SYNCPARAM:
					switch (baseMsg.command.param)
					{
					case ChParam.CH_TYPE:
					{
						ChannelParam channelParam = new ChannelParam();
						channelParam.channel = baseMsg.command.channelLo;
						channelParam.param = ChParam.CH_TYPE;
						while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
						{
							channelParam.value.Add((int)MixEngine.chData[baseMsg.command.channelLo++].channelType);
						}
						sendProto(channelParam, socketInfo);
						break;
					}
					case ChParam.CH_FADER:
					{
						ChannelParam channelParam = new ChannelParam();
						channelParam.channel = baseMsg.command.channelLo;
						channelParam.param = ChParam.CH_FADER;
						while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
						{
							channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.Fader));
						}
						sendProto(channelParam, socketInfo);
						channelParam = new ChannelParam();
						channelParam.channel = 0;
						channelParam.param = ChParam.CH_NONE;
						channelParam.value.Add(0);
						sendProto(channelParam, socketInfo);
						break;
					}
					case ChParam.CH_ON:
					{
						ChannelParam channelParam = new ChannelParam();
						channelParam.channel = baseMsg.command.channelLo;
						channelParam.param = ChParam.CH_ON;
						while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
						{
							channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.On));
						}
						sendProto(channelParam, socketInfo);
						channelParam = new ChannelParam();
						channelParam.channel = 0;
						channelParam.param = ChParam.CH_NONE;
						channelParam.value.Add(0);
						sendProto(channelParam, socketInfo);
						break;
					}
					case ChParam.CH_CUE:
					{
						ChannelParam channelParam = new ChannelParam();
						channelParam.channel = baseMsg.command.channelLo;
						channelParam.param = ChParam.CH_CUE;
						while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
						{
							channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.Cue));
						}
						sendProto(channelParam, socketInfo);
						break;
					}
					case ChParam.CH_NAME:
					{
						ChannelParam channelParam = new ChannelParam();
						channelParam.channel = baseMsg.command.channelLo;
						channelParam.param = ChParam.CH_NAME;
						while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
						{
							channelParam.valuestr.Add(MixEngine.chData[baseMsg.command.channelLo++].getParamStr(Channel.Pr.Name));
						}
						sendProto(channelParam, socketInfo);
						channelParam = new ChannelParam();
						channelParam.channel = 0;
						channelParam.param = ChParam.CH_NONE;
						channelParam.value.Add(0);
						sendProto(channelParam, socketInfo);
						break;
					}
					case ChParam.CH_SENDTOMIX:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 16)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMIX;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMix, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMIXON:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 16)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMIXON;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMixOn, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMIXPRE:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 16)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMIXPRE;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMixPre, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMIXPAN:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 16)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMIXPAN;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMixPan, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMATRIX:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 8)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMATRIX;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMatrix, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMATRIXON:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 8)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMATRIXON;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMatrixOn, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMATRIXPRE:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 8)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMATRIXPRE;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMatrixPre, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					case ChParam.CH_SENDTOMATRIXPAN:
						if (baseMsg.command.indexLo >= 0 && baseMsg.command.indexLo < 8)
						{
							ChannelParam channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.index = baseMsg.command.indexLo;
							channelParam.param = ChParam.CH_SENDTOMATRIXPAN;
							while (baseMsg.command.channelLo <= baseMsg.command.channelHi)
							{
								channelParam.value.Add(MixEngine.chData[baseMsg.command.channelLo++].getParam(Channel.Pr.SendToMatrixPan, baseMsg.command.indexLo));
							}
							sendProto(channelParam, socketInfo);
							channelParam = new ChannelParam();
							channelParam.channel = 0;
							channelParam.param = ChParam.CH_NONE;
							channelParam.value.Add(0);
							sendProto(channelParam, socketInfo);
						}
						break;
					}
					break;
				case CmdParam.CMD_SYNCCHANNEL:
				{
					ChannelParam channelParam = new ChannelParam();
					channelParam.channel = baseMsg.command.channelLo;
					channelParam.param = ChParam.CH_NAME;
					for (int i = baseMsg.command.channelLo; i <= baseMsg.command.channelHi; i++)
					{
						channelParam.valuestr.Add(MixEngine.chData[i].getParamStr(Channel.Pr.Name));
					}
					sendProto(channelParam, socketInfo);
					List<ChParam> list = new List<ChParam>();
					list.Add(ChParam.CH_HA);
					list.Add(ChParam.CH_PAN);
					list.Add(ChParam.CH_FADER);
					list.Add(ChParam.CH_ON);
					list.Add(ChParam.CH_EQON);
					list.Add(ChParam.CH_EQ1HPFON);
					list.Add(ChParam.CH_EQ1TYPE);
					list.Add(ChParam.CH_EQ4LPFON);
					list.Add(ChParam.CH_EQ4TYPE);
					list.Add(ChParam.CH_EQHPFFREQ);
					list.Add(ChParam.CH_EQHPFON);
					list.Add(ChParam.CH_EQ1G);
					list.Add(ChParam.CH_EQ1F);
					list.Add(ChParam.CH_EQ1Q);
					list.Add(ChParam.CH_EQ2G);
					list.Add(ChParam.CH_EQ2F);
					list.Add(ChParam.CH_EQ2Q);
					list.Add(ChParam.CH_EQ3G);
					list.Add(ChParam.CH_EQ3F);
					list.Add(ChParam.CH_EQ3Q);
					list.Add(ChParam.CH_EQ4G);
					list.Add(ChParam.CH_EQ4F);
					list.Add(ChParam.CH_EQ4Q);
					foreach (ChParam item in list)
					{
						if (speakToParam.ContainsKey(item))
						{
							channelParam = new ChannelParam();
							channelParam.channel = baseMsg.command.channelLo;
							channelParam.param = item;
							for (int j = baseMsg.command.channelLo; j <= baseMsg.command.channelHi; j++)
							{
								channelParam.value.Add(MixEngine.chData[j].getParam(speakToParam[item]));
							}
							sendProto(channelParam, socketInfo);
						}
					}
					channelParam = new ChannelParam();
					channelParam.channel = 0;
					channelParam.param = ChParam.CH_NONE;
					channelParam.value.Add(0);
					sendProto(channelParam, socketInfo);
					break;
				}
				}
			}
			if (baseMsg.channelParam != null)
			{
				if (speakToParam.ContainsKey(baseMsg.channelParam.param))
				{
					MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, speakToParam[baseMsg.channelParam.param], 0, baseMsg.channelParam.value[0]);
				}
				else
				{
					switch (baseMsg.channelParam.param)
					{
					case ChParam.CH_CUE:
						MixEngine.setParam(socketInfo.sourcetag, baseMsg.channelParam.channel, MixEngine.Pr.Cue, 0, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_NAME:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.Name, 0, baseMsg.channelParam.valuestr[0]);
						break;
					case ChParam.CH_SENDTOMIX:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMix, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMIXPRE:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMixPre, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMIXPAN:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMixPan, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMIXON:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMixOn, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMATRIX:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMatrix, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMATRIXPRE:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMatrixPre, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMATRIXPAN:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMatrixPan, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_SENDTOMATRIXON:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.SendToMatrixOn, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_MUTEGROUP:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.MuteGroup, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					case ChParam.CH_DCAGROUP:
						MixEngine.chData[baseMsg.channelParam.channel].setParam(socketInfo.sourcetag, Channel.Pr.DCAGroup, baseMsg.channelParam.index, baseMsg.channelParam.value[0]);
						break;
					}
				}
			}
			if (baseMsg.geqSetParam != null)
			{
				switch (baseMsg.geqSetParam.param)
				{
				case GEQParam.GEQ_BAND:
					MixEngine.setParam(socketInfo.sourcetag, baseMsg.geqSetParam.rack, MixEngine.Pr.GEQBand, baseMsg.geqSetParam.index, baseMsg.geqSetParam.value[0]);
					break;
				case GEQParam.GEQ_ON:
					MixEngine.setParam(socketInfo.sourcetag, baseMsg.geqSetParam.rack, MixEngine.Pr.GEQOn, 0, baseMsg.geqSetParam.value[0]);
					break;
				case GEQParam.GEQ_LINK:
					MixEngine.setParam(socketInfo.sourcetag, baseMsg.geqSetParam.rack, MixEngine.Pr.GEQLink, 0, baseMsg.geqSetParam.value[0]);
					break;
				case GEQParam.GEQ_LINKLR:
					MixEngine.setParam(socketInfo.sourcetag, baseMsg.geqSetParam.rack, MixEngine.Pr.GEQLinkLR, 0, baseMsg.geqSetParam.value[0]);
					break;
				case GEQParam.GEQ_TYPE:
					MixEngine.setParam(socketInfo.sourcetag, baseMsg.geqSetParam.rack, MixEngine.Pr.GEQType, 0, baseMsg.geqSetParam.value[0]);
					break;
				}
			}
			if (baseMsg.generalParam == null)
			{
				return;
			}
			switch (baseMsg.generalParam.param)
			{
			case GenParam.GEN_MUTEMASTER:
				MixEngine.setParam(socketInfo.sourcetag, baseMsg.generalParam.index, MixEngine.Pr.MuteMaster, 0, baseMsg.generalParam.value[0]);
				break;
			case GenParam.GEN_SELCHANNEL:
				MixEngine.setParam("SelChannel", baseMsg.generalParam.value[0], 1);
				Console.selChannel = baseMsg.generalParam.value[0];
				break;
			case GenParam.GEN_METERS:
				socketInfo.useMeters = baseMsg.generalParam.value[0] > 0;
				break;
			case GenParam.GEN_OUT_COUNT:
				if (baseMsg.generalParam.request)
				{
					GeneralParam generalParam2 = new GeneralParam();
					generalParam2.index = 0;
					generalParam2.subindex = 0;
					generalParam2.param = GenParam.GEN_OUT_COUNT;
					generalParam2.value.Add(Console.getOutportCount());
					sendProto(generalParam2, socketInfo);
				}
				break;
			case GenParam.GEN_OUT_ALL:
				if (baseMsg.generalParam.request)
				{
					GeneralParam generalParam3 = new GeneralParam();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_COUNT;
					generalParam3.value.Add(Console.getOutportCount());
					sendProto(generalParam3, socketInfo);
					generalParam3.value.Clear();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_NAME;
					for (int num7 = 0; num7 < Console.getOutportCount(); num7++)
					{
						generalParam3.valuestr.Add(MixEngine.OutPort[num7].hardname);
					}
					sendProto(generalParam3, socketInfo);
					generalParam3.valuestr.Clear();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_ATT;
					for (int num8 = 0; num8 < Console.getOutportCount(); num8++)
					{
						generalParam3.value.Add(MixEngine.OutPort[num8].getParam(OutPort.Pr.Attenuation));
					}
					sendProto(generalParam3, socketInfo);
					generalParam3.value.Clear();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_DELAYON;
					for (int num9 = 0; num9 < Console.getOutportCount(); num9++)
					{
						generalParam3.value.Add(MixEngine.OutPort[num9].getParam(OutPort.Pr.DelayOn));
					}
					sendProto(generalParam3, socketInfo);
					generalParam3.value.Clear();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_DELAYTIME;
					for (int num10 = 0; num10 < Console.getOutportCount(); num10++)
					{
						generalParam3.value.Add(MixEngine.OutPort[num10].getParam(OutPort.Pr.DelayTime));
					}
					sendProto(generalParam3, socketInfo);
					generalParam3.value.Clear();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_PHASE;
					for (int num11 = 0; num11 < Console.getOutportCount(); num11++)
					{
						generalParam3.value.Add(MixEngine.OutPort[num11].getParam(OutPort.Pr.Phase));
					}
					sendProto(generalParam3, socketInfo);
					generalParam3.value.Clear();
					generalParam3.index = 0;
					generalParam3.subindex = 0;
					generalParam3.param = GenParam.GEN_OUT_ALL;
					generalParam3.value.Add(1);
					sendProto(generalParam3, socketInfo);
					generalParam3.value.Clear();
				}
				break;
			case GenParam.GEN_OUT_ATT:
				if (!baseMsg.generalParam.request)
				{
					MixEngine.setOutPortParam(socketInfo.sourcetag, OutPort.Pr.Attenuation, baseMsg.generalParam.index, baseMsg.generalParam.value[0]);
				}
				break;
			case GenParam.GEN_OUT_DELAYON:
				if (!baseMsg.generalParam.request)
				{
					MixEngine.setOutPortParam(socketInfo.sourcetag, OutPort.Pr.DelayOn, baseMsg.generalParam.index, baseMsg.generalParam.value[0]);
				}
				break;
			case GenParam.GEN_OUT_DELAYTIME:
				if (!baseMsg.generalParam.request)
				{
					MixEngine.setOutPortParam(socketInfo.sourcetag, OutPort.Pr.DelayTime, baseMsg.generalParam.index, baseMsg.generalParam.value[0]);
				}
				break;
			case GenParam.GEN_OUT_PHASE:
				if (!baseMsg.generalParam.request)
				{
					MixEngine.setOutPortParam(socketInfo.sourcetag, OutPort.Pr.Phase, baseMsg.generalParam.index, baseMsg.generalParam.value[0]);
				}
				break;
			case (GenParam)4:
			case GenParam.GEN_OUT_NONE:
			case GenParam.GEN_OUT_NAME:
				break;
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("FaderService", "Server", "faderService.processProc() Error: " + ex.Message);
		}
	}

	public void sendProto(string sourcetag, ChannelParam chParam)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket == null)
					{
						continue;
					}
					switch (chParam.param)
					{
					case ChParam.CH_METERPRE:
					case ChParam.CH_METERDYN0IN:
					case ChParam.CH_METERDYN1IN:
					case ChParam.CH_METERDYN0GR:
					case ChParam.CH_METERDYN1GR:
					case ChParam.CH_METEREQIN:
					case ChParam.CH_METEREQOUT:
					case ChParam.CH_METERPOST:
						if (workerSocket.clientVersion != null && (workerSocket.clientVersion.CompareTo("20141231") > 0 || chParam.param == ChParam.CH_METERPRE) && workerSocket.useMeters && sourcetag != workerSocket.sourcetag)
						{
							sendProto(chParam, workerSocket);
						}
						break;
					default:
						if (sourcetag != workerSocket.sourcetag)
						{
							sendProto(chParam, workerSocket);
						}
						break;
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error A: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, GEQSetParam chParam)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(chParam, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error A: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, GeneralParam chParam)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(chParam, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error A: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, RackSync rackParam)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(rackParam, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error RackSync: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto RackSync Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, GEQSync chParam)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(chParam, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error A: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, GenMessage genParam)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(genParam, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error B: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, BaseMessage baseMsg)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(baseMsg, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error BaseMsg: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto(BaseMsg) Error: " + ex2.Message);
		}
	}

	public void sendProto(string sourcetag, SystemStatus sysStatus)
	{
		try
		{
			lock (workerSockets)
			{
				foreach (SocketPacket workerSocket in workerSockets)
				{
					if (workerSocket != null && sourcetag != workerSocket.sourcetag)
					{
						sendProto(sysStatus, workerSocket);
					}
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error BaseMsg: " + ex.Message);
		}
		catch (Exception ex2)
		{
			MixEngine.logDebug("sendProto(SysStatus) Error: " + ex2.Message);
		}
	}

	public void sendProto(ChannelParam chParam, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.channelParam = chParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error C: " + ex.Message);
		}
	}

	public void sendProto(GEQSetParam mxParam, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.geqSetParam = mxParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error mxParam: " + ex.Message);
		}
	}

	public void sendProto(GEQSync mxParam, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.geqSync = mxParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error mxParam: " + ex.Message);
		}
	}

	public void sendProto(RackSync rackParam, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.rackSync = rackParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error rackParam: " + ex.Message);
		}
	}

	public void sendProto(SystemStatus sysStatus, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.sysStatus = sysStatus;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error D: " + ex.Message);
		}
	}

	public void sendProto(GenMessage genMsg, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.genMessage = genMsg;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error E: " + ex.Message);
		}
	}

	public void sendProto(UserRecord userMsg, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.userRecord = userMsg;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error F: " + ex.Message);
		}
	}

	public void sendProto(BaseMessage msgBase, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					msgBase.sessionID = destInfo.sessionID;
					Serializer.SerializeWithLengthPrefix(destination, msgBase, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error G: " + ex.Message);
		}
	}

	public void sendProto(BusSync chParam, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.busSync = chParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error H: " + ex.Message);
		}
	}

	public void sendProto(ChannelSync chParam, SocketPacket destInfo)
	{
		try
		{
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.channelSync = chParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error J: " + ex.Message);
		}
	}

	public void sendProto(GeneralParam genParam, SocketPacket destInfo)
	{
		try
		{
			MixEngine.logDebug("sendProto() genParam");
			if (destInfo.currentSocket != null && destInfo.currentSocket.Connected)
			{
				using (NetworkStream destination = new NetworkStream(destInfo.currentSocket))
				{
					BaseMessage baseMessage = new BaseMessage();
					baseMessage.sessionID = destInfo.sessionID;
					baseMessage.generalParam = genParam;
					Serializer.SerializeWithLengthPrefix(destination, baseMessage, PrefixStyle.Fixed32BigEndian);
					return;
				}
			}
		}
		catch (IOException ex)
		{
			MixEngine.logDebug("IO Error K: " + ex.Message);
		}
	}

	private void sendToClient(Socket destSocket, string msg)
	{
		try
		{
			byte[] bytes = Encoding.ASCII.GetBytes(msg);
			destSocket.Send(bytes);
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("sendToClient() Error: " + ex.Message);
		}
	}

	private string GetIP()
	{
		try
		{
			string hostName = Dns.GetHostName();
			IPHostEntry hostByName = Dns.GetHostByName(hostName);
			string result = "";
			IPAddress[] addressList = hostByName.AddressList;
			int num = 0;
			if (num < addressList.Length)
			{
				IPAddress iPAddress = addressList[num];
				return iPAddress.ToString();
			}
			return result;
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("GetIP() Error: " + ex.Message);
		}
		return "";
	}

	private void ButtonCloseClick(object sender, EventArgs e)
	{
		CloseSockets();
	}

	private void CloseSockets()
	{
		try
		{
			if (mainSocket != null)
			{
				mainSocket.Close();
			}
			Socket socket = null;
			for (int i = 0; i < workerSockets.Count; i++)
			{
				socket = workerSockets[i].currentSocket;
				if (socket != null)
				{
					socket.Close();
					socket = null;
				}
			}
			workerSockets.Clear();
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("CloseSockets() Error: " + ex.Message);
		}
	}

	public string GetMd5Hash(MD5 md5Hash, string input)
	{
		byte[] array = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	public bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
	{
		try
		{
			string md5Hash2 = GetMd5Hash(md5Hash, input);
			StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
			if (ordinalIgnoreCase.Compare(md5Hash2, hash) == 0)
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			MixEngine.logDebug("VerifyMd5Hash Error: " + ex.Message);
		}
		return false;
	}
}
