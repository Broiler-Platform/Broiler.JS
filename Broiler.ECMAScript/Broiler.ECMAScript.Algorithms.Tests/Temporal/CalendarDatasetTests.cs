using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.CalendarTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// Broiler.VM's retained calendar dataset at library level (milestone SJL-10.3, part B): the lines of
/// <c>conformance/datasets/time/temporal-calendars.js</c> whose operations are the calendars' are computed with
/// <see cref="TemporalCalendars"/> and held to the dataset's expected answer, the reference polyfill's at
/// <c>e8cc03fc</c> with Broiler.VM's 70 named divergences applied, which bundle <c>sjl-10-001</c>'s rule takes as the
/// population. The program's lines are JavaScript; here each kind of line is rebuilt from the library's operations
/// as an engine's adapter would call them, and written in the string forms the program prints.
/// </summary>
/// <remarks>
/// Left to the durations sub-area (SJL-10.3, part C): the lines that round a difference to months and that round or
/// total a duration relative to a date, 7 to a calendar. Every other line is computed, and a line no pattern below
/// matches fails the test.
/// </remarks>
public class CalendarDatasetTests
{
    private static readonly IsoDate BaseDate = new(2023, 3, 15);

    private static TemporalCalendars Cal => Calendars;

    // The program's expected answers: the polyfill's, each line divergences.txt names replaced by Broiler.VM's.
    private static Dictionary<string, string> Expected()
    {
        var expected = Lines(DatasetText("temporal-calendars.polyfill-e8cc03fc.txt")).ToDictionary(static l => l.Label, static l => l.Answer, StringComparer.Ordinal);
        var divergences = Lines(DatasetText("temporal-calendars.divergences.txt"));
        Assert.Equal(70, divergences.Count);
        foreach (var (label, answer) in divergences)
        {
            Assert.True(expected.ContainsKey(label), label);
            expected[label] = answer;
        }

        Assert.Equal(3999, expected.Count);
        return expected;
    }

    // The answer the program prints for a refusal.
    private static string Thrown(CalendarStatus status) =>
        TemporalCalendars.IsTypeError(status) ? "throws TypeError" : "throws RangeError";

    private static string Date(CalendarStatus status, IsoDate date, CalendarId calendar) =>
        status == CalendarStatus.Completed ? DateString(date, calendar) : Thrown(status);

