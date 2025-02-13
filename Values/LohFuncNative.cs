using Kinetic;
using Kinetic.App;
using Loh.Runtime;

namespace Loh.Values;

public sealed unsafe class LohFuncNative
{

	public string Name;
	public Response<Arguments> NativeFn;

	public LohFuncNative(string name, Response<Arguments> fn)
	{
		Name = name;
		NativeFn = fn;
	}

	public override string ToString()
	{
		return $"NF<{Name}>";
	}

}
