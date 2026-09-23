using System;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.ShadowRealm;

[JSClassGenerator("ShadowRealm")]
public partial class JSShadowRealm : JSObject
{
    // The encapsulated child realm. Code passed to evaluate runs against this
    // context's own global environment and intrinsics, isolated from the
    // realm that created the ShadowRealm.
    private readonly JSContext realm;

    // The context that constructed this ShadowRealm, which the child realm is made from. evaluate asks
    // the host whether it may compile a string on this context, and the child forwards every question
    // its own code asks to it (see the constructor).
    private readonly JSContext creatorRealm;

    [JSExport(Length = 0)]
    public JSShadowRealm(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var outer = JSEngine.Current as JSContext;
        creatorRealm = outer;
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

        // Code running INSIDE the child - an eval, a Function constructor or a nested ShadowRealm's
        // evaluate - asks the host on the child context, the one its intrinsics belong to. An embedder
        // never sees that context, so without this its policy would stop at the ShadowRealm boundary:
        // a handler that let evaluate's own string through would never be asked about the strings that
        // string compiles. JSD-0030 requires a child realm to inherit its parent's compilation policy
        // on every route, so each question is forwarded to the creator, and through it to that
        // context's creator for a nested ShadowRealm. A rewrite by the creator's handler is the
        // compile's source and location, as on every other route.
        if (outer != null)
            realm.EvalEvent += (_, e) => ForwardEvalEvent(outer, e);
    }

    private static void ForwardEvalEvent(JSContext creator, EvalEventArgs e)
    {
        var script = e.Script;
        var location = e.Location;
        creator.DispatchEvalEvent(ref script, ref location);
        e.Script = script;
        e.Location = location;
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

        // PerformShadowRealmEval asks the host (HostEnsureCanCompileStrings) before it parses, the
        // same question eval and the Function constructors ask, and it names the ShadowRealm's own
        // realm. The child context has no EvalEvent subscribers and nothing forwards them, so the event
        // is raised on the context that constructed this ShadowRealm, which the child realm was made
        // from. That is the caller whenever the caller built it; a context calling evaluate on another
        // context's ShadowRealm is answered by that other context. Raised while the caller is still
        // current and outside both catches below, so a host's refusal is created in the caller realm
        // and reaches it as the host threw it, rather than rewritten as the parse SyntaxError or the
        // inner-realm TypeError. A location the host sets is the compile's location, as on every
        // other route.
        string location = null;
        (shadowRealm.creatorRealm ?? callerRealm)?.DispatchEvalEvent(ref sourceText, ref location);

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
                compiled = CoreScript.Compile(sourceText, location, codeCache: evalRealm.CodeCache);
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
    public JSValue ImportValue(in Arguments a)
    {
        // §ShadowRealm.prototype.importValue:
        //   1. Let O be this value; perform ? GetShadowRealmRecord(O).
        //   2. Let specifierString be ? ToString(specifier).
        //   3. ... (module load is not yet implemented)
        // Step 2 must run BEFORE the "not implemented" failure — a specifier whose
        // @@toPrimitive / toString / valueOf returns an abrupt completion has to
        // surface that completion (test262 importValue/specifier-tostring).
        if (a.This is not JSShadowRealm)
            throw JSEngine.NewTypeError("ShadowRealm.prototype.importValue called on incompatible receiver");

        var specifier = a.Get1();
        _ = ToStringForImport(specifier);

        throw JSEngine.NewTypeError("ShadowRealm.prototype.importValue is not implemented");
    }

    // ToString(argument) — the part of the spec that may abruptly complete: an Object operand
    // is first ToPrimitive(string) (observing @@toPrimitive / toString / valueOf), and a Symbol
    // (whether produced directly or returned from @@toPrimitive) is a TypeError.
    private static string ToStringForImport(JSValue value)
    {
        if (value is Runtime.JSObject @object)
            value = @object.ToStringPrimitive();

        if (value.IsSymbol)
            throw JSEngine.NewTypeError("Cannot convert a Symbol value to a string");

        return value.StringValue;
    }

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
        // the realm that owns the target). WrappedFunctionCreate step 8: if this
        // copy completes abruptly (e.g. a throwing `length`/`name` getter), the
        // failure is swallowed and a fresh TypeError is thrown in the caller realm
        // rather than letting the target realm's exception escape.
        JSValue lengthValue = JSUndefined.Value;
        JSValue nameValue;
        var outer = JSEngine.Current as JSContext;
        try
        {
            if (srcRealm != null)
                JSEngine.CurrentContext = srcRealm;
            // CopyNameAndLength step 3 does HasOwnProperty(Target, "length"), which runs
            // [[GetOwnProperty]] — a Proxy's getOwnPropertyDescriptor trap. A throwing trap
            // therefore aborts the copy and is re-surfaced as a TypeError below, and "length"
            // is only Get when the own property exists (test262
            // ShadowRealm/.../evaluate/throws-typeerror-wrap-throwing).
            if (!target.GetOwnPropertyDescriptor(CreateString("length")).IsUndefined)
                lengthValue = target[KeyStrings.length];
            nameValue = target[KeyStrings.name];
        }
        catch (Exception)
        {
            JSEngine.CurrentContext = outer;
            throw JSEngine.NewTypeError("ShadowRealm could not copy the wrapped function's name and length");
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

            wrapped = CreateFunction(del, name, null, 0, createPrototype: false);
            if (wrapped is JSFunction wrappedFunction)
            {
                wrappedFunction.FastAddValue(KeyStrings.length, CreateNumber(length), JSPropertyAttributes.ConfigurableReadonlyValue);
                wrappedFunction.FastAddValue(KeyStrings.name, CreateString(name), JSPropertyAttributes.ConfigurableReadonlyValue);
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
