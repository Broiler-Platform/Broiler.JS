using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The differential check's sections for sub-area (e), stage SJL-9.3e (Broiler.VM's JSD-0082, decision D22): Broiler.VM's realm
/// <c>Intl.DurationFormat</c>, pinned in <c>Vm/JsRealm.DurationFormat.cs</c> over the earlier stages' NumberFormat and ListFormat
/// copies, against the library's <see cref="DurationFormatCore"/>, over the population of <see cref="DurationPopulation"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each arm reads the options as an engine does.</b> Broiler.VM's arm is its own constructor over the options bag as a script
/// object; the library's reads the same bag in the constructor's order, as an engine's adapter does, and hands the library what it
/// read: ResolveLocale over the same providers, [[DigitalFormat]], each unit's GetDurationUnitOptions and <c>fractionalDigits</c>.
/// The realm's prototype methods and the reads of the argument are not copied: both arms are handed the same valid record, as
/// ToTemporalDuration makes it.
/// </para>
/// <para>
/// <b>Every format carries its charge</b> (decision D18, row U19): Broiler.VM's meter over PartitionDurationFormatPattern - one per unit
/// visited, and the charges of the number and list formats it constructs - against the units the library charges its
/// <see cref="WorkCharge"/>. The per-part charge of <c>formatToParts</c> is the engine's and is in neither arm.
/// </para>
/// <para>
/// <b>Broiler.VM's reading.</b> Where the two arms differ, the library is asked again as Broiler.VM reads the data: at the data
/// locale's language, with Broiler.VM's <c>nu</c> values, <c>latn</c> first, and without the numbering systems' own symbols. A
/// difference that reading removes is the data policy of the correspondence's rows U13 to U16, Broiler.JS's embedding read as the
/// library reads it (decisions D5, D13, D21 and D34); in Broiler.VM's embedding the reading is the library's own, and no difference
/// can be classed so.
/// </para>
/// </remarks>
internal static class DurationSection
{
    private const string DefaultLocale = "en-US";

    /// <summary>The row label of a difference Broiler.VM's reading of the data removes.</summary>
    internal const string DataRow = "C5 data (D5, D13, D21, D34)";

    private const string ReadingMark = " (Broiler.VM's reading: ";

