using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.Runtime;
using System;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Extensions;

public static class JSStringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Left(this string value, int max) => value.Length > max ? value.Substring(0, max) : value;

    // Both trim StrWhiteSpaceChar, the shared library's set; they trimmed .NET's, which holds
    // U+0085 and not U+FEFF. Nothing in the engine calls them since SJL-3.5.
    [Obsolete("Use Broiler.ECMAScript.Numbers.NumberParsing.TrimStrWhiteSpace, which this forwards to. To be removed in the next major version.")]
    public static string JSTrim(this string text) => NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Both).ToString();

    // Coerce with the JS ToString abstract op (StringValue: ToPrimitive(string) then
    // toString/valueOf), not CLR ToString — so parseInt/parseFloat of an object honour
    // its valueOf/toString rather than yielding "[object Object]".
    [Obsolete("Use Broiler.ECMAScript.Numbers.NumberParsing.TrimStrWhiteSpace, which this forwards to. To be removed in the next major version.")]
    public static string JSTrim(this JSValue text) => NumberParsing.TrimStrWhiteSpace(text.StringValue, TrimEnds.Both).ToString();
}
