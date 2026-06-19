using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845 — three further
// clusters:
//   * Set.prototype.symmetricDifference toggles each `other` key against THIS set's live
//     data (Problem 26).
//   * A built-in function's toString omits a property name that is not a valid
//     NativeFunction IdentifierName (the legacy RegExp "$&"-style statics, Problem 53).
//   * %TypedArray%.prototype.copyWithin skips an out-of-bounds index after a shrink but
//     still completes the in-bounds part of an overlap-reversed copy (Problem 24).
public class Issue845ClusterTests3
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Set.prototype.symmetricDifference (Problem 26) ----

    [Theory]
    [InlineData("[...new Set([1,2,3]).symmetricDifference(new Set([2,3,4]))].join(',')", "1,4")]
    [InlineData("[...new Set(['a','b','c']).symmetricDifference(new Set(['b','c','d']))].join(',')", "a,d")]
    // A duplicate key in `other` must not re-add / re-order a value.
    [InlineData("[...new Set([1,2]).symmetricDifference(new Set([3,3,1]))].join(',')", "2,3")]
    public void SymmetricDifference(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void SymmetricDifferenceTogglesAgainstLiveReceiver()
    {
        // A set-like whose keys() iterator mutates the receiver mid-iteration; the toggle
        // is against the receiver's live data, so the result order matches the spec.
        const string code = @"
            const baseSet = new Set(['a','b','c','d','e']);
            function mutatingIterator(){
              let index = 0; let values = ['x','b','c','c'];
              return { next(){ if (index===0){ baseSet.delete('b'); baseSet.delete('c'); baseSet.add('b'); baseSet.add('d'); }
                return { done: index >= values.length, value: values[index++] }; } };
            }
            const evil = { size: 4, get has(){ baseSet.add('q'); return function(){ throw new Error('has'); }; }, keys(){ return mutatingIterator(); } };
            [...baseSet.symmetricDifference(evil)].join(',');";
        Assert.Equal("a,c,d,e,q,x", Eval(code));
    }

    // ---- Built-in function toString NativeFunction syntax (Problem 53) ----

    [Theory]
    // Legacy RegExp statics whose names are not valid IdentifierNames omit the name.
    [InlineData("Object.getOwnPropertyDescriptor(RegExp, '$&').get.toString()", "function get () { [native code] }")]
    [InlineData("Object.getOwnPropertyDescriptor(RegExp, \"$'\").get.toString()", "function get () { [native code] }")]
    // Valid identifier names are preserved.
    [InlineData("Object.getOwnPropertyDescriptor(RegExp, '$_').get.toString()", "function get $_() { [native code] }")]
    [InlineData("Object.getOwnPropertyDescriptor(RegExp.prototype, 'source').get.toString()", "function get source() { [native code] }")]
    [InlineData("parseInt.toString()", "function parseInt() { [native code] }")]
    public void NativeFunctionToString(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void NativeAccessorKeepsItsNameProperty()
        => Assert.Equal("get $&", Eval("Object.getOwnPropertyDescriptor(RegExp, '$&').get.name"));

    // ---- TypedArray copyWithin (Problem 24) ----

    [Theory]
    // Plain (non-resizable) behaviour is unchanged, including the overlap-reversed copy.
    [InlineData("new Int8Array([1,2,3,4,5]).copyWithin(0,3).join(',')", "4,5,3,4,5")]
    [InlineData("new Int8Array([1,2,3,4,5]).copyWithin(1,3).join(',')", "1,4,5,4,5")]
    public void CopyWithinPlain(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // Shrinking the buffer during argument coercion: the copy is clamped to the live
    // length, but the in-bounds part of an overlap-reversed copy still runs.
    [InlineData("3", "{ valueOf(){ rab.resize(3); return 2; } }", "1", "0,1,1")] // overlapping forward
    [InlineData("3", "0", "{ valueOf(){ rab.resize(3); return 2; } }", "2,1,2")] // truncated backward
    public void CopyWithinShrinkDuringCoercion(string newLen, string target, string start, string expected)
    {
        var code = $@"
            const rab = new ArrayBuffer(4, {{ maxByteLength: 8 }});
            const ta = new Int8Array(rab);
            for (let i = 0; i < 4; ++i) ta[i] = i;
            ta.copyWithin({target}, {start});
            Array.from(ta).join(',');";
        Assert.Equal(expected, Eval(code));
    }
}
