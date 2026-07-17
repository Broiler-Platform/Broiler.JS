using System;
using System.Collections.Generic;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Recognizes one deliberately narrow counted-reduction shape for the Phase 5
/// speculative numeric tier. Any uncertainty leaves the ordinary compiler path intact.
/// </summary>
internal static class NumericLoopPlanner
{
    public static NumericLoopPlan TryCreate(AstFunctionExpression function)
    {
        if (function.Body is not AstBlock body)
            return null;

        var parameters = GetSimpleParameters(function);
        if (parameters == null)
            return null;

        var statements = ToList(body.Statements);
        if (statements.Count != 3
            || !TryGetSingleNumericDeclaration(statements[0], out var accumulatorName, out var accumulatorInitial)
            || statements[1] is not AstForStatement loop
            || statements[2] is not AstReturnStatement { Argument: AstIdentifier returned }
            || !returned.Name.Equals(accumulatorName))
        {
            return null;
        }

        if (!TryGetSingleNumericDeclaration(loop.Init, out var inductionName, out var inductionInitial)
            || accumulatorName.Equals(inductionName)
            || loop.Test is not AstBinaryExpression test
            || test.Left is not AstIdentifier testInduction
            || !testInduction.Name.Equals(inductionName)
            || test.Right is not AstIdentifier limitIdentifier
            || limitIdentifier.Name.Equals(accumulatorName)
            || limitIdentifier.Name.Equals(inductionName)
            || !parameters.TryGetValue(limitIdentifier.Name.Value, out var limitArgumentIndex)
            || !TryGetComparison(test.Operator, out var comparison)
            || !TryGetStep(loop.Update, inductionName, out var step)
            || !TryGetReduction(loop.Body, accumulatorName, inductionName, out var scale, out var offset))
        {
            return null;
        }

        if ((comparison is NumericLoopComparison.LessThan or NumericLoopComparison.LessThanOrEqual && step < 0)
            || (comparison is NumericLoopComparison.GreaterThan or NumericLoopComparison.GreaterThanOrEqual && step > 0))
        {
            return null;
        }

        return new NumericLoopPlan(
            limitArgumentIndex,
            accumulatorInitial,
            inductionInitial,
            step,
            comparison,
            scale,
            offset);
    }

    private static Dictionary<string, int> GetSimpleParameters(AstFunctionExpression function)
    {
        var parameters = new Dictionary<string, int>(StringComparer.Ordinal);
        var enumerator = function.Params.GetFastEnumerator();
        var index = 0;
        while (enumerator.MoveNext(out var parameter))
        {
            if (parameter.Init != null
                || parameter.Identifier is not AstIdentifier identifier
                || !parameters.TryAdd(identifier.Name.Value, index++))
            {
                return null;
            }
        }
        return parameters;
    }

    private static List<AstStatement> ToList(
        Broiler.JavaScript.ExpressionCompiler.Core.IFastEnumerable<AstStatement> values)
    {
        var result = new List<AstStatement>(values.Count);
        var enumerator = values.GetFastEnumerator();
        while (enumerator.MoveNext(out var value))
            result.Add(value);
        return result;
    }

    private static bool TryGetSingleNumericDeclaration(
        AstNode node,
        out StringSpan name,
        out double value)
    {
        name = default;
        value = default;
        if (node is not AstVariableDeclaration declaration
            || declaration.Kind == FastVariableKind.Const
            || declaration.Using
            || declaration.AwaitUsing
            || declaration.Declarators.Count != 1)
            return false;

        var enumerator = declaration.Declarators.GetFastEnumerator();
        if (!enumerator.MoveNext(out var declarator)
            || declarator.Identifier is not AstIdentifier identifier
            || !TryGetNumber(declarator.Init, out value))
        {
            return false;
        }

        name = identifier.Name;
        return true;
    }

    private static bool TryGetComparison(TokenTypes token, out NumericLoopComparison comparison)
    {
        comparison = token switch
        {
            TokenTypes.Less => NumericLoopComparison.LessThan,
            TokenTypes.LessOrEqual => NumericLoopComparison.LessThanOrEqual,
            TokenTypes.Greater => NumericLoopComparison.GreaterThan,
            TokenTypes.GreaterOrEqual => NumericLoopComparison.GreaterThanOrEqual,
            _ => default,
        };
        return token is TokenTypes.Less or TokenTypes.LessOrEqual or TokenTypes.Greater or TokenTypes.GreaterOrEqual;
    }

