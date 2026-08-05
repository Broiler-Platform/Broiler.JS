#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler;

/// <summary>
/// Item 1-1's remaining half: the capture layout computed <em>without</em> a body tree, checked
/// against the one <see cref="LambdaRewriter"/> derives <em>from</em> the tree.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the item's own named sub-project, and it is the whole of what blocks it.</b> The
/// roadmap states the obstacle precisely: the capture mechanism is not missing — a creation site
/// already passes a <c>Box[]</c> and a compiler that knows a name's <em>index</em> in it binds
/// that name with an array load — it is <em>unaddressable</em>, because the index is decided by
/// <c>LambdaRewriter</c> walking the enclosing tree, and a deferred body has no tree to walk.
/// </para>
/// <para>
/// <b>So the layout has to be derivable from the source alone, and the only thing that matters
/// about it is that it never misses.</b> Over-approximating boxes a binding that did not need it —
/// a cost, paid once per creation site, and the enclosing function loses that name's numeric tier.
/// Under-approximating means a deferred body resolves a name to a box that is not there, which is
/// a <em>miscompile</em>. The two failure modes are not comparable, and the go/no-go for the whole
/// item is whether the tree-free prediction ever misses on real source.
/// </para>
/// <para>
/// <b>Prediction and truth are compared by reference identity on the
/// <see cref="BParameterExpression"/>, not by name</b>, because names are exactly what a
/// comparison here must not trust: two bindings can share a spelling across scopes, and an
/// agreement on spellings would hide a disagreement on bindings. The predicted set is recorded by
/// the front end against the lambda it belongs to; the actual set is read off
/// <see cref="ClosureRepository"/> at relay, once the rewrite that populates it has run.
/// </para>
/// <para>
/// Off by default. It costs a <c>FreeNameScan</c> and a scope resolution per compiled function,
/// which is compile time nothing that ships needs to pay.
/// </para>
/// </remarks>
public static class DeferredCaptureLayout
{
    public const string EnvironmentVariable = "BROILER_JS_CAPTURE_LAYOUT_CHECK";

