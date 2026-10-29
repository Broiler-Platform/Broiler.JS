// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           2
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// <c>Intl.supportedValuesOf</c> (ECMA-402 <c>sec-intl.supportedvaluesof</c>) after the engine has converted its key to a
/// String, and the lists it reads (Broiler.VM's JSD-0082, sub-area (a), rows L39 to L41).
/// </summary>
/// <remarks>
/// <b>Ported from Broiler.VM's <c>SupportedValuesOf</c></b> (<c>JsRealm.DateTimeFormat</c>) at its SJL-9 rule commit
/// <c>4cf9ca44</c>, with its calendar list (<c>FormatterCalendars</c>, row L41: one list for the library to hold) and the
/// sanctioned units of <c>JsRealm.NumberFormat</c>. The collations are the engine's <c>Intl.Collator</c>'s (row L40, engine
/// policy, milestone SJL-17's), the currencies and numbering systems the locale data's, and the time zones the shared time
/// zone database's primary identifiers.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.supportedvaluesof via Broiler.VM JsRealm.DateTimeFormat SupportedValuesOf; IP=Low; Security=Low; Resources=0; Fingerprint=2D6949
// Broiler-Human:        PENDING
public static class SupportedValues
{
    /// <summary>
    /// The calendars the formatter writes, sorted (the Intl era and month code proposal's AvailableCalendars, Broiler.VM's
    /// JSD-0057): every calendar type of its Table 1 but the aliases, <c>islamic</c> and <c>islamic-rgsa</c>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.supportedvaluesof via Broiler.VM JsRealm.DateTimeFormat SupportedValuesOf; IP=Low; Security=Low; Resources=0; Fingerprint=CE7907
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> AvailableCalendars => Calendars;

    /// <summary>The sanctioned single units of ECMA-402's Table 2 (<c>sec-issanctionedsingleunitidentifier</c>), sorted.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.supportedvaluesof via Broiler.VM JsRealm.DateTimeFormat SupportedValuesOf; IP=Low; Security=Low; Resources=0; Fingerprint=890E98
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> SanctionedUnits => Units;

    private static ImmutableArray<string> Calendars { get; } =
    [
        "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "gregory", "hebrew", "indian",
        "islamic-civil", "islamic-tbla", "islamic-umalqura", "iso8601", "japanese", "persian", "roc",
    ];

    private static ImmutableArray<string> Units { get; } =
    [
        "acre", "bit", "byte", "celsius", "centimeter", "day", "degree", "fahrenheit", "fluid-ounce", "foot",
        "gallon", "gigabit", "gigabyte", "gram", "hectare", "hour", "inch", "kilobit", "kilobyte", "kilogram",
        "kilometer", "liter", "megabit", "megabyte", "meter", "microsecond", "mile", "mile-scandinavian",
        "milliliter", "millimeter", "millisecond", "minute", "month", "nanosecond", "ounce", "percent", "petabyte",
        "pound", "second", "stone", "terabit", "terabyte", "week", "yard", "year",
    ];

    /// <summary>
    /// The values the implementation supports for a key, sorted by code unit and each once (<c>sec-intl.supportedvaluesof</c>,
    /// steps 2 to 9): <c>calendar</c>, <c>collation</c>, <c>currency</c>, <c>numberingSystem</c>, <c>timeZone</c> or <c>unit</c>.
    /// </summary>
    /// <param name="key">The key, as the engine's ToString gave it.</param>
    /// <param name="data">The locale data, whose currencies and numbering systems are listed.</param>
    /// <param name="timeZones">The time zone database, whose primary identifiers are listed.</param>
    /// <param name="collator">The engine's <c>Intl.Collator</c> data, whose collations are listed.</param>
    /// <param name="values">The values, or an empty list for a key it refuses.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidKey"/>, a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.supportedvaluesof via Broiler.VM JsRealm.DateTimeFormat SupportedValuesOf; IP=Low; Security=Low; Resources=1; Fingerprint=3B64A0
    // Broiler-Human:        PENDING
    public static IntlStatus Of(string key, ILocaleData data, ITimeZoneDatabase timeZones, ICollatorLocaleData collator, out IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(timeZones);
        ArgumentNullException.ThrowIfNull(collator);

        var list = new List<string>();

        switch (key)
        {
            case "calendar":
                list.AddRange(Calendars);
                break;
            case "collation":
                list.AddRange(collator.Collations);
                break;
            case "currency":
                foreach (var code in data.CurrencyCodes)
                {
                    if (code.Length == 3 && !list.Contains(code))
                    {
                        list.Add(code);
                    }
                }

                break;
            case "numberingSystem":
                list.AddRange(data.NumberingSystems);
                break;
            case "timeZone":
                // ECMA-402 6.5.3: every primary identifier the time zone data names (Broiler.VM's JSD-0053).
                list.AddRange(timeZones.PrimaryIdentifiers);
                break;
            case "unit":
                list.AddRange(Units);
                break;
            default:
                values = [];
                return IntlStatus.InvalidKey;
        }

        list.Sort(StringComparer.Ordinal);
        values = list.AsReadOnly();
        return IntlStatus.Completed;
    }
}
