using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Intl;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The plural rules of a regional variety are its own wherever a service selects a plural (SJL-9.5, task 8.4, a review
// fix, 2026-11-08): CLDR's pt-PT cardinal "one" is "i = 1 and v = 0", where pt's is "i = 0..1", so 0 and 1.5 are
// "other" in pt-PT. Intl.PluralRules, Intl.NumberFormat and Intl.RelativeTimeFormat's own [[PluralRules]] all read the
// rules along the data locale's parent chain (DataLocalePluralData); RelativeTimeFormat read pt's before the fix.
// And JSIntlListFormat's public constructor refuses a type or style that is not an option value.
public class IntlDataLocalePluralRulesTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("0", "other")]
    [InlineData("1", "one")]
    [InlineData("1.5", "other")]
    public void PluralRules_selects_by_pt_PT_s_rules(string value, string expected)
        => Assert.Equal(expected, Eval($"new Intl.PluralRules('pt-PT').select({value})"));

    [Theory]
    [InlineData("0", "dentro de 0 dias")]
    [InlineData("1", "dentro de 1 dia")]
    [InlineData("1.5", "dentro de 1,5 dias")]
    public void RelativeTimeFormat_selects_by_pt_PT_s_rules(string value, string expected)
        => Assert.Equal(expected, Eval($"new Intl.RelativeTimeFormat('pt-PT').format({value}, 'day')"));

    [Fact]
    public void RelativeTimeFormat_keeps_pt_s_rules_for_pt()
        => Assert.Equal("em 0 dia", Eval("new Intl.RelativeTimeFormat('pt').format(0, 'day')"));

    [Fact]
    public void The_ListFormat_constructor_takes_the_option_values()
    {
        Load();
        using var ctx = new JSContext();
        var format = new JSIntlListFormat("en", "disjunction", "short");
        Assert.NotNull(format);
    }

    [Theory]
    [InlineData("and", "long", "type")]
    [InlineData("conjunction", "wide", "style")]
    public void The_ListFormat_constructor_refuses_another_value(string type, string style, string parameter)
    {
        Load();
        using var ctx = new JSContext();
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() => new JSIntlListFormat("en", type, style));
        Assert.Equal(parameter, thrown.ParamName);
    }
}
