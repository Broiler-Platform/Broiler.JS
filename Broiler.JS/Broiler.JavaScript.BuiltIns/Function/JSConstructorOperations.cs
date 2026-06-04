using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Function;

internal static class JSConstructorOperations
{
    internal static bool IsConstructor(JSValue value) => value switch
    {
        JSFunction function when function.BoundTargetFunction is JSObject boundTarget && !function.BoundTargetFunction.IsUndefined
            => IsConstructor(boundTarget),
        JSFunction function => function.prototype != null || function.IsConstructable,
        JSProxy proxy => proxy.IsConstructable,
        _ => false
    };
}
