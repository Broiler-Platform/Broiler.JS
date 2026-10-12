using System.Globalization;
using System.Text;
using Broiler.JavaScript.Vectors.Tests.Corpus;

namespace Broiler.JavaScript.Vectors.Tests.Runners;

/// <summary>The schema's section 8, with the library-level runner's own verdict.</summary>
public enum Verdict
{
    Match,
    ClassifiedDivergence,
    Difference,

    /// <summary>A library-level result of <c>status: not-implemented</c>: the library has nothing to call yet.</summary>
    NotImplemented,
}

/// <summary>One vector's result: what was expected of the running engine, what it gave, and the verdict.</summary>
public sealed record VectorResult(VectorRecord Vector, string Expected, string Rendered, Verdict Verdict)
{
    /// <summary>A divergence vector this engine now matches: the next revision may retire the field.</summary>
    public bool DivergenceNoLongerHolds(string engine)
        => Verdict == Verdict.Match && Vector["divergence." + engine] is not null;
}

/// <summary>An engine-level runner's view of an engine: evaluate a script in a fresh realm, and render the outcome.</summary>
public interface IVectorEngine
{
    /// <summary><c>broiler-js</c> or <c>broiler-vm</c>, as the corpus's per-engine fields name it.</summary>
    string Name { get; }

    /// <summary>
    /// Evaluates <paramref name="prelude"/> and then <paramref name="script"/> as two Scripts of one
    /// fresh realm, and renders the outcome under the schema's section 7. A fault of the harness,
    /// which no vector expects, is rendered <c>fault: </c> and a description.
    /// </summary>
    string Evaluate(string prelude, string script);
}

public static class VectorRun
{
    public static string ExpectedFor(VectorRecord vector, string engine)
        => vector.Class == "impl-defined" && vector["per-engine." + engine] is { } own ? own : vector.Out;

    public static Verdict Compare(VectorRecord vector, string engine, string rendered)
    {
        var expected = ExpectedFor(vector, engine);
        if (!Outcome.TryParseAlternatives(expected, out var alternatives, out _, out var message))
            throw new InvalidDataException($"{vector.Id}: the expected outcome does not parse: {message}");
        if (alternatives.Any(a => a.Text == Normalise(rendered)))
            return Verdict.Match;
        // A fault is the runner's, not the engine's, and no field of a vector can classify it.
        if (rendered.StartsWith("fault: ", StringComparison.Ordinal))
            return Verdict.Difference;
        if (vector.Class is "divergence" or "impl-defined" && vector["divergence." + engine] is not null)
            return Verdict.ClassifiedDivergence;
        return Verdict.Difference;
    }

    /// <summary>Runs every vector of <paramref name="vectors"/> through <paramref name="engine"/>, in order.</summary>
    public static IReadOnlyList<VectorResult> RunEngine(IEnumerable<VectorRecord> vectors, string prelude, IVectorEngine engine)
    {
        var results = new List<VectorResult>();
        foreach (var vector in vectors)
        {
            var rendered = Normalise(engine.Evaluate(prelude, vector.Script));
            results.Add(new VectorResult(vector, ExpectedFor(vector, engine.Name), rendered, Compare(vector, engine.Name, rendered)));
        }

        return results;
    }

    /// <summary>
    /// The report the schema's section 7 describes, one tab-separated line per vector, with the
    /// corpus revision, the engine and the verdict counts in comment lines first.
    /// </summary>
    public static string Report(IReadOnlyList<VectorResult> results, string engine, VectorSources sources)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"# engine: {engine}\n");
        builder.Append(CultureInfo.InvariantCulture, $"# corpus revision: {sources.Revision}\n");
        builder.Append(CultureInfo.InvariantCulture, $"# vectors: {results.Count}\n");
        foreach (var verdict in Enum.GetValues<Verdict>())
            builder.Append(CultureInfo.InvariantCulture, $"# {Name(verdict)}: {results.Count(r => r.Verdict == verdict)}\n");
        builder.Append(CultureInfo.InvariantCulture, $"# divergence no longer holds: {results.Count(r => r.DivergenceNoLongerHolds(engine))}\n");
        builder.Append("id\tclass\texpected\trendered\tverdict\n");
        foreach (var result in results)
        {
            var verdict = Name(result.Verdict) + (result.DivergenceNoLongerHolds(engine) ? " (divergence no longer holds)" : string.Empty);
            builder.Append(CultureInfo.InvariantCulture, $"{result.Vector.Id}\t{result.Vector.Class}\t{result.Expected}\t{result.Rendered}\t{verdict}\n");
        }

        return builder.ToString();
    }

    public static string Name(Verdict verdict) => verdict switch
    {
        Verdict.Match => "match",
        Verdict.ClassifiedDivergence => "classified-divergence",
        Verdict.Difference => "difference",
        _ => "not-implemented",
    };

    /// <summary>Line endings in a rendered outcome are normalised to LF; nothing else is.</summary>
    private static string Normalise(string rendered) => rendered.Replace("\r\n", "\n").Replace('\r', '\n');
}
