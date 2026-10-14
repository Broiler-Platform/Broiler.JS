using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// SerializeJSONObject and SerializeJSONArray break an indented line with LINE FEED (U+000A) alone,
// on every platform. The serializer wrote the platform's line terminator, CR LF on Windows; test262's
// JSON tests run on Linux, where the two agree, so only a Windows run of these cases shows it. Each
// case compares the whole text, so a CR anywhere fails it.
public class JsonStringifyLineTerminatorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // An object's member and its closing brace.
    [InlineData("JSON.stringify({ a: 1 }, null, 2)", "{\n  \"a\": 1\n}")]
    // An array's element and its closing bracket.
    [InlineData("JSON.stringify([1, 2], null, 2)", "[\n  1,\n  2\n]")]
    // Nested, with a String gap; empty ones stay on one line.
    [InlineData("JSON.stringify({ a: [1, { b: 2 }], c: [], d: {} }, null, '\\t')",
        "{\n\t\"a\": [\n\t\t1,\n\t\t{\n\t\t\t\"b\": 2\n\t\t}\n\t],\n\t\"c\": [],\n\t\"d\": {}\n}")]
    // A CR LF in the data is escaped, not a line break.
    [InlineData("JSON.stringify({ a: 'x\\r\\ny' }, null, 1)", "{\n \"a\": \"x\\r\\ny\"\n}")]
    public void Indented_output_breaks_lines_with_line_feed(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));
}
