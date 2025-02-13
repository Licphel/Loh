using Kinetic.App;
using Loh.Values;

namespace Loh.Library;

public class LohLibOs
{

	public static void Load()
	{
		LohTable o = new LohTable();

		o.Put("print", (v) => Console.WriteLine(v.GetString(0)));
		o.Put("clock", (v) => v.Return(DateTime.Now));
		o.Put("print_timespan", (v) => Console.WriteLine(((TimeSpan) (v.GetDynamic(1) - v.GetDynamic(0))).TotalMilliseconds));

		LohEngine.LoadedScripts[new ID("lang/os.loh")] = new LohState() { Table = o };
	}

}
