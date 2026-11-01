using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>A tree rendered as an S-expression, for tests that read its shape: each node's kind and the facts that name it.</summary>
internal static class Tree
{
    internal static string Dump(SyntaxNode node)
    {
        var builder = new StringBuilder();
        Write(builder, node);
        return builder.ToString();
    }

    private static void Write(StringBuilder into, SyntaxNode node)
    {
        into.Append('(').Append(node.Kind);
        var fact = Fact(node);
        if (fact.Length > 0)
            into.Append(' ').Append(fact);
        var children = new List<SyntaxNode>();
        node.AddChildren(children);
        foreach (var child in children)
        {
            into.Append(' ');
            Write(into, child);
        }

        into.Append(')');
    }

    private static string Fact(SyntaxNode node) => node switch
    {
        Identifier n => n.Name,
        BindingIdentifier n => n.Name,
        IdentifierName n => n.Name,
        PrivateIdentifier n => n.Name,
        NumericLiteral n => n.Text,
        StringLiteral n => "'" + n.Value + "'",
        BooleanLiteral n => n.Value ? "true" : "false",
        RegularExpressionLiteral n => "/" + n.Pattern + "/" + n.Flags,
        TemplateElement n => "`" + (n.Cooked ?? "<invalid>") + "|" + n.Raw + "`",
        BinaryExpression n => n.Operator.ToString(),
        LogicalExpression n => n.Operator.ToString(),
        UnaryExpression n => n.Operator.ToString(),
        UpdateExpression n => (n.IsPrefix ? "prefix " : "postfix ") + n.Operator,
        AssignmentExpression n => n.Operator.ToString(),
        MemberExpression n => (n.IsComputed ? "computed" : "dot") + (n.IsOptional ? " optional" : string.Empty),
        CallExpression n => n.IsOptional ? "optional" : string.Empty,
        YieldExpression n => n.IsDelegate ? "delegate" : string.Empty,
        VariableDeclaration n => n.DeclarationKind.ToString(),
        ExpressionStatement { Directive: { } d } => "directive '" + d + "'",
        FunctionNode n => string.Join(" ", new[] { n.IsAsync ? "async" : null, n.IsGenerator ? "generator" : null, n.IsArrow ? "arrow" : null }.Where(static s => s is not null)),
        PropertyDefinition n => n.PropertyKind.ToString(),
        PatternProperty n => n.IsShorthand ? "shorthand" : string.Empty,
        MethodDefinition n => (n.IsStatic ? "static " : string.Empty) + n.MethodKind,
        FieldDefinition n => (n.IsStatic ? "static" : string.Empty) + (n.IsAccessor ? " accessor" : string.Empty),
        ForOfStatement n => n.IsAwait ? "await" : string.Empty,
        ImportDeclaration n => n.Phase.ToString(),
        ImportCall n => n.Phase.ToString(),
        ArrayLiteral n => n.TrailingComma ? "trailing" : string.Empty,
        ProgramNode n => n.HasTopLevelAwait ? "tla" : string.Empty,
        _ => string.Empty,
    };

    /// <summary>Every node of the tree, in document order, with its parent.</summary>
    internal static IEnumerable<(SyntaxNode Node, SyntaxNode? Parent)> Walk(SyntaxNode root)
    {
        var stack = new Stack<(SyntaxNode, SyntaxNode?)>();
        stack.Push((root, null));
        while (stack.Count > 0)
        {
            var (node, parent) = stack.Pop();
            yield return (node, parent);
            var children = new List<SyntaxNode>();
            node.AddChildren(children);
            for (var i = children.Count - 1; i >= 0; i--)
                stack.Push((children[i], node));
        }
    }
}

/// <summary>
/// The parser (stage SJL-13.2c; Broiler.VM's JSD-0083, sub-area P): the syntactic grammar of every goal, production by
/// production; the correspondence's rows P; cover grammars converted; automatic semicolon insertion; the limits; full spans.
/// </summary>
public sealed class ParserTests
{
    internal static ParseRequest Request(string text, ParseGoal goal = ParseGoal.Script) =>
        new(new SourceText(text), goal, Runners.UnicodeDataHome.Tables, NoValidator.Instance);

    internal static ParseResult Parse(ParseRequest request) => SyntaxParser.Parse(request);

    internal static ParseResult Parse(string text, ParseGoal goal = ParseGoal.Script) => Parse(Request(text, goal));

    private static ProgramNode Accept(string text, ParseGoal goal = ParseGoal.Script) => Accept(Request(text, goal));

    private static ProgramNode Accept(ParseRequest request)
    {
        var result = Parse(request);
        Assert.True(result.Outcome == ParseOutcome.Accepted, request.Source.Text + " was refused: " + string.Join(", ", result.Diagnostics.Select(static d => d.Code + " " + d.MessageId + " at " + d.Span.Line + ":" + d.Span.Column)));
        Assert.Empty(result.Diagnostics);
        return result.Tree!;
    }

    private static SyntaxDiagnostic Refuse(string text, ParseGoal goal = ParseGoal.Script) => Refuse(Request(text, goal));

    private static SyntaxDiagnostic Refuse(ParseRequest request)
    {
        var result = Parse(request);
        Assert.True(result.Outcome == ParseOutcome.Refused, request.Source.Text + " was accepted: " + (result.Tree is null ? string.Empty : Tree.Dump(result.Tree)));
        Assert.Null(result.Tree);
        return Assert.Single(result.Diagnostics);
    }

    private static string First(string text, ParseGoal goal = ParseGoal.Script) => Tree.Dump(Accept(text, goal).Body[0]);

    // ---------------------------------------------------------------------------------------------------------------
    // The parse entry.

    [Fact]
    public void The_entry_answers_accepted_or_refused_for_every_goal_and_never_not_implemented()
    {
        foreach (var goal in Enum.GetValues<ParseGoal>())
        {
            foreach (var text in new[] { string.Empty, "1", "let let = 1;", "/(/", "a\u2028b", "(", ")", "{" })
                Assert.NotEqual(ParseOutcome.NotImplemented, Parse(text, goal).Outcome);
        }
    }

    [Fact]
    public void A_refusal_is_a_parser_diagnostic_with_a_provisional_code_at_the_offending_token()
    {
        var unexpected = Refuse("var a = 1 +;\n");
        Assert.Equal(("JSE9108", "syntax.token.unexpected", DiagnosticPhase.Parser), (unexpected.Code, unexpected.MessageId, unexpected.Phase));
        Assert.Equal((11, 1, 1, 12), (unexpected.Span.Offset, unexpected.Span.Length, unexpected.Span.Line, unexpected.Span.Column));
        Assert.Equal([";"], unexpected.Arguments);

        var missing = Refuse("if (a b");
        Assert.Equal(("JSE9109", "syntax.token.missing"), (missing.Code, missing.MessageId));
        Assert.Equal((6, 7), (missing.Span.Offset, missing.Span.Column));
        Assert.Equal([")"], missing.Arguments);

        var end = Refuse("f(");
        Assert.Equal((2, 0), (end.Span.Offset, end.Span.Length));

        // A lexical refusal reached by the parse keeps the tokenizer's diagnostic.
        Assert.Equal(("JSE9104", DiagnosticPhase.Tokenizer), (Refuse("x = 'abc").Code, Refuse("x = 'abc").Phase));

        // A lexical refusal past an earlier grammar refusal is never reached.
        Assert.Equal("JSE9108", Refuse("x = ) 'abc").Code);
    }

    [Fact]
    public void An_accepted_parse_answers_its_tree_and_the_tokens_it_read_and_asks_no_regular_expression_provider()
    {
        var result = Parse("var x = /re/g; x");
        Assert.Equal(ParseOutcome.Accepted, result.Outcome);
        Assert.Equal(SyntaxKind.Program, result.Tree!.Kind);
        Assert.Equal([TokenKind.IdentifierName, TokenKind.IdentifierName, TokenKind.Equals, TokenKind.RegularExpressionLiteral, TokenKind.Semicolon, TokenKind.IdentifierName],
            result.Tokens.Select(static t => t.Kind).ToArray());
        Assert.Empty(Parse("var x = ").Tokens);
    }

