using Broiler.JavaScript.Engine;
namespace Broiler.JavaScript.Integration.Tests;
public class ReproT
{
    private static string Eval(string code){ using var ctx = new JSContext(); return ctx.Eval(code).ToString(); }
    [Xunit.Fact]
    public void Probe()
    {
        foreach (var (l,c) in new[]{
          ("abc-empty-opt","JSON.stringify(/abc()?/.exec('abc-------abc'))"),
          ("a-opt-no-match","JSON.stringify(/(a)?/.exec('b'))"),
          ("a-opt-match","JSON.stringify(/(a)?/.exec('a'))"),
          ("nonempty-opt","JSON.stringify(/a(b)?c/.exec('ac'))"),
          ("nonempty-opt2","JSON.stringify(/a(b)?c/.exec('abc'))"),
          ("alt","JSON.stringify(/(x)|(y)/.exec('y'))"),
        }) System.Console.WriteLine($"R {l} :: {Eval(c)}");
    }
}
