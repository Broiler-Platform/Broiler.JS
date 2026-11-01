// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsIntlTables.cs
//     blob 46f1856060283e74727ccfc0b87f9872cfccb0e6: JsIntlTables, its readers and its decoded tables
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the fields, constructor lines and members of the tables this stage does not compare, each the time zone, calendar, zone-name, collation or segmentation data another milestone's library owns: `collation`, `timeZones`, `calendars`, `zoneNames`, `UnstatedRelease`, `TimeZones`, `Calendars`, `ZoneNames`, `Collation` and `Segmentation`, and the `using` lines only they needed.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The internationalization tables a composition handed over, decoded once and shared by every
/// runtime its descriptor makes (JSD-0043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Each table is decoded the first time a guest needs it</b>, under a thread-safe lazy: a
/// descriptor's runtimes may run on several threads, and the tables never change, so the first
/// decode is the only one and every later read is a dictionary lookup.
/// </para>
/// <para>
/// <b>Nothing here is guest-visible state.</b> The decoded tables are the composition's data in a
/// shape the profile can search; no realm, engine or guest value is held, so sharing them across
/// runtimes shares nothing a guest could write.
/// </para>
/// </remarks>
internal sealed class JsIntlTables
{
    private readonly IJsIntlData data;

    private readonly Lazy<Dictionary<string, string>> likely;

    private readonly Lazy<Dictionary<string, Dictionary<string, string>>> aliases;

    private readonly Lazy<Dictionary<string, string>> extensions;

    private readonly Lazy<string[]> locales;

    private readonly Lazy<JsNumberData> numbers;

    private readonly Lazy<JsDateData> dates;

    private readonly Lazy<JsLocaleInfo> localeInfo;

    internal JsIntlTables(IJsIntlData data)
    {
        this.data = data;
        likely = new(() => Pairs(JsIntlTable.LikelySubtags));
        aliases = new(ReadAliases);
        extensions = new(ReadExtensions);
        locales = new(() => [.. Lines(JsIntlTable.Locales)]);
        numbers = new(ReadNumbers);
        dates = new(ReadDates);
        localeInfo = new(ReadLocaleInfo);
    }

    /// <summary>The CLDR release the tables come from.</summary>
    internal string CldrVersion => data.CldrVersion;

    /// <summary>The locales the data supports, in canonical form.</summary>
    internal IReadOnlyList<string> Locales => locales.Value;

    /// <summary>The likely subtags of a source in CLDR's form - <c>en</c>, <c>und-Latn</c>, <c>und-US</c> - or nothing.</summary>
    internal string? Likely(string source) => likely.Value.TryGetValue(source, out var target) ? target : null;

    /// <summary>The alias of <paramref name="from"/> of one kind - language, script, region, variant, subdivision - or nothing.</summary>
    internal string? Alias(string kind, string from) =>
        aliases.Value.TryGetValue(kind, out var table) && table.TryGetValue(from, out var replacement) ? replacement : null;

    /// <summary>Every language alias, for the rule matcher that reads them all.</summary>
    internal IReadOnlyDictionary<string, string> LanguageAliases => aliases.Value["language"];

    /// <summary>Whether <paramref name="key"/> is a key of extension <paramref name="singleton"/> the BCP 47 data defines.</summary>
    internal bool IsKey(char singleton, string key) => extensions.Value.ContainsKey(singleton + "|" + key + "|");

    /// <summary>
    /// Whether <paramref name="type"/> is a type of <paramref name="key"/>, and its preferred form:
    /// itself, or the type a deprecated one was replaced by.
    /// </summary>
    internal bool TryType(char singleton, string key, string type, out string preferred)
    {
        if (extensions.Value.TryGetValue(singleton + "|" + key + "|" + type, out var stated))
        {
            preferred = stated.Length == 0 ? type : stated;
            return true;
        }

        preferred = type;
        return false;
    }

    /// <summary>The number, currency, unit and plural data, decoded the first time a number is formatted.</summary>
    internal JsNumberData Numbers => numbers.Value;

    /// <summary>The calendar, hour cycle and day period data, decoded the first time a date is formatted.</summary>
    internal JsDateData Dates => dates.Value;

    /// <summary>The week data and the scripts' line directions, decoded the first time a locale is asked for them.</summary>
    internal JsLocaleInfo LocaleInfo => localeInfo.Value;

