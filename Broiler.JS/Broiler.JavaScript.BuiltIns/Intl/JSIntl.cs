using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Numerics;
using Broiler.ECMAScript.Intl;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Date;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.BuiltIns.Temporal;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Runtime.Unicode;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Segmentation;
// IntlEnumerationData (Intl.supportedValuesOf's currencies and units, sub-area (a)) is the last reader of this namespace
// here since SJL-9.5's task 8.5, which ends DateTimeFormat's use of CldrLocaleData (noted 2026-11-09).
using UnicodeCldr.LocaleData;

namespace Broiler.JavaScript.BuiltIns.Intl;

public static class JSIntl
{
    private static readonly ConditionalWeakTable<JSObject, JSObject> Cache = [];
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
    private static readonly KeyString CurrencyDisplayKey = KeyStrings.GetOrCreate("currencyDisplay");
    private static readonly KeyString CurrencySignKey = KeyStrings.GetOrCreate("currencySign");
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

    public static JSValue GetIntlObject()
    {
        if (JSEngine.CurrentContext is JSContext realm)
            return GetIntlObject(realm);

        if (JSEngine.CurrentContext is JSObject global)
            return Cache.GetValue(global, static _ => CreateIntlObject(null));

        return CreateIntlObject(null);
    }

    // The realm's one Intl namespace object. Creating it records each Intl constructor as the
    // realm's intrinsic under its qualified name ("Intl.NumberFormat"), which is where the
    // objects the engine creates without a NewTarget (`Intl.NumberFormat()`, `locale.maximize()`)
    // take their prototype from (Intrinsics.IntlPrototype) — not from `globalThis.Intl`, which
    // guest code may replace.
    internal static JSObject GetIntlObject(JSContext realm)
        => Cache.GetValue(realm, static global => CreateIntlObject((JSContext)global));

    private static JSObject CreateIntlObject(JSContext realm)
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
                var key = a.Get1().StringValue;
                // The numbering-system and calendar enumerations are backed by real data; other keys
                // (currency, collation, …) return an empty list rather than throwing, until their data
                // is wired up.
                if (key == "numberingSystem")
                {
                    var list = JSValue.CreateArray();
                    foreach (var ns in SupportedNumberingSystemsSorted)
                        list.AddArrayItem(JSValue.CreateString(ns));
                    return list;
                }

                if (key == "calendar")
                {
                    var list = JSValue.CreateArray();
                    foreach (var cal in AvailableCanonicalCalendars)
                        list.AddArrayItem(JSValue.CreateString(cal));
                    return list;
                }

                if (key == "currency")
                {
                    var list = JSValue.CreateArray();
                    foreach (var c in IntlEnumerationData.Currencies)
                        list.AddArrayItem(JSValue.CreateString(c));
                    return list;
                }

                if (key == "collation")
                {
                    // The collations this engine recognises (the set the Collator resolves
                    // against), excluding the reserved "standard"/"search", in ascending order.
                    var list = JSValue.CreateArray();
                    foreach (var co in SupportedCollationsSorted)
                        list.AddArrayItem(JSValue.CreateString(co));
                    return list;
                }

                if (key == "unit")
                {
                    var list = JSValue.CreateArray();
                    foreach (var u in IntlEnumerationData.Units)
                        list.AddArrayItem(JSValue.CreateString(u));
                    return list;
                }

                // "timeZone" returns AvailablePrimaryTimeZoneIdentifiers() — the shared database's
                // primary identifiers, CLDR's, sorted in code-unit order (test262 supportedValuesOf/
                // timeZones, timeZones-include-non-continental; milestone SJL-8.6).
                if (key == "timeZone")
                {
                    var list = JSValue.CreateArray();
                    foreach (var tz in TemporalTimeZones.PrimaryIdentifiers)
                        list.AddArrayItem(JSValue.CreateString(tz));
                    return list;
                }

