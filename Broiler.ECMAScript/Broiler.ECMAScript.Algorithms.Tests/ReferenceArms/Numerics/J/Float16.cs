// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSFloat16Array.cs
//     blob a993a97c7cc519471c980e74ee69621b77f6de55: GetValue's decode (:23-24) and SetValue's encode (:32, :35)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/DataView/DataView.cs
//     blob f1ba2dee735e5af368bb96b2192582bd60a7045c: GetFloat16's decode (:374-375) and SetFloat16's encode (:605-606)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Objects/JSMath.cs
//     blob 2a886f800921f506a36be355e461a294514f7de5: F16round (:325-337)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; the sites sit in a static class of their own, one method per site, named
//     <source class>_<source member>;
//   - each encode takes the double of ToNumber (`value.DoubleValue`) and returns the 16 bits the site stores:
//     JSFloat16Array writes the Half with BitConverter.TryWriteBytes into the element's two bytes, so its copy
//     writes it into a two-byte span the same way and returns BitConverter.ToUInt16 of those bytes (host byte
//     order on both sides); DataView's is BitConverter.HalfToUInt16Bits, as the source computes it, and its
//     WriteUInt16 (the endianness) is the adapter's;
//   - each decode takes the 16 bits and returns the Number's double (`new JSNumber((double)half)` is
//     `(double)half`): JSFloat16Array reads the Half with BitConverter.ToHalf from the element's two bytes, so its
//     copy writes the bits into a two-byte span with BitConverter.TryWriteBytes and reads them that way;
//     DataView's GetInt16Int (the byte read and endianness) is the adapter's, and its `(ushort)temp` is the
//     ushort parameter bits;
//   - F16round takes the double of ToNumber (`args.Get1().DoubleValue`) and returns `r`, not `new JSNumber(r)`;
//     its [JSExport("f16round")] attribute is not copied.

using System;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>Float16Array's, DataView's and Math.f16round's binary16 conversions.</summary>
internal static class Float16
{
    // JSFloat16Array.SetValue (:32, :35).
    internal static ushort JSFloat16Array_SetValue(double value)
    {
        var half = (Half)value;
        Span<byte> bytes = stackalloc byte[2];
        BitConverter.TryWriteBytes(bytes, half);
        return BitConverter.ToUInt16(bytes);
    }

    // JSFloat16Array.GetValue (:23-24).
    internal static double JSFloat16Array_GetValue(ushort bits)
    {
        Span<byte> bytes = stackalloc byte[2];
        BitConverter.TryWriteBytes(bytes, bits);
        var half = BitConverter.ToHalf(bytes);
        return (double)half;
    }

    // DataView.SetFloat16 (:605-606).
    internal static ushort DataView_SetFloat16(double value)
    {
        var half = (Half)value;
        return BitConverter.HalfToUInt16Bits(half);
    }

    // DataView.GetFloat16 (:374-375).
    internal static double DataView_GetFloat16(ushort bits)
    {
        var half = BitConverter.UInt16BitsToHalf(bits);
        return (double)half;
    }

    /// <summary>
    /// ES2025 §2.8 — Math.f16round(x)
    /// Rounds a number to the nearest IEEE 754 half-precision (16-bit) float.
    /// </summary>
    internal static double JSMath_F16round(double d)
    {
        var r = (double)(Half)d;

        return r;
    }
}
