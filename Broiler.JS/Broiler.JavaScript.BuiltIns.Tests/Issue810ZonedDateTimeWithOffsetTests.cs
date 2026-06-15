using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.ZonedDateTime.prototype.with validates the offset field up front (as PrepareCalendarFields
// does): a malformed offset string is a RangeError and a non-string offset is a TypeError, rather than
// the TypeError that previously leaked from delegating an offset-only partial to PlainDateTime.with.
// Issue #810 problem 93.
public class Issue810ZonedDateTimeWithOffsetTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string ErrorFor(string offsetLiteral, string offsetOption) => Eval($$"""
            var zdt = new Temporal.ZonedDateTime(0n, "UTC");
            try { zdt.with({ offset: {{offsetLiteral}} }, { offset: "{{offsetOption}}" }); "no throw"; }
            catch (e) { e.constructor.name; }
        """);

    [Theory]
    [InlineData("\"00:00\"")]   // missing sign
    [InlineData("\"+0\"")]      // too short
    [InlineData("\"-000:00\"")] // too long
    public void InvalidOffsetString_ThrowsRangeError(string offsetLiteral)
    {
        foreach (var opt in new[] { "use", "prefer", "ignore", "reject" })
            Assert.Equal("RangeError", ErrorFor(offsetLiteral, opt));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("1000n")]
    public void NonStringOffset_ThrowsTypeError(string offsetLiteral)
    {
        foreach (var opt in new[] { "use", "prefer", "ignore", "reject" })
            Assert.Equal("TypeError", ErrorFor(offsetLiteral, opt));
    }
}
