using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.Date;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.BuiltIns.Temporal;
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
    private static readonly KeyString CurrencyDisplayKey = KeyStrings.GetOrCreate("currencyDisplay");
    private static readonly KeyString CurrencySignKey = KeyStrings.GetOrCreate("currencySign");
    private static readonly string[] CurrencyDisplayValues = ["code", "symbol", "narrowSymbol", "name"];
    private static readonly string[] CurrencySignValues = ["standard", "accounting"];
    private static readonly string[] NumberFormatStyleValues = ["decimal", "percent", "currency", "unit"];
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
    // SetNumberFormatDigitOptions: roundingIncrement must be one of these values.
    private static readonly int[] SanctionedRoundingIncrements =
        [1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000];
    private static readonly KeyString TimeZoneKey = KeyStrings.GetOrCreate("timeZone");
    private static readonly KeyString CollationKey = KeyStrings.GetOrCreate("collation");
    private static readonly KeyString LanguageKey = KeyStrings.GetOrCreate("language");
    private static readonly KeyString ScriptKey = KeyStrings.GetOrCreate("script");
    private static readonly KeyString RegionKey = KeyStrings.GetOrCreate("region");
    private static readonly KeyString NumberingSystemKey = KeyStrings.GetOrCreate("numberingSystem");
    // ECMAScript IsStructurallyValidLanguageTag matches UTS-35 `unicode_locale_id`,
    // which REQUIRES a leading `unicode_language_id` (a language subtag). A wholly
    // private-use tag (e.g. "x-private") has no language subtag and is therefore NOT
    // structurally valid — unlike BCP-47 langtag, the standalone `privateuse`
    // alternative is not part of the grammar, so it is intentionally absent here.
    // The UTS-35 `unicode_language_subtag` is `alpha{2,3} | alpha{5,8}` — unlike BCP-47 langtag it
    // has NO `extlang` production, so a 3-alpha subtag after the language (e.g. the "els" in
    // "en-els") is not structurally valid and must be rejected.
    private static readonly Regex StructurallyValidLanguageTagPattern = new(
        @"^(?:[A-Za-z]{2,3}|[A-Za-z]{5,8})(?:-[A-Za-z]{4})?(?:-(?:[A-Za-z]{2}|\d{3}))?(?:-(?:[0-9A-Za-z]{5,8}|\d[0-9A-Za-z]{3}))*(?:-(?:[0-9A-WY-Za-wy-z](?:-[0-9A-Za-z]{2,8})+))*(?:-x(?:-[0-9A-Za-z]{1,8})+)?$",
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

                // "timeZone" returns AvailablePrimaryTimeZoneIdentifiers() — the canonical (primary)
                // IANA zone identifiers, sorted in code-unit order (test262 supportedValuesOf/
                // timeZones, timeZones-include-non-continental).
                if (key == "timeZone")
                {
                    var list = JSValue.CreateArray();
                    foreach (var tz in Temporal.JSTemporalZonedDateTime.AvailablePrimaryTimeZoneIdentifiers())
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

                // ECMA-402 PluralRuleSelectRange / ResolvePluralRange: after ToNumber-coercing
                // both endpoints, a NaN start or end is a RangeError (a range cannot be ordered).
                var startValue = start.DoubleValue;
                var endValue = end.DoubleValue;
                if (double.IsNaN(startValue) || double.IsNaN(endValue))
                    throw JSEngine.NewRangeError("Intl.PluralRules.prototype.selectRange called with a NaN start or end value");
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
                var options = ValidateConstructorArguments("DurationFormat", in a, out var canonical, coerceOptions: false);
                var locale = ResolveLocaleFromCanonical(canonical);
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
            var locale = ResolveLocaleFromCanonical(canonical);
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
            var locale = ResolveLocaleFromCanonical(canonical);
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
        var optionsObject = ValidateOptionsArgument(a.GetAt(1));
        ValidateLocaleOptions(canonicalTag, optionsObject);
        // ApplyOptionsToTag + the Unicode-extension options (calendar/collation/hourCycle/caseFirst/
        // numeric/numberingSystem) fold the options into the tag, canonicalizing keyword values.
        return optionsObject == null ? canonicalTag : ApplyLocaleOptions(canonicalTag, optionsObject);
    }

    // Bcp47 keyword value aliases (UTS #35 LocaleId canonicalization). Only the deprecated values
    // browsers canonicalize are listed; any other value is used verbatim.
    private static readonly Dictionary<string, string> CalendarValueAliases = new(StringComparer.Ordinal)
    {
        ["islamicc"] = "islamic-civil",
        ["ethiopic-amete-alem"] = "ethioaa",
        ["gregorian"] = "gregory",
    };

    // Bcp47 collation ("co") keyword value aliases (UTS #35). The deprecated long names mostly
    // exceed the 8-char subtag limit so they can only arrive as an option value, but the table
    // is applied uniformly to both the option and the -u-co- tag value.
    private static readonly Dictionary<string, string> CollationValueAliases = new(StringComparer.Ordinal)
    {
        ["dictionary"] = "dict",
        ["gb2312han"] = "gb2312",
        ["phonebook"] = "phonebk",
        ["traditional"] = "trad",
    };

    // Collation-strength ("ks") keyword value aliases (UTS #35 bcp47/collation.xml). The
    // long names are deprecated aliases for the level<n>/identic short forms.
    private static readonly Dictionary<string, string> CollationStrengthValueAliases = new(StringComparer.Ordinal)
    {
        ["primary"] = "level1",
        ["secondary"] = "level2",
        ["tertiary"] = "level3",
        ["quaternary"] = "level4",
        ["identical"] = "identic",
    };

    // Measurement-system ("ms") keyword value aliases (UTS #35 bcp47/measure.xml).
    // "imperial" is the deprecated alias for "uksystem"; "metric" and "ussystem" stay.
    private static readonly Dictionary<string, string> MeasurementSystemValueAliases = new(StringComparer.Ordinal)
    {
        ["imperial"] = "uksystem",
    };

    // The Unicode boolean keys (UTS #35 bcp47/collation.xml): only these keys declare a
    // "true" type with a "yes" alias, so only for them does "yes" canonicalize to "true"
    // (and a canonical "true" is then dropped — the bare key implies true). Other "k*" keys
    // such as ka/kf/kr/ks/kv take "yes" as an ordinary type value that must be preserved.
    private static readonly HashSet<string> BooleanUnicodeKeywordKeys = new(StringComparer.Ordinal)
    {
        "kb", "kc", "kh", "kk", "kn",
    };

    // Collation types sanctioned by CLDR for a "co" keyword. "standard" and "search" are
    // reserved (never reflected as the resolved collation), so they are excluded; any other
    // value is unsupported and falls back to "default".
    private static readonly HashSet<string> KnownCollations = new(StringComparer.Ordinal)
    {
        "big5han", "compat", "dict", "direct", "ducet", "emoji", "eor", "gb2312",
        "phonebk", "phonetic", "pinyin", "reformed", "searchjl", "stroke",
        "trad", "unihan", "zhuyin",
    };

    // The recognised collations in ascending code-unit order, for Intl.supportedValuesOf
    // ("collation"). Kept in sync with KnownCollations (both exclude "standard"/"search").
    private static readonly string[] SupportedCollationsSorted = BuildSortedCollations();

    private static string[] BuildSortedCollations()
    {
        var array = new string[KnownCollations.Count];
        KnownCollations.CopyTo(array);
        System.Array.Sort(array, StringComparer.Ordinal);
        return array;
    }

    private static readonly Regex UnicodeKeywordTypePattern =
        new(@"^[0-9a-z]{3,8}(?:-[0-9a-z]{3,8})*$", RegexOptions.CultureInvariant);

    // True when a (case-insensitive) string is a well-formed Unicode BCP-47 extension
    // type — the grammar the calendar / numberingSystem options must satisfy.
    internal static bool IsWellFormedUnicodeKeywordType(string value)
        => !string.IsNullOrEmpty(value) && UnicodeKeywordTypePattern.IsMatch(value.ToLowerInvariant());

    // CanonicalizeUnicodeLocaleId restricted to the extension-free base name (language,
    // script, region, variants): applies the regular-grandfathered replacement and the
    // language/region/variant subtag aliases. Used by ApplyOptionsToTag to re-canonicalize a
    // base name reassembled from Locale option overrides.
    private static string CanonicalizeBaseName(string baseName)
    {
        if (RegularGrandfatheredMappings.TryGetValue(baseName, out var preferred))
            return preferred;

        var secondDash = baseName.IndexOf('-');
        if (secondDash > 0)
        {
            var thirdDash = baseName.IndexOf('-', secondDash + 1);
            if (thirdDash > 0)
            {
                var prefix = baseName[..thirdDash];
                if (RegularGrandfatheredMappings.TryGetValue(prefix, out var prefixPreferred))
                    return ApplySubtagAliases(CanonicalizeLanguageTagCase(prefixPreferred + baseName[thirdDash..]));
            }
        }

        return CanonicalizeMainTagVariants(ApplySubtagAliases(baseName));
    }

    // InitializeLocale steps 12-30: apply the language/script/region options and the Unicode-extension
    // keyword options to the canonical tag, then re-emit it with the "-u-" keywords canonicalized
    // (values de-aliased, a boolean "true" dropped) and sorted by key. Existing variants, attributes
    // and other singleton extensions are preserved.
    private static string ApplyLocaleOptions(string tag, JSObject options)
    {
        var parts = tag.Split('-');
        var language = parts[0];
        var i = 1;
        string script = null, region = null;
        if (i < parts.Length && parts[i].Length == 4 && IsAllAlpha(parts[i])) { script = parts[i]; i++; }
        if (i < parts.Length
            && ((parts[i].Length == 2 && IsAllAlpha(parts[i])) || (parts[i].Length == 3 && IsAllDigitTag(parts[i]))))
        { region = parts[i]; i++; }

        var variants = new List<string>();
        while (i < parts.Length && parts[i].Length != 1) { variants.Add(parts[i]); i++; }

        // Walk the singleton extensions, splitting out the "-u-" attributes/keywords from the rest.
        var attributes = new List<string>();
        var keywords = new List<(string Key, List<string> Types)>();
        var otherExtensions = new List<(char Singleton, string Body)>();
        while (i < parts.Length)
        {
            var singleton = char.ToLowerInvariant(parts[i][0]);
            i++;
            var block = new List<string>();
            while (i < parts.Length && parts[i].Length != 1) { block.Add(parts[i].ToLowerInvariant()); i++; }
            if (singleton == 'u')
            {
                foreach (var sub in block)
                {
                    if (sub.Length == 2) keywords.Add((sub, new List<string>()));
                    else if (keywords.Count > 0) keywords[^1].Types.Add(sub);
                    else attributes.Add(sub);
                }
            }
            else
            {
                otherExtensions.Add((singleton, string.Join("-", block)));
            }
        }

        // Base-name options.
        if (OptionString(options, LanguageKey) is { } langOpt)
        {
            if (!Regex.IsMatch(langOpt, "^(?:[A-Za-z]{2,3}|[A-Za-z]{5,8})$", RegexOptions.CultureInvariant))
                throw JSEngine.NewRangeError("Invalid language option");
            language = langOpt.ToLowerInvariant();
        }
        if (OptionString(options, ScriptKey) is { } scriptOpt)
        {
            if (!Regex.IsMatch(scriptOpt, "^[A-Za-z]{4}$", RegexOptions.CultureInvariant))
                throw JSEngine.NewRangeError("Invalid script option");
            script = char.ToUpperInvariant(scriptOpt[0]) + scriptOpt[1..].ToLowerInvariant();
        }
        if (OptionString(options, RegionKey) is { } regionOpt)
        {
            if (!Regex.IsMatch(regionOpt, "^(?:[A-Za-z]{2}|[0-9]{3})$", RegexOptions.CultureInvariant))
                throw JSEngine.NewRangeError("Invalid region option");
            region = regionOpt.ToUpperInvariant();
        }

        // The variants option (read after region, before the keyword options) replaces the
        // tag's variant subtags. It must be a "-"-separated sequence of unicode_variant_subtag
        // (alphanum{5,8} | digit alphanum{3}) with no duplicate subtags (case-insensitive),
        // else a RangeError; the canonical form lowercases and ordinally sorts the subtags.
        if (OptionString(options, KeyStrings.GetOrCreate("variants")) is { } variantsOpt)
        {
            var subtags = variantsOpt.ToLowerInvariant().Split('-');
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var sub in subtags)
            {
                if (!Regex.IsMatch(sub, "^(?:[0-9a-z]{5,8}|[0-9][0-9a-z]{3})$", RegexOptions.CultureInvariant)
                    || !seen.Add(sub))
                    throw JSEngine.NewRangeError("Invalid variants option");
            }
            variants = new List<string>(subtags);
            variants.Sort(System.StringComparer.Ordinal);
        }

        // Unicode-extension keyword options.
        SetKeyword(keywords, "ca", ReadTypeOption(options, "calendar", "ca"));
        SetKeyword(keywords, "co", ReadTypeOption(options, "collation", "co"));
        SetKeyword(keywords, "fw", ReadFirstDayOfWeekOption(options));
        SetKeyword(keywords, "hc", GetOption(options, KeyStrings.GetOrCreate("hourCycle"), ["h11", "h12", "h23", "h24"], false, null));
        SetKeyword(keywords, "kf", GetOption(options, KeyStrings.GetOrCreate("caseFirst"), ["upper", "lower", "false"], false, null));
        var numericOption = options[KeyStrings.GetOrCreate("numeric")];
        SetKeyword(keywords, "kn", numericOption.IsUndefined ? null : (numericOption.BooleanValue ? "true" : "false"));
        SetKeyword(keywords, "nu", ReadTypeOption(options, "numberingSystem", "nu"));

        // Re-emit. ApplyOptionsToTag finishes by re-canonicalizing the assembled tag, so an
        // option combination that forms a grandfathered tag (e.g. language "cel" + variant
        // "gaulish" => "cel-gaulish") is replaced by its preferred value ("xtg"), and any
        // deprecated language/region subtag introduced by an option is de-aliased.
        var baseName = new StringBuilder(language);
        if (script != null) baseName.Append('-').Append(script);
        if (region != null) baseName.Append('-').Append(region);
        foreach (var v in variants) baseName.Append('-').Append(v);

        // The "-u-" keywords are sorted by key and their values canonicalized.
        var sb = new StringBuilder(CanonicalizeBaseName(baseName.ToString()));

        if (attributes.Count > 0 || keywords.Count > 0)
        {
            sb.Append("-u");
            foreach (var attr in attributes) sb.Append('-').Append(attr);
            keywords.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));
            foreach (var (key, types) in keywords)
            {
                sb.Append('-').Append(key);
                var canonical = CanonicalizeKeywordValue(key, string.Join("-", types));
                // A boolean keyword whose value is the default "true" drops the value.
                if (canonical is "" or "true")
                    continue;
                sb.Append('-').Append(canonical);
            }
        }

        // Preserve other singleton extensions, sorted by singleton with private-use ("x") last.
        otherExtensions.Sort((a, b) =>
            (a.Singleton == 'x' ? '￿' : a.Singleton).CompareTo(b.Singleton == 'x' ? '￿' : b.Singleton));
        foreach (var (singleton, body) in otherExtensions)
        {
            sb.Append('-').Append(singleton);
            if (body.Length > 0) sb.Append('-').Append(body);
        }

        // The "-u-" block was emitted ahead of the other singletons above; re-sort every extension
        // into canonical alphabetical order (private-use last) so e.g. an "-a-" extension precedes
        // "-u-" (test262 Locale/constructor-tag).
        return SortExtensions(sb.ToString());
    }

    // CLDR subdivision aliases (supplemental/subdivisionAlias.xml) used for the "sd" Unicode
    // keyword (and the related "rg" region-override keyword, which carries the same syntactic
    // subdivision value). Norway renumbered its subdivisions in 2020, hence no23 → no50, etc.
    // Only the entries a test262 case actually exercises are listed; an unknown value passes
    // through unchanged.
    private static readonly Dictionary<string, string> SubdivisionValueAliases = new(StringComparer.Ordinal)
    {
        ["no23"] = "no50",
        ["cn11"] = "cnbj",
        ["cz10a"] = "cz110",
        ["fra"] = "frges",
        ["frb"] = "frbre",
        ["frc"] = "frcvl",
        ["frd"] = "frbfc",
        ["fre"] = "frbre",
        ["frg"] = "frges",
        // A multi-region replacement list ("lud" -> "lucl ludi lurd luvd luwi") canonicalizes
        // to the first entry.
        ["lud"] = "lucl",
    };

    // CLDR timezone aliases (supplemental/metaZones.xml / windowsZones — used here as the
    // "tz" Unicode-keyword value canonicalization). Each pre-deprecation tz id maps to its
    // current canonical tz id (CN/Chongqing folded into CN/Shanghai, etc.); only the entries
    // covered by test262 are listed.
    private static readonly Dictionary<string, string> TimezoneValueAliases = new(StringComparer.Ordinal)
    {
        ["cnckg"] = "cnsha",
        ["aqams"] = "nzakl",
        ["cnhrb"] = "cnsha",
        ["eire"] = "iedub",
        ["est"] = "papty",
        ["gmt0"] = "gmt",
        ["uct"] = "utc",
        ["zulu"] = "utc",
    };

    private static string CanonicalizeKeywordValue(string key, string value)
    {
        if (value.Length == 0)
            return value;
        if (key == "ca" && CalendarValueAliases.TryGetValue(value, out var ca))
            return ca;
        if (key == "co" && CollationValueAliases.TryGetValue(value, out var co))
            return co;
        if (key == "ks" && CollationStrengthValueAliases.TryGetValue(value, out var ks))
            return ks;
        if (key == "ms" && MeasurementSystemValueAliases.TryGetValue(value, out var ms))
            return ms;
        if ((key == "sd" || key == "rg") && SubdivisionValueAliases.TryGetValue(value, out var sd))
            return sd;
        if (key == "tz" && TimezoneValueAliases.TryGetValue(value, out var tz))
            return tz;
        // "yes" / "no" are the deprecated aliases for boolean Unicode keyword values
        // ("true" / "false"); only "yes" is reachable from a -u- tag (a 2-char subtag is
        // parsed as another keyword key, not a type), but normalise the option form too.
        if (BooleanUnicodeKeywordKeys.Contains(key))
        {
            if (value == "yes") return "true";
            if (value == "no") return "false";
        }
        return value;
    }

    // Canonicalizes a requested collation value (option or -u-co- tag) and returns it only
    // when it is a recognised collation; an unsupported (or reserved "standard"/"search")
    // value returns null so the caller falls back to the "default" collation.
    internal static string CanonicalizeCollation(string value)
    {
        if (value == null)
            return null;
        var lower = value.ToLowerInvariant();
        if (!UnicodeKeywordTypePattern.IsMatch(lower))
            return null;
        var canonical = CanonicalizeKeywordValue("co", lower);
        return KnownCollations.Contains(canonical) ? canonical : null;
    }

    // Canonicalizes the Unicode ("-u-") extension of an already case-folded tag (CanonicalizeUnicode-
    // LocaleId, UTS #35): the keyword type-value aliases are applied (e.g. "en-u-ca-islamicc" →
    // "en-u-ca-islamic-civil"), the attributes and the keyword pairs are sorted, duplicate attributes
    // and duplicate keyword keys are removed (the first occurrence of a key wins), and a keyword value
    // equal to "true" is dropped to the value-less canonical form.
    private static string CanonicalizeUnicodeKeywordValues(string tag)
    {
        var subtags = tag.Split('-');
        var u = -1;
        for (var i = 0; i < subtags.Length; i++)
        {
            if (subtags[i].Length != 1)
                continue;
            // Stop at the private-use singleton ("x"): a "u" inside private-use is a
            // private subtag, not a Unicode extension.
            if (subtags[i][0] == 'u') { u = i; break; }
            if (subtags[i][0] == 'x') break;
        }
        if (u < 0)
            return tag;

        var result = new List<string>(subtags.Length);
        for (var i = 0; i <= u; i++)
            result.Add(subtags[i]);

        var j = u + 1;
        // Attributes (3–8 chars) appear before the first 2-char key; canonical form sorts them and
        // removes duplicates.
        var attributes = new List<string>();
        while (j < subtags.Length && subtags[j].Length >= 3 && subtags[j].Length <= 8)
        {
            attributes.Add(subtags[j]);
            j++;
        }
        attributes.Sort(StringComparer.Ordinal);
        string lastAttribute = null;
        foreach (var attribute in attributes)
        {
            if (attribute == lastAttribute)
                continue;
            result.Add(attribute);
            lastAttribute = attribute;
        }

        // Keyword pairs (a 2-char key followed by its 3–8-char type subtags), until the extension
        // ends (a 1-char singleton starts the next one). Canonical form sorts the keywords by key and
        // keeps only the first occurrence of a duplicated key.
        var keywords = new List<(string Key, string Value)>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        while (j < subtags.Length && subtags[j].Length == 2)
        {
            var key = subtags[j];
            j++;
            var types = new List<string>();
            while (j < subtags.Length && subtags[j].Length >= 3 && subtags[j].Length <= 8)
            {
                types.Add(subtags[j]);
                j++;
            }

            if (!seenKeys.Add(key))
                continue; // duplicate key: the first occurrence wins

            var canonical = CanonicalizeKeywordValue(key, string.Join("-", types));
            // UTS #35 §3.6.4: a "true" Unicode keyword value is the default and is omitted from the
            // canonical form ("und-u-kb-yes" → "und-u-kb", "de-u-kf-true" → "de-u-kf").
            keywords.Add((key, canonical == "true" ? string.Empty : canonical));
        }
        keywords.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));
        foreach (var (key, value) in keywords)
        {
            result.Add(key);
            if (value.Length > 0)
                result.AddRange(value.Split('-'));
        }

        // Any following singleton extension / private-use sequence is copied verbatim.
        for (; j < subtags.Length; j++)
            result.Add(subtags[j]);

        return string.Join("-", result);
    }

    // UTS-35 §3.6.1 canonical form: the extension singletons of a tag appear in alphabetical order,
    // with the private-use ("x") singleton last (e.g. "en-a-bar-u-baz-x-u-foo", and a "-u-…-t-…" tag
    // re-emits "-t-" before "-u-"). Each extension's own body is canonicalized earlier, so reordering
    // whole blocks here is sufficient.
    private static string SortExtensions(string tag)
    {
        var parts = tag.Split('-');
        if (parts.Length < 2 || parts[0].Length == 1)
            return tag;

        var i = 1;
        while (i < parts.Length && parts[i].Length != 1) i++;
        if (i >= parts.Length)
            return tag; // no extension singletons

        var baseTag = string.Join("-", parts, 0, i);
        var extensions = new List<(char Singleton, string Body)>();
        while (i < parts.Length)
        {
            var singleton = char.ToLowerInvariant(parts[i][0]);
            var block = new List<string> { parts[i] };
            i++;
            if (singleton == 'x')
                // The private-use singleton is terminal: every remaining subtag (even 1-char ones)
                // belongs to its body.
                while (i < parts.Length) { block.Add(parts[i]); i++; }
            else
                while (i < parts.Length && parts[i].Length != 1) { block.Add(parts[i]); i++; }
            extensions.Add((singleton, string.Join("-", block)));
        }

        extensions.Sort((a, b) =>
            (a.Singleton == 'x' ? '￿' : a.Singleton).CompareTo(b.Singleton == 'x' ? '￿' : b.Singleton));

        var sb = new StringBuilder(baseTag);
        foreach (var ext in extensions) sb.Append('-').Append(ext.Body);
        return sb.ToString();
    }

    private static void SetKeyword(List<(string Key, List<string> Types)> keywords, string key, string value)
    {
        if (value == null)
            return;
        var types = new List<string>(value.Split('-'));
        for (var k = 0; k < keywords.Count; k++)
            if (keywords[k].Key == key) { keywords[k] = (key, types); return; }
        keywords.Add((key, types));
    }

    // A calendar/collation/numberingSystem option: coerced to a lowercase string and validated against
    // the Unicode keyword type grammar (a malformed value is a RangeError).
    private static string ReadTypeOption(JSObject options, string optionName, string key)
    {
        var value = OptionString(options, KeyStrings.GetOrCreate(optionName));
        if (value == null)
            return null;
        value = value.ToLowerInvariant();
        if (!UnicodeKeywordTypePattern.IsMatch(value))
            throw JSEngine.NewRangeError($"Invalid {optionName} option");
        return value;
    }

    private static string OptionString(JSObject options, KeyString key)
    {
        var v = options[key];
        return v == null || v.IsUndefined ? null : v.ToString();
    }

    // The firstDayOfWeek option (-u-fw-) per Intl.Locale: coerced to a string, mapped through
    // WeekdayToString (the ISO day numbers 1..7 — plus the alias 0 — become mon..sun, anything
    // else passes through), then validated against the Unicode keyword type sequence.
    private static string ReadFirstDayOfWeekOption(JSObject options)
    {
        var value = OptionString(options, KeyStrings.GetOrCreate("firstDayOfWeek"));
        if (value == null)
            return null;
        value = WeekdayToString(value).ToLowerInvariant();
        if (!UnicodeKeywordTypePattern.IsMatch(value))
            throw JSEngine.NewRangeError("Invalid firstDayOfWeek option");
        return value;
    }

    private static string WeekdayToString(string fw) => fw switch
    {
        "1" => "mon",
        "2" => "tue",
        "3" => "wed",
        "4" => "thu",
        "5" => "fri",
        "6" => "sat",
        "7" => "sun",
        "0" => "sun",
        _ => fw,
    };

    private static bool IsAllDigitTag(string s)
    {
        foreach (var c in s)
            if (c < '0' || c > '9') return false;
        return s.Length > 0;
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
        // truncate toward zero, then clamp a negative result to 0 — so a negative length yields an empty
        // list and the indexed getters are never read (rather than wrapping the negative into a uint).
        var lengthNumber = lengthValue.DoubleValue;
        var lengthInteger = double.IsNaN(lengthNumber) ? 0 : Math.Truncate(lengthNumber);
        if (lengthInteger < 0)
            lengthInteger = 0;
        else if (lengthInteger > 4294967295d)
            lengthInteger = 4294967295d;
        var length = (uint)lengthInteger;
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
            HasDuplicateSingletonSubtag(tag) ||
            HasInvalidUnicodeExtensionKey(tag) ||
            HasInvalidTransformedExtension(tag))
            throw JSEngine.NewRangeError("Invalid language tag");

        // CanonicalizeUnicodeLocaleId: a regular grandfathered tag is replaced
        // wholesale by its preferred form (e.g. "art-lojban" -> "jbo"). Each entry's
        // key is a two-subtag tag (language + extlang/variant), and the substitution
        // applies even when more subtags follow ("art-lojban-fonipa" -> "jbo-fonipa")
        // since the grandfathered prefix is what carries the deprecation.
        if (RegularGrandfatheredMappings.TryGetValue(tag, out var preferred))
            return preferred;

        var secondDash = tag.IndexOf('-');
        if (secondDash > 0)
        {
            var thirdDash = tag.IndexOf('-', secondDash + 1);
            if (thirdDash > 0)
            {
                var prefix = tag.Substring(0, thirdDash);
                if (RegularGrandfatheredMappings.TryGetValue(prefix, out var prefixPreferred))
                    return SortExtensions(CanonicalizeUnicodeKeywordValues(ApplySubtagAliases(
                        CanonicalizeLanguageTagCase(prefixPreferred + tag.Substring(thirdDash)))));
            }
        }

        return SortExtensions(CanonicalizeTransformedExtension(
            CanonicalizeUnicodeKeywordValues(
                CanonicalizeMainTagVariants(
                    ApplySubtagAliases(
                        CanonicalizeLanguageTagCase(tag))))));
    }

    // CLDR variantAlias (supplemental/supplementalMetadata.xml) — single-variant
    // replacements applied during LocaleId canonicalization (UTS #35 §3.6.4). Only the
    // entries test262 actually exercises are listed; an unknown variant passes through
    // unchanged.
    private static readonly Dictionary<string, string> SingleVariantAliases = new(StringComparer.Ordinal)
    {
        ["heploc"] = "alalc97",  // CLDR variantAlias maps "heploc" → "alalc97". A lone
                                 // "hepburn" (Hepburn romanization) is a valid variant and is
                                 // NOT aliased; only the "hepburn-heploc" sequence collapses to
                                 // "alalc97" (handled in CanonicalizeMainTagVariants).
        ["polytoni"] = "polyton",
        ["arevela"] = null,  // dropped (no replacement); the language subtag handles it
        ["arevmda"] = null,
        ["aaland"] = null,
    };

    // UTS #35 §3.6.4 LocaleId canonicalization: the variant subtags in the main language
    // tag (after the optional script + region, before any extension singleton) are
    // substituted via variantAlias, deduplicated, and sorted alphabetically (ordinal).
    // Variants inside a transformed extension's tlang are canonicalized separately by
    // CanonicalizeTransformedExtension.
    private static string CanonicalizeMainTagVariants(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return tag;

        var parts = tag.Split('-');
        // Locate the start of the variants run and the start of the first extension.
        var i = 1; // skip the language subtag
        if (i < parts.Length && parts[i].Length == 4 && IsAllAlpha(parts[i])) i++; // script
        if (i < parts.Length && ((parts[i].Length == 2 && IsAllAlpha(parts[i])) || (parts[i].Length == 3 && IsAllDigitTag(parts[i])))) i++; // region

        var variantStart = i;
        while (i < parts.Length && IsVariantSubtag(parts[i]))
            i++;

        if (i - variantStart == 0)
            return tag;

        // Substitute via variantAlias (dropped entries return null), deduplicate while
        // preserving the first occurrence's order, then sort alphabetically. A language+variant
        // compound alias (e.g. "hy-arevmda" -> "hyw") replaces the language subtag and drops the
        // variant.
        var language = parts[0];
        var variants = new List<string>(i - variantStart);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rawVariants = new HashSet<string>(StringComparer.Ordinal);
        for (var j = variantStart; j < i; j++) rawVariants.Add(parts[j]);
        for (var j = variantStart; j < i; j++)
        {
            var v = parts[j];
            // "hepburn-heploc": heploc -> alalc97 supersedes the hepburn prefix, so the pair
            // collapses to the single "alalc97". Drop "hepburn" only when "heploc" is also
            // present; a lone "hepburn" is preserved.
            if (v == "hepburn" && rawVariants.Contains("heploc"))
                continue;
            if (LanguageVariantAliases.TryGetValue(parts[0] + "-" + v, out var languageReplacement))
            {
                language = languageReplacement;
                continue; // the variant is folded into the language replacement
            }
            if (SingleVariantAliases.TryGetValue(v, out var replacement))
            {
                if (replacement == null) continue; // dropped
                v = replacement;
            }
            if (seen.Add(v))
                variants.Add(v);
        }
        variants.Sort(System.StringComparer.Ordinal);

        // Rebuild: pre-variant prefix (with any replaced language) + canonical variants +
        // everything after the variant run (extension singletons / private-use).
        var rebuilt = new List<string>(variantStart + variants.Count + (parts.Length - i));
        rebuilt.Add(language);
        for (var j = 1; j < variantStart; j++) rebuilt.Add(parts[j]);
        rebuilt.AddRange(variants);
        for (var j = i; j < parts.Length; j++) rebuilt.Add(parts[j]);

        return string.Join("-", rebuilt);
    }

    // UTS #35 §3.6.2 transformed extension canonicalization: within a "-t-" extension, the
    // tlang's variants are sorted alphabetically (ordinal) and the tfields are sorted by
    // their two-character tkey. Everything outside the "-t-" payload is preserved verbatim,
    // so this composes with the rest of the language-tag canonicalization pipeline.
    private static string CanonicalizeTransformedExtension(string tag)
    {
        if (string.IsNullOrEmpty(tag) || tag.IndexOf("-t-", System.StringComparison.OrdinalIgnoreCase) < 0)
            return tag;

        var parts = tag.Split('-');
        var changed = false;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length != 1 || (parts[i][0] != 't' && parts[i][0] != 'T'))
                continue;

            // Collect the payload up to the next singleton (start of another extension).
            var start = i + 1;
            var end = start;
            while (end < parts.Length && parts[end].Length != 1)
                end++;

            if (end == start)
                continue;

            var payload = new List<string>(end - start);
            for (var j = start; j < end; j++)
                payload.Add(parts[j]);

            var canonical = CanonicalizeTransformedPayload(payload);
            if (canonical != null)
            {
                for (var j = 0; j < canonical.Count; j++)
                    parts[start + j] = canonical[j];
                changed = true;
            }

            i = end - 1;
        }

        return changed ? string.Join("-", parts) : tag;
    }

    // Sorts the tlang's variants (the run of variant subtags after a leading language [+
    // optional script + optional region]) and the tfields (each a 2-char tkey followed by
    // 3-8-char tvalue chunks) by their respective keys. Returns null when the payload's
    // grammar doesn't match the expected shape, so the caller leaves it untouched.
    private static List<string> CanonicalizeTransformedPayload(List<string> payload)
    {
        var p = 0;
        var canonical = new List<string>(payload.Count);

        if (p < payload.Count && IsLanguageSubtag(payload[p]))
        {
            // The tlang (language[-script][-region]) is canonicalized like a normal language
            // tag — deprecated language/region subtags are replaced (e.g. "iw" -> "he") — while
            // staying lowercase, since the whole transform extension is lowercase.
            var head = new List<string> { payload[p] }; p++;
            if (p < payload.Count && IsScriptSubtag(payload[p])) { head.Add(payload[p]); p++; }
            if (p < payload.Count && IsRegionSubtag(payload[p])) { head.Add(payload[p]); p++; }
            canonical.AddRange(ApplySubtagAliases(string.Join("-", head)).ToLowerInvariant().Split('-'));

            var variants = new List<string>();
            while (p < payload.Count && IsVariantSubtag(payload[p]))
            {
                variants.Add(payload[p]);
                p++;
            }
            variants.Sort(System.StringComparer.Ordinal);
            canonical.AddRange(variants);
        }

        // tfields: each starts with a tkey (alpha + digit), followed by one or more tvalue
        // chunks (3-8 alphanum). Collect them as (key, values) and sort by key alphabetically.
        var fields = new List<(string Key, List<string> Values)>();
        while (p < payload.Count)
        {
            if (!IsTKey(payload[p]))
                return null;
            var key = payload[p];
            p++;
            var values = new List<string>();
            while (p < payload.Count && IsTValue(payload[p]))
            {
                values.Add(payload[p]);
                p++;
            }
            if (values.Count == 0)
                return null;
            // Deprecated tfield values are replaced by their preferred value (UTS #35
            // bcp47/transform_*.xml, e.g. the "m0" key's "names" -> "prprname").
            if (TransformedFieldValueAliases.TryGetValue(key, out var valueAliases))
                for (var v = 0; v < values.Count; v++)
                    if (valueAliases.TryGetValue(values[v], out var preferred))
                        values[v] = preferred;
            fields.Add((key, values));
        }
        fields.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Key, b.Key));
        foreach (var (key, values) in fields)
        {
            canonical.Add(key);
            canonical.AddRange(values);
        }

        return canonical;
    }

    // Bcp47 transform-extension tfield value aliases (UTS #35 bcp47/transform_*.xml type
    // "deprecated"/"preferred" entries), keyed by tkey. Only the entries test262 exercises
    // are listed; an unrecognized value passes through unchanged.
    private static readonly Dictionary<string, Dictionary<string, string>> TransformedFieldValueAliases =
        new(StringComparer.Ordinal)
    {
        ["m0"] = new(StringComparer.Ordinal) { ["names"] = "prprname" },
    };

    // Bcp47 language subtag aliases (CLDR supplemental languageAlias, reason="deprecated").
    // The lowercase deprecated tag maps to its single preferred language subtag — these are
    // strict in/out replacements (no script/region adjustments).
    private static readonly Dictionary<string, string> SimpleLanguageAliases = new(StringComparer.Ordinal)
    {
        ["aar"] = "aa",
        ["ces"] = "cs",
        ["cmn"] = "zh",
        ["drw"] = "fa-AF",
        ["heb"] = "he",
        ["in"] = "id",
        ["iw"] = "he",
        ["ji"] = "yi",
        ["jw"] = "jv",
        ["mo"] = "ro",
        ["tw"] = "ak",
    };

    // CLDR languageAlias entries that map a (language + region) pair to a single replacement
    // language with the region dropped — the canonical sign-language identifiers (UTS-35).
    // Matched case-insensitively on a `sgn-XX` prefix; the region is removed entirely so e.g.
    // "sgn-GR" canonicalizes to "gss" with no trailing region subtag.
    private static readonly Dictionary<string, string> SignLanguageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BR"] = "bzs", // Brazilian Sign Language
        ["CO"] = "csn", // Colombian Sign Language
        ["DE"] = "gsg", // German Sign Language
        ["DK"] = "dsl", // Danish Sign Language
        ["ES"] = "ssp", // Spanish Sign Language
        ["FR"] = "fsl", // French Sign Language
        ["GB"] = "bfi", // British Sign Language
        ["GR"] = "gss", // Greek Sign Language
        ["IE"] = "isg", // Irish Sign Language
        ["IT"] = "ise", // Italian Sign Language
        ["JP"] = "jsl", // Japanese Sign Language
        ["MX"] = "mfs", // Mexican Sign Language
        ["NI"] = "ncs", // Nicaraguan Sign Language
        ["NL"] = "dse", // Dutch Sign Language
        ["NO"] = "nsl", // Norwegian Sign Language
        ["PT"] = "psr", // Portuguese Sign Language
        ["SE"] = "swl", // Swedish Sign Language
        ["US"] = "ase", // American Sign Language
        ["ZA"] = "sfs", // South African Sign Language
    };

    // CLDR languageAlias entries keyed by language + variant: the whole pair is replaced by a single
    // preferred language and the variant is dropped (e.g. "hy-arevmda" -> "hyw" Western Armenian).
    // Only the entries test262 exercises are listed.
    private static readonly Dictionary<string, string> LanguageVariantAliases = new(StringComparer.Ordinal)
    {
        ["hy-arevmda"] = "hyw",
    };

    // CLDR supplemental languageAlias entries that, after replacing the language subtag,
    // also fill in a default script subtag — but only when no script is already present
    // (e.g. "sh" -> "sr-Latn", but "sh-Cyrl" -> "sr-Cyrl"). A complex alias may instead carry a
    // default *region* (e.g. "cnr" -> "sr-ME", but "cnr-BA" -> "sr-BA"); at most one of
    // DefaultScript / DefaultRegion is non-null, and it is only inserted when the tag does not
    // already carry a subtag of that kind.
    private static readonly Dictionary<string, (string Language, string DefaultScript, string DefaultRegion)> ComplexLanguageAliases =
        new(StringComparer.Ordinal)
    {
        ["sh"] = ("sr", "Latn", null),
        ["cnr"] = ("sr", null, "ME"),
    };

    // Region replacements selected by the tag's script subtag (CLDR territoryAlias "overlong"
    // entries with a multi-region replacement list). When the script matches an entry here the
    // mapped region wins; otherwise the SimpleRegionAliases default applies. Only the entries
    // a test262 case actually exercises are listed.
    private static readonly Dictionary<string, Dictionary<string, string>> ScriptConditionalRegionAliases =
        new(StringComparer.Ordinal)
    {
        // SU (Soviet Union) defaults to RU, but with Armenian script Armn → AM (Armenia).
        ["SU"] = new(StringComparer.Ordinal) { ["Armn"] = "AM" },
    };

    // Bcp47 region subtag aliases (CLDR supplemental territoryAlias) whose deprecated code
    // maps to a single preferred region. "Overlong" aliases whose replacement is a list (e.g.
    // "SU" → "RU AM AZ ..." that selects by likely-subtags) use the first list entry as the
    // default; the ScriptConditionalRegionAliases table above overrides this default for the
    // explicit script combinations test262 covers.
    private static readonly Dictionary<string, string> SimpleRegionAliases = new(StringComparer.Ordinal)
    {
        ["BU"] = "MM",
        ["CS"] = "RS",
        ["DD"] = "DE",
        ["DY"] = "BJ",
        ["FX"] = "FR",
        ["HV"] = "BF",
        ["NH"] = "VU",
        ["RH"] = "ZW",
        ["SU"] = "RU",
        ["TP"] = "TL",
        ["UK"] = "GB",
        ["VD"] = "VN",
        ["YD"] = "YE",
        ["YU"] = "RS",
        ["ZR"] = "CD",
    };

    // UN M.49 numeric region subtags that canonicalize to a single ISO 3166-1 alpha-2 code
    // (CLDR supplemental territoryAlias). Only the entries test262 exercises are listed; a numeric
    // code without a unique single-territory mapping (e.g. "419" Latin America) is left numeric.
    private static readonly Dictionary<string, string> NumericRegionAliases = new(StringComparer.Ordinal)
    {
        ["554"] = "NZ", // New Zealand
    };

    // CanonicalizeUnicodeLocaleId §3.6.4 LanguageAlias and TerritoryAlias substitutions over an
    // already case-folded tag. Walks the prefix (language, optional script, optional region)
    // and replaces deprecated subtags with their CLDR-preferred form; everything beyond the
    // region (variants, extensions, private-use) is preserved verbatim.
    private static string ApplySubtagAliases(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return tag;

        var subtags = tag.Split('-');
        if (subtags.Length == 0)
            return tag;

        // Language subtag: a complex alias replaces the language AND inserts a default script
        // (only when no script is already present); a simple alias just substitutes.
        var language = subtags[0];
        var hasScript = subtags.Length >= 2 && subtags[1].Length == 4 && IsAllAlpha(subtags[1]);

        // CLDR sign-language aliases: a "sgn" language with a 2-alpha region (e.g. "sgn-GR")
        // collapses to a single ISO 639-3 sign-language code (e.g. "gss") with the region
        // dropped. Apply BEFORE the language-only alias step so the (language, region) pair
        // matches before "sgn" alone is considered. The script slot (if present at index 1)
        // is preserved by keying the region by its actual index.
        if (subtags.Length >= 2
            && language.Equals("sgn", StringComparison.OrdinalIgnoreCase))
        {
            var regionIdx = hasScript ? 2 : 1;
            if (regionIdx < subtags.Length
                && subtags[regionIdx].Length == 2
                && IsAllAlpha(subtags[regionIdx])
                && SignLanguageAliases.TryGetValue(subtags[regionIdx], out var signLang))
            {
                var rest = new string[subtags.Length - 1];
                rest[0] = signLang;
                // Copy everything after the region: keeps the script (if any) DROPPED — per
                // CLDR the replacement language is reported without script/region — and any
                // trailing variant/extension subtags are preserved.
                for (var i = regionIdx + 1; i < subtags.Length; i++)
                    rest[i - 1 - (regionIdx == 1 ? 0 : 1)] = subtags[i];
                // Truncate trailing nulls when a script was skipped.
                var trim = 0;
                for (var i = rest.Length - 1; i > 0 && rest[i] == null; i--)
                    trim++;
                if (trim > 0)
                    System.Array.Resize(ref rest, rest.Length - trim);
                return string.Join("-", rest);
            }
        }

        if (ComplexLanguageAliases.TryGetValue(language, out var complex))
        {
            subtags[0] = complex.Language;
            if (complex.DefaultScript != null && !hasScript)
            {
                // Splice the default script in between the (new) language and the rest.
                subtags = SpliceSubtag(subtags, 1, complex.DefaultScript);
                hasScript = true;
            }
            else if (complex.DefaultRegion != null)
            {
                // Splice the default region (after the language, and the script if present) only
                // when the tag has no region of its own.
                var insertAt = hasScript ? 2 : 1;
                var hasRegion = insertAt < subtags.Length
                    && ((subtags[insertAt].Length == 2 && IsAllAlpha(subtags[insertAt]))
                        || (subtags[insertAt].Length == 3 && IsAllDigitTag(subtags[insertAt])));
                if (!hasRegion)
                    subtags = SpliceSubtag(subtags, insertAt, complex.DefaultRegion);
            }
        }
        else if (SimpleLanguageAliases.TryGetValue(language, out var simple))
        {
            subtags[0] = simple;
        }

        // Region subtag: lives at index 1 (no script) or 2 (after a script). Both 2-letter
        // alphabetic and 3-digit numeric (UN M.49) regions can be aliases.
        var regionIndex = hasScript ? 2 : 1;
        if (regionIndex < subtags.Length)
        {
            var region = subtags[regionIndex];
            var isAlphaRegion = region.Length == 2 && IsAllAlpha(region);
            if (isAlphaRegion)
            {
                // A script-conditional override (e.g. SU + Armn → AM) wins over the
                // default alias (SU → RU); fall back to the simple table otherwise.
                if (hasScript
                    && ScriptConditionalRegionAliases.TryGetValue(region, out var byScript)
                    && byScript.TryGetValue(subtags[1], out var conditional))
                    subtags[regionIndex] = conditional;
                else if (SimpleRegionAliases.TryGetValue(region, out var aliased))
                    subtags[regionIndex] = aliased;
            }
            else if (region.Length == 3 && IsAllDigitTag(region)
                && NumericRegionAliases.TryGetValue(region, out var numericAliased))
            {
                subtags[regionIndex] = numericAliased;
            }
        }

        return string.Join("-", subtags);
    }

    // Returns a copy of <paramref name="subtags"/> with <paramref name="value"/> inserted at
    // <paramref name="index"/> (shifting the remaining subtags right).
    private static string[] SpliceSubtag(string[] subtags, int index, string value)
    {
        var widened = new string[subtags.Length + 1];
        for (var i = 0; i < index; i++)
            widened[i] = subtags[i];
        widened[index] = value;
        for (var i = index; i < subtags.Length; i++)
            widened[i + 1] = subtags[i];
        return widened;
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

    // A structurally valid language tag carries each extension singleton ('u', 't', 'a'…) at most
    // once, compared case-insensitively (UTS-35 unicode_locale_id / RFC 5646: "de-DE-u-kn-true-U-kn-true"
    // and "pt-u-ca-gregory-u-nu-latn" are both invalid). The privateuse singleton "x" is terminal —
    // its single-character payload subtags are not extension singletons and may repeat — so scanning
    // stops once "x" is seen.
    private static bool HasDuplicateSingletonSubtag(string tag)
    {
        var subtags = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        HashSet<char> singletons = null;

        for (var i = 0; i < subtags.Length; i++)
        {
            if (subtags[i].Length != 1)
                continue;

            var singleton = char.ToLowerInvariant(subtags[i][0]);
            singletons ??= new HashSet<char>();
            if (!singletons.Add(singleton))
                return true;

            if (singleton == 'x')
                break;
        }

        return false;
    }

    // Validates the inner grammar of every transform ("-t-") extension in a language tag
    // (UTS #35 §3.6 transformed_extensions): an optional tlang (language subtag, optional
    // script, optional region, zero+ variants) followed by zero+ tfields (each tkey = 2
    // chars alpha+digit, with one or more tvalue chunks of 3-8 alphanum). The structural
    // language-tag regex only enforces "2-8 alphanum subtags inside any singleton extension",
    // so e.g. "en-t-root" or "en-t-d0" (tkey without tvalue) slip through it.
    // Returns true if any "-t-" extension in <paramref name="tag"/> is malformed.
    private static bool HasInvalidTransformedExtension(string tag)
    {
        var subtags = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < subtags.Length; i++)
        {
            // The "x" singleton terminates the tag's extension area: every subtag from here on
            // is a privateuse subtag, so a "t" inside it (e.g. "cmn-hans-cn-x-t-u") is not a
            // transformed extension and must not be validated as one.
            if (subtags[i].Length == 1 && (subtags[i][0] == 'x' || subtags[i][0] == 'X'))
                break;
            if (!subtags[i].Equals("t", StringComparison.OrdinalIgnoreCase))
                continue;

            // Collect the t-extension's payload: every subtag after "-t-" until the next
            // singleton (which starts a different extension, e.g. "-u-" or "-x-") or end.
            var start = i + 1;
            var end = start;
            while (end < subtags.Length && subtags[end].Length != 1)
                end++;

            var payload = subtags.AsSpan(start, end - start);
            if (payload.Length == 0)
                return true; // "...-t-" with no content (or "...-t-x-..." with no t-payload)

            var p = 0;
            // Optional tlang: starts with a unicode_language_subtag (alpha{2,3} or alpha{5,8}).
            if (IsLanguageSubtag(payload[0]))
            {
                p = 1;
                if (p < payload.Length && IsScriptSubtag(payload[p])) p++;
                if (p < payload.Length && IsRegionSubtag(payload[p])) p++;
                // UTS-35 §3.6: a tlang carries no duplicate unicode_variant_subtag (compared
                // case-insensitively). "de-t-en-emodeng-emodeng" is structurally invalid for
                // the same reason "en-emodeng-emodeng" is.
                HashSet<string> tlangVariants = null;
                while (p < payload.Length && IsVariantSubtag(payload[p]))
                {
                    tlangVariants ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!tlangVariants.Add(payload[p]))
                        return true;
                    p++;
                }
            }

            // Any leading non-tlang subtags, and everything after the tlang, must form tfields:
            // each is a tkey (alpha+digit) followed by 1+ tvalue chunks (3-8 alphanum).
            while (p < payload.Length)
            {
                if (!IsTKey(payload[p]))
                    return true;
                p++;

                var valueCount = 0;
                while (p < payload.Length && IsTValue(payload[p]))
                {
                    p++;
                    valueCount++;
                }
                if (valueCount == 0)
                    return true;
            }

            i = end - 1; // continue scanning after this t-extension
        }

        return false;
    }

    private static bool IsAllAlphaNumeric(string s)
    {
        foreach (var c in s)
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) return false;
        return true;
    }

    private static bool IsLanguageSubtag(string s)
        => (s.Length is 2 or 3 || s.Length is >= 5 and <= 8) && IsAllAlpha(s);

    private static bool IsScriptSubtag(string s) => s.Length == 4 && IsAllAlpha(s);

    private static bool IsRegionSubtag(string s)
    {
        if (s.Length == 2) return IsAllAlpha(s);
        if (s.Length != 3) return false;
        foreach (var c in s)
            if (c is < '0' or > '9') return false;
        return true;
    }

    private static bool IsVariantSubtag(string s)
    {
        if (s.Length is >= 5 and <= 8) return IsAllAlphaNumeric(s);
        // 4-char variant: digit followed by 3 alphanum.
        if (s.Length != 4) return false;
        if (s[0] is < '0' or > '9') return false;
        return IsAllAlphaNumeric(s);
    }

    private static bool IsTKey(string s)
    {
        if (s.Length != 2) return false;
        var c0 = s[0];
        var c1 = s[1];
        return ((c0 >= 'A' && c0 <= 'Z') || (c0 >= 'a' && c0 <= 'z')) && c1 is >= '0' and <= '9';
    }

    private static bool IsTValue(string s) => s.Length is >= 3 and <= 8 && IsAllAlphaNumeric(s);

    private static bool HasInvalidUnicodeExtensionKey(string tag)
    {
        var subtags = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < subtags.Length; i++)
        {
            // A private-use ("x") singleton is terminal — any "u" inside its payload is a
            // private subtag, not a Unicode extension, and its keys are not constrained.
            if (subtags[i].Length == 1 && (subtags[i][0] == 'x' || subtags[i][0] == 'X'))
                break;
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

        // The collation option is read and validated once, in order, by ApplyLocaleOptions
        // (ReadTypeOption "collation"); reading it here too would surface the getter out of
        // order (InitializeLocale reads each option exactly once — constructor-getter-order).
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

        // GetOption validates "style" against the sanctioned set ("decimal", "percent",
        // "currency", "unit") — a value such as "invalid" is a RangeError — and applies the
        // "decimal" default. It is read first, matching SetNumberFormatUnitOptions order.
        var style = GetOption(options, StyleKey, NumberFormatStyleValues, false, "decimal");

        var currencyValue = options[CurrencyKey];
        if (!currencyValue.IsUndefined)
        {
            var currency = currencyValue.StringValue;
            if (!IsWellFormedCurrencyCode(currency))
                throw JSEngine.NewRangeError("Invalid currency option");
        }

        if (style == "currency" && currencyValue.IsUndefined)
            throw JSEngine.NewTypeError("Intl.NumberFormat currency style requires a currency option");

        // SetNumberFormatUnitOptions reads currencyDisplay and currencySign right
        // after currency, unconditionally (even when style is not "currency"), and
        // validates them against their sanctioned value lists. The resolved values
        // are only reflected when style is "currency"; here we read them purely so
        // the option getters fire in spec order (test262 NumberFormat
        // constructor-option-read-order) and an invalid value is a RangeError.
        _ = GetOption(options, CurrencyDisplayKey, CurrencyDisplayValues, false, "symbol");
        _ = GetOption(options, CurrencySignKey, CurrencySignValues, false, "standard");

        var unitValue = options[UnitKey];
        if (style == "unit" && unitValue.IsUndefined)
            throw JSEngine.NewTypeError("Intl.NumberFormat unit style requires a unit option");

        // SetNumberFormatUnitOptions: a provided unit must be a well-formed unit identifier
        // (a sanctioned single unit, or "<numerator>-per-<denominator>" of two such units).
        // Anything else — including valid CLDR units that ECMA-402 does not sanction, such as
        // "acre-foot" or "ampere" — is a RangeError.
        if (!unitValue.IsUndefined && !IsWellFormedUnitIdentifier(unitValue.StringValue))
            throw JSEngine.NewRangeError($"Invalid unit identifier: {unitValue.StringValue}");

        // unitDisplay is read (and validated) regardless of style, but the
        // resolved slot only exists when style is "unit".
        var unitDisplay = GetOption(options, UnitDisplayKey, UnitDisplayValues, false, "short");
        if (style != "unit")
            unitDisplay = null;

        var notation = GetOption(options, NotationKey, NotationValues, false, "standard");

        // SetNumberFormatDigitOptions: read the digit options from the bag exactly
        // once, in spec order (minimumIntegerDigits, minimumFractionDigits,
        // maximumFractionDigits, minimumSignificantDigits, maximumSignificantDigits),
        // and snapshot them so later format/resolvedOptions calls reuse the values
        // instead of re-invoking option getters.
        var digitOptions = SnapshotDigitOptions(options);

        // The rounding options follow the digit options inside SetNumberFormatDigitOptions,
        // ahead of compactDisplay/useGrouping/signDisplay. roundingIncrement is a numeric
        // option restricted to a sanctioned set; the string enums below are validated against
        // their sanctioned value lists (a bad value, e.g. an empty string for
        // trailingZeroDisplay, is a RangeError). All four always have a resolved value (their
        // defaults) which resolvedOptions reflects.
        var (roundingIncrement, roundingMode, roundingPriority, trailingZeroDisplay) = ReadRoundingOptions(options);

        // notation precedes compactDisplay (spec order; observed by the compactDisplay
        // getter call-order tests). compactDisplay is always validated, but only
        // reflected when notation is "compact".
        var compactDisplay = GetOption(options, CompactDisplayKey, CompactDisplayValues, false, "short");
        if (notation != "compact")
            compactDisplay = null;

        // GetUseGroupingOption: the default is "min2" for compact notation, "auto"
        // otherwise; true → "always", a falsy value → boolean false, and an unrecognized
        // string falls back to the default. Read after compactDisplay, before signDisplay.
        var useGrouping = ResolveUseGrouping(options, notation);

        var signDisplay = GetOption(options, SignDisplayKey, SignDisplayValues, false, "auto");

        // SetNumberFormatDigitOptions: when roundingIncrement ≠ 1 the rounding strategy must be
        // fraction-digits — not significant-digits, nor a morePrecision/lessPrecision priority (a
        // TypeError) — and the resolved maximum and minimum fraction digits must be equal (a RangeError).
        if (roundingIncrement != 1)
        {
            int? Frac(string name)
            {
                var v = digitOptions?[KeyStrings.GetOrCreate(name)];
                return v == null || v.IsUndefined ? null : (int)v.DoubleValue;
            }
            var hasSignificant = Frac("minimumSignificantDigits") != null || Frac("maximumSignificantDigits") != null;
            if (roundingPriority != "auto" || hasSignificant)
                throw JSEngine.NewTypeError(
                    "roundingIncrement may only be used with fractionDigits rounding (roundingPriority \"auto\" and no significant-digit options)");

            // ri ≠ 1 forces the default maximum fraction digits down to the default minimum, so an
            // unspecified pair resolves equal; an explicitly mismatched pair is a RangeError.
            var mnfdDefault = (style ?? "decimal") == "currency" ? 2 : 0;
            var mnfd = Frac("minimumFractionDigits");
            var mxfd = Frac("maximumFractionDigits");
            int rmin, rmax;
            if (mnfd == null && mxfd == null) { rmin = rmax = mnfdDefault; }
            else
            {
                rmin = mnfd ?? Math.Min(mnfdDefault, mxfd.Value);
                rmax = mxfd ?? Math.Max(mnfdDefault, mnfd.Value);
            }
            if (rmax != rmin)
                throw JSEngine.NewRangeError("maximumFractionDigits is not equal to minimumFractionDigits");
        }

        return new JSIntlNumberFormatResolved(notation, signDisplay, compactDisplay, unitDisplay)
        {
            DigitOptions = digitOptions,
            RoundingIncrement = roundingIncrement,
            RoundingMode = roundingMode,
            RoundingPriority = roundingPriority,
            TrailingZeroDisplay = trailingZeroDisplay,
            UseGrouping = useGrouping,
        };
    }

    // The ECMA-402 sanctioned single unit identifiers — the same set
    // Intl.supportedValuesOf("unit") reports (AvailableCanonicalUnits).
    private static readonly HashSet<string> SanctionedSingleUnits = new(IntlEnumerationData.Units, StringComparer.Ordinal);

    // IsWellFormedUnitIdentifier (ECMA-402): a sanctioned single unit, or
    // "<numerator>-per-<denominator>" where both are sanctioned single units.
    private static bool IsWellFormedUnitIdentifier(string unit)
    {
        if (SanctionedSingleUnits.Contains(unit))
            return true;

        var perIndex = unit.IndexOf("-per-", StringComparison.Ordinal);
        if (perIndex < 0)
            return false;

        var numerator = unit[..perIndex];
        var denominator = unit[(perIndex + 5)..];
        return SanctionedSingleUnits.Contains(numerator) && SanctionedSingleUnits.Contains(denominator);
    }

    // GetBooleanOrStringNumberFormatOption(options, "useGrouping", « "min2", "auto",
    // "always" », defaultUseGrouping): true maps to "always", any falsy value to the
    // boolean false (returned as the "false" sentinel), and an unrecognized string
    // (e.g. "true"/"false") to the default. The default is "min2" for compact notation.
    private static string ResolveUseGrouping(JSObject options, string notation)
    {
        var fallback = notation == "compact" ? "min2" : "auto";
        if (options == null)
            return fallback;
        var v = options[KeyStrings.GetOrCreate("useGrouping")];
        if (v == null || v.IsUndefined)
            return fallback;
        if (v.IsBoolean)
            return v.BooleanValue ? "always" : "false";
        // ToBoolean(value) === false (null, 0, "", NaN) resolves to boolean false.
        if (!v.BooleanValue)
            return "false";
        var s = v.ToString();
        if (s is "min2" or "auto" or "always")
            return s;
        // Legacy "true"/"false" strings resolve to the fallback; every other string value
        // is unsupported and throws a RangeError (Intl.NumberFormat v3
        // GetBooleanOrStringNumberFormatOption — e.g. "MIN2", "True", "42").
        if (s is "true" or "false")
            return fallback;
        throw JSEngine.NewRangeError("useGrouping value is not supported");
    }

    // Reads the digit-related options from the bag once (in spec order) and stores
    // them as plain data properties, so subsequent reads observe construction-time
    // values without re-triggering option getters. Absent options are left out so
    // callers apply their own (style-dependent) defaults.
    // SetNumberFormatDigitOptions reads the rounding options immediately after the
    // digit options, in order: roundingIncrement (a sanctioned numeric), roundingMode,
    // roundingPriority, trailingZeroDisplay. Intl.PluralRules shares this step but does
    // not otherwise consume the values, so it just needs the reads to happen (the order
    // and per-value validation are observable — Intl.PluralRules options read order).
    // NumberFormat reads them inline because it also applies the increment/significant
    // cross-checks. Returns the four resolved values.
    internal static (int RoundingIncrement, string RoundingMode, string RoundingPriority, string TrailingZeroDisplay)
        ReadRoundingOptions(JSObject options)
    {
        var roundingIncrement = GetNumberOption(options, RoundingIncrementKey, 1, 5000) ?? 1;
        if (System.Array.IndexOf(SanctionedRoundingIncrements, roundingIncrement) < 0)
            throw JSEngine.NewRangeError("roundingIncrement value is out of range.");
        var roundingMode = GetOption(options, RoundingModeKey,
            ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"],
            false, "halfExpand");
        var roundingPriority = GetOption(options, RoundingPriorityKey,
            ["auto", "morePrecision", "lessPrecision"], false, "auto");
        var trailingZeroDisplay = GetOption(options, TrailingZeroDisplayKey,
            ["auto", "stripIfInteger"], false, "auto");
        return (roundingIncrement, roundingMode, roundingPriority, trailingZeroDisplay);
    }

    internal static JSObject SnapshotDigitOptions(JSObject options)
    {
        if (options == null)
            return null;

        // Each digit option has a sanctioned [min, max] range (SetNumberFormatDigitOptions
        // / GetNumberOption); a NaN or out-of-range value (e.g. maximumSignificantDigits: 0)
        // is a RangeError. The original value is still snapshotted so downstream
        // format/resolvedOptions reads observe the construction-time value. The snapshot
        // has a null prototype: only the digits we explicitly capture are visible, so
        // a property planted on Object.prototype can't bleed into the formatter.
        var snapshot = new JSObject();
        snapshot.SetPrototypeOf(Null.JSNull.Value);
        foreach (var (name, min, max) in new (string, int, int)[]
        {
            ("minimumIntegerDigits", 1, 21),
            ("minimumFractionDigits", 0, 100),
            ("maximumFractionDigits", 0, 100),
            ("minimumSignificantDigits", 1, 21),
            ("maximumSignificantDigits", 1, 21),
        })
        {
            var key = KeyStrings.GetOrCreate(name);
            var value = options[key];
            if (value != null && !value.IsUndefined)
            {
                var number = value.DoubleValue;
                if (double.IsNaN(number) || number < min || number > max)
                    throw JSEngine.NewRangeError($"Invalid {key} option");

                snapshot.FastAddValue(key, value, JSPropertyAttributes.EnumerableConfigurableValue);
            }
        }

        return snapshot;
    }

    internal static string ResolveLocale(JSValue locales)
        => ResolveLocaleFromCanonical(CanonicalizeLocaleList(locales));

    // ResolveLocale, then drop any Unicode ("-u-") extension keyword whose key is not
    // relevant to the service (ECMA-402 ResolveLocale keeps only the relevantExtensionKeys
    // in the resolved [[Locale]]). So `ja-JP-u-cu-usd` resolves to `ja-JP` for a service
    // that only cares about, say, `nu`.
    internal static string ResolveLocale(JSValue locales, string[] relevantExtensionKeys)
        => FilterUnicodeExtensionKeywords(ResolveLocale(locales), relevantExtensionKeys);

    // ResolveLocale over an already-canonicalized requested-locale list (a single
    // CanonicalizeLocaleList call's result), so the caller's locales object is not read
    // a second time. Picks the first requested locale, falling back to the host locale.
    internal static string ResolveLocaleFromCanonical(JSValue canonicalLocales)
    {
        if (canonicalLocales is JSObject array)
        {
            // LookupMatcher (ECMA-402): take the first requested locale that has a
            // BestAvailableLocale match; a structurally valid but unsupported tag (e.g.
            // "xyz") is skipped so a later supported locale wins, falling back to the
            // default locale when none match (test262 intl402 .../locales-valid).
            var length = array[KeyStrings.length].UIntValue;
            for (uint i = 0; i < length; i++)
            {
                var locale = array[i];
                if (locale.IsUndefined)
                    continue;

                if (IsLocaleAvailable(locale.StringValue))
                    return locale.StringValue;
            }
        }

        return string.IsNullOrEmpty(CultureInfo.CurrentCulture.Name) ? "en-US" : CultureInfo.CurrentCulture.Name;
    }

    internal static string ResolveLocaleFromCanonical(JSValue canonicalLocales, string[] relevantExtensionKeys)
        => FilterUnicodeExtensionKeywords(ResolveLocaleFromCanonical(canonicalLocales), relevantExtensionKeys);

    // The Unicode-extension keys each service uses to negotiate the resolved locale.
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
        if (string.IsNullOrEmpty(tag) || tag.IndexOf("-u-", System.StringComparison.OrdinalIgnoreCase) < 0)
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
    internal static readonly string[] SupportedNumberingSystemsSorted =
    {
        "adlm", "ahom", "arab", "arabext", "bali", "beng", "bhks", "brah", "cakm",
        "cham", "deva", "diak", "fullwide", "gara", "gong", "gonm", "gujr", "gukh",
        "guru", "hanidec", "hmng", "hmnp", "java", "kali", "kawi", "khmr", "knda",
        "krai", "lana", "lanatham", "laoo", "latn", "lepc", "limb", "mathbold",
        "mathdbl", "mathmono", "mathsanb", "mathsans", "mlym", "modi", "mong", "mroo",
        "mtei", "mymr", "mymrepka", "mymrpao", "mymrshan", "mymrtlng", "nagm", "newa",
        "nkoo", "olck", "onao", "orya", "osma", "outlined", "rohg", "saur", "segment",
        "shrd", "sind", "sinh", "sora", "sund", "sunu", "takr", "talu", "tamldec",
        "telu", "thai", "tibt", "tirh", "tnsa", "tols", "vaii", "wara", "wcho",
    };

    private static readonly HashSet<string> SupportedNumberingSystems =
        new(SupportedNumberingSystemsSorted, StringComparer.Ordinal);

    // AvailableCanonicalCalendars: the canonical calendar identifiers reported by
    // Intl.supportedValuesOf("calendar"), in code-unit (ascending) order. This is the set
    // required by the Intl era/monthCode additions; "islamic" and "islamic-rgsa" are
    // deliberately excluded (they are not part of the required/available set, matching V8 and
    // test262 supportedValuesOf/calendars-required-by-intl-era-monthcode).
    internal static readonly string[] AvailableCanonicalCalendars =
    {
        "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "gregory", "hebrew",
        "indian", "islamic-civil", "islamic-tbla", "islamic-umalqura",
        "iso8601", "japanese", "persian", "roc",
    };

    internal static bool IsSupportedNumberingSystem(string value)
        => value != null && SupportedNumberingSystems.Contains(value);

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
        value = value.ToLowerInvariant();
        if (!UnicodeKeywordTypePattern.IsMatch(value))
            throw JSEngine.NewRangeError("Invalid calendar option");
        // Canonicalize deprecated calendar aliases (e.g. "islamicc" → "islamic-civil").
        return CanonicalizeKeywordValue("ca", value);
    }

    // Negotiates the resolved `nu` (numbering system) against the locale's `-u-nu-`
    // extension, given the already-read numberingSystem option value. Per spec: a supported
    // `-u-nu-` value is used and reflected in the locale; a supported option that DIFFERS
    // overrides it and removes the `-u-nu-` from the locale; an unsupported value (in either
    // place) falls through to "latn" with `-u-nu-` removed.
    internal static (string NumberingSystem, string Locale) ResolveNumberingSystem(string localeTag, string optionValue)
    {
        var value = "latn";
        var reflectInLocale = false;

        var ext = GetUnicodeExtensionType(localeTag, "nu");
        if (IsSupportedNumberingSystem(ext))
        {
            value = ext;
            reflectInLocale = true;
        }

        if (optionValue != null && IsSupportedNumberingSystem(optionValue) &&
            !string.Equals(optionValue, value, StringComparison.Ordinal))
        {
            value = optionValue;
            reflectInLocale = false;
        }

        var locale = reflectInLocale ? localeTag : RemoveUnicodeExtensionKeyword(localeTag, "nu");
        return (value, locale);
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
            if (parts[j].Length == 2 && string.Equals(parts[j], key, System.StringComparison.OrdinalIgnoreCase))
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

    // The CLDR-derived default hour cycle for a locale: "h12" for 12-hour regions (and
    // English without a region), "h23" otherwise. A coarse approximation of the CLDR
    // <hours> preference data, sufficient for the common cases.
    internal static string DefaultHourCycle(string tag)
    {
        var region = RegionSubtag(tag);
        if (region != null)
            return JSIntlLocale.TwelveHourRegions.Contains(region) ? "h12" : "h23";

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

    // An offset time-zone identifier: a sign, two-digit hour and an optional two-digit minute, with
    // either a colon or no separator (±HH, ±HHMM, ±HH:MM). No seconds or fractional component is
    // accepted (CreateDateTimeFormat's IsTimeZoneOffsetString is stricter than Temporal's).
    private static readonly Regex OffsetTimeZonePattern =
        new(@"^([+-])(\d{2})(?::?(\d{2}))?$", RegexOptions.CultureInvariant);

    // Validates and normalizes an offset time-zone identifier to ±HH:MM. Hours are 00-23 and minutes
    // 00-59; a zero offset always normalizes with a "+" sign (so "-00" / "-00:00" → "+00:00"). Returns
    // false for any string that is not a well-formed offset.
    internal static bool TryNormalizeOffsetTimeZone(string timeZone, out string normalized)
    {
        normalized = null;
        var m = OffsetTimeZonePattern.Match(timeZone);
        if (!m.Success)
            return false;
        var hours = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var minutes = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
        if (hours > 23 || minutes > 59)
            return false;
        var sign = (hours == 0 && minutes == 0) ? "+" : m.Groups[1].Value;
        normalized = $"{sign}{hours:D2}:{minutes:D2}";
        return true;
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
    private readonly string numberingSystem;
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
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), JSValue.CreateString(@this.numberingSystem));
        return result;
    }

    public JSIntlRelativeTimeFormat(in Arguments a) : this()
    {
        var options = JSIntl.ValidateConstructorArguments("RelativeTimeFormat", in a, out var canonical);
        (numberingSystem, locale) = JSIntl.ResolveNumberingSystem(JSIntl.ResolveLocaleFromCanonical(canonical), options);
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
        // %Segments.prototype% [ @@iterator ] — a method (named "[Symbol.iterator]") returning a Segment
        // Iterator over the segment data objects. Exposed as an own property so `segments[@@iterator]`
        // is observable, in addition to the internal GetIterableEnumerator that backs for-of.
        FastAddValue(
            (IJSSymbol)JSSymbol.iterator,
            JSValue.CreateFunction(static (in Arguments a) =>
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
            JSValue.CreateFunction((in Arguments a) =>
            {
                var result = new JSObject();
                if (index < list.Count)
                {
                    var (start, length) = list[index];
                    index++;
                    result.FastAddValue(valueKey, segments.CreateSegmentDataObject(start, length), JSPropertyAttributes.EnumerableConfigurableValue);
                    result.FastAddValue(doneKey, JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
                }
                else
                {
                    result.FastAddValue(valueKey, JSUndefined.Value, JSPropertyAttributes.EnumerableConfigurableValue);
                    result.FastAddValue(doneKey, JSValue.BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);
                }

                return result;
            }, "next", null, 0, false),
            JSPropertyAttributes.ConfigurableValue);
        iterator.FastAddValue(
            (IJSSymbol)JSSymbol.iterator,
            JSValue.CreateFunction(static (in Arguments a) => a.This, "[Symbol.iterator]", null, 0, false),
            JSPropertyAttributes.ConfigurableValue);
        return iterator;
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
                if (Joinable(category, WordCategory(input, nextStart, nextLength)))
                {
                    length = nextStart + nextLength - start;
                    j++;
                    continue;
                }

                // UAX #29 WB11/WB12: a numeric run is not broken by a single MidNum/MidNumLet
                // separator (e.g. "." or ",") that sits between two numbers — "1.23" and "3,000"
                // are each one word. Consume the separator and the following numeric grapheme.
                if (category == WordCat.Number
                    && IsNumericInfix(input, nextStart, nextLength)
                    && j + 1 < graphemes.Count
                    && WordCategory(input, graphemes[j + 1].start, graphemes[j + 1].length) == WordCat.Number)
                {
                    var (afterStart, afterLength) = graphemes[j + 1];
                    length = afterStart + afterLength - start;
                    j += 2;
                    continue;
                }

                break;
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

    // The UAX #29 MidNum / MidNumLet separators that can sit between two numbers without
    // breaking the numeric word (decimal points and digit-group separators). Only the common
    // ASCII/Arabic separators that test262 exercises are recognised.
    private static bool IsNumericInfix(string input, int start, int length)
    {
        if (length != 1)
            return false;
        return input[start] switch
        {
            '.' or ',' or ';' or '٫' or '٬' or '’' or '，' or '．' => true,
            _ => false,
        };
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
        return JSValue.CreateString(self.Format(durationObject));
    }

    // Temporal.Duration.prototype.toLocaleString(locales, options): formats the duration with a
    // freshly-constructed Intl.DurationFormat, exactly as `new Intl.DurationFormat(locales,
    // options).format(duration)` would.
    internal static JSValue TemporalToLocaleString(JSValue duration, JSValue locales, JSValue options)
    {
        var args = new Arguments(JSUndefined.Value, locales, options ?? JSUndefined.Value);
        var optionsObject = JSIntl.ValidateConstructorArguments(
            "DurationFormat", in args, out var canonical, requireNew: false, coerceOptions: false);
        var locale = JSIntl.ResolveLocaleFromCanonical(canonical);
        var df = new JSIntlDurationFormat(locale, optionsObject);
        return JSValue.CreateString(df.Format(ToDurationFormatRecord(duration)));
    }

    public static JSValue FormatToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDurationFormat self)
            throw JSEngine.NewTypeError("Intl.DurationFormat.prototype.formatToParts called on incompatible receiver");

        var durationObject = ToDurationFormatRecord(a.Get1());
        var parts = JSValue.CreateArray();

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
        // A folded fractional value is formatted from its exact decimal string (parsed as an Intl
        // mathematical value) so no sub-second precision is lost to the double round-trip.
        var operand = exactValue != null ? JSValue.CreateString(exactValue) : JSValue.CreateNumber(value);
        return nf.ComputeFormatParts(operand);
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("DurationFormat")] as JSFunction)?.prototype
            : null;

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
                record.FastAddValue(KeyStrings.GetOrCreate(Units[i]), JSValue.CreateNumber(slots[i]), JSPropertyAttributes.EnumerableConfigurableValue);
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

        var items = StringListFromIterable(a.Get1());
        var parts = @this.BuildParts(items);
        var result = JSValue.CreateArray();
        var arr = (JSObject)result;
        for (uint i = 0; i < (uint)parts.Count; i++)
            arr.SetPropertyOrThrow(JSValue.CreateNumber(i), MakePart(parts[(int)i].type, parts[(int)i].value));
        return result;
    }

    // Like formatToParts, but returns ("element"|"literal", value) tuples. Used by
    // Intl.DurationFormat to interleave its own per-unit parts with the list's
    // separators (the "element" entries are replaced by the caller).
    internal List<(string type, string value)> FormatPartsForUnits(List<string> items) => BuildParts(items);

    // Build the parts list directly from the CLDR start/middle/end/pair patterns.
    // The earlier implementation formatted the full list to a single string and then
    // recovered the separators with IndexOf against each element, which misattributed
    // substrings when an element happened to also occur inside a separator (e.g. the
    // "or" in `formatToParts("foo")` gets iterated to ['f','o','o'], and the second
    // 'o' would match the 'o' in the "or " connector before reaching the actual third
    // element). Walking the pattern templates directly produces the parts unambiguously.
    private List<(string type, string value)> BuildParts(List<string> items)
    {
        var count = items.Count;
        if (count == 0)
            return new List<(string, string)>();
        if (count == 1)
            return new List<(string, string)> { ("element", items[0]) };

        var (start, middle, end, pair) = GetPatterns();
        if (count == 2)
            return ExpandPattern(pair, new List<(string, string)> { ("element", items[0]) }, new List<(string, string)> { ("element", items[1]) });

        var inner = ExpandPattern(end,
            new List<(string, string)> { ("element", items[count - 2]) },
            new List<(string, string)> { ("element", items[count - 1]) });
        for (var i = count - 3; i >= 1; i--)
            inner = ExpandPattern(middle, new List<(string, string)> { ("element", items[i]) }, inner);
        return ExpandPattern(start, new List<(string, string)> { ("element", items[0]) }, inner);
    }

    // Walk a "{0} … {1}" pattern emitting placeholder substitutions as the supplied
    // sub-parts and the surrounding literal text as "literal" parts; adjacent literal
    // parts are merged so the result has no zero-content runs.
    private static List<(string type, string value)> ExpandPattern(string pattern,
        List<(string type, string value)> parts0,
        List<(string type, string value)> parts1)
    {
        var result = new List<(string type, string value)>();
        var literal = new StringBuilder();
        void FlushLiteral()
        {
            if (literal.Length > 0)
            {
                result.Add(("literal", literal.ToString()));
                literal.Clear();
            }
        }
        void Append(List<(string type, string value)> subParts)
        {
            foreach (var p in subParts)
            {
                if (p.type == "literal")
                {
                    literal.Append(p.value);
                }
                else
                {
                    FlushLiteral();
                    result.Add(p);
                }
            }
        }
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '{' && i + 2 < pattern.Length && pattern[i + 2] == '}')
            {
                if (pattern[i + 1] == '0') { Append(parts0); i += 2; continue; }
                if (pattern[i + 1] == '1') { Append(parts1); i += 2; continue; }
            }
            literal.Append(pattern[i]);
        }
        FlushLiteral();
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
        options = JSIntl.ValidateDisplayNamesOptions(JSIntl.ValidateConstructorArguments("DisplayNames", in a, out var canonical, coerceOptions: false));
        locale = JSIntl.ResolveLocaleFromCanonical(canonical);
    }

    public static JSValue OfPrototype(in Arguments a)
    {
        if (a.This is not JSIntlDisplayNames @this)
            throw JSEngine.NewTypeError("Intl.DisplayNames.prototype.of called on incompatible receiver");

        var canonical = @this.ValidateCode(a.Get1());

        // For "currency", only codes the implementation has data for (AvailableCurrencies, the same set
        // Intl.supportedValuesOf("currency") returns) resolve to a name; an unknown but well-formed code
        // follows the fallback option — "code" yields the code, "none" yields undefined.
        if (@this.options.Type == "currency" && !IntlEnumerationData.CurrencySet.Contains(canonical))
            return @this.options.Fallback == "none" ? JSUndefined.Value : JSValue.CreateString(canonical);

        return JSValue.CreateString(canonical);
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
                // Intl.DisplayNames type "language" requires the code to be matched by the
                // unicode_language_id nonterminal, which — unlike a full unicode_locale_id —
                // has no extension/singleton subtags. Reject any single-character subtag
                // (e.g. the "u" in "en-u-hebrew") before structural validation.
                foreach (var subtag in code.Split('-'))
                    if (subtag.Length == 1)
                        throw JSEngine.NewRangeError($"Invalid language code: {code}");
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
    internal static readonly HashSet<string> TwelveHourRegions = new(StringComparer.Ordinal)
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

    // Scripts written right-to-left (ISO 15924 codes), used by getTextInfo to report the
    // locale's character direction. Not exhaustive, but covers the scripts CLDR marks RTL.
    private static readonly HashSet<string> RightToLeftScripts = new(StringComparer.Ordinal)
    {
        "Adlm", "Arab", "Aran", "Hebr", "Mand", "Mani", "Mend", "Merc", "Mero", "Narb",
        "Nbat", "Nkoo", "Orkh", "Palm", "Phli", "Phlp", "Phnx", "Prti", "Rohg", "Samr",
        "Sarb", "Sogd", "Sogo", "Syrc", "Thaa", "Yezi",
    };

    // Languages whose default script is right-to-left, consulted when the tag carries no
    // explicit script subtag (e.g. "ar", "he", "fa").
    private static readonly HashSet<string> RightToLeftLanguages = new(StringComparer.Ordinal)
    {
        "ar", "arc", "ckb", "dv", "fa", "glk", "he", "ku", "mzn", "nqo", "prs",
        "ps", "sd", "syr", "ug", "ur", "yi",
    };

    // Regions where the week conventionally starts on Sunday (firstDay 7); everywhere else
    // defaults to Monday (firstDay 1). An approximation sufficient for sensible defaults.
    private static readonly HashSet<string> SundayFirstRegions = new(StringComparer.Ordinal)
    {
        "US", "CA", "AU", "BR", "CN", "JP", "KR", "IL", "IN", "MX", "PH", "ZA", "HK", "TW",
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
        var (lang, script, region, rest) = locale.SplitCore();
        var result = CldrLikelySubtags.Maximize(lang, script, region);
        // The Add Likely Subtags algorithm "signaling an error" leaves the locale unchanged.
        if (result == null)
            return new JSIntlLocale(locale.Tag);
        return new JSIntlLocale(JoinCore(result.Value.lang, result.Value.script, result.Value.region, rest));
    }

    public static JSValue MinimizePrototype(in Arguments a)
    {
        var locale = RequireLocale(in a, "minimize");
        var (lang, script, region, rest) = locale.SplitCore();
        var result = CldrLikelySubtags.Minimize(lang, script, region);
        if (result == null)
            return new JSIntlLocale(locale.Tag);
        return new JSIntlLocale(JoinCore(result.Value.lang, result.Value.script, result.Value.region, rest));
    }

    // Splits the canonical tag into its language/script/region core and the remainder
    // (variants, extensions and private-use subtags), which the likely-subtags algorithms
    // leave untouched.
    private (string lang, string script, string region, string rest) SplitCore()
    {
        var parts = tag.Split('-');
        var i = 1;
        string script = null, region = null;
        if (i < parts.Length && parts[i].Length == 4 && IsAllAlpha(parts[i]))
        {
            script = parts[i];
            i++;
        }
        if (i < parts.Length
            && ((parts[i].Length == 2 && IsAllAlpha(parts[i])) || (parts[i].Length == 3 && IsAllDigit(parts[i]))))
        {
            region = parts[i];
            i++;
        }
        var rest = i < parts.Length ? string.Join("-", parts[i..]) : null;
        return (parts[0], script, region, rest);
    }

    private static string JoinCore(string lang, string script, string region, string rest)
    {
        var core = lang;
        if (!string.IsNullOrEmpty(script))
            core += "-" + script;
        if (!string.IsNullOrEmpty(region))
            core += "-" + region;
        return string.IsNullOrEmpty(rest) ? core : core + "-" + rest;
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
        var locale = RequireLocale(in a, "getTextInfo");

        // §1.4.x getTextInfo: an object with a single "direction" property ("ltr" / "rtl").
        var info = new JSObject();
        info.FastAddValue(
            KeyStrings.GetOrCreate("direction"),
            JSValue.CreateString(locale.IsRightToLeft() ? "rtl" : "ltr"),
            JSPropertyAttributes.EnumerableConfigurableValue);
        return info;
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
        var locale = RequireLocale(in a, "getWeekInfo");

        // getWeekInfo returns { firstDay, weekend } in that key order — minimalDays was removed
        // from WeekInfoOfLocale by a normative ECMA-402 change. Day numbers follow ISO-8601
        // (Monday = 1 … Sunday = 7). CLDR's full per-region data is not bundled, so this uses
        // reasonable defaults (Saturday+Sunday weekend) with a Sunday-first region table.
        // An explicit -u-fw- keyword (mon..sun) overrides the region-derived default.
        var region = locale.GetRegion();
        var firstDay = WeekdayToNumber(locale.GetUnicodeKeyword("fw"))
            ?? (region != null && SundayFirstRegions.Contains(region) ? 7 : 1);

        var weekend = JSValue.CreateArray();
        weekend.AddArrayItem(JSValue.CreateNumber(6)); // Saturday
        weekend.AddArrayItem(JSValue.CreateNumber(7)); // Sunday

        var info = new JSObject();
        info.FastAddValue(KeyStrings.GetOrCreate("firstDay"),
            JSValue.CreateNumber(firstDay), JSPropertyAttributes.EnumerableConfigurableValue);
        info.FastAddValue(KeyStrings.GetOrCreate("weekend"),
            weekend, JSPropertyAttributes.EnumerableConfigurableValue);
        return info;
    }

    // The ISO-8601 day number (Monday = 1 … Sunday = 7) for a -u-fw- keyword value, or null
    // when the value is absent or not one of the weekday abbreviations.
    private static int? WeekdayToNumber(string fw) => fw switch
    {
        "mon" => 1,
        "tue" => 2,
        "wed" => 3,
        "thu" => 4,
        "fri" => 5,
        "sat" => 6,
        "sun" => 7,
        _ => null,
    };

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

    // Character direction for getTextInfo: an explicit script subtag wins, otherwise fall
    // back to the language's default direction.
    private bool IsRightToLeft()
    {
        var script = GetScript();
        return script != null
            ? RightToLeftScripts.Contains(script)
            : RightToLeftLanguages.Contains(GetLanguage());
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
        while (i < parts.Length)
        {
            // Stop at the private-use ("x") singleton; "u" inside private-use is a private
            // subtag, not a Unicode extension.
            if (parts[i].Length == 1 && (parts[i][0] == 'x' || parts[i][0] == 'X'))
                return null;
            if (parts[i].Length == 1 && (parts[i][0] == 'u' || parts[i][0] == 'U'))
                break;
            i++;
        }
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
    private readonly string notation;
    private readonly JSObject digitOptions;

    public JSIntlPluralRules(in Arguments a) : base(CurrentPrototype())
    {
        var options = JSIntl.ValidateConstructorArguments("PluralRules", in a, out var canonical);
        locale = JSIntl.ResolveLocaleFromCanonical(canonical);
        var typeKey = KeyStrings.GetOrCreate("type");
        // GetOption reads the "type" getter exactly once and validates it against the sanctioned
        // set, throwing a RangeError for any other value (e.g. "cardinal\0cookie") rather than
        // silently accepting it (test262 sm/extensions/quote-string-for-nul-character).
        type = JSIntl.GetOption(options, typeKey, ["cardinal", "ordinal"], false, "cardinal");
        // notation precedes the digit options (SetNumberFormatDigitOptions) and is reported by
        // resolvedOptions; the digit options are snapshotted once at construction.
        notation = JSIntl.GetOption(options, KeyStrings.GetOrCreate("notation"),
            ["standard", "scientific", "engineering", "compact"], false, "standard");
        digitOptions = JSIntl.SnapshotDigitOptions(options);
        // SetNumberFormatDigitOptions also reads roundingIncrement, roundingMode,
        // roundingPriority and trailingZeroDisplay (after the digit options); reading them
        // keeps the observable option order spec-compliant and validates each value.
        if (options != null)
            JSIntl.ReadRoundingOptions(options);
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

        var digits = @this.digitOptions;
        JSValue Digit(string name, int fallback)
        {
            var v = digits?[KeyStrings.GetOrCreate(name)];
            return v == null || v.IsUndefined ? JSValue.CreateNumber(fallback) : v;
        }
        bool HasDigit(string name) => digits != null && !digits[KeyStrings.GetOrCreate(name)].IsUndefined;

        // Spec resolvedOptions order: locale, type, notation, minimumIntegerDigits, then the
        // fraction-digit or significant-digit pair (significant digits override when supplied),
        // then pluralCategories.
        var result = new JSObject();
        result.CreateDataProperty(KeyStrings.GetOrCreate("locale"), JSValue.CreateString(@this.locale));
        result.CreateDataProperty(KeyStrings.GetOrCreate("type"), JSValue.CreateString(@this.type));
        result.CreateDataProperty(KeyStrings.GetOrCreate("notation"), JSValue.CreateString(@this.notation));
        result.CreateDataProperty(KeyStrings.GetOrCreate("minimumIntegerDigits"), Digit("minimumIntegerDigits", 1));

        if (HasDigit("minimumSignificantDigits") || HasDigit("maximumSignificantDigits"))
        {
            result.CreateDataProperty(KeyStrings.GetOrCreate("minimumSignificantDigits"), Digit("minimumSignificantDigits", 1));
            result.CreateDataProperty(KeyStrings.GetOrCreate("maximumSignificantDigits"), Digit("maximumSignificantDigits", 21));
        }
        else
        {
            result.CreateDataProperty(KeyStrings.GetOrCreate("minimumFractionDigits"), Digit("minimumFractionDigits", 0));
            result.CreateDataProperty(KeyStrings.GetOrCreate("maximumFractionDigits"), Digit("maximumFractionDigits", 3));
        }

        var pluralCategories = JSValue.CreateArray();
        result.CreateDataProperty(KeyStrings.GetOrCreate("pluralCategories"), pluralCategories);
        if (pluralCategories is JSObject array)
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

    // SetNumberFormatDigitOptions rounding slots. These always have a resolved value
    // (their defaults), so resolvedOptions reflects them unconditionally.
    public int RoundingIncrement { get; set; } = 1;
    public string RoundingMode { get; set; } = "halfExpand";
    public string RoundingPriority { get; set; } = "auto";
    public string TrailingZeroDisplay { get; set; } = "auto";

    // GetUseGroupingOption: the resolved useGrouping value, always one of "auto",
    // "always", "min2", or "false" (the sentinel for boolean false). resolvedOptions
    // maps "false" back to the boolean and the formatter drives grouping from it.
    public string UseGrouping { get; set; } = "auto";
}

public class JSIntlNumberFormat : JSObject
{
    private readonly string locale;
    private readonly string numberingSystem;
    private JSObject options;
    private JSIntlNumberFormatResolved resolved;

    public JSIntlNumberFormat(in Arguments a) : this()
    {
        options = JSIntl.ValidateConstructorArguments("NumberFormat", in a, out var canonical, requireNew: false);
        // ECMA-402 InitializeNumberFormat reads the numberingSystem option immediately
        // after localeMatcher and ahead of the style/currency/digit options, so its
        // getter must fire here — before ValidateNumberFormatOptions — rather than at
        // the locale-negotiation step below (test262 NumberFormat
        // constructor-numberingSystem-order, constructor-option-read-order).
        var nuOption = JSIntl.ReadNumberingSystemOption(options);
        resolved = JSIntl.ValidateNumberFormatOptions(options);
        var resolvedLocale = JSIntl.ResolveLocaleFromCanonical(canonical, JSIntl.NumberFormatRelevantKeys);
        (numberingSystem, locale) = JSIntl.ResolveNumberingSystem(resolvedLocale, nuOption);
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
        // A BigInt operand is converted via its exact magnitude (BigIntValue truncates to Int64 and
        // overflows for large values); ToNumber gives the nearest double.
        var x = value != null && value.IsBigInt
            ? BigInt.JSBigInt.ToNumber(((BigInt.JSBigInt)value).value)
            : (value ?? JSUndefined.Value).DoubleValue;

        // ECMA-402 ToIntlMathematicalValue: a string or BigInt operand is formatted from its
        // exact decimal value, not from the nearest double (so format("1.15") rounds the real
        // 1.15, and a large BigInt keeps every digit). The double above is retained only as an
        // approximation for sign, notation exponents and plural selection.
        var (exactNum, exactDen) = ExactMagnitude(value, x);

        var style = StyleOption();
        var isCurrency = style == "currency";
        var isUnit = style == "unit";
        var isPercent = style == "percent";
        var currency = isCurrency ? ResolveCurrency() : default;

        // ECMA-402 §15.5.13 PartitionNumberPattern step 13.b: when style is "percent", the
        // mathematical value is multiplied by 100 before being formatted. The percent literal
        // is appended by AssemblePercentParts below in the locale's pattern position.
        if (isPercent && !double.IsNaN(x) && !double.IsInfinity(x))
        {
            x *= 100;
            exactNum *= 100;
        }

        var signDisplay = resolved?.SignDisplay ?? "auto";
        var isNaN = double.IsNaN(x);
        var isInfinity = double.IsInfinity(x);
        var signBit = !isNaN && double.IsNegative(x);

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
            magnitude = FormatFiniteMagnitude(exactNum, exactDen, currency.FractionDigits, currency.FractionDigits, signBit, out roundedIsZero);
        }
        else
        {
            var notation = resolved?.Notation ?? "standard";
            if (notation == "scientific" || notation == "engineering")
                magnitude = FormatScientificEngineering(Math.Abs(x), notation == "engineering", signBit, out roundedIsZero);
            else if (notation == "compact")
                magnitude = FormatCompact(Math.Abs(x), signBit, out roundedIsZero);
            else
                magnitude = FormatFiniteMagnitude(exactNum, exactDen, 0, 3, signBit, out roundedIsZero);
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

        List<(string, string)> assembled;
        if (isCurrency)
            assembled = AssembleCurrencyParts(magnitude, sign, currency);
        else
        {
            var plain = AssemblePlainParts(magnitude, sign);
            if (isUnit)
                assembled = AssembleUnitParts(plain, x);
            else if (isPercent)
                assembled = AssemblePercentParts(plain);
            else
                assembled = plain;
        }

        return MapNumberingSystemDigits(assembled);
    }

    // Exact non-negative magnitude (|value|) used for rounding, as num/den. BigInt and
    // numeric-string operands keep their full precision; every other operand uses the exact
    // value of the double ToNumber result. Non-finite operands return 0 (they take the
    // NaN/Infinity path before this is consulted).
    private static (BigInteger num, BigInteger den) ExactMagnitude(JSValue value, double approx)
    {
        if (value != null && value.IsBigInt)
            return (BigInteger.Abs(((BigInt.JSBigInt)value).value), BigInteger.One);
        if (value != null && value.IsString
            && TryParseExactDecimal(value.StringValue, out var num, out var den))
            return (num, den);
        if (double.IsNaN(approx) || double.IsInfinity(approx))
            return (BigInteger.Zero, BigInteger.One);
        return ExactRational(Math.Abs(approx));
    }

    // Parses the magnitude of a JS numeric string into an exact rational num/den (den a
    // power of ten). Supports optional sign, decimal point, exponent, and the 0x/0o/0b
    // non-decimal integer forms. Returns false (caller falls back to the double value) for
    // anything it does not recognise, e.g. "Infinity" or a non-numeric string.
    private static bool TryParseExactDecimal(string s, out BigInteger num, out BigInteger den)
    {
        num = BigInteger.Zero;
        den = BigInteger.One;
        s = s.Trim();
        if (s.Length == 0)
            return true; // empty / whitespace string is the mathematical value 0
        if (s[0] is '+' or '-')
            s = s[1..]; // magnitude only; the sign is taken from the double approximation
        if (s.Length == 0)
            return false;

        if (s.Length > 2 && s[0] == '0' && s[1] is 'x' or 'X' or 'o' or 'O' or 'b' or 'B')
        {
            var radix = s[1] is 'x' or 'X' ? 16 : s[1] is 'o' or 'O' ? 8 : 2;
            BigInteger acc = BigInteger.Zero;
            foreach (var c in s[2..])
            {
                var d = c is >= '0' and <= '9' ? c - '0'
                    : c is >= 'a' and <= 'f' ? c - 'a' + 10
                    : c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
                if (d < 0 || d >= radix)
                    return false;
                acc = acc * radix + d;
            }
            num = acc;
            return true;
        }

        var exp = 0;
        var eIdx = s.IndexOfAny(['e', 'E']);
        if (eIdx >= 0)
        {
            if (!int.TryParse(s[(eIdx + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exp))
                return false;
            s = s[..eIdx];
        }

        var dot = s.IndexOf('.');
        var intPart = dot < 0 ? s : s[..dot];
        var fracPart = dot < 0 ? string.Empty : s[(dot + 1)..];
        if (intPart.Length == 0 && fracPart.Length == 0)
            return false;
        foreach (var c in intPart)
            if (c is < '0' or > '9') return false;
        foreach (var c in fracPart)
            if (c is < '0' or > '9') return false;

        var digits = intPart + fracPart;
        var n = digits.Length == 0 ? BigInteger.Zero : BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        var e10 = exp - fracPart.Length;
        if (e10 >= 0)
            num = n * BigInteger.Pow(10, e10);
        else
        {
            num = n;
            den = BigInteger.Pow(10, -e10);
        }
        return true;
    }

    // Wraps the assembled number in the locale's percent pattern (e.g. "n%" / "n %" / "%n").
    // The literal between the number and the percent sign — when the pattern has one (e.g.
    // de-DE's "n %") — is emitted as a "literal" part so formatToParts surfaces it.
    private List<(string, string)> AssemblePercentParts(List<(string, string)> plain)
    {
        var symbol = Culture().NumberFormat.PercentSymbol;
        // .NET's PercentPositivePattern values:
        //   0 = "n %", 1 = "n%", 2 = "%n", 3 = "% n"
        var pattern = Culture().NumberFormat.PercentPositivePattern;
        var parts = new List<(string, string)>(plain.Count + 2);
        if (pattern == 2 || pattern == 3)
        {
            parts.Add(("percentSign", symbol));
            if (pattern == 3)
                parts.Add(("literal", " "));
            parts.AddRange(plain);
        }
        else
        {
            parts.AddRange(plain);
            if (pattern == 0)
                parts.Add(("literal", " "));
            parts.Add(("percentSign", symbol));
        }
        return parts;
    }

    // Translates the ASCII digits produced by the formatter into the resolved
    // numbering system's digits (e.g. "arab" 0-9 → ٠-٩, "hanidec" → 〇一二…). Only the
    // numeric digit-bearing parts are remapped; separators, symbols and literals keep
    // the locale's characters.
    private List<(string, string)> MapNumberingSystemDigits(List<(string, string)> parts)
    {
        if (numberingSystem == null || numberingSystem == "latn")
            return parts;

        for (var i = 0; i < parts.Count; i++)
        {
            var (type, value) = parts[i];
            if (type is not ("integer" or "fraction" or "exponentInteger"))
                continue;

            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
                sb.Append(c is >= '0' and <= '9' ? MapDigit(numberingSystem, c - '0') : c.ToString());
            parts[i] = (type, sb.ToString());
        }

        return parts;
    }

    // CLDR numbering systems whose digits 0-9 are a contiguous code-point range, keyed by the code
    // point of digit zero. Covers every numbering system with a simple digit mapping (the "latn" and
    // non-contiguous "hanidec" systems are handled separately), so Intl.NumberFormat renders the
    // correct script digits rather than falling back to ASCII (test262 NumberFormat/.../numbering-systems).
    private static readonly Dictionary<string, int> ContiguousDigitZero = new()
    {
        ["adlm"] = 0x1E950, ["ahom"] = 0x11730, ["arab"] = 0x0660, ["arabext"] = 0x06F0,
        ["bali"] = 0x1B50, ["beng"] = 0x09E6, ["bhks"] = 0x11C50, ["brah"] = 0x11066,
        ["cakm"] = 0x11136, ["cham"] = 0xAA50, ["deva"] = 0x0966, ["diak"] = 0x11950,
        ["fullwide"] = 0xFF10, ["gara"] = 0x10D40, ["gong"] = 0x11DA0, ["gonm"] = 0x11D50,
        ["gujr"] = 0x0AE6, ["gukh"] = 0x16130, ["guru"] = 0x0A66, ["hmng"] = 0x16B50,
        ["hmnp"] = 0x1E140, ["java"] = 0xA9D0, ["kali"] = 0xA900, ["kawi"] = 0x11F50,
        ["khmr"] = 0x17E0, ["knda"] = 0x0CE6, ["krai"] = 0x16D70, ["lana"] = 0x1A80,
        ["lanatham"] = 0x1A90, ["laoo"] = 0x0ED0, ["lepc"] = 0x1C40, ["limb"] = 0x1946,
        ["mathbold"] = 0x1D7CE, ["mathdbl"] = 0x1D7D8, ["mathmono"] = 0x1D7F6,
        ["mathsanb"] = 0x1D7EC, ["mathsans"] = 0x1D7E2, ["mlym"] = 0x0D66, ["modi"] = 0x11650,
        ["mong"] = 0x1810, ["mroo"] = 0x16A60, ["mtei"] = 0xABF0, ["mymr"] = 0x1040,
        ["mymrepka"] = 0x116DA, ["mymrpao"] = 0x116D0, ["mymrshan"] = 0x1090,
        ["mymrtlng"] = 0xA9F0, ["nagm"] = 0x1E4F0, ["newa"] = 0x11450, ["nkoo"] = 0x07C0,
        ["olck"] = 0x1C50, ["onao"] = 0x1E5F1, ["orya"] = 0x0B66, ["osma"] = 0x104A0,
        ["outlined"] = 0x1CCF0, ["rohg"] = 0x10D30, ["saur"] = 0xA8D0, ["segment"] = 0x1FBF0,
        ["shrd"] = 0x111D0, ["sind"] = 0x112F0, ["sinh"] = 0x0DE6, ["sora"] = 0x110F0,
        ["sund"] = 0x1BB0, ["sunu"] = 0x11BF0, ["takr"] = 0x116C0, ["talu"] = 0x19D0,
        ["tamldec"] = 0x0BE6, ["telu"] = 0x0C66, ["thai"] = 0x0E50, ["tibt"] = 0x0F20,
        ["tirh"] = 0x114D0, ["tnsa"] = 0x16AC0, ["tols"] = 0x11DE0, ["vaii"] = 0xA620,
        ["wara"] = 0x118E0, ["wcho"] = 0x1E2F0,
    };

    // Numbering systems whose digits are not a contiguous range.
    private static readonly Dictionary<string, string[]> AlgorithmicDigits = new()
    {
        ["hanidec"] = new[] { "〇", "一", "二", "三", "四", "五", "六", "七", "八", "九" },
    };

    internal static string MapDigit(string numberingSystem, int d)
    {
        if (AlgorithmicDigits.TryGetValue(numberingSystem, out var glyphs))
            return glyphs[d];
        if (ContiguousDigitZero.TryGetValue(numberingSystem, out var zero))
            return char.ConvertFromUtf32(zero + d);
        return ((char)('0' + d)).ToString();
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
    private List<(string, string)> FormatScientificEngineering(double absX, bool engineering, bool negative, out bool roundedIsZero)
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

        var result = FormatFiniteMagnitude(mantissa, minFrac, maxFrac, negative, out roundedIsZero);
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
    // English (short) compact scale: 10^3 K, 10^6 M, 10^9 B, 10^12 T. Ordered largest-first
    // so the FormatCompact loop picks the largest unit not exceeding the value's magnitude.
    private static readonly (int Exp, string Suffix)[] CompactUnitsEn = [(12, "T"), (9, "B"), (6, "M"), (3, "K")];
    // English long compact words (compactDisplay: "long"); the number and word are separated
    // by a literal space (CLDR "0 thousand" / "0 million" / …).
    private static readonly (int Exp, string Suffix)[] CompactUnitsEnLong =
        [(12, "trillion"), (9, "billion"), (6, "million"), (3, "thousand")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsJa = [(12, "兆"), (8, "億"), (4, "万")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsKo = [(12, "조"), (8, "억"), (4, "만"), (3, "천")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsZhHant = [(12, "兆"), (8, "億"), (4, "萬")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsZhHans = [(12, "兆"), (8, "亿"), (4, "万")];
    // German compact (CLDR de). The SHORT form has no thousands abbreviation — values below a
    // million are shown in full (98765 → "98.765") — and starts compacting at 10^6 ("0 Mio." /
    // "0 Mrd." / "0 Bio."), joining with a NO-BREAK SPACE. The LONG form does abbreviate
    // thousands ("0 Tausend" / "0 Million(en)" / "0 Milliarde(n)" / "0 Billion(en)") with an
    // ordinary space.
    private static readonly (int Exp, string Suffix)[] CompactUnitsDe = [(12, "Bio."), (9, "Mrd."), (6, "Mio.")];
    private static readonly (int Exp, string Suffix)[] CompactUnitsDeLong =
        [(12, "Billionen"), (9, "Milliarden"), (6, "Millionen"), (3, "Tausend")];

    private static (int Exp, string Suffix)[] GetCompactUnits(string localeTag, bool longForm)
    {
        var tag = localeTag ?? string.Empty;
        var dash = tag.IndexOf('-');
        var language = (dash < 0 ? tag : tag[..dash]).ToLowerInvariant();
        switch (language)
        {
            case "en":
                return longForm ? CompactUnitsEnLong : CompactUnitsEn;
            case "de":
                return longForm ? CompactUnitsDeLong : CompactUnitsDe;
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
    private List<(string, string)> FormatCompact(double absX, bool negative, out bool roundedIsZero)
    {
        var isLong = resolved?.CompactDisplay == "long";
        var units = GetCompactUnits(locale, isLong);
        if (units == null)
            return FormatFiniteMagnitude(absX, 0, 3, negative, out roundedIsZero);

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
        {
            // CLDR compact patterns that include a literal space between the number and the
            // suffix word: English long ("988 million"), and both German forms ("988 Mio." /
            // "988 Millionen"). CJK suffixes and English short ("988M") have no space. The
            // German *short* pattern joins with a NO-BREAK SPACE (U+00A0); the long forms use
            // an ordinary space.
            if (ReferenceEquals(units, CompactUnitsDe))
                parts.Add(("literal", " "));
            else if (ReferenceEquals(units, CompactUnitsEnLong)
                || ReferenceEquals(units, CompactUnitsDeLong))
                parts.Add(("literal", " "));
            parts.Add(("compact", suffix));
        }
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

        // The compact integer is grouped with the resolved (CLDR "min2" by default) grouping —
        // most compact values are abbreviated to ≤4 digits and stay ungrouped, but a locale that
        // leaves a range uncompacted (German short: 98765 → "98.765") still groups it.
        var result = new List<(string, string)>();
        AppendIntegerParts(result, intDigits);
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

    // Maps a (signed) ECMA-402 roundingMode plus the value's sign onto the unsigned
    // rounding direction applied to the magnitude: "zero" truncates toward zero,
    // "infinity" rounds away from zero, and the "half-*" variants only differ at the
    // exact midpoint (toward zero / away / to even).
    private enum UnsignedRoundingMode { Zero, Infinity, HalfZero, HalfInfinity, HalfEven }

    private UnsignedRoundingMode GetUnsignedRoundingMode(bool negative)
        => (resolved?.RoundingMode ?? "halfExpand") switch
        {
            "ceil" => negative ? UnsignedRoundingMode.Zero : UnsignedRoundingMode.Infinity,
            "floor" => negative ? UnsignedRoundingMode.Infinity : UnsignedRoundingMode.Zero,
            "expand" => UnsignedRoundingMode.Infinity,
            "trunc" => UnsignedRoundingMode.Zero,
            "halfCeil" => negative ? UnsignedRoundingMode.HalfZero : UnsignedRoundingMode.HalfInfinity,
            "halfFloor" => negative ? UnsignedRoundingMode.HalfInfinity : UnsignedRoundingMode.HalfZero,
            "halfExpand" => UnsignedRoundingMode.HalfInfinity,
            "halfTrunc" => UnsignedRoundingMode.HalfZero,
            "halfEven" => UnsignedRoundingMode.HalfEven,
            _ => UnsignedRoundingMode.HalfInfinity,
        };

    // Exact non-negative rational value of a finite double, as num/den with den a power
    // of two. Used so rounding decisions are made on the real mathematical value of the
    // Number (e.g. 1.15 is really 1.14999…), exactly as ECMA-402 requires, rather than on
    // a re-rounded decimal string.
    private static (BigInteger num, BigInteger den) ExactRational(double x)
    {
        long bits = BitConverter.DoubleToInt64Bits(x);
        int exponent = (int)((bits >> 52) & 0x7FF);
        long fraction = bits & 0xFFFFFFFFFFFFFL;
        BigInteger mantissa = exponent == 0 ? fraction : fraction | 0x10000000000000L;
        exponent -= 1075; // remove the bias (1023) and the 52-bit fraction shift
        if (mantissa.IsZero)
            return (BigInteger.Zero, BigInteger.One);
        return exponent >= 0
            ? (mantissa << exponent, BigInteger.One)
            : (mantissa, BigInteger.One << -exponent);
    }

    // Rounds the non-negative ratio a/b (b > 0) to an integer using the unsigned mode.
    // The midpoint is detected exactly by comparing 2·remainder against b.
    private static BigInteger RoundRatio(BigInteger a, BigInteger b, UnsignedRoundingMode mode)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r.IsZero)
            return q;
        var cmp = (r << 1).CompareTo(b); // <0 below, 0 at, >0 above the midpoint
        var roundUp = mode switch
        {
            UnsignedRoundingMode.Zero => false,
            UnsignedRoundingMode.Infinity => true,
            UnsignedRoundingMode.HalfZero => cmp > 0,
            UnsignedRoundingMode.HalfInfinity => cmp >= 0,
            UnsignedRoundingMode.HalfEven => cmp > 0 || (cmp == 0 && !q.IsEven),
            _ => cmp >= 0,
        };
        return roundUp ? q + 1 : q;
    }

    // Sign of (num/den − 10^k): lets us pin down floor(log10(x)) exactly.
    private static int CompareToPowerOfTen(BigInteger num, BigInteger den, int k)
        => k >= 0
            ? num.CompareTo(BigInteger.Pow(10, k) * den)
            : (num * BigInteger.Pow(10, -k)).CompareTo(den);

    // ToRawFixed (ECMA-402): rounds the non-negative exact value num/den to a multiple of
    // roundingIncrement·10^-maxFrac using the unsigned rounding mode, then returns the
    // integer/fraction digit strings with trailing fraction zeros trimmed down to minFrac.
    private static (string intDigits, string fracDigits, bool isZero) RoundFixed(
        BigInteger num, BigInteger den, int minFrac, int maxFrac, int increment, UnsignedRoundingMode mode)
    {
        var scale = BigInteger.Pow(10, maxFrac);
        // qInc = round(x · 10^maxFrac / increment); scaled = qInc · increment = x · 10^maxFrac.
        var qInc = RoundRatio(num * scale, den * increment, mode);
        var scaled = qInc * increment;
        var s = scaled.ToString(CultureInfo.InvariantCulture);

        if (maxFrac == 0)
            return (s, string.Empty, scaled.IsZero);

        if (s.Length <= maxFrac)
            s = s.PadLeft(maxFrac + 1, '0');
        var intDigits = s[..^maxFrac];
        var fracDigits = s[^maxFrac..];
        while (fracDigits.Length > minFrac && fracDigits.EndsWith('0'))
            fracDigits = fracDigits[..^1];
        return (intDigits, fracDigits, scaled.IsZero);
    }

    // ToRawPrecision core (ECMA-402): rounds the non-negative exact value num/den (> 0) to
    // maxSig significant digits using the unsigned rounding mode and returns the
    // significant-digit string (trailing zeros trimmed to minSig) together with the exponent
    // e of its most-significant digit.
    private static (string digits, int e) RawPrecisionDigits(
        BigInteger num, BigInteger den, int minSig, int maxSig, UnsignedRoundingMode mode)
    {
        var e = (int)Math.Floor(BigInteger.Log10(num) - BigInteger.Log10(den));
        while (CompareToPowerOfTen(num, den, e) < 0) e--;       // ensure 10^e ≤ x
        while (CompareToPowerOfTen(num, den, e + 1) >= 0) e++;  // ensure x < 10^(e+1)

        var k = maxSig - 1 - e;
        BigInteger a, b;
        if (k >= 0) { a = num * BigInteger.Pow(10, k); b = den; }
        else { a = num; b = den * BigInteger.Pow(10, -k); }

        var digits = RoundRatio(a, b, mode).ToString(CultureInfo.InvariantCulture);
        if (digits.Length > maxSig)
        {
            // Rounding carried into a new most-significant digit (e.g. 9.99 → 10).
            e += digits.Length - maxSig;
            digits = digits[..maxSig];
        }

        var sig = digits.Length;
        while (sig > minSig && digits[sig - 1] == '0')
            sig--;
        return (digits[..sig], e);
    }

    // Overload that derives the exact value from a double (used by notations that have
    // already scaled the value into a double mantissa, e.g. scientific / compact).
    private List<(string, string)> FormatFiniteMagnitude(
        double magnitude, int defaultMinFrac, int defaultMaxFrac, bool negative, out bool roundedIsZero)
    {
        var (num, den) = ExactRational(magnitude);
        return FormatFiniteMagnitude(num, den, defaultMinFrac, defaultMaxFrac, negative, out roundedIsZero);
    }

    private List<(string, string)> FormatFiniteMagnitude(
        BigInteger num, BigInteger den, int defaultMinFrac, int defaultMaxFrac, bool negative, out bool roundedIsZero)
    {
        var priority = resolved?.RoundingPriority ?? "auto";
        var hasSig = HasOption("minimumSignificantDigits") || HasOption("maximumSignificantDigits");
        var mode = GetUnsignedRoundingMode(negative);

        // roundingPriority "morePrecision"/"lessPrecision" resolve both a significant-digit
        // and a fraction-digit rounding and pick between them; "auto" with significant-digit
        // options uses significant digits; otherwise the fraction-digit rules apply.
        if (priority is "morePrecision" or "lessPrecision")
            return FormatWithPriority(num, den, defaultMinFrac, defaultMaxFrac, priority, mode, out roundedIsZero);
        if (hasSig)
            return FormatSignificantDigits(num, den, mode, out roundedIsZero);
        return FormatFractionDigits(num, den, defaultMinFrac, defaultMaxFrac, mode, out roundedIsZero);
    }

    // roundingType fractionDigits: rounds to between minimumFractionDigits and
    // maximumFractionDigits (honouring roundingIncrement and the rounding mode).
    private List<(string, string)> FormatFractionDigits(
        BigInteger num, BigInteger den, int defaultMinFrac, int defaultMaxFrac, UnsignedRoundingMode mode, out bool roundedIsZero)
    {
        var (minFrac, maxFrac) = ResolveFractionDigits(defaultMinFrac, defaultMaxFrac);
        var minInt = ReadIntOption("minimumIntegerDigits", 1);
        var increment = resolved?.RoundingIncrement ?? 1;

        var (intDigits, fracDigits, isZero) = RoundFixed(num, den, minFrac, maxFrac, increment, mode);
        roundedIsZero = isZero;
        return BuildDigitParts(intDigits, fracDigits, minInt);
    }

    // roundingType significantDigits: rounds to at most maximumSignificantDigits and pads
    // with trailing zeros up to minimumSignificantDigits.
    private List<(string, string)> FormatSignificantDigits(
        BigInteger num, BigInteger den, UnsignedRoundingMode mode, out bool roundedIsZero)
    {
        var minSig = ReadIntOption("minimumSignificantDigits", 1);
        var maxSig = ReadIntOption("maximumSignificantDigits", 21);
        if (maxSig < minSig)
            maxSig = minSig;
        var minInt = ReadIntOption("minimumIntegerDigits", 1);

        var (intDigits, fracDigits) = ToRawPrecision(num, den, minSig, maxSig, mode);
        roundedIsZero = num.IsZero;
        return BuildDigitParts(intDigits, fracDigits, minInt);
    }

    // FormatNumericToString morePrecision/lessPrecision: compute the significant-digit and
    // fraction-digit roundings, compare their rounding magnitudes (significant: e−maxSig+1,
    // fraction: −maxFrac), and keep the more (or less) precise of the two.
    private List<(string, string)> FormatWithPriority(
        BigInteger num, BigInteger den, int defaultMinFrac, int defaultMaxFrac, string priority,
        UnsignedRoundingMode mode, out bool roundedIsZero)
    {
        var minSig = ReadIntOption("minimumSignificantDigits", 1);
        var maxSig = ReadIntOption("maximumSignificantDigits", 21);
        if (maxSig < minSig)
            maxSig = minSig;
        var (_, maxFrac) = ResolveFractionDigits(defaultMinFrac, defaultMaxFrac);

        int sMagnitude;
        if (num.IsZero)
            sMagnitude = 1 - maxSig; // e = 0 for zero
        else
        {
            var (_, e) = RawPrecisionDigits(num, den, minSig, maxSig, mode);
            sMagnitude = e - maxSig + 1;
        }
        var fMagnitude = -maxFrac;

        var fixedIsMorePrecise = fMagnitude < sMagnitude;
        var useFixed = (priority == "morePrecision" && fixedIsMorePrecise)
                    || (priority == "lessPrecision" && !fixedIsMorePrecise);

        return useFixed
            ? FormatFractionDigits(num, den, defaultMinFrac, defaultMaxFrac, mode, out roundedIsZero)
            : FormatSignificantDigits(num, den, mode, out roundedIsZero);
    }

    // Shared assembly of the integer/fraction "parts" from already-rounded digit strings:
    // strips leading integer zeros, pads to minimumIntegerDigits, and emits the grouped
    // integer parts plus an optional decimal separator and fraction part.
    private List<(string, string)> BuildDigitParts(string intDigits, string fracDigits, int minInt)
    {
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

    // ToRawPrecision (ECMA-402): renders x with between minSig and maxSig significant
    // digits using the unsigned rounding mode, returning the integer- and fraction-digit
    // strings. The most significant digit sits at 10^e; trailing zeros are trimmed to minSig.
    private (string intDigits, string fracDigits) ToRawPrecision(
        BigInteger num, BigInteger den, int minSig, int maxSig, UnsignedRoundingMode mode)
    {
        if (num.IsZero)
            return ("0", new string('0', Math.Max(0, minSig - 1)));

        var (digits, e) = RawPrecisionDigits(num, den, minSig, maxSig, mode);

        if (e >= 0)
        {
            var intLen = e + 1;
            if (digits.Length <= intLen)
                return (digits.PadRight(intLen, '0'), string.Empty);
            return (digits[..intLen], digits[intLen..]);
        }

        return ("0", new string('0', -e - 1) + digits);
    }

    private void AppendIntegerParts(List<(string, string)> result, string intDigits)
    {
        var mode = resolved?.UseGrouping ?? "auto";

        // The locale's grouping sizes (rightmost group first; the last entry repeats),
        // e.g. [3] for en-US — 1,000,000 — and [3,2] for en-IN — 10,00,000.
        var groups = SplitIntoGroups(intDigits, Culture().NumberFormat.NumberGroupSizes);

        // The grouping decision is driven by how many integer digits lie to the LEFT of the
        // rightmost (primary) grouping separator — not by the most-significant group's size,
        // which differs under a multi-level grouping pattern such as en-IN's 3;2 (100000 →
        // "1,00,000": leftmost group "1", but 3 digits precede the primary separator):
        //   "false"  → never group;
        //   "min2"   → group only when ≥ 2 digits precede the primary separator (1000 → "1000",
        //              10000 → "10,000", en-IN 100000 → "1,00,000");
        //   "auto"   → group when ≥ minimumGroupingDigits precede it (pl/es/it use 2);
        //   "always" → group whenever there is more than one group.
        var digitsBeforePrimary = intDigits.Length - groups[^1].Length;
        var shouldGroup = groups.Count > 1 && mode switch
        {
            "false" => false,
            "min2" => digitsBeforePrimary >= 2,
            "always" => true,
            _ => digitsBeforePrimary >= CldrLocaleData.MinimumGroupingDigits(locale),
        };

        if (!shouldGroup)
        {
            result.Add(("integer", intDigits));
            return;
        }

        var groupSeparator = GroupSeparator();
        for (var i = 0; i < groups.Count; i++)
        {
            if (i > 0)
                result.Add(("group", groupSeparator));
            result.Add(("integer", groups[i]));
        }
    }

    // Splits an integer-digit string into locale groups, left to right. NumberGroupSizes
    // lists group sizes from the right; the final entry repeats, and a trailing 0 means
    // "no further grouping" (the remaining most-significant digits stay in one group).
    private static List<string> SplitIntoGroups(string intDigits, int[] sizes)
    {
        var groups = new List<string>();
        var pos = intDigits.Length;
        var i = 0;
        while (pos > 0)
        {
            var size = sizes.Length == 0 ? 0 : sizes[Math.Min(i, sizes.Length - 1)];
            if (size <= 0)
            {
                groups.Insert(0, intDigits[..pos]);
                break;
            }
            var start = Math.Max(0, pos - size);
            groups.Insert(0, intDigits.Substring(start, pos - start));
            pos = start;
            i++;
        }
        return groups;
    }

    private string NanSymbol() => CldrLocaleData.NaNSymbol(locale);

    // Digit options come from the construction-time snapshot (read once from the
    // options bag), not the live options object, so format-time reads do not
    // re-invoke option getters.
    // SetNumberFormatDigitOptions fraction-digit resolution: when only one of the
    // fraction-digit bounds is given, the other default is pulled toward it — a lone maximum
    // lowers the default minimum (min(defaultMin, max)), a lone minimum raises the default
    // maximum (max(defaultMax, min)). Used by both the formatter and resolvedOptions so they
    // agree; the defaults are style-dependent (currency → its minor-unit digit count).
    internal (int Min, int Max) ResolveFractionDigits(int defaultMinFrac, int defaultMaxFrac)
    {
        var hasMinFrac = HasOption("minimumFractionDigits");
        var hasMaxFrac = HasOption("maximumFractionDigits");
        if (hasMinFrac && hasMaxFrac)
        {
            var min = ReadIntOption("minimumFractionDigits", defaultMinFrac);
            var max = ReadIntOption("maximumFractionDigits", defaultMaxFrac);
            return (min, Math.Max(max, min));
        }
        if (hasMaxFrac)
        {
            var max = ReadIntOption("maximumFractionDigits", defaultMaxFrac);
            return (Math.Min(defaultMinFrac, max), max);
        }
        if (hasMinFrac)
        {
            var min = ReadIntOption("minimumFractionDigits", defaultMinFrac);
            return (min, Math.Max(defaultMaxFrac, min));
        }
        return (defaultMinFrac, defaultMaxFrac);
    }

    // The default fraction-digit bounds for the resolved style: currency uses the currency's
    // CLDR minor-unit count (CurrencyDigits, default 2) for both bounds; every other style
    // uses 0 … 3, matching the magnitude formatter.
    private (int Min, int Max) DefaultFractionDigits()
    {
        // Compact notation switches the formatter into "compactRounding" mode (1-2
        // significant digits) when no explicit fraction / significant options are present
        // (ECMA-402 §15.5.4 SetNumberFormatDigitOptions step 17). The fraction-digit
        // defaults collapse to 0/0 — both the currency's CLDR minor-unit digit count
        // (e.g. KWD's 3) and the generic 0/3 cap are overridden.
        if ((resolved?.Notation ?? "standard") == "compact")
            return (0, 0);
        // The currency's CLDR minor-unit digit count is only the default under "standard"
        // notation. Scientific / engineering notation ignores it and uses the generic 0 … 3
        // bounds (ECMA-402 SetNumberFormatDigitOptions passes mnfdDefault 0 / mxfdDefault 3
        // unless style is currency *and* notation is standard).
        if (StyleOption() == "currency" && (resolved?.Notation ?? "standard") == "standard")
        {
            var cDigits = ResolveCurrency().FractionDigits;
            return (cDigits, cDigits);
        }
        return (0, 3);
    }

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
            ReadStringOption("currency", string.Empty).ToUpperInvariant(),
            ReadStringOption("currencyDisplay", "symbol"));

    // The currency option is canonicalized to upper case (CanonicalizeUCurrencyCode); the
    // value has already been validated as a well-formed 3-letter code at construction time.
    private static JSValue CanonicalCurrencyValue(JSValue currency)
        => JSValue.CreateString(currency.StringValue.ToUpperInvariant());

    private string ReadStringOption(string name, string fallback)
    {
        if (options == null)
            return fallback;
        var v = options[KeyStrings.GetOrCreate(name)];
        return v == null || v.IsUndefined ? fallback : v.StringValue;
    }

    public static JSValue FormatRangePrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat self)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatRange called on incompatible receiver");

        var sb = new StringBuilder();
        foreach (var (_, value, _) in self.ComputeRangeParts(a[0], a.GetAt(1)))
            sb.Append(value);
        return JSValue.CreateString(sb.ToString());
    }

    public static JSValue FormatRangeToPartsPrototype(in Arguments a)
    {
        if (a.This is not JSIntlNumberFormat self)
            throw JSEngine.NewTypeError("Intl.NumberFormat.prototype.formatRangeToParts called on incompatible receiver");

        var typeKey = KeyStrings.GetOrCreate("type");
        var valueKey = KeyStrings.GetOrCreate("value");
        var sourceKey = KeyStrings.GetOrCreate("source");
        var parts = JSValue.CreateArray();
        foreach (var (type, value, source) in self.ComputeRangeParts(a[0], a.GetAt(1)))
        {
            var part = new JSObject();
            part.FastAddValue(typeKey, JSValue.CreateString(type), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(valueKey, JSValue.CreateString(value), JSPropertyAttributes.EnumerableConfigurableValue);
            part.FastAddValue(sourceKey, JSValue.CreateString(source), JSPropertyAttributes.EnumerableConfigurableValue);
            parts.AddArrayItem(part);
        }
        return parts;
    }

    // PartitionNumberRangePattern (Intl.NumberFormat v3): format both endpoints, and
    // when their formatted parts are identical render the single value prefixed by the
    // approximately sign (every part shared); otherwise the start parts (source
    // "startRange"), the locale range separator (shared) and the end parts (source
    // "endRange").
    internal List<(string type, string value, string source)> ComputeRangeParts(JSValue startValue, JSValue endValue)
    {
        var start = CoerceRangeOperand(startValue);
        var end = CoerceRangeOperand(endValue);

        var startParts = ComputeFormatParts(start);
        var endParts = ComputeFormatParts(end);

        static string Concat(List<(string, string)> parts)
        {
            var sb = new StringBuilder();
            foreach (var (_, value) in parts)
                sb.Append(value);
            return sb.ToString();
        }

        var result = new List<(string, string, string)>();
        if (Concat(startParts) == Concat(endParts))
        {
            result.Add(("approximatelySign", "~", "shared"));
            foreach (var (type, value) in startParts)
                result.Add((type, value, "shared"));
            return result;
        }

        foreach (var (type, value) in startParts)
            result.Add((type, value, "startRange"));
        result.Add(("literal", " – ", "shared"));
        foreach (var (type, value) in endParts)
            result.Add((type, value, "endRange"));
        return result;
    }

    // ToIntlMathematicalValue with the range-specific guards: a missing operand or a
    // Symbol is a TypeError, NaN a RangeError. Returns the coerced numeric value.
    private static JSValue CoerceRangeOperand(JSValue value)
    {
        if (value == null || value.IsUndefined)
            throw JSEngine.NewTypeError("Invalid number range");
        if (value.IsSymbol)
            throw JSEngine.NewTypeError("Cannot convert a Symbol value to a number");
        if (value.IsBigInt)
            return value;
        if (double.IsNaN(value.DoubleValue))
            throw JSEngine.NewRangeError("Invalid number range");
        return value;
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
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), JSValue.CreateString(@this.numberingSystem));

        var styleKey = KeyStrings.GetOrCreate("style");
        var style = @this.options is null || @this.options[styleKey].IsUndefined ? "decimal" : @this.options[styleKey].StringValue;
        result.CreateDataProperty(KeyStrings.GetOrCreate("style"), JSValue.CreateString(style));

        if (@this.options != null)
        {
            var currencyKey = KeyStrings.GetOrCreate("currency");
            var unitKey = KeyStrings.GetOrCreate("unit");
            var useGroupingKey = KeyStrings.GetOrCreate("useGrouping");
            var minimumIntegerDigitsKey = KeyStrings.GetOrCreate("minimumIntegerDigits");
            var minimumFractionDigitsKey = KeyStrings.GetOrCreate("minimumFractionDigits");
            var maximumFractionDigitsKey = KeyStrings.GetOrCreate("maximumFractionDigits");
            var minimumSignificantDigitsKey = KeyStrings.GetOrCreate("minimumSignificantDigits");
            var maximumSignificantDigitsKey = KeyStrings.GetOrCreate("maximumSignificantDigits");

            // Spec order: currency, currencyDisplay, currencySign appear together (only when
            // style is "currency"), each with its resolved value.
            // A well-formed currency code is canonicalized to upper case (e.g. "usd" → "USD")
            // before it is reflected by resolvedOptions.
            if (style == "currency")
            {
                if (!@this.options[currencyKey].IsUndefined)
                    result.CreateDataProperty(currencyKey, CanonicalCurrencyValue(@this.options[currencyKey]));
                result.CreateDataProperty(KeyStrings.GetOrCreate("currencyDisplay"),
                    JSValue.CreateString(@this.ReadStringOption("currencyDisplay", "symbol")));
                result.CreateDataProperty(KeyStrings.GetOrCreate("currencySign"),
                    JSValue.CreateString(@this.CurrencySignOption()));
            }
            // A currency option supplied without style:"currency" is validated at construction
            // but is NOT reflected by resolvedOptions (ECMA-402 only emits currency/currencyDisplay/
            // currencySign for the currency style).
            if (!@this.options[unitKey].IsUndefined)
                result.CreateDataProperty(unitKey, @this.options[unitKey]);
            // unitDisplay sits with unit in the resolvedOptions table — before the digit
            // options — and is reflected only when style is "unit" (resolved.UnitDisplay
            // is null otherwise).
            if (@this.resolved?.UnitDisplay != null)
                result.CreateDataProperty(KeyStrings.GetOrCreate("unitDisplay"),
                    JSValue.CreateString(@this.resolved.UnitDisplay));
            // Digit options reflect the construction-time snapshot (read once),
            // not the live options bag, so resolvedOptions does not re-trigger
            // option getters.
            var digits = @this.resolved?.DigitOptions;

            if (digits != null && !digits[minimumIntegerDigitsKey].IsUndefined)
                result.CreateDataProperty(minimumIntegerDigitsKey, digits[minimumIntegerDigitsKey]);
            else
                result.CreateDataProperty(minimumIntegerDigitsKey, JSValue.CreateNumber(1));

            // Fraction-digit reflection mirrors the formatter: the style-dependent defaults
            // (currency → its minor-unit digit count) feed the SetNumberFormatDigitOptions
            // resolution, so e.g. JPY reports 0/0 and USD 2/2 rather than the decimal 0/3.
            var (defMinFrac, defMaxFrac) = @this.DefaultFractionDigits();
            var (minFrac, maxFrac) = @this.ResolveFractionDigits(defMinFrac, defMaxFrac);
            result.CreateDataProperty(minimumFractionDigitsKey, JSValue.CreateNumber(minFrac));
            result.CreateDataProperty(maximumFractionDigitsKey, JSValue.CreateNumber(maxFrac));

            // When either significant-digit option is supplied the other gets its
            // SetNumberFormatDigitOptions default (minimum → 1, maximum → 21), so
            // both are reported together.
            var hasMinSig = digits != null && !digits[minimumSignificantDigitsKey].IsUndefined;
            var hasMaxSig = digits != null && !digits[maximumSignificantDigitsKey].IsUndefined;
            if (hasMinSig || hasMaxSig)
            {
                result.CreateDataProperty(minimumSignificantDigitsKey,
                    hasMinSig ? digits[minimumSignificantDigitsKey] : JSValue.CreateNumber(1));
                result.CreateDataProperty(maximumSignificantDigitsKey,
                    hasMaxSig ? digits[maximumSignificantDigitsKey] : JSValue.CreateNumber(21));
            }

            // useGrouping always has a resolved value ("auto"/"always"/"min2", or boolean
            // false); the spec places it after the digit options and before notation. The
            // "false" sentinel maps back to the boolean.
            var resolvedGrouping = @this.resolved?.UseGrouping ?? "auto";
            result.CreateDataProperty(useGroupingKey,
                resolvedGrouping == "false" ? JSValue.BooleanFalse : JSValue.CreateString(resolvedGrouping));
        }
        else
        {
            result.CreateDataProperty(KeyStrings.GetOrCreate("minimumIntegerDigits"), JSValue.CreateNumber(1));
            result.CreateDataProperty(KeyStrings.GetOrCreate("minimumFractionDigits"), JSValue.CreateNumber(0));
            result.CreateDataProperty(KeyStrings.GetOrCreate("maximumFractionDigits"), JSValue.CreateNumber(3));
            result.CreateDataProperty(KeyStrings.GetOrCreate("useGrouping"), JSValue.CreateString("auto"));
        }

        // notation/signDisplay always have a resolved value; compactDisplay is
        // reflected only when notation is "compact". Per the resolvedOptions table
        // compactDisplay precedes signDisplay (test262 NumberFormat
        // resolvedOptions/return-keys-order-default). These are read from the slots
        // resolved at construction (not the live options object) so getter side
        // effects observe construction-time order, not access.
        var r = @this.resolved;
        if (r != null)
        {
            result.CreateDataProperty(KeyStrings.GetOrCreate("notation"), JSValue.CreateString(r.Notation));
            if (r.CompactDisplay != null)
                result.CreateDataProperty(KeyStrings.GetOrCreate("compactDisplay"), JSValue.CreateString(r.CompactDisplay));
            result.CreateDataProperty(KeyStrings.GetOrCreate("signDisplay"), JSValue.CreateString(r.SignDisplay));

            // SetNumberFormatDigitOptions rounding slots are always present (spec order:
            // after notation/compactDisplay/signDisplay).
            result.CreateDataProperty(KeyStrings.GetOrCreate("roundingIncrement"), JSValue.CreateNumber(r.RoundingIncrement));
            result.CreateDataProperty(KeyStrings.GetOrCreate("roundingMode"), JSValue.CreateString(r.RoundingMode));
            result.CreateDataProperty(KeyStrings.GetOrCreate("roundingPriority"), JSValue.CreateString(r.RoundingPriority));
            result.CreateDataProperty(KeyStrings.GetOrCreate("trailingZeroDisplay"), JSValue.CreateString(r.TrailingZeroDisplay));
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
        var options = JSIntl.ValidateConstructorArguments("Collator", in a, out var canonical, requireNew: false);
        // Keep only the collation-relevant Unicode keywords (co/kf/kn) in the resolved locale, in
        // canonical form (so e.g. "-kn-true" reduces to "-kn").
        locale = JSIntl.ResolveLocaleFromCanonical(canonical, JSIntl.CollatorRelevantKeys);

        if (TryGetUnicodeExtension(locale, "kn", out var kn))
            numeric = kn == "true";
        if (TryGetUnicodeExtension(locale, "kf", out var kf) && (kf == "upper" || kf == "lower" || kf == "false"))
            caseFirst = kf;
        if (TryGetUnicodeExtension(locale, "co", out var co) && JSIntl.CanonicalizeCollation(co) is { } tagCo)
            collation = tagCo;

        // usage / sensitivity / caseFirst are constrained string options: a provided value outside
        // the allowed set is a RangeError (ECMA-402 GetOption), so route them through the shared
        // validating helper rather than reading the raw string. The current field value is passed as
        // the default so an absent option keeps the locale-derived / spec default.
        usage = JSIntl.GetOption(options, KeyStrings.GetOrCreate("usage"), ["sort", "search"], false, usage);
        sensitivity = JSIntl.GetOption(options, KeyStrings.GetOrCreate("sensitivity"), ["base", "accent", "case", "variant"], false, sensitivity);
        if (TryGetOwnOption(options, "ignorePunctuation", out var ignorePunctuationValue))
            ignorePunctuation = ignorePunctuationValue.BooleanValue;
        var numericPresent = TryGetOwnOption(options, "numeric", out var numericValue);
        if (numericPresent)
            numeric = numericValue.BooleanValue;
        var caseFirstKey = KeyStrings.GetOrCreate("caseFirst");
        var caseFirstPresent = options != null && options[caseFirstKey] is { IsUndefined: false };
        caseFirst = JSIntl.GetOption(options, caseFirstKey, ["upper", "lower", "false"], false, caseFirst);
        if (TryGetOwnOption(options, "collation", out var collationValue)
            && JSIntl.CanonicalizeCollation(collationValue.StringValue) is { } optCo)
            collation = optCo;

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

        // A reserved ("standard"/"search") or unsupported -u-co- value is not a real collation, so it
        // is dropped from the resolved locale (test262 Collator/ignore-invalid-unicode-ext-values).
        if (TryGetUnicodeExtension(locale, "co", out var coExt) && JSIntl.CanonicalizeCollation(coExt) == null)
            locale = JSIntl.RemoveUnicodeExtensionKeyword(locale, "co");

        compareInfo = ResolveCompareInfo(locale, collation);
    }

    // CompareInfo for the resolved locale, honouring the Unicode-extension collation tailoring
    // (-u-co-). .NET ships a small set of named collation variants accessible via the BCP-47
    // CultureInfo path: GetCultureInfo("de-DE-u-co-phonebk").CompareInfo tailors Ä to Ae
    // (CompareInfo.GetCompareInfo("de-DE_phoneboo") returns a CompareInfo with the same Name
    // but does NOT actually apply the tailoring — only the full BCP-47 culture-construction
    // path is wired through to the ICU tailored collator). Falls back to the locale's default
    // CompareInfo (or the invariant one) when the tailoring isn't recognised.
    private static CompareInfo ResolveCompareInfo(string localeTag, string collation)
    {
        var bareLocale = StripUnicodeExtension(localeTag);
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

    private JSIntlCollator() : base(CurrentPrototype()) { }

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
        if (!left.IsNormalized(System.Text.NormalizationForm.FormD))
            left = left.Normalize(System.Text.NormalizationForm.FormD);
        if (!right.IsNormalized(System.Text.NormalizationForm.FormD))
            right = right.Normalize(System.Text.NormalizationForm.FormD);

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
    private static readonly KeyString CalendarKey = KeyStrings.GetOrCreate("calendar");
    private static readonly KeyString NumberingSystemKey = KeyStrings.GetOrCreate("numberingSystem");
    private static readonly KeyString WeekdayKey = KeyStrings.GetOrCreate("weekday");
    private static readonly KeyString EraKey = KeyStrings.GetOrCreate("era");
    private static readonly KeyString TimeZoneNameKey = KeyStrings.GetOrCreate("timeZoneName");
    private static readonly KeyString FormatMatcherKey = KeyStrings.GetOrCreate("formatMatcher");

    // The sanctioned value sets for the date/time option getters (CreateDateTimeFormat).
    private static readonly string[] NarrowShortLong = { "narrow", "short", "long" };
    private static readonly string[] NumericTwoDigit = { "2-digit", "numeric" };
    private static readonly string[] MonthValues = { "2-digit", "numeric", "narrow", "short", "long" };
    private static readonly string[] HourCycleValues = { "h11", "h12", "h23", "h24" };
    private static readonly string[] TimeZoneNameValues =
        { "short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric" };
    private static readonly string[] FormatMatcherValues = { "basic", "best fit" };
    private readonly CultureInfo locale;
    private readonly string localeTag;
    private readonly string numberingSystem;
    private JSObject options;

    // dateStyle / timeStyle are GetOption-coerced (to a string) and validated once at
    // construction, then reported verbatim by resolvedOptions. Reading them from the
    // raw user options object would surface the original (possibly non-string) value
    // — e.g. `{ dateStyle: { toString() { return "full"; } } }` must resolve to the
    // string "full", not the object.
    internal static readonly string[] DateTimeStyleValues = { "full", "long", "medium", "short" };
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
        var dayPeriod = DayPeriodName(JSDateMath.HourFromTime(wall), JSDateMath.MinFromTime(wall));
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
    {
        "weekday", "era", "year", "month", "day", "dayPeriod",
        "hour", "minute", "second", "fractionalSecondDigits", "timeZoneName",
    };

    private static readonly string[] DateTimeFormatOptionKeys =
    {
        "localeMatcher", "calendar", "numberingSystem", "hour12", "hourCycle", "timeZone",
        "weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second",
        "fractionalSecondDigits", "timeZoneName", "formatMatcher", "dateStyle", "timeStyle",
    };

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
            resolved[TimeZoneKey] = JSValue.CreateString(defaultTimeZone);
            options = resolved;
        }

        var dtf = new JSIntlDateTimeFormat(new Arguments(JSUndefined.Value, locales, options ?? JSUndefined.Value));
        return new JSString(JSIntlDateTimeFormatEngine.PartsToString(dtf.FormatTemporalToParts(temporal, zonedAllowed: true, enforceStyle: true)));
    }

    // The date/time component options whose presence suppresses defaulting (era / timeZoneName are
    // supplementary and do not count).
    private static readonly string[] DateTimeNeedsDefaultsKeys =
    {
        "weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits",
    };

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
            merged[KeyStrings.GetOrCreate(name)] = JSValue.CreateString("numeric");
        return merged;
    }

    private static readonly string[] DateComponentKeys = { "weekday", "year", "month", "day" };
    private static readonly string[] TimeComponentKeys =
        { "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits" };

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
                merged[KeyStrings.GetOrCreate(name)] = JSValue.CreateString("numeric");
        if (needDefaults && defaults is "time" or "all")
            foreach (var name in new[] { "hour", "minute", "second" })
                merged[KeyStrings.GetOrCreate(name)] = JSValue.CreateString("numeric");

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
        return JSIntlDateTimeFormatEngine.FormatToParts(pattern, in fields, FractionalSecondDigits(), null);
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
        if (a.Length > 0 && a[0] != null && IsTemporalDateTime(a[0]))
            return new JSString(JSIntlDateTimeFormatEngine.PartsToString(FormatTemporalToParts(a[0])));

        var value = a.Length == 0 || a[0] == null || a[0].IsUndefined
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : a.Get1().DoubleValue;
        var clipped = JSDateMath.TimeClip(value);
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
        var clipped = JSDateMath.TimeClip(value);
        if (double.IsNaN(clipped))
            throw JSEngine.NewRangeError("Invalid time value");

        if (@this.SupportsDayPeriod())
        {
            var (h, m) = @this.WallHourMinute(clipped);
            var dayPeriodParts = JSValue.CreateArray();
            if (@this.UsesHourFormatting())
            {
                var hourPart = new JSObject();
                hourPart[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("hour");
                hourPart[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(FormatEnglishHour(h));
                var literalPart = new JSObject();
                literalPart[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("literal");
                literalPart[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(" ");
                dayPeriodParts.AddArrayItem(hourPart);
                dayPeriodParts.AddArrayItem(literalPart);
            }

            var dayPeriodPart = new JSObject();
            dayPeriodPart[KeyStrings.GetOrCreate("type")] = JSValue.CreateString("dayPeriod");
            dayPeriodPart[KeyStrings.GetOrCreate("value")] = JSValue.CreateString(@this.DayPeriodNameForStyle(h, m, @this.DayPeriodStyle()));
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
                    sb.Append(c is >= '0' and <= '9' ? JSIntlNumberFormat.MapDigit(numberingSystem, c - '0') : c.ToString());
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
        // resolvedOptions reports dtf.[[Calendar]] — the resolved calendar IDENTIFIER (e.g. "iso8601",
        // not collapsed to "gregory") after the ResolveLocale `ca` negotiation, NOT the raw option.
        result.CreateDataProperty(KeyStrings.GetOrCreate("calendar"), JSValue.CreateString(@this.ResolvedCalendarId()));
        result.CreateDataProperty(KeyStrings.GetOrCreate("numberingSystem"), JSValue.CreateString(@this.numberingSystem));
        // The default time zone must be reported as a CANONICAL identifier — a host whose local
        // zone is "Etc/UTC" (e.g. a UTC container) reports "UTC", not "Etc/UTC" (test262
        // DateTimeFormat/prototype/resolvedOptions/basic). SystemTimeZoneId normalizes Windows ids
        // to IANA (falling back to UTC); CanonicalizeTimeZoneId then applies the UTC-cluster rule.
        result.CreateDataProperty(KeyStrings.GetOrCreate("timeZone"),
            JSValue.CreateString(Temporal.JSTemporalZonedDateTime.CanonicalizeTimeZoneId(Temporal.JSTemporalNow.SystemTimeZoneId())));

        if (@this.options != null)
        {
            JSIntlResolvedOptionsExtensions.SetIfDefined(result, @this.options, "timeZone");
            // hourCycle / hour12 are RESOLVED values (not plain passthrough): when the
            // format includes an hour component, both are always present — hourCycle
            // resolved from the hour12/hourCycle options (defaulting per locale) and
            // hour12 derived to match. They appear right after timeZone per the spec.
            if (@this.UsesHourFormatting())
            {
                result.CreateDataProperty(KeyStrings.GetOrCreate("hourCycle"), JSValue.CreateString(@this.ResolveHourCycle()));
                result.CreateDataProperty(KeyStrings.GetOrCreate("hour12"), @this.ResolveHour12() ? JSValue.BooleanTrue : JSValue.BooleanFalse);
            }
            // dateStyle / timeStyle report the coerced+validated string captured at
            // construction (not the raw option value).
            if (@this.dateStyle != null)
                result.CreateDataProperty(DateStyleKey, JSValue.CreateString(@this.dateStyle));
            if (@this.timeStyle != null)
                result.CreateDataProperty(TimeStyleKey, JSValue.CreateString(@this.timeStyle));
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
        return (JSDateMath.HourFromTime(wall), JSDateMath.MinFromTime(wall));
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
                snapshot[key] = JSValue.CreateString(value);
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
            snapshot[Hour12Key] = hour12.BooleanValue ? JSValue.BooleanTrue : JSValue.BooleanFalse;
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
            snapshot[FractionalSecondDigitsKey] = JSValue.CreateNumber(fractionalSecondDigits.Value);

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
                snapshot[YearKey] = JSValue.CreateString("numeric");
                snapshot[MonthKey] = JSValue.CreateString("numeric");
                snapshot[DayKey] = JSValue.CreateString("numeric");
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

    // Reads and validates the timeZone option (CreateDateTimeFormat): an offset
    // identifier (leading + / -) is validated against the ECMA-402 grammar and
    // normalized to ±HH:MM; a U+2212 minus or a malformed offset is a RangeError; a
    // named IANA zone is returned untouched. Returns null when the option is absent.
    private static string ReadTimeZoneOption(JSObject userOptions)
    {
        var value = userOptions == null ? JSUndefined.Value : userOptions[TimeZoneKey];
        if (value.IsUndefined)
            return null;

        var timeZone = value.StringValue;
        if (timeZone.Contains('−'))
            throw JSEngine.NewRangeError("Invalid timeZone option");
        if (timeZone.Length > 0 && (timeZone[0] == '+' || timeZone[0] == '-'))
        {
            if (JSIntl.TryNormalizeOffsetTimeZone(timeZone, out var normalized))
                return normalized;
            throw JSEngine.NewRangeError($"Invalid timeZone option: {timeZone}");
        }

        // A named time zone is validated against the IANA database and case-normalized — "utc" → "UTC",
        // "africa/abidjan" → "Africa/Abidjan" — but NOT canonicalized through backward links
        // ("Asia/Calcutta" stays "Asia/Calcutta"); an unknown or legacy non-IANA name is a RangeError.
        return Temporal.JSTemporalZonedDateTime.CanonicalizeTimeZoneId(timeZone);
    }

    internal JSIntlDateTimeFormat(CultureInfo locale) : base()
    {
        this.locale = locale;
        localeTag = locale.Name;
        numberingSystem = "latn";
    }

    private static JSObject CurrentPrototype()
        => (JSEngine.CurrentContext as JSObject)?[KeyStrings.GetOrCreate("Intl")] is JSObject intl
            ? (intl[KeyStrings.GetOrCreate("DateTimeFormat")] as JSFunction)?.prototype
            : null;

    private static double TimeClipRange(double number)
    {
        var clipped = JSDateMath.TimeClip(number);
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
