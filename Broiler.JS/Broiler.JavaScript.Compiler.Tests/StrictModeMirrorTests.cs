using System;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Item 4-5's first change: the engine's strict-mode flag is read on EVERY call, and it was an
// AsyncLocal read (7.0 ns) where a [ThreadStatic] read is 0.3 ns. The AsyncLocal stays as the
// mechanism that carries strictness across a suspension; a ThreadStatic mirror answers the reads,
// kept in step by the AsyncLocal's change handler.
//
// The mirror is only sound if the handler fires on BOTH the ways the value can move: a direct set
// (a strict/sloppy transition) and an execution-context switch (an async body resuming on another
// thread). The second is the one a ThreadStatic would get wrong on its own, and the reason
// JSEngine's comment says the flag "must remain an AsyncLocal" — so it stays one, and these tests
// are what say the mirror did not quietly undo that.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class StrictModeMirrorTests
{
    private static JSContext Context()
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .Build();

    private static string Run(string source)
    {
        using var context = Context();
        try
        {
            return context.Eval(source).ToString();
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }

    /// <summary>
    /// Runs a body that suspends, pumping the event loop so the continuations actually run, then
    /// reads back what it recorded. <c>Eval</c> alone returns before any microtask has been
    /// pumped, so the post-await half of every case below would silently be missing.
    /// </summary>
    private static string Drive(string body)
    {
        using var context = Context();
        context.Eval("globalThis.out = [];");
        context.Execute(body);
        return context.Eval("out.join('|')").ToString();
    }

    // The transition cases: strictness is a property of the code currently running, not of any
    // frame on the stack, so it has to be restored on the way back out in both directions.
    [Fact]
    public void ASloppyCalleeInvokedFromStrictCodeIsSloppy()
        => Assert.Equal("ok|ReferenceError", Run("""
            function sloppy() { undeclaredA = 1; return 'ok'; }
            function strict() { 'use strict'; try { undeclaredB = 1; return 'no throw'; } catch (e) { return e.name; } }
            [sloppy(), strict()].join('|');
            """));

    [Fact]
    public void AStrictCalleeInvokedFromSloppyCodeIsStrict()
        => Assert.Equal("ReferenceError|ok", Run("""
            function strict() { 'use strict'; try { undeclaredA = 1; return 'no throw'; } catch (e) { return e.name; } }
            function sloppy() { return strict() + '|' + (function () { undeclaredB = 1; return 'ok'; })(); }
            sloppy();
            """));

    // And back out again: after a strict callee returns, the sloppy caller must be sloppy once
    // more. A mirror that was written but never restored would leave the caller strict.
    [Fact]
    public void StrictnessIsRestoredWhenAStrictCalleeReturns()
        => Assert.Equal("ok", Run("""
            function strict() { 'use strict'; return 1; }
            function sloppy() { strict(); undeclaredAfter = 1; return 'ok'; }
            sloppy();
            """));

    [Fact]
    public void NestingStrictAndSloppyRestoresEachLevel()
        => Assert.Equal("sloppy,strict,sloppy,strict,sloppy", Run("""
            var out = [];
            function probe() { try { undeclared1 = 1; out.push('sloppy'); } catch (e) { out.push('strict'); } }
            function s3() { 'use strict'; probeStrict(); }
            function probeStrict() { 'use strict'; try { undeclared2 = 1; out.push('sloppy'); } catch (e) { out.push('strict'); } }
            function level1() { probe(); probeStrict(); probe(); s3(); probe(); }
            level1();
            out.join(',');
            """));

    // THE CASE THE MIRROR COULD BREAK. An async body suspends at `await` and resumes on whatever
    // thread the microtask queue pumps it from; ExecutionContext capture is what carries the
    // strictness across, and a bare ThreadStatic would not survive it. Asserted on both sides of
    // the await so a resumption that lost the flag is visible.
    [Fact]
    public void StrictnessSurvivesAnAwaitInAStrictAsyncFunction()
        => Assert.Equal("ReferenceError|ReferenceError", Drive("""
            async function strictAsync() {
              'use strict';
              try { beforeAwait = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
              await Promise.resolve(0);
              try { afterAwait = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
            }
            strictAsync();
            """));

    // The same for a sloppy async body, which must NOT come back strict — the mirror restoring a
    // stale `true` would show up here and nowhere else.
    [Fact]
    public void ASloppyAsyncFunctionStaysSloppyAcrossAnAwait()
        => Assert.Equal("ok|ok", Drive("""
            async function sloppyAsync() {
              try { beforeAwait2 = 1; out.push('ok'); } catch (e) { out.push(e.name); }
              await Promise.resolve(0);
              try { afterAwait2 = 1; out.push('ok'); } catch (e) { out.push(e.name); }
            }
            sloppyAsync();
            """));

    // A strict async function awaiting inside a sloppy caller's continuation: the resumption must
    // restore the awaiting function's strictness, not the resuming context's.
    [Fact]
    public void AStrictAndASloppyAsyncBodyInterleaveWithoutLeaking()
        => Assert.Equal("strict|sloppy|strict|sloppy", Drive("""
            async function s() {
              'use strict';
              try { leakA = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
              await Promise.resolve(0);
              try { leakB = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
            }
            async function n() {
              try { leakC = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
              await Promise.resolve(0);
              try { leakD = 1; out.push('sloppy'); } catch (e) { out.push('strict'); }
            }
            s(); n();
            """));

    // A generator suspends and resumes too, without an ExecutionContext capture — it resumes on
    // the thread that calls next(). Kept because it is the other suspendable shape and the mirror
    // has to be right for it as well.
    [Fact]
    public void StrictnessSurvivesAGeneratorSuspension()
        => Assert.Equal("ReferenceError|ReferenceError", Drive("""
            function* strictGen() {
              'use strict';
              try { genA = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
              yield 1;
              try { genB = 1; out.push('no throw'); } catch (e) { out.push(e.name); }
            }
            var it = strictGen();
            it.next(); it.next();
            """));

    // Strict `this` is a second observable of the same flag, reached by a different route.
    [Fact]
    public void StrictThisBindingIsUnaffected()
        => Assert.Equal("undefined|object", Run("""
            function strict() { 'use strict'; return typeof this; }
            function sloppy() { return typeof this; }
            [strict(), sloppy()].join('|');
            """));
}
