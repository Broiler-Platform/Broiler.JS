using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// A locale's default numbering system is CLDR's (JSIntlDefaultNumberingSystems, generated from CLDR
// 47.0.0), where every locale defaulted to "latn"; and a Temporal value's digits follow the formatter's
// numbering system, as a Date's do. Found by test262's
// intl402/DateTimeFormat/prototype/format/temporal-objects-no-time-clip-non-latin-numerals.js, which
// passed before only because String.prototype.includes compared by culture (correction X2, SJL-5.5).
public class DefaultNumberingSystemTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("ar-EG", "arab")]
    [InlineData("ar-SA", "arab")]
    [InlineData("ar", "latn")]
    [InlineData("ar-MA", "latn")]
    [InlineData("fa", "arabext")]
    [InlineData("fa-IR", "arabext")]
    [InlineData("bn", "beng")]
    [InlineData("mr", "deva")]
    [InlineData("my", "mymr")]
    [InlineData("ks", "arabext")]
    [InlineData("ks-Deva", "latn")]
    [InlineData("en", "latn")]
    [InlineData("ar-EG-u-nu-latn", "latn")]
    public void A_locale_resolves_to_CLDR_s_default_numbering_system(string locale, string expected)
    {
        Assert.Equal(expected, Eval($"new Intl.NumberFormat('{locale}').resolvedOptions().numberingSystem"));
        Assert.Equal(expected, Eval($"new Intl.DateTimeFormat('{locale}').resolvedOptions().numberingSystem"));
    }

    [Fact]
    public void The_default_numbering_system_writes_the_digits()
        => Assert.Equal("١٬٢٣٤", Eval("new Intl.NumberFormat('ar-EG').format(1234)"));

    [Fact]
    public void An_option_overrides_the_default()
        => Assert.Equal("1234", Eval("new Intl.NumberFormat('ar-EG', { numberingSystem: 'latn', useGrouping: false }).format(1234)"));

    [Fact]
    public void A_locale_s_numbering_systems_start_with_its_default()
        => Assert.Equal("arab", Eval("new Intl.Locale('ar-EG').getNumberingSystems()[0]"));

    // The Temporal path took the engine's ASCII digits as they were: ar-EG-u-nu-arab wrote a Date in
    // Arabic-Indic digits and a Temporal.PlainDate in Latin ones.
    [Theory]
    [InlineData("ar-EG", "new Temporal.PlainDate(275760, 9, 13)", "١٣")]
    [InlineData("ar-EG", "new Temporal.PlainDate(2020, 9, 13)", "١٣")]
    [InlineData("en-u-nu-arab", "new Temporal.PlainDate(2020, 9, 13)", "١٣")]
    [InlineData("ar-EG", "new Date(Date.UTC(2020, 8, 13, 12))", "١٣")]
    public void A_date_s_digits_follow_the_numbering_system(string locale, string value, string day)
    {
        var options = "{ year: 'numeric', month: 'numeric', day: 'numeric', timeZone: 'UTC', calendar: 'iso8601' }";
        Assert.Equal("true", Eval($"new Intl.DateTimeFormat('{locale}', {options}).format({value}).includes('{day}')"));
        Assert.Equal("true", Eval(
            $"new Intl.DateTimeFormat('{locale}', {options}).formatToParts({value}).find(p => p.type === 'day').value === '{day}'"));
    }
}
