using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms;

/// <summary>
/// The comparison of bundle sjl-3-001's reference-arm rule: every operation over its classes, S
/// against every variant of every arm that implements it, exactly. It classifies nothing; the
/// classification is a file written beside its output.
/// </summary>
/// <remarks>
/// <para>
/// <b>Output.</b> <c>summary.json</c> holds, per operation and class, how many inputs each variant
/// ran and how many of them differed from S, the operations an arm does not implement and why, and
/// the population's manifest. <c>differences.tsv.gz</c> holds one line per input on which any
/// variant differs from S: the operation, the class, the input's line in that class, its parameter,
/// the input (escaped as the population is), S's result, and every variant's result, the ones that
/// agree included.
/// </para>
/// <para>
/// <b>A run with a line limit is void</b> (the rule's "a run that stopped before the end of any
/// operation's population"); its summary says so, and it exists to try the comparison out.
/// </para>
/// </remarks>
internal sealed class Comparison(Population population, IReadOnlyList<Operation> operations, int? limit, TextWriter log)
{
    private const int ChunkLines = 16384;

    public void Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var started = Stopwatch.StartNew();
        var summary = new List<Dictionary<string, object?>>();
        using var file = File.Create(Path.Combine(outputDirectory, "differences.tsv.gz"));
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        using var writer = new StreamWriter(gzip, new UTF8Encoding(false)) { NewLine = "\n" };
        writer.WriteLine("operation\tclass\tline\tparameter\tinput\tS\tvariants");

        foreach (var operation in operations)
        {
            foreach (var spec in operation.Classes)
            {
                var lines = population.Lines(spec);
                if (limit is { } most && lines.Count > most)
                    lines = lines.Take(most).ToList();

                var runs = new long[operation.Variants.Length];
                var differs = new long[operation.Variants.Length];
                long sharedRuns = 0;
                var clock = Stopwatch.StartNew();
                for (var first = 0; first < lines.Count; first += ChunkLines)
                {
                    var count = Math.Min(ChunkLines, lines.Count - first);
                    var rows = new List<string>[count];
                    var chunkRuns = new long[count, operation.Variants.Length + 1];
                    var chunkDiffers = new long[count, operation.Variants.Length];
                    Parallel.For(0, count, at =>
                    {
                        var line = first + at;
                        foreach (var input in Inputs(operation, spec, lines[line]))
                        {
                            var shared = Safe(operation.Shared, input);
                            chunkRuns[at, operation.Variants.Length]++;
                            string[]? results = null;
                            var any = false;
                            for (var v = 0; v < operation.Variants.Length; v++)
                            {
                                var variant = operation.Variants[v];
                                if (operation.Name == "NumericLiteral" && variant.Arm == "J" && !Arms.JLiteralApplies(shared))
                                    continue;

                                results ??= new string[operation.Variants.Length];
                                results[v] = Safe(variant.Run, input);
                                chunkRuns[at, v]++;
                                if (!string.Equals(results[v], shared, StringComparison.Ordinal))
                                {
                                    chunkDiffers[at, v]++;
                                    any = true;
                                }
                            }

                            if (any)
                                (rows[at] ??= []).Add(Row(operation, spec, line, input, shared, results!));
                        }
                    });

                    for (var at = 0; at < count; at++)
                    {
                        sharedRuns += chunkRuns[at, operation.Variants.Length];
                        for (var v = 0; v < operation.Variants.Length; v++)
                        {
                            runs[v] += chunkRuns[at, v];
                            differs[v] += chunkDiffers[at, v];
                        }

                        if (rows[at] is { } found)
                            foreach (var row in found)
                                writer.WriteLine(row);
                    }
                }

                log.WriteLine($"{operation.Name} {spec}: {lines.Count} lines, {sharedRuns} inputs, " +
                              string.Join(", ", operation.Variants.Select((variant, v) => $"{variant.Arm}:{variant.Name} {differs[v]} of {runs[v]} differ")) +
                              $" ({clock.Elapsed.TotalSeconds:F1} s)");
                summary.Add(new Dictionary<string, object?>
                {
                    ["operation"] = operation.Name,
                    ["class"] = spec,
                    ["lines"] = lines.Count,
                    ["S"] = sharedRuns,
                    ["variants"] = operation.Variants.Select((variant, v) => new Dictionary<string, object?>
                    {
                        ["arm"] = variant.Arm,
                        ["variant"] = variant.Name,
                        ["ran"] = runs[v],
                        ["differ"] = differs[v],
                    }).ToList(),
                });
            }
        }

