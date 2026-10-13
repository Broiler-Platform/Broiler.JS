// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Number/JSNumberPrototype.cs
//     blob 57034b8bbe140141fc7ff74110d1851ea350141e: ToIntegerOrInfinity (:252-258)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSTypedArray.cs
//     blob dc63ebcf4f5a937ba4aefa3a904a6716776cfb79: ToIntegerOrInfinity (:46-62), ToArrayLikeLength (:368-387) and
//     FromShared's array-like length (:468-470)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSArrayBuffer.cs
//     blob 6066c08bbe6b1d3b76cfb7919e83fd10a61e959a: ToIntegerOrInfinity (:60-76), ToBufferLength (:78-89) and
//     ToBufferLengthLong (:91-110)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/JSArrayPrototype.Iteration.cs
//     blob 334e80d7a9aaba0e784f7d083656d9db2de12451: MaxArrayLikeLength (:21), ToLength (:127-140) and
//     ToIntegerOrInfinity (:145-161)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSTypedArray.prototype.cs
//     blob e0d44f367193b430befb4dcbd1e40094efb0fb33: ToIntegerOrInfinityIndex (:23-42)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Objects/JSAtomics.cs
//     blob bff6404d1209bb56aac2527162bc4e8bd0b3a291: ToIndex (:94-109) and ToIntegerOperand (:111-123)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/BigInt/JSBigInt.cs
//     blob 1d2bf1c1a37c5604bdd6776ada12ae184be276ba: JSToString's radix (:565-576) and ToBitsIndex (:622-636)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntl.cs
//     blob 40d15926fa075a9cd864e4e004f849177395cb48: JSIntl.CanonicalizeLocaleList's length (:1318-1327) and
//     JSIntlSegments.ContainingPrototype's index (:3189-3191)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/BuiltInsAssemblyInitializer.cs
//     blob ad47ad4d90bc820b767a5c2fe46c7154b80f221b: PatchRegExpPrototype's local functions ToLength (:1478-1489)
//     and ToLengthDouble (:1491-1502), and its OneReplacement's position (:1847-1852)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Json/JSJSON.cs
//     blob 642f56c9646f4c1ea8eb6ad89cbcc09b0b6c093f: MaxArrayLikeLength (:40) and ToLength (:83-96)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Proxy/JSProxy.cs
//     blob 7eec935d8269981878c6f54ee5403420e5218675: ToArrayLikeLength (:913-922)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/DataView/DataView.cs
//     blob f1ba2dee735e5af368bb96b2192582bd60a7045c: ToIndex (:166-184) and ToByteOffset (:263-282)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/TypedArrayParameters.cs
//     blob 15df43fa873e454734f5dc8035fa956a4817e577: ToTypedArrayLength (:156-173)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; the helpers sit in a static class of their own, one method per site, named
//     <source class>_<source member>, made internal; the inline sites are methods of their own;
//   - each takes the double the engine has after ToNumber: the source's `value.DoubleValue`, `ToNumber(value)` or
//     `ToNumberPrimitive(value).DoubleValue` (and, for ToArrayLikeLength, FromShared and CanonicalizeLocaleList,
//     the Get of "length" before it) is the parameter, named as the local it initialises (doubleValue where the
//     source truncates it in the same expression, radixArg after JSToString's argument);
//   - the `value == null || value.IsUndefined` / `radixArg.IsUndefined` / `lengthValue.IsUndefined` branches are
//     the adapter's. Most return what NaN returns (DataView.ToIndex, JSAtomics.ToIndex, ToBitsIndex,
//     JSJSON.ToLength, JSArrayPrototype.Iteration.ToLength: 0; CanonicalizeLocaleList: an empty list); the
//     others give the caller's defaultValue (the four ToIntegerOrInfinity helpers that take one, and
//     ToBufferLengthLong) or radix 10 (JSToString), which the adapter must supply for undefined;
//   - the [MethodImpl(MethodImplOptions.AggressiveInlining)] on ToByteOffset and on PatchRegExpPrototype's local
//     functions is not copied;
//   - a RangeError is a null result (the return type of a method whose source throws one is nullable) and the
//     source's throw is kept in a comment beside it; ToBufferLength returns null when ToBufferLengthLong does, where the source's exception
//     propagates;
//   - ToArrayLikeLength's instance field bytesPerElement and OneReplacement's input.Length are parameters;
//     FromShared's call of ToIntegerOrInfinity (defaultValue 0) and ToBufferLength's of ToBufferLengthLong are
//     calls of this class's copies;
//   - the class constant MaxArrayLikeLength (9007199254740991d in both sources) is a local constant of the
//     method that reads it;
//   - the Broiler Code Assurance annotations on the copied members are not copied.

