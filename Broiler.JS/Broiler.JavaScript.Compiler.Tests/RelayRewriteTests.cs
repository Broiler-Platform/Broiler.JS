using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// A relayed lambda is no longer closure-rewritten a second time when the walk that emitted its
// enclosing lambda already descended through it (docs/performance-roadmap.md item 1-1's
// remaining half).
//
// LambdaRewriter.Rewrite descends through nested lambdas — that is how CheckForClosure threads a
// capture up the whole chain — and RuntimeMethodBuilder.Relay then called it again with the
// relayed lambda as its own root, so a lambda at depth d was walked d+1 times. Counted on the
// real corpora, the second walk found nothing to do on any site: 0 of 415 on jQuery, 0 of 978 on
// Box2D, 0 of 1 574 on Typescript.
//
// "Found nothing to do" is a count, not a proof, so what these assert is the behaviour the
// second walk would have been protecting: a capture that has to be threaded through more than one
// level, a write through such a capture, and the two rewrites that build lambdas AFTER the
// descending walk has been through — a generator body and an async body, whose state machines are
// synthesized later and whose lambdas therefore still carry the flag clear and are rewritten at
// relay exactly as before. Every case is asserted on BOTH settings of the switch, so it is a
// statement about closure semantics rather than a description of the skip.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class RelayRewriteTests
{
    private static string Eval(string source, bool skipRewritten)
    {
        var previous = ClosureRewriteDiagnostics.SkipRewrittenRelays;
        ClosureRewriteDiagnostics.SkipRewrittenRelays = skipRewritten;
        try
        {
            using var context = new JSContext();
            return context.Eval(source).ToString();
        }
        finally
        {
            ClosureRewriteDiagnostics.SkipRewrittenRelays = previous;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACaptureReadsThroughEveryInterveningLevel(bool skip)
    {
        // The case the relay-time rewrite would have to exist for: `outer` is bound at depth 0
        // and read at depth 3, so each of the two lambdas in between has to carry a box it never
        // mentions itself. Only a walk that sees the whole chain can set that up.
        const string source = """
            (function () {
              var outer = 7;
              return (function () {
                return (function () {
                  return (function () { return outer; })();
                })();
              })();
            })();
            """;

        Assert.Equal("7", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AWriteThroughATransitiveCaptureIsSeenByTheBindingsOwner(bool skip)
    {
        // Same chain in the other direction. A by-value capture would read 1 here, and a chain
        // that boxed at the wrong level would read 1 as well — the two failures the threading
        // exists to prevent are indistinguishable from the outside, which is why the write is
        // asserted from the scope that owns the binding.
        const string source = """
            (function () {
              var counter = 1;
              (function () {
                (function () { counter = counter + 41; })();
              })();
              return counter;
            })();
            """;

        Assert.Equal("42", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EachClosureInstanceKeepsItsOwnCellAcrossTwoLevels(bool skip)
    {
        // Per-instance boxes, two levels down. Sharing one cell reports the last iteration five
        // times; capturing by value reports the first.
        const string source = """
            var fns = [];
            for (let i = 0; i < 5; i++) {
              fns.push((function () { return function () { return i * 10; }; })());
            }
            fns.map(function (f) { return f(); }).join(',');
            """;

        Assert.Equal("0,10,20,30,40", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AGeneratorNestedInAClosureStillCapturesIt(bool skip)
    {
        // GeneratorRewriter builds the state machine's lambdas after the descending walk has
        // been through, so they carry the flag clear and are rewritten at relay as before. If
        // the mark were set on them anyway, this is what would break.
        const string source = """
            (function () {
              var base = 10;
              function* g() { yield base + 1; yield base + 2; }
              var out = [];
              for (const v of g()) out.push(v);
              return out.join(',');
            })();
            """;

        Assert.Equal("11,12", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnAsyncBodyNestedInAClosureStillCapturesIt(bool skip)
    {
        // The RewriteRootOnly path: an async body is pre-rewritten before its enclosing scope
        // exists, and that pass deliberately stops at each nested lambda. It therefore must not
        // mark anything, or the enclosing walk's threading would be skipped for a body that
        // never had it.
        const string source = """
            (function () {
              var base = 5;
              var seen = 0;
              (async function () { seen = base + 1; })();
              return seen;
            })();
            """;

        Assert.Equal("6", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnArrowTwoLevelsDownKeepsTheEnclosingThis(bool skip)
    {
        // `this` is captured by the same mechanism as any other binding, and an arrow has none
        // of its own at any depth.
        const string source = """
            var o = {
              v: 3,
              run: function () {
                return (function (self) { return (() => self.v * 2)(); })(this);
              }
            };
            o.run();
            """;

        Assert.Equal("6", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANamedFunctionExpressionRecursesThroughItsOwnName(bool skip)
    {
        // The name binding of a named function expression is a capture of a cell the enclosing
        // scope cannot see, created by the compiler rather than by the source.
        const string source = """
            (function () {
              var fact = function f(n) { return n <= 1 ? 1 : n * f(n - 1); };
              return fact(5);
            })();
            """;

        Assert.Equal("120", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADirectEvalTwoLevelsDownReadsAndWritesTheOuterBinding(bool skip)
    {
        // Direct eval resolves against the scope chain at run time, so it exercises the capture
        // from the side the static threading does not cover.
        const string source = """
            (function () {
              var x = 4;
              (function () { (function () { eval('x = x * 3'); })(); })();
              return x;
            })();
            """;

        Assert.Equal("12", Eval(source, skip));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FiveLevelsOfNestingReadAndWriteEveryLevel(bool skip)
    {
        // The depth term. Each level both owns a binding and reaches past its parent for one, so
        // no single level's repository is sufficient and every intermediate has to carry boxes.
        const string source = """
            (function () {
              var a = 1;
              return (function () {
                var b = 2;
                return (function () {
                  var c = 4;
                  return (function () {
                    var d = 8;
                    return (function () {
                      a += 16; b += 16; c += 16; d += 16;
                      return a + b + c + d;
                    })();
                  })();
                })();
              })() + ':' + a;
            })();
            """;

        Assert.Equal("79:17", Eval(source, skip));
    }

    [Fact]
    public void NoRelayInANestedProgramNeedsARewriteOfItsOwn()
    {
        // The claim itself, as an assertion rather than as a corpus reading. Compiling and
        // running a program with lambdas at four depths relays every one of them, and none has
        // anything left for a second walk to do — which is what makes the skip a removal of
        // repeated work rather than a removal of work.
        //
        // Mutation-testing this the other way round is what the Theory cases above are: with the
        // switch off, every one of them still passes, so the second walk is not what they were
        // relying on.
        const string source = """
            (function () {
              var a = 1;
              var fns = [];
              for (var i = 0; i < 3; i++) {
                fns.push(function () {
                  return function () { return function () { return a + i; }; };
                });
              }
              return fns.map(function (f) { return f()()(); }).join(',');
            })();
            """;

        var previous = ClosureRewriteDiagnostics.SkipRewrittenRelays;
        ClosureRewriteDiagnostics.SkipRewrittenRelays = true;
        try
        {
            using var context = new JSContext();
            ClosureRewriteDiagnostics.Reset();
            Assert.Equal("4,4,4", context.Eval(source).ToString());

            Assert.True(
                ClosureRewriteDiagnostics.SkippedRelays > 0,
                "the program relays nested lambdas, so some relay must have been skippable");
            Assert.Equal(0, ClosureRewriteDiagnostics.RewroteRelays);
        }
        finally
        {
            ClosureRewriteDiagnostics.SkipRewrittenRelays = previous;
        }
    }
}
