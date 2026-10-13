using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using System.Threading.Tasks;
using Broiler.JavaScript.BuiltIns.Array.Typed;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Objects;

/// <summary>
/// The <c>Atomics</c> namespace object. This engine is single-agent (no real OS threads),
/// so every atomic read-modify-write is a plain, uninterrupted read/compute/write on the
/// underlying integer TypedArray bytes — which is exactly the observable semantics of an
/// atomic operation when there is only one agent. <c>wait</c> cannot truly block (there is
/// no second agent to wake it), so it reports a value mismatch or a timeout without
/// suspending; <c>notify</c> always wakes zero waiters.
/// </summary>
[JSClassGenerator("Atomics"), JSInternalObject]
public partial class JSAtomics : JSObject
{
    private enum ElementKind { Int8, Uint8, Int16, Uint16, Int32, Uint32, BigInt64, BigUint64 }

    private enum BinaryOp { Add, Sub, And, Or, Xor, Exchange }

    // ── Validation (spec evaluation order: TypedArray type BEFORE index coercion,
    //    index coercion BEFORE value coercion) ──────────────────────────────────

    private static JSTypedArray ValidateIntegerTypedArray(JSValue value, bool waitable, out ElementKind kind)
    {
        if (value is not JSTypedArray typedArray)
            throw JSEngine.NewTypeError("Atomics operation requires a TypedArray");

        if (typedArray.buffer.isDetached)
            throw JSEngine.NewTypeError("Atomics operation on a detached ArrayBuffer");

        if (!TryGetIntegerKind(typedArray, out kind))
            throw JSEngine.NewTypeError("Atomics operation requires an integer TypedArray");

        if (waitable && kind != ElementKind.Int32 && kind != ElementKind.BigInt64)
            throw JSEngine.NewTypeError("Atomics.wait requires an Int32Array or BigInt64Array");

        return typedArray;
    }

    private static bool TryGetIntegerKind(JSTypedArray typedArray, out ElementKind kind)
    {
        switch (typedArray)
        {
            case JSInt8Array: kind = ElementKind.Int8; return true;
            case JSUInt8Array: kind = ElementKind.Uint8; return true;
            case JSInt16Array: kind = ElementKind.Int16; return true;
            case JSUInt16Array: kind = ElementKind.Uint16; return true;
            case JSInt32Array: kind = ElementKind.Int32; return true;
            case JSUInt32Array: kind = ElementKind.Uint32; return true;
            case JSBigInt64Array: kind = ElementKind.BigInt64; return true;
            case JSBigUint64Array: kind = ElementKind.BigUint64; return true;
            // Float16/32/64Array and Uint8ClampedArray are not valid atomic element types.
            default: kind = default; return false;
        }
    }

    private static bool IsBigIntKind(ElementKind kind)
        => kind is ElementKind.BigInt64 or ElementKind.BigUint64;

    // ValidateAtomicAccess: the array length is captured BEFORE ToIndex(requestIndex), so the
    // bounds-check uses the length as it was on entry — even if coercing the index runs user code
    // that grows the (growable shared) buffer. This is why e.g. Atomics.wait(ta, {valueOf grows},
    // value, timeout) throws a RangeError without ever coercing value/timeout.
    private static int ValidateAtomicAccess(JSTypedArray typedArray, JSValue requestIndex)
    {
        var length = typedArray.length;
        var accessIndex = IndexArgument(requestIndex);
        if (accessIndex >= length)
            throw JSEngine.NewRangeError("Atomics access index is out of bounds");

        return accessIndex;
    }

    // RevalidateAtomicAccess after value coercion (which may have run user code that
    // detached or shrank the buffer).
    private static void RevalidateAtomicAccess(JSTypedArray typedArray, int index)
    {
        if (typedArray.buffer.isDetached)
            throw JSEngine.NewTypeError("Atomics operation on a detached ArrayBuffer");

        if (index >= typedArray.length)
            throw JSEngine.NewRangeError("Atomics access index is out of bounds");
    }

    // ToIndex (§7.1.22): this engine's ToNumber (valueOf runs once for an object), then the shared
    // library's range rule (SJL-4.5); an index past int.MaxValue is out of every array's bounds.
    private static int IndexArgument(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        if (!IntegerConversions.TryToIndex(value.DoubleValue, out var index) || index > int.MaxValue)
            throw JSEngine.NewRangeError("Invalid atomic access index");

        return (int)index;
    }

