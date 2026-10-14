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

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;

/// <summary>
/// The comparison of bundle sjl-4-001's reference-arm rule: every operation over its classes, every
/// variant of every arm against S called as that arm's engine will call it, exactly. It classifies
/// nothing; the classification is scripts/conformance/classify_numerics_arms.py, run on its output.
/// </summary>
/// <remarks>
/// <para>
/// <b>Output.</b> <c>summary.json</c> holds, per operation and class, the lines read, the inputs they
/// gave, and per variant how many it ran, how many differed from S and how many it was not called
/// for; the operations an arm does not implement and why; and the population's manifest.
/// <c>differences.tsv.gz</c> holds one line per input and variant that differ: the operation, the
/// class, the line, the parameter and derived input (<see cref="NumericsInput.Detail"/>), the line as
/// the population writes it, the variant, S's result and the variant's result.
/// </para>
/// <para>
/// <b>A run with a line limit, or an operation filter, is void</b> (the rule's "a run that stopped
/// before the end of any operation's population"); its summary says so.
/// </para>
/// </remarks>
internal sealed class NumericsComparison(NumericsPopulation population, IReadOnlyList<NumericsOperation> operations, long? limit, bool filtered, TextWriter log)
{
    private const int ChunkLines = 4096;

    public void Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        NumericsArms.DrawnFrom = population.Values.Length - NumericsArms.B1Drawn;
        var compared = population.Compared;
        NumericsArms.ComparedSource = input => Enumerable.Range(0, compared.Length)
            .Where(at => double.IsFinite(compared[at]))
            .Select(at => input with { A = compared[at], Parameter = at, Detail = "B1C:" + at.ToString(CultureInfo.InvariantCulture) });

        var started = Stopwatch.StartNew();
        var summary = new List<Dictionary<string, object?>>();
        using var file = File.Create(Path.Combine(outputDirectory, "differences.tsv.gz"));
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        using var writer = new StreamWriter(gzip, new UTF8Encoding(false)) { NewLine = "\n" };
        writer.WriteLine("operation\tclass\tline\tdetail\tinput\tvariant\tS\tresult");

        foreach (var operation in operations)
        {
            var shared = operation.Variants.Select(v => v.Shared).Distinct().ToArray();
            var sharedIndex = operation.Variants.Select(v => Array.IndexOf(shared, v.Shared)).ToArray();
            foreach (var name in operation.Classes)
            {
                var clock = Stopwatch.StartNew();
                long lines = 0, inputs = 0;
                var ran = new long[operation.Variants.Length];
                var differ = new long[operation.Variants.Length];
                var skipped = new long[operation.Variants.Length];
                foreach (var chunk in population.Chunks(name, ChunkLines))
                {
                    var take = limit is { } most ? (int)Math.Min(chunk.Count, Math.Max(0, most - lines)) : chunk.Count;
                    if (take == 0)
                        break;

                    var rows = new List<string>?[take];
                    var counts = new long[take, (3 * operation.Variants.Length) + 1];
                    Parallel.For(0, take, at =>
                    {
                        foreach (var input in operation.Expand(chunk[at]))
                        {
                            counts[at, 3 * operation.Variants.Length]++;
                            var sharedResults = new string?[shared.Length];
                            for (var v = 0; v < operation.Variants.Length; v++)
                            {
                                var variant = operation.Variants[v];
                                if (variant.Applies is { } applies && !applies(input))
                                {
                                    counts[at, (3 * v) + 2]++;
                                    continue;
                                }

                                var s = sharedResults[sharedIndex[v]] ??= Safe(variant.Shared, input);
                                var result = Safe(variant.Run, input);
                                counts[at, 3 * v]++;
                                if (!string.Equals(result, s, StringComparison.Ordinal))
                                {
                                    counts[at, (3 * v) + 1]++;
                                    (rows[at] ??= []).Add(Row(operation, input, variant, s, result));
                                }
                            }
                        }
                    });

                    for (var at = 0; at < take; at++)
                    {
                        inputs += counts[at, 3 * operation.Variants.Length];
                        for (var v = 0; v < operation.Variants.Length; v++)
                        {
                            ran[v] += counts[at, 3 * v];
                            differ[v] += counts[at, (3 * v) + 1];
                            skipped[v] += counts[at, (3 * v) + 2];
                        }

                        if (rows[at] is { } found)
                            foreach (var row in found)
                                writer.WriteLine(row);
                    }

                    lines += take;
                }

                log.WriteLine($"{operation.Name} {name}: {lines} lines, {inputs} inputs, " +
                              string.Join(", ", operation.Variants.Select((variant, v) => $"{variant.Arm}:{variant.Name} {differ[v]} of {ran[v]} differ ({skipped[v]} not called)")) +
                              $" ({clock.Elapsed.TotalSeconds:F1} s)");
                log.Flush();
                summary.Add(new Dictionary<string, object?>
                {
                    ["operation"] = operation.Name,
                    ["class"] = name,
                    ["lines"] = lines,
                    ["inputs"] = inputs,
                    ["variants"] = operation.Variants.Select((variant, v) => new Dictionary<string, object?>
                    {
                        ["arm"] = variant.Arm,
                        ["variant"] = variant.Name,
                        ["ran"] = ran[v],
                        ["differ"] = differ[v],
                        ["notCalled"] = skipped[v],
                    }).ToList(),
                });
            }
        }

        var manifest = Path.Combine(population.Directory, "manifest.json");
        var document = new Dictionary<string, object?>
        {
            ["rule"] = "docs/evidence/sjl-4-001/reference-arms.md",
            ["revision"] = "docs/evidence/sjl-4-001/reference-arms-revised-2026-10-13.md",
            ["void"] = limit is not null || filtered,
            ["lineLimit"] = limit,
            ["seconds"] = Math.Round(started.Elapsed.TotalSeconds),
            ["population"] = File.Exists(manifest) ? JsonDocument.Parse(File.ReadAllText(manifest)).RootElement : null,
            ["notImplemented"] = NumericsArms.NotImplemented.Select(static n => new Dictionary<string, string> { ["arm"] = n.Arm, ["operation"] = n.Operation, ["reason"] = n.Reason }).ToList(),
            ["operations"] = summary,
        };
        File.WriteAllText(Path.Combine(outputDirectory, "summary.json"),
            JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private static string Row(NumericsOperation operation, NumericsInput input, NumericsVariant variant, string shared, string result) =>
        string.Join('\t',
            operation.Name, input.Class, input.Line.ToString(CultureInfo.InvariantCulture),
            Render.Listed(input.Detail + (input.Text is { } text && input.Class != "R1M" ? "|" + text : string.Empty)),
            Render.Listed(input.Raw), variant.Arm + ":" + variant.Name, Render.ListedResult(shared), Render.ListedResult(result));

    /// <summary>Runs one implementation, rendering an engine's error by its kind and any other exception by its type.</summary>
    public static string Safe(Func<NumericsInput, string> run, NumericsInput input)
    {
        try
        {
            return run(input);
        }
        catch (J.JsErrorException error)
        {
            return "error:" + error.Kind;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception:" + exception.GetType().Name;
        }
    }
}
