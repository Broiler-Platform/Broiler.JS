// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           6
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Intl;

/// <summary>The levels a collator compares (UTS #10, section 3.1).</summary>
// Broiler-AI:           Origin=Ported; Spec=UTS10 s3.1, ECMA-402 sec-intl-collator-internal-slots via Broiler.VM JsCollator; IP=Low; Security=Low; Resources=0; Fingerprint=F60EFD
// Broiler-Human:        PENDING
public enum CollationStrength
{
    /// <summary>Primaries only: <c>sensitivity</c> <c>base</c>, and <c>case</c> with the case level.</summary>
    Primary = 1,

    /// <summary>Primaries and secondaries: <c>sensitivity</c> <c>accent</c>.</summary>
    Secondary = 2,

    /// <summary>Primaries, secondaries and tertiaries: <c>sensitivity</c> <c>variant</c>.</summary>
    Tertiary = 3,
}

/// <summary>How a collator orders case at the tertiary level and on its case level (Broiler.VM's <c>JsCaseFirst</c>).</summary>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl-collator-internal-slots, UTS35 kf via Broiler.VM JsCaseFirst; IP=Low; Security=Low; Resources=0; Fingerprint=941D0D
// Broiler-Human:        PENDING
public enum CollationCaseFirst
{
    /// <summary>The root's order: lower case first, by tertiary weight (<c>caseFirst</c> <c>false</c>).</summary>
    Off = 0,

    /// <summary>Upper case first (<c>caseFirst</c> <c>upper</c>).</summary>
    Upper = 1,

    /// <summary>Lower case first, stated (<c>caseFirst</c> <c>lower</c>).</summary>
    Lower = 2,
}

/// <summary>
/// The resolved options a <see cref="UcaCollator"/> compares under, as a record (Broiler.VM's JSD-0084, X3): the strength, the
/// case level, the case order, the shifted alternate, numeric collation, and the tailoring by name. The engine reads the
/// options in the specification's order and resolves the locale; this record is what it hands the library.
/// </summary>
/// <param name="Strength">The levels compared.</param>
/// <param name="CaseLevel">Whether case is compared after the primaries.</param>
/// <param name="CaseFirst">The case order.</param>
/// <param name="Shifted">Whether variable elements are shifted out of the compared levels: <c>ignorePunctuation</c>.</param>
/// <param name="Numeric">Whether runs of decimal digits compare by value: <c>numeric</c>.</param>
/// <param name="Tailoring">
/// The tailoring by name, as <see cref="UcaCollationData.TailoringFor"/> answers it, or <see langword="null"/> for the root.
/// A name the data does not carry collates by the root, as Broiler.VM's <c>TailoringNamed</c> answered nothing for it.
/// </param>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl-collator-internal-slots via Broiler.VM JsCollator; IP=Low; Security=Low; Resources=0; Fingerprint=0D27AF
// Broiler-Human:        PENDING
public sealed record UcaCollatorOptions(
    CollationStrength Strength, bool CaseLevel, CollationCaseFirst CaseFirst, bool Shifted, bool Numeric, string? Tailoring)
{
    /// <summary>
    /// The record ECMA-402's resolved <c>sensitivity</c>, <c>ignorePunctuation</c>, <c>caseFirst</c> and <c>numeric</c> select, as
    /// Broiler.VM's <c>NewCollator</c> maps them onto UCA settings: <c>base</c> and <c>case</c> compare primaries, <c>case</c> with
    /// the case level, <c>accent</c> adds secondaries, and anything else (<c>variant</c>) tertiaries; <c>upper</c> and
    /// <c>lower</c> state the case order, and anything else (<c>false</c>) leaves it off; <c>ignorePunctuation</c> is the shifted
    /// alternate.
    /// </summary>
    /// <param name="sensitivity">The resolved <c>[[Sensitivity]]</c>.</param>
    /// <param name="ignorePunctuation">The resolved <c>[[IgnorePunctuation]]</c>.</param>
    /// <param name="caseFirst">The resolved <c>[[CaseFirst]]</c>.</param>
    /// <param name="numeric">The resolved <c>[[Numeric]]</c>.</param>
    /// <param name="tailoring">The tailoring by name, or <see langword="null"/> for the root.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sensitivity"/> or <paramref name="caseFirst"/> is null.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.collator via Broiler.VM JsRealm.NewCollator; IP=Low; Security=Low; Resources=0; Fingerprint=EBCC0C
    // Broiler-Human:        PENDING
    public static UcaCollatorOptions FromResolved(string sensitivity, bool ignorePunctuation, string caseFirst, bool numeric, string? tailoring)
    {
        ArgumentNullException.ThrowIfNull(sensitivity);
        ArgumentNullException.ThrowIfNull(caseFirst);

        return new UcaCollatorOptions(
            sensitivity switch
            {
                "base" or "case" => CollationStrength.Primary,
                "accent" => CollationStrength.Secondary,
                _ => CollationStrength.Tertiary,
            },
            CaseLevel: string.Equals(sensitivity, "case", StringComparison.Ordinal),
            caseFirst switch
            {
                "upper" => CollationCaseFirst.Upper,
                "lower" => CollationCaseFirst.Lower,
                _ => CollationCaseFirst.Off,
            },
            Shifted: ignorePunctuation,
            Numeric: numeric,
            Tailoring: tailoring);
    }
}
