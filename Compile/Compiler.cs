using KryptonM;
using Loh.Lexing;
using Loh.Runtime;
using Loh.Values;

namespace Loh.Compile;

public unsafe class Compiler
{

    private readonly Queue<int> BreakJumps = new Queue<int>();
    private LohTable ConstTable = new LohTable();
    private readonly Queue<int> ContiJumps = new Queue<int>();

    private Lexeme CurClassName;
    private readonly string FileName;

    private FrameTrace FrameNow;
    private readonly HashSet<string> Globalvars = new HashSet<string>();
    private int Index;

    private readonly List<Lexeme> Lexemes;

    private int Line;
    private readonly Queue<int> Loopstarts = new Queue<int>();
    private readonly Dictionary<Token, Rule> RuleMap;
    private string Src;

    public Compiler(string file, List<Lexeme> lexemes, string src)
    {
        Src = src;
        Lexemes = lexemes;
        RuleMap = Rule.GetRuleMap(this);
        FileName = file;

        LohException.PreCrash(() => FileName, () => Line);
    }

    private LohState State => FrameNow.Function.State;

    private bool End => Current.Type == Token.Eof;
    private Lexeme Current => Lexemes[Index];
    private Lexeme Previous2 => Lexemes[Index - 1];
    private Lexeme Previous => Lexemes[Index - 1];
    private Lexeme Next => Lexemes[Index + 1];

    public LohFunc Compile()
    {
        NewFrame(new Lexeme(Token.Ident, "__main__", Union.Null, 0), 1);

        if(!Check(Token.Eof))
            while(!Match(Token.Eof))
                Declaration();

        return EndCompile();
    }

    private void NewFrame(Lexeme name, int fntype) // Fntype: 0 - function, 1 - mainbody, 2 - method, 3 - constructor.
    {
        FrameTrace enc = FrameNow;
        FrameNow = new FrameTrace();
        FrameNow.Enclosing = enc;
        FrameNow.FnType = fntype;
        FrameNow.Function = new LohFunc();
        FrameNow.Function.State = new LohState();
        FrameNow.Function.State.TopFunc = new LohClosure(FrameNow.Function); // Give it a reference
        FrameNow.Function.Name = name.Portion;
        if(enc != null)
            FrameNow.Function.State.Table = FrameNow.Enclosing.Function.State.Table;

        // Get an empty slot.
        // This is for calls, to store a function local variable.
        FrameNow.TryExpandLocal(FrameNow.LocalCount);
        ref Local local = ref FrameNow.Locals[FrameNow.LocalCount];
        local.Name = name;
        local.Depth = FrameNow.ScopeDepth;
        FrameNow.LocalCount++;
    }

    private LohFunc EndCompile()
    {
        EmitConst(Union.Null);
        Emit(VMOP.Return);
        LohFunc fn = FrameNow.Function;
        FrameNow = FrameNow.Enclosing;
        return fn;
    }

    private void BeginScope()
    {
        FrameNow.ScopeDepth++;
    }

    private void EndScope()
    {
        FrameNow.ScopeDepth--;
        PopVars();
    }

    private void PopVars(int dec = 0)
    {
        while(FrameNow.LocalCount > 0 && FrameNow.TopLocal.Depth > FrameNow.ScopeDepth)
        {
            if(FrameNow.TopLocal.Captured)
                Emit(VMOP.StoUpval);
            else
                EmitPop();
            FrameNow.LocalCount--;
        }
    }

    public void Declaration()
    {
        if(Match(Token.Const))
        {
            if(Match(Token.Function))
            {
                DecFunction(true);
            }
            else if(Check(Token.Ident))
            {
                DecVariable(true);
            }
            else
            {
                Index--;
                Statement();
            }
        }
        else if(Match(Token.Local))
        {
            if(Match(Token.Function))
            {
                DecFunction(false);
            }
            else if(Check(Token.Ident))
            {
                DecVariable(false);
            }
            else
            {
                Index--;
                Statement();
            }
        }
        else
        {
            Statement();
        }
    }

