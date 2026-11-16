using System.Collections.Generic;
using Broiler.Regex;
using Broiler.Regex.Text;
using Broiler.JavaScript.Runtime.Unicode;
using NetMatch = System.Text.RegularExpressions.Match;
using Broiler.ECMAScript.Text;

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

        // Broiler.Regex 0.1.0-preview.6's flags reader answers a refusal as a status, where
        // RegexFlagsParser.Parse, internal since that version, threw it; the flags it accepts
        // are the same.
        if (RegexFlagsText.Parse(flags, out var brFlags, out _) != RegexStatus.Success)
            return null;

        // Outside Unicode mode, keep the .NET default and route only the gap shapes. The scan
        // reads the pattern as Broiler.Regex's parser does (GapScan), since the parser and its
        // tree are internal from 0.1.0-preview.6; a pattern it cannot read is one Broiler
        // refuses, so the .NET translator, or a SyntaxError, takes over as before.
        if (!unicodeMode && !GapScan.HasGap(pattern))
            return null;

        // The constructor parses the pattern as the routing's parse did before 0.1.0-preview.6,
        // and refuses (throws) where that parse refused, so a pattern Broiler cannot own
        // still falls back to the .NET translator.
        try
        {
            return new BroilerRegex(pattern, brFlags, JSUnicode.Tables);
        }
        catch
        {
            return null;
        }
    }

    // Decides whether a pattern outside Unicode mode is a gap case worth routing.
    //
    // Until Broiler.Regex 0.1.0-preview.6 this walked the package's public syntax tree, which that
    // version made internal (its task 2.9: "the 29 breaks"). It now reads the pattern text by the
    // grammar the package's RegexParser reads outside Unicode mode (ECMA-262 22.2.1 with Annex B's
    // B.1.2), consuming exactly what that parser consumes, and answers the walk's own questions of each
    // term: whether it can match the empty string, whether it can match a non-empty one, and whether it
    // is a capturing group or a back-reference inside a look-behind. The walk's Unicode-mode cases (an
    // astral or lone-surrogate atom, any back-reference, a class with set operations or a property
    // escape) never applied: TryRouteToBroiler scans only outside Unicode mode, where the parser builds
    // none of them. A stopgap until SJL-11.5, whose deletion list names GapScan and TryRouteToBroiler.
    private sealed class GapScan
    {
        private readonly string _src;
        private int _pos;
        private int _depth = 1;
        private int _totalCaptureGroups;
        private bool _hasGroupNames;
        private bool _hasGap;

        private GapScan(string pattern) => _src = pattern;

        // A pattern the parser would refuse: the scan stops, and the pattern is not routed.
        private sealed class Refused : System.Exception
        {
        }

        // What the walk asked of a node: CanMatchEmpty and CanMatchNonEmpty.
        private readonly record struct Shape(bool Empty, bool NonEmpty);

        private static readonly Shape Character = new(false, true);
        private static readonly Shape Assertion = new(true, false);
        private static readonly Shape Backreference = new(true, true);

        /// <summary>
        /// True when <paramref name="pattern"/>, read outside Unicode mode, has a gap shape: a capturing
        /// group or a back-reference inside a look-behind (problems 3 and 4), or a quantifier whose maximum
        /// is above one over a body that can match both the empty string and a non-empty one (problem 8).
        /// False also for a pattern Broiler.Regex's parser refuses.
        /// </summary>
        public static bool HasGap(string pattern)
        {
            var scan = new GapScan(pattern);
            try
            {
                scan.PreScan();
                scan.Disjunction(insideLookbehind: false);
                if (scan._pos != scan._src.Length)
                    return false;
            }
            catch (Refused)
            {
                return false;
            }

            return scan._hasGap;
        }

        // The parser's pre-scan, outside v: the total number of capturing groups, which decides whether
        // \n is a back-reference, and whether any group is named, which decides whether \k is one.
        private void PreScan()
        {
            var inClass = false;
            for (var i = 0; i < _src.Length; i++)
            {
                var c = _src[i];
                if (c == '\\')
                {
                    i++;
                    continue;
                }
                if (c == '[') { inClass = true; continue; }
                if (c == ']') { inClass = false; continue; }
                if (inClass || c != '(')
                    continue;

                if (i + 1 < _src.Length && _src[i + 1] == '?')
                {
                    if (i + 3 < _src.Length && _src[i + 2] == '<' && _src[i + 3] != '=' && _src[i + 3] != '!')
                    {
                        _totalCaptureGroups++;
                        _hasGroupNames = true;
                    }
                    continue;
                }

                _totalCaptureGroups++;
            }
        }

        private Shape Disjunction(bool insideLookbehind)
        {
            var shape = Alternative(insideLookbehind);
            while (Peek() == '|')
            {
                _pos++;
                var next = Alternative(insideLookbehind);
                shape = new Shape(shape.Empty || next.Empty, shape.NonEmpty || next.NonEmpty);
            }
            return shape;
        }

        // An alternative of no term is the empty node; of one, that term; else a sequence, which can
        // match empty when every term can, and non-empty when any term can.
        private Shape Alternative(bool insideLookbehind)
        {
            bool empty = true, nonEmpty = false;
            while (!AtEnd && Peek() is not ('|' or ')'))
            {
                var term = Term(insideLookbehind);
                empty &= term.Empty;
                nonEmpty |= term.NonEmpty;
            }
            return new Shape(empty, nonEmpty);
        }

        private Shape Term(bool insideLookbehind)
        {
            if (TryAssertion(insideLookbehind, out var assertion, out var quantifiable))
            {
                if (!HasQuantifier())
                    return assertion;
                // Annex B lets a look-ahead, and no other assertion, carry a quantifier.
                if (!quantifiable)
                    throw new Refused();
                return Quantified(assertion);
            }

            return Quantified(Atom(insideLookbehind));
        }

        private bool TryAssertion(bool insideLookbehind, out Shape shape, out bool quantifiable)
        {
            shape = Assertion;
            quantifiable = false;
            var c = Peek();
            if (c is '^' or '$')
            {
                _pos++;
                return true;
            }
            if (c == '\\' && PeekAt(1) is 'b' or 'B')
            {
                _pos += 2;
                return true;
            }
            if (c == '(' && PeekAt(1) == '?')
            {
                if (PeekAt(2) is '=' or '!')
                {
                    _pos += 3;
                    Group(insideLookbehind);
                    quantifiable = true;
                    return true;
                }
                if (PeekAt(2) == '<' && PeekAt(3) is '=' or '!')
                {
                    _pos += 4;
                    Group(insideLookbehind: true);
                    return true;
                }
            }
            return false;
        }

        private bool HasQuantifier()
        {
            if (Peek() is '*' or '+' or '?')
                return true;
            if (Peek() != '{')
                return false;
            var save = _pos;
            var found = TryBraceQuantifier(out _, out _);
            _pos = save;
            return found;
        }

        // The walk's quantifier case: a gap when the maximum is above one (or unbounded) and the body
        // can match both empty and non-empty; the quantified term can match empty when its minimum is
        // zero or its body can, and non-empty when its maximum is not zero and its body can.
        private Shape Quantified(Shape body)
        {
            int min, max;
            switch (Peek())
            {
                case '*': min = 0; max = -1; _pos++; break;
                case '+': min = 1; max = -1; _pos++; break;
                case '?': min = 0; max = 1; _pos++; break;
                case '{':
                    if (!TryBraceQuantifier(out min, out max))
                        return body;
                    break;
                default:
                    return body;
            }
            if (Peek() == '?')
                _pos++;

            if ((max == -1 || max > 1) && body.Empty && body.NonEmpty)
                _hasGap = true;
            return new Shape(min == 0 || body.Empty, max != 0 && body.NonEmpty);
        }

        // {n}, {n,} or {n,m}, the counts saturating at int.MaxValue, -1 for no maximum.
        private bool TryBraceQuantifier(out int min, out int max)
        {
            max = 0;
            var save = _pos;
            _pos++;
            if (!TryDecimal(out min))
            {
                _pos = save;
                return false;
            }
            max = min;
            if (Peek() == ',')
            {
                _pos++;
                if (Peek() == '}')
                    max = -1;
                else if (!TryDecimal(out max))
                {
                    _pos = save;
                    return false;
                }
            }
            if (Peek() != '}')
            {
                _pos = save;
                return false;
            }
            _pos++;
            if (max != -1 && max < min)
                throw new Refused();
            return true;
        }

        private Shape Atom(bool insideLookbehind)
        {
            var c = Peek();
            switch (c)
            {
                case '.':
                    _pos++;
                    return Character;
                case '(':
                    return GroupAtom(insideLookbehind);
                case '[':
                    // Outside v no class holds the empty string.
                    Class();
                    return Character;
                case '\\':
                    return AtomEscape(insideLookbehind);
                case '*' or '+' or '?':
                    throw new Refused();
                case '{' when HasQuantifier():
                    throw new Refused();
            }

            // A pattern character, ']', '{' and '}' among them under Annex B: one code unit.
            _pos++;
            return Character;
        }

        private Shape GroupAtom(bool insideLookbehind)
        {
            _pos++;
            if (Peek() != '?')
            {
                if (insideLookbehind)
                    _hasGap = true;
                return Group(insideLookbehind);
            }

            var second = PeekAt(1);
            if (second == ':')
            {
                _pos += 2;
                return Group(insideLookbehind);
            }
            if (second == '<' && PeekAt(2) is not ('=' or '!'))
            {
                _pos += 2;
                SkipGroupName();
                if (insideLookbehind)
                    _hasGap = true;
                return Group(insideLookbehind);
            }
            if (second is 'i' or 'm' or 's' or '-')
            {
                _pos++;
                SkipModifierFlags();
                if (Peek() == '-')
                {
                    _pos++;
                    SkipModifierFlags();
                }
                Expect(':');
                return Group(insideLookbehind);
            }
            throw new Refused();
        }

        // A group's body and its ')', descending as the parser does: the pattern's own disjunction is
        // depth 1 and each group adds 1, up to Broiler.Regex's nesting limit, which the routing's parse
        // and the constructor take.
        private Shape Group(bool insideLookbehind)
        {
            if (_depth >= BroilerRegex.MaximumNestingLimit)
                throw new Refused();
            _depth++;
            var body = Disjunction(insideLookbehind);
            Expect(')');
            _depth--;
            return body;
        }

        // A GroupName's '<' already read: its RegExpIdentifierName, which holds no '>', and the '>'.
        private void SkipGroupName()
        {
            while (!AtEnd && Peek() != '>')
                _pos++;
            if (AtEnd)
                throw new Refused();
            _pos++;
        }

        private void SkipModifierFlags()
        {
            while (Peek() is 'i' or 'm' or 's')
                _pos++;
        }

        private Shape AtomEscape(bool insideLookbehind)
        {
            _pos++;
            var c = Peek();

            // \1 to \9: a back-reference when its number is a group's, else a legacy octal or identity
            // escape.
            if (c is >= '1' and <= '9')
            {
                var start = _pos;
                TryDecimal(out var number);
                if (number <= _totalCaptureGroups)
                {
                    if (insideLookbehind)
                        _hasGap = true;
                    return Backreference;
                }
                _pos = start;
                LegacyOctalOrIdentity();
                return Character;
            }

            // \k<name> is a back-reference only when the pattern names a group; else the letter k.
            if (c == 'k' && _hasGroupNames)
            {
                _pos++;
                if (Peek() != '<')
                    throw new Refused();
                _pos++;
                SkipGroupName();
                if (insideLookbehind)
                    _hasGap = true;
                return Backreference;
            }

            // \d \D \w \W \s \S, or a character escape.
            if (c is 'd' or 'D' or 'w' or 'W' or 's' or 'S')
                _pos++;
            else
                CharacterEscape();
            return Character;
        }

        // A CharacterEscape outside Unicode mode, its '\' already read.
        private void CharacterEscape()
        {
            if (AtEnd)
                throw new Refused();
            var c = Peek();
            switch (c)
            {
                case 'n' or 'r' or 't' or 'f' or 'v':
                    _pos++;
                    return;
                case '0' when !char.IsAsciiDigit(PeekAt(1)):
                    _pos++;
                    return;
                case 'x' when HasHexDigits(1, 2):
                    _pos += 3;
                    return;
                case 'u' when HasHexDigits(1, 4):
                    _pos += 5;
                    return;
                case 'c':
                    // \c and a letter; else the backslash alone, and the c is read next.
                    if (char.IsAsciiLetter(PeekAt(1)))
                        _pos += 2;
                    return;
            }
            if (c is >= '0' and <= '7')
            {
                LegacyOctalOrIdentity();
                return;
            }
            _pos++;
        }

        // Up to three octal digits whose value is at most 0xFF; else one code unit.
        private void LegacyOctalOrIdentity()
        {
            if (Peek() is not (>= '0' and <= '7'))
            {
                _pos++;
                return;
            }
            var value = 0;
            for (var count = 0; count < 3 && Peek() is >= '0' and <= '7'; count++)
            {
                var next = value * 8 + (Peek() - '0');
                if (next > 0xFF)
                    break;
                value = next;
                _pos++;
            }
        }

        // A class outside v, '[' at the cursor: its atoms up to the first ']' no escape consumes.
        private void Class()
        {
            _pos++;
            if (Peek() == '^')
                _pos++;
            while (true)
            {
                if (AtEnd)
                    throw new Refused();
                if (Peek() == ']')
                {
                    _pos++;
                    return;
                }
                ClassAtom();
                if (Peek() == '-' && _pos + 1 < _src.Length && PeekAt(1) != ']')
                {
                    _pos++;
                    ClassAtom();
                }
            }
        }

        private void ClassAtom()
        {
            if (Peek() != '\\')
            {
                _pos++;
                return;
            }
            _pos++;
            var c = Peek();
            if (c is 'd' or 'D' or 'w' or 'W' or 's' or 'S' or 'b' or '-')
                _pos++;
            else if (c == 'c' && (char.IsAsciiDigit(PeekAt(1)) || PeekAt(1) == '_'))
                _pos += 2;
            else
                CharacterEscape();
        }

        private bool TryDecimal(out int value)
        {
            value = 0;
            if (!char.IsAsciiDigit(Peek()))
                return false;
            long accumulated = 0;
            while (char.IsAsciiDigit(Peek()))
            {
                accumulated = System.Math.Min(accumulated * 10 + (Peek() - '0'), int.MaxValue);
                _pos++;
            }
            value = (int)accumulated;
            return true;
        }

        private bool HasHexDigits(int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!char.IsAsciiHexDigit(PeekAt(offset + i)))
                    return false;
            }
            return true;
        }

        private void Expect(char c)
        {
            if (Peek() != c)
                throw new Refused();
            _pos++;
        }

        private bool AtEnd => _pos >= _src.Length;

        private char Peek() => _pos < _src.Length ? _src[_pos] : '\0';

        private char PeekAt(int offset) => _pos + offset < _src.Length ? _src[_pos + offset] : '\0';
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

        for (var i = 1; i <= count; i++)
            originalName[i] = br.GroupNames.NameOfGroup(i);

        var named = new List<(string, List<int>)>();
        for (var i = 0; i < br.GroupNames.Count; i++)
        {
            var name = br.GroupNames.NameAt(i);
            var indices = new List<int>();
            foreach (var group in br.GroupNames.GroupsNamed(name))
                indices.Add(group);
            named.Add((name, indices));
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
            var match = broiler.Match(input, start);
            if (match.Limit == RegexLimit.None)
                return FromBroiler(match);

            // The iterative matcher reports resource ceilings as data. Preserve the
            // existing fallback when the .NET translator can represent this pattern.
            if (value != null)
                return FromNet(value.Match(input, start));
            return RegexMatchData.NoMatch;
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
                next = (int)Utf16.AdvanceStringIndex(input, match.Index, unicode || unicodeSets);
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

        var groups = new RegexCapture[m.Groups.Length];
        for (var i = 0; i < m.Groups.Length; i++)
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
