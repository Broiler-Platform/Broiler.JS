using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Broiler.JavaScript.BuiltIns.RegExp;

/// <summary>
/// Counts what <see cref="RegexTiering"/> actually did on a run — how many patterns got hot
/// enough to be worth a decision, which way each decision went, and how many instances reused a
/// decision instead of racing again (docs/performance-roadmap.md phase 5, item 2).
/// </summary>
/// <remarks>
/// <para>
/// The counters exist because the mechanism's value is entirely a question about the corpus and
/// not about the mechanism. A race that never fires costs nothing and buys nothing; a race that
/// fires and refuses is the trim case working as designed; a race that fires and promotes is the
/// only shape that can move a score. Without these three numbers a wall-clock result on the
/// suite cannot be attributed to any of them, which is the failure §3.5 records as
/// "instrumented after the fact".
/// </para>
/// <para>
/// <c>PatternsRebuilt</c> is the counter this item did not need and the next one does: it counts
/// every <see cref="JSRegExp"/> construction that translated a pattern and built a
/// <see cref="System.Text.RegularExpressions.Regex"/>, which a regex literal in a loop does once
/// per evaluation because the engine caches nothing between them. It is here rather than in its
/// own type because it is measured on the same run and answers the same question — where does a
/// regex-shaped cost actually land — and putting it behind a second flag would mean two runs to
/// compare two halves of one answer.
/// </para>
/// <para>
/// Off by default; every recording site is a static read on a branch that predicts perfectly
/// while it is off, which is the bargain <c>CallPathDiagnostics</c> and
/// <c>NumberBoxingDiagnostics</c> already make.
/// </para>
/// </remarks>
public static class RegexTieringDiagnostics
{
    private static long racesRun;
    private static long racesPromoted;
    private static long verdictsReused;
    private static long verdictsReusedPromoted;
    private static long raceRounds;
    private static long patternsRebuilt;

    /// <summary>Whether tiering decisions are counted. Off by default.</summary>
    public static bool Enabled;

    /// <summary>Patterns that reached the promotion threshold and raced their compiled form.</summary>
    public static long RacesRun => Interlocked.Read(ref racesRun);

    /// <summary>Races whose compiled arm won by the adoption margin.</summary>
    public static long RacesPromoted => Interlocked.Read(ref racesPromoted);

    /// <summary>Hot instances that found a verdict already recorded for their pattern.</summary>
    public static long VerdictsReused => Interlocked.Read(ref verdictsReused);

    /// <summary>Of <see cref="VerdictsReused"/>, the ones that inherited a compiled form.</summary>
    public static long VerdictsReusedPromoted => Interlocked.Read(ref verdictsReusedPromoted);

    /// <summary>
    /// Interleaved rounds actually run across all races. Below <c>RacesRun × 8</c> means the
    /// wall-clock budget cut races short, which is the shape a slow pattern produces.
    /// </summary>
    public static long RaceRounds => Interlocked.Read(ref raceRounds);

    /// <summary>
    /// <see cref="JSRegExp"/> constructions that translated a pattern and built a matcher.
    /// </summary>
    public static long PatternsRebuilt => Interlocked.Read(ref patternsRebuilt);

    internal static void RecordRace(bool promoted)
    {
        if (!Enabled)
            return;

        Interlocked.Increment(ref racesRun);
        if (promoted)
            Interlocked.Increment(ref racesPromoted);
    }

    internal static void RecordVerdictReused(bool promoted)
    {
        if (!Enabled)
            return;

        Interlocked.Increment(ref verdictsReused);
        if (promoted)
            Interlocked.Increment(ref verdictsReusedPromoted);
    }

    internal static void RecordRaceRounds(int rounds)
    {
        if (!Enabled)
            return;

        Interlocked.Add(ref raceRounds, rounds);
    }

    internal static void RecordPatternBuilt()
    {
        if (!Enabled)
            return;

        Interlocked.Increment(ref patternsRebuilt);
    }

    /// <summary>One race, with the numbers it decided on.</summary>
    /// <param name="Pattern">The translated pattern, as the matcher holds it.</param>
    /// <param name="SubjectLength">Length of the subject the race ran on.</param>
    /// <param name="InterpretedMs">Total time the interpreted arm took across <paramref name="Rounds"/>.</param>
    /// <param name="CompiledMs">The same for the compiled arm.</param>
    /// <param name="Promoted">Whether the compiled arm won by the adoption margin.</param>
    /// <param name="Rounds">Interleaved rounds the budget allowed.</param>
    public readonly record struct RaceRecord(
        string Pattern,
        int SubjectLength,
        double InterpretedMs,
        double CompiledMs,
        bool Promoted,
        int Rounds);

    private static readonly ConcurrentQueue<RaceRecord> races = new();

    /// <summary>
    /// Every race run while the counters were on, in the order they happened.
    /// </summary>
    /// <remarks>
    /// The aggregate counters say <em>how many</em> patterns promoted; they cannot say
    /// <em>which</em>, and on this corpus that is the whole question — phase 5's standalone
    /// measurement found one pattern of seven where compiling is a 4.3× loss, so a run reporting
    /// "6 of 6 promoted" is either the race working on subjects the standalone probe did not use,
    /// or the race being wrong. Only the per-race rows separate those two, and they are cheap
    /// because a race happens at most once per pattern per process.
    /// </remarks>
    public static IReadOnlyList<RaceRecord> Races => [.. races];

    internal static void RecordRaceDetail(in RaceRecord record)
    {
        if (!Enabled)
            return;

        races.Enqueue(record);
    }

    /// <summary>Zeroes every counter.</summary>
    public static void Reset()
    {
        Interlocked.Exchange(ref racesRun, 0);
        Interlocked.Exchange(ref racesPromoted, 0);
        Interlocked.Exchange(ref verdictsReused, 0);
        Interlocked.Exchange(ref verdictsReusedPromoted, 0);
        Interlocked.Exchange(ref raceRounds, 0);
        Interlocked.Exchange(ref patternsRebuilt, 0);
        races.Clear();
    }
}
