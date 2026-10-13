// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSInt16Array.cs
//     blob 5baf22909cf6ec3571adc390a169ac8517cba9fd: SetValue's narrowing (:49, :52)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSUInt16Array.cs
//     blob a9a0e55e214749b4e0b1d5fe8729c112323af0ad: SetValue's narrowing (:31, :34)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSInt8Array.cs
//     blob e90a5cee8329a7585de921311d9afdd45f357f5f: SetValue's narrowing (:48, :51)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSUInt8Array.cs
//     blob d036d9c878fbcae569cb6673c23b9764989a172d: SetValue's narrowing (:38, :41)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSUint8ClampedArray.cs
//     blob 28f48497ebaf3ee586736920f8d2b378ced5739a: SetValue's ToUint8Clamp (:48-69)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSBigInt64Array.cs
//     blob db615f3a36ba298229114481bc8b4110abe88606: SetValue's 64-bit mask (:35-38)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSBigUint64Array.cs
//     blob e8ee65cf73cdd85eb355c40fad364ccea2a3a32b: SetValue's 64-bit mask (:35-37)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/DataView/DataView.cs
//     blob f1ba2dee735e5af368bb96b2192582bd60a7045c: RawBitsFor (:548-559); the narrowing of SetInt16 (:629),
//     SetInt8 (:652), SetUint16 (:665) and SetUint8 (:687)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/String/JSStringStatic.cs
//     blob e7b8ed420668a102e7eb0da68502b7af2914db39: FromCharCode's ToUint16 (:23)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Objects/JSAtomics.cs
//     blob bff6404d1209bb56aac2527162bc4e8bd0b3a291: ElementKind (:25), TruncateToElement (:125-134) and
//     TruncateToBigElement (:136-142)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; the narrowing and clamping that the typed arrays' SetValue, DataView's set* methods and
//     String.fromCharCode do inline sit in a static class of their own, one method per site, named
//     <source class>_<source member>; JSAtomics' members keep their names behind a JSAtomics_ prefix and its
//     private ElementKind is an internal enum here;
//   - each site takes the value the engine has after its coercion: the double of ToNumber (`value.DoubleValue`),
//     or for the BigInt sites the BigInteger of ToBigInt (`ToBigIntValue(value).value`, `JSBigInt.Coerce`);
//     `value.IntValue` (JSValue's, not overridden by JSNumber) is a call to this arm's JSValue.IntValue, and
//     `(value ?? JSUndefined.Value)`, TrySetForeignReceiver, the bounds checks and the byte writes are the
//     adapter's: each method returns the value the site writes (a byte, or the 16- or 64-bit pattern), and
//     reading it back (JSInt8Array's GetValue reads its byte as (sbyte)) is the adapter's too;
//   - RawBitsFor's value is always a JSBigInt (GetSetArgs coerces it with JSBigInt.Coerce when bigInt is set),
//     so its `value is JSBigInt bigint ? bigint.value : new BigInteger(value.BigIntValue)` is the parameter big;
//   - the Broiler Code Assurance annotations on the copied members are not copied.

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>The typed arrays', DataView's and String.fromCharCode's narrowing, clamping and 64-bit masks.</summary>
internal static class TypedArrays
{
    // JSInt16Array.SetValue (:49, :52).
    internal static short JSInt16Array_SetValue(double value)
    {
        var intValue = JSValue.IntValue(value);
        return (short)intValue;
    }

    // JSUInt16Array.SetValue (:31, :34).
    internal static ushort JSUInt16Array_SetValue(double value)
    {
        var intValue = JSValue.IntValue(value);
        return (ushort)intValue;
    }

    // JSInt8Array.SetValue (:48, :51): the byte stored; GetValue reads it as (sbyte).
    internal static byte JSInt8Array_SetValue(double value)
    {
        var intValue = JSValue.IntValue(value);
        return (byte)intValue;
    }

    // JSUInt8Array.SetValue (:38, :41).
    internal static byte JSUInt8Array_SetValue(double value)
    {
        var intValue = JSValue.IntValue(value);
        return (byte)(uint)intValue;
    }

