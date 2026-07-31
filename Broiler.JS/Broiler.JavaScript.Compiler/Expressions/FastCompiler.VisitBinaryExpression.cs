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

        // With both operands already native doubles the whole tree stays unboxed and only
        // its final value is boxed. Without this a numeric local would be RE-boxed for any
        // operator that has no native case below — which cost `t += i % 100` an extra
        // allocation per iteration rather than saving one.
        if (isLeftNumber && isRightNumber
            && TryCreateNativeNumericOperation(@operator, left, right) is { } nativeNumeric)
        {
            return nativeNumeric;
        }

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

    /// <summary>
    /// <paramref name="operator"/> applied to two CLR doubles, still unboxed, or null when it
    /// has no native form and the operands have to go back through the JSValue operators.
    /// </summary>
    /// <remarks>
    /// Only operators whose JavaScript semantics on two numbers ARE the CLR double operator
    /// appear here. Two families are deliberately absent. The bitwise and shift operators run
    /// ToInt32 first, whose modulo-2^32 wrapping a plain cast does not reproduce. And
    /// <c>&lt;=</c> / <c>&gt;=</c> are absent because the backend emits an ORDERED compare for
    /// them, which answers true when either side is NaN — every relational comparison
    /// involving NaN is false in JavaScript. <c>&lt;</c> and <c>&gt;</c> do not have that
    /// problem and are the forms a loop test actually uses.
    /// </remarks>
    private static BExpression TryCreateNativeNumericValue(TokenTypes @operator, BExpression left, BExpression right)
        => @operator switch
        {
            TokenTypes.Plus => BExpression.Add(left, right),
            TokenTypes.Minus => BExpression.Subtract(left, right),
            TokenTypes.Multiply => BExpression.Multiply(left, right),
            TokenTypes.Divide => BExpression.Divide(left, right),
            TokenTypes.Mod => BExpression.Modulo(left, right),
            TokenTypes.Power => BExpression.Power(left, right),
            _ => null,
        };

    /// <summary>The boxed result of a native numeric operation or comparison, or null.</summary>
    private static BExpression TryCreateNativeNumericOperation(TokenTypes @operator, BExpression left, BExpression right)
    {
        if (TryCreateNativeNumericValue(@operator, left, right) is { } value)
            return JSNumberBuilder.New(value);

        return @operator switch
        {
            TokenTypes.Less => JSBooleanBuilder.NewFromCLRBoolean(BExpression.Binary(left, BOperator.Less, right)),
            TokenTypes.Greater => JSBooleanBuilder.NewFromCLRBoolean(BExpression.Binary(left, BOperator.Greater, right)),
            _ => null,
        };
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

        // A local the analysis proved numeric is already a raw double.
        if (TryGetNumericLocalStorage(ast, out var storage))
            return (false, true, storage);

        // ... and an arithmetic node over two native operands is itself native, so a whole
        // tree stays unboxed and only its final value is boxed. Without this recursion only
        // the leaves were native and every intermediate result was re-boxed
        // (docs/performance-roadmap.md P2-2 item 3).
        //
        // Decided on the SYNTAX first, deliberately. Compiling a subtree speculatively and
        // throwing the result away would leak whatever the visit allocated on the way —
        // an inline-cache site per discarded attempt, among other compile-time state.
        if (ast is AstBinaryExpression nested && IsNativeNumericAst(nested))
        {
            var (_, _, nestedLeft) = ToNativeExpression(nested.Left);
            var (_, _, nestedRight) = ToNativeExpression(nested.Right);
            return (false, true, TryCreateNativeNumericValue(nested.Operator, nestedLeft, nestedRight));
        }

        return (false, false, Visit(ast));
    }

    /// <summary>
    /// Whether this expression compiles to an unboxed double without visiting it — a numeric
    /// literal, a numeric local, or a native arithmetic node over two of those.
    /// </summary>
    private bool IsNativeNumericAst(AstExpression ast) => ast switch
    {
        AstLiteral { TokenType: TokenTypes.Number } => true,
        AstIdentifier => TryGetNumericLocalStorage(ast, out _),
        AstBinaryExpression binary => IsNativeNumericOperator(binary.Operator)
            && IsNativeNumericAst(binary.Left)
            && IsNativeNumericAst(binary.Right),
        _ => false,
    };

    private static bool IsNativeNumericOperator(TokenTypes @operator) => @operator
        is TokenTypes.Plus or TokenTypes.Minus or TokenTypes.Multiply
        or TokenTypes.Divide or TokenTypes.Mod or TokenTypes.Power;

    /// <summary>The raw double storage behind <paramref name="ast"/>, if it is a numeric local.</summary>
    private bool TryGetNumericLocalStorage(AstExpression ast, out BExpression storage)
    {
        storage = null;
        if (ast is not AstIdentifier identifier
            || withBoundaries.Count != 0
            || evalShadowBoundary != null)
        {
            return false;
        }

        if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable?.NumericStorage == null)
            return false;

        storage = variable.NumericStorage;
        return true;
    }

    /// <summary>Coerces a compiled value to the CLR double a numeric local stores.</summary>
    private static BExpression ToDoubleExpression(BExpression exp)
        => exp.Type == typeof(double) ? exp : JSValueBuilder.DoubleValue(ToJSValueExpression(exp));

    /// <summary>
    /// An assignment into <paramref name="variable"/>, unboxing the value when the binding is
    /// a numeric local. Writing through <c>Expression</c> there would target a boxing read.
    /// </summary>
    private static BExpression AssignToVariable(FastFunctionScope.VariableScope variable, BExpression value)
        => variable.NumericStorage != null
            ? BExpression.Assign(variable.NumericStorage, ToDoubleExpression(value))
            : BExpression.Assign(variable.Expression, value);
}
