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

    private static ParseRequest RowSourceRequest(string text, bool model, bool allErrors = false) =>
        new(new SourceText(text), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance)
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
            return (a, b, o, s, ro, rs);
        });

        // Not vacuous: most of the corpus is accepted, some of it refused both ways, every row source refused on alone, and
        // every source of the second kind refused first by a row of the model's. The population's own counts are reported
        // apart from the row sources'. (Revised 2026-11-25, at SJL-15.2 stage 2's review; it read "and some refused on alone
        // (the row sources are, each of them)", and it asserted only that the count refused on alone, population and row
        // sources together, was at least the number of row sources.)
        Assert.True(accepted > items.Count / 2, accepted.ToString(CultureInfo.InvariantCulture) + " of " + items.Count.ToString(CultureInfo.InvariantCulture) + " accepted");
        Assert.True(bothRefused > 0);
        Assert.Equal(RowSources.Length, onlyOnRowSources);
        Assert.Equal(BothRefusedRowSources.Length, semanticFirstRowSources);
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
            return (count, more);
        });
        Assert.True(seen > 0);
        Assert.True(extra >= RowSources.Length);
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
    private static (int References, int Bindings, int Dynamic, int Functions) Complete(string name, ParseResult on, ParseResult off)
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

        // What a scope binds, the test's own reading.
        Declaration? Binds(Scope scope, string n)
        {
            var first = scope.Declarations.FirstOrDefault(d => d.Name == n);
            if (first is null && n == "arguments" && scope.Kind is ScopeKind.Parameters or ScopeKind.StaticBlock && factsNodes.Contains(scope.Node))
                first = model.EnvironmentOf(scope.Node).Arguments;
            return first;
        }

        var sloppyEval = unit.Kind == FunctionKind.EvalCode && !unit.IsStrict;
        bool EvalVar(Scope scope, Declaration declaration) => sloppyEval && ReferenceEquals(scope, model.Scopes[0]) && !declaration.IsLexical;

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
            }

            if (answer.Kind == ResolutionKind.Dynamic)
            {
                Assert.True(expected.Count > 0 && expected.SequenceEqual(answer.DynamicScopes, ReferenceEqualityComparer.Instance), at + ": the dynamic scopes");
                Assert.True((answer.Reason == DynamicReason.With) == (answer.DynamicScopes[0].Kind == ScopeKind.With), at + ": the reason");
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
                if (EvalVar(chain[i], declaration))
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
}
