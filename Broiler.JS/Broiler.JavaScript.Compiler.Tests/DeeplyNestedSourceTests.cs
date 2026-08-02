using System.Text;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

/// <summary>
/// Fixtures for the front end's stack depth. What puts it at risk is source *nesting*,
/// not source size: the scanner, the parser, the syntax validator, the compiler's
/// visitors and the IL emitter each recurse once per level, so a deeply nested expression
/// consumes the compiling thread's stack in proportion to its depth while a long flat
/// statement list does not.
/// </summary>
/// <remarks>
/// These assert a value rather than an exception because the failure mode being pinned is
/// not a thrown error — it is the test host *aborting*. A stack overflow is not a
/// catchable exception on .NET, so a regression here does not fail a test, it kills the
/// run. Sizes are chosen to sit above the depth an ordinary thread's stack survives
/// (roughly 1 200 levels on a 1 MiB Windows thread, 9 700 on an 8 MiB Linux one) and well
/// below what <c>CompilationStack</c> provides, so the fixtures are decisive on both.
/// </remarks>
public class DeeplyNestedSourceTests
{
    [Fact]
    public void DeepBinaryExpressionChain_Compiles()
    {
        const int operators = 20_000;

        var source = new StringBuilder("function f() { var a = 1", operators * 2 + 32);
        for (var i = 0; i < operators; i++)
            source.Append("+1");
        source.Append("; return a; } f();");

        using var ctx = new JSContext();
        Assert.Equal(operators + 1, ctx.Eval(source.ToString()).DoubleValue);
    }

    [Fact]
    public void DeepConditionalExpressionNesting_Compiles()
    {
        // Right-nested, so the depth is in the parser's own recursive descent rather than
        // only in the passes that walk the finished tree.
        const int levels = 15_000;

        var source = new StringBuilder("function t(x) { return ", levels * 24 + 64);
        for (var i = 0; i < levels; i++)
            source.Append("x === ").Append(i).Append(" ? ").Append(i).Append(" : (");
        source.Append('0').Append(new string(')', levels)).Append("; } t(7);");

        using var ctx = new JSContext();
        Assert.Equal(7, ctx.Eval(source.ToString()).DoubleValue);
    }

    [Fact]
    public void DeepConditionalExpressionNesting_PastTheCompilationStack_Compiles()
    {
        // The fixture above at 15 000 levels passed before the PARSER was guarded, because a
        // 64 MiB compilation worker absorbs it. This one does not: measured on this engine the
        // parser's descent costs ~2.7 KB a level, so 25 000 levels wants ~67 MiB and the
        // process ABORTED here — 20 000 completed, 25 000 did not, with no exception to catch
        // because a CLR stack overflow is not one.
        //
        // Sized just past that edge rather than far past it, so it stays a fixture rather than
        // a soak: it is the smallest depth that was fatal, and it is decisive without touching
        // CompilationStack.SizeBytes — a process-wide static xUnit's parallel classes would
        // race on, which is why item 1-2's guard-alone row is a manual result and this is not.
        const int levels = 25_000;

        var source = new StringBuilder("function t(x) { return ", levels * 24 + 64);
        for (var i = 0; i < levels; i++)
            source.Append("x === ").Append(i).Append(" ? ").Append(i).Append(" : (");
        source.Append('0').Append(new string(')', levels)).Append("; } t(7);");

        using var ctx = new JSContext();
        Assert.Equal(7, ctx.Eval(source.ToString()).DoubleValue);
    }

    [Fact]
    public void LongFlatStatementList_Compiles()
    {
        // The size case, kept deliberately: it already passed before nesting was addressed,
        // and pinning that is what stops "the compiler recurses over long input" from being
        // re-diagnosed from source length again.
        const int statements = 20_000;

        var source = new StringBuilder("function g() { var a = 0;", statements * 12 + 32);
        for (var i = 0; i < statements; i++)
            source.Append(" a = a + 1;");
        source.Append(" return a; } g();");

        using var ctx = new JSContext();
        Assert.Equal(statements, ctx.Eval(source.ToString()).DoubleValue);
    }

    [Fact]
    public void SyntaxErrorInDeeplyNestedSource_ReportsAsOneInShallowSource()
    {
        // Compiling off the calling thread must not change what compiling *reports*. The
        // exception is captured and rethrown rather than wrapped, so the type an embedder
        // catches for a rejected script is the same whether the source was deep enough to
        // need another stack or not.
        const int levels = 12_000;

        var deep = new StringBuilder(levels * 2 + 32);
        deep.Append("var a = ").Append('(', levels).Append('1').Append(')', levels - 1).Append(';');

        using var ctx = new JSContext();
        var shallow = Assert.ThrowsAny<Exception>(() => ctx.Eval("var a = (1;"));
        var nested = Assert.ThrowsAny<Exception>(() => ctx.Eval(deep.ToString()));

        Assert.Equal(shallow.GetType(), nested.GetType());
    }
}
