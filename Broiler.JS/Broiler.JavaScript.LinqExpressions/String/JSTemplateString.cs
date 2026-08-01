using Broiler.JavaScript.Runtime;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Broiler.JavaScript.LinqExpressions.String;

/// <summary>
/// Accumulates the quasis and substitutions of a template literal and joins them once.
/// </summary>
/// <remarks>
/// The parts are held as strings and concatenated in a single pass rather than appended
/// into a <see cref="System.Text.StringBuilder"/> and materialised from it: the builder
/// carries a full copy of the result while its <c>ToString</c> allocates a second, so a
/// template evaluated over a large substitution paid twice the result's size at peak.
/// <c>string.Concat</c> sizes the result from the parts and fills it in one allocation.
/// A substitution is still stringified as the template reaches it, so a user
/// <c>toString</c> still runs in evaluation order.
/// </remarks>
public class JSTemplateString
{
    private readonly List<string> _parts;

    /// <param name="size">
    /// Combined length of the template's literal parts — a lower bound on the result
    /// rather than a part count, so it is not used as the list's capacity. The parameter
    /// is kept because the compiler emits this constructor
    /// (<c>JSTemplateStringBuilder.New</c>).
    /// </param>
    public JSTemplateString(int size) => _parts = new List<string>(4);

    public void Add(string t) => _parts.Add(t);

    public void Add(JSValue value) => _parts.Add(value.ToString());

    public JSTemplateString AddQuasi(string text)
    {
        _parts.Add(text);
        return this;
    }

    public JSTemplateString AddExpression(JSValue value)
    {
        _parts.Add(value.ToString());
        return this;
    }

    public override string ToString() => string.Concat(CollectionsMarshal.AsSpan(_parts));

    public JSValue ToJSString() => JSValue.CreateString(ToString());
}
