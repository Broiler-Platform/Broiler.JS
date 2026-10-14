// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-5-001 (docs/evidence/sjl-5-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 1f253ff (SJL-4's merge, the commit the area verdict's correspondence reads):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.String.cs
//     blob 4a7219b4c74419f7f60295966c39169a31d537ed: StringLengthCeiling (:58), StringCharge (:1045-1046), the bodies of the
//     codePointAt, isWellFormed and toWellFormed built-ins (:248-270, :289-331), StringWellFormed (:695-717),
//     StringRelativeIndex (:1146-1155), StringPadTo (:1457-1506) and StringExpandDollars (:1608-1661)
//   src/Broiler.VM.Profile.JavaScript/JsRealm.RegExp.cs
//     blob 4c73835b1cd2c45d9d59e1d5b6afb7a8c727f667: RegExpAdvanceIndex (:1231-1234), RegExpAppend (:1257-1273),
//     RegExpReplaceText (:2067-2142) and RegExpSubstitute (:2203-2312)
//   src/Broiler.VM.Profile.JavaScript.Format/JsRegExpMatcher.cs
//     blob 022861d79e7094d92ae3aeea4ebde8f2d05d2fe4: Advance (:855-866)
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Array.cs
//     blob 8c0a4248459f20bf63741e90eda527bf3acbd973: ArrayRelative (:1609-1620)
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Global.cs
//     blob 143a29b97b16d8e2d09acee49741b541ce9e296d: the five sets (:52-81), GlobalUriEncode (:333-376),
//     GlobalUriDecode (:381-454), GlobalUriOctetAt (:459-475), GlobalUriSequenceLength (:480-486),
//     GlobalUriAppendUtf8 (:491-515), GlobalUriAppendOctet (:520-525), GlobalUriAppendCodePoint (:530-541),
//     GlobalEscapeText (:546-576), GlobalUnescapeText (:581-631), GlobalUriHexValue (:636-642),
//     GlobalUriIsAlphanumeric (:647-648) and GlobalUriInSet (:657-668)
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Base64.cs
//     blob 331394be664dce5cc87fe8819101056d0a311cdb: Base64Alphabet (:48-49), the toHex built-in's encoder
//     (:114-115), Base64Encode (:320-345), Base64Result (:350-366), Base64SkipSpace (:371-379),
//     Base64Decode (:393-529), Base64DecodeFull (:534-546), Base64DecodeFinal (:554-573), Base64HexDigit
//     (:578-584) and Base64DecodeHex (:592-620)
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the members sit in a static class JsRealmText of their own instead of the partial class
//     JsRealm, whose other members are the engine's; private members are internal;
//   - the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines), the
//     generated assurance header and the SPDX lines removed;
//   - JsEngine is the arm's JsEngine below: Charge(ulong) adds to the arm's meter, which the comparison reads,
//     and Error(name, message) is the recorded exception JsThrow carrying the error's constructor name;
//   - StringPadTo takes the receiver's String, maxLength's ToInteger and the filler argument (null for
//     undefined, or its ToString) where the source takes (thisValue, arguments) and runs StringThis,
//     engine.ToInteger and engine.ToStringValue in that order; it returns the String where the source wraps
//     it in JsValue.String;
//   - the codePointAt, isWellFormed, toWellFormed and toHex built-ins are the thin functions CodePointAt,
//     IsWellFormed, ToWellFormed and ToHex: each takes what the lambda's first lines compute (the receiver's
//     String, the position's ToInteger, the bytes) and returns what the lambda wraps (NaN for undefined in
//     CodePointAt), the lambda's other lines verbatim;
//   - RegExpReplaceText takes the replacement's ToString where the source takes the JsValue and converts it
//     (`callable` is false, `converted` the parameter); a callable replacement, which the comparison never
//     passes, throws NotSupportedException where the source calls it;
//   - RegExpSubstitute takes the result's captures as strings (null for undefined) and its groups as a
//     dictionary of plain values (null for undefined groups, an absent key read as undefined): the source's
//     `engine.GetProperty(namedCaptures, name)` and `engine.ToStringValue(capture)` are the dictionary's
//     lookup and the value itself, `X.Type == JsType.Undefined` is `X is null`, and `value.AsString()` is
//     `value`;
//   - ArrayRelative takes the value's ToInteger, where the source takes the value and calls engine.ToInteger;
//   - JsRegExpMatcher.Advance sits in a nested class of the same name.

using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.V;

/// <summary>The arm's engine: the meter Broiler.VM's members charge, and the errors they raise.</summary>
internal sealed class JsEngine
{
    public ulong Charged { get; private set; }

