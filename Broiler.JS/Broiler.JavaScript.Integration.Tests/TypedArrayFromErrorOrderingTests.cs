using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for %TypedArray%.from error-ordering and validation
// (test262 test/staging/sm/TypedArray/from_errors.js and the immutable-arraybuffer
// custom-constructor case). See ES2024 23.2.2.1.
public class TypedArrayFromErrorOrderingTests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // from() / from(undefined) / from(null) throw a TypeError (GetMethod's ToObject step).
    [Theory]
    [InlineData("Int8Array.from()")]
    [InlineData("Int8Array.from(undefined)")]
    [InlineData("Int8Array.from(null)")]
    public void FromNullishSourceThrowsTypeError(string call)
    {
        var code = "var t='no throw'; try { " + call + "; } catch (e) { t = e.constructor.name; } t;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // A non-undefined, non-callable mapfn throws a TypeError BEFORE the source is
    // observed in any way (no @@iterator lookup, no length read, no element read).
    [Fact]
    public void NonCallableMapfnThrowsBeforeTouchingSource()
    {
        var code = @"
            var touched = '';
            var src = new Proxy({}, {
                has: () => { touched += 'h'; return false; },
                get: () => { touched += 'g'; return undefined; },
                getOwnPropertyDescriptor: () => { touched += 'd'; return undefined; }
            });
            var threw = '';
            try { Int8Array.from(src, {}); } catch (e) { threw = e.constructor.name; }
            threw + '|' + touched;";
        Assert.Equal("TypeError|", Eval(code).ToString());
    }

    // mapfn provided as null is non-callable -> TypeError (only undefined disables mapping).
    [Fact]
    public void NullMapfnThrowsTypeError()
    {
        var code = "var t=''; try { Int8Array.from([1,2,3], null); } catch (e) { t = e.constructor.name; } t;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // A primitive (non-nullish) @@iterator is a TypeError; a null/undefined
    // @@iterator falls back to the array-like path (here: length 0 -> empty).
    [Theory]
    [InlineData("Int8Array.from({[Symbol.iterator]: 17})", "throw")]
    [InlineData("Int8Array.from({[Symbol.iterator]: 'x'})", "throw")]
    [InlineData("Int8Array.from({[Symbol.iterator]: null}).length", "0")]
    [InlineData("Int8Array.from({[Symbol.iterator]: undefined}).length", "0")]
    public void IteratorPropertyValidation(string expr, string expected)
    {
        var code = "var r; try { r = '' + (" + expr + "); } catch (e) { r = e instanceof TypeError ? 'throw' : 'other'; } r;";
        Assert.Equal(expected, Eval(code).ToString());
    }

    // An iterator whose next() returns a primitive is a TypeError.
    [Fact]
    public void IteratorNextReturningPrimitiveThrows()
    {
        var code = @"
            var src = { [Symbol.iterator]() { return { next() { return 5; } }; } };
            var t=''; try { Int8Array.from(src); } catch (e) { t = e.constructor.name; } t;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // For an array-like source the result is constructed (via the receiver C)
    // BEFORE any element is visited: the call order is length, construct, element.
    [Fact]
    public void ResultConstructedBeforeArrayLikeElementsVisited()
    {
        var code = @"
            var log = [];
            function C(len) { log.push('construct'); return new Int8Array(len); }
            C.from = Int8Array.from;
            var src = {
                get length() { log.push('length'); return 1; },
                get 0() { log.push('elem0'); return 7; }
            };
            C.from(src, function(v){ return v; });
            log.join(',');";
        Assert.Equal("length,construct,elem0", Eval(code).ToString());
    }
}
