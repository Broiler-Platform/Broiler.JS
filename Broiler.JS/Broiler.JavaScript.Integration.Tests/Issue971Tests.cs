using System;
using System.Linq;
using System.Text;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Issue #971 — the test262 clusters fixed in this change. Each region names the paths the
// behaviour comes from, so a regression here says which suite would go red.
public class Issue971Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    private static string EvalString(string code) => Eval(code).ToString();

    // ── A block-level `function arguments(){}` copies out ────────────────────────────────
    // 10.2.11: "arguments" joins paramBindings, while the Annex B condition tests paramNames,
    // so the copy-out runs and overwrites the arguments object in the variable environment.
    // test262 staging/sm/lexical-environment/block-scoped-functions-annex-b-arguments and
    // staging/sm/regress/regress-602621. (annexB/language/function-code/
    // block-decl-func-skip-arguments quotes the pre-2021 wording and now disagrees with the
    // spec it cites; V8 and SpiderMonkey both answer the way this does.)

    [Theory]
    // The object is still there on the way in, and it is the function on the way out.
    [InlineData("(function() { var before = typeof arguments; { function arguments() {} } return before + ',' + typeof arguments; })()", "object,function")]
    // The name is not read anywhere else in the function, so the copy-out is the only thing
    // that materialises the binding at all.
    [InlineData("(function() { { function arguments() {} } return typeof arguments; })()", "function")]
    // Through a direct eval, which is what regress-602621 wraps it in.
    [InlineData("(function test(arg) { eval(arg); { function arguments() { return 1; } } return typeof arguments; })('42')", "function")]
    // Inside the block the block binding is what the name means.
    [InlineData("(function() { { function arguments() { return 7; } return typeof arguments(); } })()", "number")]
    public void BlockLevelArgumentsFunctionCopiesOut(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Theory]
    // A parameter really spelled `arguments` IS in paramNames, so the whole Annex B step —
    // copy-out included — is skipped and the parameter survives the block.
    [InlineData("(function(arguments) { { function arguments(){} } return typeof arguments; })(1)", "number")]
    // The same rule for any other name: a formal parameter blocks the hoist
    // (test262 annexB/language/function-code/block-decl-func-skip-param).
    [InlineData("(function(f) { { function f(){} } return typeof f; })(1)", "number")]
    // An arrow has no arguments object of its own; at program scope there is none either, so
    // the copy-out lands on the enclosing binding as it always did.
    [InlineData("(() => { { function arguments(){} } return typeof arguments; })()", "function")]
    public void ArgumentsCopyOutRespectsParameterNames(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    // ── B.3.5: a direct eval's `var` over a simple catch parameter ───────────────────────
    // The binding is created in the variable environment, but every reference in the eval body
    // resolves through the running LexicalEnvironment and reaches the catch parameter.
    // test262 staging/sm/lexical-environment/var-in-catch-body-annex-b-eval.

    [Fact(Timeout = 600000)]
    public void EvalVarOverCatchParameterAssignsTheParameter()
    {
        const string source = """
            var x = "global-x";
            var log = "";
            function g() {
              try { throw 8; } catch (x) {
                eval("var x = 42;");
                log += x;
              }
              x = "g";
              log += x;
            }
            g();
            log + "|" + x;
            """;

        Assert.Equal("42g|global-x", EvalString(source));
    }

    [Theory]
    // A read inside the eval, before the declaration, is still the catch parameter.
    [InlineData("(function(){ try { throw 8 } catch (x) { return eval('var x = 42; 0') + ',' + x; } })()", "0,42")]
    // The var binding itself was created, and holds undefined once the catch block is left.
    [InlineData("(function(){ try { throw 8 } catch (x) { eval('var x = 42;'); } return String(typeof x); })()", "undefined")]
    // `var x;` with no initializer assigns nothing at all.
    [InlineData("(function(){ try { throw 8 } catch (x) { eval('var x;'); return x; } })()", "8")]
    // Reading the parameter from inside the eval before anything is declared is unchanged.
    [InlineData("(function(){ try { throw 8 } catch (x) { return eval('x'); } })()", "8")]
    public void EvalVarOverCatchParameterKeepsBothBindings(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    // ── A program-level binding is constructed once ──────────────────────────────────────
    // It is held by both the program block and the compiler root, and both emitted its
    // construction: the root's copy is the JSVariable Register published, the block's replaced
    // the local, and a write through one was then invisible to a read through the other. What
    // saw it was a closure a direct eval created, which reads the published one — and so read a
    // global the eval had itself just written as its pre-eval value.
    // test262 staging/sm/lexical-environment/block-scoped-functions-annex-b-eval.

    [Fact(Timeout = 600000)]
    public void ClosureCreatedInsideEvalSeesTheEvalsOwnWrites()
    {
        const string source = """
            var log = "";
            function h() {
              return eval("log += 'A'; (function(){ return log; })");
            }
            var f = h();
            log + "|" + f();
            """;

        Assert.Equal("A|A", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void BlockLevelFunctionDeclaredByEvalWritesTheLiveGlobal()
    {
        const string source = """
            var log = "";
            function h() {
              eval(`
                log += (delete q);
                {
                  function q() { log += "q"; }
                  log += (delete q);
                }
              `);
              return q;
            }
            h()();
            log;
            """;

        Assert.Equal("truefalseq", EvalString(source));
    }

    [Theory]
    // The write reaches the global whichever side reads it back.
    [InlineData("var v = 1; (function(){ eval('v = 2;'); })(); String(v)", "2")]
    [InlineData("var v = 1; var r = (function(){ return eval('v = 2; (function(){ return v; })'); })(); String(r())", "2")]
    // And a closure still observes writes made after the eval returned.
    [InlineData("var v = 1; var r = (function(){ return eval('(function(){ return v; })'); })(); v = 3; String(r())", "3")]
    public void GlobalVarHasOneCell(string source, string expected)
        => Assert.Equal(expected, EvalString(source));

    // ── An all-constant ArrayLiteral is emitted as data ──────────────────────────────────
    // Past ConstantArrayTemplateThreshold elements the literal becomes one encoded string plus
    // a call instead of an Add per element. test262 staging/sm/String/string-upper-lower-mapping
    // is 65 536 two-element arrays, which as IL runs to megabytes inside a single method.

    private static string LargeLiteral(Func<int, string> element, int count = 400)
        => "[" + string.Join(",", Enumerable.Range(0, count).Select(element)) + "]";

    [Fact(Timeout = 600000)]
    public void LargeConstantArrayKeepsItsElements()
    {
        var source = $"var a = {LargeLiteral(i => i.ToString())}; a.length + ',' + a[0] + ',' + a[399] + ',' + a.join('').length;";
        Assert.Equal("400,0,399,1090", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void LargeConstantArrayOfNestedArraysKeepsItsShape()
    {
        var source = $"""
            var a = {LargeLiteral(i => $"[\"k{i}\", {i}]")};
            a.length + ',' + a[7][0] + ',' + a[7][1] + ',' + Array.isArray(a[7]);
            """;
        Assert.Equal("400,k7,7,true", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void EachEvaluationOfALargeConstantArrayIsFresh()
    {
        var source = "function make() { return " + LargeLiteral(i => $"[{i}]") + """
            ; }
            var a = make(), b = make();
            a[0].push("x");
            (a === b) + ',' + (a[0] === b[0]) + ',' + a[0].length + ',' + b[0].length;
            """;
        Assert.Equal("false,false,2,1", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void LargeConstantArrayPreservesHolesAndNegativeZero()
    {
        // 400 elements: index 0 is a hole, index 1 is -0, the rest are 1.
        var elements = new StringBuilder(",-0");
        for (var i = 2; i < 400; i++)
            elements.Append(",1");

        var source = $"""
            var a = [{elements}];
            a.length + ',' + (0 in a) + ',' + (1 in a) + ',' + Object.is(a[1], -0) + ',' + a[399];
            """;
        Assert.Equal("400,false,true,true,1", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void LargeConstantArrayCarriesEveryLiteralKind()
    {
        // Strings holding the encoding's own delimiters must survive: the format is
        // length-prefixed, so nothing in a string is ever read as structure.
        var elements = new StringBuilder("\"a5:b\",'s3:',\"h\",\"a\",true,false,null,1.5,-3,1e21,\"\\u0000:\"");
        for (var i = 11; i < 400; i++)
            elements.Append(",0");

        var source = $"""
            var a = [{elements}];
            a.slice(0, 11).map(String).join('|') + '|' + a[10].length + '|' + a.length;
            """;
        Assert.Equal("a5:b|s3:|h|a|true|false|null|1.5|-3|1e+21|\u0000:|2|400", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void LargeConstantArrayCarriesLoneSurrogates()
    {
        // The encoding is a .NET string and the emitted constant is a `ldstr`, so an unpaired
        // surrogate is carried as the code unit it is rather than being replaced.
        var elements = new StringBuilder(@"'\ud800','\udfff','😀'");
        for (var i = 3; i < 400; i++)
            elements.Append(",0");

        var source = $"""
            var a = [{elements}];
            a[0].length + ',' + a[0].charCodeAt(0) + ',' + a[1].charCodeAt(0) + ',' + a[2].codePointAt(0) + ',' + a.length;
            """;
        Assert.Equal("1,55296,57343,128512,400", EvalString(source));
    }

    [Fact(Timeout = 600000)]
    public void ConstantArrayTemplateIsNotUsedForASmallLiteral()
    {
        // Below the threshold nothing changes; asserted through behaviour rather than through
        // the emitted code, so it holds whatever the threshold is set to.
        Assert.Equal("3,1,undefined,true", EvalString("var a = [1, , 3]; String(a.length + ',' + a[0] + ',' + a[1] + ',' + !(1 in a))"));
    }

    // ── A direct eval of nothing but `new.target` does not compile ───────────────────────
    // PerformEval shares the calling context's [[NewTarget]], so a body that is only
    // `new.target` is inert in the same way a body that is only a literal is: no declaration,
    // no reference, no call. test262 staging/sm/class/newTargetEval evaluates it 4 400 times.

    [Theory]
    [InlineData("(function f(){ return String(eval('new.target')); })()", "undefined")]
    [InlineData("(function f(){ return eval('new.target') === f; })()", "false")]
    [InlineData("String(new (function f(){ this.r = eval('new.target') === f; })().r)", "true")]
    // Through an arrow, which shares the enclosing function's new.target.
    [InlineData("String(new (function f(){ this.r = (() => eval('new.target'))() === f; })().r)", "true")]
    // And through a nested eval.
    [InlineData("String(new (function f(){ this.r = eval('eval(\"new.target\")') === f; })().r)", "true")]
    // A body that is more than new.target still takes the ordinary path.
    [InlineData("(function f(){ var t = 1; return String(eval('t; new.target')); })()", "undefined")]
    public void EvalOfNewTargetAlone(string expression, string expected)
        => Assert.Equal(expected, EvalString(expression));

    [Fact(Timeout = 600000)]
    public void NewTargetIsStillASyntaxErrorInGlobalEvalCode()
    {
        using var ctx = new JSContext();
        var error = Assert.Throws<JSException>(() => ctx.Eval("eval('new.target')"));
        Assert.Contains("new.target", error.Message);
    }

    [Fact(Timeout = 600000)]
    public void LargeArrayWithANonConstantElementStillEvaluatesIt()
    {
        var elements = new StringBuilder("x++");
        for (var i = 1; i < 400; i++)
            elements.Append(",1");

        var source = $"var x = 0; var a = [{elements}]; a.length + ',' + a[0] + ',' + x;";
        Assert.Equal("400,0,1", EvalString(source));
    }
}
