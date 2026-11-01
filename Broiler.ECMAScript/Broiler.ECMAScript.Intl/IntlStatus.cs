// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           8
// Human-reviewed:   0/1
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// How an Intl operation of this library ended. An expected failure is a status, never an exception (convention 3;
/// Broiler.VM's JSD-0082, X3: "an ill-formed tag, an unknown calendar or numbering system, a value out of range: each
/// a status, which each engine maps to its own <c>RangeError</c> or <c>TypeError</c> at the specification's moment").
/// </summary>
/// <remarks>
/// <para>
/// <b>A status names the step that failed, not a message</b>: each engine renders its own message for it, so a status is
/// as fine as the messages the engines keep, and changing an engine's text changes nothing here. Every status below is
/// a <c>RangeError</c> in ECMA-402; the <c>TypeError</c>s of the area are the engines' own, where they read script values
/// (an element of a locale list that is neither a String nor an Object, an options value that is not an Object).
/// </para>
/// <para>
/// <b>The numbers are stable.</b> Each stage of milestone SJL-9.3 appends the statuses its sub-area's operations answer,
/// and no number is reused.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 X3 statuses; IP=Low; Security=Low; Resources=0; Fingerprint=16C4DB
// Broiler-Human:        PENDING
public enum IntlStatus
{
    /// <summary>The operation completed, and its result is valid.</summary>
    Completed = 0,

    /// <summary>
    /// The tag is not a well-formed language tag (ECMA-402 IsWellFormedLanguageTag, <c>sec-iswellformedlanguagetag</c>):
    /// CanonicalizeLocaleList and the <c>Intl.Locale</c> constructor throw a <c>RangeError</c>.
    /// </summary>
    InvalidLanguageTag = 1,

    /// <summary>The <c>language</c> option is not a <c>unicode_language_subtag</c> (UpdateLanguageId, <c>sec-updatelanguageid</c>): a <c>RangeError</c>.</summary>
    InvalidLanguageSubtag = 2,

    /// <summary>The <c>script</c> option is not a <c>unicode_script_subtag</c> (UpdateLanguageId): a <c>RangeError</c>.</summary>
    InvalidScriptSubtag = 3,

    /// <summary>The <c>region</c> option is not a <c>unicode_region_subtag</c> (UpdateLanguageId): a <c>RangeError</c>.</summary>
    InvalidRegionSubtag = 4,

    /// <summary>The <c>variants</c> option is empty or not a sequence of <c>unicode_variant_subtag</c>s without a repeat (UpdateLanguageId): a <c>RangeError</c>.</summary>
    InvalidVariants = 5,

    /// <summary>
    /// A keyword's value from an option - a calendar, a collation, a numbering system, a first day of the week - is not
    /// the <c>type</c> nonterminal of UTS #35 (the constructors' "is not well-formed" steps): a <c>RangeError</c>.
    /// </summary>
    InvalidType = 6,

    /// <summary>The key is none of <c>Intl.supportedValuesOf</c>'s (<c>sec-intl.supportedvaluesof</c>): a <c>RangeError</c>.</summary>
    InvalidKey = 7,
}
