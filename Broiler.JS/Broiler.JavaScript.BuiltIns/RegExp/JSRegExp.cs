using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Runtime.Unicode;
using Broiler.Regex;
using Broiler.Regex.Text;
using System;
using System.Collections.Generic;
using System.Text;
using Broiler.ECMAScript.Text;

namespace Broiler.JavaScript.BuiltIns.RegExp;


[JSClassGenerator("RegExp")]
public partial class JSRegExp : JSObject, IJSRegExp
{
    string IJSRegExp.Pattern => pattern;
    string IJSRegExp.Flags => flags;
    bool IJSRegExp.IsMatch(string input) => IsMatch(input);

    internal static bool IsRegExpLike(JSValue value)
    {
        if (value is not JSObject @object)
            return false;

        var matchSymbol = GetGlobalSymbolFactory?.Invoke("match");
        if (matchSymbol != null)
        {
            var matcher = @object[matchSymbol];
            if (!matcher.IsUndefined)
                return matcher.BooleanValue;
        }

        return value is JSRegExp;
    }

    /// <summary>
    /// The text helpers' escaper over this engine's Unicode tables (milestone SJL-11.5; Broiler.VM's JSD-0081,
    /// decision D20, onto Broiler.Regex's R9): RegExp.escape reads white space through it.
    /// </summary>
    private static readonly RegexEscaper Escaper = new(JSUnicode.Tables);

    /// <summary>The longest string this engine holds, the limit the text helpers are given.</summary>
    private const int LongestString = 0x3FFFFFDF;

    // RegExp.escape (ECMA-262 22.2.5.1): the argument's type check is this engine's, and the text is
    // Broiler.Regex's EscapeString (SJL-11.5, where the escape helpers were this engine's own).
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.5.1; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: RegExp.escape("\u2028") answers anything but "\\u2028", or RegExp.escape(1) does not throw a TypeError
    // Broiler-Human:        PENDING
    [JSExport("escape", Length = 1)]
    internal static JSValue Escape(in Arguments a)
    {
        var input = a.Get1();
        if (!input.IsString)
            throw JSEngine.NewTypeError("RegExp.escape requires a string argument");

        if (Escaper.EscapeString(input.StringValue, LongestString, null, out var escaped) != RegexStatus.Success)
            throw JSEngine.NewRangeError("Invalid string length");

        return CreateString(escaped);
    }

    public string pattern;

    // The source getter's text (ECMA-262 22.2.6.13.1, EscapeRegExpPattern): Broiler.Regex's EscapePattern over
    // [[OriginalSource]] (SJL-11.5, where the helper was this engine's own). The pattern is the text the
    // literal or the constructor gave, unchanged, since the scanner no longer rewrites braced escapes (row T1).
    [JSExport("source")]
    public string Source
        => RegexEscaper.EscapePattern(pattern, LongestString, null, out var source) == RegexStatus.Success
            ? source
            : throw JSEngine.NewRangeError("Invalid string length");

    [JSExport]
    public string flags;

    [JSExport("global")]
    public bool globalSearch;

    [JSExport]
    public bool multiline;
    [JSExport]
    public bool ignoreCase;
    [JSExport]
    public bool hasIndices;
    [JSExport]
    public bool sticky;
    [JSExport]
    public bool unicode;
    [JSExport]
    public bool unicodeSets;

    // The capture layout the exec result is built from: the group count and each name with the groups that
    // carry it, in source order, from Broiler.Regex's compiled pattern (BuildCaptureMapFromBroiler).
    internal CaptureGroupMap captureMap;

    // NOTE: not [JSExport]. `lastIndex` is exposed to JS as a per-instance own data
    // property added in the constructor (see below), not via a generated prototype
    // accessor — see the LastIndex note in JSRegExpPrototype.cs.
    public int lastIndex = 0;

