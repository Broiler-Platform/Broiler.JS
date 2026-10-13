// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.Runtime/JSValue.cs
//     blob 97651b44cbe933d688947317321b0cab13a7bd19: IntValue (:383), ToUint32 (:385-404), IntegerValue (:406-423),
//     UIntValue (:428) and Power (:1740-1762, its Number path)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; the members sit in a static class of their own instead of the abstract JSValue;
//   - IntValue, IntegerValue and UIntValue are virtual properties reading DoubleValue; each is a static method
//     taking that double as a parameter named DoubleValue, so the body reads as the source's. JSNumber overrides
//     none of them (JSNull and JSUndefined do, with 0; the adapter's);
//   - IntegerValue's getter body is the method's body; its `#pragma warning disable 1718` / `restore` around
//     `if (v != v)` is removed, because rule SL10 forbids preprocessor directives under Broiler.ECMAScript/
//     (the architecture test SL10_No_shared_source_holds_a_preprocessor_directive_or_a_CR reads this project),
//     and without it `v != v` is warning CS1718, an error here; it is written `double.IsNaN(v)`, which .NET
//     defines as `d != d`: true exactly for NaN, as `v != v` is;
//   - Power takes the two Numbers' doubles as v and a1 (the source's names): the source's
//     ArithmeticOperandDiagnostics recording (CRLF lines :1742-1743), its ToNumericPrimitive coercion of both
//     operands and the re-dispatch to a coerced operand's own Power (a BigInt's) are the adapter's;
//     `NumberOne` (JSNumber.One) is 1d, `NumberNaN` (JSNumber.NaN) is double.NaN, and `CreateNumber(x)` is x.

using System;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>JSValue's integer conversions and its Number exponentiation.</summary>
internal static class JSValue
{
    internal static int IntValue(double DoubleValue) => unchecked((int)ToUint32(DoubleValue));

    // ToUint32 (§7.1.6) / ToInt32 (§7.1.5): NaN, ±0 and ±∞ map to 0; every other
    // value is truncated toward zero and reduced modulo 2^32. The fast path covers
    // finite values inside the signed 64-bit range, where (long) truncates exactly
    // and the low 32 bits are the result. Values outside that range (including ±∞,
    // which .NET converts to long.MaxValue/MinValue rather than wrapping) take the
    // floating-point modulo path.
    internal static uint ToUint32(double d)
    {
        const double TwoPow32 = 4294967296.0;
        if (d >= -9.2233720368547758E18 && d < 9.2233720368547758E18)
            return unchecked((uint)((long)d << 32 >> 32));

        if (!double.IsFinite(d))
            return 0;

        var num = Math.Truncate(d) % TwoPow32;
        if (num < 0)
            num += TwoPow32;

        return (uint)num;
    }

    /// <summary>
    /// Integer value restricts value within int.MaxValue and
    /// more than int.MaxValue is returned as int.MaxValue
    /// </summary>
    internal static int IntegerValue(double DoubleValue)
    {
        var v = DoubleValue;
        if (v > 2147483647.0)
            return 2147483647;
        if (double.IsNaN(v))
            return 0;
        return (int)v;
    }

    internal static uint UIntValue(double DoubleValue) => ToUint32(DoubleValue);

    internal static double Power(double v, double a1)
    {
        if (a1 == 0)
            return 1d;

        if (a1 == double.PositiveInfinity || a1 == double.NegativeInfinity)
        {
            if (v == 1 || v == -1)
                return double.NaN;
        }

        return Math.Pow(v, a1);
    }
}
