using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here (Intl.DateTimeFormat timeZone option validation / normalization):
//
//   The constructor accepted any string as the timeZone option without validating it
//   against the IANA database, and resolvedOptions reported it (or the host zone) without
//   case-normalization. A named time zone is now validated and case-normalized — "utc" →
//   "UTC", "africa/abidjan" → "Africa/Abidjan" — while a backward alias keeps its own
//   name ("Asia/Calcutta" is NOT canonicalized to "Asia/Kolkata"); an unknown or legacy
//   non-IANA name (e.g. "ACT", "") is a RangeError.
public class Issue836DateTimeFormatTimeZoneTests
{
    private static string TimeZoneOf(string tz)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"new Intl.DateTimeFormat('en', {{ timeZone: '{tz}' }}).resolvedOptions().timeZone").ToString();
    }

    private static string ErrorName(string tz)
    {
        using var ctx = new JSContext();
        return ctx.Eval(
            "(function(){ try { new Intl.DateTimeFormat('en', { timeZone: " + tz + " }); return 'NONE'; }" +
            " catch (e) { return e.constructor.name; } })()").ToString();
    }

    [Fact]
    public void UtcLowercaseNormalizesToUpper() => Assert.Equal("UTC", TimeZoneOf("utc"));

    [Fact]
    public void NamedZoneCaseInsensitive() => Assert.Equal("Africa/Abidjan", TimeZoneOf("AFRICA/ABIDJAN"));

    [Fact]
    public void BackwardAliasNotCanonicalized() => Assert.Equal("Asia/Calcutta", TimeZoneOf("Asia/Calcutta"));

    [Fact]
    public void PrimaryZonePreserved() => Assert.Equal("Asia/Kolkata", TimeZoneOf("Asia/Kolkata"));

    [Fact]
    public void InvalidZoneThrowsRangeError() => Assert.Equal("RangeError", ErrorName("'Not/AZone'"));

    [Fact]
    public void EmptyZoneThrowsRangeError() => Assert.Equal("RangeError", ErrorName("''"));

    [Fact]
    public void LegacyNonIanaThrowsRangeError() => Assert.Equal("RangeError", ErrorName("'ACT'"));
}
