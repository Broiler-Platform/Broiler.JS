using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// Per-site type feedback: what an emitted property read or call actually saw over a whole run
// (docs/performance-roadmap.md item 4-1).
//
// The point of the item is that this is NOT the inline cache. The cache observes shapes to
// answer the current read and then forgets them — it replaces stale entries and drops
// everything once a site passes four shapes. Feedback has to retain, because "this site only
// ever saw one shape" is a claim about history that items 4-2 and 4-4 speculate on. So the
// tests here are about RETENTION and CLASSIFICATION, not about lookup.
//
// Shares Phase 3's collection because TypeFeedback, like the other diagnostics, is a
// process-wide side table.
[Collection(Phase3DiagnosticsCollection.Name)]
public class TypeFeedbackTests
{
    /// <summary>Runs source with feedback on, and reports what the sites saw.</summary>
    private static (TypeFeedback.Distribution Reads, TypeFeedback.Distribution Calls, string Result) Observe(string body)
    {
        using var context = new JSContext();
        using (TypeFeedback.Enable())
        {
            TypeFeedback.Reset();
            var result = context.Eval("(function(){ " + body + " })()").ToString();
            return (TypeFeedback.PropertyDistribution(), TypeFeedback.CallDistribution(), result);
        }
    }

    // ── it is off unless asked ───────────────────────────────────────────────────────

    [Fact]
    public void NothingIsRecordedWhileFeedbackIsDisabled()
    {
        TypeFeedback.Reset();
        Assert.False(TypeFeedback.Enabled);

        using (var context = new JSContext())
            context.Eval("(function(){ var o = {a:1}; var s = 0; for (var i = 0; i < 50; i++) s += o.a; return s; })()");

        Assert.Equal(0, TypeFeedback.PropertyDistribution().Observations);
        Assert.Equal(0, TypeFeedback.CallDistribution().Observations);
    }

    // A call site is emitted only while feedback is on at COMPILE time — that is what keeps a
    // call free of any extra work when the flag is clear. Compiling first and enabling after
    // must therefore record no calls, and this pins that so nobody reads a partial figure as a
    // whole one.
    [Fact]
    public void ACallCompiledBeforeFeedbackWasEnabledIsNotRecorded()
    {
        TypeFeedback.Reset();
        using var context = new JSContext();

        // Compiled with the flag clear.
        var fn = context.Eval("var f = function (x) { return x + 1; }; var g = function () { var s = 0; for (var i = 0; i < 20; i++) s += f(i); return s; }; g;");

        using (TypeFeedback.Enable())
        {
            context.Eval("g()");

            // `g()` itself is compiled inside the scope, so that ONE call is recorded. The
            // twenty `f(i)` calls in g's body were compiled before and are not — which is the
            // property being pinned: enabling the flag does not retrofit compiled code, and a
            // figure collected that way would understate the truth rather than overstate it.
            Assert.True(TypeFeedback.CallDistribution().Observations <= 1,
                $"expected only the eval'd call, got {TypeFeedback.CallDistribution().Observations}");
        }

        Assert.NotNull(fn);
    }

    // ── retention: the claim the inline cache cannot make ────────────────────────────

    [Fact]
    public void AReadThatOnlyEverSeesOneShapeIsMonomorphic()
    {
        var (reads, _, result) = Observe(
            "var o = { a: 1 }; var s = 0; for (var i = 0; i < 50; i++) { s += o.a; } return s;");

        Assert.Equal("50", result);
        Assert.True(reads.MonomorphicObservations >= 50,
            $"expected the hot read to be monomorphic, got {reads.MonomorphicObservations} of {reads.Observations}");
        Assert.Equal(0, reads.MegamorphicSites);
    }

    // Five distinct shapes at one site is past the four-entry cap, so the site is megamorphic —
    // and unlike the cache, feedback still SAYS so afterwards instead of simply stopping.
    [Fact]
    public void AReadThatSeesMoreShapesThanTheCapIsMegamorphic()
    {
        var (reads, _, result) = Observe("""
            var os = [ {a:1}, {b:1,a:1}, {c:1,a:1}, {d:1,a:1}, {e:1,a:1} ];
            var s = 0;
            for (var i = 0; i < os.length; i++) { s += os[i].a; }
            return s;
            """);

        Assert.Equal("5", result);
        Assert.True(reads.MegamorphicSites >= 1,
            $"expected a megamorphic read site, got {reads.MegamorphicSites}");
    }

