using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The Unicode area's corrections and policies (milestone SJL-7.5), each with the row of bundle sjl-7-001's
// correspondence from code (Broiler.VM's docs/evidence/sjl-7-001/correspondence.md) it confirms. The shared
// corpus (conformance/vectors/unicode) holds most of them; these cases hold the paths it does not, and a
// sample of each row it does. Each failed before the cut-over and passes after it.
public class SharedUnicodeTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    internal static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    internal static string ErrorName(string expression)
        => Eval($"try {{ {expression}; 'no error' }} catch (e) {{ e.constructor.name }}");

    // ---- lexical classes: identifiers and white space (rows I1, I3, I4, W1) ----------------------------

    // I3: a group name is a RegExpIdentifierName, whose characters are IdentifierStartChar and
    // IdentifierPartChar: U+2E2F (Pattern_Syntax) is refused, and Unicode 17's letters and marks admitted.
    [Theory]
    [InlineData("new RegExp('(?<\\u2E2Fx>.)')")]
    [InlineData("new RegExp('(?<x\\u2E2F>.)')")]
    [InlineData("new RegExp('(?<\\u{2E2F}>.)', 'u')")]
    public void A_group_name_with_pattern_syntax_is_a_SyntaxError(string expression)
        => Assert.Equal("SyntaxError", ErrorName(expression));

    [Theory]
    [InlineData("new RegExp('(?<\\u{16EA0}>.)', 'u').exec('a').groups['\\u{16EA0}']", "a")]
    [InlineData("new RegExp('(?<a\\u{11B60}>.)', 'u').exec('b').groups['a\\u{11B60}']", "b")]
    [InlineData("/(?<\\u0870>.)/.exec('c').groups['\\u0870']", "c")]
    public void A_group_name_of_Unicode_17_is_admitted(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // W1: U+0085 is neither WhiteSpace nor LineTerminator, so RegExp.escape leaves it, and escapes the
    // white space it is beside.
    [Fact]
    public void RegExp_escape_escapes_the_specification_s_white_space_only()
        => Assert.Equal("\\x20\u0085\\u3000\\ufeff\\u2028", Eval("RegExp.escape(' \\u0085\\u3000\\ufeff\\u2028')"));

    // I1 and W1 through eval: the scanner's classes.
    [Theory]
    [InlineData("eval('var \\u2E2F = 1')")]
    [InlineData("eval('1\\u0085+1')")]
    public void The_scanner_refuses_pattern_syntax_and_U0085(string expression)
        => Assert.Equal("SyntaxError", ErrorName(expression));

    // I4: a native function's name in its source text is an IdentifierName or a computed name; the
    // legacy RegExp statics' names are not, and are left out.
    [Theory]
    [InlineData("Math.max.toString()", "function max() { [native code] }")]
    [InlineData("Object.getOwnPropertyDescriptor(RegExp.prototype, 'flags').get.toString()", "function get flags() { [native code] }")]
    [InlineData("RegExp.prototype[Symbol.replace].toString()", "function [Symbol.replace]() { [native code] }")]
    [InlineData("Object.getOwnPropertyDescriptor(RegExp, '$&').get.toString()", "function get () { [native code] }")]
    public void A_native_function_s_name_is_an_identifier_name(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // ---- normalization (rows N1 and N2) ----------------------------------------------------------------

    // N1: a lone surrogate is a code point of class 0 without a decomposition, and passes through, where
    // the host's normalizer threw; the collator's decomposition, which localeCompare reaches, likewise.
    [Theory]
    [InlineData("'\\uD800'.normalize() === '\\uD800'")]
    [InlineData("'a\\uDC00\\u0301'.normalize('NFD') === 'a\\uDC00\\u0301'")]
    [InlineData("'\\u00E9\\uD800'.normalize('NFD') === 'e\\u0301\\uD800'")]
    [InlineData("'\\uDBFF'.normalize('NFKC') === '\\uDBFF'")]
    [InlineData("'\\uD800'.localeCompare('\\uD800') === 0")]
    [InlineData("new Intl.Collator().compare('a\\uDC00', 'a\\uDC00') === 0")]
    [InlineData("'a\\uD800'.localeCompare('b') < 0")]
    public void A_lone_surrogate_passes_through(string expression) => Assert.Equal("true", Eval(expression));

    // N2: Unicode 16's decompositions, from the pinned data, where the host's ICU (Unicode 15.1) left them.
    [Theory]
    [InlineData("'\\u{105C9}'.normalize('NFD') === '\\u{105D2}\\u0307'")]
    [InlineData("'\\u{11383}'.normalize('NFD') === '\\u{11382}\\u{113C9}'")]
    [InlineData("'\\u{1611E}\\u{1611E}'.normalize('NFC') === '\\u{16121}'")]
    [InlineData("'\\u{105D2}\\u0307'.normalize() === '\\u{105C9}'")]
    public void Unicode_16_decompositions_are_known(string expression) => Assert.Equal("true", Eval(expression));

    // N4 and N5 hold: the specification's answers, and the form is checked before the text.
    [Theory]
    [InlineData("'\\u00E9'.normalize('NFD') === 'e\\u0301'")]
    [InlineData("'e\\u0301'.normalize() === '\\u00E9'")]
    [InlineData("'\\uFB01'.normalize('NFKC') === 'fi'")]
    [InlineData("'\\uAC00'.normalize('NFD') === '\\u1100\\u1161'")]
    [InlineData("'\\u1100\\u1161'.normalize('NFC') === '\\uAC00'")]
    public void The_specification_s_answers_hold(string expression) => Assert.Equal("true", Eval(expression));

    [Fact]
    public void An_unknown_form_is_a_RangeError_before_the_text_is_read()
        => Assert.Equal("RangeError", ErrorName("'\\uD800'.normalize('nfc')"));
}
