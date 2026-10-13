using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using J = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;
using V = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;

/// <summary>
/// One arm's implementation of one operation, with the shared implementation it is compared with:
/// S called as that arm's engine will call it (under its BigInt policy, with or without its meter,
/// projected onto what the arm's member answers). <see cref="Applies"/> says which inputs the arm's
/// engine can hand the member at all.
/// </summary>
internal sealed record NumericsVariant(
    string Arm, string Name, Func<NumericsInput, string> Run, Func<NumericsInput, string> Shared,
    Func<NumericsInput, bool>? Applies = null);

/// <summary>An operation of the rule: the classes it runs over, how a line becomes its inputs, and its variants.</summary>
internal sealed record NumericsOperation(
    string Name, string[] Classes, Func<NumericsInput, IEnumerable<NumericsInput>> Expand, NumericsVariant[] Variants);

/// <summary>
/// The operations of bundle sjl-4-001's reference-arm rule, as its revision of 2026-10-13 reads them,
/// and for each the shared implementation (arm S) and the arms' variants. Every result is rendered by
/// <see cref="Render"/>, so the comparison is exact.
/// </summary>
/// <remarks>
/// <para>
/// <b>An arm is called as its engine calls the member.</b> Where an engine's call site does more than
/// call the member - Broiler.JS's NudgeToCalendarUnit computes the comparison and the parity it hands
/// <c>ApplyRoundingPicksEnd</c>, Broiler.VM's <c>length</c> passes <c>ToInteger</c>'s result to
/// <c>ArrayToLength</c> - the variant repeats those lines, and says so.
/// </para>
/// <para>
/// <b>S under two policies.</b> Every BigInt variant of arm V is compared with S under Broiler.VM's
/// width policy (2^20 bits) and its meter, the total charge after a bar; every variant of arm J with S
/// under the unbounded policy and no meter. A refusal past either policy is the RangeError each engine
/// raises for it.
/// </para>
/// </remarks>
internal static class NumericsArms
{
    public const string Shared = "S";

    public static readonly BigIntPolicy VmPolicy = new(1L << 20);

    /// <summary>The widest result arm J and S under the unbounded policy are asked to compute (the revision's 2^24 bits).</summary>
    public const long UnboundedComputeLimit = 1L << 24;

    /// <summary>The widest value arm J's quadratic radix text and prefixed parse are run on, besides the first drawn values.</summary>
    public const long JTextLimit = 4101;

    /// <summary>How many of B1V's drawn values arm J's radix text and prefixed parse run on.</summary>
    public const int JTextDrawn = 256;

    /// <summary>B1V's drawn values, which the generator writes last.</summary>
    public const int B1Drawn = 4096;

    public static IReadOnlyList<(string Arm, string Operation, string Reason)> NotImplemented { get; } =
    [
        ("V", "ToInt16", "no member of arm V's list narrows to 16 bits: the engine narrows ToUint32's result inside JsElements.Write, which the rule does not copy"),
        ("V", "ToUint16", "as ToInt16"),
        ("V", "ToInt8", "as ToInt16"),
        ("V", "ToUint8", "as ToInt16"),
        ("V", "Float16 encode (payload policy)", "Broiler.VM's policy is the canonical NaN; arm V is compared under that policy"),
        ("J", "Float16 encode (canonical policy)", "Broiler.JS's policy keeps the payload; arm J is compared under that policy"),
        ("V", "Rounding-mode name", "no member of arm V's list parses an option name: the parse is in JsTemporal.Options and JsRealm.NumberFormat, which the rule does not copy"),
        ("J", "TemporalRoundingOptions.GetRoundingMode", "it reads options[\"roundingMode\"]; its string part, NormalizeRoundingMode, is copied and compared"),
        ("V", "BigInt operands wider than 2^20 bits", "Broiler.VM cannot hold such a value (JsBigInt's constructor refuses it), so arm V is not called with one"),
        ("J", "BigInt results wider than 2^24 bits", "the revision of 2026-10-13: arm J and S under the unbounded policy are not asked to build them"),
        ("J", "BigInt::toString at a radix other than 10, and StringToBigInt's prefixed texts, of values wider than 4,101 bits beyond the first 256 drawn", "the revision of 2026-10-13: arm J's radix text and prefixed parse are quadratic"),
        ("S,J,V", "Divisors of 0n and negative exponents", "the engines raise the RangeError before calling the kernel (convention 2)"),
        ("S,J,V", "Comparison with NaN or an infinity", "each engine's adapter answers it before calling the kernel"),
    ];

    public static IReadOnlyList<NumericsOperation> Operations { get; } = Build();

    // ---------------------------------------------------------------------------------------------
    // Inputs.

    private static IEnumerable<NumericsInput> Once(NumericsInput input) => [input];

    private static Func<NumericsInput, IEnumerable<NumericsInput>> Each(params int[] parameters) =>
        input => parameters.Select(p => input with { Parameter = p, Detail = p.ToString(CultureInfo.InvariantCulture) });

    private static int[] Range(int from, int to) => Enumerable.Range(from, to - from + 1).ToArray();

    private static readonly string[] ModeNames = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"];

    private static RoundingMode SMode(int parameter) => (RoundingMode)parameter;

    private static V.TemporalRounding VMode(int parameter) => Enum.Parse<V.TemporalRounding>(((RoundingMode)parameter).ToString());

    private static string Name(int parameter) => ModeNames[parameter];

    /// <summary>Every mode, at both signs where <paramref name="signed"/>: the parameter is the mode, the second the sign.</summary>
    private static Func<NumericsInput, IEnumerable<NumericsInput>> Modes(bool signed) => input =>
        from mode in Range(0, 8)
        from negative in signed ? new[] { 0, 1 } : new[] { 0 }
        select input with { Parameter = mode, Parameter2 = negative, Detail = Name(mode) + (signed ? (negative == 1 ? "/negative" : "/positive") : string.Empty) };

    private static readonly string[] UnsignedNames = ["zero", "infinity", "half-zero", "half-infinity", "half-even"];

    private static string Unsigned(UnsignedRoundingMode mode) => UnsignedNames[(int)mode];

    private static bool Fits(BigInteger value) => value.GetBitLength() <= V.JsBigInt.MaximumBits;

    // ---------------------------------------------------------------------------------------------
    // Results.

    private static string Status(ResultStatus status, BigInteger value) =>
        status == ResultStatus.Completed ? Render.Big(value) : Render.RangeError;

    private delegate ResultStatus SBinary(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result);

    private delegate ResultStatus SUnary(BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result);

    private static string SVm(SBinary op, BigInteger left, BigInteger right)
    {
        var meter = new Meter();
        var status = op(left, right, VmPolicy, meter.Charge, out var result);
        return Render.Charged(Status(status, result), meter.Total);
    }

    private static string SUnbounded(SBinary op, BigInteger left, BigInteger right) =>
        Status(op(left, right, BigIntPolicy.Unbounded, null, out var result), result);

    private static string SVm(SUnary op, BigInteger value)
    {
        var meter = new Meter();
        var status = op(value, VmPolicy, meter.Charge, out var result);
        return Render.Charged(Status(status, result), meter.Total);
    }

    private static string SUnbounded(SUnary op, BigInteger value) =>
        Status(op(value, BigIntPolicy.Unbounded, null, out var result), result);

