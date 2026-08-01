using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine;

/// <summary>
/// One entry of the call stack: the per-invocation bookkeeping a running function needs —
/// its stack-trace identity, the <c>new.target</c> handed to it, and any bindings a direct
/// eval introduced into it.
/// </summary>
/// <remarks>
/// A struct, held in the context's <see cref="CallFrameStack"/> array rather than as an object
/// per call. See <see cref="CallFrameStack"/> for why the stack is an array and not the linked
/// list of heap nodes it replaces.
/// </remarks>
public struct CallFrame
{
    public string FileName;
    public StringSpan Function;
    public int Line;
    public int Column;
    public JSValue NewTarget;
    internal Dictionary<uint, JSVariable> DirectEvalBindings;

    /// <summary>
    /// Non-null when this slot is standing in for a body that can outlive it — a generator or
    /// async function. Such a body's mutable state lives in the heap frame, and the slot is
    /// only a marker of where it currently sits on the stack; see
    /// <see cref="CallFrameStack.EnterSuspendable"/>.
    /// </summary>
    internal CallStackItem Escaped;
}

/// <summary>
/// A handle to a pushed frame, held by the running call for as long as it is on the stack.
/// </summary>
/// <remarks>
/// This is what a compiled function body keeps in place of the old activation-record
/// reference, and being a struct it is an ordinary CLR local — so an ordinary call now puts
/// nothing at all on the heap for its frame. The <see cref="Escaped"/> reference is null for
/// every ordinary call; it is carried so that a generator or async body, whose slot index is
/// not stable across suspensions, can still be found by identity rather than by position.
/// </remarks>
public readonly struct FrameToken
{
    internal readonly int Slot;
    internal readonly CallStackItem Escaped;

    internal FrameToken(int slot, CallStackItem escaped)
    {
        Slot = slot;
        Escaped = escaped;
    }

    /// <summary>A token naming no frame — what a call site passes when it has no owner to hand on.</summary>
    public static readonly FrameToken None = new(-1, null);

    internal bool IsNone => Slot < 0 && Escaped == null;
}

/// <summary>
/// The call stack of one execution context, as a growable array of <see cref="CallFrame"/>
/// rather than a linked list of per-call objects.
/// </summary>
/// <remarks>
/// <para>
/// This replaces an activation record allocated on entry to every compiled function body
/// (docs/performance-roadmap.md §7). Recycling those objects had already taken the allocation
/// to zero; moving the data into an array removes the objects themselves, and with them the
/// two rules recycling needed. Both of those rules existed for the same reason: a linked list
/// whose nodes can be reissued has links that go stale, and a stale link is indistinguishable
/// from a live one. An array has no links. The chain is the array's own order, so it cannot go
/// stale, cannot form a cycle, and needs no stamping to validate.
/// </para>
/// <para>
/// What the array cannot express by itself is a frame whose lifetime is not a contiguous span
/// of the stack, and this engine has those: a generator or async body is entered once, when it
/// is primed, and then leaves the stack at every suspension without unwinding — its caller
/// returns, the depth drops below it, and it is resumed later under a different caller
/// entirely. Those bodies keep a <see cref="CallStackItem"/> on the heap and occupy a slot that
/// merely points at it, so the data survives while the position does not.
/// </para>
/// </remarks>
public sealed class CallFrameStack
{
    private const int InitialCapacity = 64;

    /// <summary>
    /// Hard ceiling on JavaScript call depth. The CLR stack is guarded independently by
    /// <see cref="JSContextExtensions.EnsureSufficientExecutionStack"/>, which trips first in
    /// practice; this only bounds how far the array will grow if it somehow does not.
    /// </summary>
    private const int MaxDepth = 1 << 20;

    private CallFrame[] frames = new CallFrame[InitialCapacity];
    private int depth;

    /// <summary>Number of frames currently on the stack.</summary>
    public int Depth => depth;

