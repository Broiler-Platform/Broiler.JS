// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsNumberFormatter.cs
//     blob e31736af3c25d85e91b2652ea0f849de0f34ed44: JsNumberFormatter, of which its resolved options and FormatNumericToString are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: every member of JsNumberFormatter but its fields, its constructor, its resolved options, `FormatNumericToString` and `IsInteger`: PartitionNumberPattern, the ranges, the patterns, the affixes and the digits' transliteration, sub-area (c)'s; and the types `JsNumberPart`, `Segment` and `Built` they use, and the `using` lines only they needed.

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// One configured <c>Intl.NumberFormat</c>'s formatting: ECMA-402's PartitionNumberPattern and
/// PartitionNumberRangePattern over the locale's CLDR patterns (JSD-0044).
/// </summary>
/// <remarks>
/// <para>
/// <b>A formatted number is built in three layers around its digits</b>, the way ICU builds one and
/// the way ECMA-402 leaves to the implementation: an inner layer, the scientific exponent; a middle
/// layer, the pattern's prefix and suffix, which carry the sign, the currency symbol, the percent
/// sign and a compact suffix; and an outer layer, a unit's or a currency's name. A range repeats or
/// shares each layer by ICU's rules - the outer when the two are the same name, the middle when the
/// two are the same text of more than one character - and puts spaces round the separator when a
/// layer is repeated, so English writes <c>$1.00 - $5.00</c> with spaces round its en dash, and <c>-$5.00-1.00</c> without.
/// </para>
/// <para>
/// <b>A field's leading and trailing spaces are its neighbours' literals</b>, as ICU trims a field
/// of default-ignorable characters, so the compact <c>1 thousand</c> is an integer, a literal and a
/// compact part.
/// </para>
/// </remarks>
internal sealed class JsNumberFormatter
{
    private readonly JsNumberData data;

    private readonly string language;

    private readonly string[]? digits;

    internal JsNumberFormatter(JsNumberData data, string language, string numberingSystem)
    {
        this.data = data;
        this.language = language;
        NumberingSystem = numberingSystem;
        digits = numberingSystem == "latn" ? null : CollectionExtensions.GetValueOrDefault(data.NumberingSystems, numberingSystem);
    }

    // ---- the resolved options (ECMA-402 table 15) ------------------------------------------------

    internal string Locale { get; init; } = string.Empty;

    internal string NumberingSystem { get; }

    internal string Style { get; init; } = "decimal";

    internal string? Currency { get; init; }

    internal string? CurrencyDisplay { get; init; }

    internal string? CurrencySign { get; init; }

    internal string? Unit { get; init; }

    internal string? UnitDisplay { get; init; }

    internal int MinimumIntegerDigits { get; init; } = 1;

    internal int? MinimumFractionDigits { get; init; }

    internal int? MaximumFractionDigits { get; init; }

    internal int? MinimumSignificantDigits { get; init; }

    internal int? MaximumSignificantDigits { get; init; }

    /// <summary><c>fractionDigits</c>, <c>significantDigits</c>, <c>morePrecision</c> or <c>lessPrecision</c>.</summary>
    internal string RoundingType { get; init; } = "fractionDigits";

    internal string ComputedRoundingPriority { get; init; } = "auto";

    internal int RoundingIncrement { get; init; } = 1;

    internal string RoundingMode { get; init; } = "halfExpand";

    internal string TrailingZeroDisplay { get; init; } = "auto";

    internal string Notation { get; init; } = "standard";

    internal string? CompactDisplay { get; init; }

    /// <summary><c>always</c>, <c>auto</c>, <c>min2</c>, or <see langword="null"/> for <see langword="false"/>.</summary>
    internal string? UseGrouping { get; init; } = "auto";

    internal string SignDisplay { get; init; } = "auto";

    // ---- the operations ------------------------------------------------------------------------

    // ---- building one number -------------------------------------------------------------------

    /// <summary>ECMA-402's FormatNumericToString: the rounded value with its sign, and its decimal string.</summary>
    internal (JsDecimal Rounded, string Formatted) FormatNumericToString(JsEngine engine, JsDecimal x)
    {
        var negative = x.Negative;
        var magnitude = x.Abs();
        engine.Charge((ulong)(magnitude.Digits.Length / 16) + 1);

        // The option reader admits only the nine names, so the fallback, halfExpand's, is the default's.
        var mode = ExactRounding.TryParseRoundingMode(RoundingMode, out var signed)
            ? ExactRounding.GetUnsignedRoundingMode(signed, negative)
            : UnsignedRoundingMode.HalfInfinity;
        JsRawFormat result;

        switch (RoundingType)
        {
            case "significantDigits":
                result = JsRawDecimal.ToRawPrecision(magnitude, MinimumSignificantDigits!.Value, MaximumSignificantDigits!.Value, mode);
                break;
            case "fractionDigits":
                result = JsRawDecimal.ToRawFixed(magnitude, MinimumFractionDigits!.Value, MaximumFractionDigits!.Value, RoundingIncrement, mode);
                break;
            default:
                var precision = JsRawDecimal.ToRawPrecision(magnitude, MinimumSignificantDigits!.Value, MaximumSignificantDigits!.Value, mode);
                var fixedResult = JsRawDecimal.ToRawFixed(magnitude, MinimumFractionDigits!.Value, MaximumFractionDigits!.Value, RoundingIncrement, mode);
                var fixedIsMorePrecise = fixedResult.RoundingMagnitude < precision.RoundingMagnitude;
                result = RoundingType == "morePrecision"
                    ? (fixedIsMorePrecise ? fixedResult : precision)
                    : (fixedIsMorePrecise ? precision : fixedResult);
                break;
        }

        var text = result.Formatted;

        if (TrailingZeroDisplay == "stripIfInteger" && IsInteger(result.Rounded))
        {
            var point = text.IndexOf('.');

            if (point >= 0)
            {
                text = text[..point];
            }
        }

        if (result.IntegerDigits < MinimumIntegerDigits)
        {
            text = new string('0', MinimumIntegerDigits - result.IntegerDigits) + text;
        }

        return (result.Rounded.WithSign(negative), text);
    }

    /// <summary>Whether a rounded value is an integer.</summary>
    private static bool IsInteger(JsDecimal value) => value.IsZero || value.Point >= value.Digits.Length;
}
