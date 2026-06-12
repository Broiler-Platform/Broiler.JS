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
public class Issue761Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

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
}
