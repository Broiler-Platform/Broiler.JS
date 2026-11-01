// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsDateData.cs
//     blob fe1b2d5a850dbdd7b170518d36e3fcab25a020ce: JsDateData and JsDayPeriodRule
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the pattern generators' cache and the member that makes one (`gate`, `generators`, `Generator`), which reach JsDatePatternGenerator, sub-area (d)'s, and decode nothing.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The Gregorian calendar, hour cycle and day period data <c>Intl.DateTimeFormat</c> reads, decoded
/// from the composition's tables (JSD-0045).
/// </summary>
/// <remarks>
/// <para>
/// <b>The pattern generators are made once per language and hour character</b> and shared: a
/// generator holds the language's patterns in ICU's matching structures and never changes after it
/// is made, so every format of the same language reads the same one. They are made under a lock,
/// because the tables are shared by the runtimes of one descriptor.
/// </para>
/// </remarks>
internal sealed class JsDateData
{
    /// <summary>Each language's data, by dotted key.</summary>
    internal Dictionary<string, Dictionary<string, string>> Locales { get; } = new(StringComparer.Ordinal);

    /// <summary>The hour cycles a region allows and prefers.</summary>
    internal Dictionary<string, (string Allowed, string Preferred)> TimeData { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's day period rules.</summary>
    internal Dictionary<string, List<JsDayPeriodRule>> DayPeriods { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// The pseudo-locale of the ISO 8601 calendar's patterns, and the suffix of each language's data
    /// in that calendar (JSD-0055).
    /// </summary>
    internal const string Iso8601 = "@iso8601";

    /// <summary>
    /// The calendars whose data is a layer of the date table (JSD-0057), each after the one it is a
    /// layer over; null for the layer of CLDR's generic patterns. <c>islamic</c> is a parent only.
    /// </summary>
    internal static readonly (string Calendar, string? Parent)[] CalendarLayers =
    [
        ("buddhist", null), ("chinese", null), ("coptic", null), ("dangi", "chinese"), ("ethiopic", null), ("ethioaa", "ethiopic"),
        ("hebrew", null), ("indian", null), ("islamic", null), ("islamic-civil", "islamic"), ("islamic-tbla", "islamic"),
        ("islamic-umalqura", "islamic"), ("japanese", null), ("persian", null), ("roc", null),
    ];

    /// <summary>The language a calendar's data language is of: <c>en</c> for <c>en@hebrew</c>.</summary>
    internal static string LanguageOf(string dataLanguage)
    {
        var at = dataLanguage.IndexOf('@', StringComparison.Ordinal);
        return at < 0 ? dataLanguage : dataLanguage[..at];
    }

    /// <summary>Whether a key is one of a calendar's patterns rather than one of its names.</summary>
    internal static bool IsPatternKey(string key) =>
        key.StartsWith("dateFormats.", StringComparison.Ordinal) ||
        key.StartsWith("timeFormats.", StringComparison.Ordinal) ||
        key.StartsWith("dateTime.", StringComparison.Ordinal) ||
        key.StartsWith("atTime.", StringComparison.Ordinal) ||
        key.StartsWith("available.", StringComparison.Ordinal) ||
        key.StartsWith("append.", StringComparison.Ordinal) ||
        key.StartsWith("interval.", StringComparison.Ordinal);

    /// <summary>A language's value for a key, or nothing.</summary>
    internal string? Value(string language, string key) =>
        Locales.TryGetValue(language, out var values) && values.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// The hour character a region prefers - <c>h</c>, <c>H</c>, <c>K</c> or <c>k</c> - from CLDR's
    /// time data, the world's where the region has none.
    /// </summary>
    internal char PreferredHourChar(string region)
    {
        if (!TimeData.TryGetValue(region, out var data) && !TimeData.TryGetValue("001", out data))
        {
            return 'H';
        }

        return data.Preferred.Length != 0 ? data.Preferred[0] : 'H';
    }
}

/// <summary>A day period rule: the period, and the minute of the day it is at or the minutes it is from and before.</summary>
internal readonly record struct JsDayPeriodRule(string Period, int From, int Before, bool At);
