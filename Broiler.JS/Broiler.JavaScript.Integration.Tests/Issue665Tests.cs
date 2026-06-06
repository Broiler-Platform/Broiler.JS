using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/665
//
// Fixed here:
//   * Problem 8  — Set.prototype.isDisjointFrom touched the argument's keys
//                  iterator even when this.size <= arg.size. Per spec the small
//                  side is iterated with the argument's [[Has]]; only when
//                  this.size > arg.size are the argument's keys enumerated.
//   * Problem 9  — Iterator.zip / zipKeyed with no iterators (e.g. zip([])) yielded
//                  an endless stream of empty results instead of being immediately
//                  done; zipKeyed result objects also had Object.prototype instead
//                  of a null [[Prototype]] (OrdinaryObjectCreate(null)).
//   * Problem 10 — Iterator.prototype.return marked the iterator "executing" while
//                  closing its underlying iterators. A reentrant next()/return()
//                  from an underlying iterator's return() therefore threw "Iterator
//                  is already executing"; per spec the generator is "completed"
//                  before the close, so such calls observe a done result.
//
// While reproducing Problem 9 two deeper, pre-existing engine bugs surfaced that
// blocked its harness and are fixed here as well:
//   * A generator whose loop body ends with a `yield` in tail/completion position
//     (e.g. `for (...) { yield x; }`) produced invalid IL: the generator flattener
//     left the yield's Return branch buried inside an assignment, emitting a
//     mid-expression branch across the loop back-edge.
//   * Shrinking an array's length walked every index in the removed range, so
//     `a.length = 2**32 - 1; a.length = 2` looped billions of times (a hang).
//
//   * Problem 4  — Date operations threw / returned NaN for years outside .NET's
//                  1–9999 range. The Date class now computes time values with
//                  ECMAScript date math across the full ±8.64e15 ms range (multi-arg
//                  and numeric construction, Date.UTC, the UTC getters, and the
//                  setUTCFullYear/setUTCHours setters), and parses ISO-8601 expanded
//                  years via the Broiler.DateTime submodule so toISOString output
//                  round-trips.
//
// Problems 1 (sm deepEqual harness), 2 (Intl.DateTimeFormat formatRange — needs
// CLDR), 3 (IteratorClose on abrupt completion during destructuring rest), 5
// (abrupt completion in `finally` overriding a pending throw), 6 (compound-assignment
// PutValue ordering) and 7
// (several unrelated root causes grouped by message) are triaged in the issue and
// remain out of scope for this change (architectural / harness-specific).
public class Issue665Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 8: isDisjointFrom does not touch keys() when this.size <= arg.size ----

    [Fact]
    public void IsDisjointFromSkipsKeysIteratorForSmallerReceiver()
        => Assert.Equal("true", Eval(
            "var s1 = new Set([1, 2]);"
            + "var s2 = { size: 2,"
            + " has: function(v){ return false; },"
            + " keys: function(){ throw new Error('keys must not be called'); } };"
            + "String(s1.isDisjointFrom(s2))"));

    [Fact]
    public void IsDisjointFromUsesKeysIteratorForLargerReceiver()
        => Assert.Equal("false", Eval(
            "var s1 = new Set([1, 2, 3]);"
            + "var s2 = { size: 1,"
            + " has: function(v){ throw new Error('has must not be called'); },"
            + " keys: function(){ var d=[2]; var i=0;"
            + "   return { next: function(){ return i < d.length ? { value: d[i++], done:false } : { value: undefined, done:true }; } }; } };"
            + "String(s1.isDisjointFrom(s2))"));

    [Fact]
    public void IsDisjointFromTrueWhenDisjoint()
        => Assert.Equal("true", Eval(
            "String(new Set([1, 2]).isDisjointFrom(new Set([3, 4])))"));

    // ---- Problem 9: empty zip is immediately done; zipKeyed result is null-proto ----

    [Fact]
    public void ZipWithNoIteratorsIsImmediatelyDone()
        => Assert.Equal("true,undefined", Eval(
            "var it = Iterator.zip([]); var r = it.next();"
            + "r.done + ',' + r.value"));

    [Fact]
    public void ZipKeyedWithNoIteratorsIsImmediatelyDone()
        => Assert.Equal("true,undefined", Eval(
            "var it = Iterator.zipKeyed({}); var r = it.next();"
            + "r.done + ',' + r.value"));

    [Fact]
    public void ZipKeyedResultHasNullPrototype()
        => Assert.Equal("true", Eval(
            "var it = Iterator.zipKeyed({ a: [1], b: [2] });"
            + "var r = it.next().value;"
            + "String(Object.getPrototypeOf(r) === null)"));

    [Fact]
    public void ZipYieldsRowsThenDone()
        => Assert.Equal("1-3,2-4,done", Eval(
            "var it = Iterator.zip([[1, 2], [3, 4]]);"
            + "var out = []; var x;"
            + "while (!(x = it.next()).done) out.push(x.value.join('-'));"
            + "out.push('done'); out.join(',')"));

    // ---- Problem 10: return() does not throw "already executing" reentrantly ----

    [Fact]
    public void ZipReturnFromSuspendedStartAllowsReentrantNext()
        => Assert.Equal("1,undefined,true", Eval(
            "var calls = 0;"
            + "var underlying = { next: function(){ throw new Error('next'); },"
            + " return: function(){ calls++; var r = it.next();"
            + "   return { value: r.value, done: r.done }; } };"
            + "var it = Iterator.zip([underlying]);"
            + "var res = it.return();"
            + "calls + ',' + res.value + ',' + res.done"));

    [Fact]
    public void ZipReturnFromSuspendedStartAllowsReentrantReturn()
        => Assert.Equal("1,undefined,true", Eval(
            "var calls = 0;"
            + "var underlying = { next: function(){ throw new Error('next'); },"
            + " return: function(){ calls++; var r = it.return();"
            + "   return { value: r.value, done: r.done }; } };"
            + "var it = Iterator.zip([underlying]);"
            + "var res = it.return();"
            + "calls + ',' + res.value + ',' + res.done"));

    // ---- Generator loop bodies whose completion value is a yield ----

    [Fact]
    public void GeneratorForLoopWithYieldTail()
        => Assert.Equal("012", Eval(
            "function* g(){ for (var i = 0; i < 3; ++i) { yield i; } }"
            + "var s = ''; for (var x of g()) s += x; s"));

    [Fact]
    public void GeneratorWhileLoopWithYieldTail()
        => Assert.Equal("123", Eval(
            "function* g(){ var i = 0; while (i < 3) { ++i; yield i; } }"
            + "var s = ''; for (var x of g()) s += x; s"));

    [Fact]
    public void GeneratorForOfLoopWithYieldTail()
        => Assert.Equal("123", Eval(
            "function* g(a){ for (var v of a) { yield v; } }"
            + "var s = ''; for (var x of g([1, 2, 3])) s += x; s"));

    [Fact]
    public void GeneratorNestedForLoopsWithYieldTail()
        => Assert.Equal("00 01 10 11 ", Eval(
            "function* g(){ for (var i = 0; i < 2; ++i) { for (var j = 0; j < 2; ++j) { yield i + '' + j; } } }"
            + "var s = ''; for (var x of g()) s += x + ' '; s"));

    [Fact]
    public void GeneratorForLoopWithDeclarationYieldTail()
        => Assert.Equal("a,ab", Eval(
            "function* prefixes(str){ for (var i = 1; i <= str.length; ++i) { var p = yield str.slice(0, i); } }"
            + "var out = []; for (var p of prefixes('ab')) out.push(p); out.join(',')"));

    // ---- Array length shrink from a huge sparse length must not iterate the gap ----

    [Fact]
    public void ArrayLengthGrowThenShrinkDoesNotHang()
        => Assert.Equal("a,b", Eval(
            "var a = ['a', 'b']; a.length = 4294967295; a.length = 2; a.join(',')"));

    [Fact]
    public void ArrayLengthShrinkDropsHighElements()
        => Assert.Equal("1,2|2", Eval(
            "var a = [1, 2, 3, 4, 5]; a.length = 2; a.join(',') + '|' + a.length"));

    [Fact]
    public void ArrayLengthShrinkStopsAtNonConfigurable()
        => Assert.Equal("4", Eval(
            "var a = [1, 2, 3, 4, 5];"
            + "Object.defineProperty(a, 3, { value: 99, configurable: false });"
            + "a.length = 1; String(a.length)"));

    // ---- Problem 4: Date across the full ECMAScript range (years outside 1–9999) ----

    [Fact]
    public void DateUtcSupportsExpandedYears()
        => Assert.Equal("+275760-09-13T00:00:00.000Z|-271821-04-20T00:00:00.000Z", Eval(
            "new Date(Date.UTC(275760, 8, 13)).toISOString() + '|'"
            + " + new Date(Date.UTC(-271821, 3, 20)).toISOString()"));

    [Fact]
    public void DateUtcStillHandlesNormalAndTwoDigitYears()
        => Assert.Equal("1577836800000|1999-01-01T00:00:00.000Z", Eval(
            "Date.UTC(2020, 0, 1) + '|' + new Date(Date.UTC(99, 0, 1)).toISOString()"));

    [Fact]
    public void NumericConstructorAndToIsoStringSpanFullRange()
        => Assert.Equal("+275760-09-13T00:00:00.000Z|-271821-04-20T00:00:00.000Z", Eval(
            "new Date(8.64e15).toISOString() + '|' + new Date(-8.64e15).toISOString()"));

    [Fact]
    public void ToIsoStringThrowsJustOutsideRange()
        => Assert.Equal("RangeError", Eval(
            "(function(){ try { new Date(8.64e15 + 1).toISOString(); return 'no-throw'; }"
            + "catch(e){ return e.constructor.name; } })()"));

    [Fact]
    public void UtcGettersReadExtendedDates()
        => Assert.Equal("275760-8-13", Eval(
            "var d = new Date(8.64e15);"
            + "d.getUTCFullYear() + '-' + d.getUTCMonth() + '-' + d.getUTCDate()"));

    [Fact]
    public void SetUtcFullYearAndHoursSupportExtendedYears()
        => Assert.Equal("-271821-04-20T00:00:00.000Z", Eval(
            "var d = new Date(0);"
            + "d.setUTCFullYear(-271821, 3, 20);"
            + "d.setUTCHours(0, 0, 0, 0);"
            + "d.toISOString()"));

    [Fact]
    public void SetUtcFullYearRevivesNaNReceiver()
        // Per the spec, setUTCFullYear treats a NaN time value as +0 and revives the
        // date (it does not early-return NaN). 2001-01-01T00:00:00Z is the result.
        => Assert.Equal("2001-01-01T00:00:00.000Z", Eval(
            "var d = new Date(NaN); d.setUTCFullYear(2001); d.toISOString()"));

    [Fact]
    public void ParsesExpandedYearIsoStringsRoundTrip()
        => Assert.Equal("true|true|true", Eval(
            "function rt(s){ return new Date(Date.parse(s)).toISOString() === s; }"
            + "rt('+275760-09-13T00:00:00.000Z') + '|'"
            + " + rt('-271821-04-20T00:00:00.000Z') + '|'"
            + " + rt('+010000-01-01T00:00:00.000Z')"));

    [Fact]
    public void StringConstructorParsesExpandedYears()
        => Assert.Equal("-000001-12-31T23:59:59.999Z", Eval(
            "new Date('-000001-12-31T23:59:59.999Z').toISOString()"));

    [Fact]
    public void NormalIsoAndRfc2822ParsingUnaffected()
        => Assert.Equal("2023-06-15T10:30:00.000Z|2023-06-15T10:30:00.000Z", Eval(
            "new Date('2023-06-15T10:30:00.000Z').toISOString() + '|'"
            + " + new Date('Thu, 15 Jun 2023 10:30:00 GMT').toISOString()"));
}
