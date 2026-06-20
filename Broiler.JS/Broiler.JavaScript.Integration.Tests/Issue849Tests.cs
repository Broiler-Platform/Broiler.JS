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
}
