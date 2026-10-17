#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.JavaScript.Runtime.Unicode;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// The early errors of a pattern's group names (ECMA-262 22.2.1.1): a GroupSpecifier's name must be a
/// RegExpIdentifierName, two GroupSpecifiers of one alternative may not share a name, and, where the pattern
/// is parsed with its group names (under <c>u</c> or <c>v</c>, or when it declares one), every <c>\k</c> is a
/// complete <c>\k&lt;GroupName&gt;</c>.
/// </summary>
/// <remarks>
/// Both the regular-expression literal's early check in the parser and the RegExp constructor call it, so the
/// two refuse the same names. A name's code points are classified by <see cref="JSUnicode"/>, which is
/// Broiler.Unicode's <c>EcmaScriptLexical</c> over the pinned tables, as an identifier's are.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: a pattern with a group name that is not a RegExpIdentifierName, a name declared twice in one alternative, or an incomplete \k where group names are parsed, is admitted, or a pattern without any of them is refused
// Broiler-Human:        PENDING
internal static class RegExpGroupNames
{
    /// <summary>
    /// The early error <paramref name="pattern"/>'s group names make, as a message, or <see langword="null"/> when
    /// they make none.
    /// </summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="unicodeMode">Whether the <c>u</c> or the <c>v</c> flag is present.</param>
    /// <param name="unicodeSets">Whether the <c>v</c> flag is present, under which classes nest.</param>
    internal static string? FirstError(string pattern, bool unicodeMode, bool unicodeSets)
    {
        if (FirstDuplicate(pattern) is { } duplicate)
            return $"Duplicate capture group name '{duplicate}'";

        if (FirstInvalid(pattern) is { } invalid)
            return $"invalid capture group name \"{invalid}\"";

        if (HasIncompleteReference(pattern, unicodeMode, unicodeSets))
            return "invalid named reference";

        return null;
    }

    /// <summary>
    /// Walks the pattern invoking <paramref name="onGroup"/>(index, name) for each capturing group in source
    /// order; the name is null for an unnamed group, and empty for a GroupSpecifier with no closing <c>&gt;</c>.
    /// </summary>
    internal static void Scan(string pattern, Action<int, string?> onGroup)
    {
        var inClass = false;
        var index = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++;
                continue;
            }
            if (c == '[')
            {
                inClass = true;
                continue;
            }
            if (c == ']')
            {
                inClass = false;
                continue;
            }
            if (c != '(' || inClass)
                continue;

            if (i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                var nameEnd = pattern.IndexOf('>', i + 3);
                var name = nameEnd > i + 3 ? pattern.Substring(i + 3, nameEnd - (i + 3)) : string.Empty;
                onGroup(++index, name);
                if (nameEnd > i + 3)
                    i = nameEnd;
                continue;
            }

            if (i + 1 < pattern.Length && pattern[i + 1] == '?')
                continue;  // non-capturing (?:…) / lookaround / modifier

