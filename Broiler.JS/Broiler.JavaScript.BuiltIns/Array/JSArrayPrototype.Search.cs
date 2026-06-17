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
        // LengthOfArrayLike (ToLength → up to 2^53-1) and the fromIndex must be computed in long space;
        // a uint length / IntValue fromIndex truncated indices beyond 2^32. includes reads every index
        // with Get (holes read as undefined), so no presence check is needed.
        var length = GetArrayLikeLengthLong(@this);
        if (length == 0)
            return JSBoolean.False;

        long fromIndex = ToIntegerOrInfinity(a[1]);
        if (fromIndex < 0)
            fromIndex = fromIndex < -length ? 0 : fromIndex + length;
        if (fromIndex < 0)
            fromIndex = 0;

        for (long index = fromIndex; index < length; index++)
        {
            if (GetElementByIndex(@this, index).SameValueZero(first))
                return JSBoolean.True;
        }

        return JSBoolean.False;
    }

    // Reads the element at a (possibly > 2^32) index: the fast 32-bit element store when present,
    // otherwise the canonical numeric-string property (so sparse / large-index entries are seen).
    private static JSValue GetElementByIndex(JSObject @object, long index)
    {
        if (index <= uint.MaxValue - 1 && @object.TryGetElement((uint)index, out var item))
            return item;
        return @object[JSValue.CreateNumber((double)index)];
    }

    // Whether the element at a (possibly > 2^32) index exists (own or inherited), used by indexOf /
    // lastIndexOf to skip array holes while still seeing sparse / large-index entries.
    private static bool HasElementAt(JSObject @object, long index)
    {
        if (index <= uint.MaxValue - 1 && @object.TryGetElement((uint)index, out _))
            return true;
        return @object.HasProperty(JSValue.CreateNumber((double)index)).BooleanValue;
    }

    [JSPrototypeMethod]
    [JSExport("indexOf", Length = 1)]
    public static JSValue IndexOf(in Arguments a)
    {
        var @this = ToArrayLikeObject(a.This);
        var first = a.Get1();
        var length = GetArrayLikeLengthLong(@this);

        // Spec § Array.prototype.indexOf step 3: if len is 0, return -1 BEFORE
        // ToIntegerOrInfinity(fromIndex) so a fromIndex valueOf side effect is not observed.
        if (length == 0)
            return JSNumber.MinusOne;

        long fromIndex = ToIntegerOrInfinity(a[1]);

        if (fromIndex < 0)
            fromIndex = fromIndex < -length ? 0 : fromIndex + length;

        if (fromIndex >= length)
            return JSNumber.MinusOne;

        // length is LengthOfArrayLike (ToLength → up to 2^53-1), so iterate with a long counter.
        // Each present index (including sparse / >2^32 entries) is compared with strict equality;
        // holes are skipped (spec § Array.prototype.indexOf steps 9-10).
        for (long index = fromIndex; index < length; index++)
        {
            if (HasElementAt(@this, index) && first.StrictEquals(GetElementByIndex(@this, index)))
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
        // LengthOfArrayLike must be a long (up to 2^53-1); a uint length capped large array-likes.
        var length = GetArrayLikeLengthLong(@this);

        // Spec § Array.prototype.lastIndexOf step 3: if len is 0, return -1 BEFORE
        // ToIntegerOrInfinity(fromIndex) so a fromIndex valueOf side effect is not observed.
        if (length == 0)
            return JSNumber.MinusOne;

        // Default fromIndex is len-1; a supplied n ≥ 0 clamps to len-1, a negative n offsets from len.
        long start;
        if (a.TryGetAt(1, out var value))
        {
            var n = ToIntegerOrInfinity(value);
            start = n >= 0 ? Math.Min(n, length - 1) : length + n;
        }
        else
        {
            start = length - 1;
        }

        for (long i = start; i >= 0; i--)
        {
            if (HasElementAt(@this, i) && GetElementByIndex(@this, i).StrictEquals(first))
                return new JSNumber(i);
        }

        return JSNumber.MinusOne;
    }

}
