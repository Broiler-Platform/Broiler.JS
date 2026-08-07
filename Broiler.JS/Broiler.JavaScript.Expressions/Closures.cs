using System.Reflection;

namespace Broiler.JavaScript.ExpressionCompiler;

public class Closures(IMethodRepository repository, ClosureSeparator.Box[] boxes, string il, string exp)
{
    // The emitter assembly needs these handles to emit field and constructor access, and it is
    // no longer the same assembly. Public rather than InternalsVisibleTo — see AssemblySplit.md.
    public static FieldInfo repositoryField = typeof(Closures).GetField(nameof(Repository));
    public static FieldInfo boxesField = typeof(Closures).GetField(nameof(Boxes));
    public static ConstructorInfo constructor = typeof(Closures).GetConstructors()[0];

    public readonly IMethodRepository Repository = repository;
    public readonly ClosureSeparator.Box[] Boxes = boxes;
    public readonly string IL = il;
    public readonly string Exp = exp;
}
