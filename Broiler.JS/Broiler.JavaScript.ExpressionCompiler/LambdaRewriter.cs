#nullable enable
using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.ExpressionCompiler;

public static class ClosureRepositoryExtensions
{
    public static ClosureRepository GetClosureRepository(this YLambdaExpression lambda) => ClosureRepository.For(lambda);
}

public class ClosureRepository
{
    private static System.Runtime.CompilerServices.ConditionalWeakTable<YLambdaExpression, ClosureRepository> cache =
        [];

    public readonly Dictionary<YParameterExpression, (YParameterExpression local, YExpression value, int index, int argIndex)>
        Closures = new(Core.ReferenceEqualityComparer.Instance);

    public List<YParameterExpression> Inputs 
        = [];

    private YLambdaExpression lambda;

    protected ClosureRepository(YLambdaExpression lambda) => this.lambda = lambda;

    public static ClosureRepository For(YLambdaExpression lambda)
    {
        if (cache.TryGetValue(lambda, out var value))
            return value;
        value = new ClosureRepository(lambda);
        cache.Add(lambda, value);
        return value;
    }

    internal bool TryGet(YParameterExpression pe, out YExpression exp)
    {
        if (Closures.TryGetValue(pe, out var ve))
        {
            exp = ve.value;
            return true;
        }
        exp = default!;
        return false;
    }

    internal YParameterExpression Setup(YParameterExpression pe, Func<YParameterExpression> source)
    {
        if (Closures.TryGetValue(pe, out var value))
            return value.local;
        var s = source();
        bool isBox = typeof(Box).IsAssignableFrom(pe.Type);
        var boxType = isBox ? pe.Type : BoxHelper.For(pe.Type).BoxType;
        var converted = YExpression.Parameter(boxType, pe.Name + "`");
        YExpression valueField = isBox ? converted : YExpression.Field(converted, "Value");
        Closures[pe] = (converted, valueField, Inputs.Count, -1);
        Inputs.Add(s);
        return converted;
    }

    internal YParameterExpression Convert(YParameterExpression pe)
    {
        if (Closures.TryGetValue(pe, out var value))
            return value.local;
        bool isBox = typeof(Box).IsAssignableFrom(pe.Type);
        var boxType = isBox ? pe.Type : BoxHelper.For(pe.Type).BoxType;
        var converted = YExpression.Parameter(boxType, pe.Name + "`");
        YExpression valueField = isBox ? converted : YExpression.Field(converted, "Value");
        var argIndex = Array.IndexOf(lambda.Parameters, pe);
        Closures[pe] = (converted, valueField, -1, argIndex);
        return converted;
    }
}


public class LambdaRewriter: YExpressionMapVisitor
{
    public class Scope
    {
        public readonly YLambdaExpression Root;
        public readonly List<YParameterExpression> Variables = [];

        public Scope(YLambdaExpression exp)
        {
            Root = exp;
            Variables.AddRange(exp.Parameters);
        }

        public static implicit operator Scope(YLambdaExpression e) => new(e);

        internal IDisposable Register(IFastEnumerable<YParameterExpression> variables)
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
    private YLambdaExpression RootExpression;

    public Scope Root => lambdaStack.TopItem;
    
    public LambdaRewriter()
    {

    }

    

    protected override YExpression VisitLambda(YLambdaExpression node)
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
        }
        if (node.This != null)
        {
            Root.Register(node.This.AsSequence());
        }
        Root.Register(node.Parameters.AsSequence());
        return base.VisitLambda(node);
    }

    protected override YExpression VisitBlock(YBlockExpression yBlockExpression)
    {
        var variables = Root.Root.Name.Name == "body" || Root.Root.Name.Name == "body_outer"
            ? CollectBlockVariables(yBlockExpression)
            : yBlockExpression.FlattenVariables.AsSequence();
        using var scope = Root.Register(variables);
        return base.VisitBlock(yBlockExpression);
    }

    private static Sequence<YParameterExpression> CollectBlockVariables(YExpression expression)
    {
        var variables = new Sequence<YParameterExpression>();
        new BlockVariableCollector(variables).Visit(expression);
        return variables;
    }

    private sealed class BlockVariableCollector(Sequence<YParameterExpression> variables) : YExpressionMapVisitor
    {
        protected override YExpression VisitBlock(YBlockExpression yBlockExpression)
        {
            variables.AddRange(yBlockExpression.FlattenVariables);
            return base.VisitBlock(yBlockExpression);
        }

        protected override YExpression VisitLambda(YLambdaExpression yLambdaExpression) => yLambdaExpression;
    }

    private static void CollectBlockVariables(YExpression expression, Sequence<YParameterExpression> variables)
    {
        switch (expression)
        {
            case YBlockExpression block:
                variables.AddRange(block.FlattenVariables);
                foreach (var (child, _) in block.FlattenExpressions)
                    CollectBlockVariables(child, variables);
                break;

            case YConvertExpression convert:
                CollectBlockVariables(convert.Target, variables);
                break;

            case YReturnExpression @return when @return.Default != null:
                CollectBlockVariables(@return.Default, variables);
                break;

            case YConditionalExpression conditional:
                CollectBlockVariables(conditional.test, variables);
                CollectBlockVariables(conditional.@true, variables);
                if (conditional.@false != null)
                    CollectBlockVariables(conditional.@false, variables);
                break;

            case YLoopExpression loop:
                CollectBlockVariables(loop.Body, variables);
                break;

            case YTryCatchFinallyExpression tryCatchFinally:
                CollectBlockVariables(tryCatchFinally.Try, variables);
                if (tryCatchFinally.Catch != null)
                    CollectBlockVariables(tryCatchFinally.Catch.Body, variables);
                if (tryCatchFinally.Finally != null)
                    CollectBlockVariables(tryCatchFinally.Finally, variables);
                break;
        }
    }

    protected override YExpression VisitParameter(YParameterExpression yParameterExpression)
    {
        CheckForClosure(lambdaStack.Top, yParameterExpression);
        return base.VisitParameter(yParameterExpression);
    }

    private YParameterExpression CheckForClosure(ScopedStack<Scope>.ScopedItem current, YParameterExpression pe, bool setup = false)
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

    public static YExpression Rewrite(YLambdaExpression convert)
    {
        var l = new LambdaRewriter();
        l.RootExpression = convert;
        l.Visit(convert);   
        return convert;
    }
}
