using System.Globalization;
using System.Text;

namespace ParseCorpus;

/// <summary>
/// The escaping every report and dump uses, so that each is ASCII, one record per line, and
/// independent of the culture and of the encoder's handling of lone surrogates.
/// </summary>
internal static class Text
{
    /// <summary>
    /// A report field: backslash, tab, line feed and carriage return as <c>\\</c>, <c>\t</c>,
    /// <c>\n</c> and <c>\r</c>; every other character outside printable ASCII as <c>\uXXXX</c>
    /// (a UTF-16 code unit, upper-case hex). A null or empty value is <c>-</c>, the manifest's
    /// placeholder for an absent value.
    /// </summary>
    internal static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "-";

        var builder = new StringBuilder(value.Length);
        Escape(builder, value);
        return builder.ToString();
    }

    /// <summary>A quoted value in a dump: <c>"</c> and the field escapes, with <c>\"</c> for a quote.</summary>
    internal static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        Escape(builder, value, quote: true);
        builder.Append('"');
        return builder.ToString();
    }

    private static void Escape(StringBuilder builder, string value, bool quote = false)
    {
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '"' when quote:
                    builder.Append("\\\"");
                    break;
                default:
                    if (character < 0x20 || character > 0x7E)
                        builder.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(character);
                    break;
            }
        }
    }
}
