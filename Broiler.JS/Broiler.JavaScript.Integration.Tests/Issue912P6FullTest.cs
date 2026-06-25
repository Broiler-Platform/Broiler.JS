using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

public class Issue912P6FullTest
{
    private static string Raw(string code)
    {
        using var ctx = new JSContext();
        try { return "" + ctx.Eval(code); }
        catch (System.Exception e) { return e.GetType().Name + (e is JSException je ? ":" + je.Message : ""); }
    }

    // The full staging/sm/lexical-environment/block-scoped-functions-annex-b-label.js test.
    [Fact]
    public void FullP6()
    {
        var code = @"
var __ok = 'pass';
function assertThrowsSyntax(str) {
  var threw = false;
  try { eval(str); } catch (e) { threw = (e instanceof SyntaxError); }
  if (!threw) __ok = 'no-throw: ' + str;
  threw = false;
  try { eval('""use strict"";' + str); } catch (e) { threw = (e instanceof SyntaxError); }
  if (!threw) __ok = 'no-strict-throw: ' + str;
}
function expectSloppyPass(str) {
  try { eval(str); } catch (e) { __ok = 'sloppy-threw: ' + str + ' -> ' + e; }
  var threw = false;
  try { eval('""use strict"";' + str); } catch (e) { threw = (e instanceof SyntaxError); }
  if (!threw) __ok = 'strict-no-throw: ' + str;
}
expectSloppyPass(`l: function f1() {}`);
expectSloppyPass(`l0: l: function f1() {}`);
expectSloppyPass(`{ f1(); l: function f1() {} }`);
expectSloppyPass(`{ f1(); l0: l: function f1() {} }`);
expectSloppyPass(`{ f1(); l: function f1() { return 42; } } if (f1() !== 42) throw new Error('a');`);
expectSloppyPass(`eval(""fe(); l: function fe() {}"")`);
assertThrowsSyntax(`if (1) l: function f2() {}`);
assertThrowsSyntax(`if (1) {} else l: function f3() {}`);
assertThrowsSyntax(`do l: function f4() {} while (0)`);
assertThrowsSyntax(`while (0) l: function f5() {}`);
assertThrowsSyntax(`for (;;) l: function f6() {}`);
expectSloppyPass(`switch (1) { case 1: l: function f7() {} }`);
expectSloppyPass(`switch (1) { case 1: if (f8() !== 'f8') throw new Error('b'); case 2: l: function f8() { return 'f8'; } } if (f8() !== 'f8') throw new Error('c');`);
__ok;";
        Assert.Equal("pass", Raw(code));
    }
}
