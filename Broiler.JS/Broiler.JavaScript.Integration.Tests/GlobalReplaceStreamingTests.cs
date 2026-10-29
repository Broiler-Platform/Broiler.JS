using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// §22.2.6.11 collects every match in step 14 and only reads their properties in step 16, so a
// global replace held one result array per match live before it built anything — measured at
// 2 033 bytes per match, dead linear in the match count, ~10.3 MB for a single 5 000-match call.
//
// RegExp.prototype[@@replace] now streams when nothing can observe those results: the receiver's
// "exec" is the pristine %RegExp.prototype.exec% captured at realm init, the replacement is a
// string, and that string contains no '$'. Under those conditions the result object is pure
// garbage and the loop appends as it goes.
//
// The guard is the whole risk here, so these tests are mostly about when the fast path must NOT
// be taken. See docs/performance-roadmap.md phase 5.
public class GlobalReplaceStreamingAllocationTests
{
    private const int Calls = 20;
    private const int Matches = 5_000;

    // One 'x' every 8 characters over a 40 000-character subject.
    private static readonly string Source = $$"""
        (function () {
            var subject = ('x' + 'aaaaaaa').repeat({{Matches}});
            var re = /x/g;
            var sink = 0;
            for (var i = 0; i < {{Calls}}; i++) { sink = subject.replace(re, 'y').length; }
            return sink;
        })()
        """;

    [Fact]
    public void AGlobalReplaceDoesNotRetainOneResultArrayPerMatch()
    {
        using var context = new JSContext();

        // Warmed first: the compile, the pattern's construction and the inline-cache entries are
        // one-time and would otherwise land in the delta.
        Assert.Equal(40_000, (int)context.Eval(Source).DoubleValue);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(40_000, (int)context.Eval(Source).DoubleValue);
        var bytesPerMatch = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)Calls / Matches;

        // Retaining a result array per match measured 2 033 B/match; streaming measures ~478.
        // The bound sits between them with roughly 2x of headroom each way, so it fails by a
        // factor before the change and is not tight after it.
        const double Bound = 1_000;
        Assert.True(
            bytesPerMatch < Bound,
            $"expected under {Bound:N0} B per match for a global replace with {Matches:N0} "
                + $"matches, got {bytesPerMatch:N0} B; retaining one result array per match "
                + "is ~2 033 B");
    }
}

// The guard. Every case here must produce the same answer the general path produces, and the
// ones named "…IsNotStreamed" additionally pin that the fast path declined — they would be
// silently wrong, not slow, if the guard let them through.
public class GlobalReplaceStreamingGuardTests
{
    [Theory]
    [InlineData("'aXbXc'.replace(/X/g, '-')", "a-b-c")]
    [InlineData("'abc'.replace(/z/g, '-')", "abc")]
    [InlineData("'aaa'.replace(/a/g, '')", "")]
    [InlineData("'abc'.replace(/b/g, 'XY')", "aXYc")]
    // Empty matches advance by one position and still emit the replacement at each.
    [InlineData("'abc'.replace(/(?:)/g, '-')", "-a-b-c-")]
    [InlineData("''.replace(/(?:)/g, '-')", "-")]
    [InlineData("'abc'.replace(/x*/g, '-')", "-a-b-c-")]
    // Sticky and global together.
    [InlineData("'aab'.replace(/a/gy, '-')", "--b")]
    // Non-global goes to the single-match path instead, and must be unaffected.
    [InlineData("'aXbXc'.replace(/X/, '-')", "a-bXc")]
    public void TheStreamedAnswerIsUnchanged(string source, string expected)
    {
        using var context = new JSContext();
        Assert.Equal(expected, context.Eval(source).ToString());
    }

    [Theory]
    // A '$' in the template reads back through the result object, so these must stay on the
    // general path. They are the cases the guard deliberately gives up.
    [InlineData("'aXbXc'.replace(/X/g, '[$&]')", "a[X]b[X]c")]
    [InlineData("'a1b2'.replace(/(\\d)/g, '<$1>')", "a<1>b<2>")]
    [InlineData("'abc'.replace(/b/g, \"[$`|$']\")", "a[a|c]c")]
    [InlineData("'abc'.replace(/b/g, '$$')", "a$c")]
    [InlineData("'a1'.replace(/(?<d>\\d)/g, '<$<d>>')", "a<1>")]
    public void ATemplateWithADollarIsNotStreamed(string source, string expected)
    {
        using var context = new JSContext();
        Assert.Equal(expected, context.Eval(source).ToString());
    }

