using System.Globalization;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The CLR bridge reads a script String into a .NET long, DateTime or DateTimeOffset, and writes a number into an object's
// detail text, with the invariant culture under every host culture (milestone SJL-9's clause 8, "Host globalization remains
// only behind the optional providers", on the owner's ruling of 2026-11-11, "Fix all + enforce"). Until that change
// JSString.BigIntValue read `long.TryParse(ToString(), out var n)`, so that under sv-SE, whose minus sign in .NET is
// U+2212, "−5" was -5; JSValueToClrConverter's four date conversions read `DateTime.Parse(value.ToString())`, so that
// under de-DE and tr-TR "11/12/2026" was 11 December; and JSNumber.ToDetailString wrote `value.ToString()`, so that under
// de-DE an object's detail text read "a: 1,5". Each case runs under the invariant culture, whose answers are the ones the
// engine gave there before the change and every host now gives, and under de-DE, sv-SE and tr-TR, applied as
// ClrBindingCultureTests applies tr-TR.
public class ClrBridgeHostCultureTests
{
    private static T UnderCulture<T>(string culture, Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    private static T InContext<T>(string culture, Func<JSContext, T> body) => UnderCulture(culture, () =>
    {
        using var ctx = new JSContext();
        return body(ctx);
    });

    public static TheoryData<string> Cultures => new() { "", "de-DE", "sv-SE", "tr-TR" };

    [Fact]
    public void The_host_cultures_would_read_these_texts_otherwise()
    {
        // The controls: without them the tests below would pass on a host with no data for these cultures.
        Assert.Equal(-5L, UnderCulture("sv-SE", () => long.Parse("\u22125")));
        Assert.Equal(12, UnderCulture("de-DE", () => DateTime.Parse("11/12/2026 13:45:00").Month));
        Assert.Equal("1,5", UnderCulture("de-DE", () => 1.5.ToString()));
    }

    // "BigInt from a string with a sign": the CLR bridge's long from a String reads the invariant culture's signs.
    [Theory]
    [MemberData(nameof(Cultures))]
    public void A_String_read_as_a_long_takes_the_invariant_culture_s_signs(string culture)
    {
        Assert.Equal(-5L, InContext(culture, ctx => JSValueToClrConverter.ToLong("-5".Marshal(), "n")));
        Assert.Equal(5L, InContext(culture, ctx => JSValueToClrConverter.ToLong("+5".Marshal(), "n")));
        Assert.Equal(7L, InContext(culture, ctx => JSValueToClrConverter.ToLong(" 7 ".Marshal(), "n")));
        // U+2212 is no sign of the invariant culture's, so the text is no integer and reads as 0, as with no LANG.
        Assert.Equal(0L, InContext(culture, ctx => JSValueToClrConverter.ToLong("\u22125".Marshal(), "n")));
        Assert.Equal(0L, InContext(culture, ctx => JSValueToClrConverter.ToNullableLong("\u22125".Marshal(), "n")));
        Assert.Equal(0L, InContext(culture, ctx => JSValueToClrConverter.ToLong(ctx.Eval("new String('\\u22125')"), "n")));
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void A_String_read_as_a_date_takes_the_invariant_culture_s_format(string culture)
    {
        var expected = new DateTime(2026, 11, 12, 13, 45, 0);
        const string text = "11/12/2026 13:45:00";
        Assert.Equal(expected, InContext(culture, ctx => JSValueToClrConverter.ToDateTime(text.Marshal(), "d")));
        Assert.Equal(expected, InContext(culture, ctx => JSValueToClrConverter.ToNullableDateTime(text.Marshal(), "d")));
        Assert.Equal(expected, InContext(culture, ctx => JSValueToClrConverter.ToDateTimeOffset(text.Marshal(), "d").DateTime));
        Assert.Equal(expected, InContext(culture, ctx => JSValueToClrConverter.ToNullableDateTimeOffset(text.Marshal(), "d")!.Value.DateTime));
        // An ISO 8601 text reads the same under every culture, before the change too.
        Assert.Equal(expected, InContext(culture, ctx => JSValueToClrConverter.ToDateTime("2026-11-12T13:45:00".Marshal(), "d")));
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void An_object_s_detail_text_writes_its_numbers_with_the_invariant_culture(string culture)
    {
        Assert.Equal("{a: 1.5, b: -2, c: x}", InContext(culture, ctx => ctx.Eval("({ a: 1.5, b: -2, c: 'x' })").ToDetailString()));
    }
}
