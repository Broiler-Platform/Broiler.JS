using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Date;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using UnicodeCldr.LocaleData;

namespace Broiler.JavaScript.BuiltIns.Intl;

public static class JSIntl
{
    private static readonly ConditionalWeakTable<JSObject, JSObject> Cache = new();
    private static readonly KeyString DateTimeFormatKey = KeyStrings.GetOrCreate("DateTimeFormat");
    private static readonly KeyString RelativeTimeFormatKey = KeyStrings.GetOrCreate("RelativeTimeFormat");
    private static readonly KeyString NumberFormatKey = KeyStrings.GetOrCreate("NumberFormat");
    private static readonly KeyString CollatorKey = KeyStrings.GetOrCreate("Collator");
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
    private static readonly KeyString NotationKey = KeyStrings.GetOrCreate("notation");
    private static readonly KeyString SignDisplayKey = KeyStrings.GetOrCreate("signDisplay");
    private static readonly KeyString CompactDisplayKey = KeyStrings.GetOrCreate("compactDisplay");
    private static readonly KeyString UnitDisplayKey = KeyStrings.GetOrCreate("unitDisplay");
    private static readonly string[] NotationValues = ["standard", "scientific", "engineering", "compact"];
    private static readonly string[] SignDisplayValues = ["auto", "never", "always", "exceptZero", "negative"];
    private static readonly string[] CompactDisplayValues = ["short", "long"];
    private static readonly string[] UnitDisplayValues = ["short", "narrow", "long"];
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

