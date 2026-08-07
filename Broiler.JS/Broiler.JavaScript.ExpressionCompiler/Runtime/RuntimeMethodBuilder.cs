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
