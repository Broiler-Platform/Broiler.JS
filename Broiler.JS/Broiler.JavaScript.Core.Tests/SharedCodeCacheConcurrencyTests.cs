using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Core.Tests;

/// <summary>
/// Executing the SAME compiled code on more than one thread, which is what the process-shared code
/// cache makes possible.
/// </summary>
/// <remarks>
/// <para>
/// <b>A context normally compiles its own copy and so owns its own inline caches.</b>
/// <c>JSContext</c> builds a private <see cref="DictionaryCodeCache"/> unless the host asks for
/// <c>UseProcessSharedCodeCache</c> or assigns <see cref="DictionaryCodeCache.Current"/> over it —
/// and a host does. The Broiler.Browser DOM bridge installs the shared cache around document
/// registration for a measured 30.7x, and its remarks reason carefully about which <em>sources</em>
/// may enter the cache. What they do not reason about is two threads <em>executing</em> what is in
/// it, which is what these tests are for.
/// </para>
/// <para>
/// <b>The first test is the premise and is deterministic; the second is a net and is not.</b> A race
/// reproduces or it does not, and this one did not — see
/// <see cref="ASharedCacheGivesTwoContextsTheSameInlineCacheSites"/> for what can be asserted
/// outright, and the remarks on the stress test for what its passing is and is not worth.
/// </para>
/// </remarks>
/// <summary>
/// Runs alone, because one of these tests measures a process-wide counter.
/// </summary>
/// <remarks>
/// <c>PropertyInlineCacheSite.NextReadSite</c> is shared by every context in the process, and its
/// own remarks call it "a bound, not an inventory" for exactly that reason - another thread
/// compiling at the same time interleaves its own sites into the range. Measuring how many sites an
/// evaluation allocated therefore only means something while nothing else is compiling, which is
/// what this collection buys. It costs almost nothing: the project has four test classes.
/// </remarks>
[CollectionDefinition(nameof(SharedCodeCacheCollection), DisableParallelization = true)]
public sealed class SharedCodeCacheCollection;

[Collection(nameof(SharedCodeCacheCollection))]
public class SharedCodeCacheConcurrencyTests
{
    /// <summary>
    /// One read site per round, seeing four shapes — the whole of the program past the gate.
    /// </summary>
    /// <remarks>
    /// <b>A site's window is four reads wide and opens once in the life of the process.</b> It fills
    /// from zero entries to four and is then either full or retired, and neither state fills again.
    /// So anything that RUNS the reads before the threads start — a warm-up, an expectation computed
    /// up front — closes the window before the race can enter it, and a source with many sites does
    /// not widen it but gives many narrow ones that threads drift apart across. Priming with the gate
    /// shut compiles the source into the shared cache, which is what allocates the site and what lets
    /// the other threads share it, and fills nothing.
    /// </remarks>
    private static string BuildSource(int round)
    {
        var shapes = "[{p" + round + ": 1}, {a: 0, p" + round + ": 2}, "
                   + "{a: 0, b: 0, p" + round + ": 3}, {a: 0, b: 0, c: 0, p" + round + ": 4}]";

        return "(function () {\n"
             + "  if (!globalThis.go) { return -1; }\n"
             + "  var s = " + shapes + ";\n"
             + "  var t = 0;\n"
             + "  for (var k = 0; k < 4; k++) { t += s[k].p" + round + "; }\n"
             + "  return t;\n"
             + "})()";
    }

