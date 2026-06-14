using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/773
//
// Fixed here, scoped to Temporal.PlainDate:
//   * Problem 2 — the "japanese" calendar (Gregorian-family, mid-year regnal eras).
//   * Problems 4/6–9 — the arithmetic calendars hebrew / coptic / ethiopic /
//     islamic-civil / islamic-tbla (TemporalArithmeticCalendar, pure epoch-day math).
//   * Problems 3/5 — the lunisolar calendars chinese / dangi (TemporalLunisolarCalendar,
//     backed by .NET's astronomical EastAsianLunisolarCalendar, valid ~1901–2100).
//   * Problem 1 — dynamic import() in a plain script no longer throws "undefined is not a
//     function"; it returns a promise (a fresh one each call), ToString-coercing the
//     specifier (abrupt completion rejecting rather than throwing) and forwarding to the
//     JSContext.HostImportModule hook, or rejecting with a TypeError when no loader is set.
public class Issue773Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Evaluates code that completes with a promise (or thenable) and drives the job loop until it
    // settles, returning the settled value — for the async dynamic-import tests.
    private static string Execute(string code)
    {
        using var ctx = new JSContext();
        return ctx.Execute(code).ToString();
    }

    // Builds "year,month,monthCode,day,era,eraYear" for a PlainDate.from property bag.
    private static string FromJapanese(string bag, string options = "undefined")
        => Eval($@"
            var d = Temporal.PlainDate.from({{ ...{bag}, calendar: 'japanese' }}, {options});
            [d.year, d.month, d.monthCode, d.day, d.era, d.eraYear].join(',')");

    // ---- Canonicalize / withCalendar accepts "japanese" ----

    [Fact]
    public void CanonicalizeAcceptsJapanese()
        => Assert.Equal("japanese",
            Eval("Temporal.PlainDate.from({ year: 2020, month: 1, day: 1, calendar: 'japanese' }).calendarId"));

    [Fact]
    public void WithCalendarConvertsIsoToJapanese()
        => Assert.Equal("1800,6,M06,15,ce,1800",
            Eval(@"var d = new Temporal.PlainDate(1800, 6, 15).withCalendar('japanese');
                   [d.year, d.month, d.monthCode, d.day, d.era, d.eraYear].join(',')"));

    // ---- era of a full date (boundaries fall mid-year) ----

    [Fact]
    public void ReiwaStartsMay1_2019()
        => Assert.Equal("2019,5,M05,1,reiwa,1",
            FromJapanese("{ year: 2019, month: 5, day: 1 }"));

    [Fact]
    public void BeforeReiwaStartIsHeisei31()
        => Assert.Equal("2019,4,M04,30,heisei,31",
            FromJapanese("{ year: 2019, month: 4, day: 30 }"));

    [Fact]
    public void ShowaToHeiseiBoundaryJan8_1989()
        => Assert.Equal("1989,1,M01,7,showa,64",
            FromJapanese("{ year: 1989, month: 1, day: 7 }"));

    [Fact]
    public void HeiseiStartsJan8_1989()
        => Assert.Equal("1989,1,M01,8,heisei,1",
            FromJapanese("{ year: 1989, month: 1, day: 8 }"));

    // ---- { era, eraYear } resolves to ISO year, then re-displays from the date ----

    [Fact]
    public void Reiwa1BeforeStartRemapsToHeisei31()
        => Assert.Equal("2019,4,M04,30,heisei,31",
            FromJapanese("{ era: 'reiwa', eraYear: 1, monthCode: 'M04', day: 30 }", "{ overflow: 'reject' }"));

    [Fact]
    public void Reiwa1OnStartStaysReiwa1()
        => Assert.Equal("2019,5,M05,1,reiwa,1",
            FromJapanese("{ era: 'reiwa', eraYear: 1, monthCode: 'M05', day: 1 }", "{ overflow: 'reject' }"));

    [Fact]
    public void Heisei37RemapsToReiwa7()
        => Assert.Equal("2025,4,M04,25,reiwa,7",
            FromJapanese("{ era: 'heisei', eraYear: 37, monthCode: 'M04', day: 25 }"));

    [Fact]
    public void Reiwa0RemapsToHeisei30()
        => Assert.Equal("2018,4,M04,25,heisei,30",
            FromJapanese("{ era: 'reiwa', eraYear: 0, monthCode: 'M04', day: 25 }"));

    [Fact]
    public void ReiwaNegative20RemapsToHeisei10()
        => Assert.Equal("1998,4,M04,25,heisei,10",
            FromJapanese("{ era: 'reiwa', eraYear: -20, monthCode: 'M04', day: 25 }"));

    [Fact]
    public void Showa64AfterStartIsHeisei1()
        => Assert.Equal("1989,1,M01,8,heisei,1",
            FromJapanese("{ era: 'showa', eraYear: 64, monthCode: 'M01', day: 8 }", "{ overflow: 'reject' }"));

    // ---- pre-Meiji uses Gregorian ce / bce eras ----

    [Fact]
    public void Meiji6IsCe1873()
        => Assert.Equal("1873,1,M01,1,meiji,6",
            FromJapanese("{ era: 'ce', eraYear: 1873, monthCode: 'M01', day: 1 }", "{ overflow: 'reject' }"));

    [Fact]
    public void PreMeijiCe1800()
        => Assert.Equal("1800,6,M06,15,ce,1800",
            FromJapanese("{ era: 'ce', eraYear: 1800, month: 6, day: 15 }"));

    [Fact]
    public void BceDateEraYear100()
        => Assert.Equal("-99,1,M01,1,bce,100",
            FromJapanese("{ era: 'bce', eraYear: 100, month: 1, day: 1 }"));

    [Fact]
    public void Ce2000IsHeisei12()
        => Assert.Equal("2000,12,M12,31,heisei,12",
            FromJapanese("{ era: 'ce', eraYear: 2000, monthCode: 'M12', day: 31 }", "{ overflow: 'reject' }"));

    // ---- era aliases ad→ce, bc→bce ----

    [Fact]
    public void AdAliasCanonicalizesToCe()
        => Assert.Equal("ce",
            Eval("Temporal.PlainDate.from({ calendar: 'japanese', era: 'ad', eraYear: 1, month: 1, day: 1 }).era"));

    [Fact]
    public void BcAliasCanonicalizesToBce()
        => Assert.Equal("bce",
            Eval("Temporal.PlainDate.from({ calendar: 'japanese', era: 'bc', eraYear: 1, month: 1, day: 1 }).era"));

    // ---- accessors / arithmetic keep the ISO month/day surface ----

    [Fact]
    public void DaysInMonthAndYearAreIso()
        => Assert.Equal("29,366,12,true",
            Eval(@"var d = Temporal.PlainDate.from({ year: 2020, month: 2, day: 1, calendar: 'japanese' });
                   [d.daysInMonth, d.daysInYear, d.monthsInYear, d.inLeapYear].join(',')"));

    [Fact]
    public void AddPreservesJapaneseCalendarAndCrossesEraBoundary()
        => Assert.Equal("2019,5,M05,1,reiwa,1,japanese",
            Eval(@"var d = Temporal.PlainDate.from({ year: 2019, month: 4, day: 30, calendar: 'japanese' }).add({ days: 1 });
                   [d.year, d.month, d.monthCode, d.day, d.era, d.eraYear, d.calendarId].join(',')"));

    [Fact]
    public void SinceBetweenJapaneseDates()
        => Assert.Equal("P1Y2M5D",
            Eval(@"var a = Temporal.PlainDate.from({ year: 2020, month: 3, day: 15, calendar: 'japanese' });
                   var b = Temporal.PlainDate.from({ year: 2019, month: 1, day: 10, calendar: 'japanese' });
                   a.since(b, { largestUnit: 'year' }).toString()"));

    [Fact]
    public void ToStringAppendsCalendarAnnotation()
        => Assert.Equal("2020-03-15[u-ca=japanese]",
            Eval(@"Temporal.PlainDate.from({ year: 2020, month: 3, day: 15, calendar: 'japanese' })
                   .toString({ calendarName: 'always' })"));

    // ─────────────────────────── Coptic (Problem 8) ───────────────────────────

    private static string FromCal(string cal, string bag, string options = "undefined")
        => Eval($@"
            var d = Temporal.PlainDate.from({{ ...{bag}, calendar: '{cal}' }}, {options});
            [d.year, d.monthCode, d.day, d.era, d.eraYear].join(',')");

    [Fact]
    public void CopticAccepted()
        => Assert.Equal("am",
            Eval("Temporal.PlainDate.from({ year: 1743, month: 1, day: 1, calendar: 'coptic' }).era"));

    [Fact] // months 1-12 = 30 days; month 13 = 6 in leap year 1687, 5 in common 1688
    public void CopticDaysInMonth()
        => Assert.Equal("30,30,6,5",
            Eval(@"function dim(y,m){ return Temporal.PlainDate.from({year:y,month:m,day:1,calendar:'coptic'},{overflow:'reject'}).daysInMonth; }
                   [dim(1687,1), dim(1687,12), dim(1687,13), dim(1688,13)].join(',')"));

    [Fact]
    public void CopticLeapYearAndMonthsInYear()
        => Assert.Equal("true,366,13,false,365",
            Eval(@"var l = Temporal.PlainDate.from({year:1687,month:1,day:1,calendar:'coptic'});
                   var c = Temporal.PlainDate.from({year:1688,month:1,day:1,calendar:'coptic'});
                   [l.inLeapYear, l.daysInYear, l.monthsInYear, c.inLeapYear, c.daysInYear].join(',')"));

    [Fact] // add basic-coptic: 1742-M13-01 +1 month -> 1743-M01-01 (13-month wrap)
    public void CopticAddOneMonthWrapsThirteenMonths()
        => Assert.Equal("1743,M01,1,am,1743",
            FromCalAdd("coptic", "{ year: 1742, monthCode: 'M13', day: 1 }", "{ months: 1 }"));

    [Fact] // add basic-coptic: 1713-M12-01 +3y 6m 17d -> 1717-M05-18
    public void CopticAddMixedDuration()
        => Assert.Equal("1717,M05,18,am,1717",
            FromCalAdd("coptic", "{ year: 1713, monthCode: 'M12', day: 1 }", "{ years: 3, months: 6, days: 17 }"));

    [Fact] // add basic-coptic: 1744-M12-29 +2m 3w -> 1745-M02-20
    public void CopticAddMonthsAndWeeks()
        => Assert.Equal("1745,M02,20,am,1745",
            FromCalAdd("coptic", "{ year: 1744, monthCode: 'M12', day: 29 }", "{ months: 2, weeks: 3 }"));

    [Fact] // add basic-coptic: 1744-M13-05 +10 days -> 1745-M01-10
    public void CopticAddDaysAcrossYear()
        => Assert.Equal("1745,M01,10,am,1745",
            FromCalAdd("coptic", "{ year: 1744, monthCode: 'M13', day: 5 }", "{ days: 10 }"));

    [Fact] // non-positive single era year: coptic am 0 / am -1 preserved
    public void CopticNonPositiveEraYearPreserved()
        => Assert.Equal("0,M01,1,am,0|-1,M01,1,am,-1",
            FromCal("coptic", "{ era: 'am', eraYear: 0, monthCode: 'M01', day: 1 }", "{ overflow: 'reject' }")
            + "|" + FromCal("coptic", "{ era: 'am', eraYear: -1, monthCode: 'M01', day: 1 }", "{ overflow: 'reject' }"));

    // ─────────────────────────── Ethiopic (Problem 7) ─────────────────────────

    [Fact] // months 1-12 = 30; month 13 = 6 in leap 1963, 5 in common 1964
    public void EthiopicDaysInMonth()
        => Assert.Equal("30,6,5,true,false",
            Eval(@"function f(y,m){ return Temporal.PlainDate.from({year:y,month:m,day:1,calendar:'ethiopic'},{overflow:'reject'}); }
                   [f(1963,1).daysInMonth, f(1963,13).daysInMonth, f(1964,13).daysInMonth, f(1963,1).inLeapYear, f(1964,1).inLeapYear].join(',')"));

    [Fact] // era-boundary-ethiopic: am 0 -> aa 5500; am -1 -> aa 5499; aa 0 -> year -5500
    public void EthiopicEraBoundaryAmToAa()
        => Assert.Equal("0,M01,1,aa,5500|-1,M01,1,aa,5499|-5500,M01,1,aa,0|-5501,M01,1,aa,-1",
            FromCal("ethiopic", "{ era: 'am', eraYear: 0, monthCode: 'M01', day: 1 }")
            + "|" + FromCal("ethiopic", "{ era: 'am', eraYear: -1, monthCode: 'M01', day: 1 }")
            + "|" + FromCal("ethiopic", "{ era: 'aa', eraYear: 0, monthCode: 'M01', day: 1 }")
            + "|" + FromCal("ethiopic", "{ era: 'aa', eraYear: -1, monthCode: 'M01', day: 1 }"));

    [Fact]
    public void EthiopicPositiveYearUsesAmEra()
        => Assert.Equal("1963,M01,1,am,1963",
            FromCal("ethiopic", "{ year: 1963, monthCode: 'M01', day: 1 }"));

    // ───────────────────── Islamic-civil / islamic-tbla (P6/P9) ───────────────

    [Fact] // era-boundary-islamic-civil: ah 0 -> bh 1; ah -1 -> bh 2; bh 0 -> ah 1; bh -1 -> ah 2
    public void IslamicCivilEraBoundaryAhBh()
        => Assert.Equal("0,M01,1,bh,1|-1,M01,1,bh,2|1,M01,1,ah,1|2,M01,1,ah,2",
            FromCal("islamic-civil", "{ era: 'ah', eraYear: 0, monthCode: 'M01', day: 1 }")
            + "|" + FromCal("islamic-civil", "{ era: 'ah', eraYear: -1, monthCode: 'M01', day: 1 }")
            + "|" + FromCal("islamic-civil", "{ era: 'bh', eraYear: 0, monthCode: 'M01', day: 1 }")
            + "|" + FromCal("islamic-civil", "{ era: 'bh', eraYear: -1, monthCode: 'M01', day: 1 }"));

    [Fact] // 12 lunar months: odd = 30, even = 29, month 12 leap-dependent
    public void IslamicCivilMonthLengths()
        => Assert.Equal("30,29,30,29,12",
            Eval(@"function f(m){ return Temporal.PlainDate.from({year:1446,month:m,day:1,calendar:'islamic-civil'},{overflow:'reject'}).daysInMonth; }
                   var d = Temporal.PlainDate.from({year:1446,month:1,day:1,calendar:'islamic-civil'});
                   [f(1), f(2), f(11), f(12), d.monthsInYear].join(',')"));

    [Fact] // since basic-islamic-civil: 1439/M07/16 .. 1440/M07/16 = 1 year
    public void IslamicCivilSinceOneYear()
        => Assert.Equal("-1,0,0,0",
            Eval(@"var a = Temporal.PlainDate.from({year:1439,monthCode:'M07',day:16,calendar:'islamic-civil'});
                   var b = Temporal.PlainDate.from({year:1440,monthCode:'M07',day:16,calendar:'islamic-civil'});
                   var s = a.since(b, { largestUnit: 'year' });
                   [s.years, s.months, s.weeks, s.days].join(',')"));

    [Fact] // islamic-tbla is islamic-civil shifted one day earlier — same fields, ISO differs by 1
    public void IslamicTblaIsOneDayBeforeCivil()
        => Assert.Equal("1",
            Eval(@"var civil = Temporal.PlainDate.from({year:1446,month:1,day:1,calendar:'islamic-civil'}).withCalendar('iso8601');
                   var tbla  = Temporal.PlainDate.from({year:1446,month:1,day:1,calendar:'islamic-tbla'}).withCalendar('iso8601');
                   String(Temporal.PlainDate.compare(civil, tbla))"));

    [Fact]
    public void ArithmeticCalendarRoundTripsThroughIso()
        => Assert.Equal("1446,M01,1,ah,1446",
            Eval(@"var d = Temporal.PlainDate.from({year:1446,month:1,day:1,calendar:'islamic-civil'});
                   var iso = d.withCalendar('iso8601');
                   var back = iso.withCalendar('islamic-civil');
                   [back.year, back.monthCode, back.day, back.era, back.eraYear].join(',')"));

    private static string FromCalAdd(string cal, string bag, string duration)
        => Eval($@"
            var d = Temporal.PlainDate.from({{ ...{bag}, calendar: '{cal}' }}, {{ overflow: 'reject' }}).add({duration});
            [d.year, d.monthCode, d.day, d.era, d.eraYear].join(',')");

    // ─────────────────────────── Hebrew (Problem 4) ───────────────────────────

    [Fact] // Rosh Hashanah anchors: 1 Tishrei 5783 = 2022-09-26, 5784 = 2023-09-16
    public void HebrewNewYearMapsToIso()
        => Assert.Equal("2022-09-26,2023-09-16",
            Eval(@"function iso(y){ return Temporal.PlainDate.from({year:y,month:1,day:1,calendar:'hebrew'}).withCalendar('iso8601').toString(); }
                   [iso(5783), iso(5784)].join(',')"));

    [Fact] // basic-hebrew: complete common year 5783 month lengths + era am
    public void HebrewCommonYearFields()
        => Assert.Equal("5783,12,M01,30,am,5783",
            Eval(@"var d = Temporal.PlainDate.from({year:5783, month:1, day:1, calendar:'hebrew'});
                   [d.year, d.monthsInYear, d.monthCode, d.daysInMonth, d.era, d.eraYear].join(',')"));

    [Fact] // daysInMonth/basic-hebrew: deficient leap 5730 has 13 months [30,29,29,29,30,30,29,...]
    public void HebrewDeficientLeapYearDaysInMonth()
        => Assert.Equal("30,29,29,29,30,30,29,30,29,30,29,30,29",
            Eval(@"var n = Temporal.PlainDate.from({year:5730,month:1,day:1,calendar:'hebrew'}).monthsInYear;
                   var a = []; for (var m = 1; m <= n; m++) a.push(Temporal.PlainDate.from({year:5730,month:m,day:1,calendar:'hebrew'}).daysInMonth);
                   a.join(',')"));

    [Fact] // daysInYear: complete leap 5779 = 385, complete common 5783 = 355
    public void HebrewDaysInYearAndLeap()
        => Assert.Equal("true,385,false,355",
            Eval(@"var l = Temporal.PlainDate.from({year:5779,month:1,day:1,calendar:'hebrew'});
                   var c = Temporal.PlainDate.from({year:5783,month:1,day:1,calendar:'hebrew'});
                   [l.inLeapYear, l.daysInYear, c.inLeapYear, c.daysInYear].join(',')"));

    [Fact] // monthCode/leap-months-hebrew: leap year 5782 — Adar I (ord 6) = M05L, Adar II (ord 7) = M06
    public void HebrewLeapMonthCodes()
        => Assert.Equal("M01,M02,M03,M04,M05,M05L,M06,M07,M08,M09,M10,M11,M12",
            Eval(@"var n = Temporal.PlainDate.from({year:5782,month:1,day:1,calendar:'hebrew'}).monthsInYear;
                   var a = []; for (var m = 1; m <= n; m++) a.push(Temporal.PlainDate.from({year:5782,month:m,day:1,calendar:'hebrew'}).monthCode);
                   a.join(',')"));

    [Fact] // from with monthCode M05L resolves to Adar I (ordinal 6) in a leap year
    public void HebrewFromAdarIByMonthCode()
        => Assert.Equal("5782,6,M05L,15,am,5782",
            Eval(@"var d = Temporal.PlainDate.from({year:5782, monthCode:'M05L', day:15, calendar:'hebrew'}, {overflow:'reject'});
                   [d.year, d.month, d.monthCode, d.day, d.era, d.eraYear].join(',')"));

    [Fact] // M06 in a leap year is Adar II (ordinal 7), not Adar I
    public void HebrewFromAdarIIByMonthCode()
        => Assert.Equal("5782,7,M06,1",
            Eval(@"var d = Temporal.PlainDate.from({year:5782, monthCode:'M06', day:1, calendar:'hebrew'}, {overflow:'reject'});
                   [d.year, d.month, d.monthCode, d.day].join(',')"));

    [Fact] // a leap month code in a common year does not exist
    public void HebrewLeapMonthCodeInCommonYearThrows()
        => Assert.Equal("RangeError",
            Eval(@"try { Temporal.PlainDate.from({year:5783, monthCode:'M05L', day:1, calendar:'hebrew'}, {overflow:'reject'}); 'no throw'; }
                   catch (e) { e.constructor.name; }"));

    [Fact] // add crosses the leap month: 1 Adar I 5782 + 1 month = 1 Adar II
    public void HebrewAddCrossesLeapMonth()
        => Assert.Equal("5782,M06,1,am,5782",
            FromCalAdd("hebrew", "{ year: 5782, monthCode: 'M05L', day: 1 }", "{ months: 1 }"));

    [Fact] // since across a year, largestUnit year
    public void HebrewSinceOneYear()
        => Assert.Equal("1,0,0,0",
            Eval(@"var a = Temporal.PlainDate.from({year:5784, month:1, day:1, calendar:'hebrew'});
                   var b = Temporal.PlainDate.from({year:5783, month:1, day:1, calendar:'hebrew'});
                   var s = a.since(b, { largestUnit: 'year' });
                   [s.years, s.months, s.weeks, s.days].join(',')"));

    [Fact] // single-era "am" with non-positive era year is preserved (like coptic)
    public void HebrewNonPositiveEraYearPreserved()
        => Assert.Equal("0,M01,1,am,0",
            FromCal("hebrew", "{ era: 'am', eraYear: 0, monthCode: 'M01', day: 1 }", "{ overflow: 'reject' }"));

    // ────────────────── Chinese / dangi lunisolar (Problems 3, 5) ──────────────────
    // Backed by .NET's astronomical EastAsianLunisolarCalendar (valid ~1901–2100). `year` is the
    // Gregorian year of the new-year day; there is no era; leap months use "M##L" codes.

    [Fact] // year is the Gregorian year; Chinese New Year 2024 = 2024-02-10; no era
    public void ChineseNewYearMapsToIso()
        => Assert.Equal("2024,1,M01,1,undefined,undefined,2024-02-10",
            Eval(@"var d = Temporal.PlainDate.from({year:2024, month:1, day:1, calendar:'chinese'});
                   [d.year, d.month, d.monthCode, d.day, String(d.era), String(d.eraYear), d.withCalendar('iso8601').toString()].join(',')"));

    [Fact] // inLeapYear matches the test262 list (1971 leap, 2024 not, 2025 leap)
    public void ChineseLeapYears()
        => Assert.Equal("true,false,true",
            Eval(@"function l(y){ return Temporal.PlainDate.from({year:y,month:1,day:1,calendar:'chinese'}).inLeapYear; }
                   [l(1971), l(2024), l(2025)].join(',')"));

    [Fact] // 1971 has a leap 5th month: ordinal 6 = monthCode M05L
    public void ChineseLeapMonthCode()
        => Assert.Equal("6,M05L,13",
            Eval(@"var d = Temporal.PlainDate.from({year:1971, month:6, day:1, calendar:'chinese'});
                   [d.month, d.monthCode, d.monthsInYear].join(',')"));

    [Fact] // month number and the equivalent leap monthCode resolve identically
    public void ChineseMonthNumberAndCodeAgree()
        => Assert.Equal("true",
            Eval(@"var a = Temporal.PlainDate.from({year:1971, month:6, day:1, calendar:'chinese'});
                   var b = Temporal.PlainDate.from({year:1971, monthCode:'M05L', day:1, calendar:'chinese'});
                   String(a.equals(b))"));

    [Fact] // adding one month to the regular 5th month lands on the leap 5th month
    public void ChineseAddCrossesLeapMonth()
        => Assert.Equal("M05L",
            Eval(@"Temporal.PlainDate.from({year:1971, monthCode:'M05', day:1, calendar:'chinese'}).add({months:1}).monthCode"));

    [Fact] // round-trip ISO → chinese → ISO
    public void ChineseRoundTripsThroughIso()
        => Assert.Equal("M01,2024-02-10",
            Eval(@"var c = new Temporal.PlainDate(2024,2,10).withCalendar('chinese');
                   c.monthCode + ',' + c.withCalendar('iso8601').toString()"));

    [Fact] // dangi (Korean) shares the convention; CNY 2024 = 2024-02-10
    public void DangiNewYearAndLeap()
        => Assert.Equal("2024-02-10,true",
            Eval(@"var d = Temporal.PlainDate.from({year:2024, month:1, day:1, calendar:'dangi'});
                   d.withCalendar('iso8601').toString() + ',' + Temporal.PlainDate.from({year:2025,month:1,day:1,calendar:'dangi'}).inLeapYear"));

    [Fact] // chinese years below the .NET calendar's span (≤1900) are served by the astronomical
           // fallback (issue #794); CNY 1500 = 1500-02-09
    public void ChineseBelowDotNetRange_UsesAstronomicalFallback()
        => Assert.Equal("1500-02-09",
            Eval(@"Temporal.PlainDate.from({year:1500, month:1, day:1, calendar:'chinese'})
                     .withCalendar('iso8601').toString()"));

    // ──────────────────────── Dynamic import() (Problem 1) ────────────────────────

    [Fact] // no longer "undefined is not a function": import() returns a Promise object
    public void DynamicImportReturnsPromise()
        => Assert.Equal("object,true",
            Eval("var p = import('x'); [typeof p, p instanceof Promise].join(',')"));

    [Fact] // each call produces a fresh promise (always-create-new-promise.js)
    public void DynamicImportCreatesNewPromiseEachCall()
        => Assert.Equal("true",
            Eval("String(import('x') !== import('x'))"));

    [Fact] // with no host loader configured the promise rejects with a TypeError
    public void DynamicImportRejectsWithoutLoader()
        => Assert.Equal("rejected:TypeError",
            Execute("import('missing.js').then(() => 'resolved', e => 'rejected:' + e.constructor.name)"));

    [Fact] // a specifier whose toString throws rejects the promise (does not throw synchronously)
    public void DynamicImportSpecifierToStringAbruptRejects()
        => Assert.Equal("rejected:boom",
            Execute("import({ toString() { throw new TypeError('boom'); } }).then(() => 'resolved', e => 'rejected:' + e.message)"));

    [Fact] // evaluating import() never throws synchronously, even for a bad specifier
    public void DynamicImportNeverThrowsSynchronously()
        => Assert.Equal("no-sync-throw",
            Eval("try { import({ toString() { throw 1; } }); 'no-sync-throw'; } catch (e) { 'threw'; }"));
}
