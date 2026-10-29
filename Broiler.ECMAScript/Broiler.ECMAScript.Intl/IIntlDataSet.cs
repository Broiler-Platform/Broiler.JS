// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           30
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The locale data an engine hands the library: the tables the data home's generator writes, one span per
/// <see cref="IntlTable"/>, the first of the two layers of the data contract (Broiler.VM's JSD-0082, decision
/// D8; the home's design, section 1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Data the engine supplies, never data this library carries</b> (decision SJL-D-6 (c)). Broiler.VM hands in
/// its <c>JsCldrTables.g.cs</c> through an adapter of its own <c>IJsIntlData</c>; Broiler.JS hands in the files of
/// <c>Broiler.ECMAScript/data/intl/generated/</c>, embedded as resources. Both are the generator's output in one
/// format, which <see cref="IntlTable"/> documents table by table.
/// </para>
/// <para>
/// <b>An implementation answers the same bytes for the life of the process.</b> The library decodes each table
/// once, lazily and thread-safely, and keeps what it decoded (decision D23's L3 reading).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 data contract; IP=Low; Security=Medium; Resources=1; Fingerprint=588868
// Broiler-Human:        PENDING
public interface IIntlDataSet
{
    /// <summary>The CLDR release the tables were generated from, for example <c>48.2.0</c> (convention 13: data versions are public).</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 data contract; IP=Low; Security=Low; Resources=0; Fingerprint=421618
    // Broiler-Human:        PENDING
    string CldrVersion { get; }

    /// <summary>One table's bytes, in the format <see cref="IntlTable"/> documents; empty for a table this data does not carry.</summary>
    /// <param name="table">The table.</param>
    /// <returns>The table's bytes, or an empty span.</returns>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 data contract; IP=Low; Security=Medium; Resources=1; Fingerprint=9F1961
    // Broiler-Human:        PENDING
    ReadOnlySpan<byte> Table(IntlTable table);
}

