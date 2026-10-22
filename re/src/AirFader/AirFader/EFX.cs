using System;
using System.Collections.Generic;
using System.Drawing;
using AirControls;

namespace AirFader;

public class EFX
{
	public enum Pr
	{
		None,
		BPM,
		Bypass,
		Mix,
		Type,
		Param
	}

	public List<Pr> changedParams = new List<Pr>();

	public List<int> changedIndexes = new List<int>();

	public int _type;

	public bool _bypass;

	public int _mix = 100;

	public int bpm = 120;

	public int Rack;

	public string EFXName = "";

	public string[] pLabel = new string[32];

	public int[] pParamIndex = new int[32];

	public Color[] pColor = new Color[32];

	public int[] pValue = new int[32];

	public int[] pMin = new int[32];

	public int[] pMax = new int[32];

	public Values.knobTypes[] pType = new Values.knobTypes[32];

	public void initParam(int index, int newParamIndex, string newLabel, int newValue, int newMin, int newMax, Values.knobTypes newType, Color newColor)
	{
		pParamIndex[index] = newParamIndex;
		pLabel[index] = newLabel;
		pType[index] = newType;
		pMin[index] = newMin;
		pMax[index] = newMax;
		pValue[index] = newValue;
		pColor[index] = newColor;
	}

	public EFX(int rack)
	{
		Rack = rack;
	}

