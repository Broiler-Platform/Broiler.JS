using System.Reflection;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

/// <summary>
/// The compiler's for-loop pass, <c>ForLoopPreparation</c> (milestone SJL-12.5, item 5; Broiler.VM's
/// JSD-0080, decision D22): the entry point the bundle's decision rule names, the parser's
/// <c>ContainsContinue</c> it reads, the shape it emits, the temporaries' names, and the completion values
/// of the loops it rewrites.
/// </summary>
/// <remarks>
/// The pass and the loops' <c>ContainsContinue</c> are internal, so these tests reach them as the bundle's
/// parse-corpus tool does: by name, through reflection. That also holds the entry point to the name and
/// signature the rule fixes.
/// </remarks>
public sealed class ForLoopPreparationTests
{
    private const string PassType = "Broiler.JavaScript.Compiler.ForLoopPreparation";

    private static readonly MethodInfo Prepare = typeof(FastCompiler).Assembly
        .GetType(PassType, throwOnError: true)!
        .GetMethod("Prepare", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            binder: null, types: [typeof(AstProgram), typeof(int)], modifiers: null)!;

    private static AstProgram Parse(string source)
        => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    private static AstProgram Prepared(string source, int directEvalDepth = 0)
        => (AstProgram)Prepare.Invoke(null, [Parse(source), directEvalDepth])!;

