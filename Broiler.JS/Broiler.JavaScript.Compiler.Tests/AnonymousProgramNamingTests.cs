using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Naming for programs that have no script of their own: eval, and the Function constructor's body.
//
// They were all compiled as "vm.js". One name for every such program makes a stack trace through a
// module loader unreadable, because the frames cannot be attributed: two frames reading
// `vm.js:1,14` and `vm.js:5060,25473` give no way to tell whether that is one program or two — and
// which it is decides whether the failing function was defined where it was called or somewhere
// else entirely. Numbering them (the way devtools shows VM123) makes each program identifiable.
public class AnonymousProgramNamingTests
{
    private static string StackOf(JSContext context, string script, string label) =>
        context.Eval($"(function(){{ try {{ {script} }} catch (e) {{ return e.stack; }} }})()", label).ToString();

    // The reported shape: one eval'd program defines a function, a second calls it. The two frames
    // have to name different programs.
    [Fact]
    public void TwoEvaluatedPrograms_AreNamedApart()
    {
        using var context = new JSContext();

        var stack = StackOf(
            context,
            "globalThis.NS = {};" +
            " (0,eval)('(function(_){ _.f = function(){ return nosuchvalue; }; })(globalThis.NS)');" +
            " (0,eval)('(function(){ return globalThis.NS.f(); })()');",
            "outer.js");

        var names = new HashSet<string>();
        foreach (var line in stack.Split('\n'))
        {
            var start = line.IndexOf("vm", StringComparison.Ordinal);
            if (start < 0)
                continue;
            var end = line.IndexOf(".js", start, StringComparison.Ordinal);
            if (end > start)
                names.Add(line.Substring(start, end - start + 3));
        }

        // Both programs appear, under names that differ.
        Assert.True(names.Count >= 2, $"expected at least two distinct program names, got [{string.Join(", ", names)}] in: {stack}");
        Assert.DoesNotContain("vm.js", names);
    }

    // Distinct sources get distinct names, so a name never covers two different programs.
    // (Re-evaluating the *same* source may reuse a cached compilation, and reusing its name with
    // it is right — the name still identifies exactly one piece of code.)
    [Fact]
    public void DistinctEvaluatedSources_GetDistinctNames()
    {
        using var context = new JSContext();

        var first = StackOf(context, "(0,eval)('nosuchvalue;');", "outer.js");
        var second = StackOf(context, "(0,eval)('\\n nosuchvalue;');", "outer.js");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("vm.js", first);
        Assert.DoesNotContain("vm.js", second);
    }

    // A script that HAS a name keeps it — the numbering is only the fallback.
    [Fact]
    public void ANamedScript_KeepsItsName()
    {
        using var context = new JSContext();

        var stack = StackOf(context, "nosuchvalue;", "inline-3");

        Assert.Contains("inline-3", stack);
        Assert.DoesNotContain("vm.js", stack);
    }
}
