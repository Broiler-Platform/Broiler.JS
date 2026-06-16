using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/820
//
// Problem 10 — ECMAScript per-repetition capture reset. A capturing group nested
// inside a quantified group must be cleared to undefined at the start of every
// repetition, so a capture that only participated in an earlier iteration does not
// leak into the final result. .NET's regex engine instead retains the last capture
// across repetitions, so /(z)((a+)?(b+)?(c))* /.exec("zaacbbbcac") used to report
// group 4 (the (b+)? group) as "bbb" rather than undefined.
//
// (test262: built-ins/RegExp/S15.10.2.5_A1_T4.js and
//  built-ins/RegExp/prototype/exec/S15.10.6.2_A1_T6.js)
public class Issue820Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Joins the captures of a match, mapping a non-participating (undefined) group to
    // the literal token "<undef>" so it is observable in the comparison.
    private static string Captures(string regex, string input)
        => Eval($@"
            var m = {regex}.exec({input});
            m.map(function (g) {{ return g === undefined ? '<undef>' : g; }}).join('|');");

    // The canonical test262 case: the inner (b+)? matched only in the middle iteration,
    // so it must reset to undefined for the final "ac" iteration.
    [Fact]
    public void NestedOptionalGroupResetsToUndefined()
        => Assert.Equal("zaacbbbcac|z|ac|a|<undef>|c",
            Captures(@"/(z)((a+)?(b+)?(c))*/", "\"zaacbbbcac\""));

    // Same semantics with named groups (exercises the bjsg rename + reset path).
    [Fact]
    public void NestedOptionalNamedGroupResetsToUndefined()
        => Assert.Equal("zaacbbbcac|z|ac|a|<undef>|c",
            Captures(@"/(?<z>z)((?<a>a+)?(?<b>b+)?(?<c>c))*/", "\"zaacbbbcac\""));

    // A group that DID participate in the final iteration keeps its value.
    [Fact]
    public void GroupParticipatingInFinalIterationIsRetained()
        => Assert.Equal("abab|b",
            Captures(@"/(?:a(b))*/", "\"abab\""));

    // A capture in the last iteration overwrites the earlier one (no stale value).
    [Fact]
    public void RepeatedSingleCaptureKeepsLastIteration()
        => Assert.Equal("xyz|z",
            Captures(@"/(?:(.))*/", "\"xyz\""));

    // Patterns without a capture nested in a quantifier are unaffected.
    [Fact]
    public void NonRepeatedCaptureIsUnaffected()
        => Assert.Equal("abc|b",
            Captures(@"/a(b)c/", "\"abc\""));
}
