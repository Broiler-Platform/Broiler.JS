// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsZoneNames.cs
//     blob e5012a2eb450992d5680b64980ed9bba4eaf352a: the whole file: JsZoneNaming and JsZoneNames
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// What a format names its zone with (JSD-0058): the metazone data, the region whose golden zones
/// apply, and the locale's name of a region.
/// </summary>
internal sealed record JsZoneNaming(JsZoneNames Names, string Region, System.Func<string, string?> RegionName);

/// <summary>
/// The names a time zone is written with (JSD-0058): CLDR's metazones, their golden zones and the
/// countries' primary zones, and the algorithms ICU's <c>TimeZoneFormat</c> and
/// <c>TimeZoneGenericNames</c> name a zone by, over each language's names in the date data.
/// </summary>
/// <remarks>
/// <para>
/// <b>A specific name</b> (<c>z</c>, <c>zzzz</c>) is the zone's own standard or daylight name, or its
/// metazone's at the instant; <b>a generic name</b> (<c>v</c>, <c>vvvv</c>) is the zone's own generic
/// name, or its metazone's - its standard name where the zone keeps no daylight time within 184 days,
/// and a partial location name where the metazone's golden zone for the locale's region keeps another
/// offset - or else the zone's country, where it is the country's only or primary zone, or exemplar
/// city, in the region format. A zone named neither way is written in CLDR's GMT format.
/// </para>
/// <para>
/// <b>A metazone's period</b> without a start begins at 1970-01-01 00:00 UTC and one without an end
/// ends at 9999-12-31 23:59 UTC, as ICU reads CLDR's.
/// </para>
/// </remarks>
internal sealed class JsZoneNames
{
    private const long NoEnd = 253_402_300_740_000L;

    private const long DaylightRange = 184L * 86_400;

    private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(long From, long To, string Metazone)>> periods =
        new(System.StringComparer.Ordinal);

    private readonly System.Collections.Generic.Dictionary<string, string> golden = new(System.StringComparer.Ordinal);

    private readonly System.Collections.Generic.Dictionary<string, string> primaryZones = new(System.StringComparer.Ordinal);

    private readonly System.Collections.Generic.Dictionary<string, string> countries = new(System.StringComparer.Ordinal);

    private readonly System.Collections.Generic.Dictionary<string, int> zonesInRegion = new(System.StringComparer.Ordinal);

    private readonly ITimeZoneDatabase zones;

    internal JsZoneNames(System.Collections.Generic.IEnumerable<string> metaZones, ITimeZoneDatabase zones)
    {
        this.zones = zones;

        foreach (var line in metaZones)
        {
            var fields = line.Split('|');

            switch (fields[0])
            {
                case "z":
                    if (!periods.TryGetValue(fields[1], out var list))
                    {
                        periods[fields[1]] = list = [];
                    }

                    list.Add((fields[2].Length == 0 ? 0 : Time(fields[2]), fields[3].Length == 0 ? NoEnd : Time(fields[3]), fields[4]));
                    break;

                case "g":
                    golden[fields[1] + "|" + fields[2]] = fields[3];
                    break;

                case "p":
                    primaryZones[fields[1]] = fields[2];
                    break;
            }
        }

        // A ZONE'S COUNTRY is zone.tab's, by primary identifier, and a country's zones are counted the
        // same way: a country with one is named by it. zone.tab's regions are ISO 3166 codes, two
        // ASCII capitals; the shared database answers the zones of each and none for a code zone.tab does
        // not list, and the codes are asked in ordinal order, the order its table lists them in.
        foreach (var region in Regions())
        {
            var listed = zones.RegionTimeZones(region);

            if (listed.Count == 0)
            {
                continue;
            }

            var distinct = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            foreach (var name in listed)
            {
                if (zones.TryFind(name, out var id))
                {
                    var primary = zones.Primary(id).Identifier;
                    countries.TryAdd(primary, region);
                    distinct.Add(primary);
                }
            }

            zonesInRegion[region] = distinct.Count;
        }
    }

    /// <summary>Every region code of two ASCII capitals, in ordinal order.</summary>
    private static System.Collections.Generic.IEnumerable<string> Regions()
    {
        for (var first = 'A'; first <= 'Z'; first++)
        {
            for (var second = 'A'; second <= 'Z'; second++)
            {
                yield return new string([first, second]);
            }
        }
    }

