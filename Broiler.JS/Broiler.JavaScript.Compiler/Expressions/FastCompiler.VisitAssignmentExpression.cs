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
                // `this` is a ThisExpression, not a binding: assigning to it (`this = x`,
                // `this += x`) is always an early SyntaxError, in sloppy mode too. The
                // parser surfaces it as an identifier named "this", so reject it here
                // rather than silently writing to the captured this-binding.
                if (id.Name.Equals("this"))
                    throw new FastParseException(id.Start, "Invalid left-hand side in assignment");
                id.VerifyIdentifierForUpdate(IsStrictMode);
                break;
        }


        // we need to rewrite left side if it is computed expression with member assignment...
        if (assignmentOperator != TokenTypes.Assign && left.Type == FastNodeType.MemberExpression && left is AstMemberExpression mem)
        {
            var isSuperCompound = mem.Object?.Type == FastNodeType.Super;
            using var objectTemp = scope.Top.GetTempVariable(typeof(JSValue));

            if (isSuperCompound)
            {
                // `super[key] op= rhs` / `super.x op= rhs`: build a single
                // SuperProperty Reference. GetSuperBase is resolved (and captured)
                // before ToPropertyKey, and the captured base plus the converted
                // key drive both the read and the write. Resolving this as an
                // ordinary member would drop the super base and operate on `this`,
                // reading/writing the wrong object.
                using var superBaseTemp = scope.Top.GetTempVariable(typeof(JSValue));

                if (mem.Computed)
                {
                    using var propertyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                    using var keyTemp = scope.Top.GetTempVariable(typeof(JSValue));
                    var leftExp = JSValueBuilder.Index(objectTemp.Expression, superBaseTemp.Expression, keyTemp.Expression);
                    return YExpression.Block(
                        YExpression.Assign(objectTemp.Expression, Visit(mem.Object)),
                        YExpression.Assign(propertyTemp.Expression, Visit(mem.Property)),
                        YExpression.Assign(superBaseTemp.Expression, scope.Top.Super),
                        YExpression.Assign(keyTemp.Expression, YExpression.Call(null, NormalizePropertyKeyMethod, propertyTemp.Expression)),
                        Assign(leftExp, right, assignmentOperator));
                }

                var superLeftExp = JSValueBuilder.Index(objectTemp.Expression, superBaseTemp.Expression, CreatePropertyKeyExpression(mem.Property, false));
                return YExpression.Block(
                    YExpression.Assign(objectTemp.Expression, Visit(mem.Object)),
                    YExpression.Assign(superBaseTemp.Expression, scope.Top.Super),
                    Assign(superLeftExp, right, assignmentOperator));
            }

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

            // A parenthesized assignment target is not an IdentifierReference, so a
            // short-circuit assignment (`||=` / `&&=` / `??=`) of an anonymous function
            // must NOT name it (LogicalAssignment NamedEvaluation requires IsIdentifierRef
            // of the LHS). The JSVariable setter would otherwise infer the variable's name,
            // so clear the placeholder to "" as part of evaluating the RHS (which only runs
            // when the assignment is actually performed). Plain `=` is handled above.
            if (shouldSuppressAnonymousFunctionName
                && assignmentOperator is TokenTypes.AssignBooleanOr or TokenTypes.AssignBooleanAnd or TokenTypes.AssignCoalesce)
            {
                var suppressedRight = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod,
                    Visit(right), YExpression.Constant(""), YExpression.Constant(false));
                return BinaryOperation.Assign(variable.Expression, suppressedRight, assignmentOperator);
            }

            return Assign(variable.Expression, right, assignmentOperator);
        }

        // A `super[key]` assignment target must be an assignable super-index expression.
        // VisitMemberExpression spills the key into a temp (and returns a Block) so a READ
        // observes the spec key-before-GetSuperBase order; a Block is not assignable, so
        // build the index node directly here for the write path. (The write path already
        // evaluates the key as part of the index target, so the spilling reorder is not
        // needed to make the existing super-assignment tests pass.)
        if (left is AstMemberExpression { Computed: true, Object.Type: FastNodeType.Super } superMember)
        {
            var superTarget = JSValueBuilder.Index(
                scope.Top.ThisExpression, scope.Top.Super, VisitExpression(superMember.Property), superMember.Coalesce);
            return Assign(superTarget, right, assignmentOperator);
        }

        // Identifiers, member expressions, array/object patterns and call expressions
        // are handled above. Any other left-hand side (a literal, `this`, a
        // binary/conditional/sequence expression, a function/arrow, ...) has an invalid
        // AssignmentTargetType, which is an early SyntaxError. Without this guard the
        // target lowers to an Expression.Assign onto a non-assignable node, which the IL
        // backend rejects with a leaked CLR NotImplementedException / InvalidProgramException.
        if (left.Type != FastNodeType.MemberExpression)
            throw new FastParseException(left.Start, "Invalid left-hand side in assignment");

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
            // A simple assignment resolves its target Reference once, BEFORE the RHS
            // runs, and the write uses that same Reference even if a direct eval in
            // the RHS introduces a more local binding (§13.15.2 / S11.13.1_A6):
            // `x = (eval("var x"), 1)` must write to the outer binding the reference
            // observed, not the eval-introduced local. Capture which binding the
            // shadow forwards to first, then evaluate the RHS, then write through the
            // captured reference.
            var simpleReferenceTemp = scope.Top.GetTempVariable(typeof(bool));
            var simpleCaptureReference = YExpression.Assign(simpleReferenceTemp.Expression, EvalShadowBuilder.CaptureReference(target));
            var initExpr = Visit(right);
            if (!IsAnonymousFunctionDefinition(right) || suppressAnonymousFunctionName)
                initExpr = YExpression.Call(null, PrepareAnonymousFunctionNameForDestructuringMethod, initExpr, YExpression.Constant(identifier.Name.Value), YExpression.Constant(false));
            var simpleResult = YExpression.Block(
                simpleReferenceTemp.Variable.AsSequence(),
                simpleCaptureReference,
                EvalShadowBuilder.SetCaptured(target, simpleReferenceTemp.Expression, initExpr));
            simpleReferenceTemp.Dispose();
            return simpleResult;
        }

        // A compound assignment resolves its target Reference once, before the RHS
        // runs. Capture which binding the shadow's read observes so a direct eval in
        // the RHS that introduces a local var cannot redirect the write (§13.15.2).
        var referenceTemp = scope.Top.GetTempVariable(typeof(bool));
        var captureReference = YExpression.Assign(referenceTemp.Expression, EvalShadowBuilder.CaptureReference(target));
        var current = EvalShadowBuilder.GetCaptured(target, referenceTemp.Expression);

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
                var logical = YExpression.Block(
                    new Sequence<YParameterExpression> { referenceTemp.Variable, currentTemp.Variable },
                    captureReference,
                    YExpression.Assign(currentTemp.Expression, current),
                    YExpression.Condition(
                        condition,
                        EvalShadowBuilder.SetCaptured(target, referenceTemp.Expression, Visit(right)),
                        currentTemp.Expression,
                        typeof(JSValue)));
                referenceTemp.Dispose();
                return logical;
            }
        }

        var rhs = Visit(right);
        var computed = BinaryOperation.Operation(current, rhs, CompoundAssignmentToBinaryOperator(assignmentOperator));
        var result = YExpression.Block(
            referenceTemp.Variable.AsSequence(),
            captureReference,
            EvalShadowBuilder.SetCaptured(target, referenceTemp.Expression, computed));
        referenceTemp.Dispose();
        return result;
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
        // ResolveWithObject above already walked the with scopes; when it found no
        // binding, the read and the write resolve the remaining scopes directly. Re-running
        // the with-aware resolvers here would fire extra `has` traps on the binding object,
        // so a compound assignment observes a single HasBinding probe like the spec.
        var dynamicAssignment = YExpression.Block(
            valueTemp.Variable.AsSequence(),
            YExpression.Assign(valueTemp.Expression, JSContextBuilder.ResolveIdentifierWithoutWithScopes(key)),
            BinaryOperation.Assign(valueTemp.Expression, Visit(right), assignmentOperator),
            JSContextBuilder.AssignIdentifierWithoutWith(key, valueTemp.Expression, IsStrictMode));

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
                // The with-object lookup above resolved the target Reference BEFORE the
                // RHS (value) runs. When it found no with-object property, write to the
                // lexical/global binding directly: re-running the with resolution here
                // would observe a property the RHS added to a with object and wrongly
                // redirect the write (S11.13.1_A6_T3).
                JSContextBuilder.AssignIdentifierWithoutWith(key, value, IsStrictMode),
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
                // A computed `super[key]` destructuring/for-head target: Visit() would
                // spill the key into a temp and return a Block (the key-before-
                // GetSuperBase ordering used for reads), which is not an assignable
                // reference — the IL backend's VisitAssign would reject the Block-typed
                // left. Build the super-index reference directly so the assignment lowers
                // to the super-index setter, mirroring the direct `super[key] = v` path.
                if (pattern is AstMemberExpression { Computed: true, Object.Type: FastNodeType.Super } superMember)
                {
                    var superTarget = JSValueBuilder.Index(
                        scope.Top.ThisExpression, scope.Top.Super,
                        VisitExpression(superMember.Property), superMember.Coalesce);
                    inits.Add(BinaryOperation.Assign(superTarget, init, TokenTypes.Assign));
                    return;
                }

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
                           inits.Add(AssignDestructuringDefault(
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
                                // Spill the extracted value into a temp and apply the
                                // default as a statement (see AssignDestructuringDefault)
                                // so a `yield`/`await` in the default stays at a statement
                                // boundary instead of inside a value-position coalesce.
                                var apTemp = scope.Top.GetTempVariable(typeof(JSValue));
                                inits.Add(YExpression.Assign(apTemp.Variable, start));
                                inits.Add(AssignDestructuringDefault(apTemp.Expression, defaultValue));
                                CreateAssignment(inits, ap.Left,
                                    apTemp.Expression,
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

                                    arrayInits.Add(AssignDestructuringDefault(moveTemp2.Expression, identifierDefaultValue));
                                    arrayInits.Add(CreateAssignment(be.Left, moveTemp2.Expression, createVariable, newScope,
                                        suppressAnonymousFunctionNameInference, initializeVariable, readOnlyAfterAssign, forceDynamicAssignment));
                                }
                                break;

                            case FastNodeType.SpreadElement:
                                var spe = element as AstSpreadElement;
                                // Per IteratorBindingInitialization for BindingRestElement
                                // (and the analogous AssignmentRestElement runtime semantics),
                                // the rest array is only filled while iteratorRecord.[[Done]]
                                // is false. If a preceding element already exhausted the
                                // iterator, the rest is the empty array and next() must NOT
                                // be called again (test262 destructuring-array-done covers
                                // exactly that observable).
                                CreateAssignment(arrayInits, spe.Argument,
                                    YExpression.Condition(
                                        iterDoneVar,
                                        JSArrayBuilder.New(),
                                        JSArrayBuilder.NewFromElementEnumerator(destExp),
                                        typeof(JSValue)),
                                    createVariable, newScope,
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
        };

        if (excludedKeys.Count == 0)
        {
            restInits.Add(JSObjectBuilder.AddRange(restTemp.Expression, source));
            restInits.Add(restTemp.Expression);
            return YExpression.Block(restTemp.Variable.AsSequence(), restInits);
        }

        // CopyDataProperties with the already-destructured keys excluded. Passing the keys into
        // the copy (rather than copying everything and deleting them afterwards) means an
        // excluded key's descriptor/value is never read — so a Proxy source's traps and an
        // ordinary accessor's getter do not fire for excluded keys (§7.3.25). The excluded keys
        // are stored as the own keys of a scratch object via the ordinary indexer, which
        // normalises every key form (string/index/symbol) exactly as the source's keys are.
        var excludedTemp = scope.Top.GetTempVariable(typeof(JSObject));
        restInits.Add(YExpression.Assign(excludedTemp.Variable, JSObjectBuilder.New()));
        var keyEnumerator = excludedKeys.GetFastEnumerator();
        while (keyEnumerator.MoveNext(out var excludedKey))
            restInits.Add(YExpression.Assign(
                JSValueBuilder.Index(excludedTemp.Expression, excludedKey),
                JSBooleanBuilder.True));
        restInits.Add(JSObjectBuilder.AddRange(restTemp.Expression, source, excludedTemp.Expression));
        restInits.Add(restTemp.Expression);
        return YExpression.Block(
            new Sequence<YParameterExpression> { restTemp.Variable, excludedTemp.Variable },
            restInits);
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

    // Apply a destructuring default ("= initializer") to a temp holding the
    // extracted value, when that value is `undefined`. Emitted as a void IfThen
    // statement (`if (temp === undefined) temp = default;`) rather than the
    // value-position coalesce `temp = temp ?? default`. The two are behaviourally
    // identical (the default is evaluated lazily, only on undefined), but the
    // statement form keeps a `yield`/`await` inside the default at a statement
    // boundary. The generator/async state-machine rewriter suspends by emitting a
    // mid-stream `return`, which corrupts the IL evaluation stack if it occurs
    // inside a value-position sub-expression (e.g. the right operand of `??`).
    private static YExpression AssignDestructuringDefault(YExpression temp, YExpression defaultValue)
        => YExpression.IfThen(
            JSValueBuilder.IsUndefined(temp),
            // The if-body must be void (the assigned value is discarded); leaving a
            // value on the stack would unbalance the then/else branches.
            YExpression.Block(YExpression.Assign(temp, defaultValue), YExpression.Empty));

    private static YExpression PrepareDestructuringInitializer(AstExpression target, AstExpression initializer, YExpression value)
    {
        if (target is not AstIdentifier id)
            return value;

        return YExpression.Call(
            null,
            PrepareAnonymousFunctionNameForDestructuringMethod,
            value,
            YExpression.Constant(id.InferenceName),
            YExpression.Constant(IsAnonymousFunctionDefinition(initializer)));
    }
}
