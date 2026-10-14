// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsNumberFormat.cs
//     blob c65c7914ced68db36cf939d9ea2880847c62bbe2: TrimWhiteSpace(string), which JsBigInt.TryParseStringInteger calls
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; TrimWhiteSpace sits in a static class JsNumberFormat of its own, whose other members (the number-to-string adapter) are the engine's and are not copied;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - it calls the shared library's public NumberParsing.TrimStrWhiteSpace(ReadOnlySpan<char>, TrimEnds) as the source does; that member and StrWhiteSpace are unchanged between the library's 4899f9c, the version Broiler.VM used, and the commit this project builds;
//   - the using directives the copied member does not need (System, System.Globalization) dropped.

using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>The one member of Broiler.VM's <c>JsNumberFormat</c> that <see cref="JsBigInt"/> calls.</summary>
internal static class JsNumberFormat
{
    /// <summary>
    /// The text without the specification's <c>StrWhiteSpace</c> at either end, the library's
    /// <see cref="NumberParsing.TrimStrWhiteSpace"/>; the string itself when there is none to trim.
    /// </summary>
    internal static string TrimWhiteSpace(string text)
    {
        var trimmed = NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Both);
        return trimmed.Length == text.Length ? text : trimmed.ToString();
    }
}
