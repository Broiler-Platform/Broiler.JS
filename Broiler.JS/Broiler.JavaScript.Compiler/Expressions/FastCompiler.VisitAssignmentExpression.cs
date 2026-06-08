using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Patterns;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.Engine.Core;
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
    private static readonly MethodInfo CloseIteratorMethod = typeof(FastCompiler)
        .GetMethod(nameof(CloseIterator), BindingFlags.NonPublic | BindingFlags.Static, [typeof(IReturnableEnumerator)])
        ?? throw new InvalidOperationException("FastCompiler.CloseIterator(IReturnableEnumerator) not found");
    private static readonly MethodInfo CloseIteratorIgnoringErrorsMethod = typeof(FastCompiler)
        .GetMethod(nameof(CloseIteratorIgnoringErrors), BindingFlags.NonPublic | BindingFlags.Static, [typeof(IReturnableEnumerator)])
        ?? throw new InvalidOperationException("FastCompiler.CloseIteratorIgnoringErrors(IReturnableEnumerator) not found");
    private static readonly MethodInfo PrepareAnonymousFunctionNameForDestructuringMethod = typeof(JSVariable)
        .GetMethod(nameof(JSVariable.PrepareAnonymousFunctionNameForDestructuring), [typeof(JSValue), typeof(string), typeof(bool)])
        ?? throw new InvalidOperationException("JSVariable.PrepareAnonymousFunctionNameForDestructuring(JSValue, string, bool) not found");
    private static readonly MethodInfo NormalizePropertyKeyMethod = typeof(JSValue)
        .GetMethod("NormalizePropertyKey", BindingFlags.NonPublic | BindingFlags.Static, [typeof(JSValue)])
        ?? throw new InvalidOperationException("JSValue.NormalizePropertyKey(JSValue) not found");

    private static readonly MethodInfo ThrowInvalidAssignmentReferenceMethod = typeof(FastCompiler)
        .GetMethod(nameof(ThrowInvalidAssignmentReference), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("FastCompiler.ThrowInvalidAssignmentReference() not found");

    private static JSValue ThrowInvalidAssignmentReference() =>
        throw JSEngine.NewReferenceError("Invalid left-hand side in assignment");


    private static void CloseIterator(IReturnableEnumerator returnable)
    {
        returnable?.Return();
    }

    private static void CloseIteratorIgnoringErrors(IReturnableEnumerator returnable)
    {
        try
        {
            returnable?.Return();
        }
        catch
        {
        }
    }

    private YExpression VisitAssignmentExpression(AstExpression left, TokenTypes assignmentOperator, AstExpression right)
    {
        // A function call (or other invalid reference) used as an assignment
        // target is a runtime ReferenceError. The target is still evaluated for
        // its side effects, but the right-hand side is not, matching the
        // behaviour already used for update expressions such as `f()++`.
        if (left.Type == FastNodeType.CallExpression)
        {
            return YExpression.Block(
                Visit(left),
                YExpression.Call(null, ThrowInvalidAssignmentReferenceMethod));
        }

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
            var shouldSuppressAnonymousFunctionName = left.WasParenthesized && IsAnonymousFunctionDefinition(right);

            // A name that resolves outside a sloppy parameter-eval function is routed
            // through an EvalShadowVariable: writes go through SetValue so they forward
            // to the outer binding until the eval introduces the var.
            if (TryResolveEvalShadow(identifier.Name, out var shadowVariable))
                return ShadowAssign(shadowVariable, identifier, right, assignmentOperator, shouldSuppressAnonymousFunctionName);

            // Reassigning `arguments` in a non-arrow function must target the
            // function-local binding. Materialize it first so the assignment
            // resolves to the static variable instead of the dynamic context name.
            if (identifier.Name.Equals("arguments")
                && scope.Top.Function?.IsArrowFunction != true
                && scope.Top.RootScope.Function != null)
            {
                VisitIdentifier(identifier, false);
            }

            if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable == null)
            {
                if (assignmentOperator == TokenTypes.Assign && (!IsAnonymousFunctionDefinition(right) || shouldSuppressAnonymousFunctionName))
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
                if (!IsAnonymousFunctionDefinition(right) || shouldSuppressAnonymousFunctionName)
                    initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(""), YExpression.Constant(false));
                return JSVariableBuilder.Assign(variable.Variable, initExpr);
            }

            if (assignmentOperator == TokenTypes.Assign)
            {
                var initExpr = Visit(right);
                if (!IsAnonymousFunctionDefinition(right) || shouldSuppressAnonymousFunctionName)
                    initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(""), YExpression.Constant(false));
                if (IsDestructuringAssignmentExpression(right))
                    return AssignMaterializedValue(variable.Expression, initExpr);
                return YExpression.Assign(variable.Expression, initExpr);
            }

            return Assign(variable.Expression, right, assignmentOperator);
        }

        return Assign(Visit(left), right, assignmentOperator);
    }

    // Compiles an assignment whose target is an EvalShadowVariable. Reads/writes use
    // GetValue/SetValue (the binding may forward to its outer binding), so the target
    // cannot be used as an ordinary assignable expression.
    private YExpression ShadowAssign(FastFunctionScope.VariableScope shadow, AstIdentifier identifier, AstExpression right, TokenTypes assignmentOperator, bool suppressAnonymousFunctionName)
    {
        var target = shadow.Variable;

        if (assignmentOperator == TokenTypes.Assign)
        {
            var initExpr = Visit(right);
            if (!IsAnonymousFunctionDefinition(right) || suppressAnonymousFunctionName)
                initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(identifier.Name.Value), YExpression.Constant(false));
            return EvalShadowBuilder.SetValue(target, initExpr);
        }

        var current = EvalShadowBuilder.GetValue(target);
        var rhs = Visit(right);

        switch (assignmentOperator)
        {
            case TokenTypes.AssignCoalesce:
            case TokenTypes.AssignBooleanAnd:
            case TokenTypes.AssignBooleanOr:
            {
                using var currentTemp = scope.Top.GetTempVariable(typeof(JSValue));
                var condition = assignmentOperator switch
                {
                    TokenTypes.AssignCoalesce => JSValueBuilder.IsNullOrUndefined(currentTemp.Expression),
                    TokenTypes.AssignBooleanAnd => JSValueBuilder.BooleanValue(currentTemp.Expression),
                    _ => YExpression.Not(JSValueBuilder.BooleanValue(currentTemp.Expression)),
                };
                return YExpression.Block(
                    currentTemp.Variable.AsSequence(),
                    YExpression.Assign(currentTemp.Expression, current),
                    YExpression.Condition(
                        condition,
                        EvalShadowBuilder.SetValue(target, rhs),
                        currentTemp.Expression,
                        typeof(JSValue)));
            }
        }

        var computed = BinaryOperation.Operation(current, rhs, CompoundAssignmentToBinaryOperator(assignmentOperator));
        return EvalShadowBuilder.SetValue(target, computed);
    }

    private static TokenTypes CompoundAssignmentToBinaryOperator(TokenTypes assignmentOperator) => assignmentOperator switch
    {
        TokenTypes.AssignAdd => TokenTypes.Plus,
        TokenTypes.AssignSubtract => TokenTypes.Minus,
        TokenTypes.AssignMultiply => TokenTypes.Multiply,
        TokenTypes.AssignDivide => TokenTypes.Divide,
        TokenTypes.AssignMod => TokenTypes.Mod,
        TokenTypes.AssignBitwideAnd => TokenTypes.BitwiseAnd,
        TokenTypes.AssignBitwideOr => TokenTypes.BitwiseOr,
        TokenTypes.AssignXor => TokenTypes.Xor,
        TokenTypes.AssignLeftShift => TokenTypes.LeftShift,
        TokenTypes.AssignRightShift => TokenTypes.RightShift,
        TokenTypes.AssignUnsignedRightShift => TokenTypes.UnsignedRightShift,
        TokenTypes.AssignPower => TokenTypes.Power,
        _ => throw new NotSupportedException($"Unsupported compound assignment {assignmentOperator}"),
    };

    private YExpression AssignIdentifier(AstIdentifier identifier, AstExpression right, TokenTypes assignmentOperator)
    {
        if (assignmentOperator == TokenTypes.Assign)
            return AssignIdentifier(identifier, Visit(right));

        var key = KeyOfName(identifier.Name);
        using var withObjectTemp = scope.Top.GetTempVariable(typeof(JSObject));
        using var valueTemp = scope.Top.GetTempVariable(typeof(JSValue));

        var retainedWithReference = JSValueBuilder.Index(withObjectTemp.Expression, key);
        var retainedWithAssignment = YExpression.Block(
            valueTemp.Variable.AsSequence(),
            YExpression.Assign(valueTemp.Expression, retainedWithReference),
            BinaryOperation.Assign(valueTemp.Expression, Visit(right), assignmentOperator),
            JSContextBuilder.AssignWithObjectIdentifier(withObjectTemp.Expression, key, valueTemp.Expression, IsStrictMode));
        var dynamicAssignment = YExpression.Block(
            valueTemp.Variable.AsSequence(),
            YExpression.Assign(valueTemp.Expression, JSContextBuilder.ResolveIdentifier(key)),
            BinaryOperation.Assign(valueTemp.Expression, Visit(right), assignmentOperator),
            JSContextBuilder.AssignIdentifier(key, valueTemp.Expression, IsStrictMode));

        return YExpression.Block(
            withObjectTemp.Variable.AsSequence(),
            YExpression.Assign(withObjectTemp.Expression, JSContextBuilder.ResolveWithObject(key)),
            YExpression.Condition(
                YExpression.NotEqual(withObjectTemp.Expression, YExpression.Constant(null, typeof(JSObject))),
                retainedWithAssignment,
                dynamicAssignment,
                typeof(JSValue)));
    }

    private YExpression AssignIdentifier(AstIdentifier identifier, YExpression value)
    {
        var key = KeyOfName(identifier.Name);
        using var withObjectTemp = scope.Top.GetTempVariable(typeof(JSObject));
        var retainedWithReference = JSValueBuilder.Index(withObjectTemp.Expression, key);

        return YExpression.Block(
            withObjectTemp.Variable.AsSequence(),
            YExpression.Assign(withObjectTemp.Expression, JSContextBuilder.ResolveWithObject(key)),
            YExpression.Condition(
                YExpression.NotEqual(withObjectTemp.Expression, YExpression.Constant(null, typeof(JSObject))),
                JSContextBuilder.AssignWithObjectIdentifier(withObjectTemp.Expression, key, value, IsStrictMode),
                JSContextBuilder.AssignIdentifier(key, value, IsStrictMode),
                typeof(JSValue)));
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

    private YExpression CreateAssignment(AstExpression pattern, YExpression init, bool createVariable = false, bool newScope = false,
        bool suppressAnonymousFunctionNameInference = false, bool initializeVariable = true, bool readOnlyAfterAssign = false,
        bool forceDynamicAssignment = false)
    {
        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        var inits = new Sequence<YExpression>();
        inits.Add(YExpression.Assign(temp.Variable, init));
        CreateAssignment(inits, pattern, temp.Expression, createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment);
        inits.Add(temp.Expression);

        return YExpression.Block(new Sequence<YParameterExpression> { temp.Variable }, inits);
    }

    private void CreateAssignment(Sequence<YExpression> inits, AstExpression pattern, YExpression init, bool createVariable = false, bool newScope = false,
        bool suppressAnonymousFunctionNameInference = false, bool initializeVariable = true, bool readOnlyAfterAssign = false,
        bool forceDynamicAssignment = false)
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
                        if (!forceDynamicAssignment && TryResolveEvalShadow(id.Name, out var shadowVar))
                        {
                            if (suppressAnonymousFunctionNameInference)
                                init = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, init, YExpression.Constant(id.Name.Value), YExpression.Constant(false));
                            inits.Add(EvalShadowBuilder.SetValue(shadowVar.Variable, init));
                            return;
                        }

                        if (forceDynamicAssignment || !TryGetStaticIdentifierVariable(id, out var variable) || variable == null)
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
                    var excludedKeys = new Sequence<YExpression>();

                    while (en.MoveNext(out var property))
                    {
                        YExpression start = null;
                        if (property.Spread || property.Key == null)
                        {
                            CreateAssignment(inits, property.Value, CreateObjectRest(init, excludedKeys), createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment);
                            continue;
                        }

                        if (property.Key.Type == FastNodeType.SpreadElement)
                        {
                            var spread = (AstSpreadElement)property.Key;
                            CreateAssignment(inits, spread.Argument, CreateObjectRest(init, excludedKeys), createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment);
                            continue;
                        }

                        var id = property.Key;
                        var propertyInit = property.Init;

                        // Determine the member access expression `init[key]` and the key to
                        // exclude when collecting an object rest.
                        YExpression memberAccess;
                        if (property.Computed
                            && id.Type != FastNodeType.Identifier
                            && id.Type != FastNodeType.Literal)
                        {
                            // A computed key that is an arbitrary expression (e.g.
                            // `{ ['x' + 'y']: t }`, `{ [a.b]: t }`, `{ [fn()]: t }`).
                            // Evaluate it exactly once and normalize it to a property key so
                            // its observable side effects (and any errors raised while
                            // evaluating the key, such as `a.b` when `a` is undefined) occur
                            // in spec order, before the destructuring target is read.
                            var keyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                            inits.Add(YExpression.Assign(
                                keyTemp.Variable,
                                YExpression.Call(null, NormalizePropertyKeyMethod, Visit(id))));
                            excludedKeys.Add(keyTemp.Expression);
                            memberAccess = JSValueBuilder.Index(init, keyTemp.Expression);
                        }
                        else
                        {
                            var key = CreatePropertyKeyExpression(id, property.Computed);
                            excludedKeys.Add(key);
                            memberAccess = CreateMemberExpression(init, id, property.Computed);
                        }

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
                               memberAccess));
                           inits.Add(JSValueExtensionsBuilder.AssignCoalesce(
                               piTemp.Expression,
                               defaultValue));
                           start = piTemp.Expression;
                        }
                        else
                        {
                           start = memberAccess;
                        }

                        switch (property.Value.Type)
                        {
                            case FastNodeType.Identifier:
                            case FastNodeType.MemberExpression:
                            case FastNodeType.ArrayPattern:
                            case FastNodeType.ObjectPattern:
                                CreateAssignment(inits, property.Value, start, createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment);
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
                                    readOnlyAfterAssign: readOnlyAfterAssign,
                                    forceDynamicAssignment: forceDynamicAssignment);
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
                                                suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment),
                                            YExpression.Empty),
                                        YExpression.Block(
                                            CreateAssignment(element, moveTemp.Expression, createVariable, newScope,
                                                suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment),
                                            YExpression.Empty)));
                                }
                                break;
                            case FastNodeType.MemberExpression:
                                var member = (AstMemberExpression)element;
                                using (var objectTemp = scope.Top.GetTempVariable(typeof(JSValue)))
                                using (var moveTemp = scope.Top.GetTempVariable(typeof(JSValue)))
                                {
                                    arrayInits.Add(YExpression.Assign(objectTemp.Variable, Visit(member.Object)));
                                    YExpression memberTarget;
                                    if (member.Computed)
                                    {
                                        var propertyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                                        var keyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                                        arrayInits.Add(YExpression.Assign(propertyTemp.Variable, Visit(member.Property)));
                                        memberTarget = JSValueBuilder.Index(objectTemp.Expression, keyTemp.Expression);
                                        arrayInits.Add(YExpression.IfThen(
                                            YExpression.OrElse(iterDoneVar,
                                                YExpression.Not(IElementEnumeratorBuilder.MoveNext(destExp, moveTemp.Expression))),
                                            YExpression.Block(
                                                YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                                YExpression.Assign(moveTemp.Expression, JSUndefinedBuilder.Value),
                                                YExpression.Empty)));
                                        arrayInits.Add(YExpression.Call(null, RequireObjectCoercibleMethod, objectTemp.Expression));
                                        arrayInits.Add(YExpression.Assign(keyTemp.Variable, YExpression.Call(null, NormalizePropertyKeyMethod, propertyTemp.Expression)));
                                        arrayInits.Add(BinaryOperation.Assign(memberTarget, moveTemp.Expression, TokenTypes.Assign));
                                        keyTemp.Dispose();
                                        propertyTemp.Dispose();
                                    }
                                    else
                                    {
                                        memberTarget = CreateMemberExpression(objectTemp.Expression, member.Property, false);
                                        arrayInits.Add(YExpression.IfThen(
                                            YExpression.OrElse(iterDoneVar,
                                                YExpression.Not(IElementEnumeratorBuilder.MoveNext(destExp, moveTemp.Expression))),
                                            YExpression.Block(
                                                YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                                YExpression.Assign(moveTemp.Expression, JSUndefinedBuilder.Value),
                                                YExpression.Empty)));
                                        arrayInits.Add(BinaryOperation.Assign(memberTarget, moveTemp.Expression, TokenTypes.Assign));
                                    }
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
                                        suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment));
                                }
                                break;

                            case FastNodeType.SpreadElement:
                                var spe = element as AstSpreadElement;
                                CreateAssignment(arrayInits, spe.Argument, JSArrayBuilder.NewFromElementEnumerator(destExp), createVariable, newScope,
                                    suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment);
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
                                    CreateAssignment(arrayInits, ape, te.Expression, createVariable, newScope, suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment);
                                }
                                break;

                            default:
                                throw new NotSupportedException($"{element.Type}");
                        }
                    }

                    arrayInits.Add(YExpression.Empty);
                    var arrayInitBlock = YExpression.Block(arrayInits);
                    // Build a void finally body – only close iterator if NOT exhausted.
                    var closeIterator = YExpression.Block(
                        YExpression.IfThen(
                            YExpression.Not(iterDoneVar),
                            YExpression.Block(
                                YExpression.Call(null, CloseIteratorMethod, returnableVar.Expression),
                                YExpression.Empty)),
                        YExpression.Empty);
                    var caughtException = scope.Top.CreateException("#arrayDestructuringIteratorClose");
                    var closeIteratorAfterThrow = YExpression.Block(
                        YExpression.IfThen(
                            YExpression.Not(iterDoneVar),
                            YExpression.Block(
                                YExpression.Call(null, CloseIteratorIgnoringErrorsMethod, returnableVar.Expression),
                                YExpression.Assign(iterDoneVar, YExpression.Constant(true)),
                                YExpression.Empty)),
                        YExpression.Throw(caughtException.Expression));

                    inits.Add(YExpression.TryCatchFinally(
                        arrayInitBlock,
                        closeIterator,
                        YExpression.Catch(caughtException.Variable, closeIteratorAfterThrow)));
                }

                return;
        }

        throw new NotImplementedException();
    }

    private YExpression CreateObjectRest(YExpression source, Sequence<YExpression> excludedKeys)
    {
        var restTemp = scope.Top.GetTempVariable(typeof(JSObject));
        var restInits = new Sequence<YExpression>
        {
            YExpression.Assign(restTemp.Variable, JSObjectBuilder.New()),
            JSObjectBuilder.AddRange(restTemp.Expression, source)
        };
        var deleteKeys = excludedKeys.GetFastEnumerator();
        while (deleteKeys.MoveNext(out var excludedKey))
            restInits.Add(JSValueBuilder.Delete(restTemp.Expression, excludedKey));
        restInits.Add(restTemp.Expression);
        return YExpression.Block(restTemp.Variable.AsSequence(), restInits);
    }

    private static bool IsAnonymousFunctionDefinition(AstExpression expression) =>
        expression switch
        {
            AstFunctionExpression { Id: null } => true,
            AstClassExpression { Identifier: null } => true,
            _ => false
        };

    private static bool IsDestructuringAssignmentExpression(AstExpression expression) =>
        expression is AstBinaryExpression
        {
            Operator: TokenTypes.Assign,
            Left.Type: FastNodeType.ArrayPattern or FastNodeType.ObjectPattern
        };

    private YExpression AssignMaterializedValue(YExpression target, YExpression value)
    {
        using var temp = scope.Top.GetTempVariable(typeof(JSValue));
        return YExpression.Block(
            temp.Variable.AsSequence(),
            YExpression.Assign(temp.Expression, value),
            YExpression.Assign(target, temp.Expression),
            temp.Expression);
    }

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
