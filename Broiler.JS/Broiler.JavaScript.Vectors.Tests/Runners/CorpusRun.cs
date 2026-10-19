using System.Diagnostics;
using System.Globalization;
using Broiler.ECMAScript.Conformance;

namespace Broiler.JavaScript.Vectors.Tests.Runners;

/// <summary>
/// The corpus test's run of a list of vectors through Broiler.JS in one evaluating process or several,
/// with the results in the list's order whatever their number.
/// </summary>
/// <remarks>
/// <para>
/// <b>One process</b>, the default, is the run as it has always been: every vector in order through the
/// one engine given, on the calling thread.
/// </para>
/// <para>
/// <b>N processes.</b> The list is cut into blocks of <see cref="DefaultBlockSize"/> vectors, and worker
/// w takes blocks w, w + N, w + 2N and so on, in order, on a thread of its own with a
/// <see cref="BroilerJsEngine"/>, and so a process, of its own. Which vectors each process evaluates, and
/// in what order, is fixed by N, so a run can be repeated exactly. Each block's results fill that
/// block's place, so the merged list, and the report written from it, is in the list's order. Each
/// vector still gets a fresh realm of its own, whichever process evaluates it.
/// </para>
/// <para>
/// <b>The timeout re-check.</b> N processes share the machine, and a vector that completes in
/// milliseconds alone can find no completion within <see cref="BroilerJsEngine.Limit"/> on a loaded one
/// (bundle sjl-10-001 retains such a run). After the parallel pass, every vector that rendered
/// <see cref="BroilerJsEngine.TimeoutOutcome"/> is evaluated again, alone, in a new process, and that
/// result is kept. A vector that never completes still aborts. One process has no parallel pass, and
/// nothing is evaluated twice.
/// </para>
/// </remarks>
public sealed class CorpusRun
{
    /// <summary>How many vectors a worker takes at a time.</summary>
    public const int DefaultBlockSize = 512;

    private CorpusRun(IReadOnlyList<VectorResult> results, int workers, TimeSpan elapsed, TimeSpan slowest, string slowestId, IReadOnlyList<string> rechecked)
    {
        Results = results;
        Workers = workers;
        Elapsed = elapsed;
        Slowest = slowest;
        SlowestId = slowestId;
        Rechecked = rechecked;
    }

    /// <summary>One result per vector, in the list's order.</summary>
    public IReadOnlyList<VectorResult> Results { get; }

    /// <summary>How many evaluating processes the parallel pass used.</summary>
    public int Workers { get; }

    /// <summary>The run's wall-clock time, the re-check included.</summary>
    public TimeSpan Elapsed { get; }

    /// <summary>The longest evaluation among the results kept: from writing the script to the answer.</summary>
    public TimeSpan Slowest { get; }

    /// <summary>The vector that took <see cref="Slowest"/>.</summary>
    public string SlowestId { get; }

    /// <summary>The vectors evaluated again alone after finding no completion in the parallel pass.</summary>
    public IReadOnlyList<string> Rechecked { get; }

