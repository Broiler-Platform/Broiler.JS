// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           5
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  4/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// One collator: ECMA-402's CompareStrings (<c>sec-collator-comparestrings</c>) over the Unicode Collation Algorithm, CLDR's
/// root collation and a tailoring, under its resolved options (Broiler.VM's JSD-0084; milestone SJL-17.3): the strings'
/// collation elements compared level by level, as UTS #10 orders them and the options choose.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitivity is a strength and a case level</b>: <c>base</c> compares primaries only, <c>accent</c> adds secondaries,
/// <c>case</c> compares primaries and then case, and <c>variant</c> adds tertiaries. <c>ignorePunctuation</c> is UCA's shifted
/// alternate: variable elements and the ignorables after them weigh nothing at the levels compared. A tailoring that states a
/// <c>[reorder]</c> moves its script's primaries before the other scripts' (Thai's), before the levels are compared.
/// </para>
/// <para>
/// <b>A string is normalized to NFD before its elements are looked up</b>, by the normalization owner's
/// <see cref="UnicodeNormalization"/> (JSD-0084, decision E6), so canonically equivalent strings compare equal.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsCollator</c></b> at its SJL-17 rule commit <c>4e6a41e9</c>, with three changes (JSD-0082's
/// WP5, JSD-0084's X3): no culture is read (none was); the string-length ceiling NFD may not pass is a status,
/// <see cref="IntlStatus.NormalizedStringTooLong"/>, where Broiler.VM threw its <c>RangeError</c>; and the caller's
/// <see cref="WorkCharge"/> is charged where Broiler.VM charged its engine's meter, in its units (decision E7, row O17): NFD as
/// <c>normalize</c> charges it, through the normalization owner's meter, and each collation element of both strings - the
/// sort keys, after every contraction and expansion - once both are looked up. Broiler.JS passes none.
/// </para>
/// <para>
/// <i>(Note of 2026-11-02, at the review of SJL-17.3.)</i> The element charge is an exception to <see cref="WorkCharge"/>'s
/// contract as written, "Charges work to the caller's budget, before the work it charges for is done": it follows the lookup
/// of both strings' elements, at Broiler.VM's point (<c>JsCollator.cs:107-109</c> at <c>4e6a41e9</c>), under JSD-0084's
/// decision E7 and row O17, which name that point. The work done before it is the lookup over the NFD forms, within a constant
/// factor of their length, and NFD's own charge has paid for that length before it.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=UTS10, ECMA-402 sec-collator-comparestrings via Broiler.VM JsCollator; IP=Low; Security=Medium; Resources=3; Fingerprint=B6048B
// Broiler-Human:        PENDING
public sealed class UcaCollator
{
    private readonly UcaCollationData.Tables data;

    private readonly UcaCollationData.Tailoring? tailoring;

    private readonly UnicodeNormalization normalization;

    /// <summary>A collator over <paramref name="data"/> under <paramref name="options"/>.</summary>
    /// <param name="data">The collation data, decoded from the engine's tables.</param>
    /// <param name="options">The resolved options.</param>
    /// <param name="normalization">The normalization owner's normalizer, over the engine's Unicode tables.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="System.IO.InvalidDataException">The collation tables are not in the format <see cref="UcaCollationData"/> states.</exception>
    // Broiler-AI:           Origin=Ported; Spec=UTS10 via Broiler.VM JsCollator; IP=Low; Security=Low; Resources=1; Fingerprint=114440
    // Broiler-Human:        PENDING
    public UcaCollator(UcaCollationData data, UcaCollatorOptions options, UnicodeNormalization normalization)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(normalization);

