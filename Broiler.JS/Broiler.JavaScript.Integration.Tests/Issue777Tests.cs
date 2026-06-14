using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/777
//
// Fixed here:
//   * Problem 1 — the Indian national (Saka) calendar (Gregorian-anchored solar arithmetic, single
//     "shaka" era) for Temporal.PlainDate / PlainDateTime / PlainYearMonth.
//   * Problems 3/4/6/7/9 — the non-ISO calendars (chinese, hebrew, dangi, islamic-civil, ethiopic,
//     …) were rejected by Temporal.PlainYearMonth at Canonicalize; PlainYearMonth now supports them
//     (accessors / from / with / add / subtract / since / until / toPlainDate / parsing), sharing
//     the calendar-date math with PlainDate / PlainDateTime via TemporalNonIso.
//   * Problem 2 — RangeError validation for options that previously passed silently:
//     PlainTime.round (roundingIncrement max / divisor, roundingMode), PlainTime.since/until
//     (largestUnit/smallestUnit time-unit + mismatch, roundingIncrement, roundingMode), and
//     PlainDateTime.toString (fractionalSecondDigits, smallestUnit, roundingMode), plus the
//     fractionalSecondDigits / smallestUnit precision formatting.
//   * Problems 5/8 — Temporal.ZonedDateTime.prototype.add / subtract (DST-aware): the date part is
//     added to the wall clock (a calendar day absorbs DST transitions) and the time part is added on
//     the exact epoch-nanosecond timeline.
//
// Out of scope: Temporal.ZonedDateTime round / with, and difference (until/since) rounding.
public class Issue777Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string ErrorName(string code)
        => Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t");

    // ─────────────────────────── indian calendar (Problem 1) ───────────────────────────

    [Fact] // 1 Chaitra 1879 Saka = ISO 1957-03-22 (the national-calendar reform date; 1957 common)
    public void IndianReformDate()
        => Assert.Equal("1957-03-22",
            Eval("Temporal.PlainDate.from({year:1879, month:1, day:1, calendar:'indian'}).withCalendar('iso8601').toString()"));

    [Fact] // 1 Chaitra 1922 Saka = ISO 2000-03-21 (2000 is a leap year, so Chaitra starts a day early)
    public void IndianLeapYearStart()
        => Assert.Equal("2000-03-21",
            Eval("Temporal.PlainDate.from({year:1922, month:1, day:1, calendar:'indian'}).withCalendar('iso8601').toString()"));

    [Fact] // ISO → indian fields: single "shaka" era, eraYear == year
    public void IndianFieldsFromIso()
        => Assert.Equal("1879,1,M01,1,shaka,1879",
            Eval(@"var d = Temporal.PlainDate.from('1957-03-22').withCalendar('indian');
                   [d.year, d.month, d.monthCode, d.day, d.era, d.eraYear].join(',')"));

    [Fact] // Chaitra is 31 days in a leap Saka year (1922 = Greg 2000) and 30 in a common one (1921)
    public void IndianChaitraLength()
        => Assert.Equal("31,true,30,false",
            Eval(@"function f(y){ return Temporal.PlainDate.from({year:y, month:1, day:1, calendar:'indian'}); }
                   [f(1922).daysInMonth, f(1922).inLeapYear, f(1921).daysInMonth, f(1921).inLeapYear].join(',')"));

    [Fact] // month lengths: Vaisakha..Bhadra (2-6) = 31, Asvina..Phalguna (7-12) = 30; 12 months
    public void IndianMonthLengths()
        => Assert.Equal("31,30,30,12",
            Eval(@"function dim(m){ return Temporal.PlainDate.from({year:1921, month:m, day:1, calendar:'indian'}, {overflow:'reject'}).daysInMonth; }
                   var d = Temporal.PlainDate.from({year:1921, month:1, day:1, calendar:'indian'});
                   [dim(2), dim(7), dim(12), d.monthsInYear].join(',')"));

    [Fact] // add a whole year keeps the month/day and stays in the shaka era
    public void IndianAddYear()
        => Assert.Equal("1923,M01,1,shaka,1923",
            Eval(@"var d = Temporal.PlainDate.from({year:1922, monthCode:'M01', day:1, calendar:'indian'}).add({years:1});
                   [d.year, d.monthCode, d.day, d.era, d.eraYear].join(',')"));

    [Fact] // add months crossing into the next year
    public void IndianAddMonthsCrossYear()
        => Assert.Equal("1923,1,M01",
            Eval(@"var d = Temporal.PlainDate.from({year:1922, month:11, day:1, calendar:'indian'}).add({months:2});
                   [d.year, d.month, d.monthCode].join(',')"));

    [Fact] // PlainDateTime also supports indian
    public void IndianDateTime()
        => Assert.Equal("1922,M01,shaka,1922",
            Eval(@"var d = Temporal.PlainDateTime.from({year:1922, month:1, day:1, hour:5, calendar:'indian'});
                   [d.year, d.monthCode, d.era, d.eraYear].join(',')"));

    [Fact] // era resolution: { era:'saka', eraYear } resolves the year
    public void IndianFromEra()
        => Assert.Equal("2000-03-21",
            Eval("Temporal.PlainDate.from({era:'saka', eraYear:1922, month:1, day:1, calendar:'indian'}).withCalendar('iso8601').toString()"));

    // ───────────────── PlainYearMonth non-ISO calendars (Problems 3/4/6/7/9) ─────────────────

    [Fact] // chinese: year is the Gregorian new-year year; CNY 2024 = ISO 2024-02-10
    public void YearMonthChinese()
        => Assert.Equal("2024,M01,chinese,2024-02-10[u-ca=chinese]",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:2024, monthCode:'M01', calendar:'chinese'});
                   [ym.year, ym.monthCode, ym.calendarId, ym.toString()].join(',')"));

    [Fact] // chinese is era-less
    public void YearMonthChineseNoEra()
        => Assert.Equal("undefined,undefined",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:2024, month:1, calendar:'chinese'});
                   [String(ym.era), String(ym.eraYear)].join(',')"));

    [Fact] // hebrew: era "am", Tishrei (M01) of 5784 = ISO 2023-09-16, 30 days
    public void YearMonthHebrew()
        => Assert.Equal("5784,M01,30,am,5784,2023-09-16[u-ca=hebrew]",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:5784, month:1, calendar:'hebrew'});
                   [ym.year, ym.monthCode, ym.daysInMonth, ym.era, ym.eraYear, ym.toString()].join(',')"));

    [Fact] // hebrew leap year has 13 months and a M05L leap month
    public void YearMonthHebrewLeap()
        => Assert.Equal("13,M05L",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:5784, monthCode:'M05L', calendar:'hebrew'});
                   [ym.monthsInYear, ym.monthCode].join(',')"));

    [Fact] // islamic-civil: era "ah", 1446 M01 = ISO 2024-07-08, 30-day first month
    public void YearMonthIslamicCivil()
        => Assert.Equal("1446,M01,30,ah,1446,2024-07-08[u-ca=islamic-civil]",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:1446, month:1, calendar:'islamic-civil'});
                   [ym.year, ym.monthCode, ym.daysInMonth, ym.era, ym.eraYear, ym.toString()].join(',')"));

    [Fact] // ethiopic: 13-month leap year (2015 ≡ 3 mod 4) has a 6-day intercalary M13
    public void YearMonthEthiopic()
        => Assert.Equal("M13,6,true,13",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:2015, month:13, calendar:'ethiopic'}, {overflow:'reject'});
                   [ym.monthCode, ym.daysInMonth, ym.inLeapYear, ym.monthsInYear].join(',')"));

    [Fact] // dangi (Korean lunisolar) round-trips its reference day through ISO
    public void YearMonthDangi()
        => Assert.Equal("2024,M01,2024-02-10[u-ca=dangi]",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:2024, monthCode:'M01', calendar:'dangi'});
                   [ym.year, ym.monthCode, ym.toString()].join(',')"));

    [Fact] // indian also works for PlainYearMonth
    public void YearMonthIndian()
        => Assert.Equal("1922,M01,shaka,1922",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:1922, month:1, calendar:'indian'});
                   [ym.year, ym.monthCode, ym.era, ym.eraYear].join(',')"));

    [Fact] // add a month, crossing the chinese leap month M05 → M05L of 1971
    public void YearMonthChineseAddLeapMonth()
        => Assert.Equal("M05L",
            Eval("Temporal.PlainYearMonth.from({year:1971, monthCode:'M05', calendar:'chinese'}).add({months:1}).monthCode"));

    [Fact] // subtract a year in the hebrew calendar
    public void YearMonthHebrewSubtractYear()
        => Assert.Equal("5783,M01",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:5784, month:1, calendar:'hebrew'}).subtract({years:1});
                   [ym.year, ym.monthCode].join(',')"));

    [Fact] // since one year in the hebrew calendar
    public void YearMonthHebrewSinceYear()
        => Assert.Equal("1,0",
            Eval(@"var a = Temporal.PlainYearMonth.from({year:5784, month:1, calendar:'hebrew'});
                   var b = Temporal.PlainYearMonth.from({year:5783, month:1, calendar:'hebrew'});
                   var s = a.since(b, { largestUnit:'year' });
                   [s.years, s.months].join(',')"));

    [Fact] // until in months across a year
    public void YearMonthIslamicCivilUntilMonths()
        => Assert.Equal("0,12",
            Eval(@"var a = Temporal.PlainYearMonth.from({year:1446, month:1, calendar:'islamic-civil'});
                   var b = Temporal.PlainYearMonth.from({year:1447, month:1, calendar:'islamic-civil'});
                   var s = a.until(b, { largestUnit:'month' });
                   [s.years, s.months].join(',')"));

    [Fact] // with() overriding the month in a non-ISO calendar
    public void YearMonthIslamicCivilWith()
        => Assert.Equal("1446,M03",
            Eval(@"var ym = Temporal.PlainYearMonth.from({year:1446, month:1, calendar:'islamic-civil'}).with({ month:3 });
                   [ym.year, ym.monthCode].join(',')"));

    [Fact] // toPlainDate combines the year-month with a day; Tishrei 15 5784 = ISO 2023-09-30
    public void YearMonthHebrewToPlainDate()
        => Assert.Equal("5784,M01,15,2023-09-30",
            Eval(@"var d = Temporal.PlainYearMonth.from({year:5784, month:1, calendar:'hebrew'}).toPlainDate({ day:15 });
                   [d.year, d.monthCode, d.day, d.withCalendar('iso8601').toString()].join(',')"));

    [Fact] // round-trips through a calendar-annotated string
    public void YearMonthParsesNonIsoAnnotation()
        => Assert.Equal("5784,M01",
            Eval(@"var ym = Temporal.PlainYearMonth.from('2023-09-16[u-ca=hebrew]');
                   [ym.year, ym.monthCode].join(',')"));

    [Fact] // equals respects the calendar
    public void YearMonthEqualsConsidersCalendar()
        => Assert.Equal("true,false",
            Eval(@"var a = Temporal.PlainYearMonth.from({year:5784, month:1, calendar:'hebrew'});
                   var b = Temporal.PlainYearMonth.from({year:5784, month:1, calendar:'hebrew'});
                   var iso = Temporal.PlainYearMonth.from('2023-09-16');
                   [a.equals(b), a.equals(iso)].join(',')"));

    // ─────────────────────────── Problem 2 (option validation) ───────────────────────────

    [Theory]
    [InlineData("pt.round({ smallestUnit: 'hour', roundingIncrement: 7 })")]      // 24 % 7 != 0
    [InlineData("pt.round({ smallestUnit: 'hour', roundingIncrement: 24 })")]     // not < 24
    [InlineData("pt.round({ smallestUnit: 'nanosecond', roundingIncrement: 1000 })")]
    [InlineData("pt.round({ smallestUnit: 'second', roundingIncrement: 60 })")]
    [InlineData("pt.round({ smallestUnit: 'minute', roundingIncrement: 0 })")]
    [InlineData("pt.round({ smallestUnit: 'minute', roundingIncrement: Infinity })")]
    [InlineData("pt.round({ smallestUnit: 'minute', roundingIncrement: NaN })")]
    [InlineData("pt.round({ smallestUnit: 'minute', roundingMode: 'CEIL' })")]
    [InlineData("pt.round({ smallestUnit: 'minute', roundingMode: 'invalid' })")]
    [InlineData("pt.round({ smallestUnit: 'day' })")]                              // not a time unit
    [InlineData("pt.round({ roundingIncrement: 1 })")]                            // missing smallestUnit
    public void PlainTimeRoundOptionValidation(string expr)
        => Assert.Equal("RangeError", ErrorName($"var pt = new Temporal.PlainTime(12, 34, 56); {expr};"));

    [Fact] // a valid round still produces a value (halfExpand by default)
    public void PlainTimeRoundValid()
        => Assert.Equal("04:00:00",
            Eval("new Temporal.PlainTime(3, 30).round({ smallestUnit: 'hour' }).toString()"));

    [Fact] // roundingMode is honored
    public void PlainTimeRoundFloor()
        => Assert.Equal("03:00:00",
            Eval("new Temporal.PlainTime(3, 30).round({ smallestUnit: 'hour', roundingMode: 'floor' }).toString()"));

    [Theory]
    [InlineData("later.since(earlier, { largestUnit: 'minute', smallestUnit: 'hour' })")]
    [InlineData("later.since(earlier, { largestUnit: 'year' })")]                 // calendar unit
    [InlineData("later.since(earlier, { roundingIncrement: NaN })")]
    [InlineData("later.since(earlier, { roundingIncrement: 1e9 + 1 })")]
    [InlineData("later.since(earlier, { smallestUnit: 'second', roundingIncrement: 60 })")]
    [InlineData("later.since(earlier, { roundingMode: 'invalid' })")]
    [InlineData("later.until(earlier, { largestUnit: 'minute', smallestUnit: 'hour' })")]
    public void PlainTimeSinceUntilOptionValidation(string expr)
        => Assert.Equal("RangeError", ErrorName(
            $"var earlier = new Temporal.PlainTime(1, 2, 3), later = new Temporal.PlainTime(4, 5, 6); {expr};"));

    [Fact] // a valid since still produces a duration
    public void PlainTimeSinceValid()
        => Assert.Equal("PT3H3M3S",
            Eval("new Temporal.PlainTime(4,5,6).since(new Temporal.PlainTime(1,2,3), { largestUnit:'hour' }).toString()"));

    [Theory]
    [InlineData("dt.toString({ fractionalSecondDigits: NaN })")]
    [InlineData("dt.toString({ fractionalSecondDigits: -1 })")]
    [InlineData("dt.toString({ fractionalSecondDigits: 10 })")]
    [InlineData("dt.toString({ fractionalSecondDigits: 'invalid' })")]
    [InlineData("dt.toString({ roundingMode: 'invalid' })")]
    [InlineData("dt.toString({ smallestUnit: 'hour' })")]
    [InlineData("dt.toString({ smallestUnit: 'day' })")]
    public void PlainDateTimeToStringOptionValidation(string expr)
        => Assert.Equal("RangeError", ErrorName(
            $"var dt = new Temporal.PlainDateTime(2000, 5, 2, 12, 34, 56, 123, 456, 789); {expr};"));

    [Fact] // fractionalSecondDigits pads/truncates and truncates (default roundingMode)
    public void PlainDateTimeToStringFractionalDigits()
        => Assert.Equal("2000-05-02T12:34:56.123|2000-05-02T12:34:56|2000-05-02T12:34:56.12345678",
            Eval(@"var dt = new Temporal.PlainDateTime(2000, 5, 2, 12, 34, 56, 123, 456, 789);
                   [dt.toString({fractionalSecondDigits:3}), dt.toString({fractionalSecondDigits:0}),
                    dt.toString({fractionalSecondDigits:8})].join('|')"));

    [Fact] // smallestUnit minute omits the seconds component
    public void PlainDateTimeToStringMinutePrecision()
        => Assert.Equal("2000-05-02T12:34",
            Eval("new Temporal.PlainDateTime(2000, 5, 2, 12, 34, 56, 123, 456, 789).toString({ smallestUnit: 'minute' })"));

    [Fact] // rounding to seconds with halfExpand carries up
    public void PlainDateTimeToStringRoundSeconds()
        => Assert.Equal("2000-05-02T12:34:57",
            Eval("new Temporal.PlainDateTime(2000, 5, 2, 12, 34, 56, 500).toString({ smallestUnit: 'second', roundingMode: 'halfExpand' })"));

    [Fact] // the no-options string is unchanged (auto precision)
    public void PlainDateTimeToStringAuto()
        => Assert.Equal("2000-05-02T12:34:56.123456789",
            Eval("new Temporal.PlainDateTime(2000, 5, 2, 12, 34, 56, 123, 456, 789).toString()"));

    // ─────────────────── ZonedDateTime add / subtract (Problems 5/8) ───────────────────

    [Fact] // pure time addition over UTC
    public void ZonedAddHours()
        => Assert.Equal("1970-01-02T01:00:00+00:00[UTC]",
            Eval("new Temporal.ZonedDateTime(0n, 'UTC').add({ hours: 25 }).toString()"));

    [Fact] // calendar (month) addition over UTC
    public void ZonedAddMonths()
        => Assert.Equal("1970-03-01T00:00:00+00:00[UTC]",
            Eval("new Temporal.ZonedDateTime(0n, 'UTC').add({ months: 2 }).toString()"));

    [Fact] // a mixed date + time duration
    public void ZonedAddMixed()
        => Assert.Equal("1970-02-02T03:00:00+00:00[UTC]",
            Eval("new Temporal.ZonedDateTime(0n, 'UTC').add({ months: 1, days: 1, hours: 3 }).toString()"));

    [Fact] // adding a month to Jan 31 constrains the day into the shorter month (2020 leap → Feb 29)
    public void ZonedAddMonthBoundaryConstrain()
        => Assert.Equal("2020-02-29",
            Eval("Temporal.ZonedDateTime.from({year:2020,month:1,day:31,timeZone:'UTC'}).add({months:1}).toPlainDate().toString()"));

    [Fact] // overflow:reject throws when the day does not exist in the resulting month
    public void ZonedAddMonthBoundaryReject()
        => Assert.Equal("RangeError",
            ErrorName("Temporal.ZonedDateTime.from({year:2020,month:1,day:31,timeZone:'UTC'}).add({months:1}, {overflow:'reject'});"));

    [Fact] // subtract is the inverse of add
    public void ZonedSubtractHour()
        => Assert.Equal("1969-12-31T23:00:00+00:00[UTC]",
            Eval("new Temporal.ZonedDateTime(0n, 'UTC').subtract({ hours: 1 }).toString()"));

    [Fact] // adding a zero (blank) duration returns an equal instant
    public void ZonedAddBlankDuration()
        => Assert.Equal("true",
            Eval("var z = new Temporal.ZonedDateTime(123n, 'UTC'); (z.add(new Temporal.Duration()).equals(z)) + ''"));

    [Fact] // the duration argument is coerced (a string "P1D" is a one-day date duration)
    public void ZonedAddCastsArgument()
        => Assert.Equal("1970-01-02T00:00:00+00:00[UTC]",
            Eval("new Temporal.ZonedDateTime(0n, 'UTC').add('P1D').toString()"));

    [Fact] // a calendar day across the spring-forward transition keeps the wall-clock time (23 h day)
    public void ZonedAddDayAcrossDstKeepsWallClock()
        => Assert.Equal("2020-03-09T00:00:00-04:00[America/New_York]",
            Eval("Temporal.ZonedDateTime.from('2020-03-08T00:00:00-05:00[America/New_York]').add({ days: 1 }).toString()"));

    [Fact] // adding 24 exact hours across the 23 h spring-forward day overshoots the wall clock by 1 h
    public void ZonedAdd24HoursAcrossDst()
        => Assert.Equal("2020-03-09T01:00:00-04:00[America/New_York]",
            Eval("Temporal.ZonedDateTime.from('2020-03-08T00:00:00-05:00[America/New_York]').add({ hours: 24 }).toString()"));

    [Fact] // subtract a calendar day back across the transition
    public void ZonedSubtractDayAcrossDst()
        => Assert.Equal("2020-03-08T00:00:00-05:00[America/New_York]",
            Eval("Temporal.ZonedDateTime.from('2020-03-09T00:00:00-04:00[America/New_York]').subtract({ days: 1 }).toString()"));

    [Fact] // a duration that pushes the result past the representable range throws
    public void ZonedAddOutOfRange()
        => Assert.Equal("RangeError",
            ErrorName("new Temporal.ZonedDateTime(0n, 'UTC').add({ years: 1000000 });"));
}
