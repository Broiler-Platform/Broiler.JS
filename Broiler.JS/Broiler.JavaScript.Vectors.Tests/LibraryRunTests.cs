using Broiler.JavaScript.Vectors.Tests.Runners;

namespace Broiler.JavaScript.Vectors.Tests;

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
}
