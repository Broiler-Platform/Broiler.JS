using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problems 82, 85): Intl.supportedValuesOf("currency") / ("unit") must be consistent with
// the values DisplayNames / NumberFormat accept. Mirrors test262 Intl/supportedValuesOf/
// currencies-accepted-by-DisplayNames.js and units-accepted-by-NumberFormat.js.
public class Issue824SupportedValuesOfTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void UnitsAcceptedByNumberFormat()
    {
        var sanctioned = """
            ["acre","bit","byte","celsius","centimeter","day","degree","fahrenheit","fluid-ounce","foot",
             "gallon","gigabit","gigabyte","gram","hectare","hour","inch","kilobit","kilobyte","kilogram",
             "kilometer","liter","megabit","megabyte","meter","microsecond","mile","mile-scandinavian",
             "milliliter","millimeter","millisecond","minute","month","nanosecond","ounce","percent",
             "petabyte","pound","second","stone","terabit","terabyte","week","yard","year"]
            """;
        var r = Eval($$"""
            const units = Intl.supportedValuesOf("unit");
            const problems = [];
            for (const unit of units) {
              const got = new Intl.NumberFormat("en", { style: "unit", unit }).resolvedOptions().unit;
              if (got !== unit) problems.push("listed-but-not-accepted:" + unit);
            }
            for (const unit of {{sanctioned}}) {
              const accepted = new Intl.NumberFormat("en", { style: "unit", unit }).resolvedOptions().unit === unit;
              if (accepted && !units.includes(unit)) problems.push("accepted-but-not-listed:" + unit);
              if (!accepted && units.includes(unit)) problems.push("listed-but-rejected:" + unit);
            }
            problems.length === 0 ? "ok" : problems.join(" | ");
        """);
        Assert.Equal("ok", r);
    }

    [Fact]
    public void CurrenciesAcceptedByDisplayNames()
    {
        var r = Eval("""
            const currencies = Intl.supportedValuesOf("currency");
            const obj = new Intl.DisplayNames("en", { type: "currency", fallback: "none" });
            const problems = [];
            for (const currency of currencies) {
              if (typeof obj.of(currency) !== "string") problems.push("listed-but-no-name:" + currency);
            }
            for (let i = 0x41; i <= 0x5A; ++i)
              for (let j = 0x41; j <= 0x5A; ++j)
                for (let k = 0x41; k <= 0x5A; ++k) {
                  const c = String.fromCharCode(i, j, k);
                  const named = typeof obj.of(c) === "string";
                  if (named && !currencies.includes(c)) problems.push("named-but-not-listed:" + c);
                  if (!named && currencies.includes(c)) problems.push("listed-but-not-named:" + c);
                }
            problems.length === 0 ? "ok" : problems.slice(0, 10).join(" | ");
        """);
        Assert.Equal("ok", r);
    }

    [Theory]
    [InlineData("Intl.supportedValuesOf('unit').length", "45")]
    [InlineData("Intl.supportedValuesOf('unit')[0]", "acre")]
    [InlineData("Intl.supportedValuesOf('currency').includes('USD')", "true")]
    [InlineData("Intl.supportedValuesOf('currency').includes('AAA')", "false")]
    // Sorted (code-unit) order is preserved.
    [InlineData("(()=>{const u=Intl.supportedValuesOf('unit');return u.every((v,i)=>i===0||u[i-1]<v);})()", "true")]
    [InlineData("(()=>{const c=Intl.supportedValuesOf('currency');return c.every((v,i)=>i===0||c[i-1]<v);})()", "true")]
    // DisplayNames fallback handling for an unknown currency.
    [InlineData("new Intl.DisplayNames('en',{type:'currency',fallback:'none'}).of('AAA')", "undefined")]
    [InlineData("new Intl.DisplayNames('en',{type:'currency',fallback:'code'}).of('AAA')", "AAA")]
    public void SpotChecks(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
