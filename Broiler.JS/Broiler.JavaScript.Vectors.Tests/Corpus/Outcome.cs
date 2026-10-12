using System.Text.RegularExpressions;

namespace Broiler.JavaScript.Vectors.Tests.Corpus;

public enum OutcomeKind
{
    Value,
    Error,
    Throw,
    Status,
}

/// <summary>
/// One outcome of the schema's section 4: a value, a thrown error object by its constructor's
/// name, any other thrown value, or a library-level status.
/// </summary>
public sealed partial record Outcome(OutcomeKind Kind, string Text, TypedValue? Value, string? Name)
{
    /// <summary>The standard errors an <c>error:</c> outcome may name.</summary>
    public static readonly IReadOnlySet<string> ErrorNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Error", "EvalError", "RangeError", "ReferenceError", "SyntaxError", "TypeError", "URIError", "AggregateError",
    };

    private const string Separator = " | ";

    /// <summary>
    /// Parses an outcome with its alternatives, separated by <c> | </c>. A string value may hold
    /// that separator itself, so the text is read value by value rather than split.
    /// </summary>
    public static bool TryParseAlternatives(string text, out IReadOnlyList<Outcome> outcomes, out string code, out string message)
    {
        var parsed = new List<Outcome>();
        outcomes = parsed;
        var position = 0;
        while (true)
        {
            if (!TryParseOne(text, ref position, out var outcome, out code, out message))
                return false;
            if (parsed.Any(o => o.Text == outcome.Text))
            {
                code = "bad-outcome";
                message = $"'{outcome.Text}' is listed twice";
                return false;
            }

            parsed.Add(outcome);
            if (position == text.Length)
                return true;
            if (string.CompareOrdinal(text, position, Separator, 0, Separator.Length) != 0)
            {
                code = "bad-outcome";
                message = $"'{text}' continues after an outcome without ' | '";
                return false;
            }

            position += Separator.Length;
        }
    }

    private static bool TryParseOne(string text, ref int position, out Outcome outcome, out string code, out string message)
    {
        outcome = null!;
        code = "bad-outcome";
        message = string.Empty;
        var colon = text.IndexOf(": ", position, StringComparison.Ordinal);
        if (colon < 0)
        {
            message = $"'{text[position..]}' is not kind: operand";
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
                message = $"'{text}' does not start with value:, error:, throw: or status:";
                return false;
        }

        var start = position;
        position = colon + 2;
        if (!TypedValue.TryReadToken(text, ref position, out var token))
        {
            message = $"'{text}' has no operand after {kindText}:";
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
                    message = $"'{token}' is an input notation; an outcome writes a Number as f64:";
                    return false;
                }

                outcome = new Outcome(kind, whole, value, null);
                return true;
            case OutcomeKind.Error:
                if (!ErrorNames.Contains(token))
                {
                    message = $"'{token}' is not a standard error name";
                    return false;
                }

                outcome = new Outcome(kind, whole, null, token);
                return true;
            default:
                if (!StatusPattern().IsMatch(token))
                {
                    message = $"'{token}' is not a status word";
                    return false;
                }

                outcome = new Outcome(kind, whole, null, token);
                return true;
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial System.Text.RegularExpressions.Regex StatusPattern();
}
