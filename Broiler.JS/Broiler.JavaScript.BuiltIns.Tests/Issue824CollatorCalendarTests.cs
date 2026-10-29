using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824: collator / calendar canonicalization.
//  - 51: Intl.Locale applies the calendar option and canonicalizes the keyword value
//        (islamicc → islamic-civil).
//  - 34: a missing boolean Unicode-extension value defaults to true and the resolved Collator locale
//        carries just "-kn" (not "-kn-true"/"-kn-false").
//  - 87: String.prototype.localeCompare orders the same as Intl.Collator.
public class Issue824CollatorCalendarTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // 51 — calendar option canonicalization.
    [Theory]
    [InlineData("new Intl.Locale('en', { calendar: 'islamicc' }).toString()", "en-u-ca-islamic-civil")]
    [InlineData("new Intl.Locale('en', { calendar: 'islamicc' }).calendar", "islamic-civil")]
    [InlineData("new Intl.Locale('en', { calendar: 'gregory' }).toString()", "en-u-ca-gregory")]
    [InlineData("new Intl.Locale('en-US', { calendar: 'islamicc' }).toString()", "en-US-u-ca-islamic-civil")]
    // Other Unicode-extension options apply too.
    [InlineData("new Intl.Locale('en', { numeric: true }).toString()", "en-u-kn")]
    [InlineData("new Intl.Locale('en', { numeric: true }).numeric", "true")]
    [InlineData("new Intl.Locale('en', { caseFirst: 'upper' }).toString()", "en-u-kf-upper")]
    [InlineData("new Intl.Locale('en', { numberingSystem: 'arab' }).toString()", "en-u-nu-arab")]
    // Keywords are sorted by key when several options are supplied.
    [InlineData("new Intl.Locale('en', { numeric: true, calendar: 'gregory' }).toString()", "en-u-ca-gregory-kn")]
    public void CalendarAndKeywordOptions(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    // 34 — Collator boolean default for kn.
    [Theory]
    [InlineData("-u-co-phonebk-kn")]
    [InlineData("-u-kn-co-phonebk")]
    [InlineData("-u-co-phonebk-kn-true")]
    [InlineData("-u-kn-true-co-phonebk")]
    public void CollatorNumericDefaultsToTrue(string extension)
    {
        var r = Eval($$"""
            const base = new Intl.Collator().resolvedOptions().locale;
            const c = new Intl.Collator([base + "{{extension}}"], { usage: "sort" });
            const locale = c.resolvedOptions().locale;
            const numeric = c.resolvedOptions().numeric;
            [numeric, locale.indexOf("-kn-false"), locale.indexOf("-kn-true"), locale.indexOf("-kn") >= 0].join(",");
        """);
        Assert.Equal("true,-1,-1,true", r);
    }

    // 87 — localeCompare matches Collator for every locale/option combination.
    [Fact]
    public void LocaleCompareMatchesCollator()
    {
        var r = Eval("""
            const strings = ["d","O","od","oe","of","ö","ö","X","y","Z","Z.","𠮷野家","吉野家","!A","A","b","C"];
            const locales = [undefined, ["de"], ["de-u-co-phonebk"], ["en"], ["ja"], ["sv"]];
            const options = [undefined, {usage:"search"}, {sensitivity:"base", ignorePunctuation:true}];
            const problems = [];
            for (const loc of locales)
              for (const opt of options) {
                const col = new Intl.Collator(loc, opt);
                const a = strings.slice().sort(col.compare);
                const b = strings.slice().sort((x,y) => x.localeCompare(y, loc, opt));
                if (a.join("") !== b.join("")) problems.push(String(loc) + "/" + JSON.stringify(opt));
              }
            problems.length === 0 ? "ok" : problems.join(" | ");
        """);
        Assert.Equal("ok", r);
    }
}
