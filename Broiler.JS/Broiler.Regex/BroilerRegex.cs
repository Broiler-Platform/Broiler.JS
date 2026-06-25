using System.Collections.Generic;
using Broiler.Regex.Ast;
using Broiler.Regex.Matching;
using Broiler.Regex.Parsing;

namespace Broiler.Regex;

/// <summary>
/// A compiled ECMAScript regular expression. The public entry point of
/// Broiler.Regex: parse a pattern + flags once, then match many inputs.
/// </summary>
/// <remarks>
/// Compilation is reusable and the compiled form is immutable, so a single
/// instance may be matched concurrently from multiple threads (each match
/// attempt allocates its own state and step budget).
/// </remarks>
public sealed class BroilerRegex
{
    private readonly Matcher _matcher;

    public string Pattern { get; }
    public RegexFlags Flags { get; }

    /// <summary>Number of capturing groups in the pattern.</summary>
    public int CaptureCount { get; }

    /// <summary>Group name → 1-based capture index.</summary>
    public IReadOnlyDictionary<string, int> GroupNames { get; }

    public BroilerRegex(string pattern, string? flags = null)
        : this(pattern, RegexFlagsParser.Parse(flags))
    {
    }

    public BroilerRegex(string pattern, RegexFlags flags)
    {
        Pattern = pattern ?? "";
        Flags = flags;

        var ast = RegexParser.Parse(Pattern, flags, out var captureCount, out var groupNames);
        CaptureCount = captureCount;
        GroupNames = groupNames;
        _matcher = new Matcher(ast, captureCount, flags, groupNames);
    }

    /// <summary>
    /// Finds the first match at or after <paramref name="start"/> (anchored at
    /// exactly <paramref name="start"/> when the <c>y</c> flag is set). Returns
    /// <see cref="RegexMatch.Empty"/> when there is no match.
    /// </summary>
    public RegexMatch Match(string input, int start = 0)
    {
        if (start < 0) start = 0;
        if (start > input.Length)
            return RegexMatch.Empty;
        return _matcher.Run(input, start) ?? RegexMatch.Empty;
    }

    /// <summary>True when the pattern matches anywhere at or after <paramref name="start"/>.</summary>
    public bool IsMatch(string input, int start = 0) => Match(input, start).Success;

    /// <summary>
    /// Enumerates all non-overlapping matches, advancing past empty matches by one
    /// code point (the <c>g</c>-flag iteration of §22.2.6.9 / §22.2.6.13).
    /// </summary>
    public IEnumerable<RegexMatch> Matches(string input)
    {
        var pos = 0;
        while (pos <= input.Length)
        {
            var m = _matcher.Run(input, pos);
            if (m == null)
                yield break;

            yield return m;

            var next = m.Index + m.Length;
            if (m.Length == 0)
            {
                next = m.Index + 1;
                if (Flags.IsUnicodeMode() && m.Index < input.Length
                    && char.IsHighSurrogate(input[m.Index])
                    && m.Index + 1 < input.Length && char.IsLowSurrogate(input[m.Index + 1]))
                    next++;
            }
            pos = next;
        }
    }
}