    private void DecVariable(bool global)
    {
        Lexeme name = Consume(Token.Ident);
        if(global)
        {
            if(Globalvars.Contains(name.Portion))
                // Some fixed variables cannot be checked, like classes.
                // We're doing the best to keep it simple.
                throw LohException.Compiling($"Fixed variable {Previous.Portion} has already been defined.");
            Globalvars.Add(name.Portion);
            if(Match(Token.EqAssign))
                Expression();
            else
                EmitConst(Union.Null);
            SetVariable(name, 1);
            Emit(VMOP.StPop);
        }
        else
        {
            var id = LocalId(FrameNow, name);
            if(id >= 0)
                throw LohException.Compiling($"Local variable {Previous.Portion} has already been defined.");
            if(Match(Token.EqAssign))
                Expression();
            else
                EmitConst(Union.Null);
            SetVariable(name, 0);
            //Do not pop! We will left it in slot!
        }
    }

    private void DecFunction(bool global)
    {
        Lexeme name = Consume(Token.Ident);
        NewFunction(0, name, global);
        SetVariable(name, global ? 1 : 0);
    }

    private void NewFunction(int type, Lexeme name, bool global)
    {
        NewFrame(name, type);
        BeginScope();

        Consume(Token.Paren1);

        // Function parameters
        if(!Check(Token.Paren2))
            do
            {
                FrameNow.Function.Arity++;
                // Considering that calling previously helped us push the arguments,
                // We don't need to slot locals, only parsing them is ok.
                ParseVariable(Consume(Token.Ident), false);
            } while(Match(Token.Comma));

        Consume(Token.Paren2);
        StmBlock(false);

        FrameTrace frame = FrameNow; // Get a copy, we haven't got upvalues yet.
        LohFunc fn = EndCompile();
        Emit(VMOP.Close, MakeConst(Union.GetFromObject(fn)));
        // Should be followed by a set local or global

        for(var i = 0; i < fn.UpvalCount; i++)
        {
            Emit(frame.Upvalues[i].IsLocal ? 1 : 0);
            Emit(frame.Upvalues[i].Index);
        }
    }

    public void Statement()
    {
        if(Match(Token.Dataonly))
        {
            CompilerDataonly cdp = new CompilerDataonly(Lexemes, Index);
            var o = cdp.Parse();
            EmitConst(Union.GetFromObject(o));
            Emit(VMOP.Return);
        }
        else if(Match(Token.Do))
        {
            BeginScope();
            StmBlock(false);
            EndScope();
        }
        else if(Match(Token.If))
        {
            StmIf();
        }
        else if(Match(Token.While))
        {
            StmWhile();
        }
        else if(Match(Token.For))
        {
            StmFor();
        }
        else if(Match(Token.Foreach))
        {
            StmForeach();
        }
        else if(Match(Token.Break))
        {
            StmBreak();
        }
        else if(Match(Token.Continue))
        {
            StmContinue();
        }
        else if(Match(Token.Return))
        {
            StmReturn();
        }
        else
        {
            StmExpression();
        }
    }

    private void StmReturn()
    {
        Expression();
        Emit(VMOP.Return);
    }

    private void StmBreak()
    {
        BreakJumps.Enqueue(EmitJump(VMOP.Jmp));
    }

    private void ExitPatchControls()
    {
        while(BreakJumps.Count != 0)
            PatchJump(BreakJumps.Dequeue());
        var curloopstart = Loopstarts.Dequeue();
        while(ContiJumps.Count != 0)
            PatchJump(ContiJumps.Dequeue(), curloopstart);
    }

    private void StmContinue()
    {
        ContiJumps.Enqueue(EmitJump(VMOP.Jmp));
    }

    private void StmFor()
    {
        // Make sure the indexes cannot be used out of for.
        BeginScope();

        // Initializer
        ConsumeLoose(Token.Paren1);
        if(Match(Token.Comma))
        {
        }
        else if(Check(Token.Ident))
        {
            DecVariable(false);
        }
        else
        {
            StmExpression();
        }

        ConsumeLoose(Token.Comma);

        var loopstart = State.Top;
        Loopstarts.Enqueue(loopstart);
        var exitjmp = -1;

        // Comparison
        if(!Match(Token.Comma))
        {
            Expression();
            Consume(Token.Comma);
            exitjmp = EmitJump(VMOP.Jmpfn);
            EmitPop();
        }

        // Increment
        if(!Match(Token.Do) && !Match(Token.Paren2))
        {
            var bodyjmp = EmitJump(VMOP.Jmp);
            var incstart = State.Top;
            Expression();
            EmitPop();
            EmitLoop(loopstart);
            loopstart = incstart;
            Loopstarts.Dequeue();
            Loopstarts.Enqueue(loopstart);
            PatchJump(bodyjmp);
        }

        ConsumeLoose(Token.Paren2);
        ConsumeLoose(Token.Do);

        // Body
        StmBlock(false);
        EmitLoop(loopstart);
        if(exitjmp != -1)
        {
            PatchJump(exitjmp);
            ExitPatchControls();
            EmitPop();
        }

        EndScope();
    }

