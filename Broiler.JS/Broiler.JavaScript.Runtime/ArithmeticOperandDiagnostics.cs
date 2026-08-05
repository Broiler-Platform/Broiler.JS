using System;
using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Counts what the generic binary arithmetic operators are actually handed at run time, so item
/// 3-1's shared compiler half can be sized before it is built
/// (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// Phase 3's ordering rests on one number — <b>42.01% of the corpus's allocation is number
/// boxing</b> — and on one diagnosis: the boxes are minted by the <em>operators</em>, whose
/// operands arrive boxed from array elements and object fields. What nobody has counted is the
/// half that decides whether a fast path can be fed at all: <b>how often a generic operator's two
/// operands are already Numbers</b>. A native form guarded on that test reaches exactly those
/// invocations and no others.
/// </para>
/// <para>
/// This is the rule item 3-1 itself paid for twice. The bitwise operators were given a native form
/// that is correct on 15 semantics cases, takes its shape from 31.84 bytes an iteration to 0.00,
/// and removes <em>zero</em> boxes on the whole corpus — because the native form needs both
/// operands native and Crypto's digits live in <c>this.array[i]</c>. §3.5: <em>before adding a
/// fast path, count how many of its operands can actually reach it.</em> These counters are that
/// count, taken on the other side of the boundary: not what the compiler could prove, but what the
/// values turn out to be.
/// </para>
/// <para>
/// <see cref="RawDoubleOperand"/> is the sharpest of the four. It counts the <c>AddValue(double)</c>
/// overload, which the compiler emits when it has proved <em>one</em> side a raw double and has to
/// meet a <c>JSValue</c> on the other — the exact shape item 3-5 specialized for <c>&lt;</c> and
/// <c>&gt;</c> and that no arithmetic operator has. Its companion says how often the other side was
/// a Number as well, i.e. how often that specialization would have fired.
/// </para>
/// <para>
/// Off by default, and read as a plain static on a branch that predicts perfectly while it is off
/// — the same bargain <c>NumberBoxingDiagnostics</c> makes one layer up, and on the same path.
/// </para>
/// </remarks>
public static class ArithmeticOperandDiagnostics
{
    private static long generic;
    private static long bothNumbers;
    private static long rawDoubleOperand;
    private static long rawDoubleOtherNumber;
    private static long relational;
    private static long relationalBothNumbers;
    private static long relationalNeitherNumber;

    [ThreadStatic]
    private static bool inRelational;
    private static long unaryNegate;
    private static long unaryUpdate;
    private static long unaryToNumeric;
    private static long unaryToNumericReused;

    /// <summary>Whether operand kinds are counted. Off by default.</summary>
    public static bool Enabled;

    /// <summary>Invocations of a generic two-<c>JSValue</c> arithmetic or bitwise operator.</summary>
    public static long Generic => Interlocked.Read(ref generic);

    /// <summary>Those whose operands were both plain Numbers before any coercion ran.</summary>
    public static long BothNumbers => Interlocked.Read(ref bothNumbers);

    /// <summary>
    /// Invocations where the compiler had already proved one operand a raw <c>double</c> and boxed
    /// nothing to make the call — today only <c>+</c> has such an overload.
    /// </summary>
    public static long RawDoubleOperand => Interlocked.Read(ref rawDoubleOperand);

    /// <summary>Those of <see cref="RawDoubleOperand"/> whose other operand was a Number too.</summary>
    public static long RawDoubleOtherNumber => Interlocked.Read(ref rawDoubleOtherNumber);

    /// <summary>
    /// Invocations of a generic relational operator — <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>,
    /// <c>&gt;=</c> — counted once per source-level comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one operator class nothing in this engine specializes and nothing counted.</b> Item
    /// 3-1's speculation is gated on <c>IsNativeNumericOperator</c>, which is the six arithmetic
    /// operators plus the bitwise ones; the relational operators are excluded, and item 3-5 helps
    /// only when one side is <em>already</em> an unboxed double — its own compile-time counter
    /// (<c>boxedNumericComparisons</c>) records how many sites it could not reach and stops there.
    /// So a comparison over two boxed Numbers is reached by no fast path at all, and until this
    /// counter nobody knew how many of them run.
    /// </para>
    /// <para>
    /// <b>Counted once per source-level comparison, which needs a re-entrancy guard rather than a
    /// case analysis.</b> The relational operators re-dispatch: <c>JSObject</c> coerces and calls
    /// the primitive's, <c>JSNumber</c> unwraps and calls the base's, <c>JSValue</c> hands a
    /// BigInt comparison to the other operand's mirror. Hooking "the ones that answer directly"
    /// means enumerating which paths delegate, which is the analysis that left <c>BitwiseXor</c>
    /// unhooked and silent about it. The flag makes only the outermost entry record, so a path
    /// added later is counted once by construction.
    /// </para>
    /// </remarks>
    public static long Relational => Interlocked.Read(ref relational);

