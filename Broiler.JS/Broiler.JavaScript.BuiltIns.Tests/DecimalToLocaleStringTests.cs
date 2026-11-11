using System;
using System.Globalization;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Decimal.prototype.toLocaleString on the shared Intl number formatting (milestone SJL-9's clause 8, on the owner's ruling of
// 2026-11-11, "Fix in code"): `locales` and `options` are read as ECMA-402's Number.prototype.toLocaleString reads them, a new
// Intl.NumberFormat over both, which formats the value's exact decimal digits; with no `locales` the locale is the
// default-locale provider's (Broiler.VM's JSD-0082, decision D7, row L35), and the host's culture is read only behind it.
// Until 2026-11-11 the method answered `value.ToString(CultureInfo.CurrentCulture)`, whatever the arguments.
public class DecimalToLocaleStringTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private static T UnderCulture<T>(string culture, Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // The value is written as a String the m literal's parse reads the same under any culture, so that only the formatting
    // is under test: `Decimal("1234567891") / 1000m` is 1234567.891 exactly.
    private const string Value = "(Decimal('1234567891') / 1000m)";

    [Theory]
    [InlineData("en", "1,234,567.891")]
    [InlineData("en-US", "1,234,567.891")]
    [InlineData("de", "1.234.567,891")]
    [InlineData("de-DE", "1.234.567,891")]
    public void A_locale_is_formatted_by_the_shared_number_formatting(string locale, string expected)
    {
        Assert.Equal(expected, Eval($"{Value}.toLocaleString('{locale}')"));
        Assert.Equal("true", Eval($"{Value}.toLocaleString('{locale}') === new Intl.NumberFormat('{locale}').format('1234567.891')"));
    }

    [Theory]
    [InlineData("'en', { maximumFractionDigits: 1 }", "1,234,567.9")]
    [InlineData("'de', { minimumFractionDigits: 4 }", "1.234.567,8910")]
    [InlineData("'de', { style: 'currency', currency: 'EUR' }", "1.234.567,89\u00A0€")]
    [InlineData("'en', { style: 'currency', currency: 'USD', currencyDisplay: 'code' }", "USD\u00A01,234,567.89")]
    [InlineData("'en', { notation: 'compact' }", "1.2M")]
    [InlineData("'en', { style: 'percent' }", "123,456,789%")]
    [InlineData("'en', { useGrouping: false }", "1234567.891")]
    [InlineData("'hi-u-nu-deva'", "१२,३४,५६७.८९१")]
    public void Options_are_read_as_Intl_NumberFormat_reads_them(string arguments, string expected)
        => Assert.Equal(expected, Eval($"{Value}.toLocaleString({arguments})"));

    // The value's digits are exact: a Number would round 12345678901234567890.123456789 to 17 significant digits.
    [Fact]
    public void The_value_is_formatted_exactly()
    {
        Assert.Equal("12,345,678,901,234,567,890.123456789",
            Eval("(Decimal('12345678901234567890123456789') / 1000000000m).toLocaleString('en', { maximumFractionDigits: 20 })"));
        // halfExpand, the default rounding mode, rounds a half away from zero.
        Assert.Equal("-2", Eval("(Decimal('-15') / 10m).toLocaleString('en', { maximumFractionDigits: 0 })"));
        Assert.Equal("0", Eval("(0m).toLocaleString('en')"));
    }

    // The options are refused as Intl.NumberFormat refuses them, and a malformed tag is a RangeError, as ECMA-402's
    // CanonicalizeLocaleList says; until 2026-11-11 both were ignored.
    [Theory]
    [InlineData("'en', { style: 'currency' }", "TypeError")]
    [InlineData("'xx-invalid-'", "RangeError")]
    [InlineData("'en', { maximumFractionDigits: 101 }", "RangeError")]
    public void Arguments_are_refused_as_Intl_NumberFormat_refuses_them(string arguments, string expected)
        => Assert.Equal(expected, Eval($"var e = 'none'; try {{ {Value}.toLocaleString({arguments}); }} catch (x) {{ e = x.name; }} e"));

    // With no locales the default-locale provider decides (decision D7, row L35), under a host culture as under none: the
    // answer is Intl.NumberFormat's with no locales, never the host culture's own .NET formatting.
    [Fact]
    public void With_no_locales_the_default_locale_provider_decides()
    {
        Assert.Equal("1,234,567.891", Eval($"{Value}.toLocaleString()"));
        Assert.Equal("1,234,567.891", Eval($"{Value}.toLocaleString(undefined)"));
        Assert.Equal("true", Eval($"{Value}.toLocaleString() === new Intl.NumberFormat().format('1234567.891')"));
        Assert.Equal("1.234.567,891", UnderCulture("de-DE", () => Eval($"{Value}.toLocaleString()")));
        Assert.Equal("true", UnderCulture("de-DE", () => Eval($"{Value}.toLocaleString() === new Intl.NumberFormat().format('1234567.891')")));
        Assert.Equal("de-DE", UnderCulture("de-DE", () => Eval("new Intl.NumberFormat().resolvedOptions().locale")));
    }

    [Fact]
    public void The_method_is_the_engine_s_own()
    {
        Assert.Equal("0", Eval("Decimal.prototype.toLocaleString.length"));
        Assert.Equal("toLocaleString", Eval("Decimal.prototype.toLocaleString.name"));
    }

    // Under invariant globalization the host lists no culture: with no locales and with `de` the locale is the default, en-US
    // (decision D7, row L44), and no host exception escapes; run in a child process.
    [Fact]
    public void Under_invariant_globalization_the_default_locale_formats()
        => InvariantGlobalizationChild.Run(typeof(DecimalToLocaleStringTests).FullName + "." + nameof(InvariantChild));

    [Fact(Explicit = true)]
    public void InvariantChild()
    {
        Assert.True(InvariantGlobalizationChild.IsChild);
        Assert.Equal("1,234,567.891", Eval($"{Value}.toLocaleString()"));
        Assert.Equal("1,234,567.891", Eval($"{Value}.toLocaleString('de')"));
        Assert.Equal("en-US", Eval("new Intl.NumberFormat('de').resolvedOptions().locale"));
        Assert.Equal("EUR\u00A01,234,567.89", Eval($"{Value}.toLocaleString('en', {{ style: 'currency', currency: 'EUR', currencyDisplay: 'code' }})"));
    }
}