    private static readonly string[] UnitNames = ["years", "months", "weeks", "days", "hours", "minutes", "seconds", "milliseconds", "microseconds", "nanoseconds"];

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale, IReadOnlyCollection<string>? only)
    {
        var arms = new Arms(embedding);
        var population = DurationPopulation.Build(embedding, random, scale);
        var suffix = " (" + embedding.Name + ")";

        if (only is null || only.Contains("U1"))
            yield return Construction(arms, population, "U1 the duration format's construction: GetDurationUnitOptions and the digital format" + suffix);
        if (only is null || only.Contains("U2"))
            yield return Formatting(arms, population, random, "U2 duration formatting: PartitionDurationFormatPattern, with the charge" + suffix);
    }

    /// <summary>
    /// The one row of sub-area (e): a difference the library's answer under Broiler.VM's reading of the data removes, written into
    /// the input, so that the row covers it only when that answer is Broiler.VM's.
    /// </summary>
    internal static IEnumerable<KnownDifference> Known()
    {
        foreach (var section in new[] { "U1", "U2" })
            yield return new KnownDifference(section, DataRow, static (input, vm, _) => input.EndsWith(ReadingMark + LocaleSection.Quote(vm) + ")", StringComparison.Ordinal));
    }

    private static void Compare(Section section, string input, Func<string> vm, Func<Reading, string> library)
    {
        var vmAnswer = Call(vm);
        var libraryAnswer = Call(() => library(Reading.Library));
        if (!string.Equals(vmAnswer, libraryAnswer, StringComparison.Ordinal))
            input += ReadingMark + LocaleSection.Quote(Call(() => library(Reading.BroilerVm))) + ")";
        section.Compare(input, vmAnswer, libraryAnswer);
    }

    private enum Reading
    {
        Library,
        BroilerVm,
    }

    // ---- U1 --------------------------------------------------------------------------------------------------------------

    private static Section Construction(Arms arms, DurationPopulation population, string name)
    {
        var section = new Section(name);
        foreach (var (locale, bag) in population.Formats)
        {
            Compare(section, "construct " + LocaleSection.Quote(locale) + " " + bag.Label,
                () => Render(VmFormat(arms, locale, bag)),
                reading => LibraryFormat(arms, locale, bag, reading, out var format) ?? Render(format!));
        }

        return section;
    }

    // ---- U2 --------------------------------------------------------------------------------------------------------------

    private static Section Formatting(Arms arms, DurationPopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void FormatOne(string locale, OptionsBag bag, DurationFields duration) =>
            Compare(section, "format " + LocaleSection.Quote(locale) + " " + bag.Label + " " + Label(duration),
                () =>
                {
                    var format = VmFormat(arms, locale, bag);
                    var before = arms.Engine.Charged;
                    var parts = JsRealm.VmPartitionDurationFormatPattern(arms.Engine, format, Record(duration));
                    return string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + ":" + (p.Unit ?? "-"))) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    if (LibraryFormat(arms, locale, bag, reading, out var format) is { } refused)
                        return refused;
                    ulong charged = 0;
                    var parts = format!.FormatToParts(duration, units => charged += units);
                    return string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + ":" + (p.Unit ?? "-"))) + " charge " + charged;
                });

        // EVERY DATASET FORMAT WITH EVERY DATASET DURATION; then each vector's own calls; then the random draws.
        foreach (var (locale, bag) in population.DatasetFormats)
        {
            foreach (var duration in population.DatasetDurations)
                FormatOne(locale, bag, duration);
        }

        foreach (var call in population.VectorCalls)
        {
            foreach (var duration in call.Durations)
                FormatOne(call.Locale, call.Bag, duration);
        }

        foreach (var (locale, bag) in population.RandomFormats)
        {
            for (var i = 0; i < 6; i++)
                FormatOne(locale, bag, random.Pick(population.RandomDurations));
        }

        foreach (var duration in population.RandomDurations)
        {
            for (var i = 0; i < 2; i++)
            {
                var (locale, bag) = random.Pick(population.Formats);
                FormatOne(locale, bag, duration);
            }
        }

        return section;
    }

    // ---- Broiler.VM's arm ------------------------------------------------------------------------------------------------

    private static JsDurationFormatObject VmFormat(Arms arms, string locale, OptionsBag bag) =>
        (JsDurationFormatObject)JsRealm.NewDurationFormat(arms.Engine, JsValue.Undefined, [JsValue.String(locale), DecimalSection.VmBag(arms.Engine, bag)]).AsObjectOrNull()!;

    private static double[] Record(DurationFields d) =>
        [d.Years, d.Months, d.Weeks, d.Days, d.Hours, d.Minutes, d.Seconds, d.Milliseconds, d.Microseconds, d.Nanoseconds];

    private static string Render(JsDurationFormatObject f) =>
        string.Join(" ", f.Locale, "nu=" + f.NumberingSystem, "style=" + f.Style, string.Join(",", f.Units.Select(static u => u.Style + "/" + u.Display)),
            "fd=" + (f.FractionalDigits?.ToString(CultureInfo.InvariantCulture) ?? "-"), "sep=" + f.HourMinuteSeparator + f.MinuteSecondSeparator);

    // ---- the library's arm, read as an engine's adapter reads -------------------------------------------------------------

    private static string Render(DurationFormatCore f) =>
        string.Join(" ", f.Locale, "nu=" + f.NumberingSystem, "style=" + f.Style switch
        {
            DurationFormatStyle.Long => "long",
            DurationFormatStyle.Narrow => "narrow",
            DurationFormatStyle.Digital => "digital",
            _ => "short",
        }, string.Join(",", f.Units.Select(static u => StyleName(u.Style) + "/" + (u.Display == DurationUnitDisplay.Always ? "always" : "auto"))),
            "fd=" + (f.FractionalDigits?.ToString(CultureInfo.InvariantCulture) ?? "-"), "sep=" + f.HourMinuteSeparator + f.MinuteSecondSeparator);

    private static string StyleName(DurationUnitStyle style) => style switch
    {
        DurationUnitStyle.Long => "long",
        DurationUnitStyle.Short => "short",
        DurationUnitStyle.Narrow => "narrow",
        DurationUnitStyle.Numeric => "numeric",
        DurationUnitStyle.TwoDigit => "2-digit",
        _ => "fractional",
    };

    /// <summary><c>new Intl.DurationFormat(locale, bag)</c>'s reads in the constructor's order, as the vectors adapter runs them, and the formatter; or the error.</summary>
    private static string? LibraryFormat(Arms arms, string locale, OptionsBag bag, Reading reading, out DurationFormatCore? format)
    {
        format = null;
        var read = new DecimalSection.BagReader(bag);
        if (read.String("localeMatcher", ["lookup", "best fit"], "best fit", out _) is { } e1)
            return e1;
        if (read.String("numberingSystem", null, null, out var nu) is { } e2)
            return e2;
        if (nu is not null && !LocaleId.IsUnicodeType(nu))
            return "error: RangeError";
        if (LocaleNegotiation.CanonicalizeLocaleList([locale], arms.Data, out var requested, out _) != IntlStatus.Completed)
            return "error: RangeError";

        var match = LocaleNegotiation.MatchLocale(arms.Available, requested, DefaultLocale);
        var values = reading == Reading.Library ? NumberFormatter.NumberingSystemKeyValues(arms.Data, match.Locale) : arms.VmNumberingSystems;
        var resolved = LocaleNegotiation.ResolveLocale(match, [new RelevantKey("nu", values) { Option = nu }], arms.Data);
        var data = reading == Reading.Library ? arms.Data : arms.VmReadingData;
        var dataLocale = reading == Reading.Library ? resolved.DataLocale
            : LocaleId.TryParse(resolved.DataLocale, out var id) == IntlStatus.Completed ? id!.Language : "en";
        var digital = DurationFormatCore.DigitalFormat(data, dataLocale);

        if (read.String("style", ["long", "short", "narrow", "digital"], "short", out var styleName) is { } e3)
            return e3;
        var style = styleName switch { "long" => DurationFormatStyle.Long, "narrow" => DurationFormatStyle.Narrow, "digital" => DurationFormatStyle.Digital, _ => DurationFormatStyle.Short };

        var units = new List<DurationUnitOptions>();
        DurationUnitStyle? previous = null;
        for (var unit = DurationUnit.Years; unit <= DurationUnit.Nanoseconds; unit++)
        {
            var name = UnitNames[(int)unit];
            var styles = DurationFormatCore.UnitStyles(unit);
            var names = styles.Select(StyleName).ToArray();
            if (read.String(name, names, null, out var given) is { } e4)
                return e4;
            if (read.String(name + "Display", ["auto", "always"], null, out var display) is { } e5)
                return e5;
            var status = DurationFormatCore.GetDurationUnitOptions(unit, given is null ? null : styles[Array.IndexOf(names, given)],
                display is null ? null : display == "always" ? DurationUnitDisplay.Always : DurationUnitDisplay.Auto, style, previous, digital.TwoDigitHours, out var options);
            if (status != IntlStatus.Completed)
                return "error: RangeError";
            units.Add(options);
            if (unit is DurationUnit.Hours or DurationUnit.Minutes or DurationUnit.Seconds or DurationUnit.Milliseconds or DurationUnit.Microseconds)
                previous = options.Style;
        }

        // GETNUMBEROPTION of fractionalDigits, 0 to 9, undefined where absent.
        int? fractionalDigits = null;
        if (bag["fractionalDigits"] is { } fd)
        {
            var number = fd switch { double d => d, string s => NumberParsing.StringToNumber(s, null), bool b => b ? 1 : 0, _ => double.NaN };
            if (DigitOptionRules.DefaultNumberOption(number, 0, 9, out var digits) != IntlStatus.Completed)
                return "error: RangeError";
            fractionalDigits = digits;
        }

        format = DurationFormatCore.Create(data, resolved.Locale, dataLocale, resolved.KeyValues["nu"] ?? "latn", style, units, fractionalDigits);
        return null;
    }

    private static string Label(DurationFields d)
    {
        var fields = Record(d);
        var text = new List<string>();
        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i] != 0 || double.IsNegative(fields[i]))
                text.Add(UnitNames[i] + ":" + (fields[i] == 0 ? "-0" : fields[i].ToString("R", CultureInfo.InvariantCulture)));
        }

        return "{" + string.Join(",", text) + "}";
    }

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
            VmReadingData = new CompositeLocaleData(new NumberTextSection.WithoutOwnSymbols(new TableLocaleData(embedding.Library)));
            Available = JsRealm.VmAvailableLocales(Engine).AsReadOnly();
            VmNumberingSystems = ["latn", .. Data.NumberingSystems.Where(static s => s != "latn").Order(StringComparer.Ordinal)];
        }

        internal JsEngine Engine { get; }

        internal CompositeLocaleData Data { get; }

        internal CompositeLocaleData VmReadingData { get; }

        internal IReadOnlyList<string> Available { get; }

        internal IReadOnlyList<string?> VmNumberingSystems { get; }
    }
}
