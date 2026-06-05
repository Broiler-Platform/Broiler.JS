using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/673
//
// Category 1 (sm "structurally equal" failures) included the
// test/staging/sm/generators/delegating-yield-* files. The shared root cause was
// that a `yield*` expression evaluated to `undefined`/`null` instead of the
// delegated iterator's return value (the value of its final `{ value, done:true }`
// result). See JSIterator.MoveNext(JSValue, out JSValue) and the generator
// delegation driver in ClrGeneratorV2.Next.
//
// The remaining issue #673 categories (finally abrupt-completion override,
// direct-eval var injection, generator IteratorClose on return(), Unicode
// identifier start coverage, and Intl range formatting) are triaged in
// docs/compliance/triage-issue-673.md and remain open.
public class Issue673Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void YieldStar_EvaluatesToDelegateReturnValue()
        => Assert.Equal("3", Eval(@"
            function* inner(){ yield 1; yield 2; return 3; }
            function* outer(){ var r = yield* inner(); yield r; }
            var it = outer();
            it.next(); it.next();        // 1, 2
            String(it.next().value);     // r === 3 is yielded here
        "));

    [Fact]
    public void YieldStar_ForwardsYieldedValuesThenReturnValue()
        => Assert.Equal("[1,2,3]", Eval(@"
            function* inner(){ yield 1; yield 2; return 3; }
            function* outer(){ var r = yield* inner(); yield r; }
            JSON.stringify([...outer()]);
        "));

    [Fact]
    public void YieldStar_ReturnValueIsUsableInExpression()
        => Assert.Equal("[\"a\",\"got:R\"]", Eval(@"
            function* inner(){ yield 'a'; return 'R'; }
            function* outer(){ var v = yield* inner(); yield 'got:' + v; }
            JSON.stringify([...outer()]);
        "));

    [Fact]
    public void YieldStar_OverArrayHasUndefinedValue()
        => Assert.Equal("undefined", Eval(@"
            function* outer(){ var r = yield* [1, 2, 3]; yield r; }
            var it = outer();
            it.next(); it.next(); it.next();   // 1, 2, 3
            String(it.next().value);           // spreading yielded r === undefined
        "));

    [Fact]
    public void YieldStar_NestedDelegationPropagatesReturnValue()
        => Assert.Equal("inner", Eval(@"
            function* inner(){ yield 1; return 'inner'; }
            function* middle(){ var r = yield* inner(); return r; }
            function* outer(){ var r = yield* middle(); yield r; }
            var it = outer();
            it.next();                   // 1
            String(it.next().value);     // outer yields 'inner'
        "));

    [Fact]
    public void YieldStar_ForwardsSentValuesToInner()
        => Assert.Equal("sent:42", Eval(@"
            function* inner(){ var got = yield 1; return 'sent:' + got; }
            function* outer(){ var r = yield* inner(); yield r; }
            var it = outer();
            it.next();                   // yields 1, pauses at `yield 1` in inner
            String(it.next(42).value);   // sends 42 into inner; inner returns 'sent:42'
        "));
}
