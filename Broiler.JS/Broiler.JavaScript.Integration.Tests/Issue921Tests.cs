using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// #921 P8 — generator try/finally completion records. A `return` inside a generator
// `try` must run the enclosing `finally` block(s), and a `finally` may override the
// pending completion by completing abruptly itself (its own return/break/continue/throw)
// or by yielding. Previously the value was dropped (finally `return` ignored), yields in
// a finally reached via `return` did not suspend, and nested break/continue/catch in an
// outer finally failed to swallow an inner return.
// (test262 staging/sm/generators/return-finally.js)
public class Issue921Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    [Fact]
    public void FinallyReturnOverridesTryReturn()
    {
        Assert.Equal("42", Eval(@"(function*(){ try { return 42; } finally {} })().next().value").ToString());
        Assert.Equal("43", Eval(@"(function*(){ try { return 42; } finally { return 43; } })().next().value").ToString());
    }

    [Fact]
    public void FinallyThrowOverridesTryReturn()
    {
        var threw = Eval(@"
            var g = (function*(){ try { return 42; } finally { throw 43; } })();
            var caught;
            try { g.next(); } catch (e) { caught = e; }
            caught");
        Assert.Equal("43", threw.ToString());
    }

    [Fact]
    public void FinallyBreakContinueCancelTryReturn()
    {
        // F.[[type]] is break / continue / labelled break — the finally's abrupt
        // completion cancels the try's return, so execution falls through to `return 43`.
        Assert.Equal("43", Eval(@"(function*(){ do try { return 42; } finally { break; } while(false); return 43; })().next().value").ToString());
        Assert.Equal("43", Eval(@"(function*(){ L: try { return 42; } finally { break L; } return 43; })().next().value").ToString());
        Assert.Equal("43", Eval(@"(function*(){ do try { return 42; } finally { continue; } while(false); return 43; })().next().value").ToString());
    }

    [Fact]
    public void NestedAbruptCompletionInOuterFinallySwallowsInnerReturn()
    {
        // The outer try returns 42; the outer finally contains a nested try whose own
        // return is swallowed by an inner break/continue/labelled-break/caught-throw, so
        // the outer return 42 survives.
        Assert.Equal("42", Eval(@"(function*(){ try { return 42; } finally { do try { return 43; } finally { break; } while(0); } })().next().value").ToString());
        Assert.Equal("42", Eval(@"(function*(){ try { return 42; } finally { L: try { return 43; } finally { break L; } } })().next().value").ToString());
        Assert.Equal("42", Eval(@"(function*(){ try { return 42; } finally { do try { return 43; } finally { continue; } while(0); } })().next().value").ToString());
        Assert.Equal("42", Eval(@"(function*(){ try { return 42; } finally { try { try { return 43; } finally { throw 9; } } catch(e){} } })().next().value").ToString());
    }

    [Fact]
    public void DeeplyNestedFinallyReturnOverride()
    {
        // Innermost break swallows return 43; inner finally completes normally → return 42
        // overrides return 41.
        Assert.Equal("42", Eval(@"(function*(){ try { return 41; } finally { try { return 42; } finally { do try { return 43; } finally { break; } while(0); } } })().next().value").ToString());
    }

    [Fact]
    public void YieldInFinallyReachedByReturnSuspendsThenCompletes()
    {
        Assert.Equal("43,false,42,true", Eval(@"
            var g = (function*(){ try { return 42; } finally { yield 43; } })();
            var a = g.next();
            var b = g.next();
            [a.value, a.done, b.value, b.done].join(',')").ToString());
    }

    [Fact]
    public void ThrowWhileParkedAtYieldInFinally()
    {
        Assert.Equal("44", Eval(@"
            var g = (function*(){ try { return 42; } finally { yield 43; } })();
            g.next();
            var caught;
            try { g.throw(44); } catch (e) { caught = e; }
            caught").ToString());
    }

    [Fact]
    public void ReturnWhileParkedAtYieldInFinally()
    {
        // Generator.prototype.return overrides; and a nested continue still completes it.
        Assert.Equal("44,true", Eval(@"
            var g = (function*(){ try { return 42; } finally { yield 43; } })();
            g.next();
            var r = g.return(44);
            [r.value, r.done].join(',')").ToString());
        Assert.Equal("44,true", Eval(@"
            var g = (function*(){ try { yield 42; } finally { do try { return 43; } finally { continue; } while(0); } })();
            g.next();
            var r = g.return(44);
            [r.value, r.done].join(',')").ToString());
    }
}
