using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class ArrayPrimitiveBenchmarks
{
    private const int Count = 1024;

    private JSContext context;
    private JSValue denseArray;
    private JSValue sparseArray;
    private JSValue[] replacementValues;
    private JSValue callbackRunner;
    private Arguments emptyArguments;
    private Arguments copyWithinArguments;
    private Arguments fillArguments;
    private Arguments reverseArguments;

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create();
        denseArray = JSValue.CreateArray(Count);
        replacementValues = new JSValue[Count];
        for (uint i = 0; i < Count; i++)
        {
            denseArray[i] = new JSNumber(i);
            replacementValues[i] = new JSNumber(i + 1);
        }

        sparseArray = JSValue.CreateArray();
        sparseArray[42] = new JSNumber(1);
        sparseArray[1_000_000] = new JSNumber(2);

        callbackRunner = context.Eval(
            "(function () { var a = Array.from({ length: 1024 }, function (_, i) { return i; }); return function () { return a.map(function (x) { return x + 1; }).reduce(function (x, y) { return x + y; }, 0); }; })()",
            "benchmark-array-callback.js");
        emptyArguments = new Arguments(JSUndefined.Value);
        copyWithinArguments = new Arguments(denseArray, new JSNumber(16), JSNumber.Zero, new JSNumber(1000));
        fillArguments = new Arguments(denseArray, JSNumber.One, JSNumber.Zero, new JSNumber(Count));
        reverseArguments = new Arguments(denseArray);
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark(Baseline = true)]
    public JSValue DenseRead()
    {
        var result = JSUndefined.Value;
        for (uint i = 0; i < Count; i++)
            result = denseArray[i];
        return result;
    }

    [Benchmark]
    public JSValue DenseWrite()
    {
        for (uint i = 0; i < Count; i++)
            denseArray[i] = replacementValues[i];
        return denseArray;
    }

    [Benchmark]
    public JSValue SparseHighIndexRead()
        => sparseArray[1_000_000];

    [Benchmark]
    public JSValue MapReduceCallbacks()
        => callbackRunner.InvokeFunction(in emptyArguments);

    [Benchmark]
    public JSValue DenseCopyWithin()
        => JSArray.CopyWithin(in copyWithinArguments);

    [Benchmark]
    public JSValue DenseFill()
        => JSArray.Fill(in fillArguments);

    [Benchmark]
    public JSValue DenseReverse()
        => JSArray.Reverse(in reverseArguments);
}
