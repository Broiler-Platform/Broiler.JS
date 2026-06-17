using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problem 98): the Intl.DateTimeFormat constructor reads the calendar option
// and then the numberingSystem option immediately after localeMatcher (ECMA-402
// InitializeDateTimeFormat). Mirrors test262
// DateTimeFormat/constructor-calendar-numberingSystem-order.
public class Issue830DateTimeFormatOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void CalendarThenNumberingSystemAfterLocaleMatcher()
    {
        var r = Eval("""
            const order = [];
            const options = {};
            for (const name of ["localeMatcher","calendar","numberingSystem"])
              Object.defineProperty(options, name, {
                get() { order.push(name); return undefined; }, enumerable: true,
              });
            new Intl.DateTimeFormat(undefined, options);
            JSON.stringify(order);
        """);
        Assert.Equal("[\"localeMatcher\",\"calendar\",\"numberingSystem\"]", r);
    }

    [Theory]
    // A malformed calendar option is a RangeError at construction.
    [InlineData("(function(){ try { new Intl.DateTimeFormat('en', { calendar: '!' }); return 'no-throw'; } catch (e) { return e.constructor.name; } })()", "RangeError")]
    // A well-formed calendar option still flows through to resolvedOptions.
    [InlineData("new Intl.DateTimeFormat('en', { calendar: 'buddhist' }).resolvedOptions().calendar", "buddhist")]
    [InlineData("new Intl.DateTimeFormat('en', { numberingSystem: 'arab' }).resolvedOptions().numberingSystem", "arab")]
    public void Behavior(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
