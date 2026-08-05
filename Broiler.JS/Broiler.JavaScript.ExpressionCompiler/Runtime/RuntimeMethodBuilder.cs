using System;
using System.Reflection;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;


public class RuntimeMethodBuilder(
    IMethodRepository methods,
    bool captureDiagnostics = false,
    bool enableJavaScriptTailCalls = false) : IMethodBuilder
{
    private static Type type = typeof(IMethodRepository);

    private static MethodInfo create = type.GetMethod(nameof(IMethodRepository.Create));


    public BExpression Relay(BExpression @this, IFastEnumerable<BExpression> closures, BLambdaExpression innerLambda)
    {
        // The closure rewrite decides which variables this lambda captures and boxes them, and
        // the boxes are referenced by the creation-site expression built below — so it has to
        // have happened before the site is built. It does not have to happen *here*: the walk
        // that emitted the enclosing lambda descended through this one already, because
        // CheckForClosure is what threads a capture up the whole lambda chain and it can only do
        // that from a walk that sees the chain. Rewriting again from this node re-walks the
        // entire subtree and changes nothing.
        //
        // The cost is not the second walk, it is the nth: relay happens once per lambda per
        // enclosing emission, so a lambda at depth d was walked d+1 times. It is also what
        // item 1-1's deferral could not remove — the IL of a nested body is postponed to first
        // invocation, and then this walked its whole subtree at the moment of deferring it.
        //
        // Only a lambda a descending walk actually entered is skipped; anything built after that
        // walk (a generator or async body rewritten into a state machine, a lambda a later pass
        // synthesizes) has the flag clear and is rewritten here as before.
        if (!innerLambda.ClosureRewritten)
        {
            ClosureRewriteDiagnostics.Rewrote();
            LambdaRewriter.Rewrite(innerLambda);
        }
        else if (ClosureRewriteDiagnostics.SkipRewrittenRelays)
        {
            ClosureRewriteDiagnostics.Skipped();
        }
        else
        {
            // The switch is off, so the repeat still runs — and this is where it is *measured*.
            // "Every relay is a repeat" is what the two counters above establish; whether a
            // repeat does anything is a different claim, and the only instrument that answers it
            // is a count of the captures the repeat creates. Over the corpus it is zero, which
            // is what makes the skip a removal of repeated work rather than of work.
            ClosureRewriteDiagnostics.Skipped();
            ClosureRewriteDiagnostics.BeginRepeatWalk();
            try
            {
                LambdaRewriter.Rewrite(innerLambda);
            }
            finally
            {
                ClosureRewriteDiagnostics.EndRepeatWalk();
            }
        }

        // Item 1-1's remaining half: the rewrite above has just decided this lambda's Box[]
        // layout FROM THE TREE. If the front end recorded what it predicted from source alone,
        // this is the only place the two can be compared — before it the truth does not exist,
        // and after it the tree is gone.
        if (DeferredCaptureLayout.Checking)
            DeferredCaptureLayout.Check(innerLambda, ClosureRepository.For(innerLambda));

        var repository = BExpression.Field(@this, Closures.repositoryField);
        var id = methods is MethodRepository runtimeMethods
            && DeferredMethod.CanDefer(innerLambda, captureDiagnostics)
            ? runtimeMethods.RegisterDeferred(
                new DeferredMethod(innerLambda, this, enableJavaScriptTailCalls),
                innerLambda.Type)
            : RegisterGenerated(innerLambda);

        return BExpression.Call(repository, create, closures == null ? BExpression.Null : BExpression.NewArray(typeof(Box), closures), BExpression.Constant(id));
    }

    private ulong RegisterGenerated(BLambdaExpression innerLambda)
    {
        var (method, il, exp) = innerLambda.CompileToBoundDynamicMethod(
            methodBuilder: this,
            captureDiagnostics: captureDiagnostics,
            enableJavaScriptTailCalls: enableJavaScriptTailCalls);
        return methods.RegisterNew(method, il, exp, innerLambda.Type);
    }
}

/// <summary>
/// Switch and counters for skipping a relay-time closure rewrite that an enclosing walk has
/// already performed.
/// </summary>
/// <remarks>
/// <para>
/// The switch exists for the reason every other one in this campaign does: the change has to be
/// measurable against a build that differs in nothing else. Setting
/// <c>BROILER_JS_RELAY_REWRITE_ONCE=0</c> restores the unconditional rewrite.
/// </para>
/// <para>
/// The counters are touched once per relayed site, never per call, so they are off every hot
/// path by construction and need no enable flag of their own. <c>Rewrote</c> is the population
/// the skip cannot reach — a lambda no descending walk had entered — and reading it is how the
/// claim "the relay-time walk is a repeat" is checked rather than asserted.
/// </para>
/// </remarks>
public static class ClosureRewriteDiagnostics
{
    public const string EnvironmentVariable = "BROILER_JS_RELAY_REWRITE_ONCE";

    private static int skipRewritten = ReadConfigured();

    private static long rewrote;
    private static long skipped;
    private static long capturesInRepeat;

    [ThreadStatic]
    private static bool inRepeatWalk;

    /// <summary>Whether a relay skips a rewrite an enclosing walk already did.</summary>
    public static bool SkipRewrittenRelays
    {
        get => Volatile.Read(ref skipRewritten) != 0;
        set => Volatile.Write(ref skipRewritten, value ? 1 : 0);
    }

    /// <summary>Relayed sites whose subtree this rewrote.</summary>
    public static long RewroteRelays => Interlocked.Read(ref rewrote);

    /// <summary>Relayed sites whose subtree an enclosing walk had already rewritten.</summary>
    public static long SkippedRelays => Interlocked.Read(ref skipped);

    /// <summary>
    /// Captures a relay-time rewrite of an already-rewritten lambda created. Only ever non-zero
    /// with <see cref="SkipRewrittenRelays"/> off, since that is the only arm that runs one.
    /// </summary>
    public static long CapturesCreatedInRepeatWalk => Interlocked.Read(ref capturesInRepeat);

    internal static void Rewrote() => Interlocked.Increment(ref rewrote);

    internal static void Skipped() => Interlocked.Increment(ref skipped);

    internal static void BeginRepeatWalk() => inRepeatWalk = true;

    internal static void EndRepeatWalk() => inRepeatWalk = false;

    internal static void CaptureCreated()
    {
        if (inRepeatWalk)
            Interlocked.Increment(ref capturesInRepeat);
    }

    public static void Reset()
    {
        Interlocked.Exchange(ref rewrote, 0);
        Interlocked.Exchange(ref skipped, 0);
        Interlocked.Exchange(ref capturesInRepeat, 0);
    }

    private static int ReadConfigured()
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "0",
            StringComparison.Ordinal)
            ? 0
            : 1;
}
