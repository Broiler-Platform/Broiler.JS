using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/838
//
// Fixed here — the Date string-representation cluster:
//
//   Problems 96, 98, 99 (Date.prototype.toString / toUTCString / toDateString serialize
//   year -1 to "-0001") — the human-readable Date formatters rendered through the backing
//   System.DateTimeOffset (`value.ToString(...)`), which only spans years 1–9999. Dates
//   outside that window are stored with the MinValue sentinel in `value` and the real
//   ECMAScript time in `rawTimeMs`; the formatters' `value == InvalidDate` guard therefore
//   misreported every such valid date as "Invalid Date", and even in range they could not
//   print proleptic years. toString, toUTCString, toDateString and toTimeString now compute
//   their fields from the ECMAScript time value via JSDateMath (the same path toISOString
//   already used) and guard on `double.IsNaN(GetTimeMs())`, so the full Date range renders
//   with a spec-compliant year ("-0001", four-digit minimum with a leading sign when
//   negative).
//
//   Problem 95 (Date.parse(new Date(0).toString()) === 0) — Date.prototype.toString emits
//   the implementation-defined zone shape "… GMT+0000 (Coordinated Universal Time)", but the
//   parser's offset specifier needs a colon ("+00:00") and could not skip the trailing
//   parenthesised zone name, so the engine's own toString output did not round-trip through
//   Date.parse. The parser now normalises that shape (inserts the colon, drops the zone-name
//   parenthetical) before matching, so toString/toTimeString output round-trips while the ISO
//   forms, the UTC string, and the negative-zero extended-year rejection are unaffected.
//
// Out of scope: the remaining problems in the issue are unrelated engine areas
// (Temporal/Intl/CLDR ordering and data, RegExp/Unicode property classes, with/Proxy
// environment records, SpiderMonkey-shell harness globals, etc.).
public class Issue838Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problems 96/98/99: negative-year human-readable Date strings ----

    [Fact]
    public void ToStringSerializesNegativeYearWithSignedFourDigitYear()
        => Assert.Equal("Fri Jan 01 -0001 00:00:00 GMT+0000 (Coordinated Universal Time)",
            Eval("new Date(Date.UTC(-1, 0, 1)).toString()"));

    [Fact]
    public void ToUTCStringSerializesNegativeYear()
        => Assert.Equal("Fri, 01 Jan -0001 00:00:00 GMT",
            Eval("new Date(Date.UTC(-1, 0, 1)).toUTCString()"));

    [Fact]
    public void ToDateStringSerializesNegativeYear()
        => Assert.Equal("Fri Jan 01 -0001", Eval("new Date(Date.UTC(-1, 0, 1)).toDateString()"));

    [Fact]
    public void NegativeYearDateIsNotReportedInvalid()
        => Assert.Equal("false", Eval(
            "var s = new Date(Date.UTC(-1, 0, 1)).toString(); String(s === 'Invalid Date')"));

    // ---- in-range dates and the genuine NaN case are unchanged ----

    [Fact]
    public void EpochToStringStillRendersInUtcContainer()
        => Assert.Equal("Thu Jan 01 1970 00:00:00 GMT+0000 (Coordinated Universal Time)",
            Eval("new Date(0).toString()"));

    [Fact]
    public void EpochToUTCStringUnchanged()
        => Assert.Equal("Thu, 01 Jan 1970 00:00:00 GMT", Eval("new Date(0).toUTCString()"));

    [Fact]
    public void EpochToDateStringUnchanged()
        => Assert.Equal("Thu Jan 01 1970", Eval("new Date(0).toDateString()"));

    [Fact]
    public void InvalidDateStillSerializesToInvalidDate()
        => Assert.Equal("Invalid Date,Invalid Date,Invalid Date", Eval(
            "var d = new Date(NaN);" +
            "d.toString() + ',' + d.toUTCString() + ',' + d.toDateString()"));

    // ---- Problem 95: toString output round-trips through Date.parse ----

    [Fact]
    public void DateParseRoundTripsToStringAtEpoch()
        => Assert.Equal("0", Eval("String(Date.parse(new Date(0).toString()))"));

    [Fact]
    public void DateParseRoundTripsToStringAtArbitraryInstant()
        => Assert.Equal("true", Eval(
            "var d = new Date(1687000000000); String(Date.parse(d.toString()) === d.getTime())"));

    [Fact]
    public void DateParseRoundTripsToStringForPreEpochInstant()
        => Assert.Equal("true", Eval(
            "var d = new Date(-5000000000); String(Date.parse(d.toString()) === d.getTime())"));

    [Fact]
    public void DateParseHonoursExplicitNonZeroOffsetInToStringShape()
        => Assert.Equal("28800000", Eval(
            "String(Date.parse('Thu Jan 01 1970 00:00:00 GMT-0800 (Pacific Standard Time)'))"));

    // ---- guard: the normalisation does not regress the existing ISO / UTC / NaN paths ----

    [Fact]
    public void IsoStringStillParses()
        => Assert.Equal("1686825000000", Eval("String(Date.parse('2023-06-15T10:30:00Z'))"));

    [Fact]
    public void UtcStringStillParses()
        => Assert.Equal("0", Eval("String(Date.parse('Thu, 01 Jan 1970 00:00:00 GMT'))"));

    [Fact]
    public void NegativeZeroExtendedYearStillRejected()
        => Assert.Equal("true", Eval("String(isNaN(Date.parse('-000000-03-31T00:45Z')))"));
}
