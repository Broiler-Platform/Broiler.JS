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
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date;

/// <summary>
/// One arm's implementation of one operation, with the shared implementation it is compared with: S called
/// as that arm's engine will call it. <see cref="Applies"/> says which inputs the arm's engine can hand the
/// member at all.
/// </summary>
internal sealed record DateVariant(
    string Arm, string Name, Func<DateInput, string> Run, Func<DateInput, string> Shared,
    Func<DateInput, bool>? Applies = null);

/// <summary>An operation of the rule: the classes it runs over, how a line becomes its inputs, and its variants.</summary>
internal sealed record DateOperation(
    string Name, string[] Classes, Func<DateInput, IEnumerable<DateInput>> Expand, DateVariant[] Variants,
    Func<DateInput, string>? SharedOnly = null, bool StringOperation = false);

/// <summary>
/// The comparison of bundle sjl-6-001's reference-arm rule: every operation over its classes, every variant
/// of every arm against S called as that arm's engine will call it, exactly. It classifies nothing; the
/// classification is scripts/conformance/classify_date_arms.py, run on its output.
/// </summary>
/// <remarks>
/// <para>
/// <b>Output.</b> <c>summary.json</c> holds, per operation and class, the lines read, the inputs they gave,
/// how many inputs S answered alone, and per variant how many it ran, how many differed from S and how many
/// it was not called for; the operations an arm does not implement and why; and the population's manifest.
/// <c>differences.tsv.gz</c> holds one line per input and variant that differ: the operation, the class, the
/// line, the parameters (<see cref="DateInput.Detail"/>), the line as the population writes it, the variant,
/// S's result and the variant's result.
/// </para>
/// <para>
/// <b>A run with a line limit, or an operation filter, is void</b> (the rule's "a run that stopped before the
/// end of any operation's population"); its summary says so. A culture run, which runs arm J's string
/// operations alone under one of the rule's three cultures, says which culture it ran under, and is judged
/// with the run under the invariant culture.
/// </para>
/// </remarks>
internal sealed class DateComparison(DatePopulation population, IReadOnlyList<DateOperation> operations, long? limit, bool filtered, string? cultureRun, TextWriter log)
{
    private const int ChunkLines = 1024;

    public void Run(string outputDirectory, string rule, string revision)
    {
        Directory.CreateDirectory(outputDirectory);
        DateArms.Load(population);

        // Arm J reads the offset and the name in force at the moment of the call for an instant outside .NET's
        // range (correspondence row L4), so the run's moment is part of its result.
        var startedUtc = DateTimeOffset.UtcNow;

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
            var width = (3 * operation.Variants.Length) + 2;
            foreach (var name in operation.Classes)
            {
                var clock = Stopwatch.StartNew();
                long lines = 0, inputs = 0, alone = 0;
                var ran = new long[operation.Variants.Length];
                var differ = new long[operation.Variants.Length];
                var skipped = new long[operation.Variants.Length];
                foreach (var chunk in population.Chunks(name, ChunkLines))
                {
                    var take = limit is { } most ? (int)Math.Min(chunk.Count, Math.Max(0, most - lines)) : chunk.Count;
                    if (take == 0)
                        break;

                    var rows = new List<string>?[take];
                    var counts = new long[take, width];
                    Parallel.For(0, take, at =>
                    {
                        foreach (var input in operation.Expand(chunk[at]))
                        {
                            counts[at, width - 2]++;
                            if (operation.SharedOnly is { } only)
                            {
                                _ = Safe(only, input);
                                counts[at, width - 1]++;
                            }

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
                        inputs += counts[at, width - 2];
                        alone += counts[at, width - 1];
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

                log.WriteLine($"{operation.Name} {name}: {lines} lines, {inputs} inputs" + (operation.SharedOnly is null ? string.Empty : $", S alone {alone}") + ", " +
                              string.Join(", ", operation.Variants.Select((variant, v) => $"{variant.Arm}:{variant.Name} {differ[v]} of {ran[v]} differ ({skipped[v]} not called)")) +
                              $" ({clock.Elapsed.TotalSeconds:F1} s)");
                log.Flush();
                summary.Add(new Dictionary<string, object?>
                {
                    ["operation"] = operation.Name,
                    ["class"] = name,
                    ["lines"] = lines,
                    ["inputs"] = inputs,
                    ["sharedAlone"] = operation.SharedOnly is null ? null : alone,
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
            ["rule"] = rule,
            ["revision"] = revision,
            ["culture"] = CultureInfo.CurrentCulture.Name.Length == 0 ? "invariant" : CultureInfo.CurrentCulture.Name,
            ["cultureRun"] = cultureRun,
            ["processZone"] = TimeZoneInfo.Local.Id,
            ["startedUtc"] = startedUtc.ToString("O", CultureInfo.InvariantCulture),
            ["void"] = limit is not null || filtered,
            ["lineLimit"] = limit,
            ["seconds"] = Math.Round(started.Elapsed.TotalSeconds),
            ["population"] = File.Exists(manifest) ? JsonDocument.Parse(File.ReadAllText(manifest)).RootElement : null,
            ["notImplemented"] = DateArms.NotImplemented.Select(static n => new Dictionary<string, string> { ["arm"] = n.Arm, ["operation"] = n.Operation, ["reason"] = n.Reason }).ToList(),
            ["operations"] = summary,
        };
        File.WriteAllText(Path.Combine(outputDirectory, "summary.json"),
            JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private static string Row(DateOperation operation, DateInput input, DateVariant variant, string shared, string result) =>
        string.Join('\t',
            operation.Name, input.Class, input.Line.ToString(CultureInfo.InvariantCulture),
            Render.Listed(input.Detail), Render.Listed(input.Raw), variant.Arm + ":" + variant.Name,
            Render.ListedResult(shared), Render.ListedResult(result));

    /// <summary>Runs one implementation, rendering an engine's error by its kind and any other exception by its type.</summary>
    public static string Safe(Func<DateInput, string> run, DateInput input)
    {
        try
        {
            return run(input);
        }
        catch (ArmError error)
        {
            return DateRender.Error(error.Kind);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception:" + exception.GetType().Name;
        }
    }
}
