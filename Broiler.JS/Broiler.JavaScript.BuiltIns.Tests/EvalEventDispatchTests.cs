using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Tests;

// JSContext.EvalEvent is how an embedder decides whether a string may be compiled at run time: a
// browser's Content-Security-Policy 'unsafe-eval' check hangs on it. The specification's hook for that
// decision, HostEnsureCanCompileStrings, runs before anything is parsed on every route that turns a
// string into code - direct and indirect eval, every dynamic-function constructor at every arity, and
// ShadowRealm.prototype.evaluate. These tests pin that the event is raised on each of those routes,
// on the context whose policy the specification asks about, before any compile, and that a handler's
// refusal reaches script as the error the handler threw.
//
// Every result is read from a host-side record or from a global through JSContext.Eval, the host's own
// entry point, which raises nothing - never through a route under test.
public class EvalEventDispatchTests
{
    private const string Refusal = "refused-by-test";

    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    /// <summary>Every way script can reach a dynamic-function constructor with no arguments.</summary>
    public static TheoryData<string> ZeroArgumentForms => new()
    {
        "new Function()",
        "Function()",
        "Function.prototype.constructor()",
        "Reflect.construct(Function, [])",
        "new (Function.bind(null))()",
        "new (class extends Function {})()",
        "new (Object.getPrototypeOf(async function () {}).constructor)()",
        "new (Object.getPrototypeOf(function* () {}).constructor)()",
        "new (Object.getPrototypeOf(async function* () {}).constructor)()",
    };

    private sealed record Dispatch(JSContext? Context, string? Script, string? Location);

    private static List<Dispatch> Record(JSContext ctx)
    {
        var seen = new List<Dispatch>();
        ctx.EvalEvent += (_, e) => seen.Add(new Dispatch(e.Context, e.Script, e.Location));
        return seen;
    }

    private static void Refuse(JSContext ctx) =>
        ctx.EvalEvent += (_, _) => throw JSEngine.NewSyntaxError(Refusal);

    private static string Read(JSContext ctx, string global) =>
        ctx.Eval($"String(globalThis.{global})").ToString();

    // -- dynamic functions with no arguments ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(ZeroArgumentForms))]
    public void EveryZeroArgumentFormRaisesEvalEventOnceWithAnEmptyBody(string form)
    {
        Load();
        using var ctx = new JSContext();
        var seen = Record(ctx);

        ctx.Eval($"globalThis.r = typeof ({form});");

        var dispatch = Assert.Single(seen);
        Assert.Same(ctx, dispatch.Context);
        Assert.Equal(string.Empty, dispatch.Script);
        Assert.Null(dispatch.Location);
        Assert.Equal("function", Read(ctx, "r"));
    }

    [Theory]
    [MemberData(nameof(ZeroArgumentForms))]
    public void ARefusingHandlerRefusesEveryZeroArgumentFormCatchably(string form)
    {
        Load();
        using var ctx = new JSContext();
        Refuse(ctx);

        ctx.Eval($$"""
            try { ({{form}}); globalThis.r = 'made'; }
            catch (e) { globalThis.r = e.name + ':' + (e instanceof SyntaxError) + ':' + e.message; }
            """);

        Assert.Equal($"SyntaxError:true:{Refusal}", Read(ctx, "r"));
    }

    [Fact(Timeout = 600000)]
    public void AnUncaughtRefusalOfAZeroArgumentFunctionReachesTheHost()
    {
        Load();
        using var ctx = new JSContext();
        Refuse(ctx);

        var thrown = Assert.ThrowsAny<Exception>(() => ctx.Eval("new Function()"));
        Assert.Contains(Refusal, thrown.Message);
    }

