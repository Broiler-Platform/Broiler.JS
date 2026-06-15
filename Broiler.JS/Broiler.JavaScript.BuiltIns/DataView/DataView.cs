using Broiler.JavaScript.BuiltIns.Array.Typed;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.DataView;

[JSClassGenerator]
public partial class DataView : JSObject
{
    internal readonly JSArrayBuffer buffer;
    internal readonly int byteOffset;

    // A length-tracking DataView (no explicit byteLength over a resizable buffer) floats
    // with the buffer; a fixed-length view stores explicitByteLength and can become out of
    // bounds when the buffer shrinks. byteLength is computed so a resize() is observed.
    private readonly bool isLengthTracking;
    private readonly int explicitByteLength;

    internal int byteLength => ComputeByteLength();

    private int ComputeByteLength()
    {
        if (buffer.isDetached)
            return 0;

        var bufferByteLength = buffer.buffer.Length;
        if (byteOffset > bufferByteLength)
            return 0;

        if (isLengthTracking)
            return bufferByteLength - byteOffset;

        if (byteOffset + explicitByteLength > bufferByteLength)
            return 0; // out of bounds after a shrink

        return explicitByteLength;
    }

    // GetViewByteLength after an IsViewOutOfBounds check: a detached or out-of-bounds view
    // throws a TypeError (before the per-access RangeError bounds check).
    private int RequireInBoundsByteLength()
    {
        if (buffer.isDetached)
            throw JSEngine.NewTypeError("Cannot operate on a detached ArrayBuffer");

        var bufferByteLength = buffer.buffer.Length;
        if (byteOffset > bufferByteLength
            || (!isLengthTracking && byteOffset + explicitByteLength > bufferByteLength))
        {
            throw JSEngine.NewTypeError("DataView is out of bounds");
        }

        return isLengthTracking ? bufferByteLength - byteOffset : explicitByteLength;
    }

    [JSExport(Length = 1)]
    public DataView(in Arguments a) : this()
    {
        // The byteOffset / byteLength RangeErrors below are thrown BEFORE the instance prototype is
        // resolved (deferred to JSFunction.CreateInstance's post-construction step), so a throwing
        // new.target `get prototype` accessor is not observed when the offset is out of range.
        var buffer = a[0] as JSArrayBuffer ?? throw JSEngine.NewTypeError("First argument to DataView constructor must be an ArrayBuffer.");
        // ToIndex(byteOffset): a fractional value truncates toward zero, NaN / undefined become 0, and
        // a negative or non-integral-index value (e.g. -Infinity, +Infinity) is a RangeError — observed
        // before the offset is range-checked against the buffer.
        var byteOffset = ToIndex(a[1]); //optional, if not available assign 0

        var bufferByteLength = buffer.buffer.Length;

        // An offset at the very end of the buffer is a valid zero-length view, so only offsets strictly
        // past the end are errors.
        if (byteOffset > bufferByteLength)
            throw JSEngine.NewRangeError("Start offset is outside the bounds of the buffer.");

        var byteLengthArg = a[2];
        if (byteLengthArg == null || byteLengthArg.IsUndefined)
        {
            // No explicit length: a resizable buffer yields a length-tracking view; a
            // fixed buffer yields a fixed view spanning (buffer length - byte offset).
            if (buffer.IsResizable)
                isLengthTracking = true;
            else
                explicitByteLength = (int)(bufferByteLength - byteOffset);
        }
        else
        {
            var byteLength = ToIndex(byteLengthArg);
            if (byteOffset + byteLength > bufferByteLength)
                throw JSEngine.NewRangeError("Invalid DataView length.");

            explicitByteLength = (int)byteLength;
        }

        this.buffer = buffer;
        this.byteOffset = (int)byteOffset;
    }

    // ToIndex (abstract operation): ToNumber the argument (observing valueOf), truncate toward zero,
    // and require the result to be an integer index in [0, 2^53-1]; undefined / NaN map to 0 and a
    // negative or out-of-range value (including ±Infinity) is a RangeError. Returned as a long so the
    // buffer-bounds comparisons happen before the value is narrowed to the int offset/length fields.
    private static long ToIndex(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue;
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0 || integer > 9007199254740991d) // 2^53 - 1
            throw JSEngine.NewRangeError("DataView offset or length is out of range.");

