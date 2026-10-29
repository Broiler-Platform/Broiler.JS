using System.Reflection;
using Broiler.JavaScript.Portable;

namespace Broiler.JavaScript.Portable.Tests;

/// <summary>
/// The bytecode compiler's whole reference closure is free of <c>System.Reflection.Emit</c>.
/// </summary>
/// <remarks>
/// <para>
/// This was not true before the model/emitter split. <c>Portable.Compiler</c> references
/// <c>Parser</c>, <c>Parser</c> referenced <c>ExpressionCompiler</c>, and
/// <c>ExpressionCompiler</c> was the IL emitter — so **the bytecode compiler transitively
/// depended on dynamic code**, which is the reason <c>Broiler.JavaScript.Portable</c> had to
/// exist as an island with no references at all.
/// </para>
/// <para>
/// It is the precondition <c>docs/roadmap/Phase-6.md</c> is waiting on: an AOT gate can only be
/// built on a closure that has no emitter in it. Asserting it here rather than in the assembly
/// split's own tests keeps it where it will be read — by whoever next touches the bytecode
/// path — and costs no new project reference.
/// </para>
/// </remarks>
public sealed class BytecodeClosureIsEmitFreeTests
{
    private static readonly string[] ReflectionEmitAssemblies =
    [
        "System.Reflection.Emit",
        "System.Reflection.Emit.ILGeneration",
        "System.Reflection.Emit.Lightweight",
    ];

    [Fact]
    public void PortableCompiler_ReferenceClosure_ContainsNoEmitter()
    {
        var offenders = new List<string>();

        foreach (var assembly in BroilerClosureOf(typeof(PortableCompiler).Assembly))
        {
            var emitRefs = assembly.GetReferencedAssemblies()
                .Select(r => r.Name!)
                .Where(r => ReflectionEmitAssemblies.Contains(r))
                .ToList();

            if (emitRefs.Count != 0)
                offenders.Add($"{assembly.GetName().Name} -> {string.Join(", ", emitRefs)}");
        }

        Assert.True(
            offenders.Count == 0,
            "The bytecode compiler's closure reaches System.Reflection.Emit through:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// Every Broiler assembly reachable from <paramref name="root"/>, itself included.
    /// </summary>
    private static List<Assembly> BroilerClosureOf(Assembly root)
    {
        var seen = new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [root.GetName().Name!] = root,
        };
        var queue = new Queue<Assembly>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            foreach (var reference in queue.Dequeue().GetReferencedAssemblies())
            {
                var name = reference.Name;
                if (name == null || !name.StartsWith("Broiler.", StringComparison.Ordinal))
                    continue;
                if (seen.ContainsKey(name))
                    continue;

                Assembly loaded;
                try
                {
                    loaded = Assembly.Load(reference);
                }
                catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
                {
                    // An assembly that will not load is one this configuration does not ship,
                    // so it cannot be carrying Emit into the closure at run time either.
                    continue;
                }

                seen[name] = loaded;
                queue.Enqueue(loaded);
            }
        }

        return [.. seen.Values];
    }
}
