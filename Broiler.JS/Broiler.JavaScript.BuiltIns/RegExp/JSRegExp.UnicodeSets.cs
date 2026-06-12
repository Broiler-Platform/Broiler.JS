using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using UnicodeEmoji.StringProperties;
using Broiler.Unicode.Properties;

namespace Broiler.JavaScript.BuiltIns.RegExp;

// v-flag (unicodeSets) extended character classes with set operations.
//
// The `v` flag adds "class set expressions" that .NET regex cannot express:
//   * properties of strings (\p{RGI_Emoji}, \p{Emoji_Keycap_Sequence}, …) which
//     match multi-code-point sequences,
//   * string literals \q{a|b|cd},
//   * and the set operators difference `A--B`, intersection `A&&B`, plus nested
//     classes [[…]…].
//
// We evaluate such a class to a concrete set — a set of single code points (as
// ranges) plus a set of multi-code-point strings — then emit an equivalent .NET
// fragment: a longest-first alternation of the literal strings followed by a
// single code-point matcher. Plain classes (no operators / strings / property of
// strings) are left untouched for the existing surrogate-aware transforms to
// handle.
//
// Anything we cannot reduce to an exact set (e.g. a General_Category property,
// for which we only have a .NET short name and no enumerable ranges, or \s inside
// a set operation) throws the clear "not supported" SyntaxError rather than
// risk a wrong match.
partial class JSRegExp
{
    /// <summary>
    /// A class set value: single code points (as normalized ranges) plus
    /// multi-code-point string elements (length ≥ 2 code points, or the empty
    /// string). Single code points always live in <see cref="Ranges"/>.
    /// </summary>
    private sealed class ClassSetValue
    {
        public readonly List<(int Lo, int Hi)> Ranges = new();
        public readonly HashSet<string> Strings = new(StringComparer.Ordinal);

        public void AddCodePoint(int cp) => Ranges.Add((cp, cp));
        public void AddRange(int lo, int hi) => Ranges.Add((lo, hi));

        public void AddString(string s)
        {
            // A string of a single code point is just a code point.
            if (s.Length == 1)
            {
                AddCodePoint(s[0]);
                return;
            }
            if (s.Length == 2 && char.IsHighSurrogate(s[0]) && char.IsLowSurrogate(s[1]))
            {
                AddCodePoint(char.ConvertToUtf32(s[0], s[1]));
                return;
            }
            Strings.Add(s);
        }
    }

