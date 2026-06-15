using System.Text.RegularExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// Lightweight regex validation for the lexer.
/// Only checks if the pattern/flags are syntactically valid — full JS-to-.NET
/// regex translation is handled by JSRegExp in Broiler.JavaScript.Core.
/// </summary>
internal static class RegExpValidator
{
    /// <summary>
    /// Returns true when the <paramref name="pattern"/> with the given
    /// <paramref name="flags"/> can be compiled into a .NET Regex.
    /// </summary>
    internal static bool IsValid(string pattern, string flags)
    {
        try
        {
            var options = RegexOptions.None;
            bool global = false;
            bool ignoreCase = false;
            bool multiline = false;
            bool dotAll = false;
            bool hasIndices = false;
            bool sticky = false;
            bool unicode = false;
            bool unicodeSets = false;
            if (flags != null)
            {
                foreach (var ch in flags)
                {
                    switch (ch)
                    {
                        case 'g':
                            if (global)
                                return false;
                            global = true;
                            break;
                        case 'i':
                            if (ignoreCase)
                                return false;
                            options |= RegexOptions.IgnoreCase;
                            ignoreCase = true;
                            break;
                        case 'm':
                            if (multiline)
                                return false;
                            options |= RegexOptions.Multiline;
                            multiline = true;
                            break;
                        case 's':
                            if (dotAll)
                                return false;
                            options |= RegexOptions.Singleline;
                            dotAll = true;
                            break;
                        case 'u':
                            if (unicode || unicodeSets)
                                return false;
                            unicode = true;
                            break;
                        case 'v':
                            if (unicodeSets || unicode)
                                return false;
                            unicodeSets = true;
                            break;
                        case 'y':
                            if (sticky)
                                return false;
                            sticky = true;
                            break;
                        case 'd':
                            if (hasIndices)
                                return false;
                            hasIndices = true;
                            break;
                        default:
                            return false;
                    }
                }
            }

            pattern = NormalizeES3CharacterClasses(pattern);

            if (unicodeSets)
            {
                return ValidateUnicodeSetsPattern(pattern);
            }

            if (unicode)
            {
                if (!RegExpUnicodeValidator.IsValidUnicodePattern(pattern))
                    return false;
                pattern = NormalizeUnicodePropertyEscapes(pattern);

                // A u-mode pattern that contains a surrogate code unit (an astral code
                // point written literally, or a lone surrogate) cannot be validated by
                // round-tripping the raw UTF-16 through a .NET Regex: .NET treats the
                // surrogate code units individually, so an astral range such as
                // [💩-💫] becomes the invalid UTF-16 range \uDCA9-\uD83D and is rejected,
                // which would mis-classify a valid regex literal as division. The
                // RegExpUnicodeValidator has already checked the syntax and the RegExp
                // runtime applies surrogate-aware transforms before compiling, so accept
                // it here without the (false-rejecting) .NET round-trip.
                if (ContainsSurrogate(pattern))
                    return true;
            }

            // ECMAScript permits group names .NET rejects (a leading `$`, names
            // beginning with a digit, ZWJ/ZWNJ etc.). The runtime renames every
            // named group to a synthetic safe name (JSRegExp.RewriteCaptureGroups);
            // mirror that here so the lexer accepts the literal rather than falling
            // back to division. Runs after the Unicode validator so u-mode errors
            // are still surfaced.
            pattern = NormalizeNamedGroups(pattern);

            // Annex B control escapes such as `\c0`, `\c8`, `\c_` (a `\c` not
            // followed by an ASCII letter) are valid in non-Unicode regexes but
            // rejected by .NET. Only reached for non-Unicode patterns (u-mode
            // rejects them above), so neutralise them for the compile check.
            if (!unicode)
            {
                pattern = NormalizeAnnexBControlEscapes(pattern);
                pattern = NeutralizeAnnexBClassRangeDashes(pattern);
                pattern = NeutralizeAnnexBUndefinedNamedBackref(pattern);
            }

            _ = new Regex(pattern, options);
            return true;
        }
        catch
        {
            return false;
        }
    }


    private static bool ContainsSurrogate(string pattern)
    {
        for (int i = 0; i < pattern.Length; i++)
            if (char.IsSurrogate(pattern[i]))
                return true;
        return false;
    }