/// <summary>
/// The tables an <see cref="IIntlDataSet"/> carries, numbered as Broiler.VM's <c>JsIntlTable</c> numbers them (JSD-0082,
/// decision D8): <c>7</c>, <c>23</c> and <c>24</c>, which SJL-7.4 retired there, stay retired and are not reused, and a
/// value is appended only for a table Broiler.VM's data never carries, which every engine that lacks it answers as an
/// empty span.
/// </summary>
/// <remarks>
/// <para>
/// <b>The text tables</b> are ASCII, one record per line, LF between lines, fields separated by <c>|</c> in the order
/// the generator writes them; a character a line cannot carry is written <c>\uXXXX</c>. <b>The binary tables</b>
/// (collation, time zones, calendars) state their own layouts. The format is versioned by the data home: a change to it
/// is a major version of this library (the home's design, section 3).
/// </para>
/// <para>
/// The time zone and calendar tables (26 to 29 and 31) are read by <c>Broiler.ECMAScript.Algorithms</c>' decoders, and the
/// collation tables (5 and 6) by milestone SJL-17's; they keep their numbers here so that one enumeration names every
/// table Broiler.VM's data hands in.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table numbers; IP=Low; Security=Low; Resources=0; Fingerprint=6972CA
// Broiler-Human:        PENDING
public enum IntlTable
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>CLDR's likely subtags (UTS #35 Part 1, Likely Subtags): <c>from</c>, <c>to</c>.</summary>
    LikelySubtags = 1,

    /// <summary>
    /// CLDR's aliases (UTS #35 Part 1, Annex C): <c>kind</c>, <c>from</c>, <c>replacement</c>, for the <c>language</c>,
    /// <c>script</c>, <c>region</c>, <c>variant</c> and <c>subdivision</c> kinds.
    /// </summary>
    Aliases = 2,

    /// <summary>The BCP 47 extension keys and types (UTS #35 Part 1, section 3.6): <c>extension</c>, <c>key</c>, <c>type</c>, <c>preferred</c>.</summary>
    Extensions = 3,

    /// <summary>The locales this data supports, one per line, in canonical form.</summary>
    Locales = 4,

    /// <summary>The root collation, binary (milestone SJL-17's).</summary>
    CollationRoot = 5,

    /// <summary>The tailorings the supported locales use, binary, in weights scaled by 256 (milestone SJL-17's).</summary>
    CollationTailorings = 6,

    // 7 was the ranges of Soft_Dotted until SJL-7.4, when Lithuanian's casing moved to the Unicode area's owner (Broiler.VM's
    // JSD-0074); the number is not reused.

    /// <summary>
    /// Each language's number data: <c>language</c>, a dotted <c>key</c>, <c>value</c>. The symbols of a numbering system
    /// other than <c>latn</c> are keys <c>symbols@&lt;system&gt;.&lt;name&gt;</c> in an embedding that carries them
    /// (decision D21).
    /// </summary>
    NumberLocales = 8,

    /// <summary>
    /// Each language's currencies: <c>language</c>, <c>code</c>, <c>symbol</c>, <c>narrow</c>, <c>one</c>,
    /// <c>other</c>, <c>name</c>, and for the symbol and the narrow symbol whether its first and last characters are
    /// symbols or separators (<c>S</c>) or not (<c>L</c>).
    /// </summary>
    Currencies = 9,

    /// <summary>The currencies whose fraction digits are not 2 (ISO 4217, as CLDR's supplemental data gives them): <c>code</c>, <c>digits</c>.</summary>
    CurrencyDigits = 10,

    /// <summary>The numbering systems with a simple digit mapping (UTS #35 Part 3, section 1): <c>name</c>, <c>digits</c>.</summary>
    NumberingSystems = 11,

    /// <summary>Each language's cardinal plural rules (UTS #35 Part 3, section 5): <c>language</c>, <c>category</c>, <c>rule</c>.</summary>
    Plurals = 12,

    /// <summary>Each language's plural range rules: <c>language</c>, <c>start</c>, <c>end</c>, <c>result</c>.</summary>
    PluralRanges = 13,

    /// <summary>Each language's unit patterns: <c>language</c>, <c>width</c>, <c>unit</c>, <c>field</c>, <c>value</c>.</summary>
    Units = 14,

    /// <summary>
    /// Each language's calendar, date field and zone name data: <c>language</c>, a dotted <c>key</c>, <c>value</c>; a
    /// language of the form <c>language@calendar</c> is a layer over the language's Gregorian data, and a key there
    /// beginning with <c>-</c> removes the key below it.
    /// </summary>
    DateLocales = 15,

    /// <summary>The hour cycles of a region (UTS #35 Part 4, Time Data): <c>region</c>, <c>allowed</c>, <c>preferred</c>.</summary>
    TimeData = 16,

    /// <summary>Each language's day period rules (UTS #35 Part 4, Day Period Rules): <c>language</c>, <c>period</c>, <c>at</c> or <c>from</c>, <c>before</c>.</summary>
    DayPeriods = 17,

    /// <summary>
    /// Each region's week (UTS #35 Part 4, Week Data): <c>region</c>, <c>firstDay</c>, <c>weekendStart</c>,
    /// <c>weekendEnd</c>, <c>minDays</c>, a field empty where the world's (<c>001</c>) applies.
    /// </summary>
    WeekData = 18,

    /// <summary>Each script's line direction (CLDR's script metadata): <c>script</c>, <c>YES</c> for right to left or <c>NO</c>.</summary>
    Scripts = 19,

    /// <summary>Each language's ordinal plural rules (UTS #35 Part 3, section 5): <c>language</c>, <c>category</c>, <c>rule</c>.</summary>
    Ordinals = 20,

    /// <summary>
    /// Each language's list patterns (UTS #35 Part 2, List Patterns): <c>language</c>, <c>type</c> as CLDR names it,
    /// and the <c>start</c>, <c>middle</c>, <c>end</c> and two-element patterns.
    /// </summary>
    ListPatterns = 21,

    /// <summary>
    /// Each language's relative time data: <c>language</c>, <c>field</c> (a unit, or a unit with <c>-short</c> or
    /// <c>-narrow</c>), <c>key</c> (a value's literal, or <c>future.</c> or <c>past.</c> and a plural category) and the
    /// pattern.
    /// </summary>
    RelativeTimes = 22,

    // 23 and 24 were the break properties' value names and the break table until SJL-7.4, when segmentation moved to the
    // Unicode area's owner (Broiler.VM's JSD-0074); the numbers are not reused.

    /// <summary>
    /// Each language's display names: <c>language</c>, <c>kind</c> (language, region, script, variant, pattern, calendar
    /// or field), <c>code</c>, and the name.
    /// </summary>
    DisplayNames = 25,

    /// <summary>The IANA Time Zone Database's zones' UTC offsets, binary, as <c>Broiler.ECMAScript.Algorithms</c>' <c>TimeZoneDatabase</c> reads them.</summary>
    TimeZones = 26,

    /// <summary>Every IANA Zone and Link name: <c>identifier</c>, the index of the zone whose offsets it reads, and its primary identifier where that is another.</summary>
    TimeZoneIds = 27,

    /// <summary>The zones of each country, from <c>zone.tab</c>: <c>region</c>, then its zones.</summary>
    TimeZoneRegions = 28,

    /// <summary>The table-backed calendars' years, binary, as <c>Broiler.ECMAScript.Algorithms</c>' <c>CalendarTables</c> reads them.</summary>
    Calendars = 29,

    /// <summary>
    /// CLDR's metazone data (UTS #35 Part 4, Metazones): <c>z</c>, a zone, the start and end of a period and the metazone
    /// it uses then; <c>g</c>, a metazone, a region and its golden zone there; <c>p</c>, a country and its primary zone.
    /// </summary>
    MetaZones = 30,

    /// <summary>The IANA Time Zone Database release the three time zone tables were compiled from, ASCII, for example <c>2026e</c>.</summary>
    TimeZoneRelease = 31,
}
