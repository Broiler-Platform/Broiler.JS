// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           4
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  5/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Conformance;

/// <summary>The kinds of outcome the corpus schema's section 4 defines.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=88CC22
// Broiler-Human:        PENDING
public enum OutcomeKind
{
    /// <summary>The operation or script produced a value: <c>value: </c> and a typed value.</summary>
    Value,

    /// <summary>A standard error object was thrown: <c>error: </c> and its constructor's name.</summary>
    Error,

    /// <summary>Any other value was thrown: <c>throw: </c> and a typed value.</summary>
    Throw,

    /// <summary>A library-level status: <c>status: </c> and a status word.</summary>
    Status,
}

/// <summary>
/// One outcome of the corpus schema's section 4: a value, a thrown error object by its
/// constructor's name, any other thrown value, or a library-level status.
/// </summary>
/// <param name="Kind">Which kind of outcome it is.</param>
/// <param name="Text">The outcome's one written form, kind included.</param>
/// <param name="Value">The value of a <see cref="OutcomeKind.Value"/> or <see cref="OutcomeKind.Throw"/> outcome.</param>
/// <param name="Name">The error name of an <see cref="OutcomeKind.Error"/> outcome, or the word of a <see cref="OutcomeKind.Status"/> one.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=4; Fingerprint=39325E
// Broiler-Human:        PENDING
public sealed record Outcome(OutcomeKind Kind, string Text, TypedValue? Value, string? Name)
{
    /// <summary>The standard errors an <c>error:</c> outcome may name (ECMA-262 sections 20.5.5 and 20.5.7).</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=FB1824
    // Broiler-Human:        PENDING
    public static FrozenSet<string> ErrorNames { get; } = FrozenSet.Create(
        StringComparer.Ordinal,
        "Error", "EvalError", "RangeError", "ReferenceError", "SyntaxError", "TypeError", "URIError", "AggregateError");

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=F91736
    // Broiler-Human:        PENDING
    private const string Separator = " | ";

    /// <summary>
    /// Parses an outcome with its alternatives, separated by <c> | </c> (schema section 4). A string
    /// value may hold that separator itself, so the text is read value by value rather than split.
    /// </summary>
    /// <param name="text">An <c>out</c> or <c>per-engine.*</c> field's value.</param>
    /// <param name="outcomes">The alternatives, in the order written.</param>
    /// <param name="code">The defect, named the way the validator reports it, when the text is refused.</param>
    /// <param name="message">What is wrong with the text, when it is refused.</param>
    /// <returns>Whether the text is one or more distinct outcomes.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=3; Fingerprint=E81555
    // Broiler-Human:        PENDING
    public static bool TryParseAlternatives(string text, out IReadOnlyList<Outcome> outcomes, out string code, out string message)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parsed = new List<Outcome>();
        outcomes = parsed;
        var position = 0;
        while (true)
        {
            if (!TryParseOne(text, ref position, out var outcome, out code, out message))
                return false;
            foreach (var earlier in parsed)
            {
                if (earlier.Text == outcome.Text)
                {
                    code = "bad-outcome";
                    message = "'" + outcome.Text + "' is listed twice";
                    return false;
                }
            }

            parsed.Add(outcome);
            if (position == text.Length)
                return true;
            if (string.CompareOrdinal(text, position, Separator, 0, Separator.Length) != 0)
            {
                code = "bad-outcome";
                message = "'" + text + "' continues after an outcome without ' | '";
                return false;
            }

            position += Separator.Length;
        }
    }

    /// <summary>Whether one of <paramref name="alternatives"/> is written exactly <paramref name="rendered"/>.</summary>
    /// <param name="alternatives">The alternatives an expected outcome lists.</param>
    /// <param name="rendered">A rendered outcome.</param>
    /// <returns><see langword="true"/> when the rendered text equals an alternative's, ordinally.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=79BD2C
    // Broiler-Human:        PENDING
    public static bool AnyIs(IReadOnlyList<Outcome> alternatives, string rendered)
    {
        ArgumentNullException.ThrowIfNull(alternatives);
        foreach (var alternative in alternatives)
        {
            if (alternative.Text == rendered)
                return true;
        }

        return false;
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=5; Fingerprint=394912
    // Broiler-Human:        PENDING
    private static bool TryParseOne(string text, ref int position, out Outcome outcome, out string code, out string message)
    {
        outcome = null!;
        code = "bad-outcome";
        message = string.Empty;
        var colon = text.IndexOf(": ", position, StringComparison.Ordinal);
        if (colon < 0)
        {
            message = "'" + text[position..] + "' is not kind: operand";
            return false;
        }

        var kindText = text[position..colon];
        OutcomeKind kind;
        switch (kindText)
        {
            case "value": kind = OutcomeKind.Value; break;
            case "error": kind = OutcomeKind.Error; break;
            case "throw": kind = OutcomeKind.Throw; break;
            case "status": kind = OutcomeKind.Status; break;
            default:
                message = "'" + text + "' does not start with value:, error:, throw: or status:";
                return false;
        }

        var start = position;
        position = colon + 2;
        if (!TypedValue.TryReadToken(text, ref position, out var token))
        {
            message = "'" + text + "' has no operand after " + kindText + ":";
            return false;
        }

        var whole = text[start..position];
        switch (kind)
        {
            case OutcomeKind.Value:
            case OutcomeKind.Throw:
                if (!TypedValue.TryParse(token, out var value, out code, out message))
                    return false;
                if (value.Kind == TypedValueKind.Integer)
                {
                    code = "bad-value";
                    message = "'" + token + "' is an input notation; an outcome writes a Number as f64:";
                    return false;
                }

                outcome = new Outcome(kind, whole, value, null);
                return true;
            case OutcomeKind.Error:
                if (!ErrorNames.Contains(token))
                {
                    message = "'" + token + "' is not a standard error name";
                    return false;
                }

                outcome = new Outcome(kind, whole, null, token);
                return true;
            default:
                // A status word: [a-z][a-z0-9-]*.
                if (!Ascii.IsLower(token[0]) || !Ascii.All(token, static c => Ascii.IsLower(c) || Ascii.IsDigit(c) || c == '-'))
                {
                    message = "'" + token + "' is not a status word";
                    return false;
                }

                outcome = new Outcome(kind, whole, null, token);
                return true;
        }
    }
}
