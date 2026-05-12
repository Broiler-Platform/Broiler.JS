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
    private static readonly KeyString NumberFormatKey = KeyStrings.GetOrCreate("NumberFormat");
    private static readonly KeyString DisplayNamesKey = KeyStrings.GetOrCreate("DisplayNames");
    private static readonly KeyString DurationFormatKey = KeyStrings.GetOrCreate("DurationFormat");
    private static readonly KeyString PluralRulesKey = KeyStrings.GetOrCreate("PluralRules");
    private static readonly KeyString FormatKey = KeyStrings.GetOrCreate("format");
    private static readonly KeyString FormatRangeKey = KeyStrings.GetOrCreate("formatRange");
    private static readonly KeyString FormatRangeToPartsKey = KeyStrings.GetOrCreate("formatRangeToParts");
    private static readonly KeyString SupportedValuesOfKey = KeyStrings.GetOrCreate("supportedValuesOf");
    private static readonly KeyString SupportedLocalesOfKey = KeyStrings.GetOrCreate("supportedLocalesOf");

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
        intl.FastAddValue(NumberFormatKey, CreateNumberFormatConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(DisplayNamesKey, CreateSimpleConstructor("DisplayNames", 2), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(DurationFormatKey, CreateSimpleConstructor("DurationFormat", 0), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(PluralRulesKey, CreateSimpleConstructor("PluralRules", 0), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(SupportedValuesOfKey,
            new JSFunction(static (in Arguments _) => JSValue.CreateArray(), "supportedValuesOf", "function supportedValuesOf() { [native code] }", length: 1, createPrototype: false),
            JSPropertyAttributes.ConfigurableValue);
        return intl;
    }

    private static JSFunction CreateSimpleConstructor(string name, int length)
        => new(static (in Arguments _) => new JSObject(), name, $"function {name}() {{ [native code] }}", length: length);

    private static JSFunction CreateDateTimeFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlDateTimeFormat(in a),
            "DateTimeFormat",
            "function DateTimeFormat() { [native code] }");
        constructor.FastAddValue(KeyStrings.length, JSValue.NumberZero, JSPropertyAttributes.ConfigurableReadonlyValue);
        constructor.prototype.FastAddProperty(FormatKey,
            new JSFunction(static (in Arguments a) =>
            {
                if (a.This is not JSIntlDateTimeFormat @this)
                    throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.format called on incompatible receiver");

                return new JSFunction((in Arguments inner) => @this.Format(in inner), "format", "function format() { [native code] }", createPrototype: false, length: 1);
            }, "get format", "function get format() { [native code] }", createPrototype: false, length: 0),
            null,
            JSPropertyAttributes.ConfigurableProperty);
        constructor.prototype.FastAddValue(FormatRangeKey,
            new JSFunction(JSIntlDateTimeFormat.FormatRangePrototype, "formatRange", "function formatRange() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(FormatRangeToPartsKey,
            new JSFunction(JSIntlDateTimeFormat.FormatRangeToPartsPrototype, "formatRangeToParts", "function formatRangeToParts() { [native code] }", createPrototype: false, length: 2),
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

    private static JSFunction CreateNumberFormatConstructor()
    {
        var constructor = CreateSimpleConstructor("NumberFormat", 0);
        constructor.FastAddValue(SupportedLocalesOfKey,
            new JSFunction(static (in Arguments a) => a.Get1().IsNullOrUndefined ? JSValue.CreateArray() : a.Get1(), "supportedLocalesOf", "function supportedLocalesOf() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        if (constructor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(FormatKey.ToString(), FormatKey)).IsUndefined)
        {
            constructor.prototype.FastAddProperty(FormatKey,
                new JSFunction(static (in Arguments _) =>
                    new JSFunction(static (in Arguments inner) => JSValue.CreateString((inner[0] ?? JSUndefined.Value).ToString()), "format", "function format() { [native code] }", createPrototype: false, length: 1),
                    "get format",
                    "function get format() { [native code] }",
                    createPrototype: false,
                    length: 0),
                null,
                JSPropertyAttributes.ConfigurableProperty);
        }
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

    public static JSValue FormatRangePrototype(in Arguments a)
        => a.This is JSIntlDateTimeFormat
            ? JSValue.CreateString($"{a.Get1()}–{a.GetAt(1)}")
            : throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRange called on incompatible receiver");

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRangeToParts called on incompatible receiver");

        var parts = JSValue.CreateArray();
        var start = new JSObject();
        start[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("startRange");
        start[KeyStrings.GetOrCreate("value")] = a.Get1();
        var end = new JSObject();
        end[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("endRange");
        end[KeyStrings.GetOrCreate("value")] = a.GetAt(1);
        parts.AddArrayItem(start);
        parts.AddArrayItem(end);
        return parts;
    }

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
