using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Item 1-1's remaining half: compiling a nested function's body AFTER the enclosing compilation
// has finished, against the enclosing scope kept alive rather than snapshotted
// (docs/performance-roadmap.md item 1-1).
//
// The item names its obstacle as the capture layout, and 0104 settled that: derived from source
// alone it never misses. What is left is the part the item does not name — a deferred body must be
// compiled against twenty-eight pieces of enclosing compiler state, each a silent miscompile if
// wrong, on the least-exercised path in the engine. The design is to keep the enclosing scope
// rather than reproduce it, which FastFunctionScope makes cheap: it is not pooled, so a reference
// retains the whole Parent chain, and LinkedStack.Switch already re-enters it.
//
// **These fixtures test the re-entry and nothing else.** Retention is on, the body is still
// compiled eagerly, and the question is whether compiling it a SECOND time — later, from the
// retained context — produces the same tree. That comparison is only possible while both exist,
// which is exactly why it is made before anything is deferred: once the eager tree stops being
// produced there is nothing to check the re-entered one against.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class DeferredBodyReentryTests
{
    private static DeferredTreeCompilation.Comparison Reenter(string source)
    {
        var previous = DeferredTreeCompilation.Retaining;
        DeferredTreeCompilation.Retaining = true;
        using var context = new JSContext();
        DeferredTreeCompilation.Reset();
        try
        {
            context.Eval(source);
            return DeferredTreeCompilation.Recompile();
        }
        finally
        {
            DeferredTreeCompilation.Retaining = previous;
            DeferredTreeCompilation.Reset();
        }
    }

    private static void AssertReproduced(string source, int atLeast = 1)
    {
        var c = Reenter(source);
        Assert.True(c.Total >= atLeast, $"expected at least {atLeast} retained context(s), got {c.Total}");
        Assert.Equal(0, c.Threw);
        Assert.True(
            c.Total == c.Reproduced,
            $"{c.Total - c.Reproduced} of {c.Total} did not reproduce.\n{c.FirstDifference}");

        // Structural equality is the weaker of the two and must hold wherever the stronger does,
        // or the partition the corpus is read through is not a partition.
        Assert.Equal(c.Total, c.Structural);
    }

    [Fact(Timeout = 600000)]
    public void TheSwitchIsOffByDefault()
        => Assert.False(DeferredTreeCompilation.Retaining);

    [Fact(Timeout = 600000)]
    public void ARetainedScopeIsStillValidAfterTheEnclosingCompilationFinished()
    {
        // The whole design in one fixture: `inner`'s body is compiled a second time, from a scope
        // whose CreateFunction frame returned long ago. FastFunctionScope is not pooled, so the
        // object and its Parent chain are still there — this is what says so rather than assuming.
        AssertReproduced("""
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              return inner();
            }
            outer();
            """);
    }

    [Fact(Timeout = 600000)]
    public void ACaptureThreeLevelsDownReproduces()
    {
        AssertReproduced("""
            function outer() {
              var q = 1;
              return (function () {
                return (function () {
                  return (function () { return q; })();
                })();
              })();
            }
            outer();
            """);
    }

    [Fact(Timeout = 600000)]
    public void APerIterationLoopCellReproduces()
    {
        // The shape that makes the most cells, and where a re-entry that resolved names against
        // the wrong scope instance would bind every closure to the same one.
        AssertReproduced("""
            var fs = [];
            for (let i = 0; i < 3; i++) { fs.push(function () { return i; }); }
            fs[0]() + fs[1]() + fs[2]();
            """);
    }

    [Fact(Timeout = 600000)]
    public void AShadowedNameReproduces()
    {
        AssertReproduced("""
            function outer() {
              var q = 1;
              var inner = function (q) { return q; };
              return inner(2);
            }
            outer();
            """);
    }

    [Fact(Timeout = 600000)]
    public void StrictnessIsRestoredRatherThanInherited()
    {
        // Strictness is one of the five FastCompiler fields the context saves, and the one whose
        // loss is observable rather than structural: 4-2a records that a fresh compile of a
        // function written under 'use strict' turns `undeclaredGlobal = t` from a ReferenceError
        // into a silent global. Re-entry happens with the compiler in whatever mode the caller
        // left it, so the saved value has to win.
        AssertReproduced("""
            'use strict';
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              return inner();
            }
            outer();
            """);
    }

    [Fact(Timeout = 600000)]
    public void ARecursiveDeclarationAndANamedExpressionBothReproduce()
    {
        // The two self-name shapes 0104 had to fix in the layout. They resolve their own name
        // through different bindings, so a re-entry that mixed the two up would show here.
        AssertReproduced("""
            function outer() {
              function fact(n) { return n < 2 ? 1 : n * fact(n - 1); }
              var g = function h(n) { return n < 1 ? 0 : h(n - 1); };
              return fact(4) + g(3);
            }
            outer();
            """);
    }

    [Fact(Timeout = 600000)]
    public void ReEntryLeavesTheCompilerStateItFound()
    {
        // A deferred compile happens in the middle of somebody else's work. If the scope stack or
        // the strictness flag were left moved, the NEXT compilation would be wrong — and it would
        // be wrong in a way no fixture about the deferred function itself could see. Two programs
        // in one context, with a re-entry between them, is what makes that observable.
        var previous = DeferredTreeCompilation.Retaining;
        DeferredTreeCompilation.Retaining = true;
        using var context = new JSContext();
        DeferredTreeCompilation.Reset();
        try
        {
            context.Eval("function a() { var q = 1; return (function () { return q; })(); } a();");
            var c = DeferredTreeCompilation.Recompile();
            Assert.True(c.Total >= 1);
            Assert.Equal(c.Total, c.Reproduced);

            // The compilation that has nothing to do with the re-entry.
            Assert.Equal("7", context.Eval("function b(x) { return x + 4; } String(b(3));").ToString());
        }
        finally
        {
            DeferredTreeCompilation.Retaining = previous;
            DeferredTreeCompilation.Reset();
        }
    }

    [Fact(Timeout = 600000)]
    public void BothEqualitiesAreShownToReportADifferenceBeforeTheirZerosAreTrusted()
    {
        // **The fixture that makes `structural = 100%` mean something**, and §3.5's rule from 0096
        // applied to the second checker this item has needed: a comparison that has never reported
        // a failure is a claim about the comparison. Neither equality can be made to fail from
        // JavaScript — if it could, that would be the defect they exist to find — so both are
        // driven directly.
        const string eager = "x = PropertyInlineCacheSite.Get(7, Context3, k); LABEL_4: y = #TempJSValue9;";

        // A counter renaming, which is what a second compilation necessarily produces. Both
        // equalities must accept it or the corpus figures are measuring the counters.
        const string renamed = "x = PropertyInlineCacheSite.Get(90, Context11, k); LABEL_12: y = #TempJSValue40;";
        Assert.True(DeferredTreeCompilation.SameUpToCounters(eager, renamed));
        Assert.True(DeferredTreeCompilation.SameStructurally(eager, renamed));

        // A token no counter produced — a different property being read. Both must reject it,
        // including the weaker one, or `structural` is not a statement about the tree at all.
        const string different = "x = PropertyInlineCacheSite.Get(7, Context3, OTHER); LABEL_4: y = #TempJSValue9;";
        Assert.False(DeferredTreeCompilation.SameUpToCounters(eager, different));
        Assert.False(DeferredTreeCompilation.SameStructurally(eager, different));

        // **And the exact gap between the two, asserted rather than described.** Here one side
        // reads the SAME site twice where the other reads two distinct ones. The strong equality
        // reports it, because ordinals are assigned by first appearance; the weak one cannot,
        // because it erases the numbers. That is the whole of what `structural` buys over
        // `reproduced` being unreliable — and it is why the corpus's 471 non-reproducing functions
        // are classified by the raw sequences rather than left inside the weaker number.
        const string reused = "x = PropertyInlineCacheSite.Get(7, Context3, k); LABEL_4: y = #TempJSValue9; z = PropertyInlineCacheSite.Get(7, Context3, k);";
        const string fresh = "x = PropertyInlineCacheSite.Get(7, Context3, k); LABEL_4: y = #TempJSValue9; z = PropertyInlineCacheSite.Get(8, Context3, k);";
        Assert.False(DeferredTreeCompilation.SameUpToCounters(reused, fresh));
        Assert.True(DeferredTreeCompilation.SameStructurally(reused, fresh));
    }

    [Fact(Timeout = 600000)]
    public void EachGensymFamilyIsCanonicalisedAgainstItsOwnTable()
    {
        // **The defect the corpus run found in this comparison itself.** The families were
        // canonicalised in one pass whose ordinal table was keyed on the bare NUMBER, so
        // `Context3` and `#TempJSValue3` shared an entry. Two families drawing the same number on
        // one side and not the other then desynchronised every ordinal after it, reporting a
        // difference that was an artifact of the comparison — worth 5 of 6 corpora going from
        // 96.6-99.9% to 100.0% once the tables were separated. The families do not share a
        // counter; the table must not either.
        const string eager = "Context3 -> #TempJSValue3 -> Context4";
        const string reentered = "Context7 -> #TempJSValue9 -> Context8";
        Assert.True(DeferredTreeCompilation.SameUpToCounters(eager, reentered));
    }

    [Fact(Timeout = 600000)]
    public void RetentionChangesNoAnswer()
    {
        // Retention is meant to be inert: the body is still compiled eagerly and the switch only
        // decides whether a context is kept. Asserted rather than assumed, because "it only holds
        // a reference" is the kind of claim that stops being true the first time someone makes the
        // held thing do something.
        const string source = """
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              var tierable = function (n) { var s = 0; for (var i = 0; i < n; i++) { s = s + i; } return s; };
              return inner() + tierable(10);
            }
            String(outer());
            """;

        static string Run(bool retaining)
        {
            var previous = DeferredTreeCompilation.Retaining;
            DeferredTreeCompilation.Retaining = retaining;
            try
            {
                using var context = new JSContext();
                return context.Eval(source).ToString();
            }
            finally
            {
                DeferredTreeCompilation.Retaining = previous;
                DeferredTreeCompilation.Reset();
            }
        }

        Assert.Equal(Run(false), Run(true));
    }
}
