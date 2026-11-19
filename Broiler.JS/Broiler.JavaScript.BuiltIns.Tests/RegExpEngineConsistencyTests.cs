using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.RegExp;

namespace Broiler.JavaScript.BuiltIns.Tests;

/// <summary>
/// Action 4 of the Broiler.JS gaps roadmap, track 2: the legacy
/// <c>String.prototype.split</c>/<c>replace</c> fallback (reached when a RegExp's
/// <c>@@split</c>/<c>@@replace</c> is removed) and <c>assert.match</c> drive matching
/// through the same <c>RunMatch</c> abstraction <c>exec</c> uses, so every operation answers
/// with the same match data. When these tests were written a pattern was routed to the
/// Broiler.Regex backend only for a gap shape, the rest going to a .NET translation that was
/// wrong for exactly those shapes; since milestone SJL-11.5 Broiler.Regex compiles and
/// matches every pattern, and the tests' names keep the word "routed".
///
/// The distinguishing pattern is <c>(a?b??)*</c> on "ab": ECMAScript's RepeatMatcher
/// abandons an empty iteration and matches the whole "ab", while .NET keeps looping and
/// matches only "a" (issue #923 problem 8). Each case below is the answer that follows
/// from the "ab"-long match; the .NET-span answer it replaces is noted beside it.
///
/// Since SJL-5.5 (correction S3) <c>String.prototype.replace</c> no longer has that fallback: a
/// RegExp whose <c>@@replace</c> is removed is converted with ToString and searched for as text,
/// as the specification says. The replace cases therefore run the public
/// <c>JSRegExp.Replace</c> members the fallback called, which still match through the routed engine.
/// </summary>
public class RegExpEngineConsistencyTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string body)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(body).ToString();
    }

    // The public JSRegExp.Replace members, run against a live context.
    private static string PublicReplace(string pattern, string input, string template)
    {
        Load();
        using var ctx = new JSContext();
        return new JSRegExp(pattern, "").Replace(input, template);
    }

    [Fact]
    public void PublicReplace_UsesTheRoutedEngine()
    {
        // Broiler match is "ab" → "[ab]"; the .NET translation matched "a" → "[a]b".
        Assert.Equal("[ab]", PublicReplace("(a?b??)*", "ab", "[$&]"));
    }

    [Fact]
    public void PublicReplace_Functional_UsesTheRoutedEngine()
    {
        // The functional replacer receives the Broiler match ("ab"), not the .NET one ("a").
        Load();
        using var ctx = new JSContext();
        var replacer = ctx.Eval("(function (m) { return '<' + m + '>'; })");
        Assert.Equal("<ab>", new JSRegExp("(a?b??)*", "").Replace("ab", replacer));
    }

    [Fact]
    public void PublicReplace_CaptureSubstitution_UsesTheRoutedEngine()
    {
        // The whole match is "ab", but $1 is the LAST iteration's capture, "b" (matching
        // V8) — the point is that the substitution reads Broiler's normalized capture
        // array, so it agrees with exec's, not the .NET translation's.
        Assert.Equal("<ab|b>", PublicReplace("(a?b??)*", "ab", "<$&|$1>"));
    }

    [Fact]
    public void StringReplace_WithoutSymbolReplace_SearchesForTheText()
    {
        // §22.1.3.19 steps 2 and 3: with no @@replace the search value is ToString'd, so the
        // RegExp's source text is what is searched for (correction S3, SJL-5.5).
        var result = Eval(@"
            delete RegExp.prototype[Symbol.replace];
            ['ab'.replace(/(a?b??)*/, '[$&]'), 'x/(a?b??)*/'.replace(/(a?b??)*/, '[$&]')].join();
        ");
        Assert.Equal("ab,x[/(a?b??)*/]", result);
    }

    [Fact]
    public void LegacySplit_UsesTheRoutedEngine()
    {
        // The whole string is one "ab"-long match, so nothing but the empty edges remain:
        // ["", ""]. The .NET span "a" left the trailing "b" behind: ["", "b"].
        var result = Eval(@"
            delete RegExp.prototype[Symbol.split];
            JSON.stringify('ab'.split(/(?:a?b??)*/));
        ");
        Assert.Equal("[\"\",\"\"]", result);
    }

    [Fact]
    public void SymbolAndPublicReplace_Agree_ForARoutedPattern()
    {
        // The spec @@replace path (exec-driven) and the public JSRegExp.Replace must give the
        // same answer for a routed pattern, since both run the Broiler engine.
        var viaSymbol = Eval("'ab'.replace(/(a?b??)*/, '[$&]');");
        var viaPublic = PublicReplace("(a?b??)*", "ab", "[$&]");
        Assert.Equal(viaSymbol, viaPublic);
        Assert.Equal("[ab]", viaSymbol);
    }

    [Fact]
    public void IJSRegExp_IsMatch_RunsThroughTheRoutedEngine()
    {
        // The engine-agnostic IsMatch that replaced IJSRegExp.Value (which handed callers the
        // .NET Regex directly). A routed pattern — a look-behind with a capture — answers the
        // match test through Broiler.Regex, agreeing with RegExp.prototype.test.
        Load();
        using var ctx = new JSContext();

        var routed = (IJSRegExp)ctx.Eval("/(?<=(a))b/");
        Assert.True(routed.IsMatch("ab"));
        Assert.False(routed.IsMatch("xb"));

        // A pattern that was not routed, which the .NET translation answered before SJL-11.5,
        // answers the same through Broiler.Regex.
        var plain = (IJSRegExp)ctx.Eval("/\\d+/");
        Assert.True(plain.IsMatch("x42"));
        Assert.False(plain.IsMatch("abc"));
    }

    [Fact]
    public void NonRoutedReplace_IsUnchanged()
    {
        // A pattern with no gap, which the .NET translation answered before SJL-11.5; the
        // abstraction must not change its result. Global replace with a capture substitution.
        Assert.Equal("[a]1[b]2", Eval("'a1b2'.replace(/([a-z])(\\d)/g, '[$1]$2');"));
        Assert.Equal("X.X.X", Eval("'a.b.c'.replace(/[a-z]/g, 'X');"));
        Assert.Equal("[\"a\",\"b\",\"c\"]", Eval("JSON.stringify('a,b,c'.split(/,/));"));
    }

    [Fact]
    public void IJSRegExp_IsMatch_HonoursTheStickyFlag()
    {
        // IsMatch answers as exec from lastIndex 0 does, and exec tries index 0 alone for a
        // sticky pattern (ECMA-262 22.2.7.2, step 13.a), with u and without it.
        Load();
        using var ctx = new JSContext();

        Assert.False(((IJSRegExp)ctx.Eval("/a/uy")).IsMatch("ba"));
        Assert.True(((IJSRegExp)ctx.Eval("/a/uy")).IsMatch("ab"));
        Assert.False(((IJSRegExp)ctx.Eval("/a/y")).IsMatch("ba"));
        Assert.True(((IJSRegExp)ctx.Eval("/a/y")).IsMatch("ab"));
        Assert.True(((IJSRegExp)ctx.Eval("/a/u")).IsMatch("ba"));
        Assert.Equal("false,true", Eval("[/a/y.test('ba'), /a/y.test('ab')].join();"));
    }

    [Fact]
    public void PublicReplace_HonoursTheStickyFlag_AsSymbolReplaceDoes()
    {
        // RegExp.prototype[@@replace] finds a sticky pattern's matches through exec, each at
        // its start alone: a global sticky pattern replaces the leading run and stops at the
        // first gap, and a non-global one replaces nothing when index 0 does not match.
        Load();
        using var ctx = new JSContext();

        Assert.Equal("XXbaa", Eval("'aabaa'.replace(/a/gy, 'X');"));
        Assert.Equal("XXbaa", new JSRegExp("a", "gy").Replace("aabaa", "X"));
        Assert.Equal("baa", Eval("'baa'.replace(/a/y, 'X');"));
        Assert.Equal("baa", new JSRegExp("a", "y").Replace("baa", "X"));
    }

    [Fact]
    public void LegacySplit_IgnoresTheStickyFlag_AsSymbolSplitDoes()
    {
        // RegExp.prototype[@@split] builds its splitter sticky whatever the flags and tries every
        // position, so y changes nothing there; the fallback split reads it the same way.
        var viaSymbol = Eval("JSON.stringify('bab'.split(/a/y));");
        var viaFallback = Eval(@"
            delete RegExp.prototype[Symbol.split];
            JSON.stringify('bab'.split(/a/y));
        ");
        Assert.Equal("[\"b\",\"b\"]", viaSymbol);
        Assert.Equal(viaSymbol, viaFallback);
    }
}
