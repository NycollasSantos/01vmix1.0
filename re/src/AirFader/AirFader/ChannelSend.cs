namespace AirFader;

public class ChannelSend
{
	public int point;

	public int mix;

	public int pan;

	public int level;

	public bool on;

	public ChannelSend()
	{
		level = 0;
		on = true;
		point = 1;
		mix = 0;
		pan = 0;
	}

	public ChannelSend(int newmix)
	{
		level = 0;
		on = true;
		point = 1;
		mix = newmix;
		pan = 0;
	}
}
