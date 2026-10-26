// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           0
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  6/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>The date tables: the Gregorian, generic, ISO 8601 and other calendars' data, the zone names and the metazones.</summary>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 date tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=6; Fingerprint=AD2E42
// Broiler-Human:        PENDING
public static partial class CldrTableGenerator
{
    /// <summary>
    /// The calendars Broiler.VM's generator writes a layer for: identifier, package, file and the calendar it
    /// is a layer over (none for the generic layer), in its order.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 date tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=F1B522
    // Broiler-Human:        PENDING
    private static ImmutableArray<(string Id, string Package, string File, string? Parent)> CalendarFiles { get; } = ImmutableArray.Create<(string Id, string Package, string File, string? Parent)>(
        ("buddhist", "cldr-cal-buddhist-full", "ca-buddhist", null),
        ("chinese", "cldr-cal-chinese-full", "ca-chinese", null),
        ("coptic", "cldr-cal-coptic-full", "ca-coptic", null),
        ("dangi", "cldr-cal-dangi-full", "ca-dangi", "chinese"),
        ("ethiopic", "cldr-cal-ethiopic-full", "ca-ethiopic", null),
        ("ethioaa", "cldr-cal-ethiopic-full", "ca-ethiopic-amete-alem", "ethiopic"),
        ("hebrew", "cldr-cal-hebrew-full", "ca-hebrew", null),
        ("indian", "cldr-cal-indian-full", "ca-indian", null),
        ("islamic", "cldr-cal-islamic-full", "ca-islamic", null),
        ("islamic-civil", "cldr-cal-islamic-full", "ca-islamic-civil", "islamic"),
        ("islamic-tbla", "cldr-cal-islamic-full", "ca-islamic-tbla", "islamic"),
        ("islamic-umalqura", "cldr-cal-islamic-full", "ca-islamic-umalqura", "islamic"),
        ("japanese", "cldr-cal-japanese-full", "ca-japanese", null),
        ("persian", "cldr-cal-persian-full", "ca-persian", null),
        ("roc", "cldr-cal-roc-full", "ca-roc", null));

