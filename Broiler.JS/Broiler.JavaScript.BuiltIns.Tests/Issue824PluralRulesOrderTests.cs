using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problem 37): Intl.PluralRules.prototype.resolvedOptions reports "notation" (after
// "type") and the significant-digit options in the spec property order. Mirrors test262
// PluralRules/prototype/resolvedOptions/order.js.
public class Issue824PluralRulesOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void ResolvedOptionsOrder()
    {
        var r = Eval("""
            const options = new Intl.PluralRules([], {
              minimumSignificantDigits: 1, maximumSignificantDigits: 2,
            }).resolvedOptions();
            const expected = ["locale","type","notation","minimumIntegerDigits",
              "minimumSignificantDigits","maximumSignificantDigits","pluralCategories"];
            const actual = Object.getOwnPropertyNames(options);
            let ok = actual.indexOf("locale") > -1;
            for (let i = 1; i < expected.length; i++)
              ok = ok && (actual.indexOf(expected[i-1]) < actual.indexOf(expected[i]));
            String(ok);
        """);
        Assert.Equal("true", r);
    }

    [Theory]
    [InlineData("new Intl.PluralRules('en').resolvedOptions().notation", "standard")]
    [InlineData("new Intl.PluralRules('en', { notation: 'compact' }).resolvedOptions().notation", "compact")]
    // Significant digits, when supplied, replace the fraction-digit pair in resolvedOptions.
    [InlineData("'minimumFractionDigits' in new Intl.PluralRules('en', { minimumSignificantDigits: 2 }).resolvedOptions()", "false")]
    [InlineData("'minimumSignificantDigits' in new Intl.PluralRules('en', { minimumSignificantDigits: 2 }).resolvedOptions()", "true")]
    // Default reports fraction digits.
    [InlineData("'minimumFractionDigits' in new Intl.PluralRules('en').resolvedOptions()", "true")]
    public void NotationAndDigits(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
