using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here (ArraySetLength / array "length" [[DefineOwnProperty]]):
//
//   * A supplied length [[Value]] is coerced with ToUint32 AND ToNumber (both observable
//     for an object value, in that order) before the descriptor invariants are checked,
//     and the "length" writability is re-read AFTER coercion — so a valueOf that flips
//     "length" to non-writable mid-coercion is honoured (coercion-order).
//   * A length [[Value]] assigned via [[Set]] is coerced exactly once through
//     ArraySetLength's ToUint32+ToNumber (not pre-coerced first) (coercion-order-set).
//   * With no [[Value]], converting "length" to an accessor (get/set) — or making it
//     configurable/enumerable — is rejected, and the getter is never invoked
//     (no-value-order).
public class Issue836ArraySetLengthTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // defineProperty: value coerced twice, writability re-read after coercion → TypeError.
    [Fact]
    public void DefinePropertyCoercesValueTwiceThenValidates()
        => Assert.Equal("2,TypeError", Eval(@"
            var array = [1, 2];
            var calls = 0;
            var length = { valueOf: function() {
                calls += 1;
                if (calls !== 1) Object.defineProperty(array, 'length', { writable: false });
                return array.length;
            }};
            var r;
            try { Object.defineProperty(array, 'length', { value: length, writable: true }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            calls + ',' + r"));

    // [[Set]] of length: @@toPrimitive observed for both ToUint32 and ToNumber → 2 reads.
    [Fact]
    public void StrictAssignCoercesValueTwice()
        => Assert.Equal("number,number,TypeError", Eval(@"
            var array = [1, 2, 3];
            var hints = [];
            var length = {};
            length[Symbol.toPrimitive] = function(hint) {
                hints.push(hint);
                Object.defineProperty(array, 'length', { writable: false });
                return 0;
            };
            var r;
            try { 'use strict'; (function(){ 'use strict'; array.length = length; })(); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            hints.join(',') + ',' + r"));

    [Fact]
    public void DefineLengthAsAccessorRejectedWithoutCallingGetter()
        => Assert.Equal("TypeError,not-called", Eval(@"
            var called = 'not-called';
            var r;
            try { Object.defineProperty([], 'length', { get: function() { called = 'called'; } }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r + ',' + called"));

    [Fact]
    public void DefineLengthConfigurableRejected()
        => Assert.Equal("TypeError", Eval(@"
            var r;
            try { Object.defineProperty([], 'length', { configurable: true }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r"));

    [Fact]
    public void ReflectDefineLengthAccessorReturnsFalse()
        => Assert.Equal("false", Eval(
            "String(Reflect.defineProperty([], 'length', { set: function(_v) {} }))"));

    // An invalid length value is a RangeError (ToUint32 ≠ ToNumber), thrown before the
    // writability invariant.
    [Fact]
    public void InvalidLengthValueIsRangeError()
        => Assert.Equal("RangeError", Eval(@"
            var r;
            try { Object.defineProperty([], 'length', { value: -1 }); r = 'no throw'; }
            catch (e) { r = e.constructor.name; }
            r"));
}