        this.data = data.Decoded;
        tailoring = options.Tailoring is null ? null : this.data.TailoringNamed(options.Tailoring);
        this.normalization = normalization;
        Options = options;
    }

    /// <summary>The resolved options the collator compares under.</summary>
    public UcaCollatorOptions Options { get; }

    /// <summary>
    /// ECMA-402's CompareStrings: -1, 0 or 1 in <paramref name="order"/>. Equal strings compare 0 without any work.
    /// </summary>
    /// <param name="x">The first string, as code units; a lone surrogate is a code point of its own.</param>
    /// <param name="y">The second string.</param>
    /// <param name="maxLength">The longest NFD form, in UTF-16 code units, the caller accepts: the engine's string-length ceiling.</param>
    /// <param name="charge">The caller's meter, or <see langword="null"/>: NFD as <c>normalize</c> charges it, then one unit per collation element of both strings.</param>
    /// <param name="order">-1, 0 or 1 when the status is <see cref="IntlStatus.Completed"/>; 0 otherwise.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.NormalizedStringTooLong"/> when a string's NFD form is longer than <paramref name="maxLength"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> or <paramref name="y"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is negative.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-collator-comparestrings via Broiler.VM JsCollator.Compare; IP=Low; Security=Medium; Resources=4; Fingerprint=6BFA71
    // Broiler-Human:        PENDING
    public IntlStatus Compare(string x, string y, int maxLength, WorkCharge? charge, out int order)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        order = 0;

        if (string.Equals(x, y, StringComparison.Ordinal))
        {
            return IntlStatus.Completed;
        }

        if (Elements(x, maxLength, charge) is not { } left || Elements(y, maxLength, charge) is not { } right)
        {
            return IntlStatus.NormalizedStringTooLong;
        }

        charge?.Invoke((ulong)(left.Count + right.Count));
        order = Compare(left, right);
        return IntlStatus.Completed;
    }

    /// <summary>
    /// A string's collation elements, after NFD and, when shifted, with variable elements removed; or <see langword="null"/>
    /// when its NFD form is longer than <paramref name="maxLength"/>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS10 s4.3 via Broiler.VM JsCollator.Elements; IP=Low; Security=Medium; Resources=3; Fingerprint=F57FF8
    // Broiler-Human:        PENDING
    private List<ulong>? Elements(string text, int maxLength, WorkCharge? charge)
    {
        if (normalization.Transform(text, NormalForm.Nfd, maxLength, charge is null ? null : new UnicodeWorkCharge(charge), out var nfd) != UnicodeStatus.Success)
        {
            return null;
        }

        var codePoints = new List<int>(nfd!.Length);

        for (var at = 0; at < nfd.Length; at++)
        {
            if (char.IsHighSurrogate(nfd[at]) && at + 1 < nfd.Length && char.IsLowSurrogate(nfd[at + 1]))
            {
                codePoints.Add(char.ConvertToUtf32(nfd[at], nfd[at + 1]));
                at++;
                continue;
            }

            codePoints.Add(nfd[at]);
        }

        var elements = new List<ulong>(codePoints.Count + 4);
        data.Elements([.. codePoints], tailoring, Options.Numeric, normalization, elements);

        // A TAILORING'S [reorder] (milestone SJL-17.3): its script's primaries move to where the scripts begin, and the
        // primaries between there and the script's move after them, so that each keeps its order among its own.
        if (tailoring?.Reorder is var (first, last, before))
        {
            for (var index = 0; index < elements.Count; index++)
            {
                var primary = UcaCollationData.Tables.Primary(elements[index]);

                if (primary >= first && primary <= last)
                {
                    primary = before + (primary - first);
                }
                else if (primary >= before && primary < first)
                {
                    primary += last + 1 - first;
                }
                else
                {
                    continue;
                }

                elements[index] = (primary << 40) | (elements[index] & ((1UL << 40) - 1));
            }
        }

        if (!Options.Shifted)
        {
            return elements;
        }

        // SHIFTED (UTS #10 s4.3): A VARIABLE ELEMENT, AND EVERY IGNORABLE ELEMENT AFTER IT UNTIL A NON-VARIABLE PRIMARY,
        // WEIGHS NOTHING AT THE THREE LEVELS COMPARED.
        var kept = new List<ulong>(elements.Count);
        var afterVariable = false;

        foreach (var element in elements)
        {
            if (UcaCollationData.Tables.Variable(element))
            {
                afterVariable = true;
                continue;
            }

            if (UcaCollationData.Tables.Primary(element) == 0 && afterVariable)
            {
                continue;
            }

            if (UcaCollationData.Tables.Primary(element) != 0)
            {
                afterVariable = false;
            }

            kept.Add(element);
        }

        return kept;
    }

    /// <summary>The level-by-level comparison of two element sequences.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS10 s7.4 via Broiler.VM JsCollator.Compare; IP=Low; Security=Low; Resources=3; Fingerprint=F55A1A
    // Broiler-Human:        PENDING
    private int Compare(List<ulong> left, List<ulong> right)
    {
        var primary = Level(left, right, static element => UcaCollationData.Tables.Primary(element), static _ => true);

        if (primary != 0)
        {
            return primary;
        }

        if (Options.CaseLevel)
        {
            var caseOrder = Level(left, right, CaseWeight, static element => UcaCollationData.Tables.Primary(element) != 0);

            if (caseOrder != 0)
            {
                return caseOrder;
            }
        }

        if (Options.Strength >= CollationStrength.Secondary)
        {
            var secondary = Level(left, right, static element => UcaCollationData.Tables.Secondary(element), static _ => true);

            if (secondary != 0)
            {
                return secondary;
            }
        }

        if (Options.Strength >= CollationStrength.Tertiary)
        {
            return Level(left, right, TertiaryWeight, static _ => true);
        }

        return 0;
    }

    /// <summary>One level: the non-zero weights of the counted elements, compared in order, the shorter sequence first.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS10 s7.4 via Broiler.VM JsCollator.Level; IP=Low; Security=Low; Resources=2; Fingerprint=E01EA6
    // Broiler-Human:        PENDING
    private static int Level(List<ulong> left, List<ulong> right, Func<ulong, ulong> weight, Func<ulong, bool> counted)
    {
        var i = 0;
        var j = 0;

        while (true)
        {
            var a = Next(left, ref i, weight, counted);
            var b = Next(right, ref j, weight, counted);

            if (a != b)
            {
                return a < b ? -1 : 1;
            }

            if (a == 0)
            {
                return 0;
            }
        }
    }

    /// <summary>The next non-zero weight of a counted element, or zero at the end.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS10 s7.4 via Broiler.VM JsCollator.Next; IP=Low; Security=Low; Resources=1; Fingerprint=C05994
    // Broiler-Human:        PENDING
    private static ulong Next(List<ulong> elements, ref int at, Func<ulong, ulong> weight, Func<ulong, bool> counted)
    {
        while (at < elements.Count)
        {
            var element = elements[at++];

            if (!counted(element))
            {
                continue;
            }

            var value = weight(element);

            if (value != 0)
            {
                return value;
            }
        }

        return 0;
    }

    /// <summary>
    /// The rank of an element's case in the order asked for: upper, mixed, lower when upper case goes first, and lower, mixed,
    /// upper otherwise.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 kf via Broiler.VM JsCollator.CaseRank; IP=Low; Security=Low; Resources=1; Fingerprint=7C8A23
    // Broiler-Human:        PENDING
    private ulong CaseRank(ulong element)
    {
        var stated = UcaCollationData.Tables.Case(element);
        return (ulong)(Options.CaseFirst == CollationCaseFirst.Upper ? 3 - stated : 1 + stated);
    }

    /// <summary>The case level's weight: the element's case rank.</summary>
    private ulong CaseWeight(ulong element) => CaseRank(element);

    /// <summary>The tertiary weight, with case ordered first when a case order is asked for.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 kf via Broiler.VM JsCollator.TertiaryWeight; IP=Low; Security=Low; Resources=1; Fingerprint=A3A1A1
    // Broiler-Human:        PENDING
    private ulong TertiaryWeight(ulong element)
    {
        var tertiary = UcaCollationData.Tables.Tertiary(element);

        if (tertiary == 0 || Options.CaseFirst == CollationCaseFirst.Off)
        {
            return tertiary;
        }

        return (CaseRank(element) << 13) | tertiary;
    }
}
