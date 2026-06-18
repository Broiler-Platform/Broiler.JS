using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static YExpression DoubleValue(YExpression exp) => JSValueBuilder.DoubleValue(exp);

    private YExpression DoubleValue(AstExpression exp) => JSValueBuilder.DoubleValue(VisitExpression(exp));

    private YExpression BooleanValue(AstExpression exp) => JSValueBuilder.BooleanValue(VisitExpression(exp));

    protected override YExpression VisitUnaryExpression(AstUnaryExpression unaryExpression)
    {
        var target = unaryExpression.Argument;

        switch (unaryExpression.Operator)
        {
            case UnaryOperator.Plus:
                return JSNumberBuilder.New(YExpression.UnaryPlus(DoubleValue(target)));

            case UnaryOperator.Minus:
                if (target.Type == FastNodeType.Literal)
                {
                    AstLiteral l = unaryExpression.Argument as AstLiteral;

                    if (l.TokenType == TokenTypes.Number)
                        return JSNumberBuilder.New(YExpression.Constant(-l.NumericValue));

                    if (l.TokenType == TokenTypes.BigInt)
                        return JSBigIntBuilder.New("-" + l.StringValue);

                    if (l.TokenType == TokenTypes.String)
                        return JSNumberBuilder.New(YExpression.Negate(DoubleValue(target)));
                }

                return JSValueBuilder.Negate(Visit(target));

            case UnaryOperator.BitwiseNot:
                return JSValueBuilder.BitwiseNot(Visit(target));

            case UnaryOperator.Negate:
                return YExpression.Condition(BooleanValue(target), JSBooleanBuilder.False, JSBooleanBuilder.True);

            case UnaryOperator.delete:
                // delete expression...
                // `delete a?.b` is a DeleteExpression over an optional chain: unwrap the
                // chain marker and let the member path apply the delete with short-circuit.
                if (target is AstOptionalChain deleteChain)
                    target = deleteChain.Expression;

                switch (target.Type)
                {
                    case FastNodeType.Literal:
                        return JSBooleanBuilder.True;

                    case FastNodeType.Identifier:
                        var id = target as AstIdentifier;
                        if (id.Name == "this")
                            return JSBooleanBuilder.True;

                        if (id.Name == "arguments"
                            && scope.Top.RootScope.Function is { IsArrowFunction: false })
                        {
                            return JSBooleanBuilder.False;
                        }

                        var hasStaticVariable = TryGetStaticIdentifierVariable(id, out var variable);
                        if (!hasStaticVariable || variable == null)
                            return JSContextBuilder.DeleteIdentifier(KeyOfName(id.Name));

                        if (variable != null && !variable.IsDeletable)
                        {
                            var canDeleteCapturedDirectEvalBinding = isDirectEvalCompilation
                                && variable.OwnerFunction != scope.Top.Function
                                && variable.Expression is YPropertyExpression { PropertyInfo.Name: nameof(JSVariable.GlobalValue) };
                            if (canDeleteCapturedDirectEvalBinding)
                                return JSBooleanBuilder.True;

                            if (!canDeleteCapturedDirectEvalBinding
                                && variable.Expression is not YPropertyExpression { PropertyInfo.Name: nameof(JSVariable.GlobalValue) })
                                return JSBooleanBuilder.False;
                        }

                        return JSContextBuilder.DeleteIdentifier(KeyOfName(id.Name));

                    case FastNodeType.MemberExpression:
                        break;

                    default:
                        return YExpression.Block(Visit(target), JSBooleanBuilder.True);
                }

                var me = target as AstMemberExpression;

                // delete of a SuperProperty reference is always a ReferenceError
                // (delete operator runtime semantics step 5a: IsSuperReference).
                // A computed key is still evaluated for its side effects first.
                if (me.Object.Type == FastNodeType.Super)
                {
                    var refError = JSExceptionBuilder.ThrowReferenceError("Unsupported reference to 'super'");
                    if (!me.Computed)
                        return refError;

                    // Evaluating the SuperProperty reference does GetThisBinding (step 2)
                    // BEFORE the key Expression (step 3): in a derived constructor `this`
                    // is in its TDZ until super() runs, so reading it must throw a
                    // ReferenceError before any key side effect (`delete super[(super(),0)]`)
                    // is evaluated. Read `this` first, then the key, then the
                    // delete-of-super ReferenceError.
                    using var thisTemp = scope.Top.GetTempVariable(typeof(JSValue));
                    return YExpression.Block(
                        YExpression.Assign(thisTemp.Expression, scope.Top.ThisExpression),
                        VisitExpression(me.Property),
                        refError);
                }

                // Build `delete base.<key>` for an already-evaluated base expression.
                YExpression DeleteFrom(YExpression baseObj)
                {
                    if (me.Computed)
                        return JSValueBuilder.Delete(baseObj, VisitExpression(me.Property));

                    var mep = me.Property;
                    switch (mep.Type)
                    {
                        case FastNodeType.Literal:
                            AstLiteral l = mep as AstLiteral;
                            if (l.TokenType == TokenTypes.Number)
                                return JSValueBuilder.Delete(baseObj, YExpression.Constant((uint)l.NumericValue));
                            if (l.TokenType == TokenTypes.String)
                                return JSValueBuilder.Delete(baseObj, KeyOfName(l.StringValue));
                            return null;

                        case FastNodeType.Identifier:
                            AstIdentifier id = mep as AstIdentifier;
                            return JSValueBuilder.Delete(baseObj, KeyOfName(id.Name));

                        default:
                            return null;
                    }
                }

                // `delete a?.b`: when the chain short-circuits (the base is nullish for a
                // `?.` link, or already-skipped for a trailing link) the delete is a no-op
                // that yields true; otherwise the property is deleted normally.
                if (me.InOptionalChain)
                {
                    using var baseTemp = scope.Top.GetTempVariable(typeof(JSValue));
                    var deleteExpr = DeleteFrom(baseTemp.Expression);
                    if (deleteExpr != null)
                    {
                        return YExpression.Block(
                            YExpression.Assign(baseTemp.Expression, VisitExpression(me.Object)),
                            YExpression.Condition(
                                JSValueBuilder.OptionalChainGuard(baseTemp.Expression, me.Coalesce),
                                JSBooleanBuilder.True,
                                deleteExpr));
                    }
                }
                else
                {
                    var nonChainDelete = DeleteFrom(VisitExpression(me.Object));
                    if (nonChainDelete != null)
                        return nonChainDelete;
                }
                break;

            case UnaryOperator.@void:
                if (target != null && target.Type != FastNodeType.Literal)
                    return YExpression.Condition(YExpression.Equal(YExpression.Null, Visit(target)), JSUndefinedBuilder.Value, JSUndefinedBuilder.Value);

                return JSUndefinedBuilder.Value;

            case UnaryOperator.@typeof:
                if (target is AstIdentifier identifier)
                    return JSValueBuilder.TypeOf(VisitIdentifier(identifier, false));

                return JSValueBuilder.TypeOf(VisitExpression(target));

            case UnaryOperator.Increment:
                return InternalVisitUpdateExpression(unaryExpression);

            case UnaryOperator.Decrement:
                return InternalVisitUpdateExpression(unaryExpression);
        }

        throw new InvalidOperationException();
    }
}