    private void StmForeach()
    {
        // Make sure the indexes cannot be used out of for.
        BeginScope();

        // Initializer
        ConsumeLoose(Token.Paren1);

        AddLocal(Consume(Token.Ident));
        var idk = LocalId(FrameNow, Previous);
        EmitConst(Union.Null);
        Emit(VMOP.SetLocal, idk);

        Consume(Token.Comma);

        AddLocal(Consume(Token.Ident));
        var idv = LocalId(FrameNow, Previous);
        EmitConst(Union.Null);
        Emit(VMOP.SetLocal, idv);

        Lexeme ilex = new Lexeme(Token.Ident, $"__itr{FrameNow.ScopeDepth}__", Union.Null, Line);
        AddLocal(ilex);
        var idi = LocalId(FrameNow, ilex);
        EmitConst(new Union(0));
        SetVariable(ilex, 0);

        ConsumeLoose(Token.Comma);

        var loopstart = State.Top;

        // Comparison
        Consume(Token.In);
        Expression();
        Emit(VMOP.GetTabLe);
        Emit(VMOP.GetLocal, idi);
        Emit(VMOP.Equal);
        var exitjmp = EmitJump(VMOP.Jmpf);
        EmitPop();
        // here the table is still in stack.
        Emit(VMOP.GetTabKv);
        Emit(idi);
        Emit(idk);
        Emit(idv);

        // Increment
        var bodyjmp = EmitJump(VMOP.Jmp);
        var incstart = State.Top;
        Emit(VMOP.GetLocal, idi);
        EmitConst(new Union(1));
        Emit(VMOP.Add);
        Emit(VMOP.SetLocal, idi);
        EmitPop();
        EmitLoop(loopstart);
        loopstart = incstart;
        Loopstarts.Enqueue(loopstart);
        PatchJump(bodyjmp);

        ConsumeLoose(Token.Paren2);
        Consume(Token.Do);

        // Body
        StmBlock(false);
        EmitLoop(loopstart);
        if(exitjmp != -1)
        {
            PatchJump(exitjmp);
            ExitPatchControls();
            EmitPop();
        }

        EndScope();
    }

    private void StmWhile()
    {
        var start = State.Top;
        Loopstarts.Enqueue(start);

        ConsumeLoose(Token.Paren1);
        Expression();

        var exitj = EmitJump(VMOP.Jmpfn);
        EmitPop(); // Pop the expression result

        ConsumeLoose(Token.Paren2);
        Consume(Token.Do);

        BeginScope();
        StmBlock(false);

        EmitLoop(start);

        PatchJump(exitj);
        ExitPatchControls();
        EmitPop();

        EndScope();
    }

    private void StmIf()
    {
        List<int> jmps = new List<int>();

        do
        {
            ConsumeLoose(Token.Paren1);
            Expression();
            ConsumeLoose(Token.Paren2);
            var thenj = EmitJump(VMOP.Jmpfn); // Jump to next branch if false.
            EmitPop();
            Consume(Token.Do);
            BeginScope();
            StmBlock(true);
            EndScope();
            jmps.Add(EmitJump(VMOP.Jmp)); // Get out of all branches.
            PatchJump(thenj); // Get into next branch.
            EmitPop();
        } while(Match(Token.If));

        if(Previous.Type == Token.Else)
        {
            Consume(Token.Do);
            BeginScope();
            StmBlock(false);
            EndScope();
        }

        foreach(var jmp in jmps)
            PatchJump(jmp);
    }

    public void StmBlock(bool isinif)
    {
        // If here's an if controlling, matching 'end' means a block's end.
        while(!(isinif ? Check(Token.End) || Check(Token.Else) : Check(Token.End))
              && !Check(Token.Eof))
            Declaration();
        Advance();
    }

    public void StmExpression()
    {
        Expression();
        EmitPop();
    }

    public void Expression()
    {
        ExGetPrecedence(CPriority.Assign);
    }

