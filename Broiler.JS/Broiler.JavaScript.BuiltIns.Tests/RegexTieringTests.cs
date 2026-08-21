using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Phase 5, item 2: a hot pattern races `RegexOptions.Compiled` against the interpreted form on
// the subject the program actually handed it, and keeps the winner
// (docs/performance-roadmap.md).
//
// What these tests are for is the one property the mechanism rests on: the arm the race picks
// is unobservable. `RegexOptions.Compiled` changes code generation and nothing else, so every
// case below asserts the SAME answer on both settings of the switch — which is what makes a
// timing-driven decision admissible in a language runtime at all. They are a regression guard
// and not a fit to the change: run with tiering off, they all pass on the unmodified engine.
//
// The cases are chosen where a codegen difference would show if there were one: capture
// contents and order, named groups, `lastIndex` progression across a global exec loop, sticky
// re-anchoring, backreferences, `replace` with a function, `split`, the Annex B statics, and
// `RegExp.prototype.compile` replacing the matcher on a live object after it has already gone
// hot.
public class RegexTieringTests : IDisposable
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private readonly bool tieringWasEnabled = RegexTiering.Enabled;
    private readonly bool diagnosticsWereEnabled = RegexTieringDiagnostics.Enabled;

    public void Dispose()
    {
        RegexTiering.Enabled = tieringWasEnabled;
        RegexTieringDiagnostics.Enabled = diagnosticsWereEnabled;
        RegexTiering.ResetForTests();
        RegexTieringDiagnostics.Reset();
        GC.SuppressFinalize(this);
    }

    private static string Eval(string expression, bool tiering)
    {
        Load();
        RegexTiering.ResetForTests();
        RegexTiering.Enabled = tiering;
        using var context = new JSContext();
        return context.Eval(expression).ToString();
    }

    /// <summary>
    /// Enough matches through one instance to take it past the promotion threshold, so the
    /// expression below the loop runs on whichever arm the race chose.
    /// </summary>
    /// <remarks>
    /// Spelled as a literal because <c>[InlineData]</c> takes compile-time constants and a
    /// string built from an <c>int</c> is not one. <see cref="WarmUpExceedsThreshold"/> is what
    /// keeps it honest: raise <see cref="RegexTiering.PromotionThreshold"/> past 1 050 and that
    /// test fails rather than these silently ceasing to exercise a promotion at all.
    /// </remarks>
    private const string PastThreshold = "var warm = 0; for (var i = 0; i < 1050; i++) ";

    private const int WarmUpMatches = 1_050;

    [Fact(Timeout = 600000)]
    public void WarmUpExceedsThreshold()
        => Assert.True(WarmUpMatches > RegexTiering.PromotionThreshold);

    [Theory]
    // The whole point: the same source, hot, must read identically on both settings.
    [InlineData(
        "var re = /(\\w)(\\d)/; " + PastThreshold + "re.test('a1'); JSON.stringify(re.exec('z9'))",
        "[\"z9\",\"z\",\"9\"]")]
    [InlineData(
        "var re = /(?<letter>\\w)(?<digit>\\d)/; " + PastThreshold + "re.test('a1'); "
            + "re.exec('z9').groups.letter + re.exec('z9').groups.digit",
        "z9")]
    // A global regex's lastIndex must still walk the subject one match at a time.
    [InlineData(
        "var re = /a/g; " + PastThreshold + "{ re.lastIndex = 0; re.test('aaa'); } "
            + "re.lastIndex = 0; var seen = []; var m; while ((m = re.exec('abaca')) !== null) seen.push(m.index); "
            + "seen.join(',')",
        "0,2,4")]
    // Sticky must still refuse a match that does not start exactly at lastIndex, and still take
    // the one that does — both directions, because a promotion that broke anchoring would only
    // show on one of them.
    [InlineData(
        "var re = /a/y; " + PastThreshold + "{ re.lastIndex = 0; re.test('a'); } "
            + "re.lastIndex = 0; var atZero = String(re.exec('ba')); "
            + "re.lastIndex = 1; var atOne = re.exec('ba')[0]; atZero + ',' + atOne",
        "null,a")]
    [InlineData(
        "var re = /(a)\\1/; " + PastThreshold + "re.test('aa'); String(re.test('ab'))",
        "false")]
    // The anchored class quantifier that measured 4.3x SLOWER compiled — the pattern the race
    // exists to refuse. Whichever way it goes, trim is still trim.
    [InlineData(
        "var re = /^[\\s\\xa0]+|[\\s\\xa0]+$/g; " + PastThreshold + "{ re.lastIndex = 0; re.test('  x  '); } "
            + "'  padded  '.replace(re, '')",
        "padded")]
    [InlineData(
        "var re = /(\\d+)/g; " + PastThreshold + "{ re.lastIndex = 0; re.test('1'); } "
            + "'a1b22c333'.replace(re, function (m, d) { return '<' + d.length + '>'; })",
        "a<1>b<2>c<3>")]
    [InlineData(
        "var re = /[,;]/; " + PastThreshold + "re.test('a,b'); 'a,b;c'.split(re).join('|')",
        "a|b|c")]
    // RegExp.prototype.compile replaces the matcher on a live object; the new pattern must not
    // inherit the old one's countdown, its verdict, or its matcher.
    [InlineData(
        "var re = /a/; " + PastThreshold + "re.test('a'); re.compile('b'); "
            + "String(re.test('a')) + ',' + String(re.test('b'))",
        "false,true")]
    public void SameAnswerOnBothSettings(string expression, string expected)
    {
        Assert.Equal(expected, Eval(expression, tiering: false));
        Assert.Equal(expected, Eval(expression, tiering: true));
    }

    [Fact(Timeout = 600000)]
    public void HotPattern_RacesExactlyOnce_AndSiblingsInheritTheVerdict()
    {
        Load();
        RegexTiering.ResetForTests();
        RegexTieringDiagnostics.Reset();
        RegexTiering.Enabled = true;
        RegexTieringDiagnostics.Enabled = true;

        try
        {
            using var context = new JSContext();
            // Three separate instances of one pattern, each taken past the threshold. The first
            // races; the other two must find the verdict already recorded rather than build and
            // time a second and third compiled form of the same pattern.
            context.Eval(
                "for (var k = 0; k < 3; k++) { var re = new RegExp('(\\\\w+)@(\\\\w+)'); "
                + "for (var i = 0; i < " + (RegexTiering.PromotionThreshold + 5) + "; i++) re.test('user@host'); }");

            Assert.Equal(1L, RegexTieringDiagnostics.RacesRun);
            Assert.Equal(2L, RegexTieringDiagnostics.VerdictsReused);
            Assert.InRange(RegexTieringDiagnostics.RaceRounds, 1L, 8L);
        }
        finally
        {
            RegexTiering.Enabled = false;
            RegexTieringDiagnostics.Enabled = false;
        }
    }

    [Fact(Timeout = 600000)]
    public void ColdPattern_NeverRaces()
    {
        Load();
        RegexTiering.ResetForTests();
        RegexTieringDiagnostics.Reset();
        RegexTiering.Enabled = true;
        RegexTieringDiagnostics.Enabled = true;

        try
        {
            using var context = new JSContext();
            context.Eval(
                "var re = /abc/; for (var i = 0; i < " + (RegexTiering.PromotionThreshold - 1) + "; i++) re.test('abc');");

            // One match short of the threshold builds nothing and times nothing: a pattern that
            // is never hot must not pay for a decision it cannot repay.
            Assert.Equal(0L, RegexTieringDiagnostics.RacesRun);
            Assert.Equal(0L, RegexTieringDiagnostics.VerdictsReused);
        }
        finally
        {
            RegexTiering.Enabled = false;
            RegexTieringDiagnostics.Enabled = false;
        }
    }

    [Fact(Timeout = 600000)]
    public void Disabled_ByDefault_RacesNothingHowEverHotThePatternGets()
    {
        Load();
        RegexTiering.ResetForTests();
        RegexTieringDiagnostics.Reset();
        RegexTiering.Enabled = false;
        RegexTieringDiagnostics.Enabled = true;

        try
        {
            using var context = new JSContext();
            context.Eval(
                "var re = /abc/; for (var i = 0; i < " + (RegexTiering.PromotionThreshold * 3) + "; i++) re.test('abc');");

            Assert.Equal(0L, RegexTieringDiagnostics.RacesRun);
        }
        finally
        {
            RegexTieringDiagnostics.Enabled = false;
        }
    }

    [Fact(Timeout = 600000)]
    public void GapFeaturePattern_RoutedToBroiler_IsNeverRaced()
    {
        Load();
        RegexTiering.ResetForTests();
        RegexTieringDiagnostics.Reset();
        RegexTiering.Enabled = true;
        RegexTieringDiagnostics.Enabled = true;

        try
        {
            using var context = new JSContext();
            // A capturing group inside a look-behind is one of the gaps JSRegExp routes to
            // Broiler.Regex, which has no compiled form to choose between.
            var result = context.Eval(
                "var re = /(?<=(\\d))x/; var last = null; "
                + "for (var i = 0; i < " + (RegexTiering.PromotionThreshold + 5) + "; i++) last = re.exec('7x'); "
                + "last[1]").ToString();

            Assert.Equal("7", result);
            Assert.Equal(0L, RegexTieringDiagnostics.RacesRun);
        }
        finally
        {
            RegexTiering.Enabled = false;
            RegexTieringDiagnostics.Enabled = false;
        }
    }

    [Fact(Timeout = 600000)]
    public void AnnexBStatics_SurviveAPromotion()
    {
        // RegExp.$1 and friends are written from the match data, which the promoted arm produces
        // in the same shape. A codegen swap that reordered captures would show here first.
        var source =
            "var re = /(\\d+)-(\\d+)/; "
            + "for (var i = 0; i < " + (RegexTiering.PromotionThreshold + 5) + "; i++) re.test('1-2'); "
            + "re.exec('40-2'); RegExp.$1 + '/' + RegExp.$2";

        Assert.Equal("40/2", Eval(source, tiering: false));
        Assert.Equal("40/2", Eval(source, tiering: true));
    }
}
