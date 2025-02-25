using KryptonM.IO;
using Loh.Values;

namespace Loh;

public class _LohTest
{

    public static void Test()
    {
        LohEngine.Init();

        LohState fn = LohEngine.Require(null,
            StringIO.Read(FileSystem.GetAbsolute("F:/C# Workspace/Loh/TheLohTest.loh")), false);

        for(var i = 0; i < 10; i++)
        {
            DateTime d1 = DateTime.Now;
            LohEngine.Exec(fn.Table["doit"].Dynamic);
            Console.WriteLine((DateTime.Now - d1).TotalMilliseconds);
        }
    }

}