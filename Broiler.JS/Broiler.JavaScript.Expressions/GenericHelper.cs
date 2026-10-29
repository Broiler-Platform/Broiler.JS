using System.Diagnostics.CodeAnalysis;
using System;
using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler;


public static class Generic
{
    public static T CreateDelegate<T>(this MethodInfo m)
        where T : Delegate => (T)m.CreateDelegate(typeof(T));

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    private static T CreateTypedDelegate<T>(
        this MethodInfo method, params Type[] types)
        where T : Delegate
    {
        if (types.Length > 0)
        {
            method = method.GetGenericMethodDefinition().MakeGenericMethod(types);
        }
        return (T)method.CreateDelegate(typeof(T));
    }

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T>(Type type, Func<T> fx) => fx.Method.CreateTypedDelegate<Func<T>>(type)();

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T1, T>(Type type, Func<T1, T> fx, T1 p1) => fx.Method.CreateTypedDelegate<Func<T1, T>>(type)(p1);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T1, T2, T>(Type type, Func<T1, T2, T> fx, T1 p1, T2 p2) => fx.Method.CreateTypedDelegate<Func<T1, T2, T>>(type)(p1, p2);
    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T1, T2, T3, T>(Type type, Func<T1, T2, T3, T> fx, T1 p1, T2 p2, T3 p3) => fx.Method.CreateTypedDelegate<Func<T1, T2, T3, T>>(type)(p1, p2, p3);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T>(Type type, Type type2, Func<T> fx) => fx.Method.CreateTypedDelegate<Func<T>>(type, type2)();

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T1, T>(Type type, Type type2, Func<T1, T> fx, T1 p1) => fx.Method.CreateTypedDelegate<Func<T1, T>>(type, type2)(p1);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T1, T2, T>(Type type, Type type2, Func<T1, T2, T> fx, T1 p1, T2 p2) => fx.Method.CreateTypedDelegate<Func<T1, T2, T>>(type, type2)(p1, p2);
    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<T1, T2, T3, T>(Type type, Type type2, Func<T1, T2, T3, T> fx, T1 p1, T2 p2, T3 p3) => fx.Method.CreateTypedDelegate<Func<T1, T2, T3, T>>(type, type2)(p1, p2, p3);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T>(this Target target, Type type, Func<T> fx) => fx.Method.CreateTypedDelegate<Func<Target, T>>(type)(target);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T1, T>(this Target target, Type type, Func<T1, T> fx, T1 p1) => fx.Method.CreateTypedDelegate<Func<Target, T1, T>>(type)(target, p1);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T1, T2, T>(this Target target, Type type, Func<T1, T2, T> fx, T1 p1, T2 p2) => fx.Method.CreateTypedDelegate<Func<Target, T1, T2, T>>(type)(target, p1, p2);
    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T1, T2, T3, T>(this Target target, Type type, Func<T1, T2, T3, T> fx, T1 p1, T2 p2, T3 p3) => fx.Method.CreateTypedDelegate<Func<Target, T1, T2, T3, T>>(type)(target, p1, p2, p3);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T>(this Target target, Type type, Type type2, Func<T> fx) => fx.Method.CreateTypedDelegate<Func<Target, T>>(type, type2)(target);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T1, T>(this Target target, Type type, Type type2, Func<T1, T> fx, T1 p1) => fx.Method.CreateTypedDelegate<Func<Target, T1, T>>(type, type2)(target, p1);

    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T1, T2, T>(this Target target, Type type, Type type2, Func<T1, T2, T> fx, T1 p1, T2 p2) => fx.Method.CreateTypedDelegate<Func<Target, T1, T2, T>>(type, type2)(target, p1, p2);
    [RequiresUnreferencedCode("This reflection helper requires the caller to preserve the selected members.")]
    [RequiresDynamicCode("This helper constructs runtime generic or array types; use the portable backend for Native AOT.")]
    public static T InvokeAs<Target, T1, T2, T3, T>(this Target target, Type type, Type type2, Func<T1, T2, T3, T> fx, T1 p1, T2 p2, T3 p3) => fx.Method.CreateTypedDelegate<Func<Target, T1, T2, T3, T>>(type, type2)(target, p1, p2, p3);
}
