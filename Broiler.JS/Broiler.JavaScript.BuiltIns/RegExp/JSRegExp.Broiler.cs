extern alias BRegex;
using System.Collections.Generic;
using BRegex::Broiler.Regex;
using BRegex::Broiler.Regex.Ast;
using BRegex::Broiler.Regex.Matching;
using NetMatch = System.Text.RegularExpressions.Match;

namespace Broiler.JavaScript.BuiltIns.RegExp;

// Broiler.Regex integration (issue #923).
//
// A handful of ECMAScript regex behaviours cannot be expressed by translating the
// pattern into a System.Text.RegularExpressions.Regex — the .NET engine's own
// semantics get them wrong no matter how the source is rewritten:
//
//   * a look-behind that contains capturing groups or back-references: .NET runs
//     the body right-to-left AND captures right-to-left, so group contents / order
//     come out reversed (issue #923 problems 3 & 4);
//   * a nullable quantifier whose body can match the empty string: ECMAScript's
//     RepeatMatcher abandons an empty iteration, .NET does not, so the match comes
//     out short (problem 8 — e.g. /(a?b??)*/ on "ab");
//   * code-point back-references and astral / lone-surrogate atoms under the `u`
//     flag, where .NET matches per UTF-16 code unit (problems 6 & 7).
//
// Broiler.Regex implements the §22.2.2 continuation-passing matcher directly, so it
// is correct by construction for exactly these cases. JSRegExp keeps the mature
// .NET translator as the default engine and routes ONLY gap-feature patterns that
// Broiler.Regex can fully handle through it (see ShouldRouteToBroiler). Everything
// else continues to use .NET unchanged, keeping the blast radius minimal.
public partial class JSRegExp
{
    // Non-null when this pattern is matched by the Broiler.Regex engine instead of
    // the .NET `value`. When set, `value` may be null (the translator could not
    // represent the pattern for .NET at all).
    internal BroilerRegex broiler;

    /// <summary>
    /// Attempts to build a <see cref="BroilerRegex"/> for <paramref name="pattern"/>
    /// when, and only when, the pattern exercises a JS/.NET semantic gap that
    /// Broiler.Regex resolves and the translator cannot. Returns null to keep using
    /// the .NET engine (the pattern is either not a gap case, or uses a Broiler
    /// feature that is not yet implemented — property escapes, v-mode set ops).
    /// </summary>
    internal static BroilerRegex TryBuildBroilerForGaps(string pattern, string flags, bool unicodeMode)
    {
        pattern ??= "";

        RegexFlags brFlags;
        RegexNode ast;
        try
        {
            brFlags = RegexFlagsParser.Parse(flags);
            // Re-parse with Broiler so the routing decision sees the real grammar
            // (not a textual heuristic). A parse failure here — including the
            // not-yet-implemented \p{…} stub — means Broiler can't own the pattern,
            // so fall back to the .NET translator.
            ast = BRegex::Broiler.Regex.Parsing.RegexParser.Parse(pattern, brFlags, out _, out _);
        }
        catch
        {
            return null;
        }

        var scan = new GapScan(unicodeMode);
        scan.Walk(ast, insideLookbehind: false);

        if (scan.UsesUnsupported || !scan.HasGap)
            return null;

        try
        {
            return new BroilerRegex(pattern, brFlags);
        }
        catch
        {
            return null;
        }
    }

    // Walks a Broiler.Regex AST to decide whether the pattern is a gap case worth
    // routing (HasGap) and whether it uses a feature Broiler.Regex cannot yet match
    // correctly (UsesUnsupported — in which case routing is abandoned).
    private sealed class GapScan(bool unicodeMode)
    {
        private readonly bool _unicode = unicodeMode;

        public bool HasGap;
        public bool UsesUnsupported;

        public void Walk(RegexNode node, bool insideLookbehind)
        {
            switch (node)
            {
                case CharNode ch:
                    // Astral or lone-surrogate atom under `u`: .NET matches per code
                    // unit, Broiler per code point (problem 7).
                    if (_unicode && (ch.CodePoint > 0xFFFF || (ch.CodePoint >= 0xD800 && ch.CodePoint <= 0xDFFF)))
                        HasGap = true;
                    break;

                case BackreferenceNode:
                    // A back-reference inside a look-behind, or any back-reference in
                    // Unicode mode, is a documented gap (problems 3, 4, 6).
                    if (insideLookbehind || _unicode)
                        HasGap = true;
                    break;

                case CharClassNode cls:
                    // v-mode set operations / \q{…} are parsed but not yet evaluated
                    // by the matcher — never route such a pattern.
                    if (cls.Set.UsesSetOperations)
                        UsesUnsupported = true;
                    break;

                case GroupNode grp:
                    if (insideLookbehind && grp.IsCapturing)
                        HasGap = true; // capturing group inside a look-behind (problems 3, 4)
                    Walk(grp.Child, insideLookbehind);
                    break;

                case ModifierGroupNode mod:
                    Walk(mod.Child, insideLookbehind);
                    break;

                case QuantifierNode q:
                    // Nullable quantifier divergence (problem 8): a repeat that runs
                    // two or more times over a body that can match BOTH empty and
                    // non-empty. ECMAScript drops the empty iteration, .NET keeps it.
                    if ((q.Max == QuantifierNode.Unbounded || q.Max > 1)
                        && CanMatchEmpty(q.Child) && CanMatchNonEmpty(q.Child))
                        HasGap = true;
                    Walk(q.Child, insideLookbehind);
                    break;

                case SequenceNode seq:
                    foreach (var t in seq.Terms)
                        Walk(t, insideLookbehind);
                    break;

                case DisjunctionNode dis:
                    foreach (var alt in dis.Alternatives)
                        Walk(alt, insideLookbehind);
                    break;

                case LookaroundNode la:
                    Walk(la.Child, insideLookbehind || la.Behind);
                    break;
            }
        }