    // Two shapes is polymorphic, and must not be reported as either of the extremes.
    [Fact]
    public void AReadThatSeesTwoShapesIsPolymorphic()
    {
        var (reads, _, result) = Observe("""
            var os = [ {a:1}, {b:1,a:1} ];
            var s = 0;
            for (var i = 0; i < 40; i++) { s += os[i % 2].a; }
            return s;
            """);

        Assert.Equal("40", result);
        Assert.True(reads.PolymorphicSites >= 1, "expected a polymorphic read site");
        Assert.Equal(0, reads.MegamorphicSites);
    }

    // ── callee identity ──────────────────────────────────────────────────────────────

    [Fact]
    public void ACallSiteThatOnlyEverSeesOneCalleeIsMonomorphic()
    {
        var (_, calls, result) = Observe(
            "var f = function (x) { return x + 1; }; var s = 0; for (var i = 0; i < 30; i++) { s += f(i); } return s;");

        Assert.Equal("465", result);
        Assert.True(calls.MonomorphicObservations >= 30,
            $"expected 30 monomorphic call observations, got {calls.MonomorphicObservations} of {calls.Observations}");
        Assert.Equal(0, calls.MegamorphicSites);
    }

    // The shape 4-4 cannot inline: one site, several different functions.
    [Fact]
    public void ACallSiteThatSeesManyCalleesIsMegamorphic()
    {
        var (_, calls, result) = Observe("""
            var fs = [ function(){return 1;}, function(){return 2;}, function(){return 3;},
                       function(){return 4;}, function(){return 5;} ];
            var s = 0;
            for (var i = 0; i < fs.length; i++) { s += fs[i](); }
            return s;
            """);

        Assert.Equal("15", result);
        Assert.True(calls.MegamorphicSites >= 1,
            $"expected a megamorphic call site, got {calls.MegamorphicSites}");
    }

    // ── the classification itself ────────────────────────────────────────────────────

    [Fact]
    public void ASiteThatNeverRanIsCold()
    {
        var (reads, _, result) = Observe(
            "var o = { a: 1 }; var s = 0; if (s) { s += o.a; } return s;");

        Assert.Equal("0", result);
        // The read inside the dead branch allocated a site and never ran it. Cold sites are
        // counted separately precisely so they cannot inflate the monomorphic share.
        Assert.True(reads.ColdSites >= 1, "expected at least one cold site");
    }

    // Cold sites must stay out of the shares, since a tier that specializes code nothing runs
    // has bought nothing.
    [Fact]
    public void ColdSitesAreExcludedFromTheShares()
    {
        var (reads, _, _) = Observe(
            "var o = { a: 1 }; var s = 0; for (var i = 0; i < 10; i++) { s += o.a; } if (s > 1e9) { s += o.a; } return s;");

        Assert.Equal(reads.MonomorphicSites + reads.PolymorphicSites + reads.MegamorphicSites, reads.LiveSites);
        Assert.True(reads.ColdSites >= 1);
        Assert.Equal(1.0, reads.MonomorphicObservationShare);
    }

    [Fact]
    public void ResetClearsEveryHistory()
    {
        Observe("var o = { a: 1 }; var s = 0; for (var i = 0; i < 20; i++) { s += o.a; } return s;");
        TypeFeedback.Reset();

        Assert.Equal(0, TypeFeedback.PropertyDistribution().Observations);
        Assert.Equal(0, TypeFeedback.CallDistribution().Observations);
    }

    // ── recording must not change what the program computes ──────────────────────────

    [Theory]
    [InlineData("var o = {a:1,b:2}; return o.a + o.b;", "3")]
    [InlineData("var f = function(a,b){ return a*b; }; return f(6,7);", "42")]
    [InlineData("var o = { m: function(){ return this.v; }, v: 9 }; return o.m();", "9")]
    [InlineData("function C(v){ this.v = v; } C.prototype.get = function(){ return this.v; }; return new C(4).get();", "4")]
    [InlineData("var o = null; return o?.a;", "undefined")]
    [InlineData("var o = null; return typeof o?.m();", "undefined")]
    public void TheAnswerIsTheSameWithFeedbackOnAsOff(string body, string expected)
    {
        using (var context = new JSContext())
            Assert.Equal(expected, context.Eval("(function(){ " + body + " })()").ToString());

        Assert.Equal(expected, Observe(body).Result);
    }
}
