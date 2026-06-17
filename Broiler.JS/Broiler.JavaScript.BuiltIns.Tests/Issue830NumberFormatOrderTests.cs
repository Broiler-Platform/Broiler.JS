using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problems 99 and 100): the Intl.NumberFormat constructor must read its
// option getters in ECMA-402 InitializeNumberFormat order. numberingSystem is read right
// after localeMatcher (ahead of style), currencyDisplay/currencySign are read after
// currency, and the rounding options precede compactDisplay/useGrouping/signDisplay.
// Mirrors test262 NumberFormat/constructor-option-read-order and
// constructor-numberingSystem-order.
public class Issue830NumberFormatOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void FullOptionReadOrder()
    {
        var r = Eval("""
            const order = [];
            function track(names, value) {
              const options = {};
              for (const name of names)
                Object.defineProperty(options, name, {
                  get() { order.push(name); return value[name]; },
                  enumerable: true,
                });
              return options;
            }
            const names = ["localeMatcher","numberingSystem","style","currency","currencyDisplay",
              "currencySign","unit","unitDisplay","notation","minimumIntegerDigits",
              "minimumFractionDigits","maximumFractionDigits","minimumSignificantDigits",
              "maximumSignificantDigits","roundingIncrement","roundingMode","roundingPriority",
              "trailingZeroDisplay","compactDisplay","useGrouping","signDisplay"];
            // Valid values so construction succeeds; the rest fall back to defaults.
            const values = { style: "currency", currency: "EUR", unit: "percent" };
            new Intl.NumberFormat(undefined, track(names, values));
            JSON.stringify(order) === JSON.stringify(names) ? "ok" : JSON.stringify(order);
        """);
        Assert.Equal("ok", r);
    }

    [Fact]
    public void NumberingSystemReadBeforeStyle()
    {
        var r = Eval("""
            const order = [];
            const options = {};
            for (const name of ["localeMatcher","numberingSystem","style"])
              Object.defineProperty(options, name, {
                get() { order.push(name); return undefined; }, enumerable: true,
              });
            new Intl.NumberFormat(undefined, options);
            JSON.stringify(order);
        """);
        Assert.Equal("[\"localeMatcher\",\"numberingSystem\",\"style\"]", r);
    }

    [Theory]
    // A supported numberingSystem option still drives the digit mapping after the reorder.
    [InlineData("new Intl.NumberFormat('en', { numberingSystem: 'arab' }).format(123)", "١٢٣")]
    [InlineData("new Intl.NumberFormat('en').resolvedOptions().numberingSystem", "latn")]
    [InlineData("new Intl.NumberFormat('en', { numberingSystem: 'arab' }).resolvedOptions().numberingSystem", "arab")]
    // currencyDisplay/currencySign are now validated at construction (RangeError on a bad value).
    [InlineData("(function(){ try { new Intl.NumberFormat('en', { currencyDisplay: 'bogus' }); return 'no-throw'; } catch (e) { return e.constructor.name; } })()", "RangeError")]
    [InlineData("(function(){ try { new Intl.NumberFormat('en', { currencySign: 'bogus' }); return 'no-throw'; } catch (e) { return e.constructor.name; } })()", "RangeError")]
    public void Behavior(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
