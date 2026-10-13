// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntl.cs
//     blob 40d15926fa075a9cd864e4e004f849177395cb48: JSIntlNumberFormat's UnsignedRoundingMode (:5406-5410),
//     GetUnsignedRoundingMode (:5412-5425) and RoundRatio (:5445-5463)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; the members sit in a static class of the same name instead of the JSObject
//     JSIntlNumberFormat; the private enum and methods are internal;
//   - GetUnsignedRoundingMode is an instance method reading `resolved?.RoundingMode` (the resolved options'
//     string, "halfExpand" unless an option set it); it is static here and that read is the parameter
//     resolvedRoundingMode, null where the source's `resolved?.RoundingMode` is null.

using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>Intl.NumberFormat's unsigned rounding of an exact ratio.</summary>
internal static class JSIntlNumberFormat
{
    // Maps a (signed) ECMA-402 roundingMode plus the value's sign onto the unsigned
    // rounding direction applied to the magnitude: "zero" truncates toward zero,
    // "infinity" rounds away from zero, and the "half-*" variants only differ at the
    // exact midpoint (toward zero / away / to even).
    internal enum UnsignedRoundingMode { Zero, Infinity, HalfZero, HalfInfinity, HalfEven }

    internal static UnsignedRoundingMode GetUnsignedRoundingMode(string? resolvedRoundingMode, bool negative)
        => (resolvedRoundingMode ?? "halfExpand") switch
        {
            "ceil" => negative ? UnsignedRoundingMode.Zero : UnsignedRoundingMode.Infinity,
            "floor" => negative ? UnsignedRoundingMode.Infinity : UnsignedRoundingMode.Zero,
            "expand" => UnsignedRoundingMode.Infinity,
            "trunc" => UnsignedRoundingMode.Zero,
            "halfCeil" => negative ? UnsignedRoundingMode.HalfZero : UnsignedRoundingMode.HalfInfinity,
            "halfFloor" => negative ? UnsignedRoundingMode.HalfInfinity : UnsignedRoundingMode.HalfZero,
            "halfExpand" => UnsignedRoundingMode.HalfInfinity,
            "halfTrunc" => UnsignedRoundingMode.HalfZero,
            "halfEven" => UnsignedRoundingMode.HalfEven,
            _ => UnsignedRoundingMode.HalfInfinity,
        };

    // Rounds the non-negative ratio a/b (b > 0) to an integer using the unsigned mode.
    // The midpoint is detected exactly by comparing 2·remainder against b.
    internal static BigInteger RoundRatio(BigInteger a, BigInteger b, UnsignedRoundingMode mode)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r.IsZero)
            return q;
        var cmp = (r << 1).CompareTo(b); // <0 below, 0 at, >0 above the midpoint
        var roundUp = mode switch
        {
            UnsignedRoundingMode.Zero => false,
            UnsignedRoundingMode.Infinity => true,
            UnsignedRoundingMode.HalfZero => cmp > 0,
            UnsignedRoundingMode.HalfInfinity => cmp >= 0,
            UnsignedRoundingMode.HalfEven => cmp > 0 || (cmp == 0 && !q.IsEven),
            _ => cmp >= 0,
        };
        return roundUp ? q + 1 : q;
    }
}
