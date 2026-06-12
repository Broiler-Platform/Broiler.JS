using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.Array.Typed;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Number;
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

    // ValidateAtomicAccess: ToIndex(requestIndex) then bounds-check against the live length.
    private static int ValidateAtomicAccess(JSTypedArray typedArray, JSValue requestIndex)
    {
        var accessIndex = ToIndex(requestIndex);
        if (accessIndex >= typedArray.length)
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

    private static int ToIndex(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue; // ToNumber (invokes valueOf once for an object)
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);

        if (integer < 0 || integer > 9007199254740991d)
            throw JSEngine.NewRangeError("Invalid atomic access index");

        if (integer > int.MaxValue)
            throw JSEngine.NewRangeError("Invalid atomic access index");

        return (int)integer;
    }

    // ToIntegerOrInfinity → low bits of the element; ToInt32(±Infinity)/NaN is 0, so an
    // out-of-long operand collapses to 0 just as the element write would.
    private static long ToIntegerOperand(JSValue value)
    {
        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            return 0;

        var integer = Math.Truncate(number);
        if (integer >= long.MaxValue) return long.MaxValue;
        if (integer <= long.MinValue) return long.MinValue;
        return (long)integer;
    }

    private static long TruncateToElement(ElementKind kind, long value) => kind switch
    {
        ElementKind.Int8 => (sbyte)value,
        ElementKind.Uint8 => (byte)value,
        ElementKind.Int16 => (short)value,
        ElementKind.Uint16 => (ushort)value,
        ElementKind.Int32 => (int)value,
        ElementKind.Uint32 => (uint)value,
        _ => value,
    };

    private static BigInteger TruncateToBigElement(ElementKind kind, BigInteger value)
    {
        var masked = value & ((BigInteger.One << 64) - 1);
        if (kind == ElementKind.BigInt64 && masked >= (BigInteger.One << 63))
            masked -= BigInteger.One << 64;
        return masked;
    }

    // ── Read-modify-write (add/sub/and/or/xor/exchange) ─────────────────────────

    private static JSValue AtomicReadModifyWrite(in Arguments a, BinaryOp op)
    {
        var typedArray = ValidateIntegerTypedArray(a.GetAt(0), waitable: false, out var kind);
        var index = ValidateAtomicAccess(typedArray, a.GetAt(1));
        var operand = a.GetAt(2) ?? JSUndefined.Value;

        if (IsBigIntKind(kind))
        {
            var v = JSBigInt.Coerce(operand).value;
            RevalidateAtomicAccess(typedArray, index);

            var old = ((JSBigInt)typedArray.GetValue((uint)index, typedArray)).value;
            var result = ApplyBig(op, old, v);
            typedArray.SetValue((uint)index, new JSBigInt(result), typedArray);
            return new JSBigInt(old);
        }
        else
        {
            var v = ToIntegerOperand(operand);
            RevalidateAtomicAccess(typedArray, index);

            var oldValue = typedArray.GetValue((uint)index, typedArray);
            var old = (long)oldValue.DoubleValue;
            var result = ApplyInt(op, old, v);
            typedArray.SetValue((uint)index, new JSNumber(result), typedArray);
            return oldValue;
        }
    }

    private static long ApplyInt(BinaryOp op, long old, long v) => op switch
    {
        BinaryOp.Add => old + v,
        BinaryOp.Sub => old - v,
        BinaryOp.And => old & v,
        BinaryOp.Or => old | v,
        BinaryOp.Xor => old ^ v,
        BinaryOp.Exchange => v,
        _ => old,
    };

    private static BigInteger ApplyBig(BinaryOp op, BigInteger old, BigInteger v) => op switch
    {
        BinaryOp.Add => old + v,
        BinaryOp.Sub => old - v,
        BinaryOp.And => old & v,
        BinaryOp.Or => old | v,
        BinaryOp.Xor => old ^ v,
        BinaryOp.Exchange => v,
        _ => old,
    };

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

            var old = ((JSBigInt)typedArray.GetValue((uint)index, typedArray)).value;
            if (old == TruncateToBigElement(kind, expected))
                typedArray.SetValue((uint)index, new JSBigInt(replacement), typedArray);

            return new JSBigInt(old);
        }
        else
        {
            var expected = ToIntegerOperand(a.GetAt(2) ?? JSUndefined.Value);
            var replacement = ToIntegerOperand(a.GetAt(3) ?? JSUndefined.Value);
            RevalidateAtomicAccess(typedArray, index);

            var oldValue = typedArray.GetValue((uint)index, typedArray);
            var old = (long)oldValue.DoubleValue;
            if (old == TruncateToElement(kind, expected))
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
            var number = operand.DoubleValue;
            var integer = double.IsNaN(number) ? 0d : Math.Truncate(number);
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
        var number = (a.GetAt(0) ?? JSUndefined.Value).DoubleValue;
        var size = double.IsNaN(number) ? 0 : Math.Truncate(number);
        return size is 1 or 2 or 4 or 8 ? JSValue.BooleanTrue : JSValue.BooleanFalse;
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
            expected = (long)(int)ToIntegerOperand(a.GetAt(2) ?? JSUndefined.Value);

        // ToNumber(timeout) is coerced (for its side effects) even though a single agent
        // cannot block: there is no other agent to notify it.
        _ = (a.GetAt(3) ?? JSUndefined.Value).DoubleValue;

        RevalidateAtomicAccess(typedArray, index);

        var current = typedArray.GetValue((uint)index, typedArray);
        bool matches = kind == ElementKind.BigInt64
            ? ((JSBigInt)current).value == TruncateToBigElement(kind, expectedBig)
            : (long)current.DoubleValue == TruncateToElement(kind, expected);

        // A mismatch returns immediately; a match would block until notified, but with no
        // other agent it can only time out.
        return new JSString(matches ? "timed-out" : "not-equal");
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
