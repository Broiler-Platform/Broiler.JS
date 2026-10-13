// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsBinary.cs
//     blob 15476b8ff93bdd9ea13a4ddfab5ff64bba09cc26: the enum JsElementKind whole; of the static class JsElements, Low64, Narrow and Clamp; the static class JsFloat16 whole
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the static class JsElements holds only Low64, Narrow and Clamp, its other members being the engine's; JsElementKind is copied because Narrow takes it; the classes JsArrayBuffer, JsDataView and JsTypedArray are not copied;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - JsElements.Clamp and JsFloat16.QuietNaN made internal (private in the source) so that the comparison can call and read them;
//   - Narrow's JsBigInt is this arm's copy of JsBigInt (JsBigInt.cs beside this file), not the engine's;
//   - the using directives the copied members do not need (System.Buffers.Binary, System.Collections.Generic, Broiler.ECMAScript.Numbers) dropped.

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>The twelve element types this realm's typed arrays store.</summary>
/// <remarks>
/// <para>
/// <b>Twelve since 2026-09-22 (JSeal B07); it was ten.</b> <c>BigInt64Array</c> and
/// <c>BigUint64Array</c> were absent while the realm had no BigInt: their elements read as a
/// BigInt, and a realm without one could only have answered a Number and lost the top bits the
/// kinds exist to carry. BigInt was admitted by card B05 (decision JSD-0033 section 7), so the two
/// kinds exist now - and exist only in a realm whose composition admits BIGINT as well as the
/// binary surface, because reading an element makes a BigInt value. Their elements move as a
/// <see cref="JsValue"/> holding a BigInt and never as a <c>double</c>: the Number path refuses
/// them by name (<see cref="JsElements.Read"/>) rather than answering a rounded value.
/// <i>(The superseded paragraph read "Ten and not twelve ... this realm has no BigInt"; it stopped
/// being true with B05 and is quoted rather than deleted.)</i>
/// </para>
/// <para>
/// <b><c>Uint8Clamped</c> is a kind and not a flag on <c>Uint8</c>.</b> The two read identically -
/// one byte, zero to 255 - and differ only in how a write converts, which is exactly the shape a
/// separate kind expresses without a second field that every read would have to ignore.
/// </para>
/// </remarks>
internal enum JsElementKind : byte
{
    /// <summary>A signed byte, written by the language's modular <c>ToInt32</c> wrap.</summary>
    Int8 = 0,

    /// <summary>An unsigned byte, written by the same modular wrap.</summary>
    Uint8 = 1,

    /// <summary>An unsigned byte whose write CLAMPS to 0..255 rather than wrapping.</summary>
    Uint8Clamped = 2,

    /// <summary>A signed 16-bit integer.</summary>
    Int16 = 3,

    /// <summary>An unsigned 16-bit integer.</summary>
    Uint16 = 4,

    /// <summary>A signed 32-bit integer.</summary>
    Int32 = 5,

    /// <summary>An unsigned 32-bit integer.</summary>
    Uint32 = 6,

    /// <summary>An IEEE 754 binary32, which a write narrows to and a read widens from.</summary>
    Float32 = 7,

    /// <summary>An IEEE 754 binary64: the language's own Number, stored exactly.</summary>
    Float64 = 8,

    /// <summary>
    /// An IEEE 754 binary16, which a write rounds to through <see cref="JsFloat16"/> and a read
    /// widens from exactly.
    /// </summary>
    /// <remarks>
    /// It is numbered after <see cref="Float64"/> rather than beside <see cref="Float32"/> so that
    /// the value of every kind that existed before it is unchanged; nothing orders the kinds by
    /// their value.
    /// </remarks>
    Float16 = 9,

    /// <summary>
    /// A signed 64-bit integer whose element is a BigInt, written by the language's
    /// <c>ToBigInt64</c> (the value modulo 2**64, read back as two's complement).
    /// </summary>
    /// <remarks>
    /// Numbered after every Number kind for the reason <see cref="Float16"/> is: no existing kind's
    /// value moves.
    /// </remarks>
    BigInt64 = 10,

    /// <summary>
    /// An unsigned 64-bit integer whose element is a BigInt, written by <c>ToBigUint64</c>.
    /// </summary>
    BigUint64 = 11,
}

/// <summary>
/// What each <see cref="JsElementKind"/> is worth in bytes, what it is called, and the one pair of
/// routines that moves a Number in and out of those bytes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every byte goes through <c>BinaryPrimitives</c>, never through <c>BitConverter</c>.</b> The
/// language says a typed array's bytes are the platform's order and a <c>DataView</c>'s are
/// whichever the caller asked for, and this profile answers "little-endian" for the platform on
/// every machine. <c>BitConverter</c> would answer the host's order instead, which means the same
/// program produces different bytes on a big-endian host and a conformance run that passes on the
/// developer's laptop says nothing about the one that does not. Declaring one order and writing it
/// explicitly costs nothing on the hosts anybody runs and makes the answer a property of the
/// profile rather than of the machine.
/// </para>
/// <para>
/// <b>The write conversions are the language's, not the CLR's.</b> A C# cast from <c>double</c> to
/// <c>int</c> is saturating and undefined for NaN; the language's <c>ToInt32</c> is modular and
/// answers zero for NaN, so <c>new Int8Array(1)[0] = 1e30</c> is 0 and not 127. Every integer kind
/// therefore goes through <see cref="JsValue.ToUint32(double)"/> and keeps the low bits, and only
/// <see cref="JsElementKind.Uint8Clamped"/> - which the specification defines as a clamp with
/// round-half-to-even - departs from that.
/// </para>
/// </remarks>
internal static class JsElements
{
    /// <summary>
    /// The low 64 bits of <paramref name="value"/> in two's complement: <paramref name="value"/>
    /// modulo 2**64.
    /// </summary>
    /// <remarks>
    /// A value that fits a <c>long</c> or a <c>ulong</c> converts directly; anything wider is masked,
    /// which reads every word of it. The engine's element conversion narrows a wide value ONCE,
    /// charged, with <see cref="Narrow"/>, so a write repeated over many elements - <c>fill</c> -
    /// never masks a wide value more than once.
    /// </remarks>
    internal static ulong Low64(BigInteger value)
    {
        if (value >= long.MinValue && value <= long.MaxValue)
        {
            return unchecked((ulong)(long)value);
        }

        if (value.Sign > 0 && value <= ulong.MaxValue)
        {
            return (ulong)value;
        }

        // BigInteger's AND IS TWO'S COMPLEMENT WITH AN INFINITE SIGN, so a negative value masks to
        // exactly its residue modulo 2**64.
        return (ulong)(value & ulong.MaxValue);
    }

