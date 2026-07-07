using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.BuiltIns.Symbol;
using System;
using System.Text;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    [JSPrototypeMethod]
    [JSExport("match", Length = 1)]
    internal static JSValue Match(in Arguments a)
    {
        var @this = a.This;
        if (@this.IsNullOrUndefined)
            throw JSEngine.NewTypeError("String.prototype.match called on null or undefined");
        
        var reg = a.Get1();
        if (!reg.IsNullOrUndefined && reg.IsObject)
        {
            var matcher = reg[(IJSSymbol)JSSymbol.match];
            // GetMethod: a null @@match is treated as absent (fall through to the
            // default RegExpCreate path), not a non-callable error.
            if (!matcher.IsNullOrUndefined)
            {
                if (!matcher.IsFunction)
                    throw JSEngine.NewTypeError("@@match is not callable");

                return matcher.Call(reg, @this);
            }
        }

        if (reg is JSRegExp jSRegExp)
            return jSRegExp.Match(@this);

        // RegExpCreate(regexp, undefined): only an *undefined* pattern becomes the
        // empty pattern ""; null (and any other value) is coerced with ToString, so
        // `"…".match(null)` searches for the literal "null", not the empty pattern.
        var pattern = reg.IsUndefined ? "" : reg.StringValue;
        var created = new JSRegExp(pattern, "");
        var builtinMatcher = created[(IJSSymbol)JSSymbol.match];
        return builtinMatcher.InvokeFunction(new Arguments(created, @this));
    }

    // GetSubstitution (ECMA-262 §22.1.3.18.1) for a string searchValue: there are no captures
    // and no named captures, so only $$, $&, $` and $' are expanded; $n and $<name> have no
    // corresponding capture and are therefore emitted verbatim (the leading '$' is literal).
    private static string GetSubstitution(string matched, string str, int position, string replacement)
    {
        if (replacement.IndexOf('$') < 0)
            return replacement;

        var result = new StringBuilder(replacement.Length);
        for (int i = 0; i < replacement.Length; i++)
        {
            var c = replacement[i];
            if (c != '$' || i + 1 >= replacement.Length)
            {
                result.Append(c);
                continue;
            }

            switch (replacement[i + 1])
            {
                case '$': // $$ -> $
                    result.Append('$');
                    i++;
                    break;
                case '&': // $& -> the matched substring
                    result.Append(matched);
                    i++;
                    break;
                case '`': // $` -> the portion of the string before the match
                    result.Append(str, 0, position);
                    i++;
                    break;
                case '\'': // $' -> the portion of the string after the match
                    var tail = position + matched.Length;
                    result.Append(str, tail, str.Length - tail);
                    i++;
                    break;
                default:
                    // $n / $<name> with no captures: not a substitution, keep '$' literal.
                    result.Append('$');
                    break;
            }
        }

        return result.ToString();
    }

    [JSPrototypeMethod]
    [JSExport("replace", Length = 2)]
    internal static JSValue Replace(in Arguments a)
    {
        var @this = a.This.AsString();
        var (f, s) = a.Get2();
        if (!f.IsNullOrUndefined && f.IsObject)
        {
            var replacer = f[(IJSSymbol)JSSymbol.replace];
            // GetMethod semantics: a null @@replace is treated as absent.
            if (!replacer.IsNullOrUndefined)
            {
                if (!replacer.IsFunction)
                    throw JSEngine.NewTypeError("@@replace is not callable");

                return replacer.Call(f, a.This, s);
            }
        }

        if (f is JSRegExp jSRegExp)
            return new JSString(jSRegExp.Replace(@this, s));

        // Find the first occurrence of the (stringified) search value.
        var substr = f.StringValue;
        // Per §22.1.3.19 step 6, if replaceValue is not callable it is coerced via ToString
        // BEFORE the match is searched for — even when there is no match (test262
        // String/prototype/replace/replaceValue-evaluation-order[-regexp-object]).
        var replacementTemplate = s.IsFunction ? null : s.StringValue;
        int start = @this.IndexOf(substr, StringComparison.Ordinal);
        if (start == -1)
            return a.This;

        int end = start + substr.Length;

        // A functional replacement is called with (matched, position, string) — and only when
        // there is a match; a non-functional replacement is a template processed through
        // GetSubstitution (so $$, $&, $` and $' are expanded). Per §22.1.3.19, ToString of a
        // non-functional replaceValue happens before the substitution.
        var replaceText = s.IsFunction
            ? s.InvokeFunction(new Arguments(JSUndefined.Value,
                CreateString(substr), CreateNumber(start), CreateString(@this))).StringValue
            : GetSubstitution(substr, @this, start, replacementTemplate);

        // Replace only the first match.
        var result = new StringBuilder(@this.Length + (replaceText.Length - substr.Length));
        result.Append(@this, 0, start);
        result.Append(replaceText);
        result.Append(@this, end, @this.Length - end);
        return new JSString(result.ToString());
    }

    [JSPrototypeMethod]
    [JSExport("replaceAll", Length = 2)]
    internal static JSValue ReplaceAll(in Arguments a)
    {
        var @thisValue = a.This;
        if (@thisValue.IsNullOrUndefined)
            throw JSEngine.NewTypeError("String.prototype.replaceAll called on null or undefined");

        var (searchValue, replaceValue) = a.Get2();

        if (!searchValue.IsNullOrUndefined && searchValue.IsObject)
        {
            var isRegExp = searchValue[(IJSSymbol)JSSymbol.match];
            if (!isRegExp.IsUndefined && isRegExp.BooleanValue)
            {
                var flags = searchValue[KeyStrings.GetOrCreate("flags")];
                if (!flags.StringValue.Contains('g'))
                    throw JSEngine.NewTypeError("String.prototype.replaceAll called with a non-global RegExp argument");
            }

            var replacer = searchValue[(IJSSymbol)JSSymbol.replace];
            // GetMethod semantics: a null @@replace is treated as absent.
            if (!replacer.IsNullOrUndefined)
            {
                if (!replacer.IsFunction)
                    throw JSEngine.NewTypeError("@@replace is not callable");

                return replacer.Call(searchValue, @thisValue, replaceValue);
            }
        }

        // Step 3: ToString(O). Use the spec ToString (StringValue routes objects through
        // ToPrimitive with a string hint, honouring Symbol.toPrimitive) rather than CLR
        // ToString, which would call the object's own toString/valueOf directly.
        var @this = @thisValue.StringValue;
        var searchString = searchValue.IsUndefined ? "undefined" : searchValue.StringValue;
        var functionalReplace = replaceValue.IsFunction;
        var replacementText = functionalReplace ? null : replaceValue.StringValue;
        var source = CreateString(@this);

        string GetReplacement(int position)
            => functionalReplace
                ? replaceValue.InvokeFunction(new Arguments(JSUndefined.Value, CreateString(searchString), CreateNumber(position), source)).StringValue
                : GetSubstitution(searchString, @this, position, replacementText!);

        if (searchString.Length == 0)
        {
            var emptySearchResult = new StringBuilder();
            for (var position = 0; position <= @this.Length; position++)
            {
                emptySearchResult.Append(GetReplacement(position));
                if (position < @this.Length)
                    emptySearchResult.Append(@this[position]);
            }

            return CreateString(emptySearchResult.ToString());
        }

        var result = new StringBuilder();
        var searchStart = 0;
        while (true)
        {
            var matchIndex = @this.IndexOf(searchString, searchStart, StringComparison.Ordinal);
            if (matchIndex < 0)
                break;

            result.Append(@this, searchStart, matchIndex - searchStart);
            result.Append(GetReplacement(matchIndex));
            searchStart = matchIndex + searchString.Length;
        }

        if (result.Length == 0 && searchStart == 0)
            return CreateString(@this);

        result.Append(@this, searchStart, @this.Length - searchStart);
        return CreateString(result.ToString());
    }

    /// <summary>
    /// Splits this string into an array of strings by separating the string into substrings.
    /// </summary>
    /// <param name="engine"> The current script environment. </param>
    /// <param name="thisObject"> The string that is being operated on. </param>
    /// <param name="separator"> A string or regular expression that indicates where to split the string. </param>
    /// <param name="limit"> The maximum number of array items to return.  Defaults to unlimited. </param>
    /// <returns> An array containing the split strings. </returns>
    [JSPrototypeMethod]
    [JSExport("split", Length = 2)]
    internal static JSValue Split(in Arguments a)
    {
        var @thisValue = a.This;
        // §22.1.3.21 step 1: RequireObjectCoercible(this value). This precedes the
        // @@split lookup (step 2); ToString(this) is step 3 and must NOT run until the
        // @@split fast path has been ruled out
        // (test262: String/prototype/split/this-value-tostring-error).
        if (@thisValue.IsNullOrUndefined)
            throw JSEngine.NewTypeError("String.prototype.split called on null or undefined");

        var (_separator, limit) = a.Get2();

        if (!_separator.IsNullOrUndefined && _separator.IsObject)
        {
            var splitter = _separator[(IJSSymbol)JSSymbol.split];
            // GetMethod semantics: a null @@split is treated as absent.
            if (!splitter.IsNullOrUndefined)
            {
                if (!splitter.IsFunction)
                    throw JSEngine.NewTypeError("@@split is not callable");

                return limit.IsUndefined
                    ? splitter.InvokeFunction(new Arguments(_separator, @thisValue))
                    : splitter.InvokeFunction(new Arguments(_separator, @thisValue, limit));
            }
        }

        // §22.1.3.21 step 3: ToString(this value), now that no @@split method applies.
        var @this = @thisValue.AsString();

        // Limit defaults to unlimited. Note the ToUint32() conversion.
        // Spec order (§22.1.3.21): ToUint32(limit) is step 6, ToString(separator)
        // is step 7, and the lim==0 short-circuit is step 8 — so a side-effecting
        // separator.toString() must still run when the result is the empty array.
        var limitMax = uint.MaxValue;

        if (!limit.IsUndefined)
            limitMax = limit.UIntValue;

        if (_separator is JSRegExp jSRegExp)
        {
            if (limitMax == 0)
                return CreateArray();
            return jSRegExp.Split(@this, limitMax);
        }

        // Coerce the string-separator BEFORE the lim==0 / undefined shortcuts. A
        // truly undefined separator skips this step (spec step 9 takes over and
        // never inspects R), so we keep its single-element-array fastpath; any
        // other separator must be ToString'd here.
        string separator = null;
        if (!_separator.IsUndefined)
            separator = _separator.StringValue;

        if (limitMax == 0)
            return CreateArray();

        if (_separator.IsUndefined)
        {
            var single = CreateArray();
            single.AddArrayItem(new JSString(@this));
            return single;
        }

        var result = CreateArray();
        if (string.IsNullOrEmpty(separator))
        {
            for (int i = 0; i < @this.Length; i++)
                result[(uint)i] = new JSString(@this[i]);

            return result;
        }

        // .NET Split is buggy, it should not remove empty string entries
        // when StringSplitOptions is None
        var splitStrings = @this.Split([separator], StringSplitOptions.None);
        if (limitMax < splitStrings.Length)
        {
            var splitStrings2 = new string[limitMax];
            System.Array.Copy(splitStrings, splitStrings2, (int)limitMax);
            splitStrings = splitStrings2;
        }

        foreach (var item in splitStrings)
            result.AddArrayItem(new JSString(item));

        return result;
    }
}