        return (long)integer;
    }

    public DataView(JSArrayBuffer buffer, int byteOffset, int byteLength) : this()
    {
        this.buffer = buffer;
        this.explicitByteLength = byteLength;
        this.byteOffset = byteOffset;
    }

    /// <summary>
    /// Stores a series of bytes at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// store the data. </param>
    /// <param name="bytes"> The bytes to store. </param>
    /// <param name="littleEndian"> Indicates whether the bytes are stored in little- or
    /// big-endian format. If false, a big-endian value is written. </param>
    internal void SetCore(int byteOffset, byte[] bytes, bool littleEndian)
    {
        if (littleEndian)
        {
            for (int i = 0; i < bytes.Length; i++)
                buffer.buffer[this.byteOffset + byteOffset + i] = bytes[i];
        }
        else
        {
            for (int i = 0; i < bytes.Length; i++)
                buffer.buffer[this.byteOffset + byteOffset + bytes.Length - 1 - i] = bytes[i];
        }
    }

    [JSExport]
    public JSValue Buffer => buffer;

    // get DataView.prototype.byteLength / byteOffset: an out-of-bounds (or detached) view
    // throws a TypeError rather than reporting a stale length/offset.
    [JSExport]
    public int ByteLength => RequireInBoundsByteLength();

    [JSExport]
    public int ByteOffset
    {
        get
        {
            RequireInBoundsByteLength();
            return byteOffset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ToByteOffset(JSValue value)
    {
        var number = value.DoubleValue;
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number))
            return int.MaxValue;

        if (double.IsNegativeInfinity(number))
            return int.MinValue;

        return (int)number;
    }

    /// <summary>
    /// Gets a signed 64-bit integer at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The signed 64-bit integer at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetBigInt64(in Arguments a) => new JSBigInt(GetInt64(in a));

    //internal method
    public unsafe long GetInt64(in Arguments a)
    {
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var littleEndian = a[1]?.BooleanValue ?? false;

        if (byteOffset < 0 || byteOffset > RequireInBoundsByteLength() - 8)
            throw JSEngine.NewRangeError($"Offset {byteOffset} is outside the bounds of DataView");

        fixed (byte* ptr = &buffer.buffer[this.byteOffset + byteOffset])
        {
            if (littleEndian)
            {
                int temp1 = (*ptr) | (*(ptr + 1) << 8) | (*(ptr + 2) << 16) | (*(ptr + 3) << 24);
                int temp2 = (*(ptr + 4)) | (*(ptr + 5) << 8) | (*(ptr + 6) << 16) | (*(ptr + 7) << 24);
                return (uint)temp1 | ((long)temp2 << 32);
            }
            else
            {
                int temp1 = (*ptr << 24) | (*(ptr + 1) << 16) | (*(ptr + 2) << 8) | (*(ptr + 3));
                int temp2 = (*(ptr + 4) << 24) | (*(ptr + 5) << 16) | (*(ptr + 6) << 8) | (*(ptr + 7));
                return (uint)temp2 | ((long)temp1 << 32);
            }
        }
    }

    [JSExport("getBigUint64", Length = 1)]
    public JSValue GetBigUInt64(in Arguments a) => new JSBigInt(new BigInteger((ulong)GetInt64(in a)));

    //internal method
    public unsafe int GetInt32Int(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var littleEndian = a[1]?.BooleanValue ?? false;
        
        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 4)
            throw JSEngine.NewRangeError($"Offset {byteOffset} is outside the bounds of DataView");

        var buffer = @this.buffer;

        fixed (byte* ptr = &buffer.buffer[@this.byteOffset + byteOffset])
        {
            if (littleEndian)
            {
                return (*ptr) | (*(ptr + 1) << 8) | (*(ptr + 2) << 16) | (*(ptr + 3) << 24);
            }
            else
            {
                return (*ptr << 24) | (*(ptr + 1) << 16) | (*(ptr + 2) << 8) | (*(ptr + 3));
            }
        }
    }

    /// <summary>
    /// Gets a 32-bit floating point number at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The 32-bit floating point number at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public unsafe JSValue GetFloat32(in Arguments a)
    {
        int temp = GetInt32Int(in a);
        return new JSNumber(*(float*)&temp);
    }

    /// <summary>
    /// Gets a 16-bit floating point number (half-precision) at the specified byte offset
    /// from the start of the DataView (ES2025 §2.8).
    /// </summary>
    [JSExport(Length = 1)]
    public JSValue GetFloat16(in Arguments a)
    {
        int temp = GetInt16Int(in a);
        var half = BitConverter.ToHalf(BitConverter.GetBytes((short)temp), 0);
        return new JSNumber((double)half);
    }

    /// <summary>
    /// Gets a 64-bit floating point number at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The 64-bit floating point number at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public unsafe JSValue GetFloat64(in Arguments a)
    {
        long temp = GetInt64(in a);
        return new JSNumber(*(double*)&temp);
    }

    //internal
    public unsafe int GetInt16Int(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var littleEndian = a[1]?.BooleanValue ?? false;

        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 2)
            throw JSEngine.NewRangeError($"Offset {byteOffset} is outside the bounds of DataView");

        var buffer = @this.buffer;

        fixed (byte* ptr = &buffer.buffer[@this.byteOffset + byteOffset])
        {
            if (littleEndian)
            {
                return (short)((*ptr) | (*(ptr + 1) << 8));
            }
            else
            {
                return (short)((*ptr << 8) | (*(ptr + 1)));
            }
        }
    }


    /// <summary>
    /// Gets a signed 16-bit integer at the specified byte offset from the start of the DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The signed 16-bit integer at the specified byte offset from the start of the
    /// DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetInt16(in Arguments a) => new JSNumber(GetInt16Int(in a));


    /// <summary>
    /// Gets a signed 32-bit integer at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The signed 32-bit integer at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetInt32(in Arguments a) => new JSNumber(GetInt32Int(in a));


    /// <summary>
    /// Gets a signed 8-bit integer (byte) at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <returns> The signed 8-bit integer (byte) at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetInt8(in Arguments a) => new JSNumber(GetInt8Int(in a));

    public int GetInt8Int(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);

        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 1)
            throw JSEngine.NewRangeError($"Offset {byteOffset} is outside the bounds of DataView");

        var buffer = @this.buffer;
        return (sbyte)buffer.buffer[@this.byteOffset + byteOffset];
    }


    /// <summary>
    /// Gets an unsigned 8-bit integer (byte) at the specified byte offset from the start of
    /// the DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The unsigned 8-bit integer (byte) at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetUint16(in Arguments a) => new JSNumber((ushort)GetInt16Int(in a));


    /// <summary>
    /// Gets an unsigned 32-bit integer at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <param name="littleEndian"> Indicates whether the number is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is read. </param>
    /// <returns> The unsigned 32-bit integer at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetUint32(in Arguments a) => new JSNumber((uint)GetInt32Int(in a));


    /// <summary>
    /// Gets an unsigned 8-bit integer (byte) at the specified byte offset from the start of
    /// the DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// read the data. </param>
    /// <returns> The unsigned 8-bit integer (byte) at the specified byte offset from the start
    /// of the DataView. </returns>
    [JSExport(Length = 1)]
    public JSValue GetUint8(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        
        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 1)
            throw JSEngine.NewRangeError($"{byteOffset} offset is outside the bounds of DataView");

        var buffer = @this.buffer;
        return new JSNumber(buffer.buffer[@this.byteOffset + byteOffset]);
    }

    /// <summary>
    /// Stores a signed 64-bit float value at the specified byte offset from the start of the
    /// DataView.
    /// </summary>
    /// <param name="byteOffset"> The offset, in bytes, from the start of the view where to
    /// store the data. </param>
    /// <param name="value"> The value to set. </param>
    /// <param name="littleEndian"> Indicates whether the 64-bit float is stored in little- or
    /// big-endian format. If false or undefined, a big-endian value is written. </param>
    // RawBytesFor reduces a (ToBigInt-coerced) value to its low 64 bits — the
    // two's-complement 8-byte pattern shared by setBigInt64/setBigUint64. A plain
    // (long)/(ulong) cast on JSBigInt.BigIntValue overflows for magnitudes that do
    // not fit a signed 64-bit integer, so mask the BigInteger directly.
    private static byte[] RawBytesFor(JSValue value)
    {
        var big = value is JSBigInt bigint ? bigint.value : new System.Numerics.BigInteger(value.BigIntValue);
        ulong bits = (ulong)(big & ((System.Numerics.BigInteger.One << 64) - 1));
        return BitConverter.GetBytes(bits);
    }

    [JSExport(Length = 2)]
    public JSValue SetBigInt64(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 8, bigInt: true);
        @this.SetCore(byteOffset, RawBytesFor(value), littleEndian);
        return JSUndefined.Value;
    }

    [JSExport("setBigUint64", Length = 2)]
    public JSValue SetBigUInt64(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 8, bigInt: true);
        @this.SetCore(byteOffset, RawBytesFor(value), littleEndian);
        return JSUndefined.Value;
    }

    [JSExport(Length = 2)]
    public JSValue SetFloat32(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 4);
        var bytes = BitConverter.GetBytes((float)value.DoubleValue);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }

    /// <summary>
    /// Stores a 16-bit floating point (half-precision) value at the specified
    /// byte offset from the start of the DataView (ES2025 §2.8).
    /// </summary>
    [JSExport(Length = 2)]
    public JSValue SetFloat16(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 2);
        var half = (Half)value.DoubleValue;
        var bytes = BitConverter.GetBytes(half);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }


    [JSExport(Length = 2)]
    public JSValue SetFloat64(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 8);
        var bytes = BitConverter.GetBytes(value.DoubleValue);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }

    [JSExport(Length = 2)]
    public JSValue SetInt16(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 2);
        var bytes = BitConverter.GetBytes((short)(uint)value.IntValue);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }

    [JSExport(Length = 2)]
    public JSValue SetInt32(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 4);
        var bytes = BitConverter.GetBytes(value.IntValue);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }


    [JSExport(Length = 2)]
    public JSValue SetInt8(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 1);
        var bytes = (byte)(sbyte)(uint)value.IntValue;

        @this.buffer.buffer[@this.byteOffset + byteOffset] = bytes;
        return JSUndefined.Value;
    }

    [JSExport(Length = 2)]
    public JSValue SetUint16(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 2);
        var bytes = BitConverter.GetBytes((ushort)value.IntValue);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }

    [JSExport(Length = 2)]
    public JSValue SetUint32(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 4);
        var bytes = BitConverter.GetBytes((uint)value.IntValue);

        @this.SetCore(byteOffset, bytes, littleEndian);
        return JSUndefined.Value;
    }

    [JSExport(Length = 2)]
    public JSValue SetUint8(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 1);
        var bytes = (byte)(uint)value.IntValue;

        @this.buffer.buffer[@this.byteOffset + byteOffset] = bytes;
        return JSUndefined.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int byteOffset, bool littleEndian, DataView dataView, JSValue value) GetSetArgs(in Arguments a, int length, bool bigInt = false)
    {
        var @this = this;

        // The immutability of the backing buffer is observable independently of the
        // arguments, so it must be checked before any argument coercion (which can run
        // user code via valueOf/toString). See DataView.prototype.set* immutable-buffer
        // tests in test262.
        if (@this.buffer.isImmutable)
            throw JSEngine.NewTypeError("Cannot modify a DataView backed by an immutable ArrayBuffer");

        // An omitted byteOffset is ToIndex(undefined) = 0 and an omitted value is undefined
        // (coerced per type); neither argument is required.
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var value = a[1] ?? JSUndefined.Value;

        // SetViewValue coerces the value (ToBigInt for the BigInt element types)
        // BEFORE the out-of-bounds RangeError check, so e.g. setBigInt64(0) with no
        // value argument is a TypeError (ToBigInt(undefined)) rather than a no-op.
        if (bigInt)
            value = JSBigInt.Coerce(value);

        var littleEndian = a[2]?.BooleanValue ?? false;

        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - length)
            throw JSEngine.NewRangeError($"Offset {byteOffset} is outside the bounds of DataView");

        return (byteOffset, littleEndian, @this, value);
    }
}
