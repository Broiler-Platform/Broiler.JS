// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript.Format/JsIntlData.cs
//     blob fd0dd0f90140eeeb9a5b2f31a8f600d12dd98f84: the whole file: IJsIntlData and JsIntlTable
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The tables the internationalization surface is built from, which a composition admitting
/// <see cref="JsSurfaces.Intl"/> hands to the profile (JSD-0043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Data the composition supplies, so a composition that declines the surface carries none of
/// it</b> (JSD-0027 section 5 item 3). The tables are generated from the pinned CLDR release into an
/// assembly of their own; the profile reads them through this interface and references no such
/// assembly, as it reads emitted machine code through <see cref="IJsNativeEmitter"/>.
/// </para>
/// <para>
/// <b>Each table is bytes in a form its reader in the profile decodes</b>, named by
/// <see cref="JsIntlTable"/>. An implementation answers the same bytes for the life of the process,
/// and the profile may decode a table once and keep what it decoded.
/// </para>
/// </remarks>
public interface IJsIntlData
{
    /// <summary>The CLDR release the tables were generated from, e.g. <c>48.2.0</c>.</summary>
    string CldrVersion { get; }

    /// <summary>One table's bytes; empty for a table this data does not carry.</summary>
    System.ReadOnlySpan<byte> Table(JsIntlTable table);
}

