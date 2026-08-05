using System.Threading;

namespace Broiler.JavaScript.Compiler;

public readonly record struct CompilerSpecializationSnapshot(
    long ScalarLocals,
    long NumericLocals,
    long MixedNumericComparisons,
    long BoxedNumericComparisons,
    long HoistedNames,
    long[] NumericRejections,
    long NumericCandidatesOffered,
    long NumericCandidatesRejected,
    long NumericCandidatesDropped,
    long NumericCandidatesSurviving,
    long[] NumericDropCauses,
    long SpeculativeNumericTrees,
    long SpeculativeNumericGuards,
    long[] NumericTreeRefusals,
    long[] NumericTreeOrderBlockers,
    long SpeculativeNumericCandidates,
    long SpeculativeNumericLocalsEmitted,
    long ImportedOuterNumericCandidates,
    long ImportedOuterNumericOffers,
    long[] DeferralRefusals,
    long DeferralFreeNames,
    long DeferralBoundFreeNames,
    long DeferralFunctionOwnedFreeNames,
    long DeferralCellBackedFreeNames);

/// <summary>
/// Why a hoisted name did not become a raw <c>double</c> local, in the order the gate asks
/// (docs/performance-roadmap.md item 3-6).
/// </summary>
/// <remarks>
/// A waterfall: a name is attributed to the FIRST conjunct it fails, which is what makes the
/// counts actionable — "widen this one and that many names become eligible" — rather than a set
/// of overlapping tallies that cannot be added up. The cost is that removing the top reason only
/// promotes the names that also pass everything below it, so the numbers are an upper bound on
/// each conjunct taken alone.
/// </remarks>
public enum NumericLocalRejection
{
    /// <summary>Became a numeric local. Not a rejection; the head of the waterfall.</summary>
    Accepted,
    /// <summary>Hoisted into a direct <c>eval</c>'s own variable environment.</summary>
    DirectEvalRoot,
    /// <summary>The whole function is not scalar-replaceable: async, generator, `eval`, `with`, `debugger`, or a nested function that can reach names it does not mention.</summary>
    FunctionNotScalarReplaceable,
    /// <summary>Program or module top level rather than a function body.</summary>
    NotInAFunction,
    /// <summary>The name is <c>arguments</c> or <c>eval</c>.</summary>
    ArgumentsOrEval,
    /// <summary>A nested function mentions the name, so it needs a cell to capture.</summary>
    CapturedByNestedFunction,
    /// <summary>
    /// A function declaration at body top level mentions the name. Its function object exists
    /// from function entry, so it can read the binding before the initializer runs even though
    /// its text is after the declaration — the one place a raw double hoisted to 0 would be
    /// observed where <c>undefined</c> belongs (docs/performance-roadmap.md item 3-7).
    /// </summary>
    CapturedByHoistedFunction,
    /// <summary>A <c>let</c>/<c>const</c> in a nested block, which is a distinct binding per entry.</summary>
    LexicalOutsideFunctionBody,
    /// <summary>The analysis could not prove the name only ever holds a number.</summary>
    NotProvenNumeric,
}

/// <summary>
/// What defeated the numeric proof for one local the optimistic fixed point had to drop
/// (docs/performance-roadmap.md item 3-8).
/// </summary>
/// <remarks>
/// <para>
/// Item 3-6 counted <em>how many</em> candidates the fixed point drops (1 916 of 2 521 offered on
/// the Octane corpus) and read them all as one population — "dropped for want of a type, not for
/// want of a rule" — which is the sentence item 3-8 was specified from. They are not one
/// population, and the difference decides what a runtime guard would have to be: a value arriving
/// from a property read wants a guard at the read, a parameter wants one at entry, and a name
/// dropped because <em>another dropped candidate</em> appears in its right-hand side wants
/// nothing at all — fixing its root fixes it for free.
/// </para>
/// <para>
/// A drop is attributed to the FIRST leaf of the assigned expression that the analysis will not
/// type, so `s = a.x * 2 + 1` is a property read rather than an operator. First drop wins: the
/// fixed point can revisit a name, and the cause that mattered is the one that removed it.
/// </para>
/// </remarks>
public enum NumericDropCause
{
    /// <summary>A name that was itself offered as a numeric candidate and has been dropped — a cascade.</summary>
    DroppedCandidate,
    /// <summary>A parameter of the enclosing function: the caller picks the type.</summary>
    Parameter,
    /// <summary>A named property read, `o.x`.</summary>
    PropertyRead,
    /// <summary>A computed element read, `a[i]`.</summary>
    ElementRead,
    /// <summary>The return value of a call or a `new`.</summary>
    CallResult,
    /// <summary>Any other name — a global, an outer-scope binding, a catch parameter, a function.</summary>
    OtherName,
    /// <summary>A literal that is not a number, or an object / array / function / template literal.</summary>
    NonNumericLiteral,
    /// <summary>An operator whose result the analysis does not type (`typeof`, `&amp;&amp;`, `instanceof`, …).</summary>
    UnhandledOperator,
    /// <summary>Anything else.</summary>
    Other,
}

