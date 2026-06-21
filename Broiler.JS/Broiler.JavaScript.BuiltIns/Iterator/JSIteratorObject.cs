using System;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
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
    private bool _started;

    // A %WrapForValidIterator% produced by Iterator.from delegates to an underlying iterator record
    // { [[Iterator]], [[NextMethod]] }: its next() calls the stored next method and returns the result
    // verbatim (NOT validated to be an Object), and its return() forwards to the underlying "return".
    private readonly JSObject _wrapIterator;
    private readonly JSValue _wrapNextMethod;

    // ---------------------------------------------------------------
    // Constructors
    // ---------------------------------------------------------------

    public JSIteratorObject(in Arguments a) : this(ResolveSubclassPrototype()) { }

    /// <summary>
    /// Iterator ( ) (ES2025 §27.1.3.1): the abstract %Iterator% constructor throws a
    /// TypeError when NewTarget is undefined (a plain call) OR when NewTarget is the
    /// %Iterator% intrinsic itself (`new Iterator()` / `Reflect.construct(Iterator, [],
    /// Iterator)`). A genuine subclass — `class X extends Iterator { constructor(){ super(); } }`
    /// — passes a distinct NewTarget and allocates from its prototype via
    /// OrdinaryCreateFromConstructor (falling back to %Iterator.prototype% when the
    /// subclass's <c>prototype</c> property is not an object).
    /// </summary>
    private static JSObject ResolveSubclassPrototype()
    {
        // A native [[Construct]] keeps its new.target in CurrentNewTarget; a plain call
        // leaves both null.
        var newTarget = JSEngine.NewTarget ?? (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget;
        var iteratorConstructor = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Iterator")];

        if (newTarget == null || newTarget.IsUndefined || ReferenceEquals(newTarget, iteratorConstructor))
            throw JSEngine.NewTypeError("Iterator is not intended to be called as a constructor");

        // OrdinaryCreateFromConstructor(newTarget, "%Iterator.prototype%"): the resolved
        // prototype is newTarget.prototype, or %Iterator.prototype% when that is absent.
        return JSEngine.NewTargetPrototype
            ?? ((iteratorConstructor as JSFunction)?.prototype);
    }

    internal JSIteratorObject(IElementEnumerator enumerator) : this(HelperPrototype()) => _enumerator = enumerator;

    // An engine iterator with an explicitly chosen prototype (the %WrapForValidIteratorPrototype%
    // used by Iterator.from, vs the %IteratorHelperPrototype% used by map/filter/…).
    internal JSIteratorObject(JSObject prototype, IElementEnumerator enumerator) : this(prototype) => _enumerator = enumerator;

    // A delegating %WrapForValidIterator% (Iterator.from over an object/iterator): next()/return()
    // forward to the underlying iterator and pass its results through unchanged.
    internal JSIteratorObject(JSObject prototype, JSObject wrapIterator, JSValue wrapNextMethod) : this(prototype)
    {
        _wrapIterator = wrapIterator;
        _wrapNextMethod = wrapNextMethod;
    }

    // %IteratorHelperPrototype% and %WrapForValidIteratorPrototype% are per-realm objects whose
    // [[Prototype]] is that realm's %Iterator.prototype%; they (not the base prototype) carry next /
    // return. Keyed by the base prototype so each realm resolves its own pair. (Stored off-object so
    // they do not appear among %Iterator.prototype%'s own properties, which test262 pins exactly.)
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<JSObject, JSObject> HelperPrototypes = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<JSObject, JSObject> WrapPrototypes = new();

    internal static void RegisterHelperPrototypes(JSObject baseProto, JSObject helperProto, JSObject wrapProto)
    {
        HelperPrototypes.AddOrUpdate(baseProto, helperProto);
        WrapPrototypes.AddOrUpdate(baseProto, wrapProto);
    }

    private static JSObject BaseIteratorPrototype()
        => ((JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Iterator")] as JSFunction)?.prototype;

    private static JSObject HelperPrototype()
    {
        var b = BaseIteratorPrototype();
        return b != null && HelperPrototypes.TryGetValue(b, out var h) ? h : b;
    }

    private static JSObject WrapPrototype()
    {
        var b = BaseIteratorPrototype();
        return b != null && WrapPrototypes.TryGetValue(b, out var w) ? w : b;
    }

    // ---------------------------------------------------------------
    // Iterator protocol – next / return
    //
    // These are NOT exported onto %Iterator.prototype%; they are registered on
    // %IteratorHelperPrototype% / %WrapForValidIteratorPrototype% (see IteratorPrototypeSetup), so the
    // base %Iterator.prototype% has no own "next"/"return" (per spec, and so its @@dispose finds no
    // "return" to call).
    // ---------------------------------------------------------------
    internal JSValue Next(in Arguments a)
    {
        ThrowIfExecuting();

        if (_wrapIterator != null)
        {
            // %WrapForValidIteratorPrototype%.next: Call([[NextMethod]], [[Iterator]]) and return the
            // result verbatim (a non-Object result is NOT rejected here, unlike the iterator protocol).
            if (!_wrapNextMethod.IsFunction)
                throw JSEngine.NewTypeError("Iterator.from: the wrapped iterator's next method is not callable");

            try
            {
                _executing = true;
                _started = true;
                return _wrapNextMethod.InvokeFunction(new Arguments(_wrapIterator));
            }
            finally
            {
                _executing = false;
            }
        }

        try
        {
            _executing = true;
            _started = true;
            if (!_done && _enumerator != null && _enumerator.MoveNext(out var value))
                return IteratorResult(value, false);

            // The source is exhausted: latch the completed state so a later next() returns done
            // immediately without re-pulling the underlying iterator (an Iterator Helper that has
            // returned done never calls the source again — test262 sm/Iterator lazy-methods-proxy-accesses).
            _done = true;
        }
        finally
        {
            _executing = false;
        }

        return IteratorResult(JSUndefined.Value, true);
    }

    internal JSValue Return(in Arguments a)
    {
        var value = a.Length > 0 ? a.Get1() : JSUndefined.Value;
        ThrowIfExecuting();

        if (_wrapIterator != null)
        {
            // %WrapForValidIteratorPrototype%.return: forward to the underlying iterator's "return"
            // method if present; otherwise the wrapper is simply marked done.
            _done = true;
            var returnMethod = _wrapIterator[KeyStrings.GetOrCreate("return")];
            if (returnMethod.IsNullOrUndefined)
                return IteratorResult(JSUndefined.Value, true);
            if (!returnMethod.IsFunction)
                throw JSEngine.NewTypeError("Iterator.from: the wrapped iterator's return method is not callable");
            return returnMethod.InvokeFunction(new Arguments(_wrapIterator));
        }

        if (_done)
            return IteratorResult(value, true);

        _done = true;
        if (_enumerator is IReturnableEnumerator returnable)
        {
            // A generator that has never been resumed is in "suspended-start": per
            // %IteratorHelperPrototype%.return it transitions straight to "completed"
            // and only then closes its underlying iterators, so a reentrant
            // next()/return() from an underlying return() observes the completed
            // state and yields a done result (it must NOT throw "already executing").
            if (!_started)
                return returnable.Return();

            // A generator that has yielded at least once is in "suspended-yield":
            // return() resumes it (state becomes "executing") to run the close, so a
            // reentrant next()/return() during that close must throw "already executing".
            try
            {
                _executing = true;
                return returnable.Return();
            }
            finally
            {
                _executing = false;
            }
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

        // A wrapper produced by Iterator.from uses %WrapForValidIteratorPrototype% (next / return,
        // but no own @@toStringTag — it inherits "Iterator" from %Iterator.prototype%).
        JSObject iterator;
        if (obj.IsString)
        {
            // GetIteratorFlattenable special-cases primitive strings: rather than ToObject-ing
            // first, GetMethod(string, @@iterator) is performed directly on the primitive, so a
            // String.prototype[@@iterator] accessor observes the primitive string as its receiver
            // (test262 Iterator/from/iterable-primitives). The method is then called with the
            // primitive as the this value to obtain the iterator.
            var stringIteratorMethod = obj[(IJSSymbol)JSSymbol.iterator];
            if (stringIteratorMethod.IsNull || stringIteratorMethod.IsUndefined)
                return new JSIteratorObject(WrapPrototype(), obj.GetElementEnumerator());
            if (!stringIteratorMethod.IsFunction)
                throw JSEngine.NewTypeError("Iterator.from requires a callable @@iterator");
            var stringIteratorValue = stringIteratorMethod.InvokeFunction(new Arguments(obj));
            if (stringIteratorValue is not JSObject stringIteratorObject)
                throw JSEngine.NewTypeError("Iterator.from requires an object iterator result");
            iterator = stringIteratorObject;
        }
        else if (obj is not JSObject @object)
        {
            throw JSEngine.NewTypeError("Iterator.from requires an iterable or iterator argument");
        }
        else
        {
            // GetIteratorFlattenable: with no (or nullish) @@iterator the object is itself the iterator;
            // otherwise its @@iterator is called to obtain one. The wrapper records { iterator, nextMethod }
            // and delegates next()/return() to it, returning results verbatim (WrapForValidIterator).
            var iteratorMethod = @object[(IJSSymbol)JSSymbol.iterator];
            if (iteratorMethod.IsNull || iteratorMethod.IsUndefined)
            {
                iterator = @object;
            }
            else if (!iteratorMethod.IsFunction)
            {
                throw JSEngine.NewTypeError("Iterator.from requires a callable @@iterator");
            }
            else
            {
                var iteratorValue = iteratorMethod.InvokeFunction(new Arguments(@object));
                if (iteratorValue is not JSObject iteratorObject)
                    throw JSEngine.NewTypeError("Iterator.from requires an object iterator result");
                iterator = iteratorObject;
            }
        }

        // GetIteratorDirect reads "next" before the %Iterator% brand check, so the
        // "get next" observation always precedes the getPrototypeOf walk below.
        var nextMethod = iterator[KeyStrings.next];

        // hasInstance = ? OrdinaryHasInstance(%Iterator%, iterator): walk the
        // iterator's prototype chain (each step is an observable [[GetPrototypeOf]],
        // so a Proxy iterator fires its getPrototypeOf trap) looking for
        // %Iterator.prototype%. If present, the iterator is already a valid Iterator
        // and is returned unwrapped; otherwise wrap it (test262 sm Iterator/from/
        // proxy-wrap-next, proxy-wrap-return, proxy-not-wrapped, return-iterator-if-iterable).
        var iteratorPrototype = BaseIteratorPrototype();
        if (iteratorPrototype != null)
        {
            var current = iterator.GetPrototypeOf();
            while (current is JSObject currentObject)
            {
                if (ReferenceEquals(currentObject, iteratorPrototype))
                    return iterator;

                current = currentObject.GetPrototypeOf();
            }
        }

        return new JSIteratorObject(WrapPrototype(), iterator, nextMethod);
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

    // %IteratorHelperPrototype% / %WrapForValidIteratorPrototype% next/return require the Iterator Helper
    // (or Wrap) brand: the receiver must be one of these engine iterator objects. A generator or any
    // other object (e.g. helperProto.next.call(generator)) lacks the slot and is a TypeError — generators
    // expose their OWN next/return via %GeneratorPrototype%, never these helper methods.
    internal static JSValue StaticNext(in Arguments a)
    {
        return a.This switch
        {
            JSIteratorObject iterator => iterator.Next(in a),
            _ => throw JSEngine.NewTypeError("Iterator Helper next called on incompatible receiver")
        };
    }

    internal static JSValue StaticReturn(in Arguments a)
    {
        return a.This switch
        {
            JSIteratorObject iterator => iterator.Return(in a),
            _ => throw JSEngine.NewTypeError("Iterator Helper return called on incompatible receiver")
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

    // IteratorClose with a *normal* completion: unlike CloseIteratorIfPossible (used after a throw
    // completion, where IteratorClose keeps the original error and swallows the return error), an
    // error thrown while reading or invoking the underlying iterator's return method must propagate.
    internal static void CloseIterator(IElementEnumerator enumerator)
    {
        if (enumerator is IReturnableEnumerator returnable)
            returnable.Return();
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
        // Iterator.prototype.take step 2: "If O is not an Object, throw a TypeError" — checked before
        // ToNumber(limit), so a non-object receiver is a TypeError even when the limit is invalid.
        if (a.This is not JSObject)
            throw JSEngine.NewTypeError("Iterator.prototype.take called on a non-object");
        var n = ReadIteratorLimitOrClose(a.This, a.Get1(), "Iterator.prototype.take");
        var en = EnumeratorFrom(a.This);
        return new JSIteratorObject(new TakeEnumerator(en, n));
    }

    internal static JSValue StaticDrop(in Arguments a)
    {
        if (a.This is not JSObject)
            throw JSEngine.NewTypeError("Iterator.prototype.drop called on a non-object");
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
            // Iterator.prototype.{take,drop}: ToNumber(limit) — a NaN limit is a RangeError. Otherwise
            // ToIntegerOrInfinity truncates toward zero *before* the sign check, so a value in (-1, 0]
            // such as -0.9 or -0 becomes 0 (valid) rather than being rejected as negative. A strictly
            // negative integer is a RangeError; +∞ means "no limit", clamped here to int.MaxValue.
            var number = limitValue.DoubleValue;
            if (double.IsNaN(number))
                throw JSEngine.NewRangeError($"{methodName} requires a non-negative number");

            var integer = Math.Truncate(number);
            if (integer < 0)
                throw JSEngine.NewRangeError($"{methodName} requires a non-negative number");

            return integer >= int.MaxValue ? int.MaxValue : (int)integer;
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
                CloseIterator(en);
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
                CloseIterator(en);
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
                CloseIterator(en);
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

                // §27.1.4.2 step 14.b.iv: fetch one padding value per iterable, but stop
                // calling the padding iterator's next() once it reports done — every
                // remaining slot is filled with undefined without another next() call.
                bool usingIterator = true;
                try
                {
                    for (int i = 0; i < _iterators.Count; i++)
                    {
                        if (usingIterator && paddingIter.MoveNext(out var paddingValue))
                            _paddingValues.Add(paddingValue);
                        else
                        {
                            usingIterator = false;
                            _paddingValues.Add(JSUndefined.Value);
                        }
                    }
                }
                catch
                {
                    CloseIteratorReverse(_iterators);
                    throw;
                }

                Exception? firstException = null;
                // Step 14.b.v: only close the padding iterator when it has NOT already
                // run to completion (a done iterator is not re-closed).
                if (usingIterator)
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

        // IteratorCloseAll on a NORMAL completion (e.g. "shortest" mode finishing because
        // one input is exhausted): close the still-open iterators in reverse order and,
        // unlike the abrupt-completion close, propagate the first error thrown by a
        // return() method (subsequent return() errors are swallowed, matching
        // IteratorClose's "if completion is a throw, keep it" rule).
        private void CloseAllActivePropagating()
        {
            Exception? firstException = null;
            for (int i = _iterators.Count - 1; i >= 0; i--)
                if (_iterators[i] != null)
                    CloseIteratorForReturn(_iterators[i], ref firstException);

            if (firstException != null)
                throw firstException;
        }

        public bool MoveNext(out JSValue value)
        {
            if (_done)
            {
                value = JSUndefined.Value;
                return false;
            }

            // With no iterators (e.g. Iterator.zip([])), the result is immediately
            // done in every mode rather than yielding an endless stream of empty rows.
            if (_iterators.Count == 0)
            {
                _done = true;
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
                            CloseAllActivePropagating();
                            value = JSUndefined.Value;
                            return false;
                        }

                        _done = true;
                        CloseAllActivePropagating();
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
                    CloseAllActivePropagating();
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

            // [[OwnPropertyKeys]]: snapshot every own key (integer-index + string, then
            // symbol) BEFORE reading any value, so a getter that mutates the object mid-read
            // cannot drop a later key (test262 zipKeyed/iterables-iteration-deleted) and
            // symbol-keyed iterables are included (zipKeyed/iterables-iteration-symbol-key).
            var ownKeys = new List<JSValue>();
            var allKeys = iterables.GetAllKeys(false, false);
            while (allKeys.MoveNext(out var key))
                ownKeys.Add(key);

            // A Proxy's GetAllKeys already returned its symbol keys via the ownKeys trap.
            if (iterables is not JSProxy)
            {
                foreach (var (symbolKey, property) in iterables.GetSymbols().AllValues())
                {
                    if (property.IsEmpty)
                        continue;

                    var symbol = JSValue.GetSymbolByKeyFactory?.Invoke(symbolKey);
                    if (symbol != null)
                        ownKeys.Add((JSValue)symbol);
                }
            }

            try
            {
                foreach (var key in ownKeys)
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

        // IteratorCloseAll on a NORMAL completion (e.g. "shortest" mode finishing because
        // one input is exhausted): close the still-open iterators in reverse order and,
        // unlike the abrupt-completion close, propagate the first error thrown by a
        // return() method (subsequent return() errors are swallowed, matching
        // IteratorClose's "if completion is a throw, keep it" rule).
        private void CloseAllActivePropagating()
        {
            Exception? firstException = null;
            for (int i = _iterators.Count - 1; i >= 0; i--)
                if (_iterators[i] != null)
                    CloseIteratorForReturn(_iterators[i], ref firstException);

            if (firstException != null)
                throw firstException;
        }

        public bool MoveNext(out JSValue value)
        {
            if (_done)
            {
                value = JSUndefined.Value;
                return false;
            }

            // With no iterators (e.g. Iterator.zipKeyed({})), the result is immediately
            // done in every mode rather than yielding an endless stream of empty objects.
            if (_iterators.Count == 0)
            {
                _done = true;
                value = JSUndefined.Value;
                return false;
            }

            // Per spec the zipKeyed result is OrdinaryObjectCreate(null): a
            // null-prototype object whose own data properties are the zipped values.
            var result = JSObject.NewWithProperties();
            result.BasePrototypeObject = null;

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
                            CloseAllActivePropagating();
                            value = JSUndefined.Value;
                            return false;
                        }

                        _done = true;
                        CloseAllActivePropagating();
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
                    CloseAllActivePropagating();
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

        // %Iterator.prototype.map% step: an abrupt completion from the mapper closes the underlying
        // iterator (IfAbruptCloseIterator) before the error propagates; the close error is swallowed.
        private JSValue Invoke(JSValue item)
        {
            try { return fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++))); }
            catch { CloseIteratorIfPossible(source); throw; }
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            if (source.MoveNext(out hasValue, out var item, out index))
            {
                value = Invoke(item);
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            if (source.MoveNext(out var item))
            {
                value = Invoke(item);
                return true;
            }

            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (source.MoveNext(out var item))
            {
                value = Invoke(item);
                return true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (source.MoveNext(out var item))
                return Invoke(item);

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

        // %Iterator.prototype.filter% step: an abrupt completion from the predicate closes the underlying
        // iterator (IfAbruptCloseIterator) before the error propagates; the close error is swallowed.
        private bool Test(JSValue item)
        {
            try { return fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(predicateCount++))).BooleanValue; }
            catch { CloseIteratorIfPossible(source); throw; }
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            while (source.MoveNext(out var item))
            {
                if (Test(item))
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
                if (Test(item))
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
                if (Test(item))
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
                if (Test(item))
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
        private bool closed = false;
        // Once the helper completes — the source is naturally exhausted, or the take limit is reached —
        // it stays done: a further next() must NOT pull from the source again (the spec's helper
        // generator has returned). Without this latch a source that keeps producing a "done" result
        // (e.g. a hand-written iterator) would be advanced one extra time per subsequent call.
        private bool exhausted = false;

        // When the take limit is reached (remaining hits 0) the helper completes with a *normal*
        // completion, which per spec performs IteratorClose on the underlying iterator. A throwing
        // return propagates; natural source exhaustion needs no close (the source is already done).
        private void CloseOnLimit()
        {
            if (closed)
                return;

            closed = true;
            if (source is IReturnableEnumerator returnable)
                returnable.Return();
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            value = JSUndefined.Value; hasValue = false; index = 0;
            if (exhausted)
                return false;

            if (taken < limit)
            {
                if (source.MoveNext(out hasValue, out value, out index))
                {
                    taken++;
                    return true;
                }

                // Natural source exhaustion: latch done (no IteratorClose needed).
                exhausted = true;
                value = JSUndefined.Value; hasValue = false; index = 0;
                return false;
            }

            // Take limit reached: latch done and close the underlying iterator.
            exhausted = true;
            CloseOnLimit();
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            value = JSUndefined.Value;
            if (exhausted)
                return false;

            if (taken < limit)
            {
                if (source.MoveNext(out value))
                {
                    taken++;
                    return true;
                }

                exhausted = true;
                value = JSUndefined.Value;
                return false;
            }

            exhausted = true;
            CloseOnLimit();
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            if (!exhausted && taken < limit)
            {
                if (source.MoveNext(out value))
                {
                    taken++;
                    return true;
                }

                exhausted = true;
            }

            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            if (!exhausted && taken < limit)
            {
                if (source.MoveNext(out var v))
                {
                    taken++;
                    return v;
                }

                exhausted = true;
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
        // Once the source is exhausted — whether while dropping the leading `count` values or during
        // later iteration — the helper stays done and must not pull from the source again (otherwise a
        // hand-written iterator that keeps reporting "done" would be advanced one extra time).
        private bool _exhausted;

        private void EnsureDropped()
        {
            if (_dropped)
                return;

            _dropped = true;

            for (int i = 0; i < count; i++)
                if (!source.MoveNext(out _)) { _exhausted = true; break; }
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            EnsureDropped();
            if (!_exhausted && source.MoveNext(out hasValue, out value, out index))
                return true;

            _exhausted = true;
            value = JSUndefined.Value; hasValue = false; index = 0;
            return false;
        }

        public bool MoveNext(out JSValue value)
        {
            EnsureDropped();
            if (!_exhausted && source.MoveNext(out value))
                return true;

            _exhausted = true;
            value = JSUndefined.Value;
            return false;
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            EnsureDropped();
            if (!_exhausted && source.MoveNextOrDefault(out value, @default))
                return true;

            _exhausted = true;
            value = @default;
            return false;
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            EnsureDropped();
            if (_exhausted)
                return @default;

            return source.NextOrDefault(@default);
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

    internal sealed class FlatMapEnumerator(IElementEnumerator source, JSValue fn) : IElementEnumerator, IReturnableEnumerator
    {
        private IElementEnumerator _inner;
        private uint _count;

        // %Iterator.prototype.flatMap% step: an abrupt completion from the mapper, or from obtaining the
        // mapped value's iterator (GetIteratorFlattenable), closes the underlying iterator before the
        // error propagates (IfAbruptCloseIterator); the close error is swallowed.
        private IElementEnumerator MapItem(JSValue item)
        {
            try { return GetFlattenableEnumerator(fn.InvokeFunction(new Arguments(JSUndefined.Value, item, JSValue.CreateNumber(_count++)))); }
            catch { CloseIteratorIfPossible(source); throw; }
        }

        // IfAbruptCloseIterator(innerNext, iterated): an abrupt completion while stepping the inner
        // iterator closes the *outer* (source) iterator before the error propagates.
        private bool StepInner(out bool hasValue, out JSValue value, out uint index)
        {
            try { return _inner.MoveNext(out hasValue, out value, out index); }
            catch { CloseIteratorIfPossible(source); throw; }
        }

        private bool StepInner(out JSValue value)
        {
            try { return _inner.MoveNext(out value); }
            catch { CloseIteratorIfPossible(source); throw; }
        }

        public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
        {
            while (true)
            {
                if (_inner != null && StepInner(out hasValue, out value, out index))
                    return true;

                if (!source.MoveNext(out var item))
                { value = JSUndefined.Value; hasValue = false; index = 0; return false; }

                _inner = MapItem(item);
            }
        }

        public bool MoveNext(out JSValue value)
        {
            while (true)
            {
                if (_inner != null && StepInner(out value)) return true;
                if (!source.MoveNext(out var item))
                { value = JSUndefined.Value; return false; }

                _inner = MapItem(item);
            }
        }

        public bool MoveNextOrDefault(out JSValue value, JSValue @default)
        {
            while (true)
            {
                if (_inner != null && StepInner(out value)) return true;
                if (!source.MoveNext(out var item))
                { value = @default; return false; }
                _inner = MapItem(item);
            }
        }

        public JSValue NextOrDefault(JSValue @default)
        {
            while (true)
            {
                if (_inner != null && StepInner(out var v)) return v;
                if (!source.MoveNext(out var item)) return @default;

                _inner = MapItem(item);
            }
        }

        // %IteratorHelperPrototype%.return for flatMap closes *both* the active inner iterator (when
        // one is alive) and the outer (source) iterator, surfacing the first close error if any.
        private JSValue CloseBoth(JSValue value)
        {
            Exception firstException = null;
            CloseIteratorForReturn(_inner, ref firstException);
            CloseIteratorForReturn(source, ref firstException);
            if (firstException != null)
                throw firstException;
            return IteratorResult(value, true);
        }

        public JSValue Return() => CloseBoth(JSUndefined.Value);

        public JSValue Return(JSValue value) => CloseBoth(value);
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