    // Regular grandfathered tags that remain structurally valid language tags but
    // whose canonical form is a single preferred subtag (UTS #35 §3.3, the CLDR
    // language-alias "type" mappings). CanonicalizeUnicodeLocaleId replaces the
    // whole tag with the preferred value. Irregular grandfathered tags (i-klingon,
    // en-GB-oed, sgn-*, …) are not structurally valid and are rejected by the
    // grammar before reaching this table.
    private static readonly Dictionary<string, string> RegularGrandfatheredMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["art-lojban"] = "jbo",
        ["cel-gaulish"] = "xtg",
        ["zh-guoyu"] = "zh",
        ["zh-hakka"] = "hak",
        ["zh-xiang"] = "hsn",
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
        intl.FastAddValue(CollatorKey, CreateCollatorConstructor(), JSPropertyAttributes.ConfigurableValue);
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
            return new JSIntlPluralRules(in a);
        }, "PluralRules", "function PluralRules() { [native code] }", length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("select"),
            new JSFunction(static (in Arguments a) =>
            {
                if (a.This is not JSIntlPluralRules pluralRules)
                    throw JSEngine.NewTypeError("Intl.PluralRules.prototype.select called on incompatible receiver");

                // ToNumber the argument (may invoke valueOf), then resolve the
                // CLDR plural category for the number.
                var n = a.Get1().DoubleValue;
                return JSValue.CreateString(pluralRules.SelectCategory(n));
            }, "select", "function select() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlPluralRules.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
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
        => new(static (in Arguments a) =>
        {
            // SupportedLocales: canonicalize the requested locales, then coerce the
            // options argument and validate the localeMatcher option (RangeError on
            // an invalid value), and finally narrow the canonicalized list to the
            // locales the runtime can actually serve (LookupSupportedLocales).
            var requested = CanonicalizeLocaleList(a.Get1());
            var options = CoerceOptionsToObject(a.GetAt(1));
            _ = GetOption(options, LocaleMatcherKey, ["lookup", "best fit"], false, "best fit");
            return LookupSupportedLocales(requested);
        }, "supportedLocalesOf", "function supportedLocalesOf() { [native code] }", length: 1, createPrototype: false);

    // The set of BCP 47 language tags the host runtime knows about, sourced from
    // the .NET globalization data (ICU/CLDR). Used as AvailableLocales for the
    // BestAvailableLocale fallback walk in LookupSupportedLocales.
    private static HashSet<string> availableLocales;

    private static HashSet<string> AvailableLocales
    {
        get
        {
            if (availableLocales == null)
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                {
                    if (!string.IsNullOrEmpty(culture.Name))
                        set.Add(culture.Name);
                }

                availableLocales = set;
            }

            return availableLocales;
        }
    }

    // LookupSupportedLocales (ECMA-402): keep each requested (already
    // canonicalized) locale whose BestAvailableLocale match is defined, dropping
    // the rest (e.g. "zxx", "und"). The original requested tag — extensions and
    // all — is preserved in the result.
    private static JSValue LookupSupportedLocales(JSValue requested)
    {
        var subset = JSValue.CreateArray();
        if (requested is not JSObject list)
            return subset;

        var length = list[KeyStrings.length].UIntValue;
        for (uint i = 0; i < length; i++)
        {
            var locale = list[i];
            if (locale.IsUndefined)
                continue;

            if (IsLocaleAvailable(locale.StringValue))
                subset.AddArrayItem(JSValue.CreateString(locale.StringValue));
        }

        return subset;
    }

    // BestAvailableLocale: strip extension sequences, then progressively trim the
    // trailing subtag (skipping single-character subtags) until an available
    // locale is found or the candidate is exhausted.
    private static bool IsLocaleAvailable(string locale)
    {
        var available = AvailableLocales;
        var candidate = RemoveExtensionSequences(locale);
        while (candidate.Length > 0)
        {
            if (available.Contains(candidate))
                return true;

            var pos = candidate.LastIndexOf('-');
            if (pos < 0)
                return false;

            if (pos >= 2 && candidate[pos - 2] == '-')
                pos -= 2;

            candidate = candidate.Substring(0, pos);
        }

        return false;
    }

    // Drop every subtag from the first singleton (length-1) subtag onward, i.e.
    // the Unicode ("-u-"), transform ("-t-") and private ("-x-") extension
    // sequences, leaving the language/script/region/variant core for matching.
    private static string RemoveExtensionSequences(string locale)
    {
        var parts = locale.Split('-');
        var end = parts.Length;
        for (var i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length == 1)
            {
                end = i;
                break;
            }
        }

        return end == parts.Length ? locale : string.Join("-", parts, 0, end);
    }

    private static JSObject CoerceOptionsToObject(JSValue options)
    {
        if (options.IsUndefined)
            return null;

        if (options is JSObject optionsObject)
            return optionsObject;

        if (options.IsNull)
            throw JSEngine.NewTypeError("Cannot convert null to object");

        // CoerceOptionsToObject: a defined non-object options argument is boxed
        // with ToObject. The wrapper has no own option properties, but GetOption
        // still walks its prototype chain, so an inherited accessor (e.g. a
        // localeMatcher getter installed on Object.prototype) is observed exactly
        // as the spec requires.
        return JSObject.CreatePrimitiveObject(options) as JSObject
            ?? throw JSEngine.NewTypeError("Cannot convert options to object");
    }

    private static JSFunction CreateGetCanonicalLocalesFunction()
        => new(static (in Arguments a) => CanonicalizeLocaleList(a.Get1()),
            "getCanonicalLocales",
            "function getCanonicalLocales() { [native code] }",
            length: 1,
            createPrototype: false);

    private static JSFunction CreateDurationFormatConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) =>
            {
                var options = ValidateConstructorArguments("DurationFormat", in a, coerceOptions: false);
                var locale = ResolveLocale(a.Get1());
                return new JSIntlDurationFormat(locale, options);
            },
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
            var options = ValidateConstructorArguments("ListFormat", in a, coerceOptions: false);
            var locale = ResolveLocale(a.Get1());
            var type = GetOption(options, TypeKey, ["conjunction", "disjunction", "unit"], false, "conjunction");
            var style = GetOption(options, StyleKey, ["long", "short", "narrow"], false, "long");
            return new JSIntlListFormat(locale, type, style);
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

        // Accessor getters on Intl.Locale.prototype. Each is a configurable,
        // non-enumerable accessor whose getter function is named "get <name>".
        void AddLocaleGetter(string name, JSFunctionDelegate getter)
            => constructor.prototype.FastAddProperty(
                KeyStrings.GetOrCreate(name),
                new JSFunction(getter, "get " + name, $"function get {name}() {{ [native code] }}", createPrototype: false, length: 0),
                null,
                JSPropertyAttributes.ConfigurableProperty);

        AddLocaleGetter("baseName", JSIntlLocale.BaseNamePrototype);
        AddLocaleGetter("calendar", JSIntlLocale.CalendarPrototype);
        AddLocaleGetter("caseFirst", JSIntlLocale.CaseFirstPrototype);
        AddLocaleGetter("collation", JSIntlLocale.CollationPrototype);
        AddLocaleGetter("firstDayOfWeek", JSIntlLocale.FirstDayOfWeekPrototype);
        AddLocaleGetter("hourCycle", JSIntlLocale.HourCyclePrototype);
        AddLocaleGetter("language", JSIntlLocale.LanguagePrototype);
        AddLocaleGetter("numberingSystem", JSIntlLocale.NumberingSystemPrototype);
        AddLocaleGetter("numeric", JSIntlLocale.NumericPrototype);
        AddLocaleGetter("region", JSIntlLocale.RegionPrototype);
        AddLocaleGetter("script", JSIntlLocale.ScriptPrototype);
        AddLocaleGetter("variants", JSIntlLocale.VariantsPrototype);

        SetIntlToStringTag(constructor, "Locale");
        return constructor;
    }

    private static JSFunction CreateSegmenterConstructor()
    {
        var constructor = new JSFunction((in Arguments a) =>
        {
            var options = ValidateConstructorArguments("Segmenter", in a, coerceOptions: false);
            var locale = ResolveLocale(a.Get1());
            var granularity = GetOption(options, GranularityKey, ["grapheme", "word", "sentence"], false, "grapheme");
            return new JSIntlSegmenter(locale, granularity);
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
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlDateTimeFormat.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddProperty(FormatKey,
            new JSFunction(static (in Arguments a) =>
            {
                if (a.This is not JSIntlDateTimeFormat @this)
                    throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.format called on incompatible receiver");

                var format = new JSFunction((in Arguments inner) => @this.Format(in inner), "format", "function format() { [native code] }", createPrototype: false, length: 1);
                format.SetNameProperty(string.Empty);
                return format;
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
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlNumberFormat.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("formatToParts"),
            new JSFunction(JSIntlNumberFormat.FormatToPartsPrototype, "formatToParts", "function formatToParts() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        if (constructor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(FormatKey.ToString(), FormatKey)).IsUndefined)
        {
            constructor.prototype.FastAddProperty(FormatKey,
                new JSFunction(static (in Arguments a) =>
                {
                    if (a.This is not JSIntlNumberFormat @this)
                        throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.format called on incompatible receiver");

                    var format = new JSFunction((in Arguments inner) => @this.Format(in inner), "format", "function format() { [native code] }", createPrototype: false, length: 1);
                    format.SetNameProperty(string.Empty);
                    return format;
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

    private static JSFunction CreateCollatorConstructor()
    {
        var constructor = new JSFunction(static (in Arguments a) => new JSIntlCollator(in a),
            "Collator",
            "function Collator() { [native code] }",
            length: 0);
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddProperty(KeyStrings.GetOrCreate("compare"),
            new JSFunction(static (in Arguments a) =>
            {
                if (a.This is not JSIntlCollator @this)
                    throw JSEngine.NewTypeError("Intl.Collator.prototype.compare called on incompatible receiver");

                var compare = new JSFunction((in Arguments inner) => @this.Compare(in inner), "compare", "function compare() { [native code] }", createPrototype: false, length: 2);
                compare.SetNameProperty(string.Empty);
                return compare;
            },
                "get compare",
                "function get compare() { [native code] }",
                createPrototype: false,
                length: 0),
            null,
            JSPropertyAttributes.ConfigurableProperty);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlCollator.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        SetIntlToStringTag(constructor, "Collator");
        return constructor;
    }

    internal static JSObject ValidateConstructorArguments(string name, in Arguments a, bool requireNew = true, bool coerceOptions = true)
    {
        // Intl.NumberFormat, Intl.DateTimeFormat and Intl.Collator are legacy
        // constructors (ECMA-402): they may be called as ordinary functions
        // without `new`, in which case they still construct and return an
        // instance. The remaining Intl constructors require `new`.
        if (requireNew && JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"Intl.{name} requires 'new'");

        ValidateLocalesArgument(a.Get1());
        var options = ValidateOptionsArgument(a.GetAt(1), coerceOptions);
        // Every Intl service constructor reads localeMatcher first (right after
        // coercing the options object) via GetOption(..., «lookup, best fit», ...),
        // which is a RangeError for any other value. Validate it centrally so all
        // constructors reject an invalid localeMatcher.
        _ = GetOption(options, LocaleMatcherKey, ["lookup", "best fit"], false, "best fit");
        return options;
    }

    internal static string ValidateLocaleConstructorArguments(in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Intl.Locale requires 'new'");

        var tag = a.Get1();
        if (!tag.IsString && !tag.IsObject)
            throw JSEngine.NewTypeError("Locale tag must be a string or object");

        var tagString = tag.StringValue;
        // ValidateLanguageTag also canonicalizes (e.g. grandfathered "art-lojban"
        // -> "jbo"); the canonical form is what the Locale stores and reports.
        var canonicalTag = ValidateLanguageTag(tagString);
        ValidateLocaleOptions(canonicalTag, ValidateOptionsArgument(a.GetAt(1)));
        return canonicalTag;
    }

    internal static JSObject ValidateOptionsArgument(JSValue options, bool coerce = true)
    {
        if (options.IsUndefined)
            return null;

        if (options is JSObject optionsObject)
            return optionsObject;

        // GetOptionsObject (ECMA-402): used by Intl.ListFormat, Intl.Segmenter,
        // Intl.DisplayNames and Intl.DurationFormat. A defined non-object options
        // argument is a TypeError (it is not coerced).
        if (!coerce)
            throw JSEngine.NewTypeError("Options must be an object or undefined");

        // CoerceOptionsToObject: a defined non-object options argument is coerced
        // with ToObject (primitives box into wrapper objects; null throws).
        if (options.IsNull)
            throw JSEngine.NewTypeError("Cannot convert undefined or null to object");

        return JSObject.CreatePrimitiveObject(options) as JSObject
            ?? throw JSEngine.NewTypeError("Options must be an object");
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

        // A single Intl.Locale (it has an [[InitializedLocale]] internal slot) is
        // treated as a one-element list using its [[Locale]] tag — never ToString.
        if (locales is JSIntlLocale singleLocale)
        {
            result.AddArrayItem(JSValue.CreateString(singleLocale.Tag));
            return result;
        }

        // Any other value (Number, Boolean, Symbol, …) is coerced with ToObject
        // per CanonicalizeLocaleList step "Else, let O be ? ToObject(locales)".
        // The resulting wrapper has no own "length", so the list comes out empty.
        if (locales is not JSObject localesObject)
        {
            localesObject = JSObject.CreatePrimitiveObject(locales) as JSObject
                ?? throw JSEngine.NewTypeError("Cannot convert locale list to object");
        }

        var lengthValue = localesObject[KeyStrings.length];
        if (lengthValue.IsUndefined)
            return result;

        var length = lengthValue.UIntValue;
        HashSet<string> seen = null;
        for (uint i = 0; i < length; i++)
        {
            if (!localesObject.HasProperty(JSValue.CreateString(i.ToString())).BooleanValue)
                continue;

            var locale = localesObject[i];

            string canonical;
            if (locale is JSIntlLocale elementLocale)
            {
                // [[InitializedLocale]] element: use the [[Locale]] slot, not ToString.
                canonical = elementLocale.Tag;
            }
            else
            {
                if (locale.IsUndefined || locale.IsNull || locale.IsBoolean || locale.IsNumber || locale.IsSymbol)
                    throw JSEngine.NewTypeError("Locale list entries must be strings or objects");

                canonical = ValidateLanguageTag(locale.StringValue);
            }
            seen ??= new HashSet<string>(StringComparer.Ordinal);
            if (seen.Add(canonical))
                result.AddArrayItem(JSValue.CreateString(canonical));
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

        // CanonicalizeUnicodeLocaleId: a regular grandfathered tag is replaced
        // wholesale by its preferred form (e.g. "art-lojban" -> "jbo").
        if (RegularGrandfatheredMappings.TryGetValue(tag, out var preferred))
            return preferred;

        return CanonicalizeLanguageTagCase(tag);
    }

    // CanonicalizeUnicodeLocaleId case folding (UTS #35 §3.2.1): the language
    // subtag is lowercased, a script subtag is title-cased, an alphabetic region
    // subtag is uppercased, and every other subtag — extlang, variants, and the
    // contents of singleton extension / privateuse sequences — is lowercased.
    // (Alias substitution and extension keyword reordering are not applied.)
    private static string CanonicalizeLanguageTagCase(string tag)
    {
        var subtags = tag.Split('-');
        var inExtension = false;

        for (var i = 0; i < subtags.Length; i++)
        {
            var subtag = subtags[i];

            if (i == 0)
            {
                // Leading subtag is the language (or, for a wholly-privateuse
                // tag, the "x" singleton); either way it is lowercased.
                subtags[i] = subtag.ToLowerInvariant();
                continue;
            }

            if (subtag.Length == 1)
            {
                // A singleton ('u', 't', 'x', …) opens an extension or privateuse
                // sequence; it and everything after it is lowercased.
                subtags[i] = subtag.ToLowerInvariant();
                inExtension = true;
                continue;
            }

            if (inExtension)
            {
                subtags[i] = subtag.ToLowerInvariant();
                continue;
            }

            if (subtag.Length == 4 && IsAllAlpha(subtag))
                // script -> Titlecase
                subtags[i] = char.ToUpperInvariant(subtag[0]) + subtag.Substring(1).ToLowerInvariant();
            else if (subtag.Length == 2 && IsAllAlpha(subtag))
                // alphabetic region -> uppercase
                subtags[i] = subtag.ToUpperInvariant();
            else
                // extlang, variant, numeric region -> lowercase
                subtags[i] = subtag.ToLowerInvariant();
        }

        return string.Join("-", subtags);
    }

    private static bool IsAllAlpha(string value)
    {
        foreach (var c in value)
        {
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                return false;
        }

        return true;
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

    internal static JSIntlNumberFormatResolved ValidateNumberFormatOptions(JSObject options)
    {
        if (options == null)
            return new JSIntlNumberFormatResolved("standard", "auto", null, null);

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

        // unitDisplay is read (and validated) regardless of style, but the
        // resolved slot only exists when style is "unit".
        var unitDisplay = GetOption(options, UnitDisplayKey, UnitDisplayValues, false, "short");
        if (style != "unit")
            unitDisplay = null;

        // notation precedes compactDisplay (spec order; observed by the
        // compactDisplay getter call-order tests). compactDisplay is always
        // validated, but only reflected when notation is "compact".
        var notation = GetOption(options, NotationKey, NotationValues, false, "standard");

        // SetNumberFormatDigitOptions: read the digit options from the bag exactly
        // once, in spec order (minimumIntegerDigits, minimumFractionDigits,
        // maximumFractionDigits, minimumSignificantDigits, maximumSignificantDigits),
        // and snapshot them so later format/resolvedOptions calls reuse the values
        // instead of re-invoking option getters.
        var digitOptions = SnapshotDigitOptions(options);

        var compactDisplay = GetOption(options, CompactDisplayKey, CompactDisplayValues, false, "short");
        if (notation != "compact")
            compactDisplay = null;

        var signDisplay = GetOption(options, SignDisplayKey, SignDisplayValues, false, "auto");

        ObserveOptions(options, RoundingIncrementKey, RoundingModeKey, RoundingPriorityKey, TrailingZeroDisplayKey);

        return new JSIntlNumberFormatResolved(notation, signDisplay, compactDisplay, unitDisplay)
        {
            DigitOptions = digitOptions,
        };
    }

    // Reads the digit-related options from the bag once (in spec order) and stores
    // them as plain data properties, so subsequent reads observe construction-time
    // values without re-triggering option getters. Absent options are left out so
    // callers apply their own (style-dependent) defaults.
    private static JSObject SnapshotDigitOptions(JSObject options)
    {
        if (options == null)
            return null;

        var snapshot = new JSObject();
        foreach (var name in new[]
        {
            "minimumIntegerDigits",
            "minimumFractionDigits",
            "maximumFractionDigits",
            "minimumSignificantDigits",
            "maximumSignificantDigits",
        })
        {
            var key = KeyStrings.GetOrCreate(name);
            var value = options[key];
            if (value != null && !value.IsUndefined)
                snapshot.FastAddValue(key, value, JSPropertyAttributes.EnumerableConfigurableValue);
        }

        return snapshot;
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

        // localeMatcher is validated centrally in ValidateConstructorArguments.
        var style = GetOption(options, StyleKey, ["narrow", "short", "long"], false, "long");
        var type = GetOption(options, TypeKey, ["language", "region", "script", "currency", "calendar", "dateTimeField"], true);
        var fallback = GetOption(options, FallbackKey, ["code", "none"], false, "code");
        var languageDisplay = GetOption(options, LanguageDisplayKey, ["dialect", "standard"], false, "dialect");
        return new JSIntlDisplayNamesOptions(style, type, fallback, languageDisplay);
    }

    internal static string GetOption(JSObject options, KeyString key, string[] allowedValues, bool required, string defaultValue = null)
    {
        // A missing (undefined) options argument is normalized to null upstream;
        // treat every option as absent so defaults/required handling still apply.
        var value = options == null ? JSUndefined.Value : options[key];
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

    // ECMA-402 GetNumberOption: reads an integral numeric option in [min, max].
    // Returns null when absent (so the caller can apply its own default), and
    // throws a RangeError for NaN or out-of-range values.
    internal static int? GetNumberOption(JSObject options, KeyString key, int min, int max)
    {
        var value = options == null ? JSUndefined.Value : options[key];
        if (value.IsUndefined)
            return null;

        var number = value.DoubleValue;
        if (double.IsNaN(number) || number < min || number > max)
            throw JSEngine.NewRangeError($"Invalid {key} option");

        return (int)Math.Floor(number);
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

        // fractionalSecondDigits \u2208 {1, 2, 3} (GetNumberOption with min 1, max 3);
        // an out-of-range value (e.g. 0 or 4) is a RangeError.
        var fractionalSecondDigits = options[KeyStrings.GetOrCreate("fractionalSecondDigits")];
        if (!fractionalSecondDigits.IsUndefined)
        {
            var digits = fractionalSecondDigits.DoubleValue;
            if (double.IsNaN(digits) || digits < 1 || digits > 3)
                throw JSEngine.NewRangeError("fractionalSecondDigits value is out of range.");
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
    // ECMA-402 sanctioned units (singular and plural spellings) mapped to the
    // singular form used to key the CLDR relative-time patterns.
    private static readonly Dictionary<string, string> SingularUnit = new(StringComparer.Ordinal)
    {
        ["second"] = "second", ["seconds"] = "second",
        ["minute"] = "minute", ["minutes"] = "minute",
        ["hour"] = "hour", ["hours"] = "hour",
        ["day"] = "day", ["days"] = "day",
        ["week"] = "week", ["weeks"] = "week",
        ["month"] = "month", ["months"] = "month",
        ["quarter"] = "quarter", ["quarters"] = "quarter",
        ["year"] = "year", ["years"] = "year",
    };

    public JSValue Format(in Arguments args)
    {
        // ToNumber(value) precedes ToString(unit) per spec.
        var value = (args[0] ?? JSUndefined.Value).DoubleValue;
        var unit = args.GetAt(1).StringValue;

        var sb = new StringBuilder();
        foreach (var (_, partValue, _) in PartitionRelativeTimePattern(value, unit))
            sb.Append(partValue);
        return JSValue.CreateString(sb.ToString());
    }

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlRelativeTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.format called on incompatible receiver");

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlRelativeTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.formatToParts called on incompatible receiver");

        var value = (a[0] ?? JSUndefined.Value).DoubleValue;
        var unit = a.GetAt(1).StringValue;

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var unitKey = KeyStrings.GetOrCreate("unit");

        var parts = JSValue.CreateArray();
        foreach (var (type, partValue, partUnit) in @this.PartitionRelativeTimePattern(value, unit))
        {
            var part = new JSObject();
            part.FastAddValue(typeKey, JSValue.CreateString(type), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(valueKey, JSValue.CreateString(partValue), JSPropertyAttributes.EnumerableConfigurableValue);
            if (partUnit != null)
                part.FastAddValue(unitKey, JSValue.CreateString(partUnit), JSPropertyAttributes.EnumerableConfigurableValue);
            parts.AddArrayItem(part);
        }
        return parts;
    }

    // PartitionRelativeTimePattern: resolves the ordered (type, value, unit) parts
    // for a value/unit. Numeric "auto" first tries an exact phrase (e.g. "tomorrow");
    // otherwise the future/past pattern for the value's plural category is filled with
    // the locale's number parts (each tagged with the unit).
    private List<(string type, string value, string unit)> PartitionRelativeTimePattern(double value, string unitArgument)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw JSEngine.NewRangeError("Value of Intl.RelativeTimeFormat must be a finite number");

        if (!SingularUnit.TryGetValue(unitArgument, out var unit))
            throw JSEngine.NewRangeError($"Invalid unit argument for Intl.RelativeTimeFormat: {unitArgument}");

        if (numeric == "auto" && value == Math.Floor(value) && value >= int.MinValue && value <= int.MaxValue)
        {
            var exact = CldrLocaleData.GetRelativeTimeExact(locale, unit, style, (int)value);
            if (exact != null)
                return [("literal", exact, null)];
        }

        // -0 and negative values are "past"; +0 and positive values are "future".
        var isPast = value < 0 || (value == 0 && double.IsNegative(value));
        var tense = isPast ? "past" : "future";

        var magnitude = Math.Abs(value);
        var pluralCategory = CldrLocaleData.SelectPlural(locale, "cardinal", magnitude);
        var pattern = CldrLocaleData.GetRelativeTimePattern(locale, unit, style, tense, pluralCategory);

        return FillRelativeTimePattern(pattern, unit, magnitude);
    }

    // Splits the pattern around the "{0}" placeholder, emitting the surrounding
    // text as "literal" parts and the formatted number's parts (each carrying the
    // unit) in its place.
    private List<(string type, string value, string unit)> FillRelativeTimePattern(string pattern, string unit, double magnitude)
    {
        var result = new List<(string, string, string)>();
        var placeholder = pattern.IndexOf("{0}", StringComparison.Ordinal);
        if (placeholder < 0)
        {
            if (pattern.Length > 0)
                result.Add(("literal", pattern, null));
            return result;
        }

        var prefix = pattern[..placeholder];
        var suffix = pattern[(placeholder + 3)..];
        if (prefix.Length > 0)
            result.Add(("literal", prefix, null));

        var numberArgs = new Arguments(JSUndefined.Value, JSValue.CreateString(locale));
        var numberFormat = new JSIntlNumberFormat(in numberArgs);
        foreach (var (type, value) in numberFormat.ComputeFormatParts(JSValue.CreateNumber(magnitude)))
            result.Add((type, value, unit));

        if (suffix.Length > 0)
            result.Add(("literal", suffix, null));

        return result;
    }

    private readonly string locale;
    private readonly string style;
    private readonly string numeric;

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlRelativeTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), JSValue.CreateString(@this.style));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numeric"), JSValue.CreateString(@this.numeric));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), JSValue.CreateString("latn"));
        return result;
    }

    public JSIntlRelativeTimeFormat(in Arguments a) : this()
    {
        var options = JSIntl.ValidateConstructorArguments("RelativeTimeFormat", in a);
        locale = JSIntl.ResolveLocale(a.Get1());
        style = JSIntl.GetOption(options, KeyStrings.GetOrCreate("style"), ["long", "short", "narrow"], false, "long");
        numeric = JSIntl.GetOption(options, KeyStrings.GetOrCreate("numeric"), ["always", "auto"], false, "always");
    }

    private JSIntlRelativeTimeFormat() : base(CurrentPrototype("RelativeTimeFormat")) { }

    private static JSObject CurrentPrototype(string name)
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate(name)] as JSFunction)?.prototype
            : null;
}

public sealed class JSIntlSegmenter : JSObject
{
    private readonly string locale;

    internal string Granularity { get; }

    public JSIntlSegmenter(string locale = "en-US", string granularity = "grapheme") : base(CurrentPrototype())
    {
        this.locale = locale;
        Granularity = granularity;
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegmenter @this)
            throw JSEngine.NewTypeError("Intl.Segmenter.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("granularity"), JSValue.CreateString(@this.Granularity));
        return result;
    }

    public static JSValue SegmentPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegmenter segmenter)
            throw JSEngine.NewTypeError("Intl.Segmenter.prototype.segment called on incompatible receiver");

        var input = a.Get1().StringValue;
        return new JSIntlSegments(input, segmenter.Granularity);
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("Segmenter")] as JSFunction)?.prototype
            : null;
}

/// <summary>
/// The object returned by Intl.Segmenter.prototype.segment — a %Segments%
/// instance. Exposes <c>containing(index)</c> and is iterable, yielding a
/// Segment Data object per segment.
///
/// Grapheme segmentation uses the BCL Unicode text-element algorithm
/// (UAX #29). Word segmentation uses a simplified rule set (runs of
/// letters/digits form words; whitespace runs group; everything else breaks).
/// Sentence segmentation treats the whole string as a single segment. These
/// approximations are sufficient for the spec'd shape and the common cases
/// exercised by the test suite without bundling a full ICU break engine.
/// </summary>
public sealed class JSIntlSegments : JSObject
{
    private static readonly KeyString SegmentKey = KeyStrings.GetOrCreate("segment");
    private static readonly KeyString IndexKey = KeyStrings.GetOrCreate("index");
    private static readonly KeyString InputKey = KeyStrings.GetOrCreate("input");
    private static readonly KeyString IsWordLikeKey = KeyStrings.GetOrCreate("isWordLike");

    private readonly string _input;
    private readonly string _granularity;
    private List<(int start, int length)> _segments;

    public JSIntlSegments(string input, string granularity)
    {
        _input = input;
        _granularity = granularity;
        FastAddValue(
            KeyStrings.GetOrCreate("containing"),
            new JSFunction(ContainingPrototype, "containing", "function containing() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
    }

    private List<(int start, int length)> Segments => _segments ??= Compute(_input, _granularity);

    public static JSValue ContainingPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegments segments)
            throw JSEngine.NewTypeError("%Segments.prototype%.containing called on incompatible receiver");

        // ToInteger(index): ToNumber then NaN/±0 → 0, otherwise truncate toward zero.
        var number = a.Get1().DoubleValue;
        var n = double.IsNaN(number) ? 0 : Math.Truncate(number);

        if (n < 0 || n >= segments._input.Length)
            return JSUndefined.Value;

        var index = (int)n;
        foreach (var (start, length) in segments.Segments)
        {
            if (index >= start && index < start + length)
                return segments.CreateSegmentDataObject(start, length);
        }

        return JSUndefined.Value;
    }

    public override IElementEnumerator GetIterableEnumerator() => new SegmentEnumerator(this);

    private JSObject CreateSegmentDataObject(int start, int length)
    {
        var result = new JSObject();
        result.FastAddValue(SegmentKey, JSValue.CreateString(_input.Substring(start, length)), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(IndexKey, JSValue.CreateNumber(start), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(InputKey, JSValue.CreateString(_input), JSPropertyAttributes.EnumerableConfigurableValue);
        if (_granularity == "word")
        {
            var isWordLike = IsWordLike(_input.Substring(start, length));
            result.FastAddValue(IsWordLikeKey, isWordLike ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        }

        return result;
    }

    private static List<(int start, int length)> Compute(string input, string granularity) => granularity switch
    {
        "word" => ComputeWords(input),
        "sentence" => ComputeSentences(input),
        _ => ComputeGraphemes(input),
    };

    private static List<(int start, int length)> ComputeGraphemes(string input)
    {
        var list = new List<(int, int)>();
        var e = StringInfo.GetTextElementEnumerator(input);
        while (e.MoveNext())
            list.Add((e.ElementIndex, ((string)e.Current).Length));

        return list;
    }

    private static List<(int start, int length)> ComputeSentences(string input)
    {
        // Approximation: the whole string is a single sentence. Sufficient for
        // the spec'd shape; full UAX #29 sentence breaking is not implemented.
        var list = new List<(int, int)>();
        if (input.Length > 0)
            list.Add((0, input.Length));

        return list;
    }

    private static List<(int start, int length)> ComputeWords(string input)
    {
        var graphemes = ComputeGraphemes(input);
        var list = new List<(int, int)>();

        var i = 0;
        while (i < graphemes.Count)
        {
            var (start, length) = graphemes[i];
            var category = WordCategory(input, start, length);

            var j = i + 1;
            while (j < graphemes.Count)
            {
                var (nextStart, nextLength) = graphemes[j];
                if (!Joinable(category, WordCategory(input, nextStart, nextLength)))
                    break;

                length = nextStart + nextLength - start;
                j++;
            }

            list.Add((start, length));
            i = j;
        }

        return list;
    }

    private enum WordCat { Letter, Number, Space, Other }

    private static WordCat WordCategory(string input, int start, int length)
    {
        foreach (var rune in input.Substring(start, length).EnumerateRunes())
        {
            if (Rune.IsLetter(rune))
                return WordCat.Letter;
            if (Rune.IsDigit(rune))
                return WordCat.Number;
            if (Rune.IsWhiteSpace(rune))
                return WordCat.Space;
            return WordCat.Other;
        }

        return WordCat.Other;
    }

    private static bool Joinable(WordCat a, WordCat b)
    {
        if (a == WordCat.Space && b == WordCat.Space)
            return true;

        var aWord = a is WordCat.Letter or WordCat.Number;
        var bWord = b is WordCat.Letter or WordCat.Number;
        return aWord && bWord;
    }

    private static bool IsWordLike(string segment)
    {
        foreach (var rune in segment.EnumerateRunes())
        {
            if (Rune.IsLetter(rune) || Rune.IsDigit(rune))
                return true;
        }

        return false;
    }

    private sealed class SegmentEnumerator(JSIntlSegments segments) : IElementEnumerator
    {
        private int _position;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (MoveNext(out value))
            {
                hasValue = true;
                index = (uint)(_position - 1);
                return true;
            }

            hasValue = false;
            index = 0;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            var all = segments.Segments;
            if (_position < all.Count)
            {
                var (start, length) = all[_position++];
                value = segments.CreateSegmentDataObject(start, length);
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
            => MoveNext(out var value) ? value : @default;
    }
}

public sealed class JSIntlDurationFormat : JSObject
{
    // Units in ECMA-402 processing order, with their singular NumberFormat unit name.
    private static readonly string[] Units =
        { "years", "months", "weeks", "days", "hours", "minutes", "seconds", "milliseconds", "microseconds", "nanoseconds" };
    private static readonly string[] SingularUnits =
        { "year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    private static readonly string[] DateStyles = { "long", "short", "narrow" };
    private static readonly string[] TimeStyles = { "long", "short", "narrow", "numeric", "2-digit" };
    private static readonly string[] SubSecondStyles = { "long", "short", "narrow", "numeric" };
    private static readonly string[] StyleValues = { "long", "short", "narrow", "digital" };
    private static readonly string[] DisplayValues = { "auto", "always" };

    private readonly string locale;
    private readonly string numberingSystem;       // null => NumberFormat default
    private readonly string style;                 // long | short | narrow | digital
    private readonly string[] unitStyle = new string[Units.Length];
    private readonly string[] unitDisplay = new string[Units.Length];
    private readonly int? fractionalDigits;

    private JSIntlDurationFormat() : base(CurrentPrototype()) { }

    public JSIntlDurationFormat(string locale, JSObject options) : this()
    {
        this.locale = locale;
        style = JSIntl.GetOption(options, KeyStrings.GetOrCreate("style"), StyleValues, false, "short");

        var ns = options == null ? JSUndefined.Value : options[KeyStrings.GetOrCreate("numberingSystem")];
        numberingSystem = ns.IsUndefined ? null : ns.StringValue;

        string prevStyle = null;
        for (var i = 0; i < Units.Length; i++)
        {
            var stylesList = i < 4 ? DateStyles : (i < 7 ? TimeStyles : SubSecondStyles);
            var digitalBase = i < 4 ? "short" : "numeric";
            (unitStyle[i], unitDisplay[i]) = GetDurationUnitOptions(options, i, stylesList, digitalBase, prevStyle);
            prevStyle = unitStyle[i];
        }

        fractionalDigits = JSIntl.GetNumberOption(options, KeyStrings.GetOrCreate("fractionalDigits"), 0, 9);
    }

    // ECMA-402 GetDurationUnitOptions (resolved per-unit style/display). The
    // internal "fractional" formatting of sub-second units is handled at format
    // time (durationToFractional) rather than stored, so the resolved sub-second
    // style stays "numeric" — which is exactly what the spec partition algorithm
    // reads back to decide whether to fold sub-seconds into a fractional value.
    private (string style, string display) GetDurationUnitOptions(
        JSObject options, int index, string[] stylesList, string digitalBase, string prevStyle)
    {
        var unit = Units[index];
        var resolved = JSIntl.GetOption(options, KeyStrings.GetOrCreate(unit), stylesList, false, null);
        var displayDefault = "always";
        var isTimeUnit = unit is "hours" or "minutes" or "seconds";

        if (resolved == null)
        {
            if (style == "digital")
            {
                if (!isTimeUnit)
                    displayDefault = "auto";
                resolved = digitalBase;
            }
            else
            {
                displayDefault = "auto";
                resolved = prevStyle is "numeric" or "2-digit" ? "numeric" : style;
            }
        }

        // A numeric minutes/seconds chained after a numeric/2-digit unit is shown
        // as a zero-padded 2-digit value joined by the time separator (e.g. 1:00:30).
        if (prevStyle is "numeric" or "2-digit" && unit is "minutes" or "seconds" && resolved == "numeric")
            resolved = "2-digit";

        var display = JSIntl.GetOption(options, KeyStrings.GetOrCreate(unit + "Display"), DisplayValues, false, displayDefault);
        return (resolved, display);
    }

    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.format called on incompatible receiver");

        var duration = a.Get1();
        ValidateDurationArgument(duration);
        if (duration is not JSObject durationObject)
            return JSValue.CreateString(string.Empty);

        return JSValue.CreateString(self.Format(durationObject));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.formatToParts called on incompatible receiver");

        var duration = a.Get1();
        ValidateDurationArgument(duration);
        var parts = JSValue.CreateArray();
        if (duration is not JSObject durationObject)
            return parts;

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var unitKey = KeyStrings.GetOrCreate("unit");
        foreach (var (type, value, unit) in self.FormatToParts(durationObject))
        {
            var part = new JSObject();
            part.FastAddValue(typeKey, JSValue.CreateString(type), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(valueKey, JSValue.CreateString(value), JSPropertyAttributes.EnumerableConfigurableValue);
            // Parts produced from a unit's NumberFormat carry the singular unit
            // name; list separators and ListFormat literals leave it absent.
            if (unit != null)
                part.FastAddValue(unitKey, JSValue.CreateString(unit), JSPropertyAttributes.EnumerableConfigurableValue);
            parts.AddArrayItem(part);
        }
        return parts;
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        void Put(string key, JSValue value) =>
            result.FastAddValue(KeyStrings.GetOrCreate(key), value, JSPropertyAttributes.EnumerableConfigurableValue);

        Put("locale", JSValue.CreateString(self.locale ?? "en"));
        Put("numberingSystem", JSValue.CreateString(self.numberingSystem ?? "latn"));
        Put("style", JSValue.CreateString(self.style));
        for (var i = 0; i < Units.Length; i++)
        {
            Put(Units[i], JSValue.CreateString(self.unitStyle[i]));
            Put(Units[i] + "Display", JSValue.CreateString(self.unitDisplay[i]));
        }
        if (self.fractionalDigits is { } digits)
            Put("fractionalDigits", JSValue.CreateNumber(digits));
        return result;
    }

    // Implements PartitionDurationFormatPattern, returning the flattened parts.
    // Each tuple's `unit` is the singular duration unit for parts produced from a
    // unit's NumberFormat, or null for time separators and ListFormat literals.
    private List<(string type, string value, string unit)> FormatToParts(JSObject durationObject)
    {
        var values = new double[Units.Length];
        for (var i = 0; i < Units.Length; i++)
        {
            var v = durationObject[KeyStrings.GetOrCreate(Units[i])];
            values[i] = v.IsUndefined ? 0 : v.DoubleValue;
        }

        var anyNegative = false;
        foreach (var v in values)
            anyNegative |= v < 0;

        // Each element of `segments` is the list of parts for one ListFormat element.
        var segments = new List<List<(string type, string value, string unit)>>();
        var needSeparator = false;
        var displayNegativeSign = true;

        for (var i = 0; i < Units.Length; i++)
        {
            var unit = Units[i];
            var unitStyleValue = unitStyle[i];
            var display = unitDisplay[i];
            var value = values[i];
            var combineFractional = false;

            // Numeric seconds/milliseconds/microseconds fold the finer sub-second
            // units into a single fractional value when the next unit is numeric.
            if ((unit is "seconds" or "milliseconds" or "microseconds") && unitStyle[i + 1] == "numeric")
            {
                value = DurationToFractional(values, unit == "seconds" ? 9 : unit == "milliseconds" ? 6 : 3);
                combineFractional = true;
            }

            var displayRequired = false;
            if (unit == "minutes" && needSeparator)
            {
                displayRequired = unitDisplay[6] == "always"
                    || values[6] != 0 || values[7] != 0 || values[8] != 0 || values[9] != 0;
            }

            if (value != 0 || display != "auto" || displayRequired)
            {
                var signNever = false;
                if (displayNegativeSign)
                {
                    displayNegativeSign = false;
                    if (value == 0 && anyNegative)
                        value = -0.0;
                }
                else
                {
                    signNever = true;
                }

                var numberParts = FormatNumberParts(SingularUnits[i], value, unitStyleValue, combineFractional, signNever);
                var unitName = SingularUnits[i];

                if (!needSeparator)
                {
                    var segment = new List<(string, string, string)>();
                    foreach (var (type, val) in numberParts)
                        segment.Add((type, val, unitName));
                    if (unitStyleValue is "2-digit" or "numeric")
                        needSeparator = true;
                    segments.Add(segment);
                }
                else
                {
                    var segment = segments[^1];
                    segment.Add(("literal", ":", null));
                    foreach (var (type, val) in numberParts)
                        segment.Add((type, val, unitName));
                }
            }

            if (combineFractional)
                break;
        }

        // Join the per-unit element strings with Intl.ListFormat (type "unit").
        var listStyle = style == "digital" ? "short" : style;
        var lf = new JSIntlListFormat(locale, "unit", listStyle);
        var elementStrings = new List<string>(segments.Count);
        foreach (var segment in segments)
        {
            var sb = new StringBuilder();
            foreach (var (_, value, _) in segment)
                sb.Append(value);
            elementStrings.Add(sb.ToString());
        }

        var result = new List<(string type, string value, string unit)>();
        var elementIndex = 0;
        foreach (var (type, value) in lf.FormatPartsForUnits(elementStrings))
        {
            if (type == "element")
                result.AddRange(segments[elementIndex++]);
            else
                result.Add((type, value, null));
        }
        return result;
    }

    private string Format(JSObject durationObject)
    {
        var sb = new StringBuilder();
        foreach (var (_, value, _) in FormatToParts(durationObject))
            sb.Append(value);
        return sb.ToString();
    }

    // Computes the value of a unit plus its finer sub-second units as a decimal,
    // mirroring the reference durationToFractional. Magnitudes used by the format
    // tests are small, so double precision is sufficient.
    private static double DurationToFractional(double[] values, int exponent)
    {
        // values indices: seconds=6, milliseconds=7, microseconds=8, nanoseconds=9
        double seconds = values[6], milliseconds = values[7], microseconds = values[8], nanoseconds = values[9];

        if (exponent == 9 && milliseconds == 0 && microseconds == 0 && nanoseconds == 0)
            return seconds;
        if (exponent == 6 && microseconds == 0 && nanoseconds == 0)
            return milliseconds;
        if (exponent == 3 && nanoseconds == 0)
            return microseconds;

        double ns = nanoseconds;
        if (exponent >= 9)
            ns += seconds * 1_000_000_000d;
        if (exponent >= 6)
            ns += milliseconds * 1_000_000d;
        if (exponent >= 3)
            ns += microseconds * 1_000d;

        return ns / Math.Pow(10, exponent);
    }

    private List<(string type, string value)> FormatNumberParts(string singularUnit, double value, string unitStyleValue, bool fractional, bool signNever)
    {
        var options = new JSObject();
        void Set(string key, JSValue v) => options.FastAddValue(KeyStrings.GetOrCreate(key), v, JSPropertyAttributes.EnumerableConfigurableValue);

        if (numberingSystem != null)
            Set("numberingSystem", JSValue.CreateString(numberingSystem));

        if (unitStyleValue == "2-digit")
            Set("minimumIntegerDigits", JSValue.CreateNumber(2));

        if (unitStyleValue is not ("numeric" or "2-digit"))
        {
            Set("style", JSValue.CreateString("unit"));
            Set("unit", JSValue.CreateString(singularUnit));
            Set("unitDisplay", JSValue.CreateString(unitStyleValue));
        }
        else
        {
            Set("useGrouping", JSValue.BooleanFalse);
        }

        if (fractional)
        {
            Set("maximumFractionDigits", JSValue.CreateNumber(fractionalDigits ?? 9));
            Set("minimumFractionDigits", JSValue.CreateNumber(fractionalDigits ?? 0));
            Set("roundingMode", JSValue.CreateString("trunc"));
        }

        if (signNever)
            Set("signDisplay", JSValue.CreateString("never"));

        var args = new Arguments(JSUndefined.Value, JSValue.CreateString(locale), options);
        var nf = new JSIntlNumberFormat(in args);
        return nf.ComputeFormatParts(JSValue.CreateNumber(value));
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("DurationFormat")] as JSFunction)?.prototype
            : null;

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
    private readonly string locale;
    private readonly string type;
    private readonly string style;

    public JSIntlListFormat(string locale = "en-US", string type = "conjunction", string style = "long")
    {
        this.locale = locale;
        this.type = type;
        this.style = style;
    }

    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat @this)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.format called on incompatible receiver");

        var items = StringListFromIterable(a.Get1());
        return JSValue.CreateString(@this.FormatList(items));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat @this)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.formatToParts called on incompatible receiver");

        // Minimal formatToParts: a single "element" part per list item plus
        // "literal" parts for the surrounding separators. We re-derive the
        // literals by diffing the full formatted string against the elements.
        var items = StringListFromIterable(a.Get1());
        var formatted = @this.FormatList(items);
        var result = JSValue.CreateArray();
        var parts = (JSObject)result;
        uint index = 0;
        int cursor = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var pos = formatted.IndexOf(items[i], cursor, StringComparison.Ordinal);
            if (pos < 0)
                pos = cursor;
            if (pos > cursor)
                parts.SetPropertyOrThrow(JSValue.CreateNumber(index++), MakePart("literal", formatted.Substring(cursor, pos - cursor)));
            parts.SetPropertyOrThrow(JSValue.CreateNumber(index++), MakePart("element", items[i]));
            cursor = pos + items[i].Length;
        }
        if (cursor < formatted.Length)
            parts.SetPropertyOrThrow(JSValue.CreateNumber(index++), MakePart("literal", formatted.Substring(cursor)));
        return result;
    }

    // Like formatToParts, but returns ("element"|"literal", value) tuples. Used by
    // Intl.DurationFormat to interleave its own per-unit parts with the list's
    // separators (the "element" entries are replaced by the caller).
    internal List<(string type, string value)> FormatPartsForUnits(List<string> items)
    {
        var formatted = FormatList(items);
        var result = new List<(string, string)>();
        var cursor = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var pos = formatted.IndexOf(items[i], cursor, StringComparison.Ordinal);
            if (pos < 0)
                pos = cursor;
            if (pos > cursor)
                result.Add(("literal", formatted.Substring(cursor, pos - cursor)));
            result.Add(("element", items[i]));
            cursor = pos + items[i].Length;
        }
        if (cursor < formatted.Length)
            result.Add(("literal", formatted.Substring(cursor)));
        return result;
    }

    private static JSObject MakePart(string type, string value)
    {
        var part = new JSObject();
        part[KeyStrings.GetOrCreate("type")] = JSValue.CreateString(type);
        part[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(value);
        return part;
    }

    // §13.5.1 StringListFromIterable: iterate the argument requiring every
    // produced value to be a String, collecting them into a list. `undefined`
    // yields the empty list; a primitive string is iterated by code point.
    private static List<string> StringListFromIterable(JSValue list)
    {
        var result = new List<string>();
        if (list.IsUndefined)
            return result;

        var en = list.GetIterableEnumerator();
        while (en.MoveNext(out var hasValue, out var item, out var _))
        {
            if (!hasValue)
                continue;
            if (!item.IsString)
            {
                // §13.5.1 step 5.b.ii: a non-String element is an error completion
                // that must be passed through IteratorClose, so the iterator's
                // return() runs (its own abrupt completion is then suppressed).
                if (en is IReturnableEnumerator returnable)
                {
                    try { returnable.Return(); }
                    catch { /* IteratorClose suppresses a secondary completion */ }
                }

                throw JSEngine.NewTypeError("Intl.ListFormat: array element must be a string");
            }

            result.Add(item.StringValue);
        }

        return result;
    }

    // CLDR list-assembly: combine the elements using the (start, middle, end,
    // pair) patterns for this list's locale/type/style.
    internal string FormatList(List<string> items)
    {
        var count = items.Count;
        if (count == 0)
            return string.Empty;
        if (count == 1)
            return items[0];

        var (start, middle, end, pair) = GetPatterns();
        if (count == 2)
            return ApplyPattern(pair, items[0], items[1]);

        var result = ApplyPattern(end, items[count - 2], items[count - 1]);
        for (var i = count - 3; i >= 1; i--)
            result = ApplyPattern(middle, items[i], result);
        return ApplyPattern(start, items[0], result);
    }

    // Substitute {0}/{1} in a single left-to-right pass so substituted content
    // containing literal "{0}"/"{1}" is never re-expanded.
    private static string ApplyPattern(string pattern, string v0, string v1)
    {
        var sb = new StringBuilder(pattern.Length + v0.Length + v1.Length);
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '{' && i + 2 < pattern.Length && pattern[i + 2] == '}')
            {
                if (pattern[i + 1] == '0') { sb.Append(v0); i += 2; continue; }
                if (pattern[i + 1] == '1') { sb.Append(v1); i += 2; continue; }
            }
            sb.Append(pattern[i]);
        }
        return sb.ToString();
    }

    // The (start, middle, end, pair) list-assembly patterns come from the shared
    // CLDR data library (UnicodeCldr.LocaleData), generated from cldr-json — so the
    // per-locale list patterns are real CLDR data rather than a hand-coded en/es
    // approximation.
    private (string Start, string Middle, string End, string Pair) GetPatterns()
        => CldrLocaleData.GetListPattern(locale, type, style);

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat @this)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), JSValue.CreateString(@this.type));
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), JSValue.CreateString(@this.style));
        return result;
    }
}