    /// <summary>
    /// Performs lexer-time validation for regular expressions that use the
    /// ES2024 <c>v</c> flag.  Unicode set notation intentionally accepts
    /// constructs such as nested character classes, set subtraction, set
    /// intersection, and <c>\q{...}</c> string literals that the .NET regex
    /// engine cannot parse.  The lexer only needs to distinguish a regex
    /// literal from division, so keep this check structural and leave full
    /// semantic support to the RegExp implementation.
    /// </summary>
    private static bool ValidateUnicodeSetsPattern(string pattern)
    {
        if (pattern == null)
            return false;

        bool inClass = false;
        int classDepth = 0;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\')
            {
                if (++i >= pattern.Length)
                    return false;

                char escaped = pattern[i];
                if (escaped == 'q' && i + 1 < pattern.Length && pattern[i + 1] == '{')
                {
                    i += 2;
                    bool closed = false;
                    for (; i < pattern.Length; i++)
                    {
                        if (pattern[i] == '\\')
                        {
                            if (++i >= pattern.Length)
                                return false;
                            continue;
                        }

                        if (pattern[i] == '}')
                        {
                            closed = true;
                            break;
                        }
                    }

                    if (!closed)
                        return false;
                }
                else if ((escaped == 'p' || escaped == 'P') && i + 1 < pattern.Length && pattern[i + 1] == '{')
                {
                    int end = pattern.IndexOf('}', i + 2);
                    if (end < 0)
                        return false;
                    i = end;
                }
                else if (escaped == 'u' && i + 1 < pattern.Length && pattern[i + 1] == '{')
                {
                    int end = pattern.IndexOf('}', i + 2);
                    if (end < 0)
                        return false;
                    i = end;
                }

                continue;
            }

            if (c == '[')
            {
                inClass = true;
                classDepth++;
                continue;
            }

            if (c == ']' && inClass)
            {
                classDepth--;
                if (classDepth <= 0)
                {
                    inClass = false;
                    classDepth = 0;
                }
            }
        }

