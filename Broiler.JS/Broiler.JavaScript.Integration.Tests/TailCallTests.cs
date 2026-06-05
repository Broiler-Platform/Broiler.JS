using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

/// <summary>
/// Proper-tail-call coverage for the script-host mode. Each case recurses far
/// deeper than the native call stack allows, so completion proves the call in
/// the given syntactic tail position is optimized rather than growing the stack.
/// </summary>
public class TailCallTests
{
    private static JSValue EvalWithScriptHost(string code)
    {
        var previous = Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST");
        Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", "1");
        try
        {
            using var ctx = new JSContext();
            return ctx.Eval(code);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", previous);
        }
    }

    private const string Prelude = "\"use strict\";\nvar N = 300000;\n";

    [Theory]
    [InlineData("function f(n){ if(n===0) return 'done'; return f(n-1); }")]                       // direct
    [InlineData("function f(n){ return n===0 ? 'done' : f(n-1); }")]                                // conditional (false branch)
    [InlineData("function f(n){ return n!==0 ? f(n-1) : 'done'; }")]                                // conditional (true branch)
    [InlineData("function f(n){ if(n===0) return 'done'; return true && f(n-1); }")]                // logical AND
    [InlineData("function f(n){ if(n===0) return 'done'; return false || f(n-1); }")]               // logical OR
    [InlineData("function f(n){ if(n===0) return 'done'; return null ?? f(n-1); }")]                // coalesce
    [InlineData("function f(n){ if(n===0) return 'done'; if(true) return f(n-1); }")]               // if body
    [InlineData("function f(n){ if(n===0) return 'done'; for(var i=0;;) return f(n-1); }")]          // for body
    [InlineData("function f(n){ if(n===0) return 'done'; while(true) return f(n-1); }")]            // while body
    [InlineData("function f(n){ if(n===0) return 'done'; do { return f(n-1); } while(false); }")]   // do-while body
    [InlineData("function f(n){ if(n===0) return 'done'; switch(1){ case 1: return f(n-1); } }")]   // switch body
    [InlineData("function f(n){ if(n===0) return 'done'; try {} finally { return f(n-1); } }")]            // finally body
    [InlineData("function f(n){ if(n===0) return 'done'; try {} catch(e){} finally { return f(n-1); } }")] // finally body, catch present
    public void TailPosition_DoesNotGrowStack(string fn)
    {
        var result = EvalWithScriptHost(Prelude + fn + "\nf(N);");
        Assert.Equal("done", result.ToString());
    }

    [Fact]
    public void Catch_Body_TailCall_DoesNotGrowStack()
    {
        // A `return f(n-1)` in a catch (no finally) must be a proper tail call.
        // Kept separate from the theory above with a much smaller depth because each
        // iteration throws a real CLR exception to reach its catch (~0.4 ms each), so
        // 300000 would take minutes. Without the tail-call lift, catch-wrapped frames
        // overflow the native stack at ~150 deep, so 20000 proves the optimization.
        var result = EvalWithScriptHost("""
            "use strict";
            var N = 20000;
            function f(n){ if(n===0) return 'done'; try { throw 0; } catch(e){ return f(n-1); } }
            f(N);
            """);
        Assert.Equal("done", result.ToString());
    }

    [Fact]
    public void Conditional_In_Return_Preserves_Value()
    {
        var result = EvalWithScriptHost("""
            "use strict";
            function f(n){ return n === 0 ? 'zero' : 'pos'; }
            '' + f(0) + ',' + f(3);
            """);
        Assert.Equal("zero,pos", result.ToString());
    }

    [Fact]
    public void LogicalAnd_In_Return_Preserves_Value()
    {
        var result = EvalWithScriptHost("""
            "use strict";
            function g(){ return 5; }
            function f(a){ return a && g(); }
            '' + f(true) + ',' + f(false) + ',' + f(0);
            """);
        Assert.Equal("5,false,0", result.ToString());
    }

    [Fact]
    public void Coalesce_In_Return_Preserves_Value()
    {
        var result = EvalWithScriptHost("""
            "use strict";
            function g(){ return 9; }
            function f(a){ return a ?? g(); }
            '' + f(null) + ',' + f(2) + ',' + f(undefined);
            """);
        Assert.Equal("9,2,9", result.ToString());
    }

    [Fact]
    public void Real_TryFinally_Still_Blocks_Tail_Call_Ordering()
    {
        // A user try/finally must run its finally BEFORE the (non-tail) call result
        // is produced; tail-call optimization must not reorder this.
        var result = EvalWithScriptHost("""
            "use strict";
            var log = [];
            function g(){ log.push('call'); return 'g'; }
            function f(){ try { return g(); } finally { log.push('finally'); } }
            var r = f();
            r + ':' + log.join(',');
            """);
        Assert.Equal("g:call,finally", result.ToString());
    }

