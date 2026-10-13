// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 44a214dc (bundle sjl-0-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Number/JSNumber.cs
//     blob 2e9e1d7fe0d4ce4372c85779b466d585c6aeb503: RoundTrippable and ToECMAString
//
// Changes from the source:
//   - line endings normalised to LF where the source has CRLF, as rule SL10 requires of the shared folder;
//   - the namespace; the two static members sit in a static class of their own instead of the JSNumber value type, whose other members are the engine's; no result changes.

using System;
using System.Globalization;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.J;

/// <summary>Number::toString at radix 10, as the built-ins rendered it.</summary>
internal static class JSNumber
{
    /// <summary>
    /// ECMAScript-compliant Number::toString(x) per ECMA-262 § 6.1.6.1.20.
    /// Produces the shortest decimal representation that round-trips through
    /// IEEE 754 double-precision, formatted using ECMAScript rules for when
    /// to use decimal vs scientific notation.
    /// </summary>
    /// <summary>
    /// The shortest decimal string that reads back as <paramref name="value"/> exactly, which is what
    /// Number::toString is defined to produce: the digits are chosen so that no shorter string denotes
    /// the same Number.
    /// </summary>
    /// <remarks>
    /// <c>"R"</c> alone does not give it. The specifier is documented as unreliable and, for 2**-25, it
    /// drops the seventeenth significant digit: .NET renders <c>2.980232238769531E-08</c>, which parses
    /// back as a DIFFERENT double (0x1.fffffffffffffp-26 rather than 0x1.0p-25). So <c>String(2**-25)</c>
    /// answered a number that is not the one it was asked about, and a page round-tripping a value
    /// through a string - a hash, a serialized payload, a cache key - got a different value back than it
    /// put in. The short form is verified by parsing and only widened to 17 digits when it fails, so
    /// every value that already round-trips keeps its shortest form: <c>String(0.1)</c> stays
    /// <c>"0.1"</c> rather than becoming <c>"0.10000000000000001"</c>.
    /// </remarks>
    internal static string RoundTrippable(double value)
    {
        var shortest = value.ToString("R", CultureInfo.InvariantCulture);
        return double.TryParse(shortest, NumberStyles.Float, CultureInfo.InvariantCulture, out var reparsed)
            && reparsed.Equals(value)
                ? shortest
                : value.ToString("G17", CultureInfo.InvariantCulture);
    }

    internal static string ToECMAString(double value)
    {
        if (double.IsNaN(value))
            return "NaN";

        if (double.IsPositiveInfinity(value))
            return "Infinity";

        if (double.IsNegativeInfinity(value))
            return "-Infinity";

        if (value == 0.0)
            return "0";

        if (value < 0)
            return "-" + ToECMAString(-value);

        // Get the round-trip representation using InvariantCulture
        // to ensure '.' as decimal separator.
        string repr = RoundTrippable(value);

        // Parse repr into significand digits and base-10 exponent
        // such that value = significand × 10^exp  (significand is an integer string).
        int eIdx = repr.IndexOf('E');
        string intMantissa;
        int exp = 0;

        if (eIdx >= 0)
        {
            intMantissa = repr.Substring(0, eIdx);
            exp = int.Parse(repr.AsSpan(eIdx + 1), CultureInfo.InvariantCulture);
        }
        else
        {
            intMantissa = repr;
        }

        int dotIdx = intMantissa.IndexOf('.');
        if (dotIdx >= 0)
        {
            int fracLen = intMantissa.Length - dotIdx - 1;
            intMantissa = string.Concat(intMantissa.AsSpan(0, dotIdx), intMantissa.AsSpan(dotIdx + 1));
            exp -= fracLen;
        }

        // Remove leading zeros (e.g., from "0.001" → "0001")
        intMantissa = intMantissa.TrimStart('0');
        if (intMantissa.Length == 0) return "0";

        // Remove trailing zeros and adjust exponent
        // e.g., "420" exp=0 → "42" exp=1
        int origLen = intMantissa.Length;
        intMantissa = intMantissa.TrimEnd('0');
        exp += origLen - intMantissa.Length;

        // Now: value = int(intMantissa) × 10^exp
        // ECMAScript spec defines:
        //   s = int(intMantissa), k = intMantissa.Length
        //   n = exp + k   (so that s × 10^(n-k) = value)
        int k = intMantissa.Length;
        int n = exp + k;

        // Step 6: If k ≤ n ≤ 21 → integer with trailing zeros
        if (k <= n && n <= 21)
            return intMantissa + new string('0', n - k);

        // Step 7: If 0 < n ≤ 21 → decimal (n < k here since step 6 handled n ≥ k)
        if (0 < n && n <= 21)
            return intMantissa.Substring(0, n) + "." + intMantissa.Substring(n);

        // Step 8: If -5 ≤ n ≤ 0 → "0.000...digits"
        if (-5 <= n && n <= 0)
            return "0." + new string('0', -n) + intMantissa;

        // Steps 9-10: Scientific notation
        int expVal = n - 1;
        string expStr = (expVal >= 0 ? "+" : "") + expVal.ToString(CultureInfo.InvariantCulture);
        if (k == 1)
            return intMantissa + "e" + expStr;

        return intMantissa[0] + "." + intMantissa.Substring(1) + "e" + expStr;
    }
}