    private static int checking = string.Equals(
        System.Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        System.StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether the front end records a predicted capture set and the relay checks it.</summary>
    public static bool Checking
    {
        get => Volatile.Read(ref checking) != 0;
        set => Volatile.Write(ref checking, value ? 1 : 0);
    }

    private static readonly ConditionalWeakTable<BLambdaExpression, HashSet<BParameterExpression>> predicted = [];

    /// <summary>
    /// The same prediction keyed by SPELLING rather than by reference, kept beside the identity
    /// one because the gap between them is a finding rather than an implementation detail — see
    /// <see cref="MissedNamesByName"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<BLambdaExpression, HashSet<string>> predictedNamesBySpelling = [];

    private static long missedByName;

    /// <summary>
    /// A lambda's first verdict, kept so a second relay of the same site can be recognised rather
    /// than counted again.
    /// </summary>
    private sealed class Verdict
    {
        public int Missed;
        public int Over;
        public int Real;
    }

    private static readonly ConditionalWeakTable<BLambdaExpression, Verdict> verdicts = [];

    private static readonly ConditionalWeakTable<BLambdaExpression, object> undeferrable = [];
    private static long excludedSites;

    /// <summary>
    /// Sites excluded from the comparison because the mechanism refuses to defer them at all — a
    /// direct <c>eval</c>, a <c>with</c> or a <c>debugger</c> in the body.
    /// </summary>
    /// <remarks>
    /// <b>Recording these as an empty prediction was a defect in this checker, and a costly one to
    /// read.</b> A <see cref="FreeNameScan.Dynamic"/> body can reach bindings its text never names,
    /// so there is no set to be right about — but an empty prediction compared against a real
    /// capture set reports every one of those captures as a <em>miss</em>, which is the strongest
    /// signal this instrument has. Mandreel, PdfJS and Typescript each contributed misses reading
    /// <c>predicted{}</c> for exactly that reason. Excluding them is not leniency: a site the
    /// mechanism will not defer needs no layout.
    /// </remarks>
    public static long ExcludedSites => Interlocked.Read(ref excludedSites);

    /// <summary>Records that this lambda cannot be deferred, so its layout is not a question.</summary>
    public static void Undeferrable(BLambdaExpression lambda)
    {
        undeferrable.AddOrUpdate(lambda, null!);
        Interlocked.Increment(ref excludedSites);
    }

    private static long checks;
    private static long repeatChecks;
    private static long repeatDisagreements;

    /// <summary>Calls to <see cref="Check"/> that found a prediction, repeats included.</summary>
    public static long Checks => Interlocked.Read(ref checks);

    /// <summary>
    /// Calls that were a SECOND (or later) relay of a lambda already checked.
    /// </summary>
    /// <remarks>
    /// <b>The first corpus run reported more checks than predictions</b> — Mandreel 2 622 exact
    /// against 1 476 predicted sites — which can only mean a site is relayed more than once, since
    /// a lambda with no prediction is not counted at all. A per-site question must be answered once
    /// per site, so repeats are recognised here instead of being added to the totals.
    /// </remarks>
    public static long RepeatChecks => Interlocked.Read(ref repeatChecks);

    /// <summary>
    /// Repeats whose verdict differed from the first. Zero makes the repeat pure duplication;
    /// non-zero would mean the rewrite decides a different capture set on a later relay, which is
    /// a finding about the rewrite rather than about the counter.
    /// </summary>
    public static long RepeatDisagreements => Interlocked.Read(ref repeatDisagreements);

    /// <summary>
    /// Missed captures when prediction and truth are matched on the binding's SPELLING instead of
    /// on the <see cref="BParameterExpression"/> instance. Compared against
    /// <see cref="MissedNames"/>, the difference is the population the front end re-binds after a
    /// nested function has already been created.
    /// </summary>
    public static long MissedNamesByName => Interlocked.Read(ref missedByName);

    private static long sites;
    private static long exact;
    private static long over;
    private static long missed;
    private static long predictedNames;
    private static long actualNames;
    private static long missedNames;
    private static long overNames;
    private static long syntheticNames;

    /// <summary>
    /// Spellings of the first few missed bindings. A count alone cannot be acted on — the whole
    /// question is <em>which</em> binding a source-derived walk failed to name.
    /// </summary>
    private static readonly List<string> missedSamples = [];

    public static IReadOnlyList<string> MissedNameSamples
    {
        get { lock (missedSamples) return missedSamples.ToArray(); }
    }

    /// <summary>Sites where prediction and truth agreed exactly.</summary>
    public static long ExactSites => Interlocked.Read(ref exact);

    /// <summary>Sites where the prediction was a strict superset — safe, and a cost.</summary>
    public static long OverApproximatedSites => Interlocked.Read(ref over);

    /// <summary>
    /// Sites where a real capture was NOT predicted. The number that decides the item: any value
    /// above zero is a miscompile waiting for the mechanism to be built on it.
    /// </summary>
    public static long MissedSites => Interlocked.Read(ref missed);

    /// <summary>Sites the front end recorded a prediction for.</summary>
    public static long Sites => Interlocked.Read(ref sites);

    public static long PredictedNames => Interlocked.Read(ref predictedNames);

    public static long ActualNames => Interlocked.Read(ref actualNames);

    public static long MissedNames => Interlocked.Read(ref missedNames);

    public static long OverApproximatedNames => Interlocked.Read(ref overNames);

    /// <summary>
    /// Captured bindings that are not identifiers in the source at all — <c>this</c>,
    /// <c>arguments</c>, <c>new.target</c>, a compiler temporary. A free-name walk cannot predict
    /// them and must not be charged for them; the mechanism handles them by their own rules, the
    /// way the enclosing scope already does.
    /// </summary>
    public static long SyntheticNames => Interlocked.Read(ref syntheticNames);

    /// <summary>Records what the front end predicts this lambda captures, from source alone.</summary>
    public static void Predict(BLambdaExpression lambda, HashSet<BParameterExpression> captures)
    {
        predicted.AddOrUpdate(lambda, captures);
        var spellings = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var c in captures)
            if (!string.IsNullOrEmpty(c.Name))
                spellings.Add(c.Name);
        predictedNamesBySpelling.AddOrUpdate(lambda, spellings);
        Interlocked.Increment(ref sites);
        Interlocked.Add(ref predictedNames, captures.Count);
    }

    /// <summary>
    /// Compares the recorded prediction against what the rewrite actually decided. Called at
    /// relay, after <see cref="LambdaRewriter"/> has populated the repository.
    /// </summary>
    /// <summary>
    /// Compares the prediction against the bindings this lambda has <em>handed in</em>.
    /// </summary>
    /// <remarks>
    /// <b>The set to compare against is not <c>Closures.Keys</c>, and getting that wrong is the
    /// first thing this checker found.</b> A lambda's repository holds two populations that the
    /// item's statement of the obstacle conflates: bindings threaded in from an enclosing scope
    /// (<c>Setup</c>, which appends to <c>Inputs</c> and records <c>index >= 0</c>) and the
    /// lambda's <em>own</em> locals that something nested captures, which must live in a cell but
    /// are not handed to it (<c>Convert</c>, <c>index == -1</c>). Only the first is what a
    /// deferred body needs a <c>Box[]</c> index for; the second is what the enclosing function
    /// must box on its children's behalf, and a free-name walk of the enclosing function correctly
    /// does not name it. Compared against the whole repository, every function that holds a
    /// captured local reads as a miss — which is what happened, on the simplest fixture there is.
    /// </remarks>
    public static void Check(BLambdaExpression lambda, ClosureRepository repository)
    {
        var actual = new List<BParameterExpression>();
        foreach (var entry in repository.Closures)
            if (entry.Value.index >= 0)
                actual.Add(entry.Key);

        Check(lambda, actual);
    }