    /// <summary>
    /// True when at least one live frame carries a direct-eval binding. Resolution walks the
    /// stack on every context-level name lookup, and in the overwhelmingly common case that no
    /// eval has introduced anything the walk can be skipped outright.
    /// </summary>
    internal bool AnyDirectEvalBindings;

    /// <summary>
    /// Native stack, in bytes, that one JavaScript execution may consume before
    /// "Maximum call stack size exceeded" is raised. 0 (the default) disables the check and
    /// leaves <see cref="JSContextExtensions.EnsureSufficientExecutionStack"/>'s CLR probe as
    /// the only limit.
    /// </summary>
    /// <remarks>
    /// The CLR probe only fires once the stack is all but gone, and .NET runs a catch handler
    /// as a funclet on the still-live stack — so a JavaScript `catch` for the resulting
    /// RangeError has no room left and re-throws on its first call, which escapes the same
    /// `try` and kills the process. (This is .NET behaviour, not an engine defect: a plain C#
    /// program that recurses until <c>TryEnsureSufficientExecutionStack</c> fails can manage a
    /// recursion depth of 1 inside its own handler.) Every real JavaScript engine can run
    /// arbitrary code in that handler because its limit trips with a deliberate reserve still
    /// free; this budget is how a host asks for the same. Set it below the thread's real stack
    /// size and the difference IS the reserve.
    /// </remarks>
    public long StackUsageLimit { get; set; }

    /// <summary>
    /// Approximate stack address where JavaScript entered on THIS thread, against which the
    /// budget is measured.
    /// </summary>
    /// <remarks>
    /// Thread-static, and that is load-bearing rather than tidiness: a context's JavaScript does
    /// not stay on one stack. A `setTimeout` callback runs on a thread-pool thread, as do async
    /// continuations and resumed generators. An anchor shared between them would be compared
    /// against an unrelated thread's stack, and the difference between two unrelated stacks is a
    /// meaningless number that can exceed any budget — which showed up as timer callbacks
    /// silently failing with a RangeError that <c>ReportError</c> swallowed.
    /// </remarks>
    [ThreadStatic]
    private static nint stackBase;

    /// <summary>
    /// Stack address at which the budget was last exceeded, or 0 when no overflow is being
    /// handled on this thread.
    /// </summary>
    /// <remarks>
    /// This is what turns the budget into a usable reserve under .NET's exception model. A catch
    /// handler runs as a funclet on top of the frames it is handling — the stack is not
    /// reclaimed until the handler finishes — so a JavaScript `catch` for this RangeError is
    /// still, physically, as deep as the throw was. Measured against the original anchor it is
    /// over budget on its very first call, and the second throw escapes the `try` that was
    /// supposed to handle it. So once the budget trips, calls at or below that point spend the
    /// reserve instead: the stack beyond the budget is exactly what it was set aside for, and
    /// <see cref="JSContextExtensions.EnsureSufficientExecutionStack"/>'s CLR probe still stands
    /// behind it as the hard floor. Cleared as soon as execution climbs back above the mark,
    /// which is the point at which the handler has genuinely returned.
    /// </remarks>
    [ThreadStatic]
    private static nint stackOverflowMark;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref CallFrame Reserve()
    {
        if (StackUsageLimit != 0)
            EnsureWithinStackBudget();

        if (depth == frames.Length)
            Grow();

        return ref frames[depth++];
    }

