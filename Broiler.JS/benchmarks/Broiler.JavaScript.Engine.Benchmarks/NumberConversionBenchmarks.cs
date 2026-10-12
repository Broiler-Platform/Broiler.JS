using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// The workloads Broiler.JS's measurement of the shared JavaScript libraries pilot (milestone
/// SJL-3, number conversion) is taken over, committed with its decision rule at milestone SJL-0,
/// before any code the rule judges exists.
/// </summary>
/// <remarks>
/// <para>
/// The rule is <c>docs/evidence/sjl-3-001/decision-rule.md</c>, part B. It compares this engine at
/// its base commit with this engine after the cut-over, never with another engine, and states its
/// repetitions and its tolerance itself. <b>No figure from these workloads belongs anywhere but an
/// evidence bundle.</b>
/// </para>
/// <para>
/// Each workload has a control: the same program minus the operations measured. For the two
/// runtime workloads the loops, values and accumulation are the same and each conversion is
/// replaced by a length read; for the literal workload the same source is compiled with every
/// literal written as <c>0</c>. A difference between a workload and its control attributes cost to
/// the operations; the rule decides on the workload alone.
/// </para>
/// <para>
/// <b>The answers count conversions, not their results.</b> The cut-over corrects results this
/// engine gets wrong today (radix digits beyond <c>long</c>, the whitespace set, long radix
/// strings), so an answer built from the results would change for a correction. Counting each
/// completed conversion keeps the answer the same at the base and after, and a lane that throws or
/// stops early still fails it. <c>--number-conversion-answers</c> prints every answer with the
/// process's effective configuration.
/// </para>
/// <para>
/// The workloads are not changed after they are committed: a change would change the population
/// the rule was written over.
/// </para>
/// </remarks>
public class NumberConversionBenchmarks
{
    /// <summary>Number to string: radix 10, radixes 2, 7, 16 and 36, toFixed, toExponential, toPrecision.</summary>
    private const string ToStringWorkload = """
        (function () {
            var values = [0, -0, 1, -1, 0.1, 0.5, 1 / 3, 2 / 3, 123.456, -987.654321, 1e21, 1.5e-7,
                5e-324, 1.7976931348623157e308, 9007199254740992, 9007199254740994, 2 ** 64, 0.000001,
                1e-7, 255, 4294967295, 3.141592653589793, 2.718281828459045, 1e100, -1e-100];
            var total = 0;
            for (var i = 0; i < 400; i++) {
                for (var j = 0; j < values.length; j++) {
                    var x = values[j] * (1 + i / 1024);
                    total += String(x).length > 0 ? 1 : 0;
                    total += x.toString(2).length > 0 ? 1 : 0;
                    total += x.toString(7).length > 0 ? 1 : 0;
                    total += x.toString(16).length > 0 ? 1 : 0;
                    total += x.toString(36).length > 0 ? 1 : 0;
                    total += x.toFixed(4).length > 0 ? 1 : 0;
                    total += x.toExponential(6).length > 0 ? 1 : 0;
                    total += x.toPrecision(10).length > 0 ? 1 : 0;
                }
            }
            return total;
        })()
        """;

    private const string ToStringControl = """
        (function () {
            var values = [0, -0, 1, -1, 0.1, 0.5, 1 / 3, 2 / 3, 123.456, -987.654321, 1e21, 1.5e-7,
                5e-324, 1.7976931348623157e308, 9007199254740992, 9007199254740994, 2 ** 64, 0.000001,
                1e-7, 255, 4294967295, 3.141592653589793, 2.718281828459045, 1e100, -1e-100];
            var text = "0";
            var total = 0;
            for (var i = 0; i < 400; i++) {
                for (var j = 0; j < values.length; j++) {
                    var x = values[j] * (1 + i / 1024);
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                    total += text.length > 0 ? 1 : 0;
                }
            }
            return total;
        })()
        """;

    /// <summary>String to number: Number, parseFloat, parseInt at radixes 10 and 36.</summary>
    private const string ToNumberWorkload = """
        (function () {
            var strings = ["0", "-0", "1", "  42  ", "\u00a0 7 \ufeff", "3.14159", "-2.5e-3", "1e21",
                "0x1F", "0b1011", "0o777", "Infinity", "-Infinity", "+Infinity", "NaN", "", "12abc",
                "1_000", ".5", "5.", "123456789012345678901234567890", "4.9406564584124654e-324",
                "1.7976931348623157e308", "0x200000000000011", "\u20289007199254740993\u2029"];
            var total = 0;
            for (var i = 0; i < 1000; i++) {
                for (var j = 0; j < strings.length; j++) {
                    var s = strings[j];
                    total += typeof Number(s) === "number" ? 1 : 0;
                    total += typeof parseFloat(s) === "number" ? 1 : 0;
                    total += typeof parseInt(s, 10) === "number" ? 1 : 0;
                    total += typeof parseInt(s, 36) === "number" ? 1 : 0;
                }
            }
            return total;
        })()
        """;

