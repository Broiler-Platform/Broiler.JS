using System;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.ShadowRealm;

[JSClassGenerator("ShadowRealm")]
public partial class JSShadowRealm : JSObject
{
    // The encapsulated child realm. Code passed to evaluate runs against this
    // context's own global environment and intrinsics, isolated from the
    // realm that created the ShadowRealm.
    private readonly JSContext realm;

    [JSExport(Length = 0)]
    public JSShadowRealm(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var outer = JSEngine.Current as JSContext;
        try
        {
            realm = new JSContext(
                outer?.synchronizationContext,
                outer?.ExperimentalFeatures ?? JavaScriptFeatureFlags.None);
        }
        finally
        {
            // Creating a JSContext sets it as the current context; restore the
            // realm that is actually executing.
            if (outer != null)
                JSEngine.CurrentContext = outer;
        }
    }

    [JSExport(Length = 1)]
    public JSValue Evaluate(in Arguments a)
    {
        if (a.This is not JSShadowRealm shadowRealm || shadowRealm.realm == null)
            throw JSEngine.NewTypeError("ShadowRealm.prototype.evaluate called on incompatible receiver");

        var sourceTextValue = a.Get1();
        if (!sourceTextValue.IsString)
            throw JSEngine.NewTypeError("ShadowRealm.prototype.evaluate expects a string");

        var sourceText = sourceTextValue.StringValue;
        var callerRealm = JSEngine.Current as JSContext;
        var evalRealm = shadowRealm.realm;

        JSValue result;
        var outer = JSEngine.Current as JSContext;
        try
        {
            JSEngine.CurrentContext = evalRealm;

            // PerformShadowRealmEval distinguishes two failure phases:
            //   * ParseScript / early-error failure  -> throw a SyntaxError
            //   * abrupt completion while evaluating  -> throw a (wrapping) TypeError
            // Both errors must be created in the CALLER realm, so reset the current
            // context before constructing them.
            JSFunctionDelegate compiled;
            try
            {
                compiled = CoreScript.Compile(sourceText, codeCache: evalRealm.CodeCache);
            }
            catch (Exception)
            {
                JSEngine.CurrentContext = outer;
                throw JSEngine.NewSyntaxError("ShadowRealm.prototype.evaluate: the provided source text could not be parsed");
            }

            try
            {
                result = JSTailCall.Resolve(compiled(new Arguments(evalRealm)));
            }
            catch (Exception)
            {
                JSEngine.CurrentContext = outer;
                throw JSEngine.NewTypeError("ShadowRealm.prototype.evaluate threw inside the inner realm");
            }
        }
        finally
        {
            JSEngine.CurrentContext = outer;
        }

        return GetWrappedValue(callerRealm, evalRealm, result);
    }

    [JSExport(Length = 2)]
    public JSValue ImportValue(in Arguments a) =>
        throw JSEngine.NewTypeError("ShadowRealm.prototype.importValue is not implemented");

    /// <summary>
    /// Implements the spec's GetWrappedValue abstract operation. Primitives are
    /// returned unchanged; callable objects are wrapped into <paramref name="destRealm"/>;
    /// non-callable objects raise a TypeError.
    /// </summary>
    private static JSValue GetWrappedValue(JSContext destRealm, JSContext srcRealm, JSValue value)
    {
        if (value == null)
            return JSUndefined.Value;

        if (!value.IsObject)
            return value;

        if (!value.IsFunction)
            throw JSEngine.NewTypeError("ShadowRealm cannot wrap non-callable objects");

        return WrappedFunctionCreate(destRealm, srcRealm, value);
    }

    /// <summary>
    /// Implements WrappedFunctionCreate: builds a callable exotic function living
    /// in <paramref name="destRealm"/> that forwards calls to <paramref name="target"/>
    /// (which lives in <paramref name="srcRealm"/>), wrapping arguments and return
    /// values across the realm boundary.
    /// </summary>
    private static JSValue WrappedFunctionCreate(JSContext destRealm, JSContext srcRealm, JSValue target)
    {
        // CopyNameAndLength: read length/name from the target (its getters run in
        // the realm that owns the target).
        JSValue lengthValue;
        JSValue nameValue;
        var outer = JSEngine.Current as JSContext;
        try
        {
            if (srcRealm != null)
                JSEngine.CurrentContext = srcRealm;
            lengthValue = target[KeyStrings.length];
            nameValue = target[KeyStrings.name];
        }
        finally
        {
            JSEngine.CurrentContext = outer;
        }

        double length = 0;
        if (lengthValue.IsNumber)
        {
            var d = lengthValue.DoubleValue;
            if (double.IsPositiveInfinity(d))
                length = double.PositiveInfinity;
            else if (double.IsNegativeInfinity(d) || double.IsNaN(d))
                length = 0;
            else
                length = Math.Max(Math.Truncate(d), 0);
        }

        var name = nameValue.IsString ? nameValue.StringValue : string.Empty;

        JSFunctionDelegate del = (in Arguments a) =>
            WrappedFunctionCall(destRealm, srcRealm, target, in a);

        JSValue wrapped;
        var outer2 = JSEngine.Current as JSContext;
        try
        {
            if (destRealm != null)
                JSEngine.CurrentContext = destRealm;

            wrapped = JSValue.CreateFunction(del, name, null, 0, createPrototype: false);
            if (wrapped is JSFunction wrappedFunction)
            {
                wrappedFunction.FastAddValue(KeyStrings.length, JSValue.CreateNumber(length), JSPropertyAttributes.ConfigurableReadonlyValue);
                wrappedFunction.FastAddValue(KeyStrings.name, JSValue.CreateString(name), JSPropertyAttributes.ConfigurableReadonlyValue);
            }
        }
        finally
        {
            JSEngine.CurrentContext = outer2;
        }

        return wrapped;
    }

    private static JSValue WrappedFunctionCall(JSContext destRealm, JSContext srcRealm, JSValue target, in Arguments a)
    {
        // Wrap the incoming arguments (which live in destRealm) into srcRealm.
        var count = a.Length;
        var wrappedArgs = new JSValue[count];
        for (var i = 0; i < count; i++)
            wrappedArgs[i] = GetWrappedValue(srcRealm, destRealm, a.GetAt(i));

        JSValue result = JSUndefined.Value;
        var threw = false;
        var outer = JSEngine.Current as JSContext;
        try
        {
            if (srcRealm != null)
                JSEngine.CurrentContext = srcRealm;

            result = JSTailCall.Resolve(target.InvokeFunction(new Arguments(JSUndefined.Value, wrappedArgs)));
        }
        catch (Exception)
        {
            threw = true;
        }
        finally
        {
            JSEngine.CurrentContext = outer;
        }

        if (threw)
            throw JSEngine.NewTypeError("ShadowRealm wrapped function threw");

        // Wrap the return value (which lives in srcRealm) back into destRealm.
        return GetWrappedValue(destRealm, srcRealm, result);
    }
}
