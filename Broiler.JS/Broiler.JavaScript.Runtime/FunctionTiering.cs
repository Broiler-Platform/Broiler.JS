using System;
using System.ComponentModel;
using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>Per-realm limits for opt-in hot-function recompilation.</summary>
public sealed class FunctionTieringOptions
{
    public static FunctionTieringOptions Disabled { get; } = new();

    public bool Enabled { get; init; }
    public int InvocationThreshold { get; init; } = 64;
    public int MaxRecompilations { get; init; } = 32;
    public long MaxRetainedCodeBytes { get; init; } = 4L * 1024 * 1024;

    internal void Validate()
    {
        if (!Enabled)
            return;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(InvocationThreshold);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxRecompilations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxRetainedCodeBytes);
    }
}

public readonly record struct FunctionTieringSnapshot(
    long Candidates,
    long Invocations,
    long RecompilationAttempts,
    long Recompilations,
    long RecompilationFailures,
    long BudgetRejections,
    long DelegateReplacements,
    long Deoptimizations,
    long RetainedCodeBytes);

/// <summary>
/// Owns one realm's tiering budget. Successful promoted delegates remain bounded by
/// both a count and an estimated retained-code limit.
/// </summary>
public sealed class FunctionTieringController
{
    private readonly FunctionTieringOptions options;
    private readonly object budgetGate = new();
    private long candidates;
    private long invocations;
    private long recompilationAttempts;
    private long recompilations;
    private long recompilationFailures;
    private long budgetRejections;
    private long delegateReplacements;
    private long deoptimizations;
    private long retainedCodeBytes;

    public FunctionTieringController(FunctionTieringOptions options = null)
    {
        this.options = options ?? FunctionTieringOptions.Disabled;
        this.options.Validate();
    }

    public bool Enabled => options.Enabled;

    public FunctionTieringSnapshot Snapshot() => new(
        Interlocked.Read(ref candidates),
        Interlocked.Read(ref invocations),
        Interlocked.Read(ref recompilationAttempts),
        Interlocked.Read(ref recompilations),
        Interlocked.Read(ref recompilationFailures),
        Interlocked.Read(ref budgetRejections),
        Interlocked.Read(ref delegateReplacements),
        Interlocked.Read(ref deoptimizations),
        Interlocked.Read(ref retainedCodeBytes));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public FunctionTieringState CreateState(long estimatedCodeBytes, Func<JSFunctionDelegate> recompile)
    {
        if (!Enabled || recompile == null)
            return null;

        Interlocked.Increment(ref candidates);
        return new FunctionTieringState(this, recompile, Math.Max(256, estimatedCodeBytes));
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RecordDeoptimization() => Interlocked.Increment(ref deoptimizations);

    internal int InvocationThreshold => options.InvocationThreshold;

    internal void RecordInvocation() => Interlocked.Increment(ref invocations);

    internal bool TryReserve(long estimatedCodeBytes)
    {
        lock (budgetGate)
        {
            if (recompilationAttempts >= options.MaxRecompilations
                || retainedCodeBytes + estimatedCodeBytes > options.MaxRetainedCodeBytes)
            {
                budgetRejections++;
                return false;
            }

            recompilationAttempts++;
            retainedCodeBytes += estimatedCodeBytes;
            return true;
        }
    }

    internal void RecordSuccess()
    {
        Interlocked.Increment(ref recompilations);
        Interlocked.Increment(ref delegateReplacements);
    }

    internal void RecordFailure(long estimatedCodeBytes)
    {
        Interlocked.Increment(ref recompilationFailures);
        lock (budgetGate)
            retainedCodeBytes -= estimatedCodeBytes;
    }
}

/// <summary>Thread-safe promotion state attached to one eligible function object.</summary>
public sealed class FunctionTieringState
{
    private readonly FunctionTieringController controller;
    private readonly Func<JSFunctionDelegate> recompile;
    private readonly long estimatedCodeBytes;
    private int invocationCount;
    private int state;

    internal FunctionTieringState(
        FunctionTieringController controller,
        Func<JSFunctionDelegate> recompile,
        long estimatedCodeBytes)
    {
        this.controller = controller;
        this.recompile = recompile;
        this.estimatedCodeBytes = estimatedCodeBytes;
    }

