using System;
using System.Collections.Generic;
using faderspeak;

namespace AirFader;

public class Channel
{
	public enum Pr
	{
		None,
		HA,
		Phantom,
		Phase,
		Fader,
		RelativeHA,
		RelativeFader,
		On,
		Cue,
		Name,
		Pan,
		Balance,
		Group,
		Pair,
		BusType,
		PanLink,
		MuteGroup,
		DCAGroup,
		InsertOn,
		DirectOutOn,
		ToStereoOn,
		ToMonoOn,
		PatchIn,
		EQHPFOn,
		EQHPFFreq,
		EQAtt,
		EQType,
		EQOn,
		EQ1Type,
		EQ4Type,
		EQ1HPFOn,
		EQ4LPFOn,
		EQ1Q,
		EQ1F,
		EQ1G,
		EQ2Q,
		EQ2F,
		EQ2G,
		EQ3Q,
		EQ3F,
		EQ3G,
		EQ4Q,
		EQ4F,
		EQ4G,
		SendToMix,
		SendToMixOn,
		SendToMixPre,
		SendToMixPan,
		SendToMatrix,
		SendToMatrixOn,
		SendToMatrixPre,
		SendToMatrixPan,
		Dyn0On,
		Dyn0Type,
		Dyn0Attack,
		Dyn0Range,
		Dyn0Hold,
		Dyn0Decay,
		Dyn0Ratio,
		Dyn0Gain,
		Dyn0Knee,
		Dyn0Threshold,
		Dyn1On,
		Dyn1Type,
		Dyn1Attack,
		Dyn1Range,
		Dyn1Hold,
		Dyn1Release,
		Dyn1Ratio,
		Dyn1Gain,
		Dyn1Knee,
		Dyn1Threshold,
		EQFlatten,
		MeterPre,
		MeterDyn0In,
		MeterDyn1In,
		MeterDyn0GR,
		MeterDyn1GR,
		MeterEQIn,
		MeterEQOut,
		MeterPost
	}

	public delegate void ChannelEventHandler(int channel, string source, string property);

	public Dictionary<Pr, int> paramValue = new Dictionary<Pr, int>();

	public Dictionary<Pr, int> paramMin = new Dictionary<Pr, int>();

	public Dictionary<Pr, int> paramMax = new Dictionary<Pr, int>();

	public Dictionary<Pr, string> paramName = new Dictionary<Pr, string>();

	public Dictionary<string, Pr> nameParam = new Dictionary<string, Pr>();

	public int channel;

	public int subChannel;

	public bool IsHeld;

	public string _name;

	public string hardname;

	public ChannelTypes channelType;

	private ChannelSend[] _mix = new ChannelSend[16];

	private ChannelSend[] _matrix = new ChannelSend[8];

	public string name
	{
		get
		{
			return _name;
		}
		set
		{
			if (_name != value)
			{
				_name = value;
				OnChange("surface", "name");
			}
		}
	}

	public event ChannelEventHandler Changed;

	private void initParam(Pr param, int value, int min, int max, string name)
	{
		paramValue[param] = value;
		paramMin[param] = min;
		paramMax[param] = max;
		paramName[param] = name;
		nameParam[name] = param;
	}

