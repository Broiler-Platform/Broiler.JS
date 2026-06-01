using System;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using System.Collections.Generic;

namespace Broiler.JavaScript.BuiltIns.Iterator;


/// <summary>
/// ES2025 Iterator built-in (§2.1).
/// Provides <c>Iterator.from()</c>, <c>Iterator.concat()</c> and lazy
/// prototype helpers (map, filter, take, drop, flatMap) as well as eager
/// terminal methods (reduce, toArray, forEach, some, every, find).
///
/// Prototype helper methods are registered manually (see
/// <see cref="DefaultBuiltInRegistry"/>) so that they work on any object
/// conforming to the iterator protocol, not only JSIteratorObject.
/// </summary>
[JSClassGenerator("Iterator")]
public partial class JSIteratorObject : JSObject
{
    internal readonly IElementEnumerator _enumerator;
    private bool _done;
    private bool _executing;

    // ---------------------------------------------------------------
    // Constructors
    // ---------------------------------------------------------------

    public JSIteratorObject(in Arguments a) : this(JSEngine.NewTargetPrototype) => throw JSEngine.NewTypeError("Iterator is not intended to be called as a constructor");

    internal JSIteratorObject(IElementEnumerator enumerator) : this() => _enumerator = enumerator;

    // ---------------------------------------------------------------
    // Iterator protocol – next / return
    // ---------------------------------------------------------------
    [JSExport("next")]
    internal JSValue Next(in Arguments a)
    {
        ThrowIfExecuting();

        try
        {
            _executing = true;
            if (!_done && _enumerator != null && _enumerator.MoveNext(out var value))
                return IteratorResult(value, false);
        }
        finally
        {
            _executing = false;
        }

        return IteratorResult(JSUndefined.Value, true);
    }

    [JSExport("return")]
    internal JSValue Return(in Arguments a)
    {
        var value = a.Length > 0 ? a.Get1() : JSUndefined.Value;
        ThrowIfExecuting();
        if (_done)
            return IteratorResult(value, true);

        try
        {
            _executing = true;
            _done = true;
            if (_enumerator is IReturnableEnumerator returnable)
                return returnable.Return();
        }
        finally
        {
            _executing = false;
        }

        return IteratorResult(value, true);
    }

    private void ThrowIfExecuting()
    {
        if (_executing)
            throw JSEngine.NewTypeError("Iterator is already executing");
    }

    // ---------------------------------------------------------------
    // Symbol.iterator – returns itself
    // ---------------------------------------------------------------
    public override IElementEnumerator GetElementEnumerator()
    {
        if (_enumerator != null)
            return _enumerator;

        return new JSIterator(this);
    }

    // ---------------------------------------------------------------
    // Static: Iterator.from  (§2.1.2)
    // ---------------------------------------------------------------
    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var obj = a.Get1();

        if (obj.IsNullOrUndefined)
            throw JSEngine.NewTypeError("Iterator.from requires an iterable or iterator argument");

        if (obj is JSIteratorObject)
            return obj;

        if (obj.IsString)
            return new JSIteratorObject(obj.GetElementEnumerator());

        if (obj is not JSObject @object)
            throw JSEngine.NewTypeError("Iterator.from requires an iterable or iterator argument");

        var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
        if (iteratorMethod.IsNull || iteratorMethod.IsUndefined)
            return new JSIteratorObject(GetDirectEnumerator(@object));

        if (!iteratorMethod.IsFunction)
            throw JSEngine.NewTypeError("Iterator.from requires a callable @@iterator");

        var iterator = iteratorMethod.InvokeFunction(new Arguments(@object));
        if (!iterator.IsObject)
            throw JSEngine.NewTypeError("Iterator.from requires an object iterator result");

