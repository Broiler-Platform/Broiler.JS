#nullable enable
using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.ExpressionCompiler;

public static class ClosureRepositoryExtensions
{
    public static ClosureRepository GetClosureRepository(this BLambdaExpression lambda) => ClosureRepository.For(lambda);
}

public class ClosureRepository
{
    private static System.Runtime.CompilerServices.ConditionalWeakTable<BLambdaExpression, ClosureRepository> cache =
        [];

    public readonly Dictionary<BParameterExpression, (BParameterExpression local, BExpression value, int index, int argIndex)>
        Closures = new(Core.ReferenceEqualityComparer.Instance);

    public List<BParameterExpression> Inputs 
        = [];

    private BLambdaExpression lambda;

    protected ClosureRepository(BLambdaExpression lambda) => this.lambda = lambda;

    public static ClosureRepository For(BLambdaExpression lambda)
    {
        if (cache.TryGetValue(lambda, out var value))
            return value;
        value = new ClosureRepository(lambda);
        cache.Add(lambda, value);
        return value;
    }

    internal bool TryGet(BParameterExpression pe, out BExpression exp)
    {
        if (Closures.TryGetValue(pe, out var ve))
        {
            exp = ve.value;
            return true;
        }
        exp = default!;
        return false;
    }

    internal BParameterExpression Setup(BParameterExpression pe, Func<BParameterExpression> source)
    {
        if (Closures.TryGetValue(pe, out var value))
            return value.local;
        var s = source();
        bool isBox = typeof(Box).IsAssignableFrom(pe.Type);
        var boxType = isBox ? pe.Type : BoxHelper.For(pe.Type).BoxType;
        var converted = BExpression.Parameter(boxType, pe.Name + "`");
        BExpression valueField = isBox ? converted : BExpression.Field(converted, "Value");
        Closures[pe] = (converted, valueField, Inputs.Count, -1);
        Inputs.Add(s);
        return converted;
    }

    internal BParameterExpression Convert(BParameterExpression pe)
    {
        if (Closures.TryGetValue(pe, out var value))
            return value.local;
        bool isBox = typeof(Box).IsAssignableFrom(pe.Type);
        var boxType = isBox ? pe.Type : BoxHelper.For(pe.Type).BoxType;
        var converted = BExpression.Parameter(boxType, pe.Name + "`");
        BExpression valueField = isBox ? converted : BExpression.Field(converted, "Value");
        var argIndex = Array.IndexOf(lambda.Parameters, pe);
        Closures[pe] = (converted, valueField, -1, argIndex);
        return converted;
    }
}


public class LambdaRewriter: BExpressionMapVisitor
{
    public class Scope
    {
        public readonly BLambdaExpression Root;
        public readonly List<BParameterExpression> Variables = [];

        public Scope(BLambdaExpression exp)
        {
            Root = exp;
            Variables.AddRange(exp.Parameters);
        }

        public static implicit operator Scope(BLambdaExpression e) => new(e);

        internal IDisposable Register(IFastEnumerable<BParameterExpression> variables)
        {
            Variables.AddRange(variables);
            return new DisposableAction(() => {
                var ve = variables.GetFastEnumerator();
                while(ve.MoveNext(out var v))
                {
                    Variables.Remove(v);
                }
            });
        }
    }

    private ScopedStack<Scope> lambdaStack = new();
    private BLambdaExpression RootExpression;

    // When false, the rewriter processes only the root lambda's own body and does
    // NOT descend into nested lambdas. Used by the async pre-rewrite (which runs in
    // isolation, before the enclosing scope exists): descending there would convert
    // a nested lambda's references to an OUTER variable into boxed closure accesses
    // and finalize/cache that nested lambda's closure repository against an
    // incomplete scope chain — stranding the capture (the enclosing scope never
    // learns to box the variable). Leaving nested lambdas untouched lets the later
    // full top-down rewrite (which has the whole scope chain) thread them correctly.
    private bool rewriteNestedLambdas = true;

    public Scope Root => lambdaStack.TopItem;

    public LambdaRewriter()
    {

    }

    

