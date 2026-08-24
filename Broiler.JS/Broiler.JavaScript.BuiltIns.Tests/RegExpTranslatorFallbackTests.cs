using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

/// <summary>
/// The tail of Action 4 (Broiler.JS gaps roadmap, track 2): a pattern routed to the
/// Broiler.Regex backend no longer has to be translatable to a .NET <c>Regex</c> as well.
/// When a transform or <c>new Regex</c> cannot represent a routed pattern, <c>CreateRegex</c>
/// runs with a null .NET engine and every operation reads Broiler.
///
/// The `v`-mode set operations `[\s&&\S]`, `[\d&&\s]` and `[\s--\d]` are the concrete case:
/// Broiler evaluates them, but the .NET UnicodeSets translator throws "not supported" for a
/// built-in class escape inside a set operation, so before this change constructing the
/// RegExp threw a SyntaxError even though the pattern is valid ECMAScript.
/// </summary>
public class RegExpTranslatorFallbackTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string body)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(body).ToString();
    }

    [Theory]
    // A set operation over built-in class escapes — the translator rejects these, so they
    // exercise the null-.NET-engine path. Each result is V8's.
    [InlineData(@"[\s&&\S]", " ", "false")]  // whitespace ∩ non-whitespace = ∅
    [InlineData(@"[\s&&\S]", "a", "false")]
    [InlineData(@"[\d&&\s]", "5", "false")]  // digit ∩ whitespace = ∅
    [InlineData(@"[\s--\d]", " ", "true")]   // whitespace minus digits still holds a space
    [InlineData(@"[\s--\d]", "5", "false")]
    [InlineData(@"[\s--\d]", "a", "false")]
    public void RoutedButUntranslatable_ConstructsAndMatches(string pattern, string input, string expected)
        => Assert.Equal(expected, Eval($"/{pattern}/v.test({System.Text.Json.JsonSerializer.Serialize(input)})"));

    [Fact(Timeout = 600000)]
    public void RoutedButUntranslatable_DrivesExecSplitAndReplace()
    {
        // exec reports the matched whitespace (as a one-element result array)…
        Assert.Equal("[\" \"]", Eval(@"JSON.stringify(/[\s--\d]/v.exec('x y'));"));
        // …split and replace agree (they read the same Broiler match data)…
        Assert.Equal("[\"a\",\"b\",\"c\"]", Eval(@"JSON.stringify('a b\tc'.split(/[\s--\d]/v));"));
        Assert.Equal("a_b_5", Eval(@"'a b 5'.replace(/[\s--\d]/gv, '_');"));
        // …and the empty-set class matches nothing, so replace is a no-op.
        Assert.Equal("a b", Eval(@"'a b'.replace(/[\s&&\S]/gv, 'X');"));
    }

    [Fact(Timeout = 600000)]
    public void RoutedButUntranslatable_HasNoNetEngine()
    {
        // A routed pattern the translator cannot represent carries a null .NET Regex; the
        // engine still answers every query. (The .toString/source come from the stored
        // pattern text, not the .NET engine.)
        Assert.Equal(@"/[\s--\d]/v", Eval(@"String(/[\s--\d]/v);"));
        Assert.Equal(@"[\s--\d]", Eval(@"/[\s--\d]/v.source;"));
        Assert.Equal("v", Eval(@"/[\s--\d]/v.flags;"));
    }

    [Theory]
    // A wider batch of `v`-mode set operations over built-in class escapes, each compared to
    // V8. The membership string is test() over the inputs [space, tab, 'a', '5', '_', '!'].
    // Several of these throw in the .NET translator and so exercise the null-engine path;
    // all must agree with V8's evaluated set.
    [InlineData(@"[\s&&\S]", "000000")]
    [InlineData(@"[\d&&\s]", "000000")]
    [InlineData(@"[\s--\d]", "110000")]
    [InlineData(@"[\w&&\s]", "000000")]
    [InlineData(@"[\d--\s]", "000100")]
    [InlineData(@"[\S&&\w]", "001110")]
    [InlineData(@"[\w--\d]", "001010")]
    [InlineData(@"[[\s\d]--\d]", "110000")]
    [InlineData(@"[\s&&[\t ]]", "110000")]
    [InlineData(@"[\D--\s]", "001011")]
    [InlineData(@"[\w&&\D]", "001010")]
    [InlineData(@"[[\w]--[\s]]", "001110")]
    public void VModeSetOperations_MatchV8(string pattern, string expected)
        => Assert.Equal(expected, Eval(
            $"(function(){{ var r=/{pattern}/v; return [' ','\\t','a','5','_','!'].map(function(c){{ return r.test(c)?'1':'0'; }}).join(''); }})()"));

    [Theory]
    // The fallback must not swallow a genuine error: a pattern invalid in both engines is
    // not routed (Broiler rejects it, so `broiler` is null and the inner catch is skipped),
    // and the SyntaxError still surfaces.
    [InlineData(@"[\s&&", "v")]   // unterminated class
    [InlineData("(", "")]          // unterminated group
    [InlineData(@"\p{Bogus}", "u")] // unknown Unicode property
    public void InvalidPattern_StillThrowsSyntaxError(string pattern, string flags)
    {
        var threw = Eval($"(function(){{ try {{ new RegExp({System.Text.Json.JsonSerializer.Serialize(pattern)}, {System.Text.Json.JsonSerializer.Serialize(flags)}); return 'made'; }} catch (e) {{ return e instanceof SyntaxError ? 'SyntaxError' : 'Other:' + e; }} }})()");
        Assert.Equal("SyntaxError", threw);
    }
}
