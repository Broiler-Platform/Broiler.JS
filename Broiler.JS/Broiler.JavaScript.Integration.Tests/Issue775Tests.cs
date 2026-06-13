using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/775
//
// Fixed here:
//   * Problems 1/2/5/8/9 — the non-ISO calendars (chinese, hebrew, dangi, islamic-civil,
//     ethiopic, …) were rejected by Temporal.PlainDateTime at Canonicalize; PlainDateTime now
//     supports them (accessors / from / add / subtract / since / until / parsing), sharing the
//     calendar-date math with PlainDate via TemporalNonIso.
//   * Problem 3 — the ethioaa (Ethiopic Amete Alem) calendar (ethiopic day arithmetic, single
//     "aa" era, years numbered 5500 higher).
//   * Problem 6 — the persian (Solar Hijri) calendar, backed by .NET's PersianCalendar, whose
//     leap-year sequence / Nowruz dates match the Iranian calendar authority's published table.
//   * Problem 7 — the islamic-umalqura (Saudi) calendar: ICU's sighting-based month-length table
//     for 1300–1600 AH (embedded in the Broiler.Unicode submodule as CldrUmmAlQura), falling back
//     to the tabular islamic-civil arithmetic outside that span (ah/bh eras).
//   * Problem 4 — RangeError validation for out-of-range from() fields (positive month/day,
//     invalid monthCode) and for since/until/round options (largestUnit < smallestUnit, invalid
//     roundingIncrement / roundingMode), plus the PlainDateTime.add sub-second overflow case.
//
// Out of scope: the Indian national calendar, and actual since/until calendar-unit rounding.
public class Issue775Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string ErrorName(string code)
        => Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t");

    // ─────────────────────────── ethioaa (Problem 3) ───────────────────────────

    [Fact] // single "aa" era, year = eraYear for any sign
    public void EthioaaEraAndYear()
        => Assert.Equal("7503,2,M02,1,aa,7503",
            Eval(@"var d = Temporal.PlainDate.from({year:7503, monthCode:'M02', day:1, calendar:'ethioaa'});
                   [d.year, d.month, d.monthCode, d.day, d.era, d.eraYear].join(',')"));

    [Fact] // 13 months, leap (year ≡ 3 mod 4) has a 6-day intercalary M13
    public void EthioaaLeapStructure()
        => Assert.Equal("true,6,false,5",
            Eval(@"function f(y){ return Temporal.PlainDate.from({year:y, month:13, day:1, calendar:'ethioaa'}, {overflow:'reject'}); }
                   [f(7515).inLeapYear, f(7515).daysInMonth, f(7514).inLeapYear, f(7514).daysInMonth].join(',')"));

    [Fact] // add 1 year keeps the month/day and stays in the aa era
    public void EthioaaAddYear()
        => Assert.Equal("7504,M02,1,aa,7504",
            Eval(@"var d = Temporal.PlainDate.from({year:7503, monthCode:'M02', day:1, calendar:'ethioaa'}).add({years:1});
                   [d.year, d.monthCode, d.day, d.era, d.eraYear].join(',')"));

    [Fact] // since (days): last day of common year 7514 back to last day of leap year 7515 = -366
    public void EthioaaSinceDays()
        => Assert.Equal("-P366D",
            Eval(@"var a = Temporal.PlainDate.from({year:7514, monthCode:'M13', day:5, calendar:'ethioaa'}, {overflow:'reject'});
                   var b = Temporal.PlainDate.from({year:7515, monthCode:'M13', day:6, calendar:'ethioaa'}, {overflow:'reject'});
                   a.since(b, { largestUnit: 'days' }).toString()"));

    // ─────────────────────────── persian (Problem 6) ───────────────────────────

    [Fact] // Nowruz (1 Farvardin) of 1206 AP = ISO 1827-03-22 (Iranian authority table)
    public void PersianNowruz1206()
        => Assert.Equal("1827-03-22",
            Eval("Temporal.PlainDate.from({year:1206, month:1, day:1, calendar:'persian'}).withCalendar('iso8601').toString()"));

    [Fact] // single "ap" era; months 1-6 = 31 days, 7-11 = 30, 12 = 29 (30 in a leap year)
    public void PersianFieldsAndMonthLengths()
        => Assert.Equal("1400,M07,16,ap,1400,31,30,29",
            Eval(@"var d = Temporal.PlainDate.from({year:1400, monthCode:'M07', day:16, calendar:'persian'});
                   function dim(m){ return Temporal.PlainDate.from({year:1399, month:m, day:1, calendar:'persian'}).daysInMonth; }
                   [d.year, d.monthCode, d.day, d.era, d.eraYear, dim(1), dim(7), Temporal.PlainDate.from({year:1398,month:12,day:1,calendar:'persian'}).daysInMonth].join(',')"));

    [Fact] // 1399 AP is a leap year (per the authority table), 1400 is not
    public void PersianLeapYears()
        => Assert.Equal("true,false",
            Eval(@"[Temporal.PlainDate.from({year:1399,month:1,day:1,calendar:'persian'}).inLeapYear,
                    Temporal.PlainDate.from({year:1400,month:1,day:1,calendar:'persian'}).inLeapYear].join(',')"));

    [Fact] // add 5 months from M07 stays in the same year (no leap months)
    public void PersianAddMonths()
        => Assert.Equal("1400,12,M12,16",
            Eval(@"var d = Temporal.PlainDate.from({year:1400, monthCode:'M07', day:16, calendar:'persian'}).add({months:5});
                   [d.year, d.month, d.monthCode, d.day].join(',')"));

    [Fact] // persian has no leap months
    public void PersianHasNoLeapMonths()
        => Assert.Equal("RangeError",
            ErrorName("Temporal.PlainDate.from({year:1400, monthCode:'M01L', day:1, calendar:'persian'});"));

    // ─────────────────────── islamic-umalqura (Problem 7) ───────────────────────

    [Fact] // ICU's sighting table: 1390 AH (1300+90 = 0x05D5) month lengths
    public void UmalquraMonthLengths1390()
        => Assert.Equal("29,30,29,30,30,30,29,30,29,30,29,30",
            Eval(@"var a = []; for (var m = 1; m <= 12; m++)
                     a.push(Temporal.PlainDate.from({year:1390, month:m, day:1, calendar:'islamic-umalqura'}, {overflow:'reject'}).daysInMonth);
                   a.join(',')"));

    [Fact] // 1391 AH month lengths
    public void UmalquraMonthLengths1391()
        => Assert.Equal("29,29,30,29,30,30,29,30,30,29,30,29",
            Eval(@"var a = []; for (var m = 1; m <= 12; m++)
                     a.push(Temporal.PlainDate.from({year:1391, month:m, day:1, calendar:'islamic-umalqura'}, {overflow:'reject'}).daysInMonth);
                   a.join(',')"));

    [Fact] // non-positive era years remap ah↔bh (years 0/±1/2 are outside the table → civil fallback)
    public void UmalquraEraBoundary()
        => Assert.Equal("0,M01,1,bh,1|-1,M01,1,bh,2|1,M01,1,ah,1|2,M01,1,ah,2",
            Eval(@"function f(era, ey){ var d = Temporal.PlainDate.from({era, eraYear:ey, monthCode:'M01', day:1, calendar:'islamic-umalqura'}, {overflow:'reject'});
                     return [d.year, d.monthCode, d.day, d.era, d.eraYear].join(','); }
                   [f('ah',0), f('ah',-1), f('bh',0), f('bh',-1)].join('|')"));

    [Fact] // add: day 1 / day 29 of a month, ± whole years
    public void UmalquraAddYears()
        => Assert.Equal("1440,M02,1,ah,1440|1445,M02,29",
            Eval(@"var a = Temporal.PlainDate.from({year:1439, monthCode:'M02', day:1, calendar:'islamic-umalqura'}, {overflow:'reject'}).add({years:1});
                   var b = Temporal.PlainDate.from({year:1444, monthCode:'M02', day:29, calendar:'islamic-umalqura'}, {overflow:'reject'}).add({years:1});
                   [a.year, a.monthCode, a.day, a.era, a.eraYear].join(',') + '|' + [b.year, b.monthCode, b.day].join(',')"));

    [Fact] // add a mixed duration: 1417 M12 d1 + 3y 6m 17d = 1421 M06 d18
    public void UmalquraAddMixed()
        => Assert.Equal("1421,6,M06,18",
            Eval(@"var d = Temporal.PlainDate.from({year:1417, monthCode:'M12', day:1, calendar:'islamic-umalqura'}, {overflow:'reject'}).add({years:3, months:6, days:17});
                   [d.year, d.month, d.monthCode, d.day].join(',')"));

    [Fact] // round-trips ISO → islamic-umalqura → ISO (1446 AH = 2024-07-07, a day before civil)
    public void UmalquraRoundTrip()
        => Assert.Equal("1446,M01,1,2024-07-07",
            Eval(@"var d = Temporal.PlainDate.from({year:1446, month:1, day:1, calendar:'islamic-umalqura'});
                   var iso = d.withCalendar('iso8601');
                   var back = iso.withCalendar('islamic-umalqura');
                   [back.year, back.monthCode, back.day, iso.toString()].join(',')"));

    [Fact] // PlainDateTime also supports islamic-umalqura
    public void DateTimeUmalqura()
        => Assert.Equal("29,ah",
            Eval(@"var d = Temporal.PlainDateTime.from({year:1446, month:1, day:1, calendar:'islamic-umalqura'});
                   [d.daysInMonth, d.era].join(',')"));

    // ───────────────── PlainDateTime non-ISO calendars (Problems 1/2/5/8/9) ─────────────────

    [Fact] // chinese: year is the Gregorian new-year year, no era, time preserved
    public void DateTimeChineseAccessors()
        => Assert.Equal("2024,1,M01,1,5,chinese,undefined",
            Eval(@"var d = Temporal.PlainDateTime.from({year:2024, month:1, day:1, hour:5, calendar:'chinese'});
                   [d.year, d.month, d.monthCode, d.day, d.hour, d.calendarId, String(d.era)].join(',')"));

    [Fact] // chinese leap month crossing under add
    public void DateTimeChineseAddCrossesLeapMonth()
        => Assert.Equal("M05L",
            Eval("Temporal.PlainDateTime.from({year:1971, monthCode:'M05', day:1, calendar:'chinese'}).add({months:1}).monthCode"));

    [Fact] // hebrew: era "am", common year 5784 fields
    public void DateTimeHebrewFields()
        => Assert.Equal("5784,M01,30,am,5784",
            Eval(@"var d = Temporal.PlainDateTime.from({year:5784, month:1, day:1, calendar:'hebrew'});
                   [d.year, d.monthCode, d.daysInMonth, d.era, d.eraYear].join(',')"));

    [Fact] // hebrew since one year
    public void DateTimeHebrewSinceOneYear()
        => Assert.Equal("1,0,0,0",
            Eval(@"var a = Temporal.PlainDateTime.from({year:5784, month:1, day:1, calendar:'hebrew'});
                   var b = Temporal.PlainDateTime.from({year:5783, month:1, day:1, calendar:'hebrew'});
                   var s = a.since(b, { largestUnit: 'year' });
                   [s.years, s.months, s.weeks, s.days].join(',')"));

    [Fact] // islamic-civil: era "ah", 12 lunar months alternating 30/29
    public void DateTimeIslamicCivilFields()
        => Assert.Equal("30,29,12,ah",
            Eval(@"var d = Temporal.PlainDateTime.from({year:1446, month:1, day:1, calendar:'islamic-civil'});
                   var d2 = Temporal.PlainDateTime.from({year:1446, month:2, day:1, calendar:'islamic-civil'});
                   [d.daysInMonth, d2.daysInMonth, d.monthsInYear, d.era].join(',')"));

    [Fact] // ethiopic: 13-month leap-year intercalary month
    public void DateTimeEthiopicLeap()
        => Assert.Equal("M13,6,true",
            Eval(@"var d = Temporal.PlainDateTime.from({year:2015, month:13, day:1, calendar:'ethiopic'}, {overflow:'reject'});
                   [d.monthCode, d.daysInMonth, d.inLeapYear].join(',')"));

    [Fact] // dangi (Korean lunisolar) round-trips through ISO
    public void DateTimeDangiRoundTrip()
        => Assert.Equal("2024-02-10T00:00:00",
            Eval("Temporal.PlainDateTime.from({year:2024, month:1, day:1, calendar:'dangi'}).withCalendar('iso8601').toString()"));

    [Fact] // parsing a date-time string with a non-ISO calendar annotation
    public void DateTimeParsesNonIsoAnnotation()
        => Assert.Equal("M01",
            Eval("Temporal.PlainDateTime.from('2024-02-10T00:00[u-ca=chinese]').monthCode"));

    [Fact] // PlainDateTime also accepts the #775 calendars
    public void DateTimePersianAndEthioaa()
        => Assert.Equal("1400,M07,ap,1400|7503,M02,aa,7503",
            Eval(@"var p = Temporal.PlainDateTime.from({year:1400, monthCode:'M07', day:16, calendar:'persian'});
                   var e = Temporal.PlainDateTime.from({year:7503, monthCode:'M02', day:1, calendar:'ethioaa'});
                   [p.year, p.monthCode, p.era, p.eraYear].join(',') + '|' + [e.year, e.monthCode, e.era, e.eraYear].join(',')"));

    // ─────────────────────────── Problem 4 (RangeError validation) ───────────────────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from({year:2000, day:1, month:-1});")]
    [InlineData("Temporal.PlainDate.from({year:2021, month:12, day:0});")]
    [InlineData("Temporal.PlainDate.from({year:2000, month:1, day:-1});")]
    [InlineData("Temporal.PlainDate.from({year:2021, month:0, day:3});")]
    [InlineData("Temporal.PlainDate.from({year:2021, monthCode:'M00', day:17});")]
    [InlineData("Temporal.PlainDate.from({year:2021, monthCode:'M13', day:17});")]
    [InlineData("Temporal.PlainDate.from({year:2021, monthCode:'M19', day:17});")]
    [InlineData("Temporal.PlainDate.from({year:2021, monthCode:'m1', day:17});")]
    [InlineData("Temporal.PlainDateTime.from({year:2021, month:0, day:3});")]
    public void FromFieldsOutOfRangeThrows(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Fact] // a numeric month above the range is still constrained (not rejected) by default
    public void FromConstrainsHighMonth()
        => Assert.Equal("2021-12-05", Eval("Temporal.PlainDate.from({year:2021, month:13, day:5}).toString()"));

    [Theory]
    [InlineData("later.since(earlier, { largestUnit: 'days', smallestUnit: 'years' })")]
    [InlineData("later.since(earlier, { largestUnit: 'weeks', smallestUnit: 'months' })")]
    [InlineData("later.since(earlier, { roundingIncrement: NaN })")]
    [InlineData("later.since(earlier, { roundingIncrement: 0 })")]
    [InlineData("later.since(earlier, { roundingIncrement: -1 })")]
    [InlineData("later.since(earlier, { roundingIncrement: 1e9 + 1 })")]
    [InlineData("later.since(earlier, { roundingIncrement: Infinity })")]
    [InlineData("later.since(earlier, { smallestUnit: 'day', roundingMode: 'CEIL' })")]
    [InlineData("later.until(earlier, { largestUnit: 'days', smallestUnit: 'years' })")]
    public void DateSinceUntilOptionValidation(string expr)
        => Assert.Equal("RangeError", ErrorName(
            $"var earlier = new Temporal.PlainDate(2000,5,2), later = new Temporal.PlainDate(2001,6,3); {expr};"));

    [Fact] // a valid largestUnit still produces the difference
    public void DateSinceValidLargestUnit()
        => Assert.Equal("P1Y1M1D", Eval(
            "new Temporal.PlainDate(2001,6,3).since(new Temporal.PlainDate(2000,5,2), { largestUnit: 'years' }).toString()"));

    [Theory]
    [InlineData("dt.round({ smallestUnit: 'nanoseconds', roundingIncrement: -Infinity })")]
    [InlineData("dt.round({ smallestUnit: 'nanoseconds', roundingIncrement: 0 })")]
    [InlineData("dt.round({ smallestUnit: 'nanoseconds', roundingIncrement: 0.9 })")]
    [InlineData("dt.round({ smallestUnit: 'nanoseconds', roundingIncrement: 1e9 + 1 })")]
    [InlineData("dt.round({ smallestUnit: 'nanoseconds', roundingIncrement: Infinity })")]
    [InlineData("dt.round({ smallestUnit: 'nanoseconds', roundingMode: 'auto' })")]
    public void DateTimeRoundOptionValidation(string expr)
        => Assert.Equal("RangeError", ErrorName(
            $"var dt = new Temporal.PlainDateTime(2000,5,2,12,34,56,0,0,5); {expr};"));

    [Fact] // adding a large sub-second duration is computed without overflow before range-checking
    public void DateTimeAddLargeSubseconds()
        => Assert.Equal("2020,6,12,6,57,27,2,353,569",
            Eval(@"var pdt = new Temporal.PlainDateTime(2020, 2, 29, 0, 57, 27, 747, 612, 578);
                   var d = pdt.add(Temporal.Duration.from({ nanoseconds: Number.MAX_SAFE_INTEGER }));
                   [d.year, d.month, d.day, d.hour, d.minute, d.second, d.millisecond, d.microsecond, d.nanosecond].join(',')"));

    [Fact] // milliseconds at MAX_SAFE_INTEGER pushes the date out of range → RangeError
    public void DateTimeAddOutOfRangeThrows()
        => Assert.Equal("RangeError",
            ErrorName("new Temporal.PlainDateTime(2020,2,29,0,57,27,747,612,578).add(Temporal.Duration.from({ milliseconds: Number.MAX_SAFE_INTEGER }));"));
}
