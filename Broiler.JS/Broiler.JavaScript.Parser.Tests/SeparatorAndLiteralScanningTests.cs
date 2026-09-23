using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

// Front-end leniencies found by the Broiler.JS core-fixes slice (JSeal audit follow-ups):
//
//  - The scanner read a doubled quote inside a string literal as an escaped quote (the SQL/VB
//    convention), so `'a''b'` was the one string "a'b". ECMAScript has no such escape
//    (§12.9.4): the literal ends at the first unescaped quote, and `'a''b'` is two adjacent
//    string literals — a SyntaxError where they meet in one expression.
//  - A line terminator between two arguments was taken as an argument separator, so `f(1\n2)`
//    was the call f(1, 2). Arguments are separated by commas only (§13.3 ArgumentList); a line
//    terminator never ends an argument list and ASI does not apply inside one.
//  - The same class of leniency in object literals (`{a: 1\nb: 2}`, `{a() {} b() {}}`) and in
//    array binding patterns (`var [a b] = []`): PropertyDefinitionList and BindingElementList
//    are comma-separated.
//  - Columns were numbered from 1 on the first line but from 0 on every later line. They are
//    now 1-based on every line: the first character of any line is column 1.
public class SeparatorAndLiteralScanningTests
{
    private static FastParseException ParseError(string source)
        => Assert.Throws<FastParseException>(
            () => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram());

    private static Ast.AstProgram Parse(string source)
        => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    // ---- string literals: a doubled quote is not an escape -----------------------------------

    [Theory]
    [InlineData("'a''b'")]
    [InlineData("\"a\"\"b\"")]
    [InlineData("x = 'it''s'")]
    [InlineData("f('a''b')")]
    [InlineData("''''")]
    public void AdjacentStringLiteralsAreASyntaxError(string source) => ParseError(source);

    [Theory]
    [InlineData("'a'\n'b'", "a", "b")]
    [InlineData("\"a\"\n\"b\"", "a", "b")]
    [InlineData("''\n''", "", "")]
    public void AdjacentStringLiteralsOnSeparateLinesAreSeparateStatements(string source, string first, string second)
    {
        var statements = Parse(source).Statements.ToArray();
        Assert.Equal(2, statements.Length);
        Assert.Equal(first, StringValue(statements[0]));
        Assert.Equal(second, StringValue(statements[1]));
    }

    [Theory]
    [InlineData("'it\\'s'", "it's")]
    [InlineData("\"say \\\"hi\\\"\"", "say \"hi\"")]
    [InlineData("\"it's\"", "it's")]
    [InlineData("'say \"hi\"'", "say \"hi\"")]
    [InlineData("''", "")]
    [InlineData("'\\''", "'")]
    public void QuotesInsideStringLiteralsNeedABackslashOrTheOtherQuote(string source, string value)
        => Assert.Equal(value, StringValue(Parse(source).Statements.Single()));

    private static string StringValue(AstStatement statement)
    {
        var expression = Assert.IsType<AstExpressionStatement>(statement).Expression;
        return Assert.IsType<AstLiteral>(expression).StringValue;
    }

    // ---- list separators -----------------------------------------------------------------------

    [Theory]
    // Arguments: commas only; a line terminator is not a separator.
    [InlineData("f(1\n2)")]
    [InlineData("f(a\nb)")]
    [InlineData("f(1\r\n2)")]
    [InlineData("f(1\u20282)")]
    [InlineData("new f(1\n2)")]
    [InlineData("f?.(1\n2)")]
    [InlineData("a.b(1\n2)")]
    [InlineData("f(...a\nb)")]
    [InlineData("f(1, 2\n3)")]
    [InlineData("import('a'\n'b')")]
    // Arguments have no elisions.
    [InlineData("f(,)")]
    [InlineData("f(,1)")]
    [InlineData("f(1,,2)")]
    [InlineData("f(...)")]
    [InlineData("new f(,)")]
    // An identifier directly after a complete expression is not a second element (it was
    // silently dropped when the expression began with a literal, parenthesis or operator).
    [InlineData("f(1 b)")]
    [InlineData("f('s' b)")]
    [InlineData("[1 b]")]
    [InlineData("[(1) b]")]
    [InlineData("(1 b)")]
    [InlineData("x = -a b")]
    [InlineData("var [a = 1 b] = []")]
    [InlineData("var {a = 1 b} = {}")]
    [InlineData("({a: 1 b})")]
    // Object literals: PropertyDefinitionList is comma-separated.
    [InlineData("({a: 1 b: 2})")]
    [InlineData("({a: 1\nb: 2})")]
    [InlineData("({a\nb})")]
    [InlineData("({a() {} b() {}})")]
    [InlineData("({a() {}\nb() {}})")]
    [InlineData("({get a() {} set a(v) {}})")]
    [InlineData("({...a\nb})")]
    [InlineData("({a = 1; b} = {})")]
    [InlineData("({a: 1;})")]
    [InlineData("x = {a: 1\n}\n({b: 2\nc: 3})")]
    // Array literals and binding patterns: commas only.
    [InlineData("[1 2]")]
    [InlineData("[1\n2]")]
    [InlineData("var [a b] = []")]
    [InlineData("var [a\nb] = []")]
    [InlineData("let [a = 1 b] = []")]
    [InlineData("function f([a b]) {}")]
    [InlineData("[a b] = []")]
    [InlineData("var {a b} = {}")]
    // Parameter lists.
    [InlineData("function f(a b) {}")]
    [InlineData("function f(a\nb) {}")]
    [InlineData("(a\nb) => 1")]
    [InlineData("({m(a\nb) {}})")]
    // Class bodies separate elements by `;`/ASI, never by a comma.
    [InlineData("class C { a() {}, b() {} }")]
    [InlineData("class C { a = 1, b = 2 }")]
    [InlineData("class C { a = 1 b = 2 }")]
    public void MissingListSeparatorsAreASyntaxError(string source) => ParseError(source);

