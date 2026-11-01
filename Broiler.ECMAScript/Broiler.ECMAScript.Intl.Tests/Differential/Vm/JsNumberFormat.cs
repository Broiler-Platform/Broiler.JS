// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsNumberFormat.cs
//     blob c65c7914ced68db36cf939d9ea2880847c62bbe2: the whole file: JsNumberFormat, the engine's adapter of the shared Number::toString and its white-space trim
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Globalization;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// This family's adapter of the shared number-to-string conversion: the strings the engine keeps for
/// the commonest Numbers, in front of <c>Broiler.ECMAScript.Algorithms</c>, which computes every
/// conversion (JSD-0065).
/// </summary>
/// <remarks>
/// <para>
/// <b>What stays here, and why</b> (milestone SJL-3.4, item 3). The conversions this class held -
/// <c>ToJsString</c>, <c>ToRadixString</c>, <c>ToNumber</c>, <c>ParseInt</c>, <c>ParseFloat</c>,
/// <c>IsWhiteSpace</c>, <c>Trim</c> and their helpers - are deleted and recorded in rule I5's
/// register; the library's <c>NumberText</c>, <c>NumberParsing</c> and <c>NumericKeys</c> replace
/// them. What stays is string creation, which the area's adapter definition leaves to the engine:
/// the shared strings for the integers 0 to 255 and for <c>NaN</c> and the infinities, so that the
/// commonest conversions allocate nothing, and the white-space trim's choice to return the string it
/// was given when nothing is trimmed. None of it computes a digit or reads a character's class.
/// </para>
/// </remarks>
internal static class JsNumberFormat
{
    private static readonly string[] SmallIntegers = BuildSmallIntegers();

    /// <summary>
    /// <c>Number::toString(x, 10)</c>: the library's <see cref="NumberText.ToString(double)"/>, with the
    /// engine's shared string for an integer from 0 to 255, <c>NaN</c> and the infinities.
    /// </summary>
    internal static string NumberToString(double value)
    {
        // -0 is 0 here, which is what Number::toString writes for it.
        if (value >= 0 && value < SmallIntegers.Length && value == Math.Floor(value))
        {
            return SmallIntegers[(int)value];
        }

        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? "Infinity" : "-Infinity";
        }

        return NumberText.ToString(value);
    }

    /// <summary>Renders an unsigned integer, which is what an array index always is.</summary>
    internal static string ToUintString(uint value) =>
        value < SmallIntegers.Length
            ? SmallIntegers[(int)value]
            : value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The text without the specification's <c>StrWhiteSpace</c> at either end, the library's
    /// <see cref="NumberParsing.TrimStrWhiteSpace"/>; the string itself when there is none to trim.
    /// </summary>
    internal static string TrimWhiteSpace(string text)
    {
        var trimmed = NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Both);
        return trimmed.Length == text.Length ? text : trimmed.ToString();
    }

    private static string[] BuildSmallIntegers()
    {
        var table = new string[256];

        for (var at = 0; at < table.Length; at++)
        {
            table[at] = at.ToString(CultureInfo.InvariantCulture);
        }

        return table;
    }
}
