namespace Loh.Values;

public class LohUpvalue
{

    public Union Location;
    public LohUpvalue Next;

    public int Slot;

    public LohUpvalue(in Union loc, int slot)
    {
        Slot = slot;
        Location = loc;
    }

}