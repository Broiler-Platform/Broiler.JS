using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>One date-time format a population input constructs: its locale, its options and CreateDateTimeFormat's <c>required</c> and <c>defaults</c>.</summary>
/// <param name="Locale">The constructor's locale, or <see langword="null"/> for none.</param>
/// <param name="Bag">The options.</param>
/// <param name="Required"><c>any</c>, <c>date</c> or <c>time</c>.</param>
/// <param name="Defaults"><c>date</c>, <c>time</c> or <c>all</c>.</param>
internal sealed record FormatInput(string? Locale, OptionsBag Bag, string Required = "any", string Defaults = "date")
{
    /// <summary>The format as a difference's report names it.</summary>
    internal string Label => (Locale is null ? "<default>" : LocaleSection.Quote(Locale)) + " " + Bag.Label + (Required == "any" && Defaults == "date" ? string.Empty : " " + Required + "/" + Defaults);
}

/// <summary>A Temporal value a format writes: its type, as HandleDateTimeValue names the format it takes, its slots and its calendar.</summary>
/// <param name="Kind"><c>date</c>, <c>year-month</c>, <c>month-day</c>, <c>time</c>, <c>date-time</c>, <c>instant</c> or <c>zoned</c>.</param>
/// <param name="Date">A plain value's ISO date (a PlainYearMonth's and a PlainMonthDay's reference date).</param>
/// <param name="Time">A PlainTime's or a PlainDateTime's time.</param>
/// <param name="EpochNanoseconds">An Instant's or a ZonedDateTime's epoch nanoseconds.</param>
/// <param name="Calendar">The calendar identifier.</param>
/// <param name="Zone">A ZonedDateTime's zone, which its <c>toLocaleString</c> writes it in.</param>
internal sealed record TemporalInput(string Kind, IsoDate Date, IsoTime Time, BigInteger EpochNanoseconds, string Calendar, string? Zone = null)
{
    internal string Label => Kind + " " + Kind switch
    {
        "time" => Time.ToString(),
        "instant" or "zoned" => EpochNanoseconds.ToString(CultureInfo.InvariantCulture) + (Zone is null ? string.Empty : "[" + Zone + "]"),
        "date-time" => Date + " " + Time,
        _ => Date.ToString(),
    } + " " + Calendar;
}

/// <summary>
/// The population of sub-area (d)'s sections (Broiler.VM's JSD-0082, decision D22): every date-time format a vector of
/// <c>intl/date-times.vectors</c> constructs, with the times and ranges its script writes; every dataset input of the
/// <c>dates</c>, <c>calendar-formats</c>, <c>temporal-intl</c> and <c>zone-names</c> programs as library calls - each case's
/// format with the program's instants, ranges, dates and Temporal values - and every name of the time data's <c>timezones</c>
/// dataset, the database's Zone and Link names alike; Temporal's limits, which HandleDateTimeValue does not clip; and seeded
/// random formats, times, ranges, Temporal values and zone names over the embedding's locales, so many per unit of scale.
/// </summary>
internal sealed class DateTimePopulation
{
    /// <summary>Each dataset case, with the times and ranges its program writes it with, which the sections compare in full.</summary>
    internal List<(FormatInput Format, IReadOnlyList<double> Times, IReadOnlyList<(double Start, double End)> Ranges)> DatasetCases { get; } = [];

    /// <summary>Each vector's own format, with the times and ranges its script writes.</summary>
    internal List<(FormatInput Format, IReadOnlyList<double> Times, IReadOnlyList<(double Start, double End)> Ranges)> VectorCases { get; } = [];

    /// <summary>Every format the sections construct: the datasets', the vectors' and the random ones.</summary>
    internal List<FormatInput> Formats { get; } = [];

    /// <summary>The temporal-intl program's formats, which the Temporal section writes every Temporal value with.</summary>
    internal List<FormatInput> TemporalFormats { get; } = [];

    internal List<TemporalInput> TemporalValues { get; } = [];