internal sealed record JSIntlDisplayNamesOptions(string Style, string Type, string Fallback, string LanguageDisplay);

public sealed class JSIntlDisplayNames : JSObject
{
    private readonly string locale;
    private readonly JSIntlDisplayNamesOptions options;

    public JSIntlDisplayNames(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        options = JSIntl.ValidateDisplayNamesOptions(JSIntl.ValidateConstructorArguments("DisplayNames", in a, coerceOptions: false));
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
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), JSValue.CreateString(@this.options.Style));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), JSValue.CreateString(@this.options.Type));
        result.CreateDataProperty(KeyStrings.GetOrCreate("fallback"), JSValue.CreateString(@this.options.Fallback));
        if (@this.options.Type == "language")
            result.CreateDataProperty(KeyStrings.GetOrCreate("languageDisplay"), JSValue.CreateString(@this.options.LanguageDisplay));
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

    // Regions that conventionally use a 12-hour clock; everything else defaults
    // to the 24-hour cycle. Approximation sufficient for sensible defaults.
    private static readonly HashSet<string> TwelveHourRegions = new(StringComparer.Ordinal)
    {
        "US", "CA", "AU", "NZ", "IN", "PH", "PK", "BD", "EG", "MX", "CO", "SA",
    };

    // A representative IANA time-zone for a handful of common regions. Not
    // exhaustive — getTimeZones falls back to UTC for regions not listed here.
    private static readonly Dictionary<string, string> RegionPrimaryTimeZone = new(StringComparer.Ordinal)
    {
        ["US"] = "America/New_York", ["CA"] = "America/Toronto", ["GB"] = "Europe/London",
        ["DE"] = "Europe/Berlin", ["FR"] = "Europe/Paris", ["ES"] = "Europe/Madrid",
        ["IT"] = "Europe/Rome", ["RU"] = "Europe/Moscow", ["CN"] = "Asia/Shanghai",
        ["JP"] = "Asia/Tokyo", ["IN"] = "Asia/Kolkata", ["AU"] = "Australia/Sydney",
        ["BR"] = "America/Sao_Paulo", ["MX"] = "America/Mexico_City",
    };

    public JSIntlLocale(string tag = "und") : base(CurrentPrototype()) => this.tag = tag;

    // The [[Locale]] internal slot: the canonical language tag. Used by
    // CanonicalizeLocaleList to read a Locale argument without calling toString.
    internal string Tag => tag;

    // Builds the result of CreateArrayFromListAndPreferred: the preferred value
    // (when present) first, then the remaining list entries that differ from it.
    private static JSValue CreateArrayFromListAndPreferred(string preferred, params string[] list)
    {
        var array = JSValue.CreateArray();
        if (preferred != null)
            array.AddArrayItem(JSValue.CreateString(preferred));
        foreach (var item in list)
            if (preferred == null || !string.Equals(item, preferred, StringComparison.Ordinal))
                array.AddArrayItem(JSValue.CreateString(item));
        return array;
    }

    // A Unicode-extension keyword type is only a usable "preferred" value when it
    // names an explicit type (an empty/absent type is ignored).
    private static string Preferred(string keywordType)
        => string.IsNullOrEmpty(keywordType) ? null : keywordType;

    private string DefaultHourCycle()
    {
        var region = GetRegion();
        if (region != null)
            return TwelveHourRegions.Contains(region) ? "h12" : "h23";
        return GetLanguage() == "en" ? "h12" : "h23";
    }

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
        var locale = RequireLocale(in a, "getCalendars");
        return CreateArrayFromListAndPreferred(Preferred(locale.GetUnicodeKeyword("ca")), "gregory");
    }

    public static JSValue GetCollationsPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getCollations");
        var preferred = Preferred(locale.GetUnicodeKeyword("co"));
        // The spec excludes "standard" and "search" from the collation list.
        if (preferred is "standard" or "search")
            preferred = null;
        return CreateArrayFromListAndPreferred(preferred, "default");
    }

    public static JSValue GetHourCyclesPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getHourCycles");
        return CreateArrayFromListAndPreferred(Preferred(locale.GetUnicodeKeyword("hc")), locale.DefaultHourCycle());
    }

    public static JSValue GetNumberingSystemsPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getNumberingSystems");
        return CreateArrayFromListAndPreferred(Preferred(locale.GetUnicodeKeyword("nu")), "latn");
    }

    public static JSValue GetTextInfoPrototype(in Arguments a)
    {
        RequireLocale(in a, "getTextInfo");
        return new JSObject();
    }

    public static JSValue GetTimeZonesPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getTimeZones");
        var region = locale.GetRegion();

        // Per spec, getTimeZones returns undefined for a locale without a region.
        if (region == null)
            return JSValue.UndefinedValue;

        // Region→IANA time-zone data is not bundled; return a representative
        // non-empty list (a primary zone for known regions, UTC otherwise).
        var array = JSValue.CreateArray();
        array.AddArrayItem(JSValue.CreateString(
            RegionPrimaryTimeZone.TryGetValue(region, out var zone) ? zone : "UTC"));
        return array;
    }

    public static JSValue GetWeekInfoPrototype(in Arguments a)
    {
        RequireLocale(in a, "getWeekInfo");
        return new JSObject();
    }

    public static JSValue ToStringPrototype(in Arguments a)
        => JSValue.CreateString(RequireLocale(in a, "toString").tag);

    public static JSValue BaseNamePrototype(in Arguments a)
        => JSValue.CreateString(RequireLocale(in a, "baseName").GetBaseName());

    public static JSValue LanguagePrototype(in Arguments a)
        => JSValue.CreateString(RequireLocale(in a, "language").GetLanguage());

    public static JSValue ScriptPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "script").GetScript());

    public static JSValue RegionPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "region").GetRegion());

    public static JSValue VariantsPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "variants").GetVariants());

    public static JSValue CalendarPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "calendar").GetUnicodeKeyword("ca"));

    public static JSValue CaseFirstPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "caseFirst").GetUnicodeKeyword("kf"));

    public static JSValue CollationPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "collation").GetUnicodeKeyword("co"));

    public static JSValue HourCyclePrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "hourCycle").GetUnicodeKeyword("hc"));

    public static JSValue NumberingSystemPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "numberingSystem").GetUnicodeKeyword("nu"));

    public static JSValue FirstDayOfWeekPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "firstDayOfWeek").GetUnicodeKeyword("fw"));

    public static JSValue NumericPrototype(in Arguments a)
    {
        var value = RequireLocale(in a, "numeric").GetUnicodeKeyword("kn");
        var numeric = value != null && (value.Length == 0 || value == "true");
        return numeric ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    private static JSValue OptionalString(string value)
        => value == null ? JSValue.UndefinedValue : JSValue.CreateString(value);

    // --- BCP-47 language tag parsing (over the stored, already-validated tag) ---

    private static bool IsAllAlpha(string s)
    {
        foreach (var c in s)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                return false;
        return s.Length > 0;
    }

    private static bool IsAllDigit(string s)
    {
        foreach (var c in s)
            if (c < '0' || c > '9')
                return false;
        return s.Length > 0;
    }

    private static string Titlecase(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();

    private string GetLanguage()
    {
        var dash = tag.IndexOf('-');
        return (dash < 0 ? tag : tag.Substring(0, dash)).ToLowerInvariant();
    }

    private string GetScript()
    {
        var parts = tag.Split('-');
        if (parts.Length > 1 && parts[1].Length == 4 && IsAllAlpha(parts[1]))
            return Titlecase(parts[1]);
        return null;
    }

    private string GetRegion()
    {
        var parts = tag.Split('-');
        var i = 1;
        if (i < parts.Length && parts[i].Length == 4 && IsAllAlpha(parts[i]))
            i++;
        if (i < parts.Length
            && ((parts[i].Length == 2 && IsAllAlpha(parts[i])) || (parts[i].Length == 3 && IsAllDigit(parts[i]))))
            return parts[i].ToUpperInvariant();
        return null;
    }

    private string GetVariants()
    {
        var parts = tag.Split('-');
        var i = 1;
        if (i < parts.Length && parts[i].Length == 4 && IsAllAlpha(parts[i]))
            i++;
        if (i < parts.Length
            && ((parts[i].Length == 2 && IsAllAlpha(parts[i])) || (parts[i].Length == 3 && IsAllDigit(parts[i]))))
            i++;

        var variants = new List<string>();
        for (; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 1)
                break;
            if ((p.Length >= 5 && p.Length <= 8) || (p.Length == 4 && p[0] >= '0' && p[0] <= '9'))
                variants.Add(p.ToLowerInvariant());
            else
                break;
        }

        return variants.Count == 0 ? null : string.Join("-", variants);
    }

    private string GetBaseName()
    {
        var parts = new List<string> { GetLanguage() };
        var script = GetScript();
        if (script != null)
            parts.Add(script);
        var region = GetRegion();
        if (region != null)
            parts.Add(region);
        var variants = GetVariants();
        if (variants != null)
            parts.Add(variants);
        return string.Join("-", parts);
    }

    // Parses the Unicode (-u-) extension and returns the type value for a 2-letter
    // keyword key, or null when absent. An empty string denotes a present keyword
    // with no explicit type (e.g. "-u-kn").
    private string GetUnicodeKeyword(string key)
    {
        var parts = tag.Split('-');
        var i = 0;
        while (i < parts.Length && !(parts[i].Length == 1 && (parts[i][0] == 'u' || parts[i][0] == 'U')))
            i++;
        if (i >= parts.Length)
            return null;

        i++; // skip the 'u' singleton
        string currentKey = null;
        var values = new List<string>();
        for (; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 1)
                break; // a new singleton ends the Unicode extension

            if (p.Length == 2)
            {
                if (string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
                    return string.Join("-", values);

                currentKey = p;
                values.Clear();
            }
            else if (currentKey != null)
            {
                values.Add(p.ToLowerInvariant());
            }
        }

        if (string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            return string.Join("-", values);

        return null;
    }
}

public sealed class JSIntlPluralRules : JSObject
{
    private readonly string locale;
    private readonly string type;

    public JSIntlPluralRules(in Arguments a) : base(CurrentPrototype())
    {
        var options = JSIntl.ValidateConstructorArguments("PluralRules", in a);
        locale = JSIntl.ResolveLocale(a.Get1());
        var typeKey = KeyStrings.GetOrCreate("type");
        type = options is null || options[typeKey].IsUndefined ? "cardinal" : options[typeKey].StringValue;
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("PluralRules")] as JSFunction)?.prototype
            : null;

    // ResolvePlural: maps a number to its CLDR plural category using the per-locale
    // rules generated from cldr-json (UnicodeCldr.LocaleData). Non-finite numbers,
    // and locales with no rules, resolve to "other".
    public string SelectCategory(double n)
        => CldrLocaleData.SelectPlural(locale, type, n);

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlPluralRules @this)
            throw JSEngine.NewTypeError("Intl.PluralRules.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), JSValue.CreateString(@this.type));
        result.CreateDataProperty(KeyStrings.GetOrCreate("minimumIntegerDigits"), JSValue.CreateNumber(1));
        result.CreateDataProperty(KeyStrings.GetOrCreate("minimumFractionDigits"), JSValue.CreateNumber(0));
        result.CreateDataProperty(KeyStrings.GetOrCreate("maximumFractionDigits"), JSValue.CreateNumber(0));
        var pluralCategories = JSValue.CreateArray();
        result.CreateDataProperty(KeyStrings.GetOrCreate("pluralCategories"), pluralCategories);
        var categories = pluralCategories;
        if (categories is JSObject array)
        {
            foreach (var category in CldrLocaleData.GetPluralCategories(@this.locale, @this.type))
                array.AddArrayItem(JSValue.CreateString(category));
        }

        return result;
    }
}

