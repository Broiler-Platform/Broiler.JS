using Broiler.JavaScript.Runtime;
using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

public class ClrGeneratorV2Builder
{
    private static readonly Type type = typeof(ClrGeneratorV2);

    private static readonly MethodInfo _endFinally = type.PublicMethod(nameof(ClrGeneratorV2.EndFinally), typeof(int));
    private static readonly MethodInfo _takeReturnValue = type.PublicMethod(nameof(ClrGeneratorV2.TakeReturnValue));
    private static readonly MethodInfo _leaveTry = type.PublicMethod(nameof(ClrGeneratorV2.LeaveTry), typeof(int), typeof(int));
    private static readonly MethodInfo _leave = type.PublicMethod(nameof(ClrGeneratorV2.Leave), typeof(int));
    private static readonly MethodInfo _beginCatch = type.PublicMethod(nameof(ClrGeneratorV2.BeginCatch));
    private static readonly MethodInfo _beginFinally = type.PublicMethod(nameof(ClrGeneratorV2.BeginFinally));
    private static readonly MethodInfo _push = type.PublicMethod(nameof(ClrGeneratorV2.PushTry), typeof(int), typeof(int), typeof(int));
    private static readonly MethodInfo _pop = type.PublicMethod(nameof(ClrGeneratorV2.Pop));
    private static readonly MethodInfo _GetVariable = type.GetMethod("GetVariable");
    private static readonly MethodInfo _InitVariables = type.GetMethod("InitVariables");


    public static Expression Push(Expression exp, int c, int f, int e) => Expression.Call(exp, _push, Expression.Constant(c), Expression.Constant(f), Expression.Constant(e));

    internal static Expression GetVariable(ParameterExpression pe, int id, Type type) => Expression.Call(pe, _GetVariable.MakeGenericMethod(type), Expression.Constant(id));

    internal static Expression InitVariables(ParameterExpression pe, int count) => Expression.Call(pe, _InitVariables, Expression.Constant(count));

    internal static Expression Pop(ParameterExpression pe) => Expression.Call(pe, _pop);
    internal static Expression BeginCatch(ParameterExpression pe) => Expression.Call(pe, _beginCatch);
    internal static Expression BeginFinally(ParameterExpression pe) => Expression.Call(pe, _beginFinally);
    internal static Expression EndFinally(ParameterExpression pe, int id) => Expression.Call(pe, _endFinally, Expression.Constant(id));
    internal static Expression TakeReturnValue(ParameterExpression pe) => Expression.Call(pe, _takeReturnValue);
    internal static Expression LeaveTry(ParameterExpression pe, int id, int jump) => Expression.Call(pe, _leaveTry, Expression.Constant(id), Expression.Constant(jump));
    internal static Expression Leave(ParameterExpression pe, int id) => Expression.Call(pe, _leave, Expression.Constant(id));
}
