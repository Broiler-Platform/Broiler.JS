using Broiler.JavaScript.BuiltIns.Array.Typed;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.DataView;

// Broiler-AI:           Origin=AI; IP=Medium; Security=Medium; Resources=4; Fingerprint=TBF
// Broiler-Falsified-If: a get or set whose index plus element size exceeds the view's current byteLength, after the buffer is resized, shrunk or detached, touches the buffer instead of throwing
// Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a fixed-length view whose buffer shrank below its byteOffset plus its length reports a nonzero byteLength
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a view whose byteOffset lies past the end of a shrunk or detached buffer returns a length instead of raising a TypeError
    // Broiler-Human:        PENDING
    private int RequireInBoundsByteLength()
    {
        if (buffer.isDetached)
            ThrowDetachedBuffer();

        var bufferByteLength = buffer.buffer.Length;
        if (byteOffset > bufferByteLength
            || (!isLengthTracking && byteOffset + explicitByteLength > bufferByteLength))
        {
            ThrowOutOfBoundsView();
        }

        return isLengthTracking ? bufferByteLength - byteOffset : explicitByteLength;
    }

    // Keep exception construction and message formatting out of the successful access
    // path. These helpers are deliberately not inlined; the Phase 0 disassembly job can
    // therefore verify that ordinary DataView reads/writes branch to cold throw blocks.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an access through a view on a detached buffer surfaces as an error other than a JS TypeError
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDetachedBuffer()
        => throw JSEngine.NewTypeError("Cannot operate on a detached ArrayBuffer");

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an access through an out-of-bounds view surfaces as a RangeError or a CLR exception instead of a JS TypeError
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOutOfBoundsView()
        => throw JSEngine.NewTypeError("DataView is out of bounds");

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: an index past the end of the view surfaces as a TypeError or a CLR exception instead of a JS RangeError
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOffsetOutOfBounds(int offset)
        => throw JSEngine.NewRangeError($"Offset {offset} is outside the bounds of DataView");

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a set through a view over an immutable buffer surfaces as an error other than a JS TypeError
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowImmutableBuffer()
        => throw JSEngine.NewTypeError("Cannot modify a DataView backed by an immutable ArrayBuffer");

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s25.3.2.1; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: new DataView over a detached ArrayBuffer, or one the new.target prototype getter detaches, returns a view or raises a RangeError instead of a TypeError
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public DataView(in Arguments a) : this()
    {
        // Per §25.3.2 DataView ( buffer [, byteOffset [, byteLength ]] ), the buffer-bounds
        // checks (steps 6 and 9b) precede OrdinaryCreateFromConstructor (step 10) AND are
        // re-performed (steps 13 and 14a) on the (possibly resized) buffer afterwards. A
        // throwing new.target `get prototype` accessor must therefore not be observed for an
        // already-out-of-range offset / length (Issue #794), but must fire ahead of a
        // bounds-only-after-resize RangeError (test262 DataView/custom-proto-access-resizes-
        // buffer-invalid-by-length / -by-offset).
        var buffer = a[0] as JSArrayBuffer ?? throw JSEngine.NewTypeError("First argument to DataView constructor must be an ArrayBuffer.");
        // ToIndex(byteOffset): a fractional value truncates toward zero, NaN / undefined become 0, and
        // a negative or non-integral-index value (e.g. -Infinity, +Infinity) is a RangeError — observed
        // before the offset is range-checked against the buffer.
        var byteOffset = ToIndex(a[1]); //optional, if not available assign 0

        var byteLengthArg = a[2];
        long byteLength = 0;
        var hasExplicitLength = byteLengthArg != null && !byteLengthArg.IsUndefined;
        if (hasExplicitLength)
            byteLength = ToIndex(byteLengthArg);

        // First bounds check (spec steps 5/6/9b) — against the buffer's CURRENT byte length, before
        // the prototype getter has had a chance to resize.
        var bufferByteLength = buffer.buffer.Length;
        if (byteOffset > bufferByteLength)
            throw JSEngine.NewRangeError("Start offset is outside the bounds of the buffer.");
        if (hasExplicitLength && byteOffset + byteLength > bufferByteLength)
            throw JSEngine.NewRangeError("Invalid DataView length.");

        // OrdinaryCreateFromConstructor (spec step 10) — surface NewTarget.prototype side effects
        // (e.g. a getter that resizes the underlying buffer) before the spec's re-check below.
        JSArrayBuffer.ForceNewTargetPrototypeAccess();

        // Spec steps 13 / 14a — re-validate against the (possibly resized) buffer.
        bufferByteLength = buffer.buffer.Length;
        if (byteOffset > bufferByteLength)
            throw JSEngine.NewRangeError("Start offset is outside the bounds of the buffer.");

        if (!hasExplicitLength)
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
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s7.1.22; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: a byteOffset or byteLength of -1 or 2^53 is accepted instead of raising a RangeError, or undefined yields anything but 0
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a structured-clone copy built from a view's byteOffset and byteLength exposes a different byte range of the cloned buffer than the source view did
    // Broiler-Human:        PENDING
    public DataView(JSArrayBuffer buffer, int byteOffset, int byteLength) : this()
    {
        this.buffer = buffer;
        explicitByteLength = byteLength;
        this.byteOffset = byteOffset;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: the returned span starts anywhere other than the view's byteOffset plus the requested offset
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> BytesAt(int offset, int length)
        => buffer.buffer.AsSpan(byteOffset + offset, length);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: writing 0x0102 little-endian stores 0x01 as the first byte
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteUInt16(int offset, ushort value, bool littleEndian)
    {
        var bytes = BytesAt(offset, sizeof(ushort));
        if (littleEndian)
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        else
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: writing 0x01020304 big-endian stores 0x04 as the first byte
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteUInt32(int offset, uint value, bool littleEndian)
    {
        var bytes = BytesAt(offset, sizeof(uint));
        if (littleEndian)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: writing 0x0102030405060708 little-endian stores 0x01 as the first byte
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteUInt64(int offset, ulong value, bool littleEndian)
    {
        var bytes = BytesAt(offset, sizeof(ulong));
        if (littleEndian)
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
    }

    [JSExport]
    public JSValue Buffer => buffer;

    // get DataView.prototype.byteLength / byteOffset: an out-of-bounds (or detached) view
    // throws a TypeError rather than reporting a stale length/offset.
    [JSExport]
    public int ByteLength => RequireInBoundsByteLength();

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: reading byteOffset on a view whose buffer was detached or shrunk below the view returns a number instead of raising a TypeError
    // Broiler-Human:        PENDING
    [JSExport]
    public int ByteOffset
    {
        get
        {
            RequireInBoundsByteLength();
            return byteOffset;
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s7.1.22; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an index above int.MaxValue but below 2^53 is narrowed to a value that passes the per-access bounds check, or an index of -1 is accepted
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ToByteOffset(JSValue value)
    {
        // ToIndex (SetViewValue step 4 / GetViewValue step 4): ToNumber then truncate
        // toward zero; NaN/undefined → 0; a negative value or one above 2^53-1 (including
        // ±Infinity) is a RangeError. This runs BEFORE the value coercion in the set*
        // methods, so an out-of-range index is rejected before the value's valueOf is
        // observed (test262: DataView/prototype/set*/index-check-before-value-conversion).
        var number = value.DoubleValue;
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0 || integer > 9007199254740991d) // [0, 2^53 - 1]
            throw JSEngine.NewRangeError("DataView offset is outside the bounds of the buffer.");

        // A valid index that still overflows int is clamped so the per-access bounds
        // check (getIndex + elementSize > viewSize) reports it as out of range.
        return integer > int.MaxValue ? int.MaxValue : (int)integer;
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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getBigInt64 over eight 0xFF bytes returns anything other than -1n
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public JSValue GetBigInt64(in Arguments a) => new JSBigInt(GetInt64(in a));

    //internal method
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s25.3.1.5; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an index whose value plus 8 exceeds the view's byteLength is read instead of raising a RangeError
    // Broiler-Human:        PENDING
    public long GetInt64(in Arguments a)
    {
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var littleEndian = a[1]?.BooleanValue ?? false;

        if (byteOffset < 0 || byteOffset > RequireInBoundsByteLength() - 8)
            ThrowOffsetOutOfBounds(byteOffset);

        var bytes = BytesAt(byteOffset, sizeof(long));
        return littleEndian
            ? BinaryPrimitives.ReadInt64LittleEndian(bytes)
            : BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getBigUint64 over eight 0xFF bytes returns -1n instead of 18446744073709551615n
    // Broiler-Human:        PENDING
    [JSExport("getBigUint64", Length = 1)]
    public JSValue GetBigUInt64(in Arguments a) => new JSBigInt(new BigInteger((ulong)GetInt64(in a)));

    //internal method
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s25.3.1.5; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an index whose value plus 4 exceeds the view's byteLength is read instead of raising a RangeError
    // Broiler-Human:        PENDING
    public int GetInt32Int(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var littleEndian = a[1]?.BooleanValue ?? false;
        
        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 4)
            ThrowOffsetOutOfBounds(byteOffset);

        var bytes = @this.BytesAt(byteOffset, sizeof(int));
        return littleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes)
            : BinaryPrimitives.ReadInt32BigEndian(bytes);
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: the big-endian bytes 3F 80 00 00 read as anything other than 1
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public JSValue GetFloat32(in Arguments a)
    {
        int temp = GetInt32Int(in a);
        return new JSNumber(BitConverter.Int32BitsToSingle(temp));
    }

    /// <summary>
    /// Gets a 16-bit floating point number (half-precision) at the specified byte offset
    /// from the start of the DataView (ES2025 §2.8).
    /// </summary>
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: the big-endian bytes 3C 00 read as anything other than 1, or the bytes BC 00 read as a positive number
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public JSValue GetFloat16(in Arguments a)
    {
        int temp = GetInt16Int(in a);
        var half = BitConverter.UInt16BitsToHalf((ushort)temp);
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: the big-endian bytes 3F F0 followed by six zero bytes read as anything other than 1
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public JSValue GetFloat64(in Arguments a)
    {
        long temp = GetInt64(in a);
        return new JSNumber(BitConverter.Int64BitsToDouble(temp));
    }

    //internal
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s25.3.1.5; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: the big-endian bytes FF FE yield 65534 instead of -2, or an index whose value plus 2 exceeds the view's byteLength is read
    // Broiler-Human:        PENDING
    public int GetInt16Int(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var littleEndian = a[1]?.BooleanValue ?? false;

        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 2)
            ThrowOffsetOutOfBounds(byteOffset);

        var bytes = @this.BytesAt(byteOffset, sizeof(short));
        return littleEndian
            ? BinaryPrimitives.ReadInt16LittleEndian(bytes)
            : BinaryPrimitives.ReadInt16BigEndian(bytes);
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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getInt16 over the big-endian bytes 80 00 returns 32768 instead of -32768
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getInt32 over four 0xFF bytes returns 4294967295 instead of -1
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getInt8 over the byte 0x80 returns 128 instead of -128
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public JSValue GetInt8(in Arguments a) => new JSNumber(GetInt8Int(in a));

    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 s25.3.1.5; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an index equal to the view's byteLength is read instead of raising a RangeError
    // Broiler-Human:        PENDING
    public int GetInt8Int(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);

        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 1)
            ThrowOffsetOutOfBounds(byteOffset);

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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getUint16 over the big-endian bytes FF FE returns -2 instead of 65534
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: getUint32 over four 0xFF bytes returns -1 instead of 4294967295
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: an index equal to the view's byteLength is read instead of raising a RangeError, or the byte 0xFF reads as -1
    // Broiler-Human:        PENDING
    [JSExport(Length = 1)]
    public JSValue GetUint8(in Arguments a)
    {
        var @this = this;
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        
        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - 1)
            ThrowOffsetOutOfBounds(byteOffset);

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
    // RawBitsFor reduces a (ToBigInt-coerced) value to its low 64 bits — the
    // two's-complement 8-byte pattern shared by setBigInt64/setBigUint64. A plain
    // (long)/(ulong) cast on JSBigInt.BigIntValue overflows for magnitudes that do
    // not fit a signed 64-bit integer, so mask the BigInteger directly.
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: -1n yields anything other than 0xFFFFFFFFFFFFFFFF, or 2 to the 64th plus 5 yields anything other than 5
    // Broiler-Human:        PENDING
    private static ulong RawBitsFor(JSValue value)
    {
        var big = value is JSBigInt bigint ? bigint.value : new System.Numerics.BigInteger(value.BigIntValue);
        return (ulong)(big & ((BigInteger.One << 64) - 1));
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=4; Fingerprint=TBF
    // Broiler-Falsified-If: setBigInt64 with 2^63 stores anything other than 80 followed by seven zero bytes big-endian
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetBigInt64(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 8, bigInt: true);
        @this.WriteUInt64(byteOffset, RawBitsFor(value), littleEndian);
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=4; Fingerprint=TBF
    // Broiler-Falsified-If: setBigUint64 with -1n stores anything other than eight 0xFF bytes
    // Broiler-Human:        PENDING
    [JSExport("setBigUint64", Length = 2)]
    public JSValue SetBigUInt64(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 8, bigInt: true);
        @this.WriteUInt64(byteOffset, RawBitsFor(value), littleEndian);
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setFloat32 with 1 stores anything other than 3F 80 00 00 big-endian
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetFloat32(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 4);
        @this.WriteUInt32(byteOffset, (uint)BitConverter.SingleToInt32Bits((float)value.DoubleValue), littleEndian);
        return JSUndefined.Value;
    }

    /// <summary>
    /// Stores a 16-bit floating point (half-precision) value at the specified
    /// byte offset from the start of the DataView (ES2025 §2.8).
    /// </summary>
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setFloat16 with 1 stores anything other than 3C 00 big-endian, or a value halfway between two halves is not rounded to even
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetFloat16(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 2);
        var half = (Half)value.DoubleValue;
        @this.WriteUInt16(byteOffset, BitConverter.HalfToUInt16Bits(half), littleEndian);
        return JSUndefined.Value;
    }


    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setFloat64 with 1 stores anything other than 3F F0 followed by six zero bytes big-endian
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetFloat64(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 8);
        @this.WriteUInt64(byteOffset, (ulong)BitConverter.DoubleToInt64Bits(value.DoubleValue), littleEndian);
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setInt16 with 65537 stores anything other than 00 01 big-endian
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetInt16(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 2);
        @this.WriteUInt16(byteOffset, (ushort)value.IntValue, littleEndian);
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setInt32 with 4294967297 stores anything other than 00 00 00 01 big-endian
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetInt32(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 4);
        @this.WriteUInt32(byteOffset, (uint)value.IntValue, littleEndian);
        return JSUndefined.Value;
    }


    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setInt8 with -1 stores anything other than 0xFF at the view's byteOffset plus the index
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetInt8(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 1);
        var bytes = (byte)(sbyte)(uint)value.IntValue;

        @this.buffer.buffer[@this.byteOffset + byteOffset] = bytes;
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setUint16 with -1 stores anything other than FF FF
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetUint16(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 2);
        @this.WriteUInt16(byteOffset, (ushort)value.IntValue, littleEndian);
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setUint32 with 4294967295 stores anything other than FF FF FF FF
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetUint32(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 4);
        @this.WriteUInt32(byteOffset, (uint)value.IntValue, littleEndian);
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: setUint8 with 256 stores anything other than 0
    // Broiler-Human:        PENDING
    [JSExport(Length = 2)]
    public JSValue SetUint8(in Arguments a)
    {
        var (byteOffset, littleEndian, @this, value) = GetSetArgs(in a, 1);
        var bytes = (byte)(uint)value.IntValue;

        @this.buffer.buffer[@this.byteOffset + byteOffset] = bytes;
        return JSUndefined.Value;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s25.3.1.6; IP=Low; Security=Medium; Resources=4; Fingerprint=TBF
    // Broiler-Falsified-If: a value whose valueOf shrinks a resizable buffer is still written at an index past the view's new end
    // Broiler-Human:        PENDING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int byteOffset, bool littleEndian, DataView dataView, JSValue value) GetSetArgs(in Arguments a, int length, bool bigInt = false)
    {
        var @this = this;

        // The immutability of the backing buffer is observable independently of the
        // arguments, so it must be checked before any argument coercion (which can run
        // user code via valueOf/toString). See DataView.prototype.set* immutable-buffer
        // tests in test262.
        if (@this.buffer.isImmutable)
            ThrowImmutableBuffer();

        // An omitted byteOffset is ToIndex(undefined) = 0 and an omitted value is undefined
        // (coerced per type); neither argument is required.
        var byteOffset = ToByteOffset(a[0] ?? JSUndefined.Value);
        var value = a[1] ?? JSUndefined.Value;

        // SetViewValue coerces the value (step 6 — ToBigInt for the BigInt element types,
        // ToNumber otherwise) BEFORE the out-of-bounds RangeError check (step 13). So
        // setBigInt64(0) with no value is a TypeError (ToBigInt(undefined)), and a value
        // whose valueOf throws is observed even when the index is out of range
        // (test262: DataView/prototype/setUint8/range-check-after-value-conversion).
        value = bigInt
            ? JSBigInt.Coerce(value)
            : value.IsNumber ? value : CreateNumber(value.DoubleValue);

        var littleEndian = a[2]?.BooleanValue ?? false;

        if (byteOffset < 0 || byteOffset > @this.RequireInBoundsByteLength() - length)
            ThrowOffsetOutOfBounds(byteOffset);

        return (byteOffset, littleEndian, @this, value);
    }
}
