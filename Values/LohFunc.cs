namespace Loh.Values;

public sealed class LohFunc
{

	public int Arity;
	public int UpvalCount;
	public LohState State;
	public string Name;

	public override string ToString()
	{
		return $"F<{Name}>";
	}

}
