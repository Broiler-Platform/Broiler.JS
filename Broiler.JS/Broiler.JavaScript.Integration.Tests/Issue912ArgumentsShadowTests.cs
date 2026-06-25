using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression for #912 Problem 3 (lexical-shadowing half): a lexical binding named
// `arguments` declared in an inner block scope must shadow the function's arguments
// object within that block. Previously the `arguments` special-case in VisitIdentifier
// always materialised the arguments object, ignoring an inner let/const/class/block-fn.
public class Issue912ArgumentsShadowTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval("'' + (" + code + ")").ToString();
    }

    [Theory]
    [InlineData("(function(){ { let arguments = 5; return arguments; } }())", "5")]
    [InlineData("(function(){ { const arguments = 6; return arguments; } }())", "6")]
    [InlineData("(function(){ { function arguments(){ return 7; } return arguments(); } }())", "7")]
    [InlineData("(function(){ { var r = arguments(); function arguments(){ return 8; } return r; } }())", "8")]
    [InlineData("(function(){ { { let arguments = 9; return arguments; } } }())", "9")]
    [InlineData("(function(a, b){ { let arguments = 11; return arguments; } }(1, 2))", "11")]
    public void InnerBlockShadowsArgumentsObject(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // Outside the block, `arguments` is still the arguments object (no over-eager shadow).
    [Fact]
    public void ArgumentsObjectIntactOutsideBlock()
        => Assert.Equal("[object Arguments]", Eval("(function(){ { let arguments = 5; } return arguments.toString(); }(1,2,3))"));

    // A function-scope `var arguments` still aliases the arguments binding (unchanged).
    [Fact]
    public void VarArgumentsStillSharesBinding()
        => Assert.Equal("hello", Eval("(function(){ var arguments = 'hello'; return arguments; }(1,2,3))"));

    // Plain `arguments` (no shadow) is still the arguments object.
    [Fact]
    public void PlainArgumentsUnchanged()
        => Assert.Equal("3", Eval("(function(){ return arguments.length; }(1,2,3))"));
}
