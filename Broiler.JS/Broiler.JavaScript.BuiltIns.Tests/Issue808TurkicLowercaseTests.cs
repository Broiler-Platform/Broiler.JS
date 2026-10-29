using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Turkic After_I (SpecialCasing.txt): in tr/az, lowercasing a LATIN CAPITAL LETTER I (U+0049) followed
// by a COMBINING DOT ABOVE (U+0307) absorbs the dot and yields the dotted "i", while a plain I yields
// the dotless "ı". Issue #808 problem 33.
public class Issue808TurkicLowercaseTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void IDotAbove_LowercasesToDottedI(string locale)
        => Assert.Equal("true", Eval($"String('\\u0049\\u0307'.toLocaleLowerCase('{locale}') === 'i');"));

    [Theory]
    [InlineData("tr")]
    [InlineData("az")]
    public void PlainI_LowercasesToDotlessI(string locale)
        => Assert.Equal("true", Eval($"String('I'.toLocaleLowerCase('{locale}') === '\\u0131');"));

    [Fact]
    public void IDotAbove_InContext()
        => Assert.Equal("aib", Eval("'A\\u0049\\u0307B'.toLocaleLowerCase('tr');"));

    [Fact]
    public void NonTurkicLocale_KeepsCombiningDot()
        => Assert.Equal("true", Eval("String('\\u0049\\u0307'.toLocaleLowerCase('en') === '\\u0069\\u0307');"));
}
