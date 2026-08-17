using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/Broiler-Platform/Broiler.JS/issues/944 — the two array
// timeout clusters, both caused by a length being clamped into the 32-bit array-index range and the
// resulting multi-billion-iteration loop being run instead of the spec's early throw.
//
//  - Array.prototype.slice clamped `count` to uint.MaxValue before ArraySpeciesCreate, so a count
//    above the 2^32-1 array-index limit became a four-billion-element copy instead of the RangeError
//    ArrayCreate owes it. Mirrors test262 Array/prototype/slice/S15.4.4.10_A3_T1, _A3_T2 and
//    create-proxied-array-invalid-len.
//  - Array.prototype.unshift read the length as a uint (2^32-1) rather than through ToLength
//    (2^53-1), ran the shift loop even with no arguments to insert, and tested the 2^53-1 overflow
//    with wrapping uint arithmetic — so `4294967295 + 1` came out as 0 and never tripped the guard.
//    Per §23.1.3.35 the overflow is a TypeError and steps 4.a-4.d are skipped entirely when there
//    are no arguments. Mirrors test262 Array/prototype/unshift/{clamps-to-integer-limit,
//    length-near-integer-limit,throws-if-integer-limit-exceeded} and staging/sm/Array/unshift-01.
public class Issue944ArrayLengthLimitTests
{
    private static string Eval(string source)
    {
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // Returns the constructor name of the error thrown by evaluating expr, or "no throw".
    private static string ThrownErrorName(string expr) => Eval($$"""
        (function () {
            try { {{expr}}; return "no throw"; }
            catch (e) { return e && e.constructor ? e.constructor.name : String(e); }
        })()
        """);

    // ---- Array.prototype.slice: count above the array-index limit -------------------------------

    // test262 Array/prototype/slice/S15.4.4.10_A3_T1 and _A3_T2.
    [Theory(Timeout = 60000)]
    [InlineData(4294967296L)]
    [InlineData(4294967297L)]
    public void SliceRejectsCountAboveTheArrayIndexLimit(long length)
        => Assert.Equal("RangeError", ThrownErrorName($$"""
            (function () {
                var obj = {};
                obj.slice = Array.prototype.slice;
                obj[0] = "x";
                obj.length = {{length}};
                obj.slice(0, {{length}});
            })()
            """));

    // test262 Array/prototype/slice/create-proxied-array-invalid-len — the RangeError comes out of
    // array creation, so the proxy's set trap is never reached.
    [Fact(Timeout = 60000)]
    public void SliceOverProxiedInvalidLengthThrowsBeforeAnyWrite()
        => Assert.Equal("RangeError|0", Eval("""
            (function () {
                var array = [];
                var callCount = 0;
                var proxy = new Proxy(array, {
                    get: function (_, name) { return name === 'length' ? Math.pow(2, 32) : array[name]; },
                    set: function () { callCount += 1; return true; }
                });
                var name;
                try { Array.prototype.slice.call(proxy); name = 'no throw'; }
                catch (e) { name = e.constructor.name; }
                return name + '|' + callCount;
            })()
            """));

    // A count within the limit is unaffected, including the sparse/array-like paths.
    [Fact(Timeout = 60000)]
    public void SliceWithinTheLimitIsUnchanged()
        => Assert.Equal("2,3|3,4|3|false|c|5", Eval("""
            (function () {
                var sparse = { slice: Array.prototype.slice, length: 3, 0: 'a', 2: 'c' };
                var r = sparse.slice(0);
                var big = { slice: Array.prototype.slice, length: 4294967295 };
                return [
                    [1, 2, 3, 4].slice(1, 3).join(','),
                    [1, 2, 3, 4].slice(-2).join(','),
                    r.length,
                    1 in r,
                    r[2],
                    big.slice(4294967290).length
                ].join('|');
            })()
            """));

    // ---- Array.prototype.unshift: ToLength, not a 32-bit clamp ----------------------------------

    // test262 Array/prototype/unshift/clamps-to-integer-limit — a zero-argument call reports (and
    // stores) the ToLength-clamped 2^53-1 without iterating.
    [Theory(Timeout = 60000)]
    [InlineData("Math.pow(2, 53) - 1")]
    [InlineData("Math.pow(2, 53)")]
    [InlineData("Math.pow(2, 53) + 2")]
    [InlineData("Infinity")]
    public void UnshiftWithNoArgumentsClampsLengthToIntegerLimit(string length)
        => Assert.Equal("9007199254740991|9007199254740991", Eval($$"""
            (function () {
                var arrayLike = {};
                arrayLike.length = {{length}};
                var result = Array.prototype.unshift.call(arrayLike);
                return result + '|' + arrayLike.length;
            })()
            """));

    // test262 Array/prototype/unshift/throws-if-integer-limit-exceeded — a resulting length past
    // 2^53-1 is a TypeError, and it is raised before anything is shifted.
    [Theory(Timeout = 60000)]
    [InlineData("Math.pow(2, 53) - 1")]
    [InlineData("Math.pow(2, 53)")]
    [InlineData("Math.pow(2, 53) + 2")]
    [InlineData("Infinity")]
    public void UnshiftThrowsTypeErrorWhenIntegerLimitExceeded(string length)
        => Assert.Equal("TypeError", ThrownErrorName($$"""
            (function () {
                var arrayLike = {};
                arrayLike.length = {{length}};
                Array.prototype.unshift.call(arrayLike, null);
            })()
            """));

    // test262 Array/prototype/unshift/length-near-integer-limit — the shift walks DOWN from the
    // length, so it touches four properties and then the getter throws. Under the old 32-bit clamp
    // it started four billion indices too low and never reached them.
    [Fact(Timeout = 60000)]
    public void UnshiftNearIntegerLimitTouchesOnlyTheTopProperties()
        => Assert.Equal("StopUnshift|9007199254740990|9007199254740987|9007199254740987|false|9007199254740989|9007199254740991", Eval("""
            (function () {
                function StopUnshift() {}
                var arrayLike = {
                    get "9007199254740986"() { throw new StopUnshift(); },
                    "9007199254740987": "9007199254740987",
                    "9007199254740989": "9007199254740989",
                    "9007199254740991": "9007199254740991",
                    length: Math.pow(2, 53) - 2
                };
                var thrown;
                try { Array.prototype.unshift.call(arrayLike, null); thrown = 'no throw'; }
                catch (e) { thrown = e.constructor.name; }
                return [
                    thrown,
                    arrayLike.length,
                    arrayLike["9007199254740987"],
                    arrayLike["9007199254740988"],
                    "9007199254740989" in arrayLike,
                    arrayLike["9007199254740990"],
                    arrayLike["9007199254740991"]
                ].join('|');
            })()
            """));

    // test262 staging/sm/Array/unshift-01 — the length setter observes the clamped value, and
    // ToLength's coercions (string, null, negative) are unchanged.
    [Fact(Timeout = 60000)]
    public void UnshiftReportsToLengthThroughTheLengthSetter()
        => Assert.Equal("0|10|1|0|9007199254740991|0", Eval("""
            (function () {
                function seen(len) {
                    var newlen;
                    var a = { get length() { return len; }, set length(v) { newlen = v; } };
                    var res = Array.prototype.unshift.call(a);
                    return res === newlen ? String(res) : res + '/' + newlen;
                }
                return [
                    seen(0),
                    seen(10),
                    seen("1"),
                    seen(null),
                    seen(Math.pow(2, 53) + 2),
                    seen(-5)
                ].join('|');
            })()
            """));

    // Ordinary unshift behaviour — element shifting, holes, the returned length and the
    // non-writable-length TypeError — is unchanged.
    [Fact(Timeout = 60000)]
    public void UnshiftWithinTheLimitIsUnchanged()
        => Assert.Equal("4|0,1,2,3|3|1,2,3|true|false|4|za|2|4294967295", Eval("""
            (function () {
                var a = [1, 2, 3];
                var pushed = a.unshift(0);
                var b = [1, 2, 3];
                var empty = b.unshift();
                var hole = [1, , 3];
                hole.unshift('x');
                var like = { 0: 'a', length: 1 };
                Array.prototype.unshift.call(like, 'z');
                return [
                    pushed, a.join(','),
                    empty, b.join(','),
                    1 in hole, 2 in hole, hole.length,
                    like[0] + like[1], like.length,
                    Array.prototype.unshift.call({ length: 4294967295 })
                ].join('|');
            })()
            """));

    [Fact(Timeout = 60000)]
    public void UnshiftOnAFrozenArrayStillThrowsTypeError()
        => Assert.Equal("TypeError", ThrownErrorName("var a = [1, 2]; Object.freeze(a); a.unshift(0)"));

    // ---- Array.prototype.toReversed: ArrayCreate enforces the array-index limit ------------------

    // test262 Array/prototype/toReversed/length-exceeding-array-length-limit — the RangeError comes
    // from creating the result, so none of the element getters run.
    [Fact(Timeout = 60000)]
    public void ToReversedRejectsLengthAboveTheArrayIndexLimit()
        => Assert.Equal("RangeError|0", Eval("""
            (function () {
                var reads = 0;
                var arrayLike = {
                    get "0"() { reads += 1; return 0; },
                    get "4294967295"() { reads += 1; return 0; },
                    get "4294967296"() { reads += 1; return 0; },
                    length: Math.pow(2, 32)
                };
                var name;
                try { Array.prototype.toReversed.call(arrayLike); name = 'no throw'; }
                catch (e) { name = e.constructor.name; }
                return name + '|' + reads;
            })()
            """));

    // Reversal within the limit is unchanged, holes included (toReversed uses Get, so a hole
    // becomes undefined rather than staying a hole).
    [Fact(Timeout = 60000)]
    public void ToReversedWithinTheLimitIsUnchanged()
        => Assert.Equal("4,3,2,1|3|true|c,,a", Eval("""
            (function () {
                var sparse = { length: 3, 0: 'a', 2: 'c' };
                var r = Array.prototype.toReversed.call(sparse);
                return [
                    [1, 2, 3, 4].toReversed().join(','),
                    r.length,
                    1 in r,
                    r.join(',')
                ].join('|');
            })()
            """));

    // ---- Array.prototype.reduceRight: ToLength, not a 32-bit clamp -------------------------------

    // test262 Array/prototype/reduceRight/length-near-integer-limit — the walk starts at len-1, so
    // it reaches the two present properties near 2^53 immediately.
    [Fact(Timeout = 60000)]
    public void ReduceRightVisitsElementsNearTheIntegerLimit()
        => Assert.Equal("2|1,9007199254740990|3,9007199254740988", Eval("""
            (function () {
                var max = Number.MAX_SAFE_INTEGER;
                var arrayLike = { length: max };
                arrayLike[max - 1] = 1;
                arrayLike[max - 3] = 3;
                var acc;
                try {
                    Array.prototype.reduceRight.call(arrayLike, function (a, el, index) {
                        a.push([el, index]);
                        if (el === 3) throw a;
                        return a;
                    }, []);
                    return 'should not be called';
                } catch (e) { acc = e; }
                return [acc.length, acc[0].join(','), acc[1].join(',')].join('|');
            })()
            """));

    // test262 staging/sm/Array/to-length — ToLength(Infinity) is 2^53-1, so the callback is reached
    // rather than the walk starting four billion indices too low.
    [Fact(Timeout = 60000)]
    public void ReduceRightOverInfiniteLengthReachesTheCallback()
        => Assert.Equal("invoked", Eval("""
            (function () {
                var max = Number.MAX_SAFE_INTEGER;
                var arrayLike = { length: Infinity };
                arrayLike[max - 1] = undefined;
                arrayLike[max - 2] = undefined;
                try { Array.prototype.reduceRight.call(arrayLike, function () { throw 'invoked'; }); }
                catch (e) { return e; }
                return 'no throw';
            })()
            """));

    // Ordinary reduceRight behaviour — direction, index argument, the missing-initial-value
    // TypeError and hole skipping — is unchanged.
    [Fact(Timeout = 60000)]
    public void ReduceRightWithinTheLimitIsUnchanged()
        => Assert.Equal("4,3,2,1|y:1,x:0|10|TypeError|c,a", Eval("""
            (function () {
                var order = [];
                [1, 2, 3, 4].reduceRight(function (acc, el) { order.push(el); return acc; }, 0);
                var indices = [];
                ['x', 'y'].reduceRight(function (acc, el, i) { indices.push(el + ':' + i); return acc; }, 0);
                var noInitial;
                try { [].reduceRight(function () { return 0; }); noInitial = 'no throw'; }
                catch (e) { noInitial = e.constructor.name; }
                var holes = [];
                Array.prototype.reduceRight.call({ length: 3, 0: 'a', 2: 'c' }, function (acc, el) { holes.push(el); return acc; }, 0);
                return [
                    order.join(','),
                    indices.join(','),
                    [1, 2, 3, 4].reduceRight(function (acc, el) { return acc + el; }, 0),
                    noInitial,
                    holes.join(',')
                ].join('|');
            })()
            """));
}
