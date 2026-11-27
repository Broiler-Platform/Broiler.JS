using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Semantics;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The semantic model's foundation (SJL-15.2 stage 1; Broiler.VM's JSD-0080, "The semantic model's design"): the request's
/// <see cref="ParseRequest.BuildSemanticModel"/>, which leaves the parse as it was; the model's scopes, a superset of the
/// side tables' sharing their instances; every node's scope; each binding identifier's declaration; and the per-function
/// facts, on the fixtures the stage names and over the determinism tests' population of the parse corpus. From stage 3,
/// each jump's target, <see cref="SemanticModel.TargetOf(BreakStatement)"/> and its <c>ContinueStatement</c> overload, on
/// fixtures and over the same population. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i> From stage 4, the private-name
/// environments, <see cref="SemanticModel.PrivateEnvironmentOf"/> and <see cref="SemanticModel.Resolve(PrivateIdentifier)"/>,
/// on fixtures and over the same population. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i> From stage 5a, resolution,
/// <see cref="SemanticModel.Resolve(Identifier)"/> and its <c>BindingIdentifier</c> overload, and the environment facts,
/// <see cref="SemanticModel.EnvironmentOf"/> and <see cref="FunctionFacts.HasSeparateVariableEnvironment"/>, on the probes'
/// and vectors' sources, on fixtures and over the same population, with an oracle of the test's own. <i>(Added 2026-11-26,
/// at SJL-15.2 stage 5a.)</i>
/// From stage 5b, Annex B's block-function aliases, <see cref="SemanticModel.AnnexBAlias"/>,
/// and the resolution of the names they bind, on the vectors' and probes' sources, on fixtures and over the same
/// population, with an oracle of the tree, and against the model's own answers with Annex B off. <i>(Added 2026-11-26, at
/// SJL-15.2 stage 5b.)</i>
/// From stage 5c, the caller's records of each direct-eval candidate, <see cref="SemanticModel.DirectEvalCalls"/> and
/// <see cref="SemanticModel.CallerScopesOf"/>, and what the request's own records decide in eval code, on fixtures and over
/// the same population, with an oracle of the tree. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
/// From stage 6, a Module's record, <see cref="SemanticModel.Module"/>, on fixtures and over the same population, with an
/// oracle of the tree. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
/// <remarks>
/// The off/on identity is written so that stage 2 narrows it mechanically: equality wherever the on path accepts, and,
/// separately, no on-path diagnostic outside <c>Rows</c>, which stage 2 turns into "every extra on-path diagnostic is a
/// <c>SemanticRows</c> row". From stage 2 the first holds wherever the on path accepts, a refusal on the on path being the
/// off path's or one by a <c>SemanticRows</c> row; the second holds with <c>SemanticRows</c>' rows added to the library's,
/// and with them removed the on path's diagnostics under all errors are the off path's. <i>(Revised 2026-11-25, at SJL-15.2
/// stage 2, which narrowed both; the remarks ended "At stage 1 both hold for every input.")</i>
/// </remarks>
public sealed class SemanticModelTests(ITestOutputHelper output)
{
    // -----------------------------------------------------------------------------------------------------------------
    // Helpers.

