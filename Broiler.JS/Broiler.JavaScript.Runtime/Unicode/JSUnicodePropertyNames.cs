#nullable enable

using System;

namespace Broiler.JavaScript.Runtime.Unicode;

/// <summary>
/// The names of a pattern's property escapes, matched exactly against ECMA-262's tables (ECMA-262 22.2.2.9,
/// UnicodeMatchProperty and UnicodeMatchPropertyValue) by Broiler.Unicode's lookup, before any engine reads
/// the pattern (milestone SJL-7.5; correspondence rows P1 to P3 and S2). The tables' names and aliases are
/// admitted as they are written, and no other: a name in another case, or with its underscores, spaces or
/// hyphens moved, is refused.
/// </summary>
/// <remarks>
/// Both the regular-expression literal's early check in the parser and the RegExp constructor call it, under
/// the <c>u</c> or <c>v</c> flag only: without them <c>\p</c> is not a property escape. The engines that run
/// the pattern afterwards, Broiler.Regex and the .NET translator, resolve the names they are given in their
/// own ways, and this check is what keeps a name the specification refuses from reaching either.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.2.9 and 22.2.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: a pattern under u or v whose property escape names something ECMA-262's tables do not hold, exactly as written, is admitted, or one they hold is refused
// Broiler-Human:        PENDING
internal static class JSUnicodePropertyNames
{
    /// <summary>
    /// The first property escape of <paramref name="pattern"/> whose name is refused, as it is written
    /// (<c>\p{…}</c>, or <c>\p</c> with no braced name), or <see langword="null"/> when every one is admitted.
    /// </summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="unicodeSets">Whether the <c>v</c> flag is present, which admits the properties of strings.</param>
    internal static string? FirstRefused(string pattern, bool unicodeSets)
    {
        for (var i = 0; i < pattern.Length - 1; i++)
        {
            if (pattern[i] != '\\')
                continue;

            // Step onto the escaped character, so that an escaped backslash is passed over whole.
            var escape = pattern[++i];
            if (escape != 'p' && escape != 'P')
                continue;

            var close = i + 1 < pattern.Length && pattern[i + 1] == '{' ? pattern.IndexOf('}', i + 2) : -1;
            if (close < 0)
                return pattern.Substring(i - 1, 2);

            if (!IsAdmitted(pattern.AsSpan(i + 2, close - i - 2), negated: escape == 'P', unicodeSets))
                return pattern.Substring(i - 1, close - i + 2);

            i = close;
        }

        return null;
    }

    /// <summary>Whether the body of a <c>\p{…}</c> (or, negated, a <c>\P{…}</c>) is admitted.</summary>
    /// <param name="body">The text between the braces.</param>
    /// <param name="negated">Whether the escape is <c>\P</c>.</param>
    /// <param name="unicodeSets">Whether the <c>v</c> flag is present.</param>
    internal static bool IsAdmitted(ReadOnlySpan<char> body, bool negated, bool unicodeSets)
    {
        var equals = body.IndexOf('=');
        if (equals >= 0)
            return JSUnicode.Properties.TryResolve(body[..equals], body[(equals + 1)..], out _);

        // A lone name under v may be a property of strings, which \P cannot negate (an early error).
        if (unicodeSets && JSUnicode.StringProperties.TryResolve(body, out _))
            return !negated;

        return JSUnicode.Properties.TryResolve(body, out _);
    }
}