                // §Intl.supportedValuesOf step 8.a: any other key is invalid — throw a RangeError
                // (test262 Intl/supportedValuesOf/invalid-key).
                throw JSEngine.NewRangeError($"Intl.supportedValuesOf: invalid key \"{key}\"");
            }, "supportedValuesOf", "function supportedValuesOf() { [native code] }", length: 1, createPrototype: false),
            JSPropertyAttributes.ConfigurableValue);
        // Intl[@@toStringTag] = "Intl"
        intl.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString("Intl"), JSPropertyAttributes.ConfigurableReadonlyValue);

        if (realm != null)
        {
            foreach (var (key, qualifiedName) in IntrinsicConstructorKeys)
            {
                if (intl[key] is JSFunction constructor)
                    realm.RegisterIntrinsic(qualifiedName, constructor, constructor.prototype);
            }
        }

        return intl;
    }

    private static readonly (KeyString Key, KeyString QualifiedName)[] IntrinsicConstructorKeys =
    [
        (DateTimeFormatKey, KeyStrings.GetOrCreate("Intl.DateTimeFormat")),
        (RelativeTimeFormatKey, KeyStrings.GetOrCreate("Intl.RelativeTimeFormat")),
        (NumberFormatKey, KeyStrings.GetOrCreate("Intl.NumberFormat")),
        (CollatorKey, KeyStrings.GetOrCreate("Intl.Collator")),
        (DisplayNamesKey, KeyStrings.GetOrCreate("Intl.DisplayNames")),
        (DurationFormatKey, KeyStrings.GetOrCreate("Intl.DurationFormat")),
        (ListFormatKey, KeyStrings.GetOrCreate("Intl.ListFormat")),
        (LocaleKey, KeyStrings.GetOrCreate("Intl.Locale")),
        (PluralRulesKey, KeyStrings.GetOrCreate("Intl.PluralRules")),
        (SegmenterKey, KeyStrings.GetOrCreate("Intl.Segmenter")),
    ];

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
                var rules = JSIntlPluralRules.Of(a.This, "select").Rules;

                // ToIntlMathematicalValue of the argument (a BigInt's included, row P2), then ResolvePlural over
                // FormatNumericToString's string (row P1), the shared library's.
                var n = ToIntlMathematicalValue(a.Get1());
                return JSValue.CreateString(rules.ResolvePlural(n, null).PluralCategory);
            }, "select", "function select() { [native code] }", createPrototype: false, length: 1),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("resolvedOptions"),
            new JSFunction(JSIntlPluralRules.ResolvedOptionsPrototype, "resolvedOptions", "function resolvedOptions() { [native code] }", createPrototype: false, length: 0),
            JSPropertyAttributes.ConfigurableValue);
        constructor.prototype.FastAddValue(KeyStrings.GetOrCreate("selectRange"),
            new JSFunction(static (in Arguments a) =>
            {
                var rules = JSIntlPluralRules.Of(a.This, "selectRange").Rules;

                var start = a.Get1();
                var end = a.GetAt(1);
                if (start.IsUndefined || end.IsUndefined)
                    throw JSEngine.NewTypeError("Intl.PluralRules.prototype.selectRange requires defined start and end values");

                // ECMA-402 ResolvePluralRange, the shared library's: both ends ToIntlMathematicalValue's, a NaN start or end
                // the RangeError of its step 1; ends that format alike take the start's category (row P5a, a correction),
                // and other ranges CLDR's plural ranges (row P5b, an adoption), where every range answered "other".
                var x = ToIntlMathematicalValue(start);
                var y = ToIntlMathematicalValue(end);
                if (rules.ResolvePluralRange(x, y, out var category, null) != IntlStatus.Completed)
                    throw JSEngine.NewRangeError("Intl.PluralRules.prototype.selectRange called with a NaN start or end value");
                return JSValue.CreateString(category);
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
            // available locales: the shared library's FilterLocales (SJL-9.5, sub-area (a);
            // row L36), whose lookup and best-fit matchers are both the lookup matcher (L38).
            var requested = CanonicalizeLocaleList(a.Get1());
            var options = CoerceOptionsToObject(a.GetAt(1));
            _ = GetOption(options, LocaleMatcherKey, ["lookup", "best fit"], false, "best fit");
            var subset = JSValue.CreateArray();
            foreach (var locale in LocaleNegotiation.FilterLocales(JSIntlLocaleProviders.AvailableLocales, RequestedLocales(requested)))
                subset.AddArrayItem(JSValue.CreateString(locale));
            return subset;
        }, "supportedLocalesOf", "function supportedLocalesOf() { [native code] }", length: 1, createPrototype: false);

    // The Strings of a list CanonicalizeLocaleList answered, in its order.
    private static List<string> RequestedLocales(JSValue canonicalLocales)
    {
        var requested = new List<string>();
        if (canonicalLocales is not JSObject list)
            return requested;

        var length = list[KeyStrings.length].UIntValue;
        for (uint i = 0; i < length; i++)
        {
            var locale = list[i];
            if (!locale.IsUndefined)
                requested.Add(locale.StringValue);
        }

        return requested;
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
                var options = ValidateConstructorArguments("DurationFormat", in a, out var canonical, coerceOptions: false);
                return new JSIntlDurationFormat(canonical, options);
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
            var options = ValidateConstructorArguments("ListFormat", in a, out var canonical, coerceOptions: false);
            var locale = ResolveLocale(canonical, NoRelevantKey).Locale;
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
            var options = ValidateConstructorArguments("Segmenter", in a, out var canonical, coerceOptions: false);
            var locale = ResolveLocaleFromCanonical(canonical, NoRelevantKeys);
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
        // Intl.DisplayNames.supportedLocalesOf, which every other service has (row L37, a correction).
        constructor.FastAddValue(SupportedLocalesOfKey, CreateSupportedLocalesOfFunction(), JSPropertyAttributes.ConfigurableValue);
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

                // [[BoundFormat]]: made once, an anonymous function of length 1 (row T4, a correction: a new function was
                // made at each read, so dtf.format !== dtf.format).
                if (@this.BoundFormat is null)
                {
                    var format = new JSFunction((in Arguments inner) => @this.Format(in inner), "format", "function format() { [native code] }", createPrototype: false, length: 1);
                    format.SetNameProperty(string.Empty);
                    @this.BoundFormat = format;
                }

                return @this.BoundFormat;
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
                    var @this = JSIntlNumberFormat.Of(a.This, "format");

                    // [[BoundFormat]]: made once, an anonymous function of length 1 (row N11, a correction: a new
                    // function was made at each read, so nf.format !== nf.format).
                    if (@this.BoundFormat is null)
                    {
                        var format = new JSFunction((in Arguments inner) => @this.Format(in inner), "format", "function format() { [native code] }", createPrototype: false, length: 1);
                        format.SetNameProperty(string.Empty);
                        @this.BoundFormat = format;
                    }

                    return @this.BoundFormat;
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
        => ValidateConstructorArguments(name, in a, out _, requireNew, coerceOptions);

    internal static JSObject ValidateConstructorArguments(string name, in Arguments a, out JSValue canonicalLocales, bool requireNew = true, bool coerceOptions = true)
    {
        // Intl.NumberFormat, Intl.DateTimeFormat and Intl.Collator are legacy
        // constructors (ECMA-402): they may be called as ordinary functions
        // without `new`, in which case they still construct and return an
        // instance. The remaining Intl constructors require `new`.
        if (requireNew && JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"Intl.{name} requires 'new'");

        // CanonicalizeLocaleList runs once, ahead of the options coercion, and both
        // validates the locales argument and produces the requested-locale list the
        // constructor resolves from. Reusing this list (rather than re-canonicalizing
        // the user's object in ResolveLocale) keeps the locales object's length/index
        // getters observed exactly once (test262 .../locales-symbol-length).
        canonicalLocales = CanonicalizeLocaleList(a.Get1());
        var options = ValidateOptionsArgument(a.GetAt(1), coerceOptions);
        // Every Intl service constructor reads localeMatcher first (right after
        // coercing the options object) via GetOption(..., «lookup, best fit», ...),
        // which is a RangeError for any other value. Validate it centrally so all
        // constructors reject an invalid localeMatcher.
        _ = GetOption(options, LocaleMatcherKey, ["lookup", "best fit"], false, "best fit");
        return options;
    }

    // The Intl.Locale constructor (ECMA-402 sec-intl.locale) over the shared library's LocaleRecord (SJL-9.5, sub-area (a);
    // rows L13 and L14): the tag and every option are read here, in the specification's order, each option checked as it is
    // read so that a refused one stops the reads where ECMA-402 does; LocaleRecord.TryCreate then canonicalises the tag,
    // applies UpdateLanguageId and makes the record. Every check is over ASCII alone, so a KELVIN SIGN or a dotted capital I
    // in an option is a RangeError, never folded into a letter first (row L14, a correction).
    // Two answers move here that row L13, classified "Same", does not name; each follows the text, and each is a correction
    // of Broiler.JS for task 8.7's changelog, under a dated note on row L13 (recorded 2026-11-04, at SJL-9.5):
    // - a tag argument that is an Intl.Locale gives its [[Locale]] (sec-intl.locale step 8), where ToString read it, so an
    //   overridden Intl.Locale.prototype.toString is no longer called;
    // - the options are coerced (CoerceOptionsToObject, step 10) before the tag is checked (IsWellFormedLanguageTag, step
    //   11), so new Intl.Locale(<a malformed tag>, null) is a TypeError, where the tag's RangeError came first.
    internal static LocaleRecord ValidateLocaleConstructorArguments(in Arguments a)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Intl.Locale requires 'new'");

        var tag = a.Get1();
        if (!tag.IsString && !tag.IsObject)
            throw JSEngine.NewTypeError("Locale tag must be a string or object");

        // An Intl.Locale argument gives its [[Locale]], never its toString.
        var tagString = tag is JSIntlLocale existing ? existing.Tag : tag.StringValue;
        var options = ValidateOptionsArgument(a.GetAt(1));
        if (LocaleId.TryParse(tagString, out _) != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid language tag");

        // UpdateLanguageId's reads, each checked as it is read.
        var language = options == null ? null : OptionString(options, LanguageKey);
        if (language != null && LocaleRecord.CheckLanguage(language) != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid language option");

        var script = options == null ? null : OptionString(options, ScriptKey);
        if (script != null && LocaleRecord.CheckScript(script) != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid script option");

        var region = options == null ? null : OptionString(options, RegionKey);
        if (region != null && LocaleRecord.CheckRegion(region) != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid region option");

        var variants = options == null ? null : OptionString(options, VariantsKey);
        if (variants != null && LocaleRecord.CheckVariants(variants) != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid variants option");

        // The keyword options, in order: calendar, collation, firstDayOfWeek, hourCycle, caseFirst, numeric, numberingSystem.
        var calendar = ReadLocaleTypeOption(options, CalendarOptionKey, "calendar");
        var collation = ReadLocaleTypeOption(options, CollationKey, "collation");
        var firstDayOfWeek = options == null ? null : OptionString(options, FirstDayOfWeekKey);
        if (firstDayOfWeek != null && LocaleRecord.CheckFirstDayOfWeek(firstDayOfWeek) != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid firstDayOfWeek option");

        var hourCycle = GetOption(options, HourCycleOptionKey, ["h11", "h12", "h23", "h24"], false, null);
        var caseFirst = GetOption(options, CaseFirstOptionKey, ["upper", "lower", "false"], false, null);
        var numericOption = options == null ? JSUndefined.Value : options[NumericOptionKey];
        bool? numeric = numericOption.IsUndefined ? null : numericOption.BooleanValue;
        var numberingSystem = ReadLocaleTypeOption(options, NumberingSystemKey, "numberingSystem");

        var status = LocaleRecord.TryCreate(tagString, new LocaleOptions
        {
            Language = language,
            Script = script,
            Region = region,
            Variants = variants,
            Calendar = calendar,
            Collation = collation,
            FirstDayOfWeek = firstDayOfWeek,
            HourCycle = hourCycle,
            CaseFirst = caseFirst,
            Numeric = numeric,
            NumberingSystem = numberingSystem,
        }, JSIntlData.LocaleData, out var record);

        return status == IntlStatus.Completed && record != null
            ? record
            : throw JSEngine.NewRangeError("Invalid language tag");
    }

    // A calendar, collation or numberingSystem option of the Locale constructor: its String, held to UTS #35's `type`.
    private static string ReadLocaleTypeOption(JSObject options, KeyString key, string name)
    {
        var value = options == null ? null : OptionString(options, key);
        if (value != null && LocaleRecord.CheckType(value) != IntlStatus.Completed)
            throw JSEngine.NewRangeError($"Invalid {name} option");
        return value;
    }

    private static readonly KeyString VariantsKey = KeyStrings.GetOrCreate("variants");
    private static readonly KeyString CalendarOptionKey = KeyStrings.GetOrCreate("calendar");
    private static readonly KeyString FirstDayOfWeekKey = KeyStrings.GetOrCreate("firstDayOfWeek");
    private static readonly KeyString HourCycleOptionKey = KeyStrings.GetOrCreate("hourCycle");
    private static readonly KeyString CaseFirstOptionKey = KeyStrings.GetOrCreate("caseFirst");
    private static readonly KeyString NumericOptionKey = KeyStrings.GetOrCreate("numeric");

    // AvailableCollations (Intl.supportedValuesOf("collation")) in ascending code-unit
    // order. Per spec this is the set of collations for which the engine actually provides
    // Collator functionality — i.e. those some locale RESOLVES (resolvedOptions().collation
    // === co), NOT the broader set of collation types the BCP 47 data defines and merely ACCEPTED as an option value (an
    // unrecognised-for-the-locale option silently falls back to "default"). Our Collator
    // resolves only the root collations emoji/eor (available everywhere) plus the locale
    // tailorings exposed by NetCollationSuffix (de phonebk; zh pinyin/stroke/phonetic/
    // zhuyin/trad; ja/ko/zh unihan). Listing a collation here that no locale resolves would
    // fail test262 intl402/Intl/supportedValuesOf/collations-accepted-by-Collator.js
    // (#912 Problem 11), so this list is kept in sync with NetCollationSuffix.
    internal static readonly string[] SupportedCollationsSorted = BuildSortedCollations();

    private static string[] BuildSortedCollations()
    {
        var array = new[]
        {
            "emoji", "eor", "phonebk", "phonetic", "pinyin", "stroke", "trad", "unihan", "zhuyin",
        };
        System.Array.Sort(array, StringComparer.Ordinal);
        return array;
    }

    // True when a String is a well-formed Unicode BCP 47 extension type, UTS #35's `type` (the grammar the calendar and
    // numberingSystem options must satisfy), over ASCII alone (the shared library's LocaleId.IsUnicodeType; row L14).
    internal static bool IsWellFormedUnicodeKeywordType(string value)
        => !string.IsNullOrEmpty(value) && LocaleId.IsUnicodeType(value);

    // Canonicalizes a requested collation value (option or -u-co- tag) and returns it only
    // when it is a collation the BCP 47 data defines, by its preferred value; an unknown (or reserved
    // "standard"/"search") value returns null so the caller falls back to the "default" collation.
    // The aliases and the list are the shared library's BCP 47 data (SJL-9.5, sub-area (a)); the
    // Collator's own resolution stays its host path until SJL-17.
    internal static string CanonicalizeCollation(string value)
    {
        if (value == null || !LocaleId.IsUnicodeType(value))
            return null;
        var lower = LocaleId.AsciiLowercase(value);
        if (!JSIntlData.LocaleData.TryExtensionType('u', "co", lower, out var canonical))
            return null;
        return canonical is "standard" or "search" ? null : canonical;
    }

    private static string OptionString(JSObject options, KeyString key)
    {
        var v = options[key];
        return v == null || v.IsUndefined ? null : v.ToString();
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

    internal static JSValue CanonicalizeLocaleList(JSValue locales)
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

        // ToLength(Get(O, "length")): ToNumber (a Symbol length throws a TypeError via DoubleValue),
        // then the shared library's ToLength (SJL-4.5), so a negative length yields an empty list and
        // the indexed getters are never read (rather than wrapping the negative into a uint).
        var lengthInteger = IntegerConversions.ToLength(lengthValue.DoubleValue);
        var length = lengthInteger > uint.MaxValue ? uint.MaxValue : (uint)lengthInteger;
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

    // IsWellFormedLanguageTag and CanonicalizeUnicodeLocaleId (ECMA-402 sec-canonicalizelocalelist, steps 7.c.v and
    // 7.c.vi): the shared library's LocaleId over the data home's alias, likely-subtags and BCP 47 tables (SJL-9.5,
    // sub-area (a)), which reads ASCII alone (rows L2 and L45), applies every CLDR alias (rows L5 to L8, data) and
    // canonicalises a -t- extension's tlang as UTS #35 Annex C does (row L10). A tag that is not well-formed is a RangeError.
    internal static string ValidateLanguageTag(string tag)
        => LocaleId.TryCanonicalize(tag, JSIntlData.LocaleData, out var canonical) == IntlStatus.Completed
            ? canonical
            : throw JSEngine.NewRangeError("Invalid language tag");

    // The names of the library's enumerations in their order, which resolvedOptions reports and the options map onto
    // (SJL-9.5, task 8.4, sub-areas (b) and (c); Broiler.VM's JSD-0082). GetOption reads them as the options' allowed
    // values too, since it checks membership only.
    // Revised 2026-11-08, at SJL-9.5 (task 8.4, a review fix): the NumberFormat constructor read its own copies,
    // CurrencyDisplayValues ["code", "symbol", "narrowSymbol", "name"], CurrencySignValues, NumberFormatStyleValues,
    // NotationValues, SignDisplayValues, CompactDisplayValues and UnitDisplayValues, each the same set; they are deleted.
    internal static readonly string[] NumberStyleNames = ["decimal", "percent", "currency", "unit"];
    internal static readonly string[] CurrencyDisplayNames = ["symbol", "narrowSymbol", "code", "name"];
    internal static readonly string[] CurrencySignNames = ["standard", "accounting"];
    internal static readonly string[] UnitDisplayNames = ["short", "narrow", "long"];
    internal static readonly string[] NotationNames = ["standard", "scientific", "engineering", "compact"];
    internal static readonly string[] CompactDisplayNames = ["short", "long"];
    internal static readonly string[] SignDisplayNames = ["auto", "never", "always", "exceptZero", "negative"];
    internal static readonly string[] GroupingNames = ["auto", "always", "min2", null];
    internal static readonly string[] RoundingModeNames = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"];
    internal static readonly string[] RoundingPriorityNames = ["auto", "morePrecision", "lessPrecision"];
    internal static readonly string[] TrailingZeroDisplayNames = ["auto", "stripIfInteger"];
    internal static readonly string[] FormatStyleNames = ["long", "short", "narrow"];

    // Intl.NumberFormat's constructor steps after CanonicalizeLocaleList, CoerceOptionsToObject and localeMatcher
    // (ECMA-402 sec-intl.numberformat): every option is read here, in the specification's order, each checked as it is
    // read, and the record is the shared library's NumberFormatter (SJL-9.5, task 8.4, sub-area (c); Broiler.VM's JSD-0082,
    // decision D24). Each option is read once, at construction (row N10, a correction): format, formatToParts, the ranges and
    // resolvedOptions read the record. ResolveLocale's steps 5 to 12 are the library's, over the nu values the data gives the
    // matched locale (decision D13, row L22), and so is the default numbering system.
    // It replaces ValidateNumberFormatOptions, SnapshotDigitOptions, ReadRoundingOptions, ResolveUseGrouping, the
    // SanctionedSingleUnits set and IsWellFormedUnitIdentifier, and JSIntlNumberFormatResolved (deleted 2026-11-08).
    internal static NumberFormatter CreateNumberFormatter(JSValue canonicalLocales, JSObject options)
    {
        var numberingSystem = ReadNumberingSystemOption(options);
        var resolved = ResolveNumberingSystemLocale(canonicalLocales, numberingSystem);
        var data = JSIntlData.LocaleData;

        // SetNumberFormatUnitOptions.
        var style = GetOption(options, StyleKey, NumberStyleNames, false, "decimal");
        var currency = OptionStringValue(options, CurrencyKey);
        if (currency == null)
        {
            if (style == "currency")
                throw JSEngine.NewTypeError("Intl.NumberFormat currency style requires a currency option");
        }
        else if (!NumberFormatter.IsWellFormedCurrencyCode(currency))
            throw JSEngine.NewRangeError("Invalid currency option");

        var currencyDisplay = GetOption(options, CurrencyDisplayKey, CurrencyDisplayNames, false, "symbol");
        var currencySign = GetOption(options, CurrencySignKey, CurrencySignNames, false, "standard");
        var unit = OptionStringValue(options, UnitKey);
        if (unit == null)
        {
            if (style == "unit")
                throw JSEngine.NewTypeError("Intl.NumberFormat unit style requires a unit option");
        }
        else if (!NumberFormatter.IsWellFormedUnitIdentifier(unit))
            throw JSEngine.NewRangeError($"Invalid unit identifier: {unit}");

        var unitDisplay = GetOption(options, UnitDisplayKey, UnitDisplayNames, false, "short");
        var notation = (NumberNotation)System.Array.IndexOf(NotationNames, GetOption(options, NotationKey, NotationNames, false, "standard"));
        var numberStyle = (NumberStyle)System.Array.IndexOf(NumberStyleNames, style);
        var digitOptions = SetNumberFormatDigitOptions(options, DigitOptionRules.NumberFormatDefaults(numberStyle, currency, notation, data), notation);
        var compactDisplay = GetOption(options, CompactDisplayKey, CompactDisplayNames, false, "short");
        var useGrouping = GetUseGroupingOption(options, notation == NumberNotation.Compact ? NumberGrouping.Min2 : NumberGrouping.Auto);
        var signDisplay = GetOption(options, SignDisplayKey, SignDisplayNames, false, "auto");

        var dataLocale = JSIntlLocaleProviders.DataLocale(resolved.DataLocale);
        return NumberFormatter.Create(DataLocalePluralData.For(dataLocale), resolved.Locale, dataLocale,
            resolved.KeyValues["nu"] ?? "latn", new NumberFormatOptions
            {
                Style = numberStyle,
                Currency = currency,
                CurrencyDisplay = (CurrencyDisplay)System.Array.IndexOf(CurrencyDisplayNames, currencyDisplay),
                CurrencySign = (CurrencySign)System.Array.IndexOf(CurrencySignNames, currencySign),
                Unit = unit,
                UnitDisplay = (UnitDisplay)System.Array.IndexOf(UnitDisplayNames, unitDisplay),
                DigitOptions = digitOptions,
                Notation = notation,
                CompactDisplay = (CompactDisplay)System.Array.IndexOf(CompactDisplayNames, compactDisplay),
                UseGrouping = useGrouping,
                SignDisplay = (SignDisplay)System.Array.IndexOf(SignDisplayNames, signDisplay),
            });
    }

    // An option's String (ToString of a defined value), or null when it is undefined.
    private static string OptionStringValue(JSObject options, KeyString key)
    {
        var value = options == null ? JSUndefined.Value : options[key];
        return value == null || value.IsUndefined ? null : value.StringValue;
    }

    // ECMA-402's SetNumberFormatDigitOptions (sec-setnumberformatdigitoptions), split at its conversions as the shared
    // library splits it (sub-area (b)): every read here, in the specification's order, each checked as it is read, the
    // digit values read first and converted only where DigitOptionRules.Plan says they are used (row M4, a correction: a
    // value is converted once, and never when the other pair wins); the library takes the decisions after them, and a
    // status it answers is the error the step throws (min above max, an increment with significant digits or a priority,
    // an increment with unequal fraction digits; rows M4, M5 and P7, corrections).
    internal static DigitOptions SetNumberFormatDigitOptions(JSObject options, FractionDigitDefaults defaults, NumberNotation notation)
    {
        var mnid = GetNumberOption(options, MinimumIntegerDigitsKey, 1, 21) ?? 1;
        var mnfd = OptionValue(options, MinimumFractionDigitsKey);
        var mxfd = OptionValue(options, MaximumFractionDigitsKey);
        var mnsd = OptionValue(options, MinimumSignificantDigitsKey);
        var mxsd = OptionValue(options, MaximumSignificantDigitsKey);
        var roundingIncrement = GetNumberOption(options, RoundingIncrementKey, 1, 5000) ?? 1;
        if (!DigitOptionRules.IsAdmittedRoundingIncrement(roundingIncrement))
            throw JSEngine.NewRangeError("roundingIncrement value is out of range.");

        // GetOption(options, "roundingMode", string, « the nine names », "halfExpand"): the property read and its ToString
        // are the engine's; the shared library's TryParseRoundingMode decides whether the text is one of the nine names.
        var roundingModeName = OptionStringValue(options, RoundingModeKey) ?? "halfExpand";
        if (!ExactRounding.TryParseRoundingMode(roundingModeName, out var roundingMode))
            throw JSEngine.NewRangeError($"Invalid {RoundingModeKey} option");

        var roundingPriority = (RoundingPriority)System.Array.IndexOf(RoundingPriorityNames,
            GetOption(options, RoundingPriorityKey, RoundingPriorityNames, false, "auto"));
        var trailingZeroDisplay = (TrailingZeroDisplay)System.Array.IndexOf(TrailingZeroDisplayNames,
            GetOption(options, TrailingZeroDisplayKey, TrailingZeroDisplayNames, false, "auto"));

        var hasSd = !mnsd.IsUndefined || !mxsd.IsUndefined;
        var hasFd = !mnfd.IsUndefined || !mxfd.IsUndefined;
        var plan = DigitOptionRules.Plan(hasSd, hasFd, roundingPriority, notation);
        int? minSd = null, maxSd = null, minFd = null, maxFd = null;

        // The conversions the plan selects, the minimum before the maximum.
        if (plan.NeedSignificantDigits && hasSd)
        {
            minSd = DefaultNumberOption(mnsd, 1, 21, 1, MinimumSignificantDigitsKey);
            maxSd = DefaultNumberOption(mxsd, minSd!.Value, 21, 21, MaximumSignificantDigitsKey);
        }

        if (plan.NeedFractionDigits && hasFd)
        {
            minFd = DefaultNumberOption(mnfd, 0, 100, null, MinimumFractionDigitsKey);
            maxFd = DefaultNumberOption(mxfd, 0, 100, null, MaximumFractionDigitsKey);
        }

        var values = new DigitOptionValues(mnid, hasSd, minSd, maxSd, hasFd, minFd, maxFd, roundingIncrement,
            roundingMode, roundingPriority, trailingZeroDisplay);

        return DigitOptionRules.SetNumberFormatDigitOptions(values, defaults, notation, out var digits) switch
        {
            IntlStatus.Completed => digits!,
            IntlStatus.RoundingIncrementNeedsFractionDigits => throw JSEngine.NewTypeError(
                "roundingIncrement may only be used with fractionDigits rounding (roundingPriority \"auto\" and no significant-digit options)"),
            IntlStatus.RoundingIncrementNeedsEqualFractionDigits => throw JSEngine.NewRangeError("maximumFractionDigits is not equal to minimumFractionDigits"),
            IntlStatus.FractionDigitsOutOfOrder => throw JSEngine.NewRangeError("minimumFractionDigits is greater than maximumFractionDigits"),
            _ => throw JSEngine.NewRangeError("roundingIncrement value is out of range."),
        };
    }

    private static readonly KeyString MinimumIntegerDigitsKey = KeyStrings.GetOrCreate("minimumIntegerDigits");
    private static readonly KeyString MinimumFractionDigitsKey = KeyStrings.GetOrCreate("minimumFractionDigits");
    private static readonly KeyString MaximumFractionDigitsKey = KeyStrings.GetOrCreate("maximumFractionDigits");
    private static readonly KeyString MinimumSignificantDigitsKey = KeyStrings.GetOrCreate("minimumSignificantDigits");
    private static readonly KeyString MaximumSignificantDigitsKey = KeyStrings.GetOrCreate("maximumSignificantDigits");
    private static readonly KeyString UseGroupingKey = KeyStrings.GetOrCreate("useGrouping");

    // An option's value as Get reads it, undefined when the options are.
    private static JSValue OptionValue(JSObject options, KeyString key)
        => options == null ? JSUndefined.Value : options[key] ?? JSUndefined.Value;

    // ECMA-402's DefaultNumberOption (sec-defaultnumberoption): ToNumber here, the range check and the floor the shared
    // library's DigitOptionRules.DefaultNumberOption.
    private static int? DefaultNumberOption(JSValue value, int minimum, int maximum, int? fallback, KeyString key)
    {
        if (value.IsUndefined)
            return fallback;

        if (DigitOptionRules.DefaultNumberOption(value.DoubleValue, minimum, maximum, out var result) != IntlStatus.Completed)
            throw JSEngine.NewRangeError($"Invalid {key} option");

        return result;
    }

    // ECMA-402's GetBooleanOrStringNumberFormatOption for useGrouping (sec-getbooleanorstringnumberformatoption, and
    // Intl.NumberFormat's step after it): true is "always", a value ToBoolean reads as false is the boolean false, the
    // strings "true" and "false" are the default, and any other string outside « "min2", "auto", "always" » is a RangeError.
    private static NumberGrouping GetUseGroupingOption(JSObject options, NumberGrouping fallback)
    {
        var value = OptionValue(options, UseGroupingKey);
        if (value.IsUndefined)
            return fallback;
        if (value.IsBoolean && value.BooleanValue)
            return NumberGrouping.Always;
        if (!value.BooleanValue)
            return NumberGrouping.Off;

        var text = value.StringValue;
        if (text is "true" or "false")
            return fallback;

        var index = System.Array.IndexOf(GroupingNames, text);
        if (index < 0 || text == null)
            throw JSEngine.NewRangeError("useGrouping value is not supported");

        return (NumberGrouping)index;
    }

    // ECMA-402's ToIntlMathematicalValue (sec-tointlmathematicalvalue): ToPrimitive with the hint number here; the value is
    // the shared library's IntlDecimal (sub-area (b)): a BigInt's digits, a String's exact decimal value (row M2a) read under
    // this engine's unbounded BigInt policy, and a Number's Number::toString digits (row M1a, a correction: the double's
    // binary value, ExactRational's, rounded where ECMA-402 rounds its decimal digits). A String the ToPrimitive gives is read
    // as a String (row M1b, a correction).
    internal static IntlDecimal ToIntlMathematicalValue(JSValue value)
    {
        value ??= JSUndefined.Value;
        var primitive = value is JSObject @object ? @object.ToNumberPrimitive() : value;
        if (primitive is BigInt.JSBigInt bigint)
            return IntlDecimal.FromBigInt(bigint.value, null);
        if (primitive.IsString)
            return IntlDecimal.FromString(primitive.StringValue, BigIntPolicy.Unbounded, null);
        if (primitive.IsSymbol)
            throw JSEngine.NewTypeError("Cannot convert a Symbol value to a number");
        return IntlDecimal.FromNumber(primitive.DoubleValue);
    }

    // ResolveLocale (sec-resolvelocale) over a CanonicalizeLocaleList result, wholly the shared library's: its matching
    // steps over the available-locale provider (row L38), then its steps 5 to 12 over the service's relevant keys, each with
    // the values the locale data gives the matched locale (SJL-9.5, task 8.4; these replace, for the services of sub-areas
    // (b) and (c), ResolveLocaleFromCanonical's keyword filter and ResolveNumberingSystem's hand negotiation).
    internal static ResolvedLocale ResolveLocale(JSValue canonicalLocales, Func<string, IReadOnlyList<RelevantKey>> relevantKeys)
    {
        var match = LocaleNegotiation.MatchLocale(
            JSIntlLocaleProviders.AvailableLocales, RequestedLocales(canonicalLocales), JSIntlLocaleProviders.DefaultLocale);
        return LocaleNegotiation.ResolveLocale(match, relevantKeys(match.Locale), JSIntlData.LocaleData);
    }

    // The relevant keys of the services that resolve none: ListFormat, DisplayNames and PluralRules.
    internal static IReadOnlyList<RelevantKey> NoRelevantKey(string locale) => [];

    // ResolveLocale for a service whose one relevant key is nu (NumberFormat, RelativeTimeFormat): the nu values of the
    // matched locale's data, its default numbering system first (decision D13, row L22), with the numberingSystem option.
    internal static ResolvedLocale ResolveNumberingSystemLocale(JSValue canonicalLocales, string numberingSystemOption)
        => ResolveLocale(canonicalLocales, locale =>
            [new RelevantKey("nu", NumberingSystemValues(locale)) { Option = numberingSystemOption }]);

    // [[LocaleData]].[[<locale>]].[[nu]]: the locale's default numbering system from the data along its chain (latn where
    // the data names none), then latn, then the data's others in code-unit order: the shared library's (decision D13).
    internal static IReadOnlyList<string> NumberingSystemValues(string locale)
        => NumberFormatter.NumberingSystemKeyValues(JSIntlData.LocaleData, JSIntlLocaleProviders.DataLocale(locale));

    // ResolveLocale's matching steps (ECMA-402 sec-resolvelocale, steps 1 to 4) over a CanonicalizeLocaleList result, so the
    // caller's locales object is not read a second time: the shared library's lookup matcher, which is also this engine's best
    // fit (row L38), over the available-locale provider's list, falling back to the default-locale provider's DefaultLocale
    // with no keyword (SJL-9.5, sub-area (a); decision D7). The resolved locale is the available locale matched, never the
    // request itself (row L29, a correction), with the requested -u- keywords of the service's relevant extension keys, in
    // canonical order, and no other (row L30, a correction). Each service then resolves its keys' values against its own data.
    internal static string ResolveLocaleFromCanonical(JSValue canonicalLocales, string[] relevantExtensionKeys)
    {
        var match = LocaleNegotiation.MatchLocale(
            JSIntlLocaleProviders.AvailableLocales, RequestedLocales(canonicalLocales), JSIntlLocaleProviders.DefaultLocale);
        var keywords = new StringBuilder();
        foreach (var keyword in match.ExtensionKeywords)
        {
            if (System.Array.IndexOf(relevantExtensionKeys, keyword.Key) < 0)
                continue;

            keywords.Append('-').Append(keyword.Key);
            if (keyword.Value.Length != 0 && keyword.Value != "true")
                keywords.Append('-').Append(keyword.Value);
        }

        return keywords.Length == 0 ? match.Locale : match.Locale + "-u" + keywords;
    }

    // ResolveLocale's steps 5 to 12, each key's value against the service's data, are still this file's, by hand, until each
    // service's sub-area moves: JSD-0082's X6 gives negotiation to sub-area (a), and the plan's rows for tasks 8.4 to 8.6 do
    // not name these members, so each is listed here with the task whose service is its last reader, for task 8.7's
    // deletion search (recorded 2026-11-04, at SJL-9.5). Each is replaced by LocaleNegotiation.ResolveLocale over the
    // service's RelevantKey list and LocaleId's parts:
    // - task 8.4: ResolveNumberingSystem (NumberFormat, RelativeTimeFormat, DurationFormat; with JSIntlDefaultNumberingSystems,
    //   decision D13); the Collator's co, kf and kn (in JSIntlCollator's constructor, with its StripUnicodeExtension), over
    //   the Collator's data through JSIntlCollator.LocaleData (decision D33 (c); its data and comparison stay SJL-17's),
    //   which no plan row names and which is assigned here to task 8.4;
    // - task 8.5: ResolveCalendar, DateTimeFormat's hc (DropHourCycleExtension, FilterUnicodeExtensionKeywords), and
    //   DefaultHourCycle and Prefers11HourCycle with their tag parsers LanguageSubtag, RegionSubtag, IsAllAlphaSubtag and
    //   IsAllDigitSubtag (row T10);
    // - with the last of their readers: GetUnicodeExtensionType, RemoveUnicodeExtensionKeyword and ReflectExtensionKeyword.
    // Noted 2026-11-08, at SJL-9.5 (task 8.4): NumberFormat, PluralRules, RelativeTimeFormat, ListFormat and DisplayNames
    // resolve through the shared library's ResolveLocale (JSIntl.ResolveLocale, ResolveNumberingSystemLocale), each nu value
    // the data's; ResolveNumberingSystem stays, its values now the data's (decision D13), for DateTimeFormat's (task 8.5) and
    // DurationFormat's (task 8.6) readers alone. The Collator's co, kf and kn stay where they are: the brief for task 8.4
    // gives the collator's move to SJL-17.5, so the item above assigned to task 8.4 waits on the Collator's own cut-over.
    // Noted 2026-11-09, at SJL-9.5 (task 8.5): DateTimeFormat resolves ca, hc and nu through the shared library's ResolveLocale
    // (JSIntl.ResolveLocale over DateTimeFormat.CalendarKeyValues, HourCycleKeyValues and NumberingSystemValues), and its
    // hour cycles are the library's, from CLDR's time data (row T10). The task 8.5 items above are deleted with their last
    // reader: ResolveCalendar, IsAvailableCalendar, IsRenderableCalendar, ReadCalendarOption, DateTimeFormatRelevantKeys,
    // DateTimeFormatRelevantKeysWithoutHourCycle, DropHourCycleExtension, FilterUnicodeExtensionKeywords, the
    // TwelveHourRegions table, DefaultHourCycle, Prefers11HourCycle, LanguageSubtag, RegionSubtag, IsAllAlphaSubtag and
    // IsAllDigitSubtag; so are NumberingSystemDigit (the digits are the library's, from the data's numbering-system table)
    // and DefaultNumberingSystem, whose last reader was DateTimeFormat's constructor from a CultureInfo (row T42).
    // GetUnicodeExtensionType, RemoveUnicodeExtensionKeyword and ReflectExtensionKeyword keep the Collator as a reader.
    // Noted 2026-11-09, at SJL-9.5 (task 8.6): DurationFormat resolves nu through the shared library's ResolveLocale
    // (ResolveNumberingSystemLocale), so ResolveNumberingSystem (both overloads), NumberFormatRelevantKeys, NumberParts,
    // JSIntlNumberFormat.ComputeFormatParts and JSIntlListFormat.FormatPartsForUnits are deleted with their last reader.
    // ResolveLocaleFromCanonical stays for the Segmenter (SJL-7's, NoRelevantKeys) and the Collator (CollatorRelevantKeys);
    // GetUnicodeExtensionType, RemoveUnicodeExtensionKeyword and ReflectExtensionKeyword stay for the Collator alone, whose
    // move the brief for task 8.6 leaves to SJL-17.5.
    // Revised 2026-11-09, at SJL-9.5 (review of tasks 8.5 and 8.6): two sentences above no longer hold as written.
    // - The 2026-11-08 note's "ResolveNumberingSystem stays, its values now the data's (decision D13), for DateTimeFormat's
    //   (task 8.5) and DurationFormat's (task 8.6) readers alone": both readers left at tasks 8.5 and 8.6, and the method
    //   is deleted (the task 8.6 note above).
    // - The task 8.5 note's "its hour cycles are the library's, from CLDR's time data (row T10)": the library reads only
    //   the region-keyed entries of CLDR's time data (ILocaleData.HourCycles(region)), at the tag's region or else the
    //   likely region of the language alone, and never the locale-keyed entries (ar-001, fr-CA, en-001 and the others). So
    //   ar's default is h23 where the data's ar-001 gives h12 (engine vector intl/date-times/families/0028, which matched
    //   at the base and now differs), fr-CA's is h12 where the data's fr-CA gives H (0173, 0175), and zh-Hant's is h23,
    //   CN's, where TW's gives h (0490). It is a library gap for its own merge (decision D52, SJL-D-9).
    // - U+202F before the day period (row T12, CLDR's data) costs test262's
    //   intl402/DateTimeFormat/prototype/format/numbering-system.js, which passes at the base and asks for a plain space
    //   before "AM" (JSD-0082, D21). Both points wait on the owner's ruling.
    // Revised 2026-11-09, at SJL-9.5's stage 4, after the owner's decision of 2026-11-09, "Keep + fix library first", and the
    // shared library's merge Broiler-Platform/Broiler.JS#1108: both points of the revision above are settled. The library reads
    // CLDR's time data for the locale's language and region before its region's, and the whole locale's likely region, so
    // ar-001 is h12 (0028 matches again), fr-CA h23 (0173, 0175) and zh-Hant h12, Taiwan's (0490): the task 8.5 note's "its hour
    // cycles are the library's, from CLDR's time data (row T10)" holds as written. U+202F before the day period stays CLDR's under
    // the owner's ruling "Follow CLDR, dated ruling"; numbering-system.js is lost under row T12, and bundle sjl-9-001's rule is
    // revised to admit that one path as class L (docs/evidence/sjl-9-001/decision-rule-revised-2026-11-09.md).

    // The relevant extension keys of the services that resolve none: ListFormat, Segmenter, DisplayNames and PluralRules.
    // Revised 2026-11-08, at SJL-9.5 (task 8.4): ListFormat, DisplayNames and PluralRules resolve through the shared
    // library's ResolveLocale with NoRelevantKey; Segmenter, SJL-7's, still reads this list.
    internal static readonly string[] NoRelevantKeys = [];

    internal static readonly string[] CollatorRelevantKeys = ["co", "kf", "kn"];

    // The numbering systems with simple digit mappings (ECMA-402 Table: "Numbering
    // System Identifiers"), i.e. the `nu` values an implementation negotiates via
    // ResolveLocale / reports from resolvedOptions. Algorithmic-only aliases ("native",
    // "traditio", "finance") are intentionally absent — they are not valid `-u-nu-`
    // values. Stored sorted (code-unit order) for supportedValuesOf.
    // Revised 2026-11-08, at SJL-9.5 (task 8.4): no service negotiates over this list any more, which also backed
    // IsSupportedNumberingSystem and its HashSet (both deleted); a nu value is supported where the data's numbering-system
    // table holds it (NumberingSystemValues). It stays for Intl.supportedValuesOf, which JSD-0082's X6 gives to sub-area (a).
    internal static readonly string[] SupportedNumberingSystemsSorted =
    [
        "adlm", "ahom", "arab", "arabext", "bali", "beng", "bhks", "brah", "cakm",
        "cham", "deva", "diak", "fullwide", "gara", "gong", "gonm", "gujr", "gukh",
        "guru", "hanidec", "hmng", "hmnp", "java", "kali", "kawi", "khmr", "knda",
        "krai", "lana", "lanatham", "laoo", "latn", "lepc", "limb", "mathbold",
        "mathdbl", "mathmono", "mathsanb", "mathsans", "mlym", "modi", "mong", "mroo",
        "mtei", "mymr", "mymrepka", "mymrpao", "mymrshan", "mymrtlng", "nagm", "newa",
        "nkoo", "olck", "onao", "orya", "osma", "outlined", "rohg", "saur", "segment",
        "shrd", "sind", "sinh", "sora", "sund", "sunu", "takr", "talu", "tamldec",
        "telu", "thai", "tibt", "tirh", "tnsa", "tols", "vaii", "wara", "wcho",
    ];

    // AvailableCanonicalCalendars: the canonical calendar identifiers reported by
    // Intl.supportedValuesOf("calendar"), in code-unit (ascending) order. This is the set
    // required by the Intl era/monthCode additions; "islamic" and "islamic-rgsa" are
    // deliberately excluded (they are not part of the required/available set, matching V8 and
    // test262 supportedValuesOf/calendars-required-by-intl-era-monthcode).
    internal static readonly string[] AvailableCanonicalCalendars =
    [
        "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "gregory", "hebrew",
        "indian", "islamic-civil", "islamic-tbla", "islamic-umalqura",
        "iso8601", "japanese", "persian", "roc",
    ];

    // Reads (and string-coerces) the numberingSystem option from the bag once, returning
    // null when it is absent/undefined. Split out so Intl.NumberFormat can fire the getter
    // in spec order (right after localeMatcher) while the locale negotiation below still
    // happens once the resolved locale tag is known.
    internal static string ReadNumberingSystemOption(JSObject options)
    {
        var optionValue = options?[NumberingSystemKey];
        if (optionValue == null || optionValue.IsUndefined)
            return null;
        var value = optionValue.StringValue;
        // GetOption coerces the value to a string and then validates it against the
        // Unicode BCP-47 `type` grammar (one or more 3-8 alphanumeric subtags); a value
        // that is not well-formed — "", "ab", "latn-ca", "latné" — is a RangeError,
        // independent of whether the runtime actually supports the numbering system.
        if (!IsWellFormedUnicodeKeywordType(value))
            throw JSEngine.NewRangeError($"Invalid numberingSystem option: {value}");
        return value;
    }

    // ResolveLocale's per-key reflection rule: the "-u-<key>" keyword stays in the resolved locale
    // when no option overrides it, or when the option value equals the extension value; an option
    // that differs from the extension drops the keyword (test262 .../resolved-*-unicode-extensions-
    // and-options). extValue is the extension's value (GetUnicodeExtensionType, null when absent;
    // callers normalize an elided boolean "" to "true").
    internal static string ReflectExtensionKeyword(string localeTag, string key, string optionValue, string extValue)
    {
        if (optionValue == null)
            return localeTag;
        return string.Equals(optionValue, extValue, StringComparison.Ordinal)
            ? localeTag
            : RemoveUnicodeExtensionKeyword(localeTag, key);
    }

    // Rebuilds a BCP-47 tag dropping a single Unicode ("-u-") extension keyword (and the
    // whole "-u-" sequence when nothing else survives).
    // Revised 2026-11-09, at SJL-9.5 (task 8.5): FilterUnicodeExtensionKeywords is deleted with DateTimeFormat's hand
    // negotiation; the comment ended "Sibling of FilterUnicodeExtensionKeywords, which keeps a set instead of removing one."
    internal static string RemoveUnicodeExtensionKeyword(string tag, string removeKey)
    {
        if (string.IsNullOrEmpty(tag) || tag.IndexOf("-u-", StringComparison.OrdinalIgnoreCase) < 0)
            return tag;

        var parts = tag.Split('-');
        var u = -1;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length != 1)
                continue;
            // Stop at the private-use ("x") singleton; "-u-" inside private-use is a private
            // subtag, not a Unicode extension to remove.
            if (parts[i][0] == 'u' || parts[i][0] == 'U') { u = i; break; }
            if (parts[i][0] == 'x' || parts[i][0] == 'X') break;
        }

        if (u < 0)
            return tag;

        var end = u + 1;
        var keywords = new List<(string Key, List<string> Types)>();
        while (end < parts.Length && parts[end].Length != 1)
        {
            if (parts[end].Length == 2)
                keywords.Add((parts[end].ToLowerInvariant(), new List<string>()));
            else if (keywords.Count > 0)
                keywords[^1].Types.Add(parts[end]);
            end++;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < u; i++)
        {
            if (i > 0)
                sb.Append('-');
            sb.Append(parts[i]);
        }

        var keptAny = false;
        foreach (var (key, types) in keywords)
        {
            if (string.Equals(key, removeKey, StringComparison.Ordinal))
                continue;

            if (!keptAny)
            {
                sb.Append("-u");
                keptAny = true;
            }

            sb.Append('-').Append(key);
            foreach (var type in types)
                sb.Append('-').Append(type);
        }

        for (var i = end; i < parts.Length; i++)
            sb.Append('-').Append(parts[i]);

        return sb.ToString();
    }

    // Returns the type value of the Unicode ("-u-") extension keyword `key` in `tag`
    // (e.g. GetUnicodeExtensionType("en-US-u-hc-h23", "hc") == "h23"), or null when the
    // keyword is absent. A bare key with no type returns the empty string.
    internal static string GetUnicodeExtensionType(string tag, string key)
    {
        if (string.IsNullOrEmpty(tag))
            return null;

        var parts = tag.Split('-');
        var u = -1;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length != 1)
                continue;
            // A "u" singleton starts the Unicode extension; an "x" singleton starts the
            // private-use sequence (terminal in canonical form), so any "-u-" inside its
            // payload is a private subtag and must not be treated as a Unicode extension.
            if (parts[i][0] == 'u' || parts[i][0] == 'U') { u = i; break; }
            if (parts[i][0] == 'x' || parts[i][0] == 'X') break;
        }

        if (u < 0)
            return null;

        var j = u + 1;
        while (j < parts.Length && parts[j].Length != 1)
        {
            if (parts[j].Length == 2 && string.Equals(parts[j], key, StringComparison.OrdinalIgnoreCase))
            {
                var types = new List<string>();
                var t = j + 1;
                while (t < parts.Length && parts[t].Length >= 3)
                {
                    types.Add(parts[t].ToLowerInvariant());
                    t++;
                }

                return types.Count == 0 ? string.Empty : string.Join("-", types);
            }

            j++;
        }

        return null;
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

    private static void ObserveOptions(JSObject options, params KeyString[] keys)
    {
        if (options == null)
            return;

        foreach (var key in keys)
            _ = options[key];
    }

}

// Intl.RelativeTimeFormat (ECMA-402 sec-relativetimeformat-objects) over the shared library's RelativeTimeFormatter
// (SJL-9.5, task 8.4, sub-area (c); Broiler.VM's JSD-0082, decision D24): the constructor reads localeMatcher,
// numberingSystem, style and numeric in the specification's order, ResolveLocale's nu key is the library's, and the record
// - SingularRelativeTimeUnit, PartitionRelativeTimePattern, its inner NumberFormat with the resolved numbering system (row
// S5, a correction) and its inner PluralRules - is the library's, over the data home's relative-time data (decision D5).
// Deleted (2026-11-08): SingularUnit, PartitionRelativeTimePattern and FillRelativeTimePattern, and the locale, numbering
// system, style and numeric fields. Its public members stay (decision SJL-D-25, disposition (a)).
public class JSIntlRelativeTimeFormat : JSObject
{
    internal RelativeTimeFormatter Formatter { get; }

    public JSValue Format(in Arguments args)
    {
        // ToNumber(value) precedes ToString(unit) per spec.
        var value = (args[0] ?? JSUndefined.Value).DoubleValue;
        var unit = args.GetAt(1).StringValue;
        Completed(Formatter.Format(value, unit, out var text, null), unit);
        return CreateString(text);
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
        Completed(@this.Formatter.FormatToParts(value, unit, out var parts, null), unit);

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var unitKey = KeyStrings.GetOrCreate("unit");

        var array = CreateArray();
        foreach (var (type, partValue, partUnit) in parts)
        {
            var part = new JSObject();
            part.FastAddValue(typeKey, CreateString(type), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(valueKey, CreateString(partValue), JSPropertyAttributes.EnumerableConfigurableValue);
            if (partUnit != null)
                part.FastAddValue(unitKey, CreateString(partUnit), JSPropertyAttributes.EnumerableConfigurableValue);
            array.AddArrayItem(part);
        }
        return array;
    }

    // The library's refusals as the RangeErrors PartitionRelativeTimePattern throws.
    private static void Completed(IntlStatus status, string unit)
    {
        switch (status)
        {
            case IntlStatus.Completed:
                return;
            case IntlStatus.RelativeTimeValueNotFinite:
                throw JSEngine.NewRangeError("Value of Intl.RelativeTimeFormat must be a finite number");
            default:
                throw JSEngine.NewRangeError($"Invalid unit argument for Intl.RelativeTimeFormat: {unit}");
        }
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlRelativeTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.RelativeTimeFormat.prototype.resolvedOptions called on incompatible receiver");

        // Table 33, in its order.
        var formatter = @this.Formatter;
        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), CreateString(formatter.Locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), CreateString(JSIntl.FormatStyleNames[(int)formatter.Style]));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numeric"), CreateString(formatter.Numeric == RelativeTimeNumeric.Auto ? "auto" : "always"));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), CreateString(formatter.NumberingSystem));
        return result;
    }

    public JSIntlRelativeTimeFormat(in Arguments a) : this()
    {
        var options = JSIntl.ValidateConstructorArguments("RelativeTimeFormat", in a, out var canonical);
        var numberingSystem = JSIntl.ReadNumberingSystemOption(options);
        var resolved = JSIntl.ResolveNumberingSystemLocale(canonical, numberingSystem);
        var style = JSIntl.GetOption(options, KeyStrings.GetOrCreate("style"), JSIntl.FormatStyleNames, false, "long");
        var numeric = JSIntl.GetOption(options, KeyStrings.GetOrCreate("numeric"), ["always", "auto"], false, "always");
        // Revised 2026-11-08, at SJL-9.5 (task 8.4, a review fix): the formatter reads its locale data through
        // DataLocalePluralData, as Intl.NumberFormat and Intl.PluralRules do, so its [[PluralRules]] select by the data
        // locale's own rules (pt-PT's "i = 1 and v = 0", not pt's "i = 0..1"). The call read
        // "RelativeTimeFormatter.Create(JSIntlData.LocaleData, resolved.Locale, JSIntlLocaleProviders.DataLocale(resolved.DataLocale), ...".
        var dataLocale = JSIntlLocaleProviders.DataLocale(resolved.DataLocale);
        Formatter = RelativeTimeFormatter.Create(DataLocalePluralData.For(dataLocale), resolved.Locale, dataLocale,
            (FormatStyle)System.Array.IndexOf(JSIntl.FormatStyleNames, style),
            numeric == "auto" ? RelativeTimeNumeric.Auto : RelativeTimeNumeric.Always,
            resolved.KeyValues["nu"] ?? "latn");
    }

    private JSIntlRelativeTimeFormat() : base(CurrentPrototype()) { }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.RelativeTimeFormat");

    // %Intl.RelativeTimeFormat.prototype% of the current realm (Intrinsics.IntlPrototype), never the
    // prototype of whatever `globalThis.Intl.RelativeTimeFormat` holds now: guest code may replace
    // the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);
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
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("granularity"), CreateString(@this.Granularity));
        return result;
    }

    public static JSValue SegmentPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegmenter segmenter)
            throw JSEngine.NewTypeError("Intl.Segmenter.prototype.segment called on incompatible receiver");

        var input = a.Get1().StringValue;
        return new JSIntlSegments(input, segmenter.Granularity);
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.Segmenter");

    // %Intl.Segmenter.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.Segmenter` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);
}

