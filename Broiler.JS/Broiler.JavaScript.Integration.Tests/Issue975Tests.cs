using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Issue #975 — the four test262 paths fixed in this change. Each region names the path the
// behaviour comes from, so a regression here says which suite would go red. Every expectation
// was compared against Node 22.
public class Issue975Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    private static string EvalString(string code) => Eval(code).ToString();

    // ── A direct eval's var belongs to a scope, not to the stack ─────────────────────────
    // test262 staging/sm/eval/exhaustive-fun-normalcaller-direct-normalcode.js. The binding is
    // registered on the frame of the function whose variable environment the eval shares, and
    // every frame on the stack could see it — so a global function called while that frame was
    // live read the eval's `y` instead of the global one. Frames now answer only where they
    // lexically enclose the code asking.

    [Fact]
    public void AnEvalVarIsNotVisibleToAnUnrelatedFunctionOnTheStack()
        => Assert.Equal("5,42", EvalString(
            @"var y = 42;
              function globalY() { return y; }
              function f() { eval('var y = 5;'); return [y, globalY()].join(','); }
              f()"));

    [Fact]
    public void AnEvalVarIsNotVisibleThroughTheGlobalObjectEither()
        => Assert.Equal("42,42", EvalString(
            @"var y = 42;
              function globalY() { return y; }
              function f() { eval('var y = 5;'); return 0; }
              f();
              [y, globalY()].join(',')"));

    [Fact]
    public void AFunctionCalledFromInsideTheEvalBodyReadsItsOwnScope()
        => Assert.Equal("42", EvalString(
            @"var y = 42;
              function globalY() { return y; }
              function f() { return eval('var y = 5; globalY()'); }
              String(f())"));

    [Theory]
    // Everything nested inside the eval's own function still resolves the name: a function
    // declared before the eval ran, one created after it, an arrow in the parameter list that
    // the eval declared into (test262 language/eval-code/direct/arrow-fn-body-cntns-arguments-*),
    // and the eval body's own nested eval.
    [InlineData(@"(function () { function g() { return y; } eval('var y = 5;'); return g(); })()", "5")]
    [InlineData(@"(function () { eval('var y = 6;'); var g = function () { return y; }; return g(); })()", "6")]
    [InlineData(@"(function () { eval('var y = 7;'); return (() => y)(); })()", "7")]
    [InlineData(@"(function () { eval('var y = 8;'); return eval('y'); })()", "8")]
    [InlineData(@"((p = eval(""var q = 'param'""), r = () => q) => r())()", "param")]
    public void AnEvalVarIsVisibleToEverythingItEncloses(string expression, string expected)
        => Assert.Equal(expected, EvalString($"var y = 42; String({expression})"));

    [Fact]
    public void AFunctionTheEvalCreatedCanDeleteTheVarTheEvalDeclared()
        => Assert.Equal("5,true", EvalString(
            @"var y = 42;
              function f() {
                var act = eval(""var y = 5; (function (what) { return what === 'get' ? y : eval('delete y'); })"");
                return [act('get'), act('delete')].join(',');
              }
              f()"));

    // ── An empty statement still ends the Directive Prologue ─────────────────────────────
    // test262 staging/sm/regress/regress-610026.js parses 2^21 empty blocks, which the parser
    // now drops rather than build. What may not be dropped is the one that TERMINATES a
    // Directive Prologue: the string after it is no longer a directive, and a list without it
    // would say the opposite.

    [Theory]
    [InlineData("function f() { ; 'use strict'; return this !== undefined; } f.call(undefined)", "true")]
    [InlineData("function f() { {} 'use strict'; return this !== undefined; } f.call(undefined)", "true")]
    [InlineData("function f() { {} ; 'use strict'; return this !== undefined; } f.call(undefined)", "true")]
    [InlineData("function f() { ; ; return this !== undefined; } f.call(undefined)", "true")]
    // …and one that terminates nothing changes nothing: the directive still applies.
    [InlineData("function f() { 'use strict'; return this !== undefined; } f.call(undefined)", "false")]
    [InlineData("function f() { 'use strict'; ; return this !== undefined; } f.call(undefined)", "false")]
    [InlineData("function f() { return this !== undefined; } f.call(undefined)", "true")]
    public void AnEmptyStatementBeforeADirectiveKeepsTheCodeSloppy(string source, string expected)
        => Assert.Equal(expected, EvalString($"String(eval({Quote(source)}))"));

    [Theory]
    // A program of nothing but empty statements has nothing to run, and one that is only a
    // prologue keeps its completion value.
    [InlineData("eval('{}{}{}')", "undefined")]
    [InlineData("eval('; ; ;')", "undefined")]
    [InlineData("eval('\"use strict\"')", "use strict")]
    [InlineData("eval('; \"use strict\"')", "use strict")]
    [InlineData("eval('1; {} {}')", "1")]
    public void ElidingEmptyStatementsKeepsTheProgramsValue(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Fact]
    public void ManyEmptyBlocksParseAndRun()
        => Assert.Equal("2", EvalString(
            @"(function () { var s = '{}'; for (var i = 0; i < 16; i++) s += s; return String(eval(s + '1; 2;')); })()"));

    // ── A direct eval of a constant direct eval ──────────────────────────────────────────
    // test262 staging/sm/class/newTargetEval.js evaluates 2 200 of them, and each one used to
    // compile a method whose whole body was the inner call. The level is not observable, so it
    // is flattened — but only where the callee is certainly %eval%.

    [Theory]
    [InlineData("eval('eval(\"40 + 2\")')", "42")]
    [InlineData("eval('eval(`40 + 2`)')", "42")]
    [InlineData("eval(\"eval('eval(`40 + 2`)')\")", "42")]
    [InlineData("(function () { eval('eval(\"var v = 9\")'); return v; })()", "9")]
    [InlineData("(function () { var w = 1; eval('eval(\"w = 2\")'); return w; })()", "2")]
    [InlineData("(function () { eval('eval(\"var u = 3\")'); return eval('delete u'); })()", "true")]
    [InlineData("(function () { return typeof eval('eval(\"new.target\")'); })()", "undefined")]
    [InlineData("(function () { return eval('eval(\"typeof arguments\")'); })()", "object")]
    [InlineData("(function () { eval('eval(\\'\"use strict\"; var s = 3;\\')'); return typeof s; })()", "undefined")]
    public void ANestedConstantEvalIsTheSameEvaluation(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Theory]
    // Not every `eval(…)` in that position is %eval%. A local binding, a `with` object and a
    // replaced global all take the ordinary path, and the call has to reach what the name
    // really means.
    [InlineData(@"(function () { var eval = function (s) { return 'local:' + s; }; return eval('eval(""1"")'); })()", "local:eval(\"1\")")]
    [InlineData(@"(function () { var o = { eval: function (s) { return 'with:' + s; } }; with (o) { return eval('1 + 1'); } })()", "with:1 + 1")]
    [InlineData(@"(function (eval) { return eval('eval(""1"")'); })(function (s) { return 'param:' + s; })", "param:eval(\"1\")")]
    public void AShadowedEvalIsNotFlattened(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Theory]
    // The early errors of the inner text are still owed to the caller, at the same moment.
    [InlineData("try { eval('eval(\"]\")'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('eval(\"var\")'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { eval('eval(\"super()\")'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { (0, eval)('eval(\"new.target\")'); 'no throw' } catch (e) { e.constructor.name }", "SyntaxError")]
    public void TheNestedTextStillCarriesItsEarlyErrors(string expression, string expected)
        => Assert.Equal(expected, EvalString(expression));

    // ── Case folding reaches the supplementary planes ────────────────────────────────────
    // test262 staging/sm/RegExp/unicode-ignoreCase.js. `char` casing is defined on UTF-16 code
    // units and leaves an astral code point alone, so a bare astral atom under /iu matched only
    // itself — while the same code point inside a class, which the .NET translator expands, did
    // not. Both engines now fold it.

    [Theory]
    // DESERET CAPITAL/SMALL LETTER LONG I, and WARANG CITI CAPITAL/SMALL LETTER NGAA.
    [InlineData(0x10400, 0x10428)]
    [InlineData(0x10428, 0x10400)]
    [InlineData(0x118A0, 0x118C0)]
    [InlineData(0x118C0, 0x118A0)]
    public void AnAstralAtomFoldsUnderIgnoreCase(int codePoint, int equivalent)
    {
        var source =
            $@"var a = String.fromCodePoint({codePoint}), b = String.fromCodePoint({equivalent});
               [new RegExp(a + '+', 'iu').exec('<' + a + b + '>')[0] === a + b,
                new RegExp('[' + a + ']+', 'iu').exec('<' + a + b + '>')[0] === a + b,
                new RegExp(a, 'u').test(b)].join(',')";

        Assert.Equal("true,true,false", EvalString(source));
    }

    [Theory]
    // The BMP behaviour it shares a path with is unchanged.
    [InlineData("/A+/iu.exec('<Aa>')[0]", "Aa")]
    [InlineData(@"/K/iu.test('\u212A')", "true")]     // K and the KELVIN SIGN are one class
    [InlineData(@"/\u017F/iu.test('s')", "true")]     // LATIN SMALL LETTER LONG S folds to s
    [InlineData(@"/\u017F/i.test('s')", "false")]     // …only with the u flag
    [InlineData(@"/\u0131/iu.test('I')", "false")]    // DOTLESS I is a class of its own
    public void BmpCaseFoldingIsUnchanged(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
