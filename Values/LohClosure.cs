namespace Loh.Values;

public class LohClosure
{

	public LohFunc Func;
	public LohUpvalue[] Upvalues;
	public int UpvalCount;

	public LohClosure(LohFunc fn)
	{
		Func = fn;
		Upvalues = new LohUpvalue[UpvalCount = fn.UpvalCount];
	}

	public override string ToString()
	{
		return Func.ToString();
	}

}
