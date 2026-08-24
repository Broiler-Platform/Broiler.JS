namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Abstraction over JavaScript RegExp objects, allowing assemblies
/// to detect and inspect regular expressions without depending on the
/// concrete <c>JSRegExp</c> class in BuiltIns.
/// </summary>
public interface IJSRegExp
{
    /// <summary>Gets the regular expression pattern string.</summary>
    string Pattern { get; }

    /// <summary>Gets the flags string (e.g. "gi").</summary>
    string Flags { get; }

    /// <summary>
    /// True when the pattern matches anywhere in <paramref name="input"/>. Runs through
    /// whichever engine the RegExp is bound to (the Broiler.Regex backend for a routed
    /// gap pattern, the .NET translation otherwise), so callers see the same answer
    /// <c>exec</c> would. Replaces the earlier <c>Regex Value</c> accessor, which exposed
    /// the .NET engine directly and was wrong for a routed pattern (and null once a
    /// pattern the translator cannot represent is routed).
    /// </summary>
    bool IsMatch(string input);
}