    public JSRegExp(in Arguments a) : this()
    {
        // Per §22.2.4 RegExp(pattern, flags), [[OriginalSource]] / [[OriginalFlags]] for a
        // pattern with a [[RegExpMatcher]] are read from the INTERNAL SLOTS (step 5), which
        // happens BEFORE RegExpAlloc reads NewTarget.prototype (step 8). Going through
        // `: base(JSEngine.NewTargetPrototype)` here evaluated NewTarget.prototype first —
        // visible to an ill-behaving subclass whose prototype getter recompiles the source
        // (test262 sm/RegExp/constructor-ordering). Defer the prototype assignment until
        // after the source has been captured. Likewise, ToString of the flags argument is
        // performed by RegExpInitialize (step 8) AFTER RegExpAlloc, so a flags object whose
        // toString observes the NewTarget.prototype getter sees it as already fired
        // (test262 sm/RegExp/constructor-ordering-2).
        var pattern = "";
        var patternValue = a.GetAt(0);
        var flagsArg = a.Length > 1 ? a.GetAt(1) : JSUndefined.Value;
        var flagsArgGiven = a.Length > 1 && !flagsArg.IsUndefined;
        // The flags value carried over from a RegExp-like pattern when no explicit flags arg
        // is supplied. ToString-ing it is deferred along with the flags arg.
        JSValue flagsCarried = null;

        if (a.Length > 0)
        {
            // Step 1: IsRegExp(pattern) — this triggers Get(pattern, @@match) on EVERY object
            // pattern, including real RegExps (test262 IsRegExpLike_Accesses_Symbol_Match).
            var isRegExpLike = IsRegExpLike(patternValue);
            if (patternValue is JSRegExp existingRe)
            {
                // Step 4: a pattern with a [[RegExpMatcher]] reads its internal slots directly,
                // skipping the "source"/"flags" property accesses. Going through the property
                // path was observable via a redefined RegExp.prototype.source/flags getter and
                // — more dramatically — let a getter run BETWEEN our @@match probe and the
                // source capture.
                pattern = existingRe.pattern;
                if (!flagsArgGiven)
                    flagsCarried = new JSString(existingRe.flags);
            }
            else if (isRegExpLike)
            {
                var regExpLike = (JSObject)patternValue;
                if (!flagsArgGiven)
                    _ = regExpLike[KeyStrings.constructor];

                var sourceKey = KeyStrings.GetOrCreate("source");
                var flagsKey = KeyStrings.GetOrCreate("flags");
                pattern = regExpLike[sourceKey].IsUndefined ? string.Empty : regExpLike[sourceKey].StringValue;
                if (!flagsArgGiven)
                    flagsCarried = regExpLike[flagsKey];
            }
            else
            {
                // Per 22.2.3.1 RegExpInitialize: if pattern is undefined, P = "";
                // if flags is undefined, F = "". Otherwise ToString the value.
                if (!patternValue.IsUndefined)
                    pattern = patternValue.StringValue;
            }
        }

        // Source has been captured. NOW read NewTarget.prototype — any user getter
        // running here can no longer back-affect it. `this()` already set the default
        // RegExp.prototype, so a subclass's distinct prototype is the only override.
        var newTargetPrototype = JSEngine.NewTargetPrototype;
        if (newTargetPrototype != null)
            BasePrototypeObject = newTargetPrototype;

        // Per §22.2.3.1 RegExpInitialize: ToString(F) runs in step 4 of RegExpInitialize, which
        // is invoked AFTER RegExpAlloc (and therefore after NewTarget.prototype has been read).
        var flags = "";
        if (flagsArgGiven)
            flags = flagsArg.StringValue;
        else if (flagsCarried != null && !flagsCarried.IsUndefined)
            flags = flagsCarried.StringValue;

        this.pattern = pattern;
        Initialize(CompilePattern(pattern, flags));

        // Initialize lastIndex as an own data property (writable, non-configurable, non-enumerable)
        ref var ownProperties = ref GetOwnProperties();
        ownProperties.Put(KeyStrings.lastIndex, NumberZero, JSPropertyAttributes.Value);
    }

    public JSRegExp(string pattern, string flags) : this()
    {
        this.pattern = pattern;
        Initialize(CompilePattern(pattern, flags));

        // Initialize lastIndex as an own data property (writable, non-configurable, non-enumerable)
        ref var ownProps = ref GetOwnProperties();
        ownProps.Put(KeyStrings.lastIndex, NumberZero, JSPropertyAttributes.Value);
    }

    /// <summary>
    /// Sets this instance's matcher, capture layout and flag fields from a compiled pattern: both constructors
    /// and <c>RegExp.prototype.compile</c>, which replaces the matcher on a live object, go through it.
    /// </summary>
    private void Initialize(CompiledPattern compiled)
    {
        broiler = compiled.Regex;
        captureMap = BuildCaptureMapFromBroiler(compiled.Regex);
        var f = compiled.Flags;
        globalSearch = (f & RegexFlags.Global) != 0;
        ignoreCase = (f & RegexFlags.IgnoreCase) != 0;
        multiline = (f & RegexFlags.Multiline) != 0;
        hasIndices = (f & RegexFlags.HasIndices) != 0;
        sticky = (f & RegexFlags.Sticky) != 0;
        unicode = (f & RegexFlags.Unicode) != 0;
        unicodeSets = (f & RegexFlags.UnicodeSets) != 0;
        flags = RegexFlagsText.Format(f);
    }

