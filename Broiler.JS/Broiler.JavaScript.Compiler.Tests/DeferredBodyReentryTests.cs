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
    private static (int Total, int Reproduced, string FirstDifference) Reenter(string source)
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
        var (total, reproduced, difference) = Reenter(source);
        Assert.True(total >= atLeast, $"expected at least {atLeast} retained context(s), got {total}");
        Assert.True(
            total == reproduced,
            $"{total - reproduced} of {total} did not reproduce.\n{difference}");
    }

    [Fact]
    public void TheSwitchIsOffByDefault()
        => Assert.False(DeferredTreeCompilation.Retaining);

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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
            var (total, reproduced, _) = DeferredTreeCompilation.Recompile();
            Assert.True(total >= 1);
            Assert.Equal(total, reproduced);

            // The compilation that has nothing to do with the re-entry.
            Assert.Equal("7", context.Eval("function b(x) { return x + 4; } String(b(3));").ToString());
        }
        finally
        {
            DeferredTreeCompilation.Retaining = previous;
            DeferredTreeCompilation.Reset();
        }
    }

    [Fact]
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
