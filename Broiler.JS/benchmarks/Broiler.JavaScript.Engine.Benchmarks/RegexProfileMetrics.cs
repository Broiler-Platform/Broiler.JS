using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NetRegex = System.Text.RegularExpressions.Regex;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits, per regex shape, what one character of subject text costs the matcher in time and in
/// allocated bytes — separately for the SCAN that finds no match and the MATCH that succeeds
/// where it starts.
/// </summary>
/// <remarks>
/// Exists to satisfy phase 5's gate, which is a measurement and not a change: "profile
/// <c>Matching/Matcher.cs</c> against the Octane regex corpus <em>first</em>, to separate
/// backtracking <em>strategy</em> from per-step <em>interpretive overhead</em>". The two want
/// different fixes — strategy is a literal-prefix scan, a first-character filter or memoization,
/// interpretive overhead is compilation to native code — and committing to the second before
/// measuring the first is how a rewrite gets chosen for the wrong reason.
/// <para>
/// The split is measurable without instrumenting the engine, which matters because
/// <c>Broiler.Regex</c> is a nested submodule and the gate is explicit that profiling comes
/// before touching it. <c>Matcher.Run</c> retries the whole pattern at every start position, so
/// a pattern that never matches does <em>length</em> attempts and a pattern anchored at 0 does
/// one. Running the same pattern against both, over the same subject, isolates the per-attempt
/// cost from the per-match cost by subtraction — and the per-attempt cost is what a better
/// strategy removes, while the per-match cost is what a compiler removes.
/// </para>
/// <para>
/// Every row is reported against two floors: <c>indexOf</c> for the same literal, which is what
/// scanning that subject costs when the scan is not a regex at all, and an inert loop. A regex
/// shape that costs what <c>indexOf</c> costs has no strategy problem worth solving.
/// </para>
/// </remarks>
internal static class RegexProfileMetrics
{
    /// <summary>Subject length. Large enough that per-character costs dominate the call.</summary>
    private const int SubjectLength = 20_000;

    private const int Repetitions = 20;

    private sealed record Site(string Name, string Setup, string Body, string Note);

    /// <summary>The loop with no regex in it, so a row can be read net of what carries it.</summary>
    private static readonly Site LoopControl = new(
        "loop-control",
        "var subject = 'a'.repeat(" + SubjectLength + ");",
        "sink = subject.length;",
        "no matching at all; subtract to get the shape's own cost");

