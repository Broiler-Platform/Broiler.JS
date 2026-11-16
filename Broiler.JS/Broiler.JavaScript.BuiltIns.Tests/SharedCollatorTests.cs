namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.Collator on the shared collator (milestone SJL-17.5; Broiler.VM's JSD-0084): each case names the row of SJL-17.1's
// correspondence (Broiler.VM's docs/evidence/sjl-17-001/correspondence.md) it holds. A correction failed before the cut-over
// and passes after; a data case holds decision E3's list. The comparisons of the vectors (conformance/vectors/collation/) are
// Broiler.JavaScript.Vectors.Tests'; these hold what no vector reads.
public class SharedCollatorTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private static string Error(string source) => Eval($"var e = 'none'; try {{ {source}; }} catch (x) {{ e = x.name; }} e");

    // O10 (a correction): the constructor reads usage, localeMatcher, collation, numeric and caseFirst, resolves the locale,
    // then reads sensitivity and ignorePunctuation, each once.
    [Fact]
    public void The_constructor_reads_its_options_in_the_specification_s_order_each_once()
        => Assert.Equal(
            "usage,localeMatcher,collation,numeric,caseFirst,sensitivity,ignorePunctuation",
            Eval("var log = []; new Intl.Collator('en', new Proxy({}, { get: function (t, k) { log.push(String(k)); } })); log.join()"));

    // O10 (a correction): a collation that is not a type sequence is a RangeError, where it resolved default.
    [Theory]
    [InlineData("a")]
    [InlineData("abc-")]
    [InlineData("toolongtype")]
    public void A_collation_that_is_not_a_type_sequence_is_a_RangeError(string collation)
        => Assert.Equal("RangeError", Error($"new Intl.Collator('en', {{ collation: '{collation}' }})"));

    // O12 (a correction, decision E8): the bound compare is made once and kept.
    [Fact]
    public void The_bound_compare_is_made_once()
        => Assert.Equal("true", Eval("var c = new Intl.Collator('en'); String(c.compare === c.compare)"));

    // O7 and O8 (corrections): numeric and caseFirst are applied, by option, by keyword and through localeCompare.
    [Theory]
    [InlineData("new Intl.Collator('en', { numeric: true }).compare('a2', 'a10')", "-1")]
    [InlineData("'a2'.localeCompare('a10', 'en-u-kn')", "-1")]
    [InlineData("new Intl.Collator('en').compare('a2', 'a10')", "1")]
    [InlineData("new Intl.Collator('en', { caseFirst: 'upper' }).compare('a', 'A')", "1")]
    [InlineData("'a'.localeCompare('A', 'en-u-kf-upper')", "1")]
    [InlineData("new Intl.Collator('en').compare('a', 'A')", "-1")]
    public void Numeric_and_caseFirst_are_applied(string expression, string expected)
        => Assert.Equal(expected, Eval($"String({expression})"));

    // O5a (a correction): ignorePunctuation ignores spaces and punctuation, CLDR's root's variable range, and not symbols.
    // O5b (a correction): below the tertiary strength width is folded.
    [Theory]
    [InlineData("new Intl.Collator('en', { ignorePunctuation: true }).compare('a+b', 'ab')", "-1")]
    [InlineData("new Intl.Collator('en', { ignorePunctuation: true }).compare('a-b', 'ab')", "0")]
    [InlineData("new Intl.Collator('en', { sensitivity: 'base' }).compare('a\\u00A0b', 'a b')", "0")]
    [InlineData("new Intl.Collator('en', { sensitivity: 'accent' }).compare('a', '\\uFF41')", "0")]
    public void Punctuation_and_width_follow_CLDR_s_root(string expression, string expected)
        => Assert.Equal(expected, Eval($"String({expression})"));

    // O19 and O5c (decision E5): Thai's default ignorePunctuation is its tailoring's, and with ignorePunctuation false a
    // space counts, with no tie-break. Under usage search the root's search collation applies, which states no shifted
    // alternate, so the default is false (a move row O5c does not name; added 2026-11-16, at the review of SJL-17.5).
    [Fact]
    public void Thai_s_default_comes_from_its_tailoring()
    {
        Assert.Equal("true", Eval("String(new Intl.Collator('th').resolvedOptions().ignorePunctuation)"));
        Assert.Equal("false", Eval("String(new Intl.Collator('th', { usage: 'search' }).resolvedOptions().ignorePunctuation)"));
        Assert.Equal("0", Eval("String(new Intl.Collator('th').compare('', ' '))"));
        Assert.Equal("-1", Eval("String(new Intl.Collator('th', { ignorePunctuation: false }).compare('', ' '))"));
        Assert.Equal("false", Eval("String(new Intl.Collator('en').resolvedOptions().ignorePunctuation)"));
    }

    // O3 (data, decision E-o3h): Han across blocks takes UCA 17's implicit weights by block.
    [Fact]
    public void Han_across_blocks_takes_the_implicit_weights()
        => Assert.Equal("-1", Eval("String('\\u4E2D'.localeCompare('\\uD840\\uDC00', 'en'))"));

    // O16 (data, decision E3): German phonebook and search, and es's and sv's traditional collations, are the shared data's.
    [Theory]
    [InlineData("new Intl.Collator('de-u-co-phonebk').compare('\\u00E4', 'af')", "-1")]
    [InlineData("new Intl.Collator('de').compare('\\u00E4', 'af')", "-1")]
    [InlineData("new Intl.Collator('de', { usage: 'search' }).compare('AE', '\\u00C4')", "-1")]
    [InlineData("new Intl.Collator('sv').compare('\\u00E4', 'z')", "1")]
    [InlineData("new Intl.Collator('es', { collation: 'trad' }).resolvedOptions().collation", "trad")]
    public void The_listed_tailorings_are_the_shared_data_s(string expression, string expected)
        => Assert.Equal(expected, Eval($"String({expression})"));

    // O11b and JSD-0082's decision D33 (c): a collation the shared data does not carry stays the host provider's, so en keeps
    // a collation (test262 Locale/prototype/getCollations/output-array-values.js) and de keeps eor
    // (resolved-collation-unicode-extensions-and-options.js); supportedValuesOf lists the same nine collations.
    [Fact]
    public void The_collations_the_shared_data_does_not_carry_stay_listed()
    {
        Assert.Equal("[\"emoji\",\"eor\"]", Eval("JSON.stringify(new Intl.Locale('en').getCollations())"));
        Assert.Equal("[\"emoji\",\"eor\",\"phonebk\"]", Eval("JSON.stringify(new Intl.Locale('de').getCollations())"));
        Assert.Equal("de eor", Eval("var r = new Intl.Collator('de-u-co-phonebk', { collation: 'eor' }).resolvedOptions(); r.locale + ' ' + r.collation"));
        Assert.Equal("en default", Eval("var r = new Intl.Collator('en-u-co-phonebk', { collation: 'pinyin' }).resolvedOptions(); r.locale + ' ' + r.collation"));
        Assert.Equal(
            "[\"emoji\",\"eor\",\"phonebk\",\"phonetic\",\"pinyin\",\"stroke\",\"trad\",\"unihan\",\"zhuyin\"]",
            Eval("JSON.stringify(Intl.supportedValuesOf('collation'))"));
    }

    // The keys co, kf and kn resolve through the shared ResolveLocale: a keyword the data supports is kept, in canonical
    // order, and an option that differs from it drops it.
    [Theory]
    [InlineData("en-u-kn-kf-upper", "{}", "en-u-kf-upper-kn")]
    [InlineData("en-u-kn", "{ numeric: false }", "en")]
    [InlineData("en-u-kf-upper", "{ caseFirst: 'upper' }", "en-u-kf-upper")]
    [InlineData("de-u-co-phonebk", "{ collation: 'phonebk' }", "de-u-co-phonebk")]
    public void The_relevant_keys_resolve_through_the_shared_library(string locale, string options, string resolved)
        => Assert.Equal(resolved, Eval($"new Intl.Collator('{locale}', {options}).resolvedOptions().locale"));
}
