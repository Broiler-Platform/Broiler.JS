using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

public class Issue912P3Char
{
    private static string Raw(string code)
    {
        using var ctx = new JSContext();
        try { return "" + ctx.Eval(code); }
        catch (System.Exception e) { return e.GetType().Name + (e is JSException je ? ":" + je.Message : ""); }
    }

    // The exact annexB/language/function-code/block-decl-func-skip-arguments.js (simple params).
    [Fact]
    public void FullSimpleParams()
    {
        var code = @"
(function() {
  var log = [];
  log.push(arguments.toString());
  {
    log.push(typeof arguments);
    log.push('' + arguments());
    function arguments() {}
    log.push('' + arguments());
  }
  log.push(arguments.toString());
  return log.join('|');
}());";
        Assert.Equal("[object Arguments]|function|undefined|undefined|[object Arguments]", Raw(code));
    }

    // The leak in isolation: after the block, `arguments` must still be the arguments object.
    [Fact]
    public void NoLeakAfterBlock()
        => Assert.Equal("[object Arguments]", Raw("(function(){ { function arguments(){} } return arguments.toString(); }(1,2,3))"));

    // The exact test262 file: all three parameter forms (simple, single named, rest).
    [Theory]
    [InlineData("function()")]
    [InlineData("function(x)")]
    [InlineData("function(..._)")]
    public void FullTest262Body(string head)
    {
        var code = @"
var __ok = 'pass';
function sv(a, b, m) { if (a !== b) __ok = m + ': ' + a; }
(" + head + @" {
  sv(arguments.toString(), '[object Arguments]', 'before');
  {
    sv(arguments(), undefined, 'call-before-decl');
    function arguments() {}
    sv(arguments(), undefined, 'call-after-decl');
  }
  sv(arguments.toString(), '[object Arguments]', 'after');
}());
__ok;";
        Assert.Equal("pass", Raw(code));
    }

    // An arrow has no arguments object: a block `function arguments` there still hoists.
    [Fact]
    public void ArrowBlockArgumentsStillHoists()
        => Assert.Equal("function", Raw("var f = () => { { function arguments(){} } return typeof arguments; }; f();"));
}
