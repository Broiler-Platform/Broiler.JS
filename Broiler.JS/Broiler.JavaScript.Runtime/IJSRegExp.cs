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
    /// True when the pattern matches in <paramref name="input"/> as <c>exec</c> from lastIndex 0
    /// finds it: anywhere, or at index 0 alone when the pattern is sticky. Runs through
    /// Broiler.Regex, which compiles and matches every RegExp since milestone SJL-11.5, so
    /// callers see the same answer <c>exec</c> would. Replaces the earlier <c>Regex Value</c>
    /// accessor, which exposed the .NET engine directly.
    /// </summary>
    bool IsMatch(string input);
}
