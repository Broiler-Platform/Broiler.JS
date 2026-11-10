using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problem 8): Intl.DateTimeFormat with a Chinese calendar formats a numeric year as the
// related (Gregorian) year plus the cyclic year name, using CLDR's zh pattern "rU年" (e.g.
// "2019己亥年"). Mirrors test262 DateTimeFormat/prototype/format/related-year-zh.js.
public class Issue824RelatedYearZhTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void RelatedYearZh()
    {
        var formatted = Eval("""
            const df = new Intl.DateTimeFormat("zh-u-ca-chinese", { year: "numeric" });
            df.format(new Date(2019, 5, 1));
        """);
        Assert.Contains(formatted, new[] { "2019己亥年", "己亥年" });
    }

    [Theory]
    // The cyclic year name advances with the sexagenary cycle.
    [InlineData("2019", "2019己亥年")]
    [InlineData("2020", "2020庚子年")]
    [InlineData("2017", "2017丁酉年")]
    public void CyclicYearName(string year, string expected)
        => Assert.Equal(expected, Eval(
            $"new Intl.DateTimeFormat('zh-u-ca-chinese', {{ year: 'numeric' }}).format(new Date({year}, 5, 1))"));

    [Fact]
    public void NonChineseLocaleKeepsParenthesizedForm()
    {
        // en keeps the root "r(U)" form rather than the zh "rU年" form.
        // Revised 2026-11-09, at SJL-9.5 (task 8.5, row T35, a correction): the cyclic year name is en's own
        // in CLDR 48.2.0, "ji-hai", where the Chinese name was written in English; the test expected "2019(己亥)".
        var formatted = Eval(
            "new Intl.DateTimeFormat('en-u-ca-chinese', { year: 'numeric' }).format(new Date(2019, 5, 1))");
        Assert.Equal("2019(ji-hai)", formatted);
    }
}