    private static readonly Site[] Sites =
    [
        new(
            "indexOf-miss",
            "var subject = 'a'.repeat(" + SubjectLength + ");",
            "sink = subject.indexOf('zqx');",
            "the FLOOR: scanning this subject for a literal without a regex. Any regex row far "
                + "above this is paying for the engine, not for the search"),

        new(
            "literal-miss",
            "var subject = 'a'.repeat(" + SubjectLength + "); var re = /zqx/;",
            "sink = re.test(subject);",
            "the same search as the floor, through the matcher. Matcher.Run retries the pattern "
                + "at every start position, so this is length attempts — each allocating a Budget "
                + "and a fresh capture array before any character is compared. This row is "
                + "STRATEGY: a literal-prefix scan or a first-character filter removes it"),

        new(
            "literal-hit-at-0",
            "var subject = 'zqx' + 'a'.repeat(" + SubjectLength + "); var re = /zqx/;",
            "sink = re.test(subject);",
            "the same pattern where the first attempt succeeds. What remains after subtracting "
                + "this from literal-miss is the scan; what remains here is one match"),

        new(
            "charclass-miss",
            "var subject = 'a'.repeat(" + SubjectLength + "); var re = /[0-9]+/;",
            "sink = re.test(subject);",
            "a quantified class that fails at every position: one class test per attempt, so it "
                + "isolates the per-attempt overhead from the per-character comparison"),

        new(
            "alternation-miss",
            "var subject = 'a'.repeat(" + SubjectLength + "); var re = /(?:zq|zr|zs)x/;",
            "sink = re.test(subject);",
            "three alternatives tried per start position; against literal-miss it says what a "
                + "disjunction costs per attempt"),

        new(
            "capture-heavy-hit",
            "var subject = 'abcdefgh'.repeat(" + (SubjectLength / 8) + "); var re = /(a)(b)(c)(d)(e)(f)(g)(h)/;",
            "sink = re.exec(subject) !== null;",
            "eight capturing groups matching at position 0. MatchState is immutable and "
                + "WithCapture CLONES the whole capture array per write, so this is the "
                + "copy-on-write cost — interpretive overhead a compiled matcher does not pay"),

        new(
            "capture-free-hit",
            "var subject = 'abcdefgh'.repeat(" + (SubjectLength / 8) + "); var re = /(?:a)(?:b)(?:c)(?:d)(?:e)(?:f)(?:g)(?:h)/;",
            "sink = re.exec(subject) !== null;",
            "the same match with non-capturing groups: the control for the row above, so the "
                + "difference is the cloning and nothing else"),

        new(
            "quantifier-walk",
            "var subject = 'a'.repeat(" + SubjectLength + "); var re = /a*b/;",
            "sink = re.test(subject);",
            "the quantifier consumes the whole subject and then fails, at every start position — "
                + "the shape the step Budget is counting. Quadratic by construction, and the "
                + "clearest STRATEGY row here: memoization or a fail-fast on the required literal "
                + "removes the repetition, not a faster interpreter"),

        new(
            "global-replace",
            "var subject = 'abcdefgh'.repeat(" + (SubjectLength / 8) + "); var re = /[aeiou]/g;",
            "sink = subject.replace(re, 'x').length;",
            "a real workload rather than a probe: repeated matching with a global flag, which is "
                + "how PdfJS and Typescript reach this engine"),
    ];

    /// <summary>
    /// Real patterns lifted from <c>tests/octane/checkout/regexp.js</c>, matched against a
    /// subject shaped like the one the suite feeds them.
    /// </summary>
    /// <remarks>
    /// These exist because the JS-level rows above cannot answer the question phase 5 actually
    /// turns on. <c>JSRegExp</c> keeps <c>System.Text.RegularExpressions</c> as the DEFAULT
    /// engine and routes only semantic-gap patterns (a look-behind with captures, a
    /// back-reference under <c>u</c>, a nullable quantifier) to <c>Broiler.Regex</c> — and
    /// Octane's corpus contains no look-behind and no <c>u</c> flag at all, so essentially none
    /// of it reaches <c>Matching/Matcher.cs</c>. The engine that IS on that path is constructed
    /// as <c>new Regex(pattern, RegexOptions.ECMAScript)</c>, with no <c>RegexOptions.Compiled</c>
    /// anywhere on the user-regex path, so it runs .NET's INTERPRETER.
    /// <para>
    /// So the measurement phase 5 needs first is not "what does the closure matcher cost", it is
    /// "what does the default engine leave on the table by not compiling". That is one flag, and
    /// this measures it on the suite's own patterns.
    /// </para>
    /// </remarks>
    private static readonly (string Name, string Pattern, string Subject)[] NetEnginePatterns =
    [
        ("caret-literal", "^ba", "bananas and bandanas, ba ba ba"),
        ("comma-split", ",", "alpha,beta,gamma,delta,epsilon,zeta,eta,theta"),
        ("trim", @"^[\s\xa0]+|[\s\xa0]+$", "        padded on both sides, and then some       "),
        ("hyphen-lower", "(-[a-z])", "background-color and border-top-width and margin-left"),
        ("char-set", "[+, ]", "one+two, three four+five, six seven"),
        ("cookie-pair", "TNQP=([^;]*)", "a=1; b=2; TNQP=deadbeefcafe; c=3; d=4"),
        ("angle-brackets", "[<>]", "<div class='x'>text</div><span>more</span>"),

        // Anomaly probe. `trim` above is the one pattern Compiled makes DRAMATICALLY worse
        // (~4.3x, stable across runs), and a per-pattern policy has to be able to say why.
        // These decompose it: anchor alone, each branch alone, alternation without anchors,
        // and alternation of anchored branches.
        ("probe-anchor-start", @"^[\s\xa0]+", "        padded on both sides, and then some       "),
        ("probe-anchor-end", @"[\s\xa0]+$", "        padded on both sides, and then some       "),
        ("probe-alt-plain", @"[\s\xa0]+|zzz", "        padded on both sides, and then some       "),
        ("probe-alt-anchored", @"^a+|b+$", "aaa padded on both sides, and then some       bbb"),
    ];