    protected override BExpression VisitLambda(BLambdaExpression node)
    {
        /// we will not mark nested lambda as relay for two reasons
        /// 1.  In case of Runtime Execution, IMethodRepository will be
        ///     available as global static variable to directly run and
        ///     register the method.
        /// 2.  In case of Assembly builder, there is no need to maintain
        ///     global repository as AssemblyBuilder will become Method 
        ///     Repository
        using var scope = lambdaStack.Push(node);
        if (node != RootExpression)
        {
            node.SetupAsClosure();
            if (!rewriteNestedLambdas)
                return node;
        }
        if (node.This != null)
        {
            Root.Register(node.This.AsSequence());
        }
        Root.Register(node.Parameters.AsSequence());
        return base.VisitLambda(node);
    }

    protected override BExpression VisitBlock(BBlockExpression yBlockExpression)
    {
        var variables = Root.Root.Name.Name == "body" || Root.Root.Name.Name == "body_outer"
            ? CollectBlockVariables(yBlockExpression)
            : yBlockExpression.FlattenVariables.AsSequence();
        using var scope = Root.Register(variables);
        return base.VisitBlock(yBlockExpression);
    }

    private static Sequence<BParameterExpression> CollectBlockVariables(BExpression expression)
    {
        var variables = new Sequence<BParameterExpression>();
        new BlockVariableCollector(variables).Visit(expression);
        return variables;
    }

    private sealed class BlockVariableCollector(Sequence<BParameterExpression> variables) : BExpressionMapVisitor
    {
        protected override BExpression VisitBlock(BBlockExpression yBlockExpression)
        {
            variables.AddRange(yBlockExpression.FlattenVariables);
            return base.VisitBlock(yBlockExpression);
        }

        protected override BExpression VisitLambda(BLambdaExpression yLambdaExpression) => yLambdaExpression;
    }

    private static void CollectBlockVariables(BExpression expression, Sequence<BParameterExpression> variables)
    {
        switch (expression)
        {
            case BBlockExpression block:
                variables.AddRange(block.FlattenVariables);
                foreach (var (child, _) in block.FlattenExpressions)
                    CollectBlockVariables(child, variables);
                break;

            case BConvertExpression convert:
                CollectBlockVariables(convert.Target, variables);
                break;

            case BReturnExpression @return when @return.Default != null:
                CollectBlockVariables(@return.Default, variables);
                break;

            case BConditionalExpression conditional:
                CollectBlockVariables(conditional.test, variables);
                CollectBlockVariables(conditional.@true, variables);
                if (conditional.@false != null)
                    CollectBlockVariables(conditional.@false, variables);
                break;

            case BLoopExpression loop:
                CollectBlockVariables(loop.Body, variables);
                break;

            case BTryCatchFinallyExpression tryCatchFinally:
                CollectBlockVariables(tryCatchFinally.Try, variables);
                if (tryCatchFinally.Catch != null)
                    CollectBlockVariables(tryCatchFinally.Catch.Body, variables);
                if (tryCatchFinally.Finally != null)
                    CollectBlockVariables(tryCatchFinally.Finally, variables);
                break;
        }
    }

    protected override BExpression VisitParameter(BParameterExpression yParameterExpression)
    {
        CheckForClosure(lambdaStack.Top, yParameterExpression);
        return base.VisitParameter(yParameterExpression);
    }

    private BParameterExpression CheckForClosure(ScopedStack<Scope>.ScopedItem current, BParameterExpression pe, bool setup = false)
    {
        if (current.Item.Variables.Contains(pe))
        {
            if (setup)
            {
                return current.Item.Root.GetClosureRepository().Convert(pe);
            }
            return pe;
        }
        var parent = current.Parent;
        if (parent == null)
            return pe;

        var repository = current.Item.Root.GetClosureRepository();
        return repository.Setup(pe, () => CheckForClosure(parent,pe,true));
    }

    public static BExpression Rewrite(BLambdaExpression convert)
    {
        var l = new LambdaRewriter();
        l.RootExpression = convert;
        l.Visit(convert);
        return convert;
    }

    /// <summary>
    /// Rewrites only the root lambda's own body, leaving nested lambdas untouched
    /// for a later enclosing-scope rewrite. Used by the async function pre-rewrite,
    /// which runs before the enclosing scope exists; see <see cref="rewriteNestedLambdas"/>.
    /// </summary>
    public static BExpression RewriteRootOnly(BLambdaExpression convert)
    {
        var l = new LambdaRewriter { rewriteNestedLambdas = false };
        l.RootExpression = convert;
        l.Visit(convert);
        return convert;
    }
}