/// <summary>
/// The object returned by Intl.Segmenter.prototype.segment — a %Segments%
/// instance. Exposes <c>containing(index)</c> and is iterable, yielding a
/// Segment Data object per segment.
///
/// The boundaries are UAX #29's default grapheme, word and sentence boundaries,
/// and a word segment's <c>isWordLike</c> is its word status, both Broiler.Unicode's
/// over the pinned Unicode 17.0.0 tables (milestone SJL-7.5), with no locale
/// tailoring and no dictionary. The objects and iterators are this engine's.
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
        // %Segments.prototype% [ @@iterator ] — a method (named "[Symbol.iterator]") returning a Segment
        // Iterator over the segment data objects. Exposed as an own property so `segments[@@iterator]`
        // is observable, in addition to the internal GetIterableEnumerator that backs for-of.
        FastAddValue(
            (IJSSymbol)JSSymbol.iterator,
            CreateFunction(static (in Arguments a) =>
            {
                if (a.This is not JSIntlSegments self)
                    throw JSEngine.NewTypeError("%Segments.prototype%[Symbol.iterator] called on incompatible receiver");
                return self.CreateSegmentIterator();
            }, "[Symbol.iterator]", null, 0, false),
            JSPropertyAttributes.ConfigurableValue);
    }

    // Creates a Segment Iterator: a one-shot iterator object whose next() walks the segment list and
    // yields the same Segment Data objects as the for-of enumeration, and whose own @@iterator returns
    // itself.
    private JSValue CreateSegmentIterator()
    {
        var segments = this;
        var list = Segments;
        var index = 0;
        var valueKey = KeyStrings.GetOrCreate("value");
        var doneKey = KeyStrings.GetOrCreate("done");

        var iterator = new JSObject();
        iterator.FastAddValue(
            KeyStrings.GetOrCreate("next"),
            CreateFunction((in Arguments a) =>
            {
                var result = new JSObject();
                if (index < list.Count)
                {
                    var (start, length) = list[index];
                    index++;
                    result.FastAddValue(valueKey, segments.CreateSegmentDataObject(start, length), JSPropertyAttributes.EnumerableConfigurableValue);
                    result.FastAddValue(doneKey, BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
                }
                else
                {
                    result.FastAddValue(valueKey, JSUndefined.Value, JSPropertyAttributes.EnumerableConfigurableValue);
                    result.FastAddValue(doneKey, BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);
                }

                return result;
            }, "next", null, 0, false),
            JSPropertyAttributes.ConfigurableValue);
        iterator.FastAddValue(
            (IJSSymbol)JSSymbol.iterator,
            CreateFunction(static (in Arguments a) => a.This, "[Symbol.iterator]", null, 0, false),
            JSPropertyAttributes.ConfigurableValue);
        return iterator;
    }

    private List<(int start, int length)> Segments => _segments ??= Compute(_input, _granularity);

    public static JSValue ContainingPrototype(in Arguments a)
    {
        if (a.This is not JSIntlSegments segments)
            throw JSEngine.NewTypeError("%Segments.prototype%.containing called on incompatible receiver");

        // ToIntegerOrInfinity(index): ToNumber, then the shared library's operation (SJL-4.5).
        var n = IntegerConversions.ToIntegerOrInfinity(a.Get1().DoubleValue);

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
        result.FastAddValue(SegmentKey, CreateString(_input.Substring(start, length)), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(IndexKey, CreateNumber(start), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(InputKey, CreateString(_input), JSPropertyAttributes.EnumerableConfigurableValue);
        if (_granularity == "word")
        {
            var isWordLike = HasWordStatus(_input.AsSpan(start, length));
            result.FastAddValue(IsWordLikeKey, isWordLike ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        }

        return result;
    }

    // The segments between consecutive boundaries, which are code unit indices from the text's start to its
    // end; an empty text has none. A granularity other than word or sentence is read as grapheme, as it was.
    private static List<(int start, int length)> Compute(string input, string granularity)
    {
        var boundaries = SegmenterHolder.Value.Boundaries(input, granularity switch
        {
            "word" => SegmentGranularity.Word,
            "sentence" => SegmentGranularity.Sentence,
            _ => SegmentGranularity.Grapheme,
        }, null);

        var list = new List<(int, int)>(Math.Max(0, boundaries.Length - 1));
        for (var i = 1; i < boundaries.Length; i++)
        {
            if (boundaries[i] > boundaries[i - 1])
                list.Add((boundaries[i - 1], boundaries[i] - boundaries[i - 1]));
        }

        return list;
    }

    // A word segment is word-like when its word status is not None: a letter, a number, kana or an ideograph
    // (ECMA-402 leaves the status to the implementation; correspondence row G4).
    private static bool HasWordStatus(ReadOnlySpan<char> segment)
        => SegmenterHolder.Value.GetWordStatus(segment, null) != WordStatus.None;

    // Built on first use: a realm that never segments does not read the break tables.
    private static class SegmenterHolder
    {
        internal static readonly UnicodeSegmenter Value = new(JSUnicode.Tables);
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

// Intl.DurationFormat (ECMA-402 sec-durationformat-objects, as tc39/proposal-temporal@e8cc03fc's intl.html amends it) over
// the shared library's DurationFormatCore (SJL-9.5, task 8.6, sub-area (e); Broiler.VM's JSD-0082, decision D24): the
// constructor reads localeMatcher, numberingSystem, style, each unit's style and display and fractionalDigits in the
// specification's order, each checked as it is read; ResolveLocale's nu key is the library's, over the nu values the data
// gives the matched locale (decision D13, row U16); GetDurationUnitOptions' defaults and checks (rows U1 to U3),
// [[DigitalFormat]] (rows U13 and U14) and PartitionDurationFormatPattern over the library's number and list formatters, with
// a fractional value summed exactly, are the library's, over the data home's number and list data (decision D5). The engine
// keeps the reads, the argument through ToTemporalDuration (rows U7 and U9, corrections: the fields are read once, in
// alphabetical order, and a String is parsed, never read back through Temporal.Duration.prototype's getters), the parts
// arrays, resolvedOptions' object and each RangeError, made from the library's status; it passes no charge (row U19).
// Deleted (2026-11-09): the Units, SingularUnits and style tables, the locale, numberingSystem, style, unitStyle, unitDisplay
// and fractionalDigits fields, GetDurationUnitOptions' body, FormatToParts (PartitionDurationFormatPattern, with its ":"
// separator for every locale), Format, DurationToFractional, DurationToFractionalString, FormatNumberParts (an inner
// Intl.NumberFormat through JSIntlNumberFormat.ComputeFormatParts), the inner Intl.ListFormat through
// JSIntlListFormat.FormatPartsForUnits, ToDurationFormatRecord and ValidateDurationArgument. Its public members stay
// (decision SJL-D-25, disposition (a)): the constructor from a locale and an options object, which now reads the locale as a
// requested one, and the prototype methods.
public sealed class JSIntlDurationFormat : JSObject
{
    // The style option's values, by DurationFormatStyle.
    private static readonly string[] StyleNames = ["long", "short", "narrow", "digital"];

    // A unit option's values, by DurationUnitStyle; "fractional" is no option's value.
    private static readonly string[] UnitStyleNames = ["long", "short", "narrow", "numeric", "2-digit", "fractional"];

    private static readonly string[] DisplayValues = ["auto", "always"];

    private static readonly KeyString StyleKey = KeyStrings.GetOrCreate("style");
    private static readonly KeyString FractionalDigitsKey = KeyStrings.GetOrCreate("fractionalDigits");

    /// <summary>The shared library's duration format: the resolved options, and PartitionDurationFormatPattern over them.</summary>
    internal DurationFormatCore Core { get; }

    // The constructor's reads after CanonicalizeLocaleList, GetOptionsObject and localeMatcher (ValidateConstructorArguments).
    internal JSIntlDurationFormat(JSValue canonicalLocales, JSObject options) : base(CurrentPrototype())
    {
        var numberingSystem = JSIntl.ReadNumberingSystemOption(options);
        var resolved = JSIntl.ResolveNumberingSystemLocale(canonicalLocales, numberingSystem);
        var dataLocale = JSIntlLocaleProviders.DataLocale(resolved.DataLocale);
        var data = DataLocalePluralData.For(dataLocale);

        // [[DigitalFormat]]'s two-digit hours, read at the data locale (row U14).
        var twoDigitHours = DurationFormatCore.DigitalFormat(data, dataLocale).TwoDigitHours;

        var style = (DurationFormatStyle)System.Array.IndexOf(StyleNames, JSIntl.GetOption(options, StyleKey, StyleNames, false, "short"));
        var units = new DurationUnitOptions[10];
        DurationUnitStyle? previous = null;
        for (var i = 0; i < units.Length; i++)
        {
            var unit = (DurationUnit)i;
            units[i] = GetDurationUnitOptions(options, unit, style, previous, twoDigitHours);
            if (unit is DurationUnit.Hours or DurationUnit.Minutes or DurationUnit.Seconds or DurationUnit.Milliseconds or DurationUnit.Microseconds)
                previous = units[i].Style;
        }

        var fractionalDigits = JSIntl.GetNumberOption(options, FractionalDigitsKey, 0, 9);
        Core = DurationFormatCore.Create(data, resolved.Locale, dataLocale, resolved.KeyValues["nu"] ?? "latn", style, units, fractionalDigits);
    }

    // The format of a locale and an options object, the locale read as the one requested locale (an empty or null one as
    // none), with no localeMatcher read.
    // Revised 2026-11-09, at SJL-9.5 (task 8.6): it read the locale as the resolved one, negotiating only its -u-nu- keyword
    // by hand (ResolveNumberingSystem); the locale is now matched against the available locales by the shared negotiation.
    // Noted 2026-11-09, at SJL-9.5 (review of task 8.6): the locale now goes through CanonicalizeLocaleList, so an ill-formed
    // tag throws a JavaScript RangeError, where the former constructor took the string as resolved and threw none.
    public JSIntlDurationFormat(string locale, JSObject options)
        : this(string.IsNullOrEmpty(locale) ? JSValue.CreateArray() : JSIntl.CanonicalizeLocaleList(CreateString(locale)), options)
    {
    }

    // ECMA-402's GetDurationUnitOptions (sec-getdurationunitoptions): the unit's two options read here, in order, each checked
    // as it is read; the defaults and ValidateDurationUnitStyle are the shared library's, whose refusals are RangeErrors.
    // Noted 2026-11-09, at SJL-9.5 (review of task 8.6, row U4, a correction): the unit's display option is read before
    // ValidateDurationUnitStyle refuses a word style after a numeric or 2-digit unit, as the specification's steps order
    // them; the former engine threw that RangeError before it read the display option, so a getter on, say,
    // minutesDisplay with { hours: "numeric", minutes: "long" } did not run.
    private static DurationUnitOptions GetDurationUnitOptions(JSObject options, DurationUnit unit, DurationFormatStyle baseStyle, DurationUnitStyle? previous, bool twoDigitHours)
    {
        var name = DurationFormatCore.UnitName(unit);
        var admitted = new List<string>();
        foreach (var admittedStyle in DurationFormatCore.UnitStyles(unit))
            admitted.Add(UnitStyleNames[(int)admittedStyle]);

        var style = JSIntl.GetOption(options, KeyStrings.GetOrCreate(name), admitted.ToArray(), false, null);
        var display = JSIntl.GetOption(options, KeyStrings.GetOrCreate(name + "Display"), DisplayValues, false, null);

        var status = DurationFormatCore.GetDurationUnitOptions(
            unit,
            style is null ? null : (DurationUnitStyle)System.Array.IndexOf(UnitStyleNames, style),
            display is null ? null : display == "always" ? DurationUnitDisplay.Always : DurationUnitDisplay.Auto,
            baseStyle,
            previous,
            twoDigitHours,
            out var resolved);

        return status switch
        {
            IntlStatus.Completed => resolved,
            IntlStatus.FractionalDurationUnitAlwaysDisplayed => throw JSEngine.NewRangeError($"Intl.DurationFormat: {name} cannot always be displayed as a fraction"),
            IntlStatus.DurationUnitAfterFractional => throw JSEngine.NewRangeError($"Intl.DurationFormat: {name} must be fractional after a fractional unit"),
            _ => throw JSEngine.NewRangeError($"Intl.DurationFormat: {name} must be numeric or 2-digit after a numeric or 2-digit unit"),
        };
    }

    // The duration format and formatToParts read: ToTemporalDuration (the proposal's amendment of ECMA-402 s15.10.1), which
    // takes a Temporal.Duration's slots, parses a String, and reads a property bag's fields once, in alphabetical order.
    private static global::Broiler.ECMAScript.Temporal.DurationFields Input(JSValue duration)
        => ((JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(duration)).Fields;

    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.format called on incompatible receiver");

        return CreateString(self.Core.Format(Input(a.Get1())));
    }

    // Temporal.Duration.prototype.toLocaleString(locales, options): formats the duration with a
    // freshly-constructed Intl.DurationFormat, exactly as `new Intl.DurationFormat(locales,
    // options).format(duration)` would.
    internal static JSValue TemporalToLocaleString(JSValue duration, JSValue locales, JSValue options)
    {
        var args = new Arguments(JSUndefined.Value, locales, options ?? JSUndefined.Value);
        var optionsObject = JSIntl.ValidateConstructorArguments(
            "DurationFormat", in args, out var canonical, requireNew: false, coerceOptions: false);
        var df = new JSIntlDurationFormat(canonical, optionsObject);
        return CreateString(df.Core.Format(Input(duration)));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.formatToParts called on incompatible receiver");

        var duration = Input(a.Get1());
        var parts = CreateArray();

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var unitKey = KeyStrings.GetOrCreate("unit");
        foreach (var (type, value, unit) in self.Core.FormatToParts(duration))
        {
            var part = new JSObject();
            part.FastAddValue(typeKey, CreateString(type), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(valueKey, CreateString(value), JSPropertyAttributes.EnumerableConfigurableValue);
            // Parts produced from a unit's NumberFormat carry the singular unit
            // name; list separators and ListFormat literals leave it absent.
            if (unit != null)
                part.FastAddValue(unitKey, CreateString(unit), JSPropertyAttributes.EnumerableConfigurableValue);
            parts.AddArrayItem(part);
        }
        return parts;
    }

    // resolvedOptions, Table 21 in its order, as the shared core resolved it: a fractional unit written as numeric (row U2).
    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.resolvedOptions called on incompatible receiver");

        var format = self.Core;
        var result = new JSObject();
        void Put(string key, JSValue value) =>
            result.FastAddValue(KeyStrings.GetOrCreate(key), value, JSPropertyAttributes.EnumerableConfigurableValue);

        Put("locale", CreateString(format.Locale));
        Put("numberingSystem", CreateString(format.NumberingSystem));
        Put("style", CreateString(StyleNames[(int)format.Style]));
        for (var i = 0; i < format.Units.Count; i++)
        {
            var (style, display) = format.Units[i];
            var name = DurationFormatCore.UnitName((DurationUnit)i);
            Put(name, CreateString(style == DurationUnitStyle.Fractional ? "numeric" : UnitStyleNames[(int)style]));
            Put(name + "Display", CreateString(display == DurationUnitDisplay.Always ? "always" : "auto"));
        }
        if (format.FractionalDigits is { } digits)
            Put("fractionalDigits", CreateNumber(digits));
        return result;
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.DurationFormat");

    // %Intl.DurationFormat.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.DurationFormat` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);
}

// Intl.ListFormat (ECMA-402 sec-listformat-objects) over the shared library's ListFormatter (SJL-9.5, task 8.4, sub-area
// (c); Broiler.VM's JSD-0082, decision D24): CreatePartsFromList and its templates, CLDR's list patterns for the type and
// style at the data locale, are the library's (decision D5). The engine keeps StringListFromIterable, the parts arrays and
// the (element, literal) parts Intl.DurationFormat reads until task 8.6. Deleted (2026-11-08): BuildParts, ExpandPattern,
// FormatList, ApplyPattern, GetPatterns and the type and style fields. Its public members stay (decision SJL-D-25,
// disposition (a)); the constructor's type and style are the option values, and any other is an
// ArgumentOutOfRangeException.
// Revised 2026-11-08, at SJL-9.5 (task 8.4, a review fix): the constructor read "the default for any other", mapping an
// unknown type or style to conjunction or long while resolvedOptions reported the default; the former engine stored the
// strings and reported them verbatim. A direct .NET caller now learns of the unknown value (for task 8.7's
// docs/public-api.md and CHANGELOG).
// Revised 2026-11-09, at SJL-9.5 (task 8.6): Intl.DurationFormat joins its units through the shared library's
// DurationFormatCore, so FormatPartsForUnits, the (element, literal) parts it read, is deleted; the engine keeps
// StringListFromIterable and the parts arrays. The sentence above read "The engine keeps StringListFromIterable, the
// parts arrays and the (element, literal) parts Intl.DurationFormat reads until task 8.6."
public sealed class JSIntlListFormat : JSObject
{
    private static readonly string[] ListTypeNames = ["conjunction", "disjunction", "unit"];

    internal ListFormatter Formatter { get; }

    public JSIntlListFormat(string locale = "en-US", string type = "conjunction", string style = "long")
    {
        var typeIndex = System.Array.IndexOf(ListTypeNames, type);
        if (typeIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(type), type, "The list type is conjunction, disjunction or unit.");

        var styleIndex = System.Array.IndexOf(JSIntl.FormatStyleNames, style);
        if (styleIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(style), style, "The list style is long, short or narrow.");

        Formatter = ListFormatter.Create(JSIntlData.LocaleData, locale, JSIntlLocaleProviders.DataLocale(locale),
            (ListFormatType)typeIndex, (FormatStyle)styleIndex);
    }

    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat @this)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.format called on incompatible receiver");

        var items = StringListFromIterable(a.Get1());
        return CreateString(@this.Formatter.Format(items, null));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat @this)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.formatToParts called on incompatible receiver");

        var items = StringListFromIterable(a.Get1());
        var parts = @this.Formatter.FormatToParts(items, null);
        var result = CreateArray();
        var arr = (JSObject)result;
        for (uint i = 0; i < (uint)parts.Count; i++)
            arr.SetPropertyOrThrow(CreateNumber(i), MakePart(parts[(int)i].Type, parts[(int)i].Value));
        return result;
    }

    private static JSObject MakePart(string type, string value)
    {
        var part = new JSObject();
        part[KeyStrings.GetOrCreate("type")] = CreateString(type);
        part[KeyStrings.GetOrCreate("value")] = CreateString(value);
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

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlListFormat @this)
            throw JSEngine.NewTypeError("Intl.ListFormat.prototype.resolvedOptions called on incompatible receiver");

        // Table 25, in its order.
        var formatter = @this.Formatter;
        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), CreateString(formatter.Locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), CreateString(ListTypeNames[(int)formatter.Type]));
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), CreateString(JSIntl.FormatStyleNames[(int)formatter.Style]));
        return result;
    }
}

