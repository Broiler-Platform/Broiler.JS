using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/727
//
// Intl.RelativeTimeFormat.prototype.format / formatToParts were stubs (format
// echoed the value; formatToParts returned []). They are now backed by the CLDR
// relative-time fields (generated into the Broiler.Unicode CldrRelativeTimeData
// table, surfaced via CldrLocaleData.GetRelativeTimePattern/GetRelativeTimeExact),
// the locale plural rules, and the locale NumberFormat for the number parts.
public class Issue727Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string DumpParts =
        "function d(p){return JSON.stringify(p.map(function(x){return {type:x.type,value:x.value,unit:x.unit};}));}";

    [Fact]
    public void EnUsFormatNumericAlways()
        => Assert.Equal("in 1,000 seconds", Eval("new Intl.RelativeTimeFormat('en-US').format(1000, 'second');"));

    [Fact]
    public void EnUsFormatToPartsGrouped()
        => Assert.Equal(
            "[{\"type\":\"literal\",\"value\":\"in \"},"
            + "{\"type\":\"integer\",\"value\":\"1\",\"unit\":\"second\"},"
            + "{\"type\":\"group\",\"value\":\",\",\"unit\":\"second\"},"
            + "{\"type\":\"integer\",\"value\":\"000\",\"unit\":\"second\"},"
            + "{\"type\":\"literal\",\"value\":\" seconds\"}]",
            Eval(DumpParts + "d(new Intl.RelativeTimeFormat('en-US').formatToParts(1000, 'second'));"));

    [Fact]
    public void EnUsFormatToPartsSingular()
        => Assert.Equal(
            "[{\"type\":\"literal\",\"value\":\"in \"},"
            + "{\"type\":\"integer\",\"value\":\"1\",\"unit\":\"second\"},"
            + "{\"type\":\"literal\",\"value\":\" second\"}]",
            Eval(DumpParts + "d(new Intl.RelativeTimeFormat('en-US').formatToParts(1, 'second'));"));

    [Fact]
    public void EnUsNegativeZeroIsPast()
        => Assert.Equal(
            "[{\"type\":\"integer\",\"value\":\"0\",\"unit\":\"second\"},"
            + "{\"type\":\"literal\",\"value\":\" seconds ago\"}]",
            Eval(DumpParts + "d(new Intl.RelativeTimeFormat('en-US').formatToParts(-0, 'second'));"));

    [Fact]
    public void EnUsNumericAutoUsesExactPhrase()
        => Assert.Equal("tomorrow", Eval("new Intl.RelativeTimeFormat('en-US', { numeric: 'auto' }).format(1, 'day');"));

    [Fact]
    public void EnUsNumericAutoNow()
        => Assert.Equal("now", Eval("new Intl.RelativeTimeFormat('en-US', { numeric: 'auto' }).format(0, 'second');"));

    [Fact]
    public void EnUsShortStyle()
        => Assert.Equal("in 1,000 sec.", Eval("new Intl.RelativeTimeFormat('en-US', { style: 'short' }).format(1000, 'second');"));

    [Fact]
    public void PolishLongFormat()
        // Polish has CLDR minimumGroupingDigits=2, so a four-digit value like 1000
        // is NOT grouped ("1000", not "1 000") — matching V8/test262.
        => Assert.Equal("za 1000 sekund", Eval("new Intl.RelativeTimeFormat('pl-PL').format(1000, 'second');"));

    [Fact]
    public void PluralUnitSpellingAccepted()
        => Assert.Equal("in 3 days", Eval("new Intl.RelativeTimeFormat('en-US').format(3, 'days');"));

    [Fact]
    public void InvalidUnitThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "(function(){ try { new Intl.RelativeTimeFormat('en').format(1, 'fortnight'); return 'no throw'; } catch (e) { return e.constructor.name; } })();"));

    [Fact]
    public void NonFiniteValueThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "(function(){ try { new Intl.RelativeTimeFormat('en').format(Infinity, 'second'); return 'no throw'; } catch (e) { return e.constructor.name; } })();"));
}
