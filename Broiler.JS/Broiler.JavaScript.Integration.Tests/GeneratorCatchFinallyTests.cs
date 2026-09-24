using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// A completion raised inside a `catch` block (its own `return` or `throw`, or a return() /
// throw() request while the catch is suspended at a yield or await) still runs the same try
// statement's `finally`. In a generator or async body the try statement is lowered to a try
// region, and unwinding popped a region as soon as its catch had begun, skipping the finally:
// `try { await load() } catch { return fallback } finally { cleanup() }` never called cleanup,
// and a finally that overrides the completion (return or throw) never got to. Expected values
// match V8.
public class GeneratorCatchFinallyTests
{
    // Runs the script (Execute pumps its jobs) and returns the global `r`.
    private static string Run(string code)
    {
        using var ctx = new JSContext();
        ctx.Execute("var r;\n" + code);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Theory(Timeout = 600000)]
    // The catch block's own return or throw.
    [InlineData("var log = []; function* g(){ try { yield 1; throw 1; } catch (e) { log.push('c'); return 'r'; } finally { log.push('f'); } } var it = g(); it.next(); r = JSON.stringify([it.next(), log]);",
        "[{\"value\":\"r\",\"done\":true},[\"c\",\"f\"]]")]
    [InlineData("var log = []; function* g(){ try { yield 1; throw 'a'; } catch (e) { throw 'b'; } finally { log.push('f'); } } var it = g(); it.next(); try { it.next(); } catch (e) { log.push(e); } r = JSON.stringify(log);",
        "[\"f\",\"b\"]")]
    // A throw() request the catch handles by returning or rethrowing.
    [InlineData("var log = []; function* g(){ try { yield 1; } catch (e) { return 'c:' + e; } finally { log.push('f'); } } var it = g(); it.next(); r = JSON.stringify([it.throw('E'), log]);",
        "[{\"value\":\"c:E\",\"done\":true},[\"f\"]]")]
    [InlineData("var log = []; function* g(){ try { yield 1; } catch (e) { throw e + '!'; } finally { log.push('f'); } } var it = g(); it.next(); try { it.throw('E'); } catch (e) { log.push(e); } r = JSON.stringify(log);",
        "[\"f\",\"E!\"]")]
    // return() and throw() while suspended at a yield inside the catch.
    [InlineData("var log = []; function* g(){ try { throw 1; } catch (e) { yield 'c'; log.push('no'); } finally { log.push('f'); } } var it = g(); it.next(); r = JSON.stringify([it.return('R'), log]);",
        "[{\"value\":\"R\",\"done\":true},[\"f\"]]")]
    [InlineData("var log = []; function* g(){ try { throw 1; } catch (e) { yield 'c'; log.push('no'); } finally { log.push('f'); } } var it = g(); it.next(); try { it.throw('T'); } catch (e) { log.push(e); } r = JSON.stringify(log);",
        "[\"f\",\"T\"]")]
    // The finally runs as usual after the catch: it may yield, or override the completion.
    [InlineData("var log = []; function* g(){ try { throw 1; } catch (e) { return 'r'; } finally { log.push('f1'); yield 'fy'; log.push('f2'); } } var it = g(); r = JSON.stringify([it.next(), it.next(), log]);",
        "[{\"value\":\"fy\",\"done\":false},{\"value\":\"r\",\"done\":true},[\"f1\",\"f2\"]]")]
    [InlineData("function* g(){ try { yield 1; throw 1; } catch (e) { return 'r'; } finally { return 'f'; } } var it = g(); it.next(); r = JSON.stringify(it.next());",
        "{\"value\":\"f\",\"done\":true}")]
    [InlineData("function* g(){ try { yield 1; throw 1; } catch (e) { throw 'c'; } finally { return 'f'; } } var it = g(); it.next(); r = JSON.stringify(it.next());",
        "{\"value\":\"f\",\"done\":true}")]
    [InlineData("function* g(){ try { yield 1; throw 1; } catch (e) { return 'r'; } finally { throw 'f'; } } var it = g(); it.next(); try { it.next(); r = 'no'; } catch (e) { r = e; }",
        "f")]
    // Nested try statements unwind inner finally first, then the outer catch and finally.
    [InlineData("var log = []; function* g(){ try { try { yield 1; throw 'a'; } catch (e) { throw 'b'; } finally { log.push('if'); } } catch (e) { log.push('oc:' + e); } finally { log.push('of'); } return 'end'; } var it = g(); it.next(); r = JSON.stringify([it.next(), log]);",
        "[{\"value\":\"end\",\"done\":true},[\"if\",\"oc:b\",\"of\"]]")]
    // Async functions: a rejected await handled by a catch that returns or throws.
    [InlineData("var log = []; async function f(){ try { await Promise.reject(new Error('x')); } catch (e) { log.push('c'); return 'caught'; } finally { log.push('f'); } } f().then(v => r = JSON.stringify([v, log]));",
        "[\"caught\",[\"c\",\"f\"]]")]
    [InlineData("var log = []; async function f(){ try { await Promise.reject(new Error('x')); } catch (e) { throw new Error('y'); } finally { log.push('f'); } } f().then(v => r = 'resolved', e => r = e.message + JSON.stringify(log));",
        "y[\"f\"]")]
    [InlineData("var log = []; async function f(){ try { throw 1; } catch (e) { await null; return 'c'; } finally { await null; log.push('f'); } } f().then(v => r = v + JSON.stringify(log));",
        "c[\"f\"]")]
    [InlineData("var log = []; async function f(){ try { try { await Promise.reject('a'); } catch (e) { log.push('ic'); throw 'b'; } finally { log.push('if'); } } catch (e) { log.push('oc:' + e); return 'r'; } finally { log.push('of'); } } f().then(v => r = JSON.stringify([v, log]));",
        "[\"r\",[\"ic\",\"if\",\"oc:b\",\"of\"]]")]
    // Async generators.
    [InlineData("var log = []; async function* g(){ try { yield 1; } catch (e) { return 'c:' + e; } finally { log.push('f'); } } (async () => { var it = g(); await it.next(); r = JSON.stringify([await it.throw('E'), log]); })();",
        "[{\"value\":\"c:E\",\"done\":true},[\"f\"]]")]
    public void FinallyRunsAfterCatchCompletesAbruptly(string code, string expected)
        => Assert.Equal(expected, Run(code));
}
