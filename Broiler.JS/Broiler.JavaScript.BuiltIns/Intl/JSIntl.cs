using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Date;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Symbol;
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
    private static readonly KeyString SegmenterKey = KeyStrings.GetOrCreate("Segmenter");
    private static readonly KeyString FormatKey = KeyStrings.GetOrCreate("format");
    private static readonly KeyString FormatRangeKey = KeyStrings.GetOrCreate("formatRange");
    private static readonly KeyString FormatRangeToPartsKey = KeyStrings.GetOrCreate("formatRangeToParts");
    private static readonly KeyString FormatToPartsKey = KeyStrings.GetOrCreate("formatToParts");
    private static readonly KeyString SupportedValuesOfKey = KeyStrings.GetOrCreate("supportedValuesOf");
    private static readonly KeyString GetCanonicalLocalesKey = KeyStrings.GetOrCreate("getCanonicalLocales");
    private static readonly KeyString SupportedLocalesOfKey = KeyStrings.GetOrCreate("supportedLocalesOf");
    private static readonly KeyString StyleKey = KeyStrings.GetOrCreate("style");
    private static readonly KeyString CurrencyKey = KeyStrings.GetOrCreate("currency");
    private static readonly KeyString UnitKey = KeyStrings.GetOrCreate("unit");
    private static readonly KeyString TypeKey = KeyStrings.GetOrCreate("type");
    private static readonly KeyString LocaleMatcherKey = KeyStrings.GetOrCreate("localeMatcher");
    private static readonly KeyString GranularityKey = KeyStrings.GetOrCreate("granularity");
    private static readonly KeyString FallbackKey = KeyStrings.GetOrCreate("fallback");
    private static readonly KeyString LanguageDisplayKey = KeyStrings.GetOrCreate("languageDisplay");
    private static readonly KeyString RoundingIncrementKey = KeyStrings.GetOrCreate("roundingIncrement");
    private static readonly KeyString RoundingModeKey = KeyStrings.GetOrCreate("roundingMode");
    private static readonly KeyString RoundingPriorityKey = KeyStrings.GetOrCreate("roundingPriority");
    private static readonly KeyString TrailingZeroDisplayKey = KeyStrings.GetOrCreate("trailingZeroDisplay");
    private static readonly KeyString TimeZoneKey = KeyStrings.GetOrCreate("timeZone");
    private static readonly KeyString CollationKey = KeyStrings.GetOrCreate("collation");
    private static readonly KeyString LanguageKey = KeyStrings.GetOrCreate("language");
    private static readonly KeyString ScriptKey = KeyStrings.GetOrCreate("script");
    private static readonly KeyString RegionKey = KeyStrings.GetOrCreate("region");
    private static readonly KeyString NumberingSystemKey = KeyStrings.GetOrCreate("numberingSystem");
    private static readonly Regex StructurallyValidLanguageTagPattern = new(
        @"^(?:(?:[A-Za-z]{2,3}(?:-[A-Za-z]{3}){0,3}|[A-Za-z]{4}|[A-Za-z]{5,8})(?:-[A-Za-z]{4})?(?:-(?:[A-Za-z]{2}|\d{3}))?(?:-(?:[0-9A-Za-z]{5,8}|\d[0-9A-Za-z]{3}))*(?:-(?:[0-9A-WY-Za-wy-z](?:-[0-9A-Za-z]{2,8})+))*(?:-x(?:-[0-9A-Za-z]{1,8})+)?|x(?:-[0-9A-Za-z]{1,8})+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> InvalidGrandfatheredLanguageTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "no-bok",
        "no-nyn",
        "zh-min",
        "zh-min-nan",
    };

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
        intl.FastAddValue(PluralRulesKey, CreatePluralRulesConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(SegmenterKey, CreateSegmenterConstructor(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(GetCanonicalLocalesKey, CreateGetCanonicalLocalesFunction(), JSPropertyAttributes.ConfigurableValue);
        intl.FastAddValue(SupportedValuesOfKey,
            new JSFunction(static (in Arguments a) =>
            {
                _ = a.Get1().StringValue;
                return JSValue.CreateArray();
            }, "supportedValuesOf", "function supportedValuesOf() { [native code] }", length: 1, createPrototype: false),
            JSPropertyAttributes.ConfigurableValue);
        // Intl[@@toStringTag] = "Intl"
        intl.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString("Intl"), JSPropertyAttributes.ConfigurableReadonlyValue);
        return intl;
    }

    private static JSFunction CreateSimpleConstructor(string name, int length)
        => new((in Arguments a) =>
        {
            ValidateConstructorArguments(name, in a);

            return new JSObject();
        }, name, $"function {name}() {{ [native code] }}", length: length);

    private static void SetIntlToStringTag(JSFunction constructor, string name)
    {
        constructor.FastAddValue(KeyStrings.prototype, constructor.prototype, JSPropertyAttributes.ReadonlyValue);
        constructor.prototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString($"Intl.{name}"), JSPropertyAttributes.ConfigurableReadonlyValue);
    }

    private static JSFunction CreatePluralRulesConstructor()
    {
        var constructor = new JSFunction((in Arguments a) =>
        {
            ValidateConstructorArguments("PluralRules", in a);
            return new JSIntlPluralRules();
        }, "PluralRules", "function PluralRules() { [native code] }", length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("selectRange"),
            new JSFunction(static (in Arguments a) =>
            {
                if (a.This is not JSIntlPluralRules)
                    throw JSEngine.NewTypeError("Intl.PluralRules.prototype.selectRange called on incompatible receiver");

                var start = a.Get1();
                var end = a.GetAt(1);
                if (start.IsUndefined || end.IsUndefined)
                    throw JSEngine.NewTypeError("Intl.PluralRules.prototype.selectRange requires defined start and end values");

                _ = start.DoubleValue;
                _ = end.DoubleValue;
                return JSValue.CreateString("other");
            }, "selectRange", "function selectRange() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "PluralRules");
        return constructor;
    }

    private static JSFunction CreateSupportedLocalesOfFunction()
        => new(static (in Arguments a) => CanonicalizeLocaleList(a.Get1()), "supportedLocalesOf", "function supportedLocalesOf() { [native code] }", length: 1, createPrototype: false);

    private static JSFunction CreateGetCanonicalLocalesFunction()
        => new(static (in Arguments a) => CanonicalizeLocaleList(a.Get1()),
            "getCanonicalLocales",
            "function getCanonicalLocales() { [native code] }",
            length: 1,
            createPrototype: false);

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
        SetIntlToStringTag(constructor, "DurationFormat");
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
        constructor.prototype.FastAddValue(FormatKey,
            new JSFunction(JSIntlListFormat.FormatPrototype, "format", "function format() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(FormatToPartsKey,
            new JSFunction(JSIntlListFormat.FormatToPartsPrototype, "formatToParts", "function formatToParts() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlListFormat.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "ListFormat");
        return constructor;
    }

    private static JSFunction CreateLocaleConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) =>
        {
            return new JSIntlLocale(ValidateLocaleConstructorArguments(in a));
        }, "Locale", "function Locale() { [native code] }", length: 1);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("maximize"),
            new JSFunction(JSIntlLocale.MaximizePrototype, "maximize", "function maximize() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("minimize"),
            new JSFunction(JSIntlLocale.MinimizePrototype, "minimize", "function minimize() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getCalendars"),
            new JSFunction(JSIntlLocale.GetCalendarsPrototype, "getCalendars", "function getCalendars() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getCollations"),
            new JSFunction(JSIntlLocale.GetCollationsPrototype, "getCollations", "function getCollations() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getHourCycles"),
            new JSFunction(JSIntlLocale.GetHourCyclesPrototype, "getHourCycles", "function getHourCycles() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getNumberingSystems"),
            new JSFunction(JSIntlLocale.GetNumberingSystemsPrototype, "getNumberingSystems", "function getNumberingSystems() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getTextInfo"),
            new JSFunction(JSIntlLocale.GetTextInfoPrototype, "getTextInfo", "function getTextInfo() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getTimeZones"),
            new JSFunction(JSIntlLocale.GetTimeZonesPrototype, "getTimeZones", "function getTimeZones() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("getWeekInfo"),
            new JSFunction(JSIntlLocale.GetWeekInfoPrototype, "getWeekInfo", "function getWeekInfo() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.toString,
            new JSFunction(JSIntlLocale.ToStringPrototype, "toString", "function toString() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "Locale");
        return constructor;
    }

    private static JSFunction CreateSegmenterConstructor()
    {
        var constructor = new JSFunction((in Arguments a) =>
        {
            ObserveOptions(ValidateConstructorArguments("Segmenter", in a), LocaleMatcherKey, GranularityKey);
            return new JSIntlSegmenter();
        }, "Segmenter", "function Segmenter() { [native code] }", length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlSegmenter.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("segment"),
            new JSFunction(JSIntlSegmenter.SegmentPrototype, "segment", "function segment() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "Segmenter");
        return constructor;
    }

    private static JSFunction CreateDisplayNamesConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlDisplayNames(in a),
            "DisplayNames",
            "function DisplayNames() { [native code] }",
            length: 2);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("of"),
            new JSFunction(JSIntlDisplayNames.OfPrototype, "of", "function of() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlDisplayNames.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "DisplayNames");
        return constructor;
    }

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
        constructor.prototype.FastAddValue(FormatToPartsKey,
            new JSFunction(JSIntlDateTimeFormat.FormatToPartsPrototype, "formatToParts", "function formatToParts() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "DateTimeFormat");
        return constructor;
    }

    private static JSFunction CreateRelativeTimeFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlRelativeTimeFormat(in a),
            "RelativeTimeFormat",
            "function RelativeTimeFormat() { [native code] }",
            length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(FormatKey,
            new JSFunction(JSIntlRelativeTimeFormat.FormatPrototype, "format", "function format() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(FormatToPartsKey,
            new JSFunction(JSIntlRelativeTimeFormat.FormatToPartsPrototype, "formatToParts", "function formatToParts() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlRelativeTimeFormat.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "RelativeTimeFormat");
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
        constructor.prototype.FastAddValue(FormatRangeKey,
            new JSFunction(JSIntlNumberFormat.FormatRangePrototype, "formatRange", "function formatRange() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(FormatRangeToPartsKey,
            new JSFunction(JSIntlNumberFormat.FormatRangeToPartsPrototype, "formatRangeToParts", "function formatRangeToParts() { [native code] }", createPrototype: false, length: 2),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "NumberFormat");
        return constructor;
    }

    internal static JSObject ValidateConstructorArguments(string name, in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"Intl.{name} requires 'new'");

        ValidateLocalesArgument(a.Get1());
        return ValidateOptionsArgument(a.GetAt(1));
    }

    internal static string ValidateLocaleConstructorArguments(in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Intl.Locale requires 'new'");

        var tag = a.Get1();
        if (!tag.IsString && !tag.IsObject)
            throw JSEngine.NewTypeError("Locale tag must be a string or object");

        var tagString = tag.StringValue;
        ValidateLanguageTag(tagString);
        ValidateLocaleOptions(tagString, ValidateOptionsArgument(a.GetAt(1)));
        return tagString;
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
        _ = CanonicalizeLocaleList(locales);
    }

    private static JSValue CanonicalizeLocaleList(JSValue locales)
    {
        var result = JSValue.CreateArray();

        if (locales.IsUndefined)
            return result;

        if (locales.IsNull)
            throw JSEngine.NewTypeError("Cannot convert undefined or null to object");

        if (locales.IsString)
        {
            result.AddArrayItem(JSValue.CreateString(ValidateLanguageTag(locales.StringValue)));
            return result;
        }

        if (locales is not JSObject localesObject)
        {
            if (locales.IsSymbol)
                _ = locales.StringValue;

            throw JSEngine.NewTypeError("Locale list must be a string or an object");
        }

        var lengthValue = localesObject[KeyStrings.length];
        if (lengthValue.IsUndefined)
            return result;

        var length = lengthValue.UIntValue;
        for (uint i = 0; i < length; i++)
        {
            if (!localesObject.HasProperty(JSValue.CreateString(i.ToString())).BooleanValue)
                continue;

            var locale = localesObject[i];
            if (locale.IsUndefined || locale.IsNull || locale.IsBoolean || locale.IsNumber || locale.IsSymbol)
                throw JSEngine.NewTypeError("Locale list entries must be strings or objects");

            result.AddArrayItem(JSValue.CreateString(ValidateLanguageTag(locale.StringValue)));
        }

        return result;
    }

    internal static string ValidateLanguageTag(string tag)
    {
        if (!StructurallyValidLanguageTagPattern.IsMatch(tag) ||
            InvalidGrandfatheredLanguageTags.Contains(tag) ||
            HasDuplicateVariantSubtag(tag) ||
            HasInvalidUnicodeExtensionKey(tag))
            throw JSEngine.NewRangeError("Invalid language tag");

        return tag;
    }

    private static bool HasDuplicateVariantSubtag(string tag)
    {
        var subtags = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> variants = null;

        for (var i = 1; i < subtags.Length; i++)
        {
            var subtag = subtags[i];
            if (subtag.Length == 1)
                break;

            var isVariant =
                (subtag.Length >= 5 && subtag.Length <= 8) ||
                (subtag.Length == 4 && char.IsDigit(subtag[0]));

            if (!isVariant)
                continue;

            variants ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!variants.Add(subtag))
                return true;
        }

        return false;
    }

    private static bool HasInvalidUnicodeExtensionKey(string tag)
    {
        var subtags = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < subtags.Length; i++)
        {
            if (!subtags[i].Equals("u", StringComparison.OrdinalIgnoreCase))
                continue;

            for (i++; i < subtags.Length; i++)
            {
                var subtag = subtags[i];
                if (subtag.Length == 1)
                    break;

                if (subtag.Length == 2 && char.IsDigit(subtag[1]))
                    return true;
            }
        }

        return false;
    }

    private static void ValidateLocaleOptions(string tag, JSObject options)
    {
        if (options == null)
            return;

        var collation = options[CollationKey];
        if (!collation.IsUndefined)
        {
            var collationValue = collation.StringValue;
            if (!Regex.IsMatch(collationValue, @"^[0-9A-Za-z]{3,8}(?:-[0-9A-Za-z]{3,8})*$", RegexOptions.CultureInvariant))
                throw JSEngine.NewRangeError("Invalid collation option");
        }

        if (tag.StartsWith("x-", StringComparison.OrdinalIgnoreCase) &&
            (!options[LanguageKey].IsUndefined ||
             !options[ScriptKey].IsUndefined ||
             !options[RegionKey].IsUndefined ||
             !options[NumberingSystemKey].IsUndefined))
            throw JSEngine.NewRangeError("Invalid locale options for private use tag");
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

    internal static string ResolveLocale(JSValue locales)
    {
        var localeList = CanonicalizeLocaleList(locales);
        if (localeList is JSObject array)
        {
            var first = array[0u];
            if (!first.IsUndefined)
                return first.StringValue;
        }

        return string.IsNullOrEmpty(CultureInfo.CurrentCulture.Name) ? "en-US" : CultureInfo.CurrentCulture.Name;
    }

    internal static JSIntlDisplayNamesOptions ValidateDisplayNamesOptions(JSObject options)
    {
        if (options == null)
            throw JSEngine.NewTypeError("Intl.DisplayNames requires an options object");

        _ = GetOption(options, LocaleMatcherKey, ["lookup", "best fit"], false, "best fit");
        var style = GetOption(options, StyleKey, ["narrow", "short", "long"], false, "long");
        var type = GetOption(options, TypeKey, ["language", "region", "script", "currency", "calendar", "dateTimeField"], true);
        var fallback = GetOption(options, FallbackKey, ["code", "none"], false, "code");
        var languageDisplay = GetOption(options, LanguageDisplayKey, ["dialect", "standard"], false, "dialect");
        return new JSIntlDisplayNamesOptions(style, type, fallback, languageDisplay);
    }

    private static string GetOption(JSObject options, KeyString key, string[] allowedValues, bool required, string defaultValue = null)
    {
        var value = options[key];
        if (value.IsUndefined)
        {
            if (required)
                throw JSEngine.NewTypeError($"Missing required {key} option");

            return defaultValue;
        }

        var stringValue = value.StringValue;
        foreach (var allowedValue in allowedValues)
        {
            if (allowedValue == stringValue)
                return stringValue;
        }

        throw JSEngine.NewRangeError($"Invalid {key} option");
    }

    internal static void ValidateDateTimeFormatOptions(JSObject options)
    {
        if (options == null)
            return;

        var timeZoneValue = options[TimeZoneKey];
        if (!timeZoneValue.IsUndefined)
        {
            var timeZone = timeZoneValue.StringValue;
            if (timeZone.Contains('\u2212'))
                throw JSEngine.NewRangeError("Invalid timeZone option");
        }
    }

    private static void ObserveOptions(JSObject options, params KeyString[] keys)
    {
        if (options == null)
            return;

        foreach (var key in keys)
            _ = options[key];
    }

    internal static bool IsWellFormedCurrencyCode(string currency)
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

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlRelativeTimeFormat)
            throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.formatToParts called on incompatible receiver");

        var value = a[0] ?? JSUndefined.Value;
        _ = value.DoubleValue;
        var unit = a.GetAt(1);
        if (unit.IsUndefined)
            throw JSEngine.NewRangeError("Invalid unit argument");
        var unitStr = unit.StringValue;
        var validUnits = new HashSet<string>
        {
            "year", "years", "quarter", "quarters", "month", "months",
            "week", "weeks", "day", "days", "hour", "hours",
            "minute", "minutes", "second", "seconds"
        };
        if (!validUnits.Contains(unitStr))
            throw JSEngine.NewRangeError($"Invalid unit argument: {unitStr}");

        return JSValue.CreateArray();
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlRelativeTimeFormat)
            throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.resolvedOptions called on incompatible receiver");

        return new JSObject();
    }

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

public sealed class JSIntlSegmenter : JSObject
{
    public JSIntlSegmenter() : base(CurrentPrototype()) { }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegmenter)
            throw JSEngine.NewTypeError("Intl.Segmenter.prototype.resolvedOptions called on incompatible receiver");

        return new JSObject();
    }

    public static JSValue SegmentPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegmenter)
            throw JSEngine.NewTypeError("Intl.Segmenter.prototype.segment called on incompatible receiver");

        _ = a.Get1().StringValue;
        return new JSObject();
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("Segmenter")] as JSFunction)?.prototype
            : null;
}

