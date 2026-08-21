using System.Reflection;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Integration.Tests;

/// <summary>
/// The exit gate of <c>docs/roadmap/AssemblySplit.md</c>, asserted rather than described.
/// </summary>
/// <remarks>
/// The split exists so that a configuration of this engine can run JavaScript without
/// <c>System.Reflection.Emit</c>. That property is invisible in ordinary use — everything still
/// works if the edge comes back, right up until someone tries to trim or AOT-compile. These two
/// assertions are what make its loss a failing test instead of a discovery.
/// </remarks>
public class AssemblySplitValidationTests
{
    /// <summary>
    /// Assemblies that carry <c>System.Reflection.Emit</c>. The API is spread over three
    /// facades — <c>TypeBuilder</c>, <c>ILGenerator</c> and <c>DynamicMethod</c> each live in a
    /// different one — so naming only the first would let a reference through.
    /// </summary>
    private static readonly string[] ReflectionEmitAssemblies =
    [
        "System.Reflection.Emit",
        "System.Reflection.Emit.ILGeneration",
        "System.Reflection.Emit.Lightweight",
    ];

    private static readonly (string Name, Assembly Assembly)[] MustStayEmitFree =
    [
        ("Broiler.JavaScript.Expressions", typeof(BExpression).Assembly),
        ("Broiler.JavaScript.Ast", typeof(AstNode).Assembly),
        ("Broiler.JavaScript.Storage", typeof(PropertySequence).Assembly),
        ("Broiler.JavaScript.Parser", typeof(FastParser).Assembly),
    ];

    [Fact(Timeout = 600000)]
    public void ModelAndFrontEnd_DoNotReferenceReflectionEmit()
    {
        foreach (var (name, assembly) in MustStayEmitFree)
        {
            var emitRefs = assembly.GetReferencedAssemblies()
                .Select(r => r.Name!)
                .Where(r => ReflectionEmitAssemblies.Contains(r))
                .ToList();

            Assert.True(
                emitRefs.Count == 0,
                $"{name} references {string.Join(", ", emitRefs)}. The point of the assembly "
                + "split is that it does not — see docs/roadmap/AssemblySplit.md.");
        }
    }

    [Fact(Timeout = 600000)]
    public void FrontEnd_DoesNotDependOnTheEmitter_TransitivelyOrOtherwise()
    {
        foreach (var (name, assembly) in MustStayEmitFree)
        {
            var closure = BroilerClosureOf(assembly);

            Assert.False(
                closure.Contains("Broiler.JavaScript.ExpressionCompiler"),
                $"{name} reaches the IL emitter through {string.Join(" -> ", closure)}. "
                + "It should reach Broiler.JavaScript.Expressions instead.");
        }
    }

    /// <summary>
    /// Every Broiler assembly reachable from <paramref name="root"/>, following references only
    /// through Broiler assemblies — a reference to the emitter three hops down still counts.
    /// </summary>
    private static HashSet<string> BroilerClosureOf(Assembly root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            foreach (var reference in queue.Dequeue().GetReferencedAssemblies())
            {
                var name = reference.Name;
                if (name == null || !name.StartsWith("Broiler.", StringComparison.Ordinal))
                    continue;
                if (!seen.Add(name))
                    continue;

                // A reference that cannot be loaded cannot be walked, but it has already been
                // recorded above, which is what the assertion reads.
                try
                {
                    queue.Enqueue(Assembly.Load(reference));
                }
                catch (FileNotFoundException)
                {
                }
                catch (FileLoadException)
                {
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        return seen;
    }
}