    public void Charge(ulong units) => Charged += units;

    public Broiler.ECMAScript.WorkCharge WorkCharge => Charge;

    public JsThrow Error(string name, string message) => new(name, message);
}

/// <summary>The arm's recorded exception: an engine error by its constructor's name.</summary>
internal sealed class JsThrow(string name, string message) : ArmError(name, message);

/// <summary>Broiler.VM's text members, as copied.</summary>
internal static class JsRealmText
{
    /// <summary>
    /// The longest string <c>repeat</c>, <c>padStart</c> and <c>padEnd</c> will materialise.
    /// </summary>
    /// <remarks>
    /// The specification's ceiling is 2^53-1, which no implementation can honour and which this one
    /// would meet by exhausting the host's memory rather than the guest's allowance. Sixteen million
    /// characters is thirty-two megabytes of UTF-16 and is a declared deviation: a program that asks
    /// for more gets the same <c>RangeError</c> the specification gives for a length it cannot
    /// represent.
    /// </remarks>
    private const int StringLengthCeiling = 1 << 24;

    /// <summary>Charges fuel for work proportional to <paramref name="units"/> characters.</summary>
    private static void StringCharge(JsEngine engine, int units) =>
        engine.Charge(units <= 0 ? 1UL : (ulong)units);

    /// <summary>Whether every surrogate in the text is half of a pair.</summary>
    internal static bool StringWellFormed(string text)
    {
        for (var at = 0; at < text.Length; at++)
        {
            var unit = text[at];

            if (!char.IsSurrogate(unit))
            {
                continue;
            }

            if (!char.IsHighSurrogate(unit) ||
                at + 1 >= text.Length ||
                !char.IsLowSurrogate(text[at + 1]))
            {
                return false;
            }

            at++;
        }

        return true;
    }

    /// <summary>Resolves a relative position, where a negative one counts from the end.</summary>
    internal static int StringRelativeIndex(double position, int length)
    {
        if (position < 0)
        {
            var shifted = length + position;
            return shifted <= 0 ? 0 : (int)shifted;
        }

        return position >= length ? length : (int)position;
    }

