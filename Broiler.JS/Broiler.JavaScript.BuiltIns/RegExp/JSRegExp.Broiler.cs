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
//     flag, where .NET matches per UTF-16 code unit (problems 6 & 7);
//   * a v-mode class-set expression (`[a&&b]`, `[a--b]`, `[[a][b]]`, `\q{…}`) or a
//     property escape used as a class member: the translator has to evaluate the set
//     itself and emit an approximation, and inside `[…]` it can only fall back to
//     .NET's code-unit-based `\p{Lu}`.
//
// Broiler.Regex implements the §22.2.2 continuation-passing matcher directly, so it is
// correct by construction for these cases and, more broadly, for the whole Unicode-mode
// grammar. JSRegExp now routes every u/v pattern Broiler can build, plus the non-Unicode
// gap shapes above; non-Unicode patterns Broiler does not specifically own stay on the
// mature — and much faster — .NET translator (see TryRouteToBroiler). Everything routed
// dispatches through RunMatch, so exec/test/split/replace/IsMatch share one engine.
public partial class JSRegExp
{
    // Non-null when this pattern is matched by the Broiler.Regex engine instead of
    // the .NET `value`. When set, `value` may be null (the translator could not
    // represent the pattern for .NET at all).
    internal BroilerRegex broiler;

