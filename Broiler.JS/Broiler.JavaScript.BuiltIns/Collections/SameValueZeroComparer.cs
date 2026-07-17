using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Decimal;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Collections;

/// <summary>
/// Implements SameValueZero for keyed collections without allocating a textual key.
/// Object and Symbol keys use reference identity; primitive keys use their ECMAScript value.
/// </summary>
internal sealed class SameValueZeroComparer : IEqualityComparer<JSValue>
{
    internal static readonly SameValueZeroComparer Instance = new();

    private SameValueZeroComparer() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(JSValue x, JSValue y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;

        return (x, y) switch
        {
            (JSNumber left, JSNumber right) =>
                left.value == right.value || double.IsNaN(left.value) && double.IsNaN(right.value),
            (JSString left, JSString right) =>
                string.Equals(left.value, right.value, System.StringComparison.Ordinal),
            (JSBigInt left, JSBigInt right) => left.value == right.value,
            (JSDecimal left, JSDecimal right) => left.value == right.value,
            (JSBoolean left, JSBoolean right) => left.BooleanValue == right.BooleanValue,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(JSValue value)
    {
        if (value is null)
            return 0;

        return value switch
        {
            JSNumber number => NumberHash(number.value),
            JSString text => System.StringComparer.Ordinal.GetHashCode(text.value),
            JSBigInt bigint => bigint.value.GetHashCode(),
            JSDecimal decimalValue => decimalValue.value.GetHashCode(),
            JSBoolean boolean => boolean.BooleanValue ? 1 : 0,
            _ => RuntimeHelpers.GetHashCode(value)
        };
    }

    private static int NumberHash(double value)
    {
        if (value == 0)
            return 0;
        if (double.IsNaN(value))
            return 0x7ff8_0000;
        return value.GetHashCode();
    }
}
