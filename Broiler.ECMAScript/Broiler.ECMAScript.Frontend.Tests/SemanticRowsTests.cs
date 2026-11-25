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
/// The semantic model's early errors (SJL-15.2 from stage 2; the session's decisions D-js-channel and D-js-semantic-rows,
/// from stage 3 D-js-jump-rows and D-js-jump-targets, and from stage 4 D-js-private-rows and D-js-private-environments):
/// the rows of <see cref="SemanticRows"/>, stage 2's of the
/// function context and of expressions, stage 3's of jump targets and stage 4's of private names, reported only with
/// <see cref="ParseRequest.BuildSemanticModel"/> on, through the declarations walk's one report and one sequence, in
/// decision D11's order. Each row is refused on and accepted off, its nearest admitted variant accepted both ways; the
/// places the rows are reported at; the boundaries of jumps and of private names; the ties of a <c>syntax</c> row with a
/// <c>semantics</c> row at one place; eval code's visible private names, read only with the member on; and the partial
/// walk before a grammar refusal. <i>(Revised 2026-11-25, at SJL-15.2 stage 4; it read "and from stage 3 D-js-jump-rows
/// and D-js-jump-targets): the rows of <see cref="SemanticRows"/>, stage 2's of the function context and of expressions
/// and stage 3's of jump targets, reported only", "the boundaries of jumps; the tie of a <c>syntax</c> row with a
/// <c>semantics</c> row at one place; and the partial walk".)</i> <i>(Revised 2026-11-25, at SJL-15.2 stage 3; it read "(SJL-15.2
/// stage 2; the session's decisions D-js-channel and D-js-semantic-rows): the rows of <see cref="SemanticRows"/>, reported
/// only" and "the places the rows are reported at; the tie".)</i>
/// </summary>
public sealed class SemanticRowsTests
{
    // -----------------------------------------------------------------------------------------------------------------
    // Requests.

