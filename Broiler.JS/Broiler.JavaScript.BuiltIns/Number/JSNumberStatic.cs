using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;
using System;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.Number;

public partial class JSNumber
{
    [JSExport("isFinite")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue IsFinite(in Arguments a)
    {
        if (a.Get1() is JSNumber n)
        {
            if (!double.IsNaN(n.value) && n.value > double.NegativeInfinity && n.value < double.PositiveInfinity)
                return BooleanTrue;
        }

        return BooleanFalse;
    }

    [JSExport("isInteger")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue IsInteger(in Arguments a)
    {
        if (a.Get1() is JSNumber n)
        {
            var v = n.value;

            if (!double.IsInfinity(v))
            {
                if (Math.Floor(v) == v)
                    return BooleanTrue;
            }
        }

        return BooleanFalse;
    }

    [JSExport("isNaN")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue IsNaN(in Arguments a)
    {
        var first = a.GetAt(0);
        if (first.IsNumber)
            return double.IsNaN(first.DoubleValue) ? BooleanTrue : BooleanFalse;

        return BooleanFalse;
    }

    [JSExport("isSafeInteger")]
    public static JSValue IsSafeInteger(in Arguments a)
    {
        if (a.Get1() is JSNumber n)
        {
            var v = n.value;
            if (!double.IsInfinity(v))
            {
                if (Math.Floor(v) == v && v >= MinSafeInteger && v <= MaxSafeInteger)
                    return BooleanTrue;
            }
        }

        return BooleanFalse;
    }
}