internal sealed class JSIntlNumberFormatResolved
{
    public JSIntlNumberFormatResolved(string notation, string signDisplay, string compactDisplay, string unitDisplay)
    {
        Notation = notation;
        SignDisplay = signDisplay;
        CompactDisplay = compactDisplay;
        UnitDisplay = unitDisplay;
    }

    // notation and signDisplay always have a resolved value; compactDisplay and
    // unitDisplay are null unless the corresponding slot exists (notation is
    // "compact" / style is "unit").
    public string Notation { get; }
    public string SignDisplay { get; }
    public string CompactDisplay { get; }
    public string UnitDisplay { get; }

    // Snapshot of the fraction/integer/significant digit options, read from the
    // options bag exactly once during construction (SetNumberFormatDigitOptions),
    // so a getter on e.g. minimumFractionDigits fires once at construction rather
    // than on every format/resolvedOptions call.
    public JSObject DigitOptions { get; set; }
}

public class JSIntlNumberFormat : JSObject
{
    private readonly string locale;
    private JSObject options;
    private JSIntlNumberFormatResolved resolved;

    public JSIntlNumberFormat(in Arguments a) : this()
    {
        options = JSIntl.ValidateConstructorArguments("NumberFormat", in a, requireNew: false);
        resolved = JSIntl.ValidateNumberFormatOptions(options);
        locale = JSIntl.ResolveLocale(a.Get1());
    }

