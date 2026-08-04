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
    long NumericCandidatesDropped);

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
    /// <summary>A <c>let</c>/<c>const</c> in a nested block, which is a distinct binding per entry.</summary>
    LexicalOutsideFunctionBody,
    /// <summary>The analysis could not prove the name only ever holds a number.</summary>
    NotProvenNumeric,
}

/// <summary>Compilation counters used by Phase 3 tests and benchmark reports.</summary>
public static class CompilerSpecializationDiagnostics
{
    private static long scalarLocals;
    private static long numericLocals;
    private static long mixedNumericComparisons;
    private static long boxedNumericComparisons;
    private static long hoistedNames;
    private static readonly long[] numericRejections = new long[8];
    private static long numericCandidatesOffered;
    private static long numericCandidatesDropped;

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

    /// <summary>How many of those the optimistic fixed point then had to drop.</summary>
    internal static void RecordNumericCandidatesDropped(int count)
        => Interlocked.Add(ref numericCandidatesDropped, count);

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
            Interlocked.Read(ref numericCandidatesDropped));

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
        Interlocked.Exchange(ref boxedNumericComparisons, 0);
        Interlocked.Exchange(ref hoistedNames, 0);
        Interlocked.Exchange(ref numericCandidatesOffered, 0);
        Interlocked.Exchange(ref numericCandidatesDropped, 0);
        for (var i = 0; i < numericRejections.Length; i++)
            Interlocked.Exchange(ref numericRejections[i], 0);
    }
}
