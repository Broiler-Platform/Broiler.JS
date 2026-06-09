using System;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public struct JSIterator(JSValue iterator, bool awaitResult = false) : IElementEnumerator, IReturnableEnumerator
{
    private uint index = 0;

    // Mirrors the iterator record's [[done]] flag. It becomes true once the iterator
    // is exhausted (a result with done:true) or when a call to next() throws. Per the
    // spec, IteratorClose must NOT call return() once [[done]] is true, so Return()
    // is a no-op in that state (e.g. when destructuring's next() throws).
    private bool done = false;

    private readonly JSValue nextMethod = iterator[KeyStrings.next];

    private JSValue StepNext()
    {
        try
        {
            return GetIteratorResult();
        }
        catch
        {
            done = true;
            throw;
        }
    }

    private JSValue StepNext(JSValue value)
    {
        try
        {
            return GetIteratorResult(value);
        }
        catch
        {
            done = true;
            throw;
        }
    }

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
        value = StepNext();
        var resultDone = value[KeyStrings.done];

        if (resultDone.BooleanValue)
        {
            done = true;
            index = 0;
            value = JSUndefined.Value;
            hasValue = false;
            return false;
        }

        value = value[KeyStrings.value];
        index = this.index++;
        hasValue = true;
        return true;
    }

    public bool MoveNext(out JSValue value)
    {
        value = StepNext();
        var resultDone = value[KeyStrings.done];

        if (resultDone.BooleanValue)
        {
            done = true;
            value = JSUndefined.Value;
            return false;
        }

        value = value[KeyStrings.value];
        return true;
    }

    public bool MoveNext(JSValue nextValue, out JSValue value)
    {
        value = StepNext(nextValue);
        var resultDone = value[KeyStrings.done];

        if (resultDone.BooleanValue)
        {
            done = true;
            // When the iterator is exhausted, surface the result's `value` (the
            // iterator's "return value"). For `yield* inner`, this is the value
            // the delegating expression evaluates to once `inner` completes.
            value = value[KeyStrings.value];
            return false;
        }

        value = value[KeyStrings.value];
        return true;
    }

    // Like MoveNext, but yields the iterator's *raw* result object (the
    // { value, done } record) instead of unwrapping it. Used by `yield*` to
    // re-yield the delegated result without re-boxing. Returns false (and leaves
    // the raw result, whose `value` is the completion value) once exhausted.
    public bool MoveNextRaw(JSValue nextValue, out JSValue rawResult)
    {
        rawResult = StepNext(nextValue);
        if (rawResult[KeyStrings.done].BooleanValue)
        {
            done = true;
            return false;
        }

        return true;
    }

    public bool MoveNextOrDefault(out JSValue value, JSValue @default)
    {
        value = StepNext();
        var resultDone = value[KeyStrings.done];

        if (resultDone.BooleanValue)
        {
            done = true;
            value = @default;
            return false;
        }

        value = value[KeyStrings.value];
        return true;
    }

    public JSValue NextOrDefault(JSValue @default)
    {
        var value = StepNext();
        var resultDone = value[KeyStrings.done];

        if (resultDone.BooleanValue)
        {
            done = true;
            return @default;
        }

        return value[KeyStrings.value];
    }

    public JSValue Return(JSValue value)
    {
        // [[done]] is already set: IteratorClose must not invoke return() again.
        if (done)
            return MakeDoneResult(value);

        done = true;

        var method = iterator[KeyStrings.@return];
        if (method.IsUndefined || method.IsNull)
            return MakeDoneResult(value);

        return ValidateIteratorResult(method.InvokeFunction(new Arguments(iterator, value)), "return");
    }

    public JSValue Return()
    {
        // [[done]] is already set: IteratorClose must not invoke return() again.
        if (done)
            return MakeDoneResult(JSUndefined.Value);

        done = true;

        var method = iterator[KeyStrings.@return];
        if (method.IsUndefined || method.IsNull)
            return MakeDoneResult(JSUndefined.Value);

        return ValidateIteratorResult(method.InvokeFunction(new Arguments(iterator)), "return");
    }

    private static JSValue MakeDoneResult(JSValue value)
    {
        var iteratorResult = JSObject.NewWithProperties();
        iteratorResult.FastAddValue(KeyStrings.value, value, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
        iteratorResult.FastAddValue(KeyStrings.done, JSValue.BooleanTrue, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
        return iteratorResult;
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