    private JSIntlNumberFormat() : base(CurrentPrototype()) { }

    public JSValue Format(in Arguments a)
    {
        var sb = new StringBuilder();
        foreach (var (_, value) in ComputeFormatParts(a.Get1()))
            sb.Append(value);
        return JSValue.CreateString(sb.ToString());
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat @this)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatToParts called on incompatible receiver");

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var parts = JSValue.CreateArray();
        foreach (var (type, value) in @this.ComputeFormatParts(a.Get1()))
        {
            var part = new JSObject();
            part.FastAddValue(typeKey, JSValue.CreateString(type), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(valueKey, JSValue.CreateString(value), JSPropertyAttributes.EnumerableConfigurableValue);
            parts.AddArrayItem(part);
        }
        return parts;
    }

    // Builds the ordered sign/number parts for format / formatToParts. Implements
    // the spec sign-display selection (auto/always/never/exceptZero/negative) over
    // the rounded magnitude, with special handling for NaN and ±Infinity. The sign
    // is decided from the rounded value, so a value that rounds to zero counts as a
    // signed zero for "auto"/"always" but is treated as unsigned for "exceptZero"
    // and "negative".
    internal List<(string type, string value)> ComputeFormatParts(JSValue value)
    {
        var x = value != null && value.IsBigInt ? (double)value.BigIntValue : (value ?? JSUndefined.Value).DoubleValue;

        var signDisplay = resolved?.SignDisplay ?? "auto";
        var isNaN = double.IsNaN(x);
        var isInfinity = double.IsInfinity(x);
        var signBit = !isNaN && double.IsNegative(x);
        var style = StyleOption();
        var isCurrency = style == "currency";
        var isUnit = style == "unit";
        var currency = isCurrency ? ResolveCurrency() : default;

        List<(string, string)> magnitude;
        bool roundedIsZero;
        if (isNaN)
        {
            magnitude = [("nan", NanSymbol())];
            roundedIsZero = false;
        }
        else if (isInfinity)
        {
            magnitude = [("infinity", CldrLocaleData.InfinitySymbol)];
            roundedIsZero = false;
        }
        else if (isCurrency)
        {
            magnitude = FormatFiniteMagnitude(Math.Abs(x), currency.FractionDigits, currency.FractionDigits, out roundedIsZero);
        }
        else
        {
            var notation = resolved?.Notation ?? "standard";
            if (notation == "scientific" || notation == "engineering")
                magnitude = FormatScientificEngineering(Math.Abs(x), notation == "engineering", out roundedIsZero);
            else if (notation == "compact")
                magnitude = FormatCompact(Math.Abs(x), out roundedIsZero);
            else
                magnitude = FormatFiniteMagnitude(Math.Abs(x), 0, 3, out roundedIsZero);
        }

        var negativeNonZero = signBit && !roundedIsZero;
        var positiveNonZero = !signBit && !isNaN && !roundedIsZero;

        var sign = signDisplay switch
        {
            "never" => null,
            "always" => signBit ? "-" : "+",
            "exceptZero" => negativeNonZero ? "-" : positiveNonZero ? "+" : null,
            "negative" => negativeNonZero ? "-" : null,
            _ => signBit ? "-" : null, // "auto"
        };

        if (isCurrency)
            return AssembleCurrencyParts(magnitude, sign, currency);

        var plain = AssemblePlainParts(magnitude, sign);
        return isUnit ? AssembleUnitParts(plain, x) : plain;
    }

    private static List<(string, string)> AssemblePlainParts(List<(string, string)> magnitude, string sign)
    {
        var parts = new List<(string, string)>();
        if (sign == "-")
            parts.Add(("minusSign", "-"));
        else if (sign == "+")
            parts.Add(("plusSign", "+"));
        parts.AddRange(magnitude);
        return parts;
    }