    private static string VRun(Func<V.JsBigInt, V.JsBigInt, Action<ulong>, V.JsBigInt?> op, BigInteger left, BigInteger right)
    {
        var meter = new Meter();
        var result = op(new V.JsBigInt(left), new V.JsBigInt(right), meter.Charge);
        return Render.Charged(result is null ? Render.RangeError : Render.Big(result.Value), meter.Total);
    }

    private static string VRun(Func<V.JsBigInt, Action<ulong>, V.JsBigInt?> op, BigInteger value)
    {
        var meter = new Meter();
        var result = op(new V.JsBigInt(value), meter.Charge);
        return Render.Charged(result is null ? Render.RangeError : Render.Big(result.Value), meter.Total);
    }

    /// <summary>Whether a power's result is proven wider than <see cref="UnboundedComputeLimit"/> from its operands' widths.</summary>
    private static bool PowerTooWideToCompute(BigInteger @base, BigInteger exponent) =>
        BigInteger.Abs(@base) >= 2 && exponent >= 0 && exponent <= int.MaxValue
        && (long)exponent * (@base.GetBitLength() - 1) > UnboundedComputeLimit;

    // ---------------------------------------------------------------------------------------------

    private static List<NumericsOperation> Build()
    {
        var operations = new List<NumericsOperation>();
        operations.AddRange(IntegerConversionOperations());
        operations.AddRange(Float16Operations());
        operations.AddRange(MathOperations());
        operations.AddRange(RoundingOperations());
        operations.AddRange(BigIntOperations());
        return operations;
    }

    // ---- Integer conversions --------------------------------------------------------------------

