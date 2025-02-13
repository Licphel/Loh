namespace Loh.Values;

public class LohUpvalue
{

	public int Slot;
	public Union Location;
	public LohUpvalue Next;

	public LohUpvalue(in Union loc, int slot)
	{
		Slot = slot;
		Location = loc;
	}

}
