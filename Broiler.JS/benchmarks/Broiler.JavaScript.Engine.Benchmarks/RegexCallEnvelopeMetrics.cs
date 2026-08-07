using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NetRegex = System.Text.RegularExpressions.Regex;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// What a JavaScript regex operation costs <em>around</em> the match — the same pattern and the
/// same subject, timed through <c>re.test</c>, <c>re.exec</c> and <c>String.prototype.search</c>,
/// against <c>Regex.IsMatch</c> called directly (docs/performance-roadmap.md phase 5).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this exists.</strong> Phase 5 spent its first four items on the matcher and its
/// fifth on whether to compile it, and the compile question came back "worth about 2× on the
/// matcher and nothing measurable on the suite". Those two answers are only compatible if the
/// matcher is a small share of what a JS regex call costs, and nothing in this engine had ever
/// measured that share. This does, with the crudest possible instrument: the identical work,
/// once through the engine and once through <c>System.Text.RegularExpressions</c> directly, at
/// the same iteration count on the same subject.
/// </para>
/// <para>
/// <strong>The decomposition is by builtin, not by internal frame</strong>, because those are the
/// boundaries a change could actually move. <c>test</c> and <c>exec</c> differ by the result
/// object §22.2.7.2 builds — an array, the matched string, <c>index</c>, <c>input</c> and
/// <c>groups</c> — which <c>test</c> discards on the next line; <c>search</c> reaches the same
/// matcher through a different builtin with no result array at all. Reading the three together
/// says how much of the envelope is the result object and how much is everything else, without
/// naming a single internal frame or needing a profiler that can see one.
/// </para>
/// <para>
/// The allocation column is deterministic and carries the claim; the time column is for
/// prioritization only (roadmap §3.1).
/// </para>
/// </remarks>
internal static class RegexCallEnvelopeMetrics
{
    private const int Iterations = 200_000;

    private const int Repetitions = 5;

    /// <summary>
    /// Three of the seven Octane patterns, chosen to span the matcher's own cost: a literal
    /// anchored at 0 (cheapest), a captured class (middle), and the trim alternation (dearest).
    /// If the envelope is a constant, its share falls as the matcher's cost rises, and these
    /// three are enough to see that.
    /// </summary>
    private static readonly (string Name, string Pattern, string Subject)[] Patterns =
    [
        ("caret-literal", "^ba", "bananas and bandanas, ba ba ba"),
        ("hyphen-lower", "(-[a-z])", "background-color and border-top-width and margin-left"),
        ("trim", @"^[\s\xa0]+|[\s\xa0]+$", "        padded on both sides, and then some       "),

        // The same pattern as the first row against a subject twenty times longer. The matcher's
        // own work is unchanged — `^ba` is anchored, so it decides at position 0 either way — so
        // any difference in the OTHER rows is the envelope scaling with the subject rather than
        // with the match. That is the discriminator for a copy: a fixed per-call cost cannot
        // move here, and a subject copy has to.
        ("caret-literal-long", "^ba", "bananas and bandanas, ba ba ba" + Repeat),
    ];

    /// <summary>Padding for the long-subject row. Ordinary text, no match anywhere in it.</summary>
    private const string Repeat =
        "the quick brown fox jumps over the lazy dog, and then does it again, and again, and again"
        + "the quick brown fox jumps over the lazy dog, and then does it again, and again, and again"
        + "the quick brown fox jumps over the lazy dog, and then does it again, and again, and again"
        + "the quick brown fox jumps over the lazy dog, and then does it again, and again, and again"
        + "the quick brown fox jumps over the lazy dog, and then does it again, and again, and again"
        + "the quick brown fox jumps over the lazy dog, and then does it again, and again, and again";

    private sealed record Row(
        string Site,
        string Shape,
        double Ms,
        double NsPerCall,
        long BytesPerCall);

