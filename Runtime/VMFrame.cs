//#define VM_STACK_OP_TRACE

using System.Runtime.CompilerServices;
using Loh.Values;

namespace Loh.Runtime;

public unsafe class VMFrame
{

	public static int FrameLength = 32;
	public static int RegLength = 64;
	public static int StackLength = FrameLength * 16;
	public static int UpvalLength = 256;

	public CallFrame[] Frames;
	public CallFrame FrameNow;
	public int FrameCount;
	public Union[] Stack;
	public int Stacktop;
	public Union[] Reg;
	public int Regtop;
	public LohUpvalue OpenUpvals;
	public LohTable StateNow;
	public Arguments Arglist;

	public void Init()
	{
		Arglist = new Arguments(this);
		Frames = new CallFrame[FrameLength];
		for(int i = 0; i < FrameLength; i++)
			Frames[i] = new CallFrame();
		Reg = new Union[RegLength];
		Stack = new Union[StackLength];
	}

	public ref Union Evaluate(LohClosure o, params object[] objs)
	{
		Stack[Stacktop++] = Union.GetFromObject(o);

		CallFrame frame = Frames[FrameCount++];
		frame.Closure = o;
		frame.Ip = o.Func.State.Code;
		frame.Slots = Stacktop - 1;
		StateNow = o.Func.State.Table;

		foreach(object arg in objs)
		{
			Stack[Stacktop++] = Union.GetFromObject(arg);
		}

		return ref Run();
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	ref Union Run()
	{
		// Local temp for speeding up.

		LohTable Tablenow = this.StateNow;
		int* Ipnow;
		int Slotsnow;
		Union[] Constsnow;
		LohUpvalue[] Upvalsnow;
		Union v, v1, v2;
		int offset, argc;
		LohTable inst;
		string str;
		LohArray arr;
		object o;
		LohFunc fn;
		LohClosure clos;

#if VM_STACK_OP_TRACE
		Console.WriteLine("-------- Stack OP Trace --------");
#endif

		TakeFrame();

		while(true)
		{
			VMOP opcode = (VMOP) (*Ipnow++);

			switch(opcode)
			{
				case VMOP.StPop:
					--Stacktop;
					break;
				case VMOP.StPush:
					Stack[Stacktop++] = Constsnow[*Ipnow++];
					break;
				case VMOP.StCpy:
					Stack[Stacktop++] = Stack[Stacktop - 1];
					break;
				case VMOP.RePop:
					Stack[Stacktop++] = Reg[--Regtop];
					break;
				case VMOP.RePush:
					Reg[Regtop++] = Stack[--Stacktop];
					break;
				case VMOP.ReStore:
					Reg[Regtop++] = Stack[Stacktop - 1];
					break;
				case VMOP.ReLoad:
					Stack[Stacktop++] = Reg[Regtop - 1];
					break;
				case VMOP.GetLocal:
					Stack[Stacktop++] = Stack[Slotsnow + *Ipnow++];
					break;
				case VMOP.SetLocal:
					Stack[Slotsnow + *Ipnow++] = Stack[Stacktop - 1];
					break;
				case VMOP.GetUpval:
					Stack[Stacktop++] = Upvalsnow[*Ipnow++].Location;
					break;
				case VMOP.SetUpval:
					Upvalsnow[*Ipnow++].Location = Stack[Stacktop--];
					break;
				case VMOP.StoUpval:
					CloseUpvals(Stacktop - 1);
					--Stacktop;
					break;
				case VMOP.GetFixed:
					v = Constsnow[*Ipnow++];
					if(Tablenow.TryGetValue(v.ToString(), out v))
					{
						Stack[Stacktop++] = v;
						break;
					}
					throw LohException.Runtime($"Unknown variable: {v}.");
					break;
				case VMOP.SetFixed:
					Tablenow[Constsnow[*Ipnow++].ToString()] = Stack[Stacktop - 1];
					break;
				case VMOP.GetTable:
					inst = (LohTable) Stack[Stacktop - 1].Objref;
					str = (string) Constsnow[*Ipnow++].Objref;
					--Stacktop;
					if(inst.TryGetValue(str, out v))
						Stack[Stacktop++] = v;
					else
						throw LohException.Runtime($"Cannot find field {str} in object {inst}.");
					break;
				case VMOP.GetTabKv:
					inst = (LohTable) Stack[Stacktop - 1].Objref;
					--Stacktop;
					offset = Stack[Slotsnow + *Ipnow++].AsInt;
					str = inst.KeyArranged[offset];
					Stack[Slotsnow + *Ipnow++] = Union.GetFromObject(str);
					Stack[Slotsnow + *Ipnow++] = Union.GetFromObject(inst[str]);
					break;
				case VMOP.SetTable:
					inst = (LohTable) Stack[Stacktop - 2].Objref;
					str = (string) Constsnow[*Ipnow++].Objref;
					v = inst[str] = Stack[--Stacktop];
					--Stacktop;
					Stack[Stacktop++] = v;
					break;
				case VMOP.GetArray:
					o = Stack[Stacktop - 2].Objref;
					v = Stack[--Stacktop];
					--Stacktop;
					if(o is LohArray)
					{
						arr = (LohArray) o;
						Stack[Stacktop++] = arr[v.AsInt];
					}
					else if(o is LohTable)
					{
						inst = (LohTable) o;
						Stack[Stacktop++] = inst[v.AsString];
					}
					break;
				case VMOP.GetTabLe:
					// Do not pop the table, since it will be used to get k-v pairs.
					inst = (LohTable) Stack[Stacktop - 1].Objref;
					Stack[Stacktop++] = new Union(inst.Count);
					break;
				case VMOP.SetArray:
					o = Stack[Stacktop - 3].Objref;
					v = Stack[Stacktop - 2];
					if(o is LohArray)
					{
						arr = (LohArray) o;
						v = arr[v.AsInt] = Stack[Stacktop - 1];
					}
					else if(o is LohTable)
					{
						inst = (LohTable) o;
						v = inst[v.AsString] = Stack[Stacktop - 1];
					}
					Stacktop -= 3;
					Stack[Stacktop++] = v;
					break;
				case VMOP.Jmp:
					offset = *Ipnow++;
					Ipnow += offset;
					break;
				case VMOP.Jmpfn:
					offset = *Ipnow++;
					if(!Stack[Stacktop - 1].AsBool)
						Ipnow += offset;
					break;
				case VMOP.Jmpf:
					offset = *Ipnow++;
					if(Stack[Stacktop - 1].AsBool)
						Ipnow += offset;
					break;
				case VMOP.Jback:
					offset = *Ipnow++;
					Ipnow -= offset;
					break;
				case VMOP.Call:
					argc = *Ipnow++;
					FrameNow.Ip = Ipnow;
					CheckedCall(Stack[Stacktop - 1 - argc], argc);
					TakeFrame();
					break;
				case VMOP.Return:
					v = Stack[--Stacktop];
					CloseUpvals(Slotsnow);
					FrameCount--;
					Stacktop = Slotsnow;

					if(FrameCount == 0)
					{
#if VM_STACK_OP_TRACE
						DebugPrint((VMOP) opcode);
						Console.WriteLine("------- Stack OP Trace End -------");
#endif

						return ref v;
					}

					Stack[Stacktop++] = v;
					TakeFrame();
					break;
				case VMOP.Close:
					fn = (LohFunc) Constsnow[*Ipnow++].Objref;
					clos = new LohClosure(fn);
					Stack[Stacktop++] = Union.GetFromObject(clos);
					for(int i = 0; i < clos.UpvalCount; i++)
					{
						int local = *Ipnow++;
						int id = *Ipnow++;
						if(local == 1)
							clos.Upvalues[i] = CaptureUpval(Stack[Slotsnow + id], Slotsnow + id);
						else
							clos.Upvalues[i] = FrameNow.Closure.Upvalues[id];
					}
					break;
				case VMOP.TabCall:
					str = (string) Constsnow[*Ipnow++].Objref;
					argc = *Ipnow++;
					FrameNow.Ip = Ipnow;
					Invoke(str, argc);
					TakeFrame();
					break;
				case VMOP.Table:
					Stack[Stacktop++] = Union.GetFromObject(new LohTable());
					break;
				case VMOP.Array:
					Stack[Stacktop++] = Union.GetFromObject(new LohArray());
					break;
				case VMOP.Equal:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2 == v1);
					break;
				case VMOP.NotEq:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2 != v1);
					break;
				case VMOP.Greater:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat > v1.AsFloat);
					break;
				case VMOP.Less:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat < v1.AsFloat);
					break;
				case VMOP.GreatEq:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat >= v1.AsFloat);
					break;
				case VMOP.LessEq:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat <= v1.AsFloat);
					break;
				case VMOP.Neg:
					v = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(-v.AsFloat);
					break;
				case VMOP.Not:
					v = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(!v.AsBool);
					break;
				case VMOP.Add:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat + v1.AsFloat);
					break;
				case VMOP.Sub:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat - v1.AsFloat);
					break;
				case VMOP.Mul:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat * v1.AsFloat);
					break;
				case VMOP.Div:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat / v1.AsFloat);
					break;
				case VMOP.Mod:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(Math.Pow(v2.AsFloat, v1.AsFloat));
					break;
				case VMOP.Pow:
					v1 = Stack[--Stacktop];
					v2 = Stack[--Stacktop];
					Stack[Stacktop++] = new Union(v2.AsFloat % v1.AsFloat);
					break;
			}

