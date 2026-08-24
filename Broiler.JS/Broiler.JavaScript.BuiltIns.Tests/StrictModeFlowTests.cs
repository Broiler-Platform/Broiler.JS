using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The runtime strict-mode flag decides whether a failing [[Set]] throws. It is scoped per
// invocation and only written on an actual strict/sloppy transition
// (docs/performance-roadmap.md P0-2), so these pin down that the observed value still
// tracks the code currently executing, across every strict/sloppy transition shape.
public class StrictModeFlowTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact(Timeout = 600000)]
    public void StrictCallee_FromSloppyCaller_ThrowsOnFailingSet()
    {
        var result = Eval("""
            var frozen = Object.freeze({ x: 1 });
            function strictCallee() { 'use strict'; frozen.x = 2; }
            function sloppyCaller() { try { strictCallee(); return 'no-throw'; } catch (e) { return e.constructor.name; } }
            sloppyCaller();
            """);

        Assert.Equal("TypeError", result);
    }

    [Fact(Timeout = 600000)]
    public void SloppyCallee_FromStrictCaller_DoesNotInheritStrictSetSemantics()
    {
        // A sloppy callee entered from strict code must drop back to non-strict, so its
        // failing [[Set]] is silently ignored rather than throwing.
        var result = Eval("""
            var frozen = Object.freeze({ x: 1 });
            function sloppyCallee() { frozen.x = 2; return 'no-throw'; }
            function strictCaller() {
                'use strict';
                try { return sloppyCallee(); } catch (e) { return e.constructor.name; }
            }
            strictCaller() + '|' + frozen.x;
            """);

        Assert.Equal("no-throw|1", result);
    }

    [Fact(Timeout = 600000)]
    public void StrictnessIsRestored_AfterANestedSloppyCall()
    {
        var result = Eval("""
            var frozen = Object.freeze({ x: 1 });
            function sloppyCallee() { frozen.x = 2; return 'inner-ok'; }
            function strictCaller() {
                'use strict';
                var inner = sloppyCallee();
                try { frozen.x = 3; return inner + '|no-throw'; } catch (e) { return inner + '|' + e.constructor.name; }
            }
            strictCaller();
            """);

        Assert.Equal("inner-ok|TypeError", result);
    }

    [Fact(Timeout = 600000)]
    public void StrictnessIsRestored_AfterANestedStrictCall()
    {
        var result = Eval("""
            var frozen = Object.freeze({ x: 1 });
            function strictCallee() { 'use strict'; return 'inner-ok'; }
            function sloppyCaller() {
                var inner = strictCallee();
                try { frozen.x = 3; return inner + '|no-throw'; } catch (e) { return inner + '|' + e.constructor.name; }
            }
            sloppyCaller();
            """);

        Assert.Equal("inner-ok|no-throw", result);
    }

    [Fact(Timeout = 600000)]
    public void DeepSameStrictnessNesting_KeepsTheFlag()
    {
        // No transition occurs anywhere in this chain, which is the case the scope
        // optimizes; the flag must still read as strict at the bottom.
        var result = Eval("""
            'use strict';
            var frozen = Object.freeze({ x: 1 });
            function a() { return b(); }
            function b() { return c(); }
            function c() { try { frozen.x = 2; return 'no-throw'; } catch (e) { return e.constructor.name; } }
            a();
            """);

        Assert.Equal("TypeError", result);
    }

    [Fact(Timeout = 600000)]
    public void AsyncAndGeneratorBodiesEnterRuntimeStrictMode()
    {
        // An async or generator body runs its code during the rewritten driver's steps, not
        // during the JSFunction.InvokeFunction call that created it, so it does not inherit
        // the EnterStrictMode scope that ordinary calls establish. ClrGeneratorV2.Next now
        // re-enters the function's own strict-mode flag around each body step, so a failing
        // [[Set]] inside a 'use strict' async or generator body throws — in the async
        // function's SYNCHRONOUS prefix, before any await, and in a generator before any
        // yield — exactly as an ordinary strict function does. (This was the KnownGap this
        // test pinned before the fix; the expectations are now "TypeError".)
        var async = Eval("""
            var log = [];
            var frozen = Object.freeze({ x: 1 });
            async function strictAsync() {
                'use strict';
                try { frozen.x = 2; log.push('no-throw'); } catch (e) { log.push(e.constructor.name); }
                await Promise.resolve();
            }
            strictAsync();
            log.join(',');
            """);

        var generator = Eval("""
            var log = [];
            var frozen = Object.freeze({ x: 1 });
            function* strictGen() {
                'use strict';
                try { frozen.x = 2; log.push('no-throw'); } catch (e) { log.push(e.constructor.name); }
                yield 1;
            }
            strictGen().next();
            log.join(',');
            """);

        // An ordinary strict function in the same position DOES throw, which is what makes
        // this a gap rather than a deliberate relaxation.
        var ordinary = Eval("""
            var frozen = Object.freeze({ x: 1 });
            function strictFn() {
                'use strict';
                try { frozen.x = 2; return 'no-throw'; } catch (e) { return e.constructor.name; }
            }
            strictFn();
            """);

        Assert.Equal("TypeError", async);
        Assert.Equal("TypeError", generator);
        Assert.Equal("TypeError", ordinary);
    }

    [Fact(Timeout = 600000)]
    public void ClassBodiesAreAlwaysStrict()
    {
        var result = Eval("""
            var frozen = Object.freeze({ x: 1 });
            class C { m() { try { frozen.x = 2; return 'no-throw'; } catch (e) { return e.constructor.name; } } }
            function sloppyCaller() { return new C().m(); }
            sloppyCaller();
            """);

        Assert.Equal("TypeError", result);
    }

    // NOTE: generator bodies do not enter the runtime strict-mode scope at all — a failing
    // [[Set]] inside a `'use strict'` generator does not throw, on the very first next()
    // before any yield. That predates the P0-2 change (which is strictness-neutral: it only
    // skips redundant AsyncLocal writes) and is recorded as a gap in
    // docs/performance-roadmap.md rather than asserted here.
    //
    // The same applies to an async continuation after `await`: the continuation does not run
    // under Eval or Execute in-process, so its strictness cannot be observed today.
}
