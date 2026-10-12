using Broiler.JavaScript.Vectors.Tests.Corpus;

namespace Broiler.JavaScript.Vectors.Tests.Runners;

/// <summary>
/// A shared library as the library-level runner sees it: an operation the corpus names, called on
/// the vector's decoded inputs, with the outcome rendered in the corpus notation.
/// </summary>
public interface IVectorLibrary
{
    string Name { get; }

    /// <summary>The rendered outcome, or <c>status: not-implemented</c> for an operation the library does not have.</summary>
    string Call(string operation, IReadOnlyList<TypedValue> inputs);
}

/// <summary>
/// The library the runner calls until milestone SJL-3 gives it one: it has no operation at all.
/// </summary>
public sealed class EmptyLibrary : IVectorLibrary
{
    public const string NotImplemented = "status: not-implemented";

    public string Name => "empty";

    public string Call(string operation, IReadOnlyList<TypedValue> inputs) => NotImplemented;
}

/// <summary>
/// The library-level runner: it calls a library's API directly with each vector's inputs, and
/// compares the outcome with the vector's <c>out</c>. A library is not an engine, so a vector's
/// per-engine fields do not apply to it.
/// </summary>
public static class LibraryRun
{
    public static IReadOnlyList<VectorResult> Run(IEnumerable<VectorRecord> vectors, IVectorLibrary library)
    {
        var results = new List<VectorResult>();
        foreach (var vector in vectors)
        {
            var rendered = library.Call(vector["op"] ?? string.Empty, Inputs(vector));
            var verdict = rendered == EmptyLibrary.NotImplemented
                ? Verdict.NotImplemented
                : Outcome.TryParseAlternatives(vector.Out, out var alternatives, out _, out _) && alternatives.Any(a => a.Text == rendered)
                    ? Verdict.Match
                    : Verdict.Difference;
            results.Add(new VectorResult(vector, vector.Out, rendered, verdict));
        }

        return results;
    }

    /// <summary>A vector's <c>in</c> field, decoded; the validator has already accepted it.</summary>
    public static IReadOnlyList<TypedValue> Inputs(VectorRecord vector)
    {
        var text = vector["in"] ?? string.Empty;
        var inputs = new List<TypedValue>();
        var position = 0;
        while (position < text.Length)
        {
            if (!TypedValue.TryReadToken(text, ref position, out var token) || !TypedValue.TryParse(token, out var value, out _, out var message))
                throw new InvalidDataException($"{vector.Id}: the inputs do not parse");
            inputs.Add(value);
            position++;
        }

        return inputs;
    }
}
