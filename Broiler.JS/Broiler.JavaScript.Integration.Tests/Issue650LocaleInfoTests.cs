using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650 — Problem 10.
//
// Intl.Locale.prototype.get{Calendars,Collations,HourCycles,NumberingSystems,
// TimeZones} returned empty arrays, so test262's "array has at least one element"
// checks failed. They now return sensible, spec-shaped defaults (an Array, with
// the -u- preferred value first when present; getTimeZones returns undefined for
// a region-less locale).
public class Issue650LocaleInfoTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void GetCalendarsReturnsNonEmptyArray()
        => Assert.Equal("true 1 gregory", Eval(
            "var c=new Intl.Locale('en').getCalendars(); '' + Array.isArray(c) + ' ' + c.length + ' ' + c[0]"));

    [Fact]
    public void GetCollationsExcludesStandardAndSearch()
        => Assert.Equal("true true", Eval(
            "var c=new Intl.Locale('en').getCollations(); '' + (c.length>0) + ' ' + (c.indexOf('standard')<0 && c.indexOf('search')<0)"));

    [Fact]
    public void GetHourCyclesReturnsValidValues()
        => Assert.Equal("true true", Eval(
            "var h=new Intl.Locale('en').getHourCycles(); '' + (h.length>0) + ' ' + h.every(function(x){return ['h11','h12','h23','h24'].indexOf(x)>=0;})"));

    [Fact]
    public void GetNumberingSystemsReturnsNonEmptyArray()
        => Assert.Equal("true 1 latn", Eval(
            "var n=new Intl.Locale('en').getNumberingSystems(); '' + Array.isArray(n) + ' ' + n.length + ' ' + n[0]"));

    [Fact]
    public void GetTimeZonesReturnsNonEmptyArrayForRegion()
        => Assert.Equal("true true", Eval(
            "var t=new Intl.Locale('en-US').getTimeZones(); '' + Array.isArray(t) + ' ' + (t.length>0)"));

    [Fact]
    public void GetTimeZonesIsUndefinedWithoutRegion()
        => Assert.Equal("undefined", Eval("'' + new Intl.Locale('en').getTimeZones()"));

    // The -u- preferred value is placed first (CreateArrayFromListAndPreferred).
    [Fact]
    public void PreferredCalendarComesFirst()
        => Assert.Equal("buddhist gregory", Eval(
            "new Intl.Locale('en-u-ca-buddhist').getCalendars().join(' ')"));

    [Fact]
    public void PreferredNumberingSystemComesFirst()
        => Assert.Equal("arab latn", Eval(
            "new Intl.Locale('en-u-nu-arab').getNumberingSystems().join(' ')"));
}
