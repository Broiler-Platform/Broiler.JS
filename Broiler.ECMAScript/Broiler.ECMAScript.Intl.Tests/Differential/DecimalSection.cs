using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The differential check's sections for sub-area (b), stage SJL-9.3b (Broiler.VM's JSD-0082, decision D22): Broiler.VM's
/// <c>JsDecimal</c>, <c>SetNumberFormatDigitOptions</c> with the two constructors' reads, <c>FormatNumericToString</c> and its
/// plural rules, pinned in <c>Vm/</c>, against the library's <see cref="IntlDecimal"/>, <see cref="DigitOptionRules"/>,
/// <see cref="NumericFormat"/> and <see cref="PluralRulesRecord"/>, over the population of <see cref="DecimalPopulation"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each arm reads the options as an engine does.</b> Broiler.VM's arm is its own constructor, <c>NewNumberFormat</c> or
/// <c>NewPluralRules</c>, over the options bag as a script object (<c>VmEngine.cs</c>'s stand-ins). The library's arm reads
/// the same bag in the constructor's order, as an engine's adapter does - GetOption's lists, GetNumberOption's ranges, the
/// ToNumber of a value - and hands the library what it read, as the vectors adapter does. So a difference is either the
/// library's, or a read this harness makes otherwise than Broiler.VM, and the report shows which.
/// </para>
/// <para>
/// <b>The datasets and the vectors in full, then the random draws.</b> Before any seeded draw, M2 constructs every dataset
/// format and every vector's own object at its own locale; M3 formats every dataset value with every dataset format and each
/// vector's own value with its own object; P1 selects every dataset value with every <c>plurals</c> format and each vector's
/// own value with its own object; P2 selects every dataset range with every <c>plurals</c> format and each vector's own range
/// with its own object. The random draws then add to those, from the sub-area's own stream, as before.
/// </para>
/// <para>
/// <b>Every compared answer carries its charge</b> where Broiler.VM charges (decision D18): the String's
/// <c>length / 8 + 1</c>, a BigInt's digits, FormatNumericToString's <c>digits / 16 + 1</c>. Broiler.VM's arm counts through its
/// engine's meter, the library's through the <see cref="WorkCharge"/> it is handed.
/// </para>
/// </remarks>
internal static class DecimalSection
{
    private const string DefaultLocale = "en-US";