    private const string ToNumberControl = """
        (function () {
            var strings = ["0", "-0", "1", "  42  ", "\u00a0 7 \ufeff", "3.14159", "-2.5e-3", "1e21",
                "0x1F", "0b1011", "0o777", "Infinity", "-Infinity", "+Infinity", "NaN", "", "12abc",
                "1_000", ".5", "5.", "123456789012345678901234567890", "4.9406564584124654e-324",
                "1.7976931348623157e308", "0x200000000000011", "\u20289007199254740993\u2029"];
            var total = 0;
            for (var i = 0; i < 1000; i++) {
                for (var j = 0; j < strings.length; j++) {
                    var s = strings[j];
                    total += typeof s.length === "number" ? 1 : 0;
                    total += typeof s.length === "number" ? 1 : 0;
                    total += typeof s.length === "number" ? 1 : 0;
                    total += typeof s.length === "number" ? 1 : 0;
                }
            }
            return total;
        })()
        """;

    /// <summary>
    /// Every numeric literal form the pilot moves: decimal, exponent, hexadecimal, binary, octal,
    /// separated, a decimal beyond 2^53, the extremes, and BigInt in decimal and hexadecimal.
    /// </summary>
    private static readonly string[] LiteralForms =
    [
        "1", "0.5", "1e21", "0x1F", "0b1011", "0o777", "1_000_000", "123.456e-7", "9007199254740993",
        "5e-324", "1.7976931348623157e308", "0.1", "3.141592653589793", ".5e10", "0x200000000000011",
        "1n", "0x1Fn", "123456789012345678901234567890n",
    ];

    private static readonly string LiteralsWorkload = LiteralSource(LiteralForms);

    private static readonly string LiteralsControl = LiteralSource(Array.ConvertAll(LiteralForms, _ => "0"));

    internal static readonly IReadOnlyDictionary<string, string> Workloads = new Dictionary<string, string>
    {
        ["ToString"] = ToStringWorkload,
        ["ToStringControl"] = ToStringControl,
        ["ToNumber"] = ToNumberWorkload,
        ["ToNumberControl"] = ToNumberControl,
        ["Literals"] = LiteralsWorkload,
        ["LiteralsControl"] = LiteralsControl,
    };

    private JSContext context;
    private string source;
    private bool compileOnly;

    [Params("ToString", "ToStringControl", "ToNumber", "ToNumberControl", "Literals", "LiteralsControl")]
    public string Workload { get; set; } = "ToString";

    [GlobalSetup]
    public void Setup()
    {
        source = Workloads[Workload];
        compileOnly = Workload.StartsWith("Literals", StringComparison.Ordinal);
        context = BenchmarkContext.Create(compileOnly ? new NoCodeCache() : new LocalDictionaryCodeCache());
        if (!compileOnly)
            context.Eval(source, Workload + ".js");
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    /// <summary>
    /// One operation: the runtime workloads evaluate their cached script once; the literal workloads
    /// compile their source once, without a cache, and do not run it.
    /// </summary>
    [Benchmark]
    public object Run()
        => compileOnly
            ? CoreScript.Compile(source, Workload + ".js", codeCache: new NoCodeCache())
            : context.Eval(source, Workload + ".js");

    /// <summary>
    /// Prints each workload's answer, and the effective configuration of this process, as one JSON
    /// object. The literal workloads are compiled and then run once to produce theirs.
    /// </summary>
    public static void WriteAnswers()
    {
        var answers = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, script) in Workloads)
        {
            using var context = BenchmarkContext.Create(new NoCodeCache());
            JSValue value;
            if (name.StartsWith("Literals", StringComparison.Ordinal))
            {
                var compiled = CoreScript.Compile(script, name + ".js", codeCache: new NoCodeCache());
                var arguments = new Arguments(context);
                value = compiled(in arguments);
            }
            else
            {
                value = context.Eval(script, name + ".js");
            }

            answers[name] = value.ToString();
        }

        var configuration = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["framework"] = RuntimeInformation.FrameworkDescription,
            ["rid"] = RuntimeInformation.RuntimeIdentifier,
            ["processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["os"] = RuntimeInformation.OSDescription,
            ["serverGc"] = GCSettings.IsServerGC.ToString(CultureInfo.InvariantCulture),
            ["concurrentGc"] = Convert.ToString(AppContext.GetData("System.GC.Concurrent"), CultureInfo.InvariantCulture) ?? "",
            ["tieredCompilation"] = Convert.ToString(AppContext.GetData("System.Runtime.TieredCompilation"), CultureInfo.InvariantCulture) ?? "",
            ["tieredPgo"] = Convert.ToString(AppContext.GetData("System.Runtime.TieredPGO"), CultureInfo.InvariantCulture) ?? "",
            ["readyToRun"] = Convert.ToString(AppContext.GetData("System.Runtime.ReadyToRun"), CultureInfo.InvariantCulture) ?? "",
            ["vectorHardwareAccelerated"] = Vector.IsHardwareAccelerated.ToString(CultureInfo.InvariantCulture),
            ["processorCount"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
        };

        Console.WriteLine(JsonSerializer.Serialize(new { answers, configuration }));
    }

    private static string LiteralSource(string[] forms)
    {
        var builder = new StringBuilder("(function () { var t = 0;");
        for (var k = 0; k < 40; k++)
            builder.Append(" t += [").Append(string.Join(", ", forms)).Append("].length;");
        return builder.Append(" return t; })()").ToString();
    }
}