    /// <summary>
    /// Two contexts sharing a code cache share the compiled code, and therefore the inline-cache
    /// sites baked into it.
    /// </summary>
    /// <remarks>
    /// <b>This is the premise the rest of this file rests on, and it is worth asserting on its own
    /// because it is invisible from the outside.</b> Nothing about a <see cref="JSContext"/> suggests
    /// that assigning a shared cache makes two of them mutate one process-wide side table; the
    /// emission-site counter is the only place it shows. A context with a cache of its own compiles
    /// its own copy and allocates its own sites, which is the contrast that makes the measurement
    /// mean something.
    /// </remarks>
    [Fact(Timeout = 600000)]
    public void ASharedCacheGivesTwoContextsTheSameInlineCacheSites()
    {
        // A marker no stress round below uses, so this test's site is its own.
        var source = BuildSource(round: 9001);
        const string Label = "shared:premise";

        int allocatedByFirst;
        int allocatedBySecond;
        int allocatedByPrivate;

        var start = PropertyInlineCacheSite.NextReadSite;

        using (var first = new JSContext())
        {
            first.CodeCache = DictionaryCodeCache.Current;
            first.Eval("globalThis.go = false;");
            first.Eval(source, Label);
            allocatedByFirst = PropertyInlineCacheSite.NextReadSite - start;
        }

        using (var second = new JSContext())
        {
            second.CodeCache = DictionaryCodeCache.Current;
            second.Eval("globalThis.go = false;");
            var mark = PropertyInlineCacheSite.NextReadSite;
            second.Eval(source, Label);
            allocatedBySecond = PropertyInlineCacheSite.NextReadSite - mark;
        }

        using (var isolated = new JSContext())
        {
            isolated.Eval("globalThis.go = false;");
            var mark = PropertyInlineCacheSite.NextReadSite;
            isolated.Eval(source, Label);
            allocatedByPrivate = PropertyInlineCacheSite.NextReadSite - mark;
        }

        Assert.True(allocatedByFirst > 0, "the first evaluation allocated no read sites at all");

        // The whole point: the second context compiled nothing and reuses the first's sites.
        Assert.Equal(0, allocatedBySecond);

        // And a context that does not share the cache does not share the sites.
        Assert.Equal(allocatedByFirst, allocatedByPrivate);
    }

    /// <summary>
    /// The same cached code, entered by many threads at once on a site none of them has filled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test never reproduced the defect it guards, and that belongs here rather than in a
    /// commit message nobody will find.</b> The defect is real by construction — the fill path was
    /// <c>entries[count++]</c> on state the test above proves is shared, and <c>Entry</c> is a
    /// six-field struct whose assignment is not atomic — and it was seen once, as an
    /// <c>IndexOutOfRangeException</c> out of the read loop on a CI leg. About 78,000 racing entries
    /// across four shapes of this test produced it zero times.
    /// </para>
    /// <para>
    /// So a green run here is evidence and not proof, and the fix is justified by the source and the
    /// stack rather than by this. What earns it its place is the shape of a regression: an entry
    /// array that goes back to being mutated where it lies, or a count incremented outside a
    /// publication, is the kind of change that turns this from unlikely into frequent — and that is
    /// what this would notice.
    /// </para>
    /// </remarks>
    [Fact(Timeout = 600000)]
    public void TheSameCachedCodeOnManyThreadsAgreesAndDoesNotThrow()
    {
        var threads = Math.Max(8, Environment.ProcessorCount * 2);
        const int Rounds = 150;
        const double Expected = 1 + 2 + 3 + 4;

        var failures = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var disagreements = new System.Collections.Concurrent.ConcurrentBag<string>();

        for (var round = 0; round < Rounds && failures.IsEmpty && disagreements.IsEmpty; round++)
        {
            var source = BuildSource(round);
            var label = "shared:round" + round;

            // Compile into the shared cache with the gate shut: allocates the site, fills nothing.
            using (var priming = new JSContext())
            {
                priming.CodeCache = DictionaryCodeCache.Current;
                priming.Eval("globalThis.go = false;");
                Assert.Equal(-1d, priming.Eval(source, label).DoubleValue);
            }

            using var gate = new Barrier(threads);
            var workers = new Thread[threads];

            for (var t = 0; t < threads; t++)
            {
                workers[t] = new Thread(() =>
                {
                    JSContext? context = null;
                    try
                    {
                        context = new JSContext();

                        // What the DOM bridge does, and the whole reason these contexts share
                        // anything at all.
                        context.CodeCache = DictionaryCodeCache.Current;
                        context.Eval("globalThis.go = true;");

                        // Everything expensive is behind us; past the gate is four property reads.
                        gate.SignalAndWait();

                        var answer = context.Eval(source, label).DoubleValue;
                        if (answer != Expected)
                            disagreements.Add($"round {round}: {answer} != {Expected}");
                    }
                    catch (Exception ex)
                    {
                        failures.Add(ex);
                    }
                    finally
                    {
                        context?.Dispose();
                    }
                })
                { IsBackground = true };
            }

            foreach (var worker in workers)
                worker.Start();

            foreach (var worker in workers)
                worker.Join();
        }

        Assert.True(failures.IsEmpty, $"a run threw: {failures.FirstOrDefault()}");

        // A wrong answer is the quieter half of this defect and worth naming separately: a
        // half-written entry can read a value out of another shape's slot without any index ever
        // leaving the array, and nothing downstream re-derives the key to catch it.
        Assert.True(disagreements.IsEmpty, string.Join("; ", disagreements.Take(5)));
    }
}