    // Wraps the formatted number in the locale's CLDR unit pattern (e.g. "{0} m"),
    // choosing the plural-category variant from the number. The text around the
    // "{0}" placeholder is split into "unit" parts (the unit name) and "literal"
    // parts (surrounding whitespace). The unit identifier and display come from the
    // options; the patterns are generated CLDR data (UnicodeCldr.LocaleData).
    private List<(string, string)> AssembleUnitParts(List<(string, string)> numberParts, double x)
    {
        var unit = ReadStringOption("unit", string.Empty);
        var display = resolved?.UnitDisplay ?? "short";
        var category = CldrLocaleData.SelectPlural(locale, "cardinal", x);
        var pattern = CldrLocaleData.GetUnitPattern(locale, unit, display, category);

        var placeholder = pattern.IndexOf("{0}", StringComparison.Ordinal);
        if (placeholder < 0)
            return numberParts;

        var parts = new List<(string, string)>();
        AppendUnitText(parts, pattern[..placeholder]);
        parts.AddRange(numberParts);
        AppendUnitText(parts, pattern[(placeholder + 3)..]);
        return parts;
    }

    // Splits a unit-pattern fragment into leading/trailing whitespace ("literal")
    // and the unit name ("unit").
    private static void AppendUnitText(List<(string, string)> parts, string text)
    {
        if (text.Length == 0)
            return;

        var start = 0;
        while (start < text.Length && char.IsWhiteSpace(text[start]))
            start++;
        var end = text.Length;
        while (end > start && char.IsWhiteSpace(text[end - 1]))
            end--;

        if (start > 0)
            parts.Add(("literal", text[..start]));
        if (end > start)
            parts.Add(("unit", text[start..end]));
        if (end < text.Length)
            parts.Add(("literal", text[end..]));
    }

    // Lays out the currency symbol around the number core and applies the sign.
    // For the "accounting" currencySign a negative is wrapped in parentheses in
    // locales that use that convention (en/ja/ko/zh); locales like de-DE use a
    // leading minus sign instead. A plus sign (signDisplay) is always prepended.
    private List<(string, string)> AssembleCurrencyParts(List<(string, string)> magnitude, string sign, CldrCurrencyFormat currency)
    {
        var core = new List<(string, string)>();
        if (currency.SymbolAfterNumber)
        {
            core.AddRange(magnitude);
            if (currency.SpacingBetweenNumberAndSymbol.Length > 0)
                core.Add(("literal", currency.SpacingBetweenNumberAndSymbol));
            core.Add(("currency", currency.Symbol));
        }
        else
        {
            core.Add(("currency", currency.Symbol));
            core.AddRange(magnitude);
        }

        var useAccountingParens = sign == "-" && CurrencySignOption() == "accounting" && currency.AccountingUsesParentheses;
        var parts = new List<(string, string)>();
        if (useAccountingParens)
        {
            parts.Add(("literal", "("));
            parts.AddRange(core);
            parts.Add(("literal", ")"));
            return parts;
        }

        if (sign == "-")
            parts.Add(("minusSign", "-"));
        else if (sign == "+")
            parts.Add(("plusSign", "+"));
        parts.AddRange(core);
        return parts;
    }

    // Formats a finite magnitude in scientific ("3.45E-4") or engineering ("345E-6")
    // notation. The exponent is chosen so the mantissa lies in [1,10) for scientific
    // or [1,1000) (with the exponent a multiple of 3) for engineering; the mantissa is
    // then formatted with the usual fraction-digit rules and an "E"+digits exponent
    // suffix is appended as exponentSeparator / exponentMinusSign / exponentInteger
    // parts.
    private List<(string, string)> FormatScientificEngineering(double absX, bool engineering, out bool roundedIsZero)
    {
        var minFrac = ReadIntOption("minimumFractionDigits", 0);
        var maxFrac = ReadIntOption("maximumFractionDigits", 3);
        if (maxFrac < minFrac)
            maxFrac = minFrac;

        var exponent = ComputeExponent(absX, engineering);
        var mantissa = absX == 0 ? 0 : absX / Math.Pow(10, exponent);

        // A mantissa that rounds up past its upper bound bumps the exponent
        // (e.g. 9.999 -> 10 in scientific, 999.9 -> 1000 in engineering).
        var upper = engineering ? 1000.0 : 10.0;
        var roundedMantissa = Math.Round(mantissa, Math.Clamp(maxFrac, 0, 15), MidpointRounding.AwayFromZero);
        if (absX != 0 && roundedMantissa >= upper)
        {
            exponent += engineering ? 3 : 1;
            mantissa = absX / Math.Pow(10, exponent);
        }

        var result = FormatFiniteMagnitude(mantissa, minFrac, maxFrac, out roundedIsZero);
        result.Add(("exponentSeparator", "E"));
        if (exponent < 0)
            result.Add(("exponentMinusSign", "-"));
        result.Add(("exponentInteger", Math.Abs(exponent).ToString(CultureInfo.InvariantCulture)));
        return result;
    }

    // Compact decimal units (descending by exponent) for the CJK locales exercised
    // by test262. Each entry is (power-of-ten boundary, suffix); the suffix is shared
    // by short and long compactDisplay in these locales. Intermediate powers (e.g.
    // 10^5..10^7 for ja) reuse the lower unit, so the divisor is the largest unit
    // whose exponent does not exceed the value's magnitude.
    private static readonly (int Exp, string Suffix)[] CompactUnitsJa = [(12, "兆"), (8, "億"), (4, "万")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsKo = [(12, "조"), (8, "억"), (4, "만"), (3, "천")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsZhHant = [(12, "兆"), (8, "億"), (4, "萬")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsZhHans = [(12, "兆"), (8, "亿"), (4, "万")];

    private static (int Exp, string Suffix)[] GetCompactUnits(string localeTag)
    {
        var tag = localeTag ?? string.Empty;
        var dash = tag.IndexOf('-');
        var language = (dash < 0 ? tag : tag[..dash]).ToLowerInvariant();
        switch (language)
        {
            case "ja":
                return CompactUnitsJa;
            case "ko":
                return CompactUnitsKo;
            case "zh":
                var lower = tag.ToLowerInvariant();
                var isTraditional = lower.Contains("hant") || lower.Contains("-tw")
                    || lower.Contains("-hk") || lower.Contains("-mo");
                return isTraditional ? CompactUnitsZhHant : CompactUnitsZhHans;
            default:
                return null;
        }
    }

    // Compact notation (notation: "compact"). The value is divided by the largest
    // applicable compact unit and rounded with the default ECMA-402 "morePrecision"
    // rule: the more precise of {maximumFractionDigits 0} and {maximumSignificantDigits
    // 2}. That reduces to: when the reduced value's integer magnitude is ≤ 1 (i.e.
    // < 100) keep 2 significant digits, otherwise round to an integer. The locale's
    // compact suffix is appended as a "compact" part. Compact numbers are not grouped
    // (CLDR useGrouping "min2": the ≤4-digit reduced values stay ungrouped). Only the
    // CJK locales with embedded data are compacted; others fall back to standard
    // formatting (unchanged behaviour).
    private List<(string, string)> FormatCompact(double absX, out bool roundedIsZero)
    {
        var units = GetCompactUnits(locale);
        if (units == null)
            return FormatFiniteMagnitude(absX, 0, 3, out roundedIsZero);

        string suffix = null;
        var reduced = absX;
        if (absX > 0 && !double.IsInfinity(absX))
        {
            var magnitude = CompactMagnitude(absX);
            foreach (var (exp, sfx) in units)
            {
                if (magnitude >= exp)
                {
                    reduced = absX / Math.Pow(10, exp);
                    suffix = sfx;
                    break;
                }
            }
        }

        int maxFrac;
        if (HasOption("maximumFractionDigits"))
        {
            maxFrac = ReadIntOption("maximumFractionDigits", 0);
        }
        else
        {
            // morePrecision default: keep 2 significant digits while the value is
            // below 100, otherwise round to an integer.
            var m = CompactMagnitude(reduced);
            maxFrac = m <= 1 ? Math.Max(0, 1 - m) : 0;
        }

        var parts = FormatCompactMantissa(reduced, maxFrac, out roundedIsZero);
        if (suffix != null)
            parts.Add(("compact", suffix));
        return parts;
    }

    private static int CompactMagnitude(double v)
    {
        if (v <= 0 || double.IsNaN(v) || double.IsInfinity(v))
            return 0;

        var m = (int)Math.Floor(Math.Log10(v));
        if (Math.Pow(10, m) > v)
            m--;
        else if (Math.Pow(10, m + 1) <= v)
            m++;
        return m;
    }

    // Formats the reduced compact value (integer + optional fraction parts) without
    // grouping, trimming trailing fraction zeros.
    private List<(string, string)> FormatCompactMantissa(double reduced, int maxFrac, out bool roundedIsZero)
    {
        var clamped = Math.Clamp(maxFrac, 0, 15);
        var rounded = Math.Round(reduced, clamped, MidpointRounding.AwayFromZero);
        roundedIsZero = rounded == 0;

        var fixedStr = rounded.ToString("F" + clamped, CultureInfo.InvariantCulture);
        var dot = fixedStr.IndexOf('.');
        var intDigits = dot < 0 ? fixedStr : fixedStr[..dot];
        var fracDigits = dot < 0 ? string.Empty : fixedStr[(dot + 1)..];

        while (fracDigits.Length > 0 && fracDigits.EndsWith('0'))
            fracDigits = fracDigits[..^1];

        intDigits = intDigits.TrimStart('0');
        if (intDigits.Length == 0)
            intDigits = "0";

        var result = new List<(string, string)> { ("integer", intDigits) };
        if (fracDigits.Length > 0)
        {
            result.Add(("decimal", DecimalSeparator()));
            result.Add(("fraction", fracDigits));
        }

        return result;
    }

    // ComputeExponentForMagnitude over floor(log10(absX)): scientific keeps the raw
    // base-10 magnitude; engineering rounds it down to a multiple of 3.
    private static int ComputeExponent(double absX, bool engineering)
    {
        if (absX == 0 || double.IsNaN(absX) || double.IsInfinity(absX))
            return 0;

        var magnitude = (int)Math.Floor(Math.Log10(absX));
        // Correct floating-point error near exact powers of ten.
        if (Math.Pow(10, magnitude) > absX)
            magnitude--;
        else if (Math.Pow(10, magnitude + 1) <= absX)
            magnitude++;

        return engineering ? (int)(Math.Floor(magnitude / 3.0) * 3) : magnitude;
    }

    private List<(string, string)> FormatFiniteMagnitude(double magnitude, int defaultMinFrac, int defaultMaxFrac, out bool roundedIsZero)
    {
        var minFrac = ReadIntOption("minimumFractionDigits", defaultMinFrac);
        var maxFrac = ReadIntOption("maximumFractionDigits", defaultMaxFrac);
        if (maxFrac < minFrac)
            maxFrac = minFrac;
        var minInt = ReadIntOption("minimumIntegerDigits", 1);

        var rounded = Math.Round(magnitude, Math.Clamp(maxFrac, 0, 15), MidpointRounding.AwayFromZero);
        roundedIsZero = rounded == 0;

        var fixedStr = rounded.ToString("F" + maxFrac, CultureInfo.InvariantCulture);
        var dot = fixedStr.IndexOf('.');
        var intDigits = dot < 0 ? fixedStr : fixedStr[..dot];
        var fracDigits = dot < 0 ? string.Empty : fixedStr[(dot + 1)..];

        // Trim trailing fraction zeros down to the minimum requested.
        while (fracDigits.Length > minFrac && fracDigits.EndsWith('0'))
            fracDigits = fracDigits[..^1];

        intDigits = intDigits.TrimStart('0');
        if (intDigits.Length == 0)
            intDigits = "0";
        while (intDigits.Length < minInt)
            intDigits = "0" + intDigits;

        var result = new List<(string, string)>();
        AppendIntegerParts(result, intDigits);
        if (fracDigits.Length > 0)
        {
            result.Add(("decimal", DecimalSeparator()));
            result.Add(("fraction", fracDigits));
        }
        return result;
    }

    private void AppendIntegerParts(List<(string, string)> result, string intDigits)
    {
        // CLDR minimumGroupingDigits: grouping is shown only when the leading
        // (most-significant) group has at least that many digits. For locales such
        // as pl/es/it (value 2), four-digit values like 1000 stay ungrouped.
        var leadingGroup = ((intDigits.Length - 1) % 3) + 1;
        if (!UseGrouping()
            || intDigits.Length <= 3
            || leadingGroup < CldrLocaleData.MinimumGroupingDigits(locale))
        {
            result.Add(("integer", intDigits));
            return;
        }

        var groupSeparator = GroupSeparator();
        var first = intDigits.Length % 3;
        if (first == 0)
            first = 3;
        result.Add(("integer", intDigits[..first]));
        for (var idx = first; idx < intDigits.Length; idx += 3)
        {
            result.Add(("group", groupSeparator));
            result.Add(("integer", intDigits.Substring(idx, 3)));
        }
    }

    private string NanSymbol() => CldrLocaleData.NaNSymbol(locale);

    private bool UseGrouping()
    {
        if (options == null)
            return true;
        var v = options[KeyStrings.GetOrCreate("useGrouping")];
        if (v == null || v.IsUndefined)
            return true;
        if (v.IsBoolean)
            return v.BooleanValue;
        if (v.IsString)
        {
            var s = v.StringValue;
            return s != "false" && s != "never";
        }
        return true;
    }

    // Digit options come from the construction-time snapshot (read once from the
    // options bag), not the live options object, so format-time reads do not
    // re-invoke option getters.
    private int ReadIntOption(string name, int fallback)
    {
        var snapshot = resolved?.DigitOptions;
        if (snapshot == null)
            return fallback;
        var v = snapshot[KeyStrings.GetOrCreate(name)];
        if (v == null || v.IsUndefined)
            return fallback;
        var d = v.DoubleValue;
        return double.IsNaN(d) ? fallback : (int)d;
    }

    private bool HasOption(string name)
    {
        var snapshot = resolved?.DigitOptions;
        if (snapshot == null)
            return false;
        var v = snapshot[KeyStrings.GetOrCreate(name)];
        return v != null && !v.IsUndefined;
    }

    private CultureInfo Culture()
    {
        if (string.IsNullOrEmpty(locale))
            return CultureInfo.InvariantCulture;
        var tag = locale;
        var uPos = tag.IndexOf("-u-", StringComparison.OrdinalIgnoreCase);
        if (uPos >= 0)
            tag = tag[..uPos];
        try { return CultureInfo.GetCultureInfo(tag); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    private string GroupSeparator() => Culture().NumberFormat.NumberGroupSeparator;

    private string DecimalSeparator() => Culture().NumberFormat.NumberDecimalSeparator;

    private string StyleOption()
    {
        if (options == null)
            return "decimal";
        var v = options[KeyStrings.GetOrCreate("style")];
        return v == null || v.IsUndefined ? "decimal" : v.StringValue;
    }

    private string CurrencySignOption()
    {
        if (options == null)
            return "standard";
        var v = options[KeyStrings.GetOrCreate("currencySign")];
        return v == null || v.IsUndefined ? "standard" : v.StringValue;
    }

    // The locale-aware currency layout (symbol string, placement, negative
    // convention and fraction digits) comes from the shared CLDR data library
    // (UnicodeCldr.LocaleData) so the hand-curated tables live next to the other
    // Unicode Consortium data and can later be generated from cldr-json.
    private CldrCurrencyFormat ResolveCurrency()
        => CldrLocaleData.ResolveCurrency(
            locale,
            ReadStringOption("currency", string.Empty),
            ReadStringOption("currencyDisplay", "symbol"));

    private string ReadStringOption(string name, string fallback)
    {
        if (options == null)
            return fallback;
        var v = options[KeyStrings.GetOrCreate(name)];
        return v == null || v.IsUndefined ? fallback : v.StringValue;
    }

    public static JSValue FormatRangePrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatRange called on incompatible receiver");

        var start = CoerceRangeValue(a[0]);
        var end = CoerceRangeValue(a.GetAt(1));
        return JSValue.CreateString($"{start}–{end}");
    }

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatRangeToParts called on incompatible receiver");

        var start = CoerceRangeValue(a[0]);
        var end = CoerceRangeValue(a.GetAt(1));
        return JSValue.CreateArray();
    }

    private static string CoerceRangeValue(JSValue value)
    {
        if (value == null || value.IsUndefined)
            throw JSEngine.NewTypeError("Invalid number range");

        // ToIntlMathematicalValue (ECMA-402): a Symbol operand cannot be coerced
        // to a numeric value, so it is a TypeError rather than a formatted string.
        if (value.IsSymbol)
            throw JSEngine.NewTypeError("Cannot convert a Symbol value to a number");

        if (value.IsNumber && double.IsNaN(value.DoubleValue))
            throw JSEngine.NewRangeError("Invalid number range");

        return value.ToString();
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat @this)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), JSValue.CreateString("latn"));

        var styleKey = KeyStrings.GetOrCreate("style");
        var style = @this.options is null || @this.options[styleKey].IsUndefined ? "decimal" : @this.options[styleKey].StringValue;
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), JSValue.CreateString(style));

        if (@this.options != null)
        {
            var currencyKey = KeyStrings.GetOrCreate("currency");
            var unitKey = KeyStrings.GetOrCreate("unit");
            var useGroupingKey = KeyStrings.GetOrCreate("useGrouping");
            var roundingIncrementKey = KeyStrings.GetOrCreate("roundingIncrement");
            var roundingModeKey = KeyStrings.GetOrCreate("roundingMode");
            var roundingPriorityKey = KeyStrings.GetOrCreate("roundingPriority");
            var trailingZeroDisplayKey = KeyStrings.GetOrCreate("trailingZeroDisplay");
            var minimumIntegerDigitsKey = KeyStrings.GetOrCreate("minimumIntegerDigits");
            var minimumFractionDigitsKey = KeyStrings.GetOrCreate("minimumFractionDigits");
            var maximumFractionDigitsKey = KeyStrings.GetOrCreate("maximumFractionDigits");
            var minimumSignificantDigitsKey = KeyStrings.GetOrCreate("minimumSignificantDigits");
            var maximumSignificantDigitsKey = KeyStrings.GetOrCreate("maximumSignificantDigits");

            if (!@this.options[currencyKey].IsUndefined)
                result.CreateDataProperty(currencyKey, @this.options[currencyKey]);
            if (!@this.options[unitKey].IsUndefined)
                result.CreateDataProperty(unitKey, @this.options[unitKey]);
            if (!@this.options[useGroupingKey].IsUndefined)
                result.CreateDataProperty(useGroupingKey, @this.options[useGroupingKey]);
            if (!@this.options[roundingIncrementKey].IsUndefined)
                result.CreateDataProperty(roundingIncrementKey, @this.options[roundingIncrementKey]);
            if (!@this.options[roundingModeKey].IsUndefined)
                result.CreateDataProperty(roundingModeKey, @this.options[roundingModeKey]);
            if (!@this.options[roundingPriorityKey].IsUndefined)
                result.CreateDataProperty(roundingPriorityKey, @this.options[roundingPriorityKey]);
            if (!@this.options[trailingZeroDisplayKey].IsUndefined)
                result.CreateDataProperty(trailingZeroDisplayKey, @this.options[trailingZeroDisplayKey]);
            // Digit options reflect the construction-time snapshot (read once),
            // not the live options bag, so resolvedOptions does not re-trigger
            // option getters.
            var digits = @this.resolved?.DigitOptions;

            if (digits != null && !digits[minimumIntegerDigitsKey].IsUndefined)
                result.CreateDataProperty(minimumIntegerDigitsKey, digits[minimumIntegerDigitsKey]);
            else
                result.CreateDataProperty(minimumIntegerDigitsKey, JSValue.CreateNumber(1));

            if (digits != null && !digits[minimumFractionDigitsKey].IsUndefined)
                result.CreateDataProperty(minimumFractionDigitsKey, digits[minimumFractionDigitsKey]);
            else
                result.CreateDataProperty(minimumFractionDigitsKey, JSValue.CreateNumber(0));

            if (digits != null && !digits[maximumFractionDigitsKey].IsUndefined)
                result.CreateDataProperty(maximumFractionDigitsKey, digits[maximumFractionDigitsKey]);
            else
                result.CreateDataProperty(maximumFractionDigitsKey, JSValue.CreateNumber(3));

            if (digits != null && !digits[minimumSignificantDigitsKey].IsUndefined)
                result.CreateDataProperty(minimumSignificantDigitsKey, digits[minimumSignificantDigitsKey]);
            if (digits != null && !digits[maximumSignificantDigitsKey].IsUndefined)
                result.CreateDataProperty(maximumSignificantDigitsKey, digits[maximumSignificantDigitsKey]);
        }
        else
        {
            result.CreateDataProperty(KeyStrings.GetOrCreate("minimumIntegerDigits"), JSValue.CreateNumber(1));
            result.CreateDataProperty(KeyStrings.GetOrCreate("minimumFractionDigits"), JSValue.CreateNumber(0));
            result.CreateDataProperty(KeyStrings.GetOrCreate("maximumFractionDigits"), JSValue.CreateNumber(3));
            result.CreateDataProperty(KeyStrings.GetOrCreate("useGrouping"), JSValue.BooleanTrue);
        }

        // notation/signDisplay always have a resolved value; compactDisplay and
        // unitDisplay are reflected only when their slot exists. These are read
        // from the slots resolved at construction (not the live options object)
        // so getter side effects observe construction-time order, not access.
        var r = @this.resolved;
        if (r != null)
        {
            result.CreateDataProperty(KeyStrings.GetOrCreate("notation"), JSValue.CreateString(r.Notation));
            result.CreateDataProperty(KeyStrings.GetOrCreate("signDisplay"), JSValue.CreateString(r.SignDisplay));
            if (r.CompactDisplay != null)
                result.CreateDataProperty(KeyStrings.GetOrCreate("compactDisplay"), JSValue.CreateString(r.CompactDisplay));
            if (r.UnitDisplay != null)
                result.CreateDataProperty(KeyStrings.GetOrCreate("unitDisplay"), JSValue.CreateString(r.UnitDisplay));
        }

        return result;
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("NumberFormat")] as JSFunction)?.prototype
            : null;
}

