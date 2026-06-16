using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.Function;

public class JSGeneratorFunctionV2 : JSFunction
{
    private sealed class PrototypeCache
    {
        public JSObject GeneratorFunctionPrototype;
        public JSObject AsyncGeneratorFunctionPrototype;
        public JSObject AsyncGeneratorPrototype;
    }

    private static readonly ConditionalWeakTable<object, PrototypeCache> PrototypeCaches = [];
    private static readonly object PrototypeCacheFallback = new();

    readonly JSGeneratorDelegateV2 @delegate;
    readonly bool asyncGenerator;
    readonly bool primeOnInvoke;

    private static JSObject CreateGeneratorFunctionPrototype(bool asyncGenerator)
    {
        var prototype = new JSObject();
        if ((Engine.Core.JSEngine.Current as Engine.IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
            prototype.BasePrototypeObject = functionPrototype;

        var constructorName = asyncGenerator ? "AsyncGeneratorFunction" : "GeneratorFunction";
        var constructor = (JSFunction)JSValue.CreateFunction((in Arguments a) =>
        {
            var created = JSFunction.CreateDynamicFunction(in a, asyncGenerator ? "async function*" : "function*");
            if (created is JSFunction function)
                function.prototype = null;

            return created;
        }, constructorName, $"function {constructorName}() {{ [native code] }}", 1, createPrototype: false);
        constructor.FastAddValue(KeyStrings.prototype, prototype, JSPropertyAttributes.ReadonlyValue);
        constructor.prototype = prototype;
        // §27.3.3.2 / §27.4.3.2: the .prototype.constructor property is non-writable
        // (attributes { writable: false, enumerable: false, configurable: true }).
        prototype.FastAddValue(KeyStrings.constructor, constructor, JSPropertyAttributes.ConfigurableReadonlyValue);

        var generatorPrototype = GetGeneratorPrototype(asyncGenerator);
        if (generatorPrototype != null)
        {
            prototype.FastAddValue(KeyStrings.prototype, generatorPrototype, JSPropertyAttributes.ConfigurableReadonlyValue);
            generatorPrototype.FastAddValue(KeyStrings.constructor, prototype, JSPropertyAttributes.ConfigurableReadonlyValue);
        }

        // §27.3.3.2 / §27.4.3.2: GeneratorFunction.prototype[@@toStringTag] / AsyncGeneratorFunction.prototype[@@toStringTag]
        prototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString(constructorName), JSPropertyAttributes.ConfigurableReadonlyValue);

        return prototype;
    }

    private static JSObject GetGeneratorPrototype(bool asyncGenerator)
    {
        if ((Engine.Core.JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Generator")] is not JSFunction generatorCtor
            || generatorCtor.prototype is not JSObject generatorPrototype)
            return null;

        if (!asyncGenerator)
            return generatorPrototype;

        var cacheKey = Engine.Core.JSEngine.Current as object ?? PrototypeCacheFallback;
        var cache = PrototypeCaches.GetOrCreateValue(cacheKey);
        if (cache.AsyncGeneratorPrototype != null)
            return cache.AsyncGeneratorPrototype;

        var asyncGeneratorPrototype = new JSObject
        {
            BasePrototypeObject = generatorPrototype.GetPrototypeOf()
        };

        AddAsyncGeneratorMethod(generatorPrototype, asyncGeneratorPrototype, KeyStrings.next, "next");
        AddAsyncGeneratorMethod(generatorPrototype, asyncGeneratorPrototype, KeyStrings.GetOrCreate("return"), "return");
        AddAsyncGeneratorMethod(generatorPrototype, asyncGeneratorPrototype, KeyStrings.GetOrCreate("throw"), "throw");

        asyncGeneratorPrototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString("AsyncGenerator"), JSPropertyAttributes.ConfigurableReadonlyValue);
        return cache.AsyncGeneratorPrototype = asyncGeneratorPrototype;
    }

    /// <summary>
    /// Adds an %AsyncGeneratorPrototype% method (next/return/throw) that always
    /// returns a promise.  Per §27.6.1.2-4, AsyncGeneratorValidate runs *after*
    /// the promise capability is created, so a receiver that is not an async
    /// generator must produce a rejected promise rather than a synchronous throw.
    /// When the receiver is a genuine async generator we delegate to the shared
    /// %GeneratorPrototype% method (JSGenerator already wraps its result in a
    /// promise for async generators).
    /// </summary>
    private static void AddAsyncGeneratorMethod(JSObject source, JSObject target, KeyString key, string name)
    {
        if (source[key] is not JSFunction inner)
            return;

        JSValue Method(in Arguments a)
        {
            if (a.This is Generator.JSGenerator generator && generator.IsAsyncGenerator)
                return inner.InvokeFunction(in a);

            var error = Engine.Core.JSEngine.NewTypeError(
                $"AsyncGenerator.prototype.{name} called on incompatible receiver");
            return Engine.Core.JSEngine.CreateResolvedOrRejectedPromise(JSException.ErrorFrom(error), false);
        }

        target.FastAddValue(key, JSValue.CreateFunction(Method, name, null, 1, createPrototype: false), JSPropertyAttributes.ConfigurableValue);
    }

    private static JSObject GetGeneratorFunctionPrototype(bool asyncGenerator)
    {
        var cacheKey = Engine.Core.JSEngine.Current as object ?? PrototypeCacheFallback;
        var cache = PrototypeCaches.GetOrCreateValue(cacheKey);
        ref var prototype = ref asyncGenerator
            ? ref cache.AsyncGeneratorFunctionPrototype
            : ref cache.GeneratorFunctionPrototype;

        return prototype ??= CreateGeneratorFunctionPrototype(asyncGenerator);
    }

    public JSGeneratorFunctionV2(JSGeneratorDelegateV2 @delegate, in StringSpan name, in StringSpan code, int length = 0, bool asyncGenerator = false, bool primeOnInvoke = false, bool coerceThis = true) : base(null, name, code, length)
    {
        this.@delegate = @delegate;
        this.asyncGenerator = asyncGenerator;
        this.primeOnInvoke = primeOnInvoke;

        // §27.3.4 / §27.4.4: The .prototype property of each generator
        // function is a new ordinary object whose [[Prototype]] is
        // %GeneratorPrototype% (or %AsyncGeneratorPrototype%).
        // Save the prototype object created by the base constructor before
        // nulling the field so IsConstructor returns false.
        var protoObj = prototype;
        if (protoObj != null)
        {
            protoObj.Delete(KeyStrings.constructor);
            var generatorPrototype = GetGeneratorPrototype(asyncGenerator);
            if (generatorPrototype != null)
                protoObj.BasePrototypeObject = generatorPrototype;
            GetOwnProperties().Put(KeyStrings.prototype, protoObj, JSPropertyAttributes.Value);
        }
        prototype = null;

        // A non-strict generator coerces an undefined/null `this` to the global object on
        // invocation (OrdinaryCallBindThis for a sloppy function); a strict generator must
        // leave `this` untouched (e.g. `({ *m(){ "use strict"; } }).m()` sees `this` ===
        // undefined). The compiler passes coerceThis = !isStrictFunction, mirroring the
        // ordinary-function handling (EnableNonStrictThis / EnableStrictMode).
        CoerceThisOnInvoke = coerceThis;
        f = InvokeFunction;
        BasePrototypeObject = GetGeneratorFunctionPrototype(asyncGenerator);
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        var args = CoerceThisOnInvoke
            ? a.OverrideThis(JSFunction.CoerceNonStrictThis(a.This))
            : a;

        var generator = JSGeneratorBuilder.CreateFromClrV2(new ClrGeneratorV2(this, @delegate, args, asyncGenerator));

        // Priming runs FunctionDeclarationInstantiation (and so the default-parameter
        // initializers) and suspends the body at its start. Per §27.4.10/§27.5.3.x
        // EvaluateBody, this happens BEFORE the generator object is created from
        // OrdinaryCreateFromConstructor — so the `.prototype` lookup that fixes the
        // generator's [[Prototype]] must observe any reassignment a parameter initializer
        // made (e.g. `function* g(a = (g.prototype = null)) {}`). Hence prime first, then
        // bind the prototype.
        if (primeOnInvoke && generator is IJSGenerator jsGenerator)
            jsGenerator.MoveNext(JSUndefined.Value, out _);

        // §27.5.3.x: the generator object is created via
        // OrdinaryCreateFromConstructor(functionObject, "%GeneratorPrototype%"), whose
        // [[Prototype]] is Get(functionObject, "prototype") — this generator function's
        // OWN .prototype property (which itself inherits %GeneratorPrototype%). So
        // `Object.getPrototypeOf(g()) === g.prototype`. Read it live; when it is not an
        // object (per GetPrototypeFromConstructor) fall back to the intrinsic
        // %GeneratorPrototype%. Only the SYNC path is rebound here: the async generator
        // prototype chain (%AsyncGeneratorPrototype% → %AsyncIteratorPrototype% carrying
        // @@asyncIterator) is wired separately and the function's .prototype does not reach
        // it, so async generators keep the default object prototype that supports `for await`.
        if (!asyncGenerator && generator is JSObject genObject)
            genObject.BasePrototypeObject = this[KeyStrings.prototype] is JSObject ownPrototype
                ? ownPrototype
                : GetGeneratorPrototype(asyncGenerator);

        return generator;
    }

    public override JSValue CreateInstance(in Arguments a)
        => throw Engine.Core.JSEngine.NewTypeError($"{name} is not a constructor");
}
