// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.DateTimeFormat.cs
//     blob bff1811cd34a91f50a9840a2bfc4de81bfb31161: JsRealm's Intl.DateTimeFormat part, of which only Intl.supportedValuesOf and the calendar lists are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: every member of JsRealm but `FormatterCalendars`, `CalendarKeyValues` and `SupportedValuesOf`, each the date-time formatter's of sub-area (d), and the types `JsDateTimeFormatObject` and `JsTemporalDateFormats`, and the `using` lines only they needed.

using System;
using System.Collections.Generic;

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
    /// <summary>
    /// The calendars the formatter writes, sorted (the Intl era and month code proposal's
    /// AvailableCalendars, JSD-0057): every calendar type of its Table 1 but the aliases.
    /// </summary>
    internal static readonly string[] FormatterCalendars =
    [
        "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "gregory", "hebrew", "indian",
        "islamic-civil", "islamic-tbla", "islamic-umalqura", "iso8601", "japanese", "persian", "roc",
    ];

    /// <summary>
    /// The locale data's <c>ca</c> values: the Gregorian calendar first, as every supported locale's
    /// default, then the others, and the deprecated Hijri types the constructor falls back from.
    /// </summary>
    private static readonly string?[] CalendarKeyValues =
    [
        "gregory", "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "hebrew", "indian", "islamic",
        "islamic-civil", "islamic-rgsa", "islamic-tbla", "islamic-umalqura", "iso8601", "japanese", "persian", "roc",
    ];

    /// <summary>ECMA-402's <c>Intl.supportedValuesOf</c>: the values this data supports for a key, sorted.</summary>
    private static JsValue SupportedValuesOf(JsEngine engine, JsValue key)
    {
        var name = engine.ToStringValue(key);
        var tables = engine.Intl!;
        var values = new List<string>();

        switch (name)
        {
            case "calendar":
                values.AddRange(FormatterCalendars);
                break;
            case "collation":
                values.Add("phonebk");
                break;
            case "currency":
                foreach (var entry in tables.Numbers.Currencies.Keys)
                {
                    var code = entry[(entry.IndexOf('|') + 1)..];

                    if (code.Length == 3 && !values.Contains(code))
                    {
                        values.Add(code);
                    }
                }

                break;
            case "numberingSystem":
                values.AddRange(tables.Numbers.NumberingSystems.Keys);
                break;
            case "timeZone":
                // ECMA-402 6.5.3: every primary identifier the time zone data names (JSD-0053).
                values.AddRange(tables.TimeZones.PrimaryIdentifiers);
                break;
            case "unit":
                values.AddRange(SanctionedUnits);
                break;
            default:
                throw engine.Error("RangeError", "Intl.supportedValuesOf: invalid key: " + name);
        }

        values.Sort(StringComparer.Ordinal);
        return JsValue.Object(engine.Realm.NewArray(values.ConvertAll(static value => JsValue.String(value))));
    }
}
