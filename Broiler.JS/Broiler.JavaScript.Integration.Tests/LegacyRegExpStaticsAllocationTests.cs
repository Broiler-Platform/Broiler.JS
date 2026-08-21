using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// The Annex B.2.4 legacy RegExp statics — RegExp.lastMatch, RegExp.leftContext,
// RegExp.rightContext — are recorded on every successful built-in exec. leftContext and
// rightContext partition the SUBJECT around the match, so computing them eagerly copies the
// whole subject once per match.
//
// RegExp.prototype[@@replace] calls exec once per match, so that made a global replace
// quadratic in allocation: measured at ~42 KB per match on a 40 KB subject, 210 MB for a single
// replace call and 4.21 GB over the profile's twenty. Recording the span and slicing on read
// takes it to ~10 MB per call, with the miss cases unchanged because a failed match never
// updates the statics at all.
//
// Asserted as bytes rather than as a ratio, because the failure is one of degree and nothing
// about the ANSWERS changes — Issue845RegExpAndWithTests covers those and would pass either
// way. See docs/performance-roadmap.md phase 5.
public class LegacyRegExpStaticsAllocationTests
{
    private const int SubjectLength = 20_000;

    /// <summary>Subject with a match every eight characters, so one replace does 5 000 of them.</summary>
    private const string Source = """
        (function () {
            var subject = 'abcdefgh'.repeat(2500);
            var re = /[aeiou]/g;
            return subject.replace(re, 'x').length;
        })()
        """;

    [Fact(Timeout = 600000)]
    public void AGlobalReplaceDoesNotCopyTheSubjectPerMatch()
    {
        using var context = new JSContext();

        // Warmed first: the compile, the pattern's own construction and the inline-cache
        // entries are one-time and would otherwise land in the delta.
        Assert.Equal(SubjectLength, (int)context.Eval(Source).DoubleValue);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(SubjectLength, (int)context.Eval(Source).DoubleValue);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        // 5 000 matches over a 40 KB subject. Copying the subject per match came to ~210 MB;
        // this build measures ~10 MB. The bound sits between them with about 4x of headroom
        // each way, so it fails by a factor before the change and is not tight after it.
        const long Bound = 50L * 1024 * 1024;
        Assert.True(
            bytes < Bound,
            $"expected under {Bound / (1024 * 1024)} MB for one global replace with 5 000 "
                + $"matches, got {bytes / (1024.0 * 1024.0):F1} MB "
                + $"({bytes / 5000.0:F0} B/match); copying the subject per match is ~42 KB/match");
    }

    [Fact(Timeout = 600000)]
    public void TheStaticsStillDescribeTheLastMatchAfterAGlobalReplace()
    {
        // The allocation test above passes trivially if the statics stop being recorded, so
        // this pins that they still are — and that they describe the LAST match, which is what
        // slicing on read has to preserve now that the span is what is stored.
        using var context = new JSContext();
        var answer = context.Eval("""
            'aXbXc'.replace(/X/g, '-');
            [RegExp.lastMatch, RegExp.leftContext, RegExp.rightContext, RegExp.input].join('|')
            """).ToString();

        Assert.Equal("X|aXb|c|aXbXc", answer);
    }
}
