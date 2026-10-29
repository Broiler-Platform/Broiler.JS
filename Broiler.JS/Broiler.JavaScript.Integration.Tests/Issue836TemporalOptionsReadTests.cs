using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here (Temporal: all option getters are read/coerced before any algorithmic
// validation runs):
//
//   Problem 48 (Instant.prototype.toString) — smallestUnit was validated (a date unit
//   rejected) while being read, before the timeZone option getter fired.
//   Problem 47 (ZonedDateTime.prototype.toString) — likewise, before timeZoneName.
//   Problem 65 (ZonedDateTime.prototype.with) — an invalid monthCode was rejected
//   before the disambiguation/offset/overflow option getters fired.
//
//   Each method now coerces the option values first and defers the algorithmic
//   validation (the date-unit / invalid-month-code rejection) until every option has
//   been read, while a non-object options bag is still a TypeError raised only after the
//   partial date-time fields are processed (so `with({day:-1}, primitive)` stays a
//   RangeError).
public class Issue836TemporalOptionsReadTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // A property bag whose getters record the order they are read in.
    private const string Observer = @"
        var actual = [];
        function obs(spec) {
            var o = {};
            Object.keys(spec).forEach(function(k) {
                Object.defineProperty(o, k, { enumerable: true, get: function() {
                    actual.push(k); return spec[k];
                }});
            });
            return o;
        }
    ";

    [Fact]
    public void InstantToStringReadsTimeZoneBeforeRejectingDateUnit()
        => Assert.Equal("fractionalSecondDigits,roundingMode,smallestUnit,timeZone", Eval(Observer + @"
            var options = obs({ smallestUnit: 'month', fractionalSecondDigits: 'auto', roundingMode: 'expand', timeZone: undefined });
            try { new Temporal.Instant(0n).toString(options); } catch (e) {}
            actual.join(',');
        "));

    [Fact]
    public void ZonedDateTimeToStringReadsTimeZoneNameBeforeRejectingDateUnit()
        => Assert.Equal("calendarName,fractionalSecondDigits,offset,roundingMode,smallestUnit,timeZoneName", Eval(Observer + @"
            var options = obs({ calendarName: 'auto', fractionalSecondDigits: 'auto', offset: 'auto',
                                roundingMode: 'expand', smallestUnit: 'month', timeZoneName: 'auto' });
            try { new Temporal.ZonedDateTime(0n, 'UTC').toString(options); } catch (e) {}
            actual.join(',');
        "));

    [Fact]
    public void ZonedDateTimeWithReadsAllOptionsBeforeRejectingMonthCode()
        => Assert.Equal("disambiguation,offset,overflow", Eval(Observer + @"
            var options = obs({ overflow: 'constrain', offset: 'prefer', disambiguation: 'compatible' });
            try { new Temporal.ZonedDateTime(0n, 'UTC').with({ monthCode: 'M08L' }, options); } catch (e) {}
            actual.join(',');
        "));

    // A non-object options bag is still a TypeError, but only after the partial fields are
    // processed: an invalid field is a RangeError first.
    [Fact]
    public void ZonedDateTimeWithPrimitiveOptionsValidFieldsThrowsTypeError()
        => Assert.Equal("TypeError", Eval(@"
            var r;
            try { new Temporal.ZonedDateTime(0n, 'UTC').with({ day: 5 }, 'bad'); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r;
        "));

    [Fact]
    public void ZonedDateTimeWithPrimitiveOptionsInvalidFieldThrowsRangeError()
        => Assert.Equal("RangeError", Eval(@"
            var r;
            try { new Temporal.ZonedDateTime(0n, 'UTC').with({ day: -1 }, 'bad'); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r;
        "));
}
