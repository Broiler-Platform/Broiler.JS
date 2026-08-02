using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits the distribution of property-map sizes over a real workload, and what each candidate
/// growth policy would cost against that distribution.
/// </summary>
/// <remarks>
/// <para>
/// This is the measurement roadmap item 2-7 is blocked on, and it is blocked on it for a
/// specific reason. `--object-alloc` showed that a property map's memory is a fixed block rather
/// than per-field storage: <see cref="VirtualMemory{T}"/> rounds its first request up to 16
/// nodes, so **one field costs the same as three and ~1 040 B more than none**. A smaller floor
/// halves a one-field object — and makes an eight-field object worse, because it pays repeated
/// resize-and-copy. Which side wins is a question about the distribution of *real* objects, and
/// every synthetic probe answers it by construction.
/// </para>
/// <para>
/// So: run Octane's own suites, record where each map's life ended, and simulate the policies
/// against that. The simulation mirrors <see cref="VirtualMemory{T}.Allocate"/> step for step
/// rather than modelling it, and the node size comes from
/// <see cref="SAUint32Map{T}.NodeSizeBytes"/> rather than from a hand-added field list, so the
/// arithmetic cannot drift from the code it is about.
/// </para>
/// <para>
/// Needs an Octane 2.0 checkout — the corpus is not vendored. Suites run in a fresh context each,
/// with the histogram reset between them, so a per-suite disagreement is visible instead of
/// averaged away. A suite that throws still reports the maps it built before it did; that is
/// data, not a gap, and the failure is named in the output.
/// </para>
/// </remarks>
internal static class PropertyMapDistributionMetrics
{
    /// <summary>
    /// Runs per benchmark, overridable with the <c>BROILER_MAP_DISTRIBUTION_RUNS</c> environment
    /// variable. Octane's scoring loop runs for a wall-clock second per benchmark and repeats;
    /// the *distribution* of map sizes converges long before that, because it is set by the
    /// shapes a workload builds rather than by how many times it builds them. Raising this is
    /// still worth doing to check that claim rather than assume it — if the shares move with the
    /// run count, the sample was too small and the answer was not converged.
    /// </summary>
    private static int RunsPerBenchmark =>
        int.TryParse(Environment.GetEnvironmentVariable("BROILER_MAP_DISTRIBUTION_RUNS"), out var runs) && runs > 0
            ? runs
            : 10;

