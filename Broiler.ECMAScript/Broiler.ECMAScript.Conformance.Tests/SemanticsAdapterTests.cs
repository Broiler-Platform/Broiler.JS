using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The library-level runner's semantics adapter (SJL-15.2, from stage 2; the session's decisions D-js-adapter and
/// D-js-adapter-form): it answers the <c>semantics</c> vectors whose identifier names a row the semantic model reports,
/// parses with <see cref="ParseRequest.BuildSemanticModel"/> on, and is read at the parse, <c>error: SyntaxError</c> as a
/// refusal and any other outcome as an acceptance. Every other semantics vector is not implemented.
/// </summary>
public class SemanticsAdapterTests
{
    private static IReadOnlyList<VectorRecord> SemanticsVectors { get; } = Home.Corpus.Records.Where(static v => v["area"] == "semantics").ToList();

    private static IReadOnlyList<VectorRecord> Answered { get; } = SemanticsVectors.Where(AlgorithmsLibrary.IsLibrarySemantics).ToList();

    /// <summary>Every answered vector is refused or accepted and agrees at the parse; both renderings and both operations occur.</summary>
    [Fact]
    public void Every_answered_semantics_vector_agrees_at_the_parse()
    {
        Assert.NotEmpty(Answered);
        var results = LibraryRun.Run(Answered, new AlgorithmsLibrary());
        Assert.All(results, static r => Assert.True(r.Rendered is AlgorithmsLibrary.SyntaxRefused or AlgorithmsLibrary.SyntaxAccepted, r.Vector.Id + " rendered " + r.Rendered));
        Assert.All(results, static r => Assert.True(AlgorithmsLibrary.MatchesSemanticsAtTheParse(r), r.Vector.Id + " rendered " + r.Rendered + " and expects " + r.Expected));
        foreach (var operation in new[] { AlgorithmsLibrary.ScriptEvaluationOperation, AlgorithmsLibrary.DirectEvalOperation })
        {
            Assert.Contains(results, r => r.Vector["op"] == operation && r.Rendered == AlgorithmsLibrary.SyntaxRefused);
            Assert.Contains(results, r => r.Vector["op"] == operation && r.Rendered == AlgorithmsLibrary.SyntaxAccepted);
        }
    }