/// <summary>
/// Why an arithmetic tree did not take item 3-1's guarded form, in the order the gate asks
/// (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// A waterfall on the same terms as <see cref="NumericLocalRejection"/>: a candidate is attributed
/// to the FIRST condition it fails, so the counts add up and each one reads as "widen this and
/// that many sites become eligible". Only a binary node whose operator is one the native forms
/// cover is counted at all — everything else is not a candidate, and counting it would put the
/// whole program in the denominator.
/// </para>
/// <para>
/// A refused root offers its children in turn, because <c>VisitBinaryExpression</c> falls through
/// to visiting the operands. So a refused three-node tree can contribute a refusal AND a
/// specialization, and the counts are of <em>candidate nodes</em> rather than of source
/// expressions. That is the useful denominator here: the question is how much arithmetic reaches
/// the guarded form, not how many statements a programmer wrote.
/// </para>
/// </remarks>
public enum NumericTreeRefusal
{
    /// <summary>Took the guarded form. Not a refusal; the head of the waterfall.</summary>
    Specialized,
    /// <summary>Every operand was already provably native, so the unguarded path has it.</summary>
    AlreadyNative,
    /// <summary>Inside a <c>with</c> block or a direct <c>eval</c>'s shadow.</summary>
    WithOrEvalShadow,
    /// <summary>No intermediate and no already-native leaf, so the guard would buy a type test and nothing else.</summary>
    NoSavingToMake,
    /// <summary>A leaf evaluated after the first coercion could observe or cause an effect.</summary>
    OrderUnsafe,
    /// <summary>More leaves than the type-test chain is worth.</summary>
    TooManyLeaves,
    /// <summary>A leaf the compiler already knows is a String, which makes the guard unsatisfiable.</summary>
    StringLeaf,
    /// <summary>Every leaf was already native — the all-native path, reached by a different route.</summary>
    NothingToGuard,
    /// <summary>An operator with no native form after all, so neither arm could be built.</summary>
    Unbuildable,
}

/// <summary>
/// What kind of leaf defeated <see cref="NumericTreeRefusal.OrderUnsafe"/> — the sub-census that
/// says whether widening that conjunct is a matter of admitting one more leaf kind or of
/// preserving evaluation order in general.
/// </summary>
public enum NumericTreeOrderBlocker
{
    /// <summary>A computed element read, `a[i]`.</summary>
    ElementRead,
    /// <summary>A named property read, `o.x`.</summary>
    PropertyRead,
    /// <summary>A call or a `new`.</summary>
    CallResult,
    /// <summary>An identifier that is not a proven-numeric local — a global, an outer binding, a parameter.</summary>
    OtherName,
    /// <summary>Anything else: a nested assignment, a conditional, a template, a non-numeric literal.</summary>
    Other,
}

