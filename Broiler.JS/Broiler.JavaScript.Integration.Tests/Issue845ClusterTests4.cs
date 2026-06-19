using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845 — three further
// clusters:
//   * Iterator.zip "longest" padding stops calling the padding iterator's next() once it
//     reports done (Problem 35).
//   * yield* does not read the delegated result's `value` while iteration is incomplete
//     (Problem 21).
//   * `delete arguments` inside a `with` deletes the shadowing with-object property
//     (Problem 41).
public class Issue845ClusterTests4
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Iterator.zip padding (Problem 35) ----

    [Fact]
    public void ZipPaddingStopsCallingNextAfterDone()
    {
        // The padding iterable yields once then reports done; zip must not call next()
        // again for the remaining slots (it fills them with undefined).
        const string code = @"
            let nextCalls = 0;
            const padding = {
              [Symbol.iterator]() { return this; },
              next() { nextCalls++; return { done: true, value: undefined }; }
            };
            const r = [...Iterator.zip([[1], [2, 3, 4]], { mode: 'longest', padding })];
            nextCalls;";
        Assert.Equal("1", Eval(code));
    }

    [Theory]
    [InlineData("JSON.stringify([...Iterator.zip([[1,2],[3,4]])])", "[[1,3],[2,4]]")]
    [InlineData("JSON.stringify([...Iterator.zip([[1],[3,4]], {mode:'longest', padding:['x','y']})])", "[[1,3],[\"x\",4]]")]
    public void ZipBasics(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- yield* value not accessed while incomplete (Problem 21) ----

    [Fact]
    public void YieldStarDoesNotReadValueWhenNotDone()
    {
        const string code = @"
            let callCount = 0;
            const spyValue = Object.defineProperty({ done: false }, 'value', { get() { callCount++; } });
            const badIter = { [Symbol.iterator]() { return { next() { return spyValue; } }; } };
            function* g() { yield* badIter; }
            const it = g();
            it.next();
            it.next();
            const incomplete = callCount;        // must still be 0
            spyValue.done = true;
            it.next();
            incomplete + ',' + callCount;";       // 0,1 once it completes
        Assert.Equal("0,1", Eval(code));
    }

    [Fact]
    public void YieldStarStillForwardsValuesAndReturn()
        => Assert.Equal("[1,2,9]",
            Eval("function* inner(){ yield 1; yield 2; return 9; } function* g(){ var r = yield* inner(); yield r; } JSON.stringify([...g()])"));

    // ---- delete arguments within a with block (Problem 41) ----

    [Fact]
    public void DeleteArgumentsInsideWithDeletesShadowingProperty()
        => Assert.Equal("true,false",
            Eval(@"(function(){ var o = { 'arguments': 42 }; var d; with (o) { d = delete arguments; } return d + ',' + ('arguments' in o); })()"));

    [Theory]
    // Outside any `with`, the arguments binding is non-deletable.
    [InlineData("(function(){ var arguments = 42; return delete arguments; })()", "false")]
    [InlineData("(function(){ return delete arguments; })()", "false")]
    public void DeleteArgumentsOutsideWithIsFalse(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
