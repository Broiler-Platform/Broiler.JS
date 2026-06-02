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
        prototype.FastAddValue(KeyStrings.constructor, constructor, JSPropertyAttributes.ConfigurableValue);

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

        CopyGeneratorMethod(generatorPrototype, asyncGeneratorPrototype, KeyStrings.next);
        CopyGeneratorMethod(generatorPrototype, asyncGeneratorPrototype, KeyStrings.GetOrCreate("return"));
        CopyGeneratorMethod(generatorPrototype, asyncGeneratorPrototype, KeyStrings.GetOrCreate("throw"));

        asyncGeneratorPrototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString("AsyncGenerator"), JSPropertyAttributes.ConfigurableReadonlyValue);
        return cache.AsyncGeneratorPrototype = asyncGeneratorPrototype;
    }

    private static void CopyGeneratorMethod(JSObject source, JSObject target, KeyString key)
    {
        var value = source[key];
        if (!value.IsUndefined)
            target.FastAddValue(key, value, JSPropertyAttributes.ConfigurableValue);
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

    public JSGeneratorFunctionV2(JSGeneratorDelegateV2 @delegate, in StringSpan name, in StringSpan code, int length = 0, bool asyncGenerator = false, bool primeOnInvoke = false) : base(null, name, code, length)
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

        CoerceThisOnInvoke = true;
        f = InvokeFunction;
        BasePrototypeObject = GetGeneratorFunctionPrototype(asyncGenerator);
    }

    public override JSValue InvokeFunction(in Arguments a)
    {
        var args = CoerceThisOnInvoke
            ? a.OverrideThis(JSFunction.CoerceNonStrictThis(a.This))
            : a;

        var generator = JSGeneratorBuilder.CreateFromClrV2(new ClrGeneratorV2(this, @delegate, args, asyncGenerator));

        if (primeOnInvoke && generator is IJSGenerator jsGenerator)
            jsGenerator.MoveNext(JSUndefined.Value, out _);

        return generator;
    }

    public override JSValue CreateInstance(in Arguments a)
        => throw Engine.Core.JSEngine.NewTypeError($"{name} is not a constructor");
}