using System;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>The engine's ToIntegerOrInfinity, ToLength and ToIndex helpers, as functions of the ToNumber double.</summary>
internal static class IntegerHelpers
{
    // ---------------------------------------------------------------------------------------------
    // ToIntegerOrInfinity

    // ToIntegerOrInfinity (ECMA-262 7.1.5): ToNumber, which throws on a Symbol or a BigInt, then
    // NaN is 0 and the rest is truncated toward zero.
    internal static double JSNumberPrototype_ToIntegerOrInfinity(double number)
    {
        return double.IsNaN(number) ? 0 : Math.Truncate(number);
    }

    // JSTypedArray.ToIntegerOrInfinity(JSValue value, int defaultValue = 0).
    internal static int JSTypedArray_ToIntegerOrInfinity(double number)
    {
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number) || number > int.MaxValue)
            return int.MaxValue;

        if (double.IsNegativeInfinity(number) || number < int.MinValue)
            return int.MinValue;

        return (int)Math.Truncate(number);
    }

    // JSArrayBuffer.ToIntegerOrInfinity(JSValue value, int defaultValue).
    internal static int JSArrayBuffer_ToIntegerOrInfinity(double number)
    {
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number))
            return int.MaxValue;

        if (double.IsNegativeInfinity(number))
            return int.MinValue;

        return (int)number;
    }

    // JSArrayPrototype.ToIntegerOrInfinity(JSValue value, long defaultValue = 0) (JSArrayPrototype.Iteration.cs).
    internal static long JSArrayPrototypeIteration_ToIntegerOrInfinity(double number)
    {
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number))
            return long.MaxValue;

        if (double.IsNegativeInfinity(number))
            return long.MinValue;

        return (long)number;
    }

    // §7.1.5 ToIntegerOrInfinity: truncate toward zero, with ±Infinity mapped to
    // long.MaxValue / long.MinValue sentinels. Unlike IntValue (which is ToUint32 and
    // collapses Infinity to 0), this preserves the infinite magnitude so index callers
    // clamp it correctly — e.g. indexOf(x, Infinity) is past the end, not index 0
    // (test262 TypedArray/prototype/{indexOf,lastIndexOf}/fromIndex-infinity).
    internal static long JSTypedArrayPrototype_ToIntegerOrInfinityIndex(double number)
    {
        if (double.IsNaN(number) || number == 0)
            return 0;
        if (double.IsPositiveInfinity(number))
            return long.MaxValue;
        if (double.IsNegativeInfinity(number))
            return long.MinValue;

        return (long)number;
    }

    // ToIntegerOrInfinity → low bits of the element; ToInt32(±Infinity)/NaN is 0, so an
    // out-of-long operand collapses to 0 just as the element write would.
    internal static long JSAtomics_ToIntegerOperand(double number)
    {
        if (double.IsNaN(number) || double.IsInfinity(number))
            return 0;

        var integer = Math.Truncate(number);
        if (integer >= long.MaxValue) return long.MaxValue;
        if (integer <= long.MinValue) return long.MinValue;
        return (long)integer;
    }

    // JSBigInt.JSToString (:565-576), for a radix argument that is not undefined (undefined is radix 10).
    internal static int? JSBigInt_JSToString_Radix(double radixArg)
    {
        // BigInt.prototype.toString(radix): undefined radix defaults to 10, any other
        // value is coerced with ToIntegerOrInfinity and must land in [2, 36].
        var radixMV = Math.Truncate(radixArg);
        if (double.IsNaN(radixMV))
            radixMV = 0;
        if (radixMV < 2 || radixMV > 36)
            return null; // throw JSEngine.NewRangeError("toString() radix must be between 2 and 36");
        var radix = (int)radixMV;
        return radix;
    }

    // JSIntlSegments.ContainingPrototype (JSIntl.cs :3189-3191).
    internal static double JSIntlSegments_ContainingPrototype_Index(double number)
    {
        // ToInteger(index): ToNumber then NaN/±0 → 0, otherwise truncate toward zero.
        var n = double.IsNaN(number) ? 0 : Math.Truncate(number);
        return n;
    }

    // BuiltInsAssemblyInitializer.PatchRegExpPrototype's OneReplacement (:1847-1852).
    internal static int BuiltInsAssemblyInitializer_OneReplacement_Position(double rawPosition, int inputLength)
    {
        // §22.2.6.11 step 16.f-g: position is ToIntegerOrInfinity(Get(result, "index"))
        // clamped to [0, lengthS]. An ill-behaving exec (e.g. a Proxy result) may report
        // an index past the end of the subject string; clamping keeps the later
        // input.AsSpan(...) slices in range instead of throwing ArgumentOutOfRangeException.
        var position = (int)Math.Clamp(double.IsNaN(rawPosition) ? 0 : Math.Truncate(rawPosition), 0, inputLength);
        return position;
    }

    // ---------------------------------------------------------------------------------------------
    // ToLength

    // JSJSON.ToLength.
    internal static long JSJSON_ToLength(double length)
    {
        const double MaxArrayLikeLength = 9007199254740991d;

        if (double.IsNaN(length) || length <= 0)
            return 0;

        if (double.IsPositiveInfinity(length) || length >= MaxArrayLikeLength)
            return (long)MaxArrayLikeLength;

        return (long)Math.Floor(length);
    }

    // JSArrayPrototype.ToLength (JSArrayPrototype.Iteration.cs).
    internal static double JSArrayPrototypeIteration_ToLength(double length)
    {
        const double MaxArrayLikeLength = 9007199254740991d;

        if (double.IsNaN(length) || length <= 0)
            return 0;

        if (double.IsPositiveInfinity(length) || length >= MaxArrayLikeLength)
            return MaxArrayLikeLength;

        return Math.Floor(length);
    }

    // ToLength(Get(arrayLike, "length")): ToNumber, then clamp to [0, 2^53-1].
    internal static long JSProxy_ToArrayLikeLength(double number)
    {
        if (double.IsNaN(number) || number <= 0)
            return 0;

        const double maxSafeInteger = 9007199254740991.0; // 2^53 - 1
        return number >= maxSafeInteger ? (long)maxSafeInteger : (long)Math.Floor(number);
    }

    // BuiltInsAssemblyInitializer.PatchRegExpPrototype's local ToLength (:1478-1489).
    internal static int BuiltInsAssemblyInitializer_ToLength(double length)
    {
        if (double.IsNaN(length) || length <= 0)
            return 0;

        if (length >= int.MaxValue)
            return int.MaxValue;

        return (int)length;
    }

    // ToLength as a double (clamped to the integer index range [0, 2^53-1]) for the
    // lastIndex advance, where the value can legitimately exceed int.MaxValue.
    internal static double BuiltInsAssemblyInitializer_ToLengthDouble(double doubleValue)
    {
        var length = Math.Truncate(doubleValue);
        if (double.IsNaN(length) || length <= 0)
            return 0;

        const double MaxSafeLength = 9007199254740991d; // 2^53 - 1
        return length > MaxSafeLength ? MaxSafeLength : length;
    }

    // LengthOfArrayLike(source) for the array-like constructor path: ToLength(Get(source, "length")) —
    // a missing/NaN/negative length becomes 0 and the value is clamped to 2^53-1 — then
    // AllocateTypedArrayBuffer must allocate len × elementSize bytes, so a length whose byte size
    // cannot fit a data block (e.g. {length: 2**53} or {length: 2**32}) is a RangeError rather than
    // silently wrapping to a smaller count.
    internal static int? JSTypedArray_ToArrayLikeLength(double number, int bytesPerElement)
    {
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0)
            integer = 0;

        if (integer > 9007199254740991.0 || (long)integer * bytesPerElement > int.MaxValue)
            return null; // throw JSEngine.NewRangeError("Invalid typed array length");

        return (int)integer;
    }

    // JSTypedArray.FromShared (:468-470).
    internal static int JSTypedArray_FromShared_Length(double number)
    {
        // Array-like path: ToLength(Get(source, "length")); a missing length is 0.
        var rawLength = JSTypedArray_ToIntegerOrInfinity(number);
        var length = rawLength < 0 ? 0 : rawLength;
        return length;
    }

    // JSIntl.CanonicalizeLocaleList (:1318-1327).
    internal static uint JSIntl_CanonicalizeLocaleList_Length(double lengthNumber)
    {
        // ToLength(Get(O, "length")): ToNumber (a Symbol length throws a TypeError via DoubleValue),
        // truncate toward zero, then clamp a negative result to 0 — so a negative length yields an empty
        // list and the indexed getters are never read (rather than wrapping the negative into a uint).
        var lengthInteger = double.IsNaN(lengthNumber) ? 0 : Math.Truncate(lengthNumber);
        if (lengthInteger < 0)
            lengthInteger = 0;
        else if (lengthInteger > 4294967295d)
            lengthInteger = 4294967295d;
        var length = (uint)lengthInteger;
        return length;
    }

    // ---------------------------------------------------------------------------------------------
    // ToIndex

    // ToIndex (abstract operation): ToNumber the argument (observing valueOf), truncate toward zero,
    // and require the result to be an integer index in [0, 2^53-1]; undefined / NaN map to 0 and a
    // negative or out-of-range value (including ±Infinity) is a RangeError. Returned as a long so the
    // buffer-bounds comparisons happen before the value is narrowed to the int offset/length fields.
    internal static long? DataView_ToIndex(double number)
    {
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0 || integer > 9007199254740991d) // 2^53 - 1
            return null; // throw JSEngine.NewRangeError("DataView offset or length is out of range.");

        return (long)integer;
    }

    // ToIndex(value): coerce to an integer index in [0, 2^53-1], else RangeError.
    // Performed before ToBigInt so that side effects occur in spec order
    // (BigInt.asIntN/asUintN step 1 runs before step 2).
    internal static long? JSBigInt_ToBitsIndex(double number)
    {
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0 || integer > 9007199254740991.0)
            return null; // throw JSEngine.NewRangeError("Invalid range for bits");

        return (long)integer;
    }

    // DataView.ToByteOffset.
    internal static int? DataView_ToByteOffset(double number)
    {
        // ToIndex (SetViewValue step 4 / GetViewValue step 4): ToNumber then truncate
        // toward zero; NaN/undefined → 0; a negative value or one above 2^53-1 (including
        // ±Infinity) is a RangeError. This runs BEFORE the value coercion in the set*
        // methods, so an out-of-range index is rejected before the value's valueOf is
        // observed (test262: DataView/prototype/set*/index-check-before-value-conversion).
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0 || integer > 9007199254740991d) // [0, 2^53 - 1]
            return null; // throw JSEngine.NewRangeError("DataView offset is outside the bounds of the buffer.");

        // A valid index that still overflows int is clamped so the per-access bounds
        // check (getIndex + elementSize > viewSize) reports it as out of range.
        return integer > int.MaxValue ? int.MaxValue : (int)integer;
    }

    // JSAtomics.ToIndex.
    internal static int? JSAtomics_ToIndex(double number)
    {
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);

        if (integer < 0 || integer > 9007199254740991d)
            return null; // throw JSEngine.NewRangeError("Invalid atomic access index");

        if (integer > int.MaxValue)
            return null; // throw JSEngine.NewRangeError("Invalid atomic access index");

        return (int)integer;
    }

    // TypedArrayParameters.ToTypedArrayLength.
    internal static int? TypedArrayParameters_ToTypedArrayLength(double numberLength)
    {
        // ToIndex: ToIntegerOrInfinity truncates toward zero FIRST (so -0.1 → -0,
        // NaN/undefined → 0), and only then is the sign / upper-bound checked. A
        // fractional value in (-1, 0) must not throw — it floors to 0.
        if (double.IsNaN(numberLength))
            return 0;

        var integer = Math.Truncate(numberLength);
        if (integer < 0 || integer > 9007199254740991.0)
            return null; // throw JSEngine.NewRangeError("Invalid typed array length");

        if (integer > int.MaxValue)
            return null; // throw JSEngine.NewRangeError("Invalid typed array length");

        return (int)integer;
    }

    // JSArrayBuffer.ToBufferLength(JSValue value, int defaultValue).
    internal static int? JSArrayBuffer_ToBufferLength(double doubleValue)
    {
        var n = JSArrayBuffer_ToBufferLengthLong(doubleValue);
        if (n is null)
            return null; // ToBufferLengthLong's RangeError propagates
        // ToBufferLength is used for callers that allocate immediately (resize, slice, …) where
        // exceeding the host's byte-array bound is a RangeError at the point of use; for the
        // ArrayBuffer / SharedArrayBuffer constructors this check must instead happen AFTER
        // OrdinaryCreateFromConstructor has fired NewTarget's prototype getter, so they call
        // ToBufferLengthLong directly and gate the int.MaxValue check themselves.
        if (n > int.MaxValue)
            return null; // throw JSEngine.NewRangeError("ArrayBuffer allocation failed");
        return (int)n;
    }

    // ToIndex (ECMA-262 §7.1.22) — accepts up to 2^53-1 per spec, returning a long so the caller
    // can defer the host-side byte-array bound (Array.MaxLength ≈ int.MaxValue) to the actual
    // CreateByteDataBlock step (test262 ArrayBuffer/data-allocation-after-object-creation).
    internal static long? JSArrayBuffer_ToBufferLengthLong(double doubleValue)
    {
        // ToIntegerOrInfinity truncates toward zero FIRST, so a fractional value in (-1, 0)
        // (e.g. -0.5) becomes -0 → 0 rather than a RangeError; only then is the sign / upper
        // bound (2^53-1) checked on the resulting integer.
        var number = Math.Truncate(doubleValue);
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (number < 0 || number > 9007199254740991d)
            return null; // throw JSEngine.NewRangeError("Invalid ArrayBuffer length");

        return (long)number;
    }
}
