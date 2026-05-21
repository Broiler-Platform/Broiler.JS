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

            if (unicode || unicodeSets)
                pattern = NormalizeUnicodePropertyEscapes(pattern);

            _ = new Regex(pattern, options);
            return true;
        }
        catch
        {
            return false;
        }
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
