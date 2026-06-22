using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Engine.Core;
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

    private static readonly MethodInfo ThrowInvalidUpdateReferenceMethod = typeof(FastCompiler)
        .GetMethod(nameof(ThrowInvalidUpdateReference), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("FastCompiler.ThrowInvalidUpdateReference() not found");

    private static JSValue ThrowInvalidUpdateReference() =>
        throw JSEngine.NewReferenceError("Invalid left-hand side expression for update");

    private BExpression InternalVisitUpdateExpression(AstUnaryExpression updateExpression)
    {
        // added support for a++, a--
        updateExpression.Argument.VerifyIdentifierForUpdate(IsStrictMode);

        if (updateExpression.Argument is AstCallExpression)
        {
            return BExpression.Block(
                VisitExpression(updateExpression.Argument),
                BExpression.Call(null, ThrowInvalidUpdateReferenceMethod));
        }

        if (updateExpression.Argument is AstIdentifier identifier)
        {
            if (!TryGetStaticIdentifierVariable(identifier, out var variable) || variable == null)
            {
                using var withObject = scope.Top.GetTempVariable(typeof(JSObject));
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<BParameterExpression> { withObject.Variable, current.Variable };
                var globalKey = KeyOfName(identifier.Name);

                if (previous != null)
                    variables.Add(previous.Variable);

                var dynamicStatements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, JSContextBuilder.ResolveIdentifier(globalKey)),
                    // Coerce to Number/BigInt once: the postfix result is the coerced
                    // old value and the operand's valueOf must run exactly once.
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                    dynamicStatements.Add(BExpression.Assign(previous.Variable, current.Expression));

                dynamicStatements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression)
                        : JSValueBuilder.Decrement(current.Expression)));
                dynamicStatements.Add(JSContextBuilder.AssignIdentifier(globalKey, current.Expression));
                dynamicStatements.Add(previous?.Expression ?? current.Expression);

                var retainedWithReference = JSValueBuilder.Index(withObject.Expression, globalKey);
                var withStatements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, retainedWithReference),
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                    withStatements.Add(BExpression.Assign(previous.Variable, current.Expression));

                withStatements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression)
                        : JSValueBuilder.Decrement(current.Expression)));
                withStatements.Add(JSContextBuilder.AssignWithObjectIdentifier(withObject.Expression, globalKey, current.Expression, IsStrictMode));
                withStatements.Add(previous?.Expression ?? current.Expression);

                return BExpression.Block(
                    variables,
                    BExpression.Assign(withObject.Expression, JSContextBuilder.ResolveWithObject(globalKey)),
                    BExpression.Condition(
                        BExpression.NotEqual(withObject.Expression, BExpression.Constant(null, typeof(JSObject))),
                        BExpression.Block(withStatements),
                        BExpression.Block(dynamicStatements),
                        typeof(JSValue)));
            }

            if (variable.Variable?.Type == typeof(JSVariable) && !variable.IsDeletable)
            {
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<BParameterExpression> { current.Variable };
                var statements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, variable.Expression),
                    // Coerce to Number/BigInt once: the postfix result is the coerced
                    // old value (`var y = "1"++` yields the Number 1).
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                {
                    variables.Add(previous.Variable);
                    statements.Add(BExpression.Assign(previous.Variable, current.Expression));
                }

                statements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression)
                        : JSValueBuilder.Decrement(current.Expression)));
                statements.Add(BExpression.Assign(variable.Expression, current.Expression));
                statements.Add(previous?.Expression ?? current.Expression);

                return BExpression.Block(variables, statements);
            }

            // An eval-introduced global `var` is deletable: its read goes through the throwing
            // global resolution (ReadExpression, which raises a ReferenceError once the binding has
            // been deleted) while its write targets the assignable global-object property
            // (Expression). The generic member-update path below visits the identifier once and uses
            // that single expression as both the read source and the assignment target — for these
            // bindings the read is a (non-assignable) method Call, so the write must be split out
            // here to target the property index instead.
            if (variable.ReadExpression != null)
            {
                using var current = scope.Top.GetTempVariable(typeof(JSValue));
                using var previous = updateExpression.Prefix ? null : scope.Top.GetTempVariable(typeof(JSValue));
                var variables = new Sequence<BParameterExpression> { current.Variable };
                var statements = new Sequence<BExpression>
                {
                    BExpression.Assign(current.Variable, variable.ReadExpression),
                    // Coerce to Number/BigInt once: the postfix result is the coerced old value.
                    BExpression.Assign(current.Variable, JSValueBuilder.ToNumeric(current.Expression))
                };

                if (previous != null)
                {
                    variables.Add(previous.Variable);
                    statements.Add(BExpression.Assign(previous.Variable, current.Expression));
                }

                statements.Add(BExpression.Assign(
                    current.Variable,
                    updateExpression.Operator == UnaryOperator.Increment
                        ? JSValueBuilder.Increment(current.Expression)
                        : JSValueBuilder.Decrement(current.Expression)));
                statements.Add(BExpression.Assign(variable.Expression, current.Expression));
                statements.Add(previous?.Expression ?? current.Expression);

                return BExpression.Block(variables, statements);
            }
        }

        var list = new Sequence<BExpression>();

        FastFunctionScope.VariableScope target = null;
        FastFunctionScope.VariableScope key = null;
        FastFunctionScope.VariableScope superBase = null;
        FastFunctionScope.VariableScope @return = null;
        var right = VisitExpression(updateExpression.Argument);

        if (updateExpression.Argument is AstMemberExpression memberExpression)
        {
            var isSuper = memberExpression.Object?.Type == FastNodeType.Super;

            target = scope.Top.GetTempVariable(typeof(JSValue));
            list.Add(BExpression.Assign(target.Variable, VisitExpression(memberExpression.Object)));

            if (isSuper)
            {
                // `++super[key]` / `++super.x`: the spec builds a single
                // SuperProperty Reference whose base (GetSuperBase) is resolved
                // BEFORE ToPropertyKey, and reuses that base and key for both the
                // read and the write. Capture them once here: evaluate the key
                // expression, then GetSuperBase, then normalize the key (whose
                // toString must observe the already-resolved base). A plain
                // member update would drop the super base and use `this` as the
                // base, reading/writing the wrong object.
                superBase = scope.Top.GetTempVariable(typeof(JSValue));

                if (memberExpression.Computed)
                {
                    key = scope.Top.GetTempVariable(typeof(JSValue));
                    list.Add(BExpression.Assign(key.Variable, VisitExpression(memberExpression.Property)));
                    list.Add(BExpression.Assign(superBase.Variable, scope.Top.Super));
                    list.Add(BExpression.Assign(key.Variable, BExpression.Call(null, NormalizeUpdatePropertyKeyMethod, key.Expression)));
                    right = JSValueBuilder.Index(target.Expression, superBase.Expression, key.Expression);
                }
                else
                {
                    list.Add(BExpression.Assign(superBase.Variable, scope.Top.Super));
                    right = JSValueBuilder.Index(target.Expression, superBase.Expression, CreatePropertyKeyExpression(memberExpression.Property, false));
                }
            }
            else if (memberExpression.Computed)
            {
                key = scope.Top.GetTempVariable(typeof(JSValue));
                list.Add(BExpression.Assign(key.Variable, VisitExpression(memberExpression.Property)));
                // Per spec, ToObject(base) must precede ToPropertyKey(key).
                // RequireObjectCoercible throws TypeError for null/undefined before
                // NormalizePropertyKey can trigger observable side effects (e.g. toString).
                list.Add(BExpression.Call(null, RequireObjectCoercibleMethod, target.Expression));
                list.Add(BExpression.Assign(key.Variable, BExpression.Call(null, NormalizeUpdatePropertyKeyMethod, key.Expression)));
                right = JSValueBuilder.Index(target.Expression, key.Expression);
            }
            else
            {
                right = CreateMemberExpression(target.Expression, memberExpression.Property, false);
            }
        }

        switch (right.NodeType)
        {
            case BExpressionType.Index:
                if (target == null)
                {
                    var index = right as BIndexExpression;
                    target = scope.Top.GetTempVariable(index.Type);
                    list.Add(BExpression.Assign(target.Variable, index.Target));
                    right = BExpression.Index(target.Variable, index.Property, index.Arguments);
                }
                break;
        }

        // ToNumeric reads the member/index once and coerces the operand to a
        // Number/BigInt exactly once, so a getter with side effects is observed only
        // once and the result of a postfix update is the coerced old value
        // (`obj.x++` where obj.x is "1" yields the Number 1, not the String "1").
        var coerced = scope.Top.GetTempVariable(typeof(JSValue));
        list.Add(BExpression.Assign(coerced.Variable, JSValueBuilder.ToNumeric(right)));

        var newValue = updateExpression.Operator == UnaryOperator.Increment
            ? JSValueBuilder.Increment(coerced.Expression)
            : JSValueBuilder.Decrement(coerced.Expression);

        if (updateExpression.Prefix)
        {
            // For prefix update on member expressions, save the computed new value
            // before writing it back. The write may silently fail (e.g. non-writable
            // property in sloppy mode), but the expression must return the new value.
            @return = scope.Top.GetTempVariable(typeof(JSValue));
            list.Add(BExpression.Assign(@return.Variable, newValue));
            list.Add(BExpression.Assign(right, @return.Variable));
        }
        else
        {
            // Postfix: the coerced old value is the result; write the new value back.
            list.Add(BExpression.Assign(right, newValue));
            @return = coerced;
        }

        list.Add(@return.Variable);

        var r = BExpression.Block(list);
        @return?.Dispose();
        if (!ReferenceEquals(@return, coerced))
            coerced.Dispose();
        key?.Dispose();
        superBase?.Dispose();
        target?.Dispose();

        return r;
    }
}