    // JSUint8ClampedArray.SetValue (:48-69).
    internal static byte JSUint8ClampedArray_SetValue(double value)
    {
        double number = value;
        // This algorithm is defined as ToUint8Clamp in the spec.
        int result;
        if (number <= 0)
            result = 0;
        else if (number >= 255)
            result = 255;
        else
        {
            var f = Math.Floor(number);
            if (f + 0.5 < number)
                result = (int)f + 1;
            else if (number < f + 0.5)
                result = (int)f;
            else if ((int)f % 2 == 0)
                result = (int)f;
            else
                result = (int)f + 1;
        }
        return (byte)result;
    }

    // JSBigInt64Array.SetValue (:35-38): the eight bytes written, as the ulong BitConverter writes.
    internal static ulong JSBigInt64Array_SetValue(BigInteger value)
    {
        // ToBigInt64 wraps modulo 2^64; a plain (long) cast on the BigInteger
        // overflows for magnitudes that do not fit a signed 64-bit integer, so
        // mask the low 64 bits (the two's-complement pattern) directly.
        var bits = (ulong)(value & ((BigInteger.One << 64) - 1));
        return bits;
    }

    // JSBigUint64Array.SetValue (:35-37): textually the same expression as JSBigInt64Array's.
    internal static ulong JSBigUint64Array_SetValue(BigInteger value)
    {
        // ToBigUint64 wraps modulo 2^64; a plain (ulong) cast on the BigInteger
        // overflows for magnitudes outside [0, 2^64), so mask the low 64 bits.
        var bits = (ulong)(value & ((BigInteger.One << 64) - 1));
        return bits;
    }

    // RawBitsFor reduces a (ToBigInt-coerced) value to its low 64 bits — the
    // two's-complement 8-byte pattern shared by setBigInt64/setBigUint64. A plain
    // (long)/(ulong) cast on JSBigInt.BigIntValue overflows for magnitudes that do
    // not fit a signed 64-bit integer, so mask the BigInteger directly.
    internal static ulong DataView_RawBitsFor(BigInteger big)
    {
        return (ulong)(big & ((BigInteger.One << 64) - 1));
    }

    // DataView.SetInt16 (:629): the 16-bit pattern WriteUInt16 stores.
    internal static ushort DataView_SetInt16(double value) => (ushort)JSValue.IntValue(value);

    // DataView.SetUint16 (:665): textually the same expression as SetInt16's.
    internal static ushort DataView_SetUint16(double value) => (ushort)JSValue.IntValue(value);

    // DataView.SetInt8 (:652).
    internal static byte DataView_SetInt8(double value)
    {
        var bytes = (byte)(sbyte)(uint)JSValue.IntValue(value);
        return bytes;
    }

    // DataView.SetUint8 (:687).
    internal static byte DataView_SetUint8(double value)
    {
        var bytes = (byte)(uint)JSValue.IntValue(value);
        return bytes;
    }

    // JSString.FromCharCode (JSStringStatic.cs :23): the code unit appended for one argument.
    internal static char JSStringStatic_FromCharCode(double value) => (char)JSValue.IntValue(value);

    internal enum ElementKind { Int8, Uint8, Int16, Uint16, Int32, Uint32, BigInt64, BigUint64 }

    internal static long JSAtomics_TruncateToElement(ElementKind kind, long value) => kind switch
    {
        ElementKind.Int8 => (sbyte)value,
        ElementKind.Uint8 => (byte)value,
        ElementKind.Int16 => (short)value,
        ElementKind.Uint16 => (ushort)value,
        ElementKind.Int32 => (int)value,
        ElementKind.Uint32 => (uint)value,
        _ => value,
    };

    internal static BigInteger JSAtomics_TruncateToBigElement(ElementKind kind, BigInteger value)
    {
        var masked = value & ((BigInteger.One << 64) - 1);
        if (kind == ElementKind.BigInt64 && masked >= (BigInteger.One << 63))
            masked -= BigInteger.One << 64;
        return masked;
    }
}
