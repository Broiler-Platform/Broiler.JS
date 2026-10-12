// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           5
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  3/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.ECMAScript.Conformance;

/// <summary>The verdicts of the schema's section 8, with the library-level runner's own.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=A8D484
// Broiler-Human:        PENDING
public enum Verdict
{
    /// <summary>The rendered outcome is one of the expected alternatives.</summary>
    Match,

    /// <summary>It is not, and the vector classifies this engine's difference with a divergence field.</summary>
    ClassifiedDivergence,

    /// <summary>It is not, and nothing classifies the difference.</summary>
    Difference,

    /// <summary>A library-level result of <c>status: not-implemented</c>: the library has nothing to call yet.</summary>
    NotImplemented,
}

/// <summary>One vector's result: what was expected of the runner's subject, what it gave, and the verdict.</summary>
/// <param name="Vector">The vector run.</param>
/// <param name="Expected">The outcome expected of the subject: its per-engine field, or <c>out</c>.</param>
/// <param name="Rendered">The outcome the subject gave, rendered.</param>
/// <param name="Verdict">The comparison's verdict.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=79F472
// Broiler-Human:        PENDING
public sealed record VectorResult(VectorRecord Vector, string Expected, string Rendered, Verdict Verdict)
{
    /// <summary>A divergence vector this engine now matches: the next revision may retire the field.</summary>
    /// <param name="engine">The engine the result is of.</param>
    /// <returns>Whether the vector carries a divergence field for an engine it now matches.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=46A348
    // Broiler-Human:        PENDING
    public bool DivergenceNoLongerHolds(string engine)
        => Verdict == Verdict.Match && Vector["divergence." + engine] is not null;
}

/// <summary>An engine-level runner's view of an engine: evaluate a script in a fresh realm, and render the outcome.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=BF6F04
// Broiler-Human:        PENDING
public interface IVectorEngine
{
    /// <summary><c>broiler-js</c> or <c>broiler-vm</c>, as the corpus's per-engine fields name it.</summary>
    string Name { get; }

    /// <summary>
    /// Evaluates <paramref name="prelude"/> and then <paramref name="script"/> as two Scripts of one
    /// fresh realm, and renders the outcome under the schema's section 7. A fault of the harness,
    /// which no vector expects, is rendered <c>fault: </c> and a description.
    /// </summary>
    /// <param name="prelude">The corpus's prelude.js.</param>
    /// <param name="script">The vector's <c>script</c> field.</param>
    /// <returns>The rendered outcome.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=E10F10
    // Broiler-Human:        PENDING
    string Evaluate(string prelude, string script);
}

/// <summary>The engine-level runner, and the report every runner writes.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=3; Fingerprint=30ECE7
// Broiler-Human:        PENDING
public static class VectorRun
{
    /// <summary>The outcome expected of <paramref name="engine"/>: its per-engine field on an impl-defined vector, otherwise <c>out</c>.</summary>
    /// <param name="vector">The vector.</param>
    /// <param name="engine">The engine's name.</param>
    /// <returns>The expected outcome's text.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=F57295
    // Broiler-Human:        PENDING
    public static string ExpectedFor(VectorRecord vector, string engine)
    {
        ArgumentNullException.ThrowIfNull(vector);
        return vector.Class == "impl-defined" && vector["per-engine." + engine] is { } own ? own : vector.Out;
    }

