using System.Text;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Parser.Bridge;

namespace Broiler.JavaScript.Parser.Tests;

/// <summary>
/// The bridge of milestone SJL-14.2 (Broiler.VM's JSD-0083, decision D44): each fact it gives this engine's tree from
/// the shared syntax library's, against the old parser's where the old parser gives it, and the context it passes in
/// the library's request. The side-by-side tool (bundle sjl-14-001's <c>tools/side-by-side</c>) compares whole trees
/// over the parse corpus; these tests hold each fact by name.
/// </summary>
public class SharedSyntaxBridgeTests
{
    private static AstProgram Old(string source) => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    private static AstProgram OldModule(string source) => FastParser.ParseModule(new StringSpan(source));

    private static AstProgram Bridged(string source, BridgeRequest? request = null) => SharedSyntaxBridge.Parse(new StringSpan(source), request ?? BridgeRequest.Script);

    private static string Names(IFastEnumerable<StringSpan>? names)
    {
        if (names == null)
            return "null";
        var text = new StringBuilder();
        var e = names.GetFastEnumerator();
        while (e.MoveNext(out var name))
            text.Append(name.Value).Append(',');
        return text.ToString();
    }

    private static T Statement<T>(AstBlock block, int index) where T : AstStatement => Assert.IsType<T>(block.Statements[index]);

    // ---- HoistingScope (from the declarations side table, in source order, each name once) ---------------------------

    [Theory]
    [InlineData("var a; let b; function c() {} const d = 1; class E {} var a;")]
    [InlineData("var x; { var y; let z; } for (var i = 0;;) { var j; } try {} catch (e) { var k; }")]
    [InlineData("function f() {} var f; function g() {} function g() {}")]
    [InlineData("label: function h() {} var u, [v, {w}] = o;")]
    public void A_programs_hoisting_scope_is_the_old_parsers(string source)
        => Assert.Equal(Names(Old(source).HoistingScope), Names(Bridged(source).HoistingScope));

    [Theory]
    [InlineData("function f(a, b) { var a; var c; function b() {} let d; }")]
    [InlineData("(a) => { var a; var b; }")]
    [InlineData("({ m(p) { var p, q; } })")]
    [InlineData("function f() { { let x; function y() {} } switch (0) { case 0: let s; } }")]
    public void A_functions_and_its_blocks_hoisting_scopes_are_the_old_parsers(string source)
    {
        var old = Old(source);
        var bridged = Bridged(source);
        Assert.Equal(Collect(old), Collect(bridged));

        static string Collect(AstProgram program)
        {
            var text = new StringBuilder();
            new Walker(node =>
            {
                if (node is AstBlock block)
                    text.Append(block.GetType().Name).Append('[').Append(Names(block.HoistingScope)).Append("] ");
                if (node is AstSwitchStatement @switch)
                    text.Append("switch[").Append(Names(@switch.HoistingScope)).Append("] ");
            }).Visit(program);
            return text.ToString();
        }
    }

    // ---- AnnexBFunctionNames (the old parser's rule over the declarations, in source order) -------------------------

    [Theory]
    [InlineData("{ function a() {} } if (x) function b() {} { let c; { function c() {} } } { function d() {} function d() {} }")]
    [InlineData("function f(p) { { function p() {} function q() {} } { function arguments() {} } }")]
    [InlineData("(() => { { function arguments() {} } })")]
    [InlineData("{ async function a() {} function* b() {} }")]
    public void Annex_b_function_names_are_the_old_parsers(string source)
    {
        Assert.Equal(Collect(Old(source)), Collect(Bridged(source)));

        static string Collect(AstProgram program)
        {
            var text = new StringBuilder();
            new Walker(node =>
            {
                if (node is AstBlock block)
                    text.Append('[').Append(Names(block.AnnexBFunctionNames)).Append("] ");
            }).Visit(program);
            return text.ToString();
        }
    }

    // ---- The Annex B.3.4 block (IsSyntheticFunctionStatementBlock) --------------------------------------------------