        return classDepth == 0;
    }


    /// <summary>
    /// Renames every named group <c>(?&lt;name&gt;…)</c> and named backreference
    /// <c>\k&lt;name&gt;</c> to a synthetic, .NET-safe name so a JavaScript-valid
    /// name the .NET engine would reject (e.g. <c>$</c>) does not make the lexer
    /// classify the regex literal as division. Only validity matters here, so the
    /// concrete names are irrelevant as long as declarations and references agree.
    /// </summary>
    private static string NormalizeNamedGroups(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf("(?<", System.StringComparison.Ordinal) < 0)
            return pattern;

        // Pass 1: map each distinct group name to a fresh safe name.
        var map = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        var counter = 0;
        ScanGroupNames(pattern, name =>
        {
            if (!map.ContainsKey(name))
                map[name] = "bjsv" + counter++;
        });

        if (map.Count == 0)
            return pattern;

        // Pass 2: rewrite declarations and \k<name> references.
        var sb = new System.Text.StringBuilder(pattern.Length + 8);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                if (!inClass && pattern[i + 1] == 'k' && i + 2 < pattern.Length && pattern[i + 2] == '<')
                {
                    int end = pattern.IndexOf('>', i + 3);
                    if (end > i + 3 && map.TryGetValue(pattern.Substring(i + 3, end - (i + 3)), out var safeRef))
                    {
                        sb.Append("\\k<").Append(safeRef).Append('>');
                        i = end;
                        continue;
                    }
                }

                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (c == '[') { inClass = true; sb.Append(c); continue; }
            if (c == ']') { inClass = false; sb.Append(c); continue; }

            if (c == '(' && !inClass
                && i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                int end = pattern.IndexOf('>', i + 3);
                if (end > i + 3 && map.TryGetValue(pattern.Substring(i + 3, end - (i + 3)), out var safe))
                {
                    sb.Append("(?<").Append(safe).Append('>');
                    i = end;
                    continue;
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    // Invokes onName for each named-group declaration in source order.
    private static void ScanGroupNames(string pattern, System.Action<string> onName)
    {
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length) { i++; continue; }
            if (c == '[') { inClass = true; continue; }
            if (c == ']') { inClass = false; continue; }
            if (c != '(' || inClass)
                continue;

            if (i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                int end = pattern.IndexOf('>', i + 3);
                if (end > i + 3)
                {
                    onName(pattern.Substring(i + 3, end - (i + 3)));
                    i = end;
                }
            }
        }
    }

    /// <summary>
    /// Neutralises Annex B escapes that .NET rejects but JavaScript accepts so the
    /// .NET compile check (used only to tell a regex literal from division) passes.
    /// The runtime applies the correct semantics on the original source.
    ///   • <c>\c</c> not followed by an ASCII letter (<c>\c0</c>, <c>\c_</c>, a
    ///     trailing <c>\c</c>) → literal <c>c</c>. <c>\cA</c>…<c>\cZ</c> stay.
    ///   • a decimal escape <c>\1</c>…<c>\9</c> outside a character class → the
    ///     literal digits. These are backreferences only when enough groups
    ///     precede them; otherwise Annex B treats them as IdentityEscapes, but
    ///     .NET rejects a reference to an undefined group. A literal digit always
    ///     compiles, which is all the validity check needs.
    /// </summary>
    // Letters that keep their special meaning after a backslash in a non-Unicode
    // regex; any other ASCII letter is an Annex B IdentityEscape (the literal letter).
    private const string RecognizedEscapeLetters = "bBcdDfknrsStuvwWx";

    private static bool IsHexDigit(char c)
        => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    // ECMAScript SyntaxCharacter (plus `/`): after a backslash these stay escaped, so
    // their IdentityEscape keeps the backslash; any other non-recognized character is
    // equivalent escaped or not, so the backslash is dropped for the .NET compile check.
    private static bool IsRegExpSyntaxCharacter(char c)
        => c is '^' or '$' or '\\' or '.' or '*' or '+' or '?'
            or '(' or ')' or '[' or ']' or '{' or '}' or '|' or '/';

    // Whether the `u` at <paramref name="uIndex"/> begins a `\uHHHH` unicode escape.
    private static bool IsUnicodeEscape(string pattern, int uIndex)
        => uIndex + 4 < pattern.Length
            && IsHexDigit(pattern[uIndex + 1]) && IsHexDigit(pattern[uIndex + 2])
            && IsHexDigit(pattern[uIndex + 3]) && IsHexDigit(pattern[uIndex + 4]);

    // Whether the `x` at <paramref name="xIndex"/> begins a `\xHH` hex escape.
    private static bool IsHexEscape(string pattern, int xIndex)
        => xIndex + 2 < pattern.Length
            && IsHexDigit(pattern[xIndex + 1]) && IsHexDigit(pattern[xIndex + 2]);

    // In a non-Unicode character class, a `-` adjacent to a CharacterClassEscape
    // (`\d \D \w \W \s \S`) cannot form a range, so Annex B treats it as a literal
    // `-` (e.g. `[--\d]` = `-` or a digit). .NET instead rejects the range, so
    // escape such a dash to keep the literal meaning. A `-` between two ordinary
    // atoms (`[a-z]`) is untouched.
    private static string NeutralizeAnnexBClassRangeDashes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf('-') < 0)
            return pattern;

        var sb = new System.Text.StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (c == '[') inClass = true;
            else if (c == ']') inClass = false;
            else if (c == '-' && inClass)
            {
                bool prevIsClassEscape = i >= 2 && pattern[i - 2] == '\\' && IsClassEscapeLetter(pattern[i - 1]);
                bool nextIsClassEscape = i + 2 < pattern.Length && pattern[i + 1] == '\\' && IsClassEscapeLetter(pattern[i + 2]);
                if (prevIsClassEscape || nextIsClassEscape)
                {
                    sb.Append("\\-");
                    continue;
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsClassEscapeLetter(char c)
        => c is 'd' or 'D' or 'w' or 'W' or 's' or 'S';

    // In a non-Unicode regex with NO named groups, `\k` is not a named backreference
    // — it is an Annex B IdentityEscape, so `\k<a>` matches the literal text `k<a>`
    // (e.g. `/\k<a>/.test("k<a>")` is true). .NET rejects `\k<a>` as a reference to an
    // undefined group, so drop the backslash. When named groups ARE present, an
    // undefined `\k<name>` is a real SyntaxError and is left for .NET to reject.
    private static string NeutralizeAnnexBUndefinedNamedBackref(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf("\\k<", System.StringComparison.Ordinal) < 0)
            return pattern;

        var hasNamed = false;
        ScanGroupNames(pattern, _ => hasNamed = true);
        if (hasNamed)
            return pattern;

        var sb = new System.Text.StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                if (!inClass && pattern[i + 1] == 'k' && i + 2 < pattern.Length && pattern[i + 2] == '<')
                {
                    // Drop the backslash; `k`, `<`, the name, and `>` are literal regex chars.
                    sb.Append('k');
                    i++;
                    continue;
                }

                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (c == '[') inClass = true;
            else if (c == ']') inClass = false;

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string NormalizeAnnexBControlEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf('\\') < 0)
            return pattern;

        var sb = new System.Text.StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];

                if (next == 'c')
                {
                    char after = i + 2 < pattern.Length ? pattern[i + 2] : '\0';
                    bool isControlLetter = (after >= 'A' && after <= 'Z') || (after >= 'a' && after <= 'z');
                    if (!isControlLetter)
                    {
                        // `\c<non-letter>` → literal `c`.
                        sb.Append('c');
                        i++; // consumed '\' and 'c'
                        continue;
                    }
                }
                else if (next == 'u' && !IsUnicodeEscape(pattern, i + 1))
                {
                    // `\u` not forming `\uHHHH` (non-Unicode mode) → Annex B
                    // IdentityEscape `u`. .NET rejects a bare `\u`.
                    sb.Append('u');
                    i++;
                    continue;
                }
                else if (next == 'x' && !IsHexEscape(pattern, i + 1))
                {
                    // `\x` not forming `\xHH` → Annex B IdentityEscape `x`.
                    sb.Append('x');
                    i++;
                    continue;
                }
                else if (next == 'k' && !(i + 2 < pattern.Length && pattern[i + 2] == '<'))
                {
                    // `\k` not forming a `\k<name>` backreference → IdentityEscape `k`.
                    sb.Append('k');
                    i++;
                    continue;
                }
                else if (!inClass && next >= '1' && next <= '9')
                {
                    // Decimal escape → drop the backslash, keep the digits literal.
                    sb.Append(next);
                    i++;
                    while (i + 1 < pattern.Length && pattern[i + 1] >= '0' && pattern[i + 1] <= '9')
                        sb.Append(pattern[++i]);
                    continue;
                }
                else if (!inClass
                    && !IsRegExpSyntaxCharacter(next)
                    && !(next >= '0' && next <= '9')
                    && RecognizedEscapeLetters.IndexOf(next) < 0)
                {
                    // Annex B IdentityEscape of any character that is not a recognized
                    // escape and not a SyntaxCharacter (`\C`, `\P`, `\_`, an accented
                    // letter, a combining mark, …) → the literal character. .NET rejects
                    // `\` + such a character; escaped or not it matches the same thing.
                    sb.Append(next);
                    i++;
                    continue;
                }

                sb.Append(c).Append(next);
                i++;
                continue;
            }

            if (c == '[') inClass = true;
            else if (c == ']') inClass = false;

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes ES3 empty character classes (<c>[]</c> and <c>[^]</c>) into
    /// .NET-compatible equivalents so that the pattern can be validated by the
    /// .NET <see cref="Regex"/> engine.  The full JS-to-.NET transformation
    /// is performed later by <c>JSRegExp.TransformES3Patterns</c>.
    /// </summary>
    private static string NormalizeES3CharacterClasses(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        // Quick check — avoid StringBuilder allocation for patterns that
        // cannot contain empty character classes.  This may false-positive on
        // patterns like "a][]b" where "][]" spans two constructs, but the
        // full loop below handles those correctly (it only rewrites actual
        // empty classes found outside existing character classes).
        if (!pattern.Contains("[]") && !pattern.Contains("[^]"))
            return pattern;

        var sb = new System.Text.StringBuilder(pattern.Length + 8);
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                sb.Append(c);
                sb.Append(pattern[++i]);
                continue;
            }

            if (inClass)
            {
                if (c == ']')
                    inClass = false;
                sb.Append(c);
                continue;
            }

            if (c == '[')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == ']')
                {
                    // [] — empty character class, matches nothing
                    sb.Append("[^\\s\\S]");
                    i++; // skip ']'
                    continue;
                }

                if (i + 2 < pattern.Length && pattern[i + 1] == '^' && pattern[i + 2] == ']')
                {
                    // [^] — complement of empty class, matches any character
                    sb.Append("[\\s\\S]");
                    i += 2; // skip '^]'
                    continue;
                }

                inClass = true;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Rewrites JavaScript Unicode property escapes into simple placeholders so
    /// the lexer can validate their syntax without relying on the .NET regex
    /// engine's property-name support.
    /// </summary>
    private static string NormalizeUnicodePropertyEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || (!pattern.Contains(@"\p{") && !pattern.Contains(@"\P{")))
            return pattern;

        var sb = new System.Text.StringBuilder(pattern.Length);

        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '\\' && i + 2 < pattern.Length && (pattern[i + 1] == 'p' || pattern[i + 1] == 'P') && pattern[i + 2] == '{')
            {
                int end = pattern.IndexOf('}', i + 3);
                if (end > i + 3)
                {
                    sb.Append('A');
                    i = end;
                    continue;
                }
            }

            if (pattern[i] == '\\' && i + 1 < pattern.Length)
            {
                sb.Append(pattern[i]);
                sb.Append(pattern[++i]);
                continue;
            }

            sb.Append(pattern[i]);
        }

        return sb.ToString();
    }
}