    [Fact]
    public void The_meter_is_charged_the_text_s_length_once_before_the_parse()
    {
        var charges = new List<ulong>();
        var request = new ParseRequest(new SourceText("var a = 1;"), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance) { Meter = charges.Add };
        Parse(request);
        Assert.Equal([10UL], charges);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Goals (ECMA-262 clauses 16.1, 16.2, 19.2.1.1 and 20.2.1.1.1).

    [Fact]
    public void A_script_has_no_return_import_or_export_and_eval_code_is_a_script()
    {
        foreach (var goal in new[] { ParseGoal.Script, ParseGoal.Eval })
        {
            Refuse("return 1;", goal);
            Refuse("import x from 'm';", goal);
            Refuse("export var a;", goal);
            Accept("import('m'); import.meta; new.target; super.x;", goal);
        }
    }

    [Fact]
    public void A_module_is_strict_has_imports_and_exports_and_awaits_at_its_top_level()
    {
        var module = Accept("import a, { b as c, 'd' as e } from 'm' with { type: 'json' };\nexport { a as default, c };\nawait a;", ParseGoal.Module);
        Assert.True(module.HasTopLevelAwait);
        Assert.Equal(SyntaxKind.ImportDeclaration, module.Body[0].Kind);
        Refuse("if (a) function f() {}", ParseGoal.Module);
        Refuse("<!-- x", ParseGoal.Module);
        Refuse("{ import x from 'm'; }", ParseGoal.Module);
        Refuse("function f() { export var a; }", ParseGoal.Module);
    }

    [Fact]
    public void Top_level_await_follows_the_switch_and_the_fact_reads_only_the_top_level()
    {
        var off = Request("await x;", ParseGoal.Module);
        Refuse(new ParseRequest(off.Source, ParseGoal.Module, off.UnicodeTables, off.RegularExpressions) { Switches = ParseSwitches.For(ParseGoal.Module) with { TopLevelAwait = false } });
        Accept(new ParseRequest(new SourceText("var await = 1; await;"), ParseGoal.Module, off.UnicodeTables, off.RegularExpressions) { Switches = ParseSwitches.For(ParseGoal.Module) with { TopLevelAwait = false } });

        var script = new ParseRequest(new SourceText("for await (x of y); await using r = s;"), ParseGoal.Script, off.UnicodeTables, off.RegularExpressions) { Switches = ParseSwitches.For(ParseGoal.Script) with { TopLevelAwait = true } };
        Assert.True(Accept(script).HasTopLevelAwait);
        Refuse("await x;");
        Assert.False(Accept("async function f() { await x; } (async () => await y);", ParseGoal.Module).HasTopLevelAwait);
        Assert.True(Accept("class C { [await x] = 1; }", ParseGoal.Module).HasTopLevelAwait);
        Assert.False(Accept("class C { static { await x; } }", ParseGoal.Module).HasTopLevelAwait);
    }

    [Theory]
    [InlineData(DynamicFunctionKind.Normal, "a, b = 1, ...c", true)]
    [InlineData(DynamicFunctionKind.Normal, "a, b,", true)]
    [InlineData(DynamicFunctionKind.Normal, "...c, d", false)]
    [InlineData(DynamicFunctionKind.Normal, "a) { (", false)]
    [InlineData(DynamicFunctionKind.Normal, "a = yield", true)]
    [InlineData(DynamicFunctionKind.Generator, "a = yield", true)]
    [InlineData(DynamicFunctionKind.Async, "a = await b", true)]
    [InlineData(DynamicFunctionKind.Normal, "a = await b", false)]
    [InlineData(DynamicFunctionKind.Normal, "", true)]
    public void The_dynamic_function_s_parameters_are_a_FormalParameters_of_its_kind(DynamicFunctionKind kind, string text, bool accepted)
    {
        var request = new ParseRequest(new SourceText(text), ParseGoal.FormalParameters, Runners.UnicodeDataHome.Tables, NoValidator.Instance) { FunctionKind = kind };
        Assert.Equal(accepted ? ParseOutcome.Accepted : ParseOutcome.Refused, Parse(request).Outcome);
    }

    [Theory]
    [InlineData(DynamicFunctionKind.Normal, "return 1", true)]
    [InlineData(DynamicFunctionKind.Normal, "yield\n1", true)]
    [InlineData(DynamicFunctionKind.Generator, "yield 1; return", true)]
    [InlineData(DynamicFunctionKind.Generator, "var x = yield + 1;", true)]
    [InlineData(DynamicFunctionKind.Async, "await 1", true)]
    [InlineData(DynamicFunctionKind.Normal, "await 1", false)]
    [InlineData(DynamicFunctionKind.AsyncGenerator, "yield await 1", true)]
    [InlineData(DynamicFunctionKind.Normal, "}", false)]
    public void The_dynamic_function_s_body_is_a_FunctionBody_of_its_kind(DynamicFunctionKind kind, string text, bool accepted)
    {
        var request = new ParseRequest(new SourceText(text), ParseGoal.FunctionBody, Runners.UnicodeDataHome.Tables, NoValidator.Instance) { FunctionKind = kind };
        Assert.Equal(accepted ? ParseOutcome.Accepted : ParseOutcome.Refused, Parse(request).Outcome);
    }

    [Fact]
    public void The_dynamic_function_s_parameters_are_the_program_s()
    {
        var request = new ParseRequest(new SourceText("a, [b] = c, ...d"), ParseGoal.FormalParameters, Runners.UnicodeDataHome.Tables, NoValidator.Instance);
        var program = Accept(request);
        Assert.Empty(program.Body);
        Assert.Equal("(Program (BindingIdentifier a) (AssignmentPattern (ArrayPattern (BindingIdentifier b)) (Identifier c)) (RestElement (BindingIdentifier d)))", Tree.Dump(program));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Statements and declarations (clause 14).

    [Theory]
    [InlineData("{ a; b }", "(BlockStatement (ExpressionStatement (Identifier a)) (ExpressionStatement (Identifier b)))")]
    [InlineData(";", "(EmptyStatement)")]
    [InlineData("var a = 1, [b] = c, { d } = e;", "(VariableDeclaration Var (VariableDeclarator (BindingIdentifier a) (NumericLiteral 1)) (VariableDeclarator (ArrayPattern (BindingIdentifier b)) (Identifier c)) (VariableDeclarator (ObjectPattern (PatternProperty shorthand (BindingIdentifier d))) (Identifier e)))")]
    [InlineData("let a;", "(VariableDeclaration Let (VariableDeclarator (BindingIdentifier a)))")]
    [InlineData("const a = 1;", "(VariableDeclaration Const (VariableDeclarator (BindingIdentifier a) (NumericLiteral 1)))")]
    [InlineData("if (a) b; else c;", "(IfStatement (Identifier a) (ExpressionStatement (Identifier b)) (ExpressionStatement (Identifier c)))")]
    [InlineData("do a; while (b)", "(DoWhileStatement (ExpressionStatement (Identifier a)) (Identifier b))")]
    [InlineData("while (a) ;", "(WhileStatement (Identifier a) (EmptyStatement))")]
    [InlineData("for (;;) ;", "(ForStatement (EmptyStatement))")]
    [InlineData("for (let i = 0; i < n; i++) ;", "(ForStatement (VariableDeclaration Let (VariableDeclarator (BindingIdentifier i) (NumericLiteral 0))) (BinaryExpression LessThan (Identifier i) (Identifier n)) (UpdateExpression postfix Increment (Identifier i)) (EmptyStatement))")]
    [InlineData("for (a in b) ;", "(ForInStatement (Identifier a) (Identifier b) (EmptyStatement))")]
    [InlineData("for (const [a] of b) ;", "(ForOfStatement (VariableDeclaration Const (VariableDeclarator (ArrayPattern (BindingIdentifier a)))) (Identifier b) (EmptyStatement))")]
    [InlineData("for ([a, b] of c) ;", "(ForOfStatement (ArrayPattern (TargetPattern (Identifier a)) (TargetPattern (Identifier b))) (Identifier c) (EmptyStatement))")]
    [InlineData("a: for (;;) { continue a; break a; }", "(LabelledStatement (IdentifierName a) (ForStatement (BlockStatement (ContinueStatement (IdentifierName a)) (BreakStatement (IdentifierName a)))))")]
    [InlineData("with (a) b;", "(WithStatement (Identifier a) (ExpressionStatement (Identifier b)))")]
    [InlineData("switch (a) { case 1: b; default: c; case 2: }", "(SwitchStatement (Identifier a) (SwitchCase (NumericLiteral 1) (ExpressionStatement (Identifier b))) (SwitchCase (ExpressionStatement (Identifier c))) (SwitchCase (NumericLiteral 2)))")]
    [InlineData("throw a;", "(ThrowStatement (Identifier a))")]
    [InlineData("try {} catch {} finally {}", "(TryStatement (BlockStatement) (CatchClause (BlockStatement)) (BlockStatement))")]
    [InlineData("try {} catch ([e]) {}", "(TryStatement (BlockStatement) (CatchClause (ArrayPattern (BindingIdentifier e)) (BlockStatement)))")]
    [InlineData("debugger;", "(DebuggerStatement)")]
    [InlineData("function* g(a, ...b) { yield* a; }", "(FunctionDeclaration (Function generator (BindingIdentifier g) (BindingIdentifier a) (RestElement (BindingIdentifier b)) (ExpressionStatement (YieldExpression delegate (Identifier a)))))")]
    [InlineData("async function f() { await a; }", "(FunctionDeclaration (Function async (BindingIdentifier f) (ExpressionStatement (AwaitExpression (Identifier a)))))")]
    [InlineData("using a = b;", "(VariableDeclaration Using (VariableDeclarator (BindingIdentifier a) (Identifier b)))")]
    public void Each_statement_has_its_node(string text, string tree) => Assert.Equal(tree, First(text));

    [Theory]
    [InlineData("var [a];")]
    [InlineData("let {a};")]
    [InlineData("for (var [a];;);")]
    [InlineData("for (let a, b of c);")]
    [InlineData("for (let a = 1 of c);")]
    [InlineData("for (var [a] = 1 in c);")]
    [InlineData("switch (a) { default: default: }")]
    [InlineData("switch (a) { b; }")]
    [InlineData("try {}")]
    [InlineData("throw\na;")]
    [InlineData("if (a) let [b] = c;")]
    [InlineData("while (a) function f() {}")]
    [InlineData("if (a) class C {}")]
    [InlineData("L: function* g() {}")]
    [InlineData("L: async function f() {}")]
    [InlineData("do {}; while (a)")]
    [InlineData("for (let.a of b);")]
    [InlineData("for (async of b);")]
    [InlineData("for (a of b, c);")]
    [InlineData("for (a + b in c);")]
    public void A_statement_the_grammar_does_not_admit_is_refused(string text) => Refuse(text);

    [Theory]
    [InlineData("let\nx = 1", "(VariableDeclaration Let (VariableDeclarator (BindingIdentifier x) (NumericLiteral 1)))")]
    [InlineData("let = 1", "(ExpressionStatement (AssignmentExpression Assign (Identifier let) (NumericLiteral 1)))")]
    [InlineData("for (let in a);", "(ForInStatement (Identifier let) (Identifier a) (EmptyStatement))")]
    [InlineData("for (let of of a);", "(ForOfStatement (VariableDeclaration Let (VariableDeclarator (BindingIdentifier of))) (Identifier a) (EmptyStatement))")]
    [InlineData("for await (async of a);", null)]
    [InlineData("for (using of = null;;) break;", "(ForStatement (VariableDeclaration Using (VariableDeclarator (BindingIdentifier of) (NullLiteral))) (BreakStatement))")]
    [InlineData("for (using of a);", "(ForOfStatement (Identifier using) (Identifier a) (EmptyStatement))")]
    [InlineData("using\nx = 1", "(ExpressionStatement (Identifier using))")]
    [InlineData("async\nfunction f() {}", "(ExpressionStatement (Identifier async))")]
    [InlineData("yield: 1", "(LabelledStatement (IdentifierName yield) (ExpressionStatement (NumericLiteral 1)))")]
    [InlineData("L: function f() {}", "(LabelledStatement (IdentifierName L) (FunctionDeclaration (Function (BindingIdentifier f))))")]
    public void Contextual_words_begin_declarations_only_where_their_lookahead_says(string text, string? tree)
    {
        if (tree is null)
        {
            Accept(new ParseRequest(new SourceText("async function f() { " + text + " }"), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance));
            return;
        }

        Assert.Equal(tree, First(text));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Expressions (clause 13).

    [Theory]
    [InlineData("a - b - c", "(BinaryExpression Subtract (BinaryExpression Subtract (Identifier a) (Identifier b)) (Identifier c))")]
    [InlineData("a ** b ** c", "(BinaryExpression Exponent (Identifier a) (BinaryExpression Exponent (Identifier b) (Identifier c)))")]
    [InlineData("a + b * c", "(BinaryExpression Add (Identifier a) (BinaryExpression Multiply (Identifier b) (Identifier c)))")]
    [InlineData("a || b && c", "(LogicalExpression Or (Identifier a) (LogicalExpression And (Identifier b) (Identifier c)))")]
    [InlineData("a ?? (b || c)", "(LogicalExpression Coalesce (Identifier a) (ParenthesizedExpression (LogicalExpression Or (Identifier b) (Identifier c))))")]
    [InlineData("a ? b : c ? d : e", "(ConditionalExpression (Identifier a) (Identifier b) (ConditionalExpression (Identifier c) (Identifier d) (Identifier e)))")]
    [InlineData("a = b += c", "(AssignmentExpression Assign (Identifier a) (AssignmentExpression AddAssign (Identifier b) (Identifier c)))")]
    [InlineData("a, b", "(SequenceExpression (Identifier a) (Identifier b))")]
    [InlineData("!typeof -a", "(UnaryExpression LogicalNot (UnaryExpression Typeof (UnaryExpression Minus (Identifier a))))")]
    [InlineData("++a--", null)]
    [InlineData("(-a) ** 2", "(BinaryExpression Exponent (ParenthesizedExpression (UnaryExpression Minus (Identifier a))) (NumericLiteral 2))")]
    [InlineData("#x in o", null)]
    [InlineData("a?.b.c", "(ChainExpression (MemberExpression dot (MemberExpression dot optional (Identifier a) (IdentifierName b)) (IdentifierName c)))")]
    [InlineData("a?.[b]?.(c)", "(ChainExpression (CallExpression optional (MemberExpression computed optional (Identifier a) (Identifier b)) (Identifier c)))")]
    [InlineData("new a.b(c)", "(NewExpression (MemberExpression dot (Identifier a) (IdentifierName b)) (Identifier c))")]
    [InlineData("new new a()()", "(NewExpression (NewExpression (Identifier a)))")]
    [InlineData("new a()()", "(CallExpression (NewExpression (Identifier a)))")]
    [InlineData("a`x${b}y`", "(TaggedTemplateExpression (Identifier a) (TemplateLiteral (TemplateElement `x|x`) (Identifier b) (TemplateElement `y|y`)))")]
    [InlineData("a`\\unicode`", "(TaggedTemplateExpression (Identifier a) (TemplateLiteral (TemplateElement `<invalid>|\\unicode`)))")]
    [InlineData("`a${`b${c}`}`", "(TemplateLiteral (TemplateElement `a|a`) (TemplateLiteral (TemplateElement `b|b`) (Identifier c) (TemplateElement `|`)) (TemplateElement `|`))")]
    [InlineData("x = /a/g / b / c", "(AssignmentExpression Assign (Identifier x) (BinaryExpression Divide (BinaryExpression Divide (RegularExpressionLiteral /a/g) (Identifier b)) (Identifier c)))")]
    [InlineData("import.meta", "(MetaProperty (IdentifierName import) (IdentifierName meta))")]
    [InlineData("import(a, b,)", "(ImportCall Evaluation (Identifier a) (Identifier b))")]
    [InlineData("super.a(super[b])", "(CallExpression (MemberExpression dot (SuperExpression) (IdentifierName a)) (MemberExpression computed (SuperExpression) (Identifier b)))")]
    [InlineData("super(...a)", "(CallExpression (SuperExpression) (SpreadElement (Identifier a)))")]
    [InlineData("[a, , ...b,]", "(ArrayLiteral trailing (Identifier a) (SpreadElement (Identifier b)))")]
    [InlineData("({ a, b: c, [d]: e, f() {}, get g() {}, set g(v) {}, async *h() {}, ...i, 1: 2, 'j': 3, 4n: 5 })", null)]
    [InlineData("({ get, set, async, get: 1, async() {} })", null)]
    [InlineData("({ #m() {}, get #g() {}, async *#a() {} })", null)]
    [InlineData("x = function* () { yield\n1 }", null)]
    [InlineData("x = class extends (a, b) {}", null)]
    public void Each_expression_has_its_node(string text, string? tree)
    {
        var dump = First(text);
        if (tree is not null)
            Assert.Equal("(ExpressionStatement " + tree + ")", dump);
    }

    [Theory]
    [InlineData("-a ** 2")]
    [InlineData("typeof a ** 2")]
    [InlineData("a ?? b || c")]
    [InlineData("a || b ?? c")]
    [InlineData("a && b ?? c")]
    [InlineData("a + b = c")]
    [InlineData("a++ = b")]
    [InlineData("new a?.b")]
    [InlineData("new import(a)")]
    [InlineData("import(...a)")]
    [InlineData("import()")]
    [InlineData("import")]
    [InlineData("import.source(a)")]
    [InlineData("super")]
    [InlineData("super?.a")]
    [InlineData("new super()")]
    [InlineData("super.#a")]
    [InlineData("a.#b in c ? 1 : #d")]
    [InlineData("1 + #a in b")]
    [InlineData("a ~ b")]
    [InlineData("a\n++b c")]
    [InlineData("`${a`")]
    [InlineData("({ get *a() {} })")]
    [InlineData("({ async\nm() {} })")]
    [InlineData("({ if })")]
    [InlineData("({ #m: 1 })")]
    [InlineData("({ #m })")]
    [InlineData("({ 'a' })")]
    [InlineData("({ [a] })")]
    [InlineData("new.t\\u0061rget")]
    [InlineData("import.m\\u0065ta")]
    public void An_expression_the_grammar_does_not_admit_is_refused(string text) => Refuse(text);

    [Fact]
    public void Literals_keep_their_raw_facts()
    {
        var program = Accept("'\\1'; 0o17; 017; 08; 1_000n; 'use\\x20strict';");
        var literals = program.Body.Select(static s => ((ExpressionStatement)s).Expression).ToArray();
        Assert.Equal(StringLiteralFlags.LegacyOctalEscape | StringLiteralFlags.HasEscape, ((StringLiteral)literals[0]).Flags);
        Assert.Equal((15d, NumericLiteralFlags.None, "0o17"), (((NumericLiteral)literals[1]).Value, ((NumericLiteral)literals[1]).Flags, ((NumericLiteral)literals[1]).Text));
        Assert.Equal(NumericLiteralFlags.LegacyOctal, ((NumericLiteral)literals[2]).Flags);
        Assert.Equal(NumericLiteralFlags.NonOctalDecimal, ((NumericLiteral)literals[3]).Flags);
        Assert.Equal(("1_000n", NumericLiteralFlags.BigInt), (((NumericLiteral)literals[4]).Text, ((NumericLiteral)literals[4]).Flags));
        Assert.True(double.IsNaN(((NumericLiteral)literals[4]).Value));
        Assert.Equal("use strict", ((StringLiteral)literals[5]).Value);

        var regex = (RegularExpressionLiteral)((ExpressionStatement)Accept("/[/]\\//gu").Body[0]).Expression;
        Assert.Equal(("[/]\\/", "gu"), (regex.Pattern, regex.Flags));
    }

    [Fact]
    public void A_directive_keeps_its_raw_text_and_only_an_exact_use_strict_is_one()
    {
        var program = Accept("'a'; \"use strict\"; 'b'\n'c' + 1; 'd';");
        var directives = program.Body.Select(static s => (s as ExpressionStatement)?.Directive).ToArray();
        Assert.Equal(["a", "use strict", "b", null, null], directives);

        // A Use Strict Directive makes the rest of the code strict: Annex B's function in an if clause is gone.
        Refuse("'use strict'; if (a) function f() {}");
        Accept("'use\\x20strict'; if (a) function f() {}");
        Accept("('use strict'); if (a) function f() {}");
        Refuse("function g() { 'use strict'; if (a) function f() {} }");
        Accept("function g() { 'use strict'; } if (a) function f() {}");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Cover grammars, converted and never parsed again (clauses 13.2.10, 13.3.6, 13.15.5 and 15.3).

    [Theory]
    [InlineData("a => a", "(Function arrow (BindingIdentifier a) (Identifier a))")]
    [InlineData("() => {}", "(Function arrow)")]
    [InlineData("(a, b,) => 1", "(Function arrow (BindingIdentifier a) (BindingIdentifier b) (NumericLiteral 1))")]
    [InlineData("(a = 1, [b], {c: d}, ...e) => 0", "(Function arrow (AssignmentPattern (BindingIdentifier a) (NumericLiteral 1)) (ArrayPattern (BindingIdentifier b)) (ObjectPattern (PatternProperty (IdentifierName c) (BindingIdentifier d))) (RestElement (BindingIdentifier e)) (NumericLiteral 0))")]
    [InlineData("({a = 1, b: [c = 2] = []}) => 0", "(Function arrow (ObjectPattern (PatternProperty shorthand (AssignmentPattern (BindingIdentifier a) (NumericLiteral 1))) (PatternProperty (IdentifierName b) (AssignmentPattern (ArrayPattern (AssignmentPattern (BindingIdentifier c) (NumericLiteral 2))) (ArrayLiteral)))) (NumericLiteral 0))")]
    [InlineData("async a => 1", "(Function async arrow (BindingIdentifier a) (NumericLiteral 1))")]
    [InlineData("async (a, ...b) => 1", "(Function async arrow (BindingIdentifier a) (RestElement (BindingIdentifier b)) (NumericLiteral 1))")]
    [InlineData("async () => await 1", "(Function async arrow (AwaitExpression (NumericLiteral 1)))")]
    [InlineData("async => async", "(Function arrow (BindingIdentifier async) (Identifier async))")]
    public void An_arrow_function_s_parameters_are_converted_from_its_cover(string text, string function) =>
        Assert.Equal("(ExpressionStatement (ArrowFunctionExpression " + function + "))", First(text));

    [Fact]
    public void A_cover_that_is_no_arrow_is_a_parenthesised_expression_or_a_call()
    {
        Assert.Equal("(ExpressionStatement (ParenthesizedExpression (SequenceExpression (Identifier a) (Identifier b))))", First("(a, b)"));
        Assert.Equal("(ExpressionStatement (CallExpression (Identifier async) (Identifier a) (SpreadElement (Identifier b))))", First("async(a, ...b)"));
        Assert.Equal("(ExpressionStatement (CallExpression (Identifier async) (Identifier a)))", First("async\n(a)"));
    }

    [Theory]
    [InlineData("(a + b) => 1")]
    [InlineData("((a)) => 1")]
    [InlineData("(a.b) => 1")]
    [InlineData("(...a,) => 1")]
    [InlineData("(...a = 1) => 1")]
    [InlineData("(a, ...b, c) => 1")]
    [InlineData("()")]
    [InlineData("(a,)")]
    [InlineData("(...a)")]
    [InlineData("() + 1")]
    [InlineData("a + () => 1")]
    [InlineData("a || b => 1")]
    [InlineData("(a)\n=> 1")]
    [InlineData("async (a)\n=> 1")]
    [InlineData("async a\n=> 1")]
    [InlineData("\\u0061sync (a) => 1")]
    [InlineData("async (...a,) => 1")]
    [InlineData("async (a = 1, ...{b}, c) => 1")]
    [InlineData("({a: 1}) => 1")]
    [InlineData("({...{a}}) => 1")]
    [InlineData("([...a = 1]) => 1")]
    [InlineData("(x => {}).y => 1")]
    [InlineData("x => {}.y")]
    [InlineData("x => {} + 1")]
    [InlineData("async x => {} ? a : b")]
    [InlineData("new (a) => 1")]
    [InlineData("switch => 1")]
    public void A_cover_that_does_not_cover_its_conversion_is_refused(string text) => Refuse(text);

    [Theory]
    [InlineData("[a, [b], {c}, ...d] = e", "(ArrayPattern (TargetPattern (Identifier a)) (ArrayPattern (TargetPattern (Identifier b))) (ObjectPattern (PatternProperty shorthand (TargetPattern (Identifier c)))) (RestElement (TargetPattern (Identifier d))))")]
    [InlineData("({a = 1, b: c.d, [e]: f = 2, ...g} = h)", "(ObjectPattern (PatternProperty shorthand (AssignmentPattern (TargetPattern (Identifier a)) (NumericLiteral 1))) (PatternProperty (IdentifierName b) (TargetPattern (MemberExpression dot (Identifier c) (IdentifierName d)))) (PatternProperty (ComputedPropertyName (Identifier e)) (AssignmentPattern (TargetPattern (Identifier f)) (NumericLiteral 2))) (RestElement (TargetPattern (Identifier g))))")]
    [InlineData("[[a] = [1]] = b", "(ArrayPattern (AssignmentPattern (ArrayPattern (TargetPattern (Identifier a))) (ArrayLiteral (NumericLiteral 1))))")]
    [InlineData("[...[a]] = b", "(ArrayPattern (RestElement (ArrayPattern (TargetPattern (Identifier a)))))")]
    [InlineData("[(a)] = b", "(ArrayPattern (TargetPattern (ParenthesizedExpression (Identifier a))))")]
    public void An_assignment_s_literal_target_is_converted_to_a_pattern(string text, string target)
    {
        var dump = First(text.StartsWith('(') ? text : "(" + text + ")");
        Assert.Contains("(AssignmentExpression Assign " + target, dump, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("({a = 1})")]
    [InlineData("[{a = 1}]")]
    [InlineData("f({a = 1})")]
    [InlineData("({a = 1}).b = 1")]
    [InlineData("[{a = 1}.b] = c")]
    [InlineData("({a = 1}) = b")]
    [InlineData("({a: {b = 1}})")]
    [InlineData("({...{a}} = b)")]
    [InlineData("({...[a]} = b)")]
    [InlineData("[...a,] = b")]
    [InlineData("[...a, b] = c")]
    [InlineData("[a + b] = c")]
    [InlineData("[a += 1] = c")]
    [InlineData("({a() {}} = b)")]
    [InlineData("({...a, b} = c)")]
    [InlineData("for ({a = 1};;);")]
    [InlineData("async ({a = 1})")]
    public void A_CoverInitializedName_or_a_target_the_pattern_does_not_admit_is_refused(string text) => Refuse(text);

    [Fact]
    public void A_CoverInitializedName_converted_anywhere_is_accepted()
    {
        Accept("({a = 1} = b); [{a = 1}] = b; ({a: {b = 1}} = c); for ({a = 1} of b); ({a = 1}) => 1; async ({a = 1}) => 1; [...{a = 1}] = b;");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Automatic semicolon insertion (clause 12.10; rows P16 and P16b; the restricted productions).

    [Theory]
    [InlineData("a\nb", ParseGoal.Script, 2)]
    [InlineData("a\n++b", ParseGoal.Script, 2)]
    [InlineData("return\na", ParseGoal.FunctionBody, 2)]
    [InlineData("x = a\n(b)", ParseGoal.Script, 1)]
    [InlineData("x = a\n/b/g", ParseGoal.Script, 1)]
    [InlineData("{ 1 2 } 3", ParseGoal.Script, -1)]
    [InlineData("{ 1\n2 } 3", ParseGoal.Script, 2)]
    [InlineData("do {} while (a) b", ParseGoal.Script, 2)]
    [InlineData("var a = 1\nvar b", ParseGoal.Script, 2)]
    [InlineData("a\n?.b", ParseGoal.Script, 1)]
    [InlineData("for (a; b\n;) ;", ParseGoal.Script, 1)]
    [InlineData("for (var i = 0\ni < 1; i++) ;", ParseGoal.Script, -1)]
    [InlineData("for (i = 0;\ni < 1\ni++) ;", ParseGoal.Script, -1)]
    [InlineData("for (x\nof y) ;", ParseGoal.Script, 1)]
    [InlineData("if (a)\nelse b", ParseGoal.Script, -1)]
    [InlineData("a\n=> 1", ParseGoal.Script, -1)]
    [InlineData("foo: for (;;) { continue\nfoo }", ParseGoal.Script, 1)]
    [InlineData("throw\n1", ParseGoal.Script, -1)]
    [InlineData("class C { x\ny }", ParseGoal.Script, 1)]
    [InlineData("class C { x y }", ParseGoal.Script, -1)]
    [InlineData("class C { get\n*a() {} }", ParseGoal.Script, 1)]
    [InlineData("class C { static\nx }", ParseGoal.Script, 1)]
    [InlineData("class C { async\nm() {} }", ParseGoal.Script, 1)]
    [InlineData("x\n++\ny", ParseGoal.Script, 2)]
    [InlineData("a = b\n++c", ParseGoal.Script, 2)]
    public void A_semicolon_is_inserted_where_clause_12_10_says_and_nowhere_else(string text, ParseGoal goal, int statements)
    {
        if (statements < 0)
        {
            Refuse(text, goal);
            return;
        }

        Assert.Equal(statements, Accept(text, goal).Body.Count);
    }

    [Fact]
    public void A_class_field_ends_at_a_line_break()
    {
        var @class = ((ClassDeclaration)Accept("class C { get\n*a() {}\nstatic\nx\nasync\nm() {} }").Body[0]).Class;
        Assert.Equal(["(FieldDefinition (IdentifierName get))", "(MethodDefinition Method (IdentifierName a) (Function generator))",
            "(FieldDefinition static (IdentifierName x))", "(FieldDefinition (IdentifierName async))", "(MethodDefinition Method (IdentifierName m) (Function))"],
            @class.Elements.Select(Tree.Dump).ToArray());
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The lexical goal the parser names (clause 12; rows T24 and decision D-t24).

    [Theory]
    [InlineData("{}\n/foo/", 2, 1)]
    [InlineData("x = {}\n/foo/g", 1, 0)]
    [InlineData("function f() {}\n/foo/", 2, 1)]
    [InlineData("x = function f() {}\n/foo/g", 1, 0)]
    [InlineData("if (a) /foo/", 1, 1)]
    [InlineData("a\n/foo/g", 1, 0)]
    [InlineData("++/foo/.lastIndex", 1, 1)]
    [InlineData("await /foo/g", 1, 0)]
    [InlineData("x = y => /foo/", 1, 1)]
    [InlineData("x = (y) / 2 / z", 1, 0)]
    [InlineData("x = a ? /b/ : /c/", 1, 2)]
    [InlineData("x = [/a/, {b: /c/}, `${/d/}`]", 1, 3)]
    public void A_slash_begins_a_literal_where_an_expression_may_begin_and_divides_after_one(string text, int statements, int literals)
    {
        var result = Parse(text);
        Assert.Equal(statements, Accept(text).Body.Count);
        Assert.Equal(literals, result.Tokens.Count(static t => t.Kind == TokenKind.RegularExpressionLiteral));
    }

    [Fact]
    public void A_regular_expression_literal_after_a_for_await_head_and_in_a_generator_is_read_as_one()
    {
        var request = Request("async function* f() { for await (x of y) /a/; yield /b/; }");
        var tokens = Parse(request).Tokens.Where(static t => t.Kind == TokenKind.RegularExpressionLiteral).Count();
        Assert.Equal(2, tokens);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Classes (clause 15.7).

    [Fact]
    public void A_class_has_fields_private_names_static_blocks_and_accessors()
    {
        var @class = ((ClassDeclaration)Accept("class C extends B { #a = 1; static b; static { this.#a; } get c() {} set c(v) {} static async *[d]() {} #e() { return #a in this; } constructor() { super(); } 'f'; 1() {} static() {} get() {} }").Body[0]).Class;
        Assert.Equal("(Identifier B)", Tree.Dump(@class.Heritage!));
        Assert.Equal([SyntaxKind.FieldDefinition, SyntaxKind.FieldDefinition, SyntaxKind.StaticBlock, SyntaxKind.MethodDefinition, SyntaxKind.MethodDefinition,
            SyntaxKind.MethodDefinition, SyntaxKind.MethodDefinition, SyntaxKind.MethodDefinition, SyntaxKind.FieldDefinition, SyntaxKind.MethodDefinition,
            SyntaxKind.MethodDefinition, SyntaxKind.MethodDefinition], @class.Elements.Select(static e => e.Kind).ToArray());
        Assert.Equal("(PrivateIdentifier #a)", Tree.Dump(((FieldDefinition)@class.Elements[0]).Key));
        var generator = (MethodDefinition)@class.Elements[5];
        Assert.True(generator.IsStatic && generator.Function.IsAsync && generator.Function.IsGenerator && generator.Key is ComputedPropertyName);
        Assert.Equal("(MethodDefinition Method (IdentifierName static) (Function))", Tree.Dump(@class.Elements[10]));
    }

    [Theory]
    [InlineData("class C { x = await; }", true)]
    [InlineData("async function f() { class C { x = await; } }", true)]
    [InlineData("async function f() { class C { x = await 1; } }", false)]
    [InlineData("async function f() { class C { [await 1] = 1; } }", true)]
    [InlineData("class C { static { await 1; } }", true)]
    [InlineData("class C { static { return; } }", false)]
    [InlineData("function f() { class C { static { return; } } }", false)]
    [InlineData("class C { accessor x = 1; }", false)]
    [InlineData("class C { accessor\nx; }", true)]
    [InlineData("@d class C {}", false)]
    [InlineData("class C extends a + b {}", false)]
    [InlineData("class C { get x(a) {} }", false)]
    [InlineData("class C { set x() {} }", false)]
    [InlineData("class C { set x(...a) {} }", false)]
    [InlineData("class { }", false)]
    public void A_class_element_is_read_as_the_grammar_reads_it(string text, bool accepted)
    {
        if (accepted)
            Accept(text);
        else
            Refuse(text);
    }

    [Fact]
    public void Decorators_and_auto_accessors_are_read_under_their_flags()
    {
        var request = new ParseRequest(new SourceText("@a @b.c @d(1) @(e) class C { @f accessor x = 1; @g static m() {} }"), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Proposals = ProposalFlags.Decorators | ProposalFlags.AutoAccessors,
        };
        var @class = ((ClassDeclaration)Accept(request).Body[0]).Class;
        Assert.Equal(4, @class.Decorators.Count);
        Assert.True(((FieldDefinition)@class.Elements[0]).IsAccessor);
        Assert.Single(((MethodDefinition)@class.Elements[1]).Decorators);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Modules (clause 16.2).

    [Theory]
    [InlineData("import 'm';")]
    [InlineData("import a from 'm';")]
    [InlineData("import * as ns from 'm';")]
    [InlineData("import a, * as ns from 'm';")]
    [InlineData("import a, { b } from 'm';")]
    [InlineData("import { } from 'm';")]
    [InlineData("import { a as b, 'c d' as e, default as f, } from 'm' with { type: 'json', 'k': 'v', _k: 'w', };")]
    [InlineData("import a from 'm' with {\n'type': 'json'\n}")]
    [InlineData("export * from 'm';")]
    [InlineData("export * as ns from 'm';")]
    [InlineData("export * as 'n s' from 'm' with { type: 'json' };")]
    [InlineData("export { a, b as c, d as 'e' };")]
    [InlineData("export { 'a' as b } from 'm';")]
    [InlineData("export var a = 1;")]
    [InlineData("export let a, b;")]
    [InlineData("export const a = 1;")]
    [InlineData("export function f() {}")]
    [InlineData("export async function f() {}")]
    [InlineData("export class C {}")]
    [InlineData("export default function () {}")]
    [InlineData("export default async function f() {}")]
    [InlineData("export default class {}")]
    [InlineData("export default a + b;")]
    [InlineData("export default async\nfunction f() {}")]
    [InlineData("import source from 'm';")]
    [InlineData("import defer from 'm';")]
    public void Each_import_and_export_form_is_read(string text) => Accept(text, ParseGoal.Module);

    [Theory]
    [InlineData("import { if } from 'm';")]
    [InlineData("import { 'a' } from 'm';")]
    [InlineData("import a, from 'm';")]
    [InlineData("import a * as b from 'm';")]
    [InlineData("import a from 'm' assert { type: 'json' };")]
    [InlineData("import a from m;")]
    [InlineData("import { a } from 'm' with { type: json };")]
    [InlineData("export * as ns;")]
    [InlineData("export default var a;")]
    [InlineData("export a;")]
    [InlineData("import source x from 'm';")]
    [InlineData("import defer * as ns from 'm';")]
    [InlineData("import.defer('m');")]
    public void An_import_or_export_the_grammar_does_not_admit_is_refused(string text) => Refuse(text, ParseGoal.Module);

    [Fact]
    public void The_import_phases_are_read_one_flag_per_form()
    {
        ParseRequest With(string text, ProposalFlags flags) =>
            new(new SourceText(text), ParseGoal.Module, Runners.UnicodeDataHome.Tables, NoValidator.Instance) { Proposals = flags };

        Assert.Equal(ImportPhase.Source, ((ImportDeclaration)Accept(With("import source x from 'm';", ProposalFlags.ImportSourceDeclarations)).Body[0]).Phase);
        Assert.Equal(ImportPhase.Source, ((ImportDeclaration)Accept(With("import source from from 'm';", ProposalFlags.ImportSourceDeclarations)).Body[0]).Phase);
        Assert.Equal(ImportPhase.Evaluation, ((ImportDeclaration)Accept(With("import source from 'm';", ProposalFlags.ImportSourceDeclarations)).Body[0]).Phase);
        Assert.Equal(ImportPhase.Defer, ((ImportDeclaration)Accept(With("import defer * as ns from 'm';", ProposalFlags.ImportDeferDeclarations)).Body[0]).Phase);
        Refuse(With("import defer x from 'm';", ProposalFlags.ImportDeferDeclarations));
        Refuse(With("import source x from 'm';", ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferCalls | ProposalFlags.ImportDeferDeclarations));
        Assert.Equal("(ExpressionStatement (ImportCall Defer (StringLiteral 'm')))", Tree.Dump(Accept(With("import.defer('m');", ProposalFlags.ImportDeferCalls)).Body[0]));
        Refuse(With("import.defer('m', b);", ProposalFlags.ImportDeferCalls));
        Refuse(With("new import.source('m');", ProposalFlags.ImportSourceCalls));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Annex B and the switches (decisions D10 and D17).

    [Fact]
    public void Annex_B_s_productions_hold_in_sloppy_code_with_the_switch_on_only()
    {
        Assert.Equal("(IfStatement (Identifier a) (FunctionDeclaration (Function (BindingIdentifier f))) (FunctionDeclaration (Function (BindingIdentifier g))))", First("if (a) function f() {} else function g() {}"));
        Accept("for (var a = 1 in b);");
        Refuse("if (a) function* f() {}");
        Refuse("if (a) async function f() {}");
        Refuse("for (let a = 1 in b);");
        Refuse("for (var [a] = 1 in b);");

        ParseRequest With(string text, bool strict, bool annexB) => new(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Script) with { ForcedStrict = strict, AnnexB = annexB },
        };

        foreach (var text in new[] { "if (a) function f() {}", "for (var a = 1 in b);" })
        {
            Accept(With(text, strict: false, annexB: true));
            Refuse(With(text, strict: true, annexB: true));
            Refuse(With(text, strict: false, annexB: false));
        }

        Refuse(With("class C { m() { if (a) function f() {} } }", strict: false, annexB: true));
    }

    [Fact]
    public void The_m_suffix_reaches_the_tree_only_through_the_extension()
    {
        Refuse("1m");
        var request = new ParseRequest(new SourceText("1.5m"), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance) { Extensions = LexerExtensions.DecimalSuffix };
        var literal = (NumericLiteral)((ExpressionStatement)Accept(request).Body[0]).Expression;
        Assert.Equal(("1.5m", NumericLiteralFlags.DecimalSuffix), (literal.Text, literal.Flags));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The correspondence's rows P (Broiler.VM's bundle sjl-13-001), each answered as the specification answers it.

    [Theory]
    [InlineData("P01", "'use strict'; var a;", ParseGoal.Script, true)]
    [InlineData("P02", "using a = b; await using c = d;", ParseGoal.Module, true)]
    [InlineData("P03", "var a = 1; a", ParseGoal.Eval, true)]
    [InlineData("P05a", "import a from 'm' with { _k: 'v', $k: 'w', \\u0061: 'x' };", ParseGoal.Module, true)]
    [InlineData("P05c", "import a from 'm' with {\n'type': 'json'\n};", ParseGoal.Module, true)]
    [InlineData("P05d", "import a from 'm' assert { type: 'json' };", ParseGoal.Module, false)]
    [InlineData("P07", "new import(a)", ParseGoal.Script, false)]
    [InlineData("P07", "import(...a)", ParseGoal.Script, false)]
    [InlineData("P09", "({ async\nm() {} })", ParseGoal.Script, false)]
    [InlineData("P09", "function f() { let\nawait 0; }", ParseGoal.Script, false)]
    [InlineData("P10", "do {}; while (false)", ParseGoal.Script, false)]
    [InlineData("P11", "while (a) const b = 1;", ParseGoal.Script, false)]
    [InlineData("P11", "L: async function f() {}", ParseGoal.Script, false)]
    [InlineData("P12", "switch (a) { default: default: }", ParseGoal.Script, false)]
    [InlineData("P12", "switch (a) { b; case 1: }", ParseGoal.Script, false)]
    [InlineData("P13", "a & f ~ 0", ParseGoal.Script, false)]
    [InlineData("P14", "switch => 1", ParseGoal.Script, false)]
    [InlineData("P15a", "[...a, b] = c", ParseGoal.Script, false)]
    [InlineData("P15a", "[...a = 1] = c", ParseGoal.Script, false)]
    [InlineData("P15a", "[...a,] = c", ParseGoal.Script, false)]
    [InlineData("P15a", "({...a, b} = c)", ParseGoal.Script, false)]
    [InlineData("P16", "a\nb", ParseGoal.Script, true)]
    [InlineData("P16b", "for (var i = 0\ni < 1; i++) ;", ParseGoal.Script, false)]
    [InlineData("P16b", "for (let i = 0\ni < 1; i++) ;", ParseGoal.Script, false)]
    [InlineData("P16b", "for (i = 0; i < 1\ni++) ;", ParseGoal.Script, false)]
    [InlineData("P17", "a`${b}${c}`; `x${y}z`", ParseGoal.Script, true)]
    [InlineData("P18", "class C { #x; m() { super.#x; } }", ParseGoal.Script, false)]
    [InlineData("P18", "class C { #x; m() { for (#x in a); } }", ParseGoal.Script, false)]
    [InlineData("P18", "class C extends a.b() {}", ParseGoal.Script, true)]
    [InlineData("P19", "@d class C {}", ParseGoal.Script, false)]
    [InlineData("P20", "class C { accessor x; }", ParseGoal.Script, false)]
    [InlineData("P21", "{ using a = b, [c] = d; }", ParseGoal.Script, false)]
    [InlineData("P21", "if (a) using b = c;", ParseGoal.Script, false)]
    [InlineData("P22", "async function f() { await: 1; }", ParseGoal.Script, false)]
    [InlineData("P22", "(debugger)", ParseGoal.Script, false)]
    [InlineData("P22", "case", ParseGoal.Script, false)]
    [InlineData("P23", "function f(...a, b) {}", ParseGoal.Script, false)]
    [InlineData("P23", "function f(...a,) {}", ParseGoal.Script, false)]
    [InlineData("P24", "function f() { class C { static { return; } } }", ParseGoal.Script, false)]
    [InlineData("P31", "({...{a}} = o)", ParseGoal.Script, false)]
    [InlineData("P31", "({...[a]} = o)", ParseGoal.Script, false)]
    [InlineData("P31", "[...[a]] = []", ParseGoal.Script, true)]
    [InlineData("P35", "0++;", ParseGoal.Script, true)]
    [InlineData("D40", "import { if } from 'm';", ParseGoal.Module, false)]
    [InlineData("EX0010", "export var a;", ParseGoal.Script, false)]
    [InlineData("EX0032", "return;", ParseGoal.Script, false)]
    public void Each_P_row_is_answered_by_the_grammar(string row, string text, ParseGoal goal, bool accepted)
    {
        var result = Parse(text, goal);
        Assert.True((result.Outcome == ParseOutcome.Accepted) == accepted, row + ": " + text);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Spans (decision D15): full, nested, in order, the source's base applied.

    [Theory]
    [InlineData("var a = [1, {b: c}, ...d], e = (f, g) => f`x${g}y`;\nclass C extends D { static #p = 1; m() { return super.m?.(this.#p); } }")]
    [InlineData("for await (const [a, {b = 1}] of c) { label: if (a) b; else { c?.d; } }")]
    [InlineData("import a, { b as c } from 'm' with { type: 'json' };\nexport default async function* () { yield* [await a]; }")]
    [InlineData("x = function f(a, [b], {c}, ...d) { 'use strict'; switch (a) { case 1: break; default: } try {} catch ({e}) {} finally {} };")]
    public void Every_node_s_span_lies_in_its_parent_s_after_its_preceding_sibling_and_names_its_text(string text)
    {
        var goal = text.StartsWith("import", StringComparison.Ordinal) || text.StartsWith("for await", StringComparison.Ordinal) ? ParseGoal.Module : ParseGoal.Script;
        var program = Accept(text, goal);
        Assert.Equal((0, text.Length, 1, 1), (program.Span.Offset, program.Span.Length, program.Span.Line, program.Span.Column));
        var lastEnd = new Dictionary<SyntaxNode, int>(ReferenceEqualityComparer.Instance);
        foreach (var (node, parent) in Tree.Walk(program))
        {
            var span = node.Span;
            Assert.True(span.Length > 0 || node is TemplateElement, node.Kind + " is empty");
            Assert.True(span.Offset + span.Length <= text.Length, node.Kind + " runs past the text");
            Assert.False(char.IsWhiteSpace(text[span.Offset]), node.Kind + " starts on white space");
            Assert.False(char.IsWhiteSpace(text[span.Offset + span.Length - 1]), node.Kind + " ends on white space");
            var lineStart = text.LastIndexOf('\n', Math.Max(0, span.Offset - 1));
            var line = 1 + text[..span.Offset].Count(static c => c == '\n');
            Assert.Equal((line, span.Offset - (span.Offset == 0 || text[span.Offset - 1] == '\n' ? span.Offset : lineStart + 1) + 1), (span.Line, span.Column));
            if (parent is not null)
            {
                Assert.True(span.Offset >= parent.Span.Offset && span.Offset + span.Length <= parent.Span.Offset + parent.Span.Length, node.Kind + " outside " + parent.Kind);
                // A shorthand's key and value, and a method's name and function, share their text.
                if (lastEnd.TryGetValue(parent, out var previousEnd) && parent is not PatternProperty and not PropertyDefinition and not MethodDefinition and not ImportSpecifier and not ExportSpecifier)
                    Assert.True(span.Offset >= previousEnd, node.Kind + " overlaps its sibling in " + parent.Kind);
                lastEnd[parent] = span.Offset + span.Length;
            }

            switch (node)
            {
                case Identifier n:
                    Assert.Equal(n.Name, text.Substring(span.Offset, span.Length));
                    break;
                case BindingIdentifier n:
                    Assert.Equal(n.Name, text.Substring(span.Offset, span.Length));
                    break;
                case NumericLiteral n:
                    Assert.Equal(n.Text, text.Substring(span.Offset, span.Length));
                    break;
                case ParenthesizedExpression:
                    Assert.Equal('(', text[span.Offset]);
                    Assert.Equal(')', text[span.Offset + span.Length - 1]);
                    break;
            }
        }
    }

    [Fact]
    public void A_span_counts_UTF_16_code_units_and_takes_the_source_s_base()
    {
        var program = Accept(new ParseRequest(new SourceText("'\U0001F600';\r\n  a", "s.js", baseOffset: 100, baseLine: 10, baseColumn: 5), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance));
        var first = program.Body[0].Span;
        var second = ((ExpressionStatement)program.Body[1]).Expression.Span;
        Assert.Equal((100, 5, 10, 5), (first.Offset, first.Length, first.Line, first.Column));
        Assert.Equal((109, 1, 11, 3), (second.Offset, second.Length, second.Line, second.Column));
        Assert.Equal((100, 10, 10, 5), (program.Span.Offset, program.Span.Length, program.Span.Line, program.Span.Column));
        var refused = Refuse(new ParseRequest(new SourceText("\n  )", "s.js", baseOffset: 7, baseLine: 3, baseColumn: 9), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance));
        Assert.Equal((10, 4, 3, "s.js"), (refused.Span.Offset, refused.Span.Line, refused.Span.Column, refused.SourceName));
    }

    [Fact]
    public void The_tree_is_immutable_and_two_parses_give_equal_trees()
    {
        const string text = "class C { #a; static { [a, b] = [b, a]; } } for (const x of y) async (z = 1) => z;";
        Assert.Equal(Tree.Dump(Accept(text)), Tree.Dump(Accept(text)));
        foreach (var type in typeof(SyntaxNode).Assembly.GetExportedTypes().Where(static t => typeof(SyntaxNode).IsAssignableFrom(t)))
        {
            Assert.All(type.GetProperties(), p => Assert.True(p.SetMethod is null || !p.SetMethod.IsPublic, type.Name + "." + p.Name + " is settable"));
            Assert.All(type.GetConstructors(), c => Assert.False(c.IsPublic, type.Name + " has a public constructor"));
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The limits, checked before the stack is (decision D14; JSD-0080's decision D27).

    /// <summary>A thread with a stack large enough for the supported maximum nesting: stage 4e derives and states the stack the library requires.</summary>
    private static T OnLargeStack<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception e)
            {
                failure = e;
            }
        }, 256 * 1024 * 1024);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw failure;
        return result;
    }

    private static ParseRequest Nesting(string text, int depth) => new(new SourceText(text), ParseGoal.Script, Runners.UnicodeDataHome.Tables, NoValidator.Instance)
    {
        Limits = new ParseLimits { NestingDepth = new DepthLimit(depth) },
    };

    [Theory]
    [InlineData("", "(", "1", ")", "")]
    [InlineData("", "[", "1", "]", "")]
    [InlineData("", "{", "", "}", "")]
    [InlineData("", "a = ", "1", "", "")]
    [InlineData("", "!", "a", "", "")]
    [InlineData("", "new ", "a", "", "")]
    [InlineData("", "f(", "1", ")", "")]
    [InlineData("", "`${", "1", "}`", "")]
    [InlineData("", "function f() {", "", "}", "")]
    [InlineData("var ", "[", "a", "]", " = a")]
    [InlineData("", "2 ** ", "2", "", "")]
    [InlineData("", "x => ", "x", "", "")]
    [InlineData("", "a ? b : ", "c", "", "")]
    public void A_source_nested_past_the_bound_is_refused_with_the_nesting_diagnostic(string prefix, string open, string inner, string close, string suffix)
    {
        string Text(int depth) => prefix + string.Concat(Enumerable.Repeat(open, depth)) + inner + string.Concat(Enumerable.Repeat(close, depth)) + suffix;
        foreach (var bound in new[] { 16, ParseLimits.MaximumSupportedNestingDepth })
        {
            var refused = OnLargeStack(() => Parse(Nesting(Text(bound + 1), bound)));
            Assert.Equal(ParseOutcome.Refused, refused.Outcome);
            Assert.Equal(("JSE9201", "limit.nesting.too-deep", bound.ToString(System.Globalization.CultureInfo.InvariantCulture)), (refused.Diagnostics[0].Code, refused.Diagnostics[0].MessageId, refused.Diagnostics[0].Arguments[0]));
            Assert.Equal(ParseOutcome.Accepted, OnLargeStack(() => Parse(Nesting(Text(bound / 4), bound))).Outcome);
        }
    }

    [Fact]
    public void The_nesting_bound_is_charged_as_Broiler_VM_s_parser_charges_it()
    {
        // Each statement and each assignment expression is one level: a statement holding a parenthesised assignment is three.
        Assert.Equal(ParseOutcome.Accepted, Parse(Nesting("(a = 1)", 4)).Outcome);
        Assert.Equal(ParseOutcome.Refused, Parse(Nesting("(a = 1)", 3)).Outcome);
        Assert.Equal(ParseOutcome.Accepted, Parse(Nesting("a + b * c - d", 2)).Outcome);
    }

    [Theory]
    [InlineData("a", "+a", "", "+a", "")]
    [InlineData("(a", "+a", ")", "+b", "")]
    [InlineData("`${(a", "+a", ")", "+b", "}`")]
    [InlineData("`${a", "+a", "}`", "+b", "")]
    [InlineData("o", ".a", "", "+b", "")]
    [InlineData("(a", "+a", ")", ".b", "")]
    [InlineData("f", "()", "", "+b", "")]
    public void A_tree_deeper_than_the_constant_is_refused_and_a_left_operand_s_spine_is_charged_to_its_chain(string prefix, string inner, string middle, string outer, string suffix)
    {
        // Half the constant in the left operand and half in the chain it begins (decision D14's four shapes and the plain
        // chain): each half alone is within the bound, the two together past it.
        string Text(int count) => prefix + string.Concat(Enumerable.Repeat(inner, count)) + middle + string.Concat(Enumerable.Repeat(outer, count)) + suffix;
        var refused = OnLargeStack(() => Parse(Text(ParseLimits.MaximumTreeDepth / 2 + 1)));
        Assert.Equal(ParseOutcome.Refused, refused.Outcome);
        Assert.Equal(("JSE9202", "limit.tree.too-deep"), (refused.Diagnostics[0].Code, refused.Diagnostics[0].MessageId));
        Assert.Equal(ParseOutcome.Accepted, OnLargeStack(() => Parse(Text(ParseLimits.MaximumTreeDepth / 2 - 10))).Outcome);
    }

    [Fact]
    public void A_left_associative_chain_at_the_constant_is_accepted_and_one_past_it_refused()
    {
        var at = "a" + string.Concat(Enumerable.Repeat("+a", ParseLimits.MaximumTreeDepth));
        Assert.Equal(ParseOutcome.Accepted, OnLargeStack(() => Parse(at)).Outcome);
        Assert.Equal(ParseOutcome.Refused, OnLargeStack(() => Parse(at + "+a")).Outcome);
    }
}