    /// <summary>
    /// Those whose operands were both plain Numbers on entry — what a guarded native comparison
    /// would answer, and the reason the guard is worth pricing.
    /// </summary>
    public static long RelationalBothNumbers => Interlocked.Read(ref relationalBothNumbers);

    /// <summary>Those where NEITHER operand was a Number, the clearest miss.</summary>
    public static long RelationalNeitherNumber => Interlocked.Read(ref relationalNeitherNumber);

    /// <summary>
    /// Opens a counting scope for one relational operator invocation, recording only if no
    /// enclosing invocation is already counting.
    /// </summary>
    internal static RelationalScope Relate(bool leftIsNumber, bool rightIsNumber)
        => new(leftIsNumber, rightIsNumber);

    /// <summary>The outermost-only guard. See <see cref="Relational"/>.</summary>
    internal readonly ref struct RelationalScope
    {
        private readonly bool outermost;

        internal RelationalScope(bool leftIsNumber, bool rightIsNumber)
        {
            outermost = !inRelational;
            if (!outermost)
                return;

            inRelational = true;
            Interlocked.Increment(ref relational);
            if (leftIsNumber && rightIsNumber)
                Interlocked.Increment(ref relationalBothNumbers);
            else if (!leftIsNumber && !rightIsNumber)
                Interlocked.Increment(ref relationalNeitherNumber);
        }

        public void Dispose()
        {
            if (outermost)
                inRelational = false;
        }
    }

    /// <summary>Boxes minted by unary <c>-</c> and <c>~</c>.</summary>
    public static long UnaryNegate => Interlocked.Read(ref unaryNegate);

    /// <summary>Boxes minted by the <c>++</c>/<c>--</c> step itself.</summary>
    public static long UnaryUpdate => Interlocked.Read(ref unaryUpdate);

    /// <summary>
    /// Boxes minted re-coercing the operand of <c>++</c>/<c>--</c>. <c>ToNumeric</c> mints
    /// unconditionally, so a Number that is already a Number is boxed a second time to be handed
    /// back as the old value — <c>x++</c> on a property costs two boxes, not one.
    /// </summary>
    public static long UnaryToNumeric => Interlocked.Read(ref unaryToNumeric);

    /// <summary>
    /// Coercions that handed the operand back instead of copying it, under
    /// <see cref="NumericUpdateReuse"/>. <c>UnaryToNumeric + UnaryToNumericReused</c> is the
    /// coercion count, which is the same on both arms — so the split is a measurement of the
    /// switch rather than of the workload.
    /// </summary>
    public static long UnaryToNumericReused => Interlocked.Read(ref unaryToNumericReused);

    /// <summary>
    /// Records one generic invocation and whether a native form guarded on "both are Numbers"
    /// could have answered it.
    /// </summary>
    internal static void RecordGeneric(bool leftIsNumber, bool rightIsNumber)
    {
        Interlocked.Increment(ref generic);
        if (leftIsNumber && rightIsNumber)
            Interlocked.Increment(ref bothNumbers);
    }

    /// <summary>Records one invocation that already carried an unboxed operand.</summary>
    internal static void RecordRawDoubleOperand(bool otherIsNumber)
    {
        Interlocked.Increment(ref rawDoubleOperand);
        if (otherIsNumber)
            Interlocked.Increment(ref rawDoubleOtherNumber);
    }

    /// <summary>Records a box minted by unary <c>-</c> or <c>~</c>.</summary>
    internal static void RecordUnaryNegate() => Interlocked.Increment(ref unaryNegate);

    /// <summary>Records a box minted by the <c>++</c>/<c>--</c> step.</summary>
    internal static void RecordUnaryUpdate() => Interlocked.Increment(ref unaryUpdate);

