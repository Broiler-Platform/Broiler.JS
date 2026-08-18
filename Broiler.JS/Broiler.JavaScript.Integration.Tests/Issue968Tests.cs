using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Issue #968 — the test262 clusters fixed in this change. Each region names the paths the
// behaviour comes from, so a regression here says which suite would go red.
public class Issue968Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    private static string EvalString(string code) => Eval(code).ToString();

    // ── RegExp.prototype.source keeps surrogates verbatim ────────────────────────────────
    // EscapeRegExpPattern escapes only `/` and the line terminators; a surrogate — lone or
    // paired — is an ordinary SourceCharacter. test262
    // language/literals/regexp/S7.8.5_A{1.1,1.4,2.1,2.4}_T2 and
    // annexB/built-ins/RegExp/RegExp-{leading,trailing}-escape-BMP assert the identity over
    // every BMP code unit.

    [Theory]
    [InlineData(@"eval('/\uD800/').source.length", "1")]
    [InlineData(@"eval('/\uD800/').source.charCodeAt(0)", "55296")]
    [InlineData(@"eval('/\uDC00/').source.length", "1")]
    [InlineData(@"eval('/a\uD800/').source.length", "2")]
    [InlineData(@"new RegExp('\uD800').source.length", "1")]
    [InlineData(@"/😀/.source.length", "2")]
    [InlineData(@"/😀/u.source.length", "2")]
    public void RegExpSourcePreservesSurrogates(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Theory]
    // A backslash escape keeps both units: `\` + U+D800 is two characters, not seven.
    [InlineData(@"eval('/\\\uD800/').source.length", "2")]
    // The escaping that IS required is unaffected.
    [InlineData(@"/a\/b/.source", @"a\/b")]
    [InlineData(@"new RegExp('a\nb').source", @"a\nb")]
    [InlineData(@"new RegExp('').source", "(?:)")]
    [InlineData(@"eval('/' + /😀/.source + '/u').test('😀')", "true")]
    public void RegExpSourceStillEscapesWhatItMust(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    // ── \d and \w keep ECMAScript (ASCII) semantics under every flag ─────────────────────
    // .NET widens both without RegexOptions.ECMAScript — which `u`, `v` and `s` all switch
    // off — and its ECMA word set contains U+0130 even with the option on. test262
    // built-ins/RegExp/CharacterClassEscapes/character-class-{digit,non-digit,word,non-word}-*.

    [Theory]
    [InlineData(@"/\d/u.test('٠')", "false")]     // ARABIC-INDIC DIGIT ZERO
    [InlineData(@"/\d/v.test('٠')", "false")]
    [InlineData(@"/\d/s.test('٠')", "false")]
    [InlineData(@"/\d/.test('٠')", "false")]
    [InlineData(@"/\D/u.test('٠')", "true")]
    [InlineData(@"/[\d]/u.test('٠')", "false")]
    [InlineData(@"/[^\d]/u.test('٠')", "true")]
    [InlineData(@"/\w/u.test('é')", "false")]     // LATIN SMALL LETTER E WITH ACUTE
    [InlineData(@"/\w/s.test('é')", "false")]
    [InlineData(@"/\W/u.test('é')", "true")]
    [InlineData(@"/[\w]/u.test('é')", "false")]
    [InlineData(@"/\w/.test('İ')", "false")]      // LATIN CAPITAL LETTER I WITH DOT ABOVE
    [InlineData(@"/[\w]/.test('İ')", "false")]
    [InlineData(@"/\W/.test('İ')", "true")]
    public void ClassEscapesUseEcmaScriptSets(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Theory]
    // The ASCII members, and the two code points /iu adds to the word set, are unchanged.
    [InlineData(@"/\d/u.test('7')", "true")]
    [InlineData(@"/\w/u.test('a')", "true")]
    [InlineData(@"/\w/u.test('_')", "true")]
    [InlineData(@"/\W/u.test('-')", "true")]
    [InlineData(@"/\W/u.test('😀')", "true")]
    [InlineData(@"/\D/u.test('😀')", "true")]
    [InlineData(@"/\w/iu.test('ſ')", "true")]     // LATIN SMALL LETTER LONG S folds to s
    [InlineData(@"/\w/iu.test('K')", "true")]     // KELVIN SIGN folds to k
    [InlineData(@"/\w/i.test('ſ')", "false")]     // …but only in Unicode mode
    [InlineData(@"/\w/i.test('K')", "false")]
    [InlineData(@"/\W/i.test('K')", "true")]
    [InlineData(@"/[\d-x]/s.test('-')", "true")]       // Annex B dash beside a class escape
    [InlineData(@"/[\d-x]/s.test('5')", "true")]
    public void ClassEscapesKeepTheirAsciiAndIgnoreCaseMembers(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    // ── A direct eval of a literal-only body is evaluated without compiling ──────────────
    // The fast path must reproduce the completion value exactly, including the strict-mode
    // case where the engine prepends its own "use strict" directive.

    [Theory]
    [InlineData(@"eval('0')", "0")]
    [InlineData(@"eval('1.5')", "1.5")]
    [InlineData(@"eval('""hi""')", "hi")]
    [InlineData(@"eval('true')", "true")]
    [InlineData(@"String(eval('null'))", "null")]
    [InlineData(@"eval('/ab/gi').source", "ab")]
    [InlineData(@"eval('/ab/gi').flags", "gi")]
    [InlineData(@"eval('/ab/gi') instanceof RegExp", "true")]
    [InlineData(@"eval('0;1;2')", "2")]
    [InlineData(@"eval('""use strict""')", "use strict")]
    [InlineData(@"typeof eval('')", "undefined")]
    // A declaration in the body means it is not a literal program at all.
    [InlineData(@"eval('0;var x=1;')", "0")]
    [InlineData(@"(function(){ 'use strict'; return String(eval('0')); })()", "0")]
    [InlineData(@"(function(){ 'use strict'; return typeof eval(''); })()", "undefined")]
    [InlineData(@"(function(){ 'use strict'; return eval(""'x'""); })()", "x")]
    public void LiteralEvalProducesTheSameCompletionValue(string expression, string expected)
        => Assert.Equal(expected, EvalString($"String({expression})"));

    [Fact]
    public void LiteralEvalBuildsAFreshRegExpEachTime()
        => Assert.Equal("0", EvalString(
            @"(function () { var a = eval('/a/g'); a.lastIndex = 5; return String(eval('/a/g').lastIndex); })()"));

    // ── Array generics with a huge or absent length ──────────────────────────────────────
    // test262 staging/sm/Array/{to-length,toLocaleString-01}.js.

    [Fact]
    public void ToLocaleStringUsesLengthOfArrayLike()
    {
        // No own "length": LengthOfArrayLike is 0, not the ToUint32 of the -1 probe.
        Assert.Equal("", EvalString("Array.prototype.toLocaleString.call({})"));
        Assert.Equal("7,baz", EvalString(
            "Array.prototype.toLocaleString.call({ length: 2, 0: 7, 1: { toLocaleString: function () { return 'baz'; } } })"));
        // The length is read exactly once, through [[Get]].
        Assert.Equal("L", EvalString(
            "(function () { var log = ''; Array.prototype.toLocaleString.call({ length: { valueOf: function () { log += 'L'; return 2; } }, 0: 'x', 1: 'z' }); return log; })()"));
        Assert.Equal("x,z", EvalString(
            "Array.prototype.toLocaleString.call({ length: { valueOf: function () { return 2; } }, 0: 'x', 1: 'z' })"));
    }

    [Fact]
    public void ArrayFromRejectsALengthAboveTheArrayIndexRange()
    {
        Assert.Equal("RangeError", EvalString(
            "(function () { try { Array.from({ length: Infinity }); return 'no throw'; } catch (e) { return e.constructor.name; } })()"));
        Assert.Equal("RangeError", EvalString(
            "(function () { try { Array.from({ length: 4294967296 }); return 'no throw'; } catch (e) { return e.constructor.name; } })()"));
        Assert.Equal("a,b", EvalString("Array.from({ length: 2, 0: 'a', 1: 'b' }).join(',')"));
        Assert.Equal("0", EvalString("String(Array.from({}).length)"));
    }

    // ── An arrow's ConciseBody inherits the for-head's [~In] ─────────────────────────────
    // test262 staging/sm/statements/arrow-function-in-for-statement-head.js.

    [Theory]
    [InlineData("for (x => 0 in 1;;) break;", "SyntaxError")]
    [InlineData("for (a in 1;;) break;", "SyntaxError")]
    [InlineData("for ((x => 0 in 1);;) break;", "ok")]
    [InlineData("for (x => (0 in 1);;) break;", "ok")]
    [InlineData("for (x => { return 0 in 1; };;) break;", "ok")]
    [InlineData("for (x => 0;;) break;", "ok")]
    [InlineData("var f = x => 0 in 1;", "ok")]
    [InlineData("for (;1 in {};) break;", "ok")]
    [InlineData("for (;;1 in {}) break;", "ok")]
    [InlineData("for (a in {}) break;", "ok")]
    [InlineData("for (a of []) break;", "ok")]
    public void ForHeadInRestrictionReachesAnArrowConciseBody(string source, string expected)
        => Assert.Equal(expected, EvalString(
            $"(function () {{ try {{ Function({Quote(source)}); return 'ok'; }} catch (e) {{ return e.constructor.name; }} }})()"));

    // ── Direct eval and global bindings ──────────────────────────────────────────────────
    // test262 staging/sm/eval/exhaustive-global-*.js: reading a global var an eval created and
    // `delete` then removed is an unresolvable reference, not undefined.

    [Fact]
    public void ReadingADeletedEvalIntroducedGlobalThrows()
    {
        Assert.Equal("ReferenceError,undefined,71", EvalString(@"
            var code =
              'var y = 5;' +
              'function act(action) {' +
              '  switch (action) {' +
              '    case ""get"": return y;' +
              '    case ""set"": y = 71; return;' +
              '    case ""delete"": return eval(""delete y"");' +
              '  }' +
              '}' +
              'act;';
            var f = eval(code);
            var deleted = f('delete');
            var read, typeOf;
            try { read = f('get'); } catch (e) { read = e.constructor.name; }
            typeOf = eval('typeof y');
            f('set');
            [read, typeOf, f('get')].join(',');"));
    }

    // ── Annex B.3.3 block-level function declarations inside a direct eval ───────────────
    // test262 staging/sm/lexical-environment/block-scoped-functions-annex-b-eval.js.

    [Fact]
    public void BlockLevelFunctionInEvalDoesNotWriteThroughAWithObject()
        => Assert.Equal("outer-geval-gwith-g", EvalString(@"
            (function () {
              var log = '';
              function f() {
                log += g();
                function g() { return 'outer-g'; }
                var o = { g: function () { return 'with-g'; } };
                with (o) { eval('{ function g() { return ""eval-g""; } }'); }
                log += g();
                log += o.g();
              }
              f();
              return log;
            })()"));

    [Fact]
    public void DeletingABlockLevelFunctionInEvalReturnsFalse()
        // The eval's var-environment binding is configurable, the block's lexical one is not.
        => Assert.Equal("true,false", EvalString(
            @"(function () { var r = []; eval('r.push(delete q); { function q() {} r.push(delete q); }'); return r.join(','); })()"));

    [Fact]
    public void ABlockLevelFunctionRestoresAVarBindingItsOwnDeleteRemoved()
        => Assert.Equal("function", EvalString(
            @"(function () { eval('(delete q); { function q() {} }'); return typeof q; })()"));

    [Fact]
    public void AnEvalFunctionDeclarationStillUpdatesACollidingCallerBinding()
    {
        Assert.Equal("99", EvalString(
            @"(function (p) { eval('function p(){return 99}'); return typeof p === 'function' ? p() : p; })(5)"));
        Assert.Equal("8", EvalString(
            @"(function () { var v = 1; eval('function v(){return 8}'); return typeof v === 'function' ? v() : v; })()"));
    }

    // ── A statement that compiles to nothing emits nothing ───────────────────────────────
    // test262 staging/sm/regress/regress-610026.js: 2^21 empty blocks used to become 2^21
    // debug-Step call sites in one method, which the JIT could not compile.

    [Theory]
    // Each of these compiles to nothing, so N of them must cost nothing. Skipping only the debug
    // Step was not enough: `{;}` and `{{}}` are blocks that CONTAIN such a statement, so they came
    // back as a BBlockExpression wrapping one Empty — not Empty — and the blow-up returned one
    // token away from the test262 file. The statement itself has to go.
    [InlineData("{}")]
    [InlineData(";")]
    [InlineData("{;}")]
    [InlineData("{{}}")]
    [InlineData("{ ; ; }")]
    public void AStatementThatCompilesToNothingCostsNothing(string unit)
        => Assert.Equal("2", EvalString(
            $"(function () {{ var s = {Quote(unit)}; for (var i = 0; i < 12; i++) s += s; return String(eval(s + '1; 2;')); }})()"));

    [Theory]
    // Eliding the statement must not disturb the completion value: an empty statement completes
    // with `empty`, and UpdateEmpty leaves the running value alone. Every expectation here is
    // Node's.
    [InlineData("{} {} 1;", "1")]
    [InlineData("1; {}", "1")]
    [InlineData("1; {;}", "1")]
    [InlineData("{;}", "undefined")]
    [InlineData("{ let x = 5; }", "undefined")]
    [InlineData("{ let x; }", "undefined")]
    [InlineData("{ var v; }", "undefined")]
    [InlineData("try{}finally{}", "undefined")]
    [InlineData("with({}){}", "undefined")]
    [InlineData("switch(1){case 1:}", "undefined")]
    [InlineData("lbl: {;}", "undefined")]
    [InlineData("if(1);else;", "undefined")]
    [InlineData("for(;;){break}", "undefined")]
    public void ElidingEmptyStatementsKeepsTheCompletionValue(string source, string expected)
        => Assert.Equal(expected, EvalString($"String(eval({Quote(source)}))"));

    // ── An object past the shape-chain cap behaves identically ───────────────────────────
    // test262 staging/sm/Proxy/ownkeys-linear.js builds a 15 000-property target; the shape
    // chain that cost O(n²) LIVE bytes is capped at ObjectShape.MaxSlots.

    [Fact]
    public void ManyNamedPropertiesKeepInsertionOrderAndValues()
        => Assert.Equal("300,k0,k299,42,,299", EvalString(@"
            (function () {
              var o = {};
              for (var i = 0; i < 300; i++) o['k' + i] = i;
              o.k42 = 42;
              var keys = Object.getOwnPropertyNames(o);
              var length = keys.length;
              var first = keys[0];
              var last = keys[keys.length - 1];
              var kept = o.k42;
              delete o.k7;
              // `undefined` joins as the empty string, hence the ',,' for the deleted key.
              return [length, first, last, kept, o.k7, o.k299].join(',');
            })()"));

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