    // The host is asked before GetPrototypeFromConstructor reads newTarget.prototype, which this engine
    // defers until after the function is built.
    [Fact(Timeout = 600000)]
    public void ARefusalComesBeforeNewTargetPrototypeIsRead()
    {
        Load();
        using var ctx = new JSContext();
        Refuse(ctx);

        ctx.Eval("""
            var read = false;
            var newTarget = new Proxy(function () {}, {
                get(target, key) { if (key === 'prototype') read = true; return target[key]; }
            });
            try { Reflect.construct(Function, [], newTarget); globalThis.r = 'made|' + read; }
            catch (e) { globalThis.r = e.name + '|' + read; }
            """);

        Assert.Equal("SyntaxError|false", Read(ctx, "r"));
    }

    // A guard, green before and after the dispatch moved: the argument-less shortcut still builds the
    // same function whether or not anything is subscribed.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroArgumentDynamicFunctionsKeepTheirShape(bool subscribed)
    {
        Load();
        using var ctx = new JSContext();
        if (subscribed)
            _ = Record(ctx);

        ctx.Eval("globalThis.r = new Function().toString();");
        Assert.Equal("function anonymous(\n) {\n\n}", Read(ctx, "r"));

        ctx.Eval("globalThis.r = Object.getPrototypeOf(async function () {}).constructor().toString();");
        Assert.Equal("async function anonymous(\n) {\n\n}", Read(ctx, "r"));

        ctx.Eval("globalThis.r = Object.getPrototypeOf(function* () {}).constructor().toString();");
        Assert.Equal("function* anonymous(\n) {\n\n}", Read(ctx, "r"));

        ctx.Eval("globalThis.r = Object.getPrototypeOf(async function* () {}).constructor().toString();");
        Assert.Equal("async function* anonymous(\n) {\n\n}", Read(ctx, "r"));

        ctx.Eval("globalThis.r = new Function().name + '|' + typeof new Function()();");
        Assert.Equal("anonymous|undefined", Read(ctx, "r"));
    }

    // A handler may rewrite what is compiled on the other routes, so it may here too: a rewritten empty
    // body is compiled like any other body rather than discarded by the shortcut.
    [Fact(Timeout = 600000)]
    public void AZeroArgumentBodyRewrittenByAHandlerIsCompiled()
    {
        Load();
        using var ctx = new JSContext();
        ctx.EvalEvent += (_, e) =>
        {
            if (e.Script == string.Empty)
                e.Script = "return 42";
        };

        ctx.Eval("globalThis.r = new Function()();");
        Assert.Equal("42", Read(ctx, "r"));

        ctx.Eval("globalThis.r = new Function().toString();");
        Assert.Equal("function anonymous(\n) {\nreturn 42\n}", Read(ctx, "r"));
    }

    // -- ShadowRealm.prototype.evaluate ---------------------------------------------------------------

    // Constructing a ShadowRealm compiles nothing and asks nothing; evaluate asks the context that
    // constructed it, which here is also the caller.
    [Fact(Timeout = 600000)]
    public void ShadowRealmEvaluateRaisesEvalEventOnTheContextThatConstructedIt()
    {
        Load();
        using var ctx = new JSContext();
        var seen = Record(ctx);

        ctx.Eval("globalThis.shadowRealm = new ShadowRealm();");
        Assert.Empty(seen);

        ctx.Eval("globalThis.r = shadowRealm.evaluate('6 * 7');");

        var dispatch = Assert.Single(seen);
        Assert.Same(ctx, dispatch.Context);
        Assert.Equal("6 * 7", dispatch.Script);
        Assert.Null(dispatch.Location);
        Assert.Equal("42", Read(ctx, "r"));
    }

