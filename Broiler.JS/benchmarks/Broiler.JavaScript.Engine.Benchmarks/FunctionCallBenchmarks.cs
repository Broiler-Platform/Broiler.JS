using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class FunctionCallBenchmarks
{
    private JSContext sameRealmContext;
    private JSContext otherRealmContext;
    private JSValue sloppyFunction;
    private JSValue strictFunction;
    private JSValue nativeFunction;
    private JSValue crossRealmFunction;
    private JSValue recursiveFunction;
    private JSValue callbackFunction;
    private JSValue tailCallFunction;
    private Arguments arguments;
    private Arguments recursiveArguments;
    private Arguments oneArgument;

    [Params(0, 1, 4, 5, 16)]
    public int ArgumentCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        otherRealmContext = BenchmarkContext.Create();
        crossRealmFunction = CompileFunction(otherRealmContext, "return a0 === undefined ? 0 : a0;");

        sameRealmContext = BenchmarkContext.Create();
        sloppyFunction = CompileFunction(sameRealmContext, "return a0 === undefined ? 0 : a0;");
        strictFunction = CompileFunction(sameRealmContext, "'use strict'; return a0 === undefined ? 0 : a0;");
        nativeFunction = JSValue.CreateFunction((in Arguments a) => a.Length == 0 ? new JSNumber(0) : a.GetAt(0));
        recursiveFunction = sameRealmContext.Eval("(function f(n) { return n === 0 ? 0 : f(n - 1) + 1; })", "benchmark-recursive.js");
        callbackFunction = sameRealmContext.Eval("(function () { return [1, 2, 3, 4].map(function (x) { return x + 1; })[3]; })", "benchmark-callback.js");
        tailCallFunction = sameRealmContext.Eval("(function () { function target(v) { return v + 1; } return function (v) { return target(v); }; })()", "benchmark-tail-call.js");

        var values = new JSValue[ArgumentCount];
        for (var i = 0; i < values.Length; i++)
            values[i] = new JSNumber(i + 1);

        arguments = values.Length == 0
            ? new Arguments(JSUndefined.Value)
            : new Arguments(JSUndefined.Value, values);
        recursiveArguments = new Arguments(JSUndefined.Value, new JSNumber(32));
        oneArgument = new Arguments(JSUndefined.Value, new JSNumber(1));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        sameRealmContext?.Dispose();
        otherRealmContext?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public JSValue ScriptSloppy()
        => sloppyFunction.InvokeFunction(in arguments);

    [Benchmark]
    public JSValue ScriptStrict()
        => strictFunction.InvokeFunction(in arguments);

    [Benchmark]
    public JSValue Native()
        => nativeFunction.InvokeFunction(in arguments);

    [Benchmark]
    public JSValue ScriptCrossRealm()
        => crossRealmFunction.InvokeFunction(in arguments);

    [Benchmark]
    public JSValue Recursive()
        => recursiveFunction.InvokeFunction(in recursiveArguments);

    [Benchmark]
    public JSValue Callback()
        => callbackFunction.InvokeFunction(new Arguments(JSUndefined.Value));

    [Benchmark]
    public JSValue TailCall()
        => tailCallFunction.InvokeFunction(in oneArgument);

    private static JSValue CompileFunction(JSContext context, string body)
        => context.Eval(
            $"(function (a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) {{ {body} }})",
            "benchmark-function.js");
}
