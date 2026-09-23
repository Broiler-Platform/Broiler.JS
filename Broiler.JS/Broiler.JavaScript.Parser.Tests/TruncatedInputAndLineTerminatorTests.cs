using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

// Front-end defects found by the Broiler.JSeal provider audits (JSeal roadmap J17 and its
// follow-ups):
//
//  - Truncated input was accepted. A binary operator with no right operand (`z +`,
//    `` `\n\n` + ; ``), an argument list, computed member, object literal or template
//    substitution cut off by the end of the source, an unterminated block comment, and a string
//    literal broken by a raw LF/CR all parsed as if the missing part had been written.
//  - The scanner counted only LF (and so CRLF) as a line break. ECMAScript's LineTerminators are
//    LF, CR, U+2028 and U+2029, with CRLF one sequence (§12.3), so every position after a lone
//    CR, LS or PS was on the wrong line.
//  - An unterminated string, template or comment was blamed on the previously scanned token,
//    whose line the compile frame then reported — not the line the failure was found on, which
//    the message named.
public class TruncatedInputAndLineTerminatorTests
{
    private static FastParseException ParseError(string source)
    {
        // The scanner reads its first two tokens while the stream is constructed, so a failure
        // there surfaces from the constructor rather than from ParseProgram.
        return Assert.Throws<FastParseException>(
            () => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram());
    }

    private static Ast.AstProgram Parse(string source)
        => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    [Theory]
    [InlineData("z +")]
    [InlineData("1 +")]
    [InlineData("`\n\n` + ;")]
    [InlineData("x = 1 +\n;")]
    [InlineData("a\n+")]
    [InlineData("a * ")]
    [InlineData("a &&")]
    [InlineData("a ??")]
    [InlineData("a instanceof")]
    [InlineData("a in")]
    [InlineData("a ===")]
    [InlineData("a **")]
    [InlineData("a++ +")]
    [InlineData("f(1,")]
    [InlineData("f(1")]
    [InlineData("f(")]
    [InlineData("new F(1,")]
    [InlineData("a[1")]
    [InlineData("a[1;")]
    [InlineData("function g() { return a[1 }")]
    [InlineData("x = {a")]
    [InlineData("x = {a: 1,")]
    [InlineData("var x = `a${1")]
    [InlineData("/* open")]
    [InlineData("/* open *")]
    [InlineData("'open\nstring'")]
    [InlineData("\"open\rstring\"")]
    [InlineData("'open")]
    [InlineData("`open")]
    public void TruncatedInputIsRejected(string source) => ParseError(source);

    [Theory]
    [InlineData("a +\nb;")]
    [InlineData("var r = a[0\n];")]
    [InlineData("var r = a[\n0\n];")]
    [InlineData("f(1,\n2\n);")]
    [InlineData("x = {\na: 1,\n};")]
    [InlineData("x = `a${1}b${2}`;")]
    [InlineData("'line\\\ncontinued';")]
    [InlineData("'ls\u2028ps\u2029ok';")]
    [InlineData("/* **/ a;")]
    [InlineData("a /* *\n */ b")]
    public void CompleteInputStillParses(string source) => Assert.NotNull(Parse(source));

    [Theory]
    [InlineData("a;\nb;\nc;")]
    [InlineData("a;\rb;\rc;")]
    [InlineData("a;\r\nb;\r\nc;")]
    [InlineData("a;\u2028b;\u2029c;")]
    [InlineData("a; /*\r*/ b; // x\u2028c;")]
    [InlineData("a;`\r`;\nc;")]
    public void EveryLineTerminatorAdvancesTheLine(string source)
    {
        var statements = Parse(source).Statements.ToArray();
        Assert.Equal(3, statements.Length);
        Assert.Equal(3, statements[2].Start.Start.Line);
    }

    [Theory]
    [InlineData("a;\rb;\r)", 3)]
    [InlineData("a;\u2028b;\u2029)", 3)]
    [InlineData("a;\r\nb;\r\n)", 3)]
    public void ParseErrorsAfterLoneTerminatorsNameTheirOwnLine(string source, int line)
        => Assert.Equal(line, ParseError(source).Token.Start.Line);

    [Theory]
    [InlineData("a = 1;\nb = 2;\nc = \"abc\nd\";", "Unterminated string literal", 3)]
    [InlineData("a = 1;\n\nc = 'abc", "Unterminated string literal", 3)]
    [InlineData("a = 1;\nb = `abc\n\ndef", "Unterminated template literal", 4)]
    [InlineData("a = 1;\n/* abc\n\nx", "Unterminated comment", 4)]
    [InlineData("a = 1;\r/* abc\u2028x", "Unterminated comment", 3)]
    public void UnterminatedTokensAreReportedWhereScanningStopped(string source, string message, int line)
    {
        var error = ParseError(source);
        Assert.StartsWith(message, error.Message);
        Assert.Equal(line, error.Token.Start.Line);
        // The message's own position and the token the compile frame reads agree.
        Assert.EndsWith($" at {line}, {error.Token.Start.Column}", error.Message);
    }
}