    private static bool ContainsContinue(AstStatement loop)
        => (bool)loop.GetType().GetField("ContainsContinue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loop)!;

    /// <summary>The entry point the decision rule names: internal, static, (AstProgram, int) to AstProgram.</summary>
    [Fact]
    public void The_pass_is_the_entry_point_the_rule_names()
    {
        Assert.NotNull(Prepare);
        Assert.Equal(typeof(AstProgram), Prepare.ReturnType);
        Assert.True(Prepare.IsAssembly, "Prepare is internal");
        Assert.False(Prepare.DeclaringType!.IsPublic, "ForLoopPreparation is internal");
        Assert.True(Prepare.DeclaringType.IsAbstract && Prepare.DeclaringType.IsSealed, "ForLoopPreparation is a static class");
    }

    /// <summary>
    /// The fixture item 5 owes: a <c>continue</c> inside a <c>switch</c> inside a <c>let</c> loop whose body
    /// moves the binding and whose update does not. The body's copy back into the carrier must run on the
    /// <c>continue</c> path, in a <c>finally</c>; a search of the tree that missed the <c>switch</c> case's
    /// statements would put it after the body, where the <c>continue</c> skips it, and the loop would read
    /// its first value for ever. The guard turns that into a failure instead of a hang.
    /// </summary>
    [Theory]
    [InlineData("for (let i = 0; i < 5;) { if (++guard > 50) throw new Error('did not terminate'); i++; switch (i % 2) { case 0: continue; default: n++; } }", 3)]
    [InlineData("for (let i = 0; i < 3;) { if (++guard > 50) throw new Error('did not terminate'); i++; n++; switch (i) { default: continue; } }", 3)]
    [InlineData("outer: for (let i = 0; i < 3;) { if (++guard > 50) throw new Error('did not terminate'); i++; n++; switch (i) { case 1: continue outer; default: break; } }", 3)]
    [InlineData("for (let i = 0; i < 4;) { if (++guard > 50) throw new Error('did not terminate'); i++; for (const x of [1]) { switch (x) { default: if (i % 2) continue; } } n++; }", 4)]
    public void A_continue_inside_a_switch_inside_a_loop_terminates(string loop, int expected)
    {
        using var context = new JSContext();
        var result = context.Eval("var n = 0, guard = 0; " + loop + " n");
        Assert.Equal(expected.ToString(System.Globalization.CultureInfo.InvariantCulture), result.ToString());
    }

    /// <summary>
    /// The fact the pass reads: the parser records a <c>continue</c> anywhere in the body, a <c>switch</c>
    /// case's statements and a nested loop's body included, and the pass then puts the copy-back in a
    /// <c>finally</c>; without one it is a trailing statement.
    /// </summary>
    [Theory]
    [InlineData("for (let i = 0; i < 3;) { i++; switch (i) { default: continue; } }", true)]
    [InlineData("for (let i = 0; i < 3;) { i++; for (;;) { switch (i) { case 1: continue; } break; } }", true)]
    [InlineData("for (let i = 0; i < 3;) { i++; switch (i) { default: break; } }", false)]
    [InlineData("for (let i = 0; i < 3;) { i++; }", false)]
    public void The_parser_records_a_continue_in_the_body_and_the_pass_reads_it(string source, bool containsContinue)
    {
        var parsed = Parse(source);
        var loop = Assert.IsType<AstForStatement>(Assert.Single(parsed.Statements));
        Assert.Equal(containsContinue, ContainsContinue(loop));

        // As written: the parser does not rewrite the head.
        var declaration = Assert.IsType<AstVariableDeclaration>(loop.Init);
        Assert.Equal(FastVariableKind.Let, declaration.Kind);

        var prepared = Assert.IsType<AstForStatement>(Assert.Single(((AstProgram)Prepare.Invoke(null, [parsed, 0])!).Statements));
        Assert.Equal(containsContinue, ContainsContinue(prepared));
        var body = Assert.IsType<AstBlock>(prepared.Body);
        Assert.Equal(containsContinue, body.Statements.Any(static s => s is AstTryStatement));
    }

    /// <summary>
    /// What moved with the rewrite: the head's TDZ names of a lexical for-in or for-of loop, and the block a
    /// C-style <c>await using</c> loop is disposed by, are the pass's; the parser leaves both out.
    /// </summary>
    [Fact]
    public void The_pass_names_the_heads_TDZ_bindings_and_wraps_a_using_loop()
    {
        var parsed = Parse("for (const [a, b] of []) {}");
        Assert.Null(Assert.IsType<AstForOfStatement>(Assert.Single(parsed.Statements)).HeadTdzNames);
        var prepared = Assert.IsType<AstForOfStatement>(Assert.Single(((AstProgram)Prepare.Invoke(null, [parsed, 0])!).Statements));
        Assert.Equal(new[] { "a", "b" }, prepared.HeadTdzNames!.Select(static name => name.Value));

        static IFastEnumerable<AstStatement> Body(AstProgram program)
            => ((AstBlock)((Broiler.JavaScript.Ast.Expressions.AstFunctionExpression)((AstExpressionStatement)program.Statements[0]).Expression).Body).Statements;

        const string usingLoop = "async function f() { for (await using x = null; false; ) {} }";
        Assert.IsType<AstForStatement>(Assert.Single(Body(Parse(usingLoop))));
        var block = Assert.IsType<AstBlock>(Assert.Single(Body(Prepared(usingLoop))));
        var loop = Assert.IsType<AstForStatement>(Assert.Single(block.Statements));
        Assert.True(Assert.IsType<AstVariableDeclaration>(loop.Init).Using);
    }

    /// <summary>
    /// The temporaries' names (decision D22): <c>&lt;d&gt;:&lt;n&gt;</c>, by the direct-eval depth and a
    /// counter of the pass from 1, in the order the parser named them (a loop after the loops inside it).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void The_temporaries_are_named_by_the_depth_and_the_passs_counter(int depth)
    {
        const string source = "for (let i = 0; i < 2; i++) { for (const x of [1]) {} } for (const k in {}) {}";
        var program = Prepared(source, depth);
        string Name(int n) => depth + ":" + n;

        Assert.Equal(new[] { Name(1), Name(2), Name(3) }, Temporaries(source, program).Order(StringComparer.Ordinal));

        // The inner loop is prepared first, so its carrier is the first; the outer loop's carrier, the
        // second, is declared beside the binding it copies; the for-in loop's, the third, comes last.
        var outer = Assert.IsType<AstForStatement>(program.Statements[0]);
        var carriers = Assert.IsType<AstVariableDeclaration>(outer.Init).Declarators.Select(static d => ((Broiler.JavaScript.Ast.AstIdentifier)d.Identifier).Name.Value);
        Assert.Equal(new[] { "i", Name(2) }, carriers);
        var inner = Assert.IsType<AstForOfStatement>(Assert.IsType<AstBlock>(outer.Body).Statements.OfType<AstForOfStatement>().Single());
        Assert.Equal(Name(1), ((Broiler.JavaScript.Ast.AstIdentifier)Assert.IsType<AstVariableDeclaration>(inner.Init).Declarators.Single().Identifier).Name.Value);
        var forIn = Assert.IsType<AstForInStatement>(program.Statements[1]);
        Assert.Equal(Name(3), ((Broiler.JavaScript.Ast.AstIdentifier)Assert.IsType<AstVariableDeclaration>(forIn.Init).Declarators.Single().Identifier).Name.Value);
    }

    /// <summary>
    /// Two passes over two parses of one source name alike, whatever was prepared before or between; a
    /// temporary's name is never an identifier. The temporaries are found by what the pass adds, not by how
    /// they are spelt: every name in the prepared tree that the parsed tree does not hold, which must be
    /// <c>0:1</c> to <c>0:4</c> here (two carriers for <c>a</c> and <c>b</c>, one for <c>m</c>, and the
    /// flag of an update that holds a closure), and the identifiers without a colon are the parsed tree's,
    /// none added.
    /// </summary>
    [Fact]
    public void The_temporaries_do_not_depend_on_what_was_prepared_before()
    {
        const string source = "for (let [a, b] = [1, 2]; a < b; a++) { } for (let m = 0; m < 1; m++, (() => m)) { }";
        var first = Prepared(source);
        _ = Prepared("for (let q = 0; q < 1; q++) { for (let r = 0; r < 1; r++) {} }");
        var second = Prepared(source);

        var temporaries = Temporaries(source, first);
        Assert.Equal(new[] { "0:1", "0:2", "0:3", "0:4" }, temporaries.Order(StringComparer.Ordinal));
        Assert.All(temporaries, static name => Assert.Matches(@"^0:[1-9][0-9]*$", name));
        Assert.Equal(Names(first), Names(second));

        var parsedIdentifiers = Names(Parse(source)).Where(static name => !name.Contains(':')).Order(StringComparer.Ordinal);
        var preparedIdentifiers = Names(first).Where(static name => !name.Contains(':')).Order(StringComparer.Ordinal);
        Assert.Equal(parsedIdentifiers, preparedIdentifiers);
    }

    /// <summary>
    /// Loops in direct eval code and in the code that calls it, whose <c>var</c> carriers a sloppy direct
    /// eval declares in one variable environment, keep their own values; the last row's function is
    /// compiled in eval code and called after that eval returned. These rows give the same results at
    /// <c>792ff56e</c>, where the parser named the carriers of both from 2: they check that the values
    /// survive the move of the rewrite, not that the names differ. The names are checked by
    /// <see cref="ForLoopPreparationDepthTests"/>.
    /// </summary>
    [Theory]
    [InlineData("(function () { var r = []; var n = 0; for (const x = 7; ;) { r.push(x); if (++n > 2) break; eval(\"for (const y of ['a']) {}\"); } return r.join(); })()", "7,7,7")]
    [InlineData("(function () { var s = ''; for (let i = 0; i < 2; i++) { eval(\"for (let j = 0; j < 2; j++) { s += i + '' + j + ','; }\"); } return s; })()", "00,01,10,11,")]
    [InlineData("(function () { var s = ''; for (const x of [1, 2]) { eval(\"for (const y of [3]) { s += x + '' + y; }\"); } return s; })()", "1323")]
    [InlineData("var f = eval(\"(function () { var r = [], n = 0; for (const x = 7; ;) { r.push(x); if (++n > 2) break; eval(\\\"for (const y of ['a']) {}\\\"); } return r.join(); })\"); f()", "7,7,7")]
    public void Loops_in_direct_eval_code_keep_their_values_beside_the_callers(string source, string expected)
    {
        using var context = new JSContext();
        Assert.Equal(expected, context.Eval(source).ToString());
    }

    /// <summary>
    /// A rewritten loop completes with the value ForBodyEvaluation gives it: V starts undefined, takes each
    /// iteration's body value that is not empty, and a <c>break</c> or <c>continue</c> completes by
    /// UpdateEmpty, so its own value wins when it has one and V when it has none. The statements the pass
    /// adds - the per-iteration declaration, the test it moves into the body, the guarded update, the copy
    /// back into the carriers and the <c>try</c> that runs the copy on a <c>continue</c> - take no part.
    /// Before they were marked, the test that ends the loop left it undefined, as an <c>if</c> without an
    /// <c>else</c> does, and an empty body of a <c>let</c> <c>for-of</c> or <c>for-in</c> loop gave the
    /// declaration's value. A <see langword="null"/> expected value is undefined. The values are those Node
    /// 22 gives, but for the last three rows: Node 22 has no <c>using</c>, and they are ForBodyEvaluation's,
    /// as a C-style <c>using</c> head is kept as written and adds nothing to the body. The first four rows are
    /// test262's <c>language/statements/for/cptn-decl-expr-iter.js</c> and <c>cptn-decl-expr-no-iter.js</c>
    /// with a <c>let</c> head for their <c>var</c> one, and the next four the C-style form of
    /// <c>for-of/cptn-decl-abrupt-empty.js</c>.
    /// </summary>
    [Theory]
    [InlineData("1; for (let run = true; run; run = false) { }", null)]
    [InlineData("2; for (let run = true; run; run = false) { 3; }", "3")]
    [InlineData("1; for (let run = false; run; ) { }", null)]
    [InlineData("2; for (let run = false; run; ) { 3; }", null)]
    [InlineData("1; for (let a; ; ) { break; }", null)]
    [InlineData("2; for (let b; ; ) { 3; break; }", "3")]
    [InlineData("4; outer: do { for (let a; ; ) { continue outer; } } while (false)", null)]
    [InlineData("5; outer: do { for (let b; ; ) { 6; continue outer; } } while (false)", "6")]
    [InlineData("for (let i = 0; i < 3; i++) { i; }", "2")]
    [InlineData("for (let i = 0; i < 3; i++) { (() => i)(); }", "2")]
    [InlineData("for (let i = 0; i < 3; i++) i;", "2")]
    [InlineData("for (let i = 0; i < 3;) { i++; }", "2")]
    [InlineData("for (let [a, b] = [0, 3]; a < b; a++) { a * 10; }", "20")]
    [InlineData("for (let i = 0; i < 3; i++) { let i = 'x' + 1; i; }", "x1")]
    [InlineData("for (let i = 0; i < 2; i++) for (let j = 0; j < 2; j++) i * 10 + j;", "11")]
    [InlineData("for (let i = 0; i < 3; i++, (() => i)) { i; }", "2")]
    [InlineData("for (let i = 0; i < 3; i++) { try { i; } finally { 'f'; } }", "2")]
    [InlineData("for (const c = 5, n = [0]; n[0] < 3; n[0]++) { c + n[0]; }", "7")]
    [InlineData("for (const k = 1; ; ) { k; break; }", "1")]
    [InlineData("1; for (let i = 0; i < 3; i++) { }", null)]
    [InlineData("1; for (let i = 0; i < 3; i++) { let x = i; }", null)]
    [InlineData("1; for (let i = 0; i < 3; i++, (() => i)) { }", null)]
    [InlineData("1; for (let i = 0; i < 3;) { i++; var z; }", "2")]
    [InlineData("1; for (const c = 0, n = [0]; n[0] < 3; n[0]++) { }", null)]
    [InlineData("for (let i = 0; i < 5; i++) { if (i < 2) continue; i; break; }", "2")]
    [InlineData("for (let i = 0; i < 3; i++) { if (i === 1) continue; i; }", "2")]
    [InlineData("for (let i = 0; i < 3; i++) { i; if (i === 2) continue; }", null)]
    [InlineData("for (let i = 0; i < 3; i++) { i; continue; }", "2")]
    [InlineData("1; for (let i = 0; i < 3; i++) { if (i === 2) continue; }", null)]
    [InlineData("for (let i = 0; i < 3; i++) { switch (i) { case 1: continue; } i; }", "2")]
    [InlineData("for (let i = 0; i < 3; i++) { i; if (i === 1) break; }", null)]
    [InlineData("for (let i = 0; ; i++) { if (i === 3) break; i; }", null)]
    [InlineData("for (let i = 0; i < 3; i++) { i; { break; } }", "0")]
    [InlineData("outer: for (let i = 0; i < 3; i++) { for (let j = 0; j < 3; j++) { i * 10 + j; if (j === 1) continue outer; } }", null)]
    [InlineData("outer: for (let i = 0; i < 3; i++) { for (;;) { i; continue outer; } }", "2")]
    [InlineData("(function () { return eval('for (let i = 0; i < 3; i++) { i; }'); })()", "2")]
    [InlineData("1; for (let [] = []; false; ) { }", null)]
    [InlineData("1; for (let x of [1, 2]) { }", null)]
    [InlineData("1; for (let x in { a: 1 }) { }", null)]
    [InlineData("1; for (const x of [1, 2]) { }", null)]
    [InlineData("for (let x of [1, 2]) { x; }", "2")]
    [InlineData("var n = 0; for (using u = null; n < 3; n++) { n; }", "2")]
    [InlineData("1; for (using u = null; ; ) { break; }", null)]
    [InlineData("2; for (using u = null; ; ) { 3; break; }", "3")]
    public void A_lexical_loop_completes_with_its_last_body_value(string source, string? expected)
    {
        using var context = new JSContext();
        var result = context.Eval(source, "x.js");
        if (expected == null)
            Assert.True(result.IsUndefined, source + " completed with " + result + ", not undefined");
        else
            Assert.Equal(expected, result.ToString());
    }

    /// <summary>
    /// What keeps the pass's statements out of the completion value: it marks each statement it adds to a
    /// loop's body as the loop's bookkeeping, and none of the source's. The blocks it builds are not marked,
    /// and neither are the statements inside a marked one other than the <c>try</c> (the test's
    /// <c>break</c>, the guarded update), which the compiler keeps out with it. Every statement of the
    /// parsed bodies but a block, which the pass may splice away, is found again in the prepared ones,
    /// unmarked; the head's declaration, which the pass replaces, is not a body's statement.
    /// </summary>
    [Theory]
    [InlineData("for (let i = 0; i < 3; i++) { i; }", 3)]
    [InlineData("for (let i = 0; i < 3; i++) { if (i) continue; i; }", 4)]
    [InlineData("for (let i = 0; i < 3; i++, (() => i)) { i; }", 5)]
    [InlineData("for (let i = 0; ; ) { let i = 1; break; }", 2)]
    [InlineData("for (const c = 0; c < 1; ) { c; break; }", 1)]
    [InlineData("for (let x of [1]) x; for (const k in {}) {}", 2)]
    public void The_pass_marks_the_statements_it_adds_and_no_other(string source, int added)
    {
        var program = Parse(source);
        var parsed = new HashSet<AstStatement>(BodyStatements(program).Where(static s => s is not AstBlock), System.Collections.Generic.ReferenceEqualityComparer.Instance);
        Assert.DoesNotContain(parsed, IsLoopBookkeeping);

        var prepared = BodyStatements((AstProgram)Prepare.Invoke(null, [program, 0])!);
        var marked = prepared.Where(IsLoopBookkeeping).ToList();
        Assert.Equal(added, marked.Count);
        Assert.All(marked, static statement => Assert.Contains(statement.Type,
            new[] { FastNodeType.VariableDeclaration, FastNodeType.IfStatement, FastNodeType.ExpressionStatement, FastNodeType.TryStatement }));

        var unmarked = prepared.Where(static s => s is not AstBlock && !IsLoopBookkeeping(s)).ToList();
        Assert.Equal(parsed.Count, unmarked.Count);
        Assert.All(unmarked, statement => Assert.Contains(statement, parsed));
    }

    private static bool IsLoopBookkeeping(AstStatement statement)
        => (bool)typeof(AstStatement).GetField("IsLoopBookkeeping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(statement)!;

    // The statements of the bodies of a program's top-level loops, at any depth, blocks among them; the
    // statements inside a marked statement other than a try are not walked. The sources above hold no
    // statement that holds a statement but a block, an if and the pass's try.
    private static List<AstStatement> BodyStatements(AstProgram program)
    {
        var statements = new List<AstStatement>();

        void Walk(AstStatement? statement)
        {
            if (statement == null)
                return;

            statements.Add(statement);
            if (IsLoopBookkeeping(statement) && statement is not AstTryStatement)
                return;

            switch (statement)
            {
                case AstBlock block:
                    foreach (var inner in block.Statements)
                        Walk(inner);
                    break;
                case AstIfStatement branch:
                    Walk(branch.True);
                    Walk(branch.False);
                    break;
                case AstTryStatement attempt:
                    Walk(attempt.Block);
                    Walk(attempt.Finally);
                    break;
            }
        }

        foreach (var statement in program.Statements)
        {
            Walk(statement switch
            {
                AstForStatement loop => loop.Body,
                AstForInStatement loop => loop.Body,
                AstForOfStatement loop => loop.Body,
                _ => throw new InvalidOperationException("Not a loop: " + statement.Type),
            });
        }

        return statements;
    }

    // The names the pass added to the parse of source: those of the prepared tree the parsed one does not hold.
    private static List<string> Temporaries(string source, AstNode prepared)
        => Names(prepared).Except(Names(Parse(source)), StringComparer.Ordinal).ToList();

    private static List<string> Names(AstNode program)
    {
        var names = new List<string>();
        var collector = new NameCollector(names);
        collector.Visit(program);
        return names;
    }

    // Every identifier's name, in the order the walk meets them, each once.
    private sealed class NameCollector(List<string> names) : AstReduce
    {
        protected override AstNode VisitIdentifier(Broiler.JavaScript.Ast.AstIdentifier identifier)
        {
            var name = identifier.Name.Value;
            if (name != null && !names.Contains(name))
                names.Add(name);
            return identifier;
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }
    }
}

/// <summary>
/// The collection of the tests that replace <c>CoreScript.Compiler</c>, which is process-wide: it runs
/// alone, after the collections that run in parallel.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompilerReplacementCollection
{
    public const string Name = "Replaces CoreScript.Compiler";
}

/// <summary>
/// The depth the temporaries of direct eval code are named by (decision D22), through the real path: the
/// constant the compiler emits for each direct eval, <c>DirectEvalSupport.Execute</c>, and the compile of the
/// eval code. A compiler that wraps the engine's records, for each program compiled, the depth the context
/// holds for it (<c>JSContext.DirectEvalCodeDepth</c>, internal, read by name) and the temporaries the
/// compiled tree declares.
/// </summary>
/// <remarks>
/// The wrapper compiles exactly as the engine's compiler does, and is in place for the length of one test, in
/// a collection that runs alone.
/// </remarks>
[Collection(CompilerReplacementCollection.Name)]
public sealed class ForLoopPreparationDepthTests
{
    private static readonly PropertyInfo DirectEvalCodeDepth = typeof(JSContext)
        .GetProperty("DirectEvalCodeDepth", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly string[] Markers = ["/*S*/", "/*E1*/", "/*E2*/", "/*E3*/", "/*F1*/", "/*I1*/", "/*E4*/"];

    /// <summary>
    /// A script (S) calls a direct eval (E1) that returns a function; the function, called after E1 returned,
    /// calls a direct eval (E2). Direct eval code E3 calls the Function constructor (F1), and an indirect eval
    /// (I1) calls a direct eval (E4). Each program carries its own marker first, and the markers of the code it
    /// compiles later only inside string literals. E2 is two levels deep although only one direct eval runs
    /// when it is compiled: the depth is the compile-time depth of the code that calls <c>eval</c>, plus one.
    /// F1 and I1 are at 0, as no direct eval encloses them, and E4 one deeper than I1.
    /// </summary>
    [Fact]
    public void Direct_eval_code_is_one_level_deeper_than_the_code_that_calls_it_was_compiled()
    {
        const string script = """
            /*S*/ for (const s of [0]) {}
            var f = eval("/*E1*/ for (const a of [1]) {} (function () { return eval('/*E2*/ for (const b of [2]) {} 2'); })");
            var r = [f()];
            r.push(eval("/*E3*/ new Function('/*F1*/ for (const c of [3]) {} return 3')()"));
            r.push((0, eval)("/*I1*/ for (const d of [4]) {} eval('/*E4*/ for (const e of [5]) {} 5')"));
            r.join()
            """;

        var records = new List<(string Marker, int Depth, string[] Temporaries)>();
        var original = CoreScript.Compiler;
        CoreScript.Compiler = new RecordingCompiler(original, records);
        string result;
        try
        {
            using var context = new JSContext();
            result = context.Eval(script).ToString();
        }
        finally
        {
            CoreScript.Compiler = original;
        }

        Assert.Equal("2,3,5", result);

        (int Depth, string Temporary) Expected(string marker) => marker switch
        {
            "/*S*/" => (0, "0:1"),
            "/*E1*/" => (1, "1:1"),
            "/*E2*/" => (2, "2:1"),
            "/*E3*/" => (1, null),
            "/*F1*/" => (0, "0:1"),
            "/*I1*/" => (0, "0:1"),
            "/*E4*/" => (1, "1:1"),
            _ => throw new InvalidOperationException(marker),
        };

        Assert.Equal(Markers.Order(StringComparer.Ordinal), records.Select(static r => r.Marker).Distinct().Order(StringComparer.Ordinal));
        Assert.All(records, record =>
        {
            var (depth, temporary) = Expected(record.Marker);
            Assert.True(depth == record.Depth, record.Marker + " was compiled at depth " + record.Depth + ", not " + depth);
            Assert.Equal(temporary == null ? [] : new[] { temporary }, record.Temporaries);
        });
    }

    // Records each compile of a program that holds a marker: the program's own marker (the first in its text),
    // the depth the context holds as the compiler reads it, and the temporaries the compiled tree declares.
    private sealed class RecordingCompiler(IJSCompiler inner, List<(string Marker, int Depth, string[] Temporaries)> records) : IJSCompiler
    {
        public BExpression<JSFunctionDelegate> Compile(in StringSpan code, string location = null, IList<string> argsList = null, ICodeCache codeCache = null)
        {
            var depth = Depth();
            return Record(code.Value, depth, inner.Compile(code, location, argsList, codeCache));
        }

        public BExpression<JSFunctionDelegate> Compile(in StringSpan code, in JSCompilationOptions options, string location = null, IList<string> argsList = null, ICodeCache codeCache = null)
        {
            var depth = Depth();
            return Record(code.Value, depth, inner.Compile(code, options, location, argsList, codeCache));
        }

        private static int Depth() => JSEngine.Current is JSContext context ? (int)DirectEvalCodeDepth.GetValue(context)! : -1;

        private BExpression<JSFunctionDelegate> Record(string text, int depth, BExpression<JSFunctionDelegate> compiled)
        {
            var marker = Markers
                .Select(m => (Marker: m, Index: text.IndexOf(m, StringComparison.Ordinal)))
                .Where(static m => m.Index >= 0)
                .OrderBy(static m => m.Index)
                .Select(static m => m.Marker)
                .FirstOrDefault();
            if (marker == null)
                return compiled;

            var printed = new StringWriter();
            compiled.Print(new System.CodeDom.Compiler.IndentedTextWriter(printed));
            var temporaries = System.Text.RegularExpressions.Regex.Matches(printed.ToString(), @"JSVariable (\d+:\d+);")
                .Select(static m => m.Groups[1].Value)
                .Distinct()
                .Order(StringComparer.Ordinal)
                .ToArray();
            lock (records)
                records.Add((marker, depth, temporaries));
            return compiled;
        }
    }
}
