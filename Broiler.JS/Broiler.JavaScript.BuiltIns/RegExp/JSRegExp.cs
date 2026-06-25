using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;
using UnicodeEmoji.StringProperties;
using Broiler.Unicode.Properties;

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

            // A well-formed surrogate pair is a single (astral) code point. RegExp.escape
            // works on code points, and an astral letter/symbol is not a syntax,
            // punctuator or white-space character, so it is emitted literally. Only a LONE
            // surrogate is escaped (handled by TryAppendEscape's char.IsSurrogate branch).
            if (char.IsHighSurrogate(c) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
            {
                sb.Append(c);
                sb.Append(str[i + 1]);
                i++;
                continue;
            }

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
        // Whether the cursor is inside an unescaped character class `[...]`. A bare
        // `/` only needs escaping outside a class — inside one it is an ordinary
        // member and must be preserved verbatim (test262 sm/RegExp/escape).
        var inClass = false;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            // A backslash begins an escape sequence; consume the following code unit
            // together with it so the escaped character is never reinterpreted. This
            // keeps an escaped backslash (`\\`) distinct from a backslash that escapes
            // a following raw line terminator (test262 sm/RegExp/escape).
            if (c == '\\' && i + 1 < pattern.Length)
            {
                var next = pattern[i + 1];
                i++;

                // `\<LineTerminator>` (an identity escape of a raw line terminator)
                // must serialize without a literal line terminator: emit just the
                // `\n` / `\r` / `\u2028` / `\u2029` escape, which matches the same
                // code point, instead of keeping the backslash and the raw character.
                if (TryAppendLineTerminatorEscape(sb, next))
                    continue;

                sb.Append('\\');
                if (char.IsSurrogate(next))
                    AppendUnicodeEscape(sb, next);
                else
                    sb.Append(next);
                continue;
            }

            switch (c)
            {
                case '[':
                    inClass = true;
                    break;
                case ']':
                    inClass = false;
                    break;
                case '/' when !inClass:
                    // A bare forward slash outside a character class must be escaped so
                    // the serialized source is safe between the delimiting slashes of a
                    // regular expression literal.
                    sb.Append(@"\/");
                    continue;
            }

            // A bare line terminator likewise cannot appear literally in the source.
            if (TryAppendLineTerminatorEscape(sb, c))
                continue;

            if (char.IsSurrogate(c))
            {
                AppendUnicodeEscape(sb, c);
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    // Appends the escaped form of a line terminator (so the RegExp source never
    // contains a raw <LF>, <CR>, <LS>, or <PS>). Returns false for any other char.
    private static bool TryAppendLineTerminatorEscape(StringBuilder sb, char c)
    {
        switch (c)
        {
            case '\n':
                sb.Append(@"\n");
                return true;
            case '\r':
                sb.Append(@"\r");
                return true;
            case '\u2028':
            case '\u2029':
                AppendUnicodeEscape(sb, c);
                return true;
            default:
                return false;
        }
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

            var advanced = nextLastIndex + 1;
            if (fullUnicode && advanced < inputString.Length
                && char.IsHighSurrogate(inputString[nextLastIndex])
                && char.IsLowSurrogate(inputString[advanced]))
                advanced++;

            SetObservableLastIndex(advanced);
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
            // A non-callable replacement is coerced with the spec ToString (StringValue), which
            // throws for an object yielding no primitive — not the lenient CLR ToString.
            return Replace(input, replaceFunction.StringValue);

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

            // ES2025 permits two named groups to share a name only when they are in
            // separate alternatives of a disjunction; the same name twice in a single
            // alternative (`(?<x>a)(?<x>b)`) is a SyntaxError.
            ValidateDuplicateNamedGroups(pattern);

            // Each (?<name>…) GroupSpecifier must be a valid RegExpIdentifierName; an exotic name
            // such as (?<🦊>…) or (?<𝟚the>…) (an ID_Continue-only first character) is a SyntaxError.
            ValidateNamedGroupNames(pattern);

            // BROILER-PATCH: Transform ES3 empty character classes and forward backreferences
            // for .NET compatibility (tests 89, 90)
            pattern = TransformES3Patterns(pattern);

            // Annex B IdentityEscape: in a non-Unicode regex `\` followed by a letter
            // that is not a recognised escape is the literal letter (e.g. /\C/ ≡ /C/,
            // /O\PQ/ ≡ /OPQ/). .NET rejects those, so drop the backslash. Skipped in
            // u/v mode, where the same escapes are syntax errors (rejected above) and
            // \p/\P are Unicode property escapes handled later.
            if (!unicode && !unicodeSets)
            {
                pattern = TransformAnnexBIdentityEscapes(pattern);
                // Annex B: a `-` adjacent to a CharacterClassEscape in a class is a
                // literal (`[--\d]` = `-` or a digit), but .NET rejects the range.
                pattern = NeutralizeAnnexBClassRangeDashes(pattern);
                // Annex B: with no named groups, `\k<a>` is a literal (`k<a>`), not a
                // backreference to an undefined group (which .NET rejects).
                pattern = NeutralizeAnnexBUndefinedNamedBackref(pattern);
            }

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
            var hasInlineModifiers = HasInlineModifiers(pattern);
            if ((options & RegexOptions.ECMAScript) != 0 && hasInlineModifiers)
                options &= ~RegexOptions.ECMAScript;

            // ES2015 §21.2.2.8: In Unicode mode, '.' matches any single
            // Unicode code point.  .NET's '.' only matches a single UTF-16
            // code unit, so expand it to also match surrogate pairs.
            if (unicode || unicodeSets)
            {
                // The braced Unicode escape `\u{H..H}` is only valid in u/v mode. A
                // regex literal has it rewritten to a fixed-width `\uHHHH` escape by the
                // source scanner, but a `new RegExp(string, "u")` pattern reaches the
                // translator with the brace form intact — which .NET rejects. Convert it
                // up front (a surrogate-pair escape for a supplementary code point) so the
                // surrogate/class transforms below, and ultimately .NET, only ever see the
                // `\uHHHH` form.
                pattern = TransformBracedUnicodeEscapes(pattern);

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
                pattern = TransformUnicodePropertyEscapes(pattern, unicodeSets, ignoreCase);
                pattern = TransformUnicodeWordBoundaries(pattern, ignoreCase);
                // Inline modifiers disable .NET ECMAScript mode, so `\w`/`\W` need the ECMAScript word
                // set re-imposed (effective-ignoreCase aware). Only needed when modifiers are present —
                // a plain /u pattern keeps ECMAScript mode and its ASCII `\w`. Runs before the
                // surrogate-aware `\W` expansion below so each `\w`/`\W` is rewritten exactly once.
                if (hasInlineModifiers)
                    pattern = TransformUnicodeWordClassEscapes(pattern, ignoreCase);
                pattern = TransformUnicodeDot(pattern, dotAll);
                // Transform character class escapes (\S, \W, \D) outside character
                // classes so they also match supplementary-plane code points (surrogate pairs).
                pattern = TransformUnicodeCharClassEscapes(pattern);
                // Transform character classes containing supplementary characters
                // (surrogate pairs) so they match as whole code points. Under ignoreCase
                // the astral members/ranges are also expanded to their case-fold class
                // (.NET applies no astral case folding of its own).
                pattern = TransformUnicodeCharClasses(pattern, ignoreCase);
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
            // Inline modifier groups `(?s:...)` / `(?m:...)` / `(?-m:...)` change the
            // effective s/m flags for a region of the pattern, so the '.', '^' and '$'
            // rewrites must follow the flags in effect at each position rather than the
            // global ones. Handle the (non-unicode) modifier case with a single
            // scope-aware pass and skip the global rewrites below
            // (test262 RegExp/regexp-modifiers/*).
            if (hasInlineModifiers && !unicode && !unicodeSets)
            {
                pattern = TransformAnchorsAndDotsWithModifiers(pattern, dotAll, multiline);
                goto afterAnchorTransforms;
            }

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

        afterAnchorTransforms:

            // ECMAScript permits a quantifier count up to 2^53-1, but .NET rejects
            // any `{n}`/`{n,}`/`{n,m}` bound above Int32.MaxValue. Clamp such bounds
            // to Int32.MaxValue — a count that large can never be satisfied by a real
            // input, so the observable match behavior is unchanged.
            pattern = ClampLargeQuantifiers(pattern);

            // ECMAScript RepeatMatcher (22.2.2.3.1) discards a min=0 quantifier iteration
            // whose body matched the empty string, so a min=0 quantifier applied to a body
            // that consumes no input does exactly zero iterations and never sets the captures
            // inside it. .NET keeps those captures, so rewrite such a quantifier to `{0}`.
            pattern = RewriteZeroWidthMinZeroQuantifiers(pattern);

            // Final transform: rename capturing groups to synthetic, source-ordered
            // names so .NET's group numbering and duplicate-name handling match
            // ECMAScript. Runs last so the capture layout reflects the user's groups
            // (the earlier transforms only add non-capturing groups / lookarounds).
            pattern = RewriteCaptureGroups(pattern, unicode || unicodeSets, out captureMap);

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
    /// Clamps quantifier bounds (<c>{n}</c>, <c>{n,}</c>, <c>{n,m}</c>) whose decimal
    /// value exceeds <see cref="int.MaxValue"/> down to <c>2147483647</c>. ECMAScript
    /// allows counts up to 2^53-1 while .NET caps them at Int32.MaxValue; a bound that
    /// large is never satisfiable, so clamping preserves observable behavior. Literal
    /// braces (inside a character class, escaped, or not forming a valid quantifier)
    /// are left untouched.
    /// </summary>
    private static string ClampLargeQuantifiers(string pattern)
    {
        if (pattern.IndexOf('{') < 0)
            return pattern;

        // True when the decimal-digit run `s[start..end)` represents a value > Int32.MaxValue.
        static bool ExceedsInt32(string s, int start, int end)
        {
            while (start < end - 1 && s[start] == '0')
                start++; // ignore leading zeros (but keep at least one digit)
            var len = end - start;
            const string Max = "2147483647"; // int.MaxValue, 10 digits
            if (len != Max.Length)
                return len > Max.Length;
            return string.CompareOrdinal(s, start, Max, 0, Max.Length) > 0;
        }

        StringBuilder sb = null;
        bool inClass = false;
        int i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                sb?.Append(c).Append(pattern[i + 1]);
                i += 2;
                continue;
            }

            if (c == '[' && !inClass)
                inClass = true;
            else if (c == ']' && inClass)
                inClass = false;

            if (c == '{' && !inClass)
            {
                // Try to parse `{ digits (, digits?)? }`.
                int j = i + 1;
                int d1Start = j;
                while (j < pattern.Length && char.IsAsciiDigit(pattern[j])) j++;
                if (j > d1Start) // at least one digit
                {
                    int d1End = j;
                    int d2Start = -1, d2End = -1;
                    if (j < pattern.Length && pattern[j] == ',')
                    {
                        j++;
                        d2Start = j;
                        while (j < pattern.Length && char.IsAsciiDigit(pattern[j])) j++;
                        d2End = j;
                    }

                    if (j < pattern.Length && pattern[j] == '}')
                    {
                        bool clamp1 = ExceedsInt32(pattern, d1Start, d1End);
                        bool clamp2 = d2Start >= 0 && d2End > d2Start && ExceedsInt32(pattern, d2Start, d2End);
                        if (clamp1 || clamp2)
                        {
                            sb ??= new StringBuilder(pattern.Length).Append(pattern, 0, i);
                            sb.Append('{');
                            sb.Append(clamp1 ? "2147483647" : pattern.Substring(d1Start, d1End - d1Start));
                            if (d2Start >= 0)
                            {
                                sb.Append(',');
                                if (d2End > d2Start)
                                    sb.Append(clamp2 ? "2147483647" : pattern.Substring(d2Start, d2End - d2Start));
                            }
                            sb.Append('}');
                            i = j + 1;
                            continue;
                        }
                    }
                }
            }

            sb?.Append(c);
            i++;
        }

        return sb?.ToString() ?? pattern;
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
    private static string RewriteCaptureGroups(string pattern, bool unicode, out CaptureGroupMap map)
    {
        map = null;
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        // Pass 1: enumerate capturing groups in source order. Decode \uXXXX / \u{…}
        // escapes in named-group names BEFORE storing them so groups[name] lookups
        // (whose keys are JS strings — already-decoded code points) hit the captureMap.
        var originalNames = new List<string> { null };           // [0] = whole match
        var nameToIndices = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var orderedNames = new List<string>();
        var anyNamed = false;

        ScanCaptureGroups(pattern, (index, name) =>
        {
            // A GroupName's RegExpIdentifierName always uses Unicode escape rules — \u{…}
            // and \u-escaped surrogate pairs are valid even in a non-u/v regex — so decode
            // it with unicode rules regardless of the pattern's flags, matching
            // ValidateNamedGroupNames (test262: named-groups/non-unicode-property-names-valid).
            var decodedName = DecodeGroupName(name, unicode: true);
            originalNames.Add(decodedName);
            if (decodedName == null)
                return;

            anyNamed = true;
            if (!nameToIndices.TryGetValue(decodedName, out var list))
            {
                nameToIndices[decodedName] = list = new List<int>();
                orderedNames.Add(decodedName);
            }
            list.Add(index);
        });

        // No named groups → .NET's left-to-right numbering already matches ECMAScript,
        // so the rename is unnecessary UNLESS the pattern needs per-repetition capture
        // resets (a capturing group inside a quantified group). Emulating those resets
        // requires the synthetic bjsg names that the balancing-group reset prologue
        // (InjectQuantifierCaptureResets) references, so fall through to the rewrite.
        if (!anyNamed && !NeedsCaptureReset(pattern))
            return pattern;

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
                // Named backreference \k<name> (only meaningful outside a class). The
                // referenced name uses the same source encoding as the GroupSpecifier,
                // so decode \uXXXX / \u{…} before looking it up in the map.
                if (!inClass && pattern[i + 1] == 'k' && i + 2 < pattern.Length && pattern[i + 2] == '<')
                {
                    var nameEnd = pattern.IndexOf('>', i + 3);
                    if (nameEnd > i + 3)
                    {
                        // The referenced name uses the same always-Unicode escape rules as
                        // the GroupSpecifier it resolves against.
                        var refName = DecodeGroupName(pattern.Substring(i + 3, nameEnd - (i + 3)), unicode: true);
                        if (refName != null && nameToIndices.TryGetValue(refName, out var refIndices))
                        {
                            sb.Append(BuildNamedBackref(refIndices));
                            i = nameEnd;
                            continue;
                        }
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
        var anyDuplicate = false;
        foreach (var name in orderedNames)
        {
            var idxs = nameToIndices[name];
            namedGroups.Add((name, idxs));
            if (idxs.Count > 1)
                anyDuplicate = true;
        }
        map = new CaptureGroupMap(originalNames.ToArray(), namedGroups);

        var rewritten = sb.ToString();

        // A name shared by several groups (ES2025 duplicate named groups) is only
        // resolvable by a \k<name> backreference when its groups reset on each
        // quantifier repetition the way ECMAScript requires. .NET retains a group's
        // capture across repetitions, so emulate the reset for duplicate-named
        // patterns; ordinary patterns keep .NET's native behaviour.
        rewritten = InjectQuantifierCaptureResets(rewritten);

        return rewritten;
    }

    // Emulates ECMAScript's per-repetition capture reset: at the start of every
    // iteration of a quantified group, pop any stale capture of the synthetic
    // (bjsgN) groups declared inside it via a balancing group, i.e. run
    // "(?(bjsgN)(?<-bjsgN>))" before the group body. The reset must run whichever
    // alternative the iteration takes, so the original body is wrapped in a
    // non-capturing group and the reset is prepended ahead of it —
    // "(?:RESET(?:BODY)){2}" — otherwise the reset would bind to BODY's first
    // alternative only and a later iteration taking another branch (e.g. the `z`
    // in "(?:(?<a>x)|(?<a>y)|z){2}") would keep the stale capture, so a trailing
    // \k<a> backreference would wrongly still match. Only invoked for patterns that
    // contain duplicate named groups.
    // True when the pattern has a capturing group nested inside a quantified group, so
    // ECMAScript's per-repetition capture reset must be emulated (.NET otherwise retains
    // an inner group's capture across repetitions, e.g. /(z)((a+)?(b+)?(c))* /.exec(
    // "zaacbbbcac") leaves group 4 as "bbb" instead of undefined). Mirrors the group walk
    // in InjectQuantifierCaptureResets: a capture is counted by every enclosing group, and
    // a group quantified by * + ? or { with inner captures needs the reset.
    private static bool NeedsCaptureReset(string pattern)
    {
        var innerCounts = new List<int>();   // declared captures seen inside each open group
        var inClass = false;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\') { i++; continue; }
            if (inClass) { if (c == ']') inClass = false; continue; }
            if (c == '[') { inClass = true; continue; }

            if (c == '(')
            {
                var capturing = true;
                if (i + 1 < pattern.Length && pattern[i + 1] == '?')
                {
                    // (?<name>…) is capturing; (?<=…)/(?<!…) and (?:…)/(?=…)/(?!…)/… are not.
                    capturing = i + 2 < pattern.Length && pattern[i + 2] == '<'
                        && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!'));
                }

                if (capturing)
                    for (var f = 0; f < innerCounts.Count; f++)
                        innerCounts[f]++;

                innerCounts.Add(0);
                continue;
            }

            if (c == ')')
            {
                if (innerCounts.Count == 0)
                    continue;

                var inner = innerCounts[^1];
                innerCounts.RemoveAt(innerCounts.Count - 1);

                var next = i + 1 < pattern.Length ? pattern[i + 1] : '\0';
                if (inner > 0 && (next == '*' || next == '+' || next == '?' || next == '{'))
                    return true;
            }
        }

        return false;
    }

    private static string InjectQuantifierCaptureResets(string pattern)
    {
        // One frame per open group: the offset just after the group's header
        // (where a reset prologue would go) and the bjsg numbers declared inside it.
        var stack = new List<(int HeaderEnd, List<int> Inner)>();
        // Each insertion is the text to splice in just before character Pos. A quantified
        // group with inner duplicates contributes a "RESET(?:" prefix at its header and a
        // matching ")" before its closing paren.
        var insertions = new List<(int Pos, string Text, int Tie)>();
        var inClass = false;

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\') { i++; continue; }
            if (inClass) { if (c == ']') inClass = false; continue; }
            if (c == '[') { inClass = true; continue; }

            if (c == '(')
            {
                var headerEnd = i + 1;
                var declaredNum = -1;
                if (i + 1 < pattern.Length && pattern[i + 1] == '?')
                {
                    if (i + 2 < pattern.Length && pattern[i + 2] == '<'
                        && i + 3 < pattern.Length && pattern[i + 3] != '=' && pattern[i + 3] != '!')
                    {
                        var close = pattern.IndexOf('>', i + 3);
                        headerEnd = close + 1;
                        var name = pattern.Substring(i + 3, close - (i + 3));
                        if (name.StartsWith("bjsg") && int.TryParse(name.Substring(4), out var n))
                            declaredNum = n;
                    }
                    else
                    {
                        // Non-capturing header: (?:  (?=  (?!  (?>  (?<=  (?<!  (?( …
                        headerEnd = i + 2 < pattern.Length && pattern[i + 2] == '<' ? i + 4 : i + 3;
                    }
                }

                // A declared group belongs to every enclosing group, so each one
                // that is quantified must reset it.
                if (declaredNum >= 0)
                    foreach (var f in stack)
                        f.Inner.Add(declaredNum);

                stack.Add((headerEnd, new List<int>()));
                continue;
            }

            if (c == ')')
            {
                if (stack.Count == 0)
                    continue;

                var frame = stack[^1];
                stack.RemoveAt(stack.Count - 1);

                var next = i + 1 < pattern.Length ? pattern[i + 1] : '\0';
                if ((next == '*' || next == '+' || next == '?' || next == '{') && frame.Inner.Count > 0)
                {
                    var reset = new StringBuilder();
                    foreach (var n in frame.Inner)
                        reset.Append("(?(bjsg").Append(n).Append(")(?<-bjsg").Append(n).Append(">))");
                    reset.Append("(?:"); // open the wrapper around the original body
                    // Tie 0 prefix sorts before any close inserted at the same position; the
                    // wrapper close (Tie 1) sorts before the group's own ')'.
                    insertions.Add((frame.HeaderEnd, reset.ToString(), 0));
                    insertions.Add((i, ")", 1));
                }
            }
        }

        if (insertions.Count == 0)
            return pattern;

        insertions.Sort((a, b) => a.Pos != b.Pos ? a.Pos.CompareTo(b.Pos) : a.Tie.CompareTo(b.Tie));
        var sb = new StringBuilder(pattern.Length + insertions.Count * 24);
        var ins = 0;
        for (var i = 0; i <= pattern.Length; i++)
        {
            while (ins < insertions.Count && insertions[ins].Pos == i)
            {
                sb.Append(insertions[ins].Text);
                ins++;
            }

            if (i < pattern.Length)
                sb.Append(pattern[i]);
        }

        return sb.ToString();
    }

    // ECMAScript RepeatMatcher (22.2.2.3.1, step 2.b): "If min = 0 and y's endIndex =
    // x's endIndex, return failure." — an optional (min = 0) iteration that matched the
    // empty string is discarded. A quantifier whose body ALWAYS matches empty (a
    // lookaround, an anchor, or an empty group — anything that consumes no input) can
    // therefore only ever perform zero iterations when min = 0, so any capturing group
    // inside it never participates and reads back as `undefined`. .NET instead keeps the
    // capture (e.g. /abc()?/ leaves group 1 as "" and /(?:(?=(abc)))?a/ as "abc"), so
    // rewrite the quantifier to `{0}`: the group's slots stay declared (preserving group
    // numbering) but never run, matching ECMAScript. This is a sound, behaviour-preserving
    // rewrite — a min = 0 quantifier over a zero-width body is observably equivalent to
    // matching it exactly zero times.
    private static string RewriteZeroWidthMinZeroQuantifiers(string pattern)
    {
        List<(int Start, int Len)> replacements = null;
        var openStack = new List<int>();
        var inClass = false;

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\') { i++; continue; }
            if (inClass) { if (c == ']') inClass = false; continue; }
            if (c == '[') { inClass = true; continue; }
            if (c == '(') { openStack.Add(i); continue; }
            if (c != ')') continue;

            if (openStack.Count == 0)
                continue;
            var open = openStack[^1];
            openStack.RemoveAt(openStack.Count - 1);

            // A min = 0 quantifier must directly follow this group, and the group must
            // consume no input.
            if (!TryReadMinZeroQuantifier(pattern, i + 1, out var qEnd))
                continue;
            if (!IsZeroWidthGroup(pattern, open, i))
                continue;

            (replacements ??= new List<(int, int)>()).Add((i + 1, qEnd - (i + 1)));
        }

        if (replacements == null)
            return pattern;

        // Replacements are recorded as their closing ')' is reached, i.e. in ascending
        // start order, so a single forward splice is enough.
        var sb = new StringBuilder(pattern.Length);
        var idx = 0;
        foreach (var (start, len) in replacements)
        {
            sb.Append(pattern, idx, start - idx);
            sb.Append("{0}");
            idx = start + len;
        }
        sb.Append(pattern, idx, pattern.Length - idx);
        return sb.ToString();
    }

    // Reads a quantifier at <paramref name="start"/> whose minimum repeat count is zero
    // (`?`, `*`, `{0}`, `{0,}`, `{0,m}`, optionally with a trailing lazy `?`). On success
    // <paramref name="end"/> is the index just past the whole quantifier.
    private static bool TryReadMinZeroQuantifier(string pattern, int start, out int end)
    {
        end = start;
        if (start >= pattern.Length)
            return false;

        var c = pattern[start];
        if (c == '?' || c == '*')
        {
            end = start + 1;
        }
        else if (c == '{')
        {
            var j = start + 1;
            var digitsStart = j;
            while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9') j++;
            if (j == digitsStart)
                return false; // no minimum digits → a literal brace, not a quantifier
            var minIsZero = long.TryParse(pattern.Substring(digitsStart, j - digitsStart), out var mv) && mv == 0;
            if (j < pattern.Length && pattern[j] == '}')
            {
                if (!minIsZero) return false; // {n} with n>0
                end = j + 1;
            }
            else if (j < pattern.Length && pattern[j] == ',')
            {
                j++;
                while (j < pattern.Length && pattern[j] >= '0' && pattern[j] <= '9') j++;
                if (j >= pattern.Length || pattern[j] != '}')
                    return false; // malformed → a literal brace run
                if (!minIsZero) return false; // {n,} or {n,m} with n>0
                end = j + 1;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        // A lazy modifier on a min = 0 quantifier doesn't change that it can match empty.
        if (end < pattern.Length && pattern[end] == '?')
            end++;
        return true;
    }

    // True when the group spanning [open, close] (indices of its '(' and ')') matches the
    // empty string for every input — a lookaround, or a group whose body is a disjunction
    // of zero-width sequences.
    private static bool IsZeroWidthGroup(string pattern, int open, int close)
    {
        int bodyStart;
        if (open + 1 <= close && open + 1 < pattern.Length && pattern[open + 1] == '?')
        {
            var c2 = open + 2 < pattern.Length ? pattern[open + 2] : '\0';
            if (c2 == '=' || c2 == '!')
                return true; // (?=…) / (?!…) lookahead — zero-width regardless of body
            if (c2 == '<')
            {
                var c3 = open + 3 < pattern.Length ? pattern[open + 3] : '\0';
                if (c3 == '=' || c3 == '!')
                    return true; // (?<=…) / (?<!…) lookbehind
                // (?<name>…) named capturing group: body starts after '>'
                var gt = pattern.IndexOf('>', open + 3);
                if (gt < 0 || gt >= close)
                    return false;
                bodyStart = gt + 1;
            }
            else if (c2 == ':')
            {
                bodyStart = open + 3; // (?:…) non-capturing
            }
            else
            {
                // Inline-modifier group (?ims-x:…): body after the ':'. Anything else
                // (e.g. atomic (?>…)) is treated conservatively as non-zero-width.
                var colon = pattern.IndexOf(':', open + 2);
                if (colon < 0 || colon >= close)
                    return false;
                bodyStart = colon + 1;
            }
        }
        else
        {
            bodyStart = open + 1; // plain capturing group
        }

        return IsZeroWidthSequence(pattern, bodyStart, close);
    }

    // True when [start, end) — a group body, possibly containing `|` alternatives —
    // consumes no input: every atom is an anchor, a lookaround, or a (recursively)
    // zero-width group, with quantifiers/alternation in between. Any input-consuming atom
    // (a literal, '.', a character class, or an escape other than \b/\B) makes it false.
    private static bool IsZeroWidthSequence(string pattern, int start, int end)
    {
        var i = start;
        while (i < end)
        {
            var c = pattern[i];
            if (c == '\\')
            {
                var n = i + 1 < end ? pattern[i + 1] : '\0';
                if (n != 'b' && n != 'B')
                    return false; // any escape other than the \b/\B boundary assertions consumes
                i += 2;
                continue;
            }
            if (c == '^' || c == '$' || c == '|' || c == '*' || c == '+' || c == '?')
            {
                i++; // anchors, alternation, and quantifier modifiers are all zero-width
                continue;
            }
            if (c == '(')
            {
                var sub = MatchingCloseParen(pattern, i);
                if (sub < 0 || sub >= end)
                    return false;
                if (!IsZeroWidthGroup(pattern, i, sub))
                    return false;
                i = sub + 1;
                continue;
            }
            // A literal character, '.', '[' class, '{'/'}' (possible literal brace), or a
            // stray ')' — treat as consuming / unknown and bail out conservatively.
            return false;
        }
        return true;
    }

    // Index of the ')' that closes the '(' at <paramref name="open"/>, skipping escapes
    // and character classes; -1 if unbalanced.
    private static int MatchingCloseParen(string pattern, int open)
    {
        var depth = 0;
        var inClass = false;
        for (var i = open; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\') { i++; continue; }
            if (inClass) { if (c == ']') inClass = false; continue; }
            if (c == '[') { inClass = true; continue; }
            if (c == '(') depth++;
            else if (c == ')') { if (--depth == 0) return i; }
        }
        return -1;
    }

    // ES2025 allows duplicate named capture groups only in SEPARATE alternatives of a
    // disjunction (so at most one participates in any match). Two GroupSpecifiers with
    // the same name reachable together in a single match (same alternative / nested /
    // inside a lookaround of that alternative) are a SyntaxError. Walks the pattern with
    // a stack of group-nesting frames; each frame tracks the names live in its CURRENT
    // alternative branch and the union over all its alternatives (folded in at `|` and on
    // close). Adding a name to a branch that already contains it — directly or bubbled up
    // from a closed child group — is the error.
    private static void ValidateDuplicateNamedGroups(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf("(?<", StringComparison.Ordinal) < 0)
            return;

        static void Conflict(string name)
            => throw JSEngine.NewSyntaxError($"Invalid regular expression: Duplicate capture group name '{name}'");

        var stack = new Stack<(HashSet<string> Current, HashSet<string> All)>();
        stack.Push((new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal)));
        var inClass = false;

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '\\') { i++; continue; }
            if (inClass) { if (c == ']') inClass = false; continue; }
            if (c == '[') { inClass = true; continue; }

            if (c == '|')
            {
                var top = stack.Peek();
                top.All.UnionWith(top.Current);
                top.Current.Clear();
                continue;
            }

            if (c == ')')
            {
                if (stack.Count > 1)
                {
                    var closed = stack.Pop();
                    closed.All.UnionWith(closed.Current);
                    var parent = stack.Peek();
                    foreach (var n in closed.All)
                        if (!parent.Current.Add(n))
                            Conflict(n);
                }
                continue;
            }

            if (c != '(')
                continue;

            // A named capture (?<name>…), distinguished from lookbehind (?<= / (?<!.
            if (i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                var nameEnd = pattern.IndexOf('>', i + 3);
                if (nameEnd > i + 3)
                {
                    var name = pattern.Substring(i + 3, nameEnd - (i + 3));
                    if (!stack.Peek().Current.Add(name))
                        Conflict(name);
                    i = nameEnd;
                }
            }

            // Every grouping ( … ) — capturing, non-capturing or lookaround — opens a
            // nested alternation frame whose names bubble up to the current alternative.
            stack.Push((new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal)));
        }
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

    // Validates every named-capture GroupSpecifier (?<name>…): the name must be a RegExpIdentifierName
    // (first code point a RegExpIdentifierStart, the rest RegExpIdentifierParts), throwing a
    // SyntaxError otherwise.
    private static void ValidateNamedGroupNames(string pattern)
    {
        ScanCaptureGroups(pattern, (_, name) =>
        {
            if (name != null && !IsValidGroupName(name))
                throw JSEngine.NewSyntaxError($"Invalid regular expression: invalid capture group name \"{name}\"");
        });
    }

    // A GroupName's RegExpIdentifierName always uses Unicode escape rules (\u{…} and \u-escaped
    // surrogate pairs are accepted) regardless of the regex's u/v flag.
    private static bool IsValidGroupName(string name)
    {
        if (!TryDecodeGroupName(name, unicode: true, out var codePoints) || codePoints.Count == 0)
            return false;
        if (!IsRegExpIdentifierStart(codePoints[0]))
            return false;
        for (var k = 1; k < codePoints.Count; k++)
            if (!IsRegExpIdentifierPart(codePoints[k]))
                return false;
        return true;
    }

    // Decodes a group-name's source text into Unicode code points: \uXXXX and (only in u/v mode)
    // \u{…} escapes are resolved, and literal surrogate pairs are combined. Returns false for a
    // malformed escape, a stray backslash, or a lone surrogate (none of which can appear in a name).
    private static bool TryDecodeGroupName(string name, bool unicode, out List<int> codePoints)
    {
        codePoints = new List<int>(name.Length);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '\\')
            {
                if (i + 1 >= name.Length || name[i + 1] != 'u') return false; // only \u escapes are legal
                i += 2;
                if (i < name.Length && name[i] == '{')
                {
                    if (!unicode) return false; // \u{…} is a u/v-mode-only escape
                    var close = name.IndexOf('}', i + 1);
                    if (close < 0 || close == i + 1) return false;
                    if (!int.TryParse(name.AsSpan(i + 1, close - (i + 1)), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp) || cp > 0x10FFFF)
                        return false;
                    codePoints.Add(cp);
                    i = close;
                }
                else
                {
                    if (i + 4 > name.Length ||
                        !int.TryParse(name.AsSpan(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var unit))
                        return false;
                    i += 3;
                    // In u/v mode a \u-escaped lead surrogate may pair with a following \u-escaped trail.
                    if (unicode && unit is >= 0xD800 and <= 0xDBFF
                        && i + 6 < name.Length && name[i + 1] == '\\' && name[i + 2] == 'u'
                        && int.TryParse(name.AsSpan(i + 3, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var trail)
                        && trail is >= 0xDC00 and <= 0xDFFF)
                    {
                        codePoints.Add(char.ConvertToUtf32((char)unit, (char)trail));
                        i += 6;
                    }
                    else if (unit is >= 0xD800 and <= 0xDFFF)
                    {
                        return false; // a lone surrogate is not a valid identifier code point
                    }
                    else codePoints.Add(unit);
                }
            }
            else if (char.IsHighSurrogate(c) && i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
            {
                codePoints.Add(char.ConvertToUtf32(c, name[i + 1]));
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                return false; // a lone surrogate
            }
            else codePoints.Add(c);
        }
        return true;
    }

    // Resolves \uXXXX (and, in u/v mode, \u{…}) escapes inside a regex named-group source
    // to the runtime string the test code reaches via groups[name]. Without this the
    // captureMap keys (and \k<name> lookups) used the raw source — so /(?<_‌>a)/'s
    // group was stored as "_\\u200C" and groups["_‌"] (an identifier resolving to
    // "_" + U+200C) found nothing (test262 RegExp/named-groups/non-unicode-property-names).
    // Returns null when decoding fails — the caller falls back to the raw name (which
    // would have already been rejected by ValidateNamedGroupNames if it were malformed).
    private static string DecodeGroupName(string rawName, bool unicode)
    {
        if (string.IsNullOrEmpty(rawName) || rawName.IndexOf('\\') < 0)
            return rawName;

        if (!TryDecodeGroupName(rawName, unicode, out var codePoints))
            return rawName;

        var sb = new StringBuilder(rawName.Length);
        foreach (var cp in codePoints)
            sb.Append(char.ConvertFromUtf32(cp));
        return sb.ToString();
    }

    private static bool IsRegExpIdentifierStart(int cp)
    {
        if (cp < 0 || cp > 0x10FFFF || cp is >= 0xD800 and <= 0xDFFF) return false;
        if (cp is '$' or '_' or 0x1885 or 0x1886 or 0x2118 or 0x212E or 0x309B or 0x309C) return true;
        return char.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0) switch
        {
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber => true,
            _ => false,
        };
    }

    private static bool IsRegExpIdentifierPart(int cp)
    {
        if (cp < 0 || cp > 0x10FFFF || cp is >= 0xD800 and <= 0xDFFF) return false;
        if (IsRegExpIdentifierStart(cp)) return true;
        if (cp is 0x200C or 0x200D or 0x00B7 or 0x0387 or 0x19DA or 0x30FB or 0xFF65) return true;
        if (cp is >= 0x1369 and <= 0x1371) return true;
        return char.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0) switch
        {
            UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation => true,
            _ => false,
        };
    }

    // Rewrites a JS \k<name> reference to its synthetic .NET form. A name shared by
    // several alternatives (ES2025 duplicate) becomes a nested conditional so the
    // backreference follows whichever same-named group participated; a name that
    // participated in no alternative falls through to an empty (always-matching)
    // branch, matching the ECMAScript "backreference to an unmatched group" rule.
    private static string BuildNamedBackref(List<int> indices)
    {
        if (indices.Count == 1)
            // Gate the backreference on the group having participated. In ECMAScript an
            // unset backreference (a forward/self reference, or a backward reference to a
            // group in an unmatched optional) always matches the empty string. .NET only
            // honours that under RegexOptions.ECMAScript, which is disabled in u/v mode —
            // there a bare `\k<bjsgN>` to an unset group FAILS the match instead. The
            // conditional matches the capture when set and the empty string otherwise, in
            // both modes (the duplicate-name path below already relies on this form).
            return $"(?(bjsg{indices[0]})\\k<bjsg{indices[0]}>)";

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

        // Under ignoreCase the ECMAScript word set is extended with the characters
        // whose simple case folding lands in the ASCII word set \u2014 only U+017F (\u017F,
        // folds to s) and U+212A (KELVIN SIGN, folds to k). The effective ignoreCase
        // at a given `\b`/`\B` is the global flag adjusted by any enclosing inline
        // modifier groups `(?i:\u2026)` / `(?-i:\u2026)`, so track it with a scope stack
        // (mirroring TransformAnchorsAndDotsWithModifiers' s/m handling) rather than
        // assuming the global flag holds throughout (test262 regexp-modifiers/*).
        static string Boundary(bool ic, bool nonBoundary)
        {
            var wordChars = ic ? @"A-Za-z0-9_\u017F\u212A" : "A-Za-z0-9_";
            var wordClass = $"[{wordChars}]";
            var nonWordClass = $"[^{wordChars}]";
            return nonBoundary
                ? $"(?:(?<=^)(?=$)|(?<=^)(?={nonWordClass})|(?<={nonWordClass})(?=$)|(?<={nonWordClass})(?={nonWordClass})|(?<={wordClass})(?={wordClass}))"
                : $"(?:(?<=^)(?={wordClass})|(?<={nonWordClass})(?={wordClass})|(?<={wordClass})(?=$)|(?<={wordClass})(?={nonWordClass}))";
        }

        StringBuilder sb = null;
        int start = 0;
        bool inClass = false;
        var ignoreCaseStack = new System.Collections.Generic.Stack<bool>();
        ignoreCaseStack.Push(ignoreCase);

        for (int i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            // Skip an escaped pair first — both inside and outside a class — so a `\]` within a
            // class does not prematurely close it and an escaped `\(`/`\)` is not mistaken for a
            // group. `\b`/`\B` outside a class are the word boundaries we rewrite.
            if (c == '\\' && i + 1 < pattern.Length)
            {
                var next = pattern[i + 1];
                if (!inClass && (next == 'b' || next == 'B'))
                {
                    sb ??= new StringBuilder(pattern.Length + 64);
                    sb.Append(pattern, start, i - start);
                    sb.Append(Boundary(ignoreCaseStack.Peek(), nonBoundary: next == 'B'));
                    start = i + 2;
                }

                i++;
                continue;
            }

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

            if (inClass)
                continue;

            if (c == '(')
            {
                if (TryParseInlineModifierGroup(pattern, i, out var groupLength, out var addFlags, out var removeFlags))
                {
                    var ic = ignoreCaseStack.Peek();
                    if (addFlags.Contains('i')) ic = true;
                    if (removeFlags.Contains('i')) ic = false;
                    ignoreCaseStack.Push(ic);
                    i += groupLength - 1;
                }
                else
                {
                    // Any other group (capture, (?:\u2026), (?=\u2026), (?<name>\u2026), \u2026) leaves the
                    // flags unchanged; mirror the current scope so its ')' pops a balanced entry.
                    ignoreCaseStack.Push(ignoreCaseStack.Peek());
                }
                continue;
            }

            if (c == ')' && ignoreCaseStack.Count > 1)
                ignoreCaseStack.Pop();
        }

        if (sb == null)
            return pattern;

        sb.Append(pattern, start, pattern.Length - start);
        return sb.ToString();
    }

    /// <summary>
    /// An inline modifier group `(?i:…)` / `(?-i:…)` forces the whole pattern out of .NET's ECMAScript
    /// mode, so `\w` / `\W` revert to .NET's broad Unicode word set instead of the ECMAScript one. In
    /// Unicode mode re-impose the ECMAScript word set — ASCII `[A-Za-z0-9_]`, extended with U+017F (ſ)
    /// and U+212A (K) only where the EFFECTIVE ignoreCase is on — tracking the (?i:…)/(?-i:…) scopes so
    /// each `\w`/`\W` uses the flag in effect at its position (test262
    /// regexp-modifiers/{add,remove}-ignoreCase-affects-slash-{lower,upper}-w). `\W` keeps matching a
    /// surrogate pair (an astral code point is a non-word character).
    /// </summary>
    private static string TransformUnicodeWordClassEscapes(string pattern, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        static string Word(bool ic, bool negated)
        {
            var chars = ic ? @"A-Za-z0-9_ſK" : "A-Za-z0-9_";
            return negated ? $@"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[^{chars}])" : $"[{chars}]";
        }

        StringBuilder sb = null;
        int start = 0;
        bool inClass = false;
        var ignoreCaseStack = new System.Collections.Generic.Stack<bool>();
        ignoreCaseStack.Push(ignoreCase);

        for (int i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            // Skip an escaped pair first (in and out of classes) so `\]` cannot mis-close a class and a
            // class-internal `\w` is left to the class transforms; only an out-of-class `\w`/`\W` is rewritten.
            if (c == '\\' && i + 1 < pattern.Length)
            {
                var next = pattern[i + 1];
                if (!inClass && (next == 'w' || next == 'W'))
                {
                    sb ??= new StringBuilder(pattern.Length + 64);
                    sb.Append(pattern, start, i - start);
                    sb.Append(Word(ignoreCaseStack.Peek(), negated: next == 'W'));
                    start = i + 2;
                }

                i++;
                continue;
            }

            if (c == '[' && !inClass) { inClass = true; continue; }
            if (c == ']' && inClass) { inClass = false; continue; }
            if (inClass) continue;

            if (c == '(')
            {
                if (TryParseInlineModifierGroup(pattern, i, out var groupLength, out var addFlags, out var removeFlags))
                {
                    var ic = ignoreCaseStack.Peek();
                    if (addFlags.Contains('i')) ic = true;
                    if (removeFlags.Contains('i')) ic = false;
                    ignoreCaseStack.Push(ic);
                    i += groupLength - 1;
                }
                else
                {
                    ignoreCaseStack.Push(ignoreCaseStack.Peek());
                }
                continue;
            }

            if (c == ')' && ignoreCaseStack.Count > 1)
                ignoreCaseStack.Pop();
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
    private static string TransformUnicodeCharClasses(string pattern, bool ignoreCase = false)
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
                bool hasSupplementary = false;
                bool classMatchesSupplementary = false;
                // For a negated class, whether the *excluded* set already covers the
                // whole supplementary range. \D/\S/\W and \p/\P escapes can each span
                // astral code points, so a class such as [^\D] (= digits) must not be
                // extended to consume surrogate pairs. A class with only BMP members
                // (e.g. [^a], [^\d]) excludes nothing astral, so its negation must
                // match an astral code point as a single unit.
                bool excludesAstral = false;
                // A lone surrogate member (an escape or raw unit that is NOT part of a
                // surrogate pair) needs code-point semantics too: it must match a lone
                // surrogate code unit only when that unit is not itself part of a pair.
                bool hasLoneSurrogate = false;

                int scanPos = i;
                while (scanPos < pattern.Length && pattern[scanPos] != ']')
                {
                    if (pattern[scanPos] == '\\' && scanPos + 1 < pattern.Length)
                    {
                        var esc = pattern[scanPos + 1];
                        if (!negated && (esc == 'D' || esc == 'S' || esc == 'W'))
                            classMatchesSupplementary = true;
                        if (esc == 'D' || esc == 'S' || esc == 'W' || esc == 'p' || esc == 'P')
                            excludesAstral = true;
                        // The scanner emits an astral \u{...} escape as a \uHHHH high
                        // surrogate escape followed by a \uHHHH low surrogate escape;
                        // that pair is a supplementary member.
                        if (esc == 'u' && IsSurrogateEscapeAt(pattern, scanPos, 0xD800, 0xDBFF)
                            && IsSurrogateEscapeAt(pattern, scanPos + 6, 0xDC00, 0xDFFF))
                        {
                            hasSupplementary = true;
                            break;
                        }
                        // A surrogate escape that is not the lead of such a pair is lone.
                        if (esc == 'u' && IsSurrogateEscapeAt(pattern, scanPos, 0xD800, 0xDFFF))
                            hasLoneSurrogate = true;
                        scanPos += 2;
                        continue;
                    }
                    if (char.IsHighSurrogate(pattern[scanPos]) && scanPos + 1 < pattern.Length
                        && char.IsLowSurrogate(pattern[scanPos + 1]))
                    {
                        hasSupplementary = true;
                        break;
                    }
                    // A raw surrogate code unit that is not part of a raw pair is lone.
                    if (char.IsSurrogate(pattern[scanPos]))
                        hasLoneSurrogate = true;
                    scanPos++;
                }

                // A negated class with no astral exclusions (e.g. [^a], [^\d]) still has
                // to consume a full code point so that an astral code point is matched as
                // a single unit rather than just its leading surrogate — rebuild it even
                // when it holds no supplementary members.
                bool negatedNeedsSurrogatePair = negated && !excludesAstral;

                if (!hasSupplementary && !classMatchesSupplementary && !negatedNeedsSurrogatePair && !hasLoneSurrogate)
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

                // We have supplementary chars - rebuild the class as code-point-aware
                // alternation. Single-unit (BMP) members stay in a [...] class; astral
                // members and any range that reaches the supplementary planes become
                // surrogate-decomposed alternatives.
                sb ??= new StringBuilder(pattern.Length + 64);
                sb.Append(pattern, start, classStart - start);

                i = classStart + 1; // skip '['
                if (negated)
                    i++; // skip '^'

                var bmpParts = new StringBuilder();
                var alternatives = new List<string>();

                // A leading ']' is a literal class member.
                if (i < pattern.Length && pattern[i] == ']')
                {
                    bmpParts.Append(']');
                    i++;
                }

                while (i < pattern.Length && pattern[i] != ']')
                {
                    // A \u escape (\uHHHH or \u{...}) decodes to a code point and so
                    // participates in range/astral handling; every other escape (\d,
                    // \w, \p{...}, \-, ...) stays verbatim in the BMP class.
                    if (pattern[i] == '\\' && i + 1 < pattern.Length && pattern[i + 1] != 'u')
                    {
                        bmpParts.Append(pattern[i]);
                        bmpParts.Append(pattern[i + 1]);
                        i += 2;
                        continue;
                    }

                    int lo = ReadClassCodePoint(pattern, ref i);

                    // A range "lo-hi": the '-' is followed by a real member (not the
                    // closing ']' and not a non-\u class escape we cannot range against).
                    if (i + 1 < pattern.Length && pattern[i] == '-' && pattern[i + 1] != ']'
                        && !(pattern[i + 1] == '\\' && (i + 2 >= pattern.Length || pattern[i + 2] != 'u')))
                    {
                        i++; // skip '-'
                        int hi = ReadClassCodePoint(pattern, ref i);
                        if (lo <= hi && (lo > 0xFFFF || hi > 0xFFFF))
                        {
                            // A range that reaches into the supplementary planes is
                            // decomposed into surrogate-aware alternatives.
                            AppendCodePointRangeAlternatives(alternatives, lo, hi);
                            // Under /iu, the range also matches the case-fold images of
                            // its supplementary members (.NET folds neither astral nor,
                            // inside a rebuilt class, anything here).
                            if (ignoreCase)
                                AppendAstralCaseFoldExtras(alternatives, lo, hi);
                        }
                        else
                        {
                            // A pure-BMP range stays in the [...] class verbatim.
                            AppendBmpCodePoint(bmpParts, lo);
                            bmpParts.Append('-');
                            AppendBmpCodePoint(bmpParts, hi);
                        }
                        continue;
                    }

                    if (lo > 0xFFFF)
                    {
                        alternatives.Add(EncodeSurrogatePair(lo));
                        // Under /iu, a supplementary member also matches its case-fold class.
                        if (ignoreCase)
                            AppendAstralCaseFoldExtras(alternatives, lo, lo);
                    }
                    else if (lo is >= 0xD800 and <= 0xDBFF)
                        // A lone lead surrogate: match a lead unit only when it is not
                        // followed by a trailing unit (otherwise the two form one pair).
                        alternatives.Add(Unit(lo) + "(?![\uDC00-\uDFFF])");
                    else if (lo is >= 0xDC00 and <= 0xDFFF)
                        // A lone trail surrogate: match a trail unit only when it is not
                        // preceded by a leading unit.
                        alternatives.Add("(?<![\uD800-\uDBFF])" + Unit(lo));
                    else
                        AppendBmpCodePoint(bmpParts, lo);
                }

                // i now points at ']'
                if (negated)
                {
                    // [^𝌆a-z…] → exclude every collected member with lookaheads, then
                    // match any single code point: a surrogate pair, a lone surrogate
                    // (not part of a pair), or a BMP unit outside the excluded set.
                    sb.Append("(?:");
                    foreach (var alt in alternatives)
                    {
                        sb.Append("(?!");
                        sb.Append(alt);
                        sb.Append(')');
                    }
                    sb.Append(@"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[^");
                    sb.Append(bmpParts);
                    sb.Append(@"\uD800-\uDFFF]))");
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
                    foreach (var alt in alternatives)
                    {
                        sb.Append(alt);
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

    // Reads one class member starting at p[i] — a BMP unit, a literal astral
    // surrogate pair, or a \uHHHH / \u{...} escape (a high+low surrogate-escape
    // pair is combined into one astral code point) — advances i past it, and
    // returns its code point.
    private static int ReadClassCodePoint(string p, ref int i)
    {
        if (p[i] == '\\' && i + 1 < p.Length && p[i + 1] == 'u')
        {
            // \u{H...H}
            if (i + 2 < p.Length && p[i + 2] == '{')
            {
                int end = p.IndexOf('}', i + 3);
                if (end > i + 3 && TryParseHex(p, i + 3, end - (i + 3), out int cpBraced))
                {
                    i = end + 1;
                    return cpBraced;
                }
            }
            // \uHHHH (optionally combining a high+low surrogate-escape pair).
            else if (TryParseHex(p, i + 2, 4, out int u))
            {
                i += 6;
                if (u >= 0xD800 && u <= 0xDBFF
                    && i + 1 < p.Length && p[i] == '\\' && p[i + 1] == 'u'
                    && TryParseHex(p, i + 2, 4, out int u2) && u2 >= 0xDC00 && u2 <= 0xDFFF)
                {
                    i += 6;
                    return char.ConvertToUtf32((char)u, (char)u2);
                }
                return u;
            }
        }

        if (char.IsHighSurrogate(p[i]) && i + 1 < p.Length && char.IsLowSurrogate(p[i + 1]))
        {
            int cp = char.ConvertToUtf32(p[i], p[i + 1]);
            i += 2;
            return cp;
        }
        return p[i++];
    }

    // True when p[pos..] is a \uHHHH escape whose value lies in [lo, hi].
    private static bool IsSurrogateEscapeAt(string p, int pos, int lo, int hi)
        => pos + 1 < p.Length && p[pos] == '\\' && p[pos + 1] == 'u'
           && TryParseHex(p, pos + 2, 4, out int v) && v >= lo && v <= hi;

    // Parses exactly count hex digits starting at p[start]; false if out of range
    // or a non-hex digit is encountered.
    private static bool TryParseHex(string p, int start, int count, out int value)
    {
        value = 0;
        if (count <= 0 || start + count > p.Length)
            return false;
        for (int k = 0; k < count; k++)
        {
            int d = HexDigitValue(p[start + k]);
            if (d < 0)
                return false;
            value = (value << 4) | d;
        }
        return true;
    }

    private static int HexDigitValue(char c)
        => c >= '0' && c <= '9' ? c - '0'
         : c >= 'a' && c <= 'f' ? c - 'a' + 10
         : c >= 'A' && c <= 'F' ? c - 'A' + 10
         : -1;

    // A single UTF-16 code unit as a fixed-width \uHHHH escape (safe inside a .NET class).
    private static string Unit(int codeUnit) => "\\u" + codeUnit.ToString("X4");

    private static int HighSurrogateOf(int cp) => 0xD800 + ((cp - 0x10000) >> 10);
    private static int LowSurrogateOf(int cp) => 0xDC00 + ((cp - 0x10000) & 0x3FF);

    // An astral code point as its \uHHHH\uHHHH surrogate-pair escape.
    private static string EncodeSurrogatePair(int cp)
        => Unit(HighSurrogateOf(cp)) + Unit(LowSurrogateOf(cp));

    // Appends a BMP code point to a character-class body. Characters that are
    // structural inside a [...] class (or non-printable ASCII) are emitted as a
    // fixed-width \uHHHH escape so a decoded code point cannot alter the class
    // structure; ordinary printable characters are emitted literally.
    private static void AppendBmpCodePoint(StringBuilder bmp, int cp)
    {
        if (cp < 0x20 || cp > 0x7E || cp is ']' or '\\' or '^' or '-' or '[')
            bmp.Append(Unit(cp));
        else
            bmp.Append((char)cp);
    }

    // A single low-surrogate sub-range "[\uLO-\uHI]" (or "\uLO" when degenerate).
    private static string LowRange(int lo, int hi)
        => lo == hi ? Unit(lo) : "[" + Unit(lo) + "-" + Unit(hi) + "]";

    // A "[\uLO-\uHI]" sub-range over BMP code units (or "\uLO" when degenerate).
    private static string UnitRange(int lo, int hi)
        => lo == hi ? Unit(lo) : "[" + Unit(lo) + "-" + Unit(hi) + "]";

    // Appends the BMP sub-range [lo, hi] (both ≤ 0xFFFF) as code-point alternatives.
    // The surrogate block U+D800–U+DFFF is split out and guarded so a surrogate that
    // forms a pair is not matched as a standalone code unit — a lead matches only when
    // not followed by a trail, and a trail only when not preceded by a lead.
    private static void AppendBmpSubRangeAlternatives(List<string> alts, int lo, int hi)
    {
        if (lo <= 0xD7FF)
            alts.Add(UnitRange(lo, System.Math.Min(hi, 0xD7FF)));
        if (hi >= 0xD800 && lo <= 0xDBFF)
            alts.Add(UnitRange(System.Math.Max(lo, 0xD800), System.Math.Min(hi, 0xDBFF)) + "(?![\uDC00-\uDFFF])");
        if (hi >= 0xDC00 && lo <= 0xDFFF)
            alts.Add("(?<![\uD800-\uDBFF])" + UnitRange(System.Math.Max(lo, 0xDC00), System.Math.Min(hi, 0xDFFF)));
        if (hi >= 0xE000)
            alts.Add(UnitRange(System.Math.Max(lo, 0xE000), hi));
    }

    // Decomposes the code-point range [lo, hi] (which reaches into the supplementary
    // planes) into surrogate-aware .NET sub-patterns, each matching one whole code
    // point. Appends each as an alternative.
    private static void AppendCodePointRangeAlternatives(List<string> alts, int lo, int hi)
    {
        // The BMP portion [lo, min(hi, 0xFFFF)] matches as single units, with the
        // surrogate block split out and guarded so a paired surrogate is not matched
        // as a lone unit.
        if (lo <= 0xFFFF)
        {
            int bmpHi = System.Math.Min(hi, 0xFFFF);
            AppendBmpSubRangeAlternatives(alts, lo, bmpHi);
            if (hi <= 0xFFFF)
                return;
            lo = 0x10000;
        }

        // The supplementary portion, decomposed by leading surrogate.
        int hLo = HighSurrogateOf(lo), lLo = LowSurrogateOf(lo);
        int hHi = HighSurrogateOf(hi), lLast = LowSurrogateOf(hi);

        if (hLo == hHi)
        {
            alts.Add(Unit(hLo) + LowRange(lLo, lLast));
            return;
        }

        alts.Add(Unit(hLo) + LowRange(lLo, 0xDFFF));
        if (hHi - hLo >= 2)
            alts.Add("[" + Unit(hLo + 1) + "-" + Unit(hHi - 1) + "]" + @"[\uDC00-\uDFFF]");
        alts.Add(Unit(hHi) + LowRange(0xDC00, lLast));
    }

    // Appends the supplementary-plane ECMAScript Canonicalize (Unicode-mode) case-fold
    // images of every code point in [lo, hi] that fall OUTSIDE [lo, hi], as surrogate-aware
    // alternatives. Iterates the (bounded, ~hundreds of entries) astral fold map rather
    // than the range itself, so even a very wide range stays cheap, and merges the images
    // into contiguous runs to keep the emitted alternation compact.
    private static void AppendAstralCaseFoldExtras(List<string> alts, int lo, int hi)
    {
        SortedSet<int> extras = null;
        foreach (var kv in AstralCaseFoldEquivalents.Value)
        {
            if (kv.Key < lo || kv.Key > hi)
                continue;
            foreach (var e in kv.Value)
                if (e < lo || e > hi)
                    (extras ??= new SortedSet<int>()).Add(e);
        }
        if (extras == null)
            return;

        int runLo = -1, prev = -1;
        foreach (var cp in extras)
        {
            if (runLo < 0) { runLo = prev = cp; continue; }
            if (cp == prev + 1) { prev = cp; continue; }
            AppendCodePointRangeAlternatives(alts, runLo, prev);
            runLo = prev = cp;
        }
        if (runLo >= 0)
            AppendCodePointRangeAlternatives(alts, runLo, prev);
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

    // ECMAScript-correct rewrite of '.', '^' and '$' for a pattern that contains
    // inline modifier groups `(?add-remove:...)`. The effective dotAll/multiline flags
    // are tracked through a stack so each anchor/dot uses the flags in scope at its
    // position, not the global ones:
    //   '.'  (dotAll off) -> a class excluding the four LineTerminators
    //        (dotAll on)  -> left as '.', matched by .NET Singleline (global) or the
    //                        enclosing (?s:...) group
    //   '^'  (multiline)  -> matches input start or just after a LineTerminator
    //        (no m)       -> input start only (\A), never after a newline
    //   '$'  (multiline)  -> matches input end or just before a LineTerminator
    //        (no m)       -> input end only (\z); unlike .NET '$' it must not match
    //                        before a trailing newline
    private static string TransformAnchorsAndDotsWithModifiers(string pattern, bool globalDotAll, bool globalMultiline)
    {
        // The four ECMAScript LineTerminators as regex escapes: \n \r \u2028 \u2029.
        const string lineTerminators = "\\n\\r\\u2028\\u2029";
        const string dotNoNewline = "[^" + lineTerminators + "]";
        const string caretMultiline = "(?<=\\A|[" + lineTerminators + "])";
        const string dollarMultiline = "(?=[" + lineTerminators + "]|\\z)";

        var sb = new StringBuilder(pattern.Length + 32);
        var dotAllStack = new System.Collections.Generic.Stack<bool>();
        var multilineStack = new System.Collections.Generic.Stack<bool>();
        dotAllStack.Push(globalDotAll);
        multilineStack.Push(globalMultiline);

        bool inClass = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\')
            {
                sb.Append(c);
                if (i + 1 < pattern.Length)
                    sb.Append(pattern[++i]);
                continue;
            }

            if (inClass)
            {
                sb.Append(c);
                if (c == ']')
                    inClass = false;
                continue;
            }

            switch (c)
            {
                case '[':
                    inClass = true;
                    sb.Append(c);
                    continue;

                case '(':
                    if (TryParseInlineModifierGroup(pattern, i, out var groupLength, out var addFlags, out var removeFlags))
                    {
                        var dot = dotAllStack.Peek();
                        var multi = multilineStack.Peek();
                        if (addFlags.Contains('s')) dot = true;
                        if (removeFlags.Contains('s')) dot = false;
                        if (addFlags.Contains('m')) multi = true;
                        if (removeFlags.Contains('m')) multi = false;
                        dotAllStack.Push(dot);
                        multilineStack.Push(multi);
                        sb.Append(pattern, i, groupLength);
                        i += groupLength - 1;
                    }
                    else
                    {
                        // Any other group ( (?:…), (?=…), (?<name>…), capture, … ) does
                        // not change the flags; mirror the current scope so the matching
                        // ')' pops a balanced entry.
                        dotAllStack.Push(dotAllStack.Peek());
                        multilineStack.Push(multilineStack.Peek());
                        sb.Append(c);
                    }
                    continue;

                case ')':
                    if (dotAllStack.Count > 1)
                    {
                        dotAllStack.Pop();
                        multilineStack.Pop();
                    }
                    sb.Append(c);
                    continue;

                case '.':
                    sb.Append(dotAllStack.Peek() ? "." : dotNoNewline);
                    continue;

                case '^':
                    sb.Append(multilineStack.Peek() ? caretMultiline : @"\A");
                    continue;

                case '$':
                    sb.Append(multilineStack.Peek() ? dollarMultiline : @"\z");
                    continue;

                default:
                    sb.Append(c);
                    continue;
            }
        }

        return sb.ToString();
    }

    // Recognizes an inline modifier group prefix `(?add-remove:` at <index> (e.g.
    // `(?s:`, `(?-m:`, `(?ims-:`). Returns the length up to and including the ':' and
    // the added/removed flag sets. `(?:` (no flags) is not a modifier group.
    private static bool TryParseInlineModifierGroup(string pattern, int index, out int length, out string addFlags, out string removeFlags)
    {
        length = 0;
        addFlags = string.Empty;
        removeFlags = string.Empty;

        if (index + 1 >= pattern.Length || pattern[index] != '(' || pattern[index + 1] != '?')
            return false;

        int i = index + 2;
        int addStart = i;
        while (i < pattern.Length && (pattern[i] == 'i' || pattern[i] == 'm' || pattern[i] == 's'))
            i++;
        var add = pattern.Substring(addStart, i - addStart);

        var remove = string.Empty;
        if (i < pattern.Length && pattern[i] == '-')
        {
            i++;
            int removeStart = i;
            while (i < pattern.Length && (pattern[i] == 'i' || pattern[i] == 'm' || pattern[i] == 's'))
                i++;
            remove = pattern.Substring(removeStart, i - removeStart);
        }

        // Must end with ':' and add or remove at least one flag (otherwise this is a
        // plain `(?:` non-capturing group or some other `(?…)` construct).
        if (i >= pattern.Length || pattern[i] != ':' || (add.Length == 0 && remove.Length == 0))
            return false;

        length = i - index + 1; // include the ':'
        addFlags = add;
        removeFlags = remove;
        return true;
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
                // A \k<name> backreference: copy the whole <name> verbatim. A
                // supplementary-plane character in the name is a surrogate pair that
                // must not be wrapped in an atomic group (which would corrupt the name).
                if (pattern[i + 1] == 'k' && i + 2 < pattern.Length && pattern[i + 2] == '<')
                {
                    var refEnd = pattern.IndexOf('>', i + 3);
                    if (refEnd > 0)
                    {
                        sb.Append(pattern, i, refEnd - i + 1);
                        i = refEnd;
                        continue;
                    }
                }

                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            // A named group (?<name> (but not a lookbehind (?<= / (?<!): copy the name
            // verbatim so a supplementary-plane character in the group name is preserved
            // rather than split into an atomic group, which would corrupt the name that
            // RewriteCaptureGroups later extracts.
            if (!inClass && c == '(' && i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
            {
                var nameEnd = pattern.IndexOf('>', i + 3);
                if (nameEnd > 0)
                {
                    sb.Append(pattern, i, nameEnd - i + 1);
                    i = nameEnd;
                    continue;
                }
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
                    // Lone lead surrogate: match only when not part of a pair. Wrap
                    // in a non-capturing group so a following quantifier binds to the
                    // guarded atom, not just the zero-width look-ahead.
                    sb.Append("(?:").Append(c).Append("(?![\uDC00-\uDFFF]))");
                }
                continue;
            }

            if (char.IsLowSurrogate(c))
            {
                // Lone trail surrogate (a preceding lead would have been consumed as
                // a pair above): match only when not part of a pair. Wrap in a group
                // so a following quantifier binds to the guarded atom.
                sb.Append("(?:(?<![\uD800-\uDBFF])").Append(c).Append(')');
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
        bool inClass = false;
        int classDepth = 0;

        for (int i = 0; i < pattern.Length; i++)
        {
            // Inside a character class, keep \uHHHH escapes intact: the class
            // transform's ReadClassCodePoint distinguishes a lone surrogate escape
            // from a raw surrogate pair, and a zero-width (?:) separator (emitted
            // below for the out-of-class case) would be invalid inside [...].
            if (inClass)
            {
                if (pattern[i] == '\\' && i + 1 < pattern.Length)
                {
                    sb.Append(pattern[i]).Append(pattern[i + 1]);
                    i++;
                    continue;
                }
                if (pattern[i] == '[')
                    classDepth++;
                else if (pattern[i] == ']' && --classDepth <= 0)
                    inClass = false;
                sb.Append(pattern[i]);
                continue;
            }
            if (pattern[i] == '[')
            {
                inClass = true;
                classDepth = 1;
                sb.Append(pattern[i]);
                continue;
            }

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

                // Not a surrogate pair. The single escape is decoded to its raw
                // character so the later lone-surrogate / class transforms operate on
                // real characters — but a code point that is itself a regex
                // metacharacter (e.g. ? → ?, * → *) must keep a backslash,
                // otherwise the decoded character is parsed as an operator and the
                // pattern becomes invalid (`/\u{3f}/u` matched a literal `?`).
                char decoded = (char)hi;

                // A decoded lone surrogate must not silently merge with an *adjacent
                // raw* surrogate to form a pair: per spec only two \u escapes, or two
                // raw code units, combine into one astral code point — never one
                // escaped and one raw (e.g. `/\uD83D<rawDC38>/u` must NOT match 🐸).
                // A zero-width (?:) separator breaks the adjacency so the later
                // surrogate transforms treat each as a lone code point, while leaving
                // the genuine all-escape (collapsed above) and all-raw pairs intact.
                bool isHigh = hi is >= 0xD800 and <= 0xDBFF;
                bool isLow = hi is >= 0xDC00 and <= 0xDFFF;
                if (isLow && sb.Length > 0 && char.IsHighSurrogate(sb[sb.Length - 1]))
                    sb.Append("(?:)");

                if (IsSyntaxCharacter(decoded) || decoded == '-')
                    sb.Append('\\');
                sb.Append(decoded);

                if (isHigh && i + 6 < pattern.Length && char.IsLowSurrogate(pattern[i + 6]))
                    sb.Append("(?:)");

                i += 5;
                continue;
            }

            sb.Append(pattern[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Rewrites the braced Unicode code-point escape <c>\u{H..H}</c> (valid only in
    /// u/v mode) into the fixed-width <c>\uHHHH</c> form .NET understands — a
    /// surrogate-pair escape for a supplementary code point — mirroring what the
    /// source scanner already does for <c>/…/u</c> literals. A backslash-escaped
    /// backslash (<c>\\u{…}</c>, where <c>u{…}</c> is literal) is left untouched.
    /// </summary>
    private static string TransformBracedUnicodeEscapes(string pattern)
    {
        if (pattern.IndexOf("\\u{", StringComparison.Ordinal) < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        int i = 0;
        bool inClass = false;
        while (i < pattern.Length)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                if (pattern[i + 1] == 'u' && i + 2 < pattern.Length && pattern[i + 2] == '{')
                {
                    int j = i + 3;
                    int value = 0;
                    bool any = false, overflow = false;
                    while (j < pattern.Length && IsHex(pattern[j]))
                    {
                        value = value * 16 + HexVal(pattern[j]);
                        if (value > 0x10FFFF) overflow = true;
                        any = true;
                        j++;
                    }
                    if (any && !overflow && j < pattern.Length && pattern[j] == '}')
                    {
                        // A braced escape `\u{X}` is always a single code point and,
                        // unlike `\uHi\uLo`, never combines with an adjacent surrogate
                        // escape. When it denotes a lone surrogate outside a character
                        // class, wrap it in a non-capturing group so the later
                        // CollapseSurrogatePairEscapes cannot pair it with a neighbour
                        // (the group also keeps a following quantifier bound to the atom).
                        if (!inClass && value is >= 0xD800 and <= 0xDFFF)
                        {
                            sb.Append("(?:");
                            AppendCodePointEscape(sb, value);
                            sb.Append(')');
                        }
                        else
                        {
                            AppendCodePointEscape(sb, value);
                        }
                        i = j + 1;
                        continue;
                    }
                }

                // Any other escape (including a literal `\\`) is copied with its
                // following char so that char can't be misread as starting a `\u{`.
                sb.Append(c);
                sb.Append(pattern[i + 1]);
                i += 2;
                continue;
            }

            if (c == '[')
                inClass = true;
            else if (c == ']')
                inClass = false;

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    // Appends a code point as one (BMP) or two (surrogate-pair) `\uHHHH` escapes.
    // A BMP escape keeps its `\uHHHH` form rather than being decoded so a value that
    // is itself a regex metacharacter (e.g. \u{3f} → `?`) is handled uniformly by the
    // later surrogate/class transforms (which re-add the backslash where needed).
    private static void AppendCodePointEscape(StringBuilder sb, int codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            sb.Append("\\u").Append(codePoint.ToString("X4"));
            return;
        }

        int v = codePoint - 0x10000;
        sb.Append("\\u").Append((0xD800 + (v >> 10)).ToString("X4"));
        sb.Append("\\u").Append((0xDC00 + (v & 0x3FF)).ToString("X4"));
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
    private static string TransformUnicodePropertyEscapes(string pattern, bool unicodeSets, bool ignoreCase)
    {
        if (pattern.IndexOf("\\p", StringComparison.Ordinal) < 0 &&
            pattern.IndexOf("\\P", StringComparison.Ordinal) < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        int i = 0;
        bool inClass = false;
        // Effective ignoreCase per position. An inline modifier group `(?i:…)` / `(?-i:…)`
        // toggles ignoreCase for its extent, so a `\P{X}` escape can sit under a different
        // effective flag than the regex's global one. A negated property escape needs the
        // case closure of the COMPLEMENT under ignoreCase (see ExpandCodePointProperty),
        // which the position-independent translation cannot pick without this stack.
        var ignoreCaseStack = new System.Collections.Generic.Stack<bool>();
        ignoreCaseStack.Push(ignoreCase);
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
                        var translated = TranslateUnicodeProperty(next == 'P', inner, inClass, unicodeSets, ignoreCaseStack.Peek());
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

            // A character class whose only member is a single property escape —
            // `[\p{X}]`, `[\P{X}]`, `[^\p{X}]`, `[^\P{X}]` — is equivalent to the
            // standalone (possibly doubly-negated) property, so translate it in the
            // standalone (code-point, surrogate-aware) form. This is what makes a negated
            // binary property (`[\P{Hex}]`) or an astral-bearing property (`[\p{Emoji}]`)
            // usable inside a class: the in-class form cannot nest a negated fragment or
            // a supplementary-plane range, so it would otherwise be rejected.
            if (c == '[' && !inClass
                && TryTranslateSinglePropertyClass(pattern, i, unicodeSets, ignoreCaseStack.Peek(), out var classTranslated, out var afterClass))
            {
                sb.Append(classTranslated);
                i = afterClass;
                continue;
            }

            // Inline modifier groups change the effective ignoreCase for their extent.
            // Track them outside a character class (where `(` is literal); the group header
            // is copied verbatim and its body — including any nested `\P{X}` — is then
            // processed under the updated flag. A plain `(…)` / `(?:…)` just inherits.
            if (!inClass && c == '(')
            {
                if (TryParseInlineModifierGroup(pattern, i, out var groupLength, out var addFlags, out var removeFlags))
                {
                    var ic = ignoreCaseStack.Peek();
                    if (addFlags.Contains('i')) ic = true;
                    if (removeFlags.Contains('i')) ic = false;
                    ignoreCaseStack.Push(ic);
                    sb.Append(pattern, i, groupLength);
                    i += groupLength;
                    continue;
                }

                ignoreCaseStack.Push(ignoreCaseStack.Peek());
                sb.Append(c);
                i++;
                continue;
            }

            if (!inClass && c == ')' && ignoreCaseStack.Count > 1)
            {
                ignoreCaseStack.Pop();
                sb.Append(c);
                i++;
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
    /// Recognises a character class consisting of exactly one property escape (with an
    /// optional leading <c>^</c>) — <c>[\p{X}]</c> / <c>[\P{X}]</c> / <c>[^\p{X}]</c> /
    /// <c>[^\P{X}]</c> — and translates it as the standalone property (the class's <c>^</c>
    /// and the escape's <c>p</c>/<c>P</c> combine into the effective polarity). Returns
    /// false (leaving the normal in-class path to handle it) for any other class shape, or
    /// when the property has no standalone translation (a native form left to .NET).
    /// </summary>
    private static bool TryTranslateSinglePropertyClass(string pattern, int start, bool unicodeSets, bool ignoreCase, out string translated, out int afterClass)
    {
        translated = null;
        afterClass = start;

        var p = start + 1; // past '['
        var classNegated = false;
        if (p < pattern.Length && pattern[p] == '^')
        {
            classNegated = true;
            p++;
        }

        if (p + 2 >= pattern.Length || pattern[p] != '\\')
            return false;

        var pc = pattern[p + 1];
        if ((pc != 'p' && pc != 'P') || pattern[p + 2] != '{')
            return false;

        var close = pattern.IndexOf('}', p + 3);
        if (close < 0)
            return false;

        // The property escape must be the class's only member.
        if (close + 1 >= pattern.Length || pattern[close + 1] != ']')
            return false;

        var inner = pattern.Substring(p + 3, close - (p + 3));
        var effectiveNegated = (pc == 'P') ^ classNegated;
        var t = TranslateUnicodeProperty(effectiveNegated, inner, inClass: false, unicodeSets, ignoreCase);
        if (t == null)
            return false;

        translated = t;
        afterClass = close + 2; // past '}' and ']'
        return true;
    }

    /// <summary>
    /// Returns the .NET replacement text for a single <c>\p{inner}</c> /
    /// <c>\P{inner}</c> escape, or <c>null</c> when it should be left untouched.
    /// Throws a SyntaxError for property classes that are recognized but not
    /// yet supported.
    /// </summary>
    private static string TranslateUnicodeProperty(bool negated, string inner, bool inClass, bool unicodeSets, bool ignoreCase)
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
                return TranslateGeneralCategory(value, prefix, negated, inClass, inner, ignoreCase);

            if (name is "sc" or "script")
            {
                var ranges = UnicodeProperties.GetScript(value);
                if (ranges != null && ExpandCodePointProperty(ranges, negated, inClass, ignoreCase) is { } expanded)
                    return expanded;

                throw NewUnsupportedPropertyError(inner);
            }

            if (name is "scx" or "scriptextensions")
            {
                var ranges = UnicodeProperties.GetScriptExtensions(value);
                if (ranges != null && ExpandCodePointProperty(ranges, negated, inClass, ignoreCase) is { } expanded)
                    return expanded;

                throw NewUnsupportedPropertyError(inner);
            }

            // Unknown Name=Value — let .NET surface its own error.
            return null;
        }

        // Lone `\p{Value}` form.
        var lone = NormalizeKey(inner);

        // A lone General_Category value (Lu, L, Letter, …).
        if (UnicodeProperties.GetGeneralCategory(lone) != null
            || (inClass && GeneralCategoryNames.ContainsKey(lone)))
            return TranslateGeneralCategory(lone, prefix, negated, inClass, inner, ignoreCase);

        // Binary Unicode properties (ASCII, Alphabetic, Assigned, Emoji, …) come from
        // the generated UCD 17.0.0 range tables in Broiler.Unicode.Properties.
        var binaryRanges = UnicodeProperties.GetBinaryProperty(lone);
        if (binaryRanges != null)
        {
            var expanded = ExpandCodePointProperty(binaryRanges, negated, inClass, ignoreCase);
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

    /// <summary>
    /// Translates a General_Category value (the <c>gc=…</c> dimension or a lone gc value).
    /// Outside a character class it expands to the bundled UCD 17.0.0 code-point ranges so
    /// supplementary-plane code points match by code point. Inside a class — where the
    /// range expansion (with its surrogate-pair alternatives) cannot be nested — it falls
    /// back to .NET's native category escape (<c>\p{Lu}</c>), which is code-unit based.
    /// </summary>
    private static string TranslateGeneralCategory(string value, string prefix, bool negated, bool inClass, string inner, bool ignoreCase)
    {
        if (inClass)
        {
            if (GeneralCategoryNames.TryGetValue(value, out var shortName))
                return $"{prefix}{{{shortName}}}";
            throw NewUnsupportedPropertyError(inner);
        }

        var ranges = UnicodeProperties.GetGeneralCategory(value);
        if (ranges != null && ExpandCodePointProperty(ranges, negated, inClass, ignoreCase) is { } expanded)
            return expanded;

        throw NewUnsupportedPropertyError(inner);
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
    private static string ExpandCodePointProperty((int Lo, int Hi)[] ranges, bool negated, bool inClass, bool ignoreCase)
    {
        if (inClass)
        {
            if (negated)
            {
                // A nested `[^…]` is not allowed in .NET regex, but `\P{X}` inside a class
                // is still expressible by emitting the BMP COMPLEMENT of X's ranges as a
                // plain class fragment (test262
                // built-ins/RegExp/property-escapes/character-class). Supplementary
                // (≥ U+10000) ranges cannot appear inside [...], so a property carrying
                // any astral code points still can't be expressed this way.
                var complement = new StringBuilder();
                int cursor = 0;
                foreach (var (lo, hi) in ranges)
                {
                    if (hi > 0xFFFF)
                        return null;
                    if (lo > cursor)
                        AppendClassRange(complement, cursor, lo - 1);
                    cursor = System.Math.Max(cursor, hi + 1);
                }
                if (cursor <= 0xFFFF)
                    AppendClassRange(complement, cursor, 0xFFFF);
                return complement.ToString();
            }

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

        // Under ignoreCase, `\P{X}` matches the case closure of the COMPLEMENT of X
        // (22.2.2.7.1: ch matches iff some `a` NOT in X has Canonicalize(a) ==
        // Canonicalize(ch)). The negative-lookahead form below is wrong there: a
        // `(?!positive)` lookahead has the active IgnoreCase applied to `positive`, so it
        // rejects every ch that case-folds into X — e.g. `(?i:\P{Lu})` would reject "a"
        // because "a" folds to "A" ∈ Lu, and reject "A" too. Emit a positive matcher over
        // the COMPLEMENT ranges instead: .NET applies the active IgnoreCase to that set,
        // yielding the correct case-closed-complement (so "a" and "A" both match). Without
        // ignoreCase the two forms are equivalent, so keep the lookahead there — it is the
        // shape the surrogate/lone-surrogate handling was tuned against.
        if (ignoreCase)
            return BuildPositiveCodePointMatcher(ComplementCodePointRanges(ranges));

        // Match one code point that is NOT in the set: reject the set, then consume
        // a full code point (a surrogate pair or any single code unit).
        return $"(?:(?!{positive})(?:[\\uD800-\\uDBFF][\\uDC00-\\uDFFF]|[\\s\\S]))";
    }

    /// <summary>
    /// Returns the code-point ranges complementary to <paramref name="ranges"/> over the
    /// whole Unicode range [U+0000, U+10FFFF]. The input is assumed sorted ascending and
    /// non-overlapping (as the bundled UCD tables are), matching the in-class complement
    /// path above.
    /// </summary>
    private static (int Lo, int Hi)[] ComplementCodePointRanges((int Lo, int Hi)[] ranges)
    {
        var result = new List<(int Lo, int Hi)>();
        int cursor = 0;
        foreach (var (lo, hi) in ranges)
        {
            if (lo > cursor)
                result.Add((cursor, lo - 1));
            cursor = System.Math.Max(cursor, hi + 1);
        }
        if (cursor <= 0x10FFFF)
            result.Add((cursor, 0x10FFFF));
        return result.ToArray();
    }

    private static string BuildPositiveCodePointMatcher((int Lo, int Hi)[] ranges)
    {
        var bmp = new StringBuilder();
        var loneSurrogates = new List<string>();
        var supplementary = new List<string>();

        foreach (var (lo, hi) in ranges)
        {
            if (lo <= 0xFFFF)
            {
                AppendBmpRange(bmp, loneSurrogates, lo, System.Math.Min(hi, 0xFFFF));
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
        foreach (var alt in loneSurrogates)
        {
            if (!first)
                sb.Append('|');
            sb.Append(alt);
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
    /// Appends a BMP range [lo, hi] to the positive matcher, splitting the surrogate
    /// block U+D800..U+DFFF out of the plain character class. A surrogate code point in
    /// the pattern must, in Unicode mode, match only a *lone* surrogate in the input —
    /// never one half of a well-formed pair — otherwise a property that contains
    /// surrogates (e.g. <c>\p{Assigned}</c>, which includes the Cs category) would match
    /// the lead unit of a supplementary code point and wrongly accept it.
    /// </summary>
    private static void AppendBmpRange(StringBuilder bmp, List<string> loneSurrogates, int lo, int hi)
    {
        if (lo < 0xD800)
            AppendClassRange(bmp, lo, System.Math.Min(hi, 0xD7FF));

        var highLo = System.Math.Max(lo, 0xD800);
        var highHi = System.Math.Min(hi, 0xDBFF);
        if (highLo <= highHi)
            loneSurrogates.Add(SurrogateClass(highLo, highHi) + "(?![\\uDC00-\\uDFFF])");

        var lowLo = System.Math.Max(lo, 0xDC00);
        var lowHi = System.Math.Min(hi, 0xDFFF);
        if (lowLo <= lowHi)
            loneSurrogates.Add("(?<![\\uD800-\\uDBFF])" + SurrogateClass(lowLo, lowHi));

        if (hi > 0xDFFF)
            AppendClassRange(bmp, System.Math.Max(lo, 0xE000), hi);

        static string SurrogateClass(int lo, int hi)
            => lo == hi ? $"[\\u{lo:X4}]" : $"[\\u{lo:X4}-\\u{hi:X4}]";
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
            alts.Add(Atom(highLo) + UnitRange(lowLo, lowHi));
            return;
        }

        alts.Add(Atom(highLo) + UnitRange(lowLo, 0xDFFF));
        if (highHi - highLo >= 2)
            alts.Add(UnitRange(highLo + 1, highHi - 1) + UnitRange(0xDC00, 0xDFFF));
        alts.Add(Atom(highHi) + UnitRange(0xDC00, lowHi));

        static string Unit(int u) => $"\\u{u:X4}";
        // A single surrogate code unit here is an internal code-UNIT pair atom (one
        // half of a surrogate pair), not a code-POINT lone surrogate. It is emitted
        // as a degenerate range `[\uX-\uX]` rather than a single-member class `[\uX]`:
        // the lone-surrogate class transform guards single-member surrogate classes
        // (so a code-point `[\uD83D]` matches only a lone surrogate) but leaves
        // surrogate *ranges* verbatim, which is exactly the code-unit semantics a
        // pair atom needs.
        static string Atom(int u) => $"[\\u{u:X4}-\\u{u:X4}]";
        static string UnitRange(int lo, int hi) => $"[{Unit(lo)}-{Unit(hi)}]";
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

    // In a non-Unicode character class, a `-` adjacent to a CharacterClassEscape
    // (`\d \D \w \W \s \S`) cannot form a range, so Annex B treats it as a literal
    // `-` (e.g. `[--\d]` matches `-` or a digit). .NET rejects the range, so escape
    // such a dash. A `-` between two ordinary atoms (`[a-z]`) is left untouched.
    private static string NeutralizeAnnexBClassRangeDashes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf('-') < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (c == '[') inClass = true;
            else if (c == ']') inClass = false;
            else if (c == '-' && inClass)
            {
                bool prevIsClassEscape = i >= 2 && pattern[i - 2] == '\\' && IsClassEscapeLetter(pattern[i - 1]);
                bool nextIsClassEscape = i + 2 < pattern.Length && pattern[i + 1] == '\\' && IsClassEscapeLetter(pattern[i + 2]);
                if (prevIsClassEscape || nextIsClassEscape)
                {
                    sb.Append("\\-");
                    continue;
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsClassEscapeLetter(char c)
        => c is 'd' or 'D' or 'w' or 'W' or 's' or 'S';

    // In a non-Unicode regex with NO named groups, `\k` is an Annex B IdentityEscape,
    // so `\k<a>` matches the literal text `k<a>`. .NET rejects `\k<a>` as a reference to
    // an undefined group, so drop the backslash. With named groups present, an undefined
    // `\k<name>` is a real SyntaxError (left for the validator/.NET to reject).
    private static string NeutralizeAnnexBUndefinedNamedBackref(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf("\\k<", StringComparison.Ordinal) < 0)
            return pattern;

        if (HasNamedGroup(pattern))
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '\\' && i + 1 < pattern.Length)
            {
                if (!inClass && pattern[i + 1] == 'k' && i + 2 < pattern.Length && pattern[i + 2] == '<')
                {
                    sb.Append('k');
                    i++;
                    continue;
                }

                sb.Append(c).Append(pattern[i + 1]);
                i++;
                continue;
            }

            if (c == '[') inClass = true;
            else if (c == ']') inClass = false;

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool HasNamedGroup(string pattern)
    {
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length) { i++; continue; }
            if (c == '[') { inClass = true; continue; }
            if (c == ']') { inClass = false; continue; }
            if (!inClass && c == '(' && i + 2 < pattern.Length && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && (i + 3 >= pattern.Length || (pattern[i + 3] != '=' && pattern[i + 3] != '!')))
                return true;
        }

        return false;
    }

    private static string TransformAnnexBIdentityEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf('\\') < 0)
            return pattern;

        var sb = new StringBuilder(pattern.Length);
        var inClass = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            // A GroupName specifier `(?<name>` always uses Unicode escape rules, so its
            // content may contain `\u{…}` or `\u`-escaped surrogate pairs that look
            // "malformed" to a non-u/v regex. Copy `(?<name>` verbatim so those escapes are
            // preserved for the group-name decoder instead of being identity-escaped
            // (test262: named-groups/non-unicode-property-names-valid). Lookbehind
            // `(?<=` / `(?<!` is not a group name and falls through to normal handling.
            if (!inClass && c == '(' && i + 3 < pattern.Length
                && pattern[i + 1] == '?' && pattern[i + 2] == '<'
                && pattern[i + 3] != '=' && pattern[i + 3] != '!')
            {
                var gtEnd = pattern.IndexOf('>', i + 3);
                if (gtEnd > i + 3)
                {
                    sb.Append(pattern, i, gtEnd - i + 1);
                    i = gtEnd;
                    continue;
                }
            }

            if (c == '\\' && i + 1 < pattern.Length)
            {
                char next = pattern[i + 1];

                // `\u`/`\x`/`\k` are recognized only when well-formed (`\uHHHH`,
                // `\xHH`, `\k<name>`); otherwise they are Annex B IdentityEscapes of
                // the literal letter (.NET rejects a bare `\u`/`\x`/`\k`).
                bool malformedU = next == 'u' && !(i + 5 < pattern.Length
                    && IsHexDigit(pattern[i + 2]) && IsHexDigit(pattern[i + 3])
                    && IsHexDigit(pattern[i + 4]) && IsHexDigit(pattern[i + 5]));
                bool malformedX = next == 'x' && !(i + 3 < pattern.Length
                    && IsHexDigit(pattern[i + 2]) && IsHexDigit(pattern[i + 3]));
                bool malformedK = next == 'k' && !(i + 2 < pattern.Length && pattern[i + 2] == '<');

                if (!inClass && (malformedU || malformedX || malformedK
                    || (!IsSyntaxCharacter(next)
                        && !(next >= '0' && next <= '9')
                        && RecognizedEscapeLetters.IndexOf(next) < 0)))
                {
                    sb.Append(next); // IdentityEscape → the literal character
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
                        // Reference to a group that does not exist. In Annex B (non-Unicode) this
                        // DecimalEscape is not a backreference but a LegacyOctalEscapeSequence:
                        // `\1`–`\7` (up to three octal digits, value ≤ 255) is the octal character,
                        // while `\8`/`\9` degrade to the literal digit (an IdentityEscape), e.g.
                        // `/7\89/` matches "789" but `/\1/` matches "\x01". Emit any octal value as a
                        // \xHH escape so .NET does not re-read it as an (undefined) backreference.
                        char d0 = pattern[i + 1];
                        if (d0 <= '7')
                        {
                            int value = d0 - '0';
                            int maxMore = d0 <= '3' ? 2 : 1; // ZeroToThree allows 3 digits, FourToSeven 2
                            int p = i + 2;
                            for (int k = 0; k < maxMore && p < pattern.Length && pattern[p] >= '0' && pattern[p] <= '7'; k++, p++)
                                value = value * 8 + (pattern[p] - '0');
                            sb.Append("\\x").Append(value.ToString("X2", CultureInfo.InvariantCulture));
                            sb.Append(pattern, p, j - p); // any remaining digits are literal
                        }
                        else
                        {
                            // \8 / \9: the literal digit(s).
                            sb.Append(pattern, i + 1, j - (i + 1));
                        }
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

            // A supplementary-plane literal (a raw surrogate pair) outside a character
            // class, in Unicode mode: .NET does no astral case folding, so expand it to an
            // alternation of its ECMAScript Canonicalize equivalence class — e.g. /𐐀+/iu
            // must also match the lowercase 𐐨. (Inside a class this is handled later by
            // TransformUnicodeCharClasses.)
            if (unicode && !inClass && char.IsHighSurrogate(c)
                && i + 1 < pattern.Length && char.IsLowSurrogate(pattern[i + 1]))
            {
                int cp = char.ConvertToUtf32(c, pattern[i + 1]);
                var astralEq = GetAstralCaseFoldEquivalents(cp);
                if (astralEq != null)
                {
                    sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                    sb.Append("(?:").Append(EncodeSurrogatePair(cp));
                    foreach (var e in astralEq)
                        sb.Append('|').Append(EncodeSurrogatePair(e));
                    sb.Append(')');
                }
                else
                {
                    sb?.Append(c).Append(pattern[i + 1]);
                }
                i++; // also consume the trailing low surrogate
                continue;
            }

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
                    if (!unicode && !inClass && IsNonUnicodeOverFold((char)cp))
                    {
                        sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                        AppendOverFoldNeutralized(sb, (char)cp, equiv);
                        i += 5;
                        continue;
                    }
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
                    if (!unicode && !inClass && IsNonUnicodeOverFold((char)cp))
                    {
                        sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                        AppendOverFoldNeutralized(sb, (char)cp, equiv);
                        i += 3;
                        continue;
                    }
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
            if (!unicode && !inClass && IsNonUnicodeOverFold(c))
            {
                sb ??= new StringBuilder(pattern.Length + 16).Append(pattern, 0, i);
                AppendOverFoldNeutralized(sb, c, litEquiv);
                continue;
            }
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
    /// A non-ASCII code unit whose .NET (ToLower-based) IgnoreCase folds it onto an
    /// ASCII character, even though ECMAScript's non-Unicode Canonicalize (toUppercase
    /// with the ASCII guard) keeps it distinct. The canonical example is U+212A KELVIN
    /// SIGN: .NET considers it equal to 'k'/'K' (ToLower yields 'k'), so left to .NET a
    /// non-Unicode <c>/K/i</c> would wrongly match 'k'. ECMAScript leaves such a
    /// code point as its own canonical form, so it must only match itself (and any true
    /// non-ASCII equivalents) — never an ASCII letter.
    /// </summary>
    private static bool IsNonUnicodeOverFold(char c)
        => c >= 128 && char.ToLowerInvariant(c) < 128;

    /// <summary>
    /// Emits <paramref name="c"/> (plus its genuine ECMAScript equivalents) with
    /// IgnoreCase locally disabled via an inline <c>(?-i:…)</c> group, so .NET's
    /// over-broad ToLower folding no longer matches an unrelated ASCII letter. Only used
    /// outside a character class (an inline group is not valid class syntax).
    /// </summary>
    private static void AppendOverFoldNeutralized(StringBuilder sb, char c, char[] equivalents)
    {
        sb.Append("(?-i:");
        if (equivalents == null || equivalents.Length == 0)
        {
            AppendUnicodeEscape(sb, c);
        }
        else
        {
            sb.Append('[');
            AppendUnicodeEscape(sb, c);
            foreach (var eq in equivalents)
                AppendUnicodeEscape(sb, eq);
            sb.Append(']');
        }
        sb.Append(')');
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

        // Characters whose ECMAScript simple-case-fold equivalence class cannot be
        // recovered from char.ToUpperInvariant/ToLowerInvariant: their case mapping
        // expands to MULTIPLE code units (e.g. U+0390 → U+0399 U+0308 U+0301), so the
        // char-level round-trip above leaves them unchanged and never groups the
        // precomposed forms together. ECMAScript Canonicalize (Unicode mode) still folds
        // them via CaseFolding.txt, so seed those classes explicitly. Unicode mode only:
        // in non-Unicode mode a multi-code-unit uppercase leaves the character unchanged,
        // so these do NOT form an equivalence class there (test262
        // built-ins/RegExp/unicode_full_case_folding).
        if (unicode)
        {
            foreach (var equivalenceClass in MultiUnitCaseFoldClasses)
                MergeCaseFoldClass(map, equivalenceClass);
        }

        return map;
    }

    // Simple-case-fold equivalence classes whose members share a CaseFolding.txt
    // mapping that expands to multiple code units (so char-level ToUpper/ToLower cannot
    // recover the grouping). Each inner array is one class; every member folds to the
    // others under ECMAScript Canonicalize in Unicode mode.
    private static readonly char[][] MultiUnitCaseFoldClasses =
    {
        new[] { '\u0390', '\u1FD3' }, // GREEK SMALL LETTER IOTA WITH DIALYTIKA AND TONOS / OXIA
        new[] { '\u03B0', '\u1FE3' }, // GREEK SMALL LETTER UPSILON WITH DIALYTIKA AND TONOS / OXIA
        new[] { '\uFB05', '\uFB06' }, // LATIN SMALL LIGATURE LONG S T / ST
    };

    private static void MergeCaseFoldClass(Dictionary<char, char[]> map, char[] equivalenceClass)
    {
        foreach (var member in equivalenceClass)
        {
            // Union any class the char already belongs to (from the char-level pass)
            // with the rest of this class, de-duplicating and excluding the member.
            var union = new List<char>();
            if (map.TryGetValue(member, out var existing))
                union.AddRange(existing);

            foreach (var other in equivalenceClass)
            {
                if (other != member && !union.Contains(other))
                    union.Add(other);
            }

            map[member] = union.ToArray();
        }
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

    // Code-point (Rune) simple case fold, used to group SUPPLEMENTARY-plane code points
    // into ECMAScript Canonicalize (Unicode-mode) equivalence classes. .NET's char-based
    // ToUpper/ToLower only cover the BMP; Rune.ToUpperInvariant/ToLowerInvariant carry the
    // same Unicode (16.0) data to the astral planes, so the toUpper→toLower round-trip
    // yields each astral letter's canonical fold (e.g. Deseret U+10400 ⇄ U+10428, Adlam,
    // Osage, Vithkuqi, Old Hungarian, …).
    private static int CaseFoldKeyCodePoint(int cp)
    {
        var r = new System.Text.Rune(cp);
        return System.Text.Rune.ToLowerInvariant(System.Text.Rune.ToUpperInvariant(r)).Value;
    }

    // Reverse map from a SUPPLEMENTARY-plane code point to the other members of its
    // ECMAScript Canonicalize (Unicode-mode) equivalence class. .NET's IgnoreCase regex
    // does no astral case folding at all, and the BMP-only char maps above cannot reach
    // these code points, so /<astral>/iu would otherwise match only the exact code point.
    // No astral class folds onto a BMP code point (and vice versa), so the classes here are
    // pure-supplementary and never overlap the BMP handling. Built once, lazily.
    private static readonly Lazy<Dictionary<int, int[]>> AstralCaseFoldEquivalents =
        new(BuildAstralCaseFoldEquivalents);

    private static Dictionary<int, int[]> BuildAstralCaseFoldEquivalents()
    {
        var groups = new Dictionary<int, List<int>>();
        for (int cp = 0x10000; cp <= 0x10FFFF; cp++)
        {
            int key = CaseFoldKeyCodePoint(cp);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new List<int>(2);
            list.Add(cp);
        }

        var map = new Dictionary<int, int[]>();
        foreach (var list in groups.Values)
        {
            if (list.Count < 2)
                continue;
            foreach (var member in list)
            {
                var others = new int[list.Count - 1];
                int k = 0;
                foreach (var m in list)
                    if (m != member)
                        others[k++] = m;
                map[member] = others;
            }
        }

        // Supplement classes that .NET's Unicode tables still miss. Garay (added in
        // Unicode 16.0) has its code points assigned but carries NO simple case mapping in
        // the Rune round-trip above, so each letter is left in a singleton class and never
        // grouped. Seed its bicameral pairs (uppercase U+10D50..U+10D65 ⇄ lowercase
        // U+10D70..U+10D85) explicitly so /…/iu folds them as ECMAScript requires.
        for (int k = 0; k <= 0x15; k++)
            SeedCaseFoldPair(map, 0x10D50 + k, 0x10D70 + k);

        return map;
    }

    // Records a ↔ b as mutual case-fold equivalents, unioning with any class either is
    // already part of (de-duplicating and excluding self).
    private static void SeedCaseFoldPair(Dictionary<int, int[]> map, int a, int b)
    {
        AddCaseFoldEquivalent(map, a, b);
        AddCaseFoldEquivalent(map, b, a);
    }

    private static void AddCaseFoldEquivalent(Dictionary<int, int[]> map, int member, int other)
    {
        if (map.TryGetValue(member, out var existing))
        {
            if (System.Array.IndexOf(existing, other) >= 0)
                return;
            var grown = new int[existing.Length + 1];
            System.Array.Copy(existing, grown, existing.Length);
            grown[existing.Length] = other;
            map[member] = grown;
        }
        else
        {
            map[member] = new[] { other };
        }
    }

    // The other code points that match <paramref name="cp"/> case-insensitively under
    // ECMAScript Canonicalize in Unicode mode, for a supplementary-plane code point; null
    // when none (or when cp is in the BMP, which the char-based maps cover).
    private static int[] GetAstralCaseFoldEquivalents(int cp)
        => cp > 0xFFFF && AstralCaseFoldEquivalents.Value.TryGetValue(cp, out var eq) ? eq : null;

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static int HexValue(char c) =>
        c >= '0' && c <= '9' ? c - '0' : (c >= 'a' ? c - 'a' + 10 : c - 'A' + 10);
}
