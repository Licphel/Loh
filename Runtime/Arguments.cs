using Loh.Values;

namespace Loh.Runtime;

public class Arguments
{

	public VMFrame Frame;
	public int Arity;
	public int Stacktop0;
	public Union ReturnVal;
	public Union Self;

	public Arguments(VMFrame frame)
	{
		Frame = frame;
	}

	public dynamic GetDynamic(int index)
	{
		return Frame.Stack[Stacktop0 - Arity + index].Boxed;
	}

	public Union GetRaw(int index)
	{
		return Frame.Stack[Stacktop0 - Arity + index];
	}

	public int GetInt(int index)
	{
		return Frame.Stack[Stacktop0 - Arity + index].AsInt;
	}

	public float GetFloat(int index)
	{
		return Frame.Stack[Stacktop0 - Arity + index].AsFloat;
	}

	public bool GetBool(int index)
	{
		return Frame.Stack[Stacktop0 - Arity + index].AsBool;
	}

	public string GetString(int index)
	{
		return Frame.Stack[Stacktop0 - Arity + index].AsString;
	}

	public T Get<T>(int index)
	{
		return (T) Frame.Stack[Stacktop0 - Arity + index].Boxed;
	}

	public void Return<T>(T o)
	{
		ReturnVal = Union.GetFromObject(o);
	}

}