    public void ExAnomyFunc(bool assign)
    {
        NewFunction(0, new Lexeme(Token.Ident, "__anomy__", Union.Null, Line), false);
    }

    public void ExRequire(bool assign)
    {
        Consume(Token.Paren1);
        Lexeme path = Consume(Token.String);
        Union lib = LohEngine.RequirePack(path.Value.Dynamic);
        EmitConst(lib);
        LohTable tb = State.Table;
        LohTable tblib = lib.Dynamic;

        // Put all library's variables into the fixed.
        /*
        HashSet<string> set = new HashSet<string>();
        foreach(var kv in tblib)
        {
            if(set.Contains(kv.Key))
                throw LohException.Compiling("Required libraries have some overlaps.");
            set.Add(kv.Key);
            tb[kv.Key] = kv.Value;
        }
        */

        Consume(Token.Paren2);
    }

    public void ExDot(bool assign)
    {
        Consume(Token.Ident);
        var name = MakeConstIdent(Previous);

        if(Match(Token.Paren1))
        {
            var argc = ArgList();
            Emit(VMOP.TabCall, name);
            Emit(argc);
        }

        else if(assign && Match(Token.EqAssign))
        {
            Expression();
            Emit(VMOP.SetTable, name);
        }
        else
        {
            Emit(VMOP.GetTable, name);
        }
    }

    public void ExColon(bool assign)
    {
        Emit(VMOP.ReStore);
        Emit(VMOP.RePop); // Copy a stack value.
        Consume(Token.Ident);
        var name = MakeConstIdent(Previous);
        Consume(Token.Paren1);
        var argc = ArgList() + 1;
        Emit(VMOP.TabCall, name);
        Emit(argc);
    }

    public void ExArrOp(bool assign)
    {
        Expression();
        Consume(Token.Sqbra2);

        // We do not emit the last dot if we find '='. It is used to assign.
        if(assign && Match(Token.EqAssign))
        {
            Expression();
            Emit(VMOP.SetArray);
        }
        else
        {
            Emit(VMOP.GetArray);
        }
    }

    public void ExCall(bool assign)
    {
        var argc = ArgList();
        Emit(VMOP.Call, argc);
    }

    private int ArgList()
    {
        var argc = 0;
        if(!Check(Token.Paren2))
            do
            {
                Expression();
                argc++;
            } while(Match(Token.Comma));

        Consume(Token.Paren2);
        return argc;
    }

    public void ExAnd(bool assign)
    {
        var endj = EmitJump(VMOP.Jmpfn);
        EmitPop();
        ExGetPrecedence(CPriority.And);
        PatchJump(endj);
    }

    public void ExOr(bool assign)
    {
        var endj = EmitJump(VMOP.Jmpf);
        EmitPop();
        ExGetPrecedence(CPriority.And);
        PatchJump(endj);
    }

    public void ExTable(bool assign)
    {
        Emit(VMOP.Table);

        if(!Check(Token.Brace2))
        {
            Emit(VMOP.ReStore);

            do
            {
                Lexeme idt = Consume(Token.Ident);
                Consume(Token.EqAssign);
                Expression();
                var b = MakeConstIdent(idt);
                Emit(VMOP.SetTable, b);
                Emit(VMOP.StPop);
                if(Check(Token.Comma))
                    Emit(VMOP.ReLoad);
            } while(Match(Token.Comma));

            Emit(VMOP.RePop);
        }

        Consume(Token.Brace2);
    }

    public void ExArray(bool assign)
    {
        Emit(VMOP.Array);

        if(!Check(Token.Sqbra2))
        {
            Emit(VMOP.ReStore);
            var i = 0;
            do
            {
                EmitConst(new Union(i));
                Expression();
                Emit(VMOP.SetArray);
                Emit(VMOP.StPop);
                if(Check(Token.Comma))
                    Emit(VMOP.ReLoad);
                i++;
            } while(Match(Token.Comma));

            Emit(VMOP.RePop);
        }

        Consume(Token.Sqbra2);
    }

    public void ExGrouping(bool assign)
    {
        Expression();
        Consume(Token.Paren2);
    }

