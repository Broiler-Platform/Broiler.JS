using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problem 97): Intl.NumberFormat.prototype.resolvedOptions returns its keys in
// the ECMA-402 table order, where compactDisplay precedes signDisplay (and unitDisplay sits
// with unit, before the digit options). Mirrors test262
// NumberFormat/prototype/resolvedOptions/return-keys-order-default.
public class Issue830NumberFormatResolvedOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void CompactCurrencyKeyOrder()
    {
        var r = Eval("""
            const keys = Object.keys(
              new Intl.NumberFormat(undefined, { notation: "compact", style: "currency", currency: "XTS" })
                .resolvedOptions());
            const expected = ["locale","numberingSystem","style","currency","currencyDisplay",
              "currencySign","minimumIntegerDigits","minimumFractionDigits","maximumFractionDigits",
              "useGrouping","notation","compactDisplay","signDisplay","roundingIncrement",
              "roundingMode","roundingPriority","trailingZeroDisplay"];
            JSON.stringify(keys) === JSON.stringify(expected) ? "ok" : JSON.stringify(keys);
        """);
        Assert.Equal("ok", r);
    }

    [Fact]
    public void UnitDisplayBeforeDigitOptions()
    {
        var r = Eval("""
            const keys = Object.keys(
              new Intl.NumberFormat(undefined, { style: "unit", unit: "hour", unitDisplay: "long" })
                .resolvedOptions());
            // unit/unitDisplay come together, ahead of minimumIntegerDigits.
            const iU = keys.indexOf("unit"), iUD = keys.indexOf("unitDisplay"),
                  iMID = keys.indexOf("minimumIntegerDigits");
            String(iU < iUD && iUD < iMID);
        """);
        Assert.Equal("true", r);
    }

    [Theory]
    // compactDisplay only appears for compact notation, and before signDisplay.
    [InlineData("Object.keys(new Intl.NumberFormat('en', { notation: 'compact' }).resolvedOptions()).indexOf('compactDisplay') < Object.keys(new Intl.NumberFormat('en', { notation: 'compact' }).resolvedOptions()).indexOf('signDisplay')", "true")]
    [InlineData("'compactDisplay' in new Intl.NumberFormat('en').resolvedOptions()", "false")]
    public void Behavior(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
