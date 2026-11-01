using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The library-level runner's frontend adapter (milestone SJL-13.2a; Broiler.VM's JSD-0083, decision D25): it reads a
/// <c>syntax</c> vector's <c>op</c> and <c>in</c>, parses under the library's default request for that goal, and reads
/// <c>error: SyntaxError</c> as a refusal and any other outcome as an acceptance. With no parser yet, every syntax vector
/// is not implemented.
/// </summary>
public class SyntaxAdapterTests
{
    private static IReadOnlyList<VectorRecord> SyntaxVectors { get; } = Home.Corpus.Records.Where(static v => v["area"] == "syntax").ToList();

    [Fact]
    public void Every_syntax_vector_is_not_implemented_until_the_library_parses()
    {
        Assert.NotEmpty(SyntaxVectors);
        var results = LibraryRun.Run(SyntaxVectors, new AlgorithmsLibrary());
        Assert.All(results, static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
        Assert.All(results, static r => Assert.False(AlgorithmsLibrary.MatchesAtTheParse(r)));
    }

    /// <summary>
    /// The two operations are the syntax area's alone, over the whole corpus, so that <see cref="IVectorLibrary.Call"/>,
    /// which sees no area, gives no other area's vector to the adapter and no syntax vector to another area's.
    /// </summary>
    [Fact]
    public void No_other_area_names_a_syntax_operation_and_every_syntax_vector_names_one()
    {
        Assert.All(SyntaxVectors, static v => Assert.True(AlgorithmsLibrary.IsSyntaxOperation(v["op"] ?? string.Empty), v.Id + " names " + v["op"]));
        var colliding = Home.Corpus.Records.Where(static v => v["area"] != "syntax" && AlgorithmsLibrary.IsSyntaxOperation(v["op"] ?? string.Empty)).Select(static v => v.Id).ToList();
        Assert.Empty(colliding);
        Assert.Equal(["ParseScript", "PerformEval"], SyntaxVectors.Select(static v => v["op"]!).Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Each_operation_is_parsed_under_the_library_s_default_request_for_its_goal()
    {
        foreach (var vector in SyntaxVectors)
        {
            var inputs = LibraryRun.Inputs(vector);
            Assert.Single(inputs);
            Assert.Equal(TypedValueKind.String, inputs[0].Kind);

            var request = AlgorithmsLibrary.SyntaxRequest(vector["op"]!, inputs[0].Chars)!;
            var goal = vector["op"] == AlgorithmsLibrary.ParseScriptOperation ? ParseGoal.Script : ParseGoal.Eval;
            Assert.Equal(goal, request.Goal);
            Assert.Equal(inputs[0].Chars, request.Source.Text);
            Assert.Equal(ParseSwitches.For(goal), request.Switches);
            Assert.Equal(goal == ParseGoal.Eval ? InheritedContext.TopLevel : null, request.Context);
            Assert.Equal(ProposalFlags.None, request.Proposals);
            Assert.Equal(LexerExtensions.None, request.Extensions);
            Assert.Same(ParseLimits.Default, request.Limits);
            Assert.Null(request.Meter);
        }

        Assert.Null(AlgorithmsLibrary.SyntaxRequest("NumericLiteral", "1"));
    }

    /// <summary>The rendering of each outcome, shown with a parse that answers it, since the library answers none yet.</summary>
    [Fact]
    public void A_refusal_an_acceptance_and_a_parse_not_made_are_each_rendered()
    {
        var script = SyntaxVectors.First(static v => v["op"] == AlgorithmsLibrary.ParseScriptOperation);
        var inputs = LibraryRun.Inputs(script);
        Assert.Equal(AlgorithmsLibrary.SyntaxRefused, AlgorithmsLibrary.SyntaxArea(script["op"]!, inputs, static _ => ParseOutcome.Refused));
        Assert.Equal(AlgorithmsLibrary.SyntaxAccepted, AlgorithmsLibrary.SyntaxArea(script["op"]!, inputs, static _ => ParseOutcome.Accepted));
        Assert.Equal(EmptyLibrary.NotImplemented, AlgorithmsLibrary.SyntaxArea(script["op"]!, inputs, static request => SyntaxParser.Parse(request).Outcome));
        Assert.Equal(EmptyLibrary.NotImplemented, AlgorithmsLibrary.SyntaxArea("NumericLiteral", inputs, static _ => throw new InvalidOperationException("not a syntax operation")));
    }

    /// <summary>
    /// The comparison at the parse (the home's rule, part B), over every syntax vector: an acceptance agrees exactly where the
    /// expected outcome is not <c>error: SyntaxError</c>, and a refusal exactly where it is. The control is the vector's own
    /// opposite: each vector agrees with one rendering and disagrees with the other.
    /// </summary>
    [Fact]
    public void A_refusal_agrees_with_a_SyntaxError_and_an_acceptance_with_any_other_outcome()
    {
        var refusals = 0;
        foreach (var vector in SyntaxVectors)
        {
            var expected = LibraryRun.Expected(vector);
            var refused = expected == AlgorithmsLibrary.SyntaxRefused;
            refusals += refused ? 1 : 0;
            var asRefused = new VectorResult(vector, expected, AlgorithmsLibrary.SyntaxRefused, refused ? Verdict.Match : Verdict.Difference);
            var asAccepted = new VectorResult(vector, expected, AlgorithmsLibrary.SyntaxAccepted, Verdict.Difference);
            Assert.True(AlgorithmsLibrary.MatchesAtTheParse(asRefused) == refused, vector.Id);
            Assert.True(AlgorithmsLibrary.MatchesAtTheParse(asAccepted) == !refused, vector.Id);
            Assert.False(AlgorithmsLibrary.MatchesAtTheParse(new VectorResult(vector, expected, EmptyLibrary.NotImplemented, Verdict.NotImplemented)), vector.Id);
        }

        Assert.InRange(refusals, 1, SyntaxVectors.Count - 1);
        var other = Home.Corpus.Records.First(static v => v["area"] == "numbers");
        Assert.False(AlgorithmsLibrary.MatchesAtTheParse(new VectorResult(other, other.Out, AlgorithmsLibrary.SyntaxAccepted, Verdict.Difference)));
    }
}