    /// <summary>The ranges of two Temporal values of one type the Temporal section writes with formatRange: the limits' own.</summary>
    internal List<(TemporalInput Start, TemporalInput End)> TemporalRanges { get; } = [];

    internal List<double> Times { get; } = [];

    internal List<(double Start, double End)> Ranges { get; } = [];

    /// <summary>
    /// The <c>timeZone</c> option's Strings: every name the timezones dataset lists (the database's Zone and Link names), each
    /// also lowered, offsets, case variants and refusals.
    /// </summary>
    internal List<string> Zones { get; } = [];

    private static readonly string[] StyleValues = ["full", "long", "medium", "short"];

    internal static DateTimePopulation Build(Embedding embedding, SeededRandom random, int scale, ITimeZoneDatabase timeZones)
    {
        var population = new DateTimePopulation();
        var locales = embedding.Lines(IntlTable.Locales).Where(static l => l != "und").ToList();

        // EVERY VECTOR'S OWN FORMAT, with the times its script writes.
        foreach (var vector in Population.IntlVectors("date-times.vectors"))
        {
            foreach (var call in VectorFormats(vector.Script))
                population.VectorCases.Add(call);
        }

        // EVERY DATASET INPUT.
        foreach (var (name, source) in Population.DatasetPrograms())
        {
            var program = WithoutComments(source);
            switch (name)
            {
                case "dates":
                {
                    var instants = DecimalPopulation.Array(program, "instants").OfType<double>().ToList();
                    var ranges = DecimalPopulation.Array(program, "ranges").OfType<List<object?>>()
                        .Where(static r => r.Count == 2 && r[0] is double && r[1] is double).Select(static r => ((double)r[0]!, (double)r[1]!)).ToList();
                    foreach (var item in DecimalPopulation.Array(program, "cases").OfType<List<object?>>())
                    {
                        if (item.Count > 0 && item[0] is string locale)
                            population.DatasetCases.Add((new FormatInput(locale, item.Count > 1 && item[1] is OptionsBag bag ? bag : OptionsBag.Empty), instants, ranges));
                    }

                    break;
                }

                case "calendar-formats":
                {
                    var dates = DecimalPopulation.Array(program, "dates").OfType<string>().Select(static d => Noon(IsoDateOf(d))).ToList();
                    foreach (var locale in Strings(program, "locales"))
                    {
                        foreach (var calendar in Strings(program, "calendars"))
                        {
                            foreach (var bag in DecimalPopulation.Array(program, "optionSets").OfType<OptionsBag>())
                                population.DatasetCases.Add((new FormatInput(locale, With(With(bag, "calendar", calendar), "timeZone", "UTC")), dates, []));
                        }
                    }

                    break;
                }

                case "temporal-intl":
                    foreach (var locale in Strings(program, "locales"))
                    {
                        foreach (var calendar in new string?[] { null, "iso8601" })
                        {
                            foreach (var bag in DecimalPopulation.Array(program, "optionSets").OfType<OptionsBag>())
                            {
                                var full = With(bag, "timeZone", "Pacific/Apia");
                                population.TemporalFormats.Add(new FormatInput(locale, calendar is null ? full : With(full, "calendar", calendar)));
                            }
                        }
                    }

                    population.TemporalValues.AddRange(TemporalDataset());
                    break;

                case "zone-names":
                {
                    var instants = DecimalPopulation.Array(program, "instants").OfType<string>().Select(Instant).ToList();
                    var zones = DecimalPopulation.Array(program, "zones").OfType<string>().ToList();
                    foreach (var locale in new[] { "en", "de" })
                    {
                        foreach (var zone in zones)
                        {
                            foreach (var style in Strings(program, "styles"))
                                population.DatasetCases.Add((new FormatInput(locale, Bag(("timeZone", zone), ("timeZoneName", style))), instants, []));
                        }
                    }

                    break;
                }
            }
        }

        // THE ZONES: every name the timezones dataset lists, the database's Zone and Link names alike, in its case and lowered;
        // then the offsets and the refusals of row T21.
        var datasetZones = TimeZoneDatasetNames();
        foreach (var zone in datasetZones)
        {
            if (!timeZones.TryFind(zone, out _))
                throw new InvalidDataException("the timezones dataset's " + zone + " is not a name of the database");
            population.Zones.Add(zone);
            population.Zones.Add(zone.ToLowerInvariant());
        }

        population.Zones.AddRange([
            "Asia/Calcutta", "asia/calcutta", "US/Eastern", "Europe/Kiev", "America/Buenos_Aires", "Etc/UTC", "Etc/GMT", "GMT", "Etc/UCT",
            "Universal", "Zulu", "Etc/GMT0", "EST", "CET", "Atlantic/Jan_Mayen", "Antarctica/South_Pole", "Pacific/Enderbury", "America/Godthab",
            "ASIA/KOLKATA", "utc", "etc/gmt-14", "Etc/GMT+5", "+05:30", "+0530", "-08", "-00:00", "+05:30:00", "+24:00", "Mars/Phobos", "",
            "Europe/Berlin ", "2024-01-05T00:00Z", "America/Loſ_Angeles", "Asia/Kolkatı", "Asia/Kolkata",
        ]);
        for (var i = 0; i < 40 * scale; i++)
        {
            var zone = random.Pick(datasetZones);
            population.Zones.Add(random.Next(2) == 0 ? zone.ToLowerInvariant() : zone.ToUpperInvariant());
        }

        // THE FORMATS: the datasets', the vectors', then seeded random ones over the embedding's locales.
        population.Formats.AddRange(population.DatasetCases.Select(static c => c.Format));
        population.Formats.AddRange(population.VectorCases.Select(static c => c.Format));
        population.Formats.AddRange(population.TemporalFormats);
        for (var i = 0; i < 60 * scale; i++)
            population.Formats.Add(RandomFormat(random, locales, datasetZones));

        // THE TIMES AND RANGES: the datasets', the vectors', then seeded random ones.
        foreach (var (_, times, ranges) in population.DatasetCases.Concat(population.VectorCases))
        {
            population.Times.AddRange(times);
            population.Ranges.AddRange(ranges);
        }

        for (var i = 0; i < 40 * scale; i++)
        {
            var time = RandomTime(random);
            population.Times.Add(time);
            population.Ranges.Add((time, Math.Min(8.64e15, time + random.Pick(RangeSteps))));
        }

        // THE TEMPORAL VALUES: the dataset's, then random dates, times and instants in random calendars.
        for (var i = 0; i < 10 * scale; i++)
        {
            var date = new IsoDate(1800 + random.Next(400), 1 + random.Next(12), 1 + random.Next(28));
            var time = new IsoTime(random.Next(24), random.Next(60), random.Next(60), random.Next(1000), 0, 0);
            var calendar = random.Pick(new[] { "iso8601", "gregory", "japanese", "hebrew" });
            population.TemporalValues.Add(new TemporalInput(random.Pick(new[] { "date", "year-month", "month-day", "date-time" }), date, time, default, calendar));
            population.TemporalValues.Add(new TemporalInput("time", default, time, default, "iso8601"));
            population.TemporalValues.Add(new TemporalInput("instant", default, default, IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(date, time)), "iso8601"));
        }

