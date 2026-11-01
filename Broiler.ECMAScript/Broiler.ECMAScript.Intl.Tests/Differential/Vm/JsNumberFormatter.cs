// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsNumberFormatter.cs
//     blob e31736af3c25d85e91b2652ea0f849de0f34ed44: the whole file: JsNumberPart and JsNumberFormatter; it replaces stage SJL-9.3b's copy of the same blob, which kept the resolved options and FormatNumericToString alone, since the class is not partial and a second copy beside it could not hold the members sub-area (c) compares
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>One part of a formatted number: its type, its text and, in a range, its source.</summary>
internal readonly record struct JsNumberPart(string Type, string Value, string? Source);

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
    /// <summary>One run of text and its part type.</summary>
    private readonly record struct Segment(string Type, string Text);

    /// <summary>A number built in its layers.</summary>
    private sealed class Built
    {
        internal List<Segment> OuterPrefix { get; } = [];

        internal List<Segment> MiddlePrefix { get; } = [];

        internal List<Segment> Core { get; } = [];

        internal List<Segment> MiddleSuffix { get; } = [];

        internal List<Segment> OuterSuffix { get; } = [];

        /// <summary>The middle layer's text, which two numbers of a range share when it is equal.</summary>
        internal string MiddleKey { get; set; } = string.Empty;

        internal int MiddleLength { get; set; }

        internal int InnerLength { get; set; }

        internal int OuterLength { get; set; }

        /// <summary>Whether the prefix ends, and the suffix starts, with a currency that spacing applies to.</summary>
        internal bool PrefixSpacing { get; set; }

        internal bool SuffixSpacing { get; set; }

        /// <summary>Whether the digits begin and end with a digit, which currency spacing needs.</summary>
        internal bool CoreIsDigits { get; set; }

        /// <summary>The plural category of the formatted value, which a name is chosen by.</summary>
        internal string Plural { get; set; } = "other";
    }

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

    /// <summary>ECMA-402's FormatNumeric.</summary>
    internal string Format(JsEngine engine, JsDecimal x)
    {
        var text = new StringBuilder();

        foreach (var part in Parts(engine, x))
        {
            text.Append(part.Value);
        }

        return text.ToString();
    }

    /// <summary>ECMA-402's PartitionNumberPattern, as the parts FormatNumericToParts answers.</summary>
    internal List<JsNumberPart> Parts(JsEngine engine, JsDecimal x)
    {
        var built = Build(engine, x, approximately: false);
        var parts = new List<(Segment Segment, string? Source)>();
        Emit(parts, built.OuterPrefix, built.MiddlePrefix, null);
        AddCore(parts, built, built, null);
        Emit(parts, built.MiddleSuffix, built.OuterSuffix, null);
        return Flatten(parts);
    }

    /// <summary>ECMA-402's PartitionNumberRangePattern.</summary>
    internal List<JsNumberPart> RangeParts(JsEngine engine, JsDecimal x, JsDecimal y)
    {
        if (string.Equals(Format(engine, x), Format(engine, y), StringComparison.Ordinal))
        {
            var approximate = Build(engine, x, approximately: true);
            var shared = new List<(Segment, string?)>();
            Emit(shared, approximate.OuterPrefix, approximate.MiddlePrefix, "shared");
            AddCore(shared, approximate, approximate, "shared");
            Emit(shared, approximate.MiddleSuffix, approximate.OuterSuffix, "shared");
            return Flatten(shared);
        }

        var first = Build(engine, x, approximately: false);
        var second = Build(engine, y, approximately: false);

        var collapseOuter = string.Equals(OuterKey(first), OuterKey(second), StringComparison.Ordinal);
        var collapseMiddle = collapseOuter &&
            string.Equals(first.MiddleKey, second.MiddleKey, StringComparison.Ordinal) &&
            first.MiddleLength > 1;

        var repeat = first.InnerLength > 0 ||
            (!collapseMiddle && first.MiddleLength > 0) ||
            (!collapseOuter && first.OuterLength > 0);

        var range = data.Value(language, "misc.range") ?? "{0}\u2013{1}";
        var open = range.IndexOf("{0}", StringComparison.Ordinal);
        var close = range.IndexOf("{1}", StringComparison.Ordinal);
        var separator = range[(open + 3)..close];

        if (repeat)
        {
            separator = (separator.Length != 0 && IsPatternWhiteSpace(separator[0]) ? string.Empty : " ") + separator +
                (separator.Length != 0 && IsPatternWhiteSpace(separator[^1]) ? string.Empty : " ");
        }

        var parts = new List<(Segment Segment, string? Source)>();

        // THE COLLAPSED OUTER NAME TAKES THE RANGE'S PLURAL, by the language's plural ranges.
        Built? outer = null;

        if (collapseOuter && first.OuterLength > 0)
        {
            outer = new Built();
            AddOuter(outer, data.PluralRange(language, first.Plural, second.Plural));
        }

        if (outer is not null)
        {
            Emit(parts, outer.OuterPrefix, [], "shared");
        }

        if (collapseMiddle)
        {
            Emit(parts, [], first.MiddlePrefix, "shared");
            AddCore(parts, first, null, "startRange");
            parts.Add((new Segment("literal", separator), "shared"));
            AddCore(parts, null, second, "endRange");
            Emit(parts, second.MiddleSuffix, [], "shared");
        }
        else
        {
            Emit(parts, collapseOuter ? [] : first.OuterPrefix, first.MiddlePrefix, "startRange");
            AddCore(parts, first, first, "startRange");
            Emit(parts, first.MiddleSuffix, collapseOuter ? [] : first.OuterSuffix, "startRange");
            parts.Add((new Segment("literal", separator), "shared"));
            Emit(parts, collapseOuter ? [] : second.OuterPrefix, second.MiddlePrefix, "endRange");
            AddCore(parts, second, second, "endRange");
            Emit(parts, second.MiddleSuffix, collapseOuter ? [] : second.OuterSuffix, "endRange");
        }

        if (outer is not null)
        {
            Emit(parts, [], outer.OuterSuffix, "shared");
        }

        return Flatten(parts);
    }

    /// <summary>What identifies a number's outer layer: its name, without its plural.</summary>
    private string OuterKey(Built built) => built.OuterLength == 0 ? string.Empty : Style + "|" + Unit + "|" + Currency;

    private static void Emit(List<(Segment, string?)> parts, List<Segment> first, List<Segment> second, string? source)
    {
        foreach (var segment in first)
        {
            parts.Add((segment, source));
        }

        foreach (var segment in second)
        {
            parts.Add((segment, source));
        }
    }

    /// <summary>
    /// The digits with the currency spacing on each side: the spacing before them is decided by
    /// <paramref name="prefixOf"/>'s prefix and after them by <paramref name="suffixOf"/>'s suffix.
    /// </summary>
    private void AddCore(List<(Segment, string?)> parts, Built? prefixOf, Built? suffixOf, string? source)
    {
        var core = prefixOf ?? suffixOf!;

        if (prefixOf is { PrefixSpacing: true, CoreIsDigits: true })
        {
            parts.Add((new Segment("literal", data.Value(language, "currency.currencySpacing.beforeCurrency.insertBetween") ?? "\u00a0"), source));
        }

        foreach (var segment in core.Core)
        {
            parts.Add((segment, source));
        }

        if (suffixOf is { SuffixSpacing: true, CoreIsDigits: true })
        {
            parts.Add((new Segment("literal", data.Value(language, "currency.currencySpacing.afterCurrency.insertBetween") ?? "\u00a0"), source));
        }
    }

    /// <summary>
    /// The parts a list of segments is: each field trimmed of the default-ignorable characters at its
    /// ends, which become literals, and adjacent literals joined, shared where their sources differ.
    /// </summary>
    private static List<JsNumberPart> Flatten(List<(Segment Segment, string? Source)> segments)
    {
        var trimmed = new List<(Segment Segment, string? Source)>();

        foreach (var (segment, source) in segments)
        {
            if (segment.Text.Length == 0)
            {
                continue;
            }

            if (segment.Type is "literal" or "group")
            {
                trimmed.Add((segment, source));
                continue;
            }

            var start = 0;
            var end = segment.Text.Length;

            while (start < end && IsDefaultIgnorable(segment.Text[start]))
            {
                start++;
            }

            while (end > start && IsDefaultIgnorable(segment.Text[end - 1]))
            {
                end--;
            }

            if (start > 0)
            {
                trimmed.Add((new Segment("literal", segment.Text[..start]), source));
            }

            if (end > start)
            {
                trimmed.Add((new Segment(segment.Type, segment.Text[start..end]), source));
            }

            if (end < segment.Text.Length && end >= start)
            {
                trimmed.Add((new Segment("literal", segment.Text[end..]), source));
            }
        }

        var parts = new List<JsNumberPart>(trimmed.Count);

        foreach (var (segment, source) in trimmed)
        {
            if (segment.Type == "literal" && parts.Count != 0 && parts[^1].Type == "literal")
            {
                var previous = parts[^1];
                parts[^1] = new JsNumberPart(
                    "literal",
                    previous.Value + segment.Text,
                    previous.Source == source ? source : "shared");
                continue;
            }

            parts.Add(new JsNumberPart(segment.Type, segment.Text, source));
        }

        return parts;
    }

    /// <summary>ICU's default-ignorable set: space separators, the tab, the bidirectional controls and the variation selectors.</summary>
    /// <remarks>
    /// Unicode classification written out here rather than read from the owner, Broiler.Unicode, and a
    /// gap rather than a choice: this is not the UCD's Default_Ignorable_Code_Point but ICU's union of
    /// the space separators, the tab, Bidi_Control and Variation_Selector, and the owner names no such
    /// set in 0.1.0-preview.4. JSD-0074's correspondence does not list it.
    /// </remarks>
    private static bool IsDefaultIgnorable(char c) =>
        c is ' ' or '\t' or '\u00a0' or '\u1680' or '\u202f' or '\u205f' or '\u3000' or '\u061c' or '\u200e' or '\u200f'
        || c is >= '\u2000' and <= '\u200a'
        || c is >= '\u202a' and <= '\u202e'
        || c is >= '\u2066' and <= '\u2069'
        || c is >= '\ufe00' and <= '\ufe0f'
        || c is >= '\u180b' and <= '\u180d' or '\u180f';

    /// <summary>Pattern_White_Space, which the range separator is tested against.</summary>
    /// <remarks>
    /// Unicode classification written out here rather than read from the owner, Broiler.Unicode, and a
    /// gap rather than a choice: the owner's <c>UnicodeProperties</c> and <c>EcmaScriptLexical</c>
    /// (0.1.0-preview.4) name no Pattern_White_Space member. JSD-0074's correspondence does not list it.
    /// </remarks>
    private static bool IsPatternWhiteSpace(char c) =>
        c is ' ' or '\t' or '\n' or '\u000b' or '\f' or '\r' or '\u0085' or '\u200e' or '\u200f' or '\u2028' or '\u2029';

    // ---- building one number -------------------------------------------------------------------

    /// <summary>ECMA-402's PartitionNumberPattern, layered.</summary>
    private Built Build(JsEngine engine, JsDecimal x, bool approximately)
    {
        var built = new Built();
        var exponent = 0;
        string n;
        JsDecimal rounded;

        if (x.Kind == JsDecimalKind.NaN)
        {
            n = Symbol("nan", "NaN");
            rounded = x;
        }
        else if (!x.IsFinite)
        {
            n = Symbol("infinity", "\u221e");
            rounded = x;
        }
        else
        {
            if (!(x.IsZero && x.Negative))
            {
                if (Style == "percent")
                {
                    x = x.Scale(2);
                }

                exponent = ComputeExponent(engine, x);
                x = x.Scale(-exponent);
            }

            (rounded, n) = FormatNumericToString(engine, x);
        }

        // THE SIGNUM, AS ICU READS IT FROM THE ROUNDED VALUE: not-a-number is a positive zero.
        var signum = rounded.Kind switch
        {
            JsDecimalKind.NaN => 1,
            JsDecimalKind.PositiveInfinity => 2,
            JsDecimalKind.NegativeInfinity => -2,
            _ when rounded.IsZero => rounded.Negative ? -1 : 1,
            _ => rounded.Negative ? -2 : 2,
        };

        // POS 0, POS_SIGN 1, NEG 2 (ICU's PatternSignType, from the sign display).
        var signType = SignDisplay switch
        {
            "always" => signum < 0 ? 2 : 1,
            "exceptZero" => signum == -2 ? 2 : signum == 2 ? 1 : 0,
            "negative" => signum == -2 ? 2 : 0,
            "never" => 0,
            _ => signum < 0 ? 2 : 0,
        };

        var compactKey = Notation == "compact" && rounded.IsFinite && !rounded.IsZero
            ? Math.Min(exponent + rounded.Magnitude, 14)
            : 0;

        var operands = JsPluralOperands.Of(rounded.IsFinite ? n : "0", Notation == "compact" ? exponent : 0);
        built.Plural = data.Plural(language, operands);

        var (pattern, patternIsCompact, joined) = Patterns(compactKey, exponent, built.Plural);

        // THE MIDDLE LAYER: the pattern's prefix and suffix with the sign written where ICU writes it.
        var prefix = Affix(pattern, patternIsCompact, joined, isPrefix: true, signType, approximately, out var prefixEndsInCurrency);
        var suffix = Affix(pattern, patternIsCompact, joined, isPrefix: false, signType, approximately, out var suffixStartsWithCurrency);
        built.MiddlePrefix.AddRange(prefix);
        built.MiddleSuffix.AddRange(suffix);
        built.PrefixSpacing = prefixEndsInCurrency;
        built.SuffixSpacing = suffixStartsWithCurrency;

        var key = new StringBuilder();
        var length = 0;

        foreach (var segment in prefix)
        {
            key.Append(segment.Type).Append('\u0001').Append(segment.Text).Append('\u0002');
            length += CodePoints(segment.Text);
        }

        key.Append('\u0003');

        foreach (var segment in suffix)
        {
            key.Append(segment.Type).Append('\u0001').Append(segment.Text).Append('\u0002');
            length += CodePoints(segment.Text);
        }

        built.MiddleKey = key.ToString();
        built.MiddleLength = length;

        // THE CORE: digits, grouping, the decimal separator, and the exponent as the inner layer.
        if (x.Kind == JsDecimalKind.NaN)
        {
            built.Core.Add(new Segment("nan", n));
        }
        else if (!x.IsFinite)
        {
            built.Core.Add(new Segment("infinity", n));
        }
        else
        {
            AddDigits(built, n);
            built.CoreIsDigits = true;

            if (Notation is "scientific" or "engineering")
            {
                var separator = Symbol("exponential", "E");
                built.Core.Add(new Segment("exponentSeparator", separator));
                built.InnerLength += CodePoints(separator);

                if (exponent < 0)
                {
                    var minus = Symbol("minusSign", "-");
                    built.Core.Add(new Segment("exponentMinusSign", minus));
                    built.InnerLength += CodePoints(minus);
                }

                var power = Transliterate(Math.Abs((long)exponent).ToString(CultureInfo.InvariantCulture));
                built.Core.Add(new Segment("exponentInteger", power));
                built.InnerLength += CodePoints(power);
            }
        }

        // THE OUTER LAYER: a unit's or a currency's name.
        AddOuter(built, built.Plural);
        return built;
    }

    /// <summary>The outer layer for a plural: a unit's pattern, or a currency's name in its pattern.</summary>
    private void AddOuter(Built built, string plural)
    {
        string? pattern = null;
        string? name = null;
        var nameType = "unit";

        if (Style == "unit" && Unit is not null && Unit != "percent")
        {
            pattern = UnitPattern(Unit, UnitDisplay ?? "short", plural);
        }
        else if (Style == "currency" && CurrencyDisplay == "name" && Currency is not null)
        {
            pattern = data.Value(language, "currency.unitPattern-count-" + plural) ??
                data.Value(language, "currency.unitPattern-count-other") ?? "{0} {1}";
            var names = CollectionExtensions.GetValueOrDefault(data.Currencies, language + "|" + Currency);
            name = names is null ? Currency
                : (plural == "one" ? names.One : names.Other) is { Length: > 0 } counted ? counted
                : names.Other.Length != 0 ? names.Other
                : names.Name.Length != 0 ? names.Name : Currency;
            nameType = "currency";
        }

        if (pattern is null)
        {
            return;
        }

        var at = pattern.IndexOf("{0}", StringComparison.Ordinal);

        if (at < 0)
        {
            return;
        }

        void Add(List<Segment> into, string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            if (name is null)
            {
                into.Add(new Segment(nameType, text));
                built.OuterLength += CodePoints(text);
                return;
            }

            // A CURRENCY'S NAME PATTERN: {1} is the name, and the rest is literal.
            var slot = text.IndexOf("{1}", StringComparison.Ordinal);

            if (slot < 0)
            {
                into.Add(new Segment("literal", text));
            }
            else
            {
                into.Add(new Segment("literal", text[..slot]));
                into.Add(new Segment("currency", name));
                into.Add(new Segment("literal", text[(slot + 3)..]));
            }

            built.OuterLength += CodePoints(text);
        }

        Add(built.OuterPrefix, pattern[..at]);
        Add(built.OuterSuffix, pattern[(at + 3)..]);
    }

    /// <summary>
    /// A unit's pattern in a width and a plural: CLDR's own for a simple unit or a compound it names,
    /// or the numerator's pattern inside the denominator's per-unit pattern or the width's compound
    /// pattern.
    /// </summary>
    private string? UnitPattern(string unit, string width, string plural)
    {
        string? Field(string id, string field)
        {
            foreach (var w in width == "narrow" ? new[] { "narrow", "short", "long" } : width == "short" ? ["short", "long"] : ["long"])
            {
                if (data.Units.TryGetValue(language + "|" + w + "|" + id + "|" + field, out var value))
                {
                    return value;
                }
            }

            return null;
        }

        // A WIDTH'S OWN OTHER FORM BEFORE A WIDER WIDTH: CLDR gives German's short nanosecond only
        // `{0} ns`, and one nanosecond is `1 ns`, as ICU writes it, not the long `1 Nanosekunde`.
        string? Pattern(string id)
        {
            foreach (var w in width == "narrow" ? new[] { "narrow", "short", "long" } : width == "short" ? ["short", "long"] : ["long"])
            {
                if (data.Units.TryGetValue(language + "|" + w + "|" + id + "|unitPattern-count-" + plural, out var value) ||
                    data.Units.TryGetValue(language + "|" + w + "|" + id + "|unitPattern-count-other", out value))
                {
                    return value;
                }
            }

            return null;
        }

        if (Pattern(unit) is { } direct)
        {
            return direct;
        }

        var per = unit.IndexOf("-per-", StringComparison.Ordinal);

        if (per < 0)
        {
            return null;
        }

        var numerator = Pattern(unit[..per]);
        var denominator = unit[(per + 5)..];

        if (numerator is null)
        {
            return null;
        }

        if (Field(denominator, "perUnitPattern") is { } perUnit)
        {
            return perUnit.Replace("{0}", numerator, StringComparison.Ordinal);
        }

        var compound = Field("per", "compoundUnitPattern") ?? "{0}/{1}";
        var single = (Field(denominator, "unitPattern-count-one") ?? Field(denominator, "unitPattern-count-other") ?? denominator)
            .Replace("{0}", string.Empty, StringComparison.Ordinal).Trim();

        return compound.Replace("{0}", numerator, StringComparison.Ordinal).Replace("{1}", single, StringComparison.Ordinal);
    }

    /// <summary>
    /// The pattern the value is written by, and a compact pattern whose affix joins it next to the
    /// number. A compact currency pattern carries its own currency, so it is the pattern itself; any
    /// other compact pattern's affix joins the style's.
    /// </summary>
    private (JsNumberPattern Pattern, bool PatternIsCompact, JsNumberPattern? Joined) Patterns(int compactKey, int exponent, string plural)
    {
        var main = JsNumberPattern.Parse(MainPattern());

        if (Notation != "compact" || exponent == 0 || CompactPattern(compactKey, plural) is not { } compact)
        {
            return (main, false, null);
        }

        return Style == "currency" && CurrencyDisplay != "name"
            ? (JsNumberPattern.Parse(compact), true, null)
            : (main, false, JsNumberPattern.Parse(compact));
    }

    /// <summary>The style's pattern.</summary>
    private string MainPattern() => Style switch
    {
        "percent" => data.Value(language, "percent.standard") ?? "#,##0%",
        "unit" when Unit == "percent" => data.Value(language, "percent.standard") ?? "#,##0%",
        "currency" when CurrencyDisplay == "name" => data.Value(language, "decimal.standard") ?? "#,##0.###",
        "currency" => (CurrencySign == "accounting" ? data.Value(language, "currency.accounting") : null) ??
            data.Value(language, "currency.standard") ?? "\u00a4#,##0.00",
        _ => data.Value(language, "decimal.standard") ?? "#,##0.###",
    };

    /// <summary>The compact pattern for a magnitude and a plural, or nothing where the locale does not compact it.</summary>
    private string? CompactPattern(int magnitude, string plural)
    {
        if (magnitude < 3)
        {
            return null;
        }

        var key = "1" + new string('0', Math.Min(magnitude, 14));
        var root = Style == "currency" && CurrencyDisplay != "name"
            ? "currency.short.standard."
            : CompactDisplay == "long" ? "decimal.long.decimalFormat." : "decimal.short.decimalFormat.";

        var pattern = data.Value(language, root + key + "-count-" + plural) ?? data.Value(language, root + key + "-count-other");
        return pattern is null or "0" ? null : pattern;
    }

    /// <summary>
    /// ECMA-402's ComputeExponentForMagnitude: 0 for the standard notation, the magnitude for the
    /// scientific, its multiple of three below for the engineering, and the compact pattern's.
    /// </summary>
    private int ExponentForMagnitude(int magnitude)
    {
        switch (Notation)
        {
            case "scientific":
                return magnitude;
            case "engineering":
                return (int)Math.Floor(magnitude / 3.0) * 3;
            case "compact":
                var pattern = CompactPattern(magnitude, "other");

                if (pattern is null)
                {
                    return 0;
                }

                var zeros = 0;

                foreach (var c in pattern)
                {
                    if (c == '0')
                    {
                        zeros++;
                    }
                }

                return Math.Min(magnitude, 14) - (zeros - 1);
            default:
                return 0;
        }
    }

    /// <summary>ECMA-402's ComputeExponent.</summary>
    private int ComputeExponent(JsEngine engine, JsDecimal x)
    {
        if (x.IsZero || Notation == "standard")
        {
            return 0;
        }

        var magnitude = x.Magnitude;
        var exponent = ExponentForMagnitude(magnitude);
        var (rounded, _) = FormatNumericToString(engine, x.Abs().Scale(-exponent));

        if (rounded.IsZero)
        {
            return exponent;
        }

        return rounded.Magnitude == magnitude - exponent ? exponent : ExponentForMagnitude(magnitude + 1);
    }

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

    /// <summary>
    /// One affix as ICU's patternInfoToStringBuilder writes it: the positive or negative subpattern's
    /// tokens, with the sign, a plus sign or the approximately sign at the sign's place.
    /// </summary>
    private List<Segment> Affix(JsNumberPattern pattern, bool patternIsCompact, JsNumberPattern? joined, bool isPrefix, int signType, bool approximately, out bool currencyAtNumber)
    {
        var plusReplacesMinus = signType == 1 && !pattern.PositiveHasPlus;
        var useNegative = pattern.NegativePrefix is not null &&
            (signType == 2 || (pattern.NegativeHasMinus && !plusReplacesMinus && !approximately));

        var tokens = new List<(JsAffixToken Token, bool Compact)>();

        foreach (var token in useNegative ? (isPrefix ? pattern.NegativePrefix! : pattern.NegativeSuffix!) : (isPrefix ? pattern.PositivePrefix : pattern.PositiveSuffix))
        {
            tokens.Add((token, patternIsCompact && token.Kind == JsAffixKind.Literal));
        }

        // A JOINED COMPACT AFFIX SITS NEXT TO THE NUMBER, its text the compact part.
        if (joined is not null)
        {
            var compactTokens = new List<(JsAffixToken, bool)>();

            foreach (var token in isPrefix ? joined.PositivePrefix : joined.PositiveSuffix)
            {
                compactTokens.Add((token, token.Kind == JsAffixKind.Literal));
            }

            if (isPrefix)
            {
                tokens.AddRange(compactTokens);
            }
            else
            {
                tokens.InsertRange(0, compactTokens);
            }
        }

        var prependSign = isPrefix && !useNegative && (signType == 2 || plusReplacesMinus || approximately);
        var signs = approximately
            ? (plusReplacesMinus ? "~+" : signType == 2 ? "~-" : "~")
            : plusReplacesMinus ? "+" : "-";

        var segments = new List<Segment>();

        void Sign()
        {
            foreach (var c in signs)
            {
                segments.Add(c switch
                {
                    '~' => new Segment("approximatelySign", Symbol("approximatelySign", "~")),
                    '+' => new Segment("plusSign", Symbol("plusSign", "+")),
                    _ => new Segment("minusSign", Symbol("minusSign", "-")),
                });
            }
        }

        if (prependSign)
        {
            Sign();
        }

        currencyAtNumber = false;

        for (var index = 0; index < tokens.Count; index++)
        {
            var (token, isCompact) = tokens[index];

            switch (token.Kind)
            {
                case JsAffixKind.Minus:
                    Sign();
                    break;
                case JsAffixKind.Plus:
                    segments.Add(new Segment("plusSign", Symbol("plusSign", "+")));
                    break;
                case JsAffixKind.Percent:
                    segments.Add(new Segment(Style == "unit" ? "unit" : "percentSign", Symbol("percentSign", "%")));
                    break;
                case JsAffixKind.PerMille:
                    segments.Add(new Segment("literal", Symbol("perMille", "\u2030")));
                    break;
                case JsAffixKind.Currency:
                    var (text, ends) = CurrencyText(token.Text.Length);
                    segments.Add(new Segment("currency", text));

                    // CLDR'S CURRENCY SPACING: only a currency next to the number, and only when its
                    // character there is neither a symbol nor a separator.
                    if (isPrefix ? index == tokens.Count - 1 : index == 0)
                    {
                        currencyAtNumber = (ends.Length == 2 ? ends[isPrefix ? 1 : 0] : 'L') == 'L';
                    }

                    break;
                default:
                    segments.Add(new Segment(isCompact ? "compact" : "literal", token.Text));
                    break;
            }
        }

        return segments;
    }

    /// <summary>The currency's text for one, two or three currency signs in a pattern, and its ends' kinds for spacing.</summary>
    private (string Text, string Ends) CurrencyText(int count)
    {
        var code = Currency ?? "XXX";
        var names = CollectionExtensions.GetValueOrDefault(data.Currencies, language + "|" + code);

        if (count >= 2 || CurrencyDisplay == "code" || names is null)
        {
            return (code, "LL");
        }

        if (CurrencyDisplay == "narrowSymbol" && names.Narrow.Length != 0)
        {
            return (names.Narrow, names.NarrowEnds);
        }

        return names.Symbol.Length != 0 ? (names.Symbol, names.SymbolEnds) : (code, "LL");
    }

    /// <summary>Adds the digits of a formatted decimal string: integer groups, the decimal separator, the fraction.</summary>
    private void AddDigits(Built built, string n)
    {
        var point = n.IndexOf('.');
        var integer = point < 0 ? n : n[..point];
        var fraction = point < 0 ? null : n[(point + 1)..];

        var main = JsNumberPattern.Parse(MainPattern());
        var grouping1 = main.Grouping1;
        var grouping2 = main.Grouping2 <= 0 ? grouping1 : main.Grouping2;
        var minimumGrouping = UseGrouping switch
        {
            "min2" => 2,
            "auto" => int.TryParse(data.Value(language, "minimumGroupingDigits"), out var locale) ? locale : 1,
            _ => 1,
        };

        // ICU'S GROUPING TEST: a separator follows the digit at position p (0 for the ones) when
        // p - g1 is a nonnegative multiple of g2, and the number groups at all only when its leading
        // group holds at least the minimum grouping digits.
        var upper = integer.Length - 1;
        var groups = UseGrouping is not null && grouping1 > 0 && upper - grouping1 + 1 >= minimumGrouping;
        var group = Symbol("group", ",");
        var start = 0;

        for (var index = 0; groups && index < integer.Length; index++)
        {
            var position = upper - index;

            if (position >= grouping1 && (position - grouping1) % grouping2 == 0)
            {
                built.Core.Add(new Segment("integer", Transliterate(integer[start..(index + 1)])));
                built.Core.Add(new Segment("group", group));
                start = index + 1;
            }
        }

        built.Core.Add(new Segment("integer", Transliterate(integer[start..])));

        if (fraction is not null)
        {
            built.Core.Add(new Segment("decimal", Symbol("decimal", ".")));
            built.Core.Add(new Segment("fraction", Transliterate(fraction)));
        }
    }

    /// <summary>ASCII digits as the numbering system's.</summary>
    private string Transliterate(string ascii)
    {
        if (digits is null)
        {
            return ascii;
        }

        var text = new StringBuilder(ascii.Length);

        foreach (var c in ascii)
        {
            text.Append(char.IsAsciiDigit(c) ? digits[c - '0'] : c.ToString());
        }

        return text.ToString();
    }

    /// <summary>A symbol of the locale's <c>latn</c> symbols, which every numbering system writes with here.</summary>
    private string Symbol(string name, string fallback) => data.Value(language, "symbols." + name) ?? fallback;

    /// <summary>The code points of a string.</summary>
    private static int CodePoints(string text)
    {
        var count = 0;

        for (var at = 0; at < text.Length; at++)
        {
            if (char.IsHighSurrogate(text[at]) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]))
            {
                at++;
            }

            count++;
        }

        return count;
    }
}
