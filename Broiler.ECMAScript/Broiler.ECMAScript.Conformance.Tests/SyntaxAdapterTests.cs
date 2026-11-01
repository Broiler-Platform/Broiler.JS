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
/// <c>error: SyntaxError</c> as a refusal and any other outcome as an acceptance. From stage 4c the parser answers every
/// syntax vector: refused where the tokenizer or the grammar refuses its source, accepted everywhere else, the vectors only a
/// static-semantic early error refuses among them until stage 4d.
/// </summary>
public class SyntaxAdapterTests
{
    private static IReadOnlyList<VectorRecord> SyntaxVectors { get; } = Home.Corpus.Records.Where(static v => v["area"] == "syntax").ToList();

    /// <summary>
    /// Stage 4c's answer (the 4b test it replaces asserted that no vector was accepted before the parser existed, and that
    /// every vector the tokenizer did not refuse was not implemented): every syntax vector is accepted or refused, none is
    /// not implemented; every refusal agrees at the parse, which shows the refusals are the specification's.
    /// </summary>
    [Fact]
    public void The_parser_answers_every_syntax_vector_and_refuses_none_that_expects_an_acceptance()
    {
        Assert.NotEmpty(SyntaxVectors);
        var results = LibraryRun.Run(SyntaxVectors, new AlgorithmsLibrary());
        Assert.All(results, static r => Assert.True(r.Rendered is AlgorithmsLibrary.SyntaxRefused or AlgorithmsLibrary.SyntaxAccepted, r.Vector.Id + " rendered " + r.Rendered));
        Assert.All(results.Where(static r => r.Rendered == AlgorithmsLibrary.SyntaxRefused), static r => Assert.True(AlgorithmsLibrary.MatchesAtTheParse(r), r.Vector.Id + " is refused and expects " + r.Expected));
        Assert.Contains(results, static r => r.Rendered == AlgorithmsLibrary.SyntaxRefused);
        Assert.Contains(results, static r => r.Rendered == AlgorithmsLibrary.SyntaxAccepted && AlgorithmsLibrary.MatchesAtTheParse(r));
    }

    /// <summary>
    /// The vectors stage 4b's tokenizer refuses, at corpus revision 30, by id; the parser of stage 4c reaches each of them
    /// and refuses it with the tokenizer's diagnostic.
    /// </summary>
    private static readonly string[] TokenizerRefusals =
    [
        "syntax/lexical/ee0001/0002", "syntax/lexical/ee0002/0002", "syntax/lexical/escapes/0001",
        "syntax/lexical/escapes/0002", "syntax/lexical/escapes/0003", "syntax/lexical/escapes/0004",
        "syntax/lexical/escapes/0005", "syntax/lexical/escapes/0006", "syntax/lexical/escapes/0007",
        "syntax/lexical/ex0001/0002", "syntax/lexical/ex0001/0003", "syntax/lexical/ex0002/0002",
        "syntax/lexical/ex0002/0003", "syntax/lexical/ex0003/0002", "syntax/lexical/ex0004/0002",
        "syntax/lexical/ex0005/0002", "syntax/lexical/ex0005/0003", "syntax/lexical/ex0006/0002",
        "syntax/lexical/ex0007/0002", "syntax/lexical/ex0025/0002", "syntax/lexical/ex0025/0003",
        "syntax/lexical/hashbang/0003", "syntax/lexical/hashbang/0004", "syntax/lexical/hashbang/0005",
        "syntax/lexical/identifiers/0009", "syntax/lexical/identifiers/0010", "syntax/lexical/identifiers/0012",
        "syntax/lexical/identifiers/0016", "syntax/lexical/line-terminators/0009",
        "syntax/lexical/line-terminators/0010", "syntax/lexical/line-terminators/0015",
        "syntax/lexical/line-terminators/0016", "syntax/lexical/m-suffix/0001", "syntax/lexical/m-suffix/0002",
        "syntax/lexical/numeric-literals/0009", "syntax/lexical/numeric-literals/0010",
        "syntax/lexical/numeric-literals/0011", "syntax/lexical/numeric-literals/0012",
        "syntax/lexical/numeric-literals/0013", "syntax/lexical/numeric-literals/0014",
        "syntax/lexical/numeric-literals/0015", "syntax/lexical/numeric-literals/0016",
        "syntax/lexical/numeric-literals/0017", "syntax/lexical/numeric-literals/0018",
        "syntax/lexical/numeric-literals/0019", "syntax/lexical/numeric-literals/0024",
        "syntax/lexical/numeric-literals/0025", "syntax/lexical/numeric-literals/0026",
        "syntax/lexical/numeric-literals/0027", "syntax/lexical/numeric-literals/0028",
        "syntax/lexical/numeric-literals/0032", "syntax/lexical/numeric-literals/0033",
        "syntax/lexical/numeric-literals/0034", "syntax/lexical/numeric-literals/0035", "syntax/lexical/t6/0001",
    ];

