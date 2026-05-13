using System;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    [JSPrototypeMethod]
    [JSExport("at", Length = 1)]
    public static JSValue At(in Arguments a)
    {
        var index = a[0];
        var @this = a.This;
        var length = a.Length;
        var i = index.IntegerValue;

        if (i < 0)
        {
            if (i < -length)
                return JSUndefined.Value;

            i += length;
        }

        if (i >= length)
            return JSUndefined.Value;

        return @this.GetOwnProperty((uint)i);
    }

    [JSPrototypeMethod]
    [JSExport("includes", Length = 1)]
    public static JSValue Includes(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var length = GetArrayLikeLength(@this);

        long fromIndex = a[1]?.IntValue ?? 0;
        if (fromIndex < 0)
            fromIndex += length;

        if (fromIndex < 0)
            fromIndex = 0;

        bool isUndefined = first.IsUndefined;
        for (uint index = (uint)fromIndex; index < length; index++)
        {
            if (@this.TryGetElement(index, out var item))
            {
                if (item.SameValueZero(first))
                    return JSBoolean.True;
            }
            else
            {
                if (isUndefined)
                    return JSBoolean.True;
            }
        }

        return JSBoolean.False;
    }

    [JSPrototypeMethod]
    [JSExport("indexOf", Length = 1)]
    public static JSValue IndexOf(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var length = GetArrayLikeLength(@this);
        long fromIndex = a[1]?.IntValue ?? 0;

        if (fromIndex < 0)
            fromIndex += length;

        if (fromIndex < 0)
            fromIndex = 0;

        for (uint index = (uint)fromIndex; index < length; index++)
            if (@this.TryGetElement(index, out var item) && first.StrictEquals(item))
                return new JSNumber(index);

        return JSNumber.MinusOne;
    }

    [JSPrototypeMethod]
    [JSExport("lastIndexOf", Length = 1)]
    public static JSValue LastIndexOf(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var n = GetArrayLikeLength(@this);
        var fromIndex = a[1]?.IntValue ?? int.MaxValue;

        if (fromIndex < 0)
            fromIndex += (int)n;

        if (n == 0)
            return JSNumber.MinusOne;

        for (long i = Math.Min((long)n - 1, fromIndex); i >= 0; i--)
        {
            if (!@this.TryGetElement((uint)i, out var item))
                continue;

            if (item.StrictEquals(first))
                return new JSNumber(i);
        }

        return JSNumber.MinusOne;
    }

}
