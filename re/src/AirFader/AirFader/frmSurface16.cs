using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;
using Sanford.Multimedia;

namespace AirFader;

public class frmSurface16 : Form
{
	public enum LayerModes
	{
		FOH,
		Master,
		Custom,
		OutportDelay,
		OutportAtt,
		DCA
	}

	public enum FaderModes
	{
		FOH,
		SOF,
		MOF,
		HA,
		Pan,
		DCA,
		OutportDelay,
		OutportAtt
	}

	private delegate void EngineChangedCB(MixEngine.Pr param, int rack, int paramIndex, int value);

	private delegate void EFXParamChangedCB(int objectIndex, string param, int paramIndex);

	private delegate void ChannelChangedCB(Channel.Pr param, int channel, int paramIndex, int value);

	private delegate void ActionCallback(string property, int index);

	private delegate void newMessageCallback(string strMessage, int errorCode);

	private delegate void syncUDFStateD();

	private delegate void BlinkCallback(int efx, bool isBlink, bool isCounting);

	private const int WM_PAINT = 15;

	private const int WM_SETREDRAW = 11;

	private decimal[] dbMap = new decimal[1024]
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

	private int[] offset1 = new int[256]
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

	private int offsetFOH;

	private int offsetCustom;

	private int offsetMaster;

	private int offsetOutport;

	private int offsetDCA;

	public bool surfaceChanged;

	public bool surfaceLocked;

	public bool showMeter;

	public bool fineMode;

	public bool syncMode;

	public bool isDebug = true;

	public bool isGlobalShutdown;

	private Random r = new Random();

	private WindowStatic thisWin = new WindowStatic();

	private Label consoleAlert = new Label();

	private AirButton btnPageHome = new AirButton();

	private AirButton btnPageSOF = new AirButton();

	private AirButton btnPageMOF = new AirButton();

	private AirButton btnPageMaster = new AirButton();

	private AirButton btnPageCustom = new AirButton();

	private AirButton btnPageDCA = new AirButton();

	private AirButton btnPageUDF = new AirButton();

	private AirButton btnMeter = new AirButton();

	private AirButton btnGEQ = new AirButton();

	private AirButton btnPageHA = new AirButton();

	private AirButton btnPagePan = new AirButton();

	private AirButton btnPageOutPortAtt = new AirButton();

	private AirButton btnPageOutPortDelay = new AirButton();

	private List<frmChannel> channelForms = new List<frmChannel>();

	public LayerModes layerMode;

	public FaderModes faderMode;

	public string UDFPage = "";

	public int SOFPage;

	public int MOFPage;

	private string MachineID = "";

	private char[] splitChar = new char[1] { ':' };

	private int silentTicks;

	public Form frmConfigH;

	private frmChannel frmChannelH;

	private frmScene frmSceneH;

	public bool[] muteMasters = new bool[8];