/// <summary>Compilation counters used by Phase 3 tests and benchmark reports.</summary>
public static class CompilerSpecializationDiagnostics
{
    private static long scalarLocals;
    private static long numericLocals;
    private static long mixedNumericComparisons;
    private static long boxedNumericComparisons;
    private static long hoistedNames;
    private static readonly long[] numericRejections = new long[9];
    private static long numericCandidatesOffered;
    private static long numericCandidatesRejected;
    private static long numericCandidatesDropped;
    private static long numericCandidatesSurviving;
    private static readonly long[] numericDropCauses = new long[9];
    private static long speculativeNumericTrees;
    private static long speculativeNumericGuards;
    private static readonly long[] numericTreeRefusals = new long[9];
    private static readonly long[] numericTreeOrderBlockers = new long[5];
    private static long speculativeNumericCandidates;
    private static long speculativeNumericLocalsEmitted;
    private static long importedOuterNumericCandidates;
    private static long importedOuterNumericOffers;
    private static readonly long[] deferralRefusals = new long[3];
    private static long deferralFreeNames;
    private static long deferralBoundFreeNames;
    private static long deferralFunctionOwnedFreeNames;
    private static long deferralCellBackedFreeNames;

    internal static void RecordScalarLocal() => Interlocked.Increment(ref scalarLocals);

    /// <summary>A local held as a raw CLR <c>double</c> (docs/performance-roadmap.md P2-2 item 3).</summary>
    internal static void RecordNumericLocal() => Interlocked.Increment(ref numericLocals);

    /// <summary>
    /// A <c>&lt;</c> or <c>&gt;</c> emitted with one operand an unboxed double and the other a
    /// <c>JSValue</c>, taking item 3-5's unbox-the-other-side form.
    /// </summary>
    internal static void RecordMixedNumericComparison() => Interlocked.Increment(ref mixedNumericComparisons);

    /// <summary>
    /// A relational comparison that still boxes, because neither operand is an unboxed double —
    /// the denominator for the one above, and what says whether item 3-5's shape is common or rare.
    /// </summary>
    internal static void RecordBoxedNumericComparison() => Interlocked.Increment(ref boxedNumericComparisons);

    /// <summary>How many names the analysis offered as numeric candidates before resolving.</summary>
    internal static void RecordNumericCandidatesOffered(int count)
        => Interlocked.Add(ref numericCandidatesOffered, count);

    /// <summary>
    /// How many of those a rejection path removed before the fixed point ever ran — the step
    /// between offered and dropped that had no counter (docs/performance-roadmap.md item 3-7).
    /// </summary>
    internal static void RecordNumericCandidatesRejected(int count)
        => Interlocked.Add(ref numericCandidatesRejected, count);

    /// <summary>How many of those the optimistic fixed point then had to drop.</summary>
    internal static void RecordNumericCandidatesDropped(int count)
        => Interlocked.Add(ref numericCandidatesDropped, count);

    /// <summary>
    /// How many names came out of the analysis proved numeric. Counted rather than inferred:
    /// offered = rejected + dropped + surviving.
    /// </summary>
    internal static void RecordNumericCandidatesSurviving(int count)
        => Interlocked.Add(ref numericCandidatesSurviving, count);

    /// <summary>Records what defeated the numeric proof for one dropped candidate (item 3-8).</summary>
    internal static void RecordNumericDropCause(NumericDropCause cause)
        => Interlocked.Increment(ref numericDropCauses[(int)cause]);

    /// <summary>
    /// One arithmetic tree emitted in item 3-1's guarded form, and how many of its leaves the
    /// guard has to test — the second number is what says whether the sites reached are the
    /// two-leaf kind or long chains.
    /// </summary>
    internal static void RecordSpeculativeNumericTree(int guardedLeaves)
    {
        Interlocked.Increment(ref speculativeNumericTrees);
        Interlocked.Add(ref speculativeNumericGuards, guardedLeaves);
    }

    /// <summary>
    /// Records why one candidate arithmetic tree did or did not take the guarded form (item 3-1).
    /// </summary>
    internal static void RecordNumericTreeDecision(NumericTreeRefusal reason)
        => Interlocked.Increment(ref numericTreeRefusals[(int)reason]);

    /// <summary>
    /// Records the kind of leaf that made a candidate order-unsafe — read only against the
    /// <see cref="NumericTreeRefusal.OrderUnsafe"/> row, which is its total.
    /// </summary>
    internal static void RecordNumericTreeOrderBlocker(NumericTreeOrderBlocker blocker)
        => Interlocked.Increment(ref numericTreeOrderBlockers[(int)blocker]);

