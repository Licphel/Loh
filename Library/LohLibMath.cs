using KryptonM.IDM;
using KryptonM.Maths;
using Loh.Values;

namespace Loh.Library;

public class LohLibMath
{

    public static void Load()
    {
        LohTable o = new LohTable();

        o.Put("sin", v => v.Return(Math.Sin(v.GetFloat(0))));
        o.Put("cos", v => v.Return(Math.Cos(v.GetFloat(0))));
        o.Put("tan", v => v.Return(Math.Tan(v.GetFloat(0))));
        o.Put("sinh", v => v.Return(Math.Sinh(v.GetFloat(0))));
        o.Put("cosh", v => v.Return(Math.Cosh(v.GetFloat(0))));
        o.Put("tanh", v => v.Return(Math.Tanh(v.GetFloat(0))));
        o.Put("asin", v => v.Return(Math.Asin(v.GetFloat(0))));
        o.Put("acos", v => v.Return(Math.Acos(v.GetFloat(0))));
        o.Put("atan", v => v.Return(Math.Atan(v.GetFloat(0))));
        o.Put("atan2", v => v.Return(Math.Atan2(v.GetFloat(0), v.GetFloat(1))));
        o.Put("asinh", v => v.Return(Math.Asinh(v.GetFloat(0))));
        o.Put("acosh", v => v.Return(Math.Acosh(v.GetFloat(0))));
        o.Put("atanh", v => v.Return(Math.Atanh(v.GetFloat(0))));

        o.Put("abs", v => v.Return(Math.Abs(v.GetFloat(0))));
        o.Put("sgn", v => v.Return(Math.Sign(v.GetFloat(0))));
        o.Put("loge", v => v.Return(Math.Log(v.GetFloat(0))));
        o.Put("log2", v => v.Return(Math.Log2(v.GetFloat(0))));
        o.Put("log10", v => v.Return(Math.Log10(v.GetFloat(0))));

        o.Put("min", v => v.Return(Math.Min(v.GetFloat(0), v.GetFloat(1))));
        o.Put("max", v => v.Return(Math.Max(v.GetFloat(0), v.GetFloat(1))));
        o.Put("roundc", v => v.Return(Math.Ceiling(v.GetFloat(0))));
        o.Put("roundf", v => v.Return(Math.Floor(v.GetFloat(0))));
        o.Put("round", v => v.Return(Math.Round(v.GetFloat(0))));
        o.Put("exp", v => v.Return(Math.Exp(v.GetFloat(0))));
        o.Put("sqrt", v => v.Return(Math.Sqrt(v.GetFloat(0))));

        o.Put("pi", Math.PI);
        o.Put("tau", Math.Tau);
        o.Put("dtr", FloatMath.DTR);
        o.Put("rtd", FloatMath.RTD);
        o.Put("e", Math.E);

        LohEngine.LoadedScripts[new ID("lang/math.loh")] = new LohState { Table = o };
    }

}