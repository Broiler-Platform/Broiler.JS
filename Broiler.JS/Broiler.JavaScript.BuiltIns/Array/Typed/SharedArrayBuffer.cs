using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

// ES2025 SharedArrayBuffer. This engine is single-agent (no real threads), so a
// SharedArrayBuffer behaves like an ArrayBuffer that can never be detached, transferred,
// or made immutable, and that grows (never shrinks) when growable. It shares the C#
// JSArrayBuffer base purely for storage/view reuse; the JS-level prototype chain is
// independent (it does NOT inherit ArrayBuffer.prototype), and the ArrayBuffer.prototype
// methods reject a SharedArrayBuffer receiver.
// [JSBaseClass("Object")]: SharedArrayBuffer reuses the JSArrayBuffer C# storage but its
// JS prototype chain is Object.prototype (it does NOT inherit ArrayBuffer.prototype). The
// attribute suppresses the generator's default base-class prototype wiring.
[JSClassGenerator("SharedArrayBuffer"), JSBaseClass("Object")]
public partial class SharedArrayBuffer : JSArrayBuffer
{
    [JSExport(Length = 1)]
    public SharedArrayBuffer(in Arguments a) : base()
    {
        // Step 1: a plain call is a TypeError; then validate byteLength vs maxByteLength — both
        // before the instance prototype is resolved (deferred to JSFunction.CreateInstance), so a
        // throwing new.target `get prototype` is never observed.
        RequireConstructor("SharedArrayBuffer");

        int length = ToBufferLength(a.Get1(), 0);
        int requestedMaxByteLength = ToMaxByteLengthOption(a.GetAt(1));

        if (requestedMaxByteLength >= 0 && length > requestedMaxByteLength)
            throw JSEngine.NewRangeError("SharedArrayBuffer byteLength exceeds maxByteLength");

        buffer = new byte[length];
        maxByteLength = requestedMaxByteLength;
        isShared = true;
    }

    private static SharedArrayBuffer RequireShared(JSValue value, string methodName)
    {
        if (value is SharedArrayBuffer shared)
            return shared;

        throw JSEngine.NewTypeError($"SharedArrayBuffer.prototype.{methodName} called on incompatible receiver");
    }

    // A growable SharedArrayBuffer reports its maximum; a non-growable one reports its
    // (immutable) byteLength.
    internal bool IsGrowable => maxByteLength >= 0;

    // get SharedArrayBuffer.prototype.byteLength
    [JSExport("byteLength")]
    public int SharedByteLength => buffer.Length;

    // get SharedArrayBuffer.prototype.maxByteLength
    [JSExport("maxByteLength")]
    public int SharedMaxByteLength => IsGrowable ? maxByteLength : buffer.Length;

    // get SharedArrayBuffer.prototype.growable
    [JSExport("growable")]
    public bool Growable => IsGrowable;

    // SharedArrayBuffer.prototype.grow(newLength)
    [JSExport("grow", Length = 1)]
    internal JSValue Grow(in Arguments a)
    {
        var source = RequireShared(a.This, "grow");

        if (!source.IsGrowable)
            throw JSEngine.NewTypeError("SharedArrayBuffer.prototype.grow: buffer is not growable");

        int newByteLength = ToBufferLength(a.Get1(), 0);

        // A SharedArrayBuffer may only grow: a request below the current length, or above
        // the maximum, is a RangeError.
        if (newByteLength < source.buffer.Length || newByteLength > source.maxByteLength)
            throw JSEngine.NewRangeError("SharedArrayBuffer.prototype.grow: invalid new length");

        var grown = new byte[newByteLength];
        System.Array.Copy(source.buffer, grown, source.buffer.Length);
        source.buffer = grown;

        return JSUndefined.Value;
    }

    // SharedArrayBuffer.prototype.slice(begin, end)
    [JSExport("slice")]
    internal JSValue SharedSlice(in Arguments a)
    {
        var source = RequireShared(a.This, "slice");

        int len = source.buffer.Length;
        var (beginVal, endVal) = a.Get2();

        int begin = ToIntegerOrInfinity(beginVal, 0);
        int end = ToIntegerOrInfinity(endVal, len);

        if (begin < 0) begin = Math.Max(len + begin, 0);
        else begin = Math.Min(begin, len);

        if (end < 0) end = Math.Max(len + end, 0);
        else end = Math.Min(end, len);

        int newLen = Math.Max(end - begin, 0);

        // The new buffer is allocated through SpeciesConstructor(O, %SharedArrayBuffer%); the
        // result must itself be a (distinct, large-enough) SharedArrayBuffer.
        var ctor = GetSharedSpeciesConstructor(source);
        var created = ctor?.CreateInstance(JSValue.CreateNumber(newLen)) ?? new SharedArrayBuffer(newLen);
        if (created is not SharedArrayBuffer target)
            throw JSEngine.NewTypeError("SharedArrayBuffer species constructor did not return a SharedArrayBuffer");
        if (ReferenceEquals(target, source))
            throw JSEngine.NewTypeError("SharedArrayBuffer species constructor returned the original SharedArrayBuffer");
        if (target.buffer.Length < newLen)
            throw JSEngine.NewTypeError("SharedArrayBuffer species constructor returned a too-small SharedArrayBuffer");

        System.Array.Copy(source.buffer, begin, target.buffer, 0, newLen);
        return target;
    }

    private static JSValue GetSharedSpeciesConstructor(SharedArrayBuffer source)
    {
        var defaultConstructor = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("SharedArrayBuffer")];
        var constructor = source[KeyStrings.constructor];
        // SpeciesConstructor: only an undefined "constructor" falls back to the default; any other
        // non-object value (e.g. null or a number) is a TypeError.
        if (constructor.IsUndefined)
            return defaultConstructor;
        if (!constructor.IsObject)
            throw JSEngine.NewTypeError("SharedArrayBuffer constructor property is not an object");

        var species = constructor[(IJSSymbol)JSSymbol.species];
        if (species.IsNullOrUndefined)
            return defaultConstructor;

        if (species is not IJSFunction)
            throw JSEngine.NewTypeError("SharedArrayBuffer species constructor is not a constructor");

        return species;
    }

    // Internal allocation used by slice; not reachable from JS without `new`.
    private SharedArrayBuffer(int length) : base(length) => isShared = true;
}
