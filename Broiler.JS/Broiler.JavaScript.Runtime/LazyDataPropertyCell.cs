using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Per-realm value cell for a real JavaScript data property. The cell contains no
/// satellite delegate, registrar, <see cref="Type"/>, or assembly reference.
/// </summary>
public sealed class LazyDataPropertyCell : IPropertyValue
{
    private const int Uninitialized = 0;
    private const int Initializing = 1;
    private const int Realized = 2;
    private const int Faulted = 3;
    private const int Canceled = 4;

    private readonly object sync = new();
    private readonly IJSFeatureResolver resolver;
    private readonly BuiltInFeatureId feature;
    private int state;
    private int initializingThreadId;
    private JSValue value;
    private ExceptionDispatchInfo failure;

    public LazyDataPropertyCell(IJSFeatureResolver resolver, BuiltInFeatureId feature)
    {
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.feature = feature;
        StartupOptimizationDiagnostics.RecordLazyCellCreated();
    }

    public BuiltInFeatureId Feature => feature;
    public bool IsRealized => Volatile.Read(ref state) == Realized;
    public bool IsCanceled => Volatile.Read(ref state) == Canceled;

    public JSValue Resolve()
    {
        var observed = Volatile.Read(ref state);
        if (observed == Realized)
            return value;
        if (observed == Canceled)
            return JSUndefined.Value;
        if (observed == Faulted)
        {
            failure.Throw();
            throw new InvalidOperationException();
        }

        lock (sync)
        {
            while (state == Initializing)
            {
                if (initializingThreadId == Environment.CurrentManagedThreadId)
                    throw new InvalidOperationException($"Recursive lazy initialization of built-in feature '{feature}'.");
                Monitor.Wait(sync);
            }

            if (state == Realized)
                return value;
            if (state == Canceled)
                return JSUndefined.Value;
            if (state == Faulted)
            {
                failure.Throw();
                throw new InvalidOperationException();
            }

            state = Initializing;
            initializingThreadId = Environment.CurrentManagedThreadId;
        }

        try
        {
            var resolved = resolver.ResolveBuiltInFeature(feature) ?? JSUndefined.Value;
            lock (sync)
            {
                value = resolved;
                initializingThreadId = 0;
                state = Realized;
                Monitor.PulseAll(sync);
            }
            StartupOptimizationDiagnostics.RecordLazyCellRealized();
            return resolved;
        }
        catch (Exception ex)
        {
            lock (sync)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
                initializingThreadId = 0;
                state = Faulted;
                Monitor.PulseAll(sync);
            }
            StartupOptimizationDiagnostics.RecordLazyCellFailure();
            throw;
        }
    }

    internal void Cancel()
    {
        lock (sync)
        {
            if (state != Uninitialized)
                return;
            state = Canceled;
        }
        StartupOptimizationDiagnostics.RecordLazyCellCanceled();
    }
}
