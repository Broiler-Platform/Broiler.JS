using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Semantics;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The semantic model's early errors (SJL-15.2 stage 2; the session's decisions D-js-channel and D-js-semantic-rows): the
/// rows of <see cref="SemanticRows"/>, reported only with <see cref="ParseRequest.BuildSemanticModel"/> on, through the
/// declarations walk's one report and one sequence, in decision D11's order. Each row is refused on and accepted off, its
/// nearest admitted variant accepted both ways; the places the rows are reported at; the tie of a <c>syntax</c> row with a
/// <c>semantics</c> row at one place; and the partial walk before a grammar refusal.
/// </summary>
public sealed class SemanticRowsTests
{
    // -----------------------------------------------------------------------------------------------------------------
    // Requests.

    /// <summary>
    /// A request: <c>m</c> the Module goal, <c>e</c> eval code (with <c>im</c> InMethod, <c>dc</c> InDerivedConstructor,
    /// <c>cf</c> InClassFieldInitializer, <c>if</c> InFunction), <c>p</c> or <c>b</c> the dynamic function's parameters or
    /// body (<c>g</c> generator, <c>a</c> async, <c>ag</c> async generator), <c>fs</c> forced strict, <c>all</c> all errors;
    /// the member on unless <paramref name="model"/> is false.
    /// </summary>
    private static ParseRequest Request(string options, string text, bool model)
    {
        var set = new HashSet<string>(options.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        var goal = set.Contains("m") ? ParseGoal.Module : set.Contains("e") ? ParseGoal.Eval
            : set.Contains("p") ? ParseGoal.FormalParameters : set.Contains("b") ? ParseGoal.FunctionBody : ParseGoal.Script;
        var switches = ParseSwitches.For(goal);
        if (set.Contains("fs"))
            switches = switches with { ForcedStrict = true };
        var limits = new ParseLimits { Reporting = set.Contains("all") ? ErrorReporting.AllErrors : ErrorReporting.FirstError };
        var kind = set.Contains("g") ? DynamicFunctionKind.Generator : set.Contains("a") ? DynamicFunctionKind.Async
            : set.Contains("ag") ? DynamicFunctionKind.AsyncGenerator : DynamicFunctionKind.Normal;
        if (goal == ParseGoal.Eval)
        {
            var cf = set.Contains("cf");
            return new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance)
            {
                Switches = switches,
                Limits = limits,
                BuildSemanticModel = model,
                Context = new InheritedContext
                {
                    InFunction = set.Contains("if") || set.Contains("im") || cf,
                    InMethod = set.Contains("im") || set.Contains("dc") || cf,
                    InDerivedConstructor = set.Contains("dc"),
                    InClassFieldInitializer = cf,
                },
            };
        }

        return new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Switches = switches,
            Limits = limits,
            BuildSemanticModel = model,
            FunctionKind = kind,
        };
    }

    private static ParseResult Parse(string options, string text, bool model = true) => SyntaxParser.Parse(Request(options, text, model));

    private static LexicalRefusal Row(string name) =>
        (LexicalRefusal)typeof(SemanticRows).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    private static string Code(string name) => Row(name).Code;

    private static string Describe(ParseResult result) =>
        result.Outcome + " [" + string.Join(", ", result.Diagnostics.Select(static d => d.Code + "@" + d.Span.Offset + "(" + string.Join(",", d.Arguments) + ")")) + "]";

    private static string Root()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !File.Exists(Path.Combine(directory, "Broiler.ECMAScript", "docs", "frontend", "stages.tsv")))
            directory = Path.GetDirectoryName(directory);
        return directory ?? throw new InvalidOperationException("The repository root was not found.");
    }

    private static List<string[]> Table(string name) =>
        File.ReadLines(Path.Combine(Root(), "Broiler.ECMAScript", "docs", "frontend", name)).Skip(1).Select(static l => l.Split('\t')).ToList();

    private static FieldInfo[] Fields() => typeof(SemanticRows).GetFields(BindingFlags.Static | BindingFlags.NonPublic);

    // -----------------------------------------------------------------------------------------------------------------
    // The table.

    [Fact]
    public void The_model_s_rows_are_a_subset_of_the_stage_table_s_semantics_rows_with_the_catalogue_s_codes_and_identifiers()
    {
        var catalogue = Table("early-errors.tsv").ToDictionary(static c => c[0], StringComparer.Ordinal);
        var semantics = Table("stages.tsv").Where(static c => c[2] == "semantics").Select(static c => c[0]).ToHashSet(StringComparer.Ordinal);
        var syntaxCodes = typeof(Rows).GetFields(BindingFlags.Static | BindingFlags.NonPublic).Select(static f => ((LexicalRefusal)f.GetValue(null)!).Code).ToHashSet(StringComparer.Ordinal);
        var fields = Fields();
        Assert.NotEmpty(fields);
        foreach (var field in fields)
        {
            Assert.Contains(field.Name, semantics);
            Assert.NotEqual("EA0006", field.Name);
            var row = (LexicalRefusal)field.GetValue(null)!;
            Assert.Equal(catalogue[field.Name][10], row.Code);
            Assert.Equal(catalogue[field.Name][11], row.MessageId);
            Assert.DoesNotContain(row.Code, syntaxCodes);
        }

        // The codes are the rows' own: no two fields share one.
        Assert.Equal(fields.Length, fields.Select(static f => ((LexicalRefusal)f.GetValue(null)!).Code).Distinct(StringComparer.Ordinal).Count());
    }

    // -----------------------------------------------------------------------------------------------------------------
    // One fixture per row: refused on, accepted off; the nearest admitted variant accepted both ways.

    /// <summary>A row's fixture: the refused source and its request, where and with what it is reported, and the nearest admitted variant.</summary>
    public sealed record Fixture(string Row, string Options, string Refused, string At, int Nth, string Argument, string AcceptedOptions, string Accepted)
    {
        public override string ToString() => Row + " " + Options + " " + Refused;
    }

    private static readonly Fixture[] Fixtures =
    [
        // 2a: a SuperProperty or a SuperCall in a non-arrow function's parameters or body, by shape.
        new("EE0074", "", "function f(a = super.x) {}", "super", 0, "", "", "function f(a = { m() { return super.x; } }.m()) {}"),
        new("EE0075", "", "function f() { super.x; }", "super", 0, "", "", "function f() { return { m() { return super.x; } }.m(); }"),
        new("EE0076", "", "function f(a = super()) {}", "super", 0, "", "", "function f(a = class extends Object { constructor() { super(); } }) {}"),
        new("EE0077", "", "function f() { super(); }", "super", 0, "", "", "function f() { class C extends Object { constructor() { super(); } } }"),
        new("EE0102", "", "function* g(a = super.x) {}", "super", 0, "", "", "function* g(a = { m() { return super.x; } }) {}"),
        new("EE0103", "", "function* g() { super.x; }", "super", 0, "", "", "function* g() { ({ m() { return super.x; } }); }"),
        new("EE0104", "", "function* g(a = super()) {}", "super", 0, "", "", "function* g(a = class extends Object { constructor() { super(); } }) {}"),
        new("EE0105", "", "function* g() { super(); }", "super", 0, "", "", "function* g() { class C extends Object { constructor() { super(); } } }"),
        new("EE0117", "", "async function* g(a = super.x) {}", "super", 0, "", "", "async function* g(a = { m() { return super.x; } }) {}"),
        new("EE0118", "", "async function* g() { super.x; }", "super", 0, "", "", "async function* g() { ({ m() { return super.x; } }); }"),
        new("EE0119", "", "async function* g(a = super()) {}", "super", 0, "", "", "async function* g(a = class extends Object { constructor() { super(); } }) {}"),
        new("EE0120", "", "async function* g() { super(); }", "super", 0, "", "", "async function* g() { class C extends Object { constructor() { super(); } } }"),
        new("EE0150", "", "async function f(a = super.x) {}", "super", 0, "", "", "async function f(a = { m() { return super.x; } }) {}"),
        new("EE0151", "", "async function f() { super.x; }", "super", 0, "", "", "async function f() { ({ m() { return super.x; } }); }"),
        new("EE0152", "", "async function f(a = super()) {}", "super", 0, "", "", "async function f(a = class extends Object { constructor() { super(); } }) {}"),
        new("EE0153", "", "async function f() { super(); }", "super", 0, "", "", "async function f() { class C extends Object { constructor() { super(); } } }"),

        // 2b: HasDirectSuper of methods, by shape and then by home.
        new("EE0093", "", "({ *g() { super(); } });", "super", 0, "", "", "({ *g() { super.x; } });"),
        new("EE0142", "", "({ async m() { super(); } });", "super", 0, "", "", "({ async m() { super.x; } });"),
        new("EE0106", "", "({ async *g() { super(); } });", "super", 0, "", "", "({ async *g() { super.x; } });"),
        new("EE0015", "", "({ m() { super(); } });", "super", 0, "", "", "({ m() { return super.x; } });"),
        new("EE0124", "", "class C { m() { super(); } }", "super", 0, "", "", "class C extends Object { m() { super.x; } }"),
        new("EE0126", "", "class C { static m() { super(); } }", "super", 0, "", "", "class C { static m() { super.x; } }"),
        new("EE0121", "", "class C { constructor() { super(); } }", "super", 0, "", "", "class C extends Object { constructor() { (() => super())(); } }"),

        // 2c: class initialisers.
        new("EE0131", "", "class C extends Object { x = super(); }", "super", 0, "", "", "class C { x = super.x; }"),
        new("EE0139", "", "class C { static { super(); } }", "super", 0, "", "", "class C { static { super.x; } }"),

        // 2d: a program's own code.
        new("EE0162", "", "super.x;", "super", 0, "", "", "({ m() { super.x; } });"),
        new("EE0172", "m", "super.x;", "super", 0, "", "m", "({ m() { super.x; } });"),

        // 2e: eval code, from the caller's context.
        new("EA0003", "e", "super.x;", "super", 0, "", "e im", "super.x;"),
        new("EA0004", "e im", "super();", "super", 0, "", "e dc", "super();"),
        new("EA0005", "e cf", "arguments;", "arguments", 0, "arguments", "e cf", "function f() { arguments; }"),

        // 2g: ContainsArguments.
        new("EE0130", "", "class C { x = arguments; }", "x", 0, "arguments", "", "class C { x = function () { return arguments; }; }"),
        new("EE0138", "", "class C { static { arguments; } }", "static", 0, "arguments", "", "class C { static { (function () { arguments; }); } }"),

        // 2h: expressions.
        new("EE0018", "", "({ __proto__: 1, '__proto__': 2 });", "'__proto__'", 0, "__proto__", "", "({ __proto__: 1, ['__proto__']: 2 });"),
        new("EE0054", "", "for (this in {}) ;", "this", 0, "", "", "for (o.p in x) ;"),
    ];

    public static TheoryData<Fixture> FixtureData()
    {
        var data = new TheoryData<Fixture>();
        foreach (var fixture in Fixtures)
            data.Add(fixture);
        return data;
    }

    // More fixtures of the same rows, from the section's triggers, the dynamic goals and the eval contexts: each refused
    // on, by its row first, and accepted off with no diagnostic.
    public static TheoryData<string, string, string> MoreTriggers() => new()
    {
        { "EE0074", "", "function f(a = () => super.x) {}" },
        { "EE0075", "", "function f() { class C extends super.x {} }" },
        { "EE0075", "", "function f() { ({ [super.x]() {} }); }" },
        { "EE0130", "", "class C { x = () => arguments; }" },
        { "EE0130", "", "class C { x = (a = arguments) => a; }" },
        { "EE0130", "", "class C { x = argu\\u006Dents; }" },
        { "EE0054", "", "for ((a + b) of []) ;" },
        { "EE0054", "", "\"use strict\"; for (arguments in o) ;" },
        { "EE0162", "", "var f = () => super.x;" },
        { "EE0162", "", "class C extends super.x {}" },
        { "EE0162", "", "({ [super.x]() {} });" },
        { "EE0172", "m", "var f = () => super.x;" },
        { "EE0172", "m", "class C extends super.x {}" },
        { "EE0172", "m", "({ [super.x]() {} });" },
        { "EE0074", "p", "a = super.x" },
        { "EE0102", "p g", "a = super.x" },
        { "EE0150", "p a", "a = super.x" },
        { "EE0117", "p ag", "a = super.x" },
        { "EE0077", "b", "super();" },
        { "EE0105", "b g", "super();" },
        { "EE0153", "b a", "super();" },
        { "EE0120", "b ag", "super();" },
        { "EA0003", "e", "super.x;" },
        { "EA0004", "e", "super();" },
        { "EA0004", "e im", "super();" },
        { "EA0005", "e cf", "() => arguments;" },
    };

    // The nearest admitted variants of the dynamic goals and of each eval context: accepted on and off.
    public static TheoryData<string, string> MoreAccepted() => new()
    {
        { "p", "a = { m() { return super.x; } }" },
        { "p g", "a = { m() { return super.x; } }" },
        { "p a", "a = { m() { return super.x; } }" },
        { "p ag", "a = { m() { return super.x; } }" },
        { "b", "class C extends Object { constructor() { super(); } }" },
        { "b g", "class C extends Object { constructor() { super(); } }" },
        { "b a", "class C extends Object { constructor() { super(); } }" },
        { "b ag", "class C extends Object { constructor() { super(); } }" },
        { "e", "arguments;" },
        { "e im", "super.x; arguments;" },
        { "e dc", "super.x; super(); arguments;" },
        { "e cf", "super.x;" },
        { "", "class C { x = function () { return arguments; }; }" },
        { "", "({ __proto__: 1, __proto__ });" },
        { "", "({ __proto__: a, __proto__: b } = o);" },
        { "", "({ __proto__: 1, __proto__() {} });" },
        { "", "for (f() in o) ;" },
        { "", "class C { constructor() {} }" },
        { "", "class C extends Object { constructor(a = super()) { super(); } }" },
        { "", "class C extends Object { static { super.x; } x = super.x; constructor() { super(); } }" },
        { "", "`${a}b${c}`; tag`x${y}`;" },
    };

    [Theory]
    [MemberData(nameof(FixtureData))]
    public void Each_row_is_refused_with_the_member_on_and_accepted_off(Fixture fixture)
    {
        // The nearest admitted variant: accepted on and off.
        foreach (var model in new[] { false, true })
        {
            var accepted = Parse(fixture.AcceptedOptions, fixture.Accepted, model);
            Assert.True(accepted.Outcome == ParseOutcome.Accepted, fixture.Accepted + (model ? " on" : " off") + ": " + Describe(accepted));
        }

        // Off: accepted, with no diagnostic.
        var off = Parse(fixture.Options, fixture.Refused, model: false);
        Assert.True(off.Outcome == ParseOutcome.Accepted && off.Diagnostics.Count == 0, fixture + " off: " + Describe(off));

        // On: refused by the row first, at its place, with its arguments, and no model.
        var on = Parse(fixture.Options, fixture.Refused);
        Assert.True(on.Outcome == ParseOutcome.Refused, fixture + " on: " + Describe(on));
        Assert.Null(on.Model);
        var first = on.Diagnostics[0];
        var row = Row(fixture.Row);
        Assert.Equal((row.Code, row.MessageId), (first.Code, first.MessageId));
        Assert.Equal(DiagnosticPhase.Semantics, first.Phase);
        Assert.Equal(Offset(fixture.Refused, fixture.At, fixture.Nth), first.Span.Offset);
        Assert.Equal(fixture.Argument.Length == 0 ? Array.Empty<string>() : new[] { fixture.Argument }, first.Arguments);

        // All errors: the row exactly once, the construct being written once.
        var all = Parse(fixture.Options + " all", fixture.Refused);
        Assert.True(all.Diagnostics.Count(d => d.Code == row.Code) == 1, fixture + " all errors: " + Describe(all));
    }

    [Theory]
    [MemberData(nameof(MoreTriggers))]
    public void Each_trigger_is_refused_by_its_row_with_the_member_on_and_accepted_off(string row, string options, string text)
    {
        var off = Parse(options, text, model: false);
        Assert.True(off.Outcome == ParseOutcome.Accepted && off.Diagnostics.Count == 0, text + " off: " + Describe(off));
        var on = Parse(options, text);
        Assert.True(on.Outcome == ParseOutcome.Refused && on.Diagnostics[0].Code == Code(row), row + " " + text + " on: " + Describe(on));
        Assert.Null(on.Model);
    }

    [Theory]
    [MemberData(nameof(MoreAccepted))]
    public void Each_admitted_variant_is_accepted_on_and_off(string options, string text)
    {
        var off = Parse(options, text, model: false);
        var on = Parse(options, text);
        Assert.True(off.Outcome == ParseOutcome.Accepted, text + " off: " + Describe(off));
        Assert.True(on.Outcome == ParseOutcome.Accepted, text + " on: " + Describe(on));
        Assert.NotNull(on.Model);
    }

    [Fact]
    public void The_fixtures_name_every_row_but_the_three_no_source_reaches_on_its_own()
    {
        var named = Fixtures.Select(static f => f.Row).ToHashSet(StringComparer.Ordinal);
        named.UnionWith(["EE0021", "EP0017", "EP0018"]);
        Assert.Equal(Fields().Select(static f => f.Name).Order(StringComparer.Ordinal), named.Order(StringComparer.Ordinal));
        Assert.Equal(Fixtures.Length, Fixtures.Select(static f => f.Row).Distinct(StringComparer.Ordinal).Count());
    }

    private static int Offset(string text, string at, int nth)
    {
        var index = -1;
        for (var i = 0; i <= nth; i++)
            index = text.IndexOf(at, index + 1, StringComparison.Ordinal);
        Assert.True(index >= 0, at + " in " + text);
        return index;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // The rows no source reaches on its own.

    [Fact]
    public void ShadowRealm_s_rows_are_refused_first_by_eval_code_s_and_the_Script_s()
    {
        // EP0017 and EP0018 have no goal of their own: under the eval goal, with the caller's context the top level's, EA0003
        // and EA0004 refuse their sources first; under the Script goal, EE0162.
        Assert.Equal(Code("EA0003"), Parse("e", "super.x;").Diagnostics[0].Code);
        Assert.Equal(Code("EA0004"), Parse("e", "super();").Diagnostics[0].Code);
        Assert.Equal(Code("EE0162"), Parse("", "super.x;").Diagnostics[0].Code);
        Assert.Equal(Code("EE0162"), Parse("", "super();").Diagnostics[0].Code);
        foreach (var options in new[] { "e all", "all" })
        {
            foreach (var text in new[] { "super.x;", "super();" })
            {
                var codes = Parse(options, text).Diagnostics.Select(static d => d.Code).ToList();
                Assert.DoesNotContain(Code("EP0017"), codes);
                Assert.DoesNotContain(Code("EP0018"), codes);
            }
        }
    }

    [Fact]
    public void EE0021_s_bound_is_two_to_the_thirty_second_and_a_template_is_accepted()
    {
        Assert.False(DeclarationsWalk.TooManyTemplateStrings((1L << 32) - 1));
        Assert.True(DeclarationsWalk.TooManyTemplateStrings(1L << 32));
        Assert.False(DeclarationsWalk.TooManyTemplateStrings(int.MaxValue));
        Assert.Equal(ParseOutcome.Accepted, Parse("", "`a${b}c${d}e`; f`g${h}`;").Outcome);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // The shared sequence, and the places.

    [Fact]
    public void A_syntax_row_and_a_semantics_row_at_one_place_keep_the_walk_s_order()
    {
        // EE0033 is reported at the target before its children are visited; EA0005 at the same identifier after.
        var result = Parse("e fs cf all", "arguments = 1;");
        Assert.Equal(new[] { (CodeOfSyntax("EE0033"), 0), (Code("EA0005"), 0) }, result.Diagnostics.Select(static d => (d.Code, d.Span.Offset)));

        // Off, EE0033 alone.
        Assert.Equal(new[] { CodeOfSyntax("EE0033") }, Parse("e fs cf all", "arguments = 1;", model: false).Diagnostics.Select(static d => d.Code));
    }

    private static (string Code, int Offset)[] Reported(string options, string text) =>
        Parse(options, text).Diagnostics.Select(static d => (d.Code, d.Span.Offset)).ToArray();

    [Fact]
    public void Each_row_is_placed_where_the_brief_s_placement_says()
    {
        Assert.Equal(Code("EE0074"), Parse("", "function f(a = () => super.x) {}").Diagnostics[0].Code);
        Assert.Equal(Code("EE0075"), Parse("", "function f() { class C extends super.x {} }").Diagnostics[0].Code);

        // One EE0130 at the field, however many references it holds.
        var once = "class C { x = () => arguments + arguments; }";
        Assert.Equal(new[] { (Code("EE0130"), once.IndexOf('x', StringComparison.Ordinal)) }, Reported("all", once));

        // Every enclosing member reports, once each, at the member, the first in source order under first error.
        var nested = "class C { x = class { static { arguments; } }; }";
        Assert.Equal(new[] { (Code("EE0130"), nested.IndexOf("x =", StringComparison.Ordinal)), (Code("EE0138"), nested.IndexOf("static", StringComparison.Ordinal)) }, Reported("all", nested));
        Assert.Equal(new[] { (Code("EE0130"), nested.IndexOf("x =", StringComparison.Ordinal)) }, Reported("", nested));

        // Every __proto__ entry after the first.
        var protos = "({ __proto__: 1, __proto__: 2, __proto__: 3 })";
        Assert.Equal(new[] { (Code("EE0018"), Offset(protos, "__proto__", 1)), (Code("EE0018"), Offset(protos, "__proto__", 2)) }, Reported("all", protos));

        // EG0008 in EE0054's place for a call head in strict code.
        Assert.Equal(new[] { CodeOfSyntax("EG0008") }, Reported("all", "\"use strict\"; for (f() in o);").Select(static r => r.Code));

        // A class's generator method: its shape's row alone.
        Assert.Equal(new[] { Code("EE0093") }, Reported("all", "class C { *g() { super(); } }").Select(static r => r.Code));

        // The special constructor: EE0125 at its name, then its shape's row at the call.
        Assert.Equal(new[] { CodeOfSyntax("EE0125"), Code("EE0093") }, Reported("all", "class C { *constructor() { super(); } }").Select(static r => r.Code));

        // A second constructor: EE0122 alone, its call admitted.
        Assert.Equal(new[] { CodeOfSyntax("EE0122") }, Reported("all", "class C { constructor() {} constructor() { super(); } }").Select(static r => r.Code));

        // Each SuperCall reports.
        var twice = "class C { m() { super(); super(); } }";
        Assert.Equal(new[] { (Code("EE0124"), Offset(twice, "super", 0)), (Code("EE0124"), Offset(twice, "super", 1)) }, Reported("all", twice));
    }

    private static string CodeOfSyntax(string row) => ((LexicalRefusal)typeof(Rows).GetField(row, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Code;

    // -----------------------------------------------------------------------------------------------------------------
    // The partial walk before a grammar refusal.

    [Theory]
    [InlineData("", "super.x; (", "EE0162")]
    [InlineData("b", "super(); (", "EE0077")]
    public void The_partial_walk_reports_the_rows_before_a_grammar_refusal_with_the_member_on(string options, string text, string row)
    {
        var offAll = Parse(options + " all", text, model: false);
        var onAll = Parse(options + " all", text);
        Assert.Single(offAll.Diagnostics);
        var grammar = offAll.Diagnostics[0];
        Assert.Equal(new[] { Code(row), grammar.Code }, onAll.Diagnostics.Select(static d => d.Code));
        Assert.Equal(grammar.Span, onAll.Diagnostics[1].Span);
        Assert.Null(onAll.Model);

        Assert.Equal(new[] { grammar.Code }, Parse(options, text, model: false).Diagnostics.Select(static d => d.Code));
        Assert.Equal(new[] { Code(row) }, Parse(options, text).Diagnostics.Select(static d => d.Code));
    }
}
