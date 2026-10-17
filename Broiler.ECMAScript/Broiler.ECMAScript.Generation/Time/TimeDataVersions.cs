// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           0
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>
/// The versions of the data the time zone and calendar tables are generated from (convention 13: data
/// versions are public): the release decision SJL-D-20 pins for both engines and the calendar sources
/// decision SJL-D-21 names, as Broiler.VM's JSD-0076 records them at milestone SJL-8.1.
/// </summary>
/// <remarks>
/// Each pin reader refuses a pin, an archive or a stated version that is not the one named here, so a
/// table written by this package is always from these versions. A later release moves a constant here,
/// the pin beside the archive in <c>Broiler.ECMAScript/data/time/</c> and Broiler.VM's pin in one dated
/// decision, never one of them alone.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-20 SJL-D-21; IP=Low; Security=Low; Resources=0; Fingerprint=A015C7
// Broiler-Human:        PENDING
public static class TimeDataVersions
{
    /// <summary>The IANA Time Zone Database release, as its <c>version</c> member states it (SJL-D-20).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-20; IP=Low; Security=Low; Resources=0; Fingerprint=01F8F0
    // Broiler-Human:        PENDING
    public const string TzdbRelease = "2026e";

    /// <summary>The CLDR release whose <c>bcp47/timezone.json</c> gives the primary identifiers (ECMA-402 6.5's note).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-20; IP=Low; Security=Low; Resources=0; Fingerprint=C5C107
    // Broiler-Human:        PENDING
    public const string CldrVersion = "48.2.0";

    /// <summary>The version of ICU4X's <c>icu_calendar</c> crate the Chinese, Korean, Qing-era and Umm al-Qura years are read from (SJL-D-21).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-21; IP=Low; Security=Low; Resources=0; Fingerprint=02AC58
    // Broiler-Human:        PENDING
    public const string IcuCalendarVersion = "2.3.0";

    /// <summary>The version of the <c>calendrical_calculations</c> crate the Persian calendar's corrections are read from (SJL-D-21).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-21; IP=Low; Security=Low; Resources=0; Fingerprint=E20077
    // Broiler-Human:        PENDING
    public const string CalendricalCalculationsVersion = "0.2.4";
}
