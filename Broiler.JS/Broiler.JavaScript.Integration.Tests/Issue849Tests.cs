using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/849
public class Issue849Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 88: a parenthesized class expression's toString reported the JSFunction
    // "native" fallback ("function native() { [native code] }") because the compiler
    // never captured the original source text. AstNode.Code already spans the class
    // keyword through the closing brace and excludes surrounding parentheses, so the
    // captured substring matches the spec's NativeFunction-vs-class branching.
    [Theory]
    [InlineData("(class {}).toString()", "class {}")]
    [InlineData("((class {})).toString()", "class {}")]
    [InlineData("class C{};C.toString()", "class C{}")]
    [InlineData("(class Named { m(){} }).toString()", "class Named { m(){} }")]
    [InlineData("(class extends Object {}).toString()", "class extends Object {}")]
    public void ClassExpressionToStringReportsSource(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The class's own prototype.constructor identity is the same class object, so the
    // constructor accessed via the prototype must report the same source text.
    [Fact]
    public void ClassPrototypeConstructorToStringMatchesClass()
    {
        Assert.Equal("class C { m(){ return 1; } }",
            Eval("class C { m(){ return 1; } }; C.prototype.constructor.toString()").ToString());
    }

    // Native functions still report the "[native code]" placeholder.
    [Fact]
    public void NativeFunctionToStringUnaffected()
    {
        Assert.Equal("function parseInt() { [native code] }",
            Eval("parseInt.toString()").ToString());
    }

    // Problem 95: %RegExpStringIteratorPrototype% must expose its own @@toStringTag
    // ("RegExp String Iterator"). Without it the prototype inherited the
    // %IteratorPrototype% accessor and reported "Iterator" instead.
    [Fact]
    public void RegExpStringIteratorPrototypeHasToStringTag()
    {
        Assert.Equal("RegExp String Iterator",
            Eval("Object.getPrototypeOf(/./[Symbol.matchAll](''))[Symbol.toStringTag]").ToString());
    }

    // The descriptor is { writable: false, enumerable: false, configurable: true }
    // (verifyProperty in the test262 case checks each attribute independently).
    [Fact]
    public void RegExpStringIteratorPrototypeToStringTagDescriptor()
    {
        var code = "var p = Object.getPrototypeOf(/./[Symbol.matchAll](''));"
            + "var d = Object.getOwnPropertyDescriptor(p, Symbol.toStringTag);"
            + "[d.writable, d.enumerable, d.configurable].join(',')";
        Assert.Equal("false,false,true", Eval(code).ToString());
    }

    // Object.prototype.toString picks up the new tag on RegExp String Iterators.
    [Fact]
    public void ObjectToStringUsesRegExpStringIteratorTag()
    {
        Assert.Equal("[object RegExp String Iterator]",
            Eval("Object.prototype.toString.call(/./[Symbol.matchAll](''))").ToString());
    }

    // Problem 97: String.prototype.split must call ToUint32(limit) (step 6) and then
    // ToString(separator) (step 7) BEFORE the lim==0 short-circuit (step 8). The old
    // path skipped separator coercion entirely when limit valueOf'd to 0, dropping a
    // user-visible side effect (test262 sm/String/split-order).
    [Fact]
    public void SplitObservesSeparatorToStringWhenLimitIsZero()
    {
        var code = "var log = [];"
            + "'abba'.split({ toString() { log.push('separator-tostring'); return 'b'; } },"
            + "             { valueOf()  { log.push('limit-valueOf'); return 0; } });"
            + "log.join(',')";
        Assert.Equal("limit-valueOf,separator-tostring", Eval(code).ToString());
    }

    // The lim==0 path still returns the empty array — only the coercion order changed.
    [Fact]
    public void SplitReturnsEmptyArrayWhenLimitIsZero()
    {
        Assert.Equal("0",
            Eval("'abc'.split('b', 0).length.toString()").ToString());
    }

    // A regular split with a string separator and positive limit is unaffected.
    [Theory]
    [InlineData("'a,b,c'.split(',').join('|')", "a|b|c")]
    [InlineData("'a,b,c'.split(',', 2).join('|')", "a|b")]
    [InlineData("'abc'.split().length.toString()", "1")]
    public void SplitUnaffectedRegressions(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 82: RegExp.prototype[Symbol.replace] must drop the substitution when an
    // ill-behaving subclass's exec returns a result whose index moved backwards
    // (position < nextSourcePosition — §22.2.6.11 step 16.p). The previous behaviour
    // emitted "abcXXcde" because it always appended the replacement and rewound the
    // running nextSourcePosition (test262 g-pos-decrement).
    [Fact]
    public void SymbolReplaceIgnoresBackwardPosition()
    {
        var code = "var r = /./g;"
            + "var callCount = 0;"
            + "r.exec = function() {"
            + "  callCount += 1;"
            + "  if (callCount === 1) { return { index: 3, length: 1, 0: 0 }; }"
            + "  else if (callCount === 2) { return { index: 1, length: 1, 0: 0 }; }"
            + "  return null;"
            + "};"
            + "r[Symbol.replace]('abcde', 'X')";
        Assert.Equal("abcXe", Eval(code).ToString());
    }

    // The well-behaved (monotonic-position) case is unaffected: every match is
    // accumulated and consecutive replacements glue correctly.
    [Theory]
    [InlineData("'ababab'.replace(/a/g, 'X')", "XbXbXb")]
    [InlineData("'abcabc'.replace(/a/g, 'X')", "XbcXbc")]
    [InlineData("'aaa'.replace(/a/g, 'X')", "XXX")]
    public void SymbolReplaceMonotonicPositionsStillReplace(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 100: an array-assignment with a rest element (`[a, ...r] = it`) must
    // skip calling it.next() once the iterator is already done. The previous compiler
    // always consumed the iterator for the rest pattern, producing an extra "next,"
    // entry in the log for `[obj.a, ...obj.r] = createIterable(0)` (test262 sm/
    // expressions/destructuring-array-done).
    [Fact]
    public void ArrayDestructuringRestSkipsNextWhenIteratorDone()
    {
        var code = "var log = '';"
            + "var obj = new Proxy({}, {"
            + "  set(t, n, v) { log += 'set:' + n + '=' + (Array.isArray(v) ? JSON.stringify(v) : v) + ','; return true; }"
            + "});"
            + "function it(n){ return { i:0, [Symbol.iterator]() { return this; },"
            + "  next() { log += 'next,'; this.i++; return this.i <= n ? {value:this.i, done:false} : {value:0, done:true}; } }; }"
            + "[obj.a, ...obj.r] = it(0);"
            + "log";
        Assert.Equal("next,set:a=undefined,set:r=[],", Eval(code).ToString());
    }

    // A rest pattern after a partially-consumed iterator collects the remaining
    // elements with exactly one trailing next call (the one that returns done).
    [Fact]
    public void ArrayDestructuringRestCollectsRemainingElements()
    {
        var code = "var log = '';"
            + "var obj = {};"
            + "function it(n){ return { i:0, [Symbol.iterator]() { return this; },"
            + "  next() { log += 'next,'; this.i++; return this.i <= n ? {value:this.i, done:false} : {value:0, done:true}; } }; }"
            + "[obj.a, ...obj.r] = it(3);"
            + "log + '|' + obj.a + '|' + obj.r.join(',')";
        // a=1, then rest collects 2 and 3, then next returns done.
        Assert.Equal("next,next,next,next,|1|2,3", Eval(code).ToString());
    }

    // Problem 71 (sm/JSON/stringify-boxed-primitives): SerializeJSONProperty step 6 runs
    // ToNumber / ToString on Number and String wrapper objects, so a user-redefined
    // valueOf / toString on the prototype chain must be observed instead of the internal
    // slot being read directly.
    [Fact]
    public void JsonStringifyNumberWrapperRespectsPrototypeValueOf()
    {
        var code = "var saved = Number.prototype.valueOf;"
            + "Object.defineProperty(Number.prototype, 'valueOf', { value: function(){ return 17; }, writable: true, configurable: true });"
            + "var r = JSON.stringify(new Number(5));"
            + "Object.defineProperty(Number.prototype, 'valueOf', { value: saved, writable: true, configurable: true });"
            + "r";
        Assert.Equal("17", Eval(code).ToString());
    }

    [Fact]
    public void JsonStringifyStringWrapperRespectsPrototypeToString()
    {
        var code = "var saved = String.prototype.toString;"
            + "Object.defineProperty(String.prototype, 'toString', { value: function(){ return 'forced'; }, writable: true, configurable: true });"
            + "var r = JSON.stringify(new String('foopy'));"
            + "Object.defineProperty(String.prototype, 'toString', { value: saved, writable: true, configurable: true });"
            + "r";
        Assert.Equal("\"forced\"", Eval(code).ToString());
    }

    // The default Number/String/Boolean wrapper behaviour is unchanged.
    [Theory]
    [InlineData("JSON.stringify(new Boolean(false))", "false")]
    [InlineData("JSON.stringify(new Boolean(true))", "true")]
    [InlineData("JSON.stringify(new Number(5))", "5")]
    [InlineData("JSON.stringify(new Number(-0))", "0")]
    [InlineData("JSON.stringify(new String('foopy'))", "\"foopy\"")]
    public void JsonStringifyWrapperDefaults(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // A Number wrapper whose valueOf returns NaN (e.g. when both methods are
    // unavailable / non-callable) still serializes as null per §25.5.2 step 9.
    [Fact]
    public void JsonStringifyNumberWrapperWithNoUsefulCoercion()
    {
        var code = "var n = new Number(5);"
            + "n.valueOf = function(){ return NaN; };"
            + "JSON.stringify(n)";
        Assert.Equal("null", Eval(code).ToString());
    }

    // Problem 84 (sm/RegExp/constructor-ordering): when Reflect.construct's
    // newTarget.prototype getter recompiles the source RegExp as a side effect, the spec
    // requires the new RegExp to capture [[OriginalSource]] BEFORE consulting
    // newTarget.prototype (RegExp(pattern, flags) steps 5 and 8). The previous order
    // produced "b" because `: base(JSEngine.NewTargetPrototype)` evaluated the getter
    // first and then read source from the (now-recompiled) original.
    [Fact]
    public void RegExpConstructorReadsSourceBeforeNewTargetPrototype()
    {
        var code = "var re = /a/;"
            + "var newRe = Reflect.construct(RegExp, [re],"
            + "  Object.defineProperty(function(){}.bind(null), 'prototype', {"
            + "    get() { re.compile('b'); return RegExp.prototype; }"
            + "  }));"
            + "newRe.source";
        Assert.Equal("a", Eval(code).ToString());
    }

    // Subclassing still routes through newTarget.prototype, so an ordinary subclass
    // instance correctly inherits its custom prototype.
    [Fact]
    public void RegExpSubclassStillInheritsCustomPrototype()
    {
        var code = "class MyRe extends RegExp {}"
            + "var r = new MyRe('x');"
            + "(r instanceof MyRe && r instanceof RegExp && r.source === 'x').toString()";
        Assert.Equal("true", Eval(code).ToString());
    }

    // Plain new RegExp from a string pattern is unaffected.
    [Theory]
    [InlineData("new RegExp('foo').source", "foo")]
    [InlineData("new RegExp(/abc/i).flags", "i")]
    [InlineData("new RegExp(/abc/i, 'g').flags", "g")]
    public void RegExpConstructorPlainCases(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 92 (sm/RegExp/replace-trace): RegExp.prototype[Symbol.replace] now reads
    // the result properties in spec order INSIDE the accumulation loop — length, then
    // "0", then "index", then "groups" (§22.2.6.11 steps 16.c–o). The previous order
    // leaked "0" before "length", which is observable through a Proxy result. A
    // non-global match has no preceding empty-match probe (step 14.f.i), so the trace
    // contains exactly one access of each property in the spec-mandated order.
    [Fact]
    public void SymbolReplaceReadsResultPropertiesInSpecOrder()
    {
        var code = "var log = [];"
            + "var rx = /./;"
            + "rx.exec = function() {"
            + "  if (this.done) return null;"
            + "  this.done = true;"
            + "  return new Proxy(['m'], {"
            + "    get(t, k) { log.push('get:' + String(k)); return t[k]; }"
            + "  });"
            + "};"
            + "rx[Symbol.replace]('mxxx', '*');"
            + "log.join(',')";
        Assert.Equal("get:length,get:0,get:index,get:groups", Eval(code).ToString());
    }

    // The global-match path still probes "0" as part of the empty-match advance check
    // (step 14.f.i) BEFORE the accumulation loop's length-first reads — exactly what
    // the upstream replace-trace test262 case observes.
    [Fact]
    public void SymbolReplaceGlobalReadsProbeZeroBeforeAccumulationLengthFirst()
    {
        var code = "var log = [];"
            + "var rx = /./g;"
            + "rx.exec = function() {"
            + "  if (this.done) return null;"
            + "  this.done = true;"
            + "  return new Proxy(['m'], {"
            + "    get(t, k) { log.push('get:' + String(k)); return t[k]; }"
            + "  });"
            + "};"
            + "rx[Symbol.replace]('mxxx', '*');"
            + "log.join(',')";
        // get:0 from gather-loop probe (step 14.f.i), then accumulation reads in spec order.
        Assert.Equal("get:0,get:length,get:0,get:index,get:groups", Eval(code).ToString());
    }

    // Problem 80 (sm/expressions/string-literal-escape-sequences): a backslash
    // followed by a CRLF line-terminator sequence is a single LineContinuation
    // (§11.8.4), but the old `case '\r'` consumed `\r`, called CanConsumeNext('\n')
    // (which already advanced past the `\r`), and then ALSO did an explicit
    // Consume() — gobbling the `\n` AND the next visible character. The string
    // literal `"a\<CR><LF>b"` therefore produced "a" because the loop returned the
    // closing quote next. Each line-terminator variant must yield "ab".
    [Theory]
    [InlineData("\"a\\\rb\"", "ab")]
    [InlineData("\"a\\\nb\"", "ab")]
    [InlineData("\"a\\\r\nb\"", "ab")]
    public void LineContinuationEatsExactlyTheLineTerminator(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Same behaviour for U+2028 LINE SEPARATOR and U+2029 PARAGRAPH SEPARATOR.
    [Fact]
    public void LineContinuationEatsLineAndParagraphSeparators()
    {
        var ls = "\"a\\\u2028b\"";
        var ps = "\"a\\\u2029b\"";
        Assert.Equal("ab", Eval(ls).ToString());
        Assert.Equal("ab", Eval(ps).ToString());
    }

    // Regular escape sequences are unaffected.
    [Theory]
    [InlineData("\"a\\nb\"", "a\nb")]
    [InlineData("\"a\\tb\"", "a\tb")]
    [InlineData("\"a\\\\b\"", "a\\b")]
    public void OtherStringEscapesUnaffected(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 81 (built-ins/RegExp/named-groups/non-unicode-property-names): a regex
    // named-group GroupSpecifier may contain \uXXXX / \u{…} escapes whose decoded
    // code points form the runtime group name. The captureMap stored the raw source
    // (`_‌`), so `groups[name]` — whose key is the JS-decoded identifier (the
    // single character "_" + U+200C) — found nothing. RewriteCaptureGroups now runs
    // DecodeGroupName on every GroupSpecifier AND on every \k<name> backreference.
    [Theory]
    [InlineData("/(?<\\u03C0>a)/.exec('bab').groups['\\u03C0']", "a")]
    [InlineData("/(?<_\\u200C>a)/.exec('bab').groups['_\\u200C']", "a")]
    [InlineData("/(?<\\u0041>.)/.test('a').toString()", "true")]
    [InlineData("RegExp('(?<\\\\u{0041}>.)','u').test('a').toString()", "true")]
    public void RegExpNamedGroupNameDecodesUnicodeEscapes(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Backreferences resolve through the same decoded-name path.
    [Fact]
    public void RegExpNamedGroupBackrefDecodesUnicodeEscape()
    {
        // A = "A": GroupSpecifier uses an escape, \k<A> uses the literal.
        var code = "var r = new RegExp('(?<\\\\u0041>.).\\\\k<A>');"
            + "var m = 'aXa'.match(r);"
            + "m ? m.slice(0,2).join(',') : 'null'";
        Assert.Equal("aXa,a", Eval(code).ToString());
    }

    // Problem 89 (sm/lexical-environment/block-scoped-functions-annex-b-generators):
    // a block-nested generator/async FunctionDeclaration inside a direct eval used to
    // route through VisitRuntimeFunctionDeclaration, which transparently created a
    // var-environment binding via GetOrCreateDirectEvalRootVariable. Generators and
    // async functions are purely block-scoped per Annex B 3.3, so `typeof g` outside
    // the eval'd block must be "undefined" — they now follow the same implicit-block
    // path as plain function declarations in eval, with the Annex B var copy-out
    // suppressed by the existing generator/async guard.
    [Fact]
    public void DirectEvalBlockScopedGeneratorIsNotHoisted()
    {
        Assert.Equal("undefined",
            Eval("function f() { eval('{ function* g() {} }'); return typeof g; } f()").ToString());
    }

    [Fact]
    public void DirectEvalAtTopLevelBlockScopedGeneratorIsNotHoisted()
    {
        Assert.Equal("undefined",
            Eval("eval('{ function* g() {} }'); typeof g").ToString());
    }

    [Fact]
    public void DirectEvalBlockScopedAsyncFunctionIsNotHoisted()
    {
        Assert.Equal("undefined",
            Eval("function f() { eval('{ async function g() {} }'); return typeof g; } f()").ToString());
    }

    // Plain function declarations in direct eval blocks are still hoisted via Annex
    // B's web-legacy var binding.
    [Fact]
    public void DirectEvalBlockScopedFunctionIsHoistedByAnnexB()
    {
        Assert.Equal("function",
            Eval("function f() { eval('{ function g() {} }'); return typeof g; } f()").ToString());
    }

    // Problems 76 / 77 / 78 / 79 (annexB/built-ins/String/prototype/{link,anchor,
    // fontcolor,fontsize}/*): the legacy String.prototype HTML wrappers must escape
    // U+0022 (`"`) inside the attribute value to the six-code-unit sequence
    // "&quot;" per Annex B.2.2.1 CreateHTML step 4.b. The previous CreateHtmlWrapper
    // appended the raw value, so e.g. `'_'.anchor('\x22')` produced
    // `<a name="""_</a>` instead of `<a name="&quot;">_</a>`.
    [Theory]
    [InlineData("'_'.anchor('\\x22')", "<a name=\"&quot;\">_</a>")]
    [InlineData("'_'.link('\\x22')", "<a href=\"&quot;\">_</a>")]
    [InlineData("'_'.fontcolor('\\x22')", "<font color=\"&quot;\">_</font>")]
    [InlineData("'_'.fontsize('\\x22')", "<font size=\"&quot;\">_</font>")]
    [InlineData("'_'.anchor('a\"b\"c')", "<a name=\"a&quot;b&quot;c\">_</a>")]
    public void HtmlWrapperEscapesQuoteInAttribute(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Non-`"` attribute characters (`<`, `>`, `&`) are NOT escaped per the same
    // spec step; only the value-delimiting quote needs escaping.
    [Theory]
    [InlineData("'_'.anchor('<')", "<a name=\"<\">_</a>")]
    [InlineData("'_'.link('<a&b>')", "<a href=\"<a&b>\">_</a>")]
    public void HtmlWrapperLeavesOtherCharactersAlone(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Tag-only wrappers (no attribute) are unaffected.
    [Theory]
    [InlineData("'<'.bold()", "<b><</b>")]
    [InlineData("'_'.italics()", "<i>_</i>")]
    public void HtmlWrappersWithoutAttributeUnaffected(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 94 (language/statements/for/scope-head-lex-open): a C-style
    // `for (let x = …; test; update)` introduces a fresh lexical environment for the
    // head bindings. Without a compile-time scope push around VisitForStatement the
    // head's `let x` shared the enclosing scope's binding table, silently replacing
    // an outer `let x` of the same name and leaving the loop's value visible after
    // termination. A captured `probeBefore` closure that resolves x in the outer
    // scope therefore wrongly saw the inner value. Only let / const heads (with the
    // parser's useLoopEnv lowering) need the synthetic scope; a plain for / var head
    // keeps the original single-scope path.
    [Fact]
    public void ForLetHeadDoesNotLeakIntoEnclosingLet()
    {
        Assert.Equal("outside",
            Eval("let x='outside'; var probe = function(){ return x; }; for (let x='inside'; false;){} probe()").ToString());
    }

    [Fact]
    public void ForConstHeadDoesNotLeakIntoEnclosingConst()
    {
        Assert.Equal("outside",
            Eval("const x='outside'; var probe = function(){ return x; }; for (const x='inside'; false;){} probe()").ToString());
    }

    // A var head is hoisted to the function scope as before, so a same-named outer
    // var IS overwritten — this regression-test pins that behaviour.
    [Fact]
    public void ForVarHeadStillFunctionScoped()
    {
        Assert.Equal("inside",
            Eval("(function(){ var x='outside'; for (var x='inside'; false;){} return x; })()").ToString());
    }

    // The probes captured inside the head and body still see the per-iteration let
    // binding — the scope push isolates the head from the OUTER scope but doesn't
    // collapse the parser's useLoopEnv lowering.
    [Fact]
    public void ForLetClosuresInHeadAndBodyResolveToLoopBinding()
    {
        var code = "var probeDecl, probeTest, probeIncr, probeBody;"
            + "var run = true;"
            + "for ("
            + "    let x = 'inside', _ = probeDecl = function() { return x; };"
            + "    run && (probeTest = function() { return x; });"
            + "    probeIncr = function() { return x; }"
            + "  )"
            + "  probeBody = function() { return x; }, run = false;"
            + "probeDecl() + '|' + probeTest() + '|' + probeBody() + '|' + probeIncr()";
        Assert.Equal("inside|inside|inside|inside", Eval(code).ToString());
    }
}
