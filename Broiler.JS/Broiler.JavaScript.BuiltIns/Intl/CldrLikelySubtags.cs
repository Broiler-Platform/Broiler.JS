using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.BuiltIns.Intl;

// The UTS #35 §4.3 "Likely Subtags" algorithms (Add / Remove Likely Subtags) over the CLDR
// likelySubtags data, used by Intl.Locale.prototype.maximize / minimize. Operates purely on the
// language/script/region core; callers preserve variants, extensions and private-use subtags.
internal static class CldrLikelySubtags
{
    private static readonly Dictionary<string, string> Map = Parse();

    private static Dictionary<string, string> Parse()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in CldrLikelySubtagsData.Packed.Split('\n'))
        {
            if (line.Length == 0)
                continue;
            var space = line.IndexOf(' ');
            if (space < 0)
                continue;
            map[line[..space]] = line[(space + 1)..];
        }
        return map;
    }

    private static (string lang, string script, string region) SplitMaximal(string value)
    {
        var parts = value.Split('-');
        return (parts[0], parts[1], parts[2]);
    }

    // Add Likely Subtags. Returns the maximized (language, script, region) — all three present — or
    // null when no data entry matches (the spec's "error signaled" case, where the caller leaves the
    // locale unchanged). Empty inputs use "und"/none.
    public static (string lang, string? script, string? region)? Maximize(string? lang, string? script, string? region)
    {
        var l = string.IsNullOrEmpty(lang) ? "und" : lang.ToLowerInvariant();
        var s = string.IsNullOrEmpty(script) ? null : Titlecase(script);
        var r = string.IsNullOrEmpty(region) ? null : region.ToUpperInvariant();

        // ICU lookup order: language-script-region, language-script, language-region, language,
        // und-script. The first match in the data wins.
        string? match = null;
        if (s != null && r != null)
            Map.TryGetValue($"{l}-{s}-{r}", out match);
        if (match == null && s != null)
            Map.TryGetValue($"{l}-{s}", out match);
        if (match == null && r != null)
            Map.TryGetValue($"{l}-{r}", out match);
        if (match == null)
            Map.TryGetValue(l, out match);
        if (match == null && s != null)
            Map.TryGetValue($"und-{s}", out match);
        if (match == null)
            return null;

        var (ml, ms, mr) = SplitMaximal(match);
        // Present input subtags are kept; missing ones (and an "und" language) come from the match.
        return (l != "und" ? l : ml, s ?? ms, r ?? mr);
    }

    // Remove Likely Subtags. Returns the minimal (language, script, region) where script and/or
    // region may be null, or null when maximization fails.
    public static (string lang, string? script, string? region)? Minimize(string? lang, string? script, string? region)
    {
        var max = Maximize(lang, script, region);
        if (max == null)
            return null;
        var (l, s, r) = max.Value;

        // Try dropping subtags in ICU order — language only, language+region, language+script — and
        // keep the first whose maximization round-trips to max.
        if (Maximize(l, null, null) is { } t1 && t1 == max.Value)
            return (l, null, null);
        if (Maximize(l, null, r) is { } t2 && t2 == max.Value)
            return (l, null, r);
        if (Maximize(l, s, null) is { } t3 && t3 == max.Value)
            return (l, s, null);
        return (l, s, r);
    }

    private static string Titlecase(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