    /// <summary>
    /// The vectors stage 4c's grammar refuses besides the tokenizer's, at corpus revision 30, by id: the probe records the
    /// syntax area and does not judge it, so these lists are what make a lost refusal, or a new one, fail. A stage that
    /// changes what the library refuses (the early errors, stage 4d), or a corpus revision that adds or retires a syntax
    /// vector, rewrites them.
    /// </summary>
    private static readonly string[] GrammarRefusals =
    [
        "syntax/annex-b/eg0003/0002", "syntax/annex-b/eg0005/0002", "syntax/annex-b/eg0007/0002",
        "syntax/expressions/ee0017/0002", "syntax/expressions/ee0025/0002", "syntax/expressions/ee0032/0002",
        "syntax/expressions/ee0037/0002", "syntax/expressions/ee0037/0003", "syntax/expressions/ee0038/0002",
        "syntax/functions-and-classes/ee0087/0002", "syntax/functions-and-classes/ee0155/0002",
        "syntax/functions-and-classes/ex0033/0002", "syntax/functions-and-classes/ex0033/0003",
        "syntax/lexical/asi/0004", "syntax/lexical/asi/0008", "syntax/lexical/asi/0009",
        "syntax/lexical/asi/0010", "syntax/lexical/asi/0013", "syntax/lexical/identifiers/0013",
        "syntax/lexical/identifiers/0014", "syntax/lexical/t6/0002", "syntax/scripts/ea0001/0002",
        "syntax/scripts/ex0008/0002", "syntax/scripts/ex0009/0002", "syntax/scripts/ex0010/0002",
        "syntax/scripts/ex0010/0003", "syntax/scripts/ex0032/0002", "syntax/scripts/grammar/0001",
        "syntax/scripts/grammar/0002", "syntax/scripts/grammar/0003", "syntax/scripts/grammar/0004",
        "syntax/scripts/grammar/0005", "syntax/scripts/grammar/0006", "syntax/scripts/grammar/0007",
        "syntax/scripts/grammar/0008", "syntax/statements/ee0053/0002",
    ];

