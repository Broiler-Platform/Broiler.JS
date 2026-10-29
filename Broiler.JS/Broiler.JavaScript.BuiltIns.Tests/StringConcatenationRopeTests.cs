using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// A concatenation above a length threshold is deferred into a rope and copied out only when
// the characters are actually needed (docs/performance-roadmap.md P2-4), so `s = s + x` in a
// loop stops being quadratic. The rope is meant to be completely invisible: everything below
// asserts that a deferred string behaves exactly like the flat one it stands for.
public class StringConcatenationRopeTests
{
    // 20 appends of 8 characters clears the 64-character threshold with room to spare, so
    // `s` here is reliably still a pending concatenation when it is first observed.
    private const string BuildRope = "var s = ''; for (var i = 0; i < 20; i++) s = s + 'abcdefgh'; ";

    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string setup, string expression)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval("(function(){ " + setup + " return (" + expression + "); })()").ToString();
    }

    [Theory]
    [InlineData("s.length", "160")]
    [InlineData("s.charAt(0) + s.charAt(7) + s.charAt(8) + s.charAt(159)", "ahah")]
    [InlineData("s[0] + s[159]", "ah")]
    [InlineData("String(s[160])", "undefined")]
    [InlineData("s.substring(0, 8)", "abcdefgh")]
    [InlineData("s.slice(-8)", "abcdefgh")]
    [InlineData("s.indexOf('h') + ',' + s.lastIndexOf('a')", "7,152")]
    [InlineData("s.split('a').length", "21")]
    [InlineData("s.toUpperCase().slice(0, 8)", "ABCDEFGH")]
    [InlineData("s.replace('abc', 'X').slice(0, 6)", "Xdefgh")]
    [InlineData("s.match(/abc/g).length", "20")]
    [InlineData("typeof s", "string")]
    [InlineData("`${s}`.length", "160")]
    [InlineData("new String(s).length", "160")]
    [InlineData("String(s.valueOf() === s)", "true")]
    public void ADeferredStringBehavesLikeAFlatOne(string expression, string expected)
        => Assert.Equal(expected, Eval(BuildRope, expression));

    [Fact]
    public void ADeferredStringEqualsTheEagerlyBuiltOne()
        => Assert.Equal("true", Eval(
            BuildRope + "var f = ''; for (var i = 0; i < 20; i++) f = f.concat('abcdefgh');",
            "s === f"));

    [Fact]
    public void ADeferredStringRoundTripsThroughJson()
        => Assert.Equal("true", Eval(BuildRope, "JSON.parse(JSON.stringify(s)) === s"));

    [Fact]
    public void ADeferredStringWorksAsAPropertyKey()
        => Assert.Equal("1,160", Eval(
            BuildRope + "var o = {}; o[s] = 1;",
            "o[s] + ',' + Object.keys(o)[0].length"));

    [Fact]
    public void ADeferredKeyMatchesTheEquivalentFlatKey()
        => Assert.Equal("1", Eval(BuildRope + "var f = s.slice(0); var o = {}; o[s] = 1;", "o[f]"));

    [Fact]
    public void AnArrayIndexKeyIsUnaffected()
        // Below the threshold a join stays flat anyway, and an array index is at most ten
        // digits, so an index key can never itself be deferred.
        => Assert.Equal("5,5", Eval("var k = '1' + '2'; var o = {}; o[k] = 5;", "o[12] + ',' + o['12']"));

    [Fact]
    public void ALongNumericConcatenationIsAnOrdinaryStringKey()
        => Assert.Equal("7,160", Eval(
            "var k = ''; for (var i = 0; i < 20; i++) k = k + '12345678'; var o = {}; o[k] = 7;",
            "o[k] + ',' + Object.keys(o)[0].length"));

    // ── the shape this exists for ─────────────────────────────────────────────────────

    [Fact]
    public void AVeryDeepConcatenationFlattensWithoutOverflowingTheStack()
        // The pending chain is exactly as deep as the number of appends, so a recursive
        // flatten would blow the stack on precisely the workload the rope is for.
        => Assert.Equal("100000,x", Eval(
            "var s = ''; for (var i = 0; i < 100000; i++) s = s + 'x';",
            "s.length + ',' + s.charAt(99999)"));

    [Fact]
    public void JoiningTwoDeferredStringsIsCorrect()
        => Assert.Equal("320,h,1", Eval(
            "var a = ''; for (var i = 0; i < 20; i++) a = a + 'abcdefgh';" +
            "var b = ''; for (var i = 0; i < 20; i++) b = b + '12345678';" +
            "var c = a + b;",
            "c.length + ',' + c.charAt(159) + ',' + c.charAt(160)"));

    [Fact]
    public void RepeatedSelfConcatenationIsCorrect()
        => Assert.Equal("5120", Eval(
            "var s = ''; for (var i = 0; i < 20; i++) s = s + 'abcdefgh';" +
            "for (var j = 0; j < 5; j++) s = s + s;",
            "s.length"));

    [Theory]
    [InlineData("var s = ''; for (var i = 0; i < 20; i++) s += 'abcdefgh';")]
    [InlineData("var s = ''; for (var i = 0; i < 20; i++) s = s.concat('abcdefgh');")]
    public void EveryConcatenationFormProducesTheSameResult(string setup)
        => Assert.Equal("160,abcdefgh", Eval(setup, "s.length + ',' + s.slice(0, 8)"));

    // ── sharing must not leak between derived strings ─────────────────────────────────

    [Fact]
    public void ExtendingADeferredStringDoesNotDisturbIt()
        => Assert.Equal("160,164,165,TAIL,OTHER", Eval(
            "var a = ''; for (var i = 0; i < 20; i++) a = a + 'abcdefgh';" +
            "var b = a + 'TAIL'; var c = a + 'OTHER';",
            "a.length + ',' + b.length + ',' + c.length + ',' + b.slice(160) + ',' + c.slice(160)"));

    [Fact]
    public void TwoStringsSharingAPrefixStayDistinct()
        => Assert.Equal("12,160", Eval(
            "var base = ''; for (var i = 0; i < 20; i++) base = base + 'abcdefgh';" +
            "var x = base + '1'; var y = base + '2';",
            "x.charAt(160) + y.charAt(160) + ',' + base.length"));

    [Fact]
    public void ObservingTwiceIsStable()
        => Assert.Equal("160,160,aa", Eval(BuildRope, "s.length + ',' + s.length + ',' + s.charAt(0) + s.charAt(0)"));

    // ── coercion, comparison and edge cases ───────────────────────────────────────────

    [Theory]
    [InlineData("var s = 'ab' + 'cd';", "s === 'abcd'", "true")]
    [InlineData("var s = 'a' + 'b' + 'c';", "s + ',' + s.length", "abc,3")]
    [InlineData("var s = '' + '';", "s.length + ',' + (s === '')", "0,true")]
    public void ShortJoinsAreUnchanged(string setup, string expression, string expected)
        => Assert.Equal(expected, Eval(setup, expression));

    [Theory]
    [InlineData("String(s < 'b')", "true")]
    [InlineData("(s + 1).length", "161")]
    [InlineData("(1 + s).length", "161")]
    [InlineData("s ? 'truthy' : 'falsy'", "truthy")]
    public void CoercionAndComparisonSeeTheWholeString(string expression, string expected)
        => Assert.Equal(expected, Eval(BuildRope, expression));

    [Fact]
    public void ConcatenatingWithEmptyIsIdentity()
        => Assert.Equal("160,true|160,true", Eval(
            BuildRope + "var head = '' + s; var tail = s + '';",
            "head.length + ',' + (head === s) + '|' + tail.length + ',' + (tail === s)"));

    [Fact]
    public void AnEmptyStringStaysFalsy()
        => Assert.Equal("falsy", Eval("var s = '';", "s ? 'truthy' : 'falsy'"));

    [Fact]
    public void ConcatenatingAnObjectUsesItsToString()
        => Assert.Equal("161,Z", Eval(
            BuildRope + "var o = { toString: function () { return 'Z'; } }; var r = s + o;",
            "r.length + ',' + r.charAt(160)"));

    [Fact]
    public void ConcatenatingASymbolStillThrows()
        => Assert.Equal("TypeError", Eval(
            BuildRope,
            "(function () { try { return s + Symbol('x'); } catch (e) { return e.constructor.name; } })()"));
}
