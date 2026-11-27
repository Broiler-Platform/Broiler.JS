using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Semantics;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The canonical dump covers every member of the semantic model (SJL-15.2 stage 8; the session's decision D-js-part-g, so
/// that what the determinism runs compare holds every model fact, part G of bundle sjl-15-001's rule): for each public
/// member of <see cref="SemanticModel"/> and of the model's types, a fixture whose dump holds the line kind that writes
/// the member, and, where a second fixture is given, the lines of that kind differ between the two, the second differing
/// from the first in that member's value. The table is held to the types' public members by reflection, so that a member
/// added later without a line fails here. A map from member to line kind alone would pass with no dump line at all.
/// </summary>
public sealed class DumpCoverageTests
{
    // The types of the model whose public members the dump writes: the Semantics namespace's classes, and the caller's
    // records CallerScopesOf answers.
    private static readonly Type[] ModelTypes =
    [
        typeof(SemanticModel), typeof(DeclarationTables), typeof(Scope), typeof(Declaration), typeof(FunctionFacts),
        typeof(EnvironmentFacts), typeof(LabelTarget), typeof(PrivateEnvironment), typeof(PrivateNameBinding), typeof(Resolution),
        typeof(ModuleRecord), typeof(ModuleRequest), typeof(ModuleRequestAttribute), typeof(ImportEntry), typeof(ExportEntry),
        typeof(CallerScope),
    ];

    private const string S = "s";
    private const string M = "m";

    // Member, the line kind that writes it, the goal (s Script, m Module), the fixture, and the fixture differing in the
    // member's value, or null.
    private static readonly (string Member, string Kind, string Goal, string Source, string? Other)[] Table =
    [
        ("SemanticModel.Scopes", "model-scope", S, "{ let x; }", "with (o) {}"),
        ("SemanticModel.Functions", "model-function", S, "function f() {}", "function* f() {}"),
        ("SemanticModel.ScopeOf(SyntaxNode)", "model-node", S, "{ let x; x; }", "{ let x; } x;"),
        ("SemanticModel.ParentOf(Scope)", "model-scope", S, "{ { let x; } }", "{ let y; } { let x; }"),
        ("SemanticModel.DeclarationOf(SyntaxNode)", "model-node", S, "var x;", "x;"),
        ("SemanticModel.FactsOf(SyntaxNode)", "model-function", S, "function f() {}", "async function f() {}"),
        ("SemanticModel.TargetOf(BreakStatement)", "model-target", S, "a: { break a; }", "a: b: { break a; }"),
        ("SemanticModel.TargetOf(ContinueStatement)", "model-target", S, "for (;;) { continue; }", "a: for (;;) { continue a; }"),
        ("SemanticModel.PrivateEnvironmentOf(ClassNode)", "model-private-environment", S, "class C { #a; }", "class C { m() { class D { #b; } } }"),
        ("SemanticModel.Resolve(PrivateIdentifier)", "model-private", S, "class C { #a; m() { this.#a; } }", "class C { #a; m() { class D { #a; n() { this.#a; } } } }"),
        ("SemanticModel.Resolve(Identifier)", "model-resolution", S, "var x; x;", "x;"),
        ("SemanticModel.Resolve(BindingIdentifier)", "model-resolution", S, "var x;", "with (o) { var x; }"),
        ("SemanticModel.EnvironmentOf(SyntaxNode)", "model-environment", S, "function f() { arguments; }", "function f() {}"),
        ("SemanticModel.AnnexBAlias(Declaration)", "model-annexb", S, "{ function f() {} }", "let f; { function f() {} }"),
        ("SemanticModel.DirectEvalCalls", "model-eval-call", S, "eval('1');", "eval('1'); eval('2');"),
        ("SemanticModel.CallerScopesOf(CallExpression)", "model-caller-scope", S, "{ let x; eval('1'); }", "{ let y; eval('1'); }"),
        ("SemanticModel.Module", "model-module", M, "export var x;", "await 0;"),
        ("SemanticModel.NamedEvaluationName(SyntaxNode)", "model-named", S, "var f = function () {};", "var g = function () {};"),
        ("SemanticModel.NamedEvaluationKey(SyntaxNode)", "model-named", S, "({ [k]: function () {} });", "({ k: function () {} });"),
        ("SemanticModel.FreeNames(SyntaxNode)", "model-captures", S, "function f() { x; }", "function f() { y; }"),
        ("SemanticModel.Captured(SyntaxNode)", "model-captured", S, "var x; function f() { x; }", "function f() { x; }"),

        ("DeclarationTables.Scopes", "scope ", S, "{ let x; }", "for (let x;;) {}"),
        ("DeclarationTables.Functions", "function ", S, "function f(a) {}", "function f(b) {}"),

        ("Scope.Kind", "model-scope", S, "{ let x; }", "with (o) {}"),
        ("Scope.Node", "model-scope", S, "{ let x; }", "{} { let x; }"),
        ("Scope.Declarations", "model-declaration", S, "var x;", "var y;"),

        ("Declaration.Name", "model-declaration", S, "var x;", "var y;"),
        ("Declaration.Kind", "model-declaration", S, "var x;", "let x;"),
        ("Declaration.Node", "model-declaration", S, "var x;", "var y; var x;"),
        ("Declaration.IsLexical", "model-declaration", S, "var x;", "let x;"),
        ("Declaration.IsBlockFunction", "model-declaration", S, "{ function f() {} }", "{ let f; }"),
        ("Declaration.IsIfClauseFunction", "model-declaration", S, "if (1) function f() {}", "if (1) { function f() {} }"),
        ("Declaration.IsMutable", "model-declaration", S, "let x;", "const x = 1;"),

        ("FunctionFacts.Node", "model-function", S, "function f() {}", "var g; function f() {}"),
        ("FunctionFacts.Kind", "model-function", S, "function f() {}", "var f = () => {};"),
        ("FunctionFacts.IsStrict", "model-function", S, "function f() {}", "function f() { 'use strict'; }"),
        ("FunctionFacts.IsAsync", "model-function", S, "function f() {}", "async function f() {}"),
        ("FunctionFacts.IsGenerator", "model-function", S, "function f() {}", "function* f() {}"),
        ("FunctionFacts.HasSimpleParameterList", "model-function", S, "function f(a) {}", "function f(...a) {}"),
        ("FunctionFacts.HasParameterExpressions", "model-function", S, "function f(a) {}", "function f(a = 1) {}"),
        ("FunctionFacts.ParameterNames", "model-function", S, "function f(a) {}", "function f(b) {}"),
        ("FunctionFacts.HasSeparateVariableEnvironment", "model-environment", S, "function f(a) {}", "function f(a = 1) {}"),

        ("EnvironmentFacts.Node", "model-environment", S, "function f() {}", "var g; function f() {}"),
        ("EnvironmentFacts.Arguments", "model-environment", S, "function f() { arguments; }", "function f() {}"),
        ("EnvironmentFacts.ArgumentsNeeded", "model-environment", S, "function f() { arguments; }", "function f() {}"),
        ("EnvironmentFacts.ArgumentsMapped", "model-environment", S, "function f(a) { arguments; }", "function f(a) { 'use strict'; arguments; }"),
        ("EnvironmentFacts.DirectEvalInParameters", "model-environment", S, "function f(a = eval('1')) {}", "function f(a = 1) {}"),
        ("EnvironmentFacts.DirectEvalInBody", "model-environment", S, "function f() { eval('1'); }", "function f() {}"),
        ("EnvironmentFacts.ContainsWith", "model-environment", S, "function f() { with (o) {} }", "function f() {}"),
        ("EnvironmentFacts.DirectEvalOrWithWithin", "model-captures", S, "function f() { () => eval('1'); }", "function f() { () => 1; }"),
        ("EnvironmentFacts.FunctionNameReferenced", "model-captures", S, "var f = function g() { g; };", "var f = function g() {};"),

        ("LabelTarget.Statement", "model-target", S, "a: { break a; }", "a: { b: { break a; } }"),
        ("LabelTarget.LabelSet", "model-target", S, "a: for (;;) { break; }", "b: for (;;) { break; }"),

        ("PrivateEnvironment.Class", "model-private-environment", S, "class C { #a; }", "var x; class C { #a; }"),
        ("PrivateEnvironment.Names", "model-private-name", S, "class C { #a; }", "class C { #b; }"),
        ("PrivateEnvironment.Outer", "model-private-environment", S, "class C { m() { class D {} } }", "class C {} class D {}"),

        ("PrivateNameBinding.Name", "model-private-name", S, "class C { #a; }", "class C { #b; }"),
        ("PrivateNameBinding.Kind", "model-private-name", S, "class C { #a; }", "class C { #a() {} }"),
        ("PrivateNameBinding.IsStatic", "model-private-name", S, "class C { #a; }", "class C { static #a; }"),
        ("PrivateNameBinding.Elements", "model-private-name", S, "class C { get #a() {} }", "class C { get #a() {} set #a(v) {} }"),
        ("PrivateNameBinding.Environment", "model-private", S, "class C { #a; m() { this.#a; } }", "class C { #a; m() { class D { #a; n() { this.#a; } } } }"),

        ("Resolution.Kind", "model-resolution", S, "var x; x;", "x;"),
        ("Resolution.Scope", "model-resolution", S, "var x; x;", "{ let x; x; }"),
        ("Resolution.Declaration", "model-resolution", S, "var x; x;", "var y, x; x;"),
        ("Resolution.Reason", "model-resolution", S, "with (o) { x; }", "function f() { eval('1'); x; }"),
        ("Resolution.DynamicScopes", "model-resolution", S, "with (o) { x; }", "with (o) { with (p) { x; } }"),
        ("Resolution.Fallback", "model-resolution", S, "var x; with (o) { x; }", "with (o) { x; }"),

        ("ModuleRecord.HasTopLevelAwait", "model-module", M, "export var x;", "await 0;"),
        ("ModuleRecord.RequestedModules", "model-request", M, "import 'a';", "import 'b';"),
        ("ModuleRecord.ImportEntries", "model-import", M, "import a from 'm';", "import { a } from 'm';"),
        ("ModuleRecord.LocalExportEntries", "model-export-local", M, "export var a;", "export var b;"),
        ("ModuleRecord.IndirectExportEntries", "model-export-indirect", M, "export { a } from 'm';", "export { b } from 'm';"),
        ("ModuleRecord.StarExportEntries", "model-export-star", M, "export * from 'm';", "import 'n'; export * from 'm';"),

        ("ModuleRequest.Specifier", "model-request", M, "import 'a';", "import 'b';"),
        ("ModuleRequest.Phase", "model-request", M, "import a from 'm';", null),
        ("ModuleRequest.Statements", "model-request", M, "import 'a';", "import 'a'; import 'a';"),
        ("ModuleRequest.Attributes", "model-attribute", M, "import a from 'm' with { type: 'json' };", "import a from 'm' with { type: 'css' };"),

        ("ModuleRequestAttribute.Key", "model-attribute", M, "import a from 'm' with { type: 'json' };", "import a from 'm' with { kind: 'json' };"),
        ("ModuleRequestAttribute.Value", "model-attribute", M, "import a from 'm' with { type: 'json' };", "import a from 'm' with { type: 'css' };"),

        ("ImportEntry.ModuleRequest", "model-import", M, "import a from 'm';", "import 'n'; import a from 'm';"),
        ("ImportEntry.ImportKind", "model-import", M, "import a from 'm';", "import * as a from 'm';"),
        ("ImportEntry.ImportName", "model-import", M, "import { a } from 'm';", "import { b as a } from 'm';"),
        ("ImportEntry.LocalName", "model-import", M, "import { a } from 'm';", "import { a as b } from 'm';"),
        ("ImportEntry.Node", "model-import", M, "import a from 'm';", "import x from 'n'; import a from 'm';"),
        ("ImportEntry.Declaration", "model-import", M, "import a from 'm';", "import b from 'm';"),

        ("ExportEntry.ExportName", "model-export-local", M, "var a; export { a };", "var a; export { a as b };"),
        ("ExportEntry.LocalName", "model-export-local", M, "var a, b; export { a as c };", "var a, b; export { b as c };"),
        ("ExportEntry.Node", "model-export-local", M, "export var a;", "export var b, a;"),
        ("ExportEntry.Declaration", "model-export-local", M, "export var a;", "var b; export var a;"),
        ("ExportEntry.ModuleRequest", "model-export-indirect", M, "export { a } from 'm';", "import 'n'; export { a } from 'm';"),
        ("ExportEntry.ImportKind", "model-export-indirect", M, "export { a } from 'm';", "export * as a from 'm';"),
        ("ExportEntry.ImportName", "model-export-indirect", M, "export { a } from 'm';", "export { b as a } from 'm';"),

        ("CallerScope.Names", "model-caller-scope", S, "{ let x; eval('1'); }", "{ let y; eval('1'); }"),
        ("CallerScope.IsCatch", "model-caller-scope", S, "try {} catch (x) { eval('1'); }", "try {} catch (x) { { let x; eval('1'); } }"),
    ];

    /// <summary>The table's rows, for the theory below.</summary>
    public static TheoryData<string> Members()
    {
        var data = new TheoryData<string>();
        foreach (var row in Table)
            data.Add(row.Member);
        return data;
    }

    [Fact]
    public void The_table_names_every_public_member_of_the_model_s_types_once()
    {
        var members = new List<string>();
        foreach (var type in ModelTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                members.Add(type.Name + "." + property.Name);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(static m => !m.IsSpecialName))
                members.Add(type.Name + "." + method.Name + "(" + string.Join(",", method.GetParameters().Select(static p => p.ParameterType.Name)) + ")");
        }

        Assert.Equal(members.Order(StringComparer.Ordinal), Table.Select(static r => r.Member).Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Members))]
    public void Each_member_has_a_dump_line_that_moves_with_its_value(string member)
    {
        var row = Table.Single(r => r.Member == member);
        var lines = Lines(row.Kind, row.Goal, row.Source);
        Assert.True(lines.Count > 0, member + ": no " + row.Kind + " line in the dump of " + row.Source);
        if (row.Other is { } other)
            Assert.False(lines.SequenceEqual(Lines(row.Kind, row.Goal, other)), member + ": the " + row.Kind + " lines are the same for " + row.Source + " and " + other);
    }

    [Fact]
    public void A_dump_without_a_model_has_no_model_line()
    {
        var result = SyntaxParser.Parse(new ParseRequest(new SourceText("var x; function f() { x; }"), ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance));
        Assert.Null(result.Model);
        Assert.DoesNotContain(Encoding.UTF8.GetString(SyntaxDump.Dump(result)).Split('\n'), static l => l.StartsWith("model-", StringComparison.Ordinal));
    }

    // The lines of one kind in the dump of a source parsed with the member on, which must be accepted.
    private static List<string> Lines(string kind, string goal, string source)
    {
        var request = new ParseRequest(new SourceText(source), goal == M ? ParseGoal.Module : ParseGoal.Script, UnicodeDataHome.Tables, NoValidator.Instance)
        {
            BuildSemanticModel = true,
        };
        var result = SyntaxParser.Parse(request);
        Assert.True(result.Outcome == ParseOutcome.Accepted, source + ": " + (result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code + " " + result.Diagnostics[0].MessageId : "-"));
        Assert.NotNull(result.Model);
        var prefix = kind.EndsWith(' ') ? kind : kind + " ";
        return Encoding.UTF8.GetString(SyntaxDump.Dump(result)).Split('\n').Where(l => l.StartsWith(prefix, StringComparison.Ordinal)).ToList();
    }
}