    // An integer element's bits, in the low 32 bits of a uint: NumericToRawBytes of a Number, which is
    // ToInt8 ... ToUint32 of it, the shared library's (SJL-4.5). Every Number kind's bits are the low
    // bits of ToUint32's, so the arithmetic below is on those, modulo the element's width.
    private static uint ElementBits(double number) => IntegerConversions.ToUint32(number);

    // The element value a kind reads its low bits as.
    private static double ElementValue(ElementKind kind, uint bits) => kind switch
    {
        ElementKind.Int8 => IntegerConversions.ToInt8(bits),
        ElementKind.Uint8 => IntegerConversions.ToUint8(bits),
        ElementKind.Int16 => IntegerConversions.ToInt16(bits),
        ElementKind.Uint16 => IntegerConversions.ToUint16(bits),
        ElementKind.Int32 => IntegerConversions.ToInt32(bits),
        _ => bits,
    };

    // A BigInt kind's 64 bits: ToBigUint64 of the value, and the BigInt the kind reads them as.
    private static ulong BigElementBits(BigInteger value) => IntegerConversions.ToBigUint64(value);

    private static BigInteger BigElementValue(ElementKind kind, ulong bits)
        => kind == ElementKind.BigInt64 ? new BigInteger(unchecked((long)bits)) : new BigInteger(bits);

    // ── Read-modify-write (add/sub/and/or/xor/exchange) ─────────────────────────

