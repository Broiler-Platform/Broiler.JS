using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Small integers are minted once per thread and reused rather than allocated fresh
// (docs/performance-roadmap.md P2-2). A JavaScript number has no observable identity — it
// compares by value and cannot carry properties — so a shared instance is supposed to be
// completely indistinguishable from a fresh one.
//
// Two things could give it away, and both are covered here. The value side: negative zero
// is `== 0` under IEEE but distinguishable through Object.is and 1/x, so it must not
// collapse onto the cached zero, and nothing outside the cached range may be captured by
// accident. The realm side: a JSPrimitive resolves its wrapper prototype from the CURRENT
// realm into a scratch field, so a reused instance must still see the realm doing the
// asking — which is why the cache is per-thread and why GetMethod had to stop treating that
// field as a cache.
public class SmallNumberCacheTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // ── negative zero must not collapse onto zero ─────────────────────────────────────

    [Theory]
    [InlineData("Object.is(-0, 0)", "false")]
    [InlineData("Object.is(-0, -0)", "true")]
    [InlineData("Object.is(0, 0)", "true")]
    [InlineData("1 / -0", "-Infinity")]
    [InlineData("1 / 0", "Infinity")]
    [InlineData("1 / (0 - 0)", "Infinity")]
    [InlineData("1 / (0 * -1)", "-Infinity")]
    [InlineData("1 / Math.round(-0.4)", "-Infinity")]
    [InlineData("1 / [-0][0]", "-Infinity")]
    [InlineData("1 / JSON.parse('-0')", "-Infinity")]
    [InlineData("Object.is(-0, 0 * -1)", "true")]
    public void NegativeZeroStaysDistinct(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ");"));

    [Fact]
    public void NegativeZeroSurvivesRepeatedCreation()
        // Whatever the cache does, the thousandth `-0` must be as negative as the first.
        => Assert.Equal("true", Eval("""
            var ok = true;
            for (var i = 0; i < 1000; i++) { var z = i * -0; if (1 / z !== -Infinity) ok = false; }
            String(ok);
            """));

    [Fact]
    public void NegativeZeroNormalizesAsAPropertyKey()
        // -0 as a key is the string "0" per ToPropertyKey, which is unrelated to the cache
        // but is the other place the two zeroes could be confused.
        => Assert.Equal("0,1", Eval("var o = {}; o[-0] = 1; Object.keys(o).join(',') + ',' + o[0];"));

    // ── only exact small integers may be captured ─────────────────────────────────────

    [Theory]
    [InlineData("0.5 + 0.25", "0.75")]
    [InlineData("(1 / 3).toFixed(3)", "0.333")]
    [InlineData("100.5", "100.5")]
    [InlineData("-0.5", "-0.5")]
    [InlineData("1e21", "1e+21")]
    [InlineData("Math.pow(2, 53) - 1", "9007199254740991")]
    [InlineData("-1e21", "-1e+21")]
    [InlineData("0 / 0", "NaN")]
    [InlineData("1 / 0", "Infinity")]
    [InlineData("-1 / 0", "-Infinity")]
    public void ValuesOutsideTheRangeAreUnchanged(string expression, string expected)
        => Assert.Equal(expected, Eval("String(" + expression + ");"));

    [Fact]
    public void NaNIsNeverEqualToItself()
        => Assert.Equal("false,true,1", Eval(
            "var n = 0 / 0; (n === n) + ',' + Object.is(n, n) + ',' + new Set([0 / 0, 0 / 0]).size;"));

    [Theory]
    // The boundaries of the cached range, and one step past each, all still arithmetic.
    [InlineData("-129", "-129")]
    [InlineData("-128", "-128")]
    [InlineData("-127", "-127")]
    [InlineData("1023", "1023")]
    [InlineData("1024", "1024")]
    [InlineData("1025", "1025")]
    public void TheRangeBoundariesAreArithmeticallyCorrect(string literal, string expected)
        => Assert.Equal(expected + "," + expected, Eval(
            "var a = " + literal + "; var b = (" + literal + " - 1) + 1; (a) + ',' + (b);"));

    [Fact]
    public void EveryIntegerAcrossTheBoundaryRoundTrips()
        => Assert.Equal("true", Eval("""
            var ok = true;
            for (var i = -200; i <= 1200; i++) {
                if ((i + 0) !== i || String(i) !== String(i + 0) || (i - 1) + 1 !== i) ok = false;
            }
            String(ok);
            """));

    [Fact]
    public void SummingTheWholeRangeIsExact()
        => Assert.Equal("516544", Eval("var s = 0; for (var i = -128; i <= 1024; i++) s += i; String(s);"));

    // ── a reused instance is not observable ───────────────────────────────────────────

    [Fact]
    public void APrimitiveStillRejectsProperties()
        => Assert.Equal("undefined,undefined", Eval(
            "var a = 5; a.tag = 1; var b = 5; String(a.tag) + ',' + String(b.tag);"));

    [Fact]
    public void APrimitiveStillThrowsOnStrictAssignment()
        => Assert.Equal("TypeError", Eval("""
            'use strict';
            (function () { var n = 5; try { n.tag = 1; return 'no-throw'; } catch (e) { return e.constructor.name; } })();
            """));

    [Fact]
    public void BoxingStillProducesDistinctWrappers()
        => Assert.Equal("false,false,1,undefined", Eval("""
            var a = new Number(5), b = new Number(5);
            a.tag = 1;
            (Object(5) === Object(5)) + ',' + (a === b) + ',' + a.tag + ',' + String(b.tag);
            """));

    [Theory]
    [InlineData("var m = new Map(); m.set(5, 'a'); m.set(5, 'b'); m.size + ',' + m.get(5);", "1,b")]
    [InlineData("var m = new Map(); m.set(-0, 'a'); m.set(0, 'b'); m.size + ',' + m.get(-0);", "1,b")]
    [InlineData("new Set([1, 1, 2, 2, 3]).size;", "3")]
    [InlineData("new Set([0, -0]).size;", "1")]
    public void KeyedCollectionsStillUseSameValueZero(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Fact]
    public void MethodsOnASmallIntegerStillWork()
        => Assert.Equal("5.00,ff,1010,true,5", Eval(
            "(5).toFixed(2) + ',' + (255).toString(16) + ',' + (10).toString(2) + ',' " +
            "+ ((5).constructor === Number) + ',' + (5).valueOf();"));

    [Fact]
    public void PatchingNumberPrototypeReachesCachedInstances()
        => Assert.Equal("42,600,-14", Eval("""
            Number.prototype.twice = function () { return this * 2; };
            (21).twice() + ',' + (300).twice() + ',' + (-7).twice();
            """));

    // ── the realm the number is used in is the one that answers ───────────────────────

    [Fact]
    public void EachRealmSeesItsOwnNumberPrototype()
    {
        // The regression this exists for: JSPrimitive.GetMethod used to fill the prototype
        // scratch field only when it was still null, so whichever realm looked a method up
        // first on a shared instance owned it for every realm afterwards. With small
        // integers reused, that would have made `(5).who()` answer for the wrong realm.
        Load();
        using var first = new JSContext();
        using var second = new JSContext();
        first.Eval("Number.prototype.who = function () { return 'first'; };");
        second.Eval("Number.prototype.who = function () { return 'second'; };");

        // Interleaved, so each realm touches the same cached instances after the other.
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal("first,first,first", first.Eval("(5).who() + ',' + (0).who() + ',' + (1000).who();").ToString());
            Assert.Equal("second,second,second", second.Eval("(5).who() + ',' + (0).who() + ',' + (1000).who();").ToString());
        }
    }

    [Fact]
    public void EachRealmSeesItsOwnPrototypeThroughDynamicAccessToo()
    {
        Load();
        using var first = new JSContext();
        using var second = new JSContext();
        first.Eval("Number.prototype.tag = 'first';");
        second.Eval("Number.prototype.tag = 'second';");

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal("first,first", first.Eval("var k = 'tag'; (7)[k] + ',' + (7).tag;").ToString());
            Assert.Equal("second,second", second.Eval("var k = 'tag'; (7)[k] + ',' + (7).tag;").ToString());
        }
    }

    [Fact]
    public void ARealmWithoutTheMethodStillReportsItMissing()
        => Assert.Equal("function,undefined", Eval("""
            Number.prototype.only = function () { return 1; };
            typeof (5).only + ',' + typeof (5).neverDefined;
            """));

    // ── numbers still flow through the rest of the engine ─────────────────────────────

    [Theory]
    [InlineData("JSON.stringify({ a: 0, b: -0, c: 5, d: 1024, e: 1.5 });", "{\"a\":0,\"b\":0,\"c\":5,\"d\":1024,\"e\":1.5}")]
    [InlineData("JSON.stringify([0, -0, 1023, 1025]);", "[0,0,1023,1025]")]
    [InlineData("[300, 5, -7, 1024, 0].sort(function (x, y) { return x - y; }).join(',');", "-7,0,5,300,1024")]
    [InlineData("[1, 2, 3].indexOf(2) + ',' + [NaN].indexOf(NaN) + ',' + [NaN].includes(NaN);", "1,-1,true")]
    [InlineData("(5 & 3) + ',' + (1 << 10) + ',' + (~5) + ',' + (-8 >>> 28);", "1,1024,-6,15")]
    [InlineData("Math.min(3, 5) + ',' + Math.abs(-7) + ',' + Math.floor(5.9);", "3,7,5")]
    [InlineData("parseInt('42') + ',' + parseFloat('3.5') + ',' + Number('7');", "42,3.5,7")]
    [InlineData("'A'.charCodeAt(0) + ',' + String.fromCharCode(65);", "65,A")]
    [InlineData("typeof [1, 2].length + ',' + [1, 2].length;", "number,2")]
    [InlineData("(5 == '5') + ',' + (5 === '5') + ',' + ('5' - 0 === 5);", "true,false,true")]
    public void OrdinaryNumericBehaviourIsUnchanged(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Fact]
    public void ArrayIndicesAndValuesStayConsistent()
        => Assert.Equal("true", Eval("""
            var a = [];
            for (var i = 0; i < 1200; i++) a[i] = i;
            var ok = a.length === 1200;
            for (var i = 0; i < 1200; i++) if (a[i] !== i) ok = false;
            String(ok);
            """));

    [Fact]
    public void IncrementAndDecrementRoundTrip()
        => Assert.Equal("5,5,1030,1030", Eval("""
            var i = 5; i++; ++i; i--; --i;
            var j = 1030; j++; ++j; j--; --j;
            i + ',' + i + ',' + j + ',' + j;
            """));
}
