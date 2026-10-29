using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845:
//   * Problem 83: the legacy (Annex B.2.4) static RegExp properties — RegExp.lastMatch / $&,
//     RegExp.$1..$9, RegExp.input / $_, RegExp.lastParen / $+, RegExp.leftContext / $`,
//     RegExp.rightContext / $' — are populated by the built-in exec (and the String methods
//     that use it). They were previously hard-wired to the empty string.
//   * Problems 23 & 66: after an empty match, the global match / matchAll / replace loops must
//     advance lastIndex by a whole code point in a Unicode (`u`/`v`) regex (AdvanceStringIndex),
//     so they don't emit a spurious extra empty match between the halves of an astral character.
//   * Problem 11: a `with` object that claimed a binding (HasBinding) but then deleted it via
//     its @@unscopables getter yields `undefined` (sloppy GetBindingValue), not a ReferenceError.
public class Issue845RegExpAndWithTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 83: legacy static RegExp properties ----

    [Fact]
    public void LastMatchReflectsMostRecentExec()
        => Assert.Equal("y", Eval("/y/g.exec('y'); RegExp.lastMatch"));

    [Fact]
    public void DollarAmpersandAliasesLastMatch()
        => Assert.Equal("y", Eval("/y/g.exec('y'); RegExp['$&']"));

    [Theory]
    [InlineData("RegExp.lastMatch", "cde")]
    [InlineData("RegExp.$1", "d")]
    [InlineData("RegExp.$2", "e")]
    [InlineData("RegExp.$3", "")]      // unmatched numbered group → empty string
    [InlineData("RegExp.lastParen", "e")]
    [InlineData("RegExp['$+']", "e")]
    [InlineData("RegExp.input", "abcdef")]
    [InlineData("RegExp['$_']", "abcdef")]
    [InlineData("RegExp.leftContext", "ab")]
    [InlineData("RegExp.rightContext", "f")]
    public void LegacyStaticsAfterCaptureMatch(string expression, string expected)
        => Assert.Equal(expected, Eval($"'abcdef'.match(/c(d)(e)/); {expression}"));

    [Fact]
    public void LegacyStaticsDefaultToEmptyBeforeAnyMatch()
        => Assert.Equal("|||", Eval("[RegExp.lastMatch, RegExp.$1, RegExp.input, RegExp.leftContext].join('|')"));

    // ---- Problems 23 & 66: AdvanceStringIndex on empty matches in Unicode mode ----

    // "🐸🐹X🐺" is 🐸🐹X🐺. Empty matches must step over a
    // surrogate pair, so there is no extra empty match between the two halves of a frog.
    private const string Frogs = "'\\uD83D\\uDC38\\uD83D\\uDC39X\\uD83D\\uDC3A'";

    [Fact]
    public void GlobalUnicodeMatchSkipsSurrogatePairsOnEmptyMatch()
        => Assert.Equal(",,X,,", Eval($"{Frogs}.match(/\\uD83D|X|/gu).join(',')"));

    [Fact]
    public void GlobalUnicodeReplaceSkipsSurrogatePairsOnEmptyMatch()
        => Assert.Equal("x🐸x🐹xx🐺x",
            Eval($"{Frogs}.replace(/\\uD83D|X|/gu, 'x')"));

    [Fact]
    public void MatchAllUnicodeSkipsSurrogatePairsOnEmptyMatch()
        => Assert.Equal("5", Eval($"[...{Frogs}.matchAll(/\\uD83D|X|/gu)].length.toString()"));

    [Fact]
    public void NonUnicodeMatchStillStepsByOneCodeUnit()
        // Without the `u` flag the same pattern advances one code unit at a time, so empty
        // matches fall between surrogate halves — more matches than the Unicode-aware count.
        => Assert.Equal("8", Eval($"{Frogs}.match(/\\uD83D|X|/g).length.toString()"));

    // ---- Problem 11: with-binding deleted by @@unscopables getter ----

    [Fact]
    public void WithBindingDeletedInUnscopablesGetterYieldsUndefined()
        => Assert.Equal("undefined", Eval(
            "var env = { binding: 0, get [Symbol.unscopables]() { delete env.binding; return null; } };" +
            "var result; with (env) { result = binding; } typeof result"));

    [Fact]
    public void WithBindingPresentStillResolves()
        => Assert.Equal("5", Eval("var o = { a: 5 }; with (o) { a; }"));

    [Fact]
    public void WithFallsThroughToOuterWhenObjectLacksBinding()
        => Assert.Equal("9", Eval("var outer = 9; with ({}) { outer; }"));
}
