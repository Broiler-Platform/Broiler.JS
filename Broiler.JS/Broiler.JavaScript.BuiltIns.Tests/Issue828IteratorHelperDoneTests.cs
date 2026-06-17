using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/828 Problem 4.
//
// A lazy %Iterator.prototype% helper (map/filter/take/drop/flatMap) latches its completed
// state: once the source is exhausted and the helper returns { done: true }, a later next()
// must return done immediately WITHOUT re-pulling the underlying iterator. The helper used to
// leave its done flag unset when the source enumerator reported exhaustion, so the next call
// pulled the source again (one extra `next` call observable through a Proxy — test262
// sm/Iterator/prototype/lazy-methods-proxy-accesses).
public class Issue828IteratorHelperDoneTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("map(x => x)")]
    [InlineData("filter(() => true)")]
    [InlineData("take(4)")]
    [InlineData("drop(0)")]
    [InlineData("flatMap(x => [x])")]
    public void HelperDoesNotPullSourceAfterDone(string method)
        => Assert.Equal("3", Eval($$"""
            var calls = 0;
            class T extends Iterator {
              constructor(){ super(); this.value = 0; }
              next(){ calls++; if (this.value < 2) return { done: false, value: this.value++ }; return { done: true, value: undefined }; }
            }
            var h = new T().{{method}};
            h.next(); h.next(); h.next();
            var doneResult = h.next();          // helper already exhausted; must not pull the source
            (doneResult.done === true) ? '' + calls : 'not-done';
        """));

    // After the helper has finished, repeated next() calls stay done and never touch the source.
    [Fact]
    public void RepeatedNextAfterDoneStaysDoneWithoutPulling()
        => Assert.Equal("true,true,2", Eval("""
            var calls = 0;
            class T extends Iterator {
              constructor(){ super(); this.value = 0; }
              next(){ calls++; if (this.value < 1) return { done: false, value: this.value++ }; return { done: true }; }
            }
            var h = new T().map(x => x);
            h.next();                            // value 0
            var d1 = h.next();                   // source done
            var d2 = h.next();                   // stays done, no extra pull
            (d1.done) + ',' + (d2.done) + ',' + calls;
        """));
}
