using System;
using System.Collections.Generic;
using Broiler.Regex.Ast;
using Broiler.Regex.Unicode;

namespace Broiler.Regex.Matching;

/// <summary>
/// Compiles a <see cref="RegexNode"/> tree to the ECMAScript matcher abstraction
/// (ECMA-262 §22.2.2) and runs it. A <see cref="Matcher"/> is a
/// <c>(State, Continuation) → State?</c> function; backtracking is expressed by
/// trying a continuation and, on failure (<c>null</c>), trying the next branch.
///
/// This is where the JS/.NET semantic gaps are resolved by construction:
/// <list type="bullet">
/// <item>look-behind compiles its body with <see cref="Direction"/> = −1, matching
/// backward while capturing in source order;</item>
/// <item><see cref="CompileQuantifier"/> implements RepeatMatcher's empty-iteration
/// guard;</item>
/// <item>atoms and back-references are code-point aware under <c>u</c>/<c>v</c>.</item>
/// </list>
/// </summary>
internal sealed class Matcher
{
    private delegate MatchState? Continuation(MatchState state);
    private delegate MatchState? CompiledMatcher(MatchState state, Continuation cont);

    /// <summary>Matching direction: +1 forward, −1 backward (inside a look-behind).</summary>
    private enum Direction { Forward = 1, Backward = -1 }

    private readonly CompiledMatcher _root;
    private readonly int _captureCount;
    private readonly bool _unicode;
    private readonly bool _sticky;
    private readonly IReadOnlyDictionary<string, int> _groupNames;
    private const long StepLimit = 10_000_000;

    /// <summary>Effective i/m/s flags at a point in compilation (mutated by modifier groups).</summary>
    private readonly struct Flags
    {
        public readonly bool IgnoreCase;
        public readonly bool Multiline;
        public readonly bool DotAll;
        public Flags(bool ignoreCase, bool multiline, bool dotAll)
        {
            IgnoreCase = ignoreCase;
            Multiline = multiline;
            DotAll = dotAll;
        }
        public Flags With(RegexFlags add, RegexFlags remove) => new(
            (IgnoreCase || (add & RegexFlags.IgnoreCase) != 0) && (remove & RegexFlags.IgnoreCase) == 0,
            (Multiline || (add & RegexFlags.Multiline) != 0) && (remove & RegexFlags.Multiline) == 0,
            (DotAll || (add & RegexFlags.DotAll) != 0) && (remove & RegexFlags.DotAll) == 0);
    }

    public Matcher(RegexNode root, int captureCount, RegexFlags flags,
        IReadOnlyDictionary<string, int> groupNames)
    {
        _captureCount = captureCount;
        _unicode = flags.IsUnicodeMode();
        _sticky = (flags & RegexFlags.Sticky) != 0;
        _groupNames = groupNames;

        var initial = new Flags(
            (flags & RegexFlags.IgnoreCase) != 0,
            (flags & RegexFlags.Multiline) != 0,
            (flags & RegexFlags.DotAll) != 0);
        _root = Compile(root, Direction.Forward, initial);
    }

    /// <summary>
    /// Attempts a match in <paramref name="input"/> at or after
    /// <paramref name="start"/>. Returns null on no match. (Sticky anchors at
    /// exactly <paramref name="start"/>.)
    /// </summary>
    public RegexMatch? Run(string input, int start)
    {
        for (var at = start; at <= input.Length; at++)
        {
            var budget = new Budget(StepLimit);
            var captures = NewCaptures();
            var state = new MatchState(input, at, captures, budget);
            var result = _root(state, s => s);
            if (result != null)
                return BuildMatch(input, at, result);

            if (_sticky)
                break;

            // Advance by a whole code point in Unicode mode so the next start does
            // not land between the halves of a surrogate pair.
            if (_unicode && at < input.Length && char.IsHighSurrogate(input[at])
                && at + 1 < input.Length && char.IsLowSurrogate(input[at + 1]))
                at++;
        }

        return null;
    }

    private int[] NewCaptures()
    {
        var captures = new int[2 * (_captureCount + 1)];
        Array.Fill(captures, -1);
        return captures;
    }