    internal static void Write()
    {
        var rows = new List<Row>();
        foreach (var site in Patterns)
        {
            rows.Add(Direct(site, match: false));
            rows.Add(Direct(site, match: true));
            rows.Add(Engine(site, "test", "if (re.test(subject)) sink++;"));
            rows.Add(Engine(site, "exec", "if (re.exec(subject) !== null) sink++;"));
            rows.Add(Engine(site, "search", "if (subject.search(re) >= 0) sink++;"));
            rows.Add(Engine(site, "control-no-regex", "sink += subject.length;"));
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.regex-call-envelope/1",
                iterations = Iterations,
                repetitions = Repetitions,
                note = "The same pattern and subject, matched Iterations times, through six "
                    + "shapes: System.Text.RegularExpressions called directly as IsMatch (the "
                    + "control for `test`) and as Match (the control for `exec`), then "
                    + "RegExp.prototype.test / RegExp.prototype.exec / String.prototype.search "
                    + "through the engine, plus a loop with no matching in it. The direct rows "
                    + "are the matcher alone; the difference between them and the engine rows "
                    + "is everything this engine does around a match. The `-long` site is the "
                    + "same anchored pattern on a subject twenty times longer, so a row that "
                    + "moves there is scaling with the subject rather than with the match. "
                    + "Bytes are deterministic; time is for prioritization only (roadmap 3.1).",
                rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>The matcher alone, on the engine's own option set and with no engine around it.</summary>
    /// <param name="match">
    /// <c>false</c> runs <see cref="NetRegex.IsMatch(string)"/>, which allocates nothing and is
    /// the right control for <c>test</c>'s semantics; <c>true</c> runs
    /// <see cref="NetRegex.Match(string)"/>, which materialises a <c>Match</c> with its group and
    /// capture collections and is the right control for <c>exec</c>. Both are here because
    /// charging the engine for what <c>Match</c> itself allocates would be exactly the
    /// mis-attribution `0108` records.
    /// </param>
    private static Row Direct((string Name, string Pattern, string Subject) site, bool match)
    {
        // RegexOptions.ECMAScript is what JSRegExp.ParseFlags starts from, so this arm is the
        // matcher the engine actually holds rather than a default-constructed one.
        var regex = new NetRegex(site.Pattern, RegexOptions.ECMAScript);
        for (var i = 0; i < 1_000; i++)
        {
            if (match)
                regex.Match(site.Subject);
            else
                regex.IsMatch(site.Subject);
        }

        var samples = new List<double>(Repetitions);
        long bytes = 0;
        for (var repetition = 0; repetition < Repetitions; repetition++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            var sink = 0;
            for (var i = 0; i < Iterations; i++)
            {
                if (match ? regex.Match(site.Subject).Success : regex.IsMatch(site.Subject))
                    sink++;
            }

            watch.Stop();
            bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(sink);
            samples.Add(watch.Elapsed.TotalMilliseconds);
        }

        return Build(site.Name, match ? "direct-netregex-match" : "direct-netregex-ismatch", samples, bytes);
    }

    private static Row Engine((string Name, string Pattern, string Subject) site, string shape, string body)
    {
        using var context = BenchmarkContext.Create();

        var source = $$"""
            (function (n) {
                var re = /{{site.Pattern}}/;
                var subject = {{JsonSerializer.Serialize(site.Subject)}};
                var sink = 0;
                for (var i = 0; i < n; i++) { {{body}} }
                return sink;
            })
            """;

        var function = context.Eval(source, $"{site.Name}-{shape}.js");

        // Warm the compile, the pattern build and the inline caches; the measured invocations
        // below then time the loop and nothing one-time.
        function.InvokeFunction(new Arguments(JSUndefined.Value, new JSNumber(1_000)));

        var samples = new List<double>(Repetitions);
        long bytes = 0;
        var arguments = new Arguments(JSUndefined.Value, new JSNumber(Iterations));
        for (var repetition = 0; repetition < Repetitions; repetition++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            function.InvokeFunction(in arguments);
            watch.Stop();
            bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            samples.Add(watch.Elapsed.TotalMilliseconds);
        }

        return Build(site.Name, shape, samples, bytes);
    }

    private static Row Build(string name, string shape, List<double> samples, long bytes)
    {
        var sorted = samples.OrderBy(v => v).ToArray();
        var median = sorted[sorted.Length / 2];
        return new Row(
            name,
            shape,
            Math.Round(median, 2),
            Math.Round(median * 1_000_000 / Iterations, 1),
            bytes / Iterations);
    }
}