    /// <summary>
    /// Where the operand of one <c>++</c>/<c>--</c> step lives, decided by the compiler and read
    /// back per invocation (docs/performance-roadmap.md item 3-1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The step is the largest single source of boxing left on the corpus after the guarded tree
    /// and the <c>ToNumeric</c> reuse — 33.2% of remaining requests — and the item's
    /// re-specification named this count as the thing to take before a typed backing store is
    /// built: <b>if the operand is an element or a field the step shares that mechanism, and if it
    /// is a local the analysis merely failed to type, it is a much smaller change.</b>
    /// </para>
    /// <para>
    /// These are <em>requests</em>, not allocations. The small-integer cache answers a large share
    /// of them for free — the reuse measurement put Crypto at 0.1% real and NavierStokes at 71.4% —
    /// so a row here is a claim about where the traffic is, and it has to be multiplied by the
    /// suite's own request-to-allocation ratio before it is a claim about memory.
    /// </para>
    /// <para>
    /// A numeric local appears in <b>none</b> of these rows, and that is the point rather than a
    /// gap: <c>i++</c> on a raw <c>double</c> local compiles to a native add and never reaches
    /// <c>Increment</c> at all, so this census counts exactly the population that still boxes.
    /// </para>
    /// </remarks>
    public enum UpdateTarget
    {
        /// <summary>A computed member, <c>a[i]++</c>.</summary>
        Element,
        /// <summary>A named member, <c>o.x++</c>.</summary>
        Property,
        /// <summary>A local or captured binding held in a <c>JSVariable</c> cell.</summary>
        LocalCell,
        /// <summary>
        /// A statically-resolved local or parameter held in a plain assignable slot — a name the
        /// numeric analysis did not prove numeric, so it is a <c>JSValue</c> rather than a raw
        /// <c>double</c>.
        /// </summary>
        LocalSlot,
        /// <summary>A global, a <c>with</c> binding, or a deletable <c>eval</c>-introduced <c>var</c>.</summary>
        GlobalOrWith,
        /// <summary>Anything else that reaches the shared member-update tail.</summary>
        Other,
    }

    private static readonly long[] updateTargets = new long[6];

    /// <summary>
    /// Where each <c>++</c>/<c>--</c> step's operand lived. Sums to <see cref="UnaryUpdate"/>,
    /// which is the invariant that says the census covers the operator rather than a subset of it.
    /// </summary>
    public static long[] UpdateTargets
    {
        get
        {
            var copy = new long[updateTargets.Length];
            for (var i = 0; i < copy.Length; i++)
                copy[i] = Interlocked.Read(ref updateTargets[i]);
            return copy;
        }
    }

    /// <summary>Records where one <c>++</c>/<c>--</c> step's operand lived.</summary>
    internal static void RecordUpdateTarget(UpdateTarget target)
        => Interlocked.Increment(ref updateTargets[(int)target]);

    /// <summary>Records a box minted coercing the operand of <c>++</c>/<c>--</c>.</summary>
    internal static void RecordUnaryToNumeric() => Interlocked.Increment(ref unaryToNumeric);

    /// <summary>Records a coercion that reused an already-Number operand instead of copying it.</summary>
    internal static void RecordUnaryToNumericReused() => Interlocked.Increment(ref unaryToNumericReused);

    public static void Reset()
    {
        Interlocked.Exchange(ref generic, 0);
        Interlocked.Exchange(ref bothNumbers, 0);
        Interlocked.Exchange(ref rawDoubleOperand, 0);
        Interlocked.Exchange(ref rawDoubleOtherNumber, 0);
        Interlocked.Exchange(ref relational, 0);
        Interlocked.Exchange(ref relationalBothNumbers, 0);
        Interlocked.Exchange(ref relationalNeitherNumber, 0);
        inRelational = false;
        Interlocked.Exchange(ref unaryNegate, 0);
        Interlocked.Exchange(ref unaryUpdate, 0);
        Interlocked.Exchange(ref unaryToNumeric, 0);
        Interlocked.Exchange(ref unaryToNumericReused, 0);
        for (var i = 0; i < updateTargets.Length; i++)
            Interlocked.Exchange(ref updateTargets[i], 0);
    }
}
