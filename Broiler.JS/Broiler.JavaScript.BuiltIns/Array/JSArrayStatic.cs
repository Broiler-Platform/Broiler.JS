using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.BuiltIns.Proxy;
using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array;


public partial class JSArray
{
    [JSExport("from", Length = 1)]
    public static JSValue StaticFrom(in Arguments a)
    {
        var (f, map, mapThis) = a.Get3();
        if (f.IsNullOrUndefined)
            throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

        if (!map.IsUndefined && !map.IsFunction)
            throw JSEngine.NewTypeError("mapFn must be a function");

        var t = a.This;
        var constructor = JSConstructorOperations.IsConstructor(t) && t is JSObject ctor ? ctor : null;
        var iteratorMethod = JSValue.SymbolIterator == null ? JSUndefined.Value : f.PropertyOrUndefined(JSValue.SymbolIterator);
        // A String is always iterable (by code point), even though @@iterator
        // lookup on a primitive string value currently misses the prototype.
        var useArrayLike = (iteratorMethod.IsUndefined || iteratorMethod.IsNull) && !f.IsString;

        if (useArrayLike)
        {
            var arrayLike = ToArrayLikeObject(f);
            var length = GetArrayLikeLength(arrayLike);
            var arrayLikeResult = constructor != null
                ? constructor.CreateInstance(new Arguments(JSUndefined.Value, JSValue.CreateNumber(length))) as JSObject
                : new JSArray();
            if (arrayLikeResult == null)
                throw JSEngine.NewTypeError("Array.from constructor must return an object");

            for (uint i = 0; i < length; i++)
            {
                var value = arrayLike[i];
                if (!map.IsUndefined)
                    value = map.InvokeFunction(new Arguments(mapThis, value, new JSNumber(i)));

                arrayLikeResult.SetPropertyOrThrow(JSValue.CreateNumber(i), value);
            }

            if (arrayLikeResult is JSArray arrayLikeArray)
                arrayLikeArray._length = length;
            else
                arrayLikeResult.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSValue.CreateNumber(length));

            return arrayLikeResult;
        }

        var r = constructor != null
            ? constructor.CreateInstance(new Arguments(JSUndefined.Value)) as JSObject
            : new JSArray();
        if (r == null)
            throw JSEngine.NewTypeError("Array.from constructor must return an object");

        var en = f.GetIterableEnumerator();
        uint index = 0;
        while (en.MoveNext(out var hasValue, out var item, out var _))
        {
            if (!hasValue)
                continue;

            if (!map.IsUndefined)
                item = map.InvokeFunction(new Arguments(mapThis, item, new JSNumber(index)));

            r.SetPropertyOrThrow(JSValue.CreateNumber(index++), item);
        }

        if (r is JSArray array)
            array._length = index;
        else
            r.SetPropertyOrThrow(KeyStrings.length.ToJSValue(), JSValue.CreateNumber(index));

        return r;
    }

    [JSExport("isArray", Length = 1)]
    public static JSValue StaticIsArray(in Arguments a) => IsArrayValue(a.Get1()) ? JSBoolean.True : JSBoolean.False;

    private static bool IsArrayValue(JSValue value)
    {
        if (value is JSArray)
            return true;

        if (value is JSProxy proxy)
            return IsArrayValue(proxy.RequireTarget());

        if (JSEngine.CurrentContext is JSObject global
            && global[KeyStrings.Array] is IJSFunction arrayCtor
            && ReferenceEquals(value, arrayCtor.Prototype))
        {
            return true;
        }

        return false;
    }

    [JSExport("of")]
    public static JSValue StaticOf(in Arguments a)
    {
        var r = new JSArray();
        var al = a.Length;
        ref var rElements = ref r.CreateElements();

        for (var ai = 0; ai < al; ai++)
            rElements.Put(r._length++, a.GetAt(ai));

        return r;
    }

    /// <summary>
    /// §4.5  Array.fromAsync(asyncIterable, mapFn?, thisArg?)
    /// Creates an array from an async iterable or iterable/array-like,
    /// returning a Promise that resolves to the new array.
    /// </summary>
    [JSExport("fromAsync", Length = 1)]
    public static JSValue StaticFromAsync(in Arguments a)
    {
        var (items, mapFn, thisArg) = a.Get3();

        if (items.IsNullOrUndefined)
            throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

        bool hasMap = mapFn.IsFunction;

        if (!mapFn.IsNullOrUndefined && !hasMap)
            throw JSEngine.NewTypeError("mapFn must be a function");

        try
        {
            var result = new JSArray();
            var en = items.GetElementEnumerator();
            uint length = 0;
            ref var elements = ref result.GetElements();

            while (en.MoveNext(out var hasValue, out var item, out var _))
            {
                if (!hasValue) continue;

                // Await-like: if the element is a promise/thenable, resolve it
                // synchronously for the current implementation.
                if (item is JSPromise p && p.state == JSPromise.PromiseState.Resolved)
                    item = p.result ?? item;

                if (hasMap)
                    item = mapFn.InvokeFunction(new Arguments(thisArg, item, new JSNumber(length)));

                elements.Put(length++, item);
            }

            result._length = length;

            return new JSPromise(result, JSPromise.PromiseState.Resolved);
        }
        catch (JSException ex)
        {
            return new JSPromise(ex.Error ?? JSError.From(ex), JSPromise.PromiseState.Rejected);
        }
        catch (Exception ex)
        {
            return new JSPromise(JSError.From(ex), JSPromise.PromiseState.Rejected);
        }
    }
}
