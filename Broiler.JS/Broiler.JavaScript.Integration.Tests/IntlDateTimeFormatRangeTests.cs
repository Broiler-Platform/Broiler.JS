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
    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void FormatToPartsSplitsTheRenderingIntoTypedParts()
        => Assert.Equal("month:01|literal:/|day:02|literal:/|year:2020", Eval(
            Formatter + Jan2
            + "return f.formatToParts(d1).map(function (p) { return p.type + ':' + p.value; }).join('|');"));

    // Every part of a two-instant range carries a `source`: the start components are
    // startRange, the separator between them is shared, the end components are endRange.
    [Fact(Timeout = 600000)]
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
    [Fact(Timeout = 600000)]
    public void FormatRangeRendersBothEndpoints()
        => Assert.Equal("true|true", Eval(
            Formatter + Jan2 + Jan5
            + "var s = f.formatRange(d1, d2);"
            + "return [s.indexOf('01/02/2020') === 0, s.lastIndexOf('01/05/2020') === s.length - 10].join('|');"));

    // ---- "SameValue": two equal instants collapse to the single, non-range rendering ----

    [Fact(Timeout = 600000)]
    public void ARangeOfOneInstantFormatsAsThatSingleValue()
        => Assert.Equal("true", Eval(
            Formatter + Jan2 + "return String(f.formatRange(d1, d1) === f.format(d1));"));

    // ...and every part of it is `shared`, since neither endpoint distinguishes it.
    [Fact(Timeout = 600000)]
    public void EveryPartOfAOneInstantRangeIsShared()
        => Assert.Equal("shared,shared,shared,shared,shared", Eval(
            Formatter + Jan2
            + "return f.formatRangeToParts(d1, d1).map(function (p) { return p.source; }).join(',');"));

    // ---- Argument validation ----

    [Theory(Timeout = 600000)]
    [InlineData("f.formatRange(new Date(NaN), d1)")]
    [InlineData("f.formatRange(d1, new Date(NaN))")]
    [InlineData("f.formatRangeToParts(new Date(NaN), d1)")]
    [InlineData("f.formatToParts(new Date(NaN))")]
    public void AnInvalidDateThrowsARangeError(string call)
        => Assert.Equal("RangeError", Eval(
            Formatter + Jan2
            + "try { " + call + "; return 'no throw'; } catch (e) { return e.name; }"));

    [Theory(Timeout = 600000)]
    [InlineData("f.formatRange(undefined, undefined)")]
    [InlineData("f.formatRange(d1, undefined)")]
    [InlineData("f.formatRangeToParts(undefined, d1)")]
    public void AnUndefinedEndpointThrowsATypeError(string call)
        => Assert.Equal("TypeError", Eval(
            Formatter + Jan2
            + "try { " + call + "; return 'no throw'; } catch (e) { return e.name; }"));

    // resolvedOptions hands back a fresh object each call, carrying the same resolved values.
    [Fact(Timeout = 600000)]
    public void ResolvedOptionsIsAFreshObjectWithStableContents()
        => Assert.Equal("true|true", Eval(
            Formatter
            + "return [f.resolvedOptions() !== f.resolvedOptions(),"
            + "        f.resolvedOptions().timeZone === f.resolvedOptions().timeZone].join('|');"));
}