    private static IEnumerable<NumericsOperation> IntegerConversionOperations()
    {
        string[] n1 = ["N1"];
        static string S32(NumericsInput i) => Render.Int(IntegerConversions.ToInt32(i.A));
        static string SU32(NumericsInput i) => Render.Int(IntegerConversions.ToUint32(i.A));
        static long Atomics(J.TypedArrays.ElementKind kind, double x) =>
            J.TypedArrays.JSAtomics_TruncateToElement(kind, J.IntegerHelpers.JSAtomics_ToIntegerOperand(x));

        yield return new("ToInt32", n1, Once,
        [
            new("J", "JSValue.IntValue", static i => Render.Int(J.JSValue.IntValue(i.A)), S32),
            new("J", "JSAtomics.TruncateToElement(Int32, ToIntegerOperand)", static i => Render.Int(Atomics(J.TypedArrays.ElementKind.Int32, i.A)), S32),
            new("V", "JsValue.ToInt32", static i => Render.Int(V.JsValue.ToInt32(i.A)), S32),
        ]);
        yield return new("ToUint32", n1, Once,
        [
            new("J", "JSValue.ToUint32(double)", static i => Render.Int(J.JSValue.ToUint32(i.A)), SU32),
            new("J", "JSValue.UIntValue", static i => Render.Int(J.JSValue.UIntValue(i.A)), SU32),
            new("J", "JSAtomics.TruncateToElement(Uint32, ToIntegerOperand)", static i => Render.Int(Atomics(J.TypedArrays.ElementKind.Uint32, i.A)), SU32),
            new("V", "JsValue.ToUint32", static i => Render.Int(V.JsValue.ToUint32(i.A)), SU32),
        ]);

        static string S16(NumericsInput i) => Render.Int(IntegerConversions.ToInt16(i.A));
        static string SU16(NumericsInput i) => Render.Int(IntegerConversions.ToUint16(i.A));
        static string S8(NumericsInput i) => Render.Int(IntegerConversions.ToInt8(i.A));
        static string SU8(NumericsInput i) => Render.Int(IntegerConversions.ToUint8(i.A));
        yield return new("ToInt16", n1, Once,
        [
            new("J", "JSInt16Array.SetValue", static i => Render.Int(J.TypedArrays.JSInt16Array_SetValue(i.A)), S16),
            // DataView writes the bits; setInt16 then getInt16 reads them as a short.
            new("J", "DataView.SetInt16", static i => Render.Int(unchecked((short)J.TypedArrays.DataView_SetInt16(i.A))), S16),
            new("J", "JSAtomics.TruncateToElement(Int16, ToIntegerOperand)", static i => Render.Int(Atomics(J.TypedArrays.ElementKind.Int16, i.A)), S16),
        ]);
        yield return new("ToUint16", n1, Once,
        [
            new("J", "JSUInt16Array.SetValue", static i => Render.Int(J.TypedArrays.JSUInt16Array_SetValue(i.A)), SU16),
            new("J", "DataView.SetUint16", static i => Render.Int(J.TypedArrays.DataView_SetUint16(i.A)), SU16),
            new("J", "JSStringStatic.FromCharCode", static i => Render.Int(J.TypedArrays.JSStringStatic_FromCharCode(i.A)), SU16),
            new("J", "JSAtomics.TruncateToElement(Uint16, ToIntegerOperand)", static i => Render.Int(Atomics(J.TypedArrays.ElementKind.Uint16, i.A)), SU16),
        ]);
        yield return new("ToInt8", n1, Once,
        [
            // The byte is read back as a signed byte (GetValue).
            new("J", "JSInt8Array.SetValue", static i => Render.Int(unchecked((sbyte)J.TypedArrays.JSInt8Array_SetValue(i.A))), S8),
            new("J", "DataView.SetInt8", static i => Render.Int(unchecked((sbyte)J.TypedArrays.DataView_SetInt8(i.A))), S8),
            new("J", "JSAtomics.TruncateToElement(Int8, ToIntegerOperand)", static i => Render.Int(Atomics(J.TypedArrays.ElementKind.Int8, i.A)), S8),
        ]);
        yield return new("ToUint8", n1, Once,
        [
            new("J", "JSUInt8Array.SetValue", static i => Render.Int(J.TypedArrays.JSUInt8Array_SetValue(i.A)), SU8),
            new("J", "DataView.SetUint8", static i => Render.Int(J.TypedArrays.DataView_SetUint8(i.A)), SU8),
            new("J", "JSAtomics.TruncateToElement(Uint8, ToIntegerOperand)", static i => Render.Int(Atomics(J.TypedArrays.ElementKind.Uint8, i.A)), SU8),
        ]);
        static string SClamp(NumericsInput i) => Render.Int(IntegerConversions.ToUint8Clamp(i.A));
        yield return new("ToUint8Clamp", n1, Once,
        [
            new("J", "JSUint8ClampedArray.SetValue", static i => Render.Int(J.TypedArrays.JSUint8ClampedArray_SetValue(i.A)), SClamp),
            new("V", "JsElements.Clamp", static i => Render.Int(V.JsElements.Clamp(i.A)), SClamp),
        ]);

        static string SInteger(NumericsInput i) => Render.Number(IntegerConversions.ToIntegerOrInfinity(i.A));
        // The helpers that map ±Infinity to their type's extreme say so; that extreme reads as the infinity.
        static string LongSentinel(long value) => value switch
        {
            long.MaxValue => "num:+Infinity",
            long.MinValue => "num:-Infinity",
            _ => Render.Number(value),
        };
        yield return new("ToIntegerOrInfinity", n1, Once,
        [
            new("J", "JSNumberPrototype.ToIntegerOrInfinity", static i => Render.Number(J.IntegerHelpers.JSNumberPrototype_ToIntegerOrInfinity(i.A)), SInteger),
            new("J", "JSTypedArray.ToIntegerOrInfinity", static i => Render.Number(J.IntegerHelpers.JSTypedArray_ToIntegerOrInfinity(i.A)), SInteger),
            new("J", "JSArrayBuffer.ToIntegerOrInfinity", static i => Render.Number(J.IntegerHelpers.JSArrayBuffer_ToIntegerOrInfinity(i.A)), SInteger),
            new("J", "JSArrayPrototype.ToIntegerOrInfinity", static i => LongSentinel(J.IntegerHelpers.JSArrayPrototypeIteration_ToIntegerOrInfinity(i.A)), SInteger),
            new("J", "JSTypedArrayPrototype.ToIntegerOrInfinityIndex", static i => LongSentinel(J.IntegerHelpers.JSTypedArrayPrototype_ToIntegerOrInfinityIndex(i.A)), SInteger),
            new("J", "JSAtomics.ToIntegerOperand", static i => Render.Number(J.IntegerHelpers.JSAtomics_ToIntegerOperand(i.A)), SInteger),
            new("J", "JSIntlSegments.ContainingPrototype's index", static i => Render.Number(J.IntegerHelpers.JSIntlSegments_ContainingPrototype_Index(i.A)), SInteger),
            new("J", "JSValue.IntegerValue", static i => Render.Number(J.JSValue.IntegerValue(i.A)), SInteger),
            // The site's next steps are the specification's: a radix outside 2 to 36 is a RangeError
            // (21.2.3.3 step 4), and a match position is clamped to [0, lengthS] (22.2.6.11 step 16.f-g).
            new("J", "JSBigInt.JSToString's radix", static i => J.IntegerHelpers.JSBigInt_JSToString_Radix(i.A) is { } r ? Render.Number(r) : Render.RangeError,
                static i => IntegerConversions.ToIntegerOrInfinity(i.A) is var r && r >= 2 && r <= 36 ? Render.Number(r) : Render.RangeError),
            new("J", "BuiltInsAssemblyInitializer.OneReplacement's position (length 1000)", static i => Render.Number(J.IntegerHelpers.BuiltInsAssemblyInitializer_OneReplacement_Position(i.A, 1000)),
                static i => Render.Number(Math.Clamp(IntegerConversions.ToIntegerOrInfinity(i.A), 0, 1000))),
            new("V", "JsValue.ToInteger", static i => Render.Number(V.JsValue.ToInteger(i.A)), SInteger),
        ]);

        static string SLength(NumericsInput i) => Render.Number(IntegerConversions.ToLength(i.A));
        yield return new("ToLength", n1, Once,
        [
            new("J", "JSJSON.ToLength", static i => Render.Number(J.IntegerHelpers.JSJSON_ToLength(i.A)), SLength),
            new("J", "JSArrayPrototype.ToLength", static i => Render.Number(J.IntegerHelpers.JSArrayPrototypeIteration_ToLength(i.A)), SLength),
            new("J", "JSProxy.ToArrayLikeLength", static i => Render.Number(J.IntegerHelpers.JSProxy_ToArrayLikeLength(i.A)), SLength),
            new("J", "BuiltInsAssemblyInitializer.ToLength", static i => Render.Number(J.IntegerHelpers.BuiltInsAssemblyInitializer_ToLength(i.A)), SLength),
            new("J", "BuiltInsAssemblyInitializer.ToLengthDouble", static i => Render.Number(J.IntegerHelpers.BuiltInsAssemblyInitializer_ToLengthDouble(i.A)), SLength),
            new("J", "JSTypedArray.ToArrayLikeLength (1-byte elements)", static i => J.IntegerHelpers.JSTypedArray_ToArrayLikeLength(i.A, 1) is { } n ? Render.Number(n) : Render.RangeError, SLength),
            new("J", "JSTypedArray.FromShared's length", static i => Render.Number(J.IntegerHelpers.JSTypedArray_FromShared_Length(i.A)), SLength),
            new("J", "JSIntl.CanonicalizeLocaleList's length", static i => Render.Number(J.IntegerHelpers.JSIntl_CanonicalizeLocaleList_Length(i.A)), SLength),
            // The engine's `length` passes its ToInteger's result (the source's first line).
            new("V", "JsRealm.ArrayToLength(ToInteger)", static i => Render.Number(V.JsRealmArray.ArrayToLength(V.JsValue.ToInteger(i.A))), SLength),
        ]);

        static string SIndex(NumericsInput i) => IntegerConversions.TryToIndex(i.A, out var index) ? Render.Number(index) : Render.RangeError;
        static string Nullable(long? value) => value is { } v ? Render.Number(v) : Render.RangeError;
        yield return new("ToIndex", n1, Once,
        [
            new("J", "DataView.ToIndex", static i => Nullable(J.IntegerHelpers.DataView_ToIndex(i.A)), SIndex),
            new("J", "JSBigInt.ToBitsIndex", static i => Nullable(J.IntegerHelpers.JSBigInt_ToBitsIndex(i.A)), SIndex),
            new("J", "DataView.ToByteOffset", static i => Nullable(J.IntegerHelpers.DataView_ToByteOffset(i.A)), SIndex),
            new("J", "JSAtomics.ToIndex", static i => Nullable(J.IntegerHelpers.JSAtomics_ToIndex(i.A)), SIndex),
            new("J", "TypedArrayParameters.ToTypedArrayLength", static i => Nullable(J.IntegerHelpers.TypedArrayParameters_ToTypedArrayLength(i.A)), SIndex),
            new("J", "JSArrayBuffer.ToBufferLengthLong", static i => Nullable(J.IntegerHelpers.JSArrayBuffer_ToBufferLengthLong(i.A)), SIndex),
            new("J", "JSArrayBuffer.ToBufferLength", static i => Nullable(J.IntegerHelpers.JSArrayBuffer_ToBufferLength(i.A)), SIndex),
            // BinaryToIndex takes ToIntegerOrInfinity's result, which the engine computes with ToInteger.
            new("V", "JsRealm.BinaryToIndex(ToInteger)", static i => V.JsRealmBinary.BinaryToIndex(V.JsValue.ToInteger(i.A)) is { } d ? Render.Number(d) : Render.RangeError, SIndex),
        ]);

        string[] b1v = ["B1V"];
        static string S64(NumericsInput i) => Render.Int(IntegerConversions.ToBigInt64(i.X));
        static string SU64(NumericsInput i) => Render.Int(IntegerConversions.ToBigUint64(i.X));
        yield return new("ToBigInt64", b1v, Once,
        [
            new("J", "JSBigInt64Array.SetValue", static i => Render.Int(unchecked((long)J.TypedArrays.JSBigInt64Array_SetValue(i.X))), S64),
            new("J", "DataView.RawBitsFor (setBigInt64)", static i => Render.Int(unchecked((long)J.TypedArrays.DataView_RawBitsFor(i.X))), S64),
            new("J", "JSAtomics.TruncateToBigElement(BigInt64)", static i => Render.Int(J.TypedArrays.JSAtomics_TruncateToBigElement(J.TypedArrays.ElementKind.BigInt64, i.X)), S64),
            new("V", "JsElements.Low64 (read as BigInt64)", static i => Render.Int(unchecked((long)V.JsElements.Low64(i.X))), S64),
            new("V", "JsElements.Narrow(BigInt64)", static i => Render.Int(V.JsElements.Narrow(V.JsElementKind.BigInt64, new V.JsBigInt(i.X)).Value), S64, static i => Fits(i.X)),
        ]);
        yield return new("ToBigUint64", b1v, Once,
        [
            new("J", "JSBigUint64Array.SetValue", static i => Render.Int(J.TypedArrays.JSBigUint64Array_SetValue(i.X)), SU64),
            new("J", "DataView.RawBitsFor (setBigUint64)", static i => Render.Int(J.TypedArrays.DataView_RawBitsFor(i.X)), SU64),
            new("J", "JSAtomics.TruncateToBigElement(BigUint64)", static i => Render.Int(J.TypedArrays.JSAtomics_TruncateToBigElement(J.TypedArrays.ElementKind.BigUint64, i.X)), SU64),
            new("V", "JsElements.Low64", static i => Render.Int(V.JsElements.Low64(i.X)), SU64),
            new("V", "JsElements.Narrow(BigUint64)", static i => Render.Int(V.JsElements.Narrow(V.JsElementKind.BigUint64, new V.JsBigInt(i.X)).Value), SU64, static i => Fits(i.X)),
        ]);
    }