    /// <summary>
    /// The same operation at three subject lengths, reported per CALL rather than per character.
    /// </summary>
    /// <remarks>
    /// A per-character figure cannot tell a fixed per-call cost from one that scales with the
    /// subject — 4 400 bytes on a 20 000-character subject reads as 0.22 B/char either way. This
    /// is the discriminator: a flat bytes-per-call column means O(1) and the per-character
    /// framing is misleading; a column that triples with the subject means something still walks
    /// or copies it.
    /// </remarks>
    private static Row[] MeasureScaling()
    {
        var rows = new List<Row>();
        foreach (var length in new[] { 5_000, 20_000, 80_000 })
        {
            foreach (var (name, setup, body) in new[]
            {
                ("test-hit", "var re = /zqx/;", "sink = re.test(subject);"),
                ("exec-hit", "var re = /zqx/;", "sink = re.exec(subject) !== null;"),
                ("replace-one", "var re = /zqx/;", "sink = subject.replace(re, 'y').length;"),
            })
            {
                rows.Add(MeasureCalls($"{name}@{length}", length, setup, body));
            }
        }

        return [.. rows];
    }

    private static Row MeasureCalls(string name, int length, string setup, string body)
    {
        using var context = BenchmarkContext.Create();
        const int Calls = 200;

        var source = $$"""
            (function (n) {
                var subject = 'zqx' + 'a'.repeat({{length}});
                {{setup}}
                var sink = 0;
                for (var i = 0; i < n; i++) { {{body}} }
                return sink;
            })
            """;

        var function = context.Eval(source, $"{name}.js");
        var arguments = new Arguments(JSUndefined.Value, new JSNumber(Calls));
        function.InvokeFunction(in arguments);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        function.InvokeFunction(in arguments);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        return new Row(name, "bytes per CALL, subject length in the name", 0, 0, null,
            Math.Round((double)bytes / Calls, 1), 0, bytes);
    }

