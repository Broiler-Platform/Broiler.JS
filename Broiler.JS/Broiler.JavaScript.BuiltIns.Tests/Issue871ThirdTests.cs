using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #871 (third batch): three more test262 script-host clusters.
//  - The Date(year, month, …) constructor must ToNumber *every* argument, in order, even when an
//    earlier one is NaN — so a later argument's coercion side effect (a throwing valueOf) is still
//    observed instead of bailing out early to an Invalid Date. Mirrors test262
//    staging/sm/Date/constructor-convert-all-arguments.js.
//  - A PrivateIdentifier (`#x`) is only valid as the left operand of `in` (the ergonomic brand
//    check); every other position — `!#x`, `#x + 1`, `1 + #x in o` — is an early SyntaxError.
//    Mirrors test262 staging/sm/PrivateName/illegal-in-class-context.js.
//  - Temporal.PlainMonthDay.from must reject an out-of-range supplied year/era for the
//    Gregorian-family calendars (it was silently dropping it to the reference year). Mirrors test262
//    intl402/Temporal/PlainMonthDay/from/dont-calculate-month-info-for-out-of-range-year.js.
public class Issue871ThirdTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(source).ToString();
    }

    private static string ThrownErrorName(string expr) => Eval($$"""
        (function () {
            try { {{expr}}; return "no throw"; }
            catch (e) { return e && e.constructor ? e.constructor.name : String(e); }
        })()
        """);

    // ---- Cluster 1: Date constructor coerces all arguments ------------------------------------

    [Fact]
    public void Date_CoercesAllArguments_EvenAfterNaN()
        => Assert.Equal("8", Eval("""
            var bad = { toString: function () { throw 17; }, valueOf: function () { throw 42; } };
            var hits = 0;
            [
              function () { new Date(bad); },
              function () { new Date(NaN, bad); },
              function () { new Date(Infinity, bad); },
              function () { new Date(1970, NaN, bad); },
              function () { new Date(1970, 4, NaN, bad); },
              function () { new Date(1970, 4, 17, NaN, bad); },
              function () { new Date(1970, 4, 17, 13, NaN, bad); },
              function () { new Date(1970, 4, 17, 13, 37, NaN, bad); },
            ].forEach(function (f) { try { f(); } catch (e) { if (e === 42) hits++; } });
            String(hits);
            """));

    [Theory]
    [InlineData("isNaN(new Date(NaN).getTime())", "true")]      // single-arg NaN is still Invalid Date
    [InlineData("new Date(2020, 5, 15).getFullYear()", "2020")] // ordinary multi-arg still works
    [InlineData("isNaN(new Date(2020, NaN).getTime())", "true")]// NaN in a component still yields NaN
    public void Date_OrdinaryCasesUnchanged(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    // ---- Cluster 2: PrivateIdentifier only valid as left operand of `in` -----------------------

    [Theory]
    [InlineData("class A { #x; h(o){ return !#x; }}")]
    [InlineData("class A { #x; h(o){ return #x + 1; }}")]
    [InlineData("class A { #x; h(o){ return 1 + #x in o; }}")]
    [InlineData("class A { #x; h(o){ return typeof #x; }}")]
    [InlineData("class A { #x; h(o){ return #x; }}")]
    [InlineData("class A { #x; h(o){ return o in #x; }}")]
    public void PrivateIdentifier_InvalidPositionIsSyntaxError(string src)
        => Assert.Equal("SyntaxError", ThrownErrorName($"eval({Quote(src)})"));

    [Theory]
    [InlineData("class A { #x; h(o){ return #x in o; }} 'ok'", "ok")]
    [InlineData("class A { #x; h(o){ return (#x in o); }} 'ok'", "ok")]
    [InlineData("class A { #x; h(o){ return #x in o && true; }} 'ok'", "ok")]
    [InlineData("class A { #x = 1; static h(o){ return o.#x; }} 'ok'", "ok")]
    public void PrivateIdentifier_ValidUsesStillParse(string src, string expected)
        => Assert.Equal(expected, Eval(src));

    private static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    // ---- Cluster 3: PlainMonthDay.from rejects out-of-range year -------------------------------

    [Theory]
    [InlineData("buddhist", "M02", 29)]
    [InlineData("gregory", "M02", 29)]
    [InlineData("japanese", "M02", 29)]
    [InlineData("roc", "M02", 29)]
    [InlineData("hebrew", "M05L", 29)]
    [InlineData("islamic-civil", "M12", 30)]
    [InlineData("persian", "M12", 30)]
    public void PlainMonthDay_RejectsOutOfRangeYear(string calendar, string monthCode, int day)
    {
        Assert.Equal("RangeError", ThrownErrorName(
            $"Temporal.PlainMonthDay.from({{ year: -999999, monthCode: '{monthCode}', day: {day}, calendar: '{calendar}' }})"));
        Assert.Equal("RangeError", ThrownErrorName(
            $"Temporal.PlainMonthDay.from({{ year: 999999, monthCode: '{monthCode}', day: {day}, calendar: '{calendar}' }})"));
    }

    [Theory]
    // A bare month-day (no year) resolves against the leap-year reference, and an in-range year is fine.
    [InlineData("Temporal.PlainMonthDay.from({ monthCode: 'M02', day: 29, calendar: 'buddhist' }).toString()", "1972-02-29[u-ca=buddhist]")]
    [InlineData("Temporal.PlainMonthDay.from({ year: 2024, monthCode: 'M02', day: 29, calendar: 'gregory' }).monthCode", "M02")]
    [InlineData("Temporal.PlainMonthDay.from({ monthCode: 'M12', day: 25 }).day", "25")]
    public void PlainMonthDay_ValidStillWorks(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