    private static JSValue AtomicReadModifyWrite(in Arguments a, BinaryOp op)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: false, out var kind);
        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));
        var operand = a.GetAt(2) ?? JSUndefined.Value;

        // AtomicReadModifyWrite works on the element's raw bytes: the operand's bits and the old
        // value's, combined modulo the element's width (SJL-4.5). Nothing passes through a double
        // wider than the element, so an operand of 2**53 or 2**63 wraps as ToInt32 wraps it.
        if (IsBigIntKind(kind))
        {
            var v = BigElementBits(JSBigInt.Coerce(operand).value);
            RevalidateAtomicAccess(typedArray, index);

            var oldValue = typedArray.GetValue((uint)index, typedArray);
            var result = Apply(op, BigElementBits(((JSBigInt)oldValue).value), v);
            typedArray.SetValue((uint)index, new JSBigInt(BigElementValue(kind, result)), typedArray);
            return oldValue;
        }
        else
        {
            var v = ElementBits(IntegerConversions.ToIntegerOrInfinity(operand.DoubleValue));
            RevalidateAtomicAccess(typedArray, index);

            var oldValue = typedArray.GetValue((uint)index, typedArray);
            var result = (uint)Apply(op, ElementBits(oldValue.DoubleValue), v);
            typedArray.SetValue((uint)index, new JSNumber(ElementValue(kind, result)), typedArray);
            return oldValue;
        }
    }

    // The operation on raw bits, modulo 2**64; the element keeps the low bits of its width.
    private static ulong Apply(BinaryOp op, ulong old, ulong v) => unchecked(op switch
    {
        BinaryOp.Add => old + v,
        BinaryOp.Sub => old - v,
        BinaryOp.And => old & v,
        BinaryOp.Or => old | v,
        BinaryOp.Xor => old ^ v,
        BinaryOp.Exchange => v,
        _ => old,
    });

    [JSExport("add", Length = 3)]
    public static JSValue Add(in Arguments a) => AtomicReadModifyWrite(in a, BinaryOp.Add);

    [JSExport("sub", Length = 3)]
    public static JSValue Sub(in Arguments a) => AtomicReadModifyWrite(in a, BinaryOp.Sub);

    [JSExport("and", Length = 3)]
    public static JSValue And(in Arguments a) => AtomicReadModifyWrite(in a, BinaryOp.And);

    [JSExport("or", Length = 3)]
    public static JSValue Or(in Arguments a) => AtomicReadModifyWrite(in a, BinaryOp.Or);

    [JSExport("xor", Length = 3)]
    public static JSValue Xor(in Arguments a) => AtomicReadModifyWrite(in a, BinaryOp.Xor);

    [JSExport("exchange", Length = 3)]
    public static JSValue Exchange(in Arguments a) => AtomicReadModifyWrite(in a, BinaryOp.Exchange);

    // ── compareExchange ─────────────────────────────────────────────────────────

    [JSExport("compareExchange", Length = 4)]
    public static JSValue CompareExchange(in Arguments a)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: false, out var kind);
        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));

        if (IsBigIntKind(kind))
        {
            var expected = JSBigInt.Coerce(a.GetAt(2) ?? JSUndefined.Value).value;
            var replacement = JSBigInt.Coerce(a.GetAt(3) ?? JSUndefined.Value).value;
            RevalidateAtomicAccess(typedArray, index);

            var oldValue = typedArray.GetValue((uint)index, typedArray);
            if (BigElementBits(((JSBigInt)oldValue).value) == BigElementBits(expected))
                typedArray.SetValue((uint)index, new JSBigInt(replacement), typedArray);

            return oldValue;
        }
        else
        {
            // The expected value's raw bytes are compared with the element's (SJL-4.5).
            var expected = IntegerConversions.ToIntegerOrInfinity((a.GetAt(2) ?? JSUndefined.Value).DoubleValue);
            var replacement = IntegerConversions.ToIntegerOrInfinity((a.GetAt(3) ?? JSUndefined.Value).DoubleValue);
            RevalidateAtomicAccess(typedArray, index);

            var oldValue = typedArray.GetValue((uint)index, typedArray);
            if (ElementValue(kind, ElementBits(expected)) == oldValue.DoubleValue)
                typedArray.SetValue((uint)index, new JSNumber(replacement), typedArray);

            return oldValue;
        }
    }

    // ── load / store ────────────────────────────────────────────────────────────

    [JSExport("load", Length = 2)]
    public static JSValue Load(in Arguments a)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: false, out _);
        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));
        return typedArray.GetValue((uint)index, typedArray);
    }

    [JSExport("store", Length = 3)]
    public static JSValue Store(in Arguments a)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: false, out var kind);
        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));
        var operand = a.GetAt(2) ?? JSUndefined.Value;

        if (IsBigIntKind(kind))
        {
            // store returns the (untruncated) ToBigInt value, not the stored bytes.
            var v = JSBigInt.Coerce(operand);
            RevalidateAtomicAccess(typedArray, index);
            typedArray.SetValue((uint)index, v, typedArray);
            return v;
        }
        else
        {
            // store returns 𝔽(ToIntegerOrInfinity(value)) — the integral Number, even
            // though the element only keeps its low bits.
            // ToIntegerOrInfinity, the shared library's (SJL-4.5), normalizes -0 (and any value in
            // (-1, 0) that truncates to zero) to +0, so the returned Number is never -0
            // (test262 Atomics/store/expected-return-value-negative-zero).
            var integer = IntegerConversions.ToIntegerOrInfinity(operand.DoubleValue);
            RevalidateAtomicAccess(typedArray, index);
            var stored = new JSNumber(integer);
            typedArray.SetValue((uint)index, stored, typedArray);
            return stored;
        }
    }

    // ── isLockFree ──────────────────────────────────────────────────────────────

    [JSExport("isLockFree", Length = 1)]
    public static JSValue IsLockFree(in Arguments a)
    {
        var size = IntegerConversions.ToIntegerOrInfinity((a.GetAt(0) ?? JSUndefined.Value).DoubleValue);
        return size is 1 or 2 or 4 or 8 ? BooleanTrue : BooleanFalse;
    }

    // ── wait / notify ───────────────────────────────────────────────────────────

    [JSExport("wait", Length = 4)]
    public static JSValue Wait(in Arguments a)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: true, out var kind);

        // Atomics.wait is only valid on a SharedArrayBuffer-backed view.
        if (!typedArray.buffer.isShared)
            throw JSEngine.NewTypeError("Atomics.wait requires a SharedArrayBuffer");

        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));

        BigInteger expectedBig = default;
        long expected = 0;
        if (kind == ElementKind.BigInt64)
            expectedBig = JSBigInt.Coerce(a.GetAt(2) ?? JSUndefined.Value).value;
        else
            expected = IntegerConversions.ToInt32((a.GetAt(2) ?? JSUndefined.Value).DoubleValue);

        // ToNumber(timeout) is coerced (for its side effects) even though a single agent
        // cannot block: there is no other agent to notify it.
        _ = (a.GetAt(3) ?? JSUndefined.Value).DoubleValue;

        RevalidateAtomicAccess(typedArray, index);

        var current = typedArray.GetValue((uint)index, typedArray);
        bool matches = kind == ElementKind.BigInt64
            ? BigElementBits(((JSBigInt)current).value) == BigElementBits(expectedBig)
            : (long)current.DoubleValue == expected;

        // A mismatch returns immediately; a match would block until notified, but with no
        // other agent it can only time out.
        return new JSString(matches ? "timed-out" : "not-equal");
    }

    // Atomics.waitAsync( typedArray, index, value, timeout ): like wait, but never blocks the
    // agent — it returns a result record { async, value }. A value mismatch or a zero timeout
    // settles synchronously (async: false); a matching value with a positive timeout yields a
    // promise (async: true) which, with no other agent to wake it, resolves to "timed-out".
    [JSExport("waitAsync", Length = 4)]
    public static JSValue WaitAsync(in Arguments a)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: true, out var kind);

        if (!typedArray.buffer.isShared)
            throw JSEngine.NewTypeError("Atomics.waitAsync requires a SharedArrayBuffer");

        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));

        BigInteger expectedBig = default;
        long expected = 0;
        if (kind == ElementKind.BigInt64)
            expectedBig = JSBigInt.Coerce(a.GetAt(2) ?? JSUndefined.Value).value;
        else
            expected = IntegerConversions.ToInt32((a.GetAt(2) ?? JSUndefined.Value).DoubleValue);

        // q = ToNumber(timeout); a NaN timeout means +∞, otherwise it is clamped to ≥ 0.
        var q = (a.GetAt(3) ?? JSUndefined.Value).DoubleValue;
        var timeout = double.IsNaN(q) ? double.PositiveInfinity : Math.Max(q, 0);

        RevalidateAtomicAccess(typedArray, index);

        var current = typedArray.GetValue((uint)index, typedArray);
        bool matches = kind == ElementKind.BigInt64
            ? BigElementBits(((JSBigInt)current).value) == BigElementBits(expectedBig)
            : (long)current.DoubleValue == expected;

        if (!matches)
            return WaitAsyncResult(false, new JSString("not-equal"));

        if (timeout == 0)
            return WaitAsyncResult(false, new JSString("timed-out"));

        // Settled on this thread, so its reactions are ordinary jobs. It was wrapped from a Task,
        // whose continuation settled it from the thread pool at no fixed point in the job queue.
        // (No agent can notify this one, and the timeout is not waited for: it reports
        // "timed-out" at once.)
        var promise = new JSPromise(new JSString("timed-out"), JSPromise.PromiseState.Resolved);
        return WaitAsyncResult(true, promise);
    }

    // The { async, value } result record returned by Atomics.waitAsync.
    private static JSObject WaitAsyncResult(bool async, JSValue value)
    {
        var result = new JSObject();
        result[KeyStrings.GetOrCreate("async")] = async ? BooleanTrue : BooleanFalse;
        result[KeyStrings.GetOrCreate("value")] = value;
        return result;
    }

    [JSExport("notify", Length = 3)]
    public static JSValue Notify(in Arguments a)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: true, out _);
        _ = ValidateAtomicAccess(typedArray, a.GetAt(1));

        // Count is coerced for its side effects; no agent is ever waiting here.
        _ = (a.GetAt(2) ?? JSUndefined.Value).DoubleValue;

        return new JSNumber(0);
    }

    /// <summary>
    /// <c>Atomics.pause( [ N ] )</c> — a micro-architectural hint that the
    /// current code is in a spin-wait loop. We have nothing to signal, so this
    /// is a no-op that validates its optional argument and returns undefined.
    /// Per sec-atomics.pause: if N is neither undefined nor an integral Number,
    /// throw a TypeError.
    /// </summary>
    [JSExport("pause", Length = 0)]
    public static JSValue Pause(in Arguments a)
    {
        var n = a.GetAt(0);
        if (!n.IsUndefined)
        {
            if (!n.IsNumber)
                throw JSEngine.NewTypeError("Atomics.pause argument must be an integral Number");

            var value = n.DoubleValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Floor(value) != value)
                throw JSEngine.NewTypeError("Atomics.pause argument must be an integral Number");
        }

        return JSUndefined.Value;
    }
}
