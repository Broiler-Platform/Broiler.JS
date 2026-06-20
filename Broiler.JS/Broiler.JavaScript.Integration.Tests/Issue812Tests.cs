using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/812
//
// Fixed here (Problem 37 — `async of` at the head of a C-style for loop):
//
//   `for (async of => {}; …; …)` begins with the async arrow function
//   `async of => {}` (a single parameter named `of`), so it is the *init*
//   clause of a C-style for, not a for-of head. The grammar only forbids the
//   bare `async of <iterable>` sequence as the left-hand side of a *sync*
//   for-of (it is permitted in `for await`, where `async` is the loop target).
//   The parser intercepted every `async of` and reported "'async' is not
//   allowed as the left-hand side of a for-of loop", rejecting the valid arrow
//   init. It now peeks past `of` for the `=>` and leaves the arrow alone.
public class Issue812Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static string ErrorName(string body) => Eval(
        "let t='NONE'; try { " + body + " } catch (e) { t = e.constructor.name; } t");

    [Fact]
    public void AsyncOfArrowIsForInitNotForOf()
        => Assert.Equal("3", Eval(
            "let n = 0; for (async of => {}; n < 3; ) { n++; } '' + n"));

    [Fact]
    public void AsyncOfArrowInitRunsOnce()
        => Assert.Equal("ok", Eval(
            "let r = 'no'; for (let f = (async of => {}); ; ) { r = 'ok'; break; } r"));

    [Fact]
    public void BareAsyncOfForOfIsSyntaxError()
        => Assert.Equal("SyntaxError", ErrorName("eval('for (async of [1, 2, 3]) { }')"));

    // Problem 59 — the braced Unicode code-point escape `\u{H..H}` (valid only in
    // u/v mode). A `/…/u` regex *literal* has it rewritten to a fixed-width `\uHHHH`
    // escape by the source scanner, but a `new RegExp(string, "u")` pattern reached
    // the translator with the brace form intact, which .NET rejected with
    // "Invalid pattern '[\u{0}]' … Insufficient or invalid hexadecimal digits."

    [Fact]
    public void RegExpStringBracedEscape_BmpInClass_Matches()
        => Assert.Equal("true", Eval("'' + new RegExp('[\\\\u{0}]', 'u').test('\\u0000')"));

    [Fact]
    public void RegExpStringBracedEscape_BmpInClass_DoesNotMatchOther()
        => Assert.Equal("false", Eval("'' + new RegExp('[\\\\u{0}]', 'u').test('a')"));

    [Fact]
    public void RegExpStringBracedEscape_AstralInClass_MatchesWholeCodePoint()
        => Assert.Equal("true", Eval("'' + new RegExp('[\\\\u{1F600}]', 'u').test('\\u{1F600}')"));

    [Fact]
    public void RegExpStringBracedEscape_Atom_Matches()
        => Assert.Equal("true", Eval("'' + new RegExp('\\\\u{41}', 'u').test('A')"));

    [Fact]
    public void RegExpStringBracedEscape_MetacharacterStaysLiteral()
        => Assert.Equal("true", Eval("'' + new RegExp('\\\\u{3f}', 'u').test('?')"));

    // Problem 91 — the Decode abstract operation (§19.2.6.5) must reject a percent
    // sequence whose octets are not a valid UTF-8 encoding of a single Unicode code
    // point: an overlong form, a value above U+10FFFF, or a surrogate (e.g. %ED%BF%BF,
    // the CESU-8 encoding of U+DFFF). The default UTF-8 decoder substituted U+FFFD
    // instead of throwing, so no URIError was raised.

    [Fact]
    public void DecodeURIComponent_SurrogateEncoding_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURIComponent('%ED%BF%BF')"));

    [Fact]
    public void DecodeURIComponent_OverlongEncoding_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURIComponent('%C0%80')"));

    [Fact]
    public void DecodeURIComponent_AboveMaxCodePoint_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURIComponent('%F4%90%80%80')"));

    [Fact]
    public void DecodeURI_LeadingSurrogateEncoding_ThrowsURIError()
        => Assert.Equal("URIError", ErrorName("decodeURI('%ED%A0%80')"));

    [Fact]
    public void DecodeURIComponent_ValidMultiByte_StillDecodes()
        => Assert.Equal("€", Eval("decodeURIComponent('%E2%82%AC')"));

    [Fact]
    public void DecodeURIComponent_AstralCodePoint_StillDecodes()
        => Assert.Equal("true", Eval("'' + (decodeURIComponent('%F0%9F%98%80') === '\\u{1F600}')"));

    // Problems 88/99/100 — a chained `new` (`new new X(args)`) is `new (new X(args))`:
    // the inner NewExpression consumes the arguments and the outer argument-less `new`
    // is applied to its result. The parser dropped the outer `new`, so `new new X(args)`
    // evaluated to `new X(args)` and never raised the "not a constructor" TypeError for
    // the outer `new` on the (non-constructor) instance.

    [Fact]
    public void NewNewNumber_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("new new Number(1)"));

    [Fact]
    public void NewNewBoolean_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("new new Boolean(true)"));

    [Fact]
    public void NewNewString_NoParens_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("new new String"));

    [Fact]
    public void NewNewUserConstructor_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("function C(){}; new new C()"));

    [Fact]
    public void TripleNew_ThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("function C(){}; new new new C()"));

    [Fact]
    public void SingleNew_StillConstructs()
        => Assert.Equal("5", Eval("function C(v){this.v=v}; '' + new C(5).v"));

    // Problem 97 — String.prototype.replace with a string search value and a function
    // replacement must call the function with (matched, position, string). The function
    // was invoked with no arguments (and even when there was no match), so the position
    // and string were undefined inside the replacer.

    [Fact]
    public void Replace_StringSearch_FunctionReceivesMatchPositionAndString()
        => Assert.Equal("a[b|1|abc]c", Eval(
            "'abc'.replace('b', function(m, p, s){ return '[' + m + '|' + p + '|' + s + ']'; })"));

    [Fact]
    public void Replace_NullSearch_CoercesToStringAndPassesPosition()
        => Assert.Equal("g1una", Eval(
            "'gnulluna'.replace(null, function(a1, a2, a3){ return a2 + ''; })"));

    [Fact]
    public void Replace_NoMatch_DoesNotCallFunction()
        => Assert.Equal("abc:0", Eval(
            "var n = 0; var r = 'abc'.replace('z', function(){ n++; return 'X'; }); r + ':' + n"));

    [Fact]
    public void Replace_StringSearch_LiteralReplacementStillWorks()
        => Assert.Equal("aXc", Eval("'abc'.replace('b', 'X')"));

    // Problem 96 — String.prototype.match coerces its argument through
    // RegExpCreate(regexp, undefined): only an *undefined* pattern becomes the empty
    // pattern; null is coerced with ToString to "null". match treated null and
    // undefined alike (empty pattern), so `"gnulluna".match(null)` matched "" at index
    // 0 instead of the literal "null" at index 1.

    [Fact]
    public void Match_Null_SearchesForLiteralNull()
        => Assert.Equal("null", Eval("'' + 'gnulluna'.match(null)[0]"));

    [Fact]
    public void Match_Null_MatchIndexIsOne()
        => Assert.Equal("1", Eval("'' + 'gnulluna'.match(null).index"));

    [Fact]
    public void Match_Undefined_UsesEmptyPattern()
        => Assert.Equal("true", Eval("'' + ('aundefinedb'.match(undefined)[0] === '')"));

    [Fact]
    public void Match_RegExpAndStringArguments_StillWork()
        => Assert.Equal("bb:b", Eval("'abbc'.match(/b+/)[0] + ':' + 'abc'.match('b')[0]"));

    // Problem 79 — a PlainTime ISO string may carry a numeric UTC offset, which a
    // zone-less PlainTime ignores, but the offset must still be well-formed: its
    // hour (00-23), minute (00-59) and second (00-59) components are range-checked.
    // "00:00-24:00" (offset hour 24) was silently parsed as 00:00 instead of throwing.

    [Fact]
    public void PlainTime_OffsetHourOutOfRange_ThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName("Temporal.PlainTime.from('00:00-24:00')"));

    [Fact]
    public void PlainTime_OffsetMinuteOutOfRange_ThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName("Temporal.PlainTime.from('00:00+05:60')"));

    [Fact]
    public void PlainTime_ValidOffsetIsIgnored()
        => Assert.Equal("12:00:00", Eval("Temporal.PlainTime.from('12:00+01:00').toString()"));

    [Fact]
    public void PlainTime_MaxValidOffsetIsIgnored()
        => Assert.Equal("12:00:00", Eval("Temporal.PlainTime.from('12:00-23:59').toString()"));

    // Problem 92 — the built-in global constructors Function and Object (and the
    // globalThis reference) must be { writable, enumerable: false, configurable }.
    // They were installed via the enumerable-defaulting indexer, so they wrongly
    // appeared in a global for-in / Object.keys enumeration (e.g. the DontEnum check
    // on `Function` failed). Other constructors (Array, String, …) were already
    // non-enumerable via the built-in registry.

    [Fact]
    public void GlobalFunction_IsNonEnumerable()
        => Assert.Equal("false:true:true", Eval(
            "var d = Object.getOwnPropertyDescriptor(globalThis, 'Function'); d.enumerable + ':' + d.writable + ':' + d.configurable"));

    [Fact]
    public void GlobalObject_IsNonEnumerable()
        => Assert.Equal("false", Eval("'' + globalThis.propertyIsEnumerable('Object')"));

    [Fact]
    public void GlobalThis_IsNonEnumerable()
        => Assert.Equal("false:true:true", Eval(
            "var d = Object.getOwnPropertyDescriptor(globalThis, 'globalThis'); d.enumerable + ':' + d.writable + ':' + d.configurable"));

    [Fact]
    public void GlobalConstructors_StillUsable()
        => Assert.Equal("function:3:5", Eval(
            "typeof Function + ':' + Object.keys({a:1,b:2,c:3}).length + ':' + (new Function('a','b','return a+b'))(2,3)"));

    // Problems 93/94 — for-in (EnumerateObjectProperties) must visit each property
    // name at most once: a key shadowed by a nearer object on the prototype chain is
    // skipped on the ancestors. The key enumerator walked the chain without
    // de-duplicating, so a shadowed inherited key was produced again.

    [Fact]
    public void ForIn_ShadowedKeyVisitedOnce()
        => Assert.Equal("1", Eval(
            "var p={a:1}; var o=Object.create(p); o.a=2; var n=0; for(var k in o){ if(k==='a') n++; } '' + n"));

    [Fact]
    public void ForIn_ShadowedKeyUsesOwnValue()
        => Assert.Equal("2", Eval(
            "var p={a:1}; var o=Object.create(p); o.a=2; var v; for(var k in o){ if(k==='a') v=o[k]; } '' + v"));

    [Fact]
    public void ForIn_DeepChainDeduplicates()
        => Assert.Equal("x,y,z", Eval(
            "var a={x:1}; var b=Object.create(a); b.y=2; var c=Object.create(b); c.z=3; c.x=9;" +
            "var r=[]; for(var k in c) r.push(k); r.sort().join(',')"));

    [Fact]
    public void ForIn_InheritedAndOwnAllEnumerated()
        => Assert.Equal("a,b,c,d", Eval(
            "var p={a:1,b:2}; var o=Object.create(p); o.c=3; o.d=4;" +
            "var r=[]; for(var k in o) r.push(k); r.sort().join(',')"));

    // Problems 47/48/49 (and the await-using head variants) — a `using` / `await using`
    // ForBinding is valid at the head of a for-of loop (a lexical binding disposed at the
    // end of each iteration). The for-head parser did not recognise it, rejecting
    // `for (using x of …)` with "Unexpected token Identifier". `using`/`await using` is
    // a declaration when followed by a BindingIdentifier — a for-of ForBinding (`for (using x of
    // …)`) or a C-style LexicalDeclaration (`for (using x = …; …; …)`, disposed when the loop scope
    // is torn down). It remains disallowed (a SyntaxError) in a for-in head, and `for (using of x)`
    // keeps `using` as the loop target.

    [Fact]
    public void ForOfUsing_DisposesEachIteration()
        => Assert.Equal("b1,d1,b2,d2", Eval(
            "var log=[]; var mk=(n)=>({[Symbol.dispose](){log.push('d'+n)}, n});" +
            "for (using r of [mk(1), mk(2)]) { log.push('b'+r.n); } log.join(',')"));

    [Fact]
    public void ForOfUsing_BindingIsAccessible()
        => Assert.Equal("10,20", Eval(
            "var out=[]; for (using r of [{[Symbol.dispose](){}, v:10},{[Symbol.dispose](){}, v:20}]) { out.push(r.v); } out.join(',')"));

    [Fact]
    public void ForOfUsing_OfAsLoopTarget()
        => Assert.Equal("3", Eval("var using; for (using of [1, 2, 3]) {} '' + using"));

    // A `using` LexicalDeclaration is valid in a C-style for head; its resource is disposed when
    // the loop's lexical environment is torn down.
    [Fact]
    public void ForUsing_PlainForHeadDisposesResource()
        => Assert.Equal("d", Eval(
            "var log=[]; for (using x = {[Symbol.dispose](){log.push('d')}}; false; ) {} log.join(',')"));

    [Fact]
    public void ForUsing_ForInHeadIsSyntaxError()
        => Assert.Equal("SyntaxError", ErrorName("eval('for (using x in {}) {}')"));

    // Problem 56 — in an ordinary script `await` is a plain IdentifierReference (top-level
    // await applies only to modules / opt-in eval). The parser parsed a top-level `await`
    // followed by an operand as an AwaitExpression (then rejected it), so `await + x` /
    // `await(x)` failed instead of treating `await` as the identifier it is.

    [Fact]
    public void Script_AwaitIdentifier_BinaryPlus()
        => Assert.Equal("5x", Eval("var await = 5; '' + await + 'x'"));

    [Fact]
    public void Script_AwaitIdentifier_InParenthesizedSum()
        => Assert.Equal("8", Eval("var await = 5; '' + (await + 3)"));

    [Fact]
    public void Script_AwaitIdentifier_Called()
        => Assert.Equal("3", Eval("var await = (x) => x; '' + await(3)"));

    [Fact]
    public void Script_AwaitIdentifier_Bare()
        => Assert.Equal("5", Eval("var await = 5; '' + await"));

    // Problem 72 — an arrow function has no `arguments` of its own; it refers to the
    // enclosing (non-arrow) function's. The enclosing function materialised its
    // `arguments` object only when it referenced `arguments` directly, so an arrow that
    // was the *only* user of `arguments` resolved it as an undefined free variable
    // ("arguments is not defined").

    [Fact]
    public void Arrow_InheritsEnclosingArguments()
        => Assert.Equal("2", Eval("function f(){ return (() => arguments.length)(); } '' + f(1, 2)"));

    [Fact]
    public void Arrow_Stored_InheritsEnclosingArguments()
        => Assert.Equal("9", Eval("function f(){ var g = () => arguments[0]; return g(); } '' + f(9)"));

    [Fact]
    public void Arrow_Nested_InheritsEnclosingArguments()
        => Assert.Equal("3", Eval("function f(){ return (() => (() => arguments.length)())(); } '' + f(1, 2, 3)"));

    [Fact]
    public void Arrow_MutatesMappedArgument()
        => Assert.Equal("9", Eval("function f(x){ (() => { arguments[0] = 9; })(); return x; } '' + f(1)"));

    [Fact]
    public void Arrow_AndDirectArgumentsCoexist()
        => Assert.Equal("2:2", Eval("function f(){ var a = (() => arguments.length)(); return a + ':' + arguments.length; } '' + f(1, 2)"));

    [Fact]
    public void Arrow_AtProgramScope_ArgumentsUnbound()
        => Assert.Equal("ReferenceError", ErrorName("(() => arguments)()"));
}