    private RegexMatch BuildMatch(string input, int start, MatchState end)
    {
        var matchEnd = end.Position;
        var value = input.Substring(start, matchEnd - start);

        var groups = new RegexGroup[_captureCount + 1];
        groups[0] = new RegexGroup(true, start, matchEnd - start, value, null);

        var indexToName = new Dictionary<int, string>();
        foreach (var (name, index) in _groupNames)
            indexToName[index] = name;

        var named = new Dictionary<string, RegexGroup>(StringComparer.Ordinal);
        for (var i = 1; i <= _captureCount; i++)
        {
            indexToName.TryGetValue(i, out var name);
            if (end.TryGetCapture(i, out var s, out var e) && e >= s)
            {
                var g = new RegexGroup(true, s, e - s, input.Substring(s, e - s), name);
                groups[i] = g;
                if (name != null) named[name] = g;
            }
            else
            {
                groups[i] = RegexGroup.Unmatched(name);
                if (name != null) named[name] = groups[i];
            }
        }

        return new RegexMatch(true, start, matchEnd - start, value, groups, named);
    }

    // ----- Compilation --------------------------------------------------------

    private CompiledMatcher Compile(RegexNode node, Direction dir, Flags flags) => node switch
    {
        EmptyNode => (s, c) => c(s),
        CharNode ch => CompileChar(ch.CodePoint, dir, flags),
        AnyCharNode => CompileAnyChar(dir, flags),
        CharClassNode cls => CompileCharClass(cls.Set, dir, flags),
        SequenceNode seq => CompileSequence(seq.Terms, dir, flags),
        DisjunctionNode dis => CompileDisjunction(dis.Alternatives, dir, flags),
        GroupNode grp => CompileGroup(grp, dir, flags),
        ModifierGroupNode mod => Compile(mod.Child, dir, flags.With(mod.Added, mod.Removed)),
        QuantifierNode q => CompileQuantifier(q, dir, flags),
        BackreferenceNode br => CompileBackreference(br, dir, flags),
        AnchorNode a => CompileAnchor(a.Kind, flags),
        LookaroundNode la => CompileLookaround(la, flags),
        _ => throw new InvalidOperationException($"Unhandled node {node.GetType().Name}"),
    };

    private CompiledMatcher CompileSequence(IReadOnlyList<RegexNode> terms, Direction dir, Flags flags)
    {
        var matchers = new CompiledMatcher[terms.Count];
        for (var i = 0; i < terms.Count; i++)
            matchers[i] = Compile(terms[i], dir, flags);

        // Forward: apply left-to-right. Backward (look-behind): apply right-to-left
        // — the spec composes Alternative terms in reverse under direction −1.
        var order = dir == Direction.Forward
            ? matchers
            : Reverse(matchers);

        return (s, c) =>
        {
            Continuation k = c;
            for (var i = order.Length - 1; i >= 0; i--)
            {
                var m = order[i];
                var next = k;
                k = state => m(state, next);
            }
            return k(s);
        };
    }

    private static CompiledMatcher[] Reverse(CompiledMatcher[] source)
    {
        var copy = new CompiledMatcher[source.Length];
        for (var i = 0; i < source.Length; i++)
            copy[i] = source[source.Length - 1 - i];
        return copy;
    }

    private CompiledMatcher CompileDisjunction(IReadOnlyList<RegexNode> alts, Direction dir, Flags flags)
    {
        var matchers = new CompiledMatcher[alts.Count];
        for (var i = 0; i < alts.Count; i++)
            matchers[i] = Compile(alts[i], dir, flags);

        return (s, c) =>
        {
            foreach (var m in matchers)
            {
                var r = m(s, c);
                if (r != null)
                    return r;
            }
            return null;
        };
    }

    private CompiledMatcher CompileGroup(GroupNode group, Direction dir, Flags flags)
    {
        var inner = Compile(group.Child, dir, flags);
        if (!group.IsCapturing)
            return inner;

        var index = group.CaptureIndex;
        return (s, c) =>
        {
            var entry = s.Position;
            return inner(s, y =>
            {
                // Capture span is [min(entry,exit), max(entry,exit)] regardless of
                // direction, so look-behind captures read left-to-right (fixes #3/#4).
                var exit = y.Position;
                var lo = dir == Direction.Forward ? entry : exit;
                var hi = dir == Direction.Forward ? exit : entry;
                return c(y.WithCapture(index, lo, hi));
            });
        };
    }

