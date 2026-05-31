using System.Text.RegularExpressions;

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
                if (!ValidateUnicodePattern(pattern))
                    return false;
                pattern = NormalizeUnicodePropertyEscapes(pattern);
            }

            _ = new Regex(pattern, options);
            return true;
        }
        catch
        {
            return false;
        }
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
    /// Validates that a regex pattern uses only escapes permitted by the
    /// ES2015+ Unicode mode.  Identity escapes (e.g. <c>\A</c>, <c>\-</c>
    /// outside a character class) and invalid character class ranges (e.g.
    /// <c>[\w-\d]</c>) are rejected.
    /// </summary>
    private static bool ValidateUnicodePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];

                if (!IsAllowedUnicodeEscape(next, inClass, pattern, i))
                    return false;

                // Skip the escape sequence length
                if (next == 'u' && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    // \u{NNNNN}
                    int end = pattern.IndexOf('}', i + 3);
                    if (end < 0) return false;
                    i = end;
                }
                else if (next == 'u' && i + 5 < pattern.Length)
                {
                    i += 5; // \uNNNN
                }
                else if (next == 'x' && i + 3 < pattern.Length)
                {
                    i += 3; // \xNN
                }
                else if (next == 'c' && i + 2 < pattern.Length)
                {
                    i += 2; // \cA
                }
                else if ((next == 'p' || next == 'P') && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    int end = pattern.IndexOf('}', i + 3);
                    if (end < 0) return false;
                    i = end;
                }
                else
                {
                    i++; // simple two-char escape
                }

                continue;
            }

            if (!inClass && c == '[')
            {
                inClass = true;

                // Validate character class ranges: in unicode mode,
                // ranges like [\w-\d] are forbidden (class escape as range endpoint).
                if (!ValidateUnicodeCharacterClass(pattern, i))
                    return false;

                continue;
            }

            if (inClass && c == ']')
            {
                inClass = false;
                continue;
            }
        }

        return true;
    }

    private static bool IsAllowedUnicodeEscape(char next, bool inClass, string pattern, int backslashIndex)
    {
        switch (next)
        {
            // Assertion escapes
            case 'b': case 'B':
            // Character class escapes
            case 'd': case 'D': case 'w': case 'W': case 's': case 'S':
            // Character escapes
            case 'f': case 'n': case 'r': case 't': case 'v':
            case '0':
            case 'x': case 'u': case 'c':
            // Unicode property escapes
            case 'p': case 'P':
            // Syntax characters that can be escaped
            case '^': case '$': case '.': case '*': case '+': case '?':
            case '(': case ')': case '[': case ']': case '{': case '}':
            case '|': case '\\': case '/':
                return true;
            case '-':
                // \- is allowed inside character classes, not outside
                return inClass;
            default:
                // Check for backreferences \1-\9
                if (next >= '1' && next <= '9')
                    return true;
                return false;
        }
    }

    /// <summary>
    /// Validates character class content for unicode mode.
    /// Rejects ranges where either endpoint is a character class escape (\w, \d, etc.)
    /// </summary>
    private static bool ValidateUnicodeCharacterClass(string pattern, int classStart)
    {
        int i = classStart + 1;
        if (i < pattern.Length && pattern[i] == '^')
            i++;

        bool prevIsClassEscape = false;

        while (i < pattern.Length)
        {
            char c = pattern[i];

            if (c == ']')
                return true;

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];
                bool isClassEscape = next is 'd' or 'D' or 'w' or 'W' or 's' or 'S';

                // Check if this is the end of a range: prevChar-\w
                if (isClassEscape && i >= 2 && pattern[i - 1] == '-' && !prevIsClassEscape)
                {
                    // The '-' before a class escape forms an invalid range
                    return false;
                }

                prevIsClassEscape = isClassEscape;

                // Check if next char is '-' forming range start: \w-nextChar
                if (isClassEscape && i + 2 < pattern.Length && pattern[i + 2] == '-' && i + 3 < pattern.Length && pattern[i + 3] != ']')
                {
                    return false;
                }

                // Skip escape
                if (next == 'u' && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    int end = pattern.IndexOf('}', i + 3);
                    if (end < 0) return false;
                    i = end + 1;
                }
                else if (next == 'u')
                {
                    i += 6;
                }
                else if (next == 'x')
                {
                    i += 4;
                }
                else if ((next == 'p' || next == 'P') && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    int end = pattern.IndexOf('}', i + 3);
                    if (end < 0) return false;
                    i = end + 1;
                }
                else
                {
                    i += 2;
                }
                continue;
            }

            prevIsClassEscape = false;
            i++;
        }

        return true;
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
