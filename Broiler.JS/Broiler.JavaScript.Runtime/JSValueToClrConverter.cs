using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Runtime;

public static class JSValueToClrConverter
{
    private static bool HasValue(this JSValue value) => value == null ? false : !value.IsNullOrUndefined;

    internal static Func<BExpression, int, BExpression> GetAtExpression;
    internal static Func<BExpression, BExpression> LengthExpression;

    private static Func<BExpression, int, BExpression> EnsureGetAtExpression =>
        GetAtExpression ?? throw new InvalidOperationException(
            "JSValueToClrConverter.GetAtExpression delegate is not initialized. Ensure the LinqExpressions assembly module initializer has run.");

    private static Func<BExpression, BExpression> EnsureLengthExpression =>
        LengthExpression ?? throw new InvalidOperationException(
            "JSValueToClrConverter.LengthExpression delegate is not initialized. Ensure the LinqExpressions assembly module initializer has run.");

    public static string ToString(JSValue value, string name) => value.HasValue() ? value.ToString() : throw new JSException($"{name} is required");

    public static JSValue ToJSNumber(this JSValue value, string name) =>
        value.IsNumber ? value : (value is JSPrimitiveObject po ? po.value.ToJSNumber(name) : throw new JSException($"{name} is not a number"));

    public static bool ToBoolean(JSValue value, string name) => value.HasValue() ? value.BooleanValue : throw new JSException($"{name} is required");
    public static bool? ToNullableBoolean(JSValue value, string name) => value.IsNullOrUndefined ? null : value.BooleanValue;

    public static long ToLong(JSValue value, string name) => value.HasValue() ? value.BigIntValue : throw new JSException($"{name} is required");

    public static long? ToNullableLong(JSValue value, string name) => value.IsNullOrUndefined ? null : value.BigIntValue;
    public static int ToInt(JSValue value, string name) => value.HasValue() ? value.IntValue : throw new JSException($"{name} is required");
    public static int? ToNullableInt(JSValue value, string name) => value.IsNullOrUndefined ? null : value.IntValue;

    public static short ToShort(JSValue value, string name) => value.HasValue() ? (short)value.IntValue : throw new JSException($"{name} is required");
    public static short? ToNullableShort(JSValue value, string name) => value.IsNullOrUndefined ? null : (short)value.IntValue;

    public static byte ToByte(JSValue value, string name) => value.HasValue() ? (byte)value.IntValue : throw new JSException($"{name} is required");
    public static byte? ToNullableByte(JSValue value, string name) => value.IsNullOrUndefined ? null : (byte)value.IntValue;
    public static sbyte ToSByte(JSValue value, string name) => value.HasValue() ? (sbyte)value.IntValue : throw new JSException($"{name} is required");
    public static sbyte? ToNullableSByte(JSValue value, string name) => value.IsNullOrUndefined ? null : (sbyte)value.IntValue;

    public static double ToDouble(JSValue value, string name) => value.HasValue() ? value.DoubleValue : throw new JSException($"{name} is required");
    public static double? ToNullableDouble(JSValue value, string name) => value.IsNullOrUndefined ? null : value.DoubleValue;
    public static float ToFloat(JSValue value, string name) => value.HasValue() ? (float)value.DoubleValue : throw new JSException($"{name} is required");
    public static float? ToNullableFloat(JSValue value, string name) => value.IsNullOrUndefined ? null : (float)value.DoubleValue;
    public static decimal ToDecimal(JSValue value, string name) => value.HasValue() ? (decimal)value.DoubleValue : throw new JSException($"{name} is required");
    public static decimal? ToNullableDecimal(JSValue value, string name) => value.IsNullOrUndefined ? null : (decimal)value.DoubleValue;