public sealed class JSIntlDurationFormat(JSObject _ = null) : JSObject
{
    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.format called on incompatible receiver");

        ValidateDurationArgument(a.Get1());
        return JSValue.CreateString(string.Empty);
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.formatToParts called on incompatible receiver");

        ValidateDurationArgument(a.Get1());
        return JSValue.CreateArray();
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.resolvedOptions called on incompatible receiver");

        return new JSObject();
    }

    private static void ValidateDurationArgument(JSValue duration)
    {
        if (duration is not JSObject durationObject)
            return;

        var hasPositive = false;
        var hasNegative = false;
        foreach (var (_, value) in durationObject.Entries)
        {
            var numericValue = value.DoubleValue;
            if (double.IsNaN(numericValue))
                continue;

            hasPositive |= numericValue > 0;
            hasNegative |= numericValue < 0;
            if (hasPositive && hasNegative)
                throw JSEngine.NewRangeError("Invalid duration");
        }
    }
}

public sealed class JSIntlListFormat : JSObject
{
    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.format called on incompatible receiver");

        return JSValue.CreateString(string.Empty);
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.formatToParts called on incompatible receiver");

        return JSValue.CreateArray();
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.resolvedOptions called on incompatible receiver");

        return new JSObject();
    }
}

internal sealed record JSIntlDisplayNamesOptions(string Style, string Type, string Fallback, string LanguageDisplay);