    /// <summary>The number of processes a value of the workers variable asks for: a whole number above 1, or 1.</summary>
    /// <param name="value">The variable's value, or <see langword="null"/> when it is unset.</param>
    /// <returns>The number of processes.</returns>
    public static int WorkersFrom(string? value)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var workers) && workers > 1 ? workers : 1;

    /// <summary>Runs <paramref name="vectors"/> in <paramref name="workers"/> processes.</summary>
    /// <param name="vectors">The vectors, already validated.</param>
    /// <param name="prelude">The corpus's prelude.js.</param>
    /// <param name="workers">The number of processes; 1 runs every vector through <paramref name="serial"/>.</param>
    /// <param name="serial">The engine a one-process run uses. Several processes use engines of their own.</param>
    /// <param name="blockSize">How many vectors a worker takes at a time.</param>
    /// <returns>The run.</returns>
    public static CorpusRun Run(IReadOnlyList<VectorRecord> vectors, string prelude, int workers, BroilerJsEngine serial, int blockSize = DefaultBlockSize)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(serial);
        ArgumentOutOfRangeException.ThrowIfLessThan(workers, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(blockSize, 1);

        var clock = Stopwatch.StartNew();
        var blocks = vectors.Chunk(blockSize).ToArray();
        var parts = new VectorResult[blocks.Length][];
        var times = new TimeSpan[vectors.Count];

        if (workers == 1)
        {
            RunBlocks(0, 1, blocks, blockSize, prelude, serial, parts, times);
        }
        else
        {
            // On Windows a redirected pipe is read synchronously on a thread-pool thread, so each worker's
            // pending answer holds one while its own thread waits; the pool starts with one per core.
            ThreadPool.GetMinThreads(out var poolThreads, out var completionThreads);
            if (poolThreads < 2 * workers)
                ThreadPool.SetMinThreads(2 * workers, completionThreads);

            var engines = new BroilerJsEngine[workers];
            try
            {
                for (var w = 0; w < workers; w++)
                    engines[w] = new BroilerJsEngine();

                var failures = new Exception?[workers];
                var threads = new Thread[workers];
                for (var w = 0; w < workers; w++)
                {
                    var worker = w;
                    threads[w] = new Thread(() =>
                    {
                        try
                        {
                            RunBlocks(worker, workers, blocks, blockSize, prelude, engines[worker], parts, times);
                        }
                        catch (Exception ex)
                        {
                            failures[worker] = ex;
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "vectors worker " + worker.ToString(CultureInfo.InvariantCulture),
                    };
                    threads[w].Start();
                }

                foreach (var thread in threads)
                    thread.Join();
                var failed = failures.OfType<Exception>().ToList();
                if (failed.Count > 0)
                    throw new AggregateException("a vectors worker failed", failed);
            }
            finally
            {
                foreach (var engine in engines)
                    engine?.Dispose();
            }
        }

        var results = parts.SelectMany(part => part).ToArray();
        var rechecked = new List<string>();
        if (workers > 1)
        {
            for (var i = 0; i < results.Length; i++)
            {
                if (results[i].Rendered != BroilerJsEngine.TimeoutOutcome)
                    continue;
                using var alone = new BroilerJsEngine();
                results[i] = VectorRun.RunEngine([results[i].Vector], prelude, alone)[0];
                times[i] = alone.LastEvaluation;
                rechecked.Add(results[i].Vector.Id);
            }
        }

        var slowest = 0;
        for (var i = 1; i < times.Length; i++)
        {
            if (times[i] > times[slowest])
                slowest = i;
        }

        return new CorpusRun(
            results,
            workers,
            clock.Elapsed,
            times.Length == 0 ? TimeSpan.Zero : times[slowest],
            times.Length == 0 ? string.Empty : vectors[slowest].Id,
            rechecked);
    }

    /// <summary>One line for the test's output: N, the elapsed time, the slowest vector and the re-check.</summary>
    /// <returns>The line.</returns>
    public string Summary()
    {
        var limit = BroilerJsEngine.Limit.TotalSeconds;
        var line = FormattableString.Invariant(
            $"{Results.Count} vectors in {Workers} evaluating process(es) (BROILER_VECTORS_WORKERS) in {(int)Elapsed.TotalMinutes} min {Elapsed.TotalSeconds % 60:0.0} s; the slowest, {SlowestId}, took {Slowest.TotalSeconds:0.000} s of the {limit:0} s limit");
        if (Workers == 1)
            return line + ".";
        return line + FormattableString.Invariant($"; {Rechecked.Count} found no completion in the parallel pass and were run again alone")
            + (Rechecked.Count == 0 ? "." : ": " + string.Join(", ", Rechecked) + ".");
    }

    // Worker `worker` of `workers`: blocks worker, worker + workers, ..., each vector as the serial
    // runner evaluates it, into the block's own place in `parts` and the vector's own in `times`.
    private static void RunBlocks(int worker, int workers, VectorRecord[][] blocks, int blockSize, string prelude, BroilerJsEngine engine, VectorResult[][] parts, TimeSpan[] times)
    {
        for (var b = worker; b < blocks.Length; b += workers)
        {
            var block = blocks[b];
            var part = new VectorResult[block.Length];
            for (var k = 0; k < block.Length; k++)
            {
                part[k] = VectorRun.RunEngine([block[k]], prelude, engine)[0];
                times[(b * blockSize) + k] = engine.LastEvaluation;
            }

            parts[b] = part;
        }
    }
}