    /// <summary>
    /// Every semantics vector whose row is not in the adapter's list is not implemented, and no answered one is; the
    /// operation-only call answers neither operation.
    /// </summary>
    [Fact]
    public void Every_other_semantics_vector_is_not_implemented()
    {
        var library = new AlgorithmsLibrary();
        var others = SemanticsVectors.Where(static v => !AlgorithmsLibrary.IsLibrarySemantics(v)).ToList();
        Assert.NotEmpty(others);
        Assert.All(LibraryRun.Run(others, library), static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
        Assert.All(LibraryRun.Run(Answered, library), static r => Assert.NotEqual(Verdict.NotImplemented, r.Verdict));

        // A vector's row is its identifier's; a group that names none is never answered.
        Assert.All(others, static v => Assert.DoesNotContain(AlgorithmsLibrary.SemanticsRow(v) ?? "-", AlgorithmsLibrary.SemanticsAnsweredRows));
        Assert.Contains(others, static v => AlgorithmsLibrary.SemanticsRow(v) is null);
        var input = LibraryRun.Inputs(Answered[0]);
        Assert.Equal(EmptyLibrary.NotImplemented, ((IVectorLibrary)library).Call(AlgorithmsLibrary.ScriptEvaluationOperation, input));
        Assert.Equal(EmptyLibrary.NotImplemented, ((IVectorLibrary)library).Call(AlgorithmsLibrary.DirectEvalOperation, input));
    }

    /// <summary>The control is each answered vector's own opposite; not-implemented and another area's vector never agree.</summary>
    [Fact]
    public void A_refusal_agrees_with_a_SyntaxError_and_an_acceptance_with_any_other_outcome()
    {
        var refusals = 0;
        foreach (var vector in Answered)
        {
            var expected = LibraryRun.Expected(vector);
            var refused = expected == AlgorithmsLibrary.SyntaxRefused;
            refusals += refused ? 1 : 0;
            var asRefused = new VectorResult(vector, expected, AlgorithmsLibrary.SyntaxRefused, refused ? Verdict.Match : Verdict.Difference);
            var asAccepted = new VectorResult(vector, expected, AlgorithmsLibrary.SyntaxAccepted, Verdict.Difference);
            Assert.True(AlgorithmsLibrary.MatchesSemanticsAtTheParse(asRefused) == refused, vector.Id);
            Assert.True(AlgorithmsLibrary.MatchesSemanticsAtTheParse(asAccepted) == !refused, vector.Id);
            Assert.False(AlgorithmsLibrary.MatchesSemanticsAtTheParse(new VectorResult(vector, expected, EmptyLibrary.NotImplemented, Verdict.NotImplemented)), vector.Id);
        }

        Assert.InRange(refusals, 1, Answered.Count - 1);
        var syntax = Home.Corpus.Records.First(static v => v["area"] == "syntax");
        Assert.False(AlgorithmsLibrary.MatchesSemanticsAtTheParse(new VectorResult(syntax, syntax.Out, AlgorithmsLibrary.SyntaxRefused, Verdict.Match)));
        var number = Home.Corpus.Records.First(static v => v["area"] == "numbers");
        Assert.False(AlgorithmsLibrary.MatchesSemanticsAtTheParse(new VectorResult(number, number.Out, AlgorithmsLibrary.SyntaxAccepted, Verdict.Difference)));
    }

    /// <summary>The two operations are the semantics area's alone, and every semantics vector names one.</summary>
    [Fact]
    public void No_other_area_names_a_semantics_operation_and_every_semantics_vector_names_one()
    {
        Assert.All(SemanticsVectors, static v => Assert.True(AlgorithmsLibrary.IsSemanticsOperation(v["op"]), v.Id + " names " + v["op"]));
        Assert.Empty(Home.Corpus.Records.Where(static v => v["area"] != "semantics" && AlgorithmsLibrary.IsSemanticsOperation(v["op"])).Select(static v => v.Id));
        Assert.All(SemanticsVectors, static v => Assert.StartsWith(AlgorithmsLibrary.SemanticsPrefix, v.Id, StringComparison.Ordinal));
    }

    /// <summary>The Script request is the library's default for its goal plus the member; each DirectEval vector's context by id.</summary>
    [Fact]
    public void The_Script_request_is_the_default_plus_the_member_and_the_eval_code_inherits_its_caller_s_context()
    {
        foreach (var vector in Answered.Where(static v => v["op"] == AlgorithmsLibrary.ScriptEvaluationOperation))
        {
            var source = LibraryRun.Inputs(vector)[0].Chars;
            var request = AlgorithmsLibrary.SemanticsScriptRequest(source);
            Assert.Equal(ParseGoal.Script, request.Goal);
            Assert.Equal(source, request.Source.Text);
            Assert.Equal(ParseSwitches.For(ParseGoal.Script), request.Switches);
            Assert.Null(request.Context);
            Assert.Equal(ProposalFlags.None, request.Proposals);
            Assert.Equal(LexerExtensions.None, request.Extensions);
            Assert.Same(ParseLimits.Default, request.Limits);
            Assert.Null(request.Meter);
            Assert.False(request.RetainTokens);
            Assert.True(request.BuildSemanticModel);
        }

        var method = new InheritedContext { InFunction = true, InMethod = true };
        var derived = new InheritedContext { InFunction = true, InMethod = true, InDerivedConstructor = true };
        var field = new InheritedContext { InFunction = true, InMethod = true, InClassFieldInitializer = true };
        var expected = new Dictionary<string, (InheritedContext Context, bool Strict)>(StringComparer.Ordinal)
        {
            ["semantics/super/ee0162/0003"] = (method, false),
            ["semantics/super/ea0003/0001"] = (method, false),
            ["semantics/super/ea0003/0002"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ea0003/0003"] = (new InheritedContext { InFunction = true }, false),
            ["semantics/super/ea0004/0001"] = (derived, true),
            ["semantics/super/ea0004/0002"] = (derived, true),
            ["semantics/super/ea0004/0003"] = (method, false),
            ["semantics/super/ea0004/0004"] = (method, true),
            ["semantics/arguments/ea0005/0001"] = (field, true),
            ["semantics/arguments/ea0005/0002"] = (field, true),
            ["semantics/arguments/ea0005/0003"] = (field, true),
        };
        foreach (var (id, (context, strict)) in expected)
        {
            var vector = Answered.Single(v => v.Id == id);
            Assert.Equal(AlgorithmsLibrary.DirectEvalOperation, vector["op"]);
            var request = AlgorithmsLibrary.DirectEvalRequest(LibraryRun.Inputs(vector)[0].Chars)!;
            Assert.NotNull(request);
            Assert.Equal(ParseGoal.Eval, request.Goal);
            Assert.True(request.BuildSemanticModel);
            Assert.Equal(ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = strict }, request.Switches);
            var actual = request.Context!;
            Assert.True(
                (context.InFunction, context.InMethod, context.InDerivedConstructor, context.InClassFieldInitializer)
                    == (actual.InFunction, actual.InMethod, actual.InDerivedConstructor, actual.InClassFieldInitializer),
                id + ": " + (actual.InFunction, actual.InMethod, actual.InDerivedConstructor, actual.InClassFieldInitializer));
            Assert.Null(actual.VisiblePrivateNames);
        }

        // ea0005/0001: the function is inside the eval code, so the eval code's own function resets ContainsArguments.
        Assert.Contains("function", AlgorithmsLibrary.DirectEvalRequest(LibraryRun.Inputs(Answered.Single(static v => v.Id == "semantics/arguments/ea0005/0001"))[0].Chars)!.Source.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every answered DirectEval vector's Script is accepted on the on path (the generator's invariant) and holds exactly one
    /// <c>eval</c> of a string literal, so that none is answered not implemented.
    /// </summary>
    [Fact]
    public void Every_answered_DirectEval_Script_is_accepted_and_holds_one_eval_of_a_string_literal()
    {
        var direct = Answered.Where(static v => v["op"] == AlgorithmsLibrary.DirectEvalOperation).ToList();
        Assert.NotEmpty(direct);
        foreach (var vector in direct)
        {
            var source = LibraryRun.Inputs(vector)[0].Chars;
            Assert.Equal(ParseOutcome.Accepted, SyntaxParser.Parse(AlgorithmsLibrary.SemanticsScriptRequest(source)).Outcome);
            Assert.NotNull(AlgorithmsLibrary.DirectEvalRequest(source));
        }

        // The limits: a second eval name, a member named eval, or a refused Script is not implemented.
        Assert.Null(AlgorithmsLibrary.DirectEvalRequest("eval('1'); eval('2');"));
        Assert.Null(AlgorithmsLibrary.DirectEvalRequest("o.eval('1');"));
        Assert.Null(AlgorithmsLibrary.DirectEvalRequest("eval(x);"));
        Assert.Null(AlgorithmsLibrary.DirectEvalRequest("super.x; eval('1');"));
    }

    /// <summary>
    /// The adapter's own list equals the Frontend's table of the model's rows, read by reflection, so that dropping a row
    /// from one shows; the adapter never reads the table itself.
    /// </summary>
    [Fact]
    public void The_adapter_s_rows_are_the_Frontend_s_SemanticRows()
    {
        var table = typeof(SyntaxParser).Assembly.GetType("Broiler.ECMAScript.Syntax.SemanticRows", throwOnError: true)!;
        var fields = table.GetFields(BindingFlags.Static | BindingFlags.NonPublic).Select(static f => f.Name).Order(StringComparer.Ordinal).ToList();
        Assert.NotEmpty(fields);
        Assert.Equal(fields, AlgorithmsLibrary.SemanticsAnsweredRows.Order(StringComparer.Ordinal));
        Assert.Equal(AlgorithmsLibrary.SemanticsAnsweredRows.Count, AlgorithmsLibrary.SemanticsAnsweredRows.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>The counts, by operation, of the vectors answered and not implemented (a record for the stage's report).</summary>
    [Fact]
    public void The_answered_vectors_by_operation()
    {
        var byOperation = SemanticsVectors.GroupBy(static v => (v["op"], AlgorithmsLibrary.IsLibrarySemantics(v))).ToDictionary(static g => g.Key, static g => g.Count());
        Assert.Equal(SemanticsVectors.Count, byOperation.Values.Sum());
        Assert.True(byOperation.GetValueOrDefault((AlgorithmsLibrary.ScriptEvaluationOperation, true)) > 0);
        Assert.True(byOperation.GetValueOrDefault((AlgorithmsLibrary.DirectEvalOperation, true)) > 0);
    }
}
