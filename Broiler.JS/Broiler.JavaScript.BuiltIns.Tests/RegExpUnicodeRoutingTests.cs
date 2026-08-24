using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

/// <summary>
/// Action 6 of the Broiler.JS gaps roadmap, track 2: routing is widened so every
/// Unicode-mode (`u`/`v`) pattern Broiler.Regex can build runs on Broiler, not the .NET
/// translation. This fixes real match bugs the .NET path had for non-gap Unicode patterns —
/// a standalone `\p{…}` under `i` threw, and in-class case folding was missed. Quantifier
/// repetition is iterative for every body shape — a single-code-point body (`a+`, `.*`)
/// through a linear fast path, any other body (a capturing group, an alternation) through an
/// explicit-stack RepeatMatcher — so a repeat over a subject of any length matches natively
/// with no recursion and no .NET fallback.
/// </summary>
public class RegExpUnicodeRoutingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string body)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(body).ToString();
    }

    [Theory]
    // Standalone property escapes under `i` — the .NET translation threw a SyntaxError on
    // these; Broiler matches them (results are V8's).
    [InlineData(@"/\p{Lu}/iu.test('σ')", "true")]     // case closure reaches the lowercase
    [InlineData(@"/\p{Ll}/iu.test('A')", "true")]
    [InlineData(@"/\p{Script=Greek}/iu.test('Ω')", "true")]
    [InlineData(@"/\p{White_Space}/iu.test('a')", "false")]
    [InlineData(@"/\p{ASCII}/iu.test('a')", "true")]
    public void StandalonePropertyUnderIgnoreCase_NowMatches(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    [Theory]
    // In-class case folding the .NET path missed: µ (U+00B5) folds to μ, which is in [α-ω].
    [InlineData(@"/[α-ω]+/iu.test('µ')", "true")]
    [InlineData(@"/[Α-Ω]/iu.test('ω')", "true")]
    [InlineData(@"/[a-z]/iu.test('K')", "true")]      // Kelvin sign folds to k
    public void InClassCaseFoldingUnderUnicode_NowMatches(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    [Fact(Timeout = 600000)]
    public void SingleCharQuantifierOverALongSubject_MatchesNativelyViaTheIterativePath()
    {
        // /a+/u over 100k characters is a single-code-point body, so Broiler repeats it
        // iteratively and matches natively — no recursion, no .NET fallback.
        Assert.Equal("100000", Eval("'a'.repeat(100000).match(/a+/u)[0].length + ''"));
        // Greedy semantics and global iteration are preserved through the iterative path.
        Assert.Equal("XbX", Eval("'aaabaaa'.replace(/a+/gu, 'X')"));
        Assert.Equal("50000", Eval("('ab'.repeat(50000).match(/a/gu)).length + ''"));
        // `.*` over a long subject likewise no longer overflows.
        Assert.Equal("100000", Eval("'x'.repeat(100000).match(/.*/u)[0].length + ''"));
    }

    [Fact(Timeout = 600000)]
    public void ComplexBodyQuantifierOverALongSubject_MatchesNativelyViaTheIterativeDriver()
    {
        // A capturing-group body is not eligible for the fast path; it runs through the
        // explicit-stack RepeatMatcher, so a long subject matches natively — no recursion,
        // no .NET fallback — and the capture is the last iteration's.
        Assert.Equal("100000", Eval("'a'.repeat(100000).match(/(a)+/u)[0].length + ''"));
        Assert.Equal("a", Eval("'a'.repeat(100000).match(/(a)+/u)[1]"));
        // An alternation body over a long subject likewise completes natively.
        Assert.Equal("100000", Eval("'ab'.repeat(50000).match(/(?:ab|a)+/u)[0].length + ''"));
    }

    [Fact(Timeout = 600000)]
    public void NonUnicodePatterns_StayOnTheNetEngine()
    {
        // Widening is Unicode-only; a plain non-Unicode pattern keeps the fast .NET path and
        // is unchanged. (Behavioural check — the routing decision is internal.)
        Assert.Equal("X.X.X", Eval("'a.b.c'.replace(/[a-z]/g, 'X')"));
        Assert.Equal("true", Eval("/\\d{3}-\\d{4}/.test('555-1234')"));
        Assert.Equal("100000", Eval("'a'.repeat(100000).match(/a+/)[0].length + ''"));
    }
}
