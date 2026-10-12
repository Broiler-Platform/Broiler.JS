using System.Linq;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The library-level runner. It has no library to call until milestone SJL-3, so it runs against an
/// empty one, and every vector's outcome is "not implemented".
/// </summary>
public class LibraryRunTests
{
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
