using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
using ParameterExpression = Broiler.JavaScript.ExpressionCompiler.Expressions.BParameterExpression;
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

    private static readonly PropertyInfo _DirectEvalSuper = typeof(JSContext).GetProperty(nameof(JSContext.DirectEvalSuper));
    /// <summary>Reads the home-object super reference made available to a direct eval body.</summary>
    public static Expression DirectEvalSuper => Expression.Property(Expression.Convert(Current, typeof(JSContext)), _DirectEvalSuper);

    private static readonly PropertyInfo _DirectEvalNewTarget = typeof(JSContext).GetProperty(nameof(JSContext.DirectEvalNewTarget));
    /// <summary>Reads the caller's <c>new.target</c> threaded into a direct eval body.</summary>
    public static Expression DirectEvalNewTarget => Expression.Property(Expression.Convert(Current, typeof(JSContext)), _DirectEvalNewTarget);

    private static readonly PropertyInfo _DirectEvalSuperConstructor = typeof(JSContext).GetProperty(nameof(JSContext.DirectEvalSuperConstructor));
    /// <summary>Reads the superclass constructor a <c>super(...)</c> in a direct eval body targets.</summary>
    public static Expression DirectEvalSuperConstructor => Expression.Property(Expression.Convert(Current, typeof(JSContext)), _DirectEvalSuperConstructor);

    private static readonly PropertyInfo _DirectEvalThisBinding = typeof(JSContext).GetProperty(nameof(JSContext.DirectEvalThisBinding));
    /// <summary>Reads the derived constructor <c>this</c> binding shared with a direct eval body.</summary>
    public static Expression DirectEvalThisBinding => Expression.Property(Expression.Convert(Current, typeof(JSContext)), _DirectEvalThisBinding);

    private static PropertyInfo _Index = typeof(JSObject).IndexProperty(typeof(KeyString));
    private static MethodInfo _AssignIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.AssignIdentifier), [typeof(KeyString).MakeByRefType(), typeof(JSValue)]);
    private static MethodInfo _AssignIdentifierStrict = typeof(JSContext).GetMethod(nameof(JSContext.AssignIdentifier), [typeof(KeyString).MakeByRefType(), typeof(JSValue), typeof(bool)]);
    private static MethodInfo _AssignIdentifierWithoutWith = typeof(JSContext).GetMethod(nameof(JSContext.AssignIdentifierWithoutWith), [typeof(KeyString).MakeByRefType(), typeof(JSValue), typeof(bool)]);
    private static MethodInfo _AssignWithObjectIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.AssignWithObjectIdentifier), [typeof(JSObject), typeof(KeyString).MakeByRefType(), typeof(JSValue), typeof(bool)]);
    private static MethodInfo _DeleteIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.DeleteIdentifier), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _PushDirectEvalScope = typeof(JSContext).GetMethod(nameof(JSContext.PushDirectEvalScope), [typeof(JSVariable[])]);
    private static MethodInfo _PushWithFallbackScope = typeof(JSContext).GetMethod(nameof(JSContext.PushWithFallbackScope), [typeof(JSVariable[]), typeof(JSVariable[])]);
    private static MethodInfo _PushWithScope = typeof(JSContext).GetMethod(nameof(JSContext.PushWithScope), [typeof(JSValue)]);
    private static MethodInfo _CaptureWithScopes = typeof(JSContext).GetMethod(nameof(JSContext.CaptureWithScopes), Type.EmptyTypes);
    private static MethodInfo _ResolveIdentifier = typeof(JSContext).GetMethod(nameof(JSContext.ResolveIdentifier), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _ResolveIdentifierStrict = typeof(JSContext).GetMethod(nameof(JSContext.ResolveIdentifierStrict), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _ResolveIdentifierWithoutWithScopes = typeof(JSContext).GetMethod(nameof(JSContext.ResolveIdentifierWithoutWithScopes), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _ResolveGlobalVarRead = typeof(JSContext).GetMethod(nameof(JSContext.ResolveGlobalVarRead), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _ResolveIdentifierOrUndefined = typeof(JSContext).GetMethod(nameof(JSContext.ResolveIdentifierOrUndefined), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _ResolveWithObject = typeof(JSContext).GetMethod(nameof(JSContext.ResolveWithObject), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _GetWithObjectBindingValue = typeof(JSContext).GetMethod(nameof(JSContext.GetWithObjectBindingValue), [typeof(JSObject), typeof(KeyString).MakeByRefType(), typeof(bool)]);
    private static MethodInfo _EnsureCanDeclareGlobalFunction = typeof(JSContext).GetMethod(nameof(JSContext.EnsureCanDeclareGlobalFunction), [typeof(KeyString).MakeByRefType()]);
    private static MethodInfo _DeclareGlobalFunction = typeof(JSContext).GetMethod(nameof(JSContext.DeclareGlobalFunction), [typeof(KeyString).MakeByRefType(), typeof(JSValue)]);
    private static MethodInfo _DeclareGlobalLexical = typeof(JSContext).GetMethod(nameof(JSContext.DeclareGlobalLexical), [typeof(JSVariable)]);
    private static MethodInfo _DeclareGlobalAnnexBFunction = typeof(JSContext).GetMethod(nameof(JSContext.DeclareGlobalAnnexBFunction), [typeof(KeyString).MakeByRefType(), typeof(JSValue)]);
    private static MethodInfo _RegisterDirectEvalVariable = typeof(JSContext).GetMethod(
        nameof(JSContext.RegisterDirectEvalVariable),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        [typeof(JSVariable)],
        null);
    public static Expression Index(Expression key) => Expression.MakeIndex(Expression.Convert(Current, typeof(JSObject)), _Index, [key]);
    public static Expression AssignIdentifier(Expression key, Expression value) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _AssignIdentifier, key, value);
    public static Expression AssignIdentifier(Expression key, Expression value, bool strictMode) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _AssignIdentifierStrict, key, value, Expression.Constant(strictMode));
    public static Expression AssignIdentifierWithoutWith(Expression key, Expression value, bool strictMode) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _AssignIdentifierWithoutWith, key, value, Expression.Constant(strictMode));
    public static Expression AssignWithObjectIdentifier(Expression withObject, Expression key, Expression value, bool strictMode) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _AssignWithObjectIdentifier, withObject, key, value, Expression.Constant(strictMode));
    public static Expression DeleteIdentifier(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _DeleteIdentifier, key);
    public static Expression PushDirectEvalScope(Expression variables) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _PushDirectEvalScope, variables);
    public static Expression PushWithFallbackScope(Expression variables, Expression shadowedVariables) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _PushWithFallbackScope, variables, shadowedVariables);
    public static Expression PushWithScope(Expression value) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _PushWithScope, value);
    public static Expression CaptureWithScopes() => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _CaptureWithScopes);
    public static Expression ResolveIdentifier(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveIdentifier, key);

    public static Expression ResolveIdentifierStrict(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveIdentifierStrict, key);
    public static Expression ResolveIdentifierWithoutWithScopes(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveIdentifierWithoutWithScopes, key);
    public static Expression ResolveGlobalVarRead(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveGlobalVarRead, key);
    public static Expression ResolveIdentifierOrUndefined(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveIdentifierOrUndefined, key);
    public static Expression ResolveWithObject(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _ResolveWithObject, key);
    public static Expression GetWithObjectBindingValue(Expression withObject, Expression key, bool strictMode) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _GetWithObjectBindingValue, withObject, key, Expression.Constant(strictMode));
    public static Expression EnsureCanDeclareGlobalFunction(Expression key) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _EnsureCanDeclareGlobalFunction, key);
    public static Expression DeclareGlobalFunction(Expression key, Expression value) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _DeclareGlobalFunction, key, value);
    public static Expression DeclareGlobalLexical(Expression variable) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _DeclareGlobalLexical, variable);
    public static Expression DeclareGlobalAnnexBFunction(Expression key, Expression value) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _DeclareGlobalAnnexBFunction, key, value);
    public static Expression RegisterDirectEvalVariable(Expression variable) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _RegisterDirectEvalVariable, variable);
    private static MethodInfo _GetOrCreateDirectEvalLocalBinding = typeof(JSContext).GetMethod(nameof(JSContext.GetOrCreateDirectEvalLocalBinding), [typeof(KeyString).MakeByRefType(), typeof(JSValue)]);
    public static Expression GetOrCreateDirectEvalLocalBinding(Expression key, Expression fallback) => Expression.Call(Expression.Convert(Current, typeof(JSContext)), _GetOrCreateDirectEvalLocalBinding, key, fallback);
    public static Expression Top => Current.PropertyExpression<IJSExecutionContext, CallStackItem>(() => (x) => x.Top);

    public static Expression NewTarget() => Expression.Coalesce(
        Top.FieldExpression<CallStackItem, JSValue>(() => (x) => x.NewTarget),
        JSUndefinedBuilder.Value);

    public static Expression Register(ParameterExpression lScope, ParameterExpression variable) => lScope.CallExpression<IJSExecutionContext, JSVariable, JSValue>(() => (x, a) => x.Register(a), variable);
}
