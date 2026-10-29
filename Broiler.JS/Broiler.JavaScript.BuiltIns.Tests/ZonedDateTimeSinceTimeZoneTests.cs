using Broiler.JavaScript.Engine;
namespace Broiler.JavaScript.BuiltIns.Tests;

// ZonedDateTime since/until: a calendar-unit largestUnit requires both operands to share a time
// zone (compared canonically); a time-unit largestUnit works across zones (issue #798 problem 3
// subset: canonicalize-iana-identifiers-before-comparing).
public class ZonedDateTimeSinceTimeZoneTests
{
    private static string Eval(string e){ using var c=new JSContext(); try { return c.Eval(e).ToString(); } catch(System.Exception ex){ return "THROW:"+ex.GetType().Name; } }

    [Fact]
    public void CalendarUnit_DifferentZones_Throws()
    {
        var r = Eval(@"
            var a=Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Kolkata]');
            var b=Temporal.ZonedDateTime.from('2021-09-01T05:30:00+05:30[Asia/Colombo]');
            var t=''; try { a.since(b,{largestUnit:'day'}); } catch(e){ t=e.constructor.name; } t;");
        Assert.Equal("RangeError", r);
    }

    [Fact]
    public void CalendarUnit_SameZone_Works()
    {
        var r = Eval(@"
            var a=Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Kolkata]');
            var b=Temporal.ZonedDateTime.from('2021-09-01T00:00:00+05:30[Asia/Kolkata]');
            String(a.since(b,{largestUnit:'day'}));");
        Assert.Equal("-P609D", r);
    }

    [Fact]
    public void TimeUnit_DifferentZones_Works()
    {
        var r = Eval(@"
            var a=Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Kolkata]');
            var b=Temporal.ZonedDateTime.from('2020-01-02T00:00:00+05:30[Asia/Colombo]');
            String(a.since(b,{largestUnit:'hours'}));");
        Assert.Equal("-PT24H", r);
    }

    [Fact]
    public void Until_CalendarUnit_DifferentZones_Throws()
    {
        var r = Eval(@"
            var a=Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Kolkata]');
            var b=Temporal.ZonedDateTime.from('2021-09-01T05:30:00+05:30[Asia/Colombo]');
            var t=''; try { a.until(b,{largestUnit:'months'}); } catch(e){ t=e.constructor.name; } t;");
        Assert.Equal("RangeError", r);
    }
}
