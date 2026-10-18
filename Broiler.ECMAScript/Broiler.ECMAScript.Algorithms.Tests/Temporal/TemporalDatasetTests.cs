using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.CalendarTestSupport;
using static Broiler.ECMAScript.Algorithms.Tests.DurationTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// Broiler.VM's retained Temporal dataset at library level (milestone SJL-10.3, part C): every line of
/// <c>conformance/datasets/time/temporal.js</c> is computed through the runner's Temporal adapter, the engine-side steps
/// around the library's durations, differences, rounding, calendars, grammar and zones, and held to the dataset's
/// expected answer, the reference polyfill's at <c>e8cc03fc</c>, from which Broiler.VM diverges on no line.
/// </summary>
/// <remarks>
/// The program's loops are rebuilt here line for line, each line written as the program's <c>String(value)</c> prints
/// it, or <c>throws</c> and the error's name. A line the program prints that this test does not compute, or one it
/// computes that the program does not print, fails the test.
/// </remarks>
public class TemporalDatasetTests
{
    private static readonly string[] Modes = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"];

    private static readonly string[] Units = ["year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond"];

    private static TemporalAdapter A => Adapter;

    // The program's String(value).
    private static string Str(object? value) => value switch
    {
        null => "undefined",
        TemporalAdapter.JsNull => "null",
        double d => d == 0 ? "0" : NumberText.ToString(d),
        int i => i.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        BigInteger i => i.ToString(CultureInfo.InvariantCulture),
        string s => s,
        _ => A.Render(value),
    };

    // The program's line(): the value's string, or "throws" and the error's name.
    private static string Answer(Func<object?> f)
    {
        try
        {
            return Str(f());
        }
        catch (TemporalAdapter.RangeError)
        {
            return "throws RangeError";
        }
        catch (TemporalAdapter.TypeError)
        {
            return "throws TypeError";
        }
    }

