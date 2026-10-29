// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsPluralRule.cs
//     blob 5ba6931e402f6bca873448b019f36905033fd61c: the whole file: JsPluralOperands and JsPluralRule, which JsIntlTables' ReadNumbers parses each rule with
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System.Collections.Generic;
using System.Globalization;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The operands of a formatted number that a plural rule reads (UTS #35 Part 3 section 5.1.1): the
/// absolute value, the integer digits, the visible fraction digits with and without trailing zeros,
/// and the compact exponent.
/// </summary>
/// <remarks>
/// <b>Each operand is held as its last digits and whether it is an integer</b>: a rule takes a value
/// modulo a small number or compares it with a small range, so a huge integer part needs only its
/// low digits and a flag that it exceeds every range.
/// </remarks>
internal readonly record struct JsPluralOperands(long Integer, bool IntegerHuge, string Fraction, int CompactExponent)
{
    /// <summary>The operands of a formatted decimal string: ASCII digits with an optional <c>.</c>.</summary>
    internal static JsPluralOperands Of(string formatted, int compactExponent)
    {
        var point = formatted.IndexOf('.');
        var integer = point < 0 ? formatted : formatted[..point];
        var fraction = point < 0 ? string.Empty : formatted[(point + 1)..];

        var trimmed = integer.TrimStart('0');
        var huge = trimmed.Length > 15;
        var low = trimmed.Length == 0 ? 0 : long.Parse(huge ? trimmed[^15..] : trimmed, CultureInfo.InvariantCulture);

        return new JsPluralOperands(low, huge, fraction, compactExponent);
    }

    /// <summary>The number of visible fraction digits, <c>v</c>.</summary>
    internal int V => Fraction.Length;

    /// <summary>The visible fraction digits without trailing zeros.</summary>
    internal string T => Fraction.TrimEnd('0');
}

/// <summary>
/// One CLDR plural rule: an <c>or</c> of <c>and</c>s of relations (UTS #35 Part 3 section 5.1).
/// </summary>
internal sealed class JsPluralRule
{
    /// <summary>One relation: an operand, an optional modulus, whether it is negated, and its ranges.</summary>
    private sealed record Relation(char Operand, long Modulus, bool Negated, (long Low, long High)[] Ranges);

    private readonly List<List<Relation>> alternatives;

    private JsPluralRule(List<List<Relation>> alternatives) =>
        this.alternatives = alternatives;

    /// <summary>Reads a rule's condition; the empty condition holds always.</summary>
    internal static JsPluralRule Parse(string condition)
    {
        var alternatives = new List<List<Relation>>();

        if (condition.Trim().Length == 0)
        {
            return new JsPluralRule(alternatives);
        }

        foreach (var alternative in condition.Split(" or "))
        {
            var relations = new List<Relation>();

            foreach (var text in alternative.Split(" and "))
            {
                relations.Add(ParseRelation(text.Trim()));
            }

            alternatives.Add(relations);
        }

        return new JsPluralRule(alternatives);
    }

    private static Relation ParseRelation(string text)
    {
        var words = text.Replace("!=", " \u0001 ").Replace("=", " = ").Replace("\u0001", "!=").Replace("%", " % ")
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        var at = 0;
        var operand = words[at++][0];
        long modulus = 0;

        if (words[at] is "%" or "mod")
        {
            modulus = long.Parse(words[at + 1], CultureInfo.InvariantCulture);
            at += 2;
        }

        var negated = false;

        switch (words[at])
        {
            case "!=":
                negated = true;
                at++;
                break;
            case "=":
                at++;
                break;
            case "is":
                at++;

                if (words[at] == "not")
                {
                    negated = true;
                    at++;
                }

                break;
            default:
                if (words[at] == "not")
                {
                    negated = true;
                    at++;
                }

                at++;
                break;
        }

        var ranges = new List<(long, long)>();

        foreach (var item in string.Concat(words[at..]).Split(','))
        {
            var dots = item.IndexOf("..", System.StringComparison.Ordinal);
            ranges.Add(dots < 0
                ? (long.Parse(item, CultureInfo.InvariantCulture), long.Parse(item, CultureInfo.InvariantCulture))
                : (long.Parse(item[..dots], CultureInfo.InvariantCulture), long.Parse(item[(dots + 2)..], CultureInfo.InvariantCulture)));
        }

        return new Relation(operand, modulus, negated, [.. ranges]);
    }

    /// <summary>Whether the rule holds for a formatted number's operands.</summary>
    internal bool Holds(JsPluralOperands operands)
    {
        if (alternatives.Count == 0)
        {
            return true;
        }

        foreach (var relations in alternatives)
        {
            if (relations.TrueForAll(relation => Holds(relation, operands)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Holds(Relation relation, JsPluralOperands operands)
    {
        // THE OPERAND'S VALUE: an integer (its low digits, and whether more lie above them), or for
        // n with a visible nonzero fraction, a value no integer range contains.
        long value;
        bool integral;
        bool huge;

        switch (relation.Operand)
        {
            case 'n':
                value = operands.Integer;
                integral = operands.T.Length == 0;
                huge = operands.IntegerHuge;
                break;
            case 'i':
                value = operands.Integer;
                integral = true;
                huge = operands.IntegerHuge;
                break;
            case 'v':
                value = operands.V;
                integral = true;
                huge = false;
                break;
            case 'w':
                value = operands.T.Length;
                integral = true;
                huge = false;
                break;
            case 'f':
            case 't':
                var digits = (relation.Operand == 'f' ? operands.Fraction : operands.T).TrimStart('0');
                huge = digits.Length > 15;
                value = digits.Length == 0 ? 0 : long.Parse(huge ? digits[^15..] : digits, CultureInfo.InvariantCulture);
                integral = true;
                break;
            case 'c':
            case 'e':
                value = operands.CompactExponent;
                integral = true;
                huge = false;
                break;
            default:
                return false;
        }

        if (relation.Modulus != 0)
        {
            value %= relation.Modulus;
            huge = false;
        }

        var inRange = false;

        if (integral && !huge)
        {
            foreach (var (low, high) in relation.Ranges)
            {
                if (low <= value && value <= high)
                {
                    inRange = true;
                    break;
                }
            }
        }

        return inRange != relation.Negated;
    }
}
