using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

/// <summary>
/// Action 4 of the Broiler.JS gaps roadmap, track 2: the legacy
/// <c>String.prototype.split</c>/<c>replace</c> fallback (reached when a RegExp's
/// <c>@@split</c>/<c>@@replace</c> is removed) and <c>assert.match</c> now drive matching
/// through the same <c>RunMatch</c> abstraction <c>exec</c> uses, so a pattern routed to
/// the Broiler.Regex backend answers every operation with the same match data — not the
/// .NET translation, which is wrong for exactly the gap patterns that get routed.
///
/// The distinguishing pattern is <c>(a?b??)*</c> on "ab": ECMAScript's RepeatMatcher
/// abandons an empty iteration and matches the whole "ab", while .NET keeps looping and
/// matches only "a" (issue #923 problem 8). Each case below is the answer that follows
/// from the "ab"-long match; the .NET-span answer it replaces is noted beside it.
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

    [Fact(Timeout = 600000)]
    public void LegacyStringReplace_UsesTheRoutedEngine()
    {
        // Broiler match is "ab" → "[ab]"; the .NET translation matched "a" → "[a]b".
        var result = Eval(@"
            delete RegExp.prototype[Symbol.replace];
            'ab'.replace(/(a?b??)*/, '[$&]');
        ");
        Assert.Equal("[ab]", result);
    }

    [Fact(Timeout = 600000)]
    public void LegacyStringReplace_Functional_UsesTheRoutedEngine()
    {
        // The functional replacer receives the Broiler match ("ab"), not the .NET one ("a").
        var result = Eval(@"
            delete RegExp.prototype[Symbol.replace];
            'ab'.replace(/(a?b??)*/, function (m) { return '<' + m + '>'; });
        ");
        Assert.Equal("<ab>", result);
    }

    [Fact(Timeout = 600000)]
    public void LegacyStringReplace_CaptureSubstitution_UsesTheRoutedEngine()
    {
        // The whole match is "ab", but $1 is the LAST iteration's capture, "b" (matching
        // V8) — the point is that the substitution reads Broiler's normalized capture
        // array, so it agrees with exec's, not the .NET translation's.
        var result = Eval(@"
            delete RegExp.prototype[Symbol.replace];
            'ab'.replace(/(a?b??)*/, '<$&|$1>');
        ");
        Assert.Equal("<ab|b>", result);
    }

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void SymbolAndLegacyReplace_Agree_ForARoutedPattern()
    {
        // The spec @@replace path (exec-driven) and the legacy fallback must now give the
        // same answer for a routed pattern, since both run the Broiler engine.
        var viaSymbol = Eval("'ab'.replace(/(a?b??)*/, '[$&]');");
        var viaLegacy = Eval(@"
            delete RegExp.prototype[Symbol.replace];
            'ab'.replace(/(a?b??)*/, '[$&]');
        ");
        Assert.Equal(viaSymbol, viaLegacy);
        Assert.Equal("[ab]", viaSymbol);
    }

    [Fact(Timeout = 600000)]
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

        // A non-routed pattern still answers through the .NET translation, unchanged.
        var plain = (IJSRegExp)ctx.Eval("/\\d+/");
        Assert.True(plain.IsMatch("x42"));
        Assert.False(plain.IsMatch("abc"));
    }

    [Fact(Timeout = 600000)]
    public void NonRoutedReplace_IsUnchanged()
    {
        // A pattern with no gap keeps using the .NET translation; the abstraction must not
        // change its result. Global replace with a capture substitution.
        Assert.Equal("[a]1[b]2", Eval("'a1b2'.replace(/([a-z])(\\d)/g, '[$1]$2');"));
        Assert.Equal("X.X.X", Eval("'a.b.c'.replace(/[a-z]/g, 'X');"));
        Assert.Equal("[\"a\",\"b\",\"c\"]", Eval("JSON.stringify('a,b,c'.split(/,/));"));
    }
}
