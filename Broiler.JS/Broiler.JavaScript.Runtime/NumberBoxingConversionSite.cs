namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Which emission site minted a compiler boxing conversion — a raw <c>double</c> crossing into a
/// <see cref="JSValue"/> (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// <strong>The census this splits could name the category and not the producer.</strong>
/// §4.2a widened phase 3's corpus from seven suites to twelve and found conversions going
/// 24.6 M → 69.3 M, with <em>Gameboy alone minting 26.9 M at 51.0% of its own requests</em> — more
/// than all seven previously-measured suites together, on a <c>Uint8Array</c> memory image, which
/// is the shape item 3-1's typed backing store was written for. On that evidence 3-1's storage half
/// was re-opened as <em>unmeasured</em> rather than refuted. But the counter that produced the
/// finding sits in the boxing factory, so it can say a conversion happened and not which of the
/// compiler's emission sites emitted it — and a typed backing store only reaches conversions minted
/// by an element read or an element write.
/// </para>
/// <para>
/// So each of the compiler's boxing sites declares itself, and the census reports the split per
/// suite. That is what turns *"Gameboy mints 26.9 M conversions"* into a claim about whether a
/// typed store would remove any of them.
/// </para>
/// <para>
/// <strong>The site travels as a constant argument, on the same single code path the engine
/// ships.</strong> A separate factory entry per site — the shape <c>CreateLiteral</c> and
/// <c>CreateConversion</c> already use — would multiply into nine, and the alternative of gating
/// the argument behind the counter flag would leave the measured arm and the shipping arm running
/// different code. One <c>ldc.i4</c> at each site is cheaper than either, and it keeps the
/// counters-off arm exactly what it was.
/// </para>
/// </remarks>
public enum NumberBoxingConversionSite
{
    /// <summary>A conversion whose site has not been attributed.</summary>
    Unclassified = 0,

    /// <summary>
    /// Reading a scalar-replaced numeric local. The local lives in a raw <c>double</c>, so every
    /// consumer wanting a <see cref="JSValue"/> boxes it at the read
    /// (<c>FastFunctionScope.DeclareVariable</c>).
    /// </summary>
    NumericLocalRead = 1,

    /// <summary>
    /// The root of item 3-1's guarded arithmetic tree — the one box a specialized tree is
    /// supposed to keep, its whole point being that the interior nodes no longer mint any —
    /// whose CONSUMER the compiler could not attribute. The residual of the five
    /// <c>GuardedTreeRootInto*</c> sites below, and deliberately visible rather than folded away:
    /// an attribution that silently defaulted would read as a finding about the corpus when it is
    /// a finding about the instrument.
    /// </summary>
    GuardedTreeRoot = 2,

    /// <summary>
    /// An operand inside a guarded tree that has to be handed to the generic arm as a
    /// <see cref="JSValue"/>, so the tree pays for a leaf it could not keep native.
    /// </summary>
    GuardedTreeOperand = 3,

    /// <summary>The result of a unary <c>+</c> or <c>-</c> computed natively.</summary>
    UnaryOperator = 4,

    /// <summary>The result of a binary arithmetic or bitwise operator computed natively.</summary>
    BinaryOperator = 5,

    /// <summary>
    /// The value of an assignment to a numeric local, boxed because the assignment is in
    /// expression position and its value is consumed.
    /// </summary>
    AssignmentResult = 6,

    /// <summary>
    /// The step or the result of <c>++</c>/<c>--</c> on a numeric local — the operator item 3-8's
    /// re-opening measured at 30.9% of the corpus's boxing.
    /// </summary>
    UpdateStep = 7,

    /// <summary>
    /// A compile-time numeric constant in an argument or element list, boxed once per evaluation
    /// of the list.
    /// </summary>
    ConstantOperand = 8,

    // ── the guarded tree's root, split by what consumes the box ──────────────────────────
    //
    // `0103` counted the root at 61.79% of the corpus's conversions and left the phase with one
    // question: the root is boxed because its CONSUMER takes a JSValue, so the only way to remove
    // it is a consumer that can take the raw double the tree already has in hand. Which consumers
    // those are is a fact about the corpus that no run-time counter can produce — the box is
    // minted at the tree, and what receives it is known only to the compiler. So the consumer is
    // attached where the compiler knows it, and travels to the tree with the node being visited.

    /// <summary>
    /// The root box is stored into an ELEMENT — <c>a[i] = &lt;tree&gt;</c>. The population a typed
    /// backing store reaches, and the one item 3-1 was originally written around.
    /// </summary>
    GuardedTreeRootIntoElement = 9,

    /// <summary>
    /// The root box is stored into a named PROPERTY — <c>o.x = &lt;tree&gt;</c>. Item 3-2's
    /// population: an unboxed shape slot, not an unboxed array.
    /// </summary>
    GuardedTreeRootIntoProperty = 10,

    /// <summary>
    /// The root box is stored into a LOCAL or a declared binding. Reachable without touching any
    /// storage representation at all, because a proven-numeric local already has a raw
    /// <c>double</c> home (item 3-3) — so a root landing here is one the existing numeric tier
    /// failed to type, not one that needs a new representation.
    /// </summary>
    GuardedTreeRootIntoLocal = 11,

    /// <summary>The root box is a function's RETURN value.</summary>
    GuardedTreeRootIntoReturn = 12,

    /// <summary>
    /// The root box is a call ARGUMENT. Item 4-5 priced these at 32 bytes each on the call path
    /// and attributed them to the caller; this says how many of them an arithmetic tree mints.
    /// </summary>
    GuardedTreeRootIntoArgument = 13,
}