public class JSIntlCollator : JSObject
{
    private readonly string locale;
    private readonly string usage = "sort";
    private readonly string sensitivity = "variant";
    private readonly bool ignorePunctuation;
    private readonly string collation = "default";
    private readonly bool numeric;
    private readonly string caseFirst = "false";
    private readonly CompareInfo compareInfo;

    public JSIntlCollator(in Arguments a) : this()
    {
        var options = JSIntl.ValidateConstructorArguments("Collator", in a, requireNew: false);
        locale = JSIntl.ResolveLocale(a.Get1());
        compareInfo = CultureInfo.CurrentCulture.CompareInfo;

        if (TryGetUnicodeExtension(locale, "kn", out var kn))
            numeric = kn == "true";
        if (TryGetUnicodeExtension(locale, "kf", out var kf) && (kf == "upper" || kf == "lower" || kf == "false"))
            caseFirst = kf;
        if (TryGetUnicodeExtension(locale, "co", out var co) && IsValidCollation(co))
            collation = co;

        if (TryGetOwnOption(options, "usage", out var usageValue))
            usage = usageValue.StringValue;
        if (TryGetOwnOption(options, "sensitivity", out var sensitivityValue))
            sensitivity = sensitivityValue.StringValue;
        if (TryGetOwnOption(options, "ignorePunctuation", out var ignorePunctuationValue))
            ignorePunctuation = ignorePunctuationValue.BooleanValue;
        if (TryGetOwnOption(options, "numeric", out var numericValue))
            numeric = numericValue.BooleanValue;
        if (TryGetOwnOption(options, "caseFirst", out var caseFirstValue))
            caseFirst = caseFirstValue.StringValue;
        if (TryGetOwnOption(options, "collation", out var collationValue) && IsValidCollation(collationValue.StringValue))
            collation = collationValue.StringValue;
    }

    private JSIntlCollator() : base(CurrentPrototype()) { }

    public JSValue Compare(in Arguments a)
    {
        var left = a.Get1().StringValue;
        var right = a.GetAt(1).StringValue;
        var options = CompareOptions.None;
        if (sensitivity == "base")
            options |= CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        else if (sensitivity == "accent")
            options |= CompareOptions.IgnoreCase;
        else if (sensitivity == "case")
            options |= CompareOptions.IgnoreNonSpace;
        if (ignorePunctuation)
            options |= CompareOptions.IgnoreSymbols;

        return JSValue.CreateNumber(compareInfo.Compare(left, right, options));
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlCollator @this)
            throw JSEngine.NewTypeError("Intl.Collator.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.FastAddValue(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("usage"), JSValue.CreateString(@this.usage), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("sensitivity"), JSValue.CreateString(@this.sensitivity), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("ignorePunctuation"), @this.ignorePunctuation ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("collation"), JSValue.CreateString(@this.collation), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("numeric"), @this.numeric ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("caseFirst"), JSValue.CreateString(@this.caseFirst), JSPropertyAttributes.EnumerableConfigurableValue);
        return result;
    }

    private static bool TryGetOwnOption(JSObject options, string name, out JSValue value)
    {
        value = JSUndefined.Value;
        if (options == null)
            return false;

        var key = KeyStrings.GetOrCreate(name);
        if (options.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(name, key)) is not JSObject descriptor)
            return false;

        value = descriptor[KeyStrings.value];
        return !value.IsUndefined;
    }

    private static bool TryGetUnicodeExtension(string locale, string key, out string value)
    {
        value = null;
        var marker = "-u-";
        var start = locale.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return false;

        var parts = locale[(start + marker.Length)..].Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (!string.Equals(parts[i], key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = i + 1 < parts.Length && parts[i + 1].Length > 2 ? parts[i + 1] : "true";
            return true;
        }

        return false;
    }

    private static bool IsValidCollation(string value)
        => Regex.IsMatch(value, @"^[0-9A-Za-z]{3,8}(?:-[0-9A-Za-z]{3,8})*$", RegexOptions.CultureInvariant);

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("Collator")] as JSFunction)?.prototype
            : null;
}

public class JSIntlDateTimeFormat : JSObject
{
    private static readonly ConcurrentDictionary<string, JSIntlDateTimeFormat> formats = new();
    private static readonly KeyString HourKey = KeyStrings.GetOrCreate("hour");
    private static readonly KeyString DayPeriodKey = KeyStrings.GetOrCreate("dayPeriod");
    private static readonly KeyString MinuteKey = KeyStrings.GetOrCreate("minute");
    private static readonly KeyString SecondKey = KeyStrings.GetOrCreate("second");
    private static readonly KeyString FractionalSecondDigitsKey = KeyStrings.GetOrCreate("fractionalSecondDigits");
    private static readonly KeyString YearKey = KeyStrings.GetOrCreate("year");
    private static readonly KeyString MonthKey = KeyStrings.GetOrCreate("month");
    private static readonly KeyString DayKey = KeyStrings.GetOrCreate("day");
    private static readonly KeyString DateStyleKey = KeyStrings.GetOrCreate("dateStyle");
    private static readonly KeyString TimeStyleKey = KeyStrings.GetOrCreate("timeStyle");
    private static readonly KeyString TimeZoneKey = KeyStrings.GetOrCreate("timeZone");
    private static readonly KeyString Hour12Key = KeyStrings.GetOrCreate("hour12");
    private static readonly KeyString HourCycleKey = KeyStrings.GetOrCreate("hourCycle");
    private readonly CultureInfo locale;
    private readonly string localeTag;
    private JSObject options;