/// <summary>The tables an <see cref="IJsIntlData"/> carries.</summary>
/// <remarks>
/// The text tables are ASCII, one record per line and fields separated by <c>|</c>, in the order the
/// generator writes them; a character a line cannot carry is written <c>\uXXXX</c>. The collation
/// tables are binary and their reader states their layout.
/// </remarks>
public enum JsIntlTable
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>CLDR's likely subtags: <c>from</c>, <c>to</c>.</summary>
    LikelySubtags = 1,

    /// <summary>CLDR's aliases: <c>kind</c>, <c>from</c>, <c>replacement</c>, for the language, script, region, variant and subdivision kinds.</summary>
    Aliases = 2,

    /// <summary>The BCP 47 extension keys and types: <c>extension</c>, <c>key</c>, <c>type</c>, <c>preferred</c>.</summary>
    Extensions = 3,

    /// <summary>The locales this data supports, one per line, in canonical form.</summary>
    Locales = 4,

    /// <summary>The root collation, binary.</summary>
    CollationRoot = 5,

    /// <summary>The tailorings the supported locales use, binary, in weights scaled by 256.</summary>
    CollationTailorings = 6,

    // 7 was the ranges of Soft_Dotted until SJL-7.4, when Lithuanian's casing moved to the Unicode area's
    // owner, whose casing tables carry them (JSD-0074); the number is not reused.

    /// <summary>Each language's number data for <c>latn</c>: <c>language</c>, a dotted <c>key</c>, <c>value</c>.</summary>
    NumberLocales = 8,

    /// <summary>
    /// Each language's currencies: <c>language</c>, <c>code</c>, <c>symbol</c>, <c>narrow</c>, <c>one</c>,
    /// <c>other</c>, <c>name</c>, and for the symbol and the narrow symbol whether its first and last
    /// characters are symbols or separators (<c>S</c>) or not (<c>L</c>).
    /// </summary>
    Currencies = 9,

    /// <summary>The currencies whose fraction digits are not 2: <c>code</c>, <c>digits</c>.</summary>
    CurrencyDigits = 10,

    /// <summary>The numbering systems with a simple digit mapping: <c>name</c>, <c>digits</c>.</summary>
    NumberingSystems = 11,

    /// <summary>Each language's cardinal plural rules: <c>language</c>, <c>category</c>, <c>rule</c>.</summary>
    Plurals = 12,

    /// <summary>Each language's plural range rules: <c>language</c>, <c>start</c>, <c>end</c>, <c>result</c>.</summary>
    PluralRanges = 13,

    /// <summary>Each language's unit patterns: <c>language</c>, <c>width</c>, <c>unit</c>, <c>field</c>, <c>value</c>.</summary>
    Units = 14,

    /// <summary>
    /// Each language's Gregorian calendar, date field and zone name data: <c>language</c>, a dotted
    /// <c>key</c>, <c>value</c>.
    /// </summary>
    DateLocales = 15,

    /// <summary>The hour cycles of a region: <c>region</c>, <c>allowed</c>, <c>preferred</c>.</summary>
    TimeData = 16,

    /// <summary>Each language's day period rules: <c>language</c>, <c>period</c>, <c>at</c> or <c>from</c>, <c>before</c>.</summary>
    DayPeriods = 17,

    /// <summary>
    /// Each region's week: <c>region</c>, <c>firstDay</c>, <c>weekendStart</c>, <c>weekendEnd</c>,
    /// <c>minDays</c>, a field empty where the world's (<c>001</c>) applies.
    /// </summary>
    WeekData = 18,

    /// <summary>Each script's line direction: <c>script</c>, <c>YES</c> for right to left or <c>NO</c>.</summary>
    Scripts = 19,

    /// <summary>Each language's ordinal plural rules: <c>language</c>, <c>category</c>, <c>rule</c>.</summary>
    Ordinals = 20,

    /// <summary>
    /// Each language's list patterns: <c>language</c>, <c>type</c> as CLDR names it, and the
    /// <c>start</c>, <c>middle</c>, <c>end</c> and two-element patterns.
    /// </summary>
    ListPatterns = 21,

    /// <summary>
    /// Each language's relative time data: <c>language</c>, <c>field</c> (a unit, or a unit with
    /// <c>-short</c> or <c>-narrow</c>), <c>key</c> (a value's literal, or <c>future.</c> or
    /// <c>past.</c> and a plural category) and the pattern.
    /// </summary>
    RelativeTimes = 22,

    // 23 and 24 were the break properties' value names and the break table until SJL-7.4, when
    // segmentation moved to the Unicode area's owner and its table to the Intl assembly's own
    // provider (JSD-0074); the numbers are not reused.

    /// <summary>
    /// Each language's display names: <c>language</c>, <c>kind</c> (language, region, script, variant,
    /// pattern, calendar or field), <c>code</c>, and the name.
    /// </summary>
    DisplayNames = 25,

    /// <summary>
    /// The IANA Time Zone Database's zones' UTC offsets (JSD-0053): a zone count, then each zone's
    /// first offset, offset palette, transitions and recurring rules, in the layout the shared
    /// library's <c>TimeZoneDatabase</c> reads (JSD-0076; until SJL-8.5 the profile's own
    /// <c>JsTimeZones</c> read it).
    /// </summary>
    TimeZones = 26,

    /// <summary>
    /// Every IANA Zone and Link name: <c>identifier</c>, the index of the zone whose offsets it reads,
    /// and its primary identifier where that is another.
    /// </summary>
    TimeZoneIds = 27,

    /// <summary>The zones of each country, from <c>zone.tab</c>: <c>region</c>, then its zones.</summary>
    TimeZoneRegions = 28,

    /// <summary>
    /// The published years of the Chinese, Korean and Umm al-Qura calendars and the Persian
    /// calendar's corrections to its 33-year rule, binary, in the layout the shared library's
    /// <c>CalendarTables</c> reads (JSD-0076).
    /// </summary>
    Calendars = 29,

    /// <summary>
    /// CLDR's metazone data (JSD-0058): <c>z</c>, a zone, the start and end of a period and the
    /// metazone it uses then; <c>g</c>, a metazone, a region and its golden zone there; <c>p</c>, a
    /// country and its primary zone.
    /// </summary>
    MetaZones = 30,

    /// <summary>
    /// The IANA Time Zone Database release the three time zone tables were compiled from, ASCII, for
    /// example <c>2026e</c>: the release the shared library's <c>TimeZoneDatabase</c> states for them
    /// (JSD-0076; added at SJL-8.5). Data that does not carry it reads as before, and the database
    /// states the release <c>unstated</c>; nothing in the profile reads the release (JSD-0077).
    /// </summary>
    TimeZoneRelease = 31,
}
