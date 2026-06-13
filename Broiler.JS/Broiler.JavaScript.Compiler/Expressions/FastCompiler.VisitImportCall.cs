using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitImportCall(AstImportCall node)
    {
        var specifier = VisitExpression(node.Source);

        // Resolve the host-provided `import` loader the way a bare identifier reference would:
        // inside a module it is the injected loader parameter; in a plain script it resolves to
        // the (absent) global, so actually calling it throws — but `import(...)` still compiles,
        // which is all an unexecuted ImportCall (e.g. one inside an uncalled arrow) needs.
        var importVar = scope.Top.GetVariable("import");
        var importFn = importVar?.Expression ?? JSContextBuilder.ResolveIdentifierOrUndefined(KeyOfName("import"));

        // import(specifier) → import(undefined-this, specifier). A second options argument, when
        // present, is evaluated (for its observable side effects) and forwarded; the loader
        // ignores any extra argument. The host import function returns a Promise.
        var args = node.Options != null
            ? ArgumentsBuilder.New(JSUndefinedBuilder.Value, specifier, VisitExpression(node.Options))
            : ArgumentsBuilder.New(JSUndefinedBuilder.Value, specifier);

        return JSFunctionBuilder.InvokeFunction(importFn, args);
    }
}
