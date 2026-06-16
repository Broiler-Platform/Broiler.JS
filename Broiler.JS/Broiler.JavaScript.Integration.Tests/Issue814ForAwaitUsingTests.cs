using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — `using` /
// `await using` combined with `for await` (async iteration), the last `using` case that
// still produced "Common Language Runtime detected an invalid program".
//
// In `for await`, the per-iteration MoveNext is awaited, so the loop body lives across a
// yield and its tracked completion value `#cv` is lifted into a generator box. A
// synchronous `using` in that body lowers to a value-producing (real, non-yielding)
// try/finally, so block-flattening produced `boxField = (try { … } finally { … })` —
// storing a try/finally's value straight into a field. CLR requires the field's target
// reference on the stack before the value, but the try's finally clears the stack, which
// faulted as an invalid program. FlattenBlocks now spills such a value-producing
// try/catch/finally into a temp before applying the consuming assignment.
public class Issue814ForAwaitUsingTests
{
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void ForAwaitWithSyncUsingHead()
        => Assert.Equal("b,d1,b,d2", Drive(
            "(async function () { var g = []; " +
            "for await (using x of [" +
            "  { [Symbol.dispose]() { g.push('d1'); } }, { [Symbol.dispose]() { g.push('d2'); } }]) { g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void ForAwaitWithSyncUsingInBody()
        => Assert.Equal("b1,d1,b2,d2", Drive(
            "(async function () { var g = []; " +
            "for await (const y of [1, 2]) { using x = { [Symbol.dispose]() { g.push('d' + y); } }; g.push('b' + y); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void ForAwaitWithAwaitUsingHead()
        => Assert.Equal("b,ad", Drive(
            "(async function () { var g = []; " +
            "for await (await using x of [{ [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }]) { g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void ForAwaitWithAwaitUsingInBody()
        => Assert.Equal("b,ad", Drive(
            "(async function () { var g = []; " +
            "for await (const y of [1]) { await using x = { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }; g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void GeneratorValueTryFinallyTailAssignedToLiftedVariable()
        // The general FlattenBlocks fix: a value-producing try/finally whose result becomes
        // a tracked (lifted) completion value across a yield, inside async iteration.
        => Assert.Equal("10", Drive(
            "(async function () { var last; " +
            "for await (const y of [1]) { await Promise.resolve(); last = (function () { try { return 10; } finally {} })(); } " +
            "globalThis.r = last; })();"));
}
