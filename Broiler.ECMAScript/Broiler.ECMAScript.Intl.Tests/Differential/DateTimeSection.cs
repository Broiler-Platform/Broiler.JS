using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The differential check's sections for sub-area (d), stage SJL-9.3d (Broiler.VM's JSD-0082, decision D22): Broiler.VM's
/// <c>JsDatePatternGenerator</c>, <c>JsDateIntervalFormat</c>, <c>JsDateTimeFormatter</c>, <c>JsZoneNames</c> and its realm's
/// CreateDateTimeFormat and ResolveTimeZone, pinned in <c>Vm/</c>, against the library's <see cref="DateTimeFormat"/>,
/// <see cref="DateTimeFormatter"/> and <see cref="ResolvedTimeZone"/>, over the population of <see cref="DateTimePopulation"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each arm reads the options as an engine does.</b> Broiler.VM's arm is its own CreateDateTimeFormat over the options bag as
/// a script object; the library's reads the same bag in the operation's order, as an engine's adapter does, and hands the library
/// what it read. The realm's prototype methods are not copied, nor HandleDateTimeValue, whose type switch reads the realm's
/// Temporal objects: the harness does what each does around the copied operations - TimeClip's refusal, the range's start and
/// end, <c>resolvedOptions</c>' components through the copied <c>ComponentsOf</c>, and a Temporal value's calendar check, format
/// and time, as Broiler.VM's HandleDateTimeValue and <c>Temporal.ZonedDateTime.prototype.toLocaleString</c> take them.
/// </para>
/// <para>
/// <b>Every format carries its charge</b> where Broiler.VM charges (decision D18): the output's length, once per String or range
/// written (row T40).
/// </para>
/// <para>
/// <b>Two readings classify a difference.</b> Where the arms differ, the library is asked again, first with the zone named by its
/// primary identifier, Broiler.VM's [[TimeZone]] (row Z8, decision D15): a difference that removes is row Z8's, the correction of
/// Broiler.VM. Then as Broiler.VM reads the data as well: at the data locale's language, with Broiler.VM's <c>nu</c> values,
/// <c>latn</c> first (decisions D5 and D13), and a region's time data by its preferred hour format alone, so that <c>hour12</c>
/// takes <c>h12</c> or <c>h23</c> where the library takes the first cycle of its kind the region allows (row T10, from stage
/// SJL-9.3f). A difference that removes is the data policy of rows T10, T15, T16, T22 and T37,
/// Broiler.JS's embedding read at the data locale with its own numbering-system defaults; Broiler.VM's tables hold their locales'
/// languages alone and no default numbering system but <c>latn</c>, so in its embedding that reading is the library's own and
/// only row Z8 can classify a difference. <i>(Extended 2026-11-09, by the owner's decision of that day, "Keep + fix library
/// first": Broiler.VM's reading also takes the region from the likely subtags of the data locale's language alone, a region's
/// time data and never a locale's, the <c>latn</c> decimal symbol (decision D21) and the Gregorian calendar as every locale's
/// default, where the library now reads the whole locale's likely region, CLDR's time data for the language and region first, the
/// numbering system's own decimal symbol and the region's calendar preference. Over Broiler.VM's embedding these readings are the
/// same as the library's for its list - its tables carry no <c>symbols@</c> key and no calendar preference table, and none of its
/// data locales has a script or an entry of its own in the time data - so a difference they removed there would be a move of
/// Broiler.VM's answers; the test holds that none is found.)</i>
/// </para>
/// </remarks>
internal static class DateTimeSection
{
    /// <summary>The row label of a difference the zone's primary identifier removes.</summary>
    internal const string ZoneRow = "Z8 (D15)";

    /// <summary>The row label of a difference Broiler.VM's reading of the data removes.</summary>
    internal const string DataRow = "C5 data (D5, D13)";

    private const string ZoneMark = " (Z8's reading: ";

