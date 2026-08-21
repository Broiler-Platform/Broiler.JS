using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for the non-Temporal items of https://github.com/MaiRat/Broiler.JS/issues/771
//
//   Problem 15 — Atomics.waitAsync was missing entirely. It now exists and returns the
//   { async, value } result record (synchronous "not-equal"/"timed-out", or an async promise).
//
//   Problem 9 (subset) — SharedArrayBuffer.prototype.slice ignored Symbol.species, so it never
//   threw when the species constructor returned a non/same/too-small buffer; and
//   Iterator.prototype.return accepted any object receiver instead of requiring the iterator
//   brand (WrapForValidIteratorPrototype.return.call({}) must be a TypeError).
public class Issue771NonTemporalTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static string ErrorName(string body) => Eval(
        "let t='NONE'; try { " + body + " } catch (e) { t = e.constructor.name; } t");

    // --- Atomics.waitAsync ---

    [Fact(Timeout = 600000)]
    public void AtomicsWaitAsyncIsAFunction()
        => Assert.Equal("function", Eval("typeof Atomics.waitAsync"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncZeroTimeoutOnMatchIsSyncTimedOut()
        => Assert.Equal("false:timed-out", Eval(
            "const a = new Int32Array(new SharedArrayBuffer(8));" +
            "const r = Atomics.waitAsync(a, 0, 0, 0); r.async + ':' + r.value"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncFalseTimeoutCoercesToZero()
        => Assert.Equal("timed-out", Eval(
            "const a = new Int32Array(new SharedArrayBuffer(8));" +
            "Atomics.waitAsync(a, 0, 0, false).value"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncValueMismatchIsNotEqual()
        => Assert.Equal("false:not-equal", Eval(
            "const a = new Int32Array(new SharedArrayBuffer(8)); a[0] = 42;" +
            "const r = Atomics.waitAsync(a, 0, 0); r.async + ':' + r.value"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncPositiveTimeoutOnMatchIsAsyncPromise()
        => Assert.Equal("true:object", Eval(
            "const a = new Int32Array(new SharedArrayBuffer(8));" +
            "const r = Atomics.waitAsync(a, 0, 0, 100); r.async + ':' + typeof r.value"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncNegativeIndexThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName(
            "Atomics.waitAsync(new Int32Array(new SharedArrayBuffer(8)), -Infinity, 0, 0);"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncNonTypedArrayThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName("Atomics.waitAsync({}, 0, 0, 0);"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncNonSharedBufferThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(
            "Atomics.waitAsync(new Int32Array(new ArrayBuffer(8)), 0, 0, 0);"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncNonIntegerTypedArrayThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(
            "Atomics.waitAsync(new Float64Array(new SharedArrayBuffer(8)), 0, 0, 0);"));

    [Fact(Timeout = 600000)]
    public void WaitAsyncSymbolValueThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(
            "Atomics.waitAsync(new Int32Array(new SharedArrayBuffer(8)), 0, Symbol(), 0);"));

    // --- SharedArrayBuffer.prototype.slice species validation ---

    [Fact(Timeout = 600000)]
    public void SabSliceWithoutSpeciesStillWorks()
        => Assert.Equal("4", Eval("new SharedArrayBuffer(8).slice(0, 4).byteLength + ''"));

    [Fact(Timeout = 600000)]
    public void SabSliceSpeciesReturningNonSharedArrayBufferThrows()
        => Assert.Equal("TypeError", ErrorName(
            "const b = new SharedArrayBuffer(8);" +
            "const c = {}; c[Symbol.species] = function () { return {}; }; b.constructor = c;" +
            "b.slice();"));

    [Fact(Timeout = 600000)]
    public void SabSliceSpeciesReturningSameBufferThrows()
        => Assert.Equal("TypeError", ErrorName(
            "const b = new SharedArrayBuffer(8);" +
            "const c = {}; c[Symbol.species] = function () { return b; }; b.constructor = c;" +
            "b.slice();"));

    [Fact(Timeout = 600000)]
    public void SabSliceSpeciesReturningSmallerBufferThrows()
        => Assert.Equal("TypeError", ErrorName(
            "const b = new SharedArrayBuffer(8);" +
            "const c = {}; c[Symbol.species] = function () { return new SharedArrayBuffer(1); }; b.constructor = c;" +
            "b.slice(0, 4);"));

    [Fact(Timeout = 600000)]
    public void SabSliceSpeciesReturningValidBufferSucceeds()
        => Assert.Equal("4", Eval(
            "const b = new SharedArrayBuffer(8);" +
            "const c = {}; c[Symbol.species] = function (n) { return new SharedArrayBuffer(n); }; b.constructor = c;" +
            "b.slice(0, 4).byteLength + ''"));

    [Fact(Timeout = 600000)]
    public void SabSliceNonCallableSpeciesThrows()
        => Assert.Equal("TypeError", ErrorName(
            "const b = new SharedArrayBuffer(8);" +
            "const c = {}; c[Symbol.species] = 42; b.constructor = c;" +
            "b.slice();"));

    // --- Iterator.prototype.return requires the iterator brand ---

    [Fact(Timeout = 600000)]
    public void IteratorReturnOnPlainObjectThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(
            "const W = Object.getPrototypeOf(Iterator.from({ [Symbol.iterator]() { return { next() { return { value: 1, done: false }; } }; } }));" +
            "W.return.call({});"));

    [Fact(Timeout = 600000)]
    public void IteratorReturnOnRealWrapperStillWorks()
        => Assert.Equal("true", Eval(
            "const it = Iterator.from({ [Symbol.iterator]() { return { next() { return { value: 1, done: false }; } }; } });" +
            "it.return().done + ''"));
}
