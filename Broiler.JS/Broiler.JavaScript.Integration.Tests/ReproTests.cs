using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

public class ReproTests
{
    private static void Log(string s) => System.IO.File.AppendAllText(@"D:\Broiler.JS\repro-out.txt", s + "\n");

    private void Run(string name, string code)
    {
        try
        {
            using var ctx = new JSContext();
            var result = ctx.Eval(code);
            Log($"[{name}] OK => {result}");
        }
        catch (Exception ex)
        {
            Log($"[{name}] FAIL => {ex.GetType().Name}: {ex.Message?.Split('\n')[0]}");
        }
    }

    [Fact]
    public void Repro()
    {
        Run("field-eval-direct", "class B{foo(){return 1;}} class C extends B{x=eval('super.foo()');} new C().x;");
        Run("field-eval-arrow-call", "class B{foo(){return 2;}} class C extends B{x=eval('(()=>super.foo())()');} new C().x;");
        Run("field-eval-arrow-prop", "class B{foo(){return 3;}} class C extends B{x=eval('(()=>super.foo)()');} new C().x;");
        Run("field-explicit-eval-arrow", "class B{foo(){return 4;}} class C extends B{constructor(){super();} x=eval('(()=>super.foo())()');} new C().x;");
        Run("method-eval-arrow", "class B{foo(){return 5;}} class C extends B{m(){return eval('(()=>super.foo())()');}} new C().m();");
        Run("field-arrow-direct-noeval", "class B{foo(){return 6;}} class C extends B{x=(()=>super.foo())();} new C().x;");
        // nested arrow without eval, in field
        Run("field-eval-nested-fn", "class B{foo(){return 7;}} class C extends B{x=eval('var f=()=>super.foo(); f();');} new C().x;");
    }
}
