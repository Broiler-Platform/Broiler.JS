using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Reading a JSException's stack must not change it.
//
// JSException.JSStackTrace both renders the JavaScript frames and collects them: the walker appends
// each frame to the exception's own trace list as it prints it, which is what keeps them printable
// by Exception.StackTrace once the context that threw is gone. So it has to collect once and render
// thereafter — and it did not. Every read walked the live frames again and appended them again, so
// an exception whose stack a host renders for more than one sink reported the whole stack once per
// render, and each repeat carried the position the frame had reached by then rather than the
// position it threw at.
//
// It reached a report as a page's first script failing on its 14th line, over five identical
//
//     at native in inline-0:line 14
//
// lines — the shape of a five-deep recursion that never happened — above a JavaScript-side `stack`
// that correctly showed the one frame there was. That the two halves of one report disagreed is the
// tell: `stack` is a string captured once, while the CLR-side rendering re-read a list that had
// been growing on every read.
public class ExceptionStackTraceReadTests
{
    /// <summary>
    /// The reported shape: a property read on <c>undefined</c> inside a function, whose stack a host
    /// renders <paramref name="reads"/> further times while the frames are still live.
    /// </summary>
    private static string TraceAfterReads(int reads)
    {
        using var context = new JSContext();

        // Stands in for a host rendering the stack of an error it has been handed — a log sink, an
        // error event, a console. Called from the `catch` so the frames are still on the stack,
        // which is the only point at which the walker has anything to collect.
        context["renderStack"] = new JSFunction((in Arguments a) =>
        {
            if (a[0] is JSError error)
                _ = error.Exception.JSStackTrace;
            return JSUndefined.Value;
        }, "renderStack");

        var source = """
            var missing;
            function readSrc() {
                try { return missing.src; }
                catch (e) { RENDERS throw e; }
            }
            readSrc();
            """.Replace("RENDERS", string.Concat(Enumerable.Repeat("renderStack(e); ", reads)));

        try
        {
            context.Eval(source, "inline-0");
        }
        catch (JSException ex)
        {
            return ex.StackTrace ?? string.Empty;
        }

        throw new Xunit.Sdk.XunitException("the script was expected to throw");
    }

    /// <summary>
    /// The two JavaScript frames are reported twice — once each — however many times the stack was
    /// rendered. Four further renders used to report ten frames: the two real ones, then the same
    /// two again per render.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void RenderingTheStackDoesNotDuplicateItsFrames(int reads)
    {
        var trace = TraceAfterReads(reads);

        Assert.Equal(2, CountOf(trace, " in inline-0:line "));
        Assert.Equal(1, CountOf(trace, "at readSrc in inline-0:line 3"));
        Assert.Equal(1, CountOf(trace, "at native in inline-0:line 6"));
    }

    /// <summary>
    /// Every frame reports where it threw, not where it had unwound to by the time something asked.
    /// The repeats carried <c>readSrc</c> at line 4 — the <c>catch</c> — which is a position it
    /// reached only after the throw, and which named the handler as if it were a call site.
    /// </summary>
    [Fact]
    public void TheFramesReportTheThrowSiteAndNotTheHandler()
    {
        var trace = TraceAfterReads(reads: 4);

        Assert.Contains("at readSrc in inline-0:line 3", trace, StringComparison.Ordinal);
        Assert.DoesNotContain("at readSrc in inline-0:line 4", trace, StringComparison.Ordinal);
    }

    /// <summary>
    /// And the CLR-side rendering agrees with the JavaScript-side <c>stack</c> string appended below
    /// it, which is captured once at construction — the two halves of one report saying the same
    /// thing is what was lost.
    /// </summary>
    [Fact]
    public void TheTwoHalvesOfOneReportAgree()
    {
        var trace = TraceAfterReads(reads: 4);

        Assert.Equal(
            CountOf(trace, ":inline-0:"),          // the JavaScript-side `stack`, indented
            CountOf(trace, " in inline-0:line ")); // the frames rendered above it
    }

    private static int CountOf(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
