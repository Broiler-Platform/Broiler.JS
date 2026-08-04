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
        Interlocked.Exchange(ref unaryNegate, 0);
        Interlocked.Exchange(ref unaryUpdate, 0);
        Interlocked.Exchange(ref unaryToNumeric, 0);
        Interlocked.Exchange(ref unaryToNumericReused, 0);
    }
}
