namespace Loh.Values;

public sealed class LohFunc
{

    public int Arity;
    public string Name;
    public LohState State;
    public int UpvalCount;

    public override string ToString()
    {
        return $"F<{Name}>";
    }

}