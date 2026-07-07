using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("ArrayBuffer")]
public partial class JSArrayBuffer : JSObject
{
    private static JSArrayBuffer RequireArrayBuffer(JSValue value, string methodName)
    {
        // The ArrayBuffer.prototype methods are not generic over a SharedArrayBuffer
        // (IsSharedArrayBuffer(O) is true → TypeError), even though both share this C#
        // base type.
        if (value is SharedArrayBuffer)
            throw JSEngine.NewTypeError($"ArrayBuffer.prototype.{methodName} is not generic for SharedArrayBuffer");

        if (value is JSArrayBuffer arrayBuffer)
            return arrayBuffer;

        throw JSEngine.NewTypeError($"ArrayBuffer.prototype.{methodName} called on incompatible receiver");
    }

    private static JSValue ToNumberPrimitive(JSValue value)
    {
        if (value is not JSObject @object)
            return value;

        var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
        {
            var primitive = toPrimitive.InvokeFunction(new Arguments(@object, JSConstants.Number));
            if (primitive.IsObject)
                throw JSEngine.NewTypeError("Cannot convert object to primitive value");

            return primitive;
        }

        if (@object[KeyStrings.valueOf] is IJSFunction valueOf)
        {
            var primitive = valueOf.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        if (@object[KeyStrings.toString] is IJSFunction toString)
        {
            var primitive = toString.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        throw JSEngine.NewTypeError("Cannot convert object to primitive value");
    }

    internal static int ToIntegerOrInfinity(JSValue value, int defaultValue)
    {
        if (value == null || value.IsUndefined)
            return defaultValue;

        var number = ToNumberPrimitive(value).DoubleValue;
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (double.IsPositiveInfinity(number))
            return int.MaxValue;

        if (double.IsNegativeInfinity(number))
            return int.MinValue;

        return (int)number;
    }

    internal static int ToBufferLength(JSValue value, int defaultValue)
    {
        var n = ToBufferLengthLong(value, defaultValue);
        // ToBufferLength is used for callers that allocate immediately (resize, slice, …) where
        // exceeding the host's byte-array bound is a RangeError at the point of use; for the
        // ArrayBuffer / SharedArrayBuffer constructors this check must instead happen AFTER
        // OrdinaryCreateFromConstructor has fired NewTarget's prototype getter, so they call
        // ToBufferLengthLong directly and gate the int.MaxValue check themselves.
        if (n > int.MaxValue)
            throw JSEngine.NewRangeError("ArrayBuffer allocation failed");
        return (int)n;
    }

    // ToIndex (ECMA-262 §7.1.22) — accepts up to 2^53-1 per spec, returning a long so the caller
    // can defer the host-side byte-array bound (Array.MaxLength ≈ int.MaxValue) to the actual
    // CreateByteDataBlock step (test262 ArrayBuffer/data-allocation-after-object-creation).
    internal static long ToBufferLengthLong(JSValue value, long defaultValue)
    {
        if (value == null || value.IsUndefined)
            return defaultValue;

        // ToIntegerOrInfinity truncates toward zero FIRST, so a fractional value in (-1, 0)
        // (e.g. -0.5) becomes -0 → 0 rather than a RangeError; only then is the sign / upper
        // bound (2^53-1) checked on the resulting integer.
        var number = Math.Truncate(ToNumberPrimitive(value).DoubleValue);
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (number < 0 || number > 9007199254740991d)
            throw JSEngine.NewRangeError("Invalid ArrayBuffer length");

        return (long)number;
    }

    private static JSValue GetSpeciesConstructor(JSArrayBuffer source)
    {
        var defaultConstructor = (JSEngine.Current as JSObject)?[KeyStrings.ArrayBuffer];
        var constructor = source[KeyStrings.constructor];
        // SpeciesConstructor: only an undefined "constructor" falls back to the default; any other
        // non-object value (e.g. null or a number) is a TypeError.
        if (constructor.IsUndefined)
            return defaultConstructor;
        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("ArrayBuffer constructor property is not an object");

        var species = constructor[(IJSSymbol)JSSymbol.species];
        if (species.IsNullOrUndefined)
            return defaultConstructor;

        if (species is not IJSFunction)
            throw JSEngine.NewTypeError("ArrayBuffer species constructor is not a constructor");

        return species;
    }

    [JSExport("isView", Length = 1)]
    public static JSValue IsView(in Arguments a)
        => a.Get1() is JSTypedArray || a.Get1() is DataView.DataView
            ? BooleanTrue
            : BooleanFalse;

    internal byte[] buffer;
    internal bool isDetached;
    internal bool isImmutable;
    // Maximum byte length of a resizable ArrayBuffer, or -1 for a fixed-length buffer.
    // A resizable buffer is reallocated in place on resize(); views read buffer.buffer
    // live, so they observe the new length automatically.
    internal int maxByteLength = -1;
    // True for a SharedArrayBuffer (single-agent here), which the ArrayBuffer.prototype
    // methods reject.
    internal bool isShared;

    internal bool IsResizable => maxByteLength >= 0;

    public byte[] Buffer => buffer;

    [JSExport(Length = 1)]
    public JSArrayBuffer(in Arguments a) : this()
    {
        // Step 1: a plain call (no new.target) is a TypeError. This must precede every coercion.
        RequireConstructor("ArrayBuffer");

        // ToIndex(length) runs before the options bag is observed, matching the spec
        // AllocateArrayBuffer evaluation order. The byteLength-vs-maxByteLength RangeError is
        // thrown here, BEFORE OrdinaryCreateFromConstructor reads NewTarget.prototype — but the
        // host-side data-block allocation check (length must fit a host byte array) is deferred
        // until after that read, since OrdinaryCreateFromConstructor precedes CreateByteDataBlock
        // in the spec (test262 ArrayBuffer/data-allocation-after-object-creation).
        long length = ToBufferLengthLong(a.Get1(), 0);
        long requestedMaxByteLength = ToMaxByteLengthOptionLong(a.GetAt(1));

        if (requestedMaxByteLength >= 0 && length > requestedMaxByteLength)
            throw JSEngine.NewRangeError("ArrayBuffer byteLength exceeds maxByteLength");

        // OrdinaryCreateFromConstructor — surface the NewTarget.prototype getter side effect now,
        // before the deferred data-block allocation check below. JSFunction.CreateInstance still
        // resolves and applies the prototype post-construction; this read is purely so a throwing
        // subclass `get prototype` fires ahead of "ArrayBuffer allocation failed".
        ForceNewTargetPrototypeAccess();

        if (length > int.MaxValue)
            throw JSEngine.NewRangeError("ArrayBuffer allocation failed");
        if (requestedMaxByteLength > int.MaxValue)
            throw JSEngine.NewRangeError("ArrayBuffer allocation failed");

        buffer = new byte[length];
        maxByteLength = requestedMaxByteLength < 0 ? -1 : (int)requestedMaxByteLength;
    }

    // Triggers the NewTarget.prototype getter (when constructing as a subclass via
    // Reflect.construct or `new (class extends X) {}`), without retaining the result —
    // JSFunction.CreateInstance still resolves and applies it post-construction. Used by
    // constructors whose ToIndex / option validation precedes OrdinaryCreateFromConstructor
    // but whose data-block allocation follows it, to keep the spec-mandated observable order.
    internal static void ForceNewTargetPrototypeAccess()
    {
        var newTarget = (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget;
        if (newTarget != null && newTarget is JSObject)
            _ = newTarget[KeyStrings.prototype];
    }

    // GetArrayBufferMaxByteLengthOption: a non-object options bag (or a missing
    // maxByteLength) selects a fixed-length buffer (-1); otherwise ToIndex the option.
    internal static int ToMaxByteLengthOption(JSValue options)
    {
        if (options is not JSObject optionsObject)
            return -1;

        var maxByteLengthValue = optionsObject[KeyStrings.GetOrCreate("maxByteLength")];
        if (maxByteLengthValue.IsUndefined)
            return -1;

        return ToBufferLength(maxByteLengthValue, 0);
    }

    // The long-returning variant used by the constructors so the int.MaxValue gate (host
    // allocation bound) can be deferred past OrdinaryCreateFromConstructor.
    internal static long ToMaxByteLengthOptionLong(JSValue options)
    {
        if (options is not JSObject optionsObject)
            return -1;

        var maxByteLengthValue = optionsObject[KeyStrings.GetOrCreate("maxByteLength")];
        if (maxByteLengthValue.IsUndefined)
            return -1;

        return ToBufferLengthLong(maxByteLengthValue, 0);
    }

    /// <summary>
    /// ArrayBuffer / SharedArrayBuffer ( … ) step 1: throw a TypeError when NewTarget is undefined
    /// (called as a plain function, not via <c>new</c> / <c>Reflect.construct</c>). The instance
    /// prototype itself is resolved later (JSFunction.CreateInstance defers it past a successful
    /// construction), so this check must NOT read <see cref="JSEngine.NewTargetPrototype"/> — both
    /// because that read can invoke a user <c>get prototype</c> accessor and because the spec orders
    /// the argument-validation RangeErrors before object creation.
    /// </summary>
    private protected static void RequireConstructor(string name)
    {
        // A native [[Construct]] keeps its new.target in CurrentNewTarget, so both must be null to
        // be a plain call.
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"Constructor {name} requires 'new'");
    }

    public JSArrayBuffer(int length) : this() => buffer = new byte[length];
    public JSArrayBuffer(byte[] buffer) : this() => this.buffer = buffer;

    public override bool BooleanValue => true;

    public override double DoubleValue => double.NaN;

    public override bool Equals(JSValue value) => ReferenceEquals(this, value);

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError($"{this} is not a function");

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    // ---------------------------------------------------------------
    // §2.9  ArrayBuffer.prototype.byteLength (getter)
    // ---------------------------------------------------------------

    // These ArrayBuffer.prototype accessors are not generic over a SharedArrayBuffer:
    // IsSharedArrayBuffer(O) is true → TypeError (SharedArrayBuffer.prototype defines its
    // own byteLength / maxByteLength / growable accessors instead).
    private void RejectShared(string accessor)
    {
        if (isShared)
            throw JSEngine.NewTypeError($"ArrayBuffer.prototype.{accessor} is not generic for SharedArrayBuffer");
    }

    [JSExport("byteLength")]
    public int ByteLength
    {
        get
        {
            RejectShared("byteLength");

            // §25.1.6.2 get ArrayBuffer.prototype.byteLength: if the buffer is
            // detached, return +0 rather than throwing.
            if (isDetached)
                return 0;

            return buffer.Length;
        }
    }

    // ---------------------------------------------------------------
    // §2.9  ArrayBuffer.prototype.detached (getter)
    // ---------------------------------------------------------------

    [JSExport("detached")]
    public bool Detached
    {
        get
        {
            RejectShared("detached");
            return isDetached;
        }
    }

    [JSExport("immutable")]
    public bool Immutable
    {
        get
        {
            RejectShared("immutable");
            return isImmutable;
        }
    }

    // ---------------------------------------------------------------
    // get ArrayBuffer.prototype.maxByteLength
    // ---------------------------------------------------------------

    [JSExport("maxByteLength")]
    public int MaxByteLength
    {
        get
        {
            RejectShared("maxByteLength");

            if (isDetached)
                return 0;

            // For a fixed-length buffer the spec defines maxByteLength as its byteLength.
            return IsResizable ? maxByteLength : buffer.Length;
        }
    }

    // ---------------------------------------------------------------
    // get ArrayBuffer.prototype.resizable
    // ---------------------------------------------------------------

    [JSExport("resizable")]
    public bool Resizable
    {
        get
        {
            RejectShared("resizable");
            return IsResizable;
        }
    }

    // ---------------------------------------------------------------
    // ArrayBuffer.prototype.resize(newLength)
    // ---------------------------------------------------------------

    [JSExport("resize", Length = 1)]
    internal JSValue Resize(in Arguments a)
    {
        var source = RequireArrayBuffer(a.This, "resize");

        // A fixed-length buffer cannot be resized; this is checked before the argument
        // is coerced (the spec validates IsFixedLengthArrayBuffer before ToIndex).
        if (!source.IsResizable)
            throw JSEngine.NewTypeError("ArrayBuffer.prototype.resize: buffer is not resizable");

        int newByteLength = ToBufferLength(a.Get1(), 0);

        if (source.isDetached)
            throw JSEngine.NewTypeError("Cannot resize a detached ArrayBuffer");

        if (newByteLength > source.maxByteLength)
            throw JSEngine.NewRangeError("ArrayBuffer.prototype.resize: newLength exceeds maxByteLength");

        // Reallocate, preserving the overlapping prefix; grown bytes are zero. Views hold
        // the JSArrayBuffer (not the raw array) and read buffer.buffer on each access, so
        // they observe the new length without any further bookkeeping.
        var resized = new byte[newByteLength];
        System.Array.Copy(source.buffer, resized, Math.Min(source.buffer.Length, newByteLength));
        source.buffer = resized;

        return JSUndefined.Value;
    }

    // ---------------------------------------------------------------
    // §2.9.1  ArrayBuffer.prototype.transfer(newLength?)
    // ---------------------------------------------------------------

    [JSExport("transfer", Length = 0)]
    internal JSValue Transfer(in Arguments a)
        => TransferImpl(in a, "transfer", preserveResizability: true);

    // ---------------------------------------------------------------
    // §2.9.2  ArrayBuffer.prototype.transferToFixedLength(newLength?)
    // ---------------------------------------------------------------

    [JSExport("transferToFixedLength", Length = 0)]
    internal JSValue TransferToFixedLength(in Arguments a)
        => TransferImpl(in a, "transferToFixedLength", preserveResizability: false);

    // ArrayBufferCopyAndDetach: copies the source bytes into a new buffer of newLength and detaches
    // the source. With preserveResizability the new buffer keeps the source's maxByteLength (so a
    // resizable source yields a resizable result); transferToFixedLength always yields a fixed buffer.
    private JSValue TransferImpl(in Arguments a, string method, bool preserveResizability)
    {
        var source = RequireArrayBuffer(a.This, method);

        // Coerce newLength (ToIndex, which may invoke valueOf) BEFORE the detached /
        // immutable validation: the spec performs the argument conversion first, so
        // its side effects must run even when the method ultimately throws.
        int newLength = a.Length > 0
            ? ToBufferLength(a.Get1(), source.buffer.Length)
            : source.buffer.Length;

        if (source.isDetached)
            throw JSEngine.NewTypeError("Cannot transfer a detached ArrayBuffer");
        if (source.isImmutable)
            throw JSEngine.NewTypeError("Cannot transfer an immutable ArrayBuffer");

        var newMaxByteLength = preserveResizability && source.IsResizable ? source.maxByteLength : -1;
        if (newMaxByteLength >= 0 && newLength > newMaxByteLength)
            throw JSEngine.NewRangeError("ArrayBuffer byteLength exceeds maxByteLength");

        var newBuffer = new byte[newLength];
        System.Array.Copy(source.buffer, newBuffer, Math.Min(source.buffer.Length, newLength));

        // Detach the source buffer.
        source.isDetached = true;
        source.buffer = System.Array.Empty<byte>();

        return new JSArrayBuffer(newBuffer) { maxByteLength = newMaxByteLength };
    }

    // ---------------------------------------------------------------
    // ArrayBuffer.prototype.transferToImmutable(newLength?)  [proposal]
    // ---------------------------------------------------------------

    [JSExport("transferToImmutable", Length = 0)]
    internal JSValue TransferToImmutable(in Arguments a)
    {
        var source = RequireArrayBuffer(a.This, "transferToImmutable");

        // Coerce newLength (ToIndex, may invoke valueOf) BEFORE the detached /
        // immutable validation, matching the spec evaluation order.
        int newLength = a.Length > 0
            ? ToBufferLength(a.Get1(), source.buffer.Length)
            : source.buffer.Length;

        if (source.isDetached)
            throw JSEngine.NewTypeError("Cannot transfer a detached ArrayBuffer");
        if (source.isImmutable)
            throw JSEngine.NewTypeError("Cannot transfer an immutable ArrayBuffer");

        var newBuffer = new byte[newLength];
        System.Array.Copy(source.buffer, newBuffer, Math.Min(source.buffer.Length, newLength));

        // Detach the source buffer; the copy becomes a fixed-length immutable buffer.
        source.isDetached = true;
        source.buffer = System.Array.Empty<byte>();

        return new JSArrayBuffer(newBuffer) { isImmutable = true };
    }

    // ---------------------------------------------------------------
    // §2.9.3  ArrayBuffer.prototype.slice(begin, end)
    // ---------------------------------------------------------------

    [JSExport("slice")]
    internal JSValue Slice(in Arguments a)
    {
        var source = RequireArrayBuffer(a.This, "slice");
        if (source.isDetached)
            throw JSEngine.NewTypeError("Cannot slice a detached ArrayBuffer");

        int len = source.buffer.Length;
        var (beginVal, endVal) = a.Get2();

        int begin = ToIntegerOrInfinity(beginVal, 0);
        int end = ToIntegerOrInfinity(endVal, len);

        if (begin < 0) begin = Math.Max(len + begin, 0);
        else begin = Math.Min(begin, len);

        if (end < 0) end = Math.Max(len + end, 0);
        else end = Math.Min(end, len);

        int newLen = Math.Max(end - begin, 0);
        var ctor = GetSpeciesConstructor(source);
        var created = ctor?.CreateInstance(CreateNumber(newLen)) ?? new JSArrayBuffer(newLen);
        if (created is not JSArrayBuffer target)
            throw JSEngine.NewTypeError("ArrayBuffer species constructor did not return an ArrayBuffer");
        if (target.isImmutable)
            throw JSEngine.NewTypeError("ArrayBuffer species constructor returned an immutable ArrayBuffer");

        if (ReferenceEquals(target, source))
            throw JSEngine.NewTypeError("ArrayBuffer species constructor returned the original ArrayBuffer");

        if (target.isDetached)
            throw JSEngine.NewTypeError("ArrayBuffer species constructor returned a detached ArrayBuffer");

        if (target.buffer.Length < newLen)
            throw JSEngine.NewTypeError("ArrayBuffer species constructor returned a too-small ArrayBuffer");

        System.Array.Copy(source.buffer, begin, target.buffer, 0, newLen);
        return target;
    }

    // ---------------------------------------------------------------
    // ArrayBuffer.prototype.sliceToImmutable(begin, end)  [proposal]
    // ---------------------------------------------------------------

    [JSExport("sliceToImmutable")]
    internal JSValue SliceToImmutable(in Arguments a)
    {
        var source = RequireArrayBuffer(a.This, "sliceToImmutable");
        if (source.isDetached)
            throw JSEngine.NewTypeError("Cannot sliceToImmutable a detached ArrayBuffer");

        int len = source.buffer.Length;
        var (beginVal, endVal) = a.Get2();

        int begin = ToIntegerOrInfinity(beginVal, 0);
        int end = ToIntegerOrInfinity(endVal, len);

        if (begin < 0) begin = Math.Max(len + begin, 0);
        else begin = Math.Min(begin, len);

        if (end < 0) end = Math.Max(len + end, 0);
        else end = Math.Min(end, len);

        int newLen = Math.Max(end - begin, 0);
        var result = new JSArrayBuffer(newLen);
        result.isImmutable = true;
        System.Array.Copy(source.buffer, begin, result.buffer, 0, newLen);
        return result;
    }
}
