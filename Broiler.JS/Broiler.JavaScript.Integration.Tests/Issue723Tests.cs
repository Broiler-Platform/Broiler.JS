using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/723
//
// Fixed here:
//
//   Problems 1 & 2 — Intl.DurationFormat.prototype.formatToParts dropped the
//   `unit` property from every part. Per ECMA-402 PartitionDurationFormatPattern,
//   each part produced from a unit's NumberFormat carries the singular unit name
//   ("hour", "minute", …); only the time separators and the surrounding
//   Intl.ListFormat literals omit it. The test262 fixtures compare against a
//   reference partitionDurationFormatPattern and assert both that
//   `"unit" in part` matches and that the unit value matches, so the missing
//   property failed `unit for entry 0` for both the negative-duration and the
//   plain formatToParts style fixtures.
//   (intl402/DurationFormat/prototype/formatToParts/*-en.js.)
public class Issue723Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code).ToString();
    }

    // Helper: project formatToParts output into "type:value:unit" triples, using
    // "<none>" when the part carries no own `unit` property.
    private const string Project =
        "function show(parts){return parts.map(function(p){"
        + "return p.type+':'+p.value+':'+(('unit' in p)?p.unit:'<none>');}).join('|');}";

    // ---- Problems 1 & 2: formatToParts carries the per-unit `unit` property ----

    // The number parts of each unit get the singular unit name; the ListFormat
    // separator (", ") between elements stays unit-less.
    [Fact]
    public void FormatToPartsAttachesUnitToNumberParts()
        => Assert.Equal(
            "integer:1:hour|literal: :hour|unit:hr:hour|literal:, :<none>|integer:30:minute|literal: :minute|unit:min:minute",
            Eval(Project
               + "show(new Intl.DurationFormat('en').formatToParts({ hours: 1, minutes: 30 }));"));

    // Every part that is not a list separator must own a `unit`; the separator
    // between the two elements must not. (Mirrors the `"unit" in part` assertion
    // that was failing as `unit for entry 0`.)
    [Fact]
    public void FormatToPartsUnitPresenceMatchesReference()
        => Assert.Equal(
            "true|true|true|true|true|true|true",
            Eval("var parts = new Intl.DurationFormat('en').formatToParts({ hours: 1, minutes: 30 });"
               + " parts.map(function(p){ return p.type === 'literal' && p.value === ', '"
               + "   ? ('unit' in p) === false : ('unit' in p) === true; }).join('|');"));

    // Numeric (digital) elements: the colon time separators inside an element stay
    // unit-less while the integer parts keep their unit.
    [Fact]
    public void FormatToPartsNumericTimeSeparatorIsUnitLess()
        => Assert.Equal(
            "integer:1:hour|literal:::<none>|integer:02:minute|literal:::<none>|integer:03:second",
            Eval(Project
               + "show(new Intl.DurationFormat('en', { hours: 'numeric' }).formatToParts({ hours: 1, minutes: 2, seconds: 3 }));"));

    // ---- Problem 9: supportedLocalesOf coerces a primitive options via ToObject ----

    // A primitive options argument is boxed (ToObject) so that an inherited
    // localeMatcher accessor on Object.prototype is read exactly once per call —
    // four primitive flavours (boolean, string, number, symbol) each trigger it.
    // The fix returns "1|1|1|1"; the bug returned "0|0|0|0".
    [Theory]
    [InlineData("Intl.ListFormat")]
    [InlineData("Intl.RelativeTimeFormat")]
    [InlineData("Intl.Segmenter")]
    public void SupportedLocalesOfCoercesPrimitiveOptions(string ctor)
        => Assert.Equal(
            "1|1|1|1",
            Eval(
                "var counts = [];"
              + "[true, 'test', 7, Symbol()].forEach(function(opt){"
              + "  var n = 0;"
              + "  Object.defineProperty(Object.prototype, 'localeMatcher', {"
              + "    get: function(){ n++; return undefined; }, configurable: true });"
              + "  try { " + ctor + ".supportedLocalesOf([], opt); }"
              + "  finally { delete Object.prototype.localeMatcher; }"
              + "  counts.push(n);"
              + "}); counts.join('|');"));
}
