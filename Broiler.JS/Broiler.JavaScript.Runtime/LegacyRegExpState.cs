namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Per-realm storage for the legacy (Annex B.2.4) static RegExp properties:
/// <c>RegExp.input</c> / <c>$_</c>, <c>RegExp.lastMatch</c> / <c>$&amp;</c>,
/// <c>RegExp.lastParen</c> / <c>$+</c>, <c>RegExp.leftContext</c> / <c>$`</c>,
/// <c>RegExp.rightContext</c> / <c>$'</c> and <c>RegExp.$1</c>..<c>RegExp.$9</c>.
///
/// These hold the result of the most recent successful match performed by the
/// built-in RegExpBuiltinExec (which also backs String.prototype.match / replace /
/// search / split). They default to the empty string until the first match.
/// </summary>
public sealed class LegacyRegExpState
{
    // §B.2.4 UpdateLegacyRegExpStaticProperties: the nine numbered capture slots.
    private readonly string[] parens = { "", "", "", "", "", "", "", "", "" };

    public string Input { get; set; } = "";
    public string LastMatch { get; private set; } = "";
    public string LastParen { get; private set; } = "";
    public string LeftContext { get; private set; } = "";
    public string RightContext { get; private set; } = "";

    /// <summary>Reads <c>RegExp.$n</c> for n in 1..9 (empty string otherwise).</summary>
    public string Paren(int n) => n >= 1 && n <= 9 ? parens[n - 1] : "";

    /// <summary>
    /// §B.2.4 UpdateLegacyRegExpStaticProperties: records a successful match of
    /// <paramref name="input"/> spanning [<paramref name="startIndex"/>,
    /// <paramref name="endIndex"/>) with the given 1-based captured substrings
    /// (a <c>null</c> entry denotes a capture that did not participate).
    /// </summary>
    public void Update(string input, int startIndex, int endIndex, string[] capturedValues)
    {
        var len = capturedValues?.Length ?? 0;
        var n = len < 9 ? len : 9;

        Input = input;
        LastMatch = input.Substring(startIndex, endIndex - startIndex);
        LastParen = n > 0 ? capturedValues[n - 1] ?? "" : "";
        LeftContext = input.Substring(0, startIndex);
        RightContext = input.Substring(endIndex);

        for (var i = 0; i < 9; i++)
            parens[i] = i < n ? capturedValues[i] ?? "" : "";
    }
}
