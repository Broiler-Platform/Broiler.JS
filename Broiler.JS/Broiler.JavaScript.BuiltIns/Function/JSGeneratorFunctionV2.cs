using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Function;

public class JSGeneratorFunctionV2 : JSFunction
{
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
        constructor.FastAddValue(KeyStrings.prototype, prototype, JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.constructor, constructor, JSPropertyAttributes.ConfigurableValue);

        return prototype;
    }

    public JSGeneratorFunctionV2(JSGeneratorDelegateV2 @delegate, in StringSpan name, in StringSpan code, int length = 0, bool asyncGenerator = false, bool primeOnInvoke = false) : base(null, name, code, length)
    {
        this.@delegate = @delegate;
        this.asyncGenerator = asyncGenerator;
        this.primeOnInvoke = primeOnInvoke;
        CoerceThisOnInvoke = true;
        f = InvokeFunction;
        BasePrototypeObject = CreateGeneratorFunctionPrototype(asyncGenerator);
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
