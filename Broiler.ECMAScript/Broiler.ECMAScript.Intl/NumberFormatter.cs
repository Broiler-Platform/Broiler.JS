// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   47
// Annotated:        47/47
// Exempt:           45
// Human-reviewed:   0/47
// IP risk:          Low
// Security risk:    High
// Criteria:         7/7
// Resource impact:  3/10 max
// Unverified:       47
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// One configured <c>Intl.NumberFormat</c>'s formatting: PartitionNumberPattern (<c>sec-partitionnumberpattern</c>) and
/// PartitionNumberRangePattern (<c>sec-partitionnumberrangepattern</c>) over the locale's CLDR patterns, as FormatNumeric,
/// FormatNumericToParts, FormatNumericRange and FormatNumericRangeToParts answer them (Broiler.VM's JSD-0082, sub-area (c),
/// rows N1 to N17).
/// </summary>
/// <remarks>
/// <para>
/// <b>A formatted number is built in three layers around its digits</b>, the way ICU builds one and the way ECMA-402 leaves
/// to the implementation: an inner layer, the scientific exponent; a middle layer, the pattern's prefix and suffix, which
/// carry the sign, the currency symbol, the percent sign and a compact suffix; and an outer layer, a unit's or a currency's
/// name. A range repeats or shares each layer by ICU's rules - the outer when the two are the same name, the middle when the
/// two are the same text of more than one character - and puts spaces round the separator when a layer is repeated, so
/// English writes <c>$1.00 - $5.00</c> with spaces round its en dash, and <c>-$5.00-1.00</c> without (CollapseNumberRange is
/// an implementation-defined operation, <c>sec-collapsenumberrange</c>; rows N9a, N9b and N9d).
/// </para>
/// <para>
/// <b>A field's leading and trailing spaces are its neighbours' literals</b>, as ICU trims a field of default-ignorable
/// characters, so the compact <c>1 thousand</c> is an integer, a literal and a compact part.
/// </para>
/// <para>
/// <b>The locale data is read at the data locale</b>, ResolveLocale's [[DataLocale]], through the <see cref="ILocaleData"/>
/// the engine hands in, ordinarily a <see cref="CompositeLocaleData"/>, which walks the locale's parents; plural categories,
/// which pick a unit's or a currency's name, are the data locale's language's (<see cref="PluralEngine"/>). Broiler.VM read
/// every value at the data locale's language; its tables carry its locales' languages alone, so for its list the two
/// readings are the same lines.
/// </para>
/// <para>
/// <b>A numbering system other than <c>latn</c> writes its own symbols</b> where the data carries them (decision D21): the
/// number table's <c>symbols@&lt;system&gt;.&lt;name&gt;</c> at the data locale, then at root (<c>und</c>), then the data
/// locale's <c>latn</c> symbol. Broiler.VM's tables carry no such key, so with them every system writes the <c>latn</c>
/// symbols, Broiler.VM's limitation, which the decision keeps.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsNumberFormatter</c> and <c>JsNumberPattern</c></b> (<c>JsNumberData.cs</c>), at its SJL-9
/// rule commit <c>4cf9ca44</c>, with the formatting culture-free (decision D19), a status where Broiler.VM's realm threw, and
/// the caller's <see cref="WorkCharge"/> where Broiler.VM charged its engine's meter: FormatNumericToString's
/// <c>digits / 16 + 1</c> at each rounding of a value, ComputeExponent's and a range's included (decision D18). The option
/// reads, the bound <c>format</c> function and the parts arrays are the engine's.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Medium; Resources=3; Fingerprint=A50B32
// Hairs-Human:        PENDING
public sealed class NumberFormatter
{
    /// <summary>The units a sanctioned unit identifier names (ECMA-402 Table 2, <c>sec-issanctionedsingleunitidentifier</c>).</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedunitidentifier via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=0; Fingerprint=A7C6AA
    // Hairs-Human:        PENDING
    private static readonly FrozenSet<string> Sanctioned = SupportedValues.SanctionedUnits.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>One run of text and its part type.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=E898DE
    // Hairs-Human:        PENDING
    private readonly record struct Segment(string Type, string Text);

    /// <summary>A number built in its layers.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=0; Fingerprint=DB8255
    // Hairs-Human:        PENDING
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

    private readonly ILocaleData data;

    private readonly string dataLocale;

    private readonly IReadOnlyList<string>? digits;

    private readonly PluralEngine plurals;

    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=00CA7F
    // Hairs-Human:        PENDING
    private NumberFormatter(ILocaleData data, string locale, string dataLocale, string numberingSystem, NumberFormatOptions options)
    {
        this.data = data;
        this.dataLocale = dataLocale;
        Locale = locale;
        NumberingSystem = numberingSystem;
        digits = numberingSystem == "latn" ? null : data.NumberingSystemDigits(numberingSystem);
        Style = options.Style;
        Currency = options.Style == NumberStyle.Currency ? LocaleTag.AsciiUpper(options.Currency!) : null;
        CurrencyDisplay = options.Style == NumberStyle.Currency ? options.CurrencyDisplay : null;
        CurrencySign = options.Style == NumberStyle.Currency ? options.CurrencySign : null;
        Unit = options.Style == NumberStyle.Unit ? options.Unit : null;
        UnitDisplay = options.Style == NumberStyle.Unit ? options.UnitDisplay : null;
        DigitOptions = options.DigitOptions;
        Notation = options.Notation;
        CompactDisplay = options.Notation == NumberNotation.Compact ? options.CompactDisplay : null;
        UseGrouping = options.UseGrouping;
        SignDisplay = options.SignDisplay;
        var language = LocaleId.TryParse(dataLocale, out var id) == IntlStatus.Completed ? id!.Language : "en";
        plurals = PluralEngine.Create(data, language, PluralRuleType.Cardinal);
    }

    // ---- the resolved options (ECMA-402 Table 15) ---------------------------------------------------------------------------

