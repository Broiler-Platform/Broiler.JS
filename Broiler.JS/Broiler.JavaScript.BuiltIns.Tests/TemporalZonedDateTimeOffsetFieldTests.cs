using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

public class TemporalZonedDateTimeOffsetFieldTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    [Fact]
    public void OffsetObjectWithToString_IsCoerced_AndObserved()
    {
        Load();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var log = [];
            var bag = {
              timeZone: "UTC",
              year: 2020, month: 1, day: 1, hour: 0, minute: 0,
              offset: { toString() { log.push('toString'); return '+00:00'; }, valueOf() { log.push('valueOf'); return 0; } }
            };
            var zdt = Temporal.ZonedDateTime.from(bag, { offset: 'use' });
            zdt.offset + '|' + log.join(',');
        """);
        Assert.Equal("+00:00|toString", result.ToString());
    }

    [Fact]
    public void OffsetNumber_ThrowsTypeError()
    {
        Load();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var threw = false;
            try { Temporal.ZonedDateTime.from({ timeZone: 'UTC', year: 2020, month: 1, day: 1, offset: 5 }); }
            catch (e) { threw = e instanceof TypeError; }
            threw;
        """);
        Assert.True(result.BooleanValue);
    }
}