internal sealed record JSIntlDisplayNamesOptions(string Style, string Type, string Fallback, string LanguageDisplay);

// Intl.DisplayNames (ECMA-402 sec-intl-displaynames-objects) over the shared library's DisplayNamesCore (SJL-9.5, task 8.4,
// sub-area (c); Broiler.VM's JSD-0082, decision D24): CanonicalCodeForDisplayNames over ASCII (row S9, a correction) and the
// names [[Fields]] holds, CLDR's at the data locale, are the library's (row S8, data, an X1 adoption; decision D5); a code
// with no name answers itself or undefined by the fallback option. Deleted (2026-11-08): ValidateCode and the locale and
// options fields. Its public members stay (decision SJL-D-25, disposition (a)).
public sealed class JSIntlDisplayNames : JSObject
{
    private static readonly string[] DisplayNamesTypeNames = ["language", "region", "script", "currency", "calendar", "dateTimeField"];

    internal DisplayNamesCore Core { get; }

    public JSIntlDisplayNames(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var options = JSIntl.ValidateDisplayNamesOptions(JSIntl.ValidateConstructorArguments("DisplayNames", in a, out var canonical, coerceOptions: false));
        var resolved = JSIntl.ResolveLocale(canonical, JSIntl.NoRelevantKey);
        Core = DisplayNamesCore.Create(JSIntlData.LocaleData, resolved.Locale, JSIntlLocaleProviders.DataLocale(resolved.DataLocale),
            (FormatStyle)System.Array.IndexOf(JSIntl.FormatStyleNames, options.Style),
            (DisplayNamesType)System.Array.IndexOf(DisplayNamesTypeNames, options.Type),
            options.Fallback == "none" ? DisplayNamesFallback.None : DisplayNamesFallback.Code,
            options.LanguageDisplay == "standard" ? LanguageDisplay.Standard : LanguageDisplay.Dialect);
    }