    public void ExBinary(bool assign)
    {
        Token key = Previous.Type;
        Rule rule = GetRule(key);
        ExGetPrecedence(rule.Priority + 1);

        switch(key)
        {
            case Token.Plus:
                Emit(VMOP.Add);
                break;
            case Token.Minus:
                Emit(VMOP.Sub);
                break;
            case Token.Star:
                Emit(VMOP.Mul);
                break;
            case Token.Slash:
                Emit(VMOP.Div);
                break;
            case Token.Power:
                Emit(VMOP.Pow);
                break;
            case Token.Mod:
                Emit(VMOP.Mod);
                break;
            case Token.NotEq:
                Emit(VMOP.NotEq);
                break;
            case Token.Equal:
                Emit(VMOP.Equal);
                break;
            case Token.Greater:
                Emit(VMOP.Greater);
                break;
            case Token.GreatEq:
                Emit(VMOP.GreatEq);
                break;
            case Token.Less:
                Emit(VMOP.Less);
                break;
            case Token.LessEq:
                Emit(VMOP.LessEq);
                break;
        }
    }

    public void ExUnary(bool assign)
    {
        Token key = Previous.Type;

        ExGetPrecedence(CPriority.Unary);

        switch(key)
        {
            case Token.Minus:
                Emit(VMOP.Neg);
                break;
            case Token.Exclam:
            case Token.Not:
                Emit(VMOP.Not);
                break;
        }
    }

    public void ExLiteral(bool assign)
    {
        switch(Previous.Type)
        {
            case Token.String:
                EmitConst(Previous.Value);
                break;
            case Token.Number:
                EmitConst(Previous.Value);
                break;
            case Token.True:
                EmitConst(Union.True);
                break;
            case Token.False:
                EmitConst(Union.False);
                break;
            case Token.Null:
                EmitConst(Union.Null);
                break;
        }
    }

    public void ExVariable(bool assign)
    {
        // It means that we are referring to a global variable (like fixed i).
        if(Previous.Type == Token.Const)
        {
            Consume(Token.Dot);
            Consume(Token.Ident);
            NamedVariable(Previous, 1, assign);
            return;
        }

        // It means that we are referring to a local variable (like local i).
        if(Previous.Type == Token.Local)
        {
            Consume(Token.Dot);
            Consume(Token.Ident);
            NamedVariable(Previous, 0, assign);
            return;
        }

        NamedVariable(Previous, 2, assign);
    }

    private void NamedVariable(Lexeme name, int site, bool assign) // site: 0 - local, 1 - global, 2 - unsure
    {
        VMOP op;
        var id = LocalId(FrameNow, name);

        if((site == 0 || site == 2) && id != -1)
        {
            if(assign && Match(Token.EqAssign))
            {
                Expression();
                SetVariable(name, 0);
                return;
            }

            Emit(VMOP.GetLocal, id);
        }
        else if((site == 0 || site == 2) && (id = UpvalueId(FrameNow, name)) != -1)
        {
            if(assign && Match(Token.EqAssign))
            {
                Expression();
                SetVariable(name, 2);
                return;
            }

            Emit(VMOP.GetUpval, id);
        }
        else if(site != 0)
        {
            id = MakeConstIdent(name);
            if(assign && Match(Token.EqAssign))
            {
                Expression();
                SetVariable(name, 1);
            }
            else
            {
                Emit(VMOP.GetFixed, id);
            }
        }
        else
        {
            throw LohException.Compiling($"Undeclared variable: {name.Portion}.");
        }
    }

    private int LocalId(FrameTrace frame, Lexeme name)
    {
        for(var i = frame.LocalCount - 1; i >= 0; i--)
        {
            Local local = frame.Locals[i];
            if(local.Name.Portion == name.Portion)
                return i;
        }

        return -1;
    }

    private bool AddLocal(Lexeme name)
    {
        var i = LocalId(FrameNow, name);
        if(i == -1)
        {
            FrameNow.TryExpandLocal(FrameNow.LocalCount);
            ref Local local = ref FrameNow.Locals[FrameNow.LocalCount];
            local.Name = name;
            local.Depth = FrameNow.ScopeDepth;
            FrameNow.LocalCount++;
            return false;
        }

        return true;
    }

    private int UpvalueId(FrameTrace frame, Lexeme name)
    {
        if(frame.Enclosing == null)
            return -1;

        var lid = LocalId(frame.Enclosing, name);
        if(lid != -1)
        {
            frame.Enclosing.Locals[lid].Captured = true;
            return AddUpvalue(frame, lid, true);
        }

        var uid = UpvalueId(frame.Enclosing, name);
        if(uid != -1)
            return AddUpvalue(frame, uid, false);

        return -1;
    }