    private static bool TryGetStep(AstExpression update, in StringSpan inductionName, out double step)
    {
        step = default;
        if (update is AstUnaryExpression { Argument: AstIdentifier identifier } unary
            && identifier.Name.Equals(inductionName))
        {
            step = unary.Operator switch
            {
                UnaryOperator.Increment => 1,
                UnaryOperator.Decrement => -1,
                _ => 0,
            };
            return step != 0;
        }

        if (update is AstBinaryExpression { Left: AstIdentifier assigned } binary
            && assigned.Name.Equals(inductionName)
            && TryGetNumber(binary.Right, out var amount))
        {
            step = binary.Operator switch
            {
                TokenTypes.AssignAdd => amount,
                TokenTypes.AssignSubtract => -amount,
                _ => 0,
            };
            return step != 0 && !double.IsNaN(step);
        }

        return false;
    }

    private static bool TryGetReduction(
        AstStatement body,
        in StringSpan accumulatorName,
        in StringSpan inductionName,
        out double scale,
        out double offset)
    {
        scale = default;
        offset = default;
        AstStatement statement = body;
        if (body is AstBlock block)
        {
            var statements = ToList(block.Statements);
            if (statements.Count != 1)
                return false;
            statement = statements[0];
        }

        return statement is AstExpressionStatement
        {
            Expression: AstBinaryExpression
            {
                Operator: TokenTypes.AssignAdd,
                Left: AstIdentifier accumulator,
            } assignment,
        }
            && accumulator.Name.Equals(accumulatorName)
            && TryGetAffineTerm(assignment.Right, inductionName, out scale, out offset);
    }

    private static bool TryGetAffineTerm(
        AstExpression expression,
        in StringSpan inductionName,
        out double scale,
        out double offset)
    {
        if (expression is AstIdentifier identifier && identifier.Name.Equals(inductionName))
        {
            scale = 1;
            offset = 0;
            return true;
        }
        if (TryGetNumber(expression, out var constant))
        {
            scale = 0;
            offset = constant;
            return true;
        }
        if (expression is not AstBinaryExpression binary)
        {
            scale = offset = default;
            return false;
        }

        var leftIsInduction = binary.Left is AstIdentifier leftIdentifier
            && leftIdentifier.Name.Equals(inductionName);
        var rightIsInduction = binary.Right is AstIdentifier rightIdentifier
            && rightIdentifier.Name.Equals(inductionName);
        var leftIsNumber = TryGetNumber(binary.Left, out var leftNumber);
        var rightIsNumber = TryGetNumber(binary.Right, out var rightNumber);

        if (binary.Operator == TokenTypes.Plus)
        {
            if (leftIsInduction && rightIsNumber)
            {
                scale = 1;
                offset = rightNumber;
                return true;
            }
            if (leftIsNumber && rightIsInduction)
            {
                scale = 1;
                offset = leftNumber;
                return true;
            }
        }
        else if (binary.Operator == TokenTypes.Minus)
        {
            if (leftIsInduction && rightIsNumber)
            {
                scale = 1;
                offset = -rightNumber;
                return true;
            }
            if (leftIsNumber && rightIsInduction)
            {
                scale = -1;
                offset = leftNumber;
                return true;
            }
        }
        else if (binary.Operator == TokenTypes.Multiply)
        {
            if (leftIsInduction && rightIsNumber)
            {
                scale = rightNumber;
                offset = 0;
                return true;
            }
            if (leftIsNumber && rightIsInduction)
            {
                scale = leftNumber;
                offset = 0;
                return true;
            }
        }

        scale = offset = default;
        return false;
    }

    private static bool TryGetNumber(AstExpression expression, out double value)
    {
        if (expression is AstLiteral { TokenType: TokenTypes.Number } literal)
        {
            value = literal.NumericValue;
            return true;
        }

        value = default;
        return false;
    }
}