        // THE TEMPORAL LIMITS, which HandleDateTimeValue does not clip (test262's temporal-objects-no-time-clip tests): the least
        // and greatest PlainDate, PlainDateTime and PlainYearMonth, in iso8601 and gregory, each alone and as a range.
        foreach (var calendar in new[] { "iso8601", "gregory" })
        {
            var limits = new (TemporalInput Min, TemporalInput Max)[]
            {
                (new TemporalInput("date", new IsoDate(-271821, 4, 19), default, default, calendar),
                    new TemporalInput("date", new IsoDate(275760, 9, 13), default, default, calendar)),
                (new TemporalInput("date-time", new IsoDate(-271821, 4, 19), new IsoTime(0, 0, 0, 0, 0, 1), default, calendar),
                    new TemporalInput("date-time", new IsoDate(275760, 9, 13), new IsoTime(23, 59, 59, 999, 999, 999), default, calendar)),
                (new TemporalInput("year-month", new IsoDate(-271821, 4, 1), default, default, calendar),
                    new TemporalInput("year-month", new IsoDate(275760, 9, 1), default, default, calendar)),
            };
            foreach (var (min, max) in limits)
            {
                population.TemporalValues.Add(min);
                population.TemporalValues.Add(max);
                population.TemporalRanges.Add((min, max));
            }
        }

