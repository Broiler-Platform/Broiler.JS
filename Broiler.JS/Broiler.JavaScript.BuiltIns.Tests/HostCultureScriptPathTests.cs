using System.Globalization;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The script paths through the sites that milestone SJL-9's clause 8 fixed on the owner's ruling of 2026-11-11, "Fix all +
// enforce", answer under every host culture as under the invariant one. Each case runs under the invariant culture and
// under de-DE, sv-SE and tr-TR, applied as DecimalCultureTests applies a culture.
//
// The RegExp cases are the ones the host culture decided before that change: outside Unicode mode a pattern with the i
// flag is a .NET Regex with RegexOptions.IgnoreCase, which folded case by the current culture's equivalences, so that under
// tr-TR /I/i did not match "i" and under de-DE and sv-SE /i/i matched U+0130. The other cases pin paths whose fixed sites
// the culture could not decide (an array index or a JSON key written as text, an engine-made group name, a template's
// delimiters, a private name's "#", a BigInt's sign): they pass with the code before the change too.
public class HostCultureScriptPathTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private static T UnderCulture<T>(string culture, Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    public static TheoryData<string> Cultures => new() { "", "de-DE", "sv-SE", "tr-TR" };

    private static void Holds(string culture, string expected, string source)
        => Assert.Equal(expected, UnderCulture(culture, () => Eval(source)));

    [Theory]
    [MemberData(nameof(Cultures))]
    public void A_case_insensitive_RegExp_folds_case_as_with_the_invariant_culture(string culture)
    {
        Holds(culture, "true", "/I/i.test('i')");
        Holds(culture, "true", "/i/i.test('I')");
        Holds(culture, "false", "/I/i.test('\\u0131')");
        Holds(culture, "false", "/i/i.test('\\u0130')");
        Holds(culture, "true", "/(?i:I)b/.test('ib')");
        Holds(culture, "xbx", "'IbI'.replace(/i/gi, 'x')");
        Holds(culture, "true", "new RegExp('[A-Z]+', 'i').test('ijk')");
        Holds(culture, "true", "/I/iu.test('i')");
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void A_RegExp_s_names_and_flags_are_read_ordinally(string culture)
    {
        Holds(culture, "ab", "var g = /(?<I>a)(?<i>b)/.exec('ab').groups; g.I + g.i");
        Holds(culture, "a[I]b", "'aIb'.replace(/(?<nI>I)/, '[$<nI>]')");
        Holds(culture, "1,3", "[...'aIaI'.matchAll(/(?<x>I)/g)].map(function (m) { return m.index; }).join()");
        Holds(culture, "gimsuy", "new RegExp('I', 'gimsuy').flags");
        Holds(culture, "aa,a", "/a{0,2}/.exec('aa')[0] + ',' + /a{00,1}/.exec('aa')[0]");
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void JSON_writes_and_reads_indexes_and_keys_as_with_the_invariant_culture(string culture)
    {
        Holds(culture, "[1,[2,3],{\"a\":-1.5,\"I\":2}]", "JSON.stringify([1, [2, 3], { a: -1.5, I: 2 }])");
        Holds(culture, "[\"0:7\",\"1:8\"]", "JSON.stringify([7, 8], function (k, v) { return typeof v === 'number' ? k + ':' + v : v; })");
        Holds(culture, "[\"1\",\"-1\",\"01\",\"4294967295\",\"I\"]", "JSON.stringify(Object.keys(JSON.parse('{\"1\":1,\"-1\":2,\"01\":3,\"4294967295\":4,\"I\":5}')))");
        Holds(culture, "0|0|1|", "var ks = []; JSON.parse('[10,[20]]', function (k, v) { ks.push(k); return v; }); ks.join('|')");
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Keys_and_templates_are_written_as_with_the_invariant_culture(string culture)
    {
        Holds(culture, "[\"0\",\"1\",\"2\"][\"0\",\"2\"]", "JSON.stringify(Object.keys('abc')) + JSON.stringify(Object.keys([1, , 3]))");
        Holds(culture, "0,1,2", "var ks = []; for (var k in [5, 6, 7]) ks.push(k); ks.join()");
        Holds(culture, "[\"0\",\"1\",\"length\"]", "JSON.stringify(Reflect.ownKeys(new Proxy([1, 2], {})))");
        Holds(culture, "g1,s0,d1", "var log = []; var p = new Proxy([5, 6], { get: function (t, k) { log.push('g' + String(k)); return t[k]; }, set: function (t, k, v) { log.push('s' + String(k)); t[k] = v; return true; }, deleteProperty: function (t, k) { log.push('d' + String(k)); return delete t[k]; } }); p[1]; p[0] = 9; delete p[1]; log.join()");
        Holds(culture, "1,1.5,2,3", "[1, 2].flatMap(function (x) { return [x, x * 1.5]; }).join()");
        Holds(culture, "a|b|c", "(function (s) { return s.raw.join('|'); })`a${1}b${2}c`");
        Holds(culture, "x1.5y-2", "`x${1.5}y${-2}`");
        Holds(culture, "3", "class C { #I = 1; #i() { return 2; } static g(o) { return o.#I + o.#i(); } } C.g(new C())");
        Holds(culture, "-5,5,7", "BigInt('-5') + ',' + BigInt('+5') + ',' + BigInt(' 7 ')");
        Holds(culture, "SyntaxError", "try { BigInt('\\u22125'); 'no error' } catch (e) { e.name }");
    }
}