    public int InvocationCount => Volatile.Read(ref invocationCount);
    public bool IsPromoted => Volatile.Read(ref state) == 2;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSFunctionDelegate ObserveInvocation()
    {
        controller.RecordInvocation();
        if (Interlocked.Increment(ref invocationCount) < controller.InvocationThreshold
            || Volatile.Read(ref state) != 0
            || Interlocked.CompareExchange(ref state, 1, 0) != 0)
        {
            return null;
        }

        if (!controller.TryReserve(estimatedCodeBytes))
        {
            Volatile.Write(ref state, 3);
            return null;
        }

        try
        {
            var optimized = recompile();
            if (optimized == null)
                throw new InvalidOperationException("The tiering compiler returned no delegate.");

            controller.RecordSuccess();
            Volatile.Write(ref state, 2);
            return optimized;
        }
        catch
        {
            controller.RecordFailure(estimatedCodeBytes);
            Volatile.Write(ref state, 3);
            return null;
        }
    }
}

public enum NumericLoopComparison
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
}

/// <summary>
/// Compiler-produced plan for the deliberately small Phase 5 counted-reduction pilot.
/// The promoted delegate keeps its accumulator and induction variable as unboxed doubles.
/// </summary>
public sealed class NumericLoopPlan
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static NumericLoopPlan Create(
        int limitArgumentIndex,
        double accumulatorInitialValue,
        double inductionInitialValue,
        double inductionStep,
        int comparison,
        double termScale,
        double termOffset)
        => new(
            limitArgumentIndex,
            accumulatorInitialValue,
            inductionInitialValue,
            inductionStep,
            (NumericLoopComparison)comparison,
            termScale,
            termOffset);

    public NumericLoopPlan(
        int limitArgumentIndex,
        double accumulatorInitialValue,
        double inductionInitialValue,
        double inductionStep,
        NumericLoopComparison comparison,
        double termScale = 1,
        double termOffset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limitArgumentIndex);
        if (inductionStep == 0 || double.IsNaN(inductionStep))
            throw new ArgumentOutOfRangeException(nameof(inductionStep));

        LimitArgumentIndex = limitArgumentIndex;
        AccumulatorInitialValue = accumulatorInitialValue;
        InductionInitialValue = inductionInitialValue;
        InductionStep = inductionStep;
        Comparison = comparison;
        TermScale = termScale;
        TermOffset = termOffset;
    }

    public int LimitArgumentIndex { get; }
    public double AccumulatorInitialValue { get; }
    public double InductionInitialValue { get; }
    public double InductionStep { get; }
    public NumericLoopComparison Comparison { get; }
    public double TermScale { get; }
    public double TermOffset { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JSFunctionDelegate Compile(JSFunctionDelegate baseline, Action deoptimize)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(deoptimize);

        var comparison = Comparison;
        var accumulatorInitialValue = AccumulatorInitialValue;
        var inductionInitialValue = InductionInitialValue;
        var inductionStep = InductionStep;
        var termScale = TermScale;
        var termOffset = TermOffset;

        return (in Arguments arguments) =>
        {
            if (LimitArgumentIndex >= arguments.Length)
            {
                deoptimize();
                return baseline(in arguments);
            }

            var limitValue = arguments.GetAt(LimitArgumentIndex);
            if (!limitValue.IsNumber)
            {
                deoptimize();
                return baseline(in arguments);
            }

            var limit = limitValue.DoubleValue;
            // The canonical integer sum has an exact closed form while its result
            // remains within Number's integer precision. Keeping this guard this
            // narrow avoids reassociation for general floating-point reductions.
            if (comparison == NumericLoopComparison.LessThan
                && accumulatorInitialValue == 0
                && inductionInitialValue == 0
                && inductionStep == 1
                && termScale == 1
                && termOffset == 0
                && limit >= 0
                && limit <= 134_217_728
                && limit == Math.Truncate(limit))
            {
                var left = (long)limit;
                if (left == 0)
                    return JSValue.CreateNumber(accumulatorInitialValue);
                var right = left - 1;
                if ((left & 1) == 0)
                    left /= 2;
                else
                    right /= 2;
                return JSValue.CreateNumber((double)left * right);
            }

            var accumulator = accumulatorInitialValue;
            var induction = inductionInitialValue;
            switch (comparison)
            {
                case NumericLoopComparison.LessThan:
                    while (induction < limit)
                    {
                        accumulator += induction * termScale + termOffset;
                        induction += inductionStep;
                    }
                    break;
                case NumericLoopComparison.LessThanOrEqual:
                    while (induction <= limit)
                    {
                        accumulator += induction * termScale + termOffset;
                        induction += inductionStep;
                    }
                    break;
                case NumericLoopComparison.GreaterThan:
                    while (induction > limit)
                    {
                        accumulator += induction * termScale + termOffset;
                        induction += inductionStep;
                    }
                    break;
                case NumericLoopComparison.GreaterThanOrEqual:
                    while (induction >= limit)
                    {
                        accumulator += induction * termScale + termOffset;
                        induction += inductionStep;
                    }
                    break;
                default:
                    deoptimize();
                    return baseline(in arguments);
            }

            return JSValue.CreateNumber(accumulator);
        };
    }
}