#if VM_STACK_OP_TRACE
			DebugPrint((VMOP) opcode);
#endif
		}

#if VM_STACK_OP_TRACE
		void DebugPrint(VMOP opcode)
		{
			Console.Write($"{GetLine().ToString("0000")} {opcode}      \t [");
				int len = Stacktop;
				for(int i = 0; i < len; i++)
				{
					Console.Write(Stack[i] + (i == len - 1 ? "" : ", "));
				}
				Console.Write("]\n");
		}
#endif

		int GetLine()
		{
			int* ini = FrameNow.Closure.Func.State.Code;
			return FrameNow.Closure.Func.State.Lines[Ipnow - ini - 1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void TakeFrame()
		{
			FrameNow = Frames[FrameCount - 1];
			Ipnow = FrameNow.Ip;
			Constsnow = FrameNow.Closure.Func.State.Values;
			Slotsnow = FrameNow.Slots;
			Upvalsnow = FrameNow.Closure.Upvalues;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CloseUpvals(int last)
		{
			while(OpenUpvals != null && OpenUpvals.Slot >= last)
			{
				LohUpvalue upv = OpenUpvals;
				OpenUpvals = upv.Next;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		LohUpvalue CaptureUpval(in Union local, int slot)
		{
			LohUpvalue preupv = null;
			LohUpvalue upv = OpenUpvals;

			while(upv != null && upv.Slot > slot)
			{
				preupv = upv;
				upv = upv.Next;
			}

			if(upv != null && upv.Slot == slot)
				return upv;

			LohUpvalue madeupv = new LohUpvalue(local, slot);
			if(preupv == null)
				OpenUpvals = madeupv;
			else
				preupv.Next = madeupv;
			return madeupv;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void Invoke(string name, int argc)
		{
			o = Stack[Stacktop - argc - 1].Objref;

			if(o is LohTable)
			{
				inst = (LohTable) o;
				if(inst.TryGetValue(str, out Union propout))
				{
					Stack[Stacktop - argc - 1] = propout;
					CheckedCall(propout, argc);
				}
				else
				{
					throw LohException.Runtime($"Cannot find field: {name}");
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CheckedCall(in Union callee, int argc)
		{
			object o = callee.Objref;
#if VM_STACK_OP_TRACE
			Console.WriteLine($">> CALL INTO {o}");
#endif
			if(o is LohClosure)
			{
				CallClosed((LohClosure) o, argc);
				return;
			}
			if(o is LohFuncNative)
			{
				Union result = CallNative((LohFuncNative) o, argc);
				Stacktop -= argc + 1;
				Stack[Stacktop++] = result;
				return;
			}
			throw LohException.Runtime($"Attempt to call an uncallable object '{callee}'!");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CallClosed(in LohClosure c, int argc)
		{
			CallFrame frame = Frames[FrameCount++];
			frame.Closure = c;
			frame.Ip = c.Func.State.Code;
			frame.Slots = Stacktop - argc - 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Union CallNative(in LohFuncNative fn, int argc)
		{
			Arglist.Arity = argc;
			Arglist.Stacktop0 = Stacktop;
			Arglist.Self = Stack[Stacktop - 2];
			fn.NativeFn(Arglist);
			Union u = Arglist.ReturnVal;
			Arglist.ReturnVal = Union.Null;
			return u;
		}
	}

}

public unsafe class CallFrame
{

	public LohClosure Closure;
	public int* Ip;
	public int Slots;

}
