using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Broiler.ECMAScript.Semantics;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// Stage SJL-13.2d (Broiler.VM's JSD-0083, decisions D7, D10 and D11): the catalogue's shared codes, the parse-time early
/// errors of every row the stage table assigns <c>syntax</c>, a positive and a negative source each, the library's order,
/// the cap, first error against all errors, the <c>request</c> row under both switch settings, the regular-expression
/// provider, and the declarations walk's side tables.
/// </summary>
public sealed class EarlyErrorTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Requests.

    /// <summary>A validator that answers one verdict for every literal and records each call.</summary>
    private sealed class RecordingValidator(RegularExpressionVerdict verdict) : IRegularExpressionValidator
    {
        internal List<(string Pattern, string Flags)> Calls { get; } = new();

        public RegularExpressionVerdict Validate(string pattern, string flags)
        {
            Calls.Add((pattern, flags));
            return verdict;
        }
    }

    private static readonly RecordingValidator Admit = new(RegularExpressionVerdict.Valid);

    /// <summary>
    /// A request: <c>m</c> the Module goal, <c>e</c> eval code outside a function, <c>ef</c> inside one, <c>p:</c> or
    /// <c>b:</c> the dynamic function's parameters or body (<c>g</c> a generator), <c>tla</c> top-level await on, <c>-tla</c>
    /// off, <c>-b</c> Annex B off, <c>dec</c>, <c>acc</c>, <c>srcd</c>, <c>srcc</c>, <c>defd</c>, <c>defc</c> a proposal's flag,
    /// <c>n3</c> a nesting bound of three, <c>all</c> all errors.
    /// </summary>
    private static ParseRequest Request(string options, string text, IRegularExpressionValidator? validator = null)
    {
        var set = new HashSet<string>(options.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        var goal = set.Contains("m") ? ParseGoal.Module : set.Contains("e") || set.Contains("ef") ? ParseGoal.Eval
            : set.Contains("p") ? ParseGoal.FormalParameters : set.Contains("b") ? ParseGoal.FunctionBody : ParseGoal.Script;
        var switches = ParseSwitches.For(goal);
        if (set.Contains("tla"))
            switches = switches with { TopLevelAwait = true };
        if (set.Contains("-tla"))
            switches = switches with { TopLevelAwait = false };
        if (set.Contains("-b"))
            switches = switches with { AnnexB = false };
        var proposals = ProposalFlags.None;
        if (set.Contains("dec"))
            proposals |= ProposalFlags.Decorators;
        if (set.Contains("acc"))
            proposals |= ProposalFlags.AutoAccessors;
        if (set.Contains("srcd"))
            proposals |= ProposalFlags.ImportSourceDeclarations;
        if (set.Contains("srcc"))
            proposals |= ProposalFlags.ImportSourceCalls;
        if (set.Contains("defd"))
            proposals |= ProposalFlags.ImportDeferDeclarations;
        if (set.Contains("defc"))
            proposals |= ProposalFlags.ImportDeferCalls;
        var limits = new ParseLimits { Reporting = set.Contains("all") ? ErrorReporting.AllErrors : ErrorReporting.FirstError };
        if (set.Contains("n3"))
            limits = new ParseLimits { NestingDepth = new DepthLimit(3), Reporting = limits.Reporting };
        var request = new ParseRequest(new SourceText(text), goal, Runners.UnicodeDataHome.Tables, validator ?? Admit)
        {
            Switches = switches,
            Proposals = proposals,
            Limits = limits,
            FunctionKind = set.Contains("g") ? DynamicFunctionKind.Generator : DynamicFunctionKind.Normal,
        };
        if (goal == ParseGoal.Eval)
            request = new ParseRequest(request.Source, goal, request.UnicodeTables, request.RegularExpressions)
            {
                Switches = switches,
                Proposals = proposals,
                Limits = limits,
                Context = new InheritedContext { InFunction = set.Contains("ef") },
            };
        return request;
    }

    private static ParseResult Parse(string options, string text, IRegularExpressionValidator? validator = null) =>
        SyntaxParser.Parse(Request(options, text, validator));

    private static string CodeOf(string row) => ((LexicalRefusal)typeof(Rows).GetField(row, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Code;

    // ---------------------------------------------------------------------------------------------------------------
    // The catalogue's codes.

    private static string Root()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !File.Exists(Path.Combine(directory, "Broiler.ECMAScript", "docs", "frontend", "stages.tsv")))
            directory = Path.GetDirectoryName(directory);
        return directory ?? throw new InvalidOperationException("The repository root was not found.");
    }

    private static List<string[]> Table(string name) =>
        File.ReadLines(Path.Combine(Root(), "Broiler.ECMAScript", "docs", "frontend", name)).Skip(1).Select(static l => l.Split('\t')).ToList();

    [Fact]
    public void The_library_s_rows_are_exactly_the_stage_table_s_syntax_rows_with_the_catalogue_s_codes_and_identifiers()
    {
        var catalogue = Table("early-errors.tsv").ToDictionary(static c => c[0], StringComparer.Ordinal);
        var syntax = Table("stages.tsv").Where(static c => c[2] == "syntax").Select(static c => c[0]).ToHashSet(StringComparer.Ordinal);
        var fields = typeof(Rows).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        Assert.Equal(syntax.OrderBy(static s => s, StringComparer.Ordinal), fields.Select(static f => f.Name).OrderBy(static s => s, StringComparer.Ordinal));
        foreach (var field in fields)
        {
            var row = (LexicalRefusal)field.GetValue(null)!;
            Assert.Equal(catalogue[field.Name][10], row.Code);
            Assert.Equal(catalogue[field.Name][11], row.MessageId);
        }

        // The tokenizer's own table names syntax rows too, with the same codes (stage 4b).
        foreach (var property in typeof(Catalogue).GetProperties(BindingFlags.Static | BindingFlags.NonPublic))
        {
            var row = (LexicalRefusal)property.GetValue(null)!;
            Assert.Contains(fields, f => ((LexicalRefusal)f.GetValue(null)!).Code == row.Code && ((LexicalRefusal)f.GetValue(null)!).MessageId == row.MessageId);
        }
    }

    [Fact]
    public void Stage_4c_s_provisional_codes_are_the_rows_they_stood_for()
    {
        Assert.Equal(Rows.EX0008, Parser.UnexpectedToken);
        Assert.Equal(Rows.EX0009, Parser.MissingToken);
        Assert.Equal(Rows.EX0011, Parser.NestingTooDeep);
        Assert.Equal(Rows.EX0012, Parser.TreeTooDeep);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Every syntax row: a source it refuses, first, by its code (or the row the catalogue says reports it), and one it
    // admits.

    /// <summary>Row, options, accepted source, refused source, and the row whose code the refusal carries when not the row's own.</summary>
    public static TheoryData<string, string, string, string, string> Cases => new()
    {
        { "EA0001", "e", "1;", "let a; let a;", "EE0160" },
        { "EA0002", "e", "x;", "new.target;", "EA0002" },
        { "EA0010", "p g", "a = 1", "a = yield", "EE0101" },
        { "EA0011", "b", "let a;", "let a; let a;", "EE0078" },
        { "EA0012", "p", "a, b", "a, a = 1", "EE0069" },
        { "EE0001", "", @"var \u0061x;", @"var \u0031x;", "EE0001" },
        { "EE0002", "", @"var a\u0062;", @"var a\u002D;", "EE0002" },
        { "EE0003", "", "010;", "\"use strict\"; 010;", "EE0003" },
        { "EE0004", "", "\"\\1\";", "\"use strict\"; \"\\1\";", "EE0004" },
        { "EE0005", "", "var eval;", "\"use strict\"; var eval;", "EE0005" },
        { "EE0006", "", "var yield;", "\"use strict\"; var yield;", "EE0006" },
        { "EE0007", "", "var await;", "var await;", "EE0007|m" },
        { "EE0008", "", "function g() { var yield; }", "function* g() { var yield; }", "EE0008" },
        { "EE0009", "", "function f() { var await; }", "async function f() { var await; }", "EE0009" },
        { "EE0010", "", @"function g() { var yi\u0065ld; }", @"function* g() { var yi\u0065ld; }", "EE0010" },
        { "EE0011", "", @"function f() { var aw\u0061it; }", @"async function f() { var aw\u0061it; }", "EE0011" },
        { "EE0012", "", "var implements;", "\"use strict\"; var implements;", "EE0012" },
        { "EE0013", "", @"var aw\u0061it;", @"var aw\u0061it;", "EE0013|m" },
        { "EE0014", "", @"({ v\u0061r: 1 });", @"var v\u0061r;", "EE0014" },
        { "EE0016", "", "class C { #m() {} }", "({ #m() {} });", "EE0016" },
        { "EE0017", "", "({ a = 1 } = {});", "({ a = 1 });", "EE0017" },
        { "EE0019", "", "/a/;", "/a/;", "EE0019|rx" },
        { "EE0020", "", "f`\\uX`;", "`\\uX`;", "EE0020" },
        { "EE0022", "", "f`\\uX${1}`;", "`\\uX${1}`;", "EE0022" },
        { "EE0023", "", "f`${1}\\uX`;", "`${1}\\uX`;", "EE0023" },
        { "EE0024", "", "f`${1}\\uX${2}`;", "`${1}\\uX${2}`;", "EE0024" },
        { "EE0025", "", "(1);", "();", "EE0025" },
        { "EE0026", "", "a.b`c`;", "a?.b`c`;", "EE0026" },
        { "EE0027", "m", "import.meta;", "import.meta;", "EE0027|s" },
        { "EE0028", "", "a++;", "1++;", "EE0028" },
        { "EE0029", "", "++a;", "++1;", "EE0029" },
        { "EE0030", "", "delete x;", "\"use strict\"; delete x;", "EE0030" },
        { "EE0031", "", "delete (x);", "\"use strict\"; delete (x);", "EE0031" },
        { "EE0032", "", "[a] = b;", "[a + 1] = b;", "EE0032" },
        { "EE0033", "", "a = 2;", "1 = 2;", "EE0033" },
        { "EE0034", "", "a += 2;", "1 += 2;", "EE0034" },
        { "EE0035", "", "a &&= 1;", "f() &&= 1;", "EE0035" },
        { "EE0036", "", "({ eval } = {});", "\"use strict\"; ({ eval } = {});", "EE0036" },
        { "EE0037", "", "({ ...a } = {});", "({ ...{} } = {});", "EE0037" },
        { "EE0038", "", "[{ a: b }] = [];", "[{ a: 1 + 1 }] = [];", "EE0038" },
        { "EE0039", "", "[a.b] = [];", "[f()] = [];", "EE0039" },
        { "EE0040", "", "{ let a; } { let a; }", "{ let a; let a; }", "EE0040" },
        { "EE0041", "", "{ let a; } var a;", "{ let a; var a; }", "EE0041" },
        { "EE0042", "", "var let = 1;", "let let = 1;", "EE0042" },
        { "EE0043", "", "let a, b;", "let a, a;", "EE0043" },
        { "EE0044", "", "const a = 1;", "const a;", "EE0044" },
        { "EE0045", "", "if (a) L: f(); else ;", "if (a) L: function f() {} else ;", "EE0045" },
        { "EE0046", "", "if (a) ; else L: f();", "if (a) ; else L: function f() {}", "EE0046" },
        { "EE0047", "", "if (a) L: f();", "if (a) L: function f() {}", "EE0047" },
        { "EE0048", "", "do L: f(); while (0);", "do L: function f() {} while (0);", "EE0048" },
        { "EE0049", "", "while (0) L: f();", "while (0) L: function f() {}", "EE0049" },
        { "EE0050", "", "for (;;) L: f();", "for (;;) L: function f() {}", "EE0050" },
        { "EE0051", "", "for (let a;;) { var b; }", "for (let a;;) { var a; }", "EE0051" },
        { "EE0052", "", "for (a in b) L: f();", "for (a in b) L: function f() {}", "EE0052" },
        { "EE0053", "", "for ([a] of b);", "for ([a + 1] of b);", "EE0053" },
        { "EE0055", "", "for (let a of b);", "for (let let of b);", "EE0055" },
        { "EE0056", "", "for (let a of b) { var c; }", "for (let a of b) { var a; }", "EE0056" },
        { "EE0057", "", "for (let [a, c] of b);", "for (let [a, a] of b);", "EE0057" },
        { "EE0060", "", "with (a) ;", "\"use strict\"; with (a) ;", "EE0060" },
        { "EE0061", "", "with (a) L: f();", "with (a) L: function f() {}", "EE0061" },
        { "EE0062", "", "switch (a) { case 1: let b; }", "switch (a) { case 1: let b; case 2: let b; }", "EE0062" },
        { "EE0063", "", "switch (a) { case 1: let b; case 2: var c; }", "switch (a) { case 1: let b; case 2: var b; }", "EE0063" },
        { "EE0064", "", "L: function f() {}", "\"use strict\"; L: function f() {}", "EE0064" },
        { "EE0065", "", "try {} catch ([a, b]) {}", "try {} catch ([a, a]) {}", "EE0065" },
        { "EE0066", "", "try {} catch (a) { let b; }", "try {} catch (a) { let a; }", "EE0066" },
        { "EE0067", "", "try {} catch (a) { var a; }", "try {} catch ([a]) { var a; }", "EE0067" },
        { "EE0068", "", "(a, b) => 1;", "(a, a) => 1;", "EE0068" },
        { "EE0069", "", "function f(a, a) {}", "function f(a, a = 1) {}", "EE0069" },
        { "EE0070", "", "function f(a, a) {}", "\"use strict\"; function f(a, a) {}", "EE0070" },
        { "EE0071", "", "function eval() {}", "function eval() { \"use strict\"; }", "EE0071" },
        { "EE0072", "", "function f(a) { \"use strict\"; }", "function f(a = 1) { \"use strict\"; }", "EE0072" },
        { "EE0073", "", "function f(a) { var a; }", "function f(a) { let a; }", "EE0073" },
        { "EE0078", "", "function f() { let a; { let a; } }", "function f() { let a; let a; }", "EE0078" },
        { "EE0079", "", "function f() { let a; } var a;", "function f() { let a; var a; }", "EE0079" },
        { "EE0080", "", "function f() { L: ; L: ; }", "function f() { L: L: ; }", "EE0080" },
        { "EE0083", "", "function* g() { (a = 1) => a; }", "function* g() { (a = yield) => 1; }", "EE0083" },
        { "EE0084", "", "async function f() { (a = 1) => a; }", "async function f() { (a = await 1) => a; }", "EE0084" },
        { "EE0085", "", "(a) => { \"use strict\"; };", "(a = 1) => { \"use strict\"; };", "EE0085" },
        { "EE0086", "", "(a) => { var a; };", "(a) => { let a; };", "EE0086" },
        { "EE0087", "", "(a) => 1;", "(a + 1) => 1;", "EE0087" },
        { "EE0088", "", "({ m(a) { \"use strict\"; } });", "({ m(a = 1) { \"use strict\"; } });", "EE0088" },
        { "EE0089", "", "({ m(a) { var a; } });", "({ m(a) { let a; } });", "EE0089" },
        { "EE0090", "", "({ set s([a, b]) {} });", "({ set s([a, a]) {} });", "EE0090" },
        { "EE0091", "", "({ set s(a) { \"use strict\"; } });", "({ set s(a = 1) { \"use strict\"; } });", "EE0091" },
        { "EE0092", "", "({ set s(a) { var a; } });", "({ set s(a) { let a; } });", "EE0092" },
        { "EE0094", "", "({ *g(a = 1) {} });", "({ *g(a = yield) {} });", "EE0094" },
        { "EE0095", "", "({ *g(a) { \"use strict\"; } });", "({ *g(a = 1) { \"use strict\"; } });", "EE0095" },
        { "EE0096", "", "({ *g(a) { var a; } });", "({ *g(a) { let a; } });", "EE0096" },
        { "EE0097", "", "function* g(a, a) {}", "\"use strict\"; function* g(a, a) {}", "EE0097" },
        { "EE0098", "", "function* eval() {}", "function* eval() { \"use strict\"; }", "EE0098" },
        { "EE0099", "", "function* g(a) { \"use strict\"; }", "function* g(a = 1) { \"use strict\"; }", "EE0099" },
        { "EE0100", "", "function* g(a) { var a; }", "function* g(a) { let a; }", "EE0100" },
        { "EE0101", "", "function* g(a = 1) {}", "function* g(a = yield) {}", "EE0101" },
        { "EE0107", "", "({ async *g(a = 1) {} });", "({ async *g(a = yield) {} });", "EE0107" },
        { "EE0108", "", "({ async *g(a = 1) {} });", "({ async *g(a = await 1) {} });", "EE0108" },
        { "EE0109", "", "({ async *g(a) { \"use strict\"; } });", "({ async *g(a = 1) { \"use strict\"; } });", "EE0109" },
        { "EE0110", "", "({ async *g(a) { var a; } });", "({ async *g(a) { let a; } });", "EE0110" },
        { "EE0111", "", "async function* g(a, a) {}", "\"use strict\"; async function* g(a, a) {}", "EE0111" },
        { "EE0112", "", "async function* eval() {}", "async function* eval() { \"use strict\"; }", "EE0112" },
        { "EE0113", "", "async function* g(a) { \"use strict\"; }", "async function* g(a = 1) { \"use strict\"; }", "EE0113" },
        { "EE0114", "", "async function* g(a) { var a; }", "async function* g(a) { let a; }", "EE0114" },
        { "EE0115", "", "async function* g(a = 1) {}", "async function* g(a = yield) {}", "EE0115" },
        { "EE0116", "", "async function* g(a = 1) {}", "async function* g(a = await 1) {}", "EE0116" },
        { "EE0122", "", "class C { constructor() {} static constructor() {} }", "class C { constructor() {} constructor() {} }", "EE0122" },
        { "EE0123", "", "class C { get #a() {} set #a(v) {} }", "class C { #a; #a; }", "EE0123" },
        { "EE0125", "", "class C { static get constructor() {} }", "class C { get constructor() {} }", "EE0125" },
        { "EE0127", "", "class C { prototype() {} }", "class C { static prototype() {} }", "EE0127" },
        { "EE0128", "", "class C { field = 1; }", "class C { constructor = 1; }", "EE0128" },
        { "EE0129", "", "class C { prototype = 1; }", "class C { static prototype = 1; }", "EE0129" },
        { "EE0132", "", "class C { #construct; }", "class C { #constructor; }", "EE0132" },
        { "EE0133", "", "class C { static { let a; } static { let a; } }", "class C { static { let a; let a; } }", "EE0133" },
        { "EE0134", "", "class C { static { let a; } static { var a; } }", "class C { static { let a; var a; } }", "EE0134" },
        { "EE0135", "", "class C { static { L: ; } }", "class C { static { L: L: ; } }", "EE0135" },
        { "EE0140", "", "class C { static { (function () { var await; }); } }", "class C { static { var await; } }", "EE0140" },
        { "EE0141", "", "({ async m(a) { \"use strict\"; } });", "({ async m(a = 1) { \"use strict\"; } });", "EE0141" },
        { "EE0143", "", "({ async m(a = 1) {} });", "({ async m(a = await 1) {} });", "EE0143" },
        { "EE0144", "", "({ async m(a) { var a; } });", "({ async m(a) { let a; } });", "EE0144" },
        { "EE0145", "", "async function f(a) { \"use strict\"; }", "async function f(a = 1) { \"use strict\"; }", "EE0145" },
        { "EE0146", "", "async function f(a = 1) {}", "async function f(a = await 1) {}", "EE0146" },
        { "EE0147", "", "async function f(a, a) {}", "\"use strict\"; async function f(a, a) {}", "EE0147" },
        { "EE0148", "", "async function eval() {}", "async function eval() { \"use strict\"; }", "EE0148" },
        { "EE0149", "", "async function f(a) { var a; }", "async function f(a) { let a; }", "EE0149" },
        { "EE0154", "", "async a => { var a; };", "async a => { let a; };", "EE0154" },
        { "EE0155", "", "async (a) => 1;", "async (a + 1) => 1;", "EE0155" },
        { "EE0156", "", "function* g() { async (a = 1) => 1; }", "function* g() { async (a = yield) => 1; }", "EE0156" },
        { "EE0157", "", "async function f() { async (a = 1) => 1; }", "async function f() { async (a = await 1) => 1; }", "EE0157" },
        { "EE0158", "", "async (a) => { var a; };", "async (a) => { let a; };", "EE0158" },
        { "EE0159", "", "async (a) => { \"use strict\"; };", "async (a = 1) => { \"use strict\"; };", "EE0159" },
        { "EE0160", "", "let a; { let a; }", "let a; let a;", "EE0160" },
        { "EE0161", "", "let a; { var b; }", "let a; var a;", "EE0161" },
        { "EE0163", "", "function f() { new.target; }", "new.target;", "EE0163" },
        { "EE0164", "", "L: ; L: ;", "L: L: ;", "EE0164" },
        { "EE0168", "m", "let a; let b;", "let a; let a;", "EE0168" },
        { "EE0169", "m", "let a; var b;", "let a; var a;", "EE0169" },
        { "EE0173", "m", "function f() { new.target; }", "new.target;", "EE0173" },
        { "EE0174", "m", "L: ;", "L: L: ;", "EE0174" },
        { "EE0178", "m", "export { a as \"b\" }; var a;", "export { a as \"\\uD800\" }; var a;", "EE0178" },
        { "EE0179", "m", "import { a, b } from \"m\";", "import { a, a } from \"m\";", "EE0168" },
        { "EE0180", "m", "import a from \"m\" with { type: \"json\" };", "import a from \"m\" with { type: \"json\", type: \"json\" };", "EE0180" },
        { "EE0181", "m", "export { \"a\" } from \"m\";", "export { \"a\" };", "EE0181" },
        { "EE0182", "m", "export { if } from \"m\";", "export { if };", "EE0182" },
        { "EG0001", "", "<!-- x", "<!-- x", "EG0001|m" },
        { "EG0003", "", "L: function f() {}", "\"use strict\"; L: function f() {}", "EE0064" },
        { "EG0004", "", "{ function f() {} function f() {} }", "\"use strict\"; { function f() {} function f() {} }", "EE0040" },
        { "EG0005", "", "if (a) function f() {}", "\"use strict\"; if (a) function f() {}", "EG0005" },
        { "EG0006", "", "try {} catch (a) { var a; }", "try {} catch (a) { var a; }", "EE0067|-b" },
        { "EG0007", "", "for (var a = 1 in b);", "\"use strict\"; for (var a = 1 in b);", "EG0007" },
        { "EG0008", "", "for (f() in b);", "\"use strict\"; for (f() in b);", "EG0008" },
        { "EP0001", "", "{ using a = b; }", "{ using let = b; }", "EP0001" },
        { "EP0002", "", "{ using a = b, c = d; }", "{ using a = b, a = c; }", "EP0002" },
        { "EP0003", "", "async function f() { await using a = b; }", "async function f() { await (using) a = b; }", "EX0009" },
        { "EP0004", "", "async function f() { await using a = b; }", "async function f() { await using let = b; }", "EP0004" },
        { "EP0005", "", "async function f() { await using a = b, c = d; }", "async function f() { await using a = b, a = c; }", "EP0005" },
        { "EP0006", "", "switch (a) { case 1: { using b = c; } }", "switch (a) { case 1: using b = c; }", "EP0006" },
        { "EP0007", "", "switch (a) { default: { using b = c; } }", "switch (a) { default: using b = c; }", "EP0007" },
        { "EP0008", "", "{ using a = b; }", "using a = b;", "EP0008" },
        { "EP0009", "dec", "@d class C {}", "@d class C {}", "EP0009|-dec" },
        { "EP0010", "acc", "class C { accessor a; }", "class C { accessor a; }", "EP0010|-acc" },
        { "EP0011", "m srcd", "import source a from \"m\";", "import source a from \"m\";", "EP0011|-srcd" },
        { "EP0012", "srcc", "import.source(\"m\");", "import.source(\"m\");", "EP0012|-srcc" },
        { "EP0013", "m defd", "import defer * as a from \"m\";", "import defer a from \"m\";", "EP0013" },
        { "EP0014", "defc", "import.defer(\"m\");", "import.defer(\"m\");", "EP0014|-defc" },
        { "EP0015", "e", "1;", "new.target;", "EA0002" },
        { "EP0016", "e", "x;", "new.target;", "EA0002" },
        { "EX0001", "", "var a = 1;", "var a = \u00A7;", "EX0001" },
        { "EX0002", "", "/* x */", "/* x", "EX0002" },
        { "EX0003", "", "3 in x;", "3in x;", "EX0003" },
        { "EX0004", "", "\"abc\";", "\"abc", "EX0004" },
        { "EX0005", "", "\"\\x41\";", "\"\\x4\";", "EX0005" },
        { "EX0006", "", "/abc/;", "/abc", "EX0006" },
        { "EX0007", "", "`abc`;", "`abc", "EX0007" },
        { "EX0008", "", "var a;", "var 1;", "EX0008" },
        { "EX0009", "", "if (a) {}", "if (a {}", "EX0009" },
        { "EX0010", "m", "import x from \"m\";", "import x from \"m\";", "EX0010|s" },
        { "EX0011", "n3", "(1);", "((((1))));", "EX0011" },
        { "EX0012", "", "a" + string.Concat(Enumerable.Repeat("+a", 100)) + ";", "a" + string.Concat(Enumerable.Repeat("+a", ParseLimits.MaximumTreeDepth + 1)) + ";", "EX0012" },
        { "EX0025", "", @"var \u{61}b;", @"var \u{}a;", "EX0025" },
        { "EX0032", "", "function f() { return; }", "return;", "EX0032" },
        { "EX0033", "", "class C { static { (function () { return; }); } }", "class C { static { return; } }", "EX0033" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_syntax_row_is_refused_first_by_its_code_and_a_source_it_does_not_refuse_is_accepted(string row, string options, string accepted, string refused, string expected)
    {
        // The expected cell: the row whose code the refusal carries, and after a bar the options that differ for the refusal
        // (`m` the Module goal, `s` the Script goal, `-x` an option of the accepted source off, `rx` a validator that refuses).
        var parts = expected.Split('|');
        var refusedOptions = options;
        IRegularExpressionValidator? validator = null;
        if (parts.Length > 1)
        {
            foreach (var option in parts[1].Split(' '))
            {
                refusedOptions = option switch
                {
                    "m" => refusedOptions + " m",
                    "s" => string.Join(' ', refusedOptions.Split(' ').Where(static o => o != "m")),
                    "rx" => refusedOptions,
                    _ when option.StartsWith('-') && option.Length > 2 && !option.StartsWith("-b", StringComparison.Ordinal) =>
                        string.Join(' ', refusedOptions.Split(' ').Where(o => o != option[1..])),
                    _ => refusedOptions + " " + option,
                };
                if (option == "rx")
                    validator = new RecordingValidator(RegularExpressionVerdict.Invalid("regexp.literal.invalid", "x", 0, 0, false));
            }
        }

        var accept = Parse(options, accepted);
        Assert.True(accept.Outcome == ParseOutcome.Accepted, row + ": " + accepted + " was refused: " + string.Join("; ", accept.Diagnostics.Select(static d => d.Code + " " + d.MessageId)));
        var refuse = Parse(refusedOptions, refused, validator);
        Assert.True(refuse.Outcome == ParseOutcome.Refused, row + ": " + refused + " was accepted");
        Assert.Equal(CodeOf(parts[0]), refuse.Diagnostics[0].Code);
    }

    [Fact]
    public void Every_syntax_row_has_a_case()
    {
        var syntax = Table("stages.tsv").Where(static c => c[2] == "syntax").Select(static c => c[0]).ToList();
        var covered = new HashSet<string>(StringComparer.Ordinal) { "EE0019" };
        foreach (var item in Cases)
            covered.Add((string)((ITheoryDataRow)item).GetData()[0]!);
        foreach (var row in DeclarationsWalk.RegularExpressionRows.Select(RowName))
            covered.Add(row);
        Assert.DoesNotContain(syntax, r => !covered.Contains(r));
    }

    private static string RowName(LexicalRefusal refusal) =>
        typeof(Rows).GetFields(BindingFlags.Static | BindingFlags.NonPublic).First(f => (LexicalRefusal)f.GetValue(null)! == refusal).Name;

    [Fact]
    public void No_request_engine_or_semantics_row_is_reported()
    {
        // EX0023 (request): the switch off reads the top level as a non-async function's body, with no diagnostic of its own.
        var codes = Table("stages.tsv").Where(static c => c[2] != "syntax").Select(static c => c[1]).ToHashSet(StringComparer.Ordinal);
        foreach (var (options, text) in new[]
        {
            ("m -tla", "await x;"), ("m -tla", "for await (x of y);"), ("", "await x;"), ("", "for await (x of y);"),
            ("", "break;"), ("", "continue;"), ("", "x: break y;"), ("m", "export { a };"), ("", "class C { m() { this.#x; } }"),
            ("", "({ __proto__: 1, __proto__: 2 });"), ("", "super.x;"), ("", "function f() { super(); }"),
        })
        {
            var result = Parse(options, text);
            foreach (var diagnostic in result.Diagnostics)
                Assert.DoesNotContain(diagnostic.Code, codes);
        }
    }

    [Fact]
    public void The_request_row_follows_the_top_level_await_switch_in_both_settings()
    {
        // On: the top level reads with [Await]; off: as a non-async function's body of the same goal (decision D10).
        foreach (var text in new[] { "await x;", "for await (x of y);", "await using r = s;" })
        {
            Assert.Equal(ParseOutcome.Accepted, Parse("m tla", text).Outcome);
            Assert.Equal(ParseOutcome.Refused, Parse("m -tla", text).Outcome);
            Assert.Equal(ParseOutcome.Accepted, Parse("tla", text.Replace("await using", "{ await using", StringComparison.Ordinal) + (text.StartsWith("await using", StringComparison.Ordinal) ? " }" : string.Empty)).Outcome);
            Assert.Equal(ParseOutcome.Refused, Parse("-tla", text).Outcome);
        }

        // With the switch off, `await` is a name in a script and a reserved word in a module, as in a non-async function.
        Assert.Equal(ParseOutcome.Accepted, Parse("-tla", "var await; await;").Outcome);
        Assert.Equal(CodeOf("EE0007"), Parse("m -tla", "var await;").Diagnostics[0].Code);
        Assert.Equal(CodeOf("EE0009"), Parse("tla", "var await;").Diagnostics[0].Code);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The library's order, the cap, first error and all errors (decision D11).

    [Fact]
    public void Diagnostics_come_in_source_order_and_a_scope_s_conflicts_when_it_closes()
    {
        const string text = "\"use strict\";\n{ let a; let a; }\nwith (b) ;\nvar eval;";
        var all = Parse("all", text).Diagnostics;
        Assert.Equal(new[] { CodeOf("EE0040"), CodeOf("EE0060"), CodeOf("EE0005") }, all.Select(static d => d.Code));
        Assert.True(all.Select(static d => d.Span.Offset).SequenceEqual(all.Select(static d => d.Span.Offset).Order()));
        Assert.Equal(DiagnosticPhase.Semantics, all[0].Phase);

        // A construct's refusal inside a scope comes before the scope's conflict, which waits for the scope to close.
        var nested = Parse("all", "\"use strict\"; { let a; with (b) ; let a; }").Diagnostics;
        Assert.Equal(new[] { CodeOf("EE0060"), CodeOf("EE0040") }, nested.Select(static d => d.Code));

        // An early error in a statement completed before the grammar's refusal comes first.
        var mixed = Parse("all", "\"use strict\"; with (a) ; var 1;").Diagnostics;
        Assert.Equal(new[] { CodeOf("EE0060"), CodeOf("EX0008") }, mixed.Select(static d => d.Code));
        Assert.Equal(DiagnosticPhase.Parser, mixed[1].Phase);
        Assert.Equal(CodeOf("EE0060"), Parse("", "\"use strict\"; with (a) ; var 1;").Diagnostics.Single().Code);
    }

    [Fact]
    public void First_error_reports_one_and_all_errors_reports_every_refusal_up_to_the_cap()
    {
        const string text = "\"use strict\"; var eval; var arguments; with (a) ; 010;";
        Assert.Single(Parse("", text).Diagnostics);
        Assert.Equal(4, Parse("all", text).Diagnostics.Count);
        var capped = SyntaxParser.Parse(new ParseRequest(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, Admit)
        {
            Limits = new ParseLimits { Reporting = ErrorReporting.AllErrors, DiagnosticCap = 2 },
        });
        Assert.Equal(new[] { CodeOf("EE0005"), CodeOf("EE0005") }, capped.Diagnostics.Select(static d => d.Code));
        Assert.Null(capped.Tree);
        Assert.Null(capped.Declarations);
    }

    [Fact]
    public void A_diagnostic_carries_its_message_identifier_and_its_arguments()
    {
        var diagnostic = Parse("", "let a; let a;").Diagnostics.Single();
        Assert.Equal("scope.lexical.duplicate", diagnostic.MessageId);
        Assert.Equal(new[] { "a" }, diagnostic.Arguments);
        Assert.Equal(11, diagnostic.Span.Offset);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The regular-expression provider (decision D7).

    [Fact]
    public void The_provider_is_asked_once_per_literal_in_source_order_and_its_refusal_is_reported_at_the_literal()
    {
        var recorder = new RecordingValidator(RegularExpressionVerdict.Valid);
        Assert.Equal(ParseOutcome.Accepted, Parse("", "/a/g; x = /b/i; function f() { return /c/; }", recorder).Outcome);
        Assert.Equal(new[] { ("a", "g"), ("b", "i"), ("c", string.Empty) }, recorder.Calls);

        // Absent: no literal, no call.
        var silent = new RecordingValidator(RegularExpressionVerdict.Invalid("regexp.literal.invalid", null, 0, 0, false));
        Assert.Equal(ParseOutcome.Accepted, Parse("", "a / b / c;", silent).Outcome);
        Assert.Empty(silent.Calls);

        // Refused: at the literal's span, by the row the verdict's identifier names.
        var refusing = new RecordingValidator(RegularExpressionVerdict.Invalid("regexp.flags.invalid", "gg", 7, 1, true));
        var diagnostic = Parse("", "x = 1; /a/gg;", refusing).Diagnostics.Single();
        Assert.Equal(7, diagnostic.Span.Offset);
        Assert.Equal(5, diagnostic.Span.Length);
        Assert.Equal(DiagnosticPhase.Semantics, diagnostic.Phase);
    }

    [Fact]
    public void A_verdict_names_its_row_by_its_identifier_or_falls_back_to_EE0019()
    {
        foreach (var row in DeclarationsWalk.RegularExpressionRows)
        {
            var first = DeclarationsWalk.RegularExpressionRows.First(r => r.MessageId == row.MessageId);
            var diagnostic = Parse("", "/a/;", new RecordingValidator(RegularExpressionVerdict.Invalid(row.MessageId, "a", 0, 0, false))).Diagnostics.Single();
            Assert.Equal(first.Code, diagnostic.Code);
            Assert.Equal(row.MessageId, diagnostic.MessageId);
        }

        // An identifier no row has, and the validator's own nesting bound (EX0026, an engine row the library never reports).
        Assert.Equal(CodeOf("EE0019"), Parse("", "/a/;", new RecordingValidator(RegularExpressionVerdict.Invalid("validator.own", null, 0, 0, false))).Diagnostics.Single().Code);
        var deep = Parse("", "/((a))/;", new RecordingValidator(RegularExpressionVerdict.NestedTooDeep("limit.regexp.too-deep", null, 0, 1))).Diagnostics.Single();
        Assert.Equal(CodeOf("EE0019"), deep.Code);
        Assert.Equal("regexp.literal.invalid", deep.MessageId);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The side tables (decision D10).

    [Fact]
    public void The_side_tables_hold_each_scope_s_declarations_strictness_and_parameters_in_source_order()
    {
        const string text = "var v; let l; function f(a, b, c) { \"use strict\"; let x; var y; { const z = 1; class K {} } }"
            + " if (q) function g() {} try {} catch (e) {} for (let i of j) ;";
        var result = Parse("", text);
        Assert.Equal(ParseOutcome.Accepted, result.Outcome);
        var tables = result.Declarations!;

        var program = tables.Scopes[0];
        Assert.Equal(ScopeKind.Script, program.Kind);
        Assert.Equal(new[] { "v", "l", "f" }, program.Declarations.Select(static d => d.Name));
        Assert.Equal(new[] { BindingKind.Var, BindingKind.Let, BindingKind.Function }, program.Declarations.Select(static d => d.Kind));
        Assert.Equal(new[] { false, true, false }, program.Declarations.Select(static d => d.IsLexical));

        var kinds = tables.Scopes.Select(static s => s.Kind).ToList();
        Assert.Equal(new[] { ScopeKind.Script, ScopeKind.Function, ScopeKind.Block, ScopeKind.Block, ScopeKind.Function, ScopeKind.Block, ScopeKind.Catch, ScopeKind.Block, ScopeKind.For }, kinds);
        var function = tables.Scopes[1];
        Assert.Equal(new[] { "x", "y" }, function.Declarations.Select(static d => d.Name));
        Assert.Equal(new[] { "z", "K" }, tables.Scopes[2].Declarations.Select(static d => d.Name));
        var ifClause = tables.Scopes[3].Declarations.Single();
        Assert.True(ifClause.IsBlockFunction && ifClause.IsIfClauseFunction);
        Assert.Equal(new[] { "e" }, tables.Scopes[6].Declarations.Select(static d => d.Name));
        Assert.Equal(new[] { "i" }, tables.Scopes[8].Declarations.Select(static d => d.Name));

        Assert.False(tables.Functions[0].IsStrict);
        Assert.True(tables.Functions[1].IsStrict);
        Assert.Equal(new[] { "a", "b", "c" }, tables.Functions[1].ParameterNames);
        Assert.True(tables.Scopes.Select(static s => s.Node.Span.Offset).SequenceEqual(tables.Scopes.Select(static s => s.Node.Span.Offset).Order()));
    }

    [Fact]
    public void A_module_s_and_a_strict_program_s_tables_say_so()
    {
        var module = Parse("m", "import a from \"m\"; export default 1; export function f() {}").Declarations!;
        Assert.Equal(ScopeKind.Module, module.Scopes[0].Kind);
        Assert.Equal(new[] { "a", "*default*", "f" }, module.Scopes[0].Declarations.Select(static d => d.Name));
        Assert.True(module.Functions[0].IsStrict);
        Assert.True(Parse("", "\"use strict\";").Declarations!.Functions[0].IsStrict);
    }
}