    public static JSValue OfPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDisplayNames @this)
            throw JSEngine.NewTypeError("Intl.DisplayNames.prototype.of called on incompatible receiver");

        var code = a.Get1().StringValue;
        if (@this.Core.Of(code, out var name, null) != IntlStatus.Completed)
            throw JSEngine.NewRangeError($"Invalid code for Intl.DisplayNames type {DisplayNamesTypeNames[(int)@this.Core.Type]}");

        return name is null ? JSUndefined.Value : CreateString(name);
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDisplayNames @this)
            throw JSEngine.NewTypeError("Intl.DisplayNames.prototype.resolvedOptions called on incompatible receiver");

        // Table 18, in its order.
        var core = @this.Core;
        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), CreateString(core.Locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), CreateString(JSIntl.FormatStyleNames[(int)core.Style]));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), CreateString(DisplayNamesTypeNames[(int)core.Type]));
        result.CreateDataProperty(KeyStrings.GetOrCreate("fallback"), CreateString(core.Fallback == DisplayNamesFallback.Code ? "code" : "none"));
        if (core.LanguageDisplay is { } display)
            result.CreateDataProperty(KeyStrings.GetOrCreate("languageDisplay"), CreateString(display == LanguageDisplay.Dialect ? "dialect" : "standard"));
        return result;
    }
}

// Intl.Locale (ECMA-402 sec-intl-locale-objects) over the shared library (milestone SJL-9.5, task 8.3, sub-area (a);
// Broiler.VM's JSD-0082): the record is LocaleRecord's, made by the constructor's steps (JSIntl.ValidateLocaleConstructorArguments);
// the parts are LocaleId's; Add and Remove Likely Subtags are LocaleId's over the data home's likely-subtags table (rows L11 and
// L12); the information methods are LocaleInfo's over the data home's time, week and script data (rows L15 to L27), with this
// engine's Intl.Collator's collations (decision D33 (c)) and the shared time zone database. Its public members stay, results
// corrected where the rows say (decision SJL-D-25, disposition (a)).
public sealed class JSIntlLocale : JSObject
{
    private readonly LocaleRecord record;

    // A Locale of a tag given to the public constructor: its canonical record, or the tag as given with no keyword when it is
    // not a well-formed tag (a caller outside the engine may pass one).
    public JSIntlLocale(string tag = "und") : this(RecordOf(tag ?? "und"))
    {
    }

    internal JSIntlLocale(LocaleRecord record) : base(CurrentPrototype()) => this.record = record;

    // The [[Locale]] internal slot: the canonical language tag. Used by
    // CanonicalizeLocaleList to read a Locale argument without calling toString.
    internal string Tag => record.Locale;

    // The record's [[Locale]] parsed, for its parts; null only for a tag the public constructor was given that is not well-formed.
    private LocaleId Id => LocaleId.TryParse(record.Locale, out var id) == IntlStatus.Completed ? id : null;

    private static LocaleRecord RecordOf(string tag)
        => LocaleRecord.TryCreate(tag, LocaleOptions.None, JSIntlData.LocaleData, out var record) == IntlStatus.Completed && record != null
            ? record
            : new LocaleRecord(tag, null, null, null, null, null, false, null);

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.Locale");

    // %Intl.Locale.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.Locale` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);

    private static JSIntlLocale RequireLocale(in Arguments a, string method)
    {
        if (a.This is not JSIntlLocale locale)
            throw JSEngine.NewTypeError($"Intl.Locale.prototype.{method} called on incompatible receiver");

        return locale;
    }

    private static JSValue StringArray(IReadOnlyList<string> values)
    {
        var array = CreateArray();
        foreach (var value in values)
            array.AddArrayItem(CreateString(value));
        return array;
    }

    // UTS #35's Add Likely Subtags (row L11; row L12, a correction: Zzzz and ZZ are removed before the lookup). When the
    // algorithm signals an error the Locale is unchanged.
    public static JSValue MaximizePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "maximize");
        return locale.Id is { } id
            ? new JSIntlLocale(RecordOf(id.Maximize(JSIntlData.LocaleData, out _).ToString()))
            : new JSIntlLocale(locale.record);
    }

    // UTS #35's Remove Likely Subtags (row L11).
    public static JSValue MinimizePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "minimize");
        return locale.Id is { } id
            ? new JSIntlLocale(RecordOf(id.Minimize(JSIntlData.LocaleData, out _).ToString()))
            : new JSIntlLocale(locale.record);
    }

    // CalendarsOfLocale: the ca keyword, else gregory (rows L15 and L16).
    public static JSValue GetCalendarsPrototype(in Arguments a)
        => StringArray(LocaleInfo.Calendars(RequireLocale(in a, "getCalendars").record));

    // CollationsOfLocale: the co keyword alone (row L15); else the collations this engine's Intl.Collator resolves for the
    // locale its available locales match, or for DefaultLocale, sorted (rows L17 and L18; decision D33 (c)).
    public static JSValue GetCollationsPrototype(in Arguments a)
        => StringArray(LocaleInfo.Collations(RequireLocale(in a, "getCollations").record, JSIntlCollator.LocaleData, JSIntlLocaleProviders.DefaultLocale));

    // HourCyclesOfLocale: the hc keyword alone (row L15); else CLDR's time data for the preferred region, the preferred
    // cycle and then the allowed ones (rows L19 to L21).
    public static JSValue GetHourCyclesPrototype(in Arguments a)
        => StringArray(LocaleInfo.HourCycles(RequireLocale(in a, "getHourCycles").record, JSIntlData.LocaleData));

    // NumberingSystemsOfLocale: the nu keyword alone (row L15); else the default numbering system of the Intl.NumberFormat
    // locale the available locales match, from the shared data's defaultNumberingSystem along its chain, else latn: the
    // shared library's LocaleInfo (decision D13; row L22). Revised 2026-11-08, at SJL-9.5 (task 8.4): the comment read
    // "else the default numbering system NumberFormat uses, which JSIntlDefaultNumberingSystems gives until SJL-9.5's task
    // 8.4 moves it, and this method with it, onto the shared data", and the method returned
    // "locale.record.NumberingSystem ?? JSIntlDefaultNumberingSystems.Of(locale.record.Locale)".
    public static JSValue GetNumberingSystemsPrototype(in Arguments a)
        => StringArray(LocaleInfo.NumberingSystems(RequireLocale(in a, "getNumberingSystems").record,
            JSIntlLocaleProviders.AvailableLocales, JSIntlData.LocaleData));

    // TextDirectionOfLocale: the direction of the script or the likely script in CLDR's script metadata (row L24), and
    // undefined when none is known (row L25, a correction).
    public static JSValue GetTextInfoPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getTextInfo");
        var direction = LocaleInfo.TextDirection(locale.record, JSIntlData.LocaleData);
        var info = new JSObject();
        info.FastAddValue(
            KeyStrings.GetOrCreate("direction"),
            direction == null ? UndefinedValue : CreateString(direction),
            JSPropertyAttributes.EnumerableConfigurableValue);
        return info;
    }

    // TimeZonesOfLocale: undefined without a region; else the region's zone.tab zones in code-unit order from the shared
    // database (milestone SJL-8.6; row L23).
    public static JSValue GetTimeZonesPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getTimeZones");
        return LocaleInfo.TimeZones(locale.record, TemporalTimeZones.Database) is { } zones
            ? StringArray(zones)
            : UndefinedValue;
    }

    // WeekInfoOfLocale: CLDR's first day and weekend for the preferred region, the fw keyword over the first day (rows L21
    // and L26). The object's keys are firstDay and weekend, in that order.
    public static JSValue GetWeekInfoPrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "getWeekInfo");
        var week = LocaleInfo.WeekInfo(locale.record, JSIntlData.LocaleData);

        var weekend = CreateArray();
        foreach (var day in week.Weekend)
            weekend.AddArrayItem(CreateNumber(day));

        var info = new JSObject();
        info.FastAddValue(KeyStrings.GetOrCreate("firstDay"),
            CreateNumber(week.FirstDay), JSPropertyAttributes.EnumerableConfigurableValue);
        info.FastAddValue(KeyStrings.GetOrCreate("weekend"),
            weekend, JSPropertyAttributes.EnumerableConfigurableValue);
        return info;
    }

    public static JSValue ToStringPrototype(in Arguments a)
        => CreateString(RequireLocale(in a, "toString").Tag);

    public static JSValue BaseNamePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "baseName");
        return CreateString(locale.Id?.BaseName ?? locale.Tag);
    }

    public static JSValue LanguagePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "language");
        return CreateString(locale.Id?.Language ?? locale.Tag);
    }

    public static JSValue ScriptPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "script").Id?.Script);

    public static JSValue RegionPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "region").Id?.Region);

    public static JSValue VariantsPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "variants").Id?.Variants is { Count: > 0 } variants ? string.Join("-", variants) : null);

    public static JSValue CalendarPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "calendar").record.Calendar);

    public static JSValue CaseFirstPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "caseFirst").record.CaseFirst);

    public static JSValue CollationPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "collation").record.Collation);

    public static JSValue HourCyclePrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "hourCycle").record.HourCycle);

    public static JSValue NumberingSystemPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "numberingSystem").record.NumberingSystem);

    public static JSValue FirstDayOfWeekPrototype(in Arguments a)
        => OptionalString(RequireLocale(in a, "firstDayOfWeek").record.FirstDayOfWeek);

    public static JSValue NumericPrototype(in Arguments a)
        => RequireLocale(in a, "numeric").record.Numeric ? BooleanTrue : BooleanFalse;

    private static JSValue OptionalString(string value)
        => value == null ? UndefinedValue : CreateString(value);
}

// Intl.PluralRules (ECMA-402 sec-pluralrules-objects) over the shared library's PluralRulesRecord (SJL-9.5, task 8.4,
// sub-area (b); Broiler.VM's JSD-0082, decision D24). The constructor reads localeMatcher, type, notation, compactDisplay
// and SetNumberFormatDigitOptions' options in the specification's order, each checked as it is read (rows P7 and M4,
// corrections); ResolvePlural selects over FormatNumericToString's string (row P1, a correction), with the compact exponent
// (decision D20), and ResolvePluralRange over CLDR's plural ranges (rows P5a and P5b); select and selectRange take
// ToIntlMathematicalValue's value, a BigInt's included (row P2, a correction); resolvedOptions reports Table 32 in its order
// (row P6, a correction). The categories and rules are the data home's, for every CLDR language (decision D5, row P3),
// read along the data locale's parent chain (DataLocalePluralData), so pt-PT selects by pt-PT's rules.
// The public members stay (decision SJL-D-25, disposition (a)); SelectCategory selects over its Number's value.
public sealed class JSIntlPluralRules : JSObject
{
    internal PluralRulesRecord Rules { get; }

    public JSIntlPluralRules(in Arguments a) : base(CurrentPrototype())
    {
        var options = JSIntl.ValidateConstructorArguments("PluralRules", in a, out var canonical);
        var resolved = JSIntl.ResolveLocale(canonical, JSIntl.NoRelevantKey);
        var type = JSIntl.GetOption(options, KeyStrings.GetOrCreate("type"), ["cardinal", "ordinal"], false, "cardinal");
        var notation = (NumberNotation)System.Array.IndexOf(JSIntl.NotationNames,
            JSIntl.GetOption(options, KeyStrings.GetOrCreate("notation"), JSIntl.NotationNames, false, "standard"));
        var compactDisplay = (CompactDisplay)System.Array.IndexOf(JSIntl.CompactDisplayNames,
            JSIntl.GetOption(options, KeyStrings.GetOrCreate("compactDisplay"), JSIntl.CompactDisplayNames, false, "short"));
        var digitOptions = JSIntl.SetNumberFormatDigitOptions(options, new FractionDigitDefaults(0, 3), notation);
        var dataLocale = JSIntlLocaleProviders.DataLocale(resolved.DataLocale);
        Rules = PluralRulesRecord.Create(DataLocalePluralData.For(dataLocale), resolved.Locale, dataLocale,
            type == "ordinal" ? PluralRuleType.Ordinal : PluralRuleType.Cardinal, notation, compactDisplay, digitOptions);
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.PluralRules");

    // %Intl.PluralRules.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.PluralRules` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);

    // ResolvePlural of a Number: the plural category of FormatNumericToString's string under the resolved digit options.
    public string SelectCategory(double n)
        => Rules.ResolvePlural(IntlDecimal.FromNumber(n), null).PluralCategory;

    internal static JSIntlPluralRules Of(JSValue thisValue, string member)
        => thisValue as JSIntlPluralRules
            ?? throw JSEngine.NewTypeError($"Intl.PluralRules.prototype.{member} called on incompatible receiver");

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        var rules = Of(a.This, "resolvedOptions").Rules;
        var digits = rules.DigitOptions;
        var result = new JSObject();

        void PutNumber(string name, int? value)
        {
            if (value is not null)
                result.CreateDataProperty(KeyStrings.GetOrCreate(name), CreateNumber(value.Value));
        }

        // Table 32, in its order.
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), CreateString(rules.Locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), CreateString(rules.Type == PluralRuleType.Ordinal ? "ordinal" : "cardinal"));
        result.CreateDataProperty(KeyStrings.GetOrCreate("notation"), CreateString(JSIntl.NotationNames[(int)rules.Notation]));
        if (rules.Notation == NumberNotation.Compact)
            result.CreateDataProperty(KeyStrings.GetOrCreate("compactDisplay"), CreateString(JSIntl.CompactDisplayNames[(int)rules.CompactDisplay]));
        PutNumber("minimumIntegerDigits", digits.MinimumIntegerDigits);
        PutNumber("minimumFractionDigits", digits.MinimumFractionDigits);
        PutNumber("maximumFractionDigits", digits.MaximumFractionDigits);
        PutNumber("minimumSignificantDigits", digits.MinimumSignificantDigits);
        PutNumber("maximumSignificantDigits", digits.MaximumSignificantDigits);

        var categories = CreateArray();
        foreach (var category in rules.PluralCategories)
            categories.AddArrayItem(CreateString(category));
        result.CreateDataProperty(KeyStrings.GetOrCreate("pluralCategories"), categories);
        JSIntlNumberFormat.PutDigitTail(result, digits);
        return result;
    }
}