    /// <summary>
    /// Names item 3-8a could speculate on: proven numeric only when a name from outside the
    /// function is assumed to hold a number. Recorded per compiled function, so a program with
    /// one function reports that function's population exactly — which is how it is tested,
    /// since the drop-cause counters beside it aggregate over every function in the program and
    /// that is what made the first reading of this number unreadable.
    /// </summary>
    /// <summary>One local emitted in item 3-8a's dual representation.</summary>
    internal static void RecordSpeculativeNumericLocal()
        => Interlocked.Increment(ref speculativeNumericLocalsEmitted);

    internal static void RecordSpeculativeNumericCandidates(int count)
        => Interlocked.Add(ref speculativeNumericCandidates, count);

    /// <summary>
    /// Locals item 3-9 would type by importing an enclosing function's proven-numeric conclusion
    /// (docs/performance-roadmap.md item 3-9).
    /// </summary>
    /// <remarks>
    /// <b>Bounded above by <c>SpeculativeNumericCandidates</c> by construction</b>, which is what
    /// makes it checkable rather than merely reported: everything an enclosing scope has PROVED
    /// numeric is also something 3-8a's pass would have ASSUMED numeric, so a reading above 26 on
    /// the Octane corpus is a defect in this counter and not a discovery.
    /// </remarks>
    internal static void RecordImportedOuterNumericCandidates(int count)
        => Interlocked.Add(ref importedOuterNumericCandidates, count);

    /// <summary>
    /// Times the enclosing scope chain answered "yes, that name is already a raw <c>double</c>"
    /// while item 3-9's pass was resolving a function (docs/performance-roadmap.md item 3-9).
    /// </summary>
    /// <remarks>
    /// <b>This is the counter that makes a zero population readable.</b> A zero candidate count on
    /// its own is consistent with two very different worlds — nested functions never read an
    /// enclosing numeric local at all, or they read them constantly and never in a position that
    /// could be typed — and the follow-up work differs completely between them. Occurrence-weighted
    /// rather than distinct, because the only question it has to answer is whether the number is
    /// zero.
    /// </remarks>
    internal static void RecordImportedOuterNumericOffer()
        => Interlocked.Increment(ref importedOuterNumericOffers);

    /// <summary>
    /// Records one function site's deferrability, and the free names behind the verdict
    /// (docs/performance-roadmap.md item 1-1's remaining half).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The three name counts are what make the verdict readable</b>, and a single refusal tally
    /// is not — item 3-9 paid for that lesson: a site attributed to
    /// <see cref="DeferralRefusal.CapturesEnclosingBinding"/> is consistent with "captures one name
    /// out of forty" and with "captures everything it touches", and the capture mechanism is worth
    /// completely different amounts in the two worlds. <c>bound / free</c> is the density and
    /// <c>functionOwned / bound</c> separates a capture from a program-top-level binding, which is
    /// the distinction the mechanism's cost actually turns on.
    /// </para>
    /// <para>
    /// Occurrence-weighted rather than distinct across sites, because the question is how much
    /// boxing a real deferral would have to arrange, not how many spellings the corpus uses.
    /// </para>
    /// </remarks>
    internal static void RecordDeferralDecision(
        DeferralRefusal refusal,
        int freeNames,
        int boundFreeNames,
        int functionOwnedFreeNames,
        int cellBackedFreeNames)
    {
        Interlocked.Increment(ref deferralRefusals[(int)refusal]);
        Interlocked.Add(ref deferralFreeNames, freeNames);
        Interlocked.Add(ref deferralBoundFreeNames, boundFreeNames);
        Interlocked.Add(ref deferralFunctionOwnedFreeNames, functionOwnedFreeNames);
        Interlocked.Add(ref deferralCellBackedFreeNames, cellBackedFreeNames);
    }

    /// <summary>Records why one hoisted name did or did not reach the numeric tier.</summary>
    internal static void RecordNumericLocalDecision(NumericLocalRejection reason)
    {
        Interlocked.Increment(ref hoistedNames);
        Interlocked.Increment(ref numericRejections[(int)reason]);
    }

