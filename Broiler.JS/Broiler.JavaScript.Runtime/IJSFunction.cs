namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Abstraction over a JavaScript function value, allowing Runtime
/// types to invoke functions without depending on the concrete
/// <c>JSFunction</c> class in Core.
/// </summary>
public interface IJSFunction
{
    /// <summary>Invokes this function with the specified arguments.</summary>
    /// <param name="a">The arguments to pass to the function.</param>
    /// <returns>The return value produced by the function.</returns>
    JSValue InvokeFunction(in Arguments a);

    /// <summary>
    /// Gets or sets the underlying <see cref="JSFunctionDelegate"/> that implements
    /// this function's invocation logic.
    /// </summary>
    JSFunctionDelegate Delegate { get; set; }

    /// <summary>
    /// Gets the prototype object associated with this function.
    /// </summary>
    JSValue Prototype { get; }

    /// <summary>
    /// True while this is a user-compiled anonymous function that still carries the
    /// default empty name and is therefore eligible for NamedEvaluation (ES2026
    /// §10.2.9 SetFunctionName) to adopt a binding/property name. It is cleared once a
    /// name has been inferred or explicitly suppressed, so a value that already has a
    /// name (a named function, a bound function, or one already named) is never
    /// renamed. Anonymous user functions report <c>name === ""</c>, so the public
    /// "name" property string can no longer distinguish this state on its own.
    /// </summary>
    bool IsAnonymousNamePending { get; set; }
}
