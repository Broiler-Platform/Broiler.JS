namespace Broiler.JavaScript.Ast.Misc;

/// <summary>
/// A position in source text. Both coordinates are 1-based: the first line is line 1, and the
/// first character of every line is column 1. Every ECMAScript LineTerminator (LF, CR, U+2028,
/// U+2029, with CRLF counted once) starts a new line. Columns count UTF-16 code units.
/// </summary>
public readonly struct SpanLocation(int line, int column)
{
    public readonly int Line = line;
    public readonly int Column = column;

    public override string ToString() => $"{Line}, {Column}";
}
