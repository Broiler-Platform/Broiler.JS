using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

// A dynamic ImportCall — `import(specifier)` or `import(specifier, options)` (ES2020 §13.3.10).
// Unlike a static import declaration this is an expression that evaluates to a Promise.
public class AstImportCall(FastToken token, AstExpression source, AstExpression options, FastToken end)
    : AstExpression(token, FastNodeType.ImportCall, end)
{
    public readonly AstExpression Source = source;
    public readonly AstExpression Options = options; // the optional second argument, or null
}
