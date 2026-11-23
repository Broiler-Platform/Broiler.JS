using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// The keyword classifier, as a facade (window W2 of milestone SJL-14; decision D50, <c>facade-obsolete</c>): it keeps its
/// members and answers as the bridge reads a name's keyword (<c>BridgeTokens.NameFacts</c>): a keyword is a name that spells
/// one of <see cref="FastKeywords"/>' members.
/// </summary>
/// <remarks>
/// Until W2 its answer was a table over the words; W2 deletes the table (SJL-14.5's deletion list) and keeps no
/// remnant: the <c>m</c> suffix reaches the library through its lexer extension (decision D17), which the table never
/// served.
/// </remarks>
[System.Obsolete(ObsoleteMarks.Parser, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class FastKeywordMap
{
    public static FastKeywordMap Instance { get; } = new();

    protected FastKeywordMap() { }

    /// <summary>Whether <paramref name="key"/> spells a keyword, and which.</summary>
    public virtual bool IsKeyword(in StringSpan key, out FastKeywords keyword)
    {
        if (key.Length > 0 && char.IsAsciiLetterLower(key[0])
            && System.Enum.TryParse(key.Value, ignoreCase: false, out keyword) && keyword != FastKeywords.none)
            return true;

        keyword = FastKeywords.none;
        return false;
    }
}
