using System.Numerics;
using System.Reflection;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.ECMAScript.Conformance;

namespace Broiler.JavaScript.Vectors.Tests.Runners;

/// <summary>
/// One vector's evaluation in this process: a fresh <see cref="JSContext"/> of the default
/// composition, with the feature flags the shell enables, rendered from the engine's own values.
/// </summary>
/// <remarks>
/// The rendering never asks the engine to format a value: a Number is read as its double, a String
/// as its code units, a BigInt as its integer, and only the runner writes them out. The one thing it
/// asks the engine for is the <c>name</c> of a thrown error's constructor, which is a property read.
/// </remarks>
public static class BroilerJsRealm
{
    /// <summary>The shell's flags (Broiler.JavaScript/Program.cs), which bundle sjl-1-001's decision rule names.</summary>
    public const JavaScriptFeatureFlags Features = JavaScriptFeatureFlags.AllExperimentalEs2026;

    // JSBigInt keeps its integer in an internal field and exposes no public accessor that does not
    // either truncate (BigIntValue) or format (ToString). Reading the field is the test project's
    // way to the value itself; a rename fails the first BigInt vector loudly, not silently.
    private static readonly FieldInfo BigIntValue = typeof(JSBigInt).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(nameof(JSBigInt), "value");

    /// <summary>Evaluates <paramref name="prelude"/>, then <paramref name="script"/>, in one fresh realm, and renders the outcome.</summary>
    public static string Evaluate(string prelude, string script)
    {
        var previous = JSEngine.CurrentContext;
        try
        {
            using var context = new JSContext(experimentalFeatures: Features);
            try
            {
                context.Eval(prelude, "prelude.js");
            }
            catch (Exception ex)
            {
                return Fault("the prelude failed: " + ex.GetType().Name + ": " + ex.Message);
            }

            try
            {
                return "value: " + Render(context.Eval(script, "vector.js"));
            }
            catch (JSException ex)
            {
                JSEngine.CurrentContext = context;
                return RenderThrown(ex.Error);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A host exception escaping the engine is the engine's result, not the runner's fault.
            return OneLine("abort: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            JSEngine.CurrentContext = previous;
        }
    }

    private static string RenderThrown(JSValue? thrown)
    {
        if (thrown is null)
            return Fault("a JSException carried no value");
        if (thrown is not IJSError)
            return "throw: " + Render(thrown);

        var name = thrown[KeyStrings.constructor][KeyStrings.name];
        return name is JSString text
            ? "error: " + text.StringValue
            : Fault("a thrown error's constructor has no string name");
    }

    private static string Render(JSValue value)
    {
        if (value.IsUndefined)
            return "undefined";
        if (value.IsNull)
            return "null";
        if (value.IsObject)
            return "object";
        if (value.IsSymbol)
            return "symbol";
        if (value is JSString text)
            return TypedValue.RenderString(text.StringValue);
        if (value.IsNumber)
            return TypedValue.RenderNumber(value.DoubleValue);
        if (value.IsBoolean)
            return TypedValue.RenderBoolean(value.BooleanValue);
        if (value is JSBigInt bigint)
            return TypedValue.RenderBigInt((BigInteger)BigIntValue.GetValue(bigint)!);
        return Fault("a value of type " + value.GetType().Name + " has no rendering");
    }

    /// <summary>A harness fault, on one line: it matches no outcome and is never classified.</summary>
    internal static string Fault(string description) => OneLine("fault: " + description);

    internal static string OneLine(string text) => text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
