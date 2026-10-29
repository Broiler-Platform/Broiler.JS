using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here (Temporal: every option getter is read/coerced before any algorithmic
// validation runs) — the second batch, extending the toString/with fixes to the
// remaining date-ish types:
//
//   PlainDate.with, PlainYearMonth.with/from, PlainMonthDay.with/from — an invalid
//   monthCode was rejected against the calendar before the overflow option getter fired.
//   Each now coerces monthCode to a string up front and validates it only after overflow.
//
//   PlainDateTime.round — the options were read in the wrong order (smallestUnit before
//   roundingIncrement/roundingMode) and the increment was never validated against the
//   unit. It now reads roundingIncrement, roundingMode, then smallestUnit, and validates
//   the increment afterwards (e.g. roundingIncrement 25 with smallestUnit "hour" throws).
public class Issue836TemporalOptionsRead2Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

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
    public void PlainDateWithReadsOverflowBeforeRejectingMonthCode()
        => Assert.Equal("overflow", Eval(Observer + @"
            var options = obs({ overflow: 'constrain' });
            try { new Temporal.PlainDate(2025, 7, 31).with({ monthCode: 'M08L' }, options); } catch (e) {}
            actual.join(',');
        "));

    [Fact]
    public void PlainYearMonthFromReadsOverflowBeforeRejectingMonthCode()
        => Assert.Equal("overflow", Eval(Observer + @"
            var options = obs({ overflow: 'constrain' });
            try { Temporal.PlainYearMonth.from({ year: 2025, monthCode: 'M08L' }, options); } catch (e) {}
            actual.join(',');
        "));

    [Fact]
    public void PlainMonthDayFromReadsOverflowBeforeRejectingMonthCode()
        => Assert.Equal("overflow", Eval(Observer + @"
            var options = obs({ overflow: 'constrain' });
            try { Temporal.PlainMonthDay.from({ monthCode: 'M08L', day: 1 }, options); } catch (e) {}
            actual.join(',');
        "));

    [Fact]
    public void PlainDateTimeRoundReadsOptionsInOrder()
        => Assert.Equal("roundingIncrement,roundingMode,smallestUnit", Eval(Observer + @"
            var options = obs({ smallestUnit: 'hour', roundingIncrement: 25, roundingMode: 'expand' });
            try { new Temporal.PlainDateTime(2025, 8, 14, 12).round(options); } catch (e) {}
            actual.join(',');
        "));

    [Fact]
    public void PlainDateTimeRoundRejectsIncrementNotDividingUnit()
        => Assert.Equal("RangeError", Eval(@"
            var r;
            try { new Temporal.PlainDateTime(2025, 8, 14, 12).round({ smallestUnit: 'hour', roundingIncrement: 25 }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r;
        "));

    [Fact]
    public void PlainDateTimeRoundAcceptsValidIncrement()
        => Assert.Equal("2025-08-14T12:00:00", Eval(
            "new Temporal.PlainDateTime(2025, 8, 14, 12, 10).round({ smallestUnit: 'hour', roundingIncrement: 12 }).toString()"));
}
