// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 44a214dc (bundle sjl-0-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/String/JSStringPrototype.Transform.cs
//     blob 702572400ea336a558f4ab1409745dc58ab19db6: the trimCharacters array (String.prototype.trim's set)
//   Broiler.JS/Broiler.JavaScript.Globals/JSGlobal.cs
//     blob 9e8ecbe4769a2797ad772ea2467116b93f7763a9: ParseInt's text path, adapted
//   Broiler.JS/Broiler.JavaScript.Extensions/JSStringExtensions.cs
//     blob eee333cb5d3361174ce708286cb7a59593f1833f: JSTrim, which is string.Trim
//
// Changes from the source:
//   - line endings normalised to LF where the source has CRLF, as rule SL10 requires of the shared folder;
//   - the namespace; the array made internal and placed in a static class with the global's text path;
//   - ParseInt adapted as its comment says; Number.parseInt (JSNumberStatic.ParseInt) has the same text path at the base, so one copy stands for both; no result changes.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.J;

/// <summary>The String built-ins' trim set and the global parseInt's text path.</summary>
internal static class Globals
{
    internal static readonly char[] trimCharacters = [
        // Whitespace
        '\x09', '\x0B', '\x0C', '\x20', '\xA0', '\xFEFF',

        // Unicode space separator (Zs). U+180E (MONGOLIAN VOWEL SEPARATOR) is
        // deliberately excluded: it was reclassified from Zs to Cf (Format) in
        // Unicode 6.3, so it is no longer WhiteSpace and must not be trimmed.
        '\u1680', '\u2000', '\u2001',
        '\u2002', '\u2003', '\u2004', '\u2005',
        '\u2006', '\u2007', '\u2008', '\u2009',
        '\u200A', '\u202F', '\u205F', '\u3000', 

        // Line terminators
        '\x0A', '\x0D', '\u2028', '\u2029',
    ];

    /// <summary>
    /// JSGlobal.ParseInt(in Arguments a) for a string argument and a radix already validated as 0
    /// (absent) or 2 to 36, ADAPTED: the lines below are the source's text path verbatim, with
    /// `p.JSTrim()` (JSStringExtensions.JSTrim, which is string.Trim at the base) applied to the string
    /// and `new JSNumber(d)` / `JSValue.CreateNumber(d)` returning d. The null and undefined argument
    /// checks and the radix coercion are the adapter's and are not here.
    /// </summary>
    internal static double ParseInt(string p, int radix)
    {
        var nan = double.NaN;

        var text = p.Trim();
        if (text.Length == 0)
            return nan;

        var d = NumberParser.ParseInt(text.Trim(), radix, false);
        return d;
    }
}