public sealed class JSIntlDisplayNames : JSObject
{
    private readonly string locale;
    private readonly JSIntlDisplayNamesOptions options;

    public JSIntlDisplayNames(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        options = JSIntl.ValidateDisplayNamesOptions(JSIntl.ValidateConstructorArguments("DisplayNames", in a));
        locale = JSIntl.ResolveLocale(a.Get1());
    }

    public static JSValue OfPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDisplayNames @this)
            throw JSEngine.NewTypeError("Intl.DisplayNames.prototype.of called on incompatible receiver");

        return JSValue.CreateString(@this.ValidateCode(a.Get1()));
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDisplayNames @this)
            throw JSEngine.NewTypeError("Intl.DisplayNames.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result[KeyStrings.GetOrCreate("locale")] = JSValue.CreateString(@this.locale);
        result[KeyStrings.GetOrCreate("style")] = JSValue.CreateString(@this.options.Style);
        result[KeyStrings.GetOrCreate("type")] = JSValue.CreateString(@this.options.Type);
        result[KeyStrings.GetOrCreate("fallback")] = JSValue.CreateString(@this.options.Fallback);
        if (@this.options.Type == "language")
            result[KeyStrings.GetOrCreate("languageDisplay")] = JSValue.CreateString(@this.options.LanguageDisplay);
        return result;
    }

    private string ValidateCode(JSValue codeValue)
    {
        var code = codeValue.StringValue;
        switch (options.Type)
        {
            case "language":
                return JSIntl.ValidateLanguageTag(code);
            case "region":
                if (Regex.IsMatch(code, "^(?:[A-Za-z]{2}|\\d{3})$", RegexOptions.CultureInvariant))
                    return code;
                break;
            case "script":
                if (Regex.IsMatch(code, "^[A-Za-z]{4}$", RegexOptions.CultureInvariant))
                    return code;
                break;
            case "currency":
                if (JSIntl.IsWellFormedCurrencyCode(code))
                    return code.ToUpperInvariant();
                break;
            case "calendar":
                if (Regex.IsMatch(code, "^[A-Za-z0-9]{3,8}(?:-[A-Za-z0-9]{3,8})*$", RegexOptions.CultureInvariant))
                    return code;
                break;
            case "dateTimeField":
                switch (code)
                {
                    case "era":
                    case "year":
                    case "quarter":
                    case "month":
                    case "weekOfYear":
                    case "weekday":
                    case "day":
                    case "dayPeriod":
                    case "hour":
                    case "minute":
                    case "second":
                    case "timeZoneName":
                        return code;
                }

                break;
        }

        throw JSEngine.NewRangeError($"Invalid code for Intl.DisplayNames type {options.Type}");
    }
}