    /// <summary>The shared body of <c>padStart</c> and <c>padEnd</c>.</summary>
    internal static string StringPadTo(
        JsEngine engine, string text, double target, string? fillArgument, bool atStart)
    {

        if (target <= text.Length)
        {
            StringCharge(engine, text.Length);
            return text;
        }

        if (target > StringLengthCeiling)
        {
            throw engine.Error("RangeError", "Invalid string length");
        }

        var filler = fillArgument is null
            ? " "
            : fillArgument;

        if (filler.Length == 0)
        {
            StringCharge(engine, text.Length);
            return text;
        }

        var total = (int)target;
        StringCharge(engine, total);
        var builder = new System.Text.StringBuilder(total);

        if (!atStart)
        {
            builder.Append(text);
        }

        for (var written = 0; written < total - text.Length; written++)
        {
            builder.Append(filler[written % filler.Length]);
        }

        if (atStart)
        {
            builder.Append(text);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Expands the dollar escapes a string replacement may carry.
    /// </summary>
    /// <remarks>
    /// <c>$$</c> is one dollar, <c>$&amp;</c> the match, <c>$`</c> what came before it and
    /// <c>$'</c> what came after. A dollar followed by anything else is a dollar: the specification
    /// leaves an unrecognised escape alone rather than dropping it.
    /// </remarks>
    internal static string StringExpandDollars(
        string template, string matched, string text, int position)
    {
        if (template.IndexOf('$', System.StringComparison.Ordinal) < 0)
        {
            return template;
        }

        var builder = new System.Text.StringBuilder(template.Length);

        for (var at = 0; at < template.Length; at++)
        {
            var character = template[at];

            if (character != '$' || at + 1 >= template.Length)
            {
                builder.Append(character);
                continue;
            }

            switch (template[at + 1])
            {
                case '$':
                    builder.Append('$');
                    at++;
                    break;

                case '&':
                    builder.Append(matched);
                    at++;
                    break;

                case '`':
                    builder.Append(text, 0, position);
                    at++;
                    break;

                case '\'':
                    builder.Append(
                        text,
                        position + matched.Length,
                        text.Length - position - matched.Length);

                    at++;
                    break;

                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>The language's <c>AdvanceStringIndex</c> over an index that may be past the input.</summary>
    internal static double RegExpAdvanceIndex(string input, double index, bool fullUnicode) =>
        fullUnicode && index + 1 < input.Length
            ? JsRegExpMatcher.Advance(input, (int)index, true)
            : index + 1;

    /// <summary>Appends one piece of a replacement's answer, charged and held to the ceiling.</summary>
    internal static void RegExpAppend(
        JsEngine engine, System.Text.StringBuilder builder, string text, int start = 0, int count = -1)
    {
        if (count < 0)
        {
            count = text.Length - start;
        }

        engine.Charge((ulong)count + 1);

        if ((long)builder.Length + count > StringLengthCeiling)
        {
            throw engine.Error("RangeError", "Invalid string length");
        }

        builder.Append(text, start, count);
    }

    /// <summary><c>replace</c> with a string on the left, which replaces the first occurrence only.</summary>
    internal static string RegExpReplaceText(
        JsEngine engine, string input, string search, string replacement)
    {
        // A TEXT REPLACEMENT IS CONVERTED BEFORE THE SEARCH, so its `toString` runs once even when
        // nothing is found - the language's order.
        var callable = false;
        var converted = replacement;
        var at = input.IndexOf(search, StringComparison.Ordinal);

        if (at < 0)
        {
            return input;
        }

        string produced;

        if (callable)
        {
            throw new System.NotSupportedException("arm V: a callable replacement is the engine's");
        }
        else
        {
            var template = converted;
            var builder = new System.Text.StringBuilder(template.Length);
            var step = 0;

            while (step < template.Length)
            {
                var character = template[step];

                if (character != '$' || step + 1 >= template.Length)
                {
                    builder.Append(character);
                    step++;
                    continue;
                }

                // A string pattern has no captures, so `$1`..`$9` stay literal and only the four
                // capture-free substitutions mean anything.
                switch (template[step + 1])
                {
                    case '$':
                        builder.Append('$');
                        step += 2;
                        continue;

                    case '&':
                        builder.Append(search);
                        step += 2;
                        continue;

                    case '`':
                        builder.Append(input, 0, at);
                        step += 2;
                        continue;

                    case '\'':
                        builder.Append(input, at + search.Length, input.Length - at - search.Length);
                        step += 2;
                        continue;

                    default:
                        builder.Append(character);
                        step++;
                        continue;
                }
            }

            produced = builder.ToString();
        }

        return input.Substring(0, at) + produced + input.Substring(at + search.Length);
    }

    /// <summary>
    /// The language's <c>GetSubstitution</c>: a text replacement with its dollar substitutions
    /// expanded against one result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Everything comes from the result, not from a matcher.</b> <paramref name="captures"/> are
    /// the result's converted captures and <paramref name="namedCaptures"/> its <c>groups</c>
    /// (already through <c>ToObject</c>, or <c>undefined</c>), so a custom <c>exec</c>'s answer is
    /// expanded exactly as a built-in one is.
    /// </para>
    /// <para>
    /// <b>The digit rule is the pinned edition's.</b> <c>$nn</c> is taken as two digits first and
    /// falls back to one only when the two-digit number is past the capture count, so <c>$01</c>
    /// is the first capture, <c>$10</c> with one capture is the first capture then "0", and
    /// <c>$0</c> and <c>$00</c> stay literal. <c>$&lt;name&gt;</c> means something only when the
    /// result has <c>groups</c> and a later "&gt;" closes it; the name is then read with an ordinary
    /// <c>Get</c> and an <c>undefined</c> value expands to nothing. <c>$'</c> starts at the end of
    /// the matched text, clamped to the input, since a custom result may claim more text than is
    /// left. The template is charged once up front and each expansion as it is appended.
    /// </para>
    /// </remarks>
    internal static string RegExpSubstitute(
        JsEngine engine,
        string matched,
        string input,
        int position,
        List<string?> captures,
        IReadOnlyDictionary<string, string?>? namedCaptures,
        string template)
    {
        engine.Charge((ulong)template.Length + 1);
        var builder = new System.Text.StringBuilder(template.Length);
        var count = captures.Count;
        var at = 0;

        while (at < template.Length)
        {
            var character = template[at];

            if (character != '$' || at + 1 >= template.Length)
            {
                builder.Append(character);
                at++;
                continue;
            }

            var next = template[at + 1];

            switch (next)
            {
                case '$':
                    builder.Append('$');
                    at += 2;
                    continue;

                case '&':
                    RegExpAppend(engine, builder, matched);
                    at += 2;
                    continue;

                case '`':
                    RegExpAppend(engine, builder, input, 0, position);
                    at += 2;
                    continue;

                case '\'':
                    var tail = Math.Min(position + matched.Length, input.Length);
                    RegExpAppend(engine, builder, input, tail, input.Length - tail);
                    at += 2;
                    continue;

                case '<':
                    var close = template.IndexOf('>', at + 2);

                    if (close < 0 || namedCaptures is null)
                    {
                        builder.Append("$<");
                        at += 2;
                        continue;
                    }

                    var capture = namedCaptures.TryGetValue(template.Substring(at + 2, close - at - 2), out var named) ? named : null;

                    if (capture is not null)
                    {
                        RegExpAppend(engine, builder, capture);
                    }

                    at = close + 1;
                    continue;

                default:
                    break;
            }

            if (next is >= '0' and <= '9')
            {
                var digits = at + 2 < template.Length && template[at + 2] is >= '0' and <= '9' ? 2 : 1;
                var index = digits == 2 ? ((next - '0') * 10) + (template[at + 2] - '0') : next - '0';

                if (index > count && digits == 2)
                {
                    digits = 1;
                    index = next - '0';
                }

                if (index >= 1 && index <= count)
                {
                    var value = captures[index - 1];

                    if (value is not null)
                    {
                        RegExpAppend(engine, builder, value);
                    }
                }
                else
                {
                    builder.Append(template, at, 1 + digits);
                }

                at += 1 + digits;
                continue;
            }

            builder.Append(character);
            at++;
        }

        return builder.ToString();
    }

    /// <summary>The characters <c>encodeURIComponent</c> leaves alone beyond the alphanumerics.</summary>
    internal const string GlobalUriUnescapedComponent = "-_.!~*'()";

    /// <summary>The characters <c>encodeURI</c> leaves alone beyond the alphanumerics.</summary>
    /// <remarks>
    /// The component set, plus <c>uriReserved</c> and <c>#</c>. A URI's own punctuation has to
    /// survive encoding or the result no longer parses as the URI it came from.
    /// </remarks>
    internal const string GlobalUriUnescapedUri = "-_.!~*'();/?:@&=+$,#";

    /// <summary>The characters <c>decodeURI</c> leaves escaped when it finds them escaped.</summary>
    /// <remarks>
    /// The mirror of the extra set above: decoding <c>%2F</c> to <c>/</c> would change the
    /// structure of the URI rather than its content, so the escape is copied through verbatim.
    /// <c>decodeURIComponent</c> passes the empty set and therefore decodes everything.
    /// </remarks>
    internal const string GlobalUriPreservedUri = ";/?:@&=+$,#";

    /// <summary>The characters Annex B's <c>escape</c> leaves alone beyond the alphanumerics.</summary>
    internal const string GlobalEscapeUnescaped = "@*_+-./";

    /// <summary>The escape hex alphabet, upper-case because the specification says upper-case.</summary>
    internal const string GlobalUriHexDigits = "0123456789ABCDEF";

    /// <summary>The specification's <c>Encode</c> over an explicit unescaped set.</summary>
    internal static string GlobalUriEncode(JsEngine engine, string text, string unescaped)
    {
        var result = new StringBuilder(text.Length);

        for (var at = 0; at < text.Length; at++)
        {
            engine.Charge(1);
            var current = text[at];

            if (GlobalUriIsAlphanumeric(current) || GlobalUriInSet(unescaped, current))
            {
                result.Append(current);
                continue;
            }

            int point;

            if (current is >= '\uD800' and <= '\uDBFF')
            {
                // A leading surrogate is only half a code point. The other half has to be there
                // and has to be a trailing surrogate; anything else is a URIError and not a
                // replacement character, which is where every delegated implementation differs.
                if (at + 1 >= text.Length || text[at + 1] is < '\uDC00' or > '\uDFFF')
                {
                    throw engine.Error("URIError", "URI malformed");
                }

                point = 0x10000 + ((current - 0xD800) << 10) + (text[at + 1] - 0xDC00);
                at++;
            }
            else if (current is >= '\uDC00' and <= '\uDFFF')
            {
                throw engine.Error("URIError", "URI malformed");
            }
            else
            {
                point = current;
            }

            GlobalUriAppendUtf8(result, point);
        }

        return result.ToString();
    }

    /// <summary>The specification's <c>Decode</c> over an explicit preserved set.</summary>
    internal static string GlobalUriDecode(JsEngine engine, string text, string preserved)
    {
        var result = new StringBuilder(text.Length);
        var at = 0;

        while (at < text.Length)
        {
            engine.Charge(1);

            if (text[at] != '%')
            {
                result.Append(text[at]);
                at++;
                continue;
            }

            var start = at;
            var lead = GlobalUriOctetAt(engine, text, at);
            at += 3;

            if ((lead & 0x80) == 0)
            {
                var decoded = (char)lead;

                if (GlobalUriInSet(preserved, decoded))
                {
                    // Preserved: the three characters of the escape are copied through unchanged,
                    // so a second decode of the result is not a different string.
                    result.Append(text, start, 3);
                }
                else
                {
                    result.Append(decoded);
                }

                continue;
            }

            var length = GlobalUriSequenceLength(engine, lead);
            var point = lead & (0xFF >> (length + 1));

            for (var extra = 1; extra < length; extra++)
            {
                engine.Charge(1);
                var continuation = GlobalUriOctetAt(engine, text, at);
                at += 3;

                if ((continuation & 0xC0) != 0x80)
                {
                    throw engine.Error("URIError", "URI malformed");
                }

                point = (point << 6) | (continuation & 0x3F);
            }

            // An overlong encoding, a surrogate half and anything past the last plane are all
            // sequences a naive decoder accepts and the specification calls malformed.
            var minimum = length switch
            {
                2 => 0x80,
                3 => 0x800,
                _ => 0x10000,
            };

            if (point < minimum || point > 0x10FFFF || point is >= 0xD800 and <= 0xDFFF)
            {
                throw engine.Error("URIError", "URI malformed");
            }

            GlobalUriAppendCodePoint(result, point);
        }

        return result.ToString();
    }

    /// <summary>Reads the <c>%XX</c> at <paramref name="at"/>, or throws a <c>URIError</c>.</summary>
    internal static int GlobalUriOctetAt(JsEngine engine, string text, int at)
    {
        if (at + 2 >= text.Length || text[at] != '%')
        {
            throw engine.Error("URIError", "URI malformed");
        }

        var high = GlobalUriHexValue(text[at + 1]);
        var low = GlobalUriHexValue(text[at + 2]);

        if (high < 0 || low < 0)
        {
            throw engine.Error("URIError", "URI malformed");
        }

        return (high << 4) | low;
    }

    /// <summary>How many octets the UTF-8 sequence opened by <paramref name="lead"/> has.</summary>
    internal static int GlobalUriSequenceLength(JsEngine engine, int lead) => lead switch
    {
        >= 0xC0 and <= 0xDF => 2,
        >= 0xE0 and <= 0xEF => 3,
        >= 0xF0 and <= 0xF7 => 4,
        _ => throw engine.Error("URIError", "URI malformed"),
    };

    /// <summary>Appends the UTF-8 octets of a code point, each as <c>%XX</c>.</summary>
    internal static void GlobalUriAppendUtf8(StringBuilder text, int point)
    {
        if (point <= 0x7F)
        {
            GlobalUriAppendOctet(text, point);
        }
        else if (point <= 0x7FF)
        {
            GlobalUriAppendOctet(text, 0xC0 | (point >> 6));
            GlobalUriAppendOctet(text, 0x80 | (point & 0x3F));
        }
        else if (point <= 0xFFFF)
        {
            GlobalUriAppendOctet(text, 0xE0 | (point >> 12));
            GlobalUriAppendOctet(text, 0x80 | ((point >> 6) & 0x3F));
            GlobalUriAppendOctet(text, 0x80 | (point & 0x3F));
        }
        else
        {
            GlobalUriAppendOctet(text, 0xF0 | (point >> 18));
            GlobalUriAppendOctet(text, 0x80 | ((point >> 12) & 0x3F));
            GlobalUriAppendOctet(text, 0x80 | ((point >> 6) & 0x3F));
            GlobalUriAppendOctet(text, 0x80 | (point & 0x3F));
        }
    }

    /// <summary>Appends one octet as <c>%XX</c> in upper-case hex.</summary>
    internal static void GlobalUriAppendOctet(StringBuilder text, int octet)
    {
        text.Append('%');
        text.Append(GlobalUriHexDigits[(octet >> 4) & 0xF]);
        text.Append(GlobalUriHexDigits[octet & 0xF]);
    }

    /// <summary>Appends a code point as one code unit or as a surrogate pair.</summary>
    internal static void GlobalUriAppendCodePoint(StringBuilder text, int point)
    {
        if (point <= 0xFFFF)
        {
            text.Append((char)point);
            return;
        }

        var shifted = point - 0x10000;
        text.Append((char)(0xD800 + (shifted >> 10)));
        text.Append((char)(0xDC00 + (shifted & 0x3FF)));
    }

    /// <summary>Annex B's <c>escape</c>, which works over code units and never throws.</summary>
    internal static string GlobalEscapeText(JsEngine engine, string text)
    {
        var result = new StringBuilder(text.Length);

        for (var at = 0; at < text.Length; at++)
        {
            engine.Charge(1);
            var current = text[at];

            if (GlobalUriIsAlphanumeric(current) || GlobalUriInSet(GlobalEscapeUnescaped, current))
            {
                result.Append(current);
            }
            else if (current < 256)
            {
                GlobalUriAppendOctet(result, current);
            }
            else
            {
                // The `%uXXXX` form is this function's alone: a code unit above 255 is emitted as
                // itself rather than as the UTF-8 octets of the code point it may be half of.
                result.Append("%u");
                result.Append(GlobalUriHexDigits[(current >> 12) & 0xF]);
                result.Append(GlobalUriHexDigits[(current >> 8) & 0xF]);
                result.Append(GlobalUriHexDigits[(current >> 4) & 0xF]);
                result.Append(GlobalUriHexDigits[current & 0xF]);
            }
        }

        return result.ToString();
    }

    /// <summary>Annex B's <c>unescape</c>, which copies a malformed escape through verbatim.</summary>
    internal static string GlobalUnescapeText(JsEngine engine, string text)
    {
        var result = new StringBuilder(text.Length);
        var at = 0;

        while (at < text.Length)
        {
            engine.Charge(1);
            var current = text[at];

            if (current == '%')
            {
                var first = at + 5 < text.Length && text[at + 1] == 'u'
                    ? GlobalUriHexValue(text[at + 2])
                    : -1;

                if (first >= 0)
                {
                    var second = GlobalUriHexValue(text[at + 3]);
                    var third = GlobalUriHexValue(text[at + 4]);
                    var fourth = GlobalUriHexValue(text[at + 5]);

                    if (second >= 0 && third >= 0 && fourth >= 0)
                    {
                        current = (char)((first << 12) | (second << 8) | (third << 4) | fourth);
                        at += 5;
                        result.Append(current);
                        at++;
                        continue;
                    }
                }

                if (at + 2 < text.Length)
                {
                    var high = GlobalUriHexValue(text[at + 1]);
                    var low = GlobalUriHexValue(text[at + 2]);

                    if (high >= 0 && low >= 0)
                    {
                        current = (char)((high << 4) | low);
                        at += 2;
                    }
                }
            }

            result.Append(current);
            at++;
        }

        return result.ToString();
    }

    /// <summary>The value of one hex digit, or a negative number when it is not one.</summary>
    internal static int GlobalUriHexValue(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };

    /// <summary>Whether <paramref name="character"/> is an ASCII letter or digit.</summary>
    internal static bool GlobalUriIsAlphanumeric(char character) =>
        character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9');

    /// <summary>Whether <paramref name="set"/> holds <paramref name="character"/>.</summary>
    /// <remarks>
    /// A loop rather than <c>IndexOf</c>: the sets are under twenty characters, and a hand-written
    /// scan has no culture to specify and no analyzer to satisfy about which comparison it means.
    /// </remarks>
    internal static bool GlobalUriInSet(string set, char character)
    {
        for (var at = 0; at < set.Length; at++)
        {
            if (set[at] == character)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The standard base64 alphabet, which <c>base64url</c> differs from in two places.</summary>
    internal const string Base64Alphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    /// <summary>Encodes <paramref name="bytes"/> as RFC 4648 section 4 or section 5 base64.</summary>
    internal static string Base64Encode(JsEngine engine, byte[] bytes, bool urlSafe, bool omitPadding)
    {
        engine.Charge(((ulong)bytes.Length * 4 / 3) + 4);

        if (urlSafe)
        {
            var text = System.Buffers.Text.Base64Url.EncodeToString(bytes);
            if (!omitPadding)
            {
                var mod = text.Length % 4;
                if (mod == 2)
                {
                    return text + "==";
                }
                if (mod == 3)
                {
                    return text + "=";
                }
            }

            return text;
        }

        var standard = Convert.ToBase64String(bytes);
        return omitPadding ? standard.TrimEnd('=') : standard;
    }

    /// <summary>What a decode produced: the bytes, how much of the text it read, and its error.</summary>
    internal sealed class Base64Result
    {
        /// <summary>The bytes decoded before the decode stopped.</summary>
        internal List<byte> Bytes { get; } = [];

        /// <summary>How many code units of the text the decoded bytes account for.</summary>
        internal int Read { get; set; }

        /// <summary>The <c>SyntaxError</c>'s message, or <see langword="null"/> when there was none.</summary>
        internal string? Error { get; set; }
    }

    /// <summary>The specification's <c>SkipAsciiWhitespace</c>.</summary>
    internal static int Base64SkipSpace(string text, int at)
    {
        while (at < text.Length && text[at] is '\t' or '\n' or '\f' or '\r' or ' ')
        {
            at++;
        }

        return at;
    }

    /// <summary>
    /// The specification's <c>FromBase64</c>: decodes at most <paramref name="maxLength"/> bytes,
    /// answering what it read and the error it stopped at.
    /// </summary>
    /// <remarks>
    /// A chunk of four characters is three bytes. A final chunk of two or three characters is one or
    /// two, padded or not as <paramref name="lastChunk"/> allows; under <c>strict</c> its unused bits
    /// must be zero and it must be padded. A chunk that would not fit in what is left of
    /// <paramref name="maxLength"/> is not started, and <c>read</c> stops before it.
    /// </remarks>
    internal static Base64Result Base64Decode(JsEngine engine, string text, bool urlSafe, string lastChunk, long maxLength)
    {
        var result = new Base64Result();

        if (maxLength == 0)
        {
            return result;
        }

        engine.Charge((ulong)text.Length + 1);
        var chunk = new char[4];
        var chunkLength = 0;
        var at = 0;

        while (true)
        {
            at = Base64SkipSpace(text, at);

            if (at == text.Length)
            {
                if (chunkLength > 0)
                {
                    if (lastChunk == "stop-before-partial")
                    {
                        return result;
                    }

                    if (lastChunk == "strict")
                    {
                        result.Error = "the base64 text ends in an unpadded chunk";
                        return result;
                    }

                    if (chunkLength == 1)
                    {
                        result.Error = "the base64 text ends in a chunk of one character";
                        return result;
                    }

                    Base64DecodeFinal(chunk, chunkLength, false, result.Bytes);
                }

                result.Read = text.Length;
                return result;
            }

            var character = text[at];
            at++;

            if (character == '=')
            {
                if (chunkLength < 2)
                {
                    result.Error = "padding in base64 text where a chunk has fewer than two characters";
                    return result;
                }

                at = Base64SkipSpace(text, at);

                if (chunkLength == 2)
                {
                    if (at == text.Length)
                    {
                        if (lastChunk != "stop-before-partial")
                        {
                            result.Error = "the base64 text ends in a chunk with one padding character of two";
                        }

                        return result;
                    }

                    if (text[at] == '=')
                    {
                        at = Base64SkipSpace(text, at + 1);
                    }
                }

                if (at < text.Length)
                {
                    result.Error = "base64 text continues after its padding";
                    return result;
                }

                if (!Base64DecodeFinal(chunk, chunkLength, lastChunk == "strict", result.Bytes))
                {
                    result.Error = "the final base64 chunk has bits set past its last byte";
                    return result;
                }

                result.Read = text.Length;
                return result;
            }

            if (urlSafe)
            {
                if (character is '+' or '/')
                {
                    result.Error = "'" + character + "' is not in the base64url alphabet";
                    return result;
                }

                character = character switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => character,
                };
            }

            if (Base64Alphabet.IndexOf(character, StringComparison.Ordinal) < 0)
            {
                result.Error = "'" + character + "' is not a base64 character";
                return result;
            }

            var remaining = maxLength - result.Bytes.Count;

            if ((remaining == 1 && chunkLength == 2) || (remaining == 2 && chunkLength == 3))
            {
                return result;
            }

            chunk[chunkLength++] = character;

            if (chunkLength == 4)
            {
                Base64DecodeFull(chunk, result.Bytes);
                chunkLength = 0;
                result.Read = at;

                if (result.Bytes.Count == maxLength)
                {
                    return result;
                }
            }
        }
    }

    /// <summary>The specification's <c>DecodeFullLengthBase64Chunk</c>: four characters, three bytes.</summary>
    internal static void Base64DecodeFull(char[] chunk, List<byte> bytes)
    {
        var bits = 0;

        for (var at = 0; at < 4; at++)
        {
            bits = (bits << 6) | Base64Alphabet.IndexOf(chunk[at], StringComparison.Ordinal);
        }

        bytes.Add((byte)(bits >> 16));
        bytes.Add((byte)(bits >> 8));
        bytes.Add((byte)bits);
    }

    /// <summary>
    /// The specification's <c>DecodeFinalBase64Chunk</c>: two characters are one byte and three are
    /// two, and under <paramref name="strict"/> the bits past the last byte must be zero.
    /// </summary>
    internal static bool Base64DecodeFinal(char[] chunk, int length, bool strict, List<byte> bytes)
    {
        var padded = new[] { chunk[0], chunk[1], length == 3 ? chunk[2] : 'A', 'A' };
        var decoded = new List<byte>(3);
        Base64DecodeFull(padded, decoded);

        if (strict && decoded[length - 1] != 0)
        {
            return false;
        }

        bytes.Add(decoded[0]);

        if (length == 3)
        {
            bytes.Add(decoded[1]);
        }

        return true;
    }

    /// <summary>The value of one hex digit of either case, or -1 for any other character.</summary>
    internal static int Base64HexDigit(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };

    /// <summary>
    /// The specification's <c>FromHex</c>: pairs of hex digits, at most <paramref name="maxLength"/>
    /// bytes, and an error for an odd length or a character that is not one.
    /// </summary>
    internal static Base64Result Base64DecodeHex(JsEngine engine, string text, long maxLength)
    {
        var result = new Base64Result();

        if (text.Length % 2 != 0)
        {
            result.Error = "hex text has an odd number of characters";
            return result;
        }

        engine.Charge((ulong)text.Length + 1);

        while (result.Read < text.Length && result.Bytes.Count < maxLength)
        {
            var high = Base64HexDigit(text[result.Read]);
            var low = Base64HexDigit(text[result.Read + 1]);

            if (high < 0 || low < 0)
            {
                result.Error = "'" + text.Substring(result.Read, 2) + "' is not a pair of hex digits";
                return result;
            }

            result.Read += 2;
            result.Bytes.Add((byte)((high << 4) | low));
        }

        return result;
    }

    /// <summary>The ToInteger'd relative bound's arithmetic of ArrayRelative.</summary>
    internal static double ArrayRelative(double relative, double length)
    {
        if (relative < 0)
        {
            relative += length;
            return relative < 0 ? 0 : relative;
        }

        return relative > length ? length : relative;
    }

    /// <summary>The codePointAt built-in after its conversions: NaN stands for undefined.</summary>
    internal static double CodePointAt(JsEngine engine, string text, double position)
    {
        StringCharge(engine, 1);

        if (position < 0 || position >= text.Length)
        {
            return double.NaN;
        }

        var at = (int)position;
        var first = text[at];

        // A CODE POINT IS NOT A CODE UNIT. `codePointAt` is the method that exists to say so:
        // a high surrogate followed by a low one is one astral character, and reporting the
        // surrogate is what `charCodeAt` is for.
        return char.IsHighSurrogate(first) &&
            at + 1 < text.Length &&
            char.IsLowSurrogate(text[at + 1])
            ? char.ConvertToUtf32(first, text[at + 1])
            : first;
    }

    /// <summary>The isWellFormed built-in after its receiver's conversion.</summary>
    internal static bool IsWellFormed(JsEngine engine, string text)
    {
        StringCharge(engine, text.Length + 1);
        return StringWellFormed(text);
    }

    /// <summary>The toWellFormed built-in after its receiver's conversion.</summary>
    internal static string ToWellFormed(JsEngine engine, string text)
    {
        StringCharge(engine, text.Length + 1);

        if (StringWellFormed(text))
        {
            return text;
        }

        var built = new System.Text.StringBuilder(text.Length);

        for (var at = 0; at < text.Length; at++)
        {
            var unit = text[at];

            if (char.IsHighSurrogate(unit) &&
                at + 1 < text.Length &&
                char.IsLowSurrogate(text[at + 1]))
            {
                built.Append(unit).Append(text[at + 1]);
                at++;
                continue;
            }

            // THE REPLACEMENT IS ONE UNIT PER LONE SURROGATE, so the answer has the same length
            // as the receiver. That is the language's rule and it is worth stating, because a
            // reader who expects an encoder's behaviour would expect a lone surrogate to become
            // three bytes and the two lengths to differ.
            built.Append(char.IsSurrogate(unit) ? '\ufffd' : unit);
        }

        return built.ToString();
    }

    /// <summary>The toHex built-in after its receiver's bytes are read.</summary>
    internal static string ToHex(JsEngine engine, byte[] bytes)
    {
        engine.Charge((ulong)bytes.Length * 2 + 1);
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>The matcher's AdvanceStringIndex, which RegExpAdvanceIndex calls.</summary>
    internal static class JsRegExpMatcher
    {
        /// <summary>The specification's <c>AdvanceStringIndex</c>.</summary>
        public static int Advance(string input, int index, bool unicode)
        {
            if (unicode &&
                index + 1 < input.Length &&
                char.IsHighSurrogate(input[index]) &&
                char.IsLowSurrogate(input[index + 1]))
            {
                return index + 2;
            }

            return index + 1;
        }
    }
}
