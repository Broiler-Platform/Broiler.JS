using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The calendar table's parse, read back against years whose dates are well known (Broiler.VM's rule N32),
/// and the hand-written row readers held to the regular expressions Broiler.VM's generator matched the
/// same rows with, over the pinned sources and over sources changed to break them.
/// </summary>
public sealed partial class CalendarTableTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> Members = new(static () =>
    {
        var pin = TimeDataHome.ReadCalendarPin(TimeDataHome.Current);
        return pin.Extract(TimeDataHome.Crates(TimeDataHome.Current, pin));
    });

    private static string Source(string member) => CalendarPin.Decode(Members.Value[member]);

    /// <summary>
    /// Chinese New Year 2024 on 10 February, the year 2012 whose leap month China places after the fourth
    /// month and Korea after the third, the Qing year 1900's leap eighth month, 1 Muharram 1445 on 19 July
    /// 2023, and the first year the Persian 33-year rule is corrected.
    /// </summary>
    [Fact]
    public void The_parse_reads_known_years()
    {
        var china = CalendarTableGenerator.EastAsianYears(Source(CalendarTableGenerator.China), "china");
        var korea = CalendarTableGenerator.EastAsianYears(Source(CalendarTableGenerator.Korea), "korea");
        var qing = CalendarTableGenerator.EastAsianYears(Source(CalendarTableGenerator.Qing), "qing");
        var ummAlQura = CalendarTableGenerator.HijriYears(Source(CalendarTableGenerator.UmmAlQura));
        var persian = CalendarTableGenerator.PersianCorrections(Source(CalendarTableGenerator.Persian));

        Assert.Equal((1912, 2102), (china[0].Year, china[^1].Year));
        Assert.Equal((1912, 2102), (korea[0].Year, korea[^1].Year));
        Assert.Equal((1900, 1911), (qing[0].Year, qing[^1].Year));
        Assert.Equal((1300, 1600), (ummAlQura[0].Year, ummAlQura[^1].Year));

        Assert.Equal(TzdbCompiler.DaysFromCivil(2024, 2, 10), china.Single(static year => year.Year == 2024).NewYear);
        Assert.Equal(5, china.Single(static year => year.Year == 2012).LeapMonth);
        Assert.Equal(4, korea.Single(static year => year.Year == 2012).LeapMonth);
        Assert.Equal(9, qing[0].LeapMonth);
        Assert.Equal(TzdbCompiler.DaysFromCivil(2023, 7, 19), ummAlQura.Single(static year => year.Year == 1445).NewYear);
        Assert.Equal(78, persian.Count);
        Assert.Equal(1502, persian[0]);
        Assert.Equal(2987, persian[^1]);
    }

    /// <summary>The hand-written readers answer what Broiler.VM's regular expressions answered, over every pinned source.</summary>
    [Fact]
    public void The_readers_agree_with_Broiler_VM_s_expressions_over_the_pinned_sources()
    {
        foreach (var file in new[] { CalendarTableGenerator.China, CalendarTableGenerator.Korea, CalendarTableGenerator.Qing })
            Assert.Equal(Reference.EastAsianYears(Source(file), file), Shape(CalendarTableGenerator.EastAsianYears(Source(file), file)));

        Assert.Equal(Reference.HijriYears(Source(CalendarTableGenerator.UmmAlQura)), Shape(CalendarTableGenerator.HijriYears(Source(CalendarTableGenerator.UmmAlQura))));
        Assert.Equal(Reference.PersianCorrections(Source(CalendarTableGenerator.Persian)), CalendarTableGenerator.PersianCorrections(Source(CalendarTableGenerator.Persian)));
    }

    /// <summary>
    /// Over sources changed to break them - a row the expression does not match and skips, a gap in the
    /// years, a leap month the layout cannot state, a malformed length, a source with no rows, a correction
    /// array out of order - the readers answer as the expressions did, or refuse with the same message.
    /// </summary>
    [Fact]
    public void The_readers_agree_with_Broiler_VM_s_expressions_over_broken_sources()
    {
        var china = Source(CalendarTableGenerator.China);
        var row = "PackedEastAsianTraditionalYearData::new(1913, ";
        var at = china.IndexOf(row, StringComparison.Ordinal);
        Assert.True(at > 0);

        string[] eastAsian =
        [
            china.Replace(row, "PackedEastAsianTraditionalYearData::new(1913,  ", StringComparison.Ordinal),
            china.Replace(row, "PackedEastAsianTraditionalYearData::new(1999, ", StringComparison.Ordinal),
            china.Remove(at, row.Length).Insert(at, "PackedEastAsianTraditionalYearData::new(1913, [l, l, l, l, l, l, l, l, l, l, l, l, x], None, gregorian(1913, 2, 6)), "),
            china.Replace("Some(", "Some(1", StringComparison.Ordinal),
            china.Replace("pub const STARTING_YEAR", "pub const FIRST_YEAR", StringComparison.Ordinal),
            "pub const STARTING_YEAR: i32 = 1912;\n",
            china.Replace(", None, ", ", Nothing, ", StringComparison.Ordinal),
        ];

        foreach (var source in eastAsian)
            Assert.Equal(Outcome(() => Reference.EastAsianYears(source, "china")), Outcome(() => Shape(CalendarTableGenerator.EastAsianYears(source, "china"))));

        var hijri = Source(CalendarTableGenerator.UmmAlQura);
        string[] broken =
        [
            hijri.Replace(".unwrap()", ".expect()", StringComparison.Ordinal),
            hijri.Replace("PackedHijriYearData::try_new(1301, ", "PackedHijriYearData::try_new(1302, ", StringComparison.Ordinal),
            hijri.Replace("[l, ", "[l,  s, ", StringComparison.Ordinal),
        ];

        foreach (var source in broken)
            Assert.Equal(Outcome(() => Reference.HijriYears(source)), Outcome(() => Shape(CalendarTableGenerator.HijriYears(source))));

        var persian = Source(CalendarTableGenerator.Persian);
        string[] corrections =
        [
            persian.Replace("NON_LEAP_CORRECTION: [i32; 78]", "NON_LEAP_CORRECTION: [i32; 77]", StringComparison.Ordinal),
            persian.Replace("1502,", "1503,", StringComparison.Ordinal),
            persian.Replace("const NON_LEAP_CORRECTION", "const LEAP_CORRECTION", StringComparison.Ordinal),
            persian.Replace("1502,", "1502 ,\t", StringComparison.Ordinal),
        ];

        foreach (var source in corrections)
            Assert.Equal(Outcome(() => Reference.PersianCorrections(source)), Outcome(() => CalendarTableGenerator.PersianCorrections(source)));
    }

    private static string Outcome<T>(Func<T> read)
    {
        try
        {
            var value = read();
            return "value: " + (value is System.Collections.IEnumerable items and not string ? string.Join(";", items.Cast<object>()) : value?.ToString());
        }
        catch (InvalidDataException refused)
        {
            return "refused: " + refused.Message;
        }
    }

    private static IReadOnlyList<string> Shape(IReadOnlyList<CalendarTableGenerator.EastAsianYear> years) =>
        years.Select(static y => Line(y.Year, y.Long, y.LeapMonth, y.NewYear)).ToList();

    private static IReadOnlyList<string> Shape(IReadOnlyList<CalendarTableGenerator.HijriYear> years) =>
        years.Select(static y => Line(y.Year, y.Long, 0, y.NewYear)).ToList();

    private static string Line(int year, bool[] lengths, int leap, long newYear) =>
        string.Create(CultureInfo.InvariantCulture, $"{year} {string.Concat(lengths.Select(static l => l ? 'l' : 's'))} {leap} {newYear}");

    /// <summary>
    /// Broiler.VM's <c>CalendarTableGenerator</c> readers at Broiler.VM <c>9508bd5</c>, verbatim but for
    /// the shape of their answers: the reference the hand-written readers are held to.
    /// </summary>
    private static partial class Reference
    {
        internal static IReadOnlyList<string> EastAsianYears(string source, string file)
        {
            var start = StartingYear(source, file);
            var years = new List<string>();

            foreach (Match match in EastAsianRow().Matches(source))
            {
                var year = Int(match.Groups[1].Value);
                var lengths = Lengths(match.Groups[2].Value, 13, file);
                var leap = match.Groups[4].Success ? Int(match.Groups[4].Value) : 0;
                var newYear = TzdbCompiler.DaysFromCivil(Int(match.Groups[5].Value), Int(match.Groups[6].Value), Int(match.Groups[7].Value));

                if (year != start + years.Count)
                    throw new InvalidDataException($"{file}: the year {year} does not follow {start + years.Count - 1}");

                if (leap is 1 or > 13 || (leap == 0 && lengths[12]))
                    throw new InvalidDataException($"{file}: the year {year} has a leap month {leap} the layout cannot state");

                years.Add(Line(year, lengths, leap, newYear));
            }

            if (years.Count == 0)
                throw new InvalidDataException($"{file} states no years");

            return years;
        }

        internal static IReadOnlyList<string> HijriYears(string source)
        {
            var start = StartingYear(source, CalendarTableGenerator.UmmAlQura);
            var years = new List<string>();

            foreach (Match match in HijriRow().Matches(source))
            {
                var year = Int(match.Groups[1].Value);

                if (year != start + years.Count)
                    throw new InvalidDataException($"{CalendarTableGenerator.UmmAlQura}: the year {year} does not follow {start + years.Count - 1}");

                years.Add(Line(
                    year,
                    Lengths(match.Groups[2].Value, 12, CalendarTableGenerator.UmmAlQura),
                    0,
                    TzdbCompiler.DaysFromCivil(Int(match.Groups[3].Value), Int(match.Groups[4].Value), Int(match.Groups[5].Value))));
            }

            if (years.Count == 0)
                throw new InvalidDataException($"{CalendarTableGenerator.UmmAlQura} states no years");

            return years;
        }

        internal static IReadOnlyList<int> PersianCorrections(string source)
        {
            var match = PersianTable().Match(source);

            if (!match.Success)
                throw new InvalidDataException($"{CalendarTableGenerator.Persian} has no NON_LEAP_CORRECTION array");

            var years = match.Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Int).ToList();

            if (years.Count != Int(match.Groups[1].Value) || years.Zip(years.Skip(1)).Any(static pair => pair.First >= pair.Second))
                throw new InvalidDataException($"{CalendarTableGenerator.Persian}: NON_LEAP_CORRECTION is not {match.Groups[1].Value} ascending years");

            return years;
        }

        private static int StartingYear(string source, string file)
        {
            var match = StartingYearLine().Match(source);
            return match.Success ? Int(match.Groups[1].Value) : throw new InvalidDataException($"{file} states no STARTING_YEAR");
        }

        private static bool[] Lengths(string list, int count, string file)
        {
            var lengths = list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(length => length switch
                {
                    "l" => true,
                    "s" => false,
                    _ => throw new InvalidDataException($"{file}: `{length}` is not a month length"),
                })
                .ToArray();

            return lengths.Length == count ? lengths : throw new InvalidDataException($"{file}: a year states {lengths.Length} month lengths, not {count}");
        }

        private static int Int(string text) => int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

        [GeneratedRegex(@"PackedEastAsianTraditionalYearData::new\((\d+), \[([ls, ]+)\], (None|Some\((\d+)\)), gregorian\((\d+), (\d+), (\d+)\)\)")]
        private static partial Regex EastAsianRow();

        [GeneratedRegex(@"PackedHijriYearData::try_new\((\d+), \[([ls, ]+)\], gregorian\((\d+), (\d+), (\d+)\)\)\.unwrap\(\)")]
        private static partial Regex HijriRow();

        [GeneratedRegex(@"const NON_LEAP_CORRECTION: \[i32; (\d+)\] = \[([0-9,\s]+)\];")]
        private static partial Regex PersianTable();

        [GeneratedRegex(@"pub const STARTING_YEAR: i32 = (\d+);")]
        private static partial Regex StartingYearLine();
    }
}
