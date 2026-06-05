using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for the precedence and associativity of the `**`
// (exponentiation) operator. Discovered while fixing issue #650: `**` was the
// loosest binary operator and left-associative, so e.g. `2 ** 53 - 1` parsed as
// `2 ** (53 - 1)`. Per spec `**` binds tighter than multiplicative operators and
// is right-associative.
public class ExponentiationPrecedenceTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval("'' + (" + code + ")").ToString();
    }

    [Theory]
    // tighter than additive
    [InlineData("2 ** 53 - 1", "9007199254740991")]
    [InlineData("5 - 2 ** 2", "1")]
    [InlineData("2 ** 2 + 1", "5")]
    // tighter than multiplicative
    [InlineData("2 * 3 ** 2", "18")]
    [InlineData("2 ** 3 * 2", "16")]
    [InlineData("16 / 2 ** 3", "2")]
    // right-associative
    [InlineData("2 ** 3 ** 2", "512")]
    [InlineData("2 ** 2 ** 3", "256")]
    // plain and parenthesised forms
    [InlineData("2 ** 3", "8")]
    [InlineData("(2 ** 3) ** 2", "64")]
    [InlineData("(-2) ** 2", "4")]
    // unary operator is permitted as the right operand
    [InlineData("2 ** -1", "0.5")]
    // comparison binds looser than exponentiation
    [InlineData("2 ** 3 > 2 ** 2", "true")]
    public void RespectsPrecedenceAndAssociativity(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // `**=` continues to work and evaluates its right side as an exponentiation.
    [Fact]
    public void AssignExponentiation()
        => Assert.Equal("512", Eval("(function(){ var x = 2; x **= 3 ** 2; return x; })()"));

    private static string ConstructorOnThrow(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"var c='no-throw'; try {{ eval({Json(expr)}); }} catch (e) {{ c = e.constructor.name; }} c").ToString();
    }

    private static string Json(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    [Theory]
    // An unparenthesised unary expression is not a valid `**` left operand.
    [InlineData("-2 ** 2")]
    [InlineData("+2 ** 2")]
    [InlineData("!1 ** 2")]
    [InlineData("~1 ** 2")]
    [InlineData("typeof 2 ** 2")]
    [InlineData("void 2 ** 2")]
    [InlineData("delete x ** 2")]
    // the unary is outside the parentheses, so still ambiguous
    [InlineData("-(2) ** 2")]
    public void UnaryLeftOperandIsSyntaxError(string expr)
        => Assert.Equal("SyntaxError", ConstructorOnThrow(expr));

    [Theory]
    // Parentheses disambiguate; ++/-- are UpdateExpressions; unary on the right is fine.
    [InlineData("(-2) ** 2", "4")]
    [InlineData("(typeof 2) ** 0", "1")]
    [InlineData("2 ** -1", "0.5")]
    [InlineData("(function(){ var x = 2; return x++ ** 2; })()", "4")]
    [InlineData("(function(){ var y = 2; return ++y ** 2; })()", "9")]
    public void ValidExponentiationLeftOperands(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // Returns the JS error-constructor name from compiling `async () => { body }`,
    // or "ok" when it compiles. Exercises the early error inside async code.
    private static string AsyncCompileResult(string body)
    {
        using var ctx = new JSContext();
        var src = "(async function(){ " + body + " })";
        return ctx.Eval($"var c='ok'; try {{ eval({Json(src)}); }} catch (e) {{ c = e.constructor.name; }} c").ToString();
    }

    [Theory]
    // An `await` UnaryExpression cannot be the left operand of `**`.
    [InlineData("return await x ** 2;")]
    [InlineData("return await x ** 2 + 1;")]
    [InlineData("return await x.y ** 2;")]
    [InlineData("return await (x) ** 2;")]
    [InlineData("return 1 + await x ** 2;")]
    public void AwaitLeftOperandOfExponentIsSyntaxError(string body)
        => Assert.Equal("SyntaxError", AsyncCompileResult(body));

    [Theory]
    // Parentheses, lower-precedence operators, and unary-on-the-right are all fine.
    [InlineData("return await (x ** 2);")]
    [InlineData("return await x + 2;")]
    [InlineData("return (await x) ** 2;")]
    [InlineData("return await x * 2 ** 3;")]
    [InlineData("return await x;")]
    public void ValidAwaitWithExponent(string body)
        => Assert.Equal("ok", AsyncCompileResult(body));
}
