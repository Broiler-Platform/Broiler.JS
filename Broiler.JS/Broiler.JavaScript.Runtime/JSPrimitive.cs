using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// JSPrimitive class does not hold prototype, prototype is only resolved from
/// current context when requested first time
/// 
/// Boolean, Number, Integer are derived from JSPrimitive
/// </summary>
public abstract class JSPrimitive: JSValue
{
    internal protected void ResolvePrototype() { 
        BasePrototypeObject = GetPrototype();
    }

    protected abstract JSValue GetPrototype();

    protected JSPrimitive() : base(null)
    {

    }

    protected JSPrimitive(JSValue prototype): base(prototype)
    {

    }

    public override JSValue this[IJSSymbol symbol] {
        get
        {
            ResolvePrototype();
            if (prototypeChain == null)
                return UndefinedValue;
            // OrdinaryGet with the primitive as the receiver: walk the wrapper prototype
            // chain through each object's [[Get]] (rather than flattening it into a single
            // internal-property lookup) so an inherited accessor's getter — and a Proxy in
            // the chain — is invoked with this primitive as its this/receiver value.
            return GetValue(symbol, this);
        }
        set
        {
            if (!SetValue(symbol, value, this, IsStrictModeEnabled?.Invoke() == true))
                ThrowOnStrictPrimitiveAssignment(symbol);
        }
    }

    public override JSValue this[KeyString name]
    {
        get
        {
            // A private member access on a primitive is a brand-check TypeError:
            // ToObject would create a fresh wrapper that has no private fields.
            if (JSObject.IsPrivateName(in name))
                JSObject.ThrowMissingPrivateMember(in name, reading: true);

            ResolvePrototype();
            if (prototypeChain == null)
                return UndefinedValue;
            // OrdinaryGet with the primitive as the receiver: walk the wrapper prototype
            // chain through each object's [[Get]] (rather than flattening it into a single
            // internal-property lookup) so an inherited accessor's getter — and a Proxy in
            // the chain — is invoked with this primitive as its this/receiver value. A
            // subclass override (e.g. JSString's own "length"/index) still takes precedence.
            return GetValue(name, this);
        }
        set
        {
            if (!SetValue(name, value, this, IsStrictModeEnabled?.Invoke() == true))
                ThrowOnStrictPrimitiveAssignment(name);
        }
    }

    // OrdinarySet on a primitive: resolve the (lazy) wrapper prototype first, then
    // let the base walk the chain for an inherited accessor's setter (invoked with
    // this primitive as the receiver). A data property — or no property — cannot be
    // created on a primitive, so base returns false and the indexer setters above
    // fall back to the no-op (non-strict) / strict-throw behaviour.
    internal protected override bool SetValue(KeyString key, JSValue value, JSValue receiver, bool throwError = true)
    {
        ResolvePrototype();
        return base.SetValue(key, value, receiver, throwError);
    }

    internal protected override bool SetValue(IJSSymbol key, JSValue value, JSValue receiver, bool throwError = true)
    {
        ResolvePrototype();
        return base.SetValue(key, value, receiver, throwError);
    }

    public override bool SetValue(uint key, JSValue value, JSValue receiver, bool throwError = true)
    {
        ResolvePrototype();
        return base.SetValue(key, value, receiver, throwError);
    }

    public override IElementEnumerator GetAllKeys(bool showEnumerableOnly = true, bool inherited = true)
    {
        ResolvePrototype();
        return base.GetAllKeys(showEnumerableOnly, inherited);
    }

    // Dynamic (non-constant-folded) property reads route through the
    // GetValue(key, receiver) virtuals rather than the indexers above, so the
    // primitive's prototype must be resolved here too. Without this, the first
    // dynamic string-key or symbol-key read on a freshly-boxed primitive
    // (e.g. ""[Symbol.iterator], "abc"[someVar]) saw a null prototypeChain and
    // returned undefined.
    internal protected override JSValue GetValue(KeyString key, JSValue receiver, bool throwError = true)
    {
        if (JSObject.IsPrivateName(in key))
            JSObject.ThrowMissingPrivateMember(in key, reading: true);

        ResolvePrototype();
        return base.GetValue(key, receiver, throwError);
    }

    internal protected override JSValue GetValue(IJSSymbol key, JSValue receiver, bool throwError = true)
    {
        ResolvePrototype();
        return base.GetValue(key, receiver, throwError);
    }

    internal override JSFunctionDelegate GetMethod(in KeyString key)
    {
        if(prototypeChain == null)
        {
            BasePrototypeObject = GetPrototype();
        }
        return prototypeChain?.GetMethod(key);
    }

    public override JSValue GetPrototypeOf()
    {
        ResolvePrototype();
        return base.GetPrototypeOf();
    }

    // Iterating a primitive (e.g. `[...true]`, `yield* 0`, `new Map(0)`) follows
    // the same @@iterator protocol as objects: the method is looked up on the
    // wrapper prototype (e.g. Number.prototype[Symbol.iterator]) and called with
    // the primitive as the receiver. Primitives are not iterable by default, but
    // become so if a Symbol.iterator is installed on their prototype. JSString
    // keeps its dedicated code-point enumerator by overriding this.
    public override IElementEnumerator GetIterableEnumerator()
    {
        var iterator = this[JSValue.SymbolIterator];
        if (iterator.IsNullOrUndefined)
            throw NewTypeError(JSException.NotIterable(this));

        if (!iterator.IsFunction)
            throw NewTypeError("@@iterator is not a function");

        var iteratorResult = iterator.InvokeFunction(new Arguments(this));
        if (!iteratorResult.IsObject)
            throw NewTypeError("@@iterator result is not an object");

        return new JSIterator(iteratorResult);
    }
}
