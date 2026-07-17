using System;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class ProfileScenarios
{
    private static JSValue sink;

    public static void Run(string scenario, int iterations)
    {
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        switch (scenario.ToLowerInvariant())
        {
            case "context":
                Context(iterations);
                break;
            case "functions":
                Functions(iterations);
                break;
            case "properties":
                Properties(iterations);
                break;
            case "arrays":
                Arrays(iterations);
                break;
            case "parsing":
                Parsing(iterations);
                break;
            case "mapset":
                MapSet(iterations);
                break;
            default:
                throw new ArgumentException(
                    $"Unknown profile scenario '{scenario}'. Expected context, functions, properties, arrays, parsing, or mapset.");
        }

        GC.KeepAlive(sink);
    }

    private static void Context(int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            using var context = BenchmarkContext.Create();
            sink = context[KeyStrings.globalThis];
        }
    }

    private static void Functions(int iterations)
    {
        using var context = BenchmarkContext.Create();
        var function = context.Eval("(function (value) { return value + 1; })", "profile-functions.js");
        var arguments = new Arguments(JSUndefined.Value, new JSNumber(1));
        for (var i = 0; i < iterations * 10_000; i++)
            sink = function.InvokeFunction(in arguments);
    }

    private static void Properties(int iterations)
    {
        using var context = BenchmarkContext.Create();
        var key = KeyStrings.GetOrCreate("value");
        var keyValue = context.Eval("'value'", "profile-property-key.js");
        var target = new JSObject();
        target[key] = new JSNumber(1);
        for (var i = 0; i < iterations * 2_000; i++)
        {
            sink = target[key];
            sink = target.HasProperty(keyValue);
        }
    }

    private static void Arrays(int iterations)
    {
        using var context = BenchmarkContext.Create();
        var array = JSValue.CreateArray(1024);
        for (uint i = 0; i < 1024; i++)
            array[i] = new JSNumber(i);

        for (var iteration = 0; iteration < iterations * 10; iteration++)
        {
            for (uint i = 0; i < 1024; i++)
                sink = array[i];
        }
    }

    private static void Parsing(int iterations)
    {
        const string source = "function calculate(alpha, beta) { let total = alpha; for (let index = 0; index < beta; index++) total += index; return total; } calculate(1, 100);";
        for (var i = 0; i < iterations * 100; i++)
        {
            var span = new StringSpan(source);
            _ = new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();
        }
    }

    private static void MapSet(int iterations)
    {
        using var context = BenchmarkContext.Create();
        var function = context.Eval(
            "(function () { var map = new Map(); var set = new Set(); for (var i = 0; i < 256; i++) { map.set(i, i); set.add(i); } var total = 0; for (var j = 0; j < 256; j++) if (set.has(j)) total += map.get(j); return total; })",
            "profile-map-set.js");
        var arguments = new Arguments(JSUndefined.Value);
        for (var i = 0; i < iterations * 10; i++)
            sink = function.InvokeFunction(in arguments);
    }
}
