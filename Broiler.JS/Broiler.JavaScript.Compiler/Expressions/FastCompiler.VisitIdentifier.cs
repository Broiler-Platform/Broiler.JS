using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    // Resolves <paramref name="name"/> to an EvalShadowVariable binding when the
    // current position is inside a sloppy function whose parameter list contains a
    // direct eval (<see cref="evalShadowBoundary"/>) AND the name resolves to a
    // binding OUTSIDE that function. The shadow forwards to the outer binding until
    // the eval introduces the var, and — being an ordinary function-scope binding —
    // is captured by closures so they observe the eval-introduced value even after
    // the function returns. Returns false (use ordinary resolution) for names bound
    // inside the boundary function or names with no outer binding to forward to.
    private bool TryResolveEvalShadow(in StringSpan name, out FastFunctionScope.VariableScope shadow)
    {
        shadow = null;
        var boundary = evalShadowBoundary;
        if (boundary == null)
            return false;

        if (name.Equals("this") || name.Equals("arguments") || name.Equals("eval")
            || name.Equals("undefined") || name.Equals("super"))
            return false;

        // A `with` object environment changes resolution dynamically; do not shadow.
        if (withBoundaries.Count != 0)
            return false;

        var withinBoundary = true;
        FastFunctionScope.VariableScope outer = null;
        var outerIsGlobal = false;
        for (var s = scope.Top; s != null; s = s.Parent)
        {
            if (s.TryGetOwnVariable(name, out var v) && v.Variable != null)
            {
                if (v.IsEvalShadow)
                {
                    shadow = v;
                    return true;
                }

                if (withinBoundary)
                    return false; // an ordinary binding declared inside the boundary function

                outer = v; // a binding in an enclosing scope: shadow it
                outerIsGlobal = s.Function == null; // the program/global var environment
                break;
            }

            if (ReferenceEquals(s, boundary))
                withinBoundary = false;
        }

        if (outer == null)
            return false; // undeclared name: nothing to forward to

        shadow = boundary.CreateEvalShadow(name, outer.Variable, outerIsGlobal);
        return true;
    }

    // Whether <paramref name="functionDeclaration"/> has a direct `eval(...)` call
    // somewhere in a parameter initializer (not nested inside another function).
    // Such a call can introduce parameter-environment vars that must shadow outer
    // bindings for the body and the closures created in the parameter list/body.
    private static bool ParametersContainDirectEval(AstFunctionExpression functionDeclaration)
    {
        var detector = new ParameterDirectEvalDetector();
        var parameters = functionDeclaration.Params.GetFastEnumerator();
        while (parameters.MoveNext(out var parameter))
        {
            if (parameter.Identifier != null)
                detector.Visit(parameter.Identifier);
            if (parameter.Init != null)
                detector.Visit(parameter.Init);

            if (detector.Found)
                return true;
        }

        return false;
    }

    // Finds a direct `eval(...)` call in an expression, without descending into
    // nested functions or classes (which establish their own scopes).
    private sealed class ParameterDirectEvalDetector : Broiler.JavaScript.Ast.AstReduce
    {
        public bool Found { get; private set; }

        protected override AstNode VisitCallExpression(AstCallExpression callExpression)
        {
            if (callExpression.Callee is AstIdentifier callee && callee.Name.Equals("eval"))
            {
                Found = true;
                return callExpression;
            }

            return base.VisitCallExpression(callExpression);
        }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
            => functionExpression;

        protected override AstNode VisitClassStatement(AstClassExpression classStatement)
            => classStatement;
    }

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
        {
            var thisExpression = scope.Top.ThisExpression;
            if (scope.Top.RootScope.MemberInits != null)
                return JSValueExtensionsBuilder.Coalesce(thisExpression, JSExceptionBuilder.Throw("Must call super constructor before accessing 'this'"));

            return thisExpression;
        }

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

        if (TryResolveEvalShadow(identifier.Name, out var shadow))
            return shadow.Expression;

        if (TryGetStaticIdentifierVariable(identifier, out var variable) && variable != null)
            return variable.Expression;

        return throwIfMissing
            ? JSContextBuilder.ResolveIdentifier(KeyOfName(identifier.Name))
            : JSContextBuilder.ResolveIdentifierOrUndefined(KeyOfName(identifier.Name));
    }
}
