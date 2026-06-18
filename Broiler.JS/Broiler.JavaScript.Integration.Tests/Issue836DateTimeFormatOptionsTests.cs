using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here (Intl.DateTimeFormat constructor option reading):
//
//   Problems 50-53, 62 (constructor-options-order*) — the constructor read only
//   localeMatcher / calendar / numberingSystem / timeZone / timeZoneName up front
//   and deferred the date/time component options (hour12, hourCycle, weekday, era,
//   year, month, day, dayPeriod, hour, minute, second, fractionalSecondDigits,
//   formatMatcher) to format time. Per CreateDateTimeFormat every option must be read
//   exactly once, in a fixed order, at construction. The constructor now reads them
//   all into a coerced snapshot (firing each user getter once, in order); the
//   formatter and resolvedOptions read from that snapshot. This also makes a throwing
//   getter throw at construction and validates each option value once (RangeError).
public class Issue836DateTimeFormatOptionsTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // The full read order for the basic component set.
    [Fact]
    public void ReadsOptionsInSpecOrder()
        => Assert.Equal(
            "localeMatcher,hour12,hourCycle,timeZone,weekday,era,year,month,day,hour,minute,second,timeZoneName,formatMatcher",
            Eval(@"
                var actual = [];
                function track(name, value) {
                    return { get [name]() { actual.push(name); return value; } };
                }
                var options = {};
                [['day','numeric'],['era','long'],['formatMatcher','best fit'],['hour','numeric'],
                 ['hour12',true],['hourCycle','h24'],['localeMatcher','best fit'],['minute','numeric'],
                 ['month','numeric'],['second','numeric'],['timeZone','UTC'],['timeZoneName','long'],
                 ['weekday','long'],['year','numeric']].forEach(function(pair) {
                    Object.defineProperty(options, pair[0], {
                        enumerable: true,
                        get: function() { actual.push(pair[0]); return pair[1]; }
                    });
                });
                new Intl.DateTimeFormat('en', options);
                actual.join(',');
            "));

    // dayPeriod sits between day and hour.
    [Fact]
    public void DayPeriodReadBetweenDayAndHour()
        => Assert.Equal("day,dayPeriod,hour", Eval(@"
            var actual = [];
            var options = {};
            ['day','dayPeriod','hour'].forEach(function(name) {
                Object.defineProperty(options, name, {
                    enumerable: true,
                    get: function() { actual.push(name); return name === 'dayPeriod' ? 'short' : 'numeric'; }
                });
            });
            new Intl.DateTimeFormat('en', options);
            actual.join(',');
        "));

    // A throwing getter must propagate from the constructor.
    [Fact]
    public void ThrowingGetterThrowsAtConstruction()
        => Assert.Equal("caught", Eval(@"
            var options = { get hour() { throw new Error('boom'); } };
            var r;
            try { new Intl.DateTimeFormat('en', options); r = 'no throw'; }
            catch (e) { r = 'caught'; }
            r;
        "));

    // An out-of-set component value is a RangeError.
    [Fact]
    public void InvalidComponentValueThrowsRangeError()
        => Assert.Equal("RangeError", Eval(@"
            var r;
            try { new Intl.DateTimeFormat('en', { weekday: 'bogus' }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r;
        "));

    // resolvedOptions still reports the requested components (snapshot round-trip).
    [Fact]
    public void ResolvedOptionsReportsComponents()
        => Assert.Equal("long", Eval(
            "new Intl.DateTimeFormat('en', { weekday: 'long' }).resolvedOptions().weekday"));

    // A bare constructor (no options) still works.
    [Fact]
    public void NoOptionsConstructsAndFormats()
        => Assert.Equal("string", Eval(
            "typeof new Intl.DateTimeFormat('en').format(0)"));
}