    // The specification asks the host about the ShadowRealm's own realm, not about the caller. So a
    // context that calls evaluate on another context's ShadowRealm is answered by the context that built
    // it: a caller that permits compilation cannot compile a string into a ShadowRealm that a refusing
    // context made, and the refusal still reaches the caller as the host threw it.
    [Fact(Timeout = 600000)]
    public void ShadowRealmEvaluateAsksTheContextThatConstructedTheShadowRealm()
    {
        Load();
        using var creator = new JSContext();
        Refuse(creator);
        var foreignShadowRealm = creator.Eval("new ShadowRealm()");

        using var caller = new JSContext();
        var seenByCaller = Record(caller);
        caller["foreignShadowRealm"] = foreignShadowRealm;

        caller.Eval("""
            try { globalThis.r = 'compiled:' + ShadowRealm.prototype.evaluate.call(foreignShadowRealm, '6 * 7'); }
            catch (e) { globalThis.r = e.name + ':' + e.message; }
            """);

        Assert.Equal($"SyntaxError:{Refusal}", Read(caller, "r"));
        Assert.Empty(seenByCaller);
    }

    // The source is deliberately unparseable: a refusal raised after the parse, or inside the parse's
    // catch, would surface as evaluate's own "could not be parsed" SyntaxError instead.
    [Fact(Timeout = 600000)]
    public void ARefusalOfShadowRealmEvaluateReachesTheCallerAsTheHandlerThrewIt()
    {
        Load();
        using var ctx = new JSContext();
        Refuse(ctx);

        ctx.Eval("""
            var shadowRealm = new ShadowRealm();
            try { shadowRealm.evaluate('('); globalThis.r = 'compiled'; }
            catch (e) { globalThis.r = e.name + ':' + (e instanceof SyntaxError) + ':' + e.message; }
            """);

        Assert.Equal($"SyntaxError:true:{Refusal}", Read(ctx, "r"));
    }

    // A handler may rewrite what evaluate compiles, as it may on every other route.
    [Fact(Timeout = 600000)]
    public void AShadowRealmSourceRewrittenByAHandlerIsTheOneEvaluated()
    {
        Load();
        using var ctx = new JSContext();
        ctx.EvalEvent += (_, e) =>
        {
            if (e.Script == "6 * 7")
                e.Script = "6 * 9";
        };

        ctx.Eval("globalThis.r = new ShadowRealm().evaluate('6 * 7');");
        Assert.Equal("54", Read(ctx, "r"));
    }

    // Code already running inside a ShadowRealm asks on the child context, which no embedder can see or
    // subscribe to. The child forwards every question to the context that constructed it, so a handler
    // that lets evaluate's own string through is still asked about each string that string compiles -
    // eval, the Function constructors and a nested ShadowRealm's evaluate - and a selective refusal
    // reaches the code inside the child (JSD-0030 follow-up SR-6).
    [Fact(Timeout = 600000)]
    public void CodeInsideAShadowRealmAsksTheContextThatConstructedIt()
    {
        Load();
        using var ctx = new JSContext();
        var seen = Record(ctx);
        ctx.EvalEvent += (_, e) =>
        {
            if (e.Script is "6 * 7" or "return 6 * 7")
                throw JSEngine.NewSyntaxError(Refusal);
        };

        ctx.Eval("""
            var shadowRealm = new ShadowRealm();
            globalThis.r1 = shadowRealm.evaluate("try { 'compiled:' + (0, eval)('6 * 7'); } catch (e) { e.name + ':' + e.message; }");
            globalThis.r2 = shadowRealm.evaluate("try { 'compiled:' + Function('return 6 * 7')(); } catch (e) { e.name + ':' + e.message; }");
            globalThis.r3 = shadowRealm.evaluate("try { 'compiled:' + new ShadowRealm().evaluate('6 * 7'); } catch (e) { e.name + ':' + e.message; }");
            globalThis.r4 = shadowRealm.evaluate("new ShadowRealm().evaluate(\"(0, eval)('6 * 9')\")");
            """);

        Assert.Equal($"SyntaxError:{Refusal}", Read(ctx, "r1"));
        Assert.Equal($"SyntaxError:{Refusal}", Read(ctx, "r2"));
        Assert.Equal($"SyntaxError:{Refusal}", Read(ctx, "r3"));
        Assert.Equal("54", Read(ctx, "r4"));

        // Every compile was asked about on the embedder's context, the nested ones included.
        Assert.All(seen, dispatch => Assert.Same(ctx, dispatch.Context));
        Assert.Equal(
            new string?[] { "6 * 7", "return 6 * 7", "6 * 7", "(0, eval)('6 * 9')", "6 * 9" },
            seen.Select(dispatch => dispatch.Script).Where(script => !script!.StartsWith("try", StringComparison.Ordinal)
                && !script.StartsWith("new ShadowRealm", StringComparison.Ordinal)));
    }