// Intl.NumberFormat (ECMA-402 sec-numberformat-objects) over the shared library's NumberFormatter (SJL-9.5, task 8.4,
// sub-area (c); Broiler.VM's JSD-0082, decision D24): the constructor reads every option once, in the specification's order
// (JSIntl.CreateNumberFormatter), and the record does the rest - the digit options and FormatNumericToString (sub-area (b)),
// compact, scientific and engineering notation in every style with ComputeExponent over the exact value, currency, unit
// and percent patterns, currency spacing and names, sign display, the numbering system's digits and symbols, ranges with
// their collapse, and the parts - over the data home's CLDR 48.2.0 tables (decisions D5, D13 and D21), with the host's
// number symbols for a locale the shared data lacks (decision D6). The engine keeps ToIntlMathematicalValue's ToPrimitive,
// a range's undefined ends, the bound format and its caching (row N11, a correction), and the parts arrays.
// Deleted with the engine's own formatter (2026-11-08): ExactRational, ExactMagnitude, TryParseExactDecimal,
// FormatFiniteMagnitude, FormatNumericToString, RoundFractionDigits, RoundSignificantDigits, RoundWithPriority, RoundFixed,
// RawPrecisionDigits, ToRawPrecision, ComputeExponent, CompareToPowerOfTen, FormatScientificEngineering, FormatMantissa,
// FormatCompact, FormatCompactMantissa, CompactMagnitude, GetCompactUnits, LocaleHasRegion and the CompactUnits* tables,
// BuildDigitParts, AppendIntegerParts, SplitIntoGroups, AssemblePlainParts, AssembleCurrencyParts, AssemblePercentParts,
// AssembleUnitParts, AppendUnitText, MapNumberingSystemDigits, MapDigit with ContiguousDigitZero and AlgorithmicDigits,
// ComputeRangeParts, CoerceRangeOperand, CoerceRangeValue, ResolveFractionDigits, DefaultFractionDigits, ReadIntOption,
// HasOption, Culture, GroupSeparator, DecimalSeparator, NanSymbol, StyleOption, CurrencySignOption, ResolveCurrency,
// CanonicalCurrencyValue, ReadStringOption and UnsignedRounding. Its public members stay (decision SJL-D-25, disposition (a)).
public class JSIntlNumberFormat : JSObject
{
    internal NumberFormatter Formatter { get; private set; }

    // [[BoundFormat]]: made the first time the format getter reads it (row N11).
    internal JSFunction BoundFormat { get; set; }

    public JSIntlNumberFormat(in Arguments a) : this()
    {
        var options = JSIntl.ValidateConstructorArguments("NumberFormat", in a, out var canonical, requireNew: false);
        Formatter = JSIntl.CreateNumberFormatter(canonical, options);
    }

    private JSIntlNumberFormat() : base(CurrentPrototype()) { }

    public JSValue Format(in Arguments a)
        => CreateString(Formatter.Format(JSIntl.ToIntlMathematicalValue(a.Get1()), null));

    internal static JSIntlNumberFormat Of(JSValue thisValue, string member)
        => thisValue as JSIntlNumberFormat
            ?? throw JSEngine.NewTypeError($"Intl.NumberFormat.prototype.{member} called on incompatible receiver");

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        var @this = Of(a.This, "formatToParts");
        return PartsArray(@this.Formatter.FormatToParts(JSIntl.ToIntlMathematicalValue(a.Get1()), null), withSource: false);
    }

    public static JSValue FormatRangePrototype(in Arguments a)
    {
        var @this = Of(a.This, "formatRange");
        var (x, y) = RangeValues(in a);
        RangeCompleted(@this.Formatter.FormatRange(x, y, out var text, null));
        return CreateString(text);
    }

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        var @this = Of(a.This, "formatRangeToParts");
        var (x, y) = RangeValues(in a);
        RangeCompleted(@this.Formatter.FormatRangeToParts(x, y, out var parts, null));
        return PartsArray(parts, withSource: true);
    }

    // The two values of a range: both present, each converted, the start first.
    private static (IntlDecimal X, IntlDecimal Y) RangeValues(in Arguments a)
    {
        var start = a.Get1();
        var end = a.GetAt(1);
        if (start == null || start.IsUndefined || end == null || end.IsUndefined)
            throw JSEngine.NewTypeError("Invalid number range");

        var x = JSIntl.ToIntlMathematicalValue(start);
        var y = JSIntl.ToIntlMathematicalValue(end);
        return (x, y);
    }

    // PartitionNumberRangePattern's refusal, step 1 (an end is not-a-number), as the RangeError it is.
    private static void RangeCompleted(IntlStatus status)
    {
        if (status != IntlStatus.Completed)
            throw JSEngine.NewRangeError("Invalid number range");
    }

    // An array of { type, value } objects, and source for a range's parts.
    private static JSValue PartsArray(IReadOnlyList<NumberPart> parts, bool withSource)
    {
        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var sourceKey = KeyStrings.GetOrCreate("source");
        var array = CreateArray();
        foreach (var part in parts)
        {
            var item = new JSObject();
            item.FastAddValue(typeKey, CreateString(part.Type), JSPropertyAttributes.EnumerableConfigurableValue);
            item.FastAddValue(valueKey, CreateString(part.Value), JSPropertyAttributes.EnumerableConfigurableValue);
            if (withSource && part.Source is not null)
                item.FastAddValue(sourceKey, CreateString(part.Source), JSPropertyAttributes.EnumerableConfigurableValue);
            array.AddArrayItem(item);
        }

        return array;
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        var formatter = Of(a.This, "resolvedOptions").Formatter;
        var digits = formatter.DigitOptions;
        var result = new JSObject();

        void Put(string name, string value)
        {
            if (value is not null)
                result.CreateDataProperty(KeyStrings.GetOrCreate(name), CreateString(value));
        }

        void PutNumber(string name, int? value)
        {
            if (value is not null)
                result.CreateDataProperty(KeyStrings.GetOrCreate(name), CreateNumber(value.Value));
        }

        // Table 15, in its order: each slot as the constructor resolved it, read back from the library's record.
        Put("locale", formatter.Locale);
        Put("numberingSystem", formatter.NumberingSystem);
        Put("style", JSIntl.NumberStyleNames[(int)formatter.Style]);
        Put("currency", formatter.Currency);
        Put("currencyDisplay", formatter.CurrencyDisplay is { } currencyDisplay ? JSIntl.CurrencyDisplayNames[(int)currencyDisplay] : null);
        Put("currencySign", formatter.CurrencySign is { } currencySign ? JSIntl.CurrencySignNames[(int)currencySign] : null);
        Put("unit", formatter.Unit);
        Put("unitDisplay", formatter.UnitDisplay is { } unitDisplay ? JSIntl.UnitDisplayNames[(int)unitDisplay] : null);
        PutNumber("minimumIntegerDigits", digits.MinimumIntegerDigits);
        PutNumber("minimumFractionDigits", digits.MinimumFractionDigits);
        PutNumber("maximumFractionDigits", digits.MaximumFractionDigits);
        PutNumber("minimumSignificantDigits", digits.MinimumSignificantDigits);
        PutNumber("maximumSignificantDigits", digits.MaximumSignificantDigits);
        result.CreateDataProperty(KeyStrings.GetOrCreate("useGrouping"),
            JSIntl.GroupingNames[(int)formatter.UseGrouping] is { } grouping ? CreateString(grouping) : BooleanFalse);
        Put("notation", JSIntl.NotationNames[(int)formatter.Notation]);
        Put("compactDisplay", formatter.CompactDisplay is { } compactDisplay ? JSIntl.CompactDisplayNames[(int)compactDisplay] : null);
        Put("signDisplay", JSIntl.SignDisplayNames[(int)formatter.SignDisplay]);
        PutDigitTail(result, digits);
        return result;
    }

    // The last four slots resolvedOptions reports of a number format or plural rules, in Table 15's and Table 32's order:
    // roundingIncrement, roundingMode, roundingPriority (the computed one) and trailingZeroDisplay.
    internal static void PutDigitTail(JSObject result, DigitOptions digits)
    {
        result.CreateDataProperty(KeyStrings.GetOrCreate("roundingIncrement"), CreateNumber(digits.RoundingIncrement));
        result.CreateDataProperty(KeyStrings.GetOrCreate("roundingMode"), CreateString(JSIntl.RoundingModeNames[(int)digits.RoundingMode]));
        result.CreateDataProperty(KeyStrings.GetOrCreate("roundingPriority"), CreateString(JSIntl.RoundingPriorityNames[(int)digits.ComputedRoundingPriority]));
        result.CreateDataProperty(KeyStrings.GetOrCreate("trailingZeroDisplay"), CreateString(JSIntl.TrailingZeroDisplayNames[(int)digits.TrailingZeroDisplay]));
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.NumberFormat");

    // %Intl.NumberFormat.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.NumberFormat` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);
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
        var options = JSIntl.ValidateConstructorArguments("Collator", in a, out var canonical, requireNew: false);
        // Keep only the collation-relevant Unicode keywords (co/kf/kn) in the resolved locale, in
        // canonical form (so e.g. "-kn-true" reduces to "-kn").
        locale = JSIntl.ResolveLocaleFromCanonical(canonical, JSIntl.CollatorRelevantKeys);

        if (TryGetUnicodeExtension(locale, "kn", out var kn))
            numeric = kn == "true";
        if (TryGetUnicodeExtension(locale, "kf", out var kf) && (kf == "upper" || kf == "lower" || kf == "false"))
            caseFirst = kf;
        // The locale's -u-co- value (canonicalized; null when absent/unrecognized) is a
        // candidate collation, resolved against the locale's available collations below.
        var extensionCollation = TryGetUnicodeExtension(locale, "co", out var co) && JSIntl.CanonicalizeCollation(co) is { } tagCo
            ? tagCo
            : null;

        // usage / sensitivity / caseFirst are constrained string options: a provided value outside
        // the allowed set is a RangeError (ECMA-402 GetOption), so route them through the shared
        // validating helper rather than reading the raw string. The current field value is passed as
        // the default so an absent option keeps the locale-derived / spec default.
        usage = JSIntl.GetOption(options, KeyStrings.GetOrCreate("usage"), ["sort", "search"], false, usage);
        sensitivity = JSIntl.GetOption(options, KeyStrings.GetOrCreate("sensitivity"), ["base", "accent", "case", "variant"], false, sensitivity);
        if (TryGetOwnOption(options, "ignorePunctuation", out var ignorePunctuationValue))
            ignorePunctuation = ignorePunctuationValue.BooleanValue;
        else
            ignorePunctuation = LocaleDefaultIgnorePunctuation(locale);
        var numericPresent = TryGetOwnOption(options, "numeric", out var numericValue);
        if (numericPresent)
            numeric = numericValue.BooleanValue;
        var caseFirstKey = KeyStrings.GetOrCreate("caseFirst");
        var caseFirstPresent = options != null && options[caseFirstKey] is { IsUndefined: false };
        caseFirst = JSIntl.GetOption(options, caseFirstKey, ["upper", "lower", "false"], false, caseFirst);
        var optionCollation = TryGetOwnOption(options, "collation", out var collationValue)
            && JSIntl.CanonicalizeCollation(collationValue.StringValue) is { } optCo
            ? optCo
            : null;

        // ResolveLocale: keep the -u-kn / -u-kf keyword in the resolved locale only when the option
        // matches the extension value (an absent option keeps it; a differing option drops it). The
        // boolean "kn" keyword's "true" is elided to a value-less form in the canonical tag.
        if (numericPresent)
        {
            var knExt = JSIntl.GetUnicodeExtensionType(locale, "kn");
            var knExtValue = knExt == null ? null : (knExt.Length == 0 ? "true" : knExt);
            locale = JSIntl.ReflectExtensionKeyword(locale, "kn", numeric ? "true" : "false", knExtValue);
        }
        if (caseFirstPresent)
            locale = JSIntl.ReflectExtensionKeyword(locale, "kf", caseFirst, JSIntl.GetUnicodeExtensionType(locale, "kf"));

        // ResolveLocale over the "co" key (UTS #35): the locale's -u-co- value is used when it is
        // available for the locale; an available collation option overrides it, and when the option
        // value differs from the extension value the -u-co- is dropped from the resolved locale.
        // The resolved -u-co- is reflected only when the chosen value came from the locale extension.
        var bareLocale = StripUnicodeExtension(locale);
        var fromExtension = false;
        if (extensionCollation != null && IsCollationAvailableForLocale(bareLocale, extensionCollation))
        {
            collation = extensionCollation;
            fromExtension = true;
        }
        if (optionCollation != null && optionCollation != collation
            && IsCollationAvailableForLocale(bareLocale, optionCollation))
        {
            collation = optionCollation;
            fromExtension = false;
        }
        if (!fromExtension)
            locale = JSIntl.RemoveUnicodeExtensionKeyword(locale, "co");

        compareInfo = ResolveCompareInfo(locale, collation, usage);
    }

    // CompareInfo for the resolved locale, honouring the Unicode-extension collation tailoring
    // (-u-co-). .NET ships a small set of named collation variants accessible via the BCP-47
    // CultureInfo path: GetCultureInfo("de-DE-u-co-phonebk").CompareInfo tailors Ä to Ae
    // (CompareInfo.GetCompareInfo("de-DE_phoneboo") returns a CompareInfo with the same Name
    // but does NOT actually apply the tailoring — only the full BCP-47 culture-construction
    // path is wired through to the ICU tailored collator). Falls back to the locale's default
    // CompareInfo (or the invariant one) when the tailoring isn't recognised.
    private static CompareInfo ResolveCompareInfo(string localeTag, string collation, string usage)
    {
        var bareLocale = StripUnicodeExtension(localeTag);

        // usage:"search" selects the locale's ICU "search" collation, which tailors
        // comparison for string matching rather than sorting (e.g. German search treats
        // Ä like AE, so "AE" and "Ä" no longer reorder). ECMA-402 never reflects "search"
        // as the resolved collation, so it is applied here independently of the -u-co-
        // option (test262 intl402/Collator/usage-de).
        if (usage == "search")
        {
            try
            {
                return CultureInfo.GetCultureInfo(bareLocale + "-u-co-search").CompareInfo;
            }
            catch (CultureNotFoundException) { /* fall through */ }
        }

        if (collation != null && collation != "default" && collation != "standard"
            && NetCollationSuffix(bareLocale, collation) != null)
        {
            try
            {
                return CultureInfo.GetCultureInfo(bareLocale + "-u-co-" + collation).CompareInfo;
            }
            catch (CultureNotFoundException) { /* fall through */ }
        }
        try { return CultureInfo.GetCultureInfo(bareLocale).CompareInfo; }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture.CompareInfo; }
    }

    // .NET's CompareInfo names map the BCP-47 collation key to a string suffix in a small
    // (locale-specific) table. Only the variants .NET actually exposes are mapped here.
    private static string NetCollationSuffix(string bareLocale, string collation)
    {
        if (string.IsNullOrEmpty(collation) || collation == "default" || collation == "standard")
            return null;
        var lang = bareLocale;
        var dash = lang.IndexOf('-');
        if (dash > 0) lang = lang.Substring(0, dash);
        return (lang.ToLowerInvariant(), collation) switch
        {
            ("de", "phonebk") => "phoneboo",
            ("zh", "pinyin") => "pinyin",
            ("zh", "stroke") => "stroke",
            ("zh", "phonetic") or ("zh", "zhuyin") => "pronun",
            ("zh", "trad") or ("zh", "unihan") => "radstr",
            ("ja", "unihan") => "radstr",
            ("ko", "unihan") => "korean",
            ("hu", "trad") => "techni",
            _ => null,
        };
    }

    private static string StripUnicodeExtension(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return "";
        var parts = tag.Split('-');
        for (var i = 0; i < parts.Length; i++)
            if (parts[i].Length == 1 && (parts[i][0] == 'u' || parts[i][0] == 'U'))
                return string.Join("-", parts, 0, i);
        return tag;
    }

    // This Collator's data as CollationsOfLocale reads it (JSD-0082, decision D33 (c); rows L17 and L18): the available
    // locales are every service's (decision D7), and a locale's sort collations are those this Collator resolves for it,
    // each value of Intl.supportedValuesOf("collation") that IsCollationAvailableForLocale admits. The Collator's data and
    // comparison stay its host path until SJL-17.
    internal static ICollatorLocaleData LocaleData { get; } = new CollatorLocaleData();

    private sealed class CollatorLocaleData : ICollatorLocaleData
    {
        public IReadOnlyList<string> AvailableLocales => JSIntlLocaleProviders.AvailableLocales;

        public IReadOnlyList<string> Collations => JSIntl.SupportedCollationsSorted;

        public IReadOnlyList<string?> SortLocaleData(string locale)
        {
            var bareLocale = StripUnicodeExtension(locale);
            var collations = new List<string?> { null };
            foreach (var collation in JSIntl.SupportedCollationsSorted)
            {
                if (IsCollationAvailableForLocale(bareLocale, collation))
                    collations.Add(collation);
            }

            return collations;
        }
    }

    // Whether a (canonical, recognized) collation is available for a locale. The root
    // collations "emoji" and "eor" are available everywhere; every other tailoring is
    // locale-specific and recognized only where .NET exposes it (NetCollationSuffix), e.g.
    // "phonebk" for German but not English.
    private static bool IsCollationAvailableForLocale(string bareLocale, string collation)
    {
        if (string.IsNullOrEmpty(collation) || collation == "default" || collation == "standard" || collation == "search")
            return false;
        if (collation == "emoji" || collation == "eor")
            return true;
        return NetCollationSuffix(bareLocale, collation) != null;
    }

    // The locale's default ignorePunctuation. CLDR gives Thai the only root collation with
    // alternate="shifted", so "th" defaults to true; every other locale defaults to false.
    private static bool LocaleDefaultIgnorePunctuation(string localeTag)
    {
        if (string.IsNullOrEmpty(localeTag))
            return false;
        var dash = localeTag.IndexOf('-');
        var language = dash < 0 ? localeTag : localeTag.Substring(0, dash);
        return string.Equals(language, "th", StringComparison.OrdinalIgnoreCase);
    }

    private JSIntlCollator() : base(CurrentPrototype()) { }

    // Removes the characters ignorePunctuation drops (whitespace, punctuation and
    // symbols — the variable-weighted characters in the Unicode Collation Algorithm),
    // so two strings can be tested for differing only in such characters.
    private static string StripIgnorablePunctuation(string s)
    {
        StringBuilder sb = null;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            {
                sb ??= new StringBuilder(s.Length).Append(s, 0, i);
                continue;
            }
            sb?.Append(c);
        }
        return sb?.ToString() ?? s;
    }

    public JSValue Compare(in Arguments a)
    {
        var left = a.Get1().StringValue;
        var right = a.GetAt(1).StringValue;
        // ECMA-402 §10.3.5 (CompareStrings) treats canonically-equivalent strings as equal,
        // but .NET's CompareInfo.Compare reads the raw UTF-16 code units and so distinguishes
        // (ä,combining-dot-below) from (a,combining-dot-below,combining-diaeresis) — different
        // composition orders of the same canonical character. Pre-normalizing both sides to
        // NFD (canonical decomposition) folds the orderings together before the locale-aware
        // collation runs, matching the spec's "canonically equivalent strings compare equal"
        // requirement (test262 intl402/Collator/prototype/compare/canonically-equivalent-strings).
        // The decomposition is Broiler.Unicode's (SJL-7.5), which passes a lone surrogate through
        // where the host's threw (N1) and answers a string already in NFD as itself; the collation
        // that follows is still the host's (SJL-17).
        left = String.JSString.NormalizeText(left, NormalForm.Nfd);
        right = String.JSString.NormalizeText(right, NormalForm.Nfd);

        var options = CompareOptions.None;
        if (sensitivity == "base")
            options |= CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        else if (sensitivity == "accent")
            options |= CompareOptions.IgnoreCase;
        else if (sensitivity == "case")
            options |= CompareOptions.IgnoreNonSpace;
        if (ignorePunctuation)
            options |= CompareOptions.IgnoreSymbols;

        var result = compareInfo.Compare(left, right, options);

        // Some locale collations (notably Thai) give punctuation/whitespace zero
        // primary weight, so "" and " " compare equal even though ignorePunctuation is
        // false. ECMA-402 only ignores those characters when ignorePunctuation is true,
        // so when it is false and the two strings are equal apart from such ignorable
        // characters, break the tie on them (test262
        // intl402/Collator/prototype/compare/ignorePunctuation).
        if (result == 0 && !ignorePunctuation && !string.Equals(left, right, StringComparison.Ordinal))
        {
            // When removing the ignorable punctuation/whitespace makes the two strings
            // identical, their ONLY difference is those characters — so break the tie on
            // them (without ignorePunctuation they are significant). A difference that
            // survives stripping is a genuine collation tie (e.g. case/accent folded by
            // the sensitivity) and is left equal.
            if (string.Equals(StripIgnorablePunctuation(left), StripIgnorablePunctuation(right), StringComparison.Ordinal))
                result = Math.Sign(string.CompareOrdinal(left, right));
        }

        return CreateNumber(result);
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlCollator @this)
            throw JSEngine.NewTypeError("Intl.Collator.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.FastAddValue(KeyStrings.GetOrCreate("locale"), CreateString(@this.locale), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("usage"), CreateString(@this.usage), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("sensitivity"), CreateString(@this.sensitivity), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("ignorePunctuation"), @this.ignorePunctuation ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("collation"), CreateString(@this.collation), JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("numeric"), @this.numeric ? BooleanTrue : BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.GetOrCreate("caseFirst"), CreateString(@this.caseFirst), JSPropertyAttributes.EnumerableConfigurableValue);
        return result;
    }

    private static bool TryGetOwnOption(JSObject options, string name, out JSValue value)
    {
        value = JSUndefined.Value;
        if (options == null)
            return false;

        // GetOption (ECMA-402) reads the property with [[Get]]: an accessor option must
        // have its getter invoked (and a throwing getter must propagate), not be skipped
        // because the descriptor carries no [[Value]]. Reading the descriptor's value
        // ignored getters entirely, so e.g. `new Intl.Collator(l, { get usage() { throw } })`
        // silently swallowed the exception.
        var key = KeyStrings.GetOrCreate(name);
        value = options[key];
        return !value.IsUndefined;
    }

    private static bool TryGetUnicodeExtension(string locale, string key, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(locale))
            return false;

        var parts = locale.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var u = -1;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length != 1)
                continue;
            // Stop at the private-use ("x") singleton; a "u" inside private-use is a
            // private subtag, not a Unicode extension.
            if (parts[i][0] == 'u' || parts[i][0] == 'U') { u = i; break; }
            if (parts[i][0] == 'x' || parts[i][0] == 'X') return false;
        }

        if (u < 0)
            return false;

        for (var i = u + 1; i < parts.Length; i++)
        {
            // The Unicode extension ends at the next singleton (start of another extension).
            if (parts[i].Length == 1)
                return false;
            if (!string.Equals(parts[i], key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = i + 1 < parts.Length && parts[i + 1].Length > 2 ? parts[i + 1] : "true";
            return true;
        }

        return false;
    }


    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.Collator");

    // %Intl.Collator.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.Collator` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);
}

