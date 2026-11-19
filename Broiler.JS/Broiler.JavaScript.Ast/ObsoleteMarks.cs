namespace Broiler.JavaScript;

/// <summary>
/// The obsolete marks of the adapter packages <c>Broiler.JavaScript.Ast</c> and <c>Broiler.JavaScript.Parser</c> (window
/// W2 of milestone SJL-14; decision D50, SJL-D-12 (i) and SJL-D-25 (b)): one diagnostic identifier for every mark, which
/// the repository root's <c>Directory.Build.props</c> suppresses with one <c>NoWarn</c> until SJL-15.5, and one removal
/// version, <c>1.0.0</c>, which each message names. <c>FastTokenStream.UndoMark</c> keeps its own mark of SJL-12.5,
/// without a removal version (JSD-0080's decision D20).
/// </summary>
internal static class ObsoleteMarks
{
    /// <summary>The diagnostic identifier every mark carries.</summary>
    internal const string DiagnosticId = "BJS0001";

    /// <summary>The mark of <c>Broiler.JavaScript.Ast</c>'s tree types.</summary>
    internal const string Ast =
        "Broiler.JavaScript.Ast's tree is built by an adapter over the shared syntax library's tree (Broiler.ECMAScript.Frontend, "
        + "namespace Broiler.ECMAScript.Syntax) since milestone SJL-14, and is removed in version 1.0.0.";

    /// <summary>The mark of <c>Broiler.JavaScript.Parser</c>'s public types.</summary>
    internal const string Parser =
        "Broiler.JavaScript.Parser is a facade over the shared syntax library (Broiler.ECMAScript.Frontend; parse with "
        + "Broiler.ECMAScript.Syntax.SyntaxParser) since milestone SJL-14, and is removed in version 1.0.0.";
}
