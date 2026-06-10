using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;
using UnicodeEmoji.StringProperties;

namespace Broiler.JavaScript.BuiltIns.RegExp;


[JSClassGenerator("RegExp")]
public partial class JSRegExp : JSObject, IJSRegExp
{
    string IJSRegExp.Pattern => pattern;
    string IJSRegExp.Flags => flags;
    Regex IJSRegExp.Value => value;

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

    [JSExport("escape", Length = 1)]
    internal static JSValue Escape(in Arguments a)
    {
        var input = a.Get1();
        if (!input.IsString)
            throw JSEngine.NewTypeError("RegExp.escape requires a string argument");

        var str = input.StringValue;
        var sb = new StringBuilder(str.Length + 4);

        for (int i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (TryAppendEscape(sb, c, i == 0))
                continue;

            sb.Append(c);
        }

        return JSValue.CreateString(sb.ToString());
    }

    private static bool TryAppendEscape(StringBuilder sb, char c, bool isFirstCharacter)
    {
        if (isFirstCharacter && IsAsciiLetterOrDigit(c))
        {
            AppendHexEscape(sb, c);
            return true;
        }

        switch (c)
        {
            case '\t':
                sb.Append(@"\t");
                return true;
            case '\n':
                sb.Append(@"\n");
                return true;
            case '\v':
                sb.Append(@"\v");
                return true;
            case '\f':
                sb.Append(@"\f");
                return true;
            case '\r':
                sb.Append(@"\r");
                return true;
            case ' ':
                sb.Append(@"\x20");
                return true;
        }

        if (IsSyntaxCharacter(c))
        {
            sb.Append('\\');
            return false;
        }

        if (IsOtherPunctuator(c))
        {
            AppendHexEscape(sb, c);
            return true;
        }

        if (char.IsSurrogate(c))
        {
            AppendUnicodeEscape(sb, c);
            return true;
        }

        if (char.IsWhiteSpace(c) || c == '\uFEFF' || c == '\u2028' || c == '\u2029')
        {
            AppendUnicodeEscape(sb, c);
            return true;
        }

        return false;
    }

    private static bool IsAsciiLetterOrDigit(char c)
        => (c >= 'a' && c <= 'z')
        || (c >= 'A' && c <= 'Z')
        || (c >= '0' && c <= '9');

    private static bool IsSyntaxCharacter(char c)
        => c == '^' || c == '$' || c == '\\' || c == '.' || c == '*'
        || c == '+' || c == '?' || c == '(' || c == ')' || c == '['
        || c == ']' || c == '{' || c == '}' || c == '|' || c == '/';

    private static bool IsOtherPunctuator(char c)
        => c == ',' || c == '-' || c == '=' || c == '<' || c == '>'
        || c == '#' || c == '&' || c == '!' || c == '%' || c == ':'
        || c == ';' || c == '@' || c == '~' || c == '\'' || c == '"'
        || c == '`';

    private static void AppendHexEscape(StringBuilder sb, char c)
    {
        sb.Append(@"\x");
        sb.Append(((int)c).ToString("x2"));
    }

    private static void AppendUnicodeEscape(StringBuilder sb, char c)
    {
        if (c <= 0xFF)
        {
            AppendHexEscape(sb, c);
            return;
        }

        sb.Append(@"\u");
        sb.Append(((int)c).ToString("x4"));
    }