    private int AddUpvalue(FrameTrace frame, int lid, bool local)
    {
        var upvalCount = frame.Function.UpvalCount;

        for(var i = 0; i < upvalCount; i++)
        {
            ref Upvalue upv = ref frame.Upvalues[i];
            if(upv.Index == lid && upv.IsLocal == local)
                return i;
        }

        frame.Upvalues[upvalCount].IsLocal = local;
        frame.Upvalues[upvalCount].Index = lid;
        return frame.Function.UpvalCount++;
    }

    private void SetVariable(Lexeme name, int type) // 0 - local, 1 - global, 2 - upvalue
    {
        var gid = ParseVariable(name, type == 1);

        if(type == 1)
            Emit(VMOP.SetFixed, gid);
        else if(type == 0)
            Emit(VMOP.SetLocal, LocalId(FrameNow, name));
        else if(type == 2)
            Emit(VMOP.SetUpval, UpvalueId(FrameNow, name));
    }

    private int ParseVariable(Lexeme name, bool global)
    {
        if(!global)
        {
            AddLocal(name);
            return 0; //Fake index
        }

        return MakeConstIdent(name);
    }

    private void ExGetPrecedence(CPriority p)
    {
        Advance();

        Action<bool> task0 = GetRule(Previous.Type).Prefix;
        if(task0 == null)
            throw LohException.Compiling("Expected expression.");

        task0(p <= CPriority.Assign);

        while(p <= GetRule(Current.Type).Priority)
        {
            Advance();
            task0 = GetRule(Previous.Type).Infix;
            task0(p <= CPriority.Assign);
        }
    }

    private Rule GetRule(Token key)
    {
        return RuleMap[key];
    }

    // Emitters.

    private void Emit(int b)
    {
        State.Tail(b, Line);
    }

    private void Emit(VMOP b)
    {
        State.Tail(b, Line);
    }

    private void Emit(VMOP b1, int b2)
    {
        State.Tail(b1, Line);
        State.Tail(b2, Line);
    }

    private void EmitLoop(int start)
    {
        Emit(VMOP.Jback);
        var offset = State.Top - start + 1;
        Emit(offset);
    }

    private int EmitJump(VMOP op)
    {
        Emit(op);
        Emit(0);
        return State.Top - 1;
    }

    private void PatchJump(int offset)
    {
        var jmp = State.Top - offset - 1;
        State.Code[offset] = jmp;
    }

    private void PatchJump(int offset, int dest)
    {
        var jmp = dest - offset - 1;
        State.Code[offset] = jmp;
    }

    private void EmitPop(int n = 1)
    {
        for(var i = 0; i < n; i++)
            Emit(VMOP.StPop);
    }

    private void EmitConst(Union o)
    {
        var i = MakeConst(o);
        Emit(VMOP.StPush, i);
    }

    private int MakeConst(Union o)
    {
        return State.PushConst(o);
    }

    private int MakeConstIdent(Lexeme name)
    {
        return MakeConst(Union.GetFromObject(name.Portion));
    }

    // Toolkit functions.

    private Lexeme Advance()
    {
        if(!End)
            Index++;

        Line = Previous.Line;
        return Previous;
    }

    private bool Match(params Token[] types)
    {
        foreach(Token type in types)
            if(Check(type))
            {
                Advance();
                return true;
            }

        return false;
    }

    private bool Check(Token type)
    {
        if(End && type == Token.Eof)
            return true;
        if(End)
            return false;
        return Current.Type == type;
    }

    private void ConsumeLoose(Token type)
    {
        if(Check(type))
            Advance();
    }

    private Lexeme Consume(Token type)
    {
        if(Check(type))
            return Advance();
        throw LohException.Compiling($"Expected {type.ToString()} but '{Current.Portion}'");
        // Exit compling
        Index = Lexemes.Count - 1;
        return Current;
    }

}

internal class Rule
{

    public Action<bool> Infix;

    public Action<bool> Prefix;
    public CPriority Priority;

    public static Rule MakeRule(Action<bool> pref, Action<bool> inf, CPriority prec)
    {
        return new Rule { Prefix = pref, Infix = inf, Priority = prec };
    }

