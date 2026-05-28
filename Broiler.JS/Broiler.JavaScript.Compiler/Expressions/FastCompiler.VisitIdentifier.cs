using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override YExpression VisitIdentifier(AstIdentifier identifier) => VisitIdentifier(identifier, true);

    private static bool IsScopeInsideWithBoundary(FastFunctionScope declarationScope, FastFunctionScope boundary)
    {
        if (ReferenceEquals(declarationScope, boundary))
            return false;

        for (var current = declarationScope.Parent; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, boundary))
                return true;
        }

        return false;
    }

    private bool TryGetStaticIdentifierVariable(AstIdentifier identifier, out FastFunctionScope.VariableScope variable)
    {
        variable = null;
        if (withBoundaries.Count == 0)
        {
            variable = scope.Top.GetVariable(identifier.Name, true);
            return true;
        }

        var boundary = withBoundaries.Peek();
        for (var current = scope.Top; current != null; current = current.Parent)
        {
            if (!current.TryGetOwnVariable(identifier.Name, out var ownVariable))
                continue;

            if (IsScopeInsideWithBoundary(current, boundary))
            {
                variable = ownVariable;
                return true;
            }

            return false;
        }

        return false;
    }

    private YExpression VisitIdentifierReference(AstIdentifier identifier)
    {
        if (identifier.Name.Equals("arguments")
            && scope.Top.Function?.IsArrowFunction == true)
        {
            if (parameterInitializerDepth > 0)
                return JSContextBuilder.Index(KeyOfName(identifier.Name));

            if (TryGetStaticIdentifierVariable(identifier, out var arrowVariable) && arrowVariable != null)
                return arrowVariable.Expression;

            return JSContextBuilder.Index(KeyOfName(identifier.Name));
        }

        if (TryGetStaticIdentifierVariable(identifier, out var variable) && variable != null)
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
            if (scope.Top.Function?.IsArrowFunction == true
            )
            {
                if (parameterInitializerDepth > 0)
                    return JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name));

                if (TryGetStaticIdentifierVariable(identifier, out var arrowVariable) && arrowVariable != null)
                    return arrowVariable.Expression;

                return throwIfMissing
                    ? JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name))
                    : JSContextBuilder.Index(KeyOfName(identifier.Name));
            }

            var functionScope = scope.Top.RootScope;
            var argumentsObject = JSArgumentsBuilder.New(functionScope.ArgumentsExpression);
            if (!IsStrictMode
                && functionScope.Function != null
                && HasSimpleParameterList(functionScope.Function.Params))
            {
                var parameters = new List<VariableDeclarator>();
                var parameterEnumerator = functionScope.Function.Params.GetFastEnumerator();
                while (parameterEnumerator.MoveNext(out var parameter))
                    parameters.Add(parameter);

                var parameterCount = parameters.Count;
                var mappedParameters = new YExpression[parameterCount];
                var seenNames = new HashSet<string>();

                for (var i = parameterCount - 1; i >= 0; i--)
                {
                    if (parameters[i].Identifier is not AstIdentifier parameterIdentifier)
                    {
                        mappedParameters[i] = YExpression.Constant(null, typeof(JSVariable));
                        continue;
                    }

                    var parameterName = parameterIdentifier.Name.Value;
                    if (!seenNames.Add(parameterName))
                    {
                        mappedParameters[i] = YExpression.Constant(null, typeof(JSVariable));
                        continue;
                    }

                    mappedParameters[i] = functionScope.GetVariable(parameterIdentifier.Name).Variable;
                }

                argumentsObject = JSArgumentsBuilder.NewMapped(functionScope.ArgumentsExpression, YExpression.NewArrayInit(typeof(JSVariable), mappedParameters));
            }

            var vs = functionScope.CreateVariable("arguments", argumentsObject);
            return vs.Expression;
        }

        if (TryGetStaticIdentifierVariable(identifier, out var variable) && variable != null)
            return variable.Expression;

        return throwIfMissing
            ? JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name))
            : JSContextBuilder.Index(KeyOfName(identifier.Name));
    }
}
