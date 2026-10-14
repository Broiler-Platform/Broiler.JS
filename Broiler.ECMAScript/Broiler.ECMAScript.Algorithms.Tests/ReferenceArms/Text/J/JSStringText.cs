// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-5-001 (docs/evidence/sjl-5-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 62c42e3 (SJL-4's merge, the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/String/JSStringPrototype.Extract.cs
//     blob fe114acc613b11bc8a2a4848535dada8c63e46ac: Clamp and Relative (:18-23), CodePointAt (:93-115)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/String/JSStringPrototype.Transform.cs
//     blob 4c579d15bb62e752a9e81cf92d517106be5cacb0: StringPad (:51-80), IsWellFormedUtf16 (:193-212) and
//     ToWellFormedUtf16 (:214-243)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/String/JSStringPrototype.Pattern.cs
//     blob 26bebd46671068be7000b491c532c09d42f2f2dd: GetSubstitution (:49-94)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSTypedArray.prototype.cs
//     blob f21356826f531d602562507121d2dc799cbfb039: ClampRelativeIndex (:38-46)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/BuiltInsAssemblyInitializer.cs
//     blob c2f97468cb8b486d952288c042a110be3c3e0ddd: the local functions AdvanceStringIndex (:1493-1500) and
//     GetSubstitution (:1502-1593)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs
//     blob 6822a824d3a593da03baafc69f6c03b09ea4181b: Replace(string, string) (:587-672)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.Broiler.cs
//     blob 67db1da236e1958aa584cb547c7b45b38e3492ea: RegexCapture and RegexMatchData (:263-280), the match the
//     template walk reads
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF, and a byte-order mark dropped;
//   - the namespace; the members sit in a static class of their own; private and public members are internal;
//   - the [JSExport...] and [JSPrototypeMethod] attributes are not copied;
//   - CodePointAt takes the receiver's String and the position's ToIntegerOrInfinity (IntegerArgument) and returns
//     the code point's double, NaN for undefined, where the source takes the arguments and wraps the result;
//   - StringPad takes ToLength(maxLength), which the source computes on its first line, and the filler's ToString
//     (null for undefined), and returns the String where the source wraps it in a JSString; its RangeError is the
//     recorded ArmError;
//   - the initializer's two local functions are static members, the second named InitializerGetSubstitution;
//     its captures are strings (null for undefined) and its named captures a dictionary of plain values (null for
//     undefined), so `namedCaptures.IsUndefined` is `namedCaptures is null`, the object test and its TypeError
//     (unreachable for a dictionary) go, the property read is the dictionary's lookup with an absent key read as
//     undefined, and `capture.IsUndefined` is `capture is null`;
//   - Replace(string, string) is a static function of the input, the template and one RegexMatchData: the source's
//     ReplaceMatches(input, match => ...) runs the lambda once per match of the engine's regular expression, and
//     the copy runs the same lambda once, on the match the comparison hands it; RegexCapture and RegexMatchData
//     are nested in JSRegExp in the source and sit beside the class here, RegexMatchData's two reference fields
//     initialised with the null-forgiving null, as the source's project builds without nullable warnings.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.J;

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
    public string Value = null!;
    public RegexCapture[] Groups = null!;

    public static readonly RegexMatchData NoMatch = new() { Success = false };
}

/// <summary>Broiler.JS's string and template members, as copied.</summary>
internal static class JSStringText
{
    internal static int Clamp(double position, int length)
        => position <= 0 ? 0 : position >= length ? length : (int)position;

    internal static int Relative(double position, int length)
        => position < 0 ? Clamp(length + position, length) : Clamp(position, length);

    internal static double CodePointAt(string text, double position)
    {
        if (position < 0 || position >= text.Length)
            return double.NaN;

        var pos = (int)position;
        int firstCodePoint = text[pos];
        if (firstCodePoint < 0xD800 || firstCodePoint > 0xDBFF || pos + 1 == text.Length)
            return firstCodePoint;

        int secondCodePoint = text[pos + 1];
        if (secondCodePoint < 0xDC00 || secondCodePoint > 0xDFFF)
            return firstCodePoint;

        var output = (double)((firstCodePoint - 0xD800) * 1024 + (secondCodePoint - 0xDC00) + 0x10000);
        return output;

    }

