using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AirControls;
using AirFader.Properties;

namespace AirFader;

public class frmChannel : Form
{
	private delegate void EngineChangedCB(MixEngine.Pr param, int rack, int paramIndex, int value);

	private delegate void CCCallback(Channel.Pr param, int channel, int paramIndex, int value);

	private delegate void ConsoleCallback(string property);

	private delegate void setDynCallback(int channel, int newType, bool reset);

	private WindowStatic thisWin = new WindowStatic();

	private bool isFullScreen;

	private Rectangle m_Bounds;

	private AirControls.AirFader[] arrFaders;

	private AirButton[] arrMuteAssign;

	private AirButton[] arrDCAAssign;

	private Label[] arrSendChanLabels;

	private AirButton[] arrSendPrePost;

	private AirButton[] arrSendOn;

	private int activeChannel = -1;

	public ChannelTypes channelType;

	private PEQ thisPEQ = new PEQ();

	private bool surfaceChanged;

	private int meterBeats;

	private int SOFPage;

	private bool fineMode;

	public bool consoleLink;

	private frmSurface16 parentSurface;

	private Color colorActive = Settings.Default.c_button_active;

	private Color colorInactive = Settings.Default.c_button_inactive;

	private Color colorActive_fore = Settings.Default.c_button_active_fore;

	private Color colorInactive_fore = Settings.Default.c_button_inactive_fore;

	private System.Threading.Timer heartBeat;

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

	private byte[] meterData;

	private Point[] eqPoints = new Point[4];

	private AirKnob activeKnob;

	private AirKnob activeQ;

	private AirKnob activeFreq;

	private AirKnob activeGain;

	private bool isDrag;

	private int activeEQ;

	private int nearEQ;

	private int startX;

	private int startY;

	private int startF;

	private int startG;

	private bool isResizing;

	private Size lastSize = new Size(0, 0);

	private IContainer components;

	private Label lblChan07;

	private Label lblChan06;

	private Label lblChan05;

	private Label lblChan04;

	private Label lblChan03;

	private Label lblChan02;

	private Label lblChan01;

	private Label lblChan00;

	private AirControls.AirFader fader7;

	private AirControls.AirFader fader6;

	private AirControls.AirFader fader5;

	private AirControls.AirFader fader4;

	private AirControls.AirFader fader3;

	private AirControls.AirFader fader2;

	private AirControls.AirFader fader1;

	private AirControls.AirFader fader0;

	private AirButton btnPage0;

	private AirButton btnPage1;

	private AirButton btnClose;

	private CTabControl tabSection;

	private TabPage tabSends;

	private TabPage tabEQ;

	private TabPage tabDyn;

	private AirKnob dyn05;

	private AirKnob dyn04;

	private AirKnob dyn03;

	private AirKnob dyn02;

	private AirKnob dyn01;

	private AirKnob dyn00;

	private AirControls.AirFader airFader1;

	private AirControls.AirFader chanFader;

	private AirControls.AirFader faderGain;

	private AirControls.AirFader faderFreq;

	private AirKnob eq4G;

	private AirKnob eq3G;

	private AirKnob eq2G;

	private AirKnob eq1G;

	private AirKnob eq4F;

	private AirKnob eq3F;

	private AirKnob eq2F;

	private AirKnob eq1F;

	private AirKnob eq4Q;

	private AirKnob eq3Q;

	private AirKnob eq2Q;

	private AirKnob eq1Q;

	private AirControls.AirFader faderQ;

	private AirButton btnToMonoOn;

	private AirButton btnToStereoOn;

	private AirButton btnDyn0Type;

	private AirButton chanPhase;

	private AirButton chanPhantom;

	private AirButton chanPanMode;

	private AirButton btnDirectOut;

	private AirButton btnInsertOn;

	private AirButton btnMuteAssign7;

	private AirButton btnMuteAssign6;

	private AirButton btnMuteAssign5;

	private AirButton btnMuteAssign4;

	private AirButton btnMuteAssign3;

	private AirButton btnMuteAssign2;

	private AirButton btnMuteAssign1;

	private AirButton btnMuteAssign0;

	private Label lblMuteGroups;

	private AirButton btnSafe;

	private AirButton btnSendOn07;

	private AirButton btnSendPre07;

	private AirButton btnSendOn06;

	private AirButton btnSendPre06;

	private AirButton btnSendOn05;

	private AirButton btnSendPre05;

	private AirButton btnSendOn04;

	private AirButton btnSendPre04;

	private AirButton btnSendOn03;

	private AirButton btnSendPre03;

	private AirButton btnSendPre02;

	private AirButton btnSendOn01;

	private AirButton btnSendPre01;

	private AirButton btnSendOn00;

	private AirButton btnSendPre00;

	private AirKnob eqHPFFreq;

	private AirKnob chanLCRLevel;

	private AirKnob chanPan;

	private Label lblHeader;

	private AirButton btnDyn0On;

	private AirPanel panelDyn0;

	private AirPanel panelMutes;

	private AirPanel panelPAN;

	private AirPanel panelDyn1;

	private AirKnob dyn15;

	private AirKnob dyn10;

	private AirKnob dyn11;

	private AirKnob dyn12;

	private AirButton btnDyn1On;

	private AirKnob dyn13;

	private AirKnob dyn14;

	private AirButton btnDyn1Type;

	private AirPanel airPanel5;

	private AirPanel panelHA;

	private AirPanel airPanel7;

	private AirButton btnNext;

	private AirButton btnPrev;

	private Label lblQ;

	private AirMeter airMeterPre;

	private AirMeter airMeterPost;

	private AirButton btnFine;

	private AirPanel picEQ;

	private Panel panelTopMenu;

	private AirButton btnHPFOn;

	private AirButton btnLowEQType;

	private AirButton btnHiEQType;

	private AirKnob eqAtt;

	private AirButton btnEQOn;

	private AirMeter meterDyn0In;

	private AirMeter meterDyn1Gain;

	private AirMeter meterDyn1In;

	private AirMeter meterDyn0Gain;

	private Label lblEq1;

	private Label lblEq4;

	private Label lblEq3;

	private Label lblEq2;

	private AirButton btnFlat;

	private AirKnob knobHA;

	private Label lblParam;

	private AirButton btnSendOn02;

	private AirButton chanMute;

	private AirKnob eq4LPF;

	private Label lblFreq;

	private AirButton btnCopy;

	private Label lblGain;

	private AirButton btnPaste;

	private AirButton btnOptions;

	private AirButton btnDCAAssign0;

	private AirButton btnDCAAssign1;

	private AirButton btnDCAAssign2;

	private AirButton btnDCAAssign3;

	private AirButton btnDCAAssign4;

	private AirButton btnDCAAssign5;

	private AirButton btnDCAAssign6;

	private AirButton btnDCAAssign7;

	private AirPanel panelDCA;

	private Label lblDCA;

	private AirButton btnPage2;

	private AirButton btnJump;

	public int TabHeight => tabDyn.Height;

