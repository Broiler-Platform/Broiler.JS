using System;
using System.Reflection;
using Expression = Broiler.JavaScript.ExpressionCompiler.Expressions.BExpression;
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
    private static MethodInfo _enableTiering;

    private static MethodInfo invokeFunction;

    private static MethodInfo _invokeSuperConstructor;

    private static MethodInfo _normalizeConstructorReturn;

    private static MethodInfo _resolveTailCall;
    private static readonly MethodInfo NumericLoopPlanFactory = typeof(NumericLoopPlan).GetMethod(
        nameof(NumericLoopPlan.Create),
        [typeof(int), typeof(double), typeof(double), typeof(double), typeof(int), typeof(double), typeof(double)])
        ?? throw new InvalidOperationException("NumericLoopPlan.Create factory not found");

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
        _enableTiering = type.PublicMethod("EnableTiering", typeof(NumericLoopPlan), typeof(string))
            ?? throw new InvalidOperationException($"EnableTiering(NumericLoopPlan, string) not found on {type.FullName}");
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
        _resolveTailCall = typeof(JSTailCall).GetMethod("Resolve", [typeof(JSValue)])
            ?? throw new InvalidOperationException("Resolve(JSValue) method not found on JSTailCall");
    }

    /// <summary>
    /// Gets the concrete <c>JSFunction</c> <see cref="Type"/> registered via
    /// <see cref="Initialize"/>.  Used by the Compiler to avoid a direct
    /// assembly reference to BuiltIns.
    /// </summary>
    public static Type FunctionType => type;

    public static Expression Prototype(Expression target) => Expression.Field(target, _prototype);

    public static Expression InvokeSuperConstructor(Expression super, Expression newTarget, Expression returnValue, Expression args)
    {
        return Expression.Assign(returnValue,
            Expression.Call(null, _invokeSuperConstructor, newTarget, super, args));
    }

    /// <summary>
    /// Runs the superclass [[Construct]] and returns the resulting instance
    /// without binding it to the derived constructor's <c>this</c>. Callers that
    /// must enforce BindThisValue (single <c>super</c> call) bind the result
    /// separately via <see cref="JSVariableBuilder.BindThis"/>.
    /// </summary>
    public static Expression ConstructSuper(Expression super, Expression newTarget, Expression args)
        => Expression.Call(null, _invokeSuperConstructor, newTarget, super, args);

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

        // A constructor's completion value can never be a proper tail call — the
        // [[Construct]] return-value semantics below must inspect the actual value
        // (object passthrough vs `this` vs TypeError). When the body ends in
        // `return <call>` the call is emitted as a JSTailCall sentinel, which is a
        // JSObject and would wrongly satisfy IsObject and pass through unchecked.
        // Resolve it first so e.g. `return Symbol()` in a derived ctor still throws.
        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, Expression.Call(null, _resolveTailCall, returnValue)),
            Expression.Condition(JSValueBuilder.IsObject(temp), temp, nonObject));
    }

    public static Expression InvokeFunction(Expression target, Expression args, bool coalesce = false, bool inChain = false)
    {
        // Inside an optional chain a short-circuit yields the skip sentinel (the chain
        // root unwraps it), and an already-short-circuited callee (skip) propagates
        // without being called. `fn?.(x)` short-circuits on a nullish callee; a trailing
        // `()` after a short-circuited chain (e.g. `a?.b()()`) propagates the sentinel.
        if (inChain)
        {
            var pes = Expression.Parameters(typeof(JSValue));
            var pe = pes[0];

            Expression guard = JSValueBuilder.IsOptionalChainSkip(pe);
            if (coalesce)
                guard = Expression.OrElse(guard, JSValueBuilder.IsNullOrUndefined(pe));

            return Expression.Block(pes.AsSequence(), Expression.Assign(pe, target),
                Expression.Condition(guard, JSValueBuilder.OptionalChainSkip(), Expression.Call(pe, invokeFunction, args)));
        }

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

    public static Expression EnableTiering(Expression target, NumericLoopPlan numericPlan, string location)
    {
        var temp = Expression.Parameter(type, "#tieredFunction");
        var planExpression = numericPlan == null
            ? Expression.Constant(null, typeof(NumericLoopPlan))
            : Expression.Call(
                null,
                NumericLoopPlanFactory,
                Expression.Constant(numericPlan.LimitArgumentIndex),
                Expression.Constant(numericPlan.AccumulatorInitialValue),
                Expression.Constant(numericPlan.InductionInitialValue),
                Expression.Constant(numericPlan.InductionStep),
                Expression.Constant((int)numericPlan.Comparison),
                Expression.Constant(numericPlan.TermScale),
                Expression.Constant(numericPlan.TermOffset));
        return Expression.Block(
            temp.AsSequence(),
            Expression.Assign(temp, target),
            Expression.Call(
                temp,
                _enableTiering,
                planExpression,
                Expression.Constant(location)),
            temp);
    }

    public static Expression CaptureWithScopes(Expression target)
        => Expression.Convert(Expression.Call(null, _captureWithScopes, Expression.Convert(target, typeof(JSValue))), type);
}
