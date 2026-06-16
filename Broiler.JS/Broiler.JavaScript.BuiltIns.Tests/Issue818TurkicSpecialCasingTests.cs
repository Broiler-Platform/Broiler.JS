using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 17
// (test/intl402/String/prototype/toLocaleLowerCase/special_casing_{Turkish,Azeri}.js):
//   Test262Error: LATIN CAPITAL LETTER I followed by COMBINING DOT BELOW, COMBINING DOT
//   ABOVE Expected SameValue(«"ı̣̇"», «"ị"») to be true.
//
// Turkic (tr/az) lowercasing only collapsed an *adjacent* "I + U+0307" to "i"; it did
// not apply the SpecialCasing.txt After_I / Before_Dot context, which allows
// combining marks of class other than 0/230 to intervene. The lowercasing now scans
// that context using Canonical_Combining_Class == 230 (Above) data, so:
//   * an I that is Before_Dot lowercases to dotted "i" (not dotless U+0131);
//   * a U+0307 that is After_I is removed;
//   * an intervening Above (ccc 230) mark — or a starter (ccc 0) — blocks the context.
public class Issue818TurkicSpecialCasingTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // "I" + COMBINING DOT BELOW (ccc 220, transparent) + COMBINING DOT ABOVE  ->  "i" + DOT BELOW
    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void IWithInterveningBelowMark_IsBeforeDot(string locale)
        => Assert.Equal("true", Eval(
            $"String('I\\u0323\\u0307'.toLocaleLowerCase('{locale}') === 'i\\u0323')"));

    // "I" + PHAISTOS DISC SIGN (U+101FD, ccc 220, transparent) + DOT ABOVE -> "i" + U+101FD
    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void IWithInterveningAstralBelowMark_IsBeforeDot(string locale)
        => Assert.Equal("true", Eval(
            $"String('I\\uD800\\uDDFD\\u0307'.toLocaleLowerCase('{locale}') === 'i\\uD800\\uDDFD')"));

    // A starter (LATIN CAPITAL LETTER A, ccc 0) blocks: I -> dotless, DOT ABOVE kept.
    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void StarterBlocksContext_INotBeforeDot(string locale)
        => Assert.Equal("true", Eval(
            $"String('IA\\u0307'.toLocaleLowerCase('{locale}') === '\\u0131a\\u0307')"));

    // An Above (ccc 230) mark blocks: I -> dotless, DOT ABOVE kept.
    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void AboveMarkBlocksContext_INotBeforeDot(string locale)
        => Assert.Equal("true", Eval(
            $"String('I\\u0300\\u0307'.toLocaleLowerCase('{locale}') === '\\u0131\\u0300\\u0307')"));

    // An astral Above mark (MUSICAL SYMBOL COMBINING DOIT, U+1D185, ccc 230) blocks too.
    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void AstralAboveMarkBlocksContext(string locale)
        => Assert.Equal("true", Eval(
            $"String('I\\uD834\\uDD85\\u0307'.toLocaleLowerCase('{locale}') === '\\u0131\\uD834\\uDD85\\u0307')"));

    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void DottedCapitalI_LowercasesToI(string locale)
        => Assert.Equal("true", Eval($"String('\\u0130'.toLocaleLowerCase('{locale}') === 'i')"));

    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void PlainI_LowercasesToDotlessI(string locale)
        => Assert.Equal("true", Eval($"String('I'.toLocaleLowerCase('{locale}') === '\\u0131')"));

    // A non-Turkic locale is unaffected: I + DOT ABOVE stays "i" + DOT ABOVE.
    [Fact]
    public void NonTurkicLocaleIsUnchanged()
        => Assert.Equal("true", Eval(
            "String('I\\u0307'.toLocaleLowerCase('en') === '\\u0069\\u0307')"));
}
