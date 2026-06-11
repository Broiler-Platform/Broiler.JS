using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.BuiltIns.DataView;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("ArrayBuffer")]
public partial class JSArrayBuffer : JSObject
{
    private static JSArrayBuffer RequireArrayBuffer(JSValue value, string methodName)
    {
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

    private static int ToIntegerOrInfinity(JSValue value, int defaultValue)
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

    private static int ToBufferLength(JSValue value, int defaultValue)
    {
        // ToIndex: a value below 0 or above 2^53-1 is a RangeError (the upper
        // bound must be checked on the full double before the int cast, or a
        // value like 2^53 silently wraps and a later allocation throws a plain
        // Error instead of the spec-mandated RangeError).
        if (value == null || value.IsUndefined)
            return defaultValue;

        var number = ToNumberPrimitive(value).DoubleValue;
        if (double.IsNaN(number) || number == 0)
            return 0;

        if (number < 0 || number > 9007199254740991d)
            throw JSEngine.NewRangeError("Invalid ArrayBuffer length");

        if (number > int.MaxValue)
            throw JSEngine.NewRangeError("ArrayBuffer allocation failed");

        return (int)number;
    }

    private static JSValue GetSpeciesConstructor(JSArrayBuffer source)
    {
        var defaultConstructor = (JSEngine.Current as JSObject)?[KeyStrings.ArrayBuffer];
        var constructor = source[KeyStrings.constructor];
        if (!constructor.IsObject)
            return defaultConstructor;

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
            ? JSValue.BooleanTrue
            : JSValue.BooleanFalse;

    internal byte[] buffer;
    internal bool isDetached;
    internal bool isImmutable;

    public byte[] Buffer => buffer;

    [JSExport(Length = 1)]
    public JSArrayBuffer(in Arguments a) : this(JSEngine.NewTargetPrototype)
    {
        // ArrayBuffer ( ... ): step 1 — if NewTarget is undefined (called as a plain
        // function, not via `new`), throw a TypeError. A native [[Construct]] keeps
        // its new.target in CurrentNewTarget, so both must be null to be a plain call.
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor ArrayBuffer requires 'new'");

        int length = ToBufferLength(a.Get1(), 0);
        buffer = new byte[length];
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

    [JSExport("byteLength")]
    public int ByteLength
    {
        get
        {
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
    public bool Detached => isDetached;

    [JSExport("immutable")]
    public bool Immutable => isImmutable;

    // ---------------------------------------------------------------
    // §2.9.1  ArrayBuffer.prototype.transfer(newLength?)
    // ---------------------------------------------------------------

    [JSExport("transfer", Length = 0)]
    internal JSValue Transfer(in Arguments a)
    {
        var source = RequireArrayBuffer(a.This, "transfer");

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

        var newBuffer = new byte[newLength];
        System.Array.Copy(source.buffer, newBuffer, Math.Min(source.buffer.Length, newLength));

        // Detach the source buffer.
        source.isDetached = true;
        source.buffer = System.Array.Empty<byte>();

        return new JSArrayBuffer(newBuffer);
    }

    // ---------------------------------------------------------------
    // §2.9.2  ArrayBuffer.prototype.transferToFixedLength(newLength?)
    // ---------------------------------------------------------------

    [JSExport("transferToFixedLength", Length = 0)]
    internal JSValue TransferToFixedLength(in Arguments a)
    {
        // In engines without resizable buffers the behaviour is identical
        // to transfer().  Broiler.JavaScript does not support resizable ArrayBuffers,
        // so the result is always a fixed-length buffer.
        return Transfer(in a);
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
        var created = ctor?.CreateInstance(JSValue.CreateNumber(newLen)) ?? new JSArrayBuffer(newLen);
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
