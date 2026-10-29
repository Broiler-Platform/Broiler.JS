using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 13
// (test/intl402/NumberFormat/prototype/formatRangeToParts/en-US.js):
//   Test262Error: Expected SameValue(«0», «5») to be true (the parts array was empty).
//
// Intl.NumberFormat.prototype.formatRange/formatRangeToParts were stubs (a raw
// "{start}–{end}" string and an empty array). They now implement
// PartitionNumberRangePattern: when the two endpoints format identically the value is
// shown once prefixed by the approximately sign (every part shared); otherwise the
// start parts (source "startRange"), the " – " separator (shared) and the end parts
// (source "endRange"). En route, SetNumberFormatDigitOptions was corrected so a lone
// maximumFractionDigits lowers the default minimum (currency default 2/2 with
// maximumFractionDigits:0 now yields "$3", not "$3.00").
public class Issue818NumberFormatRangeTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string Setup =
        "var nf = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });" +
        "function dump(parts){ return parts.map(function(p){ return p.type + ':' + p.value + ':' + p.source; }).join('|'); }";

    [Fact]
    public void DistinctRangeHasStartAndEndSources()
        => Assert.Equal(
            "currency:$:startRange|integer:3:startRange|literal: – :shared|currency:$:endRange|integer:5:endRange",
            Eval(Setup + "dump(nf.formatRangeToParts(3, 5))"));

    [Fact]
    public void EqualEndpointsRenderApproximatelyShared()
        => Assert.Equal(
            "approximatelySign:~:shared|currency:$:shared|integer:1:shared",
            Eval(Setup + "dump(nf.formatRangeToParts(1, 1))"));

    [Fact]
    public void EndpointsThatRoundEqualRenderApproximately()
        => Assert.Equal(
            "approximatelySign:~:shared|currency:$:shared|integer:3:shared",
            Eval(Setup + "dump(nf.formatRangeToParts(2.999, 3.001))"));

    [Fact]
    public void FormatRangeStringJoinsTheParts()
        => Assert.Equal("$3 – $5", Eval(Setup + "nf.formatRange(3, 5)"));

    [Fact]
    public void RangePartsHaveObjectPrototypeAndAllThreeKeys()
        => Assert.Equal("true", Eval(
            Setup +
            "var p = nf.formatRangeToParts(3, 5)[0];" +
            "String(Object.getPrototypeOf(p) === Object.prototype && 'type' in p && 'value' in p && 'source' in p)"));

    [Fact]
    public void FormatRangeRejectsNaN()
        => Assert.Equal("RangeError", Eval(
            "var nf = new Intl.NumberFormat('en');" +
            "try { nf.formatRange(NaN, 1); 'no-throw'; } catch (e) { e.constructor.name; }"));

    // SetNumberFormatDigitOptions: a lone maximumFractionDigits lowers the currency
    // default minimum instead of being clamped back up to it.
    [Fact]
    public void CurrencyMaximumFractionDigitsZeroDropsTheFraction()
        => Assert.Equal("$3", Eval(
            "new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(3)"));

    [Fact]
    public void CurrencyDefaultStillHasTwoFractionDigits()
        => Assert.Equal("$3.00", Eval(
            "new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(3)"));

    [Fact]
    public void DecimalMinimumFractionDigitsStillRaisesMaximum()
        => Assert.Equal("3.50000", Eval(
            "new Intl.NumberFormat('en-US', { minimumFractionDigits: 5 }).format(3.5)"));
}