    private CompiledMatcher CompileQuantifier(QuantifierNode q, Direction dir, Flags flags)
    {
        var inner = Compile(q.Child, dir, flags);
        var capIndices = CollectCaptureIndices(q.Child);
        var min = q.Min;
        var max = q.Max;
        var greedy = q.Greedy;

        // RepeatMatcher (§22.2.2.3.1). The empty-iteration guard — abandoning a
        // min=0 iteration that consumed nothing — is what makes a nullable
        // quantifier match the JS-correct (longer) string (fixes #8).
        MatchState? Repeat(int remMin, int remMax, MatchState x, Continuation c)
        {
            if (!x.Budget.Step())
                return null;
            if (remMax == 0)
                return c(x);

            Continuation d = y =>
            {
                if (remMin == 0 && y.Position == x.Position)
                    return null; // empty match for an optional repeat → stop
                var nextMin = remMin == 0 ? 0 : remMin - 1;
                var nextMax = remMax == QuantifierNode.Unbounded ? QuantifierNode.Unbounded : remMax - 1;
                return Repeat(nextMin, nextMax, y, c);
            };

            var xr = x.WithResetCaptures(capIndices);

            if (remMin != 0)
                return inner(xr, d);

            if (greedy)
                return inner(xr, d) ?? c(x);

            return c(x) ?? inner(xr, d);
        }

        return (s, c) => Repeat(min, max, s, c);
    }

    private CompiledMatcher CompileBackreference(BackreferenceNode br, Direction dir, Flags flags)
    {
        var index = br.Name != null
            ? (_groupNames.TryGetValue(br.Name, out var i) ? i : 0)
            : br.Index;

        if (index <= 0 || index > _captureCount)
        {
            if (br.Name != null)
                throw new RegexSyntaxException($"Reference to non-existent group '{br.Name}'");
            // A numeric reference past the group count matches the empty string.
            return (s, c) => c(s);
        }

        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;

        return (s, c) =>
        {
            if (!s.TryGetCapture(index, out var cs, out var ce))
                return c(s); // an unmatched group's back-reference matches empty
            var len = ce - cs;
            if (len == 0)
                return c(s);

            int from, to;
            if (dir == Direction.Forward)
            {
                from = s.Position;
                to = from + len;
                if (to > s.Input.Length)
                    return null;
            }
            else
            {
                to = s.Position;
                from = to - len;
                if (from < 0)
                    return null;
            }

            if (!RegionEquals(s.Input, from, s.Input, cs, len, ignoreCase, unicode))
                return null;

            return c(s.WithPosition(dir == Direction.Forward ? to : from));
        };
    }

    private CompiledMatcher CompileAnchor(AnchorKind kind, Flags flags)
    {
        var multiline = flags.Multiline;
        return kind switch
        {
            AnchorKind.StartOfInput => (s, c) =>
                (s.Position == 0 || (multiline && IsLineTerminator(CodePointBefore(s.Input, s.Position).cp)))
                    ? c(s) : null,
            AnchorKind.EndOfInput => (s, c) =>
                (s.Position == s.Input.Length || (multiline && IsLineTerminator(CodePointAt(s.Input, s.Position).cp)))
                    ? c(s) : null,
            AnchorKind.WordBoundary => (s, c) => IsWordBoundary(s) ? c(s) : null,
            AnchorKind.NonWordBoundary => (s, c) => !IsWordBoundary(s) ? c(s) : null,
            _ => throw new InvalidOperationException(),
        };
    }

    private CompiledMatcher CompileLookaround(LookaroundNode la, Flags flags)
    {
        var dir = la.Behind ? Direction.Backward : Direction.Forward;
        var inner = Compile(la.Child, dir, flags);
        var negative = la.Negative;

        return (s, c) =>
        {
            var matched = inner(s, y => y);
            if (negative)
                return matched != null ? null : c(s);
            if (matched == null)
                return null;
            // Positive look-around keeps the captures it set, but restores position.
            return c(new MatchState(s.Input, s.Position, matched.Captures, s.Budget));
        };
    }

    // ----- Atom matchers ------------------------------------------------------

    private CompiledMatcher CompileChar(int codePoint, Direction dir, Flags flags)
    {
        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;
        return (s, c) =>
        {
            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            return CaseFolding.Equal(actual, codePoint, ignoreCase, unicode)
                ? c(s.WithPosition(nextPos)) : null;
        };
    }

    private CompiledMatcher CompileAnyChar(Direction dir, Flags flags)
    {
        var dotAll = flags.DotAll;
        return (s, c) =>
        {
            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            if (!dotAll && IsLineTerminator(actual))
                return null;
            return c(s.WithPosition(nextPos));
        };
    }