    private const string NotFinite = "not finite";

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale, IReadOnlyCollection<string>? only)
    {
        var arms = new Arms(embedding);
        var population = DecimalPopulation.Build(embedding, random, scale);
        var suffix = " (" + embedding.Name + ")";

        if (only is null || only.Contains("M1"))
            yield return Values(arms, population, "M1 Intl mathematical value: Number, String, BigInt" + suffix);
        if (only is null || only.Contains("M2"))
            yield return Digits(arms, population, random, "M2 digit options: SetNumberFormatDigitOptions in both constructors" + suffix);
        if (only is null || only.Contains("M3"))
            yield return Formatting(arms, population, random, "M3 FormatNumericToString" + suffix);
        if (only is null || only.Contains("P1"))
            yield return Plurals(arms, population, random, "P1 plural rules: select, categories" + suffix);
        if (only is null || only.Contains("P2"))
            yield return Ranges(arms, population, random, "P2 plural ranges: selectRange" + suffix);
    }

    // ---- M1 --------------------------------------------------------------------------------------------------------------

    private static Section Values(Arms arms, DecimalPopulation population, string name)
    {
        var section = new Section(name);

        foreach (var value in population.Values)
        {
            section.Compare("value " + value.Label + (value.Text is null ? string.Empty : " (String)"),
                VmCall(() => Render(VmValue(arms, value, out var charged)) + " charge " + charged),
                LibraryCall(() => Render(LibraryValue(arms, value, out var charged)) + " charge " + charged));
        }

        return section;
    }

    // ---- M2 --------------------------------------------------------------------------------------------------------------

    private static Section Digits(Arms arms, DecimalPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void NumberFormat(string locale, OptionsBag bag) =>
            section.Compare("NumberFormat " + LocaleSection.Quote(locale) + " " + bag.Label,
                VmCall(() => Render(VmNumberFormat(arms, locale, bag))),
                LibraryCall(() => LibraryNumberFormat(arms, locale, bag, out string? rendered) ?? rendered!));

        void PluralRules(string locale, OptionsBag bag) =>
            section.Compare("PluralRules " + LocaleSection.Quote(locale) + " " + bag.Label,
                VmCall(() => Render(VmPluralRules(arms, locale, bag).Formatter) + " " + VmPluralRules(arms, locale, bag).Type),
                LibraryCall(() => LibraryPluralRules(arms, locale, bag, out var rules) ?? Render(rules!.DigitOptions, NotationName(rules.Notation),
                    rules.Notation == NumberNotation.Compact ? (rules.CompactDisplay == CompactDisplay.Long ? "long" : "short") : null)
                    + " " + (rules.Type == PluralRuleType.Ordinal ? "ordinal" : "cardinal")));

        // EVERY DATASET FORMAT AND EVERY VECTOR'S OWN OBJECT, at its own locale.
        foreach (var (locale, bag) in population.DatasetNumberFormats
            .Concat(population.VectorCalls.Where(static c => c.Service == "NumberFormat").Select(static c => (c.Locale, c.Bag)))
            .DistinctBy(static f => f.Item1 + " " + f.Item2.Label))
            NumberFormat(locale, bag);

        foreach (var (locale, bag) in population.DatasetPluralFormats
            .Concat(population.VectorCalls.Where(static c => c.Service == "PluralRules").Select(static c => (c.Locale, c.Bag)))
            .DistinctBy(static f => f.Item1 + " " + f.Item2.Label))
            PluralRules(locale, bag);

        foreach (var bag in population.NumberFormatBags)
            NumberFormat(random.Next(3) == 0 ? random.Pick(population.Locales) : "en", bag);

        foreach (var bag in population.PluralRulesBags)
            PluralRules(random.Next(3) == 0 ? random.Pick(population.Locales) : "en", bag);

        return section;
    }

    // ---- M3 --------------------------------------------------------------------------------------------------------------

    private static Section Formatting(Arms arms, DecimalPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);
        var finite = population.Values.Where(v => Finite(arms, v)).ToList();
        var formats = new List<(string Label, JsNumberFormatter Vm, DigitOptions Library)>();

        foreach (var bag in population.NumberFormatBags)
        {
            if (Constructed(() => VmNumberFormat(arms, "en", bag)) is { } vm && LibraryNumberFormat(arms, "en", bag, out DigitOptions? digits) is null)
                formats.Add(("NumberFormat " + bag.Label, vm, digits!));
        }

        foreach (var bag in population.PluralRulesBags)
        {
            if (Constructed(() => VmPluralRules(arms, "en", bag)) is { } vm && LibraryPluralRules(arms, "en", bag, out var rules) is null)
                formats.Add(("PluralRules " + bag.Label, vm.Formatter, rules!.DigitOptions));
        }

        void Compare((string Label, JsNumberFormatter Vm, DigitOptions Library) format, DecimalInput value) =>
            section.Compare(format.Label + " " + value.Label + (value.Text is null ? string.Empty : " (String)"),
                VmCall(() =>
                {
                    var x = VmValue(arms, value, out _);
                    var before = arms.Engine.Charged;
                    var (rounded, text) = format.Vm.FormatNumericToString(arms.Engine, x);
                    return Render(rounded) + " " + text + " charge " + (arms.Engine.Charged - before);
                }),
                LibraryCall(() =>
                {
                    var x = LibraryValue(arms, value, out _);
                    ulong charged = 0;
                    var result = NumericFormat.FormatNumericToString(format.Library, x, units => charged += units);
                    return Render(result.RoundedNumber) + " " + result.FormattedString + " charge " + charged;
                }));

        // EVERY DATASET FORMAT, AT ITS OWN LOCALE, WITH EVERY DATASET VALUE; then each vector's own value with its own object.
        (string Label, JsNumberFormatter Vm, DigitOptions Library)? NumberFormatAt(string locale, OptionsBag bag) =>
            Constructed(() => VmNumberFormat(arms, locale, bag)) is { } vm && LibraryNumberFormat(arms, locale, bag, out DigitOptions? digits) is null
                ? ("NumberFormat " + LocaleSection.Quote(locale) + " " + bag.Label, vm, digits!)
                : null;

        (string Label, JsNumberFormatter Vm, DigitOptions Library)? PluralRulesAt(string locale, OptionsBag bag) =>
            Constructed(() => VmPluralRules(arms, locale, bag)) is { } vm && LibraryPluralRules(arms, locale, bag, out var rules) is null
                ? ("PluralRules " + LocaleSection.Quote(locale) + " " + bag.Label, vm.Formatter, rules!.DigitOptions)
                : null;

        var datasetValues = population.DatasetValues.Where(v => Finite(arms, v)).ToList();
        var datasetFormats = population.DatasetNumberFormats.Select(f => NumberFormatAt(f.Locale, f.Bag))
            .Concat(population.DatasetPluralFormats.Select(f => PluralRulesAt(f.Locale, f.Bag)));

        foreach (var format in datasetFormats)
        {
            if (format is null)
                continue;
            foreach (var value in datasetValues)
                Compare(format.Value, value);
        }

        foreach (var call in population.VectorCalls.Where(static c => c.Method is "format" or "formatToParts" or "select"))
        {
            if ((call.Service == "NumberFormat" ? NumberFormatAt(call.Locale, call.Bag) : PluralRulesAt(call.Locale, call.Bag)) is { } format
                && Finite(arms, call.Arguments[0]))
                Compare(format, call.Arguments[0]);
        }

        foreach (var format in formats)
        {
            for (var i = 0; i < 30; i++)
                Compare(format, random.Pick(finite));
        }

        foreach (var value in finite)
        {
            for (var i = 0; i < 3 && formats.Count > 0; i++)
                Compare(random.Pick(formats), value);
        }

        return section;
    }

    // ---- P1 and P2 -------------------------------------------------------------------------------------------------------

    /// <summary>The plural rules objects both arms construct: each locale with no options, ordinal and compact, and some bags; each bag with some locales.</summary>
    private static List<(string Label, JsPluralRulesObject Vm, PluralRulesRecord Library)> PluralFormats(Arms arms, DecimalPopulation population, SeededRandom random)
    {
        var pairs = new List<(string Locale, OptionsBag Bag)>();
        var ordinal = new OptionsBag([new("type", "ordinal")]);
        var compact = new OptionsBag([new("notation", "compact")]);
        var compactLong = new OptionsBag([new("notation", "compact"), new("compactDisplay", "long")]);

        foreach (var locale in population.Locales)
        {
            pairs.Add((locale, OptionsBag.Empty));
            pairs.Add((locale, ordinal));
            pairs.Add((locale, compact));
            pairs.Add((locale, compactLong));
            pairs.Add((locale, random.Pick(population.PluralRulesBags)));
        }

        foreach (var bag in population.PluralRulesBags)
        {
            for (var i = 0; i < 3; i++)
                pairs.Add((i == 0 ? "en" : random.Pick(population.Locales), bag));
        }

        var formats = new List<(string, JsPluralRulesObject, PluralRulesRecord)>();
        foreach (var (locale, bag) in pairs.DistinctBy(static p => p.Locale + " " + p.Bag.Label))
        {
            if (PluralFormat(arms, locale, bag) is { } format)
                formats.Add(format);
        }

        return formats;
    }

    /// <summary>The plural rules object both arms construct for a locale and a bag, or <see langword="null"/> when either refuses it (M2 compares that).</summary>
    private static (string Label, JsPluralRulesObject Vm, PluralRulesRecord Library)? PluralFormat(Arms arms, string locale, OptionsBag bag) =>
        Constructed(() => VmPluralRules(arms, locale, bag)) is { } vm && LibraryPluralRules(arms, locale, bag, out var rules) is null
            ? (LocaleSection.Quote(locale) + " " + bag.Label, vm, rules!)
            : null;

    private static Section Plurals(Arms arms, DecimalPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void Categories(string label, JsPluralRulesObject vm, PluralRulesRecord library) =>
            section.Compare("categories " + label,
                VmCall(() => string.Join(",", JsRealm.VmPluralCategoriesOf(arms.Engine, vm))),
                LibraryCall(() => string.Join(",", library.PluralCategories)));

        void Select(string label, JsPluralRulesObject vm, PluralRulesRecord library, DecimalInput value)
        {
            var input = "select " + label + " " + value.Label + (value.Text is null ? string.Empty : " (String)");
            section.Compare(input + AtExponentZero(arms, library, () => SelectAtZero(arms, library, value)),
                VmCall(() =>
                {
                    var (category, formatted) = JsRealm.VmResolvePlural(arms.Engine, vm, VmValue(arms, value, out _));
                    return category + " | " + formatted;
                }),
                LibraryCall(() =>
                {
                    var resolved = library.ResolvePlural(LibraryValue(arms, value, out _));
                    return resolved.PluralCategory + " | " + resolved.FormattedString;
                }));
        }

        // EVERY PLURALS FORMAT, AT ITS OWN LOCALE, WITH EVERY DATASET VALUE; then each vector's own value with its own object.
        foreach (var (locale, bag) in population.DatasetPluralFormats)
        {
            if (PluralFormat(arms, locale, bag) is not (var label, var vm, var library))
                continue;
            Categories(label, vm, library);
            foreach (var value in population.DatasetValues)
                Select(label, vm, library, value);
        }

        foreach (var call in population.VectorCalls.Where(static c => c is { Service: "PluralRules", Method: "select" }))
        {
            if (PluralFormat(arms, call.Locale, call.Bag) is (var label, var vm, var library))
                Select(label, vm, library, call.Arguments[0]);
        }

        foreach (var (label, vm, library) in PluralFormats(arms, population, random))
        {
            Categories(label, vm, library);

            for (var i = 0; i < 20; i++)
                Select(label, vm, library, random.Pick(population.Values));
        }

        return section;
    }

    private static Section Ranges(Arms arms, DecimalPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void Range(string label, JsPluralRulesObject vm, PluralRulesRecord library, DecimalInput start, DecimalInput end)
        {
            var input = "selectRange " + label + " " + start.Label + " " + end.Label;
            section.Compare(input + AtExponentZero(arms, library, () => RangeAtZero(arms, library, start, end)),
                VmCall(() => JsRealm.VmResolvePluralRange(arms.Engine, vm, VmValue(arms, start, out _), VmValue(arms, end, out _))),
                LibraryCall(() => library.ResolvePluralRange(LibraryValue(arms, start, out _), LibraryValue(arms, end, out _), out var category) == IntlStatus.Completed
                    ? category
                    : "error: RangeError"));
        }

        // EVERY PLURALS FORMAT, AT ITS OWN LOCALE, WITH EVERY DATASET RANGE; then each vector's own range with its own object.
        foreach (var (locale, bag) in population.DatasetPluralFormats)
        {
            if (PluralFormat(arms, locale, bag) is not (var label, var vm, var library))
                continue;
            foreach (var (start, end) in population.DatasetRanges)
                Range(label, vm, library, start, end);
        }

        foreach (var call in population.VectorCalls.Where(static c => c is { Service: "PluralRules", Method: "selectRange" }))
        {
            if (PluralFormat(arms, call.Locale, call.Bag) is (var label, var vm, var library))
                Range(label, vm, library, call.Arguments[0], call.Arguments[1]);
        }

        var formats = PluralFormats(arms, population, random);

        foreach (var (start, end) in population.Ranges)
        {
            for (var i = 0; i < 4 && formats.Count > 0; i++)
            {
                var (label, vm, library) = random.Pick(formats);
                Range(label, vm, library, start, end);
            }
        }

        return section;
    }

    /// <summary>
    /// For a compact plural rules object, what the library would answer with the operands <c>c</c> and <c>e</c> at 0, as
    /// Broiler.VM passes them (row P4), written into the input so that row P4 can tell a difference decision D20 makes from
    /// another; nothing for any other notation.
    /// </summary>
    private static string AtExponentZero(Arms arms, PluralRulesRecord library, Func<string> atZero) =>
        library.Notation == NumberNotation.Compact ? " (c=e=0: " + LibraryCall(atZero) + ")" : string.Empty;

    private static string SelectAtZero(Arms arms, PluralRulesRecord library, DecimalInput value)
    {
        var x = LibraryValue(arms, value, out _);
        if (!x.IsFinite)
            return "other";
        return library.Engine.Select(NumericFormat.FormatNumericToString(library.DigitOptions, x).FormattedString, 0);
    }

    private static string RangeAtZero(Arms arms, PluralRulesRecord library, DecimalInput start, DecimalInput end)
    {
        var x = LibraryValue(arms, start, out _);
        var y = LibraryValue(arms, end, out _);
        if (x.Kind == IntlDecimalKind.NaN || y.Kind == IntlDecimalKind.NaN)
            return "error: RangeError";

        string Formatted(IntlDecimal v) => v.IsFinite ? NumericFormat.FormatNumericToString(library.DigitOptions, v).FormattedString : v.ToString();
        string Category(IntlDecimal v) => v.IsFinite ? library.Engine.Select(Formatted(v), 0) : "other";
        return Formatted(x) == Formatted(y) ? Category(x) : library.Engine.SelectRange(Category(x), Category(y));
    }

    // ---- the correspondence's rows ---------------------------------------------------------------------------------------

    /// <summary>
    /// The differences of sub-area (b) that Broiler.VM's correspondence classes: row P4, the compact exponent Broiler.VM's
    /// PluralRules passes as 0 and the library as ComputeExponent's, decision D20's latent correction, which reaches an answer
    /// only where a language's rules read <c>c</c> or <c>e</c> - none of Broiler.VM's <c>de</c> and <c>en</c>. A difference is
    /// P4's only when the library's own answer with the operands at 0 is Broiler.VM's, both arms wrote the same String, and the
    /// rules object is compact; any other stays unclassified.
    /// </summary>
    internal static IEnumerable<KnownDifference> Known()
    {
        yield return new KnownDifference("P1", "P4", static (input, vm, library) =>
            input.Contains("notation:\"compact\"", StringComparison.Ordinal)
            && vm.Split(" | ") is [var vmCategory, var vmText] && library.Split(" | ") is [_, var libraryText]
            && vmText == libraryText
            && input.EndsWith(" (c=e=0: " + vmCategory + ")", StringComparison.Ordinal));

        yield return new KnownDifference("P2", "P4", static (input, vm, library) =>
            input.Contains("notation:\"compact\"", StringComparison.Ordinal)
            && !vm.StartsWith("error", StringComparison.Ordinal) && !library.StartsWith("error", StringComparison.Ordinal)
            && input.EndsWith(" (c=e=0: " + vm + ")", StringComparison.Ordinal));
    }

    // ---- Broiler.VM's arm ------------------------------------------------------------------------------------------------

    private static JsDecimal VmValue(Arms arms, DecimalInput value, out ulong charged)
    {
        ulong units = 0;
        WorkCharge charge = u => units += u;
        var result = value.Text is { } text ? JsDecimal.FromString(text, charge)
            : value.BigInt is { } big ? JsDecimal.FromBigInt(new JsBigInt(big), charge)
            : JsDecimal.FromNumber(value.Number);
        charged = units;
        return result;
    }

    private static JsNumberFormatter VmNumberFormat(Arms arms, string locale, OptionsBag bag)
    {
        var made = JsRealm.NewNumberFormat(arms.Engine, JsValue.Undefined, [JsValue.String(locale), VmBag(arms.Engine, bag)], JsValue.Undefined, called: false);
        return ((JsNumberFormatObject)made.AsObjectOrNull()!).Formatter;
    }

    private static JsPluralRulesObject VmPluralRules(Arms arms, string locale, OptionsBag bag) =>
        (JsPluralRulesObject)JsRealm.NewPluralRules(arms.Engine, JsValue.Undefined, [JsValue.String(locale), VmBag(arms.Engine, bag)]).AsObjectOrNull()!;

    internal static JsValue VmBag(JsEngine engine, OptionsBag bag)
    {
        var o = new JsObject(engine.Realm.ObjectPrototype);
        foreach (var (key, value) in bag.Entries)
        {
            o.DefineOrdinary(key, value switch
            {
                string s => JsValue.String(s),
                bool b => JsValue.Boolean(b),
                double d => JsValue.Number(d),
                _ => throw new NotSupportedException("no bag holds a " + value.GetType().Name),
            });
        }

        return JsValue.Object(o);
    }

    private static string Render(JsDecimal x) =>
        x.Kind switch
        {
            JsDecimalKind.Finite => (x.Negative ? "-" : "+") + "0." + x.Digits + "e" + x.Point.ToString(CultureInfo.InvariantCulture),
            JsDecimalKind.NaN => "NaN",
            JsDecimalKind.PositiveInfinity => "+Infinity",
            _ => "-Infinity",
        };

    private static string Render(JsNumberFormatter f) =>
        string.Join(" ", "mnid=" + f.MinimumIntegerDigits, "mnfd=" + Int(f.MinimumFractionDigits), "mxfd=" + Int(f.MaximumFractionDigits),
            "mnsd=" + Int(f.MinimumSignificantDigits), "mxsd=" + Int(f.MaximumSignificantDigits), "type=" + f.RoundingType,
            "priority=" + f.ComputedRoundingPriority, "increment=" + f.RoundingIncrement, "mode=" + f.RoundingMode, "tzd=" + f.TrailingZeroDisplay,
            "notation=" + f.Notation, "compactDisplay=" + (f.CompactDisplay ?? "-"));

    // ---- the library's arm, read as an engine's adapter reads -------------------------------------------------------------

    private static IntlDecimal LibraryValue(Arms arms, DecimalInput value, out ulong charged)
    {
        ulong units = 0;
        WorkCharge charge = u => units += u;
        var result = value.Text is { } text ? IntlDecimal.FromString(text, arms.Policy, charge)
            : value.BigInt is { } big ? IntlDecimal.FromBigInt(big, charge)
            : IntlDecimal.FromNumber(value.Number);
        charged = units;
        return result;
    }

    private static string Render(IntlDecimal x) =>
        x.Kind switch
        {
            IntlDecimalKind.Finite => (x.IsNegative ? "-" : "+") + "0." + x.Digits + "e" + x.Point.ToString(CultureInfo.InvariantCulture),
            IntlDecimalKind.NaN => "NaN",
            IntlDecimalKind.PositiveInfinity => "+Infinity",
            _ => "-Infinity",
        };

    private static string Render(DigitOptions d) =>
        string.Join(" ", "mnid=" + d.MinimumIntegerDigits, "mnfd=" + Int(d.MinimumFractionDigits), "mxfd=" + Int(d.MaximumFractionDigits),
            "mnsd=" + Int(d.MinimumSignificantDigits), "mxsd=" + Int(d.MaximumSignificantDigits), "type=" + d.RoundingType switch
            {
                NumberRoundingType.SignificantDigits => "significantDigits",
                NumberRoundingType.MorePrecision => "morePrecision",
                NumberRoundingType.LessPrecision => "lessPrecision",
                _ => "fractionDigits",
            },
            "priority=" + d.ComputedRoundingPriority switch { RoundingPriority.MorePrecision => "morePrecision", RoundingPriority.LessPrecision => "lessPrecision", _ => "auto" },
            "increment=" + d.RoundingIncrement, "mode=" + ModeName(d.RoundingMode),
            "tzd=" + (d.TrailingZeroDisplay == TrailingZeroDisplay.StripIfInteger ? "stripIfInteger" : "auto"));

    /// <summary>The library's NumberFormat record as Broiler.VM's formatter's properties are rendered, with its notation.</summary>
    private static string Render(DigitOptions d, string notation, string? compactDisplay) =>
        Render(d) + " notation=" + notation + " compactDisplay=" + (compactDisplay ?? "-");

    private static string ModeName(RoundingMode mode) => mode switch
    {
        RoundingMode.Ceil => "ceil",
        RoundingMode.Floor => "floor",
        RoundingMode.Expand => "expand",
        RoundingMode.Trunc => "trunc",
        RoundingMode.HalfCeil => "halfCeil",
        RoundingMode.HalfFloor => "halfFloor",
        RoundingMode.HalfTrunc => "halfTrunc",
        RoundingMode.HalfEven => "halfEven",
        _ => "halfExpand",
    };

    private static string Int(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static ResolvedLocale? LibraryResolve(Arms arms, string locale, IReadOnlyList<RelevantKey> keys)
    {
        if (LocaleNegotiation.CanonicalizeLocaleList([locale], arms.Data, out var requested, out _) != IntlStatus.Completed)
            return null;
        return LocaleNegotiation.ResolveLocale(LocaleNegotiation.MatchLocale(arms.Available, requested, DefaultLocale), keys, arms.Data);
    }

    /// <summary><c>new Intl.NumberFormat(locale, bag)</c>'s reads, as the vectors adapter runs them, and the digit options; or the error.</summary>
    private static string? LibraryNumberFormat(Arms arms, string locale, OptionsBag bag, out string? rendered)
    {
        rendered = null;
        var read = new BagReader(bag);
        if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
            return e1;
        if (read.String("numberingSystem", null, null, out var nu) is { } e2)
            return e2;
        if (nu is not null && !LocaleId.IsUnicodeType(nu))
            return "error: RangeError";
        if (LibraryResolve(arms, locale, [new RelevantKey("nu", arms.NumberingSystems) { Option = nu }]) is null)
            return "error: RangeError";

        if (read.String("style", ["decimal", "percent", "currency", "unit"], "decimal", out var style) is { } e3)
            return e3;
        if (read.String("currency", null, null, out var currency) is { } e4)
            return e4;
        if (currency is null ? style == "currency" : !(currency.Length == 3 && currency.All(char.IsAsciiLetter)))
            return currency is null ? "error: TypeError" : "error: RangeError";
        if (read.String("currencyDisplay", ["code", "symbol", "narrowSymbol", "name"], "symbol", out _) is { } e5)
            return e5;
        if (read.String("currencySign", ["standard", "accounting"], "standard", out _) is { } e6)
            return e6;
        if (read.String("unit", null, null, out var unit) is { } e7)
            return e7;
        if (unit is null && style == "unit")
            return "error: TypeError";
        if (unit is not null && !IsWellFormedUnit(unit))
            return "error: RangeError";
        if (read.String("unitDisplay", ["short", "narrow", "long"], "short", out _) is { } e8)
            return e8;
        if (read.String("notation", ["standard", "scientific", "engineering", "compact"], "standard", out var notationName) is { } e9)
            return e9;

        var notation = Notation(notationName!);
        var numberStyle = style switch { "percent" => NumberStyle.Percent, "currency" => NumberStyle.Currency, "unit" => NumberStyle.Unit, _ => NumberStyle.Decimal };
        if (read.Digits(DigitOptionRules.NumberFormatDefaults(numberStyle, currency, notation, arms.Data), notation, out var digits) is { } e10)
            return e10;
        if (read.String("compactDisplay", ["short", "long"], "short", out var compactDisplay) is { } e11)
            return e11;
        if (read.Grouping() is { } e12)
            return e12;
        if (read.String("signDisplay", ["auto", "never", "always", "exceptZero", "negative"], "auto", out _) is { } e13)
            return e13;

        rendered = Render(digits!, notationName!, notation == NumberNotation.Compact ? compactDisplay : null);
        return null;
    }

    /// <summary>The digit options of <see cref="LibraryNumberFormat"/>, for FormatNumericToString.</summary>
    private static string? LibraryNumberFormat(Arms arms, string locale, OptionsBag bag, out DigitOptions? digits)
    {
        digits = null;
        if (LibraryNumberFormat(arms, locale, bag, out string? _) is { } refused)
            return refused;

        var read = new BagReader(bag);
        _ = read.String("style", null, "decimal", out var style);
        _ = read.String("currency", null, null, out var currency);
        _ = read.String("notation", null, "standard", out var notationName);
        var notation = Notation(notationName!);
        var numberStyle = style switch { "percent" => NumberStyle.Percent, "currency" => NumberStyle.Currency, "unit" => NumberStyle.Unit, _ => NumberStyle.Decimal };
        return read.Digits(DigitOptionRules.NumberFormatDefaults(numberStyle, currency, notation, arms.Data), notation, out digits);
    }

    /// <summary><c>new Intl.PluralRules(locale, bag)</c>'s reads, as the vectors adapter runs them, and the record; or the error.</summary>
    private static string? LibraryPluralRules(Arms arms, string locale, OptionsBag bag, out PluralRulesRecord? rules)
    {
        rules = null;
        var read = new BagReader(bag);
        if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
            return e1;
        if (LibraryResolve(arms, locale, []) is not { } resolved)
            return "error: RangeError";
        if (read.String("type", ["cardinal", "ordinal"], "cardinal", out var type) is { } e2)
            return e2;
        if (read.String("notation", ["standard", "scientific", "engineering", "compact"], "standard", out var notationName) is { } e3)
            return e3;
        if (read.String("compactDisplay", ["short", "long"], "short", out var compactDisplay) is { } e4)
            return e4;

        var notation = Notation(notationName!);
        if (read.Digits(new FractionDigitDefaults(0, 3), notation, out var digits) is { } e5)
            return e5;

        rules = PluralRulesRecord.Create(arms.Data, resolved.Locale, resolved.DataLocale, type == "ordinal" ? PluralRuleType.Ordinal : PluralRuleType.Cardinal,
            notation, compactDisplay == "long" ? CompactDisplay.Long : CompactDisplay.Short, digits!);
        return null;
    }

    private static string NotationName(NumberNotation notation) => notation switch
    {
        NumberNotation.Scientific => "scientific",
        NumberNotation.Engineering => "engineering",
        NumberNotation.Compact => "compact",
        _ => "standard",
    };

    private static NumberNotation Notation(string name) => name switch
    {
        "scientific" => NumberNotation.Scientific,
        "engineering" => NumberNotation.Engineering,
        "compact" => NumberNotation.Compact,
        _ => NumberNotation.Standard,
    };

    /// <summary>ECMA-402's IsWellFormedUnitIdentifier over its sanctioned units, as an engine checks the option.</summary>
    private static bool IsWellFormedUnit(string unit)
    {
        var sanctioned = SupportedValues.SanctionedUnits;
        if (sanctioned.Contains(unit))
            return true;
        var per = unit.IndexOf("-per-", StringComparison.Ordinal);
        return per > 0 && sanctioned.Contains(unit[..per]) && sanctioned.Contains(unit[(per + 5)..]);
    }

    // ---- helpers ----------------------------------------------------------------------------------------------------------

    private static bool Finite(Arms arms, DecimalInput value) => LibraryCall(() => LibraryValue(arms, value, out _).IsFinite ? "y" : "n") == "y";

    private static T? Constructed<T>(Func<T> make) where T : class
    {
        try
        {
            return make();
        }
        catch (VmError)
        {
            return null;
        }
    }

    private static string VmCall(Func<string> call)
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

    private static string LibraryCall(Func<string> call)
    {
        try
        {
            return call();
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
            Tables = new JsIntlTables(embedding.Vm);
            Engine = new JsEngine(new VmIntl(Tables, HomeTimeZones.Current));
            Data = new TableLocaleData(embedding.Library);
            Available = JsRealm.VmAvailableLocales(Engine).AsReadOnly();
            NumberingSystems = ["latn", .. Data.NumberingSystems.Where(static s => s != "latn").Order(StringComparer.Ordinal)];
        }

        internal JsIntlTables Tables { get; }

        internal JsEngine Engine { get; }

        internal TableLocaleData Data { get; }

        /// <summary>Broiler.VM's available locales, the embedding's (decision D7), which both arms resolve against.</summary>
        internal IReadOnlyList<string> Available { get; }

        /// <summary>Broiler.VM's NumberFormat <c>nu</c> values: <c>latn</c> first, then the rest by code unit.</summary>
        internal IReadOnlyList<string?> NumberingSystems { get; }

        /// <summary>Broiler.VM's BigInt policy, which its String reader reads a prefixed literal under.</summary>
        internal BigIntPolicy Policy { get; } = new(JsBigInt.MaximumBits);
    }

    /// <summary>An options bag read as an engine reads one: GetOption, GetNumberOption and ToNumber, each at its step.</summary>
    internal sealed class BagReader(OptionsBag bag)
    {
        private static readonly string[] Modes = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"];

        internal string? String(string key, string[]? values, string? fallback, out string? value)
        {
            var given = bag[key];
            value = given is null ? fallback : ToStringValue(given);
            return value is not null && values is not null && Array.IndexOf(values, value) < 0 ? "error: RangeError" : null;
        }

        internal string? Grouping()
        {
            var given = bag["useGrouping"];
            if (given is null || given is true || (given is bool b && !b))
                return null;
            var text = ToStringValue(given);
            if (text.Length == 0 || text is "true" or "false")
                return null;
            return Array.IndexOf(["min2", "auto", "always"], text) < 0 ? "error: RangeError" : null;
        }

        internal string? Digits(FractionDigitDefaults defaults, NumberNotation notation, out DigitOptions? digits)
        {
            digits = null;
            if (Number("minimumIntegerDigits", 1, 21, 1, out var mnid) is { } e1)
                return e1;
            var mnfd = bag["minimumFractionDigits"];
            var mxfd = bag["maximumFractionDigits"];
            var mnsd = bag["minimumSignificantDigits"];
            var mxsd = bag["maximumSignificantDigits"];
            if (Number("roundingIncrement", 1, 5000, 1, out var increment) is { } e2)
                return e2;
            if (!DigitOptionRules.IsAdmittedRoundingIncrement(increment!.Value))
                return "error: RangeError";
            if (String("roundingMode", Modes, "halfExpand", out var modeName) is { } e3)
                return e3;
            if (String("roundingPriority", ["auto", "morePrecision", "lessPrecision"], "auto", out var priorityName) is { } e4)
                return e4;
            if (String("trailingZeroDisplay", ["auto", "stripIfInteger"], "auto", out var trailing) is { } e5)
                return e5;

            _ = ExactRounding.TryParseRoundingMode(modeName, out var mode);
            var priority = priorityName switch { "morePrecision" => RoundingPriority.MorePrecision, "lessPrecision" => RoundingPriority.LessPrecision, _ => RoundingPriority.Auto };
            var hasSd = mnsd is not null || mxsd is not null;
            var hasFd = mnfd is not null || mxfd is not null;
            var plan = DigitOptionRules.Plan(hasSd, hasFd, priority, notation);
            int? minSd = null, maxSd = null, minFd = null, maxFd = null;

            if (plan.NeedSignificantDigits && hasSd)
            {
                if (Default(mnsd, 1, 21, 1, out minSd) is { } e6)
                    return e6;
                if (Default(mxsd, minSd!.Value, 21, 21, out maxSd) is { } e7)
                    return e7;
            }

            if (plan.NeedFractionDigits && hasFd)
            {
                if (Default(mnfd, 0, 100, null, out minFd) is { } e8)
                    return e8;
                if (Default(mxfd, 0, 100, null, out maxFd) is { } e9)
                    return e9;
            }

            var values = new DigitOptionValues(mnid!.Value, hasSd, minSd, maxSd, hasFd, minFd, maxFd, increment.Value, mode, priority,
                trailing == "stripIfInteger" ? TrailingZeroDisplay.StripIfInteger : TrailingZeroDisplay.Auto);
            return DigitOptionRules.SetNumberFormatDigitOptions(values, defaults, notation, out digits) switch
            {
                IntlStatus.Completed => null,
                IntlStatus.RoundingIncrementNeedsFractionDigits => "error: TypeError",
                _ => "error: RangeError",
            };
        }

        private string? Number(string key, int minimum, int maximum, int? fallback, out int? value) => Default(bag[key], minimum, maximum, fallback, out value);

        private static string? Default(object? given, int minimum, int maximum, int? fallback, out int? value)
        {
            if (given is null)
            {
                value = fallback;
                return null;
            }

            var status = DigitOptionRules.DefaultNumberOption(ToNumber(given), minimum, maximum, out var converted);
            value = converted;
            return status == IntlStatus.Completed ? null : "error: RangeError";
        }

        private static double ToNumber(object value) => value switch
        {
            double d => d,
            string s => NumberParsing.StringToNumber(s, null),
            bool b => b ? 1 : 0,
            _ => double.NaN,
        };

        /// <summary>ToString as Broiler.VM's engine stand-in wrote a Number at stage SJL-9.3b: its round-trip form.</summary>
        /// <remarks><i>(Noted 2026-11-01, at stage SJL-9.3c, when the stand-in came to write a Number by Number::toString, as
        /// Broiler.VM's engine does: no option this reader converts admits a Number's String, so either form gives the same
        /// refusal, and sub-area (b)'s answers do not move.)</i></remarks>
        private static string ToStringValue(object value) => value switch
        {
            string s => s,
            bool b => b ? "true" : "false",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            _ => string.Empty,
        };
    }
}
