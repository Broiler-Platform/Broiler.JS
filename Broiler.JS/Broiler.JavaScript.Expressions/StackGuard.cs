using System;
using System.Threading;
using System.Threading.Tasks;

namespace Broiler.JavaScript.ExpressionCompiler;

public class BDispatcher
{
    public static object Queue(object input, Func<object,object> func)
    {
        TaskCompletionSource<object> result = new();
        ThreadPool.QueueUserWorkItem((input) => {
            result.SetResult(func(input));
        }, input);
        return result.Task.GetAwaiter().GetResult();
    }
}

/// <summary>
/// Base for the expression-compiler visitors, segmenting a recursive walk onto a fresh stack
/// before it can exhaust the one it is on (roadmap item 1-2, the real fix).
/// </summary>
/// <remarks>
/// This existed before item 1-2 and <strong>could not fire</strong>. Three things were wrong at
/// once, and each alone was enough: it measured <c>address - start</c> on a stack that grows
/// <em>downwards</em>, so the difference was negative and the branch was unreachable; it
/// truncated both stack addresses to <c>int</c>, which on a 64-bit host discards the high half of
/// a pointer; and its threshold was <strong>1 024 bytes</strong>, so had the sign been right it
/// would have hopped threads roughly every four stack frames.
/// <para>
/// The direction is the one <c>CallFrames.EnsureWithinStackBudget</c> already gets right and says
/// so in a comment: the anchor is the high address, the current frame is below it, and the amount
/// consumed is <c>anchor - current</c>. Measured as an unsigned difference here so that a host
/// whose stack grows the other way degrades to "never segments" rather than to "segments on every
/// node".
/// </para>
/// <para>
/// Segmentation continues on a <see cref="CompilationStack"/> worker rather than on a thread-pool
/// thread. A pool thread is 1 MiB and is exactly the stack this guard exists to escape, so
/// hopping onto one bought a few frames and no more; a compilation worker is sized by the engine
/// (64 MiB by default) and is parked and reused, so the hop costs a pair of semaphore handoffs
/// instead of a thread creation.
/// </para>
/// <para>
/// <strong>Compiler stack depth is a function of source NESTING, not source size</strong> — a flat
/// 200 000-statement function compiles fine while ~19 400 nested operators did not — so the
/// threshold is in megabytes of consumed stack and not in nodes visited.
/// </para>
/// </remarks>
public abstract class StackGuard<T,TIn> {

    private StackSegment segment;

    public unsafe T Visit(TIn input)
    {
        int self;
        var current = (nint)(&self);
        var outermost = !segment.IsAnchored;

        if (!segment.ShouldSegment(current))
        {
            if (!outermost)
                return VisitIn(input);

            try
            {
                return VisitIn(input);
            }
            finally
            {
                segment.Release();
            }
        }

        return segment.Continue(() => VisitIn(input));
    }

    public abstract T VisitIn(TIn input);

}