    /// <summary>
    /// Suites to skip, comma-separated, via <c>BROILER_MAP_DISTRIBUTION_SKIP</c>. Everything runs
    /// in one process with no per-suite timeout, so Mandreel — which needs upwards of five
    /// minutes just to compile — is worth being able to set aside while iterating, and worth
    /// putting back for the run of record.
    /// </summary>
    private static string[] Skipped =>
        (Environment.GetEnvironmentVariable("BROILER_MAP_DISTRIBUTION_SKIP") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>The map this item is about; the rest share the implementation.</summary>
    private const string PropertyMapType = "JSObjectProperty";

    /// <summary>x64: object header plus the array length field, which every array pays.</summary>
    private const int ArrayHeaderBytes = 24;

    private sealed record Suite(string Name, string[] Files);

    // scripts/octane-suites.json's load order, duplicated here rather than read from it: this
    // emitter has to run from the benchmarks project with no repository layout assumed.
    private static readonly Suite[] Suites =
    [
        new("Richards", ["richards.js"]),
        new("DeltaBlue", ["deltablue.js"]),
        new("Crypto", ["crypto.js"]),
        new("RayTrace", ["raytrace.js"]),
        new("EarleyBoyer", ["earley-boyer.js"]),
        new("RegExp", ["regexp.js"]),
        new("Splay", ["splay.js"]),
        new("NavierStokes", ["navier-stokes.js"]),
        new("PdfJS", ["pdfjs.js"]),
        new("Mandreel", ["mandreel.js"]),
        new("Gameboy", ["gbemu-part1.js", "gbemu-part2.js"]),
        new("CodeLoad", ["code-load.js"]),
        new("Box2D", ["box2d.js"]),
        new("zlib", ["zlib.js", "zlib-data.js"]),
        new("Typescript", ["typescript.js", "typescript-input.js", "typescript-compiler.js"]),
    ];

    /// <summary>
    /// Drives every registered benchmark's Setup / run / TearDown directly instead of going
    /// through BenchmarkSuite.RunSuites, which would spend a second per benchmark scoring. Each
    /// failure is collected rather than thrown, so one broken suite does not lose the run.
    /// </summary>
    private const string Driver = """
        (function () {
            var suites = (typeof BenchmarkSuite !== 'undefined' && BenchmarkSuite.suites) || [];
            var ran = 0, failures = [];
            for (var i = 0; i < suites.length; i++) {
                var suite = suites[i];
                for (var j = 0; j < suite.benchmarks.length; j++) {
                    var benchmark = suite.benchmarks[j];
                    try {
                        benchmark.Setup();
                        for (var k = 0; k < __runs; k++) benchmark.run();
                        benchmark.TearDown();
                        ran++;
                    } catch (e) {
                        failures.push(suite.name + '/' + benchmark.name + ': ' + e);
                    }
                }
            }
            return ran + '|' + failures.join(' ;; ');
        })()
        """;

    /// <summary>How a candidate policy picks the new capacity when the backing array must grow.</summary>
    /// <remarks>
    /// Signature matches the decision <see cref="VirtualMemory{T}.Allocate"/> makes: given the
    /// slots already handed out and the slots now needed, return the capacity to allocate.
    /// </remarks>
    private sealed record Policy(string Name, string Note, Func<int, int, int> NextCapacity);

    private static readonly Policy[] Policies =
    [
        new("round-up-16", "current: VirtualMemory.Allocate as written", static (last, needed) => RoundUp(last, needed, 16)),
        new("round-up-8", "halve the floor, keep the shape of the formula", static (last, needed) => RoundUp(last, needed, 8)),
        new("round-up-4", "floor = one node group, the smallest the trie can use", static (last, needed) => RoundUp(last, needed, 4)),
        new("min-4-then-double", "no over-allocation at the bottom, geometric after", static (last, needed) => Math.Max(Math.Max(last * 2, needed), 4)),
    ];

    /// <summary>`((max / round) + 1) * round`, guarded by the doubling branch, exactly as written.</summary>
    private static int RoundUp(int last, int needed, int round)
    {
        var capacity = last * 2;
        return capacity <= needed ? ((needed / round) + 1) * round : capacity;
    }

    internal static void Write(string octaneDirectory)
    {
        if (!Directory.Exists(octaneDirectory))
        {
            Console.Error.WriteLine($"octane directory not found: {octaneDirectory}");
            Console.Error.WriteLine("Clone https://github.com/chromium/octane and pass its path.");
            Environment.ExitCode = 2;
            return;
        }

        var basePath = Path.Combine(octaneDirectory, "base.js");
        if (!File.Exists(basePath))
        {
            Console.Error.WriteLine($"not an Octane checkout (no base.js): {octaneDirectory}");
            Environment.ExitCode = 2;
            return;
        }

        var baseSource = File.ReadAllText(basePath);
        var suiteRows = new List<object>();
        var aggregate = new long[PropertyStorageMetrics.MaxTrackedGroups + 2];
        var skipped = Skipped;

        foreach (var suite in Suites)
        {
            if (Array.Exists(skipped, name => string.Equals(name, suite.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine($"{suite.Name}: skipped by BROILER_MAP_DISTRIBUTION_SKIP");
                continue;
            }

            Console.Error.WriteLine($"{suite.Name}: running ...");
            var row = RunSuite(octaneDirectory, baseSource, suite, aggregate);
            if (row != null)
                suiteRows.Add(row);
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.property-map-distribution/1",
                runsPerBenchmark = RunsPerBenchmark,
                skipped,
                nodeSizeBytes = SAUint32Map<JSObjectProperty>.NodeSizeBytes,
                arrayHeaderBytes = ArrayHeaderBytes,
                note = "Final node-group count per SAUint32Map<JSObjectProperty>, over Octane 2.0. "
                    + "histogram[k] = maps whose life ended at k four-node groups; a map that never "
                    + "allocated is an object with no named properties and is not counted. "
                    + "See docs/performance-roadmap.md item 2-7.",
                suites = suiteRows,
                aggregate = Describe(aggregate),
                policies = SimulatePolicies(aggregate),
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object RunSuite(string octaneDirectory, string baseSource, Suite suite, long[] aggregate)
    {
        var sources = new List<string> { baseSource };
        foreach (var file in suite.Files)
        {
            var path = Path.Combine(octaneDirectory, file);
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"{suite.Name}: missing {file}, skipped");
                return null;
            }

            sources.Add(File.ReadAllText(path));
        }

        using var context = BenchmarkContext.Create();

        string outcome;
        long[] histogram;
        PropertyStorageSnapshot snapshot;

        // Load OUTSIDE the recording window: a suite's top-level code builds the engine's own
        // maps for its globals and prototypes, and those are one-time rather than per-object.
        // The window opens once the workload starts allocating.
        try
        {
            for (var i = 0; i < sources.Count; i++)
                context.Eval(sources[i], suite.Files.Length > 0 && i > 0 ? suite.Files[i - 1] : "base.js");

            context.Eval($"var __runs = {RunsPerBenchmark};", "runs.js");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{suite.Name}: failed to load — {e.GetType().Name}: {e.Message}");
            return new { suite = suite.Name, loaded = false, error = $"{e.GetType().Name}: {e.Message}" };
        }

        using (PropertyStorageMetrics.Enable())
        {
            PropertyStorageMetrics.Reset();
            try
            {
                outcome = context.Eval(Driver, "driver.js").ToString();
            }
            catch (Exception e)
            {
                outcome = $"0|driver threw {e.GetType().Name}: {e.Message}";
            }

            snapshot = PropertyStorageMetrics.Snapshot();
        }

        histogram = snapshot.FinalGroupCountsByValueType.TryGetValue(PropertyMapType, out var found)
            ? found
            : new long[PropertyStorageMetrics.MaxTrackedGroups + 2];

        for (var i = 0; i < histogram.Length && i < aggregate.Length; i++)
            aggregate[i] += histogram[i];

        var split = outcome.Split('|', 2);

        return new
        {
            suite = suite.Name,
            loaded = true,
            benchmarksRun = int.TryParse(split[0], out var ran) ? ran : 0,
            failures = split.Length > 1 && split[1].Length > 0 ? split[1] : null,
            groupAllocations = snapshot.GroupAllocations,
            backingArrayResizes = snapshot.BackingArrayResizes,
            nodesCopiedByResizes = snapshot.NodesCopiedByResizes,
            propertyMaps = Describe(histogram),
            otherMapTypes = snapshot.FinalGroupCountsByValueType
                .Where(pair => pair.Key != PropertyMapType && pair.Value.Sum() > 0)
                .ToDictionary(pair => pair.Key, pair => pair.Value.Sum()),
        };
    }

    private static object Describe(long[] histogram)
    {
        var total = histogram.Sum();
        var buckets = new Dictionary<string, long>();
        for (var groups = 1; groups < histogram.Length; groups++)
        {
            if (histogram[groups] == 0)
                continue;

            var label = groups > PropertyStorageMetrics.MaxTrackedGroups
                ? $">{PropertyStorageMetrics.MaxTrackedGroups}"
                : groups.ToString();
            buckets[label] = histogram[groups];
        }

        // The share at one group is the whole question: it is the population a smaller floor
        // helps, and the population a larger floor is charging for growth it never uses.
        var oneGroup = histogram.Length > 1 ? histogram[1] : 0;
        var upToFour = 0L;
        for (var groups = 1; groups <= 4 && groups < histogram.Length; groups++)
            upToFour += histogram[groups];

        return new
        {
            maps = total,
            byGroupCount = buckets,
            shareAtOneGroup = total == 0 ? 0 : Math.Round((double)oneGroup / total, 4),
            shareWithinTheCurrentFloor = total == 0 ? 0 : Math.Round((double)upToFour / total, 4),
        };
    }

    private sealed record PolicyCost(
        string Policy,
        string Note,
        long LiveBytes,
        long AllocatedBytes,
        long BackingArrayAllocations,
        long NodesCopied);

    private static object[] SimulatePolicies(long[] histogram)
    {
        var nodeSize = SAUint32Map<JSObjectProperty>.NodeSizeBytes;
        var costs = Policies.Select(policy => Cost(policy, histogram, nodeSize)).ToArray();

        // Deltas against the policy in the code, which is what the decision turns on.
        var current = costs[0];

        return costs.Select(cost => (object)new
        {
            policy = cost.Policy,
            note = cost.Note,
            liveBytes = cost.LiveBytes,
            allocatedBytes = cost.AllocatedBytes,
            backingArrayAllocations = cost.BackingArrayAllocations,
            nodesCopied = cost.NodesCopied,
            liveVsCurrent = Ratio(cost.LiveBytes, current.LiveBytes),
            allocatedVsCurrent = Ratio(cost.AllocatedBytes, current.AllocatedBytes),
        }).ToArray();
    }

    private static double Ratio(long value, long baseline)
        => baseline == 0 ? 0 : Math.Round((double)value / baseline, 4);

    private static PolicyCost Cost(Policy policy, long[] histogram, int nodeSize)
    {
        long liveBytes = 0, allocatedBytes = 0, copiedNodes = 0, allocations = 0;

        for (var groups = 1; groups < histogram.Length; groups++)
        {
            var maps = histogram[groups];
            if (maps == 0)
                continue;

            var (finalCapacity, totalSlots, copied, grows) = Simulate(groups, policy);
            liveBytes += maps * (ArrayHeaderBytes + (long)finalCapacity * nodeSize);
            allocatedBytes += maps * ((long)totalSlots * nodeSize + grows * ArrayHeaderBytes);
            copiedNodes += maps * copied;
            allocations += maps * grows;
        }

        return new PolicyCost(policy.Name, policy.Note, liveBytes, allocatedBytes, allocations, copiedNodes);
    }

    /// <summary>
    /// Replays <paramref name="groups"/> successive four-node allocations under one policy,
    /// mirroring <see cref="VirtualMemory{T}.Allocate"/>: it grows when the backing array cannot
    /// hold what is now needed, and a grow copies everything handed out so far.
    /// </summary>
    private static (int FinalCapacity, int TotalSlots, long CopiedNodes, long Grows) Simulate(int groups, Policy policy)
    {
        int last = 0, capacity = 0, totalSlots = 0;
        long copied = 0, grows = 0;

        for (var group = 0; group < groups; group++)
        {
            var needed = last + SAUint32Map<JSObjectProperty>.NodeBlock;
            if (capacity == 0 || capacity <= needed)
            {
                var next = policy.NextCapacity(last, needed);
                totalSlots += next;
                grows++;
                if (capacity > 0)
                    copied += last;
                capacity = next;
            }

            last += SAUint32Map<JSObjectProperty>.NodeBlock;
        }

        return (capacity, totalSlots, copied, grows);
    }
}