public sealed class JSIntlLocale : JSObject
{
    private readonly string tag;

    public JSIntlLocale(string tag = "und") : base(CurrentPrototype()) => this.tag = tag;

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("Locale")] as JSFunction)?.prototype
            : null;

    private static JSIntlLocale RequireLocale(in Arguments a, string method)
    {
        if (a.This is not JSIntlLocale locale)
            throw JSEngine.NewTypeError($"Intl.Locale.prototype.{method} called on incompatible receiver");

        return locale;
    }

    public static JSValue MaximizePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "maximize");
        return new JSIntlLocale(locale.tag);
    }

    public static JSValue MinimizePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "minimize");
        return new JSIntlLocale(locale.tag);
    }

    public static JSValue GetCalendarsPrototype(in Arguments a)
    {
        RequireLocale(in a, "getCalendars");
        return JSValue.CreateArray();
    }

    public static JSValue GetCollationsPrototype(in Arguments a)
    {
        RequireLocale(in a, "getCollations");
        return JSValue.CreateArray();
    }

    public static JSValue GetHourCyclesPrototype(in Arguments a)
    {
        RequireLocale(in a, "getHourCycles");
        return JSValue.CreateArray();
    }

    public static JSValue GetNumberingSystemsPrototype(in Arguments a)
    {
        RequireLocale(in a, "getNumberingSystems");
        return JSValue.CreateArray();
    }

    public static JSValue GetTextInfoPrototype(in Arguments a)
    {
        RequireLocale(in a, "getTextInfo");
        return new JSObject();
    }

    public static JSValue GetTimeZonesPrototype(in Arguments a)
    {
        RequireLocale(in a, "getTimeZones");
        return JSValue.CreateArray();
    }

    public static JSValue GetWeekInfoPrototype(in Arguments a)
    {
        RequireLocale(in a, "getWeekInfo");
        return new JSObject();
    }

    public static JSValue ToStringPrototype(in Arguments a)
        => JSValue.CreateString(RequireLocale(in a, "toString").tag);
}

