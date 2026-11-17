using System;
using System.Collections.Generic;
using Broiler.Regex;
using Broiler.Regex.Text;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime.Unicode;
using Broiler.ECMAScript.Text;

namespace Broiler.JavaScript.BuiltIns.RegExp;

// The adapter onto Broiler.Regex (milestone SJL-11.5 of the shared JavaScript libraries programme; Broiler.VM's
// JSD-0081, decisions D7, D10 and D20 to D23).
//
// Every pattern, with or without u or v, is compiled and matched by Broiler.Regex. The .NET translator, its routing
// between the two engines, the .NET Regex field and its overflow fallback are gone: this engine keeps its
// RegExp objects, lastIndex, the Symbol protocols, species, GetSubstitution's evaluation, the legacy statics, argument
// conversion and its errors, and nothing of the pattern's grammar or its matching.
//
// The policy (decisions D7 and D10), the one both engines pass: no meter and no step ceiling; a frame ceiling of
// 1 << 20 and a trail ceiling of 1 << 21; a nesting limit of 512. A ceiling reached is a catchable RangeError and
// the nesting limit a SyntaxError, never a no-match.
public partial class JSRegExp
{
    /// <summary>The nesting limit this engine passes (decision D7): 511 nested groups compile, 512 are refused.</summary>
    internal const int NestingLimit = BroilerRegex.MaximumNestingLimit;

    /// <summary>The match ceilings this engine passes (decision D10): no step ceiling, the engines' frames and trail.</summary>
    internal static readonly RegexMatchCeilings Ceilings =
        new(null, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail);

    // This pattern's compiled form; set by every constructor and by RegExp.prototype.compile.
    internal BroilerRegex broiler;

    /// <summary>A pattern compiled under its flags, with the flags read.</summary>
    internal readonly record struct CompiledPattern(BroilerRegex Regex, RegexFlags Flags);

    /// <summary>
    /// RegExpInitialize's flags and pattern steps (ECMA-262 22.2.3.1, steps 5 to 13): the flags read by Broiler.Regex's
    /// flag parser and the pattern parsed and compiled by Broiler.Regex under this engine's nesting limit, with no meter
    /// and no compile budget.
    /// </summary>
    /// <exception cref="JSException">
    /// A SyntaxError for flags or a pattern the specification refuses, and for a pattern nested deeper than the limit
    /// (decision D7).
    /// </exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.3.1 and JSD-0081 decisions D7 and D10; IP=Low; Security=High; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: new RegExp("a", "gg"), new RegExp("(?<=a)?", "u") or a pattern of 512 nested groups is constructed instead of throwing a SyntaxError
    // Broiler-Human:        PENDING
    internal static CompiledPattern CompilePattern(string pattern, string flags)
    {
        pattern ??= "";
        if (RegexFlagsText.Parse(flags ?? "", out var parsed, out var flagsDiagnostic) != RegexStatus.Success)
            throw JSEngine.NewSyntaxError($"Invalid flags supplied to RegExp constructor '{flags}': {flagsDiagnostic.Message}");

        var status = BroilerRegex.Compile(pattern, parsed, JSUnicode.Tables, NestingLimit, null, null,
            out var regex, out _, out var diagnostic);
        if (status != RegexStatus.Success)
            throw JSEngine.NewSyntaxError($"Invalid regular expression: /{pattern}/{flags}: {diagnostic.Message}");

        return new CompiledPattern(regex, parsed);
    }

    /// <summary>
    /// Builds the <see cref="CaptureGroupMap"/> of a compiled pattern: Broiler.Regex numbers the captures in source
    /// order and keeps the names native, so the map records the count and each name with the groups that carry it.
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

    // ----- The match result ----------------------------------------------------------
    //
    // Groups[0] is the whole match; Groups[1..] are the captures in ECMAScript (source) order.

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