    private static string EscapePatternSource(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "(?:)";

        var sb = new StringBuilder(pattern.Length);
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            switch (c)
            {
                case '/':
                    if (i > 0 && pattern[i - 1] == '\\')
                    {
                        sb.Append('/');
                        continue;
                    }

                    sb.Append(@"\/");
                    continue;
                case '\n':
                    sb.Append(@"\n");
                    continue;
                case '\r':
                    sb.Append(@"\r");
                    continue;
                case '\u2028':
                case '\u2029':
                    AppendUnicodeEscape(sb, c);
                    continue;
            }

            if (char.IsSurrogate(c))
            {
                AppendUnicodeEscape(sb, c);
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    public string pattern;

    [JSExport("source")]
    public string Source => EscapePatternSource(pattern);

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

    internal Regex value;

    // Non-null when the pattern contains at least one named capturing group: it
    // records the ECMAScript capture ordering and name→index mapping that .NET's
    // group renaming (see RewriteCaptureGroups) would otherwise lose. Null for
    // patterns with no named groups (the .NET numbering already matches).
    internal CaptureGroupMap captureMap;

    [JSExport]
    public int lastIndex = 0;

    public JSRegExp(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var pattern = "";
        var flags = "";
        var patternValue = a.GetAt(0);

        if (a.Length > 0)
        {
            if (IsRegExpLike(patternValue))
            {
                var regExpLike = (JSObject)patternValue;
                if (a.Length < 2 || a.GetAt(1).IsUndefined)
                    _ = regExpLike[KeyStrings.constructor];

                var sourceKey = KeyStrings.GetOrCreate("source");
                var flagsKey = KeyStrings.GetOrCreate("flags");
                pattern = regExpLike[sourceKey].IsUndefined ? string.Empty : regExpLike[sourceKey].StringValue;
                flags = a.Length > 1 && !a.GetAt(1).IsUndefined
                    ? a.GetAt(1).StringValue
                    : (regExpLike[flagsKey].IsUndefined ? string.Empty : regExpLike[flagsKey].StringValue);
            }
            else
            {
                // Per 22.2.3.1 RegExpInitialize: if pattern is undefined, P = "";
                // if flags is undefined, F = "". Otherwise ToString the value.
                if (!patternValue.IsUndefined)
                    pattern = patternValue.StringValue;

                if (a.Length > 1 && !a.GetAt(1).IsUndefined)
                    flags = a.GetAt(1).StringValue;
            }
        }

        this.pattern = pattern;

        (value, globalSearch, ignoreCase, multiline, hasIndices, sticky, unicode, unicodeSets, this.flags) = CreateRegex(pattern, flags, out captureMap);

        // Initialize lastIndex as an own data property (writable, non-configurable, non-enumerable)
        ref var ownProperties = ref GetOwnProperties();
        ownProperties.Put(KeyStrings.lastIndex, JSValue.NumberZero, JSPropertyAttributes.Value);
    }

    public JSRegExp(string pattern, string flags) : this()
    {
        this.pattern = pattern;

        (value, globalSearch, ignoreCase, multiline, hasIndices, sticky, unicode, unicodeSets, this.flags) = CreateRegex(pattern, flags, out captureMap);

        // Initialize lastIndex as an own data property (writable, non-configurable, non-enumerable)
        ref var ownProps = ref GetOwnProperties();
        ownProps.Put(KeyStrings.lastIndex, JSValue.NumberZero, JSPropertyAttributes.Value);
    }

    /// <summary>
    /// Finds all regular expression matches within the given string.
    /// </summary>
    /// <param name="input"> The string on which to perform the search. </param>
    /// <returns> An array containing the matched strings. </returns>
    public JSValue Match(JSValue input)
    {
        var isGlobal = this[KeyStrings.GetOrCreate("global")].BooleanValue;

        // If the global flag is not set, returns a single match.
        if (!isGlobal)
            return ExecuteMatch(input);

        SetObservableLastIndex(0);
        var inputString = input.StringValue;
        var matchValues = JSValue.CreateArray();
        uint matchCount = 0;

        while (true)
        {
            var result = ExecuteMatch(JSValue.CreateString(inputString));
            if (result.IsNull)
                return matchCount == 0 ? JSValue.NullValue : matchValues;

            var match = result[0].StringValue;
            matchValues[matchCount++] = JSValue.CreateString(match);

            if (match.Length != 0)
                continue;

            _ = this[KeyStrings.GetOrCreate("unicode")].BooleanValue;
            var nextLastIndex = GetObservableLastIndex();
            if (nextLastIndex >= inputString.Length)
                return matchValues;

            SetObservableLastIndex(nextLastIndex + 1);
        }
    }

    private JSValue ExecuteMatch(JSValue input)
    {
        var exec = this[KeyStrings.GetOrCreate("exec")];
        if (exec.IsUndefined)
            return Exec(new Arguments(this, input));

        if (!exec.IsFunction)
            throw JSEngine.NewTypeError("RegExp exec property is not callable");

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
            return JSValue.CreateArray();

        // Find the first match.
        Match match = value.Match(input, 0);


        var results = JSValue.CreateArray();
        int startIndex = 0;
        Match lastMatch = null;

        while (match.Success == true)
        {
            // Do not match the an empty substring at the start or end of the string or at the
            // end of the previous match.
            if (match.Length == 0 && (match.Index == 0 || match.Index == input.Length || match.Index == startIndex))
            {
                // Find the next match.
                match = match.NextMatch();
                continue;
            }

            // Add the match results to the array.
            var element = input.Substring(startIndex, match.Index - startIndex);
            results.AddArrayItem(JSValue.CreateString(element));

            if (results.Length >= limit)
                return results;

            startIndex = match.Index + match.Length;

            for (int i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                if (group.Captures.Count == 0)
                    results.AddArrayItem(JSUndefined.Value);       // Non-capturing groups return "undefined".
                else
                    results.AddArrayItem(JSValue.CreateString(match.Groups[i].Value));

                if (results.Length >= limit)
                    return results;
            }

            // Record the last match.
            lastMatch = match;

            // Find the next match.
            match = match.NextMatch();
        }
        var ele = input.Substring(startIndex, input.Length - startIndex);
        results.AddArrayItem(JSValue.CreateString(ele));
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
            return Replace(input, replaceFunction.ToString());

        return value.Replace(input, match =>
        {
            // Set the deprecated RegExp properties.
            //this.Engine.RegExp.SetDeprecatedProperties(input, match);

            JSValue[] parameters = new JSValue[match.Groups.Count + 2];
            for (int i = 0; i < match.Groups.Count; i++)
            {
                if (match.Groups[i].Success == false)
                    parameters[i] = JSUndefined.Value;
                else
                    parameters[i] = JSValue.CreateString(match.Groups[i].Value);
            }

            parameters[match.Groups.Count] = JSValue.CreateNumber(match.Index);
            parameters[match.Groups.Count + 1] = JSValue.CreateString(input);

            var a = new Arguments(JSValue.NullValue, parameters);
            return replaceFunction.InvokeFunction(a).ToString();
        }, globalSearch == true ? int.MaxValue : 1);
    }

    /// <summary>
    /// Returns a copy of the given string with text replaced using a regular expression.
    /// </summary>
    /// <param name="input"> The string on which to perform the search. </param>
    /// <param name="replaceText"> A string containing the text to replace for every successful match. </param>
    /// <returns> A copy of the given string with text replaced using a regular expression. </returns>
    public string Replace(string input, string replaceText)
    {
        // Check if the replacement string contains any patterns.
        bool replaceTextContainsPattern = replaceText.IndexOf('$') >= 0;

        // Replace the input string with replaceText, recording the last match found.
        Match lastMatch = null;
        string result = value.Replace(input, match =>
        {
            lastMatch = match;

            // If there is no pattern, replace the pattern as is.
            if (replaceTextContainsPattern == false)
                return replaceText;

            // Patterns
            // $$	Inserts a "$".
            // $&	Inserts the matched substring.
            // $`	Inserts the portion of the string that precedes the matched substring.
            // $'	Inserts the portion of the string that follows the matched substring.
            // $n or $nn	Where n or nn are decimal digits, inserts the nth parenthesized submatch string, provided the first argument was a RegExp object.
            var replacementBuilder = new StringBuilder();
            for (int i = 0; i < replaceText.Length; i++)
            {
                char c = replaceText[i];
                if (c == '$' && i < replaceText.Length - 1)
                {
                    c = replaceText[++i];
                    if (c == '$')
                        replacementBuilder.Append('$');
                    else if (c == '&')
                        replacementBuilder.Append(match.Value);
                    else if (c == '`')
                        replacementBuilder.Append(input.AsSpan(0, match.Index));
                    else if (c == '\'')
                        replacementBuilder.Append(input.AsSpan(match.Index + match.Length));
                    else if (c >= '0' && c <= '9')
                    {
                        int matchNumber1 = c - '0';

                        // The match number can be one or two digits long.
                        int matchNumber2 = 0;
                        if (i < replaceText.Length - 1 && replaceText[i + 1] >= '0' && replaceText[i + 1] <= '9')
                            matchNumber2 = matchNumber1 * 10 + (replaceText[i + 1] - '0');

                        // Try the two digit capture first.
                        if (matchNumber2 > 0 && matchNumber2 < match.Groups.Count)
                        {
                            // Two digit capture replacement.
                            replacementBuilder.Append(match.Groups[matchNumber2].Value);
                            i++;
                        }
                        else if (matchNumber1 > 0 && matchNumber1 < match.Groups.Count)
                        {
                            // Single digit capture replacement.
                            replacementBuilder.Append(match.Groups[matchNumber1].Value);
                        }
                        else
                        {
                            // Capture does not exist.
                            replacementBuilder.Append('$');
                            i--;
                        }
                    }
                    else
                    {
                        // Unknown replacement pattern.
                        replacementBuilder.Append('$');
                        replacementBuilder.Append(c);
                    }
                }
                else
                    replacementBuilder.Append(c);
            }

            return replacementBuilder.ToString();
        }, globalSearch == true ? -1 : 1);

        return result;
    }

    /// <summary>
    /// Parses the flags parameter into an enum.
    /// </summary>
    /// <param name="flags"> Available flags, which may be combined, are:
    /// g (global search for all occurrences of pattern)
    /// i (ignore case)
    /// m (multiline search)
    /// s (dotAll – dot matches newlines)
    /// u (unicode)
    /// y (sticky)
    /// v (unicodeSets)
    /// d (hasIndices)</param>
    /// <returns> RegexOptions flags that correspond to the given flags. </returns>
    private static string BuildFlagsString(bool hasIndices, bool globalSearch, bool ignoreCase, bool multiline, bool dotAll, bool unicode, bool unicodeSets, bool sticky)
    {
        var builder = new StringBuilder(8);
        if (hasIndices)
            builder.Append('d');
        if (globalSearch)
            builder.Append('g');
        if (ignoreCase)
            builder.Append('i');
        if (multiline)
            builder.Append('m');
        if (dotAll)
            builder.Append('s');
        if (unicode)
            builder.Append('u');
        if (unicodeSets)
            builder.Append('v');
        if (sticky)
            builder.Append('y');
        return builder.ToString();
    }

    private static (RegexOptions Options, bool GlobalSearch, bool IgnoreCase, bool Multiline, bool DotAll, bool HasIndices, bool Sticky, bool Unicode, bool UnicodeSets, string NormalizedFlags) ParseFlags(string flags)
    {
        bool globalSearch = false;
        bool ignoreCase = false;
        bool multiline = false;
        bool dotAll = false;
        bool hasIndices = false;
        bool sticky = false;
        bool unicode = false;
        bool unicodeSets = false;

        var options = RegexOptions.ECMAScript;

        if (flags == null)
            return (options, globalSearch, ignoreCase, multiline, dotAll, hasIndices, sticky, unicode, unicodeSets, string.Empty);

        for (int i = 0; i < flags.Length; i++)
        {
            char flag = flags[i];
            if (flag == 'g')
            {
                if (globalSearch == true)
                    throw JSEngine.NewSyntaxError("The 'g' flag cannot be specified twice");
                globalSearch = true;
            }
            else if (flag == 'i')
            {
                if ((options & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase)
                    throw JSEngine.NewSyntaxError("The 'i' flag cannot be specified twice");
                options |= RegexOptions.IgnoreCase;
                ignoreCase = true;
            }
            else if (flag == 'm')
            {
                if ((options & RegexOptions.Multiline) == RegexOptions.Multiline)
                    throw JSEngine.NewSyntaxError("The 'm' flag cannot be specified twice");
                options |= RegexOptions.Multiline;
                multiline = true;
            }
            else if (flag == 's')
            {
                if (dotAll)
                    throw JSEngine.NewSyntaxError("The 's' flag cannot be specified twice");
                dotAll = true;
                // Singleline makes . match \n as well.
                // We remove ECMAScript mode because it does not support Singleline.
                options &= ~RegexOptions.ECMAScript;
                options |= RegexOptions.Singleline;
            }
            else if (flag == 'u')
            {
                if (unicode)
                    throw JSEngine.NewSyntaxError("The 'u' flag cannot be specified twice");
                if (unicodeSets)
                    throw JSEngine.NewSyntaxError("The 'u' and 'v' flags cannot be used together");
                options &= ~RegexOptions.ECMAScript;
                unicode = true;
            }
            else if (flag == 'v')
            {
                if (unicodeSets)
                    throw JSEngine.NewSyntaxError("The 'v' flag cannot be specified twice");
                if (unicode)
                    throw JSEngine.NewSyntaxError("The 'u' and 'v' flags cannot be used together");
                options &= ~RegexOptions.ECMAScript;
                unicodeSets = true;
            }
            else if (flag == 'y')
            {
                if (sticky)
                    throw JSEngine.NewSyntaxError("The 'y' flag cannot be specified twice");
                sticky = true;
            }
            else if (flag == 'd')
            {
                if (hasIndices)
                    throw JSEngine.NewSyntaxError("The 'd' flag cannot be specified twice");
                hasIndices = true;
            }
            else
            {
                throw JSEngine.NewSyntaxError($"Unknown flag {flag}");
            }
        }

        return (options, globalSearch, ignoreCase, multiline, dotAll, hasIndices, sticky, unicode, unicodeSets,
            BuildFlagsString(hasIndices, globalSearch, ignoreCase, multiline, dotAll, unicode, unicodeSets, sticky));
    }

    /// <summary>
    /// Creates a .NET Regex object using the given pattern and options.
    /// Supports ES2025 inline pattern modifiers (§2.6) and duplicate
    /// named capturing groups (§2.7).
    /// </summary>
    public static (Regex, bool, bool, bool, bool, bool, bool, bool, string) CreateRegex(string pattern, string flags, out CaptureGroupMap captureMap)
    {
        captureMap = null;
        try
        {
            var (options, globalSearch, ignoreCase, multiline, dotAll, hasIndices, sticky, unicode, unicodeSets, normalizedFlags) = ParseFlags(flags);

            // ES2015+ Unicode (u) mode forbids identity escapes, octal escapes,
            // lone syntax characters ({ } ]), quantified assertions, etc. .NET's
            // engine is lenient about these, so validate the raw pattern here (the
            // same check the lexer applies to regex literals) before transforming.
            if (unicode && !RegExpUnicodeValidator.IsValidUnicodePattern(pattern))
                throw JSEngine.NewSyntaxError("Invalid regular expression: invalid pattern in Unicode mode");

            // BROILER-PATCH: Transform ES3 empty character classes and forward backreferences
            // for .NET compatibility (tests 89, 90)
            pattern = TransformES3Patterns(pattern);

            // Annex B IdentityEscape: in a non-Unicode regex `\` followed by a letter
            // that is not a recognised escape is the literal letter (e.g. /\C/ ≡ /C/,
            // /O\PQ/ ≡ /OPQ/). .NET rejects those, so drop the backslash. Skipped in
            // u/v mode, where the same escapes are syntax errors (rejected above) and
            // \p/\P are Unicode property escapes handled later.
            if (!unicode && !unicodeSets)
                pattern = TransformAnnexBIdentityEscapes(pattern);

            // ECMAScript \s must match all Unicode whitespace (Zs category + BOM + line terminators).
            // .NET's \s only covers ASCII whitespace, so expand to the full set.
            pattern = TransformUnicodeWhitespace(pattern, unicode || unicodeSets);

            // .NET IgnoreCase only applies simple (ToLower-based) case folding, so it
            // misses ECMAScript case equivalences such as µ↔Μ↔μ, the Greek symbol
            // variants and ſ↔s (in Unicode mode). Expand literals to their full
            // ECMAScript Canonicalize equivalence class. The class differs by mode:
            // non-Unicode uses toUppercase (with the ASCII guard, so ſ does NOT fold
            // to s), Unicode/Unicode-sets use case folding (so ſ DOES fold to s).
            if (ignoreCase)
                pattern = TransformUnicodeCaseFolding(pattern, unicode || unicodeSets);

            // Per sec-patterns-static-semantics-early-errors, an inline modifier
            // group `(?<add>-<remove>:…)` may only contain i/m/s flags, may not repeat
            // a flag within a group, may not list a flag in both the added and removed
            // sets (e.g. `(?s-s:a)`), and must add or remove at least one flag. .NET
            // accepts these, so validate first (independent of the mode switch below).
            ValidateInlineModifiers(pattern);

            // §2.6 — Detect inline pattern modifiers (?i:...) / (?-i:...) / (?ims:...) etc.
            // .NET ECMAScript mode does not support them, so switch to default mode.
            if ((options & RegexOptions.ECMAScript) != 0 && HasInlineModifiers(pattern))
                options &= ~RegexOptions.ECMAScript;

            // ES2015 §21.2.2.8: In Unicode mode, '.' matches any single
            // Unicode code point.  .NET's '.' only matches a single UTF-16
            // code unit, so expand it to also match surrogate pairs.
            if (unicode || unicodeSets)
            {
                // v-mode (unicodeSets) extended character classes with set
                // operations (A--B, A&&B), string literals \q{…}, and properties
                // of strings expand to a literal alternation that .NET can match.
                // Run before the property/surrogate transforms so the rewritten
                // fragment (plain classes + escaped literals) flows through them.
                if (unicodeSets)
                    pattern = TransformUnicodeSetsClasses(pattern);

                // Property escapes (\p{…}/\P{…}) are only valid in u/v mode.
                // Translate the General_Category dimension to .NET-compatible
                // short forms before the surrogate-aware class transforms run.
                pattern = TransformUnicodePropertyEscapes(pattern, unicodeSets);
                pattern = TransformUnicodeWordBoundaries(pattern, ignoreCase);
                pattern = TransformUnicodeDot(pattern, dotAll);
                // Transform character class escapes (\S, \W, \D) outside character
                // classes so they also match supplementary-plane code points (surrogate pairs).
                pattern = TransformUnicodeCharClassEscapes(pattern);
                // Transform character classes containing supplementary characters
                // (surrogate pairs) so they match as whole code points.
                pattern = TransformUnicodeCharClasses(pattern);
                // Finally, give lone surrogates and surrogate pairs that sit
                // *outside* a character class their ECMAScript code-point semantics
                // (a lone surrogate must not match a code unit that forms a pair; a
                // pair is one atom for the purpose of a following quantifier). Runs
                // last so the earlier class/dot transforms aren't disturbed.
                pattern = TransformUnicodeLoneSurrogates(pattern);
            }

            // ES §21.2.2.8 Atom: Without dotAll, '.' must not match any of
            // the four LineTerminator characters: \n \r \u2028 \u2029.
            // .NET's '.' (without Singleline) only excludes \n, so replace
            // remaining '.' with a class that excludes all four.
            // Skip when unicode/unicodeSets already handled the dots above,
            // or when dotAll (Singleline) is active and '.' should match all.
            if (!dotAll && !unicode && !unicodeSets)
                pattern = TransformDotLineTerminators(pattern);

            if ((options & RegexOptions.Multiline) == RegexOptions.Multiline)
            {
                // In the .NET Regex implementation with multiline mode:
                // '^' matches the start of the string or \n (positive lookbehind)
                // '$' matches the end of the string or \n (positive lookahead)
                // In Javascript, we want all three characters to also match \r in the same way they match \n.
                // Note: '.' is already handled above by TransformDotLineTerminators or TransformUnicodeDot.

                StringBuilder builder = null;
                int start = 0, end = -1;
                while (end < pattern.Length)
                {
                    end = pattern.IndexOfAny(['^', '$', '\\'], end + 1);
                    if (end == -1)
                        break;
                    
                    builder ??= new StringBuilder();
                    builder.Append(pattern.AsSpan(start, end - start));
                    
                    start = end + 1;
                    switch (pattern[end])
                    {
                        case '^':
                            // [^abc] is a thing. The ^ does NOT match the start of the line in this case.
                            if (end > 0 && pattern[end - 1] == '[')
                                builder.Append('^');
                            else
                                builder.Append(@"(?<=^|\r)");
                            break;

                        case '$':
                            builder.Append(@"(?=$|\r)");
                            break;

                        case '\\':
                            // $ is an anchor. \$ matches the literal dollar sign. \\$ is a backslash then an anchor.
                            if (end < pattern.Length - 1)
                            {
                                builder.Append(pattern[end]);
                                builder.Append(pattern[end + 1]);
                                start++;
                                end++;
                            }
                            break;
                    }
                }

                if (builder != null)
                {
                    builder.Append(pattern.AsSpan(start));
                    pattern = builder.ToString();
                }
            }

            // Final transform: rename capturing groups to synthetic, source-ordered
            // names so .NET's group numbering and duplicate-name handling match
            // ECMAScript. Runs last so the capture layout reflects the user's groups
            // (the earlier transforms only add non-capturing groups / lookarounds).
            pattern = RewriteCaptureGroups(pattern, out captureMap);

            return (new Regex(pattern, options), globalSearch, ignoreCase, multiline, hasIndices, sticky, unicode, unicodeSets, normalizedFlags);
        }
        catch (JSException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw JSEngine.NewSyntaxError(ex.Message);
        }
        catch
        {
            throw JSEngine.NewSyntaxError("Invalid regular expression");
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the pattern contains inline modifier
    /// groups such as <c>(?i:...)</c>, <c>(?-m:...)</c>, <c>(?si:...)</c>.
    /// These are not supported in .NET ECMAScript mode.
    /// </summary>
    // Validates ES2025 inline modifier groups `(?<add>-<remove>:…)` per the pattern
    // static-semantics early errors. Throws SyntaxError when: a flag is repeated
    // within a group; the same flag is both added and removed; a non-i/m/s flag
    // (e.g. uppercase `I`) appears; or the group adds and removes nothing (`(?-:…)`).
    private static void ValidateInlineModifiers(string pattern)
    {
        for (int i = 0; i + 2 < pattern.Length; i++)
        {
            if (pattern[i] == '\\')
            {
                i++; // skip the escaped character
                continue;
            }

            if (pattern[i] != '(' || pattern[i + 1] != '?')
                continue;

            // `(?` followed by an ASCII letter or `-` begins a modifier group: no
            // other `(?…` construct — (?:…), (?=…), (?!…), (?<name>…), (?<=…), (?<!…) —
            // starts that way, so a letter here is always an (attempted) modifier flag.
            int j = i + 2;
            var first = pattern[j];
            var firstIsLetter = (first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z');
            if (!firstIsLetter && first != '-')
                continue;

            var added = new HashSet<char>();
            var removed = new HashSet<char>();
            var current = added;
            bool sawDash = false;

            while (j < pattern.Length)
            {
                char c = pattern[j];
                if (c == 'i' || c == 'm' || c == 's')
                {
                    if (!current.Add(c))
                        throw JSEngine.NewSyntaxError(
                            "Invalid regular expression: a modifier flag must not be repeated within a modifier group");
                    j++;
                }
                else if (c == 'x')
                {
                    // .NET-only flag tolerated for passthrough; not a duplicate-checked
                    // JS modifier flag.
                    j++;
                }
                else if (c == '-' && !sawDash)
                {
                    sawDash = true;
                    current = removed;
                    j++;
                }
                else
                {
                    break;
                }
            }

            // A modifier group must be terminated by ':'. Anything else after the
            // flags (e.g. an uppercase `I`) is an invalid flag.
            if (j >= pattern.Length || pattern[j] != ':')
                throw JSEngine.NewSyntaxError(
                    "Invalid regular expression: invalid flag in modifier group");

            // `(?-:…)` adds and removes nothing.
            if (sawDash && added.Count == 0 && removed.Count == 0)
                throw JSEngine.NewSyntaxError(
                    "Invalid regular expression: a modifier group must add or remove at least one flag");

            foreach (var c in added)
            {
                if (removed.Contains(c))
                    throw JSEngine.NewSyntaxError(
                        "Invalid regular expression: a modifier flag must not appear in both the added and removed flag sets");
            }
        }
    }

    private static bool HasInlineModifiers(string pattern)
    {
        // Look for (?[imsx-]+: which is the inline-modifier syntax.
        for (int i = 0; i < pattern.Length - 3; i++)
        {
            if (pattern[i] == '(' && pattern[i + 1] == '?')
            {
                int j = i + 2;
                // Skip valid modifier characters
                while (j < pattern.Length && (pattern[j] == 'i' || pattern[j] == 'm' ||
                       pattern[j] == 's' || pattern[j] == 'x' || pattern[j] == '-'))
                {
                    j++;
                }

                // If followed by ':' and we consumed at least one modifier char, it's an inline modifier
                if (j > i + 2 && j < pattern.Length && pattern[j] == ':')
                    return true;
            }

            // Skip escaped characters
            if (pattern[i] == '\\' && i + 1 < pattern.Length)
                i++;
        }

        return false;
    }

    // Records the ECMAScript capture-group layout of a pattern whose groups were
    // renamed to synthetic, source-ordered names (bjsg1, bjsg2, …) so .NET keeps
    // them distinct and numbered left-to-right.
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

    // .NET merges capturing groups that share a name into a single numbered group
    // and orders all named groups after the unnamed ones — neither matches
    // ECMAScript, where every '(' is its own group numbered strictly left-to-right
    // and duplicate names (ES2025) are distinct groups living in mutually-exclusive
    // alternatives. Whenever a pattern contains any named group we therefore rename
    // *every* capturing group to a synthetic, position-encoded name (bjsg1, bjsg2,
    // …). .NET then numbers them 1..n in source order (so integer indexing and
    // numeric backreferences line up with ECMAScript) and keeps duplicates apart.
    // A \k<name> reference is rewritten to the synthetic name, or — for a
    // duplicated name — to a nested conditional that backreferences whichever of
    // the same-named groups participated in the match.
    private static string RewriteCaptureGroups(string pattern, out CaptureGroupMap map)
    {
        map = null;
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        // Pass 1: enumerate capturing groups in source order.
        var originalNames = new List<string> { null };           // [0] = whole match
        var nameToIndices = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var orderedNames = new List<string>();
        var anyNamed = false;

        ScanCaptureGroups(pattern, (index, name) =>
        {
            originalNames.Add(name);
            if (name == null)
                return;

            anyNamed = true;
            if (!nameToIndices.TryGetValue(name, out var list))
            {
                nameToIndices[name] = list = new List<int>();
                orderedNames.Add(name);
            }
            list.Add(index);
        });

        if (!anyNamed)
            return pattern;  // no named groups → .NET numbering already matches ECMAScript.

        // Pass 2: emit the rewritten pattern, renaming every capturing group and
        // resolving \k<name> references against the map built above.
        var sb = new StringBuilder(pattern.Length + 16);
        var inClass = false;
        var groupIndex = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                // Named backreference \k<name> (only meaningful outside a class).
                if (!inClass && pattern[i + 1] == 'k' && i + 2 < pattern.Length && pattern[i + 2] == '<')
                {
                    var nameEnd = pattern.IndexOf('>', i + 3);
                    if (nameEnd > i + 3 &&
                        nameToIndices.TryGetValue(pattern.Substring(i + 3, nameEnd - (i + 3)), out var refIndices))
                    {
                        sb.Append(BuildNamedBackref(refIndices));
                        i = nameEnd;
                        continue;
                    }
                }

                // Numbered backreference \N. Every group is being renamed to a
                // synthetic name, so a plain \N no longer resolves under .NET's
                // named/numbered group numbering; rewrite it to the matching
                // \k<bjsgN>. (Over-large / forward / self references were already
                // neutralised by TransformES3Patterns, so any \N reaching here is a
                // valid backward reference.)
                if (!inClass && pattern[i + 1] >= '1' && pattern[i + 1] <= '9')
                {
                    var refNum = 0;
                    var j = i + 1;
                    while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9')
                        refNum = refNum * 10 + (pattern[j++] - '0');

                    sb.Append("\\k<bjsg").Append(refNum).Append('>');
                    i = j - 1;
                    continue;
                }

                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (c == '[')
            {
                inClass = true;
                sb.Append(c);
                continue;
            }
            if (c == ']')
            {
                inClass = false;
                sb.Append(c);
                continue;
            }

            if (c == '(' && !inClass)
            {
                // Named group (?<name>…), but not a lookbehind (?<=…) / (?<!…).
                if (i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                    && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
                {
                    var nameEnd = pattern.IndexOf('>', i + 3);
                    groupIndex++;
                    sb.Append("(?<bjsg").Append(groupIndex).Append('>');
                    if (nameEnd > i + 3)
                        i = nameEnd;
                    continue;
                }

                // Any other (?…) construct is non-capturing.
                if (i + 1 < pattern.Length && pattern[i + 1] == '?')
                {
                    sb.Append(c);
                    continue;
                }

                // Plain unnamed capturing group.
                groupIndex++;
                sb.Append("(?<bjsg").Append(groupIndex).Append('>');
                continue;
            }

            sb.Append(c);
        }

        var namedGroups = new List<(string, List<int>)>(orderedNames.Count);
        foreach (var name in orderedNames)
            namedGroups.Add((name, nameToIndices[name]));
        map = new CaptureGroupMap(originalNames.ToArray(), namedGroups);
        return sb.ToString();
    }

    // Walks the pattern invoking onGroup(index, name) for each capturing group in
    // source order (name is null for unnamed groups). Mirrors the group-detection
    // in RewriteCaptureGroups' second pass so the assigned indices stay in lockstep.
    private static void ScanCaptureGroups(string pattern, Action<int, string> onGroup)
    {
        var inClass = false;
        var index = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++;
                continue;
            }
            if (c == '[')
            {
                inClass = true;
                continue;
            }
            if (c == ']')
            {
                inClass = false;
                continue;
            }
            if (c != '(' || inClass)
                continue;

            if (i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                var nameEnd = pattern.IndexOf('>', i + 3);
                var name = nameEnd > i + 3 ? pattern.Substring(i + 3, nameEnd - (i + 3)) : string.Empty;
                onGroup(++index, name);
                if (nameEnd > i + 3)
                    i = nameEnd;
                continue;
            }

            if (i + 1 < pattern.Length && pattern[i + 1] == '?')
                continue;  // non-capturing (?:…) / lookaround / modifier

            onGroup(++index, null);
        }
    }

    // Rewrites a JS \k<name> reference to its synthetic .NET form. A name shared by
    // several alternatives (ES2025 duplicate) becomes a nested conditional so the
    // backreference follows whichever same-named group participated; a name that
    // participated in no alternative falls through to an empty (always-matching)
    // branch, matching the ECMAScript "backreference to an unmatched group" rule.
    private static string BuildNamedBackref(List<int> indices)
    {
        if (indices.Count == 1)
            return $"\\k<bjsg{indices[0]}>";

        var acc = $"(?(bjsg{indices[^1]})\\k<bjsg{indices[^1]}>)";
        for (var k = indices.Count - 2; k >= 0; k--)
            acc = $"(?(bjsg{indices[k]})\\k<bjsg{indices[k]}>|{acc})";
        return acc;
    }

    /// <summary>
    /// In Unicode mode, \S, \W, \D outside character classes need to also
    /// match supplementary-plane code points (encoded as surrogate pairs).
    /// .NET's \S/\W/\D only match single UTF-16 code units, missing
    /// surrogate pairs that are non-space/non-word/non-digit code points.
    /// </summary>
    private static string TransformUnicodeCharClassEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        StringBuilder sb = null;
        int start = 0;
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '[' && !inClass)
            {
                inClass = true;
                continue;
            }

            if (inClass && c == ']')
            {
                inClass = false;
                continue;
            }
            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];
                if (!inClass && (next == 'S' || next == 'W' || next == 'D'))
                {
                    // Expand \S / \W / \D to also match surrogate pairs.
                    // A surrogate pair is always a non-whitespace, non-digit code point.
                    // For \W, supplementary code points are non-word characters.
                    // The surrogate-pair alternative must come FIRST so it is
                    // tried before \S/\W/\D, which would otherwise greedily
                    // match a lone high surrogate as a single code unit.
                    sb ??= new StringBuilder(pattern.Length + 64);
                    sb.Append(pattern, start, i - start);
                    sb.Append(@"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|");
                    sb.Append('\\');
                    sb.Append(next);
                    sb.Append(@")");
                    start = i + 2;
                }
                i++; // skip escaped char
                continue;
            }
        }

