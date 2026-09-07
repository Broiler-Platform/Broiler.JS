using System;

namespace Broiler.JavaScript.ExpressionCompiler;

/// <summary>
/// Thrown by a guarded walk that has run out of stack to finish on, so that the frames it is
/// standing in unwind and the whole operation can be retried on a sized stack.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StackSegment"/>'s ordinary answer to a deep tree is to continue on a fresh stack
/// from where it stands, which costs a pair of semaphore handoffs and keeps the frames already
/// walked. That works while there is room left to perform the handoff. It stops working at the
/// very bottom of a small stack: the hop itself needs frames — an execution-context capture, a
/// closure, a semaphore wait — and a walk that discovers the shortage too late overflows *while
/// escaping*, which is what a Windows CI leg showed as
/// </para>
/// <code>
///   at CompilationStack+Worker.Run
///   at CompilationStack.RunOnFreshStack
///   at StackSegment.Continue          &lt;- the hop itself
///   at ILCodeGenerator.VisitReturn
///   at ILCodeGenerator.VisitReturn ...
/// </code>
/// <para>
/// Unwinding needs no frames, so it always works. The cost is that the walk is abandoned rather
/// than continued, and whatever it had built is discarded — which is only safe at a boundary
/// where the operation is a function of its inputs. <c>CompileToBoundDynamicMethod</c> is one:
/// it makes a new <c>DynamicMethod</c>, a new <c>ILGenerator</c> and a new <c>ILCodeGenerator</c>
/// on every call, so a half-emitted method is garbage and nothing else. Inner lambdas already
/// registered with the method repository are re-registered by the retry and their first ids are
/// orphaned; the emitted code refers to the second set throughout, so this leaks entries rather
/// than corrupting anything.
/// </para>
/// <para>
/// This is deliberately NOT the design <see cref="Runtime.DeferredMethod"/> rejected, which was
/// to compile every deferred body on a sized worker: that paid a ~180 us handoff per function
/// and took a suite from 3.5 minutes to over 20. A retry is paid only by a walk that could not
/// finish, which is a deep tree and almost never. A shallow body never throws, never unwinds and
/// never hops, exactly as before.
/// </para>
/// </remarks>
public sealed class StackSegmentExhausted : Exception
{
    public StackSegmentExhausted()
        : base("The walk ran out of stack; retry the operation on a sized stack.")
    {
    }
}