    private const string DataMark = " (Broiler.VM's reading: ";

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale, IReadOnlyCollection<string>? only)
    {
        var arms = new Arms(embedding);
        var population = DateTimePopulation.Build(embedding, random, scale, HomeTimeZones.Current);
        var suffix = " (" + embedding.Name + ")";

        if (only is null || only.Contains("T1"))
            yield return Construction(arms, population, "T1 the date-time format's construction and resolved options" + suffix);
        if (only is null || only.Contains("T2"))
            yield return Formatting(arms, population, random, "T2 date-time formatting: format and formatToParts, with the charge" + suffix);
        if (only is null || only.Contains("T3"))
            yield return Ranges(arms, population, random, "T3 date-time ranges: formatRange and formatRangeToParts, with the charge" + suffix);
        if (only is null || only.Contains("T4"))
            yield return Temporal(arms, population, "T4 Intl over Temporal: the formats of Temporal's types, with the charge" + suffix);
        if (only is null || only.Contains("T5"))
            yield return Zones(arms, population, "T5 the time zone's resolution" + suffix);
    }

    /// <summary>
    /// Sub-area (d)'s two rows: a difference the library's answer with the zone named by its primary removes (row Z8), and one its
    /// answer under Broiler.VM's reading of the data removes, each written into the input, so that the row covers it only when that
    /// answer is Broiler.VM's. Any other difference stays unclassified.
    /// </summary>
    internal static IEnumerable<KnownDifference> Known()
    {
        foreach (var section in new[] { "T1", "T2", "T3", "T4", "T5" })
        {
            yield return new KnownDifference(section, ZoneRow, static (input, vm, _) => input.Contains(ZoneMark + Quote(vm) + ")", StringComparison.Ordinal));
            yield return new KnownDifference(section, DataRow, static (input, vm, _) => input.Contains(DataMark + Quote(vm) + ")", StringComparison.Ordinal));
        }
    }

    /// <summary>How the library's arm reads: as the library does; with the zone named by its primary; or as Broiler.VM reads the data too.</summary>
    private enum Reading
    {
        Library,
        PrimaryZone,
        BroilerVm,
    }

    /// <summary>Compares one input, and on a difference writes the library's answers under the two readings into it.</summary>
    private static void Compare(Section section, string input, Func<string> vm, Func<Reading, string> library)
    {
        var vmAnswer = Call(vm);
        var libraryAnswer = Call(() => library(Reading.Library));
        if (!string.Equals(vmAnswer, libraryAnswer, StringComparison.Ordinal))
        {
            input += ZoneMark + Quote(Call(() => library(Reading.PrimaryZone))) + ")";
            input += DataMark + Quote(Call(() => library(Reading.BroilerVm))) + ")";
        }

        section.Compare(input, vmAnswer, libraryAnswer);
    }

    private static string Quote(string text) => LocaleSection.Quote(text);

    // ---- T1 ------------------------------------------------------------------------------------------------------------------

    private static Section Construction(Arms arms, DateTimePopulation population, string name)
    {
        var section = new Section(name);
        foreach (var format in population.Formats)
        {
            Compare(section, "format " + format.Label,
                () => RenderVm(VmFormat(arms, format)),
                reading => LibraryFormat(arms, format, reading, out var made) is { } refused ? refused : RenderLibrary(made!, reading));
        }

        return section;
    }

    private static string RenderVm(JsDateTimeFormatObject format)
    {
        var text = new StringBuilder();
        text.Append(format.Locale).Append('|').Append(format.Calendar).Append('|').Append(format.NumberingSystem).Append('|')
            .Append(format.Formatter.TimeZone).Append('|').Append(format.HourCycle ?? "-").Append('|');
        if (format.DateStyle is null && format.TimeStyle is null)
        {
            var components = JsRealm.VmComponentsOf(format.Formatter.Pattern);
            foreach (var component in DateTimeComponents.Names)
            {
                if (components.TryGetValue(component, out var value))
                    text.Append(component).Append('=').Append(component == "fractionalSecondDigits" ? value.Length.ToString(CultureInfo.InvariantCulture) : value).Append(',');
            }
        }

        return text.Append('|').Append(format.DateStyle ?? "-").Append('|').Append(format.TimeStyle ?? "-").Append('|').Append(format.Formatter.Pattern).ToString();
    }

    private static string RenderLibrary(DateTimeFormat format, Reading reading)
    {
        var text = new StringBuilder();
        text.Append(format.Locale).Append('|').Append(format.Calendar).Append('|').Append(format.NumberingSystem).Append('|')
            .Append(reading == Reading.Library ? format.TimeZone : format.Formatter.ResolvedTimeZone.PrimaryIdentifier).Append('|')
            .Append(format.HourCycle ?? "-").Append('|');
        foreach (var (component, value) in format.ResolvedComponents)
            text.Append(component).Append('=').Append(value).Append(',');
        return text.Append('|').Append(format.DateStyle ?? "-").Append('|').Append(format.TimeStyle ?? "-").Append('|').Append(format.Formatter.Pattern).ToString();
    }

    // ---- T2 and T3 -------------------------------------------------------------------------------------------------------

    private static Section Formatting(Arms arms, DateTimePopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void FormatOne(FormatInput format, double time) =>
            Compare(section, "format " + format.Label + " " + time.ToString("R", CultureInfo.InvariantCulture),
                () =>
                {
                    var made = VmFormat(arms, format);
                    var clipped = EcmaTime.TimeClip(time);
                    if (double.IsNaN(clipped))
                        return "error: RangeError";
                    var before = arms.Engine.Charged;
                    var parts = made.Formatter.Parts(arms.Engine, clipped, null);
                    return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    if (LibraryFormat(arms, format, reading, out var made) is { } refused)
                        return refused;
                    var status = made!.HandleDateTimeValue(DateTimeValue.Number(time), out var formatter, out var clipped);
                    if (status != IntlStatus.Completed)
                        return Refusal(status);
                    ulong charged = 0;
                    var parts = formatter!.FormatToParts(clipped, units => charged += units);
                    return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + charged;
                });

        // EVERY DATASET CASE WITH ITS PROGRAM'S TIMES, then each vector's own format with its script's.
        foreach (var (format, times, _) in population.DatasetCases.Concat(population.VectorCases))
        {
            foreach (var time in times)
                FormatOne(format, time);
        }

        // THE RANDOM DRAWS: each format with times, and each time with a format.
        foreach (var format in population.Formats)
        {
            for (var i = 0; i < 3; i++)
                FormatOne(format, random.Pick(population.Times));
        }

        foreach (var time in population.Times.Distinct())
            FormatOne(random.Pick(population.Formats), time);

        return section;
    }

    private static Section Ranges(Arms arms, DateTimePopulation population, SeededRandom random, string name)
    {
        var section = new Section(name);

        void RangeOne(FormatInput format, double start, double end) =>
            Compare(section, "range " + format.Label + " " + start.ToString("R", CultureInfo.InvariantCulture) + " .. " + end.ToString("R", CultureInfo.InvariantCulture),
                () =>
                {
                    var made = VmFormat(arms, format);
                    var x = EcmaTime.TimeClip(start);
                    var y = EcmaTime.TimeClip(end);
                    if (double.IsNaN(x) || double.IsNaN(y))
                        return "error: RangeError";
                    var before = arms.Engine.Charged;
                    var parts = made.Formatter.RangeParts(arms.Engine, x, y);
                    return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + (arms.Engine.Charged - before);
                },
                reading =>
                {
                    if (LibraryFormat(arms, format, reading, out var made) is { } refused)
                        return refused;
                    var status = made!.HandleDateTimeRange(DateTimeValue.Number(start), DateTimeValue.Number(end), out var formatter, out var x, out var y);
                    if (status != IntlStatus.Completed)
                        return Refusal(status);
                    ulong charged = 0;
                    var parts = formatter!.FormatRangeToParts(x, y, units => charged += units);
                    return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + charged;
                });

        foreach (var (format, _, ranges) in population.DatasetCases.Concat(population.VectorCases))
        {
            foreach (var (start, end) in ranges)
                RangeOne(format, start, end);
        }

        foreach (var (start, end) in population.Ranges)
        {
            for (var i = 0; i < 2; i++)
                RangeOne(random.Pick(population.Formats), start, end);
        }

        return section;
    }

    // ---- T4 --------------------------------------------------------------------------------------------------------------

    private static Section Temporal(Arms arms, DateTimePopulation population, string name)
    {
        var section = new Section(name);

        foreach (var format in population.TemporalFormats.Concat(population.Formats.Take(80)))
        {
            foreach (var value in population.TemporalValues)
            {
                // A ZONEDDATETIME is written by its own toLocaleString, which names its zone and refuses the option's.
                var used = value.Kind == "zoned"
                    ? format with { Bag = new OptionsBag([.. format.Bag.Entries.Where(static e => e.Key != "timeZone")]), Required = "any", Defaults = "all" }
                    : format;
                Compare(section, "temporal " + used.Label + " " + value.Label,
                    () => VmTemporal(arms, used, value),
                    reading => LibraryTemporal(arms, used, value, reading));
            }

            // THE RANGES OF TEMPORAL VALUES, formatRange's HandleDateTimeValue of each end, none clipped.
            foreach (var (start, end) in population.TemporalRanges)
            {
                Compare(section, "temporal range " + format.Label + " " + start.Label + " .. " + end.Label,
                    () => VmTemporalRange(arms, format, start, end),
                    reading => LibraryTemporalRange(arms, format, start, end, reading));
            }
        }

        return section;
    }

    /// <summary>
    /// Broiler.VM's HandleDateTimeValue (and ZonedDateTime's <c>toLocaleString</c>) over a value, as its realm takes it: the
    /// calendar's check first, then the type's format, then its time - a date's noon, a time on 1970-01-01, each at UTC.
    /// </summary>
    private static string VmTemporal(Arms arms, FormatInput input, TemporalInput value)
    {
        var format = VmFormat(arms, input, value.Zone);
        var step = VmStep(format, value);

        if (!step.CalendarOk)
            return "error: RangeError";
        var formatter = format.Temporal!.For(step.Kind);
        if (formatter is null)
            return "error: TypeError";
        var before = arms.Engine.Charged;
        var parts = formatter.Parts(arms.Engine, step.Time, null);
        return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + (arms.Engine.Charged - before);
    }

    /// <summary>Broiler.VM's realm's step for one value: the format it takes, its time and whether its calendar passes.</summary>
    private static (string Kind, double Time, bool CalendarOk) VmStep(JsDateTimeFormatObject format, TemporalInput value)
    {
        static double Milliseconds(BigInteger epochNs)
        {
            var quotient = BigInteger.DivRem(epochNs, 1_000_000, out var remainder);
            return (double)(remainder.Sign < 0 ? quotient - 1 : quotient);
        }

        static double Noon(IsoDate date) => Milliseconds(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(date, new IsoTime(12, 0, 0, 0, 0, 0))));

        bool Same(bool isoAllowed) => value.Calendar == format.Calendar || (isoAllowed && value.Calendar == "iso8601");

        return value.Kind switch
        {
            "date" => ("date", Noon(value.Date), Same(true)),
            "year-month" => ("year-month", Noon(value.Date), Same(false)),
            "month-day" => ("month-day", Noon(value.Date), Same(false)),
            "time" => ("time", Milliseconds(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(1970, 1, 1), value.Time))), true),
            "date-time" => ("date-time", Milliseconds(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(value.Date, value.Time))), Same(true)),
            "instant" => ("instant", Milliseconds(value.EpochNanoseconds), true),
            _ => ("instant", Milliseconds(value.EpochNanoseconds), value.Calendar == "iso8601" || value.Calendar == format.Calendar),
        };
    }

    /// <summary>Broiler.VM's formatRange over two Temporal values of one type: each end's step, the start's format writing both.</summary>
    private static string VmTemporalRange(Arms arms, FormatInput input, TemporalInput start, TemporalInput end)
    {
        var format = VmFormat(arms, input);
        var x = VmStep(format, start);
        if (!x.CalendarOk)
            return "error: RangeError";
        var formatter = format.Temporal!.For(x.Kind);
        if (formatter is null)
            return "error: TypeError";
        var y = VmStep(format, end);
        if (!y.CalendarOk)
            return "error: RangeError";
        var before = arms.Engine.Charged;
        var parts = formatter.RangeParts(arms.Engine, x.Time, y.Time);
        return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + (arms.Engine.Charged - before);
    }

    private static string LibraryTemporalRange(Arms arms, FormatInput input, TemporalInput start, TemporalInput end, Reading reading)
    {
        if (LibraryFormat(arms, input, reading, out var format) is { } refused)
            return refused;
        var status = format!.HandleDateTimeRange(ValueOf(start), ValueOf(end), out var formatter, out var x, out var y);
        if (status != IntlStatus.Completed)
            return Refusal(status);
        ulong charged = 0;
        var parts = formatter!.FormatRangeToParts(x, y, units => charged += units);
        return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + charged;
    }

    private static DateTimeValue ValueOf(TemporalInput value) => value.Kind switch
    {
        "date" => DateTimeValue.PlainDate(value.Date, value.Calendar),
        "year-month" => DateTimeValue.PlainYearMonth(value.Date, value.Calendar),
        "month-day" => DateTimeValue.PlainMonthDay(value.Date, value.Calendar),
        "time" => DateTimeValue.PlainTime(value.Time),
        "date-time" => DateTimeValue.PlainDateTime(new IsoDateTime(value.Date, value.Time), value.Calendar),
        "instant" => DateTimeValue.Instant(value.EpochNanoseconds),
        _ => DateTimeValue.ZonedDateTime(value.EpochNanoseconds, value.Calendar),
    };

    private static string LibraryTemporal(Arms arms, FormatInput input, TemporalInput value, Reading reading)
    {
        if (LibraryFormat(arms, input, reading, out var format, value.Zone) is { } refused)
            return refused;
        var status = format!.HandleDateTimeValue(ValueOf(value), out var formatter, out var time);
        if (status != IntlStatus.Completed)
            return Refusal(status);
        ulong charged = 0;
        var parts = formatter!.FormatToParts(time, units => charged += units);
        return Render(parts.Select(static p => (p.Type, p.Value, p.Source))) + " charge " + charged;
    }

    // ---- T5 --------------------------------------------------------------------------------------------------------------

    private static Section Zones(Arms arms, DateTimePopulation population, string name)
    {
        var section = new Section(name);
        foreach (var zone in population.Zones)
        {
            Compare(section, "zone " + Quote(zone),
                () =>
                {
                    var (identifier, offset, kind, rules) = JsRealm.VmResolveTimeZone(arms.Engine, JsValue.String(zone));
                    return identifier + " " + offset.ToString(CultureInfo.InvariantCulture) + " " + kind + " " + (rules is null ? "fixed" : "rules");
                },
                reading =>
                {
                    var status = ResolvedTimeZone.Resolve(zone, HomeTimeZones.Current, out var resolved);
                    if (status != IntlStatus.Completed)
                        return Refusal(status);
                    return (reading == Reading.Library ? resolved!.Identifier : resolved!.PrimaryIdentifier) + " "
                        + resolved.OffsetMinutes.ToString(CultureInfo.InvariantCulture) + " " + resolved.Kind + " " + (resolved.Rules is null ? "fixed" : "rules");
                });
        }

        return section;
    }

    // ---- the two arms' formats -------------------------------------------------------------------------------------------

    private static JsDateTimeFormatObject VmFormat(Arms arms, FormatInput input, string? zonedTimeZone = null)
    {
        var key = input.Label + "|" + zonedTimeZone;
        if (!arms.VmFormats.TryGetValue(key, out var made))
        {
            made = JsRealm.VmCreateDateTimeFormat(arms.Engine, input.Locale is null ? JsValue.Undefined : JsValue.String(input.Locale),
                DecimalSection.VmBag(arms.Engine, input.Bag), input.Required, input.Defaults, zonedTimeZone);
            arms.VmFormats[key] = made;
        }

        return made;
    }

    /// <summary>
    /// The library's format of an input, as an engine's adapter makes it: the locale list, the options read in
    /// CreateDateTimeFormat's order and checked, ResolveLocale over Broiler.VM's providers, the zone, then
    /// <see cref="DateTimeFormat.Create"/>; or the refusal, as its error's kind.
    /// </summary>
    private static string? LibraryFormat(Arms arms, FormatInput input, Reading reading, out DateTimeFormat? format, string? zonedTimeZone = null)
    {
        var vmReading = reading == Reading.BroilerVm;
        var key = input.Label + "|" + zonedTimeZone + "|" + vmReading;
        if (arms.LibraryFormats.TryGetValue(key, out var cached))
        {
            format = cached.Format;
            return cached.Refused;
        }

        var refused = Make(arms, input, vmReading, zonedTimeZone, out format);
        arms.LibraryFormats[key] = (format, refused);
        return refused;
    }

    private static string? Make(Arms arms, FormatInput input, bool vmReading, string? zonedTimeZone, out DateTimeFormat? format)
    {
        format = null;
        var data = vmReading ? arms.VmReadingData : arms.Data;
        var dates = vmReading ? arms.VmReadingDates : arms.Dates;
        var bag = input.Bag;
        List<string> tags = input.Locale is null ? [] : [input.Locale];
        if (LocaleNegotiation.CanonicalizeLocaleList(tags, arms.Data, out var requested, out _) != IntlStatus.Completed)
            return "error: RangeError";

        string? Get(string option, string[]? values)
        {
            if (bag[option] is not { } given)
                return null;
            var text = given switch
            {
                string s => s,
                bool b => b ? "true" : "false",
                double d => NumberText.ToString(d),
                _ => throw new NotSupportedException("no bag holds a " + given.GetType().Name),
            };
            return values is not null && Array.IndexOf(values, text) < 0 ? throw new OptionRefused() : text;
        }

        try
        {
            _ = Get("localeMatcher", ["lookup", "best fit"]);
            var calendar = Get("calendar", null);
            if (calendar is not null && !LocaleId.IsUnicodeType(calendar))
                return "error: RangeError";
            var numberingSystem = Get("numberingSystem", null);
            if (numberingSystem is not null && !LocaleId.IsUnicodeType(numberingSystem))
                return "error: RangeError";
            bool? hour12 = bag[("hour12")] switch
            {
                null => null,
                bool b => b,
                string s => s.Length != 0,
                double d => d is not (0 or double.NaN),
                _ => true,
            };
            var hourCycle = Get("hourCycle", ["h11", "h12", "h23", "h24"]);

            var match = LocaleNegotiation.MatchLocale(arms.Available, requested, "en-US");
            var resolved = LocaleNegotiation.ResolveLocale(match,
            [
                // THE ca VALUES, as an engine hands them: the locale's default calendar first, from the data's calendar preferences
                // (added 2026-11-09, by the owner's decision of that day, "Keep + fix library first"; it read
                // `new RelevantKey("ca", DateTimeFormat.CalendarKeyValues)`, which Broiler.VM's reading still takes).
                new RelevantKey("ca", vmReading ? DateTimeFormat.CalendarKeyValues : DateTimeFormat.CalendarKeyValuesOf(data, match.Locale)) { Option = calendar },
                new RelevantKey("hc", DateTimeFormat.HourCycleKeyValues) { Option = hour12 is null ? hourCycle : null, OptionIsNull = hour12 is not null },
                new RelevantKey("nu", vmReading ? arms.VmNumberingSystems : DateTimeFormat.NumberingSystemKeyValues(data, match.Locale)) { Option = numberingSystem },
            ], data);

            var zone = ResolvedTimeZone.Utc;
            var zoneOption = Get("timeZone", null);
            if (zonedTimeZone is not null)
            {
                if (zoneOption is not null)
                    return "error: TypeError";
                zoneOption = zonedTimeZone;
            }

            if (zoneOption is not null)
            {
                var status = ResolvedTimeZone.Resolve(zoneOption, HomeTimeZones.Current, out var found);
                if (status != IntlStatus.Completed)
                    return Refusal(status);
                zone = found!;
            }

            int? fractional = null;
            if (bag["fractionalSecondDigits"] is { } digits)
            {
                var value = digits switch { double d => d, string s => NumberParsing.StringToNumber(s, null), bool b => b ? 1 : 0, _ => double.NaN };
                if (double.IsNaN(value) || value < 1 || value > 3)
                    return "error: RangeError";
                fractional = (int)Math.Floor(value);
            }

            var components = new DateTimeComponents
            {
                Weekday = Get("weekday", ["narrow", "short", "long"]),
                Era = Get("era", ["narrow", "short", "long"]),
                Year = Get("year", ["2-digit", "numeric"]),
                Month = Get("month", ["2-digit", "numeric", "narrow", "short", "long"]),
                Day = Get("day", ["2-digit", "numeric"]),
                DayPeriod = Get("dayPeriod", ["narrow", "short", "long"]),
                Hour = Get("hour", ["2-digit", "numeric"]),
                Minute = Get("minute", ["2-digit", "numeric"]),
                Second = Get("second", ["2-digit", "numeric"]),
                FractionalSecondDigits = fractional,
                TimeZoneName = Get("timeZoneName", ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"]),
            };
            _ = Get("formatMatcher", ["basic", "best fit"]);
            var dateStyle = Get("dateStyle", ["full", "long", "medium", "short"]);
            var timeStyle = Get("timeStyle", ["full", "long", "medium", "short"]);

            var created = DateTimeFormat.Create(dates, resolved, new DateTimeFormatOptions
            {
                Hour12 = hour12,
                TimeZone = zone,
                Components = components,
                DateStyle = dateStyle,
                TimeStyle = timeStyle,
                Required = input.Required switch { "date" => DateTimeRequired.Date, "time" => DateTimeRequired.Time, _ => DateTimeRequired.Any },
                Defaults = input.Defaults switch { "time" => DateTimeDefaults.Time, "all" => DateTimeDefaults.All, _ => DateTimeDefaults.Date },
                ForZonedDateTime = zonedTimeZone is not null,
            }, out format);
            return created == IntlStatus.Completed ? null : Refusal(created);
        }
        catch (OptionRefused)
        {
            return "error: RangeError";
        }
    }

    /// <summary>GetOption's <c>RangeError</c> for a value outside the option's list.</summary>
    private sealed class OptionRefused : Exception;

    /// <summary>A status as an engine throws it.</summary>
    private static string Refusal(IntlStatus status) => status switch
    {
        IntlStatus.TimeZoneOffsetHasSeconds or IntlStatus.InvalidTimeZone or IntlStatus.TimeValueOutOfRange or IntlStatus.TemporalCalendarMismatch => "error: RangeError",
        _ => "error: TypeError",
    };

    private static string Render(IEnumerable<(string Type, string Value, string? Source)> parts)
    {
        var text = new StringBuilder();
        foreach (var (type, value, source) in parts)
            text.Append(type).Append(source is null ? string.Empty : "@" + source).Append(':').Append(value).Append('|');
        return text.ToString();
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
            Engine = new JsEngine(new VmIntl(new JsIntlTables(embedding.Vm), HomeTimeZones.Current, HomeCalendars.Current, embedding.Lines(IntlTable.MetaZones)));
            Data = new CompositeLocaleData(new TableLocaleData(embedding.Library));
            VmReadingData = new AtLanguage(Data);
            Dates = new DateTimeData(Data, HomeTimeZones.Current, HomeCalendars.Current);
            VmReadingDates = new DateTimeData(VmReadingData, HomeTimeZones.Current, HomeCalendars.Current);
            Available = JsRealm.VmAvailableLocales(Engine).AsReadOnly();
            VmNumberingSystems = ["latn", .. Data.NumberingSystems.Where(static s => s != "latn").Order(StringComparer.Ordinal)];
        }

        internal JsEngine Engine { get; }

        internal CompositeLocaleData Data { get; }

        /// <summary>The same data read as Broiler.VM reads its date data: every locale-keyed value at the locale's language.</summary>
        internal ILocaleData VmReadingData { get; }

        internal DateTimeData Dates { get; }

        internal DateTimeData VmReadingDates { get; }

        internal IReadOnlyList<string> Available { get; }

        internal IReadOnlyList<string?> VmNumberingSystems { get; }

        internal Dictionary<string, JsDateTimeFormatObject> VmFormats { get; } = new(StringComparer.Ordinal);

        internal Dictionary<string, (DateTimeFormat? Format, string? Refused)> LibraryFormats { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// Locale data read as Broiler.VM's realm reads its date-time format's: a locale's date, number and display-name values at
    /// the locale's language (<c>JsLocaleTag.Parse(DataLocale).Language</c>); every other question as the data answers it.
    /// </summary>
    private sealed class AtLanguage(ILocaleData inner) : ILocaleData
    {
        private static string Language(string locale)
        {
            var dash = locale.IndexOf('-', StringComparison.Ordinal);
            return dash < 0 ? locale : locale[..dash];
        }

        public IReadOnlyList<string> Locales => inner.Locales;

        public bool HoldsLocale(IntlTable table, string locale) => inner.HoldsLocale(table, locale);

        // Broiler.VM takes a data locale's region, where it names none, from the likely subtags of its language alone; the library
        // from those of the whole locale (zh-Hant's TW, where zh gives CN; added 2026-11-09, by the owner's decision of that day,
        // "Keep + fix library first"). So a source with a script or a region has no likely subtags here, and the library's
        // Add Likely Subtags falls back to the language's, which gives Broiler.VM's region. It read
        // `inner.LikelySubtags(source)`.
        public string? LikelySubtags(string source) => source.Contains('-', StringComparison.Ordinal) ? null : inner.LikelySubtags(source);

        public string? Alias(LocaleAliasKind kind, string from) => inner.Alias(kind, from);

        public bool IsExtensionKey(char singleton, string key) => inner.IsExtensionKey(singleton, key);

        public bool TryExtensionType(char singleton, string key, string type, out string preferred) => inner.TryExtensionType(singleton, key, type, out preferred);

        // Broiler.VM reads a region's preferred hour format alone (its JsDateData.PreferredHourChar), and its hour12 takes h12 or
        // h23 where the preferred is of the other kind; the library takes the first of that kind the region allows (row T10,
        // settled at stage SJL-9.3f). So the allowed formats are read here as the preferred alone, which gives Broiler.VM's.
        // Broiler.VM reads a region's time data, never a locale's: since 2026-11-09 the library asks for the language and region's
        // entry first (fr-CA, ar-001), which this reading answers as none.
        public RegionHourCycles? HourCycles(string region) =>
            !region.Contains('-', StringComparison.Ordinal) && inner.HourCycles(region) is { } cycles ? cycles with { Allowed = cycles.Preferred } : null;

        public RegionWeek? Week(string region) => inner.Week(region);

        public bool? IsRightToLeft(string script) => inner.IsRightToLeft(script);

        // Broiler.VM's date-time format writes the latn decimal symbol for every numbering system; since 2026-11-09 the library
        // writes the system's own, symbols@<system>.decimal (decision D21), which this reading answers as none. It read
        // `inner.NumberValue(Language(locale), key)` for every key.
        public string? NumberValue(string locale, string key) =>
            key.StartsWith("symbols@", StringComparison.Ordinal) ? null : inner.NumberValue(Language(locale), key);

        public CurrencyNames? Currency(string locale, string code) => inner.Currency(Language(locale), code);

        public IReadOnlyList<string> CurrencyCodes => inner.CurrencyCodes;

        public int? CurrencyDigits(string code) => inner.CurrencyDigits(code);

        public IReadOnlyList<string> NumberingSystems => inner.NumberingSystems;

        public IReadOnlyList<string>? NumberingSystemDigits(string system) => inner.NumberingSystemDigits(system);

        public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) => inner.PluralRules(language, type);

        public string? PluralRange(string language, string start, string end) => inner.PluralRange(language, start, end);

        public string? UnitPattern(string locale, string width, string unit, string field) => inner.UnitPattern(Language(locale), width, unit, field);

        public ListPatterns? ListPattern(string locale, string type) => inner.ListPattern(Language(locale), type);

        public string? RelativeTime(string locale, string field, string key) => inner.RelativeTime(Language(locale), field, key);

        public string? DisplayName(string locale, string kind, string code) => inner.DisplayName(Language(locale), kind, code);

        public string? DateValue(string locale, string calendar, string key) => inner.DateValue(Language(locale), calendar, key);

        public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => inner.DateValues(Language(locale), calendar);

        public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => inner.DayPeriods(Language(language));

        public IReadOnlyList<MetazonePeriod>? MetazonePeriods(string zone) => inner.MetazonePeriods(zone);

        public string? GoldenZone(string metazone, string region) => inner.GoldenZone(metazone, region);

        public string? PrimaryZone(string region) => inner.PrimaryZone(region);
    }
}
