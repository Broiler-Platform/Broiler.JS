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
/// facts, on the fixtures the stage names and over the determinism tests' population of the parse corpus.
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
    ];

    // Sources both paths refuse, where the on path's first diagnostic is a row of the model's at an earlier place than the
    // off path's first, so that the identity test's branch for it runs (SJL-15.2 stage 2's review).
    private static readonly string[] BothRefusedRowSources = ["super.x; let a; let a;"];

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
        };
        const string script = "var v; let l; const c = 1; { using u = null; } async function f(p) { await using w = null; } class K {} try {} catch (e) {} x = function n() {};";
        var seen = new HashSet<BindingKind>();
        foreach (var scope in Model(script).Model.Scopes)
        {
            foreach (var declaration in scope.Declarations)
            {
                Assert.Equal(expected[declaration.Kind], (declaration.IsMutable, declaration.IsLexical));
                Assert.False(declaration.IsBlockFunction);
                Assert.False(declaration.IsIfClauseFunction);
                seen.Add(declaration.Kind);
            }
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
}
