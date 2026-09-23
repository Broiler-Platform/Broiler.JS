using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Parser;

namespace Broiler.JavaScript.Parser.Tests;

// CoverParenthesizedExpressionAndArrowParameterList (§13.2, §15.3) leniencies left open by the
// parser/intrinsics slice. The cover grammar admits `()`, a trailing comma and a rest element
// only so that an ArrowParameters can be recognised; as a ParenthesizedExpression (§13.2.1.1
// early errors: the cover must also match `( Expression )`) each of them is a SyntaxError.
//
//  - `(1,)`, `(a,)` and `()` parsed as expressions (the latter as an empty expression, so
//    `export default function () {}();` parsed);
//  - `(...a)` not followed by `=>` compiled and failed at run time instead of at parse time;
//  - `(...a,) => 1` and `(a, ...b,) => 1` were accepted: a rest element is always last, with
//    no trailing comma (ArrowFormalParameters / FormalParameters, §15.1);
//  - `((a)) => 1` and `(a, (b)) => 1` were accepted: a parenthesized expression is never a
//    binding identifier or pattern, nor a target inside one (`([(a)]) => 1`,
//    `({a: (b)}) => 1`);
//  - `(...a = []) => 1` parsed and then crashed the compiler (NotImplementedException): a rest
//    parameter takes no initializer.
public class CoverParenthesizedExpressionTests
{
    private static FastParseException ParseError(string source)
        => Assert.Throws<FastParseException>(
            () => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram());

    private static Ast.AstProgram Parse(string source)
        => new FastParser(new FastTokenStream(new StringSpan(source))).ParseProgram();

    [Theory]
    // A trailing comma belongs to ArrowParameters only.
    [InlineData("(1,)")]
    [InlineData("(a,)")]
    [InlineData("(a, b,)")]
    [InlineData("(,)")]
    [InlineData("x = (1,)")]
    [InlineData("f((1,))")]
    [InlineData("(1,).x")]
    [InlineData("(a,)\n=> 1")]
    // `()` is not an expression.
    [InlineData("()")]
    [InlineData("x = ()")]
    [InlineData("()\n=> 1")]
    [InlineData("export default function () {}();")]
    // A rest element belongs to ArrowParameters only …
    [InlineData("(...a)")]
    [InlineData("(a, ...b)")]
    [InlineData("x = (...a)")]
    // … is always last and never followed by a comma …
    [InlineData("(...a,) => 1")]
    [InlineData("(a, ...b,) => 1")]
    [InlineData("(...a, b) => 1")]
    [InlineData("async (...a,) => 1")]
    [InlineData("async (a, ...b,) => 1")]
    // … and a parenthesized expression is not a binding.
    [InlineData("((a)) => 1")]
    [InlineData("((a, b)) => 1")]
    [InlineData("(a, (b)) => 1")]
    [InlineData("(...(a)) => 1")]
    // … nor a target inside a binding pattern …
    [InlineData("([(a)]) => 1")]
    [InlineData("([a, [(b)]]) => 1")]
    [InlineData("({a: (b)}) => 1")]
    [InlineData("({a: (b) = 1}) => 1")]
    [InlineData("({a: (b = 1)}) => 1")]
    [InlineData("([...(a)]) => 1")]
    [InlineData("({...(a)}) => 1")]
    [InlineData("async ([(a)]) => 1")]
    // … and a rest parameter has no initializer.
    [InlineData("(...a = []) => 1")]
    [InlineData("async (...a = []) => 1")]
    public void CoverOnlyFormsAreSyntaxErrorsOutsideArrowParameters(string source) => ParseError(source);

    [Theory]
    [InlineData("(a,) => 1")]
    [InlineData("(a, b,) => 1")]
    [InlineData("() => 1")]
    [InlineData("(...a) => 1")]
    [InlineData("(a, ...b) => 1")]
    [InlineData("(...[a]) => 1")]
    [InlineData("(...{a}) => 1")]
    [InlineData("(a = (1, 2),) => 1")]
    // An initializer is an ordinary expression, parenthesized or not.
    [InlineData("([a = (1)], {b: c = (2)}, d = (3)) => 1")]
    [InlineData("([a, [b], {c: {d}}]) => 1")]
    [InlineData("async (a,) => 1")]
    [InlineData("async (...a) => 1")]
    [InlineData("async () => 1")]
    [InlineData("x = (a, b,) => { }")]
    [InlineData("f((a,) => 1, 2)")]
    public void ArrowParametersKeepTheirCoverForms(string source)
    {
        var expression = Assert.IsType<AstExpressionStatement>(Parse(source).Statements.Single()).Expression;
        Assert.NotNull(expression);
    }

    [Theory]
    // Calls keep their own trailing comma and spread, including a call of `async`.
    [InlineData("f(a,)")]
    [InlineData("f(...a)")]
    [InlineData("async(a,)")]
    [InlineData("async(...a)")]
    // Ordinary parenthesized expressions.
    [InlineData("(1, 2)")]
    [InlineData("((a))")]
    [InlineData("(a) = 1")]
    [InlineData("[(a)] = [1]")]
    [InlineData("({a: (b)} = {})")]
    public void OrdinaryParenthesesAndCallsStillParse(string source)
        => Assert.Single(Parse(source).Statements);

    [Fact]
    public void TrailingCommaArrowHasOneParameter()
    {
        var expression = Assert.IsType<AstExpressionStatement>(Parse("(a,) => a").Statements.Single()).Expression;
        var function = Assert.IsType<AstFunctionExpression>(expression);
        Assert.Single(function.Params);
    }
}
