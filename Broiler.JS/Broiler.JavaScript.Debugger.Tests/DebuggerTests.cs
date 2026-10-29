using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Debugger.Tests;

public class DebuggerTests
{
    [Fact]
    public void V8RemoteObject_FromNumber_HasNumberType()
    {
        using var ctx = new JSContext();
        var num = new JSNumber(42);
        var remote = new V8RemoteObject(num);
        Assert.Equal("number", remote.Type);
    }

    [Fact]
    public void V8RemoteObject_FromString_HasStringType()
    {
        var remote = new V8RemoteObject("test");
        Assert.Equal("string", remote.Type);
    }

    [Fact]
    public void V8CallFrame_DefaultValues()
    {
        var frame = new V8CallFrame();
        Assert.Equal(0, frame.ColumnNumber);
        Assert.Equal(0, frame.LineNumber);
    }

    // The engine's lines and columns are 1-based (SpanLocation); the DevTools protocol's
    // columnNumber is 0-based, so a call indented by two spaces reports column 2. (The line stays
    // 1-based here, as it was before the scanner's columns were made consistent.)
    [Fact]
    public void V8StackTrace_ReportsZeroBasedColumns()
    {
        using var ctx = new JSContext();
        V8StackTrace? trace = null;
        ctx["capture"] = new JSFunction((in Arguments a) =>
        {
            trace = new V8StackTrace(ctx);
            return JSUndefined.Value;
        }, "capture");
        ctx.Eval("function f() {\n  capture();\n}\n  f();", "columns.js");
        Assert.NotNull(trace);
        var frames = string.Join(";", trace!.CallFrames.Select(f => $"{f.FunctionName}@{f.LineNumber}:{f.ColumnNumber}"));
        Assert.Equal("f@2:2;@4:2", frames);
    }

    // An inspector with no connection, so that the Runtime domain can be called directly over the
    // contexts a test gives it.
    private sealed class LocalInspector : V8InspectorProtocol
    {
        public override void Dispose() { }
        public override Task ConnectAsync() => Task.CompletedTask;
        public override void SendMessage(string message) { }

        // Made without running V8InspectorProtocol's constructor, which throws ArgumentException at
        // this commit as at 48fee2d3: it binds every public method of V8Runtime as an instance
        // method, and GetProperties is static. The Runtime domain's compiles read only the two
        // tables set here.
        public static LocalInspector Create(JSContext context)
        {
            var inspector = (LocalInspector)RuntimeHelpers.GetUninitializedObject(typeof(LocalInspector));
            inspector.Contexts = new() { [context.ID] = context };
            inspector.InjectedScripts = new();
            return inspector;
        }
    }

    // What one of the Runtime domain's calls did with the code: ran it, or threw, naming the JavaScript
    // error's constructor.
    private static string Outcome(V8ReturnValue value)
        => value.ExceptionDetails is not { } details ? "ran" : "threw " + (details.Exception?.ClassName ?? "a host exception");

    // Correction J1 of milestone SJL-12.5 (the changelog): Runtime.evaluate, Runtime.compileScript and
    // Runtime.callFunctionOn compile a script without top-level await under the options of the context
    // they target, also when they are called while that context runs a program EvalWithTopLevelAwaitAsync
    // was given. Before, that program's permission was an ambient scope, and their compiles took it.
    [Fact]
    public async Task The_runtime_domain_compiles_without_top_level_await_while_a_program_with_it_runs()
    {
        using var ctx = new JSContext();
        var runtime = new V8Runtime(LocalInspector.Create(ctx));
        var outcomes = new List<string>();

        void Probe(string code, string function)
        {
            var evaluated = runtime.Evaluate(new V8Runtime.EvaluateParams { ContextId = ctx.ID, Expression = code });
            var compiled = runtime.CompileScript(new V8Runtime.CompileScriptParams { ExecutionContextId = ctx.ID, Expression = code });
            var called = runtime.CallFunctionOn(new V8Runtime.CallFunctionOnParams
            {
                ExecutionContextId = ctx.ID,
                FunctionDeclaration = function,
                Arguments = [],
            });
            outcomes.Add($"{code}: {Outcome(evaluated)} {Outcome(compiled)} {Outcome(called)}");
        }

        ctx["probe"] = new JSFunction((in Arguments a) =>
        {
            Probe("await 1", "await 1; (function () { return 3; })");
            Probe("1 + 2", "(function () { return 1 + 2; })");
            return JSUndefined.Value;
        }, "probe");

        Probe("await 1", "await 1; (function () { return 3; })");
        await ctx.EvalWithTopLevelAwaitAsync("probe(); await 0; probe();");

        Assert.Equal(
            new[]
            {
                "await 1: threw SyntaxError threw SyntaxError threw SyntaxError",
                "await 1: threw SyntaxError threw SyntaxError threw SyntaxError",
                "1 + 2: ran ran ran",
                "await 1: threw SyntaxError threw SyntaxError threw SyntaxError",
                "1 + 2: ran ran ran",
            },
            outcomes);
    }
}
