using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// The first frame of a JavaScript stack trace is the ENGINE method that raised the error — the
// [CallerMemberName]/[CallerFilePath]/[CallerLineNumber] trio the JSException constructor
// captures — followed by the JavaScript frames.
//
// Every error raised from a lower assembly reaches the Engine through a factory delegate, because
// Runtime and Storage cannot reference the assembly that knows how to build a JSError. While those
// delegates were shaped `Func<string, Exception>` there was nowhere to carry the caller info, so
// the compiler filled it in at the line that WIRED the delegate rather than at the throw. Every
// TypeError the engine could raise then reported the same origin —
//
//     at InitializeFactories:...\Engine\Core\JSValueCoreExtensions.cs:17,1
//
// — the module initializer, which has nothing to do with any failure. That is the shape a report
// of html5test.com arrived in: a `Cannot get property length of undefined` whose only non-JS frame
// named a line that installs a delegate at startup, sending the reader to the wrong file entirely.
//
// The delegates now declare the caller-info parameters themselves (JSErrorFactory /
// JSExceptionFactory), so each throw site records its own position and the initializer forwards
// what it is handed.
public class EngineErrorOriginFrameTests
{
    /// <summary>The first <c>at</c> line of the stack of whatever <paramref name="source"/> throws.</summary>
    /// <remarks>
    /// The source runs as the whole body of its own function so that a leading <c>'use strict'</c>
    /// is a directive prologue. Inlined into the <c>try</c> below it would be an ordinary
    /// expression statement instead, and the cases that need strict mode to throw at all — a
    /// <c>delete</c> of a non-configurable property — would quietly return false.
    /// </remarks>
    private static string OriginFrameOf(string source)
    {
        using var context = new JSContext();
        return context.Eval(
            """
            String((function () {
                try { (function () { SOURCE })(); }
                catch (e) {
                    var frames = e.stack.split('\n').filter(function (l) { return l.indexOf('    at ') === 0; });
                    return frames.length ? frames[0].trim() : 'no frame';
                }
                return 'no throw';
            })())
            """.Replace("SOURCE", source),
            "t.js").ToString();
    }

    // The regression itself. A module initializer runs once at startup and cannot be the origin of
    // anything a script provokes, so naming one is always wrong — whichever of them it names.
    //
    // The cases cover the rewired factory delegates — JSValue.NewTypeError, JSObject.NewTypeError
    // and NewRangeError, JSVariable.NewReferenceErrorFactory, UriHelper.NewURIError,
    // PropertySequence.TypeErrorFactory — plus a few that always reached the Engine directly and
    // so have to stay right.
    [Theory]
    [InlineData("var u; u.foo;")]
    [InlineData("var u; u();")]
    [InlineData("'use strict'; var o = {}; Object.defineProperty(o, 'x', { value: 1, configurable: false }); delete o.x;")]
    [InlineData("decodeURIComponent('%');")]
    [InlineData("{ q; let q; }")]
    [InlineData("new Array(-1);")]
    [InlineData("Object.create(5);")]
    [InlineData("someGlobalThatIsNotDefined;")]
    [InlineData("var o = {}; [...o];")]
    [InlineData("'use strict'; var o = Object.freeze({}); Object.defineProperty(o, 'a', { value: 1 });")]
    public void TheOriginFrameIsNeverAModuleInitializer(string source)
    {
        var frame = OriginFrameOf(source);

        Assert.StartsWith("at ", frame);
        Assert.DoesNotContain("JSValueCoreExtensions", frame);
        Assert.DoesNotContain("EngineAssemblyInitializer", frame);
        Assert.DoesNotContain("PropertySequenceCoreExtensions", frame);
    }

    // Being "not the initializer" is satisfied by any constant, so this pins that the frame tracks
    // the throw site: two failures that reach the SAME factory delegate — both of these go through
    // JSValue.NewTypeError — have to be told apart. Before the fix both read
    // `InitializeFactories:JSValueCoreExtensions.cs:17,1`.
    [Fact(Timeout = 600000)]
    public void DistinctThrowSitesSharingAFactoryReportDistinctOrigins()
    {
        var read = OriginFrameOf("var u; u.foo;");
        var call = OriginFrameOf("var u; u();");

        Assert.NotEqual(read, call);
    }

    // The JavaScript frames are the part a page author can act on, and they sit below the origin
    // frame. A trace that lost them would still pass the assertions above.
    [Fact(Timeout = 600000)]
    public void TheJavaScriptFramesFollowTheOriginFrame()
    {
        using var context = new JSContext();
        var frames = context.Eval(
            """
            (function () {
                function inner() { var u; return u.foo; }
                function outer() { return inner(); }
                try { outer(); } catch (e) { return e.stack; }
            })()
            """,
            "t.js").ToString();

        Assert.Contains("inner", frames);
        Assert.Contains("outer", frames);
    }
}
