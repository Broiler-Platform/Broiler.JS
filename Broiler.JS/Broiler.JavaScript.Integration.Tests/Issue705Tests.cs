using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/705
//
// Fixed here (Problems 7-10) — Intl.NumberFormat number formatting:
//
//   Intl.NumberFormat.prototype.format / formatToParts were stubs: format did
//   ToString on the input (so format(-Infinity) === "-Infinity") and formatToParts
//   always emitted a single {type:"integer"} part. The spec formats NaN, +Infinity
//   and -Infinity with locale symbols ("NaN"/"非數值" and "∞"), applies the
//   signDisplay option (auto/always/never/exceptZero/negative), and splits the
//   result into typed parts (minusSign/plusSign/integer/group/decimal/fraction/
//   infinity/nan). The sign is chosen from the rounded magnitude, so a value that
//   rounds to zero is a signed zero for "auto"/"always" but unsigned for
//   "exceptZero"/"negative".
//
// Out of scope (unchanged): style:"currency" formatting (Problem 4) still needs CLDR
//   currency patterns; the scope-param, dynamic-super, parser/regex SyntaxError and
//   DateTimeFormat formatRange/CLDR families remain as documented in prior issues.
public class Issue705Tests
{
    private static string Format(string locale, string signDisplay, string value)
        => Eval($"new Intl.NumberFormat('{locale}', {{signDisplay:'{signDisplay}'}}).format({value});").ToString();

    private static string Parts(string locale, string signDisplay, string value)
        => Eval($"new Intl.NumberFormat('{locale}', {{signDisplay:'{signDisplay}'}}).formatToParts({value})" +
                ".map(function(p){return p.type+':'+p.value;}).join('|');").ToString();

    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // ---- Problem 7 / 10: format() renders ±Infinity with the ∞ symbol + sign ----

    [Theory]
    [InlineData("auto", "-∞")]
    [InlineData("always", "-∞")]
    [InlineData("never", "∞")]
    [InlineData("exceptZero", "-∞")]
    [InlineData("negative", "-∞")]
    public void NegativeInfinityFormatsWithSymbol(string signDisplay, string expected)
        => Assert.Equal(expected, Format("en-US", signDisplay, "-Infinity"));

    [Theory]
    [InlineData("auto", "∞")]
    [InlineData("always", "+∞")]
    [InlineData("never", "∞")]
    [InlineData("exceptZero", "+∞")]
    [InlineData("negative", "∞")]
    public void PositiveInfinityFormatsWithSymbol(string signDisplay, string expected)
        => Assert.Equal(expected, Format("en-US", signDisplay, "Infinity"));

    // The NaN symbol is locale dependent (zh-TW uses 非數值).
    [Fact]
    public void NaNUsesLocaleSymbol()
    {
        Assert.Equal("NaN", Format("en-US", "auto", "NaN"));
        Assert.Equal("+NaN", Format("en-US", "always", "NaN"));
        Assert.Equal("非數值", Format("zh-TW", "auto", "NaN"));
    }

    // ---- Sign selection over the rounded magnitude ----

    // A negative value that rounds to zero is a signed zero: shown for auto/always,
    // hidden for exceptZero/negative.
    [Theory]
    [InlineData("auto", "-0")]
    [InlineData("always", "-0")]
    [InlineData("never", "0")]
    [InlineData("exceptZero", "0")]
    [InlineData("negative", "0")]
    public void NegativeRoundingToZeroIsSignedZero(string signDisplay, string expected)
        => Assert.Equal(expected, Format("en-US", signDisplay, "-0.0001"));

    [Theory]
    [InlineData("auto", "987")]
    [InlineData("always", "+987")]
    [InlineData("never", "987")]
    [InlineData("exceptZero", "+987")]
    [InlineData("negative", "987")]
    public void PositiveIntegerSignDisplay(string signDisplay, string expected)
        => Assert.Equal(expected, Format("en-US", signDisplay, "987"));

    [Fact]
    public void NegativeIntegerAlwaysShowsMinus()
        => Assert.Equal("-987", Format("en-US", "negative", "-987"));

    // ---- Problem 8 / 9: formatToParts splits sign / number / symbol parts ----

    [Fact]
    public void FormatToPartsNegativeInfinity()
        => Assert.Equal("minusSign:-|infinity:∞", Parts("en-US", "auto", "-Infinity"));

    [Fact]
    public void FormatToPartsPositiveInfinityAlways()
        => Assert.Equal("plusSign:+|infinity:∞", Parts("en-US", "always", "Infinity"));

    [Fact]
    public void FormatToPartsNaN()
        => Assert.Equal("nan:NaN", Parts("en-US", "auto", "NaN"));

    [Fact]
    public void FormatToPartsNegativeInteger()
        => Assert.Equal("minusSign:-|integer:987", Parts("en-US", "auto", "-987"));

    // "negative" never shows a sign on a value that rounds to zero.
    [Fact]
    public void FormatToPartsNegativeRoundingToZeroNegativeSignDisplay()
        => Assert.Equal("integer:0", Parts("en-US", "negative", "-0.0001"));

    // Grouping splits the integer into integer/group parts for large numbers.
    [Fact]
    public void FormatToPartsGroupsLargeIntegers()
        => Assert.Equal("integer:1|group:,|integer:234|group:,|integer:567",
            Parts("en-US", "auto", "1234567"));

    // ---- Problem 4: currency formatting (style:"currency", accounting sign) ----

    private static string CurrencyParts(string locale, string signDisplay, string value)
        => Eval($"new Intl.NumberFormat('{locale}', {{style:'currency', currency:'USD', " +
                $"currencySign:'accounting', signDisplay:'{signDisplay}'}}).formatToParts({value})" +
                ".map(function(p){return p.type+':'+p.value;}).join('|');").ToString();

    // USD defaults to 2 fraction digits; the symbol precedes the number in en-US.
    [Fact]
    public void CurrencyPositiveEnUs()
        => Assert.Equal("currency:$|integer:987|decimal:.|fraction:00", CurrencyParts("en-US", "auto", "987"));

    // An accounting negative is wrapped in parentheses in en-US.
    [Fact]
    public void CurrencyAccountingNegativeEnUs()
        => Assert.Equal("literal:(|currency:$|integer:987|decimal:.|fraction:00|literal:)",
            CurrencyParts("en-US", "auto", "-987"));

    // ko-KR uses the "US$" symbol; the accounting parens convention still applies.
    [Fact]
    public void CurrencySymbolIsLocaleSpecific()
        => Assert.Equal("literal:(|currency:US$|integer:987|decimal:.|fraction:00|literal:)",
            CurrencyParts("ko-KR", "auto", "-987"));

    // de-DE places the symbol after the number, separated by a no-break space
    // (U+00A0, normalized to <nbsp> below), and uses a leading minus for
    // accounting negatives instead of parentheses.
    [Fact]
    public void CurrencyDeUsesTrailingSymbolAndMinus()
    {
        var actual = CurrencyParts("de-DE", "auto", "-987").Replace(" ", "<nbsp>");
        Assert.Equal("minusSign:-|integer:987|decimal:,|fraction:00|literal:<nbsp>|currency:$", actual);
    }

    // "never" suppresses the accounting parens entirely (uses the positive layout).
    [Fact]
    public void CurrencyNeverSuppressesAccountingParens()
        => Assert.Equal("currency:$|integer:987|decimal:.|fraction:00", CurrencyParts("en-US", "never", "-987"));
}