        return population;
    }

    /// <summary>The <c>identifiers</c> of the time data's timezones dataset, <c>conformance/datasets/time/timezones.js</c>, in its order.</summary>
    internal static IReadOnlyList<string> TimeZoneDatasetNames()
    {
        var home = Path.Combine(Path.GetDirectoryName(CorpusDirectory.Home(AppContext.BaseDirectory))!, "datasets", "time", "timezones.js");
        var names = Strings(WithoutComments(File.ReadAllText(home)), "identifiers").ToList();
        if (names.Count == 0)
            throw new InvalidDataException(home + ": no identifiers");
        return names;
    }

    private static readonly double[] RangeSteps = [0, 1, 999, 60_000, 3_600_000, 86_400_000, 2_592_000_000, 31_536_000_000, 3_153_600_000_000];

    private static double RandomTime(SeededRandom random) => random.Next(10) switch
    {
        0 => (double)((long)(random.NextUInt64() % 17_280_000_000_000_001UL) - 8_640_000_000_000_000L),
        _ => (double)((long)(random.NextUInt64() % 12_622_780_800_000UL) - 2_208_988_800_000L),
    };

    private static readonly string[] Components = ["weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits", "timeZoneName"];

    private static FormatInput RandomFormat(SeededRandom random, IReadOnlyList<string> locales, IReadOnlyList<string> zones)
    {
        var locale = random.Pick(locales);
        locale += random.Next(6) switch
        {
            0 => "-u-ca-" + random.Pick(DateTimeFormat.CalendarKeyValues)!,
            1 => "-u-hc-" + random.Pick(new[] { "h11", "h12", "h23", "h24" }),
            2 => "-u-nu-" + random.Pick(new[] { "arab", "deva", "thai", "latn", "fullwide" }),
            _ => string.Empty,
        };

        var entries = new List<(string Key, object Value)>();
        if (random.Next(4) == 0)
        {
            if (random.Next(3) != 0)
                entries.Add(("dateStyle", random.Pick(StyleValues)));
            if (random.Next(3) != 0)
                entries.Add(("timeStyle", random.Pick(StyleValues)));
        }
        else
        {
            foreach (var component in Components)
            {
                if (random.Next(3) != 0)
                    continue;
                entries.Add((component, component == "fractionalSecondDigits"
                    ? (object)(double)(1 + random.Next(3))
                    : random.Pick(Admitted(component))));
            }
        }

        switch (random.Next(5))
        {
            case 0:
                entries.Add(("hourCycle", random.Pick(new[] { "h11", "h12", "h23", "h24" })));
                break;
            case 1:
                entries.Add(("hour12", random.Next(2) == 0));
                break;
        }

        if (random.Next(4) == 0)
            entries.Add(("calendar", random.Pick(DateTimeFormat.CalendarKeyValues)!));
        entries.Add(("timeZone", random.Next(4) switch
        {
            0 => "UTC",
            1 => random.Pick(new[] { "+05:30", "-08:00", "+00:00", "Etc/GMT+5", "Asia/Calcutta" }),
            _ => random.Pick(zones),
        }));
        return new FormatInput(locale, Bag([.. entries]));
    }

    private static string[] Admitted(string component) => component switch
    {
        "weekday" or "era" or "dayPeriod" => ["narrow", "short", "long"],
        "month" => ["2-digit", "numeric", "narrow", "short", "long"],
        "timeZoneName" => ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"],
        _ => ["2-digit", "numeric"],
    };

    // ---- the datasets' and vectors' forms ---------------------------------------------------------------------------------

    /// <summary>The temporal-intl program's values (its <c>values</c> object), as the program's constructors and <c>from</c> make them.</summary>
    private static IEnumerable<TemporalInput> TemporalDataset()
    {
        yield return new TemporalInput("date", new IsoDate(2024, 3, 5), default, default, "iso8601");
        yield return new TemporalInput("date", new IsoDate(2024, 3, 5), default, default, "gregory");
        yield return new TemporalInput("date", new IsoDate(-44, 3, 15), default, default, "gregory");
        yield return new TemporalInput("date-time", new IsoDate(2024, 3, 5), new IsoTime(13, 4, 5, 678, 0, 0), default, "iso8601");
        yield return new TemporalInput("time", default, new IsoTime(0, 30, 45, 123, 0, 0), default, "iso8601");
        yield return new TemporalInput("year-month", new IsoDate(2024, 3, 5), default, default, "gregory");
        yield return new TemporalInput("month-day", new IsoDate(1972, 3, 5), default, default, "gregory");
        var instant = IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(2024, 3, 5), new IsoTime(13, 4, 5, 678, 0, 0)));
        yield return new TemporalInput("instant", default, default, instant, "iso8601");
        yield return new TemporalInput("zoned", default, default, instant - (BigInteger)3_600_000_000_000, "iso8601", "Europe/Berlin");
        yield return new TemporalInput("year-month", new IsoDate(2024, 3, 1), default, default, "iso8601");
        yield return new TemporalInput("month-day", new IsoDate(1972, 3, 5), default, default, "iso8601");
    }

    /// <summary>
    /// Every <c>new Intl.DateTimeFormat(locale, bag)</c> a vector's script constructs, with the times its script writes: the
    /// literal Numbers, sums and <c>Date.UTC</c> calls it formats, and each range of two; and every <c>new Date(n).toLocale*String</c>,
    /// whose format is the method's.
    /// </summary>
    private static IEnumerable<(FormatInput Format, IReadOnlyList<double> Times, IReadOnlyList<(double Start, double End)> Ranges)> VectorFormats(string script)
    {
        var times = new List<double>();
        foreach (Match match in Regex.Matches(script, @"Date\.UTC\(([-0-9, ]+)\)"))
        {
            var fields = match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries).Select(static f => double.Parse(f, CultureInfo.InvariantCulture)).ToList();
            double Field(int i, double fallback) => i < fields.Count ? fields[i] : fallback;
            times.Add(EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(Field(0, 1970), Field(1, 0), Field(2, 1)),
                EcmaTime.MakeTime(Field(3, 0), Field(4, 0), Field(5, 0), Field(6, 0)))));
        }

        foreach (Match match in Regex.Matches(script, @"(?:format|formatToParts|formatRange|formatRangeToParts)\((-?[0-9.e]+)(?: \+ ([0-9]+))?(?:, (-?[0-9.e]+))?\)"))
        {
            var first = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) + (match.Groups[2].Success ? double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 0);
            times.Add(first);
            if (match.Groups[3].Success)
                times.Add(double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
        }

        foreach (Match match in Regex.Matches(script, @", (1[0-9]{12})\)$"))
            times.Add(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));

        var clipped = times.Where(static t => !double.IsNaN(EcmaTime.TimeClip(t))).Select(static t => EcmaTime.TimeClip(t)).ToList();
        var ranges = script.Contains(".formatRange", StringComparison.Ordinal) && clipped.Count >= 2
            ? new List<(double, double)> { (clipped[0], clipped[1]) }
            : [];

        foreach (var (format, at) in Constructors(script, "new Intl.DateTimeFormat("))
            yield return (format with { Required = "any", Defaults = "date" }, clipped, ranges);

        foreach (var (method, required, defaults) in new[] { ("toLocaleString", "any", "all"), ("toLocaleDateString", "date", "date"), ("toLocaleTimeString", "time", "time") })
        {
            foreach (var (format, _) in Constructors(script, ")." + method + "("))
            {
                var date = Regex.Match(script, @"new Date\(([0-9]+)\)");
                yield return (format with { Required = required, Defaults = defaults }, date.Success ? [double.Parse(date.Groups[1].Value, CultureInfo.InvariantCulture)] : [], []);
            }
        }
    }

    /// <summary>Each call at <paramref name="opening"/> whose arguments are a String locale, or none, and an optional bag of literals.</summary>
    private static IEnumerable<(FormatInput Format, int At)> Constructors(string script, string opening)
    {
        for (var at = script.IndexOf(opening, StringComparison.Ordinal); at >= 0; at = script.IndexOf(opening, at + 1, StringComparison.Ordinal))
        {
            var reader = new DecimalPopulation.LiteralReader(script, at + opening.Length);
            if (reader.Take(")"))
            {
                yield return (new FormatInput(null, OptionsBag.Empty), at);
                continue;
            }

            if (reader.Value() is not string locale)
                continue;
            var bag = OptionsBag.Empty;
            if (reader.Take(","))
            {
                var options = reader.Value();
                if (options is OptionsBag given)
                    bag = given;
                else if (options is not string)
                    continue;
            }

            if (reader.Take(")"))
                yield return (new FormatInput(locale, bag), at);
        }
    }

    /// <summary>A program without its line comments, which the literal reader does not skip: each <c>//</c> outside a string to its line's end.</summary>
    private static string WithoutComments(string program)
    {
        var text = new System.Text.StringBuilder(program.Length);
        char? quote = null;
        for (var i = 0; i < program.Length; i++)
        {
            var c = program[i];
            if (quote is null && c == '/' && i + 1 < program.Length && program[i + 1] == '/')
            {
                while (i < program.Length && program[i] != '\n')
                    i++;
                text.Append('\n');
                continue;
            }

            if (quote is null && c is '\'' or '"')
                quote = c;
            else if (quote == c && program[i - 1] != '\\')
                quote = null;
            text.Append(c);
        }

        return text.ToString();
    }

    private static IEnumerable<string> Strings(string program, string key) => DecimalPopulation.Array(program, key).OfType<string>();

    private static OptionsBag Bag(params (string Key, object Value)[] entries) => new([.. entries.Select(static e => new KeyValuePair<string, object>(e.Key, e.Value))]);

    private static OptionsBag With(OptionsBag bag, string key, string value) =>
        bag.Has(key) ? bag : new OptionsBag([.. bag.Entries, new KeyValuePair<string, object>(key, value)]);

    private static IsoDate IsoDateOf(string text) =>
        new(int.Parse(text.AsSpan(0, 4), CultureInfo.InvariantCulture), int.Parse(text.AsSpan(5, 2), CultureInfo.InvariantCulture), int.Parse(text.AsSpan(8, 2), CultureInfo.InvariantCulture));

    private static double Noon(IsoDate date) => EcmaTime.MakeDate(EcmaTime.MakeDay(date.Year, date.Month - 1, date.Day), EcmaTime.MakeTime(12, 0, 0, 0));

    /// <summary>Date.parse of the zone-names program's instants, <c>yyyy-MM-ddTHH:mmZ</c>.</summary>
    private static double Instant(string text)
    {
        var date = IsoDateOf(text);
        return EcmaTime.MakeDate(EcmaTime.MakeDay(date.Year, date.Month - 1, date.Day),
            EcmaTime.MakeTime(int.Parse(text.AsSpan(11, 2), CultureInfo.InvariantCulture), int.Parse(text.AsSpan(14, 2), CultureInfo.InvariantCulture), 0, 0));
    }
}
