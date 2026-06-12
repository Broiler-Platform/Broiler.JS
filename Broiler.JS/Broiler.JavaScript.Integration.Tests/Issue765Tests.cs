using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/765.
public class Issue765Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // P38/P39/P40: a function whose observable `prototype` property is overwritten
    // with a primitive is still a constructor (constructorness must not be re-read
    // off the mutable prototype cache field), and the instance it builds falls back
    // to %Object.prototype% per OrdinaryCreateFromConstructor.
    [Fact]
    public void FunctionWithPrimitivePrototypeStaysConstructable()
        => Assert.Equal("true", Eval(
            "function F(){} F.prototype = 1;"
            + " var d = new F();"
            + " '' + (typeof F.prototype === 'number' && Object.prototype.isPrototypeOf(d));"));

    [Fact]
    public void FunctionExpressionWithPrimitivePrototypeStaysConstructable()
        => Assert.Equal("true", Eval(
            "var F = function(){}; F.prototype = 'x';"
            + " '' + (new F() instanceof Object);"));

    // P41: ECMAScript allows quantifier counts up to 2^53-1; .NET caps them at
    // Int32.MaxValue. Building such a regex must not throw, and the (unsatisfiable)
    // count must simply never match.
    [Fact]
    public void HugeQuantifierExactDoesNotThrowAndNeverMatches()
        => Assert.Equal("false", Eval(
            "'' + new RegExp('b{' + Number.MAX_SAFE_INTEGER + '}', 'u').test('')"));

    [Fact]
    public void HugeQuantifierOpenEndedDoesNotThrow()
        => Assert.Equal("false", Eval(
            "'' + new RegExp('b{' + Number.MAX_SAFE_INTEGER + ',}?').test('a')"));

    [Fact]
    public void HugeQuantifierRangeDoesNotThrow()
        => Assert.Equal("false", Eval(
            "'' + new RegExp('b{' + Number.MAX_SAFE_INTEGER + ',' + Number.MAX_SAFE_INTEGER + '}').test('b')"));

    [Fact]
    public void NormalQuantifierStillMatches()
        => Assert.Equal("true", Eval("'' + /b{2,3}/.test('bbb')"));

    // P36: `lastIndex` is a per-instance own data property, not a property of
    // %RegExp.prototype%. Reading it off the prototype previously threw
    // "Failed to convert this to JSRegExp".
    [Fact]
    public void RegExpPrototypeHasNoLastIndex()
        => Assert.Equal("true", Eval(
            "'' + (Object.getOwnPropertyNames(RegExp.prototype).indexOf('lastIndex') === -1)"));

    [Fact]
    public void RegExpInstanceLastIndexStillWorks()
        => Assert.Equal("2", Eval(
            "var re = /a/g; re.exec('xa'); '' + re.lastIndex;"));

    [Fact]
    public void RegExpInstanceLastIndexDescriptorIsSpecCompliant()
        => Assert.Equal("true", Eval(
            "var d = Object.getOwnPropertyDescriptor(/a/, 'lastIndex');"
            + " '' + (d.writable && !d.enumerable && !d.configurable);"));

    // P18: in sloppy mode `let` is an IdentifierReference when not followed by a
    // BindingList, including in the C-style for-head (`for (let; ;)`,
    // `for (let = 3; ;)`).
    [Fact]
    public void LetAsIdentifierInForHead()
        => Assert.Equal("1|3", Eval(
            "var let, out = [];"
            + " let = 1; for (let; ;) break; out.push(let);"
            + " let = 2; for (let = 3; ;) break; out.push(let);"
            + " out.join('|');"));

    // P33/P34: `let` as the LeftHandSideExpression of a for-in head.
    [Fact]
    public void LetAsIdentifierInForInHead()
        => Assert.Equal("key", Eval(
            "var obj = Object.create(null); obj.key = 1; var let;"
            + " for (let in obj) ; '' + let;"));

    // Normal lexical declarations must be unaffected.
    [Fact]
    public void LetLexicalDeclarationStillWorks()
        => Assert.Equal("2|3|6", Eval(
            "let a = 2; let [b] = [3]; var s = 0; for (let i of [1,2,3]) s += i;"
            + " a + '|' + b + '|' + s;"));

    // P20: `let` is a valid LabelIdentifier in sloppy mode.
    [Fact]
    public void LetAsLabelSloppy()
        => Assert.Equal("done", Eval("let: { break let; } 'done';"));

    // P20: `let` as a label is a SyntaxError in strict mode.
    [Fact]
    public void LetAsLabelStrictThrows()
        => Assert.Equal("true", Eval(
            "var t = false;"
            + " try { eval(\"'use strict'; let: 42\"); } catch (e) { t = e instanceof SyntaxError; }"
            + " '' + t;"));

    [Fact]
    public void EscapedLetAsLabelStrictThrows()
        => Assert.Equal("true", Eval(
            "var t = false;"
            + " try { eval(\"'use strict'; l\\\\u0065t: 42\"); } catch (e) { t = e instanceof SyntaxError; }"
            + " '' + t;"));

    // P42/P43/P46 etc.: small, fully-enumerable Unicode binary properties (and their
    // aliases) are supported as `\p{…}`/`\P{…}` escapes in u-mode.
    [Theory]
    [InlineData(@"/^\p{ASCII_Hex_Digit}+$/u", "0aF", "true")]
    [InlineData(@"/^\p{AHex}+$/u", "0aF", "true")]
    [InlineData(@"/^\p{Hex}+$/u", "０Ｆ", "true")]   // fullwidth hex digits
    [InlineData(@"/^\p{Bidi_Control}$/u", "‎", "true")]
    [InlineData(@"/^\p{Join_C}$/u", "‍", "true")]
    [InlineData(@"/^\p{White_Space}$/u", " ", "true")]
    [InlineData(@"/^\p{QMark}$/u", "«", "true")]
    [InlineData(@"/^\P{ASCII_Hex_Digit}$/u", "g", "true")]   // negation
    [InlineData(@"/^\p{ASCII_Hex_Digit}$/u", "g", "false")]
    public void BinaryPropertyEscapes(string regex, string input, string expected)
        => Assert.Equal(expected, Eval($"'' + {regex}.test({System.Text.Json.JsonSerializer.Serialize(input)})"));

    // The astral-range matcher emits standalone surrogate units as one-char classes
    // so the lone-surrogate transform no longer breaks supplementary-plane matches
    // (Variation_Selector U+E0100–E01EF), while a genuine lone surrogate still must
    // not match a surrogate pair.
    [Fact]
    public void AstralPropertyEscapeMatchesSupplementaryCodePoints()
        => Assert.Equal("true|true|false", Eval(
            "var re = /\\p{Variation_Selector}/u;"
            + " re.test(String.fromCodePoint(0xE0100)) + '|'"
            + " + re.test(String.fromCodePoint(0xE01EF)) + '|'"
            + " + re.test('a');"));

    [Fact]
    public void LoneHighSurrogateDoesNotMatchSurrogatePair()
        => Assert.Equal("false", Eval(
            "'' + /\\uD800[\\uDC00-\\uDFFF]/u.test(String.fromCodePoint(0x10000))"));

    // The full UCD 17.0.0 binary-property database (Broiler.Unicode.Properties) backs
    // large properties like Alphabetic/Assigned/Cased that previously raised
    // "not supported yet".
    [Theory]
    [InlineData(@"/^\p{Alphabetic}+$/u", "abcπ你", "true")]
    [InlineData(@"/^\p{Alpha}+$/u", "abc", "true")]
    [InlineData(@"/^\p{Alphabetic}$/u", "1", "false")]
    [InlineData(@"/^\p{White_Space}$/u", " ", "true")]
    [InlineData(@"/^\p{Cased}$/u", "A", "true")]
    [InlineData(@"/^\p{Emoji_Presentation}$/u", "🐸", "true")]
    [InlineData(@"/^\p{ID_Start}$/u", "_", "false")]
    [InlineData(@"/^\p{ID_Continue}$/u", "_", "true")]
    public void LargeBinaryPropertyEscapes(string regex, string input, string expected)
        => Assert.Equal(expected, Eval($"'' + {regex}.test({System.Text.Json.JsonSerializer.Serialize(input)})"));

    // Assigned includes the surrogate category (Cs); a supplementary code point that
    // sits in an unassigned gap (U+1000C, between Linear B blocks) must NOT match
    // \p{Assigned} even though its lead surrogate U+D800 is itself assigned.
    [Fact]
    public void AssignedDoesNotMatchSupplementaryGapViaSurrogate()
        => Assert.Equal("false|true", Eval(
            "var s = String.fromCodePoint(0x1000C);"
            + " /^\\p{Assigned}$/u.test(s) + '|' + /^\\P{Assigned}$/u.test(s);"));

    // P6 + the whole Script / Script_Extensions category: backed by the UCD 17.0.0
    // database (long names and short aliases; BMP and supplementary plane).
    [Theory]
    [InlineData(@"/^\p{Script=Greek}+$/u", "αβγ", "true")]
    [InlineData(@"/^\p{sc=Latn}+$/u", "abc", "true")]
    [InlineData(@"/^\p{Script=Han}$/u", "𠮷", "true")]            // supplementary
    [InlineData(@"/^\p{Script=Hiragana}$/u", "あ", "true")]
    [InlineData(@"/^\p{Script=Greek}$/u", "a", "false")]
    [InlineData(@"/^\p{Script_Extensions=Greek}$/u", "·", "true")] // U+0387 via scx
    [InlineData(@"/^\p{scx=Latn}$/u", "·", "true")]                // U+00B7 in many scx
    [InlineData(@"/^\p{Script=Linear_B}$/u", "𐀀", "true")]        // supplementary
    public void ScriptPropertyEscapes(string regex, string input, string expected)
        => Assert.Equal(expected, Eval($"'' + {regex}.test({System.Text.Json.JsonSerializer.Serialize(input)})"));

    [Fact]
    public void LoneScriptNameIsSyntaxError()
        => Assert.Equal("true", Eval(
            "var t = false; try { new RegExp('\\\\p{Han}', 'u'); } catch (e) { t = e instanceof SyntaxError; } '' + t;"));
}
