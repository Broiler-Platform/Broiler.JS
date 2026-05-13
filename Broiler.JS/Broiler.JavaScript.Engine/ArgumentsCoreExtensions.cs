using System;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine;

/// <summary>
/// Core-dependent implementations for <see cref="Arguments"/> factory delegates.
/// These methods were extracted from Arguments when it moved to Runtime because
/// they depend on Core-only types (JSArguments, JSException) and JSValue factory delegates.
/// </summary>
internal static class ArgumentsCoreExtensions
{
    internal static Arguments ForApplyCore(JSValue @this, JSValue args)
    {
        if (args.IsNullOrUndefined)
            return new Arguments(@this);

        if (args.IsArray)
        {
            var arrayLength = (uint)args.Length;
            switch (arrayLength)
            {
                case 0:
                    return new Arguments(@this);
                case 1:
                    return new Arguments(@this, args[0u]);
                case 2:
                    return new Arguments(@this, args[0u], args[1u]);
                case 3:
                    return new Arguments(@this, args[0u], args[1u], args[2u]);
                case 4:
                    return new Arguments(@this, args[0u], args[1u], args[2u], args[3u]);
                default:
                    var argList = new JSValue[arrayLength];
                    var ee = args.GetElementEnumerator();
                    while (ee.MoveNext(out var hasValue, out var value, out var index))
                        argList[index] = hasValue ? value : JSUndefined.Value;
                    return new Arguments(@this, argList);
            }
        }

        if (args.TypeOf() == JSConstants.Arguments)
        {
            var argumentsLength = args.Length;
            switch (argumentsLength)
            {
                case 0:
                    return new Arguments(@this);
                case 1:
                    return new Arguments(@this, args[0u]);
                case 2:
                    return new Arguments(@this, args[0u], args[1u]);
                case 3:
                    return new Arguments(@this, args[0u], args[1u], args[2u]);
                case 4:
                    return new Arguments(@this, args[0u], args[1u], args[2u], args[3u]);
                default:
                    var argList = new JSValue[args.Length];
                    var ee = args.GetElementEnumerator();
                    while (ee.MoveNext(out var hasValue, out var value, out var index))
                        argList[index] = hasValue ? value : JSUndefined.Value;
                    return new Arguments(@this, argList);
            }
        }

        if (args is not JSObject @object)
            throw JSValue.NewTypeError("CreateListFromArrayLike called on non-object");

        var length = Math.Max(@object.Length, 0);
        switch (length)
        {
            case 0:
                return new Arguments(@this);
            case 1:
                return new Arguments(@this, @object[0u]);
            case 2:
                return new Arguments(@this, @object[0u], @object[1u]);
            case 3:
                return new Arguments(@this, @object[0u], @object[1u], @object[2u]);
            case 4:
                return new Arguments(@this, @object[0u], @object[1u], @object[2u], @object[3u]);
            default:
                var argList = new JSValue[length];
                for (uint i = 0; i < length; i++)
                    argList[i] = @object[i];
                return new Arguments(@this, argList);
        }
    }

    internal static JSValue RestFromCore(Arguments self, uint index)
    {
        var a = JSValue.CreateArray();
        for (uint i = index; i < self.Length; i++)
            a.AddArrayItem(self.GetAt((int)i));
        return a;
    }

    internal static StringSpan GetStringCore(JSValue item, string name, string function, string filePath, int line) =>
        item.IsString ? item.StringValue : throw new JSException(name + " is required", function, filePath, line);

    internal static JSValue GetSpreadTargetCore(JSValue a) =>
        ((JSSpreadValue)a).Value;
}