    [Fact]
    public void An_if_clause_function_shadowing_a_parameter_gets_the_synthetic_block()
    {
        const string source = "function f(g) { if (true) function g() {} if (true) function h() {} }";
        var function = (AstFunctionExpression)Statement<AstExpressionStatement>(Bridged(source), 0).Expression;
        var body = (AstBlock)function.Body;
        var shadowing = Statement<AstIfStatement>(body, 0);
        var synthetic = Assert.IsType<AstBlock>(shadowing.True);
        Assert.True(synthetic.IsSyntheticFunctionStatementBlock);
        Assert.Equal("g,", Names(synthetic.HoistingScope));

        var plain = Statement<AstIfStatement>(body, 1);
        Assert.IsType<AstExpressionStatement>(plain.True);
        Assert.Equal("h,", Names(body.AnnexBFunctionNames));

        var oldBody = (AstBlock)((AstFunctionExpression)Statement<AstExpressionStatement>(Old(source), 0).Expression).Body;
        Assert.True(Assert.IsType<AstBlock>(Statement<AstIfStatement>(oldBody, 0).True).IsSyntheticFunctionStatementBlock);
    }

    // ---- ContainsContinue, WasParenthesized, the m suffix, the top-level await fact ---------------------------------

    [Theory]
    [InlineData("for (;;) { continue; }", true)]
    [InlineData("for (;;) { function f() { for (;;) continue; } }", true)]
    [InlineData("for (;;) { break; }", false)]
    [InlineData("for (const x of y) { switch (x) { case 1: continue; } }", true)]
    public void A_loop_contains_a_continue_as_the_old_parser_counted(string source, bool expected)
    {
        var loop = Bridged(source).Statements[0];
        var field = loop.GetType().GetField("ContainsContinue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Assert.Equal(expected, (bool)field.GetValue(loop)!);
        Assert.Equal(field.GetValue(Old(source).Statements[0]), field.GetValue(loop));
    }

    [Fact]
    public void A_parenthesised_expression_is_marked_and_its_parentheses_are_in_the_chain()
    {
        var statement = Statement<AstExpressionStatement>(Bridged("(a); (b, c); (x) => x;"), 0);
        var a = Assert.IsType<AstIdentifier>(statement.Expression);
        Assert.True(a.WasParenthesized);
        Assert.Equal(TokenTypes.BracketStart, a.Start.Previous?.Type);
        Assert.Equal(TokenTypes.BracketEnd, a.End.Next?.Type);

        var sequence = Assert.IsType<AstSequenceExpression>(Statement<AstExpressionStatement>(Bridged("(b, c);"), 0).Expression);
        Assert.True(sequence.WasParenthesized);
        Assert.Equal(TokenTypes.BracketStart, sequence.Start.Type);

        var arrow = Assert.IsType<AstFunctionExpression>(Statement<AstExpressionStatement>(Bridged("(x) => x;"), 0).Expression);
        Assert.True(arrow.Params.First().Identifier.WasParenthesized);
    }

    [Fact]
    public void The_m_suffix_is_a_decimal_literal()
    {
        var literal = Assert.IsType<AstLiteral>(Statement<AstExpressionStatement>(Bridged("1.5m;"), 0).Expression);
        Assert.Equal(TokenTypes.Decimal, literal.TokenType);
        Assert.Equal(TokenTypes.Decimal, literal.Start.Type);
        Assert.Equal("1.5m", literal.Start.Span.Value);
    }

    [Fact]
    public void Top_level_await_is_the_programs_async_fact()
    {
        Assert.True(Bridged("await x;", BridgeRequest.Script with { AllowTopLevelAwait = true }).IsAsync);
        Assert.True(Bridged("await x;", BridgeRequest.Module).IsAsync);
        Assert.False(Bridged("async function f() { await x; }", BridgeRequest.Module).IsAsync);
    }

    // ---- The token chain's facts ------------------------------------------------------------------------------------

    [Fact]
    public void Tokens_carry_the_old_parsers_facts()
    {
        const string source = "var \\u0061b = 'x\\ty';\nlet n = 0x10, t = tag`a${n}b\\unicode`, r = /x\\u{1F600}/u;";
        var old = Old(source);
        var bridged = Bridged(source);
        foreach (var (o, b) in Pairs(old, bridged))
        {
            Assert.Equal(o.Type, b.Type);
            Assert.Equal(o.Span.Offset, b.Span.Offset);
            Assert.Equal(o.Span.Length, b.Span.Length);
            // An invalid template piece's cooked text is never read (the tag sees undefined); the old scanner kept a
            // partial cooking there and the library none, so only a valid piece's is held.
            if (!o.CookedInvalid)
                Assert.Equal(o.CookedText, b.CookedText);
            Assert.Equal(o.Flags, b.Flags);
            Assert.Equal(o.Number, b.Number);
            Assert.Equal(o.CookedInvalid, b.CookedInvalid);
            Assert.Equal(o.Start.Line, b.Start.Line);
            Assert.Equal(o.Start.Column, b.Start.Column);
        }

        static IEnumerable<(FastToken, FastToken)> Pairs(AstProgram old, AstProgram bridged)
        {
            var o = new List<FastToken>();
            var b = new List<FastToken>();
            new Walker(node => { if (node is AstLiteral or AstIdentifier) o.Add(node.Start); }).Visit(old);
            new Walker(node => { if (node is AstLiteral or AstIdentifier) b.Add(node.Start); }).Visit(bridged);
            Assert.Equal(o.Count, b.Count);
            return o.Zip(b);
        }
    }

    // ---- The regular-expression provider (decision D51) -------------------------------------------------------------

    [Theory]
    [InlineData("/(/;")]
    [InlineData("/a)/;")]
    [InlineData("/x/gg;")]
    [InlineData("/\\u{110000}/u;")]
    public void An_invalid_regular_expression_literal_is_refused(string source)
        => Assert.Throws<FastParseException>(() => Bridged(source));

    [Fact]
    public void A_regular_expression_literals_body_is_the_scanners()
    {
        var literal = Assert.IsType<AstLiteral>(Statement<AstExpressionStatement>(Bridged("/\\u{1F600}[a-z]/u;"), 0).Expression);
        var old = Assert.IsType<AstLiteral>(Statement<AstExpressionStatement>(Old("/\\u{1F600}[a-z]/u;"), 0).Expression);
        Assert.Equal(old.Regex, literal.Regex);
    }

    // ---- What the bridge refuses itself, and the kept texts (decision D46) -------------------------------------------

    [Fact]
    public void An_import_attribute_this_engine_cannot_honour_is_refused_as_the_old_parser_refuses_it()
    {
        const string source = "import a from 'm' with { kind: 'json' };";
        var old = Assert.Throws<FastParseException>(() => OldModule(source));
        var bridged = Assert.Throws<FastParseException>(() => Bridged(source, BridgeRequest.Module));
        Assert.Equal(old.Message, bridged.Message);
        Assert.NotNull(Bridged("import a from 'm' with { type: 'json' };", BridgeRequest.Module));
    }

    [Theory]
    [InlineData("await 1;", "1, 7")]
    [InlineData("var a = await f();", "1, 15")]
    public void A_scripts_top_level_await_is_refused_as_the_library_refuses_it(string source, string position)
    {
        // The old parser never read a script's await as an expression, so the text the shell's top-level-await retry
        // reads ("Unexpected await") never came from a script's parse; the bridge gives the library's diagnostic, with
        // its position, and the retry never fires on it.
        var refusal = Assert.Throws<FastParseException>(() => Bridged(source));
        Assert.DoesNotContain("Unexpected await", refusal.Message);
        Assert.EndsWith(" at " + position, refusal.Message);
        Assert.Throws<FastParseException>(() => Old(source));
        Assert.True(Bridged(source, BridgeRequest.Script with { AllowTopLevelAwait = true }).IsAsync);
    }

    [Fact]
    public void A_scripts_await_as_a_name_is_still_a_name()
    {
        Assert.False(Bridged("var await = 1; await;").IsAsync);
        Assert.Throws<FastParseException>(() => Bridged("var a = 1 1;"));
    }

    [Fact]
    public void An_invalid_regular_expression_is_refused_with_the_literal_and_its_position()
        => Assert.Equal("Invalid regular expression: /(/g at 1, 9", Assert.Throws<FastParseException>(() => Bridged("var r = /(/g;")).Message);

    [Fact]
    public void A_redeclaration_keeps_the_text_tests_read()
        => Assert.Contains("already defined", Assert.Throws<FastParseException>(() => Bridged("let a; let a;")).Message);

    [Theory]
    [InlineData("import { a, a } from 'm';", "Identifier 'a' has already been declared")]
    [InlineData("import { a } from 'm'; var a = 1;", "Identifier 'a' has already been declared")]
    [InlineData("export default 1; export default 2;", "Duplicate export name 'default'")]
    public void A_modules_redeclaration_keeps_the_text_its_tests_read(string source, string expected)
        => Assert.Contains(expected, Assert.Throws<FastParseException>(() => Bridged(source, BridgeRequest.Module)).Message);

    [Fact]
    public void A_unary_operator_before_exponentiation_keeps_the_old_parsers_text()
        => Assert.Contains("exponentiation", Assert.Throws<FastParseException>(() => Bridged("async function f(){ return await 2 ** 2; }")).Message);

    [Fact]
    public void An_unexpected_character_is_named_by_its_code_point()
        => Assert.Contains("U+FFFF", Assert.Throws<FastParseException>(() => Bridged("var a = 1; \uFFFF var b = 2;")).Message);

    [Fact]
    public void A_refusal_is_at_the_token_its_diagnostic_names()
    {
        var refusal = Assert.Throws<FastParseException>(() => Bridged("a;\n  )"));
        Assert.Equal(2, refusal.Token.Start.Line);
        Assert.Equal(3, refusal.Token.Start.Column);
    }

    // ---- Spans: each node's span is its text (bundle sjl-14-001's rule, part C, class S) ------------------------------

    private static T First<T>(AstNode root, Func<T, bool>? where = null) where T : AstNode
    {
        T? found = null;
        new Walker(node =>
        {
            if (found == null && node is T match && (where == null || where(match)))
                found = match;
        }).Visit(root);
        return found ?? throw new Xunit.Sdk.XunitException("no " + typeof(T).Name);
    }

    private static string SpanText(string source, AstNode node)
    {
        var start = node.Start.Span.Offset;
        var end = node.End.Type == TokenTypes.EOF ? source.Length : node.End.Span.Offset + node.End.Span.Length;
        return source.Substring(start, end - start);
    }

    [Theory]
    [InlineData("/* lead */ var a;")]
    [InlineData("  // lead\nvar a;")]
    [InlineData("var a;")]
    public void A_program_begins_at_its_sources_first_code_unit(string source)
    {
        Assert.Equal(0, Bridged(source).Start.Span.Offset);
        Assert.Equal(source, SpanText(source, Bridged(source)));
    }

    [Theory]
    [InlineData("{\n  a;\n}", "{\n  a;\n}")]
    [InlineData("if (x) {} else { b; }", "{ b; }")]
    [InlineData("try { a; } catch (e) { b; } finally { c; }", "{ a; }")]
    public void A_block_begins_at_its_brace(string source, string block)
        => Assert.Equal(block, SpanText(source, First<AstBlock>(Bridged(source), b => b is not AstProgram && b.Statements.Count > 0)));

    [Theory]
    [InlineData("function f(a, b = (1)) { return a; }", "{ return a; }")]
    [InlineData("function /* ( */ g() {\n}", "{\n}")]
    [InlineData("(async () => { x; })", "{ x; }")]
    [InlineData("((a, b,) => {})", "{}")]
    [InlineData("(a => { a; })", "{ a; }")]
    [InlineData("({ get [g()]() { return 1; } })", "{ return 1; }")]
    [InlineData("({ 'm(n)'(p) { p; } })", "{ p; }")]
    [InlineData("(class { static { s; } })", "{ s; }")]
    public void A_functions_body_begins_at_its_brace(string source, string body)
        => Assert.Equal(body, SpanText(source, First<AstBlock>(Bridged(source), b => b is not AstProgram)));

    [Fact]
    public void An_await_and_a_field_end_where_their_text_ends()
    {
        const string awaited = "async function f() { await (x); }";
        Assert.Equal("await (x)", SpanText(awaited, First<AstAwaitExpression>(Bridged(awaited))));
        const string field = "(class { x = (1); })";
        Assert.Equal("x = (1)", SpanText(field, First<AstClassProperty>(Bridged(field))));
    }

    [Fact]
    public void A_new_expression_begins_at_its_new()
    {
        const string source = "new new X(a)();";
        var outer = First<AstNewExpression>(Bridged(source));
        Assert.Equal(0, outer.Start.Span.Offset);
        Assert.Equal(4, First<AstNewExpression>(outer.Callee).Start.Span.Offset);
        Assert.True(outer.Start.Span.Offset <= outer.End.Span.Offset);
        Assert.Equal("new X", SpanText("new X;", First<AstNewExpression>(Bridged("new X;"))));
    }

    // ---- Token facts at a node's edge (review findings of 2026-11-03) ---------------------------------------------------

    [Theory]
    [InlineData("tag`\\01`;")]
    [InlineData("tag`\\1x`;")]
    [InlineData("tag`\\xg`;")]
    [InlineData("tag`\\xAg`;")]
    [InlineData("tag`a\\n\\x41\\u0042\\u{43}\\8b`;")]
    [InlineData("tag`\\u00g`;")]
    [InlineData("tag`\\u{g}x`;")]
    [InlineData("tag`\\u{110000}`;")]
    [InlineData("tag`\\0\\08`;")]
    [InlineData("tag`x\\9${1}\\xz`;")]
    public void An_invalid_template_pieces_value_is_the_old_parsers(string source)
    {
        static string Pieces(AstProgram program)
        {
            var text = new StringBuilder();
            new Walker(node =>
            {
                if (node is AstLiteral { TokenType: TokenTypes.TemplateEnd or TokenTypes.TemplatePart } piece)
                    text.Append('[').Append(piece.StringValue).Append(']');
            }).Visit(program);
            return text.ToString();
        }

        Assert.Equal(Pieces(Old(source)), Pieces(Bridged(source)));
    }

    [Fact]
    public void An_object_propertys_first_token_is_its_names_own()
    {
        const string source = "({'g': 3, 1: 4});";
        var properties = new List<AstClassProperty>();
        new Walker(node =>
        {
            if (node is AstClassProperty property)
                properties.Add(property);
        }).Visit(Bridged(source));
        Assert.Equal("g", properties[0].Start.CookedText);
        Assert.Equal(TokenTypes.Number, properties[1].Start.Type);
        Assert.Equal(1, properties[1].Start.Number);
    }

    [Theory]
    [InlineData("(class { \u2118() {} })")]
    [InlineData("(class { \u2118x = 1 })")]
    public void A_name_is_read_by_the_engines_identifier_classes(string source)
    {
        var bridged = First<AstClassProperty>(Bridged(source));
        var old = First<AstClassProperty>(Old(source));
        Assert.Equal(old.Start.Type, bridged.Start.Type);
        Assert.Equal(TokenTypes.Identifier, bridged.Start.Type);
    }

    // ---- The inherited context of eval code (decision D44; bundle sjl-14-001's probes) --------------------------------

    private static BridgeRequest Eval => new() { Goal = BridgeGoal.Eval };

    [Fact]
    public void Eval_codes_new_target_follows_the_function_flag()
    {
        Assert.Throws<FastParseException>(() => Bridged("new.target", Eval));
        Assert.NotNull(Bridged("new.target", Eval with { InFunction = true }));
        // IC1: the flag reaches eval code the bridge parses, whatever compiled its caller.
        Assert.NotNull(Bridged("() => new.target", Eval with { InFunction = true }));
    }

    [Fact]
    public void Eval_codes_method_derived_constructor_and_field_initialiser_flags_reach_the_request()
    {
        var context = SharedSyntaxBridge.Request(new StringSpan("super.x"), Eval with
        {
            InFunction = true,
            InMethod = true,
            InDerivedConstructor = true,
            InClassFieldInitializer = true,
            VisiblePrivateNames = ["#a"],
        }).Context;
        Assert.NotNull(context);
        Assert.True(context.InFunction);
        Assert.True(context.InMethod);
        Assert.True(context.InDerivedConstructor);
        Assert.True(context.InClassFieldInitializer);
        Assert.Equal(["#a"], context.VisiblePrivateNames);
        Assert.Null(SharedSyntaxBridge.Request(new StringSpan("1"), BridgeRequest.Script).Context);
    }

    [Fact]
    public void Eval_codes_super_and_arguments_placement_stays_the_engines_check()
    {
        // The library's stage table puts EA0003 to EA0005 at the semantics stage (no parser checks them), so at this
        // pin it reads only InFunction of the context; DirectEvalSupport's walk still refuses them after the bridge's
        // parse, as it does after the old parser's. The bridge carries the flags for the library's later stages.
        // IC6 (probes IC101, IC108): a super call in a derived constructor's eval is admitted at the parse; its
        // derived-constructor flag reaches the request (above), which the library does not read at this pin. IC3 and
        // IC5 are run-time answers (new.target's value, arguments' visibility), not the parse's, so no request test
        // holds them; the probes do, with the shell's switch off and on.
        Assert.NotNull(Bridged("super.x", Eval with { InFunction = true }));
        Assert.NotNull(Bridged("super()", Eval with { InFunction = true, InMethod = true }));
        Assert.NotNull(Bridged("super()", Eval with { InFunction = true, InMethod = true, InDerivedConstructor = true }));
        Assert.NotNull(Bridged("arguments", Eval with { InFunction = true, InMethod = true, InClassFieldInitializer = true }));
    }

    [Fact]
    public void Eval_code_inherits_the_callers_strictness()
    {
        // IC2 and IC7, and K1 (X09): a strict caller's eval code is strict, whatever its own directives say.
        Assert.NotNull(Bridged("08", Eval));
        Assert.Throws<FastParseException>(() => Bridged("08", Eval with { ForcedStrict = true }));
        Assert.Throws<FastParseException>(() => Bridged("with (o) {}", Eval with { ForcedStrict = true }));
        // R36 (X07): a class field named `static` is no strict-mode reserved word use.
        Assert.NotNull(Bridged("(class { static = 1 })", Eval with { ForcedStrict = true }));
        // N26 (X05): a hashbang at the start of eval code is admitted.
        Assert.NotNull(Bridged("#!hashbang\n1", Eval with { ForcedStrict = true }));
    }

    [Fact]
    public void Eval_code_sees_the_private_names_visible_to_its_caller()
    {
        // IC4: the names are passed, and the library checks them against what eval code names.
        var visible = Eval with { InFunction = true, InMethod = true, VisiblePrivateNames = ["#a"] };
        Assert.NotNull(Bridged("this.#a", visible));
        Assert.NotNull(Bridged("#a in this", visible));
    }

    // ---- The dynamic function's goals (CreateDynamicFunction, P26 and D61) -------------------------------------------

    [Fact]
    public void A_dynamic_functions_parameters_and_body_are_parsed_alone()
    {
        SharedSyntaxBridge.CheckDynamicFunction("a, b = 1", "return a + b;", BridgeFunctionKind.Normal);
        // Y07: a body that closes the function early is refused.
        Assert.Throws<FastParseException>(() => SharedSyntaxBridge.CheckDynamicFunction("a", "}); (function(){", BridgeFunctionKind.Normal));
        Assert.Throws<FastParseException>(() => SharedSyntaxBridge.CheckDynamicFunction("/*", "*/){", BridgeFunctionKind.Normal));
        // The kind decides yield and await in the pieces.
        SharedSyntaxBridge.CheckDynamicFunction("", "yield 1;", BridgeFunctionKind.Generator);
        Assert.Throws<FastParseException>(() => SharedSyntaxBridge.CheckDynamicFunction("", "yield 1;", BridgeFunctionKind.Normal));
        SharedSyntaxBridge.CheckDynamicFunction("", "await 1;", BridgeFunctionKind.Async);
        // Y02, Y08, Y09: a super property or call in either piece is the semantics stage's (EE0074 to EE0077), which
        // the whole function's compile after these parses refuses; the library's parse admits it.
        SharedSyntaxBridge.CheckDynamicFunction("a = super.x", "super();", BridgeFunctionKind.Normal);
    }

    // ---- Limits and the parse thread (decision D47) -------------------------------------------------------------------

    [Fact]
    public void The_library_nests_to_its_supported_maximum_from_a_small_stack()
    {
        var depth = Broiler.ECMAScript.Syntax.ParseLimits.MaximumSupportedNestingDepth - 2;
        var source = new string('[', depth) + new string(']', depth);
        AstProgram? program = null;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                program = Bridged(source);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }, 256 * 1024);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
        Assert.NotNull(program);

