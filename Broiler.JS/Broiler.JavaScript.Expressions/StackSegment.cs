using System;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.ExpressionCompiler;

/// <summary>
/// Tracks how much stack a recursive walk has consumed and says when it should continue on a
/// fresh one (roadmap item 1-2, the real fix).
/// </summary>
/// <remarks>
/// The front end recurses over source <em>nesting</em> in several independent passes — the
/// parser's recursive descent, the syntax validator's <c>AstMapVisitor</c> walk, and the IL
/// emitter's <c>BExpressionVisitor</c> walk — and each can exhaust the thread it is on. They
/// share the rules here rather than each carrying a copy, because the rules are exactly what the
/// original <c>StackGuard</c> got wrong three times over: it measured the difference in the wrong
/// direction on a downward-growing stack, truncated 64-bit addresses to <c>int</c>, and set its
/// threshold at 1 024 <em>bytes</em>.
/// <para>
/// A walk keeps one <see cref="StackSegment"/> as a field and calls <see cref="ShouldSegment"/> at
/// its dispatch point, passing the address of one of its own locals. The struct is mutable by
/// design and must be stored in a field, never copied: the anchor it holds is per-walk state.
/// </para>
/// <para>
/// Verified by turning each mechanism off independently. On a 20 000-operator chain through the
/// script host: mitigation on / guard on completes, <em>mitigation off / guard on completes</em>
/// — which is this class doing the work — mitigation on / guard off completes, and with both off
/// the process aborts. That last row is what makes the other three mean anything.
/// </para>
/// </remarks>
public struct StackSegment
{
    /// <summary>
    /// Stack consumed, in bytes, before a walk continues on a fresh stack. Overridable with
    /// <c>BROILER_JS_VISITOR_SEGMENT_BYTES</c>; <c>0</c> disables segmentation entirely, which is
    /// what makes a deeply-nested fixture decisive rather than merely green.
    /// </summary>
    /// <remarks>
    /// Deliberately well below <see cref="CompilationStack.DefaultSizeBytes"/>. A walk cannot
    /// know how large the stack it is standing on actually is, and a threshold above it would
    /// never be reached before the CLR aborted the process; 4 MiB leaves room on a 64 MiB
    /// compilation worker for the frames between one segment and the next, and still segments on
    /// a smaller host thread rather than riding it to the floor.
    /// </remarks>
    public static readonly long SegmentAtBytes = Resolve();

    private const long DefaultSegmentAtBytes = 4L * 1024 * 1024;

    private static long Resolve()
    {
        var configured = Environment.GetEnvironmentVariable("BROILER_JS_VISITOR_SEGMENT_BYTES");
        return configured != null && long.TryParse(configured, out var bytes) && bytes >= 0
            ? bytes
            : DefaultSegmentAtBytes;
    }

    // The stack address the outermost Enter anchored to. Scoped to that walk rather than left set
    // for the life of the visitor: a reused visitor whose next walk begins at a lower address
    // would otherwise measure the difference between two unrelated stacks and segment at once.
    private nint anchor;

    /// <summary>
    /// Whether this walk has consumed more stack than <see cref="SegmentAtBytes"/> since it
    /// began, given the address of a local in the caller's frame.
    /// </summary>
    /// <remarks>
    /// The difference is taken as the <see cref="CallFrames"/>-style <c>anchor - current</c>,
    /// because the stack grows downwards, and read as <c>ulong</c> so that a host whose stack
    /// grows the other way degrades to "never segments" rather than to "segments on every node".
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldSegment(nint current)
    {
        if (anchor == 0)
        {
            anchor = current;
            return false;
        }

        if (SegmentAtBytes == 0)
            return false;

        var consumed = (ulong)(anchor - current);
        return consumed >= (ulong)SegmentAtBytes && consumed <= long.MaxValue;
    }

    /// <summary>Whether this walk has anchored yet — i.e. whether the caller is its outermost frame.</summary>
    public readonly bool IsAnchored => anchor != 0;

    /// <summary>Releases the anchor, so the next outermost walk anchors to its own stack.</summary>
    public void Release() => anchor = 0;

    /// <summary>
    /// Runs <paramref name="work"/> on a fresh sized stack, with this walk's anchor cleared for
    /// the duration so the continuation anchors to the new stack and restored afterwards so the
    /// frames still unwinding on this one keep measuring against their own.
    /// </summary>
    public T Continue<T>(Func<T> work)
    {
        var previous = anchor;
        anchor = 0;
        try
        {
            // RunOnFreshStack, not Run: a segmenter only ever fires from inside a compilation,
            // and Run returns inline in exactly that case — it would segment nothing.
            return CompilationStack.RunOnFreshStack(work);
        }
        finally
        {
            anchor = previous;
        }
    }
}
