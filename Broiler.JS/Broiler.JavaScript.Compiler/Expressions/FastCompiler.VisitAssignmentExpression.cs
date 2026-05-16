using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.LinqExpressions.Utils;
using Broiler.JavaScript.Runtime;
using System;
using System.Reflection;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    private static readonly MethodInfo RequireObjectCoercibleMethod = typeof(JSObjectStatic)
        .InternalMethod(nameof(JSObjectStatic.RequireObjectCoercible), typeof(JSValue))
        ?? throw new InvalidOperationException("JSObjectStatic.RequireObjectCoercible(JSValue) not found");
    private static readonly MethodInfo ReturnableEnumeratorReturnMethod = typeof(IReturnableEnumerator)
        .GetMethod(nameof(IReturnableEnumerator.Return), [typeof(JSValue)])
        ?? throw new InvalidOperationException("IReturnableEnumerator.Return(JSValue) not found");

    private YExpression VisitAssignmentExpression(AstExpression left, TokenTypes assignmentOperator, AstExpression right)
    {
        switch (left.Type)
        {
            case FastNodeType.ArrayPattern:
            case FastNodeType.ObjectPattern:
                return CreateAssignment(left, Visit(right));

            case FastNodeType.Identifier:
                var id = left as AstIdentifier;
                id.VerifyIdentifierForUpdate();
                break;
        }


        // we need to rewrite left side if it is computed expression with member assignment...
        if (assignmentOperator != TokenTypes.Assign && left.Type == FastNodeType.MemberExpression && left is AstMemberExpression mem)
        {
            if (mem.Object.Type != FastNodeType.Identifier)
            {
                // this needs to be computed...
                var tmp = scope.Top.GetTempVariable();
                var leftExp = CreateMemberExpression(tmp.Expression, mem.Property, mem.Computed);
                return YExpression.Block(YExpression.Assign(tmp.Expression, Visit(mem.Object)), Assign(leftExp, right, assignmentOperator), tmp.Expression);
            }
        }

        if (left.Type == FastNodeType.Identifier)
        {
            var identifier = (AstIdentifier)left;
            var variable = scope.Top.GetVariable(identifier.Name, true);
            if (variable == null && assignmentOperator == TokenTypes.Assign)
                return JSContextBuilder.AssignIdentifier(KeyOfName(identifier.Name), Visit(right));

            return Assign(variable?.Expression ?? JSContextBuilder.Index(KeyOfName(identifier.Name)), right, assignmentOperator);
        }

        return Assign(Visit(left), right, assignmentOperator);
    }

    private YExpression Assign(YExpression exp, AstExpression right, TokenTypes assignmentOperator)
    {
        if (assignmentOperator == TokenTypes.AssignAdd && right.Type == FastNodeType.Literal && right is AstLiteral literal)
        {
            if (literal.TokenType == TokenTypes.String)
                return YExpression.Assign(exp, JSValueBuilder.AddString(exp, YExpression.Constant(literal.StringValue)));

            if (literal.TokenType == TokenTypes.Number)
                return YExpression.Assign(exp, JSValueBuilder.AddDouble(exp, YExpression.Constant(literal.NumericValue)));
        }

        return BinaryOperation.Assign(exp, Visit(right), assignmentOperator);
    }

    private YExpression CreateAssignment(AstExpression pattern, YExpression init, bool createVariable = false, bool newScope = false)
    {
        var inits = new Sequence<YExpression>();
        CreateAssignment(inits, pattern, init, createVariable, newScope);

        return YExpression.Block(inits);
    }

    private void CreateAssignment(Sequence<YExpression> inits, AstExpression pattern, YExpression init, bool createVariable = false, bool newScope = false)
    {
        YExpression target;

        switch (pattern.Type)
        {
            case FastNodeType.Identifier:
                {
                    var id = pattern as AstIdentifier;
                    if (createVariable)
                    {
                        var v = scope.Top.CreateVariable(id.Name.Value, JSVariableBuilder.New(id.Name.Value), newScope);
                        target = v.Expression;
                    }
                    else
                    {
                        target = VisitIdentifierReference(id);
                    }

                    inits.Add(YExpression.Assign(target, init));
                }
                return;

            case FastNodeType.ObjectPattern:
                var objectPattern = pattern as AstObjectPattern;
                {
                    using var tempValue = scope.Top.GetTempVariable(typeof(JSValue));
                    inits.Add(YExpression.Assign(tempValue.Variable, YExpression.Call(null, RequireObjectCoercibleMethod, init)));
                    init = tempValue.Expression;

                    var en = objectPattern.Properties.GetFastEnumerator();

                    while (en.MoveNext(out var property))
                    {
                        YExpression start = null;
                        switch (property.Key.Type)
                        {
                            case FastNodeType.Identifier:
                            case FastNodeType.Literal:
                                var id = property.Key;
                                var propertyInit = property.Init;
                                if (propertyInit != null)
                                {
                                    var piTemp = scope.Top.GetTempVariable(typeof(JSValue));
                                    inits.Add(YExpression.Assign(piTemp.Variable,
                                        JSValueBuilder.Coalesce(
                                        CreateMemberExpression(init, id, property.Computed),
                                        Visit(propertyInit))));
                                    start = piTemp.Variable;
                                }
                                else
                                {
                                    start = CreateMemberExpression(init, id, property.Computed);
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        switch (property.Value.Type)
                        {
                            case FastNodeType.Identifier:
                            case FastNodeType.ArrayPattern:
                            case FastNodeType.ObjectPattern:
                                CreateAssignment(inits, property.Value, start, true, newScope);
                                break;
                            // TODO
                            case FastNodeType.BinaryExpression:
                                var ap = property.Value as AstBinaryExpression;
                                CreateAssignment(inits, ap.Left,
                                    YExpression.Coalesce(
                                        JSValueExtensionsBuilder.NullIfUndefined(start),
                                        Visit(ap.Right))
                                );
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                return;

            case FastNodeType.ArrayPattern:
                var arrayPattern = pattern as AstArrayPattern;
                using (var enVar = scope.Top.GetTempVariable(typeof(IElementEnumerator)))
                using (var returnableVar = scope.Top.GetTempVariable(typeof(IReturnableEnumerator)))
                {
                    var destExp = enVar.Expression;
                    inits.Add(YExpression.Assign(destExp, IElementEnumeratorBuilder.Get(init)));
                    inits.Add(YExpression.Assign(returnableVar.Expression, YExpression.TypeAs(destExp, typeof(IReturnableEnumerator))));
                    var en = arrayPattern.Elements.GetFastEnumerator();
                    var arrayInits = new Sequence<YExpression>();
                    var hasSpread = false;

                    while (en.MoveNext(out var element))
                    {
                        switch (element.Type)
                        {
                            case FastNodeType.EmptyExpression:
                                // Elision: advance iterator without assigning
                                using (var skipTemp = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    arrayInits.Add(IElementEnumeratorBuilder.MoveNext(destExp, skipTemp.Expression));
                                }
                                break;
                            case FastNodeType.Identifier:
                                var id = element as AstIdentifier;
                                if (createVariable)
                                    scope.Top.CreateVariable(id.Name.Value, null, newScope);

                                var assignee = VisitIdentifierReference(id);
                                arrayInits.Add(IElementEnumeratorBuilder.AssignMoveNext(assignee, destExp));
                                break;
                            case FastNodeType.BinaryExpression:
                                var be = element as AstBinaryExpression;
                                if (be.Left.Type != FastNodeType.Identifier)
                                {
                                    using var te = scope.Top.GetTempVariable(typeof(JSValue));
                                    arrayInits.Add(IElementEnumeratorBuilder.MoveNext(destExp, te.Expression));
                                    arrayInits.Add(JSValueExtensionsBuilder.AssignCoalesce(te.Expression, Visit(be.Right)));

                                    CreateAssignment(arrayInits, be.Left, te.Expression, true, newScope);

                                    break;
                                }

                                id = be.Left as AstIdentifier;
                                if (createVariable)
                                    scope.Top.CreateVariable(id.Name.Value, null, newScope);

                                assignee = VisitIdentifierReference(id);
                                arrayInits.Add(IElementEnumeratorBuilder.AssignMoveNext(assignee, destExp));
                                arrayInits.Add(JSValueExtensionsBuilder.AssignCoalesce(assignee, Visit(be.Right)));
                                break;

                            case FastNodeType.SpreadElement:
                                var spe = element as AstSpreadElement;
                                hasSpread = true;
                                // loop...
                                if (createVariable && spe.Argument is AstIdentifier id2)
                                    scope.Top.CreateVariable(id2.Name.Value, null, newScope);

                                var spid = Visit(spe.Argument);
                                arrayInits.Add(YExpression.Assign(spid, JSArrayBuilder.NewFromElementEnumerator(destExp)));
                                break;

                            case FastNodeType.ObjectPattern:
                            case FastNodeType.ArrayPattern:
                                var ape = element;
                                // nested array ...
                                // nested object ...
                                using (var te = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    var check = IElementEnumeratorBuilder.MoveNext(destExp, te.Expression);
                                    arrayInits.Add(check);
                                    CreateAssignment(arrayInits, ape, te.Expression, true, newScope);
                                }
                                break;

                            default:
                                throw new NotSupportedException($"{element.Type}");
                        }
                    }

                    var arrayInitBlock = YExpression.Block(arrayInits);
                    if (hasSpread)
                    {
                        inits.Add(arrayInitBlock);
                    }
                    else
                    {
                        var closeIterator = YExpression.Condition(
                            YExpression.NotEqual(YExpression.Convert(returnableVar.Expression, typeof(object)), YExpression.Null),
                            YExpression.Block(
                                YExpression.Call(returnableVar.Expression, ReturnableEnumeratorReturnMethod, JSUndefinedBuilder.Value),
                                JSUndefinedBuilder.Value),
                            JSUndefinedBuilder.Value,
                            typeof(JSValue));

                        inits.Add(YExpression.TryFinally(arrayInitBlock, closeIterator));
                    }
                }

                return;
        }

        throw new NotImplementedException();
    }
}
