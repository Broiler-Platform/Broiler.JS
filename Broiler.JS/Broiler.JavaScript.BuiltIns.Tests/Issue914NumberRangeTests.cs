using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/914
//
// P8 (test/intl402/NumberFormat/prototype/formatRange/en-US.js) and
// P10 (.../formatRange/pt-PT.js): Intl.NumberFormat.prototype.formatRange must apply
// ICU's range-collapse + locale range separator:
//  - a shared affix identical at both endpoints renders once (a sign-led prefix like
//    "+$", or a currency suffix like NBSP+euro),
//  - the en-dash (–) separator is padded with spaces only when a non-digit abuts
//    it, while a locale whose CLDR range pattern is a spaced hyphen-minus (pt-PT) is
//    always spaced,
//  - pt-PT places the currency symbol after the amount with a NBSP (CLDR pt-PT).
public class Issue914NumberRangeTests
{
    private const string Dash = "–"; // en-dash
    private const string Nb = " ";   // no-break space
    private const string Eur = "€";  // euro sign

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // --- P8: en-US (test262 formatRange/en-US.js) ---

    [Fact]
    public void EnCurrencyRangeKeepsBothSymbolsWithSpacedDash()
        => Assert.Equal("$3 " + Dash + " $5", Eval(
            "new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',maximumFractionDigits:0}).formatRange(3,5)"));

    [Fact]
    public void EnCurrencyRangeApproximatelyWhenEndpointsFormatEqual()
        => Assert.Equal("~$3", Eval(
            "new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',maximumFractionDigits:0}).formatRange(2.9,3.1)"));

    [Fact]
    public void EnCurrencyRangeCollapsesSignAndCurrencyPrefixTight()
        => Assert.Equal("+$2.90" + Dash + "3.10", Eval(
            "new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',signDisplay:'always'}).formatRange(2.9,3.1)"));

    [Fact]
    public void EnDecimalRangeUsesTightDashBetweenDigits()
        => Assert.Equal("987,654,321,987,654,321" + Dash + "987,654,321,987,654,322", Eval(
            "new Intl.NumberFormat('en-US').formatRange('987654321987654321','987654321987654322')"));

    // --- P10: pt-PT (test262 formatRange/pt-PT.js) ---

    [Fact]
    public void PtCurrencyRangeCollapsesSuffixWithSpacedHyphen()
        => Assert.Equal("3 - 5" + Nb + Eur, Eval(
            "new Intl.NumberFormat('pt-PT',{style:'currency',currency:'EUR',maximumFractionDigits:0}).formatRange(3,5)"));

    [Fact]
    public void PtCurrencyRangeApproximately()
        => Assert.Equal("~3" + Nb + Eur, Eval(
            "new Intl.NumberFormat('pt-PT',{style:'currency',currency:'EUR',maximumFractionDigits:0}).formatRange(2.9,3.1)"));

    [Fact]
    public void PtCurrencyRangeCollapsesSignPrefixAndCurrencySuffix()
        => Assert.Equal("+2,90 - 3,10" + Nb + Eur, Eval(
            "new Intl.NumberFormat('pt-PT',{style:'currency',currency:'EUR',signDisplay:'always'}).formatRange(2.9,3.1)"));

    [Fact]
    public void PtDecimalRangeUsesSpacedHyphen()
        => Assert.Equal(
            "987" + Nb + "654" + Nb + "321" + Nb + "987" + Nb + "654" + Nb + "321 - "
            + "987" + Nb + "654" + Nb + "321" + Nb + "987" + Nb + "654" + Nb + "322",
            Eval("new Intl.NumberFormat('pt-PT').formatRange('987654321987654321','987654321987654322')"));

    // pt-PT single currency format places the symbol after the amount with a NBSP.
    [Fact]
    public void PtSingleCurrencyPlacesSymbolAfterAmount()
        => Assert.Equal("3" + Nb + Eur, Eval(
            "new Intl.NumberFormat('pt-PT',{style:'currency',currency:'EUR',maximumFractionDigits:0}).format(3)"));

    // en-US single currency format keeps the symbol before the amount (no regression).
    [Fact]
    public void EnSingleCurrencyPlacesSymbolBeforeAmount()
        => Assert.Equal("$3", Eval(
            "new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',maximumFractionDigits:0}).format(3)"));
}
