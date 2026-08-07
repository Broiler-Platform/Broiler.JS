using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Broiler.JavaScript.BuiltIns.RegExp;

/// <summary>
/// Decides, per pattern, whether <see cref="RegexOptions.Compiled"/> is worth using — by
/// running both forms on the subject the program actually handed the pattern and keeping
/// the faster one (docs/performance-roadmap.md phase 5, item 2).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why there is no predicate here.</strong> Phase 5's measurement found
/// <c>Compiled</c> worth 1.7×–2.3× on six of seven real Octane patterns and a stable
/// <strong>4.3× against</strong> on the seventh — <c>/^[\s\xa0]+|[\s\xa0]+$/</c>, an ordinary
/// <c>trim</c>. That kills "compile after N uses", because a trim is exactly the pattern a
/// program runs hundreds of thousands of times, so a use counter finds it first and makes it
/// four times worse. Decomposing the loss found it is neither alternation nor anchoring but
/// specifically an <em>anchored character-class quantifier</em> — a rule drawn from eleven
/// patterns on one runtime, which is not a rule anyone should compile into a branch. The
/// roadmap's conclusion was that the design most likely to survive a wider corpus is the one
/// that needs no predicate at all: measure both forms once, on the real subject, and keep the
/// winner. This is that design.
/// </para>
/// <para>
/// <strong>Why it is safe.</strong> <c>RegexOptions.Compiled</c> changes code generation and
/// nothing else: the two <see cref="Regex"/> instances are built from the same pattern string
/// and the same option set, and .NET guarantees they match identically. So the race can pick
/// either arm without any observable difference, which is what makes a timing-driven decision
/// admissible at all — the engine is choosing between two implementations of one function, not
/// between two behaviours. Nothing here is reachable by script: the pattern, the flags, the
/// capture layout and <c>lastIndex</c> are untouched.
/// </para>
/// <para>
/// <strong>What the race costs and why it is bounded.</strong> A site pays for the decision
/// once: one <c>Compiled</c> construction (7–26 µs, measured) plus at most
/// <see cref="RaceBudgetMilliseconds"/> of matching, split between the arms. It is only paid
/// after <see cref="PromotionThreshold"/> matches have already gone through the pattern, so a
/// pattern used a handful of times never builds anything. The verdict is then cached by
/// (pattern, options) for the process, so the second <see cref="JSRegExp"/> built from the same
/// literal inherits it instead of racing again.
/// </para>
/// <para>
/// <strong>What it deliberately does not do.</strong> It does not re-decide. A pattern whose
/// subject distribution changes after the race keeps the arm the race chose; re-racing on a
/// schedule would make the engine's throughput depend on when the clock happened to fire, and
/// there is no measurement saying it would pay. It also does not reach a
/// <see cref="JSRegExp"/> routed to <c>Broiler.Regex</c>, which has no compiled form to
/// choose between.
/// </para>
/// <para>
/// Off unless <c>BROILER_JS_REGEX_TIERING=1</c> — see the type's own switch. The roadmap's
/// §3.5 discipline is that a change with a losing side has to be measurable against a build
/// that differs in nothing else, and this one has two: the race itself, and any pattern whose
/// race picks wrong.
/// </para>
/// </remarks>
public static class RegexTiering
{
    public const string EnvironmentVariable = "BROILER_JS_REGEX_TIERING";

    /// <summary>
    /// Matches a single <see cref="JSRegExp"/> performs before the pattern is worth a decision.
    /// </summary>
    /// <remarks>
    /// Sized against the two costs the decision carries. A <c>Compiled</c> build measured
    /// 7–26 µs and the race adds at most a further <see cref="RaceBudgetMilliseconds"/>, while a
    /// match on the patterns this is aimed at costs tens of nanoseconds; at 1 000 matches the
    /// decision is a few percent of what the pattern has already spent, and a pattern that never
    /// reaches 1 000 matches cannot repay a build no matter which arm wins.
    /// </remarks>
    public const int PromotionThreshold = 1_000;

