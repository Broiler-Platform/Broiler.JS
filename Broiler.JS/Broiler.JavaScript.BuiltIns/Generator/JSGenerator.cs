using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Generator;

[JSClassGenerator("Generator")]
public partial class JSGenerator : JSObject, IJSGenerator
{
    readonly IElementEnumerator en;
    private ClrGeneratorV2 cg;
    private readonly string name;

    internal JSValue value;
    internal bool done;
    private bool executing;

    public JSGenerator(in Arguments a) : base(JSEngine.NewTargetPrototype) => throw new NotImplementedException();

    public JSGenerator(IElementEnumerator en, string name) : this()
    {
        this.en = en;
        this.name = name;
    }

    public JSGenerator(ClrGeneratorV2 g) : this()
    {
        cg = g;
        value = JSUndefined.Value;
    }

    public override string ToString() => $"[object {name}]";

    [JSExport("toString")]
    public new JSValue ToString(in Arguments a) => JSValue.CreateString(ToString());


    public JSValue Return(JSValue value)
    {
        ThrowIfExecuting();
        if (cg != null && cg.HasDelegatedEnumerator)
        {
            var delegatedResult = cg.TryReturnDelegated(value, out var iteratorResult)
                ? iteratorResult
                : JSUndefined.Value;

            if (!delegatedResult.IsUndefined)
            {
                var delegatedDone = delegatedResult[KeyStrings.done].BooleanValue;
                var delegatedValue = delegatedResult[KeyStrings.value];
                if (!delegatedDone)
                {
                    done = false;
                    this.value = delegatedValue;
                    return ValueObject;
                }

                cg.EndDelegation(delegatedValue);
                done = true;
                this.value = JSUndefined.Value;
                return NewWithProperties().AddProperty(KeyStrings.value, delegatedValue).AddProperty(KeyStrings.done, JSValue.BooleanTrue);
            }

            cg.EndDelegation(value);
            done = true;
            this.value = JSUndefined.Value;
            return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, JSValue.BooleanTrue);
        }

        done = true;
        this.value = JSUndefined.Value;

        return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, done ? JSValue.BooleanTrue : JSValue.BooleanFalse);
    }

    public JSValue Throw(JSValue value)
    {
        ThrowIfExecuting();
        if (cg != null && cg.HasDelegatedEnumerator)
        {
            var missingThrow = false;
            try
            {
                if (!cg.TryThrowDelegated(value, out var delegatedResult))
                {
                    missingThrow = true;
                    if (cg.DelegatedEnumerator is IReturnableEnumerator returnable)
                        returnable.Return();

                    cg.EndDelegation();
                    throw JSEngine.NewTypeError("Iterator does not provide a throw method");
                }

                var delegatedDone = delegatedResult[KeyStrings.done].BooleanValue;
                var delegatedValue = delegatedResult[KeyStrings.value];
                if (!delegatedDone)
                {
                    done = false;
                    this.value = delegatedValue;
                    return ValueObject;
                }

                cg.EndDelegation(delegatedValue);
                return Next(delegatedValue);
            }
            catch (Exception ex)
            {
                if (missingThrow)
                {
                    done = true;
                    this.value = JSUndefined.Value;
                    throw;
                }

                cg.EndDelegation();
                cg.InjectException(JSException.From(ex));
                return Next();
            }
        }

        if (cg != null)
        {
            cg.InjectException(JSException.FromValue(value));
            return Next();
        }

        throw JSException.FromValue(value);
    }

    public JSValue ValueObject => NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, done ? JSValue.BooleanTrue : JSValue.BooleanFalse);

    public bool MoveNext(JSValue replaceOld, out JSValue item)
    {
        var c = JSEngine.Current as IJSExecutionContext;
        var top = c?.Top;
        ThrowIfExecuting();

        if (done)
        {
            item = JSUndefined.Value;
            value = item;
            return false;
        }

        try
        {
            executing = true;
            // c.Top = cg.StackItem;
            cg.Next(replaceOld, out item, out done);
            value = item;

            if (!done)
                return true;

            value = item;
            done = true;

            return false;
        }
        catch
        {
            done = true;
            value = JSUndefined.Value;
            throw;
        }
        finally
        {
            executing = false;
            if (c != null) c.Top = top;
        }
    }

    public JSValue Next(JSValue replaceOld = null)
    {
        ThrowIfExecuting();

        if (done)
        {
            value = JSUndefined.Value;
            return ValueObject;
        }

        if (en != null)
        {
            if (en.MoveNext(out JSValue item))
            {
                value = item;
                return ValueObject;
            }

            done = true;
            value = JSUndefined.Value;

            return ValueObject;
        }

        var c = JSEngine.Current as IJSExecutionContext;
        var top = c?.Top;
        
        try
        {
            executing = true;
            cg.Next(replaceOld, out value, out done);
            return ValueObject;
        }
        catch
        {
            done = true;
            value = JSUndefined.Value;
            throw;
        }
        finally
        {
            executing = false;
            if (c != null) c.Top = top;
        }
    }

    private void ThrowIfExecuting()
    {
        if (!executing)
            return;

        done = true;
        value = JSUndefined.Value;
        throw JSEngine.NewTypeError("Generator is already running");
    }

    public override IElementEnumerator GetElementEnumerator() => new ElementEnumerator(this);

    public override IElementEnumerator GetAsyncIterableEnumerator()
        => IsAsyncGenerator
            ? new ElementEnumerator(this)
            : base.GetAsyncIterableEnumerator();

    private bool IsAsyncGenerator => cg?.IsAsyncGenerator == true;

    private JSValue AsAsyncIteratorResult(Func<JSValue> operation)
    {
        try
        {
            return JSEngine.CreateResolvedOrRejectedPromise(operation(), true);
        }
        catch (Exception ex)
        {
            return JSEngine.CreateResolvedOrRejectedPromise(JSException.ErrorFrom(ex), false);
        }
    }

    private struct ElementEnumerator(JSGenerator generator) : IElementEnumerator
    {
        int index = -1;

        public bool MoveNext(out JSValue value)
        {
            generator.Next();
            if (!generator.done)
            {
                index++;
                value = generator.value;
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            generator.Next();

            if (!generator.done)
            {
                this.index++;
                index = (uint)this.index;
                hasValue = true;
                value = generator.value;
                return true;
            }

            index = 0;
            value = JSUndefined.Value;
            hasValue = false;

            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            generator.Next();

            if (!generator.done)
            {
                index++;
                value = generator.value;
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            generator.Next();

            if (!generator.done)
            {
                index++;
                return generator.value;
            }

            return @default;
        }
    }

    [JSExport("next", Length = 1)]
    public JSValue Next(in Arguments a)
    {
        var nextValue = a.Length == 0 ? null : a.Get1();
        return IsAsyncGenerator
            ? AsAsyncIteratorResult(() => Next(nextValue))
            : Next(nextValue);
    }

    [JSExport("return", Length = 1)]
    public JSValue Return(in Arguments a)
    {
        var returnValue = a.Get1();
        return IsAsyncGenerator
            ? AsAsyncIteratorResult(() => Return(returnValue))
            : Return(returnValue);
    }

    [JSExport("throw", Length = 1)]
    public JSValue Throw(in Arguments a)
    {
        var throwValue = a.Get1();
        return IsAsyncGenerator
            ? AsAsyncIteratorResult(() => Throw(throwValue))
            : Throw(throwValue);
    }
}
