using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitMeta(AstMeta astMeta)
    {
        if (astMeta.Identifier.Name.Equals("import") && astMeta.Property.Name.Equals("meta"))
            return ImportMeta(astMeta);

        // only new.target is supported....
        if (!(astMeta.Identifier.Name.Equals("new") && astMeta.Property.Name.Equals("target")))
            throw JSEngine.NewSyntaxError($"{astMeta.Identifier.Name}.{astMeta.Property} not supported");

        // new.target is only legal inside an ordinary (non-arrow) function — a function,
        // method, constructor, or accessor — or a class element initializer. At the
        // program/script top level (including a top-level arrow, which only inherits an
        // enclosing ordinary function's binding) it is an early SyntaxError. Direct eval
        // validates its own new.target placement (DirectEvalSupport), so it is exempt.
        if (!isDirectEvalCompilation && !inMemberInitializer && !EnclosedByOrdinaryFunction(scope.Top))
            throw new FastParseException(astMeta.Start, "new.target expression is not allowed here");

        // Inside a function, new.target resolves to the lexically captured cell
        // (which an arrow function inherits from its enclosing ordinary function).
        // At the program/root level there is no cell, so read the live value — except
        // in a direct eval, whose top-level new.target shares the caller's [[NewTarget]]
        // threaded in by PerformEval (a function declared inside the eval still gets its
        // own cell and so keeps using NewTargetExpression).
        if (scope.Top.NewTargetExpression != null)
            return scope.Top.NewTargetExpression;

        return isDirectEvalCompilation ? JSContextBuilder.DirectEvalNewTarget : JSContextBuilder.NewTarget();
    }

    /// <summary>
    /// <c>import.meta</c> — the host-defined object belonging to the module being evaluated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It compiles to a read of <c>meta</c> off the module record the body already receives as its
    /// <c>module</c> parameter, so the object's identity, its lazy creation and everything on it
    /// belong to the module host rather than to the compiler. That is what keeps
    /// <c>import.meta === import.meta</c> true and lets a host with its own key form report its own
    /// URL, without the compiler knowing what a module key is.
    /// </para>
    /// <para>
    /// The two error paths are separate on purpose. Outside module code <c>import.meta</c> is an
    /// early SyntaxError per ES2025 §13.3.12 — a script, an ordinary <c>eval</c>, a
    /// <c>Function</c> body — and that is what a feature-detect written as
    /// <c>try { eval('import.meta') }</c> expects to see. Inside module code compiled without a
    /// module record — a host that compiles module source without binding <c>module</c> — the
    /// construct is legal but has nothing to read, so it is a deterministic ReferenceError naming
    /// that rather than a silent <c>undefined</c> a page would then dereference.
    /// </para>
    /// </remarks>
    private BExpression ImportMeta(AstMeta astMeta)
    {
        if (!isModuleCompilation)
            throw new FastParseException(astMeta.Start, "Cannot use 'import.meta' outside a module");

        var module = scope.Top.GetVariable("module");
        if (module?.Expression == null)
            throw JSEngine.NewReferenceError("import.meta is not available: this module has no module record");

        return JSValueBuilder.Index(module.Expression, KeyOfName("meta"));
    }
}
