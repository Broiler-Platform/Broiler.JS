using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The differential check's sections for sub-area (c), stage SJL-9.3c (Broiler.VM's JSD-0082, decision D22): Broiler.VM's
/// <c>JsNumberFormatter</c> and its realm's <c>Intl.ListFormat</c>, <c>Intl.RelativeTimeFormat</c> and <c>Intl.DisplayNames</c>,
/// pinned in <c>Vm/</c>, against the library's <see cref="NumberFormatter"/>, <see cref="ListFormatter"/>,
/// <see cref="RelativeTimeFormatter"/> and <see cref="DisplayNamesCore"/>, over the population of <see cref="NumberTextPopulation"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each arm reads the options as an engine does.</b> Broiler.VM's arm is its own constructor over the options bag as a script
/// object; the library's reads the same bag in the constructor's order, as an engine's adapter does, and hands the library
/// what it read. The realm's prototype methods are not copied: the harness does what each does around the copied operation -
/// a range's not-a-number check (<c>RangeValues</c>), the joining of a list's parts (<c>format</c>), and <c>of</c>'s fallback.
/// </para>
/// <para>
/// <b>Every answer carries its charge</b> where Broiler.VM charges (decision D18): FormatNumericToString's at each rounding, and a
/// list's template steps. Display names charge nothing in Broiler.VM, and the library is handed no meter there (row L43).
/// </para>
/// <para>
/// <b>Broiler.VM's reading.</b> Where the two arms differ, the library is asked again as Broiler.VM reads the data: at the data
/// locale's language, with Broiler.VM's <c>nu</c> values, <c>latn</c> first, and without the numbering systems' own symbols
/// (decisions D5, D13 and D21). A difference that reading removes is the data policy of the correspondence's rows N1b to N9e,
/// N8a and N8b, S1b, S4b and S8, Broiler.JS's embedding read as the library reads it; Broiler.VM's tables hold their locales'
/// languages alone, no default numbering system but <c>latn</c> and no own symbols, so in its embedding the reading is the
/// library's own and no difference can be classed so.
/// </para>
/// </remarks>
internal static class NumberTextSection
{
    private const string DefaultLocale = "en-US";

    /// <summary>The row label of a difference Broiler.VM's reading of the data removes.</summary>
    internal const string DataRow = "C5 data (D5, D13, D21)";

