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

    // ---- case mapping (rows C1 to C8) ------------------------------------------------------------------

    // C1: U+00AA and U+00BA are Cased (Other_Lowercase), so a sigma after them is final.
    [Theory]
    [InlineData("'\\u00AA\\u03A3'.toLowerCase() === '\\u00AA\\u03C2'")]
    [InlineData("'\\u00BA\\u03A3'.toLowerCase() === '\\u00BA\\u03C2'")]
    // C3: Unicode 17's Beria Erfe case pairs, which the hand patches lacked.
    [InlineData("'\\u{16EA0}'.toLowerCase() === '\\u{16EBB}'")]
    [InlineData("'\\u{16EBB}'.toUpperCase() === '\\u{16EA0}'")]
    // C4: the Turkic lower case reads the pinned data for every other code point.
    [InlineData("'\\uA7CB'.toLocaleLowerCase('tr') === '\\u0264'")]
    [InlineData("'\\uA7CE'.toLocaleLowerCase('tr') === '\\uA7CF'")]
    // C5: U+1DF1A is Soft_Dotted, so Lithuanian upper case removes the dot above after it.
    [InlineData("'\\u{1DF1A}\\u0307'.toLocaleUpperCase('lt') === '\\u{1DF1A}'")]
    // C6: More_Above admits no intervening character of class 0 or 230, and U+0903 is of class 0.
    [InlineData("'I\\u0903\\u0300'.toLocaleLowerCase('lt') === 'i\\u0903\\u0300'")]
    [InlineData("'I\\u0903\\u0307'.toLocaleLowerCase('tr') === '\\u0131\\u0903\\u0307'")]
    // C7: a list of locales is canonicalized and its first element taken.
    [InlineData("'i'.toLocaleUpperCase(['tr', 'en']) === '\\u0130'")]
    [InlineData("'i'.toLocaleUpperCase(['en', 'tr']) === 'I'")]
    [InlineData("'I'.toLocaleLowerCase(['az']) === '\\u0131'")]
    // C9 holds: the language is matched after canonicalization, a Unicode extension removed.
    [InlineData("'i'.toLocaleUpperCase('TR') === '\\u0130'")]
    [InlineData("'i'.toLocaleUpperCase('tr-u-co-search') === '\\u0130'")]
    [InlineData("'i'.toLocaleUpperCase([]) === 'I'")]
    // C11 holds: lone surrogates pass through.
    [InlineData("'\\uD800a'.toUpperCase() === '\\uD800A'")]
    public void Case_mapping_follows_the_pinned_data(string expression) => Assert.Equal("true", Eval(expression));

    // C8: CanonicalizeLocaleList(null) throws a TypeError, and a malformed tag a RangeError.
    [Theory]
    [InlineData("'i'.toLocaleUpperCase(null)", "TypeError")]
    [InlineData("'i'.toLocaleLowerCase(null)", "TypeError")]
    [InlineData("'i'.toLocaleUpperCase('x!')", "RangeError")]
    [InlineData("'i'.toLocaleUpperCase(['tr', 5])", "TypeError")]
    public void A_locale_list_is_canonicalized_first(string expression, string error)
        => Assert.Equal(error, ErrorName(expression));

    // C8: with no locale the default is this engine's, en-US, and never the host's culture; nor does
    // toUpperCase or toLowerCase read the culture.
    [Fact]
    public void Casing_does_not_read_the_host_culture()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal("I,I,I,i,i", Eval("['i'.toLocaleUpperCase(), 'i'.toLocaleUpperCase(undefined), 'i'.toUpperCase(), 'I'.toLocaleLowerCase(), 'I'.toLowerCase()].join()"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = saved;
        }
    }

    // ---- property lookup and properties of strings (rows P1 to P4 and S2) ------------------------------

    // P1 and S2: names are matched exactly as ECMA-262's tables write them, where a loose lookup ignored their
    // case, underscores, spaces and hyphens; P3: an unknown name no longer falls through to .NET's own \p.
    // Through the constructor and, by the parser's early check, through a literal.
    [Theory]
    [InlineData("new RegExp('\\\\p{letter}', 'u')")]
    [InlineData("new RegExp('\\\\p{Script_Extensions=greek}', 'u')")]
    [InlineData("new RegExp('\\\\p{GC=Lu}', 'u')")]
    [InlineData("new RegExp('\\\\p{lu}', 'u')")]
    [InlineData("new RegExp('\\\\p{Uppercase Letter}', 'u')")]
    [InlineData("new RegExp('[\\\\p{lowercase_letter}]', 'v')")]
    [InlineData("new RegExp('\\\\p{rgiemoji}', 'v')")]
    [InlineData("new RegExp('\\\\P{RGI_Emoji}', 'v')")]
    [InlineData("new RegExp('\\\\p{RGI_Emoji}', 'u')")]
    [InlineData("new RegExp('\\\\p{IsGreek}', 'u')")]
    [InlineData("new RegExp('\\\\p{Han}', 'u')")]
    [InlineData("new RegExp('\\\\p{Lu', 'u')")]
    [InlineData("eval('/\\\\p{letter}/u')")]
    [InlineData("eval('/\\\\P{lu}/v')")]
    [InlineData("eval('/[\\\\p{rgiemoji}]/v')")]
    [InlineData("eval('/\\\\p{IsGreek}/u')")]
    public void A_name_ECMA_262_does_not_hold_is_a_SyntaxError(string expression)
        => Assert.Equal("SyntaxError", ErrorName(expression));

    [Theory]
    [InlineData("/\\p{Lu}/u.test('A')")]
    [InlineData("/\\p{General_Category=Uppercase_Letter}/u.test('A')")]
    [InlineData("/\\p{gc=LC}/u.test('a')")]
    [InlineData("/[\\p{Lowercase_Letter}]/u.test('a')")]
    [InlineData("/\\p{Script_Extensions=Greek}/u.test('\\u03A9')")]
    [InlineData("/^\\p{RGI_Emoji}$/v.test('\\u{1F44D}\\u{1F3FD}')")]
    [InlineData("/^\\p{Basic_Emoji}$/v.test('\\u231A')")]
    [InlineData("/\\p{letter}/.test('p{letter}')")]
    [InlineData("eval('/\\\\p{Script=Latin}/u').test('a')")]
    public void A_name_ECMA_262_holds_is_admitted(string expression) => Assert.Equal("true", Eval(expression));

    // P2: Katakana_Or_Hiragana (Hrkt) is a value of Script and Script_Extensions, and names an empty set.
    [Theory]
    [InlineData("/\\p{sc=Hrkt}/u.test('\\u3042')", "false")]
    [InlineData("/\\p{Script=Katakana_Or_Hiragana}/u.test('\\u30A2')", "false")]
    [InlineData("/\\p{scx=Hrkt}/u.test('\\u30FC')", "false")]
    [InlineData("/^\\p{sc=Hrkt}*$/u.test('')", "true")]
    [InlineData("/\\P{sc=Hrkt}/u.test('\\u3042')", "true")]
    [InlineData("/[^\\p{sc=Hrkt}]/u.test('a')", "true")]
    [InlineData("'a'.replace(/\\p{sc=Hrkt}/gu, 'X')", "a")]
    public void Katakana_Or_Hiragana_is_an_empty_set(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // P2 and P4 on the .NET translator's path, which CreateRegex builds beside Broiler.Regex: the empty set
    // matches nothing, and a v-mode set operation takes General_Category.
    [Theory]
    [InlineData("\\p{sc=Hrkt}", "u", "", false)]
    [InlineData("\\p{sc=Hrkt}", "u", "\\u3042", false)]
    [InlineData("[\\p{gc=Lu}--\\p{Script=Latin}]", "v", "\\u03A9", true)]
    [InlineData("[\\p{gc=Lu}--\\p{Script=Latin}]", "v", "A", false)]
    [InlineData("[\\p{Lu}&&\\p{Script=Greek}]", "v", "\\u03A9", true)]
    [InlineData("[\\p{Lu}&&\\p{Script=Greek}]", "v", "\\u03C9", false)]
    [InlineData("\\p{Lu}", "u", "A", true)]
    public void The_translator_resolves_names_exactly(string pattern, string flags, string input, bool expected)
    {
        Load();
        using var ctx = new JSContext();
        var (regex, _, _, _, _, _, _, _, _) = RegExp.JSRegExp.CreateRegex(pattern, flags, out _);
        Assert.NotNull(regex);
        Assert.Equal(expected, regex.IsMatch(Unescape(input)));
    }

    // The translator's in-class General_Category table chooses .NET's spelling for names Broiler.Unicode's
    // lookup has admitted, and holds no name it would refuse.
    [Fact]
    public void Every_general_category_name_the_translator_spells_is_admitted()
    {
        var field = typeof(RegExp.JSRegExp).GetField("GeneralCategoryNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var names = Assert.IsType<Dictionary<string, string>>(field!.GetValue(null));
        Assert.Equal(78, names.Count);
        foreach (var name in names.Keys)
            Assert.Equal("true", Eval($"/\\p{{General_Category={name}}}/u instanceof RegExp && /\\p{{{name}}}/u instanceof RegExp"));
    }

    private static string Unescape(string text) => System.Text.RegularExpressions.Regex.Unescape(text);
}
