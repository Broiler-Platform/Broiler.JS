// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           2
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Conformance;

/// <summary>
/// A shared library as the library-level runner sees it: an operation the corpus names, called on
/// the vector's decoded inputs, with the outcome rendered in the corpus notation.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=D9A3A6
// Broiler-Human:        PENDING
public interface IVectorLibrary
{
    /// <summary>The library's name, as a report names its subject.</summary>
    string Name { get; }

    /// <summary>Calls one operation.</summary>
    /// <param name="operation">The vector's <c>op</c> field.</param>
    /// <param name="inputs">The vector's <c>in</c> field, decoded.</param>
    /// <returns>The rendered outcome, or <see cref="EmptyLibrary.NotImplemented"/> for an operation the library does not have.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B405B2
    // Broiler-Human:        PENDING
    string Call(string operation, IReadOnlyList<TypedValue> inputs);
}

/// <summary>
/// The library the runner calls until milestone SJL-3 gives it one: it has no operation at all.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=76A1DC
// Broiler-Human:        PENDING
public sealed class EmptyLibrary : IVectorLibrary
{
    /// <summary>The outcome of an operation the library does not have.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=75820D
    // Broiler-Human:        PENDING
    public const string NotImplemented = "status: not-implemented";

    /// <inheritdoc/>
    public string Name => "empty";

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=1CBCC6
    // Broiler-Human:        PENDING
    public string Call(string operation, IReadOnlyList<TypedValue> inputs) => NotImplemented;
}

/// <summary>
/// The library-level runner: it calls a library's API directly with each vector's inputs, and
/// compares the outcome with the vector's <c>out</c>. A library is not an engine, so a vector's
/// per-engine fields do not apply to it.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=959A3D
// Broiler-Human:        PENDING
public static class LibraryRun
{
    /// <summary>Runs every vector of <paramref name="vectors"/> through <paramref name="library"/>, in order.</summary>
    /// <param name="vectors">The vectors, already validated.</param>
    /// <param name="library">The library.</param>
    /// <returns>One result per vector, in order.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=298F32
    // Broiler-Human:        PENDING
    public static IReadOnlyList<VectorResult> Run(IEnumerable<VectorRecord> vectors, IVectorLibrary library)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(library);
        var results = new List<VectorResult>();
        foreach (var vector in vectors)
        {
            var rendered = library.Call(vector["op"] ?? string.Empty, Inputs(vector));
            var verdict = rendered == EmptyLibrary.NotImplemented
                ? Verdict.NotImplemented
                : Outcome.TryParseAlternatives(vector.Out, out var alternatives, out _, out _) && Outcome.AnyIs(alternatives, rendered)
                    ? Verdict.Match
                    : Verdict.Difference;
            results.Add(new VectorResult(vector, vector.Out, rendered, verdict));
        }

        return results;
    }

    /// <summary>A vector's <c>in</c> field, decoded; the validator has already accepted it.</summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The decoded inputs, in order.</returns>
    /// <exception cref="FormatException">The inputs do not parse, which a validated vector's never do.</exception>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=64DCB6
    // Broiler-Human:        PENDING
    public static IReadOnlyList<TypedValue> Inputs(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        var text = vector["in"] ?? string.Empty;
        var inputs = new List<TypedValue>();
        var position = 0;
        while (position < text.Length)
        {
            if (!TypedValue.TryReadToken(text, ref position, out var token) || !TypedValue.TryParse(token, out var value, out _, out _))
                throw new FormatException(vector.Id + ": the inputs do not parse");
            inputs.Add(value);
            position++;
        }

        return inputs;
    }
}
