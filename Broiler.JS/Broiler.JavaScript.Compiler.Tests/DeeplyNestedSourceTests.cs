using System.Text;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

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
/// <para>
/// <b>Revised at window W2 of milestone SJL-14</b> (bundle <c>sjl-14-001</c>'s corrections, rows R12 and R13 of K10;
/// JSD-0080's decision D27, decision D47): the shared syntax library parses every compile, and it refuses a source that
/// nests past its supported depth (<c>ParseLimits.MaximumSupportedNestingDepth</c>) or whose tree is deeper than its
/// bound (<c>ParseLimits.MaximumTreeDepth</c>) with a SyntaxError, the implementation limit class C3 lists, where the
/// old parser accepted them on a segmented stack. The fixtures that asserted acceptance past those bounds (they read
/// 20 000 operators, 15 000 and 25 000 conditional levels, and 20 000 operators on a 1 MiB thread) now assert, at the
/// same sizes, the refusal, a catchable SyntaxError and never an aborted host, and assert acceptance at a size within
/// the bound. The fixture with 12 000 parentheses and the flat list are unchanged.
/// </para>
/// </remarks>
public class DeeplyNestedSourceTests
{
    // Within ParseLimits.MaximumTreeDepth (10 000): a left-nested chain of this many operators is this deep.
    private const int OperatorsWithinTheBound = 9_000;

    // Within ParseLimits.MaximumSupportedNestingDepth (512): each level nests a conditional in a parenthesis.
    private const int LevelsWithinTheBound = 200;

    private static string BinaryChain(int operators)
    {
        var source = new StringBuilder("function f() { var a = 1", operators * 2 + 32);
        for (var i = 0; i < operators; i++)
            source.Append("+1");
        source.Append("; return a; } f();");
        return source.ToString();
    }

    private static string ConditionalNesting(int levels)
    {
        // Right-nested, so the depth is in the parser's own recursive descent rather than
        // only in the passes that walk the finished tree.
        var source = new StringBuilder("function t(x) { return ", levels * 24 + 64);
        for (var i = 0; i < levels; i++)
            source.Append("x === ").Append(i).Append(" ? ").Append(i).Append(" : (");
        source.Append('0').Append(new string(')', levels)).Append("; } t(7);");
        return source.ToString();
    }

    private static void AssertRefusedForDepth(JSContext ctx, string source)
    {
        var error = Assert.Throws<JSException>(() => ctx.Eval(source));
        Assert.Equal("SyntaxError", error.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Contains("too deep", error.Message);
    }

    [Fact]
    public void DeepBinaryExpressionChain_Compiles()
    {
        using var ctx = new JSContext();
        Assert.Equal(OperatorsWithinTheBound + 1, ctx.Eval(BinaryChain(OperatorsWithinTheBound)).DoubleValue);

        // Row R13: past the library's tree-depth bound the source is refused, as class C3 lists it.
        AssertRefusedForDepth(ctx, BinaryChain(20_000));
    }

    [Fact]
    public void DeepConditionalExpressionNesting_Compiles()
    {
        using var ctx = new JSContext();
        Assert.Equal(7, ctx.Eval(ConditionalNesting(LevelsWithinTheBound)).DoubleValue);

        // Row R12: past the library's supported nesting the source is refused, as class C3 lists it.
        AssertRefusedForDepth(ctx, ConditionalNesting(15_000));
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
        //
        // Row R12, at window W2: the library refuses this depth at the parse, on its own stack, with a SyntaxError,
        // so the host is not aborted and the refusal is catchable.
        const int levels = 25_000;

        using var ctx = new JSContext();
        AssertRefusedForDepth(ctx, ConditionalNesting(levels));
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

    /// <summary>
    /// The first fixture again, on a thread with the 1 MiB stack Windows gives an ordinary one.
    /// </summary>
    /// <remarks>
    /// The fixtures above all run on whatever stack the test host happens to provide, which is
    /// why this class was green on Linux and killed the run on Windows: the guard segments on a
    /// consumed-bytes threshold of 4 MiB, and a 1 MiB thread is gone at about a quarter of it, so
    /// the segmenting branch was simply unreachable there. Eight megabytes of Linux stack hid a
    /// defect that one megabyte of Windows stack did not.
    /// <para>
    /// Pinning the stack size here makes the fixture decisive on every platform rather than on
    /// the one that happened to be smaller, and it does it without touching any process-wide
    /// setting that xUnit's parallel classes would race on. A body compiled on first call is
    /// compiled on the thread that CALLS it, so running the call here is what puts the compiler's
    /// walk on this thread rather than on a CompilationStack worker.
    /// </para>
    /// <para>
    /// Asserted through a captured result rather than by catching: a CLR stack overflow is not a
    /// catchable exception, so before the fix this did not fail, it aborted the process — which
    /// is exactly the failure being pinned.
    /// </para>
    /// </remarks>
    [Fact]
    public void DeepBinaryExpressionChain_OnAOneMebibyteThread_Compiles()
    {
        // Row R13, at window W2: within the library's tree-depth bound (it read 20 000 operators, which the library
        // now refuses, as the first fixture asserts).
        const int operators = OperatorsWithinTheBound;
        const int oneMebibyteStack = 1024 * 1024;

        double result = 0;
        Exception? failure = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    using var ctx = new JSContext();
                    result = ctx.Eval(BinaryChain(operators)).DoubleValue;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            },
            oneMebibyteStack);

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromMinutes(5)), "the compile did not finish");
        Assert.Null(failure);
        Assert.Equal(operators + 1, result);
    }
}