    // ---- Float16 --------------------------------------------------------------------------------

    private static IEnumerable<NumericsOperation> Float16Operations()
    {
        string[] encode = ["N2E", "N1", "N2N"];
        yield return new("Float16 encode (canonical policy)", encode, Once,
        [
            new("V", "JsFloat16.Encode", static i => Render.F16(V.JsFloat16.Encode(i.A)), static i => Render.F16(Numbers.Float16.Encode(i.A, Float16NanPolicy.Canonical))),
        ]);
        static string SPayload(NumericsInput i) => Render.F16(Numbers.Float16.Encode(i.A, Float16NanPolicy.PreservePayload));
        yield return new("Float16 encode (payload policy)", encode, Once,
        [
            new("J", "JSFloat16Array.SetValue", static i => Render.F16(J.Float16.JSFloat16Array_SetValue(i.A)), SPayload),
            new("J", "DataView.SetFloat16", static i => Render.F16(J.Float16.DataView_SetFloat16(i.A)), SPayload),
        ]);
        static string SDecode(NumericsInput i) => Render.F64(Numbers.Float16.Decode(i.Half));
        yield return new("Float16 decode", ["N2D"], Once,
        [
            new("J", "JSFloat16Array.GetValue", static i => Render.F64(J.Float16.JSFloat16Array_GetValue(i.Half)), SDecode),
            new("J", "DataView.GetFloat16", static i => Render.F64(J.Float16.DataView_GetFloat16(i.Half)), SDecode),
            new("V", "JsFloat16.Decode", static i => Render.F64(V.JsFloat16.Decode(i.Half)), SDecode),
        ]);
        static string SRound(NumericsInput i) => Render.F64(Numbers.Float16.Round(i.A));
        yield return new("Math.f16round", encode, Once,
        [
            new("J", "JSMath.F16round", static i => Render.F64(J.Float16.JSMath_F16round(i.A)), SRound),
            new("V", "JsFloat16.Round", static i => Render.F64(V.JsFloat16.Round(i.A)), SRound),
        ]);
    }

    // ---- Math -----------------------------------------------------------------------------------

    private static IEnumerable<NumericsOperation> MathOperations()
    {
        static string SRound(NumericsInput i) => Render.F64(MathKernels.Round(i.A));
        yield return new("Math.round", ["N1"], Once,
        [
            new("J", "JSMath.Round", static i => Render.F64(J.JSMath.Round(i.A)), SRound),
            new("V", "JsRealm.MathRoundHalfUp", static i => Render.F64(V.JsRealmMath.MathRoundHalfUp(i.A)), SRound),
        ]);
        static string SPower(NumericsInput i) => Render.F64(MathKernels.Power(i.A, i.B));
        yield return new("Number::exponentiate", ["N3P"], Once,
        [
            new("J", "JSMath.Pow", static i => Render.F64(J.JSMath.Pow(i.A, i.B)), SPower),
            new("J", "JSValue.Power", static i => Render.F64(J.JSValue.Power(i.A, i.B)), SPower),
            new("V", "JsRealm.MathPower", static i => Render.F64(V.JsRealmMath.MathPower(i.A, i.B)), SPower),
        ]);
        static string SHypot(NumericsInput i) => Render.F64(MathKernels.Hypot(i.Values));
        yield return new("Math.hypot", ["N3H"], Once,
        [
            new("J", "JSMath.Hypot", static i => Render.F64(J.JSMath.Hypot(i.Values)), SHypot),
            new("V", "JsRealm.MathHypotenuse", static i => Render.F64(V.JsRealmMath.MathHypotenuse(i.Values, static _ => { })), SHypot),
        ]);
        static string SCbrt(NumericsInput i) => Render.F64(MathKernels.Cbrt(i.A));
        yield return new("Math.cbrt", ["N1", "N3C"], Once,
        [
            new("J", "JSMath.Cbrt", static i => Render.F64(J.JSMath.Cbrt(i.A)), SCbrt),
            new("V", "JsRealm's cbrt", static i => Render.F64(V.JsRealmMath.Cbrt(i.A)), SCbrt),
        ]);
        static string SExpm1(NumericsInput i) => Render.F64(MathKernels.Expm1(i.A));
        yield return new("Math.expm1", ["N1", "N3E"], Once,
        [
            new("J", "JSMath.Expm1", static i => Render.F64(J.JSMath.Expm1(i.A)), SExpm1),
            new("V", "JsRealm.MathExpMinusOne", static i => Render.F64(V.JsRealmMath.MathExpMinusOne(i.A)), SExpm1),
        ]);
        static string SLog1p(NumericsInput i) => Render.F64(MathKernels.Log1p(i.A));
        yield return new("Math.log1p", ["N1", "N3E"], Once,
        [
            new("J", "JSMath.Log1p", static i => Render.F64(J.JSMath.Log1p(i.A)), SLog1p),
            new("V", "JsRealm.MathLogOnePlus", static i => Render.F64(V.JsRealmMath.MathLogOnePlus(i.A)), SLog1p),
        ]);
        static string SSum(NumericsInput i)
        {
            var sum = new PreciseSum();
            foreach (var value in i.Values!)
                sum.Add(value);
            return Render.F64(sum.Result());
        }
        yield return new("Math.sumPrecise", ["N4"], Once,
        [
            new("J", "JSMath.SumPrecise", static i => J.JSMath.SumPrecise(i.Values!) is { } d ? Render.F64(d) : Render.RangeError, SSum),
            new("V", "JsRealm.MathSumPrecise", static i => Render.F64(V.JsRealmMath.MathSumPrecise(i.Values!, static _ => { })), SSum),
        ]);
    }

    // ---- Exact rounding -------------------------------------------------------------------------

