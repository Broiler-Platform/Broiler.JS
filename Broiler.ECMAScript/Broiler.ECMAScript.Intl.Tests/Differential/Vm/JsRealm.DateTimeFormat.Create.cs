// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.DateTimeFormat.cs
//     blob bff1811cd34a91f50a9840a2bfc4de81bfb31161: JsRealm's Intl.DateTimeFormat part, of which CreateDateTimeFormat, the Temporal formats' patterns, the hour-cycle letters, the skeletons, the style patterns, ComponentsOf and ResolveTimeZone are kept, with the types JsDateTimeFormatObject and JsTemporalDateFormats; it is a second copy of the blob stage SJL-9.3a copied as `JsRealm.DateTimeFormat.cs`, and the two hold no member in common
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: every member of JsRealm but `DateTimeComponents`, `CreateDateTimeFormat`, `TemporalFormatPattern`, `AdjustStylePattern`, `HourCycleOfChar`, `CharOfHourCycle`, `HourCycleOf`, `ReplaceHourCycle`, `SkeletonOfComponents`, `StylePattern`, `ComponentsOf` and `ResolveTimeZone`: the prototype and its methods, which build script objects, the Temporal objects' reads, which need the Temporal realm, the legacy constructor, and the three members the first copy holds.

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.DateTimeFormat</c>, the three <c>Date.prototype.toLocale*</c> methods through it, and
/// <c>Intl.supportedValuesOf</c> (ECMA-402 s11, s8.3.2; JSD-0045).
/// </summary>
/// <remarks>
/// <para>
/// <b>The constructor reads its options in ECMA-402's order</b> and keeps what it resolved. A format
/// built from components takes its pattern from <see cref="JsDatePatternGenerator"/> and one built
/// from a style takes CLDR's style patterns; both are written with the resolved hour cycle's letter.
/// </para>
/// <para>
/// <b>The time zones are the IANA Time Zone Database's</b> (JSD-0053), from tzdb 2026e. An offset
/// resolves to its <c>+HH:MM</c> or <c>-HH:MM</c> form, and a name, in any ASCII case, to its primary
/// identifier: a name resolving to UTC to <c>UTC</c>, IANA's <c>Etc/GMT+N</c> and <c>Etc/GMT-N</c>
/// zones to themselves as fixed offsets, and any other to its zone, whose offset is read at each
/// instant formatted. Any other name is a <c>RangeError</c>.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary>ECMA-402's table of date-time components, in table order, with the values each admits.</summary>
    private static readonly (string Name, string[]? Values)[] DateTimeComponents =
    [
        ("weekday", ["narrow", "short", "long"]),
        ("era", ["narrow", "short", "long"]),
        ("year", ["2-digit", "numeric"]),
        ("month", ["2-digit", "numeric", "narrow", "short", "long"]),
        ("day", ["2-digit", "numeric"]),
        ("dayPeriod", ["narrow", "short", "long"]),
        ("hour", ["2-digit", "numeric"]),
        ("minute", ["2-digit", "numeric"]),
        ("second", ["2-digit", "numeric"]),
        ("fractionalSecondDigits", null),
        ("timeZoneName", ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"]),
    ];

    /// <summary>ECMA-402's CreateDateTimeFormat (s11.1.2).</summary>
    internal static JsDateTimeFormatObject CreateDateTimeFormat(JsEngine engine, JsObject prototype, JsValue locales, JsValue optionsValue, string required, string defaults, string? toLocaleStringTimeZone = null)
    {
        var requested = CanonicalizeLocaleList(engine, locales);
        var options = CoerceOptionsToObject(engine, optionsValue);

        // RESOLVEOPTIONS: the locale matcher, then each resolution option in the constructor's order.
        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");
        var calendar = GetStringOption(engine, options, "calendar", null, null);

        if (calendar is not null && !IsTypeSequence(calendar))
        {
            throw engine.Error("RangeError", "Intl.DateTimeFormat: the calendar option is not a well-formed type: " + calendar);
        }

        var numberingSystem = GetStringOption(engine, options, "numberingSystem", null, null);

        if (numberingSystem is not null && !IsTypeSequence(numberingSystem))
        {
            throw engine.Error("RangeError", "Intl.DateTimeFormat: the numberingSystem option is not a well-formed type: " + numberingSystem);
        }

        var hour12 = GetBooleanOption(engine, options, "hour12", null);
        var hourCycle = GetStringOption(engine, options, "hourCycle", ["h11", "h12", "h23", "h24"], null);

        var tables = engine.Intl!;
        var numbers = tables.Numbers;
        var dates = tables.Dates;
        var systems = new List<string?> { "latn" };

        foreach (var system in numbers.NumberingSystems.Keys)
        {
            if (system != "latn")
            {
                systems.Add(system);
            }
        }

        systems.Sort(1, systems.Count - 1, StringComparer.Ordinal);
        var nuValues = systems.ToArray();

        var resolved = ResolveLocale(
            engine,
            requested,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ca"] = calendar,
                ["hc"] = hour12 is null ? hourCycle : NullOption,
                ["nu"] = numberingSystem,
            },
            ["ca", "hc", "nu"],
            (locale, key) => key switch
            {
                "ca" => CalendarKeyValues,
                "hc" => [null, "h11", "h12", "h23", "h24"],
                "nu" => nuValues,
                _ => [null],
            });

        var tag = JsLocaleTag.Parse(resolved.DataLocale);
        var language = tag?.Language ?? "en";
        var region = tag?.Region is { Length: > 0 } explicitRegion
            ? explicitRegion
            : tables.Likely(language) is { } likely ? likely[(likely.LastIndexOf('-') + 1)..] : "001";
        var preferred = dates.PreferredHourChar(region);
        var defaultCycle = HourCycleOfChar(preferred);

        var hc = hour12 switch
        {
            true => defaultCycle is "h11" or "h12" ? defaultCycle : "h12",
            false => defaultCycle is "h23" or "h24" ? defaultCycle : "h23",
            null => resolved.Keys["hc"] ?? defaultCycle,
        };

        // TEMPORAL'S ZonedDateTime.prototype.toLocaleString NAMES ITS OWN ZONE (the proposal's
        // amendment of CreateDateTimeFormat), and refuses one the options name as well.
        var timeZoneOption = engine.GetProperty(options, "timeZone");

        if (toLocaleStringTimeZone is not null)
        {
            if (timeZoneOption.Type != JsType.Undefined)
            {
                throw engine.Error("TypeError", "Temporal.ZonedDateTime.prototype.toLocaleString: the timeZone option cannot be given; the value's own zone is used");
            }

            timeZoneOption = JsValue.String(toLocaleStringTimeZone);
        }

        var (timeZone, offset, kind, zone) = ResolveTimeZone(engine, timeZoneOption);

        var components = new Dictionary<string, string>(StringComparer.Ordinal);
        var hasExplicitComponents = false;

        foreach (var (name, values) in DateTimeComponents)
        {
            string? value;

            if (name == "fractionalSecondDigits")
            {
                value = GetNumberOption(engine, options, name, 1, 3, null) is { } digits
                    ? new string('S', digits)
                    : null;
            }
            else
            {
                value = GetStringOption(engine, options, name, values, null);
            }

            if (value is not null)
            {
                components[name] = value;
                hasExplicitComponents = true;
            }
        }

        _ = GetStringOption(engine, options, "formatMatcher", ["basic", "best fit"], "best fit");
        var dateStyle = GetStringOption(engine, options, "dateStyle", ["full", "long", "medium", "short"], null);
        var timeStyle = GetStringOption(engine, options, "timeStyle", ["full", "long", "medium", "short"], null);

        var hcChar = hc switch { "h11" => 'K', "h12" => 'h', "h24" => 'k', _ => 'H' };
        var decimalSymbol = numbers.Value(language, "symbols.decimal") ?? ".";
        var resolvedCalendar = resolved.Keys["ca"] ?? "gregory";

        // THE DEPRECATED HIJRI CALENDARS FALL BACK to the astronomical tabular one (the Intl era and
        // month code proposal's amendment of CreateDateTimeFormat).
        if (resolvedCalendar is "islamic" or "islamic-rgsa")
        {
            resolvedCalendar = "islamic-tbla";
        }

        // A CALENDAR'S DATA is the language's under its own layer: the ISO 8601 calendar's patterns
        // (JSD-0055), or CLDR's names and patterns of another calendar (JSD-0057).
        var dataLanguage = resolvedCalendar == "gregory" ? language : language + "@" + resolvedCalendar;
        (TemporalCalendars Calendars, CalendarId Id)? calendarSystem =
            resolvedCalendar is "gregory" or "iso8601" ? null : (tables.Calendars, JsTemporal.CalendarIdOf(resolvedCalendar));
        var generator = dates.Generator(dataLanguage, resolved.Keys["hc"] is { } keyword ? CharOfHourCycle(keyword) : preferred, decimalSymbol);
        var explicitComponents = new Dictionary<string, string>(components, StringComparer.Ordinal);
        string pattern;

        if (dateStyle is not null || timeStyle is not null)
        {
            if (hasExplicitComponents)
            {
                throw engine.Error("TypeError", "Intl.DateTimeFormat: dateStyle and timeStyle cannot be combined with date-time components");
            }

            if (required == "date" && timeStyle is not null)
            {
                throw engine.Error("TypeError", "Intl.DateTimeFormat: a date format cannot have a timeStyle");
            }

            if (required == "time" && dateStyle is not null)
            {
                throw engine.Error("TypeError", "Intl.DateTimeFormat: a time format cannot have a dateStyle");
            }

            pattern = StylePattern(dates, dataLanguage, dateStyle, timeStyle, hc, hcChar, generator);
        }
        else
        {
            var needDefaults = true;

            if (required is "date" or "any")
            {
                needDefaults &= !(components.ContainsKey("weekday") || components.ContainsKey("year") ||
                    components.ContainsKey("month") || components.ContainsKey("day"));
            }

            if (required is "time" or "any")
            {
                needDefaults &= !(components.ContainsKey("dayPeriod") || components.ContainsKey("hour") ||
                    components.ContainsKey("minute") || components.ContainsKey("second") || components.ContainsKey("fractionalSecondDigits"));
            }

            if (needDefaults && defaults is "date" or "all")
            {
                components["year"] = "numeric";
                components["month"] = "numeric";
                components["day"] = "numeric";
            }

            if (needDefaults && defaults is "time" or "all")
            {
                components["hour"] = "numeric";
                components["minute"] = "numeric";
                components["second"] = "numeric";
            }

            pattern = ReplaceHourCycle(
                generator.BestPattern(SkeletonOfComponents(components, hcChar), JsDatePatternGenerator.MatchHourLength), hcChar);
        }

        var digitsOfSystem = numbers.NumberingSystems.TryGetValue(resolved.Keys["nu"] ?? "latn", out var digitList) ? digitList : numbers.NumberingSystems["latn"];
        var patternCycle = HourCycleOf(pattern);
        var intervalChar = patternCycle is null ? preferred : hcChar;

        // AN IANA ZONE'S NAMES (JSD-0058) are read with the locale's region's golden zones and names.
        var displayNames = tables.LocaleInfo.DisplayNames;
        var naming = kind == JsZoneKind.Named
            ? new JsZoneNaming(tables.ZoneNames, region, code => displayNames.TryGetValue(language + "|region|" + code, out var name) ? name : null)
            : null;

        var formatter = new JsDateTimeFormatter(
            dates,
            dataLanguage,
            digitsOfSystem,
            timeZone,
            offset,
            kind,
            pattern,
            () => new JsDateIntervalFormat(dates, dataLanguage, dates.Generator(dataLanguage, intervalChar, decimalSymbol), JsDatePatternGenerator.SkeletonOf(pattern)),
            zone,
            calendarSystem,
            naming);

        // THE FORMATS OF TEMPORAL'S OBJECTS (the proposal's amendment of CreateDateTimeFormat), built
        // when one is first formatted and only where the realm has Temporal.
        JsTemporalDateFormats? temporal = null;

        if (engine.Realm.TemporalDurationPrototype is not null)
        {
            var stylePattern = dateStyle is not null || timeStyle is not null ? pattern : null;
            var zoned = toLocaleStringTimeZone is not null;

            JsDateTimeFormatter Formatter(string formatPattern, bool plain)
            {
                var cycle = HourCycleOf(formatPattern) is null ? preferred : hcChar;
                return new JsDateTimeFormatter(
                    dates,
                    dataLanguage,
                    digitsOfSystem,
                    plain ? "UTC" : timeZone,
                    plain ? 0 : offset,
                    plain ? JsZoneKind.Utc : kind,
                    formatPattern,
                    () => new JsDateIntervalFormat(dates, dataLanguage, dates.Generator(dataLanguage, cycle, decimalSymbol), JsDatePatternGenerator.SkeletonOf(formatPattern)),
                    plain ? null : zone,
                    calendarSystem,
                    plain ? null : naming);
            }

            string Best(Dictionary<string, string> formatOptions) =>
                ReplaceHourCycle(generator.BestPattern(SkeletonOfComponents(formatOptions, hcChar), JsDatePatternGenerator.MatchHourLength), hcChar);

            temporal = new JsTemporalDateFormats(kindOfValue =>
            {
                string? formatPattern;

                if (stylePattern is not null)
                {
                    formatPattern = kindOfValue switch
                    {
                        "date" => dateStyle is null ? null : AdjustStylePattern(stylePattern, ["weekday", "era", "year", "month", "day"], Best),
                        "year-month" => dateStyle is null ? null : AdjustStylePattern(stylePattern, ["era", "year", "month"], Best),
                        "month-day" => dateStyle is null ? null : AdjustStylePattern(stylePattern, ["month", "day"], Best),
                        "time" => timeStyle is null ? null : AdjustStylePattern(stylePattern, ["dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"], Best),
                        "date-time" => AdjustStylePattern(stylePattern, ["weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"], Best),
                        _ => stylePattern,
                    };
                }
                else
                {
                    formatPattern = kindOfValue switch
                    {
                        "date" => TemporalFormatPattern(explicitComponents, "date", "date", inheritAll: false, Best),
                        "year-month" => TemporalFormatPattern(explicitComponents, "year-month", "year-month", inheritAll: false, Best),
                        "month-day" => TemporalFormatPattern(explicitComponents, "month-day", "month-day", inheritAll: false, Best),
                        "time" => TemporalFormatPattern(explicitComponents, "time", "time", inheritAll: false, Best),
                        "date-time" => TemporalFormatPattern(explicitComponents, "any", "all", inheritAll: false, Best),
                        _ => TemporalFormatPattern(explicitComponents, "any", zoned ? "zoned-date-time" : "all", inheritAll: true, Best),
                    };
                }

                return formatPattern is null ? null : Formatter(formatPattern, plain: kindOfValue != "instant");
            });
        }

        return new JsDateTimeFormatObject(prototype, formatter)
        {
            Locale = resolved.Locale,
            Calendar = resolvedCalendar,
            Temporal = temporal,
            NumberingSystem = resolved.Keys["nu"] ?? "latn",
            HourCycle = patternCycle is null ? null : hc,
            DateStyle = dateStyle,
            TimeStyle = timeStyle,
        };
    }

    /// <summary>
    /// The proposal's GetDateTimeFormat (amended ECMA-402 s15.6.1) over the explicitly requested
    /// components: the pattern a Temporal type is written in, or nothing where the options name
    /// components and none of the type's own.
    /// </summary>
    private static string? TemporalFormatPattern(Dictionary<string, string> options, string required, string defaults, bool inheritAll, Func<Dictionary<string, string>, string> best)
    {
        string[] requiredOptions = required switch
        {
            "date" => ["weekday", "year", "month", "day"],
            "time" => ["dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"],
            "year-month" => ["year", "month"],
            "month-day" => ["month", "day"],
            _ => ["weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"],
        };

        string[] defaultOptions = defaults switch
        {
            "date" => ["year", "month", "day"],
            "time" => ["hour", "minute", "second"],
            "year-month" => ["year", "month"],
            "month-day" => ["month", "day"],
            _ => ["year", "month", "day", "hour", "minute", "second"],
        };

        var formatOptions = inheritAll
            ? new Dictionary<string, string>(options, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        if (!inheritAll && required is "date" or "year-month" or "any" && options.TryGetValue("era", out var era))
        {
            formatOptions["era"] = era;
        }

        var anyPresent = false;

        foreach (var name in new[] { "weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits" })
        {
            anyPresent |= options.ContainsKey(name);
        }

        var needDefaults = true;

        foreach (var name in requiredOptions)
        {
            if (options.TryGetValue(name, out var value))
            {
                formatOptions[name] = value;
                needDefaults = false;
            }
        }

        if (needDefaults)
        {
            if (anyPresent && !inheritAll)
            {
                return null;
            }

            foreach (var name in defaultOptions)
            {
                formatOptions[name] = "numeric";
            }

            if (defaults == "zoned-date-time" && !formatOptions.ContainsKey("timeZoneName"))
            {
                formatOptions["timeZoneName"] = "short";
            }
        }

        return best(formatOptions);
    }

    /// <summary>
    /// The proposal's AdjustDateTimeStyleFormat (amended ECMA-402 s15.6.2): the style's pattern where
    /// it writes only fields the type has, else the best pattern for those of its fields the type has.
    /// </summary>
    private static string AdjustStylePattern(string stylePattern, string[] allowed, Func<Dictionary<string, string>, string> best)
    {
        var components = ComponentsOf(stylePattern);
        var conflicting = false;

        foreach (var name in components.Keys)
        {
            conflicting |= Array.IndexOf(allowed, name) < 0;
        }

        if (!conflicting)
        {
            return stylePattern;
        }

        var formatOptions = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in allowed)
        {
            if (components.TryGetValue(name, out var value))
            {
                formatOptions[name] = value;
            }
        }

        return best(formatOptions);
    }

    /// <summary>The hour cycle a CLDR hour letter writes.</summary>
    private static string HourCycleOfChar(char c) => c switch { 'h' => "h12", 'K' => "h11", 'k' => "h24", _ => "h23" };

    private static char CharOfHourCycle(string hc) => hc switch { "h11" => 'K', "h12" => 'h', "h24" => 'k', _ => 'H' };

    /// <summary>The hour cycle of a pattern's first hour field, or nothing for a pattern without an hour.</summary>
    private static string? HourCycleOf(string pattern)
    {
        var quoted = false;

        foreach (var c in pattern)
        {
            if (c == '\'')
            {
                quoted = !quoted;
            }
            else if (!quoted && c is 'h' or 'H' or 'k' or 'K')
            {
                return HourCycleOfChar(c);
            }
        }

        return null;
    }

    /// <summary>A pattern with every hour field written in <paramref name="hourChar"/>, as V8 writes the resolved cycle into ICU's.</summary>
    private static string ReplaceHourCycle(string pattern, char hourChar)
    {
        var result = new System.Text.StringBuilder(pattern.Length + 1);
        var replace = true;
        var last = '\0';

        foreach (var c in pattern)
        {
            if (c == '\'')
            {
                replace = !replace;
                result.Append(c);
            }
            else if (c is 'h' or 'H' or 'k' or 'K')
            {
                if (replace && last == 'd')
                {
                    result.Append(' ');
                }

                result.Append(replace ? hourChar : c);
            }
            else
            {
                result.Append(c);
            }

            last = c;
        }

        return result.ToString();
    }

    /// <summary>The skeleton of the requested components, the hour in the resolved cycle's letter.</summary>
    private static string SkeletonOfComponents(Dictionary<string, string> components, char hourChar)
    {
        var skeleton = new System.Text.StringBuilder();

        string? Value(string name) => components.TryGetValue(name, out var value) ? value : null;

        skeleton.Append(Value("weekday") switch { "narrow" => "EEEEE", "short" => "EEE", "long" => "EEEE", _ => string.Empty });
        skeleton.Append(Value("era") switch { "narrow" => "GGGGG", "short" => "GGG", "long" => "GGGG", _ => string.Empty });
        skeleton.Append(Value("year") switch { "2-digit" => "yy", "numeric" => "y", _ => string.Empty });
        skeleton.Append(Value("month") switch
        {
            "narrow" => "MMMMM",
            "long" => "MMMM",
            "short" => "MMM",
            "2-digit" => "MM",
            "numeric" => "M",
            _ => string.Empty,
        });
        skeleton.Append(Value("day") switch { "2-digit" => "dd", "numeric" => "d", _ => string.Empty });
        skeleton.Append(Value("dayPeriod") switch { "narrow" => "BBBBB", "long" => "BBBB", "short" => "B", _ => string.Empty });
        skeleton.Append(Value("hour") switch { "2-digit" => new string(hourChar, 2), "numeric" => hourChar.ToString(), _ => string.Empty });
        skeleton.Append(Value("minute") switch { "2-digit" => "mm", "numeric" => "m", _ => string.Empty });
        skeleton.Append(Value("second") switch { "2-digit" => "ss", "numeric" => "s", _ => string.Empty });
        skeleton.Append(Value("fractionalSecondDigits") ?? string.Empty);
        skeleton.Append(Value("timeZoneName") switch
        {
            "short" => "z",
            "long" => "zzzz",
            "shortOffset" => "O",
            "longOffset" => "OOOO",
            "shortGeneric" => "v",
            "longGeneric" => "vvvv",
            _ => string.Empty,
        });

        return skeleton.ToString();
    }

    /// <summary>
    /// The pattern of a date style, a time style or both, joined by CLDR's date-time pattern for the
    /// date style; a time whose hour cycle is not the resolved one is regenerated from its skeleton.
    /// </summary>
    private static string StylePattern(JsDateData dates, string language, string? dateStyle, string? timeStyle, string hc, char hcChar, JsDatePatternGenerator generator)
    {
        var date = dateStyle is null ? null : dates.Value(language, "dateFormats." + dateStyle);
        var time = timeStyle is null ? null : dates.Value(language, "timeFormats." + timeStyle);
        var pattern = date is not null && time is not null
            ? JsDatePatternGenerator.Substitute(dates.Value(language, "atTime." + dateStyle) ?? "{1} {0}", time, date, string.Empty)
            : date ?? time ?? string.Empty;

        if (time is null || HourCycleOf(pattern) == hc)
        {
            return pattern;
        }

        var skeleton = new System.Text.StringBuilder();

        foreach (var c in JsDatePatternGenerator.SkeletonOf(pattern))
        {
            if (c is 'h' or 'H' or 'k' or 'K')
            {
                skeleton.Append(hcChar);
            }
            else if (c is not ('a' or 'b' or 'B'))
            {
                skeleton.Append(c);
            }
        }

        return ReplaceHourCycle(generator.BestPattern(skeleton.ToString(), JsDatePatternGenerator.MatchHourLength), hcChar);
    }

    /// <summary>The components a pattern writes, as resolvedOptions reports them.</summary>
    private static Dictionary<string, string> ComponentsOf(string pattern)
    {
        var components = new Dictionary<string, string>(StringComparer.Ordinal);
        var tokens = JsDatePatternGenerator.Tokens(pattern);

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (token[0] == '\'')
            {
                index = JsDatePatternGenerator.QuoteEnd(tokens, index, null);
                continue;
            }

            var count = token.Length;
            string Text() => count <= 3 ? "short" : count == 4 ? "long" : "narrow";
            string Digits() => count == 2 ? "2-digit" : "numeric";

            switch (token[0])
            {
                case 'G':
                    components["era"] = Text();
                    break;
                case 'y':
                    components["year"] = Digits();
                    break;
                case 'M' or 'L':
                    components["month"] = count <= 2 ? Digits() : Text();
                    break;
                case 'E' or 'c':
                    components["weekday"] = count == 6 ? "short" : Text();
                    break;
                case 'd':
                    components["day"] = Digits();
                    break;
                case 'B' or 'b':
                    components["dayPeriod"] = Text();
                    break;
                case 'h' or 'H' or 'k' or 'K':
                    components["hour"] = Digits();
                    break;
                case 'm':
                    components["minute"] = Digits();
                    break;
                case 's':
                    components["second"] = Digits();
                    break;
                case 'S':
                    components["fractionalSecondDigits"] = token;
                    break;
                case 'z':
                    components["timeZoneName"] = count < 4 ? "short" : "long";
                    break;
                case 'O':
                    components["timeZoneName"] = count < 4 ? "shortOffset" : "longOffset";
                    break;
                case 'v':
                    components["timeZoneName"] = count < 4 ? "shortGeneric" : "longGeneric";
                    break;
            }
        }

        return components;
    }

    /// <summary>
    /// The time zone an option names: UTC by default, an offset string by its <c>+HH:MM</c> or <c>-HH:MM</c> form,
    /// and otherwise an IANA Zone or Link name, matched without regard to ASCII case and resolved to its
    /// primary identifier (ECMA-402 11.1.2 steps 29 to 36, JSD-0053): a name resolving to UTC as
    /// <c>UTC</c>, an <c>Etc/GMT</c> zone with an hour as its fixed offset, and any other as the zone.
    /// </summary>
    /// <remarks>
    /// The offset strings, the <c>Etc/GMT</c> zones and the lookup are the shared library's since SJL-8.5
    /// (JSD-0076, JSD-0077): <c>OffsetTimeZones</c> reads and writes the offsets and names the
    /// <c>Etc/GMT</c> zones' fixed offsets, and the Intl data's <c>TimeZoneDatabase</c> finds a name, its
    /// primary identifier and its rules. This keeps the option's conversion, the two <c>RangeError</c>s and
    /// the formatter's choice to report the primary identifier.
    /// </remarks>
    private static (string Identifier, int Offset, JsZoneKind Kind, ITimeZoneRules? Zone) ResolveTimeZone(JsEngine engine, JsValue value)
    {
        if (value.Type == JsType.Undefined)
        {
            return ("UTC", 0, JsZoneKind.Utc, null);
        }

        var text = engine.ToStringValue(value);

        if (OffsetTimeZones.IsTimeZoneOffsetString(text))
        {
            if (!OffsetTimeZones.TryParseOffsetIdentifier(text, out var minutes))
            {
                throw engine.Error("RangeError", "Intl.DateTimeFormat: a time zone offset cannot have seconds: " + text);
            }

            return (OffsetTimeZones.FormatOffsetIdentifier(minutes), minutes, minutes == 0 ? JsZoneKind.Gmt : JsZoneKind.Offset, null);
        }

        var zones = engine.Intl!.TimeZones;

        if (!zones.TryFind(text, out var id))
        {
            throw engine.Error("RangeError", "Intl.DateTimeFormat: " + text + " is not an available time zone");
        }

        var primary = zones.Primary(id);

        if (primary.Identifier == "UTC")
        {
            return ("UTC", 0, JsZoneKind.Utc, null);
        }

        // IANA'S ETC/GMT ZONES WITH AN HOUR ARE FIXED OFFSETS by definition, with POSIX's inverted sign.
        if (OffsetTimeZones.TryGetEtcGmtOffset(primary.Identifier, out var etcGmt))
        {
            return (primary.Identifier, etcGmt, JsZoneKind.Offset, null);
        }

        return (primary.Identifier, 0, JsZoneKind.Named, zones.Rules(primary));
    }
}