    [Fact]
    public void Catch_With_Finally_Does_Not_Reorder_Tail_Call()
    {
        // `return` inside a catch that has a finally is NOT a tail position: the
        // finally must still run after the catch's call result is produced.
        var result = EvalWithScriptHost("""
            "use strict";
            var log = [];
            function g(){ log.push('call'); return 'g'; }
            function f(){ try { throw 0; } catch(e){ return g(); } finally { log.push('finally'); } }
            var r = f();
            r + ':' + log.join(',');
            """);
        Assert.Equal("g:call,finally", result.ToString());
    }

    [Fact]
    public void Catch_Inside_Outer_Finally_Still_Runs_Finally()
    {
        // A catch tail call enabled only when there is no finally — an *enclosing*
        // try/finally must keep the inner catch's call non-tail so the outer
        // finally still runs.
        var result = EvalWithScriptHost("""
            "use strict";
            var log = [];
            function g(){ log.push('call'); return 'g'; }
            function f(){
              try {
                try { throw 0; } catch(e){ return g(); }
              } finally { log.push('finally'); }
            }
            var r = f();
            r + ':' + log.join(',');
            """);
        Assert.Equal("g:call,finally", result.ToString());
    }

    [Fact]
    public void Catch_Tail_Call_Preserves_Catch_Side_Effects_And_Value()
    {
        // The catch body runs (binding + side effects) and a tail call from it
        // returns the eventual value.
        var result = EvalWithScriptHost("""
            "use strict";
            var seen = [];
            function f(n){
              try { throw n; }
              catch(e){ seen.push(e); if(n > 0) return f(n-1); return 'done:' + seen.join(','); }
            }
            f(3);
            """);
        Assert.Equal("done:3,2,1,0", result.ToString());
    }

    [Fact]
    public void Finally_Return_Overrides_Try_Completion()
    {
        // A return in finally overrides the try's pending return; without one the
        // try value flows through. Guards the finally tail-call lift's correctness.
        var result = EvalWithScriptHost("""
            "use strict";
            function f(x){ try { return 'T'; } finally { if (x) return 'F'; } }
            '' + f(true) + ',' + f(false);
            """);
        Assert.Equal("F,T", result.ToString());
    }

    // test262 language/expressions/call/tco-non-eval-*: a call written `eval(...)`
    // whose binding is not %eval% is an ordinary call and must be a proper tail call.
    // Each reassigns `eval` to the recursing function itself; with PTC, callCount
    // reaches exactly 1 (only the n===0 base case) instead of overflowing the stack.

    // Note: these run in sloppy mode (no top-level "use strict"): the global case
    // reassigns `eval` and the with case uses `with`, both illegal under strict.

    [Fact]
    public void NonEval_Global_TailCall()
    {
        var result = EvalWithScriptHost("""
            var globalCount = 0;
            function f(n) {
              "use strict";
              if (n === 0) { globalCount += 1; return; }
              return eval(n - 1);
            }
            eval = f;
            f(300000);
            '' + globalCount;
            """);
        Assert.Equal("1", result.ToString());
    }

    [Fact]
    public void NonEval_With_Scope_TailCall()
    {
        var result = EvalWithScriptHost("""
            var globalCount = 0;
            var f, scope = {};
            with (scope) {
              f = function (n) {
                "use strict";
                if (n === 0) { globalCount += 1; return; }
                return eval(n - 1);
              }
            }
            scope.eval = f;
            f(300000);
            '' + globalCount;
            """);
        Assert.Equal("1", result.ToString());
    }

    [Fact]
    public void NonEval_Function_Dynamic_TailCall()
    {
        var result = EvalWithScriptHost("""
            var globalCount = 0;
            (function() {
              function f(n) {
                "use strict";
                if (n === 0) { globalCount += 1; return; }
                return eval(n - 1);
              }
              eval("var eval = f;");
              f(300000);
            })();
            '' + globalCount;
            """);
        Assert.Equal("1", result.ToString());
    }

    [Fact]
    public void NonTail_Indirect_Eval_Returns_Value_Not_Sentinel()
    {
        // The tail-call lift must not leak a JSTailCall sentinel into a non-tail use.
        var result = EvalWithScriptHost("""
            eval = function(n){ return n * 10; };
            function f(){ var x = eval(5); return x + 1; }
            '' + f();
            """);
        Assert.Equal("51", result.ToString());
    }

    [Fact]
    public void Direct_Eval_In_Tail_Position_Returns_Real_Value()
    {
        // A genuine direct eval in tail position evaluates its body to a real value.
        var result = EvalWithScriptHost("""
            function f(){ return eval('40 + 2'); }
            '' + f();
            """);
        Assert.Equal("42", result.ToString());
    }

    [Fact]
    public void Finally_Inside_Outer_Finally_Still_Runs_Outer()
    {
        // An inner finally tail call must not skip an enclosing finally.
        var result = EvalWithScriptHost("""
            "use strict";
            var log = [];
            function g(){ log.push('call'); return 'g'; }
            function f(){
              try {
                try {} finally { if (true) return g(); }
              } finally { log.push('outer'); }
            }
            var r = f();
            r + ':' + log.join(',');
            """);
        Assert.Equal("g:call,outer", result.ToString());
    }
}