	private int findNearest(double find, List<double> list)
	{
		int result = 0;
		double num = double.MaxValue;
		for (int i = 0; i < list.Count; i++)
		{
			double num2 = Math.Abs(list[i] - find);
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public void setParamByIndex(string source, int index, int value)
	{
		if (index < 0 || index > pMin.Length - 1 || value < pMin[index] || value > pMax[index] || pValue[index] == value)
		{
			return;
		}
		_ = pValue[index];
		pValue[index] = value;
		if (source != "CONSOLE" && source != "LOGIC")
		{
			if (MixEngine.connStatus != 0)
			{
				if (!changedParams.Contains(Pr.Param))
				{
					changedParams.Add(Pr.Param);
				}
				if (!changedIndexes.Contains(index))
				{
					changedIndexes.Add(index);
				}
				if (!MixEngine.updateRack.Contains(Rack))
				{
					MixEngine.updateRack.Add(Rack);
				}
			}
			int type = _type;
			if (type == 7)
			{
				if (pValue[6] == 1 && index == 0)
				{
					double find = (double)pValue[0] * 0.1 / (60.0 / (double)bpm * 1000.0) - 0.0208;
					setParamByIndex("LOGIC", 7, findNearest(find, Values.FloatMap[Values.knobTypes.EFXNote]));
				}
				else if (pValue[6] == 1 && index == 7)
				{
					double value2 = 60.0 / (double)bpm * 1000.0 * Values.FloatMap[Values.knobTypes.EFXNote][pValue[7]];
					setParamByIndex("LOGIC", 0, (int)(Math.Round(value2, 1) * 10.0));
				}
			}
		}
		MixEngine.PushEFXParam(Rack, "Param", index);
	}

	public void setParamByKnob(string source, int index, int value)
	{
		setParamByIndex(source, index, value);
	}

	public void setParamByMIDIIndex(string source, int index, int value)
	{
		setParamByIndex(source, Array.IndexOf(pParamIndex, index), value);
	}

	public void setEFXType(int newType)
	{
		switch (newType)
		{
		case 0:
		{
			_type = 0;
			EFXName = "REVERB HALL";
			initParam(0, 12, "Rev Time", 29, 0, 83, Values.knobTypes.EFXRevTime, Color.Yellow);
			initParam(1, 11, "Ini.Dly", 360, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "Hi.Ratio", 2, 0, 9, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(3, 14, "Lo.Ratio", 13, 0, 23, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(4, 15, "Diff.", 8, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 100, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 19, "E/R Dly", 20, 0, 1000, Values.knobTypes.Div10MS, Color.LightGreen);
			initParam(7, 20, "E/R Bal.", 44, 0, 100, Values.knobTypes.Percent, Color.LightGreen);
			initParam(8, 17, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(9, 18, "LPF", 85, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(10, 17, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(11, 18, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(12, 21, "Gate Lvl", 0, 0, 61, Values.knobTypes.EFXGate, Color.Red);
			initParam(13, 22, "Attack", 4, 0, 120, Values.knobTypes.IntegerMS, Color.Red);
			initParam(14, 23, "Hold", 172, 0, 215, Values.knobTypes.HoldTime48, Color.Red);
			initParam(15, 24, "Decay", 12, 0, 159, Values.knobTypes.Decay48, Color.Red);
			for (int num2 = 16; num2 < 32; num2++)
			{
				initParam(num2, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 1:
		{
			_type = 1;
			EFXName = "REVERB ROOM";
			initParam(0, 12, "Rev Time", 7, 0, 83, Values.knobTypes.EFXRevTime, Color.Yellow);
			initParam(1, 11, "Ini.Dly", 150, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "Hi.Ratio", 3, 0, 9, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(3, 14, "Lo.Ratio", 11, 0, 23, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(4, 15, "Diff.", 8, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 98, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 19, "E/R Dly", 60, 0, 1000, Values.knobTypes.Div10MS, Color.LightGreen);
			initParam(7, 20, "E/R Bal.", 50, 0, 100, Values.knobTypes.Percent, Color.LightGreen);
			initParam(8, 17, "HPF", 25, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(9, 18, "LPF", 88, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(10, 17, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(11, 18, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(12, 21, "Gate Lvl", 0, 0, 61, Values.knobTypes.EFXGate, Color.Red);
			initParam(13, 22, "Attack", 4, 0, 120, Values.knobTypes.IntegerMS, Color.Red);
			initParam(14, 23, "Hold", 158, 0, 215, Values.knobTypes.HoldTime48, Color.Red);
			initParam(15, 24, "Decay", 0, 0, 159, Values.knobTypes.Decay48, Color.Red);
			for (int k = 16; k < 32; k++)
			{
				initParam(k, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 2:
		{
			_type = 2;
			EFXName = "REVERB STAGE";
			initParam(0, 12, "Rev Time", 15, 0, 83, Values.knobTypes.EFXRevTime, Color.Yellow);
			initParam(1, 11, "Ini.Dly", 460, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "Hi.Ratio", 6, 0, 9, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(3, 14, "Lo.Ratio", 7, 0, 23, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(4, 15, "Diff.", 6, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 65, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 19, "E/R Dly", 120, 0, 1000, Values.knobTypes.Div10MS, Color.LightGreen);
			initParam(7, 20, "E/R Bal.", 39, 0, 100, Values.knobTypes.Percent, Color.LightGreen);
			initParam(8, 17, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(9, 18, "LPF", 93, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(10, 17, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(11, 18, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(12, 21, "Gate Lvl", 0, 0, 61, Values.knobTypes.EFXGate, Color.Red);
			initParam(13, 22, "Attack", 4, 0, 120, Values.knobTypes.IntegerMS, Color.Red);
			initParam(14, 23, "Hold", 159, 0, 215, Values.knobTypes.HoldTime48, Color.Red);
			initParam(15, 24, "Decay", 0, 0, 159, Values.knobTypes.Decay48, Color.Red);
			for (int num3 = 16; num3 < 32; num3++)
			{
				initParam(num3, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 3:
		{
			_type = 3;
			EFXName = "REVERB PLATE";
			initParam(0, 12, "Rev Time", 23, 0, 83, Values.knobTypes.EFXRevTime, Color.Yellow);
			initParam(1, 11, "Ini.Dly", 420, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "Hi.Ratio", 2, 0, 9, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(3, 14, "Lo.Ratio", 9, 0, 23, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(4, 15, "Diff.", 6, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 70, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 19, "E/R Dly", 160, 0, 1000, Values.knobTypes.Div10MS, Color.LightGreen);
			initParam(7, 20, "E/R Bal.", 50, 0, 100, Values.knobTypes.Percent, Color.LightGreen);
			initParam(8, 17, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(9, 18, "LPF", 81, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(10, 17, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(11, 18, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			initParam(12, 21, "Gate Lvl", 0, 0, 61, Values.knobTypes.EFXGate, Color.Red);
			initParam(13, 22, "Attack", 4, 0, 120, Values.knobTypes.IntegerMS, Color.Red);
			initParam(14, 23, "Hold", 161, 0, 215, Values.knobTypes.HoldTime48, Color.Red);
			initParam(15, 24, "Decay", 15, 0, 159, Values.knobTypes.Decay48, Color.Red);
			for (int num6 = 16; num6 < 32; num6++)
			{
				initParam(num6, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 4:
		{
			_type = 4;
			EFXName = "EARLY REF.";
			initParam(0, 11, "Type", 1, 0, 5, Values.knobTypes.EFXRefType, Color.Yellow);
			initParam(1, 12, "RoomSize", 23, 0, 199, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(2, 13, "Liveness", 7, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(3, 14, "Ini.Dly", 50, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(4, 15, "Diff.", 7, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 85, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 17, "Er Num.", 10, 0, 18, Values.knobTypes.IntegerPlusOne, Color.Yellow);
			initParam(7, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Yellow);
			initParam(8, 18, "FB.Gain", 101, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(9, 19, "Hi.Ratio", 3, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(10, 20, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(11, 21, "LPF", 95, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			for (int m = 12; m < 32; m++)
			{
				initParam(m, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 5:
		{
			_type = 5;
			EFXName = "GATE REVERB";
			initParam(0, 11, "Type", 0, 0, 1, Values.knobTypes.EFXGateType, Color.Yellow);
			initParam(1, 12, "RoomSize", 15, 0, 199, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(2, 13, "Liveness", 10, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(3, 14, "Ini.Dly", 30, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(4, 15, "Diff.", 8, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 96, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 17, "Er Num.", 17, 0, 18, Values.knobTypes.IntegerPlusOne, Color.Yellow);
			initParam(7, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Yellow);
			initParam(8, 18, "FB.Gain", 108, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(9, 19, "Hi.Ratio", 4, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(10, 20, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(11, 21, "LPF", 90, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			for (int num5 = 12; num5 < 32; num5++)
			{
				initParam(num5, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 6:
		{
			_type = 6;
			EFXName = "REVERSE GATE";
			initParam(0, 11, "Type", 0, 0, 1, Values.knobTypes.EFXGateType, Color.Yellow);
			initParam(1, 12, "RoomSize", 27, 0, 199, Values.knobTypes.EFXDiv10, Color.Yellow);
			initParam(2, 13, "Liveness", 10, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(3, 14, "Ini.Dly", 350, 0, 5000, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(4, 15, "Diff.", 8, 0, 10, Values.knobTypes.Integer, Color.Yellow);
			initParam(5, 16, "Density", 96, 0, 100, Values.knobTypes.Percent, Color.Yellow);
			initParam(6, 17, "Er Num.", 11, 0, 18, Values.knobTypes.IntegerPlusOne, Color.Yellow);
			initParam(7, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Yellow);
			initParam(8, 18, "FB.Gain", 106, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(9, 19, "Hi.Ratio", 7, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(10, 20, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(11, 21, "LPF", 96, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			for (int n = 12; n < 32; n++)
			{
				initParam(n, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			break;
		}
		case 7:
		{
			_type = 7;
			EFXName = "MONO DELAY";
			initParam(0, 11, "Delay", 2500, 0, 27300, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(1, 12, "FB.Gain", 139, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(2, 13, "Hi.Ratio", 5, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(3, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Yellow);
			initParam(4, 14, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(5, 15, "LPF", 92, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(6, 16, "Sync", 0, 0, 1, Values.knobTypes.OnOff, Color.Red);
			initParam(7, 17, "Note", 6, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			for (int j = 8; j < 30; j++)
			{
				initParam(j, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			initParam(31, 42, "BPM", 120, 25, 300, Values.knobTypes.Integer, Color.White);
			break;
		}
		case 8:
		{
			_type = 8;
			EFXName = "STEREO DELAY";
			initParam(0, 11, "Delay L", 2500, 0, 13500, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(1, 12, "Delay R", 3750, 0, 13500, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "FB.G L", 139, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(3, 14, "FB.G R", 124, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(4, 15, "Hi.Ratio", 4, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(5, 16, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(6, 17, "LPF", 92, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(7, 18, "Sync", 0, 0, 1, Values.knobTypes.OnOff, Color.Red);
			initParam(8, 19, "Note L", 6, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			initParam(9, 20, "Note R", 8, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			for (int num4 = 10; num4 < 30; num4++)
			{
				initParam(num4, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			initParam(31, 42, "BPM", 120, 25, 300, Values.knobTypes.Integer, Color.White);
			break;
		}
		case 9:
		{
			_type = 9;
			EFXName = "MOD. DELAY";
			initParam(0, 11, "Delay", 2500, 0, 27250, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(1, 12, "FB.Gain", 137, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(2, 13, "Hi.Ratio", 4, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(3, 14, "Freq.", 16, 0, 799, Values.knobTypes.EFXModFreq, Color.Coral);
			initParam(4, 15, "Depth", 60, 0, 100, Values.knobTypes.Percent, Color.Coral);
			initParam(5, 16, "Wave", 0, 0, 1, Values.knobTypes.EFXModWave, Color.Coral);
			initParam(6, 17, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(7, 18, "LPF", 96, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(8, 19, "Sync", 0, 0, 1, Values.knobTypes.OnOff, Color.Red);
			initParam(9, 20, "Dly.Note", 6, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			initParam(10, 21, "Mod.Note", 10, 0, 12, Values.knobTypes.EFXModNote, Color.Red);
			for (int num = 11; num < 30; num++)
			{
				initParam(num, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			initParam(31, 42, "BPM", 120, 25, 300, Values.knobTypes.Integer, Color.White);
			break;
		}
		case 10:
		{
			_type = 10;
			EFXName = "DELAY LCR";
			initParam(0, 11, "Delay L", 2500, 0, 27300, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(1, 12, "Delay C", 7500, 0, 27300, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "Delay R", 5000, 0, 27300, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(3, 14, "FB.Dly", 7500, 0, 27300, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(4, 15, "Level L", 180, 0, 200, Values.knobTypes.EFXLevelPct, Color.LightGreen);
			initParam(5, 16, "Level C", 180, 0, 200, Values.knobTypes.EFXLevelPct, Color.LightGreen);
			initParam(6, 17, "Level R", 180, 0, 200, Values.knobTypes.EFXLevelPct, Color.LightGreen);
			initParam(7, 18, "FB.Gain", 139, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(8, 19, "Hi.Ratio", 5, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(9, 20, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(10, 21, "LPF", 101, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(11, 22, "Sync", 0, 0, 1, Values.knobTypes.OnOff, Color.Red);
			initParam(12, 23, "Note L", 6, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			initParam(13, 24, "Note C", 10, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			initParam(14, 25, "Note R", 9, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			initParam(15, 26, "Note FB", 10, 0, 14, Values.knobTypes.EFXNote, Color.Red);
			for (int l = 16; l < 30; l++)
			{
				initParam(l, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			initParam(31, 42, "BPM", 120, 25, 300, Values.knobTypes.Integer, Color.White);
			break;
		}
		case 11:
		{
			_type = 11;
			EFXName = "ECHO";
			initParam(0, 11, "Delay L", 1760, 0, 13500, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(1, 12, "Delay R", 2460, 0, 13500, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(2, 13, "FB.Dly L", 2460, 0, 13500, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(3, 14, "FB.Dly R", 1760, 0, 13500, Values.knobTypes.Div10MS, Color.Yellow);
			initParam(4, 15, "FB.G L", 149, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(5, 16, "FB.G R", 49, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(6, 17, "L->R FBG", 134, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(7, 18, "R->L FBG", 134, 0, 198, Values.knobTypes.EFXFBGain, Color.LightGreen);
			initParam(8, 19, "Hi.Ratio", 3, 0, 9, Values.knobTypes.EFXDiv10, Color.LightGreen);
			initParam(9, 20, "HPF", 0, 0, 104, Values.knobTypes.EFXHPF, Color.White);
			initParam(10, 21, "LPF", 83, 0, 101, Values.knobTypes.EFXLPF, Color.White);
			initParam(11, 22, "Sync", 0, 0, 1, Values.knobTypes.OnOff, Color.Red);
			initParam(12, 23, "Note L", 4, 0, 13, Values.knobTypes.EFXNote, Color.Red);
			initParam(13, 24, "Note R", 6, 0, 13, Values.knobTypes.EFXNote, Color.Red);
			initParam(14, 25, "Note FBL", 6, 0, 13, Values.knobTypes.EFXNote, Color.Red);
			initParam(15, 26, "Note FBR", 4, 0, 13, Values.knobTypes.EFXNote, Color.Red);
			for (int i = 16; i < 30; i++)
			{
				initParam(i, 0, "", 0, 0, 0, Values.knobTypes.None, Color.Black);
			}
			initParam(30, 41, "Mix", 100, 0, 100, Values.knobTypes.Percent, Color.White);
			initParam(31, 42, "BPM", 120, 25, 300, Values.knobTypes.Integer, Color.White);
			break;
		}
		}
	}

	public void setParam(string source, string param, int value)
	{
		string text;
		if ((text = param.ToUpper()) != null && text == "BYPASS" && value >= 0 && value <= 1)
		{
			_bypass = value == 1;
			if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
			{
				MixEngine.setParam("EFXBYPASS", Rack, value);
			}
			if (source != "UI")
			{
				MixEngine.PushEFXParam(Rack, "EFXBYPASS", 0);
			}
		}
	}

	public void setParam(string source, Pr param, int value)
	{
		switch (param)
		{
		case Pr.BPM:
		{
			if (value > 300 || value < 25 || bpm == value)
			{
				break;
			}
			int num = bpm;
			bpm = value;
			if (source != "CONSOLE" && source != "LOGIC")
			{
				if (MixEngine.connStatus != 0)
				{
					if (!changedParams.Contains(Pr.BPM))
					{
						changedParams.Add(Pr.BPM);
					}
					if (!MixEngine.updateRack.Contains(Rack))
					{
						MixEngine.updateRack.Add(Rack);
					}
				}
				int type = _type;
				if (type == 7 && pValue[6] == 1)
				{
					double value2 = (double)pValue[0] * 0.1 * ((double)num / (double)bpm);
					setParamByIndex("LOGIC", 0, (int)(Math.Round(value2, 1) * 10.0));
				}
			}
			MixEngine.PushEFXParam(Rack, "BPM", 0);
			break;
		}
		case Pr.Bypass:
			if (_bypass != (value == 1))
			{
				_bypass = value == 1;
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.setParam("EFX.BYPASS", Rack, value);
				}
				MixEngine.PushEFXParam(Rack, "BYPASS", 0);
			}
			break;
		case Pr.Mix:
			if (value > 100 || value < 0 || _mix == value)
			{
				break;
			}
			_mix = value;
			if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
			{
				if (!changedParams.Contains(Pr.Mix))
				{
					changedParams.Add(Pr.Mix);
				}
				if (!MixEngine.updateRack.Contains(Rack))
				{
					MixEngine.updateRack.Add(Rack);
				}
			}
			MixEngine.PushEFXParam(Rack, "MIX", 0);
			break;
		case Pr.Type:
			if (value <= 67 && value >= 0)
			{
				setEFXType(value);
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.setParam("EFX.TYPE", Rack, value);
				}
				MixEngine.PushEFXParam(Rack, "TYPE", 0);
			}
			break;
		}
	}
}