    private CompiledMatcher CompileCharClass(CharSet set, Direction dir, Flags flags)
    {
        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;
        return (s, c) =>
        {
            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            return set.Contains(actual, ignoreCase, unicode)
                ? c(s.WithPosition(nextPos)) : null;
        };
    }

    /// <summary>Reads the code point in <paramref name="dir"/>, yielding the next position.</summary>
    private bool ReadCodePoint(MatchState s, Direction dir, out int codePoint, out int nextPos)
    {
        if (dir == Direction.Forward)
        {
            if (s.Position >= s.Input.Length)
            {
                codePoint = -1; nextPos = s.Position; return false;
            }
            var (cp, width) = CodePointAt(s.Input, s.Position);
            codePoint = cp;
            nextPos = s.Position + width;
            return true;
        }
        else
        {
            if (s.Position <= 0)
            {
                codePoint = -1; nextPos = s.Position; return false;
            }
            var (cp, width) = CodePointBefore(s.Input, s.Position);
            codePoint = cp;
            nextPos = s.Position - width;
            return true;
        }
    }

    // ----- Code-point / character helpers ------------------------------------

    private (int cp, int width) CodePointAt(string input, int pos)
    {
        var c = input[pos];
        if (_unicode && char.IsHighSurrogate(c) && pos + 1 < input.Length && char.IsLowSurrogate(input[pos + 1]))
            return (char.ConvertToUtf32(c, input[pos + 1]), 2);
        return (c, 1);
    }

    private (int cp, int width) CodePointBefore(string input, int pos)
    {
        if (pos <= 0)
            return (-1, 0);
        var c = input[pos - 1];
        if (_unicode && char.IsLowSurrogate(c) && pos - 2 >= 0 && char.IsHighSurrogate(input[pos - 2]))
            return (char.ConvertToUtf32(input[pos - 2], c), 2);
        return (c, 1);
    }

    private static bool IsLineTerminator(int cp)
        => cp is 0x000A or 0x000D or 0x2028 or 0x2029;

    private bool IsWordBoundary(MatchState s)
    {
        var before = s.Position > 0 && IsWordChar(CodePointBefore(s.Input, s.Position).cp);
        var after = s.Position < s.Input.Length && IsWordChar(CodePointAt(s.Input, s.Position).cp);
        return before != after;
    }

    private static bool IsWordChar(int cp) => UnicodeCharSets.IsWord(cp);

    /// <summary>Code-point-aware, case-fold-aware comparison of two equal-length regions.</summary>
    private static bool RegionEquals(string a, int aStart, string b, int bStart, int len,
        bool ignoreCase, bool unicode)
    {
        int ai = aStart, bi = bStart;
        var aEnd = aStart + len;
        while (ai < aEnd)
        {
            var (acp, aw) = ReadAt(a, ai, unicode);
            var (bcp, bw) = ReadAt(b, bi, unicode);
            if (!CaseFolding.Equal(acp, bcp, ignoreCase, unicode))
                return false;
            ai += aw;
            bi += bw;
        }
        return ai == aEnd;
    }

    private static (int cp, int width) ReadAt(string input, int pos, bool unicode)
    {
        var c = input[pos];
        if (unicode && char.IsHighSurrogate(c) && pos + 1 < input.Length && char.IsLowSurrogate(input[pos + 1]))
            return (char.ConvertToUtf32(c, input[pos + 1]), 2);
        return (c, 1);
    }

    /// <summary>Collects the 1-based capture indices contained in a subtree.</summary>
    private static int[] CollectCaptureIndices(RegexNode node)
    {
        var list = new List<int>();
        void Walk(RegexNode n)
        {
            switch (n)
            {
                case GroupNode g:
                    if (g.IsCapturing) list.Add(g.CaptureIndex);
                    Walk(g.Child);
                    break;
                case ModifierGroupNode m: Walk(m.Child); break;
                case QuantifierNode q: Walk(q.Child); break;
                case LookaroundNode la: Walk(la.Child); break;
                case SequenceNode seq:
                    foreach (var t in seq.Terms) Walk(t);
                    break;
                case DisjunctionNode d:
                    foreach (var a in d.Alternatives) Walk(a);
                    break;
            }
        }
        Walk(node);
        return list.ToArray();
    }
}
