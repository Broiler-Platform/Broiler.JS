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
                var locale = ResolveLocaleFromCanonical(canonical, NumberFormatRelevantKeys);
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

    // The locale's default numbering system, the first of its nu values (decision D13, row L22), which replaces the
    // JSIntlDefaultNumberingSystems table generated from CLDR 47.0.0.
    internal static string DefaultNumberingSystem(string locale) => NumberingSystemValues(locale)[0];

    // A numbering system's digit d (0 to 9) from the data's numbering-system table (ECMA-402 Table 12), or the ASCII digit
    // for latn or a system the data does not hold; it replaces JSIntlNumberFormat's ContiguousDigitZero and AlgorithmicDigits
    // tables and MapDigit (deleted 2026-11-08), for DateTimeFormat's digits until task 8.5 moves them.
    internal static string NumberingSystemDigit(string numberingSystem, int d)
        => numberingSystem is null or "latn" || JSIntlData.LocaleData.NumberingSystemDigits(numberingSystem) is not { Count: 10 } digits
            ? ((char)('0' + d)).ToString()
            : digits[d];

    // The parts of a NumberFormat's formatToParts as (type, value) pairs, for Intl.DurationFormat's internals until task 8.6
    // moves them onto the library's duration format.
    internal static List<(string type, string value)> NumberParts(IReadOnlyList<NumberPart> parts)
    {
        var list = new List<(string, string)>(parts.Count);
        foreach (var part in parts)
            list.Add((part.Type, part.Value));
        return list;
    }

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

    // The relevant extension keys of the services that resolve none: ListFormat, Segmenter, DisplayNames and PluralRules.
    // Revised 2026-11-08, at SJL-9.5 (task 8.4): ListFormat, DisplayNames and PluralRules resolve through the shared
    // library's ResolveLocale with NoRelevantKey; Segmenter, SJL-7's, still reads this list.
    internal static readonly string[] NoRelevantKeys = [];

    // The Unicode-extension keys each service uses to negotiate the resolved locale. NumberFormatRelevantKeys, nu, is
    // DurationFormat's.
    // Revised 2026-11-08, at SJL-9.5 (task 8.4): NumberFormat and RelativeTimeFormat resolve nu through the shared
    // library's ResolveLocale (ResolveNumberingSystemLocale) and no longer read the array; the comment read
    // "NumberFormat's, nu, is also RelativeTimeFormat's and DurationFormat's."
    internal static readonly string[] NumberFormatRelevantKeys = ["nu"];
    internal static readonly string[] DateTimeFormatRelevantKeys = ["ca", "hc", "nu"];
    // The relevant keys minus "hc": used to drop the locale's -u-hc- extension when an
    // hour12 / hourCycle option overrides it (ResolveLocale only reflects a keyword whose
    // value actually came from the locale, not one supplied by an option).
    internal static readonly string[] DateTimeFormatRelevantKeysWithoutHourCycle = ["ca", "nu"];

    internal static string DropHourCycleExtension(string tag)
        => FilterUnicodeExtensionKeywords(tag, DateTimeFormatRelevantKeysWithoutHourCycle);
    internal static readonly string[] CollatorRelevantKeys = ["co", "kf", "kn"];

    // Rebuilds a BCP-47 tag keeping only the "-u-" extension keywords whose key is in
    // relevantKeys; attributes and irrelevant keys are dropped, and the whole "-u-"
    // sequence is removed when nothing relevant survives. Other singleton extensions
    // ("-t-", "-x-", …) are preserved.
    private static string FilterUnicodeExtensionKeywords(string tag, string[] relevantKeys)
    {
        if (string.IsNullOrEmpty(tag) || tag.IndexOf("-u-", StringComparison.OrdinalIgnoreCase) < 0)
            return tag;

        var parts = tag.Split('-');
        var u = -1;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length != 1)
                continue;
            // Stop scanning at the private-use singleton ("x"): everything beyond is
            // private-use, so a "u" inside that payload is not a Unicode extension.
            if (parts[i][0] == 'u' || parts[i][0] == 'U') { u = i; break; }
            if (parts[i][0] == 'x' || parts[i][0] == 'X') break;
        }

        if (u < 0)
            return tag;

        // Walk the "-u-" extension (up to the next singleton subtag or the end),
        // grouping subtags into keywords: a 2-char subtag is a key, longer subtags are
        // its type values (or leading attributes before the first key).
        var end = u + 1;
        var keywords = new List<(string Key, List<string> Types)>();
        while (end < parts.Length && parts[end].Length != 1)
        {
            if (parts[end].Length == 2)
                keywords.Add((parts[end].ToLowerInvariant(), new List<string>()));
            else if (keywords.Count > 0)
                keywords[^1].Types.Add(parts[end]);
            // (a subtag before the first key is an attribute — dropped)
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
            if (System.Array.IndexOf(relevantKeys, key) < 0)
                continue;

            if (!keptAny)
            {
                sb.Append("-u");
                keptAny = true;
            }

            sb.Append('-').Append(key);
            // CanonicalizeUnicodeLocaleId: a boolean-keyword value of "true" is the default and is
            // dropped (e.g. "kn-true" → "kn"), so resolvedOptions().locale carries just the key.
            if (types.Count == 1 && string.Equals(types[0], "true", StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var type in types)
                sb.Append('-').Append(type);
        }

        // Preserve any later singleton extensions ("-t-", "-x-", …) after the "-u-" run.
        for (var i = end; i < parts.Length; i++)
            sb.Append('-').Append(parts[i]);

        return sb.ToString();
    }

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

    // ResolveLocale's negotiation of the `nu` (numbering system) relevant key. Returns
    // the resolved numbering system and the resolved locale tag. Per spec: a supported
    // `-u-nu-` value is used and reflected in the locale; a `numberingSystem` option that
    // is supported and DIFFERENT overrides it and removes the `-u-nu-` from the locale; an
    // unsupported value (in either place) falls through to "latn" with `-u-nu-` removed.
    internal static (string NumberingSystem, string Locale) ResolveNumberingSystem(string localeTag, JSObject options)
        => ResolveNumberingSystem(localeTag, ReadNumberingSystemOption(options));

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

    // Reads (string-coerces and validates) the calendar option from the bag once,
    // returning null when absent. InitializeDateTimeFormat reads it right after
    // localeMatcher and ahead of numberingSystem; a value that is not a well-formed
    // Unicode BCP-47 extension type is a RangeError.
    internal static string ReadCalendarOption(JSObject options)
    {
        var value = options == null ? null : OptionString(options, KeyStrings.GetOrCreate("calendar"));
        if (value == null)
            return null;
        if (!LocaleId.IsUnicodeType(value))
            throw JSEngine.NewRangeError("Invalid calendar option");
        // ResolveLocale lowers an option's value by ASCII casing and replaces a deprecated calendar by its preferred one from
        // the BCP 47 data ("islamicc" -> "islamic-civil"), the shared library's (SJL-9.5, sub-area (a)).
        value = LocaleId.AsciiLowercase(value);
        return JSIntlData.LocaleData.TryExtensionType('u', "ca", value, out var preferred) ? preferred : value;
    }

    // Negotiates the resolved `nu` (numbering system) against the locale's `-u-nu-` extension, given the already-read
    // numberingSystem option value, for Intl.DateTimeFormat and Intl.DurationFormat until tasks 8.5 and 8.6 move them onto
    // the library's ResolveLocale: a supported `-u-nu-` value is used and reflected in the locale; a supported option that
    // differs overrides it and removes the `-u-nu-` from the locale; an unsupported value (in either place) falls through to
    // the locale's default with `-u-nu-` removed.
    // Revised 2026-11-08, at SJL-9.5 (task 8.4; decision D13, row L22): "supported" and the default are the data's, the nu
    // values NumberingSystemValues gives the locale, where the comment read "The default is CLDR's
    // (JSIntlDefaultNumberingSystems): "arab" for ar-EG, "arabext" for fa, "latn" for en or ar." and the supported set was
    // SupportedNumberingSystemsSorted.
    internal static (string NumberingSystem, string Locale) ResolveNumberingSystem(string localeTag, string optionValue)
    {
        var values = NumberingSystemValues(localeTag);
        var value = values[0];
        var reflectInLocale = false;

        var ext = GetUnicodeExtensionType(localeTag, "nu");
        if (ext != null && Contains(values, ext))
        {
            value = ext;
            reflectInLocale = true;
        }

        if (optionValue != null && Contains(values, optionValue) &&
            !string.Equals(optionValue, value, StringComparison.Ordinal))
        {
            value = optionValue;
            reflectInLocale = false;
        }

        var locale = reflectInLocale ? localeTag : RemoveUnicodeExtensionKeyword(localeTag, "nu");
        return (value, locale);

        static bool Contains(IReadOnlyList<string> list, string item)
        {
            foreach (var candidate in list)
            {
                if (string.Equals(candidate, item, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    // Negotiates the resolved `ca` (calendar) against the locale's `-u-ca-` extension, given the
    // already-read+canonicalized calendar option value, mirroring ResolveNumberingSystem. Per
    // ResolveLocale: a supported `-u-ca-` value is used and reflected in the locale; a supported
    // option that DIFFERS overrides it and drops the `-u-ca-`; an unsupported value (in either
    // place) falls through to the default "gregory" with `-u-ca-` removed. "Supported" here is the
    // set of available canonical calendars (so "iso8601" counts but "invalid" does not).
    internal static (string Calendar, string Locale) ResolveCalendar(string localeTag, string optionValue)
    {
        // The ResolveLocale `ca` negotiation runs over the full set of AVAILABLE canonical calendars,
        // so any valid identifier (including "hebrew"/"japanese") is kept in the resolved locale; only
        // an unavailable identifier (e.g. "invalid") falls back to "gregory" and drops the -u-ca-.
        var value = "gregory";
        var reflectInLocale = false;

        var ext = GetUnicodeExtensionType(localeTag, "ca");
        if (IsAvailableCalendar(ext))
        {
            value = ext;
            reflectInLocale = true;
        }

        if (optionValue != null && IsAvailableCalendar(optionValue) && !string.Equals(optionValue, value, StringComparison.Ordinal))
        {
            value = optionValue;
            reflectInLocale = false;
        }

        var locale = reflectInLocale ? localeTag : RemoveUnicodeExtensionKeyword(localeTag, "ca");
        return (value, locale);
    }

    // A calendar identifier this implementation lists among the available (canonical) calendars.
    internal static bool IsAvailableCalendar(string c)
        => c != null && System.Array.IndexOf(AvailableCanonicalCalendars, c) >= 0;

    // A calendar this formatter can actually project: the proleptic Gregorian calendars plus the
    // special calendars the formatting engine renders. An available-but-unrenderable calendar coming
    // from the locale's -u-ca- (e.g. "hebrew") is reported as "gregory", matching the gregorian output.
    internal static bool IsRenderableCalendar(string c)
        => c is "gregory" or "iso8601" || JSIntlDateTimeFormatEngine.IsSupportedCalendar(c);

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
    // whole "-u-" sequence when nothing else survives). Sibling of
    // FilterUnicodeExtensionKeywords, which keeps a set instead of removing one.
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

    // Regions that conventionally use a 12-hour clock; everything else defaults to the 24-hour cycle. DateTimeFormat's hand
    // table (row T10), which was Intl.Locale's until SJL-9.5's sub-area (a) moved getHourCycles onto CLDR's time data; it
    // leaves with DateTimeFormat's sub-area (d), task 8.5.
    private static readonly HashSet<string> TwelveHourRegions = new(StringComparer.Ordinal)
    {
        "US", "CA", "AU", "NZ", "IN", "PH", "PK", "BD", "EG", "MX", "CO", "SA",
    };

    // The CLDR-derived default hour cycle for a locale: "h12" for 12-hour regions (and
    // English without a region), "h23" otherwise. A coarse approximation of the CLDR
    // <hours> preference data, sufficient for the common cases.
    internal static string DefaultHourCycle(string tag)
    {
        var region = RegionSubtag(tag);
        if (region != null)
            return TwelveHourRegions.Contains(region) ? "h12" : "h23";

        return LanguageSubtag(tag) == "en" ? "h12" : "h23";
    }

    // CLDR <hours> preferred 12-hour cycle: "h" (h12) in almost every locale, but "K" (h11)
    // in the small set whose day-period clock starts at 0 — Japan (ja / JP). Used when the
    // hour12 option requests a 12-hour clock.
    internal static bool Prefers11HourCycle(string tag)
    {
        var region = RegionSubtag(tag);
        if (region != null)
            return region == "JP";
        return LanguageSubtag(tag) == "ja";
    }

    private static string LanguageSubtag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return null;

        var dash = tag.IndexOf('-');
        return (dash < 0 ? tag : tag.Substring(0, dash)).ToLowerInvariant();
    }

    private static string RegionSubtag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return null;

        var parts = tag.Split('-');
        var i = 1;
        if (i < parts.Length && parts[i].Length == 4 && IsAllAlphaSubtag(parts[i]))
            i++; // skip script

        if (i < parts.Length
            && ((parts[i].Length == 2 && IsAllAlphaSubtag(parts[i])) || (parts[i].Length == 3 && IsAllDigitSubtag(parts[i]))))
            return parts[i].ToUpperInvariant();

        return null;
    }

    private static bool IsAllAlphaSubtag(string s)
    {
        foreach (var c in s)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                return false;
        return s.Length > 0;
    }

    private static bool IsAllDigitSubtag(string s)
    {
        foreach (var c in s)
            if (c < '0' || c > '9')
                return false;
        return s.Length > 0;
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

public sealed class JSIntlDurationFormat : JSObject
{
    // Units in ECMA-402 processing order, with their singular NumberFormat unit name.
    private static readonly string[] Units =
        ["years", "months", "weeks", "days", "hours", "minutes", "seconds", "milliseconds", "microseconds", "nanoseconds"];
    private static readonly string[] SingularUnits =
        ["year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond"];

    private static readonly string[] DateStyles = ["long", "short", "narrow"];
    private static readonly string[] TimeStyles = ["long", "short", "narrow", "numeric", "2-digit"];
    private static readonly string[] SubSecondStyles = ["long", "short", "narrow", "numeric"];
    private static readonly string[] StyleValues = ["long", "short", "narrow", "digital"];
    private static readonly string[] DisplayValues = ["auto", "always"];

    private readonly string locale;
    private readonly string numberingSystem;       // null => NumberFormat default
    private readonly string style;                 // long | short | narrow | digital
    private readonly string[] unitStyle = new string[Units.Length];
    private readonly string[] unitDisplay = new string[Units.Length];
    private readonly int? fractionalDigits;

    private JSIntlDurationFormat() : base(CurrentPrototype()) { }

    public JSIntlDurationFormat(string locale, JSObject options) : this()
    {
        (numberingSystem, this.locale) = JSIntl.ResolveNumberingSystem(locale, options);
        style = JSIntl.GetOption(options, KeyStrings.GetOrCreate("style"), StyleValues, false, "short");

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

        // ECMA-402 GetDurationUnitOptions: once a unit is shown with "numeric"/"2-digit"
        // style, every following unit must also be "numeric"/"2-digit" — a "long"/"short"/
        // "narrow" style after one is a RangeError. A numeric minutes/seconds chained after
        // such a unit is shown as a zero-padded 2-digit value joined by the time separator
        // (e.g. 1:00:30).
        if (prevStyle is "numeric" or "2-digit")
        {
            if (resolved is not ("numeric" or "2-digit"))
                throw JSEngine.NewRangeError(
                    $"Invalid {unit} style \"{resolved}\" following a \"{prevStyle}\" unit");
            if (unit is "minutes" or "seconds")
                resolved = "2-digit";
        }

        var display = JSIntl.GetOption(options, KeyStrings.GetOrCreate(unit + "Display"), DisplayValues, false, displayDefault);
        return (resolved, display);
    }

    public static JSValue FormatPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.format called on incompatible receiver");

        var durationObject = ToDurationFormatRecord(a.Get1());
        return CreateString(self.Format(durationObject));
    }

    // Temporal.Duration.prototype.toLocaleString(locales, options): formats the duration with a
    // freshly-constructed Intl.DurationFormat, exactly as `new Intl.DurationFormat(locales,
    // options).format(duration)` would.
    internal static JSValue TemporalToLocaleString(JSValue duration, JSValue locales, JSValue options)
    {
        var args = new Arguments(JSUndefined.Value, locales, options ?? JSUndefined.Value);
        var optionsObject = JSIntl.ValidateConstructorArguments(
            "DurationFormat", in args, out var canonical, requireNew: false, coerceOptions: false);
        var locale = JSIntl.ResolveLocaleFromCanonical(canonical, JSIntl.NumberFormatRelevantKeys);
        var df = new JSIntlDurationFormat(locale, optionsObject);
        return CreateString(df.Format(ToDurationFormatRecord(duration)));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.formatToParts called on incompatible receiver");

        var durationObject = ToDurationFormatRecord(a.Get1());
        var parts = CreateArray();

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var unitKey = KeyStrings.GetOrCreate("unit");
        foreach (var (type, value, unit) in self.FormatToParts(durationObject))
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

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        void Put(string key, JSValue value) =>
            result.FastAddValue(KeyStrings.GetOrCreate(key), value, JSPropertyAttributes.EnumerableConfigurableValue);

        Put("locale", CreateString(self.locale ?? "en"));
        Put("numberingSystem", CreateString(self.numberingSystem ?? "latn"));
        Put("style", CreateString(self.style));
        for (var i = 0; i < Units.Length; i++)
        {
            Put(Units[i], CreateString(self.unitStyle[i]));
            Put(Units[i] + "Display", CreateString(self.unitDisplay[i]));
        }
        if (self.fractionalDigits is { } digits)
            Put("fractionalDigits", CreateNumber(digits));
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
            string fractionalExact = null;

            // Numeric seconds/milliseconds/microseconds fold the finer sub-second
            // units into a single fractional value when the next unit is numeric.
            if ((unit is "seconds" or "milliseconds" or "microseconds") && unitStyle[i + 1] == "numeric")
            {
                var exponent = unit == "seconds" ? 9 : unit == "milliseconds" ? 6 : 3;
                value = DurationToFractional(values, exponent);
                // The double is only kept for the display / sign checks below; the value is
                // formatted from its EXACT decimal string so e.g. 10000000 s + 1 ns renders as
                // "10000000.000000001" rather than the nearest double.
                fractionalExact = DurationToFractionalString(values, exponent);
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
                    // The negative sign is carried by the first displayed unit only when the duration
                    // is actually negative; a lone negative-zero field (DurationSign 0) must format as
                    // "+0", so normalize its negative zero away to avoid a spurious "-0".
                    if (value == 0)
                        value = anyNegative ? -0.0 : 0.0;
                }
                else
                {
                    signNever = true;
                }

                var numberParts = FormatNumberParts(SingularUnits[i], value, fractionalExact, unitStyleValue, combineFractional, signNever);
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

    // The exact decimal string of the folded sub-second value (DurationToFractional computed with
    // real arithmetic): the seconds/milliseconds/microseconds/nanoseconds fields are integers, so
    // their exact magnitudes combine over 10^exponent without the precision loss a double incurs.
    private static string DurationToFractionalString(double[] values, int exponent)
    {
        var total = new BigInteger(values[9]); // nanoseconds
        if (exponent >= 9)
            total += new BigInteger(values[6]) * 1_000_000_000;
        if (exponent >= 6)
            total += new BigInteger(values[7]) * 1_000_000;
        if (exponent >= 3)
            total += new BigInteger(values[8]) * 1_000;

        var negative = total.Sign < 0;
        var magnitude = BigInteger.Abs(total);
        var scale = BigInteger.Pow(10, exponent);
        var intPart = magnitude / scale;
        var fracPart = magnitude % scale;

        var s = intPart.ToString(CultureInfo.InvariantCulture);
        if (!fracPart.IsZero)
            s += "." + fracPart.ToString(CultureInfo.InvariantCulture).PadLeft(exponent, '0');
        return negative ? "-" + s : s;
    }

    private List<(string type, string value)> FormatNumberParts(string singularUnit, double value, string exactValue, string unitStyleValue, bool fractional, bool signNever)
    {
        var options = new JSObject();
        void Set(string key, JSValue v) => options.FastAddValue(KeyStrings.GetOrCreate(key), v, JSPropertyAttributes.EnumerableConfigurableValue);

        if (numberingSystem != null)
            Set("numberingSystem", CreateString(numberingSystem));

        if (unitStyleValue == "2-digit")
            Set("minimumIntegerDigits", CreateNumber(2));

        if (unitStyleValue is not ("numeric" or "2-digit"))
        {
            Set("style", CreateString("unit"));
            Set("unit", CreateString(singularUnit));
            Set("unitDisplay", CreateString(unitStyleValue));
        }
        else
        {
            Set("useGrouping", BooleanFalse);
        }

        if (fractional)
        {
            Set("maximumFractionDigits", CreateNumber(fractionalDigits ?? 9));
            Set("minimumFractionDigits", CreateNumber(fractionalDigits ?? 0));
            Set("roundingMode", CreateString("trunc"));
        }

        if (signNever)
            Set("signDisplay", CreateString("never"));

        var args = new Arguments(JSUndefined.Value, CreateString(locale), options);
        var nf = new JSIntlNumberFormat(in args);
        // A folded fractional value is formatted from its exact decimal string (parsed as an Intl
        // mathematical value) so no sub-second precision is lost to the double round-trip.
        var operand = exactValue != null ? CreateString(exactValue) : CreateNumber(value);
        return nf.ComputeFormatParts(operand);
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.DurationFormat");

    // %Intl.DurationFormat.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.DurationFormat` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);

    // ToDurationRecord(input): a String is parsed as an ISO 8601 / Temporal duration (an invalid string
    // is a RangeError), an object's unit fields are read and validated directly, and any other type is a
    // TypeError. Returns the object whose unit getters the formatter then reads (a parsed string yields a
    // Temporal.Duration, which exposes the same year/month/…/nanosecond properties).
    private static JSObject ToDurationFormatRecord(JSValue duration)
    {
        if (duration is JSTemporalDuration temporalDuration)
        {
            // A Temporal.Duration is read from its internal slots, never its
            // Temporal.Duration.prototype getters (which user code may have replaced) —
            // matching ToTemporalDuration, which clones a Temporal.Duration from its slots.
            // Snapshot the slots into a plain record so the formatter's field reads observe
            // data, not installed accessors.
            var slots = new[]
            {
                temporalDuration.years, temporalDuration.months, temporalDuration.weeks, temporalDuration.days,
                temporalDuration.hours, temporalDuration.minutes, temporalDuration.seconds,
                temporalDuration.milliseconds, temporalDuration.microseconds, temporalDuration.nanoseconds,
            };
            var record = new JSObject();
            for (var i = 0; i < Units.Length; i++)
                record.FastAddValue(KeyStrings.GetOrCreate(Units[i]), CreateNumber(slots[i]), JSPropertyAttributes.EnumerableConfigurableValue);
            return record;
        }

        if (duration.IsString)
            return (JSObject)JSTemporalDuration.From(new Arguments(JSUndefined.Value, duration));

        if (duration is not JSObject durationObject)
            throw JSEngine.NewTypeError("Duration argument must be an object or string");

        ValidateDurationArgument(durationObject);
        return durationObject;
    }

    private static void ValidateDurationArgument(JSObject durationObject)
    {
        var any = false;
        var hasPositive = false;
        var hasNegative = false;
        // Field values in Units order (years, months, weeks, days, hours, minutes,
        // seconds, milliseconds, microseconds, nanoseconds); absent fields stay 0.
        var values = new double[Units.Length];
        for (var i = 0; i < Units.Length; i++)
        {
            var unit = Units[i];
            var value = durationObject[KeyStrings.GetOrCreate(unit)];
            // A field left undefined is absent; reading by key also runs any getter.
            if (value == null || value.IsUndefined)
                continue;

            any = true;
            // ToIntegerIfIntegral runs ToNumber, which throws a TypeError for a BigInt.
            if (value.IsBigInt)
                throw JSEngine.NewTypeError($"Cannot convert a BigInt duration value for {unit}");
            var numericValue = value.DoubleValue;
            // ToIntegerIfIntegral: a non-finite or non-integral field is a RangeError.
            if (double.IsNaN(numericValue) || double.IsInfinity(numericValue) || Math.Truncate(numericValue) != numericValue)
                throw JSEngine.NewRangeError($"Invalid duration value for {unit}");

            values[i] = numericValue;
            hasPositive |= numericValue > 0;
            hasNegative |= numericValue < 0;
        }

        // ToDurationRecord step: if no duration field was provided, throw a TypeError.
        if (!any)
            throw JSEngine.NewTypeError("Duration must specify at least one field");

        if (hasPositive && hasNegative)
            throw JSEngine.NewRangeError("Invalid duration: inconsistent sign");

        // IsValidDuration (Temporal §7.5.x), invoked by ToDurationRecord: the years,
        // months and weeks fields must each be below 2^32 in magnitude, and the combined
        // time portion (days through nanoseconds) must total under 2^53 seconds. The total
        // is computed with exact integer arithmetic — every field is integral here, so the
        // real value of each double is exact — to avoid the rounding that floating-point
        // multiplication by 10^-3/10^-6/10^-9 would introduce near the 2^53 boundary.
        const double twoPow32 = 4294967296d; // 2^32
        for (var i = 0; i < 3; i++)
        {
            if (Math.Abs(values[i]) >= twoPow32)
                throw JSEngine.NewRangeError($"Duration value for {Units[i]} is out of range");
        }

        // Nanoseconds per: day, hour, minute, second, millisecond, microsecond, nanosecond.
        ReadOnlySpan<long> nanosPerUnit =
            [86_400_000_000_000L, 3_600_000_000_000L, 60_000_000_000L, 1_000_000_000L, 1_000_000L, 1_000L, 1L];
        var totalNanoseconds = BigInteger.Zero;
        for (var i = 0; i < nanosPerUnit.Length; i++)
            totalNanoseconds += new BigInteger(values[i + 3]) * nanosPerUnit[i];

        // 2^53 seconds expressed in nanoseconds (9007199254740992 × 10^9).
        var maxNanoseconds = new BigInteger(9007199254740992L) * 1_000_000_000L;
        if (BigInteger.Abs(totalNanoseconds) >= maxNanoseconds)
            throw JSEngine.NewRangeError("Duration time total is out of range");
    }
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

    // Like formatToParts, but returns ("element"|"literal", value) tuples. Used by
    // Intl.DurationFormat to interleave its own per-unit parts with the list's
    // separators (the "element" entries are replaced by the caller).
    internal List<(string type, string value)> FormatPartsForUnits(List<string> items)
    {
        var parts = Formatter.FormatToParts(items, null);
        var list = new List<(string, string)>(parts.Count);
        foreach (var part in parts)
            list.Add((part.Type, part.Value));
        return list;
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

    // The (type, value) parts of a value, for Intl.DurationFormat until its own move (task 8.6).
    // Revised 2026-11-08, at SJL-9.5 (task 8.4): Intl.RelativeTimeFormat formats through the shared library's
    // RelativeTimeFormatter and no longer calls this; the comment read "for Intl.DurationFormat and
    // Intl.RelativeTimeFormat's callers until their own moves (task 8.6)".
    internal List<(string type, string value)> ComputeFormatParts(JSValue value)
        => JSIntl.NumberParts(Formatter.FormatToParts(JSIntl.ToIntlMathematicalValue(value), null));

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
    private static readonly KeyString CalendarKey = KeyStrings.GetOrCreate("calendar");
    private static readonly KeyString NumberingSystemKey = KeyStrings.GetOrCreate("numberingSystem");
    private static readonly KeyString WeekdayKey = KeyStrings.GetOrCreate("weekday");
    private static readonly KeyString EraKey = KeyStrings.GetOrCreate("era");
    private static readonly KeyString TimeZoneNameKey = KeyStrings.GetOrCreate("timeZoneName");
    private static readonly KeyString FormatMatcherKey = KeyStrings.GetOrCreate("formatMatcher");

    // The sanctioned value sets for the date/time option getters (CreateDateTimeFormat).
    private static readonly string[] NarrowShortLong = ["narrow", "short", "long"];
    private static readonly string[] NumericTwoDigit = ["2-digit", "numeric"];
    private static readonly string[] MonthValues = ["2-digit", "numeric", "narrow", "short", "long"];
    private static readonly string[] HourCycleValues = ["h11", "h12", "h23", "h24"];
    private static readonly string[] TimeZoneNameValues =
        ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"];
    private static readonly string[] FormatMatcherValues = ["basic", "best fit"];
    private readonly CultureInfo locale;
    private readonly string localeTag;
    private readonly string numberingSystem;
    private JSObject options;

    // dateStyle / timeStyle are GetOption-coerced (to a string) and validated once at
    // construction, then reported verbatim by resolvedOptions. Reading them from the
    // raw user options object would surface the original (possibly non-string) value
    // — e.g. `{ dateStyle: { toString() { return "full"; } } }` must resolve to the
    // string "full", not the object.
    internal static readonly string[] DateTimeStyleValues = ["full", "long", "medium", "short"];
    private readonly string dateStyle;
    private readonly string timeStyle;

    // True when the constructor supplied the year/month/day = "numeric" defaults because the user
    // specified no date/time component and no dateStyle/timeStyle (ToDateTimeOptions, defaults
    // "date"). The defaults are stored on the snapshot so resolvedOptions reports them, but the
    // formatter must still treat itself as "default" (RequestedFields) so a Temporal value formats
    // its own default fields rather than being constrained to the injected year/month/day.
    private readonly bool dateDefaultsApplied;

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
        var hc = ResolveHourCycle();
        return hc == "h11" || hc == "h12";
    }

    // Resolves the hourCycle reported by resolvedOptions (CreateDateTimeFormat). The
    // hour12 option wins, mapping to h11/h12 (true) or h23/h24 (false) depending on
    // whether the locale default is a "0-based" cycle; otherwise an explicit hourCycle
    // option wins, then the locale's -u-hc- extension, then the locale default.
    // Kept as the single source of truth so ResolveHour12 never disagrees.
    private string ResolveHourCycle()
    {
        var hcDefault = JSIntl.DefaultHourCycle(localeTag);

        var hour12 = options?[Hour12Key];
        if (hour12 != null && !hour12.IsUndefined)
        {
            // hour12 picks the locale's preferred clock of the requested kind from CLDR's
            // <hours> data: hour12 true selects the 12-hour cycle, which is "h12" everywhere
            // except the few locales (Japanese) whose preferred 12-hour cycle is "h11"; hour12
            // false selects "h23" (the 24-hour cycle observed by other engines).
            if (hour12.BooleanValue)
                return JSIntl.Prefers11HourCycle(localeTag) ? "h11" : "h12";
            return "h23";
        }

        var option = OptionString(HourCycleKey);
        if (option != null)
            return option;

        var ext = JSIntl.GetUnicodeExtensionType(localeTag, "hc");
        if (!string.IsNullOrEmpty(ext) && (ext == "h11" || ext == "h12" || ext == "h23" || ext == "h24"))
            return ext;

        return hcDefault;
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
            dateStyle: dateStyle,
            timeStyle: timeStyle,
            hour12: ResolveHour12(),
            calendar: ResolvedCalendar(),
            hasWeekday: OptionString(KeyStrings.GetOrCreate("weekday")) != null,
            weekdayStyle: OptionString(KeyStrings.GetOrCreate("weekday")),
            hasTimeZoneName: OptionString(KeyStrings.GetOrCreate("timeZoneName")) != null,
            hourCycle: ResolveHourCycle(),
            hasEra: OptionString(KeyStrings.GetOrCreate("era")) != null,
            eraStyle: OptionString(KeyStrings.GetOrCreate("era")),
            hourStyle: OptionString(HourKey),
            minuteStyle: OptionString(MinuteKey),
            secondStyle: OptionString(SecondKey));

    // The resolved calendar: per ECMA-402 InitializeDateTimeFormat the `calendar` OPTION
    // takes precedence over the locale tag's -u-ca- value, then -u-ca-, then the locale's
    // default ("gregory"). An unsupported value (one this engine cannot project the year /
    // era through) falls back to "gregory" so formatting still produces a usable result.
    internal string ResolvedCalendar()
    {
        var ca = ResolvedCalendarId();
        return JSIntlDateTimeFormatEngine.IsSupportedCalendar(ca) ? ca : "gregory";
    }

    // The resolved calendar IDENTIFIER (dateTimeFormat.[[Calendar]]): the `calendar` option, then the
    // locale tag's -u-ca- value, then the locale default ("gregory"). Unlike ResolvedCalendar() it does
    // NOT collapse an identifier the formatting engine cannot project (e.g. "iso8601", which it renders
    // as proleptic Gregorian) down to "gregory": the unchanged identifier is what a Temporal value's
    // calendar must be checked against (CheckTemporalCalendar) and what resolvedOptions reports.
    internal string ResolvedCalendarId()
    {
        // An explicit `calendar` option that names an available calendar is the resolved [[Calendar]]
        // and is reported verbatim (test262 resolvedOptions/calendar — every available calendar,
        // including "hebrew"/"japanese", round-trips). Otherwise the resolved locale's negotiated
        // -u-ca- value is reported, collapsed to "gregory" when this engine cannot render it (so a
        // bare `en-u-ca-hebrew` reports "gregory", matching the gregorian formatting).
        var option = OptionString(CalendarKey);
        if (option != null && JSIntl.IsAvailableCalendar(option))
            return option;
        var ext = UnicodeKeyword(localeTag, "ca");
        return JSIntl.IsRenderableCalendar(ext) ? ext : "gregory";
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
                // A Unicode keyword value may span several subtags, e.g. "ca" → "islamic-tbla"; collect
                // all following type subtags (length 3–8) until the next key (length 2) or extension end.
                var value = new List<string>();
                for (var j = i + 1; j < parts.Length && parts[j].Length > 2; j++)
                    value.Add(parts[j].ToLowerInvariant());
                return value.Count > 0 ? string.Join("-", value) : null;
            }
        }
        return null;
    }

    private JSIntlDateTimeFormatEngine.Fields ResolveFields(double clipped)
    {
        var tz = OptionString(TimeZoneKey);
        // The display name is always computed so it is available whenever the resolved pattern emits a
        // zone token — the explicit timeZoneName option, or a long/full time style. The explicit option
        // wins; otherwise the time style chooses the width: timeStyle "full" uses the long zone name
        // ("Coordinated Universal Time") and "long" uses the short name ("UTC").
        var style = OptionString(KeyStrings.GetOrCreate("timeZoneName"))
            ?? (timeStyle == "full" ? "long" : "short");
        var zoneName = JSIntlDateTimeFormatEngine.FormatTimeZoneName(localeTag, tz, style, clipped);
        var wall = JSIntlDateTimeFormatEngine.ToZone(clipped, tz);
        var dayPeriod = DayPeriodName((int)EcmaTime.HourFromTime(wall), (int)EcmaTime.MinFromTime(wall));
        return new(wall, zoneName, dayPeriod);
    }

    // ── Temporal integration (FormatDateTimePattern / HandleDateTimeValue) ───────────────────────
    // The field categories a Temporal type can supply to Intl.DateTimeFormat. A plain type that lacks
    // a category drops the formatter's request for it; a formatter that requests *only* unsupported
    // categories has no overlap and is a TypeError. A ZonedDateTime cannot be formatted directly.
    [Flags]
    private enum TemporalFields
    {
        None = 0, Era = 1, Year = 2, Month = 4, Day = 8, Weekday = 16,
        Hour = 32, Minute = 64, Second = 128, FractionalSecond = 256, DayPeriod = 512, TimeZoneName = 1024,
        Date = Era | Year | Month | Day | Weekday,
        Time = Hour | Minute | Second | FractionalSecond | DayPeriod,
    }

    // True when the value is any Temporal date/time object this formatter handles specially.
    private static bool IsTemporalDateTime(JSValue value)
        => value is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalPlainTime
            or JSTemporalPlainYearMonth or JSTemporalPlainMonthDay or JSTemporalInstant
            or JSTemporalZonedDateTime;

    private bool Has(KeyString key) => options != null && options[key] is { IsUndefined: false };

    // The fields the formatter is *requesting*, plus whether it was created without any explicit
    // date/time component or style (in which case a Temporal type fills in its own defaults).
    private (TemporalFields requested, bool isDefault) RequestedFields()
    {
        var f = TemporalFields.None;
        if (Has(YearKey)) f |= TemporalFields.Year;
        if (Has(MonthKey)) f |= TemporalFields.Month;
        if (Has(DayKey)) f |= TemporalFields.Day;
        if (Has(KeyStrings.GetOrCreate("weekday"))) f |= TemporalFields.Weekday;
        if (Has(KeyStrings.GetOrCreate("era"))) f |= TemporalFields.Era;
        if (Has(HourKey)) f |= TemporalFields.Hour;
        if (Has(MinuteKey)) f |= TemporalFields.Minute;
        if (Has(SecondKey)) f |= TemporalFields.Second;
        if (Has(FractionalSecondDigitsKey)) f |= TemporalFields.FractionalSecond;
        if (Has(DayPeriodKey)) f |= TemporalFields.DayPeriod;
        if (Has(KeyStrings.GetOrCreate("timeZoneName"))) f |= TemporalFields.TimeZoneName;

        // era and timeZoneName are *supplementary*: they are not date/time components for the
        // defaulting decision, so a formatter requesting only those is still "default" (a plain type
        // formats its defaults, dropping the unsupported supplementary field, instead of failing the
        // no-overlap check). They are re-added to a supporting type in EffectiveTemporalFields.
        var hadComponents = (f & ~SupplementaryFields) != TemporalFields.None;

        // Expand dateStyle / timeStyle into the component fields they render (en/CLDR layout, matching
        // the engine's hardcoded styles). timeStyle long/full also implies a time-zone name.
        if (dateStyle != null)
            f |= TemporalFields.Year | TemporalFields.Month | TemporalFields.Day
                | (dateStyle == "full" ? TemporalFields.Weekday : TemporalFields.None);
        if (timeStyle != null)
        {
            f |= TemporalFields.Hour | TemporalFields.Minute;
            if (timeStyle is "medium" or "long" or "full") f |= TemporalFields.Second;
            if (timeStyle is "long" or "full") f |= TemporalFields.TimeZoneName;
        }

        // A formatter that only carries the injected year/month/day defaults (no user-specified
        // component or style) is still "default": a Temporal value formats its own default fields
        // rather than being constrained to year/month/day.
        return (f, dateDefaultsApplied || (!hadComponents && dateStyle == null && timeStyle == null));
    }

    // Temporal.X.prototype.toLocaleString: create a DateTimeFormat for the locale/options and format
    // the Temporal value through the same HandleDateTimeValue path (so toLocaleString and
    // DateTimeFormat.prototype.format agree). An optional default time zone is injected when absent
    // (used by ZonedDateTime, which formats in its own zone); a conflicting one is a RangeError.
    private static readonly string[] DateTimeComponentKeys =
    [
        "weekday", "era", "year", "month", "day", "dayPeriod",
        "hour", "minute", "second", "fractionalSecondDigits", "timeZoneName",
    ];

    private static readonly string[] DateTimeFormatOptionKeys =
    [
        "localeMatcher", "calendar", "numberingSystem", "hour12", "hourCycle", "timeZone",
        "weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second",
        "fractionalSecondDigits", "timeZoneName", "formatMatcher", "dateStyle", "timeStyle",
    ];

    public static JSValue TemporalToLocaleString(JSValue temporal, JSValue locales, JSValue options, string defaultTimeZone = null)
    {
        if (defaultTimeZone != null)
        {
            if (options != null && !options.IsNullOrUndefined && options is not JSObject)
                throw JSEngine.NewTypeError("Temporal toLocaleString options must be an object or undefined");

            var resolved = new JSObject();
            if (options is JSObject src)
                foreach (var name in DateTimeFormatOptionKeys)
                {
                    var key = KeyStrings.GetOrCreate(name);
                    var v = src[key];
                    if (!v.IsUndefined) resolved[key] = v;
                }

            // ZonedDateTime formats in its own zone; supplying any timeZone option (even a matching
            // one) is a TypeError.
            if (!resolved[TimeZoneKey].IsUndefined)
                throw JSEngine.NewTypeError(
                    "Temporal.ZonedDateTime.toLocaleString: the timeZone option is not allowed");
            resolved[TimeZoneKey] = CreateString(defaultTimeZone);
            options = resolved;
        }

        var dtf = new JSIntlDateTimeFormat(new Arguments(JSUndefined.Value, locales, options ?? JSUndefined.Value));
        return new JSString(JSIntlDateTimeFormatEngine.PartsToString(dtf.FormatTemporalToParts(temporal, zonedAllowed: true, enforceStyle: true)));
    }

    // The date/time component options whose presence suppresses defaulting (era / timeZoneName are
    // supplementary and do not count).
    private static readonly string[] DateTimeNeedsDefaultsKeys =
    [
        "weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits",
    ];

    // ToDateTimeOptions(options, "any", "all"): when no date/time component or style is requested, the
    // default is the full date+time. Used by Date.prototype.toLocaleString (the legacy [[Call]] uses
    // defaults "all", unlike the Intl.DateTimeFormat constructor which uses "date"). Returns the
    // options unchanged when defaults are not needed or it is not an object.
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
    // <paramref name="defaults"/> fields ("date" / "time" / "all" / "any"). Date.prototype's legacy
    // toLocaleString / toLocaleDateString / toLocaleTimeString construct an Intl.DateTimeFormat with
    // ToDateTimeOptions(options, "any"|"date"|"time", "all"|"date"|"time") respectively, which is why
    // their default output (and their option validation) must match Intl.DateTimeFormat exactly.
    // Returns a fresh options object carrying the supplied options' properties plus the defaulted
    // numeric components; throws a TypeError for the date+timeStyle / time+dateStyle conflicts.
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

    // The per-type formatting metadata: supported fields, default fields, an identity for same-type
    // range checks, the calendar, and the wall-clock (or zone-projected) Fields.
    private readonly struct TemporalMeta
    {
        public readonly TemporalFields Mask, Defaults;
        public readonly string Kind, CalendarId;
        public readonly JSIntlDateTimeFormatEngine.Fields Fields;
        public TemporalMeta(TemporalFields mask, TemporalFields defaults, string kind, string calendarId, JSIntlDateTimeFormatEngine.Fields fields)
        { Mask = mask; Defaults = defaults; Kind = kind; CalendarId = calendarId; Fields = fields; }
    }

    private const TemporalFields DateDefaults = TemporalFields.Year | TemporalFields.Month | TemporalFields.Day;
    private const TemporalFields TimeDefaults = TemporalFields.Hour | TemporalFields.Minute | TemporalFields.Second;
    // Fields that don't participate in the overlap/default decision; added back if the type supports them.
    private const TemporalFields SupplementaryFields = TemporalFields.Era | TemporalFields.TimeZoneName;

    // HandleDateTimeValue: classify a Temporal value. ZonedDateTime is a TypeError for
    // DateTimeFormat.format, but toLocaleString (zonedAllowed) formats it in its own zone, with the
    // zone name included by default.
    private TemporalMeta ClassifyTemporal(JSValue value, bool zonedAllowed) => value switch
    {
        JSTemporalZonedDateTime zdt => zonedAllowed
            ? new TemporalMeta(TemporalFields.Date | TemporalFields.Time | TemporalFields.TimeZoneName,
                DateDefaults | TimeDefaults | TemporalFields.TimeZoneName, "zoneddatetime", zdt.calendarId,
                ResolveFields((double)(zdt.epochNanoseconds / 1_000_000)))
            : throw JSEngine.NewTypeError(
                "Intl.DateTimeFormat: cannot format a Temporal.ZonedDateTime; use zonedDateTime.toLocaleString() instead"),
        JSTemporalPlainDate d => new TemporalMeta(TemporalFields.Date, DateDefaults, "date", d.calendarId,
            new JSIntlDateTimeFormatEngine.Fields(d.isoYear, d.isoMonth, d.isoDay, 0, 0, 0, 0)),
        JSTemporalPlainDateTime dt => new TemporalMeta(TemporalFields.Date | TemporalFields.Time, DateDefaults | TimeDefaults, "datetime", dt.calendarId,
            new JSIntlDateTimeFormatEngine.Fields(dt.isoYear, dt.isoMonth, dt.isoDay, dt.hour, dt.minute, dt.second, dt.millisecond, dayPeriod: DayPeriodName(dt.hour, dt.minute))),
        JSTemporalPlainTime t => new TemporalMeta(TemporalFields.Time, TimeDefaults, "time", "iso8601",
            new JSIntlDateTimeFormatEngine.Fields(1970, 1, 1, t.hour, t.minute, t.second, t.millisecond, dayPeriod: DayPeriodName(t.hour, t.minute))),
        JSTemporalPlainYearMonth ym => new TemporalMeta(TemporalFields.Era | TemporalFields.Year | TemporalFields.Month, TemporalFields.Year | TemporalFields.Month, "yearmonth", ym.calendarId,
            new JSIntlDateTimeFormatEngine.Fields(ym.isoYear, ym.isoMonth, ym.referenceISODay, 0, 0, 0, 0)),
        JSTemporalPlainMonthDay md => new TemporalMeta(TemporalFields.Month | TemporalFields.Day, TemporalFields.Month | TemporalFields.Day, "monthday", md.calendarId,
            new JSIntlDateTimeFormatEngine.Fields(md.referenceISOYear, md.isoMonth, md.isoDay, 0, 0, 0, 0)),
        JSTemporalInstant inst => new TemporalMeta(TemporalFields.Date | TemporalFields.Time | TemporalFields.TimeZoneName, DateDefaults | TimeDefaults, "instant", "iso8601",
            ResolveFields((double)(inst.epochNanoseconds / 1_000_000))),
        _ => throw JSEngine.NewTypeError("Intl.DateTimeFormat: unsupported Temporal value"),
    };

    // The Temporal value's calendar must be compatible with the formatter's resolved calendar.
    // For PlainDate / PlainDateTime / Instant / ZonedDateTime the iso8601 calendar is compatible with
    // any calendar (HandleDateTimeTemporalDate's iso8601 exception); for PlainYearMonth / PlainMonthDay
    // (exact = true) the calendars must be identical — even an iso8601 instance is a mismatch.
    private void CheckTemporalCalendar(string calendarId, bool exact)
    {
        var formatterCalendar = ResolvedCalendarId();
        var compatible = exact
            ? calendarId == formatterCalendar
            : calendarId == "iso8601" || calendarId == formatterCalendar;
        if (!compatible)
            throw JSEngine.NewRangeError(
                $"Intl.DateTimeFormat: calendar \"{calendarId}\" does not match the formatter calendar \"{formatterCalendar}\"");
    }

    // PlainYearMonth / PlainMonthDay require an exact calendar match (no iso8601 exception).
    private static bool RequiresExactCalendar(string kind) => kind is "yearmonth" or "monthday";

    // The effective fields shared by both endpoints of a format/formatRange, or a TypeError when the
    // formatter and the Temporal type have no field in common.
    private TemporalFields EffectiveTemporalFields(in TemporalMeta meta, bool enforceStyle)
    {
        // toLocaleString builds the formatter with a type-specific "required", so an incompatible
        // whole style is a TypeError at that point: a date-only type (PlainDate / PlainYearMonth /
        // PlainMonthDay) rejects timeStyle, and the time-only PlainTime rejects dateStyle. Direct
        // DateTimeFormat.format uses required "any" and instead just drops the unsupported fields.
        if (enforceStyle)
        {
            if (timeStyle != null && (meta.Mask & TemporalFields.Time) == 0)
                throw JSEngine.NewTypeError("Intl.DateTimeFormat: timeStyle is not supported for this Temporal type");
            if (dateStyle != null && (meta.Mask & TemporalFields.Date) == 0)
                throw JSEngine.NewTypeError("Intl.DateTimeFormat: dateStyle is not supported for this Temporal type");
        }

        var (requested, isDefault) = RequestedFields();
        var effective = isDefault ? meta.Defaults : requested & meta.Mask;

        // An explicit supplementary field (era / timeZoneName) is honoured by a type that supports it
        // even when the formatter is otherwise default; it never counts toward the overlap check.
        effective |= requested & SupplementaryFields & meta.Mask;

        if (effective == TemporalFields.None)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat: the format options and the Temporal type have no fields in common");
        return effective;
    }

    // FormatDateTimePattern: build the parts for a Temporal value, or throw TypeError/RangeError.
    private List<JSIntlDateTimeFormatEngine.Part> FormatTemporalToParts(JSValue value, bool zonedAllowed = false, bool enforceStyle = false)
    {
        var meta = ClassifyTemporal(value, zonedAllowed);
        // Resolve the effective fields first: an incompatible whole style (e.g. timeStyle on a
        // date-only type) is a TypeError and must be reported ahead of a calendar mismatch
        // RangeError, so `PlainMonthDay.from('01-01').toLocaleString('en', {timeStyle:'short'})`
        // throws the TypeError rather than the iso8601-vs-locale-default calendar RangeError.
        var pattern = ResolveTemporalPattern(EffectiveTemporalFields(in meta, enforceStyle));
        CheckTemporalCalendar(meta.CalendarId, RequiresExactCalendar(meta.Kind));
        var fields = meta.Fields;
        // The numbering system's digits, as a Date's parts and FormatTemporalRangeToParts take them.
        return MapNumbering(JSIntlDateTimeFormatEngine.FormatToParts(pattern, in fields, FractionalSecondDigits(), null));
    }

    // FormatDateTimeRange with two Temporal endpoints: both must be the same Temporal type and share a
    // calendar (else TypeError / RangeError), then the interval is formatted with one shared pattern.
    private List<JSIntlDateTimeFormatEngine.Part> FormatTemporalRangeToParts(JSValue startValue, JSValue endValue)
    {
        var start = ClassifyTemporal(startValue, zonedAllowed: false);
        var end = ClassifyTemporal(endValue, zonedAllowed: false);
        if (start.Kind != end.Kind)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.formatRange: both arguments must be the same Temporal type");
        if (start.CalendarId != end.CalendarId)
            throw JSEngine.NewRangeError("Intl.DateTimeFormat.formatRange: the two arguments must have the same calendar");
        CheckTemporalCalendar(start.CalendarId, RequiresExactCalendar(start.Kind));
        var pattern = ResolveTemporalPattern(EffectiveTemporalFields(in start, enforceStyle: false));
        var startFields = start.Fields;
        var endFields = end.Fields;
        return MapNumbering(JSIntlDateTimeFormatEngine.FormatRangeToParts(pattern, in startFields, in endFields, FractionalSecondDigits()));
    }

    // Builds an engine pattern from the effective Temporal fields, projecting each component's width
    // from the formatter's options (numeric by default). dateStyle/timeStyle are already expanded
    // into fields, so the component-based builder is used (clean field separators).
    private JSIntlDateTimeFormatEngine.Pattern ResolveTemporalPattern(TemporalFields f)
        => JSIntlDateTimeFormatEngine.ResolvePattern(
            localeTag: localeTag,
            hasYear: f.HasFlag(TemporalFields.Year), yearStyle: f.HasFlag(TemporalFields.Year) ? (OptionString(YearKey) ?? (dateStyle == "short" ? "2-digit" : "numeric")) : null,
            hasMonth: f.HasFlag(TemporalFields.Month), monthStyle: f.HasFlag(TemporalFields.Month) ? (OptionString(MonthKey) ?? StyleMonthWidth()) : null,
            hasDay: f.HasFlag(TemporalFields.Day), dayStyle: f.HasFlag(TemporalFields.Day) ? (OptionString(DayKey) ?? "numeric") : null,
            hasHour: f.HasFlag(TemporalFields.Hour), hasMinute: f.HasFlag(TemporalFields.Minute), hasSecond: f.HasFlag(TemporalFields.Second),
            fractionalSecondDigits: f.HasFlag(TemporalFields.FractionalSecond) ? FractionalSecondDigits() : 0,
            hasDayPeriodField: f.HasFlag(TemporalFields.DayPeriod),
            dateStyle: null, timeStyle: null,
            hour12: ResolveHour12(),
            calendar: ResolvedCalendar(),
            hasWeekday: f.HasFlag(TemporalFields.Weekday),
            weekdayStyle: OptionString(KeyStrings.GetOrCreate("weekday")) ?? (dateStyle == "full" ? "long" : null),
            hasTimeZoneName: f.HasFlag(TemporalFields.TimeZoneName),
            hourCycle: ResolveHourCycle(),
            hasEra: f.HasFlag(TemporalFields.Era),
            eraStyle: OptionString(KeyStrings.GetOrCreate("era")));

    // The month width implied by dateStyle when no explicit month option is present.
    private string StyleMonthWidth() => dateStyle switch
    {
        "full" or "long" => "long",
        "medium" => "short",
        _ => "numeric", // short / null
    };

    private static JSValue PartsArray(System.Collections.Generic.List<JSIntlDateTimeFormatEngine.Part> parts)
    {
        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var sourceKey = KeyStrings.GetOrCreate("source");
        var array = CreateArray();
        foreach (var p in parts)
        {
            var obj = new JSObject();
            obj[typeKey] = CreateString(p.Type);
            obj[valueKey] = CreateString(p.Value);
            if (p.Source != null)
                obj[sourceKey] = CreateString(p.Source);
            array.AddArrayItem(obj);
        }
        return array;
    }

    public static JSIntlDateTimeFormat Get(CultureInfo culture)
        => formats.GetOrAdd(culture.Name, static key => new JSIntlDateTimeFormat(CultureInfo.GetCultureInfo(key)));

    public JSValue Format(in Arguments a)
    {
        if (a.Length > 0 && a[0] != null && IsTemporalDateTime(a[0]))
            return new JSString(JSIntlDateTimeFormatEngine.PartsToString(FormatTemporalToParts(a[0])));

        var value = a.Length == 0 || a[0] == null || a[0].IsUndefined
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : a.Get1().DoubleValue;
        var clipped = EcmaTime.TimeClip(value);
        if (double.IsNaN(clipped))
            throw JSEngine.NewRangeError("Invalid time value");

        if (SupportsDayPeriod())
        {
            var (h, m) = WallHourMinute(clipped);
            var dayPeriod = DayPeriodNameForStyle(h, m, DayPeriodStyle());
            if (UsesHourFormatting())
                return new JSString($"{FormatEnglishHour(h)} {dayPeriod}");

            return new JSString(dayPeriod);
        }

        var pattern = ResolveEnginePattern();
        var fields = ResolveFields(clipped);
        var parts = JSIntlDateTimeFormatEngine.FormatToParts(pattern, in fields, FractionalSecondDigits(), "literal");
        return new JSString(JSIntlDateTimeFormatEngine.PartsToString(MapNumbering(parts)));
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
        return CreateString(sb.ToString());
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

        // ToDateTimeFormattable on both endpoints, in argument order: a Temporal object is
        // kept as-is, anything else is ToNumber-coerced now (observably running its
        // valueOf/toString). This happens for BOTH arguments before the same-kind check —
        // PartitionDateTimeRangePattern must not report a different-kind pair before the
        // arguments have been coerced (and TimeClip(NaN) must not throw a RangeError ahead
        // of that TypeError).
        var startTemporal = IsTemporalDateTime(startArg);
        var endTemporal = IsTemporalDateTime(endArg);
        var startNumber = startTemporal ? 0d : startArg.DoubleValue;
        var endNumber = endTemporal ? 0d : endArg.DoubleValue;

        // PartitionDateTimeRangePattern step 5: when either endpoint is a Temporal object,
        // both must be the same Temporal type (else TypeError).
        if (startTemporal || endTemporal)
        {
            if (!startTemporal || !endTemporal)
                throw JSEngine.NewTypeError("Intl.DateTimeFormat.formatRange: both arguments must be the same Temporal type");
            return FormatTemporalRangeToParts(startArg, endArg);
        }

        var startValue = TimeClipRange(startNumber);
        var endValue = TimeClipRange(endNumber);
        var pattern = ResolveEnginePattern();
        var startFields = ResolveFields(startValue);
        var endFields = ResolveFields(endValue);
        return MapNumbering(JSIntlDateTimeFormatEngine.FormatRangeToParts(pattern, in startFields, in endFields, FractionalSecondDigits()));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.formatToParts called on incompatible receiver");

        if (a.Length > 0 && a[0] != null && IsTemporalDateTime(a[0]))
            return PartsArray(@this.FormatTemporalToParts(a[0]));

        var value = a.Length == 0 || a[0] == null || a[0].IsUndefined
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : a.Get1().DoubleValue;
        var clipped = EcmaTime.TimeClip(value);
        if (double.IsNaN(clipped))
            throw JSEngine.NewRangeError("Invalid time value");

        if (@this.SupportsDayPeriod())
        {
            var (h, m) = @this.WallHourMinute(clipped);
            var dayPeriodParts = CreateArray();
            if (@this.UsesHourFormatting())
            {
                var hourPart = new JSObject();
                hourPart[KeyStrings.GetOrCreate("type")] = CreateString("hour");
                hourPart[KeyStrings.GetOrCreate("value")] = CreateString(FormatEnglishHour(h));
                var literalPart = new JSObject();
                literalPart[KeyStrings.GetOrCreate("type")] = CreateString("literal");
                literalPart[KeyStrings.GetOrCreate("value")] = CreateString(" ");
                dayPeriodParts.AddArrayItem(hourPart);
                dayPeriodParts.AddArrayItem(literalPart);
            }

            var dayPeriodPart = new JSObject();
            dayPeriodPart[KeyStrings.GetOrCreate("type")] = CreateString("dayPeriod");
            dayPeriodPart[KeyStrings.GetOrCreate("value")] = CreateString(@this.DayPeriodNameForStyle(h, m, @this.DayPeriodStyle()));
            dayPeriodParts.AddArrayItem(dayPeriodPart);
            return dayPeriodParts;
        }

        var pattern = @this.ResolveEnginePattern();
        var fields = @this.ResolveFields(clipped);
        var engineParts = JSIntlDateTimeFormatEngine.FormatToParts(pattern, in fields, @this.FractionalSecondDigits(), null);
        return PartsArray(@this.MapNumbering(engineParts));
    }

    // Translates the ASCII digits the engine produces for the numeric date/time fields into the
    // resolved numbering system's digits (e.g. "arab" → ٠-٩, "hanidec" → 〇一二…), and renders the
    // fractional-seconds decimal point with that numbering system's decimal separator (arab uses
    // U+066B). Names, separators and symbols keep the locale's characters.
    private System.Collections.Generic.List<JSIntlDateTimeFormatEngine.Part> MapNumbering(
        System.Collections.Generic.List<JSIntlDateTimeFormatEngine.Part> parts)
    {
        if (numberingSystem == null || numberingSystem == "latn")
            return parts;

        var decimalSeparator = numberingSystem is "arab" or "arabext" ? "٫" : ".";
        var mapped = new System.Collections.Generic.List<JSIntlDateTimeFormatEngine.Part>(parts.Count);
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Type is "year" or "relatedYear" or "month" or "day"
                or "hour" or "minute" or "second" or "fractionalSecond")
            {
                var sb = new StringBuilder(part.Value.Length);
                foreach (var c in part.Value)
                    sb.Append(c is >= '0' and <= '9' ? JSIntl.NumberingSystemDigit(numberingSystem, c - '0') : c.ToString());
                mapped.Add(new JSIntlDateTimeFormatEngine.Part(part.Type, sb.ToString(), part.Source));
            }
            else if (part.Type == "literal" && part.Value == "."
                && i + 1 < parts.Count && parts[i + 1].Type == "fractionalSecond")
            {
                mapped.Add(new JSIntlDateTimeFormatEngine.Part(part.Type, decimalSeparator, part.Source));
            }
            else
            {
                mapped.Add(part);
            }
        }
        return mapped;
    }

    public static JSValue ResolvedOptionsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDateTimeFormat @this)
            throw JSEngine.NewTypeError("Intl.DateTimeFormat.prototype.resolvedOptions called on incompatible receiver");

        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), CreateString(@this.localeTag));
        // resolvedOptions reports dtf.[[Calendar]] — the resolved calendar IDENTIFIER (e.g. "iso8601",
        // not collapsed to "gregory") after the ResolveLocale `ca` negotiation, NOT the raw option.
        result.CreateDataProperty(KeyStrings.GetOrCreate("calendar"), CreateString(@this.ResolvedCalendarId()));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), CreateString(@this.numberingSystem));
        // The default time zone is SystemTimeZoneIdentifier: the host's zone as a PRIMARY identifier — a
        // host whose local zone is "Etc/UTC" (e.g. a UTC container) reports "UTC" (test262
        // DateTimeFormat/prototype/resolvedOptions/basic) — resolved through the shared database, or
        // "UTC" for a host zone the database does not hold (milestone SJL-8.6).
        result.CreateDataProperty(KeyStrings.GetOrCreate("timeZone"),
            CreateString(TemporalTimeZones.SystemTimeZoneIdentifier()));

        if (@this.options != null)
        {
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "timeZone");
            // hourCycle / hour12 are RESOLVED values (not plain passthrough): when the
            // format includes an hour component, both are always present — hourCycle
            // resolved from the hour12/hourCycle options (defaulting per locale) and
            // hour12 derived to match. They appear right after timeZone per the spec.
            if (@this.UsesHourFormatting())
            {
                result.CreateDataProperty(KeyStrings.GetOrCreate("hourCycle"), CreateString(@this.ResolveHourCycle()));
                result.CreateDataProperty(KeyStrings.GetOrCreate("hour12"), @this.ResolveHour12() ? BooleanTrue : BooleanFalse);
            }
            // dateStyle / timeStyle report the coerced+validated string captured at
            // construction (not the raw option value).
            if (@this.dateStyle != null)
                result.CreateDataProperty(DateStyleKey, CreateString(@this.dateStyle));
            if (@this.timeStyle != null)
                result.CreateDataProperty(TimeStyleKey, CreateString(@this.timeStyle));
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
        => (options != null && !options[HourKey].IsUndefined)
            // Every timeStyle (full/long/medium/short) renders an hour component, so its
            // resolvedOptions carries a resolved hourCycle / hour12 just like an explicit hour.
            || timeStyle != null;

    private string DayPeriodStyle()
        => options?[DayPeriodKey]?.StringValue ?? string.Empty;

    private static string FormatEnglishHour(DateTimeOffset value) => FormatEnglishHour(value.Hour);

    private static string FormatEnglishHour(int hour)
        => ((hour % 12) == 0 ? 12 : hour % 12).ToString(CultureInfo.InvariantCulture);

    // The wall-clock hour/minute in the formatter's time zone (system local when unset), full range.
    private (int hour, int minute) WallHourMinute(double clipped)
    {
        var wall = JSIntlDateTimeFormatEngine.ToZone(clipped, OptionString(TimeZoneKey));
        return ((int)EcmaTime.HourFromTime(wall), (int)EcmaTime.MinFromTime(wall));
    }

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
        => DayPeriodNameForStyle(value.Hour, value.Minute, style);

    private static string DayPeriodWidth(string style) => style switch
    {
        "narrow" => "narrow",
        "short" => "abbreviated",
        _ => "wide",
    };

    private string DayPeriodNameForStyle(int hour, int minute, string style)
        => CldrLocaleData.GetDayPeriodName(localeTag, ResolveDayPeriodKey(localeTag, hour, minute), DayPeriodWidth(style));

    // The flexible day-period name for the dayPeriod option at a wall-clock time, or null when the
    // option is absent. Pre-renders the 'B' token for Temporal formatting so it matches the legacy
    // FormatDayPeriod path.
    private string DayPeriodName(int hour, int minute)
    {
        var style = OptionString(DayPeriodKey);
        return style == null ? null : DayPeriodNameForStyle(hour, minute, style);
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
        var userOptions = JSIntl.ValidateConstructorArguments(
            "DateTimeFormat", in a, out var canonical, requireNew: false);

        // CreateDateTimeFormat reads every option exactly once, in a fixed order, before
        // any formatting takes place. Read them all here — firing each user getter once,
        // in order — and capture the coerced/validated values in a private snapshot the
        // formatter and resolvedOptions then read from. This keeps the observable getter
        // order spec-compliant (test262 DateTimeFormat constructor-options-order*),
        // surfaces a throwing getter at construction (constructor-options-throwing-getters),
        // validates each option once (RangeError for an out-of-set value), and never
        // re-invokes a getter at format time.
        //
        // The snapshot has a NULL prototype: only the options we explicitly Capture below
        // are present, so a later property read on the snapshot can't accidentally pick up
        // a value the user planted on Object.prototype (test262
        // default-options-object-prototype.js).
        var snapshot = new JSObject();
        snapshot.SetPrototypeOf(Null.JSNull.Value);
        void Capture(KeyString key, string value)
        {
            if (value != null)
                snapshot[key] = CreateString(value);
        }

        // localeMatcher (step 4) was already read by ValidateConstructorArguments.
        // calendar then numberingSystem.
        var calendarOption = JSIntl.ReadCalendarOption(userOptions);
        Capture(CalendarKey, calendarOption);
        var nuOption = JSIntl.ReadNumberingSystemOption(userOptions);
        if (nuOption != null && !JSIntl.IsWellFormedUnicodeKeywordType(nuOption))
            throw JSEngine.NewRangeError("Invalid numberingSystem option");
        Capture(NumberingSystemKey, nuOption);

        // hour12 (Boolean) then hourCycle.
        var hour12 = userOptions == null ? JSUndefined.Value : userOptions[Hour12Key];
        if (!hour12.IsUndefined)
            snapshot[Hour12Key] = hour12.BooleanValue ? BooleanTrue : BooleanFalse;
        Capture(HourCycleKey, JSIntl.GetOption(userOptions, HourCycleKey, HourCycleValues, false, null));

        // timeZone (Get + ToString + offset-identifier normalization).
        Capture(TimeZoneKey, ReadTimeZoneOption(userOptions));

        // Date/time components, in table order: weekday, era, year, month, day,
        // dayPeriod, hour, minute, second, fractionalSecondDigits.
        Capture(WeekdayKey, JSIntl.GetOption(userOptions, WeekdayKey, NarrowShortLong, false, null));
        Capture(EraKey, JSIntl.GetOption(userOptions, EraKey, NarrowShortLong, false, null));
        Capture(YearKey, JSIntl.GetOption(userOptions, YearKey, NumericTwoDigit, false, null));
        Capture(MonthKey, JSIntl.GetOption(userOptions, MonthKey, MonthValues, false, null));
        Capture(DayKey, JSIntl.GetOption(userOptions, DayKey, NumericTwoDigit, false, null));
        Capture(DayPeriodKey, JSIntl.GetOption(userOptions, DayPeriodKey, NarrowShortLong, false, null));
        Capture(HourKey, JSIntl.GetOption(userOptions, HourKey, NumericTwoDigit, false, null));
        Capture(MinuteKey, JSIntl.GetOption(userOptions, MinuteKey, NumericTwoDigit, false, null));
        Capture(SecondKey, JSIntl.GetOption(userOptions, SecondKey, NumericTwoDigit, false, null));
        var fractionalSecondDigits = JSIntl.GetNumberOption(userOptions, FractionalSecondDigitsKey, 1, 3);
        if (fractionalSecondDigits.HasValue)
            snapshot[FractionalSecondDigitsKey] = CreateNumber(fractionalSecondDigits.Value);

        // timeZoneName then formatMatcher.
        Capture(TimeZoneNameKey, JSIntl.GetOption(userOptions, TimeZoneNameKey, TimeZoneNameValues, false, null));
        _ = JSIntl.GetOption(userOptions, FormatMatcherKey, FormatMatcherValues, false, "best fit");

        // dateStyle then timeStyle: GetOption-coerced once here so resolvedOptions reports
        // the coerced string rather than the raw option value.
        dateStyle = JSIntl.GetOption(userOptions, DateStyleKey, DateTimeStyleValues, false, null);
        timeStyle = JSIntl.GetOption(userOptions, TimeStyleKey, DateTimeStyleValues, false, null);

        // CreateDateTimeFormat: dateStyle / timeStyle may not be combined with an explicit
        // date/time component option. (An explicitly-undefined style is absent by GetOption.)
        if (dateStyle != null || timeStyle != null)
        {
            foreach (var name in DateTimeComponentKeys)
                if (snapshot[KeyStrings.GetOrCreate(name)] is { IsUndefined: false })
                    throw JSEngine.NewTypeError(
                        $"Intl.DateTimeFormat: the {name} option cannot be combined with dateStyle or timeStyle");
        }

        // ToDateTimeOptions(options, "any", "date"): when the user specified no date/time COMPONENT
        // and no dateStyle/timeStyle, default year/month/day to "numeric" so resolvedOptions reports
        // them (test262 resolvedOptions/basic) and so they are own properties — not inherited from a
        // tainted Object.prototype (test262 default-options-object-prototype). era / timeZoneName are
        // supplementary and do not suppress the defaults.
        if (dateStyle == null && timeStyle == null)
        {
            var hasComponent = false;
            foreach (var name in DateComponentKeys)
                if (!snapshot[KeyStrings.GetOrCreate(name)].IsUndefined) { hasComponent = true; break; }
            if (!hasComponent)
                foreach (var name in TimeComponentKeys)
                    if (!snapshot[KeyStrings.GetOrCreate(name)].IsUndefined) { hasComponent = true; break; }

            if (!hasComponent)
            {
                snapshot[YearKey] = CreateString("numeric");
                snapshot[MonthKey] = CreateString("numeric");
                snapshot[DayKey] = CreateString("numeric");
                dateDefaultsApplied = true;
            }
        }

        options = snapshot;

        var resolvedLocale = JSIntl.ResolveLocaleFromCanonical(canonical, JSIntl.DateTimeFormatRelevantKeys);
        (numberingSystem, localeTag) = JSIntl.ResolveNumberingSystem(resolvedLocale, nuOption);
        // hour12 forces a non-keyword hour cycle, so it always drops the locale's -u-hc-. An
        // hourCycle option only drops it when it DIFFERS from the extension value; an equal value
        // keeps -u-hc- in the resolved locale (test262 resolvedOptions/resolved-hour-cycle-...).
        if (!hour12.IsUndefined)
            localeTag = JSIntl.DropHourCycleExtension(localeTag);
        else
            localeTag = JSIntl.ReflectExtensionKeyword(
                localeTag, "hc", OptionString(HourCycleKey), JSIntl.GetUnicodeExtensionType(localeTag, "hc"));
        // Negotiate the `ca` (calendar) relevant key the same way: an unavailable `calendar`
        // option/extension is dropped, and the resolved locale keeps -u-ca- only when the value
        // came from (or matches) the extension. Only the resolved LOCALE is taken here; the
        // reported calendar value is derived on demand by ResolvedCalendarId.
        (_, localeTag) = JSIntl.ResolveCalendar(localeTag, OptionString(CalendarKey));
        locale = CultureInfo.CurrentCulture;
    }

    // Reads and validates the timeZone option (CreateDateTimeFormat) through the shared database
    // (milestone SJL-8.6): an offset string is normalized to ±HH:MM, and one with seconds is a
    // RangeError; a named zone is matched by its ASCII case and case-normalized — "utc" → "UTC",
    // "africa/abidjan" → "Africa/Abidjan" — but NOT canonicalized through backward links
    // ("Asia/Calcutta" stays "Asia/Calcutta"); anything else, an ISO date-time string among them, is a
    // RangeError. Returns null when the option is absent.
    private static string ReadTimeZoneOption(JSObject userOptions)
    {
        var value = userOptions == null ? JSUndefined.Value : userOptions[TimeZoneKey];
        if (value.IsUndefined)
            return null;

        return TemporalTimeZones.ResolveFormatterTimeZone(value.StringValue);
    }

    internal JSIntlDateTimeFormat(CultureInfo locale) : base()
    {
        this.locale = locale;
        localeTag = locale.Name;
        numberingSystem = JSIntl.DefaultNumberingSystem(localeTag);
    }

    private static readonly KeyString IntrinsicKey = KeyStrings.GetOrCreate("Intl.DateTimeFormat");

    // %Intl.DateTimeFormat.prototype% of the current realm (Intrinsics.IntlPrototype), never the prototype
    // of whatever `globalThis.Intl.DateTimeFormat` holds now: guest code may replace the namespace or its members.
    private static JSObject CurrentPrototype()
        => Intrinsics.IntlPrototype(IntrinsicKey);

    private static double TimeClipRange(double number)
    {
        var clipped = EcmaTime.TimeClip(number);
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
