using System.Threading;

namespace Broiler.JavaScript.BuiltIns.Number;

/// <summary>
/// Counts the JavaScript numbers the engine boxes, so phase 3's remaining prize can be bounded
/// rather than argued (docs/performance-roadmap.md items 3-8 and 3-1).
/// </summary>
/// <remarks>
/// <para>
/// Every phase 3 item so far was sized by a per-shape figure — 31.98 bytes an iteration, over and
/// over — and every one of them then moved the whole corpus by nothing. The number that would have
/// predicted that is the one nobody had: how much of a real run's allocation is number boxing at
/// all. A raw <c>double</c> anywhere can only ever remove a box, so this count times the box size
/// is the ceiling on 3-0, 3-1, 3-2, 3-3, 3-5, 3-7 and 3-8 together.
/// </para>
/// <para>
/// <strong>Two counters, because there are two ways to mint a <c>JSNumber</c> and they belong to
/// different items.</strong> <see cref="JSNumber.Create"/> is what the compiler emits for a boxing
/// conversion and what <c>JSValue.CreateNumber</c> routes the generic operators through, so
/// <c>FactoryAllocations</c> is the population an unboxed representation reaches. A builtin that
/// writes <c>new JSNumber(x)</c> directly — <c>JSMath</c> alone has 57 — bypasses the factory
/// entirely, so <c>Allocations</c> counts at the constructor and the difference between the two is
/// the builtin return values. Item 3-8's first census counted only the factory and was therefore a
/// <em>lower</em> bound; naming the gap is what turns it into a measurement.
/// </para>
/// <para>
/// <c>CacheHits</c> is the share the small-integer table answers without allocating. It matters
/// because a probe that counts boxing <em>operations</em> and calls the answer bytes overstates it
/// by whatever the table absorbs.
/// </para>
/// <para>
/// Off by default. The check is a plain static read on a branch that predicts perfectly while it
/// is off — the same bargain <c>CallPathDiagnostics</c> makes, and unlike that one this sits on an
/// allocation path, so a counter costing a fraction of the allocation it counts needs no more
/// elaborate gating.
/// </para>
/// </remarks>
public static class NumberBoxingDiagnostics
{
    private static long requests;
    private static long cacheHits;
    private static long factoryAllocations;
    private static long allocations;
    private static long literalRequests;
    private static long conversionRequests;

    /// <summary>Whether boxing is counted. Off by default.</summary>
    public static bool Enabled;

    /// <summary>One call to the boxing factory, whatever it goes on to do.</summary>
    internal static void RecordRequest() => Interlocked.Increment(ref requests);

    /// <summary>
    /// A request from a compile-time numeric LITERAL. A subset of <c>Requests</c>, counted apart
    /// because a literal's box is the one boxing the engine could avoid without changing any
    /// representation at all — the value is known at compile time.
    /// </summary>
    internal static void RecordLiteralRequest() => Interlocked.Increment(ref literalRequests);

    /// <summary>
    /// A request from the compiler's boxing CONVERSION — a raw double crossing into a
    /// <c>JSValue</c>. Disjoint from the literal count and from every operator result, so
    /// `Requests - conversion - literal` is what the operators and builtins mint.
    /// </summary>
    internal static void RecordConversionRequest() => Interlocked.Increment(ref conversionRequests);

    /// <summary>A request the small-integer table answered without allocating.</summary>
    internal static void RecordCacheHit() => Interlocked.Increment(ref cacheHits);

    /// <summary>A request the factory had to allocate for.</summary>
    internal static void RecordFactoryAllocation() => Interlocked.Increment(ref factoryAllocations);

    /// <summary>
    /// Every <see cref="JSNumber"/> the process constructs, counted at the constructor so a
    /// builtin that bypasses the factory is included.
    /// </summary>
    internal static void RecordAllocation() => Interlocked.Increment(ref allocations);

    public static NumberBoxingSnapshot Snapshot()
        => new(
            Interlocked.Read(ref requests),
            Interlocked.Read(ref cacheHits),
            Interlocked.Read(ref factoryAllocations),
            Interlocked.Read(ref allocations),
            Interlocked.Read(ref literalRequests),
            Interlocked.Read(ref conversionRequests));

    public static void Reset()
    {
        Interlocked.Exchange(ref requests, 0);
        Interlocked.Exchange(ref cacheHits, 0);
        Interlocked.Exchange(ref factoryAllocations, 0);
        Interlocked.Exchange(ref allocations, 0);
        Interlocked.Exchange(ref literalRequests, 0);
        Interlocked.Exchange(ref conversionRequests, 0);
    }
}

/// <param name="Requests">Calls to the boxing factory.</param>
/// <param name="CacheHits">Those the small-integer table answered without allocating.</param>
/// <param name="FactoryAllocations">Those the factory allocated for — the population an unboxed representation reaches.</param>
/// <param name="Allocations">Every <see cref="JSNumber"/> constructed, factory or not.</param>
/// <param name="LiteralRequests">Factory calls made by a compile-time numeric literal.</param>
/// <param name="ConversionRequests">Factory calls made by the compiler boxing a raw double to cross into a <c>JSValue</c>.</param>
public readonly record struct NumberBoxingSnapshot(
    long Requests,
    long CacheHits,
    long FactoryAllocations,
    long Allocations,
    long LiteralRequests,
    long ConversionRequests)
{
    /// <summary>
    /// Boxes minted by a builtin writing <c>new JSNumber(x)</c> instead of going through the
    /// factory — the population no change to the compiler's representation can reach.
    /// </summary>
    public long DirectAllocations => Allocations - FactoryAllocations;
}
