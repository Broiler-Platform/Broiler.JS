using System;
using System.Linq;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The library-level run's per-vector listing (SJL-15.2 stage 8; the session's decision D-js-vector-listing): part F of
/// bundle sjl-15-001's rule compares "per vector the adapter answers, its verdict", so this test writes, for every
/// <c>semantics</c> vector the adapter answers, its line of <see cref="VectorRun.Report"/> into the test's output, where
/// a run with a <c>trx</c> logger keeps it, between a begin line and an end line. Each line's verdict is the vector's at
/// the parse (the session's decision D-js-adapter-form, which the owner ruled on 2026-11-25 to keep): <c>match</c> where
/// the adapter's rendering agrees with the expected outcome read at the parse, <c>difference</c> otherwise; the rendered
/// and expected outcomes are the run's own. The test lists and decides nothing: the agreement itself is
/// <see cref="SemanticsAdapterTests.Every_answered_semantics_vector_agrees_at_the_parse"/>'s, so a run in which a vector
/// does not agree still writes its listing.
/// </summary>
public sealed class SemanticsVectorListingTests(ITestOutputHelper output)
{
    /// <summary>The line before the listing in the test's output.</summary>
    internal const string Begin = "# begin: the answered semantics vectors, each with its verdict at the parse";

    /// <summary>The line after it.</summary>
    internal const string End = "# end: the answered semantics vectors";

    [Fact]
    public void Each_answered_semantics_vector_s_verdict_at_the_parse_is_listed()
    {
        var answered = Home.Corpus.Records.Where(static v => v["area"] == "semantics").Where(AlgorithmsLibrary.IsLibrarySemantics).ToList();
        Assert.NotEmpty(answered);
        var results = LibraryRun.Run(answered, new AlgorithmsLibrary());
        var atTheParse = results.Select(static r => r with { Verdict = AlgorithmsLibrary.MatchesSemanticsAtTheParse(r) ? Verdict.Match : Verdict.Difference }).ToList();
        var report = VectorRun.Report(atTheParse, "Broiler.ECMAScript library-level run, semantics vectors read at the parse", Home.Corpus.Sources);
        output.WriteLine(Begin);
        foreach (var line in report.TrimEnd('\n').Split('\n'))
            output.WriteLine(line);
        output.WriteLine(End);

        var lines = report.Split('\n');
        Assert.Equal(answered.Select(static v => v.Id), lines.Where(static l => l.StartsWith(AlgorithmsLibrary.SemanticsPrefix, StringComparison.Ordinal)).Select(static l => l.Split('\t')[0]));
    }
}