    /// <summary>
    /// <paramref name="value"/> reduced to what an element of <paramref name="kind"/> would read
    /// back: <c>BigInt.asIntN(64, v)</c> or <c>BigInt.asUintN(64, v)</c>.
    /// </summary>
    /// <remarks>
    /// Storing the narrowed value writes exactly the bytes storing the original would, so a caller
    /// that converts once and writes many times narrows once. A value already in range is answered
    /// as itself.
    /// </remarks>
    internal static JsBigInt Narrow(JsElementKind kind, JsBigInt value)
    {
        var low = Low64(value.Value);
        var narrowed = kind == JsElementKind.BigInt64
            ? new BigInteger(unchecked((long)low))
            : new BigInteger(low);

        return narrowed == value.Value ? value : new JsBigInt(narrowed);
    }

    /// <summary>The clamp <see cref="JsElementKind.Uint8Clamped"/> writes through.</summary>
    /// <remarks>
    /// Round-half-to-EVEN, which is the one place the language rounds that way: 0.5 stores 0 and
    /// 1.5 stores 2. Every other rounding in the language is half-up, so a reader who assumes
    /// <c>Math.round</c>'s rule here gets 1 and 2 and a picture one grey level off.
    /// </remarks>
    internal static byte Clamp(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return value >= 255 ? (byte)255 : (byte)Math.Round(value, MidpointRounding.ToEven);
    }
}

/// <summary>
/// The one conversion between a Number and an IEEE 754 binary16, which <c>Math.f16round</c>,
/// <c>DataView.prototype.getFloat16</c>/<c>setFloat16</c> and <c>Float16Array</c> all go through.
/// </summary>
/// <remarks>
/// <para>
/// <b>The narrowing is the platform's <c>(System.Half)double</c>, and that is a verified claim
/// rather than an assumption.</b> The language rounds a Number to binary16 ONCE, directly, ties to
/// even; a conversion that went through <c>float</c> first would round twice and answer wrongly
/// for a double just above a binary16 midpoint - <c>1 + 2**-11 + 2**-40</c> becomes the tie
/// <c>1 + 2**-11</c> in binary32 and then 1, where the correct answer is <c>1 + 2**-10</c>. The
/// runtime this profile targets converts from the double directly; it was checked on 2026-09-21
/// against an integer-arithmetic reference over every binary16 value, every midpoint between two
/// neighbours, the doubles either side of each midpoint and random points between neighbours, with
/// no disagreement, and the probe <c>src/tests/differential/the-float16-surface.js</c> repeats the
/// boundary sweep in the guest on every run. Should a runtime change that, the probe is what fails.
/// </para>
/// <para>
/// <b>What the rounding answers at the edges, all of which is IEEE 754's and the language's.</b>
/// A magnitude at or above 65520 - the midpoint between the largest finite value 65504 and the
/// next power of two - becomes an infinity of the same sign; a magnitude at or below 2**-25 - half
/// the smallest subnormal - becomes a zero of the same sign, so a negative number that underflows
/// is <c>-0</c>; subnormals are kept rather than flushed; infinities and signed zeros are exact.
/// </para>
/// <para>
/// <b>Every NaN is written as the one quiet NaN <c>0x7E00</c>.</b> The specification lets an
/// implementation choose which NaN encoding a write stores, and choosing one makes the bytes a
/// property of the profile rather than of which NaN the guest happened to compute. A read is
/// exact in the other direction: every binary16 value, NaN included, widens to a double without
/// rounding.
/// </para>
/// </remarks>
internal static class JsFloat16
{
    /// <summary>The quiet NaN every NaN is stored as.</summary>
    internal const ushort QuietNaN = 0x7E00;

    /// <summary>The binary16 bits <paramref name="value"/> rounds to, ties to even.</summary>
    internal static ushort Encode(double value) =>
        double.IsNaN(value)
            ? QuietNaN
            : BitConverter.HalfToUInt16Bits((Half)value);

    /// <summary>The Number the binary16 <paramref name="bits"/> denote, which is exact.</summary>
    internal static double Decode(ushort bits) =>
        (double)BitConverter.UInt16BitsToHalf(bits);

    /// <summary>
    /// <paramref name="value"/> rounded to the nearest binary16 and widened back: <c>Math.f16round</c>.
    /// </summary>
    internal static double Round(double value) => Decode(Encode(value));
}
