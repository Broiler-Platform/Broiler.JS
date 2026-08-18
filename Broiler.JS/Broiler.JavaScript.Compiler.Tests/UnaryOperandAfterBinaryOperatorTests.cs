using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A binary operator whose right operand begins with a prefix `+` or `-` of the SAME token type
// dropped that unary operator, so `a * b - -1` evaluated as `a * b - 1`.
//
// The cause was in the parser (FastParser.NextExpression): after a binary operator matched, it was
// consumed once at the top of the operator loop AND again by the `CheckAndConsume(previousType)`
// that runs when the right-operand recursion re-enters with the operator as its `previousType`.
// The extra consume is a no-op for any token that is not the operator — which is why this went
// unnoticed — but `+`/`-` are also prefix operators, so `- -1`'s leading unary minus is the same
// `Minus` token and CheckAndConsume swallowed it. Only the first operator of the loop was affected,
// so `x - -1 - -1` lost exactly one negation (evaluated `x - 1 - -1`).
//
// The value is what matters here, so every case asserts the evaluated result. This is the exact
// shape minified code uses constantly (`2*(W|0)- -1+~W+(Q&~W)` in Google's botguard bundle), and
// getting it wrong made that bundle compute a wrong challenge answer and then crash downstream.
public sealed class UnaryOperandAfterBinaryOperatorTests
{
    private static string Run(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    [Theory]
    // The minimal reproductions: the right operand's unary minus must survive.
    [InlineData("2 - -1", "3")]
    [InlineData("2 * 3 - -1", "7")]
    [InlineData("2 / 1 - -1", "3")]
    [InlineData("2 % 3 - -1", "3")]
    [InlineData("2 + 3 - -1", "6")]
    [InlineData("2 - 3 - -1", "0")]
    // Chained: each negation must be kept, not just the first.
    [InlineData("2 - -1 - -1", "4")]
    [InlineData("2 - -1 - -1 - -1", "5")]
    [InlineData("2 * 3 - -1 - -1", "8")]
    // A leading `+` unary operand behind a `+` binary operator is the mirror case.
    [InlineData("2 * 3 + +1", "7")]
    [InlineData("2 * 3 - +1", "5")]
    // Interaction with precedence, grouping and other operand kinds.
    [InlineData("2 * (128 | 0) - -1 + ~128 + (40 & ~128)", "168")]
    [InlineData("2 ** 2 - -1", "5")]
    [InlineData("2 - -1 * 3", "5")]
    [InlineData("2 - -1 + 3", "6")]
    [InlineData("2 * 3 - - -1", "5")]
    [InlineData("2 * 3 - -1e2", "106")]
    [InlineData("2 * 3 - -0x10", "22")]
    [InlineData("2 << 1 - -1", "8")]
    [InlineData("2 * 3 - (-1)", "7")]
    public void RightOperandUnarySignIsKept(string expression, string expected)
        => Assert.Equal(expected, Run(expression));

    [Theory]
    // Same shape with a variable / call / member operand on the left, so the fix is not specific to
    // literal-times-literal.
    [InlineData("var a = 2, b = 3; a * b - -1;", "7")]
    [InlineData("var a = 2, b = 3; a - b - -1;", "0")]
    [InlineData("function f(){ return 6; } f() - -1;", "7")]
    [InlineData("var o = { p: 6 }; o.p - -1;", "7")]
    [InlineData("var c = 128; 2 * c - -1;", "257")]
    public void RightOperandUnarySignIsKeptWithNonLiteralLeftOperand(string source, string expected)
        => Assert.Equal(expected, Run(source));
}
