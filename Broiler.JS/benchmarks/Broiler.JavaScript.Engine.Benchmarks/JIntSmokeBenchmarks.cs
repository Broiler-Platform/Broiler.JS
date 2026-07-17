using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class JIntSmokeBenchmarks
{
    private const string DromaeoHelpers = """
        var startTest = function () { };
        var test = function (name, fn) { fn(); };
        var endTest = function () { };
        var prep = function (fn) { fn(); };
        """;

    private string script;

    [Params(
        "minimal",
        "evaluation",
        "array-stress",
        "linq-js",
        "stopwatch",
        "dromaeo-3d-cube",
        "dromaeo-core-eval",
        "dromaeo-object-array",
        "dromaeo-object-regexp",
        "dromaeo-object-string",
        "dromaeo-string-base64")]
    public string Scenario { get; set; } = "minimal";

    [GlobalSetup]
    public void Setup()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Scripts", Scenario + ".js");
        script = File.ReadAllText(path);
        if (Scenario.Contains("dromaeo", StringComparison.Ordinal))
            script = DromaeoHelpers + Environment.NewLine + Environment.NewLine + script;
    }

    [Benchmark]
    public JSValue ExecuteScriptInFreshContext()
    {
        using var context = BenchmarkContext.Create(new LocalDictionaryCodeCache());
        return context.Eval(script, Scenario + ".js", context);
    }
}
