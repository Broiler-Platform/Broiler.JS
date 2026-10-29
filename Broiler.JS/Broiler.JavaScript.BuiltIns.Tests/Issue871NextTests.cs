using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #871 (second batch): three more test262 script-host clusters.
//  - A structurally invalid language tag with a duplicate extension singleton (case-insensitive),
//    e.g. "de-DE-u-kn-true-U-kn-true" or "pt-u-ca-gregory-u-nu-latn", must be rejected with a
//    RangeError by every Intl constructor / getCanonicalLocales. Mirrors test262
//    intl402/language-tags-{invalid,with-underscore}.js and Intl/getCanonicalLocales/invalid-tags.js.
//  - Math.sumPrecise must be correctly rounded and overflow-aware (the naive/Neumaier sum returned
//    NaN for inputs that overflow intermediately). Mirrors test262 Math/sumPrecise/sum.js.
//  - A well-formed but out-of-range ISO monthCode ("M00", "M13") must be a RangeError even with the
//    default overflow:"constrain"; it is never constrained. Mirrors test262
//    Temporal/PlainDate/prototype/with/overflow.js and
//    Temporal/PlainYearMonth/from/calendarresolvefields-error-ordering.js.
public class Issue871NextTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(source).ToString();
    }

    private static string ThrownErrorName(string expr) => Eval($$"""
        (function () {
            try { {{expr}}; return "no throw"; }
            catch (e) { return e && e.constructor ? e.constructor.name : String(e); }
        })()
        """);

    // ---- Cluster 1: duplicate singleton subtag rejection --------------------------------------

    [Theory]
    [InlineData("Intl.getCanonicalLocales('de-DE-u-kn-true-U-kn-true')")]
    [InlineData("Intl.getCanonicalLocales('pt-u-ca-gregory-u-nu-latn')")]
    [InlineData("new Intl.Collator('pt-u-ca-gregory-u-nu-latn')")]
    [InlineData("new Intl.Locale('de-DE-u-kn-true-U-kn-true')")]
    public void RejectsDuplicateSingletonSubtag(string expr)
        => Assert.Equal("RangeError", ThrownErrorName(expr));

    [Theory]
    // Distinct singletons (u / t / x) and repeated single-char subtags inside private-use are valid.
    [InlineData("Intl.getCanonicalLocales('en-u-ca-gregory-t-en-x-foo')[0]", "en-t-en-u-ca-gregory-x-foo")]
    [InlineData("Intl.getCanonicalLocales('en-x-a-a')[0]", "en-x-a-a")]
    [InlineData("Intl.getCanonicalLocales('en-US-u-ca-gregory')[0]", "en-US-u-ca-gregory")]
    public void AcceptsValidSingletons(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    // ---- Cluster 2: Math.sumPrecise correctly-rounded / overflow-aware ------------------------

    [Theory]
    [InlineData("Math.sumPrecise([1, 2, 3])", "6")]
    [InlineData("Math.sumPrecise([0.1, 0.2])", "0.30000000000000004")]
    [InlineData("Math.sumPrecise([1e308, -1e308])", "0")]
    [InlineData("Math.sumPrecise([1e308, 1e308, 0.1, 0.1, 1e30, 0.1, -1e30, -1e308, -1e308])", "0.30000000000000004")]
    // Intermediate overflow that cancels back into range (was NaN before the fix).
    [InlineData("Math.sumPrecise([8.98846567431158e+307, 8.988465674311579e+307, -1.7976931348623157e+308])", "9.9792015476736e+291")]
    // Rounds to the maximum finite double rather than overflowing.
    [InlineData("Math.sumPrecise([-2.534858246857893e+115, 8.988465674311579e+307, 8.98846567431158e+307])", "1.7976931348623157e+308")]
    [InlineData("Math.sumPrecise([8.98846567431158e+307, 8.98846567431158e+307])", "Infinity")]
    public void SumPreciseIsCorrectlyRounded(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    [Fact]
    public void SumPreciseEmptyIsNegativeZero()
        => Assert.Equal("-Infinity", Eval("String(1 / Math.sumPrecise([]))"));

    [Theory]
    [InlineData("Math.sumPrecise([1, NaN, 2])", "NaN")]
    [InlineData("Math.sumPrecise([Infinity, -Infinity])", "NaN")]
    public void SumPreciseNonFinite(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    [Theory]
    [InlineData("Math.sumPrecise([1, '2'])", "TypeError")]
    [InlineData("Math.sumPrecise(5)", "TypeError")]
    public void SumPreciseRejectsBadInput(string expr, string expected)
        => Assert.Equal(expected, ThrownErrorName(expr));

    // ---- Cluster 3: out-of-range ISO monthCode is a RangeError --------------------------------

    [Theory]
    [InlineData("Temporal.PlainDate.from('2020-05-15').with({ monthCode: 'M00' })")]
    [InlineData("Temporal.PlainDate.from('2020-05-15').with({ monthCode: 'M13' })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2021, monthCode: 'M00' })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2021, monthCode: 'M99L' })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2021, month: 11, monthCode: 'M12' })")]
    public void RejectsOutOfRangeMonthCode(string expr)
        => Assert.Equal("RangeError", ThrownErrorName(expr));

    [Fact]
    public void MissingYearThrowsTypeErrorBeforeMonthCodeRangeError()
        => Assert.Equal("TypeError", ThrownErrorName("Temporal.PlainYearMonth.from({ monthCode: 'M99L' })"));

    [Theory]
    [InlineData("Temporal.PlainDate.from('2020-05-15').with({ monthCode: 'M07' }).toString()", "2020-07-15")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2021, monthCode: 'M11' }).toString()", "2021-11")]
    [InlineData("Temporal.PlainDate.from({ year: 2020, monthCode: 'M12', day: 1 }).toString()", "2020-12-01")]
    public void AcceptsValidMonthCode(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
