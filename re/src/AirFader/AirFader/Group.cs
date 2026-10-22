using System.Collections.Generic;

namespace AirFader;

public class Group
{
	public bool ha;

	public bool eq;

	public bool dyn1;

	public bool dyn2;

	public bool mixLevelOn;

	public bool mixLevelSend;

	public bool matrixLevelOn;

	public bool matrixLevelSend;

	public bool fader;

	public bool on;

	public bool insert;

	public bool directout;

	public bool att;

	public List<int> members;

	public bool[] mixSend = new bool[16];

	public bool[] matrixSend = new bool[8];

	public Group()
	{
		ha = true;
		dyn1 = true;
		dyn2 = true;
		eq = true;
		on = true;
		fader = true;
		insert = true;
		directout = true;
		att = true;
		mixLevelSend = true;
		mixLevelOn = true;
		matrixLevelSend = true;
		matrixLevelOn = true;
		for (int i = 0; i < 16; i++)
		{
			mixSend[i] = true;
		}
		for (int j = 0; j < 8; j++)
		{
			matrixSend[j] = true;
		}
		members = new List<int>();
	}
}
