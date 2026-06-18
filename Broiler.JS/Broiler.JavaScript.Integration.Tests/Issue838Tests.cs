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
//   Problem 35 (Array.prototype[@@unscopables] should not include "with") — the change-
//   array-by-copy proposal added toReversed/toSorted/toSpliced to the @@unscopables list but
//   deliberately NOT "with" ("with" is a reserved word that can never name a binding shadowed
//   inside a `with` statement). The engine's list carried an extra "with" entry, so the
//   property set did not match the spec (§23.1.3.40). Removed it; the Array.prototype.with
//   method itself is unaffected.
//
//   Problem 97 (Date.prototype.toLocale{String,DateString,TimeString} throw the same
//   exceptions as Intl.DateTimeFormat) — these methods are specified to construct an
//   Intl.DateTimeFormat, so an invalid locales argument must surface the constructor's
//   error: null → TypeError (ToObject(null)), a malformed language tag → RangeError. The
//   Broiler .NET fast path (taken when no options object is supplied) skipped that step and
//   treated a null locale like undefined, so `new Date(0).toLocaleString(null)` returned a
//   string instead of throwing. The fast path now validates the locales argument through the
//   same CanonicalizeLocaleList the Intl constructor uses, while preserving the spec's step
//   order (a NaN date still returns "Invalid Date" before any locale validation).
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

    // ---- Problem 97: Date.prototype.toLocale* throws the same exceptions as DateTimeFormat ----

    [Fact]
    public void ToLocaleStringNullLocaleThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try { new Date(0).toLocaleString(null); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ToLocaleDateStringNullLocaleThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try { new Date(0).toLocaleDateString(null); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ToLocaleTimeStringNullLocaleThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try { new Date(0).toLocaleTimeString(null); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ToLocaleStringNullLocaleMatchesDateTimeFormatConstructor()
        => Assert.Equal("true", Eval(
            "function err(fn){ try { fn(); return 'no-throw'; } catch (e) { return e.constructor.name; } }" +
            "String(err(function(){ new Date(0).toLocaleString(null); }) ===" +
            "       err(function(){ new Intl.DateTimeFormat(null); }))"));

    [Fact]
    public void ToLocaleStringMalformedTagThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try { new Date(0).toLocaleString('i'); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void InvalidDateReturnsInvalidDateBeforeLocaleValidation()
        => Assert.Equal("Invalid Date", Eval("new Date(NaN).toLocaleString(null)"));

    [Fact]
    public void ToLocaleStringStillWorksWithNoArgsAndValidLocale()
        => Assert.Equal("string,string", Eval(
            "(typeof new Date(0).toLocaleString()) + ',' + (typeof new Date(0).toLocaleString('en-US'))"));

    [Fact]
    public void ToLocaleStringStillFormatsThroughIntlOptions()
        => Assert.Equal("1970", Eval(
            "new Date(0).toLocaleString('en-US', { year: 'numeric', timeZone: 'UTC' })"));

    // ---- Problem 35: Array.prototype[@@unscopables] matches the spec list (no "with") ----

    [Fact]
    public void ArrayUnscopablesMatchesSpecListWithoutWith()
        => Assert.Equal(
            "at,copyWithin,entries,fill,find,findIndex,findLast,findLastIndex," +
            "flat,flatMap,includes,keys,toReversed,toSorted,toSpliced,values",
            Eval("Object.keys(Array.prototype[Symbol.unscopables]).join(',')"));

    [Fact]
    public void ArrayUnscopablesDoesNotIncludeWith()
        => Assert.Equal("false", Eval("String('with' in Array.prototype[Symbol.unscopables])"));

    [Fact]
    public void ArrayUnscopablesHasNullPrototype()
        => Assert.Equal("true", Eval(
            "String(Object.getPrototypeOf(Array.prototype[Symbol.unscopables]) === null)"));

    [Fact]
    public void ArrayWithMethodItselfStillExistsAndWorks()
        => Assert.Equal("function,1,9,3", Eval(
            "(typeof Array.prototype.with) + ',' + [1, 2, 3].with(1, 9).join(',')"));
}
