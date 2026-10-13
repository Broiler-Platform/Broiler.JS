using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Parses strings into numbers.
/// </summary>
public static class NumberParser
{
    // Since SJL-3.5 the conversions are the shared library's (Broiler.ECMAScript.Algorithms), and
    // this class keeps only its public members, as forwarders (decision SJL-D-25).

    public static bool TryCoerceToUInt32(in StringSpan input, out uint value) {

        if(input.Length == 0)
        {
            value = 0;
            return false;
        }

        char ch = input[0];
        if (ch == '0')
        {
            if (input.Equals("0"))
            {
                value = 0;
                return true;
            }
        
            var next = input[1];
            if (char.IsDigit(next))
            {
                value = 0;
                return false;
            }
        }

        var d = CoerceToNumber(in input);
        if (d == 0 && NumberParsing.TrimStrWhiteSpace(input.AsSpan(), TrimEnds.Both).IsEmpty)
        {
            value = 0;
            return false;
        }
        
        if (d < uint.MaxValue && (d % 1 ) ==0)
        {
            value = (uint)d;
            return true;
        }
        
        value = 0;
        return false;
    }

    /// <summary>
    /// Whether a key is an array index: the canonical string of an integer from 0 to 2^32 - 2.
    /// </summary>
    public static bool TryGetArrayIndex(in StringSpan input, out uint value) => NumericKeys.TryParseArrayIndex(input.AsSpan(), out value);

    /// <summary>
    /// Converts a string to a number (used in type coercion): StringToNumber.
    /// </summary>
    /// <returns> The result of parsing the string as a number. </returns>
    public static double CoerceToNumber(in StringSpan input) => NumberParsing.StringToNumber(input.AsSpan());
}