    /// <summary>A metazone period's bound, <c>yyyy-MM-dd HH:mm</c> UTC, in milliseconds.</summary>
    private static long Time(string text)
    {
        static int Number(string text, int at, int length) =>
            int.Parse(text.Substring(at, length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);

        var days = IsoCalendarMath.EpochDays(Number(text, 0, 4), Number(text, 5, 2), Number(text, 8, 2));
        return ((days * 1440) + (Number(text, 11, 2) * 60) + Number(text, 14, 2)) * 60_000;
    }

    /// <summary>The metazone a zone uses at a time value, or nothing.</summary>
    internal string? Metazone(string zone, long time)
    {
        if (periods.TryGetValue(zone, out var list))
        {
            foreach (var (from, to, metazone) in list)
            {
                if (time >= from && time < to)
                {
                    return metazone;
                }
            }
        }

        return null;
    }

    /// <summary>A metazone's golden zone in a region, or in the world's (<c>001</c>) where the region has none.</summary>
    internal string? Golden(string metazone, string region) =>
        golden.TryGetValue(metazone + "|" + region, out var zone) || golden.TryGetValue(metazone + "|001", out zone) ? zone : null;

    /// <summary>
    /// A zone's specific name at a time value, long or short, in a language's date data: its own name
    /// for standard or daylight time, or its metazone's; nothing where neither has one.
    /// </summary>
    internal string? Specific(JsDateData data, string language, string zone, ITimeZoneRules offsets, long time, bool longForm)
    {
        var type = (longForm ? "l" : "s") + (offsets.IsDaylightAt(Seconds(time)) ? "d" : "s");
        return Specific(data, language, zone, time, type);
    }

    private string? Specific(JsDateData data, string language, string zone, long time, string type) =>
        data.Value(language, "tz." + zone + "." + type) ??
        (Metazone(zone, time) is { } metazone ? data.Value(language, "mz." + metazone + "." + type) : null);

    /// <summary>
    /// A zone's generic name at a time value, long or short (ICU's TZGNCore::getDisplayName): its
    /// non-location name, or its location name; nothing where it has neither.
    /// </summary>
    internal string? Generic(JsDateData data, string language, string region, System.Func<string, string?> regionName, string zone, ITimeZoneRules offsets, long time, bool longForm) =>
        NonLocation(data, language, region, regionName, zone, offsets, time, longForm) ?? Location(data, language, regionName, zone);

    /// <summary>ICU's formatGenericNonLocationName.</summary>
    private string? NonLocation(JsDateData data, string language, string region, System.Func<string, string?> regionName, string zone, ITimeZoneRules offsets, long time, bool longForm)
    {
        var width = longForm ? "l" : "s";

        if (data.Value(language, "tz." + zone + "." + width + "g") is { } own)
        {
            return own;
        }

        if (Metazone(zone, time) is not { } metazone)
        {
            return null;
        }

        var seconds = Seconds(time);
        var generic = data.Value(language, "mz." + metazone + "." + width + "g");

        // STANDARD TIME IS NAMED AS SUCH where the zone keeps no daylight time within 184 days of the
        // instant, unless the standard name is the generic one.
        if (!offsets.IsDaylightAt(seconds) && !DaylightNear(offsets, seconds) &&
            Specific(data, language, zone, time, width + "s") is { } standard &&
            (generic is null || !string.Equals(standard, generic, System.StringComparison.OrdinalIgnoreCase)))
        {
            return standard;
        }

        if (generic is null)
        {
            return null;
        }

        // A ZONE THAT DOES NOT KEEP ITS METAZONE'S GOLDEN ZONE'S TIME at the instant is named by its
        // location as well.
        if (Golden(metazone, region) is { } goldenZone && goldenZone != zone && zones.TryFind(goldenZone, out var id))
        {
            var reference = zones.Rules(id);

            if (reference.OffsetSecondsAt(seconds) != offsets.OffsetSecondsAt(seconds) || reference.IsDaylightAt(seconds) != offsets.IsDaylightAt(seconds))
            {
                return Partial(data, language, regionName, zone, metazone, generic);
            }
        }

        return generic;
    }

    /// <summary>Whether the transition before or after an instant, within 184 days, borders daylight time.</summary>
    private static bool DaylightNear(ITimeZoneRules offsets, long seconds)
    {
        if (offsets.PreviousTransition(seconds + 1, daylightFlag: true) is { } before && seconds - before < DaylightRange && offsets.IsDaylightAt(before - 1))
        {
            return true;
        }

        return offsets.NextTransition(seconds, seconds + DaylightRange, daylightFlag: true) is { } after && after - seconds < DaylightRange && offsets.IsDaylightAt(after);
    }

    /// <summary>ICU's getPartialLocationName: the metazone's generic name and the zone's location in the fallback format.</summary>
    private string Partial(JsDateData data, string language, System.Func<string, string?> regionName, string zone, string metazone, string generic)
    {
        string location;

        if (countries.TryGetValue(zone, out var country))
        {
            location = Golden(metazone, country) == zone ? regionName(country) ?? country : City(data, language, zone) ?? zone;
        }
        else
        {
            location = City(data, language, zone) ?? zone;
        }

        return Format(data.Value(language, "zone.fallbackFormat") ?? "{1} ({0})", location, generic);
    }

    /// <summary>ICU's getGenericLocationName: the zone's country, where it is the only or primary one, or its city.</summary>
    private string? Location(JsDateData data, string language, System.Func<string, string?> regionName, string zone)
    {
        if (!countries.TryGetValue(zone, out var country))
        {
            return null;
        }

        var primary = zonesInRegion.TryGetValue(country, out var count) && count == 1 ||
            primaryZones.TryGetValue(country, out var first) && first == zone;
        var location = primary ? regionName(country) ?? country : City(data, language, zone);
        return location is null ? null : Format(data.Value(language, "zone.regionFormat") ?? "{0}", location, null);
    }

    /// <summary>A zone's exemplar city: the date data's, or the last segment of its identifier.</summary>
    private static string? City(JsDateData data, string language, string zone) =>
        data.Value(language, "tz." + zone + ".city") ??
        (zone.StartsWith("Etc/", System.StringComparison.Ordinal) || !zone.Contains('/', System.StringComparison.Ordinal)
            ? null
            : zone[(zone.LastIndexOf('/') + 1)..].Replace('_', ' '));

    private static string Format(string pattern, string first, string? second)
    {
        var text = pattern.Replace("{0}", first, System.StringComparison.Ordinal);
        return second is null ? text : text.Replace("{1}", second, System.StringComparison.Ordinal);
    }

    private static long Seconds(long time) => (long)System.Math.Floor(time / 1000.0);
}
