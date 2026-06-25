using System.Collections.Generic;
using System.Globalization;

namespace Broiler.Regex.Unicode;

/// <summary>
/// ECMAScript case-folding for case-insensitive (<c>i</c>) matching
/// (ECMA-262 §22.2.2.9.4 Canonicalize).
/// </summary>
/// <remarks>
/// The spec distinguishes two modes:
/// <list type="bullet">
/// <item><b>non-Unicode</b>: canonicalize via <c>toUppercase</c>, but a multi-char
/// uppercasing or a result that leaves the ASCII range when the input was ASCII is
/// rejected (so <c>ſ</c> does NOT fold to <c>s</c>).</item>
/// <item><b>Unicode (<c>u</c>/<c>v</c>)</b>: canonicalize via the Unicode
/// <c>CaseFolding.txt</c> "common + simple" mapping (so <c>ſ→s</c>, <c>K→k</c>).</item>
/// </list>
/// This implementation covers ASCII fully plus a documented subset of the
/// non-ASCII simple folds that test262 exercises (the Greek/Latin special cases
/// the .NET translator also special-cased). TODO: replace with the full
/// <c>Broiler.Unicode</c> simple-fold table for complete conformance.
/// </remarks>
public static class CaseFolding
{
    /// <summary>
    /// Returns the canonical representative of <paramref name="codePoint"/> for the
    /// given mode. Two code points are case-equivalent iff their canonical forms are
    /// equal.
    /// </summary>
    public static int Canonicalize(int codePoint, bool unicode)
    {
        if (unicode)
            return SimpleFold(codePoint);

        // Non-Unicode Canonicalize: ToUppercase of the single code unit, with the
        // ASCII guard (a non-ASCII char must not fold into the ASCII range).
        if (codePoint > 0xFFFF)
            return codePoint;

        var ch = (char)codePoint;
        var upper = char.ToUpperInvariant(ch);
        if (upper == ch)
            return codePoint;

        // Reject a fold that crosses into ASCII from a non-ASCII source.
        if (codePoint >= 0x80 && upper < 0x80)
            return codePoint;

        return upper;
    }

    /// <summary>
    /// Unicode "simple" case fold: lower-cases the code point, with a few
    /// special-cased folds that <c>ToLowerInvariant</c> alone gets wrong for
    /// regex equivalence.
    /// </summary>
    private static int SimpleFold(int codePoint)
    {
        switch (codePoint)
        {
            case 0x017F: return 's';    // ſ LATIN SMALL LETTER LONG S → s
            case 0x212A: return 'k';    // K KELVIN SIGN → k
            case 0x212B: return 0x00E5; // Å ANGSTROM SIGN → å
            case 0x2126: return 0x03C9; // Ω OHM SIGN → ω
            case 0x1E9E: return 0x00DF; // ẞ LATIN CAPITAL SHARP S → ß
        }

        if (codePoint <= 0xFFFF)
        {
            var lower = char.ToLowerInvariant((char)codePoint);
            if (lower != (char)codePoint)
                return lower;

            // Some letters only have an upper form whose lower we want as the rep.
            var upper = char.ToUpperInvariant((char)codePoint);
            if (upper != (char)codePoint)
            {
                var upperLower = char.ToLowerInvariant(upper);
                if (upperLower != (char)codePoint)
                    return upperLower;
            }
        }

        return codePoint;
    }

    /// <summary>
    /// Yields the small set of code points known to be case-fold siblings of
    /// <paramref name="codePoint"/> (used to test class membership in both
    /// directions). This is the inverse of the special-case table above.
    /// </summary>
    public static IEnumerable<int> SimpleSiblings(int codePoint, bool unicode)
    {
        if (!unicode)
        {
            if (codePoint <= 0xFFFF)
            {
                var ch = (char)codePoint;
                var upper = char.ToUpperInvariant(ch);
                if (upper != ch && !(codePoint < 0x80 && upper >= 0x80))
                    yield return upper;
                var lower = char.ToLowerInvariant(ch);
                if (lower != ch && !(codePoint < 0x80 && lower >= 0x80))
                    yield return lower;
            }
            yield break;
        }

        switch (codePoint)
        {
            case 's': yield return 0x017F; break;
            case 'k': yield return 0x212A; break;
            case 0x00E5: yield return 0x212B; break;
            case 0x03C9: yield return 0x2126; break;
            case 0x00DF: yield return 0x1E9E; break;
        }

        if (codePoint <= 0xFFFF)
        {
            var ch = (char)codePoint;
            var upper = char.ToUpperInvariant(ch);
            if (upper != ch) yield return upper;
            var lower = char.ToLowerInvariant(ch);
            if (lower != ch) yield return lower;
        }
    }

    /// <summary>True when the two code points are case-equivalent in the given mode.</summary>
    public static bool Equal(int a, int b, bool ignoreCase, bool unicode)
    {
        if (a == b)
            return true;
        if (!ignoreCase)
            return false;
        return Canonicalize(a, unicode) == Canonicalize(b, unicode);
    }
}
