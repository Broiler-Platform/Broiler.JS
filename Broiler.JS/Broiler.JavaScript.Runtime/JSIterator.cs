using System;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public struct JSIterator(JSValue iterator, bool awaitResult = false) : IElementEnumerator, IReturnableEnumerator
{
    private uint index = 0;

    private readonly JSValue GetIteratorResult()
    {
        var result = JSObjectCoreExtensions.InvokeMethodOn(iterator, KeyStrings.next);
        if (awaitResult && result is IJSPromise promise)
            result = promise.Task.GetAwaiter().GetResult();

        if (!result.IsObject)
            throw JSValue.NewTypeError("Iterator result is not an object");

        return result;
    }

    public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
    {
        value = GetIteratorResult();
        var done = value[KeyStrings.done];
        value = value[KeyStrings.value];
        
        if (done.BooleanValue)
        {
            index = 0;
            hasValue = false;
            return false;
        }
        
        index = this.index++;
        hasValue = true;
        return true;
    }

    public readonly bool MoveNext(out JSValue value)
    {
        value = GetIteratorResult();
        var done = value[KeyStrings.done];
        value = value[KeyStrings.value];
        
        if (done.BooleanValue)
            return false;

        return true;
    }

    public readonly bool MoveNextOrDefault(out JSValue value, JSValue @default)
    {
        value = GetIteratorResult();
        var done = value[KeyStrings.done];

        if (done.BooleanValue)
        {
            value = @default;
            return false;
        }

        value = value[KeyStrings.value];
        return true;
    }

    public readonly JSValue NextOrDefault(JSValue @default)
    {
        var value = GetIteratorResult();
        var done = value[KeyStrings.done];

        if (done.BooleanValue)
            return @default;

        return value[KeyStrings.value];
    }

    public readonly JSValue Return(JSValue value)
    {
        var method = iterator[KeyStrings.@return];
        if (method.IsUndefined || method.IsNull)
        {
            var iteratorResult = JSObject.NewWithProperties();
            iteratorResult.FastAddValue(KeyStrings.value, value, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
            iteratorResult.FastAddValue(KeyStrings.done, JSValue.BooleanTrue, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
            return iteratorResult;
        }

        var result = method.InvokeFunction(new Arguments(iterator, value));
        if (!result.IsObject)
            throw JSValue.NewTypeError("Iterator return result is not an object");

        return result;
    }
}