    private static ParseResult Parse(string text, ParseGoal goal = ParseGoal.Script, bool model = true, ProposalFlags proposals = ProposalFlags.None, DynamicFunctionKind kind = DynamicFunctionKind.Normal)
    {
        var request = new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Proposals = proposals,
            BuildSemanticModel = model,
        };
        if (kind != DynamicFunctionKind.Normal)
            request = new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance) { Proposals = proposals, BuildSemanticModel = model, FunctionKind = kind };
        return SyntaxParser.Parse(request);
    }

    private static (ParseResult Result, SemanticModel Model) Model(string text, ParseGoal goal = ParseGoal.Script, ProposalFlags proposals = ProposalFlags.None, DynamicFunctionKind kind = DynamicFunctionKind.Normal)
    {
        var result = Parse(text, goal, model: true, proposals, kind);
        Assert.True(result.Outcome == ParseOutcome.Accepted, text + ": " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code + " " + result.Diagnostics[0].MessageId : "-"));
        Assert.NotNull(result.Model);
        return (result, result.Model!);
    }

    // Every node of a tree, in tree order (iterative).
    private static List<SyntaxNode> Nodes(SyntaxNode root)
    {
        var list = new List<SyntaxNode>();
        var pending = new Stack<SyntaxNode>();
        var children = new List<SyntaxNode>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            list.Add(node);
            children.Clear();
            node.AddChildren(children);
            for (var i = children.Count - 1; i >= 0; i--)
                pending.Push(children[i]);
        }

        return list;
    }

    private static T First<T>(ParseResult result, Func<T, bool>? where = null)
        where T : SyntaxNode =>
        Nodes(result.Tree!).OfType<T>().First(n => where is null || where(n));

    private static T Nth<T>(ParseResult result, int index, Func<T, bool>? where = null)
        where T : SyntaxNode =>
        Nodes(result.Tree!).OfType<T>().Where(n => where is null || where(n)).ElementAt(index);

    private static BindingIdentifier Binding(ParseResult result, string name, int index = 0) => Nth<BindingIdentifier>(result, index, b => b.Name == name);

    private static Identifier Reference(ParseResult result, string name, int index = 0) => Nth<Identifier>(result, index, i => i.Name == name);

    private static ScopeKind[] Kinds(SemanticModel model) => model.Scopes.Select(static s => s.Kind).ToArray();

    // The scopes from a scope up to the model's first, by ParentOf.
    private static ScopeKind[] Chain(SemanticModel model, Scope scope)
    {
        var kinds = new List<ScopeKind>();
        for (Scope? s = scope; s is not null; s = model.ParentOf(s))
            kinds.Add(s.Kind);
        return kinds.ToArray();
    }

    private static Scope Only(SemanticModel model, ScopeKind kind) => model.Scopes.Single(s => s.Kind == kind);

    private static Scope Of(SemanticModel model, ScopeKind kind, SyntaxNode node) => model.Scopes.Single(s => s.Kind == kind && ReferenceEquals(s.Node, node));

    private static T OnDocumentedStack<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    result = work();
                }
                catch (Exception e)
                {
                    failure = e;
                }
            },
            ParseLimits.StackBytes);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw failure;
        return result;
    }

    private static string Name(DeterminismTests.Item item) => item.Id + " " + item.Variant + " " + item.Configuration;

    // -----------------------------------------------------------------------------------------------------------------
    // Over the determinism tests' population: the off/on identity, the projection and the completeness.

    // The model's rows (SJL-15.2 stage 2), as (code, message id) pairs.
    private static HashSet<(string Code, string MessageId)> SemanticRowSet() =>
        typeof(SemanticRows).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Select(static f => (LexicalRefusal)f.GetValue(null)!).Select(static r => (r.Code, r.MessageId)).ToHashSet();

    // The fixtures' refused sources of the model's rows, a second population of Script requests: the determinism
    // population may hold no source the off path accepts and a semantics row refuses (SJL-15.2 stage 2).
    private static readonly string[] RowSources =
    [
        "function f(a = super.x) {}", "function f() { super(); }", "function* g() { super.x; }", "async function f() { super(); }",
        "async function* g(a = super()) {}", "({ m() { super(); } });", "({ *g() { super(); } });", "class C { m() { super(); } }",
        "class C { static m() { super(); } }", "class C { constructor() { super(); } }", "class C extends Object { x = super(); }",
        "class C { static { super(); } }", "super.x;", "class C { x = arguments; }", "class C { static { arguments; } }",
        "({ __proto__: 1, '__proto__': 2 });", "for (this in {}) ;", "class C { x = class { static { arguments; } }; }",

        // Stage 3's jump rows. (Added 2026-11-25, at SJL-15.2 stage 3.)
        "break;", "continue;", "break L;", "L: { continue L; }", "function f() { break L; }", "class C { static { break L; } }",

        // Stage 4's private-name rows. (Added 2026-11-25, at SJL-15.2 stage 4.)
        "this.#x;", "class C { m() { this.#y; } }", "class C { m(o) { return #y in o; } }", "class C extends (o => o.#x, Object) { #x; }",
        "function f() { class C { m() { this.#y; } } }",
    ];

    // Sources both paths refuse, where the on path's first diagnostic is a row of the model's at an earlier place than the
    // off path's first, so that the identity test's branch for it runs (SJL-15.2 stage 2's review).
    private static readonly string[] BothRefusedRowSources = ["super.x; let a; let a;", "break; let a; let a;", "this.#x; let a; let a;"];

    // Stage 6's rows, which only the Module goal reaches: sources the off path accepts and the rows refuse, and a source
    // both paths refuse in which the row comes first, each parsed with a Module request (ModuleRowSourceRequest).
    // (Added 2026-11-27, at SJL-15.2 stage 6.)
    private static readonly string[] ModuleRowSources = ["export { x };", "var a; export { a, a };"];

    private static readonly string[] ModuleBothRefusedRowSources = ["var a; export { a, a }; let b; let b;"];

    private static ParseRequest RowSourceRequest(string text, bool model, bool allErrors = false) =>
        new(new SourceText(text), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            BuildSemanticModel = model,
            Limits = new ParseLimits { Reporting = allErrors ? ErrorReporting.AllErrors : ErrorReporting.FirstError },
        };

    private static ParseRequest ModuleRowSourceRequest(string text, bool model, bool allErrors = false) =>
        new(new SourceText(text), ParseGoal.Module, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            BuildSemanticModel = model,
            Limits = new ParseLimits { Reporting = allErrors ? ErrorReporting.AllErrors : ErrorReporting.FirstError },
        };

    /// <summary>
    /// Off and on over the population: an accepted parse on is the parse off; a refusal on with the off path accepting is a
    /// refusal by the model's rows alone, with no model; with both refused, the on path's first diagnostic is the off path's
    /// or one of the model's rows, and where it is the off path's the dumps are the same bytes. <i>(Revised 2026-11-25, at
    /// SJL-15.2 stage 2, which narrowed it; its refusal branch held every refusal's dump equal, "A refusal has no model, so
    /// its whole dump, the ordered diagnostics with their message ids, arguments and spans among it, is the off path's
    /// byte for byte".)</i>
    /// From stage 6 the row sources also hold Modules, parsed with a Module request, since only that goal reaches stage
    /// 6's rows. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void With_the_member_on_every_accepted_parse_is_the_parse_with_it_off()
    {
        var rows = SemanticRowSet();
        var items = DeterminismTests.Population.Value;
        var (accepted, bothRefused, onlyOn, semanticFirst, onlyOnRowSources, semanticFirstRowSources) = OnDocumentedStack(() =>
        {
            var (a, b, o, s, ro, rs) = (0, 0, 0, 0, 0, 0);
            void One(string name, ParseResult off, ParseResult on, bool rowSource = false)
            {
                Assert.Null(off.Model);
                if (on.Outcome == ParseOutcome.Accepted)
                {
                    a++;
                    Assert.True(off.Outcome == on.Outcome, name + ": the outcome differs");
                    Assert.True(off.Diagnostics.Count == on.Diagnostics.Count, name + ": the diagnostics differ");
                    Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on, model: false)), name + ": the dump without the model differs");
                    Assert.NotNull(on.Model);
                    return;
                }

                // A refusal has no model.
                Assert.Null(on.Model);
                if (off.Outcome == ParseOutcome.Accepted)
                {
                    // Refused by the model's rows alone.
                    o++;
                    ro += rowSource ? 1 : 0;
                    Assert.All(on.Diagnostics, d => Assert.True(rows.Contains((d.Code, d.MessageId)), name + ": " + d.Code + " refuses on and is not a SemanticRows row"));
                    return;
                }

                b++;
                var first = on.Diagnostics[0];
                var offFirst = off.Diagnostics[0];
                if (first.Code == offFirst.Code && first.MessageId == offFirst.MessageId && first.Span == offFirst.Span && first.Arguments.SequenceEqual(offFirst.Arguments))
                {
                    // The off path's first: the whole dump, the ordered diagnostics among it, is the off path's byte for byte.
                    Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on)), name + ": the refusal's dump differs");
                    return;
                }

                s++;
                rs += rowSource ? 1 : 0;
                Assert.True(rows.Contains((first.Code, first.MessageId)), name + ": the first diagnostic on, " + first.Code + ", is neither the off path's nor a SemanticRows row");
            }

            foreach (var item in items)
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item)), SyntaxParser.Parse(DeterminismTests.Request(item, model: true)));
            foreach (var text in RowSources)
                One("row source \"" + text + "\"", SyntaxParser.Parse(RowSourceRequest(text, model: false)), SyntaxParser.Parse(RowSourceRequest(text, model: true)), rowSource: true);
            foreach (var text in BothRefusedRowSources)
                One("row source \"" + text + "\"", SyntaxParser.Parse(RowSourceRequest(text, model: false)), SyntaxParser.Parse(RowSourceRequest(text, model: true)), rowSource: true);
            foreach (var text in ModuleRowSources.Concat(ModuleBothRefusedRowSources))
                One("module row source \"" + text + "\"", SyntaxParser.Parse(ModuleRowSourceRequest(text, model: false)), SyntaxParser.Parse(ModuleRowSourceRequest(text, model: true)), rowSource: true);
            return (a, b, o, s, ro, rs);
        });

        // Not vacuous: most of the corpus is accepted, some of it refused both ways, every row source refused on alone, and
        // every source of the second kind refused first by a row of the model's. The population's own counts are reported
        // apart from the row sources'. (Revised 2026-11-25, at SJL-15.2 stage 2's review; it read "and some refused on alone
        // (the row sources are, each of them)", and it asserted only that the count refused on alone, population and row
        // sources together, was at least the number of row sources.)
        Assert.True(accepted > items.Count / 2, accepted.ToString(CultureInfo.InvariantCulture) + " of " + items.Count.ToString(CultureInfo.InvariantCulture) + " accepted");
        Assert.True(bothRefused > 0);
        Assert.Equal(RowSources.Length + ModuleRowSources.Length, onlyOnRowSources);
        Assert.Equal(BothRefusedRowSources.Length + ModuleBothRefusedRowSources.Length, semanticFirstRowSources);
        output.WriteLine("identity branches: accepted on " + accepted.ToString(CultureInfo.InvariantCulture) + ", refused both ways "
            + bothRefused.ToString(CultureInfo.InvariantCulture) + " (of which a SemanticRows row first on " + semanticFirst.ToString(CultureInfo.InvariantCulture)
            + ", of those row sources " + semanticFirstRowSources.ToString(CultureInfo.InvariantCulture)
            + "), refused on alone " + onlyOn.ToString(CultureInfo.InvariantCulture) + " (population " + (onlyOn - onlyOnRowSources).ToString(CultureInfo.InvariantCulture)
            + ", row sources " + onlyOnRowSources.ToString(CultureInfo.InvariantCulture) + ")");
    }

    /// <summary>
    /// No on-path diagnostic is outside the library's rows, <c>Rows</c> and <c>SemanticRows</c>; under all errors, the on
    /// path's diagnostics with the model's rows removed are the off path's, in order, by code, message id, span and
    /// arguments. Under first error an earlier row of the model's displaces the off path's diagnostic, which the test above
    /// reads. <i>(Revised 2026-11-25, at SJL-15.2 stage 2, which narrowed it; its rows were <c>Rows</c>' alone, and it held
    /// "At stage 1 the refusals are the same, on or off", comparing the first-error lists by code and span.)</i>
    /// From stage 6 the row sources also hold Modules, parsed with a Module request. <i>(Added 2026-11-27, at SJL-15.2
    /// stage 6.)</i>
    /// </summary>
    [Fact]
    public void With_the_member_on_no_diagnostic_is_outside_the_library_s_rows()
    {
        var semantic = SemanticRowSet();
        var rows = typeof(Rows).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Select(static f => (LexicalRefusal)f.GetValue(null)!).Select(static r => (r.Code, r.MessageId)).ToHashSet();
        rows.UnionWith(semantic);
        var items = DeterminismTests.Population.Value;
        var (seen, extra) = OnDocumentedStack(() =>
        {
            var (count, more) = (0, 0);
            void One(string name, ParseResult off, ParseResult on)
            {
                foreach (var diagnostic in on.Diagnostics)
                {
                    Assert.True(rows.Contains((diagnostic.Code, diagnostic.MessageId)), name + ": " + diagnostic.Code + " is not a row");
                    count++;
                    more += semantic.Contains((diagnostic.Code, diagnostic.MessageId)) ? 1 : 0;
                }

                // Under all errors, the on path's list without the model's rows is the off path's.
                static (string, string, SourceSpan, string) Key(SyntaxDiagnostic d) => (d.Code, d.MessageId, d.Span, string.Join("\u0000", d.Arguments));
                Assert.True(
                    off.Diagnostics.Select(Key).SequenceEqual(on.Diagnostics.Where(d => !semantic.Contains((d.Code, d.MessageId))).Select(Key)),
                    name + ": the diagnostics differ but for the model's rows");
            }

            foreach (var item in items)
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item, allErrors: true)), SyntaxParser.Parse(DeterminismTests.Request(item, model: true, allErrors: true)));
            foreach (var text in RowSources.Concat(BothRefusedRowSources))
                One("row source \"" + text + "\"", SyntaxParser.Parse(RowSourceRequest(text, model: false, allErrors: true)), SyntaxParser.Parse(RowSourceRequest(text, model: true, allErrors: true)));
            foreach (var text in ModuleRowSources.Concat(ModuleBothRefusedRowSources))
                One("module row source \"" + text + "\"", SyntaxParser.Parse(ModuleRowSourceRequest(text, model: false, allErrors: true)), SyntaxParser.Parse(ModuleRowSourceRequest(text, model: true, allErrors: true)));
            return (count, more);
        });
        Assert.True(seen > 0);
        Assert.True(extra >= RowSources.Length + ModuleRowSources.Length);
        output.WriteLine("all-errors identity: diagnostics on " + seen.ToString(CultureInfo.InvariantCulture) + ", of which SemanticRows rows " + extra.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void The_model_s_scopes_and_facts_project_onto_the_tables_and_every_node_has_a_scope()
    {
        var items = DeterminismTests.Population.Value;
        var checkedNodes = OnDocumentedStack(() =>
        {
            var count = 0L;
            foreach (var item in items)
            {
                var on = SyntaxParser.Parse(DeterminismTests.Request(item, model: true));
                if (on.Model is not { } model)
                    continue;
                var tables = on.Declarations!;
                var name = Name(item);

                // The projection: the model's scopes that are the tables' are the tables' list in its order, and every other
                // has one of the model's kinds; the same for the facts.
                var tableScopes = new HashSet<Scope>(tables.Scopes, ReferenceEqualityComparer.Instance);
                Assert.True(model.Scopes.Where(tableScopes.Contains).SequenceEqual(tables.Scopes), name + ": the scopes do not project onto the tables'");
                Assert.True(model.Scopes.Where(s => !tableScopes.Contains(s)).All(static s => s.Kind >= ScopeKind.With), name + ": a model scope of a tables' kind");
                Assert.True(tables.Scopes.All(static s => s.Kind < ScopeKind.With), name + ": a tables' scope of a model kind");
                var tableFunctions = new HashSet<FunctionFacts>(tables.Functions, ReferenceEqualityComparer.Instance);
                Assert.True(model.Functions.Where(tableFunctions.Contains).SequenceEqual(tables.Functions), name + ": the facts do not project onto the tables'");
                Assert.True(model.Functions.Where(f => !tableFunctions.Contains(f)).All(static f => f.Kind is FunctionKind.StaticBlock or FunctionKind.FieldInitializer), name + ": a model fact of a tables' kind");

                // The parents: the first scope's is null, every other's an earlier scope.
                var index = new Dictionary<Scope, int>(ReferenceEqualityComparer.Instance);
                foreach (var scope in model.Scopes)
                    index[scope] = index.Count;
                Assert.Null(model.ParentOf(model.Scopes[0]));
                for (var i = 1; i < model.Scopes.Count; i++)
                {
                    var parent = model.ParentOf(model.Scopes[i]);
                    Assert.True(parent is not null && index[parent] < i, name + ": scope " + i.ToString(CultureInfo.InvariantCulture) + "'s parent");
                }

                // The completeness: every node reachable through AddChildren has a scope of the model's; every facts' node
                // answers its facts; every declaration's node answers a declaration.
                // The other way round: every BindingIdentifier of the tree declares a binding (the tree has no other kind of
                // one: labels are IdentifierNames, and an assignment pattern's targets are expressions), so each answers a
                // declaration, which is in one of the model's scopes.
                var declared = new HashSet<Declaration>(model.Scopes.SelectMany(static s => s.Declarations), ReferenceEqualityComparer.Instance);
                foreach (var node in Nodes(on.Tree!))
                {
                    Assert.True(index.ContainsKey(model.ScopeOf(node)), name + ": a node's scope is not the model's");
                    if (node is BindingIdentifier binding)
                    {
                        var declaration = model.DeclarationOf(binding);
                        Assert.True(declaration is not null && declared.Contains(declaration), name + ": the binding " + binding.Name + " at " + binding.Span.Offset.ToString(CultureInfo.InvariantCulture) + " has no declaration of the model's");
                    }

                    count++;
                }

                foreach (var facts in model.Functions)
                    Assert.Same(facts, model.FactsOf(facts.Node));
                foreach (var scope in model.Scopes)
                {
                    foreach (var declaration in scope.Declarations)
                        Assert.NotNull(model.DeclarationOf(declaration.Node));
                }

                Assert.Same(model.Scopes[0], model.ScopeOf(on.Tree!));
            }

            return count;
        });
        Assert.True(checkedNodes > 100_000, checkedNodes.ToString(CultureInfo.InvariantCulture) + " nodes");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // The member: off by default, no model on a refusal, and a node of another tree refused.

    [Fact]
    public void The_member_is_off_by_default_and_then_no_model_is_built()
    {
        var request = new ParseRequest(new SourceText("var a;"), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance);
        Assert.False(request.BuildSemanticModel);
        var result = SyntaxParser.Parse(request);
        Assert.Equal(ParseOutcome.Accepted, result.Outcome);
        Assert.Null(result.Model);
        Assert.NotNull(result.Declarations);
    }

    [Theory]
    [InlineData("let a; let a;")]
    [InlineData("var x = ")]
    [InlineData("function f(a, a) { 'use strict'; }")]
    public void A_refused_source_has_no_model_with_the_member_on(string text)
    {
        var result = Parse(text);
        Assert.Equal(ParseOutcome.Refused, result.Outcome);
        Assert.Null(result.Model);
        Assert.Null(result.Declarations);
    }

    [Fact]
    public void A_node_of_another_tree_or_a_node_without_facts_is_refused()
    {
        var (result, model) = Model("function f(a) { return a; } class C { x; }");
        var (other, otherModel) = Model("function f(a) { return a; } class C { x; }");
        var foreign = First<FunctionNode>(other);
        Assert.Throws<ArgumentException>(() => model.ScopeOf(foreign));
        Assert.Throws<ArgumentException>(() => model.DeclarationOf(foreign));
        Assert.Throws<ArgumentException>(() => model.FactsOf(foreign));
        Assert.Throws<ArgumentException>(() => model.ParentOf(otherModel.Scopes[1]));
        Assert.Throws<ArgumentNullException>(() => model.ScopeOf(null!));

        // A node of the tree that has no facts: a statement, and a field without an initialiser.
        Assert.Throws<ArgumentException>(() => model.FactsOf(First<ReturnStatement>(result)));
        Assert.Throws<ArgumentException>(() => model.FactsOf(First<FieldDefinition>(result)));
        Assert.NotNull(model.FactsOf(First<FunctionNode>(result)));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Fixtures.

    [Fact]
    public void A_named_function_expression_with_defaults()
    {
        var (result, model) = Model("var f = function g(a, b = a) { let c; return g; };");
        var function = First<FunctionNode>(result);
        Assert.Equal(new[] { ScopeKind.Script, ScopeKind.FunctionName, ScopeKind.Parameters, ScopeKind.Function }, Kinds(model));
        var program = model.Scopes[0];
        var name = Of(model, ScopeKind.FunctionName, function);
        var parameters = Of(model, ScopeKind.Parameters, function);
        var body = Of(model, ScopeKind.Function, function);
        Assert.Equal(new[] { ScopeKind.Function, ScopeKind.Parameters, ScopeKind.FunctionName, ScopeKind.Script }, Chain(model, body));

        Assert.Same(program, model.ScopeOf(function));
        Assert.Same(program, model.ScopeOf(First<VariableDeclaration>(result)));
        Assert.Same(name, model.ScopeOf(Binding(result, "g")));
        Assert.Same(parameters, model.ScopeOf(Binding(result, "a")));
        Assert.Same(parameters, model.ScopeOf(First<AssignmentPattern>(result)));
        Assert.Same(parameters, model.ScopeOf(Reference(result, "a")));
        Assert.Same(body, model.ScopeOf(First<ReturnStatement>(result)));
        Assert.Same(body, model.ScopeOf(Reference(result, "g")));

        var g = model.DeclarationOf(Binding(result, "g"))!;
        Assert.Equal((BindingKind.FunctionName, false), (g.Kind, g.IsMutable));
        Assert.Same(name.Declarations.Single(), g);
        Assert.Equal(new[] { "a", "b" }, parameters.Declarations.Select(static d => d.Name));
        Assert.All(parameters.Declarations, static d => Assert.Equal(BindingKind.Parameter, d.Kind));
        Assert.Same(parameters.Declarations[1], model.DeclarationOf(Binding(result, "b")));
        Assert.Equal(BindingKind.Let, model.DeclarationOf(Binding(result, "c"))!.Kind);
        Assert.Equal(BindingKind.Var, model.DeclarationOf(Binding(result, "f"))!.Kind);
        Assert.Null(model.DeclarationOf(Reference(result, "g")));
        Assert.Null(model.DeclarationOf(First<AssignmentPattern>(result)));

        var facts = model.FactsOf(function);
        Assert.Equal((FunctionKind.Normal, false, true), (facts.Kind, facts.HasSimpleParameterList, facts.HasParameterExpressions));
        Assert.Equal(new[] { "a", "b" }, facts.ParameterNames);
    }

    [Fact]
    public void An_arrow_with_a_destructuring_default_that_holds_a_computed_key()
    {
        var (result, model) = Model("const h = ({ [k]: x } = {}) => x;");
        var arrow = First<FunctionNode>(result);
        Assert.Equal(new[] { ScopeKind.Script, ScopeKind.Parameters, ScopeKind.Function }, Kinds(model));
        var parameters = Of(model, ScopeKind.Parameters, arrow);
        var body = Of(model, ScopeKind.Function, arrow);
        Assert.Same(model.Scopes[0], model.ParentOf(parameters));
        Assert.Same(parameters, model.ParentOf(body));
        Assert.Same(model.Scopes[0], model.ScopeOf(arrow));
        Assert.Same(model.Scopes[0], model.ScopeOf(First<ArrowFunctionExpression>(result)));
        Assert.Same(parameters, model.ScopeOf(First<ObjectPattern>(result)));
        Assert.Same(parameters, model.ScopeOf(First<ComputedPropertyName>(result)));
        Assert.Same(parameters, model.ScopeOf(Reference(result, "k")));
        Assert.Same(parameters, model.ScopeOf(First<ObjectLiteral>(result)));
        Assert.Same(body, model.ScopeOf(Reference(result, "x")));
        Assert.Same(parameters.Declarations.Single(), model.DeclarationOf(Binding(result, "x")));
        var h = model.DeclarationOf(Binding(result, "h"))!;
        Assert.Equal((BindingKind.Const, false), (h.Kind, h.IsMutable));
        var facts = model.FactsOf(arrow);
        Assert.Equal((FunctionKind.Arrow, false, true), (facts.Kind, facts.HasSimpleParameterList, facts.HasParameterExpressions));

        // A computed key alone, without an initialiser, is a parameter expression too; a plain pattern is not.
        Assert.True(Model("({ [k]: x }) => x;").Model.Functions[1].HasParameterExpressions);
        var plain = Model("({ a: x }, ...[y]) => x;").Model.Functions[1];
        Assert.Equal((false, false), (plain.HasSimpleParameterList, plain.HasParameterExpressions));
    }

    private const string ClassBodyText = " extends B { [k] = 1; f; accessor a = 2; static { let s; } constructor() { super(); } get g() { return 1; } set g(v) {} }";

    [Fact]
    public void A_class_declaration_with_every_element()
    {
        var (result, model) = Model("class C" + ClassBodyText, proposals: ProposalFlags.AutoAccessors);
        var @class = First<ClassNode>(result);
        Assert.Equal(
            new[] { ScopeKind.Script, ScopeKind.ClassBody, ScopeKind.StaticBlock, ScopeKind.Parameters, ScopeKind.Function, ScopeKind.Parameters, ScopeKind.Function, ScopeKind.Parameters, ScopeKind.Function },
            Kinds(model));
        var program = model.Scopes[0];
        var body = Of(model, ScopeKind.ClassBody, @class);
        Assert.Same(program, model.ParentOf(body));
        Assert.Equal(new[] { ScopeKind.StaticBlock, ScopeKind.ClassBody, ScopeKind.Script }, Chain(model, Only(model, ScopeKind.StaticBlock)));
        foreach (var method in Nodes(result.Tree!).OfType<MethodDefinition>())
        {
            Assert.Same(body, model.ScopeOf(method));
            Assert.Same(body, model.ScopeOf(method.Key));
            Assert.Same(body, model.ScopeOf(method.Function));
            Assert.Equal(new[] { ScopeKind.Function, ScopeKind.Parameters, ScopeKind.ClassBody, ScopeKind.Script }, Chain(model, Of(model, ScopeKind.Function, method.Function)));
        }

        // The declaration's name is outside, in the scope that binds it; the heritage, the elements, their keys and their
        // initialisers inside.
        Assert.Same(program, model.ScopeOf(@class));
        Assert.Same(program, model.ScopeOf(First<ClassDeclaration>(result)));
        Assert.Same(program, model.ScopeOf(Binding(result, "C")));
        Assert.Same(body, model.ScopeOf(Reference(result, "B")));
        foreach (var field in Nodes(result.Tree!).OfType<FieldDefinition>())
        {
            Assert.Same(body, model.ScopeOf(field));
            Assert.Same(body, model.ScopeOf(field.Key));
            if (field.Value is { } value)
                Assert.Same(body, model.ScopeOf(value));
        }

        Assert.Same(body, model.ScopeOf(First<ComputedPropertyName>(result)));
        Assert.Same(body, model.ScopeOf(Reference(result, "k")));
        Assert.Same(body, model.ScopeOf(First<StaticBlock>(result)));
        Assert.Same(Only(model, ScopeKind.StaticBlock), model.ScopeOf(First<VariableDeclaration>(result)));
        var constructor = First<MethodDefinition>(result, static m => m.Function.Body.Count == 1 && m.MethodKind == MethodKind.Method).Function;
        Assert.Same(Of(model, ScopeKind.Function, constructor), model.ScopeOf(First<CallExpression>(result)));
        var setter = First<MethodDefinition>(result, static m => m.MethodKind == MethodKind.Set).Function;
        Assert.Same(Of(model, ScopeKind.Parameters, setter), model.ScopeOf(Binding(result, "v")));

        // One declaration per name: the outer Class one the tables hold; the inner ClassName one in the class's scope.
        var outer = model.DeclarationOf(Binding(result, "C"))!;
        Assert.Equal((BindingKind.Class, true), (outer.Kind, outer.IsMutable));
        Assert.Same(program.Declarations.Single(), outer);
        var inner = body.Declarations.Single();
        Assert.Equal((BindingKind.ClassName, false, "C"), (inner.Kind, inner.IsMutable, inner.Name));
        Assert.Same(outer.Node, inner.Node);

        // The facts: the program, each initialised field, the static block, then the methods, in source order.
        Assert.Equal(
            new[] { FunctionKind.Script, FunctionKind.FieldInitializer, FunctionKind.FieldInitializer, FunctionKind.StaticBlock, FunctionKind.DerivedConstructor, FunctionKind.Getter, FunctionKind.Setter },
            model.Functions.Select(static f => f.Kind));
        Assert.Equal(new[] { FunctionKind.Script, FunctionKind.DerivedConstructor, FunctionKind.Getter, FunctionKind.Setter }, result.Declarations!.Functions.Select(static f => f.Kind));
        var accessor = First<FieldDefinition>(result, static f => f.IsAccessor);
        Assert.Equal(FunctionKind.FieldInitializer, model.FactsOf(accessor).Kind);
        Assert.Throws<ArgumentException>(() => model.FactsOf(First<FieldDefinition>(result, static f => f.Value is null)));
        Assert.All(model.Functions.Skip(1), static f => Assert.True(f.IsStrict));
    }

    [Fact]
    public void A_class_expression_with_every_element()
    {
        var (result, model) = Model("x = class D" + ClassBodyText + ";", proposals: ProposalFlags.AutoAccessors);
        var @class = First<ClassNode>(result);
        var body = Of(model, ScopeKind.ClassBody, @class);
        Assert.Same(model.Scopes[0], model.ScopeOf(@class));
        Assert.Same(model.Scopes[0], model.ScopeOf(First<ClassExpression>(result)));
        Assert.Same(body, model.ScopeOf(Binding(result, "D")));
        Assert.Same(body, model.ScopeOf(Reference(result, "B")));
        var d = model.DeclarationOf(Binding(result, "D"))!;
        Assert.Same(body.Declarations.Single(), d);
        Assert.Equal((BindingKind.ClassName, false), (d.Kind, d.IsMutable));
        Assert.Empty(model.Scopes[0].Declarations);
        Assert.Equal(FunctionKind.DerivedConstructor, model.FactsOf(First<MethodDefinition>(result, static m => m.Function.Body.Count == 1 && m.MethodKind == MethodKind.Method).Function).Kind);
    }

    [Fact]
    public void An_anonymous_class_has_its_scope_and_a_base_class_a_base_constructor()
    {
        var (result, model) = Model("x = class { constructor() {} m() {} };");
        var body = Of(model, ScopeKind.ClassBody, First<ClassNode>(result));
        Assert.Empty(body.Declarations);
        Assert.Equal(new[] { FunctionKind.Script, FunctionKind.ClassConstructor, FunctionKind.Method }, model.Functions.Select(static f => f.Kind));
    }

    [Fact]
    public void A_class_s_decorators_are_outside_it_and_its_elements_decorators_inside()
    {
        var (result, model) = Model("@d class C { @e m() {} @f x = 1; }", proposals: ProposalFlags.Decorators);
        var body = Of(model, ScopeKind.ClassBody, First<ClassNode>(result));
        Assert.Same(model.Scopes[0], model.ScopeOf(First<Decorator>(result)));
        Assert.Same(model.Scopes[0], model.ScopeOf(Reference(result, "d")));
        Assert.Same(body, model.ScopeOf(Reference(result, "e")));
        Assert.Same(body, model.ScopeOf(Reference(result, "f")));
    }

    [Fact]
    public void A_with_statement_s_body_is_in_its_object_environment()
    {
        var (result, model) = Model("with (o) { var v; p; }");
        var with = First<WithStatement>(result);
        Assert.Equal(new[] { ScopeKind.Script, ScopeKind.With, ScopeKind.Block }, Kinds(model));
        var scope = Of(model, ScopeKind.With, with);
        Assert.Empty(scope.Declarations);
        Assert.Same(model.Scopes[0], model.ScopeOf(with));
        Assert.Same(model.Scopes[0], model.ScopeOf(Reference(result, "o")));
        Assert.Same(scope, model.ScopeOf(with.Body));
        Assert.Same(model.Scopes[2], model.ScopeOf(Reference(result, "p")));
        Assert.Equal(new[] { ScopeKind.Block, ScopeKind.With, ScopeKind.Script }, Chain(model, model.Scopes[2]));
        Assert.Equal("v", model.Scopes[0].Declarations.Single().Name);
    }

    [Fact]
    public void A_catch_clause_s_parameter_and_body()
    {
        var (result, model) = Model("try {} catch ({ a = 1 }) { let b; }");
        var @catch = First<CatchClause>(result);
        Assert.Equal(new[] { ScopeKind.Script, ScopeKind.Block, ScopeKind.Catch, ScopeKind.Block }, Kinds(model));
        var scope = Of(model, ScopeKind.Catch, @catch);
        Assert.Same(model.Scopes[0], model.ScopeOf(@catch));
        Assert.Same(scope, model.ScopeOf(@catch.Parameter!));
        Assert.Same(scope, model.ScopeOf(First<NumericLiteral>(result)));
        Assert.Same(scope, model.ScopeOf(@catch.Body));
        Assert.Same(model.Scopes[3], model.ScopeOf(First<VariableDeclaration>(result)));
        Assert.Same(scope, model.ParentOf(model.Scopes[3]));
        var a = model.DeclarationOf(Binding(result, "a"))!;
        Assert.Equal((BindingKind.CatchParameter, true), (a.Kind, a.IsMutable));
    }

    [Fact]
    public void A_for_statement_with_a_lexical_head()
    {
        var (result, model) = Model("for (let i = 0; i < n; i++) { i; }");
        var @for = First<ForStatement>(result);
        var scope = Of(model, ScopeKind.For, @for);
        Assert.Same(model.Scopes[0], model.ScopeOf(@for));
        Assert.Same(scope, model.ScopeOf(@for.Init!));
        Assert.Same(scope, model.ScopeOf(@for.Test!));
        Assert.Same(scope, model.ScopeOf(@for.Update!));
        Assert.Same(scope, model.ScopeOf(@for.Body));
        Assert.Same(scope, model.ScopeOf(Reference(result, "n")));
        Assert.Equal(new[] { ScopeKind.Block, ScopeKind.For, ScopeKind.Script }, Chain(model, model.Scopes[2]));

        // A head that declares no lexical name has no scope: its parts are in the scope around it.
        var (plain, plainModel) = Model("for (var j = 0; j < n; j++) ;");
        Assert.Equal(new[] { ScopeKind.Script }, Kinds(plainModel));
        Assert.Same(plainModel.Scopes[0], plainModel.ScopeOf(First<ForStatement>(plain).Init!));
    }

    [Theory]
    [InlineData("for (let [a] of xs) a;")]
    [InlineData("for (const a in xs) { a; }")]
    public void A_for_in_or_of_statement_with_a_lexical_head(string text)
    {
        var (result, model) = Model(text);
        var statement = (Statement?)Nodes(result.Tree!).OfType<ForOfStatement>().FirstOrDefault() ?? First<ForInStatement>(result);
        var (left, right, body) = statement is ForOfStatement of ? (of.Left, (SyntaxNode)of.Right, of.Body) : (((ForInStatement)statement).Left, ((ForInStatement)statement).Right, ((ForInStatement)statement).Body);
        var scope = Of(model, ScopeKind.For, statement);
        Assert.Same(model.Scopes[0], model.ScopeOf(statement));
        Assert.Same(scope, model.ScopeOf(left));
        Assert.Same(scope, model.ScopeOf(First<VariableDeclarator>(result)));
        Assert.Same(scope, model.ScopeOf(right));
        Assert.Same(scope, model.ScopeOf(body));
        Assert.Same(scope.Declarations.Single(), model.DeclarationOf(Binding(result, "a")));
    }

    [Fact]
    public void A_switch_statement_s_discriminant_is_outside_its_case_block()
    {
        var (result, model) = Model("switch (d) { case t: let x; break; default: x; }");
        var @switch = First<SwitchStatement>(result);
        var scope = Of(model, ScopeKind.Switch, @switch);
        Assert.Same(model.Scopes[0], model.ScopeOf(@switch));
        Assert.Same(model.Scopes[0], model.ScopeOf(@switch.Discriminant));
        foreach (var @case in @switch.Cases)
            Assert.Same(scope, model.ScopeOf(@case));
        Assert.Same(scope, model.ScopeOf(Reference(result, "t")));
        Assert.Same(scope, model.ScopeOf(Reference(result, "x")));
        Assert.Same(scope.Declarations.Single(), model.DeclarationOf(Binding(result, "x")));
    }

    [Fact]
    public void A_label_is_in_the_scope_of_the_statement_holding_it()
    {
        var (result, model) = Model("l: for (;;) { break l; continue l; }");
        var labelled = First<LabelledStatement>(result);
        Assert.Same(model.Scopes[0], model.ScopeOf(labelled));
        Assert.Same(model.Scopes[0], model.ScopeOf(labelled.Label));
        var block = Only(model, ScopeKind.Block);
        Assert.Same(block, model.ScopeOf(First<BreakStatement>(result)));
        Assert.Same(block, model.ScopeOf(First<BreakStatement>(result).Label!));
        Assert.Same(block, model.ScopeOf(First<ContinueStatement>(result).Label!));
        Assert.Null(model.DeclarationOf(labelled.Label));
    }

    [Fact]
    public void An_if_clause_function_is_in_its_implicit_block()
    {
        var (result, model) = Model("if (q) function g(a) {}");
        var declaration = First<FunctionDeclaration>(result);
        Assert.Equal(new[] { ScopeKind.Script, ScopeKind.Block, ScopeKind.Parameters, ScopeKind.Function }, Kinds(model));
        var block = Of(model, ScopeKind.Block, declaration);
        Assert.Same(model.Scopes[0], model.ScopeOf(declaration));
        Assert.Same(block, model.ScopeOf(declaration.Function));
        Assert.Same(block, model.ScopeOf(Binding(result, "g")));
        Assert.Equal(new[] { ScopeKind.Function, ScopeKind.Parameters, ScopeKind.Block, ScopeKind.Script }, Chain(model, Of(model, ScopeKind.Function, declaration.Function)));
        var g = model.DeclarationOf(Binding(result, "g"))!;
        Assert.Same(block.Declarations.Single(), g);
        Assert.True(g.IsIfClauseFunction && g.IsBlockFunction && g.IsMutable);
    }

    [Fact]
    public void A_function_declaration_s_name_is_in_the_scope_that_binds_it()
    {
        var (result, model) = Model("function f(a) { function h() {} } ({ m() {}, get p() { return 1; }, set p(v) {} });");
        Assert.DoesNotContain(ScopeKind.FunctionName, Kinds(model));
        var f = First<FunctionNode>(result, static n => n.Name?.Name == "f");
        Assert.Same(model.Scopes[0], model.ScopeOf(Binding(result, "f")));
        Assert.Same(Of(model, ScopeKind.Function, f), model.ScopeOf(Binding(result, "h")));
        Assert.Equal(BindingKind.Function, model.DeclarationOf(Binding(result, "f"))!.Kind);
        Assert.Equal(
            new[] { FunctionKind.Script, FunctionKind.Normal, FunctionKind.Normal, FunctionKind.Method, FunctionKind.Getter, FunctionKind.Setter },
            model.Functions.Select(static x => x.Kind));
        Assert.Same(model.Scopes[0], model.ScopeOf(First<FunctionNode>(result, static n => n.Name is null && n.Body.Count == 0 && n.Parameters.Count == 0)));
    }

    [Fact]
    public void A_module_with_imports_and_an_export_default()
    {
        var (result, model) = Model("import a, * as ns from \"m\"; import { b as c } from \"n\"; export default function () { return a; }", ParseGoal.Module);
        Assert.Equal(new[] { ScopeKind.Module, ScopeKind.Parameters, ScopeKind.Function }, Kinds(model));
        foreach (var name in new[] { "a", "ns", "c" })
        {
            var declaration = model.DeclarationOf(Binding(result, name))!;
            Assert.Equal((BindingKind.Import, false), (declaration.Kind, declaration.IsMutable));
            Assert.Same(model.Scopes[0], model.ScopeOf(Binding(result, name)));
        }

        var exportDefault = First<ExportDefaultDeclaration>(result);
        var @default = model.DeclarationOf(exportDefault)!;
        Assert.Equal(("*default*", BindingKind.ExportDefault, true), (@default.Name, @default.Kind, @default.IsMutable));
        Assert.Equal(FunctionKind.Module, model.FactsOf(result.Tree!).Kind);
        Assert.True(model.FactsOf(result.Tree!).IsStrict);

        var (expression, expressionModel) = Model("export default 1;", ParseGoal.Module);
        Assert.Same(expressionModel.Scopes[0].Declarations.Single(), expressionModel.DeclarationOf(First<ExportDefaultDeclaration>(expression)));
        Assert.Null(expressionModel.DeclarationOf(First<NumericLiteral>(expression)));
    }

    [Fact]
    public void Eval_code_is_a_kind_of_facts_not_of_scope()
    {
        var (result, model) = Model("var e = 1; let l;", ParseGoal.Eval);
        Assert.Equal(new[] { ScopeKind.Script }, Kinds(model));
        Assert.Null(model.ParentOf(model.Scopes[0]));
        Assert.Same(model.Scopes[0], model.ScopeOf(result.Tree!));
        Assert.Equal(FunctionKind.EvalCode, model.FactsOf(result.Tree!).Kind);
    }

    [Theory]
    [InlineData(DynamicFunctionKind.Normal, false, false)]
    [InlineData(DynamicFunctionKind.Generator, false, true)]
    [InlineData(DynamicFunctionKind.Async, true, false)]
    [InlineData(DynamicFunctionKind.AsyncGenerator, true, true)]
    public void A_dynamic_function_s_parameters_and_body(DynamicFunctionKind kind, bool isAsync, bool isGenerator)
    {
        var (parameters, parametersModel) = Model("a, { b } = c", ParseGoal.FormalParameters, kind: kind);
        Assert.Equal(new[] { ScopeKind.Parameters, ScopeKind.Function }, Kinds(parametersModel));
        var first = parametersModel.Scopes[0];
        Assert.Null(parametersModel.ParentOf(first));
        Assert.Same(first, parametersModel.ParentOf(parametersModel.Scopes[1]));
        Assert.Same(first, parametersModel.ScopeOf(parameters.Tree!));
        Assert.Same(first, parametersModel.ScopeOf(Binding(parameters, "a")));
        Assert.Same(first, parametersModel.ScopeOf(Reference(parameters, "c")));
        Assert.Equal(new[] { "a", "b" }, first.Declarations.Select(static d => d.Name));
        Assert.Same(first.Declarations[1], parametersModel.DeclarationOf(Binding(parameters, "b")));
        var facts = parametersModel.FactsOf(parameters.Tree!);
        Assert.Equal((FunctionKind.DynamicFunction, isAsync, isGenerator, false, true), (facts.Kind, facts.IsAsync, facts.IsGenerator, facts.HasSimpleParameterList, facts.HasParameterExpressions));

        var (body, bodyModel) = Model("let x; return x;", ParseGoal.FunctionBody, kind: kind);
        Assert.Equal(new[] { ScopeKind.Parameters, ScopeKind.Function }, Kinds(bodyModel));
        Assert.Empty(bodyModel.Scopes[0].Declarations);
        Assert.Same(bodyModel.Scopes[1], bodyModel.ScopeOf(First<ReturnStatement>(body)));
        Assert.Same(bodyModel.Scopes[0], bodyModel.ScopeOf(body.Tree!));
        var bodyFacts = bodyModel.FactsOf(body.Tree!);
        Assert.Equal((FunctionKind.DynamicFunction, isAsync, isGenerator, true, false), (bodyFacts.Kind, bodyFacts.IsAsync, bodyFacts.IsGenerator, bodyFacts.HasSimpleParameterList, bodyFacts.HasParameterExpressions));
    }

    // The dynamic function's goals, outside the determinism population (whose requests are Script, Module and Eval ones):
    // the only goals where the on path adds a scope to the walk's top level (the program's Parameters scope). Each source
    // runs under every DynamicFunctionKind, sloppy and forced strict, with every error reported; a source written to reach
    // a row names the row's code, which at least one of its eight parses reports.
    public static TheoryData<ParseGoal, string, string?> DynamicGoalSources() => new()
    {
        { ParseGoal.FormalParameters, "a, { b } = c", null },
        { ParseGoal.FormalParameters, "a, [b, ...c], { [d]: e = f } = {}, ...g", null },
        { ParseGoal.FormalParameters, "", null },
        { ParseGoal.FormalParameters, "a, a", Rows.EE0070.Code },
        { ParseGoal.FormalParameters, "a, a = 1, { b: a }", Rows.EE0069.Code },
        { ParseGoal.FormalParameters, "a = yield, b = await", null },
        { ParseGoal.FunctionBody, "let x; return x;", null },
        { ParseGoal.FunctionBody, "var v; function f() {} class C {} { let w; }", null },
        { ParseGoal.FunctionBody, "", null },
        { ParseGoal.FunctionBody, "let x; let x;", Rows.EE0078.Code },
        { ParseGoal.FunctionBody, "let x; var x;", Rows.EE0079.Code },
        { ParseGoal.FunctionBody, "yield 1; await 2;", null },
    };

    [Theory]
    [MemberData(nameof(DynamicGoalSources))]
    public void A_dynamic_function_s_parse_with_the_member_on_is_the_parse_with_it_off(ParseGoal goal, string text, string? reaches)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        var (accepted, refused) = (0, 0);
        foreach (var kind in new[] { DynamicFunctionKind.Normal, DynamicFunctionKind.Generator, DynamicFunctionKind.Async, DynamicFunctionKind.AsyncGenerator })
        {
            foreach (var strict in new[] { false, true })
            {
                ParseRequest Request(bool model) => new(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance)
                {
                    Switches = ParseSwitches.For(goal) with { ForcedStrict = strict },
                    Limits = new ParseLimits { Reporting = ErrorReporting.AllErrors },
                    FunctionKind = kind,
                    BuildSemanticModel = model,
                };

                var off = SyntaxParser.Parse(Request(model: false));
                var on = SyntaxParser.Parse(Request(model: true));
                var name = goal + " " + kind + (strict ? " strict" : "") + " \"" + text + "\"";
                Assert.Null(off.Model);
                Assert.True(off.Outcome == on.Outcome, name + ": the outcome differs");
                Assert.True(
                    off.Diagnostics.Select(static d => (d.Code, d.MessageId, d.Span, string.Join("\u0000", d.Arguments)))
                        .SequenceEqual(on.Diagnostics.Select(static d => (d.Code, d.MessageId, d.Span, string.Join("\u0000", d.Arguments)))),
                    name + ": the diagnostics differ");
                Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on, model: false)), name + ": the dump without the model differs");
                if (on.Outcome == ParseOutcome.Accepted)
                {
                    Assert.NotNull(on.Model);
                    Assert.Equal(ScopeKind.Parameters, on.Model!.Scopes[0].Kind);
                    accepted++;
                }
                else
                {
                    // No model on a refusal: the whole dump is the off path's.
                    Assert.Null(on.Model);
                    Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on)), name + ": the refusal's dump differs");
                    refused++;
                }

                foreach (var diagnostic in on.Diagnostics)
                    codes.Add(diagnostic.Code);
            }
        }

        // Not vacuous: a source written to reach a row reaches it, and every other source is accepted under some kind.
        Assert.Equal(8, accepted + refused);
        if (reaches is not null)
            Assert.Contains(reaches, codes);
        else
            Assert.True(accepted > 0, text + " is never accepted");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Every FunctionKind, and every BindingKind's mutability and flags.

    [Theory]
    [InlineData("function f() {}", ParseGoal.Script, FunctionKind.Normal, false, false)]
    [InlineData("async function* f() {}", ParseGoal.Script, FunctionKind.Normal, true, true)]
    [InlineData("x = function* () {};", ParseGoal.Script, FunctionKind.Normal, false, true)]
    [InlineData("x = () => 1;", ParseGoal.Script, FunctionKind.Arrow, false, false)]
    [InlineData("x = async () => 1;", ParseGoal.Script, FunctionKind.Arrow, true, false)]
    [InlineData("({ m() {} });", ParseGoal.Script, FunctionKind.Method, false, false)]
    [InlineData("({ async *m() {} });", ParseGoal.Script, FunctionKind.Method, true, true)]
    [InlineData("class A { static async m() {} }", ParseGoal.Script, FunctionKind.Method, true, false)]
    [InlineData("({ get p() { return 1; } });", ParseGoal.Script, FunctionKind.Getter, false, false)]
    [InlineData("class A { set p(v) {} }", ParseGoal.Script, FunctionKind.Setter, false, false)]
    [InlineData("class A { constructor() {} }", ParseGoal.Script, FunctionKind.ClassConstructor, false, false)]
    [InlineData("class A { 'constructor'() {} }", ParseGoal.Script, FunctionKind.ClassConstructor, false, false)]
    [InlineData("class A extends B { constructor() { super(); } }", ParseGoal.Script, FunctionKind.DerivedConstructor, false, false)]
    [InlineData("class A { static constructor() {} }", ParseGoal.Script, FunctionKind.Method, false, false)]
    [InlineData("class A { x = 1; }", ParseGoal.Script, FunctionKind.FieldInitializer, false, false)]
    [InlineData("class A { static { } }", ParseGoal.Script, FunctionKind.StaticBlock, false, false)]
    public void Every_function_kind(string text, ParseGoal goal, FunctionKind kind, bool isAsync, bool isGenerator)
    {
        var (_, model) = Model(text, goal);
        var facts = model.Functions[1];
        Assert.Equal((kind, isAsync, isGenerator), (facts.Kind, facts.IsAsync, facts.IsGenerator));
        Assert.Equal(model.Functions.Count, model.Functions.Distinct().Count());
    }

    [Theory]
    [InlineData("1;", ParseGoal.Script, FunctionKind.Script)]
    [InlineData("1;", ParseGoal.Module, FunctionKind.Module)]
    [InlineData("1;", ParseGoal.Eval, FunctionKind.EvalCode)]
    [InlineData("a", ParseGoal.FormalParameters, FunctionKind.DynamicFunction)]
    [InlineData("1;", ParseGoal.FunctionBody, FunctionKind.DynamicFunction)]
    public void Every_program_kind(string text, ParseGoal goal, FunctionKind kind)
    {
        var (result, model) = Model(text, goal);
        var facts = model.FactsOf(result.Tree!);
        Assert.Equal((kind, false, false, true, false), (facts.Kind, facts.IsAsync, facts.IsGenerator, facts.HasSimpleParameterList, facts.HasParameterExpressions));

        // The facts are the same with the member off: the tables carry them too.
        var off = Parse(text, goal, model: false).Declarations!.Functions[0];
        Assert.Equal((facts.Kind, facts.IsAsync, facts.IsGenerator, facts.HasSimpleParameterList, facts.HasParameterExpressions), (off.Kind, off.IsAsync, off.IsGenerator, off.HasSimpleParameterList, off.HasParameterExpressions));
    }

    [Theory]
    [InlineData("function f(a) {}", true, false)]
    [InlineData("function f(a, ...b) {}", false, false)]
    [InlineData("function f([a]) {}", false, false)]
    [InlineData("function f(a = 1) {}", false, true)]
    [InlineData("function f({ a: [b = 1] }) {}", false, true)]
    [InlineData("function f({ ['a']: b }) {}", false, true)]
    [InlineData("function f(...{ [k]: b }) {}", false, true)]
    [InlineData("function f() {}", true, false)]
    public void Simple_lists_and_parameter_expressions(string text, bool simple, bool expressions)
    {
        var (_, model) = Model(text);
        var facts = model.Functions[1];
        Assert.Equal((simple, expressions), (facts.HasSimpleParameterList, facts.HasParameterExpressions));
    }

    /// <summary>
    /// Every binding kind's mutability and flags, each kind seen. <i>(Added 2026-11-26, at SJL-15.2 stage 5a, when
    /// <see cref="BindingKind.Arguments"/>, which no scope holds, joined the kinds: it is read from
    /// <see cref="SemanticModel.EnvironmentOf"/>, and its node answers no declaration.)</i>
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5b, when <see cref="BindingKind.BlockFunctionAlias"/>, which no scope holds either, joined them: it is read from
    /// <see cref="SemanticModel.AnnexBAlias"/>, and its node answers the block function's own declaration.)</i>
    /// </summary>
    [Fact]
    public void Every_binding_kind_s_mutability_and_flags()
    {
        var expected = new Dictionary<BindingKind, (bool Mutable, bool Lexical)>
        {
            [BindingKind.Var] = (true, false),
            [BindingKind.Let] = (true, true),
            [BindingKind.Const] = (false, true),
            [BindingKind.Using] = (false, true),
            [BindingKind.AwaitUsing] = (false, true),
            [BindingKind.Function] = (true, false),
            [BindingKind.Class] = (true, true),
            [BindingKind.CatchParameter] = (true, true),
            [BindingKind.Parameter] = (true, false),
            [BindingKind.FunctionName] = (false, false),
            [BindingKind.ClassName] = (false, false),
            [BindingKind.Arguments] = (true, false),
            [BindingKind.BlockFunctionAlias] = (true, false),
        };
        const string script = "var v; let l; const c = 1; { using u = null; } async function f(p) { await using w = null; } class K {} try {} catch (e) {} x = function n() {};";
        var seen = new HashSet<BindingKind>();
        var scriptModel = Model(script).Model;
        foreach (var declaration in scriptModel.Scopes.SelectMany(static s => s.Declarations).Concat(scriptModel.Functions.Select(f => scriptModel.EnvironmentOf(f.Node).Arguments).OfType<Declaration>()))
        {
            Assert.Equal(expected[declaration.Kind], (declaration.IsMutable, declaration.IsLexical));
            Assert.False(declaration.IsBlockFunction);
            Assert.False(declaration.IsIfClauseFunction);
            if (declaration.Kind == BindingKind.Arguments)
                Assert.Null(scriptModel.DeclarationOf(declaration.Node));
            seen.Add(declaration.Kind);
        }

        foreach (var scope in Model("import i from \"m\"; export default 1;", ParseGoal.Module).Model.Scopes)
        {
            foreach (var declaration in scope.Declarations)
            {
                Assert.Equal(declaration.Kind == BindingKind.ExportDefault, declaration.IsMutable);
                Assert.True(declaration.IsLexical);
                seen.Add(declaration.Kind);
            }
        }

        // The Annex B alias, which no scope holds, read from AnnexBAlias (added 2026-11-26, at SJL-15.2 stage 5b).
        var aliasModel = Model("function h() { { function z() {} } }").Model;
        var blockFunction = aliasModel.Scopes.Single(static s => s.Kind == ScopeKind.Block).Declarations.Single();
        var alias = aliasModel.AnnexBAlias(blockFunction)!;
        Assert.Equal(expected[alias.Kind], (alias.IsMutable, alias.IsLexical));
        Assert.False(alias.IsBlockFunction);
        Assert.False(alias.IsIfClauseFunction);
        Assert.Same(blockFunction, aliasModel.DeclarationOf(alias.Node));
        seen.Add(alias.Kind);

        Assert.Equal(Enum.GetValues<BindingKind>().OrderBy(static k => k), seen.OrderBy(static k => k));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Jump targets (SJL-15.2 stage 3; the session's decision D-js-jump-targets). (Added 2026-11-25, at SJL-15.2 stage 3.)

    // Every break and continue of a tree, in tree order.
    private static List<Statement> Jumps(ParseResult result) =>
        Nodes(result.Tree!).Where(static n => n is BreakStatement or ContinueStatement).Cast<Statement>().ToList();

    private static LabelTarget TargetOf(SemanticModel model, Statement jump) => jump switch
    {
        BreakStatement b => model.TargetOf(b),
        ContinueStatement c => model.TargetOf(c),
        _ => throw new ArgumentException("not a jump", nameof(jump)),
    };

    // The accepted sources with jumps the fixtures name, beside the population.
    private static readonly string[] JumpSources =
    [
        "while (c) { break; }", "while (c) { switch (d) { case 0: break; } }", "while (c) { switch (d) { case 0: continue; } }",
        "L: { break L; }", "A: B: { break A; }", "L: switch (d) { default: break L; }", "L: break L;",
        "var n = 0; A: B: for (var i = 0; i < 2; i++) { n++; continue A; } n",
        "function f() { var n = 0; A: B: for (var i = 0; i < 3; i++) { n++; continue A; } return n; } f()",
        "var n = 0; A: B: C: do { n++; if (n < 3) continue B; } while (n < 5); n",
        "var s = ''; o: for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { if (j) continue o; s += i + '' + j; } } s",
        "var n = 0; A: { B: for (var i = 0; i < 2; i++) { n++; continue B; } } n",
        "for (;;) break;", "for (let i = 0;;) continue;", "for (x in y) break;", "for (x of y) continue;", "for (let x of y) break;",
        "do break; while (c)", "L: while (1) { M: { break L; } }", "A: while (1) { B: { continue A; } }", "L1: L2: while (c) continue L1;",
        "while (a) { (function () { while (b) break; }); }", "while (a) { (() => { for (;;) continue; }); }",
        "while (a) { ({ m() { do break; while (b) } }); }", "while (a) { class C { static { while (b) continue; } } }",
        "L: while (c) { if (x) break L; continue L; break; continue; }", "L: if (x) break L; else { M: break M; }",
    ];

    // The same under the other goals, whose label owner is not the Script's: the Module, a function body and eval code.
    private static readonly (ParseGoal Goal, string Text)[] GoalJumpSources =
    [
        (ParseGoal.Module, "L: for (;;) { if (x) break L; continue L; } switch (0) { default: break; }"),
        (ParseGoal.Module, "A: B: { break A; } o: for (;;) { for (;;) { continue o; } }"),
        (ParseGoal.FunctionBody, "L: while (c) { if (x) break L; continue L; } A: B: do { continue A; } while (c); M: break M;"),
        (ParseGoal.FunctionBody, "while (a) { class C { static { L: while (b) { break L; } } } (() => { for (;;) continue; }); }"),
        (ParseGoal.Eval, "L: for (;;) { continue L; } A: B: C: do { break B; } while (c); switch (d) { case 0: break; }"),
    ];

    /// <summary>
    /// Each jump's target and label set: the innermost iteration statement or switch for an unlabelled break, the innermost
    /// iteration statement for an unlabelled continue, the iteration statement whose label set holds the label for a
    /// labelled continue, wherever the label stands in it (H20 read by the specification), and the labelled item, through
    /// every label on it, for a labelled break; the label set outermost first.
    /// </summary>
    [Theory]
    [InlineData("while (c) { break; }", 0, nameof(WhileStatement), 0, "")]
    [InlineData("while (c) { switch (d) { case 0: break; } }", 0, nameof(SwitchStatement), 0, "")]
    [InlineData("while (c) { switch (d) { case 0: continue; } }", 0, nameof(WhileStatement), 0, "")]
    [InlineData("L: { break L; }", 0, nameof(BlockStatement), 0, "L")]
    [InlineData("A: B: { break A; }", 0, nameof(BlockStatement), 0, "A B")]
    [InlineData("L: switch (d) { default: break L; }", 0, nameof(SwitchStatement), 0, "L")]
    [InlineData("var n = 0; A: B: for (var i = 0; i < 2; i++) { n++; continue A; } n", 0, nameof(ForStatement), 0, "A B")]
    [InlineData("var n = 0; A: B: C: do { n++; if (n < 3) continue B; } while (n < 5); n", 0, nameof(DoWhileStatement), 0, "A B C")]
    [InlineData("var s = ''; o: for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { if (j) continue o; s += i + '' + j; } } s", 0, nameof(ForStatement), 0, "o")]
    [InlineData("var n = 0; A: { B: for (var i = 0; i < 2; i++) { n++; continue B; } } n", 0, nameof(ForStatement), 0, "B")]
    [InlineData("L: break L;", 0, nameof(BreakStatement), 0, "L")]
    [InlineData("L: while (1) { M: { break L; } }", 0, nameof(WhileStatement), 0, "L")]
    [InlineData("A: while (1) { B: { continue A; } }", 0, nameof(WhileStatement), 0, "A")]
    [InlineData("for (;;) break;", 0, nameof(ForStatement), 0, "")]
    [InlineData("for (let i = 0;;) continue;", 0, nameof(ForStatement), 0, "")]
    [InlineData("for (x in y) break;", 0, nameof(ForInStatement), 0, "")]
    [InlineData("for (x of y) continue;", 0, nameof(ForOfStatement), 0, "")]
    [InlineData("for (let x of y) break;", 0, nameof(ForOfStatement), 0, "")]
    [InlineData("do break; while (c)", 0, nameof(DoWhileStatement), 0, "")]
    [InlineData("while (a) { (function () { while (b) break; }); }", 0, nameof(WhileStatement), 1, "")]
    [InlineData("while (a) { (() => { for (;;) continue; }); }", 0, nameof(ForStatement), 0, "")]
    [InlineData("while (a) { ({ m() { do break; while (b) } }); }", 0, nameof(DoWhileStatement), 0, "")]
    [InlineData("while (a) { class C { static { while (b) continue; } } }", 0, nameof(WhileStatement), 1, "")]
    [InlineData("L: if (x) break L; else { M: break M; }", 0, nameof(IfStatement), 0, "L")]
    [InlineData("L: if (x) break L; else { M: break M; }", 1, nameof(BreakStatement), 1, "M")]
    public void Each_jump_answers_its_target_and_the_target_s_own_label_set(string text, int jump, string targetType, int targetIndex, string labels)
    {
        var (result, model) = Model(text);
        var target = TargetOf(model, Jumps(result)[jump]);
        var expected = Nodes(result.Tree!).Where(n => n.GetType().Name == targetType).ElementAt(targetIndex);
        Assert.Same(expected, target.Statement);
        Assert.Equal(labels.Split(' ', StringSplitOptions.RemoveEmptyEntries), target.LabelSet);
        Assert.IsNotType<LabelledStatement>(target.Statement);
    }

    [Fact]
    public void Every_jump_that_targets_one_statement_answers_the_same_instance()
    {
        var (result, model) = Model("L: while (c) { if (x) break L; continue L; break; continue; }");
        var jumps = Jumps(result);
        Assert.Equal(4, jumps.Count);
        var first = TargetOf(model, jumps[0]);
        foreach (var jump in jumps)
            Assert.Same(first, TargetOf(model, jump));
        Assert.Same(First<WhileStatement>(result), first.Statement);
        Assert.Equal(new[] { "L" }, first.LabelSet);
    }

    [Fact]
    public void A_null_jump_or_a_jump_of_another_tree_is_refused()
    {
        const string text = "while (c) { break; continue; }";
        var (_, model) = Model(text);
        var (other, _) = Model(text);
        Assert.Throws<ArgumentNullException>(() => model.TargetOf((BreakStatement)null!));
        Assert.Throws<ArgumentNullException>(() => model.TargetOf((ContinueStatement)null!));
        Assert.Throws<ArgumentException>(() => model.TargetOf(First<BreakStatement>(other)));
        Assert.Throws<ArgumentException>(() => model.TargetOf(First<ContinueStatement>(other)));
    }

    /// <summary>
    /// Over the determinism tests' population and the fixtures' accepted sources, under the Script goal and under the Module,
    /// function body and eval goals: every jump of an accepted tree has a target; the target is in the tree and its span
    /// holds the jump's; a continue's is an iteration statement and an unlabelled break's an iteration statement or a
    /// switch; a labelled jump's label is in the label set; and the label set is the chain of labelled statements directly
    /// over the target, outermost first.
    /// </summary>
    [Fact]
    public void Every_jump_of_an_accepted_tree_has_a_target_over_the_population()
    {
        var items = DeterminismTests.Population.Value;
        var jumps = OnDocumentedStack(() =>
        {
            var count = 0;
            void One(string name, ParseResult on)
            {
                if (on.Model is not { } model)
                    return;
                var parents = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
                var children = new List<SyntaxNode>();
                foreach (var node in Nodes(on.Tree!))
                {
                    children.Clear();
                    node.AddChildren(children);
                    foreach (var child in children)
                        parents[child] = node;
                }

                foreach (var jump in Nodes(on.Tree!).Where(static n => n is BreakStatement or ContinueStatement).Cast<Statement>())
                {
                    count++;
                    var target = TargetOf(model, jump);
                    var statement = target.Statement;
                    var at = name + ": the jump at " + jump.Span.Offset.ToString(CultureInfo.InvariantCulture);
                    Assert.NotNull(model.ScopeOf(statement));
                    Assert.True(statement.Span.Offset <= jump.Span.Offset && jump.Span.Offset + jump.Span.Length <= statement.Span.Offset + statement.Span.Length, at + ": the target does not hold it");
                    Assert.False(statement is LabelledStatement, at + ": the target is a labelled statement");
                    var label = jump switch { BreakStatement b => b.Label, ContinueStatement c => c.Label, _ => null };
                    var iteration = statement is WhileStatement or DoWhileStatement or ForStatement or ForInStatement or ForOfStatement;
                    if (jump is ContinueStatement)
                        Assert.True(iteration, at + ": a continue's target is no iteration statement");
                    else if (label is null)
                        Assert.True(iteration || statement is SwitchStatement, at + ": an unlabelled break's target is no iteration statement or switch");
                    if (label is not null)
                        Assert.Contains(label.Name, target.LabelSet);

                    var chain = new List<string>();
                    for (var node = (SyntaxNode)statement; parents.TryGetValue(node, out var parent) && parent is LabelledStatement labelled && ReferenceEquals(labelled.Body, node); node = parent)
                        chain.Add(labelled.Label.Name);
                    chain.Reverse();
                    Assert.True(chain.SequenceEqual(target.LabelSet), at + ": the label set is not the chain over the target");
                }
            }

            foreach (var item in items)
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item, model: true)));
            foreach (var text in JumpSources)
            {
                var on = Parse(text);
                Assert.True(on.Outcome == ParseOutcome.Accepted, text);
                One("jump source \"" + text + "\"", on);
            }

            foreach (var (goal, text) in GoalJumpSources)
            {
                var on = Parse(text, goal);
                Assert.True(on.Outcome == ParseOutcome.Accepted, goal + ": " + text);
                One(goal + " jump source \"" + text + "\"", on);
            }

            return count;
        });
        Assert.True(jumps > JumpSources.Length, jumps.ToString(CultureInfo.InvariantCulture) + " jumps");
        output.WriteLine("jumps with a target: " + jumps.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The model section of the canonical dump holds each jump's target (decision D-js-part-g): one <c>model-target</c> line
    /// per jump, naming the jump, its target and the target's label set; a dump without the model is the dump with it off.
    /// </summary>
    [Fact]
    public void The_dump_s_model_section_holds_each_jump_s_target()
    {
        const string text = "L: while (1) { break L; }";
        var (on, _) = Model(text);
        var lines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(on)).Split('\n');
        var targets = lines.Where(static l => l.StartsWith("model-target ", StringComparison.Ordinal)).ToList();
        var nodes = Nodes(on.Tree!);
        var jump = nodes.FindIndex(static n => n is BreakStatement);
        var loop = nodes.FindIndex(static n => n is WhileStatement);
        Assert.Single(targets);
        Assert.Equal("model-target " + jump.ToString(CultureInfo.InvariantCulture) + " " + loop.ToString(CultureInfo.InvariantCulture) + " \"L\"", targets[0]);
        var off = Parse(text, model: false);
        Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on, model: false)));
        Assert.DoesNotContain("model-target", System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(off)), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Private names (SJL-15.2 stage 4; the session's decision D-js-private-environments). (Added 2026-11-25, at SJL-15.2
    // stage 4.)

    private static (ParseResult Result, SemanticModel Model) EvalModel(string text, params string[] visible)
    {
        var request = new ParseRequest(new SourceText(text), ParseGoal.Eval, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            BuildSemanticModel = true,
            Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = true },
            Context = new InheritedContext { InFunction = true, InMethod = true, VisiblePrivateNames = visible },
        };
        var result = SyntaxParser.Parse(request);
        Assert.True(result.Outcome == ParseOutcome.Accepted, text + ": " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : "-"));
        return (result, result.Model!);
    }

    // The private names of a tree in tree order, with the given name.
    private static PrivateIdentifier Private(ParseResult result, string name, int index = 0) => Nth<PrivateIdentifier>(result, index, p => p.Name == name);

    private static ClassNode Class(ParseResult result, int index = 0) => Nth<ClassNode>(result, index);

    /// <summary>
    /// Each kind and placement, a getter and setter pair one binding with two elements, and the declaring keys and every
    /// reference answering one instance.
    /// </summary>
    [Fact]
    public void Each_private_name_resolves_to_its_binding_with_its_kind_and_placement()
    {
        var (result, model) = Model(
            "class C { #f = 1; #m() {} get #g() { return 1; } set #s(v) {} get #p() { return 1; } set #p(v) {} static #t; static #u() {} "
            + "n() { return [this.#f, this.#m(), this.#g, this.#s = 1, this.#p, C.#t, C.#u(), #f in this]; } }");
        var environment = model.PrivateEnvironmentOf(Class(result));
        Assert.Equal(new[] { "#f", "#m", "#g", "#s", "#p", "#t", "#u" }, environment.Names.Select(static b => b.Name));
        Assert.Equal(
            new[] { PrivateNameKind.Field, PrivateNameKind.Method, PrivateNameKind.Accessor, PrivateNameKind.Accessor, PrivateNameKind.Accessor, PrivateNameKind.Field, PrivateNameKind.Method },
            environment.Names.Select(static b => b.Kind));
        Assert.Equal(new[] { false, false, false, false, false, true, true }, environment.Names.Select(static b => b.IsStatic));
        Assert.Equal(new[] { 1, 1, 1, 1, 2, 1, 1 }, environment.Names.Select(static b => b.Elements.Count));
        foreach (var binding in environment.Names)
        {
            Assert.Same(environment, binding.Environment);
            var all = Nodes(result.Tree!).OfType<PrivateIdentifier>().Where(p => p.Name == binding.Name).ToList();
            Assert.True(all.Count >= 2, binding.Name);
            foreach (var node in all)
                Assert.Same(binding, model.Resolve(node));
        }

        // The pair's elements are the getter and the setter, in source order.
        var pair = environment.Names.Single(static b => b.Name == "#p");
        Assert.Equal(new[] { MethodKind.Get, MethodKind.Set }, pair.Elements.Cast<MethodDefinition>().Select(static m => m.MethodKind));

        // An auto-accessor, under its proposal.
        var (accessor, accessorModel) = Model("class C { accessor #a = 1; m() { return this.#a; } }", proposals: ProposalFlags.AutoAccessors);
        var a = accessorModel.Resolve(Private(accessor, "#a", 1))!;
        Assert.Equal(PrivateNameKind.Accessor, a.Kind);
        Assert.Same(a, accessorModel.Resolve(Private(accessor, "#a")));
        Assert.IsType<FieldDefinition>(Assert.Single(a.Elements));

        // Placement plays no part: a static name used through an instance and the other way round resolve as written.
        var (crossed, crossedModel) = Model("class C { static #s; #i; m() { return this.#s; } static n(o) { return o.#i; } }");
        Assert.Same(crossedModel.Resolve(Private(crossed, "#s")), crossedModel.Resolve(Private(crossed, "#s", 1)));
        Assert.Same(crossedModel.Resolve(Private(crossed, "#i")), crossedModel.Resolve(Private(crossed, "#i", 1)));
    }

    /// <summary>
    /// The innermost class that declares a name wins; a nested class reads an outer name; a class's heritage reads the
    /// names around the class; computed keys, initialisers, static blocks, arrows, parameter defaults and an object literal's
    /// methods read the class's own; an object literal's private key declares nothing.
    /// </summary>
    [Fact]
    public void A_reference_resolves_in_the_innermost_class_that_declares_it_and_a_heritage_in_the_names_around_its_class()
    {
        var (shadow, shadowModel) = Model("class O { #x; m() { class I { #x; n(o) { return o.#x; } } return this.#x; } }");
        var outerX = shadowModel.Resolve(Private(shadow, "#x"))!;
        var innerX = shadowModel.Resolve(Private(shadow, "#x", 1))!;
        Assert.NotSame(outerX, innerX);
        Assert.Same(innerX, shadowModel.Resolve(Private(shadow, "#x", 2)));
        Assert.Same(outerX, shadowModel.Resolve(Private(shadow, "#x", 3)));
        Assert.Same(Class(shadow, 1), innerX.Environment.Class);

        var (nested, nestedModel) = Model("class O { #x; m() { class I { n(o) { return o.#x; } } } }");
        Assert.Same(nestedModel.Resolve(Private(nested, "#x")), nestedModel.Resolve(Private(nested, "#x", 1)));

        var (heritage, heritageModel) = Model("class O { #x; m() { class I extends (o => o.#x, Object) {} } }");
        Assert.Same(heritageModel.PrivateEnvironmentOf(Class(heritage)), heritageModel.Resolve(Private(heritage, "#x", 1))!.Environment);

        foreach (var text in new[]
        {
            "class C { #x; [o => o.#x] = 1; }", "class C { #x; y = this.#x; }", "class C { #x; static { (o => o.#x); } }",
            "class C { #x; m() { return () => this.#x; } }", "class C { #x; m(a = this.#x) {} }", "class C { #x; m() { return { f(o) { return o.#x; } }; } }",
        })
        {
            var (result, model) = Model(text);
            Assert.Same(model.Resolve(Private(result, "#x")), model.Resolve(Private(result, "#x", 1)));
            Assert.Same(Class(result), model.Resolve(Private(result, "#x", 1))!.Environment.Class);
        }

        // An escaped spelling is its StringValue: the escaped key and the plain reference, or the plain key and the escaped
        // reference, answer one binding of #y.
        foreach (var text in new[] { "class C { #\\u0079; m() { return this.#y; } }", "class C { #y; m() { return this.#\\u0079; } }" })
        {
            var (escaped, escapedModel) = Model(text);
            var y = escapedModel.Resolve(Private(escaped, "#y"))!;
            Assert.Equal("#y", y.Name);
            Assert.Same(y, escapedModel.Resolve(Private(escaped, "#y", 1)));
            Assert.Same(escapedModel.PrivateEnvironmentOf(Class(escaped)).Names.Single(), y);
        }

        // An object literal's private key declares nothing; EE0016 refuses it on both paths, so that no model holds one.
        Assert.Null(Parse("({ #m() {} });").Model);
        Assert.Null(Parse("class C { #m() {} f() { return { #m() {}, g() { return this.#m; } }; } }").Model);
        Assert.Null(Parse("class C { f() { return { #m() {}, g() { return this.#m; } }; } }").Model);
    }

    /// <summary>
    /// Eval code: a name the caller sees and no class of the eval code declares has no binding in the unit; an eval-code
    /// class's own names resolve; the dynamic function's goals answer null; an eval-code class's outer environment is null.
    /// </summary>
    [Fact]
    public void Eval_code_s_caller_names_and_the_dynamic_goals_answer_null()
    {
        var (caller, callerModel) = EvalModel("this.#x;", "#x");
        Assert.Null(callerModel.Resolve(Private(caller, "#x")));

        var (own, ownModel) = EvalModel("class D { #y; m() { return this.#y + this.#x; } }", "#x");
        var y = ownModel.Resolve(Private(own, "#y", 1))!;
        Assert.Same(ownModel.Resolve(Private(own, "#y")), y);
        Assert.Null(ownModel.Resolve(Private(own, "#x")));
        Assert.Null(ownModel.PrivateEnvironmentOf(Class(own)).Outer);

        var (body, bodyModel) = Model("return this.#x;", ParseGoal.FunctionBody);
        Assert.Null(bodyModel.Resolve(Private(body, "#x")));
    }

    /// <summary>
    /// Each class's environment: its names in source order, once each, <c>#constructor</c> excluded only by the source
    /// EE0132 refuses; the outer chain, a class in another class's heritage skipping that class; an empty environment for a
    /// class that declares none; a binding's environment the class's.
    /// </summary>
    [Fact]
    public void Each_class_has_a_private_environment_whose_outer_one_is_the_environment_it_is_evaluated_in()
    {
        var (result, model) = Model("class O { #o; m() { class A extends (class I { n(p) { return p.#o; } }, Object) { #a; } } }");
        var o = model.PrivateEnvironmentOf(Class(result, 0));
        var a = model.PrivateEnvironmentOf(Class(result, 1));
        var i = model.PrivateEnvironmentOf(Class(result, 2));
        Assert.Same(Class(result, 1), a.Class);
        Assert.Null(o.Outer);
        Assert.Same(o, a.Outer);
        Assert.Same(o, i.Outer);
        Assert.Empty(i.Names);
        Assert.Same(o.Names.Single(), model.Resolve(Private(result, "#o", 1)));
        Assert.Equal(new[] { "#a" }, a.Names.Select(static b => b.Name));

        var (method, methodModel) = Model("class O { #x; m() { class I extends (o => o.#x, Object) {} } }");
        Assert.Same(methodModel.PrivateEnvironmentOf(Class(method, 0)), methodModel.PrivateEnvironmentOf(Class(method, 1)).Outer);

        var (empty, emptyModel) = Model("class C { m() {} } (class {});");
        Assert.Empty(emptyModel.PrivateEnvironmentOf(Class(empty, 0)).Names);
        Assert.Empty(emptyModel.PrivateEnvironmentOf(Class(empty, 1)).Names);

        var (order, orderModel) = Model("class C { #b; static #a() {} get #c() { return 1; } #d; set #c(v) {} }");
        var names = orderModel.PrivateEnvironmentOf(Class(order)).Names;
        Assert.Equal(new[] { "#b", "#a", "#c", "#d" }, names.Select(static b => b.Name));
        foreach (var binding in names)
            Assert.Same(binding.Environment, orderModel.PrivateEnvironmentOf(binding.Environment.Class));
    }

    [Fact]
    public void A_null_or_foreign_class_or_private_name_is_refused()
    {
        const string text = "class C { #x; m() { return this.#x; } }";
        var (_, model) = Model(text);
        var (other, _) = Model(text);
        Assert.Throws<ArgumentNullException>(() => model.Resolve((PrivateIdentifier)null!));
        Assert.Throws<ArgumentNullException>(() => model.PrivateEnvironmentOf(null!));
        Assert.Throws<ArgumentException>(() => model.Resolve(Private(other, "#x")));
        Assert.Throws<ArgumentException>(() => model.PrivateEnvironmentOf(Class(other)));
    }

    // Accepted sources with private names, beside the population.
    private static readonly string[] PrivateSources =
    [
        "class C { #x = 1; m() { return this.#x; } } new C().m()", "class C { #x = 2; m() { class D { n(o) { return o.#x; } } return new D().n(this); } } new C().m()",
        "class C { m() { return this.#x; } #x; }", "class C { #x; m() { class D { #x; n(o) { return o.#x; } } } }",
        "class C { static #s; m() { return C.#s; } }", "class C { #x; static m(o) { return #x in o; } }",
        "class C { get #a() { return 1; } set #a(v) {} m() { return this.#a; } }", "class C { #x = 1; y = this.#x; }",
        "class C { static #s; static { C.#s; } }", "class C { #x; m(a = this.#x) {} }", "class C { #x; m({ [this.#x]: y }) {} }",
        "class C { #x; [o => o.#x] = 1; }", "class C { #x; m() { return { f(o) { return o.#x; } }; } }",
        "class O { #x; m() { class I extends (o => o.#x, Object) {} } }", "class C { #x; m(o) { return o?.#x; } }",
        "class O { #o; m() { class A extends (class I { n(p) { return p.#o; } }, Object) { #a; m() { return this.#a; } } } }",

        // The fixtures' admitted variants of SemanticRowsTests not above, Script and Module alike.
        "class C { #x; m() { this.#x; } }", "class C { static #s; m() { return this.#s; } }", "class C { #x; static m(o) { return o.#x; } }",
        "class C { static get #a() { return 1; } static set #a(v) {} static m() { return C.#a; } }",
        "class C { #m() {} *#g() {} async #f() {} async *#h() {} n() { this.#m(); this.#g(); this.#f(); this.#h(); } }",
        "class C { #x; get g() { return this.#x; } set g(v) { this.#x = v; } }", "class C { #x; y = () => this.#x; }",
        "class C { static #s; static { (function () { C.#s; }); } }",
        "class C { #x; m() { this.#x = 1; this.#x++; this.#x += 1; [this.#x] = [1]; ({ a: this.#x } = {}); for (this.#x of []); delete this.y; } }",
        "class C { #x; m() { switch (0) { case 0: this.#x; } } }", "class C { #x; m() { return this.#x; } }",
        "class C { #\\u0079; m() { return this.#y; } }", "class C { #y; m() { return this.#\\u0079; } }",
    ];

    /// <summary>
    /// Over the determinism tests' population and the fixtures' accepted sources, Script and Module trees: every private
    /// name has a scope; every class element's key and every reference resolves to a binding of its name; a key's binding
    /// lists its element; a reference's binding is that of the innermost class whose elements hold it and whose names hold
    /// it; every class's environment holds its distinct private key names in source order.
    /// </summary>
    [Fact]
    public void Every_private_name_of_an_accepted_tree_resolves_over_the_population()
    {
        var items = DeterminismTests.Population.Value;
        var (references, keys, classes) = OnDocumentedStack(() =>
        {
            var (r, k, c) = (0, 0, 0);
            void One(string name, ParseResult on)
            {
                if (on.Model is not { } model || on.Tree!.Goal is not (ParseGoal.Script or ParseGoal.Module))
                    return;
                var parents = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
                var children = new List<SyntaxNode>();
                var nodes = Nodes(on.Tree!);
                foreach (var node in nodes)
                {
                    children.Clear();
                    node.AddChildren(children);
                    foreach (var child in children)
                        parents[child] = node;
                }

                // The classes whose elements hold a node, innermost first.
                IEnumerable<ClassNode> Holding(SyntaxNode node)
                {
                    for (var at = node; parents.TryGetValue(at, out var parent); at = parent)
                    {
                        if (parent is ClassNode @class && @class.Elements.Contains(at))
                            yield return @class;
                    }
                }

                foreach (var node in nodes)
                {
                    if (node is ClassNode @class)
                    {
                        c++;
                        var keyNames = @class.Elements.Select(static e => e switch { MethodDefinition m => m.Key, FieldDefinition f => f.Key, _ => null })
                            .OfType<PrivateIdentifier>().Select(static p => p.Name).Distinct(StringComparer.Ordinal);
                        Assert.True(keyNames.SequenceEqual(model.PrivateEnvironmentOf(@class).Names.Select(static b => b.Name)), name + ": the class at " + @class.Span.Offset.ToString(CultureInfo.InvariantCulture) + " has other names");
                        continue;
                    }

                    if (node is not PrivateIdentifier privateName)
                        continue;
                    var at = name + ": " + privateName.Name + " at " + privateName.Span.Offset.ToString(CultureInfo.InvariantCulture);
                    Assert.NotNull(model.ScopeOf(privateName));
                    var parent = parents[privateName];
                    if (parent is PropertyDefinition)
                        continue; // an object literal's key, which EE0016 refuses: no model holds one
                    var binding = model.Resolve(privateName);
                    Assert.True(binding is not null && binding.Name == privateName.Name, at + ": no binding of its name");
                    if (parent is MethodDefinition or FieldDefinition)
                    {
                        k++;
                        Assert.Contains(parent, binding!.Elements);
                        continue;
                    }

                    r++;
                    var expected = Holding(parent).First(h => model.PrivateEnvironmentOf(h).Names.Any(b => b.Name == privateName.Name));
                    Assert.True(ReferenceEquals(expected, binding!.Environment.Class), at + ": not the innermost class that holds and declares it");
                }
            }

            foreach (var item in items)
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item, model: true)));
            foreach (var text in PrivateSources)
            {
                var on = Parse(text);
                Assert.True(on.Outcome == ParseOutcome.Accepted, text);
                One("private source \"" + text + "\"", on);
                var module = Parse(text, ParseGoal.Module);
                Assert.True(module.Outcome == ParseOutcome.Accepted, "module " + text);
                One("module private source \"" + text + "\"", module);
            }

            return (r, k, c);
        });
        Assert.True(references > PrivateSources.Length && keys > PrivateSources.Length && classes > PrivateSources.Length);
        output.WriteLine("private names resolved: references " + references.ToString(CultureInfo.InvariantCulture) + ", keys "
            + keys.ToString(CultureInfo.InvariantCulture) + "; classes " + classes.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The model section of the canonical dump holds the private-name tables (decision D-js-part-g): one
    /// <c>model-private-environment</c> line per class, one <c>model-private-name</c> line per binding, one
    /// <c>model-private</c> line per private name; a dump without the model is the dump with it off.
    /// </summary>
    [Fact]
    public void The_dump_s_model_section_holds_the_private_names()
    {
        const string text = "class C { #x; m() { this.#x; } }";
        var (on, _) = Model(text);
        var lines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(on)).Split('\n');
        var nodes = Nodes(on.Tree!);
        var @class = nodes.FindIndex(static n => n is ClassNode).ToString(CultureInfo.InvariantCulture);
        var field = nodes.FindIndex(static n => n is FieldDefinition).ToString(CultureInfo.InvariantCulture);
        var names = nodes.Select((n, i) => (n, i)).Where(static p => p.n is PrivateIdentifier).Select(static p => p.i.ToString(CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(new[] { "model-private-environment " + @class + " -" }, lines.Where(static l => l.StartsWith("model-private-environment ", StringComparison.Ordinal)));
        Assert.Equal(new[] { "model-private-name \"#x\" 0 0 " + field }, lines.Where(static l => l.StartsWith("model-private-name ", StringComparison.Ordinal)));
        Assert.Equal(names.Select(n => "model-private " + n + " " + @class), lines.Where(static l => l.StartsWith("model-private ", StringComparison.Ordinal)));
        Assert.Equal(2, names.Count);
        var off = Parse(text, model: false);
        Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on, model: false)));
        Assert.DoesNotContain("model-private", System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(off)), StringComparison.Ordinal);
    }

    /// <summary>
    /// The model section of the canonical dump holds the environment facts and the resolutions (decision D-js-part-g; SJL-15.2
    /// stage 5a): one <c>model-environment</c> line per facts entry, one <c>model-resolution</c> line per reference and per
    /// binding identifier, an <c>arguments</c> binding named by its node; a dump without the model is the dump with it off.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5a.)</i>
    /// </summary>
    [Fact]
    public void The_dump_s_model_section_holds_the_environments_and_the_resolutions()
    {
        const string text = "function f(a) { with (a) { return arguments; } }";
        var (on, model) = Resolved(text);
        var lines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(on)).Split('\n');
        var nodes = Nodes(on.Tree!);
        string Index(SyntaxNode node) => nodes.FindIndex(n => ReferenceEquals(n, node)).ToString(CultureInfo.InvariantCulture);
        string ScopeIndex(Scope scope) => model.Scopes.ToList().FindIndex(s => ReferenceEquals(s, scope)).ToString(CultureInfo.InvariantCulture);
        var f = First<FunctionNode>(on);
        Assert.Equal(new[] { "model-environment " + Index(on.Tree!) + " 0000000", "model-environment " + Index(f) + " 1110010" }, lines.Where(static l => l.StartsWith("model-environment ", StringComparison.Ordinal)));
        var resolutions = lines.Where(static l => l.StartsWith("model-resolution ", StringComparison.Ordinal)).ToList();
        Assert.Equal(nodes.Count(static n => n is Identifier or BindingIdentifier), resolutions.Count);
        var arguments = Reference(on, "arguments");
        Assert.Contains("model-resolution " + Index(arguments) + " 1 - - 0 " + ScopeIndex(Only(model, ScopeKind.With)) + " 0 " + ScopeIndex(Of(model, ScopeKind.Parameters, f)) + " args:" + Index(f), resolutions);
        var off = Parse(text, model: false);
        Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on, model: false)));
        Assert.DoesNotContain("model-resolution", System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(off)), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Resolution and the environment facts (SJL-15.2 stage 5a; the session's decisions D-js-resolution,
    // D-js-environment-facts and D-js-arguments-binding). (Added 2026-11-26, at SJL-15.2 stage 5a.)

    private static FunctionNode Fn(ParseResult result, string name) => First<FunctionNode>(result, f => f.Name?.Name == name);

    private static Declaration Decl(Scope scope, string name, int index = 0) => scope.Declarations.Where(d => d.Name == name).ElementAt(index);

    private static void IsStatic(Resolution r, Scope scope, Declaration declaration)
    {
        Assert.Equal(ResolutionKind.Static, r.Kind);
        Assert.Same(scope, r.Scope);
        Assert.Same(declaration, r.Declaration);
        Assert.Empty(r.DynamicScopes);
        Assert.Null(r.Reason);
        Assert.Null(r.Fallback);
    }

    private static void IsGlobal(Resolution r)
    {
        Assert.Equal(ResolutionKind.Global, r.Kind);
        Assert.Null(r.Scope);
        Assert.Null(r.Declaration);
        Assert.Empty(r.DynamicScopes);
        Assert.Null(r.Reason);
        Assert.Null(r.Fallback);
    }

    private static void IsInherited(Resolution r, Declaration? declaration)
    {
        Assert.Equal(ResolutionKind.Inherited, r.Kind);
        Assert.Null(r.Scope);
        Assert.Same(declaration, r.Declaration);
        Assert.Empty(r.DynamicScopes);
        Assert.Null(r.Reason);
        Assert.Null(r.Fallback);
    }

    // A dynamic resolution with exactly these scopes, innermost first, and that reason; its fallback.
    private static Resolution IsDynamic(Resolution r, DynamicReason reason, params Scope[] scopes)
    {
        Assert.Equal(ResolutionKind.Dynamic, r.Kind);
        Assert.Null(r.Scope);
        Assert.Null(r.Declaration);
        Assert.True(scopes.SequenceEqual(r.DynamicScopes, ReferenceEqualityComparer.Instance), "dynamic scopes " + string.Join(",", r.DynamicScopes.Select(static s => s.Kind)));
        Assert.Equal(reason, r.Reason);
        Assert.NotNull(r.Fallback);
        Assert.NotEqual(ResolutionKind.Dynamic, r.Fallback!.Kind);
        return r.Fallback;
    }

    // A fixture's model, its tree also put through the population's check (Complete) beside the off path's parse, on the
    // documented stack, so that every Identifier and BindingIdentifier of every fixture is checked, not only those the
    // fixture names.
    private static (ParseResult Result, SemanticModel Model) Resolved(string text, ParseGoal goal = ParseGoal.Script)
    {
        var (result, model) = Model(text, goal);
        OnDocumentedStack(() => Complete("fixture \"" + text + "\"", result, Parse(text, goal, model: false)));
        return (result, model);
    }

    private static (ParseResult Result, SemanticModel Model) SloppyEvalModel(string text, bool strict = false)
    {
        ParseResult Parsed(bool model) => SyntaxParser.Parse(new ParseRequest(new SourceText(text), ParseGoal.Eval, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            BuildSemanticModel = model,
            Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = strict },
            Context = new InheritedContext { InFunction = true },
        });
        var result = Parsed(true);
        Assert.True(result.Outcome == ParseOutcome.Accepted, text + ": " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : "-"));
        OnDocumentedStack(() => Complete("fixture (eval code" + (strict ? ", strict" : string.Empty) + ") \"" + text + "\"", result, Parsed(false)));
        return (result, result.Model!);
    }

    /// <summary>Row FF3's probes and vectors (V001 to V008, ff3/0001 to ff3/0009): the arguments binding, its need and its mapping.</summary>
    [Fact]
    public void The_arguments_probes_answer_the_binding_its_need_and_its_mapping()
    {
        // V001, ff3/0001: needed and mapped, the reference to the binding in the parameters' scope.
        var (r, m) = Resolved("__reached = true; function f(a) { arguments[0] = 2; return a; } f(1)");
        var f = Fn(r, "f");
        var env = m.EnvironmentOf(f);
        Assert.NotNull(env.Arguments);
        Assert.Same(f, env.Arguments!.Node);
        Assert.Equal((BindingKind.Arguments, "arguments"), (env.Arguments.Kind, env.Arguments.Name));
        Assert.True(env.ArgumentsNeeded);
        Assert.True(env.ArgumentsMapped);
        IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Parameters, f), env.Arguments);
        IsStatic(m.Resolve(Reference(r, "a")), Of(m, ScopeKind.Parameters, f), Decl(Of(m, ScopeKind.Parameters, f), "a"));
        IsStatic(m.Resolve(Reference(r, "f")), m.Scopes[0], Decl(m.Scopes[0], "f"));
        IsGlobal(m.Resolve(Reference(r, "__reached")));

        // V002, ff3/0002: needed, not mapped.
        (r, m) = Resolved("__reached = true; function f(a) { 'use strict'; arguments[0] = 2; return a; } f(1)");
        env = m.EnvironmentOf(Fn(r, "f"));
        Assert.True(env.ArgumentsNeeded);
        Assert.False(env.ArgumentsMapped);

        // V003, ff3/0003: needed by a direct-eval candidate, whose own callee the function's environment may shadow.
        (r, m) = Resolved("__reached = true; function f() { return eval('arguments.length'); } f(1, 2)");
        f = Fn(r, "f");
        env = m.EnvironmentOf(f);
        Assert.True(env.ArgumentsNeeded);
        Assert.True(env.DirectEvalInBody);
        Assert.False(env.DirectEvalInParameters);
        IsGlobal(IsDynamic(m.Resolve(Reference(r, "eval")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Parameters, f)));

        // V004, ff3/0004: an arrow's reference reads the function's binding; the arrow has none.
        (r, m) = Resolved("__reached = true; function f() { return (() => arguments[1])(); } f(1, 9)");
        f = Fn(r, "f");
        var arrow = First<FunctionNode>(r, static n => n.IsArrow);
        IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Parameters, f), m.EnvironmentOf(f).Arguments!);
        Assert.True(m.EnvironmentOf(f).ArgumentsNeeded);
        Assert.Null(m.EnvironmentOf(arrow).Arguments);
        Assert.False(m.EnvironmentOf(arrow).ArgumentsNeeded);

        // V005, ff3/0005: a mention of eval that is no call is no candidate.
        (r, m) = Resolved("__reached = true; function f() { var e = eval; return e('typeof arguments'); } f(1)");
        env = m.EnvironmentOf(Fn(r, "f"));
        Assert.NotNull(env.Arguments);
        Assert.False(env.ArgumentsNeeded);
        Assert.False(env.DirectEvalInBody);
        IsGlobal(m.Resolve(Reference(r, "eval")));

        // V006, ff3/0006: the reference is the var's, which the arguments object initialises.
        (r, m) = Resolved("__reached = true; function f() { var arguments; return typeof arguments; } f()");
        f = Fn(r, "f");
        IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "arguments"));
        Assert.NotNull(m.EnvironmentOf(f).Arguments);
        Assert.True(m.EnvironmentOf(f).ArgumentsNeeded);

        // V007, V008, ff3/0007, ff3/0008: a function declaration or a lexical declaration of the name, and no binding.
        foreach (var (text, kind) in new[] { ("__reached = true; function f() { function arguments() { return 4; } return arguments(); } f()", BindingKind.Function), ("__reached = true; function f() { let arguments = 6; return arguments; } f()", BindingKind.Let) })
        {
            (r, m) = Resolved(text);
            f = Fn(r, "f");
            Assert.Null(m.EnvironmentOf(f).Arguments);
            Assert.False(m.EnvironmentOf(f).ArgumentsNeeded);
            var declaration = Decl(Of(m, ScopeKind.Function, f), "arguments");
            Assert.Equal(kind, declaration.Kind);
            IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Function, f), declaration);
        }

        // ff3/0009: a parameter of the name, and no binding.
        (r, m) = Resolved("function f(arguments) { return arguments; } f(5)");
        f = Fn(r, "f");
        Assert.Null(m.EnvironmentOf(f).Arguments);
        IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Parameters, f), Decl(Of(m, ScopeKind.Parameters, f), "arguments"));
    }

    /// <summary>Row FF4's probes and vectors (V009 to V011, ff4/0001 to ff4/0004): the separate variable environment and the body's var of a parameter's name.</summary>
    [Fact]
    public void The_separate_variable_environment_probes_answer_their_bindings()
    {
        // V009, ff4/0001: the closure in the parameters skips the body.
        var (r, m) = Resolved("__reached = true; var x = 'outer'; function f(a = () => x) { var x = 'inner'; return a(); } f()");
        var f = Fn(r, "f");
        Assert.True(m.FactsOf(f).HasSeparateVariableEnvironment);
        IsStatic(m.Resolve(Reference(r, "x")), m.Scopes[0], Decl(m.Scopes[0], "x"));

        // V010, ff4/0002: the closure's a is the parameter, the body's the var.
        (r, m) = Resolved("__reached = true; function f(a, b = function () { return a; }) { var a = 2; return a + ',' + b(); } f(1)");
        f = Fn(r, "f");
        IsStatic(m.Resolve(Reference(r, "a", 0)), Of(m, ScopeKind.Parameters, f), Decl(Of(m, ScopeKind.Parameters, f), "a"));
        IsStatic(m.Resolve(Reference(r, "a", 1)), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "a"));

        // V011, ff4/0003 and ff4/0004: the body's a is the var, with a separate environment and without one.
        foreach (var (text, separate) in new[] { ("__reached = true; function f(a = 1) { var a; return a; } f()", true), ("function f(a, b) { var a; return a; } f(3)", false) })
        {
            (r, m) = Resolved(text);
            f = Fn(r, "f");
            Assert.Equal(separate, m.FactsOf(f).HasSeparateVariableEnvironment);
            IsStatic(m.Resolve(Reference(r, "a")), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "a"));
        }
    }

    /// <summary>Row FF5's probes and vectors (V012 to V017, ff5/0001 to ff5/0008): direct eval in a function's own code.</summary>
    [Fact]
    public void The_direct_eval_probes_answer_the_environments_that_may_gain_bindings()
    {
        // V012, ff5/0001: the function shares its parameters' environment, which may gain x.
        var (r, m) = Resolved("__reached = true; var x = 'outer'; function f() { eval('var x = \"inner\"'); return x; } f()");
        var f = Fn(r, "f");
        Assert.True(m.EnvironmentOf(f).DirectEvalInBody);
        Assert.False(m.FactsOf(f).HasSeparateVariableEnvironment);
        IsStatic(IsDynamic(m.Resolve(Reference(r, "x")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Parameters, f)), m.Scopes[0], Decl(m.Scopes[0], "x"));

        // V013, V014, ff5/0002, ff5/0003: no candidate.
        foreach (var text in new[] { "__reached = true; function f() { var x = 1; (0, eval)('var x = 2'); return x; } f()", "__reached = true; function f() { var x = 1; var e = eval; e('var x = 2'); return x; } f()" })
        {
            (r, m) = Resolved(text);
            f = Fn(r, "f");
            Assert.False(m.EnvironmentOf(f).DirectEvalInBody);
            IsStatic(m.Resolve(Reference(r, "x")), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "x"));
        }

        // V015, ff5/0004: the arrow's call is the arrow's.
        (r, m) = Resolved("__reached = true; function f() { var x = 1; (() => eval('var x = 2'))(); return x; } f()");
        f = Fn(r, "f");
        var arrow = First<FunctionNode>(r, static n => n.IsArrow);
        Assert.True(m.EnvironmentOf(arrow).DirectEvalInBody);
        Assert.False(m.EnvironmentOf(f).DirectEvalInBody);
        IsStatic(m.Resolve(Reference(r, "x")), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "x"));
        IsGlobal(IsDynamic(m.Resolve(Reference(r, "eval")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Parameters, arrow)));

        // V016, ff5/0005: a call in a block is the function's; x is bound before the gaining scope.
        (r, m) = Resolved("__reached = true; function f() { var x = 1; { eval('var x = 2'); } return x; } f()");
        f = Fn(r, "f");
        Assert.True(m.EnvironmentOf(f).DirectEvalInBody);
        IsStatic(m.Resolve(Reference(r, "x")), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "x"));

        // ff5/0006: a function the eval declares.
        (r, m) = Resolved("function f() { eval('function g() { return 3; }'); return g(); } f()");
        f = Fn(r, "f");
        IsGlobal(IsDynamic(m.Resolve(Reference(r, "g")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Parameters, f)));

        // V017, ff5/0007: strict, no gaining scope.
        (r, m) = Resolved("__reached = true; function f() { 'use strict'; eval('var y = 1'); return typeof y; } f()");
        Assert.False(m.EnvironmentOf(Fn(r, "f")).DirectEvalInBody);
        IsGlobal(m.Resolve(Reference(r, "y")));

        // ff5/0008: the function's gaining scope is not on the Script's chain.
        (r, m) = Resolved("var y = 0; function f() { eval('var y = 1'); } f(); y");
        IsStatic(m.Resolve(Reference(r, "y")), m.Scopes[0], Decl(m.Scopes[0], "y"));
    }

    /// <summary>Rows RS1 to RS4's probes and vectors (V018 to V022, rs2/0001 to rs2/0003): <c>with</c>, a write through it, the unresolvable name and the constant.</summary>
    [Fact]
    public void The_resolution_probes_answer_with_global_and_the_constant()
    {
        // V018, rs2/0001.
        var (r, m) = Resolved("__reached = true; var x = 'g'; var o = { x: 'o' }; function f() { with (o) { return x; } } f()");
        var with = Of(m, ScopeKind.With, First<WithStatement>(r));
        IsStatic(IsDynamic(m.Resolve(Reference(r, "x")), DynamicReason.With, with), m.Scopes[0], Decl(m.Scopes[0], "x"));
        Assert.True(m.EnvironmentOf(Fn(r, "f")).ContainsWith);

        // V019, rs2/0002: the var's binding with an initialiser is looked up through the object environment.
        (r, m) = Resolved("__reached = true; var o = { x: 1 }; with (o) { var x = 2; } o.x");
        with = Of(m, ScopeKind.With, First<WithStatement>(r));
        IsStatic(IsDynamic(m.Resolve(Binding(r, "x")), DynamicReason.With, with), m.Scopes[0], Decl(m.Scopes[0], "x"));
        Assert.True(m.EnvironmentOf(r.Tree!).ContainsWith);

        // rs2/0003.
        (r, m) = Resolved("function f(o) { with (o) { return typeof y; } } f({ y: 1 })");
        IsGlobal(IsDynamic(m.Resolve(Reference(r, "y")), DynamicReason.With, Of(m, ScopeKind.With, First<WithStatement>(r))));

        // V020, V021.
        (r, m) = Resolved("__reached = true; typeof undeclaredName");
        IsGlobal(m.Resolve(Reference(r, "undeclaredName")));
        (r, m) = Resolved("__reached = true; undeclaredName");
        IsGlobal(m.Resolve(Reference(r, "undeclaredName")));

        // V022: the constant, RS4 read from Resolve and IsMutable.
        (r, m) = Resolved("__reached = true; const c = 1; var r; try { c = 2; } catch (e) { r = e.name; } r");
        var resolution = m.Resolve(Reference(r, "c"));
        IsStatic(resolution, m.Scopes[0], Decl(m.Scopes[0], "c"));
        Assert.False(resolution.Declaration!.IsMutable);
    }

    /// <summary>
    /// The constructs: parameters against the body, a named function expression's name, a class's inner name, a catch
    /// parameter under Annex B.3.4, <c>with</c>, a Script's and a Module's declarations, eval code, the dynamic function's
    /// goals, the gaining scopes of a call in the parameters and in the body, and R-a for a function declaration.
    /// </summary>
    [Fact]
    public void Each_construct_resolves_as_its_environments_say()
    {
        // The closure in the parameters sees the outer x, not the body's.
        var (r, m) = Resolved("var x; function f(a = () => x) { var x; }");
        IsStatic(m.Resolve(Reference(r, "x")), m.Scopes[0], Decl(m.Scopes[0], "x"));

        // A named function expression's own name, and one named arguments, which the binding shadows.
        (r, m) = Resolved("(function g() { return g; });");
        var g = Fn(r, "g");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.FunctionName, g), Decl(Of(m, ScopeKind.FunctionName, g), "g"));
        (r, m) = Resolved("(function arguments() { return arguments; });");
        var named = Fn(r, "arguments");
        IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Parameters, named), m.EnvironmentOf(named).Arguments!);

        // A class's inner name in its heritage and its field initialiser, and the outer name outside.
        (r, m) = Resolved("let D = class C extends (() => C, Object) { x = C; }; C;");
        var classScope = Of(m, ScopeKind.ClassBody, First<ClassNode>(r));
        IsStatic(m.Resolve(Reference(r, "C", 0)), classScope, Decl(classScope, "C"));
        IsStatic(m.Resolve(Reference(r, "C", 1)), classScope, Decl(classScope, "C"));
        IsGlobal(m.Resolve(Reference(r, "C", 2)));
        (r, m) = Resolved("class K { m() { return K; } } K;");
        classScope = Of(m, ScopeKind.ClassBody, First<ClassNode>(r));
        IsStatic(m.Resolve(Reference(r, "K", 0)), classScope, Decl(classScope, "K"));
        IsStatic(m.Resolve(Reference(r, "K", 1)), m.Scopes[0], Decl(m.Scopes[0], "K"));
        IsStatic(m.Resolve(Binding(r, "K")), m.Scopes[0], Decl(m.Scopes[0], "K"));

        // Annex B.3.4: the catch parameter, for the reference and for the var's binding with an initialiser.
        (r, m) = Resolved("try {} catch (e) { var e; e; }");
        var catchScope = Only(m, ScopeKind.Catch);
        IsStatic(m.Resolve(Reference(r, "e")), catchScope, Decl(catchScope, "e"));
        (r, m) = Resolved("try {} catch (e) { var e = 1; }");
        catchScope = Only(m, ScopeKind.Catch);
        IsStatic(m.Resolve(Binding(r, "e", 1)), catchScope, Decl(catchScope, "e"));
        IsStatic(m.Resolve(Binding(r, "e", 0)), catchScope, Decl(catchScope, "e"));

        // with: the body's var, and a for-in head's binding without an initialiser.
        (r, m) = Resolved("function f(o) { with (o) { var x; x; } }");
        var f = Fn(r, "f");
        var with = Only(m, ScopeKind.With);
        IsStatic(IsDynamic(m.Resolve(Reference(r, "x")), DynamicReason.With, with), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "x"));
        IsStatic(m.Resolve(Binding(r, "x")), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "x"));
        (r, m) = Resolved("function f(o) { with (o) { for (var x in o); } }");
        f = Fn(r, "f");
        IsStatic(IsDynamic(m.Resolve(Binding(r, "x")), DynamicReason.With, Only(m, ScopeKind.With)), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "x"));

        // A Script's lexical and var declarations, an undeclared name, and a Module's import.
        (r, m) = Resolved("let l; var v; l; v; typeof undeclared;");
        IsStatic(m.Resolve(Reference(r, "l")), m.Scopes[0], Decl(m.Scopes[0], "l"));
        IsStatic(m.Resolve(Reference(r, "v")), m.Scopes[0], Decl(m.Scopes[0], "v"));
        IsGlobal(m.Resolve(Reference(r, "undeclared")));
        (r, m) = Resolved("import i from \"m\"; i; j;", ParseGoal.Module);
        IsStatic(m.Resolve(Reference(r, "i")), m.Scopes[0], Decl(m.Scopes[0], "i"));
        IsStatic(m.Resolve(Binding(r, "i")), m.Scopes[0], Decl(m.Scopes[0], "i"));
        IsGlobal(m.Resolve(Reference(r, "j")));

        // Eval code: an undeclared name inherited with no declaration; sloppy code's var inherited with it, for the
        // reference and the binding; strict code's var and sloppy code's let static.
        (r, m) = SloppyEvalModel("var x; x; y; let z; z;");
        IsInherited(m.Resolve(Reference(r, "y")), null);
        IsInherited(m.Resolve(Reference(r, "x")), Decl(m.Scopes[0], "x"));
        IsInherited(m.Resolve(Binding(r, "x")), Decl(m.Scopes[0], "x"));
        IsStatic(m.Resolve(Reference(r, "z")), m.Scopes[0], Decl(m.Scopes[0], "z"));
        IsStatic(m.Resolve(Binding(r, "z")), m.Scopes[0], Decl(m.Scopes[0], "z"));
        (r, m) = SloppyEvalModel("function h() {} h;");
        IsInherited(m.Resolve(Reference(r, "h")), Decl(m.Scopes[0], "h"));
        (r, m) = SloppyEvalModel("var x; x; y;", strict: true);
        IsStatic(m.Resolve(Reference(r, "x")), m.Scopes[0], Decl(m.Scopes[0], "x"));
        IsStatic(m.Resolve(Binding(r, "x")), m.Scopes[0], Decl(m.Scopes[0], "x"));
        IsInherited(m.Resolve(Reference(r, "y")), null);
        Assert.True(m.FactsOf(r.Tree!).HasSeparateVariableEnvironment);

        // The dynamic function's goals: each piece with the other empty.
        (r, m) = Resolved("a, b = a, c = d", ParseGoal.FormalParameters);
        var parameters = m.Scopes[0];
        Assert.Equal(ScopeKind.Parameters, parameters.Kind);
        IsStatic(m.Resolve(Reference(r, "a")), parameters, Decl(parameters, "a"));
        IsGlobal(m.Resolve(Reference(r, "d")));
        Assert.True(m.FactsOf(r.Tree!).HasSeparateVariableEnvironment);
        Assert.NotNull(m.EnvironmentOf(r.Tree!).Arguments);
        (r, m) = Resolved("var q; return a + q + arguments.length;", ParseGoal.FunctionBody);
        var body = Of(m, ScopeKind.Function, r.Tree!);
        IsGlobal(m.Resolve(Reference(r, "a")));
        IsStatic(m.Resolve(Reference(r, "q")), body, Decl(body, "q"));
        var programArguments = m.EnvironmentOf(r.Tree!).Arguments;
        Assert.NotNull(programArguments);
        Assert.Same(r.Tree, programArguments!.Node);
        IsStatic(m.Resolve(Reference(r, "arguments")), m.Scopes[0], programArguments);
        Assert.True(m.EnvironmentOf(r.Tree!).ArgumentsNeeded);
        Assert.False(m.FactsOf(r.Tree!).HasSeparateVariableEnvironment);

        // A call in the parameters: calleeEnv, which the Parameters entry stands for, between the parameters and the name.
        (r, m) = Resolved("(function f(a = eval(\"\")) { return f; });");
        f = Fn(r, "f");
        Assert.True(m.EnvironmentOf(f).DirectEvalInParameters);
        Assert.False(m.EnvironmentOf(f).DirectEvalInBody);
        Assert.True(m.FactsOf(f).HasSeparateVariableEnvironment);
        IsStatic(IsDynamic(m.Resolve(Reference(r, "f")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Parameters, f)), Of(m, ScopeKind.FunctionName, f), Decl(Of(m, ScopeKind.FunctionName, f), "f"));

        // A call in the body: the parameter is the eval's var's binding where the environment is shared, and the body's
        // own environment gains where it is separate.
        (r, m) = Resolved("function f(a) { eval(\"\"); return a; }");
        f = Fn(r, "f");
        IsStatic(m.Resolve(Reference(r, "a")), Of(m, ScopeKind.Parameters, f), Decl(Of(m, ScopeKind.Parameters, f), "a"));
        (r, m) = Resolved("function f(a = 1) { eval(\"\"); return a; }");
        f = Fn(r, "f");
        IsStatic(IsDynamic(m.Resolve(Reference(r, "a")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Function, f)), Of(m, ScopeKind.Parameters, f), Decl(Of(m, ScopeKind.Parameters, f), "a"));

        // R-a for a function declaration of a parameter's name, with the environment shared and separate.
        foreach (var text in new[] { "function f(a) { function a() {} return a; }", "function f(a = 1) { function a() {} return a; }" })
        {
            (r, m) = Resolved(text);
            f = Fn(r, "f");
            var declaration = Decl(Of(m, ScopeKind.Function, f), "a");
            Assert.Equal(BindingKind.Function, declaration.Kind);
            IsStatic(m.Resolve(Reference(r, "a")), Of(m, ScopeKind.Function, f), declaration);
        }
    }

    /// <summary>The candidate rule, own code, the arguments object's need through an eval candidate, and <c>with</c>.</summary>
    [Fact]
    public void Each_function_s_environment_facts_are_its_own_code_s()
    {
        foreach (var (text, candidate) in new[] { ("function f() { (eval)(\"\"); }", true), ("function f() { ((eval))(\"\"); }", true), ("function f() { eval?.(\"\"); }", false), ("function f() { (0, eval)(\"\"); }", false), ("function f() { new eval(); }", false), ("function f() { eval`x`; }", false), ("function f() { o.eval(\"\"); }", false) })
        {
            var (fr, fm) = Resolved(text);
            Assert.True(candidate == fm.EnvironmentOf(Fn(fr, "f")).DirectEvalInBody, text);
            Assert.True(candidate == fm.EnvironmentOf(Fn(fr, "f")).ArgumentsNeeded, text);
        }

        // A class's computed key is the enclosing function's strict code: no gaining, but the eval code may read arguments.
        var (r, m) = Resolved("function f() { class C { [eval(\"\")]() {} } }");
        var f = Fn(r, "f");
        Assert.False(m.EnvironmentOf(f).DirectEvalInBody);
        Assert.True(m.EnvironmentOf(f).ArgumentsNeeded);
        Assert.True(m.EnvironmentOf(f).ArgumentsMapped);

        // An arrow's call is the arrow's, though it reaches f's x by name at run time.
        (r, m) = Resolved("function f() { var x; (() => eval(\"x = 1\"))(); }");
        f = Fn(r, "f");
        Assert.False(m.EnvironmentOf(f).DirectEvalInBody);
        Assert.True(m.EnvironmentOf(First<FunctionNode>(r, static n => n.IsArrow)).DirectEvalInBody);
        Assert.True(m.EnvironmentOf(f).ArgumentsNeeded);

        // A static block's binding, needed by its eval; a field initialiser has none.
        (r, m) = Resolved("class C { static { eval(\"arguments\"); } x = 1; y; }");
        var block = First<StaticBlock>(r);
        Assert.NotNull(m.EnvironmentOf(block).Arguments);
        Assert.Same(block, m.EnvironmentOf(block).Arguments!.Node);
        Assert.True(m.EnvironmentOf(block).ArgumentsNeeded);
        Assert.False(m.EnvironmentOf(block).DirectEvalInBody);
        var field = First<FieldDefinition>(r);
        Assert.Null(m.EnvironmentOf(field).Arguments);
        Assert.False(m.EnvironmentOf(field).ArgumentsNeeded);
        Assert.False(m.FactsOf(field).HasSeparateVariableEnvironment);
        Assert.False(m.FactsOf(block).HasSeparateVariableEnvironment);

        // Needed through a dynamic resolution's fallback to the var of the name.
        (r, m) = Resolved("function f(o) { var arguments; with (o) { arguments; } }");
        f = Fn(r, "f");
        IsStatic(IsDynamic(m.Resolve(Reference(r, "arguments")), DynamicReason.With, Only(m, ScopeKind.With)), Of(m, ScopeKind.Function, f), Decl(Of(m, ScopeKind.Function, f), "arguments"));
        Assert.True(m.EnvironmentOf(f).ArgumentsNeeded);

        // A binding's write does not observe the object.
        (r, m) = Resolved("function f() { var arguments = 1; }");
        Assert.False(m.EnvironmentOf(Fn(r, "f")).ArgumentsNeeded);

        // with in own code, an arrow's being the arrow's.
        (r, m) = Resolved("function f(o) { with (o) {} }");
        Assert.True(m.EnvironmentOf(Fn(r, "f")).ContainsWith);
        (r, m) = Resolved("function f(o) { (() => { with (o) {} })(); }");
        Assert.False(m.EnvironmentOf(Fn(r, "f")).ContainsWith);
        Assert.True(m.EnvironmentOf(First<FunctionNode>(r, static n => n.IsArrow)).ContainsWith);
    }

    /// <summary>Several declarations of one name make one binding, and every answer names the first.</summary>
    [Fact]
    public void Several_declarations_of_one_name_answer_the_first()
    {
        var (r, m) = Resolved("var v; var v; v;");
        IsStatic(m.Resolve(Reference(r, "v")), m.Scopes[0], Decl(m.Scopes[0], "v", 0));
        (r, m) = Resolved("var v; var v = 1; v;");
        var first = Decl(m.Scopes[0], "v", 0);
        IsStatic(m.Resolve(Binding(r, "v", 0)), m.Scopes[0], first);
        IsStatic(m.Resolve(Binding(r, "v", 1)), m.Scopes[0], first);
        IsStatic(m.Resolve(Reference(r, "v")), m.Scopes[0], first);
        (r, m) = Resolved("function g() { return 1 } function g() { return 2 } g;");
        IsStatic(m.Resolve(Reference(r, "g")), m.Scopes[0], Decl(m.Scopes[0], "g", 0));
        IsStatic(m.Resolve(Binding(r, "g", 1)), m.Scopes[0], Decl(m.Scopes[0], "g", 0));
        (r, m) = Resolved("function f(a, a) { return a; }");
        var parameters = Of(m, ScopeKind.Parameters, Fn(r, "f"));
        IsStatic(m.Resolve(Binding(r, "a", 0)), parameters, Decl(parameters, "a", 0));
        IsStatic(m.Resolve(Binding(r, "a", 1)), parameters, Decl(parameters, "a", 0));
        IsStatic(m.Resolve(Reference(r, "a")), parameters, Decl(parameters, "a", 0));
    }

    /// <summary>A null node, a node of another tree, and a node without facts are refused, by each overload by a cast.</summary>
    [Fact]
    public void A_null_or_foreign_reference_or_a_node_without_facts_is_refused()
    {
        var (result, model) = Resolved("var a = 1; a; function f(b) { return b; } class C { x; y = 1; }");
        var (other, _) = Resolved("var a = 1; a; function f(b) { return b; } class C { x; y = 1; }");
        Assert.Throws<ArgumentNullException>(() => model.Resolve((Identifier)null!));
        Assert.Throws<ArgumentNullException>(() => model.Resolve((BindingIdentifier)null!));
        Assert.Throws<ArgumentNullException>(() => model.EnvironmentOf(null!));
        Assert.Throws<ArgumentException>(() => model.Resolve(Reference(other, "a")));
        Assert.Throws<ArgumentException>(() => model.Resolve(Binding(other, "a")));
        Assert.Throws<ArgumentException>(() => model.EnvironmentOf(First<FunctionNode>(other)));
        Assert.Throws<ArgumentException>(() => model.EnvironmentOf(First<ReturnStatement>(result)));
        Assert.Throws<ArgumentException>(() => model.EnvironmentOf(First<FieldDefinition>(result)));
        Assert.NotNull(model.EnvironmentOf(First<FieldDefinition>(result, static d => d.Value is not null)));
        Assert.NotNull(model.EnvironmentOf(result.Tree!));
    }

    // Accepted sources for resolution and the environment facts, beside the population and the fixtures (which Resolved and
    // SloppyEvalModel put through the same check): further probes' sources and constructs.
    private static readonly (ParseGoal Goal, string Text)[] ResolutionSources =
    [
        (ParseGoal.Script, "function f(a) { arguments[0] = 2; return a; } f(1)"), (ParseGoal.Script, "function f() { return eval('arguments.length'); } f(1, 2)"),
        (ParseGoal.Script, "function f() { return (() => arguments[1])(); } f(1, 9)"), (ParseGoal.Script, "function f() { var arguments; return typeof arguments; } f()"),
        (ParseGoal.Script, "function f() { function arguments() { return 4; } return arguments(); } f()"), (ParseGoal.Script, "function f() { let arguments = 6; return arguments; } f()"),
        (ParseGoal.Script, "var x = 'outer'; function f(a = () => x) { var x = 'inner'; return a(); } f()"), (ParseGoal.Script, "function f(a, b = function () { return a; }) { var a = 2; return a + ',' + b(); } f(1)"),
        (ParseGoal.Script, "var x = 'outer'; function f() { eval('var x = \"inner\"'); return x; } f()"), (ParseGoal.Script, "function f() { var x = 1; (() => eval('var x = 2'))(); return x; } f()"),
        (ParseGoal.Script, "var o = { x: 1 }; with (o) { var x = 2; } o.x"), (ParseGoal.Script, "function f(o) { with (o) { for (var x in o); for (var [y, z = x] of o); } }"),
        (ParseGoal.Script, "(function f(a = eval(\"\")) { return f; });"), (ParseGoal.Script, "function f(a = 1) { eval(\"\"); { let a; a; } return a; }"),
        (ParseGoal.Script, "function f(o) { var arguments; with (o) { arguments; } }"), (ParseGoal.Script, "class C { static { eval(\"arguments\"); } x = () => eval(\"1\"); [eval(\"k\")] = 1; }"),
        (ParseGoal.Script, "try {} catch (e) { var e = 1; e; }"), (ParseGoal.Script, "function f() { class C extends (eval(\"\"), Object) { m() { return arguments; } } }"),
        (ParseGoal.Script, "function f(a = (b = eval(\"\")) => b) { return a; }"), (ParseGoal.Script, "function f(a, a) { return a; } var v; var v = 1; v;"),
        (ParseGoal.Script, "x = function g() { with (g) { return (function () { eval(\"\"); return g; })(); } };"),
        (ParseGoal.Module, "import i from \"m\"; export var e = i; export default function () { return e; }"),
        (ParseGoal.FormalParameters, "a, b = a, c = eval(\"\"), d = arguments"), (ParseGoal.FunctionBody, "var q; eval(\"\"); return a + q + arguments.length;"),
        (ParseGoal.Eval, "var x; x; function h() { eval(\"\"); return x + y; } let z; z; with (o) { x; }"),
    ];

    // One accepted tree's resolutions and environment facts, each as the lookup over the model's scopes and an oracle of the
    // test's own require, with the off path's tables beside them: the check the population and every fixture pass through.
    // It returns the references, bindings, dynamic answers and facts nodes it checked.
    // From stage 5b it also puts the tree through CompleteAliases, and its lookup counts the aliases a variable scope binds
    // and the per-name gain of arguments. (Added 2026-11-26, at SJL-15.2 stage 5b.)
    // From stage 5c it takes the request's caller's records, for eval code: where a record binds the name, an eval-code
    // variable's answer for a reference, a binding resolved when it runs or a dynamic answer's fallback carries no
    // declaration, every other binding keeping the eval body's; and CompleteAliases withholds an alias a record that is not
    // a Catch clause's binds. Null, the default, is every earlier stage's check. (Added 2026-11-26, at SJL-15.2 stage 5c.)
    // (Revised 2026-11-26, at SJL-15.2 stage 5c, when the eval-var case gained a caller's record of the name; the
    // signature ended "ParseResult off)", the alias call read "CompleteAliases(name, on, off);", and the eval-var check
    // began "if (EvalVar(chain[i], declaration))".)
    private static (int References, int Bindings, int Dynamic, int Functions) Complete(string name, ParseResult on, ParseResult off, IReadOnlyList<CallerScope>? callers = null)
    {
        var (refs, binds, dyn, fns) = (0, 0, 0, 0);
        if (on.Model is not { } model)
            return default;
        var tree = on.Tree!;
        var unit = model.FactsOf(tree);
        var nodes = Nodes(tree);
        var parents = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
        var children = new List<SyntaxNode>();
        foreach (var node in nodes)
        {
            children.Clear();
            node.AddChildren(children);
            foreach (var child in children)
                parents[child] = node;
        }

        // The tables, on and off: no arguments binding, and the same separate-environment facts.
        Assert.DoesNotContain(on.Declarations!.Scopes.SelectMany(static s => s.Declarations), static d => d.Kind == BindingKind.Arguments);
        Assert.DoesNotContain(off.Declarations!.Scopes.SelectMany(static s => s.Declarations), static d => d.Kind == BindingKind.Arguments);
        Assert.DoesNotContain(model.Scopes.SelectMany(static s => s.Declarations), static d => d.Kind == BindingKind.Arguments);
        Assert.True(on.Declarations.Functions.Select(static f => f.HasSeparateVariableEnvironment).SequenceEqual(off.Declarations!.Functions.Select(static f => f.HasSeparateVariableEnvironment)), name + ": the separate environment differs off and on");
        foreach (var facts in model.Functions)
            Assert.Equal(facts.Kind == FunctionKind.EvalCode ? facts.IsStrict : facts.HasParameterExpressions, facts.HasSeparateVariableEnvironment);

        // Each declaration's scope, and each facts node's parameters', body's and block's scopes, by identity.
        var holder = new Dictionary<Declaration, Scope>(ReferenceEqualityComparer.Instance);
        var scopeByKind = new Dictionary<(ScopeKind, SyntaxNode), Scope>();
        foreach (var scope in model.Scopes)
        {
            foreach (var declaration in scope.Declarations)
                holder.TryAdd(declaration, scope);
            if (scope.Kind is ScopeKind.Parameters or ScopeKind.Function or ScopeKind.StaticBlock)
                scopeByKind[(scope.Kind, scope.Node)] = scope;
        }

        // The oracle: the facts node whose own code holds a node, whether it is in that node's parameters, and the
        // code's strictness (a class's parts but its decorators being strict).
        var factsNodes = new HashSet<SyntaxNode>(model.Functions.Select(static f => f.Node), ReferenceEqualityComparer.Instance);
        (SyntaxNode Owner, bool InParameters, bool Strict) OwnerOf(SyntaxNode node)
        {
            var inClass = false;
            for (var at = node; ; )
            {
                var parent = parents[at];
                var owns = parent switch
                {
                    FunctionNode or StaticBlock or ProgramNode => true,
                    FieldDefinition field => ReferenceEquals(at, field.Value),
                    _ => false,
                };
                if (owns)
                {
                    var inParameters = parent switch
                    {
                        FunctionNode function => function.Parameters.Contains(at),
                        ProgramNode program => program.Parameters.Contains(at),
                        _ => false,
                    };
                    return (parent, inParameters, inClass || model.FactsOf(parent).IsStrict);
                }

                if (parent is ClassNode @class && !@class.Decorators.Contains(at))
                    inClass = true;
                at = parent;
            }
        }

        SyntaxNode ReaderOf(SyntaxNode owner)
        {
            while (owner is FunctionNode { IsArrow: true })
                owner = OwnerOf(owner).Owner;
            return owner;
        }

        var inParams = new HashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);
        var inBody = new HashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);
        var readers = new HashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);
        var withs = new HashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);
        foreach (var node in nodes)
        {
            if (node is WithStatement)
                withs.Add(OwnerOf(node).Owner);
            if (node is not CallExpression { IsOptional: false } call)
                continue;
            var callee = call.Callee;
            while (callee is ParenthesizedExpression p)
                callee = p.Expression;
            if (callee is not Identifier { Name: "eval" })
                continue;
            var (owner, parameters, strict) = OwnerOf(call);
            if (!strict)
                (parameters ? inParams : inBody).Add(owner);
            readers.Add(ReaderOf(owner));
        }

        foreach (var facts in model.Functions)
        {
            var env = model.EnvironmentOf(facts.Node);
            Assert.Same(facts.Node, env.Node);
            Assert.True(inParams.Contains(facts.Node) == env.DirectEvalInParameters, name + ": DirectEvalInParameters at " + facts.Node.Span.Offset.ToString(CultureInfo.InvariantCulture));
            Assert.True(inBody.Contains(facts.Node) == env.DirectEvalInBody, name + ": DirectEvalInBody at " + facts.Node.Span.Offset.ToString(CultureInfo.InvariantCulture));
            Assert.True(withs.Contains(facts.Node) == env.ContainsWith, name + ": ContainsWith at " + facts.Node.Span.Offset.ToString(CultureInfo.InvariantCulture));
            Assert.True(!env.DirectEvalInParameters || facts.HasParameterExpressions, name + ": a call in parameters without expressions");
        }

        // The gaining scopes, by section 3.4's rule from the facts.
        var gaining = new HashSet<Scope>(ReferenceEqualityComparer.Instance);
        foreach (var facts in model.Functions)
        {
            if (!scopeByKind.TryGetValue((ScopeKind.Parameters, facts.Node), out var parameters))
                continue;
            var env = model.EnvironmentOf(facts.Node);
            if (env.DirectEvalInBody)
                gaining.Add(facts.HasSeparateVariableEnvironment ? scopeByKind[(ScopeKind.Function, facts.Node)] : parameters);
            if (env.DirectEvalInParameters)
                gaining.Add(parameters);
        }

        // The Annex B aliases a sloppy function's or sloppy eval code's variable scope binds, and the scopes that may gain
        // arguments by a block function's evaluation, from the model's answers, which CompleteAliases checks against an
        // oracle of the tree. (Added 2026-11-26, at SJL-15.2 stage 5b.)
        CompleteAliases(name, on, off, callers: callers);
        var aliasBinds = new Dictionary<(Scope, string), Declaration>();
        var argumentsGains = new HashSet<Scope>(ReferenceEqualityComparer.Instance);
        foreach (var scope in model.Scopes)
        {
            foreach (var declaration in scope.Declarations)
            {
                if (model.AnnexBAlias(declaration) is not { Kind: BindingKind.BlockFunctionAlias } alias)
                    continue;
                var varScope = VarScopeOf(model, scope);
                var facts = model.FactsOf(varScope.Node);
                if (facts.Kind is FunctionKind.Script)
                    continue;
                if (facts.Kind is not FunctionKind.EvalCode && alias.Name == "arguments")
                    argumentsGains.Add(facts.HasSeparateVariableEnvironment ? varScope : scopeByKind[(ScopeKind.Parameters, varScope.Node)]);
                else
                    aliasBinds[(varScope, alias.Name)] = alias;
            }
        }

        // What a scope binds, the test's own reading.
        // (Revised 2026-11-26, at SJL-15.2 stage 5b, when a variable scope's aliases joined it; it ended "return first;"
        // after the arguments binding.)
        Declaration? Binds(Scope scope, string n)
        {
            var first = scope.Declarations.FirstOrDefault(d => d.Name == n);
            if (first is null && n == "arguments" && scope.Kind is ScopeKind.Parameters or ScopeKind.StaticBlock && factsNodes.Contains(scope.Node))
                first = model.EnvironmentOf(scope.Node).Arguments;
            if (first is null && aliasBinds.TryGetValue((scope, n), out var alias))
                first = alias;
            return first;
        }

        var sloppyEval = unit.Kind == FunctionKind.EvalCode && !unit.IsStrict;
        bool EvalVar(Scope scope, Declaration declaration) => sloppyEval && ReferenceEquals(scope, model.Scopes[0]) && !declaration.IsLexical;

        // The names a caller's record the request gives binds, which the lookup of an eval-code variable reaches first.
        // (Added 2026-11-26, at SJL-15.2 stage 5c.)
        var held = new HashSet<string>(callers is null ? Enumerable.Empty<string>() : callers.SelectMany(static c => c.Names), StringComparer.Ordinal);

        // The answer the lookup requires, checked member by member.
        void Check(SyntaxNode node, string n, Resolution answer, string at)
        {
            var chain = new List<Scope>();
            for (Scope? s = model.ScopeOf(node); s is not null; s = model.ParentOf(s))
                chain.Add(s);
            var expected = new List<Scope>();
            Resolution end;
            var i = 0;
            for (; i < chain.Count; i++)
            {
                if (chain[i].Kind == ScopeKind.With)
                {
                    expected.Add(chain[i]);
                    continue;
                }

                if (Binds(chain[i], n) is not null)
                    break;
                if (gaining.Contains(chain[i]))
                    expected.Add(chain[i]);
                else if (n == "arguments" && argumentsGains.Contains(chain[i]))
                    expected.Add(chain[i]);
            }

            if (answer.Kind == ResolutionKind.Dynamic)
            {
                // The innermost entry's reason: With, then a sloppy direct eval's gain, then a block function's
                // arguments. (Revised 2026-11-26, at SJL-15.2 stage 5b, when BlockFunction joined the reasons; it read
                // "(answer.Reason == DynamicReason.With) == (answer.DynamicScopes[0].Kind == ScopeKind.With)".)
                Assert.True(expected.Count > 0 && expected.SequenceEqual(answer.DynamicScopes, ReferenceEqualityComparer.Instance), at + ": the dynamic scopes");
                var innermost = answer.DynamicScopes[0];
                var reason = innermost.Kind == ScopeKind.With ? DynamicReason.With : gaining.Contains(innermost) ? DynamicReason.SloppyDirectEval : DynamicReason.BlockFunction;
                Assert.True(answer.Reason == reason, at + ": the reason");
                Assert.True(answer.Scope is null && answer.Declaration is null && answer.Fallback is { Kind: not ResolutionKind.Dynamic }, at + ": a dynamic answer's members");
                end = answer.Fallback!;
            }
            else
            {
                Assert.True(expected.Count == 0 && answer.DynamicScopes.Count == 0 && answer.Reason is null && answer.Fallback is null, at + ": not dynamic where it should be");
                end = answer;
            }

            if (i < chain.Count)
            {
                var declaration = Binds(chain[i], n)!;
                if (EvalVar(chain[i], declaration) && held.Contains(n))
                    Assert.True(end.Kind == ResolutionKind.Inherited && end.Scope is null && end.Declaration is null, at + ": eval code's var under a caller's record of its name");
                else if (EvalVar(chain[i], declaration))
                    Assert.True(end.Kind == ResolutionKind.Inherited && end.Scope is null && ReferenceEquals(end.Declaration, declaration), at + ": eval code's var");
                else
                    Assert.True(end.Kind == ResolutionKind.Static && ReferenceEquals(end.Scope, chain[i]) && ReferenceEquals(end.Declaration, declaration), at + ": not the first binding scope's first declaration");
            }
            else
            {
                Assert.True(end.Kind == (unit.Kind == FunctionKind.EvalCode ? ResolutionKind.Inherited : ResolutionKind.Global) && end.Scope is null && end.Declaration is null, at + ": the chain's end");
            }

            if (end.Declaration is { } d)
                Assert.Equal(n, d.Name);
        }

        // The bindings the language resolves when it runs: a var declarator's with an initialiser, and a for-in
        // or for-of var head's.
        bool RunTime(BindingIdentifier binding)
        {
            SyntaxNode at = binding;
            while (parents.TryGetValue(at, out var parent) && parent is ArrayPattern or ObjectPattern or PatternProperty or RestElement or AssignmentPattern)
            {
                if (parent is AssignmentPattern assignment && !ReferenceEquals(assignment.Target, at))
                    return false;
                at = parent;
            }

            return parents.TryGetValue(at, out var declarator) && declarator is VariableDeclarator d && ReferenceEquals(d.Target, at)
                && parents[d] is VariableDeclaration { DeclarationKind: DeclarationKind.Var } v
                && (d.Initializer is not null || parents.TryGetValue(v, out var loop) && (loop is ForInStatement fi && ReferenceEquals(fi.Left, v) || loop is ForOfStatement fo && ReferenceEquals(fo.Left, v)));
        }

        var read = new HashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);
        foreach (var node in nodes)
        {
            var at = name + ": " + node.GetType().Name + " at " + node.Span.Offset.ToString(CultureInfo.InvariantCulture);
            if (node is Identifier reference)
            {
                refs++;
                var answer = model.Resolve(reference);
                Assert.Null(model.DeclarationOf(reference));
                Check(reference, reference.Name, answer, at);
                if (answer.Kind == ResolutionKind.Dynamic)
                    dyn++;
                if ((answer.Kind == ResolutionKind.Dynamic ? answer.Fallback! : answer) is { Kind: ResolutionKind.Static, Declaration: { } d, Scope: { } s })
                {
                    if (d.Kind == BindingKind.Arguments)
                        read.Add(d.Node);
                    else if (d.Kind == BindingKind.Var && d.Name == "arguments" && s.Kind is ScopeKind.Function or ScopeKind.StaticBlock)
                        read.Add(s.Node);
                }

                Assert.True(answer.Kind != ResolutionKind.Global || unit.Kind is FunctionKind.Script or FunctionKind.Module or FunctionKind.DynamicFunction, at + ": global outside a Script, Module or dynamic goal");
                Assert.True(answer.Kind != ResolutionKind.Inherited || unit.Kind == FunctionKind.EvalCode, at + ": inherited outside eval code");
            }
            else if (node is BindingIdentifier binding)
            {
                binds++;
                var answer = model.Resolve(binding);
                if (RunTime(binding))
                {
                    Check(binding, binding.Name, answer, at + " (run-time binding)");
                    continue;
                }

                var own = model.DeclarationOf(binding)!;
                var scope = holder[own];
                var first = Binds(scope, binding.Name)!;
                if (EvalVar(scope, first))
                    IsInherited(answer, first);
                else
                    IsStatic(answer, scope, first);
            }
        }

        // The arguments binding exactly where FunctionDeclarationInstantiation's steps 15 to 18 create one, its need
        // by a reference or a candidate, and its mapping.
        foreach (var facts in model.Functions)
        {
            fns++;
            var env = model.EnvironmentOf(facts.Node);
            var bodyScope = facts.Kind is FunctionKind.StaticBlock ? scopeByKind[(ScopeKind.StaticBlock, facts.Node)]
                : facts.Kind is FunctionKind.FieldInitializer or FunctionKind.Script or FunctionKind.Module or FunctionKind.EvalCode ? null
                : scopeByKind[(ScopeKind.Function, facts.Node)];
            var exists = bodyScope is not null && facts.Kind != FunctionKind.Arrow && !facts.ParameterNames.Contains("arguments")
                && (facts.HasParameterExpressions || !bodyScope.Declarations.Any(static d => d.Name == "arguments" && (d.IsLexical || d.Kind == BindingKind.Function)));
            var at = name + ": facts at " + facts.Node.Span.Offset.ToString(CultureInfo.InvariantCulture);
            Assert.True(exists == env.Arguments is not null, at + ": the arguments binding");
            if (env.Arguments is { } arguments)
            {
                Assert.True(arguments.Kind == BindingKind.Arguments && arguments.Name == "arguments" && ReferenceEquals(arguments.Node, facts.Node) && arguments.IsMutable && !arguments.IsLexical, at + ": the binding's members");
                Assert.Null(model.DeclarationOf(arguments.Node));
            }

            var needed = env.Arguments is not null && (read.Contains(facts.Node) || readers.Contains(facts.Node));
            Assert.True(needed == env.ArgumentsNeeded, at + ": the need");
            Assert.True((env.ArgumentsNeeded && !facts.IsStrict && facts.HasSimpleParameterList) == env.ArgumentsMapped, at + ": the mapping");
        }

        return (refs, binds, dyn, fns);
    }

    /// <summary>
    /// Over the determinism tests' population and further sources (each fixture's own tree passing the same check, through
    /// Resolved and SloppyEvalModel): every reference and every binding resolved, each answer as the lookup over the
    /// model's scopes requires, recomputed by the test; the two overloads in agreement; and each facts node's environment
    /// facts as an independent oracle over a parent map recomputes them, iteratively. With the member on and off the
    /// tables hold no <c>arguments</c> binding and the same separate-environment facts.
    /// </summary>
    [Fact]
    public void Every_reference_and_binding_resolves_and_every_function_s_facts_hold_over_the_population()
    {
        var items = DeterminismTests.Population.Value;
        var (references, bindings, dynamic, functions) = OnDocumentedStack(() =>
        {
            var (refs, binds, dyn, fns) = (0, 0, 0, 0);
            void One(string name, ParseResult on, ParseResult off)
            {
                var (r, b, d, f) = Complete(name, on, off);
                (refs, binds, dyn, fns) = (refs + r, binds + b, dyn + d, fns + f);
            }

            foreach (var item in items)
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item, model: true)), SyntaxParser.Parse(DeterminismTests.Request(item)));
            foreach (var (goal, text) in ResolutionSources)
            {
                ParseResult Parsed(bool model) => goal == ParseGoal.Eval
                    ? SyntaxParser.Parse(new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance) { BuildSemanticModel = model, Context = new InheritedContext { InFunction = true } })
                    : Parse(text, goal, model);
                var on = Parsed(true);
                Assert.True(on.Outcome == ParseOutcome.Accepted, text);
                One("resolution source \"" + text + "\"", on, Parsed(false));
            }

            return (refs, binds, dyn, fns);
        });
        Assert.True(references > 10_000 && bindings > 1_000 && dynamic > ResolutionSources.Length && functions > 1_000);
        output.WriteLine("resolved: references " + references.ToString(CultureInfo.InvariantCulture) + ", bindings " + bindings.ToString(CultureInfo.InvariantCulture)
            + ", dynamic " + dynamic.ToString(CultureInfo.InvariantCulture) + "; environments " + functions.ToString(CultureInfo.InvariantCulture));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Annex B's block-function aliases (SJL-15.2 stage 5b; the session's decision D-js-annexb-aliases; ECMA-262 Annex B.3.2,
    // which row AB1 names by an earlier edition's number, B.3.3). (Added 2026-11-26, at SJL-15.2 stage 5b.)

    // A request with Annex B on or off, the rest as given, its dynamic function's kind and meter included.
    private static ParseRequest WithAnnexB(ParseRequest request, bool annexB)
    {
        var copy = new ParseRequest(request.Source, request.Goal, request.UnicodeTables, request.RegularExpressions)
        {
            Switches = request.Switches with { AnnexB = annexB },
            Proposals = request.Proposals,
            Extensions = request.Extensions,
            Limits = request.Limits,
            RetainTokens = request.RetainTokens,
            BuildSemanticModel = request.BuildSemanticModel,
            FunctionKind = request.FunctionKind,
            Meter = request.Meter,
        };
        if (request.Goal == ParseGoal.Eval)
            copy = new ParseRequest(request.Source, request.Goal, request.UnicodeTables, request.RegularExpressions)
            {
                Switches = request.Switches with { AnnexB = annexB },
                Context = request.Context,
                Proposals = request.Proposals,
                Extensions = request.Extensions,
                Limits = request.Limits,
                RetainTokens = request.RetainTokens,
                BuildSemanticModel = request.BuildSemanticModel,
                FunctionKind = request.FunctionKind,
                Meter = request.Meter,
            };
        return copy;
    }

    // The n-th block function of a name, in the model's scopes' and declarations' order.
    private static Declaration BlockFn(SemanticModel model, string name, int index = 0) =>
        model.Scopes.SelectMany(static s => s.Declarations).Where(d => d.IsBlockFunction && d.Name == name).ElementAt(index);

    // The first scope on the chain from a scope that holds its function's, script's, module's or static block's var-scoped
    // names.
    private static Scope VarScopeOf(SemanticModel model, Scope scope)
    {
        for (Scope? s = scope; ; s = model.ParentOf(s!))
        {
            if (s!.Kind is ScopeKind.Function or ScopeKind.Script or ScopeKind.StaticBlock or ScopeKind.Module)
                return s;
        }
    }

    // An answer that is a model-only alias of the function's name: its members, in no scope, answering null itself, and
    // its node one admitted block function's name, whose declaration DeclarationOf answers.
    private static Declaration IsAlias(SemanticModel model, Declaration function)
    {
        var alias = model.AnnexBAlias(function);
        Assert.NotNull(alias);
        Assert.Equal(BindingKind.BlockFunctionAlias, alias!.Kind);
        Assert.Equal(function.Name, alias.Name);
        Assert.False(alias.IsLexical || alias.IsBlockFunction || alias.IsIfClauseFunction);
        Assert.True(alias.IsMutable);
        Assert.DoesNotContain(model.Scopes.SelectMany(static s => s.Declarations), d => ReferenceEquals(d, alias));
        Assert.Null(model.AnnexBAlias(alias));
        Assert.True(model.DeclarationOf(alias.Node) is { IsBlockFunction: true, Kind: BindingKind.Function } own && own.Name == alias.Name);
        return alias;
    }

    private static (ParseResult Result, SemanticModel Model) AnnexBOff(string text)
    {
        var result = SyntaxParser.Parse(WithAnnexB(new ParseRequest(new SourceText(text), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance) { Switches = ParseSwitches.For(ParseGoal.Script), BuildSemanticModel = true }, annexB: false));
        Assert.True(result.Outcome == ParseOutcome.Accepted, text);
        OnDocumentedStack(() => CompleteAliases("fixture (Annex B off) \"" + text + "\"", result, result, annexB: false));
        return (result, result.Model!);
    }

    /// <summary>
    /// Row AB1's vectors (ab1/0001 to ab1/0013) and probes (V023 to V030): each block function's alias answer and the
    /// resolution of every reference to its name; the probes, which ran as indirect-eval code, both as a Script and as eval
    /// code.
    /// </summary>
    [Fact]
    public void The_annex_b_vectors_and_probes_answer_each_block_function_s_alias()
    {
        // ab1/0001, V023: a Script's alias; the reference stays global.
        var (r, m) = Resolved("{ function g() { return 1; } } g()");
        IsAlias(m, BlockFn(m, "g"));
        IsGlobal(m.Resolve(Reference(r, "g")));

        // ab1/0002, V024: a function's alias, which the reference names.
        (r, m) = Resolved("function f() { { function g() { return 5; } } return g(); } f()");
        var alias = IsAlias(m, BlockFn(m, "g"));
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), alias);

        // ab1/0003, V025: before the block, the same alias, undefined until the block runs.
        (r, m) = Resolved("function f() { var t = g; { function g() {} } return t; } f()");
        alias = IsAlias(m, BlockFn(m, "g"));
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), alias);

        // ab1/0004: the inner source under e, a reference added after the block, inherited with the alias.
        (r, m) = SloppyEvalModel("{ function g() { return 4; } } g;");
        alias = IsAlias(m, BlockFn(m, "g"));
        IsInherited(m.Resolve(Reference(r, "g")), alias);

        // ab1/0005, V028: a block function named arguments writes f's arguments binding.
        (r, m) = Resolved("function f() { { function arguments() {} } return typeof arguments; } f()");
        var f = Fn(r, "f");
        var arguments = m.EnvironmentOf(f).Arguments!;
        Assert.Same(arguments, m.AnnexBAlias(BlockFn(m, "arguments")));
        IsStatic(m.Resolve(Reference(r, "arguments")), Of(m, ScopeKind.Parameters, f), arguments);

        // ab1/0006: a case clause's function in a Script.
        (r, m) = Resolved("switch (1) { case 1: function g() { return 6; } } g()");
        IsAlias(m, BlockFn(m, "g"));
        IsGlobal(m.Resolve(Reference(r, "g")));

        // ab1/0007: two blocks' functions, one alias, its node the first.
        (r, m) = Resolved("function f() { { function g() { return 1; } } { function g() { return 2; } } return g(); } f()");
        alias = IsAlias(m, BlockFn(m, "g"));
        Assert.Same(alias, m.AnnexBAlias(BlockFn(m, "g", 1)));
        Assert.Same(Binding(r, "g"), alias.Node);
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), alias);

        // ab1/0008, V027: a parameter of the name withholds it.
        (r, m) = Resolved("function f(g) { { function g() { return 3; } } return typeof g; } f(1)");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Parameters, Fn(r, "f")), Of(m, ScopeKind.Parameters, Fn(r, "f")).Declarations[0]);

        // ab1/0009, V029: an enclosing let withholds it.
        (r, m) = Resolved("function f() { { let g = 1; { function g() {} } } return typeof g; } f()");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsGlobal(m.Resolve(Reference(r, "g")));

        // ab1/0010: the Script's let withholds it.
        (r, m) = Resolved("let g = 0; { function g() {} } typeof g");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsStatic(m.Resolve(Reference(r, "g")), m.Scopes[0], Decl(m.Scopes[0], "g"));

        // ab1/0011, V030: strict code has none.
        (r, m) = Resolved("'use strict'; { function g() {} } typeof g");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsGlobal(m.Resolve(Reference(r, "g")));

        // ab1/0012: the inner source under e, an enclosing let in its function.
        (r, m) = SloppyEvalModel("function f() { { let g = 1; { function g() {} } } return typeof g; } f()");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsInherited(m.Resolve(Reference(r, "g")), null);

        // ab1/0013: the inner source under e, strict by its directive and by the forced switch.
        (r, m) = SloppyEvalModel("'use strict'; { function g() {} } typeof g");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsInherited(m.Resolve(Reference(r, "g")), null);
        (r, m) = SloppyEvalModel("{ function g() {} } typeof g", strict: true);
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsInherited(m.Resolve(Reference(r, "g")), null);

        // The probes' programs as eval code under e (they ran as indirect-eval code).
        (r, m) = SloppyEvalModel("__reached = true; { function g() { return 1; } } g()");
        IsInherited(m.Resolve(Reference(r, "g")), IsAlias(m, BlockFn(m, "g")));
        (r, m) = SloppyEvalModel("__reached = true; function f() { { function g() { return 5; } } return g(); } f()");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));
        (r, m) = SloppyEvalModel("__reached = true; function f() { var t = g; { function g() {} } return t; } f()");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));
        (r, m) = SloppyEvalModel("__reached = true; eval('{ function g() { return 4; } }'); g()");
        Assert.DoesNotContain(m.Scopes.SelectMany(static s => s.Declarations), static d => d.IsBlockFunction);
        IsInherited(m.Resolve(Reference(r, "g")), null);
        (r, m) = SloppyEvalModel("__reached = true; function f(g) { { function g() { return 3; } } return typeof g; } f(1)");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        (r, m) = SloppyEvalModel("__reached = true; function f() { { function arguments() {} } return typeof arguments; } f()");
        Assert.Same(m.EnvironmentOf(Fn(r, "f")).Arguments, m.AnnexBAlias(BlockFn(m, "arguments")));
        (r, m) = SloppyEvalModel("__reached = true; function f() { { let g = 1; { function g() {} } } return typeof g; } f()");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsInherited(m.Resolve(Reference(r, "g")), null);
        (r, m) = SloppyEvalModel("'use strict'; __reached = true; { function g() {} } typeof g");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsInherited(m.Resolve(Reference(r, "g")), null);

        // The probes as Scripts, beside the vectors above.
        (r, m) = Resolved("__reached = true; { function g() { return 1; } } g()");
        IsAlias(m, BlockFn(m, "g"));
        IsGlobal(m.Resolve(Reference(r, "g")));
        (r, m) = Resolved("__reached = true; function f() { { function arguments() {} } return typeof arguments; } f()");
        Assert.Same(m.EnvironmentOf(Fn(r, "f")).Arguments, m.AnnexBAlias(BlockFn(m, "arguments")));
        (r, m) = Resolved("'use strict'; __reached = true; { function g() {} } typeof g");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
    }

    /// <summary>
    /// Stage 5a's interim, closed: a name only a block function's alias binds now answers that alias, not the scopes' outer
    /// binding.
    /// </summary>
    [Fact]
    public void A_name_only_an_alias_binds_answers_the_alias_not_an_outer_binding()
    {
        var (r, m) = Resolved("function f() { { function g() {} } return g; }");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));
        (r, m) = Resolved("var g; function f() { { function g() {} } return g; }");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));
        Assert.NotSame(Decl(m.Scopes[0], "g"), m.Resolve(Reference(r, "g")).Declaration);
    }

    /// <summary>
    /// The constructs: what withholds an alias, what a block function writes instead, each kind of code, the readings of
    /// duplicates and labels, and the <c>arguments</c> cases.
    /// </summary>
    [Fact]
    public void Each_construct_admits_or_withholds_the_alias_as_the_pinned_edition_says()
    {
        // Nested blocks of one name: the outer admitted, the inner withheld by it (test262's nested-blocks file).
        var (r, m) = Resolved("function f() { { function g() { return 1; } { function g() { return 2; } } } return g(); }");
        var alias = IsAlias(m, BlockFn(m, "g"));
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g", 1)));
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), alias);

        // An enclosing block's lexical declaration after the block, and the body's: withheld.
        foreach (var declared in new[] { "let g;", "const g = 1;", "class g {}" })
        {
            (r, m) = Resolved("function f() { { { function g() {} } " + declared + " } return g; }");
            Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
            IsGlobal(m.Resolve(Reference(r, "g")));
        }

        (r, m) = Resolved("function f() { { function g() {} } let g; return g; }");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), Decl(Of(m, ScopeKind.Function, Fn(r, "f")), "g"));
        (r, m) = Resolved("function f() { let arguments; { function arguments() {} } }");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "arguments")));

        // Lexical for heads withhold it; a var head is the binding written.
        foreach (var loop in new[] { "for (let g;;) { { function g() {} } break; }", "for (let g of x) { { function g() {} } }", "for (let g in x) { { function g() {} } }" })
        {
            (r, m) = Resolved("function f(x) { " + loop + " }");
            Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        }

        (r, m) = Resolved("function f(x) { for (var g of x) { { function g() {} } } return g; }");
        var head = Decl(Of(m, ScopeKind.Function, Fn(r, "f")), "g");
        Assert.Equal(BindingKind.Var, head.Kind);
        Assert.Same(head, m.AnnexBAlias(BlockFn(m, "g")));
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), head);

        // A simple catch parameter does not withhold it (Annex B.3.4) and still shadows it inside; a pattern does.
        (r, m) = Resolved("function f() { try {} catch (g) { { function g() {} } g; } return g; }");
        alias = IsAlias(m, BlockFn(m, "g"));
        var @catch = Only(m, ScopeKind.Catch);
        IsStatic(m.Resolve(Reference(r, "g")), @catch, @catch.Declarations[0]);
        IsStatic(m.Resolve(Reference(r, "g", 1)), Of(m, ScopeKind.Function, Fn(r, "f")), alias);
        (r, m) = Resolved("function f() { try {} catch ({ g }) { { function g() {} } } return g; }");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        IsGlobal(m.Resolve(Reference(r, "g")));

        // An existing var or top-level function is the binding written; Resolve names it as before.
        (r, m) = Resolved("function f() { var g; { function g() {} } return g; }");
        var body = Of(m, ScopeKind.Function, Fn(r, "f"));
        Assert.Same(Decl(body, "g"), m.AnnexBAlias(BlockFn(m, "g")));
        IsStatic(m.Resolve(Reference(r, "g")), body, Decl(body, "g"));
        (r, m) = Resolved("function f() { function g() {} { function g() {} } return g; }");
        body = Of(m, ScopeKind.Function, Fn(r, "f"));
        Assert.Equal(BindingKind.Function, Decl(body, "g").Kind);
        Assert.Same(Decl(body, "g"), m.AnnexBAlias(BlockFn(m, "g")));
        IsStatic(m.Resolve(Reference(r, "g")), body, Decl(body, "g"));

        // Generators and async functions have none; a plain one inside a sloppy generator or async function does.
        (r, m) = Resolved("function f() { { function* g() {} async function h() {} async function* k() {} } }");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        Assert.Null(m.AnnexBAlias(BlockFn(m, "h")));
        Assert.Null(m.AnnexBAlias(BlockFn(m, "k")));
        foreach (var head2 in new[] { "function* f()", "async function f()", "async function* f()" })
        {
            (r, m) = Resolved(head2 + " { { function g() {} } return g; }");
            IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));
        }

        // A block function in a function nested in f's block is that function's.
        (r, m) = Resolved("function f() { { function h() { { function g() {} } return g; } } return g; }");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "h")), IsAlias(m, BlockFn(m, "g")));
        IsGlobal(m.Resolve(Reference(r, "g", 1)));
        IsAlias(m, BlockFn(m, "h"));

        // An if clause's function; one whose name is a parameter; both clauses, one alias.
        (r, m) = Resolved("function f(a) { if (a) function g() {} return g; }");
        Assert.True(BlockFn(m, "g").IsIfClauseFunction);
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));
        (r, m) = Resolved("function f(g) { if (g) function g() {} }");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        (r, m) = Resolved("function f(a) { if (a) function g() {} else function g() {} return g; }");
        alias = IsAlias(m, BlockFn(m, "g"));
        Assert.Same(alias, m.AnnexBAlias(BlockFn(m, "g", 1)));
        Assert.Same(Binding(r, "g"), alias.Node);

        // A labelled block function (reading n9): admitted.
        (r, m) = Resolved("function f() { { l: function g() {} } return g; }");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));

        // Duplicates in one block and in one switch (reading n8): both admitted, one alias, as the syntax vector
        // syntax/annex-b/eg0004/0001 records; that vector's source as a Script.
        (r, m) = Resolved("function f() { { function g() {} function g() {} } return g; }");
        alias = IsAlias(m, BlockFn(m, "g"));
        Assert.Same(alias, m.AnnexBAlias(BlockFn(m, "g", 1)));
        Assert.Same(Binding(r, "g"), alias.Node);
        (r, m) = Resolved("function f(x) { switch (x) { case 1: function g() {} case 2: function g() {} } return g; }");
        alias = IsAlias(m, BlockFn(m, "g"));
        Assert.Same(alias, m.AnnexBAlias(BlockFn(m, "g", 1)));
        (r, m) = Resolved("{ function f() { return 1; } function f() { return 2; } } f()");
        alias = IsAlias(m, BlockFn(m, "f"));
        Assert.Same(alias, m.AnnexBAlias(BlockFn(m, "f", 1)));
        IsGlobal(m.Resolve(Reference(r, "f")));

        // An arrow's body is the arrow's; an arrow parameter of the name withholds it.
        (r, m) = Resolved("var a = () => { { function g() {} } return g; };");
        var arrow = First<FunctionNode>(r, static n => n.IsArrow);
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, arrow), IsAlias(m, BlockFn(m, "g")));
        (r, m) = Resolved("var a = (g) => { { function g() {} } };");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));

        // A named function expression's own name: the alias, in the body's scope, comes first.
        (r, m) = Resolved("var x = function g() { { function g() {} } return g; };");
        var expression = First<FunctionNode>(r, static n => n.Name?.Name == "g" && n.Body.Count > 1);
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, expression), IsAlias(m, BlockFn(m, "g")));

        // A sloppy object-literal method has one; a class method, a static block and a module have none.
        (r, m) = Resolved("var o = { m() { { function g() {} } return g; } };");
        IsAlias(m, BlockFn(m, "g"));
        foreach (var text in new[] { "class C { m() { { function g() {} } } }", "class C { static { { function g() {} } } }" })
        {
            (r, m) = Resolved(text);
            Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        }

        // (Revised 2026-11-27, at SJL-15.2 stage 6, whose EE0171 refuses the module's source, its block function being no
        // declaration of the module's items; it read "{ function g() {} } export { g };".)
        (r, m) = Resolved("{ function g() {} } g;", ParseGoal.Module);
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));

        // With Annex B off, none.
        (r, m) = AnnexBOff("function f() { { function g() {} } return g; } { function h() {} }");
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));
        Assert.Null(m.AnnexBAlias(BlockFn(m, "h")));
        IsGlobal(m.Resolve(Reference(r, "g")));

        // Inside a with statement the object comes first, the alias after it; a closure reads the alias.
        (r, m) = Resolved("function f(o) { with (o) { { function g() {} } g; } return g; }");
        alias = IsAlias(m, BlockFn(m, "g"));
        IsStatic(IsDynamic(m.Resolve(Reference(r, "g")), DynamicReason.With, Only(m, ScopeKind.With)), Of(m, ScopeKind.Function, Fn(r, "f")), alias);
        IsStatic(m.Resolve(Reference(r, "g", 1)), Of(m, ScopeKind.Function, Fn(r, "f")), alias);
        (r, m) = Resolved("function f() { { function g() {} } return () => g; }");
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, Fn(r, "f")), IsAlias(m, BlockFn(m, "g")));

        // The dynamic function's body goal: admitted, its parameters unseen; arguments, the program's binding.
        (r, m) = Resolved("{ function g() {} } return g;", ParseGoal.FunctionBody);
        Assert.Equal(ScopeKind.Function, VarScopeOf(m, m.ScopeOf(Binding(r, "g"))).Kind);
        IsStatic(m.Resolve(Reference(r, "g")), Of(m, ScopeKind.Function, r.Tree!), IsAlias(m, BlockFn(m, "g")));
        (r, m) = Resolved("{ function arguments() {} } return arguments;", ParseGoal.FunctionBody);
        Assert.NotNull(m.EnvironmentOf(r.Tree!).Arguments);
        Assert.Same(m.EnvironmentOf(r.Tree!).Arguments, m.AnnexBAlias(BlockFn(m, "arguments")));

        // arguments in a Script and in eval code: an alias of its own; the Script's reference global, eval code's inherited.
        (r, m) = Resolved("{ function arguments() {} } arguments;");
        IsAlias(m, BlockFn(m, "arguments"));
        IsGlobal(m.Resolve(Reference(r, "arguments")));
        (r, m) = SloppyEvalModel("{ function arguments() {} } arguments;");
        IsInherited(m.Resolve(Reference(r, "arguments")), IsAlias(m, BlockFn(m, "arguments")));
        (r, m) = SloppyEvalModel("{ function g() {} } g;", strict: true);
        Assert.Null(m.AnnexBAlias(BlockFn(m, "g")));

        // An arguments binding the evaluation creates: the variable environment's scope may gain the name, and only it.
        (r, m) = Resolved("function f(a = 0) { { function arguments() {} } return arguments + a; }");
        var f = Fn(r, "f");
        alias = IsAlias(m, BlockFn(m, "arguments"));
        IsStatic(IsDynamic(m.Resolve(Reference(r, "arguments")), DynamicReason.BlockFunction, Of(m, ScopeKind.Function, f)), Of(m, ScopeKind.Parameters, f), m.EnvironmentOf(f).Arguments!);
        IsStatic(m.Resolve(Reference(r, "a")), Of(m, ScopeKind.Parameters, f), Of(m, ScopeKind.Parameters, f).Declarations[0]);
        Assert.True(m.EnvironmentOf(f).ArgumentsNeeded);
        (r, m) = Resolved("(() => { { function arguments() {} } return arguments; });");
        arrow = First<FunctionNode>(r, static n => n.IsArrow);
        IsAlias(m, BlockFn(m, "arguments"));
        IsGlobal(IsDynamic(m.Resolve(Reference(r, "arguments")), DynamicReason.BlockFunction, Of(m, ScopeKind.Parameters, arrow)));
        (r, m) = Resolved("function f(o) { with (o) { (() => { { function arguments() {} } eval(\"\"); return arguments; })(); } }");
        arrow = First<FunctionNode>(r, static n => n.IsArrow);
        var both = IsDynamic(m.Resolve(Reference(r, "arguments")), DynamicReason.SloppyDirectEval, Of(m, ScopeKind.Parameters, arrow), Only(m, ScopeKind.With));
        IsStatic(both, Of(m, ScopeKind.Parameters, Fn(r, "f")), m.EnvironmentOf(Fn(r, "f")).Arguments!);
        (r, m) = Resolved("function f(a = 0) { var arguments; { function arguments() {} } }");
        f = Fn(r, "f");
        Assert.Same(Decl(Of(m, ScopeKind.Function, f), "arguments"), m.AnnexBAlias(BlockFn(m, "arguments")));
    }

    /// <summary>
    /// Every declaration's answer, and refusals: a null declaration, a declaration of another tree; the model's own
    /// declarations answer null.
    /// </summary>
    [Fact]
    public void Every_declaration_answers_and_a_null_or_foreign_declaration_is_refused()
    {
        var (result, model) = Resolved("function f(a) { { function g() {} } return arguments; } var v;");
        var (other, otherModel) = Resolved("function f(a) { { function g() {} } return arguments; } var v;");
        Assert.Throws<ArgumentNullException>(() => model.AnnexBAlias(null!));
        Assert.Throws<ArgumentException>(() => model.AnnexBAlias(BlockFn(otherModel, "g")));
        Assert.Throws<ArgumentException>(() => model.AnnexBAlias(otherModel.AnnexBAlias(BlockFn(otherModel, "g"))!));
        Assert.Throws<ArgumentException>(() => model.AnnexBAlias(otherModel.EnvironmentOf(Fn(other, "f")).Arguments!));
        foreach (var declaration in model.Scopes.SelectMany(static s => s.Declarations))
            Assert.True(declaration.IsBlockFunction || model.AnnexBAlias(declaration) is null);
        Assert.Null(model.AnnexBAlias(model.EnvironmentOf(Fn(result, "f")).Arguments!));
        Assert.Null(model.AnnexBAlias(model.AnnexBAlias(BlockFn(model, "g"))!));
    }

    /// <summary>
    /// The model dump's <c>model-annexb</c> lines, one per block function, and the <c>alias:</c> form in the resolution
    /// lines, for a function's alias, eval code's and an <c>arguments</c> binding the evaluation creates; a dump without a
    /// model is unchanged.
    /// </summary>
    [Fact]
    public void The_dump_s_model_section_holds_each_block_function_s_alias()
    {
        const string text = "function f() { { function g() {} function* h() {} } { function arguments() {} } return g; }";
        var on = Parse(text);
        var model = on.Model!;
        var lines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(on)).Split('\n');
        var nodes = Nodes(on.Tree!);
        string Index(SyntaxNode node) => nodes.FindIndex(n => ReferenceEquals(n, node)).ToString(CultureInfo.InvariantCulture);
        var annexb = lines.Where(static l => l.StartsWith("model-annexb ", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, annexb.Count);
        Assert.EndsWith(" alias:" + Index(Binding(on, "g")), annexb[0], StringComparison.Ordinal);
        Assert.EndsWith(" -", annexb[1], StringComparison.Ordinal);
        Assert.EndsWith(" args:" + Index(Fn(on, "f")), annexb[2], StringComparison.Ordinal);
        Assert.Contains(lines, l => l.StartsWith("model-resolution " + Index(Reference(on, "g")) + " 0 ", StringComparison.Ordinal) && l.Contains(" alias:" + Index(Binding(on, "g")) + " ", StringComparison.Ordinal));

        var eval = SyntaxParser.Parse(new ParseRequest(new SourceText("{ function g() {} } g;"), ParseGoal.Eval, UnicodeDataHome.Tables, NoValidator.Instance) { BuildSemanticModel = true, Context = new InheritedContext { InFunction = true } });
        var evalNodes = Nodes(eval.Tree!);
        var evalLines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(eval)).Split('\n');
        var g = evalNodes.FindIndex(n => n is Identifier { Name: "g" }).ToString(CultureInfo.InvariantCulture);
        var gBinding = evalNodes.FindIndex(n => n is BindingIdentifier { Name: "g" }).ToString(CultureInfo.InvariantCulture);
        Assert.Contains("model-resolution " + g + " 3 - alias:" + gBinding + " - - -", evalLines);

        var created = Parse("function f(a = 0) { { function arguments() {} } return arguments; }");
        var createdNodes = Nodes(created.Tree!);
        var createdLines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(created)).Split('\n');
        var reference = createdNodes.FindIndex(n => n is Identifier { Name: "arguments" }).ToString(CultureInfo.InvariantCulture);
        Assert.Contains(createdLines, l => l.StartsWith("model-resolution " + reference + " 1 - - 2 ", StringComparison.Ordinal));

        var off = Parse(text, model: false);
        Assert.True(SyntaxDump.Dump(off).AsSpan().SequenceEqual(SyntaxDump.Dump(on, model: false)));
        Assert.DoesNotContain("model-annexb", System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(off)), StringComparison.Ordinal);
    }

    // Accepted sources for the aliases, beside the population, the fixtures and the resolution sources.
    private static readonly (ParseGoal Goal, string Text)[] AnnexBSources =
    [
        (ParseGoal.Script, "{ function g() { return 1; } } g()"), (ParseGoal.Script, "function f() { { function g() { return 5; } } return g(); } f()"),
        (ParseGoal.Script, "function f() { var t = g; { function g() {} } return t; } f()"), (ParseGoal.Script, "eval('{ function g() { return 4; } }'); g()"),
        (ParseGoal.Script, "function f() { { function arguments() {} } return typeof arguments; } f()"), (ParseGoal.Script, "switch (1) { case 1: function g() { return 6; } } g()"),
        (ParseGoal.Script, "function f() { { function g() { return 1; } } { function g() { return 2; } } return g(); } f()"),
        (ParseGoal.Script, "function f(g) { { function g() { return 3; } } return typeof g; } f(1)"),
        (ParseGoal.Script, "function f() { { let g = 1; { function g() {} } } return typeof g; } f()"), (ParseGoal.Script, "let g = 0; { function g() {} } typeof g"),
        (ParseGoal.Script, "'use strict'; { function g() {} } typeof g"), (ParseGoal.Eval, "function f() { { let g = 1; { function g() {} } } return typeof g; } f()"),
        (ParseGoal.Eval, "'use strict'; { function g() {} } typeof g"), (ParseGoal.Eval, "{ function g() { return 4; } } g;"),
        (ParseGoal.Script, "function f(a = 0) { { function arguments() {} } return arguments; }"), (ParseGoal.Script, "(() => { { function arguments() {} } return arguments; })"),
        (ParseGoal.Script, "function f() { { function g() {} { function g() {} } } try {} catch (h) { { function h() {} } } }"),
        (ParseGoal.Script, "function f(o) { with (o) { { function g() {} } g; } { l: function k() {} } if (o) function m() {} else function m() {} return g + k + m; }"),
        (ParseGoal.Script, "var x = function g() { { function g() {} } return g; }; var o = { m() { { function g() {} } return g; } };"),
        (ParseGoal.FunctionBody, "{ function g() {} } { function arguments() {} } return g + arguments;"),
        (ParseGoal.Script, "if (true) { function test262Fn() {} }"), (ParseGoal.Script, "label: function g() {} label1: label2: function f() {}"),
        (ParseGoal.Script, "(function() { { function arguments() {} } }()); (function(x) { { function arguments() {} } }()); (function(..._) { { function arguments() {} } }());"),
    ];

    // One tree's alias answers, checked against an oracle of the tree's own: the null list's clauses and the answers'
    // kinds recomputed from the syntax nodes through a parent map, iteratively, with only the facts' strictness, separate
    // variable environment and arguments binding read from the model, which the function facts' and environment facts'
    // own tests check; the parameters' names are read from the tree and checked against the facts. With annexB false every
    // answer is null. It returns the block functions, the admitted ones and the aliases made.
    // From stage 5c, with the request's caller's records given, a record that is not a Catch clause's and binds the name
    // also withholds sloppy eval code's own alias (EvalDeclarationInstantiation step 13.b.ii.4). (Added 2026-11-26, at
    // SJL-15.2 stage 5c.) (Revised 2026-11-26, at SJL-15.2 stage 5c; the signature ended "bool annexB = true)".)
    private static (int Functions, int Admitted, int Aliases) CompleteAliases(string name, ParseResult on, ParseResult off, bool annexB = true, IReadOnlyList<CallerScope>? callers = null)
    {
        if (on.Model is not { } model)
            return default;
        var tree = on.Tree!;
        var nodes = Nodes(tree);
        var parents = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
        var children = new List<SyntaxNode>();
        foreach (var node in nodes)
        {
            children.Clear();
            node.AddChildren(children);
            foreach (var child in children)
                parents[child] = node;
        }

        // No alias in any scope, on or off; every declaration of the model answers.
        Assert.DoesNotContain(on.Declarations!.Scopes.SelectMany(static s => s.Declarations), static d => d.Kind == BindingKind.BlockFunctionAlias);
        Assert.DoesNotContain(off.Declarations!.Scopes.SelectMany(static s => s.Declarations), static d => d.Kind == BindingKind.BlockFunctionAlias);
        Assert.DoesNotContain(model.Scopes.SelectMany(static s => s.Declarations), static d => d.Kind == BindingKind.BlockFunctionAlias);
        foreach (var declaration in model.Scopes.SelectMany(static s => s.Declarations))
            Assert.True(declaration.IsBlockFunction || model.AnnexBAlias(declaration) is null, name + ": an answer for a declaration that is no block function");
        foreach (var facts in model.Functions)
        {
            if (model.EnvironmentOf(facts.Node).Arguments is { } arguments)
                Assert.Null(model.AnnexBAlias(arguments));
        }

        static void BoundNames(SyntaxNode? pattern, List<BindingIdentifier> into)
        {
            var pending = new Stack<SyntaxNode>();
            if (pattern is not null)
                pending.Push(pattern);
            while (pending.Count > 0)
            {
                switch (pending.Pop())
                {
                    case BindingIdentifier binding:
                        into.Add(binding);
                        break;
                    case ArrayPattern array:
                        foreach (var element in array.Elements)
                        {
                            if (element is not null)
                                pending.Push(element);
                        }

                        break;
                    case ObjectPattern @object:
                        foreach (var property in @object.Properties)
                            pending.Push(property);
                        break;
                    case PatternProperty property:
                        pending.Push(property.Value);
                        break;
                    case RestElement rest:
                        pending.Push(rest.Argument);
                        break;
                    case AssignmentPattern assignment:
                        pending.Push(assignment.Target);
                        break;
                }
            }
        }

        // A statement list's lexically declared names, labels looked through.
        static HashSet<string> LexicalNames(IEnumerable<Statement> statements, bool top)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var bound = new List<BindingIdentifier>();
            foreach (var statement in statements)
            {
                var inner = statement;
                while (inner is LabelledStatement labelled)
                    inner = labelled.Body;
                switch (inner)
                {
                    case VariableDeclaration { DeclarationKind: not DeclarationKind.Var } declaration:
                        foreach (var declarator in declaration.Declarations)
                            BoundNames(declarator.Target, bound);
                        break;
                    case ClassDeclaration { Class.Name: { } className }:
                        names.Add(className.Name);
                        break;
                    case FunctionDeclaration { Function.Name: { } functionName } when !top:
                        names.Add(functionName.Name);
                        break;
                }
            }

            foreach (var binding in bound)
                names.Add(binding.Name);
            return names;
        }

        // The var-declared binding identifiers of a body, down to the next function, class or static block: its vars, and
        // at its top level its function declarations.
        static List<BindingIdentifier> VarNames(IReadOnlyList<Statement> body)
        {
            var found = new List<BindingIdentifier>();
            foreach (var statement in body)
            {
                var inner = statement;
                while (inner is LabelledStatement labelled)
                    inner = labelled.Body;
                if (inner is FunctionDeclaration { Function.Name: { } name })
                    found.Add(name);
            }

            var pending = new Stack<SyntaxNode>(body.Reverse());
            var kids = new List<SyntaxNode>();
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (node is FunctionNode or ClassNode or StaticBlock)
                    continue;
                if (node is VariableDeclaration { DeclarationKind: DeclarationKind.Var } declaration)
                {
                    foreach (var declarator in declaration.Declarations)
                        BoundNames(declarator.Target, found);
                }

                kids.Clear();
                node.AddChildren(kids);
                for (var i = kids.Count - 1; i >= 0; i--)
                    pending.Push(kids[i]);
            }

            return found;
        }

        var (functions, admittedCount) = (0, 0);
        var aliasNode = new Dictionary<(SyntaxNode, string), BindingIdentifier>();
        var answers = new List<(Declaration Function, SyntaxNode Owner, Declaration? Answer)>();
        foreach (var node in nodes)
        {
            if (node is not FunctionDeclaration { Function.Name: { } fname } declarationNode)
                continue;
            var own = model.DeclarationOf(fname)!;
            SyntaxNode at = declarationNode;
            var container = parents[at];
            while (container is LabelledStatement)
            {
                at = container;
                container = parents[at];
            }

            var inBlock = container is BlockStatement or SwitchCase || container is IfStatement && ReferenceEquals(at, declarationNode);
            var here = name + ": " + fname.Name + " at " + fname.Span.Offset.ToString(CultureInfo.InvariantCulture);
            Assert.True(inBlock == own.IsBlockFunction, here + ": IsBlockFunction");
            var answer = model.AnnexBAlias(own);
            if (!inBlock)
            {
                Assert.Null(answer);
                continue;
            }

            functions++;

            // The owner of the function's code and its strictness, a class's parts being strict.
            SyntaxNode owner = container;
            var inClass = false;
            while (owner is not (FunctionNode or StaticBlock or ProgramNode))
            {
                if (owner is ClassNode)
                    inClass = true;
                owner = parents[owner];
            }

            var facts = model.FactsOf(owner);
            var f = fname.Name;
            var functionCode = owner is FunctionNode or StaticBlock || owner is ProgramNode { Goal: ParseGoal.FormalParameters or ParseGoal.FunctionBody };

            // The parameters' names, the BoundNames of the owner's FormalParameters, read from the tree and checked
            // against the facts.
            var parameters = new List<BindingIdentifier>();
            foreach (var parameter in owner switch { FunctionNode function => function.Parameters, ProgramNode program => program.Parameters, _ => Array.Empty<Pattern>() })
                BoundNames(parameter, parameters);
            var parameterNames = parameters.Select(static x => x.Name).ToList();
            Assert.True(parameterNames.SequenceEqual(facts.ParameterNames), here + ": the parameters' names");
            var withheld = !annexB || declarationNode.Function.IsAsync || declarationNode.Function.IsGenerator || inClass || facts.IsStrict
                || (functionCode && parameterNames.Contains(f));

            // Each construct between the function's own block and its code's owner that declares the name.
            var next = container switch
            {
                SwitchCase => parents[parents[container]],
                _ => parents[container],
            };
            for (var n = next; !withheld && !ReferenceEquals(n, owner); n = parents[n])
            {
                var bound = new List<BindingIdentifier>();
                switch (n)
                {
                    case BlockStatement block:
                        withheld = LexicalNames(block.Body, top: false).Contains(f);
                        break;
                    case SwitchStatement @switch:
                        withheld = LexicalNames(@switch.Cases.SelectMany(static c => c.Consequent), top: false).Contains(f);
                        break;
                    case ForStatement { Init: VariableDeclaration { DeclarationKind: not DeclarationKind.Var } init }:
                        foreach (var declarator in init.Declarations)
                            BoundNames(declarator.Target, bound);
                        break;
                    case ForInStatement { Left: VariableDeclaration { DeclarationKind: not DeclarationKind.Var } left }:
                        foreach (var declarator in left.Declarations)
                            BoundNames(declarator.Target, bound);
                        break;
                    case ForOfStatement { Left: VariableDeclaration { DeclarationKind: not DeclarationKind.Var } left }:
                        foreach (var declarator in left.Declarations)
                            BoundNames(declarator.Target, bound);
                        break;
                    case CatchClause { Parameter: { } parameter } when parameter is not BindingIdentifier:
                        BoundNames(parameter, bound);
                        break;
                }

                withheld = withheld || bound.Exists(b => b.Name == f);
            }

            var top = owner switch
            {
                FunctionNode function => function.Body,
                StaticBlock block => block.Body,
                _ => ((ProgramNode)owner).Body,
            };
            withheld = withheld || LexicalNames(top, top: true).Contains(f);
            withheld = withheld || (callers is not null && owner is ProgramNode && facts.Kind == FunctionKind.EvalCode
                && callers.Any(c => !c.IsCatch && c.Names.Contains(f)));
            if (withheld)
            {
                Assert.True(answer is null, here + ": admitted where the oracle withholds it");
                continue;
            }

            admittedCount++;
            Assert.True(answer is not null && answer.Name == f, here + ": withheld where the oracle admits it");
            var existing = VarNames(top).Where(b => b.Name == f).OrderBy(static b => b.Span.Offset).FirstOrDefault();
            var arguments = model.EnvironmentOf(owner).Arguments;
            if (existing is not null)
            {
                Assert.True(answer!.Kind is BindingKind.Var or BindingKind.Function && ReferenceEquals(answer.Node, existing) && !answer.IsLexical, here + ": not the first var-scoped declaration");
            }
            else if (functionCode && f == "arguments" && arguments is not null && !facts.HasSeparateVariableEnvironment)
            {
                Assert.Same(arguments, answer);
            }
            else
            {
                Assert.True(answer!.Kind == BindingKind.BlockFunctionAlias, here + ": not an alias");
                if (!aliasNode.TryGetValue((owner, f), out var first) || fname.Span.Offset < first.Span.Offset)
                    aliasNode[(owner, f)] = fname;
            }

            answers.Add((own, owner, answer));
        }

        // One alias per owner and name, its node the earliest admitted block function's name.
        var aliases = new Dictionary<(SyntaxNode, string), Declaration>();
        foreach (var (function, owner, answer) in answers)
        {
            if (answer is not { Kind: BindingKind.BlockFunctionAlias })
                continue;
            if (aliases.TryGetValue((owner, function.Name), out var made))
                Assert.Same(made, answer);
            else
                aliases[(owner, function.Name)] = answer;
            Assert.Same(aliasNode[(owner, function.Name)], answer.Node);
            Assert.True(model.DeclarationOf(answer.Node) is { IsBlockFunction: true } blockFunction && blockFunction.Name == answer.Name, name + ": the alias's node answers no block function");
            Assert.Null(model.AnnexBAlias(answer));
            Assert.True(answer.IsMutable && !answer.IsLexical && !answer.IsBlockFunction && !answer.IsIfClauseFunction, name + ": the alias's flags");
        }

        return (functions, admittedCount, aliases.Count);
    }

    /// <summary>
    /// Over the determinism tests' population, the resolution sources and the Annex B sources: every block function's
    /// alias answer as an oracle of the tree recomputes it; and, for each source accepted with Annex B both on and off, the
    /// resolutions with Annex B on against the model's own with it off: each difference is a name a function's or sloppy
    /// eval code's alias binds, or an <c>arguments</c> binding a block function's evaluation creates, which adds the
    /// variable environment's scope to the answer with Annex B off as a dynamic entry, with reason
    /// <see cref="DynamicReason.BlockFunction"/> where it is the innermost, that answer its fallback; none in a Script's
    /// variable scope; none where the alias answer is an existing binding; no binding identifier's; and eval code's only in
    /// the declaration it carries. Each difference is counted by its variable scope's function kind.
    /// </summary>
    [Fact]
    public void Every_block_function_s_alias_and_every_moved_resolution_hold_over_the_population()
    {
        var items = DeterminismTests.Population.Value;
        var (functions, admitted, aliases, compared, moved, movedByKind) = OnDocumentedStack(() =>
        {
            var (fns, adm, als, cmp, mov) = (0, 0, 0, 0, 0);
            var byKind = new SortedDictionary<string, int>(StringComparer.Ordinal);
            void One(string name, ParseRequest request, ParseRequest offRequest)
            {
                var on = SyntaxParser.Parse(request);
                if (on.Outcome != ParseOutcome.Accepted)
                    return;
                var (f, a, l) = CompleteAliases(name, on, SyntaxParser.Parse(offRequest));
                (fns, adm, als) = (fns + f, adm + a, als + l);
                var without = SyntaxParser.Parse(WithAnnexB(request, annexB: false));
                if (without.Outcome != ParseOutcome.Accepted)
                    return;
                CompleteAliases(name + " (Annex B off)", without, without, annexB: false);
                cmp++;
                var model = on.Model!;
                var baseline = without.Model!;
                var onNodes = Nodes(on.Tree!);
                var offNodes = Nodes(without.Tree!);
                Assert.Equal(onNodes.Count, offNodes.Count);
                Assert.Equal(model.Scopes.Count, baseline.Scopes.Count);
                int IndexOf(SemanticModel m, Scope scope) => m.Scopes.ToList().FindIndex(x => ReferenceEquals(x, scope));

                // The scopes that may gain the name arguments by a block function's evaluation, read from the alias
                // answers: a function's variable scope where its variable environment is separate, and otherwise its
                // Parameters scope.
                var argumentsGains = new HashSet<Scope>(ReferenceEqualityComparer.Instance);
                foreach (var scope in model.Scopes)
                {
                    foreach (var declaration in scope.Declarations)
                    {
                        if (model.AnnexBAlias(declaration) is not { Kind: BindingKind.BlockFunctionAlias, Name: "arguments" })
                            continue;
                        var varScope = VarScopeOf(model, scope);
                        var varFacts = model.FactsOf(varScope.Node);
                        if (varFacts.Kind is FunctionKind.Script or FunctionKind.EvalCode)
                            continue;
                        argumentsGains.Add(varFacts.HasSeparateVariableEnvironment ? varScope : model.Scopes.Single(x => x.Kind == ScopeKind.Parameters && ReferenceEquals(x.Node, varScope.Node)));
                    }
                }

                for (var i = 0; i < onNodes.Count; i++)
                {
                    if (onNodes[i] is BindingIdentifier binding)
                    {
                        Same(model.Resolve(binding), baseline.Resolve((BindingIdentifier)offNodes[i]), model, baseline, name + ": a binding moved at " + binding.Span.Offset.ToString(CultureInfo.InvariantCulture));
                        continue;
                    }

                    if (onNodes[i] is not Identifier reference)
                        continue;
                    var withAlias = model.Resolve(reference);
                    var withoutAlias = baseline.Resolve((Identifier)offNodes[i]);
                    if (Equivalent(withAlias, withoutAlias, model, baseline))
                        continue;
                    mov++;
                    var at = name + ": " + reference.Name + " at " + reference.Span.Offset.ToString(CultureInfo.InvariantCulture);
                    var end = withAlias.Kind == ResolutionKind.Dynamic ? withAlias.Fallback! : withAlias;
                    string kind;
                    if (withAlias.Kind == ResolutionKind.Dynamic && withAlias.Reason == DynamicReason.BlockFunction || withAlias.DynamicScopes.Count > withoutAlias.DynamicScopes.Count)
                    {
                        // The per-name gain: the answer with Annex B off, with the gaining scope added among its dynamic
                        // scopes in chain order, the reason BlockFunction where that scope is the innermost entry, and the
                        // fallback the answer with Annex B off.
                        Assert.Equal("arguments", reference.Name);
                        Assert.True(withAlias.Kind == ResolutionKind.Dynamic, at + ": the arguments gain is not dynamic");
                        var before = withoutAlias.Kind == ResolutionKind.Dynamic ? withoutAlias.DynamicScopes : Array.Empty<Scope>();
                        var beforeIndices = before.Select(x => IndexOf(baseline, x)).ToList();
                        var extra = withAlias.DynamicScopes.Where(x => !beforeIndices.Contains(IndexOf(model, x))).ToList();
                        Assert.True(extra.Count == 1 && argumentsGains.Contains(extra[0]), at + ": not one entry of a scope a block function's arguments gains");
                        var gainFacts = model.FactsOf(extra[0].Node);
                        Assert.True(extra[0].Kind == (gainFacts.HasSeparateVariableEnvironment ? ScopeKind.Function : ScopeKind.Parameters) && gainFacts.Kind is not (FunctionKind.Script or FunctionKind.EvalCode), at + ": the gaining scope is not the variable environment's");
                        Assert.True(withAlias.DynamicScopes.Where(x => !ReferenceEquals(x, extra[0])).Select(x => IndexOf(model, x)).SequenceEqual(beforeIndices), at + ": the other dynamic scopes moved");
                        var reason = ReferenceEquals(withAlias.DynamicScopes[0], extra[0]) ? DynamicReason.BlockFunction : withoutAlias.Reason;
                        Assert.True(withAlias.Reason == reason, at + ": the reason");
                        Assert.True(Equivalent(withAlias.Fallback!, withoutAlias.Kind == ResolutionKind.Dynamic ? withoutAlias.Fallback! : withoutAlias, model, baseline), at + ": the fallback is not the answer with Annex B off");
                        kind = "arguments gain, " + gainFacts.Kind.ToString();
                    }
                    else if (end.Declaration is { Kind: BindingKind.BlockFunctionAlias } alias)
                    {
                        var facts = model.FactsOf(end.Kind == ResolutionKind.Inherited ? model.Scopes[0].Node : end.Scope!.Node);
                        Assert.True(facts.Kind is not FunctionKind.Script, at + ": a Script's reference moved");
                        Assert.Equal(reference.Name, alias.Name);
                        if (end.Kind == ResolutionKind.Inherited)
                        {
                            var other = withoutAlias.Kind == ResolutionKind.Dynamic ? withoutAlias.Fallback! : withoutAlias;
                            Assert.True(other.Kind == ResolutionKind.Inherited && other.Declaration is null, at + ": eval code's difference is not the declaration alone");
                        }

                        kind = facts.Kind.ToString();
                    }
                    else
                    {
                        Assert.Fail(at + ": a moved answer that names no alias");
                        return;
                    }

                    byKind[kind] = byKind.TryGetValue(kind, out var count) ? count + 1 : 1;
                }
            }

            foreach (var item in items)
                One(Name(item), DeterminismTests.Request(item, model: true), DeterminismTests.Request(item));
            foreach (var (goal, text) in ResolutionSources.Concat(AnnexBSources))
            {
                ParseRequest Request(bool model) => goal == ParseGoal.Eval
                    ? new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance) { BuildSemanticModel = model, Context = new InheritedContext { InFunction = true } }
                    : new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance) { Switches = ParseSwitches.For(goal), BuildSemanticModel = model };
                Assert.True(SyntaxParser.Parse(Request(true)).Outcome == ParseOutcome.Accepted, text);
                One("source \"" + text + "\"", Request(true), Request(false));
            }

            return (fns, adm, als, cmp, mov, byKind);
        });
        Assert.True(functions > AnnexBSources.Length && admitted > AnnexBSources.Length && aliases > 0 && compared > 1_000 && moved > 0);
        output.WriteLine("block functions " + functions.ToString(CultureInfo.InvariantCulture) + ", admitted " + admitted.ToString(CultureInfo.InvariantCulture)
            + ", aliases " + aliases.ToString(CultureInfo.InvariantCulture) + "; sources compared with Annex B off " + compared.ToString(CultureInfo.InvariantCulture)
            + ", references moved " + moved.ToString(CultureInfo.InvariantCulture) + " (" + string.Join(", ", movedByKind.Select(static p => p.Key + " " + p.Value.ToString(CultureInfo.InvariantCulture))) + ")");
    }

    // Whether two resolutions of one node, in two models of one source, are the same: kinds, scopes and declarations by
    // their place in their model.
    private static bool Equivalent(Resolution a, Resolution b, SemanticModel ma, SemanticModel mb)
    {
        static string Key(Resolution r, SemanticModel m)
        {
            string Scope(Scope? s) => s is null ? "-" : m.Scopes.ToList().FindIndex(x => ReferenceEquals(x, s)).ToString(CultureInfo.InvariantCulture);
            string Declared(Declaration? d) => d is null ? "-" : d.Kind + ":" + d.Name + ":" + d.Node.Span.Offset.ToString(CultureInfo.InvariantCulture);
            var text = r.Kind + " " + Scope(r.Scope) + " " + Declared(r.Declaration) + " " + (r.Reason?.ToString() ?? "-") + " " + string.Join(",", r.DynamicScopes.Select(Scope));
            return r.Fallback is { } fallback ? text + " / " + Key(fallback, m) : text;
        }

        return Key(a, ma) == Key(b, mb);
    }

    private static void Same(Resolution a, Resolution b, SemanticModel ma, SemanticModel mb, string at) =>
        Assert.True(Equivalent(a, b, ma, mb), at);

    // -----------------------------------------------------------------------------------------------------------------
    // The caller's records of a direct eval (SJL-15.2 stage 5c; the session's decision D-js-caller-scopes).
    // (Added 2026-11-26, at SJL-15.2 stage 5c.)

    // An eval request with the caller's records, the member on or off, Annex B on or off, forced strict or not.
    private static ParseRequest CallerRequest(string text, IReadOnlyList<CallerScope>? callers, bool model = true, bool annexB = true, bool strict = false, bool allErrors = false) =>
        new(new SourceText(text), ParseGoal.Eval, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = strict, AnnexB = annexB },
            Context = new InheritedContext { InFunction = true, CallerScopes = callers },
            Limits = new ParseLimits { Reporting = allErrors ? ErrorReporting.AllErrors : ErrorReporting.FirstError },
            BuildSemanticModel = model,
        };

    // A list written as the tests write it: "{x,y}{c:e}" innermost first, "" empty; null for "null".
    private static CallerScope[]? Records(string written)
    {
        if (written == "null")
            return null;
        var list = new List<CallerScope>();
        foreach (var part in written.Split('}', StringSplitOptions.RemoveEmptyEntries))
        {
            var body = part.TrimStart('{');
            var isCatch = body.StartsWith("c:", StringComparison.Ordinal);
            list.Add(new CallerScope { Names = (isCatch ? body[2..] : body).Split(',', StringSplitOptions.RemoveEmptyEntries), IsCatch = isCatch });
        }

        return list.ToArray();
    }

    // A list as the tests write it.
    private static string Written(IReadOnlyList<CallerScope>? list) =>
        list is null ? "null" : string.Concat(list.Select(static s => "{" + (s.IsCatch ? "c:" : "") + string.Join(",", s.Names) + "}"));

    /// <summary>
    /// Every case of the chain rule (D-js-caller-scopes): each fixture's first direct-eval candidate answers its records,
    /// written innermost first; the eval source is then parsed under that list, eval code forced strict where the call is
    /// strict code, and refused or accepted as EvalDeclarationInstantiation step 3.d says. Blocks, a <c>switch</c>'s
    /// CaseBlock and a <c>for</c> head's record, every name each binds, block functions and classes included; a <c>for</c>
    /// head's initialiser and a for-of head's expression in the head's record, a <c>switch</c> discriminant outside its
    /// CaseBlock; a sloppy body's lexical declarations but not its <c>var</c>s or top-level functions; a call in parameters
    /// with every parameter and a non-arrow's <c>arguments</c>; a named function expression's name never reached; a Catch
    /// clause's record of either form, exempt with Annex B on, a parameterless catch omitted, a catch pattern's initialiser in
    /// its record; <c>with</c> skipped; the class around a field initialiser the chain's stop, a class inside it listed;
    /// static blocks, heritage and computed keys; and a Script's top level, which answers an empty list, never the Script's
    /// scope. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Theory]
    [InlineData("{ let x; eval('var x'); }", "{x}", "var x", false, true)]
    [InlineData("function f() { let a; { const b = 1; eval('var b'); } }", "{b}{a}", "var b", false, true)]
    [InlineData("function f() { let a; { const b = 1; eval('var a'); } }", "{b}{a}", "var a", false, true)]
    [InlineData("switch (0) { case 0: let x; eval('var x'); }", "{x}", "var x", false, true)]
    [InlineData("for (let i = 0; i < 1; i++) { eval('var i'); }", "{}{i}", "var i", false, true)]
    [InlineData("for (let i = eval('var i');;) break;", "{i}", "var i", false, true)]
    [InlineData("for (let x of eval('var x')) ;", "{x}", "var x", false, true)]
    [InlineData("switch (eval('var x')) { case 0: let x; }", "", "var x", false, false)]
    [InlineData("function f() { let a; var b; function c() {} eval('var b; var c;'); }", "{a}", "var b; var c;", false, false)]
    [InlineData("function f() { let a; var b; function c() {} eval('var a'); }", "{a}", "var a", false, true)]
    [InlineData("function f() { { function g() {} class K {} eval('var K'); } }", "{g,K}{}", "var K", false, true)]
    [InlineData("function f() { { function g() {} eval('var g'); } }", "{g}{}", "var g", false, true)]
    [InlineData("function f(a, b = eval('var c'), c) {}", "{a,b,c,arguments}", "var c", false, true)]
    [InlineData("function f(a, b = eval('var arguments')) {}", "{a,b,arguments}", "var arguments", false, true)]
    [InlineData("function f(a, b = eval('var d')) {}", "{a,b,arguments}", "var d", false, false)]
    [InlineData("((a = eval('var arguments')) => 0);", "{a}", "var arguments", false, false)]
    [InlineData("((p = eval('var arguments'), arguments) => 0);", "{p,arguments}", "var arguments", false, true)]
    [InlineData("(function a(x = eval('var a')) {});", "{x,arguments}", "var a", false, false)]
    [InlineData("() => { let q; eval('var q'); };", "{q}", "var q", false, true)]
    [InlineData("'use strict'; { let x; eval('var x'); }", "{x}", "var x", true, false)]
    [InlineData("{ let x; eval(\"'use strict'; var x\"); }", "{x}", "'use strict'; var x", false, false)]
    [InlineData("function f() { 'use strict'; let x; { eval('var x'); } }", "{}", "var x", true, false)]
    [InlineData("try {} catch (e) { eval('var e'); }", "{}{c:e}", "var e", false, false)]
    [InlineData("try {} catch ({ e, f: [g] }) { eval('var g'); }", "{}{c:e,g}", "var g", false, false)]
    [InlineData("try {} catch { let y; eval('var y'); }", "{y}", "var y", false, true)]
    [InlineData("try {} catch ({ a = eval('var a') }) {}", "{c:a}", "var a", false, false)]
    [InlineData("function f(o) { let x; with (o) { eval('var x'); } }", "{}{x}", "var x", false, true)]
    [InlineData("with (o) eval('var x');", "", "var x", false, false)]
    [InlineData("with (eval('var x')) { let x; }", "", "var x", false, false)]
    [InlineData("class C { x = eval(''); }", "", "", true, false)]
    [InlineData("class C { x = class D { [eval('')]() {} }; }", "{D}", "", true, false)]
    [InlineData("class C { static { eval(''); } }", "", "", true, false)]
    [InlineData("class C { static { { let q; eval('var q'); } } }", "{q}", "var q", true, false)]
    [InlineData("class C extends (eval('var C'), Object) {}", "{C}", "var C", true, false)]
    [InlineData("class C { [eval('')]() {} }", "{C}", "", true, false)]
    [InlineData("eval('var x');", "", "var x", false, false)]
    [InlineData("let x; eval('var x;');", "", "var x;", false, false)]
    public void Each_call_answers_the_records_between_it_and_its_variable_environment(string script, string records, string evalSource, bool strict, bool refused)
    {
        var (_, m) = Model(script);
        var call = m.DirectEvalCalls[0];
        Assert.Equal(records, Written(m.CallerScopesOf(call)));
        var result = SyntaxParser.Parse(CallerRequest(evalSource, m.CallerScopesOf(call), strict: strict, allErrors: true));
        Assert.True(refused == (result.Outcome == ParseOutcome.Refused), script + ": " + string.Join(",", result.Diagnostics.Select(static d => d.Code)));
        if (refused)
            Assert.Equal("JSE1907", Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>
    /// The dynamic goals, eval code and the catch exemption with Annex B off: a dynamic function's body and parameters are a
    /// function's; a call in sloppy eval code continues into the request's own list, or answers null where the request gives
    /// none; in strict eval code it stops at the program. A Catch clause's record refuses with Annex B off (EA0008).
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void The_dynamic_goals_and_eval_code_continue_the_chain_as_their_environments_say()
    {
        var body = Model("let q; eval('');", ParseGoal.FunctionBody).Model;
        Assert.Equal("{q}", Written(body.CallerScopesOf(body.DirectEvalCalls[0])));
        var parameters = Model("a, b = eval('')", ParseGoal.FormalParameters).Model;
        Assert.Equal("{a,b,arguments}", Written(parameters.CallerScopesOf(parameters.DirectEvalCalls[0])));

        foreach (var (outer, expected) in new[] { ("null", "null"), ("", "{x}"), ("{z}{c:e}", "{x}{z}{c:e}") })
        {
            var given = Records(outer);
            var result = SyntaxParser.Parse(CallerRequest("let x; { eval('var y'); }", given));
            var m = result.Model!;
            Assert.Equal(expected == "null" ? "null" : "{}" + expected, Written(m.CallerScopesOf(m.DirectEvalCalls[0])));
            if (given is not null)
            {
                // The request's own instances, appended as given.
                var answer = m.CallerScopesOf(m.DirectEvalCalls[0])!;
                for (var i = 0; i < given.Length; i++)
                    Assert.Same(given[i], answer[answer.Count - given.Length + i]);
            }
        }

        var strictEval = SyntaxParser.Parse(CallerRequest("let x; { eval(''); }", null, strict: true)).Model!;
        Assert.Equal("{}", Written(strictEval.CallerScopesOf(strictEval.DirectEvalCalls[0])));
        var function = SyntaxParser.Parse(CallerRequest("function f() { let x; eval(''); }", null)).Model!;
        Assert.Equal("{x}", Written(function.CallerScopesOf(function.DirectEvalCalls[0])));

        var exempt = Model("try {} catch (e) { eval('var e'); }").Model;
        var list = exempt.CallerScopesOf(exempt.DirectEvalCalls[0]);
        Assert.Equal(ParseOutcome.Accepted, SyntaxParser.Parse(CallerRequest("var e", list)).Outcome);
        Assert.Equal("JSE1908", Assert.Single(SyntaxParser.Parse(CallerRequest("var e", list, annexB: false)).Diagnostics).Code);
        Assert.Equal("JSE1908", Assert.Single(SyntaxParser.Parse(CallerRequest("var e", Records("{e}"), annexB: false)).Diagnostics).Code);
    }

    /// <summary>
    /// The members' order and errors: <see cref="SemanticModel.DirectEvalCalls"/> lists every candidate in source order, of
    /// every strictness, the parenthesised callee among them and not an optional call, a member call, a tagged template or
    /// <c>(0, eval)</c>; one instance per scope, shared; <see cref="SemanticModel.CallerScopesOf"/> refuses a null call, a
    /// call of another tree and a call that is no candidate. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void The_candidates_are_in_source_order_and_a_null_foreign_or_other_call_is_refused()
    {
        var (r, m) = Model("{ let a; eval(1); (eval)(2); eval?.(3); o.eval(4); (0, eval)(5); eval`6`; f(eval(7)); } function g() { 'use strict'; eval(8); }");
        Assert.Equal(["1", "2", "7", "8"], m.DirectEvalCalls.Select(static c => ((NumericLiteral)c.Arguments[0]).Text));
        Assert.True(m.DirectEvalCalls.Select(static c => c.Span.Offset).SequenceEqual(m.DirectEvalCalls.Select(static c => c.Span.Offset).Order()));
        var first = m.CallerScopesOf(m.DirectEvalCalls[0])!;
        Assert.Same(first[0], m.CallerScopesOf(m.DirectEvalCalls[1])![0]);
        Assert.Equal("", Written(m.CallerScopesOf(m.DirectEvalCalls[3])));

        Assert.Throws<ArgumentNullException>(() => m.CallerScopesOf(null!));
        var other = Model("eval(1);").Model;
        Assert.Throws<ArgumentException>(() => m.CallerScopesOf(other.DirectEvalCalls[0]));
        Assert.Throws<ArgumentException>(() => m.CallerScopesOf(Nth<CallExpression>(r, 0, static c => c.Callee is MemberExpression)));
    }

    /// <summary>
    /// Section 5's answers: with the caller's records given, a record that is not a Catch clause's withholds eval code's Annex
    /// B alias, and a Catch clause's keeps it; an inherited answer for a reference, for a binding resolved when it runs, or
    /// as a dynamic answer's fallback carries no declaration where a record binds the name; every other binding keeps its
    /// declaration; null keeps stage 5b's answers. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void The_caller_s_records_decide_eval_code_s_aliases_and_carried_declarations()
    {
        (ParseResult Result, SemanticModel Model, Declaration? Alias) Eval(string text, string records, bool strict = false)
        {
            var result = SyntaxParser.Parse(CallerRequest(text, Records(records), strict: strict));
            Assert.True(result.Outcome == ParseOutcome.Accepted, text + " under " + records);
            var block = result.Model!.Scopes.SelectMany(static s => s.Declarations).FirstOrDefault(static d => d.IsBlockFunction);
            return (result, result.Model, block is null ? null : result.Model.AnnexBAlias(block));
        }

        // The alias: withheld by {x}, kept by {c:x}, by {y} and by null; the reference carries it only where no record binds x.
        const string alias = "{ function x() {} } x;";
        var (r0, m0, a0) = Eval(alias, "{x}");
        Assert.Null(a0);
        IsInherited(m0.Resolve(Reference(r0, "x")), null);
        var (r1, m1, a1) = Eval(alias, "{}{c:x}");
        Assert.True(a1 is { Kind: BindingKind.BlockFunctionAlias });
        IsInherited(m1.Resolve(Reference(r1, "x")), null);
        var (r2, m2, a2) = Eval(alias, "null");
        Assert.True(a2 is { Kind: BindingKind.BlockFunctionAlias });
        IsInherited(m2.Resolve(Reference(r2, "x")), a2);
        var (r3, m3, a3) = Eval(alias, "{y}");
        Assert.True(a3 is { Kind: BindingKind.BlockFunctionAlias });
        IsInherited(m3.Resolve(Reference(r3, "x")), a3);
        var (r4, m4, a4) = Eval(alias, "{x}", strict: true);
        Assert.Null(a4);
        IsInherited(m4.Resolve(Reference(r4, "x")), null);

        // A catch parameter before the binding: the reference and the run-time binding carry none, the others keep theirs.
        const string carried = "var x = 2; x; var x; function x() {}";
        var c = SyntaxParser.Parse(CallerRequest(carried, Records("{}{c:x}")));
        var top = c.Model!.Scopes[0];
        IsInherited(c.Model.Resolve(Reference(c, "x")), null);
        IsInherited(c.Model.Resolve(Binding(c, "x", 0)), null);
        IsInherited(c.Model.Resolve(Binding(c, "x", 1)), Decl(top, "x"));
        IsInherited(c.Model.Resolve(Binding(c, "x", 2)), Decl(top, "x"));
        var n = SyntaxParser.Parse(CallerRequest(carried, null));
        IsInherited(n.Model!.Resolve(Reference(n, "x")), Decl(n.Model.Scopes[0], "x"));
        IsInherited(n.Model.Resolve(Binding(n, "x", 0)), Decl(n.Model.Scopes[0], "x"));

        // The fallback of a dynamic answer.
        const string fallback = "var x; with (o) { x; }";
        var f = SyntaxParser.Parse(CallerRequest(fallback, Records("{c:x}")));
        IsInherited(IsDynamic(f.Model!.Resolve(Reference(f, "x")), DynamicReason.With, f.Model.Scopes.Single(static s => s.Kind == ScopeKind.With)), null);
        var g = SyntaxParser.Parse(CallerRequest(fallback, null));
        IsInherited(IsDynamic(g.Model!.Resolve(Reference(g, "x")), DynamicReason.With, g.Model.Scopes.Single(static s => s.Kind == ScopeKind.With)), Decl(g.Model.Scopes[0], "x"));
    }

    // The test's own chain: from the call up its parents, each enclosing record's names as the tree declares them, stopping
    // at the variable environment, then the request's list for sloppy eval code. Iterative; reads the tree, the facts'
    // strictness and the arguments bindings, never the model's scopes.
    private static string OracleChain(SemanticModel model, SyntaxNode program, Dictionary<SyntaxNode, SyntaxNode> parents, CallExpression call, IReadOnlyList<CallerScope>? request)
    {
        static void Bound(SyntaxNode? pattern, List<string> names)
        {
            var pending = new Stack<SyntaxNode>();
            if (pattern is not null)
                pending.Push(pattern);
            while (pending.Count > 0)
            {
                switch (pending.Pop())
                {
                    case BindingIdentifier b: names.Add(b.Name); break;
                    case AssignmentPattern a: pending.Push(a.Target); break;
                    case RestElement r: pending.Push(r.Argument); break;
                    case ArrayPattern a:
                        for (var i = a.Elements.Count - 1; i >= 0; i--)
                        {
                            if (a.Elements[i] is { } e)
                                pending.Push(e);
                        }

                        break;
                    case ObjectPattern o:
                        for (var i = o.Properties.Count - 1; i >= 0; i--)
                            pending.Push(o.Properties[i] is PatternProperty p ? p.Value : o.Properties[i]);
                        break;
                }
            }
        }

        static List<string> Lexical(IEnumerable<Statement> statements, bool top)
        {
            var names = new List<string>();
            foreach (var statement in statements)
            {
                var inner = statement;
                while (inner is LabelledStatement l)
                    inner = l.Body;
                switch (inner)
                {
                    case VariableDeclaration { DeclarationKind: not DeclarationKind.Var } v:
                        foreach (var d in v.Declarations)
                            Bound(d.Target, names);
                        break;
                    case ClassDeclaration { Class.Name: { } name }:
                        names.Add(name.Name);
                        break;
                    case FunctionDeclaration { Function.Name: { } name } when !top:
                        names.Add(name.Name);
                        break;
                }
            }

            return names;
        }

        static string Entry(IEnumerable<string> names, bool isCatch) => "{" + (isCatch ? "c:" : "") + string.Join(",", names.Distinct()) + "}";

        var text = new System.Text.StringBuilder();
        var inField = false;
        for (SyntaxNode at = call; ; )
        {
            var parent = parents[at];
            switch (parent)
            {
                case BlockStatement block:
                    text.Append(Entry(Lexical(block.Body, top: false), false));
                    break;
                case SwitchStatement sw when !ReferenceEquals(at, sw.Discriminant):
                    text.Append(Entry(Lexical(sw.Cases.SelectMany(static c => c.Consequent), top: false), false));
                    break;
                case ForStatement { Init: VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head }:
                    text.Append(Entry(Lexical([head], top: false), false));
                    break;
                case ForInStatement { Left: VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head }:
                    text.Append(Entry(Lexical([head], top: false), false));
                    break;
                case ForOfStatement { Left: VariableDeclaration { DeclarationKind: not DeclarationKind.Var } head }:
                    text.Append(Entry(Lexical([head], top: false), false));
                    break;
                case CatchClause { Parameter: { } parameter }:
                {
                    var names = new List<string>();
                    Bound(parameter, names);
                    text.Append(Entry(names, true));
                    break;
                }

                case FieldDefinition field when ReferenceEquals(at, field.Value):
                    inField = true;
                    break;
                case ClassNode @class when !@class.Decorators.Contains(at) && !ReferenceEquals(at, @class.Name):
                    if (inField)
                        return text.ToString();
                    text.Append(Entry(@class.Name is { } className ? [className.Name] : [], false));
                    break;
                case FunctionNode function:
                {
                    if (model.FactsOf(function).IsStrict)
                        return text.ToString();
                    if (function.Parameters.Contains(at))
                    {
                        var names = new List<string>();
                        foreach (var p in function.Parameters)
                            Bound(p, names);
                        if (model.EnvironmentOf(function).Arguments is not null)
                            names.Add("arguments");
                        return text.Append(Entry(names, false)).ToString();
                    }

                    return text.Append(Entry(Lexical(function.Body, top: true), false)).ToString();
                }

                case StaticBlock:
                    return text.ToString();
                case ProgramNode p:
                {
                    var facts = model.FactsOf(p);
                    if (facts.IsStrict || facts.Kind is FunctionKind.Script or FunctionKind.Module)
                        return text.ToString();
                    if (facts.Kind == FunctionKind.DynamicFunction)
                    {
                        if (p.Parameters.Contains(at))
                        {
                            var names = new List<string>();
                            foreach (var parameter in p.Parameters)
                                Bound(parameter, names);
                            if (model.EnvironmentOf(p).Arguments is not null)
                                names.Add("arguments");
                            return text.Append(Entry(names, false)).ToString();
                        }

                        return text.Append(Entry(Lexical(p.Body, top: true), false)).ToString();
                    }

                    text.Append(Entry(Lexical(p.Body, top: true), false));
                    return request is null ? "null" : text.Append(Written(request)).ToString();
                }
            }

            at = parent;
        }
    }

    /// <summary>
    /// Over the determinism tests' population and the fixtures' sources: every candidate answers without throwing, its
    /// records as the test's own oracle over a parent map recomputes them from the tree; the instances shared per scope; the
    /// answer null exactly where the chain reaches sloppy eval code under a null list; and
    /// <see cref="SemanticModel.DirectEvalCalls"/> the tree's candidates in source order. Eval code is also parsed under an
    /// empty list and a list naming none of its names. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void Every_candidate_answers_its_records_over_the_population()
    {
        var items = DeterminismTests.Population.Value;
        var (calls, nulls, units) = OnDocumentedStack(() =>
        {
            var (c, n, u) = (0, 0, 0);
            void One(string name, ParseResult result, IReadOnlyList<CallerScope>? request)
            {
                if (result.Model is not { } model)
                    return;
                u++;
                var tree = result.Tree!;
                var parents = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
                var children = new List<SyntaxNode>();
                var candidates = new List<CallExpression>();
                foreach (var node in Nodes(tree))
                {
                    children.Clear();
                    node.AddChildren(children);
                    foreach (var child in children)
                        parents[child] = node;
                    if (node is CallExpression { IsOptional: false } call)
                    {
                        var callee = call.Callee;
                        while (callee is ParenthesizedExpression p)
                            callee = p.Expression;
                        if (callee is Identifier { Name: "eval" })
                            candidates.Add(call);
                    }
                }

                Assert.True(candidates.OrderBy(static x => x.Span.Offset).SequenceEqual(model.DirectEvalCalls, ReferenceEqualityComparer.Instance), name + ": the candidates");
                var byNames = new Dictionary<string, HashSet<CallerScope>>(StringComparer.Ordinal);
                foreach (var call in model.DirectEvalCalls)
                {
                    c++;
                    var answer = model.CallerScopesOf(call);
                    n += answer is null ? 1 : 0;
                    Assert.True(OracleChain(model, tree, parents, call, request) == Written(answer), name + ": the records at " + call.Span.Offset.ToString(CultureInfo.InvariantCulture) + " " + Written(answer));
                }

                // One instance per record: two calls in one block share it.
                foreach (var scope in model.Scopes)
                {
                    var seen = model.DirectEvalCalls.Where(x => ReferenceEquals(model.ScopeOf(x), scope)).Select(x => model.CallerScopesOf(x)).Where(static a => a is { Count: > 0 }).Select(static a => a![0]).Distinct().Count();
                    Assert.True(seen <= 1, name + ": one instance per scope");
                }
            }

            foreach (var item in items)
            {
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item, model: true)), null);
                if (item.Variant[0] == 'E')
                {
                    foreach (var list in new[] { Array.Empty<CallerScope>(), [new CallerScope { Names = ["éé not a name"] }, new CallerScope { IsCatch = true }] })
                    {
                        var request = DeterminismTests.Request(item, model: true);
                        One(Name(item) + " with a list", SyntaxParser.Parse(new ParseRequest(request.Source, ParseGoal.Eval, UnicodeDataHome.Tables, NoValidator.Instance)
                        {
                            Switches = request.Switches,
                            Context = new InheritedContext { InFunction = request.Context!.InFunction, CallerScopes = list },
                            Limits = request.Limits,
                            BuildSemanticModel = true,
                        }), list);
                    }
                }
            }

            foreach (var text in CallerSources)
                One("caller source \"" + text + "\"", Parse(text), null);
            foreach (var text in CallerSources)
            {
                foreach (var list in new[] { null, Records(""), Records("{z}{c:w}") })
                    One("caller source as eval code \"" + text + "\"", SyntaxParser.Parse(CallerRequest(text, list)), list);
            }

            return (c, n, u);
        });
        Assert.True(calls > CallerSources.Length && nulls > 0 && units > 1_000);
        output.WriteLine("candidates " + calls.ToString(CultureInfo.InvariantCulture) + ", of them null " + nulls.ToString(CultureInfo.InvariantCulture) + ", over units " + units.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The population check under the caller's records: stage 5a's completeness clause for eval code's variables, as stage 5b
    /// extended it, gains the case of a caller's record that binds the name. Each eval-code item of the determinism tests'
    /// population and further eval sources go through Complete under a list whose Catch clause's record binds every name
    /// the source holds, which with Annex B on refuses nothing: every eval-code variable's reference, binding resolved when
    /// it runs and dynamic answer's fallback carries no declaration, and every other binding keeps the eval body's. They go
    /// through it again under a list whose record that is not a Catch clause's binds each name an eval-code alias would
    /// bind, which refuses only where a var-declared name collides (EA0007) and otherwise withholds those aliases.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void Every_reference_and_binding_resolves_under_the_caller_s_records_over_the_population()
    {
        static ParseRequest Under(ParseRequest request, IReadOnlyList<CallerScope> list, bool model) =>
            new(request.Source, ParseGoal.Eval, request.UnicodeTables, request.RegularExpressions)
            {
                Switches = request.Switches,
                Context = new InheritedContext { InFunction = request.Context!.InFunction, CallerScopes = list },
                Limits = request.Limits,
                BuildSemanticModel = model,
            };

        // The declaration an answer carries, through a dynamic answer's fallback.
        static Declaration? Carried(Resolution answer) => (answer.Kind == ResolutionKind.Dynamic ? answer.Fallback! : answer).Declaration;

        var requests = DeterminismTests.Population.Value.Where(static i => i.Variant[0] == 'E')
            .Select(static i => (Name: Name(i), Request: DeterminismTests.Request(i, model: true)))
            .Concat(CallerSources.Concat(EvalVariableSources).Select(static t => (Name: "eval source \"" + t + "\"", Request: CallerRequest(t, null))))
            .ToList();
        var (catchUnits, nulled, aliasUnits, withheld, collided) = OnDocumentedStack(() =>
        {
            var (cu, nd, au, wh, co) = (0, 0, 0, 0, 0);
            foreach (var (name, request) in requests)
            {
                var baseline = SyntaxParser.Parse(request);
                if (baseline.Model is not { } model)
                    continue;
                var baseNodes = Nodes(baseline.Tree!);
                var names = baseNodes.Select(static n => n switch { Identifier i => i.Name, BindingIdentifier b => b.Name, _ => null })
                    .OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
                if (names.Length == 0)
                    continue;

                // Every name in a Catch clause's record, an empty record inside it.
                CallerScope[] catchList = [new CallerScope(), new CallerScope { Names = names, IsCatch = true }];
                var on = SyntaxParser.Parse(Under(request, catchList, model: true));
                Assert.True(on.Outcome == baseline.Outcome, name + ": refused under a Catch clause's record");
                Complete(name + " under a Catch clause's record", on, SyntaxParser.Parse(Under(request, catchList, model: false)), catchList);
                cu++;
                var onNodes = Nodes(on.Tree!);
                Assert.Equal(baseNodes.Count, onNodes.Count);
                for (var k = 0; k < baseNodes.Count; k++)
                {
                    if (baseNodes[k] is Identifier reference && Carried(model.Resolve(reference)) is not null && Carried(on.Model!.Resolve((Identifier)onNodes[k])) is null)
                        nd++;
                }

                // Each name an eval-code alias binds, in a record that is not a Catch clause's.
                var aliased = model.Scopes.SelectMany(scope => scope.Declarations.Where(d => d.IsBlockFunction && model.AnnexBAlias(d) is { Kind: BindingKind.BlockFunctionAlias }
                        && ReferenceEquals(VarScopeOf(model, scope), model.Scopes[0])))
                    .Select(static d => d.Name).Distinct(StringComparer.Ordinal).ToArray();
                if (aliased.Length == 0)
                    continue;
                CallerScope[] lexicalList = [new CallerScope { Names = aliased }];
                var under = SyntaxParser.Parse(Under(request, lexicalList, model: true));
                if (under.Outcome != ParseOutcome.Accepted)
                {
                    Assert.True(under.Diagnostics.All(static d => d.Code == "JSE1907"), name + ": refused otherwise than by EA0007");
                    co++;
                    continue;
                }

                Complete(name + " under a record that is not a Catch clause's", under, SyntaxParser.Parse(Under(request, lexicalList, model: false)), lexicalList);
                au++;
                var m = under.Model!;
                foreach (var scope in m.Scopes)
                {
                    if (!ReferenceEquals(VarScopeOf(m, scope), m.Scopes[0]))
                        continue;
                    foreach (var declaration in scope.Declarations.Where(d => d.IsBlockFunction && aliased.Contains(d.Name)))
                    {
                        Assert.True(m.AnnexBAlias(declaration) is null, name + ": an alias kept that a record withholds, " + declaration.Name);
                        wh++;
                    }
                }
            }

            return (cu, nd, au, wh, co);
        });
        Assert.True(catchUnits > CallerSources.Length && nulled > 0 && aliasUnits > 0 && withheld > 0);
        output.WriteLine("under a Catch clause's record: units " + catchUnits.ToString(CultureInfo.InvariantCulture) + ", references carrying none " + nulled.ToString(CultureInfo.InvariantCulture)
            + "; under a record that is not a Catch clause's: units " + aliasUnits.ToString(CultureInfo.InvariantCulture) + ", block functions " + withheld.ToString(CultureInfo.InvariantCulture)
            + ", refused by EA0007 " + collided.ToString(CultureInfo.InvariantCulture));
    }

    // Eval sources whose variables, run-time bindings, fallbacks and aliases the population check reads under the caller's
    // records. (Added 2026-11-26, at SJL-15.2 stage 5c.)
    private static readonly string[] EvalVariableSources =
    [
        "var x = 2; x; var x; function x() {}", "var x; with (o) { x; }", "{ function g() {} } g;", "for (var k in o) k; for (var v of o) v; var [d = 1] = o; d;",
        "{ function g() {} } var h; { function h() {} } g; h;", "switch (0) { case 0: function s() {} } s;", "if (1) function i() {} i;",
        "function inner() { { function g() {} } return g; } { function g() {} } g;", "{ function g() {} } with (o) { g; } eval('');",
    ];

    private static readonly string[] CallerSources =
    [
        "{ let x; eval('var x'); }", "function f() { let a; { const b = 1; eval(''); } }", "switch (0) { case 0: let x; eval(''); }",
        "for (let i = eval('');;) { eval(''); }", "for (let x of eval('')) ;", "switch (eval('')) { case 0: let x; }",
        "function f() { { function g() {} class K {} eval(''); } }", "function f(a, b = eval(''), c) {}", "((a = eval('')) => 0);",
        "(function a(x = eval('')) {});", "try {} catch ({ e, f: [g] }) { eval(''); }", "try {} catch { let y; eval(''); }",
        "try {} catch ({ a = eval('') }) {}", "function f(o) { let x; with (o) { eval(''); } }", "class C { x = eval(''); }",
        "class C { x = class D { [eval('')]() {} }; }", "class C { static { { let q; eval(''); } } }", "class C extends (eval(''), Object) {}",
        "var x; { let y; eval(''); eval(''); } (eval)('');",
    ];

    /// <summary>
    /// The identity forms (B4's off-path list test, B5b's section 8): with the member on, eval code under a null list, an
    /// empty one and one naming none of its names, and a list's entries permuted, gives the same outcome and the same
    /// dump but for the caller's-records lines, every member of the previous stages answering alike; an empty list and null
    /// differ only in those lines. The population's eval-code items and the fixtures' sources. <i>(Added 2026-11-26, at
    /// SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void Null_empty_non_colliding_and_permuted_lists_answer_alike_on_every_earlier_member()
    {
        static string Earlier(ParseResult result) => string.Join("\n", System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(result)).Split('\n')
            .Where(static l => !l.StartsWith("model-eval-call ", StringComparison.Ordinal) && !l.StartsWith("model-caller-scope ", StringComparison.Ordinal)));

        var sources = DeterminismTests.Population.Value.Where(static i => i.Variant[0] == 'E').Select(static i => i.Text).Concat(CallerSources)
            .Concat(["var x = 1; x; { function g() {} } g; with (o) { x; }", "let a; var b; eval(''); function c() {}"]).ToList();
        var compared = OnDocumentedStack(() =>
        {
            var count = 0;
            foreach (var text in sources)
            {
                var baseline = SyntaxParser.Parse(CallerRequest(text, null));
                foreach (var list in new[] { Records(""), Records("{zz}{c:yy}"), Records("{c:yy}{zz}") })
                {
                    var other = SyntaxParser.Parse(CallerRequest(text, list));
                    Assert.True(baseline.Outcome == other.Outcome, text + ": the outcome");
                    Assert.True(Earlier(baseline) == Earlier(other), text + ": an earlier member answers otherwise under " + Written(list));
                }

                count++;
            }

            return count;
        });
        Assert.Equal(sources.Count, compared);

        // Permuting colliding entries changes no outcome or row.
        foreach (var (a, b) in new[] { ("{x}{c:y}", "{c:y}{x}"), ("{c:x}{y}", "{y}{c:x}") })
        {
            var ra = SyntaxParser.Parse(CallerRequest("var x; var y;", Records(a), allErrors: true));
            var rb = SyntaxParser.Parse(CallerRequest("var x; var y;", Records(b), allErrors: true));
            Assert.Equal(ra.Diagnostics.Select(static d => (d.Code, d.Span.Offset)), rb.Diagnostics.Select(static d => (d.Code, d.Span.Offset)));
        }
    }

    /// <summary>
    /// The dump's caller's-records lines (D-js-part-g): one <c>model-eval-call</c> line per candidate in order, with its
    /// entries' count or <c>-</c>, and one <c>model-caller-scope</c> line per entry; a dump without a model is unchanged.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    [Fact]
    public void The_dump_s_model_section_holds_each_candidate_s_records()
    {
        const string text = "function f(a = eval('')) { let x; { try {} catch (e) { eval(''); } } }";
        var lines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(Model(text).Result)).Split('\n');
        var calls = lines.Where(static l => l.StartsWith("model-eval-call ", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, calls.Count);
        Assert.EndsWith(" 1", calls[0], StringComparison.Ordinal);
        Assert.EndsWith(" 4", calls[1], StringComparison.Ordinal);
        var records = lines.Where(static l => l.StartsWith("model-caller-scope ", StringComparison.Ordinal)).ToList();
        Assert.Equal(["model-caller-scope 0 \"a\" \"arguments\"", "model-caller-scope 0", "model-caller-scope 1 \"e\"", "model-caller-scope 0", "model-caller-scope 0 \"x\""], records);
        Assert.DoesNotContain(System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(Parse(text, model: false))).Split('\n'), static l => l.StartsWith("model-", StringComparison.Ordinal));
        var evalNull = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(SyntaxParser.Parse(CallerRequest("eval('')", null)))).Split('\n');
        Assert.EndsWith(" -", evalNull.Single(static l => l.StartsWith("model-eval-call ", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // The module record (SJL-15.2 stage 6; the session's decision D-js-module-record). (Added 2026-11-27, at SJL-15.2 stage 6.)

    private static (ParseResult Result, SemanticModel Model, ModuleRecord Record) ModuleOf(string text, ProposalFlags proposals = ProposalFlags.None)
    {
        var (result, model) = Model(text, ParseGoal.Module, proposals);
        Assert.NotNull(model.Module);
        return (result, model, model.Module!);
    }

    // The record as text: the requests (specifier, phase number, attributes as written, statements), then the import
    // entries, the local, indirect and star export entries, each request by its place in the record.
    private static string Rendered(ModuleRecord record)
    {
        var requests = record.RequestedModules.ToList();
        string R(ModuleRequest? request) => request is null ? "-" : requests.IndexOf(request).ToString(CultureInfo.InvariantCulture);
        static string N(string? text) => text ?? "-";
        var parts = new List<string> { "tla " + (record.HasTopLevelAwait ? "1" : "0") };
        parts.AddRange(requests.Select(r => "req " + r.Specifier + " " + ((int)r.Phase).ToString(CultureInfo.InvariantCulture)
            + " [" + string.Join(",", r.Attributes.Select(static a => a.Key + "=" + a.Value)) + "] x" + r.Statements.Count.ToString(CultureInfo.InvariantCulture)));
        parts.AddRange(record.ImportEntries.Select(e => "import " + R(e.ModuleRequest) + " " + e.ImportKind + " " + N(e.ImportName) + " " + e.LocalName));
        parts.AddRange(record.LocalExportEntries.Select(e => "local " + N(e.ExportName) + " " + N(e.LocalName)));
        parts.AddRange(record.IndirectExportEntries.Select(e => "indirect " + N(e.ExportName) + " " + R(e.ModuleRequest) + " " + e.ImportKind + " " + N(e.ImportName)));
        parts.AddRange(record.StarExportEntries.Select(e => "star " + R(e.ModuleRequest)));
        return string.Join("; ", parts);
    }

    /// <summary>
    /// The record of each form (ECMA-262 ParseModule; ImportEntries' and ExportEntries' informative tables, one fixture per
    /// row, and the rewrites of step 10): the requests in the order of their first statement, duplicates removed by
    /// ModuleRequestsEqual; the import entries; and the export entries partitioned into the local, indirect and star lists,
    /// each in source order. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Theory]
    // The ImportEntry table, one row each, and the default binding before the namespace or the named ones.
    [InlineData("import v from \"mod\";", "tla 0; req mod 0 [] x1; import 0 Name default v")]
    [InlineData("import * as ns from \"mod\";", "tla 0; req mod 0 [] x1; import 0 NamespaceObject - ns")]
    [InlineData("import {x} from \"mod\";", "tla 0; req mod 0 [] x1; import 0 Name x x")]
    [InlineData("import {x as v} from \"mod\";", "tla 0; req mod 0 [] x1; import 0 Name x v")]
    [InlineData("import \"mod\";", "tla 0; req mod 0 [] x1")]
    [InlineData("import x, * as ns from \"m\";", "tla 0; req m 0 [] x1; import 0 Name default x; import 0 NamespaceObject - ns")]
    [InlineData("import x, {y, z as w} from \"m\";", "tla 0; req m 0 [] x1; import 0 Name default x; import 0 Name y y; import 0 Name z w")]
    [InlineData("import {default as y} from \"m\";", "tla 0; req m 0 [] x1; import 0 Name default y")]
    [InlineData("import {\"a b\" as z} from \"m\";", "tla 0; req m 0 [] x1; import 0 Name a b z")]
    [InlineData("import {\\u0061} from \"m\";", "tla 0; req m 0 [] x1; import 0 Name a a")]
    [InlineData("import {} from \"m\";", "tla 0; req m 0 [] x1")]
    [InlineData("import x, {} from \"m\";", "tla 0; req m 0 [] x1; import 0 Name default x")]
    // The ExportEntry table, one row each.
    [InlineData("export var v;", "tla 0; local v v")]
    [InlineData("export default function f() {}", "tla 0; local default f")]
    [InlineData("export default function () {}", "tla 0; local default *default*")]
    [InlineData("export default 42;", "tla 0; local default *default*")]
    [InlineData("var x; export {x};", "tla 0; local x x")]
    [InlineData("var v; export {v as x};", "tla 0; local x v")]
    [InlineData("export {x} from \"mod\";", "tla 0; req mod 0 [] x1; indirect x 0 Name x")]
    [InlineData("export {v as x} from \"mod\";", "tla 0; req mod 0 [] x1; indirect x 0 Name v")]
    [InlineData("export * from \"mod\";", "tla 0; req mod 0 [] x1; star 0")]
    [InlineData("export * as ns from \"mod\";", "tla 0; req mod 0 [] x1; indirect ns 0 All -")]
    // The anonymous defaults, the declarations' bound names, StringValues and string names.
    [InlineData("export default function* () {}", "tla 0; local default *default*")]
    [InlineData("export default async function () {}", "tla 0; local default *default*")]
    [InlineData("export default async function* () {}", "tla 0; local default *default*")]
    [InlineData("export default class {}", "tla 0; local default *default*")]
    [InlineData("export default class C {}", "tla 0; local default C")]
    [InlineData("export var {a, b: [c]} = o;", "tla 0; local a a; local c c")]
    [InlineData("export var a, b;", "tla 0; local a a; local b b")]
    [InlineData("export let x = 1; export const y = 2; export function f() {} export class C {}", "tla 0; local x x; local y y; local f f; local C C")]
    [InlineData("var a; export {\\u0061};", "tla 0; local a a")]
    [InlineData("var a; export { a as \"b c\" };", "tla 0; local b c a")]
    [InlineData("export * as \"s\" from \"m\";", "tla 0; req m 0 [] x1; indirect s 0 All -")]
    [InlineData("export { \"x\" as y } from \"m\";", "tla 0; req m 0 [] x1; indirect y 0 Name x")]
    // The rewrites of step 10.a.ii, and rewritten entries among the true indirect ones in source order.
    [InlineData("import {a} from \"m\"; export {a as b};", "tla 0; req m 0 [] x1; import 0 Name a a; indirect b 0 Name a")]
    [InlineData("import * as ns from \"m\"; export {ns};", "tla 0; req m 0 [] x1; import 0 NamespaceObject - ns; indirect ns 0 All -")]
    [InlineData("import * as ns from \"m\"; export { ns as default };", "tla 0; req m 0 [] x1; import 0 NamespaceObject - ns; indirect default 0 All -")]
    [InlineData("import d from \"m\"; export { d };", "tla 0; req m 0 [] x1; import 0 Name default d; indirect d 0 Name default")]
    [InlineData("import a from \"m\"; export default a;", "tla 0; req m 0 [] x1; import 0 Name default a; local default *default*")]
    [InlineData("import {a} from \"m\"; export {x} from \"n\"; export {a}; export * as s from \"o\";", "tla 0; req m 0 [] x1; req n 0 [] x1; req o 0 [] x1; import 0 Name a a; indirect x 1 Name x; indirect a 0 Name a; indirect s 2 All -")]
    // The requests: duplicates removed, by SV, in the order of their first statement.
    [InlineData("import a from \"m\"; import b from \"m\";", "tla 0; req m 0 [] x2; import 0 Name default a; import 0 Name default b")]
    [InlineData("import \"m\"; import 'm'; import \"\\u006d\";", "tla 0; req m 0 [] x3")]
    [InlineData("export {x} from \"m\"; import \"n\";", "tla 0; req m 0 [] x1; req n 0 [] x1; indirect x 0 Name x")]
    [InlineData("import \"m\" with { type: \"json\" }; import \"m\" with { type: \"css\" };", "tla 0; req m 0 [type=json] x1; req m 0 [type=css] x1")]
    [InlineData("import \"m\" with { type: \"json\", a: \"b\" }; import \"m\" with { a: \"b\", type: \"json\" };", "tla 0; req m 0 [type=json,a=b] x2")]
    [InlineData("import \"m\" with { type: \"json\" }; import \"m\" with { \"type\": \"json\" };", "tla 0; req m 0 [type=json] x2")]
    [InlineData("import \"m\" with {}; import \"m\";", "tla 0; req m 0 [] x2")]
    [InlineData("export * from \"m\" with { type: \"json\" };", "tla 0; req m 0 [type=json] x1; star 0")]
    [InlineData("export {} from \"m\";", "tla 0; req m 0 [] x1")]
    [InlineData("import(\"m\");", "tla 0")]
    [InlineData("export * from \"m\"; export * from \"n\";", "tla 0; req m 0 [] x1; req n 0 [] x1; star 0; star 1")]
    [InlineData("export { a } from \"m\"; export { a as b } from \"m\";", "tla 0; req m 0 [] x2; indirect a 0 Name a; indirect b 0 Name a")]
    // No import and no export: a record with empty lists.
    [InlineData("var x = 1;", "tla 0")]
    [InlineData("", "tla 0")]
    public void Each_form_gives_the_record_ParseModule_builds(string text, string expected)
    {
        Assert.Equal(expected, Rendered(ModuleOf(text).Record));
    }

    /// <summary>
    /// The attributes of a shared request are its first statement's in that statement's order; each statement's own order
    /// stays its own, through <see cref="ModuleRequest.Statements"/>; the specification's [[Attributes]] is the list
    /// sorted by key, ordinally. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void A_shared_request_shows_its_first_statement_s_attribute_order()
    {
        var (_, _, record) = ModuleOf("import \"m\" with { type: \"json\", a: \"b\" }; import \"m\" with { a: \"b\", type: \"json\" };");
        var request = Assert.Single(record.RequestedModules);
        Assert.Equal(["type", "a"], request.Attributes.Select(static a => a.Key));
        Assert.Equal(2, request.Statements.Count);
        Assert.Equal(["a", "type"], ((ImportDeclaration)request.Statements[1]).Attributes!.Select(static a => ((IdentifierName)a.Key).Name));
        Assert.Equal(["a", "type"], request.Attributes.Select(static a => a.Key).Order(StringComparer.Ordinal));
    }

    /// <summary>A lone surrogate in a specifier is carried as it is, the SV, which an engine refuses at its own row. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i></summary>
    [Fact]
    public void A_lone_surrogate_specifier_is_carried_as_it_is()
    {
        var request = Assert.Single(ModuleOf("import \"\\uD800\";").Record.RequestedModules);
        Assert.Equal("\uD800", request.Specifier);
    }

    /// <summary>
    /// The phase, the library's, beyond the pinned edition: a deferred and an evaluating request of one specifier are two,
    /// the deferred one's entry the namespace form; a source-phase binding's entry is its own kind with no import name, never
    /// <c>"default"</c>, and an export of it is rewritten with that kind. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void The_phase_keeps_a_deferred_or_source_request_apart_and_a_source_binding_is_its_own_kind()
    {
        const ProposalFlags declarations = ProposalFlags.ImportSourceDeclarations | ProposalFlags.ImportDeferDeclarations;
        Assert.Equal("tla 0; req m 2 [] x1; req m 0 [] x1; import 0 NamespaceObject - ns; import 1 NamespaceObject - ns2",
            Rendered(ModuleOf("import defer * as ns from \"m\"; import * as ns2 from \"m\";", declarations).Record));
        Assert.Equal("tla 0; req m 1 [] x1; import 0 Source - s", Rendered(ModuleOf("import source s from \"m\";", declarations).Record));
        Assert.Equal("tla 0; req m 1 [] x1; import 0 Source - s; indirect s 0 Source -", Rendered(ModuleOf("import source s from \"m\"; export { s };", declarations).Record));
        Assert.Equal(ImportPhase.Evaluation, ModuleOf("export * from \"m\";").Record.RequestedModules[0].Phase);
    }

    /// <summary>
    /// [[HasTLA]] is the tree's fact, read: true for an <c>await</c> anywhere Contains looks, a class's heritage and computed
    /// keys and an object literal's computed keys included, and for <c>await using</c> (ecma262 main's, beyond the pinned
    /// edition); false for an <c>await</c> only inside a function, an async arrow, a method or a static block, and for a module
    /// with none. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Theory]
    [InlineData("await 1;", true)]
    [InlineData("for await (x of y);", true)]
    [InlineData("{ await 1; }", true)]
    [InlineData("if (a) await 1;", true)]
    [InlineData("try { await 1; } catch {}", true)]
    [InlineData("L: await 1;", true)]
    [InlineData("{ for await (x of y); }", true)]
    [InlineData("class C extends (await x) {}", true)]
    [InlineData("class C { [await x]() {} }", true)]
    [InlineData("({ [await x]: 1 });", true)]
    [InlineData("({ [await x]() {} });", true)]
    [InlineData("await using x = y;", true)]
    [InlineData("export var f = async () => { await 0; };", false)]
    [InlineData("(async function () { await 1; })();", false)]
    [InlineData("async function f() { for await (x of y); }", false)]
    [InlineData("class C { static { (async () => { await 1; })(); } }", false)]
    [InlineData("class C { async m() { await 1; } }", false)]
    [InlineData("var x = 1; export { x };", false)]
    public void The_record_s_top_level_await_is_the_tree_s(string text, bool expected)
    {
        var (result, _, record) = ModuleOf(text);
        Assert.Equal(expected, record.HasTopLevelAwait);
        Assert.Equal(result.Tree!.HasTopLevelAwait, record.HasTopLevelAwait);
        Assert.Equal(expected, ContainsAwait(result.Tree!));
    }

    /// <summary>
    /// Only a Module has a record: a Script, eval code and the dynamic function's goals have none, a Script with top-level
    /// <c>await</c> on keeping the fact on its tree alone. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void Only_a_Module_has_a_record()
    {
        Assert.Null(Model("var x;").Model.Module);
        Assert.Null(Model("var x;", ParseGoal.Eval).Model.Module);
        Assert.Null(Model("a, b", ParseGoal.FormalParameters).Model.Module);
        Assert.Null(Model("return 1;", ParseGoal.FunctionBody).Model.Module);
        var script = SyntaxParser.Parse(new ParseRequest(new SourceText("await 1;"), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Script) with { TopLevelAwait = true },
            BuildSemanticModel = true,
        });
        Assert.Equal(ParseOutcome.Accepted, script.Outcome);
        Assert.True(script.Tree!.HasTopLevelAwait);
        Assert.Null(script.Model!.Module);
        Assert.Null(Parse("import x from \"m\";", ParseGoal.Module, model: false).Model);
    }

    /// <summary>
    /// The record's instances: one record; every entry's request the element of the requests itself; an import entry's
    /// declaration the one <see cref="SemanticModel.DeclarationOf"/> answers, and a local export entry's the module scope's
    /// first of its name, which a top-level reference resolves to; every list read-only; every entry's node in the tree, in
    /// the Module scope. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void The_record_shares_its_instances_and_its_declarations_are_the_model_s()
    {
        const string text = "import a, {b} from \"m\"; import * as ns from \"m\"; var v; var v; export { v, a as x, ns }; export default function () {} export {c} from \"m\"; export * from \"n\"; v; ";
        var (result, model, record) = ModuleOf(text);
        Assert.Same(model.Module, model.Module);
        Assert.Equal(2, record.RequestedModules.Count);
        foreach (var entry in record.ImportEntries)
        {
            Assert.Contains(entry.ModuleRequest, record.RequestedModules);
            Assert.Same(entry.Declaration, model.DeclarationOf(entry.Node));
            Assert.Equal(BindingKind.Import, entry.Declaration.Kind);
        }

        var moduleScope = model.Scopes[0];
        Assert.Equal(ScopeKind.Module, moduleScope.Kind);
        foreach (var entry in record.LocalExportEntries)
        {
            Assert.Null(entry.ModuleRequest);
            Assert.Same(moduleScope.Declarations.First(d => d.Name == entry.LocalName), entry.Declaration);
        }

        foreach (var entry in record.IndirectExportEntries.Concat(record.StarExportEntries))
        {
            Assert.Null(entry.Declaration);
            Assert.Contains(entry.ModuleRequest!, record.RequestedModules);
        }

        var local = record.LocalExportEntries.Single(static e => e.ExportName == "v");
        Assert.Same(local.Declaration, model.Resolve(Reference(result, "v")).Declaration);
        Assert.Equal(BindingKind.ExportDefault, record.LocalExportEntries.Single(static e => e.ExportName == "default").Declaration!.Kind);
        Assert.IsType<ExportDefaultDeclaration>(record.LocalExportEntries.Single(static e => e.ExportName == "default").Node);
        Assert.IsType<ExportAllDeclaration>(Assert.Single(record.StarExportEntries).Node);
        Assert.All(record.IndirectExportEntries, static e => Assert.IsType<ExportSpecifier>(e.Node));
        var nodes = Nodes(result.Tree!).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var node in record.ImportEntries.Select(static e => (SyntaxNode)e.Node).Concat(record.LocalExportEntries.Concat(record.IndirectExportEntries).Concat(record.StarExportEntries).Select(static e => e.Node)))
        {
            Assert.Contains(node, nodes);
            Assert.Same(moduleScope, model.ScopeOf(node));
        }

        foreach (var list in new System.Collections.IEnumerable[] { record.RequestedModules, record.ImportEntries, record.LocalExportEntries, record.IndirectExportEntries, record.StarExportEntries, record.RequestedModules[0].Attributes, record.RequestedModules[0].Statements })
            Assert.True(list is System.Collections.IList { IsReadOnly: true }, "a list is writable");
    }

    // The test's own oracle of ParseModule over the tree, iterative and apart from the walk: ModuleRequests with
    // ModuleRequestsEqual and the phase, ImportEntries, ExportEntries with step 10's partition, and Contains await.
    private static string Oracle(ProgramNode program)
    {
        static string Name(SyntaxNode node) => node is StringLiteral s ? s.Value : ((IdentifierName)node).Name;
        var requests = new List<(string Specifier, ImportPhase Phase, List<(string, string)> Attributes, int Count)>();
        int Request(StringLiteral source, IReadOnlyList<ImportAttribute>? attributes, ImportPhase phase)
        {
            var list = (attributes ?? []).Select(a => (Name(a.Key), a.Value.Value)).ToList();
            for (var i = 0; i < requests.Count; i++)
            {
                if (requests[i].Specifier == source.Value && requests[i].Phase == phase && requests[i].Attributes.Count == list.Count && list.All(requests[i].Attributes.Contains))
                {
                    var known = requests[i];
                    known.Count++;
                    requests[i] = known;
                    return i;
                }
            }

            requests.Add((source.Value, phase, list, 1));
            return requests.Count - 1;
        }

        var imports = new List<(int Request, string Kind, string? Name, string Local)>();
        var exports = new List<(string? Export, int Request, string Kind, string? Import, string? Local)>();
        foreach (var statement in program.Body)
        {
            switch (statement)
            {
                case ImportDeclaration import:
                {
                    var r = Request(import.Source, import.Attributes, import.Phase);
                    if (import.DefaultBinding is { } d)
                        imports.Add(import.Phase == ImportPhase.Source ? (r, "Source", null, d.Name) : (r, "Name", "default", d.Name));
                    if (import.NamespaceBinding is { } ns)
                        imports.Add((r, "NamespaceObject", null, ns.Name));
                    foreach (var s in import.NamedImports ?? [])
                        imports.Add((r, "Name", s.Imported is null ? s.Local.Name : Name(s.Imported), s.Local.Name));
                    break;
                }

                case ExportNamedDeclaration { Declaration: { } declaration }:
                {
                    var names = new List<string>();
                    var pending = new Stack<SyntaxNode>();
                    var children = new List<SyntaxNode>();
                    pending.Push(declaration);
                    while (pending.Count > 0)
                    {
                        var node = pending.Pop();
                        if (node is BindingIdentifier b)
                        {
                            names.Add(b.Name);
                            continue;
                        }

                        if (node is FunctionNode f)
                        {
                            if (f.Name is { } fn)
                                names.Add(fn.Name);
                            continue;
                        }

                        if (node is ClassNode c)
                        {
                            if (c.Name is { } cn)
                                names.Add(cn.Name);
                            continue;
                        }

                        if (node is Expression)
                            continue;
                        children.Clear();
                        node.AddChildren(children);
                        for (var i = children.Count - 1; i >= 0; i--)
                            pending.Push(children[i]);
                    }

                    exports.AddRange(names.Select(static n => ((string?)n, -1, "None", (string?)null, (string?)n)));
                    break;
                }

                case ExportNamedDeclaration export:
                {
                    var r = export.Source is { } from ? Request(from, export.Attributes, ImportPhase.Evaluation) : -1;
                    foreach (var s in export.Specifiers)
                        exports.Add(r < 0 ? (Name(s.Exported ?? s.Local), -1, "None", null, Name(s.Local)) : (Name(s.Exported ?? s.Local), r, "Name", Name(s.Local), null));
                    break;
                }

                case ExportDefaultDeclaration exportDefault:
                    exports.Add(("default", -1, "None", null, exportDefault.Declaration switch
                    {
                        FunctionDeclaration { Function.Name: { } n } => n.Name,
                        ClassDeclaration { Class.Name: { } n } => n.Name,
                        _ => "*default*",
                    }));
                    break;
                case ExportAllDeclaration all:
                {
                    var r = Request(all.Source, all.Attributes, ImportPhase.Evaluation);
                    exports.Add(all.Exported is null ? (null, r, "AllButDefault", null, null) : (Name(all.Exported), r, "All", null, null));
                    break;
                }
            }
        }

        var parts = new List<string> { "tla " + (ContainsAwait(program) ? "1" : "0") };
        parts.AddRange(requests.Select(static r => "req " + r.Specifier + " " + ((int)r.Phase).ToString(CultureInfo.InvariantCulture) + " [" + string.Join(",", r.Attributes.Select(static a => a.Item1 + "=" + a.Item2)) + "] x" + r.Count.ToString(CultureInfo.InvariantCulture)));
        parts.AddRange(imports.Select(static i => "import " + i.Request.ToString(CultureInfo.InvariantCulture) + " " + i.Kind + " " + (i.Name ?? "-") + " " + i.Local));
        var local = new List<string>();
        var indirect = new List<string>();
        var star = new List<string>();
        foreach (var e in exports)
        {
            if (e.Request < 0)
            {
                var ie = imports.FindIndex(i => i.Local == e.Local);
                if (ie < 0)
                    local.Add("local " + e.Export + " " + e.Local);
                else if (imports[ie].Kind == "NamespaceObject")
                    indirect.Add("indirect " + e.Export + " " + imports[ie].Request.ToString(CultureInfo.InvariantCulture) + " All -");
                else
                    indirect.Add("indirect " + e.Export + " " + imports[ie].Request.ToString(CultureInfo.InvariantCulture) + " " + imports[ie].Kind + " " + (imports[ie].Name ?? "-"));
            }
            else if (e.Kind == "AllButDefault")
            {
                star.Add("star " + e.Request.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                indirect.Add("indirect " + (e.Export ?? "-") + " " + e.Request.ToString(CultureInfo.InvariantCulture) + " " + e.Kind + " " + (e.Import ?? "-"));
            }
        }

        parts.AddRange(local);
        parts.AddRange(indirect);
        parts.AddRange(star);
        return string.Join("; ", parts);
    }

    // Contains await of the module's items, the test's own reading of ECMA-262's Contains: it stops at every function, an
    // arrow's included (whose Contains answers only new.target, this and super), at a field's initialiser and a static
    // block, and enters a class's heritage and its elements' computed keys, an object literal's computed keys, and
    // decorators, which the tree reads beyond the edition.
    private static bool ContainsAwait(ProgramNode program)
    {
        var pending = new Stack<SyntaxNode>();
        var children = new List<SyntaxNode>();
        foreach (var statement in program.Body)
            pending.Push(statement);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            switch (node)
            {
                case AwaitExpression:
                case ForOfStatement { IsAwait: true }:
                case VariableDeclaration { DeclarationKind: DeclarationKind.AwaitUsing }:
                    return true;
                case FunctionNode:
                case StaticBlock:
                    continue;
                case FieldDefinition field:
                    foreach (var decorator in field.Decorators)
                        pending.Push(decorator);
                    pending.Push(field.Key);
                    continue;
            }

            children.Clear();
            node.AddChildren(children);
            foreach (var child in children)
                pending.Push(child);
        }

        return false;
    }

    /// <summary>
    /// Over every accepted item of the determinism tests' population, and the fixtures' Modules: a Module's record is the
    /// one the test's own oracle of the tree recomputes, field by field, its top-level-await fact the tree's and Contains
    /// await; every local export entry has a declaration; and no other goal has a record. <i>(Added 2026-11-27, at SJL-15.2
    /// stage 6.)</i>
    /// </summary>
    [Fact]
    public void Every_accepted_module_s_record_is_the_one_an_oracle_of_the_tree_recomputes()
    {
        var items = DeterminismTests.Population.Value;
        var (modules, others, entries) = OnDocumentedStack(() =>
        {
            var (m, o, e) = (0, 0, 0);
            void One(string name, ParseResult result, bool module)
            {
                if (result.Model is not { } model)
                    return;
                if (!module)
                {
                    Assert.True(model.Module is null, name + ": a record for a goal that is not Module");
                    o++;
                    return;
                }

                var record = model.Module;
                Assert.True(record is not null, name + ": no record");
                m++;
                e += record!.ImportEntries.Count + record.LocalExportEntries.Count + record.IndirectExportEntries.Count + record.StarExportEntries.Count;
                Assert.True(Oracle(result.Tree!) == Rendered(record), name + ": " + Rendered(record) + " against the oracle's " + Oracle(result.Tree!));
                Assert.True(result.Tree!.HasTopLevelAwait == record.HasTopLevelAwait, name + ": the top-level-await fact differs from the tree's");
                Assert.All(record.LocalExportEntries, entry => Assert.True(entry.Declaration is not null, name + ": " + entry.LocalName + " has no declaration"));
            }

            foreach (var item in items)
                One(Name(item), SyntaxParser.Parse(DeterminismTests.Request(item, model: true)), item.Variant == "module");
            foreach (var text in ModuleFixtureSources)
                One("fixture \"" + text + "\"", Parse(text, ParseGoal.Module), module: true);
            return (m, o, e);
        });

        Assert.True(modules > 0 && others > 0 && entries > 0);
        output.WriteLine("module records: modules " + modules.ToString(CultureInfo.InvariantCulture) + ", entries " + entries.ToString(CultureInfo.InvariantCulture) + ", other goals " + others.ToString(CultureInfo.InvariantCulture));
    }

    private static readonly string[] ModuleFixtureSources =
    [
        "import a, {b, c as d, \"e f\" as g} from \"m\" with { type: \"json\" }; import * as ns from \"m\" with { type: \"json\" }; export { a, d as \"h i\", ns as default }; export * from \"n\"; export * as s from \"o\"; var v, w; export { v as x, w }; export {y} from \"m\" with { type: \"json\" }; await 1;",
        "export default class {} export var [p, {q}] = r; export function f() {} export let z; import(\"x\");",
        "import \"a\"; import {} from \"b\"; export {} from \"c\"; import x, {} from \"a\"; export { x as y };",
    ];

    /// <summary>
    /// The dump's module-record lines (D-js-part-g): the fact line, each request with its attributes in its first
    /// statement's order, then the entries; a Script's model and a dump without a model gain none. <i>(Added 2026-11-27, at
    /// SJL-15.2 stage 6.)</i>
    /// </summary>
    [Fact]
    public void The_dump_s_model_section_holds_the_module_record()
    {
        const string text = "import a from \"m\" with { type: \"json\", b: \"c\" }; var v; export { v as w }; export { a }; export * from \"n\"; export * as s from \"m\" with { b: \"c\", type: \"json\" };";
        var lines = System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(ModuleOf(text).Result)).Split('\n');
        var module = lines.Where(static l => l.StartsWith("model-module ", StringComparison.Ordinal) || l.StartsWith("model-request ", StringComparison.Ordinal)
            || l.StartsWith("model-attribute ", StringComparison.Ordinal) || l.StartsWith("model-import ", StringComparison.Ordinal) || l.StartsWith("model-export-", StringComparison.Ordinal)).ToList();
        Assert.Equal("model-module 0", module[0]);
        Assert.StartsWith("model-request 0 \"m\" 0 ", module[1], StringComparison.Ordinal);
        Assert.Equal(6, module[1].Split(' ').Length);
        Assert.Equal(["model-attribute \"type\" \"json\"", "model-attribute \"b\" \"c\""], module.Skip(2).Take(2));
        Assert.StartsWith("model-request 1 \"n\" 0 ", module[4], StringComparison.Ordinal);
        Assert.StartsWith("model-import 0 1 \"default\" \"a\" ", module[5], StringComparison.Ordinal);
        Assert.StartsWith("model-export-local \"w\" \"v\" ", module[6], StringComparison.Ordinal);
        Assert.StartsWith("model-export-indirect \"a\" 0 1 \"default\" ", module[7], StringComparison.Ordinal);
        Assert.StartsWith("model-export-indirect \"s\" 0 3 - ", module[8], StringComparison.Ordinal);
        Assert.StartsWith("model-export-star 1 ", module[9], StringComparison.Ordinal);
        Assert.Equal(10, module.Count);
        Assert.DoesNotContain(System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(Model("var v;").Result)).Split('\n'), static l => l.StartsWith("model-module", StringComparison.Ordinal));
        Assert.DoesNotContain(System.Text.Encoding.UTF8.GetString(SyntaxDump.Dump(Parse(text, ParseGoal.Module, model: false))).Split('\n'), static l => l.StartsWith("model-", StringComparison.Ordinal));
    }
}
