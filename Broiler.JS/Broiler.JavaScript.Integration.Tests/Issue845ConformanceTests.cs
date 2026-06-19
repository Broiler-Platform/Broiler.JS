using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845 — three further
// clusters:
//   * Object.prototype.propertyIsEnumerable boxes a primitive receiver (ToObject), so a
//     string primitive's index char is observable (Problem 77).
//   * A u/v-mode regex `\u{…}` escape must be a non-empty in-range hex run, else SyntaxError.
//   * A catch parameter that is an always-reserved word is a SyntaxError.
public class Issue845ConformanceTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Evaluates `code` and returns the thrown error's constructor name, or "no throw".
    private static string ErrorOf(string code)
        => Eval($"(function(){{ try {{ {code}; return 'no throw'; }} catch (e) {{ return e.constructor.name; }} }})()");

    // ---- propertyIsEnumerable boxes a primitive receiver (Problem 77) ----

    [Theory]
    [InlineData("'s'.propertyIsEnumerable(0)", "true")]
    [InlineData("'s'.propertyIsEnumerable(1)", "false")]
    [InlineData("'s'.propertyIsEnumerable('length')", "false")]
    [InlineData("(5).propertyIsEnumerable('x')", "false")]
    [InlineData("({ x: 1 }).propertyIsEnumerable('x')", "true")]
    public void PropertyIsEnumerableBoxesPrimitive(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    [InlineData("Object.prototype.propertyIsEnumerable.call(null, 'x')")]
    [InlineData("Object.prototype.propertyIsEnumerable.call(undefined, 'x')")]
    public void PropertyIsEnumerableRejectsNullUndefined(string code)
        => Assert.Equal("TypeError", ErrorOf(code));

    // ---- u/v-mode braced unicode escape validation ----

    [Theory]
    [InlineData(@"/\u{110000}/u")]                              // > U+10FFFF
    [InlineData(@"/\u{00110000}/u")]                            // leading zeros, > U+10FFFF
    [InlineData(@"/\u{100000000000000000000000000000}/u")]      // overflowing digit run
    [InlineData(@"/\u{G}/u")]                                   // non-hex
    [InlineData(@"/\u{0.0}/u")]                                 // non-hex
    [InlineData(@"/\u{}/u")]                                    // empty
    [InlineData(@"/\u{/u")]                                     // unterminated
    public void InvalidBracedUnicodeEscapeIsSyntaxError(string source)
        => Assert.Equal("SyntaxError", ErrorOf($"eval({System.Text.Json.JsonSerializer.Serialize(source)})"));

    [Theory]
    [InlineData(@"/\u{41}/u", "A")]
    [InlineData(@"/\u{10FFFF}/u", "\U0010FFFF")]
    [InlineData(@"/\u{1F438}/u", "\U0001F438")]
    public void ValidBracedUnicodeEscapeMatches(string pattern, string subject)
        => Assert.Equal("true", Eval($"({pattern}).test({System.Text.Json.JsonSerializer.Serialize(subject)})"));

    // ---- Catch parameter rejects always-reserved words ----

    [Theory]
    [InlineData("class")]
    [InlineData("const")]
    [InlineData("enum")]
    [InlineData("export")]
    [InlineData("extends")]
    [InlineData("import")]
    [InlineData("super")]
    public void CatchParameterRejectsReservedWord(string word)
        => Assert.Equal("SyntaxError", ErrorOf($"eval('try {{}} catch ({word}) {{}}')"));

    [Theory]
    [InlineData("try {} catch (e) {}")]
    [InlineData("try {} catch ({ e }) {}")]
    [InlineData("try {} catch (let) {}")]   // contextual, valid in sloppy code
    public void CatchParameterAcceptsValidBinding(string code)
        => Assert.Equal("no throw", ErrorOf($"eval({System.Text.Json.JsonSerializer.Serialize(code)})"));
}
