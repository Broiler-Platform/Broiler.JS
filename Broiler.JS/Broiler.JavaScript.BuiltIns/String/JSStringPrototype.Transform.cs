using System.Globalization;
using System.Text;
using Broiler.ECMAScript;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Text;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.Unicode.Properties;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    [JSPrototypeMethod]
    [JSExport("normalize", Length = 0)]
    internal static JSValue Normalize(in Arguments a)
    {
        var @this = a.This.AsString();
        var input = a.Get1();

        string form = input.IsUndefined ? "NFC" : input.StringValue;

        return form switch
        {
            "NFC" => new JSString(@this.Normalize(NormalizationForm.FormC)),
            "NFD" => new JSString(@this.Normalize(NormalizationForm.FormD)),
            "NFKC" => new JSString(@this.Normalize(NormalizationForm.FormKC)),
            "NFKD" => new JSString(@this.Normalize(NormalizationForm.FormKD)),
            _ => throw JSEngine.NewRangeError($"The normalization form should be one of NFC, NFD, NFKC, NFKD."),
        };
    }

    [JSPrototypeMethod]
    [JSExport("padEnd", Length = 1)]
    internal static JSValue PadEnd(in Arguments a)
    {
        var @this = a.This.AsString();
        var (s, c) = a.Get2();
        return StringPad(@this, s, c, padStart: false);
    }

    [JSPrototypeMethod]
    [JSExport("padStart", Length = 1)]
    internal static JSValue PadStart(in Arguments a)
    {
        var @this = a.This.AsString();
        var (s, c) = a.Get2();
        return StringPad(@this, s, c, padStart: true);
    }

    // This engine's length policy (SJL-5.5): the longest String a built-in builds, which is the longest
    // a .NET string can be. A pad or a repeat past it is a RangeError, not the runtime's exception.
    internal const int MaximumLength = 0x3FFFFFDF;

    // StringPaddingBuiltinsImpl (ECMA-262 22.1.3.17.1): maxLength is converted before fillString, and
    // fillString only when padding is needed. StringPad itself is the shared library's (SJL-5.5),
    // under this engine's length policy: a result longer than MaximumLength is a RangeError, where the
    // builder used to let .NET's OutOfMemoryException through past the runtime's largest string (P4).
    private static JSString StringPad(string str, JSValue maxLengthArg, JSValue fillArg, bool padStart)
    {
        // intMaxLength = ToLength(maxLength), the shared library's (SJL-4.5).
        var maxLength = IntegerConversions.ToLength(maxLengthArg.DoubleValue);

        if (maxLength <= str.Length)
            return new JSString(str);

        var fillString = fillArg.IsUndefined ? " " : fillArg.StringValue;
        var status = StringPadding.Pad(
            str,
            (long)maxLength,
            fillString,
            padStart ? PadPlacement.Start : PadPlacement.End,
            new LengthLimit(MaximumLength),
            out var padded);

        return status == ResultStatus.Completed
            ? new JSString(padded)
            : throw JSEngine.NewRangeError("Invalid string length");
    }

    [JSPrototypeMethod]
    [JSExport("repeat", Length = 1)]
    internal static JSValue Repeat(in Arguments a)
    {
        var @this = a.This.AsString();

        // Step 3: n = ToIntegerOrInfinity(count), the shared library's (SJL-4.5). ToNumber maps
        // undefined/NaN to 0.
        var n = IntegerConversions.ToIntegerOrInfinity(a.Get1().DoubleValue);

        // Step 4: a negative or infinite count is a RangeError.
        if (n < 0 || double.IsPositiveInfinity(n))
            throw JSEngine.NewRangeError($"Invalid count value");

        // An empty string repeated any finite number of times is empty, and a
        // zero count always yields the empty string. Handling this first avoids
        // allocating for huge (but legal) counts such as 2^31 - 1.
        if (@this.Length == 0 || n == 0)
            return new JSString(string.Empty);

        // A result longer than this engine's strings hold is a RangeError: past MaximumLength the
        // runtime cannot allocate it, and its OutOfMemoryException used to reach a script (P4).
        if (n * @this.Length > MaximumLength)
            throw JSEngine.NewRangeError($"Invalid count value");

        var c = (int)n;

        // Write the repetitions straight into the result. Accumulating them in a
        // StringBuilder and materialising that afterwards keeps two full-size buffers
        // alive at once, so the peak cost was twice the string actually produced — and
        // for a single-character unit it also appended one character at a time. A legal
        // but enormous count makes that the difference between a gigabyte and two: WPT's
        // editing/crashtests/insertparagraph-in-listitem-in-svg-followed-by-collapsible-spaces
        // evaluates `" ".repeat(336860180)`, a 642 MiB result.
        if (@this.Length == 1)
            return new JSString(new string(@this[0], c));

        return new JSString(string.Create(c * @this.Length, (@this, c), static (destination, state) =>
        {
            var (unit, count) = state;
            for (var i = 0; i < count; i++)
                unit.CopyTo(destination[(i * unit.Length)..]);
        }));
    }

    [JSPrototypeMethod]
    [JSExport("toLocaleLowerCase", Length = 0)]
    internal static JSValue ToLocaleLowerCase(in Arguments a)
    {
        var @this = a.This.AsString();
        var locale = a.Get1();

        try
        {
            CultureInfo culture = locale.IsNullOrUndefined ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(locale.ToString());
            return new JSString(JSStringSpecialCasing.ToLocaleLower(@this, culture));
        }
        catch (CultureNotFoundException)
        {
            throw JSEngine.NewRangeError($"Incorrect locale information provided");
        }
    }

    [JSPrototypeMethod]
    [JSExport("toLocaleUpperCase", Length = 0)]
    internal static JSValue ToLocaleUpperCase(in Arguments a)
    {
        var @this = a.This.AsString();
        var locale = a.Get1();

        try
        {
            CultureInfo culture = locale.IsNullOrUndefined ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(locale.ToString());
            return new JSString(JSStringSpecialCasing.ToLocaleUpper(@this, culture));
        }
        catch (CultureNotFoundException)
        {
            throw JSEngine.NewRangeError($"Incorrect locale information provided");
        }
    }

    [JSPrototypeMethod]
    [JSExport("toLowerCase")]
    internal static JSValue ToLowerCase(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(JSStringSpecialCasing.ToLower(@this));
    }

    [JSPrototypeMethod]
    [JSExport("toUpperCase")]
    internal static JSValue ToUpperCase(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(JSStringSpecialCasing.ToUpper(@this));
    }

    [JSPrototypeMethod]
    [JSExport("isWellFormed")]
    internal static JSValue IsWellFormed(in Arguments a)
    {
        var @this = a.This.AsString();
        return Utf16.IsWellFormed(@this) ? JSBoolean.True : JSBoolean.False;
    }

    // One U+FFFD per lone surrogate, so the result is as long as the receiver; the shared library's
    // toWellFormed (SJL-5.5), which answers a well-formed receiver itself.
    [JSPrototypeMethod]
    [JSExport("toWellFormed")]
    internal static JSValue ToWellFormed(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(Utf16.ToWellFormed(@this));
    }

    [JSPrototypeMethod]
    [JSExport("trim")]
    internal static JSValue Trim(in Arguments a) => new JSString(TrimString(a.This.AsString(), TrimEnds.Both));

    [JSPrototypeMethod]
    [JSExport("trimEnd")]
    internal static JSValue TrimEnd(in Arguments a) => new JSString(TrimString(a.This.AsString(), TrimEnds.End));

    [JSPrototypeMethod]
    [JSExport("trimStart")]
    internal static JSValue TrimStart(in Arguments a) => new JSString(TrimString(a.This.AsString(), TrimEnds.Start));

    // TrimString (ECMA-262 22.1.3.32.1) over the shared white-space set (SJL-3.5), which is
    // generated from the pinned Unicode data; a string with nothing to trim is returned as is.
    private static string TrimString(string text, TrimEnds ends)
    {
        var trimmed = NumberParsing.TrimStrWhiteSpace(text, ends);
        return trimmed.Length == text.Length ? text : trimmed.ToString();
    }
}
