using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The roadmap retest-queue item "older Intl.DateTimeFormat range/parts, SameValue, and
// Proxy-ordering reports".
//
// None of the three reproduces, and these tests pin what was checked. They deliberately avoid
// asserting rendered separator text (an en dash today, ICU-version dependent), and instead
// assert the structure the specification fixes: which parts appear, what each part's `source`
// is, and the order the options bag is read in.
public class IntlDateTimeFormatRangeTests
{
    private static string Eval(string body)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        return ctx.Eval("(function () {" + body + "})()").ToString();
    }

    // A fully explicit formatter: fixed locale, fixed time zone, all-numeric fields, so the
    // output does not move with locale data.
    private const string Formatter =
        "var f = new Intl.DateTimeFormat('en-US',"
        + " { timeZone: 'UTC', year: 'numeric', month: '2-digit', day: '2-digit' });";

    private const string Jan2 = "var d1 = new Date(Date.UTC(2020, 0, 2));";
    private const string Jan5 = "var d2 = new Date(Date.UTC(2020, 0, 5));";

    // ---- "Proxy-ordering": the options bag is read in InitializeDateTimeFormat order ----

    // ECMA-402 reads the options in a fixed order, and a Proxy is how a test observes it.
    [Fact]
    public void TheOptionsBagIsReadInSpecifiedOrder()
        => Assert.Equal(
            "localeMatcher,calendar,numberingSystem,hour12,hourCycle,timeZone,"
            + "weekday,era,year,month,day,dayPeriod,hour,minute,second,fractionalSecondDigits,"
            + "timeZoneName,formatMatcher,dateStyle,timeStyle",
            Eval(
                "var seen = [];"
                + "var options = new Proxy({}, {"
                + "  get: function (t, k) { if (typeof k === 'string') seen.push(k); return undefined; },"
                + "  has: function () { return true; }"
                + "});"
                + "try { new Intl.DateTimeFormat('en-US', options); } catch (e) { seen.push('THREW:' + e.name); }"
                + "return seen.join(',');"));

    // ---- "parts": formatToParts and formatRangeToParts shape ----

    [Fact]
    public void FormatToPartsSplitsTheRenderingIntoTypedParts()
        => Assert.Equal("month:01|literal:/|day:02|literal:/|year:2020", Eval(
            Formatter + Jan2
            + "return f.formatToParts(d1).map(function (p) { return p.type + ':' + p.value; }).join('|');"));

    // Every part of a two-instant range carries a `source`: the start components are
    // startRange, the separator between them is shared, the end components are endRange.
    [Fact]
    public void FormatRangeToPartsMarksEachPartWithItsSource()
        => Assert.Equal(
            "month/startRange,literal/startRange,day/startRange,literal/startRange,year/startRange,"
            + "literal/shared,"
            + "month/endRange,literal/endRange,day/endRange,literal/endRange,year/endRange",
            Eval(
                Formatter + Jan2 + Jan5
                + "return f.formatRangeToParts(d1, d2).map(function (p) { return p.type + '/' + p.source; }).join(',');"));

    // The rendered range still begins with the start date and ends with the end date, whatever
    // separator the locale data supplies.
    // Revised 2026-11-09, at SJL-9.5 (task 8.5, row T30, implementation-defined): the shared library writes a range with CLDR 48.2.0's
    // interval pattern for the skeleton, en's "M/d/y – M/d/y" for yMMdd, and does not widen its fields to the
    // requested 2-digit month and day as ICU does (V8: "01/02/2020 – 01/05/2020"), so the endpoints are
    // "1/2/2020" and "1/5/2020" (a gap of the library, reported with SJL-9.5's stage 3). The test looked for
    // "01/02/2020" and "01/05/2020" and expected "true|true".
    // Revised 2026-11-09, at SJL-9.5 (review of task 8.5): the assertion of task 8.5 looked for "1/2/2020" and "1/5/2020"
    // ("s.length - 8"), which pinned the library's answer as the expected one. The widening of an interval pattern's fields
    // to the requested widths is not row T30's choice of pattern: T30 reads "CLDR 48.2.0's intervalFormats with ICU's
    // fallbacks", and ICU widens them, so format writes "01/02/2020" while formatRange writes "1/2/2020". The original
    // assertion is restored and skipped until a library merge (decision D52, SJL-D-9; DateIntervalFormatter's
    // AdjustFieldWidth) widens the fields, or until the owner rules that T30 admits the answer.
    // Revised 2026-11-09, at SJL-9.5's stage 4 (row T30, implementation-defined): the test is restored, unskipped, with the
    // library's answer. The shared library's merge Broiler-Platform/Broiler.JS#1108 (item 5) found that the pinned reference,
    // ICU 78.3, and ICU 77.1 both answer "1/2/2020 – 1/5/2020" (thin spaces around the en dash) for this format, so the
    // review's reading that "ICU widens them" does not hold and no library change is owed: an interval pattern's fields keep
    // the skeleton's widths, which is T30's "CLDR 48.2.0's intervalFormats with ICU's fallbacks". The skip read "SJL-9.5
    // (2026-11-09): the shared library's formatRange does not widen an interval pattern's fields to the requested 2-digit
    // month and day; a library gap for its own merge (decision D52, SJL-D-9), held for the owner's ruling on row T30", and the
    // skipped assertion looked for "01/02/2020" at 0 and "01/05/2020" at "s.length - 10", expecting "true|true".
    // Revised 2026-11-09, at SJL-9.5's stage 4 (review): two older sentences above no longer hold as written. The lead
    // comment's "whatever separator the locale data supplies" (and the class note's "They deliberately avoid asserting
    // rendered separator text") no longer describes this test, which pins en's separator, a thin space, an en dash and a
    // thin space (row T30). The task 8.5 revision's "does not widen its fields to the requested 2-digit month and day as
    // ICU does (V8: "01/02/2020 – 01/05/2020")" is wrong about ICU: Node 22.22.0 (ICU 77.1) answers
    // "1/2/2020 – 1/5/2020" for this formatter's range and "01/02/2020" for its format.
    [Fact]
    public void FormatRangeRendersBothEndpoints()
        => Assert.Equal("1/2/2020\u2009\u2013\u20091/5/2020", Eval(
            Formatter + Jan2 + Jan5
            + "return f.formatRange(d1, d2);"));

    // ---- "SameValue": two equal instants collapse to the single, non-range rendering ----

    [Fact]
    public void ARangeOfOneInstantFormatsAsThatSingleValue()
        => Assert.Equal("true", Eval(
            Formatter + Jan2 + "return String(f.formatRange(d1, d1) === f.format(d1));"));

    // ...and every part of it is `shared`, since neither endpoint distinguishes it.
    [Fact]
    public void EveryPartOfAOneInstantRangeIsShared()
        => Assert.Equal("shared,shared,shared,shared,shared", Eval(
            Formatter + Jan2
            + "return f.formatRangeToParts(d1, d1).map(function (p) { return p.source; }).join(',');"));

    // ---- Argument validation ----

    [Theory]
    [InlineData("f.formatRange(new Date(NaN), d1)")]
    [InlineData("f.formatRange(d1, new Date(NaN))")]
    [InlineData("f.formatRangeToParts(new Date(NaN), d1)")]
    [InlineData("f.formatToParts(new Date(NaN))")]
    public void AnInvalidDateThrowsARangeError(string call)
        => Assert.Equal("RangeError", Eval(
            Formatter + Jan2
            + "try { " + call + "; return 'no throw'; } catch (e) { return e.name; }"));

    [Theory]
    [InlineData("f.formatRange(undefined, undefined)")]
    [InlineData("f.formatRange(d1, undefined)")]
    [InlineData("f.formatRangeToParts(undefined, d1)")]
    public void AnUndefinedEndpointThrowsATypeError(string call)
        => Assert.Equal("TypeError", Eval(
            Formatter + Jan2
            + "try { " + call + "; return 'no throw'; } catch (e) { return e.name; }"));

    // resolvedOptions hands back a fresh object each call, carrying the same resolved values.
    [Fact]
    public void ResolvedOptionsIsAFreshObjectWithStableContents()
        => Assert.Equal("true|true", Eval(
            Formatter
            + "return [f.resolvedOptions() !== f.resolvedOptions(),"
            + "        f.resolvedOptions().timeZone === f.resolvedOptions().timeZone].join('|');"));
}
