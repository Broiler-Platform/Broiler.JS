using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

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
            if (_enumerator != null && _enumerator.MoveNext(out var value))
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

        try
        {
            _executing = true;
            if (_enumerator is IReturnableEnumerator returnable)
                return returnable.Return(value);
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

            if (item is not JSObject @object)
                throw JSEngine.NewTypeError("Iterator.concat requires iterable arguments");

            var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
            if (!iteratorMethod.IsNull && !iteratorMethod.IsUndefined && !iteratorMethod.IsFunction)
                throw JSEngine.NewTypeError("Iterator.concat requires a callable @@iterator");

            iterables[i] = new ConcatSource(@object, iteratorMethod);
        }

        return new JSIteratorObject(new ConcatEnumerator(iterables));
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
            returnable.Return(JSUndefined.Value);
        }
        catch
        {
        }
    }

    internal static JSValue StaticMap(in Arguments a)
    {
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();

        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.map requires a callable argument");
        }

        return new JSIteratorObject(new MapEnumerator(en, fn));
    }

    internal static JSValue StaticFilter(in Arguments a)
    {
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();

        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.filter requires a callable argument");
        }

        return new JSIteratorObject(new FilterEnumerator(en, fn));
    }

    internal static JSValue StaticTake(in Arguments a)
    {
        var en = EnumeratorFrom(a.This);

        try
        {
            var n = a.Get1().DoubleValue;

            if (double.IsNaN(n) || n < 0)
                throw JSEngine.NewRangeError("Iterator.prototype.take requires a non-negative number");

            return new JSIteratorObject(new TakeEnumerator(en, (int)n));
        }
        catch
        {
            CloseIteratorIfPossible(en);
            throw;
        }
    }

    internal static JSValue StaticDrop(in Arguments a)
    {
        var en = EnumeratorFrom(a.This);

        try
        {
            var n = a.Get1().DoubleValue;

            if (double.IsNaN(n) || n < 0)
                throw JSEngine.NewRangeError("Iterator.prototype.drop requires a non-negative number");

            return new JSIteratorObject(new DropEnumerator(en, (int)n));
        }
        catch
        {
            CloseIteratorIfPossible(en);
            throw;
        }
    }

    internal static JSValue StaticFlatMap(in Arguments a)
    {
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();

        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.flatMap requires a callable argument");
        }

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
        var en = EnumeratorFrom(a.This);
        var (fn, initialValue) = a.Get2();

        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.reduce requires a callable argument");
        }

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
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();
        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.forEach requires a callable argument");
        }

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
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();
        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.some requires a callable argument");
        }

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
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();
        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.every requires a callable argument");
        }

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
        var en = EnumeratorFrom(a.This);
        var fn = a.Get1();
        if (!fn.IsFunction)
        {
            CloseIteratorIfPossible(en);
            throw JSEngine.NewTypeError("Iterator.prototype.find requires a callable argument");
        }

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
    internal sealed class MapEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator
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
    }

    internal sealed class FilterEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator
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
    }

    internal sealed class TakeEnumerator(IElementEnumerator source, int limit) : IElementEnumerator
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
    }

    internal sealed class DropEnumerator(IElementEnumerator source, int count) : IElementEnumerator
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
    }

    internal sealed class FlatMapEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator
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
    }

    private readonly record struct ConcatSource(JSObject Iterable, JSValue IteratorMethod);

    private sealed class ConcatEnumerator(ConcatSource[] iterables) : IElementEnumerator, IReturnableEnumerator
    {
        private int _current = 0;
        private IElementEnumerator _currentEnum = iterables.Length > 0 ? GetEnumerator(iterables[0]) : null;

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

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
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
            while (_currentEnum != null)
            {
                if (_currentEnum.MoveNext(out var v)) return v;
                if (!Advance()) break;
            }

            return @default;
        }

        public JSValue Return(JSValue value)
        {
            if (_currentEnum is IReturnableEnumerator returnable)
                return returnable.Return(value);

            return IteratorResult(value, true);
        }
    }
}
