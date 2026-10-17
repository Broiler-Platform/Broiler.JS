namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Structural validation of a regular-expression pattern under the ES2015+
/// Unicode (u) flag. Shared by the lexer (regex literals) and the RegExp
/// constructor so both reject the same set of patterns.
/// </summary>
public static class RegExpUnicodeValidator
{
    /// <summary>
    /// Validates that a regex pattern uses only escapes permitted by the
    /// ES2015+ Unicode mode.  Identity escapes (e.g. <c>\A</c>, <c>\-</c>
    /// outside a character class) and invalid character class ranges (e.g.
    /// <c>[\w-\d]</c>) are rejected.
    /// </summary>
    public static bool IsValidUnicodePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        bool inClass = false;
        // Group-type stack: true if the group is a lookahead/lookbehind assertion.
        var groupAssertion = new System.Collections.Generic.Stack<bool>();
        // Set when the previous atom was a closing ')' of an assertion group; a
        // following quantifier is then a SyntaxError in Unicode mode.
        bool afterAssertion = false;
        int captureGroupCount = CountCaptureGroups(pattern);

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            // QuantifiableAssertion only applies in Annex B (non-Unicode): in
            // Unicode mode a quantifier directly on a lookahead/lookbehind is invalid.
            if (afterAssertion && !inClass && (c == '*' || c == '+' || c == '?' || c == '{'))
                return false;
            afterAssertion = false;

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];

                // Decimal escapes: in Unicode mode the only legal forms are \0 (not
                // followed by a digit) and, outside a character class, a backreference
                // \k to an existing capture group. Octal escapes, \0 followed by a
                // digit, and any \1-\9 inside a class are SyntaxErrors.
                if (next >= '0' && next <= '9')
                {
                    if (next == '0')
                    {
                        if (i + 2 < pattern.Length && pattern[i + 2] >= '0' && pattern[i + 2] <= '9')
                            return false;
                        i++;
                        continue;
                    }

                    if (inClass)
                        return false;

                    int j = i + 1;
                    int num = 0;
                    while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9')
                    {
                        num = num * 10 + (pattern[j] - '0');
                        j++;
                    }
                    if (num > captureGroupCount)
                        return false;
                    i = j - 1;
                    continue;
                }

                // \cX is a control escape only when followed by an ASCII letter.
                if (next == 'c')
                {
                    if (!(i + 2 < pattern.Length && IsAsciiLetter(pattern[i + 2])))
                        return false;
                    i += 2;
                    continue;
                }

                if (!IsAllowedUnicodeEscape(next, inClass, pattern, i))
                    return false;

                // Skip the escape sequence length
                if (next == 'u')
                {
                    // In Unicode mode `\u` must be followed by exactly four hex digits
                    // or a `\u{CodePoint}` braced escape; anything else (too few digits,
                    // a non-hex digit, an empty/unterminated brace) is a SyntaxError.
                    if (i + 2 < pattern.Length && pattern[i + 2] == '{')
                    {
                        int end = pattern.IndexOf('}', i + 3);
                        if (end <= i + 3) return false;
                        for (int k = i + 3; k < end; k++)
                            if (!IsHexDigit(pattern[k])) return false;
                        i = end;
                    }
                    else if (i + 5 < pattern.Length
                        && IsHexDigit(pattern[i + 2]) && IsHexDigit(pattern[i + 3])
                        && IsHexDigit(pattern[i + 4]) && IsHexDigit(pattern[i + 5]))
                    {
                        i += 5; // \uNNNN
                    }
                    else
                    {
                        return false;
                    }
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

            if (inClass)
            {
                if (c == ']')
                    inClass = false;
                continue;
            }

            // Outside a character class, the following are syntax characters in
            // Unicode mode and must be escaped (or, for '{', form a valid quantifier).
            switch (c)
            {
                case '(':
                    groupAssertion.Push(IsAssertionGroupStart(pattern, i));
                    continue;

                case ')':
                    if (groupAssertion.Count > 0)
                        afterAssertion = groupAssertion.Pop();
                    continue;

                case ']':
                case '}':
                    // A lone ']' or '}' is a literal only under Annex B; Unicode mode
                    // requires it to be escaped.
                    return false;

                case '{':
                {
                    // Must be a valid quantifier '{n}', '{n,}' or '{n,m}'.
                    int j = i + 1;
                    int digits = 0;
                    while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9') { j++; digits++; }
                    if (digits == 0)
                        return false;
                    if (j < pattern.Length && pattern[j] == ',')
                    {
                        j++;
                        while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9') j++;
                    }
                    if (j >= pattern.Length || pattern[j] != '}')
                        return false;
                    i = j; // consume through the closing '}'
                    continue;
                }
            }
        }

        return true;
    }

    private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    // Counts capturing groups (plain '(' and named '(?<name>') so backreferences can
    // be validated in Unicode mode. Non-capturing groups and lookaround assertions
    // are excluded.
    private static int CountCaptureGroups(string pattern)
    {
        int count = 0;
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\')
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

            if (!inClass && c == '(')
            {
                if (i + 1 >= pattern.Length || pattern[i + 1] != '?')
                    count++;
                else if (i + 2 < pattern.Length && pattern[i + 2] == '<'
                    && i + 3 < pattern.Length && pattern[i + 3] != '=' && pattern[i + 3] != '!')
                    count++;
            }
        }

        return count;
    }

    // Whether the group starting at '(' (index <paramref name="open"/>) is a
    // lookahead/lookbehind assertion ((?= (?! (?<= (?<!) rather than a capturing,
    // non-capturing, or named-capturing group.
    private static bool IsAssertionGroupStart(string pattern, int open)
    {
        if (open + 2 >= pattern.Length || pattern[open + 1] != '?')
            return false;

        char d = pattern[open + 2];
        if (d == '=' || d == '!')
            return true;

        return d == '<' && open + 3 < pattern.Length && (pattern[open + 3] == '=' || pattern[open + 3] == '!');
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
            // Named backreference \k<GroupName>
            case 'k':
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
    /// Validates character class content for unicode mode: no range may have a class escape at either end
    /// (<c>\d \D \s \S \w \W</c>, or a property escape <c>\p{…}</c> / <c>\P{…}</c>), the early error of
    /// <c>NonemptyClassRanges</c> and <c>NonemptyClassRangesNoDash</c> (ECMA-262 22.2.1.1, IsCharacterClass).
    /// </summary>
    /// <remarks>
    /// The class is read atom by atom as ClassRanges is: an atom, then, when a <c>-</c> follows it and something
    /// other than the closing <c>]</c> follows that, the range's other end. So <c>[--\p{Hex}]</c> and
    /// <c>[\p{Hex}-\uFFFF]</c> are refused, while <c>[\w-]</c>, <c>[-\w]</c> and <c>[a\-\d]</c>, which hold no
    /// range, are not.
    /// </remarks>
    private static bool ValidateUnicodeCharacterClass(string pattern, int classStart)
    {
        int i = classStart + 1;
        if (i < pattern.Length && pattern[i] == '^')
            i++;

        while (i < pattern.Length && pattern[i] != ']')
        {
            if (!ReadClassAtom(pattern, ref i, out bool startIsClass))
                return false;

            if (i + 1 < pattern.Length && pattern[i] == '-' && pattern[i + 1] != ']')
            {
                i++;
                if (!ReadClassAtom(pattern, ref i, out bool endIsClass))
                    return false;
                if (startIsClass || endIsClass)
                    return false;
            }
        }

        return true;
    }

    // Reads one ClassAtom at pattern[i], leaving i just after it, and says whether it is a class escape.
    // Returns false for an escape cut short by the pattern's end.
    private static bool ReadClassAtom(string pattern, ref int i, out bool isClass)
    {
        isClass = false;
        char c = pattern[i];

        if (c != '\\')
        {
            i += char.IsHighSurrogate(c) && i + 1 < pattern.Length && char.IsLowSurrogate(pattern[i + 1]) ? 2 : 1;
            return true;
        }

        if (i + 1 >= pattern.Length)
            return false;

        char next = pattern[i + 1];
        isClass = next is 'd' or 'D' or 'w' or 'W' or 's' or 'S' or 'p' or 'P';

        if ((next == 'u' || next == 'p' || next == 'P') && i + 2 < pattern.Length && pattern[i + 2] == '{')
        {
            int end = pattern.IndexOf('}', i + 3);
            if (end < 0) return false;
            i = end + 1;
        }
        else if (next == 'u')
        {
            i = System.Math.Min(i + 6, pattern.Length);
        }
        else if (next == 'x')
        {
            i = System.Math.Min(i + 4, pattern.Length);
        }
        else if (next == 'c')
        {
            i = System.Math.Min(i + 3, pattern.Length);
        }
        else
        {
            i += 2;
        }

        return true;
    }
}