        var tooDeep = new string('[', depth + 10) + new string(']', depth + 10);
        Assert.Contains("nested too deeply", Assert.Throws<FastParseException>(() => Bridged(tooDeep)).Message);
    }

    [Fact]
    public void The_parse_has_the_librarys_stack_however_small_the_compile_stack_is_configured()
    {
        // Review finding of 2026-11-03: with the compile stack configured below ParseLimits.StackBytes the bridge's
        // hop ran on a worker of that size, and a nesting within the supported maximum overflowed it and ended the
        // process. The hop's worker now has at least the library's stack.
        var depth = Broiler.ECMAScript.Syntax.ParseLimits.MaximumSupportedNestingDepth - 2;
        var source = new string('[', depth) + new string(']', depth);
        var configured = Broiler.JavaScript.ExpressionCompiler.CompilationStack.SizeBytes;
        Broiler.JavaScript.ExpressionCompiler.CompilationStack.SizeBytes = 1024 * 1024;
        try
        {
            Assert.NotNull(Bridged(source));
        }
        finally
        {
            Broiler.JavaScript.ExpressionCompiler.CompilationStack.SizeBytes = configured;
        }
    }

    /// <summary>Every node of a tree, through its fields, in order.</summary>
    private sealed class Walker(Action<AstNode> visit)
    {
        private readonly HashSet<object> seen = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

        internal void Visit(object? value)
        {
            switch (value)
            {
                case null or string or FastToken:
                    return;
                case AstNode node:
                    if (!seen.Add(node))
                        return;
                    visit(node);
                    foreach (var field in Fields(node.GetType()))
                        Visit(field.GetValue(node));
                    return;
                case System.Collections.IEnumerable items:
                    foreach (var item in items)
                        Visit(item);
                    return;
            }

            var type = value.GetType();
            if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace?.StartsWith("Broiler", StringComparison.Ordinal) == true)
            {
                foreach (var field in Fields(type))
                    Visit(field.GetValue(value));
            }
        }

        private static IEnumerable<System.Reflection.FieldInfo> Fields(Type type)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType != typeof(FastToken) && field.FieldType != typeof(StringSpan) && field.Name is not ("Start" or "End"))
                        yield return field;
                }
            }
        }
    }
}