    /// <summary>
    /// Finds all regular expression matches within the given string.
    public JSValue Match(JSValue input)
    {
        var isGlobal = this[KeyStrings.GetOrCreate("global")].BooleanValue;

        // If the global flag is not set, returns a single match.
        if (!isGlobal)
            return ExecuteMatch(input);

        SetObservableLastIndex(0);
        var inputString = input.StringValue;
        var matchValues = CreateArray();
        uint matchCount = 0;

        while (true)
        {
            var result = ExecuteMatch(CreateString(inputString));
            if (result.IsNull)
                return matchCount == 0 ? NullValue : matchValues;

            var match = result[0].StringValue;
            matchValues[matchCount++] = CreateString(match);

            if (match.Length != 0)
                continue;

            // §22.2.6.9 [Symbol.match] step 8.g.iv: after an empty match, advance
            // lastIndex with AdvanceStringIndex. In a Unicode (`u`/`v`) regex a leading
            // surrogate paired with a trailing one is one code point, so the index moves
            // by two — otherwise the loop would yield a spurious extra empty match between
            // the two halves of an astral character.
            var fullUnicode = this[KeyStrings.GetOrCreate("unicode")].BooleanValue
                || this[KeyStrings.GetOrCreate("unicodeSets")].BooleanValue;
            var nextLastIndex = GetObservableLastIndex();
            if (nextLastIndex >= inputString.Length)
                return matchValues;

            SetObservableLastIndex((int)Utf16.AdvanceStringIndex(inputString, nextLastIndex, fullUnicode));
        }
    }

    private JSValue ExecuteMatch(JSValue input)
    {
        // RegExpExec: only a callable "exec" is invoked; any other value (undefined, null,
        // or a non-callable primitive) falls back to the built-in RegExpBuiltinExec.
        var exec = this[KeyStrings.GetOrCreate("exec")];
        if (!exec.IsFunction)
            return Exec(new Arguments(this, input));

        var result = exec.InvokeFunction(new Arguments(this, input));
        if (!result.IsObject && !result.IsNull)
            throw JSEngine.NewTypeError("RegExp exec result must be an object or null");

        return result;
    }

    /// <summary>
    /// Splits the given string into an array of strings by separating the string into substrings.
    /// </summary>
    /// <param name="input"> The string to split. </param>
    /// <param name="limit"> The maximum number of array items to return.  Defaults to unlimited. </param>
    /// <returns> An array containing the split strings. </returns>
    public JSValue Split(string input, uint limit = uint.MaxValue)
    {
        // Return an empty array if limit = 0.
        if (limit == 0)
            return CreateArray();

        var results = CreateArray();
        int startIndex = 0;

        // Iterate through Broiler.Regex's matches, so the pattern splits on the same matches
        // exec reports. Groups is already the ECMAScript-order capture array; an unmatched
        // group contributes undefined. The y flag does not anchor the search: RegExp.prototype
        // [@@split] (ECMA-262 22.2.6.14) builds its splitter sticky whatever the flags and
        // tries every position, so the flag changes nothing there either.
        foreach (var match in EnumerateMatches(input, anchored: false))
        {
            // Do not split on an empty match at the start or end of the string or at the
            // end of the previous match.
            if (match.Length == 0 && (match.Index == 0 || match.Index == input.Length || match.Index == startIndex))
                continue;

            // Add the text preceding this match.
            var element = input.Substring(startIndex, match.Index - startIndex);
            results.AddArrayItem(CreateString(element));

            if (results.Length >= limit)
                return results;

            startIndex = match.Index + match.Length;

            var groups = match.Groups;
            for (int i = 1; i < groups.Length; i++)
            {
                var group = groups[i];
                results.AddArrayItem(group.Success ? CreateString(group.Value) : JSUndefined.Value);

                if (results.Length >= limit)
                    return results;
            }
        }

        var ele = input.Substring(startIndex, input.Length - startIndex);
        results.AddArrayItem(CreateString(ele));
        return results;
    }