    /// <summary>
    /// Each main-data locale's Gregorian calendar data and the zone names a UTC or fixed-offset zone uses,
    /// flattened to dotted keys: the month, day, day period and era names; the date, time and date-time
    /// patterns; the available formats, append items and interval formats; the date fields' display names; and
    /// the GMT formats and the names of UTC and of the GMT zone, where the locale's data names it. Alternate
    /// forms (<c>-alt-</c>) and plural-dependent formats (<c>-count-</c>) are not carried. Then the ISO 8601
    /// patterns and the other calendars' layers.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 date tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=5; Fingerprint=1715D5
    // Broiler-Human:        PENDING
    private static IEnumerable<string> DateLocales(Archive archive, ImmutableArray<string> languages, ImmutableArray<string> calendars)
    {
        var lines = new List<string>();

        foreach (var language in languages)
        {
            var gregorian = archive.Json("json/cldr-dates-full/main/" + language + "/ca-gregorian.json")
                .GetProperty("main").GetProperty(language).GetProperty("dates").GetProperty("calendars").GetProperty("gregorian");

            foreach (var name in new[] { "months", "days", "dayPeriods", "eras", "dateFormats", "timeFormats" })
            {
                FlattenDates(language, name, gregorian.GetProperty(name), lines);
            }

            var dateTime = gregorian.GetProperty("dateTimeFormats");

            foreach (var style in new[] { "full", "long", "medium", "short" })
            {
                lines.Add(language + "|dateTime." + style + "|" + Escape(dateTime.GetProperty(style).GetString()!));
                lines.Add(language + "|atTime." + style + "|" + Escape(gregorian.GetProperty("dateTimeFormats-atTime").GetProperty("standard").GetProperty(style).GetString()!));
            }

            FlattenDates(language, "available", dateTime.GetProperty("availableFormats"), lines);
            FlattenDates(language, "append", dateTime.GetProperty("appendItems"), lines);

            foreach (var entry in dateTime.GetProperty("intervalFormats").EnumerateObject())
            {
                if (entry.Name == "intervalFormatFallback")
                {
                    lines.Add(language + "|interval.fallback|" + Escape(entry.Value.GetString()!));
                }
                else if (!entry.Name.Contains("-alt-", StringComparison.Ordinal))
                {
                    FlattenDates(language, "interval." + entry.Name, entry.Value, lines);
                }
            }

            var fields = archive.Json("json/cldr-dates-full/main/" + language + "/dateFields.json")
                .GetProperty("main").GetProperty(language).GetProperty("dates").GetProperty("fields");

            foreach (var field in new[] { "era", "year", "quarter", "month", "week", "weekOfMonth", "weekday", "dayOfYear", "weekdayOfMonth", "day", "dayperiod", "hour", "minute", "second", "zone" })
            {
                lines.Add(language + "|field." + field + "|" + Escape(fields.GetProperty(field).GetProperty("displayName").GetString()!));
            }

            var zones = archive.Json("json/cldr-dates-full/main/" + language + "/timeZoneNames.json")
                .GetProperty("main").GetProperty(language).GetProperty("dates").GetProperty("timeZoneNames");

            foreach (var name in new[] { "hourFormat", "gmtFormat", "gmtZeroFormat" })
            {
                lines.Add(language + "|zone." + name + "|" + Escape(zones.GetProperty(name).GetString()!));
            }

            lines.AddRange(ZoneNames(archive, language, zones));

            // A LOCALE WHOSE DATA NAMES NO METAZONE - a seed locale of CLDR's - has no GMT zone names;
            // Broiler.VM's languages all have them.
            foreach (var (key, names) in new[]
            {
                ("utc", zones.GetProperty("zone").GetProperty("Etc").GetProperty("UTC")),
                ("gmt", zones.TryGetProperty("metazone", out var metazones) && metazones.TryGetProperty("GMT", out var gmt) ? gmt : default),
            })
            {
                if (names.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var width in new[] { "long", "short" })
                {
                    if (names.TryGetProperty(width, out var named))
                    {
                        lines.Add(language + "|zone." + key + "." + width + "|" + Escape(named.GetProperty("standard").GetString()!));
                    }
                }
            }
        }

        lines.AddRange(Iso8601Patterns(archive));
        lines.AddRange(CalendarLocales(archive, languages, calendars));
        return lines.Order(StringComparer.Ordinal);
    }

    /// <summary>
    /// The primary identifier of each name CLDR's time zone keys list (JSD-0053's): the key's <c>_iana</c>
    /// name, or its first alias where it states none.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 zone primaries via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=42CBA4
    // Broiler-Human:        PENDING
    private static Dictionary<string, string> ZonePrimaries(Archive archive)
    {
        var primaries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var key in archive.Json("json/cldr-bcp47/bcp47/timezone.json").GetProperty("keyword").GetProperty("u").GetProperty("tz").EnumerateObject())
        {
            if (key.Value.ValueKind != JsonValueKind.Object || !key.Value.TryGetProperty("_alias", out var alias))
            {
                continue;
            }

            var names = alias.GetString()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var primary = key.Value.TryGetProperty("_iana", out var iana) ? iana.GetString()! : names[0];

            foreach (var name in names)
            {
                primaries[name] = primary;
            }
        }