public sealed class JSIntlPluralRules : JSObject
{
    public JSIntlPluralRules() : base(CurrentPrototype()) { }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("PluralRules")] as JSFunction)?.prototype
            : null;
}

public class JSIntlNumberFormat : JSObject
{
    public JSIntlNumberFormat(in Arguments a) : this()
    {
        JSIntl.ValidateNumberFormatOptions(JSIntl.ValidateConstructorArguments("NumberFormat", in a));
    }

    private JSIntlNumberFormat() : base(CurrentPrototype()) { }

    public JSValue Format(in Arguments a) => JSValue.CreateString((a[0] ?? JSUndefined.Value).ToString());

    public static JSValue FormatRangePrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatRange called on incompatible receiver");

        var start = (a[0] ?? JSUndefined.Value).DoubleValue;
        var end = (a.GetAt(1) ?? JSUndefined.Value).DoubleValue;
        if (double.IsNaN(start) || double.IsNaN(end))
            throw JSEngine.NewRangeError("Invalid number range");
        return JSValue.CreateString($"{start}–{end}");
    }

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatRangeToParts called on incompatible receiver");

        var start = (a[0] ?? JSUndefined.Value).DoubleValue;
        var end = (a.GetAt(1) ?? JSUndefined.Value).DoubleValue;
        if (double.IsNaN(start) || double.IsNaN(end))
            throw JSEngine.NewRangeError("Invalid number range");
        return JSValue.CreateArray();
    }

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

    public JSValue Format(in Arguments a)
    {
        var value = a.Length == 0 || a[0] == null || a[0].IsUndefined
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : a.Get1().DoubleValue;
        var clipped = JSDateMath.TimeClip(value);
        if (double.IsNaN(clipped))
            throw JSEngine.NewRangeError("Invalid time value");

        return new JSString(clipped.ToString(CultureInfo.InvariantCulture));
    }

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlDateTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.format called on incompatible receiver");

    public static JSValue FormatRangePrototype(in Arguments a)
        => a.This is JSIntlDateTimeFormat
            ? JSValue.CreateString($"{CoerceRangeTime(a.Get1())}–{CoerceRangeTime(a.GetAt(1))}")
            : throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRange called on incompatible receiver");

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRangeToParts called on incompatible receiver");

        var startValue = CoerceRangeTime(a.Get1());
        var endValue = CoerceRangeTime(a.GetAt(1));
        var parts = JSValue.CreateArray();
        var start = new JSObject();
        start[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("startRange");
        start[KeyStrings.GetOrCreate("value")] = JSValue.CreateNumber(startValue);
        var end = new JSObject();
        end[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("endRange");
        end[KeyStrings.GetOrCreate("value")] = JSValue.CreateNumber(endValue);
        parts.AddArrayItem(start);
        parts.AddArrayItem(end);
        return parts;
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatToParts called on incompatible receiver");

        var value = a.Length == 0 || a[0] == null || a[0].IsUndefined
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : a.Get1().DoubleValue;
        var clipped = JSDateMath.TimeClip(value);
        if (double.IsNaN(clipped))
            throw JSEngine.NewRangeError("Invalid time value");

        var formatted = clipped.ToString(CultureInfo.InvariantCulture);
        var parts = JSValue.CreateArray();
        var part = new JSObject();
        part[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("literal");
        part[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(formatted);
        parts.AddArrayItem(part);
        return parts;
    }

    internal JSValue Format(DateTimeOffset value, JSObject format) => new JSString(value.ToString(locale));

    public JSIntlDateTimeFormat(in Arguments a) : base(CurrentPrototype())
    {
        JSIntl.ValidateDateTimeFormatOptions(JSIntl.ValidateConstructorArguments("DateTimeFormat", in a));
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

    private static double CoerceRangeTime(JSValue value)
    {
        var clipped = JSDateMath.TimeClip(value.DoubleValue);
        if (double.IsNaN(clipped))
            throw JSEngine.NewRangeError("Invalid time value");

        return clipped;
    }
}
