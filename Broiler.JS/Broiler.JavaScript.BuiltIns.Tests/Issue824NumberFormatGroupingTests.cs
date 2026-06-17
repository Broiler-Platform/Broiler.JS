using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824: Intl.NumberFormat grouping. The useGrouping option resolves to "auto"/"always"/
// "min2"/false (test262 test-option-useGrouping-extended, useGrouping-extended-*), grouping honours
// the locale's group sizes including India's 3,2,2… pattern (useGrouping-en-IN), and "min2" suppresses
// grouping unless the leading group has at least two digits (useGrouping-extended-en-US/de-DE).
public class Issue824NumberFormatGroupingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Theory]
    // en-US default ("auto"): standard 3-digit grouping.
    [InlineData("new Intl.NumberFormat('en-US').format(100)", "100")]
    [InlineData("new Intl.NumberFormat('en-US').format(1000)", "1,000")]
    [InlineData("new Intl.NumberFormat('en-US').format(1000000)", "1,000,000")]
    // en-IN: Indian 3,2,2 grouping.
    [InlineData("new Intl.NumberFormat('en-IN').format(1000)", "1,000")]
    [InlineData("new Intl.NumberFormat('en-IN').format(10000)", "10,000")]
    [InlineData("new Intl.NumberFormat('en-IN').format(100000)", "1,00,000")]
    [InlineData("new Intl.NumberFormat('en-IN').format(10000000)", "1,00,00,000")]
    // en-IN useGrouping:false stays ungrouped.
    [InlineData("new Intl.NumberFormat('en-IN', { useGrouping: false }).format(100000)", "100000")]
    public void GroupingByLocale(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    [Theory]
    // "always" groups from 1000 up.
    [InlineData("always", 100, "100")]
    [InlineData("always", 1000, "1,000")]
    [InlineData("always", 10000, "10,000")]
    [InlineData("always", 100000, "100,000")]
    // "min2" only groups when the leading group has 2+ digits.
    [InlineData("min2", 100, "100")]
    [InlineData("min2", 1000, "1000")]
    [InlineData("min2", 10000, "10,000")]
    [InlineData("min2", 100000, "100,000")]
    public void UseGroupingEnUs(string mode, int value, string expected)
        => Assert.Equal(expected, Eval($"new Intl.NumberFormat('en-US', {{ useGrouping: '{mode}' }}).format({value})"));

    [Theory]
    // de-DE uses '.' as the group separator; "min2" keeps 1000 ungrouped.
    [InlineData("min2", 1000, "1000")]
    [InlineData("min2", 10000, "10.000")]
    [InlineData("always", 1000, "1.000")]
    public void UseGroupingDeDe(string mode, int value, string expected)
        => Assert.Equal(expected, Eval($"new Intl.NumberFormat('de-DE', {{ useGrouping: '{mode}' }}).format({value})"));

    [Theory]
    // resolvedOptions().useGrouping mapping (test-option-useGrouping-extended).
    [InlineData("{}", "auto")]
    [InlineData("{ useGrouping: undefined }", "auto")]
    [InlineData("{ useGrouping: 'auto' }", "auto")]
    [InlineData("{ useGrouping: true }", "always")]
    [InlineData("{ useGrouping: 'always' }", "always")]
    [InlineData("{ useGrouping: false }", "false")]
    [InlineData("{ useGrouping: null }", "false")]
    [InlineData("{ useGrouping: 'min2' }", "min2")]
    [InlineData("{ useGrouping: 'false' }", "auto")]
    [InlineData("{ useGrouping: 'true' }", "auto")]
    [InlineData("{ notation: 'compact' }", "min2")]
    [InlineData("{ notation: 'compact', useGrouping: 'auto' }", "auto")]
    [InlineData("{ notation: 'compact', useGrouping: true }", "always")]
    [InlineData("{ notation: 'compact', useGrouping: false }", "false")]
    public void ResolvedUseGrouping(string options, string expected)
        => Assert.Equal(expected, Eval(
            $"String(new Intl.NumberFormat(undefined, {options}).resolvedOptions().useGrouping)"));
}

// resolvedOptions property order (test262 NumberFormat/prototype/resolvedOptions/order.js).
public class Issue824NumberFormatOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    [Fact]
    public void ResolvedOptionsOrder()
    {
        Load();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            const options = new Intl.NumberFormat([], {
              style: "currency", currency: "EUR", currencyDisplay: "symbol",
              minimumSignificantDigits: 1, maximumSignificantDigits: 2,
            }).resolvedOptions();
            const expected = ["locale","numberingSystem","style","currency","currencyDisplay",
              "currencySign","minimumIntegerDigits","minimumSignificantDigits",
              "maximumSignificantDigits","useGrouping","notation","signDisplay"];
            const actual = Object.getOwnPropertyNames(options);
            let ok = actual.indexOf("locale") > -1;
            for (let i = 1; i < expected.length; i++)
              ok = ok && (actual.indexOf(expected[i-1]) < actual.indexOf(expected[i]));
            String(ok);
        """).ToString();
        Assert.Equal("true", r);
    }
}
