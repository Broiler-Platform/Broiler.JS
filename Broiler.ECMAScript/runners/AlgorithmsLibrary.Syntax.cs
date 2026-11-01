using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Syntax;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The library-level runner's frontend adapter (milestone SJL-13.2a; Broiler.VM's JSD-0083, decision D25; plan stage 4a):
/// the <c>syntax</c> area's vectors (corpus revision 30, <c>conformance/vectors/syntax/</c>) parsed by
/// <c>Broiler.ECMAScript.Frontend</c> under the library's default request for the goal each vector's <c>op</c> names.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it reads.</b> A syntax vector's <c>op</c> and <c>in</c> alone: <c>ParseScript</c> parses <c>in</c>'s one String
/// under the <see cref="ParseGoal.Script"/> goal, and <c>PerformEval</c> as the eval code of a top-level direct eval, the
/// <see cref="ParseGoal.Eval"/> goal with <see cref="InheritedContext.TopLevel"/> (the corpus's <c>SCHEMA.md</c> and README,
/// the syntax area's row). Every other field of the request is the library's default for the goal
/// (<see cref="ParseRequest"/>'s remarks). The vector's <c>script</c> is the engine-level runners', and is not read here.
/// </para>
/// <para>
/// <b>What it answers.</b> A refusal is rendered <c>error: SyntaxError</c>; an acceptance <c>status: accepted</c>, a
/// library-level status (the schema's section 4), since a parse knows that a source is accepted and not what it completes
/// with; and a parse the library cannot make yet <c>status: not-implemented</c>. <see cref="MatchesAtTheParse"/> then reads a
/// vector's expected outcome as the home's rule reads it (part B): <c>error: SyntaxError</c> as a refusal, any other outcome
/// as an acceptance. Until stage 4c gives the library a parser, every syntax vector is <c>status: not-implemented</c>.
/// </para>
/// <para>
/// <b>The operation names collide with no other area's</b>: no vector outside <c>syntax/</c> names <c>ParseScript</c> or
/// <c>PerformEval</c>, which the Conformance tests check over the whole corpus, and this adapter is the last the chain asks,
/// after the Intl area's. <see cref="IVectorLibrary.Call"/> sees no area, so the check is what keeps the two names the
/// syntax area's.
/// </para>
/// <para>
/// <b>Its providers are placeholders until the stages that read them.</b> The home holds neither engine's tables nor
/// either engine's validator. <see cref="RunnerUnicodeTables"/> and <see cref="RunnerRegularExpressions"/> carry nothing
/// and throw if read, so that the stage that first reads one (4b for the tables, 4d for the validator) must give the runner
/// a real provider rather than have a parse silently read an empty one.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>A syntax vector's operation: the source parsed as a Script.</summary>
    internal const string ParseScriptOperation = "ParseScript";

    /// <summary>A syntax vector's operation: the source parsed as the eval code of a top-level direct eval.</summary>
    internal const string PerformEvalOperation = "PerformEval";

    /// <summary>The library-level status of an accepted source: accepted at the parse, its completion value unknown there.</summary>
    internal const string SyntaxAccepted = "status: accepted";

    /// <summary>A refused source, as the corpus writes a refusal.</summary>
    internal const string SyntaxRefused = "error: SyntaxError";

    /// <summary>Whether an operation is one of the syntax area's.</summary>
    internal static bool IsSyntaxOperation(string operation) => operation is ParseScriptOperation or PerformEvalOperation;

    /// <summary>The library's default request for a syntax operation's goal, over the source the vector's <c>in</c> holds; null for any other operation.</summary>
    internal static ParseRequest? SyntaxRequest(string operation, string source) => operation switch
    {
        ParseScriptOperation => new ParseRequest(new SourceText(source), ParseGoal.Script, RunnerUnicodeTables.Instance, RunnerRegularExpressions.Instance),
        PerformEvalOperation => new ParseRequest(new SourceText(source), ParseGoal.Eval, RunnerUnicodeTables.Instance, RunnerRegularExpressions.Instance),
        _ => null,
    };

    /// <summary>A parse's outcome, rendered in the corpus notation (see the type's remarks).</summary>
    internal static string RenderParse(ParseOutcome outcome) => outcome switch
    {
        ParseOutcome.Accepted => SyntaxAccepted,
        ParseOutcome.Refused => SyntaxRefused,
        _ => EmptyLibrary.NotImplemented,
    };

    /// <summary>
    /// Whether a syntax vector's result agrees at the parse (the home's rule, part B): a refusal where the expected outcome
    /// is <c>error: SyntaxError</c>, or an acceptance where it is any other outcome. A vector of another area, or a result
    /// that is neither a refusal nor an acceptance, never agrees.
    /// </summary>
    internal static bool MatchesAtTheParse(VectorResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Vector["area"] != "syntax" || !Outcome.TryParseAlternatives(result.Expected, out var alternatives, out _, out _))
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

    /// <summary>The syntax area, the last the chain asks (milestone SJL-13.2a).</summary>
    private static string SyntaxArea(string operation, IReadOnlyList<TypedValue> inputs) =>
        SyntaxArea(operation, inputs, static request => SyntaxParser.Parse(request).Outcome);

    /// <summary>The syntax area over a given parse's outcome, so that a test can show the rendering of each outcome before the library parses.</summary>
    internal static string SyntaxArea(string operation, IReadOnlyList<TypedValue> inputs, Func<ParseRequest, ParseOutcome> parse)
    {
        ArgumentNullException.ThrowIfNull(parse);
        if (!IsSyntaxOperation(operation) || inputs.Count != 1 || inputs[0].Kind != TypedValueKind.String)
            return EmptyLibrary.NotImplemented;

        return RenderParse(parse(SyntaxRequest(operation, inputs[0].Chars)!));
    }

    /// <summary>
    /// The runner's Unicode provider at stage 4a: no table. Nothing reads it before stage 4b's tokenizer, which gives the
    /// runner the tables it reads; until then a read throws, so that no parse reads an empty table unnoticed.
    /// </summary>
    internal sealed class RunnerUnicodeTables : IUnicodeTables
    {
        private RunnerUnicodeTables()
        {
        }

        /// <summary>The one instance.</summary>
        internal static RunnerUnicodeTables Instance { get; } = new();

        /// <inheritdoc/>
        public string UnicodeVersion => throw Unread("its Unicode version");

        /// <inheritdoc/>
        public ReadOnlySpan<byte> Table(UnicodeTable table) => throw Unread("the table " + table);

        private static NotSupportedException Unread(string what) => new(
            "The library-level runner's stage-4a Unicode provider has no table, and " + what + " was read: the stage whose code "
            + "reads the provider (4b, the tokenizer) gives the runner its tables.");
    }

    /// <summary>
    /// The runner's regular-expression provider at stage 4a: no validator. The home holds neither engine's validator, and the
    /// library asks for none before stage 4d, which gives the runner its provider (for the corpus runner, one that answers from
    /// the classification, decision D26); until then a call throws.
    /// </summary>
    internal sealed class RunnerRegularExpressions : IRegularExpressionValidator
    {
        private RunnerRegularExpressions()
        {
        }

        /// <summary>The one instance.</summary>
        internal static RunnerRegularExpressions Instance { get; } = new();

        /// <inheritdoc/>
        public RegularExpressionVerdict Validate(string pattern, string flags) => throw new NotSupportedException(
            "The library-level runner's stage-4a regular-expression provider has no validator: the stage whose code asks it "
            + "(4d, the parse-time early errors) gives the runner its provider.");
    }
}
