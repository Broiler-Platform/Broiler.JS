using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Runs phase 5's eleven patterns <em>through the engine</em>, on both settings of
/// <see cref="RegexTiering"/>, and reports which way each race went and what it was worth
/// (docs/performance-roadmap.md phase 5, item 2).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this exists beside <c>--regex-profile</c>'s <c>netEngine</c> table.</strong> That
/// table measures <c>System.Text.RegularExpressions</c> directly — two <c>Regex</c> objects, a
/// tight <c>IsMatch</c> loop, no engine around them — which was the right instrument for the
/// question it asked (<em>what does the default engine leave on the table by not compiling?</em>)
/// and answered it: about 2× on six patterns and 4.3× <em>against</em> on the seventh. It cannot
/// answer this item's question, because the policy has to pay for itself through
/// <c>JSRegExp.RunMatch</c>, a <c>lastIndex</c> read and write, an exec-result shape and a
/// countdown — and because the race's own decision is the thing under test. `0108` is the
/// standing lesson: a mechanism replicated outside the engine is measured without the engine's
/// insides.
/// </para>
/// <para>
/// <strong>The arms.</strong> Identical JavaScript on both, one process, ABBA-interleaved with
/// the off arm first and last, medians reported. Each shape runs the same pattern past the
/// promotion threshold, so the on arm is guaranteed to have made its decision inside the
/// measured region — deliberately, because the race's cost is part of what the arm has to
/// carry. The verdict column comes from <see cref="RegexTieringDiagnostics"/> on a separate
/// counting pass, so the timed arms carry no counters.
/// </para>
/// <para>
/// <strong>Read the verdict column before the ratio.</strong> A shape whose race refused
/// promotion should read ~1.00× and its residual is the race's own cost, paid once; a shape that
/// promoted should read the compiled arm's advantage net of everything the engine does around
/// the match. A refused shape reading materially below 1.00× would mean the countdown or the
/// race is costing something on the steady-state path, which is the one way this mechanism can
/// be wrong without being incorrect.
/// </para>
/// </remarks>
internal static class RegexTieringMetrics
{
    /// <summary>Matches per timed arm. Comfortably past the promotion threshold.</summary>
    private const int Matches = 200_000;

    /// <summary>Interleaved pairs. Odd count so the off arm both opens and closes the set.</summary>
    private const int Pairs = 7;

    /// <summary>
    /// The same eleven patterns and subjects <c>--regex-profile</c>'s <c>netEngine</c> table
    /// uses, so the two are directly comparable — seven lifted from Octane's own
    /// <c>regexp.js</c>, four decomposing the one that loses.
    /// </summary>
    private static readonly (string Name, string Pattern, string Subject)[] Patterns =
    [
        ("caret-literal", "^ba", "bananas and bandanas, ba ba ba"),
        ("comma-split", ",", "alpha,beta,gamma,delta,epsilon,zeta,eta,theta"),
        ("trim", @"^[\s\xa0]+|[\s\xa0]+$", "        padded on both sides, and then some       "),
        ("hyphen-lower", "(-[a-z])", "background-color and border-top-width and margin-left"),
        ("char-set", "[+, ]", "one+two, three four+five, six seven"),
        ("cookie-pair", "TNQP=([^;]*)", "a=1; b=2; TNQP=deadbeefcafe; c=3; d=4"),
        ("angle-brackets", "[<>]", "<div class='x'>text</div><span>more</span>"),
        ("probe-anchor-start", @"^[\s\xa0]+", "        padded on both sides, and then some       "),
        ("probe-anchor-end", @"[\s\xa0]+$", "        padded on both sides, and then some       "),
        ("probe-alt-plain", @"[\s\xa0]+|zzz", "        padded on both sides, and then some       "),
        ("probe-alt-anchored", @"^a+|b+$", "aaa padded on both sides, and then some       bbb"),
    ];

    private sealed record Row(
        string Site,
        string Pattern,
        double OffMs,
        double OnMs,
        double Speedup,
        int PairsWonByOn,
        bool Promoted,
        long RaceRounds);