    public static CompilerSpecializationSnapshot Snapshot()
        => new(
            Interlocked.Read(ref scalarLocals),
            Interlocked.Read(ref numericLocals),
            Interlocked.Read(ref mixedNumericComparisons),
            Interlocked.Read(ref boxedNumericComparisons),
            Interlocked.Read(ref hoistedNames),
            ReadRejections(),
            Interlocked.Read(ref numericCandidatesOffered),
            Interlocked.Read(ref numericCandidatesRejected),
            Interlocked.Read(ref numericCandidatesDropped),
            Interlocked.Read(ref numericCandidatesSurviving),
            ReadDropCauses(),
            Interlocked.Read(ref speculativeNumericTrees),
            Interlocked.Read(ref speculativeNumericGuards),
            Read(numericTreeRefusals),
            Read(numericTreeOrderBlockers),
            Interlocked.Read(ref speculativeNumericCandidates),
            Interlocked.Read(ref speculativeNumericLocalsEmitted),
            Interlocked.Read(ref importedOuterNumericCandidates),
            Interlocked.Read(ref importedOuterNumericOffers),
            Read(deferralRefusals),
            Interlocked.Read(ref deferralFreeNames),
            Interlocked.Read(ref deferralBoundFreeNames),
            Interlocked.Read(ref deferralFunctionOwnedFreeNames),
            Interlocked.Read(ref deferralCellBackedFreeNames));

    private static long[] Read(long[] counters)
    {
        var copy = new long[counters.Length];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = Interlocked.Read(ref counters[i]);
        return copy;
    }

    private static long[] ReadDropCauses()
    {
        var copy = new long[numericDropCauses.Length];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = Interlocked.Read(ref numericDropCauses[i]);
        return copy;
    }

    private static long[] ReadRejections()
    {
        var copy = new long[numericRejections.Length];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = Interlocked.Read(ref numericRejections[i]);
        return copy;
    }

    public static void Reset()
    {
        Interlocked.Exchange(ref scalarLocals, 0);
        Interlocked.Exchange(ref numericLocals, 0);
        Interlocked.Exchange(ref mixedNumericComparisons, 0);
        Interlocked.Exchange(ref speculativeNumericTrees, 0);
        Interlocked.Exchange(ref speculativeNumericGuards, 0);
        Interlocked.Exchange(ref boxedNumericComparisons, 0);
        Interlocked.Exchange(ref hoistedNames, 0);
        Interlocked.Exchange(ref numericCandidatesOffered, 0);
        Interlocked.Exchange(ref numericCandidatesRejected, 0);
        Interlocked.Exchange(ref numericCandidatesDropped, 0);
        Interlocked.Exchange(ref numericCandidatesSurviving, 0);
        for (var i = 0; i < numericRejections.Length; i++)
            Interlocked.Exchange(ref numericRejections[i], 0);
        for (var i = 0; i < numericDropCauses.Length; i++)
            Interlocked.Exchange(ref numericDropCauses[i], 0);
        for (var i = 0; i < numericTreeRefusals.Length; i++)
            Interlocked.Exchange(ref numericTreeRefusals[i], 0);
        for (var i = 0; i < numericTreeOrderBlockers.Length; i++)
            Interlocked.Exchange(ref numericTreeOrderBlockers[i], 0);
        Interlocked.Exchange(ref speculativeNumericCandidates, 0);
        Interlocked.Exchange(ref importedOuterNumericCandidates, 0);
        Interlocked.Exchange(ref importedOuterNumericOffers, 0);
        Interlocked.Exchange(ref speculativeNumericLocalsEmitted, 0);
        for (var i = 0; i < deferralRefusals.Length; i++)
            Interlocked.Exchange(ref deferralRefusals[i], 0);
        Interlocked.Exchange(ref deferralFreeNames, 0);
        Interlocked.Exchange(ref deferralBoundFreeNames, 0);
        Interlocked.Exchange(ref deferralFunctionOwnedFreeNames, 0);
        Interlocked.Exchange(ref deferralCellBackedFreeNames, 0);
    }
}