	public List<int> layerFOH = new List<int>
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
		10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
		20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
		30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
		40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
		50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
		60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
		70, 71
	};

	public List<int> layerMaster = new List<int>
	{
		72, 73, 74, 75, 76, 77, 78, 79, 80, 81,
		82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
		92, 93, 94, 95, 98, -1, 96, 97
	};

	public List<int> layerCustom = new List<int> { 0, 1, 64, 65, 72, 96, 97, 98 };

	public List<int> masterSection = new List<int> { 96 };

	public List<string> arrLayout = new List<string> { "Label", "Mute", "Fader" };

	private Dictionary<string, decimal> arrLayoutHeight = new Dictionary<string, decimal>
	{
		{ "Label", 1.2m },
		{ "Cue", 0.8m },
		{ "Pan", 2.0m },
		{ "Phantom", 0.8m },
		{ "HA", 2.0m },
		{ "Name", 1.0m },
		{ "Fader", 10.0m },
		{ "Mute", 1.0m },
		{ "Info", 1.0m }
	};

	private Skin theme = new Skin();

	private PowerStatus power = SystemInformation.PowerStatus;

	private List<AirControls.AirFader> arrFaders = new List<AirControls.AirFader>();

	private List<AirButton> arrLabels = new List<AirButton>();

	private List<TextBox> arrNames = new List<TextBox>();

	private List<AirButton> arrCues = new List<AirButton>();

	private List<AirButton> arrMutes = new List<AirButton>();

	private List<AirButton> arrPhantom = new List<AirButton>();

	private List<AirKnob> arrPans = new List<AirKnob>();

	private List<AirKnob> arrHAs = new List<AirKnob>();

	private List<AirPanel> arrHilights = new List<AirPanel>();

	private List<Label> arrInfo = new List<Label>();

	private Console consoleInfo;

	private List<AirButton> arrBtnUDF = new List<AirButton>(16);

	private List<AirButton> arrBtnMM = new List<AirButton>(8);

	private System.Threading.Timer threadTimer;

	private System.Threading.Timer timeoutTimer;

	private Stopwatch animationTimer = new Stopwatch();

	private int[] tapCount = new int[4];

	private long[] tapStart = new long[4];

	private long[] tapLast = new long[4];

	private long[] tapAlign = new long[4];

	private bool[] tapBlink = new bool[4];

	private bool bAllowPaint = true;

	private Rectangle m_Bounds;

	private int clockTicks = 29;

	private int meterTicks;

	private int meterValue = 50;

	private int meterInc = 1;

	private int resizeInProgress;

	private int lastWidth;

	private int lastHeight;

	private bool isResizing;

	private string[] buttonsHome = new string[16]
	{
		"Tools:tools", "Rack:rack", "Meters:meters", "Sync:sync", "Load\nScene:loadscene", "Save\nScene:savescene", "Prev\nScene:prevscene", "Next\nScene:nextscene", "Mute 1:mutegroup:0", "Mute 2:mutegroup:1",
		"Mute 3:mutegroup:2", "Mute 4:mutegroup:3", "Mute 5:mutegroup:4", "Mute 6:mutegroup:5", "Mute 7:mutegroup:6", "Mute 8:mutegroup:7"
	};

	private string[] buttonsTools = new string[16]
	{
		"HA:ha", "Pan:pan", "Output\nAtt:outportatt", "Output\nDelay:outportdelay", "Lamp:lamp", "Sync:sync", "", "", "", "",
		"", "", "", "", "", "Bounce:bounce"
	};

	private int offsetLen = 19;

	private IContainer components;

	private AirButton btnConnect;

	private Label lblDebug;

	private TextBox txtRAW;

	private AirButton btnFine;

	private AirButton btnConfig;

	private AirButton btnFullScreen;

	private Label lblClock;

	private System.Windows.Forms.Timer timerClock;

	private Label lblPanelHome;

	private AirButton btnLock;

	private Label lblParamPage;

	private Label lblBrand;

	private AirPanel panelSends;

	private AirPanel panelConsole;

	private AirPanel panelHome;

	private AirPanel panelLayer;

	private Panel panelFaders;

	private AirPanel panelTopR;

	private Label lblConnStatus;

	private AirPanel panelUDF;

	private Label label3;

	private AirMeter meterBattery;

	private Label lblLocked;

	private Label lblSceneName;

	private ListBox lbLog;

	private AirPanel panelLCD;

	private Label label4;

	private Label label5;

	private BackgroundWorker backgroundWorker1;

	public static void Suspend(Control control)
	{
		Message m = Message.Create(control.Handle, 11, IntPtr.Zero, IntPtr.Zero);
		NativeWindow nativeWindow = NativeWindow.FromHandle(control.Handle);
		nativeWindow.DefWndProc(ref m);
	}

	public static void Resume(Control control)
	{
		Message m = Message.Create(wparam: new IntPtr(1), hWnd: control.Handle, msg: 11, lparam: IntPtr.Zero);
		NativeWindow nativeWindow = NativeWindow.FromHandle(control.Handle);
		nativeWindow.DefWndProc(ref m);
		control.Invalidate(invalidateChildren: true);
	}

	public frmSurface16()
	{
		try
		{
			Process currentProcess = Process.GetCurrentProcess();
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				if (process.ProcessName.ToLower().Contains("01vmix.exe") && process.Id != currentProcess.Id && frmConfirm.askQuestion("It looks like 01vmix is already running,\nor didn't exit cleanly last time.\nShould I fix it?", "Terminate old program?", this) == DialogResult.Yes)
				{
					process.Kill();
				}
			}
		}
		catch (Exception)
		{
		}
		try
		{
			if (Settings.Default.Upgraded)
			{
				Settings.Default.Upgrade();
				Settings.Default.Upgraded = false;
				if (Settings.Default.Version != Application.ProductVersion)
				{
					Settings.Default.Expired = false;
					Settings.Default.Version = Application.ProductVersion;
				}
				Settings.Default.Save();
			}
			if (isDebug)
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				folderPath += "\\01vmix";
				try
				{
					if (Directory.Exists(folderPath) && File.Exists(folderPath + "\\Error.Log"))
					{
						File.Delete(folderPath + "\\Error.Log");
					}
				}
				catch (IOException)
				{
				}
			}
			if (isDebug)
			{
				logDebug("Initializing Components");
			}
			InitializeComponent();
			MixEngine.setConsoleType(Settings.Default.ConsoleType);
			MixEngine.hMainWindow = this;
			if (isDebug)
			{
				logDebug("License User: " + MixEngine.getLicenseUser());
			}
			if (isDebug)
			{
				logDebug("License Key : " + MixEngine.getLicenseKey());
			}
			if (isDebug)
			{
				logDebug("Loading Channel Strip Layout");
			}
			arrLayout.Clear();
			string[] array = Settings.Default.ChannelStrip.Split(',');
			foreach (string item in array)
			{
				arrLayout.Add(item);
			}
			if (isDebug)
			{
				logDebug("Loading Custom Fader Layer");
			}
			layerCustom.Clear();
			string[] array2 = Settings.Default.CustomLayer.Split(',');
			foreach (string s in array2)
			{
				int result = 0;
				if (int.TryParse(s, out result))
				{
					layerCustom.Add(result);
				}
			}
			if (isDebug)
			{
				logDebug("Loading Primary Layer Box");
			}
			btnPageHome.BackColor = Settings.Default.c_button_active;
			btnPageHome.ForeColor = Settings.Default.c_button_active_fore;
			btnPageHome.ParentColor = panelConsole.BackColor;
			btnPageHome.ForeColor = Color.Black;
			btnPageHome.Text = "Home";
			btnPageHome.Click += btnPageHome_Click;
			btnPageHome.Tag = "Home";
			panelConsole.Controls.Add(btnPageHome);
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageSOF.ParentColor = panelConsole.BackColor;
			btnPageSOF.ForeColor = Color.Black;
			btnPageSOF.Text = "Sends";
			btnPageSOF.BackText = "On Faders";
			btnPageSOF.Click += btnPageSOF_Click;
			btnPageSOF.Tag = "SOF";
			panelConsole.Controls.Add(btnPageSOF);
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.ParentColor = panelConsole.BackColor;
			btnPageMOF.ForeColor = Color.Black;
			btnPageMOF.Text = "Matrix";
			btnPageMOF.BackText = "On Faders";
			btnPageMOF.Click += btnPageMOF_Click;
			btnPageMOF.Tag = "MOF";
			panelConsole.Controls.Add(btnPageMOF);
			btnPageMaster.BackColor = Settings.Default.c_button_inactive;
			btnPageMaster.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMaster.ParentColor = panelConsole.BackColor;
			btnPageMaster.ForeColor = Color.Black;
			btnPageMaster.Text = "Master";
			btnPageMaster.Tag = "Master";
			btnPageMaster.Click += btnPageMaster_Click;
			panelConsole.Controls.Add(btnPageMaster);
			btnPageCustom.BackColor = Settings.Default.c_button_inactive;
			btnPageCustom.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageCustom.ParentColor = panelConsole.BackColor;
			btnPageCustom.ForeColor = Color.Black;
			btnPageCustom.Text = "Custom";
			btnPageCustom.Tag = "Custom";
			btnPageCustom.Click += btnPageCustom_Click;
			panelConsole.Controls.Add(btnPageCustom);
			btnPageDCA.BackColor = Settings.Default.c_button_inactive;
			btnPageDCA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageDCA.ParentColor = panelConsole.BackColor;
			btnPageDCA.ForeColor = Color.Black;
			btnPageDCA.Text = "DCA";
			btnPageDCA.Tag = "DCA";
			btnPageDCA.Click += btnPageDCA_Click;
			panelConsole.Controls.Add(btnPageDCA);
			consoleInfo = new Console();
			if (isDebug)
			{
				logDebug("Creating Sidebar Keys");
			}
			for (int l = 0; l < 16; l++)
			{
				AirButton airButton = new AirButton();
				airButton.BackColor = Settings.Default.c_button_inactive;
				airButton.ForeColor = Settings.Default.c_button_inactive_fore;
				airButton.ParentColor = panelHome.BackColor;
				airButton.Click += btnUDF_Click;
				airButton.Tag = "";
				arrBtnUDF.Add(airButton);
				panelHome.Controls.Add(airButton);
			}
			thisWin.Parse(Settings.Default.windowMain);
			thisWin.SetWindow(this);
			if (isDebug)
			{
				logDebug("Calling NumFadersChange from frmSurface16()");
			}
			numFaders_Change();
			if (isDebug)
			{
				logDebug("Resetting Surface to Home Layer");
			}
			btnPageHome_Click(null, EventArgs.Empty);
			if (isDebug)
			{
				logDebug("Subscribing to Channel Events");
			}
			MixEngine.ChannelChanged += MixEngine_ChannelChanged;
			MixEngine.EngineChanged += MixEngine_EngineChanged;
			MixEngine.EFXParamChanged += MixEngine_EFXParamChanged;
			for (int m = 0; m < 4; m++)
			{
				tapBlink[m] = false;
				tapStart[m] = 0L;
				tapLast[m] = 0L;
				tapAlign[m] = 0L;
				tapCount[m] = 0;
			}
			animationTimer.Reset();
			animationTimer.Start();
			MixEngine_Action("MasterSection", 0);
			if (isDebug)
			{
				logDebug("Subscribing to MixEngine Events");
			}
			MixEngine.NewMessage += MixEngine_NewMessage;
			MixEngine.Action += MixEngine_Action;
			MixEngine.chData[MixEngine.chOffsetMix + 4].setParam("UI", Channel.Pr.Pair, 0, 1);
			MixEngine.chData[MixEngine.chOffsetMix + 6].setParam("UI", Channel.Pr.Pair, 0, 1);
			MixEngine.chData[64].setParam("console", Channel.Pr.Group, 0, 35);
			MixEngine.chData[65].setParam("console", Channel.Pr.Group, 0, 35);
			MixEngine.chData[66].setParam("console", Channel.Pr.Group, 0, 36);
			MixEngine.chData[67].setParam("console", Channel.Pr.Group, 0, 36);
			MixEngine.chData[68].setParam("console", Channel.Pr.Group, 0, 37);
			MixEngine.chData[69].setParam("console", Channel.Pr.Group, 0, 37);
			MixEngine.chData[70].setParam("console", Channel.Pr.Group, 0, 38);
			MixEngine.chData[71].setParam("console", Channel.Pr.Group, 0, 38);
			MixEngine.setParam("UI", 0, MixEngine.Pr.RackSource, 0, 151);
			MixEngine.setParam("UI", 1, MixEngine.Pr.RackSource, 0, 152);
			MixEngine.setParam("UI", 2, MixEngine.Pr.RackSource, 0, 127);
			MixEngine.setParam("UI", 3, MixEngine.Pr.RackSource, 0, 0);
			base.Controls.Add(consoleAlert);
			if (isDebug)
			{
				logDebug("Creating First Channel View Window");
			}
			frmChannelH = new frmChannel(this);
			if (Settings.Default.ConsoleFollow)
			{
				frmChannelH.consoleLink = true;
			}
			if (Settings.Default.serverAutoStart && Settings.Default.serverPort != 0)
			{
				if (isDebug)
				{
					logDebug("Starting FaderService...");
				}
				MixEngine.faderServer.start(IPAddress.Any, Settings.Default.serverPort);
				if (isDebug)
				{
					logDebug("FaderService listening on port #" + Settings.Default.serverPort + ".");
				}
			}
		}
		catch (Exception ex3)
		{
			logError("Form16()", "UI", ex3.ToString());
		}
	}

	private void MixEngine_EngineChanged(MixEngine.Pr param, int rack, int paramIndex, int value)
	{
		if (lblDebug.InvokeRequired)
		{
			EngineChangedCB method = MixEngine_EngineChanged;
			BeginInvoke(method, param, rack, paramIndex, value);
			return;
		}
		try
		{
			bool flag = false;
			switch (param)
			{
			case MixEngine.Pr.None:
				updateUI();
				syncUDFState();
				break;
			case MixEngine.Pr.MuteMaster:
			{
				if (faderMode != 0 && faderMode != FaderModes.Pan && faderMode != FaderModes.HA)
				{
					break;
				}
				for (int l = 0; l < arrFaders.Count; l++)
				{
					if (arrFaders[l].Channel < 0 || arrFaders[l].Channel >= MixEngine.chData.Length)
					{
						continue;
					}
					flag = false;
					for (int m = 0; m < 8; m++)
					{
						if (MixEngine.muteGroup[m].members.Contains(arrFaders[l].Channel) && MixEngine.getParam(m, MixEngine.Pr.MuteMaster, 0) == 1)
						{
							flag = true;
						}
					}
					setMuteColor(arrMutes[l], MixEngine.chData[arrFaders[l].Channel].getParamBool(Channel.Pr.On), flag);
				}
				syncUDFState();
				break;
			}
			case MixEngine.Pr.SceneName:
			case MixEngine.Pr.SceneLoad:
			case MixEngine.Pr.SceneSave:
				if (rack == Console.sceneNum)
				{
					lblSceneName.Text = Console.sceneNum.ToString("000") + ": " + Console.sceneName;
				}
				break;
			case MixEngine.Pr.GEQType:
			case MixEngine.Pr.RackType:
			case MixEngine.Pr.RackSource:
				if (UDFPage == "rack")
				{
					execUDF("rack");
				}
				break;
			case MixEngine.Pr.HA:
			{
				if (faderMode == FaderModes.HA)
				{
					for (int j = 0; j < arrFaders.Count - masterSection.Count; j++)
					{
						if (arrFaders[j].Channel >= 0 && arrFaders[j].Channel < MixEngine.chData.Length && MixEngine.chData[arrFaders[j].Channel].getParam(Channel.Pr.PatchIn) - 1 == rack)
						{
							arrFaders[j].Value = value;
						}
					}
				}
				for (int k = 0; k < arrFaders.Count - masterSection.Count; k++)
				{
					if (arrFaders[k].Channel >= 0 && arrFaders[k].Channel < MixEngine.chData.Length && MixEngine.chData[arrFaders[k].Channel].getParam(Channel.Pr.PatchIn) - 1 == rack)
					{
						arrHAs[k].Value = MixEngine.chData[arrFaders[k].Channel].getParam(Channel.Pr.HA);
					}
				}
				break;
			}
			case MixEngine.Pr.Phantom:
			{
				if (layerMode != LayerModes.Custom && layerMode != 0)
				{
					break;
				}
				for (int i = 0; i < arrFaders.Count; i++)
				{
					if (arrFaders[i].Channel >= 0 && arrFaders[i].Channel < MixEngine.chData.Length && MixEngine.chData[arrFaders[i].Channel].getParam(Channel.Pr.PatchIn) - 1 == rack)
					{
						if (value == 1)
						{
							arrPhantom[i].BackColor = Settings.Default.c_button_active;
							arrPhantom[i].ForeColor = Settings.Default.c_button_active_fore;
						}
						else
						{
							arrPhantom[i].BackColor = Settings.Default.c_button_inactive;
							arrPhantom[i].ForeColor = Settings.Default.c_button_inactive_fore;
						}
					}
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			logError("EngineChanged()", "UI", ex.ToString());
		}
	}

	private void MixEngine_EFXParamChanged(int objectIndex, string param, int paramIndex)
	{
		if (lblDebug.InvokeRequired)
		{
			EFXParamChangedCB method = MixEngine_EFXParamChanged;
			BeginInvoke(method, objectIndex, param, paramIndex);
			return;
		}
		try
		{
			if (param == "BPM")
			{
				tapAlign[objectIndex] = animationTimer.ElapsedTicks;
				syncUDFState();
			}
		}
		catch (Exception ex)
		{
			logError("EFXChanged()", "UI", ex.ToString());
		}
	}

	private void MixEngine_ChannelChanged(Channel.Pr param, int channel, int paramIndex, int value)
	{
		if (lblDebug.InvokeRequired)
		{
			ChannelChangedCB method = MixEngine_ChannelChanged;
			BeginInvoke(method, param, channel, paramIndex, value);
			return;
		}
		try
		{
			switch (param)
			{
			case Channel.Pr.MeterPre:
			case Channel.Pr.MeterPost:
			{
				if (!showMeter || faderMode == FaderModes.DCA || faderMode == FaderModes.OutportAtt || faderMode == FaderModes.OutportDelay)
				{
					break;
				}
				for (int num13 = 0; num13 < arrFaders.Count; num13++)
				{
					if (arrFaders[num13].Channel >= 0 && arrFaders[num13].Channel < MixEngine.chData.Length)
					{
						switch (MixEngine.chData[arrFaders[num13].Channel].channelType)
						{
						case ChannelTypes.Input:
						case ChannelTypes.STIN:
							arrFaders[num13].MeterLevel = MixEngine.chData[arrFaders[num13].Channel].getParam(Channel.Pr.MeterPre);
							break;
						case ChannelTypes.Mix:
						case ChannelTypes.Matrix:
						case ChannelTypes.Stereo:
							arrFaders[num13].MeterLevel = MixEngine.chData[arrFaders[num13].Channel].getParam(Channel.Pr.MeterPost);
							break;
						}
					}
				}
				break;
			}
			case Channel.Pr.Fader:
			{
				if (faderMode == FaderModes.FOH && (layerMode == LayerModes.Custom || layerMode == LayerModes.FOH || layerMode == LayerModes.Master))
				{
					for (int m = 0; m < arrFaders.Count - masterSection.Count; m++)
					{
						if (arrFaders[m].Channel == channel)
						{
							arrFaders[m].Value = value;
						}
					}
				}
				for (int n = arrFaders.Count - masterSection.Count; n < arrFaders.Count; n++)
				{
					if (arrFaders[n].Channel == channel)
					{
						arrFaders[n].Value = value;
					}
				}
				break;
			}
			case Channel.Pr.SendToMix:
			{
				if (faderMode != FaderModes.SOF || SOFPage != paramIndex)
				{
					break;
				}
				for (int num12 = 0; num12 < arrFaders.Count; num12++)
				{
					if (arrFaders[num12].Channel == channel)
					{
						arrFaders[num12].Value = value;
					}
				}
				break;
			}
			case Channel.Pr.SendToMixOn:
			{
				if (faderMode != FaderModes.SOF || SOFPage != paramIndex)
				{
					break;
				}
				for (int num2 = 0; num2 < arrFaders.Count; num2++)
				{
					if (arrFaders[num2].Channel == channel)
					{
						setMuteColor(arrMutes[num2], value == 1, groupMute: false);
					}
				}
				break;
			}
			case Channel.Pr.SendToMatrix:
			{
				if (faderMode != FaderModes.MOF || MOFPage != paramIndex)
				{
					break;
				}
				for (int num7 = 0; num7 < arrFaders.Count; num7++)
				{
					if (arrFaders[num7].Channel == channel)
					{
						arrFaders[num7].Value = value;
					}
				}
				break;
			}
			case Channel.Pr.SendToMatrixOn:
			{
				if (faderMode != FaderModes.MOF || MOFPage != paramIndex)
				{
					break;
				}
				for (int l = 0; l < arrFaders.Count; l++)
				{
					if (arrFaders[l].Channel == channel)
					{
						setMuteColor(arrMutes[l], value == 1, groupMute: false);
					}
				}
				break;
			}
			case Channel.Pr.Group:
				updateUI();
				break;
			case Channel.Pr.Pan:
			{
				if (faderMode == FaderModes.Pan)
				{
					for (int num10 = 0; num10 < arrFaders.Count - masterSection.Count; num10++)
					{
						if (arrFaders[num10].Channel == channel)
						{
							arrFaders[num10].Value = value;
						}
					}
				}
				if (faderMode == FaderModes.SOF || faderMode == FaderModes.MOF)
				{
					break;
				}
				for (int num11 = 0; num11 < arrFaders.Count; num11++)
				{
					if (arrPans[num11].Channel == channel)
					{
						arrPans[num11].Value = value;
					}
				}
				break;
			}
			case Channel.Pr.PatchIn:
			{
				if (faderMode == FaderModes.HA)
				{
					for (int num14 = 0; num14 < arrFaders.Count - masterSection.Count; num14++)
					{
						if (arrFaders[num14].Channel == channel)
						{
							arrFaders[num14].Value = MixEngine.chData[channel].getParam(Channel.Pr.HA);
						}
					}
				}
				if (layerMode != 0 && layerMode != LayerModes.Custom)
				{
					break;
				}
				for (int num15 = 0; num15 < arrFaders.Count - masterSection.Count; num15++)
				{
					if (arrFaders[num15].Channel == channel)
					{
						arrHAs[num15].Value = MixEngine.chData[channel].getParam(Channel.Pr.HA);
					}
				}
				break;
			}
			case Channel.Pr.On:
			case Channel.Pr.MuteGroup:
			{
				if (faderMode == FaderModes.FOH || faderMode == FaderModes.HA || faderMode == FaderModes.Pan)
				{
					for (int num3 = 0; num3 < arrFaders.Count - masterSection.Count; num3++)
					{
						if (arrFaders[num3].Channel != channel)
						{
							continue;
						}
						bool groupMute = false;
						for (int num4 = 0; num4 < 8; num4++)
						{
							if (MixEngine.muteGroup[num4].members.Contains(channel) && MixEngine.getParam(num4, MixEngine.Pr.MuteMaster, 0) == 1)
							{
								groupMute = true;
							}
						}
						setMuteColor(arrMutes[num3], MixEngine.chData[channel].getParamBool(Channel.Pr.On), groupMute);
					}
				}
				for (int num5 = arrFaders.Count - masterSection.Count; num5 < arrFaders.Count; num5++)
				{
					if (arrFaders[num5].Channel != channel)
					{
						continue;
					}
					bool groupMute2 = false;
					for (int num6 = 0; num6 < 8; num6++)
					{
						if (MixEngine.muteGroup[num6].members.Contains(channel) && MixEngine.getParam(num6, MixEngine.Pr.MuteMaster, 0) == 1)
						{
							groupMute2 = true;
						}
					}
					setMuteColor(arrMutes[num5], MixEngine.chData[channel].getParamBool(Channel.Pr.On), groupMute2);
				}
				break;
			}
			case Channel.Pr.Cue:
			{
				if (layerMode == LayerModes.FOH || layerMode == LayerModes.Custom || layerMode == LayerModes.Master)
				{
					for (int j = 0; j < arrFaders.Count - masterSection.Count; j++)
					{
						if (arrFaders[j].Channel == channel)
						{
							arrCues[j].BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
							arrCues[j].ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
						}
					}
				}
				for (int k = arrFaders.Count - masterSection.Count; k < arrFaders.Count; k++)
				{
					if (arrFaders[k].Channel == channel)
					{
						arrCues[k].BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
						arrCues[k].ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					}
				}
				break;
			}
			case Channel.Pr.Name:
			{
				if (MixEngine.chData[channel].channelType == ChannelTypes.Mix && UDFPage == "mix")
				{
					execUDF("mix:" + SOFPage);
				}
				if (MixEngine.chData[channel].channelType == ChannelTypes.Mix && UDFPage == "matrix")
				{
					execUDF("matrix:" + MOFPage);
				}
				if (UDFPage == "rack")
				{
					execUDF("rack");
				}
				if (layerMode == LayerModes.Custom || layerMode == LayerModes.FOH || layerMode == LayerModes.Master)
				{
					for (int num8 = 0; num8 < arrFaders.Count; num8++)
					{
						if (arrFaders[num8].Channel == channel)
						{
							arrNames[num8].Text = MixEngine.chData[channel].name;
						}
					}
					break;
				}
				for (int num9 = arrFaders.Count - masterSection.Count; num9 < arrFaders.Count; num9++)
				{
					if (arrFaders[num9].Channel == channel)
					{
						arrNames[num9].Text = MixEngine.chData[channel].name;
					}
				}
				break;
			}
			case Channel.Pr.SendToMixPan:
			{
				if (faderMode != FaderModes.SOF || SOFPage / 2 != paramIndex / 2)
				{
					break;
				}
				for (int num = 0; num < arrFaders.Count; num++)
				{
					if (arrPans[num].Channel == channel)
					{
						arrPans[num].Value = value;
					}
				}
				break;
			}
			case Channel.Pr.SendToMatrixPan:
			{
				if (faderMode != FaderModes.MOF || MOFPage / 2 != paramIndex / 2)
				{
					break;
				}
				for (int i = 0; i < arrFaders.Count; i++)
				{
					if (arrPans[i].Channel == channel)
					{
						arrPans[i].Value = value;
					}
				}
				break;
			}
			default:
				logDebug("frmSurface16.MixEngine_ChannelChanged() unknown param: " + MixEngine.chData[channel].paramName[param]);
				break;
			case Channel.Pr.HA:
			case Channel.Pr.Phantom:
			case Channel.Pr.Phase:
			case Channel.Pr.RelativeHA:
			case Channel.Pr.RelativeFader:
			case Channel.Pr.Balance:
			case Channel.Pr.Pair:
			case Channel.Pr.BusType:
			case Channel.Pr.PanLink:
			case Channel.Pr.DCAGroup:
			case Channel.Pr.InsertOn:
			case Channel.Pr.DirectOutOn:
			case Channel.Pr.ToStereoOn:
			case Channel.Pr.ToMonoOn:
			case Channel.Pr.EQHPFOn:
			case Channel.Pr.EQHPFFreq:
			case Channel.Pr.EQAtt:
			case Channel.Pr.EQType:
			case Channel.Pr.EQOn:
			case Channel.Pr.EQ1Type:
			case Channel.Pr.EQ4Type:
			case Channel.Pr.EQ1HPFOn:
			case Channel.Pr.EQ4LPFOn:
			case Channel.Pr.EQ1Q:
			case Channel.Pr.EQ1F:
			case Channel.Pr.EQ1G:
			case Channel.Pr.EQ2Q:
			case Channel.Pr.EQ2F:
			case Channel.Pr.EQ2G:
			case Channel.Pr.EQ3Q:
			case Channel.Pr.EQ3F:
			case Channel.Pr.EQ3G:
			case Channel.Pr.EQ4Q:
			case Channel.Pr.EQ4F:
			case Channel.Pr.EQ4G:
			case Channel.Pr.SendToMixPre:
			case Channel.Pr.SendToMatrixPre:
			case Channel.Pr.Dyn0On:
			case Channel.Pr.Dyn0Type:
			case Channel.Pr.Dyn0Attack:
			case Channel.Pr.Dyn0Range:
			case Channel.Pr.Dyn0Hold:
			case Channel.Pr.Dyn0Decay:
			case Channel.Pr.Dyn0Ratio:
			case Channel.Pr.Dyn0Gain:
			case Channel.Pr.Dyn0Knee:
			case Channel.Pr.Dyn0Threshold:
			case Channel.Pr.Dyn1On:
			case Channel.Pr.Dyn1Type:
			case Channel.Pr.Dyn1Attack:
			case Channel.Pr.Dyn1Range:
			case Channel.Pr.Dyn1Hold:
			case Channel.Pr.Dyn1Release:
			case Channel.Pr.Dyn1Ratio:
			case Channel.Pr.Dyn1Gain:
			case Channel.Pr.Dyn1Knee:
			case Channel.Pr.Dyn1Threshold:
				break;
			}
		}
		catch (Exception ex)
		{
			logError("ChannelChanged()", "UI", ex.ToString());
		}
	}

	private void btnPageDCA_Click(object sender, EventArgs e)
	{
		if (!surfaceLocked)
		{
			switchLayerMode(LayerModes.DCA);
		}
	}

	private void logError(string Source, string Category, string Message)
	{
		try
		{
			MixEngine.logError(Source, Category, Message);
		}
		catch (Exception ex)
		{
			frmConfirm.sayFail("Could not record Error Log due to:\n" + ex.ToString(), "Log Error", this);
		}
		switch (Category)
		{
		case "UI":
			frmConfirm.sayFail("Unfortunately, there was an error updating the User Interface.\nI wrote it down in Error.Log and will try to keep going.", "User Interface Error", this);
			break;
		case "OutEvent":
			frmConfirm.sayFail("Something bad happened when I tried to tell the console what you changed.\nI wrote it down in Error.Log but you probably better re-start and/or re-sync.", "Outbound Event Error", this);
			break;
		case "InEvent":
			frmConfirm.sayFail("Something bad happened when the console told me about a parameter change.\nI wrote it down in Error.Log but you probably better re-start and/or re-sync.", "Inbound Event Error", this);
			break;
		default:
			frmConfirm.sayFail("I've run into a problem with: " + Category + ".\nI wrote it down in Error.Log and will try to keep going.", "General Error", this);
			break;
		}
	}

	private void logDebug(string Message)
	{
		MixEngine.logDebug(Message);
	}

	private int intRound(decimal value)
	{
		return (int)Math.Round(value);
	}

	private void MixEngine_Action(string property, int index)
	{
		try
		{
			if (lblDebug.InvokeRequired)
			{
				ActionCallback method = MixEngine_Action;
				BeginInvoke(method, property, index);
				return;
			}
			switch (property)
			{
			case "SceneStatus":
				if (frmSceneH != null && !frmSceneH.IsDisposed)
				{
					frmSceneH.refreshGrid();
				}
				break;
			case "RackSource":
			case "RackType":
				if (UDFPage == "rack")
				{
					execUDF("rack");
				}
				break;
			case "OutPortDelayOn":
			{
				if (layerMode != LayerModes.OutportDelay)
				{
					break;
				}
				for (int m = 0; m < arrFaders.Count; m++)
				{
					if (arrFaders[m].Channel == index)
					{
						arrMutes[m].BackColor = (MixEngine.OutPort[index].getParamBool(OutPort.Pr.DelayOn) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
						arrMutes[m].ForeColor = (MixEngine.OutPort[index].getParamBool(OutPort.Pr.DelayOn) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					}
				}
				break;
			}
			case "OutPortDelayTime":
			{
				if (layerMode != LayerModes.OutportDelay)
				{
					break;
				}
				for (int i = 0; i < arrFaders.Count; i++)
				{
					if (arrFaders[i].Channel == index)
					{
						arrFaders[i].Value = MixEngine.OutPort[index].getParam(OutPort.Pr.DelayTime);
						arrInfo[i].Text = ((double)((float)MixEngine.OutPort[index]._delayTime / 100000f) * 1127.26).ToString("F1") + " ft\n" + ((double)((float)MixEngine.OutPort[index]._delayTime / 100000f) * 343.59).ToString("F1") + " m";
					}
				}
				break;
			}
			case "OutPortAtt":
			{
				if (layerMode != LayerModes.OutportAtt)
				{
					break;
				}
				for (int k = 0; k < arrFaders.Count; k++)
				{
					if (arrFaders[k].Channel == index)
					{
						arrFaders[k].Value = MixEngine.OutPort[index].getParam(OutPort.Pr.Attenuation);
					}
				}
				break;
			}
			case "OutPortPhase":
			{
				if (layerMode != LayerModes.OutportAtt)
				{
					break;
				}
				for (int n = 0; n < arrFaders.Count; n++)
				{
					if (arrFaders[n].Channel == index)
					{
						arrMutes[n].BackColor = (MixEngine.OutPort[index].getParamBool(OutPort.Pr.Phase) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
						arrMutes[n].ForeColor = (MixEngine.OutPort[index].getParamBool(OutPort.Pr.Phase) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					}
				}
				break;
			}
			case "DCAFader":
			{
				if (layerMode != LayerModes.DCA)
				{
					break;
				}
				for (int l = 0; l < arrFaders.Count; l++)
				{
					if (arrFaders[l].Channel == index)
					{
						arrFaders[l].Value = MixEngine.DCA[index].fader;
					}
				}
				break;
			}
			case "DCAMute":
			{
				if (layerMode != LayerModes.DCA)
				{
					break;
				}
				for (int j = 0; j < arrFaders.Count; j++)
				{
					if (arrFaders[j].Channel == index)
					{
						setMuteColor(arrMutes[j], !MixEngine.DCA[index]._muted, groupMute: false);
					}
				}
				break;
			}
			case "ConsoleType":
			case "MasterSection":
				masterSection.Clear();
				switch (Settings.Default.MasterSection)
				{
				case "96":
				case "Stereo":
					masterSection.Add(MixEngine.chOffsetStereo);
					break;
				case "98":
				case "Mono":
					masterSection.Add(MixEngine.chOffsetStereo + 2);
					break;
				case "96,98":
				case "Both":
				case "Mono+ST":
					masterSection.Add(MixEngine.chOffsetStereo + 2);
					masterSection.Add(MixEngine.chOffsetStereo);
					break;
				case "99":
				case "Monitor":
					masterSection.Add(MixEngine.chOffsetStereo + 3);
					break;
				case "ST+Monitor":
					masterSection.Add(MixEngine.chOffsetStereo);
					masterSection.Add(MixEngine.chOffsetStereo + 3);
					break;
				case "Monitor+ST":
					masterSection.Add(MixEngine.chOffsetStereo + 3);
					masterSection.Add(MixEngine.chOffsetStereo);
					break;
				}
				numFaders_Change();
				break;
			}
		}
		catch (Exception ex)
		{
			logError("MixEngine_Action()", "UI", ex.ToString());
		}
	}

	private void MixEngine_NewMessage(string strMessage, int errorCode)
	{
		try
		{
			if (lblDebug.InvokeRequired)
			{
				newMessageCallback method = MixEngine_NewMessage;
				BeginInvoke(method, strMessage, errorCode);
				return;
			}
			switch (errorCode)
			{
			case -1:
				lblDebug.ForeColor = Color.Yellow;
				break;
			case -99:
				btnConnect.Text = "Connect";
				btnConnect.BackText = "";
				lblDebug.ForeColor = Color.Red;
				lblConnStatus.Text = "Connection Lost!";
				lblConnStatus.ForeColor = Color.Red;
				frmConfirm.sayFail("Connection Lost", "Error", this);
				break;
			case -98:
				lblDebug.ForeColor = Color.White;
				lblConnStatus.Text = "Connection Restored!";
				lblConnStatus.ForeColor = Color.Lime;
				MixEngine_NewMessage("Scene Dump...", 0);
				break;
			case -97:
				lblDebug.ForeColor = Color.Red;
				lblConnStatus.Text = "Connection Bad...";
				lblConnStatus.ForeColor = Color.Orange;
				break;
			case 0:
				if (strMessage == "SCENENAME")
				{
					MixEngine.logDebug("Called SCENENAME for some reason");
					lblSceneName.Text = Console.sceneNum.ToString("000") + ": " + Console.sceneName;
				}
				else
				{
					lblConnStatus.Text = strMessage;
					lblConnStatus.ForeColor = Color.Lime;
					lblDebug.ForeColor = Color.White;
				}
				break;
			case 1:
				lblConnStatus.Text = strMessage;
				lblConnStatus.ForeColor = Color.OrangeRed;
				break;
			case 2:
				frmConfirm.sayOK(strMessage, "Info", this);
				break;
			case 3:
				frmConfirm.sayFail(strMessage, "Error", this);
				break;
			case 99:
				if (strMessage == "ATTENTION")
				{
					Font font = FontFit("Arial", FontStyle.Bold, "Intercom Flasher", (int)Math.Round((double)base.Width * 0.9), (int)((double)base.Height * 0.9), 32f);
					consoleAlert.AutoSize = false;
					consoleAlert.Left = 10;
					consoleAlert.Top = 10;
					consoleAlert.Width = base.Width - 20;
					consoleAlert.Height = panelFaders.Top - 20;
					consoleAlert.BorderStyle = BorderStyle.FixedSingle;
					consoleAlert.BackColor = Color.Yellow;
					consoleAlert.ForeColor = Color.Red;
					consoleAlert.TextAlign = ContentAlignment.MiddleCenter;
					consoleAlert.Font = font;
					consoleAlert.Text = "Intercom Flasher";
					consoleAlert.BringToFront();
					consoleAlert.Show();
				}
				else
				{
					consoleAlert.Hide();
				}
				break;
			}
			lblDebug.Text = strMessage;
			if (lbLog.Visible)
			{
				lbLog.Items.Add(strMessage);
			}
		}
		catch (Exception ex)
		{
			logError("MixEngine_NewMessage()", "UI", ex.ToString());
		}
	}

	private int FuzzyFind(decimal[] group, decimal find)
	{
		try
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
				Loc.Msg("WARN: Subzero index (" + num + ") in FuzzyFind. Please notify support!");
			}
			if (num >= group.Length)
			{
				Loc.Msg("WARN: Overlimit index (" + num + ") in FuzzyFind. Please notify support!");
			}
			decimal num2 = group[num] - find;
			decimal num3 = find - group[num - 1];
			if (num2 <= num3)
			{
				return num;
			}
			return num - 1;
		}
		catch (Exception ex)
		{
			logError("FuzzyFind()", "Math", ex.ToString());
		}
		return 0;
	}

	public byte[] ToByteArray(string hex)
	{
		try
		{
			int length = hex.Length;
			byte[] array = new byte[length / 2];
			for (int i = 0; i < length; i += 2)
			{
				try
				{
					array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
				}
				catch (Exception)
				{
					return new byte[0];
				}
			}
			return array;
		}
		catch (Exception ex2)
		{
			logError("ToByteArray()", "Math", ex2.ToString());
		}
		return new byte[0];
	}

	public byte[] StuffString(string name)
	{
		try
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
			return BitConverter.GetBytes(num2);
		}
		catch (Exception ex)
		{
			logError("StuffString()", "Math", ex.ToString());
		}
		return new byte[0];
	}

	public byte[] StuffValue(int value)
	{
		try
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
			byte[] array = BitConverter.GetBytes(num2);
			Array.Resize(ref array, 5);
			return array;
		}
		catch (Exception ex)
		{
			logError("StuffValue()", "Math", ex.ToString());
		}
		return new byte[0];
	}

	public string StuffValueStr(int value)
	{
		try
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
			return num2.ToString("x10");
		}
		catch (Exception ex)
		{
			logError("StuffValueSTR()", "Math", ex.ToString());
		}
		return "";
	}

	public string UnStuffValue(string hex)
	{
		try
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
		catch (Exception ex)
		{
			logError("UnStuffValue()", "Math", ex.ToString());
		}
		return "";
	}

	public string UnStuffString(string hex)
	{
		try
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
				text = (char)(num2 & 0xFF) + text;
				num2 >>= 8;
			}
			return text;
		}
		catch (Exception ex)
		{
			logError("UnStuffString()", "Math", ex.ToString());
		}
		return "";
	}

	private byte[] UnStuffDump(string hex)
	{
		try
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
				for (int j = 19; j < num3 + 19 - 6 - 8; j += 8)
				{
					for (int k = 0; k < 7; k++)
					{
						try
						{
							array[num + j] <<= 1;
							array2[num2 + k] = (byte)(array[num + j + k + 1] + (array[num + j] & 0x80));
						}
						catch (Exception ex)
						{
							Loc.Msg(ex.Message);
							return null;
						}
					}
					num2 += 7;
				}
				num += num3 + 6;
				num += 2;
			}
			return array2;
		}
		catch (Exception ex2)
		{
			logError("UnStuffDump()", "Math", ex2.ToString());
		}
		return new byte[0];
	}

	private void setMuteColor(AirButton btn, bool on, bool groupMute)
	{
		if (groupMute && on)
		{
			btn.BackColor = Color.Yellow;
			btn.ForeColor = Color.Black;
			btn.Text = "[On]";
		}
		if (groupMute && !on)
		{
			btn.BackColor = Settings.Default.c_channel_off;
			btn.ForeColor = Settings.Default.c_channel_off_fore;
			btn.Text = "Off";
		}
		if (!groupMute && on)
		{
			btn.BackColor = Settings.Default.c_channel_on;
			btn.ForeColor = Settings.Default.c_channel_on_fore;
			btn.Text = "On";
		}
		if (!groupMute && !on)
		{
			btn.BackColor = Settings.Default.c_channel_off;
			btn.ForeColor = Settings.Default.c_channel_off_fore;
			btn.Text = "Off";
		}
	}

	private void btnConnect_Click(object sender, EventArgs e)
	{
		try
		{
			if (surfaceLocked)
			{
				return;
			}
			if (Settings.Default.autoMeters)
			{
				showMeter = true;
				for (int i = 0; i < arrFaders.Count; i++)
				{
					arrFaders[i].ShowMeter = showMeter;
				}
				syncUDFState();
			}
			if (MixEngine.connStatus == ConnectionStatus.Disconnected)
			{
				surfaceChanged = false;
				// Open-source build: no license key and no trial are required,
				// so the connection is started unconditionally.
				if (MixEngine.LICENSING_ENABLED)
				{
					if (MixEngine.getLicenseKey().Length != 19)
					{
						frmTrial frmTrial2 = new frmTrial();
						frmTrial2.ShowDialog(this);
						return;
					}
					if (!MixEngine.checkSerialV2(MixEngine.getLicenseUser(), MixEngine.getLicenseKey()))
					{
						frmConfirm.sayFail("Invalid License Key. Please visit \"Config --> About\" to Register.", "Not Connected", this);
						return;
					}
				}
				lblConnStatus.Text = "Connecting...";
				lblConnStatus.ForeColor = Color.Yellow;
				MixEngine.logDebug("Starting SingleThread...");
				switch (MixEngine.connectConsole(Settings.Default.MIDI_Input, Settings.Default.MIDI_Output))
				{
				case 1:
					lblDebug.Text = "Devices opened.";
					lblConnStatus.Text = "Connected";
					lblConnStatus.ForeColor = Color.Lime;
					MixEngine.connPhase = ConnectionPhase.PreSync;
					btnConnect.Text = "Go";
					btnConnect.BackText = "Offline";
					break;
				case -1:
					lblDebug.Text = "Device not found.";
					lblConnStatus.Text = "Device Not Found";
					lblConnStatus.ForeColor = Color.Red;
					btnConnect.Text = "Connect";
					btnConnect.BackText = "";
					frmConfirm.sayFail("I couldn't find the name of the console.\nPlease click \"Config\" and make sure Input and Output devices are selected.", "Device Not Found", this);
					break;
				case -2:
					lblDebug.Text = "Connection Error.";
					lblConnStatus.Text = "Connection Failed";
					lblConnStatus.ForeColor = Color.Red;
					btnConnect.Text = "Connect";
					btnConnect.BackText = "";
					frmConfirm.sayFail("I tried to connect to \"" + Settings.Default.MIDI_Input + "\" but couldn't.\nCheck your network config and physical/wireless connectivity.", "Connection Failed", this);
					break;
				case 0:
					break;
				}
			}
			else
			{
				MixEngine.logDebug("Stopping SingleThread...");
				MixEngine.reqParam("StopMeter", 0, 0);
				MixEngine.disconnectConsole();
				btnConnect.Text = "Connect";
				btnConnect.BackText = "";
			}
		}
		catch (Exception ex)
		{
			logError("ConnectClick()", "UI", ex.ToString());
		}
	}

	private void inDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
	{
		logError("inDeviceError()", "InEvent", e.ToString());
	}

	private void btnNumber_Click(object sender, EventArgs e)
	{
		if (surfaceLocked)
		{
			return;
		}
		try
		{
			if (layerMode == LayerModes.DCA || layerMode == LayerModes.OutportAtt || layerMode == LayerModes.OutportDelay)
			{
				return;
			}
			AirButton airButton = (AirButton)sender;
			int num = int.Parse(airButton.Tag.ToString());
			if (num >= 0 && num < MixEngine.chData.Length && MixEngine.chData[num].channelType == ChannelTypes.Monitor)
			{
				return;
			}
			if (isDebug)
			{
				logDebug("Number_Click: Open Forms Count: " + Application.OpenForms.Count);
			}
			if (frmChannelH != null)
			{
				if (isDebug)
				{
					logDebug("Using existing frmChannelH");
				}
				frmChannelH.ActiveChannel = num;
				frmChannelH.Activate();
				frmChannelH.Show();
				return;
			}
			foreach (Form openForm in Application.OpenForms)
			{
				if (openForm is frmChannel)
				{
					((frmChannel)openForm).ActiveChannel = num;
					((frmChannel)openForm).Activate();
					((frmChannel)openForm).Show();
					return;
				}
			}
			frmChannel frmChannel2 = new frmChannel(this);
			frmChannel2.ActiveChannel = num;
			frmChannel2.Show();
			if (isDebug)
			{
				logDebug("Number_Click: Open Forms Count: " + Application.OpenForms.Count);
			}
		}
		catch (Exception ex)
		{
			logError("ChanClick()", "UI", ex.ToString());
		}
	}

	private void airFader_FaderChanged(int channel, int value, string flag)
	{
		try
		{
			if (channel < 0 || channel >= MixEngine.chData.Length)
			{
				return;
			}
			if (flag == "Master")
			{
				MixEngine.chData[channel].setParam("UI", Channel.Pr.Fader, 0, value);
				return;
			}
			switch (faderMode)
			{
			case FaderModes.DCA:
				MixEngine.DCA[channel].setFader(value, "UI");
				break;
			case FaderModes.FOH:
				MixEngine.chData[channel].setParam("UI", Channel.Pr.Fader, 0, value);
				break;
			case FaderModes.HA:
				MixEngine.chData[channel].setParam("UI", Channel.Pr.HA, 0, value);
				break;
			case FaderModes.MOF:
				MixEngine.chData[channel].setParam("UI", Channel.Pr.SendToMatrix, MOFPage, value);
				break;
			case FaderModes.OutportAtt:
				MixEngine.OutPort[channel].setParam("UI", OutPort.Pr.Attenuation, value);
				break;
			case FaderModes.OutportDelay:
				MixEngine.OutPort[channel].setParam("UI", OutPort.Pr.DelayTime, value);
				break;
			case FaderModes.Pan:
				MixEngine.chData[channel].setParam("UI", Channel.Pr.Pan, 0, value);
				break;
			case FaderModes.SOF:
				MixEngine.chData[channel].setParam("UI", Channel.Pr.SendToMix, SOFPage, value);
				break;
			}
		}
		catch (Exception ex)
		{
			logError("FaderChanged()", "UI", ex.ToString());
		}
	}

	private void nameFader_TextChanged(object sender, EventArgs e)
	{
		try
		{
			TextBox textBox = (TextBox)sender;
			int num = int.Parse(textBox.Tag.ToString());
			if (textBox.Focused)
			{
				MixEngine.chData[num].setParam("UI", Channel.Pr.Name, 0, textBox.Text);
			}
		}
		catch (Exception ex)
		{
			logError("TextChanged()", "UI", ex.ToString());
		}
	}

	private void muteFader_Click(object sender, EventArgs e)
	{
		if (surfaceLocked)
		{
			return;
		}
		try
		{
			AirButton airButton = (AirButton)sender;
			int num = int.Parse(airButton.Tag.ToString());
			if (airButton.Flag == "Master")
			{
				MixEngine.chData[num].setParam("UI", Channel.Pr.On, 0, !MixEngine.chData[num].getParamBool(Channel.Pr.On));
				return;
			}
			switch (faderMode)
			{
			case FaderModes.DCA:
				MixEngine.DCA[num].setMute(!MixEngine.DCA[num]._muted, "surface");
				break;
			case FaderModes.FOH:
			case FaderModes.HA:
			case FaderModes.Pan:
				MixEngine.chData[num].setParam("UI", Channel.Pr.On, 0, !MixEngine.chData[num].getParamBool(Channel.Pr.On));
				break;
			case FaderModes.SOF:
				MixEngine.chData[num].setParam("UI", Channel.Pr.SendToMixOn, SOFPage, !MixEngine.chData[num].getParamBool(Channel.Pr.SendToMixOn, SOFPage));
				break;
			case FaderModes.MOF:
				MixEngine.chData[num].setParam("UI", Channel.Pr.SendToMatrixOn, MOFPage, !MixEngine.chData[num].getParamBool(Channel.Pr.SendToMatrixOn, MOFPage));
				break;
			case FaderModes.OutportAtt:
				MixEngine.OutPort[num].setParam("UI", OutPort.Pr.Phase, !MixEngine.OutPort[num].getParamBool(OutPort.Pr.Phase));
				break;
			case FaderModes.OutportDelay:
				MixEngine.OutPort[num].setParam("UI", OutPort.Pr.DelayOn, !MixEngine.OutPort[num].getParamBool(OutPort.Pr.DelayOn));
				break;
			}
		}
		catch (Exception ex)
		{
			logError("MuteFaderClick()", "UI", ex.ToString());
		}
	}

	private void phantomButton_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				int result = 0;
				if (int.TryParse(((AirButton)sender).Tag.ToString(), out result))
				{
					MixEngine.chData[result].setParam("UI", Channel.Pr.Phantom, 0, !MixEngine.chData[result].getParamBool(Channel.Pr.Phantom));
				}
			}
		}
		catch (Exception ex)
		{
			logError("PhantomClick()", "UI", ex.ToString());
		}
	}

	private void haKnob_KnobChanged(AirKnob sender, int channel, int value, string flag)
	{
		try
		{
			if (!surfaceLocked && channel >= 0 && channel < MixEngine.chData.Length)
			{
				MixEngine.chData[channel].setParam("UI", Channel.Pr.HA, 0, value);
			}
		}
		catch (Exception ex)
		{
			logError("haKnob_Changed()", "UI", ex.ToString());
		}
	}

	private void panKnob_KnobChanged(AirKnob sender, int channel, int value, string flag)
	{
		try
		{
			if (!surfaceLocked && channel >= 0 && channel < MixEngine.chData.Length)
			{
				switch (faderMode)
				{
				case FaderModes.SOF:
					MixEngine.chData[channel].setParam("UI", Channel.Pr.SendToMixPan, SOFPage, value);
					break;
				case FaderModes.MOF:
					MixEngine.chData[channel].setParam("UI", Channel.Pr.SendToMatrixPan, MOFPage, value);
					break;
				case FaderModes.FOH:
				case FaderModes.HA:
				case FaderModes.Pan:
					MixEngine.chData[channel].setParam("UI", Channel.Pr.Pan, 0, value);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			logError("panKnob_Changed()", "UI", ex.ToString());
		}
	}

	private void btnCue_Click(object sender, EventArgs e)
	{
		if (surfaceLocked)
		{
			return;
		}
		try
		{
			AirButton airButton = (AirButton)sender;
			int num = int.Parse(airButton.Tag.ToString());
			if (num >= 0 && num < MixEngine.chData.Length && (layerMode == LayerModes.FOH || layerMode == LayerModes.Custom || layerMode == LayerModes.Master || airButton.Flag == "Master"))
			{
				MixEngine.setParam("UI", num, MixEngine.Pr.Cue, 0, (MixEngine.chData[num].getParam(Channel.Pr.Cue) != 1) ? 1 : 0);
			}
		}
		catch (Exception ex)
		{
			logError("CueClick()", "UI", ex.ToString());
		}
	}

	private void btnFine_Click(object sender, EventArgs e)
	{
		if (surfaceLocked)
		{
			return;
		}
		try
		{
			fineMode = !fineMode;
			for (int i = 0; i < arrFaders.Count; i++)
			{
				arrFaders[i].fineMode = fineMode;
				arrPans[i].FineMode = fineMode;
				arrHAs[i].FineMode = fineMode;
			}
			if (fineMode)
			{
				btnFine.BackColor = Settings.Default.c_button_active;
				btnFine.ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				btnFine.BackColor = Settings.Default.c_button_inactive;
				btnFine.ForeColor = Settings.Default.c_button_inactive_fore;
			}
		}
		catch (Exception ex)
		{
			logError("FineClick()", "UI", ex.ToString());
		}
	}

	private void btnLayer_Click(object sender, EventArgs e)
	{
		try
		{
			AirButton airButton = (AirButton)sender;
			if (layerMode == LayerModes.Custom)
			{
				offsetCustom = int.Parse(airButton.Tag.ToString());
			}
			else if (layerMode == LayerModes.Master)
			{
				offsetMaster = int.Parse(airButton.Tag.ToString());
			}
			else if (layerMode == LayerModes.OutportAtt || layerMode == LayerModes.OutportDelay)
			{
				offsetOutport = int.Parse(airButton.Tag.ToString());
			}
			else if (layerMode == LayerModes.DCA)
			{
				offsetDCA = int.Parse(airButton.Tag.ToString());
			}
			else
			{
				offsetFOH = int.Parse(airButton.Tag.ToString());
			}
			updateUI();
		}
		catch (Exception ex)
		{
			logError("LayerClick()", "UI", ex.ToString());
		}
	}

	private void btnConfig_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				if (frmConfigH == null || frmConfigH.IsDisposed)
				{
					frmConfigH = new frmConfig(this);
					frmConfigH.Show();
				}
				else
				{
					frmConfigH.Activate();
				}
			}
		}
		catch (Exception ex)
		{
			logError("ConfigClick()", "UI", ex.ToString());
		}
	}

	private void btnFullScreen_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				if (base.FormBorderStyle == FormBorderStyle.Sizable)
				{
					thisWin.WindowState = "Fullscreen";
					thisWin.SetWindow(this);
					btnFullScreen.Text = "Normal\nScreen";
					btnFullScreen.BackText = "";
				}
				else
				{
					thisWin.WindowState = "Normal";
					thisWin.SetWindow(this);
					btnFullScreen.Text = "Full\nScreen";
					btnFullScreen.BackText = "";
				}
			}
		}
		catch (Exception ex)
		{
			logError("FullScreenClick()", "UI", ex.ToString());
		}
	}

	private void btnLock_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				for (int i = 0; i < arrFaders.Count; i++)
				{
					arrFaders[i].Enabled = false;
				}
				btnLock.Text = "UnLock";
				btnLock.BackColor = Color.Red;
				btnLock.ForeColor = Color.Yellow;
				surfaceLocked = true;
				lblLocked.Left = base.Width / 2 - lblLocked.Width / 2;
				lblLocked.Top = base.Height / 2 - lblLocked.Height / 2;
				lblLocked.Show();
			}
			else
			{
				for (int j = 0; j < arrFaders.Count; j++)
				{
					arrFaders[j].Enabled = true;
				}
				btnLock.Text = "Lock";
				btnLock.BackColor = Settings.Default.c_button_inactive;
				btnLock.ForeColor = Settings.Default.c_button_inactive_fore;
				surfaceLocked = false;
				lblLocked.Hide();
			}
		}
		catch (Exception ex)
		{
			logError("LockClick()", "UI", ex.ToString());
		}
	}

	private void lblDebug_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked && lblDebug.Text != null)
			{
				Clipboard.SetText(lblDebug.Text);
			}
		}
		catch (Exception ex)
		{
			logError("DebugClick()", "UI", ex.ToString());
		}
	}

	private void refreshMeters()
	{
		try
		{
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				MixEngine.reqParam("InputMeterRange", 0, Console.maxInputs, 127);
				MixEngine.reqParam("InputMeterRange", MixEngine.chOffsetMix, Console.maxMIX, 127);
				MixEngine.reqParam("InputMeterRange", MixEngine.chOffsetMatrix, Console.maxMATRIX, 127);
				MixEngine.reqParam("InputMeterRange", MixEngine.chOffsetStereo, 3, 127);
			}
			else
			{
				MixEngine.reqParam("InputMeterRange", 0, Console.maxInputs, 127);
				MixEngine.reqParam("InputMeterRange", MixEngine.chOffsetMix, Console.maxMIX, 127);
				MixEngine.reqParam("InputMeterRange", MixEngine.chOffsetMatrix, Console.maxMATRIX, 127);
				MixEngine.reqParam("InputMeterRange", MixEngine.chOffsetStereo, 3, 127);
			}
		}
		catch (Exception ex)
		{
			logError("RefreshMeters()", "UI", ex.ToString());
		}
	}

	private void timerClock_Tick(object sender, EventArgs e)
	{
		try
		{
			lblClock.Text = DateTime.Now.ToShortTimeString();
			if (clockTicks % 10 == 0 && showMeter)
			{
				refreshMeters();
			}
			if (clockTicks++ < 29)
			{
				return;
			}
			if (power.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
			{
				meterBattery.Hide();
			}
			else
			{
				meterBattery.Show();
				int num = (int)(power.BatteryLifePercent * 100f);
				lblDebug.Text = num.ToString();
				if (num >= 25)
				{
					meterBattery.Value = num;
					meterBattery.ForeColor = Color.Lime;
					meterBattery.BackColor = Color.DarkGray;
				}
				else if (num >= 10)
				{
					meterBattery.Value = num;
					meterBattery.ForeColor = Color.DarkOrange;
					meterBattery.BackColor = Color.DarkGray;
				}
				else
				{
					meterBattery.Value = num;
					meterBattery.ForeColor = Color.Red;
					meterBattery.BackColor = Color.Yellow;
				}
				if (power.PowerLineStatus == PowerLineStatus.Online)
				{
					meterBattery.BorderColor = Color.Lime;
				}
				else
				{
					meterBattery.BackColor = Color.DarkGray;
				}
			}
			clockTicks = 0;
		}
		catch (Exception ex)
		{
			logError("ClockTick()", "UI", ex.ToString());
		}
	}

	private void tickUI(object data)
	{
		try
		{
			for (int i = 0; i < 4; i++)
			{
				if (tapStart[i] < 0)
				{
					continue;
				}
				bool isCounting = true;
				long num = (long)Math.Round(60.0 / (double)MixEngine.EFXData[i].bpm * (double)Stopwatch.Frequency);
				long num2 = (long)Math.Round(2.4 * (double)Stopwatch.Frequency);
				long num3 = (long)Math.Round((double)Stopwatch.Frequency / 8.0);
				long num4 = (animationTimer.ElapsedTicks - tapAlign[i]) % num;
				if (tapCount[i] < 2)
				{
					if (animationTimer.ElapsedTicks - tapLast[i] > num2)
					{
						isCounting = false;
					}
				}
				else if ((double)(animationTimer.ElapsedTicks - tapLast[i]) > (double)num * 1.75)
				{
					isCounting = false;
				}
				if (num4 < num3)
				{
					if (!tapBlink[i])
					{
						tapMakeBlink(i, isBlink: true, isCounting);
					}
				}
				else if (tapBlink[i])
				{
					tapMakeBlink(i, isBlink: false, isCounting);
				}
			}
		}
		catch (Exception ex)
		{
			logError("TickUI()", "UI", ex.ToString());
		}
	}

	private void updateUI()
	{
		try
		{
			string[] array = new string[10] { "00FFFF", "8080FF", "FF00FF", "80FF00", "FFC080", "C080C0", "FF8080", "008080", "FFFF00", "C08000" };
			for (int i = 0; i < panelLayer.Controls.Count; i++)
			{
				((AirButton)panelLayer.Controls[i]).ParentColor = panelLayer.BackColor;
				if ((offsetCustom == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.Custom) || (offsetDCA == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.DCA) || (offsetMaster == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.Master) || (offsetFOH == (int)panelLayer.Controls[i].Tag && layerMode == LayerModes.FOH) || (offsetOutport == (int)panelLayer.Controls[i].Tag && (layerMode == LayerModes.OutportAtt || layerMode == LayerModes.OutportDelay)))
				{
					panelLayer.Controls[i].BackColor = Settings.Default.c_button_active;
					panelLayer.Controls[i].ForeColor = Settings.Default.c_button_active_fore;
				}
				else
				{
					panelLayer.Controls[i].BackColor = Settings.Default.c_button_inactive;
					panelLayer.Controls[i].ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			int num = 0;
			int num2 = -1;
			for (int j = arrFaders.Count - masterSection.Count; j < arrFaders.Count; j++)
			{
				arrNames[j].BackColor = Settings.Default.c_led_back;
				arrNames[j].ForeColor = Settings.Default.c_led_fore;
				arrFaders[j].BackColor = Settings.Default.c_fader_back;
				arrFaders[j].ForeColor = Settings.Default.c_fader_fore;
				arrFaders[j].TailColor = Settings.Default.c_fader_tail;
				arrLabels[j].ParentColor = Settings.Default.c_background;
				arrCues[j].ParentColor = Settings.Default.c_background;
				arrMutes[j].ParentColor = Settings.Default.c_background;
				arrPhantom[j].ParentColor = Settings.Default.c_background;
				num2 = ((faderMode == FaderModes.FOH || faderMode == FaderModes.HA || faderMode == FaderModes.Pan || faderMode == FaderModes.DCA || faderMode == FaderModes.OutportAtt || faderMode == FaderModes.OutportDelay) ? masterSection[j - (arrFaders.Count - masterSection.Count)] : ((faderMode == FaderModes.SOF) ? ((j != arrFaders.Count - 1) ? masterSection[j - (arrFaders.Count - masterSection.Count)] : (MixEngine.chOffsetMix + SOFPage)) : ((faderMode != FaderModes.MOF) ? (-1) : ((j != arrFaders.Count - 1) ? masterSection[j - (arrFaders.Count - masterSection.Count)] : (MixEngine.chOffsetMatrix + MOFPage)))));
				if (num2 >= 0 && num2 < MixEngine.chData.Length)
				{
					arrFaders[j].Channel = num2;
					arrLabels[j].Text = MixEngine.chData[num2].hardname.Replace("Ch", "").Trim();
					arrInfo[j].Text = MixEngine.chData[num2].hardname.Replace("Ch", "").Trim();
					arrNames[j].Text = MixEngine.chData[num2].name;
					arrNames[j].ReadOnly = false;
					arrLabels[j].Tag = num2;
					arrMutes[j].Tag = num2;
					arrNames[j].Tag = num2;
					arrCues[j].Tag = num2;
					arrPhantom[j].Tag = num2;
					arrPans[j].Channel = num2;
					arrHAs[j].Channel = num2;
					arrMutes[j].Flag = "Master";
					arrCues[j].Flag = "Master";
					arrPans[j].Flag = "Master";
					arrHAs[j].Flag = "Master";
					arrFaders[j].Flag = "Master";
					arrFaders[j].MeterLevel = 0;
					if (MixEngine.chData[num2].channelType == ChannelTypes.Monitor)
					{
						arrCues[j].Hide();
						arrPans[j].Hide();
					}
					else
					{
						if (arrLayout.Contains("Cue"))
						{
							arrCues[j].Show();
						}
						else
						{
							arrCues[j].Hide();
						}
						if (arrLayout.Contains("Pan"))
						{
							arrPans[j].Show();
						}
						else
						{
							arrPans[j].Hide();
						}
					}
					if (arrLayout.Contains("Label"))
					{
						arrLabels[j].Show();
					}
					else
					{
						arrLabels[j].Hide();
					}
					if (arrLayout.Contains("Info"))
					{
						arrInfo[j].Show();
					}
					else
					{
						arrInfo[j].Hide();
					}
					if (arrLayout.Contains("Name"))
					{
						arrNames[j].Show();
					}
					else
					{
						arrNames[j].Hide();
					}
					if (arrLayout.Contains("Mute"))
					{
						arrMutes[j].Show();
					}
					else
					{
						arrMutes[j].Hide();
					}
					if (arrLayout.Contains("Fader"))
					{
						arrFaders[j].Show();
					}
					else
					{
						arrFaders[j].Hide();
					}
					arrHAs[j].Hide();
					arrPhantom[j].Hide();
					switch (MixEngine.chData[num2].channelType)
					{
					case ChannelTypes.Input:
					case ChannelTypes.STIN:
						arrFaders[j].FaderColor = "White";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[j].ValueType.Equals("busfader"))
						{
							arrFaders[j].ValueType = "fader";
						}
						break;
					case ChannelTypes.Mix:
					case ChannelTypes.Matrix:
						arrFaders[j].FaderColor = "Purple";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[j].ValueType.Equals("fader"))
						{
							arrFaders[j].ValueType = "busfader";
						}
						break;
					case ChannelTypes.Stereo:
						arrFaders[j].FaderColor = "Red";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[j].ValueType.Equals("fader"))
						{
							arrFaders[j].ValueType = "busfader";
						}
						break;
					case ChannelTypes.Monitor:
						arrFaders[j].FaderColor = "Green";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[j].ValueType.Equals("fader"))
						{
							arrFaders[j].ValueType = "busfader";
						}
						break;
					}
					arrLabels[j].BackColor = Settings.Default.c_button_inactive;
					arrLabels[j].ForeColor = Settings.Default.c_button_inactive_fore;
					bool groupMute = false;
					for (int k = 0; k < 8; k++)
					{
						if (MixEngine.muteGroup[k].members.Contains(num2) && MixEngine.getParam(k, MixEngine.Pr.MuteMaster, 0) == 1)
						{
							groupMute = true;
						}
					}
					setMuteColor(arrMutes[j], MixEngine.chData[num2].getParamBool(Channel.Pr.On), groupMute);
					arrCues[j].BackColor = ((MixEngine.chData[num2].getParam(Channel.Pr.Cue) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrCues[j].ForeColor = ((MixEngine.chData[num2].getParam(Channel.Pr.Cue) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrPans[j].Value = MixEngine.chData[num2].getParam(Channel.Pr.Pan);
					if (MixEngine.chData[num2].getParam(Channel.Pr.Fader) != arrFaders[j].Value)
					{
						arrFaders[j].Value = MixEngine.chData[num2].getParam(Channel.Pr.Fader);
					}
				}
				else
				{
					arrFaders[j].MeterLevel = 0;
					arrFaders[j].Channel = -1;
					arrLabels[j].Text = "";
					arrLabels[j].Tag = -1;
					arrMutes[j].Tag = -1;
					arrCues[j].Tag = -1;
					arrPans[j].Channel = -1;
					arrNames[j].Text = "";
					arrInfo[j].Text = "";
					arrFaders[j].Hide();
					arrLabels[j].Hide();
					arrInfo[j].Hide();
					arrNames[j].Hide();
					arrMutes[j].Hide();
					arrCues[j].Hide();
					arrPans[j].Hide();
					arrPhantom[j].Hide();
					arrHAs[j].Hide();
				}
			}
			for (int l = 0; l < arrFaders.Count - masterSection.Count; l++)
			{
				arrNames[l].BackColor = Settings.Default.c_led_back;
				arrNames[l].ForeColor = Settings.Default.c_led_fore;
				arrFaders[l].BackColor = Settings.Default.c_fader_back;
				arrFaders[l].ForeColor = Settings.Default.c_fader_fore;
				arrFaders[l].TailColor = Settings.Default.c_fader_tail;
				arrLabels[l].ParentColor = Settings.Default.c_background;
				arrCues[l].ParentColor = Settings.Default.c_background;
				arrMutes[l].ParentColor = Settings.Default.c_background;
				arrPhantom[l].ParentColor = Settings.Default.c_background;
				switch (layerMode)
				{
				case LayerModes.FOH:
					num = offsetFOH + l;
					num2 = ((num < layerFOH.Count) ? layerFOH[num] : (-1));
					break;
				case LayerModes.Master:
					num = offsetMaster + l;
					num2 = ((num < layerMaster.Count) ? layerMaster[num] : (-1));
					if (num2 >= 0 && faderMode == FaderModes.MOF && MixEngine.chData[num2].channelType != ChannelTypes.Mix)
					{
						num2 = -1;
					}
					break;
				case LayerModes.Custom:
					num = offsetCustom + l;
					num2 = ((num < layerCustom.Count) ? layerCustom[num] : (-1));
					if (num2 >= 0 && faderMode == FaderModes.SOF && MixEngine.chData[num2].channelType != ChannelTypes.Input && MixEngine.chData[num2].channelType != ChannelTypes.STIN)
					{
						num2 = -1;
					}
					if (num2 >= 0 && faderMode == FaderModes.MOF)
					{
						if (Console.type != ConsoleTypes.M7CL && (MixEngine.chData[num2].channelType == ChannelTypes.Input || MixEngine.chData[num2].channelType == ChannelTypes.STIN))
						{
							num2 = -1;
						}
						else if (MixEngine.chData[num2].channelType == ChannelTypes.Matrix || MixEngine.chData[num2].channelType == ChannelTypes.Stereo)
						{
							num2 = -1;
						}
					}
					break;
				case LayerModes.OutportDelay:
				case LayerModes.OutportAtt:
					num = offsetOutport + l;
					num2 = ((num < MixEngine.OutPort[0].getCount) ? num : (-1));
					break;
				case LayerModes.DCA:
					num = offsetDCA + l;
					num2 = ((num < MixEngine.DCA.Length) ? num : (-1));
					break;
				}
				if (num2 != -1 && layerMode != LayerModes.Custom && layerMode != LayerModes.OutportAtt && layerMode != LayerModes.OutportDelay && l != 0 && arrFaders[0].Channel != -1 && MixEngine.chData[arrFaders[0].Channel].channelType != MixEngine.chData[num2].channelType)
				{
					num2 = -1;
				}
				if (num2 != -1 && (layerMode == LayerModes.OutportAtt || layerMode == LayerModes.OutportDelay) && l != 0 && arrFaders[0].Channel != -1 && MixEngine.OutPort[arrFaders[0].Channel].type != MixEngine.OutPort[num2].type)
				{
					num2 = -1;
				}
				if (num2 != -1 && layerMode != LayerModes.Custom && num2 >= Settings.Default.maxChannels && MixEngine.chData[num2].channelType == ChannelTypes.Input)
				{
					num2 = -1;
				}
				if (num2 == -1)
				{
					arrFaders[l].MeterLevel = 0;
					arrFaders[l].Channel = -1;
					arrLabels[l].Text = "";
					arrLabels[l].Tag = -1;
					arrMutes[l].Tag = -1;
					arrCues[l].Tag = -1;
					arrPans[l].Channel = -1;
					arrNames[l].Text = "";
					arrInfo[l].Text = "";
					arrFaders[l].Hide();
					arrLabels[l].Hide();
					arrInfo[l].Hide();
					arrNames[l].Hide();
					arrMutes[l].Hide();
					arrCues[l].Hide();
					arrPans[l].Hide();
					arrPhantom[l].Hide();
					arrHAs[l].Hide();
					continue;
				}
				switch (faderMode)
				{
				case FaderModes.OutportDelay:
				case FaderModes.OutportAtt:
					arrFaders[l].Channel = num2;
					arrLabels[l].Text = MixEngine.OutPort[num2].hardname;
					arrNames[l].Text = MixEngine.OutPort[num2].hardname;
					arrMutes[l].Flag = "Outport";
					arrCues[l].Flag = "Outport";
					arrPans[l].Flag = "Outport";
					arrHAs[l].Flag = "Outport";
					arrFaders[l].Flag = "Outport";
					arrNames[l].ReadOnly = true;
					arrInfo[l].Text = ((double)((float)MixEngine.OutPort[num2]._delayTime / 100000f) * 1127.26).ToString("F1") + " ft\n" + ((double)((float)MixEngine.OutPort[num2]._delayTime / 100000f) * 343.59).ToString("F1") + " m";
					arrLabels[l].Tag = num2;
					arrMutes[l].Tag = num2;
					arrNames[l].Tag = num2;
					arrCues[l].Tag = num2;
					if (arrLayout.Contains("Label"))
					{
						arrLabels[l].Show();
					}
					else
					{
						arrLabels[l].Hide();
					}
					arrCues[l].Hide();
					arrInfo[l].Show();
					if (arrLayout.Contains("Name"))
					{
						arrNames[l].Show();
					}
					else
					{
						arrNames[l].Hide();
					}
					if (arrLayout.Contains("Mute"))
					{
						arrMutes[l].Show();
					}
					else
					{
						arrMutes[l].Hide();
					}
					if (arrLayout.Contains("Fader"))
					{
						arrFaders[l].Show();
					}
					else
					{
						arrFaders[l].Hide();
					}
					arrPans[l].Hide();
					arrHAs[l].Hide();
					arrPhantom[l].Hide();
					arrFaders[l].MeterLevel = 0;
					arrFaders[l].FaderColor = "Blue";
					arrLabels[l].BackColor = Settings.Default.c_button_inactive;
					arrLabels[l].ForeColor = Settings.Default.c_button_inactive_fore;
					switch (layerMode)
					{
					case LayerModes.OutportAtt:
						if (MixEngine.OutPort[num2].getParam(OutPort.Pr.Attenuation) != arrFaders[l].Value)
						{
							arrFaders[l].Value = MixEngine.OutPort[num2].getParam(OutPort.Pr.Attenuation);
						}
						if (MixEngine.OutPort[num2].getParamBool(OutPort.Pr.Phase))
						{
							arrMutes[l].BackColor = Settings.Default.c_button_active;
							arrMutes[l].ForeColor = Settings.Default.c_button_active_fore;
						}
						else
						{
							arrMutes[l].BackColor = Settings.Default.c_button_inactive;
							arrMutes[l].ForeColor = Settings.Default.c_button_inactive_fore;
						}
						arrMutes[l].Text = "Phase\nInvert";
						break;
					case LayerModes.OutportDelay:
						if (MixEngine.OutPort[num2].getParam(OutPort.Pr.DelayTime) != arrFaders[l].Value)
						{
							arrFaders[l].Value = MixEngine.OutPort[num2].getParam(OutPort.Pr.DelayTime);
						}
						if (MixEngine.OutPort[num2].getParamBool(OutPort.Pr.DelayOn))
						{
							arrMutes[l].BackColor = Settings.Default.c_button_active;
							arrMutes[l].ForeColor = Settings.Default.c_button_active_fore;
						}
						else
						{
							arrMutes[l].BackColor = Settings.Default.c_button_inactive;
							arrMutes[l].ForeColor = Settings.Default.c_button_inactive_fore;
						}
						arrMutes[l].Text = "Delay";
						break;
					}
					break;
				case FaderModes.DCA:
					arrFaders[l].Channel = num2;
					arrLabels[l].Text = MixEngine.DCA[num2]._name;
					arrInfo[l].Text = MixEngine.DCA[num2].hardname.Replace("Ch", "").Trim();
					arrNames[l].Text = MixEngine.DCA[num2].hardname;
					arrNames[l].ReadOnly = true;
					arrLabels[l].Tag = num2;
					arrMutes[l].Tag = num2;
					arrNames[l].Tag = num2;
					arrCues[l].Tag = num2;
					arrMutes[l].Flag = "DCA";
					arrCues[l].Flag = "DCA";
					arrPans[l].Flag = "DCA";
					arrHAs[l].Flag = "DCA";
					arrFaders[l].Flag = "DCA";
					if (arrLayout.Contains("Label"))
					{
						arrLabels[l].Show();
					}
					else
					{
						arrLabels[l].Hide();
					}
					arrCues[l].Hide();
					if (arrLayout.Contains("Info"))
					{
						arrInfo[l].Show();
					}
					else
					{
						arrInfo[l].Hide();
					}
					if (arrLayout.Contains("Name"))
					{
						arrNames[l].Show();
					}
					else
					{
						arrNames[l].Hide();
					}
					if (arrLayout.Contains("Mute"))
					{
						arrMutes[l].Show();
					}
					else
					{
						arrMutes[l].Hide();
					}
					if (arrLayout.Contains("Fader"))
					{
						arrFaders[l].Show();
					}
					else
					{
						arrFaders[l].Hide();
					}
					arrPans[l].Hide();
					arrHAs[l].Hide();
					arrPhantom[l].Hide();
					arrFaders[l].MeterLevel = 0;
					arrFaders[l].FaderColor = "Blue";
					arrLabels[l].BackColor = Settings.Default.c_button_inactive;
					arrLabels[l].ForeColor = Settings.Default.c_button_inactive_fore;
					if (MixEngine.DCA[num2].fader != arrFaders[l].Value)
					{
						arrFaders[l].Value = MixEngine.DCA[num2].fader;
					}
					setMuteColor(arrMutes[l], !MixEngine.DCA[num2]._muted, groupMute: false);
					break;
				case FaderModes.FOH:
				case FaderModes.HA:
				case FaderModes.Pan:
				{
					arrFaders[l].Channel = num2;
					arrLabels[l].Text = MixEngine.chData[num2].hardname.Replace("Ch", "").Trim();
					arrInfo[l].Text = MixEngine.chData[num2].hardname.Replace("Ch", "").Trim();
					arrNames[l].Text = MixEngine.chData[num2].name;
					arrNames[l].ReadOnly = false;
					arrLabels[l].Tag = num2;
					arrMutes[l].Tag = num2;
					arrNames[l].Tag = num2;
					arrCues[l].Tag = num2;
					arrPhantom[l].Tag = num2;
					arrPans[l].Channel = num2;
					arrHAs[l].Channel = num2;
					arrMutes[l].Flag = "";
					arrCues[l].Flag = "";
					arrPans[l].Flag = "";
					arrHAs[l].Flag = "";
					arrFaders[l].Flag = "";
					if (arrLayout.Contains("Label"))
					{
						arrLabels[l].Show();
					}
					else
					{
						arrLabels[l].Hide();
					}
					if (arrLayout.Contains("Cue"))
					{
						arrCues[l].Show();
					}
					else
					{
						arrCues[l].Hide();
					}
					if (arrLayout.Contains("Info"))
					{
						arrInfo[l].Show();
					}
					else
					{
						arrInfo[l].Hide();
					}
					if (arrLayout.Contains("Name"))
					{
						arrNames[l].Show();
					}
					else
					{
						arrNames[l].Hide();
					}
					if (arrLayout.Contains("Mute"))
					{
						arrMutes[l].Show();
					}
					else
					{
						arrMutes[l].Hide();
					}
					if (arrLayout.Contains("Fader"))
					{
						arrFaders[l].Show();
					}
					else
					{
						arrFaders[l].Hide();
					}
					if (MixEngine.chData[num2].channelType == ChannelTypes.Matrix || MixEngine.chData[num2].channelType == ChannelTypes.Mix || MixEngine.chData[num2].channelType == ChannelTypes.Stereo)
					{
						arrPans[l].Hide();
						arrHAs[l].Hide();
						arrPhantom[l].Hide();
					}
					else
					{
						if (arrLayout.Contains("Pan"))
						{
							arrPans[l].Show();
						}
						else
						{
							arrPans[l].Hide();
						}
						if (arrLayout.Contains("HA"))
						{
							arrHAs[l].Show();
						}
						else
						{
							arrHAs[l].Hide();
						}
						if (arrLayout.Contains("Phantom"))
						{
							arrPhantom[l].Show();
						}
						else
						{
							arrPhantom[l].Hide();
						}
					}
					arrFaders[l].MeterLevel = 0;
					switch (MixEngine.chData[num2].channelType)
					{
					case ChannelTypes.Input:
					case ChannelTypes.STIN:
						arrFaders[l].FaderColor = "White";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[l].ValueType.Equals("busfader"))
						{
							arrFaders[l].ValueType = "fader";
						}
						break;
					case ChannelTypes.Mix:
					case ChannelTypes.Matrix:
						arrFaders[l].FaderColor = "Purple";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[l].ValueType.Equals("fader"))
						{
							arrFaders[l].ValueType = "busfader";
						}
						break;
					case ChannelTypes.Stereo:
						arrFaders[l].FaderColor = "Red";
						if (Console.type == ConsoleTypes.O1V96V2 && arrFaders[l].ValueType.Equals("fader"))
						{
							arrFaders[l].ValueType = "busfader";
						}
						break;
					}
					switch (MixEngine.chData[num2].getParam(Channel.Pr.Group))
					{
					case 0:
						arrLabels[l].BackColor = Settings.Default.c_button_inactive;
						arrLabels[l].ForeColor = Settings.Default.c_button_inactive_fore;
						break;
					case 35:
						arrLabels[l].BackColor = Color.Plum;
						break;
					case 36:
						arrLabels[l].BackColor = Color.LightGreen;
						break;
					case 37:
						arrLabels[l].BackColor = Color.Salmon;
						break;
					case 38:
						arrLabels[l].BackColor = Color.SkyBlue;
						break;
					default:
						arrLabels[l].BackColor = Color.FromArgb(int.Parse("FF" + array[MixEngine.chData[num].getParam(Channel.Pr.Group) % 10], NumberStyles.AllowHexSpecifier));
						break;
					}
					bool groupMute2 = false;
					for (int m = 0; m < 8; m++)
					{
						if (MixEngine.muteGroup[m].members.Contains(num2) && MixEngine.getParam(m, MixEngine.Pr.MuteMaster, 0) == 1)
						{
							groupMute2 = true;
						}
					}
					setMuteColor(arrMutes[l], MixEngine.chData[num2].getParamBool(Channel.Pr.On), groupMute2);
					arrCues[l].BackColor = ((MixEngine.chData[num2].getParam(Channel.Pr.Cue) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrCues[l].ForeColor = ((MixEngine.chData[num2].getParam(Channel.Pr.Cue) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrPhantom[l].BackColor = ((MixEngine.getParam(0, MixEngine.Pr.Phantom, MixEngine.chData[arrFaders[l].Channel].getParam(Channel.Pr.PatchIn) - 1) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrPhantom[l].ForeColor = ((MixEngine.getParam(0, MixEngine.Pr.Phantom, MixEngine.chData[arrFaders[l].Channel].getParam(Channel.Pr.PatchIn) - 1) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrHAs[l].Value = MixEngine.getParam(0, MixEngine.Pr.HA, MixEngine.chData[arrFaders[l].Channel].getParam(Channel.Pr.PatchIn) - 1);
					arrPans[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.Pan);
					switch (faderMode)
					{
					case FaderModes.FOH:
						if (MixEngine.chData[num2].getParam(Channel.Pr.Fader) != arrFaders[l].Value)
						{
							arrFaders[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.Fader);
						}
						break;
					case FaderModes.HA:
						if (MixEngine.chData[num2].getParam(Channel.Pr.HA) != arrFaders[l].ValueInt)
						{
							arrFaders[l].ValueInt = MixEngine.chData[num2].getParam(Channel.Pr.HA);
						}
						break;
					case FaderModes.Pan:
						if (MixEngine.chData[num2].getParam(Channel.Pr.Pan) != arrFaders[l].Value)
						{
							arrFaders[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.Pan);
						}
						break;
					}
					break;
				}
				case FaderModes.SOF:
				case FaderModes.MOF:
					arrFaders[l].Channel = num2;
					arrLabels[l].Text = MixEngine.chData[num2].hardname.Replace("Ch", "").Trim();
					arrInfo[l].Text = MixEngine.chData[num2].hardname.Replace("Ch", "").Trim();
					arrNames[l].Text = MixEngine.chData[num2].name;
					arrNames[l].ReadOnly = false;
					arrLabels[l].Tag = num2;
					arrMutes[l].Tag = num2;
					arrNames[l].Tag = num2;
					arrCues[l].Tag = num2;
					arrPhantom[l].Tag = num2;
					arrPans[l].Channel = num2;
					arrHAs[l].Channel = num2;
					arrMutes[l].Flag = "";
					arrCues[l].Flag = "";
					arrPans[l].Flag = "";
					arrHAs[l].Flag = "";
					arrFaders[l].Flag = "";
					if (arrLayout.Contains("Label"))
					{
						arrLabels[l].Show();
					}
					else
					{
						arrLabels[l].Hide();
					}
					if (arrLayout.Contains("Cue"))
					{
						arrCues[l].Show();
					}
					else
					{
						arrCues[l].Hide();
					}
					if (arrLayout.Contains("Info"))
					{
						arrInfo[l].Show();
					}
					else
					{
						arrInfo[l].Hide();
					}
					if (arrLayout.Contains("Name"))
					{
						arrNames[l].Show();
					}
					else
					{
						arrNames[l].Hide();
					}
					if (arrLayout.Contains("Mute"))
					{
						arrMutes[l].Show();
					}
					else
					{
						arrMutes[l].Hide();
					}
					if (arrLayout.Contains("Fader"))
					{
						arrFaders[l].Show();
					}
					else
					{
						arrFaders[l].Hide();
					}
					if (MixEngine.chData[num2].channelType == ChannelTypes.Matrix || MixEngine.chData[num2].channelType == ChannelTypes.Mix || MixEngine.chData[num2].channelType == ChannelTypes.Stereo)
					{
						arrPans[l].Hide();
						arrHAs[l].Hide();
						arrPhantom[l].Hide();
					}
					else
					{
						if (arrLayout.Contains("Pan"))
						{
							if (faderMode == FaderModes.SOF && MixEngine.chData[SOFPage + MixEngine.chOffsetMix].getParam(Channel.Pr.Pair) > 0)
							{
								arrPans[l].Show();
							}
							else if (faderMode == FaderModes.MOF && MixEngine.chData[MOFPage + MixEngine.chOffsetMatrix].getParam(Channel.Pr.Pair) > 0)
							{
								arrPans[l].Show();
							}
							else
							{
								arrPans[l].Hide();
							}
						}
						else
						{
							arrPans[l].Hide();
						}
						if (arrLayout.Contains("HA"))
						{
							arrHAs[l].Show();
						}
						else
						{
							arrHAs[l].Hide();
						}
						if (arrLayout.Contains("Phantom"))
						{
							arrPhantom[l].Show();
						}
						else
						{
							arrPhantom[l].Hide();
						}
					}
					arrFaders[l].MeterLevel = 0;
					switch (faderMode)
					{
					case FaderModes.SOF:
						switch (MixEngine.chData[num2].channelType)
						{
						case ChannelTypes.Input:
						case ChannelTypes.STIN:
							arrFaders[l].FaderColor = "Orange";
							break;
						case ChannelTypes.Mix:
						case ChannelTypes.Matrix:
							arrFaders[l].FaderColor = "Missing";
							arrMutes[l].Hide();
							break;
						case ChannelTypes.Stereo:
							arrFaders[l].FaderColor = "Missing";
							arrMutes[l].Hide();
							break;
						}
						break;
					case FaderModes.MOF:
						switch (MixEngine.chData[num2].channelType)
						{
						case ChannelTypes.Input:
						case ChannelTypes.STIN:
							if (Console.type == ConsoleTypes.M7CL)
							{
								arrFaders[l].FaderColor = "Orange";
								arrMutes[l].Show();
							}
							else
							{
								arrFaders[l].FaderColor = "Missing";
								arrMutes[l].Hide();
							}
							break;
						case ChannelTypes.Mix:
							arrFaders[l].FaderColor = "Orange";
							break;
						case ChannelTypes.Matrix:
							arrFaders[l].FaderColor = "Missing";
							break;
						case ChannelTypes.Stereo:
							arrFaders[l].FaderColor = "Missing";
							break;
						}
						break;
					}
					switch (MixEngine.chData[num2].getParam(Channel.Pr.Group))
					{
					case 0:
						arrLabels[l].BackColor = Settings.Default.c_button_inactive;
						arrLabels[l].ForeColor = Settings.Default.c_button_inactive_fore;
						break;
					case 35:
						arrLabels[l].BackColor = Color.Plum;
						break;
					case 36:
						arrLabels[l].BackColor = Color.LightGreen;
						break;
					case 37:
						arrLabels[l].BackColor = Color.Salmon;
						break;
					case 38:
						arrLabels[l].BackColor = Color.SkyBlue;
						break;
					default:
						arrLabels[l].BackColor = Color.FromArgb(int.Parse("FF" + array[MixEngine.chData[num].getParam(Channel.Pr.Group) % 10], NumberStyles.AllowHexSpecifier));
						break;
					}
					if (faderMode == FaderModes.SOF)
					{
						setMuteColor(arrMutes[l], MixEngine.chData[num].getParam(Channel.Pr.SendToMixOn, SOFPage) == 1, groupMute: false);
					}
					else if (faderMode == FaderModes.MOF)
					{
						setMuteColor(arrMutes[l], MixEngine.chData[num].getParam(Channel.Pr.SendToMatrixOn, MOFPage) == 1, groupMute: false);
					}
					arrCues[l].BackColor = ((MixEngine.chData[num2].getParam(Channel.Pr.Cue) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrCues[l].ForeColor = ((MixEngine.chData[num2].getParam(Channel.Pr.Cue) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrPhantom[l].BackColor = ((MixEngine.getParam(0, MixEngine.Pr.Phantom, MixEngine.chData[arrFaders[l].Channel].getParam(Channel.Pr.PatchIn) - 1) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrPhantom[l].ForeColor = ((MixEngine.getParam(0, MixEngine.Pr.Phantom, MixEngine.chData[arrFaders[l].Channel].getParam(Channel.Pr.PatchIn) - 1) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrHAs[l].Value = MixEngine.getParam(0, MixEngine.Pr.HA, MixEngine.chData[arrFaders[l].Channel].getParam(Channel.Pr.PatchIn) - 1);
					if (faderMode == FaderModes.SOF)
					{
						arrPans[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.SendToMixPan, SOFPage);
					}
					else if (faderMode == FaderModes.MOF)
					{
						arrPans[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.SendToMatrixPan, MOFPage);
					}
					if (faderMode == FaderModes.SOF && MixEngine.chData[num2].getParam(Channel.Pr.SendToMix, SOFPage) != arrFaders[l].Value)
					{
						arrFaders[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.SendToMix, SOFPage);
					}
					if (faderMode == FaderModes.MOF && MixEngine.chData[num2].getParam(Channel.Pr.SendToMatrix, MOFPage) != arrFaders[l].Value)
					{
						arrFaders[l].Value = MixEngine.chData[num2].getParam(Channel.Pr.SendToMatrix, MOFPage);
					}
					break;
				}
			}
			_ = showMeter;
		}
		catch (Exception ex)
		{
			logError("UpdateUI()", "UI", ex.ToString());
		}
	}

	private void switchFaderMode(FaderModes newMode)
	{
		int faderCount = Settings.Default.faderCount;
		int num = 0;
		switch (newMode)
		{
		case FaderModes.FOH:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			for (int m = 0; m < arrFaders.Count; m++)
			{
				arrFaders[m].Minimum = 0;
				arrFaders[m].Maximum = 1023;
				arrFaders[m].ValueType = "fader";
				arrFaders[m].ShowKnob = false;
				arrFaders[m].FaderTails = true;
				if (m < arrFaders.Count - masterSection.Count)
				{
					arrFaders[m].Flag = "Channel";
				}
				else
				{
					arrFaders[m].Flag = "Master";
				}
			}
			faderMode = FaderModes.FOH;
			if (layerMode != 0 && layerMode != LayerModes.Master && layerMode != LayerModes.Custom)
			{
				switchLayerMode(LayerModes.FOH);
			}
			break;
		}
		case FaderModes.DCA:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			for (int num13 = 0; num13 < arrFaders.Count; num13++)
			{
				arrFaders[num13].Minimum = 0;
				arrFaders[num13].Maximum = 1023;
				arrFaders[num13].ValueType = "fader";
				arrFaders[num13].ShowKnob = false;
				arrFaders[num13].FaderTails = true;
				if (num13 < arrFaders.Count - masterSection.Count)
				{
					arrFaders[num13].Flag = "DCA";
				}
				else
				{
					arrFaders[num13].Flag = "Master";
				}
			}
			faderMode = FaderModes.DCA;
			if (layerMode != LayerModes.DCA)
			{
				switchLayerMode(LayerModes.DCA);
			}
			break;
		}
		case FaderModes.HA:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_active;
			btnPageHA.ForeColor = Settings.Default.c_button_active_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			for (int num11 = 0; num11 < arrFaders.Count - masterSection.Count; num11++)
			{
				arrFaders[num11].Minimum = 0;
				arrFaders[num11].Maximum = 72;
				arrFaders[num11].KnobColor = "White";
				arrFaders[num11].ValueType = "ha";
				arrFaders[num11].ShowKnob = true;
				arrFaders[num11].FaderTails = true;
				arrFaders[num11].Flag = "HA";
			}
			for (int num12 = arrFaders.Count - masterSection.Count; num12 < arrFaders.Count; num12++)
			{
				arrFaders[num12].Minimum = 0;
				arrFaders[num12].Maximum = 1023;
				arrFaders[num12].ValueType = "fader";
				arrFaders[num12].ShowKnob = false;
				arrFaders[num12].FaderTails = true;
				arrFaders[num12].Flag = "Master";
			}
			faderMode = FaderModes.HA;
			if (layerMode != 0 && layerMode != LayerModes.Custom)
			{
				switchLayerMode(LayerModes.FOH);
			}
			break;
		}
		case FaderModes.Pan:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_active;
			btnPagePan.ForeColor = Settings.Default.c_button_active_fore;
			for (int num14 = 0; num14 < arrFaders.Count - masterSection.Count; num14++)
			{
				arrFaders[num14].Minimum = -63;
				arrFaders[num14].Maximum = 63;
				arrFaders[num14].KnobColor = "Red";
				arrFaders[num14].ValueType = "pan";
				arrFaders[num14].ShowKnob = true;
				arrFaders[num14].FaderTails = false;
				arrFaders[num14].Flag = "Pan";
			}
			for (int num15 = arrFaders.Count - masterSection.Count; num15 < arrFaders.Count; num15++)
			{
				arrFaders[num15].Minimum = 0;
				arrFaders[num15].Maximum = 1023;
				arrFaders[num15].ValueType = "fader";
				arrFaders[num15].ShowKnob = false;
				arrFaders[num15].FaderTails = true;
				arrFaders[num15].Flag = "Master";
			}
			faderMode = FaderModes.Pan;
			if (layerMode != 0 && layerMode != LayerModes.Master && layerMode != LayerModes.Custom)
			{
				switchLayerMode(LayerModes.FOH);
			}
			break;
		}
		case FaderModes.OutportAtt:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHome.BackColor = Settings.Default.c_button_inactive;
			btnPageHome.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMaster.BackColor = Settings.Default.c_button_inactive;
			btnPageMaster.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageCustom.BackColor = Settings.Default.c_button_inactive;
			btnPageCustom.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageDCA.BackColor = Settings.Default.c_button_inactive;
			btnPageDCA.ForeColor = Settings.Default.c_button_inactive_fore;
			for (int num6 = 0; num6 < faderCount; num6++)
			{
				arrFaders[num6].Minimum = -960;
				arrFaders[num6].Maximum = 240;
				arrFaders[num6].KnobColor = "White";
				arrFaders[num6].ValueType = "div10";
				arrFaders[num6].ShowKnob = true;
				arrFaders[num6].FaderTails = false;
				arrFaders[num6].Flag = "OutportAtt";
			}
			for (int num7 = arrFaders.Count - masterSection.Count; num7 < arrFaders.Count; num7++)
			{
				arrFaders[num7].Minimum = 0;
				arrFaders[num7].Maximum = 1023;
				arrFaders[num7].ValueType = "fader";
				arrFaders[num7].ShowKnob = false;
				arrFaders[num7].FaderTails = true;
				arrFaders[num7].Flag = "Master";
			}
			layerMode = LayerModes.OutportAtt;
			faderMode = FaderModes.OutportAtt;
			num = 0;
			for (int num8 = 0; (double)num8 < Math.Ceiling((double)Console.maxOutOmni / (double)faderCount); num8++)
			{
				panelLayer.Controls[num].Text = MixEngine.OutPort[num8 * faderCount].hardname;
				((AirButton)panelLayer.Controls[num]).BackText = "Port";
				panelLayer.Controls[num].Show();
				panelLayer.Controls[num].Tag = num8 * faderCount;
				num++;
			}
			for (int num9 = 0; (double)num9 < Math.Ceiling((double)Console.maxOutSlot / (double)faderCount); num9++)
			{
				panelLayer.Controls[num].Text = MixEngine.OutPort[num9 * faderCount + Console.maxOutOmni].hardname;
				((AirButton)panelLayer.Controls[num]).BackText = "Port";
				panelLayer.Controls[num].Show();
				panelLayer.Controls[num].Tag = num9 * faderCount + Console.maxOutOmni;
				num++;
			}
			for (int num10 = 0; num10 < 1; num10++)
			{
				panelLayer.Controls[num].Text = "2TR";
				((AirButton)panelLayer.Controls[num]).BackText = "Port";
				panelLayer.Controls[num].Show();
				panelLayer.Controls[num].Tag = num10 * faderCount + Console.maxOutOmni + Console.maxOutSlot;
				num++;
			}
			while (num < panelLayer.Controls.Count)
			{
				panelLayer.Controls[num++].Hide();
			}
			break;
		}
		case FaderModes.OutportDelay:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHome.BackColor = Settings.Default.c_button_inactive;
			btnPageHome.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMaster.BackColor = Settings.Default.c_button_inactive;
			btnPageMaster.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageCustom.BackColor = Settings.Default.c_button_inactive;
			btnPageCustom.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageDCA.BackColor = Settings.Default.c_button_inactive;
			btnPageDCA.ForeColor = Settings.Default.c_button_inactive_fore;
			for (int n = 0; n < faderCount; n++)
			{
				arrFaders[n].Minimum = 0;
				arrFaders[n].Maximum = 60000;
				arrFaders[n].KnobColor = "White";
				arrFaders[n].ValueType = "div100";
				arrFaders[n].ShowKnob = true;
				arrFaders[n].FaderTails = false;
				arrFaders[n].Flag = "OutportDelay";
			}
			for (int num2 = arrFaders.Count - masterSection.Count; num2 < arrFaders.Count; num2++)
			{
				arrFaders[num2].Minimum = 0;
				arrFaders[num2].Maximum = 1023;
				arrFaders[num2].ValueType = "fader";
				arrFaders[num2].ShowKnob = false;
				arrFaders[num2].FaderTails = true;
				arrFaders[num2].Flag = "Master";
			}
			layerMode = LayerModes.OutportDelay;
			faderMode = FaderModes.OutportDelay;
			num = 0;
			for (int num3 = 0; (double)num3 < Math.Ceiling((double)Console.maxOutOmni / (double)faderCount); num3++)
			{
				panelLayer.Controls[num].Text = MixEngine.OutPort[num3 * faderCount].hardname;
				((AirButton)panelLayer.Controls[num]).BackText = "Port";
				panelLayer.Controls[num].Show();
				panelLayer.Controls[num].Tag = num3 * faderCount;
				num++;
			}
			for (int num4 = 0; (double)num4 < Math.Ceiling((double)Console.maxOutSlot / (double)faderCount); num4++)
			{
				panelLayer.Controls[num].Text = MixEngine.OutPort[num4 * faderCount + Console.maxOutOmni].hardname;
				((AirButton)panelLayer.Controls[num]).BackText = "Port";
				panelLayer.Controls[num].Show();
				panelLayer.Controls[num].Tag = num4 * faderCount + Console.maxOutOmni;
				num++;
			}
			for (int num5 = 0; num5 < 1; num5++)
			{
				panelLayer.Controls[num].Text = "2TR";
				panelLayer.Controls[num].Show();
				panelLayer.Controls[num].Tag = num5 * faderCount + Console.maxOutOmni + Console.maxOutSlot;
				((AirButton)panelLayer.Controls[num]).BackText = "Port";
				num++;
			}
			while (num < panelLayer.Controls.Count)
			{
				panelLayer.Controls[num++].Hide();
			}
			break;
		}
		case FaderModes.SOF:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_active;
			btnPageSOF.ForeColor = Settings.Default.c_button_active_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_inactive;
			btnPageMOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			lblPanelHome.Text = "Sends on Faders";
			for (int k = 0; k < arrBtnUDF.Count; k++)
			{
				if (MixEngine.chData[MixEngine.chOffsetMix + k].name.Contains("Mx "))
				{
					arrBtnUDF[k].Text = MixEngine.chData[MixEngine.chOffsetMix + k].name;
				}
				else
				{
					arrBtnUDF[k].Text = k + 1 + "\n" + MixEngine.chData[MixEngine.chOffsetMix + k].name;
				}
				arrBtnUDF[k].Tag = "mix:" + k;
			}
			for (int l = 0; l < arrFaders.Count; l++)
			{
				arrFaders[l].Minimum = 0;
				arrFaders[l].Maximum = 1023;
				arrFaders[l].ValueType = "fader";
				arrFaders[l].ShowKnob = false;
				arrFaders[l].FaderTails = true;
				if (l < arrFaders.Count - masterSection.Count)
				{
					arrFaders[l].Flag = "SOF";
				}
				else
				{
					arrFaders[l].Flag = "Master";
				}
			}
			faderMode = FaderModes.SOF;
			if (layerMode != 0 && layerMode != LayerModes.Master && layerMode != LayerModes.Custom)
			{
				switchLayerMode(LayerModes.FOH);
			}
			break;
		}
		case FaderModes.MOF:
		{
			btnPageSOF.BackColor = Settings.Default.c_button_inactive;
			btnPageSOF.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPageMOF.BackColor = Settings.Default.c_button_active;
			btnPageMOF.ForeColor = Settings.Default.c_button_active_fore;
			btnPageHA.BackColor = Settings.Default.c_button_inactive;
			btnPageHA.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPagePan.BackColor = Settings.Default.c_button_inactive;
			btnPagePan.ForeColor = Settings.Default.c_button_inactive_fore;
			lblPanelHome.Text = "Matrix on Faders";
			for (int i = 0; i < arrBtnUDF.Count; i++)
			{
				if (i < 8)
				{
					if (MixEngine.chData[MixEngine.chOffsetMatrix + i].name.Contains("Mt "))
					{
						arrBtnUDF[i].Text = MixEngine.chData[MixEngine.chOffsetMatrix + i].name;
					}
					else
					{
						arrBtnUDF[i].Text = i + 1 + "\n" + MixEngine.chData[MixEngine.chOffsetMatrix + i].name;
					}
					arrBtnUDF[i].Tag = "matrix:" + i;
				}
				else
				{
					arrBtnUDF[i].Tag = "";
				}
			}
			for (int j = 0; j < arrFaders.Count; j++)
			{
				arrFaders[j].Minimum = 0;
				arrFaders[j].Maximum = 1023;
				arrFaders[j].ValueType = "fader";
				arrFaders[j].ShowKnob = false;
				arrFaders[j].FaderTails = true;
				if (j < arrFaders.Count - masterSection.Count)
				{
					arrFaders[j].Flag = "MOF";
				}
				else
				{
					arrFaders[j].Flag = "Master";
				}
			}
			faderMode = FaderModes.MOF;
			if (layerMode != 0 && layerMode != LayerModes.Master && layerMode != LayerModes.Custom)
			{
				switchLayerMode(LayerModes.FOH);
			}
			break;
		}
		}
		syncUDFState();
		updateUI();
	}

	private void switchLayerMode(LayerModes newMode)
	{
		try
		{
			int faderCount = Settings.Default.faderCount;
			int num = 0;
			int num2 = 0;
			switch (newMode)
			{
			case LayerModes.FOH:
			{
				layerMode = LayerModes.FOH;
				btnPageHome.BackColor = Settings.Default.c_button_active;
				btnPageHome.ForeColor = Settings.Default.c_button_active_fore;
				btnPageMaster.BackColor = Settings.Default.c_button_inactive;
				btnPageMaster.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageCustom.BackColor = Settings.Default.c_button_inactive;
				btnPageCustom.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageDCA.BackColor = Settings.Default.c_button_inactive;
				btnPageDCA.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageSOF.Show();
				if (Console.type == ConsoleTypes.M7CL)
				{
					btnPageMOF.Show();
				}
				else
				{
					btnPageMOF.Hide();
				}
				num = 0;
				num2 = layerFOH.Count - Console.maxSTIN;
				if (num2 / faderCount > panelLayer.Controls.Count)
				{
					num2 = panelLayer.Controls.Count * faderCount;
				}
				for (; num * faderCount < num2; num++)
				{
					panelLayer.Controls[num].Text = num * faderCount + 1 + " - " + (((num + 1) * faderCount > num2) ? num2.ToString() : ((num + 1) * faderCount).ToString());
					((AirButton)panelLayer.Controls[num]).BackText = "";
					panelLayer.Controls[num].Show();
					panelLayer.Controls[num].Tag = num * faderCount;
				}
				for (int i = 0; i < Console.maxSTIN; i += faderCount)
				{
					if (num < panelLayer.Controls.Count)
					{
						panelLayer.Controls[num].Text = i + 1 + " - " + ((i + faderCount > Console.maxSTIN) ? (Console.maxSTIN / 2).ToString() : ((i + faderCount) / 2).ToString());
						((AirButton)panelLayer.Controls[num]).BackText = "ST IN";
						panelLayer.Controls[num].Show();
						panelLayer.Controls[num].Tag = num2 + i;
						num++;
					}
				}
				while (num < panelLayer.Controls.Count)
				{
					panelLayer.Controls[num++].Hide();
				}
				if (faderMode != 0 && faderMode != FaderModes.HA && faderMode == FaderModes.MOF && Console.type != ConsoleTypes.M7CL && faderMode != FaderModes.Pan && faderMode != FaderModes.SOF)
				{
					switchFaderMode(FaderModes.FOH);
				}
				break;
			}
			case LayerModes.Master:
			{
				layerMode = LayerModes.Master;
				btnPageMaster.BackColor = Settings.Default.c_button_active;
				btnPageMaster.ForeColor = Settings.Default.c_button_active_fore;
				btnPageHome.BackColor = Settings.Default.c_button_inactive;
				btnPageHome.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageCustom.BackColor = Settings.Default.c_button_inactive;
				btnPageCustom.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageDCA.BackColor = Settings.Default.c_button_inactive;
				btnPageDCA.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageSOF.Hide();
				btnPageMOF.Show();
				num = 0;
				for (int j = 0; (double)j < Math.Ceiling((double)Console.maxMIX / (double)faderCount); j++)
				{
					panelLayer.Controls[num].Text = j * (arrFaders.Count - masterSection.Count) + 1 + " - " + (((j + 1) * (arrFaders.Count - masterSection.Count) < Console.maxMIX) ? ((j + 1) * (arrFaders.Count - masterSection.Count)).ToString() : Console.maxMIX.ToString());
					((AirButton)panelLayer.Controls[num]).BackText = "Mix";
					panelLayer.Controls[num].Show();
					panelLayer.Controls[num].Tag = j * faderCount;
					num++;
				}
				for (int k = 0; (double)k < Math.Ceiling((double)Console.maxMATRIX / (double)faderCount); k++)
				{
					panelLayer.Controls[num].Text = k * (arrFaders.Count - masterSection.Count) + 1 + " - " + (((k + 1) * (arrFaders.Count - masterSection.Count) < Console.maxMATRIX) ? ((k + 1) * (arrFaders.Count - masterSection.Count)).ToString() : Console.maxMATRIX.ToString());
					((AirButton)panelLayer.Controls[num]).BackText = "Matrix";
					panelLayer.Controls[num].Show();
					panelLayer.Controls[num].Tag = k * faderCount + Console.maxMIX;
					num++;
				}
				for (int l = 0; (double)l < Math.Ceiling(3.0 / (double)faderCount); l++)
				{
					panelLayer.Controls[num].Text = "Stereo";
					((AirButton)panelLayer.Controls[num]).BackText = "";
					panelLayer.Controls[num].Show();
					panelLayer.Controls[num].Tag = l * faderCount + Console.maxMIX + Console.maxMATRIX;
					num++;
				}
				while (num < panelLayer.Controls.Count)
				{
					panelLayer.Controls[num++].Hide();
				}
				if (faderMode != 0 && faderMode != FaderModes.MOF && faderMode != FaderModes.Pan)
				{
					switchFaderMode(FaderModes.FOH);
				}
				break;
			}
			case LayerModes.Custom:
				layerMode = LayerModes.Custom;
				btnPageCustom.BackColor = Settings.Default.c_button_active;
				btnPageCustom.ForeColor = Settings.Default.c_button_active_fore;
				btnPageHome.BackColor = Settings.Default.c_button_inactive;
				btnPageHome.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageMaster.BackColor = Settings.Default.c_button_inactive;
				btnPageMaster.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageDCA.BackColor = Settings.Default.c_button_inactive;
				btnPageDCA.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageSOF.Show();
				btnPageMOF.Show();
				for (num = 0; num < panelLayer.Controls.Count; num++)
				{
					if (num * faderCount < layerCustom.Count)
					{
						panelLayer.Controls[num].Text = num * faderCount + 1 + " - " + (num + 1) * faderCount;
						((AirButton)panelLayer.Controls[num]).BackText = "";
						panelLayer.Controls[num].Show();
						panelLayer.Controls[num].Tag = num * faderCount;
					}
					else
					{
						panelLayer.Controls[num].Hide();
					}
				}
				if (faderMode != 0 && faderMode != FaderModes.HA && faderMode != FaderModes.MOF && faderMode != FaderModes.Pan && faderMode != FaderModes.SOF)
				{
					switchFaderMode(FaderModes.FOH);
				}
				break;
			case LayerModes.DCA:
				layerMode = LayerModes.DCA;
				btnPageDCA.BackColor = Settings.Default.c_button_active;
				btnPageDCA.ForeColor = Settings.Default.c_button_active_fore;
				btnPageHome.BackColor = Settings.Default.c_button_inactive;
				btnPageHome.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageMaster.BackColor = Settings.Default.c_button_inactive;
				btnPageMaster.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageCustom.BackColor = Settings.Default.c_button_inactive;
				btnPageCustom.ForeColor = Settings.Default.c_button_inactive_fore;
				btnPageSOF.Hide();
				btnPageMOF.Hide();
				for (num = 0; num < panelLayer.Controls.Count; num++)
				{
					if (num == 0)
					{
						panelLayer.Controls[num].Text = "1 - 8";
						((AirButton)panelLayer.Controls[num]).BackText = "DCA";
						panelLayer.Controls[num].Show();
						panelLayer.Controls[num].Tag = num * faderCount;
					}
					else
					{
						panelLayer.Controls[num].Hide();
					}
				}
				if (faderMode != FaderModes.DCA)
				{
					switchFaderMode(FaderModes.DCA);
				}
				break;
			}
			syncUDFState();
			updateUI();
		}
		catch (Exception ex)
		{
			logError("SwitchFaderMode()", "UI", ex.ToString());
		}
	}

	public void numFaders_Change()
	{
		try
		{
			foreach (Form openForm in Application.OpenForms)
			{
				if (openForm is frmChannel)
				{
					((frmChannel)openForm).setColors();
				}
			}
			Color backColor = ((!((double)(BackColor = Settings.Default.c_background).GetBrightness() > 0.2)) ? RGBHSL.ModifyBrightness(BackColor, 1.5) : RGBHSL.ModifyBrightness(BackColor, 0.8));
			lblBrand.BackColor = backColor;
			panelLayer.BackColor = backColor;
			panelLCD.BackColor = backColor;
			panelTopR.BackColor = backColor;
			panelHome.BackColor = backColor;
			panelConsole.BackColor = backColor;
			label4.ForeColor = Settings.Default.c_foreground;
			label5.ForeColor = Settings.Default.c_foreground;
			lblPanelHome.ForeColor = Settings.Default.c_foreground;
			foreach (Control control4 in panelTopR.Controls)
			{
				if (control4 is AirButton)
				{
					((AirButton)control4).ParentColor = panelTopR.BackColor;
					((AirButton)control4).BackColor = Settings.Default.c_button_inactive;
					((AirButton)control4).ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			if (fineMode)
			{
				btnFine.BackColor = Settings.Default.c_button_active;
				btnFine.ForeColor = Settings.Default.c_button_active_fore;
			}
			foreach (Control control5 in panelConsole.Controls)
			{
				if (control5 is AirButton)
				{
					((AirButton)control5).ParentColor = panelConsole.BackColor;
					((AirButton)control5).BackColor = Settings.Default.c_button_inactive;
					((AirButton)control5).ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			foreach (Control control6 in panelHome.Controls)
			{
				if (control6 is AirButton)
				{
					((AirButton)control6).ParentColor = panelHome.BackColor;
				}
			}
			offsetFOH = 0;
			offsetCustom = 0;
			offsetMaster = 0;
			bAllowPaint = false;
			if (isDebug)
			{
				logDebug("Setting Home Layer for Console Type");
			}
			switch (Settings.Default.ConsoleType)
			{
			case "Yamaha LS9-32":
			case "Yamaha LS9-16":
			{
				btnPageDCA.Hide();
				layerFOH.Clear();
				for (int k = 0; k < Console.maxInputs - Console.maxSTIN && k < Settings.Default.maxChannels; k++)
				{
					layerFOH.Add(k);
				}
				for (int l = MixEngine.chOffsetSTIN; l < Console.maxSTIN + MixEngine.chOffsetSTIN; l++)
				{
					layerFOH.Add(l);
				}
				layerMaster.Clear();
				layerMaster = new List<int>
				{
					72, 73, 74, 75, 76, 77, 78, 79, 80, 81,
					82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
					92, 93, 94, 95, 98, -1, 96, 97
				};
				break;
			}
			case "Yamaha M7CL-32":
			case "Yamaha M7CL-48":
			{
				btnPageDCA.Show();
				layerFOH.Clear();
				for (int num = 0; num < Console.maxInputs - Console.maxSTIN && num < Settings.Default.maxChannels; num++)
				{
					layerFOH.Add(num);
				}
				for (int num2 = MixEngine.chOffsetSTIN; num2 < Console.maxSTIN + MixEngine.chOffsetSTIN; num2++)
				{
					layerFOH.Add(num2);
				}
				layerMaster.Clear();
				layerMaster = new List<int>
				{
					56, 57, 58, 59, 60, 61, 62, 63, 64, 65,
					66, 67, 68, 69, 70, 71, 72, 73, 74, 75,
					76, 77, 78, 79, 82, -1, 80, 81
				};
				break;
			}
			case "Yamaha 01V96":
			case "Yamaha 01V96i":
			{
				btnPageDCA.Hide();
				layerFOH.Clear();
				for (int m = 0; m < Console.maxInputs - Console.maxSTIN && m < Settings.Default.maxChannels; m++)
				{
					layerFOH.Add(m);
				}
				for (int n = MixEngine.chOffsetSTIN; n < Console.maxSTIN + MixEngine.chOffsetSTIN; n++)
				{
					layerFOH.Add(n);
				}
				layerMaster.Clear();
				layerMaster = new List<int>
				{
					40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
					50, 51, 52, 53, 54, 55, 56, 57
				};
				break;
			}
			default:
			{
				btnPageDCA.Hide();
				layerFOH.Clear();
				for (int i = 0; i < Console.maxInputs - Console.maxSTIN && i < Settings.Default.maxChannels; i++)
				{
					layerFOH.Add(i);
				}
				for (int j = MixEngine.chOffsetSTIN; j < Console.maxSTIN + MixEngine.chOffsetSTIN; j++)
				{
					layerFOH.Add(j);
				}
				layerMaster.Clear();
				layerMaster = new List<int>
				{
					72, 73, 74, 75, 76, 77, 78, 79, 80, 81,
					82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
					92, 93, 94, 95, 98, -1, 96, 97
				};
				break;
			}
			}
			_ = SystemInformation.PrimaryMonitorSize.Width;
			_ = SystemInformation.PrimaryMonitorSize.Width;
			if (isDebug)
			{
				logDebug("Setting Up FaderCount. Current Value: " + Settings.Default.faderCount);
			}
			if (Settings.Default.faderCount <= 0)
			{
				Settings.Default.faderCount = 8;
				Settings.Default.Save();
			}
			int faderCount = Settings.Default.faderCount;
			if (isDebug)
			{
				logDebug("NumFaders is now: " + Settings.Default.faderCount);
			}
			panelFaders.Controls.Add(panelLayer);
			if (isDebug)
			{
				logDebug("ArrFaders.Count=" + arrFaders.Count);
			}
			if (isDebug)
			{
				logDebug("Remove Extra Faders");
			}
			for (int num3 = arrFaders.Count - 1; num3 >= faderCount + masterSection.Count; num3--)
			{
				if (isDebug)
				{
					logDebug("-- Remove Fader: FaderNum=" + num3 + ", arrFaders.Count=" + arrFaders.Count);
				}
				panelFaders.Controls.Remove(arrMutes[num3]);
				arrMutes.RemoveAt(num3);
				panelFaders.Controls.Remove(arrPans[num3]);
				arrPans.RemoveAt(num3);
				panelFaders.Controls.Remove(arrPhantom[num3]);
				arrPhantom.RemoveAt(num3);
				panelFaders.Controls.Remove(arrHAs[num3]);
				arrHAs.RemoveAt(num3);
				panelFaders.Controls.Remove(arrLabels[num3]);
				arrLabels.RemoveAt(num3);
				panelFaders.Controls.Remove(arrNames[num3]);
				arrNames.RemoveAt(num3);
				panelFaders.Controls.Remove(arrInfo[num3]);
				arrInfo.RemoveAt(num3);
				panelFaders.Controls.Remove(arrFaders[num3]);
				arrFaders.RemoveAt(num3);
				panelFaders.Controls.Remove(arrCues[num3]);
				arrCues.RemoveAt(num3);
			}
			if (isDebug)
			{
				logDebug("Add Additional Faders");
			}
			for (int num4 = arrFaders.Count; num4 < faderCount + masterSection.Count; num4++)
			{
				if (isDebug)
				{
					logDebug("-- Add Fader: FaderNum=" + num4 + ", arrFaders.Count=" + arrFaders.Count);
				}
				AirButton airButton = new AirButton();
				airButton.ParentColor = panelFaders.BackColor;
				airButton.Name = "muteFader" + num4;
				airButton.TabIndex = 100 + num4;
				airButton.TextAlign = ContentAlignment.MiddleCenter;
				airButton.Text = (num4 + 1).ToString();
				airButton.Click += muteFader_Click;
				airButton.Tag = num4;
				panelFaders.Controls.Add(airButton);
				arrMutes.Add(airButton);
				AirKnob airKnob = new AirKnob();
				airKnob.KnobLabel = "";
				airKnob.KnobStyle = 1;
				airKnob.KnobType = Values.knobTypes.None;
				airKnob.Maximum = 63;
				airKnob.Minimum = -63;
				airKnob.FineMode = false;
				airKnob.Steps = 1;
				airKnob.Value = 0;
				airKnob.ValueType = "pan";
				airKnob.Name = "panFader" + num4;
				airKnob.TabIndex = 100 + num4;
				airKnob.KnobChanged += panKnob_KnobChanged;
				airKnob.Tag = num4;
				panelFaders.Controls.Add(airKnob);
				arrPans.Add(airKnob);
				airKnob = new AirKnob();
				airKnob.KnobLabel = "";
				airKnob.KnobStyle = 0;
				airKnob.KnobType = Values.knobTypes.None;
				airKnob.Maximum = 72;
				airKnob.Minimum = 0;
				airKnob.FineMode = false;
				airKnob.Steps = 1;
				airKnob.Value = 0;
				airKnob.ValueType = "ha";
				airKnob.Name = "haKnob" + num4;
				airKnob.TabIndex = 100 + num4;
				airKnob.KnobChanged += haKnob_KnobChanged;
				airKnob.Tag = num4;
				panelFaders.Controls.Add(airKnob);
				arrHAs.Add(airKnob);
				airButton = new AirButton();
				airButton.ParentColor = panelFaders.BackColor;
				airButton.Name = "cueFader" + num4;
				airButton.TabIndex = 100 + num4;
				airButton.Font = new Font("Arial", 12.25f, FontStyle.Bold, GraphicsUnit.Point, 0);
				airButton.TextAlign = ContentAlignment.MiddleCenter;
				airButton.Text = "Cue";
				airButton.Click += btnCue_Click;
				airButton.Tag = num4;
				panelFaders.Controls.Add(airButton);
				arrCues.Add(airButton);
				airButton = new AirButton();
				airButton.ParentColor = panelFaders.BackColor;
				airButton.Name = "phantomFader" + num4;
				airButton.TabIndex = 100 + num4;
				airButton.Font = new Font("Arial", 12.25f, FontStyle.Bold, GraphicsUnit.Point, 0);
				airButton.TextAlign = ContentAlignment.MiddleCenter;
				airButton.Text = "+48v";
				airButton.Click += phantomButton_Click;
				airButton.Tag = num4;
				panelFaders.Controls.Add(airButton);
				arrPhantom.Add(airButton);
				AirControls.AirFader airFader = new AirControls.AirFader();
				airFader.Name = "airFader" + num4;
				airFader.TabIndex = 200 + num4;
				airFader.Channel = num4;
				airFader.Minimum = 0;
				airFader.Maximum = 1023;
				airFader.ShowKnob = false;
				airFader.FaderColor = "White";
				airFader.ValueType = "fader";
				airFader.MeterType = "full";
				airFader.ShowMeter = showMeter;
				airFader.FineMode = fineMode;
				airFader.FaderChanged += airFader_FaderChanged;
				panelFaders.Controls.Add(airFader);
				arrFaders.Add(airFader);
				AirButton airButton2 = new AirButton();
				airButton2.Name = "lblFader" + num4;
				airButton2.TabIndex = 0;
				airButton2.BackColor = Settings.Default.c_button_inactive;
				airButton2.ForeColor = Settings.Default.c_button_inactive_fore;
				airButton2.ParentColor = panelFaders.BackColor;
				airButton2.Text = (num4 + 1).ToString();
				airButton2.Tag = num4;
				airButton2.Click += btnNumber_Click;
				panelFaders.Controls.Add(airButton2);
				arrLabels.Add(airButton2);
				AirLabel airLabel = new AirLabel();
				airLabel.Name = "lblInfo" + num4;
				airLabel.TabIndex = 0;
				airLabel.BackColor = Color.Black;
				airLabel.ForeColor = Color.White;
				airLabel.TextAlign = ContentAlignment.MiddleCenter;
				airLabel.TextRenderingHint = TextRenderingHint.AntiAlias;
				panelFaders.Controls.Add(airLabel);
				arrInfo.Add(airLabel);
				TextBox textBox = new TextBox();
				textBox.Name = "nameFader" + num4;
				textBox.BackColor = Color.Black;
				textBox.BorderStyle = BorderStyle.FixedSingle;
				textBox.Font = new Font("Arial", 9.75f, FontStyle.Bold, GraphicsUnit.Point, 0);
				textBox.ForeColor = Color.Lime;
				textBox.TextAlign = HorizontalAlignment.Center;
				textBox.TabIndex = 300 + num4;
				textBox.Tag = num4;
				if (Console.type == ConsoleTypes.M7CL)
				{
					textBox.MaxLength = 8;
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					textBox.MaxLength = 4;
				}
				else
				{
					textBox.MaxLength = 6;
				}
				textBox.TextChanged += nameFader_TextChanged;
				panelFaders.Controls.Add(textBox);
				arrNames.Add(textBox);
			}
			if (isDebug)
			{
				logDebug("ArrFaders.Count=" + arrFaders.Count);
			}
			panelLayer.Controls.Clear();
			if (isDebug)
			{
				logDebug("MaxChannels=" + Settings.Default.maxChannels);
			}
			int num5 = (int)Math.Ceiling((decimal)((Settings.Default.maxChannels < Console.maxInputs - Console.maxSTIN) ? Settings.Default.maxChannels : (Console.maxInputs - Console.maxSTIN)) / (decimal)faderCount);
			num5 += (int)Math.Ceiling((decimal)Console.maxSTIN / (decimal)faderCount);
			int num6 = (int)Math.Ceiling((decimal)Console.maxMIX / (decimal)faderCount);
			num6 += (int)Math.Ceiling((decimal)Console.maxMATRIX / (decimal)faderCount);
			num6++;
			int num7 = (int)Math.Ceiling((decimal)Console.maxOutOmni / (decimal)faderCount);
			num7 += (int)Math.Ceiling((decimal)Console.maxOutSlot / (decimal)faderCount);
			num7++;
			if (num6 > num5)
			{
				num5 = num6;
			}
			if (num7 > num5)
			{
				num5 = num7;
			}
			if (isDebug)
			{
				logDebug("Set Up Layer Buttons. numButtons=" + num5);
			}
			for (int num8 = 0; num8 < num5; num8++)
			{
				AirButton airButton3 = new AirButton();
				airButton3.Name = "btnLayer" + num8;
				airButton3.BackColor = Settings.Default.c_button_inactive;
				airButton3.ForeColor = Settings.Default.c_button_inactive_fore;
				airButton3.ParentColor = panelLayer.BackColor;
				airButton3.TabIndex = 350 + num8;
				airButton3.Click += btnLayer_Click;
				airButton3.Tag = 0;
				airButton3.Width = 20;
				panelLayer.Controls.Add(airButton3);
			}
			panelHome.Show();
			if (isDebug)
			{
				logDebug("Call frmSurface16_Resize(Null,Empty) From numFaders_Change()");
			}
			frmSurface16_Resize(null, EventArgs.Empty);
			bAllowPaint = true;
			if (isDebug)
			{
				logDebug("call switchFaderMode(FOH) From numFaders_Change()");
			}
			switchFaderMode(FaderModes.FOH);
			switchLayerMode(LayerModes.FOH);
			if (isDebug)
			{
				logDebug("Call UpdateUI() From numFaders_Change()");
			}
			updateUI();
		}
		catch (Exception ex)
		{
			logError("NumFadersChange()", "UI", ex.ToString());
		}
	}

	private void btnUDF_MouseUp(object sender, MouseEventArgs e)
	{
		MixEngine.setParam("UDF", 0, 0);
	}

	private void btnUDF_MouseDown(object sender, MouseEventArgs e)
	{
		MixEngine.setParam("UDF", 0, 1);
	}

	private void frmSurface16_ResizeEnd(object sender, EventArgs e)
	{
		isResizing = false;
		if (isDebug)
		{
			logDebug("Call frmSurface16_Resize(Null,Empty) From Resize_End()");
		}
		frmSurface16_Resize(null, EventArgs.Empty);
	}

	private void frmSurface16_ResizeBegin(object sender, EventArgs e)
	{
		isResizing = true;
	}

	private void frmSurface16_Resize(object sender, EventArgs e)
	{
		try
		{
			if (isResizing)
			{
				return;
			}
			if (arrFaders.Count == 0)
			{
				if (isDebug)
				{
					logDebug("frmSurface16_Resize(): Skipping Resize - no faders to draw yet");
				}
			}
			else
			{
				if (base.WindowState == FormWindowState.Minimized)
				{
					return;
				}
				if (base.WindowState == FormWindowState.Normal && thisWin.WindowState != "Fullscreen" && thisWin.Ready)
				{
					thisWin.WindowSize = base.Size;
				}
				if (isDebug)
				{
					logDebug("Starting frmSurface16_Resize(). Size: " + base.ClientRectangle.ToString());
				}
				lastWidth = base.ClientRectangle.Width;
				lastHeight = base.ClientRectangle.Height;
				int num = ((SystemInformation.PrimaryMonitorSize.Width <= 1024) ? 1 : 2);
				_ = SystemInformation.PrimaryMonitorSize.Width;
				if (isDebug)
				{
					logDebug("arrFaders.Count=" + arrFaders.Count);
				}
				int faderCount = Settings.Default.faderCount;
				decimal num2 = -5m;
				if (faderCount < 1)
				{
					Loc.Msg("ERROR: NumFaders is Zero in frmSurface16_resize!");
				}
				Suspend(this);
				if (base.ClientRectangle.Width / 6 > lblBrand.Width)
				{
					lblBrand.Left = 5 * num;
					lblBrand.Top = 5 * num;
					lblBrand.Show();
					panelLCD.Left = lblBrand.Right + 5 * num;
					panelLCD.Top = 5 * num;
				}
				else
				{
					lblBrand.Hide();
					panelLCD.Left = 5 * num;
					panelLCD.Top = 5 * num;
				}
				panelFaders.Left = 5 * num;
				panelFaders.Top = panelLCD.Bottom + 5 * num;
				panelFaders.Width = (int)Math.Round((double)base.ClientRectangle.Width * (((double)faderCount + 1.5) / ((double)faderCount + 4.5)));
				panelFaders.Height = base.ClientRectangle.Height - panelFaders.Top - 5 * num;
				panelConsole.Top = panelLCD.Top;
				panelConsole.Height = (int)Math.Round((double)base.ClientRectangle.Height * 0.24);
				panelConsole.Width = (int)Math.Round((double)(base.ClientRectangle.Width - panelFaders.Width) - 15.0 * (double)num);
				panelConsole.Left = panelFaders.Right + 5 * num;
				panelTopR.Width = panelFaders.Right - panelLCD.Right - 5 * num;
				if (panelTopR.Width > (panelConsole.Width - 20) * 3 + 20)
				{
					panelTopR.Width = (panelConsole.Width - 20) * 3 + 20;
				}
				panelTopR.Left = (panelConsole.Left - panelLCD.Right) / 2 - panelTopR.Width / 2 + panelLCD.Right;
				panelTopR.Top = panelLCD.Top;
				panelTopR.Height = panelLCD.Height;
				panelSends.Top = panelConsole.Bottom + 5 * num;
				panelSends.Width = (int)Math.Round((double)(base.ClientRectangle.Width - panelFaders.Width) - 15.0 * (double)num);
				panelSends.Height = base.ClientRectangle.Height - panelSends.Top - 10;
				panelSends.Left = panelFaders.Right + 5 * num;
				panelHome.Top = panelConsole.Bottom + 5 * num;
				panelHome.Size = panelSends.Size;
				panelHome.Left = panelFaders.Right + 5 * num;
				panelUDF.Top = panelConsole.Bottom + 5 * num;
				panelUDF.Size = panelSends.Size;
				panelUDF.Left = panelFaders.Right + 5 * num;
				decimal num3 = Math.Floor((decimal)(panelFaders.Width - 10 * num) / ((decimal)faderCount + 1.25m + (decimal)masterSection.Count));
				if (isDebug)
				{
					logDebug("faderWidth=" + num3);
				}
				if (Settings.Default.layerPos == "Left")
				{
					panelLayer.Width = 60 + 12 * num;
					panelLayer.Left = (int)num2 + 2 * num;
					panelLayer.Top = 0;
					num2 += (decimal)(panelLayer.Width + 4 * num);
				}
				if (isDebug)
				{
					logDebug("Setting Fader Size and Location");
				}
				decimal sumLayout = 0m;
				arrLayout.ForEach(delegate(string i)
				{
					sumLayout += arrLayoutHeight[i];
				});
				Font font = FontFit("Arial", FontStyle.Bold, "OOOOOO", (int)num3 + 4, intRound((decimal)(panelFaders.Height - (arrLayout.Count - 1) * 5) * (arrLayoutHeight["Label"] / sumLayout)), 24f);
				Font font2 = FontFit("Arial", FontStyle.Bold, "OOOOO", (int)num3 - 8, intRound((decimal)(panelFaders.Height - (arrLayout.Count - 1) * 5) * (arrLayoutHeight["Cue"] / sumLayout)), 24f);
				Font font3 = ((Console.type != ConsoleTypes.M7CL) ? FontFit("Arial", FontStyle.Bold, "OOOOOO", (int)num3 + 4, intRound((decimal)(panelFaders.Height - (arrLayout.Count - 1) * 5) * (arrLayoutHeight["Name"] / sumLayout)), 24f) : FontFit("Arial", FontStyle.Bold, "OOOOOOOO", (int)num3 + 4, intRound((decimal)(panelFaders.Height - (arrLayout.Count - 1) * 5) * (arrLayoutHeight["Name"] / sumLayout)), 24f));
				Font font4 = FontFit("Arial", FontStyle.Bold, "Monitor", (int)num3 - 10, intRound((decimal)(panelFaders.Height - (arrLayout.Count - 1) * 5) * (arrLayoutHeight["Info"] / sumLayout) * 0.45m), 24f);
				Font font5 = FontFit("Arial", FontStyle.Bold, "-999.99", (int)num3 - 8, 20, 24f);
				for (int j = 0; j < arrFaders.Count; j++)
				{
					if (j >= faderCount)
					{
						num2 = panelFaders.Width - intRound((decimal)(masterSection.Count - (j - faderCount)) * (num3 + 5m));
					}
					if (j % 8 == 0)
					{
						num2 += 5m;
					}
					int num4 = 0;
					int num5 = 0;
					int num6 = 0;
					for (int k = 0; k < arrLayout.Count; k++)
					{
						num6 = intRound((decimal)(panelFaders.Height - (arrLayout.Count - 1) * 5) * (arrLayoutHeight[arrLayout[k]] / sumLayout));
						switch (arrLayout[k])
						{
						case "Label":
							arrLabels[j].Font = font;
							arrLabels[j].Width = (int)num3 - 2;
							arrLabels[j].Height = num6;
							arrLabels[j].Left = (int)num2;
							arrLabels[j].Top = num4;
							num4 += arrLabels[j].Height + 5;
							break;
						case "Cue":
							arrCues[j].Font = font2;
							arrCues[j].Width = (int)num3 - 2;
							arrCues[j].Height = num6;
							arrCues[j].Left = (int)num2;
							arrCues[j].Top = num4;
							num4 += arrCues[j].Height + 5;
							if (!arrLayout.Contains("Info"))
							{
								arrInfo[j].Width = arrCues[j].Width;
								arrInfo[j].Height = arrCues[j].Height;
								arrInfo[j].Left = (int)num2;
								arrInfo[j].Top = arrCues[j].Top;
								arrInfo[j].Font = font4;
							}
							break;
						case "Pan":
							arrPans[j].Width = (int)num3 - 2;
							arrPans[j].Height = num6;
							arrPans[j].Left = (int)num2;
							arrPans[j].Top = num4;
							if ((decimal)arrPans[j].Height > num3)
							{
								num5 += arrPans[j].Height - intRound(num3);
								arrPans[j].Height = intRound(num3);
							}
							num4 += arrPans[j].Height + 5;
							if (!arrLayout.Contains("Info"))
							{
								arrInfo[j].Width = arrPans[j].Width;
								arrInfo[j].Height = arrPans[j].Height;
								arrInfo[j].Left = (int)num2;
								arrInfo[j].Top = arrPans[j].Top;
								arrInfo[j].Font = font4;
							}
							break;
						case "Phantom":
							arrPhantom[j].Font = font2;
							arrPhantom[j].Width = (int)num3 - 2;
							arrPhantom[j].Height = num6;
							arrPhantom[j].Left = (int)num2;
							arrPhantom[j].Top = num4;
							num4 += arrPhantom[j].Height + 5;
							if (!arrLayout.Contains("Info"))
							{
								arrInfo[j].Width = arrPhantom[j].Width;
								arrInfo[j].Height = arrPhantom[j].Height;
								arrInfo[j].Left = (int)num2;
								arrInfo[j].Top = arrPhantom[j].Top;
								arrInfo[j].Font = font4;
							}
							break;
						case "HA":
							arrHAs[j].Width = (int)num3 - 2;
							arrHAs[j].Height = num6;
							arrHAs[j].Left = (int)num2;
							arrHAs[j].Top = num4;
							if ((decimal)arrHAs[j].Height > num3)
							{
								num5 += arrHAs[j].Height - intRound(num3);
								arrHAs[j].Height = intRound(num3);
							}
							num4 += arrHAs[j].Height + 5;
							break;
						case "Info":
							arrInfo[j].Font = font4;
							arrInfo[j].Width = (int)num3 - 2;
							arrInfo[j].Height = num6;
							arrInfo[j].Left = (int)num2;
							arrInfo[j].Top = num4;
							num4 += arrInfo[j].Height + 5;
							break;
						case "Name":
							arrNames[j].Font = font3;
							arrNames[j].Width = (int)num3 - 2;
							arrNames[j].Height = num6;
							arrNames[j].Left = (int)num2;
							arrNames[j].Top = num4;
							num4 += arrNames[j].Height + 5;
							num5 += num6 - arrNames[j].Height;
							break;
						case "Mute":
							arrMutes[j].Font = font2;
							arrMutes[j].Width = (int)num3 - 2;
							arrMutes[j].Height = num6;
							arrMutes[j].Left = (int)num2;
							arrMutes[j].Top = num4;
							num4 += arrMutes[j].Height + 5;
							break;
						case "Fader":
							arrFaders[j].Width = (int)num3 - 1;
							arrFaders[j].Height = num6 + num5;
							arrFaders[j].Left = (int)num2;
							arrFaders[j].Top = num4;
							arrFaders[j].Font = font5;
							num4 += arrFaders[j].Height + 5;
							break;
						}
					}
					num2 += num3;
					if (Settings.Default.layerPos == "Center" && j == faderCount / 2 - 1)
					{
						panelLayer.Width = 60 + 6 * num;
						panelLayer.Left = (int)num2 + 2 * num;
						panelLayer.Top = 0;
						num2 += (decimal)(panelLayer.Width + 4 * num);
					}
				}
				if (Settings.Default.layerPos == "Right")
				{
					if (masterSection.Count > 0)
					{
						panelLayer.Width = arrFaders[faderCount].Left - arrFaders[faderCount - 1].Right - 20;
						panelLayer.Left = arrFaders[faderCount - 1].Right + 10;
						panelLayer.Top = 0;
						panelLayer.Height = panelFaders.Height;
					}
					else
					{
						panelLayer.Width = panelFaders.Width - arrFaders[faderCount - 1].Right - 20;
						panelLayer.Left = arrFaders[faderCount - 1].Right + 10;
						panelLayer.Top = 0;
						panelLayer.Height = panelFaders.Height;
					}
				}
				if (Settings.Default.maxChannels == 0)
				{
					Loc.Msg("ERROR: Max Channels is zero. Attempting to fix...");
					Settings.Default.maxChannels = 32;
					Settings.Default.Save();
				}
				if (Console.maxInputs == 0)
				{
					Loc.Msg("ERROR: Console.MaxInputs is zero. Attempting to fix... Notify Support!");
					Console.maxInputs = 32;
				}
				int num7 = ((Settings.Default.maxChannels + Console.maxSTIN < Console.maxInputs) ? (Settings.Default.maxChannels + Console.maxSTIN) : Console.maxInputs);
				if (isDebug)
				{
					logDebug("chanRange=" + num7 + ", numFaders=" + faderCount);
				}
				if (num7 == 0)
				{
					Loc.Msg("ERROR: Channel Range is zero. Attempting to fix... Notify Support!");
					num7 = 72;
				}
				decimal num8 = 75m;
				try
				{
					num8 = (panelLayer.Height - 40 - 10 * (num7 / 32)) / panelLayer.Controls.Count;
				}
				catch (Exception ex)
				{
					logError("Form16Resize_ButtonHeight", "UI", ex.ToString());
				}
				if (num8 > 75m)
				{
					num8 = 75m;
				}
				decimal d = 25m;
				Font font6 = FontFit("Arial", FontStyle.Bold, "SLOT9-99", panelLayer.Width - 10, (int)Math.Round(num8 / 1.75m) - 3, 20f);
				Font subFont = new Font("Arial", font6.Size * 0.75f);
				for (int l = 0; l < panelLayer.Controls.Count; l++)
				{
					if (panelLayer.Controls[l] is AirButton)
					{
						panelLayer.Controls[l].Font = font6;
						((AirButton)panelLayer.Controls[l]).SubFont = subFont;
						panelLayer.Controls[l].Width = panelLayer.Width - 6;
						panelLayer.Controls[l].Height = (int)Math.Round(num8 - 4.0m);
						panelLayer.Controls[l].Left = 3;
						panelLayer.Controls[l].Top = (int)Math.Round(d);
						d += num8;
						if (l == 3)
						{
							d += 10m;
						}
						if (l == 7)
						{
							d += 10m;
						}
					}
				}
				panelSends_Resize(null, EventArgs.Empty);
				panelHome_Resize(null, EventArgs.Empty);
				panelTopR_Resize(null, EventArgs.Empty);
				panelUDF_Resize(null, EventArgs.Empty);
				for (int m = 0; m < arrFaders.Count; m++)
				{
				}
				Resume(this);
				updateUI();
			}
		}
		catch (Exception ex2)
		{
			logError("Form16Resize()", "UI", ex2.ToString());
		}
	}

	private void panelConsole_Resize(object sender, EventArgs e)
	{
		try
		{
			int num = (panelConsole.Width - 14) / 2 - 4;
			int num2 = (panelConsole.Height - 41) / 3;
			Font font = FontFit("Arial", FontStyle.Bold, "CUSTOM", num - 2, (int)Math.Round((double)num2 / 1.75) - 2, 20f);
			Font subFont = new Font("Arial", font.Size * 0.75f);
			btnPageHome.Font = font;
			btnPageHome.SubFont = subFont;
			btnPageHome.Width = num;
			btnPageHome.Height = num2;
			btnPageHome.Left = 10;
			btnPageHome.Top = 19;
			btnPageMaster.Font = font;
			btnPageMaster.SubFont = subFont;
			btnPageMaster.Width = num;
			btnPageMaster.Height = num2;
			btnPageMaster.Left = 10;
			btnPageMaster.Top = num2 + 2 + 19;
			btnPageCustom.Font = font;
			btnPageCustom.SubFont = subFont;
			btnPageCustom.Width = num;
			btnPageCustom.Height = num2;
			btnPageCustom.Left = 10;
			btnPageCustom.Top = 2 * (num2 + 2) + 19;
			btnPageSOF.Font = font;
			btnPageSOF.SubFont = subFont;
			btnPageSOF.Width = num;
			btnPageSOF.Height = num2;
			btnPageSOF.Left = num + 14;
			btnPageSOF.Top = 19;
			btnPageMOF.Font = font;
			btnPageMOF.SubFont = subFont;
			btnPageMOF.Width = num;
			btnPageMOF.Height = num2;
			btnPageMOF.Left = num + 14;
			btnPageMOF.Top = num2 + 2 + 19;
			btnPageDCA.Font = font;
			btnPageDCA.SubFont = subFont;
			btnPageDCA.Width = num;
			btnPageDCA.Height = num2;
			btnPageDCA.Left = num + 14;
			btnPageDCA.Top = 2 * (num2 + 2) + 19;
		}
		catch (Exception ex)
		{
			logError("PanelConsoleResize()", "UI", ex.ToString());
		}
	}

	private void panelHome_Resize(object sender, EventArgs e)
	{
		try
		{
			lblPanelHome.Top = 2;
			lblPanelHome.Height = 20;
			lblPanelHome.Width = panelHome.Width - 50;
			lblPanelHome.Left = 25;
			Font font = FontFit("Arial", FontStyle.Bold, "Sends on Faders", lblPanelHome.Width - 8, 20, 20f);
			lblPanelHome.Font = font;
			int num = 16;
			decimal num2 = (decimal)(panelHome.Width - 14) / 2.0m;
			decimal num3 = (decimal)(panelHome.Height - 58) / ((decimal)num / 2.0m);
			font = FontFit("Arial", FontStyle.Bold, "NO PATCH\nOOOOOOOO", (int)num2 - 5, (int)num3 - 8, 20f);
			Font subFont = new Font("Arial", font.Size * 0.75f);
			int num4 = 0;
			foreach (Control control in panelHome.Controls)
			{
				if (control is AirButton)
				{
					control.Font = font;
					((AirButton)control).SubFont = subFont;
					control.Width = (int)Math.Round(num2) - 4;
					control.Height = (int)Math.Round(num3) - 4;
					control.Left = (int)Math.Round((decimal)(num4 % 2) * num2) + 10;
					control.Top = (int)Math.Round((decimal)(num4 / 2) * num3) + 24 + num4 / 8 * 20;
					num4++;
				}
			}
		}
		catch (Exception ex)
		{
			logError("panelHome_Resize()", "UI", ex.ToString());
		}
	}

	private void syncUDFState()
	{
		if (lblDebug.InvokeRequired)
		{
			syncUDFStateD method = syncUDFState;
			BeginInvoke(method, new object[0]);
			return;
		}
		try
		{
			foreach (AirButton item in arrBtnUDF)
			{
				int num = 0;
				string text = "";
				string[] array = item.Tag.ToString().Split(splitChar, 2);
				text = array[0];
				if (array.Length > 1)
				{
					num = int.Parse(array[1]);
				}
				if (text == "")
				{
					item.Hide();
				}
				else
				{
					item.Show();
				}
				if (text.Contains("~"))
				{
					item.BackColor = Settings.Default.c_button_inactive;
					item.ForeColor = Color.Gray;
				}
				else if (text != "tap")
				{
					item.BackColor = Settings.Default.c_button_inactive;
					item.ForeColor = Settings.Default.c_button_inactive_fore;
				}
				switch (text)
				{
				case "mix":
					if (SOFPage == num && faderMode == FaderModes.SOF)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					else if (MixEngine.chData[num + MixEngine.chOffsetMix].getParam(Channel.Pr.Pair) == 1 && num % 2 == 1)
					{
						item.Hide();
					}
					else if (MixEngine.chData[num + MixEngine.chOffsetMix].channelType != ChannelTypes.Mix)
					{
						item.Hide();
					}
					break;
				case "matrix":
					if (MOFPage == num && faderMode == FaderModes.MOF)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				case "mutegroup":
					if (MixEngine.getParam(num, MixEngine.Pr.MuteMaster, 0) == 1)
					{
						item.BackColor = Color.Red;
						item.ForeColor = Color.White;
					}
					break;
				case "meters":
					if (showMeter)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				case "pan":
					if (faderMode == FaderModes.Pan)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				case "ha":
					if (faderMode == FaderModes.HA)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				case "outportatt":
					if (faderMode == FaderModes.OutportAtt)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				case "outportdelay":
					if (faderMode == FaderModes.OutportDelay)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				case "lamp":
					if (Console.LampToggle)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
					}
					break;
				}
				item.LEDColor = item.BackColor;
			}
		}
		catch (Exception ex)
		{
			logError("syncUDFState()", "UI", ex.ToString());
		}
	}

	private void btnPageHome_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				lblPanelHome.Text = "";
				for (int i = 0; i < 16; i++)
				{
					string[] array = buttonsHome[i].Split(splitChar, 2);
					arrBtnUDF[i].Text = array[0];
					arrBtnUDF[i].Tag = array[1];
					arrBtnUDF[i].LEDColor = arrBtnUDF[i].BackColor;
				}
				UDFPage = "Home";
				syncUDFState();
				switchFaderMode(FaderModes.FOH);
				if (layerMode != 0)
				{
					switchLayerMode(LayerModes.FOH);
				}
			}
		}
		catch (Exception ex)
		{
			logError("HomeClick()", "UI", ex.ToString());
		}
	}

	private void btnPageMaster_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				if (layerMode != LayerModes.Master)
				{
					switchLayerMode(LayerModes.Master);
				}
				else
				{
					switchFaderMode(FaderModes.FOH);
				}
			}
		}
		catch (Exception ex)
		{
			logError("MasterLayerClick()", "UI", ex.ToString());
		}
	}

	private void btnPageCustom_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				if (layerMode != LayerModes.Custom)
				{
					switchLayerMode(LayerModes.Custom);
				}
				else
				{
					switchFaderMode(FaderModes.FOH);
				}
			}
		}
		catch (Exception ex)
		{
			logError("CustomLayerClick()", "UI", ex.ToString());
		}
	}

	private void panelSends_Resize(object sender, EventArgs e)
	{
		try
		{
			int num = 16;
			decimal num2 = (decimal)(panelSends.Width - 14) / 2.0m;
			decimal num3 = (decimal)(panelSends.Height - 38) / ((decimal)num / 2.0m);
			Font font = FontFit("Arial", FontStyle.Bold, "(NO PATCH)\nOOOOOOOO", (int)num2 - 5, (int)num3 - 8, 20f);
			Font subFont = new Font("Arial", font.Size * 0.75f);
			int num4 = 0;
			foreach (Control control in panelSends.Controls)
			{
				if (control is AirButton)
				{
					control.Font = font;
					((AirButton)control).SubFont = subFont;
					control.Width = (int)Math.Round(num2) - 4;
					control.Height = (int)Math.Round(num3) - 4;
					control.Left = (int)Math.Round((decimal)(num4 % 2) * num2) + 10;
					control.Top = (int)Math.Round((decimal)(num4 / 2) * num3) + 22;
					num4++;
				}
			}
		}
		catch (Exception ex)
		{
			logError("panelSends_Resize()", "UI", ex.ToString());
		}
	}

	private void btnPageSOF_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				if (faderMode == FaderModes.SOF)
				{
					switchFaderMode(FaderModes.FOH);
				}
				else
				{
					switchFaderMode(FaderModes.SOF);
				}
			}
		}
		catch (Exception ex)
		{
			logError("PageSOFClick()", "UI", ex.ToString());
		}
	}

	private void btnPageMOF_Click(object sender, EventArgs e)
	{
		try
		{
			if (!surfaceLocked)
			{
				if (faderMode == FaderModes.MOF)
				{
					switchFaderMode(FaderModes.FOH);
				}
				else
				{
					switchFaderMode(FaderModes.MOF);
				}
			}
		}
		catch (Exception ex)
		{
			logError("PageMOFClick()", "UI", ex.ToString());
		}
	}

	private void panelUDF_Resize(object sender, EventArgs e)
	{
		try
		{
			int num = 16;
			decimal num2 = (decimal)(panelUDF.Width - 14) / 2.0m;
			decimal num3 = (decimal)(panelUDF.Height - 38) / ((decimal)num / 2.0m);
			Font font = FontFit("Arial", FontStyle.Bold, "(NO PATCH)\nOOOOOOOO", (int)num2 - 5, (int)num3 - 8, 20f);
			Font subFont = new Font("Arial", font.Size * 0.75f);
			int num4 = 0;
			foreach (Control control in panelUDF.Controls)
			{
				if (control is AirButton)
				{
					control.Font = font;
					((AirButton)control).SubFont = subFont;
					control.Width = (int)Math.Round(num2) - 4;
					control.Height = (int)Math.Round(num3) - 4;
					control.Left = (int)Math.Round((decimal)(num4 % 2) * num2) + 10;
					control.Top = (int)Math.Round((decimal)(num4 / 2) * num3) + 22;
					num4++;
				}
			}
		}
		catch (Exception ex)
		{
			logError("PanelUDFResize()", "UI", ex.ToString());
		}
	}

	public void tapMakeBlink(int efx, bool isBlink, bool isCounting)
	{
		if (this == null)
		{
			return;
		}
		if (base.InvokeRequired)
		{
			BlinkCallback method = tapMakeBlink;
			BeginInvoke(method, efx, isBlink, isCounting);
			return;
		}
		try
		{
			foreach (AirButton item in arrBtnUDF)
			{
				int num = 0;
				string text = "";
				string[] array = item.Tag.ToString().Split(splitChar, 2);
				text = array[0];
				if (array.Length > 1)
				{
					num = int.Parse(array[1]);
				}
				if (text == "tap" && efx == num)
				{
					if (isCounting)
					{
						item.BackColor = Settings.Default.c_button_active;
						item.ForeColor = Settings.Default.c_button_active_fore;
						item.Text = "Tap " + (num + 5).ToString() + " (" + tapCount[num] + ")\n" + MixEngine.EFXData[num].bpm.ToString() + " bpm";
					}
					else
					{
						item.BackColor = Settings.Default.c_button_inactive;
						item.ForeColor = Settings.Default.c_button_inactive_fore;
						item.Text = "Tap " + (num + 5) + "\n" + MixEngine.EFXData[num].bpm + " bpm";
					}
					if (isBlink)
					{
						item.LEDColor = Color.Red;
					}
					else
					{
						item.LEDColor = Color.Silver;
					}
				}
			}
			tapBlink[efx] = isBlink;
		}
		catch (Exception ex)
		{
			logDebug("tapMakeBlink: " + ex.ToString());
		}
	}

	private void btnUDF_Click(object sender, EventArgs e)
	{
		if (surfaceLocked)
		{
			return;
		}
		try
		{
			string action = ((AirButton)sender).Tag.ToString();
			execUDF(action, (AirButton)sender);
		}
		catch (Exception ex)
		{
			logError("btnUDF_Click()", "UI", ex.ToString());
		}
	}

	private void execUDF(string action)
	{
		execUDF(action, null);
	}

	private void execUDF(string action, AirButton sender)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		char[] separator = new char[1] { ':' };
		try
		{
			if (action.IndexOf(":") > 0)
			{
				string[] array = action.Split(separator, 2);
				action = array[0];
				num = int.Parse(array[1]);
			}
			switch (action)
			{
			case "efx":
			{
				frmEFX frmEFX2 = new frmEFX();
				frmEFX2.Show();
				frmEFX2.ActiveEFX = num;
				break;
			}
			case "geq":
			{
				frmGraphicEQ frmGraphicEQ2 = new frmGraphicEQ();
				frmGraphicEQ2.ActiveRack = num;
				frmGraphicEQ2.Show();
				break;
			}
			case "tap":
			{
				long num4 = animationTimer.ElapsedTicks - tapStart[num];
				float num5 = (float)num4 / (float)Stopwatch.Frequency;
				long num6 = (long)Math.Round(2.5);
				float num7 = (float)(animationTimer.ElapsedTicks - tapLast[num]) / (float)Stopwatch.Frequency;
				float num8 = num7;
				if (tapCount[num] > 1)
				{
					num8 = (float)(tapLast[num] - tapStart[num]) / (float)(tapCount[num] - 1) / (float)Stopwatch.Frequency;
				}
				tapLast[num] = animationTimer.ElapsedTicks;
				if (tapCount[num] < 1)
				{
					tapStart[num] = animationTimer.ElapsedTicks;
				}
				else if ((tapCount[num] == 2 && num7 > (float)num6) || (tapCount[num] >= 2 && (double)num7 > (double)num8 * 1.75))
				{
					tapCount[num] = 0;
					tapStart[num] = animationTimer.ElapsedTicks;
				}
				else
				{
					num3 = (int)Math.Round(60f / num5 * (float)tapCount[num]);
					if (num3 >= 25)
					{
						MixEngine.EFXData[num].setParam("UI", EFX.Pr.BPM, num3);
					}
				}
				tapCount[num]++;
				tapMakeBlink(num, tapBlink[num], isCounting: true);
				syncUDFState();
				break;
			}
			case "tools":
				lblPanelHome.Text = "Tools";
				for (num2 = 0; num2 < 16; num2++)
				{
					if (buttonsTools[num2] == "")
					{
						arrBtnUDF[num2].Text = "";
						arrBtnUDF[num2].Tag = "";
					}
					else
					{
						string[] array2 = buttonsTools[num2].Split(separator, 2);
						arrBtnUDF[num2].Text = array2[0];
						arrBtnUDF[num2].Tag = array2[1];
					}
				}
				syncUDFState();
				UDFPage = action;
				break;
			case "rack":
				lblPanelHome.Text = "Rack";
				num2 = 0;
				if (Console.type != ConsoleTypes.O1V96V2)
				{
					for (int k = 0; k < 8; k++)
					{
						if (MixEngine.RackData[k].Type == 1)
						{
							arrBtnUDF[num2].Text = "GEQ " + (k + 1) + "\n" + MixEngine.getParamStr(k, MixEngine.Pr.RackSource, 0);
							arrBtnUDF[num2].Tag = "geq:" + k;
							arrBtnUDF[num2].Show();
							num2++;
						}
						else if (MixEngine.RackData[k].Type == 2)
						{
							arrBtnUDF[num2].Text = "GEQ " + (k + 1) + "A\n" + MixEngine.getParamStr(k, MixEngine.Pr.RackSource, 0);
							arrBtnUDF[num2].Tag = "geq:" + k;
							arrBtnUDF[num2].Show();
							num2++;
							arrBtnUDF[num2].Text = "GEQ " + (k + 1) + "B\n" + MixEngine.getParamStr(k + 8, MixEngine.Pr.RackSource, 0);
							arrBtnUDF[num2].Tag = "geq:" + (k + 8);
							arrBtnUDF[num2].Show();
							num2++;
						}
					}
				}
				for (; num2 < arrBtnUDF.Count; num2++)
				{
					arrBtnUDF[num2].Text = "";
					arrBtnUDF[num2].Tag = "";
					arrBtnUDF[num2].Hide();
				}
				if (Console.type != ConsoleTypes.O1V96V2)
				{
					for (num2 = 0; num2 < 4; num2++)
					{
						arrBtnUDF[arrBtnUDF.Count - (4 - num2)].Text = "Tap " + (num2 + 5);
						arrBtnUDF[arrBtnUDF.Count - (4 - num2)].Tag = "tap:" + num2;
						arrBtnUDF[arrBtnUDF.Count - (4 - num2)].Show();
					}
				}
				syncUDFState();
				UDFPage = action;
				break;
			case "meters":
			{
				showMeter = !showMeter;
				for (int i = 0; i < arrFaders.Count; i++)
				{
					arrFaders[i].ShowMeter = showMeter;
				}
				if (showMeter)
				{
					refreshMeters();
				}
				syncUDFState();
				break;
			}
			case "mix":
				if (faderMode == FaderModes.SOF && SOFPage == num)
				{
					switchFaderMode(FaderModes.FOH);
				}
				else
				{
					SOFPage = num;
					switchFaderMode(FaderModes.SOF);
				}
				syncUDFState();
				break;
			case "matrix":
				if (faderMode == FaderModes.MOF && MOFPage == num)
				{
					switchFaderMode(FaderModes.FOH);
				}
				else
				{
					MOFPage = num;
					switchFaderMode(FaderModes.MOF);
				}
				syncUDFState();
				break;
			case "sync":
				MixEngine.connPhase = ConnectionPhase.PreSync;
				break;
			case "mutegroup":
				MixEngine.setParam("UI", num, MixEngine.Pr.MuteMaster, 0, (MixEngine.getParam(num, MixEngine.Pr.MuteMaster, 0) != 1) ? 1 : 0);
				syncUDFState();
				break;
			case "lamp":
				if (Console.LampToggle)
				{
					MixEngine.setParam("Lamp", 0, Console.Lamp);
					Console.LampToggle = false;
				}
				else
				{
					if (Console.Lamp < 50)
					{
						MixEngine.setParam("Lamp", 0, 100);
					}
					else
					{
						MixEngine.setParam("Lamp", 0, 0);
					}
					Console.LampToggle = true;
				}
				syncUDFState();
				break;
			case "bounce":
			{
				Form form = new frmBounce();
				form.Show();
				break;
			}
			case "ha":
				switchFaderMode(FaderModes.HA);
				break;
			case "pan":
				switchFaderMode(FaderModes.Pan);
				break;
			case "outportatt":
				switchFaderMode(FaderModes.OutportAtt);
				break;
			case "outportdelay":
				switchFaderMode(FaderModes.OutportDelay);
				break;
			case "nextscene":
			{
				if (frmConfirm.askQuestion("This will overwrite current mix.\nLoad New Scene?", "Confirm Scene", this) != DialogResult.Yes)
				{
					break;
				}
				for (int j = Console.sceneNum + 1; j <= Console.sceneHigh; j++)
				{
					if (MixEngine.sceneData[j].isValid)
					{
						MixEngine.setParam("RecallScene", j, 0);
						j = 999;
					}
				}
				break;
			}
			case "prevscene":
			{
				if (frmConfirm.askQuestion("This will overwrite current mix.\nLoad New Scene?", "Confirm Scene", this) != DialogResult.Yes)
				{
					break;
				}
				for (int num9 = Console.sceneNum - 1; num9 >= 0; num9--)
				{
					if (MixEngine.sceneData[num9].isValid)
					{
						MixEngine.setParam("RecallScene", num9, 0);
						num9 = -1;
					}
				}
				break;
			}
			case "loadscene":
				if (frmSceneH == null || frmSceneH.IsDisposed)
				{
					frmSceneH = new frmScene();
					frmSceneH.setMode("LoadScene");
					frmSceneH.Show();
				}
				else
				{
					frmSceneH.setMode("LoadScene");
					frmSceneH.Show();
					frmSceneH.Activate();
				}
				break;
			case "savescene":
				if (frmSceneH == null || frmSceneH.IsDisposed)
				{
					frmSceneH = new frmScene();
					frmSceneH.setMode("SaveScene");
					frmSceneH.Show();
				}
				else
				{
					frmSceneH.setMode("SaveScene");
					frmSceneH.Show();
					frmSceneH.Activate();
				}
				break;
			}
		}
		catch (Exception ex)
		{
			logError("UDFClick()", "UI", ex.ToString());
		}
	}

	private void panelUDF_VisibleChanged(object sender, EventArgs e)
	{
		try
		{
			if (panelUDF.Visible)
			{
				btnPageUDF.BackColor = Settings.Default.c_button_active;
				btnPageUDF.ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				btnPageUDF.BackColor = Settings.Default.c_button_inactive;
				btnPageUDF.ForeColor = Settings.Default.c_button_inactive_fore;
			}
		}
		catch (Exception ex)
		{
			logError("PanelUDF_VisibleChanged()", "UI", ex.ToString());
		}
	}

	private Font FontFit(string fontName, FontStyle fontStyle, string thisText, int thisWide, int thisHigh, float maxSize)
	{
		try
		{
			Graphics graphics = CreateGraphics();
			while (maxSize > 4f)
			{
				Font font = new Font(fontName, maxSize, fontStyle);
				SizeF sizeF = graphics.MeasureString(thisText, font);
				if (sizeF.Width <= (float)thisWide && sizeF.Height <= (float)thisHigh)
				{
					return font;
				}
				maxSize -= 0.5f;
			}
			return new Font(fontName, 4f, fontStyle);
		}
		catch (Exception ex)
		{
			logError("FontFit()", "UI", ex.ToString());
			return new Font("Arial", 12f);
		}
	}

	private void panelTopR_Resize(object sender, EventArgs e)
	{
		try
		{
			decimal num = (panelTopR.Height - 36) / 1;
			decimal num2 = ((decimal)panelTopR.Width - 40.00m) / (decimal)panelTopR.Controls.Count;
			Font font = FontFit("Arial", FontStyle.Bold, "CONNECT\nME", (int)num2 - 4, (int)num - 4, 20f);
			Font subFont = new Font("Arial", font.Size * 0.75f);
			int num3 = 0;
			foreach (Control control in panelTopR.Controls)
			{
				if (control is AirButton)
				{
					control.Font = font;
					((AirButton)control).SubFont = subFont;
					control.Top = 18;
					control.Height = (int)num;
					control.Width = (int)Math.Round(num2 - 2m);
					control.Left = (int)Math.Round(num2 * (decimal)num3 + 20m);
					num3++;
				}
			}
		}
		catch (Exception ex)
		{
			logError("PanelTopRResize()", "UI", ex.ToString());
		}
	}

	private void frmSurface16_Load(object sender, EventArgs e)
	{
		try
		{
			Loc.Apply(this);
			threadTimer = new System.Threading.Timer(tickUI, "tick", 50, 50);
			if (Settings.Default.autoConnect)
			{
				btnConnect_Click(btnConnect, null);
			}
		}
		catch (Exception ex)
		{
			logError("Form16Load()", "UI", ex.ToString());
		}
	}

	private void lblBrand_DoubleClick(object sender, EventArgs e)
	{
	}

	private void lbLog_DoubleClick(object sender, EventArgs e)
	{
		lbLog.Items.Clear();
	}

	private void lbLog_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			string text = lbLog.SelectedItem.ToString();
			Clipboard.SetText(text);
		}
		catch (Exception ex)
		{
			Loc.Msg(ex.ToString());
		}
	}

	private void frmSurface16_FormClosing(object sender, FormClosingEventArgs e)
	{
		try
		{
			if (!isGlobalShutdown && Settings.Default.confirmExit)
			{
				if (surfaceLocked)
				{
					e.Cancel = true;
					return;
				}
				if (frmConfirm.askQuestion("Exit 01vmix?", "Confirm Exit", this) == DialogResult.No)
				{
					e.Cancel = true;
					return;
				}
			}
			isGlobalShutdown = true;
			thisWin.WindowState = ((base.FormBorderStyle == FormBorderStyle.None) ? "Fullscreen" : base.WindowState.ToString());
			thisWin.Page = 0;
			Settings.Default.windowMain = WindowStatic.Store(thisWin);
			Settings.Default.Save();
			if (MixEngine.connStatus != 0)
			{
				MixEngine.reqParam("StopMeter", 0, 0);
				Thread.Sleep(100);
				MixEngine.logDebug("Stopping SingleThread...");
				MixEngine.disconnectConsole();
				btnConnect.Text = "Connect";
				btnConnect.BackText = "";
			}
			MixEngine.termFlag.Set();
			Thread.Sleep(100);
		}
		catch (Exception ex)
		{
			logError("ExitClick()", "UI", ex.ToString());
		}
		MixEngine.logClose();
	}

	private void panelLCD_Click(object sender, EventArgs e)
	{
		try
		{
			frmKeyboard frmKeyboard2 = new frmKeyboard();
			frmKeyboard2.setProperty("SceneName", Console.sceneNum);
			frmKeyboard2.Show();
		}
		catch (Exception ex)
		{
			logError("LCD_Click()", "UI", ex.ToString());
		}
	}

	private void lblBrand_Click(object sender, EventArgs e)
	{
	}

	private void panelHome_VisibleChanged(object sender, EventArgs e)
	{
		if (!panelHome.Visible)
		{
			btnPageHome.BackColor = Settings.Default.c_button_inactive;
			btnPageHome.ForeColor = Settings.Default.c_button_inactive_fore;
		}
	}

	private void frmSurface16_LocationChanged(object sender, EventArgs e)
	{
		if (base.WindowState == FormWindowState.Normal && thisWin.WindowState != "Fullscreen" && thisWin.Ready)
		{
			thisWin.WindowPos = base.Location;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmSurface16));
		this.txtRAW = new System.Windows.Forms.TextBox();
		this.timerClock = new System.Windows.Forms.Timer(this.components);
		this.panelFaders = new System.Windows.Forms.Panel();
		this.lblLocked = new System.Windows.Forms.Label();
		this.lbLog = new System.Windows.Forms.ListBox();
		this.panelTopR = new AirControls.AirPanel();
		this.btnConnect = new AirControls.AirButton();
		this.btnConfig = new AirControls.AirButton();
		this.btnFullScreen = new AirControls.AirButton();
		this.btnLock = new AirControls.AirButton();
		this.btnFine = new AirControls.AirButton();
		this.panelUDF = new AirControls.AirPanel();
		this.label3 = new System.Windows.Forms.Label();
		this.panelSends = new AirControls.AirPanel();
		this.lblParamPage = new System.Windows.Forms.Label();
		this.panelLayer = new AirControls.AirPanel();
		this.panelLCD = new AirControls.AirPanel();
		this.label5 = new System.Windows.Forms.Label();
		this.label4 = new System.Windows.Forms.Label();
		this.lblSceneName = new System.Windows.Forms.Label();
		this.meterBattery = new AirControls.AirMeter();
		this.lblClock = new System.Windows.Forms.Label();
		this.lblConnStatus = new System.Windows.Forms.Label();
		this.lblDebug = new System.Windows.Forms.Label();
		this.lblBrand = new System.Windows.Forms.Label();
		this.panelHome = new AirControls.AirPanel();
		this.lblPanelHome = new System.Windows.Forms.Label();
		this.panelConsole = new AirControls.AirPanel();
		this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
		this.panelTopR.SuspendLayout();
		this.panelUDF.SuspendLayout();
		this.panelSends.SuspendLayout();
		this.panelLCD.SuspendLayout();
		this.panelHome.SuspendLayout();
		base.SuspendLayout();
		this.txtRAW.Location = new System.Drawing.Point(1, 106);
		this.txtRAW.Name = "txtRAW";
		this.txtRAW.Size = new System.Drawing.Size(277, 20);
		this.txtRAW.TabIndex = 13;
		this.txtRAW.Text = "F043303E1201010600000001F7";
		this.txtRAW.Visible = false;
		this.timerClock.Enabled = true;
		this.timerClock.Interval = 1000;
		this.timerClock.Tick += new System.EventHandler(timerClock_Tick);
		this.panelFaders.Location = new System.Drawing.Point(12, 123);
		this.panelFaders.Name = "panelFaders";
		this.panelFaders.Size = new System.Drawing.Size(535, 107);
		this.panelFaders.TabIndex = 195;
		this.lblLocked.AutoSize = true;
		this.lblLocked.BackColor = System.Drawing.Color.Transparent;
		this.lblLocked.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblLocked.Font = new System.Drawing.Font("Arial", 36f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblLocked.ForeColor = System.Drawing.Color.Yellow;
		this.lblLocked.Location = new System.Drawing.Point(34, 237);
		this.lblLocked.Name = "lblLocked";
		this.lblLocked.Size = new System.Drawing.Size(368, 58);
		this.lblLocked.TabIndex = 201;
		this.lblLocked.Text = "Screen Locked";
		this.lblLocked.Visible = false;
		this.lbLog.FormattingEnabled = true;
		this.lbLog.Location = new System.Drawing.Point(1, 132);
		this.lbLog.Name = "lbLog";
		this.lbLog.Size = new System.Drawing.Size(509, 290);
		this.lbLog.TabIndex = 221;
		this.lbLog.Visible = false;
		this.lbLog.SelectedIndexChanged += new System.EventHandler(lbLog_SelectedIndexChanged);
		this.lbLog.DoubleClick += new System.EventHandler(lbLog_DoubleClick);
		this.panelTopR.BackColor = System.Drawing.Color.FromArgb(0, 0, 72);
		this.panelTopR.BorderColor = System.Drawing.Color.Black;
		this.panelTopR.BorderSize = 1f;
		this.panelTopR.Controls.Add(this.btnConnect);
		this.panelTopR.Controls.Add(this.btnConfig);
		this.panelTopR.Controls.Add(this.btnFullScreen);
		this.panelTopR.Controls.Add(this.btnLock);
		this.panelTopR.Controls.Add(this.btnFine);
		this.panelTopR.EQColor = System.Drawing.Color.Lime;
		this.panelTopR.Location = new System.Drawing.Point(343, 8);
		this.panelTopR.Name = "panelTopR";
		this.panelTopR.Padding = new System.Windows.Forms.Padding(4);
		this.panelTopR.PanelType = 0;
		this.panelTopR.Size = new System.Drawing.Size(638, 93);
		this.panelTopR.TabIndex = 199;
		this.panelTopR.Resize += new System.EventHandler(panelTopR_Resize);
		this.btnConnect.BackColor = AirFader.Properties.Settings.Default.c_button_inactive;
		this.btnConnect.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnConnect.BackText = "";
		this.btnConnect.BorderColor = System.Drawing.Color.Black;
		this.btnConnect.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnConnect.ForeColor = AirFader.Properties.Settings.Default.c_button_inactive_fore;
		this.btnConnect.LEDColor = System.Drawing.Color.WhiteSmoke;
		this.btnConnect.Location = new System.Drawing.Point(25, 17);
		this.btnConnect.Name = "btnConnect";
		this.btnConnect.ParentColor = this.panelTopR.BackColor;
		this.btnConnect.Size = new System.Drawing.Size(70, 55);
		this.btnConnect.TabIndex = 1;
		this.btnConnect.Text = "Connect";
		this.btnConnect.UseVisualStyleBackColor = false;
		this.btnConnect.Click += new System.EventHandler(btnConnect_Click);
		this.btnConfig.BackColor = AirFader.Properties.Settings.Default.c_button_inactive;
		this.btnConfig.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnConfig.BackText = "";
		this.btnConfig.BorderColor = System.Drawing.Color.Black;
		this.btnConfig.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnConfig.ForeColor = AirFader.Properties.Settings.Default.c_button_inactive_fore;
		this.btnConfig.LEDColor = System.Drawing.Color.WhiteSmoke;
		this.btnConfig.Location = new System.Drawing.Point(102, 17);
		this.btnConfig.Name = "btnConfig";
		this.btnConfig.ParentColor = this.panelTopR.BackColor;
		this.btnConfig.Size = new System.Drawing.Size(70, 55);
		this.btnConfig.TabIndex = 62;
		this.btnConfig.Text = "Config";
		this.btnConfig.UseVisualStyleBackColor = false;
		this.btnConfig.Click += new System.EventHandler(btnConfig_Click);
		this.btnFullScreen.BackColor = AirFader.Properties.Settings.Default.c_button_inactive;
		this.btnFullScreen.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnFullScreen.BackText = "";
		this.btnFullScreen.BorderColor = System.Drawing.Color.Black;
		this.btnFullScreen.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFullScreen.ForeColor = AirFader.Properties.Settings.Default.c_button_inactive_fore;
		this.btnFullScreen.LEDColor = System.Drawing.Color.WhiteSmoke;
		this.btnFullScreen.Location = new System.Drawing.Point(330, 17);
		this.btnFullScreen.Name = "btnFullScreen";
		this.btnFullScreen.ParentColor = this.panelTopR.BackColor;
		this.btnFullScreen.Size = new System.Drawing.Size(70, 55);
		this.btnFullScreen.TabIndex = 69;
		this.btnFullScreen.Text = "Full\nScreen";
		this.btnFullScreen.UseVisualStyleBackColor = false;
		this.btnFullScreen.Click += new System.EventHandler(btnFullScreen_Click);
		this.btnLock.BackColor = AirFader.Properties.Settings.Default.c_button_inactive;
		this.btnLock.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnLock.BackText = "";
		this.btnLock.BorderColor = System.Drawing.Color.Black;
		this.btnLock.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnLock.ForeColor = AirFader.Properties.Settings.Default.c_button_inactive_fore;
		this.btnLock.LEDColor = System.Drawing.Color.WhiteSmoke;
		this.btnLock.Location = new System.Drawing.Point(254, 17);
		this.btnLock.Name = "btnLock";
		this.btnLock.ParentColor = this.panelTopR.BackColor;
		this.btnLock.Size = new System.Drawing.Size(70, 55);
		this.btnLock.TabIndex = 83;
		this.btnLock.Text = "Lock";
		this.btnLock.UseVisualStyleBackColor = false;
		this.btnLock.Click += new System.EventHandler(btnLock_Click);
		this.btnFine.BackColor = AirFader.Properties.Settings.Default.c_button_inactive;
		this.btnFine.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnFine.BackText = "";
		this.btnFine.BorderColor = System.Drawing.Color.Black;
		this.btnFine.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFine.ForeColor = AirFader.Properties.Settings.Default.c_button_inactive_fore;
		this.btnFine.LEDColor = System.Drawing.Color.WhiteSmoke;
		this.btnFine.Location = new System.Drawing.Point(178, 17);
		this.btnFine.Name = "btnFine";
		this.btnFine.ParentColor = this.panelTopR.BackColor;
		this.btnFine.Size = new System.Drawing.Size(70, 55);
		this.btnFine.TabIndex = 220;
		this.btnFine.Text = "Fine\nMode";
		this.btnFine.UseVisualStyleBackColor = false;
		this.btnFine.Click += new System.EventHandler(btnFine_Click);
		this.panelUDF.BackColor = System.Drawing.Color.FromArgb(0, 0, 64);
		this.panelUDF.BorderColor = System.Drawing.Color.Black;
		this.panelUDF.BorderSize = 1f;
		this.panelUDF.Controls.Add(this.label3);
		this.panelUDF.EQColor = System.Drawing.Color.Lime;
		this.panelUDF.Location = new System.Drawing.Point(516, 386);
		this.panelUDF.Name = "panelUDF";
		this.panelUDF.Padding = new System.Windows.Forms.Padding(4);
		this.panelUDF.PanelType = 0;
		this.panelUDF.Size = new System.Drawing.Size(172, 83);
		this.panelUDF.TabIndex = 200;
		this.panelUDF.Visible = false;
		this.panelUDF.VisibleChanged += new System.EventHandler(panelUDF_VisibleChanged);
		this.label3.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label3.ForeColor = AirFader.Properties.Settings.Default.c_foreground;
		this.label3.Location = new System.Drawing.Point(22, 6);
		this.label3.Margin = new System.Windows.Forms.Padding(0);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(212, 22);
		this.label3.TabIndex = 115;
		this.label3.Text = "UDF";
		this.label3.Visible = false;
		this.panelSends.BackColor = System.Drawing.Color.FromArgb(64, 0, 64);
		this.panelSends.BorderColor = System.Drawing.Color.Black;
		this.panelSends.BorderSize = 1f;
		this.panelSends.Controls.Add(this.lblParamPage);
		this.panelSends.EQColor = System.Drawing.Color.Lime;
		this.panelSends.Location = new System.Drawing.Point(516, 475);
		this.panelSends.Name = "panelSends";
		this.panelSends.Padding = new System.Windows.Forms.Padding(4);
		this.panelSends.PanelType = 0;
		this.panelSends.Size = new System.Drawing.Size(172, 78);
		this.panelSends.TabIndex = 158;
		this.panelSends.Visible = false;
		this.lblParamPage.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblParamPage.ForeColor = AirFader.Properties.Settings.Default.c_foreground;
		this.lblParamPage.Location = new System.Drawing.Point(22, 6);
		this.lblParamPage.Margin = new System.Windows.Forms.Padding(0);
		this.lblParamPage.Name = "lblParamPage";
		this.lblParamPage.Size = new System.Drawing.Size(146, 22);
		this.lblParamPage.TabIndex = 115;
		this.lblParamPage.Text = "Sends On Faders";
		this.panelLayer.BackColor = System.Drawing.Color.FromArgb(0, 0, 72);
		this.panelLayer.BorderColor = System.Drawing.Color.Black;
		this.panelLayer.BorderSize = 1f;
		this.panelLayer.EQColor = System.Drawing.Color.Lime;
		this.panelLayer.Location = new System.Drawing.Point(516, 222);
		this.panelLayer.Name = "panelLayer";
		this.panelLayer.Padding = new System.Windows.Forms.Padding(2);
		this.panelLayer.PanelType = 0;
		this.panelLayer.Size = new System.Drawing.Size(172, 152);
		this.panelLayer.TabIndex = 162;
		this.panelLCD.BackColor = System.Drawing.Color.FromArgb(0, 0, 72);
		this.panelLCD.BorderColor = System.Drawing.Color.Black;
		this.panelLCD.BorderSize = 2f;
		this.panelLCD.Controls.Add(this.label5);
		this.panelLCD.Controls.Add(this.label4);
		this.panelLCD.Controls.Add(this.lblSceneName);
		this.panelLCD.Controls.Add(this.meterBattery);
		this.panelLCD.Controls.Add(this.lblClock);
		this.panelLCD.Controls.Add(this.lblConnStatus);
		this.panelLCD.EQColor = System.Drawing.Color.Lime;
		this.panelLCD.Location = new System.Drawing.Point(135, 8);
		this.panelLCD.Name = "panelLCD";
		this.panelLCD.Padding = new System.Windows.Forms.Padding(2);
		this.panelLCD.PanelType = 2;
		this.panelLCD.Size = new System.Drawing.Size(202, 93);
		this.panelLCD.TabIndex = 222;
		this.label5.AutoSize = true;
		this.label5.Font = new System.Drawing.Font("Arial", 8.25f, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, 0);
		this.label5.ForeColor = AirFader.Properties.Settings.Default.c_foreground;
		this.label5.Location = new System.Drawing.Point(5, 57);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(48, 14);
		this.label5.TabIndex = 132;
		this.label5.Text = "STATUS";
		this.label4.AutoSize = true;
		this.label4.Font = new System.Drawing.Font("Arial", 8.25f, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, 0);
		this.label4.ForeColor = AirFader.Properties.Settings.Default.c_foreground;
		this.label4.Location = new System.Drawing.Point(5, 18);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(41, 14);
		this.label4.TabIndex = 131;
		this.label4.Text = "SCENE";
		this.label4.Click += new System.EventHandler(panelLCD_Click);
		this.lblSceneName.Font = new System.Drawing.Font("Arial", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblSceneName.ForeColor = System.Drawing.Color.Lime;
		this.lblSceneName.Location = new System.Drawing.Point(5, 32);
		this.lblSceneName.Name = "lblSceneName";
		this.lblSceneName.Size = new System.Drawing.Size(192, 25);
		this.lblSceneName.TabIndex = 130;
		this.lblSceneName.Text = "123 - Demo Scene";
		this.lblSceneName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.lblSceneName.Click += new System.EventHandler(panelLCD_Click);
		this.meterBattery.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.meterBattery.BarColor = System.Drawing.Color.Lime;
		this.meterBattery.BarStyle = 12;
		this.meterBattery.BorderColor = System.Drawing.Color.DimGray;
		this.meterBattery.Channel = 0;
		this.meterBattery.Location = new System.Drawing.Point(8, 5);
		this.meterBattery.Maximum = 100;
		this.meterBattery.Minimum = 0;
		this.meterBattery.Name = "meterBattery";
		this.meterBattery.Size = new System.Drawing.Size(39, 10);
		this.meterBattery.TabIndex = 129;
		this.meterBattery.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.meterBattery.Value = new decimal(new int[4] { 50, 0, 0, 0 });
		this.meterBattery.ValueDB = new decimal(new int[4] { 1080, 0, 0, -2147352576 });
		this.lblClock.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblClock.ForeColor = System.Drawing.Color.Lime;
		this.lblClock.Location = new System.Drawing.Point(58, 2);
		this.lblClock.Name = "lblClock";
		this.lblClock.Size = new System.Drawing.Size(68, 17);
		this.lblClock.TabIndex = 76;
		this.lblClock.Text = "12:00am";
		this.lblClock.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblConnStatus.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblConnStatus.ForeColor = System.Drawing.Color.Yellow;
		this.lblConnStatus.Location = new System.Drawing.Point(5, 71);
		this.lblConnStatus.Name = "lblConnStatus";
		this.lblConnStatus.Size = new System.Drawing.Size(174, 19);
		this.lblConnStatus.TabIndex = 128;
		this.lblConnStatus.Text = "Not Connected";
		this.lblConnStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.lblDebug.AutoEllipsis = true;
		this.lblDebug.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblDebug.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblDebug.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
		this.lblDebug.Location = new System.Drawing.Point(106, 446);
		this.lblDebug.Name = "lblDebug";
		this.lblDebug.Size = new System.Drawing.Size(296, 23);
		this.lblDebug.TabIndex = 10;
		this.lblDebug.Text = "Debug...";
		this.lblDebug.Visible = false;
		this.lblDebug.Click += new System.EventHandler(lblDebug_Click);
		this.lblBrand.AutoSize = false;
		this.lblBrand.BackColor = System.Drawing.Color.FromArgb(0, 0, 72);
		this.lblBrand.Font = new System.Drawing.Font("Arial", 16f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblBrand.ForeColor = System.Drawing.Color.White;
		this.lblBrand.Location = new System.Drawing.Point(12, 8);
		this.lblBrand.Name = "lblBrand";
		this.lblBrand.Size = new System.Drawing.Size(117, 93);
		this.lblBrand.TabIndex = 122;
		this.lblBrand.Text = "01VMIX";
		this.lblBrand.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblBrand.Click += new System.EventHandler(lblBrand_Click);
		this.lblBrand.DoubleClick += new System.EventHandler(lblBrand_DoubleClick);
		this.panelHome.BackColor = System.Drawing.Color.FromArgb(0, 0, 72);
		this.panelHome.BorderColor = System.Drawing.Color.Black;
		this.panelHome.BorderSize = 1f;
		this.panelHome.Controls.Add(this.lblPanelHome);
		this.panelHome.EQColor = System.Drawing.Color.Lime;
		this.panelHome.Location = new System.Drawing.Point(516, 559);
		this.panelHome.Name = "panelHome";
		this.panelHome.PanelType = 0;
		this.panelHome.Size = new System.Drawing.Size(172, 62);
		this.panelHome.TabIndex = 160;
		this.panelHome.Visible = false;
		this.panelHome.VisibleChanged += new System.EventHandler(panelHome_VisibleChanged);
		this.lblPanelHome.Font = new System.Drawing.Font("Arial", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblPanelHome.ForeColor = AirFader.Properties.Settings.Default.c_foreground;
		this.lblPanelHome.Location = new System.Drawing.Point(21, 6);
		this.lblPanelHome.Name = "lblPanelHome";
		this.lblPanelHome.Size = new System.Drawing.Size(146, 22);
		this.lblPanelHome.TabIndex = 82;
		this.lblPanelHome.Text = "Mute Groups";
		this.lblPanelHome.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.panelConsole.BackColor = System.Drawing.Color.FromArgb(0, 0, 72);
		this.panelConsole.BorderColor = System.Drawing.Color.Black;
		this.panelConsole.BorderSize = 1f;
		this.panelConsole.EQColor = System.Drawing.Color.Lime;
		this.panelConsole.Location = new System.Drawing.Point(739, 149);
		this.panelConsole.Name = "panelConsole";
		this.panelConsole.Padding = new System.Windows.Forms.Padding(4);
		this.panelConsole.PanelType = 0;
		this.panelConsole.Size = new System.Drawing.Size(275, 93);
		this.panelConsole.TabIndex = 159;
		this.panelConsole.Resize += new System.EventHandler(panelConsole_Resize);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		this.BackColor = AirFader.Properties.Settings.Default.c_background;
		base.ClientSize = new System.Drawing.Size(1012, 711);
		base.Controls.Add(this.lblBrand);
		base.Controls.Add(this.lblDebug);
		base.Controls.Add(this.panelLCD);
		base.Controls.Add(this.lbLog);
		base.Controls.Add(this.lblLocked);
		base.Controls.Add(this.panelUDF);
		base.Controls.Add(this.panelSends);
		base.Controls.Add(this.panelLayer);
		base.Controls.Add(this.txtRAW);
		base.Controls.Add(this.panelFaders);
		base.Controls.Add(this.panelTopR);
		base.Controls.Add(this.panelHome);
		base.Controls.Add(this.panelConsole);
		this.DoubleBuffered = true;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.KeyPreview = true;
		base.Name = "frmSurface16";
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "01vmix";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(frmSurface16_FormClosing);
		base.Load += new System.EventHandler(frmSurface16_Load);
		base.ResizeBegin += new System.EventHandler(frmSurface16_ResizeBegin);
		base.ResizeEnd += new System.EventHandler(frmSurface16_ResizeEnd);
		base.LocationChanged += new System.EventHandler(frmSurface16_LocationChanged);
		base.Resize += new System.EventHandler(frmSurface16_Resize);
		this.panelTopR.ResumeLayout(false);
		this.panelUDF.ResumeLayout(false);
		this.panelSends.ResumeLayout(false);
		this.panelLCD.ResumeLayout(false);
		this.panelLCD.PerformLayout();
		this.panelHome.ResumeLayout(false);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
