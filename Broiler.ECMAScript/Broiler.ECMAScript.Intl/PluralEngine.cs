// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           8
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  2/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// A language's plural rules of one type, parsed once: ECMA-402's PluralRuleSelect (<c>sec-pluralruleselect</c>) over a decimal
/// String and its compact exponent, PluralRuleSelectRange (<c>sec-pluralruleselectrange</c>) over CLDR's plural ranges, and the
/// categories <c>resolvedOptions().pluralCategories</c> lists (Broiler.VM's JSD-0082, sub-area (b), rows P1 to P8).
/// </summary>
/// <remarks>
/// <para>
/// <b>UTS #35 Part 3's plural rules</b>, section 5.1, over the operands of section 5.1.1: <c>n</c>, <c>i</c>, <c>v</c>,
/// <c>w</c>, <c>f</c>, <c>t</c>, and the compact decimal exponent <c>c</c> with its synonym <c>e</c> (decision D20). Each
/// operand is held as its last fifteen digits and whether more lie above them: a rule takes a value modulo a small number or
/// compares it with a small range, so a huge integer part needs only its low digits and a flag that it exceeds every range.
/// </para>
/// <para>
/// <b>The rules are the locale data's</b>, as CLDR's rule text (<see cref="ILocaleData.PluralRules"/>), and a range's category
/// is the data's plural range for the two categories, or <c>other</c> where it names none, for both types, as ICU resolves an
/// ordinal range by the cardinal data (row P5b, an adoption of Broiler.VM's algorithm).
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsPluralRule</c>, <c>JsPluralOperands</c>, <c>JsNumberData.Plural</c> and
/// <c>PluralRange</c>, and <c>JsRealm.PluralCategoriesOf</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with
/// <see cref="NumberFormatInfo.InvariantInfo"/> where it parsed with the invariant culture (decision D19), and the caller's
/// <see cref="WorkCharge"/> on <see cref="Select"/>, the one member whose work grows with what it is given.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=9B5FB0
// Broiler-Human:        PENDING
public sealed class PluralEngine
{
    /// <summary>The plural categories in the order <c>resolvedOptions</c> lists them (<c>sec-intl.pluralrules.prototype.resolvedoptions</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=56DA8F
    // Broiler-Human:        PENDING
    private static string[] CategoryOrder() => ["zero", "one", "two", "few", "many", "other"];

    private readonly ILocaleData data;

    private readonly List<(string Category, PluralRule Rule)> rules;

    private PluralEngine(ILocaleData data, string language, PluralRuleType type, List<(string Category, PluralRule Rule)> rules, IReadOnlyList<string> categories)
    {
        this.data = data;
        Language = language;
        Type = type;
        this.rules = rules;
        Categories = categories;
    }

    /// <summary>The language whose rules select, as the tables key it.</summary>
    public string Language { get; }

    /// <summary>Cardinal or ordinal.</summary>
    public PluralRuleType Type { get; }

    /// <summary>
    /// Every category PluralRuleSelect can answer for the language and type, <c>other</c> always among them, in the order
    /// <c>zero</c>, <c>one</c>, <c>two</c>, <c>few</c>, <c>many</c>, <c>other</c> (row P6, the <c>pluralCategories</c> property).
    /// </summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>
    /// The engine of a language's rules of one type, each rule parsed once; a language the data holds no rules for selects
    /// <c>other</c> alone.
    /// </summary>
    /// <param name="data">The locale data.</param>
    /// <param name="language">The language as the tables key it: Broiler.VM reads the resolved locale's language subtag.</param>
    /// <param name="type">Cardinal or ordinal.</param>
    /// <returns>The engine.</returns>
    /// <exception cref="InvalidDataException">A rule's text is not UTS #35's rule syntax: the table is not the generator's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=1; Fingerprint=0F0C32
    // Broiler-Human:        PENDING
    public static PluralEngine Create(ILocaleData data, string language, PluralRuleType type)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(language);

