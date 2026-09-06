using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Roadmap item 3-0: `a[i]` where the compiler holds `i` in a raw double reads the element from
// that double instead of boxing a JSNumber purely to name a slot.
//
// The fast path is only legal because of its guard, so the guard is what these pin. Only a
// non-negative integral double at most 2^32-2 names an array index; every other double is an
// ordinary string-keyed property and has to keep resolving through ToPropertyKey. A guard that
// is too strict costs an allocation and never a wrong answer, so the failure mode worth testing
// is the opposite one — an index fast path swallowing a key it had no right to.
public class NumericIndexKeyTests
{
    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    // Every value below is assigned to `i` first so the analysis sees a numeric local and the
    // fast path is actually the thing under test; a literal index would lower to a constant uint
    // and prove nothing.
    private const string Fixture = """
        var a = [];
        a[0] = 'zero';
        a[1] = 'one';
        a[4294967294] = 'max';
        a['1.5'] = 'frac';
        a['-1'] = 'neg';
        a['NaN'] = 'nan';
        a['Infinity'] = 'inf';
        a['4294967295'] = 'above';
        var i = 0;
        """;

    [Theory]
    [InlineData("1", "one", "an ordinary index")]
    [InlineData("1.5", "frac", "a fractional key is the STRING '1.5', not index 1")]
    [InlineData("-1", "neg", "a negative key is the string '-1'")]
    [InlineData("-0", "zero", "ToString(-0) is '0', so -0 names slot 0")]
    [InlineData("0/0", "nan", "NaN is the string 'NaN'")]
    [InlineData("1/0", "inf", "Infinity is the string 'Infinity'")]
    [InlineData("4294967294", "max", "2^32-2 is the largest array index")]
    [InlineData("4294967295", "above", "2^32-1 is NOT an array index; it is a string key")]
    [InlineData("2", "undefined", "absent")]
    public void ANumericLocalIndexResolvesTheSameKeyABoxedOneWould(string value, string expected, string why)
    {
        Assert.Equal(expected, Eval($"{Fixture} i = {value}; String(a[i]);"));
        _ = why;
    }

    [Fact(Timeout = 600000)]
    public void AHoleStillReachesThePrototypeChain()
    {
        // The fast path must not answer "absent" itself — a missing element is an ordinary
        // [[Get]] that continues up the chain.
        Assert.Equal(
            "inherited",
            Eval("""
                var a = [0, , 2];
                Object.prototype[1] = 'inherited';
                var i = 1;
                var r = a[i];
                delete Object.prototype[1];
                String(r);
                """));
    }

    [Fact(Timeout = 600000)]
    public void AnIndexAccessorStillRuns()
    {
        Assert.Equal(
            "getter",
            Eval("""
                var o = {};
                Object.defineProperty(o, 3, { get: function () { return 'getter'; }, configurable: true });
                var i = 3;
                String(o[i]);
                """));
    }

    [Theory]
    [InlineData("1", "b")]
    [InlineData("5", "undefined")]
    [InlineData("1.5", "undefined")]
    public void StringIndexingIsUnchanged(string index, string expected)
        => Assert.Equal(expected, Eval($"var s = 'abc'; var i = 0; i = {index}; String(s[i]);"));

    [Theory]
    [InlineData("2", "30")]
    [InlineData("1.5", "undefined")]
    [InlineData("-1", "undefined")]
    public void TypedArrayIndexingIsUnchanged(string index, string expected)
        => Assert.Equal(
            expected,
            Eval($"var t = new Int32Array([10, 20, 30]); var i = 0; i = {index}; String(t[i]);"));

    [Fact(Timeout = 600000)]
    public void AProxyStillSeesTheGetTrap()
    {
        // The fast path calls [[Get]] on the receiver, so an exotic receiver must be entirely
        // unaffected — including that the trap receives the key as a string.
        Assert.Equal(
            "proxied!|7",
            Eval("""
                var seen = null;
                var p = new Proxy({ 7: 'proxied' }, {
                    get: function (t, k) { seen = k; return k in t ? t[k] + '!' : undefined; }
                });
                var i = 7;
                p[i] + '|' + seen;
                """));
    }

    [Fact(Timeout = 600000)]
    public void ANonCanonicalNumericStringIsADifferentPropertyAndStaysThatWay()
    {
        Assert.Equal("x|y", Eval("var o = { '0': 'x', '00': 'y' }; var i = 0; o[i] + '|' + o['00'];"));
    }

    [Fact(Timeout = 600000)]
    public void ReadsSeeEveryUpdateToTheIndexInOrder()
    {
        Assert.Equal("a,b,c", Eval("var a = ['a','b','c']; var out = []; for (var i = 0; i < 3; i++) out.push(a[i]); out.join(',');"));
    }

    [Fact(Timeout = 600000)]
    public void AnIndexReadInAnOptionalChainIsUnaffected()
    {
        // Excluded from the fast path on purpose: inside a chain the key evaluation stays lifted
        // into the gated branch. Pinned so the exclusion is not quietly removed.
        Assert.Equal("b|undefined", Eval("var a = ['a','b']; var n = null; var i = 1; a?.[i] + '|' + String(n?.[i]);"));
    }

    [Fact(Timeout = 600000)]
    public void WritesThroughANumericLocalIndexStillLandOnTheSameKey()
    {
        // The agreement test between the two halves: a write and a read through the same numeric
        // local must name the same property, including for the keys the guard rejects.
        Assert.Equal(
            "1:one|1.5:frac|-1:neg",
            Eval("""
                var a = [];
                var i = 0;
                i = 1;    a[i] = 'one';
                i = 1.5;  a[i] = 'frac';
                i = -1;   a[i] = 'neg';
                i = 1;    var r1 = a[i];
                i = 1.5;  var r2 = a[i];
                i = -1;   var r3 = a[i];
                '1:' + r1 + '|1.5:' + r2 + '|-1:' + r3;
                """));
    }

