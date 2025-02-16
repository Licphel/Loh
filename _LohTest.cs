using Kinetic.IO;
using Kinetic.Math;
using Loh.Library;
using Loh.Values;
using Morinia.World.TheGen;

namespace Loh;

public class _LohTest
{

	public static void Test()
	{
		LohEngine.Init();

		LohState fn = LohEngine.Require(null, StringIO.Read(FileSystem.GetAbsolute("F:/C# Workspace/Loh/TheLohTest.loh")), false);

		for(int i = 0; i < 10; i++)
		{
			var d1 = DateTime.Now;
			LohEngine.Exec(fn.Table["doit"].Dynamic);
			Console.WriteLine((DateTime.Now - d1).TotalMilliseconds);
		}
	}

}
