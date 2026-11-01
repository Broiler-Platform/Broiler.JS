// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Locale.cs
//     blob d70618ca673e4d30b47b1e3424d7a734253b68ef: the whole file: JsRealm's Intl.Locale part and JsLocaleObject
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.Locale</c> (ECMA-402 s15; JSD-0046): a canonicalized locale identifier with its parts,
/// its keywords as properties, Add and Remove Likely Subtags, and the locale's information.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Locale is any well-formed tag, not only a supported one.</b> Its parts and keywords come from
/// the tag and the options; Add and Remove Likely Subtags and the aliases read CLDR's data for every
/// language, as canonicalization already does.
/// </para>
/// <para>
/// <b>The information methods answer from the data the profile holds</b>: the calendars and numbering
/// systems it formats, the collations <c>Intl.Collator</c> offers, CLDR's hour cycles and weeks for
/// every region and its line direction for every script. Since JSD-0053 the time zones in use in a
/// region are the IANA Time Zone Database's <c>zone.tab</c> entries for it.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.Locale.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? LocalePrototype { get; private set; }

    /// <summary><c>%Intl.Locale%</c>, or nothing where the surface is not built.</summary>
    internal JsNativeFunction? LocaleConstructor { get; private set; }

    /// <summary>%Intl.Locale%.[[LocaleExtensionKeys]]: the five ECMA-402 requires, and the collator's two.</summary>
    private static readonly string[] LocaleExtensionKeys = ["ca", "co", "fw", "hc", "kf", "kn", "nu"];

    /// <summary>ECMA-402's table of weekday strings, by their position in the week from Sunday at 0 and 7.</summary>
    private static readonly string[] WeekdayKeys = ["sun", "mon", "tue", "wed", "thu", "fri", "sat", "sun"];

    private void SetupLocale(JsObject intl)
    {
        var prototype = new JsObject(ObjectPrototype);
        LocalePrototype = prototype;

        var constructor = new JsNativeFunction(this, FunctionPrototype, "Locale", 1,
            static (engine, thisValue, arguments) => throw engine.Error("TypeError", "Constructor Intl.Locale requires 'new'"),
            static (engine, newTarget, arguments) => NewLocale(engine, newTarget, arguments))
        {
            BuildsFromNewTarget = true
        };
        LocaleConstructor = constructor;

        constructor.SetOwnProperty("prototype", JsProperty.Data(JsValue.Object(prototype), JsPropertyAttributes.None));
        prototype.SetOwnProperty("constructor", JsProperty.Data(JsValue.Object(constructor), JsPropertyAttributes.Writable | JsPropertyAttributes.Configurable));

        void Getter(string name, System.Func<JsEngine, JsLocaleObject, JsValue> read) =>
            prototype.SetOwnProperty(name, JsProperty.Accessor(Native("get " + name, 0, (engine, thisValue, arguments) =>
            read(engine, LocaleOfThis(engine, thisValue, name))), null, JsPropertyAttributes.Configurable));

        static JsValue Optional(string? value) => value is null ? JsValue.Undefined : JsValue.String(value);

        Getter("baseName", static (engine, locale) => JsValue.String(Tag(locale.Locale).LanguageId()));
        Getter("calendar", static (engine, locale) => Optional(locale.Calendar));
        Getter("caseFirst", static (engine, locale) => Optional(locale.CaseFirst));
        Getter("collation", static (engine, locale) => Optional(locale.Collation));
        Getter("firstDayOfWeek", static (engine, locale) => Optional(locale.FirstDayOfWeek));
        Getter("hourCycle", static (engine, locale) => Optional(locale.HourCycle));
        Getter("language", static (engine, locale) => JsValue.String(Tag(locale.Locale).Language));
        Getter("numberingSystem", static (engine, locale) => Optional(locale.NumberingSystem));
        Getter("numeric", static (engine, locale) => JsValue.Boolean(locale.Numeric));
        Getter("region", static (engine, locale) => Optional(Tag(locale.Locale).Region));
        Getter("script", static (engine, locale) => Optional(Tag(locale.Locale).Script));
        Getter("variants", static (engine, locale) =>
            Tag(locale.Locale).Variants is { Count: > 0 } variants ? JsValue.String(string.Join('-', variants)) : JsValue.Undefined);

        Method(prototype, "maximize", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "maximize");
            var tag = Tag(locale.Locale);
            _ = tag.Maximize(engine.Intl!);
            return JsValue.Object(NewLocaleObject(engine, engine.Realm.LocalePrototype!, tag.ToString(), JsValue.Undefined));
        });

        Method(prototype, "minimize", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "minimize");
            var tag = Tag(locale.Locale);
            _ = tag.Minimize(engine.Intl!);
            return JsValue.Object(NewLocaleObject(engine, engine.Realm.LocalePrototype!, tag.ToString(), JsValue.Undefined));
        });

        Method(prototype, "toString", 0, static (engine, thisValue, arguments) =>
            JsValue.String(LocaleOfThis(engine, thisValue, "toString").Locale));

        Method(prototype, "getCalendars", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getCalendars");
            return StringArray(engine, [locale.Calendar ?? "gregory"]);
        });

        Method(prototype, "getCollations", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getCollations");

            if (locale.Collation is not null)
            {
                return StringArray(engine, [locale.Collation]);
            }

            if (LookupMatchingLocale(AvailableLocales(engine), [locale.Locale]) is { } match)
            {
                var list = new List<string>();

                foreach (var value in CollatorKeyData(match.Locale, "co", "sort"))
                {
                    if (value is not null)
                    {
                        list.Add(value);
                    }
                }

                list.Sort(System.StringComparer.Ordinal);
                return StringArray(engine, list);
            }

            return StringArray(engine, ["emoji", "eor"]);
        });

        Method(prototype, "getHourCycles", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getHourCycles");
            return StringArray(engine, locale.HourCycle is not null ? [locale.HourCycle] : HourCyclesOf(engine, locale.Locale));
        });

        Method(prototype, "getNumberingSystems", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getNumberingSystems");
            return StringArray(engine, [locale.NumberingSystem ?? "latn"]);
        });

        Method(prototype, "getTimeZones", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getTimeZones");

            // THE ZONES IN USE IN A REGION are zone.tab's for it, each a primary identifier
            // (ECMA-402 6.5 and 15.5.13, JSD-0053), in ordinal order: the shared database's answer
            // since SJL-8.5 (JSD-0076).
            return Tag(locale.Locale).Region is not { } region
                ? JsValue.Undefined
                : StringArray(engine, [.. engine.Intl!.TimeZones.RegionTimeZones(region)]);
        });

        Method(prototype, "getTextInfo", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getTextInfo");
            var info = new JsObject(engine.Realm.ObjectPrototype);
            info.DefineOrdinary("direction", Optional(TextDirectionOf(engine, locale.Locale)));
            return JsValue.Object(info);
        });

        Method(prototype, "getWeekInfo", 0, static (engine, thisValue, arguments) =>
        {
            var locale = LocaleOfThis(engine, thisValue, "getWeekInfo");
            var (firstDay, weekend) = WeekInfoOf(engine, locale);
            var info = new JsObject(engine.Realm.ObjectPrototype);
            info.DefineOrdinary("firstDay", JsValue.Number(firstDay));
            info.DefineOrdinary("weekend", JsValue.Object(engine.Realm.NewArray(weekend.ConvertAll(static day => JsValue.Number(day)))));
            return JsValue.Object(info);
        });

        prototype.SetOwnSymbol(ToStringTagSymbol, JsProperty.Data(JsValue.String("Intl.Locale"), JsPropertyAttributes.Configurable));
        intl.SetOwnProperty("Locale", JsProperty.Data(JsValue.Object(constructor), JsPropertyAttributes.Writable | JsPropertyAttributes.Configurable));
    }

    /// <summary>The parsed form of a canonicalized locale identifier a Locale holds.</summary>
    private static JsLocaleTag Tag(string locale) => JsLocaleTag.Parse(locale)!;

    private static JsValue StringArray(JsEngine engine, List<string> values) =>
        JsValue.Object(engine.Realm.NewArray(values.ConvertAll(static value => JsValue.String(value))));

    /// <summary>The Locale <paramref name="thisValue"/> is, or the <c>TypeError</c>.</summary>
    private static JsLocaleObject LocaleOfThis(JsEngine engine, JsValue thisValue, string member) =>
        thisValue.AsObjectOrNull() as JsLocaleObject ??
        throw engine.Error("TypeError", "Intl.Locale.prototype." + member + " requires that 'this' be an Intl.Locale");

    /// <summary>ECMA-402's <c>Intl.Locale</c> constructor (s15.1.1).</summary>
    internal static JsValue NewLocale(JsEngine engine, JsValue newTarget, JsValue[] arguments)
    {
        var prototype = engine.PrototypeFromConstructor(newTarget, engine.Realm.LocalePrototype!);
        var tag = Argument(arguments, 0);

        if (!tag.IsString && !tag.IsObject)
        {
            throw engine.Error("TypeError", "Intl.Locale: the tag is neither a String nor an Object");
        }

        var text = tag.AsObjectOrNull() is JsLocaleObject existing ? existing.Locale : engine.ToStringValue(tag);
        return JsValue.Object(NewLocaleObject(engine, prototype, text, Argument(arguments, 1)));
    }

    /// <summary>The steps of the constructor after the tag is a String: the options read in order, and the record made.</summary>
    private static JsLocaleObject NewLocaleObject(JsEngine engine, JsObject prototype, string text, JsValue optionsValue)
    {
        var tables = engine.Intl!;
        var options = CoerceOptionsToObject(engine, optionsValue);

        if (JsLocaleTag.Parse(text) is not { } parsed)
        {
            throw engine.Error("RangeError", "Incorrect locale information provided: " + text);
        }

        text = UpdateLanguageId(engine, parsed.Canonicalize(tables).ToString(), options);

        string? TypeOption(string property)
        {
            var value = GetStringOption(engine, options, property, null, null);

            if (value is not null && !IsTypeSequence(value))
            {
                throw engine.Error("RangeError", "Intl.Locale: the " + property + " option is not a well-formed type: " + value);
            }

            return value;
        }

        var overrides = new Dictionary<string, string?>(System.StringComparer.Ordinal)
        {
            ["ca"] = TypeOption("calendar"),
            ["co"] = TypeOption("collation")
        };

        var firstDay = GetStringOption(engine, options, "firstDayOfWeek", null, null);

        if (firstDay is not null)
        {
            if (firstDay.Length == 1 && firstDay[0] is >= '0' and <= '7')
            {
                firstDay = WeekdayKeys[firstDay[0] - '0'];
            }

            if (!IsTypeSequence(firstDay))
            {
                throw engine.Error("RangeError", "Intl.Locale: the firstDayOfWeek option is not a well-formed type: " + firstDay);
            }
        }

        overrides["fw"] = firstDay;
        overrides["hc"] = GetStringOption(engine, options, "hourCycle", ["h11", "h12", "h23", "h24"], null);
        overrides["kf"] = GetStringOption(engine, options, "caseFirst", ["upper", "lower", "false"], null);
        overrides["kn"] = GetBooleanOption(engine, options, "numeric", null) is { } numeric ? (numeric ? "true" : "false") : null;
        overrides["nu"] = TypeOption("numberingSystem");

        // MAKELOCALERECORD: each key from the tag's keywords, overridden by its option.
        var record = JsLocaleTag.Parse(text)!;
        var values = new Dictionary<string, string?>(System.StringComparer.Ordinal);

        foreach (var key in LocaleExtensionKeys)
        {
            var entry = record.Keywords.FindIndex(pair => pair.Key == key);
            var value = entry >= 0 ? record.Keywords[entry].Type : null;

            if (overrides[key] is { } overrideValue)
            {
                value = CanonicalizeUValue(tables, key, overrideValue);

                if (entry >= 0)
                {
                    record.Keywords[entry] = (key, value);
                }
                else
                {
                    record.Keywords.Add((key, value));
                }
            }

            values[key] = value;
        }

        record.HasUnicodeExtension = record.Attributes.Count != 0 || record.Keywords.Count != 0;
        var locale = record.Canonicalize(tables).ToString();

        return new JsLocaleObject(prototype)
        {
            Locale = locale,
            Calendar = values["ca"],
            Collation = values["co"],
            FirstDayOfWeek = values["fw"],
            HourCycle = values["hc"],
            CaseFirst = values["kf"],
            Numeric = values["kn"] is "true" or "",
            NumberingSystem = values["nu"],
        };
    }

    /// <summary>ECMA-402's CanonicalizeUValue: the lower-case value, a deprecated one replaced, and <c>true</c> left implicit.</summary>
    private static string CanonicalizeUValue(JsIntlTables tables, string key, string value)
    {
        var lower = JsLocaleTag.AsciiLower(value);

        if (tables.TryType('u', key, lower, out var preferred))
        {
            lower = preferred;
        }

        return lower == "true" ? string.Empty : lower;
    }

    /// <summary>ECMA-402's UpdateLanguageId (s15.1.2): the language, script, region and variants the options replace.</summary>
    private static string UpdateLanguageId(JsEngine engine, string tag, JsValue options)
    {
        var parsed = JsLocaleTag.Parse(tag)!;
        var baseName = parsed.LanguageId();

        static bool Letters(string text, int least, int most) =>
            text.Length >= least && text.Length <= most && JsLocaleTag.IsAlpha(text);

        var language = GetStringOption(engine, options, "language", null, parsed.Language)!;

        if (!(Letters(language, 2, 3) || Letters(language, 5, 8)))
        {
            throw engine.Error("RangeError", "Intl.Locale: invalid language subtag: " + language);
        }

        var script = GetStringOption(engine, options, "script", null, parsed.Script);

        if (script is not null && !Letters(script, 4, 4))
        {
            throw engine.Error("RangeError", "Intl.Locale: invalid script subtag: " + script);
        }

        var region = GetStringOption(engine, options, "region", null, parsed.Region);

        if (region is not null && !(Letters(region, 2, 2) || (region.Length == 3 && Digits(region))))
        {
            throw engine.Error("RangeError", "Intl.Locale: invalid region subtag: " + region);
        }

        var variants = GetStringOption(engine, options, "variants", null, parsed.Variants.Count == 0 ? null : string.Join('-', parsed.Variants));

        if (variants is not null)
        {
            if (variants.Length == 0)
            {
                throw engine.Error("RangeError", "Intl.Locale: the variants option is empty");
            }

            // ASCII-lowercase, not the platform's casing, which would lower U+212A KELVIN SIGN to `k`
            // and let it through the check below.
            var lower = JsLocaleTag.AsciiLower(variants);
            var seen = new List<string>();

            foreach (var variant in lower.Split('-'))
            {
                if (!JsLocaleTag.IsVariant(variant) || seen.Contains(variant))
                {
                    throw engine.Error("RangeError", "Intl.Locale: invalid variants: " + variants);
                }

                seen.Add(variant);
            }
        }

        var extensions = tag[baseName.Length..];
        var updated = new System.Text.StringBuilder(language);

        if (script is not null)
        {
            updated.Append('-').Append(script);
        }

        if (region is not null)
        {
            updated.Append('-').Append(region);
        }

        if (variants is not null)
        {
            updated.Append('-').Append(variants);
        }

        return updated.Append(extensions).ToString();
    }

    /// <summary>
    /// ECMA-402's RegionPreference (s15.5.8): the locale's region, its <c>-u-sd</c> subdivision's, or
    /// its likely one, and its <c>-u-rg</c> override.
    /// </summary>
    private static (string Region, string? Override) RegionPreference(JsEngine engine, string locale)
    {
        var tables = engine.Intl!;
        var tag = Tag(locale);
        var region = tag.Region ?? Subdivision(engine, tag, "sd");

        if (region is null)
        {
            var maximal = Tag(locale);
            _ = maximal.Maximize(tables);
            region = maximal.Canonicalize(tables).Region ?? "001";
        }

        return (region, Subdivision(engine, tag, "rg"));
    }

    /// <summary>ECMA-402's CanonicalUnicodeSubdivision (s15.5.7): the region a subdivision keyword names.</summary>
    private static string? Subdivision(JsEngine engine, JsLocaleTag tag, string key)
    {
        var entry = tag.Keywords.FindIndex(pair => pair.Key == key);

        if (entry < 0)
        {
            return null;
        }

        var value = tag.Keywords[entry].Type;
        var prefix = value.Length >= 3 && char.IsAsciiDigit(value[0]) ? 3 : 2;

        if (value.Length < prefix + 1 || value.Length > prefix + 4 || !JsLocaleTag.IsAlphanumeric(value) ||
            !(prefix == 3 ? Digits(value[..3]) : JsLocaleTag.IsAlpha(value[..2])))
        {
            return null;
        }

        return JsLocaleTag.Parse("und-" + value[..prefix])?.Canonicalize(engine.Intl!).Region;
    }

    private static bool Digits(string text)
    {
        foreach (var c in text)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>ECMA-402's HourCyclesOfLocale (s15.5.11): CLDR's time data for the preferred regions, preferred first.</summary>
    private static List<string> HourCyclesOf(JsEngine engine, string locale)
    {
        var (region, regionOverride) = RegionPreference(engine, locale);
        var language = Tag(locale).Language;
        var times = engine.Intl!.Dates.TimeData;
        var cycles = new List<string>();

        foreach (var preferred in regionOverride is null ? new[] { region } : [regionOverride, region])
        {
            if (cycles.Count != 0)
            {
                break;
            }

            if (!times.TryGetValue(language + "_" + preferred, out var data) && !times.TryGetValue(preferred, out data))
            {
                continue;
            }

            foreach (var format in (data.Preferred + " " + data.Allowed).Split(' '))
            {
                var cycle = format.Length == 0 ? null : format[0] switch
                {
                    'h' => "h12",
                    'H' => "h23",
                    'K' => "h11",
                    'k' => "h24",
                    _ => null,
                };

                if (cycle is not null && !cycles.Contains(cycle))
                {
                    cycles.Add(cycle);
                }
            }
        }

        return cycles.Count == 0 ? ["h23"] : cycles;
    }

    /// <summary>ECMA-402's TextDirectionOfLocale (s15.5.14): the line direction of the locale's script, or its likely one's.</summary>
    private static string? TextDirectionOf(JsEngine engine, string locale)
    {
        var tables = engine.Intl!;
        var tag = Tag(locale);

        if (tag.Script is null && !tag.Maximize(tables))
        {
            return null;
        }

        return tag.Script is { } script && tables.LocaleInfo.RightToLeft.TryGetValue(script, out var rightToLeft)
            ? rightToLeft ? "rtl" : "ltr"
            : null;
    }

    /// <summary>ECMA-402's WeekInfoOfLocale (s15.5.17): CLDR's first day and weekend for the preferred region, the keyword's first day over it.</summary>
    private static (int FirstDay, List<int> Weekend) WeekInfoOf(JsEngine engine, JsLocaleObject locale)
    {
        var weeks = engine.Intl!.LocaleInfo.Weeks;
        var (region, regionOverride) = RegionPreference(engine, locale.Locale);
        var lookup = regionOverride is not null && weeks.ContainsKey(regionOverride) ? regionOverride : weeks.ContainsKey(region) ? region : "001";
        var (FirstDay, WeekendStart, WeekendEnd, _) = weeks["001"];
        var week = weeks[lookup];

        static int Day(string key) => key switch
        {
            "mon" => 1,
            "tue" => 2,
            "wed" => 3,
            "thu" => 4,
            "fri" => 5,
            "sat" => 6,
            "sun" => 7,
            _ => 0,
        };

        var firstDay = Day(week.FirstDay.Length != 0 ? week.FirstDay : FirstDay);
        var start = Day(week.WeekendStart.Length != 0 ? week.WeekendStart : WeekendStart);
        var end = Day(week.WeekendEnd.Length != 0 ? week.WeekendEnd : WeekendEnd);
        var weekend = new List<int>();

        for (var day = start; ; day = (day % 7) + 1)
        {
            weekend.Add(day);

            if (day == end || weekend.Count == 7)
            {
                break;
            }
        }

        weekend.Sort();

        if (locale.FirstDayOfWeek is { } keyword && Day(keyword) is > 0 and var overridden)
        {
            firstDay = overridden;
        }

        return (firstDay, weekend);
    }
}

/// <summary>An <c>Intl.Locale</c>: its canonicalized identifier and its keywords.</summary>
internal sealed class JsLocaleObject : JsObject
{
    internal JsLocaleObject(JsObject prototype) : base(prototype) { }

    internal string Locale { get; init; } = string.Empty;

    internal string? Calendar { get; init; }

    internal string? Collation { get; init; }

    internal string? FirstDayOfWeek { get; init; }

    internal string? HourCycle { get; init; }

    internal string? CaseFirst { get; init; }

    internal bool Numeric { get; init; }

    internal string? NumberingSystem { get; init; }
}
