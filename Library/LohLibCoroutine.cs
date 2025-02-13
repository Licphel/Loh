using Kinetic.App;
using Loh.Values;

namespace Loh.Library;

public class LohLibCoroutine
{

	public static void Load()
	{
		LohTable o = new LohTable();

		o.Put("execute", (v) =>
		{
			LohClosure fn = v.Get<LohClosure>(0);
			new Coroutine(() => LohEngine.Exec(fn)).Start();
		});

		LohEngine.LoadedScripts[new ID("lang/coroutine.loh")] = new LohState() { Table = o };
	}

}