    /// <summary>
    /// The vectors that expect a refusal and that the library accepts at stage 4c, by id: each is refused only by a
    /// static-semantic early error (or the regular-expression provider's answer), which stage 4d reports.
    /// </summary>
    private static readonly string[] EarlyErrorAcceptances =
    [
        "syntax/annex-b/eg0004/0002", "syntax/annex-b/eg0006/0002", "syntax/annex-b/eg0008/0002",
        "syntax/expressions/ee0005/0002", "syntax/expressions/ee0006/0002", "syntax/expressions/ee0008/0002",
        "syntax/expressions/ee0009/0002", "syntax/expressions/ee0010/0002", "syntax/expressions/ee0011/0002",
        "syntax/expressions/ee0012/0002", "syntax/expressions/ee0012/0003", "syntax/expressions/ee0014/0002",
        "syntax/expressions/ee0016/0002", "syntax/expressions/ee0026/0002", "syntax/expressions/ee0027/0001",
        "syntax/expressions/ee0028/0002", "syntax/expressions/ee0029/0002", "syntax/expressions/ee0030/0002",
        "syntax/expressions/ee0030/0003", "syntax/expressions/ee0031/0002", "syntax/expressions/ee0033/0002",
        "syntax/expressions/ee0034/0002", "syntax/expressions/ee0035/0002", "syntax/expressions/ee0036/0002",
        "syntax/expressions/ee0039/0002", "syntax/expressions/ee0039/0003", "syntax/expressions/p15b/0001",
        "syntax/expressions/p15b/0002", "syntax/functions-and-classes/ee0068/0002",
        "syntax/functions-and-classes/ee0069/0002", "syntax/functions-and-classes/ee0070/0002",
        "syntax/functions-and-classes/ee0071/0002", "syntax/functions-and-classes/ee0072/0002",
        "syntax/functions-and-classes/ee0073/0002", "syntax/functions-and-classes/ee0078/0002",
        "syntax/functions-and-classes/ee0079/0002", "syntax/functions-and-classes/ee0080/0002",
        "syntax/functions-and-classes/ee0083/0002", "syntax/functions-and-classes/ee0084/0002",
        "syntax/functions-and-classes/ee0085/0002", "syntax/functions-and-classes/ee0086/0002",
        "syntax/functions-and-classes/ee0088/0002", "syntax/functions-and-classes/ee0089/0002",
        "syntax/functions-and-classes/ee0090/0002", "syntax/functions-and-classes/ee0091/0002",
        "syntax/functions-and-classes/ee0092/0002", "syntax/functions-and-classes/ee0094/0002",
        "syntax/functions-and-classes/ee0095/0002", "syntax/functions-and-classes/ee0096/0002",
        "syntax/functions-and-classes/ee0097/0002", "syntax/functions-and-classes/ee0098/0002",
        "syntax/functions-and-classes/ee0099/0002", "syntax/functions-and-classes/ee0100/0002",
        "syntax/functions-and-classes/ee0101/0002", "syntax/functions-and-classes/ee0107/0002",
        "syntax/functions-and-classes/ee0108/0002", "syntax/functions-and-classes/ee0109/0002",
        "syntax/functions-and-classes/ee0110/0002", "syntax/functions-and-classes/ee0111/0002",
        "syntax/functions-and-classes/ee0112/0002", "syntax/functions-and-classes/ee0113/0002",
        "syntax/functions-and-classes/ee0114/0002", "syntax/functions-and-classes/ee0115/0002",
        "syntax/functions-and-classes/ee0116/0002", "syntax/functions-and-classes/ee0122/0002",
        "syntax/functions-and-classes/ee0123/0002", "syntax/functions-and-classes/ee0125/0002",
        "syntax/functions-and-classes/ee0127/0002", "syntax/functions-and-classes/ee0128/0002",
        "syntax/functions-and-classes/ee0129/0002", "syntax/functions-and-classes/ee0132/0002",
        "syntax/functions-and-classes/ee0133/0002", "syntax/functions-and-classes/ee0134/0002",
        "syntax/functions-and-classes/ee0135/0002", "syntax/functions-and-classes/ee0140/0002",
        "syntax/functions-and-classes/ee0141/0002", "syntax/functions-and-classes/ee0143/0002",
        "syntax/functions-and-classes/ee0144/0002", "syntax/functions-and-classes/ee0145/0002",
        "syntax/functions-and-classes/ee0146/0002", "syntax/functions-and-classes/ee0147/0002",
        "syntax/functions-and-classes/ee0148/0002", "syntax/functions-and-classes/ee0149/0002",
        "syntax/functions-and-classes/ee0154/0002", "syntax/functions-and-classes/ee0156/0002",
        "syntax/functions-and-classes/ee0157/0002", "syntax/functions-and-classes/ee0158/0002",
        "syntax/functions-and-classes/ee0159/0002", "syntax/lexical/ee0003/0002",
        "syntax/lexical/ee0004/0002", "syntax/lexical/ee0004/0004", "syntax/lexical/ee0004/0005",
        "syntax/lexical/ee0020/0002", "syntax/lexical/ee0022/0002", "syntax/lexical/ee0023/0002",
        "syntax/lexical/ee0024/0002", "syntax/lexical/html-comments/0007", "syntax/lexical/identifiers/0008",
        "syntax/lexical/t16/0001", "syntax/lexical/t16/0002", "syntax/lexical/t16/0003",
        "syntax/lexical/t16/0004", "syntax/lexical/t16/0005", "syntax/lexical/templates/0003",
        "syntax/lexical/templates/0005", "syntax/scripts/ea0002/0002", "syntax/scripts/ee0160/0002",
        "syntax/scripts/ee0161/0002", "syntax/scripts/ee0163/0002", "syntax/scripts/ee0163/0003",
        "syntax/scripts/ee0164/0002", "syntax/scripts/ep0008/0002", "syntax/scripts/ep0008/0003",
        "syntax/statements/ee0040/0002", "syntax/statements/ee0041/0002", "syntax/statements/ee0042/0002",
        "syntax/statements/ee0043/0002", "syntax/statements/ee0044/0002", "syntax/statements/ee0045/0002",
        "syntax/statements/ee0046/0002", "syntax/statements/ee0047/0002", "syntax/statements/ee0048/0002",
        "syntax/statements/ee0049/0002", "syntax/statements/ee0050/0002", "syntax/statements/ee0051/0002",
        "syntax/statements/ee0052/0002", "syntax/statements/ee0055/0002", "syntax/statements/ee0056/0002",
        "syntax/statements/ee0057/0002", "syntax/statements/ee0060/0002", "syntax/statements/ee0061/0002",
        "syntax/statements/ee0062/0002", "syntax/statements/ee0063/0002", "syntax/statements/ee0064/0002",
        "syntax/statements/ee0065/0002", "syntax/statements/ee0066/0002", "syntax/statements/ee0067/0002",
        "syntax/statements/ep0006/0001", "syntax/statements/ep0007/0001",
    ];

