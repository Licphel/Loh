using KryptonM.IDM;
using Loh.Values;

namespace Loh.Library;

public class LohLibTable
{

    public static void Load()
    {
        LohTable o = new LohTable();

        o.Put("sizeof", v => { v.Return(v.Get<LohTable>(0).Count); });
        o.Put("copy", v =>
        {
            LohTable table = v.Get<LohTable>(0);
            LohTable dst = new LohTable();
            foreach(KeyValuePair<string, Union> kv in (IDictionary<string, Union>)table)
                dst[kv.Key] = kv.Value;
            v.Return(dst);
        });

        LohEngine.LoadedScripts[new ID("lang/table.loh")] = new LohState { Table = o };
    }

}