    // NoInlining so the marker's offset from this frame is the same on every call, which is
    // what makes the difference between two samples a usable measure of stack consumed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private unsafe void EnsureWithinStackBudget()
    {
        byte marker = 0;
        var current = (nint)(&marker);
        var anchor = stackBase;

        // Stacks grow downwards on every platform this runs on, so the outermost JavaScript
        // frame sits at the highest address. Re-anchor when this thread has none yet (0), and
        // whenever we are ABOVE the one it has — which means the recorded anchor is stale: the
        // call that set it has returned and JavaScript has re-entered nearer the top of the
        // stack. Both together mean each thread measures only against its own stack, and a
        // thread that finishes one execution starts the next from scratch rather than inheriting
        // a deep anchor and tripping early.
        if (anchor == 0 || current > anchor)
        {
            stackBase = current;
            stackOverflowMark = 0;
            return;
        }

        var mark = stackOverflowMark;
        if (mark != 0)
        {
            // Below the mark we are inside the handler for an overflow already reported, running
            // on the reserve. Above it that handler has returned, so budgeting resumes.
            if (current <= mark)
                return;

            stackOverflowMark = 0;
        }

        if (anchor - current >= StackUsageLimit)
        {
            stackOverflowMark = current;
            throw JSEngine.NewRangeError("Maximum call stack size exceeded");
        }
    }

    private void Grow()
    {
        if (depth >= MaxDepth)
            throw new InvalidOperationException("JavaScript call stack depth limit exceeded.");

        Array.Resize(ref frames, frames.Length * 2);
    }

    /// <summary>Pushes a frame for an ordinary call and returns its handle.</summary>
    public FrameToken Enter(ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column, JSValue newTarget)
    {
        var slot = depth;
        ref var frame = ref Reserve();
        frame.FileName = scriptInfo?.FileName;
        frame.Function = IsValidFunctionSpan(scriptInfo?.Code, nameOffset, nameLength)
            ? new StringSpan(scriptInfo.Code, nameOffset, nameLength)
            : CallStackItem.InlineName;
        frame.Line = line;
        frame.Column = column;
        frame.NewTarget = newTarget;
        frame.DirectEvalBindings = null;
        frame.Escaped = null;
        return new FrameToken(slot, null);
    }

    /// <summary>Pushes a frame identified by an explicit name, used for a whole script/program.</summary>
    public FrameToken Enter(string fileName, in StringSpan function, int line, int column)
    {
        var slot = depth;
        ref var frame = ref Reserve();
        frame.FileName = fileName;
        frame.Function = function;
        frame.Line = line;
        frame.Column = column;
        frame.NewTarget = null;
        frame.DirectEvalBindings = null;
        frame.Escaped = null;
        return new FrameToken(slot, null);
    }

    /// <summary>
    /// Pushes a frame for a body that can outlive its slot — a generator or async function.
    /// Its state is held in a heap frame that the slot points at, so a suspension that drops
    /// the slot loses only the position.
    /// </summary>
    public FrameToken EnterSuspendable(CallStackItem heapFrame)
    {
        var slot = depth;
        ref var frame = ref Reserve();
        frame.Escaped = heapFrame;
        frame.FileName = null;
        frame.Function = default;
        frame.Line = 0;
        frame.Column = 0;
        frame.NewTarget = null;
        frame.DirectEvalBindings = null;
        return new FrameToken(slot, heapFrame);
    }

    /// <summary>
    /// Records the statement position now executing, and re-establishes the frame as the
    /// innermost one. A resumed generator body needs the second half: its slot is long gone,
    /// so it takes a fresh one — which is why a suspendable frame is located by identity.
    /// </summary>
    public void Step(in FrameToken token, int line, int column)
    {
        var escaped = token.Escaped;
        if (escaped == null)
        {
            if ((uint)token.Slot >= (uint)depth)
                return;

            depth = token.Slot + 1;
            ref var frame = ref frames[token.Slot];
            frame.Line = line;
            frame.Column = column;
            return;
        }

        escaped.Line = line;
        escaped.Column = column;
        if (depth == 0 || !ReferenceEquals(frames[depth - 1].Escaped, escaped))
        {
            ref var frame = ref Reserve();
            frame.Escaped = escaped;
            frame.DirectEvalBindings = null;
        }
    }

