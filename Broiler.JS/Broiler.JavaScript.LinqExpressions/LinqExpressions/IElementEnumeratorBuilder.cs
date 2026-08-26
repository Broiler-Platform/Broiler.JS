using System;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LambdaGen;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;


public class IElementEnumeratorBuilder
{
    public static Expression Get(Expression target)
    {
        if (typeof(JSValue).IsAssignableFrom(target.Type))
            return target.CallExpression<JSValue, IElementEnumerator>(() => (x) => x.GetIterableEnumerator());

        if (ArgumentsBuilder.refType == target.Type || target.Type == typeof(Arguments))
            return ArgumentsBuilder.GetElementEnumerator(target);

        throw new NotImplementedException();
    }

    public static Expression GetAsync(Expression target)
    {
        if (typeof(JSValue).IsAssignableFrom(target.Type))
            return target.CallExpression<JSValue, IElementEnumerator>(() => (x) => x.GetAsyncIterableEnumerator());

        return Get(target);
    }

    public static Expression MoveNext(Expression target, Expression item) => target.CallExpression<IElementEnumerator, JSValue, bool>(() => (x, a) => x.MoveNext(out a), item);

    /// <summary>
    /// One <c>for await…of</c> step: the raw <c>next()</c> result, for the async function to await.
    /// See <see cref="AsyncIterationStep"/> — the awaiting cannot happen inside the enumerator.
    /// </summary>
    public static Expression AsyncNextRaw(Expression target)
        => target.CallExpression<IElementEnumerator, JSValue>(() => (x) => x.AsyncNextRaw());

    private static readonly System.Reflection.MethodInfo AsyncStepIsDoneMethod =
        typeof(AsyncIterationStep).GetMethod(nameof(AsyncIterationStep.IsDone), [typeof(JSValue)])
        ?? throw new InvalidOperationException("AsyncIterationStep.IsDone(JSValue) not found");

    private static readonly System.Reflection.MethodInfo AsyncStepValueMethod =
        typeof(AsyncIterationStep).GetMethod(nameof(AsyncIterationStep.Value), [typeof(JSValue)])
        ?? throw new InvalidOperationException("AsyncIterationStep.Value(JSValue) not found");

    /// <summary>Reads the settled step result's <c>done</c>.</summary>
    public static Expression AsyncStepIsDone(Expression settledResult)
        => Expression.Call(null, AsyncStepIsDoneMethod, settledResult);

    /// <summary>Reads the settled step result's <c>value</c>.</summary>
    public static Expression AsyncStepValue(Expression settledResult)
        => Expression.Call(null, AsyncStepValueMethod, settledResult);

    public static Expression AssignMoveNext(Expression assignee, Expression target) => Expression.Assign(assignee,
            target.CallExpression<IElementEnumerator, JSValue, JSValue>(() => (x, a) => x.NextOrDefault(a), JSUndefinedBuilder.Value));
}