    /// <summary>The verdict of the schema's section 8 for one rendered outcome.</summary>
    /// <param name="vector">The vector, already validated.</param>
    /// <param name="engine">The engine that rendered the outcome.</param>
    /// <param name="rendered">The rendered outcome.</param>
    /// <returns>The verdict.</returns>
    /// <exception cref="FormatException">The vector's expected outcome does not parse, which a validated corpus never has.</exception>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=15FCC0
    // Broiler-Human:        PENDING
    public static Verdict Compare(VectorRecord vector, string engine, string rendered)
    {
        ArgumentNullException.ThrowIfNull(rendered);
        var expected = ExpectedFor(vector, engine);
        if (!Outcome.TryParseAlternatives(expected, out var alternatives, out _, out var message))
            throw new FormatException(vector.Id + ": the expected outcome does not parse: " + message);
        if (Outcome.AnyIs(alternatives, Normalise(rendered)))
            return Verdict.Match;

        // A fault is the runner's, not the engine's, and no field of a vector can classify it.
        if (rendered.StartsWith("fault: ", StringComparison.Ordinal))
            return Verdict.Difference;
        if (vector.Class is "divergence" or "impl-defined" && vector["divergence." + engine] is not null)
            return Verdict.ClassifiedDivergence;
        return Verdict.Difference;
    }

    /// <summary>Runs every vector of <paramref name="vectors"/> through <paramref name="engine"/>, in order.</summary>
    /// <param name="vectors">The vectors, already validated.</param>
    /// <param name="prelude">The corpus's prelude.js.</param>
    /// <param name="engine">The engine.</param>
    /// <returns>One result per vector, in order.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=BED042
    // Broiler-Human:        PENDING
    public static IReadOnlyList<VectorResult> RunEngine(IEnumerable<VectorRecord> vectors, string prelude, IVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(engine);
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
    /// corpus revision, the subject and the verdict counts in comment lines first.
    /// </summary>
    /// <param name="results">The results, in run order.</param>
    /// <param name="engine">The engine or library the results are of.</param>
    /// <param name="sources">The corpus's pins.</param>
    /// <returns>The report, with LF line endings.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=3; Fingerprint=93EA25
    // Broiler-Human:        PENDING
    public static string Report(IReadOnlyList<VectorResult> results, string engine, VectorSources sources)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(sources);
        var counts = new int[4];
        var noLongerHolds = 0;
        foreach (var result in results)
        {
            counts[(int)result.Verdict]++;
            if (result.DivergenceNoLongerHolds(engine))
                noLongerHolds++;
        }

        var builder = new StringBuilder();
        builder.Append("# engine: ").Append(engine).Append('\n');
        builder.Append("# corpus revision: ").Append(Ascii.Decimal(sources.Revision)).Append('\n');
        builder.Append("# vectors: ").Append(Ascii.Decimal(results.Count)).Append('\n');
        foreach (var verdict in (ReadOnlySpan<Verdict>)[Verdict.Match, Verdict.ClassifiedDivergence, Verdict.Difference, Verdict.NotImplemented])
            builder.Append("# ").Append(Name(verdict)).Append(": ").Append(Ascii.Decimal(counts[(int)verdict])).Append('\n');
        builder.Append("# divergence no longer holds: ").Append(Ascii.Decimal(noLongerHolds)).Append('\n');
        builder.Append("id\tclass\texpected\trendered\tverdict\n");
        foreach (var result in results)
        {
            builder.Append(result.Vector.Id).Append('\t').Append(result.Vector.Class).Append('\t')
                .Append(result.Expected).Append('\t').Append(result.Rendered).Append('\t').Append(Name(result.Verdict));
            if (result.DivergenceNoLongerHolds(engine))
                builder.Append(" (divergence no longer holds)");
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>A verdict as the report writes it.</summary>
    /// <param name="verdict">The verdict.</param>
    /// <returns><c>match</c>, <c>classified-divergence</c>, <c>difference</c> or <c>not-implemented</c>.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=BDBBD5
    // Broiler-Human:        PENDING
    public static string Name(Verdict verdict) => verdict switch
    {
        Verdict.Match => "match",
        Verdict.ClassifiedDivergence => "classified-divergence",
        Verdict.Difference => "difference",
        _ => "not-implemented",
    };

    /// <summary>Line endings in a rendered outcome are normalised to LF; nothing else is.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=9DEAA0
    // Broiler-Human:        PENDING
    private static string Normalise(string rendered) => rendered.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
