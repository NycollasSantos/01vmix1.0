namespace AirFader;

public class PEQ
{
	public delegate void PEQEventHandler();

	public int eqType;

	public bool eqOn;

	public bool eqHPFOn;

	public int eqHPFFreq;

	public int eqAtt;

	public int eq1Type;

	public bool eq1HPF;

	public int eq4Type;

	public bool eq4LPF;

	public int eq1Q;

	public int eq1F;

	public int eq1G;

	public int eq2Q;

	public int eq2F;

	public int eq2G;

	public int eq3Q;

	public int eq3F;

	public int eq3G;

	public int eq4Q;

	public int eq4F;

	public int eq4G;

	public bool isSet;

	public event PEQEventHandler Changed;

	protected void OnChange()
	{
		if (this.Changed != null)
		{
			this.Changed();
		}
	}

	public void Notify()
	{
		isSet = true;
		OnChange();
	}
}
