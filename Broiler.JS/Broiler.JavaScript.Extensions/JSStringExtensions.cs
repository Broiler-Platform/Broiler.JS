using Broiler.JavaScript.Runtime;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Extensions;

public static class JSStringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Left(this string value, int max) => value.Length > max ? value.Substring(0, max) : value;

    public static string JSTrim(this string text) => text.Trim();

    // Coerce with the JS ToString abstract op (StringValue: ToPrimitive(string) then
    // toString/valueOf), not CLR ToString — so parseInt/parseFloat of an object honour
    // its valueOf/toString rather than yielding "[object Object]".
    public static string JSTrim(this JSValue text) => text.StringValue.Trim();
}
