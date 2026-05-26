using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
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
        .GetMethod(nameof(IReturnableEnumerator.Return), Type.EmptyTypes)
        ?? throw new InvalidOperationException("IReturnableEnumerator.Return() not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForDestructuringMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForDestructuring), [typeof(JSValue), typeof(string), typeof(bool)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForDestructuring(JSValue, string, bool) not found");
    private static readonly MethodInfo NormalizePropertyKeyMethod = typeof(JSValue)
        .GetMethod("NormalizePropertyKey", BindingFlags.NonPublic | BindingFlags.Static, [typeof(JSValue)])
        ?? throw new InvalidOperationException("JSValue.NormalizePropertyKey(JSValue) not found");

    private YExpression VisitAssignmentExpression(AstExpression left, TokenTypes assignmentOperator, AstExpression right)
    {
        switch (left.Type)
        {
            case FastNodeType.ArrayPattern:
            case FastNodeType.ObjectPattern:
                return CreateAssignment(left, Visit(right), suppressAnonymousFunctionNameInference: true);

            case FastNodeType.Identifier:
                var id = left as AstIdentifier;
                id.VerifyIdentifierForUpdate(IsStrictMode);
                break;
        }


        // we need to rewrite left side if it is computed expression with member assignment...
        if (assignmentOperator != TokenTypes.Assign && left.Type == FastNodeType.MemberExpression && left is AstMemberExpression mem)
        {
            using var objectTemp = scope.Top.GetTempVariable(typeof(JSValue));
            if (mem.Computed)
            {
                using var propertyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                using var keyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                var leftExp = JSValueBuilder.Index(objectTemp.Expression, keyTemp.Expression);
                return YExpression.Block(
                    YExpression.Assign(objectTemp.Expression, Visit(mem.Object)),
                    YExpression.Assign(propertyTemp.Expression, Visit(mem.Property)),
                    YExpression.Call(null, RequireObjectCoercibleMethod, objectTemp.Expression),
                    YExpression.Assign(keyTemp.Expression, YExpression.Call(null, NormalizePropertyKeyMethod, propertyTemp.Expression)),
                    Assign(leftExp, right, assignmentOperator));
            }

            var memberExp = CreateMemberExpression(objectTemp.Expression, mem.Property, false);
            return YExpression.Block(
                YExpression.Assign(objectTemp.Expression, Visit(mem.Object)),
                Assign(memberExp, right, assignmentOperator));
        }

        if (left.Type == FastNodeType.Identifier)
        {
            var identifier = (AstIdentifier)left;
            if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable == null)
            {
                if (assignmentOperator == TokenTypes.Assign && !IsAnonymousFunctionDefinition(right))
                {
                    var initExpr = Visit(right);
                    initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(""), YExpression.Constant(false));
                    return AssignIdentifier(identifier, initExpr);
                }
                return AssignIdentifier(identifier, right, assignmentOperator);
            }

            if (assignmentOperator == TokenTypes.Assign && variable.IsLexical && variable.Variable?.Type == typeof(JSVariable))
            {
                var initExpr = Visit(right);
                if (!IsAnonymousFunctionDefinition(right))
                    initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(""), YExpression.Constant(false));
                return JSVariableBuilder.Assign(variable.Variable, initExpr);
            }

            if (assignmentOperator == TokenTypes.Assign)
            {
                var initExpr = Visit(right);
                if (!IsAnonymousFunctionDefinition(right))
                    initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(""), YExpression.Constant(false));
                return YExpression.Assign(variable.Expression, initExpr);
            }

            return Assign(variable.Expression, right, assignmentOperator);
        }

        return Assign(Visit(left), right, assignmentOperator);
    }

    private YExpression AssignIdentifier(AstIdentifier identifier, AstExpression right, TokenTypes assignmentOperator)
    {
        if (assignmentOperator == TokenTypes.Assign)
            return AssignIdentifier(identifier, Visit(right));

        var key = KeyOfName(identifier.Name);
        using var valueTemp = scope.Top.GetTempVariable(typeof(JSValue));
        return YExpression.Block(
            valueTemp.Variable.AsSequence(),
            YExpression.Assign(valueTemp.Expression, JSContextBuilder.ResolveIdentifier(key)),
            BinaryOperation.Assign(valueTemp.Expression, Visit(right), assignmentOperator),
            JSContextBuilder.AssignIdentifier(key, valueTemp.Expression));
    }

    private YExpression AssignIdentifier(AstIdentifier identifier, YExpression value)
        => JSContextBuilder.AssignIdentifier(KeyOfName(identifier.Name), value);

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

    private YExpression CreateAssignment(AstExpression pattern, YExpression init, bool createVariable = false, bool newScope = false,
        bool suppressAnonymousFunctionNameInference = false, bool initializeVariable = true, bool readOnlyAfterAssign = false)
    {
        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var inits = new Sequence<YExpression>();
        inits.Add(YExpression.Assign(temp.Variable, init));
        CreateAssignment(inits, pattern, temp.Expression, createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign);
        inits.Add(temp.Expression);

        return YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, inits);
    }

    private void CreateAssignment(Sequence<YExpression> inits, AstExpression pattern, YExpression init, bool createVariable = false, bool newScope = false,
        bool suppressAnonymousFunctionNameInference = false, bool initializeVariable = true, bool readOnlyAfterAssign = false)
    {
        YExpression target;

        switch (pattern.Type)
        {
            case FastNodeType.Identifier:
                {
                    var id = pattern as AstIdentifier;
                    if (createVariable)
                    {
                        var v = scope.Top.CreateVariable(id.Name.Value, null, newScope, initialize: initializeVariable);
                        target = v.Expression;
                        if (suppressAnonymousFunctionNameInference)
                        {
                            init = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, init, YExpression.Constant(id.Name.Value), YExpression.Constant(false));
                        }
                        inits.Add(YExpression.Assign(target, init));
                        if (readOnlyAfterAssign)
                            inits.Add(JSVariableBuilder.SetReadOnly(v.Variable, true));
                        return;
                    }
                    else
                    {
                        if (!TryGetStaticIdentifierVariable(id, out var variable) || variable == null)
                        {
                            if (suppressAnonymousFunctionNameInference)
                            {
                                init = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, init, YExpression.Constant(id.Name.Value), YExpression.Constant(false));
                            }

                            inits.Add(AssignIdentifier(id, init));
                            return;
                        }

                        if (suppressAnonymousFunctionNameInference)
                        {
                            init = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, init, YExpression.Constant(id.Name.Value), YExpression.Constant(false));
                        }

                        if (!newScope && variable.IsLexical && variable.Variable?.Type == typeof(JSVariable))
                        {
                            inits.Add(JSVariableBuilder.Assign(variable.Variable, init));
                            return;
                        }

                        target = variable.Expression;
                    }

                    if (suppressAnonymousFunctionNameInference)
                    {
                        init = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, init, YExpression.Constant(id.Name.Value), YExpression.Constant(false));
                    }
                    inits.Add(YExpression.Assign(target, init));
                }
                return;

            case FastNodeType.MemberExpression:
                inits.Add(BinaryOperation.Assign(Visit(pattern), init, TokenTypes.Assign));
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
                                      var defaultValue = Visit(propertyInit);
                                      if (suppressAnonymousFunctionNameInference)
                                      {
                                          defaultValue = PrepareDestructuringInitializer(property.Value, propertyInit, defaultValue);
                                      }

                                      var piTemp = scope.Top.GetTempVariable(typeof(JSValue));
                                      inits.Add(YExpression.Assign(
                                          piTemp.Variable,
                                          CreateMemberExpression(init, id, property.Computed)));
                                      inits.Add(JSValueExtensionsBuilder.AssignCoalesce(
                                          piTemp.Expression,
                                          defaultValue));
                                      start = piTemp.Expression;
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
                            case FastNodeType.MemberExpression:
                            case FastNodeType.ArrayPattern:
                            case FastNodeType.ObjectPattern:
                                CreateAssignment(inits, property.Value, start, createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign);
                                break;
                            // TODO
                            case FastNodeType.BinaryExpression:
                                var ap = property.Value as AstBinaryExpression;
                                var defaultValue = Visit(ap.Right);
                                if (suppressAnonymousFunctionNameInference)
                                {
                                    defaultValue = PrepareDestructuringInitializer(ap.Left, ap.Right, defaultValue);
                                }
                                CreateAssignment(inits, ap.Left,
                                    YExpression.Coalesce(
                                        JSValueExtensionsBuilder.NullIfUndefined(start),
                                        defaultValue),
                                    suppressAnonymousFunctionNameInference: suppressAnonymousFunctionNameInference,
                                    initializeVariable: initializeVariable,
                                    readOnlyAfterAssign: readOnlyAfterAssign);
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
                using (var iterDoneTemp = scope.Top.GetTempVariable(typeof(bool)))
                {
                    var destExp = enVar.Expression;
                    var iterDoneVar = iterDoneTemp.Expression;
                    inits.Add(YExpression.Assign(destExp, IElementEnumeratorBuilder.Get(init)));
                    inits.Add(YExpression.Assign(returnableVar.Expression, YExpression.TypeAs(destExp, typeof(IReturnableEnumerator))));
                    inits.Add(YExpression.Assign(iterDoneVar, YExpression.Constant(false)));
                    var en = arrayPattern.Elements.GetFastEnumerator();
                    var arrayInits = new Sequence<YExpression>();

                    while (en.MoveNext(out var element))
                    {
                        switch (element.Type)
                        {
                            case FastNodeType.EmptyExpression:
                                // Elision: advance iterator without assigning, track done
                                using (var skipTemp = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    arrayInits.Add(YExpression.IfThen(
                                        YExpression.Not(iterDoneVar),
                                        YExpression.IfThen(
                                            YExpression.Not(IElementEnumeratorBuilder.MoveNext(destExp, skipTemp.Expression)),
                                            YExpression.Block(
                                                YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                                YExpression.Empty))));
                                }
                                break;
                            case FastNodeType.Identifier:
                                using (var moveTemp = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    arrayInits.Add(YExpression.IfThen(
                                        YExpression.OrElse(iterDoneVar,
                                            YExpression.Not(IElementEnumeratorBuilder.MoveNext(destExp, moveTemp.Expression))),
                                        YExpression.Block(
                                            YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                            CreateAssignment(element, JSUndefinedBuilder.Value, createVariable, newScope,
                                                suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign),
                                            YExpression.Empty),
                                        YExpression.Block(
                                            CreateAssignment(element, moveTemp.Expression, createVariable, newScope,
                                                suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign),
                                            YExpression.Empty)));
                                }
                                break;
                            case FastNodeType.BinaryExpression:
                                var be = element as AstBinaryExpression;
                                using (var moveTemp2 = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    arrayInits.Add(YExpression.IfThen(
                                        YExpression.OrElse(iterDoneVar,
                                            YExpression.Not(IElementEnumeratorBuilder.MoveNext(destExp, moveTemp2.Expression))),
                                        YExpression.Block(
                                            YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                            YExpression.Assign(moveTemp2.Expression, JSUndefinedBuilder.Value),
                                            YExpression.Empty)));
                                    var identifierDefaultValue = Visit(be.Right);
                                    if (suppressAnonymousFunctionNameInference)
                                    {
                                        identifierDefaultValue = PrepareDestructuringInitializer(be.Left, be.Right, identifierDefaultValue);
                                    }

                                    arrayInits.Add(JSValueExtensionsBuilder.AssignCoalesce(moveTemp2.Expression, identifierDefaultValue));
                                    arrayInits.Add(CreateAssignment(be.Left, moveTemp2.Expression, createVariable, newScope,
                                        suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign));
                                }
                                break;

                            case FastNodeType.SpreadElement:
                                var spe = element as AstSpreadElement;
                                CreateAssignment(arrayInits, spe.Argument, JSArrayBuilder.NewFromElementEnumerator(destExp), createVariable, newScope,
                                    suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign);
                                arrayInits.Add(YExpression.Assign(iterDoneVar, YExpression.Constant(true)));
                                break;

                            case FastNodeType.ObjectPattern:
                            case FastNodeType.ArrayPattern:
                                var ape = element;
                                // nested array ...
                                // nested object ...
                                using (var te = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    arrayInits.Add(YExpression.IfThen(
                                        YExpression.OrElse(iterDoneVar,
                                            YExpression.Not(IElementEnumeratorBuilder.MoveNext(destExp, te.Expression))),
                                        YExpression.Block(
                                            YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                            YExpression.Assign(te.Expression, JSUndefinedBuilder.Value),
                                            YExpression.Empty)));
                                    CreateAssignment(arrayInits, ape, te.Expression, createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign);
                                }
                                break;

                            default:
                                throw new NotSupportedException($"{element.Type}");
                        }
                    }

                    var arrayInitBlock = YExpression.Block(arrayInits);
                    // Build a void finally body – only close iterator if NOT exhausted.
                    var closeIterator = YExpression.Block(
                        YExpression.IfThen(
                            YExpression.Not(iterDoneVar),
                            YExpression.IfThen(
                                YExpression.NotEqual(YExpression.Convert(returnableVar.Expression, typeof(object)), YExpression.Null),
                                YExpression.Block(
                                        YExpression.Call(returnableVar.Expression, ReturnableEnumeratorReturnMethod),
                                    YExpression.Empty))),
                        YExpression.Empty);

                    inits.Add(YExpression.TryFinally(arrayInitBlock, closeIterator));
                }

                return;
        }

        throw new NotImplementedException();
    }

    private static bool IsAnonymousFunctionDefinition(AstExpression expression) =>
        expression switch
        {
            AstFunctionExpression { Id: null } => true,
            AstClassExpression { Identifier: null } => true,
            _ => false
        };

    private static YExpression PrepareDestructuringInitializer(AstExpression target, AstExpression initializer, YExpression value)
    {
        if (target is not AstIdentifier id)
            return value;

        return YExpression.Call(
            null,
            PrepareAnonymousFunctionNameForDestructuringMethod,
            value,
            YExpression.Constant(id.Name.Value),
            YExpression.Constant(IsAnonymousFunctionDefinition(initializer)));
    }
}
