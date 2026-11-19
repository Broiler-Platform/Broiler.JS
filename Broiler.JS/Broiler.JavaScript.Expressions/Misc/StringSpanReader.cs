// Moved from Broiler.JavaScript.Ast with its namespace at window W2 of milestone SJL-14 (decision D50: a non-tree type,
// whose new home is Broiler.JavaScript.Expressions); Ast keeps a type forwarder to it, and it is not marked obsolete. It is
// compiled in a nullable context, as Ast compiled it, so that its public signatures read as they did.
#nullable enable

using System.IO;

namespace Broiler.JavaScript.Ast.Misc;

public class StringSpanReader(StringSpan span) : TextReader
{
    private int index = 0;

    public override int Peek()
    {
        if (index >= span.Length)
            return -1;

        return span[index];
    }

    public override int Read()
    {
        if (index >= span.Length)
            return -1;

        return span[index++];
    }

    public override string ReadToEnd() => span.Substring(index).Value ?? string.Empty;
}