    // ── the write half ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1", "1", "an ordinary index")]
    [InlineData("1.5", "1.5", "the string key '1.5', not index 1")]
    [InlineData("-1", "-1", "the string key '-1'")]
    [InlineData("-0", "0", "ToString(-0) is '0'")]
    [InlineData("0/0", "NaN", "the string key 'NaN'")]
    [InlineData("1/0", "Infinity", "the string key 'Infinity'")]
    [InlineData("4294967294", "4294967294", "the largest array index")]
    [InlineData("4294967295", "4294967295", "a string key, not an index")]
    public void AWriteLandsOnTheKeyToStringWouldProduce(string value, string expectedKey, string why)
    {
        // Read back through the STRING form rather than through the numeric local, so the write
        // is checked against the key the spec names rather than against the read fast path
        // agreeing with a shared bug.
        Assert.Equal(
            "written",
            Eval($"var o = {{}}; var i = 0; i = {value}; o[i] = 'written'; String(o['{expectedKey}']);"));
        _ = why;
    }

    [Fact(Timeout = 600000)]
    public void AWriteEvaluatesToTheAssignedValue()
        => Assert.Equal("7", Eval("var a = []; var i = 3; String(a[i] = 7);"));

    [Fact(Timeout = 600000)]
    public void AWriteExtendsLength()
        => Assert.Equal("4", Eval("var a = []; var i = 3; a[i] = 'x'; String(a.length);"));

    [Fact(Timeout = 600000)]
    public void AnIndexSetterStillRuns()
    {
        Assert.Equal(
            "5",
            Eval("""
                var seen = 0;
                var o = {};
                Object.defineProperty(o, 2, { set: function (v) { seen = v; }, configurable: true });
                var i = 2;
                o[i] = 5;
                String(seen);
                """));
    }

    [Fact(Timeout = 600000)]
    public void AFrozenArrayRefusesTheWriteSilentlyInSloppyMode()
        => Assert.Equal("1", Eval("var a = [1]; Object.freeze(a); var i = 0; a[i] = 99; String(a[0]);"));

    [Fact(Timeout = 600000)]
    public void AFrozenArrayThrowsInStrictMode()
    {
        var message = Eval("""
            'use strict';
            var a = [1];
            Object.freeze(a);
            var i = 0;
            var caught = 'none';
            try { a[i] = 99; } catch (e) { caught = e.name; }
            caught + ':' + a[0];
            """);

        Assert.Equal("TypeError:1", message);
    }

    [Fact(Timeout = 600000)]
    public void AProxyStillSeesTheSetTrapWithAStringKey()
    {
        Assert.Equal(
            "9|4",
            Eval("""
                var seen = null;
                var p = new Proxy({}, {
                    set: function (t, k, v) { seen = k; t[k] = v; return true; }
                });
                var i = 4;
                p[i] = 9;
                p[4] + '|' + seen;
                """));
    }

    [Theory]
    [InlineData("1", "99")]
    [InlineData("1.5", "20")]
    [InlineData("-1", "20")]
    public void TypedArrayWritesAreUnchanged(string index, string expectedAtOne)
    {
        // An out-of-range or non-canonical index on a typed array is silently discarded, and in
        // particular must not land on some other slot.
        Assert.Equal(
            expectedAtOne,
            Eval($"var t = new Int32Array([10, 20, 30]); var i = 0; i = {index}; t[i] = 99; String(t[1]);"));
    }

    [Fact(Timeout = 600000)]
    public void ANullOrUndefinedReceiverStillReportsTheVariableIndexMessage()
    {
        // A constant index has always reported through the this[uint] override ("Cannot set
        // property 0 of …") and a variable one through the JSValue setter ("Cannot set properties
        // of …"). 3-0 must not swap a variable index onto the other message; that pre-existing
        // difference is not this item's to reconcile.
        //
        // The setter arm has since gained the "(setting '0')" clause a browser appends — it names
        // the key the assignment was for, which is the half this message never said. The two
        // WORDINGS are still distinct, which is what this test is here to hold.
        Assert.Equal(
            "Cannot set properties of null (setting '0')|Cannot set properties of undefined (setting '0')",
            Eval("""
                var i = 0;
                var n = null;
                var u;
                var a = 'x', b = 'y';
                try { n[i] = 1; } catch (e) { a = e.message; }
                try { u[i] = 1; } catch (e) { b = e.message; }
                a + '|' + b;
                """));
    }

    [Fact(Timeout = 600000)]
    public void ACompoundAssignmentThroughANumericLocalIndexIsUnaffected()
    {
        // Excluded on purpose: its target is read and written through one reference. Pinned so
        // the exclusion is not quietly removed, and so the base is still evaluated once.
        Assert.Equal(
            "3|1",
            Eval("""
                var calls = 0;
                function base() { calls++; return a; }
                var a = [1];
                var i = 0;
                base()[i] += 2;
                a[0] + '|' + calls;
                """));
    }

    [Fact(Timeout = 600000)]
    public void TheReceiverAndKeyAreEvaluatedBeforeTheRightHandSide()
    {
        // §13.15.2 order. The RHS reassigns the index, which must not affect where the value
        // lands, because the key was already read.
        Assert.Equal(
            "at2:set|at9:undefined",
            Eval("""
                var a = [];
                var i = 2;
                a[i] = (function () { i = 9; return 'set'; })();
                'at2:' + a[2] + '|at9:' + a[9];
                """));
    }
}