    internal static bool IsWellFormedUtf16(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    return false;

                i++;
            }
            else if (char.IsLowSurrogate(ch))
            {
                return false;
            }
        }

        return true;
    }

    internal static string ToWellFormedUtf16(string value)
    {
        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    result.Append(ch);
                    result.Append(value[++i]);
                }
                else
                {
                    result.Append('\uFFFD');
                }
            }
            else if (char.IsLowSurrogate(ch))
            {
                result.Append('\uFFFD');
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }

    internal static string StringPad(string str, long maxLength, string? fillArg, bool padStart)
    {

        if (maxLength <= str.Length)
            return str;

        var fillString = fillArg is null ? " " : fillArg;
        if (fillString.Length == 0)
            return str;

        // A result longer than this engine's strings hold is a RangeError, as repeat's is.
        if (maxLength > int.MaxValue)
            throw new ArmError("RangeError", "Invalid string length");

        var fillLength = (int)maxLength - str.Length;
        var filler = new StringBuilder(fillLength);
        while (filler.Length < fillLength)
            filler.Append(fillString);
        filler.Length = fillLength;

        return padStart
            ? filler.Append(str).ToString()
            : str + filler;
    }

    internal static int ClampRelativeIndex(long relative, int len)
    {
        if (relative < 0)
            return relative <= -len ? 0 : (int)(len + relative);
        return relative >= len ? len : (int)relative;
    }

    internal static string GetSubstitution(string matched, string str, int position, string replacement)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int AdvanceStringIndex(string input, int index, bool unicode)
    {
        if (!unicode || index + 1 >= input.Length || !char.IsHighSurrogate(input[index]) || !char.IsLowSurrogate(input[index + 1]))
            return index + 1;

        return index + 2;
    }

    internal static string InitializerGetSubstitution(string matched, string input, int position, IReadOnlyList<string?> captures, IReadOnlyDictionary<string, string?>? namedCaptures, string replacement)
    {
        if (replacement.IndexOf('$') < 0)
            return replacement;

        var replacementBuilder = new StringBuilder();
        for (int i = 0; i < replacement.Length; i++)
        {
            var c = replacement[i];
            if (c != '$' || i >= replacement.Length - 1)
            {
                replacementBuilder.Append(c);
                continue;
            }

            c = replacement[++i];
            switch (c)
            {
                case '$':
                    replacementBuilder.Append('$');
                    break;
                case '&':
                    replacementBuilder.Append(matched);
                    break;
                case '`':
                    replacementBuilder.Append(input.AsSpan(0, Math.Max(position, 0)));
                    break;
                case '\'':
                    replacementBuilder.Append(input.AsSpan(Math.Min(position + matched.Length, input.Length)));
                    break;
                case '<':
                {
                    // §22.1.3.18.1: $< introduces a named-capture reference ONLY when the
                    // pattern contains named groups (namedCaptures is defined). When it is
                    // undefined, the literal characters "$<" are emitted and parsing resumes
                    // immediately after them, so `"abcd".replace(/(.)(.)/, "$<x>")` yields
                    // "$<x>cd" rather than swallowing the "<x>".
                    if (namedCaptures is null)
                    {
                        replacementBuilder.Append("$<");
                        break;
                    }

                    var end = replacement.IndexOf('>', i + 1);
                    if (end < 0)
                    {
                        replacementBuilder.Append("$<");
                        break;
                    }

                    var groupName = replacement.Substring(i + 1, end - i - 1);
                    var capture = namedCaptures.TryGetValue(groupName, out var named) ? named : null;
                    if (capture is not null)
                        replacementBuilder.Append(capture.ToString());

                    i = end;
                    break;
                }
                default:
                    if (c is >= '0' and <= '9')
                    {
                        var captureIndex = c - '0';
                        if (i < replacement.Length - 1 && replacement[i + 1] is >= '0' and <= '9')
                        {
                            var twoDigitIndex = (captureIndex * 10) + (replacement[i + 1] - '0');
                            if (twoDigitIndex > 0 && twoDigitIndex <= captures.Count)
                            {
                                captureIndex = twoDigitIndex;
                                i++;
                            }
                        }

                        if (captureIndex > 0 && captureIndex <= captures.Count)
                        {
                            var capture = captures[captureIndex - 1];
                            if (capture is not null)
                                replacementBuilder.Append(capture.ToString());
                            break;
                        }
                    }

                    replacementBuilder.Append('$');
                    replacementBuilder.Append(c);
                    break;
            }
        }

        return replacementBuilder.ToString();
    }

    /// <summary>
    /// Returns a copy of the given string with text replaced using a regular expression.
    /// </summary>
    /// <param name="input"> The string on which to perform the search. </param>
    /// <param name="replaceText"> A string containing the text to replace for every successful match. </param>
    /// <returns> A copy of the given string with text replaced using a regular expression. </returns>
    internal static string Replace(string input, string replaceText, RegexMatchData match)
    {
        // Check if the replacement string contains any patterns.
        bool replaceTextContainsPattern = replaceText.IndexOf('$') >= 0;

        return ((Func<RegexMatchData, string>)(match =>
        {
            // If there is no pattern, replace the pattern as is.
            if (replaceTextContainsPattern == false)
                return replaceText;

            var groups = match.Groups;

            // An unmatched capture substitutes the empty string, matching .NET's Group.Value
            // for a non-participating group (RegexMatchData reports it as null).
            string Capture(int n) => groups[n].Success ? groups[n].Value : string.Empty;

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
                        if (matchNumber2 > 0 && matchNumber2 < groups.Length)
                        {
                            // Two digit capture replacement.
                            replacementBuilder.Append(Capture(matchNumber2));
                            i++;
                        }
                        else if (matchNumber1 > 0 && matchNumber1 < groups.Length)
                        {
                            // Single digit capture replacement.
                            replacementBuilder.Append(Capture(matchNumber1));
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
        }))(match);
    }
}
