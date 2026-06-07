using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/697
//
// Fixed here:
//
// Problem 8 — encodeURI / encodeURIComponent did not raise URIError on an
//   unpaired surrogate code unit. They delegated to .NET's Uri.EscapeUriString /
//   Uri.EscapeDataString, which silently re-encode (or drop) lone surrogates and
//   never throw, and whose unescaped-character sets do not match the spec's. The
//   abstract Encode operation (§19.2.6.5) is now implemented directly: code units
//   in the function-specific unescaped set are copied verbatim, everything else is
//   UTF-8 encoded and percent-escaped, and an unpaired high or low surrogate is a
//   URIError. (encodeURI/S15.1.3.3_A1.2/A1.3 and the encodeURIComponent siblings.)
//
// Problem 10 (subset) — RegExp.prototype[Symbol.matchAll] read the source
//   regexp's flags by inspecting each individual flag accessor (hasIndices,
//   global, …) instead of `Get(R, "flags")`, and copied `lastIndex` without
//   ToLength coercion. So a `flags` getter, a `flags` value whose toString throws,
//   or a `lastIndex` valueOf that throws never fired — the abrupt completion the
//   spec requires (steps 5 and 7) was lost. The JSRegExp fast path now reads
//   `R.flags` (ToString) and `R.lastIndex` (ToLength) observably.
public class Issue697Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // Run `source`, reporting the thrown error's constructor name or "ok".
    private static string Catch(string source)
        => Eval("var r; try { " + source + " r = 'ok'; } catch (e) { r = e.constructor.name; } r;").ToString();

    // ---- Problem 8: encodeURI/encodeURIComponent reject unpaired surrogates ----

    [Theory]
    [InlineData("encodeURI('\\uD800')")]            // lone high surrogate
    [InlineData("encodeURI('\\uDC00')")]            // lone low surrogate
    [InlineData("encodeURI('\\uD800\\u0041')")]     // high not followed by low
    [InlineData("encodeURIComponent('a\\uD834b')")] // lone high surrogate, mid-string
    [InlineData("encodeURIComponent('\\uDFFF')")]   // lone low surrogate
    public void EncodeRejectsUnpairedSurrogate(string expr)
        => Assert.Equal("URIError", Catch(expr + ";"));

    // A valid surrogate pair is UTF-8 encoded, not rejected.
    [Fact]
    public void EncodeURIEncodesValidSurrogatePair()
        => Assert.Equal("%F0%9F%98%80", Eval("encodeURI('\\uD83D\\uDE00');").ToString());

    // encodeURI leaves the reserved/unescaped marks intact.
    [Fact]
    public void EncodeURIPreservesReservedCharacters()
        => Assert.Equal("http://a.b/c%20d?e=1#f", Eval("encodeURI('http://a.b/c d?e=1#f');").ToString());

    // encodeURIComponent escapes the reserved characters that encodeURI keeps.
    [Fact]
    public void EncodeURIComponentEscapesReserved()
        => Assert.Equal("a%20b%2Fc%3Fd%3D1", Eval("encodeURIComponent('a b/c?d=1');").ToString());

    // ---- Problem 10: matchAll propagates abrupt completions ----

    // A throwing `flags` getter on the receiver propagates.
    [Fact]
    public void MatchAllPropagatesFlagsGetterThrow()
        => Assert.Equal("Error", Catch(
            "var re = /./; Object.defineProperty(re, 'flags', { get() { throw new Error(); } });" +
            " re[Symbol.matchAll]('');"));

    // A `flags` value whose toString throws propagates (valueOf must not be called).
    [Fact]
    public void MatchAllPropagatesFlagsToStringThrow()
        => Assert.Equal("Error", Catch(
            "var re = /\\w/; Object.defineProperty(re, 'flags', { value: { toString() { throw new Error(); } } });" +
            " re[Symbol.matchAll]('');"));

    // A `lastIndex` whose valueOf throws propagates (ToLength coercion).
    [Fact]
    public void MatchAllPropagatesLastIndexValueOfThrow()
        => Assert.Equal("Error", Catch(
            "var re = /./; re.lastIndex = { valueOf() { throw new Error(); } };" +
            " re[Symbol.matchAll]('');"));

    // Ordinary matchAll still yields the expected matches.
    [Fact]
    public void MatchAllStillIterates()
        => Assert.Equal("a:0,a:1", Eval(
            "var r = []; for (var m of 'aabb'.matchAll(/(a)/g)) r.push(m[0] + ':' + m.index); r.join(',');").ToString());
}
