using System.Collections.Generic;
using Broiler.Regex.Unicode;

namespace Broiler.Regex.Ast;

/// <summary>
/// The model of a character class (<c>[...]</c>) or a standalone class escape
/// (<c>\d</c>, <c>\w</c>, …). Built up by the parser as a union of code-point
/// ranges plus built-in class escapes, optionally negated as a whole
/// (ECMA-262 §22.2.1 CharacterClass / ClassRanges).
/// </summary>
public sealed class CharSet
{
    private readonly List<(int Lo, int Hi)> _ranges = new();
    private readonly List<ClassEscape> _escapes = new();

    /// <summary>True for a negated class <c>[^…]</c>.</summary>
    public bool Negated { get; set; }

    /// <summary>
    /// Set when the class uses a <c>v</c>-mode set operation (<c>&amp;&amp;</c>,
    /// <c>--</c>) or a <c>\q{…}</c> string disjunction. Currently parsed but not
    /// evaluated — see the README's "Known limitations".
    /// </summary>
    public bool UsesSetOperations { get; set; }

    public IReadOnlyList<(int Lo, int Hi)> Ranges => _ranges;
    public IReadOnlyList<ClassEscape> Escapes => _escapes;

    public void AddCodePoint(int codePoint) => _ranges.Add((codePoint, codePoint));

    public void AddRange(int lo, int hi)
    {
        if (lo > hi)
            throw new RegexSyntaxException($"Range out of order in character class: U+{lo:X}-U+{hi:X}");
        _ranges.Add((lo, hi));
    }

    public void AddEscape(ClassEscape escape) => _escapes.Add(escape);

    /// <summary>
    /// Tests whether <paramref name="codePoint"/> is a member of this class.
    /// Case folding is applied by the caller's <paramref name="canonicalize"/>
    /// (so the same set logic serves both case-sensitive and <c>i</c> matching).
    /// </summary>
    public bool Contains(int codePoint, bool ignoreCase, bool unicode)
    {
        var member = ContainsRaw(codePoint);

        // Under `i`, a class also matches any code point that case-folds to a
        // member (and vice-versa). Check the canonical form too.
        if (!member && ignoreCase)
        {
            var folded = CaseFolding.Canonicalize(codePoint, unicode);
            if (folded != codePoint)
                member = ContainsRaw(folded);

            if (!member)
            {
                // Reverse direction: does any simple-fold sibling of codePoint fall
                // inside the class? Covered by the small sibling table.
                foreach (var sibling in CaseFolding.SimpleSiblings(codePoint, unicode))
                {
                    if (ContainsRaw(sibling)) { member = true; break; }
                }
            }
        }

        return Negated ? !member : member;
    }

    private bool ContainsRaw(int codePoint)
    {
        foreach (var (lo, hi) in _ranges)
        {
            if (codePoint >= lo && codePoint <= hi)
                return true;
        }

        foreach (var escape in _escapes)
        {
            if (UnicodeCharSets.MatchesEscape(escape, codePoint))
                return true;
        }

        return false;
    }
}

/// <summary>A built-in character-class escape usable inside or outside a class.</summary>
public enum ClassEscape
{
    /// <summary><c>\d</c></summary>
    Digit,
    /// <summary><c>\D</c></summary>
    NonDigit,
    /// <summary><c>\w</c></summary>
    Word,
    /// <summary><c>\W</c></summary>
    NonWord,
    /// <summary><c>\s</c></summary>
    Space,
    /// <summary><c>\S</c></summary>
    NonSpace,
}