    private string OptionString(KeyString key)
    {
        var value = options?[key];
        return value == null || value.IsUndefined ? null : value.StringValue;
    }

    private int FractionalSecondDigits()
    {
        var value = options?[FractionalSecondDigitsKey];
        return value == null || value.IsUndefined ? 0 : (int)value.DoubleValue;
    }

    private bool ResolveHour12()
    {
        var hour12 = options?[Hour12Key];
        if (hour12 != null && !hour12.IsUndefined)
            return hour12.BooleanValue;
        var cycle = OptionString(HourCycleKey);
        if (cycle == "h23" || cycle == "h24")
            return false;
        return true; // en default
    }

    private JSIntlDateTimeFormatEngine.Pattern ResolveEnginePattern()
        => JSIntlDateTimeFormatEngine.ResolvePattern(
            localeTag: localeTag,
            hasYear: OptionString(YearKey) != null,
            yearStyle: OptionString(YearKey),
            hasMonth: OptionString(MonthKey) != null,
            monthStyle: OptionString(MonthKey),
            hasDay: OptionString(DayKey) != null,
            dayStyle: OptionString(DayKey),
            hasHour: OptionString(HourKey) != null,
            hasMinute: OptionString(MinuteKey) != null,
            hasSecond: OptionString(SecondKey) != null,
            fractionalSecondDigits: FractionalSecondDigits(),
            hasDayPeriodField: false,
            dateStyle: OptionString(DateStyleKey),
            timeStyle: OptionString(TimeStyleKey),
            hour12: ResolveHour12(),
            calendar: ResolvedCalendar());

    // The resolved calendar: the requested -u-ca- value when it is one this engine
    // supports (era-based Gregorian-derived calendars), otherwise "gregory".
    internal string ResolvedCalendar()
    {
        var ca = UnicodeKeyword(localeTag, "ca");
        return JSIntlDateTimeFormatEngine.IsSupportedCalendar(ca) ? ca : "gregory";
    }

    // Reads a Unicode (-u-) keyword value from a BCP-47 tag, e.g. "ca" from
    // "en-u-ca-buddhist". Returns null when absent.
    private static string UnicodeKeyword(string tag, string key)
    {
        if (string.IsNullOrEmpty(tag))
            return null;
        var parts = tag.Split('-');
        var u = System.Array.IndexOf(parts, "u");
        if (u < 0)
            return null;
        for (var i = u + 1; i < parts.Length; i++)
        {
            if (parts[i].Length == 2 && string.Equals(parts[i], key, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < parts.Length && parts[i + 1].Length > 2)
                    return parts[i + 1].ToLowerInvariant();
                return null;
            }
        }
        return null;
    }

    private JSIntlDateTimeFormatEngine.Fields ResolveFields(double clipped)
        => new(JSIntlDateTimeFormatEngine.ToZone(clipped, OptionString(TimeZoneKey)));

    private static JSValue PartsArray(System.Collections.Generic.List<JSIntlDateTimeFormatEngine.Part> parts)
    {
        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var sourceKey = KeyStrings.GetOrCreate("source");
        var array = JSValue.CreateArray();
        foreach (var p in parts)
        {
            var obj = new JSObject();
            obj[typeKey] = JSValue.CreateString(p.Type);
            obj[valueKey] = JSValue.CreateString(p.Value);
            if (p.Source != null)
                obj[sourceKey] = JSValue.CreateString(p.Source);
            array.AddArrayItem(obj);
        }
        return array;
    }

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

        if (SupportsDayPeriod())
        {
            var localTime = DateTimeOffset.FromUnixTimeMilliseconds((long)clipped).ToLocalTime();
            var dayPeriod = FormatDayPeriod(localTime, DayPeriodStyle());
            if (UsesHourFormatting())
                return new JSString($"{FormatEnglishHour(localTime)} {dayPeriod}");

            return new JSString(dayPeriod);
        }

        var pattern = ResolveEnginePattern();
        var fields = ResolveFields(clipped);
        var parts = JSIntlDateTimeFormatEngine.FormatToParts(pattern, in fields, FractionalSecondDigits(), "literal");
        return new JSString(JSIntlDateTimeFormatEngine.PartsToString(parts));
    }

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlDateTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.format called on incompatible receiver");

    public static JSValue FormatRangePrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRange called on incompatible receiver");

        var parts = @this.ComputeRangeParts(a);
        var sb = new StringBuilder();
        foreach (var part in parts)
            sb.Append(part.Value);
        return JSValue.CreateString(sb.ToString());
    }

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRangeToParts called on incompatible receiver");

        return PartsArray(@this.ComputeRangeParts(a));
    }

    private System.Collections.Generic.List<JSIntlDateTimeFormatEngine.Part> ComputeRangeParts(in Arguments a)
    {
        // FormatDateTimeRange step 3: TypeError if either endpoint is undefined — checked
        // before ToNumber, so a throwing valueOf on the other argument is not observed.
        var startArg = a.Get1();
        var endArg = a.GetAt(1);
        if (startArg == null || startArg.IsUndefined || endArg == null || endArg.IsUndefined)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat range start and end dates must not be undefined");

        var startValue = CoerceRangeTime(startArg);
        var endValue = CoerceRangeTime(endArg);
        var pattern = ResolveEnginePattern();
        var startFields = ResolveFields(startValue);
        var endFields = ResolveFields(endValue);
        return JSIntlDateTimeFormatEngine.FormatRangeToParts(pattern, in startFields, in endFields, FractionalSecondDigits());
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

        if (@this.SupportsDayPeriod())
        {
            var localTime = DateTimeOffset.FromUnixTimeMilliseconds((long)clipped).ToLocalTime();
            var dayPeriodParts = JSValue.CreateArray();
            if (@this.UsesHourFormatting())
            {
                var hourPart = new JSObject();
                hourPart[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("hour");
                hourPart[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(FormatEnglishHour(localTime));
                var literalPart = new JSObject();
                literalPart[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("literal");
                literalPart[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(" ");
                dayPeriodParts.AddArrayItem(hourPart);
                dayPeriodParts.AddArrayItem(literalPart);
            }

            var dayPeriodPart = new JSObject();
            dayPeriodPart[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("dayPeriod");
            dayPeriodPart[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(@this.FormatDayPeriod(localTime, @this.DayPeriodStyle()));
            dayPeriodParts.AddArrayItem(dayPeriodPart);
            return dayPeriodParts;
        }

        // minute + second (optionally with fractionalSecondDigits), no hour/date
        // fields: render "mm:ss[.fff]" as typed parts.
        if (@this.options != null
            && !@this.options[MinuteKey].IsUndefined
            && !@this.options[SecondKey].IsUndefined
            && @this.options[HourKey].IsUndefined)
        {
            var localTime = DateTimeOffset.FromUnixTimeMilliseconds((long)clipped).ToLocalTime();
            var timeParts = JSValue.CreateArray();
            AddDateTimePart(timeParts, "minute", localTime.Minute.ToString("D2", CultureInfo.InvariantCulture));
            AddDateTimePart(timeParts, "literal", ":");
            AddDateTimePart(timeParts, "second", localTime.Second.ToString("D2", CultureInfo.InvariantCulture));

            var fractionalSecondDigits = @this.options[FractionalSecondDigitsKey];
            if (!fractionalSecondDigits.IsUndefined)
            {
                var digits = (int)fractionalSecondDigits.DoubleValue;
                if (digits >= 1 && digits <= 3)
                {
                    AddDateTimePart(timeParts, "literal", ".");
                    AddDateTimePart(timeParts, "fractionalSecond",
                        localTime.Millisecond.ToString("D3", CultureInfo.InvariantCulture).Substring(0, digits));
                }
            }

            return timeParts;
        }

        var pattern = @this.ResolveEnginePattern();
        var fields = @this.ResolveFields(clipped);
        var engineParts = JSIntlDateTimeFormatEngine.FormatToParts(pattern, in fields, @this.FractionalSecondDigits(), null);
        return PartsArray(engineParts);
    }

    private static void AddDateTimePart(JSValue parts, string type, string value)
    {
        var part = new JSObject();
        part[KeyStrings.GetOrCreate("type")] = JSValue.CreateString(type);
        part[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(value);
        parts.AddArrayItem(part);
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.localeTag));
        result.CreateDataProperty(KeyStrings.GetOrCreate("calendar"), JSValue.CreateString(@this.ResolvedCalendar()));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), JSValue.CreateString("latn"));
        result.CreateDataProperty(KeyStrings.GetOrCreate("timeZone"), JSValue.CreateString(TimeZoneInfo.Local.Id));

        if (@this.options != null)
        {
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "calendar");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "numberingSystem");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "timeZone");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "hourCycle");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "hour12");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "dateStyle");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "timeStyle");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "weekday");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "era");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "year");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "month");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "day");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "dayPeriod");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "hour");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "minute");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "second");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "fractionalSecondDigits");
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "timeZoneName");
        }

        return result;
    }

    internal JSValue Format(DateTimeOffset value, JSObject format) => new JSString(value.ToString(locale));

    private bool SupportsDayPeriod()
        => options != null && !options[DayPeriodKey].IsUndefined;

    private bool UsesHourFormatting()
        => options != null && !options[HourKey].IsUndefined;

    private string DayPeriodStyle()
        => options?[DayPeriodKey]?.StringValue ?? string.Empty;

    private static string FormatEnglishHour(DateTimeOffset value)
        => ((value.Hour % 12) == 0 ? 12 : value.Hour % 12).ToString(CultureInfo.InvariantCulture);

    // CLDR day-period rule overrides for the ECMA-402 dayPeriod option. The bundled
    // Broiler.Unicode tables (a) surface the fixed "midnight" period at 00:00, which
    // the dayPeriod option never emits (browsers fold midnight into the surrounding
    // flexible period, so 00:00 → "at night" for en), and (b) for "en" carry a
    // mis-generated morning1/night1 split (morning1=0-720, night1=1260-1440) rather
    // than the CLDR morning1=06:00-12:00 with night1 wrapping 21:00-06:00. The
    // corrected rules keep the flexible periods contiguous; "noon" is retained (it
    // IS surfaced by the option). Keyed by language subtag.
    private static readonly Dictionary<string, string> DayPeriodRuleOverrides = new(StringComparer.Ordinal)
    {
        ["en"] = "noon@720;morning1=360-720;afternoon1=720-1080;evening1=1080-1260;night1=1260-360",
    };

    // The localized day-period name for the time, from CLDR data: the day-period
    // rules pick the period (am/pm/noon/midnight/morning/…) and the ECMA-402
    // dayPeriod option (long/short/narrow) selects the CLDR width.
    private string FormatDayPeriod(DateTimeOffset value, string style)
    {
        var period = ResolveDayPeriodKey(localeTag, value.Hour, value.Minute);
        var width = style switch
        {
            "narrow" => "narrow",
            "short" => "abbreviated",
            _ => "wide",
        };
        return CldrLocaleData.GetDayPeriodName(localeTag, period, width);
    }

    private static string ResolveDayPeriodKey(string localeTag, int hour, int minute)
    {
        var dash = localeTag?.IndexOf('-') ?? -1;
        var language = (dash < 0 ? localeTag ?? string.Empty : localeTag[..dash]).ToLowerInvariant();
        if (DayPeriodRuleOverrides.TryGetValue(language, out var rules))
            return MatchDayPeriodRules(rules, hour * 60 + minute) ?? (hour < 12 ? "am" : "pm");

        return CldrLocaleData.GetDayPeriod(localeTag, hour, minute);
    }

    // Mirrors UnicodeCldr.LocaleData's day-period rule matcher: "@" ("at") rules
    // win first, then half-open ranges (a from>before range wraps past midnight).
    private static string MatchDayPeriodRules(string rules, int time)
    {
        foreach (var rule in rules.Split(';'))
        {
            var at = rule.IndexOf('@');
            if (at >= 0)
            {
                if (int.Parse(rule[(at + 1)..], CultureInfo.InvariantCulture) == time)
                    return rule[..at];
                continue;
            }

            var eq = rule.IndexOf('=');
            if (eq < 0)
                continue;

            var period = rule[..eq];
            var dash = rule.IndexOf('-', eq + 1);
            var from = int.Parse(rule[(eq + 1)..dash], CultureInfo.InvariantCulture);
            var before = int.Parse(rule[(dash + 1)..], CultureInfo.InvariantCulture);
            var matched = from <= before
                ? time >= from && time < before
                : time >= from || time < before;
            if (matched)
                return period;
        }

        return null;
    }

    public JSIntlDateTimeFormat(in Arguments a) : base(CurrentPrototype())
    {
        options = JSIntl.ValidateConstructorArguments("DateTimeFormat", in a, requireNew: false);
        JSIntl.ValidateDateTimeFormatOptions(options);
        localeTag = JSIntl.ResolveLocale(a.Get1());
        locale = CultureInfo.CurrentCulture;
    }

    internal JSIntlDateTimeFormat(CultureInfo locale) : base()
    {
        this.locale = locale;
        localeTag = locale.Name;
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

internal static class JSIntlResolvedOptionsExtensions
{
    internal static void SetIfDefined(JSObject target, JSObject options, string name)
    {
        var key = KeyStrings.GetOrCreate(name);
        var value = options?[key] ?? JSValue.UndefinedValue;
        if (!value.IsUndefined)
            // CreateDataPropertyOrThrow: define an own property directly. Ordinary
            // assignment ([[Set]]) would walk the prototype chain and could invoke a
            // setter installed on Object.prototype by client code (ECMA-402 requires
            // resolvedOptions to be unaffected by such taint).
            target.CreateDataProperty(key, value);
    }
}
