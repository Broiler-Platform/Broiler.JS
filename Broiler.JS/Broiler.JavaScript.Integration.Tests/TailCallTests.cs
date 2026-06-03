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
    public void TailPosition_DoesNotGrowStack(string fn)
    {
        var result = EvalWithScriptHost(Prelude + fn + "\nf(N);");
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
}
