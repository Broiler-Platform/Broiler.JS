using System;
using System.Threading;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

/// <summary>
/// Switch and counters for skipping a relay-time closure rewrite that an enclosing walk has
/// already performed.
/// </summary>
/// <remarks>
/// <para>
/// The switch exists for the reason every other one in this campaign does: the change has to be
/// measurable against a build that differs in nothing else. Setting
/// <c>BROILER_JS_RELAY_REWRITE_ONCE=0</c> restores the unconditional rewrite.
/// </para>
/// <para>
/// The counters are touched once per relayed site, never per call, so they are off every hot
/// path by construction and need no enable flag of their own. <c>Rewrote</c> is the population
/// the skip cannot reach — a lambda no descending walk had entered — and reading it is how the
/// claim "the relay-time walk is a repeat" is checked rather than asserted.
/// </para>
/// <para>
/// It lives with <c>LambdaRewriter</c>, which is the walk it counts, rather than beside the
/// emitting builder it used to share a file with. The two assemblies that report into it are on
/// opposite sides of the model/emitter split, which is why the reporting methods below are public
/// where they used to be internal — an <c>InternalsVisibleTo</c> would compile while preserving
/// exactly the coupling the split removes. They are engine-internal by convention: a host reads
/// the counters and sets the switch, and has no reason to call them.
/// </para>
/// </remarks>
public static class ClosureRewriteDiagnostics
{
    public const string EnvironmentVariable = "BROILER_JS_RELAY_REWRITE_ONCE";

    private static int skipRewritten = ReadConfigured();

    private static long rewrote;
    private static long skipped;
    private static long capturesInRepeat;

    [ThreadStatic]
    private static bool inRepeatWalk;

    /// <summary>Whether a relay skips a rewrite an enclosing walk already did.</summary>
    public static bool SkipRewrittenRelays
    {
        get => Volatile.Read(ref skipRewritten) != 0;
        set => Volatile.Write(ref skipRewritten, value ? 1 : 0);
    }

    /// <summary>Relayed sites whose subtree this rewrote.</summary>
    public static long RewroteRelays => Interlocked.Read(ref rewrote);

    /// <summary>Relayed sites whose subtree an enclosing walk had already rewritten.</summary>
    public static long SkippedRelays => Interlocked.Read(ref skipped);

    /// <summary>
    /// Captures a relay-time rewrite of an already-rewritten lambda created. Only ever non-zero
    /// with <see cref="SkipRewrittenRelays"/> off, since that is the only arm that runs one.
    /// </summary>
    public static long CapturesCreatedInRepeatWalk => Interlocked.Read(ref capturesInRepeat);

    public static void Rewrote() => Interlocked.Increment(ref rewrote);

    public static void Skipped() => Interlocked.Increment(ref skipped);

    public static void BeginRepeatWalk() => inRepeatWalk = true;

    public static void EndRepeatWalk() => inRepeatWalk = false;

    public static void CaptureCreated()
    {
        if (inRepeatWalk)
            Interlocked.Increment(ref capturesInRepeat);
    }

    public static void Reset()
    {
        Interlocked.Exchange(ref rewrote, 0);
        Interlocked.Exchange(ref skipped, 0);
        Interlocked.Exchange(ref capturesInRepeat, 0);
    }

    private static int ReadConfigured()
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "0",
            StringComparison.Ordinal)
            ? 0
            : 1;
}
