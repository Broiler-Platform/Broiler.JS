using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class BinaryDataBenchmarks
{
    private JSContext context;
    private JSValue dataViewWriter;
    private JSValue typedArrayWriter;
    private Arguments emptyArguments;

    [Params(false, true)]
    public bool LittleEndian { get; set; }

    [Params(0, 1)]
    public int AlignmentOffset { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create();
        var endian = LittleEndian ? "true" : "false";
        dataViewWriter = context.Eval(
            $"(function () {{ var view = new DataView(new ArrayBuffer(8200)); return function () {{ for (var i = 0; i < 1024; i++) view.setFloat64({AlignmentOffset} + i * 8, i + 0.5, {endian}); return view.getFloat64({AlignmentOffset} + 1023 * 8, {endian}); }}; }})()",
            "benchmark-dataview.js");
        typedArrayWriter = context.Eval(
            "(function () { var view = new Float64Array(1024); return function () { for (var i = 0; i < view.length; i++) view[i] = i + 0.5; return view[1023]; }; })()",
            "benchmark-typed-array.js");
        emptyArguments = new Arguments(JSUndefined.Value);
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark(Baseline = true)]
    public JSValue DataViewFloat64Write()
        => dataViewWriter.InvokeFunction(in emptyArguments);

    [Benchmark]
    public JSValue TypedArrayFloat64Write()
        => typedArrayWriter.InvokeFunction(in emptyArguments);
}
