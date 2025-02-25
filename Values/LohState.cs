using KryptonM;
using Loh.Runtime;

namespace Loh.Values;

public sealed unsafe class LohState
{

    public int* Code;

    public List<object> DebugASM = new List<object>();
    public int* Lines;
    public LohTable Table = new LohTable();
    public int Top, Capacity;
    public LohClosure TopFunc;
    public int TopV, CapacityV;
    public Union[] Values;

    private void RecheckCap()
    {
        if(Capacity < Top + 1)
        {
            var oldcap = Capacity;
            Capacity = NativeMem.MemGetNextCap(oldcap);
            Code = NativeMem.MemReallocate(Code, Capacity, oldcap);
            Lines = NativeMem.MemReallocate(Lines, Capacity, oldcap);
        }
    }

    public void Tail(VMOP b, int line)
    {
        RecheckCap();

        Code[Top] = (int)b;
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
            var oldcap = CapacityV;
            CapacityV = NativeMem.MemGetNextCap(oldcap);
            Values = NativeMem.MemReallocate(Values, CapacityV);
        }

        Values[TopV] = value;
        TopV++;

        return TopV - 1;
    }

}