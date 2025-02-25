using KryptonM.IDM;
using Loh.Values;

namespace Loh.Library;

public class LohLibArray
{

    public static void Load()
    {
        LohTable o = new LohTable();

        o.Put("sizeof", v => v.Return(v.Get<LohArray>(0).Count));
        o.Put("insert", v => v.Get<LohArray>(0).Add(v.GetRaw(1)));
        o.Put("remove", v => v.Get<LohArray>(0).Remove(v.GetRaw(1)));
        o.Put("discard", v => v.Get<LohArray>(0).RemoveAt(v.GetInt(1)));
        o.Put("clear", v => v.Get<LohArray>(0).Clear());
        o.Put("copy", v =>
        {
            LohArray arr = v.Get<LohArray>(0);
            LohArray dst = new LohArray();
            for(var i = 0; i < arr.Count; i++)
                dst[i] = arr[i];
            v.Return(dst);
        });

        LohEngine.LoadedScripts[new ID("lang/array.loh")] = new LohState { Table = o };
    }

}