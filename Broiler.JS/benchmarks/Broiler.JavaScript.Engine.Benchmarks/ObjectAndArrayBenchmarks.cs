using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class ObjectAndArrayBenchmarks
{
    private static readonly IReadOnlyDictionary<string, string> Scripts = new Dictionary<string, string>
    {
        ["PropertyGetSet"] = """
            (function () {
                var obj = { a: 1, b: 2, c: 3 };
                var total = 0;
                for (var i = 0; i < 1000; i++) {
                    obj.a = i;
                    obj.b = obj.a + obj.c;
                    total += obj.b;
                }
                return total;
            })()
            """,
        ["ObjectKeysValues"] = """
            (function () {
                var obj = {};
                for (var i = 0; i < 128; i++) {
                    obj["p" + i] = i;
                }

                var total = 0;
                for (var r = 0; r < 100; r++) {
                    var keys = Object.keys(obj);
                    var values = Object.values(obj);
                    total += keys.length + values[values.length - 1];
                }
                return total;
            })()
            """,
        ["ObjectSpreadRest"] = """
            (function () {
                var source = { a: 1, b: 2, c: 3, d: 4 };
                var total = 0;
                for (var i = 0; i < 200; i++) {
                    var copy = { z: i, ...source, ["k" + i]: i };
                    var { a, ...rest } = copy;
                    total += rest.b + rest.d + Object.keys(rest).length;
                }
                return total;
            })()
            """,
        ["SparseArrayObjectSpread"] = """
            (function () {
                var array = [];
                array[42] = 2;
                array[1000000] = 7;

                var total = 0;
                for (var i = 0; i < 100; i++) {
                    var copy = { ...array };
                    total += copy[42] + copy[1000000] + Object.keys(copy).length;
                }
                return total;
            })()
            """,
        ["ArrayIterationCallbacks"] = """
            (function () {
                var array = Array.from({ length: 256 }, function (_, i) { return i; });
                var total = 0;
                for (var r = 0; r < 100; r++) {
                    total += array
                        .map(function (x) { return x + 1; })
                        .filter(function (x) { return (x & 1) === 0; })
                        .reduce(function (a, b) { return a + b; }, 0);
                }
                return total;
            })()
            """,
    };

    private JSContext context;
    private string script;

    [Params("PropertyGetSet", "ObjectKeysValues", "ObjectSpreadRest", "SparseArrayObjectSpread", "ArrayIterationCallbacks")]
    public string Scenario { get; set; } = "PropertyGetSet";

    [GlobalSetup]
    public void Setup()
    {
        script = Scripts[Scenario];
        context = BenchmarkContext.Create(new LocalDictionaryCodeCache());
        context.Eval(script, Scenario + ".js");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        context?.Dispose();
    }

    [Benchmark]
    public JSValue EvalScenario()
        => context.Eval(script, Scenario + ".js");
}
