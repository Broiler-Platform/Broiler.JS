using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitBinaryExpression(AstBinaryExpression binaryExpression)
    {
        var @operator = binaryExpression.Operator;

        if (@operator > TokenTypes.BeginAssignTokens && @operator < TokenTypes.EndAssignTokens)
            return VisitAssignmentExpression(binaryExpression.Left, @operator, binaryExpression.Right);

        // Ergonomic brand check: `#name in rval`. A leading PrivateIdentifier is only
        // valid as the left operand of `in`; resolve it to its private-name key and
        // emit a brand-presence test rather than a normal `in` (which would try to
        // resolve `#name` as a variable).
        if (@operator == TokenTypes.In
            && binaryExpression.Left is AstIdentifier privateLeft
            && privateLeft.Name.Length > 0
            && privateLeft.Name.Value[0] == '#')
        {
            return JSObjectBuilder.PrivateNameIn(
                KeyOfPrivateName(privateLeft.Name),
                ToJSValueExpression(Visit(binaryExpression.Right)));
        }

        var (isLeftString, isLeftNumber, left) = ToNativeExpression(binaryExpression.Left);
        var (isRightString, isRightNumber, right) = ToNativeExpression(binaryExpression.Right);

        switch (@operator)
        {
            case TokenTypes.Plus:
                if (isLeftNumber && isRightNumber)
                    return JSNumberBuilder.New(BExpression.Add(left, right));

                if (isLeftString && isRightString)
                    return JSStringBuilder.New(ClrStringBuilder.Concat(left, right));

                if (isRightNumber)
                    return JSValueBuilder.AddDouble(ToJSValueExpression(left), right);

                if (isRightString)
                    return JSValueBuilder.AddString(ToJSValueExpression(left), right);

                return JSValueBuilder.Add(ToJSValueExpression(left), ToJSValueExpression(right));

            case TokenTypes.Equal:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.Equal(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.Equal(left, right));
                }

                return JSValueBuilder.Equals(ToJSValueExpression(left), right);

            case TokenTypes.NotEqual:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.NotEqual(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.NotEqual(left, right));
                }

                return JSValueBuilder.NotEquals(ToJSValueExpression(left), right);

            case TokenTypes.StrictlyEqual:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.Equal(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.Equal(left, right));
                }

                return JSValueBuilder.StrictEquals(ToJSValueExpression(left), right);

            case TokenTypes.StrictlyNotEqual:
                if (isLeftNumber)
                {
                    // to do
                    // Add cocering...
                    if (isRightNumber)
                        return JSBooleanBuilder.NewFromCLRBoolean(BExpression.NotEqual(left, right));
                }

                if (isLeftString)
                {
                    if (isRightString)
                        return JSBooleanBuilder.NewFromCLRBoolean(ClrStringBuilder.NotEqual(left, right));
                }

                return JSValueBuilder.NotStrictEquals(ToJSValueExpression(left), right);
        }
        
        var be = BinaryOperation.Operation(ToJSValueExpression(left), ToJSValueExpression(right), @operator);
        return be ?? throw new FastParseException(binaryExpression.Start, $"Undefined binary operation {@operator}");
    }

    public static BExpression ToJSValueExpression(BExpression exp)
    {
        if (typeof(JSValue).IsAssignableFrom(exp.Type))
            return exp;

        if (exp.Type == typeof(string))
            return JSStringBuilder.New(exp);

        if (exp.Type == typeof(double))
            return JSNumberBuilder.New(exp);

        throw new NotImplementedException();
    }

    public (bool isString, bool isNumber, BExpression exp) ToNativeExpression(AstExpression ast)
    {
        if (ast.Type == FastNodeType.Literal && ast is AstLiteral a)
        {
            switch (a.TokenType)
            {
                case TokenTypes.String:
                    return (true, false, BExpression.Constant(a.StringValue));

                case TokenTypes.Number:
                    return (false, true, BExpression.Constant(a.NumericValue));
            }
        }
        return (false, false, Visit(ast));
    }
}
