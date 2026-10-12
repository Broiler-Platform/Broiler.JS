using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Runners;

namespace Broiler.JavaScript.Vectors.Tests;

/// <summary>The corpus in its home, loaded and validated once for every test that reads it.</summary>
public static class Home
{
    /// <summary>conformance/vectors/ in this repository, found from the test assembly's directory.</summary>
    public static string Root { get; } = CorpusDirectory.Home(AppContext.BaseDirectory);

    private static readonly Lazy<VectorCorpus> Loaded = new(() => CorpusDirectory.Load(Root));

    public static VectorCorpus Corpus => Loaded.Value;

    /// <summary>The script every engine-level runner evaluates before a vector's own.</summary>
    public static string Prelude => CorpusDirectory.Prelude(Root);
}
