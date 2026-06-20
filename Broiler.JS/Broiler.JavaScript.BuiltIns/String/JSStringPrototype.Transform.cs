using System.Globalization;
using System.Text;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

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

    // Implements the abstract operation StringPad (ECMA-262). The filler is the
    // full fill string repeated and truncated to the required width, not just a
    // single character. Argument coercion order matters: maxLength is converted
    // before fillString, and fillString is only coerced when padding is needed.
    private static JSString StringPad(string str, JSValue maxLengthArg, JSValue fillArg, bool padStart)
    {
        var maxLengthValue = maxLengthArg.DoubleValue;
        var maxLength = double.IsNaN(maxLengthValue) ? 0 : System.Math.Truncate(maxLengthValue);

        if (maxLength <= str.Length)
            return new JSString(str);

        var fillString = fillArg.IsUndefined ? " " : fillArg.StringValue;
        if (fillString.Length == 0)
            return new JSString(str);

        var fillLength = (int)maxLength - str.Length;
        var filler = new StringBuilder(fillLength);
        while (filler.Length < fillLength)
            filler.Append(fillString);
        filler.Length = fillLength;

        return padStart
            ? new JSString(filler.Append(str).ToString())
            : new JSString(str + filler);
    }

    [JSPrototypeMethod]
    [JSExport("repeat", Length = 1)]
    internal static JSValue Repeat(in Arguments a)
    {
        var @this = a.This.AsString();

        // Step 3: n = ToIntegerOrInfinity(count). ToNumber maps undefined/NaN to 0.
        var n = a.Get1().DoubleValue;
        n = double.IsNaN(n) ? 0 : System.Math.Truncate(n);

        // Step 4: a negative or infinite count is a RangeError.
        if (n < 0 || double.IsPositiveInfinity(n))
            throw JSEngine.NewRangeError($"Invalid count value");

        // An empty string repeated any finite number of times is empty, and a
        // zero count always yields the empty string. Handling this first avoids
        // allocating for huge (but legal) counts such as 2^31 - 1.
        if (@this.Length == 0 || n == 0)
            return new JSString(string.Empty);

        if (n * @this.Length > int.MaxValue)
            throw JSEngine.NewRangeError($"Invalid count value");

        var c = (int)n;
        var result = new StringBuilder(c * @this.Length);
        for (var i = 0; i < c; i++)
            result.Append(@this);

        return new JSString(result.ToString());

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
        return IsWellFormedUtf16(@this) ? JSBoolean.True : JSBoolean.False;
    }

    [JSPrototypeMethod]
    [JSExport("toWellFormed")]
    internal static JSValue ToWellFormed(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(ToWellFormedUtf16(@this));
    }

    private static bool IsWellFormedUtf16(string value)
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

    private static string ToWellFormedUtf16(string value)
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

    private static readonly char[] trimCharacters = [
        // Whitespace
        '\x09', '\x0B', '\x0C', '\x20', '\xA0', '\xFEFF',

        // Unicode space separator
        '\u1680', '\u180E', '\u2000', '\u2001',
        '\u2002', '\u2003', '\u2004', '\u2005',
        '\u2006', '\u2007', '\u2008', '\u2009',
        '\u200A', '\u202F', '\u205F', '\u3000', 

        // Line terminators
        '\x0A', '\x0D', '\u2028', '\u2029',
    ];

    [JSPrototypeMethod]
    [JSExport("trim")]
    internal static JSValue Trim(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(@this.Trim(trimCharacters));
    }

    [JSPrototypeMethod]
    [JSExport("trimEnd")]
    internal static JSValue TrimEnd(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(@this.TrimEnd(trimCharacters));
    }

    [JSPrototypeMethod]
    [JSExport("trimStart")]
    internal static JSValue TrimStart(in Arguments a)
    {
        var @this = a.This.AsString();
        return new JSString(@this.TrimStart(trimCharacters));
    }
}
