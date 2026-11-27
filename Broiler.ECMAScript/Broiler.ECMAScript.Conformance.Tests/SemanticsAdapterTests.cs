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
/// D-js-adapter-form, from stage 3 D-js-adapter-groups and from stage 4 D-js-adapter-visible-names): it answers the
/// <c>semantics</c> vectors whose identifier names
/// a row the semantic model reports, and those of the correspondence groups the model decides, parses with
/// <see cref="ParseRequest.BuildSemanticModel"/> on, and is read at the parse, <c>error: SyntaxError</c> as a refusal and
/// any other outcome as an acceptance. Every other semantics vector is not implemented. <i>(Revised 2026-11-25, at SJL-15.2
/// stage 3, when group <c>lb1</c> was answered; it read "(SJL-15.2, from stage 2; the session's decisions D-js-adapter and
/// D-js-adapter-form): it answers the <c>semantics</c> vectors whose identifier names a row the semantic model reports,
/// parses".)</i> From stage 4 a <c>DirectEval</c> vector's eval code also inherits the private names visible at the call,
/// read from its Script's model, and group <c>pn1</c> stays not implemented. <i>(Revised 2026-11-25, at SJL-15.2 stage 4;
/// it read "D-js-adapter-form, and from stage 3 D-js-adapter-groups): it answers" and ended "Every other semantics vector is
/// not implemented.")</i>
/// From stage 5c (D-js-adapter-caller-scopes) the eval code also receives its caller's records, read
/// from the Script's model, and a direct eval the eval code holds is followed; the vectors of EA0007 are answered, those of
/// EA0006 stay not implemented. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
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
    /// Every semantics vector whose row is not in the adapter's list, and that is not of one of its groups, is not
    /// implemented, and no answered one is; the operation-only call answers neither operation. <i>(Revised 2026-11-25, at
    /// SJL-15.2 stage 3, which added the groups; it read "Every semantics vector whose row is not in the adapter's list is
    /// not implemented, and no answered one is".)</i>
    /// </summary>
    [Fact]
    public void Every_other_semantics_vector_is_not_implemented()
    {
        var library = new AlgorithmsLibrary();
        var others = SemanticsVectors.Where(static v => !AlgorithmsLibrary.IsLibrarySemantics(v)).ToList();
        Assert.NotEmpty(others);
        Assert.All(LibraryRun.Run(others, library), static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
        Assert.All(LibraryRun.Run(Answered, library), static r => Assert.NotEqual(Verdict.NotImplemented, r.Verdict));

        // A vector's row is its identifier's; a group that names none is answered only when it is one of the adapter's
        // groups. (Revised 2026-11-25, at SJL-15.2 stage 3; it read "a group that names none is never answered".)
        Assert.All(others, static v => Assert.DoesNotContain(AlgorithmsLibrary.SemanticsRow(v) ?? "-", AlgorithmsLibrary.SemanticsAnsweredRows));
        Assert.All(others, static v => Assert.DoesNotContain(AlgorithmsLibrary.SemanticsAnsweredGroups, g => g.Group == AlgorithmsLibrary.SemanticsGroup(v) && g.Operation == v["op"]));
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

    /// <summary>
    /// The Script request is the library's default for its goal plus the member; each answered DirectEval vector's context,
    /// its visible private names and its strictness by id. <i>(Revised 2026-11-25, at SJL-15.2 stage 4, when the visible
    /// private names joined the context; it read "each answered DirectEval vector's context and strictness by id", and held
    /// every context's names null.)</i>
    /// From stage 5c each context's caller's records too, by id, compared entry by entry,
    /// innermost first, as <c>{names}</c> or <c>{c:names}</c> for a Catch clause's: an empty list, never null, for every
    /// entry not named. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
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

            // A sloppy Script's own top-level eval of the row's source.
            ["semantics/arguments/ee0130/0005"] = (InheritedContext.TopLevel, false),
            ["semantics/arguments/ee0138/0004"] = (InheritedContext.TopLevel, false),
            ["semantics/expressions/ee0054/0005"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0015/0004"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0074/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0075/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0076/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0077/0004"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0093/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0102/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0103/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0104/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0105/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0106/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0117/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0118/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0119/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0120/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0121/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0124/0004"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0126/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0131/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0139/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0142/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0150/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0151/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0152/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/super/ee0153/0003"] = (InheritedContext.TopLevel, false),

            // Stage 4: the private-name rows' eval code, in a method of the class that declares #x, or at a sloppy Script's
            // top level. Class code is strict. (Added 2026-11-25, at SJL-15.2 stage 4.)
            ["semantics/private-names/ea0009/0001"] = (method, true),
            ["semantics/private-names/ea0009/0002"] = (InheritedContext.TopLevel, false),
            ["semantics/private-names/ea0009/0003"] = (method, true),

            // Stage 5c: eval code's var against its caller's records; ea0007/0005's outer eval code, at a sloppy Script's top
            // level. (Added 2026-11-26, at SJL-15.2 stage 5c.)
            ["semantics/scopes/ea0007/0001"] = (new InheritedContext { InFunction = true }, false),
            ["semantics/scopes/ea0007/0002"] = (new InheritedContext { InFunction = true }, false),
            ["semantics/scopes/ea0007/0003"] = (InheritedContext.TopLevel, false),
            ["semantics/scopes/ea0007/0004"] = (InheritedContext.TopLevel, false),
            ["semantics/scopes/ea0007/0005"] = (InheritedContext.TopLevel, false),
        };

        // The caller's records, by id (SJL-15.2 stage 5c; SCRATCH's expected-callers.tsv): a sloppy function's body gives its
        // record, a block or a Catch clause theirs; a strict holder, a field initialiser and a Script's top level an empty
        // list. (Added 2026-11-26, at SJL-15.2 stage 5c.)
        var records = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["semantics/super/ee0162/0003"] = "{}",
            ["semantics/super/ea0003/0001"] = "{}",
            ["semantics/super/ea0003/0003"] = "{}",
            ["semantics/super/ea0004/0003"] = "{}",
            ["semantics/scopes/ea0007/0001"] = "{}{x}",
            ["semantics/scopes/ea0007/0002"] = "{}{x}",
            ["semantics/scopes/ea0007/0003"] = "{x}",
            ["semantics/scopes/ea0007/0004"] = "{}{c:x}",
        };

        // The visible private names, by id: an empty list for every other entry, never null. (Added 2026-11-25, at SJL-15.2
        // stage 4.)
        var visible = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["semantics/private-names/ea0009/0001"] = ["#x"],
            ["semantics/private-names/ea0009/0003"] = ["#x"],
        };

        // Every answered DirectEval vector has its entry, and no other vector has one. (Added 2026-11-25, at SJL-15.2 stage
        // 2's review; the entries above the sloppy top-level ones were the only ones asserted.)
        Assert.Equal(
            expected.Keys.Order(StringComparer.Ordinal),
            Answered.Where(static v => v["op"] == AlgorithmsLibrary.DirectEvalOperation).Select(static v => v.Id).Order(StringComparer.Ordinal));
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
            Assert.Equal(visible.GetValueOrDefault(id, []), actual.VisiblePrivateNames);
            Assert.NotNull(actual.CallerScopes);
            Assert.True(records.GetValueOrDefault(id, "") == Written(actual.CallerScopes), id + ": " + Written(actual.CallerScopes));
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
    /// The visible private names a direct eval's request carries (D-js-adapter-visible-names): the innermost class whose
    /// elements hold the call and the environments around it, innermost first, each name once; a class's heritage not held
    /// by its elements; a computed key, a field initialiser, a static block and an arrow held; an empty list, never null,
    /// where no class holds the call or none declares a name. Group <c>pn1</c> is not answered. <i>(Added 2026-11-25, at
    /// SJL-15.2 stage 4.)</i>
    /// </summary>
    [Theory]
    [InlineData("class A { #a; m() { class B { #b; n() { return eval('1'); } } } }", "#b #a")]
    [InlineData("class A { #a; m() { class B extends (eval('1'), Object) { #b; } } }", "#a")]
    [InlineData("class A { #a; x = eval('1'); }", "#a")]
    [InlineData("class A { #a; m() { return (() => eval('1'))(); } }", "#a")]
    [InlineData("class A { #a; static { eval('1'); } }", "#a")]
    [InlineData("class A { #a; [eval('1')] = 1; }", "#a")]
    [InlineData("class A { #a; get #b() { return 1; } set #b(v) {} m() { class B { #a; #c; n() { return eval('1'); } } } }", "#a #c #b")]
    [InlineData("class A { m() { eval('1'); } }", "")]
    [InlineData("eval('1');", "")]
    [InlineData("function f() { eval('1'); }", "")]
    public void A_direct_eval_inherits_the_private_names_visible_at_the_call(string script, string names)
    {
        var request = AlgorithmsLibrary.DirectEvalRequest(script);
        Assert.NotNull(request);
        Assert.Equal(names.Split(' ', StringSplitOptions.RemoveEmptyEntries), request!.Context!.VisiblePrivateNames);
        Assert.All(SemanticsVectors.Where(static v => AlgorithmsLibrary.SemanticsGroup(v) == "pn1"), static v => Assert.False(AlgorithmsLibrary.IsLibrarySemantics(v), v.Id));
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

    /// <summary>
    /// Stage 6's rows answer no vector (SJL-15.2 stage 6): only the Module goal reaches EE0170 and EE0171, which schema 1 has
    /// no field for, so no semantics vector's identifier names either, and listing them, as the test above requires, moves no
    /// vector's answer. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void No_semantics_vector_names_the_module_record_s_rows()
    {
        Assert.Contains("EE0170", AlgorithmsLibrary.SemanticsAnsweredRows);
        Assert.Contains("EE0171", AlgorithmsLibrary.SemanticsAnsweredRows);
        Assert.NotEmpty(SemanticsVectors);
        Assert.All(SemanticsVectors, static v => Assert.DoesNotContain("ee0170", v.Id, StringComparison.OrdinalIgnoreCase));
        Assert.All(SemanticsVectors, static v => Assert.DoesNotContain("ee0171", v.Id, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(Answered, static v => AlgorithmsLibrary.SemanticsRow(v) is "EE0170" or "EE0171");
    }

    /// <summary>
    /// The adapter's groups (D-js-adapter-groups): each has vectors, all of its operation and answered; every row a group
    /// names is one of the adapter's rows; and every row each of its vectors' notes names is one of the group's, a check on
    /// the corpus, not a dispatch. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i>
    /// </summary>
    [Fact]
    public void Each_group_rests_on_answered_rows_that_its_vectors_notes_name()
    {
        Assert.NotEmpty(AlgorithmsLibrary.SemanticsAnsweredGroups);
        foreach (var (group, operation, rows) in AlgorithmsLibrary.SemanticsAnsweredGroups)
        {
            Assert.NotEmpty(rows);
            Assert.All(rows, static r => Assert.Contains(r, AlgorithmsLibrary.SemanticsAnsweredRows));
            var vectors = SemanticsVectors.Where(v => AlgorithmsLibrary.SemanticsGroup(v) == group).ToList();
            Assert.NotEmpty(vectors);
            foreach (var vector in vectors)
            {
                Assert.Null(AlgorithmsLibrary.SemanticsRow(vector));
                Assert.Equal(operation, vector["op"]);
                Assert.Contains(vector, Answered);
                var named = RowsNamed(vector["note"] ?? string.Empty);
                Assert.NotEmpty(named);
                Assert.All(named, r => Assert.Contains(r, rows));
            }
        }

        // lb1, the vectors of hand-off H20, are answered and all acceptances.
        var lb1 = Answered.Where(static v => AlgorithmsLibrary.SemanticsGroup(v) == "lb1").ToList();
        Assert.NotEmpty(lb1);
        Assert.All(LibraryRun.Run(lb1, new AlgorithmsLibrary()), static r => Assert.Equal(AlgorithmsLibrary.SyntaxAccepted, r.Rendered));
    }

    // A list of the caller's records as the tests write it (SJL-15.2 stage 5c).
    private static string Written(IReadOnlyList<CallerScope>? list) =>
        list is null ? "null" : string.Concat(list.Select(static s => "{" + (s.IsCatch ? "c:" : "") + string.Join(",", s.Names) + "}"));

    /// <summary>
    /// A direct eval the eval code holds (SJL-15.2 stage 5c; D-js-adapter-caller-scopes): <c>ea0007/0005</c>'s outer eval
    /// code, accepted with an empty list, holds one, whose records are the outer eval code's own lexical declarations, and the
    /// inner parse refuses with EA0007, the answer. The flags and the private names of an eval code's own holder come from
    /// the outer request. The vectors of EA0006 stay not implemented, and no EA0008 vector exists. <i>(Added 2026-11-26, at
    /// SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void A_direct_eval_in_eval_code_is_followed_and_the_innermost_parse_answers()
    {
        var vector = Answered.Single(static v => v.Id == "semantics/scopes/ea0007/0005");
        var outer = AlgorithmsLibrary.DirectEvalRequest(LibraryRun.Inputs(vector)[0].Chars)!;
        Assert.Equal("let x; eval('var x;')", outer.Source.Text);
        Assert.Empty(outer.Context!.CallerScopes!);
        Assert.Equal(ParseOutcome.Accepted, SyntaxParser.Parse(outer).Outcome);
        var model = SyntaxParser.Parse(outer).Model!;
        Assert.Equal("{x}", Written(model.CallerScopesOf(model.DirectEvalCalls.Single())));
        var answer = AlgorithmsLibrary.DirectEvalParse(outer);
        Assert.Equal(ParseOutcome.Refused, answer.Outcome);
        Assert.Equal("JSE1907", answer.Diagnostics[0].Code);
        Assert.Equal(AlgorithmsLibrary.SyntaxRefused, LibraryRun.Run([vector], new AlgorithmsLibrary()).Single().Rendered);

        // An eval code's flags, private names and strictness at a nested call: a method's outer request, a class of the eval
        // code, and a level that holds two eval names answered itself.
        var inMethod = AlgorithmsLibrary.DirectEvalRequest("class C { #a; m() { return eval(\"class D { #b; n() { return eval('this.#a + this.#b'); } }\"); } }")!;
        Assert.Equal(ParseOutcome.Accepted, AlgorithmsLibrary.DirectEvalParse(inMethod).Outcome);
        var refused = AlgorithmsLibrary.DirectEvalRequest("class C { #a; m() { return eval(\"(function () { return eval('this.#c'); })\"); } }")!;
        Assert.Equal(ParseOutcome.Refused, AlgorithmsLibrary.DirectEvalParse(refused).Outcome);
        var sloppy = AlgorithmsLibrary.DirectEvalRequest("eval(\"let y; { eval('var y'); eval('1'); }\")")!;
        Assert.Equal(ParseOutcome.Accepted, AlgorithmsLibrary.DirectEvalParse(sloppy).Outcome);
        var arrow = AlgorithmsLibrary.DirectEvalRequest("function f() { return eval(\"(() => eval('new.target'))\"); }")!;
        Assert.Equal(ParseOutcome.Accepted, AlgorithmsLibrary.DirectEvalParse(arrow).Outcome);

        Assert.All(SemanticsVectors.Where(static v => AlgorithmsLibrary.SemanticsRow(v) == "EA0006"), static v => Assert.False(AlgorithmsLibrary.IsLibrarySemantics(v), v.Id));
        Assert.DoesNotContain(SemanticsVectors, static v => AlgorithmsLibrary.SemanticsRow(v) == "EA0008");
    }

    // The catalogue rows a note names, E[EAP] and four digits, in order.
    private static List<string> RowsNamed(string note)
    {
        var rows = new List<string>();
        for (var i = 0; i + 6 <= note.Length; i++)
        {
            if (note[i] == 'E' && note[i + 1] is 'E' or 'A' or 'P' && note.AsSpan(i + 2, 4).IndexOfAnyExceptInRange('0', '9') < 0)
                rows.Add(note.Substring(i, 6));
        }

        return rows;
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
