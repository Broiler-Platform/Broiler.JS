using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
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
    private static readonly KeyString ListFormatKey = KeyStrings.GetOrCreate("ListFormat");
    private static readonly KeyString LocaleKey = KeyStrings.GetOrCreate("Locale");
    private static readonly KeyString PluralRulesKey = KeyStrings.GetOrCreate("PluralRules");
    private static readonly KeyString FormatKey = KeyStrings.GetOrCreate("format");
    private static readonly KeyString FormatRangeKey = KeyStrings.GetOrCreate("formatRange");
    private static readonly KeyString FormatRangeToPartsKey = KeyStrings.GetOrCreate("formatRangeToParts");
    private static readonly KeyString SupportedValuesOfKey = KeyStrings.GetOrCreate("supportedValuesOf");
    private static readonly KeyString SupportedLocalesOfKey = KeyStrings.GetOrCreate("supportedLocalesOf");
    private static readonly KeyString StyleKey = KeyStrings.GetOrCreate("style");
    private static readonly KeyString CurrencyKey = KeyStrings.GetOrCreate("currency");
    private static readonly KeyString UnitKey = KeyStrings.GetOrCreate("unit");
    private static readonly KeyString TypeKey = KeyStrings.GetOrCreate("type");
    private static readonly KeyString LocaleMatcherKey = KeyStrings.GetOrCreate("localeMatcher");
    private static readonly KeyString FallbackKey = KeyStrings.GetOrCreate("fallback");
    private static readonly KeyString LanguageDisplayKey = KeyStrings.GetOrCreate("languageDisplay");
    private static readonly KeyString RoundingIncrementKey = KeyStrings.GetOrCreate("roundingIncrement");
    private static readonly KeyString RoundingModeKey = KeyStrings.GetOrCreate("roundingMode");
    private static readonly KeyString RoundingPriorityKey = KeyStrings.GetOrCreate("roundingPriority");
    private static readonly KeyString TrailingZeroDisplayKey = KeyStrings.GetOrCreate("trailingZeroDisplay");

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
        intl.FastAddValue(DisplayNamesKey, CreateDisplayNamesConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(DurationFormatKey, CreateDurationFormatConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(ListFormatKey, CreateListFormatConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(LocaleKey, CreateLocaleConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(PluralRulesKey, CreateSimpleConstructor("PluralRules", 0), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(SupportedValuesOfKey,
            new JSFunction(static (in Arguments a) =>
            {
                _ = a.Get1().StringValue;
                return JSValue.CreateArray();
            }, "supportedValuesOf", "function supportedValuesOf() { [native code] }", length: 1, createPrototype: false),
            JSPropertyAttributes.ConfigurableValue);
        return intl;
    }

    private static JSFunction CreateSimpleConstructor(string name, int length)
        => new((in Arguments a) =>
        {
            ValidateConstructorArguments(name, in a);

            return new JSObject();
        }, name, $"function {name}() {{ [native code] }}", length: length);

    private static JSFunction CreateSupportedLocalesOfFunction()
        => new(static (in Arguments a) => JSValue.CreateArray(), "supportedLocalesOf", "function supportedLocalesOf() { [native code] }", length: 1, createPrototype: false);

    private static JSFunction CreateDurationFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) =>
            new JSIntlDurationFormat(ValidateConstructorArguments("DurationFormat", in a)),
            "DurationFormat",
            "function DurationFormat() { [native code] }",
            length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(FormatKey,
            new JSFunction(JSIntlDurationFormat.FormatPrototype, "format", "function format() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("formatToParts"),
            new JSFunction(JSIntlDurationFormat.FormatToPartsPrototype, "formatToParts", "function formatToParts() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlDurationFormat.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        return constructor;
    }

    private static JSFunction CreateListFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) =>
        {
            ValidateConstructorArguments("ListFormat", in a);
            return new JSIntlListFormat();
        }, "ListFormat", "function ListFormat() { [native code] }", length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        return constructor;
    }

    private static JSFunction CreateLocaleConstructor()
        => new(static (in Arguments a) =>
        {
            ValidateLocaleConstructorArguments(in a);
            return new JSIntlLocale();
        }, "Locale", "function Locale() { [native code] }", length: 1);

    private static JSFunction CreateDisplayNamesConstructor()
        => new((in Arguments a) =>
        {
            ObserveOptions(ValidateConstructorArguments("DisplayNames", in a), FallbackKey, LanguageDisplayKey, LocaleMatcherKey, StyleKey, TypeKey);
            return new JSObject();
        }, "DisplayNames", "function DisplayNames() { [native code] }", length: 2);

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
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlNumberFormat(in a),
            "NumberFormat",
            "function NumberFormat() { [native code] }",
            length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey,
            new JSFunction(static (in Arguments a) => a.Get1().IsNullOrUndefined ? JSValue.CreateArray() : a.Get1(), "supportedLocalesOf", "function supportedLocalesOf() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        if (constructor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(FormatKey.ToString(), FormatKey)).IsUndefined)
        {
            constructor.prototype.FastAddProperty(FormatKey,
                new JSFunction(static (in Arguments a) =>
                {
                    if (a.This is not JSIntlNumberFormat @this)
                        throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.format called on incompatible receiver");

                    return new JSFunction((in Arguments inner) => @this.Format(in inner), "format", "function format() { [native code] }", createPrototype: false, length: 1);
                },
                    "get format",
                    "function get format() { [native code] }",
                    createPrototype: false,
                    length: 0),
                null,
                JSPropertyAttributes.ConfigurableProperty);
        }
        return constructor;
    }

    internal static JSObject ValidateConstructorArguments(string name, in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"Intl.{name} requires 'new'");

        ValidateLocalesArgument(a.Get1());
        return ValidateOptionsArgument(a.GetAt(1));
    }

    internal static void ValidateLocaleConstructorArguments(in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Intl.Locale requires 'new'");

        var tag = a.Get1();
        if (!tag.IsString && !tag.IsObject)
            throw JSEngine.NewTypeError("Locale tag must be a string or object");

        _ = ValidateOptionsArgument(a.GetAt(1));
    }

    internal static JSObject ValidateOptionsArgument(JSValue options)
    {
        if (options.IsUndefined)
            return null;

        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Options must be an object");

        return optionsObject;
    }

    private static void ValidateLocalesArgument(JSValue locales)
    {
        if (locales.IsUndefined)
            return;

        if (locales.IsNull)
            throw JSEngine.NewTypeError("Cannot convert undefined or null to object");

        if (locales.IsString)
            return;

        if (locales is not JSObject localesObject)
        {
            if (locales.IsSymbol)
                _ = locales.StringValue;

            return;
        }

        var lengthValue = localesObject[KeyStrings.length];
        if (lengthValue.IsUndefined)
            return;

        var length = lengthValue.UIntValue;
        for (uint i = 0; i < length; i++)
        {
            var locale = localesObject[i];
            if (locale.IsUndefined || locale.IsNull || locale.IsBoolean || locale.IsNumber || locale.IsSymbol)
                throw JSEngine.NewTypeError("Locale list entries must be strings or objects");

            _ = locale.StringValue;
        }
    }

    internal static void ValidateNumberFormatOptions(JSObject options)
    {
        if (options == null)
            return;

        var styleValue = options[StyleKey];
        var style = styleValue.IsUndefined ? null : styleValue.StringValue;

        var currencyValue = options[CurrencyKey];
        if (!currencyValue.IsUndefined)
        {
            var currency = currencyValue.StringValue;
            if (!IsWellFormedCurrencyCode(currency))
                throw JSEngine.NewRangeError("Invalid currency option");
        }

        if (style == "currency" && currencyValue.IsUndefined)
            throw JSEngine.NewTypeError("Intl.NumberFormat currency style requires a currency option");

        var unitValue = options[UnitKey];
        if (style == "unit" && unitValue.IsUndefined)
            throw JSEngine.NewTypeError("Intl.NumberFormat unit style requires a unit option");

        ObserveOptions(options, RoundingIncrementKey, RoundingModeKey, RoundingPriorityKey, TrailingZeroDisplayKey);
    }

    private static void ObserveOptions(JSObject options, params KeyString[] keys)
    {
        if (options == null)
            return;

        foreach (var key in keys)
            _ = options[key];
    }

    private static bool IsWellFormedCurrencyCode(string currency)
    {
        if (currency.Length != 3)
            return false;

        foreach (var ch in currency)
        {
            if ((ch < 'A' || ch > 'Z') && (ch < 'a' || ch > 'z'))
                return false;
        }

        return true;
    }
}

public class JSIntlRelativeTimeFormat : JSObject
{
    public JSValue Format(in Arguments args)
    {
        var value = args[0] ?? JSUndefined.Value;
        _ = value.DoubleValue;
        return value;
    }

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlRelativeTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.format called on incompatible receiver");

    public JSIntlRelativeTimeFormat(in Arguments a) : this()
    {
        JSIntl.ValidateConstructorArguments("RelativeTimeFormat", in a);
    }

    private JSIntlRelativeTimeFormat() : base(CurrentPrototype("RelativeTimeFormat")) { }

    private static JSObject CurrentPrototype(string name)
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate(name)] as JSFunction)?.prototype
            : null;
}

public sealed class JSIntlDurationFormat(JSObject _ = null) : JSObject
{
    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.format called on incompatible receiver");

        return JSValue.CreateString(string.Empty);
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.formatToParts called on incompatible receiver");

        return JSValue.CreateArray();
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.resolvedOptions called on incompatible receiver");

        return new JSObject();
    }
}

public sealed class JSIntlListFormat : JSObject;

public sealed class JSIntlLocale : JSObject;

public class JSIntlNumberFormat : JSObject
{
    public JSIntlNumberFormat(in Arguments a) : this()
    {
        JSIntl.ValidateNumberFormatOptions(JSIntl.ValidateConstructorArguments("NumberFormat", in a));
    }

    private JSIntlNumberFormat() : base(CurrentPrototype()) { }

    public JSValue Format(in Arguments a) => JSValue.CreateString((a[0] ?? JSUndefined.Value).ToString());

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("NumberFormat")] as JSFunction)?.prototype
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