	public int ActiveChannel
	{
		get
		{
			return activeChannel;
		}
		set
		{
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: Setting ActiveChannel to " + value);
			}
			if (activeChannel == value || value < 0 || value >= MixEngine.chData.Length)
			{
				return;
			}
			if (MixEngine.chData[value].channelType == ChannelTypes.None)
			{
				Loc.Msg("Invalid Channel #" + (value + 1));
				return;
			}
			activeChannel = value;
			channelType = MixEngine.chData[activeChannel].channelType;
			if (consoleLink)
			{
				MixEngine.setParam("SelChannel", activeChannel, 1);
				Console.selChannel = activeChannel;
			}
			updateUI();
		}
	}

	private Font FontFit(string fontName, FontStyle fontStyle, string thisText, int thisWide, int thisHigh, float maxSize)
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

	private void updateUI()
	{
		chanFader.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.Fader);
		bool groupMute = false;
		for (int i = 0; i < 8; i++)
		{
			if (MixEngine.muteGroup[i].members.Contains(activeChannel) && MixEngine.getParam(i, MixEngine.Pr.MuteMaster, 0) == 1)
			{
				groupMute = true;
			}
		}
		setMuteColor(chanMute, MixEngine.chData[activeChannel].getParamBool(Channel.Pr.On), groupMute);
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			chanPan.KnobLabel = "Pan";
			chanPan.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.Pan);
		}
		else if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) > 0)
		{
			chanPan.KnobLabel = "Balance";
			chanPan.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.Balance);
		}
		else
		{
			chanPan.KnobLabel = "Pan";
			chanPan.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.Pan);
		}
		knobHA.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.HA);
		knobHA.KnobLabel = MixEngine.chData[activeChannel].getParamStr(Channel.Pr.PatchIn);
		chanPhantom.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Phantom) ? colorActive : colorInactive);
		chanPhantom.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Phantom) ? colorActive_fore : colorInactive_fore);
		chanPhase.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Phase) ? colorActive : colorInactive);
		chanPhase.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Phase) ? colorActive_fore : colorInactive_fore);
		btnInsertOn.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.InsertOn) ? colorActive : colorInactive);
		btnInsertOn.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.InsertOn) ? colorActive_fore : colorInactive_fore);
		btnDirectOut.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.DirectOutOn) ? colorActive : colorInactive);
		btnDirectOut.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.DirectOutOn) ? colorActive_fore : colorInactive_fore);
		btnToStereoOn.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.ToStereoOn) ? colorActive : colorInactive);
		btnToStereoOn.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.ToStereoOn) ? colorActive_fore : colorInactive_fore);
		btnToMonoOn.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.ToMonoOn) ? colorActive : colorInactive);
		btnToMonoOn.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.ToMonoOn) ? colorActive_fore : colorInactive_fore);
		btnDyn0On.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Dyn0On) ? colorActive : colorInactive);
		btnDyn0On.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Dyn0On) ? colorActive_fore : colorInactive_fore);
		btnDyn1On.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Dyn1On) ? colorActive : colorInactive);
		btnDyn1On.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Dyn1On) ? colorActive_fore : colorInactive_fore);
		setDyn0Type(activeChannel, MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn0Type), reset: false);
		setDyn1Type(activeChannel, MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn1Type), reset: false);
		btnEQOn.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQOn) ? colorActive : colorInactive);
		btnEQOn.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQOn) ? colorActive_fore : colorInactive_fore);
		btnHPFOn.BackColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQHPFOn) ? colorActive : colorInactive);
		btnHPFOn.ForeColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQHPFOn) ? colorActive_fore : colorInactive_fore);
		eqHPFFreq.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQHPFFreq);
		eqAtt.Maximum = MixEngine.chData[activeChannel].paramMax[Channel.Pr.EQAtt];
		eqAtt.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQAtt);
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			eqAtt.Steps = 1;
		}
		else
		{
			eqAtt.Steps = 10;
		}
		picEQ.EQColor = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQOn) ? Settings.Default.EQActive : Settings.Default.EQInactive);
		setEQType(1, MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type));
		setEQType(4, MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type));
		eq1Q.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Q);
		eq1F.Minimum = MixEngine.chData[activeChannel].paramMin[Channel.Pr.EQ1F];
		eq1F.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1F);
		if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 2)
		{
			eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1HPFOn);
		}
		else
		{
			eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1G);
		}
		eq2Q.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ2Q);
		eq2F.Minimum = MixEngine.chData[activeChannel].paramMin[Channel.Pr.EQ2F];
		eq2F.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ2F);
		eq2G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ2G);
		eq3Q.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ3Q);
		eq3F.Minimum = MixEngine.chData[activeChannel].paramMin[Channel.Pr.EQ3F];
		eq3F.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ3F);
		eq3G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ3G);
		eq4Q.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Q);
		eq4F.Minimum = MixEngine.chData[activeChannel].paramMin[Channel.Pr.EQ4F];
		eq4F.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4F);
		eq4G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4G);
		eq4LPF.Value = (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQ4LPFOn) ? 1 : 0);
		setSOFPage(SOFPage);
		for (int j = 0; j < 8; j++)
		{
			if (MixEngine.muteGroup[j].members.Contains(activeChannel))
			{
				arrMuteAssign[j].BackColor = Settings.Default.c_button_active;
				arrMuteAssign[j].ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				arrMuteAssign[j].BackColor = Settings.Default.c_button_inactive;
				arrMuteAssign[j].ForeColor = Settings.Default.c_button_inactive_fore;
			}
			if (MixEngine.DCA[j].members.Contains(activeChannel))
			{
				arrDCAAssign[j].BackColor = Settings.Default.c_button_active;
				arrDCAAssign[j].ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				arrDCAAssign[j].BackColor = Settings.Default.c_button_inactive;
				arrDCAAssign[j].ForeColor = Settings.Default.c_button_inactive_fore;
			}
		}
		switch (channelType)
		{
		case ChannelTypes.Input:
			chanFader.ValueType = "fader";
			chanPhase.Show();
			panelHA.Show();
			panelDyn0.Show();
			btnToStereoOn.Show();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				btnToMonoOn.Hide();
				knobHA.Show();
				knobHA.Enabled = false;
				chanPhantom.Hide();
				eqHPFFreq.Hide();
				btnHPFOn.Hide();
			}
			else
			{
				btnToMonoOn.Show();
				knobHA.Show();
				knobHA.Enabled = true;
				chanPhantom.Show();
				eqHPFFreq.Show();
				btnHPFOn.Show();
			}
			panelPAN.Show();
			chanFader.FaderColor = "White";
			if (Console.type == ConsoleTypes.M7CL)
			{
				panelDCA.Show();
				panelMutes.Show();
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelDCA.Show();
				panelMutes.Show();
			}
			else
			{
				panelDCA.Hide();
				panelMutes.Show();
			}
			if (!tabSection.TabPages.Contains(tabSends))
			{
				tabSection.TabPages.Add(tabSends);
			}
			btnPage0.Text = "Mix\r\n1 - 8";
			btnPage1.Text = "Mix\r\n9 - 16";
			btnPage2.Text = "Matrix\r\n1 - 8";
			btnPage0.Show();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				btnPage1.Hide();
			}
			else
			{
				btnPage1.Show();
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				btnPage2.Show();
			}
			else
			{
				btnPage2.Hide();
			}
			if (activeKnob == knobHA)
			{
				airKnob_Click(knobHA, EventArgs.Empty);
			}
			break;
		case ChannelTypes.STIN:
			chanFader.ValueType = "fader";
			chanPhase.Show();
			panelHA.Show();
			panelDyn0.Show();
			btnToStereoOn.Show();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				btnToMonoOn.Hide();
				knobHA.Show();
				knobHA.Enabled = false;
				chanPhantom.Hide();
				btnHPFOn.Hide();
				eqHPFFreq.Hide();
			}
			else
			{
				btnToMonoOn.Show();
				knobHA.Show();
				knobHA.Enabled = true;
				chanPhantom.Show();
				btnHPFOn.Show();
				eqHPFFreq.Show();
			}
			panelPAN.Show();
			chanFader.FaderColor = "White";
			if (Console.type == ConsoleTypes.M7CL)
			{
				panelDCA.Show();
				panelMutes.Show();
			}
			else if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelDCA.Show();
				panelMutes.Show();
			}
			else
			{
				panelDCA.Hide();
				panelMutes.Show();
			}
			if (!tabSection.TabPages.Contains(tabSends))
			{
				tabSection.TabPages.Add(tabSends);
			}
			btnPage0.Text = "Mix\r\n1 - 8";
			btnPage1.Text = "Mix\r\n9 - 16";
			btnPage2.Text = "Matrix\r\n1 - 8";
			btnPage0.Show();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				btnPage1.Hide();
			}
			else
			{
				btnPage1.Show();
			}
			btnPage2.Hide();
			if (activeKnob == knobHA)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			break;
		case ChannelTypes.Mix:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				chanFader.ValueType = "busfader";
			}
			else
			{
				chanFader.ValueType = "fader";
			}
			panelHA.Hide();
			panelDyn0.Hide();
			btnToStereoOn.Show();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				btnToMonoOn.Hide();
			}
			else
			{
				btnToMonoOn.Show();
			}
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelPAN.Hide();
			}
			else
			{
				panelPAN.Show();
			}
			btnHPFOn.Hide();
			eqHPFFreq.Hide();
			chanFader.FaderColor = "Purple";
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelDCA.Hide();
				panelMutes.Hide();
				if (tabSection.TabPages.Contains(tabSends))
				{
					tabSection.TabPages.Remove(tabSends);
				}
			}
			else
			{
				panelDCA.Hide();
				panelMutes.Show();
				if (!tabSection.TabPages.Contains(tabSends))
				{
					tabSection.TabPages.Add(tabSends);
				}
			}
			btnPage0.Text = "Mix\r\n1 - 8";
			btnPage1.Text = "Mix\r\n9 - 16";
			btnPage2.Text = "Matrix\r\n1 - 8";
			btnPage0.Hide();
			btnPage1.Hide();
			btnPage2.Show();
			if (activeKnob == knobHA)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			break;
		case ChannelTypes.Matrix:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				chanFader.ValueType = "busfader";
			}
			else
			{
				chanFader.ValueType = "fader";
			}
			panelHA.Hide();
			panelDyn0.Hide();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				btnToStereoOn.Show();
			}
			else
			{
				btnToStereoOn.Hide();
			}
			btnToMonoOn.Hide();
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelPAN.Show();
			}
			else if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) == 0)
			{
				panelPAN.Hide();
			}
			else
			{
				panelPAN.Show();
			}
			btnHPFOn.Hide();
			eqHPFFreq.Hide();
			chanFader.FaderColor = "Purple";
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelDCA.Hide();
				panelMutes.Hide();
			}
			else
			{
				panelDCA.Hide();
				panelMutes.Show();
			}
			if (tabSection.TabPages.Contains(tabSends))
			{
				tabSection.TabPages.Remove(tabSends);
			}
			btnPage0.Text = "Mix\r\n1 - 8";
			btnPage1.Text = "Mix\r\n9 - 16";
			btnPage2.Text = "Matrix\r\n1 - 8";
			btnPage0.Hide();
			btnPage1.Hide();
			btnPage2.Show();
			if (activeKnob == knobHA)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			if (activeKnob == chanPan)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			break;
		case ChannelTypes.Stereo:
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				chanFader.ValueType = "busfader";
			}
			else
			{
				chanFader.ValueType = "fader";
			}
			panelHA.Hide();
			panelDyn0.Hide();
			btnToStereoOn.Hide();
			btnToMonoOn.Hide();
			panelPAN.Show();
			btnHPFOn.Hide();
			eqHPFFreq.Hide();
			chanFader.FaderColor = "Red";
			if (Console.type == ConsoleTypes.O1V96V2)
			{
				panelDCA.Hide();
				panelMutes.Hide();
			}
			else
			{
				panelDCA.Hide();
				panelMutes.Show();
			}
			if (!tabSection.TabPages.Contains(tabSends))
			{
				tabSection.TabPages.Add(tabSends);
			}
			btnPage0.Text = "Mix\r\n1 - 8";
			btnPage1.Text = "Mix\r\n9 - 16";
			btnPage2.Text = "Matrix\r\n1 - 8";
			btnPage0.Hide();
			btnPage1.Hide();
			btnPage2.Show();
			if (activeKnob == knobHA)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			if (activeKnob == chanPan)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			break;
		}
		setHeader();
		if (activeQ == null)
		{
			setEQBand(eq1Q.Tag.ToString());
		}
		else
		{
			setEQBand(activeQ.Tag.ToString());
		}
		meterBeats = 0;
		airMeterPost.Value = 0m;
		airMeterPre.Value = 0m;
		picEQ_Plot();
		if (activeKnob == null)
		{
			activeKnob = dyn00;
		}
		airFader1.ValueType = activeKnob.ValueType;
		airFader1.TickType = activeKnob.ValueType;
		airFader1.Maximum = activeKnob.Maximum;
		airFader1.Minimum = activeKnob.Minimum;
		airFader1.Steps = activeKnob.Steps;
		airFader1.Value = activeKnob.Value;
		lblParam.Text = activeKnob.KnobLabel;
	}

	private void MixEngine_EngineChanged(MixEngine.Pr param, int rack, int paramIndex, int value)
	{
		if (activeChannel == -1)
		{
			return;
		}
		if (dyn00.InvokeRequired)
		{
			EngineChangedCB method = MixEngine_EngineChanged;
			BeginInvoke(method, param, rack, paramIndex, value);
			return;
		}
		bool flag = false;
		switch (param)
		{
		case MixEngine.Pr.None:
			updateUI();
			break;
		case MixEngine.Pr.MuteMaster:
		{
			flag = false;
			for (int i = 0; i < 8; i++)
			{
				if (MixEngine.muteGroup[i].members.Contains(activeChannel))
				{
					arrMuteAssign[i].BackColor = Settings.Default.c_button_active;
					arrMuteAssign[i].ForeColor = Settings.Default.c_button_active_fore;
					if (MixEngine.getParam(i, MixEngine.Pr.MuteMaster, 0) == 1)
					{
						flag = true;
					}
				}
				else
				{
					arrMuteAssign[i].BackColor = Settings.Default.c_button_inactive;
					arrMuteAssign[i].ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			setMuteColor(chanMute, MixEngine.chData[activeChannel].getParamBool(Channel.Pr.On), flag);
			break;
		}
		case MixEngine.Pr.HA:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.PatchIn) - 1 == rack)
			{
				knobHA.Value = value;
				if (activeKnob == knobHA)
				{
					airFader1.Value = knobHA.Value;
				}
			}
			break;
		case MixEngine.Pr.Phantom:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.PatchIn) - 1 == rack)
			{
				if (value > 0)
				{
					chanPhantom.BackColor = Settings.Default.c_button_active;
					chanPhantom.ForeColor = Settings.Default.c_button_active_fore;
				}
				else
				{
					chanPhantom.BackColor = Settings.Default.c_button_inactive;
					chanPhantom.ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			break;
		}
	}

	private void setKnobByTag(AirPanel panel, Channel.Pr param, int value)
	{
		foreach (Control control in panel.Controls)
		{
			if (((control is AirKnob) & (control.Tag is Channel.Pr)) && (Channel.Pr)control.Tag == param)
			{
				((AirKnob)control).Value = value;
			}
		}
	}

	private void MixEngine_ChannelChanged(Channel.Pr param, int channel, int paramIndex, int value)
	{
		if (activeChannel == -1)
		{
			return;
		}
		if (dyn00.InvokeRequired)
		{
			CCCallback method = MixEngine_ChannelChanged;
			Invoke(method, param, channel, paramIndex, value);
			return;
		}
		switch (param)
		{
		case Channel.Pr.MeterPre:
			airMeterPre.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.MeterPre);
			airMeterPost.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.MeterPost);
			meterDyn0In.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.MeterDyn0In);
			meterDyn1In.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.MeterDyn1In);
			meterDyn0Gain.Value = Math.Abs(MixEngine.chData[activeChannel].getParam(Channel.Pr.MeterDyn0GR) - 126);
			meterDyn1Gain.Value = Math.Abs(MixEngine.chData[activeChannel].getParam(Channel.Pr.MeterDyn1GR) - 126);
			return;
		case Channel.Pr.Pair:
			if (MixEngine.chData[channel].channelType == ChannelTypes.Mix || MixEngine.chData[channel].channelType == ChannelTypes.Matrix)
			{
				setSOFPage(SOFPage);
				if (MixEngine.chData[activeChannel].channelType == ChannelTypes.Matrix && MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) == 0)
				{
					panelPAN.Hide();
				}
				else
				{
					panelPAN.Show();
				}
				if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) > 0)
				{
					chanPan.KnobLabel = "Balance";
					chanPan.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.Balance);
				}
				else
				{
					chanPan.KnobLabel = "Pan";
					chanPan.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.Pan);
				}
				return;
			}
			break;
		}
		if (channel != activeChannel)
		{
			return;
		}
		bool flag = false;
		switch (param)
		{
		case Channel.Pr.PatchIn:
			knobHA.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.HA);
			if (activeKnob == knobHA)
			{
				airFader1.Value = knobHA.Value;
			}
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Phantom) > 0)
			{
				chanPhantom.BackColor = Settings.Default.c_button_active;
				chanPhantom.ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				chanPhantom.BackColor = Settings.Default.c_button_inactive;
				chanPhantom.ForeColor = Settings.Default.c_button_inactive_fore;
			}
			knobHA.KnobLabel = MixEngine.mapInPatch[value];
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.PatchIn) > 0 && MixEngine.chData[activeChannel].getParam(Channel.Pr.PatchIn) <= Console.maxInputs && Console.type != ConsoleTypes.O1V96V2)
			{
				knobHA.Enabled = true;
				chanPhantom.Show();
				break;
			}
			knobHA.Enabled = false;
			if (activeKnob == knobHA)
			{
				if (dyn00.Visible)
				{
					airKnob_Click(dyn00, EventArgs.Empty);
				}
				else
				{
					airKnob_Click(dyn10, EventArgs.Empty);
				}
			}
			chanPhantom.Hide();
			break;
		case Channel.Pr.Phase:
			if (value > 0)
			{
				chanPhase.BackColor = Settings.Default.c_button_active;
				chanPhase.ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				chanPhase.BackColor = Settings.Default.c_button_inactive;
				chanPhase.ForeColor = Settings.Default.c_button_inactive_fore;
			}
			break;
		case Channel.Pr.On:
		case Channel.Pr.MuteGroup:
		{
			flag = false;
			for (int n = 0; n < 8; n++)
			{
				if (MixEngine.muteGroup[n].members.Contains(activeChannel))
				{
					if (param == Channel.Pr.MuteGroup)
					{
						arrMuteAssign[n].BackColor = Settings.Default.c_button_active;
						arrMuteAssign[n].ForeColor = Settings.Default.c_button_active_fore;
					}
					if (MixEngine.getParam(n, MixEngine.Pr.MuteMaster, 0) == 1)
					{
						flag = true;
					}
				}
				else if (param == Channel.Pr.MuteGroup)
				{
					arrMuteAssign[n].BackColor = Settings.Default.c_button_inactive;
					arrMuteAssign[n].ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			setMuteColor(chanMute, MixEngine.chData[activeChannel].getParamBool(Channel.Pr.On), flag);
			break;
		}
		case Channel.Pr.DCAGroup:
		{
			for (int num4 = 0; num4 < 8; num4++)
			{
				if (MixEngine.DCA[num4].members.Contains(activeChannel))
				{
					arrDCAAssign[num4].BackColor = Settings.Default.c_button_active;
					arrDCAAssign[num4].ForeColor = Settings.Default.c_button_active_fore;
				}
				else
				{
					arrDCAAssign[num4].BackColor = Settings.Default.c_button_inactive;
					arrDCAAssign[num4].ForeColor = Settings.Default.c_button_inactive_fore;
				}
			}
			break;
		}
		case Channel.Pr.Fader:
			chanFader.Value = value;
			break;
		case Channel.Pr.Pan:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) == 0 || Console.type == ConsoleTypes.O1V96V2)
			{
				chanPan.Value = value;
			}
			if (activeKnob == chanPan)
			{
				airFader1.Value = chanPan.Value;
			}
			break;
		case Channel.Pr.Balance:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) > 0)
			{
				chanPan.Value = value;
			}
			if (activeKnob == chanPan)
			{
				airFader1.Value = chanPan.Value;
			}
			break;
		case Channel.Pr.SendToMix:
		{
			for (int m = 0; m < 8; m++)
			{
				if (arrFaders[m].Channel == paramIndex && arrFaders[m].ValueType == "fader")
				{
					arrFaders[m].Value = value;
				}
			}
			break;
		}
		case Channel.Pr.SendToMixOn:
		{
			for (int l = 0; l < 8; l++)
			{
				if (arrFaders[l].Channel == paramIndex)
				{
					setMuteColor(arrSendOn[l], value == 1, groupMute: false);
				}
			}
			break;
		}
		case Channel.Pr.SendToMixPre:
		{
			for (int k = 0; k < 8; k++)
			{
				if (arrFaders[k].Channel == paramIndex)
				{
					arrSendPrePost[k].BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrSendPrePost[k].ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
				}
			}
			break;
		}
		case Channel.Pr.SendToMixPan:
		{
			for (int j = 0; j < 8; j++)
			{
				if (arrFaders[j].Channel / 2 == paramIndex / 2 && arrFaders[j].ValueType == "pan")
				{
					arrFaders[j].Value = value;
				}
			}
			break;
		}
		case Channel.Pr.SendToMatrix:
		{
			for (int i = 0; i < 8; i++)
			{
				if (arrFaders[i].Channel - 16 == paramIndex)
				{
					arrFaders[i].Value = value;
				}
			}
			break;
		}
		case Channel.Pr.SendToMatrixOn:
		{
			for (int num2 = 0; num2 < 8; num2++)
			{
				if (arrFaders[num2].Channel - 16 == paramIndex)
				{
					setMuteColor(arrSendOn[num2], value == 1, groupMute: false);
				}
			}
			break;
		}
		case Channel.Pr.SendToMatrixPre:
		{
			for (int num = 0; num < 8; num++)
			{
				if (arrFaders[num].Channel - 16 == paramIndex)
				{
					arrSendPrePost[num].BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrSendPrePost[num].ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
				}
			}
			break;
		}
		case Channel.Pr.SendToMatrixPan:
		{
			for (int num3 = 0; num3 < 8; num3++)
			{
				if ((arrFaders[num3].Channel - 16) / 2 == paramIndex / 2 && arrFaders[num3].ValueType == "pan")
				{
					arrFaders[num3].Value = value;
				}
			}
			break;
		}
		case Channel.Pr.InsertOn:
			btnInsertOn.BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnInsertOn.ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		case Channel.Pr.DirectOutOn:
			btnDirectOut.BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnDirectOut.ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		case Channel.Pr.ToStereoOn:
			btnToStereoOn.BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnToStereoOn.ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		case Channel.Pr.ToMonoOn:
			btnToMonoOn.BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnToMonoOn.ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		case Channel.Pr.Name:
			setHeader();
			break;
		case Channel.Pr.Dyn0On:
			btnDyn0On.BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnDyn0On.ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		case Channel.Pr.Dyn0Type:
			setDyn0Type(activeChannel, value, reset: true);
			break;
		case Channel.Pr.Dyn0Attack:
		case Channel.Pr.Dyn0Range:
		case Channel.Pr.Dyn0Hold:
		case Channel.Pr.Dyn0Decay:
		case Channel.Pr.Dyn0Ratio:
		case Channel.Pr.Dyn0Gain:
		case Channel.Pr.Dyn0Knee:
			setKnobByTag(panelDyn0, param, value);
			break;
		case Channel.Pr.Dyn0Threshold:
			meterDyn0In.TickMark = (decimal)value / 10.0m;
			setKnobByTag(panelDyn0, param, value);
			break;
		case Channel.Pr.Dyn1On:
			btnDyn1On.BackColor = ((value == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			btnDyn1On.ForeColor = ((value == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			break;
		case Channel.Pr.Dyn1Type:
			setDyn1Type(activeChannel, value, reset: true);
			break;
		case Channel.Pr.Dyn1Attack:
		case Channel.Pr.Dyn1Range:
		case Channel.Pr.Dyn1Hold:
		case Channel.Pr.Dyn1Release:
		case Channel.Pr.Dyn1Ratio:
		case Channel.Pr.Dyn1Gain:
		case Channel.Pr.Dyn1Knee:
			setKnobByTag(panelDyn1, param, value);
			break;
		case Channel.Pr.Dyn1Threshold:
			meterDyn1In.TickMark = (decimal)value / 10.0m;
			setKnobByTag(panelDyn1, param, value);
			break;
		case Channel.Pr.EQHPFOn:
			if (value == 1)
			{
				btnHPFOn.BackColor = Settings.Default.c_button_active;
				btnHPFOn.ForeColor = Settings.Default.c_button_active_fore;
			}
			else
			{
				btnHPFOn.BackColor = Settings.Default.c_button_inactive;
				btnHPFOn.ForeColor = Settings.Default.c_button_inactive_fore;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQHPFFreq:
			eqHPFFreq.Value = value;
			if (activeFreq == eqHPFFreq)
			{
				faderFreq.Value = eqHPFFreq.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQAtt:
			eqAtt.Value = value;
			if (activeGain == eqAtt)
			{
				faderGain.Value = eqAtt.Value;
			}
			break;
		case Channel.Pr.EQType:
			picEQ_Plot();
			break;
		case Channel.Pr.EQOn:
			if (value == 1)
			{
				btnEQOn.BackColor = Settings.Default.c_button_active;
				btnEQOn.ForeColor = Settings.Default.c_button_active_fore;
				picEQ.EQColor = Settings.Default.EQActive;
				picEQ_Plot();
			}
			else
			{
				btnEQOn.BackColor = Settings.Default.c_button_inactive;
				btnEQOn.ForeColor = Settings.Default.c_button_inactive_fore;
				picEQ.EQColor = Settings.Default.EQInactive;
				picEQ_Plot();
			}
			break;
		case Channel.Pr.EQ1Type:
			setEQType(1, value);
			picEQ_Plot();
			break;
		case Channel.Pr.EQ4Type:
			setEQType(4, value);
			picEQ_Plot();
			break;
		case Channel.Pr.EQ4LPFOn:
			eq4LPF.Value = value;
			picEQ_Plot();
			break;
		case Channel.Pr.EQ1Q:
			eq1Q.Value = value;
			if (activeQ == eq1Q)
			{
				faderQ.Value = eq1Q.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ2Q:
			eq2Q.Value = value;
			if (activeQ == eq2Q)
			{
				faderQ.Value = eq2Q.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ3Q:
			eq3Q.Value = value;
			if (activeQ == eq3Q)
			{
				faderQ.Value = eq3Q.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ4Q:
			eq4Q.Value = value;
			if (activeQ == eq4Q)
			{
				faderQ.Value = eq4Q.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ1F:
			eq1F.Value = value;
			if (activeFreq == eq1F)
			{
				faderFreq.Value = eq1F.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ2F:
			eq2F.Value = value;
			if (activeFreq == eq2F)
			{
				faderFreq.Value = eq2F.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ3F:
			eq3F.Value = value;
			if (activeFreq == eq3F)
			{
				faderFreq.Value = eq3F.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ4F:
			eq4F.Value = value;
			if (activeFreq == eq4F)
			{
				faderFreq.Value = eq4F.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ1HPFOn:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 2)
			{
				eq1G.Value = value;
			}
			if (activeGain == eq1G)
			{
				faderGain.Value = eq1G.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ1G:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) != 2)
			{
				eq1G.Value = value;
			}
			if (activeGain == eq1G)
			{
				faderGain.Value = eq1G.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ2G:
			eq2G.Value = value;
			if (activeGain == eq2G)
			{
				faderGain.Value = eq2G.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ3G:
			eq3G.Value = value;
			if (activeGain == eq3G)
			{
				faderGain.Value = eq3G.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.EQ4G:
			eq4G.Value = value;
			if (activeGain == eq4G)
			{
				faderGain.Value = eq4G.Value;
			}
			picEQ_Plot();
			break;
		case Channel.Pr.RelativeHA:
		case Channel.Pr.RelativeFader:
		case Channel.Pr.Cue:
		case Channel.Pr.Group:
		case Channel.Pr.Pair:
		case Channel.Pr.BusType:
		case Channel.Pr.PanLink:
			break;
		}
	}

	private void setEQType(int band, int type)
	{
		switch (band)
		{
		case 1:
			switch (type)
			{
			case 0:
				btnLowEQType.Text = "Peak";
				eq1Q.Show();
				eq1G.KnobLabel = "Low";
				eq1G.KnobStyle = 0;
				eq1G.Maximum = 180;
				eq1G.Minimum = -180;
				eq1G.Steps = 5;
				eq1G.Tag = "Low";
				eq1G.ValueType = "div10";
				eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1G);
				break;
			case 1:
				btnLowEQType.Text = "Shelf";
				eq1Q.Hide();
				eq1G.KnobLabel = "Low";
				eq1G.KnobStyle = 0;
				eq1G.Maximum = 180;
				eq1G.Minimum = -180;
				eq1G.Steps = 5;
				eq1G.Tag = "Low";
				eq1G.ValueType = "div10";
				eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1G);
				break;
			case 2:
				btnLowEQType.Text = "HPF";
				eq1Q.Hide();
				eq1G.KnobLabel = "HPF";
				eq1G.KnobStyle = 0;
				eq1G.Maximum = 1;
				eq1G.Minimum = 0;
				eq1G.Steps = 1;
				eq1G.Tag = "eq1HPF";
				eq1G.ValueType = "onoff";
				eq1G.Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1HPFOn);
				break;
			}
			break;
		case 4:
			switch (type)
			{
			case 0:
				btnHiEQType.Text = "Peak";
				if (activeGain == eq4LPF)
				{
					activeGain = eq4G;
				}
				eq4Q.Show();
				eq4G.Show();
				eq4LPF.Hide();
				break;
			case 1:
				btnHiEQType.Text = "Shelf";
				if (activeGain == eq4LPF)
				{
					activeGain = eq4G;
				}
				eq4Q.Hide();
				eq4G.Show();
				eq4LPF.Hide();
				break;
			case 2:
				btnHiEQType.Text = "LPF";
				if (activeGain == eq4G)
				{
					activeGain = eq4LPF;
				}
				eq4Q.Hide();
				eq4G.Hide();
				eq4LPF.Show();
				break;
			}
			break;
		}
		if (activeGain != null)
		{
			setEQBand(activeGain.Tag.ToString());
		}
	}

	private void setHeader()
	{
		switch (channelType)
		{
		case ChannelTypes.Input:
		case ChannelTypes.STIN:
			lblHeader.Text = "Ch " + (MixEngine.chData[activeChannel].subChannel + 1) + ((MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) != 0) ? (" [Grp " + MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) + "]") : "") + "\n" + MixEngine.chData[activeChannel].name;
			break;
		case ChannelTypes.Mix:
			lblHeader.Text = "Mix " + (MixEngine.chData[activeChannel].subChannel + 1) + ((MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) != 0) ? (" [Grp " + MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) + "]") : "") + "\n" + MixEngine.chData[activeChannel].name;
			break;
		case ChannelTypes.Matrix:
			lblHeader.Text = "Matrix " + (MixEngine.chData[activeChannel].subChannel + 1) + ((MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) != 0) ? (" [Grp " + MixEngine.chData[activeChannel].getParam(Channel.Pr.Group) + "]") : "") + "\n" + MixEngine.chData[activeChannel].name;
			break;
		case ChannelTypes.Stereo:
			switch (MixEngine.chData[activeChannel].subChannel)
			{
			case 0:
				lblHeader.Text = "ST L\n" + MixEngine.chData[activeChannel].name;
				break;
			case 1:
				lblHeader.Text = "ST R\n" + MixEngine.chData[activeChannel].name;
				break;
			case 2:
				lblHeader.Text = "MONO\n" + MixEngine.chData[activeChannel].name;
				break;
			default:
				lblHeader.Text = "MONITOR";
				break;
			}
			break;
		}
		Text = lblHeader.Text.Replace("\n", " - ");
		lblHeader.Font = FontFit("Arial", FontStyle.Bold, lblHeader.Text, (int)Math.Round((double)lblHeader.Width * 0.9), (int)Math.Round((double)lblHeader.Height * 0.9), 18f);
	}

	private int intRound(decimal value)
	{
		return (int)Math.Round(value);
	}

	public frmChannel(frmSurface16 opener)
	{
		parentSurface = opener;
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Init");
		}
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		InitializeComponent();
		MixEngine.logDebug("Init::WindowSize Is " + Settings.Default.windowChannel);
		thisWin.Parse(Settings.Default.windowChannel);
		if (thisWin.WindowState == "Fullscreen")
		{
			isFullScreen = true;
		}
		isResizing = true;
		thisWin.SetWindow(this);
		isResizing = false;
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Window Properties Set");
		}
		setColors();
		try
		{
			meterData = File.ReadAllBytes("MeterData.dat");
		}
		catch (FileNotFoundException)
		{
			Loc.Msg("Missing critical data! [MeterData]");
			return;
		}
		catch (Exception)
		{
			Loc.Msg("Error loading! [MeterData]");
			return;
		}
		arrFaders = new AirControls.AirFader[8] { fader0, fader1, fader2, fader3, fader4, fader5, fader6, fader7 };
		arrMuteAssign = new AirButton[8] { btnMuteAssign0, btnMuteAssign1, btnMuteAssign2, btnMuteAssign3, btnMuteAssign4, btnMuteAssign5, btnMuteAssign6, btnMuteAssign7 };
		arrDCAAssign = new AirButton[8] { btnDCAAssign0, btnDCAAssign1, btnDCAAssign2, btnDCAAssign3, btnDCAAssign4, btnDCAAssign5, btnDCAAssign6, btnDCAAssign7 };
		arrSendChanLabels = new Label[8] { lblChan00, lblChan01, lblChan02, lblChan03, lblChan04, lblChan05, lblChan06, lblChan07 };
		arrSendOn = new AirButton[8] { btnSendOn00, btnSendOn01, btnSendOn02, btnSendOn03, btnSendOn04, btnSendOn05, btnSendOn06, btnSendOn07 };
		arrSendPrePost = new AirButton[8] { btnSendPre00, btnSendPre01, btnSendPre02, btnSendPre03, btnSendPre04, btnSendPre05, btnSendPre06, btnSendPre07 };
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Subscribe Console Events");
		}
		if (thisWin.Page >= 0 && thisWin.Page < tabSection.TabCount)
		{
			tabSection.SelectedIndex = thisWin.Page;
		}
		else
		{
			tabSection.SelectedIndex = 0;
		}
	}

	private void PEQCopy_Changed()
	{
		try
		{
			btnPaste.BackColor = Settings.Default.c_button_inactive;
			btnPaste.ForeColor = Settings.Default.c_button_inactive_fore;
		}
		catch (Exception ex)
		{
			logError("PEQCopy_Changed()", "UI", ex.ToString());
		}
	}

	private void Console_Changed(string property)
	{
		if (dyn00.InvokeRequired)
		{
			ConsoleCallback method = Console_Changed;
			Invoke(method, property);
			return;
		}
		switch (property)
		{
		case "SelChannel":
			if (consoleLink)
			{
				ActiveChannel = Console.selChannel;
			}
			break;
		case "ClockSpeed":
			updateUI();
			break;
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

	private void logError(string Source, string Category, string Message)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			folderPath += "\\01vmix";
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}
			TextWriter textWriter = new StreamWriter(folderPath + "\\Error.Log", append: true);
			textWriter.WriteLine("============ " + Source + " Error =================");
			textWriter.WriteLine("VERSION:   " + Application.ProductVersion.ToString());
			textWriter.WriteLine("TIMESTAMP: " + DateTime.Now.ToString());
			textWriter.WriteLine(Message);
			textWriter.WriteLine("============ End " + Source + " Error =============");
			textWriter.Close();
		}
		catch (Exception ex)
		{
			frmConfirm.sayFail("Could not record Error Log due to:\n" + ex.ToString(), "Log Error");
		}
		switch (Category)
		{
		case "UI":
			frmConfirm.sayFail("Unfortunately, there was an error updating the User Interface.\nI wrote it down in Error.Log and will try to keep going.", "User Interface Error");
			break;
		case "OutEvent":
			frmConfirm.sayFail("Something bad happened when I tried to tell the console what you changed.\nI wrote it down in Error.Log but you probably better re-start and/or re-sync.", "Outbound Event Error");
			break;
		case "InEvent":
			frmConfirm.sayFail("Something bad happened when the console told me about a parameter change.\nI wrote it down in Error.Log but you probably better re-start and/or re-sync.", "Inbound Event Error");
			break;
		default:
			frmConfirm.sayFail("I've run into a problem with: " + Category + ".\nI wrote it down in Error.Log and will try to keep going.", "General Error");
			break;
		}
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

	private void tabSection_DrawItem(object sender, DrawItemEventArgs e)
	{
		Graphics graphics = e.Graphics;
		TabPage tabPage = tabSection.TabPages[e.Index];
		Rectangle tabRect = tabSection.GetTabRect(e.Index);
		Brush brush;
		if (e.State == DrawItemState.Selected)
		{
			brush = new SolidBrush(Color.Black);
			graphics.FillRectangle(Brushes.Aqua, e.Bounds);
		}
		else
		{
			brush = new SolidBrush(Color.Black);
		}
		Font font = new Font("Arial", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
		StringFormat stringFormat = new StringFormat();
		stringFormat.Alignment = StringAlignment.Center;
		stringFormat.LineAlignment = StringAlignment.Center;
		graphics.DrawString(tabPage.Text, font, brush, tabRect, new StringFormat(stringFormat));
	}

	private void showJumper()
	{
		Form form = new Form();
		form.Width = base.ClientRectangle.Width;
		form.Height = base.ClientRectangle.Height;
		form.BackColor = Settings.Default.c_background;
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		form.TopMost = true;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = PointToScreen(new Point(0, 0));
		form.SuspendLayout();
		int num = (int)Math.Ceiling((double)(Console.maxInputs - Console.maxSTIN) / 8.0) + (int)Math.Ceiling((double)Console.maxSTIN / 8.0) + 1;
		Size size = new Size((int)Math.Round((double)(form.Width - 10) / 8.0) - 10, (int)Math.Round(((double)(form.Height - 10) - (double)(num / 4) * 10.0) / (double)num) - 10);
		Font font = FontFit("Arial", FontStyle.Bold, "STEREO12", size.Width - 4, size.Height - 4, 20f);
		Font subFont = new Font("Arial", font.Size * 0.75f);
		AirButton airButton = new AirButton();
		airButton.BackColor = Settings.Default.c_button_inactive;
		airButton.ForeColor = Settings.Default.c_button_inactive_fore;
		airButton.ParentColor = form.BackColor;
		airButton.Size = size;
		airButton.Font = font;
		airButton.SubFont = subFont;
		airButton.Location = new Point(form.Width - (size.Width + 10), 10);
		airButton.Name = "btnCancel";
		airButton.TabIndex = 1;
		airButton.Tag = -1;
		airButton.Text = "Cancel";
		airButton.Click += btnGoto_Click;
		form.Controls.Add(airButton);
		airButton = new AirButton();
		airButton.BackColor = Settings.Default.c_button_inactive;
		airButton.ForeColor = Settings.Default.c_button_inactive_fore;
		airButton.ParentColor = form.BackColor;
		airButton.Size = size;
		airButton.Font = font;
		airButton.SubFont = subFont;
		airButton.Location = new Point(form.Width - (size.Width + 10) * 2, 10);
		airButton.Name = "btnBus";
		airButton.TabIndex = 1;
		airButton.Tag = -1;
		airButton.Text = "Mix +\nMatrix";
		airButton.Click += btnGoto_Click;
		form.Controls.Add(airButton);
		airButton = new AirButton();
		airButton.BackColor = Settings.Default.c_button_inactive;
		airButton.ForeColor = Settings.Default.c_button_inactive_fore;
		airButton.ParentColor = form.BackColor;
		airButton.Size = size;
		airButton.Font = font;
		airButton.SubFont = subFont;
		airButton.Location = new Point(form.Width - (size.Width + 10) * 3, 10);
		airButton.Name = "btnChannel";
		airButton.TabIndex = 1;
		airButton.Tag = -1;
		airButton.Text = "Inputs";
		airButton.Click += btnGoto_Click;
		form.Controls.Add(airButton);
		Label label = new Label();
		label.ForeColor = Settings.Default.c_foreground;
		label.Font = font;
		label.Location = new Point(10, 10);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 10 - (size.Width + 10) * 3, size.Height);
		label.TabIndex = 0;
		label.Text = "Jump to Channel";
		label.TextAlign = ContentAlignment.MiddleCenter;
		form.Controls.Add(label);
		int num2 = size.Height + 20;
		for (int i = 0; i < Console.maxInputs; i++)
		{
			if (i >= 0)
			{
				if (i % 8 == 0 && i > 0)
				{
					num2 += size.Height + 10;
				}
				if (i % 32 == 0)
				{
					AirPanel airPanel = new AirPanel();
					airPanel.PanelType = 2;
					airPanel.BackColor = Color.DarkGoldenrod;
					airPanel.BorderColor = Color.Black;
					airPanel.Width = form.Width - 20;
					airPanel.Height = 5;
					airPanel.Left = 10;
					airPanel.Top = num2 - 2;
					form.Controls.Add(airPanel);
					num2 += 10;
				}
			}
			airButton = new AirButton();
			airButton.ParentColor = form.BackColor;
			airButton.Size = size;
			airButton.Font = font;
			airButton.SubFont = subFont;
			airButton.Location = new Point(i % 8 * (size.Width + 10) + 10, num2);
			airButton.Name = "btnGoto" + i;
			airButton.TabIndex = i + 100;
			airButton.Click += btnGoto_Click;
			form.Controls.Add(airButton);
		}
		if (activeChannel < Console.maxInputs)
		{
			switchJumper(form, 0);
		}
		else
		{
			switchJumper(form, 2);
		}
		form.ResumeLayout();
		form.ShowDialog();
	}

	public void switchJumper(Form jumpForm, int page)
	{
		int i;
		if (page == 0)
		{
			jumpForm.Controls[2].ForeColor = Settings.Default.c_button_active_fore;
			jumpForm.Controls[2].BackColor = Settings.Default.c_button_active;
			jumpForm.Controls[1].ForeColor = Settings.Default.c_button_inactive_fore;
			jumpForm.Controls[1].BackColor = Settings.Default.c_button_inactive;
			i = MixEngine.chOffsetInput;
		}
		else
		{
			jumpForm.Controls[2].ForeColor = Settings.Default.c_button_inactive_fore;
			jumpForm.Controls[2].BackColor = Settings.Default.c_button_inactive;
			jumpForm.Controls[1].ForeColor = Settings.Default.c_button_active_fore;
			jumpForm.Controls[1].BackColor = Settings.Default.c_button_active;
			i = MixEngine.chOffsetMix;
		}
		for (int j = 0; j < jumpForm.Controls.Count; j++)
		{
			if (jumpForm.Controls[j] is Label)
			{
				if (page == 0)
				{
					jumpForm.Controls[j].Text = "Select Input Channel";
				}
				else
				{
					jumpForm.Controls[j].Text = "Select Mix/Matrix/Stereo Bus";
				}
			}
			else
			{
				if (!(jumpForm.Controls[j] is AirButton) || !jumpForm.Controls[j].Name.StartsWith("btnGoto"))
				{
					continue;
				}
				AirButton airButton = (AirButton)jumpForm.Controls[j];
				if (i < MixEngine.chData.Length)
				{
					if (page == 0)
					{
						for (; MixEngine.chData[i].channelType == ChannelTypes.None && i < MixEngine.chOffsetSTIN + Console.maxSTIN; i++)
						{
						}
						if (MixEngine.chData[i].channelType != 0 && i < MixEngine.chOffsetSTIN + Console.maxSTIN)
						{
							airButton.Text = i + 1 + "\n" + MixEngine.chData[i].getParamStr(Channel.Pr.Name);
							airButton.Tag = i;
							airButton.BackColor = ((i == activeChannel) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
							airButton.ForeColor = ((i == activeChannel) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
							airButton.Show();
						}
						else
						{
							airButton.Hide();
						}
					}
					else if (page == 2)
					{
						if (MixEngine.chData[i].channelType != 0 && MixEngine.chData[i].channelType != ChannelTypes.Monitor)
						{
							airButton.Text = MixEngine.chData[i].hardname + "\n" + MixEngine.chData[i].getParamStr(Channel.Pr.Name);
							airButton.Tag = i;
							airButton.BackColor = ((i == activeChannel) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
							airButton.ForeColor = ((i == activeChannel) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
							airButton.Show();
						}
						else
						{
							airButton.Hide();
						}
						if (i + 1 == MixEngine.chOffsetMatrix)
						{
							for (int k = 0; k < 8; k++)
							{
								jumpForm.Controls[++j].Hide();
							}
						}
					}
					i++;
				}
				else
				{
					airButton.Hide();
				}
			}
		}
	}

	private void btnGoto_Click(object sender, EventArgs e)
	{
		try
		{
			int result = 0;
			if (int.TryParse(((AirButton)sender).Tag.ToString(), out result))
			{
				if (((Control)sender).Name == "btnCancel")
				{
					((Form)((AirButton)sender).Parent).Close();
					return;
				}
				if (((Control)sender).Name == "btnChannel")
				{
					switchJumper((Form)((AirButton)sender).Parent, 0);
					return;
				}
				if (((Control)sender).Name == "btnBus")
				{
					switchJumper((Form)((AirButton)sender).Parent, 2);
					return;
				}
				if (result >= 0 && result < MixEngine.chData.Length)
				{
					ActiveChannel = result;
				}
			}
			((Form)((AirButton)sender).Parent).Close();
		}
		catch (Exception ex)
		{
			logError("btnGoto_Click()", "UI", ex.ToString());
		}
	}

	public void setColors()
	{
		_ = Settings.Default.c_button_active;
		_ = Settings.Default.c_button_inactive;
		_ = Settings.Default.c_button_active_fore;
		_ = Settings.Default.c_button_inactive_fore;
		Color backColor = ((!((double)(BackColor = Settings.Default.c_background).GetBrightness() > 0.2)) ? RGBHSL.ModifyBrightness(BackColor, 1.5) : RGBHSL.ModifyBrightness(BackColor, 0.8));
		BackColor = Settings.Default.c_background;
		tabDyn.BackColor = Settings.Default.c_background;
		tabEQ.BackColor = Settings.Default.c_background;
		tabSends.BackColor = Settings.Default.c_background;
		panelDCA.BackColor = backColor;
		panelDyn1.BackColor = backColor;
		panelMutes.BackColor = backColor;
		panelDyn0.BackColor = backColor;
		panelPAN.BackColor = backColor;
		lblHeader.BackColor = Settings.Default.c_led_back;
		lblHeader.ForeColor = Settings.Default.c_led_fore;
		lblEq4.BackColor = Settings.Default.c_fader_back;
		lblEq4.ForeColor = Settings.Default.c_fader_fore;
		lblEq3.BackColor = Settings.Default.c_fader_back;
		lblEq3.ForeColor = Settings.Default.c_fader_fore;
		lblEq2.BackColor = Settings.Default.c_fader_back;
		lblEq2.ForeColor = Settings.Default.c_fader_fore;
		lblEq1.BackColor = Settings.Default.c_fader_back;
		lblEq1.ForeColor = Settings.Default.c_fader_fore;
		lblDCA.BackColor = panelDCA.BackColor;
		lblDCA.ForeColor = Settings.Default.c_foreground;
		lblMuteGroups.BackColor = panelMutes.BackColor;
		lblMuteGroups.ForeColor = Settings.Default.c_foreground;
		lblChan00.BackColor = Settings.Default.c_fader_fore;
		lblChan00.ForeColor = Settings.Default.c_fader_back;
		lblChan01.BackColor = Settings.Default.c_fader_fore;
		lblChan01.ForeColor = Settings.Default.c_fader_back;
		lblChan02.BackColor = Settings.Default.c_fader_fore;
		lblChan02.ForeColor = Settings.Default.c_fader_back;
		lblChan03.BackColor = Settings.Default.c_fader_fore;
		lblChan03.ForeColor = Settings.Default.c_fader_back;
		lblChan04.BackColor = Settings.Default.c_fader_fore;
		lblChan04.ForeColor = Settings.Default.c_fader_back;
		lblChan05.BackColor = Settings.Default.c_fader_fore;
		lblChan05.ForeColor = Settings.Default.c_fader_back;
		lblChan06.BackColor = Settings.Default.c_fader_fore;
		lblChan06.ForeColor = Settings.Default.c_fader_back;
		lblChan07.BackColor = Settings.Default.c_fader_fore;
		lblChan07.ForeColor = Settings.Default.c_fader_back;
		knobHA.BackColor = backColor;
		dyn15.BackColor = backColor;
		dyn05.BackColor = backColor;
		airFader1.BackColor = Settings.Default.c_fader_back;
		airFader1.ForeColor = Settings.Default.c_fader_fore;
		chanFader.BackColor = Settings.Default.c_fader_back;
		chanFader.ForeColor = Settings.Default.c_fader_fore;
		chanMute.ParentColor = Settings.Default.c_background;
		faderGain.BackColor = Settings.Default.c_fader_back;
		faderGain.ForeColor = Settings.Default.c_fader_fore;
		faderFreq.BackColor = Settings.Default.c_fader_back;
		faderFreq.ForeColor = Settings.Default.c_fader_fore;
		faderQ.BackColor = Settings.Default.c_fader_back;
		faderQ.ForeColor = Settings.Default.c_fader_fore;
		fader0.BackColor = Settings.Default.c_fader_back;
		fader0.ForeColor = Settings.Default.c_fader_fore;
		fader1.BackColor = Settings.Default.c_fader_back;
		fader1.ForeColor = Settings.Default.c_fader_fore;
		fader2.BackColor = Settings.Default.c_fader_back;
		fader2.ForeColor = Settings.Default.c_fader_fore;
		fader3.BackColor = Settings.Default.c_fader_back;
		fader3.ForeColor = Settings.Default.c_fader_fore;
		fader4.BackColor = Settings.Default.c_fader_back;
		fader4.ForeColor = Settings.Default.c_fader_fore;
		fader5.BackColor = Settings.Default.c_fader_back;
		fader5.ForeColor = Settings.Default.c_fader_fore;
		fader6.BackColor = Settings.Default.c_fader_back;
		fader6.ForeColor = Settings.Default.c_fader_fore;
		fader7.BackColor = Settings.Default.c_fader_back;
		fader7.ForeColor = Settings.Default.c_fader_fore;
		btnFine.BackColor = Settings.Default.c_button_inactive;
		btnFine.ForeColor = Settings.Default.c_button_inactive_fore;
		btnFine.ParentColor = btnFine.Parent.BackColor;
		btnFlat.BackColor = Settings.Default.c_button_inactive;
		btnFlat.ForeColor = Settings.Default.c_button_inactive_fore;
		btnFlat.ParentColor = btnFlat.Parent.BackColor;
		btnCopy.BackColor = Settings.Default.c_button_inactive;
		btnCopy.ForeColor = Settings.Default.c_button_inactive_fore;
		btnCopy.ParentColor = btnCopy.Parent.BackColor;
		if (MixEngine.PEQCopy.isSet)
		{
			btnPaste.BackColor = Settings.Default.c_button_inactive;
			btnPaste.ForeColor = Settings.Default.c_button_inactive_fore;
		}
		else
		{
			btnPaste.BackColor = Settings.Default.c_button_inactive;
			btnPaste.ForeColor = Settings.Default.c_button_inactive_fore;
		}
		btnOptions.BackColor = Settings.Default.c_button_inactive;
		btnOptions.ForeColor = Settings.Default.c_button_inactive_fore;
		btnOptions.ParentColor = btnOptions.Parent.BackColor;
		btnJump.BackColor = Settings.Default.c_button_inactive;
		btnJump.ForeColor = Settings.Default.c_button_inactive_fore;
		btnJump.ParentColor = btnJump.Parent.BackColor;
		btnClose.BackColor = Settings.Default.c_button_inactive;
		btnClose.ForeColor = Settings.Default.c_button_inactive_fore;
		btnClose.ParentColor = btnClose.Parent.BackColor;
		btnNext.BackColor = Settings.Default.c_button_inactive;
		btnNext.ForeColor = Settings.Default.c_button_inactive_fore;
		btnNext.ParentColor = btnNext.Parent.BackColor;
		btnPrev.BackColor = Settings.Default.c_button_inactive;
		btnPrev.ForeColor = Settings.Default.c_button_inactive_fore;
		btnPrev.ParentColor = btnPrev.Parent.BackColor;
		chanPhantom.BackColor = Settings.Default.c_button_inactive;
		chanPhantom.ForeColor = Settings.Default.c_button_inactive_fore;
		chanPhantom.ParentColor = chanPhantom.Parent.BackColor;
		chanPhase.BackColor = Settings.Default.c_button_inactive;
		chanPhase.ForeColor = Settings.Default.c_button_inactive_fore;
		chanPhase.ParentColor = chanPhase.Parent.BackColor;
		btnDirectOut.BackColor = Settings.Default.c_button_inactive;
		btnDirectOut.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDirectOut.ParentColor = btnDirectOut.Parent.BackColor;
		btnInsertOn.BackColor = Settings.Default.c_button_inactive;
		btnInsertOn.ForeColor = Settings.Default.c_button_inactive_fore;
		btnInsertOn.ParentColor = btnInsertOn.Parent.BackColor;
		btnSafe.BackColor = Settings.Default.c_button_inactive;
		btnSafe.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSafe.ParentColor = btnSafe.Parent.BackColor;
		btnDyn0On.BackColor = Settings.Default.c_button_inactive;
		btnDyn0On.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDyn0On.ParentColor = btnDyn0On.Parent.BackColor;
		btnDyn1On.BackColor = Settings.Default.c_button_inactive;
		btnDyn1On.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDyn1On.ParentColor = btnDyn1On.Parent.BackColor;
		btnDyn0Type.BackColor = Settings.Default.c_button_inactive;
		btnDyn0Type.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDyn0Type.ParentColor = btnDyn0Type.Parent.BackColor;
		btnDyn1Type.BackColor = Settings.Default.c_button_inactive;
		btnDyn1Type.ForeColor = Settings.Default.c_button_inactive_fore;
		btnDyn1Type.ParentColor = btnDyn1Type.Parent.BackColor;
		btnMuteAssign0.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign0.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign0.ParentColor = btnMuteAssign0.Parent.BackColor;
		btnMuteAssign1.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign1.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign1.ParentColor = btnMuteAssign1.Parent.BackColor;
		btnMuteAssign2.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign2.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign2.ParentColor = btnMuteAssign2.Parent.BackColor;
		btnMuteAssign3.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign3.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign3.ParentColor = btnMuteAssign3.Parent.BackColor;
		btnMuteAssign4.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign4.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign4.ParentColor = btnMuteAssign4.Parent.BackColor;
		btnMuteAssign5.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign5.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign5.ParentColor = btnMuteAssign5.Parent.BackColor;
		btnMuteAssign6.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign6.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign6.ParentColor = btnMuteAssign6.Parent.BackColor;
		btnMuteAssign7.BackColor = Settings.Default.c_button_inactive;
		btnMuteAssign7.ForeColor = Settings.Default.c_button_inactive_fore;
		btnMuteAssign7.ParentColor = btnMuteAssign7.Parent.BackColor;
		btnToMonoOn.BackColor = Settings.Default.c_button_inactive;
		btnToMonoOn.ForeColor = Settings.Default.c_button_inactive_fore;
		btnToMonoOn.ParentColor = btnToMonoOn.Parent.BackColor;
		btnToStereoOn.BackColor = Settings.Default.c_button_inactive;
		btnToStereoOn.ForeColor = Settings.Default.c_button_inactive_fore;
		btnToStereoOn.ParentColor = btnToStereoOn.Parent.BackColor;
		chanPanMode.BackColor = Settings.Default.c_button_inactive;
		chanPanMode.ForeColor = Settings.Default.c_button_inactive_fore;
		chanPanMode.ParentColor = chanPanMode.Parent.BackColor;
		btnHPFOn.BackColor = Settings.Default.c_button_inactive;
		btnHPFOn.ForeColor = Settings.Default.c_button_inactive_fore;
		btnHPFOn.ParentColor = btnHPFOn.Parent.BackColor;
		btnHiEQType.BackColor = Settings.Default.c_button_inactive;
		btnHiEQType.ForeColor = Settings.Default.c_button_inactive_fore;
		btnHiEQType.ParentColor = btnHiEQType.Parent.BackColor;
		btnLowEQType.BackColor = Settings.Default.c_button_inactive;
		btnLowEQType.ForeColor = Settings.Default.c_button_inactive_fore;
		btnLowEQType.ParentColor = btnLowEQType.Parent.BackColor;
		btnSendOn00.BackColor = Settings.Default.c_button_inactive;
		btnSendOn00.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn00.ParentColor = btnSendOn00.Parent.BackColor;
		btnSendOn01.BackColor = Settings.Default.c_button_inactive;
		btnSendOn01.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn01.ParentColor = btnSendOn01.Parent.BackColor;
		btnSendOn02.BackColor = Settings.Default.c_button_inactive;
		btnSendOn02.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn02.ParentColor = btnSendOn02.Parent.BackColor;
		btnSendOn03.BackColor = Settings.Default.c_button_inactive;
		btnSendOn03.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn03.ParentColor = btnSendOn03.Parent.BackColor;
		btnSendOn04.BackColor = Settings.Default.c_button_inactive;
		btnSendOn04.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn04.ParentColor = btnSendOn04.Parent.BackColor;
		btnSendOn05.BackColor = Settings.Default.c_button_inactive;
		btnSendOn05.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn05.ParentColor = btnSendOn05.Parent.BackColor;
		btnSendOn06.BackColor = Settings.Default.c_button_inactive;
		btnSendOn06.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn06.ParentColor = btnSendOn06.Parent.BackColor;
		btnSendOn07.BackColor = Settings.Default.c_button_inactive;
		btnSendOn07.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendOn07.ParentColor = btnSendOn07.Parent.BackColor;
		btnSendPre00.BackColor = Settings.Default.c_button_inactive;
		btnSendPre00.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre00.ParentColor = btnSendPre00.Parent.BackColor;
		btnSendPre01.BackColor = Settings.Default.c_button_inactive;
		btnSendPre01.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre01.ParentColor = btnSendPre01.Parent.BackColor;
		btnSendPre02.BackColor = Settings.Default.c_button_inactive;
		btnSendPre02.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre02.ParentColor = btnSendPre02.Parent.BackColor;
		btnSendPre03.BackColor = Settings.Default.c_button_inactive;
		btnSendPre03.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre03.ParentColor = btnSendPre03.Parent.BackColor;
		btnSendPre04.BackColor = Settings.Default.c_button_inactive;
		btnSendPre04.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre04.ParentColor = btnSendPre04.Parent.BackColor;
		btnSendPre05.BackColor = Settings.Default.c_button_inactive;
		btnSendPre05.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre05.ParentColor = btnSendPre05.Parent.BackColor;
		btnSendPre06.BackColor = Settings.Default.c_button_inactive;
		btnSendPre06.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre06.ParentColor = btnSendPre06.Parent.BackColor;
		btnSendPre07.BackColor = Settings.Default.c_button_inactive;
		btnSendPre07.ForeColor = Settings.Default.c_button_inactive_fore;
		btnSendPre07.ParentColor = btnSendPre07.Parent.BackColor;
	}

	private void btnClose_Click(object sender, EventArgs e)
	{
		Hide();
	}

	private void btnFine_Click(object sender, EventArgs e)
	{
		if (!fineMode)
		{
			for (int i = 0; i < arrFaders.Length; i++)
			{
				arrFaders[i].FineMode = true;
			}
			faderFreq.fineMode = true;
			faderQ.fineMode = true;
			faderGain.fineMode = true;
			chanFader.fineMode = true;
			airFader1.fineMode = true;
			dyn00.FineMode = true;
			dyn01.FineMode = true;
			dyn02.FineMode = true;
			dyn03.FineMode = true;
			dyn04.FineMode = true;
			dyn05.FineMode = true;
			dyn10.FineMode = true;
			dyn11.FineMode = true;
			dyn12.FineMode = true;
			dyn13.FineMode = true;
			dyn14.FineMode = true;
			dyn15.FineMode = true;
			knobHA.FineMode = true;
			chanPan.FineMode = true;
			eq1G.FineMode = true;
			eq2G.FineMode = true;
			eq3G.FineMode = true;
			eq4G.FineMode = true;
			eqAtt.FineMode = true;
			eqHPFFreq.FineMode = true;
			fineMode = true;
			btnFine.BackColor = Settings.Default.c_button_active;
			btnFine.ForeColor = Settings.Default.c_button_active_fore;
		}
		else
		{
			for (int j = 0; j < arrFaders.Length; j++)
			{
				arrFaders[j].FineMode = false;
			}
			faderFreq.fineMode = false;
			faderGain.fineMode = false;
			faderQ.fineMode = false;
			chanFader.fineMode = false;
			airFader1.fineMode = false;
			dyn00.FineMode = false;
			dyn01.FineMode = false;
			dyn02.FineMode = false;
			dyn03.FineMode = false;
			dyn04.FineMode = false;
			dyn05.FineMode = false;
			dyn10.FineMode = false;
			dyn11.FineMode = false;
			dyn12.FineMode = false;
			dyn13.FineMode = false;
			dyn14.FineMode = false;
			dyn15.FineMode = false;
			knobHA.FineMode = false;
			chanPan.FineMode = false;
			eq1G.FineMode = false;
			eq2G.FineMode = false;
			eq3G.FineMode = false;
			eq4G.FineMode = false;
			eqAtt.FineMode = false;
			eqHPFFreq.FineMode = false;
			fineMode = false;
			btnFine.BackColor = Settings.Default.c_button_inactive;
			btnFine.ForeColor = Settings.Default.c_button_inactive_fore;
		}
	}

	private void btnSendOn_Click(object sender, EventArgs e)
	{
		int num = (int)((AirButton)sender).Tag;
		if (num < 16)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMixOn, num, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.SendToMixOn, num));
		}
		else
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMatrixOn, num - 16, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.SendToMatrixOn, num - 16));
		}
	}

	private void btnSendPre_Click(object sender, EventArgs e)
	{
		int num = (int)((AirButton)sender).Tag;
		if (num < 16)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMixPre, num, (MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMixPre, num) != 1) ? 1 : 0);
		}
		else
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMatrixPre, num - 16, (MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMatrixPre, num - 16) != 1) ? 1 : 0);
		}
	}

	private void fader_FaderChanged(int channel, int value, string flag)
	{
		if (channel < 16)
		{
			if (MixEngine.chData[channel + MixEngine.chOffsetMix].getParam(Channel.Pr.Pair) > 0 && MixEngine.chData[channel + MixEngine.chOffsetMix].getParam(Channel.Pr.BusType) != 2 && channel % 2 == 0)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMixPan, channel, value);
			}
			else
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMix, channel, value);
			}
		}
		else if (MixEngine.chData[channel - 16 + MixEngine.chOffsetMatrix].getParam(Channel.Pr.Pair) > 0 && (channel - 16) % 2 == 0)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMatrixPan, channel - 16, value);
		}
		else
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.SendToMatrix, channel - 16, value);
		}
	}

	private void setSOFPage(int page)
	{
		if (activeChannel < 0 || activeChannel > MixEngine.chData.Length)
		{
			return;
		}
		if (channelType != ChannelTypes.Input && channelType != ChannelTypes.STIN)
		{
			page = 2;
		}
		else if (page > 1 && Console.type != ConsoleTypes.M7CL)
		{
			page = 0;
		}
		for (int i = 0; i < 8; i++)
		{
			int num = i + page * 8;
			arrFaders[i].Channel = num;
			arrSendOn[i].Tag = num;
			arrSendPrePost[i].Tag = num;
			if (arrFaders[i].Channel < 16)
			{
				if (MixEngine.chData[num + MixEngine.chOffsetMix].getParam(Channel.Pr.Pair) > 0 && MixEngine.chData[num + MixEngine.chOffsetMix].getParam(Channel.Pr.BusType) != 2 && num % 2 == 0)
				{
					arrFaders[i].Minimum = -63;
					arrFaders[i].Maximum = 63;
					arrFaders[i].FaderColor = "White";
					arrFaders[i].KnobColor = "Red";
					arrFaders[i].ValueType = "pan";
					arrFaders[i].ShowKnob = true;
					arrFaders[i].FaderTails = false;
					arrFaders[i].Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMixPan, num);
					arrSendChanLabels[i].Text = "PAN >";
					arrSendOn[i].Hide();
					arrSendPrePost[i].Hide();
				}
				else
				{
					arrFaders[i].Minimum = 0;
					arrFaders[i].Maximum = 1023;
					arrFaders[i].FaderColor = "Orange";
					arrFaders[i].ValueType = "fader";
					arrFaders[i].ShowKnob = false;
					arrFaders[i].FaderTails = true;
					arrFaders[i].Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMix, num);
					arrSendChanLabels[i].Text = MixEngine.chData[num + MixEngine.chOffsetMix].name;
					setMuteColor(arrSendOn[i], MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMixOn, num) == 1, groupMute: false);
					arrSendPrePost[i].BackColor = ((MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMixPre, num) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrSendPrePost[i].ForeColor = ((MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMixPre, num) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrSendOn[i].Show();
					arrSendPrePost[i].Show();
				}
				if (MixEngine.chData[num + MixEngine.chOffsetMix].getParam(Channel.Pr.BusType) == 2)
				{
					arrSendPrePost[i].Hide();
				}
			}
			else
			{
				if (MixEngine.chData[i + MixEngine.chOffsetMatrix].getParam(Channel.Pr.Pair) > 0 && MixEngine.chData[i + MixEngine.chOffsetMatrix].getParam(Channel.Pr.BusType) != 2 && i % 2 == 0)
				{
					arrFaders[i].Minimum = -63;
					arrFaders[i].Maximum = 63;
					arrFaders[i].FaderColor = "White";
					arrFaders[i].KnobColor = "Red";
					arrFaders[i].ValueType = "pan";
					arrFaders[i].ShowKnob = true;
					arrFaders[i].FaderTails = false;
					arrFaders[i].Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMatrixPan, i);
					arrSendChanLabels[i].Text = "PAN >";
					arrSendOn[i].Hide();
					arrSendPrePost[i].Hide();
				}
				else
				{
					arrFaders[i].Minimum = 0;
					arrFaders[i].Maximum = 1023;
					arrFaders[i].FaderColor = "Orange";
					arrFaders[i].ValueType = "fader";
					arrFaders[i].ShowKnob = false;
					arrFaders[i].FaderTails = true;
					arrFaders[i].Value = MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMatrix, i);
					arrSendChanLabels[i].Text = MixEngine.chData[i + MixEngine.chOffsetMatrix].name;
					setMuteColor(arrSendOn[i], MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMatrixOn, i) == 1, groupMute: false);
					arrSendPrePost[i].BackColor = ((MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMatrixPre, i) == 1) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
					arrSendPrePost[i].ForeColor = ((MixEngine.chData[activeChannel].getParam(Channel.Pr.SendToMatrixPre, i) == 1) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
					arrSendOn[i].Show();
					arrSendPrePost[i].Show();
				}
				if (MixEngine.chData[i + MixEngine.chOffsetMatrix].getParam(Channel.Pr.BusType) == 2)
				{
					arrSendPrePost[i].Hide();
				}
			}
		}
		switch (page)
		{
		case 0:
			btnPage0.BackColor = Settings.Default.c_button_active;
			btnPage0.ForeColor = Settings.Default.c_button_active_fore;
			btnPage1.BackColor = Settings.Default.c_button_inactive;
			btnPage1.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPage2.BackColor = Settings.Default.c_button_inactive;
			btnPage2.ForeColor = Settings.Default.c_button_inactive_fore;
			break;
		case 1:
			btnPage0.BackColor = Settings.Default.c_button_inactive;
			btnPage0.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPage1.BackColor = Settings.Default.c_button_active;
			btnPage1.ForeColor = Settings.Default.c_button_active_fore;
			btnPage2.BackColor = Settings.Default.c_button_inactive;
			btnPage2.ForeColor = Settings.Default.c_button_inactive_fore;
			break;
		case 2:
			btnPage0.BackColor = Settings.Default.c_button_inactive;
			btnPage0.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPage1.BackColor = Settings.Default.c_button_inactive;
			btnPage1.ForeColor = Settings.Default.c_button_inactive_fore;
			btnPage2.BackColor = Settings.Default.c_button_active;
			btnPage2.ForeColor = Settings.Default.c_button_active_fore;
			break;
		}
		SOFPage = page;
	}

	private void btnPage0_Click(object sender, EventArgs e)
	{
		setSOFPage(0);
	}

	private void btnPage1_Click(object sender, EventArgs e)
	{
		setSOFPage(1);
	}

	private void btnPage2_Click(object sender, EventArgs e)
	{
		setSOFPage(2);
	}

	private decimal calcGainAt(decimal Gain, decimal Q, decimal Freq, int SampleFreq)
	{
		int num = intRound(Q);
		int num2 = intRound(Freq);
		if (num < 0)
		{
			num = 0;
		}
		switch (num)
		{
		case 41:
		{
			if (Gain == 0m)
			{
				return 0.0m;
			}
			int num15 = (int)Math.Abs(Gain) / 6;
			if (num15 > 3)
			{
				num15 = 3;
			}
			int num16 = ((num15 >= 3) ? 3 : (num15 + 1));
			decimal num17 = 0.0m;
			decimal num18 = 0.0m;
			if (SampleFreq - num2 + 68 <= 0)
			{
				return Gain;
			}
			if (SampleFreq - num2 + 68 >= 124)
			{
				return 0.0m;
			}
			if (num15 > 0)
			{
				num17 = (decimal)meterData[(num15 - 1) * 124 + (SampleFreq - num2 + 68) + 15251] / 10.0m;
			}
			if (num16 > 0)
			{
				num18 = (decimal)meterData[(num16 - 1) * 124 + (SampleFreq - num2 + 68) + 15251] / 10.0m;
			}
			return (decimal)Math.Sign(Gain) * ((num18 - num17) * (Math.Abs(Gain) - (decimal)num15 * 6.0m) / 6.0m + num17);
		}
		case 42:
		{
			if (Gain == 0m)
			{
				return 0.0m;
			}
			int num7 = (int)Math.Abs(Gain) / 6;
			if (num7 > 3)
			{
				num7 = 3;
			}
			int num8 = ((num7 >= 3) ? 3 : (num7 + 1));
			decimal num9 = 0.0m;
			decimal num10 = 0.0m;
			if (num2 - SampleFreq + 68 <= 0)
			{
				return Gain;
			}
			if (num2 - SampleFreq + 68 >= 124)
			{
				return 0.0m;
			}
			if (num7 > 0)
			{
				num9 = (decimal)meterData[(num7 - 1) * 124 + (num2 - SampleFreq + 68) + 15251] / 10.0m;
			}
			if (num8 > 0)
			{
				num10 = (decimal)meterData[(num8 - 1) * 124 + (num2 - SampleFreq + 68) + 15251] / 10.0m;
			}
			return (decimal)Math.Sign(Gain) * ((num10 - num9) * (Math.Abs(Gain) - (decimal)num7 * 6.0m) / 6.0m + num9);
		}
		case 43:
		{
			if (num2 - SampleFreq + 68 <= 0)
			{
				return -60.0m;
			}
			if (num2 - SampleFreq + 68 >= 124)
			{
				return 0.0m;
			}
			decimal num11 = meterData[(num2 - SampleFreq + 68) * 2 + 15624];
			decimal num12 = meterData[(num2 - SampleFreq + 68) * 2 + 15624 + 1];
			return -(num11 * 256m + num12) / 10.0m;
		}
		case 44:
		{
			if (SampleFreq - num2 + 68 <= 0)
			{
				return -60.0m;
			}
			if (SampleFreq - num2 + 68 >= 124)
			{
				return 0.0m;
			}
			decimal num13 = meterData[(SampleFreq - num2 + 68) * 2 + 15624];
			decimal num14 = meterData[(SampleFreq - num2 + 68) * 2 + 15624 + 1];
			return -(num13 * 256m + num14) / 10.0m;
		}
		default:
		{
			if (Gain == 0m)
			{
				return 0.0m;
			}
			if (Math.Abs(Gain) > 18m)
			{
				Gain = Math.Sign(Gain) * 18;
			}
			if (Gain % 6m == 0m)
			{
				return (decimal)Math.Sign(Gain) * (decimal)meterData[((int)Math.Abs(Gain) / 6 - 1) * 5084 + num * 124 + Math.Abs(num2 - SampleFreq)] / 10.0m;
			}
			int num3 = (int)Math.Abs(Gain) / 6;
			if (num3 > 3)
			{
				num3 = 3;
			}
			int num4 = ((num3 >= 3) ? 3 : (num3 + 1));
			decimal num5 = 0.0m;
			decimal num6 = 0.0m;
			if (num3 > 0)
			{
				num5 = (decimal)meterData[(num3 - 1) * 5084 + num * 124 + Math.Abs(num2 - SampleFreq)] / 10.0m;
			}
			if (num4 > 0)
			{
				num6 = (decimal)meterData[(num4 - 1) * 5084 + num * 124 + Math.Abs(num2 - SampleFreq)] / 10.0m;
			}
			return (decimal)Math.Sign(Gain) * ((num6 - num5) * (Math.Abs(Gain) - (decimal)num3 * 6.0m) / 6.0m + num5);
		}
		}
	}

	private void picEQ_Plot()
	{
		decimal num = 0m;
		if (activeChannel < 0 || activeChannel > MixEngine.chData.Length)
		{
			return;
		}
		decimal[] array = new decimal[5];
		Point[] array2 = new Point[123];
		for (int i = 4; i <= 124; i++)
		{
			num = 0m;
			if (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQHPFOn))
			{
				ref decimal reference = ref array[4];
				reference = calcGainAt(0m, 44m, eqHPFFreq.Value, i);
			}
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 0)
			{
				if (eq1Q.Value < 41)
				{
					ref decimal reference2 = ref array[0];
					reference2 = calcGainAt((decimal)eq1G.Value / 10.0m, eq1Q.Value, eq1F.Value, i);
				}
			}
			else if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 1)
			{
				ref decimal reference3 = ref array[0];
				reference3 = calcGainAt((decimal)eq1G.Value / 10.0m, 41m, eq1F.Value, i);
			}
			else if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 2)
			{
				if (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQ1HPFOn))
				{
					ref decimal reference4 = ref array[0];
					reference4 = calcGainAt((decimal)eq1G.Value / 10.0m, 44m, eq1F.Value, i);
				}
				else
				{
					array[0] = 0m;
				}
			}
			ref decimal reference5 = ref array[1];
			reference5 = calcGainAt((decimal)eq2G.Value / 10.0m, eq2Q.Value, eq2F.Value, i);
			ref decimal reference6 = ref array[2];
			reference6 = calcGainAt((decimal)eq3G.Value / 10.0m, eq3Q.Value, eq3F.Value, i);
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type) == 0)
			{
				ref decimal reference7 = ref array[3];
				reference7 = calcGainAt((decimal)eq4G.Value / 10.0m, eq4Q.Value, eq4F.Value, i);
			}
			else if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type) == 1)
			{
				ref decimal reference8 = ref array[3];
				reference8 = calcGainAt((decimal)eq4G.Value / 10.0m, 42m, eq4F.Value, i);
			}
			else if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type) == 2)
			{
				if (MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQ4LPFOn))
				{
					ref decimal reference9 = ref array[3];
					reference9 = calcGainAt(0m, 43m, eq4F.Value, i);
				}
				else
				{
					array[3] = 0m;
				}
			}
			num = array[0] + array[1] + array[2] + array[3] + array[4];
			int num2 = picEQ.Height - (int)Math.Round((num + 20m) / 40m * (decimal)picEQ.Height);
			array2[i - 4].X = (int)Math.Round((decimal)(picEQ.Width - 1) / 120m * (decimal)(i - 4));
			array2[i - 4].Y = num2;
			if (Math.Abs(eq1F.Value - i) == 0)
			{
				eqPoints[0].Y = picEQ.Height - (int)Math.Round((array[0] + 20m) / 40m * (decimal)picEQ.Height);
				eqPoints[0].X = array2[i - 4].X;
			}
			if (Math.Abs(eq2F.Value - i) == 0)
			{
				eqPoints[1].Y = picEQ.Height - (int)Math.Round((array[1] + 20m) / 40m * (decimal)picEQ.Height);
				eqPoints[1].X = array2[i - 4].X;
			}
			if (Math.Abs(eq3F.Value - i) == 0)
			{
				eqPoints[2].Y = picEQ.Height - (int)Math.Round((array[2] + 20m) / 40m * (decimal)picEQ.Height);
				eqPoints[2].X = array2[i - 4].X;
			}
			if (Math.Abs(eq4F.Value - i) == 0)
			{
				eqPoints[3].Y = picEQ.Height - (int)Math.Round((array[3] + 20m) / 40m * (decimal)picEQ.Height);
				eqPoints[3].X = array2[i - 4].X;
			}
		}
		array2[array2.Length - 1].X = 0;
		array2[array2.Length - 1].Y = picEQ.Height / 2;
		array2[array2.Length - 2].X = picEQ.Width;
		array2[array2.Length - 2].Y = picEQ.Height / 2;
		picEQ.SetPoints(array2, eqPoints, nearEQ, activeEQ);
	}

	private void frmChannel_Deactivate(object sender, EventArgs e)
	{
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Deactivate Window");
		}
		if (isFullScreen)
		{
			thisWin.WindowState = "Fullscreen";
		}
		else
		{
			thisWin.WindowState = base.WindowState.ToString();
		}
		if (tabSection.SelectedIndex >= 0)
		{
			thisWin.Page = tabSection.SelectedIndex;
		}
		else
		{
			thisWin.Page = 0;
		}
		Settings.Default.windowChannel = WindowStatic.Store(thisWin);
	}

	private void airKnob_Click(object sender, EventArgs e)
	{
		if ((AirKnob)sender != activeKnob || activeKnob == null)
		{
			if (activeKnob != null)
			{
				activeKnob.Active = false;
			}
			activeKnob = (AirKnob)sender;
			airFader1.ValueType = activeKnob.ValueType;
			airFader1.TickType = activeKnob.ValueType;
			airFader1.Maximum = activeKnob.Maximum;
			airFader1.Minimum = activeKnob.Minimum;
			airFader1.Steps = activeKnob.Steps;
			airFader1.Value = activeKnob.Value;
			airFader1.Invalidate();
			lblParam.Text = activeKnob.KnobLabel;
			activeKnob.Active = true;
		}
	}

	private void airFader1_FaderChanged(int channel, int value, string flag)
	{
		if (activeKnob == null)
		{
			activeKnob = dyn00;
			activeKnob.Active = true;
		}
		activeKnob.Value = value;
		activeKnob.Invalidate();
		any_KnobChanged(activeKnob, activeKnob.Channel, activeKnob.Value, activeKnob.Flag);
	}

	private void any_KnobChanged(AirKnob sender, int channel, int value, string flag)
	{
		if (sender == activeKnob)
		{
			airFader1.Value = value;
			airFader1.Invalidate();
		}
		if (sender == activeQ)
		{
			faderQ.Value = value;
		}
		else if (sender == activeFreq)
		{
			faderFreq.Value = value;
		}
		else if (sender == activeGain)
		{
			faderGain.Value = value;
		}
		if (sender == knobHA)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.HA, 0, value);
			return;
		}
		if (sender == chanPan)
		{
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.Pair) > 0)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Balance, 0, value);
			}
			else
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Pan, 0, value);
			}
			return;
		}
		if (sender == eqAtt)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQAtt, 0, value);
			return;
		}
		if (sender == eqHPFFreq)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQHPFFreq, 0, value);
			return;
		}
		if (sender == eq1Q)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Q, 0, value);
			return;
		}
		if (sender == eq1F)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1F, 0, value);
			return;
		}
		if (sender == eq1G)
		{
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 2)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1HPFOn, 0, value);
			}
			else
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1G, 0, value);
			}
			return;
		}
		if (sender == eq2Q)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ2Q, 0, value);
			return;
		}
		if (sender == eq2F)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ2F, 0, value);
			return;
		}
		if (sender == eq2G)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ2G, 0, value);
			return;
		}
		if (sender == eq3Q)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ3Q, 0, value);
			return;
		}
		if (sender == eq3F)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ3F, 0, value);
			return;
		}
		if (sender == eq3G)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ3G, 0, value);
			return;
		}
		if (sender == eq4Q)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4Q, 0, value);
			return;
		}
		if (sender == eq4F)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4F, 0, value);
			return;
		}
		if (sender == eq4G)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4G, 0, value);
			return;
		}
		if (sender == eq4LPF)
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4LPFOn, 0, value);
			return;
		}
		switch (MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn0Type))
		{
		case 0:
		case 1:
			if (sender == dyn00)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Threshold, 0, value);
			}
			if (sender == dyn01)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Range, 0, value);
			}
			if (sender == dyn02)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Attack, 0, value);
			}
			if (sender == dyn03)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Hold, 0, value);
			}
			if (sender == dyn04)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Decay, 0, value);
			}
			break;
		case 2:
		case 3:
			if (sender == dyn00)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Threshold, 0, value);
			}
			if (sender == dyn01)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Ratio, 0, value);
			}
			if (sender == dyn02)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Attack, 0, value);
			}
			if (sender == dyn03)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Decay, 0, value);
			}
			if (sender == dyn04)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Gain, 0, value);
			}
			if (sender == dyn05)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Knee, 0, value);
			}
			break;
		}
		switch (MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn1Type))
		{
		case 2:
		case 3:
		case 4:
		case 5:
			if (sender == dyn10)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Threshold, 0, value);
			}
			if (sender == dyn11)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Ratio, 0, value);
			}
			if (sender == dyn12)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Attack, 0, value);
			}
			if (sender == dyn13)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Release, 0, value);
			}
			if (sender == dyn14)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Gain, 0, value);
			}
			if (sender == dyn15)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Knee, 0, value);
			}
			break;
		case 6:
			if (sender == dyn10)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Threshold, 0, value);
			}
			if (sender == dyn11)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Release, 0, value);
			}
			break;
		}
	}

	private void eq_Click(object sender, EventArgs e)
	{
		setEQBand(((AirKnob)sender).Tag.ToString());
	}

	private void lblEq_Click(object sender, EventArgs e)
	{
		setEQBand(((Label)sender).Tag.ToString());
	}

	private void setEQBand(string band)
	{
		if (activeQ != null)
		{
			activeQ.Active = false;
		}
		if (activeFreq != null)
		{
			activeFreq.Active = false;
		}
		if (activeGain != null)
		{
			activeGain.Active = false;
		}
		faderGain.FineMode = fineMode;
		switch (band)
		{
		case "HPF":
			activeQ = eq1Q;
			activeFreq = eq1F;
			activeGain = eq1G;
			faderFreq.Show();
			faderGain.Show();
			lblQ.Text = "Q";
			lblFreq.Text = "F";
			lblGain.Text = "G";
			lblQ.Hide();
			faderQ.Hide();
			lblFreq.Show();
			lblGain.Show();
			lblEq1.BackColor = Color.Yellow;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.WhiteSmoke;
			activeEQ = 0;
			break;
		case "Low":
		case "eq1HPF":
			activeQ = eq1Q;
			activeFreq = eq1F;
			activeGain = eq1G;
			faderFreq.Show();
			faderGain.Show();
			lblQ.Text = "Q";
			lblFreq.Text = "F";
			lblGain.Text = "G";
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type) == 0)
			{
				lblQ.Show();
				faderQ.Show();
			}
			else
			{
				lblQ.Hide();
				faderQ.Hide();
			}
			lblFreq.Show();
			lblGain.Show();
			lblEq1.BackColor = Color.Yellow;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.WhiteSmoke;
			activeEQ = 0;
			activeQ.Maximum = MixEngine.chData[activeChannel].paramMax[Channel.Pr.EQ1Q];
			break;
		case "LowMid":
			activeQ = eq2Q;
			activeFreq = eq2F;
			activeGain = eq2G;
			faderQ.Show();
			faderFreq.Show();
			faderGain.Show();
			lblQ.Text = "Q";
			lblFreq.Text = "F";
			lblGain.Text = "G";
			lblQ.Show();
			lblFreq.Show();
			lblGain.Show();
			lblEq1.BackColor = Color.WhiteSmoke;
			lblEq2.BackColor = Color.Yellow;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.WhiteSmoke;
			activeEQ = 1;
			break;
		case "HiMid":
			activeQ = eq3Q;
			activeFreq = eq3F;
			activeGain = eq3G;
			faderQ.Show();
			faderFreq.Show();
			faderGain.Show();
			lblQ.Text = "Q";
			lblFreq.Text = "F";
			lblGain.Text = "G";
			lblQ.Show();
			lblFreq.Show();
			lblGain.Show();
			lblEq1.BackColor = Color.WhiteSmoke;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.Yellow;
			lblEq4.BackColor = Color.WhiteSmoke;
			activeEQ = 2;
			break;
		case "High":
			activeQ = eq4Q;
			activeFreq = eq4F;
			activeGain = eq4G;
			faderFreq.Show();
			faderGain.Show();
			lblQ.Text = "Q";
			lblFreq.Text = "F";
			lblGain.Text = "G";
			lblFreq.Show();
			lblGain.Show();
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type) == 0)
			{
				lblQ.Show();
				faderQ.Show();
			}
			else
			{
				lblQ.Hide();
				faderQ.Hide();
			}
			lblEq1.BackColor = Color.WhiteSmoke;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.Yellow;
			activeEQ = 3;
			break;
		case "LPF":
			faderGain.FineMode = false;
			activeQ = eq4Q;
			activeFreq = eq4F;
			activeGain = eq4LPF;
			faderQ.Hide();
			faderFreq.Show();
			faderGain.Show();
			lblQ.Text = "Q";
			lblFreq.Text = "F";
			lblGain.Text = "G";
			lblQ.Hide();
			lblFreq.Show();
			lblGain.Show();
			lblEq1.BackColor = Color.WhiteSmoke;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.Yellow;
			activeEQ = 3;
			break;
		case "HPFFreq":
			activeQ = null;
			activeFreq = eqHPFFreq;
			activeGain = null;
			faderQ.Hide();
			faderFreq.Show();
			faderGain.Hide();
			lblQ.Text = "";
			lblFreq.Text = "F";
			lblGain.Text = "";
			lblQ.Hide();
			lblFreq.Show();
			lblGain.Hide();
			activeEQ = 4;
			lblEq1.BackColor = Color.WhiteSmoke;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.WhiteSmoke;
			break;
		case "EQAtt":
			activeQ = null;
			activeFreq = null;
			activeGain = eqAtt;
			faderQ.Hide();
			faderFreq.Hide();
			faderGain.Show();
			lblQ.Text = "";
			lblFreq.Text = "";
			lblGain.Text = "EQ Att";
			lblQ.Hide();
			lblFreq.Hide();
			lblGain.Show();
			activeEQ = 5;
			lblEq1.BackColor = Color.WhiteSmoke;
			lblEq2.BackColor = Color.WhiteSmoke;
			lblEq3.BackColor = Color.WhiteSmoke;
			lblEq4.BackColor = Color.WhiteSmoke;
			break;
		}
		if (activeQ != null)
		{
			faderQ.Value = activeQ.Value;
			faderQ.Minimum = activeQ.Minimum;
			faderQ.Maximum = activeQ.Maximum;
			activeQ.Active = true;
		}
		if (activeFreq != null)
		{
			faderFreq.Value = activeFreq.Value;
			faderFreq.Minimum = activeFreq.Minimum;
			faderFreq.Maximum = activeFreq.Maximum;
			activeFreq.Active = true;
		}
		if (activeGain != null)
		{
			faderGain.ValueType = activeGain.ValueType;
			faderGain.TickType = activeGain.ValueType;
			faderGain.Minimum = activeGain.Minimum;
			faderGain.Maximum = activeGain.Maximum;
			faderGain.Value = activeGain.Value;
			faderGain.Steps = activeGain.Steps;
			activeGain.Active = true;
		}
		picEQ_Plot();
	}

	private void faderQ_FaderChanged(int channel, int value, string flag)
	{
		if (activeQ == null)
		{
			activeQ = eq1Q;
		}
		activeQ.Value = value;
		any_KnobChanged(activeQ, activeQ.Channel, activeQ.Value, activeQ.Flag);
	}

	private void faderFreq_FaderChanged(int channel, int value, string flag)
	{
		if (activeFreq == null)
		{
			activeFreq = eq1F;
		}
		activeFreq.Value = value;
		any_KnobChanged(activeFreq, activeFreq.Channel, activeFreq.Value, activeFreq.Flag);
	}

	private void faderGain_FaderChanged(int channel, int value, string flag)
	{
		if (activeGain == null)
		{
			activeGain = eq1G;
		}
		activeGain.Value = value;
		any_KnobChanged(activeGain, activeGain.Channel, activeGain.Value, activeGain.Flag);
	}

	private void frmChannel_Load(object sender, EventArgs e)
	{
		Loc.Apply(this);
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Load");
		}
		doLayout();
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Subscribe Channel Events");
		}
		Console.Changed += Console_Changed;
		MixEngine.PEQCopy.Changed += PEQCopy_Changed;
		MixEngine.ChannelChanged += MixEngine_ChannelChanged;
		MixEngine.EngineChanged += MixEngine_EngineChanged;
		heartBeat = new System.Threading.Timer(tickUI, "tick", 10000, 10000);
		activeKnob = dyn00;
		activeQ = eq1Q;
		activeGain = eq1G;
		activeFreq = eq1F;
		activeKnob.Active = true;
		activeGain.Active = true;
	}

	private void tickUI(object data)
	{
	}

	private void frmChannel_FormClosing(object sender, FormClosingEventArgs e)
	{
		if (parentSurface.isGlobalShutdown)
		{
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: FormClosing");
			}
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: Unsubscribe Console Events");
			}
			Console.Changed -= Console_Changed;
			MixEngine.PEQCopy.Changed -= PEQCopy_Changed;
			MixEngine.ChannelChanged -= MixEngine_ChannelChanged;
			MixEngine.EngineChanged -= MixEngine_EngineChanged;
		}
		else
		{
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: FormClosing: Hiding Form Only");
			}
			e.Cancel = true;
			Hide();
		}
	}

	private void chanFader_FaderChanged(int channel, int value, string flag)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Fader, 0, value);
	}

	private void chanPhantom_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Phantom, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Phantom));
	}

	private void chanPhase_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Phase, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Phase));
	}

	private void btnPrev_Click(object sender, EventArgs e)
	{
		int num = activeChannel;
		while (num > 0 && num < MixEngine.chData.Length)
		{
			num--;
			if (MixEngine.chData[num].channelType != 0)
			{
				ActiveChannel = num;
				break;
			}
		}
	}

	private void btnNext_Click(object sender, EventArgs e)
	{
		int num = activeChannel;
		while (num >= 0 && num < MixEngine.chData.Length - 1)
		{
			num++;
			if (MixEngine.chData[num].channelType != 0)
			{
				ActiveChannel = num;
				break;
			}
		}
	}

	private void btnDCAAssign_Click(object sender, EventArgs e)
	{
		AirButton airButton = (AirButton)sender;
		int num = int.Parse(airButton.Tag.ToString());
		if (MixEngine.DCA[num].members.Contains(activeChannel))
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.DCAGroup, num, 0);
		}
		else
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.DCAGroup, num, 1);
		}
	}

	private void btnMuteAssign_Click(object sender, EventArgs e)
	{
		AirButton airButton = (AirButton)sender;
		int num = int.Parse(airButton.Tag.ToString());
		if (MixEngine.muteGroup[num].members.Contains(activeChannel))
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.MuteGroup, num, 0);
		}
		else
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.MuteGroup, num, 1);
		}
	}

	private void btnHiEQType_Click(object sender, EventArgs e)
	{
		Form form = new Form();
		form.Width = 328;
		form.Height = 100;
		form.BackColor = Color.Gold;
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		form.TopMost = true;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = btnHiEQType.PointToScreen(new Point(-10 - form.Width, 0));
		Label label = new Label();
		label.ForeColor = Color.Black;
		label.Font = new Font("Arial", 12f, FontStyle.Bold);
		label.Location = new Point(20, 9);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 40, 24);
		label.TabIndex = 0;
		label.Text = "High EQ Type";
		label.TextAlign = ContentAlignment.TopCenter;
		form.Controls.Add(label);
		Font font = new Font("Arial", 16f, FontStyle.Bold);
		string[] array = new string[3] { "Peak", "Shelf", "LPF" };
		for (int i = 0; i < 3; i++)
		{
			AirButton airButton = new AirButton();
			airButton.BackColor = ((i == MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type)) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			airButton.ForeColor = ((i == MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type)) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			airButton.ParentColor = form.BackColor;
			airButton.Location = new Point(i * 106 + 10, 36);
			airButton.Name = "btnPicker" + i;
			airButton.Size = new Size(100, 60);
			airButton.Font = font;
			airButton.TabIndex = 128;
			airButton.Tag = array[i] + "1";
			airButton.Text = array[i];
			airButton.Click += btnPicker_Click;
			form.Controls.Add(airButton);
		}
		form.ShowDialog();
	}

	private void btnLowEQType_Click(object sender, EventArgs e)
	{
		Form form = new Form();
		string[] array;
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			array = new string[3] { "Peak", "Shelf", "HPF" };
			form.Width = 328;
		}
		else if (channelType == ChannelTypes.Input || channelType == ChannelTypes.STIN)
		{
			array = new string[2] { "Peak", "Shelf" };
			form.Width = 222;
		}
		else
		{
			array = new string[3] { "Peak", "Shelf", "HPF" };
			form.Width = 328;
		}
		form.Height = 100;
		form.BackColor = Color.Gold;
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		form.TopMost = true;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = btnLowEQType.PointToScreen(new Point(-10 - form.Width, btnHiEQType.Height - form.Height));
		Label label = new Label();
		label.ForeColor = Color.Black;
		label.Font = new Font("Arial", 12f, FontStyle.Bold);
		label.Location = new Point(20, 9);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 40, 24);
		label.TabIndex = 0;
		label.Text = "Low EQ Type";
		label.TextAlign = ContentAlignment.TopCenter;
		form.Controls.Add(label);
		Font font = new Font("Arial", 16f, FontStyle.Bold);
		for (int i = 0; i < array.Length; i++)
		{
			AirButton airButton = new AirButton();
			airButton.BackColor = ((i == MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type)) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			airButton.ForeColor = ((i == MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type)) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			airButton.ParentColor = form.BackColor;
			airButton.Location = new Point(i * 106 + 10, 36);
			airButton.Name = "btnPicker" + i;
			airButton.Size = new Size(100, 60);
			airButton.Font = font;
			airButton.TabIndex = 128;
			airButton.Tag = array[i] + "0";
			airButton.Text = array[i];
			airButton.Click += btnPicker_Click;
			form.Controls.Add(airButton);
		}
		form.ShowDialog();
	}

	private void btnDyn0Type_Click(object sender, EventArgs e)
	{
		Form form = new Form();
		form.Width = 434;
		form.Height = 100;
		form.BackColor = Color.Gold;
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		form.TopMost = true;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = panelDyn0.PointToScreen(new Point(btnDyn0Type.Right + 10, btnDyn0Type.Top));
		form.Name = "panelHiEQType";
		Label label = new Label();
		label.ForeColor = Color.Black;
		label.Font = new Font("Arial", 12f, FontStyle.Bold);
		label.Location = new Point(20, 9);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 40, 24);
		label.TabIndex = 0;
		label.Text = "Choose Dynamics #1 Type";
		label.TextAlign = ContentAlignment.TopCenter;
		form.Controls.Add(label);
		Font font = new Font("Arial", 12f, FontStyle.Bold);
		string[] array = ((Console.type != ConsoleTypes.O1V96V2) ? new string[4] { "Gate", "Ducking", "Compress", "Expander" } : new string[2] { "Gate", "Ducking" });
		for (int i = 0; i < array.Length; i++)
		{
			AirButton airButton = new AirButton();
			airButton.BackColor = ((i == MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn0Type)) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			airButton.ForeColor = ((i == MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn0Type)) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			airButton.ParentColor = form.BackColor;
			airButton.Location = new Point(i * 106 + 10, 36);
			airButton.Name = "btnPicker" + i;
			airButton.Size = new Size(100, 60);
			airButton.Font = font;
			airButton.TabIndex = 128;
			airButton.Tag = array[i] + "0";
			airButton.Text = array[i];
			airButton.Click += btnPicker_Click;
			form.Controls.Add(airButton);
		}
		form.ShowDialog();
	}

	private void btnDyn1Type_Click(object sender, EventArgs e)
	{
		Form form = new Form();
		form.Width = 434;
		form.Height = 100;
		form.BackColor = Color.Gold;
		form.FormBorderStyle = FormBorderStyle.None;
		form.TopMost = true;
		form.ShowInTaskbar = false;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = panelDyn1.PointToScreen(new Point(btnDyn1Type.Right + 10, btnDyn1Type.Top));
		form.Name = "panelHiEQType";
		Label label = new Label();
		label.ForeColor = Color.Black;
		label.Font = new Font("Arial", 12f, FontStyle.Bold);
		label.Location = new Point(20, 9);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 40, 24);
		label.TabIndex = 0;
		label.Text = "Choose Dynamics #2 Type";
		label.TextAlign = ContentAlignment.TopCenter;
		form.Controls.Add(label);
		string[] array;
		int[] array2;
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			array = new string[4] { "Compress", "Expander", "Compand H", "Compand S" };
			array2 = new int[4] { 2, 3, 4, 5 };
		}
		else if (channelType == ChannelTypes.Input || channelType == ChannelTypes.STIN)
		{
			array = new string[4] { "Compress", "Compand H", "Compand S", "De-Esser" };
			array2 = new int[4] { 2, 4, 5, 6 };
		}
		else
		{
			array = new string[4] { "Compress", "Expander", "Compand H", "Compand S" };
			array2 = new int[4] { 2, 3, 4, 5 };
		}
		Font font = new Font("Arial", 12f, FontStyle.Bold);
		for (int i = 0; i < 4; i++)
		{
			AirButton airButton = new AirButton();
			airButton.BackColor = ((array2[i] == MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn1Type)) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
			airButton.ForeColor = ((array2[i] == MixEngine.chData[activeChannel].getParam(Channel.Pr.Dyn1Type)) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
			airButton.ParentColor = form.BackColor;
			airButton.Location = new Point(i * 106 + 10, 36);
			airButton.Name = "btnPicker" + i;
			airButton.Size = new Size(100, 60);
			airButton.Font = font;
			airButton.TabIndex = 128;
			airButton.Tag = array[i] + "1";
			airButton.Text = array[i];
			airButton.Click += btnPicker_Click;
			form.Controls.Add(airButton);
		}
		form.ShowDialog();
	}

	private void btnPicker_Click(object sender, EventArgs e)
	{
		switch (((AirButton)sender).Tag.ToString())
		{
		case "Gate0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Type, 0, 0);
			break;
		case "Ducking0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Type, 0, 1);
			break;
		case "Compress0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Type, 0, 2);
			break;
		case "Expander0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0Type, 0, 3);
			break;
		case "Compress1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Type, 0, 2);
			break;
		case "Expander1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Type, 0, 3);
			break;
		case "Compand H1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Type, 0, 4);
			break;
		case "Compand S1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Type, 0, 5);
			break;
		case "De-Esser1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1Type, 0, 6);
			break;
		case "Peak0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Type, 0, 0);
			setEQBand("Low");
			break;
		case "Shelf0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Type, 0, 1);
			setEQBand("Low");
			break;
		case "HPF0":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Type, 0, 2);
			setEQBand("HPF");
			break;
		case "Peak1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4Type, 0, 0);
			setEQBand("High");
			break;
		case "Shelf1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4Type, 0, 1);
			setEQBand("High");
			break;
		case "LPF1":
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4Type, 0, 2);
			setEQBand("LPF");
			break;
		}
		((Form)((AirButton)sender).Parent).Close();
	}

	private void setDyn0Type(int channel, int newType, bool reset)
	{
		if (dyn00.InvokeRequired)
		{
			setDynCallback method = setDyn0Type;
			Invoke(method, channel, newType, reset);
			return;
		}
		meterDyn0In.TickMark = (decimal)MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Threshold) / 10.0m;
		switch (newType)
		{
		case 0:
			dyn00.Tag = Channel.Pr.Dyn0Threshold;
			if (Console.type == ConsoleTypes.M7CL)
			{
				dyn00.Minimum = -720;
			}
			else
			{
				dyn00.Minimum = -540;
			}
			dyn00.Maximum = 0;
			dyn00.KnobLabel = "Thresh";
			dyn00.ValueType = "div10";
			dyn00.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Threshold);
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			dyn00.Show();
			dyn01.Tag = Channel.Pr.Dyn0Range;
			dyn01.Minimum = -70;
			dyn01.Maximum = 0;
			dyn01.KnobLabel = "Range";
			dyn01.ValueType = "range";
			dyn01.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Range);
			if (activeKnob == dyn01)
			{
				airKnob_Click(dyn01, EventArgs.Empty);
			}
			dyn01.Show();
			dyn02.Tag = Channel.Pr.Dyn0Attack;
			dyn02.Minimum = 0;
			dyn02.Maximum = 120;
			dyn02.KnobLabel = "Attack";
			dyn02.ValueType = "ms";
			dyn02.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Attack);
			if (activeKnob == dyn02)
			{
				airKnob_Click(dyn02, EventArgs.Empty);
			}
			dyn02.Show();
			dyn03.Tag = Channel.Pr.Dyn0Hold;
			dyn03.Minimum = 0;
			dyn03.Maximum = 215;
			dyn03.KnobLabel = "Hold";
			if (Console.clockSpeed == 44)
			{
				dyn03.ValueType = "holdtime44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn03.ValueType = "holdtime48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn03.ValueType = "holdtime88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn03.ValueType = "holdtime96";
			}
			dyn03.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Hold);
			if (activeKnob == dyn03)
			{
				airKnob_Click(dyn03, EventArgs.Empty);
			}
			dyn03.Show();
			dyn04.Tag = Channel.Pr.Dyn0Decay;
			dyn04.Minimum = 0;
			dyn04.Maximum = 159;
			dyn04.KnobLabel = "Decay";
			if (Console.clockSpeed == 44)
			{
				dyn04.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn04.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn04.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn04.ValueType = "decay96";
			}
			dyn04.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Decay);
			if (activeKnob == dyn04)
			{
				airKnob_Click(dyn04, EventArgs.Empty);
			}
			dyn04.Show();
			if (activeKnob == dyn05)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			dyn05.Hide();
			meterDyn0Gain.Minimum = 0;
			meterDyn0Gain.Maximum = 76;
			btnDyn0Type.Text = "Gate";
			break;
		case 1:
			dyn00.Tag = Channel.Pr.Dyn0Threshold;
			dyn00.Minimum = -540;
			dyn00.Maximum = 0;
			dyn00.KnobLabel = "Thresh";
			dyn00.ValueType = "div10";
			dyn00.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Threshold);
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			dyn00.Show();
			dyn01.Tag = Channel.Pr.Dyn0Range;
			dyn01.Minimum = -70;
			dyn01.Maximum = 0;
			dyn01.KnobLabel = "Range";
			dyn01.ValueType = "range";
			dyn01.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Range);
			if (activeKnob == dyn01)
			{
				airKnob_Click(dyn01, EventArgs.Empty);
			}
			dyn01.Show();
			dyn02.Tag = Channel.Pr.Dyn0Attack;
			dyn02.Minimum = 0;
			dyn02.Maximum = 120;
			dyn02.KnobLabel = "Attack";
			dyn02.ValueType = "ms";
			dyn02.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Attack);
			if (activeKnob == dyn02)
			{
				airKnob_Click(dyn02, EventArgs.Empty);
			}
			dyn02.Show();
			dyn03.Tag = Channel.Pr.Dyn0Hold;
			dyn03.Minimum = 0;
			dyn03.Maximum = 215;
			dyn03.KnobLabel = "Hold";
			if (Console.clockSpeed == 44)
			{
				dyn03.ValueType = "holdtime44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn03.ValueType = "holdtime48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn03.ValueType = "holdtime88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn03.ValueType = "holdtime96";
			}
			dyn03.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Hold);
			if (activeKnob == dyn03)
			{
				airKnob_Click(dyn03, EventArgs.Empty);
			}
			dyn03.Show();
			dyn04.Tag = Channel.Pr.Dyn0Decay;
			dyn04.Minimum = 0;
			dyn04.Maximum = 159;
			if (Console.clockSpeed == 44)
			{
				dyn04.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn04.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn04.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn04.ValueType = "decay96";
			}
			dyn04.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Decay);
			if (activeKnob == dyn04)
			{
				airKnob_Click(dyn04, EventArgs.Empty);
			}
			dyn04.Show();
			if (activeKnob == dyn05)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			dyn05.Hide();
			btnDyn0Type.Text = "Duck";
			break;
		case 2:
			dyn00.Tag = Channel.Pr.Dyn0Threshold;
			dyn00.Minimum = -540;
			dyn00.Maximum = 0;
			dyn00.KnobLabel = "Thresh";
			dyn00.ValueType = "div10";
			dyn00.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Threshold);
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			dyn00.Show();
			dyn01.Tag = Channel.Pr.Dyn0Ratio;
			dyn01.Minimum = 0;
			dyn01.Maximum = 15;
			dyn01.KnobLabel = "Ratio";
			dyn01.ValueType = "ratio";
			dyn01.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Ratio);
			if (activeKnob == dyn01)
			{
				airKnob_Click(dyn01, EventArgs.Empty);
			}
			dyn01.Show();
			dyn02.Tag = Channel.Pr.Dyn0Attack;
			dyn02.Minimum = 0;
			dyn02.Maximum = 120;
			dyn02.KnobLabel = "Attack";
			dyn02.ValueType = "ms";
			dyn02.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Attack);
			if (activeKnob == dyn02)
			{
				airKnob_Click(dyn02, EventArgs.Empty);
			}
			dyn02.Show();
			dyn03.Tag = Channel.Pr.Dyn0Decay;
			dyn03.Minimum = 0;
			dyn03.Maximum = 159;
			dyn03.KnobLabel = "Release";
			if (Console.clockSpeed == 44)
			{
				dyn03.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn03.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn03.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn03.ValueType = "decay96";
			}
			dyn03.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Decay);
			if (activeKnob == dyn03)
			{
				airKnob_Click(dyn03, EventArgs.Empty);
			}
			dyn03.Show();
			dyn04.Tag = Channel.Pr.Dyn0Gain;
			dyn04.Minimum = 0;
			dyn04.Maximum = 180;
			dyn04.KnobLabel = "Outgain";
			dyn04.ValueType = "expandergain";
			dyn04.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Gain);
			if (activeKnob == dyn04)
			{
				airKnob_Click(dyn04, EventArgs.Empty);
			}
			dyn04.Show();
			dyn05.Tag = Channel.Pr.Dyn0Knee;
			dyn05.Minimum = 0;
			dyn05.Maximum = 5;
			dyn05.KnobLabel = "Knee";
			dyn05.ValueType = "knee";
			dyn05.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Knee);
			if (activeKnob == dyn05)
			{
				airKnob_Click(dyn05, EventArgs.Empty);
			}
			dyn05.Show();
			btnDyn0Type.Text = "Comp";
			break;
		case 3:
			dyn00.Tag = Channel.Pr.Dyn0Threshold;
			dyn00.Minimum = -540;
			dyn00.Maximum = 0;
			dyn00.KnobLabel = "Thresh";
			dyn00.ValueType = "div10";
			dyn00.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Threshold);
			if (activeKnob == dyn00)
			{
				airKnob_Click(dyn00, EventArgs.Empty);
			}
			dyn00.Show();
			dyn01.Tag = Channel.Pr.Dyn0Ratio;
			dyn01.Minimum = 0;
			dyn01.Maximum = 15;
			dyn01.KnobLabel = "Ratio";
			dyn01.ValueType = "ratio";
			dyn01.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Ratio);
			if (activeKnob == dyn01)
			{
				airKnob_Click(dyn01, EventArgs.Empty);
			}
			dyn01.Show();
			dyn02.Tag = Channel.Pr.Dyn0Attack;
			dyn02.Minimum = 0;
			dyn02.Maximum = 120;
			dyn02.KnobLabel = "Attack";
			dyn02.ValueType = "ms";
			dyn02.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Attack);
			if (activeKnob == dyn02)
			{
				airKnob_Click(dyn02, EventArgs.Empty);
			}
			dyn02.Show();
			dyn03.Tag = Channel.Pr.Dyn0Decay;
			dyn03.Minimum = 0;
			dyn03.Maximum = 159;
			dyn03.KnobLabel = "Release";
			if (Console.clockSpeed == 44)
			{
				dyn03.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn03.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn03.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn03.ValueType = "decay96";
			}
			dyn03.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Decay);
			if (activeKnob == dyn03)
			{
				airKnob_Click(dyn03, EventArgs.Empty);
			}
			dyn03.Show();
			dyn04.Tag = Channel.Pr.Dyn0Gain;
			dyn04.Minimum = 0;
			dyn04.Maximum = 180;
			dyn04.KnobLabel = "Outgain";
			dyn04.ValueType = "expandergain";
			dyn04.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Gain);
			if (activeKnob == dyn04)
			{
				airKnob_Click(dyn04, EventArgs.Empty);
			}
			dyn04.Show();
			dyn05.Tag = Channel.Pr.Dyn0Knee;
			dyn05.Minimum = 0;
			dyn05.Maximum = 5;
			dyn05.KnobLabel = "Knee";
			dyn05.ValueType = "knee";
			dyn05.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn0Knee);
			if (activeKnob == dyn05)
			{
				airKnob_Click(dyn05, EventArgs.Empty);
			}
			dyn05.Show();
			btnDyn0Type.Text = "Expand";
			break;
		}
	}

	private void setDyn1Type(int channel, int newType, bool reset)
	{
		if (dyn10.InvokeRequired)
		{
			setDynCallback method = setDyn1Type;
			Invoke(method, channel, newType, reset);
			return;
		}
		meterDyn1In.TickMark = (decimal)MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Threshold) / 10.0m;
		switch (newType)
		{
		case 2:
			dyn10.Tag = Channel.Pr.Dyn1Threshold;
			dyn10.Minimum = -540;
			dyn10.Maximum = 0;
			dyn10.KnobLabel = "Thresh";
			dyn10.ValueType = "div10";
			dyn10.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Threshold);
			if (activeKnob == dyn10)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn10.Show();
			dyn11.Tag = Channel.Pr.Dyn1Ratio;
			dyn11.Minimum = 0;
			dyn11.Maximum = 15;
			dyn11.KnobLabel = "Ratio";
			dyn11.ValueType = "ratio";
			dyn11.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Ratio);
			if (activeKnob == dyn11)
			{
				airKnob_Click(dyn11, EventArgs.Empty);
			}
			dyn11.Show();
			dyn12.Tag = Channel.Pr.Dyn1Attack;
			dyn12.Minimum = 0;
			dyn12.Maximum = 120;
			dyn12.KnobLabel = "Attack";
			dyn12.ValueType = "ms";
			dyn12.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Attack);
			if (activeKnob == dyn12)
			{
				airKnob_Click(dyn12, EventArgs.Empty);
			}
			dyn12.Show();
			dyn13.Tag = Channel.Pr.Dyn1Release;
			dyn13.Minimum = 0;
			dyn13.Maximum = 159;
			dyn13.KnobLabel = "Release";
			if (Console.clockSpeed == 44)
			{
				dyn13.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn13.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn13.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn13.ValueType = "decay96";
			}
			dyn13.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Release);
			if (activeKnob == dyn13)
			{
				airKnob_Click(dyn13, EventArgs.Empty);
			}
			dyn13.Show();
			dyn14.Tag = Channel.Pr.Dyn1Gain;
			dyn14.Minimum = 0;
			dyn14.Maximum = 180;
			dyn14.KnobLabel = "Outgain";
			dyn14.ValueType = "expandergain";
			dyn14.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Gain);
			if (activeKnob == dyn14)
			{
				airKnob_Click(dyn14, EventArgs.Empty);
			}
			dyn14.Show();
			dyn15.Tag = Channel.Pr.Dyn1Knee;
			dyn15.Minimum = 0;
			dyn15.Maximum = 5;
			dyn15.KnobLabel = "Knee";
			dyn15.ValueType = "knee";
			dyn15.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Knee);
			if (activeKnob == dyn15)
			{
				airKnob_Click(dyn15, EventArgs.Empty);
			}
			dyn15.Show();
			meterDyn1Gain.Minimum = 0;
			meterDyn1Gain.Maximum = 52;
			btnDyn1Type.Text = "Comp";
			break;
		case 3:
			dyn10.Tag = Channel.Pr.Dyn1Threshold;
			dyn10.Minimum = -540;
			dyn10.Maximum = 0;
			dyn10.KnobLabel = "Thresh";
			dyn10.ValueType = "div10";
			dyn10.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Threshold);
			if (activeKnob == dyn10)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn10.Show();
			dyn11.Tag = Channel.Pr.Dyn1Ratio;
			dyn11.Minimum = 0;
			dyn11.Maximum = 15;
			dyn11.KnobLabel = "Ratio";
			dyn11.ValueType = "ratio";
			dyn11.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Ratio);
			if (activeKnob == dyn11)
			{
				airKnob_Click(dyn11, EventArgs.Empty);
			}
			dyn11.Show();
			dyn12.Tag = Channel.Pr.Dyn1Attack;
			dyn12.Minimum = 0;
			dyn12.Maximum = 120;
			dyn12.KnobLabel = "Attack";
			dyn12.ValueType = "ms";
			dyn12.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Attack);
			if (activeKnob == dyn12)
			{
				airKnob_Click(dyn12, EventArgs.Empty);
			}
			dyn12.Show();
			dyn13.Tag = Channel.Pr.Dyn1Release;
			dyn13.Minimum = 0;
			dyn13.Maximum = 159;
			dyn13.KnobLabel = "Release";
			if (Console.clockSpeed == 44)
			{
				dyn13.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn13.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn13.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn13.ValueType = "decay96";
			}
			dyn13.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Release);
			if (activeKnob == dyn13)
			{
				airKnob_Click(dyn13, EventArgs.Empty);
			}
			dyn13.Show();
			dyn14.Tag = Channel.Pr.Dyn1Gain;
			dyn14.Minimum = 0;
			dyn14.Maximum = 180;
			dyn14.KnobLabel = "Outgain";
			dyn14.ValueType = "expandergain";
			dyn14.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Gain);
			if (activeKnob == dyn14)
			{
				airKnob_Click(dyn14, EventArgs.Empty);
			}
			dyn14.Show();
			dyn15.Tag = Channel.Pr.Dyn1Knee;
			dyn15.Minimum = 0;
			dyn15.Maximum = 5;
			dyn15.KnobLabel = "Knee";
			dyn15.ValueType = "knee";
			dyn15.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Knee);
			if (activeKnob == dyn15)
			{
				airKnob_Click(dyn15, EventArgs.Empty);
			}
			dyn15.Show();
			btnDyn1Type.Text = "Expand";
			break;
		case 4:
			dyn10.Tag = Channel.Pr.Dyn1Threshold;
			dyn10.Minimum = -540;
			dyn10.Maximum = 0;
			dyn10.KnobLabel = "Thresh";
			dyn10.ValueType = "div10";
			dyn10.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Threshold);
			if (activeKnob == dyn10)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn10.Show();
			dyn11.Tag = Channel.Pr.Dyn1Ratio;
			dyn11.Minimum = 0;
			dyn11.Maximum = 15;
			dyn11.KnobLabel = "Ratio";
			dyn11.ValueType = "ratio";
			dyn11.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Ratio);
			if (activeKnob == dyn11)
			{
				airKnob_Click(dyn11, EventArgs.Empty);
			}
			dyn11.Show();
			dyn12.Tag = Channel.Pr.Dyn1Attack;
			dyn12.Minimum = 0;
			dyn12.Maximum = 120;
			dyn12.KnobLabel = "Attack";
			dyn12.ValueType = "ms";
			dyn12.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Attack);
			if (activeKnob == dyn12)
			{
				airKnob_Click(dyn12, EventArgs.Empty);
			}
			dyn12.Show();
			dyn13.Tag = Channel.Pr.Dyn1Release;
			dyn13.Minimum = 0;
			dyn13.Maximum = 159;
			dyn13.KnobLabel = "Release";
			if (Console.clockSpeed == 44)
			{
				dyn13.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn13.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn13.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn13.ValueType = "decay96";
			}
			dyn13.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Release);
			if (activeKnob == dyn13)
			{
				airKnob_Click(dyn13, EventArgs.Empty);
			}
			dyn13.Show();
			dyn14.Tag = Channel.Pr.Dyn1Gain;
			dyn14.Minimum = 0;
			dyn14.Maximum = 180;
			dyn14.KnobLabel = "Outgain";
			dyn14.ValueType = "compandergain";
			dyn14.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Gain);
			if (activeKnob == dyn14)
			{
				airKnob_Click(dyn14, EventArgs.Empty);
			}
			dyn14.Show();
			dyn15.Tag = Channel.Pr.Dyn1Knee;
			dyn15.Minimum = 0;
			dyn15.Maximum = 89;
			dyn15.KnobLabel = "Width";
			dyn15.ValueType = "kneewidth";
			dyn15.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Knee);
			if (activeKnob == dyn15)
			{
				airKnob_Click(dyn15, EventArgs.Empty);
			}
			dyn15.Show();
			btnDyn1Type.Text = "CmpdrH";
			break;
		case 5:
			dyn10.Tag = Channel.Pr.Dyn1Threshold;
			dyn10.Minimum = -540;
			dyn10.Maximum = 0;
			dyn10.KnobLabel = "Thresh";
			dyn10.ValueType = "div10";
			dyn10.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Threshold);
			if (activeKnob == dyn10)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn10.Show();
			dyn11.Tag = Channel.Pr.Dyn1Ratio;
			dyn11.Minimum = 0;
			dyn11.Maximum = 15;
			dyn11.KnobLabel = "Ratio";
			dyn11.ValueType = "ratio";
			dyn11.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Ratio);
			if (activeKnob == dyn11)
			{
				airKnob_Click(dyn11, EventArgs.Empty);
			}
			dyn11.Show();
			dyn12.Tag = Channel.Pr.Dyn1Attack;
			dyn12.Minimum = 0;
			dyn12.Maximum = 120;
			dyn12.KnobLabel = "Attack";
			dyn12.ValueType = "ms";
			dyn12.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Attack);
			if (activeKnob == dyn12)
			{
				airKnob_Click(dyn12, EventArgs.Empty);
			}
			dyn12.Show();
			dyn13.Tag = Channel.Pr.Dyn1Release;
			dyn13.Minimum = 0;
			dyn13.Maximum = 159;
			dyn13.KnobLabel = "Release";
			if (Console.clockSpeed == 44)
			{
				dyn13.ValueType = "decay44";
			}
			else if (Console.clockSpeed == 48)
			{
				dyn13.ValueType = "decay48";
			}
			else if (Console.clockSpeed == 88)
			{
				dyn13.ValueType = "decay88";
			}
			else if (Console.clockSpeed == 96)
			{
				dyn13.ValueType = "decay96";
			}
			dyn13.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Release);
			if (activeKnob == dyn13)
			{
				airKnob_Click(dyn13, EventArgs.Empty);
			}
			dyn13.Show();
			dyn14.Tag = Channel.Pr.Dyn1Gain;
			dyn14.Minimum = 0;
			dyn14.Maximum = 180;
			dyn14.KnobLabel = "Outgain";
			dyn14.ValueType = "compandergain";
			dyn14.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Gain);
			if (activeKnob == dyn14)
			{
				airKnob_Click(dyn14, EventArgs.Empty);
			}
			dyn14.Show();
			dyn15.Tag = Channel.Pr.Dyn1Knee;
			dyn15.Minimum = 0;
			dyn15.Maximum = 89;
			dyn15.KnobLabel = "Width";
			dyn15.ValueType = "kneewidth";
			dyn15.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Knee);
			if (activeKnob == dyn15)
			{
				airKnob_Click(dyn15, EventArgs.Empty);
			}
			dyn15.Show();
			btnDyn1Type.Text = "CmpdrS";
			break;
		case 6:
			dyn10.Tag = Channel.Pr.Dyn1Threshold;
			dyn10.Minimum = -540;
			dyn10.Maximum = 0;
			dyn10.KnobLabel = "Thresh";
			dyn10.ValueType = "div10";
			dyn10.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Threshold);
			if (activeKnob == dyn10)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn10.Show();
			dyn11.Tag = Channel.Pr.Dyn1Release;
			dyn11.Minimum = 72;
			dyn11.Maximum = 116;
			dyn11.KnobLabel = "Freq";
			dyn11.ValueType = "freq";
			dyn11.Value = MixEngine.chData[channel].getParam(Channel.Pr.Dyn1Release);
			if (activeKnob == dyn11)
			{
				airKnob_Click(dyn11, EventArgs.Empty);
			}
			dyn11.Show();
			if (activeKnob == dyn12)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn12.Hide();
			if (activeKnob == dyn13)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn13.Hide();
			if (activeKnob == dyn14)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn14.Hide();
			if (activeKnob == dyn15)
			{
				airKnob_Click(dyn10, EventArgs.Empty);
			}
			dyn15.Hide();
			btnDyn1Type.Text = "De-Ess";
			break;
		}
	}

	private void btnDyn0On_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn0On, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Dyn0On));
	}

	private void btnDyn1On_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.Dyn1On, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.Dyn1On));
	}

	private void btnEQOn_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQOn));
	}

	private void btnHPFOn_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQHPFOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQHPFOn));
	}

	private void btnDirectOut_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.DirectOutOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.DirectOutOn));
	}

	private void btnInsertOn_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.InsertOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.InsertOn));
	}

	public int Distance2D(Point p1, Point p2)
	{
		int num = 0;
		double num2 = Math.Pow(p2.X - p1.X, 2.0);
		double num3 = Math.Pow(p2.Y - p1.Y, 2.0);
		double d = num2 + num3;
		return (int)Math.Sqrt(d);
	}

	private void picEQ_MouseDown(object sender, MouseEventArgs e)
	{
		if ((e.Button & MouseButtons.Left) == 0)
		{
			return;
		}
		int num = 0;
		int num2 = -1;
		int num3 = 900;
		for (int i = 0; i < 4; i++)
		{
			num = Distance2D(eqPoints[i], e.Location);
			if (num < num3)
			{
				num3 = num;
				num2 = i;
			}
		}
		nearEQ = num2;
		if (num3 < base.Width / 12)
		{
			activeEQ = num2;
		}
		isDrag = true;
		startX = e.X;
		startY = e.Y;
		switch (activeEQ)
		{
		case 0:
			startF = eq1F.Value;
			startG = eq1G.Value;
			setEQBand("Low");
			break;
		case 1:
			startF = eq2F.Value;
			startG = eq2G.Value;
			setEQBand("LowMid");
			break;
		case 2:
			startF = eq3F.Value;
			startG = eq3G.Value;
			setEQBand("HiMid");
			break;
		case 3:
			if (MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type) == 2)
			{
				startF = eq4F.Value;
				startG = eq4G.Value;
				setEQBand("LPF");
			}
			else
			{
				startF = eq4F.Value;
				startG = eq4G.Value;
				setEQBand("High");
			}
			break;
		}
		picEQ_Plot();
	}

	private void picEQ_MouseMove(object sender, MouseEventArgs e)
	{
		if (isDrag)
		{
			int num = startF + (int)Math.Round((decimal)(e.X - startX) * (121.0m / (decimal)picEQ.Width) * (fader0.FineMode ? 0.2m : 1.0m));
			if (num > 124)
			{
				num = 124;
			}
			if (num < 4)
			{
				num = 4;
			}
			int num2 = startG + (int)Math.Round((decimal)(startY - e.Y) * (40.0m / (decimal)picEQ.Height) * 10.0m * (fader0.FineMode ? 0.2m : 1.0m));
			if (num2 > 180)
			{
				num2 = 180;
			}
			if (num2 < -180)
			{
				num2 = -180;
			}
			if (activeEQ != 3 || MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type) != 2)
			{
				faderGain.SetValue(num2);
			}
			faderFreq.SetValue(num);
			return;
		}
		int num3 = 0;
		int num4 = -1;
		int num5 = 900;
		for (int i = 0; i < 4; i++)
		{
			num3 = Distance2D(eqPoints[i], e.Location);
			if (num3 < num5)
			{
				num5 = num3;
				num4 = i;
			}
		}
		if (num5 < base.Width / 12 && nearEQ != num4)
		{
			nearEQ = num4;
			picEQ_Plot();
		}
	}

	private void picEQ_MouseUp(object sender, MouseEventArgs e)
	{
		isDrag = false;
	}

	private void picEQ_MouseDoubleClick(object sender, MouseEventArgs e)
	{
		switch (activeEQ)
		{
		}
	}

	private void btnToStereoOn_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.ToStereoOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.ToStereoOn));
	}

	private void btnToMonoOn_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.ToMonoOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.ToMonoOn));
	}

	private void eq4LPFOn_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4LPFOn, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQ4LPFOn));
		setEQBand("High");
	}

	private void eqFlat_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQFlatten, 0, 1);
	}

	private void btnZoom_Click(object sender, EventArgs e)
	{
		picEQ.Width = tabEQ.Width - 20;
		picEQ.Height = tabEQ.Height - 20;
		picEQ.Top = 10;
		picEQ.Left = 10;
		foreach (Control control in tabEQ.Controls)
		{
			if (control.Tag != null && control.Tag.ToString() != "EQPlot")
			{
				control.Hide();
			}
		}
	}

	private void frmChannel_ResizeBegin(object sender, EventArgs e)
	{
		isResizing = true;
	}

	private void frmChannel_Resize(object sender, EventArgs e)
	{
		if (base.WindowState != FormWindowState.Maximized && base.WindowState != FormWindowState.Minimized && !isFullScreen)
		{
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: Size changed to: " + base.Size.ToString());
			}
			thisWin.WindowSize = base.Size;
		}
		if (!isResizing)
		{
			MixEngine.logDebug("ChanView: Resize: Calling doLayout()");
			doLayout();
		}
	}

	private void frmChannel_ResizeEnd(object sender, EventArgs e)
	{
		isResizing = false;
		MixEngine.logDebug("ChanView: ResizeEnd: Calling doLayout()");
		doLayout();
	}

	public void doLayout()
	{
		if (parentSurface.isDebug)
		{
			MixEngine.logDebug("ChanView: Starting doLayout()");
		}
		if (arrSendPrePost == null)
		{
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: Cancelling doLayout() - still initializing objects");
			}
			return;
		}
		lastSize = base.ClientSize;
		tabSection.Size = base.ClientSize;
		tabSection.ItemSize = new Size((int)Math.Round(1.0 / 11.0 * (double)tabSection.Width), (int)Math.Round(0.1 * (double)tabSection.Height));
		MixEngine.logDebug("ChanView: doLayout: Size is " + base.ClientSize.ToString());
		tabDyn.Size = tabSection.ClientSize;
		tabEQ.Size = tabSection.ClientSize;
		tabSends.Size = tabSection.ClientSize;
		MixEngine.logDebug("ChanView: doLayout: TabDyn Size is " + tabDyn.Size.ToString());
		MixEngine.logDebug("ChanView: doLayout: TabEQ Size is " + tabEQ.Size.ToString());
		MixEngine.logDebug("ChanView: doLayout: TabSends Size is " + tabSends.Size.ToString());
		panelTopMenu.Location = new Point(tabSection.ItemSize.Width * 3 + 10, 0);
		panelTopMenu.Width = tabSection.Width - panelTopMenu.Left;
		panelTopMenu.Height = tabSection.ItemSize.Height;
		btnClose.Width = (int)Math.Round(0.10526315789473684 * (double)panelTopMenu.Width);
		btnClose.Height = panelTopMenu.Height - 5;
		btnClose.Left = panelTopMenu.Width - btnClose.Width - 10;
		btnClose.Top = 5;
		Font font = FontFit("Arial", FontStyle.Bold, "Option", btnClose.Width - 4, btnClose.Height - 4, 20f);
		new Font("Arial", font.Size * 0.75f);
		Font font2 = FontFit("Arial", FontStyle.Bold, "-999.99", btnClose.Width - 8, 20, 24f);
		btnClose.Font = font;
		btnFine.Size = btnClose.Size;
		btnFine.Left = btnClose.Left - btnFine.Width - 5;
		btnFine.Top = 5;
		btnFine.Font = font;
		btnOptions.Size = btnClose.Size;
		btnOptions.Left = btnFine.Left - btnOptions.Width - 5;
		btnOptions.Top = 5;
		btnOptions.Font = font;
		btnJump.Size = btnClose.Size;
		btnJump.Left = btnOptions.Left - btnJump.Width - 12;
		btnJump.Top = 5;
		btnJump.Font = font;
		btnNext.Size = btnClose.Size;
		btnNext.Left = btnJump.Left - btnNext.Width - 5;
		btnNext.Top = 5;
		btnNext.Font = font;
		btnPrev.Size = btnClose.Size;
		btnPrev.Left = btnNext.Left - btnPrev.Width - 5;
		btnPrev.Top = 5;
		btnPrev.Font = font;
		lblHeader.Location = new Point(0, 5);
		lblHeader.Width = btnPrev.Left - 10;
		lblHeader.Height = panelTopMenu.Height - 5;
		panelHA.Location = new Point(10, 5);
		panelHA.Size = new Size((int)Math.Round(0.125 * (double)tabDyn.Width), (int)Math.Round(0.5599999999999999 * (double)tabDyn.Height) - 5);
		knobHA.Location = new Point(10, 25);
		knobHA.Width = panelHA.Width - 20;
		knobHA.Height = (int)Math.Round(0.5 * (double)(panelHA.Height - 60));
		chanPhantom.Location = new Point(10, knobHA.Bottom + 5);
		chanPhantom.Width = panelHA.Width - 20;
		chanPhantom.Height = (int)Math.Round(0.25 * (double)(panelHA.Height - 60));
		chanPhantom.Font = font;
		chanPhase.Location = new Point(10, chanPhantom.Bottom + 5);
		chanPhase.Width = panelHA.Width - 20;
		chanPhase.Height = (int)Math.Round(0.25 * (double)(panelHA.Height - 60));
		chanPhase.Font = font;
		panelDyn0.Location = new Point(panelHA.Right + 10, 5);
		panelDyn0.Size = new Size((int)Math.Round(0.6875 * (double)tabDyn.Width), (int)Math.Round(0.27999999999999997 * (double)tabDyn.Height) - 5);
		btnDyn0Type.Location = new Point(20, 5);
		btnDyn0Type.Height = (int)Math.Round(0.5 * (double)(panelDyn0.Height - 15));
		btnDyn0Type.Width = (int)Math.Round(0.125 * (double)panelDyn0.Width);
		btnDyn0Type.Font = font;
		btnDyn0On.Location = new Point(20, btnDyn0Type.Bottom + 5);
		btnDyn0On.Height = (int)Math.Round(0.5 * (double)(panelDyn0.Height - 15));
		btnDyn0On.Width = (int)Math.Round(0.125 * (double)panelDyn0.Width);
		btnDyn0On.Font = font;
		int num = btnDyn0Type.Right + 10;
		int num2 = (int)Math.Round((double)(panelDyn0.Width - btnDyn0Type.Right - 55) / 6.0);
		foreach (Control control5 in panelDyn0.Controls)
		{
			if (control5 is AirKnob)
			{
				control5.Location = new Point(num, (int)Math.Round(1.0 / 3.0 * (double)panelDyn0.Height) - 5);
				control5.Width = num2;
				control5.Height = (int)Math.Round(2.0 / 3.0 * (double)panelDyn0.Height);
				num += control5.Width + 5;
			}
		}
		meterDyn0In.Location = new Point(dyn00.Left, 20);
		meterDyn0In.Width = dyn02.Right - dyn00.Left;
		meterDyn0In.Height = dyn00.Top - 25;
		meterDyn0Gain.Location = new Point(dyn03.Left, 20);
		meterDyn0Gain.Size = meterDyn0In.Size;
		panelDyn1.Location = new Point(panelHA.Right + 10, panelDyn0.Bottom + 5);
		panelDyn1.Size = panelDyn0.Size;
		btnDyn1Type.Location = new Point(20, 5);
		btnDyn1Type.Height = (int)Math.Round(0.5 * (double)(panelDyn1.Height - 15));
		btnDyn1Type.Width = (int)Math.Round(0.125 * (double)panelDyn1.Width);
		btnDyn1Type.Font = font;
		btnDyn1On.Location = new Point(20, btnDyn1Type.Bottom + 5);
		btnDyn1On.Height = (int)Math.Round(0.5 * (double)(panelDyn1.Height - 15));
		btnDyn1On.Width = (int)Math.Round(0.125 * (double)panelDyn1.Width);
		btnDyn1On.Font = font;
		num = btnDyn1Type.Right + 10;
		num2 = (int)Math.Round((double)(panelDyn0.Width - btnDyn0Type.Right - 55) / 6.0);
		foreach (Control control6 in panelDyn1.Controls)
		{
			if (control6 is AirKnob)
			{
				control6.Location = new Point(num, (int)Math.Round(1.0 / 3.0 * (double)panelDyn1.Height) - 5);
				control6.Width = num2;
				control6.Height = (int)Math.Round(2.0 / 3.0 * (double)panelDyn1.Height);
				num += control6.Width + 5;
			}
		}
		meterDyn1In.Location = new Point(dyn10.Left, 20);
		meterDyn1In.Width = dyn12.Right - dyn10.Left;
		meterDyn1In.Height = dyn10.Top - 25;
		meterDyn1Gain.Location = new Point(dyn13.Left, 20);
		meterDyn1Gain.Size = meterDyn1In.Size;
		airMeterPre.Location = new Point(panelDyn0.Right + 10, 5);
		airMeterPre.Width = (int)Math.Round(0.025 * (double)tabDyn.Width);
		airMeterPre.Height = tabDyn.Height - 10;
		airMeterPost.Location = new Point(airMeterPre.Right + 5, 5);
		airMeterPost.Size = airMeterPre.Size;
		if (Settings.Default.ChannelStrip.IndexOf("Mute") > Settings.Default.ChannelStrip.IndexOf("Fader"))
		{
			chanFader.Location = new Point(airMeterPost.Right + 10, 5);
			chanFader.Width = tabDyn.Width - airMeterPost.Right - 20;
			chanFader.Height = intRound((decimal)tabDyn.Height * 0.91m);
			chanFader.Font = font2;
			chanMute.Location = new Point(airMeterPost.Right + 10, chanFader.Bottom + 5);
			chanMute.Width = tabDyn.Width - airMeterPost.Right - 20;
			chanMute.Height = tabDyn.Height - chanMute.Top - 5;
			chanMute.Font = font;
		}
		else
		{
			chanMute.Location = new Point(airMeterPost.Right + 10, 5);
			chanMute.Width = tabDyn.Width - airMeterPost.Right - 20;
			chanMute.Height = intRound((decimal)tabDyn.Height * 0.09m);
			chanMute.Font = font;
			chanFader.Location = new Point(airMeterPost.Right + 10, chanMute.Bottom + 5);
			chanFader.Width = tabDyn.Width - airMeterPost.Right - 20;
			chanFader.Height = tabDyn.Height - chanFader.Top - 5;
			chanFader.Font = font2;
		}
		panelPAN.Location = new Point(10, panelHA.Bottom + 5);
		panelPAN.Width = knobHA.Width * 2 + 30;
		panelPAN.Height = (int)Math.Round(0.3 * (double)tabDyn.Height) - 5;
		chanPan.Location = new Point(10, 25);
		chanPan.Width = knobHA.Width;
		chanPan.Height = (int)Math.Round((double)panelPAN.Height - 50.0);
		btnToStereoOn.Location = new Point(chanPan.Right + 10, 10);
		btnToStereoOn.Width = panelPAN.Width - 40 - chanPan.Width;
		btnToStereoOn.Height = (int)Math.Round((double)(panelPAN.Height - 20) / 2.0) - 5;
		btnToStereoOn.Font = font;
		btnToMonoOn.Location = new Point(chanPan.Right + 10, btnToStereoOn.Bottom + 10);
		btnToMonoOn.Size = btnToStereoOn.Size;
		btnToMonoOn.Font = font;
		panelMutes.Location = new Point(panelPAN.Right + 10, panelHA.Bottom + 5);
		panelMutes.Size = new Size((int)Math.Round((double)(panelDyn0.Right - panelPAN.Right - 20) / 2.0), panelPAN.Height);
		lblMuteGroups.Location = new Point(10, 1);
		lblMuteGroups.Size = new Size(panelMutes.Width - 20, 22);
		num = 10;
		int num3 = 0;
		foreach (Control control7 in panelMutes.Controls)
		{
			if (control7 is AirButton)
			{
				control7.Location = new Point(num, 23 + (int)Math.Round((double)(num3++ / 4) * ((double)(panelMutes.Height - 40) / 2.0 + 10.0)));
				control7.Width = (int)Math.Round(0.25 * (double)(panelMutes.Width - 15)) - 5;
				control7.Height = (int)Math.Round((double)(panelMutes.Height - 40) / 2.0);
				control7.Font = font;
				num += control7.Width + 5;
				if (num3 == 4)
				{
					num = 10;
				}
			}
		}
		panelDCA.Location = new Point(panelMutes.Right + 10, panelHA.Bottom + 5);
		panelDCA.Size = new Size(panelDyn1.Right - panelMutes.Right - 10, panelPAN.Height);
		lblDCA.Location = new Point(10, 1);
		lblDCA.Size = new Size(panelDCA.Width - 20, 22);
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			lblDCA.Text = "BUS Assign";
		}
		else
		{
			lblDCA.Text = "DCAs";
		}
		num = 10;
		num3 = 0;
		foreach (Control control8 in panelDCA.Controls)
		{
			if (control8 is AirButton)
			{
				control8.Location = new Point(num, 23 + (int)Math.Round((double)(num3++ / 4) * ((double)(panelDCA.Height - 40) / 2.0 + 10.0)));
				control8.Width = (int)Math.Round(0.25 * (double)(panelDCA.Width - 15)) - 5;
				control8.Height = (int)Math.Round((double)(panelDCA.Height - 40) / 2.0);
				control8.Font = font;
				num += control8.Width + 5;
				if (num3 == 4)
				{
					num = 10;
				}
			}
		}
		airFader1.Top = panelPAN.Bottom + 5;
		airFader1.Left = 10;
		airFader1.Height = tabDyn.Height - panelPAN.Bottom - 10;
		airFader1.Width = panelDyn0.Right - 10;
		airFader1.Font = font2;
		btnHPFOn.Location = new Point(10, 5);
		btnHPFOn.Width = (int)Math.Round(1.0 / 12.0 * (double)tabEQ.Width);
		btnHPFOn.Height = (int)Math.Round(1.0 / 9.0 * (double)tabEQ.Height);
		btnHPFOn.Font = font;
		eqHPFFreq.Size = btnHPFOn.Size;
		eqHPFFreq.Location = new Point(btnHPFOn.Right + 10, 5);
		btnFlat.Size = btnHPFOn.Size;
		btnFlat.Left = (int)Math.Round((double)tabEQ.Width / 2.0 - (double)btnEQOn.Width / 2.0);
		btnFlat.Top = 5;
		btnFlat.Font = font;
		btnEQOn.Size = btnHPFOn.Size;
		btnEQOn.Location = new Point(btnFlat.Right + 5, 5);
		btnEQOn.Font = font;
		btnPaste.Size = btnHPFOn.Size;
		btnPaste.Location = new Point(btnFlat.Left - btnPaste.Width - 25, 5);
		btnPaste.Font = font;
		btnCopy.Size = btnHPFOn.Size;
		btnCopy.Location = new Point(btnPaste.Left - btnCopy.Width - 5, 5);
		btnCopy.Font = font;
		lblGain.Width = btnHPFOn.Width;
		lblGain.Left = tabEQ.Width - lblGain.Width - 10;
		lblGain.Top = 5;
		lblGain.Height = 20;
		faderGain.Width = lblGain.Width;
		faderGain.Left = lblGain.Left;
		faderGain.Top = lblGain.Bottom + 5;
		faderGain.Height = tabEQ.Height - 10 - lblGain.Bottom;
		faderGain.Font = font2;
		btnHiEQType.Size = btnHPFOn.Size;
		btnHiEQType.Left = faderGain.Left - btnHiEQType.Width - 10;
		btnHiEQType.Top = 5;
		btnHiEQType.Font = font;
		eqAtt.Size = eqHPFFreq.Size;
		eqAtt.Left = btnHiEQType.Left - eqAtt.Width - 10;
		eqAtt.Top = 5;
		eq4G.Width = btnHPFOn.Width;
		eq4G.Height = (int)Math.Round((double)(tabEQ.Height - 2 * (btnHPFOn.Height + 5)) / 4.0) - 5;
		eq4G.Location = new Point(btnHiEQType.Left, btnHiEQType.Bottom + 5);
		eq4LPF.Size = eq4G.Size;
		eq4LPF.Location = eq4G.Location;
		eq3G.Size = eq4G.Size;
		eq3G.Location = new Point(btnHiEQType.Left, eq4G.Bottom + 5);
		eq2G.Size = eq4G.Size;
		eq2G.Location = new Point(btnHiEQType.Left, eq3G.Bottom + 5);
		eq1G.Size = eq4G.Size;
		eq1G.Location = new Point(btnHiEQType.Left, eq2G.Bottom + 5);
		btnLowEQType.Size = btnHPFOn.Size;
		btnLowEQType.Location = new Point(btnHiEQType.Left, eq1G.Bottom + 5);
		btnLowEQType.Font = font;
		faderQ.Left = 10;
		faderQ.Width = btnHiEQType.Left - 20;
		faderQ.Height = btnHiEQType.Height;
		faderQ.Top = tabEQ.Height - faderQ.Height - 5;
		faderQ.Font = font2;
		lblQ.Location = new Point(faderQ.Left + 2, faderQ.Top + 2);
		lblQ.Height = faderQ.Height - 4;
		faderFreq.Size = faderQ.Size;
		faderFreq.Location = new Point(10, faderQ.Top - faderFreq.Height - 5);
		faderFreq.Font = font2;
		lblFreq.Location = new Point(faderFreq.Left + 2, faderFreq.Top + 2);
		lblFreq.Height = faderFreq.Height - 4;
		picEQ.Location = new Point(10, btnHPFOn.Bottom + 5);
		picEQ.Width = btnHiEQType.Left - 20;
		picEQ.Height = faderFreq.Top - btnHPFOn.Bottom - 10;
		Size size = new Size((int)Math.Round((double)(tabSends.Width - 50) / 9.0), (int)Math.Round((double)tabSends.Height * 0.8));
		Font font3 = FontFit("Arial", FontStyle.Bold, "FFFFFF", (int)Math.Round((double)size.Width * 0.9), (int)Math.Round((double)size.Width * 0.39), 18f);
		for (int i = 0; i < arrSendPrePost.Length; i++)
		{
			int left = i * (size.Width + 5);
			arrSendPrePost[i].Width = size.Width;
			arrSendPrePost[i].Height = (int)Math.Round((double)tabSends.Height * 0.06);
			arrSendPrePost[i].Location = new Point(left, 5);
			arrSendPrePost[i].Font = font;
			arrSendOn[i].Width = size.Width;
			arrSendOn[i].Height = (int)Math.Round((double)tabSends.Height * 0.07);
			arrSendOn[i].Location = new Point(left, arrSendPrePost[i].Bottom + 5);
			arrSendOn[i].Font = font;
			arrSendChanLabels[i].Width = size.Width;
			arrSendChanLabels[i].Height = (int)Math.Round((double)size.Width * 0.4);
			arrSendChanLabels[i].Location = new Point(left, tabSends.Height - arrSendChanLabels[i].Height - 5);
			arrSendChanLabels[i].Font = font3;
			arrFaders[i].Top = arrSendOn[i].Bottom + 5;
			arrFaders[i].Left = left;
			arrFaders[i].Width = size.Width;
			arrFaders[i].Height = arrSendChanLabels[i].Top - arrSendOn[i].Bottom - 10;
			arrFaders[i].Font = font2;
		}
		btnPage1.Size = new Size(size.Width, size.Width);
		btnPage1.Left = arrFaders[7].Right + 10;
		btnPage1.Top = (int)Math.Round((double)tabSends.Height / 2.0);
		btnPage1.Font = font;
		btnPage0.Size = btnPage1.Size;
		btnPage0.Left = btnPage1.Left;
		btnPage0.Top = btnPage1.Top - btnPage0.Height - 10;
		btnPage0.Font = font;
		btnPage2.Size = btnPage1.Size;
		btnPage2.Left = btnPage1.Left;
		btnPage2.Top = btnPage1.Bottom + 10;
		btnPage2.Font = font;
		lblHeader.Font = FontFit("Arial", FontStyle.Bold, lblHeader.Text, (int)Math.Round((double)lblHeader.Width * 0.9), (int)Math.Round((double)lblHeader.Height * 0.9), 18f);
		picEQ_Plot();
	}

	private void frmChannel_Shown(object sender, EventArgs e)
	{
		setSOFPage(0);
		airKnob_Click(dyn00, EventArgs.Empty);
	}

	private void btnCopy_Click(object sender, EventArgs e)
	{
		MixEngine.PEQCopy.eq1Type = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type);
		MixEngine.PEQCopy.eq1HPF = MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQ1HPFOn);
		MixEngine.PEQCopy.eq1F = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1F);
		MixEngine.PEQCopy.eq1G = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1G);
		MixEngine.PEQCopy.eq1Q = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Q);
		MixEngine.PEQCopy.eq2F = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ2F);
		MixEngine.PEQCopy.eq2G = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ2G);
		MixEngine.PEQCopy.eq2Q = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ2Q);
		MixEngine.PEQCopy.eq3F = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ3F);
		MixEngine.PEQCopy.eq3G = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ3G);
		MixEngine.PEQCopy.eq3Q = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ3Q);
		MixEngine.PEQCopy.eq4F = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4F);
		MixEngine.PEQCopy.eq4G = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4G);
		MixEngine.PEQCopy.eq4Q = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Q);
		MixEngine.PEQCopy.eq4LPF = MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQ4LPFOn);
		MixEngine.PEQCopy.eq4Type = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type);
		MixEngine.PEQCopy.eqAtt = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQAtt);
		MixEngine.PEQCopy.eqHPFFreq = MixEngine.chData[activeChannel].getParam(Channel.Pr.EQHPFFreq);
		MixEngine.PEQCopy.eqHPFOn = MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQHPFOn);
		MixEngine.PEQCopy.eqOn = MixEngine.chData[activeChannel].getParamBool(Channel.Pr.EQOn);
		MixEngine.PEQCopy.Notify();
	}

	private void btnPaste_Click(object sender, EventArgs e)
	{
		try
		{
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Q, 0, MixEngine.PEQCopy.eq1Q);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1F, 0, MixEngine.PEQCopy.eq1F);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1G, 0, MixEngine.PEQCopy.eq1G);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ2Q, 0, MixEngine.PEQCopy.eq2Q);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ2F, 0, MixEngine.PEQCopy.eq2F);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ2G, 0, MixEngine.PEQCopy.eq2G);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ3Q, 0, MixEngine.PEQCopy.eq3Q);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ3F, 0, MixEngine.PEQCopy.eq3F);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ3G, 0, MixEngine.PEQCopy.eq3G);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4Q, 0, MixEngine.PEQCopy.eq4Q);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4F, 0, MixEngine.PEQCopy.eq4F);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4G, 0, MixEngine.PEQCopy.eq4G);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4LPFOn, 0, MixEngine.PEQCopy.eq4LPF);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQOn, 0, MixEngine.PEQCopy.eqOn);
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQAtt, 0, MixEngine.PEQCopy.eqAtt);
			if ((channelType == ChannelTypes.Input || channelType == ChannelTypes.STIN) && Console.type != ConsoleTypes.O1V96V2)
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQHPFFreq, 0, MixEngine.PEQCopy.eqHPFFreq);
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQHPFOn, 0, MixEngine.PEQCopy.eqHPFOn);
				if (MixEngine.PEQCopy.eq1Type == 2)
				{
					MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Type, 0, 1);
				}
				else
				{
					MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Type, 0, MixEngine.PEQCopy.eq1Type);
				}
			}
			else
			{
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1Type, 0, MixEngine.PEQCopy.eq1Type);
				MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ1HPFOn, 0, MixEngine.PEQCopy.eq1HPF);
			}
			MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.EQ4Type, 0, MixEngine.PEQCopy.eq4Type);
			setEQType(1, MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ1Type));
			setEQType(4, MixEngine.chData[activeChannel].getParam(Channel.Pr.EQ4Type));
			picEQ_Plot();
		}
		catch (Exception ex)
		{
			logError("PEQ_Paste()", "UI", ex.ToString());
		}
	}

	private void chanMute_Click(object sender, EventArgs e)
	{
		MixEngine.chData[activeChannel].setParam("UI", Channel.Pr.On, 0, !MixEngine.chData[activeChannel].getParamBool(Channel.Pr.On));
	}

	private void btnOptions_Click(object sender, EventArgs e)
	{
		Form form = new Form();
		Font font = new Font("Arial", 14f, FontStyle.Bold);
		form.Width = 200;
		form.Height = 386;
		form.BackColor = Settings.Default.c_background;
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		form.TopMost = true;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = btnOptions.PointToScreen(new Point(btnOptions.Width - 200, btnOptions.Height + 10));
		form.Name = "panelOptions";
		AirPanel airPanel = new AirPanel();
		airPanel.Location = new Point(0, 0);
		airPanel.Size = new Size(form.Width - 1, form.Height - 1);
		airPanel.PanelType = 2;
		airPanel.BackColor = Color.Gold;
		form.Controls.Add(airPanel);
		Label label = new Label();
		label.ForeColor = Color.Black;
		label.Font = new Font("Arial", 12f, FontStyle.Bold);
		label.Location = new Point(20, 9);
		label.Name = "lblPicker";
		label.Size = new Size(form.Width - 40, 24);
		label.TabIndex = 0;
		label.Text = "Window Options";
		label.TextAlign = ContentAlignment.TopCenter;
		airPanel.Controls.Add(label);
		AirButton airButton = new AirButton();
		airButton.BackColor = ((base.FormBorderStyle == FormBorderStyle.None) ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
		airButton.ForeColor = ((base.FormBorderStyle == FormBorderStyle.None) ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
		airButton.ParentColor = airPanel.BackColor;
		airButton.Location = new Point(10, 36);
		airButton.Name = "btnFullScreen";
		airButton.Size = new Size(180, 60);
		airButton.Font = font;
		airButton.TabIndex = 128;
		airButton.Text = "Full Screen";
		airButton.Click += btnOption_Click;
		airPanel.Controls.Add(airButton);
		AirButton airButton2 = new AirButton();
		airButton2.BackColor = (base.TopMost ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
		airButton2.ForeColor = (base.TopMost ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
		airButton2.ParentColor = airPanel.BackColor;
		airButton2.Location = new Point(10, 106);
		airButton2.Name = "btnTopMost";
		airButton2.Size = new Size(180, 60);
		airButton2.Font = font;
		airButton2.TabIndex = 128;
		airButton2.Text = "Stay On Top";
		airButton2.Click += btnOption_Click;
		airPanel.Controls.Add(airButton2);
		AirButton airButton3 = new AirButton();
		airButton3.BackColor = Settings.Default.c_button_inactive;
		airButton3.ForeColor = Settings.Default.c_button_inactive_fore;
		airButton3.ParentColor = airPanel.BackColor;
		airButton3.Location = new Point(10, 176);
		airButton3.Name = "btnNewWin";
		airButton3.Size = new Size(180, 60);
		airButton3.Font = font;
		airButton3.TabIndex = 128;
		airButton3.Text = "New Window";
		airButton3.Click += btnOption_Click;
		airPanel.Controls.Add(airButton3);
		AirButton airButton4 = new AirButton();
		airButton4.BackColor = (consoleLink ? Settings.Default.c_button_active : Settings.Default.c_button_inactive);
		airButton4.ForeColor = (consoleLink ? Settings.Default.c_button_active_fore : Settings.Default.c_button_inactive_fore);
		airButton4.ParentColor = airPanel.BackColor;
		airButton4.Location = new Point(10, 246);
		airButton4.Name = "btnFollow";
		airButton4.Size = new Size(180, 60);
		airButton4.Font = font;
		airButton4.TabIndex = 129;
		airButton4.Text = "Link To Console\nChannel Select";
		airButton4.Click += btnOption_Click;
		airPanel.Controls.Add(airButton4);
		AirButton airButton5 = new AirButton();
		airButton5.BackColor = Settings.Default.c_button_inactive;
		airButton5.ForeColor = Settings.Default.c_button_inactive_fore;
		airButton5.ParentColor = airPanel.BackColor;
		airButton5.Location = new Point(10, 316);
		airButton5.Name = "btnCancel";
		airButton5.Size = new Size(180, 60);
		airButton5.Font = font;
		airButton5.TabIndex = 129;
		airButton5.Text = "Cancel";
		airButton5.Click += btnOption_Click;
		airPanel.Controls.Add(airButton5);
		form.ShowDialog();
	}

	private void btnOption_Click(object sender, EventArgs e)
	{
		AirButton airButton = (AirButton)sender;
		switch (airButton.Name)
		{
		case "btnTopMost":
			base.TopMost = !base.TopMost;
			break;
		case "btnFullScreen":
			if (!isFullScreen)
			{
				isFullScreen = true;
				isResizing = true;
				thisWin.WindowState = "Fullscreen";
				thisWin.SetWindow(this);
				isResizing = false;
				doLayout();
			}
			else
			{
				isResizing = true;
				thisWin.WindowState = "Normal";
				thisWin.SetWindow(this);
				isFullScreen = false;
				isResizing = false;
				doLayout();
			}
			break;
		case "btnFollow":
			if (consoleLink)
			{
				consoleLink = false;
			}
			else
			{
				consoleLink = true;
				ActiveChannel = Console.selChannel;
			}
			Settings.Default.ConsoleFollow = consoleLink;
			Settings.Default.Save();
			break;
		case "btnNewWin":
		{
			foreach (Form openForm in Application.OpenForms)
			{
				if (openForm is frmChannel && !openForm.Visible)
				{
					((frmChannel)openForm).ActiveChannel = activeChannel;
					((frmChannel)openForm).Activate();
					((frmChannel)openForm).Show();
					((Form)airButton.Parent.Parent).Close();
					return;
				}
			}
			frmChannel frmChannel2 = new frmChannel(parentSurface);
			frmChannel2.ActiveChannel = activeChannel;
			frmChannel2.Show();
			break;
		}
		}
		((Form)airButton.Parent.Parent).Close();
	}

	private void lblHeader_MouseDown(object sender, MouseEventArgs e)
	{
	}

	private void lblHoldTimer_Tick(object sender, EventArgs e)
	{
	}

	private void lblHeader_MouseUp(object sender, MouseEventArgs e)
	{
	}

	private void lblHeader_Click(object sender, EventArgs e)
	{
		frmKeyboard frmKeyboard2 = new frmKeyboard();
		frmKeyboard2.setProperty("ChannelName", activeChannel);
		frmKeyboard2.ShowDialog(this);
	}

	private void frmChannel_LocationChanged(object sender, EventArgs e)
	{
		if (base.WindowState != FormWindowState.Maximized && base.WindowState != FormWindowState.Minimized && !isFullScreen)
		{
			if (parentSurface.isDebug)
			{
				MixEngine.logDebug("ChanView: Location changed to: " + base.Location.ToString());
			}
			thisWin.WindowPos = base.Location;
		}
	}

	private void btnDCAAssign0_Click(object sender, EventArgs e)
	{
	}

	private void frmChannel_Move(object sender, EventArgs e)
	{
	}

	private void airKnob_MouseDown(object sender, MouseEventArgs e)
	{
		airKnob_Click(sender, e);
	}

	private void eqKnob_MouseDown(object sender, MouseEventArgs e)
	{
		eq_Click(sender, e);
	}

	private void btnJump_Click(object sender, EventArgs e)
	{
		showJumper();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirFader.frmChannel));
		this.lblHeader = new System.Windows.Forms.Label();
		this.panelTopMenu = new System.Windows.Forms.Panel();
		this.btnJump = new AirControls.AirButton();
		this.btnOptions = new AirControls.AirButton();
		this.btnFine = new AirControls.AirButton();
		this.btnPrev = new AirControls.AirButton();
		this.btnClose = new AirControls.AirButton();
		this.btnNext = new AirControls.AirButton();
		this.airPanel7 = new AirControls.AirPanel();
		this.lblEq4 = new System.Windows.Forms.Label();
		this.eq1Q = new AirControls.AirKnob();
		this.lblEq3 = new System.Windows.Forms.Label();
		this.eq2Q = new AirControls.AirKnob();
		this.eq3Q = new AirControls.AirKnob();
		this.lblEq2 = new System.Windows.Forms.Label();
		this.eq4Q = new AirControls.AirKnob();
		this.eq1F = new AirControls.AirKnob();
		this.eq2F = new AirControls.AirKnob();
		this.lblEq1 = new System.Windows.Forms.Label();
		this.eq3F = new AirControls.AirKnob();
		this.eq4F = new AirControls.AirKnob();
		this.airPanel5 = new AirControls.AirPanel();
		this.btnDirectOut = new AirControls.AirButton();
		this.btnInsertOn = new AirControls.AirButton();
		this.btnSafe = new AirControls.AirButton();
		this.tabSection = new AirFader.CTabControl();
		this.tabDyn = new System.Windows.Forms.TabPage();
		this.panelDCA = new AirControls.AirPanel();
		this.lblDCA = new System.Windows.Forms.Label();
		this.btnDCAAssign0 = new AirControls.AirButton();
		this.btnDCAAssign1 = new AirControls.AirButton();
		this.btnDCAAssign2 = new AirControls.AirButton();
		this.btnDCAAssign3 = new AirControls.AirButton();
		this.btnDCAAssign4 = new AirControls.AirButton();
		this.btnDCAAssign5 = new AirControls.AirButton();
		this.btnDCAAssign6 = new AirControls.AirButton();
		this.btnDCAAssign7 = new AirControls.AirButton();
		this.panelHA = new AirControls.AirPanel();
		this.knobHA = new AirControls.AirKnob();
		this.chanPhantom = new AirControls.AirButton();
		this.chanPhase = new AirControls.AirButton();
		this.panelDyn1 = new AirControls.AirPanel();
		this.meterDyn1Gain = new AirControls.AirMeter();
		this.meterDyn1In = new AirControls.AirMeter();
		this.btnDyn1Type = new AirControls.AirButton();
		this.btnDyn1On = new AirControls.AirButton();
		this.dyn10 = new AirControls.AirKnob();
		this.dyn11 = new AirControls.AirKnob();
		this.dyn12 = new AirControls.AirKnob();
		this.dyn13 = new AirControls.AirKnob();
		this.dyn14 = new AirControls.AirKnob();
		this.dyn15 = new AirControls.AirKnob();
		this.panelMutes = new AirControls.AirPanel();
		this.lblMuteGroups = new System.Windows.Forms.Label();
		this.btnMuteAssign0 = new AirControls.AirButton();
		this.btnMuteAssign1 = new AirControls.AirButton();
		this.btnMuteAssign2 = new AirControls.AirButton();
		this.btnMuteAssign3 = new AirControls.AirButton();
		this.btnMuteAssign4 = new AirControls.AirButton();
		this.btnMuteAssign5 = new AirControls.AirButton();
		this.btnMuteAssign6 = new AirControls.AirButton();
		this.btnMuteAssign7 = new AirControls.AirButton();
		this.panelDyn0 = new AirControls.AirPanel();
		this.meterDyn0Gain = new AirControls.AirMeter();
		this.meterDyn0In = new AirControls.AirMeter();
		this.btnDyn0Type = new AirControls.AirButton();
		this.btnDyn0On = new AirControls.AirButton();
		this.dyn00 = new AirControls.AirKnob();
		this.dyn01 = new AirControls.AirKnob();
		this.dyn02 = new AirControls.AirKnob();
		this.dyn03 = new AirControls.AirKnob();
		this.dyn04 = new AirControls.AirKnob();
		this.dyn05 = new AirControls.AirKnob();
		this.chanMute = new AirControls.AirButton();
		this.airFader1 = new AirControls.AirFader();
		this.panelPAN = new AirControls.AirPanel();
		this.chanPan = new AirControls.AirKnob();
		this.lblParam = new System.Windows.Forms.Label();
		this.btnToMonoOn = new AirControls.AirButton();
		this.btnToStereoOn = new AirControls.AirButton();
		this.chanPanMode = new AirControls.AirButton();
		this.chanLCRLevel = new AirControls.AirKnob();
		this.airMeterPre = new AirControls.AirMeter();
		this.airMeterPost = new AirControls.AirMeter();
		this.chanFader = new AirControls.AirFader();
		this.tabEQ = new System.Windows.Forms.TabPage();
		this.eqAtt = new AirControls.AirKnob();
		this.btnPaste = new AirControls.AirButton();
		this.btnCopy = new AirControls.AirButton();
		this.btnEQOn = new AirControls.AirButton();
		this.btnFlat = new AirControls.AirButton();
		this.eqHPFFreq = new AirControls.AirKnob();
		this.lblGain = new System.Windows.Forms.Label();
		this.btnHPFOn = new AirControls.AirButton();
		this.lblFreq = new System.Windows.Forms.Label();
		this.eq1G = new AirControls.AirKnob();
		this.eq4G = new AirControls.AirKnob();
		this.lblQ = new System.Windows.Forms.Label();
		this.eq4LPF = new AirControls.AirKnob();
		this.faderGain = new AirControls.AirFader();
		this.eq2G = new AirControls.AirKnob();
		this.faderFreq = new AirControls.AirFader();
		this.eq3G = new AirControls.AirKnob();
		this.faderQ = new AirControls.AirFader();
		this.picEQ = new AirControls.AirPanel();
		this.btnLowEQType = new AirControls.AirButton();
		this.btnHiEQType = new AirControls.AirButton();
		this.tabSends = new System.Windows.Forms.TabPage();
		this.btnPage2 = new AirControls.AirButton();
		this.btnSendOn07 = new AirControls.AirButton();
		this.btnSendPre07 = new AirControls.AirButton();
		this.btnSendOn06 = new AirControls.AirButton();
		this.btnSendPre06 = new AirControls.AirButton();
		this.btnSendOn05 = new AirControls.AirButton();
		this.btnSendPre05 = new AirControls.AirButton();
		this.btnSendOn04 = new AirControls.AirButton();
		this.btnSendPre04 = new AirControls.AirButton();
		this.btnSendOn03 = new AirControls.AirButton();
		this.btnSendPre03 = new AirControls.AirButton();
		this.btnSendOn02 = new AirControls.AirButton();
		this.btnSendPre02 = new AirControls.AirButton();
		this.btnSendOn01 = new AirControls.AirButton();
		this.btnSendPre01 = new AirControls.AirButton();
		this.btnSendOn00 = new AirControls.AirButton();
		this.btnSendPre00 = new AirControls.AirButton();
		this.lblChan00 = new System.Windows.Forms.Label();
		this.btnPage1 = new AirControls.AirButton();
		this.btnPage0 = new AirControls.AirButton();
		this.lblChan07 = new System.Windows.Forms.Label();
		this.lblChan06 = new System.Windows.Forms.Label();
		this.lblChan05 = new System.Windows.Forms.Label();
		this.lblChan01 = new System.Windows.Forms.Label();
		this.lblChan04 = new System.Windows.Forms.Label();
		this.lblChan02 = new System.Windows.Forms.Label();
		this.lblChan03 = new System.Windows.Forms.Label();
		this.fader0 = new AirControls.AirFader();
		this.fader1 = new AirControls.AirFader();
		this.fader2 = new AirControls.AirFader();
		this.fader3 = new AirControls.AirFader();
		this.fader4 = new AirControls.AirFader();
		this.fader5 = new AirControls.AirFader();
		this.fader6 = new AirControls.AirFader();
		this.fader7 = new AirControls.AirFader();
		this.panelTopMenu.SuspendLayout();
		this.airPanel7.SuspendLayout();
		this.airPanel5.SuspendLayout();
		this.tabSection.SuspendLayout();
		this.tabDyn.SuspendLayout();
		this.panelDCA.SuspendLayout();
		this.panelHA.SuspendLayout();
		this.panelDyn1.SuspendLayout();
		this.panelMutes.SuspendLayout();
		this.panelDyn0.SuspendLayout();
		this.panelPAN.SuspendLayout();
		this.tabEQ.SuspendLayout();
		this.tabSends.SuspendLayout();
		base.SuspendLayout();
		this.lblHeader.Font = new System.Drawing.Font("Arial", 18f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblHeader.Location = new System.Drawing.Point(3, 6);
		this.lblHeader.Name = "lblHeader";
		this.lblHeader.Size = new System.Drawing.Size(236, 52);
		this.lblHeader.TabIndex = 106;
		this.lblHeader.Text = "Ch 1 - Kick";
		this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblHeader.Click += new System.EventHandler(lblHeader_Click);
		this.lblHeader.MouseDown += new System.Windows.Forms.MouseEventHandler(lblHeader_MouseDown);
		this.lblHeader.MouseUp += new System.Windows.Forms.MouseEventHandler(lblHeader_MouseUp);
		this.panelTopMenu.Controls.Add(this.btnJump);
		this.panelTopMenu.Controls.Add(this.btnOptions);
		this.panelTopMenu.Controls.Add(this.lblHeader);
		this.panelTopMenu.Controls.Add(this.btnFine);
		this.panelTopMenu.Controls.Add(this.btnPrev);
		this.panelTopMenu.Controls.Add(this.btnClose);
		this.panelTopMenu.Controls.Add(this.btnNext);
		this.panelTopMenu.Location = new System.Drawing.Point(229, 1);
		this.panelTopMenu.Name = "panelTopMenu";
		this.panelTopMenu.Size = new System.Drawing.Size(571, 63);
		this.panelTopMenu.TabIndex = 115;
		this.btnJump.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnJump.BackText = "";
		this.btnJump.BorderColor = System.Drawing.Color.Black;
		this.btnJump.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnJump.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnJump.LEDColor = System.Drawing.Color.Gray;
		this.btnJump.Location = new System.Drawing.Point(305, 1);
		this.btnJump.Name = "btnJump";
		this.btnJump.ParentColor = System.Drawing.Color.Gray;
		this.btnJump.Size = new System.Drawing.Size(33, 60);
		this.btnJump.TabIndex = 113;
		this.btnJump.Text = "Jump";
		this.btnJump.UseVisualStyleBackColor = false;
		this.btnJump.Click += new System.EventHandler(btnJump_Click);
		this.btnOptions.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnOptions.BackText = "";
		this.btnOptions.BorderColor = System.Drawing.Color.Black;
		this.btnOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnOptions.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnOptions.LEDColor = System.Drawing.Color.Gray;
		this.btnOptions.Location = new System.Drawing.Point(387, 1);
		this.btnOptions.Name = "btnOptions";
		this.btnOptions.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnOptions.Size = new System.Drawing.Size(33, 60);
		this.btnOptions.TabIndex = 112;
		this.btnOptions.Text = "Option";
		this.btnOptions.UseVisualStyleBackColor = false;
		this.btnOptions.Click += new System.EventHandler(btnOptions_Click);
		this.btnFine.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnFine.BackText = "";
		this.btnFine.BorderColor = System.Drawing.Color.Black;
		this.btnFine.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnFine.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFine.LEDColor = System.Drawing.Color.Gray;
		this.btnFine.Location = new System.Drawing.Point(426, 1);
		this.btnFine.Name = "btnFine";
		this.btnFine.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnFine.Size = new System.Drawing.Size(66, 60);
		this.btnFine.TabIndex = 111;
		this.btnFine.Text = "Fine\nMode";
		this.btnFine.UseVisualStyleBackColor = false;
		this.btnFine.Click += new System.EventHandler(btnFine_Click);
		this.btnPrev.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnPrev.BackText = "";
		this.btnPrev.BorderColor = System.Drawing.Color.Black;
		this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnPrev.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnPrev.LEDColor = System.Drawing.Color.Gray;
		this.btnPrev.Location = new System.Drawing.Point(245, 1);
		this.btnPrev.Name = "btnPrev";
		this.btnPrev.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnPrev.Size = new System.Drawing.Size(53, 60);
		this.btnPrev.TabIndex = 110;
		this.btnPrev.Text = "<";
		this.btnPrev.UseVisualStyleBackColor = false;
		this.btnPrev.Click += new System.EventHandler(btnPrev_Click);
		this.btnClose.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnClose.BackText = "";
		this.btnClose.BorderColor = System.Drawing.Color.Black;
		this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnClose.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnClose.LEDColor = System.Drawing.Color.Gray;
		this.btnClose.Location = new System.Drawing.Point(498, 1);
		this.btnClose.Name = "btnClose";
		this.btnClose.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnClose.Size = new System.Drawing.Size(66, 60);
		this.btnClose.TabIndex = 87;
		this.btnClose.Text = "Close";
		this.btnClose.UseVisualStyleBackColor = false;
		this.btnClose.Click += new System.EventHandler(btnClose_Click);
		this.btnNext.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnNext.BackText = "";
		this.btnNext.BorderColor = System.Drawing.Color.Black;
		this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnNext.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnNext.LEDColor = System.Drawing.Color.Gray;
		this.btnNext.Location = new System.Drawing.Point(341, 1);
		this.btnNext.Name = "btnNext";
		this.btnNext.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnNext.Size = new System.Drawing.Size(46, 60);
		this.btnNext.TabIndex = 109;
		this.btnNext.Text = ">";
		this.btnNext.UseVisualStyleBackColor = false;
		this.btnNext.Click += new System.EventHandler(btnNext_Click);
		this.airPanel7.BorderColor = System.Drawing.Color.Black;
		this.airPanel7.BorderSize = 1f;
		this.airPanel7.Controls.Add(this.lblEq4);
		this.airPanel7.Controls.Add(this.eq1Q);
		this.airPanel7.Controls.Add(this.lblEq3);
		this.airPanel7.Controls.Add(this.eq2Q);
		this.airPanel7.Controls.Add(this.eq3Q);
		this.airPanel7.Controls.Add(this.lblEq2);
		this.airPanel7.Controls.Add(this.eq4Q);
		this.airPanel7.Controls.Add(this.eq1F);
		this.airPanel7.Controls.Add(this.eq2F);
		this.airPanel7.Controls.Add(this.lblEq1);
		this.airPanel7.Controls.Add(this.eq3F);
		this.airPanel7.Controls.Add(this.eq4F);
		this.airPanel7.EQColor = System.Drawing.Color.Lime;
		this.airPanel7.Location = new System.Drawing.Point(543, 613);
		this.airPanel7.Name = "airPanel7";
		this.airPanel7.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.airPanel7.PanelType = 0;
		this.airPanel7.Size = new System.Drawing.Size(199, 306);
		this.airPanel7.TabIndex = 51;
		this.airPanel7.Visible = false;
		this.lblEq4.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblEq4.Location = new System.Drawing.Point(4, 26);
		this.lblEq4.Name = "lblEq4";
		this.lblEq4.Size = new System.Drawing.Size(32, 23);
		this.lblEq4.TabIndex = 129;
		this.lblEq4.Tag = "High";
		this.lblEq4.Text = "Hi";
		this.lblEq4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.eq1Q.Active = false;
		this.eq1Q.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq1Q.Channel = 0;
		this.eq1Q.KnobLabel = "";
		this.eq1Q.KnobStyle = 0;
		this.eq1Q.KnobType = AirControls.Values.knobTypes.None;
		this.eq1Q.Location = new System.Drawing.Point(36, 223);
		this.eq1Q.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq1Q.Maximum = 40;
		this.eq1Q.Minimum = 0;
		this.eq1Q.Name = "eq1Q";
		this.eq1Q.Size = new System.Drawing.Size(51, 56);
		this.eq1Q.Steps = 1;
		this.eq1Q.TabIndex = 36;
		this.eq1Q.Tag = "Low";
		this.eq1Q.Value = 40;
		this.eq1Q.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq1Q.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq1Q.ValueList");
		this.eq1Q.ValueType = "q";
		this.eq1Q.Click += new System.EventHandler(eq_Click);
		this.lblEq3.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblEq3.Location = new System.Drawing.Point(5, 130);
		this.lblEq3.Name = "lblEq3";
		this.lblEq3.Size = new System.Drawing.Size(32, 23);
		this.lblEq3.TabIndex = 128;
		this.lblEq3.Tag = "HiMid";
		this.lblEq3.Text = "H-M";
		this.lblEq3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.eq2Q.Active = false;
		this.eq2Q.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq2Q.Channel = 0;
		this.eq2Q.KnobLabel = "";
		this.eq2Q.KnobStyle = 0;
		this.eq2Q.KnobType = AirControls.Values.knobTypes.None;
		this.eq2Q.Location = new System.Drawing.Point(36, 173);
		this.eq2Q.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq2Q.Maximum = 40;
		this.eq2Q.Minimum = 0;
		this.eq2Q.Name = "eq2Q";
		this.eq2Q.Size = new System.Drawing.Size(51, 56);
		this.eq2Q.Steps = 1;
		this.eq2Q.TabIndex = 37;
		this.eq2Q.Tag = "LowMid";
		this.eq2Q.Value = 40;
		this.eq2Q.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq2Q.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq2Q.ValueList");
		this.eq2Q.ValueType = "q";
		this.eq2Q.Click += new System.EventHandler(eq_Click);
		this.eq3Q.Active = false;
		this.eq3Q.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq3Q.Channel = 0;
		this.eq3Q.KnobLabel = "";
		this.eq3Q.KnobStyle = 0;
		this.eq3Q.KnobType = AirControls.Values.knobTypes.None;
		this.eq3Q.Location = new System.Drawing.Point(36, 123);
		this.eq3Q.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq3Q.Maximum = 40;
		this.eq3Q.Minimum = 0;
		this.eq3Q.Name = "eq3Q";
		this.eq3Q.Size = new System.Drawing.Size(51, 56);
		this.eq3Q.Steps = 1;
		this.eq3Q.TabIndex = 38;
		this.eq3Q.Tag = "HiMid";
		this.eq3Q.Value = 40;
		this.eq3Q.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq3Q.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq3Q.ValueList");
		this.eq3Q.ValueType = "q";
		this.eq3Q.Click += new System.EventHandler(eq_Click);
		this.lblEq2.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblEq2.Location = new System.Drawing.Point(5, 180);
		this.lblEq2.Name = "lblEq2";
		this.lblEq2.Size = new System.Drawing.Size(32, 23);
		this.lblEq2.TabIndex = 127;
		this.lblEq2.Tag = "LowMid";
		this.lblEq2.Text = "L-M";
		this.lblEq2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.eq4Q.Active = false;
		this.eq4Q.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq4Q.Channel = 0;
		this.eq4Q.KnobLabel = "";
		this.eq4Q.KnobStyle = 0;
		this.eq4Q.KnobType = AirControls.Values.knobTypes.None;
		this.eq4Q.Location = new System.Drawing.Point(36, 16);
		this.eq4Q.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq4Q.Maximum = 40;
		this.eq4Q.Minimum = 0;
		this.eq4Q.Name = "eq4Q";
		this.eq4Q.Size = new System.Drawing.Size(51, 56);
		this.eq4Q.Steps = 1;
		this.eq4Q.TabIndex = 39;
		this.eq4Q.Tag = "High";
		this.eq4Q.Value = 40;
		this.eq4Q.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq4Q.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq4Q.ValueList");
		this.eq4Q.ValueType = "q";
		this.eq4Q.Click += new System.EventHandler(eq_Click);
		this.eq1F.Active = false;
		this.eq1F.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq1F.Channel = 0;
		this.eq1F.KnobLabel = "";
		this.eq1F.KnobStyle = 1;
		this.eq1F.KnobType = AirControls.Values.knobTypes.None;
		this.eq1F.Location = new System.Drawing.Point(87, 223);
		this.eq1F.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq1F.Maximum = 124;
		this.eq1F.Minimum = 4;
		this.eq1F.Name = "eq1F";
		this.eq1F.Size = new System.Drawing.Size(51, 56);
		this.eq1F.Steps = 1;
		this.eq1F.TabIndex = 40;
		this.eq1F.Tag = "Low";
		this.eq1F.Value = 124;
		this.eq1F.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq1F.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq1F.ValueList");
		this.eq1F.ValueType = "freq";
		this.eq1F.Click += new System.EventHandler(eq_Click);
		this.eq2F.Active = false;
		this.eq2F.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq2F.Channel = 0;
		this.eq2F.KnobLabel = "";
		this.eq2F.KnobStyle = 1;
		this.eq2F.KnobType = AirControls.Values.knobTypes.None;
		this.eq2F.Location = new System.Drawing.Point(87, 173);
		this.eq2F.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq2F.Maximum = 124;
		this.eq2F.Minimum = 4;
		this.eq2F.Name = "eq2F";
		this.eq2F.Size = new System.Drawing.Size(51, 56);
		this.eq2F.Steps = 1;
		this.eq2F.TabIndex = 41;
		this.eq2F.Tag = "LowMid";
		this.eq2F.Value = 124;
		this.eq2F.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq2F.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq2F.ValueList");
		this.eq2F.ValueType = "freq";
		this.eq2F.Click += new System.EventHandler(eq_Click);
		this.lblEq1.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblEq1.Location = new System.Drawing.Point(5, 230);
		this.lblEq1.Name = "lblEq1";
		this.lblEq1.Size = new System.Drawing.Size(32, 23);
		this.lblEq1.TabIndex = 126;
		this.lblEq1.Tag = "Low";
		this.lblEq1.Text = "Lo";
		this.lblEq1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.eq3F.Active = false;
		this.eq3F.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq3F.Channel = 0;
		this.eq3F.KnobLabel = "";
		this.eq3F.KnobStyle = 1;
		this.eq3F.KnobType = AirControls.Values.knobTypes.None;
		this.eq3F.Location = new System.Drawing.Point(87, 123);
		this.eq3F.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq3F.Maximum = 124;
		this.eq3F.Minimum = 4;
		this.eq3F.Name = "eq3F";
		this.eq3F.Size = new System.Drawing.Size(51, 56);
		this.eq3F.Steps = 1;
		this.eq3F.TabIndex = 42;
		this.eq3F.Tag = "HiMid";
		this.eq3F.Value = 124;
		this.eq3F.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq3F.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq3F.ValueList");
		this.eq3F.ValueType = "freq";
		this.eq3F.Click += new System.EventHandler(eq_Click);
		this.eq4F.Active = false;
		this.eq4F.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq4F.Channel = 0;
		this.eq4F.KnobLabel = "";
		this.eq4F.KnobStyle = 1;
		this.eq4F.KnobType = AirControls.Values.knobTypes.None;
		this.eq4F.Location = new System.Drawing.Point(93, 16);
		this.eq4F.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
		this.eq4F.Maximum = 124;
		this.eq4F.Minimum = 4;
		this.eq4F.Name = "eq4F";
		this.eq4F.Size = new System.Drawing.Size(51, 56);
		this.eq4F.Steps = 1;
		this.eq4F.TabIndex = 43;
		this.eq4F.Tag = "High";
		this.eq4F.Value = 124;
		this.eq4F.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eq4F.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq4F.ValueList");
		this.eq4F.ValueType = "freq";
		this.eq4F.Click += new System.EventHandler(eq_Click);
		this.airPanel5.BorderColor = System.Drawing.Color.Black;
		this.airPanel5.BorderSize = 1f;
		this.airPanel5.Controls.Add(this.btnDirectOut);
		this.airPanel5.Controls.Add(this.btnInsertOn);
		this.airPanel5.Controls.Add(this.btnSafe);
		this.airPanel5.EQColor = System.Drawing.Color.Lime;
		this.airPanel5.Location = new System.Drawing.Point(229, 613);
		this.airPanel5.Name = "airPanel5";
		this.airPanel5.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.airPanel5.PanelType = 0;
		this.airPanel5.Size = new System.Drawing.Size(292, 103);
		this.airPanel5.TabIndex = 112;
		this.airPanel5.Visible = false;
		this.btnDirectOut.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDirectOut.BackText = "";
		this.btnDirectOut.BorderColor = System.Drawing.Color.Black;
		this.btnDirectOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDirectOut.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDirectOut.LEDColor = System.Drawing.Color.Gray;
		this.btnDirectOut.Location = new System.Drawing.Point(7, 20);
		this.btnDirectOut.Name = "btnDirectOut";
		this.btnDirectOut.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnDirectOut.Size = new System.Drawing.Size(70, 64);
		this.btnDirectOut.TabIndex = 99;
		this.btnDirectOut.Tag = "1";
		this.btnDirectOut.Text = "Direct\nOut";
		this.btnDirectOut.UseVisualStyleBackColor = false;
		this.btnDirectOut.Click += new System.EventHandler(btnDirectOut_Click);
		this.btnInsertOn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnInsertOn.BackText = "";
		this.btnInsertOn.BorderColor = System.Drawing.Color.Black;
		this.btnInsertOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnInsertOn.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnInsertOn.LEDColor = System.Drawing.Color.Gray;
		this.btnInsertOn.Location = new System.Drawing.Point(83, 20);
		this.btnInsertOn.Name = "btnInsertOn";
		this.btnInsertOn.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnInsertOn.Size = new System.Drawing.Size(70, 64);
		this.btnInsertOn.TabIndex = 98;
		this.btnInsertOn.Tag = "1";
		this.btnInsertOn.Text = "Insert\nOn";
		this.btnInsertOn.UseVisualStyleBackColor = false;
		this.btnInsertOn.Click += new System.EventHandler(btnInsertOn_Click);
		this.btnSafe.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSafe.BackText = "";
		this.btnSafe.BorderColor = System.Drawing.Color.Black;
		this.btnSafe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSafe.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSafe.LEDColor = System.Drawing.Color.Gray;
		this.btnSafe.Location = new System.Drawing.Point(201, 20);
		this.btnSafe.Name = "btnSafe";
		this.btnSafe.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSafe.Size = new System.Drawing.Size(70, 64);
		this.btnSafe.TabIndex = 100;
		this.btnSafe.Tag = "1";
		this.btnSafe.Text = "Safe";
		this.btnSafe.UseVisualStyleBackColor = false;
		this.btnSafe.Visible = false;
		this.tabSection.Controls.Add(this.tabDyn);
		this.tabSection.Controls.Add(this.tabEQ);
		this.tabSection.Controls.Add(this.tabSends);
		this.tabSection.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
		this.tabSection.Font = new System.Drawing.Font("Arial", 14f, System.Drawing.FontStyle.Bold);
		this.tabSection.ItemSize = new System.Drawing.Size(70, 55);
		this.tabSection.Location = new System.Drawing.Point(0, 0);
		this.tabSection.Multiline = true;
		this.tabSection.Name = "tabSection";
		this.tabSection.SelectedIndex = 0;
		this.tabSection.Size = new System.Drawing.Size(965, 600);
		this.tabSection.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
		this.tabSection.TabIndex = 89;
		this.tabDyn.Controls.Add(this.panelDCA);
		this.tabDyn.Controls.Add(this.panelHA);
		this.tabDyn.Controls.Add(this.panelDyn1);
		this.tabDyn.Controls.Add(this.panelMutes);
		this.tabDyn.Controls.Add(this.panelDyn0);
		this.tabDyn.Controls.Add(this.chanMute);
		this.tabDyn.Controls.Add(this.airFader1);
		this.tabDyn.Controls.Add(this.panelPAN);
		this.tabDyn.Controls.Add(this.airMeterPre);
		this.tabDyn.Controls.Add(this.airMeterPost);
		this.tabDyn.Controls.Add(this.chanFader);
		this.tabDyn.Location = new System.Drawing.Point(4, 59);
		this.tabDyn.Name = "tabDyn";
		this.tabDyn.Size = new System.Drawing.Size(957, 537);
		this.tabDyn.TabIndex = 2;
		this.tabDyn.Text = "Main";
		this.panelDCA.BorderColor = System.Drawing.Color.WhiteSmoke;
		this.panelDCA.BorderSize = 3f;
		this.panelDCA.Controls.Add(this.lblDCA);
		this.panelDCA.Controls.Add(this.btnDCAAssign0);
		this.panelDCA.Controls.Add(this.btnDCAAssign1);
		this.panelDCA.Controls.Add(this.btnDCAAssign2);
		this.panelDCA.Controls.Add(this.btnDCAAssign3);
		this.panelDCA.Controls.Add(this.btnDCAAssign4);
		this.panelDCA.Controls.Add(this.btnDCAAssign5);
		this.panelDCA.Controls.Add(this.btnDCAAssign6);
		this.panelDCA.Controls.Add(this.btnDCAAssign7);
		this.panelDCA.EQColor = System.Drawing.Color.Lime;
		this.panelDCA.Location = new System.Drawing.Point(440, 302);
		this.panelDCA.Name = "panelDCA";
		this.panelDCA.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.panelDCA.PanelType = 2;
		this.panelDCA.Size = new System.Drawing.Size(219, 158);
		this.panelDCA.TabIndex = 117;
		this.lblDCA.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblDCA.Location = new System.Drawing.Point(86, 3);
		this.lblDCA.Name = "lblDCA";
		this.lblDCA.Size = new System.Drawing.Size(136, 23);
		this.lblDCA.TabIndex = 101;
		this.lblDCA.Text = "DCAs";
		this.lblDCA.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.btnDCAAssign0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign0.BackText = "";
		this.btnDCAAssign0.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign0.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign0.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign0.Location = new System.Drawing.Point(3, 26);
		this.btnDCAAssign0.Name = "btnDCAAssign0";
		this.btnDCAAssign0.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign0.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign0.TabIndex = 102;
		this.btnDCAAssign0.Tag = "0";
		this.btnDCAAssign0.Text = "1";
		this.btnDCAAssign0.UseVisualStyleBackColor = false;
		this.btnDCAAssign0.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign1.BackText = "";
		this.btnDCAAssign1.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign1.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign1.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign1.Location = new System.Drawing.Point(59, 26);
		this.btnDCAAssign1.Name = "btnDCAAssign1";
		this.btnDCAAssign1.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign1.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign1.TabIndex = 103;
		this.btnDCAAssign1.Tag = "1";
		this.btnDCAAssign1.Text = "2";
		this.btnDCAAssign1.UseVisualStyleBackColor = false;
		this.btnDCAAssign1.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign2.BackText = "";
		this.btnDCAAssign2.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign2.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign2.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign2.Location = new System.Drawing.Point(115, 26);
		this.btnDCAAssign2.Name = "btnDCAAssign2";
		this.btnDCAAssign2.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign2.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign2.TabIndex = 104;
		this.btnDCAAssign2.Tag = "2";
		this.btnDCAAssign2.Text = "3";
		this.btnDCAAssign2.UseVisualStyleBackColor = false;
		this.btnDCAAssign2.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign3.BackText = "";
		this.btnDCAAssign3.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign3.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign3.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign3.Location = new System.Drawing.Point(171, 26);
		this.btnDCAAssign3.Name = "btnDCAAssign3";
		this.btnDCAAssign3.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign3.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign3.TabIndex = 105;
		this.btnDCAAssign3.Tag = "3";
		this.btnDCAAssign3.Text = "4";
		this.btnDCAAssign3.UseVisualStyleBackColor = false;
		this.btnDCAAssign3.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign4.BackText = "";
		this.btnDCAAssign4.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign4.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign4.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign4.Location = new System.Drawing.Point(3, 87);
		this.btnDCAAssign4.Name = "btnDCAAssign4";
		this.btnDCAAssign4.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign4.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign4.TabIndex = 106;
		this.btnDCAAssign4.Tag = "4";
		this.btnDCAAssign4.Text = "5";
		this.btnDCAAssign4.UseVisualStyleBackColor = false;
		this.btnDCAAssign4.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign5.BackText = "";
		this.btnDCAAssign5.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign5.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign5.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign5.Location = new System.Drawing.Point(59, 87);
		this.btnDCAAssign5.Name = "btnDCAAssign5";
		this.btnDCAAssign5.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign5.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign5.TabIndex = 107;
		this.btnDCAAssign5.Tag = "5";
		this.btnDCAAssign5.Text = "6";
		this.btnDCAAssign5.UseVisualStyleBackColor = false;
		this.btnDCAAssign5.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign6.BackText = "";
		this.btnDCAAssign6.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign6.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign6.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign6.Location = new System.Drawing.Point(115, 87);
		this.btnDCAAssign6.Name = "btnDCAAssign6";
		this.btnDCAAssign6.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign6.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign6.TabIndex = 108;
		this.btnDCAAssign6.Tag = "6";
		this.btnDCAAssign6.Text = "7";
		this.btnDCAAssign6.UseVisualStyleBackColor = false;
		this.btnDCAAssign6.Click += new System.EventHandler(btnDCAAssign_Click);
		this.btnDCAAssign7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDCAAssign7.BackText = "";
		this.btnDCAAssign7.BorderColor = System.Drawing.Color.Black;
		this.btnDCAAssign7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDCAAssign7.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDCAAssign7.LEDColor = System.Drawing.Color.Gray;
		this.btnDCAAssign7.Location = new System.Drawing.Point(171, 87);
		this.btnDCAAssign7.Name = "btnDCAAssign7";
		this.btnDCAAssign7.ParentColor = System.Drawing.Color.FromArgb(50, 50, 0);
		this.btnDCAAssign7.Size = new System.Drawing.Size(50, 55);
		this.btnDCAAssign7.TabIndex = 109;
		this.btnDCAAssign7.Tag = "7";
		this.btnDCAAssign7.Text = "8";
		this.btnDCAAssign7.UseVisualStyleBackColor = false;
		this.btnDCAAssign7.Click += new System.EventHandler(btnDCAAssign_Click);
		this.panelHA.BorderColor = System.Drawing.Color.Black;
		this.panelHA.BorderSize = 1f;
		this.panelHA.Controls.Add(this.knobHA);
		this.panelHA.Controls.Add(this.chanPhantom);
		this.panelHA.Controls.Add(this.chanPhase);
		this.panelHA.EQColor = System.Drawing.Color.Lime;
		this.panelHA.Location = new System.Drawing.Point(10, 5);
		this.panelHA.Name = "panelHA";
		this.panelHA.Padding = new System.Windows.Forms.Padding(4, 18, 4, 18);
		this.panelHA.PanelType = 0;
		this.panelHA.Size = new System.Drawing.Size(95, 290);
		this.panelHA.TabIndex = 113;
		this.knobHA.Active = false;
		this.knobHA.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.knobHA.Channel = 0;
		this.knobHA.KnobLabel = "HA";
		this.knobHA.KnobStyle = 0;
		this.knobHA.KnobType = AirControls.Values.knobTypes.None;
		this.knobHA.Location = new System.Drawing.Point(13, 22);
		this.knobHA.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.knobHA.Maximum = 72;
		this.knobHA.Minimum = 0;
		this.knobHA.Name = "knobHA";
		this.knobHA.Size = new System.Drawing.Size(68, 95);
		this.knobHA.Steps = 1;
		this.knobHA.TabIndex = 84;
		this.knobHA.Tag = "None";
		this.knobHA.Value = 72;
		this.knobHA.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.knobHA.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("knobHA.ValueList");
		this.knobHA.ValueType = "ha";
		this.knobHA.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.knobHA.Click += new System.EventHandler(airKnob_Click);
		this.knobHA.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.chanPhantom.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanPhantom.BackText = "";
		this.chanPhantom.BorderColor = System.Drawing.Color.Black;
		this.chanPhantom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.chanPhantom.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.chanPhantom.LEDColor = System.Drawing.Color.Gray;
		this.chanPhantom.Location = new System.Drawing.Point(13, 135);
		this.chanPhantom.Name = "chanPhantom";
		this.chanPhantom.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.chanPhantom.Size = new System.Drawing.Size(70, 64);
		this.chanPhantom.TabIndex = 82;
		this.chanPhantom.Tag = "1";
		this.chanPhantom.Text = "+48v";
		this.chanPhantom.UseVisualStyleBackColor = false;
		this.chanPhantom.Click += new System.EventHandler(chanPhantom_Click);
		this.chanPhase.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanPhase.BackText = "";
		this.chanPhase.BorderColor = System.Drawing.Color.Black;
		this.chanPhase.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.chanPhase.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.chanPhase.LEDColor = System.Drawing.Color.Gray;
		this.chanPhase.Location = new System.Drawing.Point(13, 205);
		this.chanPhase.Name = "chanPhase";
		this.chanPhase.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.chanPhase.Size = new System.Drawing.Size(70, 64);
		this.chanPhase.TabIndex = 83;
		this.chanPhase.Tag = "1";
		this.chanPhase.Text = "Phase";
		this.chanPhase.UseVisualStyleBackColor = false;
		this.chanPhase.Click += new System.EventHandler(chanPhase_Click);
		this.panelDyn1.BorderColor = System.Drawing.Color.FromArgb(224, 224, 224);
		this.panelDyn1.BorderSize = 3f;
		this.panelDyn1.Controls.Add(this.meterDyn1Gain);
		this.panelDyn1.Controls.Add(this.meterDyn1In);
		this.panelDyn1.Controls.Add(this.btnDyn1Type);
		this.panelDyn1.Controls.Add(this.btnDyn1On);
		this.panelDyn1.Controls.Add(this.dyn10);
		this.panelDyn1.Controls.Add(this.dyn11);
		this.panelDyn1.Controls.Add(this.dyn12);
		this.panelDyn1.Controls.Add(this.dyn13);
		this.panelDyn1.Controls.Add(this.dyn14);
		this.panelDyn1.Controls.Add(this.dyn15);
		this.panelDyn1.EQColor = System.Drawing.Color.Lime;
		this.panelDyn1.Location = new System.Drawing.Point(110, 154);
		this.panelDyn1.Name = "panelDyn1";
		this.panelDyn1.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.panelDyn1.PanelType = 0;
		this.panelDyn1.Size = new System.Drawing.Size(549, 142);
		this.panelDyn1.TabIndex = 111;
		this.meterDyn1Gain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.meterDyn1Gain.BarColor = System.Drawing.Color.Coral;
		this.meterDyn1Gain.BarStyle = 11;
		this.meterDyn1Gain.BorderColor = System.Drawing.Color.DimGray;
		this.meterDyn1Gain.Channel = 0;
		this.meterDyn1Gain.Location = new System.Drawing.Point(314, 15);
		this.meterDyn1Gain.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.meterDyn1Gain.Maximum = 76;
		this.meterDyn1Gain.Minimum = 0;
		this.meterDyn1Gain.Name = "meterDyn1Gain";
		this.meterDyn1Gain.Size = new System.Drawing.Size(216, 18);
		this.meterDyn1Gain.TabIndex = 111;
		this.meterDyn1Gain.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.meterDyn1Gain.Value = new decimal(new int[4] { 4, 0, 0, 0 });
		this.meterDyn1Gain.ValueDB = new decimal(new int[4] { 5960, 0, 0, -2147352576 });
		this.meterDyn1In.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.meterDyn1In.BarColor = System.Drawing.Color.Lime;
		this.meterDyn1In.BarStyle = 10;
		this.meterDyn1In.BorderColor = System.Drawing.Color.DimGray;
		this.meterDyn1In.Channel = 0;
		this.meterDyn1In.Location = new System.Drawing.Point(93, 15);
		this.meterDyn1In.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.meterDyn1In.Maximum = 127;
		this.meterDyn1In.Minimum = 0;
		this.meterDyn1In.Name = "meterDyn1In";
		this.meterDyn1In.Size = new System.Drawing.Size(216, 18);
		this.meterDyn1In.TabIndex = 110;
		this.meterDyn1In.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.meterDyn1In.Value = new decimal(new int[4] { 5, 0, 0, 0 });
		this.meterDyn1In.ValueDB = new decimal(new int[4] { 7780, 0, 0, -2147352576 });
		this.btnDyn1Type.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDyn1Type.BackText = "";
		this.btnDyn1Type.BorderColor = System.Drawing.Color.Black;
		this.btnDyn1Type.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDyn1Type.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDyn1Type.LEDColor = System.Drawing.Color.Gray;
		this.btnDyn1Type.Location = new System.Drawing.Point(20, 5);
		this.btnDyn1Type.Name = "btnDyn1Type";
		this.btnDyn1Type.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnDyn1Type.Size = new System.Drawing.Size(70, 64);
		this.btnDyn1Type.TabIndex = 84;
		this.btnDyn1Type.Tag = "1";
		this.btnDyn1Type.Text = "Comp";
		this.btnDyn1Type.UseVisualStyleBackColor = false;
		this.btnDyn1Type.Click += new System.EventHandler(btnDyn1Type_Click);
		this.btnDyn1On.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDyn1On.BackText = "";
		this.btnDyn1On.BorderColor = System.Drawing.Color.Black;
		this.btnDyn1On.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDyn1On.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDyn1On.LEDColor = System.Drawing.Color.Gray;
		this.btnDyn1On.Location = new System.Drawing.Point(20, 71);
		this.btnDyn1On.Name = "btnDyn1On";
		this.btnDyn1On.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnDyn1On.Size = new System.Drawing.Size(70, 64);
		this.btnDyn1On.TabIndex = 107;
		this.btnDyn1On.Tag = "1";
		this.btnDyn1On.Text = "On";
		this.btnDyn1On.UseVisualStyleBackColor = false;
		this.btnDyn1On.Click += new System.EventHandler(btnDyn1On_Click);
		this.dyn10.Active = false;
		this.dyn10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn10.Channel = 0;
		this.dyn10.KnobLabel = "Threshold";
		this.dyn10.KnobStyle = 1;
		this.dyn10.KnobType = AirControls.Values.knobTypes.None;
		this.dyn10.Location = new System.Drawing.Point(93, 40);
		this.dyn10.Maximum = 0;
		this.dyn10.Minimum = -540;
		this.dyn10.Name = "dyn10";
		this.dyn10.Size = new System.Drawing.Size(68, 95);
		this.dyn10.Steps = 10;
		this.dyn10.TabIndex = 5;
		this.dyn10.Value = 0;
		this.dyn10.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.dyn10.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn10.ValueList");
		this.dyn10.ValueType = "div10";
		this.dyn10.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn10.Click += new System.EventHandler(airKnob_Click);
		this.dyn10.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn11.Active = false;
		this.dyn11.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn11.Channel = 0;
		this.dyn11.KnobLabel = "Range";
		this.dyn11.KnobStyle = 0;
		this.dyn11.KnobType = AirControls.Values.knobTypes.None;
		this.dyn11.Location = new System.Drawing.Point(167, 40);
		this.dyn11.Maximum = 100;
		this.dyn11.Minimum = 0;
		this.dyn11.Name = "dyn11";
		this.dyn11.Size = new System.Drawing.Size(68, 95);
		this.dyn11.Steps = 1;
		this.dyn11.TabIndex = 6;
		this.dyn11.Value = 100;
		this.dyn11.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn11.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn11.ValueList");
		this.dyn11.ValueType = "fader";
		this.dyn11.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn11.Click += new System.EventHandler(airKnob_Click);
		this.dyn11.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn12.Active = false;
		this.dyn12.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn12.Channel = 0;
		this.dyn12.KnobLabel = "Attack";
		this.dyn12.KnobStyle = 0;
		this.dyn12.KnobType = AirControls.Values.knobTypes.None;
		this.dyn12.Location = new System.Drawing.Point(241, 40);
		this.dyn12.Maximum = 100;
		this.dyn12.Minimum = 0;
		this.dyn12.Name = "dyn12";
		this.dyn12.Size = new System.Drawing.Size(68, 95);
		this.dyn12.Steps = 1;
		this.dyn12.TabIndex = 7;
		this.dyn12.Value = 100;
		this.dyn12.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn12.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn12.ValueList");
		this.dyn12.ValueType = "fader";
		this.dyn12.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn12.Click += new System.EventHandler(airKnob_Click);
		this.dyn12.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn13.Active = false;
		this.dyn13.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn13.Channel = 0;
		this.dyn13.KnobLabel = "Hold";
		this.dyn13.KnobStyle = 0;
		this.dyn13.KnobType = AirControls.Values.knobTypes.None;
		this.dyn13.Location = new System.Drawing.Point(315, 40);
		this.dyn13.Maximum = 100;
		this.dyn13.Minimum = 0;
		this.dyn13.Name = "dyn13";
		this.dyn13.Size = new System.Drawing.Size(68, 95);
		this.dyn13.Steps = 1;
		this.dyn13.TabIndex = 8;
		this.dyn13.Value = 100;
		this.dyn13.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn13.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn13.ValueList");
		this.dyn13.ValueType = "fader";
		this.dyn13.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn13.Click += new System.EventHandler(airKnob_Click);
		this.dyn13.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn14.Active = false;
		this.dyn14.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn14.Channel = 0;
		this.dyn14.KnobLabel = "Decay";
		this.dyn14.KnobStyle = 0;
		this.dyn14.KnobType = AirControls.Values.knobTypes.None;
		this.dyn14.Location = new System.Drawing.Point(389, 40);
		this.dyn14.Maximum = 100;
		this.dyn14.Minimum = 0;
		this.dyn14.Name = "dyn14";
		this.dyn14.Size = new System.Drawing.Size(68, 95);
		this.dyn14.Steps = 1;
		this.dyn14.TabIndex = 9;
		this.dyn14.Value = 100;
		this.dyn14.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn14.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn14.ValueList");
		this.dyn14.ValueType = "fader";
		this.dyn14.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn14.Click += new System.EventHandler(airKnob_Click);
		this.dyn14.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn15.Active = false;
		this.dyn15.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn15.Channel = 0;
		this.dyn15.KnobLabel = "None";
		this.dyn15.KnobStyle = 0;
		this.dyn15.KnobType = AirControls.Values.knobTypes.None;
		this.dyn15.Location = new System.Drawing.Point(463, 40);
		this.dyn15.Maximum = 100;
		this.dyn15.Minimum = 0;
		this.dyn15.Name = "dyn15";
		this.dyn15.Size = new System.Drawing.Size(68, 95);
		this.dyn15.Steps = 1;
		this.dyn15.TabIndex = 10;
		this.dyn15.Tag = "None";
		this.dyn15.Value = 100;
		this.dyn15.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn15.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn15.ValueList");
		this.dyn15.ValueType = "fader";
		this.dyn15.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn15.Click += new System.EventHandler(airKnob_Click);
		this.dyn15.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.panelMutes.BorderColor = System.Drawing.Color.WhiteSmoke;
		this.panelMutes.BorderSize = 3f;
		this.panelMutes.Controls.Add(this.lblMuteGroups);
		this.panelMutes.Controls.Add(this.btnMuteAssign0);
		this.panelMutes.Controls.Add(this.btnMuteAssign1);
		this.panelMutes.Controls.Add(this.btnMuteAssign2);
		this.panelMutes.Controls.Add(this.btnMuteAssign3);
		this.panelMutes.Controls.Add(this.btnMuteAssign4);
		this.panelMutes.Controls.Add(this.btnMuteAssign5);
		this.panelMutes.Controls.Add(this.btnMuteAssign6);
		this.panelMutes.Controls.Add(this.btnMuteAssign7);
		this.panelMutes.EQColor = System.Drawing.Color.Lime;
		this.panelMutes.Location = new System.Drawing.Point(206, 302);
		this.panelMutes.Name = "panelMutes";
		this.panelMutes.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.panelMutes.PanelType = 2;
		this.panelMutes.Size = new System.Drawing.Size(228, 158);
		this.panelMutes.TabIndex = 109;
		this.lblMuteGroups.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblMuteGroups.Location = new System.Drawing.Point(86, 3);
		this.lblMuteGroups.Name = "lblMuteGroups";
		this.lblMuteGroups.Size = new System.Drawing.Size(136, 23);
		this.lblMuteGroups.TabIndex = 101;
		this.lblMuteGroups.Text = "Mute Groups";
		this.lblMuteGroups.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.btnMuteAssign0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign0.BackText = "";
		this.btnMuteAssign0.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign0.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign0.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign0.Location = new System.Drawing.Point(5, 26);
		this.btnMuteAssign0.Name = "btnMuteAssign0";
		this.btnMuteAssign0.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign0.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign0.TabIndex = 90;
		this.btnMuteAssign0.Tag = "0";
		this.btnMuteAssign0.Text = "1";
		this.btnMuteAssign0.UseVisualStyleBackColor = false;
		this.btnMuteAssign0.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign1.BackText = "";
		this.btnMuteAssign1.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign1.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign1.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign1.Location = new System.Drawing.Point(61, 26);
		this.btnMuteAssign1.Name = "btnMuteAssign1";
		this.btnMuteAssign1.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign1.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign1.TabIndex = 91;
		this.btnMuteAssign1.Tag = "1";
		this.btnMuteAssign1.Text = "2";
		this.btnMuteAssign1.UseVisualStyleBackColor = false;
		this.btnMuteAssign1.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign2.BackText = "";
		this.btnMuteAssign2.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign2.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign2.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign2.Location = new System.Drawing.Point(117, 26);
		this.btnMuteAssign2.Name = "btnMuteAssign2";
		this.btnMuteAssign2.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign2.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign2.TabIndex = 92;
		this.btnMuteAssign2.Tag = "2";
		this.btnMuteAssign2.Text = "3";
		this.btnMuteAssign2.UseVisualStyleBackColor = false;
		this.btnMuteAssign2.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign3.BackText = "";
		this.btnMuteAssign3.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign3.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign3.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign3.Location = new System.Drawing.Point(173, 26);
		this.btnMuteAssign3.Name = "btnMuteAssign3";
		this.btnMuteAssign3.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign3.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign3.TabIndex = 93;
		this.btnMuteAssign3.Tag = "3";
		this.btnMuteAssign3.Text = "4";
		this.btnMuteAssign3.UseVisualStyleBackColor = false;
		this.btnMuteAssign3.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign4.BackText = "";
		this.btnMuteAssign4.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign4.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign4.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign4.Location = new System.Drawing.Point(5, 87);
		this.btnMuteAssign4.Name = "btnMuteAssign4";
		this.btnMuteAssign4.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign4.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign4.TabIndex = 94;
		this.btnMuteAssign4.Tag = "4";
		this.btnMuteAssign4.Text = "5";
		this.btnMuteAssign4.UseVisualStyleBackColor = false;
		this.btnMuteAssign4.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign5.BackText = "";
		this.btnMuteAssign5.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign5.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign5.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign5.Location = new System.Drawing.Point(61, 87);
		this.btnMuteAssign5.Name = "btnMuteAssign5";
		this.btnMuteAssign5.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign5.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign5.TabIndex = 95;
		this.btnMuteAssign5.Tag = "5";
		this.btnMuteAssign5.Text = "6";
		this.btnMuteAssign5.UseVisualStyleBackColor = false;
		this.btnMuteAssign5.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign6.BackText = "";
		this.btnMuteAssign6.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign6.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign6.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign6.Location = new System.Drawing.Point(117, 87);
		this.btnMuteAssign6.Name = "btnMuteAssign6";
		this.btnMuteAssign6.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign6.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign6.TabIndex = 96;
		this.btnMuteAssign6.Tag = "6";
		this.btnMuteAssign6.Text = "7";
		this.btnMuteAssign6.UseVisualStyleBackColor = false;
		this.btnMuteAssign6.Click += new System.EventHandler(btnMuteAssign_Click);
		this.btnMuteAssign7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnMuteAssign7.BackText = "";
		this.btnMuteAssign7.BorderColor = System.Drawing.Color.Black;
		this.btnMuteAssign7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnMuteAssign7.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnMuteAssign7.LEDColor = System.Drawing.Color.Gray;
		this.btnMuteAssign7.Location = new System.Drawing.Point(173, 87);
		this.btnMuteAssign7.Name = "btnMuteAssign7";
		this.btnMuteAssign7.ParentColor = System.Drawing.Color.FromArgb(50, 0, 0);
		this.btnMuteAssign7.Size = new System.Drawing.Size(50, 55);
		this.btnMuteAssign7.TabIndex = 97;
		this.btnMuteAssign7.Tag = "7";
		this.btnMuteAssign7.Text = "8";
		this.btnMuteAssign7.UseVisualStyleBackColor = false;
		this.btnMuteAssign7.Click += new System.EventHandler(btnMuteAssign_Click);
		this.panelDyn0.BorderColor = System.Drawing.Color.FromArgb(224, 224, 224);
		this.panelDyn0.BorderSize = 3f;
		this.panelDyn0.Controls.Add(this.meterDyn0Gain);
		this.panelDyn0.Controls.Add(this.meterDyn0In);
		this.panelDyn0.Controls.Add(this.btnDyn0Type);
		this.panelDyn0.Controls.Add(this.btnDyn0On);
		this.panelDyn0.Controls.Add(this.dyn00);
		this.panelDyn0.Controls.Add(this.dyn01);
		this.panelDyn0.Controls.Add(this.dyn02);
		this.panelDyn0.Controls.Add(this.dyn03);
		this.panelDyn0.Controls.Add(this.dyn04);
		this.panelDyn0.Controls.Add(this.dyn05);
		this.panelDyn0.EQColor = System.Drawing.Color.Lime;
		this.panelDyn0.Location = new System.Drawing.Point(110, 5);
		this.panelDyn0.Name = "panelDyn0";
		this.panelDyn0.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.panelDyn0.PanelType = 0;
		this.panelDyn0.Size = new System.Drawing.Size(549, 142);
		this.panelDyn0.TabIndex = 107;
		this.meterDyn0Gain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.meterDyn0Gain.BarColor = System.Drawing.Color.Coral;
		this.meterDyn0Gain.BarStyle = 11;
		this.meterDyn0Gain.BorderColor = System.Drawing.Color.DimGray;
		this.meterDyn0Gain.Channel = 0;
		this.meterDyn0Gain.Location = new System.Drawing.Point(314, 13);
		this.meterDyn0Gain.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.meterDyn0Gain.Maximum = 76;
		this.meterDyn0Gain.Minimum = 0;
		this.meterDyn0Gain.Name = "meterDyn0Gain";
		this.meterDyn0Gain.Size = new System.Drawing.Size(216, 18);
		this.meterDyn0Gain.TabIndex = 109;
		this.meterDyn0Gain.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.meterDyn0Gain.Value = new decimal(new int[4] { 4, 0, 0, 0 });
		this.meterDyn0Gain.ValueDB = new decimal(new int[4] { 5960, 0, 0, -2147352576 });
		this.meterDyn0In.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.meterDyn0In.BarColor = System.Drawing.Color.Lime;
		this.meterDyn0In.BarStyle = 10;
		this.meterDyn0In.BorderColor = System.Drawing.Color.DimGray;
		this.meterDyn0In.Channel = 0;
		this.meterDyn0In.Location = new System.Drawing.Point(93, 13);
		this.meterDyn0In.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.meterDyn0In.Maximum = 127;
		this.meterDyn0In.Minimum = 0;
		this.meterDyn0In.Name = "meterDyn0In";
		this.meterDyn0In.Size = new System.Drawing.Size(216, 18);
		this.meterDyn0In.TabIndex = 108;
		this.meterDyn0In.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.meterDyn0In.Value = new decimal(new int[4] { 5, 0, 0, 0 });
		this.meterDyn0In.ValueDB = new decimal(new int[4] { 7780, 0, 0, -2147352576 });
		this.btnDyn0Type.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDyn0Type.BackText = "";
		this.btnDyn0Type.BorderColor = System.Drawing.Color.Black;
		this.btnDyn0Type.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDyn0Type.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDyn0Type.LEDColor = System.Drawing.Color.Gray;
		this.btnDyn0Type.Location = new System.Drawing.Point(20, 5);
		this.btnDyn0Type.Name = "btnDyn0Type";
		this.btnDyn0Type.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnDyn0Type.Size = new System.Drawing.Size(70, 64);
		this.btnDyn0Type.TabIndex = 84;
		this.btnDyn0Type.Tag = "1";
		this.btnDyn0Type.Text = "Gate";
		this.btnDyn0Type.UseVisualStyleBackColor = false;
		this.btnDyn0Type.Click += new System.EventHandler(btnDyn0Type_Click);
		this.btnDyn0On.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnDyn0On.BackText = "";
		this.btnDyn0On.BorderColor = System.Drawing.Color.Black;
		this.btnDyn0On.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnDyn0On.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnDyn0On.LEDColor = System.Drawing.Color.Gray;
		this.btnDyn0On.Location = new System.Drawing.Point(20, 71);
		this.btnDyn0On.Name = "btnDyn0On";
		this.btnDyn0On.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnDyn0On.Size = new System.Drawing.Size(70, 64);
		this.btnDyn0On.TabIndex = 107;
		this.btnDyn0On.Tag = "1";
		this.btnDyn0On.Text = "On";
		this.btnDyn0On.UseVisualStyleBackColor = false;
		this.btnDyn0On.Click += new System.EventHandler(btnDyn0On_Click);
		this.dyn00.Active = false;
		this.dyn00.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn00.Channel = 0;
		this.dyn00.KnobLabel = "Threshold";
		this.dyn00.KnobStyle = 1;
		this.dyn00.KnobType = AirControls.Values.knobTypes.None;
		this.dyn00.Location = new System.Drawing.Point(93, 38);
		this.dyn00.Maximum = 0;
		this.dyn00.Minimum = -540;
		this.dyn00.Name = "dyn00";
		this.dyn00.Size = new System.Drawing.Size(68, 95);
		this.dyn00.Steps = 10;
		this.dyn00.TabIndex = 5;
		this.dyn00.Value = 0;
		this.dyn00.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.dyn00.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn00.ValueList");
		this.dyn00.ValueType = "div10";
		this.dyn00.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn00.Click += new System.EventHandler(airKnob_Click);
		this.dyn00.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn01.Active = false;
		this.dyn01.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn01.Channel = 0;
		this.dyn01.KnobLabel = "Range";
		this.dyn01.KnobStyle = 0;
		this.dyn01.KnobType = AirControls.Values.knobTypes.None;
		this.dyn01.Location = new System.Drawing.Point(167, 38);
		this.dyn01.Maximum = 0;
		this.dyn01.Minimum = -70;
		this.dyn01.Name = "dyn01";
		this.dyn01.Size = new System.Drawing.Size(68, 95);
		this.dyn01.Steps = 1;
		this.dyn01.TabIndex = 6;
		this.dyn01.Value = 0;
		this.dyn01.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.dyn01.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn01.ValueList");
		this.dyn01.ValueType = "fader";
		this.dyn01.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn01.Click += new System.EventHandler(airKnob_Click);
		this.dyn01.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn02.Active = false;
		this.dyn02.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn02.Channel = 0;
		this.dyn02.KnobLabel = "Attack";
		this.dyn02.KnobStyle = 0;
		this.dyn02.KnobType = AirControls.Values.knobTypes.None;
		this.dyn02.Location = new System.Drawing.Point(241, 38);
		this.dyn02.Maximum = 120;
		this.dyn02.Minimum = 0;
		this.dyn02.Name = "dyn02";
		this.dyn02.Size = new System.Drawing.Size(68, 95);
		this.dyn02.Steps = 1;
		this.dyn02.TabIndex = 7;
		this.dyn02.Value = 120;
		this.dyn02.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn02.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn02.ValueList");
		this.dyn02.ValueType = "ms";
		this.dyn02.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn02.Click += new System.EventHandler(airKnob_Click);
		this.dyn02.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn03.Active = false;
		this.dyn03.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn03.Channel = 0;
		this.dyn03.KnobLabel = "Hold";
		this.dyn03.KnobStyle = 0;
		this.dyn03.KnobType = AirControls.Values.knobTypes.None;
		this.dyn03.Location = new System.Drawing.Point(315, 38);
		this.dyn03.Maximum = 215;
		this.dyn03.Minimum = 0;
		this.dyn03.Name = "dyn03";
		this.dyn03.Size = new System.Drawing.Size(68, 95);
		this.dyn03.Steps = 1;
		this.dyn03.TabIndex = 8;
		this.dyn03.Value = 215;
		this.dyn03.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn03.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn03.ValueList");
		this.dyn03.ValueType = "holdtime44";
		this.dyn03.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn03.Click += new System.EventHandler(airKnob_Click);
		this.dyn03.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn04.Active = false;
		this.dyn04.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn04.Channel = 0;
		this.dyn04.KnobLabel = "Decay";
		this.dyn04.KnobStyle = 0;
		this.dyn04.KnobType = AirControls.Values.knobTypes.None;
		this.dyn04.Location = new System.Drawing.Point(389, 38);
		this.dyn04.Maximum = 159;
		this.dyn04.Minimum = 0;
		this.dyn04.Name = "dyn04";
		this.dyn04.Size = new System.Drawing.Size(68, 95);
		this.dyn04.Steps = 1;
		this.dyn04.TabIndex = 9;
		this.dyn04.Value = 159;
		this.dyn04.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.dyn04.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn04.ValueList");
		this.dyn04.ValueType = "decay44";
		this.dyn04.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn04.Click += new System.EventHandler(airKnob_Click);
		this.dyn04.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.dyn05.Active = false;
		this.dyn05.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.dyn05.Channel = 0;
		this.dyn05.KnobLabel = "None";
		this.dyn05.KnobStyle = 0;
		this.dyn05.KnobType = AirControls.Values.knobTypes.None;
		this.dyn05.Location = new System.Drawing.Point(463, 38);
		this.dyn05.Maximum = 100;
		this.dyn05.Minimum = 0;
		this.dyn05.Name = "dyn05";
		this.dyn05.Size = new System.Drawing.Size(68, 95);
		this.dyn05.Steps = 1;
		this.dyn05.TabIndex = 10;
		this.dyn05.Tag = "None";
		this.dyn05.Value = 0;
		this.dyn05.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.dyn05.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("dyn05.ValueList");
		this.dyn05.ValueType = "fader";
		this.dyn05.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.dyn05.Click += new System.EventHandler(airKnob_Click);
		this.dyn05.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.chanMute.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanMute.BackText = "";
		this.chanMute.BorderColor = System.Drawing.Color.Black;
		this.chanMute.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.chanMute.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.chanMute.LEDColor = System.Drawing.Color.Gray;
		this.chanMute.Location = new System.Drawing.Point(723, 11);
		this.chanMute.Name = "chanMute";
		this.chanMute.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.chanMute.Size = new System.Drawing.Size(66, 39);
		this.chanMute.TabIndex = 116;
		this.chanMute.Text = "On";
		this.chanMute.UseVisualStyleBackColor = false;
		this.chanMute.Click += new System.EventHandler(chanMute_Click);
		this.airFader1.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.airFader1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.airFader1.Channel = 0;
		this.airFader1.FaderColor = "Blue";
		this.airFader1.FaderStyle = 23;
		this.airFader1.FaderTails = false;
		this.airFader1.FineMode = false;
		this.airFader1.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.airFader1.Horizontal = true;
		this.airFader1.IsActive = false;
		this.airFader1.KnobColor = "White";
		this.airFader1.LabelType = "fader";
		this.airFader1.Location = new System.Drawing.Point(8, 466);
		this.airFader1.Maximum = 0;
		this.airFader1.MeterLevel = 0;
		this.airFader1.MeterType = "side";
		this.airFader1.Minimum = -540;
		this.airFader1.Name = "airFader1";
		this.airFader1.ShowKnob = false;
		this.airFader1.ShowMeter = false;
		this.airFader1.Size = new System.Drawing.Size(651, 62);
		this.airFader1.Steps = 5;
		this.airFader1.TabIndex = 17;
		this.airFader1.TailColor = System.Drawing.Color.Coral;
		this.airFader1.TickType = "fader";
		this.airFader1.Value = 0;
		AirControls.AirFader airFader = this.airFader1;
		int[] bits = new int[4];
		airFader.ValueDB = new decimal(bits);
		this.airFader1.ValueInt = 0;
		this.airFader1.ValueType = "div10";
		this.airFader1.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(airFader1_FaderChanged);
		this.panelPAN.BorderColor = System.Drawing.Color.WhiteSmoke;
		this.panelPAN.BorderSize = 1f;
		this.panelPAN.Controls.Add(this.chanPan);
		this.panelPAN.Controls.Add(this.lblParam);
		this.panelPAN.Controls.Add(this.btnToMonoOn);
		this.panelPAN.Controls.Add(this.btnToStereoOn);
		this.panelPAN.Controls.Add(this.chanPanMode);
		this.panelPAN.Controls.Add(this.chanLCRLevel);
		this.panelPAN.EQColor = System.Drawing.Color.Lime;
		this.panelPAN.Location = new System.Drawing.Point(9, 302);
		this.panelPAN.Name = "panelPAN";
		this.panelPAN.Padding = new System.Windows.Forms.Padding(18, 4, 18, 4);
		this.panelPAN.PanelType = 0;
		this.panelPAN.Size = new System.Drawing.Size(191, 158);
		this.panelPAN.TabIndex = 110;
		this.chanPan.Active = false;
		this.chanPan.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanPan.Channel = 0;
		this.chanPan.KnobLabel = "Pan";
		this.chanPan.KnobStyle = 1;
		this.chanPan.KnobType = AirControls.Values.knobTypes.None;
		this.chanPan.Location = new System.Drawing.Point(13, 26);
		this.chanPan.Maximum = 63;
		this.chanPan.Minimum = -63;
		this.chanPan.Name = "chanPan";
		this.chanPan.Size = new System.Drawing.Size(68, 95);
		this.chanPan.Steps = 1;
		this.chanPan.TabIndex = 104;
		this.chanPan.Value = 0;
		this.chanPan.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.chanPan.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("chanPan.ValueList");
		this.chanPan.ValueType = "pan";
		this.chanPan.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.chanPan.Click += new System.EventHandler(airKnob_Click);
		this.chanPan.MouseDown += new System.Windows.Forms.MouseEventHandler(airKnob_MouseDown);
		this.lblParam.Font = new System.Drawing.Font("Arial", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblParam.ForeColor = System.Drawing.Color.White;
		this.lblParam.Location = new System.Drawing.Point(62, 118);
		this.lblParam.Name = "lblParam";
		this.lblParam.Size = new System.Drawing.Size(68, 23);
		this.lblParam.TabIndex = 114;
		this.lblParam.Text = "Param";
		this.lblParam.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblParam.Visible = false;
		this.btnToMonoOn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnToMonoOn.BackText = "";
		this.btnToMonoOn.BorderColor = System.Drawing.Color.Black;
		this.btnToMonoOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnToMonoOn.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnToMonoOn.LEDColor = System.Drawing.Color.Gray;
		this.btnToMonoOn.Location = new System.Drawing.Point(102, 77);
		this.btnToMonoOn.Name = "btnToMonoOn";
		this.btnToMonoOn.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnToMonoOn.Size = new System.Drawing.Size(70, 64);
		this.btnToMonoOn.TabIndex = 81;
		this.btnToMonoOn.Tag = "1";
		this.btnToMonoOn.Text = "Mono";
		this.btnToMonoOn.UseVisualStyleBackColor = false;
		this.btnToMonoOn.Click += new System.EventHandler(btnToMonoOn_Click);
		this.btnToStereoOn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnToStereoOn.BackText = "";
		this.btnToStereoOn.BorderColor = System.Drawing.Color.Black;
		this.btnToStereoOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnToStereoOn.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnToStereoOn.LEDColor = System.Drawing.Color.Gray;
		this.btnToStereoOn.Location = new System.Drawing.Point(102, 7);
		this.btnToStereoOn.Name = "btnToStereoOn";
		this.btnToStereoOn.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnToStereoOn.Size = new System.Drawing.Size(70, 64);
		this.btnToStereoOn.TabIndex = 80;
		this.btnToStereoOn.Tag = "1";
		this.btnToStereoOn.Text = "Stereo";
		this.btnToStereoOn.UseVisualStyleBackColor = false;
		this.btnToStereoOn.Click += new System.EventHandler(btnToStereoOn_Click);
		this.chanPanMode.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanPanMode.BackText = "";
		this.chanPanMode.BorderColor = System.Drawing.Color.Black;
		this.chanPanMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.chanPanMode.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.chanPanMode.LEDColor = System.Drawing.Color.Gray;
		this.chanPanMode.Location = new System.Drawing.Point(87, 58);
		this.chanPanMode.Name = "chanPanMode";
		this.chanPanMode.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.chanPanMode.Size = new System.Drawing.Size(34, 40);
		this.chanPanMode.TabIndex = 88;
		this.chanPanMode.Tag = "1";
		this.chanPanMode.Text = "Mode";
		this.chanPanMode.UseVisualStyleBackColor = false;
		this.chanPanMode.Visible = false;
		this.chanLCRLevel.Active = false;
		this.chanLCRLevel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanLCRLevel.Channel = 0;
		this.chanLCRLevel.KnobLabel = "Center";
		this.chanLCRLevel.KnobStyle = 0;
		this.chanLCRLevel.KnobType = AirControls.Values.knobTypes.None;
		this.chanLCRLevel.Location = new System.Drawing.Point(87, 7);
		this.chanLCRLevel.Maximum = 100;
		this.chanLCRLevel.Minimum = 0;
		this.chanLCRLevel.Name = "chanLCRLevel";
		this.chanLCRLevel.Size = new System.Drawing.Size(68, 45);
		this.chanLCRLevel.Steps = 1;
		this.chanLCRLevel.TabIndex = 105;
		this.chanLCRLevel.Value = 0;
		this.chanLCRLevel.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.chanLCRLevel.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("chanLCRLevel.ValueList");
		this.chanLCRLevel.ValueType = "fader";
		this.chanLCRLevel.Visible = false;
		this.chanLCRLevel.Click += new System.EventHandler(airKnob_Click);
		this.airMeterPre.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.airMeterPre.BarColor = System.Drawing.Color.Lime;
		this.airMeterPre.BarStyle = 2;
		this.airMeterPre.BorderColor = System.Drawing.Color.LightGray;
		this.airMeterPre.Channel = 0;
		this.airMeterPre.Location = new System.Drawing.Point(667, 6);
		this.airMeterPre.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.airMeterPre.Maximum = 127;
		this.airMeterPre.Minimum = 0;
		this.airMeterPre.Name = "airMeterPre";
		this.airMeterPre.Size = new System.Drawing.Size(18, 522);
		this.airMeterPre.TabIndex = 113;
		this.airMeterPre.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.airMeterPre.Value = new decimal(new int[4] { 100, 0, 0, 0 });
		this.airMeterPre.ValueDB = new decimal(new int[4] { 150, 0, 0, 131072 });
		this.airMeterPost.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.airMeterPost.BarColor = System.Drawing.Color.Lime;
		this.airMeterPost.BarStyle = 2;
		this.airMeterPost.BorderColor = System.Drawing.Color.LightGray;
		this.airMeterPost.Channel = 0;
		this.airMeterPost.Location = new System.Drawing.Point(692, 6);
		this.airMeterPost.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.airMeterPost.Maximum = 127;
		this.airMeterPost.Minimum = 0;
		this.airMeterPost.Name = "airMeterPost";
		this.airMeterPost.Size = new System.Drawing.Size(18, 522);
		this.airMeterPost.TabIndex = 114;
		this.airMeterPost.TickMark = new decimal(new int[4] { 1, 0, 0, -2147483648 });
		this.airMeterPost.Value = new decimal(new int[4] { 100, 0, 0, 0 });
		this.airMeterPost.ValueDB = new decimal(new int[4] { 150, 0, 0, 131072 });
		this.chanFader.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.chanFader.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.chanFader.Channel = 0;
		this.chanFader.FaderColor = "White";
		this.chanFader.FaderStyle = 0;
		this.chanFader.FaderTails = true;
		this.chanFader.FineMode = false;
		this.chanFader.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.chanFader.Horizontal = false;
		this.chanFader.IsActive = false;
		this.chanFader.KnobColor = "White";
		this.chanFader.LabelType = "fader";
		this.chanFader.Location = new System.Drawing.Point(723, 57);
		this.chanFader.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.chanFader.Maximum = 1023;
		this.chanFader.MeterLevel = 0;
		this.chanFader.MeterType = "side";
		this.chanFader.Minimum = 0;
		this.chanFader.Name = "chanFader";
		this.chanFader.ShowKnob = false;
		this.chanFader.ShowMeter = false;
		this.chanFader.Size = new System.Drawing.Size(66, 472);
		this.chanFader.Steps = 1;
		this.chanFader.TabIndex = 20;
		this.chanFader.TailColor = System.Drawing.Color.Coral;
		this.chanFader.TickType = "fader";
		this.chanFader.Value = 867;
		this.chanFader.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.chanFader.ValueInt = 823;
		this.chanFader.ValueType = "fader";
		this.chanFader.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(chanFader_FaderChanged);
		this.tabEQ.Controls.Add(this.eqAtt);
		this.tabEQ.Controls.Add(this.btnPaste);
		this.tabEQ.Controls.Add(this.btnCopy);
		this.tabEQ.Controls.Add(this.btnEQOn);
		this.tabEQ.Controls.Add(this.btnFlat);
		this.tabEQ.Controls.Add(this.eqHPFFreq);
		this.tabEQ.Controls.Add(this.lblGain);
		this.tabEQ.Controls.Add(this.btnHPFOn);
		this.tabEQ.Controls.Add(this.lblFreq);
		this.tabEQ.Controls.Add(this.eq1G);
		this.tabEQ.Controls.Add(this.eq4G);
		this.tabEQ.Controls.Add(this.lblQ);
		this.tabEQ.Controls.Add(this.eq4LPF);
		this.tabEQ.Controls.Add(this.faderGain);
		this.tabEQ.Controls.Add(this.eq2G);
		this.tabEQ.Controls.Add(this.faderFreq);
		this.tabEQ.Controls.Add(this.eq3G);
		this.tabEQ.Controls.Add(this.faderQ);
		this.tabEQ.Controls.Add(this.picEQ);
		this.tabEQ.Controls.Add(this.btnLowEQType);
		this.tabEQ.Controls.Add(this.btnHiEQType);
		this.tabEQ.Location = new System.Drawing.Point(4, 59);
		this.tabEQ.Name = "tabEQ";
		this.tabEQ.Size = new System.Drawing.Size(957, 537);
		this.tabEQ.TabIndex = 1;
		this.tabEQ.Text = "EQ";
		this.eqAtt.Active = false;
		this.eqAtt.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eqAtt.Channel = 0;
		this.eqAtt.KnobLabel = "";
		this.eqAtt.KnobStyle = 0;
		this.eqAtt.KnobType = AirControls.Values.knobTypes.None;
		this.eqAtt.Location = new System.Drawing.Point(583, 8);
		this.eqAtt.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
		this.eqAtt.Maximum = 240;
		this.eqAtt.Minimum = -960;
		this.eqAtt.Name = "eqAtt";
		this.eqAtt.Size = new System.Drawing.Size(60, 64);
		this.eqAtt.Steps = 10;
		this.eqAtt.TabIndex = 50;
		this.eqAtt.Tag = "EQAtt";
		this.eqAtt.Value = 0;
		this.eqAtt.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.eqAtt.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eqAtt.ValueList");
		this.eqAtt.ValueType = "div10";
		this.eqAtt.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.eqAtt.Click += new System.EventHandler(eq_Click);
		this.eqAtt.MouseDown += new System.Windows.Forms.MouseEventHandler(eqKnob_MouseDown);
		this.btnPaste.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnPaste.BackText = "";
		this.btnPaste.BorderColor = System.Drawing.Color.Black;
		this.btnPaste.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnPaste.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnPaste.LEDColor = System.Drawing.Color.Gray;
		this.btnPaste.Location = new System.Drawing.Point(466, 8);
		this.btnPaste.Margin = new System.Windows.Forms.Padding(5);
		this.btnPaste.Name = "btnPaste";
		this.btnPaste.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnPaste.Size = new System.Drawing.Size(66, 64);
		this.btnPaste.TabIndex = 134;
		this.btnPaste.Tag = "Paste";
		this.btnPaste.Text = "Paste";
		this.btnPaste.UseVisualStyleBackColor = false;
		this.btnPaste.Click += new System.EventHandler(btnPaste_Click);
		this.btnCopy.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnCopy.BackText = "";
		this.btnCopy.BorderColor = System.Drawing.Color.Black;
		this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnCopy.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnCopy.LEDColor = System.Drawing.Color.Gray;
		this.btnCopy.Location = new System.Drawing.Point(390, 8);
		this.btnCopy.Margin = new System.Windows.Forms.Padding(5);
		this.btnCopy.Name = "btnCopy";
		this.btnCopy.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnCopy.Size = new System.Drawing.Size(66, 64);
		this.btnCopy.TabIndex = 133;
		this.btnCopy.Tag = "Copy";
		this.btnCopy.Text = "Copy";
		this.btnCopy.UseVisualStyleBackColor = false;
		this.btnCopy.Click += new System.EventHandler(btnCopy_Click);
		this.btnEQOn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnEQOn.BackText = "";
		this.btnEQOn.BorderColor = System.Drawing.Color.Black;
		this.btnEQOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnEQOn.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnEQOn.LEDColor = System.Drawing.Color.Gray;
		this.btnEQOn.Location = new System.Drawing.Point(314, 8);
		this.btnEQOn.Margin = new System.Windows.Forms.Padding(5);
		this.btnEQOn.Name = "btnEQOn";
		this.btnEQOn.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnEQOn.Size = new System.Drawing.Size(66, 64);
		this.btnEQOn.TabIndex = 123;
		this.btnEQOn.Tag = "1";
		this.btnEQOn.Text = "EQ On";
		this.btnEQOn.UseVisualStyleBackColor = false;
		this.btnEQOn.Click += new System.EventHandler(btnEQOn_Click);
		this.btnFlat.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnFlat.BackText = "";
		this.btnFlat.BorderColor = System.Drawing.Color.Black;
		this.btnFlat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnFlat.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnFlat.LEDColor = System.Drawing.Color.Gray;
		this.btnFlat.Location = new System.Drawing.Point(238, 8);
		this.btnFlat.Margin = new System.Windows.Forms.Padding(5);
		this.btnFlat.Name = "btnFlat";
		this.btnFlat.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnFlat.Size = new System.Drawing.Size(66, 64);
		this.btnFlat.TabIndex = 130;
		this.btnFlat.Tag = "Flat";
		this.btnFlat.Text = "Flat";
		this.btnFlat.UseVisualStyleBackColor = false;
		this.btnFlat.Click += new System.EventHandler(eqFlat_Click);
		this.eqHPFFreq.Active = false;
		this.eqHPFFreq.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eqHPFFreq.Channel = 0;
		this.eqHPFFreq.KnobLabel = "";
		this.eqHPFFreq.KnobStyle = 0;
		this.eqHPFFreq.KnobType = AirControls.Values.knobTypes.None;
		this.eqHPFFreq.Location = new System.Drawing.Point(84, 8);
		this.eqHPFFreq.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.eqHPFFreq.Maximum = 63;
		this.eqHPFFreq.Minimum = 4;
		this.eqHPFFreq.Name = "eqHPFFreq";
		this.eqHPFFreq.Size = new System.Drawing.Size(60, 64);
		this.eqHPFFreq.Steps = 1;
		this.eqHPFFreq.TabIndex = 50;
		this.eqHPFFreq.Tag = "HPFFreq";
		this.eqHPFFreq.Value = 63;
		this.eqHPFFreq.ValueDB = new decimal(new int[4] { 1000, 0, 0, 131072 });
		this.eqHPFFreq.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eqHPFFreq.ValueList");
		this.eqHPFFreq.ValueType = "freq";
		this.eqHPFFreq.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.eqHPFFreq.Click += new System.EventHandler(eq_Click);
		this.eqHPFFreq.MouseDown += new System.Windows.Forms.MouseEventHandler(eqKnob_MouseDown);
		this.lblGain.Font = new System.Drawing.Font("Arial", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblGain.ForeColor = System.Drawing.Color.White;
		this.lblGain.Location = new System.Drawing.Point(722, 5);
		this.lblGain.Name = "lblGain";
		this.lblGain.Size = new System.Drawing.Size(65, 20);
		this.lblGain.TabIndex = 116;
		this.lblGain.Text = "G";
		this.lblGain.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnHPFOn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnHPFOn.BackText = "";
		this.btnHPFOn.BorderColor = System.Drawing.Color.Black;
		this.btnHPFOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnHPFOn.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnHPFOn.LEDColor = System.Drawing.Color.Gray;
		this.btnHPFOn.Location = new System.Drawing.Point(8, 8);
		this.btnHPFOn.Margin = new System.Windows.Forms.Padding(5);
		this.btnHPFOn.Name = "btnHPFOn";
		this.btnHPFOn.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnHPFOn.Size = new System.Drawing.Size(66, 64);
		this.btnHPFOn.TabIndex = 123;
		this.btnHPFOn.Tag = "1";
		this.btnHPFOn.Text = "HPF On";
		this.btnHPFOn.UseVisualStyleBackColor = false;
		this.btnHPFOn.Click += new System.EventHandler(btnHPFOn_Click);
		this.lblFreq.Font = new System.Drawing.Font("Arial", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblFreq.ForeColor = System.Drawing.Color.White;
		this.lblFreq.Location = new System.Drawing.Point(8, 410);
		this.lblFreq.Name = "lblFreq";
		this.lblFreq.Size = new System.Drawing.Size(17, 40);
		this.lblFreq.TabIndex = 132;
		this.lblFreq.Text = "F";
		this.lblFreq.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblFreq.Visible = false;
		this.eq1G.Active = false;
		this.eq1G.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq1G.Channel = 0;
		this.eq1G.KnobLabel = "Low";
		this.eq1G.KnobStyle = 0;
		this.eq1G.KnobType = AirControls.Values.knobTypes.None;
		this.eq1G.Location = new System.Drawing.Point(656, 354);
		this.eq1G.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.eq1G.Maximum = 180;
		this.eq1G.Minimum = -180;
		this.eq1G.Name = "eq1G";
		this.eq1G.Size = new System.Drawing.Size(58, 83);
		this.eq1G.Steps = 5;
		this.eq1G.TabIndex = 44;
		this.eq1G.Tag = "Low";
		this.eq1G.Value = 0;
		this.eq1G.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.eq1G.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq1G.ValueList");
		this.eq1G.ValueType = "div10";
		this.eq1G.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.eq1G.Click += new System.EventHandler(eq_Click);
		this.eq1G.MouseDown += new System.Windows.Forms.MouseEventHandler(eqKnob_MouseDown);
		this.eq4G.Active = false;
		this.eq4G.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq4G.Channel = 0;
		this.eq4G.KnobLabel = "High";
		this.eq4G.KnobStyle = 0;
		this.eq4G.KnobType = AirControls.Values.knobTypes.None;
		this.eq4G.Location = new System.Drawing.Point(656, 80);
		this.eq4G.Margin = new System.Windows.Forms.Padding(8, 5, 8, 5);
		this.eq4G.Maximum = 180;
		this.eq4G.Minimum = -180;
		this.eq4G.Name = "eq4G";
		this.eq4G.Size = new System.Drawing.Size(58, 83);
		this.eq4G.Steps = 5;
		this.eq4G.TabIndex = 47;
		this.eq4G.Tag = "High";
		this.eq4G.Value = 0;
		this.eq4G.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.eq4G.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq4G.ValueList");
		this.eq4G.ValueType = "div10";
		this.eq4G.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.eq4G.Click += new System.EventHandler(eq_Click);
		this.eq4G.MouseDown += new System.Windows.Forms.MouseEventHandler(eqKnob_MouseDown);
		this.lblQ.Font = new System.Drawing.Font("Arial", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblQ.ForeColor = System.Drawing.Color.White;
		this.lblQ.Location = new System.Drawing.Point(8, 478);
		this.lblQ.Name = "lblQ";
		this.lblQ.Size = new System.Drawing.Size(17, 40);
		this.lblQ.TabIndex = 115;
		this.lblQ.Text = "Q";
		this.lblQ.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblQ.Visible = false;
		this.eq4LPF.Active = false;
		this.eq4LPF.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq4LPF.Channel = 0;
		this.eq4LPF.KnobLabel = "LPF";
		this.eq4LPF.KnobStyle = 0;
		this.eq4LPF.KnobType = AirControls.Values.knobTypes.None;
		this.eq4LPF.Location = new System.Drawing.Point(656, 80);
		this.eq4LPF.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.eq4LPF.Maximum = 1;
		this.eq4LPF.Minimum = 0;
		this.eq4LPF.Name = "eq4LPF";
		this.eq4LPF.Size = new System.Drawing.Size(58, 83);
		this.eq4LPF.Steps = 1;
		this.eq4LPF.TabIndex = 131;
		this.eq4LPF.Tag = "LPF";
		this.eq4LPF.Value = 0;
		this.eq4LPF.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.eq4LPF.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq4LPF.ValueList");
		this.eq4LPF.ValueType = "onoff";
		this.eq4LPF.Visible = false;
		this.eq4LPF.Click += new System.EventHandler(eq_Click);
		this.faderGain.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.faderGain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.faderGain.Channel = 0;
		this.faderGain.FaderColor = "Blue";
		this.faderGain.FaderStyle = 22;
		this.faderGain.FaderTails = false;
		this.faderGain.FineMode = false;
		this.faderGain.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.faderGain.Horizontal = false;
		this.faderGain.IsActive = false;
		this.faderGain.KnobColor = "White";
		this.faderGain.LabelType = "div10";
		this.faderGain.Location = new System.Drawing.Point(724, 29);
		this.faderGain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.faderGain.Maximum = 180;
		this.faderGain.MeterLevel = 0;
		this.faderGain.MeterType = "side";
		this.faderGain.Minimum = -180;
		this.faderGain.Name = "faderGain";
		this.faderGain.ShowKnob = false;
		this.faderGain.ShowMeter = false;
		this.faderGain.Size = new System.Drawing.Size(64, 501);
		this.faderGain.Steps = 1;
		this.faderGain.TabIndex = 49;
		this.faderGain.Tag = "G";
		this.faderGain.TailColor = System.Drawing.Color.Coral;
		this.faderGain.TickType = "div10";
		this.faderGain.Value = 0;
		AirControls.AirFader airFader2 = this.faderGain;
		int[] bits2 = new int[4];
		airFader2.ValueDB = new decimal(bits2);
		this.faderGain.ValueInt = 0;
		this.faderGain.ValueType = "div10";
		this.faderGain.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(faderGain_FaderChanged);
		this.eq2G.Active = false;
		this.eq2G.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq2G.Channel = 0;
		this.eq2G.KnobLabel = "Lo-Mid";
		this.eq2G.KnobStyle = 0;
		this.eq2G.KnobType = AirControls.Values.knobTypes.None;
		this.eq2G.Location = new System.Drawing.Point(656, 262);
		this.eq2G.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.eq2G.Maximum = 180;
		this.eq2G.Minimum = -180;
		this.eq2G.Name = "eq2G";
		this.eq2G.Size = new System.Drawing.Size(58, 83);
		this.eq2G.Steps = 5;
		this.eq2G.TabIndex = 45;
		this.eq2G.Tag = "LowMid";
		this.eq2G.Value = 0;
		this.eq2G.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.eq2G.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq2G.ValueList");
		this.eq2G.ValueType = "div10";
		this.eq2G.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.eq2G.Click += new System.EventHandler(eq_Click);
		this.eq2G.MouseDown += new System.Windows.Forms.MouseEventHandler(eqKnob_MouseDown);
		this.faderFreq.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.faderFreq.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.faderFreq.Channel = 0;
		this.faderFreq.FaderColor = "Blue";
		this.faderFreq.FaderStyle = 22;
		this.faderFreq.FaderTails = false;
		this.faderFreq.FineMode = false;
		this.faderFreq.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.faderFreq.Horizontal = true;
		this.faderFreq.IsActive = false;
		this.faderFreq.KnobColor = "Red";
		this.faderFreq.LabelType = "freq";
		this.faderFreq.Location = new System.Drawing.Point(6, 399);
		this.faderFreq.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.faderFreq.Maximum = 124;
		this.faderFreq.MeterLevel = 0;
		this.faderFreq.MeterType = "side";
		this.faderFreq.Minimum = 4;
		this.faderFreq.Name = "faderFreq";
		this.faderFreq.ShowKnob = true;
		this.faderFreq.ShowMeter = false;
		this.faderFreq.Size = new System.Drawing.Size(640, 61);
		this.faderFreq.Steps = 1;
		this.faderFreq.TabIndex = 48;
		this.faderFreq.Tag = "F";
		this.faderFreq.TailColor = System.Drawing.Color.Coral;
		this.faderFreq.TickType = "freq";
		this.faderFreq.Value = 124;
		this.faderFreq.ValueDB = new decimal(new int[4] { 5900, 0, 0, -2147352576 });
		this.faderFreq.ValueInt = 124;
		this.faderFreq.ValueType = "freq";
		this.faderFreq.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(faderFreq_FaderChanged);
		this.eq3G.Active = false;
		this.eq3G.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.eq3G.Channel = 0;
		this.eq3G.KnobLabel = "Hi-Mid";
		this.eq3G.KnobStyle = 0;
		this.eq3G.KnobType = AirControls.Values.knobTypes.None;
		this.eq3G.Location = new System.Drawing.Point(656, 173);
		this.eq3G.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.eq3G.Maximum = 180;
		this.eq3G.Minimum = -180;
		this.eq3G.Name = "eq3G";
		this.eq3G.Size = new System.Drawing.Size(58, 83);
		this.eq3G.Steps = 5;
		this.eq3G.TabIndex = 46;
		this.eq3G.Tag = "HiMid";
		this.eq3G.Value = 0;
		this.eq3G.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.eq3G.ValueList = (System.Collections.Generic.List<string>)resources.GetObject("eq3G.ValueList");
		this.eq3G.ValueType = "div10";
		this.eq3G.KnobChanged += new AirControls.AirKnob.AirKnobEventHandler(any_KnobChanged);
		this.eq3G.Click += new System.EventHandler(eq_Click);
		this.eq3G.MouseDown += new System.Windows.Forms.MouseEventHandler(eqKnob_MouseDown);
		this.faderQ.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.faderQ.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.faderQ.Channel = 0;
		this.faderQ.FaderColor = "Blue";
		this.faderQ.FaderStyle = 22;
		this.faderQ.FaderTails = false;
		this.faderQ.FineMode = false;
		this.faderQ.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.faderQ.Horizontal = true;
		this.faderQ.IsActive = false;
		this.faderQ.KnobColor = "White";
		this.faderQ.LabelType = "q";
		this.faderQ.Location = new System.Drawing.Point(6, 469);
		this.faderQ.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
		this.faderQ.Maximum = 40;
		this.faderQ.MeterLevel = 0;
		this.faderQ.MeterType = "side";
		this.faderQ.Minimum = 0;
		this.faderQ.Name = "faderQ";
		this.faderQ.ShowKnob = true;
		this.faderQ.ShowMeter = false;
		this.faderQ.Size = new System.Drawing.Size(640, 61);
		this.faderQ.Steps = 1;
		this.faderQ.TabIndex = 35;
		this.faderQ.Tag = "Q";
		this.faderQ.TailColor = System.Drawing.Color.Coral;
		this.faderQ.TickType = "q";
		this.faderQ.Value = 0;
		this.faderQ.ValueDB = new decimal(new int[4] { 99900, 0, 0, -2147352576 });
		this.faderQ.ValueInt = 0;
		this.faderQ.ValueType = "q";
		this.faderQ.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(faderQ_FaderChanged);
		this.picEQ.BackColor = System.Drawing.Color.Transparent;
		this.picEQ.BorderColor = System.Drawing.Color.Black;
		this.picEQ.BorderSize = 1f;
		this.picEQ.EQColor = System.Drawing.Color.Lime;
		this.picEQ.Location = new System.Drawing.Point(5, 80);
		this.picEQ.Name = "picEQ";
		this.picEQ.PanelType = 1;
		this.picEQ.Size = new System.Drawing.Size(638, 314);
		this.picEQ.TabIndex = 118;
		this.picEQ.Tag = "EQPlot";
		this.picEQ.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(picEQ_MouseDoubleClick);
		this.picEQ.MouseDown += new System.Windows.Forms.MouseEventHandler(picEQ_MouseDown);
		this.picEQ.MouseMove += new System.Windows.Forms.MouseEventHandler(picEQ_MouseMove);
		this.picEQ.MouseUp += new System.Windows.Forms.MouseEventHandler(picEQ_MouseUp);
		this.btnLowEQType.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnLowEQType.BackText = "";
		this.btnLowEQType.BorderColor = System.Drawing.Color.Black;
		this.btnLowEQType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnLowEQType.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnLowEQType.LEDColor = System.Drawing.Color.Gray;
		this.btnLowEQType.Location = new System.Drawing.Point(655, 446);
		this.btnLowEQType.Margin = new System.Windows.Forms.Padding(5);
		this.btnLowEQType.Name = "btnLowEQType";
		this.btnLowEQType.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnLowEQType.Size = new System.Drawing.Size(66, 64);
		this.btnLowEQType.TabIndex = 124;
		this.btnLowEQType.Tag = "1";
		this.btnLowEQType.Text = "Type";
		this.btnLowEQType.UseVisualStyleBackColor = false;
		this.btnLowEQType.Click += new System.EventHandler(btnLowEQType_Click);
		this.btnHiEQType.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnHiEQType.BackText = "";
		this.btnHiEQType.BorderColor = System.Drawing.Color.Black;
		this.btnHiEQType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnHiEQType.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnHiEQType.LEDColor = System.Drawing.Color.Gray;
		this.btnHiEQType.Location = new System.Drawing.Point(651, 8);
		this.btnHiEQType.Margin = new System.Windows.Forms.Padding(5);
		this.btnHiEQType.Name = "btnHiEQType";
		this.btnHiEQType.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnHiEQType.Size = new System.Drawing.Size(66, 64);
		this.btnHiEQType.TabIndex = 125;
		this.btnHiEQType.Tag = "1";
		this.btnHiEQType.Text = "Type";
		this.btnHiEQType.UseVisualStyleBackColor = false;
		this.btnHiEQType.Click += new System.EventHandler(btnHiEQType_Click);
		this.tabSends.Controls.Add(this.btnPage2);
		this.tabSends.Controls.Add(this.btnSendOn07);
		this.tabSends.Controls.Add(this.btnSendPre07);
		this.tabSends.Controls.Add(this.btnSendOn06);
		this.tabSends.Controls.Add(this.btnSendPre06);
		this.tabSends.Controls.Add(this.btnSendOn05);
		this.tabSends.Controls.Add(this.btnSendPre05);
		this.tabSends.Controls.Add(this.btnSendOn04);
		this.tabSends.Controls.Add(this.btnSendPre04);
		this.tabSends.Controls.Add(this.btnSendOn03);
		this.tabSends.Controls.Add(this.btnSendPre03);
		this.tabSends.Controls.Add(this.btnSendOn02);
		this.tabSends.Controls.Add(this.btnSendPre02);
		this.tabSends.Controls.Add(this.btnSendOn01);
		this.tabSends.Controls.Add(this.btnSendPre01);
		this.tabSends.Controls.Add(this.btnSendOn00);
		this.tabSends.Controls.Add(this.btnSendPre00);
		this.tabSends.Controls.Add(this.lblChan00);
		this.tabSends.Controls.Add(this.btnPage1);
		this.tabSends.Controls.Add(this.btnPage0);
		this.tabSends.Controls.Add(this.lblChan07);
		this.tabSends.Controls.Add(this.lblChan06);
		this.tabSends.Controls.Add(this.lblChan05);
		this.tabSends.Controls.Add(this.lblChan01);
		this.tabSends.Controls.Add(this.lblChan04);
		this.tabSends.Controls.Add(this.lblChan02);
		this.tabSends.Controls.Add(this.lblChan03);
		this.tabSends.Controls.Add(this.fader0);
		this.tabSends.Controls.Add(this.fader1);
		this.tabSends.Controls.Add(this.fader2);
		this.tabSends.Controls.Add(this.fader3);
		this.tabSends.Controls.Add(this.fader4);
		this.tabSends.Controls.Add(this.fader5);
		this.tabSends.Controls.Add(this.fader6);
		this.tabSends.Controls.Add(this.fader7);
		this.tabSends.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.tabSends.Location = new System.Drawing.Point(4, 59);
		this.tabSends.Name = "tabSends";
		this.tabSends.Size = new System.Drawing.Size(957, 537);
		this.tabSends.TabIndex = 0;
		this.tabSends.Text = "Sends";
		this.btnPage2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnPage2.BackText = "";
		this.btnPage2.BorderColor = System.Drawing.Color.Black;
		this.btnPage2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnPage2.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnPage2.LEDColor = System.Drawing.Color.Gray;
		this.btnPage2.Location = new System.Drawing.Point(578, 373);
		this.btnPage2.Name = "btnPage2";
		this.btnPage2.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnPage2.Size = new System.Drawing.Size(70, 64);
		this.btnPage2.TabIndex = 107;
		this.btnPage2.Text = "Matrix\r\n1 - 8";
		this.btnPage2.UseVisualStyleBackColor = false;
		this.btnPage2.Click += new System.EventHandler(btnPage2_Click);
		this.btnSendOn07.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn07.BackText = "";
		this.btnSendOn07.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn07.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn07.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn07.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn07.Location = new System.Drawing.Point(494, 49);
		this.btnSendOn07.Name = "btnSendOn07";
		this.btnSendOn07.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn07.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn07.TabIndex = 106;
		this.btnSendOn07.Text = "On";
		this.btnSendOn07.UseVisualStyleBackColor = false;
		this.btnSendOn07.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre07.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre07.BackText = "";
		this.btnSendPre07.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre07.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre07.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre07.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre07.Location = new System.Drawing.Point(494, 6);
		this.btnSendPre07.Name = "btnSendPre07";
		this.btnSendPre07.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre07.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre07.TabIndex = 105;
		this.btnSendPre07.Text = "Pre";
		this.btnSendPre07.UseVisualStyleBackColor = false;
		this.btnSendPre07.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn06.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn06.BackText = "";
		this.btnSendOn06.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn06.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn06.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn06.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn06.Location = new System.Drawing.Point(423, 49);
		this.btnSendOn06.Name = "btnSendOn06";
		this.btnSendOn06.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn06.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn06.TabIndex = 104;
		this.btnSendOn06.Text = "On";
		this.btnSendOn06.UseVisualStyleBackColor = false;
		this.btnSendOn06.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre06.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre06.BackText = "";
		this.btnSendPre06.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre06.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre06.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre06.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre06.Location = new System.Drawing.Point(423, 6);
		this.btnSendPre06.Name = "btnSendPre06";
		this.btnSendPre06.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre06.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre06.TabIndex = 103;
		this.btnSendPre06.Text = "Pre";
		this.btnSendPre06.UseVisualStyleBackColor = false;
		this.btnSendPre06.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn05.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn05.BackText = "";
		this.btnSendOn05.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn05.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn05.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn05.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn05.Location = new System.Drawing.Point(352, 49);
		this.btnSendOn05.Name = "btnSendOn05";
		this.btnSendOn05.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn05.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn05.TabIndex = 102;
		this.btnSendOn05.Text = "On";
		this.btnSendOn05.UseVisualStyleBackColor = false;
		this.btnSendOn05.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre05.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre05.BackText = "";
		this.btnSendPre05.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre05.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre05.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre05.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre05.Location = new System.Drawing.Point(352, 6);
		this.btnSendPre05.Name = "btnSendPre05";
		this.btnSendPre05.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre05.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre05.TabIndex = 101;
		this.btnSendPre05.Text = "Pre";
		this.btnSendPre05.UseVisualStyleBackColor = false;
		this.btnSendPre05.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn04.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn04.BackText = "";
		this.btnSendOn04.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn04.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn04.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn04.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn04.Location = new System.Drawing.Point(283, 49);
		this.btnSendOn04.Name = "btnSendOn04";
		this.btnSendOn04.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn04.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn04.TabIndex = 100;
		this.btnSendOn04.Text = "On";
		this.btnSendOn04.UseVisualStyleBackColor = false;
		this.btnSendOn04.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre04.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre04.BackText = "";
		this.btnSendPre04.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre04.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre04.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre04.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre04.Location = new System.Drawing.Point(283, 6);
		this.btnSendPre04.Name = "btnSendPre04";
		this.btnSendPre04.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre04.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre04.TabIndex = 99;
		this.btnSendPre04.Text = "Pre";
		this.btnSendPre04.UseVisualStyleBackColor = false;
		this.btnSendPre04.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn03.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn03.BackText = "";
		this.btnSendOn03.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn03.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn03.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn03.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn03.Location = new System.Drawing.Point(213, 49);
		this.btnSendOn03.Name = "btnSendOn03";
		this.btnSendOn03.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn03.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn03.TabIndex = 98;
		this.btnSendOn03.Text = "On";
		this.btnSendOn03.UseVisualStyleBackColor = false;
		this.btnSendOn03.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre03.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre03.BackText = "";
		this.btnSendPre03.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre03.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre03.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre03.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre03.Location = new System.Drawing.Point(213, 6);
		this.btnSendPre03.Name = "btnSendPre03";
		this.btnSendPre03.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre03.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre03.TabIndex = 97;
		this.btnSendPre03.Text = "Pre";
		this.btnSendPre03.UseVisualStyleBackColor = false;
		this.btnSendPre03.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn02.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn02.BackText = "";
		this.btnSendOn02.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn02.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn02.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn02.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn02.Location = new System.Drawing.Point(144, 49);
		this.btnSendOn02.Name = "btnSendOn02";
		this.btnSendOn02.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn02.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn02.TabIndex = 96;
		this.btnSendOn02.Text = "On";
		this.btnSendOn02.UseVisualStyleBackColor = false;
		this.btnSendOn02.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre02.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre02.BackText = "";
		this.btnSendPre02.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre02.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre02.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre02.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre02.Location = new System.Drawing.Point(144, 6);
		this.btnSendPre02.Name = "btnSendPre02";
		this.btnSendPre02.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre02.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre02.TabIndex = 95;
		this.btnSendPre02.Text = "Pre";
		this.btnSendPre02.UseVisualStyleBackColor = false;
		this.btnSendPre02.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn01.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn01.BackText = "";
		this.btnSendOn01.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn01.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn01.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn01.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn01.Location = new System.Drawing.Point(73, 49);
		this.btnSendOn01.Name = "btnSendOn01";
		this.btnSendOn01.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn01.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn01.TabIndex = 94;
		this.btnSendOn01.Text = "On";
		this.btnSendOn01.UseVisualStyleBackColor = false;
		this.btnSendOn01.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre01.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre01.BackText = "";
		this.btnSendPre01.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre01.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre01.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre01.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre01.Location = new System.Drawing.Point(73, 6);
		this.btnSendPre01.Name = "btnSendPre01";
		this.btnSendPre01.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre01.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre01.TabIndex = 93;
		this.btnSendPre01.Text = "Pre";
		this.btnSendPre01.UseVisualStyleBackColor = false;
		this.btnSendPre01.Click += new System.EventHandler(btnSendPre_Click);
		this.btnSendOn00.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendOn00.BackText = "";
		this.btnSendOn00.BorderColor = System.Drawing.Color.Black;
		this.btnSendOn00.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendOn00.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendOn00.LEDColor = System.Drawing.Color.Gray;
		this.btnSendOn00.Location = new System.Drawing.Point(3, 49);
		this.btnSendOn00.Name = "btnSendOn00";
		this.btnSendOn00.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendOn00.Size = new System.Drawing.Size(66, 41);
		this.btnSendOn00.TabIndex = 92;
		this.btnSendOn00.Text = "On";
		this.btnSendOn00.UseVisualStyleBackColor = false;
		this.btnSendOn00.Click += new System.EventHandler(btnSendOn_Click);
		this.btnSendPre00.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnSendPre00.BackText = "";
		this.btnSendPre00.BorderColor = System.Drawing.Color.Black;
		this.btnSendPre00.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnSendPre00.Font = new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btnSendPre00.LEDColor = System.Drawing.Color.Gray;
		this.btnSendPre00.Location = new System.Drawing.Point(3, 6);
		this.btnSendPre00.Name = "btnSendPre00";
		this.btnSendPre00.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnSendPre00.Size = new System.Drawing.Size(66, 37);
		this.btnSendPre00.TabIndex = 91;
		this.btnSendPre00.Text = "Pre";
		this.btnSendPre00.UseVisualStyleBackColor = false;
		this.btnSendPre00.Click += new System.EventHandler(btnSendPre_Click);
		this.lblChan00.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan00.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan00.Location = new System.Drawing.Point(4, 508);
		this.lblChan00.Name = "lblChan00";
		this.lblChan00.Size = new System.Drawing.Size(66, 25);
		this.lblChan00.TabIndex = 59;
		this.lblChan00.Text = "1";
		this.lblChan00.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.btnPage1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnPage1.BackText = "";
		this.btnPage1.BorderColor = System.Drawing.Color.Black;
		this.btnPage1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnPage1.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnPage1.LEDColor = System.Drawing.Color.Gray;
		this.btnPage1.Location = new System.Drawing.Point(578, 303);
		this.btnPage1.Name = "btnPage1";
		this.btnPage1.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnPage1.Size = new System.Drawing.Size(70, 64);
		this.btnPage1.TabIndex = 85;
		this.btnPage1.Text = "Mix\r\n9 - 16";
		this.btnPage1.UseVisualStyleBackColor = false;
		this.btnPage1.Click += new System.EventHandler(btnPage1_Click);
		this.btnPage0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.btnPage0.BackText = "";
		this.btnPage0.BorderColor = System.Drawing.Color.Black;
		this.btnPage0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.btnPage0.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.btnPage0.LEDColor = System.Drawing.Color.Gray;
		this.btnPage0.Location = new System.Drawing.Point(578, 233);
		this.btnPage0.Name = "btnPage0";
		this.btnPage0.ParentColor = System.Drawing.Color.FromArgb(50, 50, 50);
		this.btnPage0.Size = new System.Drawing.Size(70, 64);
		this.btnPage0.TabIndex = 84;
		this.btnPage0.Text = "Mix\r\n1 - 8";
		this.btnPage0.UseVisualStyleBackColor = false;
		this.btnPage0.Click += new System.EventHandler(btnPage0_Click);
		this.lblChan07.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan07.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan07.Location = new System.Drawing.Point(494, 508);
		this.lblChan07.Name = "lblChan07";
		this.lblChan07.Size = new System.Drawing.Size(66, 25);
		this.lblChan07.TabIndex = 66;
		this.lblChan07.Text = "8";
		this.lblChan07.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblChan06.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan06.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan06.Location = new System.Drawing.Point(424, 508);
		this.lblChan06.Name = "lblChan06";
		this.lblChan06.Size = new System.Drawing.Size(66, 25);
		this.lblChan06.TabIndex = 65;
		this.lblChan06.Text = "7";
		this.lblChan06.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblChan05.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan05.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan05.Location = new System.Drawing.Point(354, 508);
		this.lblChan05.Name = "lblChan05";
		this.lblChan05.Size = new System.Drawing.Size(66, 25);
		this.lblChan05.TabIndex = 64;
		this.lblChan05.Text = "6";
		this.lblChan05.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblChan01.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan01.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan01.Location = new System.Drawing.Point(74, 508);
		this.lblChan01.Name = "lblChan01";
		this.lblChan01.Size = new System.Drawing.Size(66, 25);
		this.lblChan01.TabIndex = 60;
		this.lblChan01.Text = "2";
		this.lblChan01.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblChan04.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan04.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan04.Location = new System.Drawing.Point(284, 508);
		this.lblChan04.Name = "lblChan04";
		this.lblChan04.Size = new System.Drawing.Size(66, 25);
		this.lblChan04.TabIndex = 63;
		this.lblChan04.Text = "5";
		this.lblChan04.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblChan02.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan02.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan02.Location = new System.Drawing.Point(144, 508);
		this.lblChan02.Name = "lblChan02";
		this.lblChan02.Size = new System.Drawing.Size(66, 25);
		this.lblChan02.TabIndex = 61;
		this.lblChan02.Text = "3";
		this.lblChan02.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.lblChan03.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lblChan03.Font = new System.Drawing.Font("Arial", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.lblChan03.Location = new System.Drawing.Point(214, 508);
		this.lblChan03.Name = "lblChan03";
		this.lblChan03.Size = new System.Drawing.Size(66, 25);
		this.lblChan03.TabIndex = 62;
		this.lblChan03.Text = "4";
		this.lblChan03.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.fader0.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader0.Channel = 0;
		this.fader0.FaderColor = "Orange";
		this.fader0.FaderStyle = 2;
		this.fader0.FaderTails = true;
		this.fader0.FineMode = false;
		this.fader0.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader0.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader0.Horizontal = false;
		this.fader0.IsActive = false;
		this.fader0.KnobColor = "White";
		this.fader0.LabelType = "fader";
		this.fader0.Location = new System.Drawing.Point(4, 97);
		this.fader0.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader0.Maximum = 1023;
		this.fader0.MeterLevel = 0;
		this.fader0.MeterType = "side";
		this.fader0.Minimum = 0;
		this.fader0.Name = "fader0";
		this.fader0.ShowKnob = false;
		this.fader0.ShowMeter = false;
		this.fader0.Size = new System.Drawing.Size(66, 409);
		this.fader0.Steps = 1;
		this.fader0.TabIndex = 51;
		this.fader0.Tag = "0";
		this.fader0.TailColor = System.Drawing.Color.Coral;
		this.fader0.TickType = "fader";
		this.fader0.Value = 867;
		this.fader0.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader0.ValueInt = 823;
		this.fader0.ValueType = "fader";
		this.fader0.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader1.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader1.Channel = 1;
		this.fader1.FaderColor = "Orange";
		this.fader1.FaderStyle = 2;
		this.fader1.FaderTails = true;
		this.fader1.FineMode = false;
		this.fader1.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader1.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader1.Horizontal = false;
		this.fader1.IsActive = false;
		this.fader1.KnobColor = "White";
		this.fader1.LabelType = "fader";
		this.fader1.Location = new System.Drawing.Point(74, 97);
		this.fader1.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader1.Maximum = 1023;
		this.fader1.MeterLevel = 0;
		this.fader1.MeterType = "side";
		this.fader1.Minimum = 0;
		this.fader1.Name = "fader1";
		this.fader1.ShowKnob = false;
		this.fader1.ShowMeter = false;
		this.fader1.Size = new System.Drawing.Size(66, 409);
		this.fader1.Steps = 1;
		this.fader1.TabIndex = 52;
		this.fader1.Tag = "1";
		this.fader1.TailColor = System.Drawing.Color.Coral;
		this.fader1.TickType = "fader";
		this.fader1.Value = 867;
		this.fader1.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader1.ValueInt = 823;
		this.fader1.ValueType = "fader";
		this.fader1.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader2.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader2.Channel = 2;
		this.fader2.FaderColor = "Orange";
		this.fader2.FaderStyle = 2;
		this.fader2.FaderTails = true;
		this.fader2.FineMode = false;
		this.fader2.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader2.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader2.Horizontal = false;
		this.fader2.IsActive = false;
		this.fader2.KnobColor = "White";
		this.fader2.LabelType = "fader";
		this.fader2.Location = new System.Drawing.Point(144, 97);
		this.fader2.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader2.Maximum = 1023;
		this.fader2.MeterLevel = 0;
		this.fader2.MeterType = "side";
		this.fader2.Minimum = 0;
		this.fader2.Name = "fader2";
		this.fader2.ShowKnob = false;
		this.fader2.ShowMeter = false;
		this.fader2.Size = new System.Drawing.Size(66, 409);
		this.fader2.Steps = 1;
		this.fader2.TabIndex = 53;
		this.fader2.Tag = "2";
		this.fader2.TailColor = System.Drawing.Color.Coral;
		this.fader2.TickType = "fader";
		this.fader2.Value = 867;
		this.fader2.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader2.ValueInt = 823;
		this.fader2.ValueType = "fader";
		this.fader2.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader3.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader3.Channel = 3;
		this.fader3.FaderColor = "Orange";
		this.fader3.FaderStyle = 2;
		this.fader3.FaderTails = true;
		this.fader3.FineMode = false;
		this.fader3.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader3.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader3.Horizontal = false;
		this.fader3.IsActive = false;
		this.fader3.KnobColor = "White";
		this.fader3.LabelType = "fader";
		this.fader3.Location = new System.Drawing.Point(214, 97);
		this.fader3.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader3.Maximum = 1023;
		this.fader3.MeterLevel = 0;
		this.fader3.MeterType = "side";
		this.fader3.Minimum = 0;
		this.fader3.Name = "fader3";
		this.fader3.ShowKnob = false;
		this.fader3.ShowMeter = false;
		this.fader3.Size = new System.Drawing.Size(66, 409);
		this.fader3.Steps = 1;
		this.fader3.TabIndex = 54;
		this.fader3.Tag = "3";
		this.fader3.TailColor = System.Drawing.Color.Coral;
		this.fader3.TickType = "fader";
		this.fader3.Value = 867;
		this.fader3.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader3.ValueInt = 823;
		this.fader3.ValueType = "fader";
		this.fader3.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader4.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader4.Channel = 4;
		this.fader4.FaderColor = "Orange";
		this.fader4.FaderStyle = 2;
		this.fader4.FaderTails = true;
		this.fader4.FineMode = false;
		this.fader4.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader4.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader4.Horizontal = false;
		this.fader4.IsActive = false;
		this.fader4.KnobColor = "White";
		this.fader4.LabelType = "fader";
		this.fader4.Location = new System.Drawing.Point(284, 97);
		this.fader4.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader4.Maximum = 1023;
		this.fader4.MeterLevel = 0;
		this.fader4.MeterType = "side";
		this.fader4.Minimum = 0;
		this.fader4.Name = "fader4";
		this.fader4.ShowKnob = false;
		this.fader4.ShowMeter = false;
		this.fader4.Size = new System.Drawing.Size(66, 409);
		this.fader4.Steps = 1;
		this.fader4.TabIndex = 55;
		this.fader4.Tag = "4";
		this.fader4.TailColor = System.Drawing.Color.Coral;
		this.fader4.TickType = "fader";
		this.fader4.Value = 867;
		this.fader4.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader4.ValueInt = 823;
		this.fader4.ValueType = "fader";
		this.fader4.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader5.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader5.Channel = 5;
		this.fader5.FaderColor = "Orange";
		this.fader5.FaderStyle = 2;
		this.fader5.FaderTails = true;
		this.fader5.FineMode = false;
		this.fader5.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader5.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader5.Horizontal = false;
		this.fader5.IsActive = false;
		this.fader5.KnobColor = "White";
		this.fader5.LabelType = "fader";
		this.fader5.Location = new System.Drawing.Point(354, 97);
		this.fader5.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader5.Maximum = 1023;
		this.fader5.MeterLevel = 0;
		this.fader5.MeterType = "side";
		this.fader5.Minimum = 0;
		this.fader5.Name = "fader5";
		this.fader5.ShowKnob = false;
		this.fader5.ShowMeter = false;
		this.fader5.Size = new System.Drawing.Size(66, 409);
		this.fader5.Steps = 1;
		this.fader5.TabIndex = 56;
		this.fader5.Tag = "5";
		this.fader5.TailColor = System.Drawing.Color.Coral;
		this.fader5.TickType = "fader";
		this.fader5.Value = 867;
		this.fader5.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader5.ValueInt = 823;
		this.fader5.ValueType = "fader";
		this.fader5.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader6.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader6.Channel = 6;
		this.fader6.FaderColor = "Orange";
		this.fader6.FaderStyle = 2;
		this.fader6.FaderTails = true;
		this.fader6.FineMode = false;
		this.fader6.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader6.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader6.Horizontal = false;
		this.fader6.IsActive = false;
		this.fader6.KnobColor = "White";
		this.fader6.LabelType = "fader";
		this.fader6.Location = new System.Drawing.Point(424, 97);
		this.fader6.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader6.Maximum = 1023;
		this.fader6.MeterLevel = 0;
		this.fader6.MeterType = "side";
		this.fader6.Minimum = 0;
		this.fader6.Name = "fader6";
		this.fader6.ShowKnob = false;
		this.fader6.ShowMeter = false;
		this.fader6.Size = new System.Drawing.Size(66, 409);
		this.fader6.Steps = 1;
		this.fader6.TabIndex = 57;
		this.fader6.Tag = "6";
		this.fader6.TailColor = System.Drawing.Color.Coral;
		this.fader6.TickType = "fader";
		this.fader6.Value = 867;
		this.fader6.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader6.ValueInt = 823;
		this.fader6.ValueType = "fader";
		this.fader6.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		this.fader7.BackColor = AirFader.Properties.Settings.Default.c_fader_back;
		this.fader7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.fader7.Channel = 7;
		this.fader7.FaderColor = "Orange";
		this.fader7.FaderStyle = 2;
		this.fader7.FaderTails = true;
		this.fader7.FineMode = false;
		this.fader7.Font = new System.Drawing.Font("Arial", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.fader7.ForeColor = AirFader.Properties.Settings.Default.c_fader_fore;
		this.fader7.Horizontal = false;
		this.fader7.IsActive = false;
		this.fader7.KnobColor = "White";
		this.fader7.LabelType = "fader";
		this.fader7.Location = new System.Drawing.Point(494, 97);
		this.fader7.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
		this.fader7.Maximum = 1023;
		this.fader7.MeterLevel = 0;
		this.fader7.MeterType = "side";
		this.fader7.Minimum = 0;
		this.fader7.Name = "fader7";
		this.fader7.ShowKnob = false;
		this.fader7.ShowMeter = false;
		this.fader7.Size = new System.Drawing.Size(66, 409);
		this.fader7.Steps = 1;
		this.fader7.TabIndex = 58;
		this.fader7.Tag = "7";
		this.fader7.TailColor = System.Drawing.Color.Coral;
		this.fader7.TickType = "fader";
		this.fader7.Value = 867;
		this.fader7.ValueDB = new decimal(new int[4] { 220, 0, 0, 131072 });
		this.fader7.ValueInt = 823;
		this.fader7.ValueType = "fader";
		this.fader7.FaderChanged += new AirControls.AirFader.AirFaderEventHandler(fader_FaderChanged);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		base.ClientSize = new System.Drawing.Size(800, 600);
		base.Controls.Add(this.airPanel7);
		base.Controls.Add(this.airPanel5);
		base.Controls.Add(this.panelTopMenu);
		base.Controls.Add(this.tabSection);
		this.DoubleBuffered = true;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "frmChannel";
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "Channel Editor";
		base.Deactivate += new System.EventHandler(frmChannel_Deactivate);
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(frmChannel_FormClosing);
		base.Load += new System.EventHandler(frmChannel_Load);
		base.Shown += new System.EventHandler(frmChannel_Shown);
		base.ResizeBegin += new System.EventHandler(frmChannel_ResizeBegin);
		base.ResizeEnd += new System.EventHandler(frmChannel_ResizeEnd);
		base.LocationChanged += new System.EventHandler(frmChannel_LocationChanged);
		base.Move += new System.EventHandler(frmChannel_Move);
		base.Resize += new System.EventHandler(frmChannel_Resize);
		this.panelTopMenu.ResumeLayout(false);
		this.airPanel7.ResumeLayout(false);
		this.airPanel5.ResumeLayout(false);
		this.tabSection.ResumeLayout(false);
		this.tabDyn.ResumeLayout(false);
		this.panelDCA.ResumeLayout(false);
		this.panelHA.ResumeLayout(false);
		this.panelDyn1.ResumeLayout(false);
		this.panelMutes.ResumeLayout(false);
		this.panelDyn0.ResumeLayout(false);
		this.panelPAN.ResumeLayout(false);
		this.tabEQ.ResumeLayout(false);
		this.tabSends.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