        if (sb == null)
            return pattern;

        sb.Append(pattern, start, pattern.Length - start);
        return sb.ToString();
    }

    /// <summary>
    /// In Unicode mode, ECMAScript word boundaries still use ECMAScript word
    /// characters, not .NET's broader Unicode categories. Replace \b and \B
    /// outside character classes with lookarounds over the ECMAScript set.
    /// </summary>
    private static string TransformUnicodeWordBoundaries(string pattern, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        var wordChars = ignoreCase
            ? @"A-Za-z0-9_\u017F\u212A"
            : "A-Za-z0-9_";
        var wordClass = $"[{wordChars}]";
        var nonWordClass = $"[^{wordChars}]";
        var boundary = $"(?:(?<=^)(?={wordClass})|(?<={nonWordClass})(?={wordClass})|(?<={wordClass})(?=$)|(?<={wordClass})(?={nonWordClass}))";
        var nonBoundary = $"(?:(?<=^)(?=$)|(?<=^)(?={nonWordClass})|(?<={nonWordClass})(?=$)|(?<={nonWordClass})(?={nonWordClass})|(?<={wordClass})(?={wordClass}))";

        StringBuilder sb = null;
        int start = 0;
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '[' && !inClass)
            {
                inClass = true;
                continue;
            }

            if (c == ']' && inClass)
            {
                inClass = false;
                continue;
            }

            if (c == '\\' && i + 1 < pattern.Length)
            {
                var next = pattern[i + 1];
                if (!inClass && (next == 'b' || next == 'B'))
                {
                    sb ??= new StringBuilder(pattern.Length + 64);
                    sb.Append(pattern, start, i - start);
                    sb.Append(next == 'b' ? boundary : nonBoundary);
                    start = i + 2;
                }

                i++;
            }
        }

        if (sb == null)
            return pattern;

        sb.Append(pattern, start, pattern.Length - start);
        return sb.ToString();
    }

    /// <summary>
    /// Transforms character classes containing supplementary-plane characters
    /// (represented as surrogate pairs) so that .NET regex treats them as
    /// whole code points rather than two independent code units.
    /// E.g. [𝌆a-z] → (?:\uD834\uDF06|[a-z])
    /// </summary>
    private static string TransformUnicodeCharClasses(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        StringBuilder sb = null;
        int start = 0;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++; // skip escaped char
                continue;
            }

            if (c == '[')
            {
                // Find the end of this character class
                int classStart = i;
                bool negated = false;
                i++; // skip '['
                if (i < pattern.Length && pattern[i] == '^')
                {
                    negated = true;
                    i++;
                }
                // Allow ] as first char in class
                if (i < pattern.Length && pattern[i] == ']')
                    i++;

                // Scan for supplementary chars (surrogate pairs) in this class, or
                // for a \D / \S / \W escape: those negated escapes include every
                // supplementary code point, so a non-negated class containing one
                // must also match a whole surrogate pair (not just the leading unit).
                var supplementaryChars = new List<string>();
                bool hasSupplementary = false;
                bool classMatchesSupplementary = false;

                int scanPos = i;
                while (scanPos < pattern.Length && pattern[scanPos] != ']')
                {
                    if (pattern[scanPos] == '\\' && scanPos + 1 < pattern.Length)
                    {
                        var esc = pattern[scanPos + 1];
                        if (!negated && (esc == 'D' || esc == 'S' || esc == 'W'))
                            classMatchesSupplementary = true;
                        scanPos += 2;
                        continue;
                    }
                    if (char.IsHighSurrogate(pattern[scanPos]) && scanPos + 1 < pattern.Length
                        && char.IsLowSurrogate(pattern[scanPos + 1]))
                    {
                        hasSupplementary = true;
                        break;
                    }
                    scanPos++;
                }

                if (!hasSupplementary && !classMatchesSupplementary)
                {
                    // Find end of class and skip
                    while (i < pattern.Length && pattern[i] != ']')
                    {
                        if (pattern[i] == '\\' && i + 1 < pattern.Length)
                            i++;
                        i++;
                    }
                    continue;
                }

                // We have supplementary chars - rebuild the class
                sb ??= new StringBuilder(pattern.Length + 64);
                sb.Append(pattern, start, classStart - start);

                // Collect BMP parts and supplementary chars
                var bmpParts = new StringBuilder();
                i = classStart + 1; // skip '['
                if (negated)
                    i++; // skip '^'
                // Handle ] as first char
                if (i < pattern.Length && pattern[i] == ']')
                {
                    bmpParts.Append(']');
                    i++;
                }

                while (i < pattern.Length && pattern[i] != ']')
                {
                    if (pattern[i] == '\\' && i + 1 < pattern.Length)
                    {
                        bmpParts.Append(pattern[i]);
                        bmpParts.Append(pattern[i + 1]);
                        i += 2;
                        continue;
                    }
                    if (char.IsHighSurrogate(pattern[i]) && i + 1 < pattern.Length 
                        && char.IsLowSurrogate(pattern[i + 1]))
                    {
                        supplementaryChars.Add(pattern.Substring(i, 2));
                        i += 2;
                        continue;
                    }
                    bmpParts.Append(pattern[i]);
                    i++;
                }

                // i now points at ']'
                if (negated)
                {
                    // [^𝌆a-z] → (?:(?!𝌆)[^a-z\uD800-\uDFFF]|(?!𝌆)[\uD800-\uDBFF][\uDC00-\uDFFF])
                    // Simplified: negative classes with supplementary chars are rare;
                    // use a negative lookahead approach
                    sb.Append("(?:");
                    foreach (var sp in supplementaryChars)
                    {
                        sb.Append("(?!");
                        sb.Append(sp);
                        sb.Append(')');
                    }
                    if (bmpParts.Length > 0)
                    {
                        sb.Append("(?:[^");
                        sb.Append(bmpParts);
                        sb.Append(@"\uD800-\uDFFF]|[\uD800-\uDBFF][\uDC00-\uDFFF])");
                    }
                    else
                    {
                        sb.Append(@"(?:[^\uD800-\uDFFF]|[\uD800-\uDBFF][\uDC00-\uDFFF])");
                    }
                    sb.Append(')');
                }
                else
                {
                    // [𝌆a-z] → (?:𝌆|[a-z]); a class containing \D/\S/\W also gets a
                    // leading surrogate-pair alternative so a whole pair is consumed
                    // as one code point before the single-unit class is tried, e.g.
                    // [\D] → (?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[\D]).
                    sb.Append("(?:");
                    if (classMatchesSupplementary)
                        sb.Append(@"[\uD800-\uDBFF][\uDC00-\uDFFF]|");
                    for (int si = 0; si < supplementaryChars.Count; si++)
                    {
                        sb.Append(supplementaryChars[si]);
                        sb.Append('|');
                    }
                    if (bmpParts.Length > 0)
                    {
                        sb.Append('[');
                        sb.Append(bmpParts);
                        sb.Append(']');
                    }
                    else
                    {
                        // Remove trailing |
                        sb.Length--;
                    }
                    sb.Append(')');
                }

                start = i + 1; // skip past ']'
            }
        }

        if (sb == null)
            return pattern;

        sb.Append(pattern, start, pattern.Length - start);
        return sb.ToString();
    }

    /// <summary>
    /// Replaces unescaped '.' outside character classes with a class that
    /// excludes all four ECMAScript LineTerminator characters:
    /// \n (U+000A), \r (U+000D), \u2028 (LS), \u2029 (PS).
    /// .NET's '.' only excludes \n; this method closes the gap for
    /// non-unicode, non-dotAll regexes.
    /// </summary>
    private static string TransformDotLineTerminators(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        StringBuilder sb = null;
        int start = 0;
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++; // skip escaped character
                continue;
            }

            if (c == '[' && !inClass)
            {
                inClass = true;
                continue;
            }

            if (inClass && c == ']')
            {
                inClass = false;
                continue;
            }

            if (c == '.' && !inClass)
            {
                sb ??= new StringBuilder(pattern.Length + 32);
                sb.Append(pattern, start, i - start);
                sb.Append(@"[^\n\r\u2028\u2029]");
                start = i + 1;
            }
        }

        if (sb == null)
            return pattern;

        sb.Append(pattern, start, pattern.Length - start);
        return sb.ToString();
    }

    /// <summary>
    /// In Unicode mode, '.' must match any single Unicode code point,
    /// including astral code points encoded as UTF-16 surrogate pairs.
    /// .NET's '.' only matches one UTF-16 code unit, so we expand
    /// unescaped '.' outside character classes to an alternation
    /// that also matches surrogate pairs.
    /// Also transforms negated character classes like [^a] to also
    /// match surrogate pairs as single code points, and converts
    /// \uHHHH\uHHHH surrogate pair escape sequences into the actual
    /// supplementary character.
    /// </summary>
    private static string TransformUnicodeDot(string pattern, bool dotAll)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        // First pass: collapse surrogate-pair \uHHHH\uHHHH escapes into
        // the actual supplementary plane character so .NET treats them as
        // a single code point.
        pattern = CollapseSurrogatePairEscapes(pattern);

        // Replacement for '.': a surrogate pair, lone surrogates, or BMP chars.
        // dotAll=true:  match any code point (surrogate pair, lone surrogate, or any BMP char).
        //               With RegexOptions.Singleline the inner '.' already matches everything.
        // dotAll=false: match any code point except LineTerminator (\n \r \u2028 \u2029).
        //               Lone surrogates are valid code points and must still match.
        string dotReplacement = dotAll
            ? @"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[^\uD800-\uDFFF])"
            : @"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[^\n\r\u2028\u2029\uD800-\uDFFF])";

        StringBuilder sb = null;
        int start = 0;
        int depth = 0; // track nested character class depth
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++; // skip escaped character
                continue;
            }

            if (c == '[' && !inClass)
            {
                inClass = true;
                depth = 1;
                continue;
            }

            if (inClass)
            {
                if (c == '[') depth++;
                if (c == ']')
                {
                    depth--;
                    if (depth <= 0) inClass = false;
                }
                continue;
            }

            if (c == '.' && !inClass)
            {
                sb ??= new StringBuilder(pattern.Length + 32);
                sb.Append(pattern, start, i - start);
                sb.Append(dotReplacement);
                start = i + 1;
            }
        }

        if (sb == null)
            return pattern;

        sb.Append(pattern, start, pattern.Length - start);
        return sb.ToString();
    }

    // In Unicode (u/v) mode a regex matches by code point, so a surrogate that
    // appears *outside* a character class needs help from .NET (which matches by
    // UTF-16 code unit):
    //   • a lone lead surrogate must match a lead code unit only when it is NOT
    //     followed by a trailing surrogate (otherwise the two form one code point),
    //   • a lone trail surrogate must match a trail code unit only when it is NOT
    //     preceded by a leading surrogate,
    //   • a lead immediately followed by a trail is a single code-point atom, so a
    //     following quantifier (?, +, *, {n}) must apply to the whole pair.
    // Surrogates inside character classes are handled by TransformUnicodeCharClasses;
    // this pass therefore skips class bodies and runs after it. By this point the
    // earlier transforms have already materialised \uHHHH escapes into real chars.
    private static string TransformUnicodeLoneSurrogates(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        var hasSurrogate = false;
        foreach (var ch in pattern)
        {
            if (char.IsSurrogate(ch))
            {
                hasSurrogate = true;
                break;
            }
        }
        if (!hasSurrogate)
            return pattern;

        var sb = new StringBuilder(pattern.Length + 16);
        var inClass = false;
        var depth = 0;

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            // Copy escapes verbatim — by now any surrogate escapes have been
            // collapsed to real chars, so a backslash here introduces some other
            // escape (\d, \(, …) that must pass through untouched.
            if (c == '\\' && i + 1 < pattern.Length)
            {
                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (!inClass && c == '[')
            {
                inClass = true;
                depth = 1;
                sb.Append(c);
                continue;
            }
            if (inClass)
            {
                if (c == '[')
                    depth++;
                else if (c == ']' && --depth <= 0)
                    inClass = false;
                sb.Append(c);
                continue;
            }

            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < pattern.Length && char.IsLowSurrogate(pattern[i + 1]))
                {
                    // Surrogate pair → atomic group so a following quantifier binds
                    // to the whole code point rather than just the trail unit.
                    sb.Append("(?:").Append(c).Append(pattern[i + 1]).Append(')');
                    i++;
                }
                else
                {
                    // Lone lead surrogate: match only when not part of a pair.
                    sb.Append(c).Append("(?![\uDC00-\uDFFF])");
                }
                continue;
            }

            if (char.IsLowSurrogate(c))
            {
                // Lone trail surrogate (a preceding lead would have been consumed as
                // a pair above): match only when not part of a pair.
                sb.Append("(?<![\uD800-\uDBFF])").Append(c);
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Finds consecutive \uHHHH\uHHHH escape sequences that form a
    /// UTF-16 surrogate pair and replaces them with the actual
    /// supplementary-plane character so .NET regex treats the pair
    /// as a single code point.
    /// </summary>
    private static string CollapseSurrogatePairEscapes(string pattern)
    {
        // Quick scan – only do work when the pattern contains \u escapes.
        if (!pattern.Contains("\\u"))
            return pattern;

        var sb = new StringBuilder(pattern.Length);

        for (int i = 0; i < pattern.Length; i++)
        {
            // Check for \uHHHH pattern
            if (i + 5 < pattern.Length && pattern[i] == '\\' && pattern[i + 1] == 'u'
                && IsHex(pattern[i + 2]) && IsHex(pattern[i + 3])
                && IsHex(pattern[i + 4]) && IsHex(pattern[i + 5]))
            {
                int hi = ParseHex4(pattern, i + 2);

                // Is this a high surrogate followed by \uHHHH low surrogate?
                if (hi >= 0xD800 && hi <= 0xDBFF
                    && i + 11 < pattern.Length
                    && pattern[i + 6] == '\\' && pattern[i + 7] == 'u'
                    && IsHex(pattern[i + 8]) && IsHex(pattern[i + 9])
                    && IsHex(pattern[i + 10]) && IsHex(pattern[i + 11]))
                {
                    int lo = ParseHex4(pattern, i + 8);
                    if (lo >= 0xDC00 && lo <= 0xDFFF)
                    {
                        // Emit the real supplementary character.
                        sb.Append(char.ConvertFromUtf32(0x10000 + ((hi - 0xD800) << 10) + (lo - 0xDC00)));
                        i += 11; // skip both \uHHHH sequences
                        continue;
                    }
                }

                // Not a surrogate pair – keep the single \uHHHH
                sb.Append((char)hi);
                i += 5;
                continue;
            }

            sb.Append(pattern[i]);
        }

        return sb.ToString();
    }

    private static bool IsHex(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static int ParseHex4(string s, int offset) =>
        (HexVal(s[offset]) << 12) | (HexVal(s[offset + 1]) << 8) | (HexVal(s[offset + 2]) << 4) | HexVal(s[offset + 3]);

    private static int HexVal(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => 0
    };

    // BROILER-PATCH: Transform ES3-specific regex patterns for .NET compatibility
    // Handles empty character classes, forward backreferences, and NUL escapes.
    /// <summary>
    /// General_Category long names / aliases → the .NET short form that
    /// <see cref="Regex"/> accepts (e.g. <c>Uppercase_Letter</c> → <c>Lu</c>).
    /// Keys are normalized (lower-cased, '_'/' ' stripped). Short forms map to
    /// themselves so that the <c>gc=Lu</c> form normalizes too.
    /// </summary>
    private static readonly Dictionary<string, string> GeneralCategoryNames = BuildGeneralCategoryNames();

    /// <summary>
    /// Emoji string-property names (UTS #51 "properties of strings") mapped to the
    /// <see cref="EmojiSequenceProperties"/> bit they select. These match
    /// multi-code-point sequences and are only valid with the <c>v</c> flag; there
    /// they expand to an alternation of the literal sequences (see
    /// <see cref="ExpandEmojiStringProperty"/>), backed by the bundled Unicode emoji data.
    ///
    /// Keys are stored in normalized form (lower-cased, '_'/' ' stripped) because
    /// lookups go through <c>NormalizeKey</c> — e.g. <c>RGI_Emoji</c> normalizes to
    /// <c>rgiemoji</c>.
    /// </summary>
    private static readonly Dictionary<string, EmojiSequenceProperties> EmojiStringPropertyNames = new(StringComparer.Ordinal)
    {
        ["rgiemoji"] = EmojiSequenceProperties.RgiEmoji,
        ["rgiemojiflagsequence"] = EmojiSequenceProperties.RgiEmojiFlagSequence,
        ["rgiemojimodifiersequence"] = EmojiSequenceProperties.RgiEmojiModifierSequence,
        ["rgiemojitagsequence"] = EmojiSequenceProperties.RgiEmojiTagSequence,
        ["rgiemojizwjsequence"] = EmojiSequenceProperties.RgiEmojiZwjSequence,
        ["basicemoji"] = EmojiSequenceProperties.BasicEmoji,
        ["emojikeycapsequence"] = EmojiSequenceProperties.EmojiKeycapSequence,
    };

    /// <summary>
    /// Cache of the expanded .NET alternation for each emoji string property, keyed by
    /// the <see cref="EmojiSequenceProperties"/> mask. Building the alternation walks the
    /// whole emoji trie (thousands of sequences for <c>RGI_Emoji</c>), so it is computed once.
    /// </summary>
    private static readonly Dictionary<EmojiSequenceProperties, string> EmojiAlternationCache = new();

    /// <summary>
    /// Code-point ranges for the Unicode scripts we can expand to regex without a
    /// full UCD. Keyed by normalized script name/alias. Not exhaustive — scripts
    /// not present here raise a clear "not supported" SyntaxError. Ranges are an
    /// approximation sufficient for common usage (notably the supplementary-plane
    /// Han ideographs the test262 v/u-flag tests probe).
    /// </summary>
    private static readonly Dictionary<string, (int Lo, int Hi)[]> ScriptRanges = new(StringComparer.Ordinal)
    {
        ["han"] = new (int, int)[]
        {
            (0x2E80, 0x2E99), (0x2E9B, 0x2EF3), (0x2F00, 0x2FD5),
            (0x3005, 0x3005), (0x3007, 0x3007), (0x3021, 0x3029), (0x3038, 0x303B),
            (0x3400, 0x4DBF), (0x4E00, 0x9FFF),
            (0xF900, 0xFA6D), (0xFA70, 0xFAD9),
            (0x20000, 0x2A6DF), (0x2A700, 0x2EBE0), (0x2F800, 0x2FA1D), (0x30000, 0x3134A),
        },
    };

    /// <summary>
    /// Code-point ranges for the Unicode binary properties we support directly.
    /// Keyed by normalized property name.
    /// </summary>
    private static readonly Dictionary<string, (int Lo, int Hi)[]> BinaryPropertyRanges = new(StringComparer.Ordinal)
    {
        ["ascii"] = new (int, int)[] { (0x00, 0x7F) },
        ["any"] = new (int, int)[] { (0x00, 0x10FFFF) },
        ["asciihexdigit"] = new (int, int)[] { (0x30, 0x39), (0x41, 0x46), (0x61, 0x66) },
    };

    private static Dictionary<string, string> BuildGeneralCategoryNames()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string shortName, params string[] names)
        {
            map[Normalize(shortName)] = shortName;
            foreach (var n in names)
                map[Normalize(n)] = shortName;
        }

        Add("L", "Letter");
        Add("Lu", "Uppercase_Letter");
        Add("Ll", "Lowercase_Letter");
        Add("Lt", "Titlecase_Letter");
        Add("Lm", "Modifier_Letter");
        Add("Lo", "Other_Letter");
        Add("M", "Mark", "Combining_Mark");
        Add("Mn", "Nonspacing_Mark");
        Add("Mc", "Spacing_Mark");
        Add("Me", "Enclosing_Mark");
        Add("N", "Number");
        Add("Nd", "Decimal_Number", "digit");
        Add("Nl", "Letter_Number");
        Add("No", "Other_Number");
        Add("P", "Punctuation", "punct");
        Add("Pc", "Connector_Punctuation");
        Add("Pd", "Dash_Punctuation");
        Add("Ps", "Open_Punctuation");
        Add("Pe", "Close_Punctuation");
        Add("Pi", "Initial_Punctuation");
        Add("Pf", "Final_Punctuation");
        Add("Po", "Other_Punctuation");
        Add("S", "Symbol");
        Add("Sm", "Math_Symbol");
        Add("Sc", "Currency_Symbol");
        Add("Sk", "Modifier_Symbol");
        Add("So", "Other_Symbol");
        Add("Z", "Separator");
        Add("Zs", "Space_Separator");
        Add("Zl", "Line_Separator");
        Add("Zp", "Paragraph_Separator");
        Add("C", "Other");
        Add("Cc", "Control", "cntrl");
        Add("Cf", "Format");
        Add("Cs", "Surrogate");
        Add("Co", "Private_Use");
        Add("Cn", "Unassigned");

        return map;

        static string Normalize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == '_' || ch == ' ')
                    continue;
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Minimal Unicode property-escape support (ES2018 <c>\p{…}</c> / <c>\P{…}</c>),
    /// only relevant in <c>u</c>/<c>v</c> mode. Translates the General_Category
    /// dimension to the short forms .NET understands — including the
    /// <c>General_Category=Value</c> and long-name forms that .NET rejects.
    ///
    /// Everything .NET already accepts (short categories, named blocks like
    /// <c>\p{IsBasicLatin}</c>) is left untouched, so this never changes the
    /// behavior of patterns that work today. Script and emoji string properties
    /// require a Unicode database that is not bundled yet and raise a clear
    /// "not supported" SyntaxError (see <see cref="UnsupportedUnicodePropertyNames"/>).
    /// </summary>
    private static string TransformUnicodePropertyEscapes(string pattern, bool unicodeSets)
    {
        if (pattern.IndexOf("\\p", StringComparison.Ordinal) < 0 &&
            pattern.IndexOf("\\P", StringComparison.Ordinal) < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        int i = 0;
        bool inClass = false;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                var next = pattern[i + 1];
                if (next == '\\')
                {
                    // Escaped backslash — copy both, do not treat the next char as an escape.
                    sb.Append(c).Append(next);
                    i += 2;
                    continue;
                }

                if ((next == 'p' || next == 'P') && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    var close = pattern.IndexOf('}', i + 3);
                    if (close > 0)
                    {
                        var inner = pattern.Substring(i + 3, close - (i + 3));
                        var translated = TranslateUnicodeProperty(next == 'P', inner, inClass, unicodeSets);
                        if (translated != null)
                        {
                            sb.Append(translated);
                            i = close + 1;
                            continue;
                        }
                        // Unrecognized — leave the original escape so .NET decides
                        // (preserves today's behavior for native forms like \p{L}).
                    }
                }

                // Any other escape: copy the backslash and its escaped character.
                sb.Append(c).Append(next);
                i += 2;
                continue;
            }

            // Track character-class context (classes do not nest in JS).
            if (c == '[' && !inClass)
                inClass = true;
            else if (c == ']' && inClass)
                inClass = false;

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the .NET replacement text for a single <c>\p{inner}</c> /
    /// <c>\P{inner}</c> escape, or <c>null</c> when it should be left untouched.
    /// Throws a SyntaxError for property classes that are recognized but not
    /// yet supported.
    /// </summary>
    private static string TranslateUnicodeProperty(bool negated, string inner, bool inClass, bool unicodeSets)
    {
        var eq = inner.IndexOf('=');
        var prefix = negated ? "\\P" : "\\p";

        static string NormalizeKey(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == '_' || ch == ' ')
                    continue;
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        if (eq >= 0)
        {
            // `Name=Value` form. .NET rejects this syntax entirely today, so it is
            // always safe to handle here.
            var name = NormalizeKey(inner.Substring(0, eq));
            var value = NormalizeKey(inner.Substring(eq + 1));

            if (name is "gc" or "generalcategory")
            {
                if (GeneralCategoryNames.TryGetValue(value, out var shortName))
                    return $"{prefix}{{{shortName}}}";

                throw NewUnsupportedPropertyError(inner);
            }

            if (name is "sc" or "script" or "scx" or "scriptextensions")
            {
                if (ScriptRanges.TryGetValue(value, out var ranges))
                {
                    var expanded = ExpandCodePointProperty(ranges, negated, inClass);
                    if (expanded != null)
                        return expanded;
                }

                throw NewUnsupportedPropertyError(inner);
            }

            // Unknown Name=Value — let .NET surface its own error.
            return null;
        }

        // Lone `\p{Value}` form.
        var lone = NormalizeKey(inner);

        if (GeneralCategoryNames.TryGetValue(lone, out var loneShort))
            return $"{prefix}{{{loneShort}}}";

        if (BinaryPropertyRanges.TryGetValue(lone, out var binaryRanges))
        {
            var expanded = ExpandCodePointProperty(binaryRanges, negated, inClass);
            if (expanded != null)
                return expanded;
            throw NewUnsupportedPropertyError(inner);
        }

        if (ScriptRanges.TryGetValue(lone, out var scriptRanges))
        {
            var expanded = ExpandCodePointProperty(scriptRanges, negated, inClass);
            if (expanded != null)
                return expanded;
            throw NewUnsupportedPropertyError(inner);
        }

        if (EmojiStringPropertyNames.TryGetValue(lone, out var emojiProperty))
        {
            // Properties of strings (UTS #51) match multi-code-point sequences, so they are
            // only meaningful with the `v` flag and cannot be negated or nested in a class
            // (a `[...]` set fragment cannot hold multi-character strings, and `\P` of a set
            // containing strings is a SyntaxError). In `v` mode, standalone, expand to an
            // alternation of the literal sequences; everything else stays a SyntaxError.
            if (unicodeSets && !negated && !inClass)
                return ExpandEmojiStringProperty(emojiProperty);

            throw NewUnsupportedPropertyError(inner);
        }

        // Anything else (native short categories like \p{L}, named blocks like
        // \p{IsBasicLatin}, or unknown lone names) is left for .NET to handle.
        return null;
    }

    private static JSException NewUnsupportedPropertyError(string inner)
        => JSEngine.NewSyntaxError(
            $"Unicode property escape '\\p{{{inner}}}' is not supported yet (requires Unicode script/sequence data)");

    /// <summary>
    /// Expands an emoji string property (UTS #51 property of strings, e.g.
    /// <c>RGI_Emoji</c> or <c>Emoji_Keycap_Sequence</c>) into a non-capturing alternation
    /// of the literal sequences drawn from the bundled Unicode emoji data. Alternatives are
    /// ordered longest-first so leftmost-longest matching picks the maximal sequence — e.g.
    /// the full ZWJ family rather than a leading single-code-point emoji. The result is cached
    /// per property because the alternation is large (thousands of sequences for RGI_Emoji).
    /// </summary>
    private static string ExpandEmojiStringProperty(EmojiSequenceProperties property)
    {
        lock (EmojiAlternationCache)
        {
            if (EmojiAlternationCache.TryGetValue(property, out var cached))
                return cached;

            var sequences = EmojiStringProperties.GetSequences(property);

            // Longest first, then ordinal for a stable pattern. Length here is in UTF-16 code
            // units, which is what the .NET regex consumes when matching the literal text.
            var ordered = new List<string>(sequences);
            ordered.Sort(static (a, b) =>
            {
                int byLength = b.Length.CompareTo(a.Length);
                return byLength != 0 ? byLength : string.CompareOrdinal(a, b);
            });

            var sb = new StringBuilder();
            sb.Append("(?:");
            for (int n = 0; n < ordered.Count; n++)
            {
                if (n > 0)
                    sb.Append('|');
                sb.Append(Regex.Escape(ordered[n]));
            }
            sb.Append(')');

            var expanded = sb.ToString();
            EmojiAlternationCache[property] = expanded;
            return expanded;
        }
    }

    /// <summary>
    /// Builds the .NET regex text matching a single code point in <paramref name="ranges"/>.
    /// Outside a character class this is a non-capturing group that handles both
    /// BMP code points and supplementary code points (via surrogate pairs); for
    /// <paramref name="negated"/> it matches one code point NOT in the set.
    /// Inside a character class only BMP, non-negated ranges can be expressed as
    /// raw class fragments; anything else returns <c>null</c> (caller errors).
    /// </summary>
    private static string ExpandCodePointProperty((int Lo, int Hi)[] ranges, bool negated, bool inClass)
    {
        if (inClass)
        {
            if (negated)
                return null; // a negated class fragment cannot be nested inside [...]

            var fragment = new StringBuilder();
            foreach (var (lo, hi) in ranges)
            {
                if (hi > 0xFFFF)
                    return null; // surrogate pairs cannot appear inside [...]
                AppendClassRange(fragment, lo, hi);
            }
            return fragment.ToString();
        }

        var positive = BuildPositiveCodePointMatcher(ranges);
        if (!negated)
            return positive;

        // Match one code point that is NOT in the set: reject the set, then consume
        // a full code point (a surrogate pair or any single code unit).
        return $"(?:(?!{positive})(?:[\\uD800-\\uDBFF][\\uDC00-\\uDFFF]|[\\s\\S]))";
    }

    private static string BuildPositiveCodePointMatcher((int Lo, int Hi)[] ranges)
    {
        var bmp = new StringBuilder();
        var supplementary = new List<string>();

        foreach (var (lo, hi) in ranges)
        {
            if (lo <= 0xFFFF)
            {
                AppendClassRange(bmp, lo, System.Math.Min(hi, 0xFFFF));
                if (hi <= 0xFFFF)
                    continue;
                AppendSupplementaryRange(supplementary, 0x10000, hi);
            }
            else
            {
                AppendSupplementaryRange(supplementary, lo, hi);
            }
        }

        var sb = new StringBuilder("(?:");
        var first = true;
        if (bmp.Length > 0)
        {
            sb.Append('[').Append(bmp).Append(']');
            first = false;
        }
        foreach (var alt in supplementary)
        {
            if (!first)
                sb.Append('|');
            sb.Append(alt);
            first = false;
        }
        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Decomposes a supplementary-plane code-point range [lo, hi] into UTF-16
    /// surrogate-pair regex alternatives (.NET regex matches code units).
    /// </summary>
    private static void AppendSupplementaryRange(List<string> alts, int lo, int hi)
    {
        int highLo = 0xD800 + ((lo - 0x10000) >> 10);
        int lowLo = 0xDC00 + ((lo - 0x10000) & 0x3FF);
        int highHi = 0xD800 + ((hi - 0x10000) >> 10);
        int lowHi = 0xDC00 + ((hi - 0x10000) & 0x3FF);

        if (highLo == highHi)
        {
            alts.Add(Unit(highLo) + UnitRange(lowLo, lowHi));
            return;
        }

        alts.Add(Unit(highLo) + UnitRange(lowLo, 0xDFFF));
        if (highHi - highLo >= 2)
            alts.Add(UnitRange(highLo + 1, highHi - 1) + UnitRange(0xDC00, 0xDFFF));
        alts.Add(Unit(highHi) + UnitRange(0xDC00, lowHi));

        static string Unit(int u) => $"\\u{u:X4}";
        static string UnitRange(int lo, int hi) => lo == hi ? Unit(lo) : $"[{Unit(lo)}-{Unit(hi)}]";
    }

    private static void AppendClassRange(StringBuilder sb, int lo, int hi)
    {
        if (lo == hi)
            sb.Append(EncodeBmpScalar(lo));
        else
            sb.Append(EncodeBmpScalar(lo)).Append('-').Append(EncodeBmpScalar(hi));

        static string EncodeBmpScalar(int cp) => cp <= 0xFF ? $"\\x{cp:X2}" : $"\\u{cp:X4}";
    }

    // Letters that keep their special meaning after a backslash in a non-Unicode
    // regex (character-class escapes, control escapes, boundaries, hex/unicode, the
    // named backreference \k and \c). Any other ASCII letter is an Annex B
    // IdentityEscape that denotes the literal letter.
    private const string RecognizedEscapeLetters = "bBcdDfknrsStuvwWx";

    private static string TransformAnnexBIdentityEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf('\\') < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];
                bool isLetter = (next >= 'a' && next <= 'z') || (next >= 'A' && next <= 'Z');
                if (!inClass && isLetter && RecognizedEscapeLetters.IndexOf(next) < 0)
                {
                    sb.Append(next); // IdentityEscape → literal letter
                    i++;
                    continue;
                }

                sb.Append(c).Append(next);
                i++;
                continue;
            }

            if (c == '[') inClass = true;
            else if (c == ']') inClass = false;
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string TransformES3Patterns(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        // Pass 1: Count total capturing groups for forward backreference detection
        int totalGroups = CountCapturingGroups(pattern);

        var sb = new StringBuilder(pattern.Length + 8);
        bool inClass = false;
        int groupsSeen = 0;
        // Capturing groups whose '(' has been seen but whose ')' has not — i.e. the
        // groups the current position is lexically nested inside. A backreference to
        // one of these is a self/ancestor reference, which always matches the empty
        // string in ECMAScript (the group is mid-match, and is reset on every
        // quantifier iteration). groupStack records the number for each open group
        // (0 = a non-capturing/lookaround group).
        var groupStack = new Stack<int>();
        var openCapturing = new HashSet<int>();

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];
                if (!inClass && next >= '1' && next <= '9')
                {
                    // Backreference \N — check if it's a forward reference
                    int refNum = 0;
                    int j = i + 1;

                    while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9')
                    {
                        refNum = refNum * 10 + (pattern[j] - '0');
                        j++;
                    }

                    if (refNum > totalGroups)
                    {
                        // Reference to a group that does not exist. In Annex B this
                        // DecimalEscape is not a backreference — `\8`/`\9` (and any
                        // over-large number) degrade to the literal digit characters
                        // (an IdentityEscape), e.g. `/7\89/` matches "789". .NET would
                        // instead reject the undefined-group reference.
                        sb.Append(pattern, i + 1, j - (i + 1));
                        i = j - 1;
                        continue;
                    }

                    if (openCapturing.Contains(refNum))
                    {
                        // Self/ancestor reference: the referenced group is still being
                        // matched, so per ECMAScript it always matches the empty string.
                        // .NET would instead reuse the previous iteration's capture
                        // (e.g. /(z\1){3}/ on "zzz"), so emit an empty group here.
                        sb.Append("(?:)");
                        i = j - 1;
                        continue;
                    }

                    if (refNum > groupsSeen)
                    {
                        // Forward reference to not-yet-captured group — matches empty string per ES3
                        sb.Append("(?:)");
                        i = j - 1;
                        continue;
                    }

                    // Normal backreference — pass through
                    sb.Append(pattern, i, j - i);
                    i = j - 1;
                    
                    continue;
                }
                
                if (next == '0')
                {
                    // \0 — NUL escape. Check if followed by an octal digit.
                    if (i + 2 < pattern.Length && pattern[i + 2] >= '0' && pattern[i + 2] <= '7')
                    {
                        // \0N — octal escape, pass through to .NET
                        sb.Append(c);
                        continue;
                    }

                    // \0 alone — NUL character. Use \x00 for .NET compatibility.
                    sb.Append("\\x00");
                    i++; // skip the '0'
                    continue;
                }

                if (next == 'c')
                {
                    // Annex B control escapes. `\cA`…`\cZ` (and lowercase) are
                    // ordinary ControlEscapes handled by .NET, so pass them
                    // through. `\c` followed by anything else is the Annex B
                    // extension and .NET rejects it:
                    //   • inside a class, `\c` + DecimalDigit/`_` is a control
                    //     character whose value is the letter's code point mod 32;
                    //   • everywhere else, the `\` is a literal backslash and `c`
                    //     is a literal `c` (ClassEscape/AtomEscape fall-through).
                    char after = i + 2 < pattern.Length ? pattern[i + 2] : '\0';
                    bool isControlLetter = (after >= 'A' && after <= 'Z') || (after >= 'a' && after <= 'z');
                    if (!isControlLetter)
                    {
                        if (inClass && ((after >= '0' && after <= '9') || after == '_'))
                        {
                            sb.Append("\\u").Append(((int)(after % 32)).ToString("x4"));
                            i += 2; // consumed '\', 'c', and the control letter
                            continue;
                        }

                        // Literal backslash + literal 'c'; the following character
                        // is processed normally on the next iteration.
                        sb.Append("\\\\c");
                        i++; // consumed '\' and 'c'
                        continue;
                    }
                }

                // Other escapes — pass through
                sb.Append(c);
                sb.Append(next);
                i++;
                continue;
            }

            if (inClass)
            {
                if (c == ']')
                    inClass = false;

                sb.Append(c);
                continue;
            }

            if (c == '[')
            {
                // Check for ES3 empty character class [] or [^]
                if (i + 1 < pattern.Length && pattern[i + 1] == ']')
                {
                    // [] — empty character class, matches nothing
                    sb.Append("[^\\s\\S]");
                    i++; // skip ']'
                    continue;
                }

                if (i + 2 < pattern.Length && pattern[i + 1] == '^' && pattern[i + 2] == ']')
                {
                    // [^] — complement of empty class, matches any character
                    sb.Append("[\\s\\S]");
                    i += 2; // skip '^]'
                    continue;
                }

                inClass = true;
                sb.Append(c);
                continue;
            }

            if (c == '(')
            {
                var capturing = IsCapturingGroupStart(pattern, i);
                if (capturing)
                {
                    groupsSeen++;
                    groupStack.Push(groupsSeen);
                    openCapturing.Add(groupsSeen);
                }
                else
                {
                    groupStack.Push(0);
                }
            }
            else if (c == ')' && groupStack.Count > 0)
            {
                var closed = groupStack.Pop();
                if (closed > 0)
                    openCapturing.Remove(closed);
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    // Count the total number of capturing groups in the pattern
    // True when pattern[i] == '(' opens a capturing group: a plain '(', or a named
    // group '(?<name>' — but not a lookbehind '(?<=' / '(?<!', a non-capturing
    // group '(?:', a lookahead '(?=' / '(?!', or an inline modifier '(?i)'.
    private static bool IsCapturingGroupStart(string pattern, int i)
    {
        if (i + 1 >= pattern.Length || pattern[i + 1] != '?')
            return true;

        return i + 2 < pattern.Length && pattern[i + 2] == '<'
            && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!'));
    }

    private static int CountCapturingGroups(string pattern)
    {
        int count = 0;
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                i++; // skip escaped char
                continue;
            }

            if (inClass)
            {
                if (c == ']') inClass = false;
                continue;
            }

            if (c == '[') { inClass = true; continue; }
            if (c == '(' && IsCapturingGroupStart(pattern, i))
            {
                count++;
            }
        }

        return count;
    }

    public override string ToString() => $"/{pattern}/{flags}";

    /// <summary>
    /// ECMAScript \s must match all Unicode whitespace (Zs category + BOM + line terminators).
    /// .NET's \s only covers ASCII whitespace, so replace \s and \S with the full set.
    /// </summary>
    private static string TransformUnicodeWhitespace(string pattern, bool unicodeMode = false)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        // Quick check: does the pattern contain \s or \S at all?
        int idx = pattern.IndexOf('\\');
        if (idx < 0)
            return pattern;

        bool hasEscape = false;
        for (int i = idx; i < pattern.Length - 1; i++)
        {
            if (pattern[i] == '\\' && (pattern[i + 1] == 's' || pattern[i + 1] == 'S'))
            {
                hasEscape = true;
                break;
            }
        }

        if (!hasEscape)
            return pattern;

        // Full ECMAScript WhiteSpace + LineTerminator character set (without surrounding brackets):
        const string esWhitespaceChars = @"\t\n\v\f\r \xA0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000\uFEFF";

        var sb = new StringBuilder(pattern.Length + 32);
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];

                if (next == 's' || next == 'S')
                {
                    if (inClass)
                    {
                        // Inside [...]: expand inline without wrapping brackets
                        // \S inside a class can't be negated inline, so we switch to subtraction syntax isn't
                        // available; instead leave \S as-is (it still covers non-ASCII via .NET)
                        if (next == 's')
                            sb.Append(esWhitespaceChars);
                        else
                            sb.Append(@"\S"); // can't negate inline; .NET \S is close enough inside classes
                    }
                    else
                    {
                        if (next == 's')
                            sb.Append('[').Append(esWhitespaceChars).Append(']');
                        else
                            AppendEsNonWhitespace(sb, esWhitespaceChars, unicodeMode);
                    }
                    i++; // skip the s/S
                    continue;
                }

                // Pass through other escapes
                sb.Append(c);
                sb.Append(next);
                i++;
                continue;
            }

            if (!inClass && c == '[')
            {
                // ES \S inside a class can't be expressed inline (it differs from
                // .NET's \S by U+FEFF and U+0085), so rewrite the whole-class forms
                // that consist solely of \S: [\S] == \S, and [^\S] == \s.
                if (i + 3 < pattern.Length && pattern[i + 1] == '\\' && pattern[i + 2] == 'S' && pattern[i + 3] == ']')
                {
                    AppendEsNonWhitespace(sb, esWhitespaceChars, unicodeMode);
                    i += 3;
                    continue;
                }

                if (i + 4 < pattern.Length && pattern[i + 1] == '^' && pattern[i + 2] == '\\' && pattern[i + 3] == 'S' && pattern[i + 4] == ']')
                {
                    sb.Append('[').Append(esWhitespaceChars).Append(']');
                    i += 4;
                    continue;
                }

                inClass = true;
            }
            else if (inClass && c == ']')
                inClass = false;

            sb.Append(c);
        }

        return sb.ToString();
    }

    // Emits the ECMAScript \S (a single non-whitespace code point). In Unicode mode
    // the surrogate-pair branch comes first so a supplementary-plane code point is
    // matched as a whole rather than as a lone high surrogate.
    private static void AppendEsNonWhitespace(StringBuilder sb, string esWhitespaceChars, bool unicodeMode)
    {
        if (unicodeMode)
            // A valid surrogate pair is one (non-whitespace) code point and is tried
            // first; otherwise any non-whitespace code unit, INCLUDING a lone
            // surrogate (an unpaired surrogate is itself a non-whitespace code point
            // in ECMAScript, so \S must match it). The pair alternative being first
            // means a real pair is never split by the single-unit fallback.
            sb.Append(@"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[^")
              .Append(esWhitespaceChars)
              .Append(@"])");
        else
            sb.Append("[^").Append(esWhitespaceChars).Append(']');
    }

    /// <summary>
    /// .NET IgnoreCase regex doesn't handle several Unicode CaseFolding pairs.
    /// This method expands literal characters and \uNNNN escapes in the pattern
    /// to include their missing case-fold equivalents so that case-insensitive
    /// matching conforms to ECMAScript Canonicalize semantics.
    /// </summary>
    private static string TransformUnicodeCaseFolding(string pattern, bool unicode)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        StringBuilder sb = null;
        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];

                if (next == 'u' && i + 5 < pattern.Length
                    && IsHexDigit(pattern[i + 2]) && IsHexDigit(pattern[i + 3])
                    && IsHexDigit(pattern[i + 4]) && IsHexDigit(pattern[i + 5]))
                {
                    int cp = HexValue(pattern[i + 2]) << 12 | HexValue(pattern[i + 3]) << 8
                           | HexValue(pattern[i + 4]) << 4 | HexValue(pattern[i + 5]);
                    var equiv = GetCaseFoldEquivalents((char)cp, unicode);
                    if (equiv != null)
                    {
                        sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                        AppendWithEquivalents(sb, (char)cp, equiv, inClass);
                        i += 5;
                        continue;
                    }

                    sb?.Append(pattern, i, 6);
                    i += 5;
                    continue;
                }

                if (next == 'x' && i + 3 < pattern.Length
                    && IsHexDigit(pattern[i + 2]) && IsHexDigit(pattern[i + 3]))
                {
                    int cp = HexValue(pattern[i + 2]) << 4 | HexValue(pattern[i + 3]);
                    var equiv = GetCaseFoldEquivalents((char)cp, unicode);
                    if (equiv != null)
                    {
                        sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                        AppendWithEquivalents(sb, (char)cp, equiv, inClass);
                        i += 3;
                        continue;
                    }

                    sb?.Append(pattern, i, 4);
                    i += 3;
                    continue;
                }

                // Pass through other escapes
                sb?.Append(c);
                sb?.Append(next);
                i++;
                continue;
            }

            if (!inClass && c == '[')
            {
                inClass = true;
                sb?.Append(c);
                continue;
            }

            if (inClass && c == ']')
            {
                inClass = false;
                sb?.Append(c);
                continue;
            }

            // Check literal characters
            var litEquiv = GetCaseFoldEquivalents(c, unicode);
            if (litEquiv != null)
            {
                sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                AppendWithEquivalents(sb, c, litEquiv, inClass);
                continue;
            }

            sb?.Append(c);
        }

        return sb?.ToString() ?? pattern;
    }

    private static void AppendWithEquivalents(StringBuilder sb, char original, char[] equivalents, bool inClass)
    {
        if (inClass)
        {
            sb.Append(original);
            foreach (var eq in equivalents)
                sb.Append(eq);
        }
        else
        {
            sb.Append('[');
            sb.Append(original);
            foreach (var eq in equivalents)
                sb.Append(eq);
            sb.Append(']');
        }
    }

    /// <summary>
    /// Returns the other characters that match <paramref name="c"/> case-insensitively
    /// under ECMAScript Canonicalize but that .NET's (simple, ToLower-based) IgnoreCase
    /// does not fold. Returns null when there are no such extra equivalents.
    /// </summary>
    /// <param name="unicode">
    /// true for the u/v flags (case folding; the long s folds to s); false otherwise
    /// (toUppercase with the ASCII guard, so the long s does NOT fold to s).
    /// </param>
    private static char[] GetCaseFoldEquivalents(char c, bool unicode)
    {
        var map = unicode ? UnicodeCaseFoldEquivalents.Value : NonUnicodeCaseFoldEquivalents.Value;
        return map.TryGetValue(c, out var equivalents) ? equivalents : null;
    }

    // Reverse maps from a character to the other members of its ECMAScript
    // Canonicalize equivalence class. Built once by grouping every BMP code unit by
    // its canonical key; only classes with more than one member AND at least one
    // non-ASCII member are kept (ASCII-only classes such as {a, A} are folded
    // correctly by .NET's own IgnoreCase, so expanding them would only bloat the
    // pattern). Surrogate code units are excluded.
    private static readonly Lazy<Dictionary<char, char[]>> NonUnicodeCaseFoldEquivalents =
        new(() => BuildCaseFoldEquivalents(unicode: false));
    private static readonly Lazy<Dictionary<char, char[]>> UnicodeCaseFoldEquivalents =
        new(() => BuildCaseFoldEquivalents(unicode: true));

    private static Dictionary<char, char[]> BuildCaseFoldEquivalents(bool unicode)
    {
        var groups = new Dictionary<char, List<char>>();

        for (int u = 0; u <= 0xFFFF; u++)
        {
            // Skip surrogate code units: they are not stand-alone characters.
            if (u >= 0xD800 && u <= 0xDFFF)
                continue;

            char ch = (char)u;
            char key = unicode ? CaseFoldKey(ch) : CanonicalizeKey(ch);

            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new List<char>(2);
            list.Add(ch);
        }

        var map = new Dictionary<char, char[]>();
        foreach (var list in groups.Values)
        {
            if (list.Count < 2)
                continue;

            bool hasNonAscii = false;
            foreach (var m in list)
            {
                if (m >= 128) { hasNonAscii = true; break; }
            }
            if (!hasNonAscii)
                continue;

            foreach (var member in list)
            {
                var others = new char[list.Count - 1];
                int k = 0;
                foreach (var m in list)
                {
                    if (m != member)
                        others[k++] = m;
                }
                map[member] = others;
            }
        }

        return map;
    }

    // ECMAScript Canonicalize for non-Unicode mode: the toUppercase of the code unit,
    // except that a code point >= 128 whose uppercase is an ASCII character is left
    // unchanged (so e.g. the long s U+017F is its own canonical form).
    private static char CanonicalizeKey(char ch)
    {
        char cu = char.ToUpperInvariant(ch);
        if (ch >= 128 && cu < 128)
            return ch;
        return cu;
    }

    // Simple case folding used by Canonicalize in Unicode mode. .NET does not expose
    // CaseFolding.txt directly, but toUppercase followed by toLowercase yields the same
    // canonical lowercase form for the affected characters.
    private static char CaseFoldKey(char ch) => char.ToLowerInvariant(char.ToUpperInvariant(ch));

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static int HexValue(char c) =>
        c >= '0' && c <= '9' ? c - '0' : (c >= 'a' ? c - 'a' + 10 : c - 'A' + 10);
}
