using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;
using System;
using System.Reflection;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly MethodInfo NormalizeUpdatePropertyKeyMethod = typeof(JSValue)
        .GetMethod("NormalizePropertyKey", BindingFlags.NonPublic | BindingFlags.Static, [typeof(JSValue)])
        ?? throw new InvalidOperationException("JSValue.NormalizePropertyKey(JSValue) not found");

    private YExpression InternalVisitUpdateExpression(AstUnaryExpression updateExpression)
    {
        // added support for a++, a--
        updateExpression.Argument.VerifyIdentifierForUpdate(IsStrictMode);

        if (updateExpression.Argument is AstIdentifier identifier)
        {
            if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable == null)
            {
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<YParameterExpression> { current.Variable };
                var globalKey = KeyOfName(identifier.Name);
                var statements = new Sequence<YExpression>
                {
                    YExpression.Assign(current.Variable, JSContextBuilder.ResolveIdentifier(globalKey))
                };

                if (previous != null)
                {
                    variables.Add(previous.Variable);
                    statements.Add(YExpression.Assign(previous.Variable, current.Expression));
                }

                statements.Add(YExpression.Assign(
                    current.Variable,
                    JSValueBuilder.AddDouble(
                        current.Expression,
                        YExpression.Constant(updateExpression.Operator == UnaryOperator.Increment ? 1d : -1d))));
                statements.Add(JSContextBuilder.AssignIdentifier(globalKey, current.Expression));
                statements.Add(previous?.Expression ?? current.Expression);

                return YExpression.Block(variables, statements);
            }
        }

        var list = new Sequence<YExpression>();

        FastFunctionScope.VariableScope target = null;
        FastFunctionScope.VariableScope key = null;
        FastFunctionScope.VariableScope @return = null;
        var right = VisitExpression(updateExpression.Argument);

        if (updateExpression.Argument is AstMemberExpression memberExpression)
        {
            target = scope.Top.GetTempVariable(typeof(JSValue));
            list.Add(YExpression.Assign(target.Variable, VisitExpression(memberExpression.Object)));

            if (memberExpression.Computed)
            {
                key = scope.Top.GetTempVariable(typeof(JSValue));
                list.Add(YExpression.Assign(key.Variable, VisitExpression(memberExpression.Property)));
                // Per spec, ToObject(base) must precede ToPropertyKey(key).
                // RequireObjectCoercible throws TypeError for null/undefined before
                // NormalizePropertyKey can trigger observable side effects (e.g. toString).
                list.Add(YExpression.Call(null, RequireObjectCoercibleMethod, target.Expression));
                list.Add(YExpression.Assign(key.Variable, YExpression.Call(null, NormalizeUpdatePropertyKeyMethod, key.Expression)));
                right = JSValueBuilder.Index(target.Expression, key.Expression);
            }
            else
            {
                right = CreateMemberExpression(target.Expression, memberExpression.Property, false);
            }
        }

        switch (right.NodeType)
        {
            case YExpressionType.Index:
                if (target == null)
                {
                    var index = right as YIndexExpression;
                    target = scope.Top.GetTempVariable(index.Type);
                    list.Add(YExpression.Assign(target.Variable, index.Target));
                    right = YExpression.Index(target.Variable, index.Property, index.Arguments);
                }
                break;
        }

        if (!updateExpression.Prefix)
        {
            @return = scope.Top.GetTempVariable(right.Type);
            list.Add(YExpression.Assign(@return.Variable, right));
        }

        var newValue = updateExpression.Operator == UnaryOperator.Increment
            ? JSValueBuilder.AddDouble(right, YExpression.Constant((double)1))
            : JSValueBuilder.AddDouble(right, YExpression.Constant((double)-1));

        if (updateExpression.Prefix)
        {
            // For prefix update on member expressions, save the computed new value
            // before writing it back. The write may silently fail (e.g. non-writable
            // property in sloppy mode), but the expression must return the new value.
            @return = scope.Top.GetTempVariable(typeof(JSValue));
            list.Add(YExpression.Assign(@return.Variable, newValue));
            list.Add(YExpression.Assign(right, @return.Variable));
        }
        else
        {
            list.Add(YExpression.Assign(right, newValue));
        }

        list.Add(@return.Variable);

        var r = YExpression.Block(list);
        @return?.Dispose();
        key?.Dispose();
        target?.Dispose();

        return r;
    }
}