    [Fact]
    public void AFunctionalReplacerIsNotStreamed()
    {
        // The spec collects ALL matches before calling the replacer for any of them, so by the
        // time user code runs the final failing exec has already reset lastIndex to 0. Streaming
        // would let the replacer observe lastIndex mid-subject, which is why a functional
        // replacement is excluded from the fast path — this is the test that would catch it.
        using var context = new JSContext();
        var answer = context.Eval("""
            var re = /X/g, seen = [];
            var out = 'aXbXc'.replace(re, function () { seen.push(re.lastIndex); return '-'; });
            out + '|' + seen.join(',')
            """).ToString();

        Assert.Equal("a-b-c|0,0", answer);
    }

    [Fact]
    public void AnOwnExecIsNotStreamed()
    {
        // An own "exec" is not the captured intrinsic, so the receiver's own function must run.
        using var context = new JSContext();
        var answer = context.Eval("""
            var calls = 0, re = /b/g, real = RegExp.prototype.exec;
            re.exec = function (s) { calls++; return real.call(this, s); };
            'abcb'.replace(re, '-') + '|' + (calls > 0)
            """).ToString();

        Assert.Equal("a-c-|true", answer);
    }

    [Fact]
    public void AnExecReturningNullIsNotStreamed()
    {
        using var context = new JSContext();
        var answer = context.Eval("""
            var re = /b/g;
            re.exec = function () { return null; };
            'abc'.replace(re, '-')
            """).ToString();

        Assert.Equal("abc", answer);
    }

    [Fact]
    public void APatchedPrototypeExecIsNotStreamed()
    {
        // The guard compares against the intrinsic captured at realm init, so replacing
        // RegExp.prototype.exec afterwards must take the receiver off the fast path.
        using var context = new JSContext();
        var answer = context.Eval("""
            var saved = RegExp.prototype.exec, hits = 0;
            RegExp.prototype.exec = function (s) { hits++; return saved.call(this, s); };
            var out = 'abcb'.replace(/b/g, '-');
            RegExp.prototype.exec = saved;
            out + '|' + (hits > 0)
            """).ToString();

        Assert.Equal("a-c-|true", answer);
    }

    [Theory]
    // §22.2.6.11 step 14.d advances an empty match by a code POINT under /u and by a code UNIT
    // without it, so the same subject splits the surrogate pair one way and not the other.
    // Asserted as code units because a C# literal for a LONE surrogate is its own trap.
    [InlineData("(?:)/gu", "2d,d83d,de00,2d")]
    [InlineData("(?:)/g", "2d,d83d,2d,de00,2d")]
    public void TheEmptyMatchAdvanceRespectsTheUnicodeFlag(string pattern, string expectedUnits)
    {
        using var context = new JSContext();
        var answer = context.Eval($$"""
            (function () {
                var r = '😀'.replace(/{{pattern}}, '-');
                var codes = [];
                for (var i = 0; i < r.length; i++) codes.push(r.charCodeAt(i).toString(16));
                return codes.join(',');
            })()
            """).ToString();

        Assert.Equal(expectedUnits, answer);
    }

    [Fact]
    public void LastIndexIsResetAfterAStreamedReplace()
    {
        using var context = new JSContext();
        var answer = context.Eval("""
            var re = /b/g;
            'abcb'.replace(re, '-');
            re.lastIndex
            """).DoubleValue;

        Assert.Equal(0d, answer);
    }

    [Fact]
    public void TheLegacyStaticsStillDescribeTheLastMatchAfterAStreamedReplace()
    {
        // The statics are updated per match inside ExecMatch, which the fast path still calls —
        // this pins that streaming did not skip them, and that they describe the LAST match.
        using var context = new JSContext();
        var answer = context.Eval("""
            'aXbXc'.replace(/X/g, '-');
            [RegExp.lastMatch, RegExp.leftContext, RegExp.rightContext].join('|')
            """).ToString();

        Assert.Equal("X|aXb|c", answer);
    }
}
