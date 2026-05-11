using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Intl;

public static class JSIntl
{
    private static readonly ConditionalWeakTable<JSObject, JSObject> Cache = new();
    private static readonly KeyString DateTimeFormatKey = KeyStrings.GetOrCreate("DateTimeFormat");
    private static readonly KeyString RelativeTimeFormatKey = KeyStrings.GetOrCreate("RelativeTimeFormat");
    private static readonly KeyString FormatKey = KeyStrings.GetOrCreate("format");

    public static JSValue GetIntlObject()
    {
        if (JSEngine.CurrentContext is JSObject global)
            return Cache.GetValue(global, static _ => CreateIntlObject());

        return CreateIntlObject();
    }

    private static JSObject CreateIntlObject()
    {
        var intl = new JSObject();
        intl.FastAddValue(DateTimeFormatKey, CreateDateTimeFormatConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(RelativeTimeFormatKey, CreateRelativeTimeFormatConstructor(), JSPropertyAttributes.ConfigurableValue);
        return intl;
    }

    private static JSFunction CreateDateTimeFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlDateTimeFormat(in a),
            "DateTimeFormat",
            "function DateTimeFormat() { [native code] }");
        constructor.FastAddValue(KeyStrings.length, JSValue.NumberZero, JSPropertyAttributes.ConfigurableReadonlyValue);
        constructor.prototype.FastAddValue(FormatKey,
            new JSFunction(JSIntlDateTimeFormat.FormatPrototype, "format", "function format() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        return constructor;
    }

    private static JSFunction CreateRelativeTimeFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlRelativeTimeFormat(in a),
            "RelativeTimeFormat",
            "function RelativeTimeFormat() { [native code] }",
            length: 0);
        constructor.prototype.FastAddValue(FormatKey,
            new JSFunction(JSIntlRelativeTimeFormat.FormatPrototype, "format", "function format() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        return constructor;
    }
}

public class JSIntlRelativeTimeFormat(in Arguments a) : JSObject(CurrentPrototype("RelativeTimeFormat"))
{
    public JSValue Format(in Arguments args) => args[0] ?? JSUndefined.Value;

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlRelativeTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.format called on incompatible receiver");

    private static JSObject CurrentPrototype(string name)
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate(name)] as JSFunction)?.prototype
            : null;
}

public class JSIntlDateTimeFormat : JSObject
{
    private static readonly ConcurrentDictionary<string, JSIntlDateTimeFormat> formats = new();
    private readonly CultureInfo locale;

    public static JSIntlDateTimeFormat Get(CultureInfo culture)
        => formats.GetOrAdd(culture.Name, static key => new JSIntlDateTimeFormat(CultureInfo.GetCultureInfo(key)));

    public JSValue Format(in Arguments a) => a[0] ?? JSUndefined.Value;

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlDateTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.format called on incompatible receiver");

    internal JSValue Format(DateTimeOffset value, JSObject format) => new JSString(value.ToString(locale));

    public JSIntlDateTimeFormat(in Arguments a) : base(CurrentPrototype())
    {
        locale = CultureInfo.CurrentCulture;
    }

    internal JSIntlDateTimeFormat(CultureInfo locale) : base()
    {
        this.locale = locale;
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("DateTimeFormat")] as JSFunction)?.prototype
            : null;
}
