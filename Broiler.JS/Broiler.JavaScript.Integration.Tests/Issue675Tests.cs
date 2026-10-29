using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/675
//
// Issue #675 reported ten common test262 failure categories from a historical
// full script-host run. Current failures and host gaps are tracked by
// scripts/compliance/test262-failures.txt and docs/compliance/known-gaps.md;
// this file preserves the regressions fixed for that issue.
//
// Problem 1 (Array.from-over-string with overridden iterator), Problem 3
// (finally abrupt completion), Problems 6 & 7 (Unicode ID_Start coverage —
// resolved by bumping every project from net8.0 to net10.0; .NET 10 ships
// Unicode 16 tables that recognise the 15.1/16.0 ID_Start additions that
// .NET 8's tables — and therefore the CI builds — missed) and Problems 8–10
// are fixed by this change:
//
//   * Problem 1 — iterating a string (`Array.from('ab')`, `[...'ab']`,
//     `for (var c of 'ab')`, `[a,b] = 'ab'`) ignored an overridden
//     String.prototype[@@iterator]: JSString.GetIterableEnumerator used the
//     hardcoded code-point enumerator unconditionally. It now consults
//     @@iterator and only takes the fast code-point path when the property is
//     the built-in default (recognised via the JSFunction's backing method).
//     Covers test/staging/sm/Array/from_string.
//   * Problem 3 — a `finally` that completes abruptly (continue/break/return)
//     must override a pending throw ("ex1"). endfinally re-raised the in-flight
//     exception so the deferred jump never ran; the IL generator now wraps a
//     branching finally in an outer try/catch guard (FinallyBranchScanner /
//     ILTryBlock). Covers test/language/statements/try/S12.14_A9..A12_T2.
//   * Problem 8 — Set.prototype.add returned the value instead of the Set, so
//     chained calls (`set.add(1).add(2)`) threw "Method add not found in 1".
//     The spec returns the Set (and WeakSet.prototype.add the WeakSet).
//   * Problem 9 — Intl.PluralRules.prototype.select was missing entirely
//     ("Method select not found in [object Intl.PluralRules]").
//   * Problem 10 — Promise.withResolvers was missing entirely ("Method
//     withResolvers not found in function Promise() { [native code] }").
public class Issue675Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problems 6 & 7: Unicode 15.1+ ID_Start coverage (via .NET 10) ----

    // .NET 8's CharUnicodeInfo tables stop at Unicode 15.0, so identifier-start
    // code points added in Unicode 15.1 (CJK Unified Ideographs Extension I) and
    // 16.0 (Tulu-Tigalari, Gurung Khema, ...) were classified as "Not Assigned"
    // and rejected by the parser. .NET 10 ships Unicode 16 tables, so these now
    // pass without the project needing its own static ID_Start fallback table.

    [Fact]
    public void Unicode15_1_CJKExtensionI_IsIdentifierStart()
        => Assert.Equal("1", Eval("var " + char.ConvertFromUtf32(0x2EBF0) + " = 1; " + char.ConvertFromUtf32(0x2EBF0)));

    [Fact]
    public void Unicode16_TuluTigalari_IsIdentifierStart()
        => Assert.Equal("1", Eval("var " + char.ConvertFromUtf32(0x11380) + " = 1; " + char.ConvertFromUtf32(0x11380)));

    [Fact]
    public void Unicode16_GurungKhema_IsIdentifierStart()
        => Assert.Equal("1", Eval("var " + char.ConvertFromUtf32(0x16100) + " = 1; " + char.ConvertFromUtf32(0x16100)));

    // The class-form failure surfaces as "Unexpected token Hash" because the
    // scanner leaves the `#<char>` private name unconsumed when <char> is
    // unrecognised as identifier-start. With .NET 10's tables the private
    // declaration scans cleanly.
    [Fact]
    public void Unicode15_1_AsPrivateClassName()
        => Assert.Equal("1", Eval(@"
            class C { #" + char.ConvertFromUtf32(0x2EBF0) + @" = 1; get() { return this.#" + char.ConvertFromUtf32(0x2EBF0) + @"; } }
            new C().get();
        "));

    // ---- Problem 1: string iteration honours String.prototype[@@iterator] ----

    // Default String iterator: still walks Unicode code points (a high/low
    // surrogate pair counts as one element).
    [Fact]
    public void StringIteration_DefaultWalksByCodePoint()
        => Assert.Equal("3", Eval(@"String(Array.from('a😀b').length);"));

    [Fact]
    public void StringIteration_DefaultArrayFromYieldsCodeUnits()
        => Assert.Equal("a,b,c", Eval(@"Array.from('abc').join(',');"));

    // Replacing String.prototype[Symbol.iterator] must affect every consumer of
    // the iteration protocol over a string primitive.
    [Fact]
    public void StringIteration_ArrayFromHonoursPrototypeOverride()
        => Assert.Equal("X,Y", Eval(@"
            String.prototype[Symbol.iterator] = function () {
                var i = 0, a = ['X', 'Y'];
                return { next: function () { return i < a.length ? { value: a[i++], done: false } : { value: undefined, done: true }; } };
            };
            Array.from('ab').join(',');
        "));

    [Fact]
    public void StringIteration_SpreadHonoursPrototypeOverride()
        => Assert.Equal("X,Y", Eval(@"
            String.prototype[Symbol.iterator] = function () {
                var i = 0, a = ['X', 'Y'];
                return { next: function () { return i < a.length ? { value: a[i++], done: false } : { value: undefined, done: true }; } };
            };
            [...'ab'].join(',');
        "));

    [Fact]
    public void StringIteration_ForOfHonoursPrototypeOverride()
        => Assert.Equal("X,Y", Eval(@"
            String.prototype[Symbol.iterator] = function () {
                var i = 0, a = ['X', 'Y'];
                return { next: function () { return i < a.length ? { value: a[i++], done: false } : { value: undefined, done: true }; } };
            };
            var r = [];
            for (var ch of 'ab') r.push(ch);
            r.join(',');
        "));

    [Fact]
    public void StringIteration_DestructuringHonoursPrototypeOverride()
        => Assert.Equal("X", Eval(@"
            String.prototype[Symbol.iterator] = function () {
                var i = 0, a = ['X', 'Y'];
                return { next: function () { return i < a.length ? { value: a[i++], done: false } : { value: undefined, done: true }; } };
            };
            var [first] = 'ab';
            first;
        "));

    // ---- Problem 3: finally abrupt completion overrides a pending throw ----

    // S12.14_A9_T2 CHECK#6 shape: `continue` in finally discards the throw and
    // the loop continues to completion.
    [Fact]
    public void FinallyContinue_OverridesPendingThrow()
        => Assert.Equal("{\"c\":2,\"log\":[\"fin\",\"fin\"]}", Eval(@"
            var log=[]; var c=0;
            do { try { c+=1; throw 'ex1'; } finally { log.push('fin'); continue; } } while (c<2);
            JSON.stringify({c:c, log:log});
        "));

    [Fact]
    public void FinallyReturn_OverridesPendingThrow()
        => Assert.Equal("R", Eval(@"
            function f(){ try { throw 'ex'; } finally { return 'R'; } }
            f();
        "));

    [Fact]
    public void FinallyBreak_OverridesPendingThrow()
        => Assert.Equal("after", Eval(@"
            var s='before';
            do { try { throw 'e'; } finally { break; } } while (true);
            s='after'; s;
        "));

    // The guard is gated on the finally actually branching out: a finally that
    // does not branch must let the pending throw propagate unchanged.
    [Fact]
    public void NonBranchingFinally_DoesNotSwallowThrow()
        => Assert.Equal("caught:keep", Eval(@"
            var r;
            try { try { throw 'keep'; } finally { var x=1; } } catch(e){ r='caught:'+e; }
            r;
        "));

    // Existing behaviour preserved: a finally return overrides a try return,
    // and an inner branching finally is itself overridden by an outer one.
    [Fact]
    public void FinallyReturn_OverridesTryReturn()
        => Assert.Equal("F", Eval(@"
            function f(){ try { return 'T'; } finally { return 'F'; } }
            f();
        "));

    [Fact]
    public void NestedBranchingFinally_OuterContinueWins()
        => Assert.Equal("done:2", Eval(@"
            var n=0;
            do { try { try { throw 'a'; } finally { throw 'b'; } } finally { n++; continue; } } while (n<2);
            'done:'+n;
        "));

    // A catch that re-throws followed by a branching finally: the finally's
    // continue overrides the re-thrown exception.
    [Fact]
    public void CatchRethrow_ThenFinallyContinue_Overrides()
        => Assert.Equal("ok:2", Eval(@"
            var k=0;
            do { try { throw 'x'; } catch(e){ throw e; } finally { k++; continue; } } while (k<2);
            'ok:'+k;
        "));

    // ---- Problem 8: Set.prototype.add returns the Set (chainable) ----

    [Fact]
    public void SetAdd_ReturnsTheSet()
        => Assert.Equal("true", Eval("var s = new Set(); String(s.add(9) === s);"));

    [Fact]
    public void SetAdd_IsChainable_PreservesInsertionOrder()
        => Assert.Equal("1,2,3", Eval(@"
            var s = new Set();
            s.add(1).add(2).add(3);
            [...s].join(',');
        "));

    [Fact]
    public void WeakSetAdd_ReturnsTheWeakSet()
        => Assert.Equal("true", Eval(@"
            var ws = new WeakSet();
            var a = {}, b = {};
            ws.add(a).add(b);
            String(ws.has(a) && ws.has(b));
        "));

    // ---- Problem 9: Intl.PluralRules.prototype.select ----

    [Fact]
    public void PluralRulesSelect_IsAFunctionOfLengthOne()
        => Assert.Equal("function,1", Eval(@"
            var pr = new Intl.PluralRules('en');
            (typeof pr.select) + ',' + pr.select.length;
        "));

    [Fact]
    public void PluralRulesSelect_CardinalEnglish()
        => Assert.Equal("one,other,other", Eval(@"
            var pr = new Intl.PluralRules('en');
            [pr.select(1), pr.select(0), pr.select(2)].join(',');
        "));

    [Fact]
    public void PluralRulesSelect_NonFiniteIsOther()
        => Assert.Equal("other,other,other", Eval(@"
            var pr = new Intl.PluralRules('en');
            [pr.select(Infinity), pr.select(-Infinity), pr.select(NaN)].join(',');
        "));

    [Fact]
    public void PluralRulesSelect_OrdinalEnglish()
        => Assert.Equal("one,two,few,other,other", Eval(@"
            var pr = new Intl.PluralRules('en', { type: 'ordinal' });
            [pr.select(1), pr.select(2), pr.select(3), pr.select(4), pr.select(11)].join(',');
        "));

    [Fact]
    public void PluralRulesSelect_ThrowsOnIncompatibleReceiver()
        => Assert.Equal("true", Eval(@"
            var select = Intl.PluralRules.prototype.select;
            var threw = false;
            try { select.call({}, 1); } catch (e) { threw = e instanceof TypeError; }
            String(threw);
        "));

    // ---- Problem 10: Promise.withResolvers ----

    [Fact]
    public void PromiseWithResolvers_ReturnsPromiseAndResolvers()
        => Assert.Equal("true,function,function", Eval(@"
            var d = Promise.withResolvers();
            [d.promise instanceof Promise, typeof d.resolve, typeof d.reject].join(',');
        "));

    [Fact]
    public void PromiseWithResolvers_HasLengthZero()
        => Assert.Equal("0", Eval("String(Promise.withResolvers.length);"));

    // Driven against a custom (synchronous) constructor so resolution is
    // observable without a microtask queue: withResolvers must hand back the
    // capability's own resolve/reject functions.
    [Fact]
    public void PromiseWithResolvers_WiresCapabilityResolveAndReject()
        => Assert.Equal("resolved-with-42", Eval(@"
            var captured;
            function C(executor){ executor(function(v){ captured = 'resolved-with-' + v; }, function(){}); }
            var d = Promise.withResolvers.call(C);
            d.resolve(42);
            captured;
        "));
}
