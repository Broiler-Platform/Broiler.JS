using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here:
//
//   Problem 54 (Intl.PluralRules options read order) — the constructor read the digit
//   options but not the rounding options (roundingIncrement, roundingMode,
//   roundingPriority, trailingZeroDisplay) that SetNumberFormatDigitOptions mandates,
//   so those getters were never fired. They are now read (and validated) in order,
//   after the significant-digit options.
//
//   Problem 59 (Intl.Locale constructor getter order) — the constructor pre-read the
//   collation option in a validation pass (firing its getter out of order) and never
//   read the variants option at all. The spurious early collation read is removed
//   (collation is still read/validated in order), and the variants option is now read
//   after region — validated, lowercased, ordinally sorted, and applied to the tag.
public class Issue836IntlOptionOrderTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 54: PluralRules reads the rounding options, in order ----

    [Fact]
    public void PluralRulesReadsRoundingOptionsInOrder()
        => Assert.Equal(
            "localeMatcher,type,notation,minimumIntegerDigits,minimumFractionDigits,maximumFractionDigits,minimumSignificantDigits,maximumSignificantDigits,roundingIncrement,roundingMode,roundingPriority,trailingZeroDisplay",
            Eval(@"
                var actual = [];
                var options = {};
                ['localeMatcher','type','notation','minimumIntegerDigits','minimumFractionDigits',
                 'maximumFractionDigits','minimumSignificantDigits','maximumSignificantDigits',
                 'roundingIncrement','roundingMode','roundingPriority','trailingZeroDisplay'].forEach(function(name) {
                    Object.defineProperty(options, name, {
                        enumerable: true,
                        get: function() {
                            actual.push(name);
                            if (name === 'localeMatcher') return 'best fit';
                            if (name === 'type') return 'cardinal';
                            if (name === 'notation') return 'standard';
                            if (name === 'roundingMode') return 'halfExpand';
                            if (name === 'roundingPriority') return 'auto';
                            if (name === 'trailingZeroDisplay') return 'auto';
                            if (name === 'roundingIncrement') return 1;
                            return 1;
                        }
                    });
                });
                new Intl.PluralRules('en', options);
                actual.join(',');
            "));

    [Fact]
    public void PluralRulesInvalidRoundingModeThrows()
        => Assert.Equal("RangeError", Eval(@"
            var r;
            try { new Intl.PluralRules('en', { roundingMode: 'bogus' }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r;
        "));

    // ---- Problem 59: Locale getter order + variants option ----

    [Fact]
    public void LocaleReadsGettersInOrder()
        => Assert.Equal(
            "language,script,region,variants,calendar,collation,hourCycle,caseFirst,numeric,numberingSystem",
            Eval(@"
                var actual = [];
                function g(name, value) { return { toString: function() { actual.push(name); return value; } }; }
                new Intl.Locale('en', {
                    get calendar() { return g('calendar', 'gregory'); },
                    get caseFirst() { return g('caseFirst', 'upper'); },
                    get collation() { return g('collation', 'zhuyin'); },
                    get hourCycle() { return g('hourCycle', 'h24'); },
                    get language() { return g('language', 'de'); },
                    get numberingSystem() { return g('numberingSystem', 'latn'); },
                    get numeric() { actual.push('numeric'); return false; },
                    get region() { return g('region', 'DE'); },
                    get script() { return g('script', 'Latn'); },
                    get variants() { return g('variants', 'fonipa'); },
                });
                actual.join(',');
            "));

    [Fact]
    public void LocaleVariantsOptionAppliedAndSorted()
        => Assert.Equal("xx-1234-12345678-1xyz-abcde", Eval(
            "new Intl.Locale('xx', { variants: '1xyz-1234-abcde-12345678' }).toString()"));

    [Fact]
    public void LocaleVariantsOptionReplacesTagVariants()
        => Assert.Equal("en-spanglis", Eval(
            "new Intl.Locale('en-fonipa', { variants: 'spanglis' }).toString()"));

    [Fact]
    public void LocaleInvalidVariantsOptionThrows()
        => Assert.Equal("RangeError", Eval(@"
            var r;
            try { new Intl.Locale('en', { variants: 'fonipa-fonipa' }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r;
        "));
}
