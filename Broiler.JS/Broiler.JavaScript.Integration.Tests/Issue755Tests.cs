using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/755
//
// Fixed here:
//
//   Problem 23 — Array.prototype[Symbol.unscopables] data property must be
//   non-writable ({ writable:false, enumerable:false, configurable:true }).
//
//   Problem 26 — BigInt.asIntN()/asUintN() with a missing `bigint` argument is a
//   TypeError (ToBigInt(undefined)), not a ReferenceError. The absent argument
//   slot used to surface as a CLR null and NRE → ReferenceError.
//
//   Problem 27/28 — %TypedArray%.prototype.slice/subarray coerce start/end via
//   ToIntegerOrInfinity (valueOf, strings, booleans, NaN→0) and treat an undefined
//   end — explicit or absent — as len, not 0.
//
//   Problem 29 — Intl.DurationFormat.prototype.format/formatToParts reject a
//   non-object/non-string argument with a TypeError, a string with a RangeError,
//   and an object with no duration fields with a TypeError (ToDurationRecord).
//
//   Problem 30/31 — Date setters (setMinutes/setSeconds/setHours/setMonth/
//   setFullYear and UTC variants): an explicitly-passed `undefined` optional
//   component is "specified" (keyed off argument count), so ToNumber(undefined)=NaN
//   makes the result NaN rather than reusing the current field value.
//
//   Problem 33 — A YieldExpression in a generator's FormalParameters and an
//   AwaitExpression in an async function's FormalParameters are early SyntaxErrors.
//
//   Problem 34 — JSON.parse reviver / JSON.stringify replacer build the "" wrapper
//   holder with CreateDataPropertyOrThrow, not [[Set]], so an inherited setter on
//   Object.prototype[""] is not invoked.
//
//   Problem 36 — Object.entries/Object.values perform [[GetOwnProperty]] and
//   [[Get]] interleaved per key (observable order for a Proxy).
//
//   Problem 37/38 — A TypedArray constructor length is ToIndex: it truncates
//   toward zero first, so a fractional value in (-1, 0) floors to 0 instead of
//   throwing a RangeError.
//
//   Problem 39/40 (partial) — a non-Unicode regex literal accepts an Annex B
//   IdentityEscape of any character that is not a recognized escape: `\_`, `\C`, an
//   accented letter or combining mark, and a malformed `\u`/`\x`/`\k` (`/\u/`,
//   `/\x/`, `/\k/`). Both the lexer (it no longer falls back to division) and the
//   runtime (it no longer hands .NET an unknown escape) are fixed. (The BMP-loop
//   test files still fail at the lone-surrogate range — an orthogonal pre-existing
//   `.source` serialization bug, not addressed here.)
//
//   Problem 35 — Generator.prototype.return run while the generator is suspended at
//   a `yield` nested inside a try/catch must still execute the enclosing `finally`
//   blocks: the state machine now unwinds the try-region stack explicitly (popping
//   regions whose own catch/finally is running, or that cannot handle the
//   completion) instead of relying on an outer frame that does not exist on a direct
//   resume. Also: `throw()`/`return()` on a completed or suspended-start generator
//   no longer runs the body.
//
//   Problem 17 — `delete` of an identifier that resolves through a `with`-fallback
//   overlay (a captured outer `let`/`const`/`var` made resolvable inside a `with`
//   body, e.g. when @@unscopables blocks the object property) returns false: it is
//   a declarative/global binding, not the transient configurable property the
//   overlay publishes for resolution.
//
//   Problem 13 — a function created by the dynamic Function constructor must not
//   expose an `anonymous` binding in its body: it is built via OrdinaryFunctionCreate
//   (no self-name binding), with name "anonymous" and the `function anonymous(...)`
//   toString stamped on afterward.
//
//   Problem 9 — three related Array/Proxy/Reflect bugs exposed by pop/shift via a
//   Proxy: (a) Array [[Set]] of `length` with a different receiver redirects to the
//   receiver's [[DefineOwnProperty]] (OrdinarySet) instead of mutating this array;
//   (b) Proxy invariant checks see an Array's virtual `length` (read via
//   [[GetOwnProperty]]); (c) Reflect.defineProperty dispatches through the JSValue
//   [[DefineOwnProperty]] overload so it actually shrinks/grows an Array's length,
//   and returns false (rather than throwing) on an invariant violation.
public class Issue755Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 23: @@unscopables descriptor is non-writable ----

    [Fact]
    public void ArrayUnscopablesIsNonWritable()
        => Assert.Equal("false", Eval(
            "Object.getOwnPropertyDescriptor(Array.prototype, Symbol.unscopables).writable.toString()"));

    [Fact]
    public void ArrayUnscopablesIsConfigurableNonEnumerable()
        => Assert.Equal("true,false,true", Eval(
            "var d=Object.getOwnPropertyDescriptor(Array.prototype, Symbol.unscopables);" +
            "[d.configurable,d.enumerable,d.value!==undefined].join(',')"));

    // ---- Problem 26: BigInt.asIntN/asUintN missing argument → TypeError ----

    [Fact]
    public void BigIntAsIntNNoArgumentThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try{BigInt.asIntN();'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void BigIntAsIntNOneArgumentThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try{BigInt.asIntN(0);'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void BigIntAsUintNNoArgumentThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try{BigInt.asUintN();'no throw';}catch(e){e.constructor.name;}"));

    // ---- Problem 27/28: TypedArray slice/subarray end coercion ----

    [Fact]
    public void TypedArraySliceUndefinedEndIsFullLength()
        => Assert.Equal("40,41,42,43", Eval(
            "Array.prototype.join.call(new Int8Array([40,41,42,43]).slice(0, undefined))"));

    [Fact]
    public void TypedArraySliceCoercesEnd()
        => Assert.Equal("40,41|40|40,41,42", Eval(
            "var s=new Int8Array([40,41,42,43]);var j=function(a){return Array.prototype.join.call(a);};" +
            "[j(s.slice(0,{valueOf:function(){return 2;}})), j(s.slice(0,'1')), j(s.slice(0,-1))].join('|')"));

    [Fact]
    public void TypedArraySubarrayUndefinedEndIsFullLength()
        => Assert.Equal("4", Eval(
            "new Int8Array([40,41,42,43]).subarray(0, undefined).length.toString()"));

    // ---- Problem 29: Intl.DurationFormat.format argument validation ----

    [Fact]
    public void DurationFormatRejectsNonObject()
        => Assert.Equal("TypeError,TypeError,TypeError,TypeError", Eval(
            "var df=new Intl.DurationFormat();function t(x){try{df.format(x);return 'no throw';}catch(e){return e.constructor.name;}}" +
            "[t(undefined),t(null),t(1),t(2n)].join(',')"));

    [Fact]
    public void DurationFormatRejectsEmptyObject()
        => Assert.Equal("TypeError,TypeError", Eval(
            "var df=new Intl.DurationFormat();function t(x){try{df.format(x);return 'no throw';}catch(e){return e.constructor.name;}}" +
            "[t({}),t({years:undefined})].join(',')"));

    [Fact]
    public void DurationFormatRejectsBadStringWithRangeError()
        => Assert.Equal("RangeError", Eval(
            "var df=new Intl.DurationFormat();try{df.format('bad string');'no throw';}catch(e){e.constructor.name;}"));

    // ---- Problem 30/31: Date setters explicit undefined → NaN ----

    [Fact]
    public void SetMinutesExplicitUndefinedMsIsNaN()
        => Assert.Equal("true", Eval(
            "var d=new Date(2016,6);Number.isNaN(d.setMinutes(0,0,undefined)).toString()"));

    [Fact]
    public void SetSecondsExplicitUndefinedMsIsNaN()
        => Assert.Equal("true", Eval(
            "var d=new Date(2016,6);Number.isNaN(d.setSeconds(0,undefined)).toString()"));

    [Fact]
    public void SetMonthExplicitUndefinedDayIsNaN()
        => Assert.Equal("true", Eval(
            "var d=new Date(2016,6,7,11,36,23,2);Number.isNaN(d.setMonth(6,undefined)).toString()"));

    [Fact]
    public void SetFullYearExplicitUndefinedDayIsNaN()
        => Assert.Equal("true", Eval(
            "var d=new Date(2016,6,7,11,36,23,2);Number.isNaN(d.setFullYear(2016,6,undefined)).toString()"));

    [Fact]
    public void SetMinutesMissingMsKeepsCurrentField()
        => Assert.Equal("false", Eval(
            "var d=new Date(2016,6,1,0,0,0,2);Number.isNaN(d.setMinutes(0,0)).toString()"));

    [Fact]
    public void SetFullYearCoercesDayArgument()
        => Assert.Equal("true", Eval(
            "var d=new Date(2016,6,7,11,36,23,2);" +
            "(d.setFullYear(2016,6,{valueOf:function(){return 2;}})===new Date(2016,6,2,11,36,23,2).getTime()).toString()"));

    // ---- Problem 33: yield/await in parameters are early SyntaxErrors ----

    [Fact]
    public void YieldInGeneratorParameterIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(
            "try{eval('(function*(x = yield){})');'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void AwaitInAsyncFunctionParameterIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(
            "try{eval('(async function(x = await 1){})');'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void YieldInGeneratorBodyIsAllowed()
        => Assert.Equal("function", Eval(
            "(typeof eval('(function*(){ yield 1; })')).toString()"));

    [Fact]
    public void DynamicGeneratorFunctionYieldInParameterIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(
            "var GF=Object.getPrototypeOf(function*(){}).constructor;" +
            "try{GF('x = yield','');'no throw';}catch(e){e.constructor.name;}"));

    // ---- Problem 34: JSON wrapper holder uses CreateDataProperty, not [[Set]] ----

    [Fact]
    public void JsonParseReviverWrapperDoesNotInvokeInheritedSetter()
        => Assert.Equal("object", Eval(
            "var called=false;" +
            "Object.defineProperty(Object.prototype,'',{configurable:true,set:function(){called=true;}});" +
            "var w;JSON.parse('2',function(){w=this;});" +
            "delete Object.prototype[''];" +
            "(called?'setter-called':typeof w)"));

    // ---- Problem 36: Object.entries/values interleave gOPD and Get per key ----

    [Fact]
    public void ObjectEntriesInterleavesDescriptorAndGet()
        => Assert.Equal("|ownKeys|gopd:a|get:a|gopd:b|get:b", Eval(
            "var log='';var o={a:1,b:2};" +
            "var p=new Proxy(o,{ownKeys:function(t){log+='|ownKeys';return Object.keys(t);}," +
            "getOwnPropertyDescriptor:function(t,k){log+='|gopd:'+k;return Object.getOwnPropertyDescriptor(t,k);}," +
            "get:function(t,k){log+='|get:'+k;return t[k];}});" +
            "Object.entries(p);log"));

    // ---- Problem 37/38: TypedArray constructor length is ToIndex ----

    [Fact]
    public void TypedArrayCtorFractionalNegativeLengthFloorsToZero()
        => Assert.Equal("0,0,0", Eval(
            "[new Int8Array(-0.1).length,new Int8Array(-0.99999).length,new Int8Array(0.9).length].join(',')"));

    [Fact]
    public void TypedArrayCtorCoercesLengthValues()
        => Assert.Equal("1,0,1,0", Eval(
            "[new Int8Array(true).length,new Int8Array(false).length,new Int8Array('1').length,new Int8Array(null).length].join(',')"));

    // ---- Problem 9: Array length [[Set]] receiver redirect + Reflect.defineProperty ----

    [Fact]
    public void ReflectDefinePropertyShrinksArrayLength()
        => Assert.Equal("true,0,", Eval(
            "var a=[1,2,3];var ok=Reflect.defineProperty(a,'length',{value:0});[ok,a.length,a.join(',')].join(',')"));

    [Fact]
    public void ReflectDefinePropertyGrowsArrayLength()
        => Assert.Equal("true,5", Eval(
            "var a=[1,2,3];var ok=Reflect.defineProperty(a,'length',{value:5});[ok,a.length].join(',')"));

    [Fact]
    public void ReflectDefinePropertyReadonlyLengthReturnsFalse()
        => Assert.Equal("false,3", Eval(
            "var a=[1,2,3];Reflect.defineProperty(a,'length',{writable:false});" +
            "var ok=Reflect.defineProperty(a,'length',{value:1});[ok,a.length].join(',')"));

    [Fact]
    public void ReflectDefinePropertyInvalidLengthThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try{Reflect.defineProperty([1],'length',{value:-1});'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void ObjectDefinePropertyReadonlyLengthStillThrows()
        => Assert.Equal("TypeError", Eval(
            "var a=[1,2,3];Object.defineProperty(a,'length',{writable:false});" +
            "try{Object.defineProperty(a,'length',{value:1});'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void ArrayLengthSetWithForeignReceiverRedirects()
        => Assert.Equal("gopd:length,dp:length", Eval(
            "var log=[];var p=new Proxy([],{" +
            "getOwnPropertyDescriptor:function(t,k){log.push('gopd:'+k);return Reflect.getOwnPropertyDescriptor(t,k);}," +
            "defineProperty:function(t,k,d){log.push('dp:'+k);return Reflect.defineProperty(t,k,d);}});" +
            "Reflect.set([],'length',0,p);log.join(',')"));

    [Fact]
    public void ArrayPopViaProxyShrinksUnderlyingArray()
        => Assert.Equal("3,2,12", Eval(
            "var a=[1,2,3];var p=new Proxy(a,{});var r=Array.prototype.pop.call(p);[r,a.length,a.join('')].join(',')"));

    // ---- Problem 13: dynamic Function has no `anonymous` self-binding ----

    [Fact]
    public void DynamicFunctionHasNoAnonymousBinding()
        => Assert.Equal("undefined", Eval("new Function('return typeof anonymous')()"));

    [Fact]
    public void DynamicFunctionNestedHasNoAnonymousBinding()
        => Assert.Equal("undefined", Eval(
            "new Function('return function() { return typeof anonymous; }')()()"));

    [Fact]
    public void DynamicFunctionNameIsAnonymous()
        => Assert.Equal("anonymous", Eval("new Function('a','return a').name"));

    [Fact]
    public void DynamicFunctionToStringUsesAnonymous()
        => Assert.Equal("function anonymous(a,b\n) {\nreturn a+b\n}", Eval(
            "new Function('a','b','return a+b').toString()"));

    [Fact]
    public void DynamicFunctionStillCallableWithLength()
        => Assert.Equal("5,1", Eval(
            "var f=new Function('a','return a*5');[f(1),f.length].join(',')"));

    [Fact]
    public void DynamicGeneratorFunctionNameIsAnonymous()
        => Assert.Equal("anonymous,x", Eval(
            "var GF=Object.getPrototypeOf(function*(){}).constructor;var g=GF('a','yield a');[g.name,g('x').next().value].join(',')"));

    // ---- Problem 17: delete of a lexical binding inside `with` returns false ----

    [Fact]
    public void DeleteConstInsideWithUnscopableReturnsFalse()
        => Assert.Equal("false", Eval(
            "const c=1;var e={};e[Symbol.unscopables]={c:true};var r;with(e){r=delete c;}r.toString()"));

    [Fact]
    public void DeleteLexicalBindingsInsideWith()
        => Assert.Equal("false,false,false,true", Eval(
            "const c=1;let l=2;var v=3;g=4;var e={};" +
            "e[Symbol.unscopables]={c:true,l:true,v:true,g:true};" +
            "var r=[];with(e){r.push(delete c);r.push(delete l);r.push(delete v);r.push(delete g);}r.join(',')"));

    [Fact]
    public void DeleteSloppyEvalVarInsideWithStillReturnsTrue()
        => Assert.Equal("true", Eval(
            "function f(){ eval('var ev=1;'); with({}){ return delete ev; } } f().toString()"));

    [Fact]
    public void DeleteWithObjectPropertyStillWorks()
        => Assert.Equal("true", Eval(
            "var o={p:1};with(o){delete p;}(o.p===undefined).toString()"));

    // ---- Problem 35: generator return runs nested finally; throw on completed ----

    [Fact]
    public void GeneratorReturnRunsFinallyAroundYieldInCatch()
        => Assert.Equal("f,42,true", Eval(
            "var o=[];function* g(){try{try{throw 0;}catch(e){yield 1;}}finally{o.push('f');}}" +
            "var it=g();it.next();var r=it.return(42);[o.join(','),r.value,r.done].join(',')"));

    [Fact]
    public void GeneratorReturnRunsFinallyAroundYieldInInnerTry()
        => Assert.Equal("f,42,true", Eval(
            "var o=[];function* g(){try{try{yield 1;}catch(e){throw e;}}finally{o.push('f');}}" +
            "var it=g();it.next();var r=it.return(42);[o.join(','),r.value,r.done].join(',')"));

    [Fact]
    public void GeneratorReturnRunsBothNestedFinallies()
        => Assert.Equal("inner,outer", Eval(
            "var o=[];function* g(){try{try{yield 1;}finally{o.push('inner');}}finally{o.push('outer');}}" +
            "var it=g();it.next();it.return(0);o.join(',')"));

    [Fact]
    public void GeneratorFinallyCanOverrideReturnValue()
        => Assert.Equal("99", Eval(
            "function* g(){try{yield 1;}finally{return 99;}}var it=g();it.next();it.return(5).value.toString()"));

    [Fact]
    public void GeneratorThrowOnCompletedThrowsValue()
        => Assert.Equal("E", Eval(
            "function E(){}function* g(){}var it=g();it.next();" +
            "try{it.throw(new E());'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void GeneratorThrowOnSuspendedStartThrowsValue()
        => Assert.Equal("E", Eval(
            "function E(){}function* g(){yield 1;}var it=g();" +
            "try{it.throw(new E());'no throw';}catch(e){e.constructor.name;}"));

    // ---- Problem 39/40: Annex B IdentityEscape in a non-Unicode regex ----

    [Fact]
    public void RegexIdentityEscapeUnderscoreAndLetter()
        => Assert.Equal("true,true,true", Eval(
            "[/\\_/.test('_'),/\\C/.test('C'),/O\\PQ/.test('OPQ')].join(',')"));

    [Fact]
    public void RegexMalformedUnicodeHexNamedEscapesAreIdentity()
        => Assert.Equal("true,true,true", Eval(
            "[eval('/\\\\u/').test('u'),eval('/\\\\x/').test('x'),eval('/\\\\k/').test('k')].join(',')"));

    [Fact]
    public void RegexIdentityEscapeSourcePreserved()
        => Assert.Equal("\\_,\\C,\\u", Eval(
            "[/\\_/.source,/\\C/.source,eval('/\\\\u/').source].join(',')"));

    [Fact]
    public void RegexRecognizedEscapesUnaffected()
        => Assert.Equal("true,false,true,true", Eval(
            "[/\\d/.test('5'),/\\d/.test('d'),/\\u0041/.test('A'),/\\x41/.test('A')].join(',')"));

    [Fact]
    public void RegexNewRegExpMalformedUnicodeEscape()
        => Assert.Equal("true", Eval("new RegExp('\\\\u').test('u').toString()"));
}
