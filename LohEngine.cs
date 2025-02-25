using KryptonM.IDM;
using KryptonM.IO;
using Loh.Compile;
using Loh.Lexing;
using Loh.Library;
using Loh.Runtime;
using Loh.Values;

namespace Loh;

public class LohEngine
{

    private static VMFrame[] Frames;
    private static int Call0;
    public static Dictionary<ID, LohState> LoadedScripts;

    public static void Init()
    {
        Frames = new VMFrame[128];
        LoadedScripts = new Dictionary<ID, LohState>();

        for(var i = 0; i < Frames.Length; i++)
        {
            Frames[i] = new VMFrame();
            Frames[i].Init();
        }

        LohLibArray.Load();
        LohLibOs.Load();
        LohLibMath.Load();
        LohLibTable.Load();
        LohLibString.Load();
        LohLibCoroutine.Load();
    }

    public static LohState Require(string name, string code = null, bool load = true)
    {
        ID idt = new ID(name);

        if(load && LoadedScripts.TryGetValue(idt, out LohState inst))
            return inst;

        if(code == null)
            code = StringIO.Read(idt.File);
        if(code == null)
            return null;
        List<Lexeme> lst = new Lexer().Analyse(idt.Full, code);
        Compiler c = new Compiler(idt.Full, lst, code);
        LohClosure fn = new LohClosure(c.Compile());

        // Run once when compiled.
        Exec(fn);

        if(load)
            LoadedScripts[idt] = fn.Func.State;

        return fn.Func.State;
    }

    public static Union RequirePack(string name)
    {
        ID idt = new ID(name);

        if(LoadedScripts.TryGetValue(idt, out LohState inst))
            return Union.GetFromObject(inst.Table);

        var code = StringIO.Read(idt.File);
        List<Lexeme> lst = new Lexer().Analyse(idt.Full, code);
        Compiler c = new Compiler(idt.Full, lst, code);
        LohClosure fn = new LohClosure(c.Compile());

        // Run once when compiled.
        Exec(fn);

        return Union.GetFromObject((LoadedScripts[idt] = fn.Func.State).Table);
    }

    public static Union Exec(LohClosure fn, params object[] objs)
    {
        Call0++;
        VMFrame frame;
        if(Call0 < Frames.Length)
            frame = Frames[Call0];
        else
            frame = new VMFrame();
        Union mv = frame.Evaluate(fn, objs);
        Call0--;
        return mv;
    }

    public static dynamic Exec(string code)
    {
        return Exec(Require(null, code, false).TopFunc).Dynamic;
    }

    public static dynamic Exec(FileHandle path)
    {
        return Exec(Require(path.Path, StringIO.Read(path), false).TopFunc).Dynamic;
    }

    public static dynamic Exec(ID path)
    {
        return Exec(Require(path.File.Path, StringIO.Read(path.File), false).TopFunc).Dynamic;
    }

}