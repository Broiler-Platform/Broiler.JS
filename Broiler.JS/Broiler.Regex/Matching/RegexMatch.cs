using System.Collections.Generic;

namespace Broiler.Regex.Matching;

/// <summary>
/// A single captured group within a <see cref="RegexMatch"/>. Mirrors the shape
/// the JavaScript layer needs (<c>Success</c>, <c>Value</c>, <c>Index</c>,
/// <c>Length</c>), so it can stand in for a <c>System.Text.RegularExpressions.Group</c>.
/// </summary>
public sealed class RegexGroup
{
    public bool Success { get; }
    public int Index { get; }
    public int Length { get; }
    public string Value { get; }
    public string? Name { get; }

    internal RegexGroup(bool success, int index, int length, string value, string? name)
    {
        Success = success;
        Index = index;
        Length = length;
        Value = value;
        Name = name;
    }

    internal static RegexGroup Unmatched(string? name) => new(false, -1, 0, "", name);
}

/// <summary>
/// The result of a match attempt. <see cref="Success"/> is false for no match.
/// <c>Groups[0]</c> is the whole match; <c>Groups[1..]</c> are the capturing
/// groups in ECMAScript (source) order.
/// </summary>
public sealed class RegexMatch
{
    public bool Success { get; }
    public int Index { get; }
    public int Length { get; }
    public string Value { get; }
    public IReadOnlyList<RegexGroup> Groups { get; }
    public IReadOnlyDictionary<string, RegexGroup> NamedGroups { get; }

    public static readonly RegexMatch Empty =
        new(false, -1, 0, "", new RegexGroup[0], new Dictionary<string, RegexGroup>());

    internal RegexMatch(bool success, int index, int length, string value,
        IReadOnlyList<RegexGroup> groups, IReadOnlyDictionary<string, RegexGroup> namedGroups)
    {
        Success = success;
        Index = index;
        Length = length;
        Value = value;
        Groups = groups;
        NamedGroups = namedGroups;
    }
}
