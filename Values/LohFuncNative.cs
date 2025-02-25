using Loh.Runtime;

namespace Loh.Values;

public sealed class LohFuncNative
{

    public string Name;
    public Action<Arguments> NativeFn;

    public LohFuncNative(string name, Action<Arguments> fn)
    {
        Name = name;
        NativeFn = fn;
    }

    public override string ToString()
    {
        return $"NF<{Name}>";
    }

}