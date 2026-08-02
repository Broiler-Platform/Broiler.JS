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
/// <remarks>
/// <para>
/// The three substrings are computed on READ, not on update, and that is the whole point of
/// this class's shape. <c>LeftContext</c> and <c>RightContext</c> partition the subject around
/// the match, so materializing them eagerly copies the ENTIRE subject on every successful
/// match — and <c>Update</c> is called from <c>RegExpBuiltinExec</c>, which
/// <c>RegExp.prototype[@@replace]</c> invokes once per match. A global replace over a subject
/// with <em>n</em> matches therefore allocated <em>n</em> copies of the subject: measured at
/// ~42 KB per match on a 40 KB subject, 210 MB for one <c>replace</c> call, and quadratic in
/// the number of matches.
/// </para>
/// <para>
/// Nothing about that work is required until somebody reads one of these properties, and
/// almost nothing does — they are a deprecated Annex B surface kept for web compatibility.
/// Holding the subject plus two indices costs a reference and eight bytes and defers the copy
/// to a reader that may never arrive.
/// </para>
/// </remarks>
public sealed class LegacyRegExpState
{
    // §B.2.4 UpdateLegacyRegExpStaticProperties: the nine numbered capture slots.
    private readonly string[] parens = ["", "", "", "", "", "", "", "", ""];

    /// <summary>
    /// The subject of the last successful match with that match's span, held together in one
    /// immutable object so <see cref="Update"/> publishes them with a single reference write.
    /// </summary>
    /// <remarks>
    /// Three separate fields would be three writes, and a reader on another thread could pair a
    /// new subject with the previous match's indices and slice outside it. The eager version had
    /// no such hazard — its fields were independent, already-built strings, so a torn read
    /// returned a stale string rather than throwing — and deferring the slice must not introduce
    /// one.
    /// </remarks>
    private sealed record Match(string Subject, int Start, int End)
    {
        public static readonly Match None = new("", 0, 0);

        public string Slice(int start, int end)
            => end <= start ? "" : Subject.Substring(start, end - start);
    }

    private Match match = Match.None;

    public string Input { get; set; } = "";
    public string LastParen { get; private set; } = "";

    /// <summary>The matched text, <c>RegExp.lastMatch</c> / <c>$&amp;</c>.</summary>
    public string LastMatch
    {
        get
        {
            var m = match;
            return m.Slice(m.Start, m.End);
        }
    }

    /// <summary>The subject before the match, <c>RegExp.leftContext</c> / <c>$`</c>.</summary>
    public string LeftContext
    {
        get
        {
            var m = match;
            return m.Slice(0, m.Start);
        }
    }

    /// <summary>The subject after the match, <c>RegExp.rightContext</c> / <c>$'</c>.</summary>
    public string RightContext
    {
        get
        {
            var m = match;
            return m.Slice(m.End, m.Subject.Length);
        }
    }

    /// <summary>Reads <c>RegExp.$n</c> for n in 1..9 (empty string otherwise).</summary>
    public string Paren(int n) => n >= 1 && n <= 9 ? parens[n - 1] : "";

    /// <summary>
    /// §B.2.4 UpdateLegacyRegExpStaticProperties: records a successful match of
    /// <paramref name="input"/> spanning [<paramref name="startIndex"/>,
    /// <paramref name="endIndex"/>) with the given 1-based captured substrings
    /// (a <c>null</c> entry denotes a capture that did not participate).
    /// </summary>
    /// <remarks>
    /// Records the span rather than the substrings. The captures are stored as given, because
    /// the caller already holds them as separate strings — there is no whole-subject copy to
    /// defer there, and deferring would mean holding the match object alive instead.
    /// </remarks>
    public void Update(string input, int startIndex, int endIndex, string[] capturedValues)
    {
        var len = capturedValues?.Length ?? 0;
        var n = len < 9 ? len : 9;

        Input = input;
        match = new Match(input ?? "", startIndex, endIndex);
        LastParen = n > 0 ? capturedValues[n - 1] ?? "" : "";

        for (var i = 0; i < 9; i++)
            parens[i] = i < n ? capturedValues[i] ?? "" : "";
    }
}