    private static Dictionary<string, Func<object?>> Program()
    {
        var lines = new Dictionary<string, Func<object?>>(StringComparer.Ordinal);
        void Line(string label, Func<object?> f) => lines.Add(label, f);

        // ---- Duration --------------------------------------------------------------------------------------------
        string[] durations =
        [
            "P1Y2M3W4DT5H6M7.008009010S", "-P1Y6M", "PT49H", "P45D", "PT0.000000001S", "-PT36H30M",
            "P2Y11M30DT23H59M59.999999999S", "PT90061.5S", "P1W", "-P400D",
        ];
        foreach (var text in durations)
        {
            var d = A.ToDuration(text);
            Line("duration " + text + " sign/blank", () => DurationMath.Sign(d.Fields) + "/" + Str(DurationMath.Sign(d.Fields) == 0));
            Line("duration " + text + " negated abs", () =>
                A.Render(new TemporalAdapter.Duration(DurationMath.Negate(d.Fields))) + " " + A.Render(new TemporalAdapter.Duration(DurationMath.Abs(d.Fields))));
            foreach (var rel in new[] { "2020-01-31", "2019-02-28", "2024-02-29T12:00[America/New_York]", "2021-03-28T01:30[Europe/Berlin]" })
            {
                foreach (var largest in new[] { "year", "month", "week", "day", "hour" })
                    Line("duration " + text + " round largest " + largest + " rel " + rel, () => A.Round(d, Bag(("largestUnit", largest), ("relativeTo", rel))));
                foreach (var unit in new[] { "year", "month", "week", "day", "hour", "second" })
                    Line("duration " + text + " total " + unit + " rel " + rel, () => A.Total(d, Bag(("unit", unit), ("relativeTo", rel))));
                foreach (var mode in Modes)
                {
                    Line("duration " + text + " round month " + mode + " rel " + rel, () =>
                        A.Round(d, Bag(("smallestUnit", "month"), ("roundingMode", mode), ("relativeTo", rel))));
                }
            }

            Line("duration " + text + " round day no rel", () => A.Round(d, Bag(("largestUnit", "day"), ("smallestUnit", "hour"), ("roundingIncrement", 3.0))));
            Line("duration " + text + " toString 2 digits", () => A.DurationToString(d, Bag(("fractionalSecondDigits", 2.0), ("roundingMode", "halfExpand"))));
            Line("duration " + text + " add PT23H", () => A.AddDurations(d, "PT23H", subtract: false));
        }

        Line("duration compare months rel", () =>
            Str(A.Compare(Bag(("months", 1.0)), Bag(("days", 30.0)), Bag(("relativeTo", "2021-02-01")))) + " " +
            Str(A.Compare(Bag(("months", 1.0)), Bag(("days", 30.0)), Bag(("relativeTo", "2021-01-01")))));
        Line("duration invalid mixed signs", () => TemporalAdapter.NewDuration(1.0, -1.0));
        Line("duration max seconds", () => A.Render(A.ToDuration(Bag(("seconds", 9007199254740991.0)))));
        Line("duration too large", () => A.ToDuration(Bag(("seconds", 9007199254740991.0 + 1))));

        // ---- PlainDate -------------------------------------------------------------------------------------------
        foreach (var text in new[] { "2020-02-29", "2021-01-31", "1999-12-31", "-000001-03-01", "+275760-09-13", "1970-01-01" })
        {
            var date = A.ToPlainDate(text);
            Line("date " + text + " fields", () =>
            {
                var f = A.CalendarFieldsOf(date.Date, date.Calendar);
                return string.Join(',', Js(f.Year), Js(f.Month), f.MonthCode.ToString(), Js(f.Day), Js(f.DayOfWeek), Js(f.DayOfYear),
                    f.WeekOfYear is { } w ? Js(w) : string.Empty, f.YearOfWeek is { } y ? Js(y) : string.Empty, Js(f.DaysInMonth), Js(f.DaysInYear), Js(f.InLeapYear));
            });
            foreach (var d in new[] { "P1M", "P1Y", "-P1M", "P1Y1M1D", "P10W", "-P400D" })
            {
                Line("date " + text + " add " + d, () => A.Add(date, d, null, subtract: false));
                Line("date " + text + " add " + d + " reject", () => A.Add(date, d, Bag(("overflow", "reject")), subtract: false));
            }

            foreach (var other in new[] { "2020-01-31", "2023-03-01", "2000-02-29" })
            {
                foreach (var largest in new[] { "year", "month", "week", "day" })
                {
                    Line("date " + text + " until " + other + " " + largest, () => A.Difference(DifferenceOperation.Until, date, other, Bag(("largestUnit", largest))));
                    Line("date " + text + " since " + other + " " + largest + " round month", () => A.Difference(DifferenceOperation.Since, date, other,
                        Bag(("largestUnit", largest is "week" or "day" ? "year" : largest), ("smallestUnit", "month"), ("roundingMode", "halfEven"))));
                }
            }

            Line("date " + text + " gregory", () =>
            {
                var g = TemporalAdapter.WithCalendar(date, "gregory");
                var f = A.CalendarFieldsOf(g.Date, g.Calendar);
                return (f.Era ?? "undefined") + " " + Js(f.EraYear) + " " + A.Render(g) + " " + Js(f.WeekOfYear);
            });
            Line("date " + text + " toZonedDateTime New York", () => A.ToZonedDateTime(date, "America/New_York"));
        }

        Line("date from fields constrain", () => A.ToPlainDate(Bag(("year", 2021.0), ("month", 2.0), ("day", 31.0))));
        Line("date from fields reject", () => A.ToPlainDate(Bag(("year", 2021.0), ("month", 2.0), ("day", 31.0)), Bag(("overflow", "reject"))));
        Line("date from monthCode", () => A.ToPlainDate(Bag(("year", 2021.0), ("monthCode", "M12"), ("day", 25.0))));
        Line("date from era", () => A.ToPlainDate(Bag(("era", "bce"), ("eraYear", 10.0), ("month", 1.0), ("day", 1.0), ("calendar", "gregory"))));
        Line("date with", () => A.With(A.ToPlainDate("2021-03-31"), Bag(("month", 4.0)), null));
        Line("date string with calendar", () => A.ToString(A.ToPlainDate("2021-03-31[u-ca=gregory]"), Bag(("calendarName", "always"))));
        Line("date string bad", () => A.ToPlainDate("2021-13-01"));

        // ---- PlainTime and PlainDateTime -------------------------------------------------------------------------
        var time = A.ToPlainTime("13:45:30.123456789");
        foreach (var unit in Units.Skip(4))
        {
            foreach (var mode in Modes)
            {
                Line("time round " + unit + " " + mode, () =>
                    A.Round(time, Bag(("smallestUnit", unit), ("roundingMode", mode), ("roundingIncrement", unit == "hour" ? 6.0 : 1.0))));
            }

            Line("time until 02:00 " + unit, () => A.Difference(DifferenceOperation.Until, time, "02:00", Bag(("largestUnit", unit))));
        }

        var dateTime = A.ToPlainDateTime("2021-12-31T23:59:59.999999999");
        foreach (var unit in new[] { "day", "hour", "minute", "second", "millisecond" })
            Line("datetime round " + unit, () => A.Round(dateTime, unit));
        foreach (var largest in new[] { "year", "month", "week", "day", "hour" })
            Line("datetime until 2020-02-29T12:00 " + largest, () => A.Difference(DifferenceOperation.Until, dateTime, "2020-02-29T12:00", Bag(("largestUnit", largest))));
        Line("datetime toString digits", () =>
            A.ToString(dateTime, Bag(("fractionalSecondDigits", 4.0))) + " " + A.ToString(dateTime, Bag(("smallestUnit", "minute"))));

        // ---- ZonedDateTime ---------------------------------------------------------------------------------------
        string[] zones = ["America/New_York", "Europe/Berlin", "Australia/Sydney", "Asia/Kolkata", "Pacific/Apia", "+05:30", "UTC"];
        string[] instants = ["2011-12-29T12:00Z", "2021-03-14T06:30Z", "2021-03-28T00:30Z", "2021-10-03T15:59:59Z", "2021-11-07T05:30Z"];
        foreach (var zone in zones)
        {
            foreach (var text in instants)
            {
                var zoned = A.ToZonedDateTimeIso(A.ToInstant(text), zone);
                var prefix = "zoned " + zone + " " + text;
                Line(prefix, () => A.Render(zoned) + " " + Str(A.HoursInDay(zoned)) + " " + Str((double)A.OffsetNanosecondsFor(zoned.TimeZone, zoned.EpochNanoseconds)));
                Line(prefix + " startOfDay", () => A.StartOfDay(zoned));
                Line(prefix + " add P1D PT24H", () => A.Render(A.Add(zoned, "P1D", null, subtract: false)) + " " + A.Render(A.Add(zoned, "PT24H", null, subtract: false)));
                Line(prefix + " round day", () => A.Round(zoned, Bag(("smallestUnit", "day"), ("roundingMode", "halfExpand"))));
                Line(prefix + " next", () => (object?)A.Transition(zoned, "next") ?? TemporalAdapter.JsNull.Value);
                Line(prefix + " previous", () => (object?)A.Transition(zoned, "previous") ?? TemporalAdapter.JsNull.Value);
                Line(prefix + " until +P1M", () => A.Difference(DifferenceOperation.Until, zoned, A.Add(zoned, Bag(("months", 1.0), ("hours", 5.0)), null, subtract: false),
                    Bag(("largestUnit", "month"), ("smallestUnit", "hour"), ("roundingMode", "halfExpand"))));
            }

            foreach (var wall in new[] { "2021-03-14T02:30", "2021-11-07T01:30", "2021-03-28T02:30", "2021-10-03T02:30", "2011-12-30T12:00" })
            {
                foreach (var d in new[] { "compatible", "earlier", "later", "reject" })
                    Line("wall " + wall + " in " + zone + " " + d, () => A.ToZonedDateTime(A.ToPlainDateTime(wall), zone, Bag(("disambiguation", d))));
            }
        }

        Line("zoned from offset prefer", () => A.ToZonedDateTime("2021-11-07T01:30-04:00[America/New_York]"));
        Line("zoned from offset mismatch", () => A.ToZonedDateTime("2021-07-01T12:00+01:00[America/New_York]"));
        Line("zoned from offset ignore", () => A.ToZonedDateTime("2021-07-01T12:00+01:00[America/New_York]", Bag(("offset", "ignore"))));
        Line("zoned with keeps offset", () => A.With(A.ToZonedDateTime("2021-11-07T01:30-05:00[America/New_York]"), Bag(("minute", 45.0)), null));
        Line("zoned toString options", () =>
        {
            var z = A.ToZonedDateTime("2021-11-07T01:30:00.5-05:00[America/New_York]");
            return A.ToString(z, Bag(("timeZoneName", "never"), ("offset", "never"), ("smallestUnit", "second"), ("roundingMode", "ceil"))) + " " +
                A.ToString(z, Bag(("timeZoneName", "critical"), ("calendarName", "always")));
        });

        // ---- Instant ---------------------------------------------------------------------------------------------
        var instant = A.ToInstant("1969-12-31T23:59:59.999999999Z");
        foreach (var unit in new[] { "hour", "minute", "second", "millisecond" })
        {
            foreach (var mode in Modes)
                Line("instant round " + unit + " " + mode, () => A.Round(instant, Bag(("smallestUnit", unit), ("roundingMode", mode))));
        }

        foreach (var text in new[] { "2001-09-09T01:46:40.123987500Z", "2001-09-09T01:46:40.123988500Z", "-000001-01-01T00:00:00.5Z" })
        {
            foreach (var mode in Modes)
            {
                Line("instant tie " + text + " " + mode, () =>
                {
                    var tie = A.ToInstant(text);
                    return A.Render(A.Round(tie, Bag(("smallestUnit", "microsecond"), ("roundingMode", mode)))) + " " +
                        A.ToString(tie, Bag(("smallestUnit", "second"), ("roundingMode", mode)));
                });
            }
        }

        Line("instant epoch", () =>
            Str((double)BigInteger.Divide(instant.EpochNanoseconds - Mod(instant.EpochNanoseconds, 1_000_000), 1_000_000)) + " " + Str(instant.EpochNanoseconds));
        Line("instant toString zone", () => A.ToString(instant, Bag(("timeZone", "Asia/Kolkata"))));
        Line("instant until", () => A.Difference(DifferenceOperation.Until, instant, "2021-01-01T00:00Z", Bag(("largestUnit", "hour"), ("smallestUnit", "minute"))));
        Line("instant from offset", () => A.ToInstant("2021-01-01T00:00+05:30[Asia/Kolkata]"));
        Line("instant out of range", () =>
        {
            // Temporal.Instant.fromEpochMilliseconds: NumberToBigInt, then the epoch's limit.
            var epoch = new BigInteger(8.64e15 + 1) * 1_000_000;
            TemporalAdapter.Check(TemporalLimits.CheckEpochNanoseconds(epoch));
            return new TemporalAdapter.Instant(epoch);
        });

        // ---- PlainYearMonth and PlainMonthDay --------------------------------------------------------------------
        Line("yearmonth add", () => A.Add(A.ToPlainYearMonth("2021-01"), Bag(("months", 13.0)), null, subtract: false));
        Line("yearmonth until", () => A.Difference(DifferenceOperation.Until, A.ToPlainYearMonth("2019-07"), "2021-03", Bag(("largestUnit", "year"))));
        Line("yearmonth fields", () =>
        {
            var ym = A.ToPlainYearMonth("2024-02");
            var f = A.CalendarFieldsOf(ym.Date, ym.Calendar);
            return Js(f.DaysInMonth) + " " + Js(f.InLeapYear) + " " + f.MonthCode;
        });
        Line("monthday leap", () => A.Render(A.ToPlainMonthDay("02-29")) + " " + A.Render(A.ToPlainMonthDay(Bag(("month", 2.0), ("day", 30.0)))));
        Line("monthday toPlainDate", () => A.MonthDayToPlainDate(A.ToPlainMonthDay("02-29"), Bag(("year", 2021.0))));

        return lines;
    }

    private static BigInteger Mod(BigInteger value, long divisor)
    {
        var remainder = BigInteger.Remainder(value, divisor);
        return remainder.Sign < 0 ? remainder + divisor : remainder;
    }

    [Fact]
    public void The_retained_Temporal_dataset_answers_as_Broiler_VM_at_library_level()
    {
        var expected = Lines(DatasetText("temporal.polyfill-e8cc03fc.txt"));
        var divergences = Lines(DatasetText("temporal.divergences.txt"));
        Assert.Empty(divergences);
        Assert.Equal(1629, expected.Count);

        var program = Program();
        Assert.Equal(expected.Select(static l => l.Label), program.Keys);

        var mismatches = new List<string>();
        foreach (var (label, answer) in expected)
        {
            var mine = Answer(program[label]);
            if (mine != answer)
                mismatches.Add(label + " => " + answer + "   library: " + mine);
        }

        Assert.True(mismatches.Count == 0, mismatches.Count + " of " + expected.Count + " lines differ:\n" + string.Join('\n', mismatches.Take(40)));
    }
}
