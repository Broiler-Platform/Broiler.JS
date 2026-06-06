using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.YExpression;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LambdaGen;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions;

public class JSFunctionBuilder
{
    static Type type;

    public static FieldInfo _prototype;

    private static FieldInfo _f;
    private static PropertyInfo _coerceThisOnInvoke;
    private static PropertyInfo _isStrictMode;
    private static MethodInfo _captureWithScopes;
    private static MethodInfo _addLegacyCallerAndArguments;
    private static PropertyInfo _isOrdinaryUserFunction;

    private static MethodInfo invokeFunction;

    private static MethodInfo _invokeSuperConstructor;

    private static MethodInfo _normalizeConstructorReturn;

    /// <summary>
    /// Initializes the builder with the concrete JSFunction type.
    /// Called from BuiltInsAssemblyInitializer.
    /// </summary>
    internal static void Initialize(Type functionType)
    {
        type = functionType;
        _prototype = type.PublicField("prototype")
            ?? throw new InvalidOperationException($"prototype field not found on {type.FullName}");
        _f = type.InternalField("f")
            ?? throw new InvalidOperationException($"f field not found on {type.FullName}");
        _coerceThisOnInvoke = type.GetProperty("CoerceThisOnInvoke")
            ?? throw new InvalidOperationException($"CoerceThisOnInvoke property not found on {type.FullName}");
        _isStrictMode = type.GetProperty("IsStrictMode")
            ?? throw new InvalidOperationException($"IsStrictMode property not found on {type.FullName}");
        _isOrdinaryUserFunction = type.GetProperty("IsOrdinaryUserFunction")
            ?? throw new InvalidOperationException($"IsOrdinaryUserFunction property not found on {type.FullName}");
        _captureWithScopes = type.PublicMethod("CaptureWithScopes", typeof(JSValue))
            ?? throw new InvalidOperationException($"CaptureWithScopes(JSValue) not found on {type.FullName}");
        _addLegacyCallerAndArguments = type.PublicMethod("AddLegacyCallerAndArguments")
            ?? throw new InvalidOperationException($"AddLegacyCallerAndArguments() not found on {type.FullName}");
        invokeFunction = typeof(JSValue).InternalMethod("InvokeFunction", ArgumentsBuilder.refType)
            ?? throw new InvalidOperationException("InvokeFunction method not found on JSValue");
        _invokeSuperConstructor = type.PublicMethod("InvokeSuperConstructor",
            typeof(JSValue), typeof(JSValue), typeof(Arguments).MakeByRefType())
            ?? throw new InvalidOperationException($"InvokeSuperConstructor method not found on {type.FullName}");
        _normalizeConstructorReturn = type.PublicMethod("ThrowDerivedConstructorReturnTypeError")
            ?? throw new InvalidOperationException($"ThrowDerivedConstructorReturnTypeError method not found on {type.FullName}");
    }

    /// <summary>
    /// Gets the concrete <c>JSFunction</c> <see cref="Type"/> registered via
    /// <see cref="Initialize"/>.  Used by the Compiler to avoid a direct
    /// assembly reference to BuiltIns.
    /// </summary>
    public static Type FunctionType => type;

    public static Expression Prototype(Expression target) => Expression.Field(target, _prototype);

    public static Expression InvokeSuperConstructor(Expression super, Expression returnValue, Expression args)
    {
        return Expression.Assign(returnValue,
            Expression.Call(null, _invokeSuperConstructor, JSUndefinedBuilder.Value, super, args));
    }

    /// <summary>
    /// Runs the superclass [[Construct]] and returns the resulting instance
    /// without binding it to the derived constructor's <c>this</c>. Callers that
    /// must enforce BindThisValue (single <c>super</c> call) bind the result
    /// separately via <see cref="JSVariableBuilder.BindThis"/>.
    /// </summary>
    public static Expression ConstructSuper(Expression super, Expression args)
        => Expression.Call(null, _invokeSuperConstructor, JSUndefinedBuilder.Value, super, args);

    /// <summary>
    /// Wraps a class constructor body value with the [[Construct]] return-value
    /// semantics. An object return passes through. Otherwise a base constructor
    /// yields <paramref name="thisValue"/>; a derived constructor yields
    /// <paramref name="thisValue"/> for an <c>undefined</c> return (whose guarded
    /// read raises a ReferenceError when <c>super</c> was never called) and
    /// throws a TypeError for any other value. <paramref name="thisValue"/> is
    /// only evaluated on the branches that need it so an object return never
    /// triggers the uninitialized-<c>this</c> guard.
    /// </summary>
    public static Expression NormalizeConstructorReturn(Expression returnValue, Expression thisValue, bool isDerived)
    {
        var temp = Expression.Parameter(typeof(JSValue), "#ctorret");
        var nonObject = isDerived
            ? Expression.Condition(
                JSValueBuilder.IsUndefined(temp),
                thisValue,
                Expression.Call(null, _normalizeConstructorReturn))
            : thisValue;

        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, returnValue),
            Expression.Condition(JSValueBuilder.IsObject(temp), temp, nonObject));
    }

    public static Expression InvokeFunction(Expression target, Expression args, bool coalesce = false)
    {
        if (coalesce)
        {
            var pes = Expression.Parameters(typeof(JSValue));
            var pe = pes[0];

            return Expression.Block(pes.AsSequence(), Expression.Assign(pe, target), Expression.Condition(JSValueBuilder.IsNullOrUndefined(pe),
                JSUndefinedBuilder.Value, Expression.Call(pe, invokeFunction, args)));
        }

        return Expression.Call(target, invokeFunction, args);
    }

    public static Expression New(Expression del, Expression name, Expression code, int length, bool createPrototype = true)
    {
        var created = NewLambdaExpression.NewExpression(type, del, name, code, Expression.Constant(length), Expression.Constant(createPrototype));
        var temp = Expression.Parameter(type, "#function");
        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, created),
            Expression.Assign(Expression.Property(temp, _isOrdinaryUserFunction), Expression.Constant(true)),
            temp);
    }

    public static Expression EnableNonStrictThis(Expression target)
    {
        var temp = Expression.Parameter(type, "#function");
        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, target),
            Expression.Assign(Expression.Property(temp, _coerceThisOnInvoke), Expression.Constant(true)),
            temp);
    }

    public static Expression EnableStrictMode(Expression target)
    {
        var temp = Expression.Parameter(type, "#function");
        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, target),
            Expression.Assign(Expression.Property(temp, _isStrictMode), Expression.Constant(true)),
            temp);
    }

    public static Expression EnableLegacyCallerAndArguments(Expression target)
    {
        var temp = Expression.Parameter(type, "#function");
        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, target),
            Expression.Call(temp, _addLegacyCallerAndArguments),
            temp);
    }

    public static Expression CaptureWithScopes(Expression target)
        => Expression.Convert(Expression.Call(null, _captureWithScopes, Expression.Convert(target, typeof(JSValue))), type);
}