    /// <summary>Wall-clock ceiling on one race, both arms together.</summary>
    /// <remarks>
    /// A race on a pathological pattern — a quantifier walk over a long subject — could
    /// otherwise cost more than the promotion can ever return. The arms are interleaved and the
    /// loop stops at the budget, so a slow pattern is decided on fewer rounds rather than on a
    /// longer pause.
    /// </remarks>
    private const double RaceBudgetMilliseconds = 4.0;

    /// <summary>Interleaved rounds per arm when the budget allows all of them.</summary>
    private const int RaceRounds = 8;

    /// <summary>
    /// How much faster the compiled arm must be before it is adopted.
    /// </summary>
    /// <remarks>
    /// A margin rather than a plain comparison, for two reasons that point the same way: a race
    /// that separates by less than its own noise is a coin toss, and the compiled arm costs
    /// memory that is never reclaimed (.NET emits and keeps a <c>DynamicMethod</c> per pattern).
    /// A tie should therefore keep the interpreted arm, which is what a 1.15 factor buys.
    /// </remarks>
    private const double AdoptionMargin = 1.15;

    /// <summary>Cap on distinct patterns whose verdict is remembered.</summary>
    /// <remarks>
    /// A program that builds unbounded distinct patterns — a templating layer, a linter — would
    /// otherwise grow this table for the lifetime of the process. Past the cap a site still
    /// races and still promotes; only the sharing across instances stops.
    /// </remarks>
    private const int VerdictCacheCapacity = 4_096;

    private static int enabled = ReadConfigured();

    /// <summary>Whether a hot pattern races its compiled form. Off by default.</summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }

    /// <summary>The verdict a race reached, cached so sibling instances need not race again.</summary>
    private enum Verdict
    {
        /// <summary>The race chose the interpreted arm; nothing is built for this pattern.</summary>
        Interpreted,

        /// <summary>The race chose the compiled arm, which <see cref="Promoted"/> holds.</summary>
        Compiled,
    }

    private sealed class PatternVerdict
    {
        public Verdict Verdict;

        /// <summary>
        /// The compiled <see cref="Regex"/> the race built, shared by every instance of this
        /// pattern. Null when the verdict is <see cref="Verdict.Interpreted"/>. A
        /// <see cref="Regex"/> is immutable and thread-safe for matching, so sharing one across
        /// <see cref="JSRegExp"/> instances is sound — the mutable per-instance state a JS
        /// RegExp has (<c>lastIndex</c>) lives on the object, never on the matcher.
        /// </summary>
        public Regex Promoted;
    }

