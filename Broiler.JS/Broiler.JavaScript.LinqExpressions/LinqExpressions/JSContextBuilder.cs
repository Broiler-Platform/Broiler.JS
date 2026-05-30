using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.YParameterExpression;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LambdaGen;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSContextStackBuilder
{
    public readonly static Type itemTypeRef = typeof(CallStackItem).MakeByRefType();

    public static void Push(Sequence<Expression> stmtList, Expression context, Expression stack, Expression fileName, Expression function, int line, int column)
    {
        var newScope = LexicalScopeBuilder.NewScope(context, fileName, function, line, column);
        stmtList.Add(Expression.Assign(stack, newScope));
    }

    public static Expression Pop(Expression stack, Expression context) => LexicalScopeBuilder.Pop(stack, context);

}

public class JSContextBuilder
{
    private static readonly FieldInfo _CurrentField = typeof(JSEngine).GetField(nameof(JSEngine.Current));
    public static Expression Current = Expression.Field(null, _CurrentField);
    public static Expression Object = Current.PropertyExpression<IJSExecutionContext, JSValue>(() => (x) => x.Object);

    private static PropertyInfo _Index = typeof(JSObject).IndexProperty(typeof(KeyString));
    private static MethodInfo _AssignIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.AssignIdentifier), [typeof(KeyString).MakeByRefType(), typeof(JSValue)]);
    private static MethodInfo _DeleteIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.DeleteIdentifier), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _PushDirectEvalScope = typeof(JSContext).GetMethod(nameof(JSContext.PushDirectEvalScope), [typeof(JSVariable[])]);
    private static MethodInfo _PushWithScope = typeof(JSContext).GetMethod(nameof(JSContext.PushWithScope), [typeof(JSValue)]);
    private static MethodInfo _CaptureWithScopes = typeof(JSContext).GetMethod(nameof(JSContext.CaptureWithScopes), Type.EmptyTypes);
    private static MethodInfo _ResolveIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.ResolveIdentifier), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _EnsureCanDeclareGlobalFunction = typeof(JSContext).GetMethod(nameof(JSContext.EnsureCanDeclareGlobalFunction), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _RegisterDirectEvalVariable = typeof(JSContext).GetMethod(
        nameof(JSContext.RegisterDirectEvalVariable),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        [typeof(JSVariable)],
        null);
    public static Expression Index(Expression key) => Expression.MakeIndex(Expression.Convert(Current, typeof(JSObject)), _Index, [key]);
    public static Expression AssignIdentifier(Expression key, Expression value) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _AssignIdentifier, key, value);
    public static Expression DeleteIdentifier(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _DeleteIdentifier, key);
    public static Expression PushDirectEvalScope(Expression variables) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _PushDirectEvalScope, variables);
    public static Expression PushWithScope(Expression value) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _PushWithScope, value);
    public static Expression CaptureWithScopes() => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _CaptureWithScopes);
    public static Expression ResolveIdentifier(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveIdentifier, key);
    public static Expression EnsureCanDeclareGlobalFunction(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _EnsureCanDeclareGlobalFunction, key);
    public static Expression RegisterDirectEvalVariable(Expression variable) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _RegisterDirectEvalVariable, variable);

    public static Expression NewTarget() => Expression.Coalesce(
        Current.PropertyExpression<IJSExecutionContext, CallStackItem>(() => (x) => x.Top).FieldExpression<CallStackItem, JSValue>(() => (x) => x.NewTarget),
        JSUndefinedBuilder.Value);

    public static Expression Register(ParameterExpression lScope, ParameterExpression variable) => lScope.CallExpression<IJSExecutionContext, JSVariable, JSValue>(() => (x, a) => x.Register(a), variable);
}
