using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Broiler.JavaScript.ExpressionCompiler;

/// <summary>
/// Runs one compilation on a thread whose stack size the engine chooses, so how deeply
/// nested a script the front end accepts is a number this repository sets rather than
/// whatever stack the host thread happened to be created with.
/// </summary>
/// <remarks>
/// The scanner, the parser, the syntax validator, the compiler's visitors and the IL
/// emitter all recurse over nested source, so the depth the front end survives is a
/// function of the stack it is handed — 1 MiB on a Windows main thread, 8 MiB on a
/// typical Linux one, and not knowable from managed code. Past that depth the process
/// does not fail, it *aborts*: a stack overflow is not a catchable exception on .NET, so
/// a syntactically valid script takes the host down with it and no <c>try</c> around the
/// compile call can help.
///
/// This is the compile-time counterpart of the script host sizing its own execution
/// thread, and it is deliberately not the same budget as
/// <c>JSContextOptions.MaxStackUsageBytes</c>. JavaScript recursion depth is observable
/// to the program — it is the `RangeError` a script can catch — whereas compiler
/// recursion depth is not, so raising the second changes no program's meaning. Keeping
/// them separate is also what stops one from being tuned for the other: a script whose
/// *source* nests deeply is not a script that *recurses* deeply, and today the first is
/// charged against the second's budget.
///
/// Blocking on the handoff is what keeps this a stack change and nothing else. The
/// compilation is not concurrent with its caller — exactly one of the two threads runs at
/// a time — ambient state reaches it through an execution context captured per
/// compilation, and the caller sees the original exception, with its original stack,
/// through <see cref="ExceptionDispatchInfo"/>.
///
/// This is a mitigation, not the fix: it moves the ceiling and makes it a stated number
/// instead of a host accident. The fix is for the front end's stack depth to follow
/// source *nesting* rather than source *size*, which needs an explicit worklist in the
/// passes that recurse without bound. <see cref="StackGuard{T, TIn}"/> is the start of
/// that fix and does not currently work — it compares stack addresses as though the stack
/// grew upwards, so its segmenting branch is unreachable. Repairing it is the item this
/// class buys time for, not one it removes.
/// </remarks>
public static class CompilationStack
{
    /// <summary>
    /// Stack size handed to a compilation thread when the host has not chosen one.
    /// </summary>
    /// <remarks>
    /// Four times the 16 MiB the script host gives JavaScript execution. Thread stacks
    /// are reserved address space and commit only as they are used, so an ordinary
    /// compilation pays nothing for the headroom; what the number bounds is how much a
    /// genuinely runaway recursion can commit before it dies, which is why this is tens
    /// of megabytes and not hundreds.
    /// </remarks>
    public const int DefaultSizeBytes = 64 * 1024 * 1024;

    /// <summary>
    /// Environment override for <see cref="SizeBytes"/>, read once. Zero opts out entirely.
    /// Present so the cost of the extra stack can be measured against a build that is
    /// otherwise identical — comparing two builds cannot separate this from anything else
    /// that changed — and so an operator can turn it off without one.
    /// </summary>
    public const string SizeEnvironmentVariable = "BROILER_JS_COMPILE_STACK_BYTES";

    private static int sizeBytes = ReadConfiguredSize();

