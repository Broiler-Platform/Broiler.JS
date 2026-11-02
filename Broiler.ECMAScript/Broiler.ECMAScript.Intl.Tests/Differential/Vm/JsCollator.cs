// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsCollator.cs
//     blob 3004ac685ed23241b0e412a559f7c0685be1cc49: the whole file: JsCaseFirst and JsCollator, the comparison milestone SJL-17.3 ports as UcaCollator
//   The same blob is the file at Broiler.VM's SJL-17 rule commit 4e6a41e9d3860b8ca79d5d3ea6de215c1592b0c0, the commit
//   milestone SJL-17.3's differential check names (Broiler.VM's JSD-0084).
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>How a collator orders case at the tertiary level and on its case level.</summary>
internal enum JsCaseFirst
{
    /// <summary>The root's order: lower case first, by tertiary weight.</summary>
    Off,

    /// <summary>Upper case first.</summary>
    Upper,

    /// <summary>Lower case first, stated.</summary>
    Lower,
}

/// <summary>
/// One collator's resolved settings and its comparison: the strings' collation elements compared
/// level by level, as UTS #10 orders them and ECMA-402's options choose (JSD-0043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitivity is a strength and a case level</b>: <c>base</c> compares primaries only,
/// <c>accent</c> adds secondaries, <c>case</c> compares primaries and then case, and <c>variant</c>
/// adds tertiaries. <c>ignorePunctuation</c> is UCA's shifted alternate: variable elements and the
/// ignorables after them weigh nothing at the levels compared.
/// </para>
/// <para>
/// <b>A string is normalized to NFD before its elements are looked up</b>, with the profile's own
/// normalization, so canonically equivalent strings compare equal, and comparing charges the work as
/// <c>normalize</c> does.
/// </para>
/// </remarks>
internal sealed class JsCollator
{
    private readonly JsCollationData data;

    private readonly JsCollationData.Tailoring? tailoring;

    internal JsCollator(
        JsCollationData data, JsCollationData.Tailoring? tailoring, int strength, bool caseLevel, JsCaseFirst caseFirst, bool shifted, bool numeric)
    {
        this.data = data;
        this.tailoring = tailoring;
        Strength = strength;
        CaseLevel = caseLevel;
        CaseFirst = caseFirst;
        Shifted = shifted;
        Numeric = numeric;
    }

    /// <summary>1 for primaries only, 2 with secondaries, 3 with tertiaries.</summary>
    internal int Strength { get; }

    /// <summary>Whether case is compared after the primaries.</summary>
    internal bool CaseLevel { get; }

    /// <summary>The case order.</summary>
    internal JsCaseFirst CaseFirst { get; }

    /// <summary>Whether variable elements are shifted out of the compared levels.</summary>
    internal bool Shifted { get; }

    /// <summary>Whether runs of digits compare by value.</summary>
    internal bool Numeric { get; }

    /// <summary>ECMA-402's <c>CompareStrings</c>: -1, 0 or 1.</summary>
    internal int Compare(JsEngine engine, string x, string y)
    {
        if (string.Equals(x, y, StringComparison.Ordinal))
        {
            return 0;
        }

        var left = Elements(engine, x);
        var right = Elements(engine, y);
        engine.Charge((ulong)(left.Count + right.Count));

        return Compare(left, right);
    }

    /// <summary>A string's collation elements, after NFD and, when shifted, with variable elements removed.</summary>
    private List<ulong> Elements(JsEngine engine, string text)
    {
        var nfd = JsRealm.NormalizeText(engine, text, compose: false, compatibility: false);
        var codePoints = new List<int>(nfd.Length);

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
        data.Elements([.. codePoints], tailoring, Numeric, elements);

        if (!Shifted)
        {
            return elements;
        }

        // SHIFTED (UTS #10 s4.3): A VARIABLE ELEMENT, AND EVERY IGNORABLE ELEMENT AFTER IT UNTIL A
        // NON-VARIABLE PRIMARY, WEIGHS NOTHING AT THE THREE LEVELS COMPARED.
        var kept = new List<ulong>(elements.Count);
        var afterVariable = false;

        foreach (var element in elements)
        {
            if (JsCollationData.Variable(element))
            {
                afterVariable = true;
                continue;
            }

            if (JsCollationData.Primary(element) == 0 && afterVariable)
            {
                continue;
            }

            if (JsCollationData.Primary(element) != 0)
            {
                afterVariable = false;
            }

            kept.Add(element);
        }

        return kept;
    }

    /// <summary>The level-by-level comparison of two element sequences.</summary>
    private int Compare(List<ulong> left, List<ulong> right)
    {
        var primary = Level(left, right, static element => JsCollationData.Primary(element), static _ => true);

        if (primary != 0)
        {
            return primary;
        }

        if (CaseLevel)
        {
            var caseOrder = Level(left, right, CaseWeight, static element => JsCollationData.Primary(element) != 0);

            if (caseOrder != 0)
            {
                return caseOrder;
            }
        }

        if (Strength >= 2)
        {
            var secondary = Level(left, right, static element => JsCollationData.Secondary(element), static _ => true);

            if (secondary != 0)
            {
                return secondary;
            }
        }

        if (Strength >= 3)
        {
            return Level(left, right, TertiaryWeight, static _ => true);
        }

        return 0;
    }

    /// <summary>One level: the non-zero weights of the counted elements, compared in order, the shorter sequence first.</summary>
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
    /// The rank of an element's case in the order asked for: upper, mixed, lower when upper case goes
    /// first, and lower, mixed, upper otherwise.
    /// </summary>
    private ulong CaseRank(ulong element)
    {
        var stated = JsCollationData.Case(element);
        return (ulong)(CaseFirst == JsCaseFirst.Upper ? 3 - stated : 1 + stated);
    }

    /// <summary>The case level's weight: the element's case rank.</summary>
    private ulong CaseWeight(ulong element) => CaseRank(element);

    /// <summary>The tertiary weight, with case ordered first when a case order is asked for.</summary>
    private ulong TertiaryWeight(ulong element)
    {
        var tertiary = JsCollationData.Tertiary(element);

        if (tertiary == 0 || CaseFirst == JsCaseFirst.Off)
        {
            return tertiary;
        }

        return (CaseRank(element) << 13) | tertiary;
    }
}