    internal static void Write()
    {
        var rows = Patterns.Select(Measure).ToArray();

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.regex-tiering-metrics/1",
                matchesPerArm = Matches,
                pairs = Pairs,
                promotionThreshold = RegexTiering.PromotionThreshold,
                note = "Phase 5 item 2, measured through the engine rather than against "
                    + "System.Text.RegularExpressions directly. `off` is the shipping default; "
                    + "`on` races RegexOptions.Compiled against the interpreted form on the real "
                    + "subject once the pattern has matched "
                    + RegexTiering.PromotionThreshold
                    + " times, and keeps the winner. Read `promoted` before `speedup`: a refused "
                    + "shape should read about 1.00x, and anything materially below it means the "
                    + "countdown or the race is costing the steady state. Time on a shared "
                    + "container is for prioritization only (roadmap 3.1); `promoted` and "
                    + "`pairsWonByOn` are the columns that carry a verdict.",
                rows,
                summary = new
                {
                    promoted = rows.Count(r => r.Promoted),
                    refused = rows.Count(r => !r.Promoted),
                    // The seven real Octane patterns alone, which is the population phase 5's
                    // own table measured; the four probes decompose one of them and would
                    // double-count it in any aggregate.
                    octanePatternsPromoted = rows.Take(7).Count(r => r.Promoted),
                },
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Row Measure((string Name, string Pattern, string Subject) site)
    {
        var (promoted, raceRounds) = Verdict(site);

        var off = new List<double>(Pairs);
        var on = new List<double>(Pairs);
        var wonByOn = 0;

        for (var pair = 0; pair < Pairs; pair++)
        {
            // ABBA within the pair, and the pair order alternates, so a machine drifting across
            // the set moves both arms rather than whichever ran second (roadmap 3.5).
            double offMs, onMs;
            if (pair % 2 == 0)
            {
                offMs = TimeArm(site, tiering: false);
                onMs = TimeArm(site, tiering: true);
            }
            else
            {
                onMs = TimeArm(site, tiering: true);
                offMs = TimeArm(site, tiering: false);
            }

            off.Add(offMs);
            on.Add(onMs);
            if (onMs < offMs)
                wonByOn++;
        }

        var offMedian = Median(off);
        var onMedian = Median(on);

        return new Row(
            site.Name,
            site.Pattern,
            Math.Round(offMedian, 2),
            Math.Round(onMedian, 2),
            Math.Round(onMedian <= 0 ? 0 : offMedian / onMedian, 3),
            wonByOn,
            promoted,
            raceRounds);
    }

    /// <summary>
    /// A separate counting pass: which way the race went for this pattern, with the diagnostics
    /// on. Kept out of the timed arms so those carry no interlocked increments.
    /// </summary>
    private static (bool Promoted, long RaceRounds) Verdict((string Name, string Pattern, string Subject) site)
    {
        RegexTiering.ResetForTests();
        RegexTieringDiagnostics.Reset();
        RegexTiering.Enabled = true;
        RegexTieringDiagnostics.Enabled = true;

        try
        {
            using var context = BenchmarkContext.Create();
            Run(context, site, RegexTiering.PromotionThreshold + 50);
            return (RegexTieringDiagnostics.RacesPromoted > 0, RegexTieringDiagnostics.RaceRounds);
        }
        finally
        {
            RegexTieringDiagnostics.Enabled = false;
            RegexTiering.Enabled = false;
        }
    }

    private static double TimeArm((string Name, string Pattern, string Subject) site, bool tiering)
    {
        // Every arm starts from an empty verdict table: a promotion carried over from the
        // previous arm would hand the off arm a compiled matcher it is supposed to be the
        // control for.
        RegexTiering.ResetForTests();
        RegexTiering.Enabled = tiering;

        try
        {
            using var context = BenchmarkContext.Create();

            // Warm the compile, the pattern build and the inline caches; then measure a fresh
            // regex so the countdown starts from zero inside the timed region.
            Run(context, site, 1_000);

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            var watch = Stopwatch.StartNew();
            Run(context, site, Matches);
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds;
        }
        finally
        {
            RegexTiering.Enabled = false;
        }
    }

    private static void Run(JSContext context, (string Name, string Pattern, string Subject) site, int matches)
    {
        // A fresh RegExp per invocation, built from a literal, matched `n` times: the shape a
        // program that keeps a pattern in a variable and uses it in a loop actually has.
        var source = $$"""
            (function (n) {
                var re = /{{site.Pattern}}/;
                var subject = {{JsonSerializer.Serialize(site.Subject)}};
                var sink = 0;
                for (var i = 0; i < n; i++) { if (re.test(subject)) sink++; }
                return sink;
            })
            """;

        var function = context.Eval(source, $"{site.Name}.js");
        function.InvokeFunction(new Arguments(JSUndefined.Value, new JSNumber(matches)));
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        return sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
    }
}
