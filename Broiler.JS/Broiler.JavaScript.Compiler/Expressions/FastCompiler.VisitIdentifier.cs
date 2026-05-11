using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitIdentifier(AstIdentifier identifier) => VisitIdentifier(identifier, true);

    private YExpression VisitIdentifierReference(AstIdentifier identifier)
    {
        var variable = scope.Top.GetVariable(identifier.Name, true);
        if (variable != null)
            return variable.Expression;

        return JSContextBuilder.Index(KeyOfName(identifier.Name));
    }

    private YExpression VisitIdentifier(AstIdentifier identifier, bool throwIfMissing)
    {
        if (identifier.Name.Equals("undefined"))
            return JSUndefinedBuilder.Value;

        if (identifier.Name.Equals("this"))
            return scope.Top.ThisExpression;

        if (identifier.Name.Equals("arguments"))
        {
            var functionScope = scope.Top.RootScope;
            var vs = functionScope.CreateVariable("arguments", JSArgumentsBuilder.New(functionScope.ArgumentsExpression));
            return vs.Expression;
        }

        var variable = scope.Top.GetVariable(identifier.Name, true);
        if (variable != null)
            return variable.Expression;

        return throwIfMissing
            ? JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name))
            : JSContextBuilder.Index(KeyOfName(identifier.Name));
    }
}