// Intl.DateTimeFormat (ECMA-402 sec-datetimeformat-objects, as tc39/proposal-temporal@e8cc03fc's intl.html amends it) over the
// shared library's DateTimeFormat (SJL-9.5, task 8.5, sub-area (d); Broiler.VM's JSD-0082, decision D24): the constructor
// reads every option once, in the specification's order, each checked as it is read, ResolveLocale's ca, hc and nu keys are
// the library's over the values the data gives the matched locale, and the hour cycle, the calendar's data, the pattern of the
// styles or of the components, the numbering system's digits, the zone's names, the intervals and the formats of Temporal's
// types are the library's, over the data home's date data (decision D5) and the shared time zones and calendars. The engine
// keeps the reads, the default zone (the host's, decision SJL-D-19, row T11), ToDateTimeFormattable, the bound format (row
// T4), the parts arrays and each RangeError and TypeError, made from the library's status; it passes no charge (row T40).
// Deleted (2026-11-09): JSIntlDateTimeFormatEngine.cs whole (the en-only pattern resolution, field formatting, interval
// algorithm and zone names), the option snapshot and every reader of it (OptionString, FractionalSecondDigits,
// ResolveHour12, ResolveHourCycle, ResolveEnginePattern, ResolvedCalendar, ResolvedCalendarId, UnicodeKeyword,
// ResolveFields, the TemporalFields flags, RequestedFields, ClassifyTemporal, CheckTemporalCalendar, EffectiveTemporalFields,
// FormatTemporalToParts, FormatTemporalRangeToParts, ResolveTemporalPattern, StyleMonthWidth, ComputeRangeParts, MapNumbering,
// the dayPeriod branches and their CLDR day-period reads through CldrLocaleData, SupportsDayPeriod, UsesHourFormatting,
// DayPeriodStyle, WallHourMinute, DayPeriodRuleOverrides, DayPeriodWidth, DayPeriodNameForStyle, DayPeriodName,
// ResolveDayPeriodKey, MatchDayPeriodRules, ReadTimeZoneOption, TimeClipRange), the value tables of the option reads now
// written once (DateTimeStyleValues among them), and the members row T42 names: the constructor from a CultureInfo,
// Format(DateTimeOffset, JSObject), FormatEnglishHour (both overloads; the int one's last readers were the dayPeriod
// branches), FormatDayPeriod and the locale field. Its public members stay (decision SJL-D-25, disposition (a)): the
// constructor, Format, the prototype methods, TemporalToLocaleString, ToDateTimeOptions and ApplyAllDefaults (which no caller
// of this repository reads any more), and Get(CultureInfo), over the library.
// Revised 2026-11-09, at SJL-9.5 (review of task 8.5): the deletion list above also covers JSIntlResolvedOptionsExtensions
// whole with its SetIfDefined, Has and RequiresExactCalendar, and, in TemporalTimeZones, ResolveFormatterTimeZone, whose
// last reader was ReadTimeZoneOption. The OptionString it names was the option snapshot's instance reader; the private
// static OptionString below is a new reader of a defined option's String.
public class JSIntlDateTimeFormat : JSObject
{
    private static readonly ConcurrentDictionary<string, JSIntlDateTimeFormat> formats = new();
    private static readonly KeyString DateStyleKey = KeyStrings.GetOrCreate("dateStyle");
    private static readonly KeyString TimeStyleKey = KeyStrings.GetOrCreate("timeStyle");
    private static readonly KeyString TimeZoneKey = KeyStrings.GetOrCreate("timeZone");
    private static readonly KeyString Hour12Key = KeyStrings.GetOrCreate("hour12");
    private static readonly KeyString HourCycleKey = KeyStrings.GetOrCreate("hourCycle");
    private static readonly KeyString CalendarKey = KeyStrings.GetOrCreate("calendar");
    private static readonly KeyString FormatMatcherKey = KeyStrings.GetOrCreate("formatMatcher");
    private static readonly KeyString FractionalSecondDigitsKey = KeyStrings.GetOrCreate("fractionalSecondDigits");