	public void initParams()
	{
		initParam(Pr.BusType, 1, 0, 2, "BusType");
		initParam(Pr.PanLink, 0, 0, 1, "PanLink");
		initParam(Pr.Group, 0, 0, 40, "Group");
		initParam(Pr.Pair, 0, 0, 1, "Pair");
		initParam(Pr.Cue, 0, 0, 1, "Cue");
		initParam(Pr.MuteGroup, 0, 0, 1, "MuteGroup");
		initParam(Pr.DCAGroup, 0, 0, 1, "DCAGroup");
		initParam(Pr.PatchIn, channel + 1, 0, 178, "PatchIn");
		initParam(Pr.HA, 0, 0, 72, "HA");
		initParam(Pr.Fader, 0, 0, 1023, "Fader");
		initParam(Pr.RelativeHA, 0, 0, 72, "RelHA");
		initParam(Pr.RelativeFader, 0, 0, 1023, "RelFader");
		initParam(Pr.Phantom, 0, 0, 1, "Phantom");
		initParam(Pr.Phase, 0, 0, 1, "Phase");
		initParam(Pr.InsertOn, 0, 0, 1, "InsertOn");
		initParam(Pr.DirectOutOn, 0, 0, 1, "DirectOutOn");
		initParam(Pr.ToStereoOn, 0, 0, 1, "ToStereoOn");
		initParam(Pr.ToMonoOn, 0, 0, 1, "ToMonoOn");
		initParam(Pr.Fader, 0, 0, 1023, "Fader");
		initParam(Pr.On, 1, 0, 1, "On");
		initParam(Pr.Pan, 0, -63, 63, "Pan");
		initParam(Pr.Balance, 0, -63, 63, "Balance");
		initParam(Pr.Name, 0, 0, 8, "Name");
		initParam(Pr.SendToMix, 0, 0, 1023, "SendToMix");
		initParam(Pr.SendToMixOn, 0, 0, 1, "SendToMixOn");
		initParam(Pr.SendToMixPre, 0, 0, 1, "SendToMixPre");
		initParam(Pr.SendToMixPan, 0, -63, 63, "SendToMixPan");
		initParam(Pr.SendToMatrix, 0, 0, 1023, "SendToMatrix");
		initParam(Pr.SendToMatrixOn, 0, 0, 1, "SendToMatrixOn");
		initParam(Pr.SendToMatrixPre, 0, 0, 1, "SendToMatrixPre");
		initParam(Pr.SendToMatrixPan, 0, -63, 63, "SendToMatrixPan");
		initParam(Pr.Dyn0On, 0, 0, 1, "Dyn1On");
		initParam(Pr.Dyn0Type, 0, 0, 6, "Dyn1Type");
		initParam(Pr.Dyn0Attack, 0, 0, 120, "Dyn1Attack");
		initParam(Pr.Dyn0Range, -56, -70, 0, "Dyn1Range");
		initParam(Pr.Dyn0Hold, 60, 0, 215, "Dyn1Hold");
		initParam(Pr.Dyn0Decay, 44, 0, 159, "Dyn1Decay");
		initParam(Pr.Dyn0Ratio, 0, 0, 15, "Dyn1Ratio");
		initParam(Pr.Dyn0Gain, 0, 0, 180, "Dyn1Gain");
		initParam(Pr.Dyn0Knee, 0, 0, 89, "Dyn1Knee");
		initParam(Pr.Dyn0Threshold, -260, -540, 0, "Dyn1Threshold");
		initParam(Pr.Dyn1On, 0, 0, 1, "Dyn2On");
		initParam(Pr.Dyn1Type, 2, 0, 6, "Dyn2Type");
		initParam(Pr.Dyn1Attack, 30, 0, 120, "Dyn2Attack");
		initParam(Pr.Dyn1Range, -56, -70, 0, "Dyn2Range");
		initParam(Pr.Dyn1Hold, 60, 0, 215, "Dyn2Hold");
		initParam(Pr.Dyn1Release, 37, 0, 159, "Dyn2Release");
		initParam(Pr.Dyn1Ratio, 6, 0, 15, "Dyn2Ratio");
		initParam(Pr.Dyn1Gain, 0, 0, 180, "Dyn2Gain");
		initParam(Pr.Dyn1Knee, 2, 0, 89, "Dyn2Knee");
		initParam(Pr.Dyn1Threshold, -80, -540, 0, "Dyn2Threshold");
		initParam(Pr.EQFlatten, 0, 0, 1, "EQFlatten");
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			initParam(Pr.EQAtt, 0, -960, 120, "EQAtt");
		}
		else
		{
			initParam(Pr.EQAtt, 0, -960, 240, "EQAtt");
		}
		initParam(Pr.EQHPFOn, 0, 0, 1, "EQHPFOn");
		initParam(Pr.EQHPFFreq, 28, 4, 63, "EQHPFFreq");
		initParam(Pr.EQType, 0, 0, 1, "EQType");
		initParam(Pr.EQOn, 1, 0, 1, "EQOn");
		initParam(Pr.EQ1Type, 1, 0, 2, "EQ1Type");
		initParam(Pr.EQ4Type, 1, 0, 2, "EQ4Type");
		initParam(Pr.EQ1HPFOn, 0, 0, 1, "EQ1HPFOn");
		initParam(Pr.EQ4LPFOn, 0, 0, 1, "EQ4LPFOn");
		initParam(Pr.EQ1Q, 23, -4, 40, "EQ1Q");
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			initParam(Pr.EQ1F, 36, 5, 124, "EQ1F");
		}
		else
		{
			initParam(Pr.EQ1F, 36, 4, 124, "EQ1F");
		}
		initParam(Pr.EQ1G, 0, -180, 180, "EQ1G");
		initParam(Pr.EQ2Q, 23, -4, 40, "EQ2Q");
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			initParam(Pr.EQ2F, 72, 5, 124, "EQ2F");
		}
		else
		{
			initParam(Pr.EQ2F, 72, 4, 124, "EQ2F");
		}
		initParam(Pr.EQ2G, 0, -180, 180, "EQ2G");
		initParam(Pr.EQ3Q, 23, -4, 40, "EQ3Q");
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			initParam(Pr.EQ3F, 96, 5, 124, "EQ3F");
		}
		else
		{
			initParam(Pr.EQ3F, 96, 4, 124, "EQ3F");
		}
		initParam(Pr.EQ3G, 0, -180, 180, "EQ3G");
		initParam(Pr.EQ4Q, 23, -4, 40, "EQ4Q");
		if (Console.type == ConsoleTypes.O1V96V2)
		{
			initParam(Pr.EQ4F, 112, 5, 124, "EQ4F");
		}
		else
		{
			initParam(Pr.EQ4F, 112, 4, 124, "EQ4F");
		}
		initParam(Pr.EQ4G, 0, -180, 180, "EQ4G");
		initParam(Pr.MeterPre, 0, 0, 128, "MeterPre");
		initParam(Pr.MeterDyn0In, 0, 0, 128, "MeterDyn0");
		initParam(Pr.MeterDyn1In, 0, 0, 128, "MeterDyn1");
		initParam(Pr.MeterDyn0GR, 0, 0, 128, "MeterDyn0GR");
		initParam(Pr.MeterDyn1GR, 0, 0, 128, "MeterDyn1GR");
		initParam(Pr.MeterEQIn, 0, 0, 128, "MeterEQIn");
		initParam(Pr.MeterEQOut, 0, 0, 128, "MeterEQOut");
		initParam(Pr.MeterPost, 0, 0, 128, "MeterEqPost");
	}

	public void setParam(string source, Pr param, int index, bool value)
	{
		setParam(source, param, index, value ? 1 : 0);
	}

	public void setParam(string source, Pr param, int index, decimal value)
	{
		setParam(source, param, index, (int)Math.Round(value));
	}

	public void setParam(string source, Pr param, int index, int value)
	{
		try
		{
			source = source.ToUpper();
			int num = ((channel % 2 == 0) ? (channel + 1) : (channel - 1));
			int num2 = ((index % 2 == 0) ? (index + 1) : (index - 1));
			if (!paramMin.ContainsKey(param))
			{
				MixEngine.logDebug("setParam was called with " + param.ToString() + " which doesn't exist");
				return;
			}
			if (value < paramMin[param] || value > paramMax[param])
			{
				MixEngine.logDebug("Value " + value + " out of range for " + paramName[param] + " on Channel " + channel);
				return;
			}
			if (channelType == ChannelTypes.Stereo && source != "LOGIC")
			{
				if (subChannel == 0)
				{
					MixEngine.chData[channel + 1].setParam("LOGIC", param, index, value);
				}
				if (subChannel == 1)
				{
					MixEngine.chData[channel - 1].setParam("LOGIC", param, index, value);
				}
			}
			ChParam chParam = ChParam.CH_NONE;
			switch (param)
			{
			case Pr.MeterPre:
			case Pr.MeterDyn0In:
			case Pr.MeterDyn1In:
			case Pr.MeterDyn0GR:
			case Pr.MeterDyn1GR:
			case Pr.MeterEQIn:
			case Pr.MeterEQOut:
			case Pr.MeterPost:
				if (value <= 255)
				{
					paramValue[param] = value;
				}
				return;
			case Pr.SendToMatrix:
				_matrix[index].level = value;
				if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMatrix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				chParam = ChParam.CH_SENDTOMATRIX;
				break;
			case Pr.SendToMatrixOn:
				_matrix[index].on = value == 1;
				if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMatrix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				chParam = ChParam.CH_SENDTOMATRIXON;
				break;
			case Pr.SendToMatrixPre:
				_matrix[index].point = value;
				if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMatrix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				chParam = ChParam.CH_SENDTOMATRIXPRE;
				break;
			case Pr.SendToMatrixPan:
				_matrix[index].pan = value;
				_matrix[num2].pan = value;
				if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMatrix].getParamBool(Pr.PanLink))
				{
					MixEngine.chData[channel].setParam("LOGIC", Pr.Pan, 0, value);
					for (int n = 0; n < Console.maxMIX; n++)
					{
						if (source != "LOGIC" && MixEngine.chData[n + MixEngine.chOffsetMix].getParamBool(Pr.PanLink))
						{
							MixEngine.chData[channel].setParam("LOGIC", Pr.SendToMixPan, n, value);
						}
					}
					for (int num10 = 0; num10 < Console.maxMATRIX; num10++)
					{
						if (source != "LOGIC" && MixEngine.chData[num10 + MixEngine.chOffsetMatrix].getParamBool(Pr.PanLink) && num10 != index && num10 != num2)
						{
							MixEngine.chData[channel].setParam("LOGIC", Pr.SendToMatrixPan, num10, value);
						}
					}
				}
				chParam = ChParam.CH_SENDTOMATRIXPAN;
				break;
			case Pr.SendToMix:
				if (source != "LOGIC" && source != "CONSOLE" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.BusType) == 2)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, index, 823);
					return;
				}
				_mix[index].level = value;
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].mixLevelSend && MixEngine.chGroup[0].mixSend[index])
				{
					foreach (int member in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member != channel)
						{
							MixEngine.chData[member].setParam("LOGIC", param, index, value);
							if (MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
							{
								MixEngine.chData[member].setParam("LOGIC", param, num2, value);
							}
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, index, value);
					if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
					{
						MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
						MixEngine.chData[num].setParam("LOGIC", param, num2, value);
					}
				}
				else if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				chParam = ChParam.CH_SENDTOMIX;
				break;
			case Pr.SendToMixOn:
				_mix[index].on = value == 1;
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].mixLevelOn && MixEngine.chGroup[0].mixSend[index])
				{
					foreach (int member2 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member2 != channel)
						{
							MixEngine.chData[member2].setParam("LOGIC", param, index, value);
							if (MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
							{
								MixEngine.chData[member2].setParam("LOGIC", param, num2, value);
							}
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, index, value);
					if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
					{
						MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
						MixEngine.chData[num].setParam("LOGIC", param, num2, value);
					}
				}
				else if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				chParam = ChParam.CH_SENDTOMIXON;
				break;
			case Pr.SendToMixPre:
				_mix[index].point = value;
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, index, value);
					if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
					{
						MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
						MixEngine.chData[num].setParam("LOGIC", param, num2, value);
					}
				}
				else if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				chParam = ChParam.CH_SENDTOMIXPRE;
				break;
			case Pr.SendToMixPan:
				_mix[index].pan = value;
				_mix[num2].pan = value;
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].mixLevelOn && MixEngine.chGroup[0].mixSend[index])
				{
					foreach (int member3 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member3 != channel)
						{
							MixEngine.chData[member3].setParam("LOGIC", param, index, value);
						}
					}
				}
				if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParam(Pr.Pair) > 0)
				{
					MixEngine.chData[channel].setParam("LOGIC", param, num2, value);
				}
				if (source != "LOGIC" && MixEngine.chData[index + MixEngine.chOffsetMix].getParamBool(Pr.PanLink))
				{
					MixEngine.chData[channel].setParam("LOGIC", Pr.Pan, 0, value);
					for (int num17 = 0; num17 < Console.maxMIX; num17++)
					{
						if (source != "LOGIC" && MixEngine.chData[num17 + MixEngine.chOffsetMix].getParamBool(Pr.PanLink) && num17 != index && num17 != num2)
						{
							MixEngine.chData[channel].setParam("LOGIC", Pr.SendToMixPan, num17, value);
						}
					}
					for (int num18 = 0; num18 < Console.maxMATRIX; num18++)
					{
						if (source != "LOGIC" && MixEngine.chData[num18 + MixEngine.chOffsetMatrix].getParamBool(Pr.PanLink))
						{
							MixEngine.chData[channel].setParam("LOGIC", Pr.SendToMatrixPan, num18, value);
						}
					}
				}
				chParam = ChParam.CH_SENDTOMIXPAN;
				break;
			case Pr.Group:
				if (value == 0)
				{
					if (paramValue[Pr.Group] < 0 || paramValue[Pr.Group] >= MixEngine.chGroup.Length)
					{
						MixEngine.logError("Channel.setParam", "Group", "Old Group " + paramValue[Pr.Group] + " is out of range.");
						return;
					}
					MixEngine.chGroup[paramValue[Pr.Group]].members.Remove(channel);
				}
				else
				{
					if (paramValue[Pr.Group] < 0 || paramValue[Pr.Group] >= MixEngine.chGroup.Length)
					{
						MixEngine.logError("Channel.setParam", "Group", "Old Group " + paramValue[Pr.Group] + " is out of range.");
						return;
					}
					MixEngine.chGroup[paramValue[Pr.Group]].members.Remove(channel);
					if (value < 0 || value >= MixEngine.chGroup.Length)
					{
						MixEngine.logError("Channel.setParam", "Group", "New Group " + paramValue[Pr.Group] + " is out of range.");
						return;
					}
					if (!MixEngine.chGroup[value].members.Contains(channel))
					{
						MixEngine.chGroup[value].members.Add(channel);
					}
					if (source != "LOGIC")
					{
						int num3 = 0;
						if (MixEngine.chGroup[value].members.Count > 0)
						{
							num3 = MixEngine.chGroup[value].members[0];
						}
						foreach (int member4 in MixEngine.chGroup[value].members)
						{
							MixEngine.chData[member4].setParam("LOGIC", Pr.RelativeFader, 0, MixEngine.chData[member4].getParam(Pr.Fader));
							if (member4 == num3)
							{
								continue;
							}
							if (MixEngine.chGroup[0].dyn1)
							{
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0On, 0, MixEngine.chData[num3].getParam(Pr.Dyn0On));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Type, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Type));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Attack, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Attack));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Range, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Range));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Hold, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Hold));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Decay, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Decay));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Ratio, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Ratio));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Gain, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Gain));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Knee, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Knee));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn0Threshold, 0, MixEngine.chData[num3].getParam(Pr.Dyn0Threshold));
							}
							if (MixEngine.chGroup[0].dyn2)
							{
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1On, 0, MixEngine.chData[num3].getParam(Pr.Dyn1On));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Type, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Type));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Attack, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Attack));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Range, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Range));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Hold, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Hold));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Release, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Release));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Ratio, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Ratio));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Gain, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Gain));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Knee, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Knee));
								MixEngine.chData[member4].setParam("LOGIC", Pr.Dyn1Threshold, 0, MixEngine.chData[num3].getParam(Pr.Dyn1Threshold));
							}
							if (MixEngine.chGroup[0].eq)
							{
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQType, 0, MixEngine.chData[num3].getParam(Pr.EQType));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQOn, 0, MixEngine.chData[num3].getParam(Pr.EQOn));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQHPFOn, 0, MixEngine.chData[num3].getParam(Pr.EQHPFOn));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQHPFFreq, 0, MixEngine.chData[num3].getParam(Pr.EQHPFFreq));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQAtt, 0, MixEngine.chData[num3].getParam(Pr.EQAtt));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ1Type, 0, MixEngine.chData[num3].getParam(Pr.EQ1Type));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ1HPFOn, 0, MixEngine.chData[num3].getParam(Pr.EQ1HPFOn));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ4Type, 0, MixEngine.chData[num3].getParam(Pr.EQ4Type));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ4LPFOn, 0, MixEngine.chData[num3].getParam(Pr.EQ4LPFOn));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ1Q, 0, MixEngine.chData[num3].getParam(Pr.EQ1Q));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ1F, 0, MixEngine.chData[num3].getParam(Pr.EQ1F));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ1G, 0, MixEngine.chData[num3].getParam(Pr.EQ1G));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ2Q, 0, MixEngine.chData[num3].getParam(Pr.EQ2Q));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ2F, 0, MixEngine.chData[num3].getParam(Pr.EQ2F));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ2G, 0, MixEngine.chData[num3].getParam(Pr.EQ2G));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ3Q, 0, MixEngine.chData[num3].getParam(Pr.EQ3Q));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ3F, 0, MixEngine.chData[num3].getParam(Pr.EQ3F));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ3G, 0, MixEngine.chData[num3].getParam(Pr.EQ3G));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ4Q, 0, MixEngine.chData[num3].getParam(Pr.EQ4Q));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ4F, 0, MixEngine.chData[num3].getParam(Pr.EQ4F));
								MixEngine.chData[member4].setParam("LOGIC", Pr.EQ4G, 0, MixEngine.chData[num3].getParam(Pr.EQ4G));
							}
							if (MixEngine.chGroup[0].on)
							{
								MixEngine.chData[member4].setParam("LOGIC", Pr.On, 0, MixEngine.chData[num3].getParam(Pr.On));
							}
							if (MixEngine.chGroup[0].mixLevelSend)
							{
								for (int i = 0; i < Console.maxMIX; i++)
								{
									if (MixEngine.chGroup[0].mixSend[i])
									{
										MixEngine.chData[member4].setParam("LOGIC", Pr.SendToMix, i, MixEngine.chData[num3].getParam(Pr.SendToMix, i));
									}
								}
							}
							if (!MixEngine.chGroup[0].mixLevelOn)
							{
								continue;
							}
							for (int j = 0; j < Console.maxMIX; j++)
							{
								if (MixEngine.chGroup[0].mixSend[j])
								{
									MixEngine.chData[member4].setParam("LOGIC", Pr.SendToMixOn, j, MixEngine.chData[num3].getParam(Pr.SendToMixOn, j));
								}
							}
						}
					}
				}
				paramValue[param] = value;
				break;
			case Pr.Pair:
				paramValue[param] = value;
				if (!(source != "LOGIC") || MixEngine.connPhase == ConnectionPhase.PostSync)
				{
					break;
				}
				MixEngine.chData[num].setParam("LOGIC", Pr.Pair, 0, value);
				if (value == 1)
				{
					MixEngine.logDebug("Pairing channels with zero Balance...");
					MixEngine.chData[channel].setParam("LOGIC", Pr.Balance, 0, 0);
					MixEngine.chData[num].setParam("LOGIC", Pr.Balance, 0, 0);
					MixEngine.chData[num].setParam("LOGIC", Pr.Pan, 0, getParam(Pr.Pan));
					MixEngine.chData[num].setParam("LOGIC", Pr.Balance, 0, getParam(Pr.Balance));
					MixEngine.chData[num].setParam("LOGIC", Pr.InsertOn, 0, getParam(Pr.InsertOn));
					MixEngine.chData[num].setParam("LOGIC", Pr.On, 0, getParam(Pr.On));
					MixEngine.chData[num].setParam("LOGIC", Pr.Fader, 0, getParam(Pr.Fader));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQAtt, 0, getParam(Pr.EQAtt));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ1F, 0, getParam(Pr.EQ1F));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ1G, 0, getParam(Pr.EQ1G));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ1HPFOn, 0, getParam(Pr.EQ1HPFOn));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ1Q, 0, getParam(Pr.EQ1Q));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ1Type, 0, getParam(Pr.EQ1Type));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ2F, 0, getParam(Pr.EQ2F));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ2G, 0, getParam(Pr.EQ2G));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ2Q, 0, getParam(Pr.EQ2Q));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ3F, 0, getParam(Pr.EQ3F));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ3G, 0, getParam(Pr.EQ3G));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ3Q, 0, getParam(Pr.EQ3Q));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ4F, 0, getParam(Pr.EQ4F));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ4G, 0, getParam(Pr.EQ4G));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ4LPFOn, 0, getParam(Pr.EQ4LPFOn));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ4Q, 0, getParam(Pr.EQ4Q));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQ4Type, 0, getParam(Pr.EQ4Type));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQHPFFreq, 0, getParam(Pr.EQHPFFreq));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQHPFOn, 0, getParam(Pr.EQHPFOn));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQOn, 0, getParam(Pr.EQOn));
					MixEngine.chData[num].setParam("LOGIC", Pr.EQType, 0, getParam(Pr.EQType));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Type, 0, getParam(Pr.Dyn1Type));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Attack, 0, getParam(Pr.Dyn1Attack));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Gain, 0, getParam(Pr.Dyn1Gain));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Hold, 0, getParam(Pr.Dyn1Hold));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Knee, 0, getParam(Pr.Dyn1Knee));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1On, 0, getParam(Pr.Dyn1On));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Range, 0, getParam(Pr.Dyn1Range));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Ratio, 0, getParam(Pr.Dyn1Ratio));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Release, 0, getParam(Pr.Dyn1Release));
					MixEngine.chData[num].setParam("LOGIC", Pr.Dyn1Threshold, 0, getParam(Pr.Dyn1Threshold));
					for (int num11 = 0; num11 < 8; num11++)
					{
						MixEngine.chData[num].setParam("LOGIC", Pr.MuteGroup, 0, getParam(Pr.MuteGroup, num11));
					}
					MixEngine.chData[num].setParam("LOGIC", Pr.ToStereoOn, 0, getParam(Pr.ToStereoOn));
					MixEngine.chData[num].setParam("LOGIC", Pr.ToMonoOn, 0, getParam(Pr.ToMonoOn));
					if (channelType == ChannelTypes.Mix)
					{
						for (int num12 = 0; num12 < Console.maxInputs; num12++)
						{
							MixEngine.chData[num12].setParam("LOGIC", Pr.SendToMixOn, subChannel - subChannel % 2 + 1, MixEngine.chData[num12].getParam(Pr.SendToMixOn, subChannel - subChannel % 2));
							MixEngine.chData[num12].setParam("LOGIC", Pr.SendToMixPre, subChannel - subChannel % 2 + 1, MixEngine.chData[num12].getParam(Pr.SendToMixPre, subChannel - subChannel % 2));
							MixEngine.chData[num12].setParam("LOGIC", Pr.SendToMix, subChannel - subChannel % 2 + 1, MixEngine.chData[num12].getParam(Pr.SendToMix, subChannel - subChannel % 2));
						}
					}
					if (channelType == ChannelTypes.Matrix)
					{
						for (int num13 = 0; num13 < Console.maxInputs; num13++)
						{
							MixEngine.chData[num13].setParam("LOGIC", Pr.SendToMatrixOn, subChannel - subChannel % 2 + 1, MixEngine.chData[num13].getParam(Pr.SendToMatrixOn, subChannel - subChannel % 2));
							MixEngine.chData[num13].setParam("LOGIC", Pr.SendToMatrixPre, subChannel - subChannel % 2 + 1, MixEngine.chData[num13].getParam(Pr.SendToMatrixPre, subChannel - subChannel % 2));
							MixEngine.chData[num13].setParam("LOGIC", Pr.SendToMatrix, subChannel - subChannel % 2 + 1, MixEngine.chData[num13].getParam(Pr.SendToMatrix, subChannel - subChannel % 2));
						}
					}
					break;
				}
				if (channelType == ChannelTypes.Mix)
				{
					for (int num14 = 0; num14 < Console.maxInputs; num14++)
					{
						MixEngine.chData[num14].setParam("LOGIC", Pr.SendToMixPan, subChannel, 0);
					}
				}
				if (channelType == ChannelTypes.Matrix)
				{
					for (int num15 = 0; num15 < Console.maxInputs; num15++)
					{
						MixEngine.chData[num15].setParam("LOGIC", Pr.SendToMatrixPan, subChannel, 0);
					}
				}
				MixEngine.chData[channel].setParam("LOGIC", Pr.Pan, 0, 0);
				MixEngine.chData[num].setParam("LOGIC", Pr.Pan, 0, 0);
				break;
			case Pr.PanLink:
			{
				paramValue[param] = value;
				if (value != 1 || !(source != "LOGIC"))
				{
					break;
				}
				for (int num16 = 0; num16 < Console.maxInputs; num16++)
				{
					if (channelType == ChannelTypes.Mix)
					{
						MixEngine.chData[num16].setParam("LOGIC", Pr.SendToMixPan, subChannel, MixEngine.chData[num16].getParam(Pr.Pan));
					}
					else if (channelType == ChannelTypes.Matrix)
					{
						MixEngine.chData[num16].setParam("LOGIC", Pr.SendToMatrixPan, subChannel, MixEngine.chData[num16].getParam(Pr.Pan));
					}
				}
				break;
			}
			case Pr.PatchIn:
				paramValue[param] = value;
				break;
			case Pr.MuteGroup:
				if (value == 0)
				{
					MixEngine.muteGroup[index].members.Remove(channel);
				}
				else if (Console.type == ConsoleTypes.O1V96V2)
				{
					for (int k = 0; k < 8; k++)
					{
						if (MixEngine.muteGroup[k].members.Contains(channel))
						{
							if (k != index)
							{
								setParam("LOGIC", Pr.MuteGroup, k, value: false);
							}
						}
						else if (k == index)
						{
							MixEngine.muteGroup[index].members.Add(channel);
						}
					}
				}
				else if (!MixEngine.muteGroup[index].members.Contains(channel))
				{
					MixEngine.muteGroup[index].members.Add(channel);
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, index, value);
				}
				chParam = ChParam.CH_MUTEGROUP;
				break;
			case Pr.ToMonoOn:
				paramValue[param] = value;
				chParam = ChParam.CH_TOMONOON;
				break;
			case Pr.ToStereoOn:
				paramValue[param] = value;
				chParam = ChParam.CH_TOSTEREOON;
				break;
			case Pr.Pan:
			{
				paramValue[param] = value;
				chParam = ChParam.CH_PAN;
				for (int l = 0; l < Console.maxMIX; l++)
				{
					if (source != "LOGIC" && MixEngine.chData[l + MixEngine.chOffsetMix].getParamBool(Pr.PanLink))
					{
						MixEngine.chData[channel].setParam("LOGIC", Pr.SendToMixPan, l, value);
					}
				}
				for (int m = 0; m < Console.maxMATRIX; m++)
				{
					if (source != "LOGIC" && MixEngine.chData[m + MixEngine.chOffsetMatrix].getParamBool(Pr.PanLink))
					{
						MixEngine.chData[channel].setParam("LOGIC", Pr.SendToMatrixPan, m, value);
					}
				}
				break;
			}
			case Pr.Balance:
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				paramValue[param] = value;
				break;
			case Pr.On:
				paramValue[param] = value;
				if (source != "LOGIC")
				{
					if (channelType == ChannelTypes.Stereo && Console.LinkSTMono)
					{
						if (subChannel == 0 || subChannel == 1)
						{
							MixEngine.chData[MixEngine.chOffsetStereo + 2].setParam("LOGIC", param, index, value);
						}
						else if (subChannel == 2)
						{
							MixEngine.chData[MixEngine.chOffsetStereo].setParam("LOGIC", param, index, value);
							MixEngine.chData[MixEngine.chOffsetStereo + 1].setParam("LOGIC", param, index, value);
						}
					}
					if (paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].on)
					{
						foreach (int member5 in MixEngine.chGroup[paramValue[Pr.Group]].members)
						{
							if (member5 != channel)
							{
								MixEngine.chData[member5].setParam("LOGIC", param, 0, value);
							}
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				chParam = ChParam.CH_ON;
				break;
			case Pr.Fader:
				paramValue[param] = value;
				if (source != "LOGIC")
				{
					if (channelType == ChannelTypes.Stereo && Console.LinkSTMono)
					{
						if (MixEngine.chData[MixEngine.chOffsetStereo].IsHeld || MixEngine.chData[MixEngine.chOffsetStereo + 1].IsHeld || MixEngine.chData[MixEngine.chOffsetStereo + 2].IsHeld)
						{
							MixEngine.chData[MixEngine.chOffsetStereo].setParam("LOGIC", Pr.RelativeFader, 0, MixEngine.chData[MixEngine.chOffsetStereo].getParam(Pr.Fader));
							MixEngine.chData[MixEngine.chOffsetStereo + 1].setParam("LOGIC", Pr.RelativeFader, 0, MixEngine.chData[MixEngine.chOffsetStereo + 1].getParam(Pr.Fader));
							MixEngine.chData[MixEngine.chOffsetStereo + 2].setParam("LOGIC", Pr.RelativeFader, 0, MixEngine.chData[MixEngine.chOffsetStereo + 2].getParam(Pr.Fader));
						}
						else if (subChannel == 0 || subChannel == 1)
						{
							decimal num4 = MixEngine.dbMap[paramValue[Pr.RelativeFader]];
							decimal num5 = MixEngine.dbMap[MixEngine.chData[MixEngine.chOffsetStereo + 2].getParam(Pr.RelativeFader)];
							decimal find = MixEngine.dbMap[paramValue[Pr.Fader]] - (num4 - num5);
							MixEngine.chData[MixEngine.chOffsetStereo + 2].setParam("LOGIC", Pr.Fader, 0, MixEngine.FuzzyFind(MixEngine.dbMap, find));
						}
						else if (subChannel == 2)
						{
							decimal num6 = MixEngine.dbMap[paramValue[Pr.RelativeFader]];
							decimal num7 = MixEngine.dbMap[MixEngine.chData[MixEngine.chOffsetStereo].getParam(Pr.RelativeFader)];
							decimal find2 = MixEngine.dbMap[paramValue[Pr.Fader]] - (num6 - num7);
							MixEngine.chData[MixEngine.chOffsetStereo].setParam("LOGIC", Pr.Fader, 0, MixEngine.FuzzyFind(MixEngine.dbMap, find2));
							MixEngine.chData[MixEngine.chOffsetStereo + 1].setParam("LOGIC", Pr.Fader, 0, MixEngine.FuzzyFind(MixEngine.dbMap, find2));
						}
					}
					else if (paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].fader)
					{
						foreach (int member6 in MixEngine.chGroup[paramValue[Pr.Group]].members)
						{
							if (MixEngine.chData[channel].IsHeld)
							{
								MixEngine.chData[member6].setParam("LOGIC", Pr.RelativeFader, 0, MixEngine.chData[member6].getParam(Pr.Fader));
							}
							else if (member6 != channel)
							{
								decimal num8 = MixEngine.dbMap[paramValue[Pr.RelativeFader]];
								decimal num9 = MixEngine.dbMap[MixEngine.chData[member6].getParam(Pr.RelativeFader)];
								decimal find3 = MixEngine.dbMap[paramValue[Pr.Fader]] - (num8 - num9);
								MixEngine.chData[member6].setParam("LOGIC", Pr.Fader, 0, MixEngine.FuzzyFind(MixEngine.dbMap, find3));
							}
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				chParam = ChParam.CH_FADER;
				break;
			case Pr.Phantom:
				MixEngine.setParam(source, paramValue[Pr.PatchIn] - 1, MixEngine.Pr.Phantom, 0, value);
				chParam = ChParam.CH_PHANTOM;
				break;
			case Pr.Phase:
				paramValue[param] = value;
				chParam = ChParam.CH_PHASE;
				break;
			case Pr.HA:
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].ha && !IsHeld)
				{
					int num19 = 0;
					int num20 = paramMax[Pr.HA];
					foreach (int member7 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (MixEngine.chData[member7].getParam(Pr.HA) > num19)
						{
							num19 = MixEngine.chData[member7].getParam(Pr.HA);
						}
						if (MixEngine.chData[member7].getParam(Pr.HA) < num20)
						{
							num20 = MixEngine.chData[member7].getParam(Pr.HA);
						}
					}
					int num21 = value - getParam(Pr.HA);
					if (num19 + num21 > paramMax[Pr.HA])
					{
						num21 = paramMax[Pr.HA] - num19;
					}
					if (num20 + num21 < 0)
					{
						num21 = -num20;
					}
					value = getParam(Pr.HA) + num21;
					foreach (int member8 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member8 != channel)
						{
							MixEngine.chData[member8].setParam("LOGIC", Pr.HA, 0, num21 + MixEngine.chData[member8].getParam(Pr.HA));
							MixEngine.setParam(source, MixEngine.chData[member8].getParam(Pr.PatchIn) - 1, MixEngine.Pr.HA, 0, MixEngine.chData[member8].getParam(Pr.HA));
						}
					}
				}
				MixEngine.setParam(source, paramValue[Pr.PatchIn] - 1, MixEngine.Pr.HA, 0, value);
				chParam = ChParam.CH_HA;
				break;
			case Pr.Dyn0Type:
				paramValue[param] = value;
				if (source != "LOGIC" && Console.type != ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 0:
						setParam("CONSOLE", Pr.Dyn0Threshold, 0, -260);
						setParam("CONSOLE", Pr.Dyn0Range, 0, -56);
						setParam("CONSOLE", Pr.Dyn0Attack, 0, 0);
						setParam("CONSOLE", Pr.Dyn0Hold, 0, 60);
						setParam("CONSOLE", Pr.Dyn0Decay, 0, 44);
						break;
					case 1:
						setParam("CONSOLE", Pr.Dyn0Threshold, 0, -190);
						setParam("CONSOLE", Pr.Dyn0Range, 0, -22);
						setParam("CONSOLE", Pr.Dyn0Attack, 0, 93);
						setParam("CONSOLE", Pr.Dyn0Hold, 0, 202);
						setParam("CONSOLE", Pr.Dyn0Decay, 0, 113);
						break;
					case 2:
						setParam("CONSOLE", Pr.Dyn0Threshold, 0, -80);
						setParam("CONSOLE", Pr.Dyn0Ratio, 0, 6);
						setParam("CONSOLE", Pr.Dyn0Attack, 0, 30);
						setParam("CONSOLE", Pr.Dyn0Decay, 0, 37);
						setParam("CONSOLE", Pr.Dyn0Gain, 0, 0);
						setParam("CONSOLE", Pr.Dyn0Knee, 0, 2);
						break;
					case 3:
						setParam("CONSOLE", Pr.Dyn0Threshold, 0, -230);
						setParam("CONSOLE", Pr.Dyn0Ratio, 0, 4);
						setParam("CONSOLE", Pr.Dyn0Attack, 0, 1);
						setParam("CONSOLE", Pr.Dyn0Decay, 0, 11);
						setParam("CONSOLE", Pr.Dyn0Gain, 0, 35);
						setParam("CONSOLE", Pr.Dyn0Knee, 0, 2);
						break;
					}
					if (paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].dyn1)
					{
						foreach (int member9 in MixEngine.chGroup[paramValue[Pr.Group]].members)
						{
							if (member9 != channel)
							{
								MixEngine.chData[member9].setParam("LOGIC", param, 0, value);
							}
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				chParam = ChParam.CH_DYN0TYPE;
				break;
			case Pr.Dyn0On:
			case Pr.Dyn0Attack:
			case Pr.Dyn0Range:
			case Pr.Dyn0Hold:
			case Pr.Dyn0Decay:
			case Pr.Dyn0Ratio:
			case Pr.Dyn0Gain:
			case Pr.Dyn0Knee:
			case Pr.Dyn0Threshold:
				paramValue[param] = value;
				if (param == Pr.Dyn0Attack)
				{
					chParam = ChParam.CH_DYN0ATTACK;
				}
				if (param == Pr.Dyn0Decay)
				{
					chParam = ChParam.CH_DYN0DECAY;
				}
				if (param == Pr.Dyn0Gain)
				{
					chParam = ChParam.CH_DYN0GAIN;
				}
				if (param == Pr.Dyn0Hold)
				{
					chParam = ChParam.CH_DYN0HOLD;
				}
				if (param == Pr.Dyn0Knee)
				{
					chParam = ChParam.CH_DYN0KNEE;
				}
				if (param == Pr.Dyn0On)
				{
					chParam = ChParam.CH_DYN0ON;
				}
				if (param == Pr.Dyn0Range)
				{
					chParam = ChParam.CH_DYN0RANGE;
				}
				if (param == Pr.Dyn0Ratio)
				{
					chParam = ChParam.CH_DYN0RATIO;
				}
				if (param == Pr.Dyn0Threshold)
				{
					chParam = ChParam.CH_DYN0THRESHOLD;
				}
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].dyn1)
				{
					foreach (int member10 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member10 != channel)
						{
							MixEngine.chData[member10].setParam("LOGIC", param, 0, value);
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				break;
			case Pr.Dyn1Type:
				paramValue[param] = value;
				if (source != "LOGIC" && Console.type != ConsoleTypes.O1V96V2)
				{
					switch (value)
					{
					case 2:
						setParam("CONSOLE", Pr.Dyn1Threshold, 0, -80);
						setParam("CONSOLE", Pr.Dyn1Ratio, 0, 6);
						setParam("CONSOLE", Pr.Dyn1Attack, 0, 30);
						setParam("CONSOLE", Pr.Dyn1Release, 0, 37);
						setParam("CONSOLE", Pr.Dyn1Gain, 0, 0);
						setParam("CONSOLE", Pr.Dyn1Knee, 0, 2);
						break;
					case 3:
						setParam("CONSOLE", Pr.Dyn1Threshold, 0, -230);
						setParam("CONSOLE", Pr.Dyn1Ratio, 0, 4);
						setParam("CONSOLE", Pr.Dyn1Attack, 0, 1);
						setParam("CONSOLE", Pr.Dyn1Release, 0, 11);
						setParam("CONSOLE", Pr.Dyn1Gain, 0, 35);
						setParam("CONSOLE", Pr.Dyn1Knee, 0, 2);
						break;
					case 4:
						setParam("CONSOLE", Pr.Dyn1Threshold, 0, -100);
						setParam("CONSOLE", Pr.Dyn1Ratio, 0, 8);
						setParam("CONSOLE", Pr.Dyn1Attack, 0, 1);
						setParam("CONSOLE", Pr.Dyn1Release, 0, 37);
						setParam("CONSOLE", Pr.Dyn1Gain, 0, 180);
						setParam("CONSOLE", Pr.Dyn1Knee, 0, 5);
						break;
					case 5:
						setParam("CONSOLE", Pr.Dyn1Threshold, 0, -80);
						setParam("CONSOLE", Pr.Dyn1Ratio, 0, 9);
						setParam("CONSOLE", Pr.Dyn1Attack, 0, 25);
						setParam("CONSOLE", Pr.Dyn1Release, 0, 30);
						setParam("CONSOLE", Pr.Dyn1Gain, 0, 180);
						setParam("CONSOLE", Pr.Dyn1Knee, 0, 23);
						break;
					case 6:
						setParam("CONSOLE", Pr.Dyn1Threshold, 0, -80);
						setParam("CONSOLE", Pr.Dyn1Release, 0, 84);
						break;
					}
					if (paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].dyn1)
					{
						foreach (int member11 in MixEngine.chGroup[paramValue[Pr.Group]].members)
						{
							if (member11 != channel)
							{
								MixEngine.chData[member11].setParam("LOGIC", param, 0, value);
							}
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				chParam = ChParam.CH_DYN1TYPE;
				break;
			case Pr.Dyn1On:
			case Pr.Dyn1Attack:
			case Pr.Dyn1Range:
			case Pr.Dyn1Hold:
			case Pr.Dyn1Release:
			case Pr.Dyn1Ratio:
			case Pr.Dyn1Gain:
			case Pr.Dyn1Knee:
			case Pr.Dyn1Threshold:
				paramValue[param] = value;
				if (param == Pr.Dyn1Attack)
				{
					chParam = ChParam.CH_DYN1ATTACK;
				}
				if (param == Pr.Dyn1Gain)
				{
					chParam = ChParam.CH_DYN1GAIN;
				}
				if (param == Pr.Dyn1Hold)
				{
					chParam = ChParam.CH_DYN1HOLD;
				}
				if (param == Pr.Dyn1Knee)
				{
					chParam = ChParam.CH_DYN1KNEE;
				}
				if (param == Pr.Dyn1On)
				{
					chParam = ChParam.CH_DYN1ON;
				}
				if (param == Pr.Dyn1Range)
				{
					chParam = ChParam.CH_DYN1RANGE;
				}
				if (param == Pr.Dyn1Ratio)
				{
					chParam = ChParam.CH_DYN1RATIO;
				}
				if (param == Pr.Dyn1Release)
				{
					chParam = ChParam.CH_DYN1RELEASE;
				}
				if (param == Pr.Dyn1Threshold)
				{
					chParam = ChParam.CH_DYN1THRESHOLD;
				}
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].dyn2)
				{
					foreach (int member12 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member12 != channel)
						{
							MixEngine.chData[member12].setParam("LOGIC", param, 0, value);
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				break;
			case Pr.EQ1Q:
			case Pr.EQ2Q:
			case Pr.EQ3Q:
			case Pr.EQ4Q:
				if (value < 0)
				{
					value = 0;
				}
				goto case Pr.EQHPFOn;
			case Pr.EQHPFOn:
			case Pr.EQHPFFreq:
			case Pr.EQAtt:
			case Pr.EQType:
			case Pr.EQOn:
			case Pr.EQ1Type:
			case Pr.EQ4Type:
			case Pr.EQ1HPFOn:
			case Pr.EQ4LPFOn:
			case Pr.EQ1F:
			case Pr.EQ1G:
			case Pr.EQ2F:
			case Pr.EQ2G:
			case Pr.EQ3F:
			case Pr.EQ3G:
			case Pr.EQ4F:
			case Pr.EQ4G:
				paramValue[param] = value;
				if (source != "LOGIC" && paramValue[Pr.Group] != 0 && MixEngine.chGroup[0].eq)
				{
					foreach (int member13 in MixEngine.chGroup[paramValue[Pr.Group]].members)
					{
						if (member13 != channel)
						{
							MixEngine.chData[member13].setParam("LOGIC", param, 0, value);
						}
					}
				}
				if (source != "LOGIC" && paramValue[Pr.Pair] != 0)
				{
					MixEngine.chData[num].setParam("LOGIC", param, 0, value);
				}
				if (param == Pr.EQOn)
				{
					chParam = ChParam.CH_EQON;
				}
				if (param == Pr.EQAtt)
				{
					chParam = ChParam.CH_EQATT;
				}
				if (param == Pr.EQ1HPFOn)
				{
					chParam = ChParam.CH_EQ1HPFON;
				}
				if (param == Pr.EQ1Type)
				{
					chParam = ChParam.CH_EQ1TYPE;
				}
				if (param == Pr.EQ4LPFOn)
				{
					chParam = ChParam.CH_EQ4LPFON;
				}
				if (param == Pr.EQ4Type)
				{
					chParam = ChParam.CH_EQ4TYPE;
				}
				if (param == Pr.EQHPFFreq)
				{
					chParam = ChParam.CH_EQHPFFREQ;
				}
				if (param == Pr.EQHPFOn)
				{
					chParam = ChParam.CH_EQHPFON;
				}
				if (param == Pr.EQ1G)
				{
					chParam = ChParam.CH_EQ1G;
				}
				if (param == Pr.EQ2G)
				{
					chParam = ChParam.CH_EQ2G;
				}
				if (param == Pr.EQ3G)
				{
					chParam = ChParam.CH_EQ3G;
				}
				if (param == Pr.EQ4G)
				{
					chParam = ChParam.CH_EQ4G;
				}
				if (param == Pr.EQ1F)
				{
					chParam = ChParam.CH_EQ1F;
				}
				if (param == Pr.EQ2F)
				{
					chParam = ChParam.CH_EQ2F;
				}
				if (param == Pr.EQ3F)
				{
					chParam = ChParam.CH_EQ3F;
				}
				if (param == Pr.EQ4F)
				{
					chParam = ChParam.CH_EQ4F;
				}
				if (param == Pr.EQ1Q)
				{
					chParam = ChParam.CH_EQ1Q;
				}
				if (param == Pr.EQ2Q)
				{
					chParam = ChParam.CH_EQ2Q;
				}
				if (param == Pr.EQ3Q)
				{
					chParam = ChParam.CH_EQ3Q;
				}
				if (param == Pr.EQ4Q)
				{
					chParam = ChParam.CH_EQ4Q;
				}
				break;
			case Pr.EQFlatten:
				setParam(source, Pr.EQ1G, 0, 0);
				setParam(source, Pr.EQ2G, 0, 0);
				setParam(source, Pr.EQ3G, 0, 0);
				setParam(source, Pr.EQ4G, 0, 0);
				setParam(source, Pr.EQ1HPFOn, 0, 0);
				setParam(source, Pr.EQ4LPFOn, 0, 0);
				setParam(source, Pr.EQHPFOn, 0, 0);
				break;
			case Pr.DCAGroup:
				if (value == 0)
				{
					MixEngine.DCA[index].members.Remove(channel);
				}
				else if (!MixEngine.DCA[index].members.Contains(channel))
				{
					MixEngine.DCA[index].members.Add(channel);
				}
				chParam = ChParam.CH_DCAGROUP;
				break;
			case Pr.Cue:
				paramValue[param] = value;
				chParam = ChParam.CH_CUE;
				break;
			default:
				paramValue[param] = value;
				break;
			}
			if (MixEngine.connPhase != ConnectionPhase.Syncing)
			{
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.enQueue(param, channel, index);
				}
				MixEngine.PushChannel(param, channel, index, value);
				if (chParam != 0)
				{
					ChannelParam channelParam = new ChannelParam();
					channelParam.channel = channel;
					channelParam.index = index;
					channelParam.param = chParam;
					channelParam.value.Add(value);
					MixEngine.faderServer.sendProto(source, channelParam);
				}
			}
		}
		catch (Exception ex)
		{
			MixEngine.logError("Channel.setParam", "SetParam", ex.ToString());
		}
	}

	public void sendNet(string source, GenParam param, int index, int subindex, int value)
	{
		GeneralParam generalParam = new GeneralParam();
		generalParam.param = param;
		generalParam.index = index;
		generalParam.subindex = subindex;
		generalParam.value.Add(value);
		MixEngine.faderServer.sendProto(source, generalParam);
	}

	public void setParam(string source, Pr param, int index, string value)
	{
		source = source.ToUpper();
		if (!paramMin.ContainsKey(param))
		{
			MixEngine.logDebug("setParam was called with " + param.ToString() + " which doesn't exist");
			return;
		}
		if (value.Length < paramMin[param] || value.Length > paramMax[param])
		{
			MixEngine.logDebug("Value '" + value + "' out of range for " + paramName[param] + " on Channel " + channel);
			return;
		}
		if (param == Pr.Name)
		{
			_name = value;
			if (Console.type == ConsoleTypes.O1V96V2 && (channelType == ChannelTypes.STIN || channelType == ChannelTypes.Stereo) && source != "LOGIC")
			{
				if (channel % 2 == 0)
				{
					MixEngine.chData[channel + 1].setParam("LOGIC", param, index, value);
				}
				else
				{
					MixEngine.chData[channel - 1].setParam("LOGIC", param, index, value);
				}
			}
		}
		if (MixEngine.connPhase != ConnectionPhase.Syncing)
		{
			if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
			{
				MixEngine.enQueue(param, channel, index);
			}
			MixEngine.PushChannel(param, channel, index, -1);
			ChParam chParam = ChParam.CH_NONE;
			if (param == Pr.Name)
			{
				chParam = ChParam.CH_NAME;
			}
			if (chParam != 0)
			{
				ChannelParam channelParam = new ChannelParam();
				channelParam.channel = channel;
				channelParam.index = index;
				channelParam.param = chParam;
				channelParam.valuestr.Add(value);
				MixEngine.faderServer.sendProto(source, channelParam);
			}
		}
	}

	public int getParam(Pr param)
	{
		if (param == Pr.HA && paramValue[Pr.PatchIn] > 0 && paramValue[Pr.PatchIn] <= MixEngine.HA.Length)
		{
			return MixEngine.getParam(0, MixEngine.Pr.HA, paramValue[Pr.PatchIn] - 1);
		}
		if (param == Pr.Phantom && paramValue[Pr.PatchIn] > 0 && paramValue[Pr.PatchIn] <= MixEngine.HA.Length)
		{
			return MixEngine.getParam(0, MixEngine.Pr.Phantom, paramValue[Pr.PatchIn] - 1);
		}
		if (paramValue.ContainsKey(param))
		{
			return paramValue[param];
		}
		MixEngine.logDebug("getParam can't find " + param);
		return 0;
	}

	public bool getParamBool(Pr param)
	{
		if (param == Pr.Phantom && paramValue[Pr.PatchIn] > 0 && paramValue[Pr.PatchIn] <= MixEngine.HA.Length)
		{
			return MixEngine.getParam(0, MixEngine.Pr.Phantom, paramValue[Pr.PatchIn] - 1) == 1;
		}
		if (paramValue.ContainsKey(param))
		{
			return paramValue[param] == 1;
		}
		MixEngine.logDebug("getParamBool can't find " + param);
		return false;
	}

	public string getParamStr(Pr param)
	{
		switch (param)
		{
		case Pr.PatchIn:
			if (paramValue[Pr.PatchIn] >= 0 && paramValue[Pr.PatchIn] < MixEngine.mapInPatch.Length)
			{
				return MixEngine.mapInPatch[paramValue[Pr.PatchIn]];
			}
			return "";
		case Pr.Name:
			return _name;
		default:
			return "";
		}
	}

	public int getParam(Pr param, int index)
	{
		switch (param)
		{
		case Pr.SendToMixOn:
			if (!_mix[index].on)
			{
				return 0;
			}
			return 1;
		case Pr.SendToMix:
			return _mix[index].level;
		case Pr.SendToMixPre:
			return _mix[index].point;
		case Pr.SendToMixPan:
			return _mix[index].pan;
		case Pr.SendToMatrixOn:
			if (!_matrix[index].on)
			{
				return 0;
			}
			return 1;
		case Pr.SendToMatrix:
			return _matrix[index].level;
		case Pr.SendToMatrixPre:
			return _matrix[index].point;
		case Pr.SendToMatrixPan:
			return _matrix[index].pan;
		case Pr.MuteGroup:
			if (!MixEngine.muteGroup[index].members.Contains(channel))
			{
				return 0;
			}
			return 1;
		case Pr.DCAGroup:
			if (!MixEngine.DCA[index].members.Contains(channel))
			{
				return 0;
			}
			return 1;
		default:
			return getParam(param);
		}
	}

	public bool getParamBool(Pr param, int index)
	{
		switch (param)
		{
		case Pr.SendToMixOn:
			if (index >= 0 && index < _mix.Length)
			{
				return _mix[index].on;
			}
			break;
		case Pr.SendToMixPre:
			if (index >= 0 && index < _mix.Length)
			{
				return _mix[index].point != 0;
			}
			break;
		case Pr.SendToMatrixOn:
			if (index >= 0 && index < _matrix.Length)
			{
				return _matrix[index].on;
			}
			break;
		case Pr.SendToMatrixPre:
			if (index >= 0 && index < _matrix.Length)
			{
				return _matrix[index].point != 0;
			}
			break;
		case Pr.MuteGroup:
			if (index >= 0 && index < MixEngine.muteGroup.Length)
			{
				return MixEngine.muteGroup[index].members.Contains(channel);
			}
			break;
		case Pr.DCAGroup:
			if (index >= 0 && index < MixEngine.DCA.Length)
			{
				return MixEngine.DCA[index].members.Contains(channel);
			}
			break;
		default:
			return getParamBool(param);
		}
		return false;
	}

	public decimal getDB(Pr param)
	{
		if (paramValue[param] < 0 || paramValue[param] >= MixEngine.dbMap.Length)
		{
			return 0m;
		}
		return MixEngine.dbMap[paramValue[param]];
	}

	public Channel(int newchannel, ChannelTypes newType)
	{
		channelType = newType;
		channel = newchannel;
		setChannelType(newType);
		initParams();
		for (int i = 0; i < 16; i++)
		{
			_mix[i] = new ChannelSend(i);
		}
		for (int j = 0; j < 8; j++)
		{
			_matrix[j] = new ChannelSend(j);
		}
	}

	public void setChannelType(ChannelTypes newType)
	{
		channelType = newType;
		switch (newType)
		{
		case ChannelTypes.Input:
			subChannel = channel;
			_name = "Ch" + (subChannel + 1).ToString().PadLeft(2);
			break;
		case ChannelTypes.STIN:
			subChannel = channel;
			_name = "StIn" + ((subChannel - MixEngine.chOffsetSTIN) / 2 + 1) + ((subChannel % 2 == 0) ? "L" : "R");
			break;
		case ChannelTypes.Mix:
			subChannel = channel - MixEngine.chOffsetMix;
			_name = "Mx" + (subChannel + 1).ToString().PadLeft(2);
			break;
		case ChannelTypes.Matrix:
			subChannel = channel - MixEngine.chOffsetMatrix;
			_name = "Mt" + (subChannel + 1).ToString().PadLeft(2);
			break;
		case ChannelTypes.Stereo:
			subChannel = channel - MixEngine.chOffsetStereo;
			switch (subChannel)
			{
			case 0:
				_name = "St L";
				break;
			case 1:
				_name = "St R";
				break;
			case 2:
				_name = "Mono";
				break;
			case 3:
				_name = "Monitor";
				break;
			}
			break;
		case ChannelTypes.Monitor:
			subChannel = 0;
			_name = "Monitor";
			break;
		}
		hardname = name;
	}

	public void fireChange(string source, string property)
	{
		OnChange(source, property);
	}

	protected virtual void OnChange(string source, string property)
	{
		if (this.Changed != null)
		{
			this.Changed(channel, source, property);
		}
	}
}
