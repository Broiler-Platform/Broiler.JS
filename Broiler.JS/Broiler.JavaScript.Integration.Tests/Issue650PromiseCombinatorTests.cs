using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650 — Problem 5.
//
// Promise.all/any/allSettled must follow PerformPromiseAll-style remainingElementsCount
// semantics: the count starts at 1 and is decremented once per settled element plus once
// after iteration, so synchronous thenables that settle during the loop resolve/reject the
// capability exactly once (and synchronously). Resolve/reject elements have an
// [[AlreadyCalled]] guard, and Promise.any rejects with an AggregateError.
//
// Mirrors test/built-ins/Promise/{all,any,allSettled}/{resolve-before-loop-exit,
// call-resolve-element-after-return, call-reject-element-after-return}.js
public class Issue650PromiseCombinatorTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Three synchronous thenables settle during the loop; the capability resolve runs
    // exactly once, with all three values present.
    [Fact]
    public void AllResolvesOnceWithSynchronousThenables()
        => Assert.Equal("1|3|p1-fulfill,p2-fulfill,p3-fulfill", Eval(@"
var callCount=0, vals;
function C(ex){ ex(function(v){ callCount++; vals=v; }, function(){}); }
C.resolve=function(v){return v;};
var p1of;
var p1={then:function(f){p1of=f;}};
var p2={then:function(f){p1of('p1-fulfill'); f('p2-fulfill');}};
var p3={then:function(f){f('p3-fulfill');}};
Promise.all.call(C,[p1,p2,p3]);
callCount + '|' + vals.length + '|' + vals.join(',')"));

    // A resolve element re-invoked after Promise.all returned is a no-op ([[AlreadyCalled]]).
    [Fact]
    public void AllResolveElementIsAlreadyCalledGuarded()
        => Assert.Equal("1|expectedValue|1|expectedValue", Eval(@"
var callCount=0, vals;
function C(ex){ ex(function(v){ callCount++; vals=v; }, function(){}); }
C.resolve=function(v){return v;};
var p1of;
var p1={then:function(f){p1of=f; f('expectedValue');}};
Promise.all.call(C,[p1]);
var before = callCount + '|' + vals[0];
p1of('unexpectedValue');
before + '|' + callCount + '|' + vals[0]"));

    // Promise.any rejects with an AggregateError carrying every rejection reason.
    [Fact]
    public void AnyRejectsWithAggregateError()
        => Assert.Equal("1|true|p1-rejection,p2-rejection,p3-rejection", Eval(@"
var callCount=0, errs, isAgg;
function C(ex){ ex(function(){ throw 'noresolve'; }, function(e){ callCount++; errs=e.errors; isAgg=(e instanceof AggregateError); }); }
C.resolve=function(v){return v;};
var p1oj;
var p1={then:function(r,j){p1oj=j;}};
var p2={then:function(r,j){p1oj('p1-rejection'); j('p2-rejection');}};
var p3={then:function(r,j){j('p3-rejection');}};
Promise.any.call(C,[p1,p2,p3]);
callCount + '|' + isAgg + '|' + errs.join(',')"));

    // A reject element re-invoked after Promise.any returned is a no-op ([[AlreadyCalled]]).
    [Fact]
    public void AnyRejectElementIsAlreadyCalledGuarded()
        => Assert.Equal("1|onRejectedValue|onRejectedValue", Eval(@"
var callCount=0, errs;
function C(ex){ ex(function(){ throw 'noresolve'; }, function(e){ callCount++; errs=e.errors; }); }
C.resolve=function(v){return v;};
var p1oj;
var p1={then:function(r,j){p1oj=j; j('onRejectedValue');}};
Promise.any.call(C,[p1]);
var before = callCount + '|' + errs[0];
p1oj('unexpected');
before + '|' + errs[0]"));

    // Promise.allSettled produces the per-element status records synchronously.
    [Fact]
    public void AllSettledProducesStatusRecords()
        => Assert.Equal("1|2|fulfilled:A|rejected:B", Eval(@"
var callCount=0, arr;
function C(ex){ ex(function(v){ callCount++; arr=v; }, function(){}); }
C.resolve=function(v){return v;};
var p1={then:function(f){f('A');}};
var p2={then:function(f,j){j('B');}};
Promise.allSettled.call(C,[p1,p2]);
callCount + '|' + arr.length + '|' + arr[0].status + ':' + arr[0].value + '|' + arr[1].status + ':' + arr[1].reason"));

    // Native Promise.all still resolves (asynchronously) with the right values.
    [Fact]
    public async System.Threading.Tasks.Task NativePromiseAllStillResolves()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("Promise.all([Promise.resolve(1), Promise.resolve(2), 3]).then(function(v){ return v.join(','); })");
        var promise = Assert.IsType<JSPromise>(result);
        var settled = await promise.Task;
        Assert.Equal("1,2,3", settled.ToString());
    }

    // Native Promise.any still resolves with the first fulfillment.
    [Fact]
    public async System.Threading.Tasks.Task NativePromiseAnyStillResolves()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("Promise.any([Promise.reject(1), Promise.resolve(2)]).then(function(v){ return 'ok:' + v; })");
        var promise = Assert.IsType<JSPromise>(result);
        var settled = await promise.Task;
        Assert.Equal("ok:2", settled.ToString());
    }

    // Issue #650 Problem 8: when the capability's resolve throws an abrupt
    // completion for an already-exhausted iterator, the iterator is not closed
    // (`return` is never called) — mirrors
    // test/built-ins/Promise/{all,allSettled}/capability-resolve-throws-no-close.js
    private const string ThrowingResolveCapability = @"
var nextCount=0, returnCount=0;
var iter={};
iter[Symbol.iterator]=function(){ return {
  next:function(){ nextCount++; return {done:true}; },
  'return':function(){ returnCount++; return {}; }
}; };
var P=function(ex){ return new Promise(function(_,reject){ ex(function(){ throw new Error('x'); }, reject); }); };
P.resolve=Promise.resolve;";

    [Fact]
    public void AllCapabilityResolveThrowDoesNotCloseExhaustedIterator()
        => Assert.Equal("1|0", Eval(
            ThrowingResolveCapability + "try { Promise.all.call(P, iter); } catch (e) {} nextCount + '|' + returnCount"));

    [Fact]
    public void AllSettledCapabilityResolveThrowDoesNotCloseExhaustedIterator()
        => Assert.Equal("1|0", Eval(
            ThrowingResolveCapability + "try { Promise.allSettled.call(P, iter); } catch (e) {} nextCount + '|' + returnCount"));
}
