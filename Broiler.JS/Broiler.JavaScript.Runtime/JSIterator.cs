using System;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public struct JSIterator(JSValue iterator, bool awaitResult = false) : IElementEnumerator, IReturnableEnumerator
{
    private uint index = 0;
    private readonly JSValue nextMethod = iterator[KeyStrings.next];

    private readonly JSValue AwaitIfNeeded(JSValue result)
    {
        if (awaitResult && result is IJSPromise promise)
            return promise.Task.GetAwaiter().GetResult();

        return result;
    }

    private readonly JSValue ValidateIteratorResult(JSValue result, string methodName)
    {
        result = AwaitIfNeeded(result);
        if (!result.IsObject)
            throw JSValue.NewTypeError($"Iterator {methodName} result is not an object");

        return result;
    }

    private readonly JSValue GetIteratorResult()
        => ValidateIteratorResult(nextMethod.InvokeFunction(new Arguments(iterator)), "next");

    private readonly JSValue GetIteratorResult(JSValue value)
        => ValidateIteratorResult(nextMethod.InvokeFunction(new Arguments(iterator, value ?? JSUndefined.Value)), "next");

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

    public readonly bool MoveNext(JSValue nextValue, out JSValue value)
    {
        value = GetIteratorResult(nextValue);
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

        return ValidateIteratorResult(method.InvokeFunction(new Arguments(iterator, value)), "return");
    }

    public readonly JSValue Return()
    {
        var method = iterator[KeyStrings.@return];
        if (method.IsUndefined || method.IsNull)
        {
            var iteratorResult = JSObject.NewWithProperties();
            iteratorResult.FastAddValue(KeyStrings.value, JSUndefined.Value, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
            iteratorResult.FastAddValue(KeyStrings.done, JSValue.BooleanTrue, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
            return iteratorResult;
        }

        return ValidateIteratorResult(method.InvokeFunction(new Arguments(iterator)), "return");
    }

    public readonly bool TryThrow(JSValue value, out JSValue iteratorResult)
    {
        var method = iterator[KeyStrings.@throw];
        if (method.IsUndefined || method.IsNull)
        {
            iteratorResult = default;
            return false;
        }

        iteratorResult = ValidateIteratorResult(method.InvokeFunction(new Arguments(iterator, value)), "throw");
        return true;
    }

    public readonly JSValue Throw(JSValue value)
    {
        if (!TryThrow(value, out var iteratorResult))
            throw JSValue.NewTypeError("Iterator does not provide a throw method");

        return iteratorResult;
    }
}
