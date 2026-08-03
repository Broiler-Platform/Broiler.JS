using System;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits allocated bytes per array ELEMENT, separately for writing an array and for reading one
/// back, as JSON.
/// </summary>
/// <remarks>
/// Exists to size roadmap item 3-1, which proposes a typed backing store (<c>double[]</c>,
/// <c>int[]</c>) for dense arrays on the grounds that "a dense array of a million doubles is
/// still a million heap objects behind a million interface references". That is a claim about
/// the WRITE side, and the item does not state what it costs on the read side — which is the
/// half that decides it, because <c>ElementArray</c>'s dense store is <c>IPropertyValue[]</c>
/// and every read hands back an <c>IPropertyValue</c>. A raw <c>double[]</c> cannot do that
/// without boxing a fresh <c>JSNumber</c> per read, so the item trades a write allocation for a
/// read allocation and the exchange rate is the whole question.
/// <para>
/// So both sides are measured, and separately. The read rows are the ones with no counterpart in
/// the item's text: today they should allocate nothing at all, because the value is already a
/// heap object and reading it is a reference copy. Whatever they cost after a typed store lands
/// is a cost 3-1 introduces, and this is the row it has to be judged against.
/// </para>
/// <para>
/// Small integers are split from large ones deliberately. P2-1 landed a per-thread
/// small-integer cache, so an element assigned a small <c>int</c> may already allocate nothing —
/// in which case 3-1 buys nothing for the integer case it names and the double case is the whole
/// item. That is a fact about the current engine, not a prediction, which is why it is measured
/// before anything is built.
/// </para>
/// </remarks>
internal static class ElementAllocationMetrics
{
    private const int Elements = 100_000;

    private sealed record Site(string Name, string Write, string Read, string Note);

    /// <summary>
    /// The same loops with the array access removed, so a row can be reported net of the loop
    /// that carries it.
    /// </summary>
    /// <remarks>
    /// Without this the numbers are useless and do not look it: the loop that carries the access
    /// allocates for the index, the accumulator and the boxing around them whatever the element
    /// store is, and that cost swamps the element. Measured raw against an <c>s += a[i]</c> body,
    /// a string array and a double array read at the same 63.7 B/element — which says nothing
    /// about element storage and everything about the loop. §3.5 records the same failure under
    /// 2-4: a probe whose bulk is inert dilutes the effect and keeps all of the noise.
    /// <para>
    /// Both bodies are deliberately written against DECLARED locals. The first version of this
    /// control used a bare <c>x = i + 0.5</c>, which is an undeclared global — a property store,
    /// not a local assignment — so the "control" allocated 88 B/element and every array site
    /// measured *cheaper* than it. A control has to be inert; this one is.
    /// </para>
    /// </remarks>
    private static readonly Site LoopControl = new(
        "loop-control",
        "x = i + 0.5;",
        "v = w;",
        "the same loops with no array access at all; subtract to get the element's own cost");

    private static readonly Site[] Sites =
    [
        new(
            "write-number",
            "a[i] = i + 0.5;",
            "v = a[i];",
            "a fresh number per element: the JSNumber a typed store would not allocate"),
        new(
            "write-reference",
            "a[i] = t;",
            "v = a[i];",
            "the SAME loop storing a hoisted reference, so the difference from write-number is "
                + "exactly the boxed value and nothing else — this is item 3-1's write prize"),
        new(
            "write-int-small",
            "a[i] = i & 1023;",
            "v = a[i];",
            "integers inside the per-thread small-integer cache: if these match write-reference, "
                + "P2-1 already took the integer case and 3-1 is a double-only item"),
        new(
            "write-int-large",
            "a[i] = i + 1000000;",
            "v = a[i];",
            "integers past the cache"),
        new(
            "read-only-number",
            "a[i] = i + 0.5;",
            "v = a[i];",
            "read side of a numeric array: today a reference copy, and what a typed store would "
                + "have to box back into a JSNumber on every read"),
        new(
            "read-only-reference",
            "a[i] = t;",
            "v = a[i];",
            "read side of a reference array — the floor a typed store's read can never beat"),
        new(
            "constant-index-reference",
            "a[0] = t;",
            "v = a[0];",
            "the same store and read at a CONSTANT index: the difference from write-reference is "
                + "what boxing the index costs, which is charged to every element access whatever "
                + "the element store holds"),
        new(
            "constant-index-number",
            "a[0] = i + 0.5;",
            "v = a[0];",
            "constant index, fresh number: isolates the value boxing from the index boxing"),
        new(
            "constant-index-reference",
            "a[0] = t;",
            "v = a[0];",
            "the same store and read at a CONSTANT index: the difference from write-reference is "
                + "what boxing the index costs, and that is charged to every element access "
                + "whatever the element store holds"),
        new(
            "constant-index-number",
            "a[0] = i + 0.5;",
            "v = a[0];",
            "constant index, fresh number: separates the value boxing from the index boxing"),
    ];

