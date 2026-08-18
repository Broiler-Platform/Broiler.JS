using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    /// <summary>
    /// Builds the array an ArrayLiteral of nothing but constants denotes, from the encoded
    /// description of that literal the compiler produced (see
    /// <c>FastCompiler.TryEncodeConstantArrayLiteral</c> for the grammar).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The point is what is NOT emitted. An ArrayLiteral otherwise compiles to a ListInit — one
    /// <c>Add</c> call site per element, plus a nested one per nested literal — and that is the
    /// whole cost of a literal used as a data table: test262's
    /// staging/sm/String/string-upper-lower-mapping is 65 536 two-element arrays, whose IL runs
    /// to megabytes inside a single method, and the runtime spends over 30 CPU-seconds on it
    /// before the first assertion runs. Here the same literal is one <c>ldstr</c> and one call.
    /// </para>
    /// <para>
    /// A fresh array (and a fresh nested array) is built per evaluation, as ArrayLiteral
    /// Evaluation requires. Only the leaves are shared, and only ever primitives — a string or
    /// number JSValue has no mutable state and no identity a program can observe, so one
    /// instance may serve every evaluation. The decode itself is memoised on the encoded string,
    /// which the emitted <c>ldstr</c> hands back as the same instance on every execution.
    /// </para>
    /// </remarks>
    public static JSValue FromTemplate(string encoded)
        => Instantiate(templates.GetValue(encoded, static text => Decode(text)));

    // Keyed on the encoded string the call site loads, so a template lives exactly as long as
    // the code that can still evaluate it.
    private static readonly ConditionalWeakTable<string, object[]> templates = new();

    // An array is an object[]; a hole is a null element; every other element is either a
    // JSValue leaf or a nested object[].
    private static JSArray Instantiate(object[] template)
    {
        var array = new JSArray();
        foreach (var item in template)
        {
            // Add(null) is the elision: it advances the length without creating an element.
            array.Add(item is object[] nested ? Instantiate(nested) : item as JSValue);
        }

        return array;
    }

    private static object[] Decode(string encoded)
    {
        var position = 0;
        var decoded = DecodeValue(encoded, ref position);
        if (position != encoded.Length || decoded is not object[] array)
            throw new ArgumentException("Malformed constant array literal template", nameof(encoded));

        return array;
    }

    private static object DecodeValue(string encoded, ref int position)
    {
        var marker = encoded[position++];
        switch (marker)
        {
            case 'a':
            {
                var count = DecodeLength(encoded, ref position);
                var items = new object[count];
                for (var i = 0; i < count; i++)
                    items[i] = DecodeValue(encoded, ref position);

                return items;
            }

            case 's':
            {
                var length = DecodeLength(encoded, ref position);
                var value = new Broiler.JavaScript.BuiltIns.String.JSString(encoded.Substring(position, length));
                position += length;
                return value;
            }

            case 'd':
            {
                // The IEEE-754 bits, not a formatted number: exact for every double a
                // NumericLiteral can produce, negative zero included.
                var bits = long.Parse(encoded.AsSpan(position, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                position += 16;
                return JSNumber.CreateLiteral(BitConverter.Int64BitsToDouble(bits));
            }

            case 't':
                return JSBoolean.True;

            case 'f':
                return JSBoolean.False;

            case 'n':
                return JSNull.Value;

            case 'h':
                return null;

            default:
                throw new ArgumentException($"Unknown constant array literal template marker '{marker}'", nameof(encoded));
        }
    }

    private static int DecodeLength(string encoded, ref int position)
    {
        var length = 0;
        while (encoded[position] != ':')
            length = (length * 10) + (encoded[position++] - '0');

        position++;
        return length;
    }
}
