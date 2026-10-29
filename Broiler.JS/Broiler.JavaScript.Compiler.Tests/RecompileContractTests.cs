using System;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The RECOMPILE contract the tier-2 hook runs under (docs/performance-roadmap.md item 4-2a).
//
// Promotion re-parses the function's SOURCE TEXT as a fresh top-level script and keeps the
// delegate that falls out. §4 already says that path "recompiles the same code the same way, so
// it cannot be faster" — what nothing said is that it does not recompile the same code the same
// way at all. A fresh compilation does not reproduce the scope the function was written in, and
// two things follow from that:
//
//   1. it produces a SECOND function object, so a body that can observe its own function object
//      observes the copy while the rest of the program still holds the original — and the two
//      differ in every own property the program installed;
//   2. strictness is inherited from the enclosing script, so a strict function re-parsed at the
//      top level of a fresh script comes back sloppy.
//
// Neither was stated, and both were wrong in the tree: (1) is what killed Octane's DeltaBlue,
// whose constructors read `X.superConstructor.call(this, ...)` off their own function object.
// (1) is refused (TieringRecompileContract), (2) is reproduced (the wrapper re-states the
// directive). Every test here runs the same source through a tiered and an untiered context and
// requires the two to agree — the untiered answer is the specification.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class RecompileContractTests
{
    private static JSContext Tiered(int threshold = 2)
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseFunctionTiering(new FunctionTieringOptions
            {
                Enabled = true,
                InvocationThreshold = threshold,
                MaxRecompilations = 64,
                MaxRetainedCodeBytes = 8L * 1024 * 1024,
            })
            .Build();

    private static JSContext Untiered()
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();

    private static string Run(JSContext context, string source)
    {
        try
        {
            return context.Eval(source).ToString();
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }

    /// <summary>
    /// Runs <paramref name="source"/> with and without tiering and asserts the answers agree,
    /// returning the tiering snapshot so a caller can also say whether the function was promoted
    /// or refused. Agreement alone is not enough — a refusal and a bug both produce it.
    /// </summary>
    private static FunctionTieringSnapshot AssertSameAnswer(string source, string expected)
    {
        using var untiered = Untiered();
        using var tiered = Tiered();

        var control = Run(untiered, source);
        Assert.Equal(expected, control);
        Assert.Equal(control, Run(tiered, source));
        return tiered.FunctionTiering.Snapshot();
    }

    // ── condition 1: the body must not be able to observe its own function object ─────

    // A function DECLARATION. Wrapped as `({source})` the declaration becomes a named function
    // EXPRESSION, whose self-name binding shadows the outer binding the body meant — so `tally`
    // names the copy, which has no `step`. Before the contract: 6|NaN|NaN|NaN.
    [Fact]
    public void ADeclarationReadingAPropertyOfItsOwnNameIsRefused()
    {
        var snapshot = AssertSameAnswer("""
            function tally(n) { var t = 0; for (var i = 0; i < n; i++) t += tally.step; return t; }
            tally.step = 2;
            [tally(3), tally(3), tally(3), tally(3)].join('|');
            """, "6|6|6|6");

        Assert.Equal(0, snapshot.Candidates);
        Assert.Equal(0, snapshot.Recompilations);
    }

    // A named function EXPRESSION, where the self-name binding is genuine rather than an artifact
    // of the wrapper — and still binds the copy. Same wrong answer, opposite reason, which is why
    // the refusal covers both rather than distinguishing them.
    [Fact]
    public void ANamedFunctionExpressionReadingItsOwnNameIsRefused()
    {
        var snapshot = AssertSameAnswer("""
            var tally = function named(n) { var t = 0; for (var i = 0; i < n; i++) t += named.step; return t; };
            tally.step = 2;
            [tally(3), tally(3), tally(3), tally(3)].join('|');
            """, "6|6|6|6");

        Assert.Equal(0, snapshot.Candidates);
        Assert.Equal(0, snapshot.Recompilations);
    }

    // Identity, not just properties: the copy is a different object, so `===` against the
    // original answers false from the second call on. Before the contract: true|false|false|false.
    [Fact]
    public void IdentityAgainstItsOwnNameIsRefused()
    {
        var snapshot = AssertSameAnswer("""
            function self(n) { var t = n; return self === globalThis.captured; }
            globalThis.captured = self;
            [self(1), self(1), self(1), self(1)].join('|');
            """, "true|true|true|true");

        Assert.Equal(0, snapshot.Candidates);
    }

    // `arguments.callee` is the function object again by a route no name check can see, and it
    // can be reached through an alias — so any mention of `arguments` is refused, not just the
    // direct member access. Before the contract: true|false|false|false.
    [Fact]
    public void ArgumentsCalleeIsRefused()
    {
        var snapshot = AssertSameAnswer("""
            function who(n) { var t = n; return arguments.callee === who; }
            [who(1), who(1), who(1), who(1)].join('|');
            """, "true|true|true|true");

        Assert.Equal(0, snapshot.Candidates);
    }

    [Fact]
    public void AnAliasedArgumentsObjectIsRefusedToo()
    {
        var snapshot = AssertSameAnswer("""
            function who(n) { var a = arguments; return a.callee === who; }
            [who(1), who(1), who(1), who(1)].join('|');
            """, "true|true|true|true");

        Assert.Equal(0, snapshot.Candidates);
    }

    // THE CONTROL, and it is what makes every refusal above mean something: the same body with
    // the self-reference replaced by an ordinary global read IS a candidate and IS promoted. A
    // refusal that was really the gate rejecting the shape for some unrelated reason would fail
    // here, and the tests above would be passing vacuously.
    [Fact]
    public void TheSameBodyWithoutASelfReferenceIsStillPromoted()
    {
        var snapshot = AssertSameAnswer("""
            var holder = { step: 2 };
            function tally(n) { var t = 0; for (var i = 0; i < n; i++) t += holder.step; return t; }
            [tally(3), tally(3), tally(3), tally(3)].join('|');
            """, "6|6|6|6");

        Assert.True(snapshot.Candidates >= 1, $"expected a candidate, got {snapshot.Candidates}");
        Assert.Equal(1, snapshot.Recompilations);
    }

    // The cost of the rule, named rather than discovered. Recursion by name is NOT a wrong
    // answer — the copy calling the copy computes the same thing — but the refusal is keyed on
    // the name being mentioned at all, which is the conservative side of a rule that cannot tell
    // "invoked" from "observed" without a use analysis. Pinned so the cost stays visible.
    [Fact]
    public void RecursionByNameIsRefusedEvenThoughItWouldHaveBeenCorrect()
    {
        var snapshot = AssertSameAnswer("""
            function fact(n) { var t = n <= 1 ? 1 : n * fact(n - 1); return t; }
            [fact(5), fact(5), fact(5), fact(5)].join('|');
            """, "120|120|120|120");

        Assert.Equal(0, snapshot.Candidates);
    }

    // AstReduce treats three compact structs as LEAVES, so a detector that inherits that
    // treatment stops looking inside them. The first draft of this contract did, and admitted
    // every one of these three while refusing the same reference written as a statement — the
    // "did not look reads as did not find" failure, one level down. One case per leaf kind.
    [Theory]
    // VariableDeclarator.Init — how the recursion case actually hid.
    [InlineData("function f(n) { var t = n <= 1 ? 1 : f.step; return t; }\nf.step = 6;\n[f(5), f(5), f(5), f(5)].join('|');", "6|6|6|6")]
    // ObjectProperty.Value.
    [InlineData("function f(n) { var o; o = { v: f.step }; return o.v; }\nf.step = 6;\n[f(5), f(5), f(5), f(5)].join('|');", "6|6|6|6")]
    // Case.Statements.
    [InlineData("function f(n) { var t = 0; switch (n) { case 5: t = f.step; break; }\nreturn t; }\nf.step = 6;\n[f(5), f(5), f(5), f(5)].join('|');", "6|6|6|6")]
    public void ASelfReferenceHiddenInACompactStructIsStillFound(string source, string expected)
    {
        var snapshot = AssertSameAnswer(source, expected);
        Assert.Equal(0, snapshot.Candidates);
    }

    // An anonymous function has no name to observe, so nothing is refused on that account.
    [Fact]
    public void AnAnonymousFunctionExpressionIsStillPromoted()
    {
        var snapshot = AssertSameAnswer("""
            var tally = function (n) { var t = 0; for (var i = 0; i < n; i++) t += 2; return t; };
            [tally(3), tally(3), tally(3), tally(3)].join('|');
            """, "6|6|6|6");

        Assert.True(snapshot.Candidates >= 1, $"expected a candidate, got {snapshot.Candidates}");
        Assert.Equal(1, snapshot.Recompilations);
    }

    // A name that only appears as a LOCAL of the same spelling is still refused: the detector
    // matches the identifier, not the binding it resolves to. Conservative on purpose — deciding
    // that this mention is a different binding needs the scope walk the contract deliberately
    // does not do.
    [Fact]
    public void AShadowedNameIsRefusedConservatively()
    {
        var snapshot = AssertSameAnswer("""
            function tally(n) { var tally = 2, t = 0; for (var i = 0; i < n; i++) t += tally; return t; }
            [tally(3), tally(3), tally(3), tally(3)].join('|');
            """, "6|6|6|6");

        Assert.Equal(0, snapshot.Candidates);
    }

    // ── condition 2: strictness is inherited, so the recompile has to re-state it ─────

    // The one condition the recompile can REPRODUCE rather than having to refuse. A strict
    // function carries no directive of its own, so re-parsing its text at the top level of a
    // fresh script makes the copy sloppy: before the fix this answered
    // ReferenceError|ok|ok — the promoted body silently created a global.
    [Fact]
    public void AStrictFunctionStaysStrictAfterPromotion()
    {
        var snapshot = AssertSameAnswer("""
            'use strict';
            function poke(n) { var t = n; undeclaredGlobal = t; return t; }
            var out = [];
            for (var i = 0; i < 4; i++) { try { poke(1); out.push('ok'); } catch (e) { out.push(e.name); } }
            out.join('|');
            """, "ReferenceError|ReferenceError|ReferenceError|ReferenceError");

        // Repaired, not refused — the function is still promoted.
        Assert.Equal(1, snapshot.Recompilations);
    }

    // A function with its OWN directive was already correct, and stays correct: the wrapper adds
    // a second one, which is a no-op rather than a change.
    [Fact]
    public void AFunctionWithItsOwnDirectiveIsUnaffected()
    {
        var snapshot = AssertSameAnswer("""
            function poke(n) { 'use strict'; var t = n; undeclaredGlobal = t; return t; }
            var out = [];
            for (var i = 0; i < 4; i++) { try { poke(1); out.push('ok'); } catch (e) { out.push(e.name); } }
            out.join('|');
            """, "ReferenceError|ReferenceError|ReferenceError|ReferenceError");

        Assert.Equal(1, snapshot.Recompilations);
    }

    // A sloppy function must NOT be made strict by the repair — the wrapper is conditional, and
    // this is what says so. A sloppy assignment to an undeclared name creates a global.
    [Fact]
    public void ASloppyFunctionIsNotMadeStrict()
    {
        var snapshot = AssertSameAnswer("""
            function poke(n) { var t = n; undeclaredGlobal = t; return t; }
            var out = [];
            for (var i = 0; i < 4; i++) { try { poke(1); out.push('ok'); } catch (e) { out.push(e.name); } }
            out.join('|') + '|' + undeclaredGlobal;
            """, "ok|ok|ok|ok|1");

        Assert.Equal(1, snapshot.Recompilations);
    }

    // ── conditions that already held, kept as regression pins ─────────────────────────

    // These four were probed alongside the four defects and agreed already. They are kept
    // because "this one was fine" is only useful if it stays fine: each is a way the fresh
    // compilation could have failed to reproduce the original scope, and each is one refactor
    // away from mattering.
    [Theory]
    // A top-level `const` is a script-level lexical binding, not a global object property.
    [InlineData("const step = 2;\nfunction tally(n) { var t = 0; for (var i = 0; i < n; i++) t += step; return t; }\n[tally(3), tally(3), tally(3), tally(3)].join('|');", "6|6|6|6")]
    // A class binding is lexical in the same way.
    [InlineData("class Box { constructor() { this.v = 2; } }\nfunction make() { return new Box().v; }\n[make(), make(), make(), make()].join('|');", "2|2|2|2")]
    // `this` in a strict function must stay undefined rather than being coerced to the global.
    [InlineData("'use strict';\nfunction what() { return typeof this; }\n[what(), what(), what(), what()].join('|');", "undefined|undefined|undefined|undefined")]
    // A default parameter initializer resolving an outer binding.
    [InlineData("var base = 5;\nfunction d(n, m) { var t = n + (m === undefined ? base : m); return t; }\n[d(1), d(1), d(1), d(1)].join('|');", "6|6|6|6")]
    public void TheEnclosingScopeIsStillResolvedAfterPromotion(string source, string expected)
        => AssertSameAnswer(source, expected);
}