    /// <summary>
    /// Stack size, in bytes, for the thread a compilation runs on. Zero compiles on the
    /// calling thread instead, which is how a host that has already sized its own thread
    /// (or one that cannot afford another) opts out.
    /// </summary>
    public static int SizeBytes
    {
        get => sizeBytes;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            sizeBytes = value;
        }
    }

    private static int ReadConfiguredSize()
    {
        var configured = Environment.GetEnvironmentVariable(SizeEnvironmentVariable);

        // An unparseable or negative value is the default, not an error: a compilation is
        // the wrong place to report a malformed environment, and refusing to compile would
        // be a far worse outcome than ignoring the setting.
        return int.TryParse(configured, out var value) && value >= 0
            ? value
            : DefaultSizeBytes;
    }

    /// <summary>
    /// Source length, in characters, below which a compilation is left on the calling
    /// thread.
    /// </summary>
    /// <remarks>
    /// This is a bound, not a guess: nesting depth cannot exceed source length, since every
    /// level costs at least one character. At the ~850 bytes per level measured on this
    /// engine, this many levels is about 435 KB — so a source this short cannot exhaust even
    /// a 1 MiB Windows main thread, and offloading it would buy nothing while charging a
    /// thread handoff to the smallest and most frequent compilations there are (`eval` of
    /// one expression, the wrapper the Function constructor compiles, an accessor body).
    /// Those are precisely where the handoff is not affordable: it is a fixed ~180 µs
    /// against a compile that may be shorter than that, whereas a source large enough to
    /// need the stack takes long enough to compile that the handoff is unmeasurable.
    /// </remarks>
    public const int InlineSourceLengthLimit = 512;

    [ThreadStatic]
    private static bool inCompilation;

    /// <summary>
    /// Whether a compilation boundary is already established on the calling thread, so a
    /// nested request needs no second one. True on a thread this class created, and also on
    /// a caller whose compilation was short enough to be assessed as safe where it stood.
    /// </summary>
    public static bool IsInCompilation => inCompilation;

    // Parked workers, reused across compilations. Reserving a large stack is a kernel
    // mapping, not a bookkeeping entry: measured on this engine, a thread per compilation
    // costs ~300 µs at the default size, which is 27% of a compile-bound workload — the
    // front end this exists to protect is also the one phase 1 is trying to make faster.
    // Renting amortizes the mapping over every compilation the process ever runs, leaving
    // two semaphore handoffs. The bag is bounded in practice by how many threads compile
    // at once, since a worker is only ever rented by one caller at a time.
    private static readonly ConcurrentBag<Worker> idle = new();

    /// <summary>Runs <paramref name="compile"/> on a thread sized by <see cref="SizeBytes"/>.</summary>
    public static T Run<T>(Func<T> compile) => Run(compile, sourceLength: -1);

    /// <summary>
    /// Runs <paramref name="work"/> on a worker stack <em>even when a compilation boundary is
    /// already established on this thread</em>, which is what a recursion segmenter needs and
    /// <see cref="Run{T}(Func{T})"/> deliberately will not do.
    /// </summary>
    /// <remarks>
    /// <see cref="Run{T}(Func{T}, int)"/> returns inline while <see cref="IsInCompilation"/> is
    /// true, because a nested compilation is already inside a boundary and a second thread would
    /// deepen nothing. <see cref="StackGuard{T, TIn}"/> wants the opposite: it fires precisely
    /// *because* the current stack is running out, and it is always inside a compilation when it
    /// does, so routing it through <c>Run</c> would inline it and segment nothing. This is that
    /// one exception, kept as its own entry point so the ordinary rule stays a rule.
    /// <para>
    /// A worker's stack is a fresh mapping each time it is created, and a parked one is reused,
    /// so repeated segmentation costs a pair of semaphore handoffs rather than a thread per hop.
    /// </para>
    /// </remarks>
    public static T RunOnFreshStack<T>(Func<T> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Deliberately independent of the boundary's own opt-out. Setting
        // BROILER_JS_COMPILE_STACK_BYTES to 0 turns off the *mitigation* — compiling on a thread
        // the engine sizes — and item 1-2 is explicit that the segmenter is the *real fix*, not
        // part of it. A segmenter with nowhere to go is not a segmenter, so it falls back to the
        // default size; disabling it is BROILER_JS_VISITOR_SEGMENT_BYTES's job. Keeping the two
        // separate is also what lets the deeply-nested fixtures prove which mechanism saved them.
        var size = sizeBytes != 0 ? sizeBytes : DefaultSizeBytes;

        if (!idle.TryTake(out var worker) || worker.StackSize != size)
        {
            worker?.Retire();
            worker = new Worker(size);
        }

        try
        {
            return worker.Run(work);
        }
        finally
        {
            if (worker.StackSize == size)
                idle.Add(worker);
            else
                worker.Retire();
        }
    }

    /// <summary>
    /// Runs <paramref name="compile"/> on a thread sized by <see cref="SizeBytes"/>, unless
    /// <paramref name="sourceLength"/> is short enough to be safe where it is. Pass -1 when
    /// the source length is not known at the call site.
    /// </summary>
    public static T Run<T>(Func<T> compile, int sourceLength)
    {
        ArgumentNullException.ThrowIfNull(compile);

        var size = sizeBytes;

        // A nested compilation — a direct eval reached while compiling, an emit that follows
        // the parse it belongs to — is already inside a boundary, so it runs inline. Handing
        // it a second thread would deepen nothing and cost a handoff.
        if (size == 0 || inCompilation)
            return compile();

        // Assessed as safe here, but still marked: the emit that follows this parse, and any
        // boundary further in, must reach the same conclusion rather than each re-deciding
        // and one of them offloading anyway.
        if (sourceLength >= 0 && sourceLength <= InlineSourceLengthLimit)
        {
            inCompilation = true;
            try
            {
                return compile();
            }
            finally
            {
                inCompilation = false;
            }
        }

        if (!idle.TryTake(out var worker) || worker.StackSize != size)
        {
            worker?.Retire();
            worker = new Worker(size);
        }

        try
        {
            return worker.Run(compile);
        }
        finally
        {
            // Returned even when the compilation threw: the worker's stack is unwound by
            // then, and a SyntaxError is an ordinary outcome of compiling, not damage.
            if (worker.StackSize == sizeBytes)
                idle.Add(worker);
            else
                worker.Retire();
        }
    }

    private sealed class Worker
    {
        private readonly SemaphoreSlim requested = new(0, 1);
        private readonly SemaphoreSlim completed = new(0, 1);

        private Func<object> work;
        private object result;
        private ExceptionDispatchInfo failure;
        private volatile bool retired;

        internal readonly int StackSize;

        internal Worker(int stackSize)
        {
            StackSize = stackSize;
            var thread = new Thread(Loop, stackSize)
            {
                // Parked between compilations, so it must never hold up process exit.
                IsBackground = true,
                Name = "broiler-js-compile",
            };

            // UnsafeStart, so the worker does not spend its whole life under the execution
            // context of whichever compilation happened to create it. Each request supplies
            // its own (see Run); a request that has none should see an empty context, not a
            // stale one belonging to a caller that returned long ago.
            thread.UnsafeStart();
        }

        internal T Run<T>(Func<T> compile)
        {
            // Captured per compilation, not once when the thread was created: the ambient
            // JavaScript context and strict-mode flag live in AsyncLocal, and a parked
            // worker would otherwise compile under whichever context happened to be current
            // when it was first rented. Thread.Start's own capture is no help here — it
            // happens once, and this thread outlives the call it was created for.
            var context = ExecutionContext.Capture();
            work = context == null
                ? () => compile()
                : () =>
                {
                    object captured = null;
                    ExecutionContext.Run(context, _ => captured = compile(), null);
                    return captured;
                };

            requested.Release();
            completed.Wait();

            var thrown = failure;
            var value = result;
            failure = null;
            result = null;
            thrown?.Throw();
            return (T)value;
        }

        internal void Retire()
        {
            retired = true;
            requested.Release();
        }

        private void Loop()
        {
            inCompilation = true;

            while (true)
            {
                requested.Wait();
                if (retired)
                    return;

                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    // Captured rather than wrapped so a SyntaxError stays a SyntaxError:
                    // CoreScript.Compile's own handler, and every embedder's, matches on the
                    // exception type.
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    work = null;
                    completed.Release();
                }
            }
        }
    }
}