    /// <summary>
    /// Attempts to build a <see cref="BroilerRegex"/> for <paramref name="pattern"/>
    /// when, and only when, the pattern exercises a JS/.NET semantic gap that
    /// Broiler.Regex should own. Returns null to keep using the .NET engine, either because
    /// the pattern is not selected for routing or because Broiler.Regex cannot parse or build
    /// it (in which case the .NET translator, or a SyntaxError, takes over as before).
    /// </summary>
    /// <remarks>
    /// The routing policy differs by mode:
    /// <list type="bullet">
    /// <item><b>Unicode (<c>u</c>/<c>v</c>)</b>: route every pattern Broiler can build. This is
    /// where the .NET translation is most complex and fragile — a standalone <c>\p{…}</c> under
    /// <c>i</c> throws, in-class case folding (<c>[α-ω]/iu</c> against <c>µ</c>) is missed, and
    /// astral atoms match per code unit — and where Broiler's code-point-native matching is
    /// correct by construction. When Broiler cannot build a Unicode pattern the .NET path still
    /// runs, so its own parser gaps never turn a valid pattern into a failure.</item>
    /// <item><b>non-Unicode</b>: route only the documented gap shapes. The common non-Unicode
    /// pattern is often ASCII and often in a hot loop, and the mature .NET engine is far faster
    /// than this clarity-first interpreter, so it stays the default until Broiler is optimized.
    /// </item>
    /// </list>
    /// </remarks>
    internal static BroilerRegex TryRouteToBroiler(string pattern, string flags, bool unicodeMode)
    {
        pattern ??= "";

        RegexFlags brFlags;
        RegexNode ast;
        try
        {
            brFlags = RegexFlagsParser.Parse(flags);
            // Re-parse with Broiler so the routing decision sees the real grammar
            // (not a textual heuristic). A parse failure here means Broiler can't own
            // the pattern, so fall back to the .NET translator.
            ast = BRegex::Broiler.Regex.Parsing.RegexParser.Parse(pattern, brFlags, out _, out _);
        }
        catch
        {
            return null;
        }

        // Outside Unicode mode, keep the .NET default and route only the gap shapes.
        if (!unicodeMode)
        {
            var scan = new GapScan(unicodeMode);
            scan.Walk(ast, insideLookbehind: false);
            if (!scan.HasGap)
                return null;
        }

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
    // routing (HasGap).
    private sealed class GapScan(bool unicodeMode)
    {
        private readonly bool _unicode = unicodeMode;

        public bool HasGap;

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
                    // A v-mode class-set expression (`[a&&b]`, `[a--b]`, `[[a][b]]`,
                    // `\q{…}`) and a property escape used as a class member are both
                    // shapes the translator cannot hand to .NET as written: it evaluates
                    // the set itself and emits an approximation, and inside `[…]` it falls
                    // back to .NET's code-unit-based `\p{Lu}`, which cannot match a
                    // supplementary-plane member. Broiler.Regex evaluates both directly.
                    if (cls.Set.UsesSetOperations || cls.Set.UsesPropertyEscape)
                        HasGap = true;
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
            // A v-mode class can hold the empty string (`[\q{}]`), which is the one
            // character class that matches without advancing.
            CharClassNode c => c.Set.MatchesEmptyString,
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
    {
        if (broiler != null)
        {
            try
            {
                return FromBroiler(broiler.Match(input, start));
            }
            catch (BRegex::Broiler.Regex.RegexOverflowException)
            {
                // Broiler's continuation-passing matcher would recurse past the stack for
                // this subject (a quantifier over a long input). The .NET engine is
                // iterative, so fall back to it for this match when the translator could
                // represent the pattern. When it could not (value is null — a pattern only
                // Broiler can express), there is nothing to fall back to; report no match
                // rather than crash. Both are rare: the widened routing pairs Broiler with a
                // .NET translation for almost every pattern.
                if (value != null)
                    return FromNet(value.Match(input, start));
                return RegexMatchData.NoMatch;
            }
        }

        // Phase 5, item 2: the pattern's thousandth match is where it becomes worth asking
        // whether `RegexOptions.Compiled` would serve it better, and the only honest way to ask
        // is to run both on the subject in hand. The countdown is a plain field decrement on a
        // path that already costs hundreds of nanoseconds, and it is zero — never taken — unless
        // RegexTiering is switched on. See RegexTiering for why there is no predicate.
        if (tierCountdown > 0 && --tierCountdown == 0)
            value = RegexTiering.Decide(value, input, start);

        return FromNet(value.Match(input, start));
    }

    /// <summary>
    /// Enumerates the non-overlapping matches of this pattern in <paramref name="input"/>
    /// through the active engine, advancing past an empty match by one code point (a
    /// surrogate pair under <c>u</c>/<c>v</c>) as §22.2.6.9 iteration requires. This is the
    /// engine-agnostic equivalent of a .NET <c>Match</c>/<c>NextMatch</c> loop, so a
    /// Broiler-routed pattern drives <see cref="Split"/> and <see cref="Replace(string,string)"/>
    /// with the same match data <c>exec</c> sees rather than the .NET translator's — which
    /// is wrong for exactly the gap patterns that get routed.
    /// </summary>
    internal IEnumerable<RegexMatchData> EnumerateMatches(string input)
    {
        var pos = 0;
        while (pos <= input.Length)
        {
            var match = RunMatch(input, pos);
            if (!match.Success)
                yield break;

            yield return match;

            var next = match.Index + match.Length;
            if (match.Length == 0)
            {
                // Advance one position past an empty match so iteration terminates, keeping
                // a surrogate pair whole under u/v (the same rule BroilerRegex.Matches uses).
                next = match.Index + 1;
                if ((unicode || unicodeSets) && match.Index < input.Length
                    && char.IsHighSurrogate(input[match.Index])
                    && match.Index + 1 < input.Length && char.IsLowSurrogate(input[match.Index + 1]))
                    next++;
            }
            pos = next;
        }
    }

    /// <summary>
    /// True when this pattern matches anywhere in <paramref name="input"/>, through the
    /// active engine. The engine-agnostic replacement for reaching a caller's own
    /// <c>System.Text.RegularExpressions.Regex.IsMatch</c> off the retired
    /// <c>IJSRegExp.Value</c>, so a routed pattern answers a match test the same way
    /// <c>exec</c> does.
    /// </summary>
    internal bool IsMatch(string input) => RunMatch(input, 0).Success;

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
