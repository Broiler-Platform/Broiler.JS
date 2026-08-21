using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Tests;

public class AstTests
{
    [Fact(Timeout = 600000)]
    public void StringSpan_FromString_HasCorrectLengthAndValue()
    {
        var span = new StringSpan("hello");
        Assert.Equal(5, span.Length);
        Assert.Equal("hello", span.Value);
    }

    [Fact(Timeout = 600000)]
    public void StringSpan_Substring_CreatesView()
    {
        var span = new StringSpan("hello world", 0, 5);
        Assert.Equal("hello", span.Value);
        Assert.Equal(5, span.Length);
    }

    [Fact(Timeout = 600000)]
    public void StringSpan_IsEmpty_ForNullOrEmpty()
    {
        var empty = new StringSpan("");
        Assert.True(empty.IsEmpty);

        var nonEmpty = new StringSpan("x");
        Assert.False(nonEmpty.IsEmpty);
    }

    [Fact(Timeout = 600000)]
    public void StringSpan_Equals_MatchesString()
    {
        var span = new StringSpan("test");
        Assert.True(span.Equals("test"));
        Assert.False(span.Equals("other"));
    }

    [Fact(Timeout = 600000)]
    public void FastToken_Constructor_SetsType()
    {
        var token = new FastToken(TokenTypes.Number, number: 42.0);
        Assert.Equal(TokenTypes.Number, token.Type);
        Assert.Equal(42.0, token.Number);
    }
}
