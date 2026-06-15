using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Verifies that Temporal.{PlainDate,PlainDateTime,PlainMonthDay,PlainYearMonth}.from read the
// `overflow` option only after the item's type is validated (spec ToTemporalX order of
// operations), and that a PlainDateTime is converted to a PlainDate via its internal slots
// rather than its observable getters.
public class TemporalFromOverflowOrderTests
{
    private static void EnsureBuiltInsLoaded()
        => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    [Theory]
    [InlineData("PlainDate")]
    [InlineData("PlainDateTime")]
    [InlineData("PlainMonthDay")]
    [InlineData("PlainYearMonth")]
    public void From_PrimitiveItem_ThrowsBeforeReadingOverflow(string type)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            var log = [];
            var options = { get overflow() { log.push('get overflow'); return 'constrain'; } };
            var threw = false;
            try { Temporal.{{type}}.from(7, options); } catch (e) { threw = e instanceof TypeError; }
            threw + '|' + log.join(',');
        """);
        // TypeError thrown for the numeric item, and options.overflow never observed.
        Assert.Equal("true|", result.ToString());
    }

    [Fact]
    public void PlainDateFrom_PlainDateTime_UsesSlotsNotGetters()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var pdt = new Temporal.PlainDateTime(2021, 7, 3, 12, 30, 0);
            var log = [];
            ['year','month','monthCode','day','calendar','calendarId'].forEach(function (p) {
                var d = Object.getOwnPropertyDescriptor(Temporal.PlainDateTime.prototype, p);
                if (d && d.get) {
                    Object.defineProperty(pdt, p, { get: function () { log.push(p); return d.get.call(this); } });
                }
            });
            var pd = Temporal.PlainDate.from(pdt);
            pd.toString() + '|' + log.join(',');
        """);
        Assert.Equal("2021-07-03|", result.ToString());
    }
}