    /// <summary>
    /// Returns a copy of the given string with text replaced using a regular expression.
    /// </summary>
    /// <param name="input"> The string on which to perform the search. </param>
    /// <param name="replaceFunction"> A function that is called to produce the text to replace
    /// for every successful match. </param>
    /// <returns> A copy of the given string with text replaced using a regular expression. </returns>
    public string Replace(string input, JSValue replaceFunction)
    {
        if (!replaceFunction.IsFunction)
            // A non-callable replacement is coerced with the spec ToString (StringValue), which
            // throws for an object yielding no primitive — not the lenient CLR ToString.
            return Replace(input, replaceFunction.StringValue);

        return ReplaceMatches(input, match =>
        {
            var groups = match.Groups;
            JSValue[] parameters = new JSValue[groups.Length + 2];
            for (int i = 0; i < groups.Length; i++)
                parameters[i] = groups[i].Success ? CreateString(groups[i].Value) : JSUndefined.Value;

            parameters[groups.Length] = CreateNumber(match.Index);
            parameters[groups.Length + 1] = CreateString(input);

            var a = new Arguments(NullValue, parameters);
            return replaceFunction.InvokeFunction(a).ToString();
        });
    }

    /// <summary>
    /// Builds the replacement result: each match up to the global/first-only limit has its
    /// span replaced by <paramref name="evaluator"/>. Iterates Broiler.Regex's matches through
    /// <see cref="EnumerateMatches"/>, so the pattern replaces on the same matches <c>exec</c>
    /// reports: a sticky pattern matches at the start of each step alone, as
    /// RegExp.prototype[@@replace] (ECMA-262 22.2.6.11) finds it through exec from lastIndex 0.
    /// </summary>
    private string ReplaceMatches(string input, System.Func<RegexMatchData, string> evaluator)
    {
        var limit = globalSearch ? int.MaxValue : 1;
        var builder = new StringBuilder(input.Length);
        int last = 0;
        int done = 0;

        foreach (var match in EnumerateMatches(input, anchored: sticky))
        {
            if (done >= limit)
                break;
            builder.Append(input, last, match.Index - last);
            builder.Append(evaluator(match));
            last = match.Index + match.Length;
            done++;
        }

        builder.Append(input, last, input.Length - last);
        return builder.ToString();
    }

    /// <summary>
    /// Returns a copy of the given string with text replaced using a regular expression.
    /// </summary>
    /// <param name="input"> The string on which to perform the search. </param>
    /// <param name="replaceText"> A string containing the text to replace for every successful match. </param>
    /// <returns> A copy of the given string with text replaced using a regular expression. </returns>
    public string Replace(string input, string replaceText)
    {
        // The template is the shared library's parse (SJL-5.5), taken once, and its parts are
        // evaluated per match. This pattern's groups are its captures, and a group that did not
        // participate substitutes the empty string; there are no named captures here, so `$<`
        // stays literal. String.prototype.replace no longer reaches this member (SJL-5.5, S3): it is
        // kept for its public callers (decision SJL-D-25).
        ReplacementPart[]? parts = null;

        return ReplaceMatches(input, match =>
        {
            var groups = match.Groups;
            parts ??= ReplacementTemplate.Parse(replaceText, groups.Length - 1, false);

            var replacementBuilder = new StringBuilder(replaceText.Length);
            foreach (var part in parts)
            {
                switch (part.Kind)
                {
                    case ReplacementPartKind.Dollar:
                        replacementBuilder.Append('$');
                        break;
                    case ReplacementPartKind.Match:
                        replacementBuilder.Append(match.Value);
                        break;
                    case ReplacementPartKind.Before:
                        replacementBuilder.Append(input.AsSpan(0, match.Index));
                        break;
                    case ReplacementPartKind.After:
                        replacementBuilder.Append(input.AsSpan(match.Index + match.Length));
                        break;
                    case ReplacementPartKind.Capture:
                        var group = groups[part.CaptureIndex];
                        replacementBuilder.Append(group.Success ? group.Value : string.Empty);
                        break;
                    default:
                        replacementBuilder.Append(replaceText.AsSpan(part.Start, part.Length));
                        break;
                }
            }

            return replacementBuilder.ToString();
        });
    }

    // The capture layout of a compiled pattern: the group count, each group's name, and each distinct name with
    // the groups that carry it, in first-appearance order (more than one only for ES2025's duplicate names).
    public sealed class CaptureGroupMap
    {
        // 1-based: OriginalName[i] is the JS name of capture group i, or null when
        // the group was unnamed. Index 0 is a placeholder for the whole match.
        public readonly string[] OriginalName;

        // Distinct JS names in first-appearance order, each paired with the capture
        // indices that share the name (more than one only for ES2025 duplicates).
        public readonly List<(string Name, List<int> Indices)> NamedGroups;

        public int Count => OriginalName.Length - 1;

        public CaptureGroupMap(string[] originalName, List<(string, List<int>)> namedGroups)
        {
            OriginalName = originalName;
            NamedGroups = namedGroups;
        }
    }

    public override string ToString() => $"/{pattern}/{flags}";
}