    private static IEnumerable<NumericsOperation> RoundingOperations()
    {
        yield return new("Rounding-mode name", ["R1M"], Once,
        [
            new("J", "TemporalRoundingOptions.NormalizeRoundingMode", static i => J.TemporalRoundingOptions.NormalizeRoundingMode(i.Text!) ?? Render.RangeError, SParse),
            new("J", "JSTemporalDuration.NormalizeRoundingMode", static i => J.JSTemporalDuration.NormalizeRoundingMode(i.Text!) ?? Render.RangeError, SParse),
        ]);

        // Negation and the unsigned modes take a mode: the names S parses.
        static IEnumerable<NumericsInput> Valid(NumericsInput i) =>
            ExactRounding.TryParseRoundingMode(i.Text, out var mode) ? [i with { Parameter = (int)mode, Detail = i.Text! }] : [];
        yield return new("NegateRoundingMode", ["R1M"], Valid,
        [
            new("J", "TemporalRoundingOptions.NegateRoundingMode", static i => J.TemporalRoundingOptions.NegateRoundingMode(Name(i.Parameter)), static i => Name((int)ExactRounding.Negate(SMode(i.Parameter)))),
            new("V", "JsTemporalCore.Negate", static i => Name((int)Enum.Parse<RoundingMode>(V.JsTemporalCore.Negate(VMode(i.Parameter)).ToString())), static i => Name((int)ExactRounding.Negate(SMode(i.Parameter)))),
        ]);

        static IEnumerable<NumericsInput> ValidSigned(NumericsInput i) =>
            Valid(i).SelectMany(v => new[] { v with { Parameter2 = 0, Detail = v.Detail + "/positive" }, v with { Parameter2 = 1, Detail = v.Detail + "/negative" } });
        static string SUnsigned(NumericsInput i) => Unsigned(ExactRounding.GetUnsignedRoundingMode(SMode(i.Parameter), i.Parameter2 == 1));
        yield return new("GetUnsignedRoundingMode", ["R1M"], ValidSigned,
        [
            new("J", "TemporalRoundingOptions.UnsignedRoundingMode", static i => J.TemporalRoundingOptions.UnsignedRoundingMode(Name(i.Parameter), i.Parameter2 == 1), SUnsigned),
            new("J", "JSTemporalZonedDateTime.UnsignedRoundingMode", static i => J.JSTemporalZonedDateTime.UnsignedRoundingMode(Name(i.Parameter), i.Parameter2 == 1), SUnsigned),
            new("J", "JSIntlNumberFormat.GetUnsignedRoundingMode", static i => UnsignedNames[(int)J.JSIntlNumberFormat.GetUnsignedRoundingMode(Name(i.Parameter), i.Parameter2 == 1)], SUnsigned),
            new("V", "JsTemporalCore.UnsignedMode", static i => UnsignedNames[(int)V.JsTemporalCore.UnsignedMode(VMode(i.Parameter), i.Parameter2 == 1)], SUnsigned),
            new("V", "JsNumberFormatter.Unsigned", static i => UnsignedNames[(int)V.JsNumberFormatter.Unsigned(Name(i.Parameter), i.Parameter2 == 1)], SUnsigned),
        ]);

        // One step: R1W's windows of width 1, at every unsigned mode.
        static IEnumerable<NumericsInput> Step(NumericsInput i) =>
            i.W - i.Z != 1 ? [] : Range(0, 4).Select(m => i with { Parameter = m, Detail = UnsignedNames[m] });
        static string SApply(NumericsInput i) =>
            Render.Int(ExactRounding.ApplyUnsignedRoundingMode(i.X, i.Y, i.Z, i.W, (UnsignedRoundingMode)i.Parameter));
        yield return new("ApplyUnsignedRoundingMode, one step", ["R1W"], Step,
        [
            // Its engine calls it with r1 the quotient's floor (RoundToIncrement), so never at the upper end.
            new("V", "JsTemporalCore.ApplyUnsigned", static i => Render.Int(V.JsTemporalCore.ApplyUnsigned(i.X, i.Y, i.Z, (V.JsTemporalCore.Unsigned)i.Parameter)), SApply,
                static i => i.Z == BigInteger.Divide(i.X, i.Y)),
            // RoundRatio rounds a non-negative a / b to an integer: the window [floor, floor + 1].
            new("J", "JSIntlNumberFormat.RoundRatio", static i => Render.Int(J.JSIntlNumberFormat.RoundRatio(i.X, i.Y, (J.JSIntlNumberFormat.UnsignedRoundingMode)i.Parameter)), SApply,
                static i => i.Z == BigInteger.Divide(i.X, i.Y)),
        ]);

        // A window: R1W's wider windows, at every mode and both signs; the result is whether the upper end is taken.
        static IEnumerable<NumericsInput> Window(NumericsInput i) => i.W - i.Z == 1 ? [] : Modes(signed: true)(i);
        static string SWindow(NumericsInput i) =>
            (ExactRounding.ApplyUnsignedRoundingMode(i.X, i.Y, i.Z, i.W, ExactRounding.GetUnsignedRoundingMode(SMode(i.Parameter), i.Parameter2 == 1)) == i.W).ToString();
        // NudgeToCalendarUnit's lines in both of Broiler.JS's copies: the progress and the window's
        // length, the comparison of twice the one with the other, and the parity of the start's count of
        // increments (computed over BigInteger; within the long range the sites use, the same).
        static bool JNudge(NumericsInput i, Func<int, bool, string, bool> picksEnd, Func<string, bool, string> unsignedMode)
        {
            var numerator = i.X - (i.Z * i.Y);
            var denominator = (i.W - i.Z) * i.Y;
            var absNum = BigInteger.Abs(numerator);
            var absDen = BigInteger.Abs(denominator);
            if (numerator.IsZero)
                return false;
            if (absNum == absDen)
                return true;
            var cmp = (absNum * 2).CompareTo(absDen);
            var even = BigInteger.Abs(i.Z) / (i.W - i.Z) % 2 == 0;
            return picksEnd(cmp, even, unsignedMode(Name(i.Parameter), i.Parameter2 == 1));
        }
        yield return new("ApplyUnsignedRoundingMode, a window", ["R1W"], Window,
        [
            // Broiler.VM's NudgeToCalendarUnit: a progress of the whole span rounds up without the call.
            new("V", "JsTemporal.Durations' NudgeToCalendarUnit with JsTemporalCore.RoundsToSecond", static i =>
                (i.X == i.W * i.Y || V.JsTemporalCore.RoundsToSecond(i.X, i.Y, i.Z, i.W, VMode(i.Parameter), i.Parameter2 == 1)).ToString(), SWindow),
            new("J", "JSTemporalZonedDateTime's NudgeToCalendarUnit with ApplyRoundingPicksEnd",
                static i => JNudge(i, J.JSTemporalZonedDateTime.ApplyRoundingPicksEnd, J.JSTemporalZonedDateTime.UnsignedRoundingMode).ToString(), SWindow),
            new("J", "JSTemporalPlainDate's NudgeToCalendarUnit with TemporalRoundingOptions.ApplyRoundingPicksEnd",
                static i => JNudge(i, J.TemporalRoundingOptions.ApplyRoundingPicksEnd, J.TemporalRoundingOptions.UnsignedRoundingMode).ToString(), SWindow),
        ]);

        // RoundNumberToIncrement over integers: R1I's lines with denominator 1.
        static IEnumerable<NumericsInput> Integers(NumericsInput i) => i.Y.IsOne ? Modes(signed: false)(i) : [];
        static string SIncrement(NumericsInput i) => Render.Int(ExactRounding.RoundNumberToIncrement(i.X, i.Y, i.Z, SMode(i.Parameter)));
        yield return new("RoundNumberToIncrement, integers", ["R1I"], Integers,
        [
            new("V", "JsTemporalCore.RoundToIncrement(x, increment, mode)", static i => Render.Int(V.JsTemporalCore.RoundToIncrement(i.X, i.Z, VMode(i.Parameter))), SIncrement),
            new("J", "TemporalRoundingOptions.RoundToIncrement", static i => Render.Int(J.TemporalRoundingOptions.RoundToIncrement(i.X, i.Z, Name(i.Parameter))), SIncrement),
            new("J", "JSTemporalDuration.RoundToIncrement", static i => Render.Int(J.JSTemporalDuration.RoundToIncrement(i.X, i.Z, Name(i.Parameter))), SIncrement),
        ]);
        yield return new("RoundNumberToIncrement, rationals", ["R1I"], Modes(signed: false),
        [
            new("V", "JsTemporalCore.RoundToIncrement(num, den, increment, mode)", static i => Render.Int(V.JsTemporalCore.RoundToIncrement(i.X, i.Y, i.Z, VMode(i.Parameter))), SIncrement),
            // ToRawFixed's line: qInc = RoundRatio(num, den * increment, mode) over the magnitude, the
            // mode unsigned by the sign; then qInc * increment, signed.
            new("J", "JSIntlNumberFormat.RoundRatio (ToRawFixed's call)", static i =>
            {
                var negative = i.X.Sign < 0;
                var q = J.JSIntlNumberFormat.RoundRatio(BigInteger.Abs(i.X), i.Y * i.Z, J.JSIntlNumberFormat.GetUnsignedRoundingMode(Name(i.Parameter), negative));
                return Render.Int((negative ? -q : q) * i.Z);
            }, SIncrement),
        ]);
        static string SAsIfPositive(NumericsInput i) => Render.Int(ExactRounding.RoundNumberToIncrementAsIfPositive(i.X, i.Y, i.Z, SMode(i.Parameter)));
        yield return new("RoundNumberToIncrementAsIfPositive", ["R1I"], Integers,
        [
            new("V", "JsTemporalCore.RoundToIncrementAsIfPositive", static i => Render.Int(V.JsTemporalCore.RoundToIncrementAsIfPositive(i.X, i.Z, VMode(i.Parameter))), SAsIfPositive),
            new("J", "TemporalRoundingOptions.RoundToIncrementAsIfPositive", static i => Render.Int(J.TemporalRoundingOptions.RoundToIncrementAsIfPositive(i.X, i.Z, Name(i.Parameter))), SAsIfPositive),
        ]);

        static string SNearest(NumericsInput i) => Render.F64(ExactRounding.ToDouble(i.X, i.Y));
        yield return new("A rational's nearest double", ["R1D"], Once,
        [
            new("V", "JsTemporalCore.ToDouble(num, den)", static i => Render.F64(V.JsTemporalCore.ToDouble(i.X, i.Y)), SNearest),
            new("J", "JSTemporalDuration.RatioToDouble", static i => Render.F64(J.JSTemporalDuration.RatioToDouble(i.X, i.Y)), SNearest),
        ]);
        static string SNearestInteger(NumericsInput i) => Render.F64(ExactRounding.ToDouble(i.X, BigInteger.One));
        yield return new("An integer's nearest double", ["R1D", "B1E"], Once,
        [
            new("V", "JsTemporalCore.ToDouble(BigInteger)", static i => Render.F64(V.JsTemporalCore.ToDouble(i.X)), SNearestInteger),
            new("J", "JSTemporalDuration.NearestDouble", static i => Render.F64(J.JSTemporalDuration.NearestDouble(i.X)), SNearestInteger),
        ]);
    }

