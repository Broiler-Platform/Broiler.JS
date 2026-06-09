using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650 — Problem 4,
// covering test/intl402/DateTimeFormat/prototype/formatToParts/fractionalSecondDigits.js.
//
// fractionalSecondDigits must be validated to the range 1..3 (out-of-range throws
// RangeError), and formatToParts for a minute+second(+fractionalSecond) format must
// emit the corresponding typed parts rather than a single literal.
public class Issue650DateTimeFormatTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    public void FractionalSecondDigitsOutOfRangeThrowsRangeError(string digits)
        => Assert.Equal("RangeError", Eval(
            $"var c='no-throw'; try {{ new Intl.DateTimeFormat('en', {{minute:'numeric', second:'numeric', fractionalSecondDigits:{digits}}}); }} catch (e) {{ c = e.constructor.name; }} c"));

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("undefined")]
    public void FractionalSecondDigitsInRangeIsAccepted(string digits)
        => Assert.Equal("ok", Eval(
            $"var c='ok'; try {{ new Intl.DateTimeFormat('en', {{minute:'numeric', second:'numeric', fractionalSecondDigits:{digits}}}); }} catch (e) {{ c = e.constructor.name; }} c"));

    // minute + second, no fractionalSecondDigits → 3 parts (mm : ss).
    [Fact]
    public void FormatToPartsMinuteSecond()
        => Assert.Equal("minute=02,literal=:,second=03", Eval(
            "var d=new Date(2019,7,10,1,2,3,234);" +
            "new Intl.DateTimeFormat('en',{minute:'numeric',second:'numeric'})" +
            ".formatToParts(d).map(function(p){return p.type+'='+p.value;}).join(',')"));

    // fractionalSecondDigits truncates (rounds down) the millisecond value.
    [Theory]
    [InlineData("234", "1", "2")]
    [InlineData("234", "2", "23")]
    [InlineData("234", "3", "234")]
    [InlineData("567", "1", "5")]
    [InlineData("567", "2", "56")]
    [InlineData("567", "3", "567")]
    public void FormatToPartsFractionalSecond(string ms, string digits, string expectedFraction)
        => Assert.Equal($"minute=02,literal=:,second=03,literal=.,fractionalSecond={expectedFraction}", Eval(
            $"var d=new Date(2019,7,10,1,2,3,{ms});" +
            $"new Intl.DateTimeFormat('en',{{minute:'numeric',second:'numeric',fractionalSecondDigits:{digits}}})" +
            ".formatToParts(d).map(function(p){return p.type+'='+p.value;}).join(',')"));

    // The dayPeriod option uses generated CLDR data (rules + names) for every
    // locale. The fixed "noon" rule wins over the afternoon range, but the fixed
    // "midnight" period is NOT surfaced by the dayPeriod option — 00:00 folds into
    // the surrounding "at night" flexible period (matching test262 / browsers).
    private static string DayPeriod(string locale, string display, int hour)
        => Eval($"new Intl.DateTimeFormat('{locale}', {{ dayPeriod: '{display}' }})" +
                $".format(new Date(2017, 11, 12, {hour}, 0, 0, 0))");

    [Theory]
    [InlineData(0, "at night")]
    [InlineData(9, "in the morning")]
    [InlineData(12, "noon")]
    [InlineData(15, "in the afternoon")]
    [InlineData(23, "at night")]
    public void DayPeriodEnglishIsCldrCorrect(int hour, string expected)
        => Assert.Equal(expected, DayPeriod("en", "long", hour));

    // German and French day periods come from the locale's CLDR data.
    [Fact]
    public void DayPeriodIsLocalized()
    {
        Assert.Equal("morgens", DayPeriod("de", "long", 9));
        Assert.Equal("nachmittags", DayPeriod("de", "long", 15));
        Assert.Equal("du matin", DayPeriod("fr", "long", 9));
    }

    [Fact]
    public void DayPeriodNarrowNoon()
        => Assert.Equal("n", DayPeriod("en", "narrow", 12));
}
