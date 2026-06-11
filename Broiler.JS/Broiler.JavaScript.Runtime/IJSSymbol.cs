namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Abstraction over a JavaScript Symbol value, allowing Runtime
/// types to accept symbol-typed keys without depending on the
/// concrete <c>JSSymbol</c> class in Core.
/// </summary>
public interface IJSSymbol
{
    /// <summary>Gets the internal numeric key that uniquely identifies this symbol.</summary>
    uint Key { get; }

    /// <summary>
    /// True when the symbol was created with no description (`Symbol()`), as opposed to
    /// an empty-string description (`Symbol("")`). The two are indistinguishable via
    /// ToString() (both yield "") but differ for SetFunctionName: an undefined
    /// description gives the name "", an empty-string description gives "[]".
    /// </summary>
    bool DescriptionIsUndefined { get; }
}