    [Fact]
    public void The_library_refuses_exactly_the_vectors_stage_4c_names()
    {
        var refused = LibraryRun.Run(SyntaxVectors, new AlgorithmsLibrary())
            .Where(static r => r.Rendered == AlgorithmsLibrary.SyntaxRefused)
            .Select(static r => r.Vector.Id)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(TokenizerRefusals.Concat(GrammarRefusals).Order(StringComparer.Ordinal).ToList(), refused);
    }

    [Fact]
    public void The_library_accepts_exactly_the_refused_vectors_stage_4c_leaves_to_stage_4d()
    {
        var accepted = LibraryRun.Run(SyntaxVectors, new AlgorithmsLibrary())
            .Where(static r => r.Rendered == AlgorithmsLibrary.SyntaxAccepted && !AlgorithmsLibrary.MatchesAtTheParse(r))
            .Select(static r => r.Vector.Id)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(EarlyErrorAcceptances.Order(StringComparer.Ordinal).ToList(), accepted);
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

    /// <summary>The rendering of each outcome, shown with a parse that answers it and with the library's own parse.</summary>
    [Fact]
    public void A_refusal_an_acceptance_and_a_parse_not_made_are_each_rendered()
    {
        var script = SyntaxVectors.First(static v => v["op"] == AlgorithmsLibrary.ParseScriptOperation);
        var inputs = LibraryRun.Inputs(script);
        Assert.Equal(AlgorithmsLibrary.SyntaxRefused, AlgorithmsLibrary.SyntaxArea(script["op"]!, inputs, static _ => ParseOutcome.Refused));
        Assert.Equal(AlgorithmsLibrary.SyntaxAccepted, AlgorithmsLibrary.SyntaxArea(script["op"]!, inputs, static _ => ParseOutcome.Accepted));
        Assert.Equal(AlgorithmsLibrary.SyntaxAccepted, AlgorithmsLibrary.SyntaxArea(script["op"]!, [new TypedValue(TypedValueKind.String, "str:\"var a = 1;\"") { Chars = "var a = 1;" }], static request => SyntaxParser.Parse(request).Outcome));
        Assert.Equal(EmptyLibrary.NotImplemented, AlgorithmsLibrary.SyntaxArea(script["op"]!, inputs, static _ => ParseOutcome.NotImplemented));
        Assert.Equal(AlgorithmsLibrary.SyntaxRefused, AlgorithmsLibrary.SyntaxArea(script["op"]!, [new TypedValue(TypedValueKind.String, "str:\"var \\\\u0030a;\"") { Chars = "var \\u0030a;" }], static request => SyntaxParser.Parse(request).Outcome));
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