            onGroup(++index, null);
        }
    }

    /// <summary>The first GroupSpecifier's name, as written, that is not a RegExpIdentifierName, or null.</summary>
    internal static string? FirstInvalid(string pattern)
    {
        string? invalid = null;
        Scan(pattern, (_, name) =>
        {
            if (invalid == null && name != null && !IsValid(name))
                invalid = name;
        });
        return invalid;
    }

    /// <summary>
    /// The first name, as written, that two GroupSpecifiers reachable in one match share, or null.
    /// </summary>
    /// <remarks>
    /// ES2025 allows duplicate named capture groups only in SEPARATE alternatives of a disjunction (so at most
    /// one participates in any match). Walks the pattern with a stack of group-nesting frames; each frame tracks
    /// the names live in its CURRENT alternative branch and the union over all its alternatives (folded in at
    /// <c>|</c> and on close). Adding a name to a branch that already contains it, directly or bubbled up from a
    /// closed child group, is the error.
    /// </remarks>
    internal static string? FirstDuplicate(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf("(?<", StringComparison.Ordinal) < 0)
            return null;

        var stack = new Stack<(HashSet<string> Current, HashSet<string> All)>();
        stack.Push((new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal)));
        var inClass = false;

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '\\') { i++; continue; }
            if (inClass) { if (c == ']') inClass = false; continue; }
            if (c == '[') { inClass = true; continue; }

            if (c == '|')
            {
                var top = stack.Peek();
                top.All.UnionWith(top.Current);
                top.Current.Clear();
                continue;
            }

            if (c == ')')
            {
                if (stack.Count > 1)
                {
                    var closed = stack.Pop();
                    closed.All.UnionWith(closed.Current);
                    var parent = stack.Peek();
                    foreach (var n in closed.All)
                        if (!parent.Current.Add(n))
                            return n;
                }
                continue;
            }

            if (c != '(')
                continue;

            // A named capture (?<name>…), distinguished from lookbehind (?<= / (?<!.
            if (i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                var nameEnd = pattern.IndexOf('>', i + 3);
                if (nameEnd > i + 3)
                {
                    var name = pattern.Substring(i + 3, nameEnd - (i + 3));
                    if (!stack.Peek().Current.Add(name))
                        return name;
                    i = nameEnd;
                }
            }

            // Every grouping ( … ), capturing, non-capturing or lookaround, opens a nested alternation frame
            // whose names bubble up to the current alternative.
            stack.Push((new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal)));
        }

        return null;
    }

    /// <summary>
    /// Whether a <c>\k</c> of <paramref name="pattern"/> is not a complete <c>\k&lt;GroupName&gt;</c> where the
    /// pattern is parsed with its group names: under <c>u</c> or <c>v</c>, or when it declares one. Elsewhere
    /// (Annex B 22.2.1, no group name and no Unicode mode) a lone <c>\k</c> is the letter.
    /// </summary>
    /// <remarks>
    /// With group names a ClassEscape is not <c>k</c> either, so a <c>\k</c> inside a class is an error there too.
    /// Whether the referenced name is declared is not this check's: that early error is the engines'.
    /// </remarks>
    internal static bool HasIncompleteReference(string pattern, bool unicodeMode, bool unicodeSets)
    {
        if (pattern.IndexOf("\\k", StringComparison.Ordinal) < 0)
            return false;

        if (!unicodeMode)
        {
            var named = false;
            Scan(pattern, (_, name) => named |= name != null);
            if (!named)
                return false;
        }

        var depth = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '[') { if (depth == 0 || unicodeSets) depth++; continue; }
            if (c == ']') { if (depth > 0) depth--; continue; }
            if (c != '\\' || i + 1 >= pattern.Length)
                continue;

            if (pattern[++i] != 'k')
                continue;

            if (depth > 0 || i + 1 >= pattern.Length || pattern[i + 1] != '<')
                return true;

            var end = pattern.IndexOf('>', i + 2);
            if (end < 0 || !IsValid(pattern.Substring(i + 2, end - (i + 2))))
                return true;

            i = end;
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="name"/>, as written, is a RegExpIdentifierName: its first code point a
    /// RegExpIdentifierStart, the rest RegExpIdentifierParts.
    /// </summary>
    /// <remarks>
    /// A GroupName's RegExpIdentifierName always uses Unicode escape rules (<c>\u{…}</c> and <c>\u</c>-escaped
    /// surrogate pairs are accepted) regardless of the regex's u/v flag.
    /// </remarks>
    internal static bool IsValid(string name)
    {
        if (!TryDecode(name, unicode: true, out var codePoints) || codePoints.Count == 0)
            return false;
        // RegExpIdentifierStart and RegExpIdentifierPart are IdentifierStartChar and IdentifierPartChar
        // (ECMA-262 22.2.1), Broiler.Unicode's over the pinned Unicode 17.0.0 tables (SJL-7.5), so a group
        // name and an identifier admit the same code points.
        if (!JSUnicode.IsIdentifierStart(codePoints[0]))
            return false;
        for (var k = 1; k < codePoints.Count; k++)
            if (!JSUnicode.IsIdentifierPart(codePoints[k]))
                return false;
        return true;
    }

    /// <summary>
    /// Decodes a group-name's source text into Unicode code points: <c>\uXXXX</c> and (only in u/v mode)
    /// <c>\u{…}</c> escapes are resolved, and literal surrogate pairs are combined. Returns false for a malformed
    /// escape, a stray backslash, or a lone surrogate (none of which can appear in a name).
    /// </summary>
    internal static bool TryDecode(string name, bool unicode, out List<int> codePoints)
    {
        codePoints = new List<int>(name.Length);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '\\')
            {
                if (i + 1 >= name.Length || name[i + 1] != 'u') return false; // only \u escapes are legal
                i += 2;
                if (i < name.Length && name[i] == '{')
                {
                    if (!unicode) return false; // \u{…} is a u/v-mode-only escape
                    var close = name.IndexOf('}', i + 1);
                    if (close < 0 || close == i + 1) return false;
                    if (!int.TryParse(name.AsSpan(i + 1, close - (i + 1)), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp) || cp > 0x10FFFF)
                        return false;
                    codePoints.Add(cp);
                    i = close;
                }
                else
                {
                    if (i + 4 > name.Length ||
                        !int.TryParse(name.AsSpan(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var unit))
                        return false;
                    i += 3;
                    // In u/v mode a \u-escaped lead surrogate may pair with a following \u-escaped trail.
                    if (unicode && unit is >= 0xD800 and <= 0xDBFF
                        && i + 6 < name.Length && name[i + 1] == '\\' && name[i + 2] == 'u'
                        && int.TryParse(name.AsSpan(i + 3, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var trail)
                        && trail is >= 0xDC00 and <= 0xDFFF)
                    {
                        codePoints.Add(char.ConvertToUtf32((char)unit, (char)trail));
                        i += 6;
                    }
                    else if (unit is >= 0xD800 and <= 0xDFFF)
                    {
                        return false; // a lone surrogate is not a valid identifier code point
                    }
                    else codePoints.Add(unit);
                }
            }
            else if (char.IsHighSurrogate(c) && i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
            {
                codePoints.Add(char.ConvertToUtf32(c, name[i + 1]));
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                return false; // a lone surrogate
            }
            else codePoints.Add(c);
        }
        return true;
    }
}