    private const string ReadingMark = " (Broiler.VM's reading: ";

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale, IReadOnlyCollection<string>? only)
    {
        var arms = new Arms(embedding);
        var population = NumberTextPopulation.Build(embedding, random, scale);
        var suffix = " (" + embedding.Name + ")";

        if (only is null || only.Contains("N1"))
            yield return Formatting(arms, population, random, "N1 number formatting: format and formatToParts, with the charge" + suffix);
        if (only is null || only.Contains("N2"))
            yield return Ranges(arms, population, random, "N2 number ranges: formatRange and formatRangeToParts, with the charge" + suffix);
        if (only is null || only.Contains("N3"))
            yield return Construction(arms, population, "N3 the number format's construction and resolved options" + suffix);
        if (only is null || only.Contains("S1"))
            yield return Lists(arms, population, random, "S1 list formatting: CreatePartsFromList, with the charge" + suffix);
        if (only is null || only.Contains("S2"))
            yield return RelativeTimes(arms, population, random, "S2 relative time: PartitionRelativeTimePattern, with the charge" + suffix);
        if (only is null || only.Contains("S3"))
            yield return DisplayNames(arms, population, "S3 display names: CanonicalCodeForDisplayNames and of" + suffix);
    }

    /// <summary>
    /// The one row of sub-area (c): a difference the library's answer under Broiler.VM's reading of the data removes, written
    /// into the input, so that the row covers it only when that answer is Broiler.VM's. Any other difference stays unclassified.
    /// </summary>
    internal static IEnumerable<KnownDifference> Known()
    {
        foreach (var section in new[] { "N1", "N2", "N3", "S1", "S2", "S3" })
            yield return new KnownDifference(section, DataRow, static (input, vm, _) => input.EndsWith(ReadingMark + Quote(vm) + ")", StringComparison.Ordinal));
    }

    /// <summary>Compares one input, and on a difference writes the library's answer under Broiler.VM's reading into it.</summary>
    private static void Compare(Section section, string input, Func<string> vm, Func<Reading, string> library)
    {
        var vmAnswer = Call(vm);
        var libraryAnswer = Call(() => library(Reading.Library));
        if (!string.Equals(vmAnswer, libraryAnswer, StringComparison.Ordinal))
            input += ReadingMark + Quote(Call(() => library(Reading.BroilerVm))) + ")";
        section.Compare(input, vmAnswer, libraryAnswer);
    }

    /// <summary>An answer as the input carries it: quoted, every code unit outside printable ASCII escaped.</summary>
    private static string Quote(string text) => LocaleSection.Quote(text);

    /// <summary>How the library's arm reads the data: as the library does, or as Broiler.VM does.</summary>
    private enum Reading
    {
        Library,
        BroilerVm,
    }

    // ---- N1 and N2 ---------------------------------------------------------------------------------------------------------

    private static Section Formatting(Arms arms, NumberTextPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void FormatOne(string locale, OptionsBag bag, DecimalInput value) =>
            Compare(section, "format " + LocaleSection.Quote(locale) + " " + bag.Label + " " + value.Label + (value.Text is null ? string.Empty : " (String)"),
                () =>
                {
                    var formatter = VmNumberFormat(arms, locale, bag);
                    var x = VmValue(value);
                    var before = arms.Engine.Charged;
                    var parts = formatter.Parts(arms.Engine, x);
                    return Render(parts) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    if (LibraryNumberFormat(arms, locale, bag, reading, out var formatter) is { } refused)
                        return refused;
                    ulong charged = 0;
                    var parts = formatter!.FormatToParts(LibraryValue(arms, value), units => charged += units);
                    return Render(parts) + " charge " + charged;
                });

        // EVERY DATASET CASE WITH EVERY DATASET VALUE; then each vector's own call.
        foreach (var (locale, bag) in population.DatasetNumberFormats)
        {
            foreach (var value in population.DatasetValues)
                FormatOne(locale, bag, value);
        }

        foreach (var call in population.VectorCalls.Where(static c => c is { Service: "NumberFormat", Method: "format" or "formatToParts" } && c.Arguments.Count == 1))
        {
            if (DecimalPopulation.Input(call.Arguments[0]) is { } value)
                FormatOne(call.Locale, call.Bag, value);
        }

        // THE RANDOM DRAWS: each format with values, and each value with formats.
        foreach (var (locale, bag) in population.NumberFormats)
        {
            for (var i = 0; i < 6; i++)
                FormatOne(locale, bag, random.Pick(population.Values));
        }

        foreach (var value in population.Values)
        {
            var (locale, bag) = random.Pick(population.NumberFormats);
            FormatOne(locale, bag, value);
        }

        return section;
    }

    private static Section Ranges(Arms arms, NumberTextPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void RangeOne(string locale, OptionsBag bag, DecimalInput start, DecimalInput end) =>
            Compare(section, "range " + LocaleSection.Quote(locale) + " " + bag.Label + " " + start.Label + " .. " + end.Label,
                () =>
                {
                    var formatter = VmNumberFormat(arms, locale, bag);
                    var x = VmValue(start);
                    var y = VmValue(end);

                    // RANGEVALUES, the realm's (not copied): a range cannot start or end at not-a-number.
                    if (x.Kind == JsDecimalKind.NaN || y.Kind == JsDecimalKind.NaN)
                        return "error: RangeError";
                    var before = arms.Engine.Charged;
                    var parts = formatter.RangeParts(arms.Engine, x, y);
                    return Render(parts) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    if (LibraryNumberFormat(arms, locale, bag, reading, out var formatter) is { } refused)
                        return refused;
                    ulong charged = 0;
                    return formatter!.FormatRangeToParts(LibraryValue(arms, start), LibraryValue(arms, end), out var parts, units => charged += units) == IntlStatus.Completed
                        ? Render(parts) + " charge " + charged
                        : "error: RangeError";
                });

        foreach (var (locale, bag) in population.DatasetNumberFormats)
        {
            foreach (var (start, end) in population.DatasetRanges)
                RangeOne(locale, bag, start, end);
        }

        foreach (var call in population.VectorCalls.Where(static c => c is { Service: "NumberFormat", Method: "formatRange" or "formatRangeToParts" } && c.Arguments.Count == 2))
        {
            if (DecimalPopulation.Input(call.Arguments[0]) is { } start && DecimalPopulation.Input(call.Arguments[1]) is { } end)
                RangeOne(call.Locale, call.Bag, start, end);
        }

        foreach (var (start, end) in population.Ranges)
        {
            for (var i = 0; i < 2; i++)
            {
                var (locale, bag) = random.Pick(population.NumberFormats);
                RangeOne(locale, bag, start, end);
            }
        }

        return section;
    }

    private static Section Construction(Arms arms, NumberTextPopulation population, string name)
    {
        var section = new Section(name);

        foreach (var (locale, bag) in population.DatasetNumberFormats.Concat(population.NumberFormats))
        {
            Compare(section, "NumberFormat " + LocaleSection.Quote(locale) + " " + bag.Label,
                () => Render(VmNumberFormat(arms, locale, bag)),
                reading => LibraryNumberFormat(arms, locale, bag, reading, out var formatter) ?? Render(formatter!));
        }

        return section;
    }

    // ---- S1, S2 and S3 -------------------------------------------------------------------------------------------------------

    private static Section Lists(Arms arms, NumberTextPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void ListOne(string locale, OptionsBag bag, List<string> list) =>
            Compare(section, "list " + LocaleSection.Quote(locale) + " " + bag.Label + " [" + string.Join(",", list.Select(LocaleSection.Quote)) + "]",
                () =>
                {
                    var made = (JsListFormatObject)JsRealm.NewListFormat(arms.Engine, JsValue.Undefined, [JsValue.String(locale), DecimalSection.VmBag(arms.Engine, bag)]).AsObjectOrNull()!;
                    var before = arms.Engine.Charged;
                    var parts = JsRealm.VmCreatePartsFromList(arms.Engine, made, list);
                    return made.Locale + " " + string.Join("|", parts.Select(static p => p.Type + ":" + p.Value)) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    var read = new DecimalSection.BagReader(bag);
                    if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
                        return e1;
                    if (Resolve(arms, locale, static (_, _) => [], reading) is not { } resolved)
                        return "error: RangeError";
                    if (read.String("type", ["conjunction", "disjunction", "unit"], "conjunction", out var type) is { } e2)
                        return e2;
                    if (read.String("style", ["long", "short", "narrow"], "long", out var style) is { } e3)
                        return e3;
                    var formatter = ListFormatter.Create(Data(arms, reading), resolved.Locale, DataLocale(resolved, reading),
                        type switch { "disjunction" => ListFormatType.Disjunction, "unit" => ListFormatType.Unit, _ => ListFormatType.Conjunction }, Style(style!));
                    ulong charged = 0;
                    var parts = formatter.FormatToParts(list, units => charged += units);
                    return formatter.Locale + " " + string.Join("|", parts.Select(static p => p.Type + ":" + p.Value)) + " charge " + charged;
                });

        foreach (var (locale, bag) in population.ListFormats)
        {
            foreach (var list in population.Lists)
                ListOne(locale, bag, list);
        }

        return section;
    }

    private static Section RelativeTimes(Arms arms, NumberTextPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void TimeOne(string locale, OptionsBag bag, double value, string unit) =>
            Compare(section, "relative " + LocaleSection.Quote(locale) + " " + bag.Label + " " + DecimalInput.Of(value).Label + " " + LocaleSection.Quote(unit),
                () =>
                {
                    var made = (JsRelativeTimeFormatObject)JsRealm.NewRelativeTimeFormat(arms.Engine, JsValue.Undefined, [JsValue.String(locale), DecimalSection.VmBag(arms.Engine, bag)]).AsObjectOrNull()!;
                    var before = arms.Engine.Charged;
                    var parts = JsRealm.VmPartitionRelativeTimePattern(arms.Engine, made, value, unit);
                    return made.Locale + " " + made.NumberingSystem + " " + string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + ":" + (p.Unit ?? "-"))) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    var read = new DecimalSection.BagReader(bag);
                    if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
                        return e1;
                    if (read.String("numberingSystem", null, null, out var nu) is { } e2)
                        return e2;
                    if (nu is not null && !LocaleId.IsUnicodeType(nu))
                        return "error: RangeError";
                    if (Resolve(arms, locale, (data, match) => [new RelevantKey("nu", NumberingSystems(arms, data, match, reading)) { Option = nu }], reading) is not { } resolved)
                        return "error: RangeError";
                    if (read.String("style", ["long", "short", "narrow"], "long", out var style) is { } e3)
                        return e3;
                    if (read.String("numeric", ["always", "auto"], "always", out var numeric) is { } e4)
                        return e4;
                    var formatter = RelativeTimeFormatter.Create(Data(arms, reading), resolved.Locale, DataLocale(resolved, reading), Style(style!),
                        numeric == "auto" ? RelativeTimeNumeric.Auto : RelativeTimeNumeric.Always, resolved.KeyValues["nu"] ?? "latn");
                    ulong charged = 0;
                    if (formatter.FormatToParts(value, unit, out var parts, units => charged += units) != IntlStatus.Completed)
                        return "error: RangeError";
                    return formatter.Locale + " " + formatter.NumberingSystem + " " + string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + ":" + (p.Unit ?? "-"))) + " charge " + charged;
                });

        // EVERY DATASET FORMAT WITH EVERY DATASET INPUT; then each vector's own call, then the random draws.
        foreach (var (locale, bag) in population.DatasetRelativeTimeFormats)
        {
            foreach (var (value, unit) in population.DatasetRelativeTimes)
                TimeOne(locale, bag, value, unit);
        }

        foreach (var call in population.VectorCalls.Where(static c => c is { Service: "RelativeTimeFormat" } && c.Arguments is [double, string]))
            TimeOne(call.Locale, call.Bag, (double)call.Arguments[0]!, (string)call.Arguments[1]!);

        foreach (var (value, unit) in population.RelativeTimes)
        {
            for (var i = 0; i < 3; i++)
            {
                var (locale, bag) = random.Pick(population.RelativeTimeFormats);
                TimeOne(locale, bag, value, unit);
            }
        }

        return section;
    }

    private static Section DisplayNames(Arms arms, NumberTextPopulation population, string name)
    {
        var section = new Section(name);

        foreach (var (locale, bag) in population.DisplayNameFormats)
        {
            var type = bag["type"] as string;
            var codes = type is not null && population.Codes.TryGetValue(type, out var listed) ? listed : ["US"];
            foreach (var code in codes)
            {
                Compare(section, "of " + LocaleSection.Quote(locale) + " " + bag.Label + " " + LocaleSection.Quote(code),
                    () =>
                    {
                        var made = (JsDisplayNamesObject)JsRealm.NewDisplayNames(arms.Engine, JsValue.Undefined, [JsValue.String(locale), DecimalSection.VmBag(arms.Engine, bag)]).AsObjectOrNull()!;

                        // INTL.DISPLAYNAMES.PROTOTYPE.OF, the realm's (not copied): the canonical code, its name, or the fallback.
                        var canonical = JsRealm.VmCanonicalCodeForDisplayNames(arms.Engine, made.Type, code);
                        var named = JsRealm.VmDisplayName(arms.Engine, made, canonical);
                        return made.Locale + " " + (named ?? (made.Fallback == "code" ? canonical : "<undefined>"));
                    },
                    reading =>
                    {
                        var read = new DecimalSection.BagReader(bag);
                        if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
                            return e1;
                        if (Resolve(arms, locale, static (_, _) => [], reading) is not { } resolved)
                            return "error: RangeError";
                        if (read.String("style", ["narrow", "short", "long"], "long", out var style) is { } e2)
                            return e2;
                        if (read.String("type", ["language", "region", "script", "currency", "calendar", "dateTimeField"], null, out var given) is { } e3)
                            return e3;
                        if (given is null)
                            return "error: TypeError";
                        if (read.String("fallback", ["code", "none"], "code", out var fallback) is { } e4)
                            return e4;
                        if (read.String("languageDisplay", ["dialect", "standard"], "dialect", out var display) is { } e5)
                            return e5;
                        var names = DisplayNamesCore.Create(Data(arms, reading), resolved.Locale, DataLocale(resolved, reading), Style(style!),
                            given switch
                            {
                                "region" => DisplayNamesType.Region,
                                "script" => DisplayNamesType.Script,
                                "currency" => DisplayNamesType.Currency,
                                "calendar" => DisplayNamesType.Calendar,
                                "dateTimeField" => DisplayNamesType.DateTimeField,
                                _ => DisplayNamesType.Language,
                            },
                            fallback == "none" ? DisplayNamesFallback.None : DisplayNamesFallback.Code,
                            display == "standard" ? LanguageDisplay.Standard : LanguageDisplay.Dialect);
                        return names.Of(code, out var result) == IntlStatus.Completed ? names.Locale + " " + (result ?? "<undefined>") : "error: RangeError";
                    });
            }
        }

        return section;
    }

    // ---- Broiler.VM's arm ------------------------------------------------------------------------------------------------

    private static JsNumberFormatter VmNumberFormat(Arms arms, string locale, OptionsBag bag) =>
        ((JsNumberFormatObject)JsRealm.NewNumberFormat(arms.Engine, JsValue.Undefined, [JsValue.String(locale), DecimalSection.VmBag(arms.Engine, bag)], JsValue.Undefined, called: false).AsObjectOrNull()!).Formatter;

    private static JsDecimal VmValue(DecimalInput value) =>
        value.Text is { } text ? JsDecimal.FromString(text, static _ => { })
        : value.BigInt is { } big ? JsDecimal.FromBigInt(new JsBigInt(big), static _ => { })
        : JsDecimal.FromNumber(value.Number);

    private static string Render(List<JsNumberPart> parts) => string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + (p.Source is null ? string.Empty : ":" + p.Source)));

    private static string Render(JsNumberFormatter f) =>
        string.Join(" ", f.Locale, "nu=" + f.NumberingSystem, "style=" + f.Style, "currency=" + (f.Currency ?? "-"), "currencyDisplay=" + (f.CurrencyDisplay ?? "-"),
            "currencySign=" + (f.CurrencySign ?? "-"), "unit=" + (f.Unit ?? "-"), "unitDisplay=" + (f.UnitDisplay ?? "-"), "notation=" + f.Notation,
            "compactDisplay=" + (f.CompactDisplay ?? "-"), "useGrouping=" + (f.UseGrouping ?? "false"), "signDisplay=" + f.SignDisplay,
            "mnid=" + f.MinimumIntegerDigits, "mnfd=" + Int(f.MinimumFractionDigits), "mxfd=" + Int(f.MaximumFractionDigits),
            "mnsd=" + Int(f.MinimumSignificantDigits), "mxsd=" + Int(f.MaximumSignificantDigits), "type=" + f.RoundingType);

    // ---- the library's arm, read as an engine's adapter reads -------------------------------------------------------------

    private static IntlDecimal LibraryValue(Arms arms, DecimalInput value) =>
        value.Text is { } text ? IntlDecimal.FromString(text, arms.Policy)
        : value.BigInt is { } big ? IntlDecimal.FromBigInt(big)
        : IntlDecimal.FromNumber(value.Number);

    private static string Render(IReadOnlyList<NumberPart> parts) => string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + (p.Source is null ? string.Empty : ":" + p.Source)));

    private static string Render(NumberFormatter f) =>
        string.Join(" ", f.Locale, "nu=" + f.NumberingSystem, "style=" + StyleName(f.Style), "currency=" + (f.Currency ?? "-"),
            "currencyDisplay=" + (f.CurrencyDisplay switch { null => "-", CurrencyDisplay.Code => "code", CurrencyDisplay.NarrowSymbol => "narrowSymbol", CurrencyDisplay.Name => "name", _ => "symbol" }),
            "currencySign=" + (f.CurrencySign switch { null => "-", CurrencySign.Accounting => "accounting", _ => "standard" }),
            "unit=" + (f.Unit ?? "-"), "unitDisplay=" + (f.UnitDisplay switch { null => "-", UnitDisplay.Long => "long", UnitDisplay.Narrow => "narrow", _ => "short" }),
            "notation=" + f.Notation switch { NumberNotation.Scientific => "scientific", NumberNotation.Engineering => "engineering", NumberNotation.Compact => "compact", _ => "standard" },
            "compactDisplay=" + (f.CompactDisplay switch { null => "-", CompactDisplay.Long => "long", _ => "short" }),
            "useGrouping=" + f.UseGrouping switch { NumberGrouping.Always => "always", NumberGrouping.Min2 => "min2", NumberGrouping.Off => "false", _ => "auto" },
            "signDisplay=" + f.SignDisplay switch { SignDisplay.Never => "never", SignDisplay.Always => "always", SignDisplay.ExceptZero => "exceptZero", SignDisplay.Negative => "negative", _ => "auto" },
            "mnid=" + f.DigitOptions.MinimumIntegerDigits, "mnfd=" + Int(f.DigitOptions.MinimumFractionDigits), "mxfd=" + Int(f.DigitOptions.MaximumFractionDigits),
            "mnsd=" + Int(f.DigitOptions.MinimumSignificantDigits), "mxsd=" + Int(f.DigitOptions.MaximumSignificantDigits),
            "type=" + f.DigitOptions.RoundingType switch
            {
                NumberRoundingType.SignificantDigits => "significantDigits",
                NumberRoundingType.MorePrecision => "morePrecision",
                NumberRoundingType.LessPrecision => "lessPrecision",
                _ => "fractionDigits",
            });

    private static string StyleName(NumberStyle style) => style switch
    {
        NumberStyle.Percent => "percent",
        NumberStyle.Currency => "currency",
        NumberStyle.Unit => "unit",
        _ => "decimal",
    };

    private static FormatStyle Style(string style) => style switch { "short" => FormatStyle.Short, "narrow" => FormatStyle.Narrow, _ => FormatStyle.Long };

    private static string Int(int? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-";

    /// <summary>The data an arm of the library reads: the composite over the tables, or with no numbering system's own symbols.</summary>
    private static ILocaleData Data(Arms arms, Reading reading) => reading == Reading.Library ? arms.Data : arms.VmReadingData;

    /// <summary>The data locale an arm of the library reads at: ResolveLocale's, or its language, as Broiler.VM reads it.</summary>
    private static string DataLocale(ResolvedLocale resolved, Reading reading) =>
        reading == Reading.Library ? resolved.DataLocale
        : LocaleId.TryParse(resolved.DataLocale, out var id) == IntlStatus.Completed ? id!.Language : "en";

    /// <summary>The <c>nu</c> values: the library's for the matched locale, or Broiler.VM's, <c>latn</c> first.</summary>
    private static IReadOnlyList<string?> NumberingSystems(Arms arms, ILocaleData data, string match, Reading reading) =>
        reading == Reading.Library ? NumberFormatter.NumberingSystemKeyValues(data, match) : arms.VmNumberingSystems;

    private static ResolvedLocale? Resolve(Arms arms, string locale, Func<ILocaleData, string, IReadOnlyList<RelevantKey>> keys, Reading reading)
    {
        if (LocaleNegotiation.CanonicalizeLocaleList([locale], arms.Data, out var requested, out _) != IntlStatus.Completed)
            return null;
        var match = LocaleNegotiation.MatchLocale(arms.Available, requested, DefaultLocale);
        return LocaleNegotiation.ResolveLocale(match, keys(arms.Data, match.Locale), arms.Data);
    }

    /// <summary><c>new Intl.NumberFormat(locale, bag)</c>'s reads in the constructor's order, as the vectors adapter runs them, and the formatter; or the error.</summary>
    private static string? LibraryNumberFormat(Arms arms, string locale, OptionsBag bag, Reading reading, out NumberFormatter? formatter)
    {
        formatter = null;
        var read = new DecimalSection.BagReader(bag);
        if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
            return e1;
        if (read.String("numberingSystem", null, null, out var nu) is { } e2)
            return e2;
        if (nu is not null && !LocaleId.IsUnicodeType(nu))
            return "error: RangeError";
        if (Resolve(arms, locale, (data, match) => [new RelevantKey("nu", NumberingSystems(arms, data, match, reading)) { Option = nu }], reading) is not { } resolved)
            return "error: RangeError";

        if (read.String("style", ["decimal", "percent", "currency", "unit"], "decimal", out var style) is { } e3)
            return e3;
        if (read.String("currency", null, null, out var currency) is { } e4)
            return e4;
        if (currency is null ? style == "currency" : !NumberFormatter.IsWellFormedCurrencyCode(currency))
            return currency is null ? "error: TypeError" : "error: RangeError";
        if (read.String("currencyDisplay", ["code", "symbol", "narrowSymbol", "name"], "symbol", out var currencyDisplay) is { } e5)
            return e5;
        if (read.String("currencySign", ["standard", "accounting"], "standard", out var currencySign) is { } e6)
            return e6;
        if (read.String("unit", null, null, out var unit) is { } e7)
            return e7;
        if (unit is null ? style == "unit" : !NumberFormatter.IsWellFormedUnitIdentifier(unit))
            return unit is null ? "error: TypeError" : "error: RangeError";
        if (read.String("unitDisplay", ["short", "narrow", "long"], "short", out var unitDisplay) is { } e8)
            return e8;
        if (read.String("notation", ["standard", "scientific", "engineering", "compact"], "standard", out var notationName) is { } e9)
            return e9;

        var notation = notationName switch { "scientific" => NumberNotation.Scientific, "engineering" => NumberNotation.Engineering, "compact" => NumberNotation.Compact, _ => NumberNotation.Standard };
        var numberStyle = style switch { "percent" => NumberStyle.Percent, "currency" => NumberStyle.Currency, "unit" => NumberStyle.Unit, _ => NumberStyle.Decimal };
        if (read.Digits(DigitOptionRules.NumberFormatDefaults(numberStyle, currency, notation, arms.Data), notation, out var digits) is { } e10)
            return e10;
        if (read.String("compactDisplay", ["short", "long"], "short", out var compactDisplay) is { } e11)
            return e11;
        if (Grouping(bag, notation == NumberNotation.Compact ? NumberGrouping.Min2 : NumberGrouping.Auto, out var grouping) is { } e12)
            return e12;
        if (read.String("signDisplay", ["auto", "never", "always", "exceptZero", "negative"], "auto", out var signDisplay) is { } e13)
            return e13;

        formatter = NumberFormatter.Create(Data(arms, reading), resolved.Locale, DataLocale(resolved, reading), resolved.KeyValues["nu"] ?? "latn", new NumberFormatOptions
        {
            Style = numberStyle,
            Currency = currency,
            CurrencyDisplay = currencyDisplay switch { "code" => CurrencyDisplay.Code, "narrowSymbol" => CurrencyDisplay.NarrowSymbol, "name" => CurrencyDisplay.Name, _ => CurrencyDisplay.Symbol },
            CurrencySign = currencySign == "accounting" ? CurrencySign.Accounting : CurrencySign.Standard,
            Unit = unit,
            UnitDisplay = unitDisplay switch { "long" => UnitDisplay.Long, "narrow" => UnitDisplay.Narrow, _ => UnitDisplay.Short },
            DigitOptions = digits!,
            Notation = notation,
            CompactDisplay = compactDisplay == "long" ? CompactDisplay.Long : CompactDisplay.Short,
            UseGrouping = grouping,
            SignDisplay = signDisplay switch { "never" => SignDisplay.Never, "always" => SignDisplay.Always, "exceptZero" => SignDisplay.ExceptZero, "negative" => SignDisplay.Negative, _ => SignDisplay.Auto },
        });
        return null;
    }

    /// <summary>
    /// GetBooleanOrStringNumberFormatOption of <c>useGrouping</c> as an engine reads it: absent the fallback, <see langword="true"/>
    /// <c>always</c>, a false value <see langword="false"/>, <c>"true"</c> and <c>"false"</c> the fallback, another String from the list.
    /// </summary>
    private static string? Grouping(OptionsBag bag, NumberGrouping fallback, out NumberGrouping grouping)
    {
        grouping = fallback;
        switch (bag["useGrouping"])
        {
            case null:
                return null;
            case true:
                grouping = NumberGrouping.Always;
                return null;
            case false or "" or 0.0 or double.NaN:
                grouping = NumberGrouping.Off;
                return null;
            case string text:
                switch (text)
                {
                    case "true" or "false":
                        return null;
                    case "min2":
                        grouping = NumberGrouping.Min2;
                        return null;
                    case "auto":
                        grouping = NumberGrouping.Auto;
                        return null;
                    case "always":
                        grouping = NumberGrouping.Always;
                        return null;
                }

                return "error: RangeError";
            default:
                return "error: RangeError";
        }
    }

    // ---- helpers ----------------------------------------------------------------------------------------------------------

    private static string Call(Func<string> call)
    {
        try
        {
            return call();
        }
        catch (VmError error)
        {
            return "error: " + error.Kind;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception: " + exception.GetType().Name;
        }
    }

    /// <summary>One embedding's two arms and the engine parameters they share.</summary>
    private sealed class Arms
    {
        internal Arms(Embedding embedding)
        {
            Engine = new JsEngine(new VmIntl(new JsIntlTables(embedding.Vm), HomeTimeZones.Current));
            Data = new CompositeLocaleData(new TableLocaleData(embedding.Library));
            VmReadingData = new CompositeLocaleData(new WithoutOwnSymbols(new TableLocaleData(embedding.Library)));
            Available = JsRealm.VmAvailableLocales(Engine).AsReadOnly();
            VmNumberingSystems = ["latn", .. Data.NumberingSystems.Where(static s => s != "latn").Order(StringComparer.Ordinal)];
        }

        internal JsEngine Engine { get; }

        /// <summary>The tables in the composite's order, as an engine hands them to the library.</summary>
        internal CompositeLocaleData Data { get; }

        /// <summary>The same, without any numbering system's own symbols, as Broiler.VM's tables carry none (decision D21).</summary>
        internal CompositeLocaleData VmReadingData { get; }

        /// <summary>Broiler.VM's available locales, the embedding's (decision D7), which both arms resolve against.</summary>
        internal IReadOnlyList<string> Available { get; }

        /// <summary>Broiler.VM's <c>nu</c> values: <c>latn</c> first, then the rest by code unit.</summary>
        internal IReadOnlyList<string?> VmNumberingSystems { get; }

        internal BigIntPolicy Policy { get; } = new(JsBigInt.MaximumBits);
    }

    /// <summary>Locale data less every numbering system's own symbols, the <c>symbols@</c> keys Broiler.VM's tables never carry.</summary>
    /// <remarks><i>(Noted 2026-11-02, at stage SJL-9.3e: internal, where it was private, so that sub-area (e)'s section reads the data as Broiler.VM does with the same type; nothing it answers changes.)</i></remarks>
    internal sealed class WithoutOwnSymbols(ILocaleData inner) : ILocaleData
    {
        public IReadOnlyList<string> Locales => inner.Locales;

        public bool HoldsLocale(IntlTable table, string locale) => inner.HoldsLocale(table, locale);

        public string? LikelySubtags(string source) => inner.LikelySubtags(source);

        public string? Alias(LocaleAliasKind kind, string from) => inner.Alias(kind, from);

        public bool IsExtensionKey(char singleton, string key) => inner.IsExtensionKey(singleton, key);

        public bool TryExtensionType(char singleton, string key, string type, out string preferred) => inner.TryExtensionType(singleton, key, type, out preferred);

        public RegionHourCycles? HourCycles(string region) => inner.HourCycles(region);

        public RegionWeek? Week(string region) => inner.Week(region);

        public bool? IsRightToLeft(string script) => inner.IsRightToLeft(script);

        public string? NumberValue(string locale, string key) => key.StartsWith("symbols@", StringComparison.Ordinal) ? null : inner.NumberValue(locale, key);

        public CurrencyNames? Currency(string locale, string code) => inner.Currency(locale, code);

        public IReadOnlyList<string> CurrencyCodes => inner.CurrencyCodes;

        public int? CurrencyDigits(string code) => inner.CurrencyDigits(code);

        public IReadOnlyList<string> NumberingSystems => inner.NumberingSystems;

        public IReadOnlyList<string>? NumberingSystemDigits(string system) => inner.NumberingSystemDigits(system);

        public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) => inner.PluralRules(language, type);

        public string? PluralRange(string language, string start, string end) => inner.PluralRange(language, start, end);

        public string? UnitPattern(string locale, string width, string unit, string field) => inner.UnitPattern(locale, width, unit, field);

        public ListPatterns? ListPattern(string locale, string type) => inner.ListPattern(locale, type);

        public string? RelativeTime(string locale, string field, string key) => inner.RelativeTime(locale, field, key);

        public string? DisplayName(string locale, string kind, string code) => inner.DisplayName(locale, kind, code);

        public string? DateValue(string locale, string calendar, string key) => inner.DateValue(locale, calendar, key);

        public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => inner.DateValues(locale, calendar);

        public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => inner.DayPeriods(language);

        public IReadOnlyList<MetazonePeriod>? MetazonePeriods(string zone) => inner.MetazonePeriods(zone);

        public string? GoldenZone(string metazone, string region) => inner.GoldenZone(metazone, region);

        public string? PrimaryZone(string region) => inner.PrimaryZone(region);
    }
}
