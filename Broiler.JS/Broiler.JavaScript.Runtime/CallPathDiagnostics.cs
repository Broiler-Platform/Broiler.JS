using System;
using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>Totals for the JavaScript call paths.</summary>
/// <param name="Calls">Every invocation, on either path.</param>
/// <param name="CallbackCalls">
/// Those taken by a native callback site (<c>Array.prototype.forEach</c> and friends), which use a
/// deliberately shorter entry than an emitted JavaScript call.
/// </param>
/// <param name="UserCalls">Those whose callee is a function compiled from JavaScript source.</param>
/// <param name="UserCallsFromPromoted">
/// Those whose callee is a JavaScript function <em>and</em> whose caller is a promoted function —
/// item 4-4's addressable surface, since inlining needs both a body to inline and a call site
/// inside a tier-2 recompile to inline it at.
/// </param>
/// <param name="StrictTransitions">
/// Entries to a call whose callee's strictness differs from the currently executing code's, each
/// of which writes the strict-mode <c>AsyncLocal</c> twice — once on entry and once on exit.
/// </param>
public readonly record struct CallPathSnapshot(
    long Calls,
    long CallbackCalls,
    long UserCalls,
    long UserCallsFromPromoted,
    long StrictTransitions);

/// <summary>
/// How many JavaScript calls actually happen, and how many of them are made from a function the
/// tiering controller has promoted (docs/performance-roadmap.md item 4-4).
/// </summary>
/// <remarks>
/// <para>
/// Item 4-1 counts calls too, and this deliberately does not reuse that count. 4-1's call feedback
/// is emitted at <em>compile</em> time and only at sites the compiler instruments, which is the
/// right design for feedback and the wrong basis for a denominator: a share computed against it
/// would silently be a share of the instrumented calls rather than of the calls. This counts at
/// <c>JSFunction.InvokeFunction</c>, which every JavaScript call goes through whatever emitted it,
/// so the two can be compared and any gap between them is itself information.
/// </para>
/// <para>
/// <b>Why the second number.</b> Inlining can only be emitted where the compiler has an
/// observation to speculate on, and item 4-3b established that this means inside a tier-2
/// recompile — a tier-1 method is compiled before anything has run. So the calls made <em>from</em>
/// promoted functions are the entire surface item 4-4 could ever address, exactly as the reads
/// inside promoted functions were 4-2b's. The caller is read from
/// <see cref="JSEngine.ExecutingFunction"/>, which the call prologue already maintains, so this
/// asks a question the engine was already answering.
/// </para>
/// <para>
/// Off by default and behind one static branch, on the same terms as
/// <see cref="PropertyOptimizationDiagnostics"/>: it is an interlocked increment on the hottest
/// path in the engine, so a timing run must not have it on. Counts and wall clock come from
/// separate passes.
/// </para>
/// </remarks>
public static class CallPathDiagnostics
{
    /// <summary>Whether call totals are recorded. Defaults to <c>false</c>.</summary>
    public static bool Enabled;

    private static long calls;
    private static long callbackCalls;
    private static long userCalls;
    private static long userCallsFromPromoted;
    private static long strictTransitions;

    /// <summary>
    /// Records one invocation through the emitted-call entry.
    /// </summary>
    /// <param name="calleeIsUserFunction">
    /// Whether the callee has a JavaScript body. A call to a native builtin has an emitted call
    /// site and no body to inline, so counting the two together would inflate item 4-4's surface
    /// with calls it could never address.
    /// </param>
    /// <param name="fromPromoted">Whether the calling function has been promoted.</param>
    public static void RecordCall(bool calleeIsUserFunction, bool fromPromoted)
    {
        Interlocked.Increment(ref calls);
        if (!calleeIsUserFunction)
            return;

        Interlocked.Increment(ref userCalls);
        if (fromPromoted)
            Interlocked.Increment(ref userCallsFromPromoted);
    }

    /// <summary>
    /// Records one invocation through the native callback entry, which a builtin uses to run a
    /// JavaScript callback.
    /// </summary>
    /// <remarks>
    /// Counted separately, and it is not an accounting detail: this entry does markedly less than
    /// the emitted-call one — one <c>using</c> scope against five, and none of the executing-
    /// function or legacy-caller bookkeeping — so the two are not the same operation and must not
    /// be averaged. It is also unreachable for inlining: there is no emitted call site.
    /// </remarks>
    public static void RecordCallbackCall()
    {
        Interlocked.Increment(ref calls);
        Interlocked.Increment(ref callbackCalls);
    }

    /// <summary>
    /// Records one call that crosses a strictness boundary, and therefore writes the strict-mode
    /// <c>AsyncLocal</c> on the way in and again on the way out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Item 4-5 fixed the READ side and left the write side unmeasured, on the strength of an
    /// argument about how often a transition happens.</b> <c>StrictModeScope</c>'s own comment says
    /// *"the write only on a transition, so … the common case is now a ThreadStatic read and a
    /// compare, with no AsyncLocal touched at all"*, which is true of a uniformly strict or
    /// uniformly sloppy call graph and false at every boundary between them — where the cost is
    /// **95.70 ns and 224 bytes per call**, measured, most of a whole call.
    /// </para>
    /// <para>
    /// Whether that matters is a question about the corpus rather than about the mechanism, and
    /// nothing could answer it. This counts the crossings so it can be.
    /// </para>
    /// </remarks>
    public static void RecordStrictTransition() => Interlocked.Increment(ref strictTransitions);

    public static CallPathSnapshot Snapshot()
        => new(
            Interlocked.Read(ref calls),
            Interlocked.Read(ref callbackCalls),
            Interlocked.Read(ref userCalls),
            Interlocked.Read(ref userCallsFromPromoted),
            Interlocked.Read(ref strictTransitions));

    public static void Reset()
    {
        Interlocked.Exchange(ref calls, 0);
        Interlocked.Exchange(ref callbackCalls, 0);
        Interlocked.Exchange(ref userCalls, 0);
        Interlocked.Exchange(ref userCallsFromPromoted, 0);
        Interlocked.Exchange(ref strictTransitions, 0);
    }
}