    public static Dictionary<Token, Rule> GetRuleMap(Compiler compiler)
    {
        Dictionary<Token, Rule> ruleMap = new Dictionary<Token, Rule>();

        foreach(Token key in Enum.GetValues(typeof(Token))) ruleMap[key] = MakeRule(null, null, CPriority.None);

        // Usable keys
        ruleMap[Token.Number] = MakeRule(compiler.ExLiteral, null, CPriority.None);
        ruleMap[Token.String] = MakeRule(compiler.ExLiteral, null, CPriority.None);
        ruleMap[Token.True] = MakeRule(compiler.ExLiteral, null, CPriority.None);
        ruleMap[Token.False] = MakeRule(compiler.ExLiteral, null, CPriority.None);
        ruleMap[Token.Null] = MakeRule(compiler.ExLiteral, null, CPriority.None);

        ruleMap[Token.Ident] = MakeRule(compiler.ExVariable, null, CPriority.None);
        ruleMap[Token.Const] = MakeRule(compiler.ExVariable, null, CPriority.None);
        ruleMap[Token.Local] = MakeRule(compiler.ExVariable, null, CPriority.None);

        ruleMap[Token.Paren1] = MakeRule(compiler.ExGrouping, compiler.ExCall, CPriority.Call);
        ruleMap[Token.Brace1] = MakeRule(compiler.ExTable, null, CPriority.Call);
        ruleMap[Token.Sqbra1] = MakeRule(compiler.ExArray, compiler.ExArrOp, CPriority.Call);

        ruleMap[Token.Minus] = MakeRule(compiler.ExUnary, compiler.ExBinary, CPriority.Term);
        ruleMap[Token.Exclam] = MakeRule(compiler.ExUnary, null, CPriority.None);

        ruleMap[Token.Plus] = MakeRule(null, compiler.ExBinary, CPriority.Term);
        ruleMap[Token.Star] = MakeRule(null, compiler.ExBinary, CPriority.Factor);
        ruleMap[Token.Slash] = MakeRule(null, compiler.ExBinary, CPriority.Factor);
        ruleMap[Token.Power] = MakeRule(null, compiler.ExBinary, CPriority.Tight);
        ruleMap[Token.Mod] = MakeRule(null, compiler.ExBinary, CPriority.Tight);
        ruleMap[Token.NotEq] = MakeRule(null, compiler.ExBinary, CPriority.Equal);
        ruleMap[Token.Equal] = MakeRule(null, compiler.ExBinary, CPriority.Equal);
        ruleMap[Token.Greater] = MakeRule(null, compiler.ExBinary, CPriority.Compare);
        ruleMap[Token.GreatEq] = MakeRule(null, compiler.ExBinary, CPriority.Compare);
        ruleMap[Token.Less] = MakeRule(null, compiler.ExBinary, CPriority.Compare);
        ruleMap[Token.LessEq] = MakeRule(null, compiler.ExBinary, CPriority.Compare);

        ruleMap[Token.And] = MakeRule(null, compiler.ExAnd, CPriority.And);
        ruleMap[Token.Or] = MakeRule(null, compiler.ExOr, CPriority.Or);

        ruleMap[Token.Dot] = MakeRule(null, compiler.ExDot, CPriority.Call);
        ruleMap[Token.Colon] = MakeRule(null, compiler.ExColon, CPriority.Call);

        ruleMap[Token.Require] = MakeRule(compiler.ExRequire, null, CPriority.None);
        ruleMap[Token.Function] = MakeRule(compiler.ExAnomyFunc, null, CPriority.None);

        return ruleMap;
    }

}

internal struct Local
{

    public Lexeme Name;
    public int Depth;
    public bool Captured;

}

internal struct Upvalue
{

    public bool IsLocal;
    public int Index;

}

internal class FrameTrace
{

    public FrameTrace Enclosing;
    public int FnType; // 0 - Normal 1 - MainBody
    public LohFunc Function;
    public int LocalCount;
    public Local[] Locals = new Local[16];
    public int ScopeDepth;
    public Upvalue[] Upvalues = new Upvalue[16];
    public ref Local TopLocal => ref Locals[LocalCount - 1];

    public void TryExpandLocal(int slotw)
    {
        if(slotw >= Locals.Length) Locals = NativeMem.MemReallocate(Locals, NativeMem.MemGetNextCap(Locals.Length));
    }

    public void TryExpandUpval(int slotw)
    {
        if(slotw >= Upvalues.Length)
            Upvalues = NativeMem.MemReallocate(Upvalues, NativeMem.MemGetNextCap(Upvalues.Length));
    }

}