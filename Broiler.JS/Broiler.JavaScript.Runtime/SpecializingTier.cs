using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// The two runtime halves of a specialized property read: the guard that decides whether the
/// speculation holds, and the slot load taken when it does (docs/performance-roadmap.md item
/// 4-2b).
/// </summary>
/// <remarks>
/// <para>
/// This is what a monomorphic read compiles into instead of
/// <see cref="PropertyInlineCacheSite.Get"/>. The cache's own monomorphic hit already ends in a
/// shape-id compare and a slot load — but it gets there through a static call taking a
/// <c>KeyString</c>, a bounds test, a side-table read, a megamorphic flag, a receiver type test,
/// a key compare, an entry loop and a holder test, and it re-reads the shape id and slot out of
/// a cache entry rather than having them as constants. Specializing removes all of that: the
/// shape id and the slot are literals in the emitted code, so what is left is a type test, two
/// integer compares and an array load.
/// </para>
/// <para>
/// <b>Split into a guard and a read</b> because the two go into different arms of one
/// conditional. Nothing runs between the test and the consequent of a branch, so the read needs
/// no re-validation; the slot load happening twice is a second hit on a line the guard has just
/// touched, and it is what buys the guard being expressible as a plain <c>bool</c> that
/// <see cref="LinqExpressions"/>' speculation builder can use unchanged.
/// </para>
/// <para>
/// <b>The key compare is not redundant.</b> The tier-2 compile addresses a tier-1 site by its
/// ORDINAL position in the function's emission, and the site counter is process-wide — two
/// threads compiling at once is enough to slide the mapping. Comparing the key the
/// specialization was built for against the key actually being read turns any such slip into a
/// failed guard, which poisons the site and falls back. So the mapping is a performance
/// heuristic and never a correctness dependency, which is the property that makes an ordinal
/// mapping acceptable at all.
/// </para>
/// </remarks>
public static class SpecializedPropertyRead
{
    /// <summary>
    /// Whether <paramref name="target"/> is an object of exactly <paramref name="shapeId"/> whose
    /// <paramref name="slot"/> holds a value, and whose read is for <paramref name="expectedKey"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="key"/> is taken by value rather than <c>in</c>: it is one <c>uint</c>, so
    /// the copy is free, and a by-ref parameter would need the emitted call to have an address to
    /// hand over — which a key that is a static field read does not obviously have.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Matches(JSValue target, KeyString key, uint expectedKey, int shapeId, int slot)
        => key.Key == expectedKey
            && target is JSObject shaped
            && shaped.HasShapeSlotValue(shapeId, slot);

    /// <summary>Loads the slot <see cref="Matches"/> has just validated.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue Read(JSValue target, int slot)
        => ((JSObject)target).ReadShapeSlotUnchecked(slot);
}

/// <summary>
/// The tier-2 compile's plan: which tier-1 read sites the recompile is re-emitting, so it can ask
/// item 4-1's feedback what each of them saw (item 4-2b).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a plan is needed at all.</b> A tier-2 recompile re-parses the function's source, and
/// every property read it emits allocates a <em>fresh</em> inline-cache site. So before this
/// existed, promoting a function silently threw away every warm cache it had and started over
/// cold — and, more to the point, there was no way to ask what the ORIGINAL sites had seen,
/// because their indices were nowhere. Tier-1 records the half-open range of read sites its body
/// compile allocated; tier-2 hands those same indices back out, in order.
/// </para>
/// <para>
/// <b>A plain static under a lock, not a thread-static.</b> A compilation does not stay on the
/// thread that asked for it — <c>CompilationStack</c> moves anything over 512 characters onto a
/// sized worker — so a thread-local would be invisible exactly when the function is big enough to
/// be worth promoting. The lock makes concurrent tier-2 recompiles serialize with each other; it
/// is not taken by ordinary compilation, and promotions are rare (under a hundred per Octane
/// suite), so what it serializes is measured in the tens.
/// </para>
/// <para>
/// <b>Exhaustion is the stopping rule.</b> The plan hands out indices until the recorded range
/// runs out and then falls back to fresh allocation, which is what keeps a nested or unrelated
/// compilation triggered from inside the recompile from consuming the range. Combined with the
/// key compare in <see cref="SpecializedPropertyRead.Matches"/>, an exhausted or misaligned plan
/// can only cost speed.
/// </para>
/// </remarks>
public static class SpecializingTier
{
    private static readonly object gate = new();

    private static Plan active;

    /// <summary>One promoted function's site range, consumed in emission order.</summary>
    private sealed class Plan(int start, int end, bool specialize)
    {
        private int next = start;

        public bool Specialize { get; } = specialize;

        public int Take() => next < end ? next++ : -1;
    }

    /// <summary>
    /// Installs the plan for the duration of one tier-2 compilation. Serializes against other
    /// tier-2 compilations; ordinary compilation never takes the lock.
    /// </summary>
    /// <param name="specialize">
    /// Whether monomorphic reads may be specialized. Separate from whether feedback happens to be
    /// recording, so the two are independently controllable — which is what lets the cost of
    /// <em>collecting</em> feedback be measured apart from the effect of <em>consuming</em> it.
    /// Without that separation the two arms of any measurement differ in two things.
    /// </param>
    public static IDisposable Recompiling(int firstReadSite, int endReadSite, bool specialize)
    {
        Monitor.Enter(gate);
        Volatile.Write(
            ref active,
            endReadSite > firstReadSite ? new Plan(firstReadSite, endReadSite, specialize) : null);
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            Volatile.Write(ref active, null);
            Monitor.Exit(gate);
        }
    }

    /// <summary>
    /// The site index the next emitted property read should use: the corresponding tier-1 site
    /// while a plan is active and has one left, otherwise a freshly allocated one.
    /// </summary>
    public static int NextReadSite()
    {
        var plan = Volatile.Read(ref active);
        if (plan != null)
        {
            var replayed = plan.Take();
            if (replayed >= 0)
                return replayed;
        }

        return PropertyInlineCacheSite.Allocate();
    }

    /// <summary>Whether a tier-2 compilation is in progress and may specialize.</summary>
    public static bool MaySpecialize => Volatile.Read(ref active)?.Specialize == true;
}
