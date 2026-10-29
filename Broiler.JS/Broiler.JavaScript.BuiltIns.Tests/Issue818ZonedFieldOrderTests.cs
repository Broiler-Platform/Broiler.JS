using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 7
// (test/built-ins/Temporal/ZonedDateTime/prototype/{since,until}/order-of-operations.js):
//   Test262Error: ... should have the same contents. order of operations.
//
// Converting a property-bag "other" to a ZonedDateTime read `timeZone` first and `offset`
// last, instead of reading every field in one alphabetical PrepareCalendarFields pass with
// `offset` between nanosecond and second and `timeZone` between second and year. The bag is
// now read in that order (the date/time fields reuse PlainDateTime's reader, with offset and
// timeZone injected at their positions).
public class Issue818ZonedFieldOrderTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // A bag whose every field getter records its name; the recorded order must be alphabetical:
    // calendar, day, hour, microsecond, millisecond, minute, month, monthCode, nanosecond,
    // offset, second, timeZone, year.
    private const string OrderedBag =
        "var log = [];" +
        "function track(name, value) { Object.defineProperty(bag, name, { get: function () { log.push(name); return value; } }); }" +
        "var bag = {};" +
        "['calendar','day','hour','microsecond','millisecond','minute','month','monthCode'," +
        " 'nanosecond','offset','second','timeZone','year'].forEach(function (n) {});" +
        "track('year', 2024); track('month', 6); track('monthCode', 'M06'); track('day', 15);" +
        "track('hour', 12); track('minute', 30); track('second', 0); track('millisecond', 0);" +
        "track('microsecond', 0); track('nanosecond', 0);" +
        "track('offset', '+00:00'); track('timeZone', 'UTC'); track('calendar', 'iso8601');";

    [Fact]
    public void SinceReadsOtherBagFieldsAlphabetically()
        => Assert.Equal(
            "calendar,day,hour,microsecond,millisecond,minute,month,monthCode,nanosecond,offset,second,timeZone,year",
            Eval(OrderedBag +
                 "var zdt = new Temporal.ZonedDateTime(0n, 'UTC');" +
                 "zdt.since(bag);" +
                 "log.join(',')"));

    [Fact]
    public void FromReadsBagFieldsAlphabetically()
        => Assert.Equal(
            "calendar,day,hour,microsecond,millisecond,minute,month,monthCode,nanosecond,offset,second,timeZone,year",
            Eval(OrderedBag +
                 "Temporal.ZonedDateTime.from(bag);" +
                 "log.join(',')"));

    // timeZone is no longer read first, and offset no longer last.
    [Fact]
    public void TimeZoneIsNotReadFirst()
        => Assert.Equal("false", Eval(
            OrderedBag +
            "var zdt = new Temporal.ZonedDateTime(0n, 'UTC');" +
            "zdt.until(bag);" +
            "String(log[0] === 'timeZone')"));

    // The conversion still produces the right value and still validates.
    [Fact]
    public void BagStillConstructsCorrectly()
        => Assert.Equal("2024-06-15T12:30:00+00:00[UTC]", Eval(
            "Temporal.ZonedDateTime.from({ year: 2024, month: 6, day: 15, hour: 12, minute: 30, timeZone: 'UTC' }).toString()"));

    [Fact]
    public void MissingTimeZoneStillThrows()
        => Assert.Equal("TypeError", Eval(
            "try { Temporal.ZonedDateTime.from({ year: 2024, month: 6, day: 15, timeZone: undefined }); 'no-throw'; }" +
            "catch (e) { e.constructor.name; }"));

    [Fact]
    public void OffsetMismatchStillRejects()
        => Assert.Equal("RangeError", Eval(
            "try { Temporal.ZonedDateTime.from({ year: 2024, month: 6, day: 1, hour: 12, offset: '+05:00', timeZone: 'UTC' });" +
            " 'no-throw'; } catch (e) { e.constructor.name; }"));
}
