using System;
using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

public delegate GeneratorState JSGeneratorDelegateV2(ClrGeneratorV2 generator, in Arguments a, int nextJump, JSValue nextValue, Exception ex);

public class GeneratorState(JSValue value, int nextJump, bool isValueDelegate)
{
    public readonly bool HasValue = value != null;
    public readonly JSValue Value = value;
    public readonly bool IsValueDelegate = isValueDelegate;
    public readonly int NextJump = nextJump;
}

public class TryBlock
{
    public int Catch;
    public int Finally;
    public int End;
    public bool CatchBegan;
    public bool FinallyBegan;
    public TryBlock Parent;
}

public class ClrGeneratorV2(JSValue generator, JSGeneratorDelegateV2 @delegate, Arguments arguments, bool asyncGenerator = false)
{
    public CallStackItem StackItem;

    private Exception lastError = null;
    private Exception injectedException = null;

    internal void InjectException(Exception ex) => injectedException = ex;

    public Box[] Variables;
    private IElementEnumerator delegatedEnumerator;
    private JSValue delegatedCompletionValue;

    public JSValue LastValue;

    public IJSExecutionContext Context = JSEngine.Current as IJSExecutionContext;

    internal bool IsAsyncGenerator => asyncGenerator;
    internal IElementEnumerator DelegatedEnumerator => delegatedEnumerator;

    public bool IsFinished;
    public int NextJump;
    internal bool HasDelegatedEnumerator => delegatedEnumerator != null;

    // this is null...
    public TryBlock Root;

    private IElementEnumerator GetDelegatedEnumerator(JSValue value)
        => asyncGenerator
            ? value.GetAsyncIterableEnumerator()
            : value.GetIterableEnumerator();

    public void InitVariables(int i) => Variables ??= new Box[i];

    public Box<T> GetVariable<T>(int i)
    {
        var b = Variables[i];
        if (b == null)
        {
            b = new Box<T>();
            Variables[i] = b;
        }

        return b as Box<T>;
    }

    internal void Next(JSValue next, out JSValue value, out bool done)
    {
        GeneratorState v = null;
        while (true)
        {
            if (delegatedEnumerator != null)
            {
                try
                {
                    if (delegatedEnumerator.MoveNext(out value))
                    {
                        done = false;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    delegatedEnumerator = null;
                    delegatedCompletionValue = null;
                    InjectException(ex);
                    continue;
                }

                delegatedEnumerator = null;
                LastValue = delegatedCompletionValue ?? JSUndefined.Value;
                delegatedCompletionValue = null;
            }

            LastValue = next ?? LastValue ?? JSUndefined.Value;
            v = GetNext(NextJump, LastValue);
            NextJump = v.NextJump;

        ProcessState:
            if (v.HasValue)
            {
                if (v.IsValueDelegate)
                {
                    try
                    {
                        delegatedEnumerator = GetDelegatedEnumerator(v.Value);
                    }
                    catch (Exception ex)
                    {
                        InjectException(ex);
                        continue;
                    }

                    continue;
                }

                value = v.Value;

                if (v.NextJump == 0 || v.NextJump == -1)
                {
                    // need to execute finally.. if it is there...
                    if (Root != null && Root.Finally > 0)
                    {
                        v = GetNext(Root.Finally, value);
                        NextJump = v.NextJump;
                        if (v.IsValueDelegate)
                            goto ProcessState;
                    }

                    done = true;
                    return;
                }

                done = false;
                return;
            }

            done = true;
            value = default;
            return;
        }
    }

    internal bool TryThrowDelegated(JSValue value, out JSValue iteratorResult)
    {
        if (delegatedEnumerator is JSIterator iterator)
            return iterator.TryThrow(value, out iteratorResult);

        iteratorResult = default;
        return false;
    }

    internal bool TryReturnDelegated(JSValue value, out JSValue iteratorResult)
    {
        if (delegatedEnumerator is JSIterator iterator)
        {
            iteratorResult = iterator.Return(value);
            return true;
        }

        iteratorResult = default;
        return false;
    }

    internal void EndDelegation(JSValue completionValue = null)
    {
        delegatedEnumerator = null;
        delegatedCompletionValue = completionValue;
    }

    private GeneratorState GetNext(int nextJump, JSValue lastValue, Exception nextExp = null)
    {
        try
        {
            var ie = injectedException;
            if (ie != null)
            {
                injectedException = null;
                throw ie;
            }

            var r = @delegate(this, in arguments, nextJump, lastValue, nextExp);

            // this is case of try end and catch end...
            if (!r.HasValue && r.NextJump > 0)
            {
                var s = GetNext(r.NextJump, lastValue);
                return s;
            }

            return r;
        }
        catch (Exception ex)
        {
            var root = Root;
            if (root != null)
            {
                if (root.CatchBegan || root.FinallyBegan)
                    throw;

                // this.Root = root.Parent;
                if (root.Catch > 0)
                    return GetNext(root.Catch, lastValue, ex);

                if (root.Finally > 0)
                {
                    lastError = ex;
                    var v = GetNext(root.Finally, lastValue);

                    if (v.HasValue)
                        return v;
                }

                throw;
            }

            throw;
        }
    }

    public void PushTry(int @catch, int @finally, int end)
    {
        if (@catch == 0 && @finally == 0)
            throw new ArgumentException("Both catch and finally cannot be empty");

        Root = new TryBlock
        {
            Catch = @catch,
            Finally = @finally,
            End = end,
            Parent = Root
        };
    }


    public void Pop()
    {
        if (Root == null)
            throw new InvalidOperationException();

        Root = Root.Parent;
    }

    public void Throw(int end)
    {
        if ((Root?.End) != end || lastError == null)
            return;

        Pop();
        throw lastError;
    }

    public void BeginFinally()
    {
        Root.CatchBegan = false;
        Root.FinallyBegan = true;
    }

    public void BeginCatch() => Root.CatchBegan = true;
}
