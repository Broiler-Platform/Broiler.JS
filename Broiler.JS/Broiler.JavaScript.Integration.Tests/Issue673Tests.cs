using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/673
//
// Category 1 (sm "structurally equal" failures) included the
// test/staging/sm/generators/delegating-yield-* files. The shared root cause was
// that a `yield*` expression evaluated to `undefined`/`null` instead of the
// delegated iterator's return value (the value of its final `{ value, done:true }`
// result). See JSIterator.MoveNext(JSValue, out JSValue) and the generator
// delegation driver in ClrGeneratorV2.Next.
//
// Category 10 ("Value is not iterable") covered iterating a *primitive* whose
// prototype defines Symbol.iterator (e.g. `[...true]`, `yield* 0`, `new Map(0)`).
// Primitives now follow the @@iterator protocol via their wrapper prototype
// (JSPrimitive.GetIterableEnumerator).
//
// Category 8 ("Unexpected token ... constructor" at Compile) was a parser bug:
// a getter/setter named `constructor` in an *object literal* (e.g.
// `{ get constructor() {} }`, `{ set constructor(_) {} }`) failed to parse. The
// name `constructor` is only special inside a class body, so the constructor
// classification in FastParser.ObjectProperty is now gated on `isClass`.
//
// Category 2 ("Expected SameValue(0, 1)") covered IteratorClose: calling
// Generator.prototype.return() on a generator suspended at a `yield` must resume
// it with a "return" completion so enclosing `finally` blocks run (closing a
// destructuring/for-of iterator). See JSGenerator.Return and the
// GeneratorReturnCompletion handling in ClrGeneratorV2.GetNext.
//
// Current failures and host gaps are tracked by
// scripts/compliance/test262-failures.txt and docs/compliance/known-gaps.md.
// This test file preserves the issue-specific regressions without treating the
// issue's original category list as the current roadmap.
public class Issue673Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void YieldStar_EvaluatesToDelegateReturnValue()
        => Assert.Equal("3", Eval(@"
            function* inner(){ yield 1; yield 2; return 3; }
            function* outer(){ var r = yield* inner(); yield r; }
            var it = outer();
            it.next(); it.next();        // 1, 2
            String(it.next().value);     // r === 3 is yielded here
        "));

    [Fact]
    public void YieldStar_ForwardsYieldedValuesThenReturnValue()
        => Assert.Equal("[1,2,3]", Eval(@"
            function* inner(){ yield 1; yield 2; return 3; }
            function* outer(){ var r = yield* inner(); yield r; }
            JSON.stringify([...outer()]);
        "));

    [Fact]
    public void YieldStar_ReturnValueIsUsableInExpression()
        => Assert.Equal("[\"a\",\"got:R\"]", Eval(@"
            function* inner(){ yield 'a'; return 'R'; }
            function* outer(){ var v = yield* inner(); yield 'got:' + v; }
            JSON.stringify([...outer()]);
        "));

    [Fact]
    public void YieldStar_OverArrayHasUndefinedValue()
        => Assert.Equal("undefined", Eval(@"
            function* outer(){ var r = yield* [1, 2, 3]; yield r; }
            var it = outer();
            it.next(); it.next(); it.next();   // 1, 2, 3
            String(it.next().value);           // spreading yielded r === undefined
        "));

    [Fact]
    public void YieldStar_NestedDelegationPropagatesReturnValue()
        => Assert.Equal("inner", Eval(@"
            function* inner(){ yield 1; return 'inner'; }
            function* middle(){ var r = yield* inner(); return r; }
            function* outer(){ var r = yield* middle(); yield r; }
            var it = outer();
            it.next();                   // 1
            String(it.next().value);     // outer yields 'inner'
        "));

    [Fact]
    public void YieldStar_ForwardsSentValuesToInner()
        => Assert.Equal("sent:42", Eval(@"
            function* inner(){ var got = yield 1; return 'sent:' + got; }
            function* outer(){ var r = yield* inner(); yield r; }
            var it = outer();
            it.next();                   // yields 1, pauses at `yield 1` in inner
            String(it.next(42).value);   // sends 42 into inner; inner returns 'sent:42'
        "));

    // ---- Category 8: `constructor` accessor in object literals parses ----

    [Fact]
    public void ObjectLiteral_GetConstructor_Parses()
        => Assert.Equal("42", Eval("var o = { get constructor() { return 42; } }; String(o.constructor);"));

    [Fact]
    public void ObjectLiteral_SetConstructor_Parses()
        => Assert.Equal("7", Eval("var hit = 0; var o = { set constructor(v) { hit = v; } }; o.constructor = 7; String(hit);"));

    [Fact]
    public void ObjectLiteral_MethodAndDataConstructor_StillWork()
        => Assert.Equal("5,9", Eval(@"
            var a = { constructor() { return 5; } };
            var b = { constructor: 9 };
            String(a.constructor()) + ',' + String(b.constructor);
        "));

    [Fact]
    public void GetConstructorAccessor_IsNotInvokedByPromiseThen()
        // test/built-ins/Promise/prototype/then/context-check-on-entry.js: the
        // IsPromise check must reject before any `constructor` lookup.
        => Assert.Equal("TypeError", Eval(@"
            var object = { get constructor() { throw new Error('getter called'); } };
            var name = 'none';
            try { Promise.prototype.then.call(object); }
            catch (e) { name = e.constructor.name; }
            name;
        "));

    [Fact]
    public void SubclassWithBaseConstructorSetter_DoesNotInvokeSetter()
        // test/language/statements/class/subclass/superclass-prototype-setter-constructor.js
        => Assert.Equal("ok", Eval(@"
            function Base() {}
            Base.prototype = { set constructor(_) { throw new Error('setter reached'); } };
            class C extends Base {}
            new C();
            'ok';
        "));

    [Fact]
    public void ClassGetConstructorAccessor_IsStillRejected()
    {
        // A class get/set accessor named `constructor` remains a SyntaxError.
        Assert.ThrowsAny<Exception>(() => Eval("class C { get constructor() {} }"));
    }

    // ---- Category 10: iterating a primitive via its prototype's @@iterator ----

    [Fact]
    public void SpreadPrimitiveBoolean_UsesPrototypeIterator()
        => Assert.Equal("[true]", Eval(@"
            Boolean.prototype[Symbol.iterator] = function*() { yield this.valueOf(); };
            JSON.stringify([...true]);
        "));

    [Fact]
    public void YieldStarOverPrimitive_UsesPrototypeIterator()
        => Assert.Equal("[true]", Eval(@"
            Boolean.prototype[Symbol.iterator] = function*() { yield this.valueOf(); };
            function* g() { yield* true; }
            JSON.stringify([...g()]);
        "));

    [Fact]
    public void ArrayFromPrimitive_UsesPrototypeIterator()
        => Assert.Equal("[true]", Eval(@"
            Boolean.prototype[Symbol.iterator] = function*() { yield this.valueOf(); };
            JSON.stringify(Array.from(true));
        "));

    [Fact]
    public void MapFromPrimitiveNumber_PreservesPrimitiveReceiver()
        // test/staging/sm/Map/iterable.js: `new Map(0)` boxes 0 to look up
        // Number.prototype[Symbol.iterator]; the method's `this` stays a number.
        => Assert.Equal("number", Eval(@"
            var t = 'none';
            Object.defineProperty(Number.prototype, Symbol.iterator, {
              value() { 'use strict'; t = typeof this; return { next() { return { done: true }; } }; },
              configurable: true
            });
            new Map(0);
            t;
        "));

    [Fact]
    public void MapConstructor_DoesNotPassArgumentsToNext()
        // test/staging/sm/Map/iterable.js: next() receives zero arguments.
        => Assert.Equal("0", Eval(@"
            var len = -1;
            var iterable = {
              [Symbol.iterator]() { return this; },
              next() { len = arguments.length; return { done: true }; }
            };
            new Map(iterable);
            String(len);
        "));

    [Fact]
    public void PrimitiveWithoutIterator_StillThrows()
        // A primitive without a Symbol.iterator on its prototype is not iterable.
        => Assert.ThrowsAny<Exception>(() => Eval("[...42]"));

    // ---- Category 2: Generator.prototype.return() runs finally / IteratorClose ----

    [Fact]
    public void GeneratorReturn_RunsFinally()
        => Assert.Equal("F,99,true", Eval(@"
            var log = '';
            function* g() { try { yield 1; } finally { log += 'F'; } }
            var it = g(); it.next();
            var r = it.return(99);
            log + ',' + r.value + ',' + r.done;
        "));

    [Fact]
    public void GeneratorReturn_SkipsCatch_RunsFinally()
        => Assert.Equal("F,7,true", Eval(@"
            var log = '';
            function* g() { try { yield 1; } catch (e) { log += 'C'; } finally { log += 'F'; } }
            var it = g(); it.next();
            var r = it.return(7);
            log + ',' + r.value + ',' + r.done;
        "));

    [Fact]
    public void GeneratorReturn_FinallyCanOverrideCompletion()
        => Assert.Equal("over,true", Eval(@"
            function* g() { try { yield 1; } finally { return 'over'; } }
            var it = g(); it.next();
            var r = it.return(9);
            r.value + ',' + r.done;
        "));

    [Fact]
    public void GeneratorReturn_BeforeStart_DoesNotRunBody()
        => Assert.Equal("none,3,true", Eval(@"
            var log = 'none';
            function* g() { try { log = 'ran'; yield 1; } finally { log = 'F'; } }
            var it = g();
            var r = it.return(3);
            log + ',' + r.value + ',' + r.done;
        "));

    [Fact]
    public void GeneratorReturn_ClosesDestructuringIterator()
        // test/language/.../dstr/array-rest-iter-rtrn-close.js shape: a yield inside
        // a destructuring rest target; return() must close the partially-read iterator.
        => Assert.Equal("1", Eval(@"
            var returnCount = 0;
            var iterable = {};
            var iterator = {
              next() { return { done: false, value: 1 }; },
              return() { returnCount += 1; return {}; }
            };
            iterable[Symbol.iterator] = function() { return iterator; };
            function* g() { var x; [ x, ...{}[yield] ] = iterable; }
            var it = g(); it.next(); it.return(5);
            String(returnCount);
        "));

    [Fact]
    public void GeneratorReturn_ClosesForOfIterator()
        => Assert.Equal("closed", Eval(@"
            var state = 'open';
            var iter = {
              next() { return { done: false, value: 1 }; },
              return() { state = 'closed'; return {}; },
              [Symbol.iterator]() { return this; }
            };
            function* g() { for (var v of iter) { yield v; } }
            var it = g(); it.next(); it.return();
            state;
        "));

    [Fact]
    public void GeneratorReturn_RunsNestedFinallies()
        => Assert.Equal("BA,done", Eval(@"
            var log = '';
            function* g() {
              try { try { yield 1; } finally { log += 'B'; } } finally { log += 'A'; }
            }
            var it = g(); it.next();
            it.return();
            log + ',done';
        "));

    // ---- Category 1: Array.prototype.concat honours @@isConcatSpreadable ----

    [Fact]
    public void Concat_ArrayWithSpreadableFalse_IsNotSpread()
        => Assert.Equal("[0,[1,2]]", Eval(@"
            var a = [1, 2];
            a[Symbol.isConcatSpreadable] = false;
            JSON.stringify([0].concat(a));
        "));

    [Fact]
    public void Concat_ArrayLikeWithSpreadableTrue_IsSpread()
        => Assert.Equal("[0,\"a\",\"b\"]", Eval(@"
            var o = { length: 2, 0: 'a', 1: 'b', [Symbol.isConcatSpreadable]: true };
            JSON.stringify([0].concat(o));
        "));

    [Fact]
    public void Concat_DefaultArraySpreads_PlainObjectDoesNot()
        => Assert.Equal("[1,2,3,{\"x\":1}]", Eval(@"
            JSON.stringify([1].concat([2, 3], { x: 1 }));
        "));
}
