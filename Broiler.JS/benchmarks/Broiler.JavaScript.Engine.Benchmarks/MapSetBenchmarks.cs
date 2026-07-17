using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class MapSetBenchmarks
{
    private static readonly IReadOnlyDictionary<string, string> Bodies = new Dictionary<string, string>
    {
        ["NumericMap"] = "var c = new Map(); for (var i = 0; i < 256; i++) c.set(i, i); var n = 0; for (var j = 0; j < 256; j++) n += c.get(j); return n;",
        ["ObjectMap"] = "var c = new Map(); var keys = []; for (var i = 0; i < 256; i++) { var k = {}; keys.push(k); c.set(k, i); } var n = 0; for (var j = 0; j < 256; j++) n += c.get(keys[j]); return n;",
        ["MixedMap"] = "var c = new Map(); var o = {}; var keys = [0, 'x', o, 1n, Symbol.for('s'), NaN, -0]; for (var i = 0; i < keys.length; i++) c.set(keys[i], i); var n = 0; for (var j = 0; j < keys.length; j++) n += c.get(keys[j]); return n;",
        ["NumericSet"] = "var c = new Set(); for (var i = 0; i < 256; i++) c.add(i); var n = 0; for (var j = 0; j < 256; j++) if (c.has(j)) n++; return n;",
        ["MutationIteration"] = "var c = new Map([[1, 1], [2, 2], [3, 3]]); var n = 0; for (var entry of c) { n += entry[1]; if (entry[0] === 1) c.set(4, 4); if (entry[0] === 2) c.delete(3); } return n;",
    };

    private JSContext context;
    private JSValue runner;
    private Arguments emptyArguments;

    [Params("NumericMap", "ObjectMap", "MixedMap", "NumericSet", "MutationIteration")]
    public string Scenario { get; set; } = "NumericMap";

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create();
        runner = context.Eval($"(function () {{ {Bodies[Scenario]} }})", $"benchmark-{Scenario}.js");
        emptyArguments = new Arguments(JSUndefined.Value);
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark]
    public JSValue Run()
        => runner.InvokeFunction(in emptyArguments);
}
