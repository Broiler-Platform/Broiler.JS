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
        var @this = ToArrayLikeObject(a.This);
        var length = GetArrayLikeLengthLong(@this);
        var relativeIndex = ToIntegerOrInfinity(a[0]);
        var index = relativeIndex >= 0 ? relativeIndex : length + relativeIndex;

        if (index < 0 || index >= length)
            return JSUndefined.Value;

        return @this[JSValue.CreateNumber(index)];
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

        for (uint index = (uint)fromIndex; index < length; index++)
        {
            if (@this[index].SameValueZero(first))
                return JSBoolean.True;
        }

        return JSBoolean.False;
    }

    [JSPrototypeMethod]
    [JSExport("indexOf", Length = 1)]
    public static JSValue IndexOf(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var length = GetArrayLikeLengthLong(@this);
        long fromIndex = ToIntegerOrInfinity(a[1]);

        if (fromIndex < 0)
            fromIndex = fromIndex < -length ? 0 : fromIndex + length;

        if (fromIndex >= length)
            return JSNumber.MinusOne;

        // length is LengthOfArrayLike (ToLength → up to 2^53-1), so iterate with a
        // long counter. Indices within the 32-bit array-index range use the fast
        // element store; larger indices are probed via HasProperty/Get on their
        // canonical numeric-string key (spec § Array.prototype.indexOf steps 9-10).
        for (long index = fromIndex; index < length; index++)
        {
            if (index < uint.MaxValue)
            {
                if (@this.TryGetElement((uint)index, out var item) && first.StrictEquals(item))
                    return new JSNumber(index);

                continue;
            }

            var key = new JSNumber(index);
            if (@this.HasProperty(key).BooleanValue && first.StrictEquals(@this[key]))
                return new JSNumber(index);
        }

        return JSNumber.MinusOne;
    }

    [JSPrototypeMethod]
    [JSExport("lastIndexOf", Length = 1)]
    public static JSValue LastIndexOf(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var n = GetArrayLikeLength(@this);
        var fromIndex = a.TryGetAt(1, out var value) ? ToIntegerOrInfinity(value) : long.MaxValue;

        if (fromIndex < 0)
            fromIndex += n;

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