    // A String the CLR bridge turns into a DateTime or DateTimeOffset is read with the invariant culture under every host
    // (milestone SJL-9's clause 8, on the owner's ruling of 2026-11-11, "Fix all + enforce"); until then each of the four
    // read `DateTime.Parse(value.ToString())`, the host's date format, so that under de-DE "11/12/2026" was 11 December.
    // The engine's records say only that it parses a string "by the platform for a host method" (bundle sjl-6-001) and
    // name no format or culture for it.
    public static DateTime ToDateTime(JSValue value, string name) =>
        value.HasValue() ? (value.ConvertTo(typeof(DateTime), out var dt) ? (DateTime)dt : DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture)) : throw new ArgumentException($"{name} is required");

    public static DateTime? ToNullableDateTime(JSValue value, string name) =>
        value.HasValue() ? (value.ConvertTo(typeof(DateTime), out var dt) ? (DateTime)dt : DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture)) : null;

    public static DateTimeOffset ToDateTimeOffset(JSValue value, string name) =>
        value.HasValue() ? (value.ConvertTo(typeof(DateTimeOffset), out var dto) ? (DateTimeOffset)dto : DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture)) : throw new ArgumentException($"{name} is required");

    public static DateTimeOffset? ToNullableDateTimeOffset(JSValue value, string name) =>
        value.HasValue() ? (value.ConvertTo(typeof(DateTimeOffset), out var dto) ? (DateTimeOffset)dto : DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture)) : null;

    private static readonly Dictionary<Type, MethodInfo> methods = [];
    private static readonly MethodInfo GetAsGeneric = typeof(JSValueToClrConverter).GetMethod(nameof(GetAs));
    private static readonly MethodInfo GetAsOrThrowGeneric = typeof(JSValueToClrConverter).GetMethod(nameof(GetAsOrThrow));

    static JSValueToClrConverter()
    {
        foreach (var method in typeof(JSValueToClrConverter).GetMethods())
        {
            if (!method.Name.StartsWith("To", StringComparison.Ordinal))
                continue;

            if (!method.IsStatic)
                continue;

            methods[method.ReturnType] = method;
        }
    }

    public static BExpression GetArgument(BExpression args, int index, Type type, BExpression defaultValue, string name)
    {
        if (methods.TryGetValue(type, out var method))
        {
            if (defaultValue == null)
                return BExpression.Call(null, method, EnsureGetAtExpression(args, index), BExpression.Constant(name));

            return BExpression.Condition(BExpression.Binary(EnsureLengthExpression(args), BOperator.Greater, BExpression.Constant(index)),
                BExpression.Call(null, method, EnsureGetAtExpression(args, index), BExpression.Constant(name)), defaultValue);
        }

        if (typeof(JSValue).IsAssignableFrom(type))
            return EnsureGetAtExpression(args, index);

        return Get(EnsureGetAtExpression(args, index), type, defaultValue, $"{name} is required");
    }

    public static BExpression Get(BExpression target, Type type, string name)
    {
        if (typeof(JSValue).IsAssignableFrom(type))
            return target;

        if (methods.TryGetValue(type, out var method))
            return BExpression.Call(null, method, target, BExpression.Constant(name));

        var m = GetAsOrThrowGeneric.MakeGenericMethod(type);
        return BExpression.Call(null, m, target, BExpression.Constant($"{name} is required"));
    }

    public static BExpression Get(BExpression target, Type type, BExpression defaultValue, string name)
    {
        if (defaultValue == null)
            return Get(target, type, name);

        if (typeof(JSValue).IsAssignableFrom(type))
            return target;

        if (methods.TryGetValue(type, out var method))
            return BExpression.Call(null, method, target, BExpression.Constant(name));

        var m = GetAsGeneric.MakeGenericMethod(type);
        return BExpression.Coalesce(BExpression.Call(null, m, target), defaultValue);
    }

    public static Func<JSValue, string, T> ToFastClrDelegate<T>()
    {
        var type = typeof(T);

        if (methods.TryGetValue(type, out var m))
            return m.CreateDelegate<Func<JSValue, string, T>>();

        return GetAsOrThrow<T>;
    }

    public static T ToFastClrValue<T>(this JSValue value)
    {
        var type = typeof(T);
        if (typeof(JSValue).IsAssignableFrom(type))
            return (T)(object)value;

        if (methods.TryGetValue(type, out var m))
        {
            var f = m.CreateDelegate<Func<JSValue, string, T>>();
            return f(value, "");
        }

        if (value.TryConvertTo(typeof(T), out var obj) && obj is T v)
            return v;

        throw new JSException($"Failed to convert JSValue to {type.Name}");
    }

    public static T GetAs<T>(JSValue value) => (value ?? JSValue.UndefinedValue).TryConvertTo(typeof(T), out var obj) && obj is T v1 ? v1 : default;

    public static T GetAsOrThrow<T>(JSValue value, string error) => (value ?? JSValue.UndefinedValue).TryConvertTo(typeof(T), out var obj) && obj is T v1 ? v1 : throw new JSException(error);

}
