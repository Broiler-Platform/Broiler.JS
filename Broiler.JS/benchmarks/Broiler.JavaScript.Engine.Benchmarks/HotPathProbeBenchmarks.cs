using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// The execution-performance roadmap's Appendix A probe corpus, as repeatable benchmarks.
/// <para>
/// Every P0/P1/P2/P3 figure quoted in <c>docs/performance-roadmap.md</c> came from an ad-hoc
/// in-process harness that lived outside the repository, which is why none of them is
/// acceptance evidence. These are the same scenarios, at the same iteration counts, so the
/// documented numbers can be reproduced and regressions caught.
/// </para>
/// <para>
/// Two deliberate differences from Appendix A. It timed a *second* <c>Eval</c> of each script;
/// here each script is evaluated once in <see cref="GlobalSetup"/> to yield a function value,
/// and the benchmark invokes that function — so compilation is excluded outright rather than
/// being made negligible by a code-cache hit. And allocation comes from BenchmarkDotNet's
/// <c>MemoryDiagnoser</c> rather than <c>GC.GetAllocatedBytesForCurrentThread()</c> deltas.
/// Both measure the loop body, which is what the probes were reading.
/// </para>
/// <para>
/// Counts are Appendix A's and are load-bearing — a probe at a different count is not
/// comparable to the figure it is being checked against. The empty-loop probe is the floor
/// every other per-iteration number is quoted against.
/// </para>
/// </summary>
public class HotPathProbeBenchmarks
{
    // ---- 3M-iteration probes: the loop floor, arithmetic, and property access ----

    private const string LoopEmptySource = """
        (function () { var s = 0; for (var i = 0; i < 3000000; i++) { s = i; } return s; })
        """;

    private const string ArithAddSource = """
        (function () { var s = 0; for (var i = 0; i < 3000000; i++) { s = s + i; } return s; })
        """;

    private const string PropOwnGetSource = """
        (function () { var o = { x: 1, y: 2 }; var s = 0; for (var i = 0; i < 3000000; i++) { s = s + o.x; } return s; })
        """;

    private const string PropOwnSetSource = """
        (function () { var o = { x: 1 }; for (var i = 0; i < 3000000; i++) { o.x = i; } return o.x; })
        """;

    private const string ClassFieldSource = """
        (function () { class C { constructor(v) { this.v = v; } } var c = new C(1); var s = 0; for (var i = 0; i < 3000000; i++) { s = s + c.v; } return s; })
        """;

    // ---- 1M-iteration probes: the call paths ----

    private const string FnCallSource = """
        (function () { function f(a) { return a; } var s = 0; for (var i = 0; i < 1000000; i++) { s = s + f(i); } return s; })
        """;

    private const string FnCallStrictSource = """
        (function () { 'use strict'; function f(a) { return a; } var s = 0; for (var i = 0; i < 1000000; i++) { s = s + f(i); } return s; })
        """;

    // `k` is named by a nested function, so it is deliberately ineligible for the P2-2
    // item-3 unboxed-locals gate. That is the point of this probe against fn-call.
    private const string ClosureCallSource = """
        (function () { var k = 1; var f = function (a) { return a + k; }; var s = 0; for (var i = 0; i < 1000000; i++) { s = s + f(i); } return s; })
        """;

    private const string ProtoMethodCallSource = """
        (function () { function P(v) { this.v = v; } P.prototype.get = function () { return this.v; }; var p = new P(1); var s = 0; for (var i = 0; i < 1000000; i++) { s = s + p.get(); } return s; })
        """;

    private const string BuiltinCallSource = """
        (function () { var s = 0; for (var i = 0; i < 1000000; i++) { s = Math.max(s, i); } return s; })
        """;

    private const string ArrayReadWriteSource = """
        (function () { var a = new Array(1000); for (var j = 0; j < 1000; j++) { a[j] = j; } var s = 0; for (var i = 0; i < 1000000; i++) { s = s + a[i % 1000]; } return s; })
        """;