    private static string SParse(NumericsInput i) =>
        ExactRounding.TryParseRoundingMode(i.Text, out var mode) ? Name((int)mode) : Render.RangeError;

    // ---- BigInt ---------------------------------------------------------------------------------

    private static IEnumerable<NumericsOperation> BigIntOperations()
    {
        string[] pairs = ["B1P"];
        foreach (var (name, s, v, j) in new (string, SBinary, Func<V.JsBigInt, V.JsBigInt, Action<ulong>, V.JsBigInt?>, Func<BigInteger, BigInteger, BigInteger>)[]
                 {
                     ("BigInt::add", BigIntMath.Add, V.JsBigInt.Add, J.JSBigInt.AddValue),
                     ("BigInt::subtract", BigIntMath.Subtract, V.JsBigInt.Subtract, J.JSBigInt.Subtract),
                     ("BigInt::multiply", BigIntMath.Multiply, V.JsBigInt.Multiply, J.JSBigInt.Multiply),
                     ("BigInt::bitwiseAND", BigIntMath.BitwiseAnd, V.JsBigInt.And, J.JSBigInt.BitwiseAnd),
                     ("BigInt::bitwiseOR", BigIntMath.BitwiseOr, V.JsBigInt.Or, J.JSBigInt.BitwiseOr),
                     ("BigInt::bitwiseXOR", BigIntMath.BitwiseXor, V.JsBigInt.Xor, J.JSBigInt.BitwiseXor),
                 })
            yield return Binary(name, pairs, Once, s, v, j);

        static IEnumerable<NumericsInput> NonZeroDivisor(NumericsInput i) => i.Y.IsZero ? [] : [i];
        yield return Binary("BigInt::divide", pairs, NonZeroDivisor, BigIntMath.Divide, V.JsBigInt.Divide, J.JSBigInt.Divide);
        yield return Binary("BigInt::remainder", pairs, NonZeroDivisor, BigIntMath.Remainder, V.JsBigInt.Remainder, J.JSBigInt.Modulo);
        static IEnumerable<NumericsInput> NonNegativeExponent(NumericsInput i) => i.Y.Sign < 0 ? [] : [i];
        yield return Binary("BigInt::exponentiate", pairs, NonNegativeExponent, BigIntMath.Power, V.JsBigInt.Power, J.JSBigInt.Power,
            jApplies: static i => !PowerTooWideToCompute(i.X, i.Y));
        yield return Binary("BigInt::leftShift", ["B1S"], Once, BigIntMath.LeftShift, V.JsBigInt.ShiftLeft, J.JSBigInt.LeftShift);
        yield return Binary("BigInt::signedRightShift", ["B1S"], Once, BigIntMath.SignedRightShift, V.JsBigInt.ShiftRight, J.JSBigInt.RightShift);

        string[] values = ["B1V"];
        yield return Unary("BigInt::unaryMinus", values, BigIntMath.Negate, V.JsBigInt.Negate, J.JSBigInt.Negate);
        yield return Unary("BigInt::bitwiseNOT", values, BigIntMath.BitwiseNot, V.JsBigInt.BitwiseNot, J.JSBigInt.BitwiseNot);

        static ulong Width(NumericsInput i) => (ulong)i.Z;
        yield return new("BigInt.asIntN", ["B1N"], Once,
        [
            new("V", "JsBigInt.AsIntN", static i => VRun((x, c) => V.JsBigInt.AsIntN(Width(i), x, c), i.X), static i => SVm((BigInteger x, BigIntPolicy p, WorkCharge? c, out BigInteger r) => BigIntMath.AsIntN(Width(i), x, p, c, out r), i.X), static i => Fits(i.X)),
            new("J", "JSBigInt.AsIntN", static i => Render.Big(J.JSBigInt.AsIntN((long)Width(i), i.X)), static i => SUnbounded((BigInteger x, BigIntPolicy p, WorkCharge? c, out BigInteger r) => BigIntMath.AsIntN(Width(i), x, p, c, out r), i.X)),
        ]);
        yield return new("BigInt.asUintN", ["B1N"], Once,
        [
            new("V", "JsBigInt.AsUintN", static i => VRun((x, c) => V.JsBigInt.AsUintN(Width(i), x, c), i.X), static i => SVm((BigInteger x, BigIntPolicy p, WorkCharge? c, out BigInteger r) => BigIntMath.AsUintN(Width(i), x, p, c, out r), i.X), static i => Fits(i.X)),
            new("J", "JSBigInt.AsUintN", static i => Render.Big(J.JSBigInt.AsUintN((long)Width(i), i.X)), static i => SUnbounded((BigInteger x, BigIntPolicy p, WorkCharge? c, out BigInteger r) => BigIntMath.AsUintN(Width(i), x, p, c, out r), i.X),
                static i => !(i.X.Sign < 0 && Width(i) > (ulong)UnboundedComputeLimit && Width(i) <= int.MaxValue)),
        ]);

        // Text at every radix.
        yield return new("BigInt::toString", values, Each(Range(2, 36)),
        [
            new("V", "JsBigInt.ToRadixString", static i =>
            {
                var meter = new Meter();
                return Render.Charged(new V.JsBigInt(i.X).ToRadixString(i.Parameter, meter.Charge), meter.Total);
            }, static i =>
            {
                var meter = new Meter();
                return Render.Charged(BigIntText.ToString(i.X, i.Parameter, meter.Charge), meter.Total);
            }, static i => Fits(i.X)),
            new("J", "JSBigInt.BigIntegerToRadixString (radix 10: ToString)", static i => JText(i.X, i.Parameter), static i => BigIntText.ToString(i.X, i.Parameter, null), JTextApplies),
            new("J", "JSBigInt.BigIntegerToRadixString (radix 10: ToString), under U+2212", static i => Minus2212(() => JText(i.X, i.Parameter)), static i => BigIntText.ToString(i.X, i.Parameter, null), JTextApplies),
        ]);

        yield return new("StringToBigInt", values, StringToBigIntTexts,
        [
            new("V", "JsBigInt.TryParseStringInteger", static i =>
            {
                var meter = new Meter();
                // True with no value is a value past the ceiling, reported with its sign (the member's contract).
                var ok = V.JsBigInt.TryParseStringInteger(i.Text!, meter.Charge, out var value, out var sign);
                var result = !ok ? Render.Undefined
                    : value is null ? Render.RangeError + ":" + sign.ToString(CultureInfo.InvariantCulture)
                    : Render.Big(value.Value);
                return Render.Charged(result, meter.Total);
            }, static i =>
            {
                var meter = new Meter();
                var status = BigIntText.StringToBigInt(i.Text, VmPolicy, meter.Charge, out var value, out var sign);
                var result = status switch
                {
                    ResultStatus.Completed => Render.Big(value),
                    ResultStatus.Malformed => Render.Undefined,
                    _ => Render.RangeError + ":" + sign.ToString(CultureInfo.InvariantCulture),
                };
                return Render.Charged(result, meter.Total);
            }),
            new("J", "JSBigInt.TryStringToBigInt", static i => JParse(i.Text!), SParseUnbounded, JParseApplies),
            new("J", "JSBigInt.TryStringToBigInt, under U+2212", static i => Minus2212(() => JParse(i.Text!)), SParseUnbounded, JParseApplies),
        ]);

        static string SNumber(NumericsInput i)
        {
            var meter = new Meter();
            return Render.Charged(Render.F64(BigIntMath.ToNumber(i.X, meter.Charge)), meter.Total);
        }
        yield return new("A BigInt's Number", ["B1V", "B1E"], Once,
        [
            new("V", "JsBigInt.ToNumber", static i =>
            {
                var meter = new Meter();
                return Render.Charged(Render.F64(new V.JsBigInt(i.X).ToNumber(meter.Charge)), meter.Total);
            }, SNumber, static i => Fits(i.X)),
            new("J", "JSBigInt.ToNumber", static i => Render.F64(J.JSBigInt.ToNumber(i.X)), static i => Render.F64(BigIntMath.ToNumber(i.X, null))),
        ]);

        static IEnumerable<NumericsInput> Integral(NumericsInput i) => double.IsFinite(i.A) && Math.Truncate(i.A) == i.A ? [i] : [];
        static string SFromNumber(NumericsInput i) => Render.Big(BigIntMath.FromIntegralNumber(i.A));
        yield return new("An integral Number's BigInt", ["N1"], Integral,
        [
            new("V", "JsBigInt.FromIntegralNumber", static i => Render.Big(V.JsBigInt.FromIntegralNumber(i.A).Value), SFromNumber),
            new("J", "JSBigInt.NumberToBigInt", static i => Render.Big(J.JSBigInt.NumberToBigInt(i.A)), SFromNumber),
        ]);

        // Comparison with every finite double of B1C; the parameter is the double's line in B1C.
        yield return new("Comparison with a Number", values, ComparedDoubles,
        [
            new("V", "JsBigInt.CompareToNumber", static i =>
            {
                var meter = new Meter();
                return Render.Charged(Render.Int(Math.Sign(new V.JsBigInt(i.X).CompareToNumber(i.A, meter.Charge))), meter.Total);
            }, static i =>
            {
                var meter = new Meter();
                return Render.Charged(Render.Int(Math.Sign(BigIntMath.CompareToNumber(i.X, i.A, meter.Charge))), meter.Total);
            }, static i => Fits(i.X)),
            new("J", "JSBigIntExtensions.CompareToNumber", static i => Render.Int(Math.Sign(J.JSBigIntExtensions.CompareToNumber(i.X, i.A))),
                static i => Render.Int(Math.Sign(BigIntMath.CompareToNumber(i.X, i.A, null)))),
            new("J", "JSBigInt.EqualsLiteral", static i => J.JSBigInt.EqualsLiteral(i.X, i.A).ToString(),
                static i => (BigIntMath.CompareToNumber(i.X, i.A, null) == 0).ToString()),
        ]);
    }

