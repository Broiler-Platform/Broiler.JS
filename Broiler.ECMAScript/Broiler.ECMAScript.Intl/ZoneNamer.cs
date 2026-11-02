// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   19
// Annotated:        19/19
// Exempt:           2
// Human-reviewed:   0/19
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  2/10 max
// Unverified:       19
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// What a format names its zone with (JSD-0058): the metazone data, the region whose golden zones
/// apply, and the locale's name of a region.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=0; Fingerprint=64DAB1
// Broiler-Human:        PENDING
internal sealed record ZoneNaming(ZoneNamer Names, string Region, System.Func<string, string?> RegionName);

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
// Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=0; Fingerprint=FF899A
// Broiler-Human:        PENDING
internal sealed class ZoneNamer
{
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=0; Fingerprint=EB587D
    // Broiler-Human:        PENDING
    private const long DaylightRange = 184L * 86_400;

    private readonly ILocaleData metazones;

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=0; Fingerprint=FBFC13
    // Broiler-Human:        PENDING
    private readonly System.Collections.Generic.Dictionary<string, string> countries = new(System.StringComparer.Ordinal);

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=0; Fingerprint=26B61B
    // Broiler-Human:        PENDING
    private readonly System.Collections.Generic.Dictionary<string, int> zonesInRegion = new(System.StringComparer.Ordinal);

    private readonly ITimeZoneDatabase zones;

    /// <summary>
    /// The names of the zones of a database, over the locale data's metazones (<see cref="ILocaleData.MetazonePeriods"/>,
    /// <see cref="ILocaleData.GoldenZone"/>, <see cref="ILocaleData.PrimaryZone"/>), which the provider decodes as Broiler.VM's
    /// constructor read the table's lines.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Medium; Resources=2; Fingerprint=C7EA2B
    // Broiler-Human:        PENDING
    internal ZoneNamer(ILocaleData metazones, ITimeZoneDatabase zones)
    {
        this.metazones = metazones;
        this.zones = zones;

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
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=1; Fingerprint=211E36
    // Broiler-Human:        PENDING
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

    /// <summary>The metazone a zone uses at a time value, or nothing.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=High; Resources=2; Fingerprint=20DA34
    // Broiler-Falsified-If: a zone's metazone at an instant differs from CLDR metaZones' period that holds it
    // Broiler-Human:        PENDING
    internal string? Metazone(string zone, long time)
    {
        if (metazones.MetazonePeriods(zone) is { } list)
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
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=1; Fingerprint=A3229E
    // Broiler-Human:        PENDING
    internal string? Golden(string metazone, string region) =>
        metazones.GoldenZone(metazone, region) ?? metazones.GoldenZone(metazone, "001");

    /// <summary>
    /// A zone's specific name at a time value, long or short, in a language's date data: its own name
    /// for standard or daylight time, or its metazone's; nothing where neither has one.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Medium; Resources=1; Fingerprint=324282
    // Broiler-Human:        PENDING
    internal string? Specific(DateData data, string language, string zone, ITimeZoneRules offsets, long time, bool longForm)
    {
        var type = (longForm ? "l" : "s") + (offsets.IsDaylightAt(Seconds(time)) ? "d" : "s");
        return Specific(data, language, zone, time, type);
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Medium; Resources=1; Fingerprint=0CA65D
    // Broiler-Human:        PENDING
    private string? Specific(DateData data, string language, string zone, long time, string type) =>
        data.Value(language, "tz." + zone + "." + type) ??
        (Metazone(zone, time) is { } metazone ? data.Value(language, "mz." + metazone + "." + type) : null);

    /// <summary>
    /// A zone's generic name at a time value, long or short (ICU's TZGNCore::getDisplayName): its
    /// non-location name, or its location name; nothing where it has neither.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=High; Resources=2; Fingerprint=D9E938
    // Broiler-Falsified-If: a generic zone name differs from UTS 35 Part 4 section 7.3's metazone, golden zone and location fallbacks
    // Broiler-Human:        PENDING
    internal string? Generic(DateData data, string language, string region, System.Func<string, string?> regionName, string zone, ITimeZoneRules offsets, long time, bool longForm) =>
        NonLocation(data, language, region, regionName, zone, offsets, time, longForm) ?? Location(data, language, regionName, zone);

    /// <summary>ICU's formatGenericNonLocationName.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Medium; Resources=1; Fingerprint=E76C05
    // Broiler-Human:        PENDING
    private string? NonLocation(DateData data, string language, string region, System.Func<string, string?> regionName, string zone, ITimeZoneRules offsets, long time, bool longForm)
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
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=1; Fingerprint=300908
    // Broiler-Human:        PENDING
    private static bool DaylightNear(ITimeZoneRules offsets, long seconds)
    {
        if (offsets.PreviousTransition(seconds + 1, daylightFlag: true) is { } before && seconds - before < DaylightRange && offsets.IsDaylightAt(before - 1))
        {
            return true;
        }

        return offsets.NextTransition(seconds, seconds + DaylightRange, daylightFlag: true) is { } after && after - seconds < DaylightRange && offsets.IsDaylightAt(after);
    }

    /// <summary>ICU's getPartialLocationName: the metazone's generic name and the zone's location in the fallback format.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Medium; Resources=1; Fingerprint=715654
    // Broiler-Human:        PENDING
    private string Partial(DateData data, string language, System.Func<string, string?> regionName, string zone, string metazone, string generic)
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
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Medium; Resources=1; Fingerprint=6BA9C1
    // Broiler-Human:        PENDING
    private string? Location(DateData data, string language, System.Func<string, string?> regionName, string zone)
    {
        if (!countries.TryGetValue(zone, out var country))
        {
            return null;
        }

        var primary = zonesInRegion.TryGetValue(country, out var count) && count == 1 ||
            metazones.PrimaryZone(country) == zone;
        var location = primary ? regionName(country) ?? country : City(data, language, zone);
        return location is null ? null : Format(data.Value(language, "zone.regionFormat") ?? "{0}", location, null);
    }

    /// <summary>A zone's exemplar city: the date data's, or the last segment of its identifier.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=1; Fingerprint=9B49A8
    // Broiler-Human:        PENDING
    private static string? City(DateData data, string language, string zone) =>
        data.Value(language, "tz." + zone + ".city") ??
        (zone.StartsWith("Etc/", System.StringComparison.Ordinal) || !zone.Contains('/', System.StringComparison.Ordinal)
            ? null
            : zone[(zone.LastIndexOf('/') + 1)..].Replace('_', ' '));

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=1; Fingerprint=CA350E
    // Broiler-Human:        PENDING
    private static string Format(string pattern, string first, string? second)
    {
        var text = pattern.Replace("{0}", first, System.StringComparison.Ordinal);
        return second is null ? text : text.Replace("{1}", second, System.StringComparison.Ordinal);
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 7 time zone names and CLDR metaZones via Broiler.VM JsZoneNames; IP=Low; Security=Low; Resources=1; Fingerprint=E94CD8
    // Broiler-Human:        PENDING
    private static long Seconds(long time) => (long)System.Math.Floor(time / 1000.0);
}
