using System;

namespace Broiler.Regex;

/// <summary>
/// The ECMAScript regular-expression flags (ECMA-262 §22.2.6 "Flags").
/// Mirrors the flag letters <c>d g i m s u v y</c>.
/// </summary>
[Flags]
public enum RegexFlags
{
    None = 0,

    /// <summary><c>d</c> — generate match indices (<c>hasIndices</c>).</summary>
    HasIndices = 1 << 0,

    /// <summary><c>g</c> — global; consult/advance <c>lastIndex</c> across calls.</summary>
    Global = 1 << 1,

    /// <summary><c>i</c> — case-insensitive matching.</summary>
    IgnoreCase = 1 << 2,

    /// <summary><c>m</c> — multiline; <c>^</c>/<c>$</c> also match at line terminators.</summary>
    Multiline = 1 << 3,

    /// <summary><c>s</c> — dotAll; <c>.</c> also matches line terminators.</summary>
    DotAll = 1 << 4,

    /// <summary><c>u</c> — Unicode mode (code-point semantics).</summary>
    Unicode = 1 << 5,

    /// <summary><c>y</c> — sticky; anchor the match at <c>lastIndex</c>.</summary>
    Sticky = 1 << 6,

    /// <summary><c>v</c> — Unicode-sets mode (implies code-point semantics + set ops).</summary>
    UnicodeSets = 1 << 7,
}

/// <summary>
/// Parses and validates an ECMAScript flags string (ECMA-262 §22.2.3.1 step 4
/// + §22.2.6). Each flag may appear at most once; <c>u</c> and <c>v</c> are
/// mutually exclusive; an unknown letter is a syntax error.
/// </summary>
public static class RegexFlagsParser
{
    public static RegexFlags Parse(string? flags)
    {
        var result = RegexFlags.None;
        if (string.IsNullOrEmpty(flags))
            return result;

        foreach (var ch in flags)
        {
            var flag = ch switch
            {
                'd' => RegexFlags.HasIndices,
                'g' => RegexFlags.Global,
                'i' => RegexFlags.IgnoreCase,
                'm' => RegexFlags.Multiline,
                's' => RegexFlags.DotAll,
                'u' => RegexFlags.Unicode,
                'y' => RegexFlags.Sticky,
                'v' => RegexFlags.UnicodeSets,
                _ => throw new RegexSyntaxException($"Invalid regular expression flags: '{ch}'"),
            };

            if ((result & flag) != 0)
                throw new RegexSyntaxException($"Duplicate regular expression flag: '{ch}'");

            result |= flag;
        }

        if ((result & RegexFlags.Unicode) != 0 && (result & RegexFlags.UnicodeSets) != 0)
            throw new RegexSyntaxException("The 'u' and 'v' regular expression flags cannot be combined");

        return result;
    }

    /// <summary>True when either <c>u</c> or <c>v</c> is set (both imply code-point semantics).</summary>
    public static bool IsUnicodeMode(this RegexFlags flags)
        => (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0;
}