    private static NumericsOperation Binary(
        string name, string[] classes, Func<NumericsInput, IEnumerable<NumericsInput>> expand, SBinary s,
        Func<V.JsBigInt, V.JsBigInt, Action<ulong>, V.JsBigInt?> v, Func<BigInteger, BigInteger, BigInteger> j,
        Func<NumericsInput, bool>? jApplies = null) =>
        new(name, classes, expand,
        [
            new("V", "JsBigInt." + v.Method.Name, i => VRun(v, i.X, i.Y), i => SVm(s, i.X, i.Y), static i => Fits(i.X) && Fits(i.Y)),
            new("J", "JSBigInt." + j.Method.Name, i => Render.Big(j(i.X, i.Y)), i => SUnbounded(s, i.X, i.Y), jApplies),
        ]);

    private static NumericsOperation Unary(
        string name, string[] classes, SUnary s, Func<V.JsBigInt, Action<ulong>, V.JsBigInt?> v, Func<BigInteger, BigInteger> j) =>
        new(name, classes, Once,
        [
            new("V", "JsBigInt." + v.Method.Name, i => VRun(v, i.X), i => SVm(s, i.X), static i => Fits(i.X)),
            new("J", "JSBigInt." + j.Method.Name, i => Render.Big(j(i.X)), i => SUnbounded(s, i.X)),
        ]);

