using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Semantics;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The library-level runner's semantics adapter (milestone SJL-15.2, from stage 2; the session's decisions D-js-adapter of
/// 2026-11-24 and D-js-adapter-form of 2026-11-25): the <c>semantics</c> area's vectors (corpus revision 42,
/// <c>conformance/vectors/semantics/</c>) whose row the semantic model reports, parsed by <c>Broiler.ECMAScript.Frontend</c>
/// with <see cref="ParseRequest.BuildSemanticModel"/> on, and judged at the parse.
/// </summary>
/// <remarks>
/// <para>
/// <b>Which vectors are answered</b> (<see cref="IsLibrarySemantics"/>): a vector of the area, of one of its two operations,
/// whose identifier names a row of <see cref="SemanticsAnsweredRows"/>. A vector's row is its identifier's third segment,
/// upper-cased, where it reads as a catalogue row (<c>semantics/{file}/{group}/{n}</c>, the generator's form); the
/// <c>note</c> decides nothing (the corpus's <c>SCHEMA.md</c>). The groups that name no row are never answered. The list is
/// the adapter's own and grows stage by stage; it is not read from the Frontend's table, so that the library cannot shape
/// its own expectations, and the Conformance tests hold it equal to that table. Every other semantics vector is
/// <c>status: not-implemented</c>.
/// </para>
/// <para>
/// <b>How each is parsed.</b> <c>ScriptEvaluation</c>: the vector's one String as a Script, under the library's default
/// request for the goal (<see cref="SyntaxRequest"/>) with the member on. <c>DirectEval</c>, the eval code a direct
/// <c>eval</c> in that Script evaluates, in the context the Script gives it: the Script is parsed with the member on and its
/// tokens retained; the one call <c>eval(</c> a string literal <c>)</c> is found among them, the Script holding exactly one
/// <c>eval</c> name, not after <c>.</c> or <c>?.</c>; its literal's String value is the eval code; and the caller's facts are
/// the model's functions whose node holds the call (a field initialiser's by its initialiser), the innermost giving the
/// strictness and the nearest that is not an arrow the inherited context (PerformEval step 10). The eval code is then parsed
/// under <see cref="ParseGoal.Eval"/> with that context, forced strict where the caller is strict, the member on. Whether a
/// name <c>eval</c> is the intrinsic one is not resolved: resolution is a later stage's. A Script the on path refuses, or
/// one with no such call or more than one <c>eval</c>, is not implemented.
/// </para>
/// <para>
/// <b>What it answers</b> is the syntax area's rendering (<see cref="RenderParse"/>): <c>error: SyntaxError</c> for a
/// refusal and <c>status: accepted</c> for an acceptance. <see cref="MatchesSemanticsAtTheParse"/> reads the expected
/// outcome at the parse, as bundle sjl-13-001's rule reads its syntax vectors': <c>error: SyntaxError</c> as a refusal, any
/// other outcome as an acceptance (the session's reading, D-js-adapter-form, flagged for the owner).
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>A semantics vector's operation: the source evaluated as a Script.</summary>
    internal const string ScriptEvaluationOperation = "ScriptEvaluation";

    /// <summary>A semantics vector's operation: the eval code a direct <c>eval</c> in the Script evaluates.</summary>
    internal const string DirectEvalOperation = "DirectEval";

    /// <summary>The identifier prefix of the semantics area's vectors.</summary>
    internal const string SemanticsPrefix = "semantics/";

    /// <summary>
    /// The rows whose vectors this adapter answers, each the semantic model's (SJL-15.2): stage 2's, the super group with
    /// EE0172, the ContainsArguments rows, eval code's rows from the caller's context, and three rows of expressions.
    /// </summary>
    internal static readonly IReadOnlyList<string> SemanticsAnsweredRows =
    [
        // Stage 2: HasDirectSuper of methods, and Contains SuperProperty or SuperCall in functions, class elements and programs.
        "EE0015", "EE0074", "EE0075", "EE0076", "EE0077", "EE0093", "EE0102", "EE0103", "EE0104", "EE0105", "EE0106",
        "EE0117", "EE0118", "EE0119", "EE0120", "EE0121", "EE0124", "EE0126", "EE0131", "EE0139", "EE0142", "EE0150",
        "EE0151", "EE0152", "EE0153", "EE0162", "EE0172", "EP0017", "EP0018",

        // Stage 2: ContainsArguments of class field initialisers and static blocks.
        "EE0130", "EE0138",

        // Stage 2: eval code's rows from the caller's context (PerformEval steps 11.f to 11.h).
        "EA0003", "EA0004", "EA0005",

        // Stage 2: expressions' rows.
        "EE0018", "EE0021", "EE0054",
    ];

    /// <summary>Whether an operation is one of the semantics area's.</summary>
    internal static bool IsSemanticsOperation(string? operation) => operation is ScriptEvaluationOperation or DirectEvalOperation;

    /// <summary>A semantics vector's row: its identifier's third segment, upper-cased, where it reads <c>E[EAP]dddd</c>; null otherwise.</summary>
    internal static string? SemanticsRow(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (!vector.Id.StartsWith(SemanticsPrefix, StringComparison.Ordinal))
            return null;
        var segments = vector.Id.Split('/');
        if (segments.Length < 4)
            return null;
        // Upper-cased by hand over ASCII: the probe reaches no culture or casing table (rule SL3).
        var group = segments[2];
        if (group.Length != 6 || group[0] is not ('e' or 'E') || group[1] is not ('e' or 'E' or 'a' or 'A' or 'p' or 'P'))
            return null;
        for (var i = 2; i < 6; i++)
        {
            if (group[i] is < '0' or > '9')
                return null;
        }

        return string.Concat("E", group[1] is 'e' or 'E' ? "E" : group[1] is 'a' or 'A' ? "A" : "P", group.AsSpan(2));
    }

    /// <summary>Whether a vector is one this adapter answers: of the area, of one of its operations, and of a row in <see cref="SemanticsAnsweredRows"/>.</summary>
    internal static bool IsLibrarySemantics(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (!vector.Id.StartsWith(SemanticsPrefix, StringComparison.Ordinal) || !IsSemanticsOperation(vector["op"]) || SemanticsRow(vector) is not { } row)
            return false;
        foreach (var answered in SemanticsAnsweredRows)
        {
            if (answered == row)
                return true;
        }

        return false;
    }

    /// <summary>The Script request of a semantics vector: the library's default for the Script goal, with the semantic model on.</summary>
    internal static ParseRequest SemanticsScriptRequest(string source, bool retainTokens = false) =>
        new(new SourceText(source), ParseGoal.Script, UnicodeDataHome.Tables, RunnerRegularExpressions.Instance)
        {
            BuildSemanticModel = true,
            RetainTokens = retainTokens,
        };

    /// <summary>The semantics area: an answered vector's parse, rendered (see the type's remarks).</summary>
    private static string SemanticsArea(VectorRecord vector)
    {
        var inputs = LibraryRun.Inputs(vector);
        if (inputs.Count != 1 || inputs[0].Kind != TypedValueKind.String)
            return EmptyLibrary.NotImplemented;
        var source = inputs[0].Chars;
        switch (vector["op"])
        {
            case ScriptEvaluationOperation:
                return RenderParse(SyntaxParser.Parse(SemanticsScriptRequest(source)).Outcome);
            case DirectEvalOperation:
                return DirectEvalRequest(source) is { } request ? RenderParse(SyntaxParser.Parse(request).Outcome) : EmptyLibrary.NotImplemented;
            default:
                return EmptyLibrary.NotImplemented;
        }
    }

    /// <summary>
    /// The eval request a <c>DirectEval</c> vector's Script makes (see the type's remarks): the eval code of its one direct
    /// <c>eval</c> of a string literal, in the context and the strictness of the code that holds the call; null where the
    /// Script is refused on the on path, or holds no such call, or more than one <c>eval</c> name.
    /// </summary>
    internal static ParseRequest? DirectEvalRequest(string script)
    {
        var parsed = SyntaxParser.Parse(SemanticsScriptRequest(script, retainTokens: true));
        if (parsed.Outcome != ParseOutcome.Accepted || parsed.Model is not { } model)
            return null;

        // The one eval name, called with one string literal, not a member's name.
        var tokens = parsed.Tokens;
        var at = -1;
        string? code = null;
        var names = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != TokenKind.IdentifierName || tokens[i].Value != "eval")
                continue;
            names++;
            var member = i > 0 && tokens[i - 1].Kind is TokenKind.Dot or TokenKind.QuestionDot;
            if (!member && i + 3 < tokens.Count && tokens[i + 1].Kind == TokenKind.LeftParen && tokens[i + 2].Kind == TokenKind.StringLiteral
                && tokens[i + 3].Kind == TokenKind.RightParen)
            {
                at = tokens[i].Start;
                code = tokens[i + 2].Value;
            }
        }

        if (names != 1 || code is null)
            return null;

        // The functions whose code holds the call, outermost first (the model's order is by start, then by creation).
        var holding = new List<FunctionFacts>();
        foreach (var facts in model.Functions)
        {
            var span = facts.Node is FieldDefinition { Value: { } value } ? value.Span : facts.Node.Span;
            if (span.Offset <= at && at < span.Offset + span.Length)
                holding.Add(facts);
        }

        if (holding.Count == 0)
            return null;
        InheritedContext? context = null;
        for (var i = holding.Count - 1; i >= 0 && context is null; i--)
        {
            context = holding[i].Kind switch
            {
                FunctionKind.Arrow => null,
                FunctionKind.Script => InheritedContext.TopLevel,
                FunctionKind.Normal => new InheritedContext { InFunction = true },
                FunctionKind.Method or FunctionKind.Getter or FunctionKind.Setter or FunctionKind.ClassConstructor or FunctionKind.StaticBlock =>
                    new InheritedContext { InFunction = true, InMethod = true },
                FunctionKind.DerivedConstructor => new InheritedContext { InFunction = true, InMethod = true, InDerivedConstructor = true },
                FunctionKind.FieldInitializer => new InheritedContext { InFunction = true, InMethod = true, InClassFieldInitializer = true },
                _ => InheritedContext.TopLevel,
            };
        }

        return new ParseRequest(new SourceText(code), ParseGoal.Eval, UnicodeDataHome.Tables, RunnerRegularExpressions.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = holding[^1].IsStrict },
            Context = context ?? InheritedContext.TopLevel,
            BuildSemanticModel = true,
        };
    }

    /// <summary>
    /// Whether an answered semantics vector's result agrees at the parse: a refusal where the expected outcome is
    /// <c>error: SyntaxError</c>, or an acceptance where it is any other outcome, as <see cref="MatchesAtTheParse"/> reads a
    /// syntax vector's. A vector of another area, or a result that is neither, never agrees.
    /// </summary>
    internal static bool MatchesSemanticsAtTheParse(VectorResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Vector["area"] != "semantics" || !Outcome.TryParseAlternatives(result.Expected, out var alternatives, out _, out _))
            return false;

        var refusalExpected = Outcome.AnyIs(alternatives, SyntaxRefused);
        var acceptanceExpected = false;
        foreach (var alternative in alternatives)
            acceptanceExpected |= !Outcome.AnyIs([alternative], SyntaxRefused);

        return result.Rendered switch
        {
            SyntaxRefused => refusalExpected,
            SyntaxAccepted => acceptanceExpected,
            _ => false,
        };
    }
}
