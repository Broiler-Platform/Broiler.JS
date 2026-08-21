using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// These began as annexB/language/function-code/block-decl-func-skip-arguments.js, which asserts
// that the arguments object survives a block-level `function arguments(){}`. That file quotes the
// pre-2021 FunctionDeclarationInstantiation ("Append "arguments" to parameterNames"), and current
// 10.2.11 no longer says it: "arguments" is appended to paramBindings, while the Annex B condition
// tests paramNames, so the declaration's copy-out runs and overwrites the binding. What the gap in
// the wording still suppresses is one step in — creating the var binding — which is why the object
// is there on the way in. V8 and SpiderMonkey both answer this way, and it is what test262
// staging/sm/lexical-environment/block-scoped-functions-annex-b-arguments and
// staging/sm/regress/regress-602621 assert; the annexB file is the one out of step (#971).
public class Issue912P3Char
{
    private static string Raw(string code)
    {
        using var ctx = new JSContext();
        try { return "" + ctx.Eval(code); }
        catch (System.Exception e) { return e.GetType().Name + (e is JSException je ? ":" + je.Message : ""); }
    }

    // The shape of that annexB file, with the outcome current 10.2.11 gives (simple params).
    [Fact(Timeout = 600000)]
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
  log.push(typeof arguments);
  return log.join('|');
}());";
        Assert.Equal("[object Arguments]|function|undefined|undefined|function", Raw(code));
    }

    // The copy-out in isolation: after the block, `arguments` is the declared function.
    [Fact(Timeout = 600000)]
    public void FunctionOverwritesArgumentsAfterBlock()
        => Assert.Equal("function", Raw("(function(){ { function arguments(){} } return typeof arguments; }(1,2,3))"));

    // The var binding is NOT created, so the object is intact until the declaration evaluates.
    [Fact(Timeout = 600000)]
    public void ArgumentsObjectSurvivesUntilTheDeclarationRuns()
        => Assert.Equal("[object Arguments]", Raw("(function(){ var s = arguments.toString(); { function arguments(){} } return s; }(1,2,3))"));

    // A parameter really spelled `arguments` IS in paramNames, so the whole step is skipped.
    [Fact(Timeout = 600000)]
    public void NamedArgumentsParameterBlocksTheHoist()
        => Assert.Equal("number", Raw("(function(arguments){ { function arguments(){} } return typeof arguments; }(1))"));

    // The three parameter forms the annexB file covers (simple, single named, rest).
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
  sv(typeof arguments, 'function', 'after');
}());
__ok;";
        Assert.Equal("pass", Raw(code));
    }

    // An arrow has no arguments object: a block `function arguments` there still hoists.
    [Fact(Timeout = 600000)]
    public void ArrowBlockArgumentsStillHoists()
        => Assert.Equal("function", Raw("var f = () => { { function arguments(){} } return typeof arguments; }; f();"));
}
