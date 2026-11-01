// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsNumberData.cs
//     blob a0373603e82f719389f0386e089384f5a6a48708: the whole file: JsNumberData, JsCurrencyNames and the pattern types beside them
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The number, currency, unit and plural data the composition handed over, decoded once and shared
/// by every runtime its descriptor makes (JSD-0044).
/// </summary>
/// <remarks>
/// <para>
/// <b>Keyed by language</b>: the tables carry <c>de</c> and <c>en</c>, and every supported locale
/// reads its language's data, as CLDR's inheritance gives <c>de-DE</c> and <c>en-US</c> nothing of
/// their own for numbers.
/// </para>
/// </remarks>
internal sealed class JsNumberData
{
    /// <summary>Each language's flattened number data, by dotted key.</summary>
    internal Dictionary<string, Dictionary<string, string>> Locales { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's currency names, by <c>language|code</c>.</summary>
    internal Dictionary<string, JsCurrencyNames> Currencies { get; } = new(StringComparer.Ordinal);

    /// <summary>The currencies whose fraction digits are not 2.</summary>
    internal Dictionary<string, int> CurrencyDigits { get; } = new(StringComparer.Ordinal);

    /// <summary>The numbering systems with a simple digit mapping, each digit a string of one code point.</summary>
    internal Dictionary<string, string[]> NumberingSystems { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's cardinal plural rules, in the table's order.</summary>
    internal Dictionary<string, List<(string Category, JsPluralRule Rule)>> Plurals { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's ordinal plural rules, in the table's order.</summary>
    internal Dictionary<string, List<(string Category, JsPluralRule Rule)>> Ordinals { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's plural range results, by <c>language|start|end</c>.</summary>
    internal Dictionary<string, string> PluralRanges { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's unit patterns, by <c>language|width|unit|field</c>.</summary>
    internal Dictionary<string, string> Units { get; } = new(StringComparer.Ordinal);

    /// <summary>One value of a language's number data, or nothing.</summary>
    internal string? Value(string language, string key) =>
        Locales.TryGetValue(language, out var locale) && locale.TryGetValue(key, out var value) ? value : null;

    /// <summary>The plural category of a formatted number in a language: the first rule that holds, or <c>other</c>.</summary>
    internal string Plural(string language, JsPluralOperands operands, bool ordinal = false)
    {
        if ((ordinal ? Ordinals : Plurals).TryGetValue(language, out var rules))
        {
            foreach (var (category, rule) in rules)
            {
                if (category != "other" && rule.Holds(operands))
                {
                    return category;
                }
            }
        }

        return "other";
    }

    /// <summary>The plural category of a range, by the language's range rules, or <c>other</c> where they name none, as ICU resolves one.</summary>
    internal string PluralRange(string language, string start, string end) =>
        PluralRanges.TryGetValue(language + "|" + start + "|" + end, out var result) ? result : "other";
}

/// <summary>A currency's names in one language; an empty field is one CLDR does not give.</summary>
internal sealed record JsCurrencyNames(string Symbol, string Narrow, string One, string Other, string Name, string SymbolEnds, string NarrowEnds);

/// <summary>One token of a pattern's prefix or suffix.</summary>
internal enum JsAffixKind
{
    /// <summary>Text, as written.</summary>
    Literal,

    /// <summary>The currency, U+00A4 CURRENCY SIGN.</summary>
    Currency,

    /// <summary>The percent sign, <c>%</c>.</summary>
    Percent,

    /// <summary>The per-mille sign.</summary>
    PerMille,

    /// <summary>The sign's place, <c>-</c>.</summary>
    Minus,

    /// <summary>A plus sign, <c>+</c>.</summary>
    Plus,
}

/// <summary>One affix token.</summary>
internal readonly record struct JsAffixToken(JsAffixKind Kind, string Text);

/// <summary>
/// A CLDR number pattern, as UTS #35 Part 3 section 3 reads one: a positive subpattern and an
/// optional negative one, each a prefix, a number and a suffix.
/// </summary>
/// <remarks>
/// <para>
/// <b>Only what a formatter reads from it is kept</b>: the affixes, the grouping sizes and, for a
/// compact pattern, how many digits it shows. The digit counts of the number part are ECMA-402's
/// options and not the pattern's.
/// </para>
/// </remarks>
internal sealed class JsNumberPattern
{
    private JsNumberPattern() { }

    internal List<JsAffixToken> PositivePrefix { get; private set; } = [];

    internal List<JsAffixToken> PositiveSuffix { get; private set; } = [];

    internal List<JsAffixToken>? NegativePrefix { get; private set; }

    internal List<JsAffixToken>? NegativeSuffix { get; private set; }

    /// <summary>The primary grouping size, or -1 where the pattern does not group.</summary>
    internal int Grouping1 { get; private set; } = -1;

    /// <summary>The secondary grouping size.</summary>
    internal int Grouping2 { get; private set; } = -1;

    /// <summary>How many <c>0</c> digits the number part shows, which a compact pattern's exponent is read from.</summary>
    internal int Zeros { get; private set; }

    /// <summary>Whether the positive subpattern writes a plus sign itself.</summary>
    internal bool PositiveHasPlus => PositivePrefix.Exists(static t => t.Kind == JsAffixKind.Plus) || PositiveSuffix.Exists(static t => t.Kind == JsAffixKind.Plus);

    /// <summary>Whether the negative subpattern has a sign's place.</summary>
    internal bool NegativeHasMinus =>
        NegativePrefix is not null &&
        (NegativePrefix.Exists(static t => t.Kind == JsAffixKind.Minus) || NegativeSuffix!.Exists(static t => t.Kind == JsAffixKind.Minus));

    /// <summary>Reads a pattern; a malformed one reads as its literal text around no number.</summary>
    internal static JsNumberPattern Parse(string pattern)
    {
        var result = new JsNumberPattern();

        var (Positive, Negative) = SplitSubpatterns(pattern);
        var (prefix, suffix, number) = ParseSubpattern(Positive);

        result.PositivePrefix = prefix;
        result.PositiveSuffix = suffix;
        result.ReadNumber(number);

        if (Negative is { } negative)
        {
            var (negativePrefix, negativeSuffix, _) = ParseSubpattern(negative);
            result.NegativePrefix = negativePrefix;
            result.NegativeSuffix = negativeSuffix;
        }

        return result;
    }

    /// <summary>The positive and negative subpatterns, split at the first unquoted <c>;</c>.</summary>
    private static (string Positive, string? Negative) SplitSubpatterns(string pattern)
    {
        var quoted = false;

        for (var at = 0; at < pattern.Length; at++)
        {
            if (pattern[at] == '\'')
            {
                quoted = !quoted;
            }
            else if (pattern[at] == ';' && !quoted)
            {
                return (pattern[..at], pattern[(at + 1)..]);
            }
        }

        return (pattern, null);
    }

    /// <summary>One subpattern's prefix tokens, suffix tokens and number part.</summary>
    private static (List<JsAffixToken> Prefix, List<JsAffixToken> Suffix, string Number) ParseSubpattern(string text)
    {
        var prefix = new List<JsAffixToken>();
        var suffix = new List<JsAffixToken>();
        var number = new StringBuilder();
        var stage = 0;
        var literal = new StringBuilder();

        void Flush(List<JsAffixToken> into)
        {
            if (literal.Length != 0)
            {
                into.Add(new JsAffixToken(JsAffixKind.Literal, literal.ToString()));
                literal.Clear();
            }
        }

        for (var at = 0; at < text.Length; at++)
        {
            var c = text[at];
            var into = stage == 0 ? prefix : suffix;

            if (c == '\'')
            {
                var close = text.IndexOf('\'', at + 1);

                if (close < 0)
                {
                    close = text.Length;
                }

                literal.Append(close == at + 1 ? "'" : text[(at + 1)..close]);
                at = close;
                continue;
            }

            if (stage != 2 && (c is '#' or '0' or '@' or ',' or '.' || char.IsAsciiDigit(c) || (stage == 1 && c is 'E' or '+')))
            {
                if (stage == 0)
                {
                    Flush(prefix);
                    stage = 1;
                }

                number.Append(c);
                continue;
            }

            if (stage == 1)
            {
                stage = 2;
                into = suffix;
            }

            switch (c)
            {
                case '\u00a4':
                    Flush(into);
                    var count = 1;

                    while (at + 1 < text.Length && text[at + 1] == '\u00a4')
                    {
                        count++;
                        at++;
                    }

                    into.Add(new JsAffixToken(JsAffixKind.Currency, new string('\u00a4', count)));
                    break;
                case '%':
                    Flush(into);
                    into.Add(new JsAffixToken(JsAffixKind.Percent, "%"));
                    break;
                case '\u2030':
                    Flush(into);
                    into.Add(new JsAffixToken(JsAffixKind.PerMille, "\u2030"));
                    break;
                case '-':
                    Flush(into);
                    into.Add(new JsAffixToken(JsAffixKind.Minus, "-"));
                    break;
                case '+':
                    Flush(into);
                    into.Add(new JsAffixToken(JsAffixKind.Plus, "+"));
                    break;
                default:
                    literal.Append(c);
                    break;
            }
        }

        Flush(stage == 0 ? prefix : suffix);
        return (prefix, suffix, number.ToString());
    }

    /// <summary>Reads the grouping sizes and the count of shown zeros from the number part.</summary>
    private void ReadNumber(string number)
    {
        var integerEnd = number.IndexOfAny(['.', 'E']);
        var integer = integerEnd < 0 ? number : number[..integerEnd];

        foreach (var c in number)
        {
            if (c == '0')
            {
                Zeros++;
            }
        }

        var last = integer.LastIndexOf(',');

        if (last < 0)
        {
            return;
        }

        Grouping1 = integer.Length - last - 1;
        var previous = integer.LastIndexOf(',', last - 1);
        Grouping2 = previous < 0 ? Grouping1 : last - previous - 1;
    }
}