/// <summary>An <c>Intl.DateTimeFormat</c>: its resolved options and its formatter.</summary>
internal sealed class JsDateTimeFormatObject : JsObject
{
    internal JsDateTimeFormatObject(JsObject prototype, JsDateTimeFormatter formatter) : base(prototype)
    {
        Formatter = formatter;
    }

    internal JsDateTimeFormatter Formatter { get; }

    internal string Locale { get; init; } = string.Empty;

    internal string Calendar { get; init; } = "gregory";

    internal string NumberingSystem { get; init; } = "latn";

    internal string? HourCycle { get; init; }

    internal string? DateStyle { get; init; }

    internal string? TimeStyle { get; init; }

    internal JsNativeFunction? BoundFormat { get; set; }

    /// <summary>The formats of Temporal's objects, or nothing where the realm has no Temporal.</summary>
    internal JsTemporalDateFormats? Temporal { get; init; }
}

/// <summary>
/// The [[TemporalPlainDateFormat]] to [[TemporalInstantFormat]] slots of a date-time format: each
/// built when first asked for, and nothing where the options exclude the type (JSD-0055).
/// </summary>
internal sealed class JsTemporalDateFormats
{
    private readonly Func<string, JsDateTimeFormatter?> build;
    private readonly Dictionary<string, JsDateTimeFormatter?> built = new(StringComparer.Ordinal);
    private readonly object gate = new();

    internal JsTemporalDateFormats(Func<string, JsDateTimeFormatter?> build) => this.build = build;

    /// <summary>The format of <c>date</c>, <c>year-month</c>, <c>month-day</c>, <c>time</c>, <c>date-time</c> or <c>instant</c>.</summary>
    internal JsDateTimeFormatter? For(string kind)
    {
        lock (gate)
        {
            if (!built.TryGetValue(kind, out var formatter))
            {
                formatter = build(kind);
                built[kind] = formatter;
            }

            return formatter;
        }
    }
}
