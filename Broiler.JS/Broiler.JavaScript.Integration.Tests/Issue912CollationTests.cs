using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression for #912 Problem 11:
// intl402/Intl/supportedValuesOf/collations-accepted-by-Collator.js — every value
// returned by Intl.supportedValuesOf("collation") must actually be resolvable by a
// Collator, and every resolvable collation must be returned (both directions).
public class Issue912CollationTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Loop 1: every supportedValuesOf("collation") value resolves for some locale.
    [Fact]
    public void EverySupportedCollationResolves()
    {
        var code = @"
            var collations = Intl.supportedValuesOf('collation');
            var locales = ['en','ar','de','es','hi','ko','ln','si','sv','zh'];
            var bad = [];
            for (var c of collations) {
              var ok = false;
              for (var loc of locales)
                if (new Intl.Collator(loc, {collation:c}).resolvedOptions().collation === c) { ok = true; break; }
              if (!ok) bad.push(c);
            }
            bad.join(',');";
        Assert.Equal("", Eval(code));
    }

    // Loop 2 (both directions): supportedValuesOf == exactly the resolvable subset of the
    // full CLDR collation list (search/standard excluded by Collator).
    [Fact]
    public void SupportedMatchesResolvableSet()
    {
        var code = @"
            var all = ['big5han','compat','dict','direct','ducet','emoji','eor','gb2312','phonebk',
                       'phonetic','pinyin','reformed','searchjl','stroke','trad','unihan','zhuyin'];
            var locales = ['en','ar','de','es','hi','ko','ln','si','sv','zh'];
            var collations = Intl.supportedValuesOf('collation');
            var mismatch = [];
            for (var c of all) {
              var ok = false;
              for (var loc of locales)
                if (new Intl.Collator(loc, {collation:c}).resolvedOptions().collation === c) { ok = true; break; }
              if (ok !== collations.includes(c)) mismatch.push(c + '(' + (ok?'resolvable':'not') + ')');
            }
            mismatch.join(',');";
        Assert.Equal("", Eval(code));
    }
}