    /// <summary>[[Locale]].</summary>
    public string Locale { get; }

    /// <summary>[[DataLocale]], at which the locale data is read.</summary>
    public string DataLocale => dataLocale;

    /// <summary>[[NumberingSystem]].</summary>
    public string NumberingSystem { get; }

    /// <summary>[[Style]].</summary>
    public NumberStyle Style { get; }

    /// <summary>[[Currency]], ASCII upper case, in the currency style; <see langword="null"/> otherwise.</summary>
    public string? Currency { get; }

    /// <summary>[[CurrencyDisplay]] in the currency style; <see langword="null"/> otherwise.</summary>
    public CurrencyDisplay? CurrencyDisplay { get; }

    /// <summary>[[CurrencySign]] in the currency style; <see langword="null"/> otherwise.</summary>
    public CurrencySign? CurrencySign { get; }

    /// <summary>[[Unit]] in the unit style; <see langword="null"/> otherwise.</summary>
    public string? Unit { get; }

    /// <summary>[[UnitDisplay]] in the unit style; <see langword="null"/> otherwise.</summary>
    public UnitDisplay? UnitDisplay { get; }

    /// <summary>The digit options: [[MinimumIntegerDigits]] to [[TrailingZeroDisplay]].</summary>
    public DigitOptions DigitOptions { get; }

    /// <summary>[[Notation]].</summary>
    public NumberNotation Notation { get; }

    /// <summary>[[CompactDisplay]] in compact notation; <see langword="null"/> otherwise.</summary>
    public CompactDisplay? CompactDisplay { get; }

    /// <summary>[[UseGrouping]].</summary>
    public NumberGrouping UseGrouping { get; }

    /// <summary>[[SignDisplay]].</summary>
    public SignDisplay SignDisplay { get; }

    // ---- making one -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The formatter an <c>Intl.NumberFormat</c> constructor makes from what it resolved (<c>sec-intl.numberformat</c>).
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">ResolveLocale's [[Locale]].</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]], at which the patterns, symbols and names are read.</param>
    /// <param name="numberingSystem">The resolved numbering system, the <c>nu</c> key's value: <c>latn</c>, or one of
    /// <see cref="ILocaleData.NumberingSystems"/>, whose digits replace the ASCII digits; any other writes ASCII digits.</param>
    /// <param name="options">What the constructor read; its currency and unit already checked by
    /// <see cref="IsWellFormedCurrencyCode"/> and <see cref="IsWellFormedUnitIdentifier"/>.</param>
    /// <returns>The formatter.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">The currency style without a well-formed currency, or the unit style without a
    /// well-formed unit: the constructor's checks, which come first, were not made.</exception>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=07F120
    // Hairs-Human:        PENDING
    public static NumberFormatter Create(ILocaleData data, string locale, string dataLocale, string numberingSystem, NumberFormatOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(dataLocale);
        ArgumentNullException.ThrowIfNull(numberingSystem);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.DigitOptions, nameof(options));

        if (options.Style == NumberStyle.Currency && (options.Currency is null || !IsWellFormedCurrencyCode(options.Currency)))
        {
            throw new ArgumentException("the currency style takes a well-formed currency code", nameof(options));
        }

        if (options.Style == NumberStyle.Unit && (options.Unit is null || !IsWellFormedUnitIdentifier(options.Unit)))
        {
            throw new ArgumentException("the unit style takes a well-formed unit identifier", nameof(options));
        }