    // A guard, green before and after: the receiver and argument checks come before the host is asked.
    [Fact(Timeout = 600000)]
    public void ShadowRealmChecksItsReceiverAndArgumentBeforeAskingTheHost()
    {
        Load();
        using var ctx = new JSContext();
        var seen = Record(ctx);
        Refuse(ctx);

        ctx.Eval("""
            try { ShadowRealm.prototype.evaluate.call({}, '1'); globalThis.r1 = 'no error'; }
            catch (e) { globalThis.r1 = e.name; }
            try { new ShadowRealm().evaluate(1); globalThis.r2 = 'no error'; }
            catch (e) { globalThis.r2 = e.name; }
            """);

        Assert.Equal("TypeError", Read(ctx, "r1"));
        Assert.Equal("TypeError", Read(ctx, "r2"));
        Assert.Empty(seen);
    }

    // A permitting handler changes nothing about how evaluate fails: a parse failure is still its own
    // SyntaxError and a throw inside the realm still surfaces as a TypeError.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShadowRealmFailuresKeepTheirShapeUnderAPermittingSubscriber(bool subscribed)
    {
        Load();
        using var ctx = new JSContext();
        var count = 0;
        if (subscribed)
            ctx.EvalEvent += (_, _) => count++;

        ctx.Eval("""
            var shadowRealm = new ShadowRealm();
            try { shadowRealm.evaluate('('); globalThis.r1 = 'compiled'; }
            catch (e) { globalThis.r1 = e.name + ':' + e.message; }
            try { shadowRealm.evaluate('throw 1'); globalThis.r2 = 'returned'; }
            catch (e) { globalThis.r2 = e.name; }
            """);

        Assert.StartsWith("SyntaxError:", Read(ctx, "r1"));
        Assert.Contains("could not be parsed", Read(ctx, "r1"));
        Assert.Equal("TypeError", Read(ctx, "r2"));
        Assert.Equal(subscribed ? 2 : 0, count);
    }

    // -- the contract an embedder relies on -----------------------------------------------------------

    [Theory]
    [InlineData("(0, eval)('1')", "1")]
    [InlineData("eval('1')", "1")]
    [InlineData("new Function('return 1')", "return 1")]
    [InlineData("new Function()", "")]
    [InlineData("new ShadowRealm().evaluate('1')", "1")]
    public void EvalEventReachesEveryRouteThatCompilesAString(string form, string script)
    {
        Load();
        using var ctx = new JSContext();
        var seen = Record(ctx);

        ctx.Eval($"({form});");

        var dispatch = Assert.Single(seen);
        Assert.Same(ctx, dispatch.Context);
        Assert.Equal(script, dispatch.Script);
    }

    // The source is unparseable on every route, so a dispatch moved below the parse would surface as the
    // parser's SyntaxError rather than the handler's. The argument-less and ShadowRealm routes are pinned
    // above.
    [Theory]
    [InlineData("eval('(')")]
    [InlineData("(0, eval)('(')")]
    [InlineData("new Function('(')")]
    [InlineData("new Function('(', '')")]
    public void ARefusalOnTheEvalAndFunctionRoutesComesBeforeTheParse(string form)
    {
        Load();
        using var ctx = new JSContext();
        Refuse(ctx);

        ctx.Eval($$"""
            try { ({{form}}); globalThis.r = 'compiled'; }
            catch (e) { globalThis.r = e.name + ':' + (e instanceof SyntaxError) + ':' + e.message; }
            """);

        Assert.Equal($"SyntaxError:true:{Refusal}", Read(ctx, "r"));
    }
}
