using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/794 — Problems 5/16 (argument /
// option validation ordering).
//
//   * ArrayBuffer / SharedArrayBuffer ({ maxByteLength }) throw the byteLength-vs-maxByteLength
//     RangeError BEFORE creating the object (before reading new.target.prototype).
//   * DataView validates byteOffset against the buffer length BEFORE reading new.target.prototype.
//   * Atomics.wait / waitAsync / notify capture the typed-array length BEFORE coercing the index,
//     so growing the (growable shared) buffer in an index `valueOf` does not widen the bounds check,
//     and a RangeError is thrown without coercing the value / timeout arguments.
public class Issue794OrderingTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // A constructor invoked via Reflect.construct with a new.target whose `prototype` getter throws
    // must still report the argument-validation RangeError (the prototype is read only after a
    // successful construction, so its getter is never reached).
    private const string ThrowingNewTarget =
        "var nt = Object.defineProperty(function(){}.bind(null), 'prototype', " +
        "{ get() { throw new EvalError('prototype-read'); } });";

    [Theory]
    [InlineData("Reflect.construct(ArrayBuffer, [10, { maxByteLength: 0 }], nt)")]
    [InlineData("Reflect.construct(SharedArrayBuffer, [10, { maxByteLength: 0 }], nt)")]
    [InlineData("Reflect.construct(DataView, [new ArrayBuffer(0), 10], nt)")]
    public void Validation_Precedes_PrototypeRead(string construct)
        => Assert.Equal("RangeError", Eval(
            ThrowingNewTarget + $"var c='no-throw'; try {{ {construct}; }} catch (e) {{ c = e.constructor.name; }} c"));

    // Subclassing still works: the instance gets the subclass prototype (resolved after a successful
    // construction) and the buffer is allocated.
    [Fact]
    public void ArrayBuffer_Subclass_StillGetsSubclassPrototype()
        => Assert.Equal("8 true", Eval(
            "class B extends ArrayBuffer {} var b = new B(8); " +
            "b.byteLength + ' ' + (Object.getPrototypeOf(b) === B.prototype)"));

    [Fact]
    public void DataView_Subclass_StillGetsSubclassPrototype()
        => Assert.Equal("true", Eval(
            "class V extends DataView {} var v = new V(new ArrayBuffer(8), 2); " +
            "'' + (Object.getPrototypeOf(v) === V.prototype && v.byteOffset === 2)"));

    // Atomics: the length is captured before the index `valueOf` grows the buffer, so the access is
    // out of bounds (RangeError) and the value/timeout are never coerced.
    private const string GrowingIndexSetup = @"
        var sab = new SharedArrayBuffer(0, { maxByteLength: 4 });
        var ta = new Int32Array(sab);
        var index = { valueOf() { sab.grow(4); return 0; } };
        var value = { valueOf() { throw new EvalError('value coerced'); } };
        var timeout = { valueOf() { throw new EvalError('timeout coerced'); } };";

    [Theory]
    [InlineData("Atomics.wait(ta, index, value, timeout)")]
    [InlineData("Atomics.waitAsync(ta, index, value, timeout)")]
    [InlineData("Atomics.notify(ta, index, value)")]
    public void Atomics_LengthCapturedBeforeIndexCoercion(string call)
        => Assert.Equal("RangeError", Eval(
            GrowingIndexSetup + $"var c='no-throw'; try {{ {call}; }} catch (e) {{ c = e.constructor.name; }} c"));

    [Fact] // the index valueOf side effect (grow) still ran even though the access was rejected
    public void Atomics_IndexValueOfSideEffectStillRuns()
        => Assert.Equal("4", Eval(
            GrowingIndexSetup + "try { Atomics.notify(ta, index, value); } catch (e) {} String(sab.byteLength)"));
}