    [Theory]
    [InlineData("f(1,\n2)", 2)]
    [InlineData("f(1\n,2)", 2)]
    [InlineData("f(\n1\n)", 1)]
    [InlineData("f(\n1,\n2,\n)", 2)]
    [InlineData("f(1, 2,)", 2)]
    [InlineData("f()", 0)]
    [InlineData("f(\n)", 0)]
    public void ArgumentListsWithCommasStillParse(string source, int arguments)
    {
        var statement = Assert.IsType<AstExpressionStatement>(Parse(source).Statements.Single());
        var call = Assert.IsType<AstCallExpression>(statement.Expression);
        Assert.Equal(arguments, call.Arguments.Count);
    }

    [Theory]
    [InlineData("({a: 1,\nb: 2})")]
    [InlineData("({a: 1\n,b: 2})")]
    [InlineData("({\na: 1\n,\nb: 2\n,\n})")]
    [InlineData("({a() {},\nb() {},})")]
    [InlineData("({get a() {}, set a(v) {}})")]
    [InlineData("({...a,\n...b})")]
    [InlineData("({a = 1, b} = {})")]
    [InlineData("[1,\n2,\n]")]
    [InlineData("[,,1,,]")]
    [InlineData("var [a,\nb,\n] = []")]
    [InlineData("var [, a,, b] = []")]
    [InlineData("function f(a,\nb,\n) {}")]
    [InlineData("class C { a() {} b() {} }")]
    [InlineData("class C { a() {}\nb() {} }")]
    [InlineData("class C { a = 1\nb = 2 }")]
    [InlineData("class C { a = 1; b = 2; }")]
    [InlineData("class C { a; b }")]
    [InlineData("class C { a\nb }")]
    [InlineData("var a = 1\nb = 2")]
    public void CommaSeparatedListsStillParse(string source) => Assert.NotNull(Parse(source));

    // ---- column numbering ----------------------------------------------------------------------

    [Theory]
    [InlineData("a;\nb;")]
    [InlineData("a;\rb;")]
    [InlineData("a;\r\nb;")]
    [InlineData("a;\u2028b;")]
    [InlineData("a;\u2029b;")]
    [InlineData("a; /*\n*/\nb;")]
    [InlineData("a;`\n`;\nb;")]
    public void EveryLineStartsAtColumnOne(string source)
    {
        var statements = Parse(source).Statements.ToArray();
        Assert.Equal(1, statements[0].Start.Start.Column);
        Assert.Equal(1, statements[^1].Start.Start.Column);
    }

    [Theory]
    [InlineData("abc;", 1, 1)]
    [InlineData("  abc;", 1, 3)]
    [InlineData("x;\n  abc;", 2, 3)]
    [InlineData("x;\n\n\tabc;", 3, 2)]
    [InlineData("/* one\n two */ abc;", 2, 9)]
    public void ColumnsAreOneBasedOnEveryLine(string source, int line, int column)
    {
        var start = Parse(source).Statements.ToArray()[^1].Start.Start;
        Assert.Equal(line, start.Line);
        Assert.Equal(column, start.Column);
    }

    [Theory]
    [InlineData("a;\n)", 2, 1)]
    [InlineData("a;\n  )", 2, 3)]
    [InlineData(")", 1, 1)]
    public void ParseErrorsNameAOneBasedColumn(string source, int line, int column)
    {
        var token = ParseError(source).Token.Start;
        Assert.Equal(line, token.Line);
        Assert.Equal(column, token.Column);
    }
}
