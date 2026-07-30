using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.Generic;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Engine;

public class CallStackItem
{
    private static readonly StringSpan Inline = "inline";

    internal CallStackItem(string fileName, in StringSpan function, int line, int column)
    {
        FileName = fileName;
        Function = function;
        Line = line;
        Column = column;
    }

    private CallStackItem() { }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CallStackItem(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
        => Push(context, scriptInfo, nameOffset, nameLength, line, column);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CallStackItem(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
        => Push(context, fileName, in function, line, column);

    // ── frame pool ────────────────────────────────────────────────────────────────
    //
    // A frame is per-invocation bookkeeping — a stack-trace entry, the new.target
    // handoff and the direct-eval binding registry — and it dies at the closing brace
    // of the call that made it. Allocating one per call made an argument-less call cost
    // exactly 80 bytes and nothing else (docs/performance-roadmap.md §7), so the frames
    // are recycled through a free list instead.
    //
    // Pop is the sole release point, and every frame whose lifetime is a synchronous span
    // is released exactly once, by the finally its own call emitted. A frame that is
    // stranded (never popped) is only ever leaked to the GC, never handed to a second
    // owner — so the failure direction of this pool is "allocates like before", not
    // "two callers share a frame". Frames that are NOT a synchronous span — generator and
    // async bodies — opt out entirely; see PinSuspendable.
    //
    // Thread-local, because that is the only lock-free way to hand a frame back: the list
    // is touched on every call and a context runs on one thread at a time.
    /// <summary>
    /// The free list itself, in one thread-local cell. Head and count live together in a
    /// holder object so that renting or releasing costs a single thread-local lookup rather
    /// than one per field — on a path that runs on every call, that difference is measurable.
    /// </summary>
    private sealed class FrameStore
    {
        public CallStackItem Head;
        public int Count;
    }

    [ThreadStatic]
    private static FrameStore store;

    /// <summary>
    /// Upper bound on retained frames. Deep recursion unwinding would otherwise park one
    /// frame per level on the free list forever; past this depth the surplus is dropped
    /// and collected normally.
    /// </summary>
    private const int MaxPooledFrames = 512;

    private CallStackItem poolNext;

    /// <summary>Set by <see cref="Pop"/>; makes a second Pop of the same frame a no-op.</summary>
    private bool released;

    /// <summary>
    /// Set by <see cref="PinSuspendable"/> — a suspended body is this frame, so it must never
    /// be reissued. Sticky for the life of the object, and deliberately not cleared on
    /// <see cref="Push"/>: a pinned frame never re-enters the pool, so it is never pushed again.
    /// </summary>
    private bool pinned;

    /// <summary>
    /// How many times this frame object has been pushed. Paired with <see cref="parentStamp"/>
    /// it makes a <see cref="Parent"/> link self-validating: see <see cref="Caller"/>.
    /// </summary>
    private uint stamp;

    /// <summary>The <see cref="stamp"/> <see cref="Parent"/> carried when this frame linked to it.</summary>
    private uint parentStamp;

    /// <summary>
    /// The frame this one was pushed under, or <c>null</c> once that link has gone stale.
    /// Walkers must use this rather than <see cref="Parent"/>.
    /// <para>
    /// Not every frame is popped in the order it was pushed. A generator or async body that
    /// suspends strands itself, and everything it had called, off the stack without running
    /// their <c>finally</c>s — so those frames go on naming a parent that has since returned.
    /// Before frames were recycled that was harmless: <see cref="Pop"/> nulls the dead frame's
    /// own Parent, so a walk through a stale link simply stopped one frame late. Once frames
    /// are reissued it is not harmless at all — the stale link points at whatever call owns
    /// that object now, and if that call was made from inside the resumed body the chain
    /// closes into a cycle and the walker spins forever. That is not theoretical: it showed up
    /// as an intermittent hang in test262 built-ins/AsyncGeneratorPrototype, looping inside
    /// TryResolveDirectEvalBinding.
    /// </para>
    /// <para>
    /// So a link records the parent's push count and is followed only while that count still
    /// matches. Reissuing a frame invalidates every link into it without having to find them,
    /// and a stale link reads as "no caller" — which is exactly what the pre-pooling engine
    /// reported, since it had nulled that frame's Parent on the way out.
    /// </para>
    /// </summary>
    public CallStackItem Caller
    {
        get
        {
            var parent = Parent;
            return parent != null && parent.stamp == parentStamp ? parent : null;
        }
    }

    /// <summary>
    /// Links this frame under the context's current top and stamps the link.
    /// <para>
    /// The top is only accepted as a caller if it is still <em>live</em>. It need not be:
    /// <c>JSGenerator.MoveNext</c> restores the top it captured on entry, and by then that
    /// frame may have been popped by the body it just ran — so the engine can be sitting on a
    /// frame that has already returned. That was inert when a frame was used once and thrown
    /// away; with recycling it is not, because the corpse can be reissued while it is still
    /// the top, and the very next call would then link itself under <em>itself</em>. The
    /// resulting one-element cycle passes any stamp check (a frame's own stamp always matches)
    /// and hangs every walker (test262 built-ins/AsyncGeneratorPrototype, intermittently, in
    /// TryResolveDirectEvalBinding).
    /// </para>
    /// <para>
    /// Treating a dead top as "no caller" reproduces what the pre-pooling engine reported,
    /// since <see cref="Pop"/> had already nulled that frame's own Parent — a walk that reached
    /// it stopped there.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LinkParent(IJSExecutionContext context)
    {
        var parent = context.Top;
        if (parent != null && parent.released)
            parent = null;

        released = false;
        stamp++;
        Parent = parent;
        parentStamp = parent?.stamp ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CallStackItem Rent()
    {
        var s = store;
        var item = s?.Head;
        if (item == null)
            return new CallStackItem();

        s.Head = item.poolNext;
        s.Count--;
        item.poolNext = null;
        return item;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static CallStackItem Enter(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
        => Rent().Push(context, scriptInfo, nameOffset, nameLength, line, column);

    /// <summary>
    /// Enters a generator or async body — one that is pushed once, at priming, and then
    /// leaves the stack at every suspension without being popped. See <see cref="PinSuspendable"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static CallStackItem EnterSuspendable(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
        => Rent().Push(context, scriptInfo, nameOffset, nameLength, line, column).PinSuspendable();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static CallStackItem EnterScope(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
        => Rent().Push(context, fileName, in function, line, column);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static CallStackItem EnterSuspendableScope(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
        => Rent().Push(context, fileName, in function, line, column).PinSuspendable();

    /// <summary>
    /// Keeps a generator or async body's own frame out of the pool permanently.
    /// <para>
    /// It is the one frame whose lifetime is not a synchronous span: it is rented when the
    /// body is primed and released whenever the body finally completes, which for an async
    /// body is a continuation the runtime may run on another thread. The pool is thread-local,
    /// so such a frame would be rented from one list and handed back to another, with only
    /// plain (non-atomic) state guarding release-once. Ordinary frames never do this — they
    /// are rented and released inside one synchronous span on one thread.
    /// </para>
    /// <para>
    /// The staleness this creates in <em>other</em> frames' links is a separate problem, and
    /// is handled by stamping rather than by pinning: see <see cref="Caller"/>. Re-pointing a
    /// suspended body's parent on resumption is not the fix either, and was tried — an async
    /// body resumes from a microtask continuation whose current top is unrelated to its
    /// caller, so re-linking splices it into a foreign chain and <see cref="Pop"/> then
    /// restores that foreign frame as the top (test262 language/expressions/async-generator).
    /// </para>
    /// <para>The cost is one un-recycled frame per generator instance, against zero per ordinary call.</para>
    /// </summary>
    private CallStackItem PinSuspendable()
    {
        pinned = true;
        return this;
    }

    private CallStackItem Push(IJSExecutionContext context, ScriptInfo scriptInfo, int nameOffset, int nameLength, int line, int column)
    {
        context = ResolveContext(context);
        context.EnsureSufficientExecutionStack();
        this.context = context;
        var ctx = context.CurrentNewTarget;

        // Assigned unconditionally: a recycled frame must not inherit the new.target of
        // whatever call last used it.
        NewTarget = ctx;
        if (ctx != null)
            context.CurrentNewTarget = null;

        FileName = scriptInfo.FileName;
        Function = IsValidFunctionSpan(scriptInfo?.Code, nameOffset, nameLength)
            ? new StringSpan(scriptInfo.Code, nameOffset, nameLength)
            : Inline;
        Line = line;
        Column = column;
        directEvalBindings = null;
        LinkParent(context);
        context.Top = this;
        return this;
    }

    private CallStackItem Push(IJSExecutionContext context, string fileName, in StringSpan function, int line, int column)
    {
        context = ResolveContext(context);
        context.EnsureSufficientExecutionStack();
        this.context = context;
        NewTarget = null;
        FileName = fileName;
        Function = function;
        Line = line;
        Column = column;
        directEvalBindings = null;
        LinkParent(context);
        context.Top = this;
        return this;
    }

    public CallStackItem Parent;
    public JSValue NewTarget;
    public StringSpan Function;
    public int Line;
    public int Column;
    private IJSExecutionContext context;
    public string FileName;
    private Dictionary<uint, JSVariable> directEvalBindings;

    internal bool TryGetDirectEvalBinding(in KeyString name, out JSVariable variable)
    {
        if (directEvalBindings?.TryGetValue(name.Key, out variable) == true)
            return true;

        variable = null;
        return false;
    }

    internal void RegisterDirectEvalBinding(JSVariable variable)
    {
        if (variable == null || variable.Name.IsEmpty)
            return;

        directEvalBindings ??= [];
        var key = KeyStrings.GetOrCreate(variable.Name);
        directEvalBindings[key.Key] = variable;
    }

    internal bool DeleteDirectEvalBinding(in KeyString name)
        => directEvalBindings?.Remove(name.Key) == true;

    public void Update() => System.Diagnostics.Debug.WriteLine($"{Function} at {Line}, {Column}");

    public void Step(int line, int column)
    {
        var ec = context;
        if (ec != null)
        {
            // A generator or async body is pushed once, at priming, and then leaves the
            // stack at every suspension without being popped — so on resumption its
            // recorded Parent is whatever was running when the body first started, which
            // has long since returned. Re-link to the frame that is actually calling us,
            // which both reports the real caller in a trace taken inside a resumed body
            // and — now that frames are recycled — keeps the chain from pointing into a
            // frame that has been handed to somebody else. An ordinary function is
            // already the top when it steps, so this costs one compare and leaves its
            // Parent (and, importantly, never self-links it) alone.
            ec.Top = this;
        }

        Line = line;
        Column = column;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop(IJSExecutionContext context)
    {
        // A frame is released exactly once. Re-popping an already-released frame would
        // otherwise restore a Top belonging to whichever call now owns it.
        if (released)
            return;

        context = context ?? this.context ?? JSEngine.Current as IJSExecutionContext;
        if (context == null)
            throw MissingExecutionContext();

        directEvalBindings = null;
        context.Top = Parent;
        Parent = null;
        released = true;

        if (!pinned)
        {
            var s = store ??= new FrameStore();
            if (s.Count < MaxPooledFrames)
            {
                poolNext = s.Head;
                s.Head = this;
                s.Count++;
            }
        }
    }

    public override string ToString() => $"{Function} at {FileName} - {Line},{Column}";

    private static IJSExecutionContext ResolveContext(IJSExecutionContext context)
    {
        context ??= JSEngine.Current as IJSExecutionContext;
        return context ?? throw MissingExecutionContext();
    }

    private static InvalidOperationException MissingExecutionContext()
        => new("Cannot enter JavaScript execution without an active JS execution context.");

    private static bool IsValidFunctionSpan(string code, int offset, int length)
        => code != null
            && offset >= 0
            && length > 0
            && offset <= code.Length
            && length <= code.Length - offset;
}
