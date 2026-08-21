using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// RegExp.prototype[@@replace] accumulated its output into a StringBuilder even when there was
// exactly one match. .NET's StringBuilder is a chunk list, so that costs two full UTF-16 copies
// of the subject — the chunks, and then ToString() flattening them — which the --regex-profile
// scaling rows measured as ~4 bytes per subject character.
//
// A single match needs no builder at all: the answer is exactly prefix + replacement + suffix,
// and string.Concat writes those three spans into ONE allocation of the final length. That is
// the 4 B/char -> 2 B/char halving these tests pin.
//
// The allocation is asserted in bytes rather than as a ratio because nothing about the ANSWERS
// changes — so the second test class below pins the answers across the edges the fast path now
// owns, and the allocation bound cannot be satisfied by returning something cheaper and wrong.
// See docs/performance-roadmap.md phase 5.
public class SingleMatchReplaceAllocationTests
{
    private const int Calls = 200;

    // The needle sits in the MIDDLE, so both slices the fast path takes are non-empty and a
    // regression that drops either one fails the answers test rather than passing quietly.
    private static readonly string Source = $$"""
        (function () {
            var subject = 'a'.repeat(10000) + 'zqx' + 'a'.repeat(10000);
            var re = /zqx/;
            var sink = 0;
            for (var i = 0; i < {{Calls}}; i++) { sink = subject.replace(re, 'y').length; }
            return sink;
        })()
        """;

    [Fact(Timeout = 600000)]
    public void ASingleMatchReplaceDoesNotBuildTheSubjectTwice()
    {
        using var context = new JSContext();

        // Warmed first: the compile, the pattern's construction and the inline-cache entries are
        // one-time and would otherwise land in the delta.
        Assert.Equal(20_001, (int)context.Eval(Source).DoubleValue);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(20_001, (int)context.Eval(Source).DoubleValue);
        var bytes = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)Calls;

        // A 20 003-character subject. Two copies measured ~82 900 B/call, one copy measures
        // ~42 800 — the ~2 400 B of fixed per-call cost (the exec result array and its
        // index / input / groups) is in both. The bound sits between them with roughly 40% of
        // headroom each way, so it fails clearly before the change and is not tight after it.
        const double Bound = 60_000;
        Assert.True(
            bytes < Bound,
            $"expected under {Bound:N0} B per call for a single-match replace on a 20 003-character "
                + $"subject, got {bytes:N0} B; building it through a StringBuilder copies the "
                + "subject twice and costs ~82 900 B");
    }
}

// The answers, across the edges the single-match fast path now owns. Every case here has exactly
// one match, so every one of them takes it — including the global regexp, whose result list is
// length 1 and which therefore no longer reaches the accumulating loop.
public class SingleMatchReplaceAnswerTests
{
    [Theory]
    // Position 0 and end-of-subject, where one of the two slices is empty.
    [InlineData("'abc'.replace(/a/, 'X')", "Xbc")]
    [InlineData("'abc'.replace(/c/, 'X')", "abX")]
    [InlineData("'abc'.replace(/abc/, 'X')", "X")]
    [InlineData("'abc'.replace(/b/, 'X')", "aXc")]
    // An empty match consumes nothing, so the suffix must start where the prefix ended.
    [InlineData("'abc'.replace(/(?:)/, 'X')", "Xabc")]
    [InlineData("'abc'.replace(/(?=b)/, 'X')", "aXbc")]
    [InlineData("''.replace(/(?:)/, 'X')", "X")]
    // An empty replacement is a pure deletion.
    [InlineData("'abc'.replace(/b/, '')", "ac")]
    // A GLOBAL regexp that matches exactly once takes the fast path too.
    [InlineData("'abc'.replace(/b/g, 'X')", "aXc")]
    [InlineData("'abc'.replace(/z/g, 'X')", "abc")]
    // No match at all returns the subject unchanged, before the fast path is reached.
    [InlineData("'abc'.replace(/z/, 'X')", "abc")]
    // The $ substitutions, which are computed before the slicing and must be unaffected by it.
    [InlineData("'abc'.replace(/b/, '[$&]')", "a[b]c")]
    [InlineData("'abc'.replace(/b/, '[$`]')", "a[a]c")]
    [InlineData("'abc'.replace(/b/, \"[$']\")", "a[c]c")]
    [InlineData("'abc'.replace(/b/, '[$$]')", "a[$]c")]
    [InlineData("'abc'.replace(/(b)/, '[$1]')", "a[b]c")]
    [InlineData("'abc'.replace(/(?<mid>b)/, '[$<mid>]')", "a[b]c")]
    // A functional replacer receives match, captures, position and the whole subject.
    [InlineData("'abc'.replace(/b/, function (m, p, s) { return m + p + s; })", "ab1abcc")]
    [InlineData("'abc'.replace(/(b)/, function (m, c, p, s) { return c + p; })", "ab1c")]
    // Surrogate pairs: the slices are UTF-16 index based, so a match between them must not
    // split one. 😀 is U+1F600.
    [InlineData("'x\\uD83D\\uDE00y'.replace(/y/, 'Z')", "x😀Z")]
    [InlineData("'x\\uD83D\\uDE00y'.replace(/x/, 'Z')", "Z😀y")]
    [InlineData("'\\uD83D\\uDE00'.replace(/\\uD83D\\uDE00/u, 'Z')", "Z")]
    public void TheAnswerIsUnchanged(string source, string expected)
    {
        using var context = new JSContext();
        Assert.Equal(expected, context.Eval(source).ToString());
    }