    /// <summary>
    /// Rewrites every top-level v-mode character class that uses set operations,
    /// string literals, or properties of strings into an equivalent .NET fragment.
    /// Other classes are copied through unchanged. Only called when the `v` flag
    /// is set.
    /// </summary>
    private static string TransformUnicodeSetsClasses(string pattern)
    {
        if (pattern.IndexOf('[') < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        int i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                sb.Append(c).Append(pattern[i + 1]);
                i += 2;
                continue;
            }

            if (c == '[')
            {
                var end = FindClassEnd(pattern, i);
                var classText = pattern.Substring(i, end - i);
                var inner = classText.Substring(1, classText.Length - 2);
                if (ClassNeedsSetEvaluation(inner))
                    sb.Append(EvaluateClassSet(inner));
                else
                    sb.Append(classText);
                i = end;
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the index just past the <c>]</c> that closes the class beginning at
    /// <paramref name="start"/> (which points at <c>[</c>), honouring escapes,
    /// nested <c>[...]</c> classes (v-mode classes nest), and <c>\q{...}</c>.
    /// </summary>
    private static int FindClassEnd(string pattern, int start)
    {
        int depth = 0;
        int i = start;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '\\')
            {
                // \q{...}: skip to the closing brace so a ']' inside cannot end the class.
                if (i + 2 < pattern.Length && pattern[i + 1] == 'q' && pattern[i + 2] == '{')
                {
                    var brace = pattern.IndexOf('}', i + 3);
                    i = brace < 0 ? pattern.Length : brace + 1;
                    continue;
                }
                i += 2;
                continue;
            }
            if (c == '[')
                depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                    return i + 1;
            }
            i++;
        }
        // Unbalanced — let the downstream .NET compile surface the error.
        return pattern.Length;
    }

    /// <summary>
    /// True when a class body uses a v-mode construct that .NET cannot express
    /// directly: the difference (<c>--</c>) or intersection (<c>&amp;&amp;</c>)
    /// operators, a string literal (<c>\q{</c>), or a property of strings.
    /// </summary>
    private static bool ClassNeedsSetEvaluation(string inner)
    {
        int i = 0;
        while (i < inner.Length)
        {
            var c = inner[i];
            if (c == '\\' && i + 1 < inner.Length)
            {
                var n = inner[i + 1];
                if (n == 'q')
                    return true;
                if ((n == 'p' || n == 'P') && i + 2 < inner.Length && inner[i + 2] == '{')
                {
                    var close = inner.IndexOf('}', i + 3);
                    if (close > 0)
                    {
                        var name = NormalizeKey(inner.Substring(i + 3, close - (i + 3)));
                        if (EmojiStringPropertyNames.ContainsKey(name))
                            return true;
                    }
                }
                i += 2;
                continue;
            }

            if ((c == '-' && i + 1 < inner.Length && inner[i + 1] == '-')
                || (c == '&' && i + 1 < inner.Length && inner[i + 1] == '&'))
                return true;

            i++;
        }
        return false;
    }

    private static string NormalizeKey(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch == '_' || ch == ' ')
                continue;
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    // ---- evaluation ----

    private static string EvaluateClassSet(string inner)
    {
        int pos = 0;
        bool negated = false;
        if (pos < inner.Length && inner[pos] == '^')
        {
            negated = true;
            pos++;
        }

        var value = ParseClassSet(inner, ref pos);
        if (pos != inner.Length)
            throw NewUnsupportedSetError();

        if (negated)
        {
            // A negated class containing strings is a SyntaxError in v-mode.
            if (value.Strings.Count > 0)
                throw NewUnsupportedSetError();
            value = ComplementCodePoints(value);
        }

        return Emit(value);
    }

    private static ClassSetValue ParseClassSet(string s, ref int pos)
    {
        var (first, _) = ParseOperand(s, ref pos);

        if (AtOperator(s, pos, '&'))
        {
            var result = first;
            while (AtOperator(s, pos, '&'))
            {
                pos += 2;
                var (operand, _) = ParseOperand(s, ref pos);
                result = Intersect(result, operand);
            }
            return result;
        }

        if (AtOperator(s, pos, '-'))
        {
            var result = first;
            while (AtOperator(s, pos, '-'))
            {
                pos += 2;
                var (operand, _) = ParseOperand(s, ref pos);
                result = Subtract(result, operand);
            }
            return result;
        }

        // Union (juxtaposition). Fold the first item in, handling a leading range.
        var union = new ClassSetValue();
        IncorporateUnionItem(union, first, FirstSingleCodePoint(first), s, ref pos);
        while (pos < s.Length)
        {
            if (AtOperator(s, pos, '&') || AtOperator(s, pos, '-'))
                throw NewUnsupportedSetError(); // operators cannot mix with a union
            var (item, itemCp) = ParseOperand(s, ref pos);
            IncorporateUnionItem(union, item, itemCp, s, ref pos);
        }
        return union;
    }

    private static bool AtOperator(string s, int pos, char op)
        => pos + 1 < s.Length && s[pos] == op && s[pos + 1] == op;

    private static int FirstSingleCodePoint(ClassSetValue v)
        => v.Strings.Count == 0 && v.Ranges.Count == 1 && v.Ranges[0].Lo == v.Ranges[0].Hi
            ? v.Ranges[0].Lo
            : -1;

    /// <summary>
    /// Adds a union item to <paramref name="union"/>. When the item is a single
    /// code point and the next token is <c>-</c> (a range, not the <c>--</c>
    /// operator), consumes the range end and adds the whole range.
    /// </summary>
    private static void IncorporateUnionItem(ClassSetValue union, ClassSetValue item, int singleCp, string s, ref int pos)
    {
        if (singleCp >= 0 && pos < s.Length && s[pos] == '-' && !AtOperator(s, pos, '-') && pos + 1 < s.Length && s[pos + 1] != ']')
        {
            pos++; // consume '-'
            var (endItem, endCp) = ParseOperand(s, ref pos);
            if (endCp < 0)
                throw NewUnsupportedSetError();
            if (endCp < singleCp)
                throw NewUnsupportedSetError();
            union.AddRange(singleCp, endCp);
            return;
        }

        union.Ranges.AddRange(item.Ranges);
        foreach (var str in item.Strings)
            union.Strings.Add(str);
    }

    /// <summary>
    /// Parses a single ClassSetOperand (no range): a nested class, \q{...} string
    /// literal, character class escape, unicode property, or a single character.
    /// Returns the value and, when the operand is one literal code point, that code
    /// point (else -1) so the caller can recognise a union range.
    /// </summary>
    private static (ClassSetValue value, int singleCp) ParseOperand(string s, ref int pos)
    {
        if (pos >= s.Length)
            throw NewUnsupportedSetError();

        var c = s[pos];

        if (c == '[')
        {
            int end = FindClassEnd(s, pos);
            var innerText = s.Substring(pos + 1, end - pos - 2);
            pos = end;
            int inner = 0;
            bool negated = false;
            if (inner < innerText.Length && innerText[inner] == '^')
            {
                negated = true;
                inner++;
            }
            var nested = ParseClassSet(innerText, ref inner);
            if (inner != innerText.Length)
                throw NewUnsupportedSetError();
            if (negated)
            {
                if (nested.Strings.Count > 0)
                    throw NewUnsupportedSetError();
                nested = ComplementCodePoints(nested);
            }
            return (nested, -1);
        }

        if (c == '\\')
        {
            if (pos + 1 >= s.Length)
                throw NewUnsupportedSetError();
            var n = s[pos + 1];

            if (n == 'q')
            {
                if (pos + 2 >= s.Length || s[pos + 2] != '{')
                    throw NewUnsupportedSetError();
                var brace = s.IndexOf('}', pos + 3);
                if (brace < 0)
                    throw NewUnsupportedSetError();
                var body = s.Substring(pos + 3, brace - (pos + 3));
                pos = brace + 1;
                var value = new ClassSetValue();
                foreach (var alt in ParseStringDisjunction(body))
                    value.AddString(alt);
                return (value, -1);
            }

            if (n == 'p' || n == 'P')
            {
                if (pos + 2 >= s.Length || s[pos + 2] != '{')
                    throw NewUnsupportedSetError();
                var close = s.IndexOf('}', pos + 3);
                if (close < 0)
                    throw NewUnsupportedSetError();
                var propInner = s.Substring(pos + 3, close - (pos + 3));
                pos = close + 1;
                return (ResolvePropertyOperand(n == 'P', propInner), -1);
            }

            if (TryGetClassEscapeRanges(n, out var escRanges))
            {
                pos += 2;
                var value = new ClassSetValue();
                value.Ranges.AddRange(escRanges);
                return (value, -1);
            }

            // A single escaped character.
            int escCp = ReadCodePoint(s, ref pos);
            var single = new ClassSetValue();
            single.AddCodePoint(escCp);
            return (single, escCp);
        }

        // Plain literal code point.
        int cp = ReadCodePoint(s, ref pos);
        var lit = new ClassSetValue();
        lit.AddCodePoint(cp);
        return (lit, cp);
    }

    /// <summary>Splits a <c>\q{...}</c> body on top-level <c>|</c> into UTF-16 strings.</summary>
    private static IEnumerable<string> ParseStringDisjunction(string body)
    {
        var alternatives = new List<string>();
        var current = new StringBuilder();
        int pos = 0;
        while (pos < body.Length)
        {
            if (body[pos] == '|')
            {
                alternatives.Add(current.ToString());
                current.Clear();
                pos++;
                continue;
            }
            int cp = ReadCodePoint(body, ref pos);
            current.Append(char.ConvertFromUtf32(cp));
        }
        alternatives.Add(current.ToString());
        return alternatives;
    }

    private static ClassSetValue ResolvePropertyOperand(bool negated, string inner)
    {
        var normalized = NormalizeKey(inner);

        if (EmojiStringPropertyNames.TryGetValue(normalized, out var emojiProperty))
        {
            // \P of a property of strings is a SyntaxError.
            if (negated)
                throw NewUnsupportedSetError();
            var value = new ClassSetValue();
            foreach (var seq in EmojiStringProperties.GetSequences(emojiProperty))
                value.AddString(seq);
            return value;
        }

        // Character properties for which we have enumerable ranges.
        var eq = inner.IndexOf('=');
        (int Lo, int Hi)[] ranges = null;
        if (eq >= 0)
        {
            var name = NormalizeKey(inner.Substring(0, eq));
            var val = NormalizeKey(inner.Substring(eq + 1));
            if (name is "sc" or "script" or "scx" or "scriptextensions")
                ScriptRanges.TryGetValue(val, out ranges);
        }
        else
        {
            ranges = UnicodeProperties.GetBinaryProperty(normalized);
            if (ranges == null)
                ScriptRanges.TryGetValue(normalized, out ranges);
        }

        if (ranges == null)
            throw NewUnsupportedSetError(); // e.g. General_Category — no enumerable ranges here

        var result = new ClassSetValue();
        result.Ranges.AddRange(ranges);
        return negated ? ComplementCodePoints(result) : result;
    }

    private static bool TryGetClassEscapeRanges(char escape, out (int Lo, int Hi)[] ranges)
    {
        switch (escape)
        {
            case 'd':
                ranges = new (int Lo, int Hi)[] { ('0', '9') };
                return true;
            case 'D':
                ranges = new (int Lo, int Hi)[] { (0, '0' - 1), ('9' + 1, 0x10FFFF) };
                return true;
            case 'w':
                ranges = WordRanges;
                return true;
            case 'W':
                ranges = ComplementRanges(WordRanges);
                return true;
            default:
                ranges = null;
                return false;
        }
    }

    private static readonly (int Lo, int Hi)[] WordRanges =
    {
        ('0', '9'), ('A', 'Z'), ('_', '_'), ('a', 'z')
    };

    /// <summary>Reads one code point (with escapes) and advances <paramref name="pos"/>.</summary>
    private static int ReadCodePoint(string s, ref int pos)
    {
        var c = s[pos];
        if (c == '\\')
        {
            pos++;
            if (pos >= s.Length)
                throw NewUnsupportedSetError();
            var e = s[pos];
            switch (e)
            {
                case 'u':
                    pos++;
                    if (pos < s.Length && s[pos] == '{')
                    {
                        var close = s.IndexOf('}', pos + 1);
                        if (close < 0)
                            throw NewUnsupportedSetError();
                        var hex = s.Substring(pos + 1, close - (pos + 1));
                        pos = close + 1;
                        return System.Convert.ToInt32(hex, 16);
                    }
                    else
                    {
                        int high = ReadFixedHex(s, ref pos, 4);
                        // Combine a surrogate pair written as two \u escapes.
                        if (char.IsHighSurrogate((char)high) && pos + 1 < s.Length && s[pos] == '\\' && s[pos + 1] == 'u')
                        {
                            int save = pos;
                            pos += 2;
                            int low = ReadFixedHex(s, ref pos, 4);
                            if (char.IsLowSurrogate((char)low))
                                return char.ConvertToUtf32((char)high, (char)low);
                            pos = save; // not a low surrogate — leave it
                        }
                        return high;
                    }
                case 'x':
                    pos++;
                    return ReadFixedHex(s, ref pos, 2);
                case 'n': pos++; return '\n';
                case 'r': pos++; return '\r';
                case 't': pos++; return '\t';
                case 'f': pos++; return '\f';
                case 'v': pos++; return '\v';
                case '0': pos++; return 0;
                default:
                    pos++;
                    return e; // escaped literal: \- \] \\ \^ \| \{ \} etc.
            }
        }

        if (char.IsHighSurrogate(c) && pos + 1 < s.Length && char.IsLowSurrogate(s[pos + 1]))
        {
            int cp = char.ConvertToUtf32(c, s[pos + 1]);
            pos += 2;
            return cp;
        }

        pos++;
        return c;
    }

    private static int ReadFixedHex(string s, ref int pos, int count)
    {
        if (pos + count > s.Length)
            throw NewUnsupportedSetError();
        var hex = s.Substring(pos, count);
        pos += count;
        return System.Convert.ToInt32(hex, 16);
    }

    // ---- set algebra on normalized code-point ranges ----

    private static List<(int Lo, int Hi)> Normalize(IEnumerable<(int Lo, int Hi)> ranges)
    {
        var list = new List<(int Lo, int Hi)>(ranges);
        list.Sort((a, b) => a.Lo != b.Lo ? a.Lo.CompareTo(b.Lo) : a.Hi.CompareTo(b.Hi));
        var merged = new List<(int Lo, int Hi)>();
        foreach (var (lo, hi) in list)
        {
            if (hi < lo)
                continue;
            if (merged.Count > 0 && lo <= merged[^1].Hi + 1)
            {
                if (hi > merged[^1].Hi)
                    merged[^1] = (merged[^1].Lo, hi);
            }
            else
            {
                merged.Add((lo, hi));
            }
        }
        return merged;
    }

    private static ClassSetValue Intersect(ClassSetValue a, ClassSetValue b)
    {
        var ra = Normalize(a.Ranges);
        var rb = Normalize(b.Ranges);
        var result = new ClassSetValue();
        int i = 0, j = 0;
        while (i < ra.Count && j < rb.Count)
        {
            int lo = Math.Max(ra[i].Lo, rb[j].Lo);
            int hi = Math.Min(ra[i].Hi, rb[j].Hi);
            if (lo <= hi)
                result.AddRange(lo, hi);
            if (ra[i].Hi < rb[j].Hi) i++; else j++;
        }
        foreach (var s in a.Strings)
            if (b.Strings.Contains(s))
                result.Strings.Add(s);
        return result;
    }

    private static ClassSetValue Subtract(ClassSetValue a, ClassSetValue b)
    {
        var result = new ClassSetValue();
        foreach (var r in SubtractRanges(Normalize(a.Ranges), Normalize(b.Ranges)))
            result.AddRange(r.Lo, r.Hi);
        foreach (var s in a.Strings)
            if (!b.Strings.Contains(s))
                result.Strings.Add(s);
        return result;
    }

    private static List<(int Lo, int Hi)> SubtractRanges(List<(int Lo, int Hi)> a, List<(int Lo, int Hi)> b)
    {
        var result = new List<(int Lo, int Hi)>();
        foreach (var (lo, hi) in a)
        {
            int cur = lo;
            foreach (var (blo, bhi) in b)
            {
                if (bhi < cur || blo > hi)
                    continue;
                if (blo > cur)
                    result.Add((cur, blo - 1));
                cur = Math.Max(cur, bhi + 1);
                if (cur > hi)
                    break;
            }
            if (cur <= hi)
                result.Add((cur, hi));
        }
        return result;
    }

    private static (int Lo, int Hi)[] ComplementRanges((int Lo, int Hi)[] ranges)
        => SubtractRanges(new List<(int Lo, int Hi)> { (0, 0x10FFFF) }, Normalize(ranges)).ToArray();

    private static ClassSetValue ComplementCodePoints(ClassSetValue v)
    {
        var result = new ClassSetValue();
        foreach (var r in SubtractRanges(new List<(int Lo, int Hi)> { (0, 0x10FFFF) }, Normalize(v.Ranges)))
            result.AddRange(r.Lo, r.Hi);
        return result;
    }

    // ---- emission ----

    private static string Emit(ClassSetValue value)
    {
        var ranges = Normalize(value.Ranges);

        var alternatives = new List<string>();

        // Multi-code-point strings, longest first so leftmost-longest matching picks
        // the maximal element (e.g. a keycap sequence over its leading digit).
        var strings = new List<string>(value.Strings);
        strings.Sort(static (a, b) =>
        {
            int byLen = b.Length.CompareTo(a.Length);
            return byLen != 0 ? byLen : string.CompareOrdinal(a, b);
        });
        bool matchesEmpty = false;
        foreach (var str in strings)
        {
            if (str.Length == 0)
            {
                matchesEmpty = true;
                continue;
            }
            alternatives.Add(Regex.Escape(str));
        }

        if (ranges.Count > 0)
            alternatives.Add(BuildPositiveCodePointMatcher(ranges.ToArray()));

        if (matchesEmpty)
            alternatives.Add(string.Empty);

        if (alternatives.Count == 0)
            return "[^\\s\\S]"; // matches no character

        var sb = new StringBuilder("(?:");
        for (int i = 0; i < alternatives.Count; i++)
        {
            if (i > 0)
                sb.Append('|');
            sb.Append(alternatives[i]);
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static JSException NewUnsupportedSetError()
        => JSEngine.NewSyntaxError(
            "Unicode v-mode class set expression is not supported yet (requires Unicode set/sequence data)");
}