    // The base date's fields merged with a partial value, as with() merges them, then resolved under constrain.
    private static CalendarStatus With(CalendarId calendar, IsoDate date, CalendarFields partial, out IsoDate result)
    {
        Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, date, CalendarFieldsType.Date, out var fields));
        return Cal.DateFromFields(calendar, TemporalCalendars.MergeFields(calendar, fields, partial), CalendarOverflow.Constrain, out result);
    }

    // AddDurationToYearMonth's calendar steps (s9.5.8, steps 8 to 15): the month's first day, the years and months
    // added, and the year-month of the result.
    private static CalendarStatus AddToYearMonth(CalendarId calendar, IsoDate yearMonth, CalendarDuration duration, out IsoDate result)
    {
        result = default;
        Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, yearMonth, CalendarFieldsType.YearMonth, out var fields));
        var status = Cal.DateFromFields(calendar, fields with { Day = 1 }, CalendarOverflow.Constrain, out var first);
        if (status != CalendarStatus.Completed)
            return status;

        status = Cal.DateAdd(calendar, first, duration, CalendarOverflow.Constrain, out var added);
        if (status != CalendarStatus.Completed)
            return status;

        Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, added, CalendarFieldsType.YearMonth, out var addedFields));
        return Cal.YearMonthFromFields(calendar, addedFields, CalendarOverflow.Constrain, out result);
    }

    [Fact]
    public void The_retained_calendar_dataset_answers_as_Broiler_VM_at_library_level()
    {
        var expected = Expected();
        var fields = new Regex("^(?<cal>[a-z-]+) (?<iso>[-+]?\\d{4,6}-\\d\\d-\\d\\d)$");
        var add = new Regex("^(?<cal>[a-z-]+) add (?<dur>\\{[^}]*\\})(?<how>| to the 30th| reject from the 30th)$");
        var until = new Regex("^(?<cal>[a-z-]+) (?<op>until|since) (?<other>\\d{4}-\\d\\d-\\d\\d) (?<unit>years|months|weeks|days)$");
        var monthDay = new Regex("^(?<cal>[a-z-]+) month-day (?<code>M\\d\\dL?)-(?<day>\\d+)$");
        var line = new Regex("^(?<cal>[a-z-]+) (?<what>.+)$");

        var mismatches = new List<string>();
        var computed = 0;
        var leftToDurations = 0;

        foreach (var (label, answer) in expected)
        {
            string? mine;
            if (fields.Match(label) is { Success: true } f)
            {
                var calendar = Id(f.Groups["cal"].Value);
                var d = Parts(calendar, ParseIso(f.Groups["iso"].Value));
                string[] values =
                [
                    d.Era ?? string.Empty, d.EraYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Js(d.Year), Js(d.Month),
                    d.MonthCode.ToString(), Js(d.Day), Js(d.DayOfYear), Js(d.DaysInMonth), Js(d.DaysInYear), Js(d.MonthsInYear), Js(d.InLeapYear),
                ];
                mine = string.Join(',', values);
            }
            else if (add.Match(label) is { Success: true } a)
            {
                var calendar = Id(a.Groups["cal"].Value);
                var start = BaseDate;
                var how = a.Groups["how"].Value;
                if (how.Length > 0)
                    Assert.Equal(CalendarStatus.Completed, With(calendar, BaseDate, new CalendarFields { Day = 30 }, out start));

                var overflow = how == " reject from the 30th" ? CalendarOverflow.Reject : CalendarOverflow.Constrain;
                var status = Cal.DateAdd(calendar, start, ParseDuration(a.Groups["dur"].Value), overflow, out var result);
                mine = Date(status, result, calendar);
            }
            else if (until.Match(label) is { Success: true } u)
            {
                var calendar = Id(u.Groups["cal"].Value);
                var unit = u.Groups["unit"].Value switch
                {
                    "years" => CalendarUnit.Year,
                    "months" => CalendarUnit.Month,
                    "weeks" => CalendarUnit.Week,
                    _ => CalendarUnit.Day,
                };
                Assert.Equal(CalendarStatus.Completed, Cal.DateUntil(calendar, BaseDate, ParseIso(u.Groups["other"].Value), unit, out var difference));
                mine = DurationString(u.Groups["op"].Value == "since" ? Negated(difference) : difference);
            }
            else if (monthDay.Match(label) is { Success: true } md)
            {
                var calendar = Id(md.Groups["cal"].Value);
                MonthCode.Parse(md.Groups["code"].Value, out var code);
                var status = Cal.MonthDayFromFields(calendar, new CalendarFields { MonthCode = code, Day = double.Parse(md.Groups["day"].Value, CultureInfo.InvariantCulture) }, CalendarOverflow.Constrain, out var reference);
                mine = status == CalendarStatus.Completed ? MonthDayString(reference, calendar) : Thrown(status);
            }
            else
            {
                var m = line.Match(label);
                Assert.True(m.Success, label);
                mine = Other(Id(m.Groups["cal"].Value), m.Groups["what"].Value);
                if (mine is null)
                {
                    leftToDurations++;
                    continue;
                }
            }

            computed++;
            if (mine != answer)
                mismatches.Add(label + " => " + answer + "   library: " + mine);
        }

        Assert.True(mismatches.Count == 0, mismatches.Count + " of " + computed + " lines differ:\n" + string.Join('\n', mismatches.Take(40)));
        Assert.Equal(15 * 7, leftToDurations);
        Assert.Equal(3999 - (15 * 7), computed);
    }

    // The lines of one calendar's arithmetic that are not fields, additions, differences or month-days.
    private static string? Other(CalendarId calendar, string what)
    {
        var parts = Parts(calendar, BaseDate);
        var year = (double)parts.Year;
        CalendarStatus status;
        IsoDate result;

        CalendarStatus From(CalendarFields fields, CalendarOverflow overflow = CalendarOverflow.Constrain) =>
            Cal.DateFromFields(calendar, fields, overflow, out result);

        CalendarFields Code(string code, double day)
        {
            MonthCode.Parse(code, out var monthCode);
            return new CalendarFields { Year = year, MonthCode = monthCode, Day = day };
        }

        switch (what)
        {
            case "from year/month/day":
                return Date(From(new CalendarFields { Year = year, Month = 1, Day = 1 }), result, calendar);
            case "from monthCode M12 day 30":
                return Date(From(Code("M12", 30)), result, calendar);
            case "from month 13 constrain":
                return Date(From(new CalendarFields { Year = year, Month = 13, Day = 31 }), result, calendar);
            case "from month 13 reject":
                return Date(From(new CalendarFields { Year = year, Month = 13, Day = 1 }, CalendarOverflow.Reject), result, calendar);
            case "from day 31 reject":
                return Date(From(new CalendarFields { Year = year, Month = 2, Day = 31 }, CalendarOverflow.Reject), result, calendar);
            case "from M13":
                return Date(From(Code("M13", 1)), result, calendar);
            case "from M05L constrain":
                return Date(From(Code("M05L", 1)), result, calendar);
            case "from M05L reject":
                return Date(From(Code("M05L", 1), CalendarOverflow.Reject), result, calendar);
            case "from month and monthCode disagree":
                return Date(From(Code("M03", 1) with { Month = 2 }), result, calendar);
            case "from era":
                return Date(From(new CalendarFields { Era = parts.Era, EraYear = parts.EraYear, Month = 6, Day = 10 }), result, calendar);
            case "from era and wrong year":
                return Date(From(new CalendarFields { Era = parts.Era, EraYear = parts.EraYear, Year = year + 1, Month = 6, Day = 10 }), result, calendar);
            case "from era without eraYear":
                return Date(From(new CalendarFields { Era = parts.Era, Month = 6, Day = 10 }), result, calendar);
            case "from no year":
                return Date(From(new CalendarFields { Month = 6, Day = 10 }), result, calendar);
            case "with month 1":
                return Date(With(calendar, BaseDate, new CalendarFields { Month = 1 }, out result), result, calendar);
            case "with monthCode M01 day 29":
                return Date(With(calendar, BaseDate, new CalendarFields { MonthCode = new MonthCode(1, false), Day = 29 }, out result), result, calendar);
            case "with year":
                return Date(With(calendar, BaseDate, new CalendarFields { Year = year - 5 }, out result), result, calendar);
            case "toPlainYearMonth":
                status = Cal.DateToYearMonth(calendar, BaseDate, out result);
                return status == CalendarStatus.Completed ? YearMonthString(result, calendar) : Thrown(status);
            case "toPlainMonthDay":
                status = Cal.DateToMonthDay(calendar, BaseDate, out result);
                return status == CalendarStatus.Completed ? MonthDayString(result, calendar) : Thrown(status);
            case "year-month add":
            case "year-month subtract":
            {
                Assert.Equal(CalendarStatus.Completed, Cal.DateToYearMonth(calendar, BaseDate, out var yearMonth));
                var duration = what == "year-month add" ? new CalendarDuration(0, 7, 0, 0) : new CalendarDuration(-1, -1, 0, 0);
                status = AddToYearMonth(calendar, yearMonth, duration, out result);
                return status == CalendarStatus.Completed ? YearMonthString(result, calendar) : Thrown(status);
            }

            case "year-month until":
            {
                // DifferenceTemporalPlainYearMonth (s9.5.7): the first days of the two months, CalendarDateUntil in
                // years, and no rounding at its default smallest unit, the month.
                Assert.Equal(CalendarStatus.Completed, Cal.DateToYearMonth(calendar, BaseDate, out var one));
                Assert.Equal(CalendarStatus.Completed, Cal.DateToYearMonth(calendar, new IsoDate(2031, 1, 10), out var two));
                Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, one, CalendarFieldsType.YearMonth, out var oneFields));
                Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, two, CalendarFieldsType.YearMonth, out var twoFields));
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, oneFields with { Day = 1 }, CalendarOverflow.Constrain, out var oneDate));
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, twoFields with { Day = 1 }, CalendarOverflow.Constrain, out var twoDate));
                Assert.Equal(CalendarStatus.Completed, Cal.DateUntil(calendar, oneDate, twoDate, CalendarUnit.Year, out var difference));
                return DurationString(difference);
            }

            case "year-month from":
                status = Cal.YearMonthFromFields(calendar, new CalendarFields { Year = year, Month = 2 }, CalendarOverflow.Constrain, out result);
                return status == CalendarStatus.Completed ? YearMonthString(result, calendar) : Thrown(status);
            case "zoned add month":
                // AddZonedDateTime (s6.5.5) in UTC: the calendar adds the month to the date, and midnight stays midnight.
                status = Cal.DateAdd(calendar, BaseDate, new CalendarDuration(0, 1, 0, 0), CalendarOverflow.Constrain, out result);
                return status == CalendarStatus.Completed ? Iso(result) + "T00:00:00+00:00[UTC]" + Annotation(calendar, "auto") : Thrown(status);
            case "month-day M02-31 reject":
                status = Cal.MonthDayFromFields(calendar, new CalendarFields { MonthCode = new MonthCode(2, false), Day = 31 }, CalendarOverflow.Reject, out result);
                return status == CalendarStatus.Completed ? MonthDayString(result, calendar) : Thrown(status);
            case "month-day month without year":
                status = Cal.MonthDayFromFields(calendar, new CalendarFields { Month = 2, Day = 1 }, CalendarOverflow.Constrain, out result);
                return status == CalendarStatus.Completed ? MonthDayString(result, calendar) : Thrown(status);
            case "month-day to date":
            {
                Assert.Equal(CalendarStatus.Completed, Cal.MonthDayFromFields(calendar, new CalendarFields { MonthCode = new MonthCode(2, false), Day = 10 }, CalendarOverflow.Constrain, out var monthDay));
                status = Cal.MonthDayToDate(calendar, monthDay, new CalendarFields { Year = Parts(calendar, new IsoDate(2001, 6, 1)).Year }, out result);
                return Date(status, result, calendar);
            }

            default:
                // THE DURATIONS SUB-AREA'S: a difference rounded to months, and a duration rounded or totalled
                // relative to the date.
                Assert.True(what.EndsWith(" round months", StringComparison.Ordinal) || what is "duration round relativeTo" or "duration total relativeTo", what);
                return null;
        }
    }
}
