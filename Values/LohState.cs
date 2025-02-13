using Kinetic.App;
using Loh.Runtime;

namespace Loh.Values;

public sealed unsafe class LohState
{

	public List<object> DebugASM = new List<object>();
	public int* Code;
	public int* Lines;
	public int Top, Capacity;
	public Union[] Values;
	public int TopV, CapacityV;
	public LohTable Table = new LohTable();
	public LohClosure TopFunc;

	void RecheckCap()
	{
		if(Capacity < Top + 1)
		{
			int oldcap = Capacity;
			Capacity = NativeMem.MemGetNextCap(oldcap);
			Code = NativeMem.MemReallocate(Code, Capacity, oldcap);
			Lines = NativeMem.MemReallocate(Lines, Capacity, oldcap);
		}
	}

	public void Tail(VMOP b, int line)
	{
		RecheckCap();

		Code[Top] = (int) b;
		Lines[Top] = line;
		Top++;

		DebugASM.Add($"{line}: {b}");
	}

	public void Tail(int b, int line)
	{
		RecheckCap();

		Code[Top] = b;
		Lines[Top] = line;
		Top++;

		DebugASM.Add($"{line}: {b}");
	}

	public int PushConst(Union value)
	{
		if(CapacityV < TopV + 1)
		{
			int oldcap = CapacityV;
			CapacityV = NativeMem.MemGetNextCap(oldcap);
			Values = NativeMem.MemReallocate(Values, CapacityV);
		}

		Values[TopV] = value;
		TopV++;

		return TopV - 1;
	}

}