    /// <summary>Pops the frame and everything above it.</summary>
    public void Pop(in FrameToken token)
    {
        var target = token.Escaped == null ? token.Slot : FindEscaped(token.Escaped);
        if (target < 0 || target >= depth)
            return;

        // Clearing the references the abandoned slots held keeps a deep call that has since
        // returned from pinning its arguments and script text alive through the array.
        for (var i = target; i < depth; i++)
            frames[i] = default;

        depth = target;
    }

    private int FindEscaped(CallStackItem escaped)
    {
        for (var i = depth - 1; i >= 0; i--)
            if (ReferenceEquals(frames[i].Escaped, escaped))
                return i;

        return -1;
    }

    /// <summary>
    /// Unwinds to a previously observed depth. Only ever shrinks: growing back would resurrect
    /// slots whose contents have been abandoned, which is exactly the class of bug that made a
    /// reissued frame dangerous in the first place.
    /// </summary>
    public void RestoreDepth(int savedDepth)
    {
        if (savedDepth < 0 || savedDepth >= depth)
            return;

        for (var i = savedDepth; i < depth; i++)
            frames[i] = default;

        depth = savedDepth;
    }

    /// <summary>The <c>new.target</c> of the innermost frame, or null when the stack is empty.</summary>
    public JSValue CurrentNewTarget
    {
        get
        {
            if (depth == 0)
                return null;

            ref var frame = ref frames[depth - 1];
            return frame.Escaped?.NewTarget ?? frame.NewTarget;
        }
    }

    public int CurrentLine => depth == 0 ? 0 : (frames[depth - 1].Escaped?.Line ?? frames[depth - 1].Line);

    public int CurrentColumn => depth == 0 ? 0 : (frames[depth - 1].Escaped?.Column ?? frames[depth - 1].Column);

    /// <summary>
    /// Reads the identity of frame <paramref name="index"/>, resolving a suspendable frame to
    /// the heap frame standing behind it.
    /// </summary>
    public void Describe(int index, out StringSpan function, out string fileName, out int line, out int column)
    {
        ref var frame = ref frames[index];
        var escaped = frame.Escaped;
        if (escaped != null)
        {
            function = escaped.Function;
            fileName = escaped.FileName;
            line = escaped.Line;
            column = escaped.Column;
            return;
        }

        function = frame.Function;
        fileName = frame.FileName;
        line = frame.Line;
        column = frame.Column;
    }

    // ── direct-eval bindings ────────────────────────────────────────────────────────

    internal void RegisterDirectEvalBinding(in FrameToken token, JSVariable variable)
    {
        if (variable == null || variable.Name.IsEmpty)
            return;

        var escaped = token.Escaped;
        if (escaped != null)
        {
            escaped.RegisterDirectEvalBinding(variable);
            AnyDirectEvalBindings = true;
            return;
        }

        if ((uint)token.Slot >= (uint)depth)
            return;

        ref var frame = ref frames[token.Slot];
        frame.DirectEvalBindings ??= [];
        frame.DirectEvalBindings[KeyStrings.GetOrCreate(variable.Name).Key] = variable;
        AnyDirectEvalBindings = true;
    }

    internal bool TryGetDirectEvalBinding(int index, in KeyString name, out JSVariable variable)
    {
        ref var frame = ref frames[index];
        var bindings = frame.Escaped != null ? frame.Escaped.DirectEvalBindings : frame.DirectEvalBindings;
        if (bindings?.TryGetValue(name.Key, out variable) == true)
            return true;

        variable = null;
        return false;
    }

    internal bool DeleteDirectEvalBinding(int index, in KeyString name)
    {
        ref var frame = ref frames[index];
        var bindings = frame.Escaped != null ? frame.Escaped.DirectEvalBindings : frame.DirectEvalBindings;
        return bindings?.Remove(name.Key) == true;
    }

    private static bool IsValidFunctionSpan(string code, int offset, int length)
        => code != null
            && offset >= 0
            && length > 0
            && offset <= code.Length
            && length <= code.Length - offset;
}
