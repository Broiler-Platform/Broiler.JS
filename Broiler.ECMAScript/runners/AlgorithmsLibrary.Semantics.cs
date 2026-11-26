using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Semantics;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The library-level runner's semantics adapter (milestone SJL-15.2, from stage 2; the session's decisions D-js-adapter of
/// 2026-11-24 and D-js-adapter-form of 2026-11-25, and from stage 3 D-js-adapter-groups): the <c>semantics</c> area's
/// vectors (corpus revision 42, <c>conformance/vectors/semantics/</c>) whose row the semantic model reports, and those of
/// the correspondence groups the model decides, parsed by <c>Broiler.ECMAScript.Frontend</c> with
/// <see cref="ParseRequest.BuildSemanticModel"/> on, and judged at the parse. <i>(Revised 2026-11-25, at SJL-15.2 stage 3,
/// when group <c>lb1</c> was answered; it read "(milestone SJL-15.2, from stage 2; the session's decisions D-js-adapter of
/// 2026-11-24 and D-js-adapter-form of 2026-11-25): the <c>semantics</c> area's vectors (corpus revision 42,
/// <c>conformance/vectors/semantics/</c>) whose row the semantic model reports, parsed".)</i>
/// From stage 5c (the session's decision D-js-adapter-caller-scopes) a <c>DirectEval</c> vector's eval code also receives
/// its caller's records, and a direct <c>eval</c> the eval code itself holds is followed. <i>(Added 2026-11-26, at SJL-15.2
/// stage 5c.)</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>Which vectors are answered</b> (<see cref="IsLibrarySemantics"/>): a vector of the area, of one of its two operations,
/// whose identifier names a row of <see cref="SemanticsAnsweredRows"/>, or, from stage 3, whose identifier names a group of
/// <see cref="SemanticsAnsweredGroups"/>, under that group's operation. A vector's row is its identifier's third segment,
/// upper-cased, where it reads as a catalogue row (<c>semantics/{file}/{group}/{n}</c>, the generator's form); the
/// <c>note</c> decides nothing (the corpus's <c>SCHEMA.md</c>). A group that names no row is answered only when it is one of
/// <see cref="SemanticsAnsweredGroups"/>, the correspondence groups whose vectors the model's rows decide, each with the
/// rows it rests on. The lists are the adapter's own and grow stage by stage; they are not read from the Frontend's table,
/// so that the library cannot shape its own expectations, and the Conformance tests hold the rows equal to that table and
/// every group's rows among them. Every other semantics vector is <c>status: not-implemented</c>. <i>(Revised 2026-11-25,
/// at SJL-15.2 stage 3, when group <c>lb1</c> was answered (the session's decision D-js-adapter-groups); it read "whose
/// identifier names a row of <see cref="SemanticsAnsweredRows"/>." and "The groups that name no row are never answered. The
/// list is the adapter's own and grows stage by stage; it is not read from the Frontend's table, so that the library cannot
/// shape its own expectations, and the Conformance tests hold it equal to that table.")</i>
/// </para>
/// <para>
/// <b>How each is parsed.</b> <c>ScriptEvaluation</c>: the vector's one String as a Script, under the library's default
/// request for the goal (<see cref="SyntaxRequest"/>) with the member on. <c>DirectEval</c>, the eval code a direct
/// <c>eval</c> in that Script evaluates, in the context the Script gives it: the Script is parsed with the member on and its
/// tokens retained; the one call <c>eval(</c> a string literal <c>)</c> is found among them, the Script holding exactly one
/// <c>eval</c> name, not after <c>.</c> or <c>?.</c>; its literal's String value is the eval code; and the caller's facts are
/// the model's functions whose node holds the call (a field initialiser's by its initialiser), the innermost giving the
/// strictness and the nearest that is not an arrow the inherited context (PerformEval step 10); from stage 4 the inherited
/// context also carries the private names visible at the call, read from the Script's model as EvalDeclarationInstantiation
/// builds them (steps 4 to 6): the names of the innermost class whose elements hold the call and of the environments
/// around it, innermost first, each once, and an empty list where no class holds the call (the session's decision
/// D-js-adapter-visible-names). The eval code is then parsed
/// under <see cref="ParseGoal.Eval"/> with that context, forced strict where the caller is strict, the member on. Which
/// binding the name <c>eval</c> resolves to is the model's <c>Resolve</c> (stage 5a); whether its value is %eval% is a
/// run-time fact, which the adapter does not decide. A Script the on path refuses, or
/// one with no such call or more than one <c>eval</c>, is not implemented. <i>(Revised 2026-11-25, at SJL-15.2 stage 4,
/// when the visible private names joined the context; it read "the nearest that is not an arrow the inherited context
/// (PerformEval step 10). The eval code is then parsed".)</i> <i>(Revised 2026-11-26, at SJL-15.2 stage 5a, when
/// resolution joined the model; it read "Whether a name <c>eval</c> is the intrinsic one is not resolved: resolution is a
/// later stage's.")</i>
/// From stage 5c the inherited context also carries the caller's records (<see cref="InheritedContext.CallerScopes"/>),
/// the Script model's <see cref="SemanticModel.CallerScopesOf"/> for the call, its candidate in
/// <see cref="SemanticModel.DirectEvalCalls"/> being the one whose callee starts at the <c>eval</c> token, so that a Script's
/// top-level call passes an empty list, never null. And where the eval code is accepted and itself holds exactly one
/// <c>eval</c> name called with one string literal, by the same rule applied to the eval code's tokens, that inner eval code
/// is parsed in the context the eval code's model gives it: the flags of the innermost function that is not an arrow and
/// holds the call, the outer request's where that is the eval code itself, PerformEval's running context there being the
/// outer eval's; the innermost holder's strictness; the private names visible at the call, the outer request's appended,
/// each once; and <c>CallerScopesOf</c> of the inner call, which appends the outer request's records. That repeats while
/// each level holds such a call, every literal shorter than the source holding it, and the innermost parse is the answer,
/// a refusal at any level being one (the session's decision D-js-adapter-caller-scopes). The Script's rule, "the Script
/// holding exactly one <c>eval</c> name", and "more than one <c>eval</c> name, is not implemented" stay the Script's.
/// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
/// </para>
/// <para>
/// <b>What it answers</b> is the syntax area's rendering (<see cref="RenderParse"/>): <c>error: SyntaxError</c> for a
/// refusal and <c>status: accepted</c> for an acceptance. <see cref="MatchesSemanticsAtTheParse"/> reads the expected
/// outcome at the parse, as bundle sjl-13-001's rule reads its syntax vectors': <c>error: SyntaxError</c> as a refusal, any
/// other outcome as an acceptance (the session's reading, D-js-adapter-form, which the owner ruled on 2026-11-25 to keep
/// ("Keep the reading at the parse (Recommended)")). <i>(Revised 2026-11-25, at SJL-15.2 stage 3, after the owner's answer;
/// it read "(the session's reading, D-js-adapter-form, flagged for the owner)".)</i>
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
    /// EE0172, the ContainsArguments rows, eval code's rows from the caller's context, and three rows of expressions; and
    /// stage 3's, the rows of jump targets. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i> Stage 4's, the rows of private
    /// names; group <c>pn1</c> stays unanswered, its vectors naming no row and two of them expecting a value the Script
    /// computes after catching the eval code's refusal (the session's decision D-js-adapter-visible-names).
    /// <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// Stage 5c's, eval code's <c>var</c>s against its caller's records
    /// (EvalDeclarationInstantiation step 3.d); the vectors of EA0006, a row only a run reaches, stay unanswered.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
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

        // Stage 3: jump targets: break or continue with no target, and ContainsUndefinedBreakTarget and ContainsUndefinedContinueTarget of each owner.
        "EE0058", "EE0059", "EE0081", "EE0082", "EE0136", "EE0137", "EE0165", "EE0166", "EE0175", "EE0176",

        // Stage 4: private names: AllPrivateIdentifiersValid of a Script and of a Module, and of eval code against the private names visible at the call.
        "EE0167", "EE0177", "EA0009",

        // Stage 5c: eval code's var against the caller's records, with Annex B on and off.
        "EA0007", "EA0008",
    ];

    /// <summary>
    /// The correspondence groups whose vectors this adapter answers, each under one operation and with the rows of
    /// <see cref="SemanticsAnsweredRows"/> its vectors rest on (SJL-15.2, from stage 3; the session's decision
    /// D-js-adapter-groups): <c>lb1</c>, correspondence row LB1, a <c>continue</c> naming any label of a loop's label set
    /// (Broiler.VM's hand-off H20), decided by EE0082 and EE0166. A group's vectors are answered as a listed row's are; the
    /// Conformance tests hold each group's rows among the answered rows and each of its vectors' notes naming only them.
    /// </summary>
    internal static readonly IReadOnlyList<(string Group, string Operation, string[] Rows)> SemanticsAnsweredGroups =
    [
        // Stage 3: row LB1, a continue to any label of an iteration statement's label set.
        ("lb1", ScriptEvaluationOperation, ["EE0082", "EE0166"]),
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

    /// <summary>A semantics vector's group: its identifier's third segment, as the corpus writes it; null where it has none.</summary>
    internal static string? SemanticsGroup(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (!vector.Id.StartsWith(SemanticsPrefix, StringComparison.Ordinal))
            return null;
        var segments = vector.Id.Split('/');
        return segments.Length < 4 ? null : segments[2];
    }

    /// <summary>
    /// Whether a vector is one this adapter answers: of the area, of one of its operations, and of a row in
    /// <see cref="SemanticsAnsweredRows"/> or a group of <see cref="SemanticsAnsweredGroups"/> under its operation.
    /// <i>(Revised 2026-11-25, at SJL-15.2 stage 3, which added the groups; it read "and of a row in
    /// <see cref="SemanticsAnsweredRows"/>".)</i>
    /// </summary>
    internal static bool IsLibrarySemantics(VectorRecord vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (!vector.Id.StartsWith(SemanticsPrefix, StringComparison.Ordinal) || !IsSemanticsOperation(vector["op"]))
            return false;
        if (SemanticsRow(vector) is { } row)
        {
            foreach (var answered in SemanticsAnsweredRows)
            {
                if (answered == row)
                    return true;
            }

            return false;
        }

        var group = SemanticsGroup(vector);
        foreach (var answered in SemanticsAnsweredGroups)
        {
            if (answered.Group == group && answered.Operation == vector["op"])
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
                return DirectEvalRequest(source) is { } request ? RenderParse(DirectEvalParse(request).Outcome) : EmptyLibrary.NotImplemented;
            default:
                return EmptyLibrary.NotImplemented;
        }
    }

    /// <summary>
    /// The eval request a <c>DirectEval</c> vector's Script makes (see the type's remarks): the eval code of its one direct
    /// <c>eval</c> of a string literal, in the context and the strictness of the code that holds the call; null where the
    /// Script is refused on the on path, or holds no such call, or more than one <c>eval</c> name. From stage 5c its context
    /// also carries the call's records from the Script's model. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    internal static ParseRequest? DirectEvalRequest(string script)
    {
        var parsed = SyntaxParser.Parse(SemanticsScriptRequest(script, retainTokens: true));
        if (parsed.Outcome != ParseOutcome.Accepted || parsed.Model is not { } model)
            return null;
        return EvalRequestAt(model, parsed.Tokens, null);
    }

    /// <summary>
    /// The parse a <c>DirectEval</c> vector is answered by (SJL-15.2 stage 5c; the session's decision
    /// D-js-adapter-caller-scopes): the outer eval request's, and while the accepted eval code holds exactly one <c>eval</c>
    /// of a string literal, the inner eval code's in the context its model gives it; the innermost parse. It takes only a
    /// request that <c>DirectEvalRequest</c> or <c>EvalRequestAt</c> builds: each parse copies the members those set (the
    /// source, the tables, the switches, the context and the limits), so a request with proposals, extensions, a dynamic
    /// function's kind or a meter would be parsed without them.
    /// </summary>
    internal static ParseResult DirectEvalParse(ParseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        for (var current = request; ;)
        {
            var parsed = SyntaxParser.Parse(new ParseRequest(current.Source, ParseGoal.Eval, current.UnicodeTables, current.RegularExpressions)
            {
                Switches = current.Switches,
                Context = current.Context,
                Limits = current.Limits,
                BuildSemanticModel = true,
                RetainTokens = true,
            });
            if (parsed.Outcome != ParseOutcome.Accepted || parsed.Model is not { } model || EvalRequestAt(model, parsed.Tokens, current) is not { } inner)
                return parsed;
            current = inner;
        }
    }

    /// <summary>
    /// The eval request of the one direct <c>eval</c> of a string literal that a unit's tokens hold (see the type's remarks),
    /// from the unit's model; null where they hold none, or more than one <c>eval</c> name. <paramref name="outer"/> is the
    /// request that parsed the unit when it is eval code, whose flags, private names and records the inner request inherits;
    /// null for a Script.
    /// </summary>
    private static ParseRequest? EvalRequestAt(SemanticModel model, IReadOnlyList<Token> tokens, ParseRequest? outer)
    {
        // The one eval name, called with one string literal, not a member's name.
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

        // The candidate the call is: its callee starts at the eval token, the tokens holding it contiguous (SJL-15.2 stage 5c).
        CallExpression? call = null;
        foreach (var candidate in model.DirectEvalCalls)
        {
            if (candidate.Callee.Span.Offset == at)
                call = candidate;
        }

        if (call is null)
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

        // The four flags from the innermost function that is not an arrow; a Script's are all false. (Revised 2026-11-25, at
        // SJL-15.2 stage 4, which builds one context with the visible private names; it chose one of six contexts, a
        // Script's being InheritedContext.TopLevel.)
        var (inFunction, inMethod, inDerivedConstructor, inClassFieldInitializer) = (false, false, false, false);
        for (var i = holding.Count - 1; i >= 0; i--)
        {
            if (holding[i].Kind == FunctionKind.Arrow)
                continue;
            (inFunction, inMethod, inDerivedConstructor, inClassFieldInitializer) = holding[i].Kind switch
            {
                // Eval code holding the call: PerformEval's running context is the outer eval's (SJL-15.2 stage 5c).
                FunctionKind.EvalCode when outer?.Context is { } context => (context.InFunction, context.InMethod, context.InDerivedConstructor, context.InClassFieldInitializer),
                FunctionKind.Normal => (true, false, false, false),
                FunctionKind.Method or FunctionKind.Getter or FunctionKind.Setter or FunctionKind.ClassConstructor or FunctionKind.StaticBlock => (true, true, false, false),
                FunctionKind.DerivedConstructor => (true, true, true, false),
                FunctionKind.FieldInitializer => (true, true, false, true),
                _ => (false, false, false, false),
            };
            break;
        }

        return new ParseRequest(new SourceText(code), ParseGoal.Eval, UnicodeDataHome.Tables, RunnerRegularExpressions.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = holding[^1].IsStrict },
            Context = new InheritedContext
            {
                InFunction = inFunction,
                InMethod = inMethod,
                InDerivedConstructor = inDerivedConstructor,
                InClassFieldInitializer = inClassFieldInitializer,
                VisiblePrivateNames = Appended(VisiblePrivateNames(model, at), outer?.Context?.VisiblePrivateNames),
                CallerScopes = model.CallerScopesOf(call),
            },
            BuildSemanticModel = true,
        };
    }

    /// <summary>
    /// The private names visible at a direct <c>eval</c> at <paramref name="at"/> (SJL-15.2 stage 4; the session's decision
    /// D-js-adapter-visible-names), as EvalDeclarationInstantiation builds them (steps 4 to 6) from the running context's
    /// PrivateEnvironment, which an arrow does not change: the innermost class whose elements hold the call, a computed key, a
    /// field initialiser and a static block among them but not its heritage, its name or its decorators; then that class's
    /// <see cref="SemanticModel.PrivateEnvironmentOf"/> and its outer environments, innermost first, each environment's names
    /// in source order and each name once; an empty list where no class holds the call. Read from the model's public
    /// members alone.
    /// </summary>
    internal static IReadOnlyList<string> VisiblePrivateNames(SemanticModel model, int at)
    {
        ArgumentNullException.ThrowIfNull(model);
        ClassNode? innermost = null;
        foreach (var scope in model.Scopes)
        {
            if (scope.Kind != ScopeKind.ClassBody || scope.Node is not ClassNode @class)
                continue;
            foreach (var element in @class.Elements)
            {
                // The model's scopes are in source order, an outer class's before an inner one's, so the last that holds it is innermost.
                if (element.Span.Offset <= at && at < element.Span.Offset + element.Span.Length)
                {
                    innermost = @class;
                    break;
                }
            }
        }

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var environment = innermost is null ? null : model.PrivateEnvironmentOf(innermost); environment is not null; environment = environment.Outer)
        {
            foreach (var binding in environment.Names)
            {
                if (seen.Add(binding.Name))
                    names.Add(binding.Name);
            }
        }

        return names;
    }

    // The names visible in eval code's classes, then its caller's, each once (EvalDeclarationInstantiation steps 4 to 6;
    // SJL-15.2 stage 5c, for a nested eval).
    private static IReadOnlyList<string> Appended(IReadOnlyList<string> names, IReadOnlyList<string>? outer)
    {
        if (outer is null)
            return names;
        var all = new List<string>(names);
        var seen = new HashSet<string>(names, StringComparer.Ordinal);
        foreach (var name in outer)
        {
            if (seen.Add(name))
                all.Add(name);
        }

        return all;
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