    internal static void Write()
    {
        var control = Measure(LoopControl);
        var rows = Sites.Select(site => Measure(site, control)).ToArray();
        var netRows = NetEnginePatterns.Select(MeasureNetEngine).ToArray();

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.regex-profile-metrics/2",
                subjectLength = SubjectLength,
                repetitions = Repetitions,
                note = "Nanoseconds and allocated bytes per subject CHARACTER, warmed then "
                    + "measured after a forced gen2 collection, reported net of a loop control "
                    + "carrying no matching. Time on a developer workstation is for "
                    + "prioritization only (roadmap 3.1); the allocation column is "
                    + "deterministic. See docs/performance-roadmap.md phase 5.",
                loopControl = control,
                sites = rows,
                netEngineNote = "The DEFAULT engine for a JS regex, measured directly: "
                    + "RegexOptions.ECMAScript as the engine builds it today, against the same "
                    + "pattern with RegexOptions.Compiled added. Compile time is reported "
                    + "separately because Compiled pays it once per pattern and Octane builds "
                    + "its patterns once.",
                netEngine = netRows,
                scaling = MeasureScaling(),
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static NetRow MeasureNetEngine((string Name, string Pattern, string Subject) site)
    {
        const int Iterations = 200_000;
        const RegexOptions AsShipped = RegexOptions.ECMAScript;

        var interpreted = new NetRegex(site.Pattern, AsShipped);
        var compiled = new NetRegex(site.Pattern, AsShipped | RegexOptions.Compiled);

        // Warm both, including the Compiled one's IL generation, so the loops below time
        // matching and not construction.
        for (var i = 0; i < 1_000; i++)
        {
            interpreted.IsMatch(site.Subject);
            compiled.IsMatch(site.Subject);
        }

        var interpretedMs = TimeMatches(interpreted, site.Subject, Iterations);
        var compiledMs = TimeMatches(compiled, site.Subject, Iterations);

        // Construction cost, which is what Compiled trades for throughput. Measured on a fresh
        // pattern string each time so nothing is cached between rounds.
        var interpretedBuild = TimeBuilds(site.Pattern, AsShipped, 200);
        var compiledBuild = TimeBuilds(site.Pattern, AsShipped | RegexOptions.Compiled, 200);

        return new NetRow(
            site.Name,
            site.Pattern,
            Math.Round(interpretedMs, 2),
            Math.Round(compiledMs, 2),
            Math.Round(compiledMs <= 0 ? 0 : interpretedMs / compiledMs, 3),
            Math.Round(interpretedBuild, 4),
            Math.Round(compiledBuild, 4));
    }

    private static double TimeMatches(NetRegex regex, string subject, int iterations)
    {
        var watch = Stopwatch.StartNew();
        var sink = 0;
        for (var i = 0; i < iterations; i++)
        {
            if (regex.IsMatch(subject))
                sink++;
        }

        watch.Stop();
        GC.KeepAlive(sink);
        return watch.Elapsed.TotalMilliseconds;
    }

    private static double TimeBuilds(string pattern, RegexOptions options, int iterations)
    {
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            // A distinct pattern string each time, so .NET's own Regex cache cannot answer.
            var built = new NetRegex(pattern + (i == -1 ? "x" : ""), options);
            GC.KeepAlive(built);
        }

        watch.Stop();
        return watch.Elapsed.TotalMilliseconds / iterations;
    }

    /// <summary>One pattern through the engine that actually serves it today.</summary>
    internal sealed record NetRow(
        string Site,
        string Pattern,
        double InterpretedMs,
        double CompiledMs,
        double Speedup,
        double InterpretedBuildMs,
        double CompiledBuildMs);

    private static Row Measure(Site site, Row control = null)
    {
        using var context = BenchmarkContext.Create();

        var source = $$"""
            (function (n) {
                {{site.Setup}}
                var sink = 0;
                for (var i = 0; i < n; i++) { {{site.Body}} }
                return sink;
            })
            """;

        var function = context.Eval(source, $"{site.Name}.js");
        var arguments = new Arguments(JSUndefined.Value, new JSNumber(Repetitions));

        // Warmed first: the compile, the pattern's own parse-and-compile, and the inline-cache
        // entries are one-time and would otherwise land in the measured deltas.
        function.InvokeFunction(in arguments);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        function.InvokeFunction(in arguments);
        watch.Stop();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        var characters = (double)SubjectLength * Repetitions;
        var nanosecondsPerCharacter = Math.Round(watch.Elapsed.TotalMilliseconds * 1_000_000 / characters, 2);
        var bytesPerCharacter = Math.Round(bytes / characters, 2);

        return new Row(
            site.Name,
            site.Note,
            nanosecondsPerCharacter,
            bytesPerCharacter,
            control == null ? null : Math.Round(nanosecondsPerCharacter - control.NanosecondsPerCharacter, 2),
            control == null ? null : Math.Round(bytesPerCharacter - control.BytesPerCharacter, 2),
            Math.Round(watch.Elapsed.TotalMilliseconds, 1),
            bytes);
    }

    /// <summary>One measured shape. The <c>net</c> figures are what phase 5 is about.</summary>
    internal sealed record Row(
        string Site,
        string Note,
        double NanosecondsPerCharacter,
        double BytesPerCharacter,
        double? NetNanosecondsPerCharacter,
        double? NetBytesPerCharacter,
        double TotalMilliseconds,
        long TotalBytes);
}