    /// <summary>
    /// A request: <c>m</c> the Module goal, <c>e</c> eval code, <c>p</c> or <c>b</c> the dynamic function's parameters or
    /// body (<c>g</c> generator, <c>a</c> async, <c>ag</c> async generator), <c>fs</c> forced strict, <c>all</c> all errors;
    /// the member on unless <paramref name="model"/> is false. Eval code's context is one PerformEval makes (step 10), each
    /// option with the flags its caller implies: <c>if</c> InFunction (a plain function); <c>im</c> InFunction and InMethod (a
    /// method); <c>dc</c> InFunction, InMethod and InDerivedConstructor (a derived constructor); <c>cf</c> InFunction,
    /// InMethod and InClassFieldInitializer (a field initialiser, whose function is a method); none of them TopLevel. The one
    /// exception is <c>cfo</c>, InClassFieldInitializer alone, a context PerformEval does not make, for the rows that read
    /// that flag alone. <i>(Revised 2026-11-25, at SJL-15.2 stage 2's review; it read "(with <c>im</c> InMethod, <c>dc</c>
    /// InDerivedConstructor, <c>cf</c> InClassFieldInitializer, <c>if</c> InFunction)", and <c>dc</c> left InFunction
    /// unset.)</i> With <c>v</c>, eval code's context also gives the visible private names: the option tokens that begin
    /// with <c>#</c>, read in their order from the options as written, not from the set, or an empty list where there are
    /// none; without <c>v</c> the list is null. <i>(Revised 2026-11-25, at SJL-15.2 stage 4, which added <c>v</c>; the
    /// eval context set no visible names.)</i>
    /// </summary>
    private static ParseRequest Request(string options, string text, bool model)
    {
        var tokens = options.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var set = new HashSet<string>(tokens, StringComparer.Ordinal);
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
            var dc = set.Contains("dc");
            return new ParseRequest(new SourceText(text), goal, UnicodeDataHome.Tables, NoValidator.Instance)
            {
                Switches = switches,
                Limits = limits,
                BuildSemanticModel = model,
                Context = new InheritedContext
                {
                    InFunction = set.Contains("if") || set.Contains("im") || dc || cf,
                    InMethod = set.Contains("im") || dc || cf,
                    InDerivedConstructor = dc,
                    InClassFieldInitializer = cf || set.Contains("cfo"),
                    VisiblePrivateNames = set.Contains("v") ? tokens.Where(static o => o.StartsWith('#')).ToArray() : null,
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

        // Stage 3: a jump with no target, at the jump (the refused sources and their twins the vectors' where one exists).
        new("EE0058", "", "continue;", "continue", 0, "", "", "var n = 0; for (var i = 0; i < 3; i++) { if (i === 1) continue; n += i; } n"),
        new("EE0059", "", "break;", "break", 0, "", "", "var r = 0; switch (1) { case 1: r = 5; break; case 2: r = 6; } r"),
        new("EE0081", "", "L: { (function () { break L; }); }", "break", 0, "L", "", "function f() { L: { break L; } return 1; } f()"),
        new("EE0082", "b", "continue L;", "continue", 0, "L", "b", "var n = 0; L: for (var i = 0; i < 3; i++) { n++; continue L; } return n;"),
        new("EE0136", "", "L: { class C { static { break L; } } }", "break", 0, "L", "", "var r = 0; class C { static { L: { r = 1; break L; } } } r"),
        new("EE0137", "", "L: do { class C { static { continue L; } } } while (0);", "continue", 0, "L", "", "var n = 0; class C { static { L: for (var i = 0; i < 2; i++) { n++; continue L; } } } n"),
        new("EE0165", "", "break L;", "break", 0, "L", "", "var r = 0; L: { r = 2; break L; r = 3; } r"),
        new("EE0166", "", "continue L;", "continue", 0, "L", "", "var n = 0; L: for (var i = 0; i < 3; i++) { if (i) continue L; n++; } n"),
        new("EE0175", "m", "break L;", "break", 0, "L", "m", "L: { break L; }"),
        new("EE0176", "m", "continue L;", "continue", 0, "L", "m", "L: for (;;) { continue L; }"),

        // Stage 4: a private name no enclosing class declares, at the member, with the name and its # as the argument.
        new("EE0167", "", "this.#x;", "this", 0, "#x", "", "class C { #x; m() { this.#x; } }"),
        new("EE0177", "m", "this.#x;", "this", 0, "#x", "m", "class C { #x; m() { this.#x; } }"),
        new("EA0009", "e v", "this.#x;", "this", 0, "#x", "e v #x", "this.#x;"),
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
        { "EA0005", "e cfo", "arguments;" },

        // A function expression that is an object literal's property value is no method (Broiler.JS admits it).
        { "EE0075", "", "var o = { run: function () { super.x = 1; } };" },

        // Stage 3: the jumps' triggers, with the vectors' refused sources. (Added 2026-11-25, at SJL-15.2 stage 3.)
        { "EE0058", "", "while (0) { (function () { continue; }); }" },
        { "EE0058", "", "for (;;) { class C { static { continue; } } }" },
        { "EE0058", "", "while (1) { (() => { continue; }); }" },
        { "EE0058", "", "var r = 'ran'; try { (function () { continue; })(); } catch (e) { r = e.name; } r" },
        { "EE0059", "", "do { (function () { break; }); } while (0);" },
        { "EE0059", "", "switch (0) { case 0: class C { static { break; } } }" },
        { "EE0081", "b", "break L;" },
        { "EE0082", "", "L: do { (function () { continue L; }); } while (0);" },
        { "EE0165", "", "L: { } break L;" },
        { "EE0166", "", "L: { continue L; }" },
        { "EE0166", "", "L: { while (c) continue L; }" },
        { "EE0166", "", "while (1) { L: switch (1) { default: continue L; } }" },
        { "EE0166", "", "A: { B: for (;;) continue A; }" },
        { "EE0166", "e", "continue L;" },
        { "EE0059", "e", "break;" },

        // Stage 4: private names no enclosing class declares, with the vectors' refused sources. (Added 2026-11-25, at
        // SJL-15.2 stage 4.)
        { "EE0167", "", "class C { m() { return this.#y; } }" },
        { "EE0167", "", "class C { #x; static m(o) { return #y in o; } }" },
        { "EE0167", "", "var r = 'ran'; try { (function () { class C { m() { return this.#y; } } })(); } catch (e) { r = e.name; } r" },
        { "EE0167", "", "function f() { class C { m() { return this.#y; } } }" },
        { "EE0167", "", "class C { m(o) { return o?.#y; } }" },
        { "EE0167", "", "class C { m(o) { return o?.a.#y; } }" },
        { "EE0167", "", "class C { m() { return this.#f(); } }" },
        { "EE0167", "", "class C { m() { return this.#t`x`; } }" },
        { "EE0167", "", "class C { m() { return new this.#C(); } }" },
        { "EE0167", "", "class C { m() { this.#y = 1; } }" },
        { "EE0167", "", "class C { m() { this.#y++; } }" },
        { "EE0167", "", "class C { m() { this.#y += 1; } }" },
        { "EE0167", "", "class C { m() { [this.#y] = [1]; } }" },
        { "EE0167", "", "class C { m() { ({ a: this.#y } = {}); } }" },
        { "EE0167", "", "class C { m() { for (this.#y of []); } }" },
        { "EE0167", "", "class C { m() { for (this.#y in {}); } }" },
        { "EE0167", "", "class C extends (o => o.#x, Object) { #x; }" },
        { "EE0167", "", "class O { m() { class I extends (o => o.#y, Object) { #y; } } }" },
        { "EE0167", "", "class C { m() { class D { #x; } return this.#x; } }" },
        { "EE0167", "", "this.#\u0079;" },
        { "EE0167", "", "switch (0) { case 0: this.#x; }" },
        { "EE0167", "", "#y in {};" },
        { "EE0167", "", "class C { #x; m() { return class { [this.#y] = 1; }; } }" },
        { "EE0177", "m", "class C { m() { return this.#y; } }" },
        { "EE0177", "m", "class C { #x; static m(o) { return #y in o; } }" },
        { "EE0177", "m", "export default class { m() { return this.#y; } }" },
        { "EA0009", "e v", "#y in this;" },
        { "EA0009", "e im fs v #x", "this.#y;" },
        { "EA0009", "e im fs v", "r += 'a'; this.#y" },
        { "EA0009", "e v", "r += 'a'; this.#x" },
        { "EA0009", "e v #x", "class D { m() { return this.#y; } }" },
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

        // Stage 3: the jumps' admitted variants, LB1 and hand-off H20 among them, read by the specification.
        { "", "L: break L;" },
        { "", "L: { break L; }" },
        { "", "L: while (1) { M: { break L; } }" },
        { "", "A: B: for (;;) continue A;" },
        { "", "A: B: C: do { continue B; } while (0)" },
        { "", "o: for (;;) { for (;;) { continue o; } }" },
        { "", "A: { B: for (;;) continue B; }" },
        { "", "A: while (1) { B: { continue A; } }" },
        { "", "L1: L2: while (c) continue L1;" },
        { "", "while (1) { L: { break; } }" },
        { "", "while (1) { switch (0) { case 0: continue; } }" },
        { "", "var n = 0; A: B: for (var i = 0; i < 2; i++) { n++; continue A; } n" },
        { "", "function f() { var n = 0; A: B: for (var i = 0; i < 3; i++) { n++; continue A; } return n; } f()" },
        { "", "var n = 0; A: B: C: do { n++; if (n < 3) continue B; } while (n < 5); n" },
        { "", "var s = ''; o: for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { if (j) continue o; s += i + '' + j; } } s" },
        { "", "var n = 0; A: { B: for (var i = 0; i < 2; i++) { n++; continue B; } } n" },
        { "m", "L: for (;;) { if (x) break L; continue L; } switch (0) { default: break; }" },
        { "b", "L: for (;;) { continue L; } M: { break M; } while (0) break;" },
        { "e", "L: for (;;) { continue L; }" },

        // Stage 4: private names a class declares, the vectors' accepted sources among them; placement playing no part.
        // (Added 2026-11-25, at SJL-15.2 stage 4.)
        { "", "class C { #x = 1; m() { return this.#x; } } new C().m()" },
        { "", "class C { #x = 2; m() { class D { n(o) { return o.#x; } } return new D().n(this); } } new C().m()" },
        { "", "class C { m() { return this.#x; } #x; }" },
        { "", "class C { #x; m() { class D { #x; n(o) { return o.#x; } } } }" },
        { "", "class C { static #s; m() { return C.#s; } }" },
        { "", "class C { static #s; m() { return this.#s; } }" },
        { "", "class C { #x; static m(o) { return o.#x; } }" },
        { "", "class C { #x; static m(o) { return #x in o; } }" },
        { "", "class C { get #a() { return 1; } set #a(v) {} m() { return this.#a; } }" },
        { "", "class C { static get #a() { return 1; } static set #a(v) {} static m() { return C.#a; } }" },
        { "", "class C { #m() {} *#g() {} async #f() {} async *#h() {} n() { this.#m(); this.#g(); this.#f(); this.#h(); } }" },
        { "", "class C { #x; get g() { return this.#x; } set g(v) { this.#x = v; } }" },
        { "", "class C { #x = 1; y = this.#x; }" },
        { "", "class C { static #s; static { C.#s; } }" },
        { "", "class C { #x; m(a = this.#x) {} }" },
        { "", "class C { #x; m({ [this.#x]: y }) {} }" },
        { "", "class C { #x; [o => o.#x] = 1; }" },
        { "", "class C { #x; y = () => this.#x; }" },
        { "", "class C { static #s; static { (function () { C.#s; }); } }" },
        { "", "class C { #x; m() { return { f(o) { return o.#x; } }; } }" },
        { "", "class O { #x; m() { class I extends (o => o.#x, Object) {} } }" },
        { "", "class C { #x; m(o) { return o?.#x; } }" },
        { "", "class C { #x; m() { this.#x = 1; this.#x++; this.#x += 1; [this.#x] = [1]; ({ a: this.#x } = {}); for (this.#x of []); delete this.y; } }" },
        { "", "class C { #x; m() { switch (0) { case 0: this.#x; } } }" },
        { "m", "class C { #x; m() { return this.#x; } }" },
        { "e", "this.#x;" },
        { "e", "#x in this; class D { m() { return this.#y; } }" },
        { "e v #x", "this.#x;" },
        { "e v", "class D { #y; m() { return this.#y; } }" },
        { "e v #x", "class D { m(o) { return o.#x; } }" },
        { "e im fs v #x", "this.#x" },
        { "e im fs v #m", "this.#m()" },
        { "e im fs v #s", "C.#s" },
        { "b", "this.#x;" },
        { "p", "a = this.#x" },
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

        // The same with InClassFieldInitializer alone, the context as the brief names it.
        Assert.Equal(new[] { (CodeOfSyntax("EE0033"), 0), (Code("EA0005"), 0) }, Parse("e fs cfo all", "arguments = 1;").Diagnostics.Select(static d => (d.Code, d.Span.Offset)));
        Assert.Equal(new[] { CodeOfSyntax("EE0033") }, Parse("e fs cfo all", "arguments = 1;", model: false).Diagnostics.Select(static d => d.Code));
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

    /// <summary>
    /// The jumps' places and boundaries (SJL-15.2 stage 3): each row at its jump, with the label as the undefined-label rows'
    /// argument; every function, arrows included, and every static block a boundary; the owner's row by the label set's
    /// owner, a static block in a dynamic function's parameters included, and eval code a Script's owner whatever its
    /// caller's context, since PerformEval parses a Script and eval code inherits no label; one row per construct; and the
    /// whole chain of enclosing statements searched. <i>(Added 2026-11-25, at SJL-15.2 stage 3.)</i>
    /// </summary>
    [Fact]
    public void Each_jump_row_is_placed_at_its_jump_and_every_function_and_static_block_is_a_boundary()
    {
        (string Code, int Offset)[] At(string row, string text, string at, int nth = 0) => [(Code(row), Offset(text, at, nth))];
        foreach (var (row, options, text, at) in new[]
        {
            ("EE0059", "", "while (0) { (() => { break; }); }", "break"),
            ("EE0058", "", "while (1) { (() => { continue; }); }", "continue"),
            ("EE0081", "", "L: { (() => { break L; }); }", "break"),
            ("EE0082", "", "L: while (1) { class C { x = function () { continue L; }; } }", "continue"),
            ("EE0136", "", "L: { class C { static { break L; } } }", "break"),
            ("EE0165", "e", "break L;", "break"),
            ("EE0165", "e if", "break L;", "break"),
            ("EE0166", "e cf", "continue L;", "continue"),
            ("EE0058", "e im", "continue;", "continue"),
            ("EE0081", "b", "break L;", "break"),
            ("EE0059", "p", "a = () => { break; }", "break"),
            ("EE0081", "p", "a = () => { break L; }", "break"),
            ("EE0136", "p", "a = class { static { break L; } }", "break"),
            ("EE0175", "m", "break L;", "break"),
            ("EE0176", "m", "continue L;", "continue"),
            ("EE0059", "m", "break;", "break"),
        })
        {
            Assert.Equal(At(row, text, at), Reported(options + " all", text));
            var first = Parse(options, text).Diagnostics[0];
            var expected = Row(row);
            Assert.Equal((expected.Code, expected.MessageId), (first.Code, first.MessageId));
            Assert.Equal(row is "EE0058" or "EE0059" ? Array.Empty<string>() : new[] { "L" }, first.Arguments);
        }

        // One row per construct: a labelled continue that no iteration statement encloses reports its owner's row alone.
        foreach (var text in new[] { "continue L;", "L: { continue L; }", "while (1) { L: { continue L; } }" })
            Assert.Equal(At("EE0166", text, "continue"), Reported("all", text));

        // The whole chain is searched: the duplicate label's syntax row alone, no jump row.
        foreach (var text in new[] { "A: A: while (1) continue A;", "A: while (1) { A: { continue A; } }" })
            Assert.Equal(new[] { CodeOfSyntax("EE0164") }, Reported("all", text).Select(static r => r.Code));

        // In source order under all errors; the first under first error.
        var both = "break; continue;";
        Assert.Equal(new[] { (Code("EE0059"), 0), (Code("EE0058"), Offset(both, "continue", 0)) }, Reported("all", both));
        Assert.Equal(At("EE0059", both, "break"), Reported("", both));
    }

    /// <summary>
    /// A jump's row precedes its label's name row, which is keyed at the label, later than the jump (decision D11): with
    /// all errors both on and the name row alone off; with first error the jump's row on. <i>(Added 2026-11-25, at SJL-15.2
    /// stage 3.)</i>
    /// </summary>
    [Fact]
    public void A_jump_s_row_comes_before_its_label_s_name_row()
    {
        const string text = "\"use strict\"; break yield;";
        var off = Parse("all", text, model: false);
        Assert.Single(off.Diagnostics);
        var name = off.Diagnostics[0];
        Assert.Equal(Offset(text, "yield", 0), name.Span.Offset);
        Assert.Equal(new[] { (Code("EE0165"), Offset(text, "break", 0)), (name.Code, name.Span.Offset) }, Reported("all", text));
        Assert.Equal(new[] { Code("EE0165") }, Reported("", text).Select(static r => r.Code));
        Assert.Equal(new[] { name.Code }, Parse("", text, model: false).Diagnostics.Select(static d => d.Code));
    }

    /// <summary>
    /// The one <c>syntax</c> place a jump's row can share: a scope's declared-name conflicts are keyed at the scope's close,
    /// which a jump written right after it starts at; D11's sub-key, the second declaration's place, which comes before the
    /// jump, puts the conflict first, under all errors and under first error alike. <i>(Added 2026-11-25, at SJL-15.2
    /// stage 3.)</i>
    /// </summary>
    [Theory]
    [InlineData("{ let a; let a; }break;", "EE0059")]
    [InlineData("for (let a;;) { var a; }break;", "EE0059")]
    [InlineData("{ { let a; let a; }continue; }", "EE0058")]
    public void A_scope_s_conflict_at_its_close_comes_before_a_jump_row_at_the_same_place(string text, string row)
    {
        var off = Parse("all", text, model: false);
        Assert.Single(off.Diagnostics);
        var conflict = off.Diagnostics[0];
        var on = Parse("all", text).Diagnostics;
        Assert.Equal(new[] { conflict.Code, Code(row) }, on.Select(static d => d.Code));
        Assert.True(conflict.Span.Offset < on[1].Span.Offset);
        Assert.Equal(new[] { conflict.Code }, Parse("", text).Diagnostics.Select(static d => d.Code));
    }

    /// <summary>
    /// The private names' places and boundaries (SJL-15.2 stage 4): each row at its member or <c>in</c> expression, the
    /// member's start and not its name, with the name and its <c>#</c> as the argument; a class's heritage reading the names
    /// around the class, its computed keys, initialisers, static blocks, parameters and nested functions its own; a
    /// <c>case</c> clause's reference refused; <c>#constructor</c> refused by EE0132 alone; one row per reference, a chain's
    /// outer reference first at their one place; a private <c>delete</c>'s EE0030 beside the row; the Module's row, and
    /// eval code's against the names its caller sees, before a PerformEval row at the same place, and before a ScriptBody
    /// conflict placed later under first error (the known difference from EvalDeclarationInstantiation's order).
    /// <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    [Fact]
    public void Each_private_name_row_is_placed_at_its_member_or_in_expression()
    {
        var member = "class C { m(o) { return o.#y; } }";
        Assert.Equal(new[] { (Code("EE0167"), Offset(member, "o.#y", 0)) }, Reported("all", member));
        Assert.Equal(new[] { "#y" }, Parse("", member).Diagnostics[0].Arguments);
        var privateIn = "class C { m(o) { return #y in o; } }";
        Assert.Equal(new[] { (Code("EE0167"), Offset(privateIn, "#y", 0)) }, Reported("all", privateIn));
        var heritage = "class C extends (o => o.#x, Object) { #x; }";
        Assert.Equal(new[] { (Code("EE0167"), Offset(heritage, "o.#x", 0)) }, Reported("all", heritage));
        foreach (var text in new[]
        {
            "class C { #x; [o => o.#x] = 1; }", "class C { #x; y = () => this.#x; }", "class C { static #s; static { (function () { C.#s; }); } }",
            "class C { #x; m(a = this.#x) {} }",
        })
        {
            var on = Parse("all", text);
            Assert.True(on.Outcome == ParseOutcome.Accepted && on.Diagnostics.Count == 0, text + ": " + Describe(on));
        }

        var inCase = "switch (0) { case 0: this.#x; }";
        Assert.Equal(new[] { (Code("EE0167"), Offset(inCase, "this", 0)) }, Reported("all", inCase));

        // #constructor is in the class's environment: EE0132 alone refuses its declaration, and its use is no undeclared name.
        var constructor = "class C { #constructor; m() { this.#constructor; } }";
        Assert.Equal(new[] { CodeOfSyntax("EE0132") }, Reported("all", constructor).Select(static r => r.Code));
        Assert.Equal(new[] { CodeOfSyntax("EE0132") }, Parse("all", constructor, model: false).Diagnostics.Select(static d => d.Code));

        // One row per reference, in source order; the first under first error.
        var two = "this.#x; this.#y;";
        Assert.Equal(new[] { (Code("EE0167"), 0), (Code("EE0167"), Offset(two, "this", 1)) }, Reported("all", two));
        Assert.Equal(new[] { (Code("EE0167"), 0) }, Reported("", two));
        Assert.Equal(new[] { "#x", "#y" }, Parse("all", two).Diagnostics.Select(static d => d.Arguments.Single()));

        // A chain: both rows at the outer member's start, the outer reference (#b) first, in the walk's order.
        var chain = Parse("all", "this.#a.#b;").Diagnostics;
        Assert.Equal(new[] { (Code("EE0167"), 0, "#b"), (Code("EE0167"), 0, "#a") }, chain.Select(static d => (d.Code, d.Span.Offset, d.Arguments.Single())));

        // A private delete: EE0030 at the unary on both paths, the row at the member on the on path.
        var delete = "delete this.#x;";
        Assert.Equal(new[] { (CodeOfSyntax("EE0030"), 0), (Code("EE0167"), Offset(delete, "this", 0)) }, Reported("all", delete));
        Assert.Equal(new[] { CodeOfSyntax("EE0030") }, Parse("all", delete, model: false).Diagnostics.Select(static d => d.Code));

        // The Module's row; eval code's under a list, before EA0005 at the same place.
        Assert.Equal(new[] { (Code("EE0177"), 0) }, Reported("m all", "this.#x;"));
        Assert.Equal(new[] { (Code("EA0009"), 0), (Code("EA0005"), 0) }, Reported("e cf v all", "arguments.#y"));
        Assert.Equal(new[] { (Code("EA0009"), 0) }, Reported("e cf v", "arguments.#y"));

        // Eval code: EA0009 keyed at its member comes before the ScriptBody's conflict keyed at the program's close, under
        // first error too, where EvalDeclarationInstantiation would raise ParseText's early error first.
        var conflict = Parse("e all", "this.#y; let a; let a;", model: false).Diagnostics.Single();
        Assert.Equal(new[] { Code("EA0009"), conflict.Code }, Reported("e v all", "this.#y; let a; let a;").Select(static r => r.Code));
        Assert.Equal(new[] { Code("EA0009") }, Reported("e v", "this.#y; let a; let a;").Select(static r => r.Code));

        // The dynamic goals and eval code with no list check nothing.
        foreach (var (options, text) in new[] { ("b", "this.#x;"), ("p", "a = this.#x"), ("e", "this.#x;"), ("e im", "#y in this;") })
        {
            var on = Parse(options + " all", text);
            Assert.True(on.Outcome == ParseOutcome.Accepted && on.Diagnostics.Count == 0, options + " " + text + ": " + Describe(on));
        }
    }

    /// <summary>
    /// With the member off nothing reads eval code's visible private names (SJL-15.2 stage 4): Broiler.JS's bridge sends
    /// them with the member off, so the same eval code with no list, an empty one and one holding <c>#x</c> gives the same
    /// outcome, the same diagnostics and the same dump, under all errors, in each caller's context. Part B's identity runs
    /// send no list and cannot show it. <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    [Fact]
    public void With_the_member_off_eval_code_s_visible_private_names_are_read_nowhere()
    {
        var sources = new[]
        {
            "this.#x;", "this.#y;", "#y in this;", "this.#a.#b;", "class D { #y; m() { return this.#y; } }", "class D { m(o) { return o.#x; } }",
            "arguments.#y", "this.#y; let a; let a;", "this.#y; super.x", "delete this.#x;", "r += 'a'; this.#y", "r += 'a'; this.#x",
            "this.#x", "this.#m()", "C.#s", "this.#x; (",
        };
        var compared = 0;
        foreach (var source in sources)
        {
            foreach (var caller in new[] { "e", "e im fs", "e cf", "e if" })
            {
                var results = new[] { caller + " all", caller + " all v", caller + " all v #x" }.Select(o => Parse(o, source, model: false)).ToArray();
                var dump = SyntaxDump.Dump(results[0]);
                foreach (var result in results)
                {
                    Assert.True(result.Outcome == results[0].Outcome && Describe(result) == Describe(results[0]), caller + " " + source + ": " + Describe(result));
                    Assert.True(SyntaxDump.Dump(result).AsSpan().SequenceEqual(dump), caller + " " + source + ": the dump differs");
                    Assert.Null(result.Model);
                }

                compared++;
            }
        }

        Assert.Equal(sources.Length * 4, compared);
    }

    /// <summary>
    /// A private name's row against a <c>syntax</c> row at the same place (SJL-15.2 stage 4; decision D11): a name row at
    /// the member's start, keyed after the member's row, which is reported before the member's children are visited; and a
    /// scope's conflict at its close, which a member written right after it starts at, before the row by D11's sub-key.
    /// <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    [Fact]
    public void A_private_name_s_row_against_a_syntax_row_at_the_same_place()
    {
        const string text = "class C { m() { return implements.#y; } }";
        var off = Parse("all", text, model: false);
        Assert.Single(off.Diagnostics);
        var name = off.Diagnostics[0];
        Assert.Equal(Offset(text, "implements", 0), name.Span.Offset);
        Assert.Equal(new[] { (Code("EE0167"), name.Span.Offset), (name.Code, name.Span.Offset) }, Reported("all", text));
        Assert.Equal(new[] { Code("EE0167") }, Reported("", text).Select(static r => r.Code));
        Assert.Equal(new[] { name.Code }, Parse("", text, model: false).Diagnostics.Select(static d => d.Code));

        const string close = "{ let a; let a; }this.#x;";
        var conflict = Parse("all", close, model: false).Diagnostics.Single();
        var on = Parse("all", close).Diagnostics;
        Assert.Equal(new[] { conflict.Code, Code("EE0167") }, on.Select(static d => d.Code));
        Assert.True(on[0].Span.Offset < on[1].Span.Offset);
        Assert.Equal(new[] { conflict.Code }, Parse("", close).Diagnostics.Select(static d => d.Code));
        Assert.Equal(new[] { conflict.Code }, Parse("", close, model: false).Diagnostics.Select(static d => d.Code));
    }

    private static string CodeOfSyntax(string row) => ((LexicalRefusal)typeof(Rows).GetField(row, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Code;

    // -----------------------------------------------------------------------------------------------------------------
    // The partial walk before a grammar refusal.

    /// <summary>
    /// Before a grammar refusal the partial walk reports the rows, in order, then the grammar's refusal; with the member off
    /// the grammar's refusal alone. The later inputs reach the rows' own walk state, the methods' homes, the field and
    /// static-block anchors, the object literal's <c>__proto__</c> entries and the for-in head, which the partial walk must
    /// read with no model. <i>(Revised 2026-11-25, at SJL-15.2 stage 2's review, which added the inputs with more than one
    /// row; it took one row per input.)</i>
    /// </summary>
    [Theory]
    [InlineData("", "super.x; (", "EE0162")]
    [InlineData("b", "super(); (", "EE0077")]
    [InlineData("", "class C { m() { super(); } x = arguments; static { arguments; } } (", "EE0124 EE0130 EE0138")]
    [InlineData("", "({ __proto__: 1, __proto__: 2, m() { super(); } }); (", "EE0018 EE0015")]
    [InlineData("", "for (this in {}) ; class E { x = () => class { static { arguments; } }; } (", "EE0054 EE0130 EE0138")]
    [InlineData("", "function f() { super(); } var g = () => super.x; class D extends Object { constructor() { super(); } } (", "EE0077 EE0162")]
    [InlineData("m", "super.x; (", "EE0172")]
    [InlineData("e cf", "arguments; (", "EA0005")]
    [InlineData("", "break; (", "EE0059")]
    [InlineData("b", "break L; (", "EE0081")]
    [InlineData("m", "break L; (", "EE0175")]
    [InlineData("", "L: while (1) { if (x) break L; continue L; } switch (0) { default: break; } M: { break M; } break; (", "EE0059")]
    [InlineData("", "this.#x; (", "EE0167")]
    [InlineData("m", "this.#x; (", "EE0177")]
    [InlineData("e v", "this.#x; (", "EA0009")]
    [InlineData("", "class C { #x; m() { this.#x; } } this.#y; (", "EE0167")]
    public void The_partial_walk_reports_the_rows_before_a_grammar_refusal_with_the_member_on(string options, string text, string rows)
    {
        var codes = rows.Split(' ').Select(Code).ToArray();
        var offAll = Parse(options + " all", text, model: false);
        var onAll = Parse(options + " all", text);
        Assert.Single(offAll.Diagnostics);
        var grammar = offAll.Diagnostics[0];
        Assert.Equal(codes.Append(grammar.Code), onAll.Diagnostics.Select(static d => d.Code));
        Assert.Equal(grammar.Span, onAll.Diagnostics[^1].Span);
        Assert.Null(offAll.Model);
        Assert.Null(onAll.Model);

        Assert.Equal(new[] { grammar.Code }, Parse(options, text, model: false).Diagnostics.Select(static d => d.Code));
        Assert.Equal(new[] { codes[0] }, Parse(options, text).Diagnostics.Select(static d => d.Code));
    }

    /// <summary>
    /// Eval code with no list before a grammar refusal: the grammar's refusal alone, on and off (SJL-15.2 stage 4).
    /// <i>(Added 2026-11-25, at SJL-15.2 stage 4.)</i>
    /// </summary>
    [Fact]
    public void The_partial_walk_reads_no_null_list()
    {
        var off = Parse("e all", "this.#x; (", model: false);
        var on = Parse("e all", "this.#x; (");
        Assert.Single(off.Diagnostics);
        Assert.Equal(Describe(off), Describe(on));
    }
}
