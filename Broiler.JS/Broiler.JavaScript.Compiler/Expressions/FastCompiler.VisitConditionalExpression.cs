using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitConditionalExpression(AstConditionalExpression conditionalExpression)
    {
        // The operand of `typeof` must resolve a bare identifier without throwing when it
        // is undeclared (`typeof undeclared` is "undefined", not a ReferenceError). Mirror
        // the ordinary typeof path (VisitUnaryExpression) so the `typeof x === "..."`
        // ternary-test optimization below preserves that semantics.
        BExpression TypeofOperand(AstExpression target)
            => target is AstIdentifier id
                ? VisitIdentifier(id, false)
                : VisitExpression(target);

        BExpression EvaluateTest(AstExpression exp)
        {
            if (exp.IsUnaryExpression(out var u) && u.Operator == UnaryOperator.Negate)
            {
                var eu = VisitExpression(u.Argument);
                var e1 = JSValueBuilder.BooleanValue(eu);
                var e2 = BExpression.Not(e1);
                return e2;
            }

            if (exp.IsBinaryExpression(out var b) && b.Left.IsUnaryExpression(out u) && u.Operator == UnaryOperator.@typeof && b.Right.IsStringLiteral(out var value))
            {
                switch (value)
                {
                    case "undefined":
                        if (b.Operator == TokenTypes.Equal || b.Operator == TokenTypes.StrictlyEqual)
                            return BExpression.Equal(TypeofOperand(u.Argument), JSUndefinedBuilder.Value);

                        if (b.Operator == TokenTypes.NotEqual || b.Operator == TokenTypes.StrictlyNotEqual)
                            return BExpression.NotEqual(TypeofOperand(u.Argument), JSUndefinedBuilder.Value);

                        break;
                    case "number":
                        if (b.Operator == TokenTypes.Equal || b.Operator == TokenTypes.StrictlyEqual)
                            return JSValueBuilder.IsNumber(TypeofOperand(u.Argument));

                        if (b.Operator == TokenTypes.NotEqual || b.Operator == TokenTypes.StrictlyNotEqual)
                            return BExpression.Not(JSValueBuilder.IsNumber(TypeofOperand(u.Argument)));

                        break;
                    case "string":
                        if (b.Operator == TokenTypes.Equal || b.Operator == TokenTypes.StrictlyEqual)
                            return JSValueBuilder.IsString(TypeofOperand(u.Argument));

                        if (b.Operator == TokenTypes.NotEqual || b.Operator == TokenTypes.StrictlyNotEqual)
                            return BExpression.Not(JSValueBuilder.IsString(TypeofOperand(u.Argument)));

                        break;
                    case "function":
                        if (b.Operator == TokenTypes.Equal || b.Operator == TokenTypes.StrictlyEqual)
                            return JSValueBuilder.IsFunction(TypeofOperand(u.Argument));

                        if (b.Operator == TokenTypes.NotEqual || b.Operator == TokenTypes.StrictlyNotEqual)
                            return BExpression.Not(JSValueBuilder.IsFunction(TypeofOperand(u.Argument)));

                        break;
                    case "object":
                        if (b.Operator == TokenTypes.Equal || b.Operator == TokenTypes.StrictlyEqual)
                            return JSValueBuilder.IsObjectType(TypeofOperand(u.Argument));

                        if (b.Operator == TokenTypes.NotEqual || b.Operator == TokenTypes.StrictlyNotEqual)
                            return BExpression.Not(JSValueBuilder.IsObjectType(TypeofOperand(u.Argument)));

                        break;
                    case "symbol":
                        if (b.Operator == TokenTypes.Equal || b.Operator == TokenTypes.StrictlyEqual)
                            return JSValueBuilder.IsSymbol(TypeofOperand(u.Argument));

                        if (b.Operator == TokenTypes.NotEqual || b.Operator == TokenTypes.StrictlyNotEqual)
                            return BExpression.Not(JSValueBuilder.IsSymbol(TypeofOperand(u.Argument)));

                        break;
                }
            }

            return JSValueBuilder.BooleanValue(VisitExpression(exp));
        }

        var test = EvaluateTest(conditionalExpression.Test);
        var @true = VisitExpression(conditionalExpression.True);
        var @false = VisitExpression(conditionalExpression.False);

        return BExpression.Condition(test, @true, @false, typeof(JSValue));
    }
}