    // ECMA-402's Table 16 of date-time components, in table order, with the values each option admits; the constructor reads
    // them in this order.
    private static readonly (KeyString Key, string[] Values)[] ComponentOptions =
    [
        (KeyStrings.GetOrCreate("weekday"), ["narrow", "short", "long"]),
        (KeyStrings.GetOrCreate("era"), ["narrow", "short", "long"]),
        (KeyStrings.GetOrCreate("year"), ["2-digit", "numeric"]),
        (KeyStrings.GetOrCreate("month"), ["2-digit", "numeric", "narrow", "short", "long"]),
        (KeyStrings.GetOrCreate("day"), ["2-digit", "numeric"]),
        (KeyStrings.GetOrCreate("dayPeriod"), ["narrow", "short", "long"]),
        (KeyStrings.GetOrCreate("hour"), ["2-digit", "numeric"]),
        (KeyStrings.GetOrCreate("minute"), ["2-digit", "numeric"]),
        (KeyStrings.GetOrCreate("second"), ["2-digit", "numeric"]),
        (FractionalSecondDigitsKey, null),
        (KeyStrings.GetOrCreate("timeZoneName"), ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"]),
    ];

    private static readonly string[] HourCycleValues = ["h11", "h12", "h23", "h24"];
    private static readonly string[] FormatMatcherValues = ["basic", "best fit"];
    private static readonly string[] StyleValues = ["full", "long", "medium", "short"];

    // The date-time data every format reads (the library's DateTimeData over the engine's locale data, the shared time zones
    // and the calendars), made once.
    // Revised 2026-11-09, at SJL-9.5's stage 4 (the shared library's merge Broiler-Platform/Broiler.JS#1108): the comment
    // ended "made once; and one per numbering system whose own decimal symbol the data states (decision D21)", and
    // DatesBySystem, a ConcurrentDictionary of that per-system data, followed. The library's DateTimeFormat now writes a
    // numbering system's own decimal symbol itself (row T37, decision D21), so every format reads this one data and
    // DatesBySystem is deleted.
    private static readonly Lazy<DateTimeData> SharedDates = new(
        static () => new DateTimeData(JSIntlData.LocaleData, Temporal.TemporalTimeData.TimeZones, Temporal.TemporalAdapter.Calendars),
        System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The shared library's format: what CreateDateTimeFormat resolved, its formatter and the formats of Temporal's types.</summary>
    internal DateTimeFormat Core { get; }

    /// <summary>[[BoundFormat]] (row T4, a correction: a new function was made at each read, so dtf.format !== dtf.format).</summary>
    internal JSFunction BoundFormat { get; set; }

    public JSIntlDateTimeFormat(in Arguments a) : base(CurrentPrototype())
    {
        var options = JSIntl.ValidateConstructorArguments("DateTimeFormat", in a, out var canonical, requireNew: false);
        Core = Create(canonical, options, DateTimeRequired.Any, DateTimeDefaults.Date, null);
    }

    private JSIntlDateTimeFormat(JSObject prototype, DateTimeFormat core) : base(prototype)
    {
        Core = core;
    }

    private JSIntlDateTimeFormat(DateTimeFormat core) : base()
    {
        Core = core;
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.DateTimeFormat");

    // %Intl.DateTimeFormat.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.DateTimeFormat` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);

    // A format for a .NET culture's name with no options, cached by the name: the format of `new Intl.DateTimeFormat(tag)`, a
    // name that is no well-formed tag read as no locale (the default). Over the library since SJL-9.5, task 8.5, as JSD-0082's
    // D24 keeps it.
    public static JSIntlDateTimeFormat Get(CultureInfo culture)
        => formats.GetOrAdd(culture.Name, static name =>
        {
            var locales = JSValue.CreateArray();
            if (name.Length != 0 && LocaleId.TryCanonicalize(name, JSIntlData.LocaleData, out var tag) == IntlStatus.Completed)
                locales.AddArrayItem(CreateString(tag));
            return new JSIntlDateTimeFormat(Create(locales, null, DateTimeRequired.Any, DateTimeDefaults.Date, null));
        });

    // ECMA-402's CreateDateTimeFormat (sec-createdatetimeformat, as e8cc03fc amends it), after CanonicalizeLocaleList,
    // CoerceOptionsToObject and localeMatcher (ValidateConstructorArguments): every read in the specification's order, each
    // checked as it is read, then the shared library's DateTimeFormat.Create over the record read, whose refusals are this
    // engine's TypeErrors. zonedTimeZone is Temporal.ZonedDateTime.prototype.toLocaleString's own zone, which the options may
    // not name.
    private static DateTimeFormat Create(JSValue canonicalLocales, JSObject options, DateTimeRequired required, DateTimeDefaults defaults, string zonedTimeZone)
    {
        // calendar, then numberingSystem, each a well-formed Unicode type (row L14).
        var calendar = OptionString(options, CalendarKey);
        if (calendar != null && !LocaleId.IsUnicodeType(calendar))
            throw JSEngine.NewRangeError("Invalid calendar option");

        var numberingSystem = JSIntl.ReadNumberingSystemOption(options);

        // hour12 (ToBoolean), then hourCycle; hour12 makes hourCycle ECMA-402's null.
        var hour12Value = options == null ? JSUndefined.Value : options[Hour12Key];
        bool? hour12 = hour12Value.IsUndefined ? null : hour12Value.BooleanValue;
        var hourCycle = JSIntl.GetOption(options, HourCycleKey, HourCycleValues, false, null);

        // ResolveLocale's ca, hc and nu keys, the shared library's, over the values the data gives the matched locale.
        // Revised 2026-11-09, at SJL-9.5's stage 4: the ca key's values were "DateTimeFormat.CalendarKeyValues", the static
        // list whose first value is gregory. They are now DateTimeFormat.CalendarKeyValuesOf the data locale, which puts first
        // the first calendar CLDR's calendarPreferenceData prefers for the locale's likely region (fa persian, th buddhist),
        // as the shared library's merge Broiler-Platform/Broiler.JS#1108 asks an engine to hand them (row T16, data policy C5).
        var resolved = JSIntl.ResolveLocale(canonicalLocales, locale =>
        [
            new RelevantKey("ca", DateTimeFormat.CalendarKeyValuesOf(JSIntlData.LocaleData, JSIntlLocaleProviders.DataLocale(locale))) { Option = calendar },
            new RelevantKey("hc", DateTimeFormat.HourCycleKeyValues) { Option = hour12 is null ? hourCycle : null, OptionIsNull = hour12 is not null },
            new RelevantKey("nu", JSIntl.NumberingSystemValues(locale)) { Option = numberingSystem },
        ]);

        // timeZone: the host's zone where it is undefined (decision SJL-D-19, row T11); Temporal.ZonedDateTime's toLocaleString
        // names its own zone, and refuses one the options name.
        var timeZoneValue = options == null ? JSUndefined.Value : options[TimeZoneKey];
        if (zonedTimeZone != null)
        {
            if (!timeZoneValue.IsUndefined)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime.toLocaleString: the timeZone option is not allowed");
            timeZoneValue = CreateString(zonedTimeZone);
        }

        var timeZoneText = timeZoneValue.IsUndefined ? Temporal.TemporalTimeZones.SystemTimeZoneIdentifier() : timeZoneValue.StringValue;
        if (ResolvedTimeZone.Resolve(timeZoneText, Temporal.TemporalTimeZones.Database, out var timeZone) != IntlStatus.Completed)
            throw JSEngine.NewRangeError($"Invalid timeZone option: {timeZoneText}");

        // The components, in Table 16's order; then formatMatcher, dateStyle and timeStyle.
        var components = new string[ComponentOptions.Length];
        int? fractionalSecondDigits = null;
        for (var i = 0; i < ComponentOptions.Length; i++)
        {
            var (key, values) = ComponentOptions[i];
            if (values is null)
                fractionalSecondDigits = JSIntl.GetNumberOption(options, key, 1, 3);
            else
                components[i] = JSIntl.GetOption(options, key, values, false, null);
        }

        _ = JSIntl.GetOption(options, FormatMatcherKey, FormatMatcherValues, false, "best fit");
        var dateStyle = JSIntl.GetOption(options, DateStyleKey, StyleValues, false, null);
        var timeStyle = JSIntl.GetOption(options, TimeStyleKey, StyleValues, false, null);

        var status = DateTimeFormat.Create(
            DatesFor(),
            new ResolvedLocale(resolved.Locale, JSIntlLocaleProviders.DataLocale(resolved.DataLocale), resolved.KeyValues),
            new DateTimeFormatOptions
            {
                Hour12 = hour12,
                TimeZone = timeZone,
                Components = new DateTimeComponents
                {
                    Weekday = components[0],
                    Era = components[1],
                    Year = components[2],
                    Month = components[3],
                    Day = components[4],
                    DayPeriod = components[5],
                    Hour = components[6],
                    Minute = components[7],
                    Second = components[8],
                    FractionalSecondDigits = fractionalSecondDigits,
                    TimeZoneName = components[10],
                },
                DateStyle = dateStyle,
                TimeStyle = timeStyle,
                Required = required,
                Defaults = defaults,
                ForZonedDateTime = zonedTimeZone != null,
            },
            out var format);

        return status switch
        {
            IntlStatus.Completed => format,
            IntlStatus.StyleWithComponents => throw JSEngine.NewTypeError("Intl.DateTimeFormat: dateStyle and timeStyle cannot be combined with a date-time component option"),
            IntlStatus.DateFormatWithTimeStyle => throw JSEngine.NewTypeError("Intl.DateTimeFormat: timeStyle may not be used with a date-only operation"),
            IntlStatus.TimeFormatWithDateStyle => throw JSEngine.NewTypeError("Intl.DateTimeFormat: dateStyle may not be used with a time-only operation"),
            _ => throw new InvalidOperationException("The shared date-time format answered " + status + ", which CreateDateTimeFormat does not expect."),
        };
    }

    // An option's String (ToString of a defined value), or null when it is undefined.
    private static string OptionString(JSObject options, KeyString key)
    {
        var value = options == null ? JSUndefined.Value : options[key];
        return value.IsUndefined ? null : value.StringValue;
    }

    // The date-time data a format reads, whatever its numbering system.
    // Revised 2026-11-09, at SJL-9.5's stage 4: it was "DatesFor(string numberingSystem)", the data a format of a numbering
    // system reads, which for a system other than latn whose own decimal symbol the data states returned a DateTimeData over
    // NumberingSystemDecimalData (DatesBySystem's). The library reads that symbol itself now (row T37), so it returns the
    // shared data, and the caller's resolved nu, which only it read, is no longer taken.
    private static DateTimeData DatesFor() => SharedDates.Value;

    // ---- Temporal's toLocaleString and Date's toLocale* ------------------------------------------------------------------

    // Temporal.X.prototype.toLocaleString (the proposal's amendment of ECMA-402 s15.11): a format made for the call by
    // CreateDateTimeFormat with the type's required and defaults - date for PlainDate, PlainYearMonth and PlainMonthDay, time
    // for PlainTime, any and all for the others - which writes the value; a ZonedDateTime's in its own zone.
    public static JSValue TemporalToLocaleString(JSValue temporal, JSValue locales, JSValue options, string defaultTimeZone = null)
    {
        var (required, defaults) = temporal switch
        {
            JSTemporalPlainDate or JSTemporalPlainYearMonth or JSTemporalPlainMonthDay => (DateTimeRequired.Date, DateTimeDefaults.Date),
            JSTemporalPlainTime => (DateTimeRequired.Time, DateTimeDefaults.Time),
            _ => (DateTimeRequired.Any, DateTimeDefaults.All),
        };

        var format = Make(locales, options, required, defaults, defaultTimeZone);
        var (formatter, time) = format.HandleValue(temporal);
        return CreateString(formatter.Format(time));
    }

    // Date.prototype.toLocaleString, toLocaleDateString and toLocaleTimeString (ECMA-402 sup-date.prototype.tolocalestring
    // and the two others): CreateDateTimeFormat with any/date/time and all/date/time, which reads each option once (row T2, a
    // correction: ToDateTimeOptions read the components and styles once more, first), and the time value it writes.
    internal static string ToLocaleString(double time, JSValue locales, JSValue options, DateTimeRequired required, DateTimeDefaults defaults)
    {
        var format = Make(locales, options, required, defaults, null);
        var (formatter, x) = format.HandleValue(CreateNumber(time));
        return formatter.Format(x);
    }

    // A format made for one call, from the arguments' locales and options, as the constructor reads them.
    // Noted 2026-11-09, at SJL-9.5 (review of task 8.5, rows T2 and T38, corrections): the options go through
    // CoerceOptionsToObject here, as the specification's CreateDateTimeFormat asks, so options null is a TypeError for
    // Date.prototype.toLocaleString, toLocaleDateString and toLocaleTimeString, where ToDateTimeOptions read null as an
    // empty bag and the call formatted; for Temporal.ZonedDateTime.prototype.toLocaleString options null is a TypeError
    // where the former engine read it as an empty bag, and a primitive such as 5 is boxed and formats where it was a
    // TypeError. Neither row names these cases; IntlDateTimeFormatLibraryTests pins them.
    private static JSIntlDateTimeFormat Make(JSValue locales, JSValue options, DateTimeRequired required, DateTimeDefaults defaults, string zonedTimeZone)
    {
        var arguments = new Arguments(JSUndefined.Value, locales, options ?? JSUndefined.Value);
        var optionsObject = JSIntl.ValidateConstructorArguments("DateTimeFormat", in arguments, out var canonical, requireNew: false);
        return new JSIntlDateTimeFormat(CurrentPrototype(), Create(canonical, optionsObject, required, defaults, zonedTimeZone));
    }

    // The date/time component options whose presence suppresses defaulting (era / timeZoneName are
    // supplementary and do not count).
    private static readonly string[] DateTimeNeedsDefaultsKeys =
    [
        "weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits",
    ];

    private static readonly string[] DateTimeFormatOptionKeys =
    [
        "localeMatcher", "calendar", "numberingSystem", "hour12", "hourCycle", "timeZone",
        "weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second",
        "fractionalSecondDigits", "timeZoneName", "formatMatcher", "dateStyle", "timeStyle",
    ];

    // ToDateTimeOptions(options, "any", "all"): when no date/time component or style is requested, the
    // default is the full date+time, as Date.prototype.toLocaleString's defaults "all" ask (unlike the Intl.DateTimeFormat
    // constructor, which uses "date"). Returns the options unchanged when defaults are not needed or it is not an object.
    // Noted 2026-11-09, at SJL-9.5 (task 8.5): no caller of this repository reads it; it stays a public member (JSD-0082,
    // D24's T42 paragraph; decision SJL-D-25).
    // Revised 2026-11-09, at SJL-9.5 (review of task 8.5): the comment read "Used by Date.prototype.toLocaleString (the
    // legacy [[Call]] uses defaults "all", unlike the Intl.DateTimeFormat constructor which uses "date")."; Date.prototype's
    // toLocale* methods read their options through CreateDateTimeFormat's required and defaults (ToLocaleString). This
    // member and ToDateTimeOptions stay as hand-written option-bag code with no caller and no call into the shared library,
    // where D24's T42 paragraph says they "stay, over the library".
    public static JSValue ApplyAllDefaults(JSValue options)
    {
        if (options is not JSObject o)
            return options;
        foreach (var name in DateTimeNeedsDefaultsKeys)
            if (!o[KeyStrings.GetOrCreate(name)].IsUndefined)
                return options;
        if (!o[DateStyleKey].IsUndefined || !o[TimeStyleKey].IsUndefined)
            return options;

        // Null prototype: ToDateTimeOptions exposes the merged bag to downstream
        // option reads, which would otherwise pick up Object.prototype taint planted by
        // client code (e.g. Object.prototype.year = '2-digit').
        var merged = new JSObject();
        merged.SetPrototypeOf(Null.JSNull.Value);
        foreach (var name in DateTimeFormatOptionKeys)
        {
            var key = KeyStrings.GetOrCreate(name);
            var v = o[key];
            if (!v.IsUndefined) merged[key] = v;
        }
        foreach (var name in new[] { "year", "month", "day", "hour", "minute", "second" })
            merged[KeyStrings.GetOrCreate(name)] = CreateString("numeric");
        return merged;
    }

    private static readonly string[] DateComponentKeys = ["weekday", "year", "month", "day"];
    private static readonly string[] TimeComponentKeys =
        ["dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"];

    // ToDateTimeOptions (ECMA-402 §11.5.1.1), parameterised by the <paramref name="required"/> and
    // <paramref name="defaults"/> fields ("date" / "time" / "all" / "any"). Returns a fresh options object carrying the supplied
    // options' properties plus the defaulted numeric components; throws a TypeError for the date+timeStyle / time+dateStyle
    // conflicts.
    // Revised 2026-11-09, at SJL-9.5 (task 8.5, row T2): Date.prototype's toLocale* methods no longer read their options
    // through it, but through CreateDateTimeFormat's required and defaults (ToLocaleString), which reads each option once; it
    // stays a public member (decision SJL-D-25). The comment read: "Date.prototype's legacy toLocaleString /
    // toLocaleDateString / toLocaleTimeString construct an Intl.DateTimeFormat with ToDateTimeOptions(options,
    // "any"|"date"|"time", "all"|"date"|"time") respectively, which is why their default output (and their option validation)
    // must match Intl.DateTimeFormat exactly."
    public static JSValue ToDateTimeOptions(JSValue options, string required, string defaults)
    {
        var source = options.IsUndefined ? null : options as JSObject;

        var needDefaults = true;
        if (source != null)
        {
            if (required is "date" or "any")
                foreach (var name in DateComponentKeys)
                    if (!source[KeyStrings.GetOrCreate(name)].IsUndefined) needDefaults = false;
            if (required is "time" or "any")
                foreach (var name in TimeComponentKeys)
                    if (!source[KeyStrings.GetOrCreate(name)].IsUndefined) needDefaults = false;

            var hasDateStyle = !source[DateStyleKey].IsUndefined;
            var hasTimeStyle = !source[TimeStyleKey].IsUndefined;
            if (required == "date" && hasTimeStyle)
                throw JSEngine.NewTypeError("Intl.DateTimeFormat: timeStyle may not be used with a date-only operation");
            if (required == "time" && hasDateStyle)
                throw JSEngine.NewTypeError("Intl.DateTimeFormat: dateStyle may not be used with a time-only operation");
            if (hasDateStyle || hasTimeStyle)
                needDefaults = false;
        }

        var merged = new JSObject();
        if (source != null)
            foreach (var name in DateTimeFormatOptionKeys)
            {
                var key = KeyStrings.GetOrCreate(name);
                var v = source[key];
                if (!v.IsUndefined) merged[key] = v;
            }

        if (needDefaults && defaults is "date" or "all")
            foreach (var name in new[] { "year", "month", "day" })
                merged[KeyStrings.GetOrCreate(name)] = CreateString("numeric");
        if (needDefaults && defaults is "time" or "all")
            foreach (var name in new[] { "hour", "minute", "second" })
                merged[KeyStrings.GetOrCreate(name)] = CreateString("numeric");

        return merged;
    }

    // ---- the values a format writes --------------------------------------------------------------------------------------

    // The proposal's IsTemporalObject (amended ECMA-402 s15.6.12).
    private static bool IsTemporalDateTime(JSValue value)
        => value is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalPlainTime
            or JSTemporalPlainYearMonth or JSTemporalPlainMonthDay or JSTemporalInstant
            or JSTemporalZonedDateTime;

    // ToDateTimeFormattable (amended ECMA-402 s15.6.11): a Temporal object as itself, anything else as a Number, here as the
    // shared library's value to format.
    private static DateTimeValue Formattable(JSValue value) => value switch
    {
        JSTemporalPlainDate date => DateTimeValue.PlainDate(date.IsoDate, date.calendarId),
        JSTemporalPlainYearMonth yearMonth => DateTimeValue.PlainYearMonth(yearMonth.IsoDate, yearMonth.calendarId),
        JSTemporalPlainMonthDay monthDay => DateTimeValue.PlainMonthDay(monthDay.IsoDate, monthDay.calendarId),
        JSTemporalPlainTime time => DateTimeValue.PlainTime(time.IsoTime),
        JSTemporalPlainDateTime dateTime => DateTimeValue.PlainDateTime(dateTime.IsoDateTime, dateTime.calendarId),
        JSTemporalInstant instant => DateTimeValue.Instant(instant.epochNanoseconds),
        JSTemporalZonedDateTime zoned => DateTimeValue.ZonedDateTime(zoned.epochNanoseconds, zoned.calendarId),
        _ => DateTimeValue.Number(value.DoubleValue),
    };

    // The proposal's HandleDateTimeValue (amended ECMA-402 s15.6.22), the shared format's: the formatter a value is written in
    // and its time in milliseconds; a plain value's format is written at UTC, so no zone moves it.
    private (DateTimeFormatter Formatter, double Time) HandleValue(JSValue value)
    {
        var status = Core.HandleDateTimeValue(Formattable(value), out var formatter, out var time);
        return status == IntlStatus.Completed ? (formatter, time) : throw ValueError(value, status);
    }

    // The value of format and formatToParts: now where it is undefined, else ToDateTimeFormattable's.
    private (DateTimeFormatter Formatter, double Time) FormatValue(in Arguments a)
    {
        if (a.Length == 0 || a[0] == null || a[0].IsUndefined)
            return (Core.Formatter, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var value = a.Get1();
        return HandleValue(IsTemporalDateTime(value) ? value : CreateNumber(value.DoubleValue));
    }

    // The start and end a range method formats (sec-intl.datetimeformat.prototype.formatRange, as the proposal amends it):
    // neither undefined, checked before either is converted; each read by ToDateTimeFormattable, in argument order, then the
    // shared format's HandleDateTimeRange.
    private (DateTimeFormatter Formatter, double X, double Y) RangeValues(in Arguments a)
    {
        var start = a.Get1();
        var end = a.GetAt(1);
        if (start == null || start.IsUndefined || end == null || end.IsUndefined)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat range start and end dates must not be undefined");

        var x = IsTemporalDateTime(start) ? start : CreateNumber(start.DoubleValue);
        var y = IsTemporalDateTime(end) ? end : CreateNumber(end.DoubleValue);
        var status = Core.HandleDateTimeRange(Formattable(x), Formattable(y), out var formatter, out var xTime, out var yTime);
        if (status == IntlStatus.Completed)
            return (formatter, xTime, yTime);

        // The value the error names: the start where the start alone is refused, else the end.
        var refused = status == IntlStatus.RangeOfDifferentTypes
            || Core.HandleDateTimeValue(Formattable(x), out _, out _) != IntlStatus.Completed ? x : y;
        throw ValueError(refused, status);
    }

    // The error of a value the shared format refuses: a RangeError for a time TimeClip refuses or a calendar the format does
    // not write, a TypeError for a type the options give no field of, a ZonedDateTime, or a range of two types.
    private Exception ValueError(JSValue value, IntlStatus status)
    {
        var calendar = value switch
        {
            JSTemporalPlainDate date => date.calendarId,
            JSTemporalPlainYearMonth yearMonth => yearMonth.calendarId,
            JSTemporalPlainMonthDay monthDay => monthDay.calendarId,
            JSTemporalPlainDateTime dateTime => dateTime.calendarId,
            JSTemporalZonedDateTime zoned => zoned.calendarId,
            _ => "iso8601",
        };

        return status switch
        {
            IntlStatus.TimeValueOutOfRange => JSEngine.NewRangeError("Invalid time value"),
            IntlStatus.TemporalCalendarMismatch => JSEngine.NewRangeError(
                $"Intl.DateTimeFormat: calendar \"{calendar}\" does not match the formatter calendar \"{Core.Calendar}\""),
            IntlStatus.TemporalTypeNotFormatted => JSEngine.NewTypeError("Intl.DateTimeFormat: the format options and the Temporal type have no fields in common"),
            IntlStatus.ZonedDateTimeNotFormatted => JSEngine.NewTypeError(
                "Intl.DateTimeFormat: cannot format a Temporal.ZonedDateTime; use zonedDateTime.toLocaleString() instead"),
            IntlStatus.RangeOfDifferentTypes => JSEngine.NewTypeError("Intl.DateTimeFormat.formatRange: both arguments must be the same Temporal type"),
            _ => new InvalidOperationException("The shared date-time format answered " + status + ", which no step of this adapter expects."),
        };
    }

    // An array of { type, value } objects, with source where a part has one.
    private static JSValue PartsArray(IReadOnlyList<DateTimePart> parts)
    {
        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var sourceKey = KeyStrings.GetOrCreate("source");
        var array = CreateArray();
        foreach (var part in parts)
        {
            var obj = new JSObject();
            obj[typeKey] = CreateString(part.Type);
            obj[valueKey] = CreateString(part.Value);
            if (part.Source != null)
                obj[sourceKey] = CreateString(part.Source);
            array.AddArrayItem(obj);
        }
        return array;
    }

    // ---- the prototype -----------------------------------------------------------------------------------------------------

    public JSValue Format(in Arguments a)
    {
        var (formatter, time) = FormatValue(in a);
        return new JSString(formatter.Format(time));
    }

    public static JSValue FormatPrototype(in Arguments a)
        => a.This is JSIntlDateTimeFormat @this
            ? @this.Format(in a)
            : throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.format called on incompatible receiver");

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatToParts called on incompatible receiver");

        var (formatter, time) = @this.FormatValue(in a);
        return PartsArray(formatter.FormatToParts(time));
    }

    public static JSValue FormatRangePrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRange called on incompatible receiver");

        var (formatter, x, y) = @this.RangeValues(in a);
        return CreateString(formatter.FormatRange(x, y));
    }

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatRangeToParts called on incompatible receiver");

        var (formatter, x, y) = @this.RangeValues(in a);
        return PartsArray(formatter.FormatRangeToParts(x, y));
    }

    // resolvedOptions, in Table 16's order, as the shared format resolved it: the hour cycle where the pattern has an hour, the
    // components of the selected pattern for a format made from components (row T7), fractionalSecondDigits a Number, and the
    // styles; each an own data property (CreateDataPropertyOrThrow), so a setter on Object.prototype is never called.
    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.resolvedOptions called on incompatible receiver");

        var format = @this.Core;
        var result = new JSObject();
        void Put(string name, string value)
        {
            if (value != null)
                result.CreateDataProperty(KeyStrings.GetOrCreate(name), CreateString(value));
        }

        Put("locale", format.Locale);
        Put("calendar", format.Calendar);
        Put("numberingSystem", format.NumberingSystem);
        Put("timeZone", format.TimeZone);

        if (format.HourCycle is { } hourCycle)
        {
            Put("hourCycle", hourCycle);
            result.CreateDataProperty(Hour12Key, format.Hour12 == true ? BooleanTrue : BooleanFalse);
        }

        foreach (var (name, value) in format.ResolvedComponents)
        {
            if (name == "fractionalSecondDigits")
                result.CreateDataProperty(FractionalSecondDigitsKey, CreateNumber(int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture)));
            else
                Put(name, value);
        }

        Put("dateStyle", format.DateStyle);
        Put("timeStyle", format.TimeStyle);
        return result;
    }
}

// Deleted 2026-11-09, at SJL-9.5's stage 4: NumberingSystemDecimalData, the internal ILocaleData adapter that answered
// symbols.decimal with the data's symbols@<system>.decimal (at the data locale, then at und, then the latn one) for a
// date-time format of a numbering system, task 8.5's stand-in for a library change. Its remarks read "This adapter is the
// engine's until the library reads the system's own symbol itself, a library change made as its own merge (decision D52,
// SJL-D-9)". That merge is Broiler-Platform/Broiler.JS#1108 (the owner's decision of 2026-11-09, "Keep + fix library
// first"): the library's DateTimeFormat reads symbols@<numberingSystem>.decimal at the locale, then at und, then latn's, then
// "." (row T37, decision D21), for a keyword's system and a locale's default system alike.
