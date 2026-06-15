using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Sub-minute UTC-offset handling for time zones whose historical offset is not a whole number of
// minutes (issue #805 problems 1 & 21). .NET's TimeZoneInfo truncates such offsets to whole minutes
// (Africa/Monrovia's -00:44:30 → -00:44:00, Pacific/Niue's -11:19:40 → -11:19:00); the engine
// restores the precise value, and a minute-precision offset in a string matches it via the spec's
// match-minutes rounding while a sub-minute offset must match exactly.
public class TemporalZonedDateTimeSubMinuteOffsetTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static JSValue Eval(string src)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(src);
    }

    // The instant of 1970-01-01T00:00:00 in Africa/Monrovia is 44 min 30 s after the epoch, because
    // the zone's offset was -00:44:30 at the time.
    [Fact]
    public void Monrovia_ExactSubMinuteOffsetInString_Parses()
    {
        var result = Eval("""
            Temporal.ZonedDateTime.from("1970-01-01T00:00:00-00:44:30[Africa/Monrovia]").epochNanoseconds.toString();
        """);
        Assert.Equal((44 * 60 + 30) * 1_000_000_000L + "", result.ToString());
    }

    [Fact]
    public void Monrovia_MinutePrecisionOffsetInString_MatchesByRounding()
    {
        // "-00:45" has minute precision, so it matches the rounded (-00:44:30 → -00:45) zone offset;
        // the resulting instant still uses the real -00:44:30 offset.
        var result = Eval("""
            Temporal.ZonedDateTime.from("1970-01-01T00:00:00-00:45[Africa/Monrovia]").epochNanoseconds.toString();
        """);
        Assert.Equal((44 * 60 + 30) * 1_000_000_000L + "", result.ToString());
    }

    [Theory]
    // A sub-minute offset string must match the zone's offset exactly; a wrong :SS, or the rounded
    // value written with :SS precision, is rejected (RangeError) under the default offset:"reject".
    [InlineData("1970-01-01T00:00:00-00:44:40[Africa/Monrovia]")]
    [InlineData("1970-01-01T00:00:00-00:45:00[Africa/Monrovia]")]
    public void Monrovia_WrongOrRoundedSubMinuteOffset_Throws(string str)
    {
        var result = Eval($$"""
            var threw = false;
            try { Temporal.ZonedDateTime.from("{{str}}"); }
            catch (e) { threw = e instanceof RangeError; }
            threw;
        """);
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Monrovia_PropertyBag_DoesNoFuzzyMatching()
    {
        // No match-minutes rounding for a property-bag offset: "-00:45" does not match -00:44:30.
        var result = Eval("""
            var threw = false;
            try {
              Temporal.ZonedDateTime.from(
                { timeZone: "Africa/Monrovia", year: 1970, month: 1, day: 1, offset: "-00:45" });
            } catch (e) { threw = e instanceof RangeError; }
            threw;
        """);
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Monrovia_PropertyBag_ExactSubMinuteOffset_Parses()
    {
        var result = Eval("""
            Temporal.ZonedDateTime.from(
              { timeZone: "Africa/Monrovia", year: 1970, month: 1, day: 1, offset: "-00:44:30" })
              .epochNanoseconds.toString();
        """);
        Assert.Equal((44 * 60 + 30) * 1_000_000_000L + "", result.ToString());
    }

    [Fact]
    public void Monrovia_OffsetGetter_ReportsSubMinuteValue()
    {
        var result = Eval("""new Temporal.ZonedDateTime(0n, "Africa/Monrovia").offset;""");
        Assert.Equal("-00:44:30", result.ToString());
    }

    [Fact]
    public void Niue_MinutePrecisionOffsetInString_MatchesByRounding()
    {
        // Pacific/Niue used -11:19:40 before 1952-10-16; "-11:20" matches it by rounding to the minute.
        var result = Eval("""
            var ref_ = new Temporal.ZonedDateTime(-543069621000000000n, "Pacific/Niue");
            ref_.equals("1952-10-16T00:00:00-11:20[Pacific/Niue]");
            new Temporal.ZonedDateTime(-543069621000000000n, "Pacific/Niue").offset;
        """);
        Assert.Equal("-11:19:40", result.ToString());
    }
}