    private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), PatternVerdict> Verdicts = new();

    /// <summary>
    /// Called once per <see cref="JSRegExp"/>, when its match count reaches
    /// <see cref="PromotionThreshold"/>. Applies an existing verdict for the pattern, or races
    /// the two forms on <paramref name="input"/> at <paramref name="start"/> and records one.
    /// </summary>
    /// <returns>
    /// The <see cref="Regex"/> the instance should use from now on — either the one it already
    /// had, or a compiled equivalent that matches identically.
    /// </returns>
    internal static Regex Decide(Regex interpreted, string input, int start)
    {
        // A race is a measurement, and a measurement that throws must not be able to fail the
        // program it is only observing: every arm below is inside the guard.
        try
        {
            var key = (interpreted.ToString(), interpreted.Options);
            if (Verdicts.TryGetValue(key, out var known))
            {
                RegexTieringDiagnostics.RecordVerdictReused(known.Verdict == Verdict.Compiled);
                return known.Promoted ?? interpreted;
            }

            var compiled = Build(interpreted);
            if (compiled == null)
                return interpreted;

            var adopt = CompiledWins(interpreted, compiled, input, start, out var race);
            RegexTieringDiagnostics.RecordRaceDetail(
                new RegexTieringDiagnostics.RaceRecord(
                    key.Item1,
                    input?.Length ?? 0,
                    race.InterpretedMs,
                    race.CompiledMs,
                    adopt,
                    race.Rounds));

            var verdict = new PatternVerdict
            {
                Verdict = adopt ? Verdict.Compiled : Verdict.Interpreted,
                Promoted = adopt ? compiled : null,
            };

            if (Verdicts.Count < VerdictCacheCapacity)
                verdict = Verdicts.GetOrAdd(key, verdict);

            RegexTieringDiagnostics.RecordRace(verdict.Verdict == Verdict.Compiled);
            return verdict.Promoted ?? interpreted;
        }
        catch
        {
            return interpreted;
        }
    }

    /// <summary>
    /// Rebuilds <paramref name="interpreted"/> with <see cref="RegexOptions.Compiled"/> added.
    /// </summary>
    /// <remarks>
    /// The pattern and options are read back off the instance rather than threaded down from
    /// <c>CreateRegex</c>, because by this point the pattern has been through every one of that
    /// method's ECMAScript translations — anchor rewriting, quantifier clamping, capture-group
    /// renaming — and <see cref="Regex.ToString"/> is the only place the translated form
    /// survives. Reconstructing it from <c>JSRegExp.pattern</c> would re-run the translation and
    /// risk racing a <em>different</em> pattern than the one being replaced.
    /// </remarks>
    private static Regex Build(Regex interpreted)
    {
        try
        {
            return new Regex(
                interpreted.ToString(),
                interpreted.Options | RegexOptions.Compiled,
                interpreted.MatchTimeout);
        }
        catch
        {
            // RegexOptions.Compiled is rejected in combination with some option sets, and
            // emitting IL can fail on a constrained host. Either way the pattern keeps the arm
            // it already has.
            return null;
        }
    }

    /// <summary>
    /// Times both arms on the same subject, interleaved, and reports whether the compiled one
    /// wins by <see cref="AdoptionMargin"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>ABBA, not A-then-B.</strong> The arms alternate and each round is charged to its
    /// own accumulator, so a machine that speeds up or slows down across the race — a frequency
    /// step, a collection, another thread arriving — moves both arms rather than the one that
    /// happened to run second. That is §3.5's rule applied to a race the engine runs on itself.
    /// </para>
    /// <para>
    /// <strong>Both arms are warmed first.</strong> The compiled one has just been constructed
    /// and has not run its emitted code yet, so its first match pays a one-time JIT cost that
    /// belongs to construction and not to matching. Timing it without the warm-up would charge
    /// the compiled arm for its own build a second time and refuse patterns that deserve
    /// promotion.
    /// </para>
    /// </remarks>
    private static bool CompiledWins(
        Regex interpreted,
        Regex compiled,
        string input,
        int start,
        out (double InterpretedMs, double CompiledMs, int Rounds) race)
    {
        race = default;

        // `start` is a valid index into `input` on every path that reaches here (RunMatch is the
        // only caller and it has already been given one), but the race must not be the thing
        // that throws if that ever stops being true.
        if (input == null || (uint)start > (uint)input.Length)
            return false;

        interpreted.Match(input, start);
        compiled.Match(input, start);

        var watch = Stopwatch.StartNew();
        double interpretedTicks = 0, compiledTicks = 0;
        var rounds = 0;

        for (var i = 0; i < RaceRounds; i++)
        {
            var before = watch.Elapsed.TotalMilliseconds;
            interpreted.Match(input, start);
            var afterInterpreted = watch.Elapsed.TotalMilliseconds;
            compiled.Match(input, start);
            var afterCompiled = watch.Elapsed.TotalMilliseconds;

            interpretedTicks += afterInterpreted - before;
            compiledTicks += afterCompiled - afterInterpreted;
            rounds++;

            if (afterCompiled >= RaceBudgetMilliseconds)
                break;
        }

        RegexTieringDiagnostics.RecordRaceRounds(rounds);
        race = (interpretedTicks, compiledTicks, rounds);

        // A race whose arms both measured zero says the pattern is far too cheap on this
        // subject for the clock to separate them, which is itself a reason to keep the arm that
        // costs no memory.
        if (compiledTicks <= 0)
            return interpretedTicks > 0;

        return interpretedTicks >= compiledTicks * AdoptionMargin;
    }

    private static int ReadConfigured()
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "1",
            StringComparison.Ordinal)
            ? 1
            : 0;

    /// <summary>Drops every cached verdict. For tests, which must not inherit each other's races.</summary>
    public static void ResetForTests() => Verdicts.Clear();
}