    internal static void Write()
    {
        var control = Measure(LoopControl);
        var rows = Sites.Select(site => Measure(site, control)).ToArray();

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.element-allocation-metrics/2",
                elementsPerSite = Elements,
                note = "Bytes per element, warmed then measured after a forced gen2 collection. "
                    + "Write and read are measured separately because item 3-1 trades one for the "
                    + "other, and both are reported net of a no-array loop control because the "
                    + "loop costs more than the element does. See docs/performance-roadmap.md "
                    + "item 3-1.",
                loopControl = control,
                sites = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Row Measure(Site site, Row control = null)
    {
        using var context = BenchmarkContext.Create();

        var allocate = site.Name == "double-preallocated"
            ? $"new Array({Elements})"
            : "[]";

        // Two functions rather than one, because the point is to attribute allocation to the
        // write or to the read and a single loop cannot. The read function takes the array the
        // write function returns, so it reads a real populated store rather than a fresh one.
        var writeSource = $$"""
            (function () {
                var t = 'x';
                var x = 0;
                var a = {{allocate}};
                for (var i = 0; i < {{Elements}}; i++) { {{site.Write}} }
                return a;
            })
            """;

        var readSource = $$"""
            (function (a) {
                var s = 0;
                var v = null;
                var w = a === null ? null : a[0];
                for (var i = 0; i < {{Elements}}; i++) { {{site.Read}} }
                return s;
            })
            """;

        var write = context.Eval(writeSource, $"{site.Name}-write.js");
        var read = context.Eval(readSource, $"{site.Name}-read.js");
        var noArguments = new Arguments(JSUndefined.Value);

        // Warmed first, so the measured runs pay for elements and nothing else — the compile, the
        // inline-cache entries and the array's own growth steps are one-time and would otherwise
        // land in the delta.
        var warm = write.InvokeFunction(in noArguments);
        read.InvokeFunction(new Arguments(JSUndefined.Value, warm));

        var writeBytes = Sample(() => write.InvokeFunction(in noArguments));

        var array = write.InvokeFunction(in noArguments);
        var readArguments = new Arguments(JSUndefined.Value, array);
        var readBytes = Sample(() => read.InvokeFunction(in readArguments));

        var writePerElement = Math.Round((double)writeBytes / Elements, 2);
        var readPerElement = Math.Round((double)readBytes / Elements, 2);

        return new Row(
            site.Name,
            site.Note,
            writePerElement,
            readPerElement,
            control == null ? null : Math.Round(writePerElement - control.WriteBytesPerElement, 2),
            control == null ? null : Math.Round(readPerElement - control.ReadBytesPerElement, 2),
            writeBytes,
            readBytes);
    }

    /// <summary>One measured shape. The <c>net</c> figures are what item 3-1 is about.</summary>
    internal sealed record Row(
        string Site,
        string Note,
        double WriteBytesPerElement,
        double ReadBytesPerElement,
        double? NetWriteBytesPerElement,
        double? NetReadBytesPerElement,
        long WriteTotalBytes,
        long ReadTotalBytes);

    private static long Sample(Action body)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        body();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