    public static void Check(BLambdaExpression lambda, ICollection<BParameterExpression> actual)
    {
        if (undeferrable.TryGetValue(lambda, out _))
            return;

        if (!predicted.TryGetValue(lambda, out var expected))
            return;

        predictedNamesBySpelling.TryGetValue(lambda, out var expectedNames);

        // Known up front so the per-name side effects below — the missed-name samples and the
        // by-spelling counter — are not repeated either. Recording them per relay would make the
        // samples a list of the same few sites over and over.
        var isRepeat = verdicts.TryGetValue(lambda, out var first);

        long missCount = 0;
        long realCount = 0;
        foreach (var capture in actual)
        {
            // A binding with no name, or one whose name is not an identifier the source could
            // have written, is outside a free-name walk's reach by construction.
            if (IsSynthetic(capture))
            {
                Interlocked.Increment(ref syntheticNames);
                continue;
            }

            realCount++;
            if (!isRepeat && expectedNames != null && !expectedNames.Contains(capture.Name!))
                Interlocked.Increment(ref missedByName);

            if (!expected.Contains(capture))
            {
                missCount++;
                if (!isRepeat)
                {
                    lock (missedSamples)
                    {
                        if (missedSamples.Count < 32)
                            missedSamples.Add((lambda.Name.ToString() ?? "?") + "/" + (capture.Name ?? "<null>")
                                + " predicted{" + string.Join("|", expectedNames ?? []) + "}");
                    }
                }
            }
        }

        var overCount = expected.Count - (realCount - missCount);
        Interlocked.Increment(ref checks);

        // A repeat is recognised and NOT added to the totals: the layout is a property of the
        // site, so counting it once per relay would make every aggregate a function of how many
        // times the enclosing lambda happened to be emitted. Whether the repeat AGREES is the
        // interesting half, and it is counted rather than assumed.
        if (isRepeat)
        {
            Interlocked.Increment(ref repeatChecks);
            if (first.Missed != missCount || first.Over != overCount || first.Real != realCount)
                Interlocked.Increment(ref repeatDisagreements);
            return;
        }

        verdicts.AddOrUpdate(lambda, new Verdict
        {
            Missed = (int)missCount,
            Over = (int)overCount,
            Real = (int)realCount,
        });

        Interlocked.Add(ref actualNames, realCount);
        Interlocked.Add(ref missedNames, missCount);
        if (overCount > 0)
            Interlocked.Add(ref overNames, overCount);

        if (missCount > 0)
            Interlocked.Increment(ref missed);
        else if (overCount > 0)
            Interlocked.Increment(ref over);
        else
            Interlocked.Increment(ref exact);
    }

    /// <summary>
    /// Whether a captured binding is one no source identifier names. Deliberately a name test and
    /// deliberately narrow: anything it lets through is charged to the prediction, so the bias is
    /// towards reporting a miss rather than excusing one.
    /// </summary>
    private static bool IsSynthetic(BParameterExpression capture)
    {
        var name = capture.Name;
        if (string.IsNullOrEmpty(name))
            return true;

        return name is "this" or "arguments" or "new.target"
            || name[0] is '<' or '`'
            || name.Contains('`')
            // Script metadata the compiler threads into every function. No source identifier
            // names it, so a free-name walk cannot predict it — and every function captures one,
            // which is why it has to be recognised rather than counted.
            || name.StartsWith("ScriptInfo_", System.StringComparison.Ordinal);
    }

    private static readonly List<string> unresolved = [];
    private static readonly List<string> noCell = [];

    public static IReadOnlyList<string> UnresolvedSamples { get { lock (unresolved) return unresolved.ToArray(); } }

    public static IReadOnlyList<string> NoCellSamples { get { lock (noCell) return noCell.ToArray(); } }

    public static void NoteUnresolved(string name)
    {
        lock (unresolved) { if (unresolved.Count < 32) unresolved.Add(name); }
    }

    public static void NoteNoCell(string name)
    {
        lock (noCell) { if (noCell.Count < 32) noCell.Add(name); }
    }

    public static void Reset()
    {
        Interlocked.Exchange(ref sites, 0);
        Interlocked.Exchange(ref exact, 0);
        Interlocked.Exchange(ref over, 0);
        Interlocked.Exchange(ref missed, 0);
        Interlocked.Exchange(ref predictedNames, 0);
        Interlocked.Exchange(ref actualNames, 0);
        Interlocked.Exchange(ref missedNames, 0);
        Interlocked.Exchange(ref overNames, 0);
        Interlocked.Exchange(ref syntheticNames, 0);
        Interlocked.Exchange(ref missedByName, 0);
        Interlocked.Exchange(ref checks, 0);
        Interlocked.Exchange(ref repeatChecks, 0);
        Interlocked.Exchange(ref repeatDisagreements, 0);
        Interlocked.Exchange(ref excludedSites, 0);
        lock (missedSamples) missedSamples.Clear();
        lock (unresolved) unresolved.Clear();
        lock (noCell) noCell.Clear();
    }
}
