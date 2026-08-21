using System.Diagnostics;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/Broiler-Platform/Broiler.JS/issues/966 — the
// test262 `language/comments/S7.4_A5.js` timeout.
//
// That test runs 65 536 direct evals of a program that is one line comment, to check that a
// comment can hold any non-terminator code point. Each of them emitted and JIT-ed a
// DynamicMethod for a program with nothing in it, which measured ~0.6 ms and put the file
// ~40 s over a 30 s budget. A body whose StatementList is empty has the undefined completion
// and declares nothing, so it is finished once its early errors have been checked.
//
// The early errors are the part that must NOT be skipped, and most of this file is about
// them: an eval of an unparseable or ill-placed body still throws, whether or not the body
// would have turned out to be inert.
public class Issue966InertEvalTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    // Bodies with no statements evaluate to undefined.
    [InlineData("typeof eval('')", "undefined")]
    [InlineData("typeof eval('   ')", "undefined")]
    [InlineData("typeof eval('//var Xyy = -1')", "undefined")]
    [InlineData("typeof eval('//c\\n')", "undefined")]
    [InlineData("typeof eval('/* c */')", "undefined")]
    [InlineData("typeof eval('/*a*//*b*/ //c')", "undefined")]
    [InlineData("typeof eval('\\t\\f\\v')", "undefined")]
    [InlineData("typeof eval('\\u2028')", "undefined")]
    [InlineData("typeof eval('\\u2029')", "undefined")]
    [InlineData("typeof eval('\\uFEFF')", "undefined")]
    [InlineData("typeof eval('\\u00A0')", "undefined")]
    // …and so does the same body reached indirectly.
    [InlineData("var g = eval; typeof g('')", "undefined")]
    [InlineData("var g = eval; typeof g('//c')", "undefined")]
    [InlineData("var g = eval; typeof g('/* c */')", "undefined")]
    public void InertBodyEvaluatesToUndefined(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // A body that does have statements is unchanged — including one that only becomes
    // non-empty because a comment precedes it.
    [InlineData("eval('//c\\n1')", "1")]
    [InlineData("eval('/*c*/ 1')", "1")]
    [InlineData("typeof eval(';')", "undefined")]
    [InlineData("typeof eval('{}')", "undefined")]
    [InlineData("eval('/*c*/ var zz = 7; zz')", "7")]
    [InlineData("eval(42)", "42")]              // a non-string argument is returned as is
    [InlineData("var g = eval; g('//c\\n2')", "2")]
    public void BodyWithStatementsIsUnchanged(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // Early errors still fire. These are the cases the fast path must not swallow: each
    // body is unparseable or ill-placed, and some of them would look inert to a scanner
    // that stopped at the first token.
    [InlineData("try { eval(']'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('}'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('/'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('var'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('super()'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('new.target'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("var g = eval; try { g(']'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("var g = eval; try { g('super()'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    public void EarlyErrorsStillFire(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // The scaffolding an inert body skips — the direct-eval scope overlay, the activation
    // binding, the declared-binding snapshot — is only observable through what the body
    // does, so a body that does nothing must leave the caller exactly as it found it.
    [InlineData("var yy = 0; eval('//var Xyy = -1'); yy", "0")]
    [InlineData("var yy = 5; eval(''); yy", "5")]
    [InlineData("(function () { var a = 1; eval('/*c*/'); return a; })()", "1")]
    [InlineData("(function (p) { eval('//c'); return p; })('kept')", "kept")]
    [InlineData("(function () { var o = { a: 1 }; with (o) { eval('//c'); return a; } })()", "1")]
    [InlineData("(function (a = eval('//c')) { return typeof a; })()", "undefined")]
    [InlineData("(function () { class C { x = eval('//c'); } return typeof new C().x; })()", "undefined")]
    [InlineData("typeof eval(\"eval('//c')\")", "undefined")]
    [InlineData("(() => typeof eval('/*x*/'))()", "undefined")]
    public void InertBodyLeavesTheCallerAlone(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact(Timeout = 600000)]
    public void StrictModeEvalIsUnaffected()
    {
        // A strict direct eval has `"use strict";` prepended to its body, so its statement
        // list is never empty and it never takes the fast path. Pinned here because the
        // prepending is what makes that true, and it is not obvious from the fast path.
        Assert.Equal("1", Eval("'use strict'; eval('//c\\n1')"));
        Assert.Equal("0", Eval("'use strict'; var yy = 0; eval('//c'); yy"));
    }

    [Fact(Timeout = 600000)]
    public void TheTimingOutTestShapeCompletesQuickly()
    {
        // S7.4_A5's own shape: one direct eval of a comment-only program per code unit. The
        // loose bound is there to fail if compilation comes back, which costs two orders of
        // magnitude and nothing less — not to measure the machine.
        using var ctx = new JSContext();
        var stopwatch = Stopwatch.StartNew();
        var result = ctx.Eval(
            """
            (function () {
              let bad = 0;
              for (let cp = 0; cp < 0x10000; cp++) {
                const ch = String.fromCharCode(cp);
                const terminator = cp === 0x0A || cp === 0x0D || cp === 0x2028 || cp === 0x2029;
                let yy = 0;
                eval('//var ' + ch + 'yy = -1');
                if (terminator ? yy !== -1 : yy !== 0) bad++;
              }
              return bad;
            })()
            """).ToString();
        stopwatch.Stop();

        Assert.Equal("0", result);
        Assert.True(
            stopwatch.Elapsed.TotalSeconds < 60,
            $"65 536 comment-only evals took {stopwatch.Elapsed.TotalSeconds:F1}s");
    }
}