        private static bool CanMatchEmpty(RegexNode node) => node switch
        {
            EmptyNode => true,
            CharNode => false,
            AnyCharNode => false,
            CharClassNode => false,
            AnchorNode => true,
            LookaroundNode => true,
            BackreferenceNode => true, // an unset back-reference matches the empty string
            GroupNode g => CanMatchEmpty(g.Child),
            ModifierGroupNode m => CanMatchEmpty(m.Child),
            QuantifierNode q => q.Min == 0 || CanMatchEmpty(q.Child),
            SequenceNode s => AllEmpty(s.Terms),
            DisjunctionNode d => AnyEmpty(d.Alternatives),
            _ => true,
        };

        private static bool CanMatchNonEmpty(RegexNode node) => node switch
        {
            EmptyNode => false,
            CharNode => true,
            AnyCharNode => true,
            CharClassNode => true,
            AnchorNode => false,
            LookaroundNode => false,
            BackreferenceNode => true,
            GroupNode g => CanMatchNonEmpty(g.Child),
            ModifierGroupNode m => CanMatchNonEmpty(m.Child),
            QuantifierNode q => q.Max != 0 && CanMatchNonEmpty(q.Child),
            SequenceNode s => AnyNonEmpty(s.Terms),
            DisjunctionNode d => AnyNonEmpty(d.Alternatives),
            _ => true,
        };

        private static bool AllEmpty(IReadOnlyList<RegexNode> ts)
        {
            foreach (var t in ts) if (!CanMatchEmpty(t)) return false;
            return true;
        }

        private static bool AnyEmpty(IReadOnlyList<RegexNode> ts)
        {
            foreach (var t in ts) if (CanMatchEmpty(t)) return true;
            return false;
        }

        private static bool AnyNonEmpty(IReadOnlyList<RegexNode> ts)
        {
            foreach (var t in ts) if (CanMatchNonEmpty(t)) return true;
            return false;
        }
    }

    /// <summary>
    /// Builds the <see cref="CaptureGroupMap"/> for a Broiler-engine pattern. Broiler
    /// already numbers captures in ECMAScript (source) order and keeps named groups
    /// native, so the map just records the count and the name→index pairs the exec
    /// result builder needs.
    /// </summary>
    internal static CaptureGroupMap BuildCaptureMapFromBroiler(BroilerRegex br)
    {
        var count = br.CaptureCount;
        var originalName = new string[count + 1]; // [0] = whole match placeholder

        var indexToName = new Dictionary<int, string>();
        foreach (var kv in br.GroupNames)
            indexToName[kv.Value] = kv.Key;

        var named = new List<(string, List<int>)>();
        for (var i = 1; i <= count; i++)
        {
            if (indexToName.TryGetValue(i, out var name))
            {
                originalName[i] = name;
                named.Add((name, new List<int> { i }));
            }
        }

        return new CaptureGroupMap(originalName, named);
    }

    // ----- Unified match result -------------------------------------------------
    //
    // A normalized view over either a Broiler.Regex RegexMatch or a .NET Match, so
    // the exec/match code path is identical for both engines. Groups[0] is the whole
    // match; Groups[1..] are the captures in ECMAScript (source) order.

    internal readonly struct RegexCapture(bool success, int index, int length, string value)
    {
        public readonly bool Success = success;
        public readonly int Index = index;
        public readonly int Length = length;
        public readonly string Value = value; // null when !Success
    }

    internal sealed class RegexMatchData
    {
        public bool Success;
        public int Index;
        public int Length;
        public string Value;
        public RegexCapture[] Groups;

        public static readonly RegexMatchData NoMatch = new() { Success = false };
    }

    /// <summary>Runs a single match at <paramref name="start"/> via the active engine.</summary>
    internal RegexMatchData RunMatch(string input, int start)
        => broiler != null
            ? FromBroiler(broiler.Match(input, start))
            : FromNet(value.Match(input, start));

    private static RegexMatchData FromBroiler(RegexMatch m)
    {
        if (!m.Success)
            return RegexMatchData.NoMatch;

        var groups = new RegexCapture[m.Groups.Count];
        for (var i = 0; i < m.Groups.Count; i++)
        {
            var g = m.Groups[i];
            groups[i] = new RegexCapture(g.Success, g.Index, g.Length, g.Success ? g.Value : null);
        }

        return new RegexMatchData
        {
            Success = true,
            Index = m.Index,
            Length = m.Length,
            Value = m.Value,
            Groups = groups,
        };
    }

    private RegexMatchData FromNet(NetMatch m)
    {
        if (!m.Success)
            return RegexMatchData.NoMatch;

        var groups = m.Groups;
        // When the pattern has named groups every capture was renamed to a synthetic,
        // source-ordered name, so .NET numbers them 1..n in ECMAScript order and the
        // captureMap supplies the count; otherwise the group collection is already in
        // order.
        var c = captureMap != null ? captureMap.Count + 1 : groups.Count;
        var arr = new RegexCapture[c];
        for (var i = 0; i < c; i++)
        {
            var g = groups[i];
            arr[i] = new RegexCapture(g.Success, g.Index, g.Length, g.Success ? g.Value : null);
        }

        return new RegexMatchData
        {
            Success = true,
            Index = m.Index,
            Length = m.Length,
            Value = m.Value,
            Groups = arr,
        };
    }
}