        return new JSIteratorObject(new JSIterator(iterator));
    }

    // ---------------------------------------------------------------
    // Static: Iterator.concat  (§4.8)
    // ---------------------------------------------------------------
    [JSExport("concat")]
    internal static JSValue Concat(in Arguments a)
    {
        var iterables = new ConcatSource[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            var item = a.GetAt(i);
            if (item is null || item.IsNullOrUndefined || item is not JSObject @object)
                throw JSEngine.NewTypeError("Iterator.concat requires iterable arguments");

            var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
            if (!iteratorMethod.IsNull && !iteratorMethod.IsUndefined && !iteratorMethod.IsFunction)
                throw JSEngine.NewTypeError("Iterator.concat requires a callable @@iterator");

            iterables[i] = new ConcatSource(@object, iteratorMethod);
        }

        return new JSIteratorObject(new ConcatEnumerator(iterables));
    }

    [JSExport("zip", Length = 1)]
    internal static JSValue Zip(in Arguments a)
    {
        var (iterables, optionsValue) = a.Get2();
        if (iterables is not JSObject iterablesObject)
            throw JSEngine.NewTypeError("Iterator.zip requires an object iterable");

        var options = GetOptionsObject(optionsValue);
        var mode = ReadJointIterationMode(options);
        JSValue padding = JSUndefined.Value;

        if (mode == "longest")
        {
            padding = options[KeyStrings.GetOrCreate("padding")];
            if (!padding.IsUndefined && padding is not JSObject)
                throw JSEngine.NewTypeError("Iterator.zip requires an object padding option");
        }

        return new JSIteratorObject(new ZipEnumerator(iterablesObject, mode, padding));
    }

    [JSExport("zipKeyed", Length = 1)]
    internal static JSValue ZipKeyed(in Arguments a)
    {
        var (iterables, optionsValue) = a.Get2();
        if (iterables is not JSObject iterablesObject)
            throw JSEngine.NewTypeError("Iterator.zipKeyed requires an object iterable");

        var options = GetOptionsObject(optionsValue);
        var mode = ReadJointIterationMode(options);
        JSValue padding = JSUndefined.Value;

        if (mode == "longest")
        {
            padding = options[KeyStrings.GetOrCreate("padding")];
            if (!padding.IsUndefined && padding is not JSObject)
                throw JSEngine.NewTypeError("Iterator.zipKeyed requires an object padding option");
        }

        return new JSIteratorObject(new ZipKeyedEnumerator(iterablesObject, mode, padding));
    }

    private static JSObject GetOptionsObject(JSValue options)
    {
        if (options.IsUndefined)
            return JSObject.NewWithProperties();

        if (options is JSObject optionObject)
            return optionObject;

        throw JSEngine.NewTypeError("Iterator options must be an object");
    }

    private static string ReadJointIterationMode(JSObject options)
    {
        var mode = options[KeyStrings.GetOrCreate("mode")];
        if (mode.IsUndefined)
            return "shortest";

        if (!mode.IsString)
            throw JSEngine.NewTypeError("Iterator mode must be a valid string");

        return mode.ToString() switch
        {
            "shortest" => "shortest",
            "longest" => "longest",
            "strict" => "strict",
            _ => throw JSEngine.NewTypeError("Iterator mode must be a valid string")
        };
    }

    private static IElementEnumerator GetIterator(JSValue value)
    {
        if (value is not JSObject @object)
            throw JSEngine.NewTypeError("Iterator.zip requires an object iterable");

        var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
        if (!iteratorMethod.IsFunction)
            throw JSEngine.NewTypeError("Iterator.zip requires a callable @@iterator");

        var iterator = iteratorMethod.InvokeFunction(new Arguments(@object));
        if (!iterator.IsObject)
            throw JSEngine.NewTypeError("Iterator.zip requires an object iterator result");

        return new JSIterator(iterator);
    }

    private static IElementEnumerator GetIteratorFlattenable(JSValue value)
    {
        if (value is not JSObject @object)
            throw JSEngine.NewTypeError("Iterator.zip requires object iterables");

        var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
        if (iteratorMethod.IsUndefined)
            return new JSIterator(@object);

        if (!iteratorMethod.IsFunction)
            throw JSEngine.NewTypeError("Iterator.zip requires a callable @@iterator");

        var iterator = iteratorMethod.InvokeFunction(new Arguments(@object));
        if (!iterator.IsObject)
            throw JSEngine.NewTypeError("Iterator.zip requires an object iterator result");

        return new JSIterator(iterator);
    }

    private static void CloseIteratorReverse(IReadOnlyList<IElementEnumerator?> iterators)
    {
        for (int i = iterators.Count - 1; i >= 0; i--)
            if (iterators[i] != null)
                CloseIteratorIfPossible(iterators[i]);
    }

    private static void CloseIteratorForReturn(IElementEnumerator? enumerator, ref Exception? firstException)
    {
        if (enumerator is not IReturnableEnumerator returnable)
            return;

        try
        {
            returnable.Return();
        }
        catch (Exception ex)
        {
            firstException ??= ex;
        }
    }

    private static JSValue CreateZipArrayResult(JSValue[] values)
    {
        var result = new JSArray();
        for (int i = 0; i < values.Length; i++)
            result.AddArrayItem(values[i]);
        return result;
    }

    internal static JSValue StaticNext(in Arguments a)
    {
        return a.This switch
        {
            JSIteratorObject iterator => iterator.Next(in a),
            JSGenerator generator => generator.Next(in a),
            _ => throw JSEngine.NewTypeError("Iterator.prototype.next called on incompatible receiver")
        };
    }

    internal static JSValue StaticReturn(in Arguments a)
    {
        return a.This switch
        {
            JSIteratorObject iterator => iterator.Return(in a),
            JSGenerator generator => generator.Return(in a),
            JSObject => IteratorResult(a.Length > 0 ? a.Get1() : JSUndefined.Value, true),
            _ => throw JSEngine.NewTypeError("Iterator.prototype.return called on incompatible receiver")
        };
    }

    // ---------------------------------------------------------------
    // Static helpers — these are the implementations of the prototype
    // methods, registered on Iterator.prototype by
    // DefaultBuiltInRegistry so they accept any iterator as `this`.
    // ---------------------------------------------------------------
    internal static IElementEnumerator EnumeratorFrom(JSValue value)
    {
        if (value is JSIteratorObject ito && ito._enumerator != null)
            return ito._enumerator;

        if (value is not JSObject @object)
            throw JSEngine.NewTypeError("Iterator helper requires an object receiver");

        return GetDirectEnumerator(@object);
    }

    private static IElementEnumerator GetDirectEnumerator(JSObject @object)
    {
        return new JSIterator(@object);
    }

    internal static void CloseIteratorIfPossible(IElementEnumerator enumerator)
    {
        if (enumerator is not IReturnableEnumerator returnable)
            return;

        try
        {
            returnable.Return();
        }
        catch
        {
        }
    }

    internal static JSValue StaticMap(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.map requires a callable argument");
        var en = EnumeratorFrom(a.This);
        return new JSIteratorObject(new MapEnumerator(en, fn));
    }

    internal static JSValue StaticFilter(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.filter requires a callable argument");
        var en = EnumeratorFrom(a.This);
        return new JSIteratorObject(new FilterEnumerator(en, fn));
    }

    internal static JSValue StaticTake(in Arguments a)
    {
        var n = ReadIteratorLimitOrClose(a.This, a.Get1(), "Iterator.prototype.take");
        var en = EnumeratorFrom(a.This);
        return new JSIteratorObject(new TakeEnumerator(en, n));
    }

    internal static JSValue StaticDrop(in Arguments a)
    {
        var n = ReadIteratorLimitOrClose(a.This, a.Get1(), "Iterator.prototype.drop");
        var en = EnumeratorFrom(a.This);
        return new JSIteratorObject(new DropEnumerator(en, n));
    }

    private static JSValue ReadCallableOrClose(JSValue iterator, JSValue callback, string message)
    {
        if (callback.IsFunction)
            return callback;

        CloseIteratorValueIfPossible(iterator);
        throw JSEngine.NewTypeError(message);
    }

    private static int ReadIteratorLimitOrClose(JSValue iterator, JSValue limitValue, string methodName)
    {
        try
        {
            var n = limitValue.DoubleValue;

            if (double.IsNaN(n) || n < 0)
                throw JSEngine.NewRangeError($"{methodName} requires a non-negative number");

            return (int)n;
        }
        catch
        {
            CloseIteratorValueIfPossible(iterator);
            throw;
        }
    }

    private static void CloseIteratorValueIfPossible(JSValue iterator)
    {
        try
        {
            switch (iterator)
            {
                case JSIteratorObject iteratorObject:
                    iteratorObject.Return(Arguments.Empty);
                    return;
                case JSGenerator generator:
                    generator.Return(Arguments.Empty);
                    return;
                case JSObject @object:
                    var returnMethod = @object[KeyStrings.@return];
                    if (!returnMethod.IsNullOrUndefined)
                        returnMethod.InvokeFunction(new Arguments(@object));
                    return;
            }
        }
        catch
        {
        }
    }

    internal static JSValue StaticFlatMap(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.flatMap requires a callable argument");
        var en = EnumeratorFrom(a.This);
        return new JSIteratorObject(new FlatMapEnumerator(en, fn));
    }

    private static IElementEnumerator GetFlattenableEnumerator(JSValue value)
    {
        if (value.IsString || value is not JSObject @object)
            throw JSEngine.NewTypeError("Iterator.prototype.flatMap mapper must return an object");

        var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
        if (iteratorMethod.IsNull || iteratorMethod.IsUndefined)
            return GetDirectEnumerator(@object);

        if (!iteratorMethod.IsFunction)
            throw JSEngine.NewTypeError("Iterator helper requires a callable @@iterator");

        var iterator = iteratorMethod.InvokeFunction(new Arguments(@object));
        if (iterator is not JSObject iteratorObject)
            throw JSEngine.NewTypeError("Iterator helper requires an object iterator result");

        return new JSIterator(iteratorObject);
    }

    internal static JSValue StaticReduce(in Arguments a)
    {
        var (callback, initialValue) = a.Get2();
        var fn = ReadCallableOrClose(a.This, callback, "Iterator.prototype.reduce requires a callable argument");
        var en = EnumeratorFrom(a.This);

        JSValue accumulator;
        uint count = 0;

        if (a.Length >= 2)
        {
            accumulator = initialValue;
        }
        else
        {
            if (!en.MoveNext(out var first))
                throw JSEngine.NewTypeError("Reduce of empty iterator with no initial value");

            accumulator = first;
            count = 1;
        }

        while (en.MoveNext(out var value))
        {
            try
            {
                accumulator = fn.InvokeFunction(new Arguments(JSUndefined.Value, accumulator, value, JSValue.CreateNumber(count++)));
            }
            catch
            {
                CloseIteratorIfPossible(en);
                throw;
            }
        }

        return accumulator;
    }

    internal static JSValue StaticToArray(in Arguments a)
    {
        var result = new JSArray();
        var en = EnumeratorFrom(a.This);

        while (en.MoveNext(out var value))
            result.Add(value);

        return result;
    }

    internal static JSValue StaticForEach(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.forEach requires a callable argument");
        var en = EnumeratorFrom(a.This);

        uint count = 0;
        while (en.MoveNext(out var value))
        {
            try
            {
                fn.InvokeFunction(new Arguments(JSUndefined.Value, value, JSValue.CreateNumber(count++)));
            }
            catch
            {
                CloseIteratorIfPossible(en);
                throw;
            }
        }

        return JSUndefined.Value;
    }

    internal static JSValue StaticSome(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.some requires a callable argument");
        var en = EnumeratorFrom(a.This);

        uint count = 0;
        while (en.MoveNext(out var value))
        {
            var result = false;
            try
            {
                result = fn.InvokeFunction(new Arguments(JSUndefined.Value, value, JSValue.CreateNumber(count++))).BooleanValue;
            }
            catch
            {
                CloseIteratorIfPossible(en);
                throw;
            }

            if (result)
            {
                CloseIteratorIfPossible(en);
                return JSBoolean.True;
            }
        }

        return JSBoolean.False;
    }

    internal static JSValue StaticEvery(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.every requires a callable argument");
        var en = EnumeratorFrom(a.This);

        uint count = 0;

        while (en.MoveNext(out var value))
        {
            var result = false;
            try
            {
                result = fn.InvokeFunction(new Arguments(JSUndefined.Value, value, JSValue.CreateNumber(count++))).BooleanValue;
            }
            catch
            {
                CloseIteratorIfPossible(en);
                throw;
            }

            if (!result)
            {
                CloseIteratorIfPossible(en);
                return JSBoolean.False;
            }
        }

        return JSBoolean.True;
    }

    internal static JSValue StaticFind(in Arguments a)
    {
        var fn = ReadCallableOrClose(a.This, a.Get1(), "Iterator.prototype.find requires a callable argument");
        var en = EnumeratorFrom(a.This);

        uint count = 0;
        while (en.MoveNext(out var value))
        {
            var result = false;
            try
            {
                result = fn.InvokeFunction(new Arguments(JSUndefined.Value, value, JSValue.CreateNumber(count++))).BooleanValue;
            }
            catch
            {
                CloseIteratorIfPossible(en);
                throw;
            }

            if (result)
            {
                CloseIteratorIfPossible(en);
                return value;
            }
        }

        return JSUndefined.Value;
    }

    private sealed class ZipEnumerator : IElementEnumerator, IReturnableEnumerator
    {
        private readonly List<IElementEnumerator?> _iterators = [];
        private readonly List<JSValue> _paddingValues = [];
        private readonly string _mode;
        private bool _done;
        private uint _index;

        public ZipEnumerator(JSObject iterables, string mode, JSValue padding)
        {
            _mode = mode;

            var inputIter = GetIterator(iterables);
            while (true)
            {
                JSValue next;
                try
                {
                    if (!inputIter.MoveNext(out next))
                        break;
                }
                catch
                {
                    CloseIteratorReverse(_iterators);
                    throw;
                }

                try
                {
                    _iterators.Add(GetIteratorFlattenable(next));
                }
                catch
                {
                    CloseIteratorReverse(_iterators);
                    CloseIteratorIfPossible(inputIter);
                    throw;
                }
            }

            if (mode == "longest" && !padding.IsUndefined)
            {
                IElementEnumerator paddingIter;
                try
                {
                    paddingIter = GetIterator(padding);
                }
                catch
                {
                    CloseIteratorReverse(_iterators);
                    throw;
                }

                try
                {
                    for (int i = 0; i < _iterators.Count; i++)
                    {
                        if (paddingIter.MoveNext(out var paddingValue))
                            _paddingValues.Add(paddingValue);
                        else
                            _paddingValues.Add(JSUndefined.Value);
                    }
                }
                catch
                {
                    CloseIteratorReverse(_iterators);
                    throw;
                }

                Exception? firstException = null;
                CloseIteratorForReturn(paddingIter, ref firstException);
                for (int i = _iterators.Count - 1; i >= 0; i--)
                    CloseIteratorIfPossible(_iterators[i]);

                if (firstException != null)
                    throw firstException;
            }
        }

        private JSValue GetPaddingValue(int index)
        {
            return index < _paddingValues.Count ? _paddingValues[index] : JSUndefined.Value;
        }

        private void CloseAllActive()
        {
            CloseIteratorReverse(_iterators);
        }

        public bool MoveNext(out JSValue value)
        {
            if (_done)
            {
                value = JSUndefined.Value;
                return false;
            }

            var row = new JSValue[_iterators.Count];

            for (int i = 0; i < _iterators.Count; i++)
            {
                var iter = _iterators[i];
                if (iter == null)
                {
                    if (_mode == "longest")
                        row[i] = GetPaddingValue(i);

                    continue;
                }

                try
                {
                    if (!iter.MoveNext(out var item))
                    {
                        _iterators[i] = null;
                        if (_mode == "longest")
                        {
                            row[i] = GetPaddingValue(i);
                            continue;
                        }

                        if (_mode == "strict")
                        {
                            if (i != 0)
                            {
                                _done = true;
                                CloseAllActive();
                                throw JSEngine.NewTypeError("Iterator.zip requires all iterators to finish together");
                            }

                            for (int j = i + 1; j < _iterators.Count; j++)
                            {
                                var nextIter = _iterators[j];
                                if (nextIter == null)
                                    continue;

                                try
                                {
                                    if (nextIter.MoveNext(out _))
                                    {
                                        _done = true;
                                        CloseAllActive();
                                        throw JSEngine.NewTypeError("Iterator.zip requires all iterators to finish together");
                                    }

                                    _iterators[j] = null;
                                }
                                catch
                                {
                                    _iterators[j] = null;
                                    _done = true;
                                    CloseAllActive();
                                    throw;
                                }
                            }

                            _done = true;
                            CloseAllActive();
                            value = JSUndefined.Value;
                            return false;
                        }

                        _done = true;
                        CloseAllActive();
                        value = JSUndefined.Value;
                        return false;
                    }

                    row[i] = item;
                }
                catch
                {
                    _iterators[i] = null;
                    _done = true;
                    CloseAllActive();
                    throw;
                }
            }

            if (_mode == "longest")
            {
                var anyActive = false;
                for (int i = 0; i < _iterators.Count; i++)
                {
                    if (_iterators[i] != null)
                    {
                        anyActive = true;
                        break;
                    }
                }

                if (!anyActive)
                {
                    _done = true;
                    CloseAllActive();
                    value = JSUndefined.Value;
                    return false;
                }
            }

            value = CreateZipArrayResult(row);
            _index++;
            return true;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (MoveNext(out value))
            {
                hasValue = true;
                index = _index - 1;
                return true;
            }

            hasValue = false;
            index = 0;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            return MoveNext(out var value) ? value : @default;
        }

        public JSValue Return()
        {
            if (_done)
                return IteratorResult(JSUndefined.Value, true);

            _done = true;

            Exception? firstException = null;
            for (int i = _iterators.Count - 1; i >= 0; i--)
                CloseIteratorForReturn(_iterators[i], ref firstException);

            if (firstException != null)
                throw firstException;

            return IteratorResult(JSUndefined.Value, true);
        }

        public JSValue Return(JSValue value) => Return();
    }

    private sealed class ZipKeyedEnumerator : IElementEnumerator, IReturnableEnumerator
    {
        private readonly List<JSValue> _keys = [];
        private readonly List<IElementEnumerator?> _iterators = [];
        private readonly List<JSValue> _paddingValues = [];
        private readonly string _mode;
        private bool _done;
        private uint _index;

        public ZipKeyedEnumerator(JSObject iterables, string mode, JSValue padding)
        {
            _mode = mode;

            var allKeys = iterables.GetAllKeys(false, false);
            try
            {
                while (allKeys.MoveNext(out var key))
                {
                    var desc = iterables.GetOwnPropertyDescriptor(key);
                    if (desc.IsUndefined)
                        continue;

                    if (!desc[KeyStrings.enumerable].BooleanValue)
                        continue;

                    var value = iterables[key];
                    if (value.IsUndefined)
                        continue;

                    _keys.Add(key);
                    _iterators.Add(GetIteratorFlattenable(value));
                }
            }
            catch
            {
                CloseIteratorReverse(_iterators);
                throw;
            }

            if (mode == "longest" && !padding.IsUndefined)
            {
                var paddingObject = (JSObject)padding;
                try
                {
                    for (int i = 0; i < _keys.Count; i++)
                        _paddingValues.Add(paddingObject[_keys[i]]);
                }
                catch
                {
                    CloseIteratorReverse(_iterators);
                    throw;
                }
            }
        }

        private JSValue GetPaddingValue(int index)
        {
            return index < _paddingValues.Count ? _paddingValues[index] : JSUndefined.Value;
        }

        private void CloseAllActive()
        {
            CloseIteratorReverse(_iterators);
        }

        public bool MoveNext(out JSValue value)
        {
            if (_done)
            {
                value = JSUndefined.Value;
                return false;
            }

            var result = JSObject.NewWithProperties();

            for (int i = 0; i < _iterators.Count; i++)
            {
                var iter = _iterators[i];
                if (iter == null)
                {
                    if (_mode == "longest")
                        result.FastAddValue(_keys[i], GetPaddingValue(i), Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);

                    continue;
                }

                try
                {
                    if (!iter.MoveNext(out var item))
                    {
                        _iterators[i] = null;
                        if (_mode == "longest")
                        {
                            result.FastAddValue(_keys[i], GetPaddingValue(i), Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
                            continue;
                        }

                        if (_mode == "strict")
                        {
                            if (i != 0)
                            {
                                _done = true;
                                CloseAllActive();
                                throw JSEngine.NewTypeError("Iterator.zipKeyed requires all iterators to finish together");
                            }

                            for (int j = i + 1; j < _iterators.Count; j++)
                            {
                                var nextIter = _iterators[j];
                                if (nextIter == null)
                                    continue;

                                try
                                {
                                    if (nextIter.MoveNext(out _))
                                    {
                                        _done = true;
                                        CloseAllActive();
                                        throw JSEngine.NewTypeError("Iterator.zipKeyed requires all iterators to finish together");
                                    }

                                    _iterators[j] = null;
                                }
                                catch
                                {
                                    _iterators[j] = null;
                                    _done = true;
                                    CloseAllActive();
                                    throw;
                                }
                            }

                            _done = true;
                            CloseAllActive();
                            value = JSUndefined.Value;
                            return false;
                        }

                        _done = true;
                        CloseAllActive();
                        value = JSUndefined.Value;
                        return false;
                    }

                    result.FastAddValue(_keys[i], item, Broiler.JavaScript.Storage.JSPropertyAttributes.EnumerableConfigurableValue);
                }
                catch
                {
                    _iterators[i] = null;
                    _done = true;
                    CloseAllActive();
                    throw;
                }
            }

            if (_mode == "longest")
            {
                var anyActive = false;
                for (int i = 0; i < _iterators.Count; i++)
                {
                    if (_iterators[i] != null)
                    {
                        anyActive = true;
                        break;
                    }
                }

                if (!anyActive)
                {
                    _done = true;
                    CloseAllActive();
                    value = JSUndefined.Value;
                    return false;
                }
            }

            value = result;
            _index++;
            return true;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (MoveNext(out value))
            {
                hasValue = true;
                index = _index - 1;
                return true;
            }

            hasValue = false;
            index = 0;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (MoveNext(out value))
                return true;

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            return MoveNext(out var value) ? value : @default;
        }

        public JSValue Return()
        {
            if (_done)
                return IteratorResult(JSUndefined.Value, true);

            _done = true;

            Exception? firstException = null;
            for (int i = _iterators.Count - 1; i >= 0; i--)
                CloseIteratorForReturn(_iterators[i], ref firstException);

            if (firstException != null)
                throw firstException;

            return IteratorResult(JSUndefined.Value, true);
        }

        public JSValue Return(JSValue value) => Return();
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    internal static JSValue IteratorResult(JSValue value, bool done)
    {
        return NewWithProperties().AddProperty(KeyStrings.value, value).AddProperty(KeyStrings.done, done ? JSBoolean.True : JSBoolean.False);
    }

    // ===============================================================
    // Private enumerator wrappers for lazy methods
    // ===============================================================
    internal sealed class MapEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator, IReturnableEnumerator
    {
        private uint _count;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (source.MoveNext(out hasValue, out var item, out index))
            {
                value = fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++)));
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (source.MoveNext(out var item))
            {
                value = fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++)));
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (source.MoveNext(out var item))
            {
                value = fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++)));
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (source.MoveNext(out var item))
                return fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++)));

            return @default;
        }

        public JSValue Return()
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(JSUndefined.Value, true);

        public JSValue Return(JSValue value)
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(value, true);
    }

    internal sealed class FilterEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator, IReturnableEnumerator
    {
        private uint index = 0;
        private uint predicateCount = 0;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            while (source.MoveNext(out var item))
            {
                if (fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(predicateCount++))).BooleanValue)
                {
                    value = item;
                    hasValue = true;
                    index = this.index++;
                    return true;
                }
            }

            value = JSUndefined.Value;
            hasValue = false;
            index = 0;

            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            while (source.MoveNext(out var item))
            {
                if (fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(predicateCount++))).BooleanValue)
                {
                    value = item;
                    return true;
                }
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            while (source.MoveNext(out var item))
            {
                if (fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(predicateCount++))).BooleanValue)
                {
                    value = item;
                    return true;
                }
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            while (source.MoveNext(out var item))
            {
                if (fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(predicateCount++))).BooleanValue)
                    return item;
            }

            return @default;
        }

        public JSValue Return()
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(JSUndefined.Value, true);

        public JSValue Return(JSValue value)
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(value, true);
    }

    internal sealed class TakeEnumerator(IElementEnumerator source, int limit) : IElementEnumerator, IReturnableEnumerator
    {
        private int taken = 0;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (taken < limit && source.MoveNext(out hasValue, out value, out index))
            {
                taken++;
                return true;
            }

            value = JSUndefined.Value; hasValue = false; index = 0;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (taken < limit && source.MoveNext(out value))
            {
                taken++;
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (taken < limit && source.MoveNext(out value))
            {
                taken++;
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (taken < limit && source.MoveNext(out var v))
            {
                taken++;
                return v;
            }

            return @default;
        }

        public JSValue Return()
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(JSUndefined.Value, true);

        public JSValue Return(JSValue value)
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(value, true);
    }

    internal sealed class DropEnumerator(IElementEnumerator source, int count) : IElementEnumerator, IReturnableEnumerator
    {
        private bool _dropped;

        private void EnsureDropped()
        {
            if (_dropped)
                return;

            _dropped = true;

            for (int i = 0; i < count; i++)
                if (!source.MoveNext(out _)) break;
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        { EnsureDropped(); return source.MoveNext(out hasValue, out value, out index); }

        public bool MoveNext(out JSValue value)
        { EnsureDropped(); return source.MoveNext(out value); }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        { EnsureDropped(); return source.MoveNextOrDefault(out value, @default); }

        public JSValue NextOrDefault(JSValue @default)
        { EnsureDropped(); return source.NextOrDefault(@default); }

        public JSValue Return()
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(JSUndefined.Value, true);

        public JSValue Return(JSValue value)
            => source is IReturnableEnumerator returnable
                ? returnable.Return()
                : IteratorResult(value, true);
    }

    internal sealed class FlatMapEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator, IReturnableEnumerator
    {
        private IElementEnumerator _inner;
        private uint _count;

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            while (true)
            {
                if (_inner != null && _inner.MoveNext(out hasValue, out value, out index))
                    return true;

                if (!source.MoveNext(out var item))
                { value = JSUndefined.Value; hasValue = false; index = 0; return false; }

                _inner = GetFlattenableEnumerator(fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++))));
            }
        }

        public bool MoveNext(out JSValue value)
        {
            while (true)
            {
                if (_inner != null && _inner.MoveNext(out value)) return true;
                if (!source.MoveNext(out var item))
                { value = JSUndefined.Value; return false; }

                _inner = GetFlattenableEnumerator(fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++))));
            }
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            while (true)
            {
                if (_inner != null && _inner.MoveNext(out value)) return true;
                if (!source.MoveNext(out var item))
                { value = @default; return false; }
                _inner = GetFlattenableEnumerator(fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++))));
            }
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            while (true)
            {
                if (_inner != null && _inner.MoveNext(out var v)) return v;
                if (!source.MoveNext(out var item)) return @default;

                _inner = GetFlattenableEnumerator(fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++))));
            }
        }

        public JSValue Return()
        {
            if (_inner is IReturnableEnumerator innerReturnable)
                return innerReturnable.Return();

            if (source is IReturnableEnumerator sourceReturnable)
                return sourceReturnable.Return();

            return IteratorResult(JSUndefined.Value, true);
        }

        public JSValue Return(JSValue value)
        {
            if (_inner is IReturnableEnumerator innerReturnable)
                return innerReturnable.Return();

            if (source is IReturnableEnumerator sourceReturnable)
                return sourceReturnable.Return();

            return IteratorResult(value, true);
        }
    }

    private readonly record struct ConcatSource(JSObject Iterable, JSValue IteratorMethod);

    private sealed class ConcatEnumerator(ConcatSource[] iterables) : IElementEnumerator, IReturnableEnumerator
    {
        private int _current = 0;
        private bool _started;
        private IElementEnumerator _currentEnum;

        private static IElementEnumerator GetEnumerator(ConcatSource iterable)
        {
            if (iterable.IteratorMethod == null || iterable.IteratorMethod.IsNull || iterable.IteratorMethod.IsUndefined)
                return GetDirectEnumerator(iterable.Iterable);

            var iterator = iterable.IteratorMethod.InvokeFunction(new Arguments(iterable.Iterable));
            if (iterator is not JSObject iteratorObject)
                throw JSEngine.NewTypeError("Iterator.concat requires an object iterator result");

            return new JSIterator(iteratorObject);
        }

        private bool Advance()
        {
            _current++;
            if (_current < iterables.Length)
            { _currentEnum = GetEnumerator(iterables[_current]); return true; }

            _currentEnum = null;
            return false;
        }

        private void EnsureStarted()
        {
            if (_started)
                return;

            _started = true;
            if (iterables.Length > 0)
                _currentEnum = GetEnumerator(iterables[0]);
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            EnsureStarted();
            while (_currentEnum != null)
            {
                if (_currentEnum.MoveNext(out hasValue, out value, out index)) return true;
                if (!Advance()) break;
            }

            value = JSUndefined.Value; hasValue = false; index = 0;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            EnsureStarted();
            while (_currentEnum != null)
            {
                if (_currentEnum.MoveNext(out value)) return true;
                if (!Advance()) break;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            EnsureStarted();
            while (_currentEnum != null)
            {
                if (_currentEnum.MoveNext(out value)) return true;
                if (!Advance()) break;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            EnsureStarted();
            while (_currentEnum != null)
            {
                if (_currentEnum.MoveNext(out var v)) return v;
                if (!Advance()) break;
            }

            return @default;
        }

        public JSValue Return(JSValue value)
        {
            if (!_started)
                return IteratorResult(value, true);

            if (_currentEnum is IReturnableEnumerator returnable)
                return returnable.Return();

            return IteratorResult(value, true);
        }

        public JSValue Return()
        {
            if (!_started)
                return IteratorResult(JSUndefined.Value, true);

            if (_currentEnum is IReturnableEnumerator returnable)
                return returnable.Return();

            return IteratorResult(JSUndefined.Value, true);
        }
    }
}