        return primaries;
    }

    /// <summary>
    /// ICU's default exemplar city of a zone identifier: its last segment, underscores as spaces, or nothing for
    /// an <c>Etc/</c> zone or an identifier without a slash.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 zone names via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=45A587
    // Broiler-Human:        PENDING
    private static string? DefaultCity(string zone) =>
        zone.StartsWith("Etc/", StringComparison.Ordinal) || !zone.Contains('/', StringComparison.Ordinal)
            ? null
            : zone[(zone.LastIndexOf('/') + 1)..].Replace('_', ' ');

    /// <summary>
    /// A main-data locale's time zone names (JSD-0058), by primary identifier: the region and fallback formats;
    /// each metazone's names (<c>mz.Name.lg</c>: long or short, generic, standard or daylight); each zone's own
    /// names (<c>tz.Zone.ld</c>); and each zone's exemplar city (<c>tz.Zone.city</c>) where it is not the
    /// default one of its primary identifier. A locale whose data states no metazone has no metazone names.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 zone names via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=0FBBE5
    // Broiler-Human:        PENDING
    private static List<string> ZoneNames(Archive archive, string language, JsonElement zones)
    {
        var primaries = ZonePrimaries(archive);
        var lines = new List<string>
        {
            language + "|zone.regionFormat|" + Escape(zones.GetProperty("regionFormat").GetString()!),
            language + "|zone.fallbackFormat|" + Escape(zones.GetProperty("fallbackFormat").GetString()!),
        };

        void Names(string prefix, JsonElement names)
        {
            foreach (var width in new[] { "long", "short" })
            {
                if (!names.TryGetProperty(width, out var named))
                {
                    continue;
                }

                foreach (var type in new[] { "generic", "standard", "daylight" })
                {
                    if (named.TryGetProperty(type, out var name))
                    {
                        lines.Add(language + "|" + prefix + "." + width[0] + type[0] + "|" + Escape(name.GetString()!));
                    }
                }
            }
        }

        // A LOCALE WHOSE DATA NAMES NO METAZONE - a seed locale of CLDR's - has no metazone names.
        if (zones.TryGetProperty("metazone", out var metazones))
        {
            foreach (var metazone in metazones.EnumerateObject())
            {
                Names("mz." + metazone.Name, metazone.Value);
            }
        }

        var cities = new Dictionary<string, string>(StringComparer.Ordinal);

        void Walk(string path, JsonElement node)
        {
            if (node.TryGetProperty("exemplarCity", out _) || node.TryGetProperty("long", out _) || node.TryGetProperty("short", out _))
            {
                if (path.StartsWith("Etc/", StringComparison.Ordinal))
                {
                    return;
                }

                var primary = primaries.TryGetValue(path, out var known) ? known : path;
                Names("tz." + primary, node);

                if (node.TryGetProperty("exemplarCity", out var city))
                {
                    cities[primary] = city.GetString()!;
                }

                return;
            }

            foreach (var child in node.EnumerateObject())
            {
                if (child.Value.ValueKind == JsonValueKind.Object)
                {
                    Walk(path.Length == 0 ? child.Name : path + "/" + child.Name, child.Value);
                }
            }
        }

        Walk(string.Empty, zones.GetProperty("zone"));

        // A ZONE'S EXEMPLAR CITY is CLDR's, or the default one of its canonical name, which may not be
        // its primary identifier's (Asia/Saigon's, not Asia/Ho_Chi_Minh's): only one that the reader
        // cannot derive from the primary identifier is written.
        foreach (var canonical in primaries.GroupBy(static pair => pair.Value, static pair => pair.Key))
        {
            var primary = canonical.Key;
            var city = cities.TryGetValue(primary, out var named) ? named : DefaultCity(canonical.First());

            if (city is not null && city != DefaultCity(primary))
            {
                lines.Add(language + "|tz." + primary + ".city|" + Escape(city));
            }
        }

        return lines;
    }

    /// <summary>
    /// The metazone data of CLDR's <c>metaZones.json</c> and <c>primaryZones.json</c>, by primary identifier:
    /// each zone's periods in a metazone (<c>z|zone|from|to|metazone</c>, a missing bound empty), each metazone's
    /// golden zone in a region (<c>g|metazone|region|zone</c>), and the primary zone of a country with several
    /// (<c>p|region|zone</c>). <c>Etc/</c> zones are left out: UTC's and a fixed offset's names are the date
    /// data's own.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 metazones via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=B8D1BF
    // Broiler-Human:        PENDING
    private static IEnumerable<string> MetaZones(Archive archive)
    {
        var primaries = ZonePrimaries(archive);
        var metaZones = archive.Json("json/cldr-core/supplemental/metaZones.json").GetProperty("supplemental").GetProperty("metaZones");
        var lines = new List<string>();

        string Primary(string zone) => primaries.TryGetValue(zone, out var primary)
            ? primary
            : throw new InvalidDataException("metaZones.json names " + zone + ", which no time zone key lists");

        void Walk(string path, JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Array)
            {
                if (path.StartsWith("Etc/", StringComparison.Ordinal))
                {
                    return;
                }

                foreach (var period in node.EnumerateArray())
                {
                    var uses = period.GetProperty("usesMetazone");
                    var from = uses.TryGetProperty("_from", out var start) ? start.GetString()! : string.Empty;
                    var to = uses.TryGetProperty("_to", out var end) ? end.GetString()! : string.Empty;
                    lines.Add("z|" + Primary(path) + "|" + from + "|" + to + "|" + uses.GetProperty("_mzone").GetString());
                }

                return;
            }

            foreach (var child in node.EnumerateObject())
            {
                Walk(path.Length == 0 ? child.Name : path + "/" + child.Name, child.Value);
            }
        }

        Walk(string.Empty, metaZones.GetProperty("metazoneInfo").GetProperty("timezone"));

        foreach (var entry in metaZones.GetProperty("metazones").EnumerateArray())
        {
            var map = entry.GetProperty("mapZone");
            lines.Add("g|" + map.GetProperty("_other").GetString() + "|" + map.GetProperty("_territory").GetString() + "|" + Primary(map.GetProperty("_type").GetString()!));
        }

        foreach (var region in archive.Json("json/cldr-core/supplemental/primaryZones.json").GetProperty("supplemental").GetProperty("primaryZones").EnumerateObject())
        {
            lines.Add("p|" + region.Name + "|" + Primary(region.Value.GetString()!));
        }

        return lines.Order(StringComparer.Ordinal);
    }

    /// <summary>
    /// The calendars a formatter writes besides the Gregorian and the ISO 8601 ones, each as a layer over the
    /// one it inherits from, by main-data locale (JSD-0057): <c>language@generic</c>, CLDR's generic calendar's
    /// patterns over the language's Gregorian ones; and <c>language@calendar</c>, a calendar's names and
    /// patterns over the Gregorian names and the generic patterns, or over its parent's layer (the Umm
    /// al-Qura, civil and astronomical Hijri calendars over <c>islamic</c>, Korean over Chinese, Ethiopian
    /// Amete Alem over Ethiopian).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A layer holds only what differs</b>: a value the calendar states otherwise than the layer below it,
    /// and a pattern key the layer below holds and the calendar does not, as a line whose key begins with
    /// <c>-</c> and whose value is empty. A name the calendar does not state is the layer below's.
    /// </para>
    /// <para>
    /// <b>Besides the Gregorian keys</b>, a layer carries the leap month patterns (<c>monthPatterns</c>) and the
    /// years' cyclic names (<c>cyclic.years</c>) of the Chinese calendar. The Japanese calendar carries its eras
    /// from Meiji on, which Temporal's eras are (JSD-0056); before Meiji its dates are written in the Gregorian
    /// eras below it.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 calendar layers via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=5; Fingerprint=00FA74
    // Broiler-Human:        PENDING
    private static List<string> CalendarLocales(Archive archive, ImmutableArray<string> languages, ImmutableArray<string> names)
    {
        var lines = new List<string>();
        var calendars = CalendarFiles.Where(calendar => names.Contains(calendar.Id, StringComparer.Ordinal)).ToList();

        if (names.Length != calendars.Count ||
            !names.SequenceEqual(calendars.Select(static calendar => calendar.Id), StringComparer.Ordinal) ||
            calendars.Any(calendar => calendar.Parent is not null && !names.Contains(calendar.Parent, StringComparer.Ordinal)))
        {
            throw new InvalidDataException("the calendars `" + string.Join(", ", names) + "` are not a subset of `" + string.Join(", ", CalendarFiles.Select(static calendar => calendar.Id)) + "` in its order, each with the calendar it is a layer over");
        }

        foreach (var language in languages)
        {
            JsonElement Calendar(string package, string file) =>
                archive.Json("json/" + package + "/main/" + language + "/" + file + ".json").GetProperty("main").GetProperty(language).GetProperty("dates").GetProperty("calendars")
                    .GetProperty(file["ca-".Length..]);

            var gregorian = CalendarMap(Calendar("cldr-dates-full", "ca-gregorian"));
            var generic = CalendarMap(Calendar("cldr-dates-full", "ca-generic"));

            // THE GENERIC LAYER: its patterns over the Gregorian ones; the names stay the Gregorian ones.
            var genericLayer = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var (key, value) in gregorian)
            {
                if (!IsPattern(key))
                {
                    genericLayer[key] = value;
                }
            }

            foreach (var (key, value) in generic)
            {
                if (IsPattern(key))
                {
                    genericLayer[key] = value;
                }
            }

            Layer(language + "@generic", gregorian, genericLayer, lines);
            var layers = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (var (id, package, file, parent) in calendars)
            {
                var below = parent is null ? genericLayer : layers[parent];
                var own = CalendarMap(Calendar(package, file));

                if (id == "japanese")
                {
                    foreach (var key in own.Keys.Where(static key => key.StartsWith("eras.", StringComparison.Ordinal) &&
                        int.Parse(key[(key.LastIndexOf('.') + 1)..], NumberStyles.Integer, NumberFormatInfo.InvariantInfo) < 232).ToList())
                    {
                        own.Remove(key);
                    }
                }

                // A NAME THE CALENDAR DOES NOT STATE IS THE LAYER BELOW'S.
                var layer = new Dictionary<string, string>(own, StringComparer.Ordinal);

                foreach (var (key, value) in below)
                {
                    if (!IsPattern(key))
                    {
                        layer.TryAdd(key, value);
                    }
                }

                Layer(language + "@" + id, below, layer, lines);
                layers[id] = layer;
            }
        }

        return lines;
    }

    /// <summary>The lines that turn <paramref name="below"/> into <paramref name="layer"/>: changed values and removed keys.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 calendar layers via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=F693C2
    // Broiler-Human:        PENDING
    private static void Layer(string name, Dictionary<string, string> below, Dictionary<string, string> layer, List<string> lines)
    {
        foreach (var (key, value) in layer)
        {
            if (!below.TryGetValue(key, out var under) || under != value)
            {
                lines.Add(name + "|" + key + "|" + Escape(value));
            }
        }

        foreach (var key in below.Keys)
        {
            if (!layer.ContainsKey(key))
            {
                lines.Add(name + "|-" + key + "|");
            }
        }
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 calendar layers via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=4383FB
    // Broiler-Human:        PENDING
    private static bool IsPattern(string key) =>
        key.StartsWith("dateFormats.", StringComparison.Ordinal) || key.StartsWith("timeFormats.", StringComparison.Ordinal) ||
        key.StartsWith("dateTime.", StringComparison.Ordinal) || key.StartsWith("atTime.", StringComparison.Ordinal) ||
        key.StartsWith("available.", StringComparison.Ordinal) || key.StartsWith("append.", StringComparison.Ordinal) ||
        key.StartsWith("interval.", StringComparison.Ordinal);

    /// <summary>A calendar's names and patterns, flattened to the date table's keys, unescaped.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 calendar layers via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=EBD949
    // Broiler-Human:        PENDING
    private static Dictionary<string, string> CalendarMap(JsonElement calendar)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        void Flatten(string key, JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                map[key] = value.GetString()!;
                return;
            }

            foreach (var property in value.EnumerateObject())
            {
                if (!property.Name.Contains("-alt-", StringComparison.Ordinal) && !property.Name.Contains("-count-", StringComparison.Ordinal))
                {
                    Flatten(key + "." + property.Name, property.Value);
                }
            }
        }

        foreach (var name in new[] { "months", "days", "dayPeriods", "eras", "monthPatterns", "dateFormats", "timeFormats" })
        {
            if (calendar.TryGetProperty(name, out var value))
            {
                Flatten(name, value);
            }
        }

        if (calendar.TryGetProperty("cyclicNameSets", out var cyclic))
        {
            Flatten("cyclic.years", cyclic.GetProperty("years").GetProperty("format"));
        }

        var dateTime = calendar.GetProperty("dateTimeFormats");

        foreach (var style in new[] { "full", "long", "medium", "short" })
        {
            map["dateTime." + style] = dateTime.GetProperty(style).GetString()!;

            if (calendar.TryGetProperty("dateTimeFormats-atTime", out var atTime))
            {
                map["atTime." + style] = atTime.GetProperty("standard").GetProperty(style).GetString()!;
            }
        }

        Flatten("available", dateTime.GetProperty("availableFormats"));
        Flatten("append", dateTime.GetProperty("appendItems"));

        foreach (var entry in dateTime.GetProperty("intervalFormats").EnumerateObject())
        {
            if (entry.Name == "intervalFormatFallback")
            {
                map["interval.fallback"] = entry.Value.GetString()!;
            }
            else if (!entry.Name.Contains("-alt-", StringComparison.Ordinal))
            {
                Flatten("interval." + entry.Name, entry.Value);
            }
        }

        return map;
    }

    /// <summary>
    /// The ISO 8601 calendar's patterns, from <c>common/main/root.xml</c>, under the pseudo-locale
    /// <c>@iso8601</c>, flattened to the Gregorian data's keys: the date, time and date-time patterns, the
    /// available formats, append items and interval formats. Its names are aliases of each locale's Gregorian
    /// names, so none is carried; and no locale of the release overrides its patterns (JSD-0055).
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 ISO 8601 patterns via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=8D7ECB
    // Broiler-Human:        PENDING
    private static List<string> Iso8601Patterns(Archive archive)
    {
        const string Language = "@iso8601";
        var root = XDocument.Parse(Encoding.UTF8.GetString(archive.Bytes(Root + "common/main/root.xml")));
        var calendar = root.Descendants("calendar").Single(static element => (string?)element.Attribute("type") == "iso8601");
        var lines = new List<string>();

        static bool Plain(XElement element) =>
            element.Attribute("alt") is null && element.Attribute("count") is null;

        foreach (var (group, length, kind) in new[] { ("dateFormats", "dateFormatLength", "dateFormat"), ("timeFormats", "timeFormatLength", "timeFormat") })
        {
            foreach (var style in calendar.Element(group)!.Elements(length))
            {
                var pattern = style.Element(kind)!.Elements("pattern").Single(Plain).Value;
                lines.Add(Language + "|" + group + "." + (string)style.Attribute("type")! + "|" + Escape(pattern));
            }
        }

        var dateTime = calendar.Element("dateTimeFormats")!;

        foreach (var style in dateTime.Elements("dateTimeFormatLength"))
        {
            var type = (string)style.Attribute("type")!;
            var standard = style.Elements("dateTimeFormat").Single(static element => element.Attribute("type") is null).Element("pattern")!.Value;

            // THE atTime FORM IS AN ALIAS OF THE STANDARD ONE in this calendar, so both keys hold it.
            lines.Add(Language + "|dateTime." + type + "|" + Escape(standard));
            lines.Add(Language + "|atTime." + type + "|" + Escape(standard));
        }

        foreach (var item in dateTime.Element("availableFormats")!.Elements("dateFormatItem").Where(Plain))
        {
            lines.Add(Language + "|available." + (string)item.Attribute("id")! + "|" + Escape(item.Value));
        }

        foreach (var item in dateTime.Element("appendItems")!.Elements("appendItem"))
        {
            lines.Add(Language + "|append." + (string)item.Attribute("request")! + "|" + Escape(item.Value));
        }

        var intervals = dateTime.Element("intervalFormats")!;
        lines.Add(Language + "|interval.fallback|" + Escape(intervals.Element("intervalFormatFallback")!.Value));

        foreach (var item in intervals.Elements("intervalFormatItem").Where(Plain))
        {
            foreach (var difference in item.Elements("greatestDifference").Where(Plain))
            {
                lines.Add(Language + "|interval." + (string)item.Attribute("id")! + "." + (string)difference.Attribute("id")! + "|" + Escape(difference.Value));
            }
        }

        return lines;
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 date tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=88C2A2
    // Broiler-Human:        PENDING
    private static void FlattenDates(string language, string key, JsonElement value, List<string> lines)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                if (!property.Name.Contains("-alt-", StringComparison.Ordinal) && !property.Name.Contains("-count-", StringComparison.Ordinal))
                {
                    FlattenDates(language, key + "." + property.Name, property.Value, lines);
                }
            }

            return;
        }

        lines.Add(language + "|" + key + "|" + Escape(value.GetString()!));
    }
}
