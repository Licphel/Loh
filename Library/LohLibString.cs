using System.Text;
using Kinetic.App;
using Loh.Values;

namespace Loh.Library;

public class LohLibString
{

	public static void Load()
	{
		LohTable o = new LohTable();

		o.Put("sizeof", (v) =>
		{
			v.Return(v.Get<string>(0).Length);
		});
		o.Put("replace", (v) =>
		{
			v.Return(v.Get<string>(0).Replace(v.GetString(1), v.GetString(2)));
		});
		o.Put("concat", (v) =>
		{
			StringBuilder bd = new StringBuilder();
			for(int i = 0; i < v.Arity; i++)
				bd.Append(v.GetString(i));
			v.Return(bd.ToString());
		});

		LohEngine.LoadedScripts[new ID("lang/string.loh")] = new LohState() { Table = o };
	}

}