        var document = new Dictionary<string, object?>
        {
            ["rule"] = "docs/evidence/sjl-3-001/reference-arms.md",
            ["void"] = limit is not null,
            ["lineLimit"] = limit,
            ["seconds"] = Math.Round(started.Elapsed.TotalSeconds),
            ["stepCap"] = J.NumberParser.StepCap,
            ["population"] = File.Exists(Path.Combine(population.Directory, "manifest.json"))
                ? JsonDocument.Parse(File.ReadAllText(Path.Combine(population.Directory, "manifest.json"))).RootElement
                : null,
            ["notImplemented"] = Arms.NotImplemented.Select(static n => new Dictionary<string, string> { ["arm"] = n.Arm, ["operation"] = n.Operation, ["reason"] = n.Reason }).ToList(),
            ["operations"] = summary,
        };
        File.WriteAllText(Path.Combine(outputDirectory, "summary.json"),
            JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>The inputs one population line gives an operation: one per parameter value, and for a literal one per strictness and BigInt ceiling.</summary>
    public static IEnumerable<Input> Inputs(Operation operation, string spec, string line)
    {
        var isDouble = spec[0] is 'D' or 'T';
        var number = isDouble ? Population.Double(line) : 0;
        var text = isDouble ? string.Empty : line;
        if (operation.Name == "NumericLiteral")
        {
            long[] ceilings = text.EndsWith('n') ? [64, 1024, 65536] : [65536];
            foreach (var strict in (bool[])[false, true])
                foreach (var bits in ceilings)
                    yield return new Input(number, text, 0, false, strict, bits);
            yield break;
        }

        if (operation.Parameters is null)
        {
            yield return new Input(number, text, 0, false, false, 0);
            yield break;
        }

        foreach (var parameter in operation.Parameters)
            yield return new Input(number, text, parameter, true, false, 0);
    }

    private static string Row(Operation operation, string spec, int line, Input input, string shared, string[] results)
    {
        var parameter = operation.Name == "NumericLiteral"
            ? (input.Strict ? "strict" : "sloppy") + "/" + input.Bits.ToString(CultureInfo.InvariantCulture)
            : input.HasParameter ? input.Parameter.ToString(CultureInfo.InvariantCulture) : "-";
        var shown = spec[0] is 'D' or 'T'
            ? BitConverter.DoubleToUInt64Bits(input.Number).ToString("X16", CultureInfo.InvariantCulture)
            : Population.Escape(input.Text);
        var variants = string.Join(" ", operation.Variants.Select((variant, v) =>
            variant.Arm + ":" + variant.Name.Replace(' ', '_') + "=" + (results[v] is null ? "(not called)" : Population.Escape(results[v]))));
        return string.Join('\t', operation.Name, spec, line.ToString(CultureInfo.InvariantCulture), parameter, shown, Population.Escape(shared), variants);
    }

    /// <summary>Runs one implementation, rendering an exception as its type and arm J's step cap as its estimate.</summary>
    public static string Safe(Func<Input, string> run, Input input)
    {
        try
        {
            return run(input);
        }
        catch (J.UnfinishedException unfinished)
        {
            return "unfinished:" + Arms.Render(unfinished.Estimate);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception:" + exception.GetType().Name;
        }
    }
}
