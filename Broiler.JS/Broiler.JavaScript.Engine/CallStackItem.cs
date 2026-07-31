using System;
using System.ComponentModel;
using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Engine;

/// <summary>
/// The heap-resident half of a call frame, allocated only for a body whose frame can outlive
/// its position on the stack — a generator or an async function.
/// </summary>
/// <remarks>
/// <para>
/// Every other call keeps its frame entirely inside the context's <see cref="CallFrameStack"/>
/// array and allocates nothing (docs/performance-roadmap.md §7). A suspendable body cannot: it
/// is entered once, when primed, and then leaves the stack at every suspension without
/// unwinding, to be resumed later under a different caller. Its slot goes away; this does not,
/// so the mutable state — position, <c>new.target</c>, any direct-eval bindings — lives here
/// and the slot merely points at it.
/// </para>
/// <para>
/// This is one allocation per generator instance, against none per ordinary call.
/// </para>
/// </remarks>
public sealed class CallStackItem
{
    internal static readonly StringSpan InlineName = "inline";

    internal CallStackItem(string fileName, in StringSpan function, int line, int column)
    {
        FileName = fileName;
        Function = function;
        Line = line;
        Column = column;
    }

    public StringSpan Function;
    public string FileName;
    public int Line;
    public int Column;
    public JSValue NewTarget;

    internal Dictionary<uint, JSVariable> DirectEvalBindings;

    internal void RegisterDirectEvalBinding(JSVariable variable)
    {
        if (variable == null || variable.Name.IsEmpty)
            return;

        DirectEvalBindings ??= [];
        DirectEvalBindings[KeyStrings.GetOrCreate(variable.Name).Key] = variable;
    }

    public override string ToString() => $"{Function} at {FileName} - {Line},{Column}";
}

/// <summary>
/// The entry points a compiled function body calls to push, position and pop its frame.
/// </summary>
/// <remarks>
/// Static rather than instance methods because a frame is now a <see cref="FrameToken"/> — a
/// struct the body holds as an ordinary CLR local — and the stack it indexes belongs to the
/// context.
/// </remarks>
public static class CallFrames
{
    /// <summary>Enters an ordinary call. Allocates nothing.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static FrameToken Enter(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
    {
        context = Resolve(context);
        context.EnsureSufficientExecutionStack();
        return context.Frames.Enter(scriptInfo, nameOffset, nameLength, line, column, TakeNewTarget(context));
    }

    /// <summary>Enters a generator or async body, which needs a heap frame; see <see cref="CallStackItem"/>.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static FrameToken EnterSuspendable(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
    {
        context = Resolve(context);
        context.EnsureSufficientExecutionStack();
        var heapFrame = new CallStackItem(
            scriptInfo?.FileName,
            IsValidFunctionSpan(scriptInfo?.Code, nameOffset, nameLength)
                ? new StringSpan(scriptInfo.Code, nameOffset, nameLength)
                : CallStackItem.InlineName,
            line,
            column)
        { NewTarget = TakeNewTarget(context) };

        return context.Frames.EnterSuspendable(heapFrame);
    }

    /// <summary>Enters a whole script or program.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static FrameToken EnterScope(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
    {
        context = Resolve(context);
        context.EnsureSufficientExecutionStack();
        return context.Frames.Enter(fileName, in function, line, column);
    }

    /// <summary>Enters a top-level-await program, which is rewritten into a state machine.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static FrameToken EnterSuspendableScope(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
    {
        context = Resolve(context);
        context.EnsureSufficientExecutionStack();
        return context.Frames.EnterSuspendable(new CallStackItem(fileName, in function, line, column));
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Step(IJSExecutionContext context, FrameToken token, int line, int column)
        => Resolve(context).Frames.Step(in token, line, column);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Pop(IJSExecutionContext context, FrameToken token)
        => Resolve(context).Frames.Pop(in token);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterDirectEvalBinding(IJSExecutionContext context, FrameToken token, JSVariable variable)
        => Resolve(context).Frames.RegisterDirectEvalBinding(in token, variable);

    /// <summary>The <c>new.target</c> of the frame currently running, as `new.target` reads it.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static JSValue CurrentNewTarget(IJSExecutionContext context)
        => Resolve(context).Frames.CurrentNewTarget;

    /// <summary>
    /// Consumes the pending <c>new.target</c>. It is cleared as it is taken so that an ordinary
    /// call made from inside a constructor does not observe the constructor's.
    /// </summary>
    private static JSValue TakeNewTarget(IJSExecutionContext context)
    {
        var newTarget = context.CurrentNewTarget;
        if (newTarget != null)
            context.CurrentNewTarget = null;

        return newTarget;
    }

    private static IJSExecutionContext Resolve(IJSExecutionContext context)
    {
        context ??= JSEngine.Current as IJSExecutionContext;
        return context ?? throw new InvalidOperationException(
            "Cannot enter JavaScript execution without an active JS execution context.");
    }

    private static bool IsValidFunctionSpan(string code, int offset, int length)
        => code != null
            && offset >= 0
            && length > 0
            && offset <= code.Length
            && length <= code.Length - offset;
}