    /// <summary>A table value with its <c>\uXXXX</c> escapes resolved.</summary>
    internal static string Unescape(string value)
    {
        if (value.IndexOf('\\') < 0)
        {
            return value;
        }

        var text = new StringBuilder(value.Length);

        for (var at = 0; at < value.Length; at++)
        {
            if (value[at] == '\\' && at + 5 < value.Length && value[at + 1] == 'u')
            {
                text.Append((char)int.Parse(value.AsSpan(at + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                at += 5;
                continue;
            }

            text.Append(value[at]);
        }

        return text.ToString();
    }

    /// <summary>Decodes the number tables.</summary>
    private JsNumberData ReadNumbers()
    {
        var data = new JsNumberData();

        foreach (var line in Lines(JsIntlTable.NumberLocales))
        {
            var fields = line.Split('|');

            if (!data.Locales.TryGetValue(fields[0], out var locale))
            {
                locale = new(StringComparer.Ordinal);
                data.Locales[fields[0]] = locale;
            }

            locale[fields[1]] = Unescape(fields[2]);
        }

        foreach (var line in Lines(JsIntlTable.Currencies))
        {
            var fields = line.Split('|');
            data.Currencies[fields[0] + "|" + fields[1]] = new JsCurrencyNames(
                Unescape(fields[2]), Unescape(fields[3]), Unescape(fields[4]), Unescape(fields[5]), Unescape(fields[6]), fields[7], fields[8]);
        }

        foreach (var line in Lines(JsIntlTable.CurrencyDigits))
        {
            var bar = line.IndexOf('|');
            data.CurrencyDigits[line[..bar]] = int.Parse(line[(bar + 1)..], CultureInfo.InvariantCulture);
        }

        foreach (var line in Lines(JsIntlTable.NumberingSystems))
        {
            var bar = line.IndexOf('|');
            var digits = Unescape(line[(bar + 1)..]);
            var list = new string[10];
            var index = 0;

            for (var at = 0; at < digits.Length && index < 10; index++)
            {
                var width = char.IsHighSurrogate(digits[at]) && at + 1 < digits.Length ? 2 : 1;
                list[index] = digits.Substring(at, width);
                at += width;
            }

            data.NumberingSystems[line[..bar]] = list;
        }

        foreach (var (table, plurals) in new[] { (JsIntlTable.Plurals, data.Plurals), (JsIntlTable.Ordinals, data.Ordinals) })
        {
            foreach (var line in Lines(table))
            {
                var fields = line.Split('|');

                if (!plurals.TryGetValue(fields[0], out var rules))
                {
                    rules = [];
                    plurals[fields[0]] = rules;
                }

                rules.Add((fields[1], JsPluralRule.Parse(Unescape(fields[2]))));
            }
        }

        foreach (var line in Lines(JsIntlTable.PluralRanges))
        {
            var fields = line.Split('|');
            data.PluralRanges[fields[0] + "|" + fields[1] + "|" + fields[2]] = fields[3];
        }

        foreach (var line in Lines(JsIntlTable.Units))
        {
            var fields = line.Split('|');
            data.Units[fields[0] + "|" + fields[1] + "|" + fields[2] + "|" + fields[3]] = Unescape(fields[4]);
        }

        return data;
    }

    /// <summary>Decodes the date tables.</summary>
    /// <summary>A layer of date data over the data below it: its values set, and its <c>-</c> keys removed.</summary>
    private static Dictionary<string, string> Layered(Dictionary<string, string> below, Dictionary<string, string> layer)
    {
        var result = new Dictionary<string, string>(below, StringComparer.Ordinal);

        foreach (var (key, value) in layer)
        {
            if (key.StartsWith('-'))
            {
                result.Remove(key[1..]);
            }
            else
            {
                result[key] = value;
            }
        }

        return result;
    }

    private JsDateData ReadDates()
    {
        var data = new JsDateData();

        foreach (var line in Lines(JsIntlTable.DateLocales))
        {
            var fields = line.Split('|');

            if (!data.Locales.TryGetValue(fields[0], out var locale))
            {
                locale = new(StringComparer.Ordinal);
                data.Locales[fields[0]] = locale;
            }

            locale[fields[1]] = Unescape(fields[2]);
        }

        // EACH LANGUAGE'S ISO 8601 CALENDAR (JSD-0055) is its Gregorian names under root's ISO 8601
        // patterns, as CLDR's aliases make it: `<language>@iso8601`. The date-time glue stays the
        // language's Gregorian one, as ICU resolves it, where root's would be "{1} {0}".
        if (data.Locales.TryGetValue(JsDateData.Iso8601, out var iso8601))
        {
            foreach (var language in new List<string>(data.Locales.Keys))
            {
                if (language.Contains('@', StringComparison.Ordinal))
                {
                    continue;
                }

                var calendar = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var (key, value) in data.Locales[language])
                {
                    if (!JsDateData.IsPatternKey(key) || key.StartsWith("dateTime.", StringComparison.Ordinal) || key.StartsWith("atTime.", StringComparison.Ordinal))
                    {
                        calendar[key] = value;
                    }
                }

                foreach (var (key, value) in iso8601)
                {
                    if (!key.StartsWith("dateTime.", StringComparison.Ordinal) && !key.StartsWith("atTime.", StringComparison.Ordinal))
                    {
                        calendar[key] = value;
                    }
                }

                data.Locales[language + JsDateData.Iso8601] = calendar;
            }
        }

        // EACH LANGUAGE'S OTHER CALENDARS (JSD-0057) are layers: CLDR's generic patterns over the
        // language's Gregorian data, and each calendar over the generic layer or its parent's. A layer's
        // key beginning with `-` removes the key below it.
        foreach (var language in new List<string>(data.Locales.Keys))
        {
            if (language.Contains('@', StringComparison.Ordinal) || !data.Locales.TryGetValue(language + "@generic", out var genericDiff))
            {
                continue;
            }

            var generic = Layered(data.Locales[language], genericDiff);
            data.Locales.Remove(language + "@generic");

            // A LAYER THE TABLE HOLDS NO LINE OF is its parent's data unchanged.
            foreach (var (calendar, parent) in JsDateData.CalendarLayers)
            {
                var diff = data.Locales.TryGetValue(language + "@" + calendar, out var lines) ? lines : [];
                data.Locales[language + "@" + calendar] = Layered(parent is null ? generic : data.Locales[language + "@" + parent], diff);
            }
        }

        foreach (var line in Lines(JsIntlTable.TimeData))
        {
            var fields = line.Split('|');
            data.TimeData[fields[0]] = (fields[1], fields[2]);
        }

        static int Minutes(string time) => time.Length == 0 ? -1 : (int.Parse(time[..2], CultureInfo.InvariantCulture) * 60) + int.Parse(time[3..], CultureInfo.InvariantCulture);

        foreach (var line in Lines(JsIntlTable.DayPeriods))
        {
            var fields = line.Split('|');

            if (!data.DayPeriods.TryGetValue(fields[0], out var rules))
            {
                rules = [];
                data.DayPeriods[fields[0]] = rules;
            }

            rules.Add(fields[3].Length == 0
                ? new JsDayPeriodRule(fields[1], Minutes(fields[2]), -1, At: true)
                : new JsDayPeriodRule(fields[1], Minutes(fields[2]), Minutes(fields[3]), At: false));
        }

        return data;
    }

    /// <summary>Decodes the week and script tables.</summary>
    private JsLocaleInfo ReadLocaleInfo()
    {
        var info = new JsLocaleInfo();

        foreach (var line in Lines(JsIntlTable.WeekData))
        {
            var fields = line.Split('|');
            info.Weeks[fields[0]] = (fields[1], fields[2], fields[3], fields[4]);
        }

        foreach (var line in Lines(JsIntlTable.Scripts))
        {
            var bar = line.IndexOf('|');
            info.RightToLeft[line[..bar]] = line[(bar + 1)..] == "YES";
        }

        foreach (var line in Lines(JsIntlTable.ListPatterns))
        {
            var fields = line.Split('|');
            info.ListPatterns[fields[0] + "|" + fields[1]] =
                (Unescape(fields[2]), Unescape(fields[3]), Unescape(fields[4]), Unescape(fields[5]));
        }

        foreach (var line in Lines(JsIntlTable.RelativeTimes))
        {
            var fields = line.Split('|');
            info.RelativeTimes[fields[0] + "|" + fields[1] + "|" + fields[2]] = Unescape(fields[3]);
        }

        foreach (var line in Lines(JsIntlTable.DisplayNames))
        {
            var fields = line.Split('|');
            info.DisplayNames[fields[0] + "|" + fields[1] + "|" + fields[2]] = Unescape(fields[3]);
        }

        return info;
    }

    /// <summary>The lines of a text table.</summary>
    private List<string> Lines(JsIntlTable table)
    {
        var text = Encoding.ASCII.GetString(data.Table(table));
        var lines = new List<string>();

        foreach (var line in text.Split('\n'))
        {
            if (line.Length != 0)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>A two-field table as a dictionary.</summary>
    private Dictionary<string, string> Pairs(JsIntlTable table)
    {
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in Lines(table))
        {
            var bar = line.IndexOf('|');
            pairs[line[..bar]] = line[(bar + 1)..];
        }

        return pairs;
    }

    private Dictionary<string, Dictionary<string, string>> ReadAliases()
    {
        var kinds = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        foreach (var kind in new[] { "language", "script", "region", "variant", "subdivision" })
        {
            kinds[kind] = new(StringComparer.Ordinal);
        }

        foreach (var line in Lines(JsIntlTable.Aliases))
        {
            var fields = line.Split('|');
            kinds[fields[0]][fields[1]] = fields[2];
        }

        return kinds;
    }

    private Dictionary<string, string> ReadExtensions()
    {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in Lines(JsIntlTable.Extensions))
        {
            var fields = line.Split('|');
            table[fields[0] + "|" + fields[1] + "|" + fields[2]] = fields[3];
        }

        return table;
    }
}
