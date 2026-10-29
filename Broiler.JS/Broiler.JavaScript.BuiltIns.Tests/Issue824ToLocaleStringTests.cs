using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problems 55, 88): Number.prototype.toLocaleString and BigInt.prototype.toLocaleString
// must produce the same result as Intl.NumberFormat(locales, options).format(this). Mirrors test262
// {Number,BigInt}/prototype/toLocaleString/returns-same-results-as-NumberFormat.js.
public class Issue824ToLocaleStringTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void NumberMatchesNumberFormat()
    {
        var r = Eval("""
            var numbers = [0, -0, 1, -1, 5.5, 123, -123, -123.45, 123.44501, 0.001234,
                -0.00000000123, 0.00000000000000000000000000000123, 1.2, 0.0000000012344501,
                123445.01, 12344501000000000000000000000000000, -12344501000000000000000000000000000,
                Infinity, -Infinity, NaN];
            var locales = [undefined, ["de"], ["th-u-nu-thai"], ["en"], ["ja-u-nu-jpanfin"], ["ar-u-nu-arab"]];
            var options = [
                undefined,
                {style: "percent"},
                {style: "currency", currency: "EUR", currencyDisplay: "symbol"},
                {useGrouping: false, minimumIntegerDigits: 3, minimumFractionDigits: 1, maximumFractionDigits: 3}
            ];
            var mismatches = [];
            locales.forEach(function (locales) {
                options.forEach(function (options) {
                    var ref = new Intl.NumberFormat(locales, options);
                    numbers.forEach(function (n) {
                        var a = ref.format(n);
                        var b = n.toLocaleString(locales, options);
                        if (a !== b) mismatches.push(n + ": '" + a + "' vs '" + b + "'");
                    });
                });
            });
            mismatches.length === 0 ? "ok" : mismatches.join(" | ");
        """);
        Assert.Equal("ok", r);
    }

    [Fact]
    public void BigIntMatchesNumberFormat()
    {
        var r = Eval("""
            var inputs = [0n, -0n, 1n, -1n, 123n, -123n, 12345n, -12345n,
              12344501000000000000000000000000000n, -12344501000000000000000000000000000n];
            var locales = [undefined, ["de"], ["th-u-nu-thai"], ["en"], ["ja-u-nu-jpanfin"], ["ar-u-nu-arab"]];
            var options = [
                undefined,
                {style: "percent"},
                {style: "currency", currency: "EUR", currencyDisplay: "symbol"},
                {useGrouping: false, minimumIntegerDigits: 3, minimumFractionDigits: 1, maximumFractionDigits: 3}
            ];
            var mismatches = [];
            for (const loc of locales)
              for (const opt of options) {
                const ref = new Intl.NumberFormat(loc, opt);
                for (const n of inputs) {
                  const a = ref.format(n);
                  const b = n.toLocaleString(loc, opt);
                  if (a !== b) mismatches.push(n + ": '" + a + "' vs '" + b + "'");
                }
              }
            mismatches.length === 0 ? "ok" : mismatches.join(" | ");
        """);
        Assert.Equal("ok", r);
    }

    [Theory]
    // Spot checks: default grouping now applies through Intl.NumberFormat.
    [InlineData("(12345).toLocaleString('en-US')", "12,345")]
    [InlineData("(12345).toLocaleString()", "12,345")]
    [InlineData("(1234567).toLocaleString('en-IN')", "12,34,567")]
    [InlineData("(12345n).toLocaleString('en-US')", "12,345")]
    [InlineData("(1234567n).toLocaleString('en-IN')", "12,34,567")]
    public void SpotChecks(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
