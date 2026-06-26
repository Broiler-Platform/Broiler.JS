using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/926
//
// Problem 4 (test/staging/sm/RegExp/unicode-braced.js): a braced Unicode escape
// naming a lone surrogate (`\u{D83D}`) is its OWN single code point and must never
// combine with an adjacent surrogate escape. `/\u{D83D}\u{DC38}/u` is two lone
// surrogates that cannot match the astral pair 🐸 (U+1F438), unlike `/\u{1F438}/u`.
//
// In a regex LITERAL the source scanner used to decode every braced escape to the
// bare `\uHHHH` form, which made `\u{D83D}\u{DC38}` indistinguishable from `\u{1F438}`
// (both `🐸`), so the RegExp runtime wrongly folded the two lone surrogates
// into one code point and matched. The scanner now keeps the brace form for lone
// surrogates so the runtime can guard each against pairing — matching the
// (already-correct) `new RegExp(string, "u")` constructor path.
public class Issue926Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Two braced lone surrogates that look like a pair must NOT match a real pair.
    [Theory]
    [InlineData(@"/\u{D83D}\u{DC38}+/u")]
    [InlineData(@"/\uD83D\u{DC38}+/u")]
    [InlineData(@"/\u{D83D}\uDC38+/u")]
    public void BracedLoneSurrogateLiteralDoesNotMatchAstralPair(string regex)
    {
        // "🐸\uDC38" = 🐸 followed by a lone trail surrogate.
        var code = "'' + " + regex + ".exec('\\uD83D\\uDC38\\uDC38')";
        Assert.Equal("null", Eval(code).ToString());
    }

    // Same patterns built via the RegExp constructor (brace form preserved) — must
    // agree with the literal path.
    [Theory]
    [InlineData(@"new RegExp('\\u{D83D}\\u{DC38}+','u')")]
    [InlineData(@"new RegExp('\\uD83D\\u{DC38}+','u')")]
    [InlineData(@"new RegExp('\\u{D83D}\\uDC38+','u')")]
    public void BracedLoneSurrogateConstructorDoesNotMatchAstralPair(string regex)
    {
        var code = "'' + " + regex + ".exec('\\uD83D\\uDC38\\uDC38')";
        Assert.Equal("null", Eval(code).ToString());
    }

    // An astral braced escape still decodes to a single code point and matches.
    [Fact]
    public void AstralBracedEscapeStillMatches()
    {
        Assert.Equal("🐸", Eval(@"/\u{1F438}/u.exec('\u{1F438}')[0]").ToString());
        Assert.Equal("A", Eval(@"/\u{41}/u.exec('ABC')[0]").ToString());
    }

    // A lone braced surrogate matches a code unit only when it is not part of a pair.
    [Fact]
    public void LoneBracedSurrogateMatchesOnlyWhenNotPaired()
    {
        // Lead surrogate not followed by a trail: matches.
        Assert.Equal("\uD83D", Eval("/\\u{D83D}/u.exec('\\uD83D\\uDBFF')[0]").ToString());
        // Lead surrogate forming a pair: no match.
        Assert.Equal("null", Eval("'' + /\\u{D83D}/u.exec('\\uD83D\\uDC00')").ToString());
        // Trail surrogate not preceded by a lead: matches.
        Assert.Equal("\uDC38", Eval("/\\u{DC38}/u.exec('\\uD7FF\\uDC38')[0]").ToString());
        // Trail surrogate forming a pair: no match.
        Assert.Equal("null", Eval("'' + /\\u{DC38}/u.exec('\\uD800\\uDC38')").ToString());
    }

    // Malformed braced escapes in /u mode are still SyntaxErrors.
    [Theory]
    [InlineData(@"/\u{}/u")]
    [InlineData(@"/\u{110000}/u")]
    [InlineData(@"/\u{G}/u")]
    public void MalformedBracedEscapeThrows(string regex)
    {
        Assert.Throws<JSException>(() => Eval(regex + ".exec('x')"));
    }
}
