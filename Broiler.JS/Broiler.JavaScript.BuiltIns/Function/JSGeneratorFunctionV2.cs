using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Function;

public class JSGeneratorFunctionV2 : JSFunction
{
    readonly JSGeneratorDelegateV2 @delegate;
    readonly bool primeOnInvoke;

    private static JSObject CreateGeneratorFunctionPrototype(bool asyncGenerator)
    {
        var prototype = new JSObject();
        if ((Engine.Core.JSEngine.Current as Engine.IJSExecutionContext)?.FunctionPrototype is JSObject functionPrototype)
            prototype.BasePrototypeObject = functionPrototype;

        var constructorName = asyncGenerator ? "AsyncGeneratorFunction" : "GeneratorFunction";
        prototype.FastAddValue(KeyStrings.constructor, JSValue.CreateFunction((in Arguments a) =>
        {
            var created = JSFunction.Constructor(in a);
            if (created is JSFunction function)
                function.prototype = null;

            return created;
        }, constructorName, $"function {constructorName}() {{ [native code] }}", 1, createPrototype: false), JSPropertyAttributes.ConfigurableValue);

        return prototype;
    }

    public JSGeneratorFunctionV2(JSGeneratorDelegateV2 @delegate, in StringSpan name, in StringSpan code, bool asyncGenerator, bool primeOnInvoke = false) : base(null, name, code)
    {
        this.@delegate = @delegate;
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

        var generator = JSGeneratorBuilder.CreateFromClrV2(new ClrGeneratorV2(this, @delegate, args));

        if (primeOnInvoke && generator is IJSGenerator jsGenerator)
            jsGenerator.MoveNext(JSUndefined.Value, out _);

        return generator;
    }

    public override JSValue CreateInstance(in Arguments a)
        => throw Engine.Core.JSEngine.NewTypeError($"{name} is not a constructor");
}
