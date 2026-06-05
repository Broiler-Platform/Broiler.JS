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
            var px = prototypeChain.GetInternalProperty(symbol);
            if (px.IsEmpty)
            {
                // throw JSEngine.Current.NewTypeError($"{name} property not found on {this.GetType().Name}:{this}");
                return UndefinedValue;
            }
            return this.GetValue(px);
        }
        set => ThrowOnStrictPrimitiveAssignment(symbol);
    }

    public override JSValue this[KeyString name]
    {
        get
        {
            ResolvePrototype();
            if (prototypeChain == null)
                return UndefinedValue;
            var px = prototypeChain.GetInternalProperty(name);
            if (px.IsEmpty)
            {
                // throw JSEngine.Current.NewTypeError($"{name} property not found on {this.GetType().Name}:{this}");
                return UndefinedValue;
            }
            return this.GetValue(px);
        }
        set => ThrowOnStrictPrimitiveAssignment(name);
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
}
