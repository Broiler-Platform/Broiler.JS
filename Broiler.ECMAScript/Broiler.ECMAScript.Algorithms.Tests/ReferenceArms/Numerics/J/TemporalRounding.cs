// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/JSTemporalRoundingOptions.cs
//     blob f93b8d2bb95664842daacc1f3478dcc13ad89ef3: TemporalRoundingOptions' NormalizeRoundingMode (:70-75),
//     NegateRoundingMode (:77-86), UnsignedRoundingMode (:88-102), ApplyRoundingPicksEnd (:104-114),
//     RoundToIncrement (:186-215) and RoundToIncrementAsIfPositive (:217-244)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/JSTemporalDuration.Arithmetic.cs
//     blob 3a89087248faaaff9dbbdfd01baf3f2685eb8d2d: JSTemporalDuration's NearestDouble (:838-867), RatioToDouble
//     (:869-883), NormalizeRoundingMode (:885-890) and RoundToIncrement (:892-922)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/JSTemporalZonedDateTime.Rounding.cs
//     blob ede39deb55f528ce6399de3d52490a8c6c4f6d6d: JSTemporalZonedDateTime's UnsignedRoundingMode (:408-420) and
//     ApplyRoundingPicksEnd (:422-431)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; each source class's members sit in a static class of the same name (JSTemporalDuration
//     and JSTemporalZonedDateTime are JSObject partial classes in the source); the private members are
//     internal;
//   - the two NormalizeRoundingMode return string?: null where the source throws a RangeError (an unknown
//     mode), the throw kept in a comment beside it;
//   - JSTemporalZonedDateTime's two members are textually the same code as TemporalRoundingOptions' (bar a
//     comment), and are copied separately all the same.

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>The shared rounding helpers of the time-based Temporal types.</summary>
internal static class TemporalRoundingOptions
{
    internal static string? NormalizeRoundingMode(string mode) => mode switch
    {
        "ceil" or "floor" or "expand" or "trunc"
            or "halfCeil" or "halfFloor" or "halfExpand" or "halfTrunc" or "halfEven" => mode,
        _ => null, // throw JSEngine.NewRangeError($"Temporal: invalid roundingMode \"{mode}\""),
    };

    // NegateRoundingMode: a "since" difference negates the operands, so the rounding mode is mirrored
    // (ceil ↔ floor, halfCeil ↔ halfFloor); the symmetric modes are unchanged.
    internal static string NegateRoundingMode(string mode) => mode switch
    {
        "ceil" => "floor",
        "floor" => "ceil",
        "halfCeil" => "halfFloor",
        "halfFloor" => "halfCeil",
        _ => mode,
    };

    // GetUnsignedRoundingMode: collapses the nine signed rounding modes to the direction they imply for a
    // value of the given sign (zero = toward zero, infinity = away from zero, half-* = the tie-break).
    internal static string UnsignedRoundingMode(string mode, bool negative) => mode switch
    {
        "ceil" => negative ? "zero" : "infinity",
        "floor" => negative ? "infinity" : "zero",
        "expand" => "infinity",
        "trunc" => "zero",
        "halfCeil" => negative ? "half-zero" : "half-infinity",
        "halfFloor" => negative ? "half-infinity" : "half-zero",
        "halfExpand" => "half-infinity",
        "halfTrunc" => "half-zero",
        "halfEven" => "half-even",
        _ => "half-infinity",
    };

    // Given cmp = sign(2·|numerator| − |denominator|) and whether the lower boundary's unit count is even,
    // whether ApplyUnsignedRoundingMode picks the END (upper) boundary rather than the start (lower) one.
    internal static bool ApplyRoundingPicksEnd(int cmp, bool even, string unsignedMode) => unsignedMode switch
    {
        "zero" => false,
        "infinity" => true,
        "half-zero" => cmp > 0,
        "half-infinity" => cmp >= 0,
        "half-even" => cmp > 0 || (cmp == 0 && !even),
        _ => false,
    };

    // RoundNumberToIncrement: rounds `value` to the nearest multiple of `increment` under the
    // requested rounding mode (the same nine modes as RoundDuration).
    internal static BigInteger RoundToIncrement(BigInteger value, BigInteger increment, string roundingMode)
    {
        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        var sign = value.Sign; // remainder shares the sign of value
        var absRem2 = BigInteger.Abs(remainder) * 2;
        var absInc = BigInteger.Abs(increment);

        var roundUp = roundingMode switch
        {
            "trunc" => false,
            "expand" => true,
            "ceil" => sign > 0,
            "floor" => sign < 0,
            "halfExpand" => absRem2 >= absInc,
            "halfTrunc" => absRem2 > absInc,
            "halfCeil" => sign > 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfFloor" => sign < 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfEven" => absRem2 > absInc || (absRem2 == absInc && !BigInteger.Abs(quotient).IsEven),
            _ => absRem2 >= absInc,
        };

        if (roundUp)
            quotient += sign >= 0 ? 1 : -1;

        return quotient * increment;
    }

