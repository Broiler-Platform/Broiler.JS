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
//
//   Problem 6 — ES2025 duplicate named capture groups. The engine delegates to
//   .NET Regex, which merges capturing groups that share a name into a single
//   numbered group and orders all named groups after the unnamed ones — so
//   `/(?<x>a)|(?<x>b)/.exec("bab")` produced `[b, b]` instead of
//   `[b, undefined, b]`. Whenever a pattern has any named group, every capturing
//   group is now renamed to a synthetic source-ordered name (bjsg1, bjsg2, …) so
//   .NET numbers them left-to-right like ECMAScript and keeps duplicates distinct;
//   \k<name> references resolve to the participating same-named group via a nested
//   conditional. This also fixes a latent ordering bug for any pattern mixing
//   named and unnamed groups (e.g. `/(?<x>a)(b)/`).
//   (built-ins/RegExp/named-groups/duplicate-names-{exec,match,matchall}.js.)
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

    // ---- Problem 6: ES2025 duplicate named capture groups ----

    // Renders exec() output as "[e0,e1,…] x=<groups.x>" with "u" for undefined,
    // so both the positional capture array and the resolved named group are checked.
    private const string ShowExec =
        "function show(m){ if(m===null) return 'null';"
      + " var a=[]; for(var i=0;i<m.length;i++) a.push(m[i]===undefined?'u':m[i]);"
      + " return '['+a.join(',')+']'; }";

    [Theory]
    // Duplicate name in disjoint alternatives: each alternative is its own slot.
    [InlineData("/(?<x>a)|(?<x>b)/.exec('bab')", "[b,u,b]")]
    [InlineData("/(?<x>b)|(?<x>a)/.exec('bab')", "[b,b,u]")]
    // \k<x> backreferences whichever same-named group matched.
    [InlineData("/(?:(?<x>a)|(?<x>b))\\k<x>/.exec('aa')", "[aa,a,u]")]
    [InlineData("/(?:(?<x>a)|(?<x>b))\\k<x>/.exec('abab')", "null")]
    // A backreference to a non-participating duplicate name matches the empty string.
    [InlineData("/^(?:(?<a>x)|(?<a>y)|z)\\k<a>$/.exec('z')", "[z,u,u]")]
    // Latent ordering bug: a named group preceding an unnamed one keeps source order.
    [InlineData("/(?<x>a)(b)/.exec('ab')", "[ab,a,b]")]
    [InlineData("/(z)(?<x>a)|(?<y>b)(w)/.exec('bw')", "[bw,u,u,b,w]")]
    public void DuplicateNamedGroupsPositions(string expr, string expected)
        => Assert.Equal(expected, Eval(ShowExec + "show(" + expr + ");"));

    // The resolved `groups` object exposes the participating capture per name.
    [Fact]
    public void DuplicateNamedGroupsResolvedGroupsObject()
        => Assert.Equal(
            "b|undefined",
            Eval("var m = /(?<x>a)|(?<x>b)/.exec('bab'); m.groups.x + '|' + ('y' in m.groups ? m.groups.y : 'undefined');"));

    // matchAll surfaces the same per-alternative slots across iterations.
    [Fact]
    public void DuplicateNamedGroupsMatchAll()
        => Assert.Equal(
            "a,u/x=a;u,b/x=b",
            Eval("var out = [];"
               + " for (var m of 'ab'.matchAll(/(?<x>a)|(?<x>b)/g)) {"
               + "   out.push((m[1]===undefined?'u':m[1])+','+(m[2]===undefined?'u':m[2])+'/x='+m.groups.x);"
               + " } out.join(';');"));
}