    /// <summary>
    /// The matcher's run of RegExpBuiltinExec (ECMA-262 22.2.7.2, steps 13 to 24) from <paramref name="start"/>, the
    /// caller's lastIndex, at <paramref name="start"/> alone when <paramref name="anchored"/> is set: Broiler.Regex's
    /// offset entry under this engine's ceilings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A start inside a surrogate pair</b> under <c>u</c> or <c>v</c> (row M4 of the correspondence; decision D23):
    /// the matcher runs from the pair, as step 13.b reads its character from element lastIndex. The step moves only the
    /// matcher's start: a non-empty match found at the pair reports <paramref name="start"/> as its index and its
    /// substring from there, as the specification's steps 22 and 24 do. An empty match found at the pair, decision
    /// D41's implementation-defined case, is reported where it was found, at the pair's first unit.
    /// </para>
    /// <para>
    /// <b>A ceiling reached</b> is a catchable RangeError naming it (decision D10), never no match.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.7.2 and JSD-0081 decisions D10, D23 and D41; IP=Low; Security=High; Resources=5; Fingerprint=TBF
    // Broiler-Falsified-If: /\udf06/u.exec("𝌆") from lastIndex 1 matches, or /^a*$/ over 2,000,000 "a" answers null instead of throwing a RangeError or matching
    // Broiler-Human:        PENDING
    internal RegexMatchData RunMatch(string input, int start, bool anchored)
    {
        if (start < 0)
            start = 0;
        if (start > input.Length)
            return RegexMatchData.NoMatch;

        var from = start;
        if ((unicode || unicodeSets) && from > 0 && from < input.Length
            && char.IsLowSurrogate(input[from]) && char.IsHighSurrogate(input[from - 1]))
            from--;

        var slots = new int[broiler.SlotCount];
        var status = broiler.Match(input, from, anchored, slots, null, Ceilings, out var limit);
        if (status == RegexStatus.NoMatch)
            return RegexMatchData.NoMatch;
        if (status == RegexStatus.LimitExceeded)
            throw JSEngine.NewRangeError(limit == RegexLimit.Trail
                ? "Maximum regular expression backtracking trail exceeded"
                : "Maximum regular expression backtracking frames exceeded");

        var end = slots[1];
        var index = slots[0] == from && end >= start ? start : slots[0];
        var groups = new RegexCapture[slots.Length / 2];
        var value = input.Substring(index, end - index);
        groups[0] = new RegexCapture(true, index, end - index, value);
        for (var g = 1; g < groups.Length; g++)
        {
            var groupStart = slots[2 * g];
            var groupEnd = slots[(2 * g) + 1];
            groups[g] = groupStart < 0
                ? new RegexCapture(false, 0, 0, null)
                : new RegexCapture(true, groupStart, groupEnd - groupStart, input.Substring(groupStart, groupEnd - groupStart));
        }

        return new RegexMatchData
        {
            Success = true,
            Index = index,
            Length = end - index,
            Value = value,
            Groups = groups,
        };
    }

    /// <summary>
    /// Enumerates the non-overlapping matches of this pattern in <paramref name="input"/>, advancing past an empty
    /// match by one code point (a surrogate pair under <c>u</c>/<c>v</c>) as §22.2.6.9 iteration requires, so that
    /// <see cref="Split"/> and <see cref="Replace(string,string)"/> read the same match data <c>exec</c> sees.
    /// </summary>
    internal IEnumerable<RegexMatchData> EnumerateMatches(string input)
    {
        var pos = 0;
        while (pos <= input.Length)
        {
            var match = RunMatch(input, pos, anchored: false);
            if (!match.Success)
                yield break;

            yield return match;

            var next = match.Index + match.Length;
            if (match.Length == 0)
            {
                // Advance one position past an empty match so iteration terminates, keeping
                // a surrogate pair whole under u/v.
                next = (int)Utf16.AdvanceStringIndex(input, match.Index, unicode || unicodeSets);
            }
            pos = next;
        }
    }

    /// <summary>
    /// True when this pattern matches anywhere in <paramref name="input"/>, as <c>exec</c> from lastIndex 0 finds it.
    /// </summary>
    internal bool IsMatch(string input) => RunMatch(input, 0, anchored: false).Success;
}
