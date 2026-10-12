using System.Linq;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The library-level runner: against an empty library every vector is "not implemented", and
/// against <c>Broiler.ECMAScript.Algorithms</c> (from milestone SJL-3) every number vector passes.
/// </summary>
public class LibraryRunTests
{
    [Fact]
    public void Every_number_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "numbers").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict != Verdict.Match)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));
    }

    [Fact]
    public void Against_the_empty_library_every_vector_is_not_implemented()
    {
        var vectors = Home.Corpus.Records.ToList();
        var results = LibraryRun.Run(vectors, new EmptyLibrary());

        Assert.Equal(vectors.Count, results.Count);
        Assert.All(results, r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
    }

    [Fact]
    public void Every_vector_s_inputs_decode()
    {
        Assert.All(Home.Corpus.Records, vector => Assert.NotEmpty(LibraryRun.Inputs(vector)));
    }

    [Fact]
    public void The_report_counts_every_verdict_and_lists_every_vector()
    {
        var results = LibraryRun.Run(Home.Corpus.Records, new EmptyLibrary());
        var lines = VectorRun.Report(results, "empty", Home.Corpus.Sources).Split('\n');

        Assert.Equal("# engine: empty", lines[0]);
        Assert.Equal("# corpus revision: " + Home.Corpus.Sources.Revision, lines[1]);
        Assert.Equal("# not-implemented: " + results.Count, lines[6]);
        Assert.Equal("id\tclass\texpected\trendered\tverdict", lines[8]);
        Assert.Equal(results.Count, lines.Skip(9).Count(line => line.Length > 0));
    }
}
