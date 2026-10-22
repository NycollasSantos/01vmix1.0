using System;
using faderspeak;

namespace AirFader;

public class OutPort
{
	public enum Pr
	{
		None,
		Attenuation,
		DelayOn,
		DelayTime,
		Phase
	}

	public int index;

	public bool _delayOn;

	public int _delayTime;

	public bool _phase;

	public int _att;

	public OutportTypes type
	{
		get
		{
			if (Console.type == ConsoleTypes.LS9)
			{
				if (index < 16)
				{
					return OutportTypes.Omni;
				}
				if (index < 48)
				{
					return OutportTypes.Slot;
				}
				return OutportTypes.TwoTR;
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				if (index < 16)
				{
					return OutportTypes.Omni;
				}
				if (index < 64)
				{
					return OutportTypes.Slot;
				}
				return OutportTypes.TwoTR;
			}
			return OutportTypes.Omni;
		}
	}

	public int subindex
	{
		get
		{
			if (Console.type == ConsoleTypes.LS9)
			{
				if (index < 16)
				{
					return index;
				}
				if (index < 48)
				{
					return index - 16;
				}
				return index - 48;
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				if (index < 16)
				{
					return index;
				}
				if (index < 64)
				{
					return index - 16;
				}
				return index - 64;
			}
			return 0;
		}
	}

	public int getCount => Console.maxOutOmni + Console.maxOutSlot + Console.maxOut2TR;

	public string hardname
	{
		get
		{
			if (Console.type == ConsoleTypes.LS9)
			{
				if (index < MixEngine.portNameLS9.Length)
				{
					return MixEngine.portNameLS9[index];
				}
				return "";
			}
			if (Console.type == ConsoleTypes.M7CL)
			{
				if (index < MixEngine.portNameM7CL.Length)
				{
					return MixEngine.portNameM7CL[index];
				}
				return "";
			}
			return "";
		}
	}

	public int getParam(Pr param)
	{
		switch (param)
		{
		case Pr.Attenuation:
			return _att;
		case Pr.DelayOn:
			if (!_delayOn)
			{
				return 0;
			}
			return 1;
		case Pr.DelayTime:
			return _delayTime;
		case Pr.Phase:
			if (!_phase)
			{
				return 0;
			}
			return 1;
		default:
			return 0;
		}
	}

	public bool getParamBool(Pr param)
	{
		return getParam(param) > 0;
	}

	public void setParam(string source, Pr param, bool value)
	{
		setParam(source, param, value ? 1 : 0);
	}

	public void setParam(string source, Pr param, int value)
	{
		try
		{
			source = source.ToUpper();
			GeneralParam generalParam = new GeneralParam();
			generalParam.param = GenParam.GEN_NONE;
			switch (param)
			{
			case Pr.Attenuation:
				if (value < -960)
				{
					value = -960;
				}
				if (value > 240)
				{
					value = 240;
				}
				_att = value;
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.setParam("OutPortAtt", index, _att);
				}
				MixEngine.OnAction("OutPortAtt", index);
				if (MixEngine.connPhase != ConnectionPhase.Syncing)
				{
					generalParam.param = GenParam.GEN_OUT_ATT;
					generalParam.index = index;
					generalParam.request = false;
					generalParam.value.Add(_att);
					MixEngine.faderServer.sendProto(source, generalParam);
				}
				break;
			case Pr.DelayOn:
				_delayOn = value > 0;
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.setParam("OutPortDelayOn", index, _delayOn);
				}
				MixEngine.OnAction("OutPortDelayOn", index);
				if (MixEngine.connPhase != ConnectionPhase.Syncing)
				{
					generalParam.param = GenParam.GEN_OUT_DELAYON;
					generalParam.index = index;
					generalParam.request = false;
					generalParam.value.Add(_delayOn ? 1 : 0);
					MixEngine.faderServer.sendProto(source, generalParam);
				}
				break;
			case Pr.DelayTime:
				if (value < 0)
				{
					value = 0;
				}
				if (value > 60000)
				{
					value = 60000;
				}
				_delayTime = value;
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.setParam("OutPortDelayTime", index, _delayTime);
				}
				MixEngine.OnAction("OutPortDelayTime", index);
				if (MixEngine.connPhase != ConnectionPhase.Syncing)
				{
					generalParam.param = GenParam.GEN_OUT_DELAYTIME;
					generalParam.index = index;
					generalParam.request = false;
					generalParam.value.Add(_delayTime);
					MixEngine.faderServer.sendProto(source, generalParam);
				}
				break;
			case Pr.Phase:
				_phase = value > 0;
				if (source != "CONSOLE" && source != "LOGIC" && MixEngine.connStatus != 0)
				{
					MixEngine.setParam("OutPortPhase", index, _phase);
				}
				MixEngine.OnAction("OutPortPhase", index);
				if (MixEngine.connPhase != ConnectionPhase.Syncing)
				{
					generalParam.param = GenParam.GEN_OUT_PHASE;
					generalParam.index = index;
					generalParam.request = false;
					generalParam.value.Add(_phase ? 1 : 0);
					MixEngine.faderServer.sendProto(source, generalParam);
				}
				break;
			}
		}
		catch (Exception)
		{
		}
	}

	public OutPort(int Index, OutportTypes PortType)
	{
		index = Index;
	}
}
