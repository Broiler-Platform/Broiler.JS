using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Runtime;

public static class JSObjectStatic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static JSObject ToJSObject(this JSValue value)
    {
        if (value is JSObject @object)
            return @object;

        if (value == null || value.IsNullOrUndefined)
            return JSException.ThrowTypeError<JSObject>(JSObject.Cannot_convert_undefined_or_null_to_object);

        return JSException.ThrowTypeError<JSObject>(JSObject.Parameter_is_not_an_object);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryAsObjectThrowIfNullOrUndefined(this JSValue value, out JSObject @object)
    {
        @object = null;
        if (value == null || value.IsNullOrUndefined)
            return JSException.ThrowTypeError<bool>(JSObject.Cannot_convert_undefined_or_null_to_object);

        @object = value as JSObject;
        return @object != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static JSValue RequireObjectCoercible(this JSValue value)
    {
        if (value == null || value.IsNullOrUndefined)
            return JSException.ThrowTypeError<JSValue>(JSObject.Cannot_convert_undefined_or_null_to_object);

        return value;
    }
}
