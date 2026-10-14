using System;
using System.Text;
using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    // ToIntegerOrInfinity (§7.1.5) of an argument, which is the shared library's (SJL-4.5); an absent
    // or undefined argument is +0, as its ToNumber is NaN.
    private static double IntegerArgument(JSValue? value)
        => value == null ? 0 : IntegerConversions.ToIntegerOrInfinity(value.DoubleValue);

    // A position clamped into [0, length], as substring and the search methods clamp it.
    private static int Clamp(double position, int length)
        => position <= 0 ? 0 : position >= length ? length : (int)position;

    // A relative index, as slice and substr resolve one: from the end when negative, then clamped.
    private static int Relative(double position, int length)
        => position < 0 ? Clamp(length + position, length) : Clamp(position, length);

    [JSPrototypeMethod]
    [JSExport("charAt", Length = 1)]
    public static JSValue CharAt(in Arguments a)
    {
        var text = a.This.AsString();
        var pos = IntegerArgument(a[0]);

        if (pos < 0 || pos >= text.Length)
            return Empty;

        return new JSString(new string(text[(int)pos], 1));
    }

    [JSPrototypeMethod]
    [JSExport("substring", Length = 2)]
    public static JSValue Substring(in Arguments a)
    {
        var @this = a.This.AsString();
        var start = Clamp(IntegerArgument(a[0]), @this.Length);
        var end = a.TryGetAt(1, out var v) && !v.IsUndefined ? Clamp(IntegerArgument(v), @this.Length) : @this.Length;

        var si = Math.Min(start, end);
        var ei = Math.Max(start, end);

        if (ei <= si)
            return Empty;

        return new JSString(@this.Substring(si, ei - si));
    }

    [JSPrototypeMethod]
    [JSExport("substr", Length = 2)]
    public static JSValue Substr(in Arguments a)
    {
        var @this = a.This.AsString();
        // B.2.2.1: a negative start counts from the end, and the length is clamped into [0, size].
        var start = Relative(IntegerArgument(a[0]), @this.Length);
        var length = a.TryGetAt(1, out var v) && !v.IsUndefined ? Clamp(IntegerArgument(v), @this.Length) : @this.Length;

        var count = Math.Min(length, @this.Length - start);
        
        if (count <= 0)
            return Empty;
        
        return new JSString(@this.Substring(start, count));
    }

    [JSPrototypeMethod]
    [JSExport("toString")]
    public static JSValue ToString(in Arguments a) => a.This.AsJSString();

    // The String iterator yields Unicode code points (not UTF-16 code units).
    [Symbol("@@iterator")]
    public static JSValue Iterator(in Arguments a) => new JSGenerator(new JSString(a.This.AsString()).GetCodePointEnumerator(), "String Iterator");

    [JSPrototypeMethod]
    [JSExport("charCodeAt", Length = 1)]
    internal static JSValue CharCodeAt(in Arguments a)
    {
        var text = a.This.AsString();
        var pos = IntegerArgument(a[0]);

        if (pos < 0 || pos >= text.Length)
            return NumberNaN;

        return CreateNumber(text[(int)pos]);
    }

    [JSPrototypeMethod]
    [JSExport("codePointAt", Length = 1)]
    internal static JSValue CodePointAt(in Arguments a)
    {
        var text = a.This.AsString();
        var position = IntegerArgument(a[0]);

        if (position < 0 || position >= text.Length)
            return JSUndefined.Value;

        var pos = (int)position;
        int firstCodePoint = text[pos];
        if (firstCodePoint < 0xD800 || firstCodePoint > 0xDBFF || pos + 1 == text.Length)
            return CreateNumber(firstCodePoint);

        int secondCodePoint = text[pos + 1];
        if (secondCodePoint < 0xDC00 || secondCodePoint > 0xDFFF)
            return CreateNumber(firstCodePoint);

        var output = (double)((firstCodePoint - 0xD800) * 1024 + (secondCodePoint - 0xDC00) + 0x10000);
        return CreateNumber(output);

    }

    [JSPrototypeMethod]
    [JSExport("concat", Length = 1)]
    internal static JSValue Concat(in Arguments a)
    {
        var @this = a.This.AsString();
        if (a.Length == 0)
            return a.This;

        StringBuilder sb = new();
        sb.Append(@this);

        // Each argument is coerced with the JS ToString abstract operation
        // (.StringValue → ToPrimitive(string) for objects), NOT the CLR ToString():
        // a Symbol wrapper must stringify through its toString ("Symbol(desc)"), and a
        // raw Symbol must throw — appending the JSValue directly used its CLR ToString
        // (the bare description).
        for (int i = 0; i < a.Length; i++)
            sb.Append(a.GetAt(i).StringValue);

        return new JSString(sb.ToString());
    }

    [JSPrototypeMethod]
    [JSExport("slice", Length = 2)]
    internal static JSValue Slice(in Arguments a)
    {
        var @this = a.This.AsString();

        //0th argument, start
        var start = Relative(IntegerArgument(a.Get1()), @this.Length);
        //1st argument, end — an absent OR explicitly `undefined` end means "to the
        // end of the string" (spec: if end is undefined, intEnd = len), not ToInteger
        // (which would coerce undefined to 0 and yield an empty slice).
        var endArg = a[1];
        var end = (endArg == null || endArg.IsUndefined) ? @this.Length : Relative(IntegerArgument(endArg), @this.Length);

        if (end <= start)
            return Empty;

        var result = @this.Substring(start, end - start);
        return new JSString(result);
    }

    [JSPrototypeMethod]
    [JSExport("valueOf")]
    internal static JSValue ValueOf(in Arguments a) => a.This.AsJSString();
}