        if (type is not (PluralRuleType.Cardinal or PluralRuleType.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        var parsed = new List<(string, PluralRule)>();
        var present = new HashSet<string>(StringComparer.Ordinal) { "other" };

        foreach (var rule in data.PluralRules(language, type) ?? [])
        {
            parsed.Add((rule.Category, PluralRule.Parse(rule.Rule)));
            present.Add(rule.Category);
        }

        var categories = new List<string>();

        foreach (var category in CategoryOrder())
        {
            if (present.Contains(category))
            {
                categories.Add(category);
            }
        }

        return new PluralEngine(data, language, type, parsed, categories.AsReadOnly());
    }

    /// <summary>
    /// PluralRuleSelect (<c>sec-pluralruleselect</c>, an implementation-defined abstract operation): the first category whose
    /// rule holds for the operands of <paramref name="formatted"/> and <paramref name="compactExponent"/>, or <c>other</c>.
    /// </summary>
    /// <param name="formatted">A decimal String, as FormatNumericToString writes one: ASCII digits with an optional <c>.</c>, no sign.</param>
    /// <param name="compactExponent">The compact decimal exponent, the operands <c>c</c> and <c>e</c> (decision D20); 0 outside compact notation.</param>
    /// <param name="charge">The caller's meter: one unit per code unit of <paramref name="formatted"/>, before the operands are read.</param>
    /// <returns><c>zero</c>, <c>one</c>, <c>two</c>, <c>few</c>, <c>many</c> or <c>other</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="formatted"/> is not a decimal String.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=High; Resources=2; Fingerprint=F39F1C
    // Broiler-Falsified-If: a decimal String's category differs from the first CLDR rule that holds for its UTS 35 operands, or c and e read other than the exponent given
    // Broiler-Human:        PENDING
    public string Select(string formatted, int compactExponent = 0, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(formatted);
        charge?.Invoke((ulong)formatted.Length);

        if (!IsDecimalString(formatted))
        {
            throw new ArgumentException("PluralRuleSelect takes a decimal String", nameof(formatted));
        }

        var operands = PluralOperands.Of(formatted, compactExponent);

        foreach (var (category, rule) in rules)
        {
            if (category != "other" && rule.Holds(operands))
            {
                return category;
            }
        }

        return "other";
    }

    /// <summary>
    /// PluralRuleSelectRange (<c>sec-pluralruleselectrange</c>, an implementation-defined abstract operation): the category of a
    /// range from a <paramref name="start"/> category to an <paramref name="end"/> one, by the language's plural ranges, or
    /// <c>other</c> where they name none (row P5b).
    /// </summary>
    /// <param name="start">The start's category.</param>
    /// <param name="end">The end's category.</param>
    /// <returns>The range's category.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=1; Fingerprint=AFA08B
    // Broiler-Human:        PENDING
    public string SelectRange(string start, string end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        return data.PluralRange(Language, start, end) ?? "other";
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=1; Fingerprint=32B429
    // Broiler-Human:        PENDING
    private static bool IsDecimalString(string text)
    {
        var point = false;

        if (text.Length == 0)
        {
            return false;
        }

        foreach (var c in text)
        {
            if (c == '.' && !point)
            {
                point = true;
            }
            else if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// The operands of a formatted number that a plural rule reads (UTS #35 Part 3 section 5.1.1): the integer digits, the visible
/// fraction digits with and without trailing zeros, and the compact exponent.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=808D65
// Broiler-Human:        PENDING
internal readonly record struct PluralOperands(long Integer, bool IntegerHuge, string Fraction, int CompactExponent)
{
    /// <summary>The operands of a formatted decimal string: ASCII digits with an optional <c>.</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Medium; Resources=2; Fingerprint=BE03E3
    // Broiler-Human:        PENDING
    internal static PluralOperands Of(string formatted, int compactExponent)
    {
        var point = formatted.IndexOf('.', StringComparison.Ordinal);
        var integer = point < 0 ? formatted : formatted[..point];
        var fraction = point < 0 ? string.Empty : formatted[(point + 1)..];

        var trimmed = integer.TrimStart('0');
        var huge = trimmed.Length > 15;
        var low = trimmed.Length == 0 ? 0 : long.Parse(huge ? trimmed[^15..] : trimmed, NumberStyles.None, NumberFormatInfo.InvariantInfo);

        return new PluralOperands(low, huge, fraction, compactExponent);
    }

    /// <summary>The number of visible fraction digits, <c>v</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=112422
    // Broiler-Human:        PENDING
    internal int V => Fraction.Length;

    /// <summary>The visible fraction digits without trailing zeros.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=0AF713
    // Broiler-Human:        PENDING
    internal string T => Fraction.TrimEnd('0');
}

/// <summary>One CLDR plural rule: an <c>or</c> of <c>and</c>s of relations (UTS #35 Part 3 section 5.1).</summary>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=C1315B
// Broiler-Human:        PENDING
internal sealed class PluralRule
{
    private readonly List<List<Relation>> alternatives;

    private PluralRule(List<List<Relation>> alternatives) =>
        this.alternatives = alternatives;

    /// <summary>Reads a rule's condition; the empty condition holds always.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=High; Resources=2; Fingerprint=CC8B9B
    // Broiler-Falsified-If: a CLDR rule text is read as another condition than UTS 35 section 5.1 states
    // Broiler-Human:        PENDING
    internal static PluralRule Parse(string condition)
    {
        var alternatives = new List<List<Relation>>();

        if (condition.Trim().Length == 0)
        {
            return new PluralRule(alternatives);
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

        return new PluralRule(alternatives);
    }

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Medium; Resources=2; Fingerprint=B64CC9
    // Broiler-Human:        PENDING
    private static Relation ParseRelation(string text)
    {
        var words = text.Replace("!=", " \u0001 ", StringComparison.Ordinal).Replace("=", " = ", StringComparison.Ordinal)
            .Replace("\u0001", "!=", StringComparison.Ordinal).Replace("%", " % ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // EVERY WORD IS READ THROUGH Word, which refuses a relation that ends early with the documented
        // InvalidDataException rather than an index out of range.
        var at = 0;
        var name = Word(words, at++, text);

        if (name.Length != 1 || !"nivwftce".Contains(name[0], StringComparison.Ordinal))
        {
            throw new InvalidDataException("a plural rule's relation is not UTS #35's syntax: " + text);
        }

        var operand = name[0];

        long modulus = 0;

        if (Word(words, at, text) is "%" or "mod")
        {
            modulus = Integer(Word(words, at + 1, text), text);
            at += 2;
        }

        var negated = false;

        switch (Word(words, at, text))
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

                if (Word(words, at, text) == "not")
                {
                    negated = true;
                    at++;
                }

                break;
            default:
                if (Word(words, at, text) == "not")
                {
                    negated = true;
                    at++;
                }

                if (Word(words, at, text) is not ("in" or "within"))
                {
                    throw new InvalidDataException("a plural rule's relation is not UTS #35's syntax: " + text);
                }

                at++;
                break;
        }

        // THE RANGE LIST: at least one word, so that an empty list is refused by Integer below.
        _ = Word(words, at, text);

        var ranges = new List<(long, long)>();

        foreach (var item in string.Concat(words[at..]).Split(','))
        {
            var dots = item.IndexOf("..", StringComparison.Ordinal);
            ranges.Add(dots < 0
                ? (Integer(item, text), Integer(item, text))
                : (Integer(item[..dots], text), Integer(item[(dots + 2)..], text)));
        }

        return new Relation(operand, modulus, negated, [.. ranges]);
    }

    /// <summary>The relation's word at <paramref name="at"/>, or the documented refusal when the relation ends before it.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=1; Fingerprint=DFBBE6
    // Broiler-Human:        PENDING
    private static string Word(string[] words, int at, string relation) =>
        at < words.Length
            ? words[at]
            : throw new InvalidDataException("a plural rule's relation is not UTS #35's syntax: " + relation);

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=1; Fingerprint=B52DC3
    // Broiler-Human:        PENDING
    private static long Integer(string digits, string relation) =>
        long.TryParse(digits, NumberStyles.None, NumberFormatInfo.InvariantInfo, out var value)
            ? value
            : throw new InvalidDataException("a plural rule's relation is not UTS #35's syntax: " + relation);

    /// <summary>Whether the rule holds for a formatted number's operands.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Medium; Resources=2; Fingerprint=2E378E
    // Broiler-Human:        PENDING
    internal bool Holds(PluralOperands operands)
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

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Medium; Resources=2; Fingerprint=4231A1
    // Broiler-Human:        PENDING
    private static bool Holds(Relation relation, PluralOperands operands)
    {
        // THE OPERAND'S VALUE: an integer (its low digits, and whether more lie above them), or for n with a visible nonzero
        // fraction, a value no integer range contains.
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
                value = digits.Length == 0 ? 0 : long.Parse(huge ? digits[^15..] : digits, NumberStyles.None, NumberFormatInfo.InvariantInfo);
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

    /// <summary>One relation: an operand, an optional modulus, whether it is negated, and its ranges.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-pluralruleselect sec-pluralruleselectrange, UTS35 Part 3 section 5 via Broiler.VM JsPluralRule; IP=Low; Security=Low; Resources=0; Fingerprint=8D05DC
    // Broiler-Human:        PENDING
    private sealed record Relation(char Operand, long Modulus, bool Negated, (long Low, long High)[] Ranges);
}
