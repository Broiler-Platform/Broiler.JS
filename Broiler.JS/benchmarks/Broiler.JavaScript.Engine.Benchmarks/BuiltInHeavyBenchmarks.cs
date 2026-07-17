using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class BuiltInHeavyBenchmarks
{
    private static readonly IReadOnlyDictionary<string, string> Scripts = new Dictionary<string, string>
    {
        ["RegExpExec"] = """
            (function () {
                var re = /([a-z]+)(\d+)/g;
                var text = "item1 value22 token333 node4444";
                var total = 0;
                for (var i = 0; i < 500; i++) {
                    re.lastIndex = 0;
                    var match;
                    while ((match = re.exec(text)) !== null) {
                        total += match[1].length + match[2].length;
                    }
                }
                return total;
            })()
            """,
        ["IntlNumberFormat"] = """
            (function () {
                var formatter = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });
                var total = 0;
                for (var i = 0; i < 200; i++) {
                    total += formatter.format(1234.56 + i).length;
                }
                return total;
            })()
            """,
        ["IntlDateTimeFormat"] = """
            (function () {
                var formatter = new Intl.DateTimeFormat("en-US", { dateStyle: "medium", timeZone: "UTC" });
                var total = 0;
                for (var i = 0; i < 200; i++) {
                    total += formatter.format(new Date(Date.UTC(2024, 0, 1 + (i % 28)))).length;
                }
                return total;
            })()
            """,
        ["TemporalPlainDate"] = """
            (function () {
                var date = Temporal.PlainDate.from("2024-01-01");
                var total = 0;
                for (var i = 0; i < 200; i++) {
                    total += date.add({ days: i % 28 }).toString().length;
                }
                return total;
            })()
            """,
        ["DateConstruction"] = """
            (function () {
                var total = 0;
                for (var i = 0; i < 1000; i++) {
                    total += new Date(Date.UTC(2024, i % 12, 1 + (i % 28))).getUTCDate();
                }
                return total;
            })()
            """,
    };

    private JSContext context;
    private string script;

    [Params("RegExpExec", "IntlNumberFormat", "IntlDateTimeFormat", "TemporalPlainDate", "DateConstruction")]
    public string Scenario { get; set; } = "RegExpExec";

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