    // ---- Arm J's text, and the texts StringToBigInt reads ----------------------------------------

    private static string JText(BigInteger value, int radix) =>
        radix == 10 ? J.JSBigInt.ToString(value) : J.JSBigInt.BigIntegerToRadixString(value, radix);

    private static string JParse(string text) =>
        J.JSBigInt.TryStringToBigInt(text, out var value) ? Render.Big(value) : Render.Undefined;

    private static string SParseUnbounded(NumericsInput i) =>
        BigIntText.StringToBigInt(i.Text, BigIntPolicy.Unbounded, null, out var value, out var sign) switch
        {
            ResultStatus.Completed => Render.Big(value),
            ResultStatus.Malformed => Render.Undefined,
            _ => Render.RangeError + ":" + sign.ToString(CultureInfo.InvariantCulture),
        };

    /// <summary>Whether arm J's quadratic text runs on a value (the revision): radix 10 always; otherwise a value of at most 4,101 bits or one of the first 256 drawn.</summary>
    private static bool JTextApplies(NumericsInput i) => i.Parameter == 10 || JTextValue(i);

    private static bool JTextValue(NumericsInput i) =>
        i.X.GetBitLength() <= JTextLimit || (i.Parameter2 >= 0 ? i.Parameter2 : (int)i.Line) is var line && IsEarlyDrawn(line);

    private static bool IsEarlyDrawn(int line) => line >= DrawnFrom && line < DrawnFrom + JTextDrawn;

    /// <summary>B1V's first drawn line: the population's count less <see cref="B1Drawn"/>, set before a run.</summary>
    public static int DrawnFrom { get; set; } = int.MaxValue;

    /// <summary>StringToBigInt's texts: the decimal ones always, the prefixed ones under <see cref="JTextValue"/>.</summary>
    private static bool JParseApplies(NumericsInput i) => i.Detail.StartsWith("d/", StringComparison.Ordinal) || i.Detail.StartsWith("-/", StringComparison.Ordinal) || JTextValue(i);

    private static string Minus2212(Func<string> run)
    {
        var previous = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "\u2212";
        CultureInfo.CurrentCulture = culture;
        try
        {
            return run();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    /// <summary>ECMA-262's StrWhiteSpaceChar at the corpus's pin: WhiteSpace, with Unicode 17.0.0's Zs, and LineTerminator.</summary>
    public static readonly char[] StrWhiteSpace =
    [
        '\u0009', '\u000B', '\u000C', ' ', '\u00A0', '\uFEFF', '\u1680', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004',
        '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', '\u202F', '\u205F', '\u3000', '\u000A', '\u000D', '\u2028', '\u2029',
    ];

    /// <summary>
    /// The texts StringToBigInt reads for one value of B1V, by the revision's construction: in each form
    /// (decimal "d", and "x", "o", "b" with their prefixes), the text alone; with every StrWhiteSpaceChar
    /// before and after it; with "+" and "-" before the digits (a sign the decimal form admits and the
    /// prefixed forms do not); with a separator after the first digit; with a digit beyond the radix
    /// after the last; with an "n" after it; and, in the prefixed forms, with the prefix in upper case.
    /// B1V's first line (0n) also gives the empty text and a text of white space alone.
    /// </summary>
    private static IEnumerable<NumericsInput> StringToBigIntTexts(NumericsInput input)
    {
        var value = input.X;
        var magnitude = BigInteger.Abs(value);
        var line = (int)input.Line;
        var texts = new List<(string Detail, string Text)>();
        if (line == 0)
        {
            texts.Add(("-/empty", string.Empty));
            texts.Add(("-/white-space", " \t\u3000\u2028"));
        }

        foreach (var (form, prefix, radix, beyond) in new[] { ("d", "", 10, 'a'), ("x", "0x", 16, 'g'), ("o", "0o", 8, '8'), ("b", "0b", 2, '2') })
        {
            var digits = Digits(magnitude, radix);
            var plain = form == "d" ? (value.Sign < 0 ? "-" : "") + digits : prefix + digits;
            texts.Add((form + "/plain", plain));
            foreach (var c in StrWhiteSpace)
                texts.Add((form + "/ws:" + ((int)c).ToString("X4", CultureInfo.InvariantCulture), c + plain + c));
            texts.Add((form + "/plus", "+" + prefix + digits));
            texts.Add((form + "/minus", "-" + prefix + digits));
            if (digits.Length >= 2)
                texts.Add((form + "/separator", prefix + digits[..1] + "_" + digits[1..]));
            texts.Add((form + "/beyond-radix", prefix + digits + beyond));
            texts.Add((form + "/n-suffix", plain + "n"));
            if (form != "d")
                texts.Add((form + "/upper-prefix", prefix.ToUpperInvariant() + digits));
        }

        return texts.Select(t => input with { Text = t.Text, Detail = t.Detail, Parameter2 = line });
    }

    /// <summary>A non-negative integer's digits in a radix, lower case, by the base class library alone.</summary>
    public static string Digits(BigInteger magnitude, int radix)
    {
        if (magnitude.IsZero)
            return "0";
        switch (radix)
        {
            case 10:
                return magnitude.ToString(CultureInfo.InvariantCulture);
            case 16:
                return magnitude.ToString("x", CultureInfo.InvariantCulture).TrimStart('0');
            default:
            {
                var bitsPerDigit = radix == 8 ? 3 : 1;
                var width = (int)magnitude.GetBitLength();
                var count = (width + bitsPerDigit - 1) / bitsPerDigit;
                var bytes = magnitude.ToByteArray(isUnsigned: true, isBigEndian: false);
                var chars = new char[count];
                for (var d = 0; d < count; d++)
                {
                    var digit = 0;
                    for (var b = 0; b < bitsPerDigit; b++)
                    {
                        var bit = (d * bitsPerDigit) + b;
                        if (bit < width && (bytes[bit >> 3] >> (bit & 7) & 1) != 0)
                            digit |= 1 << b;
                    }

                    chars[count - 1 - d] = (char)('0' + digit);
                }

                return new string(chars);
            }
        }
    }

    private static IEnumerable<NumericsInput> ComparedDoubles(NumericsInput input) => ComparedSource!(input);

    /// <summary>Set by the comparison before a run: every finite double of B1C, with its line.</summary>
    public static Func<NumericsInput, IEnumerable<NumericsInput>>? ComparedSource { get; set; }
}