    // ---- Allocation-shaped probes ----

    private const string ObjectAllocSource = """
        (function () { var last = null; for (var i = 0; i < 500000; i++) { last = { a: i, b: i + 1, c: i + 2 }; } return last.c; })
        """;

    private const string ArrayPushSource = """
        (function () { var a = []; for (var i = 0; i < 500000; i++) { a.push(i); } return a.length; })
        """;

    private const string StringConcatSource = """
        (function () { var s = ''; for (var i = 0; i < 200000; i++) { s = 'x' + i; } return s.length; })
        """;

    // Not in Appendix A's list, but it is the probe P2-4 is quoted on (1 604 ms / 3.20 GB
    // before, 10.7 ms / 4.4 MB after). Repeated accumulation was quadratic; keep it pinned.
    private const string StringConcatAccumulateSource = """
        (function () { var s = ''; for (var i = 0; i < 20000; i++) { s = s + 'x'; } return s.length; })
        """;

    private JSContext context;
    private Arguments noArguments;

    private JSValue loopEmpty;
    private JSValue arithAdd;
    private JSValue propOwnGet;
    private JSValue propOwnSet;
    private JSValue classField;
    private JSValue fnCall;
    private JSValue fnCallStrict;
    private JSValue closureCall;
    private JSValue protoMethodCall;
    private JSValue builtinCall;
    private JSValue arrayReadWrite;
    private JSValue objectAlloc;
    private JSValue arrayPush;
    private JSValue stringConcat;
    private JSValue stringConcatAccumulate;

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create();
        noArguments = new Arguments(JSUndefined.Value);

        loopEmpty = Compile(LoopEmptySource, "loop-empty.js");
        arithAdd = Compile(ArithAddSource, "arith-add.js");
        propOwnGet = Compile(PropOwnGetSource, "prop-own-get.js");
        propOwnSet = Compile(PropOwnSetSource, "prop-own-set.js");
        classField = Compile(ClassFieldSource, "class-field.js");
        fnCall = Compile(FnCallSource, "fn-call.js");
        fnCallStrict = Compile(FnCallStrictSource, "fn-call-strict.js");
        closureCall = Compile(ClosureCallSource, "closure-call.js");
        protoMethodCall = Compile(ProtoMethodCallSource, "proto-method-call.js");
        builtinCall = Compile(BuiltinCallSource, "builtin-call.js");
        arrayReadWrite = Compile(ArrayReadWriteSource, "array-rw.js");
        objectAlloc = Compile(ObjectAllocSource, "obj-alloc.js");
        arrayPush = Compile(ArrayPushSource, "array-push.js");
        stringConcat = Compile(StringConcatSource, "string-concat.js");
        stringConcatAccumulate = Compile(StringConcatAccumulateSource, "string-concat-accumulate.js");
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    /// <summary>
    /// The empty-loop floor. Every other probe's per-iteration cost is quoted against this,
    /// so a change here reprices the whole table rather than just this row.
    /// </summary>
    [Benchmark(Baseline = true)]
    public JSValue LoopEmpty() => loopEmpty.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ArithAdd() => arithAdd.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue PropertyOwnGet() => propOwnGet.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue PropertyOwnSet() => propOwnSet.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ClassField() => classField.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue FunctionCall() => fnCall.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue FunctionCallStrict() => fnCallStrict.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ClosureCall() => closureCall.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue PrototypeMethodCall() => protoMethodCall.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue BuiltInCall() => builtinCall.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ArrayReadWrite() => arrayReadWrite.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ObjectAllocation() => objectAlloc.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue ArrayPush() => arrayPush.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue StringConcat() => stringConcat.InvokeFunction(in noArguments);

    [Benchmark]
    public JSValue StringConcatAccumulate() => stringConcatAccumulate.InvokeFunction(in noArguments);

    private JSValue Compile(string source, string fileName) => context.Eval(source, fileName);
}
