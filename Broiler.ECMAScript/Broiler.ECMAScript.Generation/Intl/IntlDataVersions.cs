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

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// The versions of the data the Intl tables are generated from: CLDR <b>48.2.0</b>, its collation
/// files' UCA <b>17.0.0</b>, and the Unicode Character Database <b>17.0.0</b>, which the UCA version is
/// tied to (Broiler.VM's decision JSD-0082, decisions D10 and D12; the area design's exit clause 2).
/// </summary>
/// <remarks>
/// A re-pin to a later release is a dated data decision that changes these constants, the pins and the
/// generated tables in the same change, in both repositories.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D10 D12 data versions; IP=Low; Security=Low; Resources=0; Fingerprint=54962F
// Broiler-Human:        PENDING
public static class IntlDataVersions
{
    /// <summary>The CLDR release, as the pin's <c>version</c> line and each <c>package.json</c> state it.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D10 data versions; IP=Low; Security=Low; Resources=0; Fingerprint=C5C107
    // Broiler-Human:        PENDING
    public const string CldrVersion = "48.2.0";

    /// <summary>The CLDR major version, as each supplemental file's <c>_cldrVersion</c> states it.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D10 data versions; IP=Low; Security=Low; Resources=0; Fingerprint=5292A9
    // Broiler-Human:        PENDING
    public const string CldrMajor = "48";

    /// <summary>The UCA version the collation files state in their <c># UCA Version:</c> line, which is the Unicode version.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 data versions; IP=Low; Security=Low; Resources=0; Fingerprint=4D7D93
    // Broiler-Human:        PENDING
    public const string UcaVersion = UnicodeVersion;

    /// <summary>The Unicode Character Database version the generator reads General_Category, decompositions and Unified_Ideograph from.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 data versions; IP=Low; Security=Low; Resources=0; Fingerprint=DD1BD0
    // Broiler-Human:        PENDING
    public const string UnicodeVersion = "17.0.0";
}
