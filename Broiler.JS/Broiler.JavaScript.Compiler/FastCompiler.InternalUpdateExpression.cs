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
                list.Add(YExpression.Assign(key.Variable, YExpression.Call(null, NormalizeUpdatePropertyKeyMethod, VisitExpression(memberExpression.Property))));
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

        switch (updateExpression.Operator)
        {
            case UnaryOperator.Increment:
                list.Add(YExpression.Assign(right, JSValueBuilder.AddDouble(right, YExpression.Constant((double)1))));
                break;

            case UnaryOperator.Decrement:
                list.Add(YExpression.Assign(right, JSValueBuilder.AddDouble(right, YExpression.Constant((double)-1))));
                break;
        }

        if (!updateExpression.Prefix)
        {
            list.Add(@return.Variable);
        }
        else
        {
            list.Add(right);
        }

        var r = YExpression.Block(list);
        @return?.Dispose();
        key?.Dispose();
        target?.Dispose();

        return r;
    }
}