    [Theory]
    // String.prototype.replace with a STRING searchValue is a different builtin — it never
    // reaches @@replace — and it replaces only the first match, so it is single-match by
    // construction. It had the same three-append assembly through a pre-sized StringBuilder.
    [InlineData("'abc'.replace('a', 'X')", "Xbc")]
    [InlineData("'abc'.replace('c', 'X')", "abX")]
    [InlineData("'abc'.replace('abc', 'X')", "X")]
    [InlineData("'abc'.replace('b', 'X')", "aXc")]
    [InlineData("'abc'.replace('b', '')", "ac")]
    [InlineData("'abc'.replace('', 'X')", "Xabc")]
    [InlineData("'abc'.replace('z', 'X')", "abc")]
    // Only the FIRST occurrence goes, which is what makes the path single-match.
    [InlineData("'abab'.replace('a', 'X')", "Xbab")]
    // The $ substitutions expand against the string match too.
    [InlineData("'abc'.replace('b', '[$&]')", "a[b]c")]
    [InlineData("'abc'.replace('b', '[$`]')", "a[a]c")]
    [InlineData("'abc'.replace('b', \"[$']\")", "a[c]c")]
    [InlineData("'abc'.replace('b', '[$$]')", "a[$]c")]
    // A functional replacer receives (matched, position, subject).
    [InlineData("'abc'.replace('b', function (m, p, s) { return m + p + s; })", "ab1abcc")]
    // Surrogate pairs must not be split by the slices.
    [InlineData("'x\\uD83D\\uDE00y'.replace('y', 'Z')", "x😀Z")]
    [InlineData("'\\uD83D\\uDE00'.replace('\\uD83D\\uDE00', 'Z')", "Z")]
    public void TheStringSearchValueAnswerIsUnchanged(string source, string expected)
    {
        using var context = new JSContext();
        Assert.Equal(expected, context.Eval(source).ToString());
    }

    [Fact(Timeout = 600000)]
    public void AnIllBehavingExecReportingAnIndexPastTheSubjectIsStillClamped()
    {
        // §22.2.6.11 step 16.f-g clamps position to [0, lengthS]. The fast path slices the
        // subject with it, so an unclamped value would throw ArgumentOutOfRangeException out of
        // string.Concat rather than produce the spec's answer.
        using var context = new JSContext();
        var answer = context.Eval("""
            var re = /b/;
            re.exec = function () { return { length: 1, 0: 'b', index: 9999 }; };
            'abc'.replace(re, 'X')
            """).ToString();

        Assert.Equal("abcX", answer);
    }

    [Fact(Timeout = 600000)]
    public void ASingleMatchStillRecordsTheLegacyStatics()
    {
        // The fast path returns before the accumulating loop, so it has to leave everything that
        // is not the output string exactly as it was — the Annex B statics among them.
        using var context = new JSContext();
        var answer = context.Eval("""
            'aXb'.replace(/X/, '-');
            [RegExp.lastMatch, RegExp.leftContext, RegExp.rightContext, RegExp.input].join('|')
            """).ToString();

        Assert.Equal("X|a|b|aXb", answer);
    }

    [Fact(Timeout = 600000)]
    public void ThePropertyReadOrderOnTheResultIsUnchanged()
    {
        // §22.2.6.11 step 16 reads length -> "0" -> index -> captures -> groups, and the order is
        // observable through a Proxy result (test262 sm/RegExp/replace-trace). Extracting the
        // per-result work into a local function so the fast path and the loop share it is what
        // keeps this to one copy; this pins that the order survived the extraction.
        using var context = new JSContext();
        var answer = context.Eval("""
            var seen = [];
            var target = { length: 2, 0: 'b', 1: 'B', index: 1, groups: undefined };
            var proxy = new Proxy(target, {
                get: function (t, k) { seen.push(String(k)); return t[k]; }
            });
            var re = /b/;
            re.exec = function () { return proxy; };
            'abc'.replace(re, '[$1]');
            seen.join(',')
            """).ToString();

        Assert.Equal("length,0,index,1,groups", answer);
    }
}
