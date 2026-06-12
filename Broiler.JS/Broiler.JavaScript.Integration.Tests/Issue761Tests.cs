using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/761
//
// Fixed here:
//
//   Problem 37 — A LineTerminator between a prefix unary operator
//   (delete/void/typeof/+/-/~/!) and its operand is insignificant: the operand
//   may begin on a following line. Previously this worked only when the operand
//   was an identifier; a number/literal/parenthesized operand (e.g. `delete\n0`,
//   `void\n0`, `!\n0`, `delete\n(0)`) failed, because the operator-lookahead left
//   the token stream parked on the intervening LineTerminator token.
//
//   Problem 38 — The NewTarget meta-property `new.target` may have line
//   terminators between its three tokens (`new\n.\ntarget`). The `new` operator
//   lookahead and the meta-property parser now skip line terminators (whitespace
//   and comments between the tokens already worked).
//
//   Problem 25 — A template literal may contain an invalid escape sequence
//   (`\01`, `\8`, `\xg`, `\u0`, `\u{g`, `\u{10FFFFF}`, …) only when it is the
//   operand of a tag (ES2018 template literal revision). In a tagged template the
//   cooked TemplateStringsArray entry is `undefined` while the `.raw` value is
//   preserved verbatim; an untagged template literal containing one is an early
//   SyntaxError. The scanner now defers (rather than throws) such escapes, marking
//   the part's cooked value invalid.
//
//   Problems 43/44/47/48 — Other_ID_Start and Other_ID_Continue grandfathered
//   characters. IsIdentifierStart was missing U+1885/U+1886 (Mongolian Ali Gali
//   Baluda, category Mn) and IsIdentifierPart was missing U+30FB/U+FF65 (Katakana
//   middle dots, category Po) — all carry the Other_ID_Start / Other_ID_Continue
//   property and must be accepted in identifiers despite their non-letter Unicode
//   category. (Unicode-17.0-only identifier characters remain limited by the host
//   runtime's Unicode data version and are out of scope.)
//
//   Problem 36 — `await`'s operand is a UnaryExpression, so `await a * b` is
//   `(await a) * b` and the await expression participates in the enclosing operator
//   chain. Parsing it as a full Expression mis-associated higher-precedence
//   operators, and a trailing EndOfStatement() consumed the statement terminator —
//   breaking automatic semicolon insertion for the enclosing statement (e.g.
//   `let y = await x\n stmt`).
//
//   (Runtime follow-up exposed by the P36 parser fix) — `await` of a non-thenable
//   value (a primitive, or an object with no `then` method) must still suspend for
//   one microtask tick and resume the async function with that value, so the
//   continuation after the await runs. The async driver previously resolved the
//   whole function with the value, discarding everything after the await — a bug
//   that was masked while `await` greedily consumed the rest of the expression.
public class Issue761Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    // Drives an async body to completion (Execute pumps the event loop) and returns
    // the value recorded in the global `r`.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    private static string U(params int[] cps) => string.Concat(cps.Select(char.ConvertFromUtf32));

    // ---- Problem 37: line terminator between prefix unary operator and operand ----

    public static IEnumerable<object[]> LineTerminators()
    {
        yield return new object[] { "\n" };                      // LF
        yield return new object[] { "\r" };                      // CR
        yield return new object[] { ((char)0x2028).ToString() }; // LINE SEPARATOR
        yield return new object[] { ((char)0x2029).ToString() }; // PARAGRAPH SEPARATOR
    }

    [Theory]
    [MemberData(nameof(LineTerminators))]
    public void DeleteAllowsLineTerminatorBeforeNumberOperand(string lt)
        => Assert.Equal("true", Eval("var r = delete" + lt + "0; r"));

    [Theory]
    [MemberData(nameof(LineTerminators))]
    public void VoidAllowsLineTerminatorBeforeNumberOperand(string lt)
        => Assert.Equal("undefined", Eval("String(void" + lt + "0)"));

    [Theory]
    [MemberData(nameof(LineTerminators))]
    public void TypeofAllowsLineTerminatorBeforeNumberOperand(string lt)
        => Assert.Equal("number", Eval("typeof" + lt + "0"));

    [Theory]
    [MemberData(nameof(LineTerminators))]
    public void NegateAllowsLineTerminatorBeforeNumberOperand(string lt)
        => Assert.Equal("true", Eval("!" + lt + "0"));

    [Theory]
    [MemberData(nameof(LineTerminators))]
    public void UnaryMinusAllowsLineTerminatorBeforeNumberOperand(string lt)
        => Assert.Equal("-5", Eval("-" + lt + "5"));

    [Fact]
    public void DeleteAllowsLineTerminatorBeforeParenthesizedOperand()
        => Assert.Equal("true", Eval("delete\n(0)"));

    [Fact]
    public void DeleteAllowsNonLineTerminatorWhitespaceBeforeOperand()
    {
        // VT (U+000B) and FF (U+000C) are whitespace, not line terminators.
        Assert.Equal("true", Eval("var r = delete" + (char)0x0B + "0; r"));
        Assert.Equal("true", Eval("var r = delete" + (char)0x0C + "0; r"));
    }

    [Fact]
    public void PrefixOperatorStillWorksWithIdentifierOperandAcrossNewline()
    {
        Assert.Equal("d", Eval("var x={}; delete\nx.y; 'd'"));
        Assert.Equal("undefined", Eval("typeof\nundefinedGlobalRef"));
    }

    // ---- Problem 38: line terminators inside the new.target meta-property ----

    [Fact]
    public void NewTargetAllowsLineBreaksBetweenTokens()
        => Assert.Equal("true", Eval(
            "var t=null; var f=function(){t = new\n.\ntarget;}; new f(); t===f"));

    [Fact]
    public void NewTargetUndefinedOnPlainCallWithLineBreaks()
        => Assert.Equal("undefined", Eval(
            "var t='x'; var f=function(){t = new\n.\ntarget;}; f(); String(t)"));

    [Fact]
    public void NewTargetStillWorksWithSpacesAndComments()
    {
        Assert.Equal("true", Eval(
            "var t=null; var f=function(){t = new   .   target;}; new f(); t===f"));
        Assert.Equal("true", Eval(
            "var t=null; var f=function(){t = new/* */./* */target;}; new f(); t===f"));
        // Multi-line comments (containing line terminators) between the tokens.
        Assert.Equal("true", Eval(
            "var t=null; var f=function(){t = new/*\n*/./*\n*/target;}; new f(); t===f"));
    }

    [Fact]
    public void NewExpressionStillWorksWithLineBreakBeforeCallee()
        => Assert.Equal("true", Eval("function Foo(){}; var o = new\nFoo(); o instanceof Foo"));

    [Fact]
    public void NestedNewExpressionStillParses()
        => Assert.Equal("true", Eval(
            "function F(){}; (new new F().constructor) instanceof Function"));

    // ---- Problem 25: invalid escape sequences in tagged / untagged templates ----

    [Fact]
    public void TaggedTemplateInvalidEscapeCookedIsUndefined()
        => Assert.Equal("true", Eval(
            "var c=(s=>s[0]);" +
            "[c`\\01`,c`\\1`,c`\\8`,c`\\9`,c`\\xg`,c`\\xAg`,c`\\u0`,c`\\u0g`," +
            " c`\\u00g`,c`\\u000g`,c`\\u{g`,c`\\u{0`,c`\\u{10FFFFF}`]" +
            ".every(v => v === undefined)"));

    // A single program (distinct source offsets) so the per-offset template-object
    // cache does not alias the cases — this mirrors how the real test262 file is
    // structured (every template at its own source position).
    [Fact]
    public void TaggedTemplateInvalidEscapeRawIsPreserved()
        => Assert.Equal("\\01,\\9,\\xg,\\xAg,\\u0,\\u0g,\\u{g,\\u{0,\\u{10FFFFF}", Eval(
            "var r=(s=>s.raw[0]);" +
            "[r`\\01`,r`\\9`,r`\\xg`,r`\\xAg`,r`\\u0`,r`\\u0g`,r`\\u{g`,r`\\u{0`,r`\\u{10FFFFF}`].join(',')"));

    [Fact]
    public void TaggedTemplateInvalidEscapeBeforeSubstitution()
        => Assert.Equal("\\u{10FFFFF}|undefined|inner|right|right", Eval(
            "((s, val) => [s.raw[0], String(s[0]), val, s[1], s.raw[1]].join('|'))" +
            "`\\u{10FFFFF}${'inner'}right`"));

    [Fact]
    public void TaggedTemplateValidEscapesStillCook()
        => Assert.Equal("10,A,A,0", Eval(
            "[(s=>s[0].charCodeAt(0))`\\n`," +   // \n => LF
            " (s=>s[0])`\\u0041`," +             // A => 'A'
            " (s=>s[0])`\\x41`," +               // \x41 => 'A'
            " (s=>s[0].charCodeAt(0))`\\0`" +    // \0 => NUL
            "].join(',')"));

    [Theory]
    [InlineData("`\\01`")]
    [InlineData("`\\xg`")]
    [InlineData("`\\u0`")]
    [InlineData("`\\u{g`")]
    [InlineData("`\\u{10FFFFF}`")]
    [InlineData("`a${1}\\9b`")]
    public void UntaggedTemplateInvalidEscapeIsSyntaxError(string source)
    {
        var ex = Assert.Throws<Broiler.JavaScript.Runtime.JSException>(() => Eval(source));
        Assert.Contains("template", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UntaggedTemplateValidEscapesStillWork()
    {
        Assert.Equal("aAb", Eval("`a\\u0041b`"));
        Assert.Equal("0", Eval("(`\\0`).charCodeAt(0).toString()"));
        Assert.Equal("hello 2!", Eval("`hello ${1+1}!`"));
    }

    // ---- Problems 43/44/47/48: Other_ID_Start / Other_ID_Continue characters ----

    [Theory]
    [InlineData(0x1885)] // MONGOLIAN LETTER ALI GALI BALUDA (Other_ID_Start, Mn)
    [InlineData(0x1886)] // MONGOLIAN LETTER ALI GALI THREE BALUDA (Other_ID_Start, Mn)
    [InlineData(0x2118)] // SCRIPT CAPITAL P (Other_ID_Start, Sm)
    [InlineData(0x212E)] // ESTIMATED SYMBOL (Other_ID_Start, So)
    [InlineData(0x309B)] // KATAKANA-HIRAGANA VOICED SOUND MARK (Other_ID_Start, Sk)
    [InlineData(0x309C)] // KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK (Other_ID_Start, Sk)
    public void OtherIdStartCharacterIsValidIdentifierStart(int cp)
    {
        Assert.Equal("7", Eval("var " + U(cp) + " = 7; " + U(cp)));
        // Escaped form (\\uXXXX) must be accepted identically.
        Assert.Equal("7", Eval("var \\u" + cp.ToString("X4") + " = 7; \\u" + cp.ToString("X4")));
    }

    [Theory]
    [InlineData(0x00B7)] // MIDDLE DOT (Other_ID_Continue, Po)
    [InlineData(0x0387)] // GREEK ANO TELEIA (Other_ID_Continue, Po)
    [InlineData(0x30FB)] // KATAKANA MIDDLE DOT (Other_ID_Continue, Po)
    [InlineData(0xFF65)] // HALFWIDTH KATAKANA MIDDLE DOT (Other_ID_Continue, Po)
    [InlineData(0x200C)] // ZERO WIDTH NON-JOINER (Cf)
    [InlineData(0x200D)] // ZERO WIDTH JOINER (Cf)
    public void OtherIdContinueCharacterIsValidIdentifierPart(int cp)
        => Assert.Equal("7", Eval("var a" + U(cp) + " = 7; a" + U(cp)));

    [Fact]
    public void IdentifierWithZwnjZwjAndKatakanaMiddleDots()
        // The full identifier from test262 part-unicode-15.1.0.js: _<ZWNJ><ZWJ>・･
        => Assert.Equal("7", Eval(
            "var " + U('_', 0x200C, 0x200D, 0x30FB, 0xFF65) + " = 7; "
                   + U('_', 0x200C, 0x200D, 0x30FB, 0xFF65)));

    [Fact]
    public void PrivateClassFieldWithOtherIdContinueCharacters()
        // test262 part-unicode-15.1.0-class.js: a private field named #_<ZWNJ><ZWJ>・･
        => Assert.Equal("7", Eval(
            "class C { #" + U('_', 0x200C, 0x200D, 0x30FB, 0xFF65) + " = 7;"
            + " get(){ return this.#" + U('_', 0x200C, 0x200D, 0x30FB, 0xFF65) + "; } }"
            + " new C().get()"));

    // ---- Problem 36: await operand precedence and ASI ----

    [Fact]
    public void AwaitOperandIsUnaryExpressionMultiplicative()
        // (await 2) * 2 == 4, not await(2 * 2)
        => Assert.Equal("4", Drive(
            "async function f(){ let x = 2; r = await Promise.resolve(2) * x; } f();"));

    [Fact]
    public void AwaitOperandIsUnaryExpressionAdditive()
        => Assert.Equal("5", Drive(
            "async function f(){ r = await Promise.resolve(2) + 3; } f();"));

    [Fact]
    public void AwaitOperandIsUnaryExpressionRelational()
        => Assert.Equal("true", Drive(
            "async function f(){ r = await Promise.resolve(2) < 5; } f();"));

    [Fact]
    public void AwaitExpressionAllowsAsiInEnclosingStatement()
    {
        // The newline after the await expression must let ASI terminate the
        // `let`/expression statement so the next statement parses.
        Assert.Equal("4", Drive(
            "async function f(){ let x = 2\n let y = await Promise.resolve(2) * x\n r = y; } f();"));
        Assert.Equal("parsed", Eval(
            "async function f(){ let y = await Promise.resolve(5)\n return y; } 'parsed'"));
    }

    [Fact]
    public void AwaitBeforeExponentiationIsSyntaxError()
    {
        var ex = Assert.Throws<Broiler.JavaScript.Runtime.JSException>(
            () => Eval("async function f(){ return await 2 ** 2; }"));
        Assert.Contains("exponentiation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParenthesizedAwaitBeforeExponentiationIsAllowed()
        => Assert.Equal("parsed", Eval(
            "async function f(){ return (await 2) ** 2; } 'parsed'"));

    [Fact]
    public void AwaitOfNonThenableRunsContinuation()
    {
        // (await 1) + 1 == 2 — the continuation after awaiting a non-thenable runs.
        Assert.Equal("2", Drive("async function f(){ r = await 1 + 1; } f();"));
        // await of a non-thenable in a loop accumulates correctly.
        Assert.Equal("3", Drive(
            "async function f(){ let s = 0; for (let i = 0; i < 3; i++) { s += await i; } r = s; } f();"));
        // await of undefined yields undefined (does not throw on a missing .then).
        Assert.Equal("u", Drive(
            "async function f(){ let v = await undefined; r = v === undefined ? 'u' : v; } f();"));
    }

    [Fact]
    public void AwaitOfNonThenableSuspendsOneMicrotaskTick()
        // Interleaving proves await(0) suspends rather than running synchronously.
        => Assert.Equal("A1,main,A2,after", Drive(
            "async function f(){ let log=[];"
            + " (async()=>{ log.push('A1'); await 0; log.push('A2'); })();"
            + " log.push('main'); await 0; log.push('after'); r = log.join(','); } f();"));
}