    // RoundNumberToIncrementAsIfPositive: rounds toward the two surrounding multiples as though the
    // value were non-negative, so the direction of "floor"/"trunc" (down, toward the Big Bang) and
    // "ceil"/"expand" (up) never depends on the sign. Temporal.Instant and Temporal.ZonedDateTime
    // round an absolute epoch-nanosecond count this way (test262 .../rounding-direction).
    internal static BigInteger RoundToIncrementAsIfPositive(BigInteger value, BigInteger increment, string roundingMode)
    {
        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        // Floor the quotient so 0 < remainder < increment (the lower surrounding multiple).
        if (remainder < 0) { quotient -= 1; remainder += increment; }
        var rem2 = remainder * 2;

        var roundUp = roundingMode switch
        {
            "trunc" or "floor" => false,
            "expand" or "ceil" => true,
            "halfExpand" or "halfCeil" => rem2 >= increment,
            "halfTrunc" or "halfFloor" => rem2 > increment,
            "halfEven" => rem2 > increment || (rem2 == increment && !quotient.IsEven),
            _ => rem2 >= increment,
        };

        if (roundUp)
            quotient += 1;

        return quotient * increment;
    }
}

/// <summary>Temporal.Duration's exact-to-double conversions and its own rounding helpers.</summary>
internal static class JSTemporalDuration
{
    // Converts a non-negative integer to the nearest IEEE-754 double (ties to even), matching
    // ECMAScript's ℝ→𝔽 — unlike .NET's (double)BigInteger, which truncates toward zero. The two agree
    // for magnitudes below 2^53 (where every integer is representable); they differ only for the very
    // large components a near-limit Temporal.Duration can produce, where rounding a component up may
    // push the duration over the maximum (then rejected as a RangeError).
    internal static double NearestDouble(BigInteger v)
    {
        var neg = v.Sign < 0;
        var a = BigInteger.Abs(v);
        var lo = (double)a; // .NET truncates toward zero, so lo ≤ a
        if (double.IsInfinity(lo)) return neg ? double.NegativeInfinity : double.PositiveInfinity;

        var loBig = new BigInteger(lo);
        double chosen;
        if (loBig == a)
        {
            chosen = lo;
        }
        else
        {
            var hi = Math.BitIncrement(lo);
            if (double.IsInfinity(hi)) return neg ? -lo : lo;
            var dLo = a - loBig;
            var dHi = new BigInteger(hi) - a;
            chosen = dLo < dHi ? lo
                : dHi < dLo ? hi
                : (BitConverter.DoubleToInt64Bits(lo) & 1) == 0 ? lo : hi; // tie → even
        }
        return neg ? -chosen : chosen;
    }

    // The nearest double to the exact rational num/den (den > 0), rounded once — unlike
    // computing the integer and fractional parts as separate doubles, which rounds twice
    // and can land on the adjacent double (#818 Problem 8). The numerator is shifted left
    // so the integer quotient carries far more than 53 significant bits; the sub-unit
    // remainder is then well below the rounding position, so NearestDouble of the shifted
    // quotient (scaled back by the same power of two) is correctly rounded.
    internal static double RatioToDouble(BigInteger num, BigInteger den)
    {
        if (num.IsZero)
            return 0.0;

        const int shift = 256;
        var scaled = (num << shift) / den;
        return Math.ScaleB(NearestDouble(scaled), -shift);
    }

    internal static string? NormalizeRoundingMode(string mode) => mode switch
    {
        "ceil" or "floor" or "expand" or "trunc"
            or "halfCeil" or "halfFloor" or "halfExpand" or "halfTrunc" or "halfEven" => mode,
        _ => null, // throw JSEngine.NewRangeError($"Temporal.Duration: invalid roundingMode \"{mode}\""),
    };

    // RoundNumberToIncrement: rounds `value` to a multiple of `increment` per `roundingMode`.
    internal static BigInteger RoundToIncrement(BigInteger value, BigInteger increment, string roundingMode)
    {
        if (increment <= 1 && roundingMode is "halfExpand" or "trunc") return value;

        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        var sign = value.Sign; // remainder has the same sign as value here
        var absRem2 = BigInteger.Abs(remainder) * 2;
        var absInc = BigInteger.Abs(increment);

        bool roundUp = roundingMode switch
        {
            "trunc" => false,
            "expand" => true,
            "ceil" => sign > 0,
            "floor" => sign < 0,
            "halfExpand" => absRem2 >= absInc,
            "halfTrunc" => absRem2 > absInc,
            "halfCeil" => sign > 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfFloor" => sign < 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfEven" => absRem2 > absInc || (absRem2 == absInc && !BigInteger.Abs(quotient).IsEven),
            _ => absRem2 >= absInc,
        };

        if (roundUp)
            quotient += sign >= 0 ? 1 : -1;

        return quotient * increment;
    }
}

/// <summary>Temporal.ZonedDateTime's rounding-mode helpers.</summary>
internal static class JSTemporalZonedDateTime
{
    // ── rounding-mode helpers ───────────────────────────────────────────────────

    internal static string UnsignedRoundingMode(string mode, bool negative) => mode switch
    {
        "ceil" => negative ? "zero" : "infinity",
        "floor" => negative ? "infinity" : "zero",
        "expand" => "infinity",
        "trunc" => "zero",
        "halfCeil" => negative ? "half-zero" : "half-infinity",
        "halfFloor" => negative ? "half-infinity" : "half-zero",
        "halfExpand" => "half-infinity",
        "halfTrunc" => "half-zero",
        "halfEven" => "half-even",
        _ => "half-infinity",
    };

    // Given cmp = sign(2·|numerator| − |denominator|), whether to pick the END (r2) boundary.
    internal static bool ApplyRoundingPicksEnd(int cmp, bool even, string unsignedMode) => unsignedMode switch
    {
        "zero" => false,
        "infinity" => true,
        "half-zero" => cmp > 0,
        "half-infinity" => cmp >= 0,
        "half-even" => cmp > 0 || (cmp == 0 && !even),
        _ => false,
    };
}
