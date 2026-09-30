using System;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: an offset or length it carries yields a view that indexes past the end of its ArrayBuffer instead of a RangeError at construction
// Broiler-Human:        PENDING
public readonly struct TypedArrayParameters
{
    public readonly JSArrayBuffer buffer;
    public readonly int length;
    public readonly int bytesPerElement;
    public readonly int byteOffset;
    public readonly JSValue copyFrom;
    public readonly JSValue map;
    public readonly JSValue thisArg;
    public readonly JSObject prototype;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static TypedArrayParameters From(in Arguments a, int bytesPerElements)
    {
        var (f, map, mapThis) = a.Get3();
        return new TypedArrayParameters(f, map, mapThis, bytesPerElements, GetConstructorPrototype(a.This));
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static TypedArrayParameters Of(in Arguments a, int bytesPerElements) => new(a.Length, bytesPerElements, GetConstructorPrototype(a.This));

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private TypedArrayParameters(int length, int bytesPerElements, JSObject prototype)
    {
        buffer = null;
        this.length = length;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = null;
        map = null;
        thisArg = null;
        this.prototype = prototype;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private TypedArrayParameters(JSValue source, JSValue map, JSValue thisArg, int bytesPerElements, JSObject prototype)
    {
        buffer = null;
        length = -1;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = source;
        this.map = map;
        this.thisArg = thisArg;
        this.prototype = prototype;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a Uint8Array built from host bytes, such as by Uint8Array.fromHex, while a script constructor is running takes that constructor's prototype instead of Uint8Array.prototype
    // Broiler-Human:        PENDING
    public TypedArrayParameters(byte[] data, int bytesPerElements)
    {
        buffer = new JSArrayBuffer(data);
        length = data.Length / bytesPerElements;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = null;
        map = null;
        thisArg = null;
        prototype = JSEngine.NewTargetPrototype;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: new Float64Array(buffer, 1, len) runs the valueOf of len before throwing the RangeError for the misaligned byteOffset
    // Broiler-Human:        PENDING
    public TypedArrayParameters(
        in Arguments a, int bytesPerElements)
    {
        // TypedArray constructor step 1: invoking a TypedArray constructor as a plain
        // function (no `new`) throws a TypeError. This struct is built only on the
        // JS-facing `(in Arguments)` construction path, so the check belongs here; a
        // native [[Construct]] keeps its new.target in CurrentNewTarget, so both must
        // be null to be a plain [[Call]]. (Mirrors the ArrayBuffer constructor.)
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor TypedArray requires 'new'");

        buffer = null;
        length = -1;
        bytesPerElement = bytesPerElements;
        byteOffset = 0;
        copyFrom = null;
        map = null;
        thisArg = null;
        if (a.Length == 0)
        {
            // AllocateTypedArray(NewTarget, proto, 0): the new.target prototype is read.
            prototype = JSEngine.NewTargetPrototype;
            buffer = null;
            byteOffset = 0;
            length = 0;
            return;
        }
        var (a1, a2, a3) = a.Get3();
        if (a1 is JSArrayBuffer arrayBuffer)
        {
            // Object first argument (§23.2.5.1 step 6.b): AllocateTypedArray reads the
            // new.target prototype BEFORE InitializeTypedArrayFromArrayBuffer coerces
            // the byteOffset/length arguments.
            prototype = JSEngine.NewTargetPrototype;
            buffer = arrayBuffer;
            byteOffset = JSTypedArray.ToIntegerOrInfinity(a2);
            length = a3.IsUndefined ? -1 : ToTypedArrayLength(a3);
            return;
        }

        if (!a1.IsObject)
        {
            // Non-object first argument (§23.2.5.1 step 6.c): ToIndex(firstArgument)
            // runs BEFORE AllocateTypedArray reads the new.target prototype, so a
            // Symbol/BigInt length throws TypeError without ever evaluating a custom
            // prototype getter.
            buffer = null;
            byteOffset = 0;
            length = ToTypedArrayLength(a1);
            prototype = JSEngine.NewTargetPrototype;
            return;
        }

        // Object first argument that is not an ArrayBuffer (typed array / iterable /
        // array-like): AllocateTypedArray reads the prototype before the copy.
        prototype = JSEngine.NewTargetPrototype;
        copyFrom = a1;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a bound TypedArray constructor, whose prototype property is undefined, is rejected with a TypeError instead of falling back to the realm's default typed-array prototype
    // Broiler-Human:        PENDING
    private static JSObject GetConstructorPrototype(JSValue constructor)
    {
        if (constructor is not IJSFunction)
            throw JSEngine.NewTypeError("TypedArray constructor is not a constructor");

        if (constructor[KeyStrings.prototype] is JSObject prototype)
            return prototype;

        throw JSEngine.NewTypeError("TypedArray constructor is not a constructor");
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: new Float64Array(2**29) returns a view of length 0 instead of throwing a RangeError, because only the element count and not its byte size is held to 2147483647
    // Broiler-Human:        PENDING
    private static int ToTypedArrayLength(JSValue value)
    {
        // ToIndex: ToIntegerOrInfinity truncates toward zero FIRST (so -0.1 → -0,
        // NaN/undefined → 0), and only then is the sign / upper-bound checked. A
        // fractional value in (-1, 0) must not throw — it floors to 0.
        var numberLength = value.DoubleValue;
        if (double.IsNaN(numberLength))
            return 0;

        var integer = Math.Truncate(numberLength);
        if (integer < 0 || integer > 9007199254740991.0)
            throw JSEngine.NewRangeError("Invalid typed array length");

        if (integer > int.MaxValue)
            throw JSEngine.NewRangeError("Invalid typed array length");

        return (int)integer;
    }
}