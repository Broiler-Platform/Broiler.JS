using System.Reflection;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Compiler.Tests;

/// <summary>
/// The compiler's for-loop pass, <c>ForLoopPreparation</c> (milestone SJL-12.5, item 5; Broiler.VM's
/// JSD-0080, decision D22): the entry point the bundle's decision rule names, the parser's
/// <c>ContainsContinue</c> it reads, the shape it emits, and the temporaries' names.
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

        Assert.Equal(new[] { Name(1), Name(2), Name(3) }, Temporaries(program).Order(StringComparer.Ordinal));

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
    /// temporary's name is never an identifier.
    /// </summary>
    [Fact]
    public void The_temporaries_do_not_depend_on_what_was_prepared_before()
    {
        const string source = "for (let [a, b] = [1, 2]; a < b; a++) { } for (let m = 0; m < 1; m++, (() => m)) { }";
        var first = Temporaries(Prepared(source));
        _ = Prepared("for (let q = 0; q < 1; q++) { for (let r = 0; r < 1; r++) {} }");
        var second = Temporaries(Prepared(source));

        Assert.NotEmpty(first);
        Assert.Equal(first, second);
        Assert.All(first, static name => Assert.Contains(':', name));
    }

    /// <summary>
    /// Direct eval code is prepared one level deeper than the code that calls it, so loops in both, with
    /// carriers in one variable environment, keep their own values.
    /// </summary>
    [Theory]
    [InlineData("(function () { var r = []; var n = 0; for (const x = 7; ;) { r.push(x); if (++n > 2) break; eval(\"for (const y of ['a']) {}\"); } return r.join(); })()", "7,7,7")]
    [InlineData("(function () { var s = ''; for (let i = 0; i < 2; i++) { eval(\"for (let j = 0; j < 2; j++) { s += i + '' + j + ','; }\"); } return s; })()", "00,01,10,11,")]
    [InlineData("(function () { var s = ''; for (const x of [1, 2]) { eval(\"for (const y of [3]) { s += x + '' + y; }\"); } return s; })()", "1323")]
    public void Loops_in_direct_eval_code_keep_their_values_beside_the_callers(string source, string expected)
    {
        using var context = new JSContext();
        Assert.Equal(expected, context.Eval(source).ToString());
    }

    private static List<string> Temporaries(AstNode program)
    {
        var names = new List<string>();
        var collector = new TemporaryCollector(names);
        collector.Visit(program);
        return names;
    }

    // Every identifier whose name holds a colon, in the order the walk meets them, each once.
    private sealed class TemporaryCollector(List<string> names) : AstReduce
    {
        protected override AstNode VisitIdentifier(Broiler.JavaScript.Ast.AstIdentifier identifier)
        {
            var name = identifier.Name.Value;
            if (name != null && name.Contains(':') && !names.Contains(name))
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