        return new NumberFormatter(data, locale, dataLocale, numberingSystem, options);
    }

    /// <summary>IsWellFormedCurrencyCode (<c>sec-iswellformedcurrencycode</c>): three ASCII letters, in either case.</summary>
    /// <param name="currency">The currency code, as the option's String.</param>
    /// <returns>Whether it is well-formed; an engine throws a <c>RangeError</c> where it is not.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedcurrencycode via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=1; Fingerprint=B717C5
    // Hairs-Human:        PENDING
    public static bool IsWellFormedCurrencyCode(string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return currency.Length == 3 && char.IsAsciiLetter(currency[0]) && char.IsAsciiLetter(currency[1]) && char.IsAsciiLetter(currency[2]);
    }

    /// <summary>
    /// IsWellFormedUnitIdentifier (<c>sec-iswellformedunitidentifier</c>): a sanctioned single unit, or two joined by
    /// <c>-per-</c>.
    /// </summary>
    /// <param name="unit">The unit identifier, as the option's String.</param>
    /// <returns>Whether it is well-formed; an engine throws a <c>RangeError</c> where it is not.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-iswellformedunitidentifier via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=1; Fingerprint=BD2F65
    // Hairs-Human:        PENDING
    public static bool IsWellFormedUnitIdentifier(string unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (Sanctioned.Contains(unit))
        {
            return true;
        }

        var per = unit.IndexOf("-per-", StringComparison.Ordinal);
        return per > 0 && Sanctioned.Contains(unit[..per]) && Sanctioned.Contains(unit[(per + 5)..]);
    }

    /// <summary>
    /// The values of the <c>nu</c> key an <c>Intl.NumberFormat</c> hands ResolveLocale for a locale ([[LocaleData]].[[&lt;locale&gt;]].[[nu]]):
    /// the locale's default numbering system first - the number data's <c>defaultNumberingSystem</c> along the locale's chain,
    /// <c>latn</c> where the data names none - then <c>latn</c>, then the data's other numbering systems in code-unit order.
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">The matched available locale.</param>
    /// <returns>The values, each once. For Broiler.VM's tables, whose locales' default is <c>latn</c>, they are Broiler.VM's list,
    /// <c>latn</c> first (row N8b).</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=9F212E
    // Hairs-Human:        PENDING
    public static IReadOnlyList<string?> NumberingSystemKeyValues(ILocaleData data, string locale)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        var first = data.NumberValue(locale, "defaultNumberingSystem") is { Length: > 0 } stated && (stated == "latn" || data.NumberingSystemDigits(stated) is not null)
            ? stated
            : "latn";
        var others = new List<string>();

        foreach (var system in data.NumberingSystems)
        {
            if (system != first && system != "latn")
            {
                others.Add(system);
            }
        }

        others.Sort(StringComparer.Ordinal);
        var values = new List<string?> { first };

        if (first != "latn")
        {
            values.Add("latn");
        }

        values.AddRange(others);
        return values.AsReadOnly();
    }

    // ---- the operations ---------------------------------------------------------------------------------------------------

    /// <summary>FormatNumeric (<c>sec-formatnumber</c>): the String PartitionNumberPattern's parts make.</summary>
    /// <param name="x">The Intl mathematical value.</param>
    /// <param name="charge">The caller's meter: FormatNumericToString's <c>digits / 16 + 1</c> units at each rounding,
    /// ComputeExponent's included (decision D18).</param>
    /// <returns>The formatted String.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=861D5F
    // Hairs-Human:        PENDING
    public string Format(IntlDecimal x, WorkCharge? charge = null)
    {
        var text = new StringBuilder();

        foreach (var part in FormatToParts(x, charge))
        {
            text.Append(part.Value);
        }

        return text.ToString();
    }

    /// <summary>PartitionNumberPattern (<c>sec-partitionnumberpattern</c>), as FormatNumericToParts answers it.</summary>
    /// <param name="x">The Intl mathematical value.</param>
    /// <param name="charge">The caller's meter, as for <see cref="Format"/>.</param>
    /// <returns>The parts, with no source.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=C91245
    // Hairs-Human:        PENDING
    public IReadOnlyList<NumberPart> FormatToParts(IntlDecimal x, WorkCharge? charge = null)
    {
        var built = Build(x, approximately: false, charge);
        var parts = new List<(Segment Segment, string? Source)>();
        Emit(parts, built.OuterPrefix, built.MiddlePrefix, null);
        AddCore(parts, built, built, null);
        Emit(parts, built.MiddleSuffix, built.OuterSuffix, null);
        return Flatten(parts);
    }

    /// <summary>FormatNumericRange (<c>sec-formatnumericrange</c>): the String PartitionNumberRangePattern's parts make.</summary>
    /// <param name="x">The start.</param>
    /// <param name="y">The end.</param>
    /// <param name="formatted">The String, or the empty string.</param>
    /// <param name="charge">The caller's meter, as Broiler.VM charges it: each end formatted once, to compare the two Strings;
    /// then, where they differ, each end formatted again for its parts, and where they are the same, the start alone formatted
    /// once more, with the approximately sign. With one unit to a formatting, 4 units and 3.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.RangeIsNotANumber"/>, a <c>RangeError</c>.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=ACCEEE
    // Hairs-Human:        PENDING
    public IntlStatus FormatRange(IntlDecimal x, IntlDecimal y, out string formatted, WorkCharge? charge = null)
    {
        var status = FormatRangeToParts(x, y, out var parts, charge);
        var text = new StringBuilder();

        foreach (var part in parts)
        {
            text.Append(part.Value);
        }

        formatted = text.ToString();
        return status;
    }

    /// <summary>PartitionNumberRangePattern (<c>sec-partitionnumberrangepattern</c>), as FormatNumericRangeToParts answers it.</summary>
    /// <param name="x">The start.</param>
    /// <param name="y">The end.</param>
    /// <param name="parts">The parts, each with its source; empty where the range is refused.</param>
    /// <param name="charge">The caller's meter, as for <see cref="FormatRange"/>.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.RangeIsNotANumber"/>, a <c>RangeError</c>, at
    /// step 1: an end is not-a-number.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=88F9D9
    // Hairs-Human:        PENDING
    public IntlStatus FormatRangeToParts(IntlDecimal x, IntlDecimal y, out IReadOnlyList<NumberPart> parts, WorkCharge? charge = null)
    {
        if (x.Kind == IntlDecimalKind.NaN || y.Kind == IntlDecimalKind.NaN)
        {
            parts = [];
            return IntlStatus.RangeIsNotANumber;
        }

        parts = RangeParts(x, y, charge);
        return IntlStatus.Completed;
    }

    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=High; Resources=2; Fingerprint=D2F819
    // Hairs-Falsified-If: a range collapses so that two different ranges write the same String, or its charges differ from each end formatted once and then each end again where the two differ, or the start alone again where they are the same
    // Hairs-Human:        PENDING
    private List<NumberPart> RangeParts(IntlDecimal x, IntlDecimal y, WorkCharge? charge)
    {
        if (string.Equals(Format(x, charge), Format(y, charge), StringComparison.Ordinal))
        {
            var approximate = Build(x, approximately: true, charge);
            var shared = new List<(Segment, string?)>();
            Emit(shared, approximate.OuterPrefix, approximate.MiddlePrefix, "shared");
            AddCore(shared, approximate, approximate, "shared");
            Emit(shared, approximate.MiddleSuffix, approximate.OuterSuffix, "shared");
            return Flatten(shared);
        }

        var first = Build(x, approximately: false, charge);
        var second = Build(y, approximately: false, charge);

        var collapseOuter = string.Equals(OuterKey(first), OuterKey(second), StringComparison.Ordinal);
        var collapseMiddle = collapseOuter &&
            string.Equals(first.MiddleKey, second.MiddleKey, StringComparison.Ordinal) &&
            first.MiddleLength > 1;

        var repeat = first.InnerLength > 0 ||
            (!collapseMiddle && first.MiddleLength > 0) ||
            (!collapseOuter && first.OuterLength > 0);

        var range = Value("misc.range") ?? "{0}\u2013{1}";
        var open = range.IndexOf("{0}", StringComparison.Ordinal);
        var close = range.IndexOf("{1}", StringComparison.Ordinal);
        var separator = range[(open + 3)..close];

        if (repeat)
        {
            separator = (separator.Length != 0 && IsPatternWhiteSpace(separator[0]) ? string.Empty : " ") + separator +
                (separator.Length != 0 && IsPatternWhiteSpace(separator[^1]) ? string.Empty : " ");
        }

        var parts = new List<(Segment Segment, string? Source)>();

        // THE COLLAPSED OUTER NAME TAKES THE RANGE'S PLURAL, by the language's plural ranges (rows N9d and P5b).
        Built? outer = null;

        if (collapseOuter && first.OuterLength > 0)
        {
            outer = new Built();
            AddOuter(outer, plurals.SelectRange(first.Plural, second.Plural));
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
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=B04E9C
    // Hairs-Human:        PENDING
    private string OuterKey(Built built) => built.OuterLength == 0 ? string.Empty : StyleName(Style) + "|" + Unit + "|" + Currency;

    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D19 culture-free formatting; IP=Low; Security=Low; Resources=1; Fingerprint=14CAAA
    // Hairs-Human:        PENDING
    private static string StyleName(NumberStyle style) => style switch
    {
        NumberStyle.Percent => "percent",
        NumberStyle.Currency => "currency",
        NumberStyle.Unit => "unit",
        _ => "decimal",
    };

    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=A29E7C
    // Hairs-Human:        PENDING
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
    /// The digits with the currency spacing on each side: the spacing before them is decided by <paramref name="prefixOf"/>'s
    /// prefix and after them by <paramref name="suffixOf"/>'s suffix (CLDR's <c>currencySpacing</c>, row N3b).
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=EE2AE8
    // Hairs-Human:        PENDING
    private void AddCore(List<(Segment, string?)> parts, Built? prefixOf, Built? suffixOf, string? source)
    {
        var core = prefixOf ?? suffixOf!;

        if (prefixOf is { PrefixSpacing: true, CoreIsDigits: true })
        {
            parts.Add((new Segment("literal", Value("currency.currencySpacing.beforeCurrency.insertBetween") ?? "\u00a0"), source));
        }

        foreach (var segment in core.Core)
        {
            parts.Add((segment, source));
        }

        if (suffixOf is { SuffixSpacing: true, CoreIsDigits: true })
        {
            parts.Add((new Segment("literal", Value("currency.currencySpacing.afterCurrency.insertBetween") ?? "\u00a0"), source));
        }
    }

    /// <summary>
    /// The parts a list of segments is: each field trimmed of the default-ignorable characters at its ends, which become
    /// literals, and adjacent literals joined, shared where their sources differ.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=66BAEE
    // Hairs-Human:        PENDING
    private static List<NumberPart> Flatten(List<(Segment Segment, string? Source)> segments)
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

        var parts = new List<NumberPart>(trimmed.Count);

        foreach (var (segment, source) in trimmed)
        {
            if (segment.Type == "literal" && parts.Count != 0 && parts[^1].Type == "literal")
            {
                var previous = parts[^1];
                parts[^1] = new NumberPart(
                    "literal",
                    previous.Value + segment.Text,
                    previous.Source == source ? source : "shared");
                continue;
            }

            parts.Add(new NumberPart(segment.Type, segment.Text, source));
        }

        return parts;
    }

    /// <summary>ICU's default-ignorable set: space separators, the tab, the bidirectional controls and the variation selectors.</summary>
    /// <remarks>
    /// Unicode classification written out here, as Broiler.VM's formatter writes it, rather than read from Broiler.Unicode: it is
    /// not the UCD's Default_Ignorable_Code_Point but ICU's union of the space separators, the tab, Bidi_Control and
    /// Variation_Selector, which Broiler.Unicode names as no set.
    /// </remarks>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=8C71F7
    // Hairs-Human:        PENDING
    private static bool IsDefaultIgnorable(char c) =>
        c is ' ' or '\t' or '\u00a0' or '\u1680' or '\u202f' or '\u205f' or '\u3000' or '\u061c' or '\u200e' or '\u200f'
        || c is >= '\u2000' and <= '\u200a'
        || c is >= '\u202a' and <= '\u202e'
        || c is >= '\u2066' and <= '\u2069'
        || c is >= '\ufe00' and <= '\ufe0f'
        || c is >= '\u180b' and <= '\u180d' or '\u180f';

    /// <summary>Pattern_White_Space, which the range separator is tested against.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=B3E3F5
    // Hairs-Human:        PENDING
    private static bool IsPatternWhiteSpace(char c) =>
        c is ' ' or '\t' or '\n' or '\u000b' or '\f' or '\r' or '\u0085' or '\u200e' or '\u200f' or '\u2028' or '\u2029';

    /// <summary>
    /// White_Space as .NET's <c>char.IsWhiteSpace</c> answers it, which Broiler.VM's <c>string.Trim()</c> reads, written out so
    /// that no platform Unicode table is read (decision D19, rule SL4; row N15): the controls U+0009 to U+000D and U+0085, and
    /// the space, line and paragraph separators.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D19 culture-free formatting; IP=Low; Security=Low; Resources=1; Fingerprint=006694
    // Hairs-Human:        PENDING
    private static bool IsWhiteSpace(char c) =>
        c is >= '\u0009' and <= '\u000d' or ' ' or '\u0085' or '\u00a0' or '\u1680' or '\u2028' or '\u2029' or '\u202f' or '\u205f' or '\u3000'
        || c is >= '\u2000' and <= '\u200a';

    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D19 culture-free formatting; IP=Low; Security=Low; Resources=1; Fingerprint=D5D35B
    // Hairs-Human:        PENDING
    private static string TrimWhiteSpace(string text)
    {
        var start = 0;
        var end = text.Length;

        while (start < end && IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end > start && IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return text[start..end];
    }

    // ---- building one number ----------------------------------------------------------------------------------------------

    /// <summary>PartitionNumberPattern, layered.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=High; Resources=2; Fingerprint=523B15
    // Hairs-Falsified-If: a number's String or parts differ from PartitionNumberPattern's over the locale's CLDR patterns and symbols, or a charge moves
    // Hairs-Human:        PENDING
    private Built Build(IntlDecimal x, bool approximately, WorkCharge? charge)
    {
        var built = new Built();
        var exponent = 0;
        string n;
        IntlDecimal rounded;

        if (x.Kind == IntlDecimalKind.NaN)
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
            if (!(x.IsZero && x.IsNegative))
            {
                if (Style == NumberStyle.Percent)
                {
                    x = x.Scale(2);
                }

                exponent = ComputeExponent(x, charge);
                x = x.Scale(-exponent);
            }

            var formatted = NumericFormat.FormatNumericToString(DigitOptions, x, charge);
            (rounded, n) = (formatted.RoundedNumber, formatted.FormattedString);
        }

        // THE SIGNUM, AS ICU READS IT FROM THE ROUNDED VALUE: not-a-number is a positive zero.
        var signum = rounded.Kind switch
        {
            IntlDecimalKind.NaN => 1,
            IntlDecimalKind.PositiveInfinity => 2,
            IntlDecimalKind.NegativeInfinity => -2,
            _ when rounded.IsZero => rounded.IsNegative ? -1 : 1,
            _ => rounded.IsNegative ? -2 : 2,
        };

        // POS 0, POS_SIGN 1, NEG 2 (ICU's PatternSignType, from the sign display).
        var signType = SignDisplay switch
        {
            SignDisplay.Always => signum < 0 ? 2 : 1,
            SignDisplay.ExceptZero => signum == -2 ? 2 : signum == 2 ? 1 : 0,
            SignDisplay.Negative => signum == -2 ? 2 : 0,
            SignDisplay.Never => 0,
            _ => signum < 0 ? 2 : 0,
        };

        var compactKey = Notation == NumberNotation.Compact && rounded.IsFinite && !rounded.IsZero
            ? Math.Min(exponent + rounded.Magnitude, 14)
            : 0;

        built.Plural = plurals.Select(rounded.IsFinite ? n : "0", Notation == NumberNotation.Compact ? exponent : 0);

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
        if (x.Kind == IntlDecimalKind.NaN)
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

            if (Notation is NumberNotation.Scientific or NumberNotation.Engineering)
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

                var power = Transliterate(Math.Abs((long)exponent).ToString(NumberFormatInfo.InvariantInfo));
                built.Core.Add(new Segment("exponentInteger", power));
                built.InnerLength += CodePoints(power);
            }
        }

        // THE OUTER LAYER: a unit's or a currency's name.
        AddOuter(built, built.Plural);
        return built;
    }

    /// <summary>The outer layer for a plural: a unit's pattern, or a currency's name in its pattern (rows N4 and N5).</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=7B0AD0
    // Hairs-Human:        PENDING
    private void AddOuter(Built built, string plural)
    {
        string? pattern = null;
        string? name = null;
        var nameType = "unit";

        if (Style == NumberStyle.Unit && Unit is not null && Unit != "percent")
        {
            pattern = UnitPattern(Unit, UnitDisplay ?? Intl.UnitDisplay.Short, plural);
        }
        else if (Style == NumberStyle.Currency && CurrencyDisplay == Intl.CurrencyDisplay.Name && Currency is not null)
        {
            pattern = Value("currency.unitPattern-count-" + plural) ??
                Value("currency.unitPattern-count-other") ?? "{0} {1}";
            var names = data.Currency(dataLocale, Currency);
            name = names is not { } found ? Currency
                : (plural == "one" ? found.One : found.Other) is { Length: > 0 } counted ? counted
                : found.Other.Length != 0 ? found.Other
                : found.Name.Length != 0 ? found.Name : Currency;
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
    /// A unit's pattern in a width and a plural: CLDR's own for a simple unit or a compound it names, or the numerator's
    /// pattern inside the denominator's per-unit pattern or the width's compound pattern (row N5b, implementation-defined).
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=High; Resources=2; Fingerprint=3ED6B0
    // Hairs-Falsified-If: a unit's pattern is not CLDR's for the width and plural, or a compound is composed otherwise than per-unit then compound pattern
    // Hairs-Human:        PENDING
    private string? UnitPattern(string unit, UnitDisplay display, string plural)
    {
        string[] widths = display switch
        {
            Intl.UnitDisplay.Narrow => ["narrow", "short", "long"],
            Intl.UnitDisplay.Short => ["short", "long"],
            _ => ["long"],
        };

        string? Field(string id, string field)
        {
            foreach (var w in widths)
            {
                if (data.UnitPattern(dataLocale, w, id, field) is { } value)
                {
                    return value;
                }
            }

            return null;
        }

        // A WIDTH'S OWN OTHER FORM BEFORE A WIDER WIDTH: CLDR gives German's short nanosecond only `{0} ns`, and one
        // nanosecond is `1 ns`, as ICU writes it, not the long `1 Nanosekunde`.
        string? Pattern(string id)
        {
            foreach (var w in widths)
            {
                if ((data.UnitPattern(dataLocale, w, id, "unitPattern-count-" + plural) ??
                    data.UnitPattern(dataLocale, w, id, "unitPattern-count-other")) is { } value)
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
        var single = TrimWhiteSpace((Field(denominator, "unitPattern-count-one") ?? Field(denominator, "unitPattern-count-other") ?? denominator)
            .Replace("{0}", string.Empty, StringComparison.Ordinal));

        return compound.Replace("{0}", numerator, StringComparison.Ordinal).Replace("{1}", single, StringComparison.Ordinal);
    }

    /// <summary>
    /// The pattern the value is written by, and a compact pattern whose affix joins it next to the number. A compact currency
    /// pattern carries its own currency, so it is the pattern itself; any other compact pattern's affix joins the style's.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=A7B763
    // Hairs-Human:        PENDING
    private (NumberPattern Pattern, bool PatternIsCompact, NumberPattern? Joined) Patterns(int compactKey, int exponent, string plural)
    {
        var main = NumberPattern.Parse(MainPattern());

        if (Notation != NumberNotation.Compact || exponent == 0 || CompactPattern(compactKey, plural) is not { } compact)
        {
            return (main, false, null);
        }

        return Style == NumberStyle.Currency && CurrencyDisplay != Intl.CurrencyDisplay.Name
            ? (NumberPattern.Parse(compact), true, null)
            : (main, false, NumberPattern.Parse(compact));
    }

    /// <summary>The style's pattern.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=E609E2
    // Hairs-Human:        PENDING
    private string MainPattern() => Style switch
    {
        NumberStyle.Percent => Value("percent.standard") ?? "#,##0%",
        NumberStyle.Unit when Unit == "percent" => Value("percent.standard") ?? "#,##0%",
        NumberStyle.Currency when CurrencyDisplay == Intl.CurrencyDisplay.Name => Value("decimal.standard") ?? "#,##0.###",
        NumberStyle.Currency => (CurrencySign == Intl.CurrencySign.Accounting ? Value("currency.accounting") : null) ??
            Value("currency.standard") ?? "\u00a4#,##0.00",
        _ => Value("decimal.standard") ?? "#,##0.###",
    };

    /// <summary>The compact pattern for a magnitude and a plural, or nothing where the locale does not compact it.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=5D034D
    // Hairs-Human:        PENDING
    private string? CompactPattern(int magnitude, string plural)
    {
        if (magnitude < 3)
        {
            return null;
        }

        var key = "1" + new string('0', Math.Min(magnitude, 14));
        var root = Style == NumberStyle.Currency && CurrencyDisplay != Intl.CurrencyDisplay.Name
            ? "currency.short.standard."
            : CompactDisplay == Intl.CompactDisplay.Long ? "decimal.long.decimalFormat." : "decimal.short.decimalFormat.";

        var pattern = Value(root + key + "-count-" + plural) ?? Value(root + key + "-count-other");
        return pattern is null or "0" ? null : pattern;
    }

    /// <summary>
    /// ComputeExponentForMagnitude (<c>sec-computeexponentformagnitude</c>): 0 for the standard notation, the magnitude for the
    /// scientific, its multiple of three below for the engineering, and the compact pattern's.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=12FBCC
    // Hairs-Human:        PENDING
    private int ExponentForMagnitude(int magnitude)
    {
        switch (Notation)
        {
            case NumberNotation.Scientific:
                return magnitude;
            case NumberNotation.Engineering:
                return (int)Math.Floor(magnitude / 3.0) * 3;
            case NumberNotation.Compact:
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

    /// <summary>ComputeExponent (<c>sec-computeexponent</c>), with the exponent taken after rounding (row N6a).</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=High; Resources=2; Fingerprint=4D037C
    // Hairs-Falsified-If: the exponent differs from ComputeExponent's after rounding, or its rounding charges other than digits / 16 + 1
    // Hairs-Human:        PENDING
    private int ComputeExponent(IntlDecimal x, WorkCharge? charge)
    {
        if (x.IsZero || Notation == NumberNotation.Standard)
        {
            return 0;
        }

        var magnitude = x.Magnitude;
        var exponent = ExponentForMagnitude(magnitude);
        var rounded = NumericFormat.FormatNumericToString(DigitOptions, x.Abs().Scale(-exponent), charge).RoundedNumber;

        if (rounded.IsZero)
        {
            return exponent;
        }

        return rounded.Magnitude == magnitude - exponent ? exponent : ExponentForMagnitude(magnitude + 1);
    }

    /// <summary>
    /// One affix as ICU's patternInfoToStringBuilder writes it: the positive or negative subpattern's tokens, with the sign, a
    /// plus sign or the approximately sign at the sign's place.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Medium; Resources=2; Fingerprint=6263F2
    // Hairs-Human:        PENDING
    private List<Segment> Affix(NumberPattern pattern, bool patternIsCompact, NumberPattern? joined, bool isPrefix, int signType, bool approximately, out bool currencyAtNumber)
    {
        var plusReplacesMinus = signType == 1 && !pattern.PositiveHasPlus;
        var useNegative = pattern.NegativePrefix is not null &&
            (signType == 2 || (pattern.NegativeHasMinus && !plusReplacesMinus && !approximately));

        var tokens = new List<(AffixToken Token, bool Compact)>();

        foreach (var token in useNegative ? (isPrefix ? pattern.NegativePrefix! : pattern.NegativeSuffix!) : (isPrefix ? pattern.PositivePrefix : pattern.PositiveSuffix))
        {
            tokens.Add((token, patternIsCompact && token.Kind == AffixKind.Literal));
        }

        // A JOINED COMPACT AFFIX SITS NEXT TO THE NUMBER, its text the compact part.
        if (joined is not null)
        {
            var compactTokens = new List<(AffixToken, bool)>();

            foreach (var token in isPrefix ? joined.PositivePrefix : joined.PositiveSuffix)
            {
                compactTokens.Add((token, token.Kind == AffixKind.Literal));
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
                case AffixKind.Minus:
                    Sign();
                    break;
                case AffixKind.Plus:
                    segments.Add(new Segment("plusSign", Symbol("plusSign", "+")));
                    break;
                case AffixKind.Percent:
                    segments.Add(new Segment(Style == NumberStyle.Unit ? "unit" : "percentSign", Symbol("percentSign", "%")));
                    break;
                case AffixKind.PerMille:
                    segments.Add(new Segment("literal", Symbol("perMille", "\u2030")));
                    break;
                case AffixKind.Currency:
                    var (text, ends) = CurrencyText(token.Text.Length);
                    segments.Add(new Segment("currency", text));

                    // CLDR'S CURRENCY SPACING: only a currency next to the number, and only when its character there is neither
                    // a symbol nor a separator.
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
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=E6E33D
    // Hairs-Human:        PENDING
    private (string Text, string Ends) CurrencyText(int count)
    {
        var code = Currency ?? "XXX";
        var names = data.Currency(dataLocale, code);

        if (count >= 2 || CurrencyDisplay == Intl.CurrencyDisplay.Code || names is not { } found)
        {
            return (code, "LL");
        }

        if (CurrencyDisplay == Intl.CurrencyDisplay.NarrowSymbol && found.Narrow.Length != 0)
        {
            return (found.Narrow, found.NarrowEnds);
        }

        return found.Symbol.Length != 0 ? (found.Symbol, found.SymbolEnds) : (code, "LL");
    }

    /// <summary>Adds the digits of a formatted decimal String: integer groups, the decimal separator, the fraction.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=High; Resources=2; Fingerprint=41FED9
    // Hairs-Falsified-If: a grouping separator is written where ICU's grouping test with the locale's minimum grouping digits writes none, or the reverse
    // Hairs-Human:        PENDING
    private void AddDigits(Built built, string n)
    {
        var point = n.IndexOf('.');
        var integer = point < 0 ? n : n[..point];
        var fraction = point < 0 ? null : n[(point + 1)..];

        var main = NumberPattern.Parse(MainPattern());
        var grouping1 = main.Grouping1;
        var grouping2 = main.Grouping2 <= 0 ? grouping1 : main.Grouping2;
        var minimumGrouping = UseGrouping switch
        {
            NumberGrouping.Min2 => 2,
            NumberGrouping.Auto => int.TryParse(Value("minimumGroupingDigits"), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var locale) ? locale : 1,
            _ => 1,
        };

        // ICU'S GROUPING TEST: a separator follows the digit at position p (0 for the ones) when p - g1 is a nonnegative
        // multiple of g2, and the number groups at all only when its leading group holds at least the minimum grouping digits.
        var upper = integer.Length - 1;
        var groups = UseGrouping != NumberGrouping.Off && grouping1 > 0 && upper - grouping1 + 1 >= minimumGrouping;
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
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=800AE4
    // Hairs-Human:        PENDING
    private string Transliterate(string ascii)
    {
        if (digits is null)
        {
            return ascii;
        }

        var text = new StringBuilder(ascii.Length);

        foreach (var c in ascii)
        {
            if (char.IsAsciiDigit(c))
            {
                text.Append(digits[c - '0']);
            }
            else
            {
                text.Append(c);
            }
        }

        return text.ToString();
    }

    /// <summary>One value of the data locale's number data, or nothing.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D19 culture-free formatting; IP=Low; Security=Low; Resources=1; Fingerprint=F69C78
    // Hairs-Human:        PENDING
    private string? Value(string key) => data.NumberValue(dataLocale, key);

    /// <summary>
    /// A symbol in the numbering system (decision D21): a system other than <c>latn</c> reads its own symbol at the data
    /// locale, then at root, where the data carries it; every system then reads the data locale's <c>latn</c> symbol.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=High; Resources=1; Fingerprint=5D9D8C
    // Hairs-Falsified-If: a numbering system's symbol is read otherwise than its own at the data locale, then at root, then the latn one
    // Hairs-Human:        PENDING
    private string Symbol(string name, string fallback)
    {
        if (NumberingSystem != "latn")
        {
            var own = "symbols@" + NumberingSystem + "." + name;

            if ((data.NumberValue(dataLocale, own) ?? data.NumberValue("und", own)) is { } stated)
            {
                return stated;
            }
        }

        return Value("symbols." + name) ?? fallback;
    }

    /// <summary>The code points of a String, which a range's collapse counts (row N17b).</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-partitionnumberpattern sec-partitionnumberrangepattern sec-computeexponent sec-collapsenumberrange via Broiler.VM JsNumberFormatter; IP=Low; Security=Low; Resources=1; Fingerprint=7340D5
    // Hairs-Human:        PENDING
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

/// <summary>One token of a pattern's prefix or suffix.</summary>
// Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=0; Fingerprint=C8D6E0
// Hairs-Human:        PENDING
internal enum AffixKind
{
    /// <summary>Text, as written.</summary>
    Literal,

    /// <summary>The currency, U+00A4 CURRENCY SIGN.</summary>
    Currency,

    /// <summary>The percent sign, <c>%</c>.</summary>
    Percent,

    /// <summary>The per-mille sign.</summary>
    PerMille,

    /// <summary>The sign's place, <c>-</c>.</summary>
    Minus,

    /// <summary>A plus sign, <c>+</c>.</summary>
    Plus,
}

/// <summary>One affix token.</summary>
// Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=0; Fingerprint=DD6046
// Hairs-Human:        PENDING
internal readonly record struct AffixToken(AffixKind Kind, string Text);

/// <summary>
/// A CLDR number pattern, as UTS #35 Part 3 section 3 reads one: a positive subpattern and an optional negative one, each a
/// prefix, a number and a suffix. Ported from Broiler.VM's <c>JsNumberPattern</c> at <c>4cf9ca44</c>.
/// </summary>
/// <remarks>
/// <b>Only what a formatter reads from it is kept</b>: the affixes, the grouping sizes and, for a compact pattern, how many
/// digits it shows. The digit counts of the number part are ECMA-402's options and not the pattern's.
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=0; Fingerprint=E89FB2
// Hairs-Human:        PENDING
internal sealed class NumberPattern
{
    private NumberPattern() { }

    internal List<AffixToken> PositivePrefix { get; private set; } = [];

    internal List<AffixToken> PositiveSuffix { get; private set; } = [];

    internal List<AffixToken>? NegativePrefix { get; private set; }

    internal List<AffixToken>? NegativeSuffix { get; private set; }

    /// <summary>The primary grouping size, or -1 where the pattern does not group.</summary>
    internal int Grouping1 { get; private set; } = -1;

    /// <summary>The secondary grouping size.</summary>
    internal int Grouping2 { get; private set; } = -1;

    /// <summary>How many <c>0</c> digits the number part shows, which a compact pattern's exponent is read from.</summary>
    internal int Zeros { get; private set; }

    /// <summary>Whether the positive subpattern writes a plus sign itself.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=0; Fingerprint=27364D
    // Hairs-Human:        PENDING
    internal bool PositiveHasPlus => PositivePrefix.Exists(static t => t.Kind == AffixKind.Plus) || PositiveSuffix.Exists(static t => t.Kind == AffixKind.Plus);

    /// <summary>Whether the negative subpattern has a sign's place.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=0; Fingerprint=396F7D
    // Hairs-Human:        PENDING
    internal bool NegativeHasMinus =>
        NegativePrefix is not null &&
        (NegativePrefix.Exists(static t => t.Kind == AffixKind.Minus) || NegativeSuffix!.Exists(static t => t.Kind == AffixKind.Minus));

    /// <summary>Reads a pattern; a malformed one reads as its literal text around no number.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=1; Fingerprint=DC5B0D
    // Hairs-Human:        PENDING
    internal static NumberPattern Parse(string pattern)
    {
        var result = new NumberPattern();

        var (positive, negative) = SplitSubpatterns(pattern);
        var (prefix, suffix, number) = ParseSubpattern(positive);

        result.PositivePrefix = prefix;
        result.PositiveSuffix = suffix;
        result.ReadNumber(number);

        if (negative is { } text)
        {
            var (negativePrefix, negativeSuffix, _) = ParseSubpattern(text);
            result.NegativePrefix = negativePrefix;
            result.NegativeSuffix = negativeSuffix;
        }

        return result;
    }

    /// <summary>The positive and negative subpatterns, split at the first unquoted <c>;</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=1; Fingerprint=109BB3
    // Hairs-Human:        PENDING
    private static (string Positive, string? Negative) SplitSubpatterns(string pattern)
    {
        var quoted = false;

        for (var at = 0; at < pattern.Length; at++)
        {
            if (pattern[at] == '\'')
            {
                quoted = !quoted;
            }
            else if (pattern[at] == ';' && !quoted)
            {
                return (pattern[..at], pattern[(at + 1)..]);
            }
        }

        return (pattern, null);
    }

    /// <summary>One subpattern's prefix tokens, suffix tokens and number part.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=High; Resources=2; Fingerprint=F7DD86
    // Hairs-Falsified-If: a CLDR pattern's affix is read as other tokens than UTS 35 Part 3 section 3 states
    // Hairs-Human:        PENDING
    private static (List<AffixToken> Prefix, List<AffixToken> Suffix, string Number) ParseSubpattern(string text)
    {
        var prefix = new List<AffixToken>();
        var suffix = new List<AffixToken>();
        var number = new StringBuilder();
        var stage = 0;
        var literal = new StringBuilder();

        void Flush(List<AffixToken> into)
        {
            if (literal.Length != 0)
            {
                into.Add(new AffixToken(AffixKind.Literal, literal.ToString()));
                literal.Clear();
            }
        }

        for (var at = 0; at < text.Length; at++)
        {
            var c = text[at];
            var into = stage == 0 ? prefix : suffix;

            if (c == '\'')
            {
                var close = text.IndexOf('\'', at + 1);

                if (close < 0)
                {
                    close = text.Length;
                }

                literal.Append(close == at + 1 ? "'" : text[(at + 1)..close]);
                at = close;
                continue;
            }

            if (stage != 2 && (c is '#' or '0' or '@' or ',' or '.' || char.IsAsciiDigit(c) || (stage == 1 && c is 'E' or '+')))
            {
                if (stage == 0)
                {
                    Flush(prefix);
                    stage = 1;
                }

                number.Append(c);
                continue;
            }

            if (stage == 1)
            {
                stage = 2;
                into = suffix;
            }

            switch (c)
            {
                case '\u00a4':
                    Flush(into);
                    var count = 1;

                    while (at + 1 < text.Length && text[at + 1] == '\u00a4')
                    {
                        count++;
                        at++;
                    }

                    into.Add(new AffixToken(AffixKind.Currency, new string('\u00a4', count)));
                    break;
                case '%':
                    Flush(into);
                    into.Add(new AffixToken(AffixKind.Percent, "%"));
                    break;
                case '\u2030':
                    Flush(into);
                    into.Add(new AffixToken(AffixKind.PerMille, "\u2030"));
                    break;
                case '-':
                    Flush(into);
                    into.Add(new AffixToken(AffixKind.Minus, "-"));
                    break;
                case '+':
                    Flush(into);
                    into.Add(new AffixToken(AffixKind.Plus, "+"));
                    break;
                default:
                    literal.Append(c);
                    break;
            }
        }

        Flush(stage == 0 ? prefix : suffix);
        return (prefix, suffix, number.ToString());
    }

    /// <summary>Reads the grouping sizes and the count of shown zeros from the number part.</summary>
    // Hairs-AI:           Origin=Ported; Spec=UTS35 Part 3 section 3 number patterns via Broiler.VM JsNumberPattern; IP=Low; Security=Low; Resources=1; Fingerprint=A31F46
    // Hairs-Human:        PENDING
    private void ReadNumber(string number)
    {
        var integerEnd = number.IndexOfAny(['.', 'E']);
        var integer = integerEnd < 0 ? number : number[..integerEnd];

        foreach (var c in number)
        {
            if (c == '0')
            {
                Zeros++;
            }
        }

        var last = integer.LastIndexOf(',');

        if (last < 0)
        {
            return;
        }

        Grouping1 = integer.Length - last - 1;
        var previous = integer.LastIndexOf(',', last - 1);
        Grouping2 = previous < 0 ? Grouping1 : last - previous - 1;
    }
}
