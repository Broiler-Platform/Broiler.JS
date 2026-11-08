// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   11
// Annotated:        11/11
// Exempt:           2
// Human-reviewed:   0/11
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       11
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Conformance;

/// <summary>
/// A shared library as the library-level runner sees it: an operation the corpus names, called on
/// the vector's decoded inputs, with the outcome rendered in the corpus notation.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=D9A3A6
// Hairs-Human:        PENDING
public interface IVectorLibrary
{
    /// <summary>The library's name, as a report names its subject.</summary>
    string Name { get; }

    /// <summary>Calls one operation.</summary>
    /// <param name="operation">The vector's <c>op</c> field.</param>
    /// <param name="inputs">The vector's <c>in</c> field, decoded.</param>
    /// <returns>The rendered outcome, or <see cref="EmptyLibrary.NotImplemented"/> for an operation the library does not have.</returns>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B405B2
    // Hairs-Human:        PENDING
    string Call(string operation, IReadOnlyList<TypedValue> inputs);
}

/// <summary>
/// A library that is handed each vector whole, its <c>script</c> among its fields, where its
/// operation's arguments are in the script and not in <c>in</c>: the Intl area's sub-area (b), whose
/// vectors give the options bag and the arguments in the script alone. <see cref="LibraryRun.Run"/>
/// calls <see cref="Call(VectorRecord)"/> on such a library instead of <see cref="IVectorLibrary.Call"/>.
/// <i>(Added 2026-11-01, at stage SJL-9.3b of milestone SJL-9.)</i>
/// </summary>
// Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=61D17E
// Hairs-Human:        PENDING
public interface IVectorRecordLibrary : IVectorLibrary
{
    /// <summary>Calls the vector's operation, reading what it needs of the vector, its <c>script</c> included.</summary>
    /// <param name="vector">The vector, already validated.</param>
    /// <returns>The rendered outcome, or <see cref="EmptyLibrary.NotImplemented"/> for an operation the library does not have.</returns>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=71E95D
    // Hairs-Human:        PENDING
    string Call(VectorRecord vector);
}

/// <summary>
/// The library the runner calls until milestone SJL-3 gives it one: it has no operation at all.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=76A1DC
// Hairs-Human:        PENDING
public sealed class EmptyLibrary : IVectorLibrary
{
    /// <summary>The outcome of an operation the library does not have.</summary>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=75820D
    // Hairs-Human:        PENDING
    public const string NotImplemented = "status: not-implemented";

    /// <inheritdoc/>
    public string Name => "empty";

    /// <inheritdoc/>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=1CBCC6
    // Hairs-Human:        PENDING
    public string Call(string operation, IReadOnlyList<TypedValue> inputs) => NotImplemented;
}

/// <summary>
/// The library-level runner: it calls a library's API directly with each vector's inputs, and
/// compares the outcome with the vector's <c>out</c>, or with the expectation both engines'
/// per-engine fields agree on (<see cref="Expected"/>). A library is not an engine, so one engine's
/// expectation alone does not apply to it. <i>(Revised 2026-10-13, at milestone SJL-4.3. The summary
/// ended: "compares the outcome with the vector's <c>out</c>. A library is not an engine, so a
/// vector's per-engine fields do not apply to it.")</i> A library that reads the vector whole, an
/// <see cref="IVectorRecordLibrary"/>, is handed the vector instead of its decoded inputs. <i>(Added
/// 2026-11-01, at stage SJL-9.3b, when the Intl area's sub-area (b) gave vectors whose options are in
/// the script alone.)</i>
/// </summary>
// Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=959A3D
// Hairs-Human:        PENDING
public static class LibraryRun
{
    /// <summary>Runs every vector of <paramref name="vectors"/> through <paramref name="library"/>, in order.</summary>
    /// <param name="vectors">The vectors, already validated.</param>
    /// <param name="library">The library.</param>
    /// <returns>One result per vector, in order.</returns>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=2C6D13
    // Hairs-Human:        PENDING
    public static IReadOnlyList<VectorResult> Run(IEnumerable<VectorRecord> vectors, IVectorLibrary library)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(library);
        var results = new List<VectorResult>();
        foreach (var vector in vectors)
        {
            var expected = Expected(vector);
            var rendered = library is IVectorRecordLibrary whole
                ? whole.Call(vector)
                : library.Call(vector["op"] ?? string.Empty, Inputs(vector));
            var verdict = rendered == EmptyLibrary.NotImplemented
                ? Verdict.NotImplemented
                : Outcome.TryParseAlternatives(expected, out var alternatives, out _, out _) && Outcome.AnyIs(alternatives, rendered)
                    ? Verdict.Match
                    : Verdict.Difference;
            results.Add(new VectorResult(vector, expected, rendered, verdict));
        }

        return results;
    }

    /// <summary>
    /// The outcome a library is compared with: <c>out</c>, except on an <c>impl-defined</c> vector
    /// whose per-engine expectations name both engines and agree, which is then the library's.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The expected outcome.</returns>
    /// <remarks>
    /// Both engines' plans commit them to the same implementation-defined result only when that result
    /// is the shared library's - its documented bound for an approximated function, its refusal past
    /// any representation - so an agreeing pair of expectations is the library's own commitment. Where
    /// the engines' expectations differ, each is an engine's policy, and the library is compared with
    /// the specification's ideal. <i>(Added 2026-10-13, at milestone SJL-4.3, when the numerics area
    /// gave impl-defined vectors both engines agree on; until then every vector was compared with its
    /// <c>out</c>.)</i>
    /// </remarks>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=63F339
    // Hairs-Human:        PENDING
    public static string Expected(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Class == "impl-defined"
            && vector["per-engine.broiler-js"] is { } js
            && vector["per-engine.broiler-vm"] is { } vm
            && js == vm)
            return js;

        return vector.Out;
    }

    /// <summary>A vector's <c>in</c> field, decoded; the validator has already accepted it.</summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The decoded inputs, in order.</returns>
    /// <exception cref="FormatException">The inputs do not parse, which a validated vector's never do.</exception>
    // Hairs-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=64DCB6
    // Hairs-Human:        PENDING
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
